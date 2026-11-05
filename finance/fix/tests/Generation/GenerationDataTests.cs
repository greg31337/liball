using fix;

namespace fix.Tests;

[TestFixture]
public class GenerationDataTests
{
    [Test]
    public void GenerationResultRetainsDeterministicFilesManifestAndDiagnostics()
    {
        ModelGenerationOptions options = new ModelGenerationOptions
        {
            Target = GenerationTarget.CSharp,
            Namespace = "Venue.Generated",
            TypePrefix = "Venue",
            Version = "FIX.Latest EP309",
            Scenario = "base",
            CategoryNames = ["Orders"],
            MessageNames = ["NewOrderSingle", "ExecutionReport"],
            FieldTags = [11, 17, 37],
            PreviousManifest = "prior"
        };
        GeneratedFile first = new GeneratedFile
        {
            Path = "Fields/ClOrdID.cs",
            Content = "namespace Venue.Generated;",
            ContentHash = "abc",
            IsUnchanged = true
        };
        GeneratedFile second = new GeneratedFile
        {
            Path = "Messages/NewOrderSingle.cs",
            Content = "namespace Venue.Generated;",
            ContentHash = "def"
        };
        GenerationDiagnostic warning = new GenerationDiagnostic
        {
            Severity = ValidationSeverity.Warning,
            Code = "identifier-normalized",
            Message = "A source name was normalized.",
            Source = "fields/11"
        };
        GenerationResult result = new GenerationResult
        {
            Success = true,
            Files = [first, second],
            Diagnostics = [warning],
            Manifest = "abc Fields/ClOrdID.cs\ndef Messages/NewOrderSingle.cs"
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.Namespace, Is.EqualTo("Venue.Generated"));
            Assert.That(options.Version, Is.EqualTo("FIX.Latest EP309"));
            Assert.That(options.CategoryNames, Is.EqualTo(new[] { "Orders" }));
            Assert.That(options.MessageNames[0], Is.EqualTo("NewOrderSingle"));
            Assert.That(options.FieldTags, Is.EqualTo(new[] { 11, 17, 37 }));
            Assert.That(options.EmitDocumentation, Is.True);
            Assert.That(options.DetectUnchangedFiles, Is.True);
            Assert.That(options.PreviousManifest, Is.EqualTo("prior"));
            Assert.That(result.Files[0], Is.SameAs(first));
            Assert.That(result.Files[1], Is.SameAs(second));
            Assert.That(result.Diagnostics[0], Is.SameAs(warning));
            Assert.That(result.Files[0].IsUnchanged, Is.True);
            Assert.That(result.Manifest, Does.Contain("NewOrderSingle.cs"));
        });
    }

    [Test]
    public void SbeAndBinaryModelsRetainEncodingTemplateAndExplicitOutcomes()
    {
        SbeGenerationOptions sbe = new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 7,
            FirstTemplateId = 100,
            LittleEndian = true,
            Scenario = "venue"
        };
        BinaryMessageContext context = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = FixEncodingTypes.SbeVersion20LittleEndian,
            TemplateId = 101,
            SchemaId = 42,
            Version = 7,
            MsgType = "D",
            DictionaryIdentity = new FixDictionaryIdentity { Name = "Venue", ApplicationVersion = FixApplicationVersions.FixLatest }
        };
        FixMessage message = new FixMessage { MsgType = "D" };
        ValidationIssue diagnostic = new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Layer = ValidationLayer.Datatype,
            Code = "precision-reduced"
        };
        BinaryDecodeResult decode = new BinaryDecodeResult
        {
            Status = DecodeStatus.Success,
            Message = message,
            Context = context,
            Consumed = 64,
            Required = 64,
            Diagnostics = [diagnostic]
        };
        BinaryEncodeResult encode = new BinaryEncodeResult
        {
            Status = EncodeStatus.DestinationTooSmall,
            Context = context,
            RequiredSize = 64,
            ErrorCode = "destination-too-small"
        };

        Assert.Multiple(() =>
        {
            Assert.That(sbe.SchemaId, Is.EqualTo(42));
            Assert.That(sbe.FirstTemplateId, Is.EqualTo(100));
            Assert.That(sbe.LittleEndian, Is.True);
            Assert.That(sbe.EmitSemanticTypes, Is.True);
            Assert.That(context.EncodingIdentifier, Is.EqualTo(FixEncodingTypes.SbeVersion20LittleEndian));
            Assert.That(context.TemplateId, Is.EqualTo(101));
            Assert.That(context.DictionaryIdentity.Name, Is.EqualTo("Venue"));
            Assert.That(decode.Message, Is.SameAs(message));
            Assert.That(decode.Context, Is.SameAs(context));
            Assert.That(decode.Consumed, Is.EqualTo(64));
            Assert.That(decode.Diagnostics[0], Is.SameAs(diagnostic));
            Assert.That(encode.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(encode.Context, Is.SameAs(context));
            Assert.That(encode.RequiredSize, Is.EqualTo(64));
            Assert.That(encode.ErrorCode, Is.EqualTo("destination-too-small"));
        });
    }
}
