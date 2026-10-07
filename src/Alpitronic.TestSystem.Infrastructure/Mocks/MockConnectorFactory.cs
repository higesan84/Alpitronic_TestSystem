
using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Infrastructure.Mocks;

/// <summary>
/// Il composition root sceglie questa factory in demo. Una factory di produzione
/// registrerebbe adapter CAN/Modbus/MQTT in base a modello e trasporto.
/// </summary>
public sealed class MockConnectorFactory : IConnectorFactory
{
    private readonly MockTestScenario _scenario;
    public MockConnectorFactory(MockTestScenario scenario) => _scenario = scenario;

    public IDutConnector CreateDut(ConnectorSelection selection) => new MockDutConnector(_scenario);
    public ITestSystemHardwareConnector CreateTestSystemHardware(ConnectorSelection selection) =>
        new MockTestSystemHardwareConnector(_scenario);
}
