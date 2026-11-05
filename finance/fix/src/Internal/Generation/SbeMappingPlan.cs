using System.Collections.Generic;

namespace fix;

internal class SbeMappingPlan
{
    internal string Package = "";
    internal int SchemaId;
    internal int Version;
    internal string ByteOrder = "littleEndian";
    internal List<SbeTypeMapping> Types = [];
    internal List<SbeFieldMapping> Fields = [];
    internal List<SbeComponentMapping> Components = [];
    internal List<SbeGroupMapping> Groups = [];
    internal List<SbeMessageMapping> Messages = [];
    internal List<GenerationDiagnostic> Diagnostics = [];
}

internal class SbeTypeMapping
{
    internal string Name = "";
    internal string Kind = "type";
    internal string PrimitiveType = "";
    internal string ReferenceType = "";
    internal string SemanticType = "";
    internal int Length;
    internal bool HasLength;
    internal bool VariableLength;
    internal string Presence = "required";
    internal string ConstantValue = "";
    internal string NullValue = "";
    internal string MinimumValue = "";
    internal string MaximumValue = "";
    internal int SinceVersion;
    internal int DeprecatedVersion;
    internal List<SbeValueMapping> Values = [];
    internal List<SbeMemberMapping> Members = [];
}

internal class SbeValueMapping
{
    internal string Name = "";
    internal string Value = "";
    internal int SinceVersion;
    internal int DeprecatedVersion;
}

internal class SbeFieldMapping
{
    internal int SourceId;
    internal int Id;
    internal string Name = "";
    internal string TypeName = "";
    internal string Kind = "field";
    internal string SemanticType = "";
    internal string Presence = "required";
    internal string ConstantValue = "";
    internal string NullValue = "";
    internal int SinceVersion;
    internal int DeprecatedVersion;
}

internal class SbeComponentMapping
{
    internal int Id;
    internal string Name = "";
    internal int SinceVersion;
    internal int DeprecatedVersion;
    internal List<SbeMemberMapping> Members = [];
}

internal class SbeGroupMapping
{
    internal int Id;
    internal string Name = "";
    internal string DimensionType = "groupSizeEncoding";
    internal int SinceVersion;
    internal int DeprecatedVersion;
    internal List<SbeMemberMapping> Members = [];
}

internal class SbeMessageMapping
{
    internal int SourceId;
    internal int TemplateId;
    internal string Name = "";
    internal string MessageType = "";
    internal string SemanticType = "";
    internal int SinceVersion;
    internal int DeprecatedVersion;
    internal List<SbeMemberMapping> Members = [];
}

internal class SbeMemberMapping
{
    internal int Position;
    internal int Id;
    internal string Name = "";
    internal string Kind = "field";
    internal string TypeName = "";
    internal string SemanticType = "";
    internal string Presence = "required";
    internal string ConstantValue = "";
    internal string NullValue = "";
    internal bool Conditional;
    internal int SinceVersion;
    internal int DeprecatedVersion;
}
