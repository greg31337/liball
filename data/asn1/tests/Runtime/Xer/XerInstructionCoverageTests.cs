using asn1;

namespace asn1.Tests;

public class XerInstructionCoverageTests
{
    [Test]
    public void EveryNegatedXerInstructionLeavesTheRepresentationUnmodified()
    {
        AsnInstructionKind[] kinds =
        [
            AsnInstructionKind.AnyAttributes,
            AsnInstructionKind.AnyElement,
            AsnInstructionKind.Attribute,
            AsnInstructionKind.Base64,
            AsnInstructionKind.Decimal,
            AsnInstructionKind.DefaultForEmpty,
            AsnInstructionKind.Element,
            AsnInstructionKind.EmbedValues,
            AsnInstructionKind.GlobalDefaults,
            AsnInstructionKind.List,
            AsnInstructionKind.Name,
            AsnInstructionKind.Namespace,
            AsnInstructionKind.PiOrComment,
            AsnInstructionKind.Text,
            AsnInstructionKind.Untagged,
            AsnInstructionKind.UseNil,
            AsnInstructionKind.UseNumber,
            AsnInstructionKind.UseOrder,
            AsnInstructionKind.UseQName,
            AsnInstructionKind.UseType,
            AsnInstructionKind.UseUnion,
            AsnInstructionKind.Whitespace
        ];

        int index = 0;
        while (index < kinds.Length)
        {
            AsnTypeDescriptor type = new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Integer,
                Name = "Number",
                Encodings =
                [
                    new AsnEncodingDescriptor
                    {
                        EncodingRules = AsnEncodingRules.ExtendedXer,
                        EncodingReference = "XER",
                        Instructions =
                        [
                            new AsnEncodingInstruction
                            {
                                Kind = kinds[index],
                                Name = kinds[index].ToString().ToUpperInvariant(),
                                Negated = true,
                                Arguments = ["intentionally", "ignored"]
                            }
                        ]
                    }
                ]
            };
            string xml = EncodeXml(type, Integer(7));
            XerValueResult decoded = DecodeXml(type, xml);

            Assert.Multiple(() =>
            {
                Assert.That(xml,
                    Is.EqualTo("<Number>7</Number>"),
                    kinds[index].ToString());
                Assert.That(decoded.Success,
                    Is.True,
                    kinds[index] + ": " + decoded.ErrorMessage);
                Assert.That(decoded.Value?.IntegerValue,
                    Is.EqualTo(new System.Numerics.BigInteger(7)),
                    kinds[index].ToString());
            });
            index = index + 1;
        }
    }

    [Test]
    public void EveryModifiedInstructionCombinationMatchesThePolicyMatrix()
    {
        AsnInstructionKind[] kinds = EffectiveKinds();
        XerInstructionResolver resolver = Resolver();

        int leftIndex = 0;
        while (leftIndex < kinds.Length)
        {
            int rightIndex = 0;
            while (rightIndex < kinds.Length)
            {
                AsnInstructionKind left = kinds[leftIndex];
                AsnInstructionKind right = kinds[rightIndex];
                bool expected = ExpectedModifiedCombination(left, right);

                Assert.That(resolver.IsModifiedCombinationAllowed(left, right),
                    Is.EqualTo(expected),
                    left + " + " + right);
                Assert.That(resolver.IsModifiedCombinationAllowed(right, left),
                    Is.EqualTo(expected),
                    right + " + " + left);
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    [Test]
    public void EveryUnmodifiedInstructionCombinationMatchesThePolicyMatrix()
    {
        AsnInstructionKind[] kinds = EffectiveKinds();
        XerInstructionResolver resolver = Resolver();

        int leftIndex = 0;
        while (leftIndex < kinds.Length)
        {
            int rightIndex = 0;
            while (rightIndex < kinds.Length)
            {
                AsnInstructionKind left = kinds[leftIndex];
                AsnInstructionKind right = kinds[rightIndex];
                bool expected = ExpectedUnmodifiedCombination(left, right);

                Assert.That(resolver.IsUnmodifiedCombinationAllowed(left, right),
                    Is.EqualTo(expected),
                    left + " + " + right);
                Assert.That(resolver.IsUnmodifiedCombinationAllowed(right, left),
                    Is.EqualTo(expected),
                    right + " + " + left);
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    [Test]
    public void CompiledGlobalsTargetsNegationAndElementReachRuntime()
    {
        AsnCompilationResult compilation = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    new AsnSourceUnit
                    {
                        Name = "xer-controls.asn",
                        Text = "Controls DEFINITIONS ::= BEGIN\n"
                            + "Base ::= [XER:ATTRIBUTE] INTEGER;\n"
                            + "Value ::= [XER:NOT ATTRIBUTE] Base;\n"
                            + "Wrapped ::= [XER:ELEMENT] [XER:UNTAGGED] INTEGER;\n"
                            + "Container ::= SEQUENCE { value Value, "
                            + "numbers SEQUENCE OF INTEGER }\n"
                            + "ENCODING-CONTROL XER\n"
                            + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS]\n"
                            + "[GLOBAL-DEFAULTS CONTROL-NAMESPACE "
                            + "\"urn:control\" PREFIX \"xc\"]\n"
                            + "[ATTRIBUTE] Container.value\n"
                            + "[NOT ATTRIBUTE] Container.value\n"
                            + "[LIST] Container.numbers\n"
                            + "END"
                    }
                ]
            });
        AsnReflectionCatalog catalog = compilation.ReflectionCatalog
            ?? throw new System.InvalidOperationException();
        AsnAssignmentDescriptor container = catalog.Assignments.Single(
            item => item.Name == "Container");
        AsnAssignmentDescriptor wrapped = catalog.Assignments.Single(
            item => item.Name == "Wrapped");
        AsnTypeDescriptor containerType = container.Type
            ?? throw new System.InvalidOperationException();
        AsnTypeDescriptor wrappedType = wrapped.Type
            ?? throw new System.InvalidOperationException();
        AsnFieldDescriptor valueField = containerType.Fields.Single(
            item => item.Name == "value");
        AsnFieldDescriptor numbersField = containerType.Fields.Single(
            item => item.Name == "numbers");
        AsnEncodingInstruction[] moduleInstructions = catalog.Modules.Single()
            .Encodings.Single().Instructions;
        AsnCodecOptions options = new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.ExtendedXer,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false
        };
        XerSchemaCodec codec = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer);
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Controls.Container",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("value", Integer(7)),
                    Present("numbers", new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.SequenceOf,
                        Items = [Integer(1), Integer(2)]
                    })
                ]
            },
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Controls.Container",
            Data = encoded.Data,
            Options = options
        });
        AsnEncodeResult wrappedEncoding = codec.EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Controls.Wrapped",
                Value = Integer(8),
                Options = options
            });

        Assert.Multiple(() =>
        {
            Assert.That(compilation.Success,
                Is.True,
                string.Join(" | ", compilation.Diagnostics.Select(
                    item => item.Code + " " + item.Message)));
            Assert.That(moduleInstructions.Count(item =>
                item.Kind == AsnInstructionKind.GlobalDefaults),
                Is.EqualTo(2));
            Assert.That(moduleInstructions.Single(item =>
                item.Kind == AsnInstructionKind.GlobalDefaults
                    && item.Arguments.Contains("CONTROL-NAMESPACE"))
                .Arguments,
                Does.Contain("\"urn:control\""));
            Assert.That(valueField.Type?.Encodings, Is.Empty);
            Assert.That(numbersField.Type?.Encodings.Single().Instructions
                .Select(item => item.Kind),
                Is.EqualTo(new[] { AsnInstructionKind.List }));
            Assert.That(wrappedType.Encodings, Is.Empty);
            Assert.That(System.Text.Encoding.UTF8.GetString(encoded.Data),
                Is.EqualTo(
                    "<Container><value>7</value><numbers>1 2</numbers></Container>"));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(decoded.Value?.Fields[1].Value?.Items.Select(
                item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                wrappedEncoding.Data), Is.EqualTo("<Wrapped>8</Wrapped>"));
        });
    }

    private static bool ExpectedModifiedCombination(
        AsnInstructionKind left,
        AsnInstructionKind right)
    {
        if (left == AsnInstructionKind.Name
            || left == AsnInstructionKind.Namespace
            || right == AsnInstructionKind.Name
            || right == AsnInstructionKind.Namespace)
        {
            return true;
        }

        return IsPair(left, right,
            "Attribute:Base64,Attribute:Decimal,Attribute:List,"
                + "Attribute:Text,Attribute:UseNumber,Attribute:UseQName,"
                + "Attribute:UseUnion,Attribute:Whitespace,"
                + "Base64:DefaultForEmpty,Base64:PiOrComment,Base64:Untagged,"
                + "Decimal:DefaultForEmpty,Decimal:PiOrComment,Decimal:Untagged,"
                + "DefaultForEmpty:EmbedValues,DefaultForEmpty:List,"
                + "DefaultForEmpty:PiOrComment,DefaultForEmpty:Text,"
                + "DefaultForEmpty:UseNil,DefaultForEmpty:UseNumber,"
                + "DefaultForEmpty:UseOrder,DefaultForEmpty:UseQName,"
                + "DefaultForEmpty:UseUnion,DefaultForEmpty:Whitespace,"
                + "EmbedValues:PiOrComment,EmbedValues:UseNil,"
                + "EmbedValues:UseOrder,List:PiOrComment,List:Untagged,"
                + "PiOrComment:Text,PiOrComment:UseNil,"
                + "PiOrComment:UseNumber,PiOrComment:UseOrder,"
                + "PiOrComment:UseQName,PiOrComment:UseType,"
                + "PiOrComment:UseUnion,PiOrComment:Whitespace,"
                + "Text:Untagged,Untagged:UseNumber,Untagged:UseQName,"
                + "Untagged:UseUnion,Untagged:Whitespace,UseNil:UseOrder");
    }

    private static bool ExpectedUnmodifiedCombination(
        AsnInstructionKind left,
        AsnInstructionKind right)
    {
        return IsPair(left, right,
            "Attribute:Base64,Attribute:List,Attribute:Name,Attribute:Text,"
                + "Attribute:UseNumber,Attribute:Whitespace,Base64:Name,"
                + "Base64:PiOrComment,List:Name,List:PiOrComment,"
                + "Name:PiOrComment,Name:Text,Name:UseNumber,"
                + "Name:Whitespace,PiOrComment:Text,PiOrComment:UseNumber,"
                + "PiOrComment:Whitespace");
    }

    private static bool IsPair(
        AsnInstructionKind left,
        AsnInstructionKind right,
        string pairs)
    {
        string forward = left + ":" + right;
        string reverse = right + ":" + left;
        string[] values = pairs.Split(',');
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == forward || values[index] == reverse)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    private static AsnInstructionKind[] EffectiveKinds()
    {
        return
        [
            AsnInstructionKind.AnyAttributes,
            AsnInstructionKind.AnyElement,
            AsnInstructionKind.Attribute,
            AsnInstructionKind.Base64,
            AsnInstructionKind.Decimal,
            AsnInstructionKind.DefaultForEmpty,
            AsnInstructionKind.EmbedValues,
            AsnInstructionKind.List,
            AsnInstructionKind.Name,
            AsnInstructionKind.Namespace,
            AsnInstructionKind.PiOrComment,
            AsnInstructionKind.Text,
            AsnInstructionKind.Untagged,
            AsnInstructionKind.UseNil,
            AsnInstructionKind.UseNumber,
            AsnInstructionKind.UseOrder,
            AsnInstructionKind.UseQName,
            AsnInstructionKind.UseType,
            AsnInstructionKind.UseUnion,
            AsnInstructionKind.Whitespace
        ];
    }

    private static XerInstructionResolver Resolver()
    {
        return new XerInstructionResolver(
            new DiagnosticBag(false, new AsnResourceLimits()));
    }

    private static string EncodeXml(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnResourceLimits limits = Limits();
        XerValueCodec codec = new XerValueCodec(limits);
        codec.Extended = true;
        XerValueResult encoded = codec.EncodeValue(type, value);
        Assert.That(encoded.Success, Is.True, encoded.ErrorMessage);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, limits);
        int index = 0;
        while (index < encoded.Tokens.Length)
        {
            XerXmlWriteResult written = writer.WriteToken(encoded.Tokens[index]);
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            index = index + 1;
        }
        Assert.That(writer.CompleteWriter().Success, Is.True);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static XerValueResult DecodeXml(
        AsnTypeDescriptor type,
        string xml)
    {
        AsnResourceLimits limits = Limits();
        XerXmlReadResult read = new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
        Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
        XerValueCodec codec = new XerValueCodec(limits);
        codec.Extended = true;
        return codec.DecodeValue(type, read.Tokens, 0, read.Tokens.Length);
    }

    private static AsnRuntimeFieldValue Present(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 64,
            MaximumCollectionElements = 1024,
            MaximumTextCharacters = 65536,
            MaximumOpenTypeBytes = 65536,
            MaximumTotalAllocationBytes = 1048576
        };
    }
}
