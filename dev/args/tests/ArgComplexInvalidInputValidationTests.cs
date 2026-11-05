using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgComplexInvalidInputValidationTests
{
    [Test]
    public void Validate_InvalidNestedCommandCombinesParseAndValidationErrors()
    {
        ArgCommand root = new ArgCommand("ship");
        root.GlobalOption("--workspace").Value("path").Required();
        ArgCommand deploy = root.Command("deploy");
        deploy.Option("--image").Value("ref").Required();
        deploy.Option("--mode").Value("name").Choice("blue").Choice("green");
        deploy.Option("--force").Flag().Conflicts("--dry-run");
        deploy.Option("--dry-run").Flag();
        deploy.Option("--profile").Value("name");
        deploy.Option("--canary").Flag().Requires("--image");
        ArgParser parser = new ArgParser(root);

        ArgParseResult parseResult = parser.Parse(new string[]
        {
            "deploy",
            "--mode",
            "turbo",
            "--force",
            "--dry-run",
            "--profile",
            "prod",
            "--profile",
            "stage",
            "--canary",
            "--unknown"
        });
        ArgValidationResult validationResult = new ArgValidator().Validate(parseResult);

        Assert.That(parseResult.Command, Is.SameAs(deploy));
        Assert.That(parseResult.Errors.Count, Is.EqualTo(1));
        AssertError(parseResult.Errors[0], "unknown_option", ArgSymbolKind.Option, "--unknown", 10);

        Assert.That(validationResult.Errors.Count, Is.EqualTo(7));
        AssertHasError(validationResult, "unknown_option", ArgSymbolKind.Option, "--unknown", 10);
        AssertHasError(validationResult, "missing_required_option", ArgSymbolKind.Option, "--image", -1);
        AssertHasError(validationResult, "missing_required_option", ArgSymbolKind.Option, "--workspace", -1);
        AssertHasError(validationResult, "duplicate_option", ArgSymbolKind.Option, "--profile", 7);
        AssertHasError(validationResult, "invalid_choice", ArgSymbolKind.Option, "--mode", 1);
        AssertHasError(validationResult, "conflicting_option", ArgSymbolKind.Option, "--force", 3);
        AssertHasError(validationResult, "missing_dependent_option", ArgSymbolKind.Option, "--canary", 9);
    }

    static void AssertHasError(ArgValidationResult result, string code, ArgSymbolKind kind, string symbolName, int tokenIndex)
    {
        int index = 0;
        while (index < result.Errors.Count)
        {
            ArgParseError error = result.Errors[index];
            if (error.Code == code && error.SymbolKind == kind && error.SymbolName == symbolName && error.TokenIndex == tokenIndex)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected " + code + " error for '" + symbolName + "' at token " + tokenIndex + ".");
    }

    static void AssertError(ArgParseError error, string code, ArgSymbolKind kind, string symbolName, int tokenIndex)
    {
        Assert.That(error.Code, Is.EqualTo(code));
        Assert.That(error.SymbolKind, Is.EqualTo(kind));
        Assert.That(error.SymbolName, Is.EqualTo(symbolName));
        Assert.That(error.TokenIndex, Is.EqualTo(tokenIndex));
    }
}
