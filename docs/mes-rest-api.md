# Contratto REST MES (mock eseguibile)

## Chiarimento sul requisito

Il brief richiede che la **Test System App si connetta al MES via API**; non richiede di implementare il MES reale. `HttpMesGateway` è il client REST della TS App; `Alpitronic.TestSystem.MesMock.Api` rende il contratto eseguibile e dimostrabile senza sostenere di replicare la piattaforma produttiva.

Questa soluzione include:

- `Alpitronic.TestSystem.Mes.Contracts`: i DTO JSON del confine REST;
- `HttpMesGateway`: il client REST usato dalla Test System App;
- `Alpitronic.TestSystem.MesMock.Api`: una Minimal API ASP.NET Core locale, utile per demo e test manuale;
- test che verificano metodo HTTP, URI e payload del client.

Il mock **non è il MES di produzione**: non include autenticazione, autorizzazione, persistenza durevole, anagrafiche prodotto o workflow manifatturiero.

## Risorse HTTP

| Metodo e URI | Scopo | Esito normale | Perché |
|---|---|---:|---|
| `GET /health` | Verificare raggiungibilità del mock | `200` | Distingue un servizio spento da un errore del workflow. |
| `GET /api/test-sessions/{serialNumber}/setup` | Recuperare setup e parametri della prova | `200` + `MesSessionSetupDto` | Il setup è una risorsa associata al seriale. |
| `PUT /api/test-sessions/{sessionId}/result` | Salvare/aggiornare l’esito di una sessione | `204` | Il `SessionId` è già noto al client: `PUT` rende sicuri retry dello stesso payload. |
| `GET /api/test-sessions/{sessionId}/result` | Ispezionare l’upload nel mock | `200` / `404` | Solo diagnostica demo/CI, non è usato dalla TS App. |

Le URI sono basate su **sostantivi** e il verbo viene espresso dal metodo HTTP. L’uso di `PUT` per il risultato sfrutta la sua semantica idempotente: reinviare lo stesso risultato alla stessa risorsa non ne crea un duplicato.

Ogni elemento `checks` del payload contiene `sampleSequence`: il MES può quindi distinguere i controlli Power/Energy delle singole coppie correlate. La Test System App valuta l'intera curva; una sola verifica oltre soglia rende la sessione `Failed`.

## Esecuzione locale

Da Windows o da un terminale con .NET SDK 8:

```powershell
dotnet run --project .\src\Alpitronic.TestSystem.MesMock.Api\Alpitronic.TestSystem.MesMock.Api.csproj --urls http://localhost:5088
```

Verifica di salute:

```powershell
Invoke-RestMethod http://localhost:5088/health
```

Recupero setup:

```powershell
Invoke-RestMethod http://localhost:5088/api/test-sessions/DEMO-0001/setup
```

### Collegamento della WinUI alla API mock

La demo WinUI rimane autonoma e usa di default `MockMesGateway`. Per provare il client REST, avviare prima il mock e impostare la variabile d’ambiente:

```powershell
$env:TESTSYSTEM_MES_BASE_URL = "http://localhost:5088/"
dotnet run --project .\src\Alpitronic.TestSystem.WinUI\Alpitronic.TestSystem.WinUI.csproj
```

All’avvio, `App.xaml.cs` seleziona `HttpMesGateway` solo per lo scenario **normale** se la variabile contiene una URI assoluta `http`/`https` valida; altrimenti conserva il mock in memoria. I bottoni “simula” sono sempre locali e non alterano un MES esterno. In un’app di produzione questa scelta diventerebbe configurazione protetta e un typed client registrato via `IHttpClientFactory`.

## Evoluzione prevista verso il MES reale

| Aspetto | Skeleton/mock | Produzione |
|---|---|---|
| Contratto | DTO C# condivisi nel repository | OpenAPI versionato, client generato o DTO contrattuali versionati. |
| Autenticazione | Assente | OAuth2/client credentials o meccanismo aziendale, gestito da handler. |
| HTTP client | `HttpClient` configurato nel composition root | Typed client tramite `IHttpClientFactory`, timeout e handler centralizzati. |
| Upload | `PUT` idempotente in memoria | Outbox locale durevole, idempotency/correlation ID e retry classificato. |
| Errori | `400`, `404`, `500` framework | `ProblemDetails` versionato, codici business e telemetry. |
| Dati | Setup deterministico, storage RAM | Setup reale, autorizzazione, audit e database MES. |

## Riferimenti

- [REST API design — Microsoft Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)
- [Minimal APIs in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [IHttpClientFactory — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
