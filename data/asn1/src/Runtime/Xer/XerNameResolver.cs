namespace asn1;

internal class XerNameResolver
{
    internal XerXmlCharacters Characters = new XerXmlCharacters();

    internal XerNameResult GetElementName(AsnTypeDescriptor type)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        string name = type.Name.Length > 0
            ? type.Name
            : this.GetBuiltinElementName(type.Kind);
        if (!this.Characters.IsValidNcName(name))
        {
            return new XerNameResult
            {
                ErrorCode = "ASNXER0100",
                ErrorMessage = "The ASN.1 type does not have a valid BASIC-XER element name."
            };
        }

        return new XerNameResult
        {
            Success = true,
            Name = name
        };
    }

    internal string GetBuiltinElementName(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.BitString)
        {
            return "BIT_STRING";
        }

        if (kind == AsnTypeKind.Boolean)
        {
            return "BOOLEAN";
        }

        if (kind == AsnTypeKind.Choice)
        {
            return "CHOICE";
        }

        if (kind == AsnTypeKind.Date)
        {
            return "DATE";
        }

        if (kind == AsnTypeKind.DateTime)
        {
            return "DATE_TIME";
        }

        if (kind == AsnTypeKind.Duration)
        {
            return "DURATION";
        }

        if (kind == AsnTypeKind.EmbeddedPdv
            || kind == AsnTypeKind.External
            || kind == AsnTypeKind.InstanceOf
            || kind == AsnTypeKind.CharacterString)
        {
            return "SEQUENCE";
        }

        if (kind == AsnTypeKind.Enumerated)
        {
            return "ENUMERATED";
        }

        if (kind == AsnTypeKind.Integer)
        {
            return "INTEGER";
        }

        if (kind == AsnTypeKind.Null)
        {
            return "NULL";
        }

        if (kind == AsnTypeKind.ObjectIdentifier)
        {
            return "OBJECT_IDENTIFIER";
        }

        if (kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return "OID_IRI";
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return "OCTET_STRING";
        }

        if (kind == AsnTypeKind.OpenType)
        {
            return "OPEN_TYPE";
        }

        if (kind == AsnTypeKind.Real)
        {
            return "REAL";
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return "RELATIVE_OID";
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return "RELATIVE_OID_IRI";
        }

        if (kind == AsnTypeKind.Sequence)
        {
            return "SEQUENCE";
        }

        if (kind == AsnTypeKind.SequenceOf)
        {
            return "SEQUENCE_OF";
        }

        if (kind == AsnTypeKind.Set)
        {
            return "SET";
        }

        if (kind == AsnTypeKind.SetOf)
        {
            return "SET_OF";
        }

        if (kind == AsnTypeKind.Time)
        {
            return "TIME";
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            return "TIME_OF_DAY";
        }

        if (kind == AsnTypeKind.ObjectDescriptor)
        {
            return "ObjectDescriptor";
        }

        if (kind == AsnTypeKind.UtcTime)
        {
            return "UTCTime";
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return "GeneralizedTime";
        }

        if (kind == AsnTypeKind.Utf8String)
        {
            return "UTF8String";
        }

        if (kind == AsnTypeKind.NumericString)
        {
            return "NumericString";
        }

        if (kind == AsnTypeKind.PrintableString)
        {
            return "PrintableString";
        }

        if (kind == AsnTypeKind.TeletexString)
        {
            return "TeletexString";
        }

        if (kind == AsnTypeKind.VideotexString)
        {
            return "VideotexString";
        }

        if (kind == AsnTypeKind.Ia5String)
        {
            return "IA5String";
        }

        if (kind == AsnTypeKind.GraphicString)
        {
            return "GraphicString";
        }

        if (kind == AsnTypeKind.VisibleString)
        {
            return "VisibleString";
        }

        if (kind == AsnTypeKind.GeneralString)
        {
            return "GeneralString";
        }

        if (kind == AsnTypeKind.UniversalString)
        {
            return "UniversalString";
        }

        return kind == AsnTypeKind.BmpString ? "BMPString" : "";
    }
}
