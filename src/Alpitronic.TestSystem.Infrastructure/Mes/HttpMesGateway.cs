using System.Net.Http.Json;
using Alpitronic.TestSystem.Application;
using Alpitronic.TestSystem.Domain;
using Alpitronic.TestSystem.Mes.Contracts;

namespace Alpitronic.TestSystem.Infrastructure.Mes;


/// <summary>
/// Adapter HTTP. Endpoint, autenticazione e correlazione vengono iniettati
/// tramite HttpClient configurato nel composition root, non nel ViewModel.
/// </summary>
public sealed class HttpMesGateway : IMesGateway
{
    private readonly HttpClient _http;
    public HttpMesGateway(HttpClient http) => _http = http;

    public async Task<TestSessionSetup> DownloadSetupAsync(string serialNumber, CancellationToken cancellationToken)
    {
        var dto = await _http.GetFromJsonAsync<MesSessionSetupDto>(
            $"api/test-sessions/{Uri.EscapeDataString(serialNumber)}/setup", cancellationToken)
            ?? throw new InvalidDataException("MES returned an empty setup.");
        return MesMapper.ToApplication(dto);
    }

    public async Task UploadResultAsync(SessionResult result, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result.SessionId);

        using var response = await _http.PutAsJsonAsync(
            $"api/test-sessions/{Uri.EscapeDataString(result.SessionId)}/result",
            MesMapper.ToDto(result), cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public static class MesMapper
{
    public static TestSessionSetup ToApplication(MesSessionSetupDto dto) => new(
        new TestSpecification(dto.ProcedureCode, TimeSpan.FromSeconds(dto.DurationSeconds),
            TimeSpan.FromMilliseconds(dto.SamplingIntervalMilliseconds), dto.RequestedPowerKw,
            dto.PowerTolerancePercent, dto.EnergyTolerancePercent),
        ToSelection(dto.Dut), ToSelection(dto.TestSystemHardware));

    public static MesSessionResultDto ToDto(SessionResult result) => new(
        result.SessionId, result.SerialNumber, result.Outcome.ToString(),
        result.Checks.Select(x => new MesCheckResultDto(x.Name, x.DeclaredValue, x.MeasuredValue,
            x.DifferencePercent, x.TolerancePercent, x.Passed, x.SampleSequence)).ToArray(), result.FailureReason);

    private static ConnectorSelection ToSelection(MesConnectorDto dto)
    {
        if (!Enum.TryParse<TransportKind>(dto.Transport, ignoreCase: true, out var transport))
            throw new InvalidDataException($"Unsupported transport '{dto.Transport}'.");
        return new ConnectorSelection(dto.DeviceModel, transport, dto.Settings);
    }
}
