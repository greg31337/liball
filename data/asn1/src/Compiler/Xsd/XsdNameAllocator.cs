namespace asn1;

internal class XsdNameAllocator
{
    internal System.Collections.Generic.List<string> TypeNames =
        new System.Collections.Generic.List<string>();

    internal void ReserveSupportNames(XsdV2SupportModule support)
    {
        int index = 0;
        while (index < support.Types.Length)
        {
            this.TypeNames.Add(support.Types[index].Name);
            index = index + 1;
        }
    }

    internal string AllocateTypeName(string source)
    {
        string candidate = new XsdDeclarationMapper().ToTypeReference(source);
        string result = candidate;
        int suffix = 1;
        while (this.ContainsTypeName(result) || this.IsReservedWord(result))
        {
            result = candidate + "-" + suffix.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }
        this.TypeNames.Add(result);
        return result;
    }

    internal string AllocateIdentifier(
        string source,
        System.Collections.Generic.List<string> used)
    {
        string candidate = new XsdDeclarationMapper().ToIdentifier(source);
        string result = candidate;
        int suffix = 1;
        while (this.ContainsIdentifier(used, result))
        {
            result = candidate + "-" + suffix.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }
        used.Add(result);
        return result;
    }

    internal bool ContainsTypeName(string value)
    {
        int index = 0;
        while (index < this.TypeNames.Count)
        {
            if (this.TypeNames[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsIdentifier(
        System.Collections.Generic.List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool IsReservedWord(string value)
    {
        switch (value)
        {
            case "ABSENT":
            case "ABSTRACT-SYNTAX":
            case "ALL":
            case "APPLICATION":
            case "AUTOMATIC":
            case "BEGIN":
            case "BIT":
            case "BITS":
            case "BMPString":
            case "BOOLEAN":
            case "BY":
            case "CHARACTER":
            case "CHOICE":
            case "CLASS":
            case "COMPONENT":
            case "COMPONENTS":
            case "CONSTRAINED":
            case "CONTAINING":
            case "COMPLETED":
            case "DATE":
            case "DATE-TIME":
            case "DEFAULT":
            case "DEFINITIONS":
            case "DURATION":
            case "EMBEDDED":
            case "ENCODED":
            case "ENCODING-CLASS":
            case "ENCODING-CONTROL":
            case "ENCODING-DEFINITIONS":
            case "ENCODE":
            case "ENCODE-DECODE":
            case "END":
            case "ENUMERATED":
            case "EXCEPT":
            case "EXPLICIT":
            case "EXPORTS":
            case "FIELDS":
            case "EXTENSIBILITY":
            case "EXTERNAL":
            case "FALSE":
            case "FROM":
            case "GeneralizedTime":
            case "GeneralString":
            case "GENERATES":
            case "GraphicString":
            case "IA5String":
            case "IDENTIFIER":
            case "IF":
            case "IMPLICIT":
            case "IMPLIED":
            case "IMPORTS":
            case "IN":
            case "INCLUDES":
            case "INSTANCE":
            case "INSTRUCTIONS":
            case "INTEGER":
            case "INTERSECTION":
            case "ISO646String":
            case "LINK-DEFINITIONS":
            case "MAPPING":
            case "MAX":
            case "MIN":
            case "MINUS-INFINITY":
            case "NON-ECN-BEGIN":
            case "NON-ECN-END":
            case "NOT-A-NUMBER":
            case "NULL":
            case "NumericString":
            case "OBJECT":
            case "ObjectDescriptor":
            case "OCTET":
            case "OF":
            case "OID-IRI":
            case "OPTIONAL":
            case "OPTIONAL-ENCODING":
            case "OPTIONS":
            case "ORDERED":
            case "OUTER":
            case "PATTERN":
            case "PDV":
            case "PER-BASIC-ALIGNED":
            case "PER-BASIC-UNALIGNED":
            case "PER-CANONICAL-ALIGNED":
            case "PER-CANONICAL-UNALIGNED":
            case "PLUS-INFINITY":
            case "PRESENT":
            case "PrintableString":
            case "PRIVATE":
            case "REAL":
            case "REFERENCE":
            case "RELATIVE-OID":
            case "RELATIVE-OID-IRI":
            case "REMAINDER":
            case "RENAMES":
            case "SEQUENCE":
            case "SET":
            case "SETTINGS":
            case "SIZE":
            case "STRING":
            case "STRUCTURE":
            case "STRUCTURED":
            case "SYNTAX":
            case "T61String":
            case "TAGS":
            case "TeletexString":
            case "TIME":
            case "TIME-OF-DAY":
            case "TO":
            case "TRANSFORMS":
            case "TRUE":
            case "UNION":
            case "UNIQUE":
            case "UNIVERSAL":
            case "USE":
            case "USE-SET":
            case "UniversalString":
            case "UTCTime":
            case "UTF8String":
            case "VALUES":
            case "VideotexString":
            case "VisibleString":
            case "WITH":
            case "AS":
            case "BER":
            case "CER":
            case "DECODE":
            case "DER":
            case "DISTRIBUTION":
                return true;
            default:
                return false;
        }
    }
}
