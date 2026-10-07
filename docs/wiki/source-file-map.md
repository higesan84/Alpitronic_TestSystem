# Mappa dei file e strategia di test

Questa pagina registra **perché ogni file sorgente o configurazione esiste**. È la checklist per chi modifica il sistema e vuole capire dove si trova una responsabilità prima di aggiungere codice.

## Domain

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.Domain/Measurements.cs` | Stabilisce unità, identità del campione, coppie correlate e serie completa tracciabile. |
| `src/Alpitronic.TestSystem.Domain/TestSpecification.cs` | Rende la specifica una fotografia validabile della prova, riproducibile anche dopo un cambio MES. |
| `src/Alpitronic.TestSystem.Domain/ResultEvaluator.cs` | Isola PASS/FAIL dall’I/O e valuta ogni coppia correlata, così un transitorio non viene nascosto dall'ultimo campione. |
| `src/Alpitronic.TestSystem.Domain/TestSessionStateMachine.cs` | Impedisce di saltare passaggi della prova o di caricare esiti non valutati. |
| `src/Alpitronic.TestSystem.Domain/ConnectorContracts.cs` | Definisce capability stable verso hardware intercambiabile senza esporre il protocollo. |
| `src/Alpitronic.TestSystem.Domain/Alpitronic.TestSystem.Domain.csproj` | Mantiene il Domain senza dipendenze di trasporto o UI. |

## Application

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.Application/ApplicationContracts.cs` | Stabilisce porte e messaggi semantici: il workflow non dipende né da HTTP né da lingua. |
| `src/Alpitronic.TestSystem.Application/ChargingTestOrchestrator.cs` | Centralizza l’ordine fisicamente significativo della sessione e il cleanup affidabile. |
| `src/Alpitronic.TestSystem.Application/ReadingMonitor.cs` | Sincronizza due stream concorrenti, rifiuta sequence duplicate/orfane e restituisce tutte le coppie correlate. |
| `src/Alpitronic.TestSystem.Application/Alpitronic.TestSystem.Application.csproj` | Permette al caso d’uso di dipendere solo da Domain. |

## Infrastructure

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.Infrastructure/ExponentialBackoffRetryPolicy.cs` | Fornisce un esempio esplicito di resilienza, separato dalle regole di business. |
| `src/Alpitronic.TestSystem.Infrastructure/Mes/HttpMesGateway.cs` | Incapsula il client REST (`GET` setup, `PUT` risultato) e il mapping tra contratti esterni e interni. |
| `src/Alpitronic.TestSystem.Infrastructure/Mocks/MockConnectors.cs` | Simula in modo deterministico gli scenari che l’hardware reale renderebbe costosi o rischiosi da riprodurre. |
| `src/Alpitronic.TestSystem.Infrastructure/Mocks/MockConnectorFactory.cs` | Seleziona fake coerenti durante demo e test. |
| `src/Alpitronic.TestSystem.Infrastructure/Mocks/MockMesGateway.cs` | Permette di verificare download/upload senza dipendere da una rete. |
| `src/Alpitronic.TestSystem.Infrastructure/Alpitronic.TestSystem.Infrastructure.csproj` | Raccoglie gli adapter senza farli filtrare negli altri layer. |

## Contratto e mock REST MES

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.Mes.Contracts/MesDtos.cs` | Espone solo il vocabolario JSON versionabile del confine REST, inclusa la sequence di ogni check, separato da Domain e dal server mock. |
| `src/Alpitronic.TestSystem.Mes.Contracts/Alpitronic.TestSystem.Mes.Contracts.csproj` | Consente a client e mock locale di concordare il wire contract senza dipendere l’uno dall’altro. |
| `src/Alpitronic.TestSystem.MesMock.Api/Program.cs` | Dimostra con endpoint Minimal API il protocollo che la TS App consuma, senza simulare un MES produttivo completo. |
| `src/Alpitronic.TestSystem.MesMock.Api/Alpitronic.TestSystem.MesMock.Api.csproj` | Ospita il mock in un processo ASP.NET Core indipendente dalla WinUI. |
| `src/Alpitronic.TestSystem.MesMock.Api/README.md` | Fornisce l’avvio minimo e indirizza al contratto HTTP completo. |

## Localization

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.Localization/TranslationContracts.cs` | Separa servizio, catalogo e persistenza per rendere JSON sostituibile con database. |
| `src/Alpitronic.TestSystem.Localization/JsonTranslationDataProvider.cs` | Offre bootstrap locale, deterministico e offline per le traduzioni. |
| `src/Alpitronic.TestSystem.Localization/TranslationService.cs` | Impone completezza del catalogo e formattazione coerente alla cultura selezionata. |
| `src/Alpitronic.TestSystem.Localization/Alpitronic.TestSystem.Localization.csproj` | Evita che la localizzazione dipenda da WinUI o database concreto. |

## WinUI

| File | Perché esiste |
|---|---|
| `src/Alpitronic.TestSystem.WinUI/App.xaml` | Registra risorse WinUI condivise senza logica di sessione. |
| `src/Alpitronic.TestSystem.WinUI/App.xaml.cs` | È l’unico composition root demo: decide adapter concreti e provider JSON. |
| `src/Alpitronic.TestSystem.WinUI/MainWindow.xaml` | Mantiene la shell separata dal contenuto e da lingua/sessione. |
| `src/Alpitronic.TestSystem.WinUI/MainWindow.xaml.cs` | Aggiorna il titolo perché `Window` non eredita il DataContext della Page. |
| `src/Alpitronic.TestSystem.WinUI/MainPage.xaml` | Esprime la UI attraverso binding, inclusi pulsanti per scenari demo e pannello di dettaglio dell’esito. |
| `src/Alpitronic.TestSystem.WinUI/MainPage.xaml.cs` | Tiene il code-behind vuoto per rendere verificabile il confine MVVM. |
| `src/Alpitronic.TestSystem.WinUI/ViewModels/Commands.cs` | Adatta operazioni Task al contratto `ICommand` e blocca avvii doppi. |
| `src/Alpitronic.TestSystem.WinUI/ViewModels/MainViewModel.cs` | Traduce semantica del core in stato presentabile sul thread UI, conserva l’esito e guida gli scenari demo locali. |
| `src/Alpitronic.TestSystem.WinUI/Assets/translations.json` | Catalogo deployabile separato dal codice, con metadati descrittivi e Unicode verificato. |
| `src/Alpitronic.TestSystem.WinUI/Alpitronic.TestSystem.WinUI.csproj` | Configura Windows App SDK, asset al runtime e dipendenze di layer. |

## Test e configurazione soluzione

| File | Rischio protetto |
|---|---|
| `tests/Alpitronic.TestSystem.Tests/ResultEvaluatorTests.cs` | Falsi PASS/FAIL, transitori non rilevati o coppie non correlate. |
| `tests/Alpitronic.TestSystem.Tests/MockConnectorAndOrchestratorTests.cs` | Violazioni del protocollo simulato, omissione di campioni, upload non atteso o connection failure. |
| `tests/Alpitronic.TestSystem.Tests/Mes/HttpMesGatewayTests.cs` | Regressioni nel metodo HTTP, URI, payload con sequence e gestione degli status code del client REST MES. |
| `tests/Alpitronic.TestSystem.Tests/Localization/TranslationServiceTests.cs` | Catalogo incompleto, regressione Unicode o formattazione culturale errata. |
| `tests/Alpitronic.TestSystem.Tests/Usings.cs` | Mantiene i test leggibili centralizzando il namespace del framework. |
| `tests/Alpitronic.TestSystem.Tests/Alpitronic.TestSystem.Tests.csproj` | Copia l’asset JSON nei test per verificare lo stesso catalogo distribuito all’app. |
| `Alpitronic.TestSystem.sln` | Dà a IDE e CI una vista unica e intenzionale dei confini del repository. |
| `Directory.Build.props` | Uniforma nullable, implicit usings e warning-as-errors per non delegare la qualità a ogni progetto. |
| `.gitignore` | Evita di versionare output, cache IDE e risultati che non sono sorgente riproducibile. |

## Lettura dei test

I test non sono un elenco di esempi felici. Ogni test deve essere intitolato alla decisione/rischio che protegge. Nuove funzionalità devono aggiungere almeno un test per l’invariante che introdurranno; i test di integrazione con un adapter vendor vivranno in un progetto separato e non sostituiranno gli unit test del Domain.
