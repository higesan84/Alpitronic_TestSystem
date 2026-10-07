# Wiki interna — Test System App

Questa wiki spiega le **intenzioni progettuali** dietro il codice: il rischio mitigato, il trade-off accettato e l'evoluzione prevista. È una documentazione autonoma, separata dai sorgenti, così la navigazione del codice non dipende da commenti-tag o riferimenti esterni.

## Percorso di lettura consigliato

1. [Architettura e confini](architecture-and-boundaries.md) — responsabilità dei progetti e direzione delle dipendenze.
2. [Sessione e concorrenza](session-and-concurrency.md) — orchestrazione, stream, `Channel`, correlazione completa dei campioni, stato e cancellazione.
3. [Comunicazioni e affidabilità](communication-and-reliability.md) — connettori, MES, retry e outbox.
4. [Contratto REST MES](../mes-rest-api.md) — endpoint, payload, status code e mock API eseguibile.
5. [Presentazione e localizzazione](presentation-and-localization.md) — WinUI, dispatcher, MVVM e provider delle traduzioni.
6. [Mappa dei file e test](source-file-map.md) — motivo dell’esistenza di ogni file sorgente/configurazione e strategia test.
7. [Riferimenti esterni](references.md) — fonti ufficiali su cui si basano le scelte.

## Regola di manutenzione

Quando si modifica una decisione trasversale (protocollo, persistenza, resilienza, modello di concorrenza, localizzazione), aggiornare la pagina wiki pertinente e il test che dimostra il comportamento. Nel codice lasciare soltanto commenti brevi che chiariscano un motivo non evidente; non duplicare la sintassi né creare dipendenze dalla struttura della documentazione.

> La wiki non sostituisce l’API documentation: documenta trade-off, invarianti e rischi.