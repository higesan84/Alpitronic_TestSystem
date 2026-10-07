
namespace Alpitronic.TestSystem.Domain;

/// <summary>
/// Lettura proveniente da un contatore. Il numero di sequenza identifica un punto
/// di acquisizione che può essere correlato con la misura indipendente del banco.
/// </summary>
public sealed record MeterReading(
    DateTimeOffset Timestamp,
    long Sequence,
    decimal PowerKw,
    decimal EnergyKwh)
{
    public void Validate()
    {
        if (Sequence < 0) throw new ArgumentOutOfRangeException(nameof(Sequence));
        if (PowerKw < 0) throw new ArgumentOutOfRangeException(nameof(PowerKw));
        if (EnergyKwh < 0) throw new ArgumentOutOfRangeException(nameof(EnergyKwh));
    }
}

public sealed record ChargeStartCommand(string SessionId, TimeSpan Duration, decimal RequestedPowerKw);

/// <summary>
/// Coppia di misure ottenute nello stesso punto logico della prova. La correlazione
/// per <see cref="MeterReading.Sequence"/> evita confronti tra istanti diversi.
/// </summary>
public sealed record CorrelatedReadings(MeterReading Dut, MeterReading TestSystemHardware);

/// <summary>
/// Serie completa di coppie correlate della sessione. Conservare l'intera sequenza
/// rende un FAIL intermedio tracciabile: l'ultimo campione non può mascherarlo.
/// </summary>
public sealed record SessionReadings(IReadOnlyList<CorrelatedReadings> Samples)
{
    public CorrelatedReadings Final => Samples.Count > 0
        ? Samples[^1]
        : throw new InvalidOperationException("The session contains no correlated readings.");

    public void ValidatePresence()
    {
        if (Samples.Count == 0)
            throw new InvalidOperationException("The session contains no correlated readings.");
    }
}

/// <summary>
/// Compatibilità per i chiamanti che valutano una sola coppia (per esempio test
/// unitari o integrazioni già esistenti). Il workflow della sessione usa invece
/// <see cref="SessionReadings"/> per valutare tutte le acquisizioni.
/// </summary>
public sealed record FinalReadings(MeterReading Dut, MeterReading TestSystemHardware);

public enum TestOutcome { Passed, Failed, Error, Cancelled }

/// <summary>
/// Esito di una grandezza in un campione specifico. La sequenza è parte del dato
/// di audit: senza di essa più controlli Power/Energy non sarebbero distinguibili.
/// </summary>
public sealed record CheckResult(
    string Name,
    decimal DeclaredValue,
    decimal MeasuredValue,
    decimal DifferencePercent,
    decimal TolerancePercent,
    bool Passed,
    long SampleSequence);

public sealed record SessionResult(
    string SessionId,
    string SerialNumber,
    TestOutcome Outcome,
    IReadOnlyList<CheckResult> Checks,
    CorrelatedReadings? FinalReadings,
    string? FailureReason = null);
