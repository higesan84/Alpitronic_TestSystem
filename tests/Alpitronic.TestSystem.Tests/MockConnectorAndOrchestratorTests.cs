using Alpitronic.TestSystem.Application;
using Alpitronic.TestSystem.Domain;
using Alpitronic.TestSystem.Infrastructure;
using Alpitronic.TestSystem.Infrastructure.Mocks;

namespace Alpitronic.TestSystem.Tests;

public sealed class MockConnectorAndOrchestratorTests
{
    private static TestSpecification FastSpec => new("FINAL", TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1), 50m, 2m, 2m);

    [Fact]
    public async Task Mock_dut_requires_a_start_command_before_producing_readings()
    {
        var dut = new MockDutConnector(new MockTestScenario());
        await dut.ConnectAsync(CancellationToken.None);

        async Task EnumerateAsync()
        {
            await foreach (var _ in dut.ReadDeclaredMeterAsync(FastSpec, CancellationToken.None)) { }
        }

        await Assert.ThrowsAsync<InvalidOperationException>(EnumerateAsync);
    }

    [Fact]
    public async Task Orchestrator_uploads_a_passed_result_for_matching_mock_meters()
    {
        var setup = new TestSessionSetup(FastSpec,
            new ConnectorSelection("dut", TransportKind.Can, new Dictionary<string, string>()),
            new ConnectorSelection("hw", TransportKind.ModbusTcp, new Dictionary<string, string>()));
        var mes = new MockMesGateway(setup);
        var sut = new ChargingTestOrchestrator(mes,
            new MockConnectorFactory(new MockTestScenario()),
            new ExponentialBackoffRetryPolicy(1), new InMemorySessionLogger());

        var result = await sut.RunAsync("SN-42", progress: null, CancellationToken.None);

        Assert.Equal(TestOutcome.Passed, result.Outcome);
        Assert.Equal(4, result.Checks.Count);
        Assert.Equal([1L, 1L, 2L, 2L], result.Checks.Select(check => check.SampleSequence));
        Assert.Single(mes.UploadedResults);
        Assert.Equal(result.SessionId, mes.UploadedResults[0].SessionId);
    }

    [Fact]
    public async Task Orchestrator_returns_error_and_does_not_upload_when_connector_cannot_connect()
    {
        var setup = new TestSessionSetup(FastSpec,
            new ConnectorSelection("dut", TransportKind.Can, new Dictionary<string, string>()),
            new ConnectorSelection("hw", TransportKind.ModbusTcp, new Dictionary<string, string>()));
        var mes = new MockMesGateway(setup);
        var sut = new ChargingTestOrchestrator(mes,
            new MockConnectorFactory(new MockTestScenario(FailOnConnect: true)),
            new ExponentialBackoffRetryPolicy(1), new InMemorySessionLogger());

        var result = await sut.RunAsync("SN-42", progress: null, CancellationToken.None);

        Assert.Equal(TestOutcome.Error, result.Outcome);
        Assert.Empty(mes.UploadedResults);
    }

    [Fact]
    public async Task Orchestrator_returns_error_without_upload_when_mes_test_does_not_exist()
    {
        var setup = new TestSessionSetup(
            FastSpec,
            new ConnectorSelection("dut", TransportKind.Can, new Dictionary<string, string>()),
            new ConnectorSelection("hw", TransportKind.ModbusTcp, new Dictionary<string, string>()));

        var mes = new MockMesGateway(setup, testExists: false);

        var sut = new ChargingTestOrchestrator(mes,
                                               new MockConnectorFactory(new MockTestScenario()),
                                               new ExponentialBackoffRetryPolicy(1),
                                               new InMemorySessionLogger());

        var result = await sut.RunAsync("SN-42", progress: null, CancellationToken.None);

        Assert.Equal(TestOutcome.Error, result.Outcome);
        Assert.Equal("SN-42", result.SerialNumber);
        Assert.Empty(result.Checks);
        Assert.Null(result.FinalReadings);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("MES_TEST_NOT_FOUND", result.FailureReason);
        Assert.Empty(mes.UploadedResults);
    }

    [Fact]
    public async Task Orchestrator_uploads_failed_result_when_dut_energy_differs_from_reference()
    {
        var setup = new TestSessionSetup(
            FastSpec,
            new ConnectorSelection("dut", TransportKind.Can, new Dictionary<string, string>()),
            new ConnectorSelection("hw", TransportKind.ModbusTcp, new Dictionary<string, string>()));
        var mes = new MockMesGateway(setup);

        var sut = new ChargingTestOrchestrator(mes,
                                               new MockConnectorFactory(new MockTestScenario(DutEnergyFactor: 1.05m)),
                                               new ExponentialBackoffRetryPolicy(1),
                                               new InMemorySessionLogger());

        var result = await sut.RunAsync("SN-42", progress: null, CancellationToken.None);

        Assert.Equal(TestOutcome.Failed, result.Outcome);
        Assert.Equal("SN-42", result.SerialNumber);
        Assert.NotNull(result.FinalReadings);

        var powerChecks = result.Checks.Where(check => check.Name == "Power").ToArray();
        Assert.Equal(2, powerChecks.Length);
        Assert.All(powerChecks, check => Assert.True(check.Passed));

        var energyChecks = result.Checks.Where(check => check.Name == "Energy").ToArray();
        Assert.Equal(2, energyChecks.Length);
        Assert.All(energyChecks, check => Assert.False(check.Passed));
        Assert.All(energyChecks, check => Assert.Equal(5m, check.DifferencePercent));
        Assert.All(energyChecks, check => Assert.Equal(2m, check.TolerancePercent));
        Assert.All(energyChecks, check => Assert.True(check.DeclaredValue > check.MeasuredValue));

        var uploaded = Assert.Single(mes.UploadedResults);
        Assert.Equal(result, uploaded);
    }

}
