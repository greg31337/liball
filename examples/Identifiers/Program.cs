using System.Globalization;
using uuid;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var uuids = new Uuid();
var identifiers = new Identifier();

// The standard DNS namespace makes name-based UUIDs reproducible across systems.
var dnsNamespace = uuids.ParseValue("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
var named = uuids.FromNameV5(dnsNamespace, "example.com");
Console.WriteLine("Identifier formats");
Console.WriteLine($"UUIDv5 for example.com: {named.ToCanonicalString()}");
Console.WriteLine(
    $"Same name, same UUID:   {named.EqualsValue(uuids.FromNameV5(dnsNamespace, "example.com"))}");
Console.WriteLine($"Normalized uppercase:  {identifiers.NormalizeText(named.ToUpperString())}");

// Fix the timestamp to make extraction easy to verify; random bits still vary.
var timestamp = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
var v7 = uuids.NewV7At(timestamp);
var ulids = new Ulid();
var ulid = ulids.NewAt(timestamp);
Console.WriteLine();
Console.WriteLine("Time-based identifiers (random portions change on each run)");
Console.WriteLine($"UUIDv7: {v7.ToCanonicalString()}");
Console.WriteLine($"ULID:   {ulid.ToCanonicalString()}");
Console.WriteLine($"UUIDv7 time: {v7.Time():yyyy-MM-dd HH:mm:ss 'UTC'}");
Console.WriteLine($"ULID time:   {ulid.Time():yyyy-MM-dd HH:mm:ss 'UTC'}");
Console.WriteLine(
    $"Detected formats: {identifiers.ClassifyKind(v7.ToCanonicalString())}, " +
    $"{identifiers.ClassifyKind(ulid.ToCanonicalString())}");
if (!ulids.ParseValue(ulid.ToCanonicalString()).EqualsValue(ulid) || v7.Time() != timestamp || ulid.Time() != timestamp)
{
    throw new InvalidOperationException("An identifier failed to round trip.");
}

Console.WriteLine("Parsing and timestamps: preserved.");
