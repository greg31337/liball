using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgParserSettingsTests
{
    [Test]
    public void Constructor_UsesConservativeGnuPosixDefaults()
    {
        ArgParserSettings settings = new ArgParserSettings();

        Assert.That(settings.Mode, Is.EqualTo(ArgParserMode.Custom));
        Assert.That(settings.EnablePosixShortClusters, Is.True);
        Assert.That(settings.EnableShortOptionAttachedValues, Is.False);
        Assert.That(settings.EnableSlashOptions, Is.False);
        Assert.That(settings.OptionsCaseInsensitive, Is.False);
        Assert.That(settings.CommandsCaseInsensitive, Is.False);
        Assert.That(settings.CollectUnmatched, Is.False);
        Assert.That(settings.StopAtFirstPositional, Is.False);
        Assert.That(settings.StopAtFirstUnmatched, Is.False);
        Assert.That(settings.EndOfOptionsDelimiter, Is.EqualTo("--"));
        Assert.That(settings.HelpWidth, Is.EqualTo(80));
        Assert.That(settings.IncludeHiddenInHelp, Is.False);
        Assert.That(settings.EnableAnsiHelpStyling, Is.False);
        Assert.That(settings.HelpAliases, Is.EqualTo(new string[] { "--help", "-h" }));
        Assert.That(settings.Environment, Is.TypeOf<ArgSystemEnvironment>());
        Assert.That(settings.MessageProvider, Is.TypeOf<ArgEnglishMessageProvider>());
        Assert.That(settings.ConversionCulture, Is.SameAs(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void Fields_CanBeChangedByConsumers()
    {
        ArgParserSettings settings = new ArgParserSettings();

        settings.Mode = ArgParserMode.WindowsFriendly;
        settings.EnablePosixShortClusters = false;
        settings.EnableShortOptionAttachedValues = true;
        settings.EnableSlashOptions = true;
        settings.OptionsCaseInsensitive = true;
        settings.CommandsCaseInsensitive = true;
        settings.CollectUnmatched = true;
        settings.StopAtFirstPositional = true;
        settings.StopAtFirstUnmatched = true;
        settings.EndOfOptionsDelimiter = "++";
        settings.HelpWidth = 100;
        settings.IncludeHiddenInHelp = true;
        settings.EnableAnsiHelpStyling = true;
        settings.HelpAliases.Clear();
        settings.HelpAliases.Add("--assist");
        settings.HelpAliases.Add("-?");
        TestArgEnvironment environment = new TestArgEnvironment();
        settings.Environment = environment;
        IArgMessageProvider messageProvider = new TestArgMessageProvider();
        settings.MessageProvider = messageProvider;
        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("fr-FR");
        settings.ConversionCulture = culture;

        Assert.That(settings.Mode, Is.EqualTo(ArgParserMode.WindowsFriendly));
        Assert.That(settings.EnablePosixShortClusters, Is.False);
        Assert.That(settings.EnableShortOptionAttachedValues, Is.True);
        Assert.That(settings.EnableSlashOptions, Is.True);
        Assert.That(settings.OptionsCaseInsensitive, Is.True);
        Assert.That(settings.CommandsCaseInsensitive, Is.True);
        Assert.That(settings.CollectUnmatched, Is.True);
        Assert.That(settings.StopAtFirstPositional, Is.True);
        Assert.That(settings.StopAtFirstUnmatched, Is.True);
        Assert.That(settings.EndOfOptionsDelimiter, Is.EqualTo("++"));
        Assert.That(settings.HelpWidth, Is.EqualTo(100));
        Assert.That(settings.IncludeHiddenInHelp, Is.True);
        Assert.That(settings.EnableAnsiHelpStyling, Is.True);
        Assert.That(settings.HelpAliases, Is.EqualTo(new string[] { "--assist", "-?" }));
        Assert.That(settings.Environment, Is.SameAs(environment));
        Assert.That(settings.MessageProvider, Is.SameAs(messageProvider));
        Assert.That(settings.ConversionCulture, Is.SameAs(culture));
    }
}
