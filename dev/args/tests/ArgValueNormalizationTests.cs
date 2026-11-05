using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValueNormalizationTests
{
    [Test]
    public void Values_NormalizeOptionsAndArgumentsBeforeRetrievalAndBuiltInConversion()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--enabled").Value("value").Normalize(TrimLower);
        app.Argument("number").Normalize(TrimLower);
        app.Argument("items").Variadic().Normalize(TrimLower);

        ArgParseResult result = app.Parse(new string[] { "--enabled", " YES ", " 42 ", " FIRST ", "Second" });
        ArgValues values = new ArgValues(result);

        Assert.That(result.Occurrences[0].Values[0], Is.EqualTo(" YES "));
        Assert.That(result.Occurrences[1].Values[0], Is.EqualTo(" 42 "));
        Assert.That(result.Occurrences[2].Values, Is.EqualTo(new string[] { " FIRST ", "Second" }));
        Assert.That(values.String("--enabled"), Is.EqualTo("yes"));
        Assert.That(values.Bool("--enabled"), Is.True);
        Assert.That(values.String("number"), Is.EqualTo("42"));
        Assert.That(values.Int("number"), Is.EqualTo(42));
        Assert.That(values.Strings("items"), Is.EqualTo(new string[] { "first", "second" }));
    }

    [Test]
    public void Validate_DefaultRawStageIgnoresNormalizerWhileNormalizedStageUsesItForValuesAndChoices()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--raw").Value("value").Choice("release").Normalize(TrimLower);
        app.Option("--normalized").Value("value").Choice(" Release ").Normalize(TrimLower).ValidateChoicesAt(ArgChoiceValidationStage.Normalized);
        app.Argument("action").Choice(" Build ").Normalize(TrimLower).ValidateChoicesAt(ArgChoiceValidationStage.Normalized);

        ArgValidationResult result = app.Validate(new string[] { "--raw", "RELEASE", "--normalized", "RELEASE", "BUILD" });

        Assert.That(result.Errors.Count, Is.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_choice"));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--raw"));
    }

    [Test]
    public void Validate_ConvertedStageNormalizesThenComparesConvertedOptionAndArgumentChoices()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--level").Value("value").Choice("#01").Normalize(RemoveMarker).ConvertChoices(ConvertInt).ValidateChoicesAt(ArgChoiceValidationStage.Converted);
        app.Argument("count").Choice("#002").Normalize(RemoveMarker).ConvertChoices(ConvertInt).ValidateChoicesAt(ArgChoiceValidationStage.Converted);

        ArgValidationResult result = app.Validate(new string[] { "--level", "#1", "#2" });

        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ConvertedStageRejectsFailedOrMissingConversion()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--failed").Value("value").Choice("1").ConvertChoices(ConvertInt).ValidateChoicesAt(ArgChoiceValidationStage.Converted);
        app.Option("--missing").Value("value").Choice("same").ValidateChoicesAt(ArgChoiceValidationStage.Converted);

        ArgValidationResult result = app.Validate(new string[] { "--failed", "not-an-int", "--missing", "same" });

        Assert.That(result.Errors.Count, Is.EqualTo(2));
        Assert.That(result.Errors[0].SymbolName, Is.EqualTo("--failed"));
        Assert.That(result.Errors[1].SymbolName, Is.EqualTo("--missing"));
    }

    [Test]
    public void Configuration_ExposesPipelineAndRejectsInvalidInputs()
    {
        ArgOption option = new ArgOption("--value");
        ArgArgument argument = new ArgArgument("value");

        Assert.That(option.Normalize(TrimLower), Is.SameAs(option));
        Assert.That(option.ConvertChoices(ConvertInt), Is.SameAs(option));
        Assert.That(option.ValidateChoicesAt(ArgChoiceValidationStage.Converted), Is.SameAs(option));
        Assert.That(option.GetValueNormalizer(), Is.EqualTo((System.Func<string, string>)TrimLower));
        Assert.That(option.GetChoiceConverter(), Is.EqualTo((System.Func<string, ArgConvertResult?>)ConvertInt));
        Assert.That(option.GetChoiceValidationStage(), Is.EqualTo(ArgChoiceValidationStage.Converted));
        Assert.That(argument.Normalize(TrimLower), Is.SameAs(argument));
        Assert.That(argument.ConvertChoices(ConvertInt), Is.SameAs(argument));
        Assert.That(argument.ValidateChoicesAt(ArgChoiceValidationStage.Normalized), Is.SameAs(argument));
        Assert.That(argument.GetValueNormalizer(), Is.EqualTo((System.Func<string, string>)TrimLower));
        Assert.That(argument.GetChoiceConverter(), Is.EqualTo((System.Func<string, ArgConvertResult?>)ConvertInt));
        Assert.That(argument.GetChoiceValidationStage(), Is.EqualTo(ArgChoiceValidationStage.Normalized));
        Assert.Throws<System.ArgumentNullException>(delegate { option.Normalize(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { option.ConvertChoices(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { argument.Normalize(null!); });
        Assert.Throws<System.ArgumentNullException>(delegate { argument.ConvertChoices(null!); });
        Assert.Throws<System.ArgumentOutOfRangeException>(delegate { option.ValidateChoicesAt((ArgChoiceValidationStage)99); });
        Assert.Throws<System.ArgumentOutOfRangeException>(delegate { argument.ValidateChoicesAt((ArgChoiceValidationStage)99); });
    }

    [Test]
    public void Values_RejectNormalizerThatReturnsNull()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--value").Value("value").Normalize(ReturnNull);
        ArgValues values = new ArgValues(app.Parse(new string[] { "--value", "text" }));

        Assert.Throws<System.InvalidOperationException>(delegate { values.String("--value"); });
    }

    static string TrimLower(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    static string RemoveMarker(string value)
    {
        return value.Substring(1);
    }

    static string ReturnNull(string value)
    {
        return null!;
    }

    static ArgConvertResult ConvertInt(string value)
    {
        int converted = 0;
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out converted))
        {
            return new ArgConvertResult(true, converted, "");
        }

        return new ArgConvertResult(false, null, "Invalid integer.");
    }
}
