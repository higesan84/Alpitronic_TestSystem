
using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Application;

/// <summary>Porta dell'applicazione verso MES; l'implementazione HTTP vive in Infrastructure.</summary>
public interface IMesGateway
{
    Task<TestSessionSetup> DownloadSetupAsync(string serialNumber, CancellationToken cancellationToken);
    Task UploadResultAsync(SessionResult result, CancellationToken cancellationToken);
}

public sealed record TestSessionSetup(
    TestSpecification Specification,
    ConnectorSelection DutConnector,
    ConnectorSelection TestSystemHardwareConnector);

/// <summary>
/// Codici semanticamente stabili prodotti dal caso d'uso. Il testo visualizzato
/// è responsabilità del layer Presentation e viene risolto da ITranslationService.
/// </summary>
public enum SessionMessageCode
{
    LoadingSetup,
    Connecting,
    Starting,
    Monitoring,
    LiveReadingsUpdated,
    Evaluating,
    UploadingResult,
    Completed,
    CancelledByOperator,
    SessionError
}

public sealed record SessionMessage(SessionMessageCode Code, string? Detail = null);

public sealed record SessionUpdate(
    TestSessionState State,
    SessionMessage Message,
    MeterReading? LatestDutReading = null,
    MeterReading? LatestHardwareReading = null,
    SessionResult? Result = null);

/// <summary>
/// Contratto della procedura di test
/// </summary>
public interface IChargingTestOrchestrator
{
    Task<SessionResult> RunAsync(string serialNumber, IProgress<SessionUpdate>? progress, CancellationToken cancellationToken);
}

public interface IRetryPolicy
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

public interface ISessionLogger
{
    void Info(string message);
    void Error(string message, Exception exception);
}
