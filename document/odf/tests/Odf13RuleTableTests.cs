namespace odf.Tests;

public class Odf13RuleTableTests
{
    [Test]
    public void Version13TablesRetainEveryGrammarRuleCategory()
    {
        SchemaRuleSet document = RequiredRules("document");
        SchemaRuleSet manifest = RequiredRules("manifest");
        SchemaRuleSet signature = RequiredRules("signature");

        Assert.Multiple(() =>
        {
            AssertRuleCounts(document, 1100, 11803, 606, 1807, 2026, 1422, 10703);
            AssertRuleCounts(manifest, 21, 136, 15, 19, 13, 14, 115);
            AssertRuleCounts(signature, 5, 23, 3, 2, 2, 1, 18);
            Assert.That(document.SourceName, Is.EqualTo("OpenDocument-v1.3-schema.rng"));
            Assert.That(manifest.SourceName, Is.EqualTo("OpenDocument-v1.3-manifest-schema.rng"));
            Assert.That(signature.SourceName, Is.EqualTo("OpenDocument-v1.3-dsig-schema.rng"));
        });
    }

    [Test]
    public void Version13IndexExposesNewDocumentVocabulary()
    {
        SchemaRuleIndex index = new SchemaRuleIndex(RequiredRules("document"));

        Assert.Multiple(() =>
        {
            Assert.That(
                index.FindElementRules(OdfNamespaces.Chart, "coordinate-region"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Text, "drop-down"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Style, "header-first"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Style, "contextual-spacing"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Table, "tab-color"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindDefinitionRules("chart-coordinate-region"),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void Version13ManifestIncludesPublicKeyEncryptionGrammar()
    {
        SchemaRuleIndex index = new SchemaRuleIndex(RequiredRules("manifest"));

        Assert.Multiple(() =>
        {
            Assert.That(
                index.FindElementRules(OdfNamespaces.Manifest, "encrypted-key"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Manifest, "PGPData"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindElementRules(OdfNamespaces.Manifest, "CipherValue"),
                Has.Length.EqualTo(1));
            Assert.That(
                index.FindAttributeRules(OdfNamespaces.Manifest, "PGPAlgorithm"),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void DeltaRecordsKnownAddedAndRemovedDocumentNames()
    {
        SchemaVersionDelta delta = RequiredDelta();
        SchemaRuleDelta addedElement = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Element,
            SchemaRuleDeltaKind.Added,
            OdfNamespaces.Chart,
            "coordinate-region");
        SchemaRuleDelta addedAttribute = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Attribute,
            SchemaRuleDeltaKind.Added,
            OdfNamespaces.Style,
            "contextual-spacing");
        SchemaRuleDelta removedAttribute = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Attribute,
            SchemaRuleDeltaKind.Removed,
            OdfNamespaces.Office,
            "process-content");

        Assert.Multiple(() =>
        {
            Assert.That(delta.PreviousVersion, Is.EqualTo(OdfVersion.Version12));
            Assert.That(delta.CurrentVersion, Is.EqualTo(OdfVersion.Version13));
            Assert.That(addedElement.PreviousIndexes, Is.Empty);
            Assert.That(addedElement.CurrentIndexes, Is.Not.Empty);
            Assert.That(addedAttribute.PreviousIndexes, Is.Empty);
            Assert.That(addedAttribute.CurrentIndexes, Is.Not.Empty);
            Assert.That(removedAttribute.PreviousIndexes, Is.Not.Empty);
            Assert.That(removedAttribute.CurrentIndexes, Is.Empty);
        });
    }

    [Test]
    public void DeltaRecordsManifestAndDefinitionChanges()
    {
        SchemaVersionDelta delta = RequiredDelta();
        SchemaRuleDelta manifestElement = RequiredChange(
            delta,
            "manifest",
            SchemaRuleDeltaCategory.Element,
            SchemaRuleDeltaKind.Added,
            OdfNamespaces.Manifest,
            "encrypted-key");
        SchemaRuleDelta definition = RequiredChange(
            delta,
            "document",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Added,
            "",
            "chart-coordinate-region");
        SchemaRuleDelta signatureVersion = RequiredChange(
            delta,
            "signature",
            SchemaRuleDeltaCategory.Definition,
            SchemaRuleDeltaKind.Changed,
            "",
            "dsig-document-signatures-attlist");

        Assert.Multiple(() =>
        {
            Assert.That(manifestElement.CurrentIndexes, Is.Not.Empty);
            Assert.That(definition.CurrentIndexes, Is.Not.Empty);
            Assert.That(signatureVersion.PreviousIndexes, Is.Not.Empty);
            Assert.That(signatureVersion.CurrentIndexes, Is.Not.Empty);
        });
    }

    [Test]
    public void DeltaContainsAllKnownDocumentVocabularyChanges()
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
                Is.EqualTo(7));
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
                Is.EqualTo(16));
            Assert.That(
                ChangeCount(
                    delta,
                    "document",
                    SchemaRuleDeltaCategory.Attribute,
                    SchemaRuleDeltaKind.Removed),
                Is.EqualTo(1));
            Assert.That(
                ChangeCount(
                    delta,
                    "manifest",
                    SchemaRuleDeltaCategory.Element,
                    SchemaRuleDeltaKind.Added),
                Is.EqualTo(8));
        });
    }

    [Test]
    public void CatalogOnlyReturnsTheSupportedVersionTransition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version12,
                    OdfVersion.Version13),
                Is.SameAs(Odf13Rules.DeltasFrom12));
            Assert.That(
                OdfSchemaRuleCatalog.GetVersionDelta(
                    OdfVersion.Version11,
                    OdfVersion.Version13),
                Is.Null);
        });
    }

    static SchemaRuleSet RequiredRules(string profile)
    {
        SchemaRuleSet? result = OdfSchemaRuleCatalog.GetRuleSet(
            OdfVersion.Version13,
            profile);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static SchemaVersionDelta RequiredDelta()
    {
        SchemaVersionDelta? result = OdfSchemaRuleCatalog.GetVersionDelta(
            OdfVersion.Version12,
            OdfVersion.Version13);
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
