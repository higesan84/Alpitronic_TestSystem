
using System.Threading.Channels;
using Alpitronic.TestSystem.Domain;

namespace Alpitronic.TestSystem.Application;

internal static class ReadingMonitor
{
    private enum Source { Dut, Hardware }
    private sealed record ReadingEvent(Source Source, MeterReading Reading);

    public static async Task<SessionReadings> MonitorAsync(
        IDutConnector dut,
        ITestSystemHardwareConnector hardware,
        TestSpecification specification,
        Action<MeterReading?, MeterReading?> publish,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ReadingEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var dutPump = PumpAsync(Source.Dut, dut.ReadDeclaredMeterAsync(specification, cancellationToken), channel.Writer, cancellationToken);
        var hardwarePump = PumpAsync(Source.Hardware, hardware.ReadReferenceMeterAsync(specification, cancellationToken), channel.Writer, cancellationToken);
        _ = CompleteWhenBothFinishAsync(channel.Writer, dutPump, hardwarePump);

        var dutBySequence = new Dictionary<long, MeterReading>();
        var hardwareBySequence = new Dictionary<long, MeterReading>();
        MeterReading? latestDut = null;
        MeterReading? latestHardware = null;

        await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
        {
            var target = entry.Source == Source.Dut ? dutBySequence : hardwareBySequence;
            if (!target.TryAdd(entry.Reading.Sequence, entry.Reading))
            {
                throw new InvalidOperationException(
                    $"{entry.Source} emitted duplicate sample sequence {entry.Reading.Sequence}.");
            }

            if (entry.Source == Source.Dut) latestDut = entry.Reading;
            else latestHardware = entry.Reading;
            publish(latestDut, latestHardware);
        }

        await Task.WhenAll(dutPump, hardwarePump);
        return CorrelateAllSamples(dutBySequence, hardwareBySequence);
    }

    private static SessionReadings CorrelateAllSamples(
        IReadOnlyDictionary<long, MeterReading> dutBySequence,
        IReadOnlyDictionary<long, MeterReading> hardwareBySequence)
    {
        if (dutBySequence.Count == 0 || hardwareBySequence.Count == 0)
            throw new InvalidOperationException("Incomplete readings: one source produced no samples.");

        var dutOnly = dutBySequence.Keys.Except(hardwareBySequence.Keys).Order().ToArray();
        var hardwareOnly = hardwareBySequence.Keys.Except(dutBySequence.Keys).Order().ToArray();
        if (dutOnly.Length > 0 || hardwareOnly.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unpaired readings. DUT-only sequences: [{string.Join(',', dutOnly)}]; " +
                $"TS-HW-only sequences: [{string.Join(',', hardwareOnly)}].");
        }

        var samples = dutBySequence
            .OrderBy(pair => pair.Key)
            .Select(pair => new CorrelatedReadings(pair.Value, hardwareBySequence[pair.Key]))
            .ToArray();

        return new SessionReadings(samples);
    }

    private static async Task PumpAsync(
        Source source,
        IAsyncEnumerable<MeterReading> readings,
        ChannelWriter<ReadingEvent> writer,
        CancellationToken cancellationToken)
    {
        await foreach (var reading in readings.WithCancellation(cancellationToken))
        {
            reading.Validate();
            await writer.WriteAsync(new ReadingEvent(source, reading), cancellationToken);
        }
    }

    private static async Task CompleteWhenBothFinishAsync(ChannelWriter<ReadingEvent> writer, Task first, Task second)
    {
        Exception? error = null;
        try { await Task.WhenAll(first, second); }
        catch (Exception ex) { error = ex; }
        writer.TryComplete(error);
    }
}
