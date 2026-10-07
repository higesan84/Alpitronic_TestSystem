
namespace Alpitronic.TestSystem.Localization;

/// <summary>
/// Porta generica verso una sorgente dati. Il provider JSON è solo la prima
/// implementazione: un provider SQL/API potrà sostituirlo senza modificare UI o servizio.
/// </summary>
public interface IDataProvider<TData>
{
    Task<TData> LoadAsync(CancellationToken cancellationToken);
}

public interface ITranslationDataProvider : IDataProvider<TranslationCatalog> { }

public sealed record SupportedLanguage(string Code, string CultureName, string DisplayName);

/// <summary>
/// Modello serializzabile, agnostico dal provider. Le chiavi sono nomi di TranslationKey;
/// le risorse sono indicizzate dal codice lingua (per esempio it-IT).
/// </summary>
public sealed class TranslationCatalog
{
    public string DefaultLanguageCode { get; init; } = "it-IT";
    public List<SupportedLanguage> Languages { get; init; } = [];
    public Dictionary<string, Dictionary<string, string>> Resources { get; init; } = [];
}

public enum TranslationKey
{
    AppTitle,
    LanguageLabel,
    SerialNumberLabel,
    StartSessionButton,
    CancelButton,
    DutMeterTitle,
    HardwareMeterTitle,
    DiagnosticsTitle,
    ReadyStatus,
    NoReading,
    PowerFormat,
    EnergyFormat,
    OutcomeFormat,
    DiagnosticFormat,
    StateCreated,
    StateLoadingSetup,
    StateConnecting,
    StateStarting,
    StateMonitoring,
    StateEvaluating,
    StateUploadingResult,
    StateCompleted,
    StateFailed,
    StateCancelled,
    MessageLoadingSetup,
    MessageConnecting,
    MessageStarting,
    MessageMonitoring,
    MessageLiveReadingsUpdated,
    MessageEvaluating,
    MessageUploadingResult,
    MessageCompleted,
    MessageCancelledByOperator,
    MessageSessionError,
    OutcomePassed,
    OutcomeFailed,
    OutcomeError,
    OutcomeCancelled,
    MissingMesTestButton,
    DivergentReadingsButton,
    OutcomeDetailsTitle,
    ResultSessionId,
    ResultSerialNumber,
    ResultCheckFormat,
    CheckNameAtSampleFormat,
    ResultReason,
    MesTestNotFoundReason,
    CheckPower,
    CheckEnergy
}

/// <summary>
/// Servizio lato presentation: offre stringhe già formattate e notifica il cambio lingua.
/// Application e Domain emettono soltanto codici semantici, non frasi localizzate.
/// </summary>
public interface ITranslationService
{
    event EventHandler? LanguageChanged;

    IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }
    SupportedLanguage CurrentLanguage { get; }

    Task InitializeAsync(CancellationToken cancellationToken);
    void SetLanguage(string languageCode);
    string Translate(TranslationKey key, params object?[] arguments);
}
