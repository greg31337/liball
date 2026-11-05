using fake;

// A fixed seed makes the sample repeatable; reserved addresses keep the data fictional.
var options = new FakeOptions
{
    Seed = 42,
    Locale = "en-US",
    SafetyMode = SafetyMode.Reserved
};

var generator = new Fake(options);
Console.WriteLine("Synthetic customers (seed 42)");
Console.WriteLine($"{"Name",-28} Email");
string? firstName = null;
for (int i = 0; i < 5; i++)
{
    string name = generator.Person().FullName();
    firstName ??= name;
    Console.WriteLine($"{name,-28} {generator.Internet().ExampleEmail()}");
}

// Restarting with identical options reproduces the first generated name.
string replay = new Fake(options).Person().FullName();
Console.WriteLine($"\nSame seed, first customer: {replay}");
if (replay != firstName)
{
    throw new InvalidOperationException("The generator did not replay.");
}
