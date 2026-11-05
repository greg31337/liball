using System;

namespace xml;

internal class XdmFactory
{
    internal XdmTypeSystem TypeSystem;

    internal XdmFactory()
    {
        TypeSystem = new XdmTypeSystem();
    }

    internal XdmAtomicValue CreateXdmAtomicValue(XdmAtomicType type)
    {
        XdmAtomicTypeDefinition definition = TypeSystem.GetXdmAtomicTypeDefinition(type);
        if (definition.IsAbstract)
        {
            throw new ArgumentException("The requested atomic type cannot label a value directly.", "type");
        }

        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = type;
        value.IsImmutable = false;
        return value;
    }

    internal XdmAtomicValue CreateXdmStringAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmStringDatatype().ParseXdmStringAtomic(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmBooleanAtomicValue(string lexicalValue)
    {
        return new XdmNumericDatatype().ParseXdmBoolean(lexicalValue);
    }

    internal XdmAtomicValue CreateXdmDecimalAtomicValue(string lexicalValue)
    {
        return new XdmNumericDatatype().ParseXdmDecimal(lexicalValue);
    }

    internal XdmAtomicValue CreateXdmIntegerAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmNumericDatatype().ParseXdmInteger(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmFloatingAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmFloatingDatatype().ParseXdmFloating(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmDurationAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmDurationDatatype().ParseXdmDuration(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmDateTimeAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmDateTimeDatatype().ParseXdmDateTime(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmBinaryAtomicValue(string lexicalValue, XdmAtomicType type)
    {
        return new XdmBinaryDatatype().ParseXdmBinary(lexicalValue, type);
    }

    internal XdmAtomicValue CreateXdmAnyUriAtomicValue(string lexicalValue)
    {
        return new XdmAnyUriDatatype().ParseXdmAnyUri(lexicalValue);
    }

    internal XdmAtomicValue CreateXdmQNameAtomicValue(string lexicalValue, XPathStaticContext context)
    {
        return new XdmQNameDatatype().ResolveXdmQNameStatic(lexicalValue, context);
    }

    internal XdmAtomicValue FreezeXdmAtomicValue(XdmAtomicValue value)
    {
        ValidateXdmAtomicValue(value);
        XdmAtomicValue result = new XdmAtomicValue();
        result.Type = value.Type;
        result.LexicalValue = value.LexicalValue;
        result.CanonicalValue = value.CanonicalValue;
        result.StringValue = value.StringValue;
        result.BooleanValue = value.BooleanValue;
        result.DecimalValue = value.DecimalValue;
        result.IntegerValue = value.IntegerValue;
        result.FloatingValue = value.FloatingValue;
        result.BinaryValue = CopyXdmAtomicBinary(value.BinaryValue);
        result.NameValue = CopyXdmAtomicName(value.NameValue);
        result.ExtendedValue = value.ExtendedValue;
        result.TypeInfo = value.TypeInfo;
        result.IsImmutable = true;
        return result;
    }

    internal void ValidateXdmAtomicValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        XdmAtomicTypeDefinition definition = TypeSystem.GetXdmAtomicTypeDefinition(value.Type);
        if (definition.IsAbstract)
        {
            throw new ArgumentException("The atomic value uses a non-instantiable type.", "value");
        }

        if ((value.Type == XdmAtomicType.HexBinary || value.Type == XdmAtomicType.Base64Binary) && value.BinaryValue == null)
        {
            throw new ArgumentException("A binary atomic value must provide binary storage.", "value");
        }

        if ((value.Type == XdmAtomicType.QName || value.Type == XdmAtomicType.Notation) && (value.NameValue == null || value.NameValue.LocalName == null || value.NameValue.LocalName.Length == 0))
        {
            throw new ArgumentException("A namespace-sensitive atomic value must provide a local name.", "value");
        }

        if (!HasXdmAtomicValueStorage(value))
        {
            throw new ArgumentException("The atomic value does not provide storage for its value category.", "value");
        }

        if (TypeSystem.IsXdmStringDerivedAtomicType(value.Type))
        {
            ValidateXdmStringAtomicStorage(value);
        }

        if (value.Type == XdmAtomicType.HexBinary || value.Type == XdmAtomicType.Base64Binary)
        {
            ValidateXdmBinaryAtomicStorage(value);
        }

        if (value.Type == XdmAtomicType.AnyUri)
        {
            ValidateXdmAnyUriAtomicStorage(value);
        }

        if (value.Type == XdmAtomicType.QName || value.Type == XdmAtomicType.Notation)
        {
            ValidateXdmQNameAtomicStorage(value);
        }
    }

    internal bool HasXdmAtomicValueStorage(XdmAtomicValue value)
    {
        XdmAtomicType type = value.Type;
        if (type == XdmAtomicType.Boolean || TypeSystem.IsXdmNumericAtomicType(type))
        {
            return true;
        }

        if (type == XdmAtomicType.HexBinary || type == XdmAtomicType.Base64Binary)
        {
            return value.BinaryValue != null;
        }

        if (type == XdmAtomicType.QName || type == XdmAtomicType.Notation)
        {
            return value.NameValue != null && value.NameValue.LocalName != null && value.NameValue.LocalName.Length != 0;
        }

        return value.StringValue != null || value.CanonicalValue != null || value.LexicalValue != null || value.ExtendedValue != null;
    }

    internal void ValidateXdmStringAtomicStorage(XdmAtomicValue value)
    {
        XdmStringDatatype datatype = new XdmStringDatatype();
        string? storedValue = value.StringValue == null ? value.CanonicalValue : value.StringValue;
        if (storedValue == null && value.LexicalValue == null)
        {
            throw new ArgumentException("A string-derived atomic value must provide a lexical or string representation.", "value");
        }

        if (storedValue != null && !datatype.IsValidXdmStringValue(storedValue, value.Type))
        {
            throw new ArgumentException("The stored string value is outside the datatype value space.", "value");
        }

        if (value.CanonicalValue != null && storedValue != null && !string.Equals(value.CanonicalValue, storedValue, StringComparison.Ordinal))
        {
            throw new ArgumentException("The canonical string representation does not match the stored value.", "value");
        }

        if (value.LexicalValue != null)
        {
            XdmStringParseResult parsed = datatype.TryParseXdmStringAtomic(value.LexicalValue, value.Type);
            if (!parsed.Success || parsed.Value == null)
            {
                throw new ArgumentException("The lexical string representation is invalid.", "value");
            }

            if (storedValue != null && !string.Equals(parsed.Value.StringValue, storedValue, StringComparison.Ordinal))
            {
                throw new ArgumentException("The lexical string representation does not produce the stored value.", "value");
            }
        }
    }

    internal void ValidateXdmBinaryAtomicStorage(XdmAtomicValue value)
    {
        XdmBinaryDatatype datatype = new XdmBinaryDatatype();
        if (value.BinaryValue == null)
        {
            throw new ArgumentException("A binary atomic value must provide binary storage.", "value");
        }

        string canonical = datatype.CanonicalizeXdmBinary(value.BinaryValue, value.Type);
        if (value.CanonicalValue != null && !string.Equals(value.CanonicalValue, canonical, StringComparison.Ordinal))
        {
            throw new ArgumentException("The canonical binary representation does not match the stored octets.", "value");
        }

        if (value.StringValue != null && !string.Equals(value.StringValue, canonical, StringComparison.Ordinal))
        {
            throw new ArgumentException("The binary string representation does not match the stored octets.", "value");
        }

        if (value.LexicalValue != null)
        {
            XdmNumericParseResult parsed = datatype.TryParseXdmBinary(value.LexicalValue, value.Type);
            if (!parsed.Success || parsed.Value == null || !datatype.EqualXdmBinary(value, parsed.Value))
            {
                throw new ArgumentException("The lexical binary representation does not match the stored octets.", "value");
            }
        }
    }

    internal void ValidateXdmAnyUriAtomicStorage(XdmAtomicValue value)
    {
        XdmAnyUriDatatype datatype = new XdmAnyUriDatatype();
        string stored = datatype.ReadXdmAnyUriValue(value);
        if (value.CanonicalValue != null && !string.Equals(value.CanonicalValue, stored, StringComparison.Ordinal))
        {
            throw new ArgumentException("The canonical xs:anyURI representation does not match its string value.", "value");
        }

        if (value.LexicalValue != null)
        {
            XdmStringParseResult parsed = datatype.TryParseXdmAnyUri(value.LexicalValue);
            if (!parsed.Success || parsed.Value == null || !string.Equals(parsed.Value.StringValue, stored, StringComparison.Ordinal))
            {
                throw new ArgumentException("The lexical xs:anyURI representation does not match its string value.", "value");
            }
        }
    }

    internal void ValidateXdmQNameAtomicStorage(XdmAtomicValue value)
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XmlName name = datatype.ReadXdmExpandedName(value, value.Type);
        string lexical = datatype.BuildXdmQNameLexical(name);
        if (value.StringValue != null && !string.Equals(value.StringValue, lexical, StringComparison.Ordinal))
        {
            throw new ArgumentException("The namespace-sensitive string representation does not match its expanded QName.", "value");
        }

        if (value.CanonicalValue != null && !string.Equals(value.CanonicalValue, lexical, StringComparison.Ordinal))
        {
            throw new ArgumentException("The namespace-sensitive canonical representation does not match its expanded QName.", "value");
        }

        if (value.LexicalValue != null)
        {
            XmlName? lexicalName = datatype.ParseXdmQNameLexical(new XdmStringDatatype().CollapseXdmStringWhitespace(value.LexicalValue));
            if (lexicalName == null || !string.Equals(lexicalName.Prefix, name.Prefix, StringComparison.Ordinal) || !string.Equals(lexicalName.LocalName, name.LocalName, StringComparison.Ordinal))
            {
                throw new ArgumentException("The lexical QName representation does not match its expanded QName.", "value");
            }
        }
    }

    internal byte[]? CopyXdmAtomicBinary(byte[]? source)
    {
        if (source == null)
        {
            return null;
        }

        byte[] result = new byte[source.Length];
        Array.Copy(source, result, result.Length);
        return result;
    }

    internal XmlName? CopyXdmAtomicName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }
}
