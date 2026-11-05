using asn1;

namespace asn1.Tests;

public class XerSchemaCodecTests
{
    [Test]
    public void RulesSelectBasicCanonicalAndCompleteExtendedInstructionModes()
    {
        AsnTypeDescriptor state = StateType();
        AsnReflectionCatalog catalog = Catalog(state);
        AsnRuntimeValue value = State("on", 7);

        AsnEncodeResult basic = Encode(
            catalog,
            AsnEncodingRules.BasicXer,
            value);
        AsnEncodeResult canonical = Encode(
            catalog,
            AsnEncodingRules.CanonicalXer,
            value);
        AsnEncodeResult extended = Encode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            value);
        AsnDecodeResult basicDecoded = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            basic.Data);
        AsnDecodeResult canonicalDecoded = Decode(
            catalog,
            AsnEncodingRules.CanonicalXer,
            canonical.Data);
        AsnDecodeResult extendedDecoded = Decode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            extended.Data);

        Assert.Multiple(() =>
        {
            Assert.That(Xml(basic.Data),
                Is.EqualTo("<State><on/></State>"));
            Assert.That(Xml(canonical.Data),
                Is.EqualTo("<State><on/></State>"));
            Assert.That(Xml(extended.Data),
                Is.EqualTo("<!--extended--><state>7</state>"));
            Assert.That(basicDecoded.Value?.TextValue, Is.EqualTo("on"));
            Assert.That(canonicalDecoded.Value?.TextValue, Is.EqualTo("on"));
            Assert.That(extendedDecoded.Value?.TextValue, Is.EqualTo("on"));
            Assert.That(extendedDecoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
        });
    }

    [Test]
    public void ExtendedRepresentationIsNotAcceptedAsBasicOrCanonicalXer()
    {
        AsnTypeDescriptor state = StateType();
        AsnReflectionCatalog catalog = Catalog(state);
        byte[] extended = Encode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            State("on", 7)).Data;

        AsnDecodeResult basic = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            extended);
        AsnDecodeResult canonical = Decode(
            catalog,
            AsnEncodingRules.CanonicalXer,
            extended);

        Assert.Multiple(() =>
        {
            Assert.That(basic.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(canonical.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(basic.Diagnostics, Is.Not.Empty);
            Assert.That(canonical.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void PreservedOpenValuesRemainBoundToTheirXerMode()
    {
        AsnTypeDescriptor open = new AsnTypeDescriptor
        {
            StableId = "type.Open",
            Name = "Open",
            Kind = AsnTypeKind.OpenType
        };
        AsnReflectionCatalog catalog = Catalog(open);
        byte[] xml = System.Text.Encoding.UTF8.GetBytes(
            "<Open><Future/></Open>");

        AsnDecodeResult basic = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            xml);
        AsnDecodeResult extended = Decode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            xml);
        AsnRuntimeValue extendedValue = extended.Value == null
            ? throw new AssertionException("Extended XER did not decode a value.")
            : extended.Value;
        AsnEncodeResult extendedAgain = Encode(
            catalog,
            AsnEncodingRules.ExtendedXer,
            extendedValue);
        AsnEncodeResult wrongMode = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.BasicXer).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Open",
                Value = extendedValue,
                Options = Options(AsnEncodingRules.BasicXer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(basic.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(basic.Value?.OpenTypeValue?.EncodingRules,
                Is.EqualTo(AsnEncodingRules.BasicXer));
            Assert.That(extended.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(extended.Value?.OpenTypeValue?.EncodingRules,
                Is.EqualTo(AsnEncodingRules.ExtendedXer));
            Assert.That(extendedAgain.Success, Is.True);
            Assert.That(Xml(extendedAgain.Data),
                Is.EqualTo("<Open><Future/></Open>"));
            Assert.That(wrongMode.Success, Is.False);
            Assert.That(wrongMode.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0143"));
        });
    }

    [Test]
    public void RequestRulesRangeAndTrailingContentAreValidated()
    {
        AsnReflectionCatalog catalog = Catalog(StateType());
        byte[] surrounded = System.Text.Encoding.UTF8.GetBytes(
            "x<State><on/></State>y");
        XerSchemaCodec basic = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.BasicXer);
        AsnDecodeResult ranged = basic.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.State",
            Data = surrounded,
            Offset = 1,
            HasCount = true,
            Count = surrounded.Length - 2,
            Options = Options(AsnEncodingRules.BasicXer)
        });
        AsnDecodeResult trailing = Decode(
            catalog,
            AsnEncodingRules.BasicXer,
            System.Text.Encoding.UTF8.GetBytes(
                "<State><on/></State><State><on/></State>"));
        AsnCodecOptions wrongOptions = Options(AsnEncodingRules.ExtendedXer);
        AsnEncodeResult wrongRules = basic.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "State",
            Value = State("on", 7),
            Options = wrongOptions
        });

        Assert.Multiple(() =>
        {
            Assert.That(ranged.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ranged.ConsumedCount, Is.EqualTo(surrounded.Length - 2));
            Assert.That(trailing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(wrongRules.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0401"));
        });
    }

    private static AsnEncodeResult Encode(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules,
        AsnRuntimeValue value)
    {
        XerSchemaCodec codec = new XerSchemaCodec(catalog, rules);
        AsnEncodeResult result = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module." + catalog.Assignments[0].Name,
            Value = value,
            Options = Options(rules)
        });
        Assert.That(result.Success, Is.True,
            result.Diagnostics.Length == 0
                ? "Encoding failed without a diagnostic."
                : result.Diagnostics[0].Message);

        return result;
    }

    private static AsnDecodeResult Decode(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules,
        byte[] data)
    {
        return new XerSchemaCodec(catalog, rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module." + catalog.Assignments[0].Name,
                Data = data,
                Options = Options(rules)
            });
    }

    private static AsnTypeDescriptor StateType()
    {
        AsnTypeDescriptor state = new AsnTypeDescriptor
        {
            StableId = "type.State",
            Name = "State",
            Kind = AsnTypeKind.Enumerated,
            NamedValues =
            [
                Named("off", 0),
                Named("on", 7)
            ]
        };
        state.Encodings =
        [
            new AsnEncodingDescriptor
            {
                EncodingRules = AsnEncodingRules.ExtendedXer,
                EncodingReference = "XER",
                Instructions =
                [
                    Instruction(
                        AsnInstructionKind.Name,
                        ["AS", "LOWERCASED"]),
                    Instruction(AsnInstructionKind.UseNumber, []),
                    Instruction(
                        AsnInstructionKind.PiOrComment,
                        ["AS", "\"<!--extended-->\"", "BEFORE-TAG"])
                ]
            }
        ];
        return state;
    }

    private static AsnEncodingInstruction Instruction(
        AsnInstructionKind kind,
        string[] arguments)
    {
        return new AsnEncodingInstruction
        {
            Kind = kind,
            Name = kind.ToString().ToUpperInvariant(),
            Arguments = arguments
        };
    }

    private static AsnValueDescriptor Named(string name, int number)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Enumerated,
                TextValue = name,
                HasIntegerValue = true,
                IntegerValue = number
            }
        };
    }

    private static AsnRuntimeValue State(string name, int number)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = name,
            HasIntegerValue = true,
            IntegerValue = number
        };
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment." + type.Name,
                    Name = type.Name,
                    QualifiedName = "Module." + type.Name,
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false
        };
    }

    private static string Xml(byte[] data)
    {
        return System.Text.Encoding.UTF8.GetString(data);
    }
}
