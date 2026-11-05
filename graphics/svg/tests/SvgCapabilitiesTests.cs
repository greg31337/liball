namespace svg.Tests;

public class SvgCapabilitiesTests
{
    [Test]
    public void CapabilitySetUsesOrdinalUniquenessAndSortedSnapshots()
    {
        SvgCapabilitySet capabilities = SvgCapabilitySet.Create(
        [
            "host.feature.z",
            "host.feature.A",
            "host.feature.a",
            "host.feature.z"
        ]);

        bool duplicateAdded = capabilities.Add("host.feature.a");
        bool newAdded = capabilities.Add("host.feature.m");
        List<string> snapshot = capabilities.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(duplicateAdded, Is.False);
            Assert.That(newAdded, Is.True);
            Assert.That(capabilities.Contains("host.feature.A"), Is.True);
            Assert.That(capabilities.Contains("host.feature.a"), Is.True);
            Assert.That(capabilities.Contains("HOST.FEATURE.A"), Is.False);
            Assert.That(snapshot, Is.EqualTo(new[]
            {
                "host.feature.A",
                "host.feature.a",
                "host.feature.m",
                "host.feature.z"
            }));
        });
    }

    [Test]
    public void CapabilityCloneAndSnapshotAreIndependent()
    {
        SvgCapabilitySet original = SvgCapabilitySet.Create(["b", "a"]);
        SvgCapabilitySet clone = original.Clone();
        List<string> snapshot = original.Snapshot();

        clone.Add("c");
        snapshot.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(original.Contains("c"), Is.False);
            Assert.That(original.Snapshot(), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(clone.Snapshot(), Is.EqualTo(new[] { "a", "b", "c" }));
        });
    }

    [Test]
    public void CapabilityRemoveCleansDuplicatePublicEntries()
    {
        SvgCapabilitySet capabilities = SvgCapabilitySet.Create(["a", "b"]);
        capabilities.Identifiers.Add("a");
        capabilities.Identifiers.Add(null);

        bool removed = capabilities.Remove("a");

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(capabilities.Contains("a"), Is.False);
            Assert.That(capabilities.Snapshot(), Is.EqualTo(new[] { "b" }));
            Assert.That(capabilities.Remove("missing"), Is.False);
        });
    }

    [Test]
    public void FeatureReportRepresentsEveryStatusAndFiltersDeterministically()
    {
        SvgFeatureReport report = new SvgFeatureReport();
        report.Add(Entry("svg.feature.z", SvgFeatureStatus.Unsupported, SvgFeatureIdentifiers.ProfileSvg2));
        report.Add(Entry("svg.feature.c", SvgFeatureStatus.PreserveOnly, SvgFeatureIdentifiers.ProfileLegacyPreservation));
        report.Add(Entry("svg.feature.a", SvgFeatureStatus.Implemented, SvgFeatureIdentifiers.ProfileSvg2));
        report.Add(Entry("svg.feature.d", SvgFeatureStatus.PolicyDisabled, SvgFeatureIdentifiers.ProfileSvg2));
        report.Add(Entry("svg.feature.b", SvgFeatureStatus.CallbackRequired, SvgFeatureIdentifiers.ProfileHostCallback));

        List<SvgFeatureEntry> snapshot = report.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "svg.feature.a",
                "svg.feature.b",
                "svg.feature.c",
                "svg.feature.d",
                "svg.feature.z"
            }));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.Implemented).Single().Identifier, Is.EqualTo("svg.feature.a"));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.CallbackRequired).Single().Identifier, Is.EqualTo("svg.feature.b"));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.PreserveOnly).Single().Identifier, Is.EqualTo("svg.feature.c"));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.PolicyDisabled).Single().Identifier, Is.EqualTo("svg.feature.d"));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.Unsupported).Single().Identifier, Is.EqualTo("svg.feature.z"));
            Assert.That(
                report.FilterByProfile(SvgFeatureIdentifiers.ProfileSvg2).Select(item => item.Identifier),
                Is.EqualTo(new[] { "svg.feature.a", "svg.feature.d", "svg.feature.z" }));
        });
    }

    [Test]
    public void FeatureReportUsesSourceOrderForEquivalentFeatureEntries()
    {
        SvgSourceUri source = new SvgSourceUri("document.svg");
        SvgSourceSpan late = new SvgSourceSpan(source, 20, 2, 1, 21, 2, 2);
        SvgSourceSpan early = new SvgSourceSpan(source, 3, 1, 4, 4, 1, 5);
        SvgFeatureReport report = new SvgFeatureReport();
        report.Add(new SvgFeatureEntry(
            "svg.feature.same",
            SvgFeatureIdentifiers.ProfileSvg2,
            SvgFeatureStatus.Unsupported,
            "late",
            late));
        report.Add(new SvgFeatureEntry(
            "svg.feature.same",
            SvgFeatureIdentifiers.ProfileSvg2,
            SvgFeatureStatus.Unsupported,
            "early",
            early));

        List<SvgFeatureEntry> snapshot = report.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Select(item => item.Detail), Is.EqualTo(new[] { "early", "late" }));
            Assert.That(report.Find("svg.feature.same"), Is.SameAs(snapshot[0]));
        });
    }

    [Test]
    public void FeatureReportSnapshotsAndDiagnosticsAreIndependentCollections()
    {
        SvgFeatureReport report = new SvgFeatureReport();
        report.Add(Entry("svg.feature.a", SvgFeatureStatus.Implemented, SvgFeatureIdentifiers.ProfileSvg2));
        SvgDiagnostic diagnostic = new SvgDiagnostic(
            SvgDiagnosticSeverity.Warning,
            SvgDiagnosticCode.UnsupportedFeature,
            "Feature warning.",
            null);
        report.AddDiagnostic(diagnostic);
        report.Entries.Add(null);
        List<SvgFeatureEntry> snapshot = report.Snapshot();

        Assert.That(snapshot, Has.Count.EqualTo(1));
        snapshot.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(report.Entries, Has.Count.EqualTo(2));
            Assert.That(report.Diagnostics, Is.EqualTo(new[] { diagnostic }));
            Assert.That(report.Find("missing"), Is.Null);
            Assert.That(report.Find(null), Is.Null);
            Assert.That(report.FilterByProfile(null), Is.Empty);
        });
    }

    [Test]
    public void PublishedProfilesMatchTheDocumentedContract()
    {
        Assert.That(new SvgCapabilities().GetProfileIdentifiers(), Is.EqualTo(new[]
        {
            "svg.profile.svg2",
            "svg.profile.svg11.compat",
            "svg.profile.legacy.preserve",
            "svg.profile.host.callback"
        }));
    }

    [Test]
    public void LibraryReportIsCompleteDeterministicAndHonestForCurrentBuild()
    {
        SvgFeatureReport report = new SvgCapabilities().GetLibraryReport();
        List<SvgFeatureEntry> entries = report.Snapshot();
        List<string> identifiers = new SvgCapabilities().GetFeatureIdentifiers();

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(item => item.Identifier), Is.EqualTo(identifiers));
            Assert.That(identifiers, Is.EqualTo(identifiers.OrderBy(item => item, StringComparer.Ordinal)));
            Assert.That(identifiers.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(identifiers.Count));
            Assert.That(identifiers, Has.All.StartsWith("svg.feature."));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.SourceMapping).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.Diagnostics).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.SecurityLimits).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.CapabilityReporting).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.DocumentMutation).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.Loading).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(new SvgCapabilities().FindLibraryFeature(SvgFeatureIdentifiers.HostImageCodec).Status, Is.EqualTo(SvgFeatureStatus.CallbackRequired));
            Assert.That(new SvgCapabilities().FindLibraryFeature("svg.feature.unknown"), Is.Null);
        });
    }

    [Test]
    public void LibraryReportAssignsEveryPublishedFeatureItsReleaseStatus()
    {
        Dictionary<string, SvgFeatureStatus> expected = new Dictionary<string, SvgFeatureStatus>
        {
            [SvgFeatureIdentifiers.SourceMapping] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Diagnostics] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.SecurityLimits] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.CapabilityReporting] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Loading] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.EditableDocument] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.DocumentMutation] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.CssParsing] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.CssSelectors] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.CssCascade] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Shapes] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Paths] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.PathProcessing] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.PaintServers] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Markers] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Clipping] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Masking] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Filters] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Images] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.PngCodec] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.HostImageCodec] = SvgFeatureStatus.CallbackRequired,
            [SvgFeatureIdentifiers.TextLayout] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.OpenType] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.HostTextShaping] = SvgFeatureStatus.CallbackRequired,
            [SvgFeatureIdentifiers.SmilAnimation] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.CssAnimation] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Interaction] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Accessibility] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Scene] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Rasterization] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.ForeignContent] = SvgFeatureStatus.CallbackRequired,
            [SvgFeatureIdentifiers.ExternalResources] = SvgFeatureStatus.CallbackRequired,
            [SvgFeatureIdentifiers.Validation] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Sanitization] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Processing] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Generation] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.Writing] = SvgFeatureStatus.Implemented,
            [SvgFeatureIdentifiers.ScriptPreservation] = SvgFeatureStatus.PreserveOnly,
            [SvgFeatureIdentifiers.ScriptExecution] = SvgFeatureStatus.Unsupported,
            [SvgFeatureIdentifiers.LegacyPreservation] = SvgFeatureStatus.PreserveOnly
        };
        SvgFeatureReport report = new SvgCapabilities().GetLibraryReport();
        List<SvgFeatureEntry> entries = report.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.GreaterThan(expected.Count));
            Assert.That(expected.All(pair => report.Find(pair.Key) != null && report.Find(pair.Key).Status == pair.Value), Is.True);
            Assert.That(entries.Count(entry => expected.ContainsKey(entry.Identifier)), Is.EqualTo(expected.Count));
            Assert.That(report.FilterByStatus(SvgFeatureStatus.PolicyDisabled), Is.Empty);
            Assert.That(report.FilterByStatus(SvgFeatureStatus.Unsupported).Select(entry => entry.Identifier), Does.Contain(SvgFeatureIdentifiers.ScriptExecution));
        });
    }

    [Test]
    public void GranularInventoryPublishesEveryFeatureFamilyWithStableIdentifiers()
    {
        SvgCapabilities capabilities = new SvgCapabilities();
        SvgFeatureReport report = capabilities.GetLibraryReport();
        string element = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.Element, "foreignObject");
        string property = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssProperty, "fill-opacity");
        string atRule = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssAtRule, "font-face");
        string selector = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.CssSelector, "pseudo-hover");
        string filter = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.FilterPrimitive, "feGaussianBlur");
        string animation = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.AnimationValue, "number");
        string image = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.ImageFormat, "jpeg");
        string font = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.FontFormat, "woff2");
        string job = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.ProcessorJob, "convert-text-to-paths");
        string callback = capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.HostCallback, "foreign-content");

        Assert.Multiple(() =>
        {
            Assert.That(element, Is.EqualTo(SvgFeatureIdentifiers.ElementPrefix + "foreignobject"));
            Assert.That(report.Find(element).Status, Is.EqualTo(SvgFeatureStatus.CallbackRequired));
            Assert.That(report.Find(property).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(atRule).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(selector).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(filter).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(animation).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(image).Status, Is.EqualTo(SvgFeatureStatus.CallbackRequired));
            Assert.That(report.Find(font).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(report.Find(job).Status, Is.EqualTo(SvgFeatureStatus.CallbackRequired));
            Assert.That(report.Find(callback).Status, Is.EqualTo(SvgFeatureStatus.CallbackRequired));
            Assert.That(report.Entries.Count(entry => entry.Identifier.StartsWith(SvgFeatureIdentifiers.ElementPrefix, StringComparison.Ordinal)), Is.EqualTo(new SvgCatalogFactory().CreateElementCatalog().GetEntries().Count + new SvgCatalogFactory().CreateLegacyCatalog().GetElementEntries().Count));
            Assert.That(report.Entries.Count(entry => entry.Identifier.StartsWith(SvgFeatureIdentifiers.CssPropertyPrefix, StringComparison.Ordinal)), Is.EqualTo(SvgPropertyCatalog.CreateDefault().Entries.Count + 2));
            Assert.That(report.Entries.Count(entry => entry.Identifier.StartsWith(SvgFeatureIdentifiers.ProcessorJobPrefix, StringComparison.Ordinal)), Is.EqualTo(35));
            Assert.Throws<ArgumentOutOfRangeException>(() => capabilities.GetFeatureIdentifierWithFamilyAndName((SvgFeatureFamily)99, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => capabilities.GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily.Element, " "));
        });
    }

    [Test]
    public void LibraryCatalogCallsReturnIndependentReportsAndProfileLists()
    {
        SvgFeatureReport first = new SvgCapabilities().GetLibraryReport();
        SvgFeatureReport second = new SvgCapabilities().GetLibraryReport();
        List<string> firstProfiles = new SvgCapabilities().GetProfileIdentifiers();
        List<string> secondProfiles = new SvgCapabilities().GetProfileIdentifiers();

        first.Find(SvgFeatureIdentifiers.SourceMapping).Status = SvgFeatureStatus.Unsupported;
        first.Entries.Clear();
        firstProfiles.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(second.Find(SvgFeatureIdentifiers.SourceMapping).Status, Is.EqualTo(SvgFeatureStatus.Implemented));
            Assert.That(second.Entries, Is.Not.Empty);
            Assert.That(secondProfiles, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void CapabilityTypesRejectMalformedInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SvgCapabilitySet.Create(null));
            Assert.Throws<ArgumentNullException>(() => new SvgCapabilitySet().Add(null));
            Assert.Throws<ArgumentException>(() => new SvgCapabilitySet().Add(""));
            Assert.Throws<ArgumentNullException>(() => new SvgFeatureEntry(null, SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "", null));
            Assert.Throws<ArgumentException>(() => new SvgFeatureEntry("", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, "", null));
            Assert.Throws<ArgumentNullException>(() => new SvgFeatureEntry("svg.feature.a", null, SvgFeatureStatus.Unsupported, "", null));
            Assert.Throws<ArgumentException>(() => new SvgFeatureEntry("svg.feature.a", "", SvgFeatureStatus.Unsupported, "", null));
            Assert.Throws<ArgumentNullException>(() => new SvgFeatureEntry("svg.feature.a", SvgFeatureIdentifiers.ProfileSvg2, SvgFeatureStatus.Unsupported, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgFeatureEntry("svg.feature.a", SvgFeatureIdentifiers.ProfileSvg2, (SvgFeatureStatus)99, "", null));
            Assert.Throws<ArgumentNullException>(() => new SvgFeatureReport().Add(null));
            Assert.Throws<ArgumentNullException>(() => new SvgFeatureReport().AddDiagnostic(null));
        });
    }

    static SvgFeatureEntry Entry(string identifier, SvgFeatureStatus status, string profile)
    {
        return new SvgFeatureEntry(identifier, profile, status, status.ToString(), null);
    }
}
