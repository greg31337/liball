using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionComparatorTests
{
    [Test]
    public void ContainsVersion_EvaluatesEqualityOperators()
    {
        UniversalVersion target = ParseVersion("1.2.3");
        UniversalVersion same = ParseVersion("1.2.3");
        UniversalVersion higher = ParseVersion("1.2.4");

        VersionComparator equal = VersionComparator.CreateEqual(target, VersionComparisonMode.Precedence);
        VersionComparator notEqual = VersionComparator.CreateNotEqual(target, VersionComparisonMode.Precedence);

        Assert.That(equal.ContainsVersion(same), Is.True);
        Assert.That(equal.ContainsVersion(higher), Is.False);
        Assert.That(notEqual.ContainsVersion(same), Is.False);
        Assert.That(notEqual.ContainsVersion(higher), Is.True);
    }

    [Test]
    public void ContainsVersion_EvaluatesGreaterThanOperators()
    {
        UniversalVersion target = ParseVersion("1.2.3");
        UniversalVersion lower = ParseVersion("1.2.2");
        UniversalVersion same = ParseVersion("1.2.3");
        UniversalVersion higher = ParseVersion("1.2.4");

        VersionComparator greaterThan = VersionComparator.CreateGreaterThan(target, VersionComparisonMode.Precedence);
        VersionComparator greaterThanOrEqual = VersionComparator.CreateGreaterThanOrEqual(target, VersionComparisonMode.Precedence);

        Assert.That(greaterThan.ContainsVersion(lower), Is.False);
        Assert.That(greaterThan.ContainsVersion(same), Is.False);
        Assert.That(greaterThan.ContainsVersion(higher), Is.True);
        Assert.That(greaterThanOrEqual.ContainsVersion(lower), Is.False);
        Assert.That(greaterThanOrEqual.ContainsVersion(same), Is.True);
        Assert.That(greaterThanOrEqual.ContainsVersion(higher), Is.True);
    }

    [Test]
    public void ContainsVersion_EvaluatesLessThanOperators()
    {
        UniversalVersion target = ParseVersion("1.2.3");
        UniversalVersion lower = ParseVersion("1.2.2");
        UniversalVersion same = ParseVersion("1.2.3");
        UniversalVersion higher = ParseVersion("1.2.4");

        VersionComparator lessThan = VersionComparator.CreateLessThan(target, VersionComparisonMode.Precedence);
        VersionComparator lessThanOrEqual = VersionComparator.CreateLessThanOrEqual(target, VersionComparisonMode.Precedence);

        Assert.That(lessThan.ContainsVersion(lower), Is.True);
        Assert.That(lessThan.ContainsVersion(same), Is.False);
        Assert.That(lessThan.ContainsVersion(higher), Is.False);
        Assert.That(lessThanOrEqual.ContainsVersion(lower), Is.True);
        Assert.That(lessThanOrEqual.ContainsVersion(same), Is.True);
        Assert.That(lessThanOrEqual.ContainsVersion(higher), Is.False);
    }

    [Test]
    public void ContainsVersion_UsesConfiguredComparisonMode()
    {
        UniversalVersion target = ParseVersion("1.0.0+build.1");
        UniversalVersion samePrecedence = ParseVersion("1.0.0+build.2");

        VersionComparator precedenceEqual = VersionComparator.CreateEqual(target, VersionComparisonMode.Precedence);
        VersionComparator identityEqual = VersionComparator.CreateEqual(target, VersionComparisonMode.Identity);

        Assert.That(precedenceEqual.ContainsVersion(samePrecedence), Is.True);
        Assert.That(identityEqual.ContainsVersion(samePrecedence), Is.False);
    }

    [Test]
    public void ContainsRelation_EvaluatesPrimitiveRelations()
    {
        UniversalVersion target = ParseVersion("1.0.0");

        Assert.That(VersionComparator.CreateEqual(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.EqualTo), Is.True);
        Assert.That(VersionComparator.CreateNotEqual(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.GreaterThan), Is.True);
        Assert.That(VersionComparator.CreateGreaterThan(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.LessThan), Is.False);
        Assert.That(VersionComparator.CreateGreaterThanOrEqual(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.EqualTo), Is.True);
        Assert.That(VersionComparator.CreateLessThan(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.LessThan), Is.True);
        Assert.That(VersionComparator.CreateLessThanOrEqual(target, VersionComparisonMode.Precedence).ContainsRelation(VersionRelation.GreaterThan), Is.False);
    }

    [Test]
    public void Constructor_StoresComparatorState()
    {
        UniversalVersion target = ParseVersion("2.0.0");
        VersionComparator comparator = new VersionComparator(RangeOperator.GreaterThanOrEqual, target, VersionComparisonMode.Identity);

        Assert.That(comparator.GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(comparator.GetVersion(), Is.SameAs(target));
        Assert.That(comparator.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Identity));
        Assert.That(comparator.GetOperatorText(), Is.EqualTo(">="));
    }

    [Test]
    public void Constructor_RejectsInvalidState()
    {
        UniversalVersion target = ParseVersion("1.0.0");

        Assert.That(
            delegate
            {
                new VersionComparator((RangeOperator)99, target, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                new VersionComparator(RangeOperator.Equal, null!, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
