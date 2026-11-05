namespace svg.Tests;

public class SvgDiagnosticTests
{
    [TestCase(SvgDiagnosticCode.None, "SVG0000")]
    [TestCase(SvgDiagnosticCode.DiagnosticsTruncated, "SVG0001")]
    [TestCase(SvgDiagnosticCode.MalformedXml, "SVG0100")]
    [TestCase(SvgDiagnosticCode.InvalidContainer, "SVG0103")]
    [TestCase(SvgDiagnosticCode.UnsupportedEncoding, "SVG0104")]
    [TestCase(SvgDiagnosticCode.DuplicateId, "SVG0105")]
    [TestCase(SvgDiagnosticCode.PatchConflict, "SVG0902")]
    [TestCase(SvgDiagnosticCode.InvalidValue, "SVG0200")]
    [TestCase(SvgDiagnosticCode.ResourceDenied, "SVG0500")]
    [TestCase(SvgDiagnosticCode.CallbackRequired, "SVG0602")]
    [TestCase(SvgDiagnosticCode.InvalidFont, "SVG0603")]
    [TestCase(SvgDiagnosticCode.RenderingFailed, "SVG0800")]
    [TestCase(SvgDiagnosticCode.InternalInvariant, "SVG9999")]
    public void DiagnosticCodesHaveStableExternalText(SvgDiagnosticCode code, string expected)
    {
        Assert.That(new SvgDiagnosticCodeNames().Get(code), Is.EqualTo(expected));
    }

    [Test]
    public void DiagnosticRetainsSourceAndRelatedProvenance()
    {
        SvgSourceUri source = new SvgSourceUri("drawing.svg");
        SvgSourceSpan primary = new SvgSourceSpan(source, 2, 1, 3, 8, 1, 9);
        SvgSourceSpan related = new SvgSourceSpan(source, 12, 2, 1, 16, 2, 5);
        SvgDiagnostic diagnostic = new SvgDiagnostic(
            SvgDiagnosticSeverity.Warning,
            SvgDiagnosticCode.InvalidReference,
            "Reference was not found.",
            primary);

        diagnostic.AddRelatedSpan(related);
        diagnostic.InstancePath = "0/3";

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Source, Is.SameAs(primary));
            Assert.That(diagnostic.ResourcePath, Is.EqualTo("0"));
            Assert.That(diagnostic.RelatedSpans, Is.EqualTo(new[] { related }));
            Assert.That(diagnostic.InstancePath, Is.EqualTo("0/3"));
            Assert.That(diagnostic.Sequence, Is.EqualTo(-1));
        });
    }

    [Test]
    public void StrictBehaviorStopsOnFirstError()
    {
        SvgDiagnosticBag bag = new SvgDiagnosticBag(10, SvgDiagnosticBehavior.Strict);
        SvgDiagnostic warning = new SvgDiagnostic(
            SvgDiagnosticSeverity.Warning,
            SvgDiagnosticCode.CompatibilityRecovery,
            "Recovered.",
            null);
        SvgDiagnostic error = new SvgDiagnostic(
            SvgDiagnosticSeverity.Error,
            SvgDiagnosticCode.InvalidValue,
            "Invalid value.",
            null);
        SvgDiagnostic ignored = new SvgDiagnostic(
            SvgDiagnosticSeverity.Info,
            SvgDiagnosticCode.None,
            "Not reached.",
            null);

        bool afterWarning = bag.Add(warning);
        bool afterError = bag.Add(error);
        bool afterIgnored = bag.Add(ignored);

        Assert.Multiple(() =>
        {
            Assert.That(afterWarning, Is.True);
            Assert.That(afterError, Is.False);
            Assert.That(afterIgnored, Is.False);
            Assert.That(bag.ShouldContinue(), Is.False);
            Assert.That(bag.Stopped, Is.True);
            Assert.That(bag.HasErrors, Is.True);
            Assert.That(bag.Count(), Is.EqualTo(2));
            Assert.That(warning.Sequence, Is.EqualTo(0));
            Assert.That(error.Sequence, Is.EqualTo(1));
            Assert.That(ignored.Sequence, Is.EqualTo(-1));
        });
    }

    [Test]
    public void CompatibleBehaviorContinuesAfterErrors()
    {
        SvgDiagnosticBag bag = new SvgDiagnosticBag(5, SvgDiagnosticBehavior.Compatible);

        bool afterFirst = bag.AddError(SvgDiagnosticCode.MalformedXml, "Bad token.", null);
        bool afterSecond = bag.AddWarning(SvgDiagnosticCode.CompatibilityRecovery, "Skipped token.", null);

        Assert.Multiple(() =>
        {
            Assert.That(afterFirst, Is.True);
            Assert.That(afterSecond, Is.True);
            Assert.That(bag.ShouldContinue(), Is.True);
            Assert.That(bag.Stopped, Is.False);
            Assert.That(bag.HasErrors, Is.True);
            Assert.That(bag.Count(), Is.EqualTo(2));
        });
    }

    [Test]
    public void BoundReplacesLastEntryWithOneTruncationDiagnostic()
    {
        SvgSourceUri source = new SvgSourceUri("bounded.svg");
        SvgDiagnosticBag bag = new SvgDiagnosticBag(3, SvgDiagnosticBehavior.Compatible);

        bag.AddInfo(SvgDiagnosticCode.None, "one", new SvgSourceSpan(source, 1, 1, 2, 2, 1, 3));
        bag.AddInfo(SvgDiagnosticCode.None, "two", new SvgSourceSpan(source, 2, 1, 3, 3, 1, 4));
        bag.AddInfo(SvgDiagnosticCode.None, "three", new SvgSourceSpan(source, 3, 1, 4, 4, 1, 5));
        bag.AddInfo(SvgDiagnosticCode.None, "four", new SvgSourceSpan(source, 4, 1, 5, 5, 1, 6));
        bag.AddInfo(SvgDiagnosticCode.None, "five", new SvgSourceSpan(source, 5, 1, 6, 6, 1, 7));

        List<SvgDiagnostic> snapshot = bag.Snapshot();
        SvgDiagnostic truncated = snapshot.Single(item => item.Code == SvgDiagnosticCode.DiagnosticsTruncated);

        Assert.Multiple(() =>
        {
            Assert.That(bag.Count(), Is.EqualTo(3));
            Assert.That(bag.DroppedCount, Is.EqualTo(3));
            Assert.That(snapshot.Select(item => item.Message), Does.Contain("one"));
            Assert.That(snapshot.Select(item => item.Message), Does.Contain("two"));
            Assert.That(snapshot.Select(item => item.Message), Does.Not.Contain("three"));
            Assert.That(snapshot.Select(item => item.Message), Does.Not.Contain("four"));
            Assert.That(snapshot.Select(item => item.Message), Does.Not.Contain("five"));
            Assert.That(truncated.Message, Is.EqualTo("Diagnostic limit reached; 3 diagnostics were omitted."));
        });
    }

    [Test]
    public void ZeroBoundRetainsNothingButStillControlsStrictProcessing()
    {
        SvgDiagnosticBag compatible = new SvgDiagnosticBag(0, SvgDiagnosticBehavior.Compatible);
        SvgDiagnosticBag strict = new SvgDiagnosticBag(0, SvgDiagnosticBehavior.Strict);

        bool compatibleResult = compatible.AddWarning(SvgDiagnosticCode.UnsupportedFeature, "Ignored.", null);
        bool strictResult = strict.AddError(SvgDiagnosticCode.InvalidValue, "Invalid.", null);

        Assert.Multiple(() =>
        {
            Assert.That(compatibleResult, Is.True);
            Assert.That(compatible.Count(), Is.Zero);
            Assert.That(compatible.DroppedCount, Is.EqualTo(1));
            Assert.That(strictResult, Is.False);
            Assert.That(strict.Count(), Is.Zero);
            Assert.That(strict.DroppedCount, Is.EqualTo(1));
            Assert.That(strict.HasErrors, Is.True);
            Assert.That(strict.Stopped, Is.True);
        });
    }

    [Test]
    public void SnapshotOrderDoesNotDependOnInsertionOrder()
    {
        SvgSourceUri source = new SvgSourceUri("ordered.svg");
        SvgSourceSpan early = new SvgSourceSpan(source, 4, 1, 5, 5, 1, 6);
        SvgSourceSpan late = new SvgSourceSpan(source, 20, 3, 1, 21, 3, 2);
        SvgDiagnosticBag forward = new SvgDiagnosticBag(10, SvgDiagnosticBehavior.Compatible);
        SvgDiagnosticBag reverse = new SvgDiagnosticBag(10, SvgDiagnosticBehavior.Compatible);

        AddOrderingSet(forward, early, late, false);
        AddOrderingSet(reverse, early, late, true);

        string[] forwardMessages = forward.Snapshot().Select(item => item.Message).ToArray();
        string[] reverseMessages = reverse.Snapshot().Select(item => item.Message).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(forwardMessages, Is.EqualTo(reverseMessages));
            Assert.That(forwardMessages, Is.EqualTo(new[]
            {
                "early error",
                "early warning",
                "late info",
                "unlocated"
            }));
        });
    }

    [Test]
    public void DiagnosticOrderingUsesCodeMessageInstanceAndSequenceTieBreakers()
    {
        SvgSourceUri source = new SvgSourceUri("ties.svg");
        SvgSourceSpan span = new SvgSourceSpan(source, 1, 1, 2, 2, 1, 3);
        SvgDiagnosticBag bag = new SvgDiagnosticBag(10, SvgDiagnosticBehavior.Compatible);
        SvgDiagnostic messageB = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "b", span);
        SvgDiagnostic instanceB = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "a", span);
        instanceB.InstancePath = "b";
        SvgDiagnostic identicalFirst = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "a", span);
        identicalFirst.InstancePath = "a";
        SvgDiagnostic codeFirst = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.MalformedXml, "z", span);
        SvgDiagnostic emptyInstance = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "a", span);
        SvgDiagnostic identicalSecond = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "a", span);
        identicalSecond.InstancePath = "a";

        bag.Add(messageB);
        bag.Add(instanceB);
        bag.Add(identicalFirst);
        bag.Add(codeFirst);
        bag.Add(emptyInstance);
        bag.Add(identicalSecond);

        Assert.That(bag.Snapshot(), Is.EqualTo(new[]
        {
            codeFirst,
            emptyInstance,
            identicalFirst,
            identicalSecond,
            instanceB,
            messageB
        }));
    }

    [Test]
    public void SnapshotIsIndependentAndClearResetsBagState()
    {
        SvgDiagnosticBag bag = new SvgDiagnosticBag(4, SvgDiagnosticBehavior.Compatible);
        bag.AddError(SvgDiagnosticCode.InvalidCss, "Invalid CSS.", null);
        List<SvgDiagnostic> snapshot = bag.Snapshot();

        snapshot.Clear();

        Assert.That(bag.Count(), Is.EqualTo(1));

        bag.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(bag.Count(), Is.Zero);
            Assert.That(bag.DroppedCount, Is.Zero);
            Assert.That(bag.HasErrors, Is.False);
            Assert.That(bag.Stopped, Is.False);
            Assert.That(bag.ShouldContinue(), Is.True);
        });
    }

    [Test]
    public void DiagnosticTypesRejectInvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new SvgDiagnostic(
                SvgDiagnosticSeverity.Error,
                SvgDiagnosticCode.InvalidArgument,
                null,
                null));
            Assert.Throws<ArgumentNullException>(() => new SvgDiagnosticBag(1, SvgDiagnosticBehavior.Compatible).Add(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgDiagnosticBag(-1, SvgDiagnosticBehavior.Compatible));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgDiagnosticBag(1, (SvgDiagnosticBehavior)99));
            Assert.That(new SvgDiagnosticCodeNames().Get((SvgDiagnosticCode)123456), Is.EqualTo("SVG9999"));
        });
    }

    static void AddOrderingSet(SvgDiagnosticBag bag, SvgSourceSpan early, SvgSourceSpan late, bool reverse)
    {
        SvgDiagnostic[] diagnostics =
        [
            new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "early warning", early),
            new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, "early error", early),
            new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "late info", late),
            new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "unlocated", null)
        ];

        if (reverse)
        {
            int index = diagnostics.Length - 1;
            while (index >= 0)
            {
                bag.Add(diagnostics[index]);
                index--;
            }
        }
        else
        {
            int index = 0;
            while (index < diagnostics.Length)
            {
                bag.Add(diagnostics[index]);
                index++;
            }
        }
    }
}
