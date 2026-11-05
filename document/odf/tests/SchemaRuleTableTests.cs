namespace odf.Tests;

public class SchemaRuleTableTests
{
    [Test]
    public void CatalogContainsEveryBundledGrammarThroughVersion14()
    {
        SchemaRuleSet[] version10 = OdfSchemaRuleCatalog.GetVersionRuleSets(
            OdfVersion.Version10);
        SchemaRuleSet[] version11 = OdfSchemaRuleCatalog.GetVersionRuleSets(
            OdfVersion.Version11);
        SchemaRuleSet[] version12 = OdfSchemaRuleCatalog.GetVersionRuleSets(
            OdfVersion.Version12);
        SchemaRuleSet[] version13 = OdfSchemaRuleCatalog.GetVersionRuleSets(
            OdfVersion.Version13);
        SchemaRuleSet[] version14 = OdfSchemaRuleCatalog.GetVersionRuleSets(
            OdfVersion.Version14);

        Assert.Multiple(() =>
        {
            Assert.That(Profiles(version10), Is.EqualTo(new[] { "document", "manifest" }));
            Assert.That(Profiles(version11), Is.EqualTo(new[] { "document", "strict", "manifest" }));
            Assert.That(Profiles(version12), Is.EqualTo(new[] { "document", "manifest", "signature" }));
            Assert.That(Profiles(version13), Is.EqualTo(new[] { "document", "manifest", "signature" }));
            Assert.That(Profiles(version14), Is.EqualTo(new[] { "document", "manifest", "signature" }));
            Assert.That(version10[0].Version, Is.EqualTo(OdfVersion.Version10));
            Assert.That(version11[0].Version, Is.EqualTo(OdfVersion.Version11));
            Assert.That(version12[0].Version, Is.EqualTo(OdfVersion.Version12));
            Assert.That(version13[0].Version, Is.EqualTo(OdfVersion.Version13));
            Assert.That(version14[0].Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionRuleSets(OdfVersion.Future),
                Is.Empty);
            Assert.That(
                OdfSchemaRuleCatalog.GetRuleSet(OdfVersion.Version12, "missing"),
                Is.Null);
        });
    }

    [Test]
    public void DocumentTablesRetainAllRuleCategories()
    {
        SchemaRuleSet version10 = RequiredRules(OdfVersion.Version10, "document");
        SchemaRuleSet version11 = RequiredRules(OdfVersion.Version11, "document");
        SchemaRuleSet version12 = RequiredRules(OdfVersion.Version12, "document");

        Assert.Multiple(() =>
        {
            AssertRuleCounts(version10, 2003, 11036, 534, 1600, 1701, 1290);
            AssertRuleCounts(version11, 2015, 11188, 536, 1608, 1754, 1292);
            AssertRuleCounts(version12, 1093, 11686, 599, 1792, 1998, 1416);
            Assert.That(version10.PatternChildIndexes, Has.Length.EqualTo(9033));
            Assert.That(version11.PatternChildIndexes, Has.Length.EqualTo(9173));
            Assert.That(version12.PatternChildIndexes, Has.Length.EqualTo(10593));
        });
    }

    [Test]
    public void ManifestAndSignatureTablesRetainTheirCompleteGrammarGraphs()
    {
        SchemaRuleSet manifest10 = RequiredRules(OdfVersion.Version10, "manifest");
        SchemaRuleSet manifest11 = RequiredRules(OdfVersion.Version11, "manifest");
        SchemaRuleSet manifest12 = RequiredRules(OdfVersion.Version12, "manifest");
        SchemaRuleSet signature12 = RequiredRules(OdfVersion.Version12, "signature");

        Assert.Multiple(() =>
        {
            AssertRuleCounts(manifest10, 16, 56, 5, 10, 3, 10);
            AssertRuleCounts(manifest11, 16, 56, 5, 10, 3, 10);
            AssertRuleCounts(manifest12, 20, 115, 7, 17, 10, 13);
            AssertRuleCounts(signature12, 5, 23, 3, 2, 2, 1);
            Assert.That(
                new SchemaRuleIndex(signature12).FindElementRules(
                    "http://www.w3.org/2000/09/xmldsig#",
                    "Signature"),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void IndexFindsNamespaceExpandedElementsAndAttributes()
    {
        SchemaRuleSet rules = RequiredRules(OdfVersion.Version12, "document");
        SchemaRuleIndex index = new SchemaRuleIndex(rules);
        SchemaRuleIndex legacyIndex = new SchemaRuleIndex(
            RequiredRules(OdfVersion.Version11, "document"));

        SchemaElementRule[] documents = index.FindElementRules(
            OdfNamespaces.Office,
            "document");
        SchemaElementRule[] tables = index.FindElementRules(
            OdfNamespaces.Table,
            "table");
        SchemaAttributeRule[] versions = index.FindAttributeRules(
            OdfNamespaces.Office,
            "version");
        SchemaRule[] paragraphContent = legacyIndex.FindDefinitionRules(
            "paragraph-content");

        Assert.Multiple(() =>
        {
            Assert.That(documents, Has.Length.EqualTo(1));
            Assert.That(documents[0].ContentPatternIndexes, Is.Not.Empty);
            Assert.That(tables, Is.Not.Empty);
            Assert.That(versions, Is.Not.Empty);
            Assert.That(versions[0].ValuePatternIndexes, Is.Not.Empty);
            Assert.That(paragraphContent.Length, Is.GreaterThan(20));
            Assert.That(index.FindStartRules(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void StrictRulesPreserveTheirBaseIncludeAndOverrides()
    {
        SchemaRuleSet strict = RequiredRules(OdfVersion.Version11, "strict");
        SchemaRuleIndex index = new SchemaRuleIndex(strict);
        SchemaRule[] includeRules = index.FindDefinitionRules(
            "#include:OpenDocument-schema-v1.1.rng");
        SchemaRule[] overrides = index.FindDefinitionRules(
            "office-meta-content");

        SchemaPattern includeGroup = index.PatternAt(includeRules[0].PatternIndex);
        SchemaPattern include = index.PatternAt(
            index.ChildPatterns(includeGroup)[0]);
        SchemaPattern overrideGroup = index.PatternAt(overrides[0].PatternIndex);
        SchemaPattern overrideReference = index.PatternAt(
            index.ChildPatterns(overrideGroup)[0]);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Rules, Has.Length.EqualTo(16));
            Assert.That(include.Kind, Is.EqualTo(SchemaPatternKind.Include));
            Assert.That(include.Value, Is.EqualTo("OpenDocument-schema-v1.1.rng"));
            Assert.That(overrideReference.Kind, Is.EqualTo(SchemaPatternKind.Ref));
            Assert.That(overrideReference.Name, Is.EqualTo("office-meta-content-strict"));
        });
    }

    [Test]
    public void DatatypeTablesPreserveLibrariesValuesAndParameters()
    {
        SchemaRuleSet rules = RequiredRules(OdfVersion.Version12, "document");
        int pointsOwner = RuleIndex(rules, "points");
        SchemaDatatype patternDatatype = FindParameterizedDatatype(
            rules,
            pointsOwner,
            "pattern");
        SchemaDatatype valueDatatype = FindValueDatatype(rules, "horizontal");

        Assert.Multiple(() =>
        {
            Assert.That(
                patternDatatype.Library,
                Is.EqualTo("http://www.w3.org/2001/XMLSchema-datatypes"));
            Assert.That(patternDatatype.Name, Is.EqualTo("string"));
            Assert.That(patternDatatype.ParameterValues[0], Does.Contain("[0-9]+"));
            Assert.That(valueDatatype.IsValue, Is.True);
            Assert.That(valueDatatype.Name, Is.EqualTo("token"));
            Assert.That(valueDatatype.Value, Is.EqualTo("horizontal"));
        });
    }

    [Test]
    public void OccurrenceTablesPreserveRequiredOptionalAndUnboundedBounds()
    {
        SchemaRuleSet manifest = RequiredRules(OdfVersion.Version12, "manifest");
        int manifestOwner = RuleIndex(manifest, "manifest");
        SchemaOccurrenceRule oneOrMore = FindOccurrence(
            manifest,
            manifestOwner,
            1,
            -1);
        SchemaOccurrenceRule optional = FindOccurrence(
            manifest,
            RuleIndex(manifest, "file-entry"),
            0,
            1);
        SchemaOccurrenceRule zeroOrMore = FindOccurrence(
            manifest,
            RuleIndex(manifest, "anyElements"),
            0,
            -1);

        Assert.Multiple(() =>
        {
            Assert.That(oneOrMore.PatternIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(optional.PatternIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(zeroOrMore.PatternIndex, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void RuleIndexRejectsInvalidPatternRequests()
    {
        SchemaRuleSet rules = RequiredRules(OdfVersion.Version10, "document");
        SchemaRuleIndex index = new SchemaRuleIndex(rules);

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfValidationException>(() => index.PatternAt(-1));
            Assert.Throws<OdfValidationException>(
                () => index.PatternAt(rules.Patterns.Length));
            Assert.Throws<OdfValidationException>(
                () => new SchemaRuleIndex(null!));
        });
    }

    static SchemaRuleSet RequiredRules(OdfVersion version, string profile)
    {
        SchemaRuleSet? result = OdfSchemaRuleCatalog.GetRuleSet(version, profile);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static string[] Profiles(SchemaRuleSet[] sets)
    {
        string[] result = new string[sets.Length];
        int index = 0;
        while (index < sets.Length)
        {
            result[index] = sets[index].Profile;
            index++;
        }
        return result;
    }

    static void AssertRuleCounts(
        SchemaRuleSet set,
        int rules,
        int patterns,
        int elements,
        int attributes,
        int occurrences,
        int datatypes)
    {
        Assert.Multiple(() =>
        {
            Assert.That(set.Rules, Has.Length.EqualTo(rules));
            Assert.That(set.Patterns, Has.Length.EqualTo(patterns));
            Assert.That(set.Elements, Has.Length.EqualTo(elements));
            Assert.That(set.Attributes, Has.Length.EqualTo(attributes));
            Assert.That(set.Occurrences, Has.Length.EqualTo(occurrences));
            Assert.That(set.Datatypes, Has.Length.EqualTo(datatypes));
        });
    }

    static int RuleIndex(SchemaRuleSet set, string name)
    {
        int index = 0;
        while (index < set.Rules.Length)
        {
            if (set.Rules[index].Name == name)
            {
                return index;
            }
            index++;
        }
        Assert.Fail("The required schema definition was not found: " + name);
        return -1;
    }

    static SchemaDatatype FindParameterizedDatatype(
        SchemaRuleSet set,
        int owner,
        string parameterName)
    {
        foreach (SchemaDatatype datatype in set.Datatypes)
        {
            SchemaPattern pattern = set.Patterns[datatype.PatternIndex];
            if (pattern.OwnerRuleIndex == owner
                && datatype.ParameterNames.Length > 0
                && datatype.ParameterNames[0] == parameterName)
            {
                return datatype;
            }
        }
        Assert.Fail("The required parameterized datatype was not found.");
        return new SchemaDatatype();
    }

    static SchemaDatatype FindValueDatatype(
        SchemaRuleSet set,
        string value)
    {
        foreach (SchemaDatatype datatype in set.Datatypes)
        {
            if (datatype.IsValue && datatype.Value == value)
            {
                return datatype;
            }
        }
        Assert.Fail("The required value datatype was not found.");
        return new SchemaDatatype();
    }

    static SchemaOccurrenceRule FindOccurrence(
        SchemaRuleSet set,
        int owner,
        int minimum,
        int maximum)
    {
        foreach (SchemaOccurrenceRule occurrence in set.Occurrences)
        {
            SchemaPattern pattern = set.Patterns[occurrence.PatternIndex];
            if (pattern.OwnerRuleIndex == owner
                && occurrence.Minimum == minimum
                && occurrence.Maximum == maximum)
            {
                return occurrence;
            }
        }
        Assert.Fail("The required occurrence rule was not found.");
        return new SchemaOccurrenceRule();
    }
}
