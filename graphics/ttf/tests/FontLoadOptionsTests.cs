using ttf;

namespace ttf.Tests;

public class FontLoadOptionsTests
{
    [Test]
    public void PolicyEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FontLoadMode.Strict, Is.EqualTo(0));
            Assert.That((int)FontLoadMode.Compatible, Is.EqualTo(1));
            Assert.That((int)FontLoadMode.InspectOnly, Is.EqualTo(2));
            Assert.That((int)FontInputOwnership.Copy, Is.EqualTo(0));
            Assert.That((int)FontInputOwnership.RetainReadOnlyMemory, Is.EqualTo(1));
            Assert.That((int)FontRepairPolicy.None, Is.EqualTo(0));
            Assert.That((int)FontRepairPolicy.Safe, Is.EqualTo(1));
            Assert.That((int)FontChecksumPolicy.RequireValid, Is.EqualTo(0));
            Assert.That((int)FontChecksumPolicy.ReportMismatch, Is.EqualTo(1));
            Assert.That((int)FontChecksumPolicy.Ignore, Is.EqualTo(2));
        });
    }

    [Test]
    public void DefaultConstructorUsesCompatibleProfile()
    {
        FontLoadOptions options = FontLoadOptions.Create();

        AssertProfile(
            options,
            FontLoadMode.Compatible,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch);
        Assert.That(options.GetLimits().GetMaxFaces(), Is.EqualTo(256));
    }

    [Test]
    public void StrictFactoryUsesStrictPoliciesAndLimits()
    {
        FontLoadOptions options = FontLoadOptions.CreateStrict();

        AssertProfile(
            options,
            FontLoadMode.Strict,
            FontRepairPolicy.None,
            FontChecksumPolicy.RequireValid);
        Assert.That(options.GetLimits().GetMaxFaces(), Is.EqualTo(32));
    }

    [Test]
    public void CompatibleFactoryUsesCompatiblePoliciesAndLimits()
    {
        FontLoadOptions options = FontLoadOptions.CreateCompatible();

        AssertProfile(
            options,
            FontLoadMode.Compatible,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch);
        Assert.That(options.GetLimits().GetMaxFaces(), Is.EqualTo(256));
    }

    [Test]
    public void InspectOnlyFactoryUsesNonRepairingInspectionPoliciesAndLimits()
    {
        FontLoadOptions options = FontLoadOptions.CreateInspectOnly();

        AssertProfile(
            options,
            FontLoadMode.InspectOnly,
            FontRepairPolicy.None,
            FontChecksumPolicy.ReportMismatch);
        Assert.That(options.GetLimits().GetMaxFaces(), Is.EqualTo(1024));
    }

    [Test]
    public void ConfiguredFactoryPreservesEveryChoice()
    {
        FontLimits limits = FontLimits.CreateCompatible();
        CancellationTokenSource source = new CancellationTokenSource();
        FontLoadOptions options = FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.RetainReadOnlyMemory,
            false,
            FontRepairPolicy.None,
            FontChecksumPolicy.Ignore,
            limits,
            source.Token);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetMode(), Is.EqualTo(FontLoadMode.Compatible));
            Assert.That(options.GetInputOwnership(), Is.EqualTo(FontInputOwnership.RetainReadOnlyMemory));
            Assert.That(options.ShouldPreserveRawTables(), Is.False);
            Assert.That(options.GetRepairPolicy(), Is.EqualTo(FontRepairPolicy.None));
            Assert.That(options.GetChecksumPolicy(), Is.EqualTo(FontChecksumPolicy.Ignore));
            Assert.That(options.GetLimits(), Is.SameAs(limits));
            Assert.That(options.GetCancellationToken(), Is.EqualTo(source.Token));
        });
    }

    [Test]
    public void FactoryRetainsCancellationToken()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        FontLoadOptions strict = FontLoadOptions.CreateStrict(source.Token);
        FontLoadOptions compatible = FontLoadOptions.CreateCompatible(source.Token);
        FontLoadOptions inspectOnly = FontLoadOptions.CreateInspectOnly(source.Token);

        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(strict.GetCancellationToken().IsCancellationRequested, Is.True);
            Assert.That(compatible.GetCancellationToken().IsCancellationRequested, Is.True);
            Assert.That(inspectOnly.GetCancellationToken().IsCancellationRequested, Is.True);
        });
    }

    [Test]
    public void ConfiguredFactoryRejectsNullLimits()
    {
        Assert.Throws<ArgumentNullException>(() => FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.ReportMismatch,
            null!,
            CancellationToken.None));
    }

    [TestCase(-1, 0, 0)]
    [TestCase(3, 0, 0)]
    public void ConfiguredFactoryRejectsUnknownMode(int mode, int ownership, int repair)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            (FontLoadMode)mode,
            (FontInputOwnership)ownership,
            (FontRepairPolicy)repair,
            FontChecksumPolicy.RequireValid));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void ConfiguredFactoryRejectsUnknownOwnership(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            FontLoadMode.Compatible,
            (FontInputOwnership)value,
            FontRepairPolicy.None,
            FontChecksumPolicy.RequireValid));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void ConfiguredFactoryRejectsUnknownRepairPolicy(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            (FontRepairPolicy)value,
            FontChecksumPolicy.RequireValid));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void ConfiguredFactoryRejectsUnknownChecksumPolicy(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            FontRepairPolicy.None,
            (FontChecksumPolicy)value));
    }

    [Test]
    public void StrictModeRejectsRepairsAndRelaxedChecksums()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Create(
                FontLoadMode.Strict,
                FontInputOwnership.Copy,
                FontRepairPolicy.Safe,
                FontChecksumPolicy.RequireValid));
            Assert.Throws<ArgumentException>(() => Create(
                FontLoadMode.Strict,
                FontInputOwnership.Copy,
                FontRepairPolicy.None,
                FontChecksumPolicy.ReportMismatch));
            Assert.Throws<ArgumentException>(() => Create(
                FontLoadMode.Strict,
                FontInputOwnership.Copy,
                FontRepairPolicy.None,
                FontChecksumPolicy.Ignore));
        });
    }

    [Test]
    public void InspectOnlyModeRejectsRepairs()
    {
        Assert.Throws<ArgumentException>(() => Create(
            FontLoadMode.InspectOnly,
            FontInputOwnership.Copy,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch));
    }

    [Test]
    public void FactoriesReturnIndependentOptionsAndLimits()
    {
        FontLoadOptions first = FontLoadOptions.CreateCompatible();
        FontLoadOptions second = FontLoadOptions.CreateCompatible();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.GetLimits(), Is.Not.SameAs(second.GetLimits()));
        });
    }

    private static FontLoadOptions Create(
        FontLoadMode mode,
        FontInputOwnership ownership,
        FontRepairPolicy repair,
        FontChecksumPolicy checksum)
    {
        return FontLoadOptions.Create(
            mode,
            ownership,
            true,
            repair,
            checksum,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    private static void AssertProfile(
        FontLoadOptions options,
        FontLoadMode mode,
        FontRepairPolicy repair,
        FontChecksumPolicy checksum)
    {
        Assert.Multiple(() =>
        {
            Assert.That(options.GetMode(), Is.EqualTo(mode));
            Assert.That(options.GetInputOwnership(), Is.EqualTo(FontInputOwnership.Copy));
            Assert.That(options.ShouldPreserveRawTables(), Is.True);
            Assert.That(options.GetRepairPolicy(), Is.EqualTo(repair));
            Assert.That(options.GetChecksumPolicy(), Is.EqualTo(checksum));
            Assert.That(options.GetCancellationToken(), Is.EqualTo(CancellationToken.None));
        });
    }
}
