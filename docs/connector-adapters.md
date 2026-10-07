# Adattatori reali: roadmap di implementazione

La demo usa `MockConnectorFactory`. Per portarla verso l'hardware, creare adapter concreti e lasciar invariati `ChargingTestOrchestrator`, dominio e ViewModel.

| Adapter | Responsabilità | Implementazione prevista |
|---|---|---|
| `CanDutConnector` | comando di start e letture dichiarate dal DUT | Incapsulare SDK CAN, mapping frame ↔ `ChargeStartCommand`/`MeterReading`, time-out e reconnect. |
| `ModbusTsHardwareConnector` | letture del contatore del banco | Incapsulare client Modbus TCP, gestire registri/endianess, validazione CRC se applicabile. |
| `Mqtt...Connector` | integrazione eventi/pub-sub | Gestire connessione, topic versionati, QoS e deduplicazione tramite `Sequence`. |

## Regole

1. Un adapter non aggiorna mai la UI e non calcola PASS/FAIL.
2. Esporre esclusivamente contratti `IDutConnector` o `ITestSystemHardwareConnector`.
3. Convertire errori di basso livello in eccezioni classificate (`TimeoutException`, `IOException`, errore non transitorio).
4. Mettere endpoint, credenziali e parametri in configurazione protetta, mai nel DTO loggato.
5. Testare ogni adapter con un fake del client del vendor e test di integrazione su banco separati dai test unitari.

> Il nome corretto dei protocolli nel codice è **CAN**, **Modbus TCP** e **MQTT**. La `TransportKind` è pensata per essere estesa senza cambiare l'orchestratore.
