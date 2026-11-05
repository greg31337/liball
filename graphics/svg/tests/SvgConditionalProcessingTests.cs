namespace svg.Tests;

public class SvgConditionalProcessingTests
{
    [Test]
    public void CombinedConditionsRequireEveryHostCapabilityAndMatchLanguagePrefixes()
    {
        SvgNode node = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect requiredFeatures='feature.one feature.two' requiredExtensions='urn:example:a urn:example:b' systemLanguage='fr-CA, EN-us'/></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.SupportedFeatures.Add("feature.one");
        options.SupportedFeatures.Add("feature.two");
        options.SupportedExtensions.Add("urn:example:a");
        options.SupportedExtensions.Add("urn:example:b");
        options.LanguagePreferences.Add("en");

        SvgConditionalResult result = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Matches, Is.True);
            Assert.That(result.Applicable, Is.True);
            Assert.That(result.RequiredFeatures.Select(item => item.Value), Is.EqualTo(new[] { "feature.one", "feature.two" }));
            Assert.That(result.RequiredExtensions.Select(item => item.Value), Is.EqualTo(new[] { "urn:example:a", "urn:example:b" }));
            Assert.That(result.SystemLanguages.Select(item => item.Value), Is.EqualTo(new[] { "fr-ca", "en-us" }));
            Assert.That(result.MissingFeatures, Is.Empty);
            Assert.That(result.MissingExtensions, Is.Empty);
            Assert.That(result.MatchedLanguage, Is.EqualTo("en-us"));
            Assert.That(result.MatchedLanguagePreference, Is.EqualTo("en"));
            Assert.That(result.LanguagePreferenceIndex, Is.EqualTo(0));
            Assert.That(result.Checks, Is.EqualTo(6));
            Assert.That(result.RequiredFeaturesSource.SourceSpan, Is.Not.Null);
            Assert.That(result.RequiredExtensionsSource.SourceSpan, Is.Not.Null);
            Assert.That(result.SystemLanguageSource.SourceSpan, Is.Not.Null);
        });
    }

    [Test]
    public void MissingCapabilitiesAreReportedAsNormalNonMatchesWithOrdinalIdentity()
    {
        SvgNode node = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect requiredFeatures='Feature.One feature.two' requiredExtensions='urn:Ext urn:ext'/></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.SupportedFeatures.Add("feature.one");
        options.SupportedFeatures.Add("feature.two");
        options.SupportedExtensions.Add("urn:ext");

        SvgConditionalResult result = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Matches, Is.False);
            Assert.That(result.MissingFeatures.Select(item => item.Value), Is.EqualTo(new[] { "Feature.One" }));
            Assert.That(result.MissingExtensions.Select(item => item.Value), Is.EqualTo(new[] { "urn:Ext" }));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Checks, Is.EqualTo(4));
        });
    }

    [Test]
    public void LanguageMatchingUsesPreferenceToAuthoredTagDirection()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect systemLanguage='en'/><circle systemLanguage='EN-gb'/></svg>");
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.LanguagePreferences.Add("en-GB");

        SvgConditionalResult general = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(document.DocumentElement.Children[0], options);
        SvgConditionalResult specific = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(document.DocumentElement.Children[1], options);
        options.LanguagePreferences.Clear();
        options.LanguagePreferences.Add("EN");
        SvgConditionalResult prefix = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(document.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(general.Success, Is.True);
            Assert.That(general.Matches, Is.False);
            Assert.That(specific.Matches, Is.True);
            Assert.That(specific.MatchedLanguagePreference, Is.EqualTo("en-gb"));
            Assert.That(prefix.Matches, Is.True);
            Assert.That(prefix.MatchedLanguage, Is.EqualTo("en-gb"));
            Assert.That(prefix.MatchedLanguagePreference, Is.EqualTo("en"));
        });
    }

    [Test]
    public void EmptyMalformedAndBoundedAttributeValuesFailAtTheirSource()
    {
        SvgNode emptyNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect requiredFeatures='' requiredExtensions=' ' systemLanguage=''/></svg>").DocumentElement.Children[0];
        SvgConditionalResult empty = new SvgConditionalProcessing().EvaluateWithNode(emptyNode);
        SvgNode longNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect requiredFeatures='feature'/></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions limitedOptions = new SvgConditionalProcessingOptions();
        limitedOptions.MaxTokenCharacters = 3;
        SvgConditionalResult limited = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(longNode, limitedOptions);
        SvgNode malformedNode = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect systemLanguage='en_US'/></svg>").DocumentElement.Children[0];
        SvgConditionalResult malformed = new SvgConditionalProcessing().EvaluateWithNode(malformedNode);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Matches, Is.False);
            Assert.That(empty.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(empty.Diagnostics.All(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(empty.Diagnostics.Select(item => item.Source), Is.EqualTo(new[] { emptyNode.Attributes[1].SourceSpan, emptyNode.Attributes[0].SourceSpan, emptyNode.Attributes[2].SourceSpan }));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(limited.Diagnostics.Single().Source, Is.SameAs(longNode.Attributes[0].SourceSpan));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
            Assert.That(malformed.Diagnostics.Single().Source, Is.SameAs(malformedNode.Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void ApplicabilityIgnoresConditionsOnNeverProcessedAndForeignElements()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs requiredExtensions=''><rect/></defs><foreignObject><x:widget xmlns:x='urn:example' systemLanguage=''/></foreignObject><unknown requiredFeatures=''/></svg>");
        SvgNode definitions = document.DocumentElement.Children[0];
        SvgNode foreign = document.DocumentElement.Children[1].Children[0];
        SvgNode unknown = document.DocumentElement.Children[2];

        SvgConditionalResult definitionsResult = new SvgConditionalProcessing().EvaluateWithNode(definitions);
        SvgConditionalResult foreignResult = new SvgConditionalProcessing().EvaluateWithNode(foreign);
        SvgConditionalResult unknownResult = new SvgConditionalProcessing().EvaluateWithNode(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(definitionsResult.Applicable, Is.False);
            Assert.That(definitionsResult.Matches, Is.True);
            Assert.That(definitionsResult.RequiredExtensionsSource, Is.Null);
            Assert.That(foreignResult.Applicable, Is.False);
            Assert.That(foreignResult.Matches, Is.True);
            Assert.That(unknownResult.Applicable, Is.True);
            Assert.That(unknownResult.Matches, Is.False);
            Assert.That(unknownResult.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
        });
    }

    [Test]
    public void SwitchSelectsBestOrderedLanguageBeforeFallbackAndIgnoresDisplay()
    {
        SvgNode switchElement = Load("<svg xmlns='http://www.w3.org/2000/svg'><switch>text<!--comment--><style systemLanguage='fr'>rect{fill:red}</style><rect id='nl' systemLanguage='nl'/><circle id='en' systemLanguage='en' display='none'/><path id='fallback'/><script systemLanguage='en'>noop()</script></switch></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.LanguagePreferences.Add("en");
        options.LanguagePreferences.Add("nl");

        SvgSwitchResult result = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(switchElement, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SelectedChild, Is.SameAs(switchElement.Children[4]));
            Assert.That(result.SelectedChild.ElementKind, Is.EqualTo(SvgElementKind.Circle));
            Assert.That(result.SelectedIndex, Is.EqualTo(1));
            Assert.That(result.Candidates.Select(item => item.Node.ElementKind), Is.EqualTo(new[] { SvgElementKind.Rect, SvgElementKind.Circle, SvgElementKind.Path }));
            Assert.That(result.Candidates[0].LanguagePreferenceIndex, Is.EqualTo(1));
            Assert.That(result.Candidates[1].LanguagePreferenceIndex, Is.EqualTo(0));
            Assert.That(result.Checks, Is.EqualTo(3));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void SwitchRequiresAllConditionsAndUsesFirstEligibleFallback()
    {
        SvgNode switchElement = Load("<svg xmlns='http://www.w3.org/2000/svg'><switch><rect requiredFeatures='missing' systemLanguage='en'/><circle requiredExtensions='urn:supported'/><path/><ellipse systemLanguage='en'/></switch></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.SupportedExtensions.Add("urn:supported");
        options.LanguagePreferences.Add("fr");

        SvgSwitchResult fallback = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(switchElement, options);
        SvgNode noMatchSwitch = Load("<svg xmlns='http://www.w3.org/2000/svg'><switch><rect requiredFeatures='missing'/><circle systemLanguage='de'/></switch></svg>").DocumentElement.Children[0];
        SvgSwitchResult noMatch = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(noMatchSwitch, options);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Success, Is.True);
            Assert.That(fallback.SelectedChild.ElementKind, Is.EqualTo(SvgElementKind.Circle));
            Assert.That(fallback.SelectedIndex, Is.EqualTo(1));
            Assert.That(fallback.Candidates[0].MissingFeatures.Single().Value, Is.EqualTo("missing"));
            Assert.That(fallback.Candidates[2].Matches, Is.True);
            Assert.That(noMatch.Success, Is.True);
            Assert.That(noMatch.SelectedChild, Is.Null);
            Assert.That(noMatch.SelectedIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void ConditionalBudgetsAndCapturedVersionsAreEnforced()
    {
        SvgNode switchElement = Load("<svg xmlns='http://www.w3.org/2000/svg'><switch><rect systemLanguage='en'/><circle systemLanguage='fr'/></switch></svg>").DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.LanguagePreferences.Add("de");
        options.Limits.MaxConditionalChecks = 1;

        SvgSwitchResult limited = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(switchElement, options);
        SvgConditionalProcessingOptions currentOptions = new SvgConditionalProcessingOptions();
        currentOptions.LanguagePreferences.Add("en");
        SvgSwitchResult current = new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(switchElement, currentOptions);
        switchElement.Children[0].Attributes[0].MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.SelectedChild, Is.Null);
            Assert.That(limited.Checks, Is.EqualTo(1));
            Assert.That(limited.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(limited.Diagnostics.Single().Source, Is.SameAs(switchElement.Children[1].Attributes[0].SourceSpan));
            Assert.That(current.Success, Is.True);
            Assert.That(current.SelectedChild, Is.SameAs(switchElement.Children[0]));
            Assert.That(current.IsCurrent(), Is.False);
            Assert.That(current.Candidates[0].IsCurrent(), Is.False);
        });
    }

    [Test]
    public void InvalidArgumentsAndHostPreferencesAreRejectedDeterministically()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect systemLanguage='en'/></svg>");
        SvgNode node = document.DocumentElement.Children[0];
        SvgConditionalProcessingOptions options = new SvgConditionalProcessingOptions();
        options.LanguagePreferences.Add("en_US");
        SvgConditionalResult invalidPreference = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, options);
        SvgConditionalProcessingOptions invalidOptions = new SvgConditionalProcessingOptions();
        invalidOptions.Limits.MaxConditionalChecks = 0;
        SvgConditionalResult invalidLimits = new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(invalidPreference.Success, Is.False);
            Assert.That(invalidPreference.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(invalidLimits.Success, Is.False);
            Assert.That(invalidLimits.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.Throws<ArgumentNullException>(() => new SvgConditionalProcessing().EvaluateWithNode(null));
            Assert.Throws<ArgumentNullException>(() => new SvgConditionalProcessing().EvaluateWithNodeAndOptions(node, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgConditionalProcessing().EvaluateWithNode(document.DocumentNode));
            Assert.Throws<ArgumentNullException>(() => new SvgConditionalProcessing().SelectSwitchWithSwitchElement(null));
            Assert.Throws<ArgumentNullException>(() => new SvgConditionalProcessing().SelectSwitchWithSwitchElementAndOptions(document.DocumentElement, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgConditionalProcessing().SelectSwitchWithSwitchElement(node));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
