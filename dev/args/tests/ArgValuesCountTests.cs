using NUnit.Framework;

namespace args.Tests;

[TestFixture]
public class ArgValuesCountTests
{
    [Test]
    public void Count_AggregatesClustersAndRepeatedCanonicalOrAliasOccurrences()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--verbose").Alias("-v").Count();
        ArgValues values = new ArgValues(app.Parse(new string[] { "-vv", "--verbose", "-v" }));

        Assert.That(values.Count("--verbose"), Is.EqualTo(4));
        Assert.That(values.Count("-v"), Is.EqualTo(4));
    }

    [Test]
    public void Count_ResolvesInheritedGlobalOptionAliasesOnNestedCommands()
    {
        ArgApp app = new ArgApp("tool");
        app.RootCommand.GlobalOption("--verbose").Alias("-v").Count();
        app.Command("build");
        IArgValues values = new ArgValues(app.Parse(new string[] { "-v", "build", "-vv" }));

        Assert.That(values.Count("--verbose"), Is.EqualTo(3));
        Assert.That(values.Count("-v"), Is.EqualTo(3));
    }

    [Test]
    public void Count_ReturnsZeroForAbsentAndNonCountOptionsWithoutChangingConversionState()
    {
        ArgApp app = new ArgApp("tool");
        app.Option("--quiet").Alias("-q").Flag();
        ArgValues values = new ArgValues(app.Parse(new string[] { "--quiet" }));
        ArgConvertResult previous = values.GetLastConversionResult();

        Assert.That(values.Count("--missing"), Is.EqualTo(0));
        Assert.That(values.Count("--quiet"), Is.EqualTo(0));
        Assert.That(values.Count("-q"), Is.EqualTo(0));
        Assert.That(values.GetLastConversionResult(), Is.SameAs(previous));
        Assert.That(values.GetConversionErrors(), Is.Empty);
    }
}
