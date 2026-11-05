namespace asn1;

internal class ValueParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;

    internal ValueParser(AsnToken[] tokens, DiagnosticBag diagnostics, bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal ValueParseResult ParseValueSyntax(AsnType? expectedType)
    {
        int start = this.Position;
        AsnType? effectiveType = this.GetEffectiveType(expectedType);
        if (this.PeekToken() == null)
        {
            this.AddDiagnostic(63, "Expected an ASN.1 value.", null);
            return this.CreateResult(false, null, start);
        }

        ValueParseResult informationValue = this.ParseValueFromObject(start);
        if (informationValue.Success)
        {
            return informationValue;
        }

        ValueParseResult parameterizedValue = this.ParseParameterizedValueReference(start);
        if (parameterizedValue.Success)
        {
            return parameterizedValue;
        }

        if (this.IsKind(this.PeekToken(), AsnTokenKind.XmlValue))
        {
            return this.ParseXmlValue(start, expectedType);
        }

        if (effectiveType == null || effectiveType.Kind == AsnTypeKind.Defined)
        {
            return this.ParseUntypedValue(start);
        }

        switch (effectiveType.Kind)
        {
            case AsnTypeKind.Boolean:
                return this.ParseBooleanValue(start);
            case AsnTypeKind.Integer:
                return this.ParseIntegerValue(start);
            case AsnTypeKind.Enumerated:
                return this.ParseIdentifierValue(start, AsnValueKind.Enumerated);
            case AsnTypeKind.Real:
                return this.ParseRealValue(start);
            case AsnTypeKind.BitString:
                return this.ParseBitStringValue(start);
            case AsnTypeKind.OctetString:
                return this.ParseOctetStringValue(start);
            case AsnTypeKind.Null:
                return this.ParseNullValue(start);
            case AsnTypeKind.ObjectIdentifier:
                return this.ParseObjectIdentifierValue(start, false);
            case AsnTypeKind.RelativeObjectIdentifier:
                return this.ParseObjectIdentifierValue(start, true);
            case AsnTypeKind.ObjectIdentifierIri:
                return this.ParseIriValue(start, AsnValueKind.ObjectIdentifierIri);
            case AsnTypeKind.RelativeObjectIdentifierIri:
                return this.ParseIriValue(start, AsnValueKind.RelativeObjectIdentifierIri);
            case AsnTypeKind.Sequence:
                return this.ParseStructuredValue(start, effectiveType, AsnValueKind.Sequence);
            case AsnTypeKind.Set:
                return this.ParseStructuredValue(start, effectiveType, AsnValueKind.Set);
            case AsnTypeKind.SequenceOf:
                return this.ParseCollectionValue(start, effectiveType, AsnValueKind.SequenceOf);
            case AsnTypeKind.SetOf:
                return this.ParseCollectionValue(start, effectiveType, AsnValueKind.SetOf);
            case AsnTypeKind.Choice:
                return this.ParseChoiceValue(start, effectiveType);
            case AsnTypeKind.OpenType:
                return this.ParseOpenTypeValue(start);
            case AsnTypeKind.External:
                return this.ParseStructuredValue(start, effectiveType, AsnValueKind.External);
            case AsnTypeKind.EmbeddedPdv:
                return this.ParseStructuredValue(start, effectiveType, AsnValueKind.EmbeddedPdv);
            case AsnTypeKind.InstanceOf:
                return this.ParseStructuredValue(start, effectiveType, AsnValueKind.InstanceOf);
            case AsnTypeKind.UtcTime:
            case AsnTypeKind.GeneralizedTime:
            case AsnTypeKind.Time:
            case AsnTypeKind.Date:
            case AsnTypeKind.TimeOfDay:
            case AsnTypeKind.DateTime:
            case AsnTypeKind.Duration:
                return this.ParseTimeValue(start, effectiveType.Kind);
            default:
                if (this.IsCharacterStringKind(effectiveType.Kind))
                {
                    return this.ParseCharacterStringValue(start);
                }

                return this.ParseUntypedValue(start);
        }
    }

    internal ValueParseResult ParseXmlValue(int start, AsnType? expectedType)
    {
        AsnToken token = this.ReadRequiredToken();
        XmlElementSyntax? element = new XmlMarkupParser(
            token.Text,
            token,
            this.Diagnostics,
            this.ReportDiagnostics).ParseDocument();
        if (element == null)
        {
            return this.CreateResult(false, null, start);
        }

        ParsedValueSyntax? syntax = this.MapXmlElementValue(element, expectedType, token);
        if (syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        syntax.SourceTokens = [token];
        syntax.Span = token.Span;
        return this.CreateResult(true, syntax, start);
    }

    internal ParsedValueSyntax? MapXmlElementValue(
        XmlElementSyntax element,
        AsnType? expectedType,
        AsnToken token)
    {
        AsnType? type = this.GetEffectiveType(expectedType);
        if (type == null || type.Kind == AsnTypeKind.Defined)
        {
            AsnTypeKind inferredKind = this.GetXmlTypeKind(element.LocalName);
            if (inferredKind == AsnTypeKind.Unknown)
            {
                return this.CreateXmlSyntax(new AsnValue
                {
                    Kind = AsnValueKind.Xml,
                    TextValue = element.Text
                }, element, token);
            }

            type = new AsnType { Kind = inferredKind };
        }

        switch (type.Kind)
        {
            case AsnTypeKind.Boolean:
                return this.MapXmlBooleanValue(element, token);
            case AsnTypeKind.Integer:
                return this.MapXmlIntegerValue(element, token);
            case AsnTypeKind.Enumerated:
                return this.MapXmlIdentifierValue(element, token, AsnValueKind.Enumerated);
            case AsnTypeKind.Real:
                return this.MapXmlRealValue(element, token);
            case AsnTypeKind.BitString:
                return this.MapXmlBitStringValue(element, token);
            case AsnTypeKind.OctetString:
                return this.MapXmlOctetStringValue(element, token);
            case AsnTypeKind.Null:
                return this.MapXmlNullValue(element, token);
            case AsnTypeKind.ObjectIdentifier:
                return this.MapXmlObjectIdentifierValue(element, token, false);
            case AsnTypeKind.RelativeObjectIdentifier:
                return this.MapXmlObjectIdentifierValue(element, token, true);
            case AsnTypeKind.ObjectIdentifierIri:
                return this.CreateXmlTextValue(element, token, AsnValueKind.ObjectIdentifierIri);
            case AsnTypeKind.RelativeObjectIdentifierIri:
                return this.CreateXmlTextValue(element, token, AsnValueKind.RelativeObjectIdentifierIri);
            case AsnTypeKind.Sequence:
                return this.MapXmlStructuredValue(element, type, token, AsnValueKind.Sequence);
            case AsnTypeKind.Set:
                return this.MapXmlStructuredValue(element, type, token, AsnValueKind.Set);
            case AsnTypeKind.SequenceOf:
                return this.MapXmlCollectionValue(element, type, token, AsnValueKind.SequenceOf);
            case AsnTypeKind.SetOf:
                return this.MapXmlCollectionValue(element, type, token, AsnValueKind.SetOf);
            case AsnTypeKind.Choice:
                return this.MapXmlChoiceValue(element, type, token);
            case AsnTypeKind.OpenType:
                return this.MapXmlOpenTypeValue(element, token);
            case AsnTypeKind.External:
                return this.MapXmlStructuredValue(element, type, token, AsnValueKind.External);
            case AsnTypeKind.EmbeddedPdv:
                return this.MapXmlStructuredValue(element, type, token, AsnValueKind.EmbeddedPdv);
            case AsnTypeKind.InstanceOf:
                return this.MapXmlStructuredValue(element, type, token, AsnValueKind.InstanceOf);
            case AsnTypeKind.UtcTime:
            case AsnTypeKind.GeneralizedTime:
            case AsnTypeKind.Time:
            case AsnTypeKind.Date:
            case AsnTypeKind.TimeOfDay:
            case AsnTypeKind.DateTime:
            case AsnTypeKind.Duration:
                return this.MapXmlTimeValue(element, type.Kind, token);
            default:
                if (this.IsCharacterStringKind(type.Kind))
                {
                    return this.CreateXmlTextValue(element, token, AsnValueKind.CharacterString);
                }

                return this.CreateXmlSyntax(new AsnValue
                {
                    Kind = AsnValueKind.Xml,
                    TextValue = element.Text
                }, element, token);
        }
    }

    internal ParsedValueSyntax? MapXmlBooleanValue(XmlElementSyntax element, AsnToken token)
    {
        string text = element.Text.Trim();
        if (element.LocalName == "true" && element.Empty)
        {
            text = "TRUE";
        }
        else if (element.LocalName == "false" && element.Empty)
        {
            text = "FALSE";
        }
        else if (text == "true" || text == "1" || text == "TRUE")
        {
            text = "TRUE";
        }
        else if (text == "false" || text == "0" || text == "FALSE")
        {
            text = "FALSE";
        }
        else
        {
            this.AddDiagnostic(101, "The XML BOOLEAN value is not true or false.", token);
            return null;
        }

        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.Boolean,
            TextValue = text
        }, element, token);
    }

    internal ParsedValueSyntax? MapXmlIntegerValue(XmlElementSyntax element, AsnToken token)
    {
        SignedIntegerParseResult number = this.ParseXmlIntegerText(element.Text.Trim());
        if (!number.Success)
        {
            return this.MapXmlIdentifierValue(element, token, AsnValueKind.Defined);
        }

        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = number.Value
        }, element, token);
    }

    internal ParsedValueSyntax? MapXmlIdentifierValue(
        XmlElementSyntax element,
        AsnToken token,
        AsnValueKind kind)
    {
        string text = element.Text.Trim();
        if (text.Length == 0 && element.Empty)
        {
            text = element.LocalName;
        }

        if (text.Length == 0)
        {
            this.AddDiagnostic(102, "The XML identifier value is empty.", token);
            return null;
        }

        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = kind,
            TextValue = text
        }, element, token);
    }

    internal ParsedValueSyntax? MapXmlRealValue(XmlElementSyntax element, AsnToken token)
    {
        string text = element.Text.Trim();
        AsnRealValue real = new AsnRealValue();
        if (text == "INF" || text == "+INF")
        {
            real.Kind = AsnRealKind.PositiveInfinity;
        }
        else if (text == "-INF")
        {
            real.Kind = AsnRealKind.NegativeInfinity;
        }
        else if (text == "NaN")
        {
            real.Kind = AsnRealKind.NotANumber;
        }
        else if (text == "0")
        {
            real.Kind = AsnRealKind.Zero;
        }
        else if (text == "-0")
        {
            real.Kind = AsnRealKind.MinusZero;
        }
        else if (text.Length > 0)
        {
            real.Kind = AsnRealKind.FiniteDecimal;
            real.HasDecimalValue = true;
            real.DecimalText = text;
        }
        else
        {
            this.AddDiagnostic(103, "The XML REAL value is empty.", token);
            return null;
        }

        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.Real,
            RealValue = real
        }, element, token);
    }

    internal ParsedValueSyntax? MapXmlBitStringValue(XmlElementSyntax element, AsnToken token)
    {
        System.Collections.Generic.List<AsnNamedValue> names =
            new System.Collections.Generic.List<AsnNamedValue>();
        if (element.Children.Length > 0)
        {
            int childIndex = 0;
            while (childIndex < element.Children.Length)
            {
                names.Add(new AsnNamedValue { Name = element.Children[childIndex].LocalName });
                childIndex = childIndex + 1;
            }

            return this.CreateXmlSyntax(new AsnValue
            {
                Kind = AsnValueKind.BitString,
                NamedValues = names.ToArray()
            }, element, token);
        }

        string digits = this.RemoveXmlWhitespace(element.Text);
        AsnBitStringValue? bits = this.DecodeXmlBinaryDigits(digits);
        if (bits == null)
        {
            this.AddDiagnostic(104, "The XML BIT STRING value must contain binary digits.", token);
            return null;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.BitString,
            TextValue = digits
        }, element, token);
        syntax.BitStringValue = bits;
        return syntax;
    }

    internal ParsedValueSyntax? MapXmlOctetStringValue(XmlElementSyntax element, AsnToken token)
    {
        string digits = this.RemoveXmlWhitespace(element.Text);
        if (digits.Length % 2 != 0)
        {
            this.AddDiagnostic(105, "The XML OCTET STRING value must contain pairs of hexadecimal digits.", token);
            return null;
        }

        byte[] octets = new byte[digits.Length / 2];
        int index = 0;
        while (index < digits.Length)
        {
            int high = this.GetXmlHexValue(digits[index]);
            int low = this.GetXmlHexValue(digits[index + 1]);
            if (high < 0 || low < 0)
            {
                this.AddDiagnostic(105, "The XML OCTET STRING value contains a non-hexadecimal digit.", token);
                return null;
            }

            octets[index / 2] = (byte)(high * 16 + low);
            index = index + 2;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.OctetString,
            TextValue = digits
        }, element, token);
        syntax.Octets = octets;
        return syntax;
    }

    internal ParsedValueSyntax? MapXmlNullValue(XmlElementSyntax element, AsnToken token)
    {
        if (element.Children.Length > 0 || element.Text.Trim().Length > 0)
        {
            this.AddDiagnostic(106, "The XML NULL value must be empty.", token);
            return null;
        }

        return this.CreateXmlSyntax(new AsnValue { Kind = AsnValueKind.Null }, element, token);
    }

    internal ParsedValueSyntax? MapXmlObjectIdentifierValue(
        XmlElementSyntax element,
        AsnToken token,
        bool relative)
    {
        string text = element.Text.Trim();
        System.Collections.Generic.List<System.Numerics.BigInteger> arcs =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        System.Collections.Generic.List<AssignedOidComponentSyntax> components =
            new System.Collections.Generic.List<AssignedOidComponentSyntax>();
        int index = 0;
        while (index < text.Length)
        {
            bool sawDot = false;
            while (index < text.Length && (char.IsWhiteSpace(text[index]) || text[index] == '.'))
            {
                if (text[index] == '.')
                {
                    sawDot = true;
                }

                index = index + 1;
            }

            if (index == text.Length)
            {
                if (sawDot)
                {
                    this.AddDiagnostic(109, "The XML object identifier contains an invalid arc.", token);
                    return null;
                }

                break;
            }

            int start = index;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                index = index + 1;
            }

            if (index == start)
            {
                this.AddDiagnostic(109, "The XML object identifier contains an invalid arc.", token);
                return null;
            }

            System.Numerics.BigInteger number = System.Numerics.BigInteger.Parse(
                text.Substring(start, index - start),
                System.Globalization.CultureInfo.InvariantCulture);
            arcs.Add(number);
            components.Add(new AssignedOidComponentSyntax { HasNumber = true, Number = number });
        }

        if (components.Count == 0)
        {
            this.AddDiagnostic(109, "The XML object identifier cannot be empty.", token);
            return null;
        }

        AsnValueKind kind = relative
            ? AsnValueKind.RelativeObjectIdentifier
            : AsnValueKind.ObjectIdentifier;
        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = kind,
            TextValue = text
        }, element, token);
        syntax.ObjectIdentifierValue = new AsnObjectIdentifierValue
        {
            Relative = relative,
            Arcs = arcs.ToArray()
        };
        syntax.ObjectIdentifierComponents = components.ToArray();
        return syntax;
    }

    internal ParsedValueSyntax CreateXmlTextValue(
        XmlElementSyntax element,
        AsnToken token,
        AsnValueKind kind)
    {
        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = kind,
            TextValue = element.Text
        }, element, token);
    }

    internal ParsedValueSyntax? MapXmlStructuredValue(
        XmlElementSyntax element,
        AsnType type,
        AsnToken token,
        AsnValueKind kind)
    {
        System.Collections.Generic.List<AsnNamedValue> named =
            new System.Collections.Generic.List<AsnNamedValue>();
        System.Collections.Generic.List<ParsedValueSyntax> items =
            new System.Collections.Generic.List<ParsedValueSyntax>();
        int index = 0;
        while (index < element.Children.Length)
        {
            XmlElementSyntax child = element.Children[index];
            AsnType? componentType = this.FindComponentType(type, child.LocalName);
            ParsedValueSyntax? value = this.MapXmlElementValue(child, componentType, token);
            if (value == null)
            {
                return null;
            }

            named.Add(new AsnNamedValue { Name = child.LocalName, Value = value.Value });
            items.Add(value);
            index = index + 1;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = kind,
            NamedValues = named.ToArray()
        }, element, token);
        syntax.Items = items.ToArray();
        return syntax;
    }

    internal ParsedValueSyntax? MapXmlCollectionValue(
        XmlElementSyntax element,
        AsnType type,
        AsnToken token,
        AsnValueKind kind)
    {
        ParsedValueSyntax[] items = new ParsedValueSyntax[element.Children.Length];
        int index = 0;
        while (index < element.Children.Length)
        {
            ParsedValueSyntax? value = this.MapXmlElementValue(
                element.Children[index],
                type.ElementType,
                token);
            if (value == null)
            {
                return null;
            }

            items[index] = value;
            index = index + 1;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue { Kind = kind }, element, token);
        syntax.Items = items;
        return syntax;
    }

    internal ParsedValueSyntax? MapXmlChoiceValue(
        XmlElementSyntax element,
        AsnType type,
        AsnToken token)
    {
        XmlElementSyntax? selected = this.FindXmlChoiceElement(element, type);
        if (selected == null)
        {
            this.AddDiagnostic(107, "The XML CHOICE value requires one selected alternative.", token);
            return null;
        }

        AsnType? selectedType = this.FindComponentType(type, selected.LocalName);
        ParsedValueSyntax? value = this.MapXmlElementValue(selected, selectedType, token);
        if (value == null)
        {
            return null;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.Choice,
            NamedValues = [new AsnNamedValue { Name = selected.LocalName, Value = value.Value }]
        }, element, token);
        syntax.InnerValue = value;
        return syntax;
    }

    internal XmlElementSyntax? FindXmlChoiceElement(XmlElementSyntax element, AsnType type)
    {
        if (this.FindComponentType(type, element.LocalName) != null)
        {
            return element;
        }

        return element.Children.Length == 1 ? element.Children[0] : null;
    }

    internal ParsedValueSyntax? MapXmlOpenTypeValue(XmlElementSyntax element, AsnToken token)
    {
        if (element.Children.Length != 1)
        {
            this.AddDiagnostic(108, "The XML open type value requires one typed child element.", token);
            return null;
        }

        XmlElementSyntax child = element.Children[0];
        AsnTypeKind kind = this.GetXmlTypeKind(child.LocalName);
        AsnType governingType = new AsnType
        {
            Kind = kind == AsnTypeKind.Unknown ? AsnTypeKind.Defined : kind,
            Name = kind == AsnTypeKind.Unknown ? child.LocalName : ""
        };
        ParsedValueSyntax? value = this.MapXmlElementValue(child, governingType, token);
        if (value == null)
        {
            return null;
        }

        ParsedValueSyntax syntax = this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.OpenType,
            TextValue = child.LocalName
        }, element, token);
        syntax.GoverningTypeSyntax = new ParsedTypeSyntax
        {
            Type = governingType,
            SourceTypeName = child.LocalName
        };
        syntax.InnerValue = value;
        return syntax;
    }

    internal ParsedValueSyntax MapXmlTimeValue(
        XmlElementSyntax element,
        AsnTypeKind kind,
        AsnToken token)
    {
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = this.GetTimeKind(kind),
            Text = element.Text
        };
        this.PopulateTimeMetadata(time);
        return this.CreateXmlSyntax(new AsnValue
        {
            Kind = AsnValueKind.Time,
            TimeValue = time,
            TextValue = element.Text
        }, element, token);
    }

    internal ParsedValueSyntax CreateXmlSyntax(
        AsnValue value,
        XmlElementSyntax element,
        AsnToken token)
    {
        return new ParsedValueSyntax
        {
            Value = value,
            XmlElement = element,
            SourceTokens = [token],
            Span = token.Span
        };
    }

    internal SignedIntegerParseResult ParseXmlIntegerText(string text)
    {
        bool negative = text.Length > 0 && text[0] == '-';
        int index = negative ? 1 : 0;
        if (index == text.Length)
        {
            return new SignedIntegerParseResult();
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return new SignedIntegerParseResult();
            }

            value = value * 10 + (text[index] - '0');
            index = index + 1;
        }

        return new SignedIntegerParseResult
        {
            Success = true,
            Value = negative ? -value : value
        };
    }

    internal AsnBitStringValue? DecodeXmlBinaryDigits(string digits)
    {
        byte[] bytes = new byte[(digits.Length + 7) / 8];
        int index = 0;
        while (index < digits.Length)
        {
            if (digits[index] != '0' && digits[index] != '1')
            {
                return null;
            }

            if (digits[index] == '1')
            {
                int byteIndex = index / 8;
                int shift = 7 - index % 8;
                bytes[byteIndex] = (byte)(bytes[byteIndex] | (byte)(1 << shift));
            }

            index = index + 1;
        }

        return new AsnBitStringValue { Bytes = bytes, BitLength = digits.Length };
    }

    internal string RemoveXmlWhitespace(string text)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            if (!char.IsWhiteSpace(text[index]))
            {
                result.Append(text[index]);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal int GetXmlHexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value >= 'a' && value <= 'f' ? value - 'a' + 10 : -1;
    }

    internal AsnTypeKind GetXmlTypeKind(string name)
    {
        switch (name)
        {
            case "BOOLEAN":
                return AsnTypeKind.Boolean;
            case "INTEGER":
                return AsnTypeKind.Integer;
            case "ENUMERATED":
                return AsnTypeKind.Enumerated;
            case "REAL":
                return AsnTypeKind.Real;
            case "BIT-STRING":
            case "BIT_STRING":
                return AsnTypeKind.BitString;
            case "OCTET-STRING":
            case "OCTET_STRING":
                return AsnTypeKind.OctetString;
            case "NULL":
                return AsnTypeKind.Null;
            case "OBJECT-IDENTIFIER":
            case "OBJECT_IDENTIFIER":
                return AsnTypeKind.ObjectIdentifier;
            case "RELATIVE-OID":
                return AsnTypeKind.RelativeObjectIdentifier;
            case "OID-IRI":
                return AsnTypeKind.ObjectIdentifierIri;
            case "RELATIVE-OID-IRI":
                return AsnTypeKind.RelativeObjectIdentifierIri;
            case "SEQUENCE":
                return AsnTypeKind.Sequence;
            case "SET":
                return AsnTypeKind.Set;
            case "SEQUENCE-OF":
                return AsnTypeKind.SequenceOf;
            case "SET-OF":
                return AsnTypeKind.SetOf;
            case "EXTERNAL":
                return AsnTypeKind.External;
            case "EMBEDDED-PDV":
                return AsnTypeKind.EmbeddedPdv;
            case "INSTANCE-OF":
                return AsnTypeKind.InstanceOf;
            case "UTF8String":
                return AsnTypeKind.Utf8String;
            case "GeneralizedTime":
                return AsnTypeKind.GeneralizedTime;
            case "UTCTime":
                return AsnTypeKind.UtcTime;
            case "TIME":
                return AsnTypeKind.Time;
            case "DATE":
                return AsnTypeKind.Date;
            case "TIME-OF-DAY":
                return AsnTypeKind.TimeOfDay;
            case "DATE-TIME":
                return AsnTypeKind.DateTime;
            case "DURATION":
                return AsnTypeKind.Duration;
            default:
                return AsnTypeKind.Unknown;
        }
    }

    internal ValueParseResult ParseUntypedValue(int start)
    {
        AsnToken current = this.ReadRequiredTokenWithoutAdvance();
        if (current.Text == "TRUE" || current.Text == "FALSE")
        {
            return this.ParseBooleanValue(start);
        }

        if (current.Text == "NULL")
        {
            return this.ParseNullValue(start);
        }

        if (current.Text == "PLUS-INFINITY"
            || current.Text == "MINUS-INFINITY"
            || current.Text == "NOT-A-NUMBER"
            || current.Kind == AsnTokenKind.RealLiteral)
        {
            return this.ParseRealValue(start);
        }

        if (current.Kind == AsnTokenKind.IntegerLiteral
            || (current.Kind == AsnTokenKind.Minus
                && this.IsNumericToken(this.PeekTokenAt(1))))
        {
            return this.PeekTokenAt(current.Kind == AsnTokenKind.Minus ? 1 : 0)?.Kind
                    == AsnTokenKind.RealLiteral
                ? this.ParseRealValue(start)
                : this.ParseIntegerValue(start);
        }

        if (current.Kind == AsnTokenKind.BinaryStringLiteral
            || current.Kind == AsnTokenKind.HexStringLiteral)
        {
            return this.ParseBitStringValue(start);
        }

        if (current.Kind == AsnTokenKind.CharacterStringLiteral
            || current.Kind == AsnTokenKind.TupleLiteral
            || current.Kind == AsnTokenKind.QuadrupleLiteral)
        {
            return this.ParseCharacterStringValue(start);
        }

        if (current.Kind == AsnTokenKind.LeftBrace)
        {
            AsnType collection = new AsnType
            {
                Kind = AsnTypeKind.SequenceOf
            };
            return this.ParseCollectionValue(start, collection, AsnValueKind.SequenceOf);
        }

        if (this.IsValueName(current)
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Colon))
        {
            return this.ParseChoiceValue(start, new AsnType { Kind = AsnTypeKind.Choice });
        }

        if (this.IsTypeStart(current))
        {
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            TypeParseResult typeResult = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
            if (typeResult.Success
                && typeResult.ConsumedTokenCount < remaining.Length
                && remaining[typeResult.ConsumedTokenCount].Kind == AsnTokenKind.Colon)
            {
                return this.ParseOpenTypeValue(start);
            }
        }

        return this.ParseDefinedValue(start);
    }

    internal ValueParseResult ParseValueFromObject(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        InformationFromObjectsParseResult parsed = new InformationFromObjectsParser(
            remaining,
            this.Diagnostics,
            false).ParseInformationFromObjectsSyntax(AsnAssignmentKind.Value);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        string text = this.JoinInformationTokens(parsed.Syntax.SourceTokens);
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.Defined,
            TextValue = text
        });
        syntax.InformationFromObjects = parsed.Syntax;
        return this.CreateResult(true, syntax, start);
    }

    internal string JoinInformationTokens(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal ValueParseResult ParseParameterizedValueReference(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedReferenceParseResult parsed = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            false).ParseParameterizedReferenceSyntax(AsnParameterKind.Value, null);
        if (!parsed.Success || parsed.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.Defined,
            TextValue = this.JoinInformationTokens(parsed.Syntax.SourceTokens)
        });
        syntax.ParameterizedReference = parsed.Syntax;
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseBooleanValue(int start)
    {
        AsnToken? token = this.PeekToken();
        if (token != null && (token.Text == "TRUE" || token.Text == "FALSE"))
        {
            this.Position = this.Position + 1;
            return this.CreateValueResult(start, new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = token.Text
            });
        }

        return this.ParseDefinedValue(start);
    }

    internal ValueParseResult ParseIntegerValue(int start)
    {
        int saved = this.Position;
        SignedIntegerParseResult number = this.ParseSignedInteger();
        if (number.Success
            && !this.IsKind(this.PeekToken(), AsnTokenKind.Plus)
            && !this.IsKind(this.PeekToken(), AsnTokenKind.Minus)
            && !this.IsKind(this.PeekToken(), AsnTokenKind.Asterisk)
            && !this.IsKind(this.PeekToken(), AsnTokenKind.Slash))
        {
            return this.CreateValueResult(start, new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = number.Value
            });
        }

        this.Position = saved;
        IntegerExpressionSyntaxParser expression =
            new IntegerExpressionSyntaxParser(this.Tokens, this.Position);
        if (expression.ParseExpression() && expression.SawOperator)
        {
            this.Position = expression.Position;
            ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
            {
                Kind = AsnValueKind.Integer,
                TextValue = this.JoinTokenText(start, this.Position)
            });
            syntax.IntegerExpression = true;
            return this.CreateResult(true, syntax, start);
        }

        this.Position = saved;
        return this.ParseDefinedValue(start);
    }

    internal string JoinTokenText(int start, int end)
    {
        string text = "";
        int index = start;
        while (index < end)
        {
            text = text + this.Tokens[index].Text;
            index = index + 1;
        }

        return text;
    }

    internal ValueParseResult ParseIdentifierValue(int start, AsnValueKind kind)
    {
        AsnToken? token = this.PeekToken();
        if (!this.IsValueName(token))
        {
            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        return this.CreateValueResult(start, new AsnValue
        {
            Kind = kind,
            TextValue = token == null ? "" : token.Text
        });
    }

    internal ValueParseResult ParseRealValue(int start)
    {
        AsnToken? current = this.PeekToken();
        if (current != null && current.Kind == AsnTokenKind.LeftBrace)
        {
            return this.ParseBinaryRealValue(start);
        }

        if (current != null
            && (current.Text == "PLUS-INFINITY"
                || current.Text == "MINUS-INFINITY"
                || current.Text == "NOT-A-NUMBER"))
        {
            this.Position = this.Position + 1;
            AsnRealKind kind = current.Text == "PLUS-INFINITY"
                ? AsnRealKind.PositiveInfinity
                : current.Text == "MINUS-INFINITY"
                    ? AsnRealKind.NegativeInfinity
                    : AsnRealKind.NotANumber;
            return this.CreateRealResult(start, new AsnRealValue { Kind = kind });
        }

        bool negative = this.IsKind(current, AsnTokenKind.Minus);
        if (negative)
        {
            this.Position = this.Position + 1;
            current = this.PeekToken();
        }

        if (!this.IsNumericToken(current))
        {
            if (negative)
            {
                this.AddDiagnostic(65, "Expected a numeric REAL value after '-'.", current);
                return this.CreateResult(false, null, start);
            }

            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        string text = (negative ? "-" : "") + (current == null ? "" : current.Text);
        bool zero = current != null && current.Kind == AsnTokenKind.IntegerLiteral && current.Text == "0";
        AsnRealValue real = zero
            ? new AsnRealValue { Kind = negative ? AsnRealKind.MinusZero : AsnRealKind.Zero }
            : new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = text
            };
        return this.CreateRealResult(start, real);
    }

    internal ValueParseResult ParseBinaryRealValue(int start)
    {
        AsnToken left = this.ReadRequiredToken();
        System.Numerics.BigInteger mantissa = System.Numerics.BigInteger.Zero;
        System.Numerics.BigInteger exponent = System.Numerics.BigInteger.Zero;
        int radix = 0;
        bool hasMantissa = false;
        bool hasBase = false;
        bool hasExponent = false;
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            AsnToken? name = this.PeekToken();
            if (name == null)
            {
                this.AddDiagnostic(74, "A named bit list is missing its closing brace.", null);
                return this.CreateResult(false, null, start);
            }

            if (!this.IsValueName(name))
            {
                this.AddDiagnostic(66, "Expected mantissa, base, or exponent in a REAL value.", name);
                return this.CreateResult(false, null, start);
            }

            this.Position = this.Position + 1;
            SignedIntegerParseResult number = this.ParseSignedInteger();
            if (!number.Success)
            {
                this.AddDiagnostic(67, "A binary REAL component requires an integer.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }

            if (name != null && name.Text == "mantissa")
            {
                mantissa = number.Value;
                hasMantissa = true;
            }
            else if (name != null && name.Text == "base")
            {
                if (number.Value < new System.Numerics.BigInteger(int.MinValue)
                    || number.Value > new System.Numerics.BigInteger(int.MaxValue))
                {
                    this.AddDiagnostic(68, "A binary REAL base must be 2, 8, or 16.", name);
                    return this.CreateResult(false, null, start);
                }

                radix = (int)number.Value;
                hasBase = true;
            }
            else if (name != null && name.Text == "exponent")
            {
                exponent = number.Value;
                hasExponent = true;
            }
            else
            {
                this.AddDiagnostic(66, "Expected mantissa, base, or exponent in a REAL value.", name);
                return this.CreateResult(false, null, start);
            }

            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(73, "A named bit list cannot have a trailing comma.", this.PeekToken());
                    return this.CreateResult(false, null, start);
                }

                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(69, "Expected ',' or '}' in a binary REAL value.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(70, "A binary REAL value is missing its closing brace.", left);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        if (!hasMantissa || !hasBase || !hasExponent || (radix != 2 && radix != 8 && radix != 16))
        {
            this.AddDiagnostic(71, "A binary REAL value requires mantissa, base 2/8/16, and exponent.", left);
            return this.CreateResult(false, null, start);
        }

        return this.CreateRealResult(start, new AsnRealValue
        {
            Kind = AsnRealKind.FiniteBinary,
            HasBinaryValue = true,
            Mantissa = mantissa,
            Radix = radix,
            Exponent = exponent
        });
    }

    internal ValueParseResult ParseBitStringValue(int start)
    {
        AsnToken? current = this.PeekToken();
        if (current != null
            && (current.Kind == AsnTokenKind.BinaryStringLiteral
                || current.Kind == AsnTokenKind.HexStringLiteral))
        {
            this.Position = this.Position + 1;
            AsnBitStringValue bits = this.DecodeBitString(current);
            ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
            {
                Kind = AsnValueKind.BitString,
                TextValue = current.Text
            });
            syntax.BitStringValue = bits;
            return this.CreateResult(true, syntax, start);
        }

        if (this.IsKind(current, AsnTokenKind.LeftBrace))
        {
            return this.ParseNamedBitListValue(start);
        }

        return this.ParseDefinedValue(start);
    }

    internal ValueParseResult ParseNamedBitListValue(int start)
    {
        this.Position = this.Position + 1;
        System.Collections.Generic.List<AsnNamedValue> names =
            new System.Collections.Generic.List<AsnNamedValue>();
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            AsnToken? name = this.PeekToken();
            if (!this.IsValueName(name))
            {
                this.AddDiagnostic(72, "Expected a named bit identifier.", name);
                return this.CreateResult(false, null, start);
            }

            names.Add(new AsnNamedValue { Name = name == null ? "" : name.Text });
            this.Position = this.Position + 1;
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(73, "Expected ',' or '}' in a named bit list.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(74, "A named bit list is missing its closing brace.", null);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        return this.CreateValueResult(start, new AsnValue
        {
            Kind = AsnValueKind.BitString,
            NamedValues = names.ToArray()
        });
    }

    internal ValueParseResult ParseOctetStringValue(int start)
    {
        AsnToken? token = this.PeekToken();
        if (token == null
            || (token.Kind != AsnTokenKind.BinaryStringLiteral
                && token.Kind != AsnTokenKind.HexStringLiteral))
        {
            return this.ParseDefinedValue(start);
        }

        AsnBitStringValue bits = this.DecodeBitString(token);
        if (bits.BitLength % 8 != 0)
        {
            this.AddDiagnostic(75, "An OCTET STRING literal must contain a whole number of octets.", token);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.OctetString,
            TextValue = token.Text
        });
        syntax.Octets = bits.Bytes;
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseNullValue(int start)
    {
        AsnToken? token = this.PeekToken();
        if (token != null && token.Text == "NULL")
        {
            this.Position = this.Position + 1;
            return this.CreateValueResult(start, new AsnValue { Kind = AsnValueKind.Null });
        }

        return this.ParseDefinedValue(start);
    }

    internal ValueParseResult ParseCharacterStringValue(int start)
    {
        AsnToken? token = this.PeekToken();
        if (token != null && token.Kind == AsnTokenKind.CharacterStringLiteral)
        {
            this.Position = this.Position + 1;
            return this.CreateValueResult(start, new AsnValue
            {
                Kind = AsnValueKind.CharacterString,
                TextValue = this.DecodeQuotedText(token.Text)
            });
        }

        if (token != null
            && (token.Kind == AsnTokenKind.TupleLiteral
                || token.Kind == AsnTokenKind.QuadrupleLiteral))
        {
            this.Position = this.Position + 1;
            return this.CreateValueResult(start, new AsnValue
            {
                Kind = AsnValueKind.CharacterString,
                TextValue = token.Text
            });
        }

        if (this.IsKind(token, AsnTokenKind.LeftBrace))
        {
            AsnType collection = new AsnType
            {
                Kind = AsnTypeKind.SequenceOf,
                ElementType = new AsnType { Kind = AsnTypeKind.CharacterString }
            };
            return this.ParseCollectionValue(start, collection, AsnValueKind.CharacterString);
        }

        return this.ParseDefinedValue(start);
    }

    internal ValueParseResult ParseObjectIdentifierValue(int start, bool relative)
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.ParseDefinedValue(start);
        }

        AsnToken left = this.ReadRequiredToken();
        System.Collections.Generic.List<AssignedOidComponentSyntax> components =
            new System.Collections.Generic.List<AssignedOidComponentSyntax>();
        System.Collections.Generic.List<System.Numerics.BigInteger> arcs =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            if (this.PeekToken() == null)
            {
                this.AddDiagnostic(78, "An object identifier value is missing its closing brace.", left);
                return this.CreateResult(false, null, start);
            }

            AssignedOidComponentSyntax? component = this.ParseObjectIdentifierComponent();
            if (component == null)
            {
                this.AddDiagnostic(76, "Expected an object identifier component.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }

            components.Add(component);
            if (component.HasNumber)
            {
                arcs.Add(component.Number);
            }

            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.AddDiagnostic(77, "Object identifier components are not comma-separated.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }

            if (this.PeekToken() == null)
            {
                this.AddDiagnostic(78, "An object identifier value is missing its closing brace.", left);
                return this.CreateResult(false, null, start);
            }
        }

        AsnToken right = this.ReadRequiredToken();
        if (components.Count == 0)
        {
            this.AddDiagnostic(79, "An object identifier value cannot be empty.", right);
            return this.CreateResult(false, null, start);
        }

        AsnValueKind kind = relative
            ? AsnValueKind.RelativeObjectIdentifier
            : AsnValueKind.ObjectIdentifier;
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue { Kind = kind });
        syntax.ObjectIdentifierComponents = components.ToArray();
        syntax.ObjectIdentifierValue = new AsnObjectIdentifierValue
        {
            Relative = relative,
            Arcs = arcs.ToArray()
        };
        return this.CreateResult(true, syntax, start);
    }

    internal AssignedOidComponentSyntax? ParseObjectIdentifierComponent()
    {
        AsnToken? first = this.PeekToken();
        if (first == null)
        {
            return null;
        }

        if (first.Kind == AsnTokenKind.IntegerLiteral)
        {
            this.Position = this.Position + 1;
            return new AssignedOidComponentSyntax
            {
                HasNumber = true,
                Number = System.Numerics.BigInteger.Parse(
                    first.Text,
                    System.Globalization.CultureInfo.InvariantCulture),
                Span = first.Span
            };
        }

        if (this.IsExternalDefinedValueStart())
        {
            AsnToken module = this.ReadRequiredToken();
            this.Position = this.Position + 1;
            AsnToken value = this.ReadRequiredToken();
            return new AssignedOidComponentSyntax
            {
                HasDefinedValue = true,
                DefinedValueModule = module.Text,
                DefinedValue = value.Text,
                Span = this.CreateSpan(module, value)
            };
        }

        if (!this.IsValueName(first))
        {
            return null;
        }

        this.Position = this.Position + 1;
        AssignedOidComponentSyntax component = new AssignedOidComponentSyntax
        {
            HasName = true,
            Name = first.Text,
            HasDefinedValue = true,
            DefinedValue = first.Text,
            Span = first.Span
        };
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
        {
            return component;
        }

        component.HasDefinedValue = false;
        component.DefinedValue = "";
        this.Position = this.Position + 1;
        AsnToken? number = this.PeekToken();
        if (number != null && number.Kind == AsnTokenKind.IntegerLiteral)
        {
            component.HasNumber = true;
            component.Number = System.Numerics.BigInteger.Parse(
                number.Text,
                System.Globalization.CultureInfo.InvariantCulture);
            this.Position = this.Position + 1;
        }
        else
        {
            string defined = this.ParseDefinedValueText();
            if (defined.Length == 0)
            {
                return null;
            }

            component.HasNumberDefinedValue = true;
            component.NumberDefinedValue = defined;
        }

        AsnToken? right = this.PeekToken();
        if (!this.IsKind(right, AsnTokenKind.RightParenthesis))
        {
            return null;
        }

        this.Position = this.Position + 1;
        component.Span = this.CreateSpan(first, right);
        return component;
    }

    internal ValueParseResult ParseIriValue(int start, AsnValueKind kind)
    {
        AsnToken? token = this.PeekToken();
        if (token == null || token.Kind != AsnTokenKind.CharacterStringLiteral)
        {
            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        return this.CreateValueResult(start, new AsnValue
        {
            Kind = kind,
            TextValue = this.DecodeQuotedText(token.Text)
        });
    }

    internal ValueParseResult ParseStructuredValue(
        int start,
        AsnType expectedType,
        AsnValueKind kind)
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.ParseDefinedValue(start);
        }

        AsnToken left = this.ReadRequiredToken();
        System.Collections.Generic.List<AsnNamedValue> named =
            new System.Collections.Generic.List<AsnNamedValue>();
        System.Collections.Generic.List<ParsedValueSyntax> items =
            new System.Collections.Generic.List<ParsedValueSyntax>();
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            AsnToken? name = this.PeekToken();
            if (name == null)
            {
                this.AddDiagnostic(82, "A structured value is missing its closing brace.", left);
                return this.CreateResult(false, null, start);
            }

            if (!this.IsValueName(name))
            {
                this.AddDiagnostic(80, "Expected a named component value.", name);
                return this.CreateResult(false, null, start);
            }

            this.Position = this.Position + 1;
            AsnType? componentType = this.FindComponentType(expectedType, name == null ? "" : name.Text);
            ValueParseResult value = this.ParseValueSyntax(componentType);
            if (!value.Success || value.Syntax == null)
            {
                return this.CreateResult(false, null, start);
            }

            named.Add(new AsnNamedValue
            {
                Name = name == null ? "" : name.Text,
                Value = value.Syntax.Value
            });
            items.Add(value.Syntax);
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(81, "A structured value cannot have a trailing comma.", this.PeekToken());
                    return this.CreateResult(false, null, start);
                }

                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(81, "Expected ',' or '}' in a structured value.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(82, "A structured value is missing its closing brace.", left);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = kind,
            NamedValues = named.ToArray()
        });
        syntax.Items = items.ToArray();
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseCollectionValue(
        int start,
        AsnType expectedType,
        AsnValueKind kind)
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return this.ParseDefinedValue(start);
        }

        AsnToken left = this.ReadRequiredToken();
        System.Collections.Generic.List<ParsedValueSyntax> items =
            new System.Collections.Generic.List<ParsedValueSyntax>();
        System.Collections.Generic.List<AsnNamedValue> named =
            new System.Collections.Generic.List<AsnNamedValue>();
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            if (this.PeekToken() == null)
            {
                this.AddDiagnostic(84, "A collection value is missing its closing brace.", left);
                return this.CreateResult(false, null, start);
            }

            AsnToken? possibleName = this.PeekToken();
            bool namedItem = this.IsValueName(possibleName)
                && this.PeekTokenAt(1) != null
                && !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Comma)
                && !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.RightBrace)
                && !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
                && !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Colon);
            if (namedItem)
            {
                this.Position = this.Position + 1;
            }

            ValueParseResult value = this.ParseValueSyntax(expectedType.ElementType);
            if (!value.Success || value.Syntax == null)
            {
                return this.CreateResult(false, null, start);
            }

            items.Add(value.Syntax);
            if (namedItem)
            {
                named.Add(new AsnNamedValue
                {
                    Name = possibleName == null ? "" : possibleName.Text,
                    Value = value.Syntax.Value
                });
            }

            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(83, "A collection value cannot have a trailing comma.", this.PeekToken());
                    return this.CreateResult(false, null, start);
                }

                continue;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(83, "Expected ',' or '}' in a collection value.", this.PeekToken());
                return this.CreateResult(false, null, start);
            }
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(84, "A collection value is missing its closing brace.", left);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = kind,
            NamedValues = named.ToArray()
        });
        syntax.Items = items.ToArray();
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseChoiceValue(int start, AsnType expectedType)
    {
        AsnToken? name = this.PeekToken();
        if (!this.IsValueName(name))
        {
            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Colon))
        {
            this.Position = start;
            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        AsnType? alternativeType = this.FindComponentType(expectedType, name == null ? "" : name.Text);
        ValueParseResult selected = this.ParseValueSyntax(alternativeType);
        if (!selected.Success || selected.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.Choice,
            NamedValues = [new AsnNamedValue
            {
                Name = name == null ? "" : name.Text,
                Value = selected.Syntax.Value
            }]
        });
        syntax.InnerValue = selected.Syntax;
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseOpenTypeValue(int start)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        TypeParseResult typeResult = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
        if (!typeResult.Success || typeResult.Syntax == null || typeResult.ConsumedTokenCount == 0)
        {
            this.AddDiagnostic(85, "An open type value requires a governing type.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + typeResult.ConsumedTokenCount;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Colon))
        {
            this.AddDiagnostic(86, "Expected ':' after the open type governor.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        ValueParseResult inner = this.ParseValueSyntax(typeResult.Syntax.Type);
        if (!inner.Success || inner.Syntax == null)
        {
            return this.CreateResult(false, null, start);
        }

        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.OpenType,
            TextValue = typeResult.Syntax.SourceTypeName
        });
        syntax.GoverningTypeSyntax = typeResult.Syntax;
        syntax.InnerValue = inner.Syntax;
        return this.CreateResult(true, syntax, start);
    }

    internal ValueParseResult ParseTimeValue(int start, AsnTypeKind typeKind)
    {
        AsnToken? token = this.PeekToken();
        if (token == null || token.Kind != AsnTokenKind.CharacterStringLiteral)
        {
            return this.ParseDefinedValue(start);
        }

        this.Position = this.Position + 1;
        string text = this.DecodeQuotedText(token.Text);
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = this.GetTimeKind(typeKind),
            Text = text
        };
        this.PopulateTimeMetadata(time);
        return this.CreateValueResult(start, new AsnValue
        {
            Kind = AsnValueKind.Time,
            TimeValue = time,
            TextValue = text
        });
    }

    internal ValueParseResult ParseDefinedValue(int start)
    {
        string text = this.ParseDefinedValueText();
        if (text.Length == 0)
        {
            this.AddDiagnostic(87, "Expected a basic or defined ASN.1 value.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        int dot = text.IndexOf('.');
        ParsedValueSyntax syntax = this.CreateSyntax(start, new AsnValue
        {
            Kind = AsnValueKind.Defined,
            TextValue = text
        });
        if (dot >= 0)
        {
            syntax.ReferenceModule = text.Substring(0, dot);
            syntax.ReferenceName = text.Substring(dot + 1);
        }
        else
        {
            syntax.ReferenceName = text;
        }

        return this.CreateResult(true, syntax, start);
    }

    internal SignedIntegerParseResult ParseSignedInteger()
    {
        bool negative = this.IsKind(this.PeekToken(), AsnTokenKind.Minus);
        if (negative)
        {
            this.Position = this.Position + 1;
        }

        AsnToken? token = this.PeekToken();
        if (token == null || token.Kind != AsnTokenKind.IntegerLiteral)
        {
            return new SignedIntegerParseResult();
        }

        this.Position = this.Position + 1;
        System.Numerics.BigInteger number = System.Numerics.BigInteger.Parse(
            token.Text,
            System.Globalization.CultureInfo.InvariantCulture);
        return new SignedIntegerParseResult
        {
            Success = true,
            Value = negative ? -number : number
        };
    }

    internal AsnBitStringValue DecodeBitString(AsnToken token)
    {
        bool hexadecimal = token.Kind == AsnTokenKind.HexStringLiteral;
        System.Text.StringBuilder digits = new System.Text.StringBuilder();
        int index = 1;
        while (index < token.Text.Length - 2)
        {
            char value = token.Text[index];
            if (!char.IsWhiteSpace(value))
            {
                digits.Append(value);
            }

            index = index + 1;
        }

        int bitLength = hexadecimal ? digits.Length * 4 : digits.Length;
        byte[] bytes = new byte[(bitLength + 7) / 8];
        index = 0;
        while (index < digits.Length)
        {
            if (hexadecimal)
            {
                int nibble = this.GetHexValue(digits[index]);
                int bitOffset = index * 4;
                int byteIndex = bitOffset / 8;
                int shift = 4 - (bitOffset % 8);
                bytes[byteIndex] = (byte)(bytes[byteIndex] | (byte)(nibble << shift));
            }
            else if (digits[index] == '1')
            {
                int byteIndex = index / 8;
                int shift = 7 - index % 8;
                bytes[byteIndex] = (byte)(bytes[byteIndex] | (byte)(1 << shift));
            }

            index = index + 1;
        }

        return new AsnBitStringValue
        {
            Bytes = bytes,
            BitLength = bitLength
        };
    }

    internal int GetHexValue(char value)
    {
        return value >= '0' && value <= '9' ? value - '0' : value - 'A' + 10;
    }

    internal string DecodeQuotedText(string text)
    {
        return text.Length < 2
            ? ""
            : text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }

    internal AsnType? FindComponentType(AsnType type, string name)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            if (type.Components[index].Name == name)
            {
                return type.Components[index].Type;
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnType? GetEffectiveType(AsnType? type)
    {
        AsnType? current = type;
        while (current != null
            && (current.Kind == AsnTypeKind.Tagged
                || current.Kind == AsnTypeKind.Prefixed
                || current.Kind == AsnTypeKind.Selection)
            && current.ElementType != null)
        {
            current = current.ElementType;
        }

        return current;
    }

    internal bool IsCharacterStringKind(AsnTypeKind kind)
    {
        int value = (int)kind;
        return kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || (value >= (int)AsnTypeKind.Utf8String && value <= (int)AsnTypeKind.BmpString);
    }

    internal AsnTimeKind GetTimeKind(AsnTypeKind kind)
    {
        switch (kind)
        {
            case AsnTypeKind.UtcTime:
                return AsnTimeKind.UtcTime;
            case AsnTypeKind.GeneralizedTime:
                return AsnTimeKind.GeneralizedTime;
            case AsnTypeKind.Date:
                return AsnTimeKind.Date;
            case AsnTypeKind.TimeOfDay:
                return AsnTimeKind.TimeOfDay;
            case AsnTypeKind.DateTime:
                return AsnTimeKind.DateTime;
            case AsnTypeKind.Duration:
                return AsnTimeKind.Duration;
            default:
                return AsnTimeKind.Time;
        }
    }

    internal void PopulateTimeMetadata(AsnTimeValue time)
    {
        int separator = time.Text.IndexOf('.');
        if (separator < 0)
        {
            separator = time.Text.IndexOf(',');
        }

        if (separator >= 0)
        {
            int index = separator + 1;
            int digits = 0;
            while (index < time.Text.Length
                && time.Text[index] >= '0'
                && time.Text[index] <= '9')
            {
                digits = digits + 1;
                index = index + 1;
            }

            time.HasFractionalDigits = digits > 0;
            time.FractionalDigits = digits;
        }

        if (time.Kind == AsnTimeKind.Duration || time.Text.EndsWith("Z", System.StringComparison.Ordinal))
        {
            if (time.Text.EndsWith("Z", System.StringComparison.Ordinal))
            {
                time.HasUtcOffsetMinutes = true;
            }

            return;
        }

        int plus = time.Text.LastIndexOf('+');
        int minus = time.Text.LastIndexOf('-');
        int zone = plus > minus ? plus : minus;
        if (zone <= 7 || zone >= time.Text.Length - 2)
        {
            return;
        }

        string zoneText = time.Text.Substring(zone + 1).Replace(":", "");
        if (zoneText.Length != 4)
        {
            return;
        }

        int digitIndex = 0;
        while (digitIndex < zoneText.Length)
        {
            if (zoneText[digitIndex] < '0' || zoneText[digitIndex] > '9')
            {
                return;
            }

            digitIndex = digitIndex + 1;
        }

        int hours = int.Parse(zoneText.Substring(0, 2), System.Globalization.CultureInfo.InvariantCulture);
        int minutes = int.Parse(zoneText.Substring(2, 2), System.Globalization.CultureInfo.InvariantCulture);
        int offset = hours * 60 + minutes;
        time.HasUtcOffsetMinutes = true;
        time.UtcOffsetMinutes = time.Text[zone] == '-' ? -offset : offset;
    }

    internal string ParseDefinedValueText()
    {
        AsnToken? first = this.PeekToken();
        if (this.IsValueName(first))
        {
            this.Position = this.Position + 1;
            return first == null ? "" : first.Text;
        }

        if (!this.IsTypeReferenceToken(first)
            || !this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
            || !this.IsValueName(this.PeekTokenAt(2)))
        {
            return "";
        }

        string text = (first == null ? "" : first.Text)
            + "."
            + (this.PeekTokenAt(2) == null ? "" : this.PeekTokenAt(2)?.Text);
        this.Position = this.Position + 3;
        return text;
    }

    internal bool IsExternalDefinedValueStart()
    {
        return this.IsTypeReferenceToken(this.PeekToken())
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
            && this.IsValueName(this.PeekTokenAt(2));
    }

    internal bool IsValueName(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.Identifier
                || token.Kind == AsnTokenKind.ValueReference
                || token.Kind == AsnTokenKind.ObjectReference);
    }

    internal bool IsTypeReferenceToken(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.TypeReference
                || token.Kind == AsnTokenKind.ModuleReference
                || token.Kind == AsnTokenKind.ObjectClassReference);
    }

    internal bool IsTypeStart(AsnToken token)
    {
        return this.IsTypeReferenceToken(token)
            || token.Kind == AsnTokenKind.LeftBracket
            || token.Text == "BOOLEAN"
            || token.Text == "INTEGER"
            || token.Text == "REAL"
            || token.Text == "BIT"
            || token.Text == "OCTET"
            || token.Text == "NULL"
            || token.Text == "OBJECT"
            || token.Text == "SEQUENCE"
            || token.Text == "SET"
            || token.Text == "CHOICE";
    }

    internal bool IsNumericToken(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.IntegerLiteral || token.Kind == AsnTokenKind.RealLiteral);
    }

    internal ValueParseResult CreateRealResult(int start, AsnRealValue real)
    {
        return this.CreateValueResult(start, new AsnValue
        {
            Kind = AsnValueKind.Real,
            RealValue = real
        });
    }

    internal ValueParseResult CreateValueResult(int start, AsnValue value)
    {
        return this.CreateResult(true, this.CreateSyntax(start, value), start);
    }

    internal ParsedValueSyntax CreateSyntax(int start, AsnValue value)
    {
        return new ParsedValueSyntax
        {
            Value = value,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
    }

    internal ValueParseResult CreateResult(bool success, ParsedValueSyntax? syntax, int start)
    {
        return new ValueParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal AsnToken[] CopyTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.Tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
    }

    internal AsnToken ReadRequiredTokenWithoutAdvance()
    {
        return this.Tokens[this.Position];
    }

    internal AsnToken? PeekToken()
    {
        return this.PeekTokenAt(0);
    }

    internal AsnToken? PeekTokenAt(int distance)
    {
        int index = this.Position + distance;
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal AsnToken? TokenAt(int index)
    {
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
        };
    }

    internal void AddDiagnostic(int number, string message, AsnToken? token)
    {
        if (!this.ReportDiagnostics)
        {
            return;
        }

        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token == null ? null : token.Span,
            null);
    }
}
