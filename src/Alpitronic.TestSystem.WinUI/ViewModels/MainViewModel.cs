using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Alpitronic.TestSystem.Application;
using Alpitronic.TestSystem.Domain;
using Alpitronic.TestSystem.Localization;

namespace Alpitronic.TestSystem.WinUI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly Func<DemoScenario, IChargingTestOrchestrator> _orchestratorFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ITranslationService _translations;
    private readonly List<DiagnosticEntry> _diagnosticEntries = [];
    private CancellationTokenSource? _sessionCancellation;
    private SupportedLanguage _selectedLanguage;
    private string _serialNumber = "DEMO-0001";
    private string _appTitle = string.Empty;
    private string _languageLabel = string.Empty;
    private string _serialNumberLabel = string.Empty;
    private string _startSessionButtonLabel = string.Empty;
    private string _cancelButtonLabel = string.Empty;
    private string _dutMeterTitle = string.Empty;
    private string _hardwareMeterTitle = string.Empty;
    private string _diagnosticsTitle = string.Empty;
    private string _state = string.Empty;
    private string _statusMessage = string.Empty;
    private string _outcome = string.Empty;
    private string _dutPower = string.Empty;
    private string _dutEnergy = string.Empty;
    private string _hardwarePower = string.Empty;
    private string _hardwareEnergy = string.Empty;
    private bool _isRunning;
    private TestSessionState _currentState = TestSessionState.Created;
    private SessionMessage? _currentMessage;
    private TestOutcome? _currentOutcome;
    private MeterReading? _latestDutReading;
    private MeterReading? _latestHardwareReading;
    private SessionResult? _currentResult;
    private string _outcomeDetails = string.Empty;
    private string _outcomeDetailsTitle = string.Empty;
    private string _missingMesTestButtonLabel = string.Empty;
    private string _divergentReadingsButtonLabel = string.Empty;

    public MainViewModel(
        Func<DemoScenario, IChargingTestOrchestrator> orchestratorFactory,
        DispatcherQueue dispatcherQueue,
        ITranslationService translations)
    {
        _orchestratorFactory = orchestratorFactory;
        _dispatcherQueue = dispatcherQueue;
        _translations = translations;
        _selectedLanguage = _translations.CurrentLanguage;
        _translations.LanguageChanged += OnLanguageChanged;
        StartCommand = new AsyncRelayCommand(() => StartAsync(DemoScenario.Normal), CanStart);

        SimulateMissingMesTestCommand = new AsyncRelayCommand(() => StartAsync(DemoScenario.MesTestNotFound), CanStart);

        SimulateDivergenceCommand = new AsyncRelayCommand(() => StartAsync(DemoScenario.DivergentReadings), CanStart);
        CancelCommand = new RelayCommand(Cancel, () => IsRunning);
        RefreshLocalizedStrings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<string> Diagnostics { get; } = new();
    public IReadOnlyList<SupportedLanguage> SupportedLanguages => _translations.SupportedLanguages;



    public ICommand SimulateMissingMesTestCommand { get; }
    public ICommand SimulateDivergenceCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }

    public SupportedLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is null || string.Equals(_selectedLanguage.Code, value.Code, StringComparison.OrdinalIgnoreCase)) return;
            _translations.SetLanguage(value.Code);
        }
    }

    public string SerialNumber { get => _serialNumber; set { Set(ref _serialNumber, value); RaiseCommandStates(); } }
    public string AppTitle { get => _appTitle; private set => Set(ref _appTitle, value); }
    public string LanguageLabel { get => _languageLabel; private set => Set(ref _languageLabel, value); }
    public string SerialNumberLabel { get => _serialNumberLabel; private set => Set(ref _serialNumberLabel, value); }
    public string StartSessionButtonLabel { get => _startSessionButtonLabel; private set => Set(ref _startSessionButtonLabel, value); }
    public string CancelButtonLabel { get => _cancelButtonLabel; private set => Set(ref _cancelButtonLabel, value); }
    public string DutMeterTitle { get => _dutMeterTitle; private set => Set(ref _dutMeterTitle, value); }
    public string HardwareMeterTitle { get => _hardwareMeterTitle; private set => Set(ref _hardwareMeterTitle, value); }
    public string DiagnosticsTitle { get => _diagnosticsTitle; private set => Set(ref _diagnosticsTitle, value); }
    public string State { get => _state; private set => Set(ref _state, value); }
    public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public string Outcome { get => _outcome; private set => Set(ref _outcome, value); }
    public string DutPower { get => _dutPower; private set => Set(ref _dutPower, value); }
    public string DutEnergy { get => _dutEnergy; private set => Set(ref _dutEnergy, value); }
    public string HardwarePower { get => _hardwarePower; private set => Set(ref _hardwarePower, value); }
    public string HardwareEnergy { get => _hardwareEnergy; private set => Set(ref _hardwareEnergy, value); }
    public bool IsRunning { get => _isRunning; private set { Set(ref _isRunning, value); RaiseCommandStates(); } }
    public string OutcomeDetails { get => _outcomeDetails; private set => Set(ref _outcomeDetails, value); }
    public string OutcomeDetailsTitle { get => _outcomeDetailsTitle; private set => Set(ref _outcomeDetailsTitle, value); }
    public string MissingMesTestButtonLabel { get => _missingMesTestButtonLabel; private set => Set(ref _missingMesTestButtonLabel, value); }
    public string DivergentReadingsButtonLabel { get => _divergentReadingsButtonLabel; private set => Set(ref _divergentReadingsButtonLabel, value); }



    private bool CanStart() => !IsRunning && !string.IsNullOrWhiteSpace(SerialNumber);

    private async Task StartAsync(DemoScenario scenario)
    {
        IsRunning = true;
        _currentState = TestSessionState.Created;
        _currentMessage = null;
        _currentOutcome = null;
        _currentResult = null;
        _latestDutReading = null;
        _latestHardwareReading = null;
        _diagnosticEntries.Clear();
        RefreshLocalizedStrings();

        var sessionCancellation = new CancellationTokenSource();
        _sessionCancellation = sessionCancellation;

        var progress = new Progress<SessionUpdate>(update => EnqueueOnUi(() => ApplyUpdate(update)));

        try
        {
            var orchestrator = _orchestratorFactory(scenario);

            var result = await orchestrator.RunAsync(SerialNumber, progress, sessionCancellation.Token);
            _currentResult = result;
            _currentOutcome = result.Outcome;
            RefreshLocalizedStrings();
        }
        finally
        {
            sessionCancellation.Dispose();
            if (ReferenceEquals(_sessionCancellation, sessionCancellation))
                _sessionCancellation = null;
            IsRunning = false;
        }
    }

    private void Cancel() => _sessionCancellation?.Cancel();

    private void OnLanguageChanged(object? sender, EventArgs e) => EnqueueOnUi(RefreshLocalizedStrings);

    private void EnqueueOnUi(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess) action();
        else _dispatcherQueue.TryEnqueue(() => action());
    }

    private void ApplyUpdate(SessionUpdate update)
    {
        _currentState = update.State;
        _currentMessage = update.Message;
        _latestDutReading = update.LatestDutReading ?? _latestDutReading;
        _latestHardwareReading = update.LatestHardwareReading ?? _latestHardwareReading;
        if (update.Result is not null)
        {
            _currentResult = update.Result;
            _currentOutcome = update.Result.Outcome;
        }
        _diagnosticEntries.Add(new DiagnosticEntry(DateTimeOffset.Now, update.State, update.Message));

        RefreshLocalizedStrings();
    }

    private void RefreshLocalizedStrings()
    {
        if (!string.Equals(_selectedLanguage.Code, _translations.CurrentLanguage.Code, StringComparison.OrdinalIgnoreCase))
        {
            _selectedLanguage = _translations.CurrentLanguage;
            OnPropertyChanged(nameof(SelectedLanguage));
        }

        AppTitle = Text(TranslationKey.AppTitle);
        LanguageLabel = Text(TranslationKey.LanguageLabel);
        SerialNumberLabel = Text(TranslationKey.SerialNumberLabel);
        StartSessionButtonLabel = Text(TranslationKey.StartSessionButton);
        CancelButtonLabel = Text(TranslationKey.CancelButton);
        DutMeterTitle = Text(TranslationKey.DutMeterTitle);
        HardwareMeterTitle = Text(TranslationKey.HardwareMeterTitle);
        DiagnosticsTitle = Text(TranslationKey.DiagnosticsTitle);
        State = TranslateState(_currentState);
        StatusMessage = _currentMessage is null ? Text(TranslationKey.ReadyStatus) : TranslateMessage(_currentMessage);
        Outcome = _currentOutcome is null
            ? string.Empty
            : Text(TranslationKey.OutcomeFormat, TranslateOutcome(_currentOutcome.Value));
        DutPower = FormatPower(_latestDutReading);
        DutEnergy = FormatEnergy(_latestDutReading);
        HardwarePower = FormatPower(_latestHardwareReading);
        HardwareEnergy = FormatEnergy(_latestHardwareReading);

        Diagnostics.Clear();
        foreach (var entry in _diagnosticEntries)
        {
            Diagnostics.Add(Text(TranslationKey.DiagnosticFormat,
                entry.Timestamp.LocalDateTime, TranslateState(entry.State), TranslateMessage(entry.Message)));
        }
        MissingMesTestButtonLabel = Text(TranslationKey.MissingMesTestButton);

        DivergentReadingsButtonLabel = Text(TranslationKey.DivergentReadingsButton);

        OutcomeDetailsTitle = Text(TranslationKey.OutcomeDetailsTitle);

        OutcomeDetails = FormatOutcomeDetails(_currentResult);
    }

    private string FormatOutcomeDetails(SessionResult? result)
    {
        if (result is null)
            return string.Empty;

        var lines = new List<string>
    {
        Text(TranslationKey.ResultSessionId, result.SessionId),
        Text(TranslationKey.ResultSerialNumber, result.SerialNumber),
        Text(TranslationKey.OutcomeFormat,
            TranslateOutcome(result.Outcome))
    };

        if (result.Checks.Count > 0)
        {
            foreach (var check in result.Checks)
            {
                var checkName = check.Name switch
                {
                    "Power" => Text(TranslationKey.CheckPower),
                    "Energy" => Text(TranslationKey.CheckEnergy),
                    _ => check.Name
                };

                var sampledCheckName = Text(TranslationKey.CheckNameAtSampleFormat,
                    checkName, check.SampleSequence);

                lines.Add(Text(
                    TranslationKey.ResultCheckFormat,
                    sampledCheckName,
                    check.DeclaredValue,
                    check.MeasuredValue,
                    check.DifferencePercent,
                    check.TolerancePercent,
                    check.Passed
                        ? Text(TranslationKey.OutcomePassed)
                        : Text(TranslationKey.OutcomeFailed)));
            }
        }

        if (!string.IsNullOrWhiteSpace(result.FailureReason))
        {
            var reason = result.FailureReason.StartsWith(
                "MES_TEST_NOT_FOUND:",
                StringComparison.Ordinal)
                ? Text(TranslationKey.MesTestNotFoundReason,
                    result.SerialNumber)
                : result.FailureReason;

            lines.Add(Text(TranslationKey.ResultReason, reason));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string FormatPower(MeterReading? reading) => reading is null
        ? Text(TranslationKey.NoReading)
        : Text(TranslationKey.PowerFormat, reading.PowerKw);

    private string FormatEnergy(MeterReading? reading) => reading is null
        ? string.Empty
        : Text(TranslationKey.EnergyFormat, reading.EnergyKwh, reading.Sequence);

    private string TranslateState(TestSessionState state) => state switch
    {
        TestSessionState.Created => Text(TranslationKey.StateCreated),
        TestSessionState.LoadingSetup => Text(TranslationKey.StateLoadingSetup),
        TestSessionState.Connecting => Text(TranslationKey.StateConnecting),
        TestSessionState.Starting => Text(TranslationKey.StateStarting),
        TestSessionState.Monitoring => Text(TranslationKey.StateMonitoring),
        TestSessionState.Evaluating => Text(TranslationKey.StateEvaluating),
        TestSessionState.UploadingResult => Text(TranslationKey.StateUploadingResult),
        TestSessionState.Completed => Text(TranslationKey.StateCompleted),
        TestSessionState.Failed => Text(TranslationKey.StateFailed),
        TestSessionState.Cancelled => Text(TranslationKey.StateCancelled),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private string TranslateMessage(SessionMessage message) => message.Code switch
    {
        SessionMessageCode.LoadingSetup => Text(TranslationKey.MessageLoadingSetup),
        SessionMessageCode.Connecting => Text(TranslationKey.MessageConnecting),
        SessionMessageCode.Starting => Text(TranslationKey.MessageStarting),
        SessionMessageCode.Monitoring => Text(TranslationKey.MessageMonitoring),
        SessionMessageCode.LiveReadingsUpdated => Text(TranslationKey.MessageLiveReadingsUpdated),
        SessionMessageCode.Evaluating => Text(TranslationKey.MessageEvaluating),
        SessionMessageCode.UploadingResult => Text(TranslationKey.MessageUploadingResult),
        SessionMessageCode.Completed => Text(TranslationKey.MessageCompleted),
        SessionMessageCode.CancelledByOperator => Text(TranslationKey.MessageCancelledByOperator),
        SessionMessageCode.SessionError => Text(TranslationKey.MessageSessionError, message.Detail ?? string.Empty),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.Code, null)
    };

    private string TranslateOutcome(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Passed => Text(TranslationKey.OutcomePassed),
        TestOutcome.Failed => Text(TranslationKey.OutcomeFailed),
        TestOutcome.Error => Text(TranslationKey.OutcomeError),
        TestOutcome.Cancelled => Text(TranslationKey.OutcomeCancelled),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };

    private string Text(TranslationKey key, params object?[] values) => _translations.Translate(key, values);

    private void RaiseCommandStates()
    {
        ((AsyncRelayCommand)StartCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SimulateMissingMesTestCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SimulateDivergenceCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record DiagnosticEntry(DateTimeOffset Timestamp, TestSessionState State, SessionMessage Message);
}

public enum DemoScenario
{
    Normal,
    MesTestNotFound,
    DivergentReadings

}
