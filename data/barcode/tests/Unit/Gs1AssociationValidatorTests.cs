namespace barcode.Tests;

public class Gs1AssociationValidatorTests
{
    [Test]
    public void ResolverHandlesExactAndWildcardIdentifiers()
    {
        Gs1AiDefinition? exact = Gs1AiResolver.ResolveDefinition("01");
        Gs1AiDefinition? wildcard = Gs1AiResolver.ResolveDefinition("3102");

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.Not.Null);
            Assert.That(exact!.WildcardPattern, Is.EqualTo("01"));
            Assert.That(wildcard, Is.Not.Null);
            Assert.That(wildcard!.WildcardPattern, Is.EqualTo("310n"));
            Assert.That(Gs1AiResolver.ResolveDefinition("04"), Is.Null);
            Assert.That(Gs1AiResolver.MatchesPattern("3102", "310n"), Is.True);
            Assert.That(Gs1AiResolver.MatchesPattern("3112", "310n"), Is.False);
        });
    }

    [Test]
    public void ResolverHonorsAssignedRangeAndPrefixLengthBoundaries()
    {
        Gs1AiDefinition? firstMeasured =
            Gs1AiResolver.ResolveDefinition("3100");
        Gs1AiDefinition? lastMeasured =
            Gs1AiResolver.ResolveDefinition("3105");

        Assert.Multiple(() =>
        {
            Assert.That(firstMeasured, Is.Not.Null);
            Assert.That(lastMeasured, Is.SameAs(firstMeasured));
            Assert.That(Gs1AiResolver.ResolveDefinition("3106"), Is.Null);
            Assert.That(Gs1AiResolver.ResolveDefinition("310"), Is.Null);
            Assert.That(Gs1AiResolver.ResolveDefinition("31000"), Is.Null);
            Assert.That(Gs1AiResolver.ResolveDefinition("310A"), Is.Null);
            Assert.That(Gs1AiResolver.MatchesPattern("3100", "310n"), Is.True);
            Assert.That(Gs1AiResolver.MatchesPattern("3105", "310n"), Is.True);
            Assert.That(Gs1AiResolver.MatchesPattern("310", "310n"), Is.False);
            Assert.That(Gs1AiResolver.MatchesPattern("31000", "310n"), Is.False);
            Assert.That(Gs1AiResolver.MatchesPattern("310A", "310n"), Is.False);
        });
    }

    [Test]
    public void RequiredAlternativesRequireEveryMemberOfOneGroup()
    {
        Gs1AssociationValidationResult missing = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("250", 16));
        Gs1AssociationValidationResult complete = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("21", 16),
            CreateElement("250", 24));
        Gs1AssociationValidationResult wildcard = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("3102", 16),
            CreateElement("3922", 26));

        Assert.Multiple(() =>
        {
            AssertIssue(
                missing,
                "GS1_REQUIRED_ASSOCIATION_MISSING",
                16,
                "250");
            Assert.That(missing.Issues[0].Expected,
                Does.Contain("01+21"));
            Assert.That(complete.Success, Is.True);
            Assert.That(wildcard.Success, Is.True);
        });
    }

    [Test]
    public void RepeatedIdentifierIsReportedAtEachLaterOccurrence()
    {
        Gs1AssociationValidationResult result = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("10", 16),
            CreateElement("10", 25),
            CreateElement("10", 34));

        BarcodeValidationIssue[] repeated = result.Issues
            .Where(issue => issue.Code
                == "GS1_REPEATED_APPLICATION_IDENTIFIER")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(repeated, Has.Length.EqualTo(2));
            Assert.That(repeated.Select(issue => issue.Offset),
                Is.EqualTo(new int[] { 25, 34 }));
        });
    }

    [Test]
    public void ExactAndWildcardExclusionsAreReportedOncePerPair()
    {
        Gs1AssociationValidationResult exact = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("255", 16));
        Gs1AssociationValidationResult wildcard = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("3102", 16),
            CreateElement("3103", 26));

        Assert.Multiple(() =>
        {
            AssertIssue(exact, "GS1_EXCLUDED_ASSOCIATION", 16, "255");
            Assert.That(exact.Issues, Has.Length.EqualTo(1));
            AssertIssue(wildcard, "GS1_EXCLUDED_ASSOCIATION", 26, "3103");
            Assert.That(wildcard.Issues, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void WildcardAssociationsCoverRangeEdgesWithoutCrossingFamilies()
    {
        Gs1AssociationValidationResult lowerAndUpperEdges = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("3100", 16),
            CreateElement("3105", 26));
        Gs1AssociationValidationResult adjacentFamilies = Validate(
            Gs1Syntax.ElementString,
            CreateElement("01", 0),
            CreateElement("3105", 16),
            CreateElement("3110", 26));
        Gs1AssociationValidationResult missingRequirement = Validate(
            Gs1Syntax.ElementString,
            CreateElement("3105", 16));

        Assert.Multiple(() =>
        {
            AssertIssue(
                lowerAndUpperEdges,
                "GS1_EXCLUDED_ASSOCIATION",
                26,
                "3105");
            Assert.That(lowerAndUpperEdges.Issues, Has.Length.EqualTo(1));
            Assert.That(adjacentFamilies.Success, Is.True);
            AssertIssue(
                missingRequirement,
                "GS1_REQUIRED_ASSOCIATION_MISSING",
                16,
                "3105");
            Assert.That(missingRequirement.Issues[0].Expected,
                Is.EqualTo("01 or 02"));
        });
    }

    [Test]
    public void DigitalLinkRequiresExactlyOneDistinctPrimaryKey()
    {
        Gs1AssociationValidationResult missing = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("91", 0));
        Gs1AssociationValidationResult multiple = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("00", 0),
            CreateElement("01", 20));

        Assert.Multiple(() =>
        {
            AssertIssue(
                missing,
                "GS1_DIGITAL_LINK_PRIMARY_KEY_MISSING",
                -1,
                "DigitalLinkPrimaryKey");
            AssertIssue(
                multiple,
                "GS1_DIGITAL_LINK_MULTIPLE_PRIMARY_KEYS",
                20,
                "01");
        });
    }

    [Test]
    public void DigitalLinkQualifierSubsequencesPreserveDeclaredOrder()
    {
        Gs1AssociationValidationResult complete = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("01", 0),
            CreateElement("22", 16),
            CreateElement("10", 21),
            CreateElement("21", 29));
        Gs1AssociationValidationResult optional = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("01", 0),
            CreateElement("10", 16),
            CreateElement("21", 24));
        Gs1AssociationValidationResult alternate = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("01", 0),
            CreateElement("235", 16));
        Gs1AssociationValidationResult outOfOrder = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("01", 0),
            CreateElement("21", 16),
            CreateElement("10", 24));
        Gs1AssociationValidationResult mixedAlternatives = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("01", 0),
            CreateElement("235", 16),
            CreateElement("10", 24));

        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(optional.Success, Is.True);
            Assert.That(alternate.Success, Is.True);
            AssertIssue(
                outOfOrder,
                "GS1_DIGITAL_LINK_QUALIFIER_SEQUENCE_INVALID",
                24,
                "10");
            Assert.That(outOfOrder.Issues[0].Expected,
                Is.EqualTo("22+10+21 or 235"));
            Assert.That(outOfOrder.Issues[0].Observed,
                Is.EqualTo("21+10"));
            AssertIssue(
                mixedAlternatives,
                "GS1_DIGITAL_LINK_QUALIFIER_SEQUENCE_INVALID",
                24,
                "10");
        });
    }

    [Test]
    public void DigitalLinkQualifierMustFollowItsPrimaryKey()
    {
        Gs1AssociationValidationResult result = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("10", 0),
            CreateElement("01", 10));

        AssertIssue(
            result,
            "GS1_DIGITAL_LINK_QUALIFIER_BEFORE_PRIMARY_KEY",
            0,
            "10");
    }

    [Test]
    public void DigitalLinkRejectsNonAttributeAiAndAcceptsDataAttributes()
    {
        Gs1AssociationValidationResult rejected = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("00", 0),
            CreateElement("7041", 20));
        Gs1AssociationValidationResult accepted = Validate(
            Gs1Syntax.DigitalLinkUri,
            CreateElement("00", 0),
            CreateElement("91", 20));

        Assert.Multiple(() =>
        {
            AssertIssue(
                rejected,
                "GS1_DIGITAL_LINK_DATA_ATTRIBUTE_NOT_PERMITTED",
                20,
                "7041");
            Assert.That(accepted.Success, Is.True);
        });
    }

    [Test]
    public void NullMessageReturnsStableIssue()
    {
        Gs1AssociationValidationResult result =
            Gs1AssociationValidator.ValidateMessage(null);

        AssertIssue(
            result,
            "GS1_ASSOCIATION_MESSAGE_MISSING",
            -1,
            "Message");
    }

    static Gs1AssociationValidationResult Validate(
        Gs1Syntax syntax,
        params Gs1Element[] elements)
    {
        Gs1Message message = new Gs1Message();
        message.Syntax = syntax;
        message.Elements = elements;
        return Gs1AssociationValidator.ValidateMessage(message);
    }

    static Gs1Element CreateElement(string identifier, int sourceOffset)
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = identifier;
        element.SourceOffset = sourceOffset;
        return element;
    }

    static void AssertIssue(
        Gs1AssociationValidationResult result,
        string code,
        int offset,
        string field)
    {
        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(issue.Offset, Is.EqualTo(offset));
            Assert.That(issue.Field, Is.EqualTo(field));
            Assert.That(issue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Standard,
                Is.EqualTo(Gs1ElementStringParser.StandardName));
            Assert.That(issue.Message, Is.Not.Empty);
            Assert.That(issue.Expected, Is.Not.Empty);
            Assert.That(issue.Observed, Is.Not.Empty);
        });
    }
}
