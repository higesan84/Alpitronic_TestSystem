using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Alpitronic.TestSystem.Domain;
using Alpitronic.TestSystem.Infrastructure.Mes;
using Alpitronic.TestSystem.Mes.Contracts;

namespace Alpitronic.TestSystem.Tests.Mes;

public sealed class HttpMesGatewayTests
{
    [Fact]
    public async Task Download_setup_uses_GET_on_the_setup_resource_and_maps_the_response()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, new MesSessionSetupDto(
            "DC_CHARGE_FINAL", 10, 1_000, 50m, 2m, 2m,
            new MesConnectorDto("Demo-DUT", "Can", new Dictionary<string, string>()),
            new MesConnectorDto("Demo-Load", "ModbusTcp", new Dictionary<string, string>()))));
        var gateway = CreateGateway(handler);

        var setup = await gateway.DownloadSetupAsync("SN-42", CancellationToken.None);

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/api/test-sessions/SN-42/setup", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("DC_CHARGE_FINAL", setup.Specification.ProcedureCode);
        Assert.Equal(TransportKind.Can, setup.DutConnector.Transport);
        Assert.Equal(TransportKind.ModbusTcp, setup.TestSystemHardwareConnector.Transport);
    }

    [Fact]
    public async Task Upload_result_uses_idempotent_PUT_on_the_session_result_resource()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var gateway = CreateGateway(handler);
        var result = new SessionResult(
            SessionId: "session-42",
            SerialNumber: "SN-42",
            Outcome: TestOutcome.Passed,
            Checks: [new CheckResult("Power", 50m, 50m, 0m, 2m, true, 7)],
            FinalReadings: null);

        await gateway.UploadResultAsync(result, CancellationToken.None);

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal("/api/test-sessions/session-42/result", handler.Request.RequestUri!.AbsolutePath);
        Assert.NotNull(handler.RequestBody);
        var payload = handler.RequestBody!.RootElement;
        Assert.Equal("session-42", payload.GetProperty("sessionId").GetString());
        Assert.Equal("SN-42", payload.GetProperty("serialNumber").GetString());
        Assert.Equal("Passed", payload.GetProperty("outcome").GetString());
        Assert.Equal(7, payload.GetProperty("checks")[0].GetProperty("sampleSequence").GetInt64());
    }

    [Fact]
    public async Task Upload_result_propagates_an_unsuccessful_HTTP_status()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict));
        var gateway = CreateGateway(handler);
        var result = new SessionResult("session-42", "SN-42", TestOutcome.Passed, [], null);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            gateway.UploadResultAsync(result, CancellationToken.None));
    }

    private static HttpMesGateway CreateGateway(RecordingHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://mes.example/") });

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T body) =>
        new(statusCode) { Content = JsonContent.Create(body) };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public JsonDocument? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Content is not null)
            {
                await using var stream = await request.Content.ReadAsStreamAsync(cancellationToken);
                RequestBody = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }

            return responseFactory(request);
        }
    }
}
