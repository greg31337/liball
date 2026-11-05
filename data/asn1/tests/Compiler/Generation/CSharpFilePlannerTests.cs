using asn1;

namespace asn1.Tests;

public class CSharpFilePlannerTests
{
    [Test]
    public void CrossModuleModelsUseMappedQualifiedReferencesAndImports()
    {
        LoweredTypeIr payload = StructuredType("Payload", []);
        LoweredAssignmentIr payloadAssignment = TypeAssignment(
            "Provider.Payload",
            "Payload",
            payload);
        LoweredTypeIr envelope = StructuredType(
            "Envelope",
            [Field("payload", payload)]);
        LoweredAssignmentIr envelopeAssignment = TypeAssignment(
            "Consumer.Envelope",
            "Envelope",
            envelope);
        LoweredModuleIr provider = Module(
            "Provider",
            [payloadAssignment]);
        LoweredModuleIr consumer = Module(
            "Consumer",
            [envelopeAssignment]);
        consumer.Imports =
        [
            new LoweredImportIr
            {
                ModuleName = "Provider",
                Symbols = ["Payload"]
            }
        ];
        LoweredCompilationIr compilation = Compilation(
            [provider, consumer]);

        CSharpGenerationPlan plan = new CSharpFilePlanner()
            .PlanCompilation(compilation, "Generated.Root");
        CSharpModulePlan consumerPlan = plan.FindModulePlan(consumer)
            ?? throw new InvalidOperationException();
        ModelEmitter emitter = new ModelEmitter
        {
            GenerationPlan = plan
        };
        CSharpModelEmission emission = emitter.EmitModel(
            envelopeAssignment,
            consumerPlan.NamespaceName.GeneratedName,
            consumerPlan.TypeScope);

        Assert.Multiple(() =>
        {
            Assert.That(consumerPlan.ImportedNamespaces,
                Is.EqualTo(new string[] { "Generated.Root.Provider" }));
            Assert.That(consumerPlan.Imports[0].Import.Symbols,
                Is.EqualTo(new string[] { "Payload" }));
            Assert.That(emission.Content, Does.Contain(
                "public global::Generated.Root.Provider.Payload Payload = new global::Generated.Root.Provider.Payload();"));
        });
    }

    [Test]
    public void RecursiveModelsUseNullableDeferredInitialization()
    {
        LoweredTypeIr node = StructuredType("Node", []);
        node.Fields = [Field("next", node)];
        node.Fields[0].Optional = true;
        LoweredAssignmentIr assignment = TypeAssignment(
            "Recursive.Node",
            "Node",
            node);
        LoweredModuleIr module = Module("Recursive", [assignment]);
        CSharpGenerationPlan plan = new CSharpFilePlanner()
            .PlanCompilation(Compilation([module]), "Generated");
        CSharpModulePlan modulePlan = plan.Modules[0];
        ModelEmitter emitter = new ModelEmitter
        {
            GenerationPlan = plan
        };

        string content = emitter.EmitModel(
            assignment,
            modulePlan.NamespaceName.GeneratedName,
            modulePlan.TypeScope).Content;

        Assert.That(content, Does.Contain("public Node? Next = null;"));
    }

    [Test]
    public void SharedParameterizedTypeHasOneCanonicalModelPartition()
    {
        LoweredTypeIr instantiated = StructuredType(
            "BoxOfInteger",
            [Field("value", new LoweredTypeIr { Kind = AsnTypeKind.Integer })]);
        instantiated.Derivations =
        [
            new LoweredDerivationIr
            {
                Kind = LoweredDerivationKind.ParameterInstantiation,
                Name = "Box{INTEGER}"
            }
        ];
        LoweredAssignmentIr first = TypeAssignment(
            "Shared.FirstUse",
            "FirstUse",
            instantiated);
        LoweredAssignmentIr second = TypeAssignment(
            "Shared.SecondUse",
            "SecondUse",
            instantiated);
        LoweredModuleIr module = Module("Shared", [first, second]);

        CSharpGenerationPlan plan = new CSharpFilePlanner()
            .PlanCompilation(Compilation([module]), "Generated");

        Assert.Multiple(() =>
        {
            Assert.That(plan.Types, Has.Length.EqualTo(1));
            Assert.That(plan.Types[0].Assignment, Is.SameAs(first));
            Assert.That(plan.Partitions.Count(item =>
                item.Kind == CSharpFilePartitionKind.Model), Is.EqualTo(1));
            Assert.That(plan.FindTypePlan(instantiated),
                Is.SameAs(plan.Types[0]));
        });
    }

    [Test]
    public void ObjectTableDispatchTargetsCanonicalGeneratedTypes()
    {
        LoweredTypeIr payload = StructuredType("Payload", []);
        LoweredObjectTableIr table = new LoweredObjectTableIr
        {
            Kind = AsnConstraintKind.SimpleTable,
            ObjectClassName = "ITEM",
            UniqueFieldName = "id",
            Entries =
            [
                new LoweredObjectTableEntryIr
                {
                    Key = new LoweredValueIr
                    {
                        Kind = AsnValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = 1
                    },
                    Type = payload
                }
            ]
        };
        LoweredTypeIr open = new LoweredTypeIr
        {
            Name = "OpenValue",
            Kind = AsnTypeKind.OpenType,
            ObjectTables = [table]
        };
        LoweredModuleIr module = Module(
            "Dispatch",
            [
                TypeAssignment("Dispatch.Payload", "Payload", payload),
                TypeAssignment("Dispatch.OpenValue", "OpenValue", open)
            ]);

        CSharpGenerationPlan plan = new CSharpFilePlanner()
            .PlanCompilation(Compilation([module]), "Generated");

        Assert.Multiple(() =>
        {
            Assert.That(plan.ObjectTables, Has.Length.EqualTo(1));
            Assert.That(plan.ObjectTables[0].Entries, Has.Length.EqualTo(1));
            Assert.That(plan.ObjectTables[0].Entries[0].Type?.Type,
                Is.SameAs(payload));
            Assert.That(plan.ObjectTables[0].Entries[0].Type?.TypeName.GeneratedName,
                Is.EqualTo("Payload"));
        });
    }

    [Test]
    public void NamespaceCollisionsAndFilePartitionsAreStableAndUnique()
    {
        LoweredModuleIr first = Module(
            "data-model",
            [TypeAssignment(
                "data-model.Item",
                "Item",
                StructuredType("Item", []))]);
        LoweredModuleIr second = Module(
            "data_model",
            [TypeAssignment(
                "data_model.Item",
                "Item",
                StructuredType("Item", []))]);
        LoweredCompilationIr compilation = Compilation([first, second]);
        CSharpFilePlanner planner = new CSharpFilePlanner();
        CSharpGenerationPlan original = planner.PlanCompilation(
            compilation,
            "class.root");
        compilation.Modules = [second, first];
        CSharpGenerationPlan reordered = new CSharpFilePlanner()
            .PlanCompilation(compilation, "class.root");

        Assert.Multiple(() =>
        {
            Assert.That(original.Modules.Select(item =>
                item.NamespaceName.GeneratedName), Is.EqualTo(new string[]
            {
                "ClassValue.Root.DataModel",
                "ClassValue.Root.DataModel_2"
            }));
            Assert.That(original.Partitions.Select(item => item.RelativePath),
                Is.EqualTo(reordered.Partitions.Select(item => item.RelativePath)));
            Assert.That(original.Partitions.Select(item => item.RelativePath)
                .Distinct().Count(), Is.EqualTo(original.Partitions.Length));
            Assert.That(original.Partitions.Select(item => item.Kind),
                Is.EqualTo(new CSharpFilePartitionKind[]
                {
                    CSharpFilePartitionKind.Model,
                    CSharpFilePartitionKind.Descriptors,
                    CSharpFilePartitionKind.Validator,
                    CSharpFilePartitionKind.Codec,
                    CSharpFilePartitionKind.Model,
                    CSharpFilePartitionKind.Descriptors,
                    CSharpFilePartitionKind.Validator,
                    CSharpFilePartitionKind.Codec
                }));
        });
    }

    [Test]
    public void PlannerRejectsUnknownImportedModules()
    {
        LoweredModuleIr module = Module("Consumer", []);
        module.Imports =
        [
            new LoweredImportIr { ModuleName = "Missing" }
        ];

        Assert.That(() => new CSharpFilePlanner().PlanCompilation(
            Compilation([module]),
            "Generated"), Throws.InvalidOperationException);
    }

    private static LoweredCompilationIr Compilation(
        LoweredModuleIr[] modules)
    {
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = modules
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation;
    }

    private static LoweredModuleIr Module(
        string name,
        LoweredAssignmentIr[] assignments)
    {
        return new LoweredModuleIr
        {
            Name = name,
            Assignments = assignments
        };
    }

    private static LoweredAssignmentIr TypeAssignment(
        string qualifiedName,
        string name,
        LoweredTypeIr type)
    {
        return new LoweredAssignmentIr
        {
            QualifiedName = qualifiedName,
            Name = name,
            Kind = AsnAssignmentKind.Type,
            Type = type,
            Exported = true
        };
    }

    private static LoweredTypeIr StructuredType(
        string name,
        LoweredFieldIr[] fields)
    {
        return new LoweredTypeIr
        {
            Name = name,
            Kind = AsnTypeKind.Sequence,
            Fields = fields,
            Extensions = new LoweredExtensionPartitionIr
            {
                RootFields = fields
            }
        };
    }

    private static LoweredFieldIr Field(
        string name,
        LoweredTypeIr type)
    {
        return new LoweredFieldIr
        {
            Name = name,
            Type = type
        };
    }
}
