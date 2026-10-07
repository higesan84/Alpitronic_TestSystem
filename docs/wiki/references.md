# Riferimenti tecnici ufficiali

Le fonti qui sotto non sono decorate: ogni link giustifica una decisione che appare nel codice o nella wiki. Ultima consultazione: **7 ottobre 2026**.

## Async stream e cancellazione

- [Tutorial Microsoft: Generate and consume async streams using C# and .NET](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream) — motivazione di `IAsyncEnumerable<T>`, `await foreach`, `WithCancellation` e `IAsyncDisposable`.
- [Microsoft: Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) — modello cooperativo `CancellationTokenSource`/`CancellationToken`, responsabilità del chiamante e `Dispose` della source.

## Channels

- [Microsoft: System.Threading.Channels library](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) — modello producer/consumer, semantica di completion, canali bounded/unbounded e back pressure; base del trade-off esplicitato in `ReadingMonitor`.

## WinUI e Windows App SDK

- [Microsoft: Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/) — ruolo di WinUI 3, resource management e deployment delle app desktop.

## Affidabilità distribuita

- [Microsoft Azure Architecture Center: Transactional Outbox pattern](https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-out-box-cosmos) — motivo per cui risultato locale e richiesta remota devono essere registrati atomicamente prima di un worker di consegna.

## API REST MES

- [Microsoft Azure Architecture Center: Web API design](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design) — risorse basate su sostantivi, semantica di `GET` e `PUT`, idempotenza del `PUT`.
- [Microsoft Learn: Minimal APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) — route group e organizzazione di endpoint Minimal API. Riferimento per il mock REST.
- [Microsoft Learn: IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) — motivazione del typed client che sostituirebbe la configurazione demo diretta di `HttpClient` in produzione.

## Documenti interni

- [Design rationale](../design-rationale.md) — mappatura ai requisiti del case study.
- [Decisioni architetturali](../architecture-decisions.md) — alternative considerate e trade-off.
- [Gestione delle traduzioni](../localization.md) — contratto provider, schema database e rollout.
- [Roadmap adapter](../connector-adapters.md) — confini CAN, Modbus TCP e MQTT.
- [Contratto REST MES](../mes-rest-api.md) — endpoint, payload e avvio del mock API.

> Usare una fonte esterna per convalidare una decisione tecnica, non per sostituire il giudizio sul dominio. L’hardware, il MES e i requisiti di tracciabilità restano il contesto che determina la scelta finale.
