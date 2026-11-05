namespace asn1;

internal class ValueAccess
{
    internal AsnRuntimeValue NormalizeValue(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        return this.NormalizeRuntimeNode(
            value,
            type,
            catalog,
            new System.Collections.Generic.List<AsnRuntimeValue>());
    }

    internal AsnRuntimeValue FromSchemaValue(
        AsnValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        AsnRuntimeValue raw = this.CreateRuntimeFromSchema(
            value,
            type,
            catalog,
            new System.Collections.Generic.List<AsnValue>());
        return this.NormalizeValue(raw, type, catalog);
    }

    internal AsnValue ToSchemaValue(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog)
    {
        AsnRuntimeValue normalized = this.NormalizeValue(value, type, catalog);
        if (normalized.Value == null)
        {
            throw new System.InvalidOperationException(
                "The normalized runtime value has no schema value.");
        }

        return normalized.Value;
    }

    internal AsnRuntimeValue NormalizeRuntimeNode(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        if (path.Contains(value))
        {
            throw new System.InvalidOperationException(
                "A runtime value cannot contain itself.");
        }

        path.Add(value);
        AsnTypeDescriptor? effectiveType = this.GetEffectiveType(type);
        if (effectiveType == null)
        {
            throw new System.InvalidOperationException(
                "The descriptor does not identify an effective runtime type.");
        }

        type = effectiveType;
        AsnRuntimeValueKind expectedKind = this.GetRuntimeKind(type);
        if (expectedKind == AsnRuntimeValueKind.Unknown)
        {
            throw new System.InvalidOperationException(
                "The descriptor does not identify an effective runtime type.");
        }

        if (value.Kind != AsnRuntimeValueKind.Unknown
            && value.Kind != expectedKind)
        {
            throw new System.InvalidOperationException(
                "The runtime value kind does not match the type descriptor.");
        }

        if (value.Fields == null
            || value.Items == null
            || value.UnknownExtensions == null
            || value.UnknownFields == null)
        {
            throw new System.InvalidOperationException(
                "Runtime value collections cannot be null.");
        }

        AsnRuntimeValue result = new AsnRuntimeValue
        {
            Kind = expectedKind,
            UnknownExtensions = this.CloneUnknownExtensions(value.UnknownExtensions),
            UnknownFields = this.CloneUnknownFields(value.UnknownFields)
        };

        if (expectedKind == AsnRuntimeValueKind.Boolean)
        {
            if (!value.HasBooleanValue)
            {
                throw new System.InvalidOperationException(
                    "A BOOLEAN runtime value requires its Boolean payload.");
            }

            result.HasBooleanValue = true;
            result.BooleanValue = value.BooleanValue;
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Integer
            || expectedKind == AsnRuntimeValueKind.Enumerated)
        {
            if (!value.HasIntegerValue)
            {
                throw new System.InvalidOperationException(
                    "An integer runtime value requires its integer payload.");
            }

            result.HasIntegerValue = true;
            result.IntegerValue = value.IntegerValue;
            result.TextValue = value.TextValue == null ? "" : value.TextValue;
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Real)
        {
            if (value.RealValue == null || !value.RealValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A REAL runtime value requires valid normalized REAL data.");
            }

            result.RealValue = this.CloneRealValue(value.RealValue);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.BitString)
        {
            if (value.BitStringValue == null || !value.BitStringValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A BIT STRING runtime value requires valid bit data.");
            }

            result.BitStringValue = this.CloneBitStringValue(value.BitStringValue);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.OctetString)
        {
            if (value.Octets == null)
            {
                throw new System.InvalidOperationException(
                    "An OCTET STRING runtime value cannot contain null data.");
            }

            result.Octets = (byte[])value.Octets.Clone();
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Null)
        {
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.CharacterString
            || expectedKind == AsnRuntimeValueKind.ObjectIdentifierIri
            || expectedKind == AsnRuntimeValueKind.RelativeObjectIdentifierIri)
        {
            if (value.TextValue == null)
            {
                throw new System.InvalidOperationException(
                    "A text runtime value cannot contain null text.");
            }

            result.TextValue = value.TextValue;
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.ObjectIdentifier
            || expectedKind == AsnRuntimeValueKind.RelativeObjectIdentifier)
        {
            bool relative = expectedKind
                == AsnRuntimeValueKind.RelativeObjectIdentifier;
            if (value.ObjectIdentifierValue == null
                || !value.ObjectIdentifierValue.IsValid()
                || value.ObjectIdentifierValue.Relative != relative)
            {
                throw new System.InvalidOperationException(
                    "The object identifier payload does not match its descriptor.");
            }

            result.ObjectIdentifierValue = this.CloneObjectIdentifierValue(
                value.ObjectIdentifierValue);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Time)
        {
            if (value.TimeValue == null || !value.TimeValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A time runtime value requires valid normalized time data.");
            }

            result.TimeValue = this.CloneTimeValue(value.TimeValue);
            result.TextValue = result.TimeValue.Text;
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Sequence
            || expectedKind == AsnRuntimeValueKind.Set
            || expectedKind == AsnRuntimeValueKind.External
            || expectedKind == AsnRuntimeValueKind.EmbeddedPdv
            || expectedKind == AsnRuntimeValueKind.InstanceOf)
        {
            result.Fields = this.NormalizeStructuredFields(
                value,
                type,
                catalog,
                path);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.Choice)
        {
            result.Fields = this.NormalizeChoiceFields(
                value,
                type,
                catalog,
                path);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.SequenceOf
            || expectedKind == AsnRuntimeValueKind.SetOf)
        {
            result.Items = this.NormalizeCollectionItems(
                value,
                type,
                catalog,
                path);
            return this.FinishRuntimeNode(result, path);
        }

        if (expectedKind == AsnRuntimeValueKind.OpenType)
        {
            result.OpenTypeValue = this.NormalizeOpenType(
                value,
                type,
                catalog,
                path);
            return this.FinishRuntimeNode(result, path);
        }

        throw new System.InvalidOperationException(
            "The descriptor runtime kind is not supported.");
    }

    internal AsnRuntimeFieldValue[] NormalizeStructuredFields(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        bool[] consumed = new bool[value.Fields.Length];
        AsnRuntimeFieldValue[] result =
            new AsnRuntimeFieldValue[type.Fields.Length];
        int descriptorIndex = 0;
        while (descriptorIndex < type.Fields.Length)
        {
            AsnFieldDescriptor descriptor = type.Fields[descriptorIndex];
            int sourceIndex = this.FindRuntimeFieldIndex(
                value.Fields,
                descriptor,
                consumed);
            AsnRuntimeFieldValue normalized = new AsnRuntimeFieldValue
            {
                Name = descriptor.Name
            };
            if (sourceIndex < 0 || !value.Fields[sourceIndex].Present)
            {
                if (!descriptor.Optional
                    && !descriptor.HasDefaultValue
                    && !descriptor.Extension)
                {
                    throw new System.InvalidOperationException(
                        "A required structured field is absent: "
                        + descriptor.Name);
                }

                normalized.Value = this.CreateDefaultRuntimeValue(
                    descriptor,
                    catalog);
            }
            else
            {
                AsnRuntimeFieldValue source = value.Fields[sourceIndex];
                if (source.Value == null || descriptor.Type == null)
                {
                    throw new System.InvalidOperationException(
                        "A present structured field requires a resolved value: "
                        + descriptor.Name);
                }

                if (source.PresentDefault && !descriptor.HasDefaultValue)
                {
                    throw new System.InvalidOperationException(
                        "Only a DEFAULT field can be marked present-default: "
                        + descriptor.Name);
                }

                normalized.Present = true;
                normalized.PresentDefault = source.PresentDefault;
                normalized.Value = this.NormalizeRuntimeNode(
                    source.Value,
                    descriptor.Type,
                    catalog,
                    path);
            }

            result[descriptorIndex] = normalized;
            descriptorIndex = descriptorIndex + 1;
        }

        this.RequireAllRuntimeFieldsConsumed(value.Fields, consumed);
        return result;
    }

    internal AsnRuntimeFieldValue[] NormalizeChoiceFields(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        bool[] consumed = new bool[value.Fields.Length];
        AsnRuntimeFieldValue? selected = null;
        AsnFieldDescriptor? selectedDescriptor = null;
        int descriptorIndex = 0;
        while (descriptorIndex < type.Fields.Length)
        {
            AsnFieldDescriptor descriptor = type.Fields[descriptorIndex];
            int sourceIndex = this.FindRuntimeFieldIndex(
                value.Fields,
                descriptor,
                consumed);
            if (sourceIndex >= 0 && value.Fields[sourceIndex].Present)
            {
                if (selected != null)
                {
                    throw new System.InvalidOperationException(
                        "A CHOICE runtime value must select exactly one field.");
                }

                selected = value.Fields[sourceIndex];
                selectedDescriptor = descriptor;
            }

            descriptorIndex = descriptorIndex + 1;
        }

        this.RequireAllRuntimeFieldsConsumed(value.Fields, consumed);
        if (selected == null
            || selectedDescriptor == null
            || selected.Value == null
            || selectedDescriptor.Type == null)
        {
            throw new System.InvalidOperationException(
                "A CHOICE runtime value must select exactly one resolved field.");
        }

        return
        [
            new AsnRuntimeFieldValue
            {
                Name = selectedDescriptor.Name,
                Present = true,
                Value = this.NormalizeRuntimeNode(
                    selected.Value,
                    selectedDescriptor.Type,
                    catalog,
                    path)
            }
        ];
    }

    internal AsnRuntimeValue[] NormalizeCollectionItems(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        if (value.Fields.Length != 0)
        {
            throw new System.InvalidOperationException(
                "A collection runtime value cannot contain named fields.");
        }

        if (type.ElementType == null)
        {
            throw new System.InvalidOperationException(
                "A collection descriptor requires an element type.");
        }

        AsnRuntimeValue[] result = new AsnRuntimeValue[value.Items.Length];
        int index = 0;
        while (index < value.Items.Length)
        {
            if (value.Items[index] == null)
            {
                throw new System.InvalidOperationException(
                    "A collection runtime value cannot contain a null item.");
            }

            result[index] = this.NormalizeRuntimeNode(
                value.Items[index],
                type.ElementType,
                catalog,
                path);
            index = index + 1;
        }

        return result;
    }

    internal AsnOpenTypeValue NormalizeOpenType(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        AsnOpenTypeValue? source = value.OpenTypeValue;
        if (source == null || !source.IsValid())
        {
            throw new System.InvalidOperationException(
                "An open runtime value requires valid open-type metadata.");
        }

        AsnOpenTypeValue result = new AsnOpenTypeValue
        {
            Resolved = source.Resolved,
            Unresolved = source.Unresolved,
            ResolvedTypeId = source.ResolvedTypeId,
            TypeName = source.TypeName,
            EncodingRules = source.EncodingRules,
            HasEncodingRules = source.HasEncodingRules,
            HasOriginalData = source.HasOriginalData,
            OriginalData = (byte[])source.OriginalData.Clone(),
            OriginalTag = this.CloneOptionalTag(source.OriginalTag),
            HasEncodedLocation = source.HasEncodedLocation,
            EncodedOffset = source.EncodedOffset,
            EncodedLength = source.EncodedLength
        };
        if (source.DispatchKey != null)
        {
            result.DispatchKey = this.CloneLooseRuntimeValue(
                source.DispatchKey,
                new System.Collections.Generic.List<AsnRuntimeValue>());
        }

        if (source.Resolved)
        {
            AsnTypeDescriptor? resolvedType = this.FindResolvedOpenType(
                source,
                type,
                catalog);
            if (resolvedType == null || source.Value == null)
            {
                throw new System.InvalidOperationException(
                    "The resolved open type is not present in its descriptor catalog.");
            }

            result.ResolvedTypeId = resolvedType.StableId;
            result.TypeName = resolvedType.Name;
            result.Value = this.NormalizeRuntimeNode(
                source.Value,
                resolvedType,
                catalog,
                path);
        }

        return result;
    }

    internal AsnTypeDescriptor? FindResolvedOpenType(
        AsnOpenTypeValue open,
        AsnTypeDescriptor owner,
        AsnReflectionCatalog catalog)
    {
        string identity = open.GetResolvedTypeIdentity();
        AsnTypeDescriptor? result = null;
        int tableIndex = 0;
        while (tableIndex < owner.ObjectTables.Length)
        {
            AsnObjectTableDescriptor table = owner.ObjectTables[tableIndex];
            int entryIndex = 0;
            while (entryIndex < table.Entries.Length)
            {
                AsnTypeDescriptor? candidate = table.Entries[entryIndex].Type;
                if (candidate != null && this.TypeIdentityMatches(candidate, identity))
                {
                    if (result != null && result != candidate)
                    {
                        return null;
                    }

                    result = candidate;
                }

                entryIndex = entryIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        AsnTypeDescriptor[] catalogTypes =
            new ReflectionServiceImplementation().GetTypes(catalog);
        int index = 0;
        while (index < catalogTypes.Length)
        {
            AsnTypeDescriptor candidate = catalogTypes[index];
            if (this.TypeIdentityMatches(candidate, identity))
            {
                if (result != null && result != candidate)
                {
                    return null;
                }

                result = candidate;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool TypeIdentityMatches(AsnTypeDescriptor type, string identity)
    {
        return type.StableId == identity
            || type.Name == identity
            || type.GeneratedName == identity;
    }

    internal int FindRuntimeFieldIndex(
        AsnRuntimeFieldValue[] fields,
        AsnFieldDescriptor descriptor,
        bool[] consumed)
    {
        int result = -1;
        int index = 0;
        while (index < fields.Length)
        {
            AsnRuntimeFieldValue? field = fields[index];
            if (field == null || field.Name == null)
            {
                throw new System.InvalidOperationException(
                    "A runtime field and its name cannot be null.");
            }

            if (field.Name == descriptor.Name
                || field.Name == descriptor.GeneratedName
                || field.Name == descriptor.StableId)
            {
                if (result >= 0)
                {
                    throw new System.InvalidOperationException(
                        "A structured runtime value contains a duplicate field: "
                        + descriptor.Name);
                }

                result = index;
                consumed[index] = true;
            }

            index = index + 1;
        }

        return result;
    }

    internal void RequireAllRuntimeFieldsConsumed(
        AsnRuntimeFieldValue[] fields,
        bool[] consumed)
    {
        int index = 0;
        while (index < consumed.Length)
        {
            if (!consumed[index])
            {
                throw new System.InvalidOperationException(
                    "The runtime field has no matching descriptor: "
                    + fields[index].Name);
            }

            index = index + 1;
        }
    }

    internal AsnRuntimeValue? CreateDefaultRuntimeValue(
        AsnFieldDescriptor descriptor,
        AsnReflectionCatalog catalog)
    {
        if (!descriptor.HasDefaultValue
            || descriptor.DefaultValue == null
            || descriptor.DefaultValue.Value == null)
        {
            return null;
        }

        if (descriptor.Type == null)
        {
            throw new System.InvalidOperationException(
                "A DEFAULT field requires a resolved type descriptor.");
        }

        return this.FromSchemaValue(
            descriptor.DefaultValue.Value,
            descriptor.Type,
            catalog);
    }

    internal AsnRuntimeValue CreateRuntimeFromSchema(
        AsnValue value,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnValue> path)
    {
        if (path.Contains(value))
        {
            throw new System.InvalidOperationException(
                "A schema value cannot contain itself.");
        }

        path.Add(value);
        AsnTypeDescriptor? effectiveType = this.GetEffectiveType(type);
        if (effectiveType == null)
        {
            throw new System.InvalidOperationException(
                "The descriptor does not identify an effective runtime type.");
        }

        type = effectiveType;
        AsnRuntimeValueKind runtimeKind = this.GetRuntimeKind(type);
        AsnValueKind expectedSchemaKind =
            (AsnValueKind)(int)runtimeKind;
        if (runtimeKind == AsnRuntimeValueKind.Unknown)
        {
            throw new System.InvalidOperationException(
                "The descriptor does not identify an effective runtime type.");
        }

        if (value.Kind != AsnValueKind.Unknown
            && value.Kind != expectedSchemaKind)
        {
            throw new System.InvalidOperationException(
                "The schema value kind does not match the type descriptor.");
        }

        AsnRuntimeValue result = new AsnRuntimeValue
        {
            Kind = runtimeKind,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.TextValue == null ? "" : value.TextValue,
            Octets = value.Octets == null ? [] : (byte[])value.Octets.Clone()
        };
        if (result.Kind == AsnRuntimeValueKind.Boolean)
        {
            if (value.TextValue == "TRUE")
            {
                result.HasBooleanValue = true;
                result.BooleanValue = true;
            }
            else if (value.TextValue == "FALSE")
            {
                result.HasBooleanValue = true;
            }
            else
            {
                throw new System.InvalidOperationException(
                    "A schema BOOLEAN value must be TRUE or FALSE.");
            }
        }

        if (value.RealValue != null)
        {
            result.RealValue = this.CloneRealValue(value.RealValue);
        }

        if (value.TimeValue != null)
        {
            result.TimeValue = this.CloneTimeValue(value.TimeValue);
        }

        if (value.BitStringValue != null)
        {
            if (!value.BitStringValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A schema BIT STRING value requires valid bit data.");
            }

            result.BitStringValue = this.CloneBitStringValue(value.BitStringValue);
        }

        if (value.ObjectIdentifierValue != null)
        {
            if (!value.ObjectIdentifierValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A schema object identifier requires valid arc data.");
            }

            result.ObjectIdentifierValue = this.CloneObjectIdentifierValue(
                value.ObjectIdentifierValue);
        }

        if (value.OpenTypeValue != null)
        {
            if (!value.OpenTypeValue.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A schema open value requires valid open-type metadata.");
            }

            result.OpenTypeValue = this.CloneLooseOpenTypeValue(
                value.OpenTypeValue);
        }

        if (result.Kind == AsnRuntimeValueKind.Sequence
            || result.Kind == AsnRuntimeValueKind.Set
            || result.Kind == AsnRuntimeValueKind.Choice
            || result.Kind == AsnRuntimeValueKind.External
            || result.Kind == AsnRuntimeValueKind.EmbeddedPdv
            || result.Kind == AsnRuntimeValueKind.InstanceOf)
        {
            result.Fields = this.CreateRuntimeFieldsFromSchema(
                value.NamedValues,
                type,
                catalog,
                path);
        }

        if (result.Kind == AsnRuntimeValueKind.SequenceOf
            || result.Kind == AsnRuntimeValueKind.SetOf)
        {
            if (type.ElementType == null || value.Items == null)
            {
                throw new System.InvalidOperationException(
                    "A schema collection requires values and an element descriptor.");
            }

            result.Items = new AsnRuntimeValue[value.Items.Length];
            int itemIndex = 0;
            while (itemIndex < value.Items.Length)
            {
                if (value.Items[itemIndex] == null)
                {
                    throw new System.InvalidOperationException(
                        "A schema collection cannot contain a null item.");
                }

                result.Items[itemIndex] = this.CreateRuntimeFromSchema(
                    value.Items[itemIndex],
                    type.ElementType,
                    catalog,
                    path);
                itemIndex = itemIndex + 1;
            }
        }

        path.RemoveAt(path.Count - 1);
        return result;
    }

    internal AsnRuntimeFieldValue[] CreateRuntimeFieldsFromSchema(
        AsnNamedValue[] namedValues,
        AsnTypeDescriptor type,
        AsnReflectionCatalog catalog,
        System.Collections.Generic.List<AsnValue> path)
    {
        if (namedValues == null)
        {
            throw new System.InvalidOperationException(
                "A structured schema value cannot contain null named values.");
        }

        AsnRuntimeFieldValue[] result =
            new AsnRuntimeFieldValue[namedValues.Length];
        int index = 0;
        while (index < namedValues.Length)
        {
            AsnNamedValue? named = namedValues[index];
            if (named == null || named.Name == null || named.Value == null)
            {
                throw new System.InvalidOperationException(
                    "A named schema value requires a name and value.");
            }

            AsnFieldDescriptor? descriptor = this.FindFieldDescriptor(
                type,
                named.Name);
            if (descriptor == null || descriptor.Type == null)
            {
                throw new System.InvalidOperationException(
                    "A named schema value has no resolved field descriptor: "
                    + named.Name);
            }

            result[index] = new AsnRuntimeFieldValue
            {
                Name = descriptor.Name,
                Present = true,
                Value = this.CreateRuntimeFromSchema(
                    named.Value,
                    descriptor.Type,
                    catalog,
                    path)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnFieldDescriptor? FindFieldDescriptor(
        AsnTypeDescriptor type,
        string name)
    {
        AsnFieldDescriptor? result = null;
        int index = 0;
        while (index < type.Fields.Length)
        {
            AsnFieldDescriptor candidate = type.Fields[index];
            if (candidate.Name == name
                || candidate.GeneratedName == name
                || candidate.StableId == name)
            {
                if (result != null)
                {
                    return null;
                }

                result = candidate;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnRuntimeValue FinishRuntimeNode(
        AsnRuntimeValue result,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        result.Value = this.CreateSchemaFromNormalized(result);
        path.RemoveAt(path.Count - 1);
        return result;
    }

    internal AsnValue CreateSchemaFromNormalized(AsnRuntimeValue value)
    {
        AsnValue result = new AsnValue
        {
            Kind = (AsnValueKind)(int)value.Kind,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.HasBooleanValue
                ? value.BooleanValue ? "TRUE" : "FALSE"
                : value.TextValue == null ? "" : value.TextValue,
            Octets = value.Octets == null ? [] : (byte[])value.Octets.Clone()
        };
        if (value.RealValue != null)
        {
            result.RealValue = this.CloneRealValue(value.RealValue);
        }

        if (value.TimeValue != null)
        {
            result.TimeValue = this.CloneTimeValue(value.TimeValue);
        }

        if (value.BitStringValue != null)
        {
            result.BitStringValue = this.CloneBitStringValue(value.BitStringValue);
        }

        if (value.ObjectIdentifierValue != null)
        {
            result.ObjectIdentifierValue = this.CloneObjectIdentifierValue(
                value.ObjectIdentifierValue);
        }

        if (value.OpenTypeValue != null)
        {
            result.OpenTypeValue = this.CloneLooseOpenTypeValue(
                value.OpenTypeValue);
        }

        System.Collections.Generic.List<AsnNamedValue> named =
            new System.Collections.Generic.List<AsnNamedValue>();
        int fieldIndex = 0;
        while (fieldIndex < value.Fields.Length)
        {
            AsnRuntimeFieldValue field = value.Fields[fieldIndex];
            if (field.Present && field.Value != null && field.Value.Value != null)
            {
                named.Add(new AsnNamedValue
                {
                    Name = field.Name,
                    Value = field.Value.Value
                });
            }

            fieldIndex = fieldIndex + 1;
        }

        result.NamedValues = named.ToArray();
        result.Items = new AsnValue[value.Items.Length];
        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            if (value.Items[itemIndex].Value == null)
            {
                throw new System.InvalidOperationException(
                    "A normalized collection item has no schema value.");
            }

            result.Items[itemIndex] = value.Items[itemIndex].Value
                ?? new AsnValue();
            itemIndex = itemIndex + 1;
        }

        return result;
    }

    internal AsnRuntimeValue CloneLooseRuntimeValue(
        AsnRuntimeValue value,
        System.Collections.Generic.List<AsnRuntimeValue> path)
    {
        if (path.Contains(value))
        {
            throw new System.InvalidOperationException(
                "An open-type dispatch key cannot contain itself.");
        }

        if (value.Octets == null
            || value.Fields == null
            || value.Items == null
            || value.UnknownExtensions == null
            || value.UnknownFields == null)
        {
            throw new System.InvalidOperationException(
                "An open-type dispatch value cannot contain null collections.");
        }

        path.Add(value);
        AsnRuntimeValue result = new AsnRuntimeValue
        {
            Kind = value.Kind,
            Value = value.Value,
            HasBooleanValue = value.HasBooleanValue,
            BooleanValue = value.BooleanValue,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.TextValue == null ? "" : value.TextValue,
            Octets = (byte[])value.Octets.Clone(),
            UnknownExtensions = this.CloneUnknownExtensions(
                value.UnknownExtensions),
            UnknownFields = this.CloneUnknownFields(
                value.UnknownFields)
        };
        if (value.BitStringValue != null)
        {
            result.BitStringValue = this.CloneBitStringValue(value.BitStringValue);
        }

        if (value.ObjectIdentifierValue != null)
        {
            result.ObjectIdentifierValue = this.CloneObjectIdentifierValue(
                value.ObjectIdentifierValue);
        }

        if (value.RealValue != null)
        {
            result.RealValue = this.CloneRealValue(value.RealValue);
        }

        if (value.TimeValue != null)
        {
            result.TimeValue = this.CloneTimeValue(value.TimeValue);
        }

        AsnRuntimeFieldValue[] fields = value.Fields;
        result.Fields = new AsnRuntimeFieldValue[fields.Length];
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            AsnRuntimeFieldValue field = fields[fieldIndex];
            result.Fields[fieldIndex] = new AsnRuntimeFieldValue
            {
                Name = field.Name,
                Present = field.Present,
                PresentDefault = field.PresentDefault,
                Value = field.Value == null
                    ? null
                    : this.CloneLooseRuntimeValue(field.Value, path)
            };
            fieldIndex = fieldIndex + 1;
        }

        AsnRuntimeValue[] items = value.Items;
        result.Items = new AsnRuntimeValue[items.Length];
        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            result.Items[itemIndex] = this.CloneLooseRuntimeValue(
                items[itemIndex],
                path);
            itemIndex = itemIndex + 1;
        }

        if (value.OpenTypeValue != null)
        {
            result.OpenTypeValue = this.CloneLooseOpenTypeValue(
                value.OpenTypeValue);
        }

        path.RemoveAt(path.Count - 1);
        return result;
    }

    internal AsnOpenTypeValue CloneLooseOpenTypeValue(AsnOpenTypeValue value)
    {
        AsnOpenTypeValue result = new AsnOpenTypeValue
        {
            Resolved = value.Resolved,
            Unresolved = value.Unresolved,
            ResolvedTypeId = value.ResolvedTypeId,
            TypeName = value.TypeName,
            EncodingRules = value.EncodingRules,
            HasEncodingRules = value.HasEncodingRules,
            HasOriginalData = value.HasOriginalData,
            OriginalData = value.OriginalData == null
                ? []
                : (byte[])value.OriginalData.Clone(),
            OriginalTag = this.CloneOptionalTag(value.OriginalTag),
            HasEncodedLocation = value.HasEncodedLocation,
            EncodedOffset = value.EncodedOffset,
            EncodedLength = value.EncodedLength
        };
        if (value.Value != null)
        {
            result.Value = this.CloneLooseRuntimeValue(
                value.Value,
                new System.Collections.Generic.List<AsnRuntimeValue>());
        }

        if (value.DispatchKey != null)
        {
            result.DispatchKey = this.CloneLooseRuntimeValue(
                value.DispatchKey,
                new System.Collections.Generic.List<AsnRuntimeValue>());
        }

        return result;
    }

    internal AsnRuntimeValueKind GetRuntimeKind(AsnTypeDescriptor type)
    {
        AsnTypeDescriptor? effectiveType = this.GetEffectiveType(type);
        if (effectiveType == null)
        {
            return AsnRuntimeValueKind.Unknown;
        }

        type = effectiveType;
        if (type.Kind == AsnTypeKind.Boolean)
        {
            return AsnRuntimeValueKind.Boolean;
        }

        if (type.Kind == AsnTypeKind.Integer)
        {
            return AsnRuntimeValueKind.Integer;
        }

        if (type.Kind == AsnTypeKind.Enumerated)
        {
            return AsnRuntimeValueKind.Enumerated;
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return AsnRuntimeValueKind.Real;
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return AsnRuntimeValueKind.BitString;
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return AsnRuntimeValueKind.OctetString;
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return AsnRuntimeValueKind.Null;
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier)
        {
            return AsnRuntimeValueKind.ObjectIdentifier;
        }

        if (type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return AsnRuntimeValueKind.RelativeObjectIdentifier;
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifierIri)
        {
            return AsnRuntimeValueKind.ObjectIdentifierIri;
        }

        if (type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return AsnRuntimeValueKind.RelativeObjectIdentifierIri;
        }

        if (type.Kind == AsnTypeKind.Choice)
        {
            return AsnRuntimeValueKind.Choice;
        }

        if (type.Kind == AsnTypeKind.Sequence)
        {
            return AsnRuntimeValueKind.Sequence;
        }

        if (type.Kind == AsnTypeKind.Set)
        {
            return AsnRuntimeValueKind.Set;
        }

        if (type.Kind == AsnTypeKind.SequenceOf)
        {
            return AsnRuntimeValueKind.SequenceOf;
        }

        if (type.Kind == AsnTypeKind.SetOf)
        {
            return AsnRuntimeValueKind.SetOf;
        }

        if (type.Kind == AsnTypeKind.InstanceOf)
        {
            return AsnRuntimeValueKind.InstanceOf;
        }

        if (type.Kind == AsnTypeKind.External)
        {
            return AsnRuntimeValueKind.External;
        }

        if (type.Kind == AsnTypeKind.EmbeddedPdv)
        {
            return AsnRuntimeValueKind.EmbeddedPdv;
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            return AsnRuntimeValueKind.OpenType;
        }

        if (type.Kind == AsnTypeKind.UtcTime
            || type.Kind == AsnTypeKind.GeneralizedTime
            || type.Kind == AsnTypeKind.Time
            || type.Kind == AsnTypeKind.Date
            || type.Kind == AsnTypeKind.TimeOfDay
            || type.Kind == AsnTypeKind.DateTime
            || type.Kind == AsnTypeKind.Duration)
        {
            return AsnRuntimeValueKind.Time;
        }

        if (type.Kind == AsnTypeKind.CharacterString
            || type.Kind == AsnTypeKind.ObjectDescriptor
            || type.Kind == AsnTypeKind.Utf8String
            || type.Kind == AsnTypeKind.NumericString
            || type.Kind == AsnTypeKind.PrintableString
            || type.Kind == AsnTypeKind.TeletexString
            || type.Kind == AsnTypeKind.VideotexString
            || type.Kind == AsnTypeKind.Ia5String
            || type.Kind == AsnTypeKind.GraphicString
            || type.Kind == AsnTypeKind.VisibleString
            || type.Kind == AsnTypeKind.GeneralString
            || type.Kind == AsnTypeKind.UniversalString
            || type.Kind == AsnTypeKind.BmpString)
        {
            return AsnRuntimeValueKind.CharacterString;
        }

        return AsnRuntimeValueKind.Unknown;
    }

    internal AsnTypeDescriptor? GetEffectiveType(AsnTypeDescriptor type)
    {
        System.Collections.Generic.List<AsnTypeDescriptor> path =
            new System.Collections.Generic.List<AsnTypeDescriptor>();
        AsnTypeDescriptor current = type;
        while (this.IsTransparentTypeKind(current.Kind))
        {
            if (path.Contains(current) || current.ElementType == null)
            {
                return null;
            }

            path.Add(current);
            current = current.ElementType;
        }

        return current;
    }

    internal bool IsTransparentTypeKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.Tagged
            || kind == AsnTypeKind.Prefixed
            || kind == AsnTypeKind.Selection
            || kind == AsnTypeKind.Defined;
    }

    internal AsnBitStringValue CloneBitStringValue(AsnBitStringValue value)
    {
        return new AsnBitStringValue
        {
            Bytes = value.Bytes == null ? [] : (byte[])value.Bytes.Clone(),
            BitLength = value.BitLength
        };
    }

    internal AsnObjectIdentifierValue CloneObjectIdentifierValue(
        AsnObjectIdentifierValue value)
    {
        return new AsnObjectIdentifierValue
        {
            Relative = value.Relative,
            Arcs = value.Arcs == null
                ? []
                : (System.Numerics.BigInteger[])value.Arcs.Clone()
        };
    }

    internal AsnRealValue CloneRealValue(AsnRealValue value)
    {
        return new AsnRealValue
        {
            Kind = value.Kind,
            HasDecimalValue = value.HasDecimalValue,
            DecimalText = value.DecimalText,
            HasBinaryValue = value.HasBinaryValue,
            Mantissa = value.Mantissa,
            Radix = value.Radix,
            Exponent = value.Exponent
        };
    }

    internal AsnTimeValue CloneTimeValue(AsnTimeValue value)
    {
        return new AsnTimeValue
        {
            Kind = value.Kind,
            Text = value.Text,
            HasUtcOffsetMinutes = value.HasUtcOffsetMinutes,
            UtcOffsetMinutes = value.UtcOffsetMinutes,
            HasFractionalDigits = value.HasFractionalDigits,
            FractionalDigits = value.FractionalDigits
        };
    }

    internal AsnUnknownExtension[] CloneUnknownExtensions(
        AsnUnknownExtension[] values)
    {
        AsnUnknownExtension[] result = new AsnUnknownExtension[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            AsnUnknownExtension value = values[index];
            if (value == null || !value.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A preserved unknown extension must be valid.");
            }

            result[index] = new AsnUnknownExtension
            {
                Index = value.Index,
                Present = value.Present,
                Tag = this.CloneOptionalTag(value.Tag),
                HasEncodedIndex = value.HasEncodedIndex,
                EncodedIndex = value.EncodedIndex,
                HasEncodedLocation = value.HasEncodedLocation,
                EncodedOffset = value.EncodedOffset,
                EncodedLength = value.EncodedLength,
                HasEncodedBitLocation = value.HasEncodedBitLocation,
                EncodedBitOffset = value.EncodedBitOffset,
                EncodedBitLength = value.EncodedBitLength,
                HasAlignmentPaddingBitCount =
                    value.HasAlignmentPaddingBitCount,
                AlignmentPaddingBitCount = value.AlignmentPaddingBitCount,
                EncodingRules = value.EncodingRules,
                HasEncodingRules = value.HasEncodingRules,
                HasBinaryData = value.HasBinaryData,
                BinaryData = value.BinaryData == null
                    ? []
                    : (byte[])value.BinaryData.Clone(),
                HasTextData = value.HasTextData,
                TextData = value.TextData
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnUnknownField[] CloneUnknownFields(AsnUnknownField[] values)
    {
        AsnUnknownField[] result = new AsnUnknownField[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            AsnUnknownField value = values[index];
            if (value == null || !value.IsValid())
            {
                throw new System.InvalidOperationException(
                    "A preserved unknown field must be valid.");
            }

            result[index] = new AsnUnknownField
            {
                Name = value.Name,
                XmlAttribute = value.XmlAttribute,
                XmlPrefix = value.XmlPrefix,
                XmlNamespaceUri = value.XmlNamespaceUri,
                Tag = this.CloneOptionalTag(value.Tag),
                HasEncodedIndex = value.HasEncodedIndex,
                EncodedIndex = value.EncodedIndex,
                HasEncodedLocation = value.HasEncodedLocation,
                EncodedOffset = value.EncodedOffset,
                EncodedLength = value.EncodedLength,
                EncodingRules = value.EncodingRules,
                HasEncodingRules = value.HasEncodingRules,
                HasBinaryData = value.HasBinaryData,
                BinaryData = value.BinaryData == null
                    ? []
                    : (byte[])value.BinaryData.Clone(),
                HasTextData = value.HasTextData,
                TextData = value.TextData
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnTag? CloneOptionalTag(AsnTag? tag)
    {
        if (tag == null)
        {
            return null;
        }

        return new AsnTag
        {
            Class = tag.Class,
            Number = tag.Number
        };
    }
}
