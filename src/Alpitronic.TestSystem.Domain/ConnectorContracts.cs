
namespace Alpitronic.TestSystem.Domain;


/// <summary>Contratto tecnico minimo condiviso da ogni adapter.</summary>
public interface IConnector : IAsyncDisposable
{
    string Name { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}


public interface IDutConnector : IConnector
{
    Task StartChargingAsync(ChargeStartCommand command, CancellationToken cancellationToken);
    IAsyncEnumerable<MeterReading> ReadDeclaredMeterAsync(TestSpecification specification, CancellationToken cancellationToken);
}

public interface ITestSystemHardwareConnector : IConnector
{
    Task StartMonitoringAsync(ChargeStartCommand command, CancellationToken cancellationToken);
    IAsyncEnumerable<MeterReading> ReadReferenceMeterAsync(TestSpecification specification, CancellationToken cancellationToken);
}

public enum TransportKind { Can, ModbusTcp, Mqtt }

/// <summary>
/// Selezione del connettore al runtime
/// </summary>
/// <param name="DeviceModel"></param>
/// <param name="Transport"></param>
/// <param name="Settings"></param>
public sealed record ConnectorSelection(string DeviceModel, TransportKind Transport, IReadOnlyDictionary<string, string> Settings);

public interface IConnectorFactory
{
    IDutConnector CreateDut(ConnectorSelection selection);
    ITestSystemHardwareConnector CreateTestSystemHardware(ConnectorSelection selection);
}
