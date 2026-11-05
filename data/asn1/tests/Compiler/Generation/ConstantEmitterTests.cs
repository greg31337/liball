using asn1;

namespace asn1.Tests;

public class ConstantEmitterTests
{
    [Test]
    public void ModuleValueContainerEmitsExactIntegerBitStringAndOidData()
    {
        System.Numerics.BigInteger huge = System.Numerics.BigInteger.Parse(
            "340282366920938463463374607431768211507");
        LoweredModuleIr module = new LoweredModuleIr
        {
            StableId = "module:telemetry",
            Name = "Telemetry",
            Assignments =
            [
                ValueAssignment(
                    "maximum",
                    new LoweredTypeIr { Kind = AsnTypeKind.Integer },
                    new LoweredValueIr
                    {
                        Kind = AsnValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = huge
                    }),
                ValueAssignment(
                    "flags",
                    new LoweredTypeIr { Kind = AsnTypeKind.BitString },
                    new LoweredValueIr
                    {
                        Kind = AsnValueKind.BitString,
                        BitStringValue = new AsnBitStringValue
                        {
                            Bytes = [0xa0],
                            BitLength = 3
                        }
                    }),
                ValueAssignment(
                    "algorithm",
                    new LoweredTypeIr { Kind = AsnTypeKind.ObjectIdentifier },
                    new LoweredValueIr
                    {
                        Kind = AsnValueKind.ObjectIdentifier,
                        ObjectIdentifierValue = new AsnObjectIdentifierValue
                        {
                            Arcs = [1, 2, 840, huge]
                        }
                    })
            ]
        };

        CSharpConstantEmission emission = new ConstantEmitter().EmitConstants(
            module,
            "Generated.Telemetry",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.ModuleValuesName?.GeneratedName,
                Is.EqualTo("TelemetryValues"));
            Assert.That(emission.Content,
                Does.Contain("public static class TelemetryValues"));
            Assert.That(emission.Content, Does.Contain(
                "public static readonly System.Numerics.BigInteger Maximum = System.Numerics.BigInteger.Parse(\"340282366920938463463374607431768211507\");"));
            Assert.That(emission.Content, Does.Contain(
                "public static readonly AsnBitStringValue Flags = new AsnBitStringValue { Bytes = [160], BitLength = 3 };"));
            Assert.That(emission.Content, Does.Contain(
                "public static readonly AsnObjectIdentifierValue Algorithm = new AsnObjectIdentifierValue { Relative = false, Arcs = [System.Numerics.BigInteger.Parse(\"1\"), System.Numerics.BigInteger.Parse(\"2\"), System.Numerics.BigInteger.Parse(\"840\"), System.Numerics.BigInteger.Parse(\"340282366920938463463374607431768211507\")] };"));
        });
    }

    [Test]
    public void NamedValuesUseDedicatedStaticContainersAndCollisionMappings()
    {
        LoweredTypeIr status = new LoweredTypeIr
        {
            Kind = AsnTypeKind.Integer,
            NamedValues =
            [
                NamedInteger("ready-state", 1),
                NamedInteger("ready_state", 2)
            ]
        };
        LoweredModuleIr module = new LoweredModuleIr
        {
            StableId = "module:protocol",
            Name = "Protocol",
            Assignments =
            [
                new LoweredAssignmentIr
                {
                    StableId = "assignment:status",
                    Kind = AsnAssignmentKind.Type,
                    Name = "status",
                    Type = status
                }
            ]
        };

        CSharpConstantEmission emission = new ConstantEmitter().EmitConstants(
            module,
            "Generated.Protocol",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.ModuleValuesName, Is.Null);
            Assert.That(emission.NamedValueContainerNames
                .Select(item => item.GeneratedName),
                Is.EqualTo(new string[] { "StatusNamedValues" }));
            Assert.That(emission.ValueNames.Select(item => item.GeneratedName),
                Is.EqualTo(new string[] { "ReadyState", "ReadyState_2" }));
            Assert.That(emission.Content,
                Does.Contain("public static class StatusNamedValues"));
            Assert.That(emission.Content, Does.Contain(
                "public static readonly System.Numerics.BigInteger ReadyState = System.Numerics.BigInteger.Parse(\"1\");"));
            Assert.That(emission.Content, Does.Contain(
                "public static readonly System.Numerics.BigInteger ReadyState_2 = System.Numerics.BigInteger.Parse(\"2\");"));
        });
    }

    [Test]
    public void ModuleValuesSupportCollectionsAndStructuredSchemaData()
    {
        LoweredTypeIr integer = new LoweredTypeIr { Kind = AsnTypeKind.Integer };
        LoweredTypeIr collection = new LoweredTypeIr
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = integer
        };
        LoweredTypeIr structure = new LoweredTypeIr
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new LoweredFieldIr { Name = "id", Type = integer }
            ]
        };
        LoweredModuleIr module = new LoweredModuleIr
        {
            StableId = "module:data",
            Name = "Data",
            Assignments =
            [
                ValueAssignment(
                    "numbers",
                    collection,
                    new LoweredValueIr
                    {
                        Kind = AsnValueKind.SequenceOf,
                        Items = [IntegerValue(4), IntegerValue(5)]
                    }),
                ValueAssignment(
                    "record",
                    structure,
                    new LoweredValueIr
                    {
                        Kind = AsnValueKind.Sequence,
                        NamedValues =
                        [
                            new LoweredNamedValueIr
                            {
                                Name = "id",
                                Value = IntegerValue(9)
                            }
                        ]
                    })
            ]
        };

        string content = new ConstantEmitter().EmitConstants(
            module,
            "Generated.Data",
            new CSharpNameScope(false)).Content;

        Assert.Multiple(() =>
        {
            Assert.That(content, Does.Contain(
                "public static readonly System.Numerics.BigInteger[] Numbers = [System.Numerics.BigInteger.Parse(\"4\"), System.Numerics.BigInteger.Parse(\"5\")];"));
            Assert.That(content, Does.Contain(
                "public static readonly AsnValue RecordValue = new AsnValue { Kind = AsnValueKind.Sequence, NamedValues = [new AsnNamedValue { Name = \"id\", Value = new AsnValue { Kind = AsnValueKind.Integer, HasIntegerValue = true, IntegerValue = System.Numerics.BigInteger.Parse(\"9\") } }] };"));
        });
    }

    [Test]
    public void SpecializedValuePayloadsRoundTripThroughBinaryIr()
    {
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules =
            [
                new LoweredModuleIr
                {
                    Name = "Payloads",
                    Assignments =
                    [
                        ValueAssignment(
                            "bits",
                            new LoweredTypeIr { Kind = AsnTypeKind.BitString },
                            new LoweredValueIr
                            {
                                Kind = AsnValueKind.BitString,
                                BitStringValue = new AsnBitStringValue
                                {
                                    Bytes = [0x80],
                                    BitLength = 1
                                },
                                ObjectIdentifierValue = new AsnObjectIdentifierValue
                                {
                                    Arcs = [2, 999]
                                },
                                RealValue = new AsnRealValue
                                {
                                    Kind = AsnRealKind.FiniteBinary,
                                    HasBinaryValue = true,
                                    Mantissa = 3,
                                    Radix = 2,
                                    Exponent = -4
                                },
                                TimeValue = new AsnTimeValue
                                {
                                    Kind = AsnTimeKind.Date,
                                    Text = "2026-08-28"
                                }
                            })
                    ]
                }
            ]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        IrBinarySerializer serializer = new IrBinarySerializer();

        LoweredCompilationIr restored = serializer.Deserialize(
            serializer.Serialize(compilation, new AsnResourceLimits()),
            new AsnResourceLimits());
        LoweredValueIr restoredValue = restored.Modules[0].Assignments[0].Value
            ?? new LoweredValueIr();
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(restored);
        AsnValue reflectedValue = catalog.Modules[0].Values[0].Value
            ?? new AsnValue();

        Assert.Multiple(() =>
        {
            Assert.That(restoredValue.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(restoredValue.BitStringValue?.BitLength, Is.EqualTo(1));
            Assert.That(restoredValue.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 2, 999 }));
            Assert.That(restoredValue.RealValue?.Mantissa,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(restoredValue.RealValue?.Exponent,
                Is.EqualTo(new System.Numerics.BigInteger(-4)));
            Assert.That(restoredValue.TimeValue?.Text,
                Is.EqualTo("2026-08-28"));
            Assert.That(reflectedValue.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(reflectedValue.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 2, 999 }));
            Assert.That(reflectedValue.RealValue?.Mantissa,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(reflectedValue.TimeValue?.Text,
                Is.EqualTo("2026-08-28"));
        });
    }

    [Test]
    public void ConstantEmitterRejectsMissingSpecializedPayloads()
    {
        LoweredModuleIr module = new LoweredModuleIr
        {
            Name = "Invalid",
            Assignments =
            [
                ValueAssignment(
                    "bits",
                    new LoweredTypeIr { Kind = AsnTypeKind.BitString },
                    new LoweredValueIr { Kind = AsnValueKind.BitString })
            ]
        };

        Assert.That(() => new ConstantEmitter().EmitConstants(
            module,
            "Generated.Invalid",
            new CSharpNameScope(false)), Throws.InvalidOperationException);
    }

    private static LoweredAssignmentIr ValueAssignment(
        string name,
        LoweredTypeIr type,
        LoweredValueIr value)
    {
        return new LoweredAssignmentIr
        {
            StableId = "assignment:" + name,
            Kind = AsnAssignmentKind.Value,
            Name = name,
            Type = type,
            Value = value
        };
    }

    private static LoweredNamedValueIr NamedInteger(string name, int value)
    {
        return new LoweredNamedValueIr
        {
            StableId = "named:" + name,
            Name = name,
            Value = IntegerValue(value)
        };
    }

    private static LoweredValueIr IntegerValue(int value)
    {
        return new LoweredValueIr
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }
}
