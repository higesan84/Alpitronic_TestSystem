using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Tests;

public sealed class ResultEvaluatorTests
{
    private static readonly TestSpecification Spec = new(
        "FINAL", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 50m, 2m, 2m);

    [Fact]
    public void Evaluate_returns_pass_when_each_difference_is_within_tolerance()
    {
        var readings = new FinalReadings(
            new MeterReading(DateTimeOffset.UtcNow, 3, 50.5m, 10.1m),
            new MeterReading(DateTimeOffset.UtcNow, 3, 50m, 10m));

        var result = ResultEvaluator.Evaluate("s1", "SN1", Spec, readings);

        Assert.Equal(TestOutcome.Passed, result.Outcome);
        Assert.All(result.Checks, check => Assert.True(check.Passed));
        Assert.All(result.Checks, check => Assert.Equal(3, check.SampleSequence));
    }

    [Fact]
    public void Evaluate_returns_fail_when_energy_exceeds_tolerance()
    {
        var readings = new FinalReadings(
            new MeterReading(DateTimeOffset.UtcNow, 3, 50m, 10.5m),
            new MeterReading(DateTimeOffset.UtcNow, 3, 50m, 10m));

        var result = ResultEvaluator.Evaluate("s1", "SN1", Spec, readings);

        Assert.Equal(TestOutcome.Failed, result.Outcome);
        Assert.False(result.Checks.Single(x => x.Name == "Energy").Passed);
    }

    [Fact]
    public void Evaluate_returns_fail_when_an_early_sample_exceeds_tolerance_even_if_the_final_sample_passes()
    {
        var readings = new SessionReadings([
            new CorrelatedReadings(
                new MeterReading(DateTimeOffset.UtcNow, 1, 50m, 10.5m),
                new MeterReading(DateTimeOffset.UtcNow, 1, 50m, 10m)),
            new CorrelatedReadings(
                new MeterReading(DateTimeOffset.UtcNow, 2, 50m, 11m),
                new MeterReading(DateTimeOffset.UtcNow, 2, 50m, 11m))
        ]);

        var result = ResultEvaluator.Evaluate("s1", "SN1", Spec, readings);

        Assert.Equal(TestOutcome.Failed, result.Outcome);
        Assert.Equal(4, result.Checks.Count);
        Assert.Equal([1L, 1L, 2L, 2L], result.Checks.Select(check => check.SampleSequence));
        Assert.False(result.Checks.Single(check => check.Name == "Energy" && check.SampleSequence == 1).Passed);
        Assert.True(result.Checks.Single(check => check.Name == "Energy" && check.SampleSequence == 2).Passed);
        Assert.Equal(2, result.FinalReadings!.Dut.Sequence);
    }

    [Fact]
    public void Evaluate_returns_error_for_unaligned_session_sample()
    {
        var readings = new SessionReadings([
            new CorrelatedReadings(
                new MeterReading(DateTimeOffset.UtcNow, 3, 50m, 10m),
                new MeterReading(DateTimeOffset.UtcNow, 4, 50m, 10m))
        ]);

        var result = ResultEvaluator.Evaluate("s1", "SN1", Spec, readings);

        Assert.Equal(TestOutcome.Error, result.Outcome);
        Assert.Empty(result.Checks);
    }

    [Fact]
    public void State_machine_rejects_invalid_transition()
    {
        var machine = new TestSessionStateMachine();
        Assert.Throws<InvalidOperationException>(() => machine.MoveTo(TestSessionState.Monitoring));
    }
}
