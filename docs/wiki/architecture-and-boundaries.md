# Architettura e confini

## Principio guida

Il sistema viene diviso per **ragione di cambiamento**, non per tecnologia. Le regole di test cambiano quando cambiano le specifiche; il protocollo cambia quando cambia un vendor; la UI cambia quando cambia l’esperienza operatore. Se questi motivi convivessero nello stesso progetto, una modifica locale avrebbe effetti imprevedibili.

```text
WinUI (presentazione) → Application (caso d'uso) → Domain (invarianti)
                          ↓
                     Infrastructure (adapter esterni)
```

La freccia indica una dipendenza di compilazione/contratto. Il Domain non conosce JSON, HTTP, CAN, WinUI o file; questo permette di provarne le regole rapidamente e senza hardware.

## Responsabilità dei progetti

| Progetto | Possiede | Non deve possedere | Perché |
|---|---|---|---|
| `Alpitronic.TestSystem.Domain` | misure, specifiche, stati, esito, capability dei dispositivi | UI, DTO, HTTP, retry | Mantiene le invarianti ripetibili e verificabili. |
| `Alpitronic.TestSystem.Application` | sequenza della sessione, porte, messaggi semantici, monitoraggio | stringhe UI, dettagli del vendor | Coordina il caso d’uso senza decidere il trasporto né la lingua. |
| `Alpitronic.TestSystem.Infrastructure` | HTTP MES, JSON remoto, mock, retry concreto | regole PASS/FAIL | Incapsula i dettagli volatili e gli effetti I/O. |
| `Alpitronic.TestSystem.Localization` | catalogo, provider e formattazione culturale | stato della sessione | Consente a UI e storage delle traduzioni di evolvere indipendentemente. |
| `Alpitronic.TestSystem.WinUI` | binding, dispatcher, selezione lingua, composition root | logica di misura/risultato | Protegge la responsività e rende il core eseguibile fuori da Windows. |

## Invarianti chiave

1. **Il meter del TS HW è il riferimento**: il DUT dichiara, il banco misura indipendentemente.
2. **Ogni punto della curva richiede campioni correlati**: i due stream devono condividere tutte le sequence e un solo check oltre soglia rende la sessione FAIL.
3. **Ogni sessione segue transizioni esplicite**: non si può caricare un risultato prima di averlo valutato.
4. **Un adapter espone capacità, non un protocollo generico**: il caso d’uso chiede “avvia ricarica” o “leggi meter”, non “invia frame CAN”.
5. **La traduzione avviene dopo il core**: codici semantici proteggono tracciabilità e test dall’ambiguità delle frasi umane.

## Trade-off deliberati

- L’esempio usa una `IConnectorFactory` anziché una libreria DI per non nascondere il meccanismo di selezione runtime. In produzione la factory sarà costruita tramite DI e registry di adapter.
- L’UI usa un ViewModel esplicito, non un event aggregator globale: il flusso sessione → update → ViewModel resta leggibile, cancellabile e testabile.
- I DTO MES non sono riusati come model di dominio: riusarli sarebbe più veloce oggi, ma farebbe dipendere PASS/FAIL dalla forma dell’API di domani.

Per la visualizzazione, consultare [flowchart architetturale](../diagrams/architecture-overview.png) e [decisioni architetturali](../architecture-decisions.md).
