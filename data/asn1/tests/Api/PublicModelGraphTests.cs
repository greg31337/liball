using System.Numerics;
using asn1;

namespace asn1.Tests;

public class PublicModelGraphTests
{
    [Test]
    public void SchemaInformationPlanAndReflectionGraphsRetainRelationships()
    {
        BigInteger upperBound = BigInteger.Parse("184467440737095516160");
        AsnValue defaultValue = new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = 7
        };
        AsnConstraintElement range = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.ValueRange,
            HasLowerBound = true,
            LowerBound = 0,
            HasUpperBound = true,
            UpperBound = upperBound
        };
        AsnConstraint constraint = new AsnConstraint
        {
            Kind = AsnConstraintKind.ValueRange,
            Elements = [range],
            Extensible = true
        };
        AsnType integerType = new AsnType
        {
            Name = "Counter",
            Kind = AsnTypeKind.Integer,
            Constraints = [constraint],
            Tag = new AsnTag { Class = AsnTagClass.Application, Number = 5000000000 },
            NamedValues = [new AsnNamedValue { Name = "default-count", Value = defaultValue }]
        };
        AsnComponent component = new AsnComponent
        {
            Name = "count",
            Type = integerType,
            HasDefaultValue = true,
            DefaultValue = defaultValue
        };
        AsnType sequenceType = new AsnType
        {
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Components = [component],
            Extensible = true
        };
        AsnObjectClassField classField = new AsnObjectClassField
        {
            Name = "&Type",
            Kind = AsnObjectFieldKind.Type,
            Type = sequenceType,
            Unique = true
        };
        AsnObjectClass objectClass = new AsnObjectClass
        {
            Name = "MESSAGE-CLASS",
            Fields = [classField],
            SyntaxTokens = ["&Type"]
        };
        AsnObjectFieldSetting setting = new AsnObjectFieldSetting { FieldName = "&Type", Type = sequenceType };
        AsnInformationObject informationObject = new AsnInformationObject
        {
            Name = "message-object",
            ObjectClass = objectClass,
            FieldSettings = [setting]
        };
        AsnOpenTypeDispatchEntry dispatch = new AsnOpenTypeDispatchEntry
        {
            Key = defaultValue,
            Type = sequenceType,
            StableId = "dispatch:message:7"
        };
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            Name = "MessageSet",
            ObjectClass = objectClass,
            Objects = [informationObject],
            Extensible = true,
            DispatchEntries = [dispatch]
        };
        AsnParameter parameter = new AsnParameter
        {
            Name = "T",
            Kind = AsnParameterKind.Type,
            GovernorType = sequenceType,
            GovernorObjectClass = objectClass
        };
        AsnActualParameter actual = new AsnActualParameter
        {
            Kind = AsnParameterKind.Type,
            Type = sequenceType,
            Object = informationObject,
            ObjectSet = objectSet
        };
        AsnAssignment assignment = new AsnAssignment
        {
            Name = "Message",
            Kind = AsnAssignmentKind.Type,
            Type = sequenceType,
            Parameters = [parameter],
            ActualParameters = [actual],
            ObjectClass = objectClass,
            Object = informationObject,
            ObjectSet = objectSet
        };
        AsnImport import = new AsnImport
        {
            ModuleName = "BaseModule",
            Symbols = ["BaseType"],
            ModuleIdentifier = new AsnObjectIdentifierValue { Arcs = [1, 3, 6, 1] }
        };
        AsnEncodingInstruction instruction = new AsnEncodingInstruction
        {
            Name = "USE-ORDER",
            Kind = AsnInstructionKind.UseOrder,
            Targets = ["Message.count"]
        };
        AsnEncodingPlanStep childStep = new AsnEncodingPlanStep
        {
            Kind = AsnPlanStepKind.Primitive,
            Path = "Message.count",
            Instruction = instruction
        };
        AsnEncodingPlanStep rootStep = new AsnEncodingPlanStep
        {
            Kind = AsnPlanStepKind.Sequence,
            Path = "Message",
            Children = [childStep]
        };
        AsnEcnClass ecnClass = new AsnEcnClass { Name = "MessageEncoding", Category = "concatenation", Steps = [rootStep] };
        AsnEcnObject ecnObject = new AsnEcnObject
        {
            Name = "message-encoding",
            ClassName = "MessageEncoding",
            Instructions = [instruction]
        };
        AsnPerInstructionRegistration registration = new AsnPerInstructionRegistration
        {
            Keyword = "APPLICATION-RULE",
            HandlerId = "example.handler",
            SupportsUnaligned = true
        };
        AsnEncodingPlan plan = new AsnEncodingPlan
        {
            StableId = "plan:message:uper",
            EncodingRules = AsnEncodingRules.Uper,
            Steps = [rootStep],
            Instructions = [instruction],
            EcnClasses = [ecnClass],
            EcnObjects = [ecnObject],
            PerInstructionRegistrations = [registration]
        };
        AsnModule module = new AsnModule
        {
            Name = "ExampleModule",
            Imports = [import],
            Assignments = [assignment],
            EncodingInstructions = [instruction]
        };

        AsnValueDescriptor valueDescriptor = new AsnValueDescriptor
        {
            StableId = "value:default-count",
            Name = "default-count",
            Kind = AsnValueKind.Integer,
            Value = defaultValue
        };
        AsnConstraintDescriptor constraintDescriptor = new AsnConstraintDescriptor
        {
            StableId = "constraint:counter",
            Kind = AsnConstraintKind.ValueRange,
            Constraint = constraint
        };
        AsnEncodingDescriptor encodingDescriptor = new AsnEncodingDescriptor
        {
            StableId = "encoding:message:uper",
            EncodingRules = AsnEncodingRules.Uper,
            Plan = plan
        };
        AsnTypeDescriptor typeDescriptor = new AsnTypeDescriptor
        {
            StableId = "type:message",
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Constraints = [constraintDescriptor],
            Encodings = [encodingDescriptor],
            SourceType = sequenceType
        };
        AsnFieldDescriptor fieldDescriptor = new AsnFieldDescriptor
        {
            StableId = "field:message:count",
            Name = "count",
            Index = 0,
            Type = typeDescriptor,
            HasDefaultValue = true,
            DefaultValue = valueDescriptor
        };
        typeDescriptor.Fields = [fieldDescriptor];
        AsnModuleDescriptor moduleDescriptor = new AsnModuleDescriptor
        {
            StableId = "module:example",
            Name = "ExampleModule",
            Types = [typeDescriptor],
            Values = [valueDescriptor],
            Encodings = [encodingDescriptor],
            SourceModule = module
        };
        AsnReflectionCatalog catalog = new AsnReflectionCatalog
        {
            StableId = "catalog:example",
            Modules = [moduleDescriptor]
        };
        AsnCompilationResult compilation = new AsnCompilationResult
        {
            Success = true,
            Modules = [module],
            Assignments = [assignment],
            EncodingPlans = [plan],
            ReflectionCatalog = catalog
        };

        Assert.That(compilation.Modules[0].Imports[0].ModuleIdentifier?.IsValid(), Is.True);
        Assert.That(compilation.Assignments[0].Type?.Components[0].DefaultValue, Is.SameAs(defaultValue));
        Assert.That(compilation.Assignments[0].ObjectSet?.DispatchEntries[0].Type, Is.SameAs(sequenceType));
        Assert.That(compilation.EncodingPlans[0].Steps[0].Children[0].Instruction, Is.SameAs(instruction));
        Assert.That(compilation.EncodingPlans[0].PerInstructionRegistrations[0].HandlerId, Is.EqualTo("example.handler"));
        Assert.That(compilation.ReflectionCatalog?.Modules[0].Types[0].Fields[0].DefaultValue, Is.SameAs(valueDescriptor));
        Assert.That(range.UpperBound, Is.EqualTo(upperBound));
    }

    [Test]
    public void RuntimeGraphRetainsSpecializedOpenAndUnknownValues()
    {
        AsnBitStringValue bits = new AsnBitStringValue { Bytes = [0xa0], BitLength = 3 };
        AsnObjectIdentifierValue oid = new AsnObjectIdentifierValue { Arcs = [2, 999] };
        AsnRealValue real = new AsnRealValue { Kind = AsnRealKind.MinusZero };
        AsnTimeValue time = new AsnTimeValue { Kind = AsnTimeKind.Date, Text = "2026-08-28" };
        AsnUnknownExtension extension = new AsnUnknownExtension
        {
            Index = 0,
            EncodingRules = AsnEncodingRules.Ber,
            HasEncodingRules = true,
            HasBinaryData = true,
            BinaryData = [0x05, 0x00]
        };
        AsnUnknownField unknownField = new AsnUnknownField
        {
            Name = "future-field",
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "<future-field />"
        };
        AsnRuntimeValue decodedOpenValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = 42
        };
        AsnOpenTypeValue openType = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "type:example:open-value",
            TypeName = "Example.OpenValue",
            Value = decodedOpenValue,
            DispatchKey = decodedOpenValue,
            EncodingRules = AsnEncodingRules.Der,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = [0x02, 0x01, 0x2a]
        };
        AsnRuntimeFieldValue field = new AsnRuntimeFieldValue
        {
            Name = "payload",
            Present = true,
            Value = decodedOpenValue
        };
        AsnRuntimeValue root = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            BitStringValue = bits,
            ObjectIdentifierValue = oid,
            RealValue = real,
            TimeValue = time,
            Fields = [field],
            Items = [decodedOpenValue],
            OpenTypeValue = openType,
            UnknownExtensions = [extension],
            UnknownFields = [unknownField]
        };

        Assert.That(root.BitStringValue?.IsValid(), Is.True);
        Assert.That(root.ObjectIdentifierValue?.IsValid(), Is.True);
        Assert.That(root.RealValue?.IsValid(), Is.True);
        Assert.That(root.TimeValue?.IsValid(), Is.True);
        Assert.That(root.Fields[0].Value, Is.SameAs(decodedOpenValue));
        Assert.That(root.OpenTypeValue?.IsValid(), Is.True);
        Assert.That(root.OpenTypeValue?.OriginalData, Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
        Assert.That(root.UnknownExtensions[0].IsValid(), Is.True);
        Assert.That(root.UnknownFields[0].IsValid(), Is.True);
    }
}
