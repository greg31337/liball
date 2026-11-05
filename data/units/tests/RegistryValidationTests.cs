using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class RegistryValidationTests
{
    [Test]
    public void Validate_ReturnsValidReportForConsistentRegistry()
    {
        UnitRegistry registry = CreateConsistentRegistry();

        RegistryValidationReport report = registry.Validate();

        Assert.That(report.IsValid(), Is.True);
        Assert.That(report.HasIssues(), Is.False);
        Assert.That(report.GetIssueCount(), Is.EqualTo(0));
        Assert.That(report.GetIssues(), Is.EqualTo(new RegistryValidationIssue[0]));
    }

    [Test]
    public void AddQuantityKind_RejectsMissingDefaultUnit()
    {
        UnitRegistry registry = new UnitRegistry();
        QuantityKindDefinition quantityKind = new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "meter",
            new string[0],
            new string[0]);

        OperationResult result = registry.AddQuantityKind(quantityKind);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(result.GetMessage(), Does.Contain("meter"));
        Assert.That(registry.GetQuantityKinds(), Is.Empty);
    }

    [Test]
    public void Validate_ReportsDuplicateAliases()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Unit("meter", "meter", "meters", "m", new string[] { "shared_alias" }, new StandardDimensions().Length());
        UnitDefinition foot = Unit("foot", "foot", "feet", "ft", new string[] { "shared_alias" }, new StandardDimensions().Length());
        registry.AddUnit(meter);
        registry.AddUnit(foot);

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue issue = FindIssue(report, RegistryValidationIssueKind.DuplicateAlias);

        Assert.That(report.ContainsKind(RegistryValidationIssueKind.DuplicateAlias), Is.True);
        Assert.That(issue.GetSubjectId(), Is.EqualTo("foot"));
        Assert.That(issue.GetDetail(), Is.EqualTo("shared_alias"));
    }

    [Test]
    public void Validate_ReportsDuplicateRegisteredAliases()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Unit("meter", "meter", "meters", "m", new string[0], new StandardDimensions().Length());
        UnitDefinition foot = Unit("foot", "foot", "feet", "ft", new string[0], new StandardDimensions().Length());
        registry.AddUnit(meter);
        registry.AddUnit(foot);
        registry.AddAlias("meter", "distance_unit");
        registry.AddAlias("foot", "distance_unit");

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue issue = FindIssue(report, RegistryValidationIssueKind.DuplicateAlias);

        Assert.That(issue.GetSubjectId(), Is.EqualTo("foot"));
        Assert.That(issue.GetDetail(), Is.EqualTo("distance_unit"));
    }

    [Test]
    public void AddQuantityKind_RejectsIncompatibleUnitMappings()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(new StandardUnits().Meter());
        registry.AddUnit(new StandardUnits().Second());
        QuantityKindDefinition quantityKind = new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "meter",
            new string[0],
            new string[] { "second" });

        OperationResult result = registry.AddQuantityKind(quantityKind);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(result.GetMessage(), Does.Contain("second"));
        Assert.That(registry.GetQuantityKinds(), Is.Empty);
    }

    [Test]
    public void RejectedQuantityKind_DoesNotHideIndependentValidationIssues()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(Unit("meter", "meter", "meters", "m", new string[] { "shared_alias" }, new StandardDimensions().Length()));
        registry.AddUnit(Unit("second", "second", "seconds", "s", new string[] { "shared_alias" }, new StandardDimensions().Time()));
        OperationResult result = registry.AddQuantityKind(new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "missing_meter",
            new string[0],
            new string[] { "second" }));

        RegistryValidationReport report = registry.Validate();

        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.RegistryInvalidMetadata));
        Assert.That(report.IsValid(), Is.False);
        Assert.That(report.GetIssueCount(), Is.EqualTo(1));
        Assert.That(report.GetIssues()[0].GetKind(), Is.EqualTo(RegistryValidationIssueKind.DuplicateAlias));
        Assert.That(report.GetIssues()[0].GetDetail(), Is.EqualTo("shared_alias"));
    }

    [Test]
    public void Validate_ReportsAliasSharedBetweenDefinitionAndRegisteredAlias()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = Unit("meter", "meter", "meters", "m", new string[] { "metre" }, new StandardDimensions().Length());
        UnitDefinition foot = Unit("foot", "foot", "feet", "ft", new string[0], new StandardDimensions().Length());
        registry.AddUnit(meter);
        registry.AddUnit(foot);
        registry.AddAlias("foot", "metre");

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue issue = FindIssue(report, RegistryValidationIssueKind.DuplicateAlias);

        Assert.That(issue.GetSubjectId(), Is.EqualTo("foot"));
        Assert.That(issue.GetDetail(), Is.EqualTo("metre"));
    }

    [Test]
    public void Validate_ReportsMissingAcceptedUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(new StandardUnits().Meter());
        registry._quantityKinds.Add(new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "meter",
            new string[0],
            new string[] { "foot" }));

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue issue = FindIssue(report, RegistryValidationIssueKind.MissingAcceptedUnit);

        Assert.That(issue.GetSubjectId(), Is.EqualTo("length"));
        Assert.That(issue.GetDetail(), Is.EqualTo("foot"));
    }

    [Test]
    public void Validate_ReportsUnresolvedAndIncompatibleSystemMappings()
    {
        UnitRegistry registry = CreateConsistentRegistry();
        registry._systems.Add(new UnitSystemDefinition(
            "broken_system",
            "Broken system",
            new string[] { "foot" },
            new string[] { "length", "mass", "time" },
            new string[] { "missing_meter", "meter", "meter" }));

        RegistryValidationReport report = registry.Validate();

        Assert.That(report.ContainsKind(RegistryValidationIssueKind.MissingSystemBaseUnit), Is.True);
        Assert.That(report.ContainsKind(RegistryValidationIssueKind.MissingPreferredQuantityKind), Is.True);
        Assert.That(report.ContainsKind(RegistryValidationIssueKind.MissingPreferredUnit), Is.True);
        Assert.That(report.ContainsKind(RegistryValidationIssueKind.IncompatiblePreferredUnitMapping), Is.True);
        Assert.That(FindIssue(report, RegistryValidationIssueKind.MissingSystemBaseUnit).GetDetail(), Is.EqualTo("foot"));
        Assert.That(FindIssue(report, RegistryValidationIssueKind.IncompatiblePreferredUnitMapping).GetDetail(), Is.EqualTo("meter"));
    }

    [Test]
    public void Validate_ReportsDuplicateMetadataIdsAndAliasesWithinCatalogs()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(new StandardUnits().Meter());
        registry._quantityKinds.Add(new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "meter",
            new string[] { "shared_kind" },
            new string[0]));
        registry._quantityKinds.Add(new QuantityKindDefinition(
            "length",
            "Other length",
            new StandardDimensions().Length(),
            "meter",
            new string[] { "shared_kind" },
            new string[0]));

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue idIssue = FindIssue(report, RegistryValidationIssueKind.DuplicateMetadataId);
        RegistryValidationIssue aliasIssue = FindIssue(report, RegistryValidationIssueKind.DuplicateMetadataAlias);

        Assert.That(idIssue.GetSubjectId(), Is.EqualTo("length"));
        Assert.That(idIssue.GetDetail(), Is.EqualTo("quantity kind"));
        Assert.That(aliasIssue.GetSubjectId(), Is.EqualTo("length"));
        Assert.That(aliasIssue.GetDetail(), Is.EqualTo("shared_kind"));
    }

    [Test]
    public void Validate_ReportsInvalidEquivalencyReferencesDimensionsAndFormulas()
    {
        UnitRegistry registry = CreateConsistentRegistry();
        registry.AddEquivalency(new EquivalencyDefinition(
            "invalid_equivalency",
            "Invalid equivalency",
            "",
            "missing_length",
            new StandardDimensions().Mass(),
            "time",
            new StandardDimensions().Time(),
            new RationalNumber(2, 1),
            1,
            new RationalNumber(2, 1),
            1,
            new string[0]));

        RegistryValidationReport report = registry.Validate();

        Assert.That(report.ContainsKind(RegistryValidationIssueKind.InvalidEquivalency), Is.True);
        Assert.That(CountIssues(report, RegistryValidationIssueKind.InvalidEquivalency), Is.EqualTo(2));
        Assert.That(FindIssue(report, RegistryValidationIssueKind.InvalidEquivalency).GetDetail(), Is.EqualTo("missing_length"));
    }

    [Test]
    public void Validate_ReportsSpecialUnitsWithUnusableConversionMetadata()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(new UnitDefinition(
            "special_length",
            "special length",
            "special lengths",
            "sl",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(2, 1),
            new RationalNumber(1, 1),
            true,
            UnitConversionKind.Special));

        RegistryValidationReport report = registry.Validate();
        RegistryValidationIssue issue = FindIssue(report, RegistryValidationIssueKind.InvalidSpecialConversionMetadata);

        Assert.That(issue.GetSubjectId(), Is.EqualTo("special_length"));
        Assert.That(issue.GetDetail(), Is.EqualTo("conversion"));
    }

    [Test]
    public void Validate_ReportsPhysicalConstantsWithIncompleteOrIncompatibleMetadata()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = new StandardUnits().Meter();
        registry.AddUnit(meter);
        PhysicalConstantDefinition unresolved = Constant("unresolved", new StandardUnits().Foot());
        PhysicalConstantDefinition incompatible = Constant("incompatible", Unit("meter", "fake meter", "fake meters", "fm", new string[0], new StandardDimensions().Time()));
        PhysicalConstantDefinition incomplete = Constant("incomplete", meter);
        incomplete._sourceLabel = "";
        registry.AddConstant(unresolved);
        registry.AddConstant(incompatible);
        registry.AddConstant(incomplete);

        RegistryValidationReport report = registry.Validate();

        Assert.That(report.ContainsKind(RegistryValidationIssueKind.InvalidPhysicalConstantMetadata), Is.True);
        Assert.That(CountIssues(report, RegistryValidationIssueKind.InvalidPhysicalConstantMetadata), Is.EqualTo(3));
    }

    [Test]
    public void ValidationReport_CopiesIssueArrays()
    {
        RegistryValidationIssue issue = new RegistryValidationIssue(
            RegistryValidationIssueKind.MissingDimension,
            "meter",
            "",
            "Unit is missing a dimension.");
        RegistryValidationReport report = new RegistryValidationReport(new RegistryValidationIssue[] { issue });

        RegistryValidationIssue[] returned = report.GetIssues();
        returned[0] = new RegistryValidationIssue(
            RegistryValidationIssueKind.DuplicateAlias,
            "foot",
            "alias",
            "Alias is duplicated.");

        Assert.That(report.HasIssues(), Is.True);
        Assert.That(report.GetIssueCount(), Is.EqualTo(1));
        Assert.That(report.ContainsKind(RegistryValidationIssueKind.MissingDimension), Is.True);
        Assert.That(report.ContainsKind(RegistryValidationIssueKind.DuplicateAlias), Is.False);
        Assert.That(report.GetIssues()[0], Is.SameAs(issue));
    }

    [Test]
    public void ValidationIssueAndReport_RejectInvalidInputs()
    {
        RegistryValidationIssue issue = new RegistryValidationIssue(
            RegistryValidationIssueKind.MissingDefaultUnit,
            "length",
            null,
            null);

        Assert.That(issue.GetDetail(), Is.EqualTo(""));
        Assert.That(issue.GetMessage(), Is.EqualTo(""));
        Assert.Throws<System.ArgumentException>(() => new RegistryValidationIssue((RegistryValidationIssueKind)999, "id", "", ""));
        Assert.Throws<System.ArgumentException>(() => new RegistryValidationIssue(RegistryValidationIssueKind.MissingDimension, "", "", ""));
        Assert.Throws<System.ArgumentNullException>(() => new RegistryValidationReport(null!));
        Assert.Throws<System.ArgumentNullException>(() => new RegistryValidationReport(new RegistryValidationIssue[] { null! }));
    }

    static RegistryValidationIssue FindIssue(RegistryValidationReport report, RegistryValidationIssueKind kind)
    {
        RegistryValidationIssue[] issues = report.GetIssues();
        int index = 0;

        while (index < issues.Length)
        {
            if (issues[index].IsKind(kind))
            {
                return issues[index];
            }

            index = index + 1;
        }

        throw new System.InvalidOperationException("Issue was not found.");
    }

    static int CountIssues(RegistryValidationReport report, RegistryValidationIssueKind kind)
    {
        RegistryValidationIssue[] issues = report.GetIssues();
        int count = 0;
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].IsKind(kind))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static PhysicalConstantDefinition Constant(string id, UnitDefinition unit)
    {
        return new PhysicalConstantDefinition(
            id,
            id,
            id,
            "",
            "exact",
            "test source",
            "test edition",
            0.0d,
            new QuantityValue(1.0d, unit),
            new string[0]);
    }

    static UnitDefinition Unit(string id, string name, string pluralName, string symbol, string[] aliases, DimensionVector dimension)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            aliases,
            dimension,
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static UnitRegistry CreateConsistentRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(new StandardUnits().Meter());
        registry.AddUnit(new StandardUnits().Second());
        registry.AddQuantityKind(new QuantityKindDefinition(
            "length",
            "Length",
            new StandardDimensions().Length(),
            "meter",
            new string[0],
            new string[0]));
        registry.AddQuantityKind(new QuantityKindDefinition(
            "time",
            "Time",
            new StandardDimensions().Time(),
            "second",
            new string[0],
            new string[0]));
        return registry;
    }
}
