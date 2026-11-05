namespace asn1;

internal class XerPrimitiveCodec
{
    internal AsnResourceLimits Limits;
    internal bool AllowWrapperAttributes;
    internal bool AllowWrapperNamespace;
    internal XerNameResolver Names = new XerNameResolver();
    internal XerXmlCharacters Characters = new XerXmlCharacters();

    internal XerPrimitiveCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal XerPrimitiveResult EncodePrimitive(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        XerNameResult name = this.Names.GetElementName(type);
        if (!name.Success)
        {
            return this.CopyNameFailure(name);
        }

        return this.EncodePrimitiveNamed(type, value, name.Name);
    }

    internal XerPrimitiveResult EncodePrimitiveNamed(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (!this.Characters.IsValidNcName(elementName))
        {
            return this.CreateError(
                "ASNXER0100",
                "The BASIC-XER wrapper name is not a valid XML NCName.",
                false);
        }

        if (!this.HasExpectedRuntimeKind(type.Kind, value))
        {
            return this.CreateError(
                "ASNXER0101",
                "The runtime value does not match the requested BASIC-XER type.",
                false);
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            return this.CreateInnerEmptyEncoding(
                value,
                elementName,
                value.BooleanValue ? "true" : "false");
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            return this.CreateTextEncoding(
                value,
                elementName,
                value.IntegerValue.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            string enumeratedName = this.FindEnumeratedName(type, value);
            if (enumeratedName.Length == 0)
            {
                return this.CreateError(
                    "ASNXER0102",
                    "The ENUMERATED value has no matching schema identifier.",
                    false);
            }

            return this.CreateInnerEmptyEncoding(
                value,
                elementName,
                enumeratedName);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return this.EncodeReal(value, elementName);
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return this.CreateEmptyEncoding(value, elementName);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.EncodeBitString(value, elementName);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.EncodeOctetString(value, elementName);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return this.EncodeObjectIdentifier(type.Kind, value, elementName);
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return this.EncodeValidatedText(
                type.Kind,
                value,
                elementName);
        }

        if (this.IsCharacterStringKind(type.Kind))
        {
            AsnTypeKind contentKind = type.Kind == AsnTypeKind.ObjectDescriptor
                ? AsnTypeKind.GraphicString
                : type.Kind;
            return this.EncodeValidatedText(
                contentKind,
                value,
                elementName);
        }

        if (this.IsTimeKind(type.Kind))
        {
            return this.EncodeTime(type.Kind, value, elementName);
        }

        return this.CreateError(
            "ASNXER0103",
            "The requested type is not a BASIC-XER scalar type.",
            false);
    }

    internal XerPrimitiveResult DecodePrimitive(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        XerNameResult name = this.Names.GetElementName(type);
        if (!name.Success)
        {
            return this.CopyNameFailure(name);
        }

        int valueOffset = offset;
        int valueCount = count;
        bool declaration = count > 0
            && tokens[offset].Kind == XerXmlTokenKind.XmlDeclaration;
        if (declaration)
        {
            XerXmlToken prolog = tokens[offset];
            if (prolog.XmlVersion != "1.0"
                || !string.Equals(
                    prolog.XmlEncoding,
                    "UTF-8",
                    System.StringComparison.OrdinalIgnoreCase)
                || prolog.HasStandalone)
            {
                return this.CreateError(
                    "ASNXER0110",
                    "The BASIC-XER XML declaration must specify version 1.0 and UTF-8 only.",
                    false);
            }

            valueOffset = valueOffset + 1;
            valueCount = valueCount - 1;
        }

        XerPrimitiveResult result = this.DecodePrimitiveNamed(
            type,
            tokens,
            valueOffset,
            valueCount,
            name.Name);
        if (result.Success && declaration)
        {
            result.ConsumedCount = result.ConsumedCount + 1;
        }

        return result;
    }

    internal XerPrimitiveResult DecodePrimitiveNamed(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (tokens == null)
        {
            throw new System.ArgumentNullException("tokens");
        }

        if (!CursorMath.IsRangeValid(offset, count, tokens.Length))
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        XerWrappedValue wrapped = this.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        XerPrimitiveResult result;
        if (type.Kind == AsnTypeKind.Boolean)
        {
            result = this.DecodeBoolean(wrapped);
        }
        else if (type.Kind == AsnTypeKind.Integer)
        {
            result = this.DecodeInteger(wrapped);
        }
        else if (type.Kind == AsnTypeKind.Enumerated)
        {
            result = this.DecodeEnumerated(type, wrapped);
        }
        else if (type.Kind == AsnTypeKind.Real)
        {
            result = this.DecodeReal(wrapped);
        }
        else if (type.Kind == AsnTypeKind.Null)
        {
            result = this.DecodeNull(wrapped);
        }
        else if (type.Kind == AsnTypeKind.BitString)
        {
            result = this.DecodeBitString(type, wrapped);
        }
        else if (type.Kind == AsnTypeKind.OctetString)
        {
            result = this.DecodeOctetString(wrapped);
        }
        else if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            result = this.DecodeObjectIdentifier(type.Kind, wrapped);
        }
        else if (type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            result = this.DecodeValidatedText(
                type.Kind,
                type.Kind == AsnTypeKind.ObjectIdentifierIri
                    ? AsnRuntimeValueKind.ObjectIdentifierIri
                    : AsnRuntimeValueKind.RelativeObjectIdentifierIri,
                wrapped);
        }
        else if (this.IsCharacterStringKind(type.Kind))
        {
            AsnTypeKind contentKind = type.Kind == AsnTypeKind.ObjectDescriptor
                ? AsnTypeKind.GraphicString
                : type.Kind;
            result = this.DecodeValidatedText(
                contentKind,
                AsnRuntimeValueKind.CharacterString,
                wrapped);
        }
        else if (this.IsTimeKind(type.Kind))
        {
            result = this.DecodeTime(type.Kind, wrapped);
        }
        else
        {
            result = this.CreateError(
                "ASNXER0103",
                "The requested type is not a BASIC-XER scalar type.",
                false);
        }

        if (result.Success)
        {
            result.ConsumedCount = wrapped.ConsumedCount;
        }

        return result;
    }

    internal XerWrappedValue ReadWrapper(
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        if (count == 0)
        {
            return new XerWrappedValue { NeedMoreData = true };
        }

        XerXmlToken first = tokens[offset];
        if (first.Kind == XerXmlTokenKind.EmptyElement)
        {
            if (first.Name != elementName
                || (first.NamespaceUri.Length > 0 && !this.AllowWrapperNamespace))
            {
                return this.CreateWrapperError(
                    "The BASIC-XER empty wrapper has the wrong element name.");
            }

            return new XerWrappedValue
            {
                Success = true,
                Empty = true,
                Attributes = first.Attributes,
                NamespaceBindings = first.NamespaceBindings,
                ConsumedCount = 1
            };
        }

        if (first.Kind != XerXmlTokenKind.StartElement
            || first.Name != elementName
            || (first.NamespaceUri.Length > 0 && !this.AllowWrapperNamespace)
            || (!this.AllowWrapperAttributes
                && (this.HasNonNamespaceAttributes(first.Attributes)
                    || !this.AllowWrapperNamespace
                        && first.Attributes.Length > 0)))
        {
            return this.CreateWrapperError(
                "The BASIC-XER scalar requires its expected unqualified wrapper.");
        }

        int depth = 1;
        int index = offset + 1;
        int limit = offset + count;
        while (index < limit && depth > 0)
        {
            XerXmlToken current = tokens[index];
            if (current.Kind == XerXmlTokenKind.StartElement)
            {
                depth = depth + 1;
            }
            else if (current.Kind == XerXmlTokenKind.EndElement)
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        if (depth != 0)
        {
            return new XerWrappedValue { NeedMoreData = true };
        }

        XerXmlToken last = tokens[index - 1];
        if (last.Name != elementName
            || (last.NamespaceUri.Length > 0 && !this.AllowWrapperNamespace))
        {
            return this.CreateWrapperError(
                "The BASIC-XER scalar wrapper is not properly nested.");
        }

        int contentCount = index - offset - 2;
        XerXmlToken[] content = new XerXmlToken[contentCount];
        int contentIndex = 0;
        while (contentIndex < contentCount)
        {
            content[contentIndex] = tokens[offset + 1 + contentIndex];
            contentIndex = contentIndex + 1;
        }

        return new XerWrappedValue
        {
            Success = true,
            Attributes = first.Attributes,
            NamespaceBindings = first.NamespaceBindings,
            Content = content,
            ConsumedCount = index - offset
        };
    }

    internal bool HasNonNamespaceAttributes(XerXmlAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (!attributes[index].NamespaceDeclaration)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal XerPrimitiveResult EncodeReal(
        AsnRuntimeValue value,
        string elementName)
    {
        AsnRealValue? real = value.RealValue;
        if (real == null || !real.IsValid())
        {
            return this.CreateError(
                "ASNXER0104",
                "The REAL runtime value is invalid.",
                false);
        }

        if (real.Kind == AsnRealKind.PositiveInfinity)
        {
            return this.CreateInnerEmptyEncoding(
                value,
                elementName,
                "PLUS-INFINITY");
        }

        if (real.Kind == AsnRealKind.NegativeInfinity)
        {
            return this.CreateInnerEmptyEncoding(
                value,
                elementName,
                "MINUS-INFINITY");
        }

        if (real.Kind == AsnRealKind.NotANumber)
        {
            return this.CreateInnerEmptyEncoding(
                value,
                elementName,
                "NOT-A-NUMBER");
        }

        if (real.Kind == AsnRealKind.Zero)
        {
            return this.CreateTextEncoding(value, elementName, "0");
        }

        if (real.Kind == AsnRealKind.MinusZero)
        {
            return this.CreateTextEncoding(value, elementName, "-0");
        }

        string text;
        if (real.Kind == AsnRealKind.FiniteDecimal)
        {
            text = real.DecimalText;
            if (!this.IsValidRealNumber(text))
            {
                text = this.FormatDecimalReal(real);
            }
        }
        else
        {
            text = this.FormatBinaryReal(real);
        }

        if (!this.IsValidRealNumber(text))
        {
            return this.CreateError(
                "ASNXER0104",
                "The REAL value cannot be represented by a BASIC-XER realnumber.",
                false);
        }

        return this.CreateTextEncoding(value, elementName, text);
    }

    internal XerPrimitiveResult EncodeBitString(
        AsnRuntimeValue value,
        string elementName)
    {
        AsnBitStringValue? bits = value.BitStringValue;
        if (bits == null || !bits.IsValid())
        {
            return this.CreateError(
                "ASNXER0105",
                "The BIT STRING runtime value is invalid.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            bits.BitLength))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER bit string exceeds the text limit.",
                true);
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < bits.BitLength)
        {
            int octet = bits.Bytes[index / 8];
            int mask = 1 << 7 - index % 8;
            text.Append((octet & mask) == 0 ? '0' : '1');
            index = index + 1;
        }

        return this.CreateTextEncoding(
            value,
            elementName,
            text.ToString());
    }

    internal XerPrimitiveResult EncodeOctetString(
        AsnRuntimeValue value,
        string elementName)
    {
        if (value.Octets == null
            || value.Octets.Length > int.MaxValue / 2
            || !new ResourceCounter(this.Limits).AllowsTextCharacters(
                value.Octets.Length * 2))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER octet string exceeds the text limit.",
                true);
        }

        const string digits = "0123456789ABCDEF";
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Octets.Length)
        {
            int octet = value.Octets[index];
            text.Append(digits[octet >> 4]);
            text.Append(digits[octet & 15]);
            index = index + 1;
        }

        return this.CreateTextEncoding(
            value,
            elementName,
            text.ToString());
    }

    internal XerPrimitiveResult EncodeObjectIdentifier(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        string elementName)
    {
        AsnObjectIdentifierValue? identifier = value.ObjectIdentifierValue;
        bool relative = kind == AsnTypeKind.RelativeObjectIdentifier;
        if (identifier == null
            || identifier.Relative != relative
            || !identifier.IsValid())
        {
            return this.CreateError(
                "ASNXER0106",
                "The object identifier runtime value is invalid.",
                false);
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < identifier.Arcs.Length)
        {
            if (index > 0)
            {
                text.Append('.');
            }

            text.Append(identifier.Arcs[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        return this.CreateTextEncoding(
            value,
            elementName,
            text.ToString());
    }

    internal XerPrimitiveResult EncodeValidatedText(
        AsnTypeKind contentKind,
        AsnRuntimeValue value,
        string elementName)
    {
        BerPrimitiveValueResult validation = new BerCharacterStringCodec(
            this.Limits).EncodeText(contentKind, value.TextValue);
        if (!validation.Success)
        {
            return this.CreateError(
                validation.ErrorCode,
                validation.ErrorMessage,
                validation.LimitExceeded);
        }

        return this.CreateTextEncoding(
            value,
            elementName,
            value.TextValue);
    }

    internal XerPrimitiveResult EncodeTime(
        AsnTypeKind kind,
        AsnRuntimeValue value,
        string elementName)
    {
        AsnTimeValue? time = value.TimeValue;
        if (time == null
            || time.Kind != this.GetTimeKind(kind)
            || !new TimeValueValidator().ValidateTime(time).Success)
        {
            return this.CreateError(
                "ASNXER0107",
                "The useful or TIME runtime value is invalid.",
                false);
        }

        return this.CreateTextEncoding(value, elementName, time.Text);
    }

    internal XerPrimitiveResult DecodeBoolean(XerWrappedValue wrapped)
    {
        string name = this.GetSingleEmptyContentName(wrapped);
        if (name != "true" && name != "false")
        {
            return this.CreateLexicalError("BOOLEAN");
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = name == "true"
        });
    }

    internal XerPrimitiveResult DecodeInteger(XerWrappedValue wrapped)
    {
        string text = this.TrimXmlWhitespace(
            this.GetSingleTextContent(wrapped));
        XerIntegerTextResult parsed = this.ParseSignedInteger(text, false);
        if (!parsed.Success)
        {
            return this.CreateLexicalError("INTEGER");
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = parsed.Value
        });
    }

    internal XerPrimitiveResult DecodeEnumerated(
        AsnTypeDescriptor type,
        XerWrappedValue wrapped)
    {
        string name = this.GetSingleEmptyContentName(wrapped);
        AsnValueDescriptor? descriptor = this.FindEnumeratedDescriptor(
            type,
            name);
        if (descriptor == null)
        {
            return this.CreateLexicalError("ENUMERATED");
        }

        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = descriptor.Name
        };
        if (descriptor.Value != null
            && descriptor.Value.HasIntegerValue)
        {
            value.HasIntegerValue = true;
            value.IntegerValue = descriptor.Value.IntegerValue;
        }

        return this.CreateDecodedValue(value);
    }

    internal XerPrimitiveResult DecodeNull(XerWrappedValue wrapped)
    {
        if (!wrapped.Empty
            && wrapped.Content.Length != 0
            && (wrapped.Content.Length != 1
                || wrapped.Content[0].Kind != XerXmlTokenKind.Text
                || !this.IsOnlyXmlWhitespace(
                    wrapped.Content[0].Value)))
        {
            return this.CreateLexicalError("NULL");
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null });
    }

    internal XerPrimitiveResult DecodeReal(XerWrappedValue wrapped)
    {
        string special = this.GetSingleEmptyContentName(wrapped);
        if (special.Length > 0)
        {
            AsnRealKind kind;
            if (special == "PLUS-INFINITY")
            {
                kind = AsnRealKind.PositiveInfinity;
            }
            else if (special == "MINUS-INFINITY")
            {
                kind = AsnRealKind.NegativeInfinity;
            }
            else if (special == "NOT-A-NUMBER")
            {
                kind = AsnRealKind.NotANumber;
            }
            else
            {
                return this.CreateLexicalError("REAL");
            }

            return this.CreateDecodedValue(new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Normalized = true,
                    Kind = kind
                }
            });
        }

        string text = this.TrimXmlWhitespace(
            this.GetSingleTextContent(wrapped));
        if (text == "0" || text == "-0")
        {
            return this.CreateDecodedValue(new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Normalized = true,
                    Kind = text == "0"
                        ? AsnRealKind.Zero
                        : AsnRealKind.MinusZero
                }
            });
        }

        AsnRealValue? real = this.ParseDecimalReal(text);
        if (real == null && this.IsZeroRealNumber(text))
        {
            return this.CreateDecodedValue(new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Normalized = true,
                    Kind = text.StartsWith(
                        "-",
                        System.StringComparison.Ordinal)
                            ? AsnRealKind.MinusZero
                            : AsnRealKind.Zero
                }
            });
        }

        if (real == null)
        {
            return this.CreateLexicalError("REAL");
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = real
        });
    }

    internal XerPrimitiveResult DecodeBitString(
        AsnTypeDescriptor type,
        XerWrappedValue wrapped)
    {
        XerPrimitiveResult named = this.DecodeNamedBitString(type, wrapped);
        if (named.Success || named.ErrorCode.Length > 0)
        {
            return named;
        }

        string text = wrapped.Empty ? "" : this.GetSingleTextContent(wrapped);
        if (!this.HasTextOrEmptyContent(wrapped))
        {
            return this.CreateLexicalError("BIT STRING");
        }

        System.Text.StringBuilder bits = new System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '0' || current == '1')
            {
                bits.Append(current);
            }
            else if (!this.IsXmlWhitespace(current))
            {
                return this.CreateLexicalError("BIT STRING");
            }

            index = index + 1;
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(bits.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER bit string exceeds the text limit.",
                true);
        }

        byte[] bytes = new byte[(bits.Length + 7) / 8];
        index = 0;
        while (index < bits.Length)
        {
            if (bits[index] == '1')
            {
                bytes[index / 8] = (byte)(
                    bytes[index / 8] | 1 << 7 - index % 8);
            }

            index = index + 1;
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = bytes,
                BitLength = bits.Length
            }
        });
    }

    internal XerPrimitiveResult DecodeNamedBitString(
        AsnTypeDescriptor type,
        XerWrappedValue wrapped)
    {
        if (wrapped.Empty || wrapped.Content.Length == 0)
        {
            return new XerPrimitiveResult();
        }

        System.Collections.Generic.List<int> indexes =
            new System.Collections.Generic.List<int>();
        int highest = -1;
        int contentIndex = 0;
        bool sawElement = false;
        while (contentIndex < wrapped.Content.Length)
        {
            XerXmlToken token = wrapped.Content[contentIndex];
            if (token.Kind == XerXmlTokenKind.Text
                && this.IsOnlyXmlWhitespace(token.Value))
            {
                contentIndex = contentIndex + 1;
                continue;
            }

            if (token.Kind != XerXmlTokenKind.EmptyElement)
            {
                return sawElement
                    ? this.CreateLexicalError("named BIT STRING")
                    : new XerPrimitiveResult();
            }

            int bitIndex = this.FindNamedBitIndex(type, token.Name);
            if (bitIndex < 0 || indexes.Contains(bitIndex))
            {
                return this.CreateLexicalError("named BIT STRING");
            }

            indexes.Add(bitIndex);
            if (bitIndex > highest)
            {
                highest = bitIndex;
            }

            sawElement = true;
            contentIndex = contentIndex + 1;
        }

        if (!sawElement)
        {
            return new XerPrimitiveResult();
        }

        int bitLength = highest + 1;
        byte[] bytes = new byte[(bitLength + 7) / 8];
        int index = 0;
        while (index < indexes.Count)
        {
            int bitIndex = indexes[index];
            bytes[bitIndex / 8] = (byte)(bytes[bitIndex / 8]
                | 1 << (7 - bitIndex % 8));
            index = index + 1;
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = bytes,
                BitLength = bitLength
            }
        });
    }

    internal int FindNamedBitIndex(
        AsnTypeDescriptor type,
        string name)
    {
        int result = this.FindNamedBitIndexInList(type.NamedValues, name);
        if (result < 0)
        {
            result = this.FindNamedBitIndexInList(
                type.RootNamedValues,
                name);
        }

        if (result < 0)
        {
            result = this.FindNamedBitIndexInList(
                type.ExtensionNamedValues,
                name);
        }

        return result;
    }

    internal int FindNamedBitIndexInList(
        AsnValueDescriptor[] descriptors,
        string name)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnValueDescriptor descriptor = descriptors[index];
            if (descriptor != null
                && descriptor.Name == name
                && descriptor.Value != null
                && descriptor.Value.HasIntegerValue
                && descriptor.Value.IntegerValue >= System.Numerics.BigInteger.Zero
                && descriptor.Value.IntegerValue
                    <= new System.Numerics.BigInteger(int.MaxValue))
            {
                if (result >= 0)
                {
                    return -1;
                }

                result = (int)descriptor.Value.IntegerValue;
            }

            index = index + 1;
        }

        return result;
    }

    internal XerPrimitiveResult DecodeOctetString(XerWrappedValue wrapped)
    {
        string text = wrapped.Empty ? "" : this.GetSingleTextContent(wrapped);
        if (!this.HasTextOrEmptyContent(wrapped))
        {
            return this.CreateLexicalError("OCTET STRING");
        }

        System.Text.StringBuilder hex = new System.Text.StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (this.GetHexDigit(current) >= 0)
            {
                hex.Append(current);
            }
            else if (!this.IsXmlWhitespace(current))
            {
                return this.CreateLexicalError("OCTET STRING");
            }

            index = index + 1;
        }

        if (hex.Length % 2 != 0)
        {
            return this.CreateLexicalError("OCTET STRING");
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(hex.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER octet string exceeds the text limit.",
                true);
        }

        byte[] octets = new byte[hex.Length / 2];
        index = 0;
        while (index < octets.Length)
        {
            octets[index] = (byte)(
                this.GetHexDigit(hex[index * 2]) * 16
                + this.GetHexDigit(hex[index * 2 + 1]));
            index = index + 1;
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = octets
        });
    }

    internal XerPrimitiveResult DecodeObjectIdentifier(
        AsnTypeKind kind,
        XerWrappedValue wrapped)
    {
        string text = this.TrimXmlWhitespace(
            this.GetSingleTextContent(wrapped));
        bool relative = kind == AsnTypeKind.RelativeObjectIdentifier;
        if (text.Length == 0)
        {
            return this.CreateLexicalError(relative
                ? "RELATIVE-OID"
                : "OBJECT IDENTIFIER");
        }

        string[] components = text.Split('.');
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            components.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The object identifier exceeds the component limit.",
                true);
        }
        System.Numerics.BigInteger[] arcs =
            new System.Numerics.BigInteger[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            XerIntegerTextResult arc = this.ParseOidArc(
                components[index],
                !relative && index == 0);
            if (!arc.Success)
            {
                return this.CreateLexicalError(relative
                    ? "RELATIVE-OID"
                    : "OBJECT IDENTIFIER");
            }

            arcs[index] = arc.Value;
            index = index + 1;
        }

        AsnObjectIdentifierValue identifier = new AsnObjectIdentifierValue
        {
            Relative = relative,
            Arcs = arcs
        };
        if (!identifier.IsValid())
        {
            return this.CreateLexicalError(relative
                ? "RELATIVE-OID"
                : "OBJECT IDENTIFIER");
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = relative
                ? AsnRuntimeValueKind.RelativeObjectIdentifier
                : AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = identifier
        });
    }

    internal XerPrimitiveResult DecodeValidatedText(
        AsnTypeKind contentKind,
        AsnRuntimeValueKind runtimeKind,
        XerWrappedValue wrapped)
    {
        string text = wrapped.Empty ? "" : this.GetSingleTextContent(wrapped);
        if (!this.HasTextOrEmptyContent(wrapped))
        {
            return this.CreateLexicalError("character string");
        }

        BerPrimitiveValueResult validation = new BerCharacterStringCodec(
            this.Limits).EncodeText(contentKind, text);
        if (!validation.Success)
        {
            return this.CreateError(
                validation.ErrorCode,
                validation.ErrorMessage,
                validation.LimitExceeded);
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = runtimeKind,
            TextValue = text
        });
    }

    internal XerPrimitiveResult DecodeTime(
        AsnTypeKind kind,
        XerWrappedValue wrapped)
    {
        string text = this.TrimXmlWhitespace(
            this.GetSingleTextContent(wrapped));
        AsnTimeValue time = new AsnTimeValue
        {
            Kind = this.GetTimeKind(kind),
            Text = text
        };
        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER time value exceeds the text limit.",
                true);
        }

        if (!new TimeValueValidator().ValidateTime(time).Success)
        {
            return this.CreateLexicalError("useful or TIME");
        }

        return this.CreateDecodedValue(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = time
        });
    }

    internal string FindEnumeratedName(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnValueDescriptor? descriptor = value.TextValue.Length > 0
            ? this.FindEnumeratedDescriptor(type, value.TextValue)
            : this.FindEnumeratedNumber(type, value);
        return descriptor == null ? "" : descriptor.Name;
    }

    internal AsnValueDescriptor? FindEnumeratedDescriptor(
        AsnTypeDescriptor type,
        string name)
    {
        AsnValueDescriptor? found = this.FindDescriptorByName(
            type.NamedValues,
            name);
        if (found == null)
        {
            found = this.FindDescriptorByName(type.RootNamedValues, name);
        }

        if (found == null)
        {
            found = this.FindDescriptorByName(
                type.ExtensionNamedValues,
                name);
        }

        return found;
    }

    internal AsnValueDescriptor? FindDescriptorByName(
        AsnValueDescriptor[] descriptors,
        string name)
    {
        int index = 0;
        while (index < descriptors.Length)
        {
            if (descriptors[index].Name == name)
            {
                return descriptors[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnValueDescriptor? FindEnumeratedNumber(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        if (!value.HasIntegerValue)
        {
            return null;
        }

        AsnValueDescriptor? found = this.FindDescriptorByNumber(
            type.NamedValues,
            value.IntegerValue);
        if (found == null)
        {
            found = this.FindDescriptorByNumber(
                type.RootNamedValues,
                value.IntegerValue);
        }

        if (found == null)
        {
            found = this.FindDescriptorByNumber(
                type.ExtensionNamedValues,
                value.IntegerValue);
        }

        return found;
    }

    internal AsnValueDescriptor? FindDescriptorByNumber(
        AsnValueDescriptor[] descriptors,
        System.Numerics.BigInteger number)
    {
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnValue? value = descriptors[index].Value;
            if (value != null
                && value.HasIntegerValue
                && value.IntegerValue == number)
            {
                return descriptors[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal string GetSingleTextContent(XerWrappedValue wrapped)
    {
        if (wrapped.Empty
            || wrapped.Content.Length != 1
            || wrapped.Content[0].Kind != XerXmlTokenKind.Text)
        {
            return "";
        }

        return wrapped.Content[0].Value;
    }

    internal string GetSingleEmptyContentName(XerWrappedValue wrapped)
    {
        if (wrapped.Empty)
        {
            return "";
        }

        string name = "";
        int index = 0;
        while (index < wrapped.Content.Length)
        {
            XerXmlToken token = wrapped.Content[index];
            if (token.Kind == XerXmlTokenKind.Text
                && this.IsOnlyXmlWhitespace(token.Value))
            {
                index = index + 1;
                continue;
            }

            if (name.Length == 0
                && token.Kind == XerXmlTokenKind.EmptyElement
                && token.NamespaceUri.Length == 0
                && token.Attributes.Length == 0)
            {
                name = token.Name;
                index = index + 1;
                continue;
            }

            return "";
        }

        return name;
    }

    internal bool HasTextOrEmptyContent(XerWrappedValue wrapped)
    {
        return wrapped.Empty
            || wrapped.Content.Length == 0
            || wrapped.Content.Length == 1
                && wrapped.Content[0].Kind == XerXmlTokenKind.Text;
    }

    internal string TrimXmlWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && this.IsXmlWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && this.IsXmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return value.Substring(start, end - start);
    }

    internal bool IsOnlyXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!this.IsXmlWhitespace(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XerIntegerTextResult ParseSignedInteger(
        string text,
        bool allowNegativeZero)
    {
        int index = 0;
        bool negative = false;
        if (text.Length > 0 && text[0] == '-')
        {
            negative = true;
            index = 1;
        }

        if (index >= text.Length
            || text[index] == '0' && index + 1 < text.Length)
        {
            return new XerIntegerTextResult();
        }

        int digitCount = 0;
        int digitIndex = index;
        while (digitIndex < text.Length)
        {
            char current = text[digitIndex];
            if (current < '0' || current > '9')
            {
                return new XerIntegerTextResult();
            }

            digitCount = digitCount + 1;
            digitIndex = digitIndex + 1;
        }

        if (!this.AllowsDecimalIntegerDigits(digitCount))
        {
            return new XerIntegerTextResult();
        }

        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        while (index < text.Length)
        {
            value = value * 10 + text[index] - '0';
            index = index + 1;
        }

        if (digitCount == 0
            || negative && value == System.Numerics.BigInteger.Zero
                && !allowNegativeZero
            || !new ResourceCounter(this.Limits).AllowsIntegerBits(
                this.GetBigIntegerBitLength(value)))
        {
            return new XerIntegerTextResult();
        }

        return new XerIntegerTextResult
        {
            Success = true,
            Value = negative ? -value : value
        };
    }

    internal bool AllowsDecimalIntegerDigits(int digitCount)
    {
        if (this.Limits.Unlimited)
        {
            return true;
        }

        long maximumDigits = ((long)this.Limits.MaximumIntegerBits * 30103L
            + 99999L) / 100000L;
        return digitCount <= maximumDigits;
    }

    internal XerIntegerTextResult ParseOidArc(
        string text,
        bool firstAbsolute)
    {
        if (firstAbsolute)
        {
            if (text == "itu-t" || text == "ccitt")
            {
                return new XerIntegerTextResult
                {
                    Success = true,
                    Value = System.Numerics.BigInteger.Zero
                };
            }

            if (text == "iso")
            {
                return new XerIntegerTextResult
                {
                    Success = true,
                    Value = System.Numerics.BigInteger.One
                };
            }

            if (text == "joint-iso-itu-t"
                || text == "joint-iso-ccitt")
            {
                return new XerIntegerTextResult
                {
                    Success = true,
                    Value = new System.Numerics.BigInteger(2)
                };
            }
        }

        int open = text.IndexOf('(');
        if (open > 0
            && text.EndsWith(")", System.StringComparison.Ordinal)
            && this.Characters.IsValidNcName(text.Substring(0, open)))
        {
            text = text.Substring(open + 1, text.Length - open - 2);
        }

        return this.ParseSignedInteger(text, false);
    }

    internal bool IsValidRealNumber(string text)
    {
        int index = 0;
        if (text.Length > 0 && text[0] == '-')
        {
            index = 1;
        }

        int integerStart = index;
        while (index < text.Length
            && text[index] >= '0' && text[index] <= '9')
        {
            index = index + 1;
        }

        int integerLength = index - integerStart;
        if (integerLength == 0)
        {
            return false;
        }

        int fractionStart = -1;
        if (index < text.Length && text[index] == '.')
        {
            index = index + 1;
            fractionStart = index;
            while (index < text.Length
                && text[index] >= '0' && text[index] <= '9')
            {
                index = index + 1;
            }
        }

        if (text[integerStart] == '0' && integerLength > 1)
        {
            return false;
        }

        if (text[integerStart] == '0' && fractionStart >= 0)
        {
            bool nonzero = false;
            int fractionIndex = fractionStart;
            while (fractionIndex < index)
            {
                if (text[fractionIndex] != '0')
                {
                    nonzero = true;
                }

                fractionIndex = fractionIndex + 1;
            }

            if (!nonzero)
            {
                return false;
            }
        }

        if (index < text.Length
            && (text[index] == 'e' || text[index] == 'E'))
        {
            index = index + 1;
            if (index < text.Length
                && (text[index] == '+' || text[index] == '-'))
            {
                index = index + 1;
            }

            int exponentStart = index;
            while (index < text.Length
                && text[index] >= '0' && text[index] <= '9')
            {
                index = index + 1;
            }

            if (index == exponentStart)
            {
                return false;
            }
        }

        return index == text.Length;
    }

    internal AsnRealValue? ParseDecimalReal(string text)
    {
        if (!this.IsValidRealNumber(text))
        {
            return null;
        }

        bool negative = text[0] == '-';
        int start = negative ? 1 : 0;
        int exponentMarker = text.IndexOf('E', start);
        if (exponentMarker < 0)
        {
            exponentMarker = text.IndexOf('e', start);
        }

        int mantissaEnd = exponentMarker < 0
            ? text.Length
            : exponentMarker;
        int point = text.IndexOf('.', start, mantissaEnd - start);
        int fractionDigits = point < 0 ? 0 : mantissaEnd - point - 1;
        System.Text.StringBuilder digits = new System.Text.StringBuilder();
        int index = start;
        while (index < mantissaEnd)
        {
            if (text[index] != '.')
            {
                digits.Append(text[index]);
            }

            index = index + 1;
        }

        XerIntegerTextResult coefficient = this.ParseSignedInteger(
            (negative ? "-" : "") + digits.ToString(),
            true);
        if (!coefficient.Success
            || coefficient.Value == System.Numerics.BigInteger.Zero)
        {
            return null;
        }

        System.Numerics.BigInteger exponent =
            new System.Numerics.BigInteger(-fractionDigits);
        if (exponentMarker >= 0)
        {
            string exponentText = text.Substring(exponentMarker + 1);
            if (exponentText.StartsWith(
                "+",
                System.StringComparison.Ordinal))
            {
                exponentText = exponentText.Substring(1);
            }

            XerIntegerTextResult parsedExponent = this.ParseSignedInteger(
                exponentText,
                true);
            if (!parsedExponent.Success)
            {
                return null;
            }

            exponent = exponent + parsedExponent.Value;
        }

        while (coefficient.Value % 10 == System.Numerics.BigInteger.Zero)
        {
            coefficient.Value = coefficient.Value / 10;
            exponent = exponent + System.Numerics.BigInteger.One;
        }

        return new AsnRealValue
        {
            Normalized = true,
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = text,
            DecimalCoefficient = coefficient.Value,
            DecimalExponent = exponent
        };
    }

    internal bool IsZeroRealNumber(string text)
    {
        if (!this.IsValidRealNumber(text))
        {
            return false;
        }

        int index = text[0] == '-' ? 1 : 0;
        while (index < text.Length
            && text[index] != 'E' && text[index] != 'e')
        {
            if (text[index] >= '1' && text[index] <= '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string FormatDecimalReal(AsnRealValue real)
    {
        if (real.DecimalCoefficient == System.Numerics.BigInteger.Zero)
        {
            return "";
        }

        return real.DecimalCoefficient.ToString(
            System.Globalization.CultureInfo.InvariantCulture)
            + "E"
            + real.DecimalExponent.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    internal string FormatBinaryReal(AsnRealValue real)
    {
        if (!real.HasBinaryValue
            || real.Mantissa == System.Numerics.BigInteger.Zero)
        {
            return "";
        }

        int multiplier = real.Radix == 2
            ? 1
            : real.Radix == 8
                ? 3
                : real.Radix == 16 ? 4 : 0;
        if (multiplier == 0)
        {
            return "";
        }

        System.Numerics.BigInteger binaryExponent =
            real.Exponent * multiplier;
        if (binaryExponent > new System.Numerics.BigInteger(int.MaxValue)
            || binaryExponent < new System.Numerics.BigInteger(int.MinValue))
        {
            return "";
        }

        int exponent = (int)binaryExponent;
        if (exponent >= 0)
        {
            int mantissaBits = this.GetBigIntegerBitLength(real.Mantissa);
            if (exponent > int.MaxValue - mantissaBits
                || !new ResourceCounter(this.Limits).AllowsIntegerBits(
                    exponent + mantissaBits))
            {
                return "";
            }

            return (real.Mantissa << exponent).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        if (exponent == int.MinValue)
        {
            return "";
        }

        int places = -exponent;
        if (places > (int.MaxValue - this.GetBigIntegerBitLength(
                real.Mantissa)) / 3
            || !new ResourceCounter(this.Limits).AllowsTextCharacters(places + 3)
            || !new ResourceCounter(this.Limits).AllowsIntegerBits(
                places * 3 + this.GetBigIntegerBitLength(real.Mantissa)))
        {
            return "";
        }

        bool negative = real.Mantissa < System.Numerics.BigInteger.Zero;
        System.Numerics.BigInteger numerator =
            System.Numerics.BigInteger.Abs(real.Mantissa)
            * System.Numerics.BigInteger.Pow(
                new System.Numerics.BigInteger(5),
                places);
        string digits = numerator.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string result;
        if (digits.Length <= places)
        {
            result = "0."
                + new string('0', places - digits.Length)
                + digits;
        }
        else
        {
            result = digits.Substring(0, digits.Length - places)
                + "." + digits.Substring(digits.Length - places);
        }

        int trim = result.Length - 1;
        while (trim >= 0 && result[trim] == '0')
        {
            trim = trim - 1;
        }

        if (trim >= 0 && result[trim] == '.')
        {
            trim = trim - 1;
        }

        result = result.Substring(0, trim + 1);
        return negative ? "-" + result : result;
    }

    internal int GetBigIntegerBitLength(System.Numerics.BigInteger value)
    {
        byte[] bytes = System.Numerics.BigInteger.Abs(value).ToByteArray();
        int length = bytes.Length;
        while (length > 1 && bytes[length - 1] == 0)
        {
            length = length - 1;
        }

        if (length > int.MaxValue / 8)
        {
            return int.MaxValue;
        }

        int bits = (length - 1) * 8;
        int highest = bytes[length - 1];
        while (highest > 0)
        {
            bits = bits + 1;
            highest = highest >> 1;
        }

        return bits == 0 ? 1 : bits;
    }

    internal XerPrimitiveResult CreateTextEncoding(
        AsnRuntimeValue value,
        string elementName,
        string text)
    {
        if (!this.Characters.IsValidText(text))
        {
            return this.CreateError(
                "ASNXER0108",
                "The BASIC-XER text value contains a forbidden XML character.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASIC-XER text value exceeds its character limit.",
                true);
        }

        if (text.Length == 0)
        {
            return this.CreateEmptyEncoding(value, elementName);
        }

        return new XerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens =
            [
                this.CreateNamedToken(
                    XerXmlTokenKind.StartElement,
                    elementName),
                new XerXmlToken
                {
                    Kind = XerXmlTokenKind.Text,
                    Value = text
                },
                this.CreateNamedToken(
                    XerXmlTokenKind.EndElement,
                    elementName)
            ]
        };
    }

    internal XerPrimitiveResult CreateEmptyEncoding(
        AsnRuntimeValue value,
        string elementName)
    {
        return new XerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens =
            [
                this.CreateNamedToken(
                    XerXmlTokenKind.EmptyElement,
                    elementName)
            ]
        };
    }

    internal XerPrimitiveResult CreateInnerEmptyEncoding(
        AsnRuntimeValue value,
        string elementName,
        string innerName)
    {
        return new XerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens =
            [
                this.CreateNamedToken(
                    XerXmlTokenKind.StartElement,
                    elementName),
                this.CreateNamedToken(
                    XerXmlTokenKind.EmptyElement,
                    innerName),
                this.CreateNamedToken(
                    XerXmlTokenKind.EndElement,
                    elementName)
            ]
        };
    }

    internal XerXmlToken CreateNamedToken(
        XerXmlTokenKind kind,
        string name)
    {
        return new XerXmlToken
        {
            Kind = kind,
            Name = name,
            LocalName = name
        };
    }

    internal bool HasExpectedRuntimeKind(
        AsnTypeKind kind,
        AsnRuntimeValue value)
    {
        if (kind == AsnTypeKind.Boolean)
        {
            return value.Kind == AsnRuntimeValueKind.Boolean
                && value.HasBooleanValue;
        }

        if (kind == AsnTypeKind.Integer)
        {
            return value.Kind == AsnRuntimeValueKind.Integer
                && value.HasIntegerValue;
        }

        if (kind == AsnTypeKind.Enumerated)
        {
            return value.Kind == AsnRuntimeValueKind.Enumerated;
        }

        if (kind == AsnTypeKind.Real)
        {
            return value.Kind == AsnRuntimeValueKind.Real
                && value.RealValue != null;
        }

        if (kind == AsnTypeKind.Null)
        {
            return value.Kind == AsnRuntimeValueKind.Null;
        }

        if (kind == AsnTypeKind.BitString)
        {
            return value.Kind == AsnRuntimeValueKind.BitString
                && value.BitStringValue != null;
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return value.Kind == AsnRuntimeValueKind.OctetString
                && value.Octets != null;
        }

        if (kind == AsnTypeKind.ObjectIdentifier)
        {
            return value.Kind == AsnRuntimeValueKind.ObjectIdentifier
                && value.ObjectIdentifierValue != null;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return value.Kind == AsnRuntimeValueKind.RelativeObjectIdentifier
                && value.ObjectIdentifierValue != null;
        }

        if (kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return value.Kind == AsnRuntimeValueKind.ObjectIdentifierIri;
        }

        if (kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return value.Kind
                == AsnRuntimeValueKind.RelativeObjectIdentifierIri;
        }

        if (this.IsCharacterStringKind(kind))
        {
            return value.Kind == AsnRuntimeValueKind.CharacterString;
        }

        return this.IsTimeKind(kind)
            && value.Kind == AsnRuntimeValueKind.Time
            && value.TimeValue != null;
    }

    internal bool IsCharacterStringKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString;
    }

    internal bool IsTimeKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal AsnTimeKind GetTimeKind(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.UtcTime)
        {
            return AsnTimeKind.UtcTime;
        }

        if (kind == AsnTypeKind.GeneralizedTime)
        {
            return AsnTimeKind.GeneralizedTime;
        }

        if (kind == AsnTypeKind.Date)
        {
            return AsnTimeKind.Date;
        }

        if (kind == AsnTypeKind.TimeOfDay)
        {
            return AsnTimeKind.TimeOfDay;
        }

        if (kind == AsnTypeKind.DateTime)
        {
            return AsnTimeKind.DateTime;
        }

        return kind == AsnTypeKind.Duration
            ? AsnTimeKind.Duration
            : AsnTimeKind.Time;
    }

    internal bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t'
            || value == '\r' || value == '\n';
    }

    internal int GetHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value >= 'a' && value <= 'f'
            ? value - 'a' + 10
            : -1;
    }

    internal XerPrimitiveResult CreateDecodedValue(AsnRuntimeValue value)
    {
        return new XerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value
        };
    }

    internal XerPrimitiveResult CreateLexicalError(string typeName)
    {
        return this.CreateError(
            "ASNXER0108",
            "The BASIC-XER " + typeName + " lexical value is invalid.",
            false);
    }

    internal XerPrimitiveResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new XerPrimitiveResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal XerPrimitiveResult CopyNameFailure(XerNameResult name)
    {
        return this.CreateError(
            name.ErrorCode,
            name.ErrorMessage,
            false);
    }

    internal XerPrimitiveResult CopyWrapperFailure(XerWrappedValue wrapped)
    {
        if (wrapped.NeedMoreData)
        {
            return new XerPrimitiveResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            };
        }

        return this.CreateError(
            wrapped.ErrorCode,
            wrapped.ErrorMessage,
            false);
    }

    internal XerWrappedValue CreateWrapperError(string message)
    {
        return new XerWrappedValue
        {
            ErrorCode = "ASNXER0109",
            ErrorMessage = message
        };
    }
}
