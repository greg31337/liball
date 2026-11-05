namespace odf.Tests;

public class Odf14RuleTableTests
{
    [Test]
    public void Version14TablesRetainEveryGrammarRuleCategory()
    {
        SchemaRuleSet document = RequiredRules("document");
        SchemaRuleSet manifest = RequiredRules("manifest");
        SchemaRuleSet signature = RequiredRules("signature");

        Assert.Multiple(() =>
        {
            AssertRuleCounts(document, 1107, 11910, 609, 1833, 2017, 1446, 10803);
            AssertRuleCounts(manifest, 21, 136, 15, 19, 13, 14, 115);
            AssertRuleCounts(signature, 5, 23, 3, 2, 2, 1, 18);
            Assert.That(document.SourceName, Is.EqualTo("OpenDocument-v1.4-schema.rng"));
            Assert.That(manifest.SourceName, Is.EqualTo("OpenDocument-v1.4-manifest-schema.rng"));
            Assert.That(signature.SourceName, Is.EqualTo("OpenDocument-v1.4-dsig-schema.rng"));
        });
    }

    [Test]
    public void Version14IndexExposesNewElementsAndAttributes()
    {
        SchemaRuleIndex index = new SchemaRuleIndex(RequiredRules("document"));

        Assert.Multiple(() =>
        {
            Assert.That(
                index.FindElementRules(OdfNamespaces.Number, "num-list-format"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Number, "num-list-label"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Style, "background-fill"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Drawing, "decorative"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Form, "input-required"),
                Is.Not.Empty);
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Style, "margin-gutter"),
                Is.Not.Empty);
        });
    }

    [Test]
    public void DeltaRecordsAllVersion14VocabularyAdditions()
    {
        SchemaVersionDelta delta = RequiredDelta();

        Assert.Multiple(() =>
        {
            Assert.That(
                ChangeCount(
                    delta,
                    "document",
                    SchemaRuleDeltaCategory.Element,
                    SchemaRuleDeltaKind.Added),
                Is.EqualTo(3));
            Assert.That(
                ChangeCount(
                    delta,
                    "document",
                    SchemaRuleDeltaCategory.Element,
                    SchemaRuleDeltaKind.Removed),
                Is.Zero);
            Assert.That(
                ChangeCount(
                    delta,
                    "document",
                    SchemaRuleDeltaCategory.Attribute,
                    SchemaRuleDeltaKind.Added),
                Is.EqualTo(14));
            Assert.That(
                ChangeCount(
                    delta,
                    "document",
                    SchemaRuleDeltaCategory.Attribute,
                    SchemaRuleDeltaKind.Removed),
                Is.Zero);
            Assert.That(
                ChangeCount(
                    delta,
                    "manifest",
                    SchemaRuleDeltaCategory.Element,
                    SchemaRuleDeltaKind.Added),
                Is.Zero);
        });
    }

    [Test]
    public void DeltaLinksAddedVocabularyToCurrentRuleIndexes()
    {
        SchemaVersionDelta delta = RequiredDelta();
        SchemaRuleDelta element = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Element,
            SchemaRuleDeltaKind.Added,
            OdfNamespaces.Number,
            "num-list-format");
        SchemaRuleDelta attribute = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Attribute,
            SchemaRuleDeltaKind.Added,
            OdfNamespaces.Drawing,
            "decorative");

        Assert.Multiple(() =>
        {
            Assert.That(delta.PreviousVersion, Is.EqualTo(OdfVersion.Version13));
            Assert.That(delta.CurrentVersion, Is.EqualTo(OdfVersion.Version14));
            Assert.That(element.PreviousIndexes, Is.Empty);
            Assert.That(element.CurrentIndexes, Is.Not.Empty);
            Assert.That(attribute.PreviousIndexes, Is.Empty);
            Assert.That(attribute.CurrentIndexes, Is.Not.Empty);
        });
    }

    [Test]
    public void DeltaRecordsAddedRemovedAndVersionedDefinitions()
    {
        SchemaVersionDelta delta = RequiredDelta();
        SchemaRuleDelta added = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Added,
            "",
            "style-background-fill");
        SchemaRuleDelta removed = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Removed,
            "",
            "common-ref-format-values");
        SchemaRuleDelta documentVersion = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Changed,
            "",
            "office-document-common-attrs");
        SchemaRuleDelta manifestVersion = RequiredChange(
            delta,
            "manifest",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Changed,
            "",
            "manifest-attlist");
        SchemaRuleDelta signatureVersion = RequiredChange(
            delta,
            "signature",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Changed,
            "",
            "dsig-document-signatures-attlist");

        Assert.Multiple(() =>
        {
            Assert.That(added.PreviousIndexes, Is.Empty);
            Assert.That(added.CurrentIndexes, Is.Not.Empty);
            Assert.That(removed.PreviousIndexes, Is.Not.Empty);
            Assert.That(removed.CurrentIndexes, Is.Empty);
            Assert.That(documentVersion.PreviousIndexes, Is.Not.Empty);
            Assert.That(documentVersion.CurrentIndexes, Is.Not.Empty);
            Assert.That(manifestVersion.CurrentIndexes, Is.Not.Empty);
            Assert.That(signatureVersion.CurrentIndexes, Is.Not.Empty);
        });
    }

    [Test]
    public void CatalogReturnsBothSupportedVersionTransitions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version13,
                    OdfVersion.Version14),
                Is.SameAs(Odf14Rules.DeltasFrom13));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version12,
                    OdfVersion.Version14),
                Is.Null);
        });
    }

    static SchemaRuleSet RequiredRules(string profile)
    {
        SchemaRuleSet? result = OdfSchemaRuleCatalog.GetRuleSet(
            OdfVersion.Version14,
            profile);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static SchemaVersionDelta RequiredDelta()
    {
        SchemaVersionDelta? result = OdfSchemaRuleCatalog.GetVersionDelta(
            OdfVersion.Version13,
            OdfVersion.Version14);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static SchemaRuleDelta RequiredChange(
        SchemaVersionDelta delta,
        string profile,
        SchemaRuleDeltaCategory category,
        SchemaRuleDeltaKind kind,
        string namespaceName,
        string name)
    {
        foreach (SchemaRuleDelta change in delta.Changes)
        {
            if (change.Profile == profile
                && change.Category == category
                && change.Kind == kind
                && change.NamespaceName == namespaceName
                && change.Name == name)
            {
                return change;
            }
        }
        Assert.Fail("The required schema delta was not found: " + name);
        return new SchemaRuleDelta();
    }

    static int ChangeCount(
        SchemaVersionDelta delta,
        string profile,
        SchemaRuleDeltaCategory category,
        SchemaRuleDeltaKind kind)
    {
        int result = 0;
        foreach (SchemaRuleDelta change in delta.Changes)
        {
            if (change.Profile == profile
                && change.Category == category
                && change.Kind == kind)
            {
                result++;
            }
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
        int datatypes,
        int children)
    {
        Assert.Multiple(() =>
        {
            Assert.That(set.Rules, Has.Length.EqualTo(rules));
            Assert.That(set.Patterns, Has.Length.EqualTo(patterns));
            Assert.That(set.Elements, Has.Length.EqualTo(elements));
            Assert.That(set.Attributes, Has.Length.EqualTo(attributes));
            Assert.That(set.Occurrences, Has.Length.EqualTo(occurrences));
            Assert.That(set.Datatypes, Has.Length.EqualTo(datatypes));
            Assert.That(set.PatternChildIndexes, Has.Length.EqualTo(children));
        });
    }
}
