
using Alpitronic.TestSystem.Domain;
using System.Runtime.CompilerServices;


namespace Alpitronic.TestSystem.Infrastructure.Mocks;

/// <summary>
/// Un solo scenario controlla DUT e banco: rende riproducibili pass, fail,
/// timeout e misure divergenti senza alcun hardware fisico.
/// </summary>
public sealed record MockTestScenario(
    decimal DutPowerFactor = 1.00m,
    decimal DutEnergyFactor = 1.00m,
    decimal HardwarePowerFactor = 1.00m,
    decimal HardwareEnergyFactor = 1.00m,
    bool FailOnConnect = false,
    int? ThrowAfterSample = null);

public sealed class MockDutConnector : IDutConnector
{
    private readonly MockTestScenario _scenario;
    private bool _connected;
    private bool _started;
    public string Name => "Mock DUT";
    public ChargeStartCommand? LastCommand { get; private set; }

    public MockDutConnector(MockTestScenario scenario) => _scenario = scenario;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_scenario.FailOnConnect) throw new TimeoutException("Simulated DUT connection timeout.");
        _connected = true;
        return Task.CompletedTask;
    }

    public Task StartChargingAsync(ChargeStartCommand command, CancellationToken cancellationToken)
    {
        if (!_connected) throw new InvalidOperationException("DUT must be connected before start.");
        _started = true;
        LastCommand = command;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<MeterReading> ReadDeclaredMeterAsync(TestSpecification specification,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_started) throw new InvalidOperationException("DUT must be started before reading.");
        await foreach (var reading in GenerateAsync(specification, _scenario.DutPowerFactor,
                           _scenario.DutEnergyFactor, _scenario.ThrowAfterSample, cancellationToken))
            yield return reading;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken) { _connected = false; return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    internal static async IAsyncEnumerable<MeterReading> GenerateAsync(TestSpecification spec,
        decimal powerFactor, decimal energyFactor, int? throwAfterSample,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sampleCount = Math.Max(1, (int)Math.Ceiling(spec.Duration / spec.SamplingInterval));
        for (var index = 1; index <= sampleCount; index++)
        {
            await Task.Delay(spec.SamplingInterval, cancellationToken);
            if (throwAfterSample is not null && index > throwAfterSample)
                throw new IOException("Simulated communication interruption.");
            var elapsedHours = (decimal)(spec.SamplingInterval.TotalHours * index);
            yield return new MeterReading(DateTimeOffset.UtcNow, index,
                spec.RequestedPowerKw * powerFactor,
                spec.RequestedPowerKw * elapsedHours * energyFactor);
        }
    }
}

public sealed class MockTestSystemHardwareConnector : ITestSystemHardwareConnector
{
    private readonly MockTestScenario _scenario;
    private bool _connected;
    private bool _monitoring;
    public string Name => "Mock TS HW";

    public MockTestSystemHardwareConnector(MockTestScenario scenario) => _scenario = scenario;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_scenario.FailOnConnect) throw new TimeoutException("Simulated TS HW connection timeout.");
        _connected = true;
        return Task.CompletedTask;
    }

    public Task StartMonitoringAsync(ChargeStartCommand command, CancellationToken cancellationToken)
    {
        if (!_connected) throw new InvalidOperationException("TS HW must be connected before monitoring.");
        _monitoring = true;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<MeterReading> ReadReferenceMeterAsync(TestSpecification specification,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_monitoring) throw new InvalidOperationException("TS HW must be started before reading.");
        await foreach (var reading in MockDutConnector.GenerateAsync(specification,
                           _scenario.HardwarePowerFactor, _scenario.HardwareEnergyFactor,
                           _scenario.ThrowAfterSample, cancellationToken))
            yield return reading;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken) { _connected = false; return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
