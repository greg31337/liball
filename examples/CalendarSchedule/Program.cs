using time;

var factory = new TimeFactory();

// Calendar months clamp to the last valid day, unlike fixed day increments.
var january = factory.ParseLocalDate("2024-01-31");
Console.WriteLine("Calendar arithmetic");
Console.WriteLine($"{january.ToIsoString()} + 1 calendar month = {january.AddMonths(1).ToIsoString()}");
Console.WriteLine($"{january.ToIsoString()} + 30 days          = {january.AddDays(30).ToIsoString()}");
var start = factory.ParseInstant("2026-01-05T09:00:00Z");
var finish = start.AddDuration(factory.ParseDuration("PT90M"));
Console.WriteLine($"90-minute meeting: {start.ToIsoString()} -> {finish.ToIsoString()}");
Console.WriteLine();

// Recurrences use local dates; the UTC duration example above measures elapsed time.
const string ruleText = "FREQ=WEEKLY;BYDAY=MO,WE,FR;COUNT=6";
Console.WriteLine($"Six local meetings: {ruleText}");
var localStart = factory.ParseLocalDateTime("2026-01-05T09:00:00");
var rule = factory.ParseRecurrenceRule(ruleText);
foreach (var occurrence in factory.EnumerateLocalRecurrenceRule(localStart, rule, 100))
{
    Console.WriteLine($"  {occurrence.ToIsoString()}");
}
