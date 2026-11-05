namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class ValidationRuleCoverageCatalogTests
{
    [Test]
    public void CreateCoverageCatalog_MapsEveryKhronosIssueCategoryDeterministically()
    {
        ValidationRuleCoverageCatalog catalog = new ValidationRuleCoverageCatalog();

        List<ValidationRuleCoverage> first = catalog.CreateCoverageCatalog();
        List<ValidationRuleCoverage> second = catalog.CreateCoverageCatalog();
        HashSet<string> codes = new HashSet<string>();
        int index = 0;
        while (index < first.Count)
        {
            codes.Add(first[index].Code);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(170));
            Assert.That(codes, Has.Count.EqualTo(170));
            Assert.That(CountCategory(first, "IoError"), Is.EqualTo(1));
            Assert.That(CountCategory(first, "SchemaError"), Is.EqualTo(16));
            Assert.That(CountCategory(first, "SemanticError"), Is.EqualTo(53));
            Assert.That(CountCategory(first, "LinkError"), Is.EqualTo(51));
            Assert.That(CountCategory(first, "DataError"), Is.EqualTo(33));
            Assert.That(CountCategory(first, "GlbError"), Is.EqualTo(16));
            Assert.That(first[0].Code, Is.EqualTo(GltfIssueCodes.IoError));
            Assert.That(first[first.Count - 1].Code, Is.EqualTo(GltfIssueCodes.GlbUnknownChunkType));
            Assert.That(CoverageKeys(first), Is.EqualTo(CoverageKeys(second)));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
        });
    }

    [Test]
    public void CreateCoverageCatalog_MapsRulesToImplementationsOrTrackedExtensions()
    {
        ValidationRuleCoverageCatalog catalog = new ValidationRuleCoverageCatalog();
        List<ValidationRuleCoverage> entries = catalog.CreateCoverageCatalog();
        int invalidCount = 0;
        int index = 0;
        while (index < entries.Count)
        {
            ValidationRuleCoverage entry = entries[index];
            bool mapped = entry.Rule.Length > 0
                && (entry.Implemented
                    || (entry.ExtensionRule
                        && entry.TrackingTask != null
                        && entry.TrackingTask.Length > 0));
            if (!mapped)
            {
                invalidCount++;
            }

            index++;
        }

        ValidationRuleCoverage? pointer = catalog.FindCoverage(
            entries,
            GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetPath);
        ValidationRuleCoverage? light = catalog.FindCoverage(
            entries,
            GltfIssueCodes.KhrLightsPunctualLightSpotAngles);
        ValidationRuleCoverage? material = catalog.FindCoverage(
            entries,
            GltfIssueCodes.KhrMaterialsVolumeNoTransmission);

        Assert.Multiple(() =>
        {
            Assert.That(invalidCount, Is.Zero);
            Assert.That(pointer, Is.Not.Null);
            Assert.That(pointer!.Implemented, Is.True);
            Assert.That(pointer.ExtensionRule, Is.True);
            Assert.That(pointer.Rule, Is.EqualTo("AnimationValidator"));
            Assert.That(light, Is.Not.Null);
            Assert.That(light!.Implemented, Is.True);
            Assert.That(light.Rule, Is.EqualTo("SceneExtensionValidator"));
            Assert.That(light.TrackingTask, Is.Null);
            Assert.That(material, Is.Not.Null);
            Assert.That(material!.Implemented, Is.True);
            Assert.That(material.Rule, Is.EqualTo("MaterialExtensionValidator"));
            Assert.That(material.TrackingTask, Is.Null);
        });
    }

    [Test]
    public void FindCoverage_ReturnsNullForNonCatalogCodeAndRejectsNulls()
    {
        ValidationRuleCoverageCatalog catalog = new ValidationRuleCoverageCatalog();
        List<ValidationRuleCoverage> entries = catalog.CreateCoverageCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.FindCoverage(entries, "PROJECT_ONLY_CODE"), Is.Null);
            Assert.That(
                () => catalog.FindCoverage(null!, "CODE"),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("entries"));
            Assert.That(
                () => catalog.FindCoverage(entries, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("code"));
        });
    }

    private static int CountCategory(
        List<ValidationRuleCoverage> entries,
        string category)
    {
        int result = 0;
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Category == category)
            {
                result++;
            }

            index++;
        }

        return result;
    }

    private static string[] CoverageKeys(List<ValidationRuleCoverage> entries)
    {
        string[] result = new string[entries.Count];
        int index = 0;
        while (index < entries.Count)
        {
            ValidationRuleCoverage entry = entries[index];
            result[index] = entry.Category + "|" + entry.Code;
            index++;
        }

        return result;
    }
}
