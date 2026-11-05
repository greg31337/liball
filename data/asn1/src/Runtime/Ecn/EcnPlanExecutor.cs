namespace asn1;

internal class EcnPlanExecutor
{
    internal AsnResourceLimits Limits;
    internal EcnBitFieldOperations Bits = new EcnBitFieldOperations();

    internal EcnPlanExecutor(AsnResourceLimits limits)
    {
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
    }

    internal EcnPlanExecutionResult EncodePlan(
        EcnPlanExecutionRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Plan == null || request.Value == null)
        {
            return this.CreateExecutionError(
                "ASNECN0200",
                "ECN plan encoding requires a plan and an abstract value.",
                false,
                null);
        }
        if (!request.Plan.Valid)
        {
            return this.CreateExecutionError(
                "ASNECN0201",
                "An invalid ECN plan cannot be executed.",
                false,
                request.Plan.Root);
        }

        EcnPlanExecutionResult encoded = this.EncodeValueByPlan(
            request.Plan,
            request.Category,
            request.Properties,
            request.Value,
            request.Bindings,
            0);
        return encoded;
    }

    internal EcnPlanExecutionResult DecodePlan(
        EcnPlanExecutionRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (request.Plan == null)
        {
            EcnPlanExecutionResult missingPlan = this.CreateExecutionError(
                "ASNECN0200",
                "ECN plan decoding requires a plan.",
                false,
                null);
            missingPlan.HasBitOffset = true;
            return missingPlan;
        }
        if (request.Data == null || request.BitCount < 0
            || request.BitCount > request.Data.Length * 8)
        {
            throw new System.ArgumentOutOfRangeException("request");
        }
        if (!request.Plan.Valid || !request.Plan.Decodable)
        {
            EcnPlanExecutionResult invalidPlan = this.CreateExecutionError(
                "ASNECN0201",
                "An invalid or non-decodable ECN plan cannot be executed.",
                false,
                request.Plan.Root);
            invalidPlan.HasBitOffset = true;
            return invalidPlan;
        }

        EcnPlanBitCursor cursor = new EcnPlanBitCursor
        {
            Field = this.Bits.CreateFromBytes(request.Data, request.BitCount),
            FinalBlock = request.FinalBlock
        };
        EcnPlanExecutionResult decoded = this.DecodeValueByPlan(
            request.Plan,
            request.Category,
            request.Properties,
            request.Bindings,
            cursor,
            0);
        decoded.ConsumedBitCount = cursor.Position;
        if (decoded.Status == AsnDecodeStatus.Error)
        {
            decoded.HasBitOffset = true;
            decoded.BitOffset = cursor.Position;
        }
        return decoded;
    }

    internal EcnPlanExecutionResult EncodeValueByPlan(
        EcnExecutablePlan plan,
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        if (!this.DepthIsAllowed(depth))
        {
            return this.CreateExecutionError(
                "ASNR0001",
                "ECN plan execution exceeds the configured nesting limit.",
                true,
                plan.Root);
        }

        EcnPlanExecutionResult condition = this.CheckEncodeConditions(
            plan,
            value,
            bindings);
        if (condition.Status != AsnDecodeStatus.Done)
        {
            return condition;
        }
        if (condition.BitCount == -1)
        {
            return this.CreateEncodedSuccess(new EcnBitField());
        }

        EcnPlanExecutionBinding? rootBinding = this.FindBinding(
            bindings,
            "$value");
        EcnPlanExecutionResult? overridden = this.EncodeBoundOverride(
            rootBinding,
            value,
            bindings,
            depth,
            plan.Root);
        if (overridden != null)
        {
            return overridden;
        }

        EcnPlanStepKind kind = plan.Root == null
            ? EcnPlanStepKind.Unknown
            : plan.Root.Kind;
        if (kind == EcnPlanStepKind.Sequence
            || kind == EcnPlanStepKind.Tag)
        {
            return this.EncodeSequenceValue(
                plan,
                value,
                bindings,
                depth);
        }
        if (kind == EcnPlanStepKind.Choice)
        {
            return this.EncodeChoiceValue(plan, value, bindings, depth);
        }
        if (kind == EcnPlanStepKind.Repetition)
        {
            return this.EncodeRepetitionValue(plan, value, bindings, depth);
        }
        if (kind == EcnPlanStepKind.Optional)
        {
            EcnPlanExecutionBinding? optional = this.FindBinding(
                bindings,
                "$value");
            if (optional != null && !optional.Present)
            {
                return this.CreateEncodedSuccess(new EcnBitField());
            }
        }

        AsnRuntimeValue transformed = this.ApplyEncoderTransforms(plan, value);
        EcnClassCategory actualCategory = category == EcnClassCategory.Unknown
            ? this.InferCategory(transformed)
            : category;
        EcnPrimitiveResult primitive = new EcnPrimitiveCodecs(this.Limits)
            .EncodePrimitive(actualCategory, properties, transformed);
        return this.FromPrimitiveEncode(primitive, plan.Root);
    }

    internal EcnPlanExecutionResult DecodeValueByPlan(
        EcnExecutablePlan plan,
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        EcnPlanExecutionBinding[] bindings,
        EcnPlanBitCursor cursor,
        int depth)
    {
        if (!this.DepthIsAllowed(depth))
        {
            return this.CreateExecutionError(
                "ASNR0001",
                "ECN plan execution exceeds the configured nesting limit.",
                true,
                plan.Root);
        }

        EcnPlanExecutionResult condition = this.CheckDecodeConditions(
            plan,
            bindings,
            cursor.FinalBlock);
        if (condition.Status != AsnDecodeStatus.Done)
        {
            return condition;
        }
        if (condition.BitCount == -1)
        {
            return this.CreateDecodedSuccess(new AsnRuntimeValue(), 0);
        }

        EcnPlanExecutionBinding? rootBinding = this.FindBinding(
            bindings,
            "$value");
        EcnPlanExecutionResult? overridden = this.DecodeBoundOverride(
            rootBinding,
            bindings,
            cursor,
            depth,
            plan.Root);
        if (overridden != null)
        {
            return overridden;
        }

        EcnPlanStepKind kind = plan.Root == null
            ? EcnPlanStepKind.Unknown
            : plan.Root.Kind;
        if (kind == EcnPlanStepKind.Sequence
            || kind == EcnPlanStepKind.Tag)
        {
            return this.DecodeSequenceValue(plan, bindings, cursor, depth);
        }
        if (kind == EcnPlanStepKind.Choice)
        {
            return this.DecodeChoiceValue(plan, bindings, cursor, depth);
        }
        if (kind == EcnPlanStepKind.Repetition)
        {
            return this.DecodeRepetitionValue(plan, bindings, cursor, depth);
        }
        if (kind == EcnPlanStepKind.Optional)
        {
            EcnPlanExecutionBinding? optional = this.FindBinding(
                bindings,
                "$value");
            if (optional != null && !optional.Present)
            {
                return this.CreateDecodedSuccess(new AsnRuntimeValue(), 0);
            }
        }

        int bitCount = this.ResolvePrimitiveBitCount(
            plan,
            properties,
            rootBinding,
            cursor);
        EcnPlanExecutionResult available = this.RequireBits(
            cursor,
            bitCount,
            plan.Root);
        if (available.Status != AsnDecodeStatus.Done)
        {
            return available;
        }
        EcnBitField field = this.Bits.SliceField(
            cursor.Field,
            cursor.Position,
            bitCount);
        EcnClassCategory actualCategory = category;
        if (actualCategory == EcnClassCategory.Unknown && rootBinding != null)
        {
            actualCategory = rootBinding.Category;
        }
        if (actualCategory == EcnClassCategory.Unknown)
        {
            return this.DecodeRawField(field, cursor, plan.Root);
        }
        EcnPrimitiveResult primitive = new EcnPrimitiveCodecs(this.Limits)
            .DecodePrimitive(actualCategory, properties, field.Data, bitCount);
        EcnPlanExecutionResult result = this.FromPrimitiveDecode(
            primitive,
            plan.Root);
        if (result.Status == AsnDecodeStatus.Done && result.Value != null)
        {
            result.Value = this.ApplyDecoderTransforms(plan, result.Value);
            cursor.Position = cursor.Position + bitCount;
            result.ConsumedBitCount = bitCount;
        }
        return result;
    }

    internal EcnPlanExecutionResult EncodeSequenceValue(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        EcnBitField output = new EcnBitField();
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        int index = 0;
        while (index < fields.Length)
        {
            EcnExecutablePlanStep step = fields[index];
            AsnRuntimeFieldValue? runtimeField = this.FindRuntimeField(
                value,
                step.Name);
            EcnPlanExecutionBinding? binding = this.FindBinding(
                bindings,
                step.Path);
            bool present = runtimeField != null && runtimeField.Present;
            if (binding != null)
            {
                present = present && binding.Present;
            }
            if (!present)
            {
                if (step.Operation == "optional-field")
                {
                    index = index + 1;
                    continue;
                }
                return this.CreateExecutionError(
                    "ASNECN0202",
                    "A required ECN structure field is absent.",
                    false,
                    step);
            }
            AsnRuntimeValue fieldValue = runtimeField == null
                || runtimeField.Value == null
                ? new AsnRuntimeValue()
                : runtimeField.Value;
            fieldValue = this.ResolveWrittenDeterminant(
                plan,
                step.Path,
                fieldValue,
                value,
                bindings,
                depth);
            EcnBitField alignment = this.CreateAlignmentPadding(
                binding,
                output.BitCount);
            output = this.Bits.AppendFields(output, alignment);
            EcnPlanExecutionResult encoded = this.EncodeFieldValue(
                plan,
                step,
                fieldValue,
                binding,
                bindings,
                depth);
            if (encoded.Status != AsnDecodeStatus.Done)
            {
                return encoded;
            }
            output = this.Bits.AppendFields(
                output,
                this.Bits.CreateFromBytes(encoded.Data, encoded.BitCount));
            index = index + 1;
        }
        return this.CreateEncodedSuccess(output);
    }

    internal EcnPlanExecutionResult EncodeChoiceValue(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        int selected = -1;
        int index = 0;
        while (index < fields.Length)
        {
            AsnRuntimeFieldValue? runtimeField = this.FindRuntimeField(
                value,
                fields[index].Name);
            EcnPlanExecutionBinding? binding = this.FindBinding(
                bindings,
                fields[index].Path);
            if ((runtimeField != null && runtimeField.Present)
                || (binding != null && binding.Selected))
            {
                if (selected >= 0)
                {
                    return this.CreateExecutionError(
                        "ASNECN0203",
                        "Exactly one ECN alternative must be selected.",
                        false,
                        fields[index]);
                }
                selected = index;
            }
            index = index + 1;
        }
        if (selected < 0)
        {
            return this.CreateExecutionError(
                "ASNECN0203",
                "Exactly one ECN alternative must be selected.",
                false,
                plan.Root);
        }
        EcnExecutablePlanStep step = fields[selected];
        AsnRuntimeFieldValue? selectedField = this.FindRuntimeField(
            value,
            step.Name);
        EcnPlanExecutionBinding? selectedBinding = this.FindBinding(
            bindings,
            step.Path);
        AsnRuntimeValue selectedValue = selectedField == null
            || selectedField.Value == null
            ? selectedBinding == null || selectedBinding.Value == null
                ? new AsnRuntimeValue()
                : selectedBinding.Value
            : selectedField.Value;
        return this.EncodeFieldValue(
            plan,
            step,
            selectedValue,
            selectedBinding,
            bindings,
            depth);
    }

    internal EcnPlanExecutionResult EncodeRepetitionValue(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(
            value.Items.Length))
        {
            return this.CreateExecutionError(
                "ASNR0002",
                "The ECN repetition exceeds the configured collection limit.",
                true,
                plan.Root);
        }
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        EcnExecutablePlanStep? element = fields.Length == 0
            ? null
            : fields[0];
        EcnPlanExecutionBinding? binding = element == null
            ? this.FindRepetitionBinding(bindings)
            : this.FindBinding(bindings, element.Path);
        EcnBitField output = new EcnBitField();
        int index = 0;
        while (index < value.Items.Length)
        {
            EcnPlanExecutionResult encoded = this.EncodeFieldValue(
                plan,
                element,
                value.Items[index],
                binding,
                bindings,
                depth);
            if (encoded.Status != AsnDecodeStatus.Done)
            {
                return encoded;
            }
            output = this.Bits.AppendFields(
                output,
                this.Bits.CreateFromBytes(encoded.Data, encoded.BitCount));
            index = index + 1;
        }
        return this.CreateEncodedSuccess(output);
    }

    internal EcnPlanExecutionResult EncodeFieldValue(
        EcnExecutablePlan owner,
        EcnExecutablePlanStep? step,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding? binding,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        EcnPlanExecutionResult? overridden = this.EncodeBoundOverride(
            binding,
            value,
            bindings,
            depth,
            step);
        if (overridden != null)
        {
            return overridden;
        }
        EcnClassCategory category = binding == null
            || binding.Category == EcnClassCategory.Unknown
            ? this.InferCategory(value)
            : binding.Category;
        EcnEncodingProperty[] properties = binding == null
            ? []
            : binding.Properties;
        EcnPrimitiveResult primitive = new EcnPrimitiveCodecs(this.Limits)
            .EncodePrimitive(category, properties, value);
        return this.FromPrimitiveEncode(primitive, step);
    }

    internal EcnPlanExecutionResult? EncodeBoundOverride(
        EcnPlanExecutionBinding? binding,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings,
        int depth,
        EcnExecutablePlanStep? step)
    {
        if (binding == null)
        {
            return null;
        }
        if (binding.EncodedValue != null)
        {
            return this.CreateEncodedSuccess(binding.EncodedValue);
        }
        if (binding.Plan != null)
        {
            return this.EncodeValueByPlan(
                binding.Plan,
                binding.Category,
                binding.Properties,
                value,
                binding.NestedBindings,
                depth + 1);
        }
        return null;
    }

    internal EcnPlanExecutionResult DecodeSequenceValue(
        EcnExecutablePlan plan,
        EcnPlanExecutionBinding[] bindings,
        EcnPlanBitCursor cursor,
        int depth)
    {
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        System.Collections.Generic.List<AsnRuntimeFieldValue> values =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        int start = cursor.Position;
        int index = 0;
        while (index < fields.Length)
        {
            EcnExecutablePlanStep step = fields[index];
            EcnPlanExecutionBinding? binding = this.FindBinding(
                bindings,
                step.Path);
            bool present = binding == null || binding.Present;
            if (!present && step.Operation == "optional-field")
            {
                values.Add(new AsnRuntimeFieldValue
                {
                    Name = step.Name
                });
                index = index + 1;
                continue;
            }
            EcnPlanExecutionResult alignment = this.ConsumeAlignmentPadding(
                binding,
                cursor,
                step);
            if (alignment.Status != AsnDecodeStatus.Done)
            {
                return alignment;
            }
            EcnPlanExecutionResult decoded = this.DecodeFieldValue(
                plan,
                step,
                binding,
                bindings,
                values,
                cursor,
                depth);
            if (decoded.Status != AsnDecodeStatus.Done)
            {
                return decoded;
            }
            values.Add(new AsnRuntimeFieldValue
            {
                Name = step.Name,
                Present = true,
                Value = decoded.Value
            });
            if (binding != null)
            {
                binding.Value = decoded.Value;
            }
            index = index + 1;
        }
        return this.CreateDecodedSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = values.ToArray()
        }, cursor.Position - start);
    }

    internal EcnBitField CreateAlignmentPadding(
        EcnPlanExecutionBinding? binding,
        int bitPosition)
    {
        int count = this.ResolveAlignmentPaddingBitCount(
            binding,
            bitPosition);
        bool[] bits = new bool[count];
        if (this.AlignmentPaddingUsesOne(binding))
        {
            int index = 0;
            while (index < bits.Length)
            {
                bits[index] = true;
                index = index + 1;
            }
        }
        return this.Bits.FromBits(bits);
    }

    internal EcnPlanExecutionResult ConsumeAlignmentPadding(
        EcnPlanExecutionBinding? binding,
        EcnPlanBitCursor cursor,
        EcnExecutablePlanStep? step)
    {
        int count = this.ResolveAlignmentPaddingBitCount(
            binding,
            cursor.Position);
        EcnPlanExecutionResult available = this.RequireBits(
            cursor,
            count,
            step);
        if (available.Status != AsnDecodeStatus.Done)
        {
            return available;
        }
        if (count > 0)
        {
            bool expected = this.AlignmentPaddingUsesOne(binding);
            EcnBitField padding = this.Bits.SliceField(
                cursor.Field,
                cursor.Position,
                count);
            bool[] bits = this.Bits.ToBits(padding);
            int index = 0;
            while (index < bits.Length)
            {
                if (bits[index] != expected)
                {
                    return this.CreateExecutionError(
                        "ASNECN0210",
                        "ECN alignment padding does not match the configured padding bit.",
                        false,
                        step);
                }
                index = index + 1;
            }
            cursor.Position = cursor.Position + count;
        }
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedBitCount = count
        };
    }

    internal int ResolveAlignmentPaddingBitCount(
        EcnPlanExecutionBinding? binding,
        int bitPosition)
    {
        if (binding == null)
        {
            return 0;
        }
        int index = 0;
        while (index < binding.Properties.Length)
        {
            EcnEncodingProperty property = binding.Properties[index];
            if (property.Kind == EcnEncodingPropertyKind.Alignment)
            {
                int unit = new EcnPrimitiveSettingsResolver().GetUnitBits(
                    property.Unit);
                if (unit > 0)
                {
                    int remainder = bitPosition % unit;
                    return remainder == 0 ? 0 : unit - remainder;
                }
            }
            index = index + 1;
        }
        return 0;
    }

    internal bool AlignmentPaddingUsesOne(
        EcnPlanExecutionBinding? binding)
    {
        if (binding == null)
        {
            return false;
        }
        int index = 0;
        while (index < binding.Properties.Length)
        {
            EcnEncodingProperty property = binding.Properties[index];
            if (property.Kind == EcnEncodingPropertyKind.Padding)
            {
                return property.Value.ToLowerInvariant() == "one";
            }
            index = index + 1;
        }
        return false;
    }

    internal EcnPlanExecutionResult DecodeChoiceValue(
        EcnExecutablePlan plan,
        EcnPlanExecutionBinding[] bindings,
        EcnPlanBitCursor cursor,
        int depth)
    {
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        EcnExecutablePlanStep? selected = null;
        EcnPlanExecutionBinding? binding = null;
        int index = 0;
        while (index < fields.Length)
        {
            EcnPlanExecutionBinding? candidate = this.FindBinding(
                bindings,
                fields[index].Path);
            if (candidate != null && candidate.Selected)
            {
                if (selected != null)
                {
                    return this.CreateExecutionError(
                        "ASNECN0203",
                        "Exactly one ECN alternative must be selected.",
                        false,
                        fields[index]);
                }
                selected = fields[index];
                binding = candidate;
            }
            index = index + 1;
        }
        if (selected == null)
        {
            return this.CreatePendingOrError(
                cursor.FinalBlock,
                "ASNECN0204",
                "An ECN choice determinant is not yet available.",
                plan.Root);
        }
        System.Collections.Generic.List<AsnRuntimeFieldValue> prior =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        int start = cursor.Position;
        EcnPlanExecutionResult decoded = this.DecodeFieldValue(
            plan,
            selected,
            binding,
            bindings,
            prior,
            cursor,
            depth);
        if (decoded.Status != AsnDecodeStatus.Done)
        {
            return decoded;
        }
        return this.CreateDecodedSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = selected.Name,
                    Present = true,
                    Value = decoded.Value
                }
            ]
        }, cursor.Position - start);
    }

    internal EcnPlanExecutionResult DecodeRepetitionValue(
        EcnExecutablePlan plan,
        EcnPlanExecutionBinding[] bindings,
        EcnPlanBitCursor cursor,
        int depth)
    {
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        EcnExecutablePlanStep? element = fields.Length == 0
            ? null
            : fields[0];
        EcnPlanExecutionBinding? binding = element == null
            ? this.FindRepetitionBinding(bindings)
            : this.FindBinding(bindings, element.Path);
        int count = binding == null ? -1 : binding.RepetitionCount;
        if (count < 0)
        {
            count = this.ResolveRepetitionCount(plan, bindings);
        }
        if (count < 0)
        {
            return this.CreatePendingOrError(
                cursor.FinalBlock,
                "ASNECN0205",
                "The ECN repetition count is not yet available.",
                plan.Root);
        }
        if (!new ResourceCounter(this.Limits).AllowsCollectionElements(count))
        {
            return this.CreateExecutionError(
                "ASNR0002",
                "The ECN repetition exceeds the configured collection limit.",
                true,
                plan.Root);
        }
        System.Collections.Generic.List<AsnRuntimeValue> items =
            new System.Collections.Generic.List<AsnRuntimeValue>();
        System.Collections.Generic.List<AsnRuntimeFieldValue> prior =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>();
        int start = cursor.Position;
        int index = 0;
        while (index < count)
        {
            EcnPlanExecutionResult decoded = this.DecodeFieldValue(
                plan,
                element,
                binding,
                bindings,
                prior,
                cursor,
                depth);
            if (decoded.Status != AsnDecodeStatus.Done
                || decoded.Value == null)
            {
                return decoded;
            }
            items.Add(decoded.Value);
            index = index + 1;
        }
        return this.CreateDecodedSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = items.ToArray()
        }, cursor.Position - start);
    }

    internal EcnPlanExecutionResult DecodeFieldValue(
        EcnExecutablePlan owner,
        EcnExecutablePlanStep? step,
        EcnPlanExecutionBinding? binding,
        EcnPlanExecutionBinding[] bindings,
        System.Collections.Generic.List<AsnRuntimeFieldValue> prior,
        EcnPlanBitCursor cursor,
        int depth)
    {
        EcnPlanExecutionResult? overridden = this.DecodeBoundOverride(
            binding,
            bindings,
            cursor,
            depth,
            step);
        if (overridden != null)
        {
            return overridden;
        }
        int bitCount = binding == null ? -1 : binding.BitCount;
        if (bitCount < 0 && step != null)
        {
            bitCount = this.ResolveDeterminantBitCount(
                owner,
                step.Path,
                bindings,
                prior);
        }
        if (bitCount < 0)
        {
            return this.CreatePendingOrError(
                cursor.FinalBlock,
                "ASNECN0206",
                "The ECN field size is not yet available.",
                step);
        }
        EcnPlanExecutionResult available = this.RequireBits(
            cursor,
            bitCount,
            step);
        if (available.Status != AsnDecodeStatus.Done)
        {
            return available;
        }
        EcnBitField field = this.Bits.SliceField(
            cursor.Field,
            cursor.Position,
            bitCount);
        EcnClassCategory category = binding == null
            ? EcnClassCategory.Unknown
            : binding.Category;
        if (category == EcnClassCategory.Unknown)
        {
            return this.DecodeRawField(field, cursor, step);
        }
        EcnEncodingProperty[] properties = binding == null
            ? []
            : binding.Properties;
        EcnPrimitiveResult primitive = new EcnPrimitiveCodecs(this.Limits)
            .DecodePrimitive(category, properties, field.Data, bitCount);
        EcnPlanExecutionResult result = this.FromPrimitiveDecode(
            primitive,
            step);
        if (result.Status == AsnDecodeStatus.Done)
        {
            cursor.Position = cursor.Position + bitCount;
            result.ConsumedBitCount = bitCount;
        }
        return result;
    }

    internal EcnPlanExecutionResult? DecodeBoundOverride(
        EcnPlanExecutionBinding? binding,
        EcnPlanExecutionBinding[] bindings,
        EcnPlanBitCursor cursor,
        int depth,
        EcnExecutablePlanStep? step)
    {
        if (binding == null || binding.Plan == null)
        {
            return null;
        }
        int bitCount = binding.BitCount;
        if (bitCount < 0)
        {
            bitCount = this.FixedPlanBitCount(binding.Plan);
        }
        EcnPlanExecutionResult available = this.RequireBits(
            cursor,
            bitCount,
            step);
        if (available.Status != AsnDecodeStatus.Done)
        {
            return available;
        }
        EcnBitField field = this.Bits.SliceField(
            cursor.Field,
            cursor.Position,
            bitCount);
        EcnPlanBitCursor nested = new EcnPlanBitCursor
        {
            Field = field,
            FinalBlock = true
        };
        EcnPlanExecutionResult decoded = this.DecodeValueByPlan(
            binding.Plan,
            binding.Category,
            binding.Properties,
            binding.NestedBindings,
            nested,
            depth + 1);
        if (decoded.Status == AsnDecodeStatus.Done)
        {
            cursor.Position = cursor.Position + nested.Position;
            decoded.ConsumedBitCount = nested.Position;
        }
        return decoded;
    }

    internal EcnPlanExecutionResult CheckEncodeConditions(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.Condition)
            {
                AsnRuntimeValue? control = this.FindControlValue(
                    step.Path,
                    value,
                    bindings);
                if (control == null)
                {
                    return this.CreateExecutionError(
                        "ASNECN0204",
                        "An ECN condition value is unavailable.",
                        false,
                        step);
                }
                if (!this.ConditionMatches(step, control))
                {
                    EcnPlanExecutionResult skipped =
                        this.CreateEncodedSuccess(new EcnBitField());
                    skipped.BitCount = -1;
                    return skipped;
                }
            }
            index = index + 1;
        }
        return this.CreateEncodedSuccess(new EcnBitField());
    }

    internal EcnPlanExecutionResult CheckDecodeConditions(
        EcnExecutablePlan plan,
        EcnPlanExecutionBinding[] bindings,
        bool finalBlock)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.Condition)
            {
                EcnPlanExecutionBinding? binding = this.FindBinding(
                    bindings,
                    step.Path);
                if (binding == null || binding.Value == null)
                {
                    return this.CreatePendingOrError(
                        finalBlock,
                        "ASNECN0204",
                        "An ECN condition value is not yet available.",
                        step);
                }
                if (!this.ConditionMatches(step, binding.Value))
                {
                    EcnPlanExecutionResult skipped =
                        this.CreateDecodedSuccess(new AsnRuntimeValue(), 0);
                    skipped.BitCount = -1;
                    return skipped;
                }
            }
            index = index + 1;
        }
        return this.CreateDecodedSuccess(new AsnRuntimeValue(), 0);
    }

    internal bool ConditionMatches(
        EcnExecutablePlanStep step,
        AsnRuntimeValue value)
    {
        if (step.Operation.Length == 0)
        {
            return value.Kind == AsnRuntimeValueKind.Boolean
                && value.HasBooleanValue
                && value.BooleanValue;
        }
        if (value.HasIntegerValue)
        {
            System.Numerics.BigInteger expected =
                this.ParseSmallInteger(step.Value);
            if (step.Operation == "=" || step.Operation == "==")
            {
                return value.IntegerValue == expected;
            }
            if (step.Operation == "!=")
            {
                return value.IntegerValue != expected;
            }
            if (step.Operation == "<")
            {
                return value.IntegerValue < expected;
            }
            if (step.Operation == ">")
            {
                return value.IntegerValue > expected;
            }
            if (step.Operation == "<=")
            {
                return value.IntegerValue <= expected;
            }
            if (step.Operation == ">=")
            {
                return value.IntegerValue >= expected;
            }
        }
        if (value.Kind == AsnRuntimeValueKind.Boolean
            && value.HasBooleanValue)
        {
            bool expected = !string.Equals(
                step.Value,
                "FALSE",
                System.StringComparison.OrdinalIgnoreCase);
            return step.Operation == "!="
                ? value.BooleanValue != expected
                : value.BooleanValue == expected;
        }
        return false;
    }

    internal AsnRuntimeValue? FindControlValue(
        string path,
        AsnRuntimeValue root,
        EcnPlanExecutionBinding[] bindings)
    {
        EcnPlanExecutionBinding? binding = this.FindBinding(bindings, path);
        if (binding != null && binding.Value != null)
        {
            return binding.Value;
        }
        AsnRuntimeFieldValue? field = this.FindRuntimeField(root, path);
        return field == null ? null : field.Value;
    }

    internal AsnRuntimeValue ResolveWrittenDeterminant(
        EcnExecutablePlan plan,
        string path,
        AsnRuntimeValue current,
        AsnRuntimeValue root,
        EcnPlanExecutionBinding[] bindings,
        int depth)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.DeterminantWrite)
            {
                EcnPlanDependency? dependency = this.FindDependency(
                    step,
                    EcnPlanDependencyKind.DeterminantValue);
                if (dependency != null && dependency.Path == path)
                {
                    AsnRuntimeFieldValue? controlled = this.FindRuntimeField(
                        root,
                        step.Path);
                    EcnPlanExecutionBinding? controlledBinding =
                        this.FindBinding(bindings, step.Path);
                    if (controlled != null && controlled.Value != null)
                    {
                        EcnPlanExecutionResult encoded = this.EncodeFieldValue(
                            plan,
                            this.FindStructuralField(plan, step.Path),
                            controlled.Value,
                            controlledBinding,
                            bindings,
                            depth);
                        if (encoded.Status == AsnDecodeStatus.Done)
                        {
                            return new AsnRuntimeValue
                            {
                                Kind = AsnRuntimeValueKind.Integer,
                                HasIntegerValue = true,
                                IntegerValue = encoded.BitCount
                            };
                        }
                    }
                }
            }
            index = index + 1;
        }
        return current;
    }

    internal int ResolveDeterminantBitCount(
        EcnExecutablePlan plan,
        string path,
        EcnPlanExecutionBinding[] bindings,
        System.Collections.Generic.List<AsnRuntimeFieldValue> prior)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if ((step.Kind == EcnPlanStepKind.DeterminantRead
                    || step.Kind == EcnPlanStepKind.DeterminantWrite)
                && step.Path == path)
            {
                EcnPlanDependency? dependency = this.FindDependency(
                    step,
                    EcnPlanDependencyKind.DeterminantValue);
                if (dependency != null)
                {
                    AsnRuntimeValue? value = this.FindPriorValue(
                        prior,
                        dependency.Path);
                    EcnPlanExecutionBinding? binding = this.FindBinding(
                        bindings,
                        dependency.Path);
                    if (value == null && binding != null)
                    {
                        value = binding.Value;
                    }
                    if (value != null && value.HasIntegerValue
                        && value.IntegerValue >= 0
                        && value.IntegerValue <= int.MaxValue)
                    {
                        return (int)value.IntegerValue;
                    }
                }
            }
            index = index + 1;
        }
        return -1;
    }

    internal int ResolveRepetitionCount(
        EcnExecutablePlan plan,
        EcnPlanExecutionBinding[] bindings)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.DeterminantRead
                || step.Kind == EcnPlanStepKind.DeterminantWrite)
            {
                EcnPlanDependency? dependency = this.FindDependency(
                    step,
                    EcnPlanDependencyKind.DeterminantValue);
                EcnPlanExecutionBinding? binding = dependency == null
                    ? null
                    : this.FindBinding(bindings, dependency.Path);
                if (binding != null && binding.Value != null
                    && binding.Value.HasIntegerValue
                    && binding.Value.IntegerValue >= 0
                    && binding.Value.IntegerValue <= int.MaxValue)
                {
                    return (int)binding.Value.IntegerValue;
                }
            }
            index = index + 1;
        }
        return -1;
    }

    internal EcnPlanDependency? FindDependency(
        EcnExecutablePlanStep step,
        EcnPlanDependencyKind kind)
    {
        int index = 0;
        while (index < step.Dependencies.Length)
        {
            if (step.Dependencies[index].Kind == kind)
            {
                return step.Dependencies[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal EcnExecutablePlanStep[] GetStructuralFields(
        EcnExecutablePlan plan)
    {
        System.Collections.Generic.List<EcnExecutablePlanStep> fields =
            new System.Collections.Generic.List<EcnExecutablePlanStep>();
        int index = 0;
        while (index < plan.Steps.Length)
        {
            if (plan.Steps[index].Kind == EcnPlanStepKind.Field
                && plan.Steps[index].Trace.SemanticKind == "field")
            {
                fields.Add(plan.Steps[index]);
            }
            index = index + 1;
        }
        return fields.ToArray();
    }

    internal EcnExecutablePlanStep? FindStructuralField(
        EcnExecutablePlan plan,
        string path)
    {
        EcnExecutablePlanStep[] fields = this.GetStructuralFields(plan);
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Path == path)
            {
                return fields[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal EcnPlanExecutionBinding? FindBinding(
        EcnPlanExecutionBinding[] bindings,
        string path)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index].Path == path)
            {
                return bindings[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal EcnPlanExecutionBinding? FindRepetitionBinding(
        EcnPlanExecutionBinding[] bindings)
    {
        EcnPlanExecutionBinding? named = this.FindBinding(bindings, "$item");
        if (named != null)
        {
            return named;
        }
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index].RepetitionCount >= 0)
            {
                return bindings[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal AsnRuntimeFieldValue? FindRuntimeField(
        AsnRuntimeValue value,
        string name)
    {
        int index = 0;
        while (index < value.Fields.Length)
        {
            if (value.Fields[index].Name == name)
            {
                return value.Fields[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal AsnRuntimeValue? FindPriorValue(
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields,
        string name)
    {
        int index = 0;
        while (index < fields.Count)
        {
            if (fields[index].Name == name)
            {
                return fields[index].Value;
            }
            index = index + 1;
        }
        return null;
    }

    internal int ResolvePrimitiveBitCount(
        EcnExecutablePlan plan,
        EcnEncodingProperty[] properties,
        EcnPlanExecutionBinding? binding,
        EcnPlanBitCursor cursor)
    {
        if (binding != null && binding.BitCount >= 0)
        {
            return binding.BitCount;
        }
        EcnPrimitiveSettingsResult settings =
            new EcnPrimitiveSettingsResolver().ResolveSettings(properties);
        if (settings.Success && settings.Settings != null
            && settings.Settings.SizeBits >= 0)
        {
            return settings.Settings.SizeBits;
        }
        int fixedPlan = this.FixedPlanBitCount(plan);
        if (fixedPlan >= 0)
        {
            return fixedPlan;
        }
        return cursor.FinalBlock
            ? cursor.Field.BitCount - cursor.Position
            : -1;
    }

    internal int FixedPlanBitCount(EcnExecutablePlan plan)
    {
        if (plan.Bounds.Exact && plan.Bounds.HasMinimumBits
            && plan.Bounds.HasMaximumBits
            && plan.Bounds.MinimumBits == plan.Bounds.MaximumBits
            && plan.Bounds.MinimumBits >= 0
            && plan.Bounds.MinimumBits <= int.MaxValue)
        {
            return (int)plan.Bounds.MinimumBits;
        }
        return -1;
    }

    internal EcnPlanExecutionResult RequireBits(
        EcnPlanBitCursor cursor,
        int bitCount,
        EcnExecutablePlanStep? step)
    {
        if (bitCount < 0)
        {
            return this.CreatePendingOrError(
                cursor.FinalBlock,
                "ASNECN0206",
                "The ECN field size is not yet available.",
                step);
        }
        if (bitCount <= cursor.Field.BitCount - cursor.Position)
        {
            return new EcnPlanExecutionResult
            {
                Status = AsnDecodeStatus.Done
            };
        }
        return this.CreatePendingOrError(
            cursor.FinalBlock,
            "ASNECN0207",
            "The final ECN input ends within an executable plan step.",
            step);
    }

    internal EcnPlanExecutionResult DecodeRawField(
        EcnBitField field,
        EcnPlanBitCursor cursor,
        EcnExecutablePlanStep? step)
    {
        if (!new ResourceCounter(this.Limits).AllowsOpenTypeBytes(
            field.Data.Length))
        {
            return this.CreateExecutionError(
                "ASNR0007",
                "The ECN raw field exceeds the configured open-type limit.",
                true,
                step);
        }
        cursor.Position = cursor.Position + field.BitCount;
        return this.CreateDecodedSuccess(new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = field.Data,
                BitLength = field.BitCount
            }
        }, field.BitCount);
    }

    internal AsnRuntimeValue ApplyEncoderTransforms(
        EcnExecutablePlan plan,
        AsnRuntimeValue value)
    {
        AsnRuntimeValue current = this.CopyScalarValue(value);
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.Transformation && step.Encodes)
            {
                current = this.ApplyIntegerTransform(current, step, false);
            }
            index = index + 1;
        }
        return current;
    }

    internal AsnRuntimeValue ApplyDecoderTransforms(
        EcnExecutablePlan plan,
        AsnRuntimeValue value)
    {
        AsnRuntimeValue current = this.CopyScalarValue(value);
        int index = plan.Steps.Length - 1;
        while (index >= 0)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Kind == EcnPlanStepKind.Transformation && step.Decodes)
            {
                current = this.ApplyIntegerTransform(current, step, true);
            }
            index = index - 1;
        }
        return current;
    }

    internal AsnRuntimeValue ApplyIntegerTransform(
        AsnRuntimeValue value,
        EcnExecutablePlanStep step,
        bool inverse)
    {
        if (!value.HasIntegerValue)
        {
            return value;
        }
        System.Numerics.BigInteger argument = this.ParseSmallInteger(step.Value);
        string operation = step.Operation.ToLowerInvariant();
        if ((operation == "add" && !inverse)
            || (operation == "subtract" && inverse))
        {
            value.IntegerValue = value.IntegerValue + argument;
        }
        else if ((operation == "subtract" && !inverse)
            || (operation == "add" && inverse))
        {
            value.IntegerValue = value.IntegerValue - argument;
        }
        else if (operation == "multiply" && !inverse)
        {
            value.IntegerValue = value.IntegerValue * argument;
        }
        else if (operation == "multiply" && inverse
            && argument != System.Numerics.BigInteger.Zero)
        {
            value.IntegerValue = value.IntegerValue / argument;
        }
        else if (operation == "divide" && !inverse
            && argument != System.Numerics.BigInteger.Zero)
        {
            value.IntegerValue = value.IntegerValue / argument;
        }
        else if (operation == "divide" && inverse)
        {
            value.IntegerValue = value.IntegerValue * argument;
        }
        else if (operation == "negate")
        {
            value.IntegerValue = -value.IntegerValue;
        }
        return value;
    }

    internal AsnRuntimeValue CopyScalarValue(AsnRuntimeValue source)
    {
        return new AsnRuntimeValue
        {
            Kind = source.Kind,
            Value = source.Value,
            HasBooleanValue = source.HasBooleanValue,
            BooleanValue = source.BooleanValue,
            HasIntegerValue = source.HasIntegerValue,
            IntegerValue = source.IntegerValue,
            TextValue = source.TextValue,
            BitStringValue = source.BitStringValue,
            Octets = source.Octets,
            ObjectIdentifierValue = source.ObjectIdentifierValue,
            RealValue = source.RealValue,
            TimeValue = source.TimeValue,
            Fields = source.Fields,
            Items = source.Items,
            OpenTypeValue = source.OpenTypeValue,
            UnknownExtensions = source.UnknownExtensions,
            UnknownFields = source.UnknownFields
        };
    }

    internal System.Numerics.BigInteger ParseSmallInteger(string text)
    {
        bool negative = text.Length > 0 && text[0] == '-';
        int index = text.Length > 0 && (text[0] == '-' || text[0] == '+')
            ? 1
            : 0;
        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            value = value * 10 + text[index] - '0';
            index = index + 1;
        }
        return negative ? -value : value;
    }

    internal EcnClassCategory InferCategory(AsnRuntimeValue value)
    {
        switch (value.Kind)
        {
            case AsnRuntimeValueKind.Boolean:
                return EcnClassCategory.Boolean;
            case AsnRuntimeValueKind.Integer:
            case AsnRuntimeValueKind.Enumerated:
                return EcnClassCategory.Integer;
            case AsnRuntimeValueKind.Real:
                return EcnClassCategory.Real;
            case AsnRuntimeValueKind.BitString:
                return EcnClassCategory.BitString;
            case AsnRuntimeValueKind.OctetString:
                return EcnClassCategory.OctetString;
            case AsnRuntimeValueKind.CharacterString:
                return EcnClassCategory.CharacterString;
            case AsnRuntimeValueKind.ObjectIdentifier:
            case AsnRuntimeValueKind.RelativeObjectIdentifier:
                return EcnClassCategory.ObjectIdentifier;
            case AsnRuntimeValueKind.Null:
                return EcnClassCategory.Null;
            case AsnRuntimeValueKind.OpenType:
                return EcnClassCategory.OpenType;
            default:
                return EcnClassCategory.Unknown;
        }
    }

    internal bool DepthIsAllowed(int depth)
    {
        return this.Limits.Unlimited
            || depth < this.Limits.MaximumNestingDepth;
    }

    internal EcnPlanExecutionResult FromPrimitiveEncode(
        EcnPrimitiveResult primitive,
        EcnExecutablePlanStep? step)
    {
        if (!primitive.Success)
        {
            return this.CreateExecutionError(
                primitive.ErrorCode,
                primitive.ErrorMessage,
                primitive.LimitExceeded,
                step);
        }
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            Data = primitive.Data,
            BitCount = primitive.BitCount
        };
    }

    internal EcnPlanExecutionResult FromPrimitiveDecode(
        EcnPrimitiveResult primitive,
        EcnExecutablePlanStep? step)
    {
        if (!primitive.Success || primitive.Value == null)
        {
            return this.CreateExecutionError(
                primitive.ErrorCode,
                primitive.ErrorMessage,
                primitive.LimitExceeded,
                step);
        }
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            Value = primitive.Value,
            BitCount = primitive.BitCount
        };
    }

    internal EcnPlanExecutionResult CreateEncodedSuccess(EcnBitField field)
    {
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            Data = field.Data,
            BitCount = field.BitCount
        };
    }

    internal EcnPlanExecutionResult CreateDecodedSuccess(
        AsnRuntimeValue value,
        int consumedBitCount)
    {
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            Value = value,
            ConsumedBitCount = consumedBitCount
        };
    }

    internal EcnPlanExecutionResult CreatePendingOrError(
        bool finalBlock,
        string code,
        string message,
        EcnExecutablePlanStep? step)
    {
        if (finalBlock)
        {
            return this.CreateExecutionError(code, message, false, step);
        }
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.NeedMoreData,
            PendingStepId = step == null ? "" : step.StableId,
            PendingPath = step == null ? "" : step.Path,
            SourceSpan = step == null ? null : step.Trace.Span
        };
    }

    internal EcnPlanExecutionResult CreateExecutionError(
        string code,
        string message,
        bool limitExceeded,
        EcnExecutablePlanStep? step)
    {
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Error,
            PendingStepId = step == null ? "" : step.StableId,
            PendingPath = step == null ? "" : step.Path,
            SourceSpan = step == null ? null : step.Trace.Span,
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
