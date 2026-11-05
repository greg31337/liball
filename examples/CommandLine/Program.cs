using global::args;

var app = new ArgApp("greet").Description("A greeting app with typed options and aliases.");
app.Option("--name").Alias("-n").Value("NAME").Default("world").Description("Who to greet");
app.Option("--repeat").Alias("-r").Value("COUNT").Default("1").Description("Repeat 1 to 5 times");
app.Option("--shout").Flag().Description("Use uppercase");
app.Option("--help").Alias("-h").Flag().Description("Show help");

// With no arguments, run a useful built-in invocation. Custom arguments are parsed normally.
string[] invocation = args.Length == 0 ? new[] { "--name", "Ada", "-r", "2", "--shout" } : args;
var parsed = app.Parse(invocation);
if (new ArgResultStatus().ParseFailed(parsed))
{
    foreach (var error in parsed.Errors)
    {
        Console.Error.WriteLine($"{error.Code}: {error.Message}");
    }

    return 1;
}

var values = new ArgValues(parsed);
if (values.IsSupplied("--help"))
{
    Console.WriteLine(new ArgHelpBuilder().RootHelp(app));
    return 0;
}

int count = values.Int("--repeat");
if (values.HasConversionErrors() || count < 1 || count > 5)
{
    Console.Error.WriteLine("--repeat must be an integer from 1 to 5.");
    return 1;
}

// Check whether a flag was supplied without attempting a missing-value conversion.
bool shout = values.IsSupplied("--shout");
Console.WriteLine($"Parsed: name={values.String("--name")}, repeat={count}, shout={shout}");
string greeting = $"Hello, {values.String("--name")}!";
if (shout)
{
    greeting = greeting.ToUpperInvariant();
}

for (int i = 0; i < count; i++)
{
    Console.WriteLine(greeting);
}

return 0;
