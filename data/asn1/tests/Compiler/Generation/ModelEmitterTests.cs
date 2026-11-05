using asn1;

namespace asn1.Tests;

public class ModelEmitterTests
{
    [Test]
    public void SequenceModelEmitsPresenceDefaultsAndUnknownContainers()
    {
        LoweredAssignmentIr assignment = TypeAssignment(
            "person-record",
            new LoweredTypeIr
            {
                Kind = AsnTypeKind.Sequence,
                Extensible = true,
                Fields =
                [
                    Field("count", AsnTypeKind.Integer, false, false, false),
                    Field("display-name", AsnTypeKind.Utf8String, true, false, false),
                    new LoweredFieldIr
                    {
                        StableId = "field:enabled",
                        Name = "enabled",
                        Type = new LoweredTypeIr { Kind = AsnTypeKind.Boolean },
                        HasDefaultValue = true,
                        DefaultValue = new LoweredValueIr
                        {
                            Kind = AsnValueKind.Boolean,
                            TextValue = "TRUE"
                        }
                    },
                    Field("future", AsnTypeKind.Integer, false, false, true)
                ]
            });

        CSharpModelEmission emission = new ModelEmitter().EmitModel(
            assignment,
            "Company.Models",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.TypeName.GeneratedName,
                Is.EqualTo("PersonRecord"));
            Assert.That(emission.Content,
                Does.Contain("namespace Company.Models;"));
            Assert.That(emission.Content,
                Does.Contain("public class PersonRecord"));
            Assert.That(emission.Content, Does.Contain(
                "public System.Numerics.BigInteger Count = new System.Numerics.BigInteger(0);"));
            Assert.That(emission.Content,
                Does.Contain("public bool HasDisplayName;"));
            Assert.That(emission.Content,
                Does.Contain("public string DisplayName = \"\";"));
            Assert.That(emission.Content,
                Does.Contain("public bool HasEnabled;"));
            Assert.That(emission.Content,
                Does.Contain("public bool Enabled = true;"));
            Assert.That(emission.Content,
                Does.Contain("public bool HasFuture;"));
            Assert.That(emission.Content, Does.Contain(
                "public AsnUnknownExtension[] UnknownExtensions = [];"));
            Assert.That(emission.Content, Does.Contain(
                "public AsnUnknownField[] UnknownFields = [];"));
            Assert.That(emission.PresenceNames.Select(item => item.GeneratedName),
                Is.EqualTo(new string[]
                {
                    "HasDisplayName",
                    "HasEnabled",
                    "HasFuture"
                }));
        });
    }

    [Test]
    public void ChoiceModelEmitsTopLevelDiscriminantAndAlternativeFields()
    {
        LoweredAssignmentIr assignment = TypeAssignment(
            "operation-result",
            new LoweredTypeIr
            {
                Kind = AsnTypeKind.Choice,
                Extensible = true,
                Fields =
                [
                    Field("number", AsnTypeKind.Integer, false, false, false),
                    Field("message", AsnTypeKind.Utf8String, false, false, false),
                    Field("none", AsnTypeKind.Boolean, false, false, false)
                ]
            });

        CSharpModelEmission emission = new ModelEmitter().EmitModel(
            assignment,
            "Generated.Choices",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.ChoiceName?.GeneratedName,
                Is.EqualTo("OperationResultSelection"));
            Assert.That(emission.Content,
                Does.Contain("public enum OperationResultSelection"));
            Assert.That(emission.Content, Does.Contain("None = 0,"));
            Assert.That(emission.Content, Does.Contain("Number = 1,"));
            Assert.That(emission.Content, Does.Contain("Message = 2,"));
            Assert.That(emission.Content, Does.Contain("None_2 = 3"));
            Assert.That(emission.Content, Does.Contain(
                "public OperationResultSelection Selection = OperationResultSelection.None;"));
            Assert.That(emission.Content, Does.Contain(
                "public System.Numerics.BigInteger Number = new System.Numerics.BigInteger(0);"));
            Assert.That(emission.Content,
                Does.Contain("public string Message = \"\";"));
            Assert.That(emission.Content,
                Does.Contain("public bool None_2 = false;"));
            Assert.That(emission.PresenceNames, Is.Empty);
        });
    }

    [Test]
    public void CollectionAndScalarModelsUseInitializedPortableRuntimeTypes()
    {
        ModelEmitter emitter = new ModelEmitter();
        CSharpNameScope typeScope = new CSharpNameScope(false);
        LoweredAssignmentIr collection = TypeAssignment(
            "names",
            new LoweredTypeIr
            {
                Kind = AsnTypeKind.SequenceOf,
                ElementType = new LoweredTypeIr { Kind = AsnTypeKind.Utf8String }
            });
        LoweredAssignmentIr bits = TypeAssignment(
            "flags",
            new LoweredTypeIr { Kind = AsnTypeKind.BitString });

        CSharpModelEmission collectionEmission = emitter.EmitModel(
            collection,
            "Generated.Data",
            typeScope);
        CSharpModelEmission bitEmission = emitter.EmitModel(
            bits,
            "Generated.Data",
            typeScope);

        Assert.Multiple(() =>
        {
            Assert.That(collectionEmission.Content,
                Does.Contain("public string[] Items = [];"));
            Assert.That(bitEmission.Content, Does.Contain(
                "public AsnBitStringValue Value = new AsnBitStringValue();"));
            Assert.That(collectionEmission.Content,
                Does.Not.Contain("System.Nullable"));
            Assert.That(bitEmission.Content, Does.Not.Contain("dynamic"));
        });
    }

    [Test]
    public void MemberAndPresenceNamesShareOneCollisionScope()
    {
        LoweredAssignmentIr assignment = TypeAssignment(
            "collisions",
            new LoweredTypeIr
            {
                Kind = AsnTypeKind.Sequence,
                Fields =
                [
                    Field("foo-bar", AsnTypeKind.Integer, true, false, false),
                    Field("foo_bar", AsnTypeKind.Integer, false, false, false),
                    Field("has-foo-bar", AsnTypeKind.Integer, false, false, false)
                ]
            });

        CSharpModelEmission emission = new ModelEmitter().EmitModel(
            assignment,
            "Generated.Collisions",
            new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.FieldNames.Select(item => item.GeneratedName),
                Is.EqualTo(new string[]
                {
                    "FooBar",
                    "FooBar_2",
                    "HasFooBar_2"
                }));
            Assert.That(emission.PresenceNames.Select(item => item.GeneratedName),
                Is.EqualTo(new string[] { "HasFooBar" }));
            Assert.That(emission.Content,
                Does.Contain("public bool HasFooBar;"));
            Assert.That(emission.Content,
                Does.Contain(" HasFooBar_2 = "));
        });
    }

    [Test]
    public void DefaultInitializersPreserveTextOctetsAndLargeIntegers()
    {
        LoweredFieldIr title = Field(
            "title",
            AsnTypeKind.Utf8String,
            false,
            true,
            false);
        title.DefaultValue = new LoweredValueIr
        {
            Kind = AsnValueKind.CharacterString,
            TextValue = "line \"one\"\nnext"
        };
        LoweredFieldIr data = Field(
            "data",
            AsnTypeKind.OctetString,
            false,
            true,
            false);
        data.DefaultValue = new LoweredValueIr
        {
            Kind = AsnValueKind.OctetString,
            Octets = [0, 255]
        };
        LoweredFieldIr number = Field(
            "number",
            AsnTypeKind.Integer,
            false,
            true,
            false);
        number.DefaultValue = new LoweredValueIr
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = System.Numerics.BigInteger.Parse(
                "340282366920938463463374607431768211507")
        };
        LoweredAssignmentIr assignment = TypeAssignment(
            "defaults",
            new LoweredTypeIr
            {
                Kind = AsnTypeKind.Sequence,
                Fields = [title, data, number]
            });

        string content = new ModelEmitter().EmitModel(
            assignment,
            "Generated.Defaults",
            new CSharpNameScope(false)).Content;

        Assert.Multiple(() =>
        {
            Assert.That(content,
                Does.Contain("Title = \"line \\\"one\\\"\\nnext\";"));
            Assert.That(content, Does.Contain("Data = [0, 255];"));
            Assert.That(content, Does.Contain(
                "Number = System.Numerics.BigInteger.Parse(\"340282366920938463463374607431768211507\");"));
        });
    }

    [Test]
    public void ModelEmitterRejectsAssignmentsWithoutResolvedTypeModels()
    {
        ModelEmitter emitter = new ModelEmitter();

        Assert.Multiple(() =>
        {
            Assert.That(() => emitter.EmitModel(
                new LoweredAssignmentIr
                {
                    Kind = AsnAssignmentKind.Value,
                    Name = "answer"
                },
                "Generated.Invalid",
                new CSharpNameScope(false)), Throws.InvalidOperationException);
            Assert.That(() => emitter.EmitModel(
                new LoweredAssignmentIr
                {
                    Kind = AsnAssignmentKind.Type,
                    Name = "Missing"
                },
                "Generated.Invalid",
                new CSharpNameScope(false)), Throws.InvalidOperationException);
        });
    }

    private static LoweredAssignmentIr TypeAssignment(
        string name,
        LoweredTypeIr type)
    {
        return new LoweredAssignmentIr
        {
            StableId = "assignment:" + name,
            Kind = AsnAssignmentKind.Type,
            Name = name,
            Type = type
        };
    }

    private static LoweredFieldIr Field(
        string name,
        AsnTypeKind kind,
        bool optional,
        bool hasDefault,
        bool extension)
    {
        return new LoweredFieldIr
        {
            StableId = "field:" + name,
            Name = name,
            Type = new LoweredTypeIr { Kind = kind },
            Optional = optional,
            HasDefaultValue = hasDefault,
            Extension = extension
        };
    }
}
