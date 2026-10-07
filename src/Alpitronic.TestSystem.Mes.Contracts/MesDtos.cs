namespace Alpitronic.TestSystem.Mes.Contracts;

public sealed record MesConnectorDto(
    string DeviceModel,
    string Transport,
    Dictionary<string, string> Settings);

public sealed record MesSessionSetupDto(
    string ProcedureCode,
    int DurationSeconds,
    int SamplingIntervalMilliseconds,
    decimal RequestedPowerKw,
    decimal PowerTolerancePercent,
    decimal EnergyTolerancePercent,
    MesConnectorDto Dut,
    MesConnectorDto TestSystemHardware);

public sealed record MesCheckResultDto(
    string Name,
    decimal DeclaredValue,
    decimal MeasuredValue,
    decimal DifferencePercent,
    decimal TolerancePercent,
    bool Passed,
    long SampleSequence);

public sealed record MesSessionResultDto(
    string SessionId,
    string SerialNumber,
    string Outcome,
    IReadOnlyList<MesCheckResultDto> Checks,
    string? FailureReason);
