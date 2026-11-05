namespace odf.Tests;

public class OdfOptionsAndResultsTests
{
    [Test]
    public void SecurityLimitsUseFinitePositiveDefaults()
    {
        OdfSecurityLimits limits = new OdfSecurityLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaximumPackageEntries, Is.EqualTo(10000));
            Assert.That(limits.MaximumTotalExpandedBytes, Is.EqualTo(1073741824));
            Assert.That(limits.MaximumSingleEntryBytes, Is.EqualTo(268435456));
            Assert.That(limits.MaximumSingleEntryBytes, Is.LessThanOrEqualTo(limits.MaximumTotalExpandedBytes));
            Assert.That(limits.MaximumCompressionRatio, Is.EqualTo(100));
            Assert.That(limits.MaximumPathLength, Is.EqualTo(4096));
            Assert.That(limits.MaximumXmlDepth, Is.EqualTo(256));
            Assert.That(limits.MaximumXmlAttributesPerElement, Is.EqualTo(1024));
            Assert.That(limits.MaximumXmlTextLength, Is.EqualTo(67108864));
            Assert.That(limits.MaximumFormulaDepth, Is.EqualTo(256));
            Assert.That(limits.MaximumEmbeddedPackageDepth, Is.EqualTo(8));
        });
    }

    [Test]
    public void LoadOptionsDefaultToCallerOwnershipPreservationAndValidation()
    {
        OdfLoadOptions options = new OdfLoadOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.SecurityLimits, Is.Not.Null);
            Assert.That(options.Cancellation, Is.Not.Null);
            Assert.That(options.StreamSelection, Is.Not.Null);
            Assert.That(options.LeaveInputOpen, Is.True);
            Assert.That(options.PreserveLexicalValues, Is.True);
            Assert.That(options.PreserveUnknownContent, Is.True);
            Assert.That(options.MaterializeXmlTrees, Is.True);
            Assert.That(options.IndexPackageEntries, Is.True);
            Assert.That(options.ValidateOnLoad, Is.True);
            Assert.That(options.AllowRecovery, Is.False);
        });
    }

    [Test]
    public void SaveOptionsDefaultToCallerOwnershipAndPreservationMode()
    {
        OdfSaveOptions options = new OdfSaveOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Cancellation, Is.Not.Null);
            Assert.That(options.LeaveOutputOpen, Is.True);
            Assert.That(options.PreserveUnknownContent, Is.True);
            Assert.That(options.PreserveLexicalValues, Is.True);
            Assert.That(options.PreserveTimestamps, Is.True);
            Assert.That(options.Deterministic, Is.False);
            Assert.That(options.IndentXml, Is.False);
            Assert.That(options.SortManifestEntries, Is.False);
            Assert.That(options.CompatibilityProfile, Is.Empty);
            Assert.That(options.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(options.TargetVersion, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(options.TargetConformanceClass, Is.EqualTo(OdfConformanceClass.Unknown));
        });
    }

    [Test]
    public void ValidationOptionsEnableAllConformanceLayersByDefault()
    {
        OdfValidationOptions options = new OdfValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(OdfValidationMode.Strict));
            Assert.That(options.SecurityLimits, Is.Not.Null);
            Assert.That(options.Cancellation, Is.Not.Null);
            Assert.That(options.Version, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(options.ConformanceClass, Is.EqualTo(OdfConformanceClass.Unknown));
            Assert.That(options.CompatibilityProfile, Is.Empty);
            Assert.That(options.ValidatePackage, Is.True);
            Assert.That(options.ValidateXml, Is.True);
            Assert.That(options.ValidateSemantics, Is.True);
            Assert.That(options.ValidateFormulas, Is.True);
            Assert.That(options.ValidateSecurity, Is.True);
            Assert.That(options.IncludeWarnings, Is.True);
            Assert.That(options.IncludeInformation, Is.False);
        });
    }

    [Test]
    public void MutableOptionStateIsIsolatedBetweenInstances()
    {
        OdfLoadOptions firstLoad = new OdfLoadOptions();
        OdfLoadOptions secondLoad = new OdfLoadOptions();
        OdfSaveOptions firstSave = new OdfSaveOptions();
        OdfSaveOptions secondSave = new OdfSaveOptions();
        OdfValidationOptions firstValidation = new OdfValidationOptions();
        OdfValidationOptions secondValidation = new OdfValidationOptions();

        firstLoad.SecurityLimits.MaximumPackageEntries = 1;
        firstLoad.Cancellation.IsCancellationRequested = true;
        firstLoad.StreamSelection.PartPaths = [OdfPartNames.Content];
        firstSave.Cancellation.Reason = "stop";
        firstValidation.SecurityLimits.MaximumFormulaDepth = 1;

        Assert.Multiple(() =>
        {
            Assert.That(secondLoad.SecurityLimits.MaximumPackageEntries, Is.EqualTo(10000));
            Assert.That(secondLoad.Cancellation.IsCancellationRequested, Is.False);
            Assert.That(secondLoad.StreamSelection.PartPaths, Is.Empty);
            Assert.That(secondSave.Cancellation.Reason, Is.Empty);
            Assert.That(secondValidation.SecurityLimits.MaximumFormulaDepth, Is.EqualTo(256));
            Assert.That(firstLoad.SecurityLimits, Is.Not.SameAs(secondLoad.SecurityLimits));
            Assert.That(firstLoad.Cancellation, Is.Not.SameAs(secondLoad.Cancellation));
            Assert.That(firstLoad.StreamSelection, Is.Not.SameAs(secondLoad.StreamSelection));
            Assert.That(firstSave.Cancellation, Is.Not.SameAs(secondSave.Cancellation));
            Assert.That(firstValidation.SecurityLimits, Is.Not.SameAs(secondValidation.SecurityLimits));
        });
    }

    [Test]
    public void LocationsUseExplicitUnavailableSentinels()
    {
        OdfLocation location = new OdfLocation();

        Assert.Multiple(() =>
        {
            Assert.That(location.PartPath, Is.Empty);
            Assert.That(location.NodePath, Is.Empty);
            Assert.That(location.XmlId, Is.Empty);
            Assert.That(location.Line, Is.EqualTo(-1));
            Assert.That(location.Column, Is.EqualTo(-1));
            Assert.That(location.ByteOffset, Is.EqualTo(-1));
            Assert.That(location.TextOffset, Is.EqualTo(-1));
            Assert.That(location.TextLength, Is.Zero);
        });
    }

    [Test]
    public void DiagnosticsHaveCompleteEmptySafeState()
    {
        OdfDiagnostic diagnostic = new OdfDiagnostic();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Information));
            Assert.That(diagnostic.Code, Is.Empty);
            Assert.That(diagnostic.Message, Is.Empty);
            Assert.That(diagnostic.Location, Is.Not.Null);
            Assert.That(diagnostic.RelatedLocation, Is.Not.Null);
            Assert.That(diagnostic.RelatedLocation, Is.Not.SameAs(diagnostic.Location));
            Assert.That(diagnostic.HasRelatedLocation, Is.False);
            Assert.That(diagnostic.Version, Is.EqualTo(OdfVersion.Unknown));
            Assert.That(diagnostic.SpecificationSection, Is.Empty);
            Assert.That(diagnostic.IsRecoverable, Is.False);
        });
    }

    [Test]
    public void EditResultsDoNotDefaultToSuccess()
    {
        OdfEditResult result = new OdfEditResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Failed));
            Assert.That(result.Changed, Is.False);
            Assert.That(result.AffectedCount, Is.Zero);
            Assert.That(result.Message, Is.Empty);
            Assert.That(result.Location, Is.Not.Null);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SearchMatches, Is.Empty);
        });
    }

    [Test]
    public void SearchMatchesStartEmptyAndCanCarryNodeAndLocationContext()
    {
        OdfSearchMatch match = new OdfSearchMatch();
        OdfNode node = new OdfNode
        {
            Kind = OdfNodeKind.Text
        };
        match.Location.PartPath = OdfPartNames.Content;
        match.Location.TextOffset = 12;
        match.Location.TextLength = 4;
        match.MatchedValue = "text";
        match.Context = "some text value";
        match.Node = node;
        match.IsExact = true;

        Assert.Multiple(() =>
        {
            Assert.That(match.Location.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(match.Location.TextOffset, Is.EqualTo(12));
            Assert.That(match.Location.TextLength, Is.EqualTo(4));
            Assert.That(match.MatchedValue, Is.EqualTo("text"));
            Assert.That(match.Context, Is.EqualTo("some text value"));
            Assert.That(match.Node, Is.SameAs(node));
            Assert.That(match.IsExact, Is.True);
        });
    }

    [Test]
    public void StreamSelectionDefaultsToAllMetadataEventsButNoBinaryPayloads()
    {
        OdfStreamSelection selection = new OdfStreamSelection();

        Assert.Multiple(() =>
        {
            Assert.That(selection.PartPaths, Is.Empty);
            Assert.That(selection.ElementNames, Is.Empty);
            Assert.That(selection.IncludePackageEntryEvents, Is.True);
            Assert.That(selection.IncludeElementEvents, Is.True);
            Assert.That(selection.IncludeTextEvents, Is.True);
            Assert.That(selection.IncludeCommentEvents, Is.True);
            Assert.That(selection.IncludeProcessingInstructionEvents, Is.True);
            Assert.That(selection.IncludeBinaryContent, Is.False);
            Assert.That(selection.SkipUnselectedSubtrees, Is.True);
        });
    }

    [Test]
    public void CancellationStateIsInitiallyInactiveAndCarriesCallerReason()
    {
        OdfCancellationState state = new OdfCancellationState();

        Assert.Multiple(() =>
        {
            Assert.That(state.IsCancellationRequested, Is.False);
            Assert.That(state.Reason, Is.Empty);
        });

        state.Reason = "user request";
        state.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(state.IsCancellationRequested, Is.True);
            Assert.That(state.Reason, Is.EqualTo("user request"));
        });
    }

    [Test]
    public void DocumentsOwnIndependentLoadAndSavePreferences()
    {
        OdfDocument first = new OdfDocument();
        OdfDocument second = new OdfDocument();

        first.LoadOptions.AllowRecovery = true;
        first.SaveOptions.Deterministic = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.LoadOptions, Is.Not.SameAs(second.LoadOptions));
            Assert.That(first.SaveOptions, Is.Not.SameAs(second.SaveOptions));
            Assert.That(second.LoadOptions.AllowRecovery, Is.False);
            Assert.That(second.SaveOptions.Deterministic, Is.False);
        });
    }

    [Test]
    public void ResultCollectionsAndLocationsAreIsolatedBetweenInstances()
    {
        OdfEditResult firstEdit = new OdfEditResult();
        OdfEditResult secondEdit = new OdfEditResult();
        OdfDiagnostic firstDiagnostic = new OdfDiagnostic();
        OdfDiagnostic secondDiagnostic = new OdfDiagnostic();
        OdfStreamSelection firstSelection = new OdfStreamSelection();
        OdfStreamSelection secondSelection = new OdfStreamSelection();

        firstEdit.Diagnostics = [new OdfDiagnostic()];
        firstEdit.SearchMatches = [new OdfSearchMatch()];
        firstEdit.Location.Line = 4;
        firstDiagnostic.Location.PartPath = OdfPartNames.Styles;
        firstSelection.PartPaths = [OdfPartNames.Content];
        firstSelection.ElementNames = [new OdfQualifiedName { LocalName = "p" }];

        Assert.Multiple(() =>
        {
            Assert.That(secondEdit.Diagnostics, Is.Empty);
            Assert.That(secondEdit.SearchMatches, Is.Empty);
            Assert.That(secondEdit.Location.Line, Is.EqualTo(-1));
            Assert.That(secondDiagnostic.Location.PartPath, Is.Empty);
            Assert.That(secondSelection.PartPaths, Is.Empty);
            Assert.That(secondSelection.ElementNames, Is.Empty);
            Assert.That(firstEdit.Location, Is.Not.SameAs(secondEdit.Location));
            Assert.That(firstDiagnostic.Location, Is.Not.SameAs(secondDiagnostic.Location));
            Assert.That(firstSelection.PartPaths, Is.Not.SameAs(secondSelection.PartPaths));
            Assert.That(firstSelection.ElementNames, Is.Not.SameAs(secondSelection.ElementNames));
        });
    }
}
