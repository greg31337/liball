using System;

namespace xml;

internal class XdmAnyUriDatatype
{
    internal XdmAtomicValue ParseXdmAnyUri(string lexicalValue)
    {
        XdmStringParseResult result = TryParseXdmAnyUri(lexicalValue);
        if (!result.Success || result.Value == null)
        {
            string message = result.ErrorMessage == null ? "The lexical value is invalid for xs:anyURI." : result.ErrorMessage;
            throw CreateXdmAnyUriLexicalError(message);
        }

        return result.Value;
    }

    internal XdmStringParseResult TryParseXdmAnyUri(string lexicalValue)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalValue);
        if (!new XdmStringDatatype().IsWellFormedXdmUtf16(normalized))
        {
            return new XdmStringParseResult(false, null, "The lexical value is not valid for xs:anyURI.");
        }

        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = XdmAtomicType.AnyUri;
        value.LexicalValue = lexicalValue;
        value.StringValue = normalized;
        value.CanonicalValue = normalized;
        value.IsImmutable = true;
        return new XdmStringParseResult(true, value, null);
    }

    internal bool IsValidXdmAnyUriValue(string value)
    {
        Guard.NotNull(value, "value");
        XdmStringDatatype strings = new XdmStringDatatype();
        return strings.IsWellFormedXdmUtf16(value) && string.Equals(value, strings.CollapseXdmStringWhitespace(value), StringComparison.Ordinal);
    }

    internal string CanonicalizeXdmAnyUri(string value)
    {
        if (!IsValidXdmAnyUriValue(value))
        {
            throw CreateXdmAnyUriLexicalError("The value is not in the value space of xs:anyURI.");
        }

        return value;
    }

    internal bool EqualXdmAnyUri(XdmAtomicValue left, XdmAtomicValue right)
    {
        string leftValue = ReadXdmAnyUriValue(left);
        string rightValue = ReadXdmAnyUriValue(right);
        return string.Equals(leftValue, rightValue, StringComparison.Ordinal);
    }

    internal string ReadXdmAnyUriValue(XdmAtomicValue value)
    {
        Guard.NotNull(value, "value");
        if (value.Type != XdmAtomicType.AnyUri)
        {
            throw new ArgumentException("The atomic value is not xs:anyURI.", "value");
        }

        string? representation = value.StringValue == null ? value.CanonicalValue : value.StringValue;
        if (representation == null)
        {
            if (value.LexicalValue == null)
            {
                throw new ArgumentException("An xs:anyURI value must provide string storage.", "value");
            }

            XdmAtomicValue parsed = ParseXdmAnyUri(value.LexicalValue);
            if (parsed.StringValue == null)
            {
                throw new InvalidOperationException("The parsed xs:anyURI value did not produce string storage.");
            }

            return parsed.StringValue;
        }

        if (!IsValidXdmAnyUriValue(representation))
        {
            throw new ArgumentException("The xs:anyURI string storage is invalid.", "value");
        }

        return representation;
    }

    internal XPathDynamicException CreateXdmAnyUriLexicalError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.FORG0001;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
