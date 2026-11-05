namespace asn1;

internal class XerValueCodec
{
    internal const string ControlNamespace = "urn:oid:2.1.5.2.0.1";
    internal AsnResourceLimits Limits;
    internal bool Canonical;
    internal bool Extended;
    internal AsnUnknownDataPolicy UnknownDataPolicy =
        AsnUnknownDataPolicy.Preserve;
    internal XerNameResolver Names = new XerNameResolver();
    internal XerXmlCharacters Characters = new XerXmlCharacters();
    internal System.Collections.Generic.List<string> EncodingPath =
        new System.Collections.Generic.List<string>();
    internal System.Collections.Generic.List<string> DecodingPath =
        new System.Collections.Generic.List<string>();

    internal XerValueCodec(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal XerValueResult EncodeValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        XerNameResult name = this.Names.GetElementName(type);
        if (!name.Success)
        {
            return this.CopyNameFailure(name);
        }

        return this.EncodeValueAtDepth(type, value, name.Name, 0);
    }

    internal XerValueResult EncodeValueNamed(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        return this.EncodeValueAtDepth(type, value, elementName, 0);
    }

    internal XerValueResult DecodeValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count)
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
                XerValueResult declarationError = this.CreateError(
                    "ASNXER0110",
                    "The BASIC-XER XML declaration must specify version 1.0 and UTF-8 only.",
                    false);
                declarationError.HasViolationOffset = true;
                declarationError.ViolationOffset = prolog.ByteOffset;
                declarationError.ValuePath = "$";
                return declarationError;
            }

            valueOffset = valueOffset + 1;
            valueCount = valueCount - 1;
        }

        XerValueResult result = this.DecodeValueAtDepth(
            type,
            tokens,
            valueOffset,
            valueCount,
            name.Name,
            0);
        if (result.Success && declaration)
        {
            result.ConsumedCount = result.ConsumedCount + 1;
        }

        return result;
    }

    internal XerValueResult DecodeValueNamed(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        return this.DecodeValueAtDepth(
            type,
            tokens,
            offset,
            count,
            elementName,
            0);
    }

    internal XerValueResult EncodeValueAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        this.EncodingPath.Add(elementName);
        XerValueResult encoded = this.EncodeValueCoreAtDepth(
            type,
            value,
            elementName,
            depth);
        if (encoded.Success && this.Extended)
        {
            encoded = this.ApplyDefaultForEmptyEncoding(type, value, encoded);
            if (encoded.Success)
            {
                encoded = this.ApplyPiOrCommentEncoding(type, encoded);
            }
        }

        if (!encoded.Success)
        {
            this.SetEncodingFailureContext(encoded);
        }

        this.EncodingPath.RemoveAt(this.EncodingPath.Count - 1);
        return encoded;
    }

    internal XerValueResult EncodeValueCoreAtDepth(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (value.UnknownExtensions.Length > 0
            && this.UnknownDataPolicy != AsnUnknownDataPolicy.Skip)
        {
            return this.CreateError(
                "ASNXER0155",
                "Binary unknown extensions cannot be transferred to XER.",
                false);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.AnyElement) != null)
        {
            return this.EncodeAnyElement(type, value, depth);
        }

        XerExtendedName identity = this.ResolveExtendedName(type, elementName);
        if (!identity.Success)
        {
            return this.CreateError("ASNXER0320", identity.ErrorMessage, false);
        }

        elementName = identity.LocalName;

        if (!this.Characters.IsValidNcName(elementName))
        {
            return this.CreateError(
                "ASNXER0100",
                "The BASIC-XER wrapper name is not a valid XML NCName.",
                false);
        }

        if (!this.AllowsDepth(depth))
        {
            return this.CreateError(
                "ASNR0001",
                "The BASIC-XER value exceeds the configured nesting depth.",
                true);
        }

        XerValueResult encoded;
        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Base64) != null)
        {
            encoded = this.EncodeBase64Value(type, value, elementName, depth);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Decimal) != null)
        {
            encoded = this.EncodeDecimalValue(type, value, elementName);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseNumber) != null)
        {
            encoded = this.EncodeNumberValue(type, value, elementName);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseQName) != null)
        {
            encoded = this.EncodeQNameValue(type, value, elementName);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Whitespace) != null)
        {
            encoded = this.EncodeWhitespaceValue(type, value, elementName);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.List) != null)
        {
            encoded = this.EncodeListValue(type, value, elementName, depth);
        }
        else if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Text) != null)
        {
            encoded = this.EncodeTextValue(type, value, elementName);
        }
        else if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            encoded = this.EncodeCollection(
                type,
                value,
                elementName,
                depth);
        }
        else if (type.Kind == AsnTypeKind.Choice
            && this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseType) != null)
        {
            encoded = this.EncodeChoiceWithType(
                type,
                value,
                elementName,
                depth);
        }
        else if (type.Kind == AsnTypeKind.Choice
            && this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseUnion) != null)
        {
            encoded = this.EncodeChoiceWithUnion(
                type,
                value,
                elementName,
                depth);
        }
        else if (type.Kind == AsnTypeKind.Choice)
        {
            encoded = this.EncodeChoice(type, value, elementName, depth);
        }
        else if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            encoded = this.EncodeCollectionOf(
                type,
                value,
                elementName,
                depth);
        }
        else if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString
            || type.Kind == AsnTypeKind.InstanceOf)
        {
            encoded = this.EncodeUseful(type, value, elementName, depth);
        }
        else if (type.Kind == AsnTypeKind.OpenType)
        {
            encoded = this.EncodeOpenType(type, value, elementName, depth);
        }
        else
        {
            XerPrimitiveResult primitive = new XerPrimitiveCodec(this.Limits)
                .EncodePrimitiveNamed(type, value, elementName);
            if (this.Canonical && primitive.Success)
            {
                primitive = this.CanonicalizePrimitiveEncoding(
                    type,
                    value,
                    elementName,
                    primitive);
            }

            encoded = this.CopyPrimitiveResult(primitive);
        }

        return this.ApplyElementIdentity(encoded, identity);
    }

    internal XerValueResult EncodeAnyElement(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        int depth)
    {
        if (!this.AllowsDepth(depth))
        {
            return this.CreateError(
                "ASNR0001",
                "The EXTENDED-XER wildcard exceeds the configured nesting depth.",
                true);
        }

        if (type.Kind != AsnTypeKind.Utf8String
            || value.Kind != AsnRuntimeValueKind.CharacterString)
        {
            return this.CreateError(
                "ASNXER0300",
                "ANY-ELEMENT requires a UTF8String runtime value.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(
            value.TextValue.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The ANY-ELEMENT XML text exceeds the configured limit.",
                true);
        }

        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(value.TextValue),
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length == 0
            || read.Tokens[0].Kind == XerXmlTokenKind.XmlDeclaration)
        {
            return this.CreateError(
                "ASNXER0301",
                "The ANY-ELEMENT UTF8String must contain one complete XML element.",
                false);
        }

        int childCount = this.GetElementTokenCount(read.Tokens, 0);
        if (childCount == 0 || childCount != read.Tokens.Length)
        {
            return this.CreateError(
                "ASNXER0301",
                "The ANY-ELEMENT UTF8String must contain one complete XML element.",
                false);
        }

        XerWildcardRestriction restriction = this.ParseWildcardRestriction(
            this.FindInstruction(type, AsnInstructionKind.AnyElement));
        if (!restriction.Success)
        {
            return this.CreateError(
                "ASNXER0302",
                restriction.ErrorMessage,
                false);
        }

        if (!this.IsWildcardNamespaceAllowed(
            restriction,
            read.Tokens[0].NamespaceUri))
        {
            return this.CreateError(
                "ASNXER0303",
                "The ANY-ELEMENT root namespace violates its namespace restriction.",
                false);
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens = read.Tokens,
            ConsumedCount = read.Tokens.Length
        };
    }

    internal XerValueResult DecodeAnyElement(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        if (type.Kind != AsnTypeKind.Utf8String
            || count == 0
            || !this.IsElement(tokens[offset]))
        {
            return this.CreateError(
                "ASNXER0300",
                "ANY-ELEMENT requires a UTF8String and one XML element.",
                false);
        }

        int childCount = this.GetElementTokenCount(tokens, offset);
        if (childCount == 0 || childCount != count)
        {
            return this.CreateError(
                "ASNXER0301",
                "The ANY-ELEMENT value is not one complete XML element.",
                false);
        }

        XerWildcardRestriction restriction = this.ParseWildcardRestriction(
            this.FindInstruction(type, AsnInstructionKind.AnyElement));
        if (!restriction.Success)
        {
            return this.CreateError(
                "ASNXER0302",
                restriction.ErrorMessage,
                false);
        }

        if (!this.IsWildcardNamespaceAllowed(
            restriction,
            tokens[offset].NamespaceUri))
        {
            return this.CreateError(
                "ASNXER0303",
                "The ANY-ELEMENT root namespace violates its namespace restriction.",
                false);
        }

        byte[] data = this.SerializeWildcardElement(tokens, offset, count);
        if (data.Length == 0
            || !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(data.Length))
        {
            return this.CreateError(
                data.Length == 0 ? "ASNXER0304" : "ASNR0010",
                data.Length == 0
                    ? "The ANY-ELEMENT XML fragment could not be retained."
                    : "The ANY-ELEMENT XML fragment exceeds the configured limit.",
                data.Length != 0);
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = System.Text.Encoding.UTF8.GetString(data)
            },
            tokens,
            offset,
            count);
    }

    internal XerValueResult DecodeValueAtDepth(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
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

        this.DecodingPath.Add(elementName);
        XerXmlToken[] effectiveTokens = this.RemovePiOrCommentTokens(
            type,
            tokens,
            offset,
            count);
        int[] range = this.GetPiOrCommentValueRange(
            type,
            effectiveTokens,
            0,
            effectiveTokens.Length);
        if (range[0] < 0)
        {
            XerValueResult associationError = this.CreateError(
                "ASNXER0359",
                "The PI-OR-COMMENT value does not contain one associated element.",
                false);
            this.SetDecodingFailureContext(
                associationError,
                tokens,
                offset,
                count);
            this.DecodingPath.RemoveAt(this.DecodingPath.Count - 1);
            return associationError;
        }

        XerValueResult defaultValue = this.DecodeDefaultForEmptyValue(
            type,
            effectiveTokens,
            range[0],
            range[1],
            elementName);
        XerValueResult decoded = defaultValue.Success
            ? defaultValue
            : this.DecodeValueCoreAtDepth(
                type,
                effectiveTokens,
                range[0],
                range[1],
                elementName,
                depth);
        if (decoded.Success && effectiveTokens != tokens)
        {
            decoded.ConsumedCount = count;
        }

        if (!decoded.Success)
        {
            this.SetDecodingFailureContext(
                decoded,
                effectiveTokens,
                range[0],
                range[1]);
        }

        this.DecodingPath.RemoveAt(this.DecodingPath.Count - 1);
        return decoded;
    }

    internal XerValueResult DecodeValueCoreAtDepth(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
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

        if (!this.AllowsDepth(depth))
        {
            return this.CreateError(
                "ASNR0001",
                "The BASIC-XER value exceeds the configured nesting depth.",
                true);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.AnyElement) != null)
        {
            return this.DecodeAnyElement(type, tokens, offset, count);
        }

        XerExtendedName identity = this.ResolveExtendedName(type, elementName);
        if (!identity.Success)
        {
            return this.CreateError("ASNXER0320", identity.ErrorMessage, false);
        }

        if (count == 0
            || !this.IsElement(tokens[offset])
            || tokens[offset].LocalName != identity.LocalName
            || tokens[offset].NamespaceUri != identity.NamespaceUri)
        {
            return this.CreateError(
                "ASNXER0321",
                "The EXTENDED-XER wrapper has the wrong expanded element name: expected "
                    + identity.NamespaceUri + "#" + identity.LocalName
                    + " but found "
                    + (count == 0 ? "empty input" : tokens[offset].NamespaceUri
                        + "#" + tokens[offset].LocalName) + ".",
                false);
        }

        elementName = tokens[offset].Name;

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Base64) != null)
        {
            return this.DecodeBase64Value(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Decimal) != null)
        {
            return this.DecodeDecimalValue(
                type,
                tokens,
                offset,
                count,
                elementName);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseNumber) != null)
        {
            return this.DecodeNumberValue(
                type,
                tokens,
                offset,
                count,
                elementName);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseQName) != null)
        {
            return this.DecodeQNameValue(
                type,
                tokens,
                offset,
                count,
                elementName);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Whitespace) != null)
        {
            return this.DecodeWhitespaceValue(
                type,
                tokens,
                offset,
                count,
                elementName);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.List) != null)
        {
            return this.DecodeListValue(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (this.Extended
            && this.FindInstruction(type, AsnInstructionKind.Text) != null)
        {
            return this.DecodeTextValue(
                type,
                tokens,
                offset,
                count,
                elementName);
        }

        if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            return this.DecodeCollection(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.Choice
            && this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseType) != null)
        {
            return this.DecodeChoiceWithType(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.Choice
            && this.Extended
            && this.FindInstruction(type, AsnInstructionKind.UseUnion) != null)
        {
            return this.DecodeChoiceWithUnion(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return this.DecodeChoice(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            return this.DecodeCollectionOf(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.External
            || type.Kind == AsnTypeKind.EmbeddedPdv
            || type.Kind == AsnTypeKind.CharacterString
            || type.Kind == AsnTypeKind.InstanceOf)
        {
            return this.DecodeUseful(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            return this.DecodeOpenType(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        XerPrimitiveCodec primitives = new XerPrimitiveCodec(this.Limits);
        primitives.AllowWrapperNamespace = this.Extended;
        XerPrimitiveResult primitive = primitives.DecodePrimitiveNamed(
            type,
            tokens,
            offset,
            count,
            elementName);
        return this.CopyPrimitiveResult(primitive);
    }

    internal bool HasModifiedSequenceContent(AsnTypeDescriptor type)
    {
        return type.Kind == AsnTypeKind.Sequence
            && (this.FindInstruction(
                    type,
                    AsnInstructionKind.EmbedValues) != null
                || this.FindInstruction(
                    type,
                    AsnInstructionKind.UseNil) != null
                || this.FindInstruction(
                    type,
                    AsnInstructionKind.UseOrder) != null
                || this.FindInstruction(
                    type,
                    AsnInstructionKind.DefaultForEmpty) != null);
    }

    internal XerValueResult EncodeModifiedSequence(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        int embedIndex = this.FindInstruction(
            type,
            AsnInstructionKind.EmbedValues) == null ? -1 : 0;
        int orderIndex = this.FindInstruction(
            type,
            AsnInstructionKind.UseOrder) == null
                ? -1
                : embedIndex < 0 ? 0 : 1;
        int nilIndex = this.FindInstruction(
            type,
            AsnInstructionKind.UseNil) == null
                ? -1
                : this.FindNilComponentIndex(
                    descriptors,
                    embedIndex,
                    orderIndex);
        if (embedIndex >= descriptors.Length
            || orderIndex >= descriptors.Length
            || (this.FindInstruction(
                    type,
                    AsnInstructionKind.UseNil) != null
                && nilIndex < 0))
        {
            return this.CreateError(
                "ASNXER0360",
                "The modified sequence instruction layout is invalid.",
                false);
        }

        string[] orderNames = orderIndex < 0
            ? []
            : this.GetUseOrderNames(
                descriptors[orderIndex],
                this.FindRuntimeField(value.Fields, descriptors[orderIndex].Name));
        if (orderIndex >= 0 && orderNames.Length == 0)
        {
            return this.CreateError(
                "ASNXER0361",
                "USE-ORDER requires a non-empty enumeration order value.",
                false);
        }

        AsnFieldDescriptor[] retained = this.GetModifiedSequenceDescriptors(
            descriptors,
            embedIndex,
            orderIndex,
            nilIndex,
            -1,
            orderNames);
        if (retained.Length < descriptors.Length
                - (embedIndex < 0 ? 0 : 1)
                - (orderIndex < 0 ? 0 : 1)
                - (nilIndex < 0 ? 0 : 1)
            || !this.ValidateRuntimeFields(value.Fields, descriptors))
        {
            return this.CreateError(
                "ASNXER0362",
                "USE-ORDER does not identify each encoded component exactly once.",
                false);
        }

        AsnTypeDescriptor view = this.CreateModifiedSequenceView(
            type,
            retained,
            false);
        AsnRuntimeValue viewValue = this.CreateModifiedSequenceRuntimeValue(
            value,
            retained);
        XerValueResult baseEncoding = this.EncodeCollection(
            view,
            viewValue,
            elementName,
            depth);
        if (!baseEncoding.Success)
        {
            return baseEncoding;
        }

        XerWrappedValue baseWrapped = this.ReadModifiedSequenceWrapper(
            baseEncoding.Tokens,
            elementName);
        if (!baseWrapped.Success)
        {
            return this.CopyWrapperFailure(baseWrapped);
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        this.AppendTokens(content, baseWrapped.Content);
        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>(
                baseWrapped.Attributes);
        bool nilPresent = true;
        AsnRuntimeValue? nilValue = null;
        if (nilIndex >= 0)
        {
            AsnRuntimeFieldValue? nilField = this.FindRuntimeField(
                value.Fields,
                descriptors[nilIndex].Name);
            nilPresent = nilField != null
                && nilField.Present
                && nilField.Value != null;
            nilValue = nilPresent && nilField != null
                ? nilField.Value
                : null;
            if (!nilPresent)
            {
                XerValueResult nilAttribute = this.AddNilIdentification(
                    attributes,
                    true);
                if (!nilAttribute.Success)
                {
                    return nilAttribute;
                }
            }
            else if (nilValue != null && descriptors[nilIndex].Type != null)
            {
                AsnTypeDescriptor? nilType = descriptors[nilIndex].Type;
                if (nilType == null)
                {
                    return this.CreateError(
                        "ASNXER0369",
                        "The USE-NIL component has no resolved type.",
                        false);
                }

                XerValueResult nilEncoding = this.EncodeValueAtDepth(
                    nilType,
                    nilValue,
                    descriptors[nilIndex].Name,
                    depth + 1);
                XerValueResult unwrapped = this.RemoveAlternativeWrapper(
                    nilEncoding);
                if (!unwrapped.Success)
                {
                    return unwrapped;
                }

                content.Clear();
                this.AppendTokens(content, unwrapped.Tokens);
                if (orderIndex >= 0)
                {
                    XerValueResult reordered = this.ReorderModifiedContent(
                        nilType,
                        content.ToArray(),
                        orderNames);
                    if (!reordered.Success)
                    {
                        return reordered;
                    }

                    content.Clear();
                    this.AppendTokens(content, reordered.Tokens);
                }
            }
        }

        string[] embedded = [];
        if (embedIndex >= 0 && (nilIndex < 0 || nilPresent))
        {
            embedded = this.GetEmbeddedValues(
                descriptors[embedIndex],
                this.FindRuntimeField(value.Fields, descriptors[embedIndex].Name));
            XerValueResult interleaved = this.InterleaveEmbeddedValues(
                content.ToArray(),
                embedded);
            if (!interleaved.Success)
            {
                return interleaved;
            }

            content.Clear();
            this.AppendTokens(content, interleaved.Tokens);
        }

        XerValueResult result = this.WrapEncoding(value, elementName, content);
        if (!result.Success)
        {
            return result;
        }

        result.Tokens[0].Attributes = attributes.ToArray();
        if (this.IsModifiedSequenceDefaultValue(
            type,
            descriptors,
            value,
            embedIndex,
            nilIndex,
            nilValue,
            embedded))
        {
            result = this.MakeEncodingContentEmpty(result);
        }

        return result;
    }

    internal XerValueResult DecodeModifiedSequence(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        int embedIndex = this.FindInstruction(
            type,
            AsnInstructionKind.EmbedValues) == null ? -1 : 0;
        int orderIndex = this.FindInstruction(
            type,
            AsnInstructionKind.UseOrder) == null
                ? -1
                : embedIndex < 0 ? 0 : 1;
        int nilIndex = this.FindInstruction(
            type,
            AsnInstructionKind.UseNil) == null
                ? -1
                : this.FindNilComponentIndex(
                    descriptors,
                    embedIndex,
                    orderIndex);
        int defaultIndex = nilIndex >= 0 || embedIndex >= 0
            ? -1
            : this.FindDefaultUntaggedIndex(type, descriptors);
        if (embedIndex >= descriptors.Length
            || orderIndex >= descriptors.Length
            || (this.FindInstruction(
                    type,
                    AsnInstructionKind.UseNil) != null
                && nilIndex < 0))
        {
            return this.CreateError(
                "ASNXER0360",
                "The modified sequence instruction layout is invalid.",
                false);
        }

        XerWrappedValue wrapped = this.ReadModifiedSequenceWrapper(
            this.CopyTokenRange(tokens, offset, count),
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        int nilState = this.GetNilIdentification(wrapped.Attributes);
        if (nilState < 0)
        {
            return this.CreateError(
                "ASNXER0363",
                "The nil identification attribute is duplicated or malformed.",
                false);
        }

        if (nilIndex < 0 && nilState > 0)
        {
            return this.CreateError(
                "ASNXER0364",
                "A nil identification attribute requires USE-NIL.",
                false);
        }

        XerEmbeddedContent split = this.SplitEmbeddedContent(
            wrapped.Content,
            embedIndex >= 0 || nilIndex >= 0);
        string[] orderNames = orderIndex < 0 || nilIndex >= 0
            ? []
            : this.GetEncodedElementNames(
                descriptors,
                split.Elements,
                embedIndex,
                orderIndex,
                nilIndex);
        if (orderIndex >= 0 && nilIndex < 0 && orderNames.Length == 0)
        {
            return this.CreateError(
                "ASNXER0365",
                "USE-ORDER content does not identify an encoded component order.",
                false);
        }

        AsnFieldDescriptor[] retained = this.GetModifiedSequenceDescriptors(
            descriptors,
            embedIndex,
            orderIndex,
            nilIndex,
            defaultIndex,
            []);
        AsnTypeDescriptor view = this.CreateModifiedSequenceView(
            type,
            retained,
            orderIndex >= 0 && nilIndex < 0);
        XerXmlToken[] baseTokens = this.CreateModifiedWrapperTokens(
            tokens[offset],
            this.RemoveNilIdentification(wrapped.Attributes),
            nilIndex >= 0 ? [] : split.Elements);
        XerValueResult baseDecoded = this.DecodeCollection(
            view,
            baseTokens,
            0,
            baseTokens.Length,
            elementName,
            depth);
        if (!baseDecoded.Success || baseDecoded.Value == null)
        {
            return baseDecoded;
        }

        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        int descriptorIndex = 0;
        while (descriptorIndex < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[descriptorIndex];
            AsnRuntimeFieldValue? field = this.FindRuntimeField(
                baseDecoded.Value.Fields,
                descriptor.Name);
            if (descriptorIndex == embedIndex)
            {
                string[] texts = split.Texts;
                if (this.HasEmptyWrapperContent(
                        tokens,
                        offset,
                        count,
                        elementName)
                    && this.FindInstruction(
                        type,
                        AsnInstructionKind.DefaultForEmpty) != null)
                {
                    texts = [this.GetDefaultForEmptyText(type)];
                }

                fields.Add(this.CreateEmbeddedField(descriptor, texts));
            }
            else if (descriptorIndex == orderIndex)
            {
                string[] recoveredOrder = nilIndex >= 0
                    ? this.GetNestedEncodedElementNames(
                        descriptors[nilIndex].Type,
                        split.Elements)
                    : orderNames;
                fields.Add(this.CreateUseOrderField(
                    descriptor,
                    recoveredOrder));
            }
            else if (descriptorIndex == nilIndex)
            {
                if (nilState > 0)
                {
                    fields.Add(new AsnRuntimeFieldValue
                    {
                        Name = descriptor.Name,
                        Present = false
                    });
                }
                else
                {
                    XerValueResult nilDecoded = this.DecodeNilContent(
                        type,
                        descriptor,
                        split,
                        wrapped.NamespaceBindings,
                        depth);
                    if (!nilDecoded.Success || nilDecoded.Value == null)
                    {
                        return nilDecoded;
                    }

                    fields.Add(new AsnRuntimeFieldValue
                    {
                        Name = descriptor.Name,
                        Present = true,
                        Value = nilDecoded.Value
                    });
                }
            }
            else if (descriptorIndex == defaultIndex
                && this.HasEmptyWrapperContent(
                    tokens,
                    offset,
                    count,
                    elementName))
            {
                AsnEncodingInstruction? defaultInstruction =
                    this.FindInstruction(
                        type,
                        AsnInstructionKind.DefaultForEmpty);
                AsnRuntimeValue? defaultValue = descriptor.Type == null
                        || defaultInstruction == null
                    ? null
                    : this.CreateInstructionValue(
                        descriptor.Type,
                        defaultInstruction);
                if (defaultValue == null)
                {
                    return this.CreateError(
                        "ASNXER0350",
                        "DEFAULT-FOR-EMPTY cannot be applied to the UNTAGGED component.",
                        false);
                }

                fields.Add(new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = defaultValue
                });
            }
            else if (field != null)
            {
                fields.Add(field);
            }

            descriptorIndex = descriptorIndex + 1;
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = fields.ToArray(),
                UnknownFields = baseDecoded.Value.UnknownFields
            },
            tokens,
            offset,
            count);
    }

    internal int FindNilComponentIndex(
        AsnFieldDescriptor[] descriptors,
        int embedIndex,
        int orderIndex)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool attribute = descriptor.Type != null
                && (this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.Attribute) != null
                    || this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.AnyAttributes) != null);
            if (index != embedIndex
                && index != orderIndex
                && descriptor.Optional
                && !attribute)
            {
                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindDefaultUntaggedIndex(
        AsnTypeDescriptor type,
        AsnFieldDescriptor[] descriptors)
    {
        if (this.FindInstruction(
                type,
                AsnInstructionKind.DefaultForEmpty) == null)
        {
            return -1;
        }

        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnTypeDescriptor? fieldType = descriptors[index].Type;
            if (fieldType != null
                && this.FindInstruction(
                    fieldType,
                    AsnInstructionKind.Untagged) != null
                && this.IsCharacterEncodableDescriptor(fieldType))
            {
                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnFieldDescriptor[] GetModifiedSequenceDescriptors(
        AsnFieldDescriptor[] descriptors,
        int embedIndex,
        int orderIndex,
        int nilIndex,
        int defaultIndex,
        string[] orderNames)
    {
        System.Collections.Generic.List<AsnFieldDescriptor> result =
            new System.Collections.Generic.List<AsnFieldDescriptor>();
        bool[] added = new bool[descriptors.Length];
        int index = 0;
        if (orderNames.Length > 0)
        {
            while (index < descriptors.Length)
            {
                if (index != embedIndex
                    && index != orderIndex
                    && index != nilIndex
                    && index != defaultIndex
                    && this.IsAttributeDescriptor(descriptors[index]))
                {
                    result.Add(descriptors[index]);
                    added[index] = true;
                }

                index = index + 1;
            }

            int order = 0;
            while (order < orderNames.Length)
            {
                int descriptorIndex = this.FindDescriptorIndex(
                    descriptors,
                    orderNames[order]);
                if (descriptorIndex < 0
                    || descriptorIndex == embedIndex
                    || descriptorIndex == orderIndex
                    || descriptorIndex == nilIndex
                    || descriptorIndex == defaultIndex
                    || added[descriptorIndex])
                {
                    return [];
                }

                result.Add(descriptors[descriptorIndex]);
                added[descriptorIndex] = true;
                order = order + 1;
            }
        }

        index = 0;
        while (index < descriptors.Length)
        {
            if (index != embedIndex
                && index != orderIndex
                && index != nilIndex
                && index != defaultIndex
                && !added[index])
            {
                result.Add(descriptors[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool IsAttributeDescriptor(AsnFieldDescriptor descriptor)
    {
        return descriptor.Type != null
            && (this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Attribute) != null
                || this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyAttributes) != null);
    }

    internal AsnTypeDescriptor CreateModifiedSequenceView(
        AsnTypeDescriptor source,
        AsnFieldDescriptor[] fields,
        bool unordered)
    {
        return new AsnTypeDescriptor
        {
            StableId = source.StableId,
            Name = source.Name,
            GeneratedName = source.GeneratedName,
            Kind = unordered ? AsnTypeKind.Set : AsnTypeKind.Sequence,
            Fields = fields,
            Extensible = source.Extensible,
            SourceSpan = source.SourceSpan
        };
    }

    internal AsnRuntimeValue CreateModifiedSequenceRuntimeValue(
        AsnRuntimeValue source,
        AsnFieldDescriptor[] retained)
    {
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        int index = 0;
        while (index < retained.Length)
        {
            AsnRuntimeFieldValue? field = this.FindRuntimeField(
                source.Fields,
                retained[index].Name);
            if (field != null)
            {
                fields.Add(field);
            }

            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = fields.ToArray(),
            UnknownFields = source.UnknownFields
        };
    }

    internal string[] GetUseOrderNames(
        AsnFieldDescriptor descriptor,
        AsnRuntimeFieldValue? field)
    {
        if (descriptor.Type == null
            || descriptor.Type.Kind != AsnTypeKind.SequenceOf
            || descriptor.Type.ElementType == null
            || descriptor.Type.ElementType.Kind != AsnTypeKind.Enumerated
            || field == null
            || !field.Present
            || field.Value == null
            || field.Value.Kind != AsnRuntimeValueKind.SequenceOf)
        {
            return [];
        }

        string[] result = new string[field.Value.Items.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnRuntimeValue item = field.Value.Items[index];
            string name = item.TextValue;
            if (name.Length == 0 && item.HasIntegerValue)
            {
                AsnValueDescriptor? named = this.FindEnumeratedByNumber(
                    descriptor.Type.ElementType,
                    item.IntegerValue);
                name = named == null ? "" : named.Name;
            }

            if (name.Length == 0)
            {
                return [];
            }

            result[index] = name;
            index = index + 1;
        }

        return result;
    }

    internal string[] GetEmbeddedValues(
        AsnFieldDescriptor descriptor,
        AsnRuntimeFieldValue? field)
    {
        if (descriptor.Type == null
            || descriptor.Type.Kind != AsnTypeKind.SequenceOf
            || descriptor.Type.ElementType == null
            || descriptor.Type.ElementType.Kind != AsnTypeKind.Utf8String
            || field == null
            || !field.Present
            || field.Value == null
            || field.Value.Kind != AsnRuntimeValueKind.SequenceOf)
        {
            return [];
        }

        string[] result = new string[field.Value.Items.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnRuntimeValue item = field.Value.Items[index];
            if (item.Kind != AsnRuntimeValueKind.CharacterString)
            {
                return [];
            }

            result[index] = item.TextValue;
            index = index + 1;
        }

        return result;
    }

    internal XerWrappedValue ReadModifiedSequenceWrapper(
        XerXmlToken[] tokens,
        string elementName)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        return wrappers.ReadWrapper(
            tokens,
            0,
            tokens.Length,
            elementName);
    }

    internal XerValueResult AddNilIdentification(
        System.Collections.Generic.List<XerXmlAttribute> attributes,
        bool value)
    {
        XerValueResult declaration = this.AddNamespaceDeclarationEncoding(
            attributes,
            "asn1",
            ControlNamespace);
        if (!declaration.Success)
        {
            return declaration;
        }

        attributes.Add(new XerXmlAttribute
        {
            Name = "asn1:nil",
            Prefix = "asn1",
            LocalName = "nil",
            NamespaceUri = ControlNamespace,
            Value = value ? "true" : "false"
        });
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal XerValueResult InterleaveEmbeddedValues(
        XerXmlToken[] elements,
        string[] values)
    {
        int elementCount = 0;
        int index = 0;
        while (index < elements.Length)
        {
            int childCount = this.GetElementTokenCount(elements, index);
            if (childCount == 0)
            {
                return this.CreateError(
                    "ASNXER0366",
                    "EMBED-VALUES can only surround complete child elements.",
                    false);
            }

            elementCount = elementCount + 1;
            index = index + childCount;
        }

        if (values.Length != elementCount + 1)
        {
            return this.CreateError(
                "ASNXER0367",
                "EMBED-VALUES requires one more UTF8String than encoded elements.",
                false);
        }

        System.Collections.Generic.List<XerXmlToken> result =
            new System.Collections.Generic.List<XerXmlToken>();
        index = 0;
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            if (values[valueIndex].Length > 0)
            {
                result.Add(new XerXmlToken
                {
                    Kind = XerXmlTokenKind.Text,
                    Value = values[valueIndex]
                });
            }

            if (index < elements.Length)
            {
                int childCount = this.GetElementTokenCount(elements, index);
                int childIndex = 0;
                while (childIndex < childCount)
                {
                    result.Add(elements[index + childIndex]);
                    childIndex = childIndex + 1;
                }

                index = index + childCount;
            }

            valueIndex = valueIndex + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = result.ToArray()
        };
    }

    internal XerValueResult ReorderModifiedContent(
        AsnTypeDescriptor type,
        XerXmlToken[] content,
        string[] orderNames)
    {
        if (type == null
            || type.Kind != AsnTypeKind.Sequence
            || orderNames.Length == 0)
        {
            return this.CreateError(
                "ASNXER0368",
                "USE-ORDER with USE-NIL requires a structured optional value.",
                false);
        }

        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        System.Collections.Generic.List<XerXmlToken> result =
            new System.Collections.Generic.List<XerXmlToken>();
        int orderIndex = 0;
        while (orderIndex < orderNames.Length)
        {
            int descriptorIndex = this.FindDescriptorIndex(
                descriptors,
                orderNames[orderIndex]);
            if (descriptorIndex < 0 || descriptors[descriptorIndex].Type == null)
            {
                return this.CreateError(
                    "ASNXER0362",
                    "USE-ORDER names an unknown optional-sequence component.",
                    false);
            }

            int contentIndex = this.FindElementForDescriptor(
                descriptors[descriptorIndex],
                content);
            int childCount = contentIndex < 0
                ? 0
                : this.GetElementTokenCount(content, contentIndex);
            if (childCount == 0)
            {
                return this.CreateError(
                    "ASNXER0362",
                    "USE-ORDER names a component that is absent from the value.",
                    false);
            }

            int childIndex = 0;
            while (childIndex < childCount)
            {
                result.Add(content[contentIndex + childIndex]);
                childIndex = childIndex + 1;
            }

            orderIndex = orderIndex + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = result.ToArray()
        };
    }

    internal int FindElementForDescriptor(
        AsnFieldDescriptor descriptor,
        XerXmlToken[] content)
    {
        if (descriptor.Type == null)
        {
            return -1;
        }

        int index = 0;
        while (index < content.Length)
        {
            if (this.IsElement(content[index])
                && this.MatchesElementName(
                    descriptor.Type,
                    descriptor.Name,
                    content[index]))
            {
                return index;
            }

            int childCount = this.GetElementTokenCount(content, index);
            index = index + (childCount == 0 ? 1 : childCount);
        }

        return -1;
    }

    internal bool IsModifiedSequenceDefaultValue(
        AsnTypeDescriptor type,
        AsnFieldDescriptor[] descriptors,
        AsnRuntimeValue value,
        int embedIndex,
        int nilIndex,
        AsnRuntimeValue? nilValue,
        string[] embedded)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.DefaultForEmpty);
        if (instruction == null)
        {
            return false;
        }

        if (embedIndex >= 0)
        {
            return embedded.Length == 1
                && embedded[0] == this.GetDefaultForEmptyText(type);
        }

        if (nilIndex >= 0
            && nilValue != null
            && descriptors[nilIndex].Type != null)
        {
            AsnTypeDescriptor? nilType = descriptors[nilIndex].Type;
            if (nilType == null)
            {
                return false;
            }

            AsnRuntimeValue? defaultValue = this.CreateInstructionValue(
                nilType,
                instruction);
            return defaultValue != null
                && this.RuntimeScalarValuesEqual(
                    nilType,
                    nilValue,
                    defaultValue);
        }

        int untaggedIndex = this.FindDefaultUntaggedIndex(type, descriptors);
        if (untaggedIndex < 0 || descriptors[untaggedIndex].Type == null)
        {
            return false;
        }

        AsnRuntimeFieldValue? field = this.FindRuntimeField(
            value.Fields,
            descriptors[untaggedIndex].Name);
        AsnTypeDescriptor? untaggedType = descriptors[untaggedIndex].Type;
        if (untaggedType == null)
        {
            return false;
        }

        AsnRuntimeValue? expected = this.CreateInstructionValue(
            untaggedType,
            instruction);
        return field != null
            && field.Present
            && field.Value != null
            && expected != null
            && this.RuntimeScalarValuesEqual(
                untaggedType,
                field.Value,
                expected);
    }

    internal string GetDefaultForEmptyText(AsnTypeDescriptor type)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.DefaultForEmpty);
        if (instruction == null
            || instruction.Arguments.Length < 2
            || instruction.Arguments[0] != "AS")
        {
            return "";
        }

        string text = this.JoinInstructionValue(instruction.Arguments, 1);
        string decoded = this.DecodeInstructionString(text);
        return decoded.Length == 0 && text != "\"\"" ? text : decoded;
    }

    internal XerXmlToken[] CopyTokenRange(
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        XerXmlToken[] result = new XerXmlToken[count];
        int index = 0;
        while (index < count)
        {
            result[index] = tokens[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal int GetNilIdentification(XerXmlAttribute[] attributes)
    {
        int state = 0;
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration
                && attribute.NamespaceUri == ControlNamespace
                && attribute.LocalName == "nil")
            {
                if (state != 0)
                {
                    return -1;
                }

                if (attribute.Value == "true" || attribute.Value == "1")
                {
                    state = 1;
                }
                else if (attribute.Value == "false" || attribute.Value == "0")
                {
                    state = 2;
                }
                else
                {
                    return -1;
                }
            }

            index = index + 1;
        }

        return state;
    }

    internal XerXmlAttribute[] RemoveNilIdentification(
        XerXmlAttribute[] attributes)
    {
        System.Collections.Generic.List<XerXmlAttribute> result =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (attribute.NamespaceDeclaration
                || attribute.NamespaceUri != ControlNamespace
                || attribute.LocalName != "nil")
            {
                result.Add(attribute);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XerEmbeddedContent SplitEmbeddedContent(
        XerXmlToken[] content,
        bool collectText)
    {
        XerEmbeddedContent result = new XerEmbeddedContent();
        System.Collections.Generic.List<XerXmlToken> elements =
            new System.Collections.Generic.List<XerXmlToken>();
        System.Collections.Generic.List<string> texts =
            new System.Collections.Generic.List<string>();
        string current = "";
        int index = 0;
        while (index < content.Length)
        {
            XerXmlToken token = content[index];
            if (token.Kind == XerXmlTokenKind.Text
                || token.Kind == XerXmlTokenKind.CData)
            {
                if (collectText)
                {
                    current = current + token.Value;
                }
                else
                {
                    elements.Add(token);
                }

                index = index + 1;
            }
            else if (token.Kind == XerXmlTokenKind.Comment
                || token.Kind == XerXmlTokenKind.ProcessingInstruction)
            {
                index = index + 1;
            }
            else
            {
                int childCount = this.GetElementTokenCount(content, index);
                if (childCount == 0)
                {
                    result.ErrorMessage =
                        "The modified sequence contains incomplete content.";
                    return result;
                }

                if (collectText)
                {
                    texts.Add(current);
                    current = "";
                }

                int childIndex = 0;
                while (childIndex < childCount)
                {
                    elements.Add(content[index + childIndex]);
                    childIndex = childIndex + 1;
                }

                index = index + childCount;
            }
        }

        if (collectText)
        {
            texts.Add(current);
        }

        result.Success = true;
        result.Elements = elements.ToArray();
        result.Texts = texts.ToArray();
        return result;
    }

    internal string[] GetEncodedElementNames(
        AsnFieldDescriptor[] descriptors,
        XerXmlToken[] elements,
        int embedIndex,
        int orderIndex,
        int nilIndex)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < elements.Length)
        {
            int childCount = this.GetElementTokenCount(elements, index);
            int descriptorIndex = this.FindDescriptorIndexForElement(
                descriptors,
                elements[index],
                []);
            if (childCount == 0
                || descriptorIndex < 0
                || descriptorIndex == embedIndex
                || descriptorIndex == orderIndex
                || descriptorIndex == nilIndex
                || this.IsAttributeDescriptor(descriptors[descriptorIndex]))
            {
                return [];
            }

            result.Add(descriptors[descriptorIndex].Name);
            index = index + childCount;
        }

        return result.ToArray();
    }

    internal string[] GetNestedEncodedElementNames(
        AsnTypeDescriptor? type,
        XerXmlToken[] elements)
    {
        if (type == null
            || type.Kind != AsnTypeKind.Sequence)
        {
            return [];
        }

        return this.GetEncodedElementNames(
            this.GetAllFields(type),
            elements,
            -1,
            -1,
            -1);
    }

    internal AsnRuntimeFieldValue CreateEmbeddedField(
        AsnFieldDescriptor descriptor,
        string[] texts)
    {
        AsnRuntimeValue[] items = new AsnRuntimeValue[texts.Length];
        int index = 0;
        while (index < texts.Length)
        {
            items[index] = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = texts[index]
            };
            index = index + 1;
        }

        return new AsnRuntimeFieldValue
        {
            Name = descriptor.Name,
            Present = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = items
            }
        };
    }

    internal AsnRuntimeFieldValue CreateUseOrderField(
        AsnFieldDescriptor descriptor,
        string[] names)
    {
        AsnRuntimeValue[] items = new AsnRuntimeValue[names.Length];
        AsnTypeDescriptor? enumeration = descriptor.Type == null
            ? null
            : descriptor.Type.ElementType;
        int index = 0;
        while (index < names.Length)
        {
            AsnValueDescriptor? named = enumeration == null
                ? null
                : this.FindNamedValueByName(enumeration, names[index]);
            items[index] = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Enumerated,
                TextValue = names[index],
                HasIntegerValue = named != null
                    && named.Value != null
                    && named.Value.HasIntegerValue,
                IntegerValue = named != null
                        && named.Value != null
                        && named.Value.HasIntegerValue
                    ? named.Value.IntegerValue
                    : new System.Numerics.BigInteger(index)
            };
            index = index + 1;
        }

        return new AsnRuntimeFieldValue
        {
            Name = descriptor.Name,
            Present = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = items
            }
        };
    }

    internal XerXmlToken[] CreateModifiedWrapperTokens(
        XerXmlToken source,
        XerXmlAttribute[] attributes,
        XerXmlToken[] content)
    {
        if (content.Length == 0)
        {
            return
            [
                new XerXmlToken
                {
                    Kind = XerXmlTokenKind.EmptyElement,
                    Name = source.Name,
                    Prefix = source.Prefix,
                    LocalName = source.LocalName,
                    NamespaceUri = source.NamespaceUri,
                    Attributes = attributes,
                    NamespaceBindings = source.NamespaceBindings
                }
            ];
        }

        XerXmlToken[] result = new XerXmlToken[content.Length + 2];
        result[0] = new XerXmlToken
        {
            Kind = XerXmlTokenKind.StartElement,
            Name = source.Name,
            Prefix = source.Prefix,
            LocalName = source.LocalName,
            NamespaceUri = source.NamespaceUri,
            Attributes = attributes,
            NamespaceBindings = source.NamespaceBindings
        };
        int index = 0;
        while (index < content.Length)
        {
            result[index + 1] = content[index];
            index = index + 1;
        }

        result[result.Length - 1] = new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement,
            Name = source.Name,
            Prefix = source.Prefix,
            LocalName = source.LocalName,
            NamespaceUri = source.NamespaceUri,
            NamespaceBindings = source.NamespaceBindings
        };
        return result;
    }

    internal XerValueResult DecodeNilContent(
        AsnTypeDescriptor owner,
        AsnFieldDescriptor descriptor,
        XerEmbeddedContent split,
        XerXmlNamespaceBinding[] bindings,
        int depth)
    {
        if (descriptor.Type == null)
        {
            return this.CreateError(
                "ASNXER0369",
                "The USE-NIL optional component has no resolved type.",
                false);
        }

        AsnEncodingInstruction? defaultInstruction = this.FindInstruction(
            owner,
            AsnInstructionKind.DefaultForEmpty);
        bool empty = split.Elements.Length == 0
            && (split.Texts.Length == 0
                || split.Texts.Length == 1 && split.Texts[0].Length == 0);
        if (empty && defaultInstruction != null)
        {
            AsnRuntimeValue? defaultValue = this.CreateInstructionValue(
                descriptor.Type,
                defaultInstruction);
            if (defaultValue != null)
            {
                return new XerValueResult
                {
                    Success = true,
                    Status = AsnDecodeStatus.Done,
                    Value = defaultValue
                };
            }
        }

        if (this.IsCharacterEncodableDescriptor(descriptor.Type))
        {
            string text = split.Texts.Length == 0 ? "" : split.Texts[0];
            XerCharacterValueResult character =
                this.DecodeCharacterValueWithBindings(
                    descriptor.Type,
                    text,
                    bindings);
            return character.Success && character.Value != null
                ? new XerValueResult
                {
                    Success = true,
                    Status = AsnDecodeStatus.Done,
                    Value = character.Value
                }
                : this.CreateError(
                    character.ErrorCode,
                    character.ErrorMessage,
                    character.LimitExceeded);
        }

        XerExtendedName identity = this.ResolveExtendedName(
            descriptor.Type,
            descriptor.Name);
        if (!identity.Success)
        {
            return this.CreateError(
                "ASNXER0320",
                identity.ErrorMessage,
                false);
        }

        XerXmlToken source = new XerXmlToken
        {
            Name = identity.Prefix.Length == 0
                ? identity.LocalName
                : identity.Prefix + ":" + identity.LocalName,
            Prefix = identity.Prefix,
            LocalName = identity.LocalName,
            NamespaceUri = identity.NamespaceUri,
            NamespaceBindings = bindings
        };
        XerXmlToken[] wrapped = this.CreateModifiedWrapperTokens(
            source,
            [],
            split.Elements);
        if (this.FindInstruction(
                owner,
                AsnInstructionKind.UseOrder) != null
            && descriptor.Type.Kind == AsnTypeKind.Sequence)
        {
            AsnTypeDescriptor unordered = this.CreateModifiedSequenceView(
                descriptor.Type,
                this.GetAllFields(descriptor.Type),
                true);
            XerValueResult decoded = this.DecodeCollection(
                unordered,
                wrapped,
                0,
                wrapped.Length,
                source.Name,
                depth + 1);
            if (decoded.Success && decoded.Value != null)
            {
                decoded.Value.Kind = AsnRuntimeValueKind.Sequence;
            }

            return decoded;
        }

        return this.DecodeValueAtDepth(
            descriptor.Type,
            wrapped,
            0,
            wrapped.Length,
            descriptor.Name,
            depth + 1);
    }

    internal bool IsCharacterEncodableDescriptor(AsnTypeDescriptor type)
    {
        if (type.Kind == AsnTypeKind.Boolean
            || type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated
            || type.Kind == AsnTypeKind.Real
            || type.Kind == AsnTypeKind.BitString
            || type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier
            || type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri
            || type.Kind == AsnTypeKind.UtcTime
            || type.Kind == AsnTypeKind.GeneralizedTime
            || type.Kind == AsnTypeKind.Time
            || type.Kind == AsnTypeKind.Date
            || type.Kind == AsnTypeKind.TimeOfDay
            || type.Kind == AsnTypeKind.DateTime
            || type.Kind == AsnTypeKind.Duration
            || type.Kind == AsnTypeKind.ObjectDescriptor
            || this.IsRestrictedCharacterKind(type.Kind))
        {
            return true;
        }

        return this.FindInstruction(type, AsnInstructionKind.Base64) != null
            || this.FindInstruction(type, AsnInstructionKind.Decimal) != null
            || this.FindInstruction(type, AsnInstructionKind.UseNumber) != null
            || this.FindInstruction(type, AsnInstructionKind.UseQName) != null
            || this.FindInstruction(type, AsnInstructionKind.List) != null
            || this.FindInstruction(type, AsnInstructionKind.UseUnion) != null;
    }

    internal XerValueResult EncodeCollection(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.Set
            ? AsnRuntimeValueKind.Set
            : AsnRuntimeValueKind.Sequence;
        if (value.Kind != expected)
        {
            return this.CreateError(
                "ASNXER0120",
                "The runtime value does not match the BASIC-XER structured type.",
                false);
        }

        if (this.Extended && this.HasModifiedSequenceContent(type))
        {
            return this.EncodeModifiedSequence(
                type,
                value,
                elementName,
                depth);
        }

        AsnFieldDescriptor[] descriptors = this.Canonical
            ? this.GetCanonicalCollectionFields(type)
            : this.GetAllFields(type);
        int wildcardAttributeIndex = this.Extended
            ? this.FindAnyAttributesDescriptorIndex(descriptors)
            : -1;
        if (wildcardAttributeIndex == -2
            || (wildcardAttributeIndex >= 0
                && (descriptors[wildcardAttributeIndex].Optional
                    || descriptors[wildcardAttributeIndex].HasDefaultValue)))
        {
            return this.CreateError(
                "ASNXER0315",
                "An enclosing type requires exactly one mandatory ANY-ATTRIBUTES component.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            descriptors.Length + value.UnknownFields.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The BASIC-XER collection exceeds the configured element limit.",
                true);
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor == null || descriptor.Type == null)
            {
                return this.CreateError(
                    "ASNXER0121",
                    "A BASIC-XER component has no resolved type.",
                    false);
            }

            AsnRuntimeFieldValue? field = this.FindRuntimeField(
                value.Fields,
                descriptor.Name);
            AsnRuntimeValue? fieldValue = field == null
                ? null
                : field.Value;
            bool present = field != null && field.Present
                && fieldValue != null;
            if (!present
                && this.Canonical
                && descriptor.HasDefaultValue)
            {
                fieldValue = new ValueAccess().CreateDefaultRuntimeValue(
                    descriptor,
                    new AsnReflectionCatalog());
                present = fieldValue != null;
            }

            if (!present
                && this.Canonical
                && descriptor.HasDefaultValue)
            {
                return this.CreateError(
                    "ASNXER0205",
                    "A canonical XER DEFAULT component requires a resolved default value: "
                        + descriptor.Name,
                    false);
            }

            if (!present)
            {
                if (!descriptor.Optional
                    && !descriptor.HasDefaultValue
                    && !descriptor.Extension)
                {
                    return this.CreateError(
                        "ASNXER0122",
                        "A required BASIC-XER component is absent: "
                            + descriptor.Name,
                        false);
                }
            }
            else
            {
                if (fieldValue == null)
                {
                    return this.CreateError(
                        "ASNXER0123",
                        "A present BASIC-XER component has no value: "
                            + descriptor.Name,
                        false);
                }

                XerValueResult encoded;
                if (this.Extended
                    && this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.Attribute) != null)
                {
                    XerCharacterValueResult character =
                        this.EncodeCharacterValue(
                            descriptor.Type,
                            fieldValue);
                    if (!character.Success)
                    {
                        return this.CreateError(
                            character.ErrorCode,
                            character.ErrorMessage,
                            character.LimitExceeded);
                    }

                    if (character.NamespaceUri.Length > 0)
                    {
                        XerValueResult declaration =
                            this.AddNamespaceDeclarationEncoding(
                                attributes,
                                character.Prefix,
                                character.NamespaceUri);
                        if (!declaration.Success)
                        {
                            return declaration;
                        }
                    }

                    encoded = this.AddAttributeEncoding(
                        descriptor.Type,
                        descriptor.Name,
                        character.Text,
                        attributes);
                }
                else if (this.Extended
                    && this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.AnyAttributes) != null)
                {
                    encoded = this.EncodeAnyAttributes(
                        descriptor.Type,
                        fieldValue,
                        attributes);
                }
                else
                {
                    encoded = this.EncodeValueAtDepth(
                        descriptor.Type,
                        fieldValue,
                        descriptor.Name,
                        depth + 1);
                }

                if (!encoded.Success)
                {
                    return encoded;
                }

                if (this.Extended
                    && this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.Untagged) != null)
                {
                    encoded = this.RemoveUntaggedWrapper(
                        descriptor.Type,
                        fieldValue,
                        encoded);
                    if (!encoded.Success)
                    {
                        return encoded;
                    }
                }

                this.AppendTokens(content, encoded.Tokens);
            }

            index = index + 1;
        }

        if (!this.ValidateRuntimeFields(value.Fields, descriptors))
        {
            return this.CreateError(
                "ASNXER0124",
                "The BASIC-XER value contains an undefined component.",
                false);
        }

        if (value.UnknownFields.Length > 0 && !type.Extensible)
        {
            return this.CreateError(
                "ASNXER0125",
                "Unknown BASIC-XER components require an extensible type.",
                false);
        }

        if (this.UnknownDataPolicy == AsnUnknownDataPolicy.Preserve)
        {
            XerValueResult unknownAttributes =
                this.AddPreservedUnknownAttributes(
                    value.UnknownFields,
                    attributes);
            if (!unknownAttributes.Success)
            {
                return unknownAttributes;
            }

            XerValueResult unknownElements =
                this.InsertPreservedUnknownElements(
                    value.UnknownFields,
                    content);
            if (!unknownElements.Success)
            {
                return unknownElements;
            }
        }

        XerValueResult result = this.WrapEncoding(value, elementName, content);
        if (result.Success && result.Tokens.Length > 0)
        {
            result.Tokens[0].Attributes = attributes.ToArray();
        }

        return result;
    }

    internal XerValueResult DecodeCollection(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        if (this.Extended && this.HasModifiedSequenceContent(type))
        {
            return this.DecodeModifiedSequence(
                type,
                tokens,
                offset,
                count,
                elementName,
                depth);
        }

        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        int wildcardAttributeIndex = this.Extended
            ? this.FindAnyAttributesDescriptorIndex(descriptors)
            : -1;
        if (wildcardAttributeIndex == -2
            || (wildcardAttributeIndex >= 0
                && (descriptors[wildcardAttributeIndex].Optional
                    || descriptors[wildcardAttributeIndex].HasDefaultValue)))
        {
            return this.CreateError(
                "ASNXER0315",
                "An enclosing type requires exactly one mandatory ANY-ATTRIBUTES component.",
                false);
        }

        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = this.Extended;
        wrappers.AllowWrapperNamespace = this.Extended;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        bool allowUnknownAttributes = wildcardAttributeIndex >= 0
            || this.Extended
                && type.Extensible
                && this.UnknownDataPolicy != AsnUnknownDataPolicy.Reject;
        if (!this.ValidateCollectionAttributes(
            descriptors,
            wrapped.Attributes,
            allowUnknownAttributes))
        {
            return this.CreateError(
                "ASNXER0330",
                "An enclosing element attribute is unknown, duplicated, or collides by expanded name.",
                false);
        }

        XerValueResult decoded;
        if (type.Kind == AsnTypeKind.Set)
        {
            decoded = this.DecodeSetContent(
                type,
                tokens,
                offset,
                wrapped,
                depth);
        }
        else
        {
            decoded = this.DecodeSequenceContent(
                type,
                tokens,
                offset,
                wrapped,
                depth);
        }

        if (!decoded.Success
            || decoded.Value == null
            || wildcardAttributeIndex >= 0
            || this.UnknownDataPolicy != AsnUnknownDataPolicy.Preserve)
        {
            return decoded;
        }

        XerValueResult preservedAttributes = this.PreserveUnknownAttributes(
            descriptors,
            wrapped.Attributes,
            wrapped.NamespaceBindings);
        if (!preservedAttributes.Success || preservedAttributes.Value == null)
        {
            return preservedAttributes;
        }

        decoded.Value.UnknownFields = this.CombineUnknownFields(
            decoded.Value.UnknownFields,
            preservedAttributes.Value.UnknownFields);
        return decoded;
    }

    internal XerValueResult DecodeSequenceContent(
        AsnTypeDescriptor type,
        XerXmlToken[] sourceTokens,
        int sourceOffset,
        XerWrappedValue wrapped,
        int depth)
    {
        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            descriptors.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The BASIC-XER collection exceeds the configured element limit.",
                true);
        }

        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        System.Collections.Generic.List<AsnUnknownField> unknown =
            new System.Collections.Generic.List<AsnUnknownField>();
        int contentIndex = 0;
        int descriptorIndex = 0;
        int siblingIndex = 0;
        while (descriptorIndex < descriptors.Length)
        {
            contentIndex = this.SkipIgnorable(
                wrapped.Content,
                contentIndex);
            AsnFieldDescriptor descriptor = descriptors[descriptorIndex];
            if (descriptor == null || descriptor.Type == null)
            {
                return this.CreateError(
                    "ASNXER0121",
                    "A BASIC-XER component has no resolved type.",
                    false);
            }

            if (this.Extended
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Attribute) != null)
            {
                XerValueResult attribute = this.DecodeAttributeValue(
                    descriptor,
                    wrapped.Attributes,
                    wrapped.NamespaceBindings);
                if (!attribute.Success)
                {
                    return attribute;
                }

                if (attribute.Value != null)
                {
                    fields.Add(new AsnRuntimeFieldValue
                    {
                        Name = descriptor.Name,
                        Present = true,
                        Value = attribute.Value
                    });
                }
                else if (descriptor.Optional
                    || descriptor.HasDefaultValue
                    || descriptor.Extension)
                {
                    fields.Add(new AsnRuntimeFieldValue
                    {
                        Name = descriptor.Name,
                        Present = false
                    });
                }
                else
                {
                    return this.CreateError(
                        "ASNXER0331",
                        "A required EXTENDED-XER attribute is absent: "
                            + descriptor.Name,
                        false);
                }

                descriptorIndex = descriptorIndex + 1;
                continue;
            }

            if (this.Extended
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyAttributes) != null)
            {
                XerValueResult wildcard = this.DecodeAnyAttributesValue(
                    descriptor.Type,
                    this.GetWildcardAttributes(
                        wrapped.Attributes,
                        descriptors));
                if (!wildcard.Success || wildcard.Value == null)
                {
                    return wildcard;
                }

                fields.Add(new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = wildcard.Value
                });
                descriptorIndex = descriptorIndex + 1;
                continue;
            }

            if (this.Extended
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Untagged) != null
                && this.CanStartUntaggedValue(
                    descriptor.Type,
                    wrapped.Content,
                    contentIndex))
            {
                int untaggedCount = this.FindUntaggedContentCount(
                    descriptors,
                    descriptorIndex,
                    wrapped.Content,
                    contentIndex);
                XerValueResult untagged = this.DecodeUntaggedValue(
                    descriptor.Type,
                    wrapped.Content,
                    contentIndex,
                    untaggedCount,
                    descriptor.Name,
                    depth + 1);
                if (!untagged.Success || untagged.Value == null)
                {
                    return untagged;
                }

                fields.Add(new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = untagged.Value
                });
                contentIndex = contentIndex + untaggedCount;
                siblingIndex = siblingIndex + 1;
                descriptorIndex = descriptorIndex + 1;
                continue;
            }

            bool present = contentIndex < wrapped.Content.Length
                && this.IsElement(wrapped.Content[contentIndex])
                && (this.MatchesElementName(
                        descriptor.Type,
                        descriptor.Name,
                        wrapped.Content[contentIndex])
                    || (this.Extended
                        && this.FindInstruction(
                            descriptor.Type,
                            AsnInstructionKind.AnyElement) != null));
            if (present)
            {
                int childCount = this.GetElementTokenCount(
                    wrapped.Content,
                    contentIndex);
                if (childCount == 0)
                {
                    return this.CreateError(
                        "ASNXER0126",
                        "A BASIC-XER component element is incomplete.",
                        false);
                }

                XerValueResult decoded = this.DecodeValueAtDepth(
                    descriptor.Type,
                    wrapped.Content,
                    contentIndex,
                    childCount,
                    descriptor.Name,
                    depth + 1);
                if (!decoded.Success || decoded.Value == null)
                {
                    return decoded;
                }

                fields.Add(new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = decoded.Value
                });
                contentIndex = contentIndex + childCount;
                siblingIndex = siblingIndex + 1;
                descriptorIndex = descriptorIndex + 1;
            }
            else if (descriptor.Optional
                || descriptor.HasDefaultValue
                || descriptor.Extension)
            {
                fields.Add(new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = false
                });
                descriptorIndex = descriptorIndex + 1;
            }
            else if (contentIndex < wrapped.Content.Length
                && type.Extensible
                && this.IsElement(wrapped.Content[contentIndex])
                && this.FindDescriptor(
                    descriptors,
                    wrapped.Content[contentIndex].Name) == null)
            {
                XerValueResult preserved = this.PreserveUnknownField(
                    wrapped.Content,
                    contentIndex,
                    siblingIndex);
                if (!preserved.Success || preserved.Value == null)
                {
                    return preserved;
                }

                if (preserved.Value.UnknownFields.Length > 0)
                {
                    unknown.Add(preserved.Value.UnknownFields[0]);
                }
                contentIndex = contentIndex + preserved.ConsumedCount;
                siblingIndex = siblingIndex + 1;
            }
            else
            {
                XerXmlToken failureToken = contentIndex
                    < wrapped.Content.Length
                    ? wrapped.Content[contentIndex]
                    : sourceTokens[sourceOffset];
                return this.CreateTokenError(
                    "ASNXER0122",
                    "A required BASIC-XER component is absent or out of order: "
                        + descriptor.Name,
                    false,
                    failureToken);
            }
        }

        contentIndex = this.SkipIgnorable(wrapped.Content, contentIndex);
        while (contentIndex < wrapped.Content.Length)
        {
            if (!type.Extensible
                || !this.IsElement(wrapped.Content[contentIndex]))
            {
                return this.CreateTokenError(
                    "ASNXER0127",
                    "The BASIC-XER SEQUENCE contains unexpected content.",
                    false,
                    wrapped.Content[contentIndex]);
            }

            XerValueResult preserved = this.PreserveUnknownField(
                wrapped.Content,
                contentIndex,
                siblingIndex);
            if (!preserved.Success || preserved.Value == null)
            {
                return preserved;
            }

            if (preserved.Value.UnknownFields.Length > 0)
            {
                unknown.Add(preserved.Value.UnknownFields[0]);
            }
            contentIndex = contentIndex + preserved.ConsumedCount;
            siblingIndex = siblingIndex + 1;
            contentIndex = this.SkipIgnorable(
                wrapped.Content,
                contentIndex);
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = fields.ToArray(),
                UnknownFields = unknown.ToArray()
            },
            sourceTokens,
            sourceOffset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult DecodeSetContent(
        AsnTypeDescriptor type,
        XerXmlToken[] sourceTokens,
        int sourceOffset,
        XerWrappedValue wrapped,
        int depth)
    {
        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            descriptors.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The BASIC-XER collection exceeds the configured element limit.",
                true);
        }

        AsnRuntimeFieldValue[] fields =
            new AsnRuntimeFieldValue[descriptors.Length];
        bool[] seen = new bool[descriptors.Length];
        System.Collections.Generic.List<AsnUnknownField> unknown =
            new System.Collections.Generic.List<AsnUnknownField>();
        int wildcardAttributeIndex = this.Extended
            ? this.FindAnyAttributesDescriptorIndex(descriptors)
            : -1;
        if (wildcardAttributeIndex >= 0)
        {
            AsnFieldDescriptor wildcardDescriptor =
                descriptors[wildcardAttributeIndex];
            if (wildcardDescriptor.Type == null)
            {
                return this.CreateError(
                    "ASNXER0121",
                    "An ANY-ATTRIBUTES component has no resolved type.",
                    false);
            }

            XerValueResult wildcard = this.DecodeAnyAttributesValue(
                wildcardDescriptor.Type,
                this.GetWildcardAttributes(
                    wrapped.Attributes,
                    descriptors));
            if (!wildcard.Success || wildcard.Value == null)
            {
                return wildcard;
            }

            fields[wildcardAttributeIndex] = new AsnRuntimeFieldValue
            {
                Name = wildcardDescriptor.Name,
                Present = true,
                Value = wildcard.Value
            };
            seen[wildcardAttributeIndex] = true;
        }

        int attributeDescriptorIndex = 0;
        while (attributeDescriptorIndex < descriptors.Length)
        {
            AsnFieldDescriptor attributeDescriptor =
                descriptors[attributeDescriptorIndex];
            if (attributeDescriptor != null
                && attributeDescriptor.Type != null
                && this.Extended
                && this.FindInstruction(
                    attributeDescriptor.Type,
                    AsnInstructionKind.Attribute) != null)
            {
                XerValueResult attribute = this.DecodeAttributeValue(
                    attributeDescriptor,
                    wrapped.Attributes,
                    wrapped.NamespaceBindings);
                if (!attribute.Success)
                {
                    return attribute;
                }

                if (attribute.Value != null)
                {
                    fields[attributeDescriptorIndex] = new AsnRuntimeFieldValue
                    {
                        Name = attributeDescriptor.Name,
                        Present = true,
                        Value = attribute.Value
                    };
                    seen[attributeDescriptorIndex] = true;
                }
            }

            attributeDescriptorIndex = attributeDescriptorIndex + 1;
        }

        int contentIndex = 0;
        int siblingIndex = 0;
        while (contentIndex < wrapped.Content.Length)
        {
            contentIndex = this.SkipIgnorable(
                wrapped.Content,
                contentIndex);
            if (contentIndex >= wrapped.Content.Length)
            {
                break;
            }

            XerXmlToken child = wrapped.Content[contentIndex];
            if (!this.IsElement(child))
            {
                return this.CreateTokenError(
                    "ASNXER0128",
                    "The BASIC-XER SET contains non-element content.",
                    false,
                    child);
            }

            int descriptorIndex = this.FindDescriptorIndexForElement(
                descriptors,
                child,
                seen);
            if (descriptorIndex == -2)
            {
                return this.CreateError(
                    "ASNXER0332",
                    "More than one SET component has the same expanded element name.",
                    false);
            }
            if (descriptorIndex < 0)
            {
                int seenDescriptorIndex =
                    this.FindDescriptorIndexForElement(
                        descriptors,
                        child,
                        []);
                if (seenDescriptorIndex >= 0
                    && seen[seenDescriptorIndex])
                {
                    return this.CreateTokenError(
                        "ASNXER0130",
                        "The BASIC-XER SET contains a duplicate component.",
                        false,
                        child);
                }
            }

            if (descriptorIndex < 0 && this.Extended)
            {
                descriptorIndex = this.FindAnyElementDescriptorIndex(
                    descriptors,
                    seen);
            }

            if (descriptorIndex < 0 && this.Extended)
            {
                int untaggedIndex = this.FindUntaggedDescriptorIndex(
                    descriptors,
                    wrapped.Content,
                    contentIndex,
                    seen);
                if (untaggedIndex == -2)
                {
                    return this.CreateError(
                        "ASNXER0337",
                        "More than one untagged SET component matches the content.",
                        false);
                }

                if (untaggedIndex >= 0)
                {
                    AsnFieldDescriptor untaggedDescriptor =
                        descriptors[untaggedIndex];
                    if (untaggedDescriptor.Type == null)
                    {
                        return this.CreateError(
                            "ASNXER0121",
                            "An untagged SET component has no resolved type.",
                            false);
                    }

                    int untaggedCount = this.FindUntaggedItemCount(
                        untaggedDescriptor.Type,
                        wrapped.Content,
                        contentIndex);
                    XerValueResult untagged = this.DecodeUntaggedValue(
                        untaggedDescriptor.Type,
                        wrapped.Content,
                        contentIndex,
                        untaggedCount,
                        untaggedDescriptor.Name,
                        depth + 1);
                    if (!untagged.Success || untagged.Value == null)
                    {
                        return untagged;
                    }

                    fields[untaggedIndex] = new AsnRuntimeFieldValue
                    {
                        Name = untaggedDescriptor.Name,
                        Present = true,
                        Value = untagged.Value
                    };
                    seen[untaggedIndex] = true;
                    contentIndex = contentIndex + untaggedCount;
                    siblingIndex = siblingIndex + 1;
                    continue;
                }
            }

            if (descriptorIndex < 0)
            {
                if (!type.Extensible)
                {
                    return this.CreateTokenError(
                        "ASNXER0129",
                        "The BASIC-XER SET contains an unknown component.",
                        false,
                        child);
                }

                XerValueResult preserved = this.PreserveUnknownField(
                    wrapped.Content,
                    contentIndex,
                    siblingIndex);
                if (!preserved.Success || preserved.Value == null)
                {
                    return preserved;
                }

                if (preserved.Value.UnknownFields.Length > 0)
                {
                    unknown.Add(preserved.Value.UnknownFields[0]);
                }
                contentIndex = contentIndex + preserved.ConsumedCount;
            }
            else
            {
                if (seen[descriptorIndex])
                {
                    return this.CreateTokenError(
                        "ASNXER0130",
                        "The BASIC-XER SET contains a duplicate component.",
                        false,
                        child);
                }

                AsnFieldDescriptor descriptor = descriptors[descriptorIndex];
                if (descriptor.Type == null)
                {
                    return this.CreateError(
                        "ASNXER0121",
                        "A BASIC-XER component has no resolved type.",
                        false);
                }

                int childCount = this.GetElementTokenCount(
                    wrapped.Content,
                    contentIndex);
                if (childCount == 0)
                {
                    return this.CreateError(
                        "ASNXER0126",
                        "A BASIC-XER component element is incomplete.",
                        false);
                }

                XerValueResult decoded = this.DecodeValueAtDepth(
                    descriptor.Type,
                    wrapped.Content,
                    contentIndex,
                    childCount,
                    descriptor.Name,
                    depth + 1);
                if (!decoded.Success || decoded.Value == null)
                {
                    return decoded;
                }

                fields[descriptorIndex] = new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = true,
                    Value = decoded.Value
                };
                seen[descriptorIndex] = true;
                contentIndex = contentIndex + childCount;
            }

            siblingIndex = siblingIndex + 1;
        }

        int index = 0;
        while (index < descriptors.Length)
        {
            if (!seen[index])
            {
                AsnFieldDescriptor descriptor = descriptors[index];
                if (!descriptor.Optional
                    && !descriptor.HasDefaultValue
                    && !descriptor.Extension)
                {
                    return this.CreateError(
                        "ASNXER0122",
                        "A required BASIC-XER component is absent: "
                            + descriptor.Name,
                        false);
                }

                fields[index] = new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = false
                };
            }

            index = index + 1;
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Set,
                Fields = fields,
                UnknownFields = unknown.ToArray()
            },
            sourceTokens,
            sourceOffset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeChoice(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        if (value.Kind != AsnRuntimeValueKind.Choice)
        {
            return this.CreateError(
                "ASNXER0131",
                "The runtime value does not match a BASIC-XER CHOICE.",
                false);
        }

        AsnRuntimeFieldValue? selection = null;
        int selectedCount = 0;
        int index = 0;
        while (index < value.Fields.Length)
        {
            AsnRuntimeFieldValue field = value.Fields[index];
            if (field != null && field.Present)
            {
                selection = field;
                selectedCount = selectedCount + 1;
            }

            index = index + 1;
        }

        if (selectedCount != 1 || selection == null || selection.Value == null)
        {
            return this.CreateError(
                "ASNXER0132",
                "A BASIC-XER CHOICE must select exactly one valued alternative.",
                false);
        }

        AsnFieldDescriptor? descriptor = this.FindDescriptor(
            this.GetAllFields(type),
            selection.Name);
        if (descriptor == null || descriptor.Type == null)
        {
            return this.CreateError(
                "ASNXER0133",
                "The selected BASIC-XER CHOICE alternative is undefined.",
                false);
        }

        XerValueResult encoded = this.EncodeValueAtDepth(
            descriptor.Type,
            selection.Value,
            descriptor.Name,
            depth + 1);
        if (!encoded.Success)
        {
            return encoded;
        }

        if (this.Extended
            && this.FindInstruction(
                descriptor.Type,
                AsnInstructionKind.Untagged) != null)
        {
            encoded = this.RemoveUntaggedWrapper(
                descriptor.Type,
                selection.Value,
                encoded);
            if (!encoded.Success)
            {
                return encoded;
            }
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        this.AppendTokens(content, encoded.Tokens);
        return this.WrapEncoding(value, elementName, content);
    }

    internal XerValueResult EncodeChoiceWithType(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        XerChoiceSelection selection = this.GetChoiceSelection(type, value);
        if (!selection.Success
            || selection.Descriptor == null
            || selection.Descriptor.Type == null
            || selection.Value == null)
        {
            return this.CreateError(
                "ASNXER0353",
                selection.ErrorMessage,
                false);
        }

        XerValueResult alternative = this.EncodeValueAtDepth(
            selection.Descriptor.Type,
            selection.Value,
            selection.Descriptor.Name,
            depth + 1);
        if (!alternative.Success)
        {
            return alternative;
        }

        XerXmlAttribute[] transferred = alternative.Tokens.Length > 0
            ? alternative.Tokens[0].Attributes
            : [];
        XerValueResult removed = this.RemoveAlternativeWrapper(alternative);
        if (!removed.Success)
        {
            return removed;
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        this.AppendTokens(content, removed.Tokens);
        XerValueResult encoded = this.WrapEncoding(value, elementName, content);
        if (!encoded.Success || encoded.Tokens.Length == 0)
        {
            return encoded;
        }

        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < transferred.Length)
        {
            attributes.Add(transferred[index]);
            index = index + 1;
        }

        if (selection.Index > 0)
        {
            XerValueResult identified = this.AddTypeIdentification(
                attributes,
                selection.Descriptor.Type,
                selection.Descriptor.Name);
            if (!identified.Success)
            {
                return identified;
            }
        }

        encoded.Tokens[0].Attributes = attributes.ToArray();
        return encoded;
    }

    internal XerValueResult DecodeChoiceWithType(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        if (descriptors.Length == 0)
        {
            return this.CreateError(
                "ASNXER0354",
                "USE-TYPE requires at least one CHOICE alternative.",
                false);
        }

        string identification = this.GetTypeIdentification(
            wrapped.Attributes);
        int selectedIndex = identification.Length == 0
            ? 0
            : this.FindTypeIdentificationIndex(
                descriptors,
                identification,
                wrapped.NamespaceBindings);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        AsnFieldDescriptor descriptor = descriptors[selectedIndex];
        if (descriptor.Type == null)
        {
            return this.CreateError(
                "ASNXER0121",
                "A USE-TYPE alternative has no resolved type.",
                false);
        }

        XerXmlAttribute[] transferred = this.RemoveControlAttributes(
            wrapped.Attributes);
        XerValueResult decoded = this.DecodeAlternativeContent(
            descriptor.Type,
            descriptor.Name,
            wrapped.Content,
            transferred,
            wrapped.NamespaceBindings,
            depth + 1);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        return this.CreateDecodedValue(
            this.CreateChoiceRuntimeValue(descriptor, decoded.Value),
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeChoiceWithUnion(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        XerChoiceSelection selection = this.GetChoiceSelection(type, value);
        if (!selection.Success
            || selection.Descriptor == null
            || selection.Descriptor.Type == null
            || selection.Value == null)
        {
            return this.CreateError(
                "ASNXER0355",
                selection.ErrorMessage,
                false);
        }

        XerCharacterValueResult character = this.EncodeCharacterValue(
            selection.Descriptor.Type,
            selection.Value);
        if (!character.Success)
        {
            return this.CreateError(
                character.ErrorCode,
                character.ErrorMessage,
                character.LimitExceeded);
        }

        XerValueResult encoded = this.WrapTextEncoding(
            value,
            elementName,
            character.Text);
        if (!encoded.Success || encoded.Tokens.Length == 0)
        {
            return encoded;
        }

        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        if (character.NamespaceUri.Length > 0)
        {
            attributes.Add(this.CreateNamespaceDeclaration(
                character.Prefix,
                character.NamespaceUri));
        }

        if (selection.Index > 0
            && !this.HasEmptyNameInstruction(selection.Descriptor.Type))
        {
            XerValueResult identified = this.AddTypeIdentification(
                attributes,
                selection.Descriptor.Type,
                selection.Descriptor.Name);
            if (!identified.Success)
            {
                return identified;
            }
        }

        encoded.Tokens[0].Attributes = attributes.ToArray();
        return encoded;
    }

    internal bool HasEmptyNameInstruction(AsnTypeDescriptor type)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.Name);
        return instruction != null
            && instruction.Arguments.Length == 2
            && instruction.Arguments[0] == "AS"
            && instruction.Arguments[1] == "\"\"";
    }

    internal XerValueResult DecodeChoiceWithUnion(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasCharacterWrapperContent(wrapped))
        {
            return this.CreateError(
                "ASNXER0356",
                "USE-UNION requires character content.",
                false);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        string identification = this.GetTypeIdentification(
            wrapped.Attributes);
        int selectedIndex = identification.Length == 0
            ? -1
            : this.FindTypeIdentificationIndex(
                descriptors,
                identification,
                wrapped.NamespaceBindings);
        if (identification.Length > 0 && selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        int index = selectedIndex < 0 ? 0 : selectedIndex;
        int limit = selectedIndex < 0 ? descriptors.Length : selectedIndex + 1;
        while (index < limit)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor.Type != null)
            {
                XerCharacterValueResult decoded =
                    this.DecodeCharacterValueWithBindings(
                        descriptor.Type,
                        text,
                        wrapped.NamespaceBindings);
                if (decoded.Success && decoded.Value != null)
                {
                    return this.CreateDecodedValue(
                        this.CreateChoiceRuntimeValue(
                            descriptor,
                            decoded.Value),
                        tokens,
                        offset,
                        wrapped.ConsumedCount);
                }
            }

            index = index + 1;
        }

        return this.CreateError(
            "ASNXER0357",
            "The USE-UNION content does not match an alternative.",
            false);
    }

    internal XerChoiceSelection GetChoiceSelection(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        if (value.Kind != AsnRuntimeValueKind.Choice)
        {
            return new XerChoiceSelection
            {
                ErrorMessage = "The runtime value is not a CHOICE."
            };
        }

        AsnRuntimeFieldValue? selected = null;
        int selectedCount = 0;
        int index = 0;
        while (index < value.Fields.Length)
        {
            AsnRuntimeFieldValue field = value.Fields[index];
            if (field != null && field.Present && field.Value != null)
            {
                selected = field;
                selectedCount = selectedCount + 1;
            }

            index = index + 1;
        }

        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        int descriptorIndex = selected == null
            ? -1
            : this.FindDescriptorIndex(descriptors, selected.Name);
        if (selectedCount != 1
            || selected == null
            || selected.Value == null
            || descriptorIndex < 0)
        {
            return new XerChoiceSelection
            {
                ErrorMessage = "The CHOICE must select exactly one defined alternative."
            };
        }

        return new XerChoiceSelection
        {
            Success = true,
            Descriptor = descriptors[descriptorIndex],
            Value = selected.Value,
            Index = descriptorIndex
        };
    }

    internal XerValueResult RemoveAlternativeWrapper(XerValueResult encoded)
    {
        if (encoded.Tokens.Length == 1
            && encoded.Tokens[0].Kind == XerXmlTokenKind.EmptyElement)
        {
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Tokens = []
            };
        }

        if (encoded.Tokens.Length < 2
            || encoded.Tokens[0].Kind != XerXmlTokenKind.StartElement
            || encoded.Tokens[encoded.Tokens.Length - 1].Kind
                != XerXmlTokenKind.EndElement)
        {
            return this.CreateError(
                "ASNXER0358",
                "The alternative encoding has no removable wrapper.",
                false);
        }

        XerXmlToken[] content = new XerXmlToken[encoded.Tokens.Length - 2];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = encoded.Tokens[index + 1];
            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = content
        };
    }

    internal XerValueResult AddTypeIdentification(
        System.Collections.Generic.List<XerXmlAttribute> attributes,
        AsnTypeDescriptor alternative,
        string defaultName)
    {
        XerExtendedName identity = this.ResolveExtendedName(
            alternative,
            defaultName);
        if (!identity.Success)
        {
            return this.CreateError(
                "ASNXER0320",
                identity.ErrorMessage,
                false);
        }

        if (identity.NamespaceUri.Length > 0)
        {
            XerValueResult declaration = this.AddNamespaceDeclarationEncoding(
                attributes,
                identity.Prefix.Length == 0 ? "ns1" : identity.Prefix,
                identity.NamespaceUri);
            if (!declaration.Success)
            {
                return declaration;
            }
        }

        attributes.Add(new XerXmlAttribute
        {
            Name = "type",
            Prefix = "asn1",
            LocalName = "type",
            NamespaceUri = ControlNamespace,
            Value = identity.NamespaceUri.Length == 0
                ? identity.LocalName
                : (identity.Prefix.Length == 0 ? "ns1" : identity.Prefix)
                    + ":" + identity.LocalName
        });
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal string GetTypeIdentification(XerXmlAttribute[] attributes)
    {
        string result = "";
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration
                && attribute.NamespaceUri == ControlNamespace
                && attribute.LocalName == "type")
            {
                if (result.Length > 0)
                {
                    return "";
                }

                result = attribute.Value;
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindTypeIdentificationIndex(
        AsnFieldDescriptor[] descriptors,
        string identification,
        XerXmlNamespaceBinding[] bindings)
    {
        int separator = identification.IndexOf(':');
        if (separator != identification.LastIndexOf(':'))
        {
            return -1;
        }

        string prefix = separator < 0
            ? ""
            : identification.Substring(0, separator);
        string localName = separator < 0
            ? identification
            : identification.Substring(separator + 1);
        XerXmlNamespaceBinding binding = new XerXmlNamespaceContext(bindings)
            .FindBinding(prefix);
        string namespaceUri = prefix.Length == 0
            ? ""
            : binding.Found ? binding.NamespaceUri : "";
        if (!this.Characters.IsValidNcName(localName)
            || prefix.Length > 0 && namespaceUri.Length == 0)
        {
            return -1;
        }

        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor != null && descriptor.Type != null)
            {
                XerExtendedName identity = this.ResolveExtendedName(
                    descriptor.Type,
                    descriptor.Name);
                if (identity.Success
                    && identity.LocalName == localName
                    && identity.NamespaceUri == namespaceUri)
                {
                    if (result >= 0)
                    {
                        return -1;
                    }

                    result = index;
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal XerXmlAttribute[] RemoveControlAttributes(
        XerXmlAttribute[] attributes)
    {
        System.Collections.Generic.List<XerXmlAttribute> result =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration
                && (attribute.NamespaceUri != ControlNamespace
                    || attribute.LocalName != "type"))
            {
                result.Add(attribute);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XerValueResult DecodeAlternativeContent(
        AsnTypeDescriptor type,
        string defaultName,
        XerXmlToken[] content,
        XerXmlAttribute[] attributes,
        XerXmlNamespaceBinding[] bindings,
        int depth)
    {
        XerExtendedName identity = this.ResolveExtendedName(type, defaultName);
        if (!identity.Success)
        {
            return this.CreateError("ASNXER0320", identity.ErrorMessage, false);
        }

        XerXmlToken[] wrapped = new XerXmlToken[content.Length + 2];
        wrapped[0] = new XerXmlToken
        {
            Kind = XerXmlTokenKind.StartElement,
            Attributes = attributes,
            NamespaceBindings = bindings
        };
        this.SetTokenIdentity(wrapped[0], identity);
        int index = 0;
        while (index < content.Length)
        {
            wrapped[index + 1] = content[index];
            index = index + 1;
        }

        wrapped[wrapped.Length - 1] = new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement
        };
        this.SetTokenIdentity(wrapped[wrapped.Length - 1], identity);
        return this.DecodeValueAtDepth(
            type,
            wrapped,
            0,
            wrapped.Length,
            defaultName,
            depth);
    }

    internal AsnRuntimeValue CreateChoiceRuntimeValue(
        AsnFieldDescriptor descriptor,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [new AsnRuntimeFieldValue
            {
                Name = descriptor.Name,
                Present = true,
                Value = value
            }]
        };
    }

    internal XerCharacterValueResult DecodeCharacterValueWithBindings(
        AsnTypeDescriptor type,
        string text,
        XerXmlNamespaceBinding[] bindings)
    {
        return this.FindInstruction(type, AsnInstructionKind.UseQName) == null
            ? this.DecodeCharacterValue(type, text)
            : this.DecodeQNameCharacter(type, text, bindings);
    }

    internal XerValueResult DecodeChoice(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = this.Extended;
        wrappers.AllowWrapperNamespace = this.Extended;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes))
        {
            return this.CreateError(
                "ASNXER0316",
                "The EXTENDED-XER CHOICE wrapper has an unsupported attribute.",
                false);
        }

        int contentIndex = this.SkipIgnorable(wrapped.Content, 0);
        if (contentIndex >= wrapped.Content.Length)
        {
            return this.CreateError(
                "ASNXER0134",
                "A BASIC-XER CHOICE requires one alternative element.",
                false);
        }

        AsnFieldDescriptor[] descriptors = this.GetAllFields(type);
        int untaggedIndex = this.FindUntaggedDescriptorIndex(
            descriptors,
            wrapped.Content,
            contentIndex,
            []);
        if (untaggedIndex == -2)
        {
            return this.CreateError(
                "ASNXER0336",
                "More than one untagged CHOICE alternative matches the content.",
                false);
        }

        if (untaggedIndex >= 0)
        {
            AsnFieldDescriptor untaggedDescriptor = descriptors[untaggedIndex];
            if (untaggedDescriptor.Type == null)
            {
                return this.CreateError(
                    "ASNXER0121",
                    "An untagged CHOICE alternative has no resolved type.",
                    false);
            }

            int untaggedCount = wrapped.Content.Length - contentIndex;
            XerValueResult untagged = this.DecodeUntaggedValue(
                untaggedDescriptor.Type,
                wrapped.Content,
                contentIndex,
                untaggedCount,
                untaggedDescriptor.Name,
                depth + 1);
            if (!untagged.Success || untagged.Value == null)
            {
                return untagged;
            }

            return this.CreateDecodedValue(
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields = [new AsnRuntimeFieldValue
                    {
                        Name = untaggedDescriptor.Name,
                        Present = true,
                        Value = untagged.Value
                    }]
                },
                tokens,
                offset,
                wrapped.ConsumedCount);
        }

        if (!this.IsElement(wrapped.Content[contentIndex]))
        {
            return this.CreateError(
                "ASNXER0134",
                "A BASIC-XER CHOICE requires one alternative element.",
                false);
        }

        XerXmlToken child = wrapped.Content[contentIndex];
        int descriptorIndex = this.FindDescriptorIndexForElement(
            descriptors,
            child,
            []);
        if (descriptorIndex == -2)
        {
            return this.CreateError(
                "ASNXER0333",
                "More than one CHOICE alternative has the same expanded element name.",
                false);
        }

        AsnFieldDescriptor? descriptor = descriptorIndex < 0
            ? null
            : descriptors[descriptorIndex];
        if (descriptor == null && this.Extended)
        {
            int wildcardIndex = this.FindAnyElementDescriptorIndex(
                descriptors,
                []);
            if (wildcardIndex >= 0)
            {
                descriptor = descriptors[wildcardIndex];
            }
        }

        if (descriptor == null || descriptor.Type == null)
        {
            return this.CreateError(
                "ASNXER0133",
                "The BASIC-XER CHOICE alternative is undefined.",
                false);
        }

        int childCount = this.GetElementTokenCount(
            wrapped.Content,
            contentIndex);
        XerValueResult decoded = this.DecodeValueAtDepth(
            descriptor.Type,
            wrapped.Content,
            contentIndex,
            childCount,
            descriptor.Name,
            depth + 1);
        if (!decoded.Success || decoded.Value == null)
        {
            return decoded;
        }

        contentIndex = this.SkipIgnorable(
            wrapped.Content,
            contentIndex + childCount);
        if (contentIndex != wrapped.Content.Length)
        {
            return this.CreateError(
                "ASNXER0135",
                "A BASIC-XER CHOICE contains more than one alternative.",
                false);
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields =
                [
                    new AsnRuntimeFieldValue
                    {
                        Name = descriptor.Name,
                        Present = true,
                        Value = decoded.Value
                    }
                ]
            },
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeCollectionOf(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.SetOf
            ? AsnRuntimeValueKind.SetOf
            : AsnRuntimeValueKind.SequenceOf;
        if (value.Kind != expected || type.ElementType == null)
        {
            return this.CreateError(
                "ASNXER0136",
                "The BASIC-XER collection-of value or element type is invalid.",
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            value.Items.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The BASIC-XER collection exceeds the configured element limit.",
                true);
        }

        XerNameResult itemName = this.Names.GetElementName(type.ElementType);
        if (!itemName.Success)
        {
            return this.CopyNameFailure(itemName);
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        XerXmlToken[][] encodings = new XerXmlToken[value.Items.Length][];
        int index = 0;
        while (index < value.Items.Length)
        {
            XerValueResult encoded = this.EncodeValueAtDepth(
                type.ElementType,
                value.Items[index],
                itemName.Name,
                depth + 1);
            if (!encoded.Success)
            {
                return encoded;
            }

            if (this.Extended
                && this.FindInstruction(
                    type.ElementType,
                    AsnInstructionKind.Untagged) != null)
            {
                encoded = this.RemoveUntaggedWrapper(
                    type.ElementType,
                    value.Items[index],
                    encoded);
                if (!encoded.Success)
                {
                    return encoded;
                }
            }

            encodings[index] = encoded.Tokens;
            index = index + 1;
        }

        int[] order = this.GetTokenEncodingOrder(
            encodings,
            this.Canonical && type.Kind == AsnTypeKind.SetOf);
        index = 0;
        while (index < order.Length)
        {
            this.AppendTokens(content, encodings[order[index]]);
            index = index + 1;
        }

        return this.WrapEncoding(value, elementName, content);
    }

    internal XerValueResult DecodeCollectionOf(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        if (type.ElementType == null)
        {
            return this.CreateError(
                "ASNXER0136",
                "The BASIC-XER collection-of element type is missing.",
                false);
        }

        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = this.Extended;
        wrappers.AllowWrapperNamespace = this.Extended;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes))
        {
            return this.CreateError(
                "ASNXER0316",
                "The EXTENDED-XER collection wrapper has an unsupported attribute.",
                false);
        }

        XerNameResult itemName = this.Names.GetElementName(type.ElementType);
        if (!itemName.Success)
        {
            return this.CopyNameFailure(itemName);
        }

        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        int contentIndex = 0;
        while (contentIndex < wrapped.Content.Length)
        {
            contentIndex = this.SkipIgnorable(
                wrapped.Content,
                contentIndex);
            if (contentIndex >= wrapped.Content.Length)
            {
                break;
            }

            bool untaggedItem = this.Extended
                && this.FindInstruction(
                    type.ElementType,
                    AsnInstructionKind.Untagged) != null;
            if (untaggedItem
                && this.CanStartUntaggedValue(
                    type.ElementType,
                    wrapped.Content,
                    contentIndex))
            {
                int itemCount = this.FindUntaggedItemCount(
                    type.ElementType,
                    wrapped.Content,
                    contentIndex);
                XerValueResult untagged = this.DecodeUntaggedValue(
                    type.ElementType,
                    wrapped.Content,
                    contentIndex,
                    itemCount,
                    itemName.Name,
                    depth + 1);
                if (!untagged.Success || untagged.Value == null)
                {
                    return untagged;
                }

                items.Add(untagged.Value);
                contentIndex = contentIndex + itemCount;
                continue;
            }

            bool wildcardItem = this.Extended
                && this.FindInstruction(
                    type.ElementType,
                    AsnInstructionKind.AnyElement) != null;
            if (!this.IsElement(wrapped.Content[contentIndex])
                || (!wildcardItem
                    && !this.MatchesElementName(
                        type.ElementType,
                        itemName.Name,
                        wrapped.Content[contentIndex])))
            {
                return this.CreateError(
                    "ASNXER0137",
                    "A BASIC-XER collection-of item has the wrong wrapper.",
                    false);
            }

            if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
                items.Count + 1))
            {
                return this.CreateError(
                    "ASNR0011",
                    "The BASIC-XER collection exceeds the configured element limit.",
                    true);
            }

            int childCount = this.GetElementTokenCount(
                wrapped.Content,
                contentIndex);
            XerValueResult decoded = this.DecodeValueAtDepth(
                type.ElementType,
                wrapped.Content,
                contentIndex,
                childCount,
                itemName.Name,
                depth + 1);
            if (!decoded.Success || decoded.Value == null)
            {
                return decoded;
            }

            items.Add(decoded.Value);
            contentIndex = contentIndex + childCount;
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.SetOf
                    ? AsnRuntimeValueKind.SetOf
                    : AsnRuntimeValueKind.SequenceOf,
                Items = items.ToArray()
            },
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeUseful(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        AsnRuntimeValueKind expected = this.GetUsefulRuntimeKind(type.Kind);
        AsnTypeDescriptor? schema = new XerUsefulTypeSchemas()
            .CreateAssociatedSchema(type.Kind);
        if (schema == null || value.Kind != expected)
        {
            return this.CreateError(
                "ASNXER0138",
                "The BASIC-XER useful type or runtime value is invalid.",
                false);
        }

        XerValueResult encoded = this.EncodeCollection(
            schema,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = value.Fields,
                UnknownFields = value.UnknownFields
            },
            elementName,
            depth);
        if (encoded.Success)
        {
            encoded.Value = value;
        }

        return encoded;
    }

    internal XerValueResult DecodeUseful(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        AsnTypeDescriptor? schema = new XerUsefulTypeSchemas()
            .CreateAssociatedSchema(type.Kind);
        if (schema == null)
        {
            return this.CreateError(
                "ASNXER0138",
                "The requested type has no BASIC-XER associated type.",
                false);
        }

        XerValueResult decoded = this.DecodeCollection(
            schema,
            tokens,
            offset,
            count,
            elementName,
            depth);
        if (decoded.Success && decoded.Value != null)
        {
            decoded.Value.Kind = this.GetUsefulRuntimeKind(type.Kind);
        }

        return decoded;
    }

    internal XerValueResult EncodeOpenType(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        AsnOpenTypeValue? open = value.OpenTypeValue;
        if (value.Kind != AsnRuntimeValueKind.OpenType
            || open == null
            || !open.IsValid())
        {
            return this.CreateError(
                "ASNXER0139",
                "The BASIC-XER open runtime value is inconsistent.",
                false);
        }

        if (this.Canonical && open.Unresolved)
        {
            return this.CreateError(
                "ASNXER0201",
                "Canonical XER requires a resolved typed open value.",
                false);
        }

        XerXmlToken[] payload = [];
        if (open.Resolved && open.Value != null)
        {
            AsnTypeDescriptor? resolved = this.ResolveOpenTypeForEncoding(
                type,
                open);
            if (resolved != null)
            {
                XerNameResult name = this.Names.GetElementName(resolved);
                if (!name.Success)
                {
                    return this.CopyNameFailure(name);
                }

                XerValueResult encoded = this.EncodeValueAtDepth(
                    resolved,
                    open.Value,
                    name.Name,
                    depth + 1);
                if (!encoded.Success)
                {
                    return encoded;
                }

                payload = encoded.Tokens;
            }
            else if (type.ObjectTables.Length > 0 || this.Canonical)
            {
                return this.CreateError(
                    "ASNXER0147",
                    "The resolved BASIC-XER open value does not select one table entry.",
                    false);
            }
        }

        if (payload.Length == 0)
        {
            XerValueResult preserved = this.ReadPreservedOpenType(open);
            if (!preserved.Success)
            {
                return preserved;
            }

            payload = preserved.Tokens;
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        this.AppendTokens(content, payload);
        return this.WrapEncoding(value, elementName, content);
    }

    internal XerValueResult DecodeOpenType(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = this.Extended;
        wrappers.AllowWrapperNamespace = this.Extended;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        int contentIndex = this.SkipIgnorable(wrapped.Content, 0);
        if (contentIndex >= wrapped.Content.Length
            || !this.IsElement(wrapped.Content[contentIndex]))
        {
            return this.CreateError(
                "ASNXER0140",
                "A BASIC-XER open type requires one typed value element.",
                false);
        }

        int childCount = this.GetElementTokenCount(
            wrapped.Content,
            contentIndex);
        int trailing = this.SkipIgnorable(
            wrapped.Content,
            contentIndex + childCount);
        if (childCount == 0 || trailing != wrapped.Content.Length)
        {
            return this.CreateError(
                "ASNXER0141",
                "A BASIC-XER open type must contain exactly one typed value.",
                false);
        }

        int openMatches = this.CountOpenTypeMatches(
            type,
            wrapped.Content[contentIndex].Name);
        if (openMatches > 1)
        {
            return this.CreateError(
                "ASNXER0148",
                "More than one open-type table entry matches the BASIC-XER element.",
                false);
        }

        AsnTypeDescriptor? resolved = this.ResolveOpenTypeForDecoding(
            type,
            wrapped.Content[contentIndex].Name);
        if (resolved == null
            && this.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateTokenError(
                "ASNXER0154",
                "No open-type table entry resolves the BASIC-XER value element.",
                false,
                wrapped.Content[contentIndex]);
        }

        bool skipUnresolved = resolved == null
            && this.UnknownDataPolicy == AsnUnknownDataPolicy.Skip;
        byte[] original = skipUnresolved
            ? []
            : this.SerializeTokenRange(
                wrapped.Content,
                contentIndex,
                childCount);
        if (!skipUnresolved
            && (original.Length == 0
                || !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                    original.Length)))
        {
            return this.CreateError(
                original.Length == 0 ? "ASNXER0142" : "ASNR0010",
                original.Length == 0
                    ? "The BASIC-XER open value could not be retained."
                    : "The BASIC-XER open value exceeds the configured byte limit.",
                original.Length != 0);
        }

        AsnOpenTypeValue open;
        if (resolved == null)
        {
            open = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = this.GetActiveEncodingRules(),
                HasOriginalData = this.UnknownDataPolicy
                    == AsnUnknownDataPolicy.Preserve,
                OriginalData = this.UnknownDataPolicy
                    == AsnUnknownDataPolicy.Preserve
                    ? original
                    : [],
                HasEncodedLocation = this.UnknownDataPolicy
                    == AsnUnknownDataPolicy.Preserve,
                EncodedOffset = this.GetOpenContentOffset(
                    tokens,
                    offset,
                    wrapped),
                EncodedLength = this.UnknownDataPolicy
                    == AsnUnknownDataPolicy.Preserve
                    ? original.Length
                    : 0
            };
        }
        else
        {
            XerNameResult resolvedName = this.Names.GetElementName(resolved);
            if (!resolvedName.Success)
            {
                return this.CopyNameFailure(resolvedName);
            }

            XerValueResult decoded = this.DecodeValueAtDepth(
                resolved,
                wrapped.Content,
                contentIndex,
                childCount,
                resolvedName.Name,
                depth + 1);
            if (!decoded.Success || decoded.Value == null)
            {
                return decoded;
            }

            string identity = resolved.StableId.Length > 0
                ? resolved.StableId
                : resolved.Name;
            open = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = identity,
                TypeName = resolved.Name,
                Value = decoded.Value,
                HasEncodingRules = true,
                EncodingRules = this.GetActiveEncodingRules(),
                HasOriginalData = true,
                OriginalData = original,
                HasEncodedLocation = true,
                EncodedOffset = this.GetOpenContentOffset(
                    tokens,
                    offset,
                    wrapped),
                EncodedLength = original.Length
            };
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = open
            },
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal int GetOpenContentOffset(
        XerXmlToken[] tokens,
        int offset,
        XerWrappedValue wrapped)
    {
        if (wrapped.Content.Length > 0)
        {
            return wrapped.Content[0].ByteOffset;
        }

        return tokens[offset].ByteOffset;
    }

    internal XerValueResult ReadPreservedOpenType(AsnOpenTypeValue open)
    {
        if (!new OpaquePayloadPolicy().CanReuseOpenType(
                open,
                this.GetActiveEncodingRules())
            || !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
                open.OriginalData.Length))
        {
            return this.CreateError(
                "ASNXER0143",
                "The open value has no compatible preserved BASIC-XER payload.",
                false);
        }

        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = open.OriginalData,
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length == 0
            || read.Tokens[0].Kind == XerXmlTokenKind.XmlDeclaration)
        {
            return this.CreateError(
                "ASNXER0144",
                "The preserved BASIC-XER open payload is not one XML element.",
                false);
        }

        int childCount = this.GetElementTokenCount(read.Tokens, 0);
        int trailing = this.SkipIgnorable(read.Tokens, childCount);
        if (childCount == 0 || trailing != read.Tokens.Length)
        {
            return this.CreateError(
                "ASNXER0144",
                "The preserved BASIC-XER open payload is not one XML element.",
                false);
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = read.Tokens,
            ConsumedCount = read.Tokens.Length
        };
    }

    internal XerValueResult EncodeUnknownField(AsnUnknownField field)
    {
        if (field == null
            || !new OpaquePayloadPolicy().CanReuseUnknownField(
                field,
                this.GetActiveEncodingRules())
            || !field.HasTextData
            || field.XmlAttribute)
        {
            return this.CreateError(
                "ASNXER0145",
                "An unknown field has no valid preserved BASIC-XER content.",
                false);
        }

        byte[] data = System.Text.Encoding.UTF8.GetBytes(field.TextData);
        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = data,
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length == 0
            || read.Tokens[0].Kind == XerXmlTokenKind.XmlDeclaration
            || !this.MatchesUnknownFieldIdentity(
                field,
                read.Tokens[0]))
        {
            return this.CreateError(
                "ASNXER0146",
                "The preserved BASIC-XER field does not match its name.",
                false);
        }

        int childCount = this.GetElementTokenCount(read.Tokens, 0);
        if (childCount == 0
            || this.SkipIgnorable(read.Tokens, childCount)
                != read.Tokens.Length)
        {
            return this.CreateError(
                "ASNXER0146",
                "The preserved BASIC-XER field must contain one element.",
                false);
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = read.Tokens,
            ConsumedCount = read.Tokens.Length
        };
    }

    internal XerValueResult PreserveUnknownField(
        XerXmlToken[] tokens,
        int offset,
        int siblingIndex)
    {
        int childCount = this.GetElementTokenCount(tokens, offset);
        if (childCount == 0)
        {
            return this.CreateError(
                "ASNXER0126",
                "An unknown BASIC-XER component is incomplete.",
                false);
        }

        if (this.UnknownDataPolicy == AsnUnknownDataPolicy.Reject)
        {
            return this.CreateError(
                "ASNXER0149",
                "The XER input contains an unknown extension element.",
                false);
        }

        if (this.UnknownDataPolicy == AsnUnknownDataPolicy.Skip)
        {
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = new AsnRuntimeValue(),
                ConsumedCount = childCount
            };
        }

        byte[] data = this.SerializeWildcardElement(
            tokens,
            offset,
            childCount);
        if (data.Length == 0)
        {
            return this.CreateError(
                "ASNXER0142",
                "The unknown BASIC-XER component could not be retained.",
                false);
        }

        AsnUnknownField field = new AsnUnknownField
        {
            Name = tokens[offset].LocalName.Length == 0
                ? tokens[offset].Name
                : tokens[offset].LocalName,
            XmlPrefix = tokens[offset].Prefix,
            XmlNamespaceUri = tokens[offset].NamespaceUri,
            HasEncodedIndex = true,
            EncodedIndex = siblingIndex,
            HasEncodedLocation = true,
            EncodedOffset = tokens[offset].ByteOffset,
            EncodedLength = this.GetTokenRangeByteLength(
                tokens,
                offset,
                childCount),
            EncodingRules = this.GetActiveEncodingRules(),
            HasEncodingRules = true,
            HasTextData = true,
            TextData = System.Text.Encoding.UTF8.GetString(data)
        };
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                UnknownFields = [field]
            },
            ConsumedCount = childCount
        };
    }

    internal XerValueResult AddPreservedUnknownAttributes(
        AsnUnknownField[] fields,
        System.Collections.Generic.List<XerXmlAttribute> destination)
    {
        int index = 0;
        while (index < fields.Length)
        {
            AsnUnknownField field = fields[index];
            if (field != null && field.XmlAttribute)
            {
                XerValueResult added = this.AddPreservedUnknownAttribute(
                    field,
                    destination);
                if (!added.Success)
                {
                    return added;
                }
            }

            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal XerValueResult AddPreservedUnknownAttribute(
        AsnUnknownField field,
        System.Collections.Generic.List<XerXmlAttribute> destination)
    {
        if (!new OpaquePayloadPolicy().CanReuseUnknownField(
                field,
                this.GetActiveEncodingRules())
            || !field.HasTextData
            || !field.XmlAttribute)
        {
            return this.CreateError(
                "ASNXER0150",
                "An unknown attribute has no compatible preserved XER fragment.",
                false);
        }

        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(field.TextData),
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length != 1
            || read.Tokens[0].Kind != XerXmlTokenKind.EmptyElement)
        {
            return this.CreateError(
                "ASNXER0151",
                "The preserved XER attribute fragment is malformed.",
                false);
        }

        XerXmlAttribute? preserved = null;
        int attributeIndex = 0;
        while (attributeIndex < read.Tokens[0].Attributes.Length)
        {
            XerXmlAttribute attribute =
                read.Tokens[0].Attributes[attributeIndex];
            if (!attribute.NamespaceDeclaration)
            {
                if (preserved != null
                    || attribute.LocalName != field.Name
                    || attribute.NamespaceUri != field.XmlNamespaceUri)
                {
                    return this.CreateError(
                        "ASNXER0152",
                        "The preserved XER attribute fragment does not match its expanded name.",
                        false);
                }

                preserved = attribute;
            }

            attributeIndex = attributeIndex + 1;
        }

        if (preserved == null)
        {
            return this.CreateError(
                "ASNXER0152",
                "The preserved XER attribute fragment has no attribute value.",
                false);
        }

        attributeIndex = 0;
        while (attributeIndex < read.Tokens[0].Attributes.Length)
        {
            XerXmlAttribute attribute =
                read.Tokens[0].Attributes[attributeIndex];
            if (attribute.NamespaceDeclaration)
            {
                XerValueResult declaration =
                    this.AddPreservedNamespaceDeclaration(
                        attribute,
                        destination);
                if (!declaration.Success)
                {
                    return declaration;
                }
            }

            attributeIndex = attributeIndex + 1;
        }

        attributeIndex = 0;
        while (attributeIndex < destination.Count)
        {
            XerXmlAttribute existing = destination[attributeIndex];
            if (!existing.NamespaceDeclaration
                && existing.LocalName == preserved.LocalName
                && existing.NamespaceUri == preserved.NamespaceUri)
            {
                return this.CreateError(
                    "ASNXER0153",
                    "A preserved XER attribute collides with an encoded attribute.",
                    false);
            }

            attributeIndex = attributeIndex + 1;
        }

        destination.Add(preserved);
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal XerValueResult AddPreservedNamespaceDeclaration(
        XerXmlAttribute declaration,
        System.Collections.Generic.List<XerXmlAttribute> destination)
    {
        int index = 0;
        while (index < destination.Count)
        {
            XerXmlAttribute existing = destination[index];
            if (existing.NamespaceDeclaration
                && existing.NamespacePrefix == declaration.NamespacePrefix)
            {
                if (existing.Value != declaration.Value)
                {
                    return this.CreateError(
                        "ASNXER0153",
                        "A preserved XER namespace binding conflicts with an encoded binding.",
                        false);
                }

                return new XerValueResult
                {
                    Success = true,
                    Status = AsnDecodeStatus.Done
                };
            }

            index = index + 1;
        }

        destination.Add(declaration);
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal XerValueResult InsertPreservedUnknownElements(
        AsnUnknownField[] fields,
        System.Collections.Generic.List<XerXmlToken> destination)
    {
        bool[] inserted = new bool[fields.Length];
        int insertedCount = 0;
        while (insertedCount < fields.Length)
        {
            int selected = -1;
            int selectedIndex = int.MaxValue;
            int index = 0;
            while (index < fields.Length)
            {
                AsnUnknownField field = fields[index];
                if (!inserted[index]
                    && field != null
                    && !field.XmlAttribute)
                {
                    int encodedIndex = field.HasEncodedIndex
                        ? field.EncodedIndex
                        : int.MaxValue;
                    if (selected < 0 || encodedIndex < selectedIndex)
                    {
                        selected = index;
                        selectedIndex = encodedIndex;
                    }
                }

                index = index + 1;
            }

            if (selected < 0)
            {
                break;
            }

            XerValueResult encoded = this.EncodeUnknownField(fields[selected]);
            if (!encoded.Success)
            {
                return encoded;
            }

            int tokenIndex = this.FindSiblingInsertionTokenIndex(
                destination,
                selectedIndex);
            destination.InsertRange(tokenIndex, encoded.Tokens);
            inserted[selected] = true;
            insertedCount = insertedCount + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal int FindSiblingInsertionTokenIndex(
        System.Collections.Generic.List<XerXmlToken> tokens,
        int siblingIndex)
    {
        int depth = 0;
        int sibling = 0;
        int index = 0;
        while (index < tokens.Count)
        {
            XerXmlToken token = tokens[index];
            bool elementStart = token.Kind == XerXmlTokenKind.StartElement
                || token.Kind == XerXmlTokenKind.EmptyElement;
            if (depth == 0 && elementStart)
            {
                if (sibling >= siblingIndex)
                {
                    return index;
                }

                sibling = sibling + 1;
            }

            if (token.Kind == XerXmlTokenKind.StartElement)
            {
                depth = depth + 1;
            }
            else if (token.Kind == XerXmlTokenKind.EndElement)
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return tokens.Count;
    }

    internal XerValueResult PreserveUnknownAttributes(
        AsnFieldDescriptor[] descriptors,
        XerXmlAttribute[] attributes,
        XerXmlNamespaceBinding[] bindings)
    {
        System.Collections.Generic.List<AsnUnknownField> fields =
            new System.Collections.Generic.List<AsnUnknownField>();
        int encodedIndex = 0;
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration)
            {
                if (!this.IsKnownCollectionAttribute(
                    descriptors,
                    attribute))
                {
                    byte[] fragment = this.SerializeUnknownAttributeFragment(
                        attribute,
                        bindings);
                    if (fragment.Length == 0
                        || !new ResourceCounter(this.Limits)
                            .AllowsOpenTypeBytes(fragment.Length))
                    {
                        return this.CreateError(
                            fragment.Length == 0
                                ? "ASNXER0151"
                                : "ASNR0010",
                            fragment.Length == 0
                                ? "The unknown XER attribute could not be retained."
                                : "The unknown XER attribute exceeds the configured byte limit.",
                            fragment.Length != 0);
                    }

                    fields.Add(new AsnUnknownField
                    {
                        Name = attribute.LocalName,
                        XmlAttribute = true,
                        XmlPrefix = attribute.Prefix,
                        XmlNamespaceUri = attribute.NamespaceUri,
                        HasEncodedIndex = true,
                        EncodedIndex = encodedIndex,
                        EncodingRules = this.GetActiveEncodingRules(),
                        HasEncodingRules = true,
                        HasTextData = true,
                        TextData = System.Text.Encoding.UTF8.GetString(fragment)
                    });
                }

                encodedIndex = encodedIndex + 1;
            }

            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                UnknownFields = fields.ToArray()
            }
        };
    }

    internal bool IsKnownCollectionAttribute(
        AsnFieldDescriptor[] descriptors,
        XerXmlAttribute attribute)
    {
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Attribute) != null)
            {
                XerExtendedName identity = this.ResolveExtendedName(
                    descriptor.Type,
                    descriptor.Name);
                if (identity.Success
                    && identity.LocalName == attribute.LocalName
                    && identity.NamespaceUri == attribute.NamespaceUri)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal byte[] SerializeUnknownAttributeFragment(
        XerXmlAttribute attribute,
        XerXmlNamespaceBinding[] bindings)
    {
        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < bindings.Length)
        {
            XerXmlNamespaceBinding binding = bindings[index];
            if (binding.Prefix != "xml"
                && binding.Prefix != "xmlns"
                && binding.NamespaceUri.Length > 0)
            {
                attributes.Add(new XerXmlAttribute
                {
                    NamespaceDeclaration = true,
                    NamespacePrefix = binding.Prefix,
                    NamespaceUri = XerXmlNamespaceContext.XmlnsNamespace,
                    Value = binding.NamespaceUri
                });
            }

            index = index + 1;
        }

        attributes.Add(new XerXmlAttribute
        {
            Name = attribute.Name,
            Prefix = attribute.Prefix,
            LocalName = attribute.LocalName,
            NamespaceUri = attribute.NamespaceUri,
            Value = attribute.Value
        });
        XerXmlToken token = new XerXmlToken
        {
            Kind = XerXmlTokenKind.EmptyElement,
            LocalName = "fragment",
            Attributes = attributes.ToArray()
        };
        return this.SerializeTokenRange([token], 0, 1);
    }

    internal AsnUnknownField[] CombineUnknownFields(
        AsnUnknownField[] first,
        AsnUnknownField[] second)
    {
        AsnUnknownField[] result = new AsnUnknownField[
            first.Length + second.Length];
        int index = 0;
        while (index < first.Length)
        {
            result[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            result[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return result;
    }

    internal bool MatchesUnknownFieldIdentity(
        AsnUnknownField field,
        XerXmlToken token)
    {
        if (field.XmlNamespaceUri.Length > 0
            || field.XmlPrefix.Length > 0)
        {
            return token.LocalName == field.Name
                && token.NamespaceUri == field.XmlNamespaceUri;
        }

        return token.Name == field.Name || token.LocalName == field.Name;
    }

    internal int GetTokenRangeByteLength(
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        XerXmlToken first = tokens[offset];
        XerXmlToken last = tokens[offset + count - 1];
        int end = last.ByteOffset + last.ByteLength;
        return end >= first.ByteOffset ? end - first.ByteOffset : 0;
    }

    internal AsnTypeDescriptor? ResolveOpenTypeForEncoding(
        AsnTypeDescriptor type,
        AsnOpenTypeValue open)
    {
        AsnTypeDescriptor? result = null;
        int matches = 0;
        string identity = open.GetResolvedTypeIdentity();
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            AsnObjectTableDescriptor table = type.ObjectTables[tableIndex];
            int entryIndex = 0;
            while (entryIndex < table.Entries.Length)
            {
                AsnObjectTableEntryDescriptor entry = table.Entries[entryIndex];
                if (entry != null
                    && entry.Type != null
                    && (entry.Type.StableId == identity
                        || entry.Type.Name == identity
                        || entry.Type.GeneratedName == identity
                        || open.TypeName == entry.Type.Name))
                {
                    result = entry.Type;
                    matches = matches + 1;
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return matches == 1 ? result : null;
    }

    internal AsnTypeDescriptor? ResolveOpenTypeForDecoding(
        AsnTypeDescriptor type,
        string elementName)
    {
        AsnTypeDescriptor? result = null;
        int matches = 0;
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            AsnObjectTableDescriptor table = type.ObjectTables[tableIndex];
            int entryIndex = 0;
            while (entryIndex < table.Entries.Length)
            {
                AsnObjectTableEntryDescriptor entry = table.Entries[entryIndex];
                if (entry != null && entry.Type != null)
                {
                    XerNameResult name = this.Names.GetElementName(entry.Type);
                    if (name.Success && name.Name == elementName)
                    {
                        result = entry.Type;
                        matches = matches + 1;
                    }
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return matches == 1 ? result : null;
    }

    internal int CountOpenTypeMatches(
        AsnTypeDescriptor type,
        string elementName)
    {
        int matches = 0;
        int tableIndex = 0;
        while (tableIndex < type.ObjectTables.Length)
        {
            AsnObjectTableDescriptor table = type.ObjectTables[tableIndex];
            int entryIndex = 0;
            while (entryIndex < table.Entries.Length)
            {
                AsnObjectTableEntryDescriptor entry = table.Entries[entryIndex];
                if (entry != null && entry.Type != null)
                {
                    XerNameResult name = this.Names.GetElementName(entry.Type);
                    if (name.Success && name.Name == elementName)
                    {
                        matches = matches + 1;
                    }
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return matches;
    }

    internal byte[] SerializeTokenRange(
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, this.Limits);
        int index = 0;
        while (index < count)
        {
            XerXmlWriteResult written = writer.WriteToken(
                tokens[offset + index]);
            if (!written.Success)
            {
                return [];
            }

            index = index + 1;
        }

        XerXmlWriteResult complete = writer.CompleteWriter();
        return complete.Success ? output.ToArray() : [];
    }

    internal AsnEncodingInstruction? FindInstruction(
        AsnTypeDescriptor type,
        AsnInstructionKind kind)
    {
        int encodingIndex = 0;
        while (encodingIndex < type.Encodings.Length)
        {
            AsnEncodingInstruction[] instructions =
                type.Encodings[encodingIndex].Instructions;
            int instructionIndex = 0;
            while (instructionIndex < instructions.Length)
            {
                AsnEncodingInstruction instruction =
                    instructions[instructionIndex];
                if (instruction != null
                    && instruction.Kind == kind
                    && !instruction.Negated)
                {
                    return instruction;
                }

                instructionIndex = instructionIndex + 1;
            }

            encodingIndex = encodingIndex + 1;
        }

        return null;
    }

    internal XerValueResult ApplyDefaultForEmptyEncoding(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        XerValueResult encoded)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.DefaultForEmpty);
        if (instruction == null
            || type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            return encoded;
        }

        AsnRuntimeValue? defaultValue = this.CreateInstructionValue(
            type,
            instruction);
        if (defaultValue == null
            || !this.RuntimeScalarValuesEqual(type, value, defaultValue))
        {
            return encoded;
        }

        return this.MakeEncodingContentEmpty(encoded);
    }

    internal XerValueResult DecodeDefaultForEmptyValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.DefaultForEmpty);
        if (instruction == null
            || type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set
            || !this.HasEmptyWrapperContent(tokens, offset, count, elementName))
        {
            return new XerValueResult();
        }

        AsnRuntimeValue? value = this.CreateInstructionValue(type, instruction);
        if (value == null)
        {
            return this.CreateError(
                "ASNXER0350",
                "DEFAULT-FOR-EMPTY contains a value that cannot be applied to the target type.",
                false);
        }

        return this.CreateDecodedValue(value, tokens, offset, count);
    }

    internal AsnRuntimeValue? CreateInstructionValue(
        AsnTypeDescriptor type,
        AsnEncodingInstruction instruction)
    {
        if (instruction.Arguments.Length < 2
            || instruction.Arguments[0] != "AS")
        {
            return null;
        }

        string text = this.JoinInstructionValue(instruction.Arguments, 1);
        if (this.IsRestrictedCharacterKind(type.Kind))
        {
            string decoded = this.DecodeInstructionString(text);
            return decoded.Length == 0 && text != "\"\""
                ? null
                : new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = decoded
                };
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            if (text != "TRUE" && text != "FALSE")
            {
                return null;
            }

            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = text == "TRUE"
            };
        }

        if (type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated)
        {
            XerIntegerTextResult parsed = new XerPrimitiveCodec(this.Limits)
                .ParseSignedInteger(text, false);
            AsnValueDescriptor? named = null;
            System.Numerics.BigInteger number;
            if (!parsed.Success)
            {
                named = this.FindNamedValueByName(type, text);
                if (named == null
                    || named.Value == null
                    || !named.Value.HasIntegerValue)
                {
                    return null;
                }

                number = named.Value.IntegerValue;
            }
            else
            {
                number = parsed.Value;
            }

            return new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.Enumerated
                    ? AsnRuntimeValueKind.Enumerated
                    : AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = number,
                TextValue = named == null ? "" : named.Name
            };
        }

        return null;
    }

    internal string JoinInstructionValue(string[] arguments, int start)
    {
        string result = "";
        int index = start;
        while (index < arguments.Length)
        {
            if (result.Length > 0)
            {
                result = result + " ";
            }

            result = result + arguments[index];
            index = index + 1;
        }

        return result;
    }

    internal bool RuntimeScalarValuesEqual(
        AsnTypeDescriptor type,
        AsnRuntimeValue left,
        AsnRuntimeValue right)
    {
        if (type.Kind == AsnTypeKind.Boolean)
        {
            return left.Kind == AsnRuntimeValueKind.Boolean
                && left.HasBooleanValue
                && right.HasBooleanValue
                && left.BooleanValue == right.BooleanValue;
        }

        if (type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated)
        {
            return left.HasIntegerValue
                && right.HasIntegerValue
                && left.IntegerValue == right.IntegerValue;
        }

        return this.IsRestrictedCharacterKind(type.Kind)
            && left.Kind == AsnRuntimeValueKind.CharacterString
            && right.Kind == AsnRuntimeValueKind.CharacterString
            && left.TextValue == right.TextValue;
    }

    internal AsnValueDescriptor? FindNamedValueByName(
        AsnTypeDescriptor type,
        string name)
    {
        AsnValueDescriptor[] values = type.NamedValues.Length > 0
            ? type.NamedValues
            : type.RootNamedValues;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].Name == name)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool HasEmptyWrapperContent(
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return false;
        }

        int index = 0;
        while (index < wrapped.Content.Length)
        {
            XerXmlToken token = wrapped.Content[index];
            if ((token.Kind != XerXmlTokenKind.Text
                    && token.Kind != XerXmlTokenKind.CData)
                || token.Value.Length != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XerValueResult MakeEncodingContentEmpty(XerValueResult encoded)
    {
        if (encoded.Tokens.Length == 0
            || !this.IsElement(encoded.Tokens[0]))
        {
            return this.CreateError(
                "ASNXER0351",
                "DEFAULT-FOR-EMPTY requires one associated wrapper element.",
                false);
        }

        XerXmlToken first = encoded.Tokens[0];
        first.Kind = XerXmlTokenKind.EmptyElement;
        encoded.Tokens = [first];
        encoded.ConsumedCount = 1;
        return encoded;
    }

    internal XerValueResult ApplyPiOrCommentEncoding(
        AsnTypeDescriptor type,
        XerValueResult encoded)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.PiOrComment);
        if (instruction == null)
        {
            return encoded;
        }

        string position = this.GetPiOrCommentPosition(instruction);
        XerXmlToken[] annotations = this.ParsePiOrCommentTokens(instruction);
        if (position.Length == 0 || annotations.Length == 0)
        {
            return this.CreateError(
                "ASNXER0352",
                "PI-OR-COMMENT requires valid XML comments or processing instructions and a position.",
                false);
        }

        XerXmlToken[] valueTokens = encoded.Tokens;
        if (position == "BEFORE-VALUE" || position == "AFTER-VALUE")
        {
            valueTokens = this.ExpandEmptyElement(valueTokens);
            if (valueTokens.Length < 2)
            {
                return this.CreateError(
                    "ASNXER0353",
                    "PI-OR-COMMENT cannot locate the associated value content.",
                    false);
            }
        }

        int insertion = 0;
        if (position == "BEFORE-VALUE")
        {
            insertion = 1;
        }
        else if (position == "AFTER-VALUE")
        {
            insertion = valueTokens.Length - 1;
        }
        else if (position == "AFTER-TAG")
        {
            insertion = valueTokens.Length;
        }

        XerXmlToken[] result = new XerXmlToken[
            valueTokens.Length + annotations.Length];
        int index = 0;
        while (index < insertion)
        {
            result[index] = valueTokens[index];
            index = index + 1;
        }

        int annotationIndex = 0;
        while (annotationIndex < annotations.Length)
        {
            result[index] = annotations[annotationIndex];
            index = index + 1;
            annotationIndex = annotationIndex + 1;
        }

        int valueIndex = insertion;
        while (valueIndex < valueTokens.Length)
        {
            result[index] = valueTokens[valueIndex];
            index = index + 1;
            valueIndex = valueIndex + 1;
        }

        encoded.Tokens = result;
        encoded.ConsumedCount = result.Length;
        return encoded;
    }

    internal string GetPiOrCommentPosition(AsnEncodingInstruction instruction)
    {
        if (instruction.Arguments.Length < 3
            || instruction.Arguments[0] != "AS")
        {
            return "";
        }

        string position = instruction.Arguments[instruction.Arguments.Length - 1];
        return position == "BEFORE-TAG"
                || position == "BEFORE-VALUE"
                || position == "AFTER-VALUE"
                || position == "AFTER-TAG"
            ? position
            : "";
    }

    internal XerXmlToken[] ParsePiOrCommentTokens(
        AsnEncodingInstruction instruction)
    {
        if (instruction.Arguments.Length != 3)
        {
            return [];
        }

        string fragment = this.DecodeInstructionString(
            instruction.Arguments[1]);
        if (fragment.Length == 0)
        {
            return [];
        }

        string wrapped = "<annotations>" + fragment + "</annotations>";
        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(wrapped),
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length < 3)
        {
            return [];
        }

        XerXmlToken[] result = new XerXmlToken[read.Tokens.Length - 2];
        int index = 0;
        while (index < result.Length)
        {
            XerXmlToken token = read.Tokens[index + 1];
            if (token.Kind != XerXmlTokenKind.Comment
                && token.Kind != XerXmlTokenKind.ProcessingInstruction)
            {
                return [];
            }

            result[index] = token;
            index = index + 1;
        }

        return result;
    }

    internal XerXmlToken[] ExpandEmptyElement(XerXmlToken[] tokens)
    {
        if (tokens.Length != 1
            || tokens[0].Kind != XerXmlTokenKind.EmptyElement)
        {
            return tokens;
        }

        XerXmlToken start = tokens[0];
        start.Kind = XerXmlTokenKind.StartElement;
        XerXmlToken end = new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement,
            Name = start.Name,
            Prefix = start.Prefix,
            LocalName = start.LocalName,
            NamespaceUri = start.NamespaceUri,
            NamespaceBindings = start.NamespaceBindings
        };
        return [start, end];
    }

    internal XerXmlToken[] RemovePiOrCommentTokens(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        if (!this.Extended
            || this.FindInstruction(
                type,
                AsnInstructionKind.PiOrComment) == null)
        {
            return offset == 0 && count == tokens.Length
                ? tokens
                : this.CopyTokenRange(tokens, offset, count);
        }

        System.Collections.Generic.List<XerXmlToken> result =
            new System.Collections.Generic.List<XerXmlToken>();
        int index = offset;
        int limit = offset + count;
        while (index < limit)
        {
            XerXmlToken token = tokens[index];
            if (token.Kind != XerXmlTokenKind.Comment
                && token.Kind != XerXmlTokenKind.ProcessingInstruction)
            {
                result.Add(token);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal int[] GetPiOrCommentValueRange(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        if (!this.Extended
            || this.FindInstruction(
                type,
                AsnInstructionKind.PiOrComment) == null)
        {
            return [offset, count];
        }

        int index = offset;
        int limit = offset + count;
        while (index < limit && !this.IsElement(tokens[index]))
        {
            index = index + 1;
        }

        int elementCount = index < limit
            ? this.GetElementTokenCount(tokens, index)
            : 0;
        return elementCount == 0
            ? [-1, 0]
            : [index, elementCount];
    }

    internal XerExtendedName ResolveExtendedName(
        AsnTypeDescriptor type,
        string defaultName)
    {
        XerExtendedName result = new XerExtendedName
        {
            Success = true,
            LocalName = defaultName
        };
        if (!this.Extended)
        {
            return result;
        }

        AsnEncodingInstruction? name = this.FindInstruction(
            type,
            AsnInstructionKind.Name);
        if (name != null)
        {
            if (name.Arguments.Length != 2 || name.Arguments[0] != "AS")
            {
                return new XerExtendedName
                {
                    ErrorMessage = "A NAME instruction requires AS and one name action."
                };
            }

            result.LocalName = this.ApplyNameAction(
                defaultName,
                name.Arguments[1]);
            if (result.LocalName.Length == 0)
            {
                return new XerExtendedName
                {
                    ErrorMessage = "A NAME instruction contains an invalid name action."
                };
            }
        }

        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.Namespace);
        if (instruction != null)
        {
            if (instruction.Arguments.Length != 2
                    && instruction.Arguments.Length != 4
                || instruction.Arguments.Length > 0
                    && instruction.Arguments[0] != "AS")
            {
                return new XerExtendedName
                {
                    ErrorMessage = "A NAMESPACE instruction has malformed arguments."
                };
            }

            result.NamespaceUri = instruction.Arguments.Length > 1
                ? this.DecodeInstructionString(instruction.Arguments[1])
                : "";
            if (result.NamespaceUri.Length == 0)
            {
                return new XerExtendedName
                {
                    ErrorMessage = "A NAMESPACE instruction requires a non-empty quoted URI."
                };
            }

            if (instruction.Arguments.Length == 4)
            {
                if (instruction.Arguments[2] != "PREFIX")
                {
                    return new XerExtendedName
                    {
                        ErrorMessage = "A NAMESPACE prefix must follow PREFIX."
                    };
                }

                result.Prefix = this.DecodeInstructionString(
                    instruction.Arguments[3]);
                if (!this.Characters.IsValidNcName(result.Prefix)
                    || result.Prefix == "xml"
                    || result.Prefix == "xmlns")
                {
                    return new XerExtendedName
                    {
                        ErrorMessage = "A NAMESPACE instruction contains an invalid prefix."
                    };
                }
            }
        }

        if (!this.Characters.IsValidNcName(result.LocalName))
        {
            return new XerExtendedName
            {
                ErrorMessage = "The effective EXTENDED-XER local name is not an XML NCName."
            };
        }

        return result;
    }

    internal string ApplyNameAction(string source, string action)
    {
        string quoted = this.DecodeInstructionString(action);
        if (quoted.Length > 0)
        {
            return quoted;
        }

        if (action == "CAPITALIZED")
        {
            return source.Length == 0
                ? ""
                : char.ToUpperInvariant(source[0]) + source.Substring(1);
        }

        if (action == "UNCAPITALIZED")
        {
            return source.Length == 0
                ? ""
                : char.ToLowerInvariant(source[0]) + source.Substring(1);
        }

        if (action == "UPPERCASED")
        {
            return source.ToUpperInvariant();
        }

        if (action == "LOWERCASED")
        {
            return source.ToLowerInvariant();
        }

        return "";
    }

    internal XerValueResult ApplyElementIdentity(
        XerValueResult source,
        XerExtendedName identity)
    {
        if (!source.Success || source.Tokens.Length == 0)
        {
            return source;
        }

        this.SetTokenIdentity(source.Tokens[0], identity);
        if (source.Tokens.Length > 1
            && source.Tokens[source.Tokens.Length - 1].Kind
                == XerXmlTokenKind.EndElement)
        {
            this.SetTokenIdentity(
                source.Tokens[source.Tokens.Length - 1],
                identity);
            source.Tokens[source.Tokens.Length - 1].Name =
                identity.Prefix.Length == 0
                    ? identity.LocalName
                    : identity.Prefix + ":" + identity.LocalName;
        }

        return source;
    }

    internal void SetTokenIdentity(
        XerXmlToken token,
        XerExtendedName identity)
    {
        token.LocalName = identity.LocalName;
        token.Prefix = identity.Prefix;
        token.NamespaceUri = identity.NamespaceUri;
        token.Name = identity.LocalName;
    }

    internal XerValueResult EncodeListValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        XerCharacterValueResult list = this.EncodeListText(type, value);
        if (!list.Success)
        {
            return this.CreateError(
                list.ErrorCode,
                list.ErrorMessage,
                list.LimitExceeded);
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        if (list.Text.Length > 0)
        {
            content.Add(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Text,
                Value = list.Text
            });
        }

        return this.WrapEncoding(value, elementName, content);
    }

    internal XerValueResult EncodeBase64Value(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        int depth)
    {
        byte[] bytes;
        if (type.Kind == AsnTypeKind.OctetString
            && value.Kind == AsnRuntimeValueKind.OctetString)
        {
            bytes = value.Octets;
        }
        else if (this.IsRestrictedCharacterKind(type.Kind)
            && value.Kind == AsnRuntimeValueKind.CharacterString)
        {
            bytes = System.Text.Encoding.UTF8.GetBytes(value.TextValue);
        }
        else if (type.Kind == AsnTypeKind.OpenType
            && value.Kind == AsnRuntimeValueKind.OpenType
            && value.OpenTypeValue != null)
        {
            AsnOpenTypeValue open = value.OpenTypeValue;
            if (new OpaquePayloadPolicy().CanReuseOpenType(
                open,
                this.GetActiveEncodingRules()))
            {
                bytes = open.OriginalData;
            }
            else if (open.Resolved && open.Value != null)
            {
                AsnTypeDescriptor? resolved = this.ResolveOpenTypeForEncoding(
                    type,
                    open);
                if (resolved == null)
                {
                    return this.CreateError(
                        "ASNXER0340",
                        "BASE64 cannot resolve the open type to an encoded value.",
                        false);
                }

                XerNameResult name = this.Names.GetElementName(resolved);
                if (!name.Success)
                {
                    return this.CopyNameFailure(name);
                }

                XerValueResult encoded = this.EncodeValueAtDepth(
                    resolved,
                    open.Value,
                    name.Name,
                    depth + 1);
                if (!encoded.Success)
                {
                    return encoded;
                }

                bytes = this.SerializeTokenRange(
                    encoded.Tokens,
                    0,
                    encoded.Tokens.Length);
            }
            else
            {
                return this.CreateError(
                    "ASNXER0340",
                    "BASE64 requires bytes or a resolved open value.",
                    false);
            }
        }
        else
        {
            return this.CreateError(
                "ASNXER0340",
                "BASE64 is not applicable to this type or runtime value.",
                false);
        }

        if (bytes == null
            || !new ResourceCounter(this.Limits).AllowsOpenTypeBytes(bytes.Length))
        {
            return this.CreateError(
                "ASNR0010",
                "The BASE64 value exceeds the configured byte limit.",
                true);
        }

        string text = System.Convert.ToBase64String(bytes);
        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return this.CreateError(
                "ASNR0009",
                "The BASE64 value exceeds the configured text limit.",
                true);
        }

        return this.WrapTextEncoding(value, elementName, text);
    }

    internal XerValueResult DecodeBase64Value(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes)
            || !this.HasCharacterWrapperContent(wrapped))
        {
            return this.CreateError(
                "ASNXER0341",
                "BASE64 requires character content without ordinary attributes.",
                false);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        XerBinaryResult binary = this.DecodeBase64Text(text);
        if (!binary.Success)
        {
            return this.CreateError(
                binary.ErrorCode,
                binary.ErrorMessage,
                binary.LimitExceeded);
        }

        AsnRuntimeValue value;
        if (type.Kind == AsnTypeKind.OctetString)
        {
            value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = binary.Bytes
            };
        }
        else if (this.IsRestrictedCharacterKind(type.Kind))
        {
            string decoded = System.Text.Encoding.UTF8.GetString(binary.Bytes);
            byte[] encoded = System.Text.Encoding.UTF8.GetBytes(decoded);
            if (!this.BytesEqual(encoded, binary.Bytes)
                || !this.Characters.IsValidText(decoded))
            {
                return this.CreateError(
                    "ASNXER0342",
                    "BASE64 does not contain a valid UTF-8 character string.",
                    false);
            }

            value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = decoded
            };
        }
        else if (type.Kind == AsnTypeKind.OpenType)
        {
            value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = new AsnOpenTypeValue
                {
                    Unresolved = true,
                    HasEncodingRules = true,
                    EncodingRules = this.GetActiveEncodingRules(),
                    HasOriginalData = true,
                    OriginalData = binary.Bytes,
                    HasEncodedLocation = true,
                    EncodedOffset = tokens[offset].ByteOffset,
                    EncodedLength = binary.Bytes.Length
                }
            };
        }
        else
        {
            return this.CreateError(
                "ASNXER0340",
                "BASE64 is not applicable to this type.",
                false);
        }

        return this.CreateDecodedValue(
            value,
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerBinaryResult DecodeBase64Text(string source)
    {
        System.Text.StringBuilder cleaned = new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Length)
        {
            char current = source[index];
            if (!this.IsXmlWhitespace(current))
            {
                cleaned.Append(current);
            }

            index = index + 1;
        }

        string text = cleaned.ToString();
        if (text.Length % 4 != 0)
        {
            return new XerBinaryResult
            {
                ErrorCode = "ASNXER0341",
                ErrorMessage = "The BASE64 lexical value has an invalid length."
            };
        }

        int padding = 0;
        if (text.Length > 0 && text[text.Length - 1] == '=')
        {
            padding = 1;
            if (text.Length > 1 && text[text.Length - 2] == '=')
            {
                padding = 2;
            }
        }

        index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            bool data = current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9'
                || current == '+' || current == '/';
            bool pad = current == '=' && index >= text.Length - padding;
            if (!data && !pad)
            {
                return new XerBinaryResult
                {
                    ErrorCode = "ASNXER0341",
                    ErrorMessage = "The BASE64 lexical value contains an invalid character."
                };
            }

            index = index + 1;
        }

        byte[] bytes = System.Convert.FromBase64String(text);
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(bytes.Length))
        {
            return new XerBinaryResult
            {
                ErrorCode = "ASNR0010",
                ErrorMessage = "The BASE64 value exceeds the configured byte limit.",
                LimitExceeded = true
            };
        }

        return new XerBinaryResult { Success = true, Bytes = bytes };
    }

    internal XerValueResult EncodeDecimalValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        if (type.Kind != AsnTypeKind.Real
            || value.Kind != AsnRuntimeValueKind.Real
            || value.RealValue == null)
        {
            return this.CreateError(
                "ASNXER0343",
                "DECIMAL requires a REAL value.",
                false);
        }

        string text = this.FormatDecimalValue(value.RealValue);
        if (text.Length == 0)
        {
            return this.CreateError(
                "ASNXER0343",
                "DECIMAL requires a finite base-ten REAL and excludes minus zero.",
                false);
        }

        return this.WrapTextEncoding(value, elementName, text);
    }

    internal string FormatDecimalValue(AsnRealValue real)
    {
        if (!real.IsValid())
        {
            return "";
        }

        if (real.Kind == AsnRealKind.Zero)
        {
            return "0";
        }

        if (real.Kind != AsnRealKind.FiniteDecimal)
        {
            return "";
        }

        AsnRealValue? normalized = real;
        if (!real.Normalized
            || !real.HasDecimalValue
            || real.DecimalCoefficient == System.Numerics.BigInteger.Zero)
        {
            normalized = new XerPrimitiveCodec(this.Limits)
                .ParseDecimalReal(real.DecimalText);
        }

        if (normalized == null)
        {
            return "";
        }

        string digits = System.Numerics.BigInteger.Abs(
            normalized.DecimalCoefficient).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        System.Numerics.BigInteger exponent = normalized.DecimalExponent;
        if (exponent > new System.Numerics.BigInteger(int.MaxValue)
            || exponent < new System.Numerics.BigInteger(int.MinValue))
        {
            return "";
        }

        int shift = (int)exponent;
        long signLength = normalized.DecimalCoefficient
            < System.Numerics.BigInteger.Zero ? 1L : 0L;
        long projectedLength;
        if (shift >= 0)
        {
            projectedLength = signLength + digits.Length + (long)shift;
        }
        else
        {
            long pointPosition = digits.Length + (long)shift;
            projectedLength = pointPosition > 0
                ? signLength + digits.Length + 1L
                : signLength + 2L - pointPosition + digits.Length;
        }

        if (projectedLength > int.MaxValue
            || !new ResourceCounter(this.Limits).AllowsTextCharacters(
                (int)projectedLength))
        {
            return "";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        if (normalized.DecimalCoefficient < System.Numerics.BigInteger.Zero)
        {
            result.Append('-');
        }

        if (shift >= 0)
        {
            result.Append(digits);
            int zeroIndex = 0;
            while (zeroIndex < shift)
            {
                result.Append('0');
                zeroIndex = zeroIndex + 1;
            }
        }
        else
        {
            int point = digits.Length + shift;
            if (point > 0)
            {
                result.Append(digits.Substring(0, point));
                result.Append('.');
                result.Append(digits.Substring(point));
            }
            else
            {
                result.Append("0.");
                int zeroIndex = 0;
                long zeroCount = -(long)point;
                while (zeroIndex < zeroCount)
                {
                    result.Append('0');
                    zeroIndex = zeroIndex + 1;
                }

                result.Append(digits);
            }
        }

        return new ResourceCounter(this.Limits).AllowsTextCharacters(
            result.Length)
                ? result.ToString()
                : "";
    }

    internal XerValueResult DecodeDecimalValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerWrappedValue wrapped = this.ReadModifiedCharacterWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        AsnRealValue? real = this.ParseDecimalValue(text);
        if (type.Kind != AsnTypeKind.Real || real == null)
        {
            return this.CreateError(
                "ASNXER0344",
                "The DECIMAL lexical value is invalid or exponential.",
                false);
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = real
            },
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal AsnRealValue? ParseDecimalValue(string text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        bool negative = text[0] == '-';
        int index = negative ? 1 : 0;
        int point = -1;
        int digits = 0;
        System.Text.StringBuilder coefficient = new System.Text.StringBuilder();
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '.' && point < 0)
            {
                point = digits;
            }
            else if (current >= '0' && current <= '9')
            {
                coefficient.Append(current);
                digits = digits + 1;
            }
            else
            {
                return null;
            }

            index = index + 1;
        }

        if (digits == 0
            || text[text.Length - 1] == '.'
            || !new ResourceCounter(this.Limits).AllowsTextCharacters(digits))
        {
            return null;
        }

        string coefficientText = coefficient.ToString();
        int firstNonzero = 0;
        while (firstNonzero < coefficientText.Length
            && coefficientText[firstNonzero] == '0')
        {
            firstNonzero = firstNonzero + 1;
        }

        if (firstNonzero == coefficientText.Length)
        {
            return new AsnRealValue
            {
                Normalized = true,
                Kind = AsnRealKind.Zero
            };
        }

        XerIntegerTextResult parsed = new XerPrimitiveCodec(this.Limits)
            .ParseSignedInteger(
                coefficientText.Substring(firstNonzero),
                true);
        if (!parsed.Success)
        {
            return null;
        }

        System.Numerics.BigInteger value = negative
            ? -parsed.Value
            : parsed.Value;
        int fractionalDigits = point < 0 ? 0 : digits - point;
        System.Numerics.BigInteger exponent =
            new System.Numerics.BigInteger(-fractionalDigits);
        while (value % 10 == System.Numerics.BigInteger.Zero)
        {
            value = value / 10;
            exponent = exponent + System.Numerics.BigInteger.One;
        }

        return new AsnRealValue
        {
            Normalized = true,
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = text,
            DecimalCoefficient = value,
            DecimalExponent = exponent
        };
    }

    internal XerValueResult EncodeNumberValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        XerPrimitiveCodec primitives = new XerPrimitiveCodec(this.Limits);
        AsnValueDescriptor? descriptor = value.TextValue.Length > 0
            ? primitives.FindEnumeratedDescriptor(type, value.TextValue)
            : primitives.FindEnumeratedNumber(type, value);
        if (type.Kind != AsnTypeKind.Enumerated
            || value.Kind != AsnRuntimeValueKind.Enumerated
            || descriptor == null
            || descriptor.Value == null
            || !descriptor.Value.HasIntegerValue)
        {
            return this.CreateError(
                "ASNXER0345",
                "USE-NUMBER requires a numbered ENUMERATED value.",
                false);
        }

        string text = descriptor.Value.IntegerValue.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        return this.WrapTextEncoding(value, elementName, text);
    }

    internal XerValueResult DecodeNumberValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerWrappedValue wrapped = this.ReadModifiedCharacterWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        XerIntegerTextResult parsed = new XerPrimitiveCodec(this.Limits)
            .ParseSignedInteger(text, false);
        AsnValueDescriptor? descriptor = parsed.Success
            ? this.FindEnumeratedByNumber(type, parsed.Value)
            : null;
        if (type.Kind != AsnTypeKind.Enumerated
            || descriptor == null
            || descriptor.Value == null)
        {
            return this.CreateError(
                "ASNXER0346",
                "The USE-NUMBER value does not identify one enumeration.",
                false);
        }

        return this.CreateDecodedValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Enumerated,
                TextValue = descriptor.Name,
                HasIntegerValue = true,
                IntegerValue = descriptor.Value.IntegerValue
            },
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeWhitespaceValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        if (!this.IsWhitespaceInstructionValid(type)
            || (!this.IsRestrictedCharacterKind(type.Kind)
                && this.FindInstruction(
                    type,
                    AsnInstructionKind.Text) == null))
        {
            return this.CreateError(
                "ASNXER0347",
                "WHITESPACE requires REPLACE or COLLAPSE on a restricted string.",
                false);
        }

        XerCharacterValueResult character = this.EncodeCharacterValue(
            type,
            value);
        if (!character.Success)
        {
            return this.CreateError(
                character.ErrorCode,
                character.ErrorMessage,
                character.LimitExceeded);
        }

        return this.WrapTextEncoding(value, elementName, character.Text);
    }

    internal XerValueResult DecodeWhitespaceValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerWrappedValue wrapped = this.ReadModifiedCharacterWrapper(
            tokens,
            offset,
            count,
            elementName);
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.Whitespace);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.IsWhitespaceInstructionValid(type)
            || instruction == null)
        {
            return this.CreateError(
                "ASNXER0347",
                "The WHITESPACE action is malformed.",
                false);
        }

        string source = wrapped.Empty ? "" : wrapped.Content[0].Value;
        string normalized = instruction.Arguments[0] == "COLLAPSE"
            ? this.CollapseWhitespace(source)
            : this.ReplaceWhitespace(source);
        XerCharacterValueResult decoded = this.DecodeCharacterValue(
            type,
            normalized);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return this.CreateDecodedValue(
            decoded.Value,
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult EncodeQNameValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        XerCharacterValueResult qname = this.EncodeQNameCharacter(type, value);
        if (!qname.Success)
        {
            return this.CreateError(
                qname.ErrorCode,
                qname.ErrorMessage,
                qname.LimitExceeded);
        }

        XerValueResult encoded = this.WrapTextEncoding(
            value,
            elementName,
            qname.Text);
        if (encoded.Success
            && encoded.Tokens.Length > 0
            && qname.NamespaceUri.Length > 0)
        {
            encoded.Tokens[0].Attributes = [this.CreateNamespaceDeclaration(
                qname.Prefix,
                qname.NamespaceUri)];
        }

        return encoded;
    }

    internal XerValueResult DecodeQNameValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerWrappedValue wrapped = this.ReadModifiedCharacterWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        XerCharacterValueResult qname = this.DecodeQNameCharacter(
            type,
            text,
            wrapped.NamespaceBindings);
        if (!qname.Success || qname.Value == null)
        {
            return this.CreateError(
                qname.ErrorCode,
                qname.ErrorMessage,
                qname.LimitExceeded);
        }

        return this.CreateDecodedValue(
            qname.Value,
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerCharacterValueResult EncodeQNameCharacter(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnFieldDescriptor[] fields = this.GetAllFields(type);
        if (!this.IsQNameShapeValid(fields)
            || value.Kind != AsnRuntimeValueKind.Sequence)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0349",
                ErrorMessage = "USE-QNAME requires its two-component sequence value."
            };
        }

        AsnRuntimeFieldValue? namespaceField = this.FindRuntimeField(
            value.Fields,
            fields[0].Name);
        AsnRuntimeFieldValue? nameField = this.FindRuntimeField(
            value.Fields,
            fields[1].Name);
        AsnRuntimeValue? nameValue = nameField == null ? null : nameField.Value;
        if (nameField == null
            || !nameField.Present
            || nameValue == null
            || nameValue.Kind != AsnRuntimeValueKind.CharacterString
            || !this.IsQNameLocalNameValid(nameValue.TextValue))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0349",
                ErrorMessage = "USE-QNAME requires a valid NCName in its second component."
            };
        }

        AsnRuntimeValue? namespaceValue = namespaceField == null
            ? null
            : namespaceField.Value;
        bool qualified = namespaceField != null
            && namespaceField.Present
            && namespaceValue != null;
        if (qualified
            && (namespaceValue == null
                || namespaceValue.Kind != AsnRuntimeValueKind.CharacterString
                || namespaceValue.TextValue.Length == 0
                || this.ContainsXmlWhitespace(namespaceValue.TextValue)))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0349",
                ErrorMessage = "USE-QNAME requires a non-empty namespace URI when present."
            };
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Prefix = qualified ? "ns1" : "",
            NamespaceUri = qualified && namespaceValue != null
                ? namespaceValue.TextValue
                : "",
            Text = qualified ? "ns1:" + nameValue.TextValue : nameValue.TextValue
        };
    }

    internal XerCharacterValueResult DecodeQNameCharacter(
        AsnTypeDescriptor type,
        string text,
        XerXmlNamespaceBinding[] bindings)
    {
        AsnFieldDescriptor[] fields = this.GetAllFields(type);
        if (!this.IsQNameShapeValid(fields))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0349",
                ErrorMessage = "USE-QNAME requires exactly two UTF8String components."
            };
        }

        int separator = text.IndexOf(':');
        if (separator != text.LastIndexOf(':'))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0350",
                ErrorMessage = "The USE-QNAME lexical value is not a QName."
            };
        }

        string prefix = separator < 0 ? "" : text.Substring(0, separator);
        string localName = separator < 0
            ? text
            : text.Substring(separator + 1);
        if (!this.IsQNameLocalNameValid(localName)
            || prefix.Length > 0 && !this.Characters.IsValidNcName(prefix))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0350",
                ErrorMessage = "The USE-QNAME lexical value contains an invalid XML name."
            };
        }

        XerXmlNamespaceBinding binding = new XerXmlNamespaceContext(bindings)
            .FindBinding(prefix);
        if (prefix.Length > 0
            && (!binding.Found || binding.NamespaceUri.Length == 0)
            || prefix.Length == 0
                && binding.Found && binding.NamespaceUri.Length > 0)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0350",
                ErrorMessage = "The USE-QNAME namespace binding is absent or invalid."
            };
        }

        AsnRuntimeFieldValue namespaceField = new AsnRuntimeFieldValue
        {
            Name = fields[0].Name,
            Present = prefix.Length > 0
        };
        if (prefix.Length > 0)
        {
            namespaceField.Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = binding.NamespaceUri
            };
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    namespaceField,
                    new AsnRuntimeFieldValue
                    {
                        Name = fields[1].Name,
                        Present = true,
                        Value = new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.CharacterString,
                            TextValue = localName
                        }
                    }
                ]
            }
        };
    }

    internal bool IsQNameShapeValid(AsnFieldDescriptor[] fields)
    {
        if (fields.Length != 2
            || fields[0] == null
            || fields[1] == null)
        {
            return false;
        }

        AsnFieldDescriptor first = fields[0];
        AsnFieldDescriptor second = fields[1];
        AsnTypeDescriptor? firstType = first.Type;
        AsnTypeDescriptor? secondType = second.Type;
        return first.Optional
            && !first.HasDefaultValue
            && firstType != null
            && secondType != null
            && firstType.Kind == AsnTypeKind.Utf8String
            && secondType.Kind == AsnTypeKind.Utf8String;
    }

    internal bool IsQNameLocalNameValid(string value)
    {
        return this.Characters.IsValidNcName(value)
            && (value.Length < 3
                || value.Substring(0, 3).ToUpperInvariant() != "XML");
    }

    internal XerXmlAttribute CreateNamespaceDeclaration(
        string prefix,
        string namespaceUri)
    {
        return new XerXmlAttribute
        {
            NamespaceDeclaration = true,
            NamespacePrefix = prefix,
            NamespaceUri = XerXmlNamespaceContext.XmlnsNamespace,
            Value = namespaceUri
        };
    }

    internal XerValueResult AddNamespaceDeclarationEncoding(
        System.Collections.Generic.List<XerXmlAttribute> attributes,
        string prefix,
        string namespaceUri)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            XerXmlAttribute attribute = attributes[index];
            if (attribute.NamespaceDeclaration
                && attribute.NamespacePrefix == prefix)
            {
                if (attribute.Value == namespaceUri)
                {
                    return new XerValueResult
                    {
                        Success = true,
                        Status = AsnDecodeStatus.Done
                    };
                }

                return this.CreateError(
                    "ASNXER0351",
                    "Two QName values require conflicting namespace prefix bindings.",
                    false);
            }

            index = index + 1;
        }

        attributes.Add(this.CreateNamespaceDeclaration(prefix, namespaceUri));
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done
        };
    }

    internal XerValueResult WrapTextEncoding(
        AsnRuntimeValue value,
        string elementName,
        string text)
    {
        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        if (text.Length > 0)
        {
            content.Add(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Text,
                Value = text
            });
        }

        return this.WrapEncoding(value, elementName, content);
    }

    internal bool IsRestrictedCharacterKind(AsnTypeKind kind)
    {
        return new XerPrimitiveCodec(this.Limits).IsCharacterStringKind(kind);
    }

    internal bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HasCharacterWrapperContent(XerWrappedValue wrapped)
    {
        return wrapped.Empty
            || wrapped.Content.Length == 1
                && (wrapped.Content[0].Kind == XerXmlTokenKind.Text
                    || wrapped.Content[0].Kind == XerXmlTokenKind.CData);
    }

    internal XerWrappedValue ReadModifiedCharacterWrapper(
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return wrapped;
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes)
            || !this.HasCharacterWrapperContent(wrapped))
        {
            return new XerWrappedValue
            {
                ErrorCode = "ASNXER0348",
                ErrorMessage = "The modified value requires character content only."
            };
        }

        return wrapped;
    }

    internal AsnValueDescriptor? FindEnumeratedByNumber(
        AsnTypeDescriptor type,
        System.Numerics.BigInteger number)
    {
        AsnValueDescriptor? result = null;
        int matches = 0;
        AsnValueDescriptor[][] lists =
        [
            type.NamedValues,
            type.RootNamedValues,
            type.ExtensionNamedValues
        ];
        int listIndex = 0;
        while (listIndex < lists.Length)
        {
            int index = 0;
            while (index < lists[listIndex].Length)
            {
                AsnValueDescriptor descriptor = lists[listIndex][index];
                if (descriptor != null
                    && descriptor.Value != null
                    && descriptor.Value.HasIntegerValue
                    && descriptor.Value.IntegerValue == number)
                {
                    if (result != descriptor)
                    {
                        result = descriptor;
                        matches = matches + 1;
                    }
                }

                index = index + 1;
            }

            listIndex = listIndex + 1;
        }

        return matches == 1 ? result : null;
    }

    internal bool IsWhitespaceInstructionValid(AsnTypeDescriptor type)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.Whitespace);
        return instruction != null
            && instruction.Arguments.Length == 1
            && (instruction.Arguments[0] == "REPLACE"
                || instruction.Arguments[0] == "COLLAPSE");
    }

    internal string ReplaceWhitespace(string source)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < source.Length)
        {
            char current = source[index];
            result.Append(current == '\t' || current == '\n' || current == '\r'
                ? ' '
                : current);
            index = index + 1;
        }

        return result.ToString();
    }

    internal string CollapseWhitespace(string source)
    {
        string replaced = this.ReplaceWhitespace(source);
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < replaced.Length)
        {
            if (replaced[index] == ' ')
            {
                pendingSpace = result.Length > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    result.Append(' ');
                }

                result.Append(replaced[index]);
                pendingSpace = false;
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal XerValueResult EncodeTextValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName)
    {
        XerCharacterValueResult character = this.EncodeCharacterValue(
            type,
            value);
        if (!character.Success)
        {
            return this.CreateError(
                character.ErrorCode,
                character.ErrorMessage,
                character.LimitExceeded);
        }

        System.Collections.Generic.List<XerXmlToken> content =
            new System.Collections.Generic.List<XerXmlToken>();
        if (character.Text.Length > 0)
        {
            content.Add(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Text,
                Value = character.Text
            });
        }

        return this.WrapEncoding(value, elementName, content);
    }

    internal XerValueResult DecodeTextValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes)
            || !wrapped.Empty
                && (wrapped.Content.Length != 1
                    || wrapped.Content[0].Kind != XerXmlTokenKind.Text))
        {
            return this.CreateError(
                "ASNXER0338",
                "TEXT requires character content without ordinary attributes.",
                false);
        }

        string text = wrapped.Empty ? "" : wrapped.Content[0].Value;
        XerCharacterValueResult character = this.DecodeCharacterValue(
            type,
            text);
        if (!character.Success || character.Value == null)
        {
            return this.CreateError(
                character.ErrorCode,
                character.ErrorMessage,
                character.LimitExceeded);
        }

        return this.CreateDecodedValue(
            character.Value,
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerValueResult DecodeListValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string elementName,
        int depth)
    {
        XerPrimitiveCodec wrappers = new XerPrimitiveCodec(this.Limits);
        wrappers.AllowWrapperAttributes = true;
        wrappers.AllowWrapperNamespace = true;
        XerWrappedValue wrapped = wrappers.ReadWrapper(
            tokens,
            offset,
            count,
            elementName);
        if (!wrapped.Success)
        {
            return this.CopyWrapperFailure(wrapped);
        }

        if (!this.HasOnlyNamespaceDeclarationAttributes(wrapped.Attributes))
        {
            return this.CreateError(
                "ASNXER0322",
                "A LIST wrapper cannot contain ordinary attributes.",
                false);
        }

        string text = "";
        if (!wrapped.Empty && wrapped.Content.Length > 0)
        {
            if (wrapped.Content.Length != 1
                || wrapped.Content[0].Kind != XerXmlTokenKind.Text)
            {
                return this.CreateError(
                    "ASNXER0323",
                    "A LIST value requires character content only.",
                    false);
            }

            text = wrapped.Content[0].Value;
        }

        XerCharacterValueResult decoded = this.DecodeListText(type, text);
        if (!decoded.Success || decoded.Value == null)
        {
            return this.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return this.CreateDecodedValue(
            decoded.Value,
            tokens,
            offset,
            wrapped.ConsumedCount);
    }

    internal XerCharacterValueResult EncodeListText(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.SetOf
            ? AsnRuntimeValueKind.SetOf
            : AsnRuntimeValueKind.SequenceOf;
        if ((type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf)
            || type.ElementType == null
            || value.Kind != expected)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0324",
                ErrorMessage = "LIST requires a collection-of value and element type."
            };
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Items.Length)
        {
            XerCharacterValueResult item = this.EncodeCharacterValue(
                type.ElementType,
                value.Items[index]);
            if (!item.Success || item.Text.Length == 0
                || this.ContainsXmlWhitespace(item.Text))
            {
                return new XerCharacterValueResult
                {
                    ErrorCode = item.Success ? "ASNXER0325" : item.ErrorCode,
                    ErrorMessage = item.Success
                        ? "A LIST item cannot be empty or contain XML whitespace."
                        : item.ErrorMessage,
                    LimitExceeded = item.LimitExceeded
                };
            }

            if (index > 0)
            {
                text.Append(' ');
            }

            text.Append(item.Text);
            index = index + 1;
        }

        if (!new ResourceCounter(this.Limits).AllowsTextCharacters(text.Length))
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNR0009",
                ErrorMessage = "The LIST value exceeds the configured text limit.",
                LimitExceeded = true
            };
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Text = text.ToString()
        };
    }

    internal XerCharacterValueResult DecodeListText(
        AsnTypeDescriptor type,
        string text)
    {
        if ((type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf)
            || type.ElementType == null)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0324",
                ErrorMessage = "LIST requires a collection-of element type."
            };
        }

        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        int offset = 0;
        while (offset < text.Length)
        {
            while (offset < text.Length && this.IsXmlWhitespace(text[offset]))
            {
                offset = offset + 1;
            }

            int start = offset;
            while (offset < text.Length && !this.IsXmlWhitespace(text[offset]))
            {
                offset = offset + 1;
            }

            if (offset > start)
            {
                XerCharacterValueResult item = this.DecodeCharacterValue(
                    type.ElementType,
                    text.Substring(start, offset - start));
                if (!item.Success || item.Value == null)
                {
                    return item;
                }

                items.Add(item.Value);
                if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
                    items.Count))
                {
                    return new XerCharacterValueResult
                    {
                        ErrorCode = "ASNR0011",
                        ErrorMessage = "The LIST exceeds the configured element limit.",
                        LimitExceeded = true
                    };
                }
            }
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.SetOf
                    ? AsnRuntimeValueKind.SetOf
                    : AsnRuntimeValueKind.SequenceOf,
                Items = items.ToArray()
            }
        };
    }

    internal XerCharacterValueResult EncodeCharacterValue(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        if (this.FindInstruction(type, AsnInstructionKind.UseQName) != null)
        {
            return this.EncodeQNameCharacter(type, value);
        }

        if (this.FindInstruction(type, AsnInstructionKind.Base64) != null)
        {
            return this.ExtractCharacterEncoding(
                this.EncodeBase64Value(type, value, "v", 0));
        }

        if (this.FindInstruction(type, AsnInstructionKind.Decimal) != null)
        {
            return this.ExtractCharacterEncoding(
                this.EncodeDecimalValue(type, value, "v"));
        }

        if (this.FindInstruction(type, AsnInstructionKind.UseNumber) != null)
        {
            return this.ExtractCharacterEncoding(
                this.EncodeNumberValue(type, value, "v"));
        }

        if (this.FindInstruction(type, AsnInstructionKind.List) != null)
        {
            return this.EncodeListText(type, value);
        }

        if (this.FindInstruction(type, AsnInstructionKind.Text) != null
            && type.Kind == AsnTypeKind.Integer
            && value.HasIntegerValue)
        {
            string name = this.FindNamedNumberName(
                type,
                value.IntegerValue);
            if (name.Length > 0)
            {
                return new XerCharacterValueResult
                {
                    Success = true,
                    Text = this.ResolveTextIdentifier(type, name)
                };
            }
        }

        if (this.FindInstruction(type, AsnInstructionKind.Text) != null
            && type.Kind == AsnTypeKind.BitString)
        {
            return this.EncodeTextBitString(type, value);
        }

        XerPrimitiveResult encoded = new XerPrimitiveCodec(this.Limits)
            .EncodePrimitiveNamed(type, value, "v");
        if (!encoded.Success)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = encoded.ErrorCode,
                ErrorMessage = encoded.ErrorMessage,
                LimitExceeded = encoded.LimitExceeded
            };
        }

        string identifier = "";
        string text = "";
        if (encoded.Tokens.Length == 1)
        {
            text = "";
        }
        else if (encoded.Tokens.Length == 3
            && encoded.Tokens[1].Kind == XerXmlTokenKind.Text)
        {
            text = encoded.Tokens[1].Value;
        }
        else if (encoded.Tokens.Length == 3
            && encoded.Tokens[1].Kind == XerXmlTokenKind.EmptyElement)
        {
            identifier = encoded.Tokens[1].LocalName;
            text = this.ResolveTextIdentifier(type, identifier);
        }
        else
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0326",
                ErrorMessage = "The value has no character-encodable XER form."
            };
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Text = text
        };
    }

    internal XerCharacterValueResult DecodeCharacterValue(
        AsnTypeDescriptor type,
        string text)
    {
        if (this.FindInstruction(type, AsnInstructionKind.UseQName) != null)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0350",
                ErrorMessage = "USE-QNAME decoding requires namespace bindings."
            };
        }

        if (this.FindInstruction(type, AsnInstructionKind.Base64) != null
            || this.FindInstruction(type, AsnInstructionKind.Decimal) != null
            || this.FindInstruction(type, AsnInstructionKind.UseNumber) != null)
        {
            XerXmlToken[] modifiedTokens = text.Length == 0
                ? [this.CreateNamedToken(XerXmlTokenKind.EmptyElement, "v")]
                :
                [
                    this.CreateNamedToken(XerXmlTokenKind.StartElement, "v"),
                    new XerXmlToken
                    {
                        Kind = XerXmlTokenKind.Text,
                        Value = text
                    },
                    this.CreateNamedToken(XerXmlTokenKind.EndElement, "v")
                ];
            XerValueResult modifiedDecoded;
            if (this.FindInstruction(type, AsnInstructionKind.Base64) != null)
            {
                modifiedDecoded = this.DecodeBase64Value(
                    type,
                    modifiedTokens,
                    0,
                    modifiedTokens.Length,
                    "v",
                    0);
            }
            else if (this.FindInstruction(
                type,
                AsnInstructionKind.Decimal) != null)
            {
                modifiedDecoded = this.DecodeDecimalValue(
                    type,
                    modifiedTokens,
                    0,
                    modifiedTokens.Length,
                    "v");
            }
            else
            {
                modifiedDecoded = this.DecodeNumberValue(
                    type,
                    modifiedTokens,
                    0,
                    modifiedTokens.Length,
                    "v");
            }

            return new XerCharacterValueResult
            {
                Success = modifiedDecoded.Success,
                Value = modifiedDecoded.Value,
                ErrorCode = modifiedDecoded.ErrorCode,
                ErrorMessage = modifiedDecoded.ErrorMessage,
                LimitExceeded = modifiedDecoded.LimitExceeded
            };
        }

        if (this.FindInstruction(type, AsnInstructionKind.Whitespace) != null)
        {
            AsnEncodingInstruction? whitespace = this.FindInstruction(
                type,
                AsnInstructionKind.Whitespace);
            if (whitespace == null
                || whitespace.Arguments.Length != 1)
            {
                return new XerCharacterValueResult
                {
                    ErrorCode = "ASNXER0347",
                    ErrorMessage = "The WHITESPACE action is malformed."
                };
            }

            text = whitespace.Arguments[0] == "COLLAPSE"
                ? this.CollapseWhitespace(text)
                : this.ReplaceWhitespace(text);
        }

        if (this.FindInstruction(type, AsnInstructionKind.List) != null)
        {
            return this.DecodeListText(type, text);
        }

        if (this.FindInstruction(type, AsnInstructionKind.Text) != null
            && type.Kind == AsnTypeKind.Integer)
        {
            AsnValueDescriptor? named = this.FindNamedNumberByText(type, text);
            if (named != null
                && named.Value != null
                && named.Value.HasIntegerValue)
            {
                return new XerCharacterValueResult
                {
                    Success = true,
                    Value = new AsnRuntimeValue
                    {
                        Kind = AsnRuntimeValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = named.Value.IntegerValue
                    }
                };
            }
        }

        if (this.FindInstruction(type, AsnInstructionKind.Text) != null
            && type.Kind == AsnTypeKind.BitString)
        {
            return this.DecodeTextBitString(type, text);
        }

        System.Collections.Generic.List<XerXmlToken> tokens =
            new System.Collections.Generic.List<XerXmlToken>();
        tokens.Add(this.CreateNamedToken(XerXmlTokenKind.StartElement, "v"));
        if (type.Kind == AsnTypeKind.Boolean
            || type.Kind == AsnTypeKind.Enumerated)
        {
            string identifier = this.FindTextIdentifier(type, text);
            if (identifier.Length == 0)
            {
                return new XerCharacterValueResult
                {
                    ErrorCode = "ASNXER0327",
                    ErrorMessage = "The character value does not select a named value."
                };
            }

            tokens.Add(this.CreateNamedToken(
                XerXmlTokenKind.EmptyElement,
                identifier));
        }
        else if (text.Length > 0)
        {
            tokens.Add(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Text,
                Value = text
            });
        }

        tokens.Add(this.CreateNamedToken(XerXmlTokenKind.EndElement, "v"));
        XerPrimitiveResult decoded = new XerPrimitiveCodec(this.Limits)
            .DecodePrimitiveNamed(type, tokens.ToArray(), 0, tokens.Count, "v");
        return new XerCharacterValueResult
        {
            Success = decoded.Success,
            Value = decoded.Value,
            ErrorCode = decoded.ErrorCode,
            ErrorMessage = decoded.ErrorMessage,
            LimitExceeded = decoded.LimitExceeded
        };
    }

    internal XerCharacterValueResult ExtractCharacterEncoding(
        XerValueResult encoded)
    {
        if (!encoded.Success)
        {
            return new XerCharacterValueResult
            {
                ErrorCode = encoded.ErrorCode,
                ErrorMessage = encoded.ErrorMessage,
                LimitExceeded = encoded.LimitExceeded
            };
        }

        if (encoded.Tokens.Length == 1
            && encoded.Tokens[0].Kind == XerXmlTokenKind.EmptyElement)
        {
            return new XerCharacterValueResult { Success = true };
        }

        if (encoded.Tokens.Length == 3
            && encoded.Tokens[1].Kind == XerXmlTokenKind.Text)
        {
            return new XerCharacterValueResult
            {
                Success = true,
                Text = encoded.Tokens[1].Value
            };
        }

        return new XerCharacterValueResult
        {
            ErrorCode = "ASNXER0352",
            ErrorMessage = "The instruction did not produce character-only content."
        };
    }

    internal string ResolveTextIdentifier(
        AsnTypeDescriptor type,
        string identifier)
    {
        AsnEncodingInstruction? instruction = this.FindInstruction(
            type,
            AsnInstructionKind.Text);
        if (instruction == null)
        {
            return identifier;
        }

        int index = 0;
        while (index < instruction.Targets.Length)
        {
            string target = this.GetTargetIdentifier(instruction.Targets[index]);
            if (target == identifier || target == "ALL")
            {
                if (instruction.Arguments.Length == 0)
                {
                    return identifier;
                }

                if (instruction.Arguments.Length == 2
                    && instruction.Arguments[0] == "AS")
                {
                    return this.ApplyNameAction(identifier, instruction.Arguments[1]);
                }

                return "";
            }

            index = index + 1;
        }

        return identifier;
    }

    internal string FindTextIdentifier(
        AsnTypeDescriptor type,
        string text)
    {
        if (type.Kind == AsnTypeKind.Boolean)
        {
            if (this.ResolveTextIdentifier(type, "true") == text)
            {
                return "true";
            }

            if (this.ResolveTextIdentifier(type, "false") == text)
            {
                return "false";
            }

            return "";
        }

        XerPrimitiveCodec primitives = new XerPrimitiveCodec(this.Limits);
        AsnValueDescriptor[] values = type.NamedValues.Length > 0
            ? type.NamedValues
            : type.RootNamedValues;
        int index = 0;
        while (index < values.Length)
        {
            string name = values[index].Name;
            if (this.ResolveTextIdentifier(type, name) == text
                && primitives.FindEnumeratedDescriptor(type, name) != null)
            {
                return name;
            }

            index = index + 1;
        }

        index = 0;
        while (index < type.ExtensionNamedValues.Length)
        {
            string name = type.ExtensionNamedValues[index].Name;
            if (this.ResolveTextIdentifier(type, name) == text)
            {
                return name;
            }

            index = index + 1;
        }

        return "";
    }

    internal string FindNamedNumberName(
        AsnTypeDescriptor type,
        System.Numerics.BigInteger number)
    {
        AsnValueDescriptor? descriptor = this.FindNamedNumberInValues(
            type.NamedValues,
            number);
        if (descriptor == null)
        {
            descriptor = this.FindNamedNumberInValues(
                type.RootNamedValues,
                number);
        }

        if (descriptor == null)
        {
            descriptor = this.FindNamedNumberInValues(
                type.ExtensionNamedValues,
                number);
        }

        return descriptor == null ? "" : descriptor.Name;
    }

    internal AsnValueDescriptor? FindNamedNumberInValues(
        AsnValueDescriptor[] values,
        System.Numerics.BigInteger number)
    {
        AsnValueDescriptor? result = null;
        int index = 0;
        while (index < values.Length)
        {
            AsnValueDescriptor descriptor = values[index];
            if (descriptor != null
                && descriptor.Value != null
                && descriptor.Value.HasIntegerValue
                && descriptor.Value.IntegerValue == number)
            {
                if (result != null)
                {
                    return null;
                }

                result = descriptor;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnValueDescriptor? FindNamedNumberByText(
        AsnTypeDescriptor type,
        string text)
    {
        AsnValueDescriptor? result = this.FindNamedNumberTextInValues(
            type,
            type.NamedValues,
            text);
        if (result == null)
        {
            result = this.FindNamedNumberTextInValues(
                type,
                type.RootNamedValues,
                text);
        }

        if (result == null)
        {
            result = this.FindNamedNumberTextInValues(
                type,
                type.ExtensionNamedValues,
                text);
        }

        return result;
    }

    internal AsnValueDescriptor? FindNamedNumberTextInValues(
        AsnTypeDescriptor type,
        AsnValueDescriptor[] values,
        string text)
    {
        AsnValueDescriptor? result = null;
        int index = 0;
        while (index < values.Length)
        {
            AsnValueDescriptor descriptor = values[index];
            if (descriptor != null
                && descriptor.Value != null
                && descriptor.Value.HasIntegerValue
                && this.ResolveTextIdentifier(type, descriptor.Name) == text)
            {
                if (result != null)
                {
                    return null;
                }

                result = descriptor;
            }

            index = index + 1;
        }

        return result;
    }

    internal XerCharacterValueResult EncodeTextBitString(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnBitStringValue? bits = value.BitStringValue;
        if (value.Kind != AsnRuntimeValueKind.BitString
            || bits == null
            || !bits.IsValid())
        {
            return new XerCharacterValueResult
            {
                ErrorCode = "ASNXER0328",
                ErrorMessage = "TEXT requires a valid named BIT STRING value."
            };
        }

        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int bitIndex = 0;
        while (bitIndex < bits.BitLength)
        {
            if (this.IsBitSet(bits, bitIndex))
            {
                string name = this.FindNamedBit(type, bitIndex);
                if (name.Length == 0)
                {
                    return new XerCharacterValueResult
                    {
                        ErrorCode = "ASNXER0328",
                        ErrorMessage = "TEXT cannot represent an unnamed set bit."
                    };
                }

                names.Add(this.ResolveTextIdentifier(type, name));
            }

            bitIndex = bitIndex + 1;
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Text = string.Join(" ", names.ToArray())
        };
    }

    internal XerCharacterValueResult DecodeTextBitString(
        AsnTypeDescriptor type,
        string text)
    {
        System.Collections.Generic.List<int> bits =
            new System.Collections.Generic.List<int>();
        int offset = 0;
        int highest = -1;
        while (offset < text.Length)
        {
            while (offset < text.Length && this.IsXmlWhitespace(text[offset]))
            {
                offset = offset + 1;
            }

            int start = offset;
            while (offset < text.Length && !this.IsXmlWhitespace(text[offset]))
            {
                offset = offset + 1;
            }

            if (offset > start)
            {
                int bitIndex = this.FindNamedBitByText(
                    type,
                    text.Substring(start, offset - start));
                if (bitIndex < 0 || bits.Contains(bitIndex))
                {
                    return new XerCharacterValueResult
                    {
                        ErrorCode = "ASNXER0329",
                        ErrorMessage = "A TEXT BIT STRING contains an unknown or duplicate name."
                    };
                }

                bits.Add(bitIndex);
                if (bitIndex > highest)
                {
                    highest = bitIndex;
                }
            }
        }

        byte[] bytes = highest < 0 ? [] : new byte[(highest + 8) / 8];
        int index = 0;
        while (index < bits.Count)
        {
            int bitIndex = bits[index];
            bytes[bitIndex / 8] = (byte)(bytes[bitIndex / 8]
                | 1 << (7 - bitIndex % 8));
            index = index + 1;
        }

        return new XerCharacterValueResult
        {
            Success = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = bytes,
                    BitLength = highest + 1
                }
            }
        };
    }

    internal int FindNamedBitByText(
        AsnTypeDescriptor type,
        string text)
    {
        int result = this.FindNamedBitTextInValues(
            type,
            type.NamedValues,
            text);
        if (result < 0)
        {
            result = this.FindNamedBitTextInValues(
                type,
                type.RootNamedValues,
                text);
        }

        if (result < 0)
        {
            result = this.FindNamedBitTextInValues(
                type,
                type.ExtensionNamedValues,
                text);
        }

        return result;
    }

    internal int FindNamedBitTextInValues(
        AsnTypeDescriptor type,
        AsnValueDescriptor[] values,
        string text)
    {
        int result = -1;
        int index = 0;
        while (index < values.Length)
        {
            AsnValueDescriptor descriptor = values[index];
            if (descriptor != null
                && descriptor.Value != null
                && descriptor.Value.HasIntegerValue
                && descriptor.Value.IntegerValue >= System.Numerics.BigInteger.Zero
                && descriptor.Value.IntegerValue
                    <= new System.Numerics.BigInteger(int.MaxValue)
                && this.ResolveTextIdentifier(type, descriptor.Name) == text)
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

    internal string GetTargetIdentifier(string target)
    {
        int separator = target.LastIndexOf(':');
        return separator < 0 ? target : target.Substring(separator + 1);
    }

    internal bool ContainsXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (this.IsXmlWhitespace(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal XerValueResult AddAttributeEncoding(
        AsnTypeDescriptor type,
        string defaultName,
        string value,
        System.Collections.Generic.List<XerXmlAttribute> destination)
    {
        XerExtendedName identity = this.ResolveExtendedName(type, defaultName);
        if (!identity.Success)
        {
            return this.CreateError(
                "ASNXER0320",
                identity.ErrorMessage,
                false);
        }

        int index = 0;
        while (index < destination.Count)
        {
            XerXmlAttribute existing = destination[index];
            if (existing.LocalName == identity.LocalName
                && existing.NamespaceUri == identity.NamespaceUri)
            {
                return this.CreateError(
                    "ASNXER0334",
                    "Two attributes have the same expanded XML name.",
                    false);
            }

            index = index + 1;
        }

        destination.Add(new XerXmlAttribute
        {
            Name = identity.LocalName,
            Prefix = identity.Prefix,
            LocalName = identity.LocalName,
            NamespaceUri = identity.NamespaceUri,
            Value = value
        });
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Tokens = []
        };
    }

    internal XerValueResult DecodeAttributeValue(
        AsnFieldDescriptor descriptor,
        XerXmlAttribute[] attributes,
        XerXmlNamespaceBinding[] bindings)
    {
        if (descriptor.Type == null)
        {
            return this.CreateError(
                "ASNXER0121",
                "An EXTENDED-XER attribute has no resolved type.",
                false);
        }

        XerExtendedName identity = this.ResolveExtendedName(
            descriptor.Type,
            descriptor.Name);
        if (!identity.Success)
        {
            return this.CreateError(
                "ASNXER0320",
                identity.ErrorMessage,
                false);
        }

        XerXmlAttribute? found = null;
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration
                && attribute.LocalName == identity.LocalName
                && attribute.NamespaceUri == identity.NamespaceUri)
            {
                if (found != null)
                {
                    return this.CreateError(
                        "ASNXER0334",
                        "Two attributes have the same expanded XML name.",
                        false);
                }

                found = attribute;
            }

            index = index + 1;
        }

        if (found == null)
        {
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done
            };
        }

        XerCharacterValueResult decoded = this.FindInstruction(
            descriptor.Type,
            AsnInstructionKind.UseQName) == null
                ? this.DecodeCharacterValue(descriptor.Type, found.Value)
                : this.DecodeQNameCharacter(
                    descriptor.Type,
                    found.Value,
                    bindings);
        if (!decoded.Success)
        {
            return this.CreateError(
                decoded.ErrorCode,
                decoded.ErrorMessage,
                decoded.LimitExceeded);
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = decoded.Value
        };
    }

    internal bool ValidateCollectionAttributes(
        AsnFieldDescriptor[] descriptors,
        XerXmlAttribute[] attributes,
        bool hasWildcard)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration)
            {
                int matches = 0;
                int descriptorIndex = 0;
                while (descriptorIndex < descriptors.Length)
                {
                    AsnFieldDescriptor descriptor = descriptors[descriptorIndex];
                    if (descriptor != null
                        && descriptor.Type != null
                        && this.FindInstruction(
                            descriptor.Type,
                            AsnInstructionKind.Attribute) != null)
                    {
                        XerExtendedName identity = this.ResolveExtendedName(
                            descriptor.Type,
                            descriptor.Name);
                        if (!identity.Success)
                        {
                            return false;
                        }

                        if (identity.LocalName == attribute.LocalName
                            && identity.NamespaceUri == attribute.NamespaceUri)
                        {
                            matches = matches + 1;
                        }
                    }

                    descriptorIndex = descriptorIndex + 1;
                }

                if (matches > 1 || matches == 0 && !hasWildcard)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return true;
    }

    internal XerXmlAttribute[] GetWildcardAttributes(
        XerXmlAttribute[] attributes,
        AsnFieldDescriptor[] descriptors)
    {
        System.Collections.Generic.List<XerXmlAttribute> result =
            new System.Collections.Generic.List<XerXmlAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            bool known = attribute.NamespaceDeclaration;
            int descriptorIndex = 0;
            while (!known && descriptorIndex < descriptors.Length)
            {
                AsnFieldDescriptor descriptor = descriptors[descriptorIndex];
                if (descriptor != null
                    && descriptor.Type != null
                    && this.FindInstruction(
                        descriptor.Type,
                        AsnInstructionKind.Attribute) != null)
                {
                    XerExtendedName identity = this.ResolveExtendedName(
                        descriptor.Type,
                        descriptor.Name);
                    known = identity.Success
                        && identity.LocalName == attribute.LocalName
                        && identity.NamespaceUri == attribute.NamespaceUri;
                }

                descriptorIndex = descriptorIndex + 1;
            }

            if (!known)
            {
                result.Add(attribute);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool MatchesElementName(
        AsnTypeDescriptor type,
        string defaultName,
        XerXmlToken token)
    {
        if (this.FindInstruction(type, AsnInstructionKind.AnyElement) != null)
        {
            return true;
        }

        XerExtendedName identity = this.ResolveExtendedName(type, defaultName);
        return identity.Success
            && identity.LocalName == token.LocalName
            && identity.NamespaceUri == token.NamespaceUri;
    }

    internal int FindDescriptorIndexForElement(
        AsnFieldDescriptor[] descriptors,
        XerXmlToken token,
        bool[] seen)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool unavailable = index < seen.Length && seen[index];
            if (!unavailable
                && descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Attribute) == null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyAttributes) == null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyElement) == null
                && this.MatchesElementName(
                    descriptor.Type,
                    descriptor.Name,
                    token))
            {
                if (result >= 0)
                {
                    return -2;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal XerValueResult RemoveUntaggedWrapper(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        XerValueResult encoded)
    {
        XerCharacterValueResult character = this.EncodeCharacterValue(
            type,
            value);
        if (character.Success)
        {
            XerXmlToken[] tokens = character.Text.Length == 0
                ? []
                : [new XerXmlToken
                {
                    Kind = XerXmlTokenKind.Text,
                    Value = character.Text
                }];
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = value,
                Tokens = tokens,
                ConsumedCount = tokens.Length
            };
        }

        if (encoded.Tokens.Length == 1
            && encoded.Tokens[0].Kind == XerXmlTokenKind.EmptyElement)
        {
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = value,
                Tokens = []
            };
        }

        if (encoded.Tokens.Length < 2
            || encoded.Tokens[0].Kind != XerXmlTokenKind.StartElement
            || encoded.Tokens[encoded.Tokens.Length - 1].Kind
                != XerXmlTokenKind.EndElement)
        {
            return this.CreateError(
                "ASNXER0335",
                "UNTAGGED cannot remove the wrapper from this encoding.",
                false);
        }

        XerXmlToken[] content = new XerXmlToken[encoded.Tokens.Length - 2];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = encoded.Tokens[index + 1];
            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens = content,
            ConsumedCount = content.Length
        };
    }

    internal bool CanStartUntaggedValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset)
    {
        if (offset >= tokens.Length)
        {
            return false;
        }

        XerXmlToken token = tokens[offset];
        if (token.Kind == XerXmlTokenKind.Text
            || token.Kind == XerXmlTokenKind.CData)
        {
            return type.Kind != AsnTypeKind.Sequence
                && type.Kind != AsnTypeKind.Set
                && type.Kind != AsnTypeKind.Choice
                && type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf;
        }

        if (!this.IsElement(token))
        {
            return false;
        }

        if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set
            || type.Kind == AsnTypeKind.Choice)
        {
            AsnFieldDescriptor[] fields = this.GetAllFields(type);
            return this.FindDescriptorIndexForElement(fields, token, []) >= 0
                || this.FindAnyElementDescriptorIndex(fields, []) >= 0;
        }

        if ((type.Kind == AsnTypeKind.SequenceOf
                || type.Kind == AsnTypeKind.SetOf)
            && type.ElementType != null)
        {
            XerNameResult name = this.Names.GetElementName(type.ElementType);
            return name.Success
                && this.MatchesElementName(type.ElementType, name.Name, token);
        }

        return false;
    }

    internal int FindUntaggedContentCount(
        AsnFieldDescriptor[] descriptors,
        int descriptorIndex,
        XerXmlToken[] content,
        int offset)
    {
        if (offset >= content.Length)
        {
            return 0;
        }

        if (content[offset].Kind == XerXmlTokenKind.Text
            || content[offset].Kind == XerXmlTokenKind.CData)
        {
            return 1;
        }

        int index = offset;
        bool consumed = false;
        while (index < content.Length)
        {
            if (consumed
                && this.IsElement(content[index])
                && this.MatchesLaterDescriptor(
                    descriptors,
                    descriptorIndex + 1,
                    content[index]))
            {
                break;
            }

            int childCount = this.GetElementTokenCount(content, index);
            if (childCount == 0)
            {
                break;
            }

            index = index + childCount;
            consumed = true;
            index = this.SkipIgnorable(content, index);
        }

        return index - offset;
    }

    internal bool MatchesLaterDescriptor(
        AsnFieldDescriptor[] descriptors,
        int start,
        XerXmlToken token)
    {
        int index = start;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Attribute) == null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyAttributes) == null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Untagged) == null
                && this.MatchesElementName(
                    descriptor.Type,
                    descriptor.Name,
                    token))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XerValueResult DecodeUntaggedValue(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset,
        int count,
        string defaultName,
        int depth)
    {
        if (count == 1
            && (tokens[offset].Kind == XerXmlTokenKind.Text
                || tokens[offset].Kind == XerXmlTokenKind.CData))
        {
            XerCharacterValueResult character = this.DecodeCharacterValue(
                type,
                tokens[offset].Value);
            if (!character.Success || character.Value == null)
            {
                return this.CreateError(
                    character.ErrorCode,
                    character.ErrorMessage,
                    character.LimitExceeded);
            }

            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = character.Value,
                ConsumedCount = 1
            };
        }

        XerExtendedName identity = this.ResolveExtendedName(type, defaultName);
        if (!identity.Success)
        {
            return this.CreateError("ASNXER0320", identity.ErrorMessage, false);
        }

        XerXmlToken[] wrapped = new XerXmlToken[count + 2];
        wrapped[0] = new XerXmlToken { Kind = XerXmlTokenKind.StartElement };
        this.SetTokenIdentity(wrapped[0], identity);
        int index = 0;
        while (index < count)
        {
            wrapped[index + 1] = tokens[offset + index];
            index = index + 1;
        }

        wrapped[wrapped.Length - 1] = new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement
        };
        this.SetTokenIdentity(wrapped[wrapped.Length - 1], identity);
        XerValueResult decoded = this.DecodeValueAtDepth(
            type,
            wrapped,
            0,
            wrapped.Length,
            defaultName,
            depth);
        if (decoded.Success)
        {
            decoded.ConsumedCount = count;
        }

        return decoded;
    }

    internal int FindUntaggedDescriptorIndex(
        AsnFieldDescriptor[] descriptors,
        XerXmlToken[] tokens,
        int offset,
        bool[] seen)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool unavailable = index < seen.Length && seen[index];
            if (!unavailable
                && descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.Untagged) != null
                && this.CanStartUntaggedValue(
                    descriptor.Type,
                    tokens,
                    offset))
            {
                if (result >= 0)
                {
                    return -2;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindUntaggedItemCount(
        AsnTypeDescriptor type,
        XerXmlToken[] tokens,
        int offset)
    {
        if (tokens[offset].Kind == XerXmlTokenKind.Text
            || tokens[offset].Kind == XerXmlTokenKind.CData)
        {
            return 1;
        }

        int index = offset;
        bool consumed = false;
        while (index < tokens.Length)
        {
            if (consumed && this.CanStartUntaggedValue(type, tokens, index))
            {
                break;
            }

            int childCount = this.GetElementTokenCount(tokens, index);
            if (childCount == 0)
            {
                break;
            }

            index = index + childCount;
            consumed = true;
            index = this.SkipIgnorable(tokens, index);
        }

        return index - offset;
    }

    internal int FindAnyAttributesDescriptorIndex(
        AsnFieldDescriptor[] descriptors)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            if (descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyAttributes) != null)
            {
                if (result >= 0)
                {
                    return -2;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindAnyElementDescriptorIndex(
        AsnFieldDescriptor[] descriptors,
        bool[] seen)
    {
        int result = -1;
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnFieldDescriptor descriptor = descriptors[index];
            bool alreadySeen = index < seen.Length && seen[index];
            if (!alreadySeen
                && descriptor != null
                && descriptor.Type != null
                && this.FindInstruction(
                    descriptor.Type,
                    AsnInstructionKind.AnyElement) != null)
            {
                if (result >= 0)
                {
                    return -2;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool HasOnlyNamespaceDeclarationAttributes(
        XerXmlAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (!attributes[index].NamespaceDeclaration)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XerWildcardRestriction ParseWildcardRestriction(
        AsnEncodingInstruction? instruction)
    {
        if (instruction == null || instruction.Arguments.Length == 0)
        {
            return new XerWildcardRestriction { Success = true };
        }

        string first = instruction.Arguments[0];
        if (first != "FROM" && first != "EXCEPT")
        {
            return new XerWildcardRestriction
            {
                ErrorMessage = "A wildcard namespace restriction must begin with FROM or EXCEPT."
            };
        }

        System.Collections.Generic.List<string> uris =
            new System.Collections.Generic.List<string>();
        bool absent = false;
        int index = 1;
        while (index < instruction.Arguments.Length)
        {
            string argument = instruction.Arguments[index];
            if (argument == "ABSENT")
            {
                if (absent)
                {
                    return new XerWildcardRestriction
                    {
                        ErrorMessage = "A wildcard URI list cannot repeat ABSENT."
                    };
                }

                absent = true;
            }
            else
            {
                string uri = this.DecodeInstructionString(argument);
                if (uri.Length == 0 || uris.Contains(uri))
                {
                    return new XerWildcardRestriction
                    {
                        ErrorMessage = uri.Length == 0
                            ? "A wildcard URI list contains an invalid quoted URI."
                            : "A wildcard URI list cannot repeat a namespace URI."
                    };
                }

                uris.Add(uri);
            }

            index = index + 1;
        }

        if (instruction.Arguments.Length == 1)
        {
            return new XerWildcardRestriction
            {
                ErrorMessage = "A wildcard namespace restriction requires a URI or ABSENT."
            };
        }

        return new XerWildcardRestriction
        {
            Success = true,
            Present = true,
            From = first == "FROM",
            Absent = absent,
            NamespaceUris = uris.ToArray()
        };
    }

    internal string DecodeInstructionString(string value)
    {
        if (value.Length < 2
            || value[0] != '"'
            || value[value.Length - 1] != '"')
        {
            return "";
        }

        string content = value.Substring(1, value.Length - 2);
        return content.Replace("\"\"", "\"");
    }

    internal bool IsWildcardNamespaceAllowed(
        XerWildcardRestriction restriction,
        string namespaceUri)
    {
        if (!restriction.Present)
        {
            return true;
        }

        bool absent = namespaceUri.Length == 0;
        bool listed = absent
            ? restriction.Absent
            : new System.Collections.Generic.List<string>(
                restriction.NamespaceUris).Contains(namespaceUri);
        return restriction.From ? listed : !listed;
    }

    internal XerValueResult EncodeAnyAttributes(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        System.Collections.Generic.List<XerXmlAttribute> destination)
    {
        AsnRuntimeValueKind expected = type.Kind == AsnTypeKind.SetOf
            ? AsnRuntimeValueKind.SetOf
            : AsnRuntimeValueKind.SequenceOf;
        if ((type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf)
            || type.ElementType == null
            || type.ElementType.Kind != AsnTypeKind.Utf8String
            || value.Kind != expected)
        {
            return this.CreateError(
                "ASNXER0310",
                "ANY-ATTRIBUTES requires a sequence-of or set-of UTF8String value.",
                false);
        }

        XerWildcardRestriction restriction = this.ParseWildcardRestriction(
            this.FindInstruction(type, AsnInstructionKind.AnyAttributes));
        if (!restriction.Success)
        {
            return this.CreateError(
                "ASNXER0302",
                restriction.ErrorMessage,
                false);
        }

        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            destination.Count + value.Items.Length))
        {
            return this.CreateError(
                "ASNR0011",
                "The ANY-ATTRIBUTES value exceeds the configured attribute limit.",
                true);
        }

        int index = 0;
        while (index < value.Items.Length)
        {
            AsnRuntimeValue item = value.Items[index];
            if (item == null
                || item.Kind != AsnRuntimeValueKind.CharacterString)
            {
                return this.CreateError(
                    "ASNXER0311",
                    "Each ANY-ATTRIBUTES item must be a UTF8String.",
                    false);
            }

            XerAnyAttributeResult parsed = this.ParseAnyAttribute(
                item.TextValue,
                restriction);
            if (!parsed.Success || parsed.Attribute == null)
            {
                return this.CreateError(
                    "ASNXER0312",
                    parsed.ErrorMessage,
                    false);
            }

            int existingIndex = 0;
            while (existingIndex < destination.Count)
            {
                XerXmlAttribute existing = destination[existingIndex];
                if (existing.NamespaceUri == parsed.Attribute.NamespaceUri
                    && existing.LocalName == parsed.Attribute.LocalName)
                {
                    return this.CreateError(
                        "ASNXER0313",
                        "ANY-ATTRIBUTES contains a duplicate expanded attribute name.",
                        false);
                }

                existingIndex = existingIndex + 1;
            }

            destination.Add(parsed.Attribute);
            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value
        };
    }

    internal XerAnyAttributeResult ParseAnyAttribute(
        string text,
        XerWildcardRestriction restriction)
    {
        int equalsIndex = text.IndexOf('=');
        if (equalsIndex <= 0 || equalsIndex + 2 >= text.Length)
        {
            return new XerAnyAttributeResult
            {
                ErrorMessage = "An ANY-ATTRIBUTES item has an invalid name/value format."
            };
        }

        string namePart = text.Substring(0, equalsIndex);
        int whitespaceIndex = -1;
        int index = 0;
        while (index < namePart.Length)
        {
            if (char.IsWhiteSpace(namePart[index]))
            {
                whitespaceIndex = index;
            }

            index = index + 1;
        }

        string namespaceUri = whitespaceIndex < 0
            ? ""
            : namePart.Substring(0, whitespaceIndex).Trim();
        string localName = whitespaceIndex < 0
            ? namePart
            : namePart.Substring(whitespaceIndex + 1);
        if (!this.Characters.IsValidNcName(localName)
            || (whitespaceIndex >= 0 && namespaceUri.Length == 0)
            || !this.IsWildcardNamespaceAllowed(restriction, namespaceUri))
        {
            return new XerAnyAttributeResult
            {
                ErrorMessage = "An ANY-ATTRIBUTES item has an invalid or disallowed namespace/name."
            };
        }

        string lexicalValue = text.Substring(equalsIndex + 1);
        char quote = lexicalValue[0];
        if ((quote != '"' && quote != '\'')
            || lexicalValue[lexicalValue.Length - 1] != quote)
        {
            return new XerAnyAttributeResult
            {
                ErrorMessage = "An ANY-ATTRIBUTES item requires a quoted XML attribute value."
            };
        }

        string wrapper = "<w " + localName + "=" + lexicalValue + "/>";
        XerXmlReadResult read = new XerXmlTokenReader(this.Limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(wrapper),
                IsFinalBlock = true
            });
        if (read.Status != AsnDecodeStatus.Done
            || read.Tokens.Length != 1
            || read.Tokens[0].Attributes.Length != 1)
        {
            return new XerAnyAttributeResult
            {
                ErrorMessage = "An ANY-ATTRIBUTES item contains an invalid XML attribute value."
            };
        }

        XerXmlAttribute source = read.Tokens[0].Attributes[0];
        return new XerAnyAttributeResult
        {
            Success = true,
            Attribute = new XerXmlAttribute
            {
                LocalName = localName,
                NamespaceUri = namespaceUri,
                Value = source.Value
            }
        };
    }

    internal XerValueResult DecodeAnyAttributesValue(
        AsnTypeDescriptor type,
        XerXmlAttribute[] attributes)
    {
        if ((type.Kind != AsnTypeKind.SequenceOf
                && type.Kind != AsnTypeKind.SetOf)
            || type.ElementType == null
            || type.ElementType.Kind != AsnTypeKind.Utf8String)
        {
            return this.CreateError(
                "ASNXER0310",
                "ANY-ATTRIBUTES requires a sequence-of or set-of UTF8String type.",
                false);
        }

        XerWildcardRestriction restriction = this.ParseWildcardRestriction(
            this.FindInstruction(type, AsnInstructionKind.AnyAttributes));
        if (!restriction.Success)
        {
            return this.CreateError(
                "ASNXER0302",
                restriction.ErrorMessage,
                false);
        }

        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        int index = 0;
        while (index < attributes.Length)
        {
            XerXmlAttribute attribute = attributes[index];
            if (!attribute.NamespaceDeclaration
                && attribute.NamespaceUri != "urn:oid:2.1.5.2.0.1")
            {
                if (!this.IsWildcardNamespaceAllowed(
                    restriction,
                    attribute.NamespaceUri))
                {
                    return this.CreateError(
                        "ASNXER0314",
                        "An XML attribute violates the ANY-ATTRIBUTES namespace restriction.",
                        false);
                }

                string prefix = attribute.NamespaceUri.Length == 0
                    ? ""
                    : attribute.NamespaceUri + " ";
                items.Add(new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = prefix + attribute.LocalName + "=\""
                        + this.EscapeAnyAttributeValue(attribute.Value) + "\""
                });
            }

            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = new AsnRuntimeValue
            {
                Kind = type.Kind == AsnTypeKind.SetOf
                    ? AsnRuntimeValueKind.SetOf
                    : AsnRuntimeValueKind.SequenceOf,
                Items = items.ToArray()
            }
        };
    }

    internal string EscapeAnyAttributeValue(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '&')
            {
                result.Append("&amp;");
            }
            else if (current == '<')
            {
                result.Append("&lt;");
            }
            else if (current == '"')
            {
                result.Append("&quot;");
            }
            else if (current == '\r')
            {
                result.Append("&#xD;");
            }
            else if (current == '\n')
            {
                result.Append("&#xA;");
            }
            else if (current == '\t')
            {
                result.Append("&#x9;");
            }
            else
            {
                result.Append(current);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal byte[] SerializeWildcardElement(
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        XerXmlToken[] copied = new XerXmlToken[count];
        int index = 0;
        while (index < count)
        {
            copied[index] = tokens[offset + index];
            index = index + 1;
        }

        XerXmlToken source = copied[0];
        System.Collections.Generic.List<XerXmlAttribute> attributes =
            new System.Collections.Generic.List<XerXmlAttribute>();
        index = 0;
        while (index < source.Attributes.Length)
        {
            attributes.Add(source.Attributes[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.NamespaceBindings.Length)
        {
            XerXmlNamespaceBinding binding = source.NamespaceBindings[index];
            bool reserved = binding.Prefix == "xml" || binding.Prefix == "xmlns";
            bool declared = false;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Count)
            {
                XerXmlAttribute attribute = attributes[attributeIndex];
                if (attribute.NamespaceDeclaration
                    && attribute.NamespacePrefix == binding.Prefix)
                {
                    declared = true;
                }

                attributeIndex = attributeIndex + 1;
            }

            if (!reserved && !declared && binding.NamespaceUri.Length > 0)
            {
                attributes.Add(new XerXmlAttribute
                {
                    NamespaceDeclaration = true,
                    NamespacePrefix = binding.Prefix,
                    NamespaceUri = XerXmlNamespaceContext.XmlnsNamespace,
                    Value = binding.NamespaceUri
                });
            }

            index = index + 1;
        }

        copied[0] = new XerXmlToken
        {
            Kind = source.Kind,
            Name = source.Name,
            Prefix = source.Prefix,
            LocalName = source.LocalName,
            NamespaceUri = source.NamespaceUri,
            Value = source.Value,
            Attributes = attributes.ToArray(),
            NamespaceBindings = source.NamespaceBindings,
            ByteOffset = source.ByteOffset,
            ByteLength = source.ByteLength
        };
        return this.SerializeTokenRange(copied, 0, count);
    }

    internal XerValueResult WrapEncoding(
        AsnRuntimeValue value,
        string elementName,
        System.Collections.Generic.List<XerXmlToken> content)
    {
        if (content.Count == 0)
        {
            return new XerValueResult
            {
                Success = true,
                Status = AsnDecodeStatus.Done,
                Value = value,
                Tokens = [this.CreateNamedToken(
                    XerXmlTokenKind.EmptyElement,
                    elementName)],
                ConsumedCount = 1
            };
        }

        System.Collections.Generic.List<XerXmlToken> tokens =
            new System.Collections.Generic.List<XerXmlToken>();
        tokens.Add(this.CreateNamedToken(
            XerXmlTokenKind.StartElement,
            elementName));
        int index = 0;
        while (index < content.Count)
        {
            tokens.Add(content[index]);
            index = index + 1;
        }

        tokens.Add(this.CreateNamedToken(
            XerXmlTokenKind.EndElement,
            elementName));
        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens = tokens.ToArray(),
            ConsumedCount = tokens.Count
        };
    }

    internal XerValueResult CreateDecodedValue(
        AsnRuntimeValue value,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        XerXmlToken[] encoded = new XerXmlToken[count];
        int index = 0;
        while (index < count)
        {
            encoded[index] = tokens[offset + index];
            index = index + 1;
        }

        return new XerValueResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens = encoded,
            ConsumedCount = count
        };
    }

    internal AsnFieldDescriptor[] GetAllFields(AsnTypeDescriptor type)
    {
        if (type.Fields.Length > 0)
        {
            return type.Fields;
        }

        AsnFieldDescriptor[] fields = new AsnFieldDescriptor[
            type.RootFields.Length + type.ExtensionFields.Length];
        int index = 0;
        while (index < type.RootFields.Length)
        {
            fields[index] = type.RootFields[index];
            index = index + 1;
        }

        int extensionIndex = 0;
        while (extensionIndex < type.ExtensionFields.Length)
        {
            fields[index] = type.ExtensionFields[extensionIndex];
            extensionIndex = extensionIndex + 1;
            index = index + 1;
        }

        return fields;
    }

    internal AsnRuntimeFieldValue? FindRuntimeField(
        AsnRuntimeFieldValue[] fields,
        string name)
    {
        AsnRuntimeFieldValue? result = null;
        int index = 0;
        while (index < fields.Length)
        {
            AsnRuntimeFieldValue field = fields[index];
            if (field != null && field.Name == name)
            {
                if (result != null)
                {
                    return null;
                }

                result = field;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnFieldDescriptor? FindDescriptor(
        AsnFieldDescriptor[] fields,
        string name)
    {
        int index = this.FindDescriptorIndex(fields, name);
        return index < 0 ? null : fields[index];
    }

    internal int FindDescriptorIndex(
        AsnFieldDescriptor[] fields,
        string name)
    {
        int result = -1;
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] != null && fields[index].Name == name)
            {
                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool ValidateRuntimeFields(
        AsnRuntimeFieldValue[] values,
        AsnFieldDescriptor[] descriptors)
    {
        int index = 0;
        while (index < values.Length)
        {
            AsnRuntimeFieldValue value = values[index];
            if (value == null
                || this.FindDescriptor(descriptors, value.Name) == null)
            {
                return false;
            }

            int duplicate = index + 1;
            while (duplicate < values.Length)
            {
                if (values[duplicate] != null
                    && values[duplicate].Name == value.Name)
                {
                    return false;
                }

                duplicate = duplicate + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal int SkipIgnorable(XerXmlToken[] tokens, int offset)
    {
        int index = offset;
        while (index < tokens.Length)
        {
            XerXmlToken token = tokens[index];
            bool whitespace = (token.Kind == XerXmlTokenKind.Text
                    || token.Kind == XerXmlTokenKind.CData)
                && token.Value.Trim().Length == 0;
            if (!whitespace
                && token.Kind != XerXmlTokenKind.Comment
                && token.Kind != XerXmlTokenKind.ProcessingInstruction)
            {
                break;
            }

            index = index + 1;
        }

        return index;
    }

    internal bool IsElement(XerXmlToken token)
    {
        return token.Kind == XerXmlTokenKind.StartElement
            || token.Kind == XerXmlTokenKind.EmptyElement;
    }

    internal int GetElementTokenCount(XerXmlToken[] tokens, int offset)
    {
        if (offset < 0 || offset >= tokens.Length)
        {
            return 0;
        }

        if (tokens[offset].Kind == XerXmlTokenKind.EmptyElement)
        {
            return 1;
        }

        if (tokens[offset].Kind != XerXmlTokenKind.StartElement)
        {
            return 0;
        }

        int depth = 1;
        int index = offset + 1;
        while (index < tokens.Length && depth > 0)
        {
            if (tokens[index].Kind == XerXmlTokenKind.StartElement)
            {
                depth = depth + 1;
            }
            else if (tokens[index].Kind == XerXmlTokenKind.EndElement)
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return depth == 0 ? index - offset : 0;
    }

    internal void AppendTokens(
        System.Collections.Generic.List<XerXmlToken> target,
        XerXmlToken[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
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

    internal AsnRuntimeValueKind GetUsefulRuntimeKind(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.External)
        {
            return AsnRuntimeValueKind.External;
        }

        if (kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnRuntimeValueKind.EmbeddedPdv;
        }

        if (kind == AsnTypeKind.InstanceOf)
        {
            return AsnRuntimeValueKind.InstanceOf;
        }

        return AsnRuntimeValueKind.CharacterString;
    }

    internal XerPrimitiveResult CanonicalizePrimitiveEncoding(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        XerPrimitiveResult encoded)
    {
        XerPrimitiveCodec primitives = new XerPrimitiveCodec(this.Limits);
        if (type.Kind == AsnTypeKind.Real)
        {
            string basicText = this.GetSingleTextToken(encoded.Tokens);
            if (basicText.Length == 0)
            {
                return encoded;
            }

            string canonicalText = this.FormatCanonicalReal(
                value.RealValue,
                basicText,
                primitives);
            if (canonicalText.Length == 0)
            {
                return primitives.CreateError(
                    "ASNXER0202",
                    "The REAL value cannot be represented in canonical XER.",
                    false);
            }

            return primitives.CreateTextEncoding(
                value,
                elementName,
                canonicalText);
        }

        if (type.Kind == AsnTypeKind.BitString
            && this.HasNamedBitList(type))
        {
            return this.EncodeCanonicalNamedBits(
                type,
                value,
                elementName,
                primitives);
        }

        if (type.Kind == AsnTypeKind.UtcTime
            || type.Kind == AsnTypeKind.GeneralizedTime
            || type.Kind == AsnTypeKind.Time
            || type.Kind == AsnTypeKind.Date
            || type.Kind == AsnTypeKind.TimeOfDay
            || type.Kind == AsnTypeKind.DateTime
            || type.Kind == AsnTypeKind.Duration)
        {
            if (value.TimeValue == null)
            {
                return primitives.CreateError(
                    "ASNXER0203",
                    "The time value cannot be represented in canonical XER.",
                    false);
            }

            DerTimeNormalizationResult normalized = new DerTimeNormalizer()
                .NormalizeTime(type.Kind, value.TimeValue.Text);
            if (!normalized.Success)
            {
                return primitives.CreateError(
                    "ASNXER0203",
                    normalized.ErrorMessage,
                    false);
            }

            return primitives.CreateTextEncoding(
                value,
                elementName,
                normalized.Text);
        }

        return encoded;
    }

    internal string FormatCanonicalReal(
        AsnRealValue? value,
        string basicText,
        XerPrimitiveCodec primitives)
    {
        if (value == null)
        {
            return "";
        }

        if (value.Kind == AsnRealKind.Zero)
        {
            return "0";
        }

        if (value.Kind == AsnRealKind.MinusZero)
        {
            return "-0";
        }

        AsnRealValue? decimalValue = primitives.ParseDecimalReal(basicText);
        if (decimalValue == null)
        {
            return "";
        }

        System.Numerics.BigInteger coefficient =
            decimalValue.DecimalCoefficient;
        bool negative = coefficient < System.Numerics.BigInteger.Zero;
        string digits = System.Numerics.BigInteger.Abs(coefficient).ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string fraction = digits.Length == 1
            ? "0"
            : digits.Substring(1);
        System.Numerics.BigInteger exponent = decimalValue.DecimalExponent
            + new System.Numerics.BigInteger(digits.Length - 1);
        return (negative ? "-" : "")
            + digits.Substring(0, 1)
            + "."
            + fraction
            + "E"
            + exponent.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    internal XerPrimitiveResult EncodeCanonicalNamedBits(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        string elementName,
        XerPrimitiveCodec primitives)
    {
        AsnBitStringValue? bits = value.BitStringValue;
        if (bits == null || !bits.IsValid())
        {
            return primitives.CreateError(
                "ASNXER0204",
                "The named BIT STRING value is invalid.",
                false);
        }

        int highestSetBit = this.FindHighestSetBit(bits);
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int bitIndex = 0;
        while (bitIndex <= highestSetBit)
        {
            if (this.IsBitSet(bits, bitIndex))
            {
                string name = this.FindNamedBit(type, bitIndex);
                if (name.Length == 0)
                {
                    string text = this.FormatBitText(bits, highestSetBit + 1);
                    return primitives.CreateTextEncoding(
                        value,
                        elementName,
                        text);
                }

                names.Add(name);
            }

            bitIndex = bitIndex + 1;
        }

        if (names.Count == 0)
        {
            return primitives.CreateEmptyEncoding(value, elementName);
        }

        XerXmlToken[] tokens = new XerXmlToken[names.Count + 2];
        tokens[0] = primitives.CreateNamedToken(
            XerXmlTokenKind.StartElement,
            elementName);
        int index = 0;
        while (index < names.Count)
        {
            tokens[index + 1] = primitives.CreateNamedToken(
                XerXmlTokenKind.EmptyElement,
                names[index]);
            index = index + 1;
        }

        tokens[tokens.Length - 1] = primitives.CreateNamedToken(
            XerXmlTokenKind.EndElement,
            elementName);
        return new XerPrimitiveResult
        {
            Success = true,
            Status = AsnDecodeStatus.Done,
            Value = value,
            Tokens = tokens,
            ConsumedCount = tokens.Length
        };
    }

    internal bool HasNamedBitList(AsnTypeDescriptor type)
    {
        return type.NamedValues.Length > 0
            || type.RootNamedValues.Length > 0
            || type.ExtensionNamedValues.Length > 0;
    }

    internal string FindNamedBit(AsnTypeDescriptor type, int bitIndex)
    {
        string result = this.FindNamedBitInList(
            type.NamedValues,
            bitIndex);
        if (result.Length == 0)
        {
            result = this.FindNamedBitInList(
                type.RootNamedValues,
                bitIndex);
        }

        if (result.Length == 0)
        {
            result = this.FindNamedBitInList(
                type.ExtensionNamedValues,
                bitIndex);
        }

        return result;
    }

    internal string FindNamedBitInList(
        AsnValueDescriptor[] descriptors,
        int bitIndex)
    {
        string result = "";
        int index = 0;
        while (index < descriptors.Length)
        {
            AsnValueDescriptor descriptor = descriptors[index];
            if (descriptor != null
                && descriptor.Value != null
                && descriptor.Value.HasIntegerValue
                && descriptor.Value.IntegerValue
                    == new System.Numerics.BigInteger(bitIndex))
            {
                if (result.Length > 0)
                {
                    return "";
                }

                result = descriptor.Name;
            }

            index = index + 1;
        }

        return result;
    }

    internal int FindHighestSetBit(AsnBitStringValue bits)
    {
        int bitIndex = bits.BitLength - 1;
        while (bitIndex >= 0 && !this.IsBitSet(bits, bitIndex))
        {
            bitIndex = bitIndex - 1;
        }

        return bitIndex;
    }

    internal bool IsBitSet(AsnBitStringValue bits, int bitIndex)
    {
        int byteIndex = bitIndex / 8;
        int mask = 1 << (7 - bitIndex % 8);
        return byteIndex >= 0
            && byteIndex < bits.Bytes.Length
            && (bits.Bytes[byteIndex] & mask) != 0;
    }

    internal string FormatBitText(AsnBitStringValue bits, int bitCount)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < bitCount)
        {
            result.Append(this.IsBitSet(bits, index) ? '1' : '0');
            index = index + 1;
        }

        return result.ToString();
    }

    internal string GetSingleTextToken(XerXmlToken[] tokens)
    {
        return tokens.Length == 3
            && tokens[1].Kind == XerXmlTokenKind.Text
                ? tokens[1].Value
                : "";
    }

    internal AsnFieldDescriptor[] GetCanonicalCollectionFields(
        AsnTypeDescriptor type)
    {
        AsnFieldDescriptor[] all = this.GetAllFields(type);
        System.Collections.Generic.List<AsnFieldDescriptor> roots =
            new System.Collections.Generic.List<AsnFieldDescriptor>();
        System.Collections.Generic.List<AsnFieldDescriptor> extensions =
            new System.Collections.Generic.List<AsnFieldDescriptor>();
        int index = 0;
        if (type.RootFields.Length > 0)
        {
            while (index < type.RootFields.Length)
            {
                roots.Add(type.RootFields[index]);
                index = index + 1;
            }
        }
        else
        {
            while (index < all.Length)
            {
                if (!all[index].Extension)
                {
                    roots.Add(all[index]);
                }

                index = index + 1;
            }
        }

        index = 0;
        if (type.ExtensionFields.Length > 0)
        {
            while (index < type.ExtensionFields.Length)
            {
                extensions.Add(type.ExtensionFields[index]);
                index = index + 1;
            }
        }
        else
        {
            while (index < all.Length)
            {
                if (all[index].Extension)
                {
                    extensions.Add(all[index]);
                }

                index = index + 1;
            }
        }

        if (type.Kind == AsnTypeKind.Set)
        {
            index = 1;
            while (index < roots.Count)
            {
                AsnFieldDescriptor field = roots[index];
                int insertion = index;
                while (insertion > 0
                    && this.CompareCanonicalFieldTags(
                        field,
                        roots[insertion - 1]) < 0)
                {
                    roots[insertion] = roots[insertion - 1];
                    insertion = insertion - 1;
                }

                roots[insertion] = field;
                index = index + 1;
            }
        }

        AsnFieldDescriptor[] result = new AsnFieldDescriptor[
            roots.Count + extensions.Count];
        index = 0;
        while (index < roots.Count)
        {
            result[index] = roots[index];
            index = index + 1;
        }

        int extensionIndex = 0;
        while (extensionIndex < extensions.Count)
        {
            result[index] = extensions[extensionIndex];
            index = index + 1;
            extensionIndex = extensionIndex + 1;
        }

        return result;
    }

    internal int CompareCanonicalFieldTags(
        AsnFieldDescriptor left,
        AsnFieldDescriptor right)
    {
        AsnTagDescriptor? leftTag = this.GetCanonicalFieldTag(left);
        AsnTagDescriptor? rightTag = this.GetCanonicalFieldTag(right);
        if (leftTag == null || rightTag == null)
        {
            return left.Index - right.Index;
        }

        int classComparison = (int)leftTag.Class - (int)rightTag.Class;
        return classComparison != 0
            ? classComparison
            : leftTag.Number.CompareTo(rightTag.Number);
    }

    internal AsnTagDescriptor? GetCanonicalFieldTag(
        AsnFieldDescriptor field)
    {
        if (field.Tags.Length > 0)
        {
            return field.Tags[0];
        }

        return field.Type == null
            ? null
            : this.GetCanonicalTypeTag(field.Type);
    }

    internal AsnTagDescriptor? GetCanonicalTypeTag(AsnTypeDescriptor type)
    {
        if (type.Tags.Length > 0)
        {
            return type.Tags[0];
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            AsnFieldDescriptor[] alternatives = this.GetAllFields(type);
            AsnTagDescriptor? smallest = null;
            int index = 0;
            while (index < alternatives.Length)
            {
                AsnTagDescriptor? tag = this.GetCanonicalFieldTag(
                    alternatives[index]);
                if (tag != null
                    && (smallest == null
                        || this.CompareCanonicalTags(tag, smallest) < 0))
                {
                    smallest = tag;
                }

                index = index + 1;
            }

            return smallest;
        }

        int number = new OerAlternativeCodec(this.Limits)
            .GetUniversalTagNumber(type.Kind);
        return number < 0
            ? null
            : new AsnTagDescriptor
            {
                Class = AsnTagClass.Universal,
                Number = new System.Numerics.BigInteger(number),
                Universal = true
            };
    }

    internal int CompareCanonicalTags(
        AsnTagDescriptor left,
        AsnTagDescriptor right)
    {
        int classComparison = (int)left.Class - (int)right.Class;
        return classComparison != 0
            ? classComparison
            : left.Number.CompareTo(right.Number);
    }

    internal int[] GetTokenEncodingOrder(
        XerXmlToken[][] encodings,
        bool sort)
    {
        int[] order = new int[encodings.Length];
        byte[][] bytes = new byte[encodings.Length][];
        int index = 0;
        while (index < encodings.Length)
        {
            order[index] = index;
            bytes[index] = sort
                ? this.SerializeTokenRange(
                    encodings[index],
                    0,
                    encodings[index].Length)
                : [];
            index = index + 1;
        }

        if (!sort)
        {
            return order;
        }

        index = 1;
        while (index < order.Length)
        {
            int item = order[index];
            int insertion = index;
            while (insertion > 0
                && StableOrder.CompareByteArrays(
                    bytes[item],
                    bytes[order[insertion - 1]]) < 0)
            {
                order[insertion] = order[insertion - 1];
                insertion = insertion - 1;
            }

            order[insertion] = item;
            index = index + 1;
        }

        return order;
    }

    internal AsnEncodingRules GetActiveEncodingRules()
    {
        if (this.Extended)
        {
            return AsnEncodingRules.ExtendedXer;
        }

        return this.Canonical
            ? AsnEncodingRules.CanonicalXer
            : AsnEncodingRules.BasicXer;
    }

    internal bool AllowsDepth(int depth)
    {
        return depth >= 0
            && (this.Limits.Unlimited
                || depth < this.Limits.MaximumNestingDepth);
    }

    internal XerValueResult CopyPrimitiveResult(XerPrimitiveResult source)
    {
        return new XerValueResult
        {
            Success = source.Success,
            Status = source.Status,
            Value = source.Value,
            Tokens = source.Tokens,
            ConsumedCount = source.ConsumedCount,
            LimitExceeded = source.LimitExceeded,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal XerValueResult CopyNameFailure(XerNameResult source)
    {
        return this.CreateError(
            source.ErrorCode,
            source.ErrorMessage,
            false);
    }

    internal XerValueResult CopyWrapperFailure(XerWrappedValue source)
    {
        return new XerValueResult
        {
            Status = source.NeedMoreData
                ? AsnDecodeStatus.NeedMoreData
                : AsnDecodeStatus.Error,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage
        };
    }

    internal XerValueResult CreateError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new XerValueResult
        {
            Status = AsnDecodeStatus.Error,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal XerValueResult CreateTokenError(
        string code,
        string message,
        bool limitExceeded,
        XerXmlToken token)
    {
        XerValueResult result = this.CreateError(
            code,
            message,
            limitExceeded);
        result.HasViolationOffset = true;
        result.ViolationOffset = token.ByteOffset;
        return result;
    }

    internal void SetEncodingFailureContext(XerValueResult result)
    {
        if (result.ValuePath.Length == 0)
        {
            result.ValuePath = this.CreateValuePath(this.EncodingPath);
        }
    }

    internal void SetDecodingFailureContext(
        XerValueResult result,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        if (result.ValuePath.Length == 0)
        {
            result.ValuePath = this.CreateValuePath(this.DecodingPath);
        }

        if (!result.HasViolationOffset
            && count > 0
            && CursorMath.IsRangeValid(offset, count, tokens.Length))
        {
            int diagnosticIndex = this.FindDiagnosticTokenIndex(
                result.ErrorCode,
                tokens,
                offset,
                count);
            result.HasViolationOffset = true;
            result.ViolationOffset = tokens[diagnosticIndex].ByteOffset;
        }
    }

    internal int FindDiagnosticTokenIndex(
        string code,
        XerXmlToken[] tokens,
        int offset,
        int count)
    {
        bool lexical = code == "ASNXER0103"
            || code == "ASNXER0104"
            || code == "ASNXER0105"
            || code == "ASNXER0106"
            || code == "ASNXER0107"
            || code == "ASNXER0108"
            || code == "ASNXER0341"
            || code == "ASNXER0343"
            || code == "ASNXER0344"
            || code == "ASNXER0346";
        int index = offset;
        int end = offset + count;
        while (lexical && index < end)
        {
            if (tokens[index].Kind == XerXmlTokenKind.Text
                || tokens[index].Kind == XerXmlTokenKind.CData)
            {
                return index;
            }

            index = index + 1;
        }

        return offset;
    }

    internal string CreateValuePath(
        System.Collections.Generic.List<string> elements)
    {
        string result = "$";
        int index = 0;
        while (index < elements.Count)
        {
            if (elements[index].Length > 0)
            {
                result = result + "." + elements[index];
            }

            index = index + 1;
        }

        return result;
    }
}
