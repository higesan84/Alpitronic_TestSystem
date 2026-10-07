
using Alpitronic.TestSystem.Application;
using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Infrastructure.Mocks;

/// <summary>Fake in memoria per demo, sviluppo UI e test di orchestrazione.</summary>
public sealed class MockMesGateway : IMesGateway
{
    private readonly TestSessionSetup _setup;
    private readonly bool _testExists;

    public List<SessionResult> UploadedResults { get; } = new();

    public MockMesGateway(TestSessionSetup setup, bool testExists = true)
    {
        _setup = setup;
        _testExists = testExists;
    }
    public Task<TestSessionSetup> DownloadSetupAsync(string serialNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("Missing serial.");
        if (!_testExists) throw new KeyNotFoundException($"MES_TEST_NOT_FOUND: {serialNumber}");

        return Task.FromResult(_setup);
    }

    public Task UploadResultAsync(SessionResult result, CancellationToken cancellationToken)
    {
        UploadedResults.Add(result);
        return Task.CompletedTask;
    }
}
