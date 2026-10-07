
namespace Alpitronic.TestSystem.Domain;

/// <summary>
/// Regola pura e facilmente testabile: il riferimento è sempre il meter
/// indipendente del banco (TS HW), non il valore dichiarato dal DUT.
/// </summary>
public static class ResultEvaluator
{
    /// <summary>
    /// Valuta ogni coppia di campioni correlati. Un singolo scostamento oltre soglia
    /// rende la sessione FAIL, anche quando l'ultimo campione rientra in tolleranza.
    /// </summary>
    public static SessionResult Evaluate(
        string sessionId,
        string serialNumber,
        TestSpecification specification,
        SessionReadings readings)
    {
        specification.Validate();
        readings.ValidatePresence();

        var checks = new List<CheckResult>(readings.Samples.Count * 2);
        var sequences = new HashSet<long>();

        foreach (var sample in readings.Samples)
        {
            sample.Dut.Validate();
            sample.TestSystemHardware.Validate();

            if (sample.Dut.Sequence != sample.TestSystemHardware.Sequence)
            {
                return AlignmentError(sessionId, serialNumber, readings.Final,
                    $"DUT and TS HW samples are not aligned at sequence {sample.Dut.Sequence}/{sample.TestSystemHardware.Sequence}.");
            }

            if (!sequences.Add(sample.Dut.Sequence))
            {
                return AlignmentError(sessionId, serialNumber, readings.Final,
                    $"Duplicate correlated sample sequence {sample.Dut.Sequence}.");
            }

            checks.Add(Compare("Power", sample.Dut.PowerKw, sample.TestSystemHardware.PowerKw,
                specification.PowerTolerancePercent, sample.Dut.Sequence));
            checks.Add(Compare("Energy", sample.Dut.EnergyKwh, sample.TestSystemHardware.EnergyKwh,
                specification.EnergyTolerancePercent, sample.Dut.Sequence));
        }

        var outcome = checks.All(x => x.Passed) ? TestOutcome.Passed : TestOutcome.Failed;
        return new SessionResult(sessionId, serialNumber, outcome, checks, readings.Final);
    }

    /// <summary>
    /// Mantiene la valutazione a campione singolo per compatibilità con chiamanti
    /// esterni; viene reindirizzata alla stessa regola usata dalla sessione completa.
    /// </summary>
    public static SessionResult Evaluate(
        string sessionId,
        string serialNumber,
        TestSpecification specification,
        FinalReadings readings) =>
        Evaluate(sessionId, serialNumber, specification,
            new SessionReadings([new CorrelatedReadings(readings.Dut, readings.TestSystemHardware)]));

    private static SessionResult AlignmentError(
        string sessionId,
        string serialNumber,
        CorrelatedReadings finalReadings,
        string reason) =>
        new(sessionId, serialNumber, TestOutcome.Error,
            Array.Empty<CheckResult>(), finalReadings, reason);

    private static CheckResult Compare(
        string name,
        decimal declared,
        decimal measured,
        decimal tolerance,
        long sampleSequence)
    {
        var difference = measured == 0m
            ? (declared == 0m ? 0m : 100m)
            : decimal.Abs(declared - measured) / decimal.Abs(measured) * 100m;
        return new CheckResult(name, declared, measured, difference, tolerance,
            difference <= tolerance, sampleSequence);
    }
}
