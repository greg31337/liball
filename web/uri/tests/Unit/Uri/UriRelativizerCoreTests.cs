namespace uri.Tests;

public class UriRelativizerCoreTests
{
    [TestCase("http://a/b/c?q", "http://a/b/c?q", "", UriReferenceKind.EmptyReference)]
    [TestCase("http://a/b/c?q", "http://a/b/c?q#section", "#section", UriReferenceKind.SameDocumentReference)]
    [TestCase("http://a/b/c?q", "http://a/b/c?new#section", "?new#section", UriReferenceKind.RelativeReference)]
    [TestCase("http://a/b/c?q", "http://a/b/d", "d", UriReferenceKind.RelativePathReference)]
    [TestCase("http://a/b/c?q", "http://a/x?new#section", "../x?new#section", UriReferenceKind.RelativePathReference)]
    [TestCase("http://a/a/b", "http://a/a/c:d", "./c:d", UriReferenceKind.RelativePathReference)]
    [TestCase("http://a/b/c", "http://other/x?new#section", "//other/x?new#section", UriReferenceKind.NetworkPathReference)]
    [TestCase("http://a/b/c", "https://a/b/c?new#section", "https://a/b/c?new#section", UriReferenceKind.Uri)]
    public void InverseCreationUsesTheFirstAvailableReferenceForm(
        string baseText,
        string targetText,
        string expected,
        UriReferenceKind expectedKind)
    {
        UriReference baseReference = Parse(baseText);
        UriReference target = Parse(targetText);

        UriReference inverse = UriRelativizerCore.CreateInverseReference(
            baseReference,
            target);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(inverse), Is.EqualTo(expected));
            Assert.That(inverse.Kind, Is.EqualTo(expectedKind));
        });
        AssertRoundTrip(baseReference, target, inverse);
    }

    [TestCase("http://a/b/c?base", "http://a/b/c", "c")]
    [TestCase("http://a/b/?base", "http://a/b/", "./")]
    [TestCase("http://a?base", "http://a", "//a")]
    public void PathBearingFormsCanRemoveAnInheritedBaseQuery(
        string baseText,
        string targetText,
        string expected)
    {
        UriReference baseReference = Parse(baseText);
        UriReference target = Parse(targetText);

        UriReference inverse = UriRelativizerCore.CreateInverseReference(
            baseReference,
            target);

        Assert.That(Compose(inverse), Is.EqualTo(expected));
        AssertRoundTrip(baseReference, target, inverse);
    }

    [TestCase("http://a/b/c", "http://a/b/?#", "./?#")]
    [TestCase("http://a/b/c", "http://a/b//d", ".//d")]
    public void GeneratedReferencesPreserveEmptyDelimitersAndPathShape(
        string baseText,
        string targetText,
        string expected)
    {
        UriReference baseReference = Parse(baseText);
        UriReference target = Parse(targetText);

        UriReference inverse = UriRelativizerCore.CreateInverseReference(
            baseReference,
            target);

        Assert.That(Compose(inverse), Is.EqualTo(expected));
        AssertRoundTrip(baseReference, target, inverse);
    }

    [TestCase("http://a/dir/base", "http://a/dir/name:part", "./name:part")]
    [TestCase("http://a/dir/base", "http://a/dir/name:part/leaf", "./name:part/leaf")]
    [TestCase("http://a/dir/base", "http://a/dir/name%3Apart", "name%3Apart")]
    [TestCase("http://a/dir/base", "http://a/dir/child/name:part", "child/name:part")]
    public void ColonGuardAppliesOnlyToTheLiteralFirstPathSegment(
        string baseText,
        string targetText,
        string expected)
    {
        UriReference baseReference = Parse(baseText);
        UriReference target = Parse(targetText);

        UriReference inverse = UriRelativizerCore.CreateInverseReference(
            baseReference,
            target);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(inverse), Is.EqualTo(expected));
            Assert.That(inverse.HasScheme, Is.False);
            Assert.That(inverse.Kind, Is.EqualTo(UriReferenceKind.RelativePathReference));
        });
        AssertRoundTrip(baseReference, target, inverse);
    }

    [TestCase("http://a/dir/base", "http://a/dir//leaf", ".//leaf")]
    [TestCase("http://a/dir/base", "http://a/dir///leaf", ".///leaf")]
    public void SlashGuardKeepsGeneratedPathsFromChangingReferenceForm(
        string baseText,
        string targetText,
        string expected)
    {
        UriReference baseReference = Parse(baseText);
        UriReference target = Parse(targetText);

        UriReference inverse = UriRelativizerCore.CreateInverseReference(
            baseReference,
            target);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(inverse), Is.EqualTo(expected));
            Assert.That(inverse.HasAuthority, Is.False);
            Assert.That(inverse.Kind, Is.EqualTo(UriReferenceKind.RelativePathReference));
        });
        AssertRoundTrip(baseReference, target, inverse);
    }

    [TestCase(UriRelativizationMode.RelativePath, "../../../x", UriReferenceKind.RelativePathReference)]
    [TestCase(UriRelativizationMode.Shortest, "/x", UriReferenceKind.AbsolutePathReference)]
    [TestCase(UriRelativizationMode.AbsolutePath, "/x", UriReferenceKind.AbsolutePathReference)]
    [TestCase(UriRelativizationMode.NetworkPath, "//example.com/x", UriReferenceKind.NetworkPathReference)]
    [TestCase(UriRelativizationMode.AbsoluteUri, "http://example.com/x", UriReferenceKind.Uri)]
    public void SelectionModeControlsThePreferredVerifiedForm(
        UriRelativizationMode mode,
        string expected,
        UriReferenceKind expectedKind)
    {
        UriReference baseReference = Parse("http://example.com/a/very/deep/base");
        UriReference target = Parse("http://example.com/x");
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.Mode = mode;

        UriRelativizationCoreResult result = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            options);

        AssertSuccessfulResult(result, expected, expectedKind);
        AssertRoundTrip(baseReference, target, result.Reference);
    }

    [Test]
    public void PreferredFormsFallBackWhenTheirRequiredComponentsDiffer()
    {
        UriReference baseReference = Parse("http://base.example/a/b");
        UriReference otherAuthority = Parse("http://other.example/x");
        UriReference otherScheme = Parse("https://other.example/x");
        UriRelativizationOptions absolutePathOptions = new UriRelativizationOptions();
        absolutePathOptions.Mode = UriRelativizationMode.AbsolutePath;
        UriRelativizationOptions networkPathOptions = new UriRelativizationOptions();
        networkPathOptions.Mode = UriRelativizationMode.NetworkPath;

        UriRelativizationCoreResult authorityFallback = UriRelativizerCore.RelativizeReferences(
            baseReference,
            otherAuthority,
            absolutePathOptions);
        UriRelativizationCoreResult schemeFallback = UriRelativizerCore.RelativizeReferences(
            baseReference,
            otherScheme,
            networkPathOptions);

        AssertSuccessfulResult(
            authorityFallback,
            "//other.example/x",
            UriReferenceKind.NetworkPathReference);
        AssertSuccessfulResult(
            schemeFallback,
            "https://other.example/x",
            UriReferenceKind.Uri);
    }

    [Test]
    public void SyntaxComparisonEnablesNormalizedRelativeOutput()
    {
        UriReference baseReference = Parse("HTTP://EXAMPLE.com/a/b");
        UriReference target = Parse("http://example.com/a/c");
        UriRelativizationOptions exactOptions = new UriRelativizationOptions();
        UriRelativizationOptions syntaxOptions = new UriRelativizationOptions();
        syntaxOptions.ComparisonLevel = UriComparisonLevel.Syntax;

        UriRelativizationCoreResult exact = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            exactOptions);
        UriRelativizationCoreResult syntax = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            syntaxOptions);

        AssertSuccessfulResult(exact, "http://example.com/a/c", UriReferenceKind.Uri);
        AssertSuccessfulResult(syntax, "c", UriReferenceKind.RelativePathReference);
        AssertPolicyRoundTrip(baseReference, target, syntax, syntaxOptions);
    }

    [Test]
    public void VerificationRejectsAReferenceThatChangesTheExactTarget()
    {
        UriReference baseReference = Parse("http://a/a/b");
        UriReference target = Parse("http://a/a/./c");
        UriRelativizationOptions exactOptions = new UriRelativizationOptions();
        UriRelativizationOptions syntaxOptions = new UriRelativizationOptions();
        syntaxOptions.ComparisonLevel = UriComparisonLevel.Syntax;

        UriRelativizationCoreResult exact = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            exactOptions);
        UriRelativizationCoreResult syntax = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            syntaxOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.False);
            Assert.That(exact.Text, Is.Empty);
            Assert.That(exact.Length, Is.Zero);
            Assert.That(exact.Diagnostic, Is.Not.Null);
            Assert.That(exact.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.UnresolvableReference));
        });
        AssertSuccessfulResult(syntax, "c", UriReferenceKind.RelativePathReference);
        AssertPolicyRoundTrip(baseReference, target, syntax, syntaxOptions);
    }

    [Test]
    public void SchemeProfileComparisonCanRemoveAProfileEquivalentAuthorityPort()
    {
        UriReference baseReference = Parse("http://example.com/a");
        UriReference target = Parse("http://example.com:80/a");
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.ComparisonLevel = UriComparisonLevel.SchemeProfile;

        UriRelativizationCoreResult result = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            options);

        AssertSuccessfulResult(result, "", UriReferenceKind.EmptyReference);
        AssertPolicyRoundTrip(baseReference, target, result, options);
    }

    [Test]
    public void ExplicitSchemeProfileControlsRoundTripEquivalence()
    {
        UriReference baseReference = Parse("x://example.com/a");
        UriReference target = Parse("x://example.com:9/a");
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.HasDefaultPort = true;
        profile.DefaultPort = 9;
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.ComparisonLevel = UriComparisonLevel.SchemeProfile;
        options.Profile = profile;

        UriRelativizationCoreResult result = UriRelativizerCore.RelativizeReferences(
            baseReference,
            target,
            options);

        AssertSuccessfulResult(result, "", UriReferenceKind.EmptyReference);
        AssertPolicyRoundTrip(baseReference, target, result, options);
    }

    [Test]
    public void RelativizationOptionsDefaultToExactRelativePathSelection()
    {
        UriRelativizationOptions options = new UriRelativizationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.Mode, Is.EqualTo(UriRelativizationMode.RelativePath));
            Assert.That(options.ComparisonLevel, Is.EqualTo(UriComparisonLevel.Exact));
            Assert.That(options.Profile, Is.Null);
        });
    }

    [Test]
    public void RelativizationOptionsAndInputsAreValidatedBeforeSelection()
    {
        UriReference absolute = Parse("http://a/b");
        UriReference fragmentedBase = Parse("http://a/b#fragment");
        UriRelativizationOptions invalidMode = new UriRelativizationOptions();
        invalidMode.Mode = (UriRelativizationMode)99;
        UriRelativizationOptions invalidLevel = new UriRelativizationOptions();
        invalidLevel.ComparisonLevel = (UriComparisonLevel)99;
        UriRelativizationOptions invalidProfile = new UriRelativizationOptions();
        invalidProfile.ComparisonLevel = UriComparisonLevel.SchemeProfile;
        invalidProfile.Profile = new UriSchemeProfile();

        UriRelativizationCoreResult missing = UriRelativizerCore.RelativizeReferences(
            absolute,
            absolute,
            null);
        UriRelativizationCoreResult mode = UriRelativizerCore.RelativizeReferences(
            absolute,
            absolute,
            invalidMode);
        UriRelativizationCoreResult level = UriRelativizerCore.RelativizeReferences(
            absolute,
            absolute,
            invalidLevel);
        UriRelativizationCoreResult profile = UriRelativizerCore.RelativizeReferences(
            absolute,
            absolute,
            invalidProfile);
        UriRelativizationCoreResult fragment = UriRelativizerCore.RelativizeReferences(
            fragmentedBase,
            absolute,
            new UriRelativizationOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(missing, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(mode, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(level, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(profile, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(fragment, UriDiagnosticCode.UnresolvableReference, UriComponent.Fragment);
        });
    }

    [Test]
    public void AbsoluteAndNetworkFallbacksDeepCopyAuthorityStorage()
    {
        UriReference baseReference = Parse("http://base/a");
        UriReference networkTarget = Parse("http://user@[2001:db8::1]:80/b");
        UriReference absoluteTarget = Parse("https://user@[2001:db8::2]:81/c");

        UriReference network = UriRelativizerCore.CreateInverseReference(
            baseReference,
            networkTarget);
        UriReference absolute = UriRelativizerCore.CreateInverseReference(
            baseReference,
            absoluteTarget);

        Assert.Multiple(() =>
        {
            Assert.That(network.Authority, Is.Not.SameAs(networkTarget.Authority));
            Assert.That(network.Authority.AddressBytes, Is.Not.SameAs(networkTarget.Authority.AddressBytes));
            Assert.That(absolute.Authority, Is.Not.SameAs(absoluteTarget.Authority));
            Assert.That(absolute.Authority.AddressBytes, Is.Not.SameAs(absoluteTarget.Authority.AddressBytes));
            Assert.That(network.OriginalText, Is.Empty);
            Assert.That(absolute.OriginalText, Is.Empty);
        });

        network.Authority.AddressBytes[0] = 255;
        absolute.Authority.AddressBytes[0] = 255;
        Assert.Multiple(() =>
        {
            Assert.That(networkTarget.Authority.AddressBytes[0], Is.EqualTo(32));
            Assert.That(absoluteTarget.Authority.AddressBytes[0], Is.EqualTo(32));
        });
    }

    [Test]
    public void InverseCreationRejectsMissingAndNonAbsoluteInputs()
    {
        UriReference absolute = Parse("http://a/b");
        UriReference relative = Parse("b");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriRelativizerCore.CreateInverseReference(null, absolute));
            Assert.Throws<ArgumentException>(() =>
                UriRelativizerCore.CreateInverseReference(absolute, null));
            Assert.Throws<ArgumentException>(() =>
                UriRelativizerCore.CreateInverseReference(relative, absolute));
            Assert.Throws<ArgumentException>(() =>
                UriRelativizerCore.CreateInverseReference(absolute, relative));
        });
    }

    private static void AssertRoundTrip(
        UriReference baseReference,
        UriReference target,
        UriReference inverse)
    {
        UriReference reparsedInverse = Parse(Compose(inverse));
        UriReference resolved = UriResolverCore.TransformStrictReference(
            baseReference,
            reparsedInverse);
        UriComparisonCoreResult comparison = UriComparerCore.CompareExact(
            target,
            resolved);
        Assert.That(comparison.Equal, Is.True, comparison.DifferentComponent.ToString());
    }

    private static void AssertPolicyRoundTrip(
        UriReference baseReference,
        UriReference target,
        UriRelativizationCoreResult result,
        UriRelativizationOptions options)
    {
        UriReference reparsed = Parse(result.Text);
        UriReference resolved = UriResolverCore.TransformStrictReference(
            baseReference,
            reparsed);
        UriComparisonCoreResult comparison = UriRelativizerCore.CompareRelativizationRoundTrip(
            resolved,
            target,
            options);
        Assert.That(comparison.Equal, Is.True, comparison.DifferentComponent.ToString());
    }

    private static void AssertSuccessfulResult(
        UriRelativizationCoreResult result,
        string expected,
        UriReferenceKind expectedKind)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Reference.Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriRelativizationCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static UriReference Parse(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }

    private static string Compose(UriReference reference)
    {
        UriCompositionResult result = UriComposerCore.RecomposeParsedReference(
            reference,
            new OperationLimits());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.Text;
    }
}
