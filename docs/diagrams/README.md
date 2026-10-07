# Flowchart del Test System App

Questa cartella contiene flowchart in **Mermaid** (`.mmd`) e le relative versioni PNG renderizzate. Le sorgenti `.mmd` sono la fonte di verità: modificare quelle e rigenerare l'immagine.

| Diagramma | Contenuto | Sorgente Mermaid | PNG renderizzato |
|---|---|---|---|
| Architettura a strati | Confini tra WinUI, Application, Domain, Infrastructure, contratto REST MES, MES/mock, DUT e TS HW | [architecture-overview.mmd](architecture-overview.mmd) | [architecture-overview.png](architecture-overview.png) |
| Ciclo di vita sessione | Dal seriale operatore a setup, monitoraggio, valutazione e upload | [session-lifecycle.mmd](session-lifecycle.mmd) | [session-lifecycle.png](session-lifecycle.png) |
| Resilienza e recovery | Retry sicuro, connessioni, interruzioni di lettura e outbox MES | [failure-recovery.mmd](failure-recovery.mmd) | [failure-recovery.png](failure-recovery.png) |
| Provider localizzazione | Separazione UI → servizio → `IDataProvider` → JSON/database | [localization-provider-flow.mmd](localization-provider-flow.mmd) | [localization-provider-flow.png](localization-provider-flow.png) |

Per rigenerare dopo una modifica:

```bash
manus-render-diagram docs/diagrams/architecture-overview.mmd docs/diagrams/architecture-overview.png
manus-render-diagram docs/diagrams/session-lifecycle.mmd docs/diagrams/session-lifecycle.png
manus-render-diagram docs/diagrams/failure-recovery.mmd docs/diagrams/failure-recovery.png
manus-render-diagram docs/diagrams/localization-provider-flow.mmd docs/diagrams/localization-provider-flow.png
```
