using asn1;

namespace asn1.Tests;

public class DescriptorEmitterTests
{
    [Test]
    public void RecursiveTypesAreAllocatedBeforeDescriptorReferencesAreWired()
    {
        LoweredTypeIr node = new LoweredTypeIr
        {
            Name = "Node",
            Kind = AsnTypeKind.Sequence
        };
        LoweredFieldIr next = new LoweredFieldIr
        {
            Name = "next",
            Type = node,
            Optional = true
        };
        node.Fields = [next];
        node.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = [next]
        };
        LoweredModuleIr module = ModuleWithAssignments(
            "Recursive",
            [TypeAssignment("Node", node)]);

        CSharpDescriptorEmission emission = new DescriptorEmitter()
            .EmitDescriptors(
                module,
                "Generated.Recursive",
                new CSharpNameScope(false));
        int allocationIndex = emission.Content.IndexOf(
            "AsnTypeDescriptor type0 = new AsnTypeDescriptor();",
            StringComparison.Ordinal);
        int referenceIndex = emission.Content.IndexOf(
            "field0.Type = type0;",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(emission.SchemaName.GeneratedName,
                Is.EqualTo("RecursiveSchema"));
            Assert.That(allocationIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(referenceIndex, Is.GreaterThan(allocationIndex));
            Assert.That(emission.Content,
                Does.Contain("type0.Fields = [field0];"));
            Assert.That(emission.Content,
                Does.Contain("type0.RootFields = [field0];"));
            Assert.That(emission.Content,
                Does.Contain("type0.RootNamedValues = [];"));
            Assert.That(emission.Content,
                Does.Contain("field0.ExtensionGroupIndex = -1;"));
            Assert.That(emission.Content,
                Does.Contain("this.Catalog.Modules = [this.Module];"));
            Assert.That(emission.Content,
                Does.Contain("this.Catalog.Assignments = [assignment0];"));
        });
    }

    [Test]
    public void DescriptorGraphIncludesDefaultsConstraintsTablesAndPlans()
    {
        LoweredTypeIr integer = new LoweredTypeIr
        {
            Name = "Identifier",
            Kind = AsnTypeKind.Integer,
            Constraints =
            [
                new LoweredConstraintIr
                {
                    DomainKind = AsnTypeKind.Integer,
                    Root = new LoweredConstraintNodeIr
                    {
                        Kind = AsnConstraintKind.ValueRange,
                        HasLowerBound = true,
                        LowerBound = 1,
                        HasUpperBound = true,
                        UpperBound = 9
                    },
                    RootIntervals =
                    [
                        new LoweredIntervalIr
                        {
                            HasLowerBound = true,
                            LowerBound = 1,
                            HasUpperBound = true,
                            UpperBound = 9
                        }
                    ]
                }
            ]
        };
        LoweredFieldIr id = new LoweredFieldIr
        {
            Name = "id",
            Type = integer,
            HasDefaultValue = true,
            DefaultValue = IntegerValue(3),
            Tags =
            [
                new LoweredTagIr
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = 0
                }
            ]
        };
        LoweredObjectTableIr table = new LoweredObjectTableIr
        {
            Kind = AsnConstraintKind.SimpleTable,
            ObjectClassName = "ITEM",
            UniqueFieldName = "id",
            Entries =
            [
                new LoweredObjectTableEntryIr
                {
                    Key = IntegerValue(7),
                    Type = integer
                }
            ]
        };
        LoweredTypeIr record = new LoweredTypeIr
        {
            Name = "Record",
            Kind = AsnTypeKind.Sequence,
            Fields = [id],
            Extensions = new LoweredExtensionPartitionIr
            {
                RootFields = [id]
            },
            ObjectTables = [table],
            Instructions =
            [
                new LoweredInstructionIr
                {
                    EncodingReference = "XER",
                    Name = "ATTRIBUTE",
                    Kind = AsnInstructionKind.Attribute,
                    Targets = ["id"]
                }
            ],
            SourceSpan = Span("schema.asn", 12, 2, 3)
        };
        LoweredModuleIr module = ModuleWithAssignments(
            "Metadata",
            [TypeAssignment("Record", record)]);

        string content = new DescriptorEmitter().EmitDescriptors(
            module,
            "Generated.Metadata",
            new CSharpNameScope(false)).Content;

        Assert.Multiple(() =>
        {
            Assert.That(content,
                Does.Contain("field0.DefaultValue = value0;"));
            Assert.That(content,
                Does.Contain("new AsnTagDescriptor"));
            Assert.That(content,
                Does.Contain("constraint0.RootIntervals = [new AsnIntervalDescriptor"));
            Assert.That(content,
                Does.Contain("table0.Entries = [new AsnObjectTableEntryDescriptor"));
            Assert.That(content,
                Does.Contain("Type = type1"));
            Assert.That(content,
                Does.Contain("new AsnEncodingDescriptor"));
            Assert.That(content,
                Does.Contain("Plan = new AsnEncodingPlan"));
            Assert.That(content, Does.Contain(
                "new AsnSourcePosition { SourceName = \"schema.asn\", Offset = 12, Line = 2, Column = 3 }"));
        });
    }

    [Test]
    public void GeneratedDescriptorNamesAreDeterministicAndCollisionSafe()
    {
        LoweredTypeIr type = new LoweredTypeIr
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new LoweredFieldIr
                {
                    Name = "foo-bar",
                    Type = new LoweredTypeIr { Kind = AsnTypeKind.Integer }
                },
                new LoweredFieldIr
                {
                    Name = "foo_bar",
                    Type = new LoweredTypeIr { Kind = AsnTypeKind.Boolean }
                }
            ]
        };
        LoweredModuleIr module = ModuleWithAssignments(
            "hostile-module",
            [TypeAssignment("class", type)]);

        CSharpDescriptorEmission first = new DescriptorEmitter()
            .EmitDescriptors(
                module,
                "Generated.Names",
                new CSharpNameScope(false));
        CSharpDescriptorEmission second = new DescriptorEmitter()
            .EmitDescriptors(
                module,
                "Generated.Names",
                new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(first.Content, Is.EqualTo(second.Content));
            Assert.That(first.FieldNames.Select(item => item.GeneratedName),
                Is.EqualTo(new string[] { "FooBar", "FooBar_2" }));
            Assert.That(first.AssignmentNames[0].GeneratedName,
                Is.EqualTo("ClassValue"));
            Assert.That(first.Content,
                Does.Contain("field0.GeneratedName = \"FooBar\";"));
            Assert.That(first.Content,
                Does.Contain("field1.GeneratedName = \"FooBar_2\";"));
        });
    }

    [Test]
    public void DescriptorEmitterRejectsEmptyNamespaces()
    {
        LoweredModuleIr module = ModuleWithAssignments("Empty", []);

        Assert.That(() => new DescriptorEmitter().EmitDescriptors(
            module,
            "",
            new CSharpNameScope(false)), Throws.ArgumentException);
    }

    private static LoweredModuleIr ModuleWithAssignments(
        string name,
        LoweredAssignmentIr[] assignments)
    {
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules =
            [
                new LoweredModuleIr
                {
                    Name = name,
                    Assignments = assignments
                }
            ]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation.Modules[0];
    }

    private static LoweredAssignmentIr TypeAssignment(
        string name,
        LoweredTypeIr type)
    {
        return new LoweredAssignmentIr
        {
            Name = name,
            QualifiedName = "Module." + name,
            Kind = AsnAssignmentKind.Type,
            Exported = true,
            Type = type
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

    private static AsnSourceSpan Span(
        string sourceName,
        int offset,
        int line,
        int column)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = offset,
                Line = line,
                Column = column
            },
            End = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = offset + 1,
                Line = line,
                Column = column + 1
            }
        };
    }
}
