using time;

var factory = new TimeFactory();
var instant = factory.ParseInstant("2026-01-05T18:00:00Z");
Console.WriteLine($"Meeting instant: {instant.ToIsoString()}");
Console.WriteLine("Fixed offsets (no daylight-saving rules):");

// Only the wall-clock representation changes; the underlying instant stays the same.
foreach (int hours in new[] { -8, 1, 9 })
{
    var offset = factory.CreateOffsetFromHours(hours);
    var zone = factory.CreateFixedOffsetTimeZone(offset);
    var local = zone.ConvertInstant(instant);
    Console.WriteLine($"  UTC{offset.ToIsoString(),6}: {local.ToIsoString()}");
    if (!local.GetInstant().EqualsValue(instant))
    {
        throw new InvalidOperationException("Time-zone conversion changed the instant.");
    }
}

Console.WriteLine("All three wall-clock times represent the same instant.");
