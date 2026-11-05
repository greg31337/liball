namespace asn1;

internal class EcnIntegratedPlanCodec
{
    internal EcnExecutablePlan Plan;
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties;
    internal AsnTypeDescriptor? Type;
    internal AsnCodecOptions Options;
    internal AsnResourceLimits Limits;

    internal EcnIntegratedPlanCodec(
        EcnExecutablePlan plan,
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        AsnTypeDescriptor? type,
        AsnCodecOptions options)
    {
        this.Plan = plan == null
            ? throw new System.ArgumentNullException("plan")
            : plan;
        this.Category = category;
        this.Properties = properties == null
            ? throw new System.ArgumentNullException("properties")
            : properties;
        this.Type = type;
        this.Options = options == null
            ? throw new System.ArgumentNullException("options")
            : options;
        this.Limits = options.ResourceLimits == null
            ? new AsnResourceLimits()
            : options.ResourceLimits;
    }

    internal AsnEncodeResult EncodeIntegrated(
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        if (bindings == null)
        {
            throw new System.ArgumentNullException("bindings");
        }
        AsnDiagnostic[] constraints = this.ValidateConstraints(value);
        if (this.HasErrors(constraints))
        {
            this.EnrichRuntimeDiagnostics(constraints, false, 0);
            return new AsnEncodeResult
            {
                Diagnostics = constraints
            };
        }
        if (this.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject
            && this.HasUnknownData(value))
        {
            return this.CreateUnknownEncodeError();
        }
        if (this.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Skip
            && value.Kind == AsnRuntimeValueKind.OpenType
            && value.OpenTypeValue != null
            && value.OpenTypeValue.Unresolved)
        {
            return new AsnEncodeResult
            {
                Success = true,
                StreamProgress = new AsnStreamProgress
                {
                    Complete = true
                }
            };
        }
        AsnRuntimeValue effective = this.Options.UnknownDataPolicy
            == AsnUnknownDataPolicy.Skip
            ? this.CloneWithoutUnknownData(value)
            : value;
        EcnPlanExecutionBinding[] effectiveBindings = bindings.Length == 0
            && this.Type != null
            ? new EcnReflectionBindingBuilder().BuildBindings(
                this.Plan,
                this.Type)
            : bindings;
        EcnPlanExecutionResult executed = new EcnPlanExecutor(this.Limits)
            .EncodePlan(new EcnPlanExecutionRequest
            {
                Plan = this.Plan,
                Category = this.Category,
                Properties = this.Properties,
                Value = effective,
                Bindings = effectiveBindings
            });
        if (executed.Status != AsnDecodeStatus.Done)
        {
            return new AsnEncodeResult
            {
                Diagnostics = [this.CreateExecutionDiagnostic(
                    executed,
                    AsnDiagnosticCategory.Encoding)]
            };
        }
        return new AsnEncodeResult
        {
            Success = true,
            Data = executed.Data,
            WrittenCount = executed.Data.Length,
            StreamProgress = new AsnStreamProgress
            {
                WrittenCount = executed.BitCount,
                HasExpectedCount = true,
                ExpectedCount = executed.BitCount,
                Complete = true
            }
        };
    }

    internal AsnDecodeResult DecodeIntegrated(
        byte[] data,
        int bitCount,
        EcnPlanExecutionBinding[] bindings)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }
        if (bindings == null)
        {
            throw new System.ArgumentNullException("bindings");
        }
        if (bitCount < 0 || bitCount > data.Length * 8)
        {
            throw new System.ArgumentOutOfRangeException("bitCount");
        }
        EcnPlanExecutionBinding[] effectiveBindings = bindings.Length == 0
            && this.Type != null
            ? new EcnReflectionBindingBuilder().BuildBindings(
                this.Plan,
                this.Type)
            : bindings;
        EcnPlanExecutionResult executed = new EcnPlanExecutor(this.Limits)
            .DecodePlan(new EcnPlanExecutionRequest
            {
                Plan = this.Plan,
                Category = this.Category,
                Properties = this.Properties,
                Bindings = effectiveBindings,
                Data = data,
                BitCount = bitCount,
                FinalBlock = true
            });
        if (executed.Status != AsnDecodeStatus.Done || executed.Value == null)
        {
            return new AsnDecodeResult
            {
                Status = executed.Status,
                Diagnostics = [this.CreateExecutionDiagnostic(
                    executed,
                    AsnDiagnosticCategory.Decoding)]
            };
        }
        if (this.Options.UnknownDataPolicy == AsnUnknownDataPolicy.Reject
            && this.HasUnknownData(executed.Value))
        {
            return this.CreateUnknownDecodeError(
                executed.ConsumedBitCount);
        }
        AsnRuntimeValue effective = this.Options.UnknownDataPolicy
            == AsnUnknownDataPolicy.Skip
            ? this.CloneWithoutUnknownData(executed.Value)
            : executed.Value;
        AsnDiagnostic[] constraints = this.ValidateConstraints(effective);
        if (this.HasErrors(constraints))
        {
            this.EnrichRuntimeDiagnostics(
                constraints,
                true,
                executed.ConsumedBitCount);
            return new AsnDecodeResult
            {
                Status = AsnDecodeStatus.Error,
                Diagnostics = constraints
            };
        }
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = effective,
            ConsumedCount = (executed.ConsumedBitCount + 7) / 8,
            StreamProgress = new AsnStreamProgress
            {
                ConsumedCount = executed.ConsumedBitCount,
                HasExpectedCount = true,
                ExpectedCount = bitCount,
                Complete = true
            }
        };
    }

    internal AsnDiagnostic[] ValidateConstraints(AsnRuntimeValue value)
    {
        return this.Type == null || !this.Options.ValidateValues
            ? []
            : new RuntimeConstraintEvaluator().ValidateRuntimeValue(
                value,
                this.Type,
                this.Options.ResourceLimits == null
                    ? new AsnResourceLimits()
                    : this.Options.ResourceLimits);
    }

    internal bool HasErrors(AsnDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Severity == AsnDiagnosticSeverity.Error)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool HasUnknownData(AsnRuntimeValue value)
    {
        if (value.UnknownFields.Length > 0
            || value.UnknownExtensions.Length > 0
            || value.OpenTypeValue != null && value.OpenTypeValue.Unresolved)
        {
            return true;
        }
        int fieldIndex = 0;
        while (fieldIndex < value.Fields.Length)
        {
            if (value.Fields[fieldIndex].Value != null
                && this.HasUnknownData(value.Fields[fieldIndex].Value
                    ?? new AsnRuntimeValue()))
            {
                return true;
            }
            fieldIndex = fieldIndex + 1;
        }
        int itemIndex = 0;
        while (itemIndex < value.Items.Length)
        {
            if (this.HasUnknownData(value.Items[itemIndex]))
            {
                return true;
            }
            itemIndex = itemIndex + 1;
        }
        return false;
    }

    internal AsnRuntimeValue CloneWithoutUnknownData(AsnRuntimeValue source)
    {
        AsnRuntimeFieldValue[] fields =
            new AsnRuntimeFieldValue[source.Fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            fields[index] = new AsnRuntimeFieldValue
            {
                Name = source.Fields[index].Name,
                Present = source.Fields[index].Present,
                PresentDefault = source.Fields[index].PresentDefault,
                Value = source.Fields[index].Value == null
                    ? null
                    : this.CloneWithoutUnknownData(source.Fields[index].Value
                        ?? new AsnRuntimeValue())
            };
            index = index + 1;
        }
        AsnRuntimeValue[] items = new AsnRuntimeValue[source.Items.Length];
        index = 0;
        while (index < items.Length)
        {
            items[index] = this.CloneWithoutUnknownData(source.Items[index]);
            index = index + 1;
        }
        AsnOpenTypeValue? open = source.OpenTypeValue;
        if (open != null && open.Unresolved)
        {
            open = new AsnOpenTypeValue
            {
                Unresolved = true,
                DispatchKey = open.DispatchKey
            };
        }
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
            Fields = fields,
            Items = items,
            OpenTypeValue = open
        };
    }

    internal AsnEncodeResult CreateUnknownEncodeError()
    {
        return new AsnEncodeResult
        {
            Diagnostics =
            [
                this.CreatePolicyDiagnostic(
                    AsnDiagnosticCategory.Encoding,
                    "ASNECN0301",
                    "The ECN unknown-data policy rejects this value.")
            ]
        };
    }

    internal AsnDecodeResult CreateUnknownDecodeError(int bitOffset)
    {
        AsnDiagnostic diagnostic = this.CreatePolicyDiagnostic(
            AsnDiagnosticCategory.Decoding,
            "ASNECN0301",
            "The ECN unknown-data policy rejects the decoded value.");
        diagnostic.HasBitOffset = true;
        diagnostic.BitOffset = bitOffset;
        return new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Error,
            Diagnostics = [diagnostic]
        };
    }

    internal AsnDiagnostic CreateExecutionDiagnostic(
        EcnPlanExecutionResult result,
        AsnDiagnosticCategory category)
    {
        AsnDiagnosticCategory actualCategory = result.LimitExceeded
            ? AsnDiagnosticCategory.Resource
            : category;
        AsnDiagnostic diagnostic = this.CreatePolicyDiagnostic(
            actualCategory,
            result.ErrorCode.Length == 0 ? "ASNECN0300" : result.ErrorCode,
            result.ErrorMessage.Length == 0
                ? "ECN plan execution did not complete."
                : result.ErrorMessage);
        diagnostic.PlanStepId = result.PendingStepId.Length == 0
            ? diagnostic.PlanStepId
            : result.PendingStepId;
        diagnostic.ValuePath = result.PendingPath.Length == 0
            ? diagnostic.ValuePath
            : result.PendingPath;
        diagnostic.HasBitOffset = result.HasBitOffset;
        diagnostic.BitOffset = result.BitOffset;
        if (result.SourceSpan != null)
        {
            diagnostic.Span = new EcnPublicPlanBuilder().CloneSpan(
                result.SourceSpan);
            diagnostic.RelatedLocations = this.BuildRelatedLocations(
                diagnostic.Span);
        }
        return diagnostic;
    }

    internal AsnDiagnostic CreatePolicyDiagnostic(
        AsnDiagnosticCategory category,
        string code,
        string message)
    {
        AsnSourceSpan? span = this.Plan.Root == null
            ? null
            : new EcnPublicPlanBuilder().CloneSpan(
                this.Plan.Root.Trace.Span);
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = category,
            Code = code,
            Message = message,
            ValuePath = this.Plan.Root == null
                || this.Plan.Root.Path.Length == 0
                ? "$value"
                : this.Plan.Root.Path,
            PlanStepId = this.Plan.Root == null
                ? this.Plan.StableId
                : this.Plan.Root.StableId,
            Span = span,
            RelatedLocations = this.BuildRelatedLocations(span)
        };
    }

    internal void EnrichRuntimeDiagnostics(
        AsnDiagnostic[] diagnostics,
        bool decoding,
        int bitOffset)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            AsnDiagnostic diagnostic = diagnostics[index];
            if (diagnostic.PlanStepId.Length == 0)
            {
                diagnostic.PlanStepId = this.Plan.Root == null
                    ? this.Plan.StableId
                    : this.Plan.Root.StableId;
            }
            if (diagnostic.ValuePath.Length == 0)
            {
                diagnostic.ValuePath = this.Plan.Root == null
                    || this.Plan.Root.Path.Length == 0
                    ? "$value"
                    : this.Plan.Root.Path;
            }
            if (diagnostic.Span == null && this.Plan.Root != null)
            {
                diagnostic.Span = new EcnPublicPlanBuilder().CloneSpan(
                    this.Plan.Root.Trace.Span);
            }
            if (decoding)
            {
                diagnostic.HasBitOffset = true;
                diagnostic.BitOffset = bitOffset;
            }
            diagnostic.RelatedLocations = this.MergeRuntimeRelatedLocations(
                diagnostic.RelatedLocations,
                diagnostic.Span);
            index = index + 1;
        }
    }

    internal AsnDiagnosticRelatedLocation[] BuildRelatedLocations(
        AsnSourceSpan? primary)
    {
        System.Collections.Generic.List<AsnDiagnosticRelatedLocation> result =
            new System.Collections.Generic.List<AsnDiagnosticRelatedLocation>();
        if (this.Type != null && this.Type.SourceSpan != null
            && !this.SpansEqual(primary, this.Type.SourceSpan))
        {
            result.Add(new AsnDiagnosticRelatedLocation
            {
                Message = "The affected ASN.1 type is declared here.",
                Span = new EcnPublicPlanBuilder().CloneSpan(
                    this.Type.SourceSpan) ?? new AsnSourceSpan()
            });
        }
        if (this.Plan.Span != null
            && !this.SpansEqual(primary, this.Plan.Span))
        {
            result.Add(new AsnDiagnosticRelatedLocation
            {
                Message = "The ECN encoding object is declared here.",
                Span = new EcnPublicPlanBuilder().CloneSpan(
                    this.Plan.Span) ?? new AsnSourceSpan()
            });
        }
        return result.ToArray();
    }

    internal AsnDiagnosticRelatedLocation[] MergeRuntimeRelatedLocations(
        AsnDiagnosticRelatedLocation[] existing,
        AsnSourceSpan? primary)
    {
        AsnDiagnosticRelatedLocation[] additions =
            this.BuildRelatedLocations(primary);
        AsnDiagnosticRelatedLocation[] result =
            new AsnDiagnosticRelatedLocation[
                existing.Length + additions.Length];
        int index = 0;
        while (index < existing.Length)
        {
            result[index] = existing[index];
            index = index + 1;
        }
        int additionIndex = 0;
        while (additionIndex < additions.Length)
        {
            result[index] = additions[additionIndex];
            index = index + 1;
            additionIndex = additionIndex + 1;
        }
        return result;
    }

    internal bool SpansEqual(AsnSourceSpan? left, AsnSourceSpan? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return left.Start.SourceName == right.Start.SourceName
            && left.Start.Offset == right.Start.Offset
            && left.End.SourceName == right.End.SourceName
            && left.End.Offset == right.End.Offset;
    }
}
