using asn1;

namespace asn1.Tests;

public class ReflectionServiceImplementationTests
{
    [Test]
    public void ServiceResolvesQualifiedNamesAndTraversesTypeGraphsOnce()
    {
        AsnTypeDescriptor leaf = new AsnTypeDescriptor
        {
            StableId = "type:Alpha.Leaf",
            Name = "Leaf",
            GeneratedName = "LeafModel",
            Kind = AsnTypeKind.Integer
        };
        AsnTypeDescriptor collection = new AsnTypeDescriptor
        {
            StableId = "type:Alpha.Items",
            Name = "Items",
            Kind = AsnTypeKind.SequenceOf,
            ElementType = leaf
        };
        AsnTypeDescriptor root = new AsnTypeDescriptor
        {
            StableId = "type:Alpha.Root",
            Name = "Root",
            GeneratedName = "RootModel",
            Kind = AsnTypeKind.Sequence
        };
        AsnTypeDescriptor dispatched = new AsnTypeDescriptor
        {
            StableId = "type:Alpha.Dispatched",
            Name = "Dispatched",
            Kind = AsnTypeKind.Utf8String
        };
        root.Fields =
        [
            new AsnFieldDescriptor { Name = "items", Type = collection },
            new AsnFieldDescriptor { Name = "again", Type = root }
        ];
        root.ObjectTables = [new AsnObjectTableDescriptor
        {
            Entries = [new AsnObjectTableEntryDescriptor { Type = dispatched }]
        }];
        AsnValueDescriptor answer = new AsnValueDescriptor
        {
            StableId = "value:Alpha.answer",
            Name = "answer",
            GeneratedName = "AnswerValue",
            Kind = AsnValueKind.Integer
        };
        AsnModuleDescriptor module = new AsnModuleDescriptor
        {
            StableId = "module:alpha:v1",
            Name = "Alpha",
            GeneratedName = "AlphaModel",
            ObjectIdentifier = "1.2.3",
            Types = [root],
            Values = [answer],
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment:root",
                    Name = "Root",
                    QualifiedName = "Alpha.Root",
                    GeneratedName = "RootModel",
                    Kind = AsnAssignmentKind.Type,
                    Type = root,
                    ObjectTables = root.ObjectTables
                },
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment:answer",
                    Name = "answer",
                    QualifiedName = "Alpha.answer",
                    GeneratedName = "AnswerValue",
                    Kind = AsnAssignmentKind.Value,
                    Value = answer
                }
            ]
        };
        AsnReflectionCatalog catalog = new AsnReflectionCatalog
        {
            Modules = [module]
        };
        IAsnReflectionService service = new AsnReflectionService(
            new ReflectionServiceImplementation());

        Assert.That(service.ResolveModule(catalog, "Alpha"), Is.SameAs(module));
        Assert.That(service.ResolveModule(catalog, "AlphaModel"), Is.SameAs(module));
        Assert.That(service.ResolveModule(catalog, "module:alpha:v1"),
            Is.SameAs(module));
        Assert.That(service.ResolveModule(catalog, "Alpha{1.2.3}"),
            Is.SameAs(module));
        Assert.That(service.ResolveType(catalog, "Alpha", "Root"),
            Is.SameAs(root));
        Assert.That(service.ResolveType(catalog, "Alpha", "RootModel"),
            Is.SameAs(root));
        Assert.That(service.ResolveType(catalog, "", "Alpha.Root"),
            Is.SameAs(root));
        Assert.That(service.ResolveType(catalog, "Alpha", "Beta.Root"), Is.Null);
        Assert.That(service.ResolveValue(catalog, "Alpha", "answer"),
            Is.SameAs(answer));
        Assert.That(service.ResolveValue(catalog, "", "Alpha.AnswerValue"),
            Is.SameAs(answer));
        Assert.That(service.GetTypes(catalog),
            Is.EqualTo(new[] { root, collection, leaf, dispatched }));
    }

    [Test]
    public void ServiceBuildsQueryableCatalogsFromValidationResults()
    {
        AsnType integer = new AsnType
        {
            Name = "Counter",
            Kind = AsnTypeKind.Integer,
            Constraints = [new AsnConstraint
            {
                Kind = AsnConstraintKind.ValueRange,
                Elements = [new AsnConstraintElement
                {
                    Kind = AsnConstraintKind.ValueRange,
                    HasLowerBound = true,
                    LowerBound = 1,
                    HasUpperBound = true,
                    UpperBound = 9
                }]
            }]
        };
        AsnValue defaultValue = new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = 3
        };
        AsnType message = new AsnType
        {
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Components = [new AsnComponent
            {
                Name = "count",
                Type = integer,
                HasDefaultValue = true,
                DefaultValue = defaultValue
            }]
        };
        AsnValue answer = new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = 5
        };
        AsnModule module = new AsnModule
        {
            Name = "Validated",
            Imports = [new AsnImport
            {
                ModuleName = "Library",
                Symbols = ["External"]
            }],
            Assignments =
            [
                new AsnAssignment
                {
                    Name = "Message",
                    Kind = AsnAssignmentKind.Type,
                    Type = message
                },
                new AsnAssignment
                {
                    Name = "answer",
                    Kind = AsnAssignmentKind.Value,
                    Type = integer,
                    Value = answer
                }
            ],
            EncodingInstructions = [new AsnEncodingInstruction
            {
                Name = "ATTRIBUTE",
                Kind = AsnInstructionKind.Attribute,
                Targets = ["Message", "count"]
            }]
        };
        IAsnReflectionService service = new ReflectionServiceImplementation();

        AsnReflectionCatalog catalog = service.BuildCatalog(
            new AsnValidationResult
            {
                Success = true,
                Modules = [module],
                Assignments = module.Assignments
            });
        AsnTypeDescriptor reflectedMessage = service.ResolveType(
            catalog,
            "Validated",
            "Validated.Message") ?? throw new InvalidOperationException();
        AsnValueDescriptor reflectedAnswer = service.ResolveValue(
            catalog,
            "Validated",
            "answer") ?? throw new InvalidOperationException();

        Assert.That(catalog.Modules.Single().Imports.Single().ModuleName,
            Is.EqualTo("Library"));
        Assert.That(reflectedMessage.Fields.Single().Name, Is.EqualTo("count"));
        Assert.That(reflectedMessage.Fields.Single().DefaultValue?.Value
            ?.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(3)));
        Assert.That(reflectedMessage.Fields.Single().Type?.Constraints.Single()
            .RootIntervals.Single().LowerBound,
            Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(reflectedAnswer.Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(5)));
        Assert.That(catalog.Modules.Single().Encodings.Single().EncodingRules,
            Is.EqualTo(AsnEncodingRules.ExtendedXer));
        Assert.That(catalog.Modules.Single().Encodings.Single().Plan?.Steps.Single()
            .Path, Is.EqualTo("Message.count"));
        Assert.That(service.GetTypes(catalog).Select(item => item.Name),
            Is.EqualTo(new[] { "Message", "Counter" }));
    }

    [Test]
    public void ServiceReturnsNullForAmbiguousAndMissingNames()
    {
        AsnReflectionCatalog catalog = new AsnReflectionCatalog
        {
            Modules =
            [
                new AsnModuleDescriptor
                {
                    StableId = "module:first",
                    Name = "Duplicate"
                },
                new AsnModuleDescriptor
                {
                    StableId = "module:second",
                    Name = "Duplicate"
                }
            ]
        };
        IAsnReflectionService service = new ReflectionServiceImplementation();

        Assert.That(service.ResolveModule(catalog, "Duplicate"), Is.Null);
        Assert.That(service.ResolveModule(catalog, "module:first"),
            Is.SameAs(catalog.Modules[0]));
        Assert.That(service.ResolveModule(catalog, "Missing"), Is.Null);
        Assert.That(service.ResolveType(catalog, "Missing", "Type"), Is.Null);
        Assert.That(service.ResolveValue(catalog, "Missing", "value"), Is.Null);
    }
}
