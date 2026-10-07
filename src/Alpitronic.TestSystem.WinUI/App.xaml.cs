using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Alpitronic.TestSystem.Application;
using Alpitronic.TestSystem.Domain;
using Alpitronic.TestSystem.Infrastructure;
using Alpitronic.TestSystem.Infrastructure.Mes;
using Alpitronic.TestSystem.Infrastructure.Mocks;
using Alpitronic.TestSystem.Localization;
using Alpitronic.TestSystem.WinUI.ViewModels;

namespace Alpitronic.TestSystem.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private MainWindow? _window;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var specification = new TestSpecification(
            ProcedureCode: "DC_CHARGE_FINAL",
            Duration: TimeSpan.FromSeconds(10),
            SamplingInterval: TimeSpan.FromSeconds(1),
            RequestedPowerKw: 50m,
            PowerTolerancePercent: 2m,
            EnergyTolerancePercent: 2m);
        var setup = new TestSessionSetup(
            specification,
            new ConnectorSelection("Demo-DUT", TransportKind.Can, new Dictionary<string, string>()),
            new ConnectorSelection("Demo-Load", TransportKind.ModbusTcp, new Dictionary<string, string>()));

        IChargingTestOrchestrator CreateOrchestrator(DemoScenario scenario)
        {
            IMesGateway mes = scenario == DemoScenario.Normal
                ? CreateMesGateway(setup)
                : new MockMesGateway(setup, scenario != DemoScenario.MesTestNotFound);

            var mockScenario = scenario == DemoScenario.DivergentReadings
                ? new MockTestScenario(DutEnergyFactor: 1.05m)
                : new MockTestScenario();

            return new ChargingTestOrchestrator(
                mes,
                new MockConnectorFactory(mockScenario),
                new ExponentialBackoffRetryPolicy(maxAttempts: 1),
                new InMemorySessionLogger());
        }

        ITranslationDataProvider translationProvider = new JsonTranslationDataProvider(
            Path.Combine(AppContext.BaseDirectory, "Assets", "translations.json"));
        ITranslationService translations = new TranslationService(translationProvider);
        await translations.InitializeAsync(CancellationToken.None);

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("WinUI dispatcher is unavailable.");
        var viewModel = new MainViewModel(CreateOrchestrator, dispatcherQueue, translations);
        var page = new MainPage
        {
            DataContext = viewModel
        };

        _window = new MainWindow { Content = page };
        _window.BindViewModel(viewModel);
        _window.Activate();
    }

    private static IMesGateway CreateMesGateway(TestSessionSetup mockSetup)
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("TESTSYSTEM_MES_BASE_URL");
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            return new MockMesGateway(mockSetup);

        if (!baseUri.AbsoluteUri.EndsWith('/'))
            baseUri = new Uri(baseUri.AbsoluteUri + "/", UriKind.Absolute);

        return new HttpMesGateway(new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(10)
        });
    }
}
