using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTestSuiteArchiveTests
{
    [Test]
    public void PinnedArchive_LoadsEveryCaseAndHasNoUnsupportedList()
    {
        IReadOnlyList<YamlTestSuiteCase> cases = YamlTestSuiteArchive.Cases();

        Assert.That(YamlTestSuiteArchive.Release, Is.EqualTo("data-2022-01-17"));
        Assert.That(YamlTestSuiteArchive.Source, Is.EqualTo("https://github.com/yaml/yaml-test-suite"));
        Assert.That(cases.Count, Is.EqualTo(YamlTestSuiteArchive.ExpectedCaseCount));
        Assert.That(cases.Count(item => item.ShouldParse), Is.EqualTo(YamlTestSuiteArchive.ExpectedValidCount));
        Assert.That(cases.Count(item => !item.ShouldParse), Is.EqualTo(YamlTestSuiteArchive.ExpectedInvalidCount));
        Assert.That(cases.Select(item => item.Id), Is.Unique);
        Assert.That(cases.All(item => item.Name != ""), Is.True);
        Assert.That(cases.Single(item => item.Id == "AVM7").InputYaml, Is.Empty);
        Assert.That(cases.All(item => item.ExpectedEvents != ""), Is.True);
        Assert.That(UnsupportedCases(), Is.Empty);
    }

    public static string[] UnsupportedCases()
    {
        return new string[0];
    }
}
