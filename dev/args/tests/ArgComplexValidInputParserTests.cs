using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgComplexValidInputParserTests
{
    [Test]
    public void Parse_ValidNestedCommandCombinesInheritedGlobalsAliasesDefaultsFallbacksRepeatablesAndPositionals()
    {
        ArgCommand root = new ArgCommand("ship");
        ArgOption environment = root.GlobalOption("--environment").Alias("-e").Value("name").Default("dev");
        ArgOption verbose = root.GlobalOption("--verbose").Alias("-v").Count();
        ArgCommand service = root.Command("service").Alias("svc");
        ArgOption tenant = service.GlobalOption("--tenant").Value("name").Required();
        ArgCommand deploy = service.Command("deploy").Alias("dep");
        ArgArgument serviceName = deploy.Argument("service").Required();
        ArgArgument artifacts = deploy.Argument("artifacts").Variadic();
        ArgOption image = deploy.Option("--image").Value("ref").Required();
        ArgOption property = deploy.Option("--property").Alias("-p").Value("key=value").Repeatable();
        ArgOption wait = deploy.Option("--wait").OptionalValue("mode").Fallback("until-ready");
        ArgOption canary = deploy.Option("--canary").Flag().Requires("--image");
        ArgParser parser = new ArgParser(root);

        ArgParseResult parseResult = parser.Parse(new string[]
        {
            "svc",
            "dep",
            "api",
            "-vv",
            "--tenant",
            "acme",
            "--image=registry/api:42",
            "-p",
            "region=us",
            "--wait",
            "--canary",
            "logs.txt",
            "-p",
            "trace=true",
            "manifest.json"
        });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);
        ArgValues values = new ArgValues(parseResult);

        Assert.That(parseResult.HasErrors(), Is.False);
        Assert.That(validationResult.HasErrors(), Is.False);
        Assert.That(parseResult.Command, Is.SameAs(deploy));
        Assert.That(parseResult.CommandPath, Is.EqualTo(new ArgCommand[] { root, service, deploy }));
        Assert.That(parseResult.Positionals, Is.Empty);
        Assert.That(parseResult.UnmatchedTokens, Is.Empty);

        Assert.That(values.String("--environment"), Is.EqualTo("dev"));
        Assert.That(values.Source("--environment"), Is.EqualTo(ArgValueSource.Default));
        Assert.That(values.String("--tenant"), Is.EqualTo("acme"));
        Assert.That(values.String("--image"), Is.EqualTo("registry/api:42"));
        Assert.That(values.Strings("--property"), Is.EqualTo(new string[] { "region=us", "trace=true" }));
        Assert.That(values.String("--wait"), Is.EqualTo("until-ready"));
        Assert.That(values.Source("--wait"), Is.EqualTo(ArgValueSource.Fallback));
        Assert.That(values.Bool("--canary"), Is.True);
        Assert.That(values.String("service"), Is.EqualTo("api"));
        Assert.That(values.Strings("artifacts"), Is.EqualTo(new string[] { "logs.txt", "manifest.json" }));
        Assert.That(parseResult.CountOption("--verbose"), Is.EqualTo(2));

        Assert.That(parseResult.OptionOccurrences("--environment")[0].Option, Is.SameAs(environment));
        Assert.That(parseResult.OptionOccurrences("--verbose")[0].Option, Is.SameAs(verbose));
        Assert.That(parseResult.OptionOccurrences("--tenant")[0].Option, Is.SameAs(tenant));
        Assert.That(parseResult.OptionOccurrences("--image")[0].Option, Is.SameAs(image));
        Assert.That(parseResult.OptionOccurrences("--property")[0].Option, Is.SameAs(property));
        Assert.That(parseResult.OptionOccurrences("--wait")[0].Option, Is.SameAs(wait));
        Assert.That(parseResult.OptionOccurrences("--canary")[0].Option, Is.SameAs(canary));
        Assert.That(ArgumentOccurrence(parseResult, "service").Argument, Is.SameAs(serviceName));
        Assert.That(ArgumentOccurrence(parseResult, "artifacts").Argument, Is.SameAs(artifacts));
    }

    static ArgOccurrence ArgumentOccurrence(ArgParseResult result, string name)
    {
        int index = 0;
        while (index < result.Occurrences.Count)
        {
            ArgOccurrence occurrence = result.Occurrences[index];
            if (occurrence.SymbolKind == ArgSymbolKind.Argument && occurrence.SymbolName == name)
            {
                return occurrence;
            }

            index = index + 1;
        }

        Assert.Fail("Expected argument occurrence '" + name + "'.");
        return new ArgOccurrence();
    }
}
