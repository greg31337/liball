using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class AssignmentClassifierTests
{
    [Test]
    public void EveryAlpha2CombinationHasExactlyOneAssignmentState()
    {
        Dictionary<CodeAssignmentStatus, int> counts = new Dictionary<CodeAssignmentStatus, int>();
        for (char first = 'A'; first <= 'Z'; first += (char)1)
        {
            for (char second = 'A'; second <= 'Z'; second += (char)1)
            {
                string code = new string(new[] { first, second });
                CodeAssignmentStatus status =
                    AssignmentClassifier.Classify(CountryCodeKind.Alpha2, code);
                counts[status] = counts.GetValueOrDefault(status) + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[CodeAssignmentStatus.Assigned], Is.EqualTo(249));
            Assert.That(counts[CodeAssignmentStatus.ExceptionallyReserved], Is.EqualTo(1));
            Assert.That(counts[CodeAssignmentStatus.TransitionallyReserved], Is.EqualTo(1));
            Assert.That(counts.GetValueOrDefault(CodeAssignmentStatus.IndeterminatelyReserved), Is.Zero);
            Assert.That(counts[CodeAssignmentStatus.FormerlyAssigned], Is.EqualTo(24));
            Assert.That(counts[CodeAssignmentStatus.UserAssigned], Is.EqualTo(42));
            Assert.That(counts[CodeAssignmentStatus.Unassigned], Is.EqualTo(359));
            Assert.That(counts.Values.Sum(), Is.EqualTo(676));
        });
    }

    [Test]
    public void CurrentAssignmentsTakePrecedenceOverReusedHistoricalIdentifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                AssignmentClassifier.Classify(CountryCodeKind.Alpha2, "BY"),
                Is.EqualTo(CodeAssignmentStatus.Assigned));
            Assert.That(
                AssignmentClassifier.Classify(CountryCodeKind.Alpha3, "BLR"),
                Is.EqualTo(CodeAssignmentStatus.Assigned));
            Assert.That(
                AssignmentClassifier.Classify(CountryCodeKind.Numeric3, "104"),
                Is.EqualTo(CodeAssignmentStatus.Assigned));
        });
    }

    [Test]
    public void EveryCurrentCountryCodeFormIsAssigned()
    {
        foreach (GeneratedCountryRow row in CountryTable.Rows)
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    AssignmentClassifier.Classify(CountryCodeKind.Alpha2, row.Alpha2),
                    Is.EqualTo(CodeAssignmentStatus.Assigned),
                    row.Alpha2);
                Assert.That(
                    AssignmentClassifier.Classify(CountryCodeKind.Alpha3, row.Alpha3),
                    Is.EqualTo(CodeAssignmentStatus.Assigned),
                    row.Alpha3);
                Assert.That(
                    AssignmentClassifier.Classify(CountryCodeKind.Numeric3, row.NumericText),
                    Is.EqualTo(CodeAssignmentStatus.Assigned),
                    row.NumericText);
            });
        }
    }

    [Test]
    public void HistoricalCodesRespectCurrentAndReservedPrecedence()
    {
        foreach (GeneratedHistoricalCountryRow row in HistoricalCountryTable.Rows)
        {
            AssertHistoricalOrCurrent(CountryCodeKind.Alpha2, row.Alpha2);
            AssertHistoricalOrCurrent(CountryCodeKind.Alpha3, row.Alpha3);
            if (row.HasNumeric)
            {
                AssertHistoricalOrCurrent(CountryCodeKind.Numeric3, row.NumericText);
            }

            Assert.That(
                AssignmentClassifier.Classify(CountryCodeKind.Alpha4, row.Alpha4),
                Is.EqualTo(CodeAssignmentStatus.FormerlyAssigned),
                row.Alpha4);
        }
    }

    [Test]
    public void EveryGeneratedReservationRetainsItsExactState()
    {
        foreach (GeneratedReservedCodeRow row in ReservedCodeTable.Rows)
        {
            CountryCodeKind kind = MapKind(row.Kind);
            CodeAssignmentStatus expected =
                CountryMapper.MapAssignmentStatus(row.AssignmentState);

            Assert.That(
                AssignmentClassifier.Classify(kind, row.Code),
                Is.EqualTo(expected),
                $"{row.Kind}:{row.Code}");
        }
    }

    [TestCase(CountryCodeKind.Alpha2, "AA")]
    [TestCase(CountryCodeKind.Alpha2, "QM")]
    [TestCase(CountryCodeKind.Alpha2, "QZ")]
    [TestCase(CountryCodeKind.Alpha2, "XA")]
    [TestCase(CountryCodeKind.Alpha2, "XZ")]
    [TestCase(CountryCodeKind.Alpha2, "ZZ")]
    [TestCase(CountryCodeKind.Alpha3, "AAA")]
    [TestCase(CountryCodeKind.Alpha3, "AAZ")]
    [TestCase(CountryCodeKind.Alpha3, "QMA")]
    [TestCase(CountryCodeKind.Alpha3, "QZZ")]
    [TestCase(CountryCodeKind.Alpha3, "XAA")]
    [TestCase(CountryCodeKind.Alpha3, "XZZ")]
    [TestCase(CountryCodeKind.Alpha3, "ZZA")]
    [TestCase(CountryCodeKind.Alpha3, "ZZZ")]
    [TestCase(CountryCodeKind.Numeric3, "900")]
    [TestCase(CountryCodeKind.Numeric3, "999")]
    public void UserAssignedRangeBoundariesAreIncluded(
        CountryCodeKind kind,
        string code)
    {
        Assert.That(
            AssignmentClassifier.Classify(kind, code),
            Is.EqualTo(CodeAssignmentStatus.UserAssigned));
    }

    [TestCase(CountryCodeKind.Alpha2, "AB")]
    [TestCase(CountryCodeKind.Alpha2, "QL")]
    [TestCase(CountryCodeKind.Alpha3, "ABC")]
    [TestCase(CountryCodeKind.Alpha3, "QLZ")]
    [TestCase(CountryCodeKind.Numeric3, "899")]
    [TestCase(CountryCodeKind.Alpha4, "ABCD")]
    public void WellFormedCodesOutsideKnownStatesAreUnassigned(
        CountryCodeKind kind,
        string code)
    {
        Assert.That(
            AssignmentClassifier.Classify(kind, code),
            Is.EqualTo(CodeAssignmentStatus.Unassigned));
    }

    [Test]
    public void ClassificationFlowsThroughNormalizationAndValidation()
    {
        CountryCodeService service = new CountryCodeService();

        CodeNormalizationResult former =
            service.NormalizeCountryAlpha4("yucs", false);
        ValidationResult rejected =
            service.ValidateCountryAlpha4("YUCS", ValidationPolicy.AssignedOnly);
        ValidationResult accepted =
            service.ValidateCountryAlpha3("QMA", ValidationPolicy.AllowUserAssigned);

        Assert.Multiple(() =>
        {
            Assert.That(former.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.FormerlyAssigned));
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.Issues[0].Kind, Is.EqualTo(ValidationFailureKind.FormerlyAssigned));
            Assert.That(accepted.IsValid, Is.True);
        });
    }

    private static CountryCodeKind MapKind(GeneratedCodeKind kind)
    {
        if (kind == GeneratedCodeKind.Alpha3)
        {
            return CountryCodeKind.Alpha3;
        }

        if (kind == GeneratedCodeKind.Numeric3)
        {
            return CountryCodeKind.Numeric3;
        }

        if (kind == GeneratedCodeKind.Alpha4)
        {
            return CountryCodeKind.Alpha4;
        }

        return CountryCodeKind.Alpha2;
    }

    private static void AssertHistoricalOrCurrent(CountryCodeKind kind, string code)
    {
        CodeAssignmentStatus expected = CodeAssignmentStatus.FormerlyAssigned;
        if (AssignmentClassifier.FindCurrent(kind, code) != null)
        {
            expected = CodeAssignmentStatus.Assigned;
        }
        else
        {
            GeneratedReservedCodeRow? reserved =
                CountryIndex.FindReservedCode(MapGeneratedKind(kind), code);
            if (reserved != null)
            {
                expected = CountryMapper.MapAssignmentStatus(reserved.AssignmentState);
            }
        }

        Assert.That(
            AssignmentClassifier.Classify(kind, code),
            Is.EqualTo(expected),
            $"{kind}:{code}");
    }

    private static GeneratedCodeKind MapGeneratedKind(CountryCodeKind kind)
    {
        if (kind == CountryCodeKind.Alpha3)
        {
            return GeneratedCodeKind.Alpha3;
        }

        if (kind == CountryCodeKind.Numeric3)
        {
            return GeneratedCodeKind.Numeric3;
        }

        if (kind == CountryCodeKind.Alpha4)
        {
            return GeneratedCodeKind.Alpha4;
        }

        return GeneratedCodeKind.Alpha2;
    }
}
