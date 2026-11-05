using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using fix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace fix.Tests;

[TestFixture]
public class GeneratedModelRuntimeTests
{
    [Test]
    public void GeneratedModelRoundTripsThroughWireValidationAndGeneratedCracker()
    {
        FixDictionary dictionary = CreateDictionary();
        GenerationResult generated = new CSharpModelGenerator().GenerateModels(dictionary, new ModelGenerationOptions
        {
            Namespace = "Venue.Runtime",
            MessageNames = ["NewOrderSingle"],
            Scenario = "base"
        });
        Assembly assembly = CompileAndLoad(generated.Files, HandlerSource());
        object operations = Create(assembly, "Venue.Runtime.Fix44.Operations.NewOrderSingleOperations");

        object model = Invoke(operations, "CreateNewOrderSingleModel");
        FixMessage message = ModelMessage(model);
        message.BeginString = FixBeginStrings.Fix44;
        message.HeaderFields.Add(Field(49, "SENDER"));
        message.BodyFields.Add(Field(11, "ORDER-1"));
        message.BodyFields.Add(Field(54, "1"));

        FixMessage built = (FixMessage)Invoke(operations, "BuildNewOrderSingleMessage", model);
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(built, dictionary, Span<byte>.Empty, new EncodeOptions());
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(built, dictionary, destination, new EncodeOptions());
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(destination, dictionary, new DecodeOptions());
        ValidationResult validation = new FixMessageValidator().ValidateMessage(decoded.Message!, dictionary, new ValidationOptions(), new ValidationContext { Scenario = "base" });

        object decodedModel = Invoke(operations, "ReadNewOrderSingleMessage", decoded.Message!);
        object handler = Create(assembly, "Venue.Runtime.Fix44.Cracking.RuntimeGeneratedMessageHandler");
        object cracker = Create(assembly, "Venue.Runtime.Fix44.Cracking.GeneratedMessageCracker");
        ApplicationDecision decision = (ApplicationDecision)Invoke(cracker, "CrackGeneratedMessage", decoded.Message!, handler);
        ApplicationDecision unsupported = (ApplicationDecision)Invoke(cracker, "CrackGeneratedMessage", new FixMessage { MsgType = "F" }, handler);

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True, DiagnosticText(generated));
            Assert.That(message.MsgType, Is.EqualTo("D"));
            Assert.That(built, Is.SameAs(message));
            Assert.That(measured.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(Text(decoded.Message.BodyFields.Single(item => item.Tag == 11)), Is.EqualTo("ORDER-1"));
            Assert.That(Text(decoded.Message.BodyFields.Single(item => item.Tag == 54)), Is.EqualTo("1"));
            Assert.That(validation.IsValid, Is.True, string.Join(",", validation.Issues.Select(item => item.Code)));
            Assert.That(ModelMessage(decodedModel), Is.SameAs(decoded.Message));
            Assert.That(decision.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(decision.Code, Is.EqualTo("generated-handler-accepted"));
            Assert.That((FixMessage)FieldValue(handler, "CapturedMessage"), Is.SameAs(decoded.Message));
            Assert.That((int)FieldValue(handler, "Calls"), Is.EqualTo(1));
            Assert.That(unsupported.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(unsupported.Code, Is.EqualTo("unsupported-generated-message"));
        });
    }

    private static Assembly CompileAndLoad(List<GeneratedFile> files, string handlerSource)
    {
        List<SyntaxTree> trees = files.Select(file => CSharpSyntaxTree.ParseText(file.Content, path: file.Path)).ToList();
        trees.Add(CSharpSyntaxTree.ParseText(handlerSource, path: "RuntimeGeneratedMessageHandler.cs"));
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Venue.Runtime.GeneratedModels",
            trees,
            CompilationReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(output);
        Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
        output.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(output);
    }

    private static List<MetadataReference> CompilationReferences()
    {
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        List<MetadataReference> references = trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(FixMessage).Assembly.Location));
        return references;
    }

    private static object Create(Assembly assembly, string typeName)
    {
        return Activator.CreateInstance(assembly.GetType(typeName, true)!)!;
    }

    private static object Invoke(object target, string methodName, params object[] arguments)
    {
        return target.GetType().GetMethod(methodName)!.Invoke(target, arguments)!;
    }

    private static FixMessage ModelMessage(object model)
    {
        return (FixMessage)FieldValue(model, "Message");
    }

    private static object FieldValue(object target, string fieldName)
    {
        return target.GetType().GetField(fieldName)!.GetValue(target)!;
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static string Text(FixField field)
    {
        return Encoding.Latin1.GetString(field.Value);
    }

    private static string DiagnosticText(GenerationResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }

    private static string HandlerSource()
    {
        return """
            namespace Venue.Runtime.Fix44.Cracking;

            public class RuntimeGeneratedMessageHandler : IGeneratedMessageHandler
            {
                public global::fix.FixMessage CapturedMessage = new global::fix.FixMessage();
                public int Calls;

                public global::fix.ApplicationDecision HandleNewOrderSingleMessage(global::Venue.Runtime.Fix44.Messages.NewOrderSingle model)
                {
                    CapturedMessage = model.Message;
                    Calls = Calls + 1;
                    global::fix.ApplicationDecision decision = new global::fix.ApplicationDecision();
                    decision.Disposition = global::fix.MessageDisposition.Accept;
                    decision.Code = "generated-handler-accepted";
                    return decision;
                }
            }
            """;
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 },
            Fields =
            [
                Definition(8, "BeginString", FixFieldType.String),
                Definition(9, "BodyLength", FixFieldType.Length),
                Definition(10, "CheckSum", FixFieldType.String),
                Definition(11, "ClOrdID", FixFieldType.String),
                Definition(35, "MsgType", FixFieldType.String),
                Definition(49, "SenderCompID", FixFieldType.String),
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCode" }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCode",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
                }
            ],
            Header = [Member(49, "SenderCompID", FixPresence.Required, 0)],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 100,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Category = "Orders",
                    Scenario = "base",
                    Members =
                    [
                        Member(11, "ClOrdID", FixPresence.Required, 0),
                        Member(54, "Side", FixPresence.Required, 1)
                    ]
                }
            ],
            Scenarios = [new FixScenarioDefinition { Name = "base" }]
        };
    }

    private static FixFieldDefinition Definition(int tag, string name, FixFieldType fieldType)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = fieldType };
    }

    private static FixMemberDefinition Member(int tag, string name, FixPresence presence, int position)
    {
        return new FixMemberDefinition
        {
            Name = name,
            Kind = FixMemberKind.Field,
            ReferenceId = tag,
            Presence = presence,
            Position = position
        };
    }
}
