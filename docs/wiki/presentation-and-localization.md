# Presentazione WinUI e localizzazione

## Perché WinUI 3 e MVVM

Il case study richiede WinUI. La View XAML contiene disposizione e binding, il ViewModel possiede lo stato presentabile e i comandi, mentre l’orchestratore rimane indipendente dalla UI. Il [Windows App SDK](references.md#winui-e-windows-app-sdk) include WinUI 3 e gestione risorse; qui usiamo un provider esplicito per rendere visibile la strategia di migrazione JSON → database.

`DispatcherQueue` è usato al confine UI perché le callback dei dispositivi possono arrivare da thread diversi. Aggiornare `ObservableCollection` o proprietà bindate fuori dal thread UI causerebbe errori intermittenti, difficili da riprodurre. Il ViewModel accoda solo la presentazione: non sposta l’orchestrazione nel dispatcher.

## Perché `ICommand` usa `async void`

`ICommand.Execute` ha firma `void`: `AsyncRelayCommand` usa quindi `async void` **solo** per adattarsi a quel contratto, mentre il lavoro vero è un `Task` (`StartAsync`). Il flag `_isExecuting` blocca il doppio click: due sessioni concorrenti sulla stessa postazione renderebbero ambigua proprietà del DUT e del banco. In produzione, aggiungere un boundary esplicito per logging/visualizzazione di ogni eccezione non prevista nel comando.

## Localizzazione provider-based

La UI dipende da `ITranslationService`; il servizio dipende da `ITranslationDataProvider`, che estende il contratto generico `IDataProvider<TranslationCatalog>`. Oggi il provider carica JSON locale; domani potrà leggere SQL, API o cache locale, restituendo lo stesso catalogo.

Questa direzione è essenziale: il ViewModel non deve capire se una frase proviene da file, database o rete. Inoltre, l’Application layer invia `SessionMessageCode`, non testo in inglese/italiano; questo evita che logica di workflow e linguaggio dell’operatore diventino inseparabili.

### Catalogo completo e fail-fast

All’avvio `TranslationService.Validate` verifica che ogni `TranslationKey` esista in ogni lingua. Preferiamo fallire all’avvio rispetto a mostrare metà UI in italiano e metà in tedesco durante una prova. Il JSON include quattro culture (`it-IT`, `en-US`, `de-DE`, `ja-JP`) per testare anche Unicode e formati numerici.

Il file JSON non ammette commenti standard. Per questo il catalogo contiene solo dati di localizzazione; le decisioni di architettura e rollout restano in questa documentazione, senza diventare metadati runtime.

### Cambio lingua durante la sessione

Le righe diagnostiche sono mantenute come dati semantici (timestamp, stato, messaggio), non come testo già formattato. Per questo, al cambio lingua il ViewModel può rigenerare anche lo storico visibile senza alterare l’evento tecnico originario. Il dettaglio grezzo dell’eccezione resta invece invariato: tradurlo artificialmente ridurrebbe l’utilità diagnostica.

Per schema database, cache e rollout, consultare [gestione delle traduzioni](../localization.md) e il [flowchart provider](../diagrams/localization-provider-flow.png).
