
using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Application;

/// <summary>
/// Orchestratore dell'intera prova. Unica sorgente di verità dela test
/// </summary>
public sealed class ChargingTestOrchestrator : IChargingTestOrchestrator
{
    private readonly IMesGateway _mes;
    private readonly IConnectorFactory _connectorFactory;
    private readonly IRetryPolicy _retry;
    private readonly ISessionLogger _log;

    public ChargingTestOrchestrator(
        IMesGateway mes,
        IConnectorFactory connectorFactory,
        IRetryPolicy retry,
        ISessionLogger log)
    {
        _mes = mes;
        _connectorFactory = connectorFactory;
        _retry = retry;
        _log = log;
    }

    public async Task<SessionResult> RunAsync(
        string serialNumber,
        IProgress<SessionUpdate>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number is required.", nameof(serialNumber));

        var sessionId = Guid.NewGuid().ToString("N");
        var machine = new TestSessionStateMachine();
        IDutConnector? dut = null;
        ITestSystemHardwareConnector? hardware = null;

        try
        {
            Move(machine, TestSessionState.LoadingSetup, SessionMessageCode.LoadingSetup, progress);
            var setup = await _retry.ExecuteAsync(
                ct => _mes.DownloadSetupAsync(serialNumber, ct), cancellationToken);
            setup.Specification.Validate();

            Move(machine, TestSessionState.Connecting, SessionMessageCode.Connecting, progress);
            dut = _connectorFactory.CreateDut(setup.DutConnector);
            hardware = _connectorFactory.CreateTestSystemHardware(setup.TestSystemHardwareConnector);
            await _retry.ExecuteAsync(dut.ConnectAsync, cancellationToken);
            await _retry.ExecuteAsync(hardware.ConnectAsync, cancellationToken);

            var command = new ChargeStartCommand(sessionId, setup.Specification.Duration,
                setup.Specification.RequestedPowerKw);
            Move(machine, TestSessionState.Starting, SessionMessageCode.Starting, progress);

            await _retry.ExecuteAsync(ct => hardware.StartMonitoringAsync(command, ct), cancellationToken);
            await _retry.ExecuteAsync(ct => dut.StartChargingAsync(command, ct), cancellationToken);

            Move(machine, TestSessionState.Monitoring, SessionMessageCode.Monitoring, progress);
            var sessionReadings = await ReadingMonitor.MonitorAsync(
                dut, hardware, setup.Specification,
                (dutReading, hardwareReading) => progress?.Report(new SessionUpdate(
                    machine.State, new SessionMessage(SessionMessageCode.LiveReadingsUpdated),
                    dutReading, hardwareReading)),
                cancellationToken);

            Move(machine, TestSessionState.Evaluating, SessionMessageCode.Evaluating, progress);
            var result = ResultEvaluator.Evaluate(sessionId, serialNumber, setup.Specification, sessionReadings);

            Move(machine, TestSessionState.UploadingResult, SessionMessageCode.UploadingResult, progress);
            await _retry.ExecuteAsync(ct => _mes.UploadResultAsync(result, ct), cancellationToken);

            Move(machine, TestSessionState.Completed, SessionMessageCode.Completed, progress, result);
            _log.Info($"Session {sessionId} completed with {result.Outcome}.");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
             machine.Cancel();
            var cancelled = new SessionResult(sessionId, serialNumber, TestOutcome.Cancelled,
                Array.Empty<CheckResult>(), null, "Cancelled by operator.");
            progress?.Report(new SessionUpdate(machine.State,
                new SessionMessage(SessionMessageCode.CancelledByOperator), Result: cancelled));
            _log.Info($"Session {sessionId} cancelled by operator.");
            return cancelled;
        }
        catch (Exception ex)
        {
            machine.Fail();
            _log.Error($"Session {sessionId} failed.", ex);
            var failed = new SessionResult(sessionId, serialNumber, TestOutcome.Error,
                Array.Empty<CheckResult>(), null, ex.Message);
            progress?.Report(new SessionUpdate(machine.State,
                new SessionMessage(SessionMessageCode.SessionError, ex.Message), Result: failed));
            return failed;
        }
        finally
        {
            await DisposeQuietlyAsync(dut);
            await DisposeQuietlyAsync(hardware);
        }
    }

    private void Move(TestSessionStateMachine machine, TestSessionState state, SessionMessageCode messageCode,
        IProgress<SessionUpdate>? progress, SessionResult? result = null)
    {
        machine.MoveTo(state);
        _log.Info(messageCode.ToString());
        progress?.Report(new SessionUpdate(state, new SessionMessage(messageCode), Result: result));
    }

    private static async ValueTask DisposeQuietlyAsync(IAsyncDisposable? disposable)
    {
        if (disposable is null) return;
        try { await disposable.DisposeAsync(); }
        catch { /* sarà disponibile nei log del connettore concreto */ }
    }
}
