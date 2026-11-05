using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgCultureConversionTests
{
    [Test]
    public void Values_UseCallerSelectedCultureForEveryCultureSensitiveBuiltInConversion()
    {
        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("fr-FR");
        culture.NumberFormat.NegativeSign = "~";
        ArgParserSettings settings = new ArgParserSettings();
        settings.ConversionCulture = culture;
        ArgApp app = new ArgApp("tool");
        app.UseSettings(settings);
        app.Option("--int").Value("value");
        app.Option("--long").Value("value");
        app.Option("--double").Value("value");
        app.Option("--decimal").Value("value");
        app.Option("--date").Value("value");
        app.Option("--duration").Value("value");

        ArgParseResult result = app.Parse(new string[] { "--int", "~42", "--long", "~9000000000", "--double", "3,5", "--decimal", "12,75", "--date", "25/08/2026", "--duration", "01:02:03,5" });
        ArgValues values = new ArgValues(result);

        Assert.That(values.Int("--int"), Is.EqualTo(-42));
        Assert.That(values.Long("--long"), Is.EqualTo(-9000000000L));
        Assert.That(values.Double("--double"), Is.EqualTo(3.5d));
        Assert.That(values.Decimal("--decimal"), Is.EqualTo(12.75m));
        Assert.That(values.DateTime("--date"), Is.EqualTo(new System.DateTime(2026, 8, 25)));
        Assert.That(values.TimeSpan("--duration"), Is.EqualTo(new System.TimeSpan(0, 1, 2, 3, 500)));
        Assert.That(values.GetConversionErrors(), Is.Empty);
    }

    [Test]
    public void Values_RejectNullSelectedCulture()
    {
        ArgParserSettings settings = new ArgParserSettings();
        settings.ConversionCulture = null!;
        ArgApp app = new ArgApp("tool");
        app.UseSettings(settings);

        ArgParseResult result = app.Parse(new string[] { });

        System.ArgumentNullException exception = Assert.Throws<System.ArgumentNullException>(delegate { new ArgValues(result); })!;
        Assert.That(exception.ParamName, Is.EqualTo("culture"));
    }
}
