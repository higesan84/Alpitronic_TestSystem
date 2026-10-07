# Test System App — scheletro WinUI 3 / MVVM

Uno scheletro deliberatamente piccolo ma **funzionante nella logica centrale** per il case study: esegue una sessione di ricarica simulata, visualizza letture live, valuta la tolleranza e carica il risultato su un MES mock.

> **Conformità al case study:** C#/.NET con UI in **WinUI 3** (Windows App SDK). L'interfaccia richiede Windows; la logica e i test sono portabili.

## Come lanciare

```powershell
# Da Windows con .NET SDK 8 e Visual Studio/Windows App SDK configurati
dotnet test .\Alpitronic.TestSystem.sln
dotnet run --project .\src\Alpitronic.TestSystem.WinUI\Alpitronic.TestSystem.WinUI.csproj
```

La demo esegue una sessione di 10 secondi con un DUT e un banco simulati. Inserire un seriale e premere **Start session**. 


## Struttura

```text
src/
  Alpitronic.TestSystem.Domain/          # Regole pure: misure, tolleranze, stati, contratti
  Alpitronic.TestSystem.Application/     # Caso d'uso: orchestratore, monitor concorrente, porte
  Alpitronic.TestSystem.Infrastructure/  # Mock, client REST MES, mapper, retry e factory
  Alpitronic.TestSystem.Localization/    # IDataProvider, JSON provider e ITranslationService
  Alpitronic.TestSystem.Mes.Contracts/   # DTO JSON del contratto REST MES
  Alpitronic.TestSystem.MesMock.Api/     # Minimal API ASP.NET Core per dimostrare il confine REST
  Alpitronic.TestSystem.WinUI/           # WinUI 3: View XAML, ViewModel MVVM, composition root
tests/Alpitronic.TestSystem.Tests/       # Unit test dominio, mock connector e orchestratore
docs/                         # Rationale, decisioni e roadmap adapter
```

## Flowchart di riferimento

- [Architettura a strati](docs/diagrams/architecture-overview.png) — confini tra WinUI, Application, Domain, Infrastructure e sistemi esterni.
- [Ciclo di vita della sessione](docs/diagrams/session-lifecycle.png) — avvio, acquisizione concorrente, PASS/FAIL e upload.
- [Gestione di errori e recovery](docs/diagrams/failure-recovery.png) — retry sicuro, riconnessione e outbox MES.

Le versioni Mermaid modificabili sono nella cartella [`docs/diagrams`](docs/diagrams/README.md).

## API REST MES dimostrabile

`HttpMesGateway` è un **client REST**: recupera il setup con `GET /api/test-sessions/{serialNumber}/setup` e carica il risultato con `PUT /api/test-sessions/{sessionId}/result`. Il requisito del brief richiede alla Test System App di connettersi al MES via API, non di reimplementare il MES produttivo.

Per rendere il protocollo dimostrabile, la solution contiene `Alpitronic.TestSystem.MesMock.Api`, una Minimal API locale. La descrizione di endpoint, payload, status code e idempotenza è in [contratto REST MES](docs/mes-rest-api.md). La WinUI rimane offline-first con `MockMesGateway`; impostando `TESTSYSTEM_MES_BASE_URL` può invece usare il client HTTP nello scenario normale.

## Wiki interna e commenti decisionali

La [wiki interna](docs/wiki/README.md) raccoglie ragioni, trade-off, invarianti e riferimenti tecnici; resta volutamente separata dal codice. I commenti nel sorgente, quando necessari, spiegano il **perché** locale senza richiamare tag o documenti esterni.

## Flusso di una sessione

1. `MainViewModel` crea `Progress<SessionUpdate>` e inoltra le notifiche sul `DispatcherQueue` WinUI.
2. `ChargingTestOrchestrator` scarica il setup dall'astrazione `IMesGateway`.
3. `IConnectorFactory` restituisce `IDutConnector` e `ITestSystemHardwareConnector` adatti al setup.
4. Il banco viene armato, poi viene inviato `ChargeStartCommand` al DUT.
5. `ReadingMonitor` consuma **in concorrenza** i due `IAsyncEnumerable<MeterReading>` e pubblica snapshot UI senza bloccare l'interfaccia.
6. `ResultEvaluator` confronta Power ed Energy su **tutte** le coppie allineate per sequenza: un solo campione fuori soglia rende la sessione FAIL.
7. L'esito viene caricato via `IMesGateway`; il ViewModel mostra stato, valori, diagnostica e dettaglio dell’esito.

## Decisioni importanti

- **WinUI 3, non WPF.** È il framework richiesto dal brief e rende l'output conforme al deliverable.
- **Procedure nel layer Application.** Il Domain contiene dati, invarianti e calcolo PASS/FAIL; l'orchestrazione ha I/O e non deve vivere nel Model.
- **Contratto REST MES fuori dal Domain.** `MesSessionSetupDto` vive in `Alpitronic.TestSystem.Mes.Contracts`; `MesMapper` lo traduce in `TestSessionSetup`.
- **Scenari demo espliciti.** I bottoni di simulazione per test MES mancante e letture divergenti usano sempre mock locali, anche quando lo scenario normale è configurato verso una API REST.
- **Base `IConnector`, interfacce per ruolo.** Il DUT sa avviare/riportare; il banco sa misurare. Una mega-interfaccia renderebbe gli adapter fragili.
- **Nessun `Task.Result`, `Wait()` o `Thread.Sleep()`.** Ogni confine I/O usa `async`, `CancellationToken`, `IAsyncEnumerable` e `DispatcherQueue` per il salto al thread UI.
- **Conclusione verificabile.** Il risultato non viene emesso se manca una sorgente, se gli stream non condividono tutte le sequence o se una coppia non è correlata.
- **Mock a scenari, non classi per use case.** `MockTestScenario` rende PASS, FAIL, timeout e interruzioni parametrici e riusabili.
- **Localizzazione provider-based.** `ITranslationService` legge tramite `ITranslationDataProvider : IDataProvider<TranslationCatalog>`; oggi JSON, domani database senza cambiare UI o core.

La motivazione completa, incluse le alternative scartate, è in [decisioni architetturali](docs/architecture-decisions.md); il ragionamento rispetto alle aree del case è in [design rationale](docs/design-rationale.md). I dettagli del menu lingua, JSON e migrazione database sono in [gestione delle traduzioni](docs/localization.md).

## Cosa completare per la produzione

- autenticazione, idempotency key e correlation ID per il MES;
- adapter vendor-specific CAN / Modbus TCP / MQTT, con time-out e classifica degli errori;
- persistenza locale affidabile (outbox) per ritentare upload MES dopo una disconnessione;
- policy di retry selettiva: soltanto operazioni idempotenti/transitorie;
- limiti di memoria e persistenza dei campioni, non una lista infinita in RAM;
- audit log strutturato e telemetria con seriale/session ID;
- autorizzazione operatori, gestione configurazione protetta e test di integrazione su banco.
