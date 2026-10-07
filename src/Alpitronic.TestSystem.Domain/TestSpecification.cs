
namespace Alpitronic.TestSystem.Domain;

/// <summary>
/// Specifica del test
/// </summary>
public sealed record TestSpecification(
    string ProcedureCode,
    TimeSpan Duration,
    TimeSpan SamplingInterval,
    decimal RequestedPowerKw,
    decimal PowerTolerancePercent,
    decimal EnergyTolerancePercent)
{
    /// <summary>
    /// Verifica che il test abbia senso
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Validate()
    {
           if (string.IsNullOrWhiteSpace(ProcedureCode))
            throw new ArgumentException("Procedure code is required.", nameof(ProcedureCode));
        if (Duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Duration));
        if (SamplingInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(SamplingInterval));
        if (RequestedPowerKw <= 0) throw new ArgumentOutOfRangeException(nameof(RequestedPowerKw));
        if (PowerTolerancePercent < 0 || EnergyTolerancePercent < 0)
            throw new ArgumentOutOfRangeException("Tolerances cannot be negative.");
    }
}
