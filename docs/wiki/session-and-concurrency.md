# Sessione, concorrenza e cancellazione

## Perché la sessione è un caso d’uso asincrono

DUT, TS HW e MES hanno latenze e failure mode indipendenti. Bloccare il thread UI o attendere prima tutte le letture renderebbe l’operatore cieco durante il test. `ChargingTestOrchestrator` coordina quindi operazioni cancellabili e pubblica `SessionUpdate` progressivi; il ViewModel aggiorna il dispatcher WinUI senza far filtrare tipi UI nel core.

La scelta usa `IAsyncEnumerable<MeterReading>` perché una lettura è una sequenza potenzialmente lunga, non un valore singolo. Microsoft descrive gli async stream come modello naturale per fonti che generano elementi in modo asincrono e che possono essere consumate progressivamente: vedi [Async streams](references.md#async-stream-e-cancellazione).

## Perché esiste `ReadingMonitor`

`ReadingMonitor` è un punto di sincronizzazione intenzionale:

1. avvia due producer indipendenti (DUT e TS HW);
2. porta entrambi gli stream in un `Channel` con un solo consumer;
3. pubblica lo snapshot live più recente senza scegliere una fonte come “padrona”;
4. indicizza **tutte** le letture per `Sequence`, rifiutando una sequenza duplicata;
5. attende il completamento di entrambi gli stream e rifiuta sequence presenti in una sola fonte;
6. restituisce l'intera serie di coppie correlate alla regola di valutazione.

Il `Channel` evita che task concorrenti modifichino direttamente lo stato della UI o una collezione condivisa. Secondo la documentazione .NET, un `Channel` è proprio una struttura producer/consumer asincrona e FIFO; il completamento deve avvenire dopo che **tutti** i producer sono finiti: vedi [Channels](references.md#channels).

### Perché il canale è `Unbounded` nello skeleton

La demo ha una durata finita, un campionamento basso e un consumer immediato; un canale illimitato rende più chiaro il flusso didattico. **Non è automaticamente una scelta di produzione.** Se il dispositivo può generare più rapidamente della UI o della persistenza, usare un `BoundedChannel` con capacità e `FullMode` documentati. La decisione deve esplicitare se attendere (non perdere audit), scartare campioni (solo grafico), o degradare la frequenza.

## Correlazione e valutazione dell'intera sessione

La demo richiede `Dut.Sequence == TestSystemHardware.Sequence` **per ogni coppia** e pretende che i due stream abbiano lo stesso insieme di sequence. `ResultEvaluator` controlla Power ed Energy per ogni coppia; basta un campione oltre tolleranza per rendere l'intera sessione `Failed`, anche se il valore finale rientra nella soglia. Questo impedisce un falso PASS che nasconda un transitorio.

Il `SessionResult` conserva l'ultima coppia per compatibilità con la UI live, mentre ogni `CheckResult` porta anche `SampleSequence` ed è quindi auditabile nel payload MES.

In una integrazione reale, scegliere **una** politica scritta e testata:

- identificativo di acquisizione comune;
- barriera di fine-test inviata a entrambi gli endpoint;
- timestamp normalizzati con massimo skew definito;
- esito `Inconclusive/Error` quando nessuna correlazione è dimostrabile.

Il criterio non è un dettaglio di UI: determina se un risultato è tracciabile.

## Cancellazione cooperativa

L’operatore possiede la `CancellationTokenSource`; l’orchestratore e tutti gli adapter ricevono il token. La cancellazione non forza un thread ad arrestarsi: ogni livello deve osservare il token e terminare in modo ordinato. Questa è la semantica raccomandata da .NET: vedi [Cancellazione cooperativa](references.md#async-stream-e-cancellazione).

La `CancellationTokenSource` viene sempre disposta dal ViewModel al termine della sessione. Non è un dettaglio cosmetico: la documentazione .NET richiede `Dispose` quando non serve più, perché può detenere risorse.

## Stati e UI

La state machine del Domain impedisce salti positivi non ammessi. `Fail()` e `Cancel()` sono transizioni terminali consentite da ogni stato: nel mondo fisico un timeout o una scelta operatore può accadere in qualunque momento. La UI mostra lo stato, ma non decide quale transizione sia valida.

Consultare anche [ciclo di vita della sessione](../diagrams/session-lifecycle.png).
