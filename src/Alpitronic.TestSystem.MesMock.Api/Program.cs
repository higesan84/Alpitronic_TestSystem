using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Alpitronic.TestSystem.Mes.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryMesResultStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("MesMockHealth");

var sessions = app.MapGroup("/api/test-sessions").WithTags("Test sessions");

sessions.MapGet("/{serialNumber}/setup", (string serialNumber) =>
{
    if (string.IsNullOrWhiteSpace(serialNumber))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The serial number is required.");
    }

    var setup = new MesSessionSetupDto(
        ProcedureCode: "DC_CHARGE_FINAL",
        DurationSeconds: 10,
        SamplingIntervalMilliseconds: 1_000,
        RequestedPowerKw: 50m,
        PowerTolerancePercent: 2m,
        EnergyTolerancePercent: 2m,
        Dut: new MesConnectorDto("Demo-DUT", "Can", new Dictionary<string, string>()),
        TestSystemHardware: new MesConnectorDto("Demo-Load", "ModbusTcp", new Dictionary<string, string>()));

    return Results.Ok(setup);
})
.WithName("GetTestSessionSetup")
.Produces<MesSessionSetupDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);

sessions.MapPut("/{sessionId}/result", (
    string sessionId,
    MesSessionResultDto result,
    InMemoryMesResultStore store) =>
{
    if (string.IsNullOrWhiteSpace(sessionId) || !string.Equals(sessionId, result.SessionId, StringComparison.Ordinal))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The session ID in the path must match the session ID in the payload.");
    }

    if (string.IsNullOrWhiteSpace(result.SerialNumber) || string.IsNullOrWhiteSpace(result.Outcome))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Serial number and outcome are required.");
    }

    store.Upsert(result);
    return Results.NoContent();
})
.WithName("PutTestSessionResult")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest);

sessions.MapGet("/{sessionId}/result", (string sessionId, InMemoryMesResultStore store) =>
    store.TryGet(sessionId, out var result)
        ? Results.Ok(result)
        : Results.NotFound())
.WithName("GetUploadedTestSessionResult")
.Produces<MesSessionResultDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();
public sealed class InMemoryMesResultStore
{
    private readonly ConcurrentDictionary<string, MesSessionResultDto> _results = new(StringComparer.Ordinal);

    public void Upsert(MesSessionResultDto result) => _results[result.SessionId] = result;

    public bool TryGet(string sessionId, out MesSessionResultDto? result) =>
        _results.TryGetValue(sessionId, out result);
}
