namespace odf.Tests;

public class EnumConsumerTests
{
    [Test]
    public void EnumBearingDataCanBePopulatedAndAggregated()
    {
        OdfVersionInfo version = new OdfVersionInfo
        {
            Version = OdfVersion.Version14,
            ConformanceClass = OdfConformanceClass.Extended
        };
        OdfPackageEntry packageEntry = new OdfPackageEntry
        {
            CompressionMethod = OdfCompressionMethod.Deflated
        };
        OdfNode node = new OdfNode
        {
            Kind = OdfNodeKind.ProcessingInstruction
        };
        OdfCellValue cellValue = new OdfCellValue
        {
            Kind = OdfValueKind.Array,
            Provenance = OdfValueProvenance.Synthesized,
            UnitKind = OdfUnitKind.Percentage
        };
        OdfStyle style = new OdfStyle
        {
            Family = OdfStyleFamily.Ruby
        };
        OdfFormulaToken token = new OdfFormulaToken
        {
            Kind = OdfFormulaTokenKind.Function
        };
        OdfFormulaNode formulaNode = new OdfFormulaNode
        {
            Kind = OdfFormulaNodeKind.FunctionCall
        };
        OdfFormulaResult formulaResult = new OdfFormulaResult
        {
            Error = OdfFormulaError.CircularReference
        };
        OdfFormula formula = new OdfFormula
        {
            Conformance = OdfFormulaConformance.Large,
            Tokens = [token],
            Root = formulaNode,
            Result = formulaResult
        };
        OdfDrawingObject drawing = new OdfDrawingObject
        {
            Kind = OdfDrawingKind.EmbeddedObject
        };
        OdfChart chart = new OdfChart
        {
            Kind = OdfChartKind.Gantt
        };
        OdfAnimation animation = new OdfAnimation
        {
            Kind = OdfAnimationKind.TransitionFilter
        };
        OdfSlideTransition transition = new OdfSlideTransition
        {
            Kind = OdfTransitionKind.SweepWipe
        };
        OdfDiagnostic diagnostic = new OdfDiagnostic
        {
            Severity = OdfValidationSeverity.Fatal
        };
        OdfEncryptionInfo encryption = new OdfEncryptionInfo
        {
            Method = OdfEncryptionMethod.Unsupported
        };
        OdfSignature signature = new OdfSignature
        {
            Status = OdfSignatureStatus.Error
        };
        OdfChange change = new OdfChange
        {
            Kind = OdfChangeKind.FormatChange
        };
        OdfAnnotation annotation = new OdfAnnotation
        {
            State = OdfAnnotationState.Resolved
        };
        OdfTextField textField = new OdfTextField
        {
            Kind = OdfTextFieldKind.Metadata
        };
        OdfFormControl formControl = new OdfFormControl
        {
            Kind = OdfFormControlKind.Label
        };
        OdfEditResult editResult = new OdfEditResult
        {
            Status = OdfEditStatus.Failed
        };
        OdfDocument document = new OdfDocument
        {
            VersionInfo = version,
            Kind = OdfDocumentKind.ImageTemplate,
            PackageKind = OdfPackageKind.Flat,
            PackageEntries = [packageEntry],
            Nodes = [node],
            CellValues = [cellValue],
            Styles = [style],
            Formulas = [formula],
            DrawingObjects = [drawing],
            Charts = [chart],
            Animations = [animation],
            SlideTransitions = [transition],
            Diagnostics = [diagnostic],
            EncryptionEntries = [encryption],
            Signatures = [signature],
            Changes = [change],
            Annotations = [annotation],
            TextFields = [textField],
            FormControls = [formControl],
            EditResults = [editResult]
        };
        OdfStreamEvent streamEvent = new OdfStreamEvent
        {
            Kind = OdfStreamEventKind.EndOfStream
        };
        OdfValidationOptions validationOptions = new OdfValidationOptions
        {
            Mode = OdfValidationMode.Profile
        };

        Assert.Multiple(() =>
        {
            Assert.That(document.VersionInfo, Is.SameAs(version));
            Assert.That(version.Version, Is.EqualTo(OdfVersion.Version14));
            Assert.That(version.ConformanceClass, Is.EqualTo(OdfConformanceClass.Extended));
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.ImageTemplate));
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(document.PackageEntries, Is.EqualTo(new[] { packageEntry }));
            Assert.That(packageEntry.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
            Assert.That(node.Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(streamEvent.Kind, Is.EqualTo(OdfStreamEventKind.EndOfStream));
            Assert.That(cellValue.Kind, Is.EqualTo(OdfValueKind.Array));
            Assert.That(cellValue.Provenance, Is.EqualTo(OdfValueProvenance.Synthesized));
            Assert.That(cellValue.UnitKind, Is.EqualTo(OdfUnitKind.Percentage));
            Assert.That(style.Family, Is.EqualTo(OdfStyleFamily.Ruby));
            Assert.That(formula.Conformance, Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(token.Kind, Is.EqualTo(OdfFormulaTokenKind.Function));
            Assert.That(formulaNode.Kind, Is.EqualTo(OdfFormulaNodeKind.FunctionCall));
            Assert.That(formulaResult.Error, Is.EqualTo(OdfFormulaError.CircularReference));
            Assert.That(drawing.Kind, Is.EqualTo(OdfDrawingKind.EmbeddedObject));
            Assert.That(chart.Kind, Is.EqualTo(OdfChartKind.Gantt));
            Assert.That(animation.Kind, Is.EqualTo(OdfAnimationKind.TransitionFilter));
            Assert.That(transition.Kind, Is.EqualTo(OdfTransitionKind.SweepWipe));
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Fatal));
            Assert.That(validationOptions.Mode, Is.EqualTo(OdfValidationMode.Profile));
            Assert.That(encryption.Method, Is.EqualTo(OdfEncryptionMethod.Unsupported));
            Assert.That(signature.Status, Is.EqualTo(OdfSignatureStatus.Error));
            Assert.That(change.Kind, Is.EqualTo(OdfChangeKind.FormatChange));
            Assert.That(annotation.State, Is.EqualTo(OdfAnnotationState.Resolved));
            Assert.That(textField.Kind, Is.EqualTo(OdfTextFieldKind.Metadata));
            Assert.That(formControl.Kind, Is.EqualTo(OdfFormControlKind.Label));
            Assert.That(editResult.Status, Is.EqualTo(OdfEditStatus.Failed));
        });
    }

    [Test]
    public void AggregateCollectionsStartEmpty()
    {
        OdfDocument document = new OdfDocument();
        OdfFormula formula = new OdfFormula();

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageEntries, Is.Empty);
            Assert.That(document.Nodes, Is.Empty);
            Assert.That(document.CellValues, Is.Empty);
            Assert.That(document.Styles, Is.Empty);
            Assert.That(document.Formulas, Is.Empty);
            Assert.That(document.DrawingObjects, Is.Empty);
            Assert.That(document.Charts, Is.Empty);
            Assert.That(document.Animations, Is.Empty);
            Assert.That(document.SlideTransitions, Is.Empty);
            Assert.That(document.Diagnostics, Is.Empty);
            Assert.That(document.EncryptionEntries, Is.Empty);
            Assert.That(document.Signatures, Is.Empty);
            Assert.That(document.Changes, Is.Empty);
            Assert.That(document.Annotations, Is.Empty);
            Assert.That(document.TextFields, Is.Empty);
            Assert.That(document.FormControls, Is.Empty);
            Assert.That(document.EditResults, Is.Empty);
            Assert.That(formula.Tokens, Is.Empty);
        });
    }
}
