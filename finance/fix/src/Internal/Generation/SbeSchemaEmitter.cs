using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class SbeSchemaEmitter
{
    internal static string EmitSchema(SbeMappingPlan plan)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.Append("<sbe:messageSchema xmlns:sbe=\"http://fixprotocol.io/2016/sbe\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        if (plan.Package.Length != 0) AppendSchemaAttribute(builder, "package", plan.Package);
        AppendSchemaAttribute(builder, "id", plan.SchemaId.ToString(CultureInfo.InvariantCulture));
        AppendSchemaAttribute(builder, "version", plan.Version.ToString(CultureInfo.InvariantCulture));
        AppendSchemaAttribute(builder, "byteOrder", plan.ByteOrder);
        AppendSchemaAttribute(builder, "headerType", "messageHeader");
        AppendSchemaAttribute(builder, "xsi:schemaLocation", "http://fixprotocol.io/2016/sbe sbe.xsd");
        builder.AppendLine(">");
        builder.AppendLine("  <types>");
        EmitMessageHeaderType(builder);
        EmitGroupSizeType(builder);
        for (int index = 0; index < plan.Types.Count; index++)
        {
            if (!IsComponentEncoding(plan, plan.Types[index].Name)) EmitSchemaType(builder, plan.Types[index], plan);
        }
        builder.AppendLine("  </types>");
        for (int index = 0; index < plan.Messages.Count; index++) EmitSchemaMessage(builder, plan.Messages[index], plan);
        builder.AppendLine("</sbe:messageSchema>");
        return builder.ToString();
    }

    internal static void EmitMessageHeaderType(StringBuilder builder)
    {
        builder.AppendLine("    <composite name=\"messageHeader\">");
        builder.AppendLine("      <type name=\"blockLength\" primitiveType=\"uint16\" />");
        builder.AppendLine("      <type name=\"templateId\" primitiveType=\"uint16\" />");
        builder.AppendLine("      <type name=\"schemaId\" primitiveType=\"uint16\" />");
        builder.AppendLine("      <type name=\"version\" primitiveType=\"uint16\" />");
        builder.AppendLine("      <type name=\"numGroups\" primitiveType=\"uint16\" />");
        builder.AppendLine("      <type name=\"numVarDataFields\" primitiveType=\"uint16\" />");
        builder.AppendLine("    </composite>");
    }

    internal static void EmitGroupSizeType(StringBuilder builder)
    {
        builder.AppendLine("    <composite name=\"groupSizeEncoding\">");
        builder.AppendLine("      <type name=\"blockLength\" primitiveType=\"uint16\" semanticType=\"Length\" />");
        builder.AppendLine("      <type name=\"numInGroup\" primitiveType=\"uint16\" semanticType=\"NumInGroup\" />");
        builder.AppendLine("    </composite>");
    }

    internal static void EmitSchemaType(StringBuilder builder, SbeTypeMapping mapping, SbeMappingPlan plan)
    {
        if (mapping.Kind == "enum") EmitSchemaEnum(builder, mapping);
        else if (mapping.Kind == "composite") EmitSchemaComposite(builder, mapping, plan);
        else if (mapping.VariableLength) EmitVariableDataComposite(builder, mapping);
        else EmitScalarType(builder, mapping, plan);
    }

    internal static void EmitScalarType(StringBuilder builder, SbeTypeMapping mapping, SbeMappingPlan plan)
    {
        builder.Append("    <type");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(mapping.Name));
        AppendSchemaAttribute(builder, "primitiveType", ResolveEmissionPrimitive(mapping, plan));
        if (mapping.HasLength) AppendSchemaAttribute(builder, "length", mapping.Length.ToString(CultureInfo.InvariantCulture));
        if (mapping.Presence != "required") AppendSchemaAttribute(builder, "presence", mapping.Presence);
        if (mapping.NullValue.Length != 0) AppendSchemaAttribute(builder, "nullValue", mapping.NullValue);
        if (mapping.MinimumValue.Length != 0) AppendSchemaAttribute(builder, "minValue", mapping.MinimumValue);
        if (mapping.MaximumValue.Length != 0) AppendSchemaAttribute(builder, "maxValue", mapping.MaximumValue);
        if (mapping.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", mapping.SemanticType);
        AppendSchemaVersionAttributes(builder, mapping.SinceVersion, mapping.DeprecatedVersion);
        if (mapping.ConstantValue.Length == 0) builder.AppendLine(" />");
        else builder.Append('>').Append(EscapeSchemaText(mapping.ConstantValue)).AppendLine("</type>");
    }

    internal static void EmitVariableDataComposite(StringBuilder builder, SbeTypeMapping mapping)
    {
        builder.Append("    <composite");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(mapping.Name));
        if (mapping.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", mapping.SemanticType);
        AppendSchemaVersionAttributes(builder, mapping.SinceVersion, mapping.DeprecatedVersion);
        builder.AppendLine(">");
        builder.AppendLine("      <type name=\"length\" primitiveType=\"uint32\" semanticType=\"Length\" />");
        builder.Append("      <type name=\"varData\"");
        AppendSchemaAttribute(builder, "primitiveType", mapping.PrimitiveType.Length == 0 ? "uint8" : mapping.PrimitiveType);
        AppendSchemaAttribute(builder, "length", "0");
        if (mapping.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", mapping.SemanticType);
        builder.AppendLine(" />");
        builder.AppendLine("    </composite>");
    }

    internal static void EmitSchemaEnum(StringBuilder builder, SbeTypeMapping mapping)
    {
        builder.Append("    <enum");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(mapping.Name));
        AppendSchemaAttribute(builder, "encodingType", mapping.PrimitiveType);
        if (mapping.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", mapping.SemanticType);
        AppendSchemaVersionAttributes(builder, mapping.SinceVersion, mapping.DeprecatedVersion);
        builder.AppendLine(">");
        for (int index = 0; index < mapping.Values.Count; index++)
        {
            builder.Append("      <validValue");
            AppendSchemaAttribute(builder, "name", ResolveSchemaName(mapping.Values[index].Name));
            AppendSchemaVersionAttributes(builder, mapping.Values[index].SinceVersion, mapping.Values[index].DeprecatedVersion);
            builder.Append('>').Append(EscapeSchemaText(mapping.Values[index].Value)).AppendLine("</validValue>");
        }
        builder.AppendLine("    </enum>");
    }

    internal static void EmitSchemaComposite(StringBuilder builder, SbeTypeMapping mapping, SbeMappingPlan plan)
    {
        builder.Append("    <composite");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(mapping.Name));
        if (mapping.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", mapping.SemanticType);
        AppendSchemaVersionAttributes(builder, mapping.SinceVersion, mapping.DeprecatedVersion);
        builder.AppendLine(">");
        for (int index = 0; index < mapping.Members.Count; index++) EmitCompositeMember(builder, mapping, mapping.Members[index], plan, index);
        builder.AppendLine("    </composite>");
    }

    internal static void EmitCompositeMember(StringBuilder builder, SbeTypeMapping owner, SbeMemberMapping member, SbeMappingPlan plan, int index)
    {
        bool primitive = SbeTypeMapper.IsSbePrimitive(member.TypeName);
        if (primitive)
        {
            builder.Append("      <type");
            AppendSchemaAttribute(builder, "name", ResolveSchemaName(member.Name));
            AppendSchemaAttribute(builder, "primitiveType", SbeTypeMapper.NormalizePrimitiveName(member.TypeName));
            if (member.Presence != "required") AppendSchemaAttribute(builder, "presence", member.Presence);
            string nullValue = member.NullValue;
            if (nullValue.Length == 0 && index == 0) nullValue = owner.NullValue;
            if (nullValue.Length != 0) AppendSchemaAttribute(builder, "nullValue", nullValue);
            if (member.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", member.SemanticType);
            AppendSchemaVersionAttributes(builder, member.SinceVersion, member.DeprecatedVersion);
            if (member.ConstantValue.Length == 0) builder.AppendLine(" />");
            else builder.Append('>').Append(EscapeSchemaText(member.ConstantValue)).AppendLine("</type>");
        }
        else
        {
            builder.Append("      <ref");
            AppendSchemaAttribute(builder, "name", ResolveSchemaName(member.Name));
            AppendSchemaAttribute(builder, "type", ResolveSchemaTypeReference(member.TypeName));
            AppendSchemaVersionAttributes(builder, member.SinceVersion, member.DeprecatedVersion);
            builder.AppendLine(" />");
        }
    }

    internal static void EmitSchemaMessage(StringBuilder builder, SbeMessageMapping message, SbeMappingPlan plan)
    {
        builder.Append("  <sbe:message");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(message.Name));
        AppendSchemaAttribute(builder, "id", message.TemplateId.ToString(CultureInfo.InvariantCulture));
        if (message.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", message.SemanticType);
        AppendSchemaVersionAttributes(builder, message.SinceVersion, message.DeprecatedVersion);
        builder.AppendLine(">");
        EmitBlockMemberCategory(builder, message.Members, "field", 2, plan, 0);
        EmitBlockMemberCategory(builder, message.Members, "group", 2, plan, 0);
        EmitBlockMemberCategory(builder, message.Members, "data", 2, plan, 0);
        builder.AppendLine("  </sbe:message>");
    }

    internal static void EmitBlockMemberCategory(StringBuilder builder, List<SbeMemberMapping> members, string category, int indent, SbeMappingPlan plan, int depth)
    {
        if (depth > plan.Components.Count + plan.Groups.Count + 1) return;
        for (int index = 0; index < members.Count; index++)
        {
            SbeMemberMapping member = members[index];
            if (member.Kind == "component")
            {
                SbeComponentMapping? component = SbeSchemaValidation.FindSchemaComponent(plan.Components, member.TypeName);
                if (component != null) EmitBlockMemberCategory(builder, component.Members, category, indent, plan, depth + 1);
            }
            else if (member.Kind == category)
            {
                if (category == "group") EmitGroupMember(builder, member, indent, plan, depth + 1);
                else EmitFieldOrDataMember(builder, member, category, indent, plan);
            }
        }
    }

    internal static void EmitFieldOrDataMember(StringBuilder builder, SbeMemberMapping member, string elementName, int indent, SbeMappingPlan plan)
    {
        AppendIndent(builder, indent);
        builder.Append('<').Append(elementName);
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(member.Name));
        AppendSchemaAttribute(builder, "id", member.Id.ToString(CultureInfo.InvariantCulture));
        AppendSchemaAttribute(builder, "type", ResolveSchemaTypeReference(member.TypeName));
        if (member.Presence != "required") AppendSchemaAttribute(builder, "presence", member.Presence);
        if (member.SemanticType.Length != 0) AppendSchemaAttribute(builder, "semanticType", member.SemanticType);
        if (member.Presence == "constant")
        {
            string valueReference = ResolveConstantValueReference(plan, member.TypeName, member.ConstantValue);
            if (valueReference.Length != 0) AppendSchemaAttribute(builder, "valueRef", valueReference);
        }
        AppendSchemaVersionAttributes(builder, member.SinceVersion, member.DeprecatedVersion);
        builder.AppendLine(" />");
    }

    internal static void EmitGroupMember(StringBuilder builder, SbeMemberMapping member, int indent, SbeMappingPlan plan, int depth)
    {
        SbeGroupMapping? group = SbeSchemaValidation.FindSchemaGroup(plan.Groups, member.TypeName);
        if (group == null) return;
        AppendIndent(builder, indent);
        builder.Append("<group");
        AppendSchemaAttribute(builder, "name", ResolveSchemaName(member.Name));
        AppendSchemaAttribute(builder, "id", member.Id.ToString(CultureInfo.InvariantCulture));
        AppendSchemaAttribute(builder, "dimensionType", ResolveSchemaTypeReference(group.DimensionType));
        int sinceVersion = member.SinceVersion > group.SinceVersion ? member.SinceVersion : group.SinceVersion;
        int deprecatedVersion = member.DeprecatedVersion > 0 ? member.DeprecatedVersion : group.DeprecatedVersion;
        AppendSchemaVersionAttributes(builder, sinceVersion, deprecatedVersion);
        builder.AppendLine(">");
        EmitBlockMemberCategory(builder, group.Members, "field", indent + 1, plan, depth);
        EmitBlockMemberCategory(builder, group.Members, "group", indent + 1, plan, depth);
        EmitBlockMemberCategory(builder, group.Members, "data", indent + 1, plan, depth);
        AppendIndent(builder, indent);
        builder.AppendLine("</group>");
    }

    internal static string ResolveConstantValueReference(SbeMappingPlan plan, string typeName, string constantValue)
    {
        SbeTypeMapping? type = SbeSchemaValidation.FindSchemaType(plan.Types, typeName);
        if (type == null || type.Kind != "enum") return "";
        for (int index = 0; index < type.Values.Count; index++)
        {
            if (type.Values[index].Value == constantValue) return ResolveSchemaName(type.Name) + "." + ResolveSchemaName(type.Values[index].Name);
        }
        return "";
    }

    internal static string ResolveEmissionPrimitive(SbeTypeMapping mapping, SbeMappingPlan plan)
    {
        if (mapping.PrimitiveType.Length != 0) return SbeTypeMapper.NormalizePrimitiveName(mapping.PrimitiveType);
        string current = mapping.ReferenceType;
        int remaining = plan.Types.Count;
        while (current.Length != 0 && remaining > 0)
        {
            if (SbeTypeMapper.IsSbePrimitive(current)) return SbeTypeMapper.NormalizePrimitiveName(current);
            SbeTypeMapping? referenced = SbeSchemaValidation.FindSchemaType(plan.Types, current);
            if (referenced == null) break;
            if (referenced.PrimitiveType.Length != 0) return SbeTypeMapper.NormalizePrimitiveName(referenced.PrimitiveType);
            current = referenced.ReferenceType;
            remaining--;
        }
        return "char";
    }

    internal static bool IsComponentEncoding(SbeMappingPlan plan, string name)
    {
        return SbeSchemaValidation.FindSchemaComponent(plan.Components, name) != null;
    }

    internal static string ResolveSchemaTypeReference(string name)
    {
        return SbeTypeMapper.IsSbePrimitive(name) ? SbeTypeMapper.NormalizePrimitiveName(name) : ResolveSchemaName(name);
    }

    internal static string ResolveSchemaName(string value)
    {
        string source = value == null ? "" : value.Trim();
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < source.Length; index++)
        {
            char character = source[index];
            bool valid = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '_';
            if (valid)
            {
                if (builder.Length == 0 && character >= '0' && character <= '9') builder.Append("Generated");
                builder.Append(character);
            }
            else if (builder.Length != 0 && builder[builder.Length - 1] != '_') builder.Append('_');
        }
        if (builder.Length == 0) builder.Append("Generated");
        if (builder.Length > 64) builder.Length = 64;
        return builder.ToString();
    }

    internal static void AppendSchemaAttribute(StringBuilder builder, string name, string value)
    {
        builder.Append(' ').Append(name).Append("=\"").Append(EscapeSchemaAttribute(value)).Append('"');
    }

    internal static void AppendSchemaVersionAttributes(StringBuilder builder, int sinceVersion, int deprecatedVersion)
    {
        if (sinceVersion > 0) AppendSchemaAttribute(builder, "sinceVersion", sinceVersion.ToString(CultureInfo.InvariantCulture));
        if (deprecatedVersion > 0) AppendSchemaAttribute(builder, "deprecated", deprecatedVersion.ToString(CultureInfo.InvariantCulture));
    }

    internal static string EscapeSchemaAttribute(string value)
    {
        return EscapeSchemaText(value).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("'", "&apos;", StringComparison.Ordinal);
    }

    internal static string EscapeSchemaText(string value)
    {
        string source = value == null ? "" : value;
        return source.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
    }

    internal static void AppendIndent(StringBuilder builder, int indent)
    {
        for (int index = 0; index < indent; index++) builder.Append("  ");
    }
}
