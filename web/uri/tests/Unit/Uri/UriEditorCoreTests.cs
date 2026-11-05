namespace uri.Tests;

public class UriEditorCoreTests
{
    [TestCase("", UriReferenceKind.EmptyReference)]
    [TestCase("#fragment", UriReferenceKind.SameDocumentReference)]
    [TestCase("?query", UriReferenceKind.RelativeReference)]
    [TestCase("relative/path", UriReferenceKind.RelativePathReference)]
    [TestCase("/absolute/path", UriReferenceKind.AbsolutePathReference)]
    [TestCase("//host/path", UriReferenceKind.NetworkPathReference)]
    [TestCase("x:path", UriReferenceKind.Uri)]
    public void ComponentConstructionDerivesReferenceKindAndValidatesOutput(
        string text,
        UriReferenceKind expectedKind)
    {
        UriReference components = Parse(text);
        components.Kind = UriReferenceKind.EmptyReference;
        components.OriginalText = "ignored";

        UriEditCoreResult result = UriEditorCore.BuildFromComponents(
            components,
            new UriValidationOptions());

        AssertSuccess(result, text, expectedKind);
        Assert.That(result.Reference, Is.Not.SameAs(components));
        Assert.That(result.Reference.OriginalText, Is.Empty);
    }

    [Test]
    public void UnselectedChangeValuesKeepEverySourceComponent()
    {
        UriReference source = Parse("http://user@host:80/a?query#fragment");
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.HasScheme = true;
        changes.Scheme = "changed";
        changes.HasAuthority = true;
        changes.Authority = Parse("//other/").Authority;
        changes.Path = "/changed";
        changes.HasQuery = true;
        changes.Query = "changed";
        changes.HasFragment = true;
        changes.Fragment = "changed";

        UriEditCoreResult result = UriEditorCore.CopyWithChanges(
            source,
            changes,
            new UriValidationOptions());

        AssertSuccess(
            result,
            "http://user@host:80/a?query#fragment",
            UriReferenceKind.Uri);
    }

    [Test]
    public void SelectedChangesSetEveryComponentAndDelimiterState()
    {
        UriReference source = Parse("http://user@host:80/a?query#fragment");
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeScheme = true;
        changes.HasScheme = true;
        changes.Scheme = "https";
        changes.ChangeAuthority = true;
        changes.HasAuthority = true;
        changes.Authority = Parse("//other:443/").Authority;
        changes.ChangePath = true;
        changes.Path = "/new/path";
        changes.ChangeQuery = true;
        changes.HasQuery = true;
        changes.Query = "";
        changes.ChangeFragment = true;
        changes.HasFragment = true;
        changes.Fragment = "";

        UriEditCoreResult result = UriEditorCore.CopyWithChanges(
            source,
            changes,
            new UriValidationOptions());

        AssertSuccess(
            result,
            "https://other:443/new/path?#",
            UriReferenceKind.Uri);
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
    }

    [Test]
    public void SelectedChangesCanClearEveryOptionalDelimiter()
    {
        UriReference source = Parse("http://user@host:80/a?query#fragment");
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeScheme = true;
        changes.HasScheme = false;
        changes.ChangeAuthority = true;
        changes.HasAuthority = false;
        changes.ChangePath = true;
        changes.Path = "relative/path";
        changes.ChangeQuery = true;
        changes.HasQuery = false;
        changes.ChangeFragment = true;
        changes.HasFragment = false;

        UriEditCoreResult result = UriEditorCore.CopyWithChanges(
            source,
            changes,
            new UriValidationOptions());

        AssertSuccess(
            result,
            "relative/path",
            UriReferenceKind.RelativePathReference);
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.Scheme, Is.Empty);
            Assert.That(result.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
    }

    [Test]
    public void IndividualDelimiterChangesPreserveUnrelatedComponents()
    {
        UriReference source = Parse("x://host/path?old#old");
        UriReferenceChanges queryChanges = new UriReferenceChanges();
        queryChanges.ChangeQuery = true;
        queryChanges.HasQuery = true;
        queryChanges.Query = "new";
        UriReferenceChanges fragmentChanges = new UriReferenceChanges();
        fragmentChanges.ChangeFragment = true;
        fragmentChanges.HasFragment = false;

        UriEditCoreResult query = UriEditorCore.CopyWithChanges(
            source,
            queryChanges,
            new UriValidationOptions());
        UriEditCoreResult fragment = UriEditorCore.CopyWithChanges(
            source,
            fragmentChanges,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            AssertSuccess(query, "x://host/path?new#old", UriReferenceKind.Uri);
            AssertSuccess(fragment, "x://host/path?old", UriReferenceKind.Uri);
        });
    }

    [Test]
    public void ConstructionAndEditingDeepCopyAuthorityStorage()
    {
        UriReference components = Parse("http://user@[2001:db8::1]:80/a");
        UriReference source = Parse("http://source/a");
        UriReference replacement = Parse("//user@[2001:db8::2]:81/");
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangeAuthority = true;
        changes.HasAuthority = true;
        changes.Authority = replacement.Authority;

        UriEditCoreResult built = UriEditorCore.BuildFromComponents(
            components,
            new UriValidationOptions());
        UriEditCoreResult edited = UriEditorCore.CopyWithChanges(
            source,
            changes,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(built.Reference.Authority, Is.Not.SameAs(components.Authority));
            Assert.That(built.Reference.Authority.AddressBytes, Is.Not.SameAs(components.Authority.AddressBytes));
            Assert.That(edited.Reference.Authority, Is.Not.SameAs(replacement.Authority));
            Assert.That(edited.Reference.Authority.AddressBytes, Is.Not.SameAs(replacement.Authority.AddressBytes));
        });

        built.Reference.Authority.AddressBytes[0] = 255;
        edited.Reference.Authority.AddressBytes[0] = 255;
        Assert.Multiple(() =>
        {
            Assert.That(components.Authority.AddressBytes[0], Is.EqualTo(32));
            Assert.That(replacement.Authority.AddressBytes[0], Is.EqualTo(32));
        });
    }

    [Test]
    public void InvalidPostEditGrammarReturnsTheSpecificComponentDiagnostic()
    {
        UriReference source = Parse("x:/valid");
        UriReferenceChanges authorityChanges = new UriReferenceChanges();
        authorityChanges.ChangeAuthority = true;
        authorityChanges.HasAuthority = true;
        authorityChanges.Authority = Parse("//host/").Authority;
        authorityChanges.ChangePath = true;
        authorityChanges.Path = "rootless";
        UriReferenceChanges pathChanges = new UriReferenceChanges();
        pathChanges.ChangeScheme = true;
        pathChanges.HasScheme = false;
        pathChanges.ChangePath = true;
        pathChanges.Path = "name:part";
        UriReferenceChanges queryChanges = new UriReferenceChanges();
        queryChanges.ChangeQuery = true;
        queryChanges.HasQuery = true;
        queryChanges.Query = "%GG";

        UriEditCoreResult authority = UriEditorCore.CopyWithChanges(
            source,
            authorityChanges,
            new UriValidationOptions());
        UriEditCoreResult path = UriEditorCore.CopyWithChanges(
            source,
            pathChanges,
            new UriValidationOptions());
        UriEditCoreResult query = UriEditorCore.CopyWithChanges(
            source,
            queryChanges,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(authority, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertFailure(path, UriDiagnosticCode.InvalidPath, UriComponent.Path);
            AssertFailure(query, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Query);
        });
    }

    [Test]
    public void InvalidInputsAndSourceModelsReturnFailuresWithoutMutation()
    {
        UriReference source = Parse("x:/valid");
        UriReference invalidSource = Parse("x:/valid");
        invalidSource.Path = "%GG";
        UriReferenceChanges changes = new UriReferenceChanges();
        string sourceBefore = Compose(source);

        UriEditCoreResult missingComponents = UriEditorCore.BuildFromComponents(
            null,
            new UriValidationOptions());
        UriEditCoreResult missingChanges = UriEditorCore.CopyWithChanges(
            source,
            null,
            new UriValidationOptions());
        UriEditCoreResult invalid = UriEditorCore.CopyWithChanges(
            invalidSource,
            changes,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(missingComponents, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(missingChanges, UriDiagnosticCode.InvalidInput, UriComponent.Input);
            AssertFailure(invalid, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
            Assert.That(Compose(source), Is.EqualTo(sourceBefore));
        });
    }

    [Test]
    public void ConstructionAndEditingApplyValidationAndOutputLimits()
    {
        UriValidationOptions componentOptions = new UriValidationOptions();
        componentOptions.Limits.MaxComponentCharacters = 2;
        UriValidationOptions outputOptions = new UriValidationOptions();
        outputOptions.Limits.MaxOutputCharacters = 3;
        UriReferenceChanges changes = new UriReferenceChanges();
        changes.ChangePath = true;
        changes.Path = "long";

        UriEditCoreResult component = UriEditorCore.BuildFromComponents(
            Parse("x:long"),
            componentOptions);
        UriEditCoreResult output = UriEditorCore.CopyWithChanges(
            Parse("x:a"),
            changes,
            outputOptions);

        Assert.Multiple(() =>
        {
            AssertFailure(component, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
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

    private static void AssertSuccess(
        UriEditCoreResult result,
        string expected,
        UriReferenceKind expectedKind)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.Reference.Kind, Is.EqualTo(expectedKind));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriEditCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
