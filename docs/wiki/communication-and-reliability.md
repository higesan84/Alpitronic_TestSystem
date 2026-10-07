# Comunicazioni e affidabilità

## Adapter guidati dalle capacità

`IDutConnector` e `ITestSystemHardwareConnector` non sono una complicazione astratta: rappresentano le due responsabilità fisiche del test. Il DUT riceve start e dichiara misure; il TS HW viene armato e misura il riferimento. Entrambi condividono solo il ciclo di vita `IConnector` (`Connect`, `Disconnect`, `DisposeAsync`).

Questo evita una mega-interfaccia in cui un meter Modbus finge di saper avviare una ricarica o un DUT CAN finge di essere un meter indipendente. Il caso d’uso resta aperto a nuovi adapter senza conoscere frame, registri o topic.

## Perché MES usa DTO e mapper

Il payload HTTP è un contratto di integrazione, non una regola di dominio. `Mes*Dto`, nel progetto `Alpitronic.TestSystem.Mes.Contracts`, conserva nomi, tipi e granularità del JSON remoto; `MesMapper` traduce in tipi con significato locale (`TestSpecification`, `ConnectorSelection`, `SessionResult`).

Il mapper è un costo intenzionale: è il punto in cui fallire subito per un trasporto sconosciuto, un setup incompleto o una versione API incompatibile. Senza questo confine, un cambio nel MES si propagherebbe silenziosamente a tutta l’app.

`HttpClient` viene iniettato perché l’ambiente di avvio deve configurare base URL, autenticazione, timeout, retry handler e correlation ID. Il ViewModel non deve conoscere endpoint né credenziali.

### REST dimostrabile, non MES fittiziamente completo

`HttpMesGateway` è un client REST concreto: fa `GET /api/test-sessions/{serialNumber}/setup` e `PUT /api/test-sessions/{sessionId}/result`. Il secondo endpoint è `PUT`, non `POST`, perché la risorsa ha già un identificatore assegnato (`SessionId`) e il retry del medesimo risultato deve essere idempotente. `Alpitronic.TestSystem.MesMock.Api` è una Minimal API separata che espone gli stessi endpoint per demo e verifica manuale. Il mock non implementa autenticazione, database, workflow produttivo o MES reale; il suo scopo è rendere osservabile il confine HTTP richiesto dal brief.

Il [contratto REST MES](../mes-rest-api.md) descrive endpoint, payload e codici di stato.

## Retry: cosa si può ripetere

`ExponentialBackoffRetryPolicy` è didattica e quindi generica; la wiki impedisce di interpretarla come policy completa di produzione:

| Operazione | Retry? | Perché |
|---|---|---|
| Download setup MES | Sì, per errori transitori classificati | È una lettura senza effetto fisico. |
| Connect | Sì, entro un budget | Può recuperare una disconnessione temporanea. |
| Start charge | Solo se idempotente o verificabile | Reinvio cieco può avviare due volte un’azione fisica. |
| Stream letture | Solo se il device supporta ripresa e correlazione | Un buco nei campioni può invalidare l’esito. |
| Upload risultato | Non perdere mai il payload; usare outbox | Il test può essere valido anche se MES è momentaneamente indisponibile. |

`OperationCanceledException` non viene mai ritentata: è una richiesta intenzionale dell’operatore, non un guasto.

## Outbox per la tracciabilità

Per upload MES, la soluzione di produzione non è “riprovare qualche volta e poi fallire”. È salvare risultato e idempotency key in una outbox locale atomica, mostrare `PendingUpload`, e lasciare a un worker il retry. Il [Transactional Outbox pattern](references.md#outbox-e-consegna-affidabile) documenta il motivo: separare scrittura locale e invio remoto può perdere eventi tra le due operazioni; una registrazione atomica consente consegna eventuale e controllabile.

Vedere anche il [flowchart errori e recovery](../diagrams/failure-recovery.png) e [roadmap adapter](../connector-adapters.md).
