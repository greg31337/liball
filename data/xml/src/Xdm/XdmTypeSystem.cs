using System;
using System.Collections.Generic;

namespace xml;

internal class XdmTypeSystem
{
    internal Dictionary<XdmAtomicType, XdmAtomicTypeDefinition> AtomicTypes;
    internal XdmAtomicTypeDefinition[] OrderedAtomicTypes;
    internal XmlName UntypedNodeTypeName;

    internal XdmTypeSystem()
    {
        AtomicTypes = new Dictionary<XdmAtomicType, XdmAtomicTypeDefinition>();
        OrderedAtomicTypes = new XdmAtomicTypeDefinition[46];
        UntypedNodeTypeName = CreateXdmTypeName("untyped");

        AddXdmAtomicTypeDefinition(XdmAtomicType.AnyAtomicType, "anyAtomicType", XdmAtomicType.AnyAtomicType, XdmAtomicType.AnyAtomicType, false, false, false, true);
        AddXdmAtomicTypeDefinition(XdmAtomicType.UntypedAtomic, "untypedAtomic", XdmAtomicType.AnyAtomicType, XdmAtomicType.UntypedAtomic, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.String, "string", XdmAtomicType.AnyAtomicType, XdmAtomicType.String, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.NormalizedString, "normalizedString", XdmAtomicType.String, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Token, "token", XdmAtomicType.NormalizedString, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Language, "language", XdmAtomicType.Token, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Name, "Name", XdmAtomicType.Token, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.NcName, "NCName", XdmAtomicType.Name, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Nmtoken, "NMTOKEN", XdmAtomicType.Token, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Id, "ID", XdmAtomicType.NcName, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Idref, "IDREF", XdmAtomicType.NcName, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Entity, "ENTITY", XdmAtomicType.NcName, XdmAtomicType.String, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Boolean, "boolean", XdmAtomicType.AnyAtomicType, XdmAtomicType.Boolean, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Decimal, "decimal", XdmAtomicType.AnyAtomicType, XdmAtomicType.Decimal, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Integer, "integer", XdmAtomicType.Decimal, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.NonPositiveInteger, "nonPositiveInteger", XdmAtomicType.Integer, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.NegativeInteger, "negativeInteger", XdmAtomicType.NonPositiveInteger, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Long, "long", XdmAtomicType.Integer, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Int, "int", XdmAtomicType.Long, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Short, "short", XdmAtomicType.Int, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Byte, "byte", XdmAtomicType.Short, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.NonNegativeInteger, "nonNegativeInteger", XdmAtomicType.Integer, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.UnsignedLong, "unsignedLong", XdmAtomicType.NonNegativeInteger, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.UnsignedInt, "unsignedInt", XdmAtomicType.UnsignedLong, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.UnsignedShort, "unsignedShort", XdmAtomicType.UnsignedInt, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.UnsignedByte, "unsignedByte", XdmAtomicType.UnsignedShort, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.PositiveInteger, "positiveInteger", XdmAtomicType.NonNegativeInteger, XdmAtomicType.Decimal, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Float, "float", XdmAtomicType.AnyAtomicType, XdmAtomicType.Float, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Double, "double", XdmAtomicType.AnyAtomicType, XdmAtomicType.Double, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Duration, "duration", XdmAtomicType.AnyAtomicType, XdmAtomicType.Duration, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.YearMonthDuration, "yearMonthDuration", XdmAtomicType.Duration, XdmAtomicType.Duration, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.DayTimeDuration, "dayTimeDuration", XdmAtomicType.Duration, XdmAtomicType.Duration, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.DateTime, "dateTime", XdmAtomicType.AnyAtomicType, XdmAtomicType.DateTime, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.DateTimeStamp, "dateTimeStamp", XdmAtomicType.DateTime, XdmAtomicType.DateTime, true, true, false, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Date, "date", XdmAtomicType.AnyAtomicType, XdmAtomicType.Date, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Time, "time", XdmAtomicType.AnyAtomicType, XdmAtomicType.Time, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.GYearMonth, "gYearMonth", XdmAtomicType.AnyAtomicType, XdmAtomicType.GYearMonth, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.GYear, "gYear", XdmAtomicType.AnyAtomicType, XdmAtomicType.GYear, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.GMonthDay, "gMonthDay", XdmAtomicType.AnyAtomicType, XdmAtomicType.GMonthDay, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.GMonth, "gMonth", XdmAtomicType.AnyAtomicType, XdmAtomicType.GMonth, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.GDay, "gDay", XdmAtomicType.AnyAtomicType, XdmAtomicType.GDay, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.HexBinary, "hexBinary", XdmAtomicType.AnyAtomicType, XdmAtomicType.HexBinary, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Base64Binary, "base64Binary", XdmAtomicType.AnyAtomicType, XdmAtomicType.Base64Binary, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.AnyUri, "anyURI", XdmAtomicType.AnyAtomicType, XdmAtomicType.AnyUri, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.QName, "QName", XdmAtomicType.AnyAtomicType, XdmAtomicType.QName, true, true, true, false);
        AddXdmAtomicTypeDefinition(XdmAtomicType.Notation, "NOTATION", XdmAtomicType.AnyAtomicType, XdmAtomicType.Notation, true, true, true, false);
    }

    internal XdmAtomicTypeDefinition GetXdmAtomicTypeDefinition(XdmAtomicType type)
    {
        if (!AtomicTypes.ContainsKey(type))
        {
            throw new ArgumentOutOfRangeException("type");
        }

        return AtomicTypes[type];
    }

    internal XdmAtomicTypeDefinition? FindXdmAtomicTypeDefinition(XmlName name)
    {
        Guard.NotNull(name, "name");
        if (!string.Equals(name.NamespaceUri, XPathNamespaces.XmlSchema, StringComparison.Ordinal) || name.LocalName == null)
        {
            return null;
        }

        int index = 0;
        while (index < OrderedAtomicTypes.Length)
        {
            XdmAtomicTypeDefinition definition = OrderedAtomicTypes[index];
            if (string.Equals(definition.Name.LocalName, name.LocalName, StringComparison.Ordinal))
            {
                return definition;
            }

            index = index + 1;
        }

        return null;
    }

    internal XdmAtomicTypeDefinition[] GetAllXdmAtomicTypeDefinitions()
    {
        XdmAtomicTypeDefinition[] result = new XdmAtomicTypeDefinition[OrderedAtomicTypes.Length];
        Array.Copy(OrderedAtomicTypes, result, result.Length);
        return result;
    }

    internal XdmAtomicTypeDefinition? GetXdmBaseAtomicTypeDefinition(XdmAtomicType type)
    {
        XdmAtomicTypeDefinition definition = GetXdmAtomicTypeDefinition(type);
        if (!definition.HasBaseType)
        {
            return null;
        }

        return GetXdmAtomicTypeDefinition(definition.BaseType);
    }

    internal XdmAtomicTypeDefinition? GetXdmPrimitiveAtomicTypeDefinition(XdmAtomicType type)
    {
        XdmAtomicTypeDefinition definition = GetXdmAtomicTypeDefinition(type);
        if (!definition.HasPrimitiveType)
        {
            return null;
        }

        return GetXdmAtomicTypeDefinition(definition.PrimitiveType);
    }

    internal bool IsXdmAtomicSubtypeOf(XdmAtomicType type, XdmAtomicType expectedBaseType)
    {
        XdmAtomicTypeDefinition current = GetXdmAtomicTypeDefinition(type);
        GetXdmAtomicTypeDefinition(expectedBaseType);
        while (true)
        {
            if (current.Type == expectedBaseType)
            {
                return true;
            }

            if (!current.HasBaseType)
            {
                return false;
            }

            current = GetXdmAtomicTypeDefinition(current.BaseType);
        }
    }

    internal bool IsXdmPrimitiveAtomicType(XdmAtomicType type)
    {
        return GetXdmAtomicTypeDefinition(type).IsPrimitive;
    }

    internal bool IsXdmNumericAtomicType(XdmAtomicType type)
    {
        return IsXdmAtomicSubtypeOf(type, XdmAtomicType.Decimal) || type == XdmAtomicType.Float || type == XdmAtomicType.Double;
    }

    internal bool IsXdmStringDerivedAtomicType(XdmAtomicType type)
    {
        return IsXdmAtomicSubtypeOf(type, XdmAtomicType.String);
    }

    internal bool IsXdmDurationDerivedAtomicType(XdmAtomicType type)
    {
        return IsXdmAtomicSubtypeOf(type, XdmAtomicType.Duration);
    }

    internal bool IsXdmNamespaceSensitiveAtomicType(XdmAtomicType type)
    {
        GetXdmAtomicTypeDefinition(type);
        return type == XdmAtomicType.QName || type == XdmAtomicType.Notation;
    }

    internal XmlName GetXdmUntypedNodeTypeName()
    {
        return CopyXdmTypeName(UntypedNodeTypeName);
    }

    internal bool IsXdmUntypedNodeTypeName(XmlName name)
    {
        Guard.NotNull(name, "name");
        return string.Equals(name.NamespaceUri, XPathNamespaces.XmlSchema, StringComparison.Ordinal) && string.Equals(name.LocalName, "untyped", StringComparison.Ordinal);
    }

    internal void AddXdmAtomicTypeDefinition(XdmAtomicType type, string localName, XdmAtomicType baseType, XdmAtomicType primitiveType, bool hasBaseType, bool hasPrimitiveType, bool isPrimitive, bool isAbstract)
    {
        XdmAtomicTypeDefinition definition = new XdmAtomicTypeDefinition(type, CreateXdmTypeName(localName), baseType, primitiveType, hasBaseType, hasPrimitiveType, isPrimitive, isAbstract);
        AtomicTypes[type] = definition;
        OrderedAtomicTypes[(int)type] = definition;
    }

    internal XmlName CreateXdmTypeName(string localName)
    {
        XmlName name = new XmlName();
        name.Prefix = "xs";
        name.LocalName = localName;
        name.NamespaceUri = XPathNamespaces.XmlSchema;
        name.QualifiedName = "xs:" + localName;
        return name;
    }

    internal XmlName CopyXdmTypeName(XmlName source)
    {
        XmlName name = new XmlName();
        name.Prefix = source.Prefix;
        name.LocalName = source.LocalName;
        name.NamespaceUri = source.NamespaceUri;
        name.QualifiedName = source.QualifiedName;
        return name;
    }
}
