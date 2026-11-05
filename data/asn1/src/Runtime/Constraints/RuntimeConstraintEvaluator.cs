namespace asn1;

internal class RuntimeConstraintEvaluator
{
    internal AsnResourceLimits Limits = new AsnResourceLimits();
    internal ResourceCounter Resources = new ResourceCounter(new AsnResourceLimits());

    internal AsnDiagnostic[] ValidateRuntimeValue(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        AsnResourceLimits limits)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        this.Limits = limits;
        this.Resources = new ResourceCounter(limits);

        System.Collections.Generic.List<AsnDiagnostic> diagnostics =
            new System.Collections.Generic.List<AsnDiagnostic>();
        if (type.Resolution == null)
        {
            this.ValidatePublicConstraints(value, type, "$", diagnostics);
        }
        else
        {
            this.ValidateResolvedValue(
                value,
                type.Resolution,
                "$",
                diagnostics);
        }

        return diagnostics.ToArray();
    }

    internal void ValidateResolvedValue(
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path,
        System.Collections.Generic.List<AsnDiagnostic> diagnostics)
    {
        if (!this.CanContinueValidation())
        {
            return;
        }

        if (!this.Resources.TryEnterNesting())
        {
            this.TryAddDiagnostic(
                diagnostics,
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Resource,
                    Code = "ASNR0002",
                    Message = "Runtime constraint validation exceeds the configured nesting-depth limit at "
                        + path + ".",
                    ValuePath = path
                });
            return;
        }

        if (type.EffectiveConstraint != null)
        {
            RuntimeConstraintEvaluationResult result = this.EvaluateGraph(
                type.EffectiveConstraint,
                value,
                type,
                path);
            if (!result.Success)
            {
                this.TryAddDiagnostic(
                    diagnostics,
                    this.CreateDiagnostic(result, type));
            }
        }

        if (type.EffectiveKind == AsnTypeKind.SequenceOf
            || type.EffectiveKind == AsnTypeKind.SetOf)
        {
            if (type.InnerType != null)
            {
                int itemIndex = 0;
                while (itemIndex < value.Items.Length
                    && this.CanContinueValidation())
                {
                    this.ValidateResolvedValue(
                        value.Items[itemIndex],
                        type.InnerType,
                        path + "[" + itemIndex.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + "]",
                        diagnostics);
                    itemIndex = itemIndex + 1;
                }
            }

            this.Resources.LeaveNesting();
            return;
        }

        if (type.EffectiveKind == AsnTypeKind.Sequence
            || type.EffectiveKind == AsnTypeKind.Set
            || type.EffectiveKind == AsnTypeKind.Choice)
        {
            int componentIndex = 0;
            while (componentIndex < type.Components.Length
                && this.CanContinueValidation())
            {
                ResolvedComponentSyntax component = type.Components[componentIndex];
                AsnComponent? source = component.Component;
                AsnRuntimeFieldValue? field = source == null
                    ? null
                    : this.FindRuntimeField(value, source.Name);
                if (field != null
                    && field.Present
                    && field.Value != null
                    && component.TypeResolution != null)
                {
                    this.ValidateResolvedValue(
                        field.Value,
                        component.TypeResolution,
                        path + "." + source?.Name,
                        diagnostics);
                }

                componentIndex = componentIndex + 1;
            }
        }

        this.Resources.LeaveNesting();
    }

    internal RuntimeConstraintEvaluationResult EvaluateGraph(
        NormalizedConstraintGraph graph,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        RuntimeConstraintEvaluationResult root = graph.RootPartition == null
            || graph.RootPartition.Expression == null
            ? this.CreateSuccess()
            : this.EvaluateNode(
                graph.RootPartition.Expression,
                value,
                type,
                path);
        if (root.Success)
        {
            return root;
        }

        if (graph.ExtensionPartition == null)
        {
            return root;
        }

        if (graph.ExtensionPartition.Expression != null)
        {
            RuntimeConstraintEvaluationResult extension = this.EvaluateNode(
                graph.ExtensionPartition.Expression,
                value,
                type,
                path);
            if (extension.Success)
            {
                return extension;
            }
        }

        return graph.ExtensionPartition.Open ? this.CreateSuccess() : root;
    }

    internal RuntimeConstraintEvaluationResult EvaluateNode(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        if (node.Kind == AsnConstraintKind.Union)
        {
            return this.EvaluateUnion(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.Intersection)
        {
            return this.EvaluateIntersection(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.Except)
        {
            return this.EvaluateExclusion(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.AllExcept)
        {
            return this.EvaluateAllExclusion(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.Size)
        {
            return this.EvaluateSize(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.PermittedAlphabet)
        {
            return this.EvaluateAlphabet(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.Pattern)
        {
            return node.CompiledPattern != null
                && node.CompiledPattern.Matches(value.TextValue)
                ? this.CreateSuccess()
                : this.CreateFailure(
                    node,
                    path,
                    "The character string does not match the required PATTERN.");
        }

        if (node.Kind == AsnConstraintKind.PropertySettings)
        {
            return this.EvaluateTimeSettings(node, value, path);
        }

        if (node.Kind == AsnConstraintKind.WithComponent
            || node.Kind == AsnConstraintKind.WithComponents)
        {
            return this.EvaluateComponentConstraint(node, value, type, path);
        }

        if (node.Kind == AsnConstraintKind.Type)
        {
            return this.EvaluateOpenTypeConstraint(node, value, path);
        }

        if (node.Kind == AsnConstraintKind.Contents)
        {
            return this.EvaluateContentsConstraint(node, value, path);
        }

        if (node.Kind == AsnConstraintKind.SimpleTable
            || node.Kind == AsnConstraintKind.ComponentRelation)
        {
            return this.EvaluateTableConstraint(node, value, path);
        }

        if (node.Kind == AsnConstraintKind.UserDefined)
        {
            return this.CreateApplicationRequirement(node, path);
        }

        if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            if (node.IntegerSet != null && value.HasIntegerValue)
            {
                return node.IntegerSet.Contains(value.IntegerValue)
                    ? this.CreateSuccess()
                    : this.CreateFailure(
                        node,
                        path,
                        "The value is not in the contained subtype.");
            }

            return node.ResolvedContainedSubtype == null
                ? this.CreateFailure(
                    node,
                    path,
                    "The contained subtype is not available for runtime evaluation.")
                : this.EvaluateContainedType(
                    node,
                    value,
                    node.ResolvedContainedSubtype,
                    path);
        }

        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            return this.RuntimeValueMatchesNode(value, node, type)
                ? this.CreateSuccess()
                : this.CreateFailure(
                    node,
                    path,
                    "The value does not equal the required single value.");
        }

        if (node.Kind == AsnConstraintKind.ValueRange)
        {
            return this.RuntimeValueInRange(value, node, type)
                ? this.CreateSuccess()
                : this.CreateFailure(
                    node,
                    path,
                    "The value is outside the permitted range.");
        }

        return this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateUnion(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            RuntimeConstraintEvaluationResult child = this.EvaluateNode(
                node.Children[index],
                value,
                type,
                path);
            if (child.Success)
            {
                return child;
            }

            index = index + 1;
        }

        return this.CreateFailure(
            node,
            path,
            "The value does not satisfy any constraint union alternative.");
    }

    internal RuntimeConstraintEvaluationResult EvaluateIntersection(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            RuntimeConstraintEvaluationResult child = this.EvaluateNode(
                node.Children[index],
                value,
                type,
                path);
            if (!child.Success)
            {
                return child;
            }

            index = index + 1;
        }

        return this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateExclusion(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        if (node.Children.Length != 2)
        {
            return this.CreateFailure(node, path, "The exclusion constraint is incomplete.");
        }

        RuntimeConstraintEvaluationResult source = this.EvaluateNode(
            node.Children[0],
            value,
            type,
            path);
        if (!source.Success)
        {
            return source;
        }

        RuntimeConstraintEvaluationResult excluded = this.EvaluateNode(
            node.Children[1],
            value,
            type,
            path);
        return excluded.Success
            ? this.CreateFailure(node, path, "The value belongs to an excluded value set.")
            : this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateAllExclusion(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        if (node.Children.Length != 1)
        {
            return this.CreateFailure(node, path, "The ALL EXCEPT constraint is incomplete.");
        }

        RuntimeConstraintEvaluationResult excluded = this.EvaluateNode(
            node.Children[0],
            value,
            type,
            path);
        return excluded.Success
            ? this.CreateFailure(node, path, "The value belongs to the excluded value set.")
            : this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateSize(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        RuntimeSizeResult size = this.GetRuntimeSize(value);
        if (!size.Success)
        {
            return this.CreateFailure(
                node,
                path,
                "The runtime value has no measurable SIZE.");
        }

        AsnRuntimeValue sizeValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = size.Size
        };
        if (node.SizeSet != null)
        {
            return node.SizeSet.Contains(size.Size)
                ? this.CreateSuccess()
                : this.CreateFailure(
                    node,
                    path,
                    "The runtime value size is outside the permitted SIZE set.");
        }

        if (node.NestedConstraints.Length == 0)
        {
            return this.CreateFailure(node, path, "The SIZE constraint has no executable body.");
        }

        RuntimeConstraintEvaluationResult nested = this.EvaluateGraph(
            node.NestedConstraints[0],
            sizeValue,
            type,
            path);
        return nested.Success
            ? nested
            : this.CreateFailure(
                node,
                nested.ValuePath,
                "The runtime value size is outside the permitted SIZE set.");
    }

    internal RuntimeSizeResult GetRuntimeSize(AsnRuntimeValue value)
    {
        if (value.Kind == AsnRuntimeValueKind.BitString && value.BitStringValue != null)
        {
            return new RuntimeSizeResult
            {
                Size = new System.Numerics.BigInteger(value.BitStringValue.BitLength),
                Success = value.BitStringValue.BitLength >= 0
            };
        }

        if (value.Kind == AsnRuntimeValueKind.OctetString)
        {
            return new RuntimeSizeResult
            {
                Size = new System.Numerics.BigInteger(value.Octets.Length),
                Success = true
            };
        }

        if (value.Kind == AsnRuntimeValueKind.CharacterString
            || value.Kind == AsnRuntimeValueKind.Time)
        {
            return new RuntimeSizeResult
            {
                Size = new System.Numerics.BigInteger(
                    this.CountUnicodeScalars(value.TextValue)),
                Success = true
            };
        }

        if (value.Kind == AsnRuntimeValueKind.SequenceOf
            || value.Kind == AsnRuntimeValueKind.SetOf)
        {
            return new RuntimeSizeResult
            {
                Size = new System.Numerics.BigInteger(value.Items.Length),
                Success = true
            };
        }

        return new RuntimeSizeResult();
    }

    internal RuntimeConstraintEvaluationResult EvaluateAlphabet(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        string text = value.TextValue;
        int utf16Index = 0;
        int scalarIndex = 0;
        while (utf16Index < text.Length)
        {
            int scalar = this.ReadUnicodeScalar(text, utf16Index);
            int width = scalar > 65535 ? 2 : 1;
            string scalarPath = path + "[" + scalarIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "]";
            bool permitted = false;
            if (node.AlphabetSet != null)
            {
                permitted = node.AlphabetSet.Contains(
                    new System.Numerics.BigInteger(scalar));
            }
            else if (node.NestedConstraints.Length > 0)
            {
                AsnRuntimeValue scalarValue = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = char.ConvertFromUtf32(scalar)
                };
                permitted = this.EvaluateGraph(
                    node.NestedConstraints[0],
                    scalarValue,
                    type,
                    scalarPath).Success;
            }

            if (!permitted)
            {
                return this.CreateFailure(
                    node,
                    scalarPath,
                    "A character is outside the permitted alphabet.");
            }

            utf16Index = utf16Index + width;
            scalarIndex = scalarIndex + 1;
        }

        return this.CreateSuccess();
    }

    internal int CountUnicodeScalars(string text)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            int scalar = this.ReadUnicodeScalar(text, index);
            index = index + (scalar > 65535 ? 2 : 1);
            count = count + 1;
        }

        return count;
    }

    internal int ReadUnicodeScalar(string text, int index)
    {
        char first = text[index];
        if (char.IsHighSurrogate(first)
            && index + 1 < text.Length
            && char.IsLowSurrogate(text[index + 1]))
        {
            return char.ConvertToUtf32(first, text[index + 1]);
        }

        return first;
    }

    internal RuntimeConstraintEvaluationResult EvaluateTimeSettings(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        string path)
    {
        if (node.CompiledTimeSettings == null || value.TimeValue == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The runtime value has no TIME value for the required SETTINGS.");
        }

        TimePropertySettings settings = node.CompiledTimeSettings;
        AsnTimeValue time = value.TimeValue;
        string text = time.Text;
        bool matches = this.MatchesBasicTimeSetting(settings, time, text)
            && this.MatchesDateTimeSetting(settings, text)
            && this.MatchesYearTimeSetting(settings, text)
            && this.MatchesPrecisionTimeSetting(settings, time, text)
            && this.MatchesZoneTimeSetting(settings, time, text)
            && this.MatchesMidnightTimeSetting(settings, text)
            && this.MatchesIntervalTimeSetting(settings, text)
            && this.MatchesEndpointTimeSetting(settings, text)
            && this.MatchesRecurrenceTimeSetting(settings, text);
        return matches
            ? this.CreateSuccess()
            : this.CreateFailure(
                node,
                path,
                "The TIME value does not satisfy the required property SETTINGS.");
    }

    internal bool MatchesBasicTimeSetting(
        TimePropertySettings settings,
        AsnTimeValue value,
        string text)
    {
        if (settings.Basic == TimeBasicSettingKind.Unspecified)
        {
            return true;
        }

        if (settings.Basic == TimeBasicSettingKind.Date)
        {
            return value.Kind == AsnTimeKind.Date;
        }

        if (settings.Basic == TimeBasicSettingKind.Time)
        {
            return value.Kind == AsnTimeKind.TimeOfDay;
        }

        if (settings.Basic == TimeBasicSettingKind.DateTime)
        {
            return value.Kind == AsnTimeKind.DateTime;
        }

        if (settings.Basic == TimeBasicSettingKind.Interval)
        {
            return text.IndexOf("/", System.StringComparison.Ordinal) >= 0
                && !text.StartsWith("R", System.StringComparison.Ordinal);
        }

        return text.StartsWith("R", System.StringComparison.Ordinal);
    }

    internal bool MatchesDateTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Date == TimeDateSettingKind.Unspecified)
        {
            return true;
        }

        string date = text;
        int timeSeparator = date.IndexOf("T", System.StringComparison.Ordinal);
        if (timeSeparator >= 0)
        {
            date = date.Substring(0, timeSeparator);
        }

        int slash = date.LastIndexOf("/", System.StringComparison.Ordinal);
        if (slash >= 0)
        {
            date = date.Substring(slash + 1);
        }

        int digitCount = this.CountDigits(date);
        if (settings.Date == TimeDateSettingKind.Century)
        {
            return digitCount == 2;
        }

        if (settings.Date == TimeDateSettingKind.Year)
        {
            return digitCount >= 4
                && date.IndexOf("-", 1, System.StringComparison.Ordinal) < 0;
        }

        if (settings.Date == TimeDateSettingKind.YearMonth)
        {
            return digitCount == 6;
        }

        if (settings.Date == TimeDateSettingKind.YearMonthDay)
        {
            return digitCount == 8;
        }

        if (settings.Date == TimeDateSettingKind.YearDay)
        {
            return digitCount == 7;
        }

        if (settings.Date == TimeDateSettingKind.YearWeek)
        {
            return date.IndexOf("W", System.StringComparison.Ordinal) >= 0
                && digitCount == 6;
        }

        return date.IndexOf("W", System.StringComparison.Ordinal) >= 0
            && digitCount == 7;
    }

    internal bool MatchesYearTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Year == TimeYearSettingKind.Unspecified)
        {
            return true;
        }

        int yearDigits = this.CountLeadingYearDigits(text);
        if (settings.Year == TimeYearSettingKind.Basic)
        {
            return yearDigits == 4
                && !text.StartsWith("-", System.StringComparison.Ordinal);
        }

        if (settings.Year == TimeYearSettingKind.Proleptic)
        {
            return yearDigits >= 4;
        }

        if (settings.Year == TimeYearSettingKind.Negative)
        {
            return text.StartsWith("-", System.StringComparison.Ordinal);
        }

        return yearDigits == settings.LargeYearDigits;
    }

    internal bool MatchesPrecisionTimeSetting(
        TimePropertySettings settings,
        AsnTimeValue value,
        string text)
    {
        if (settings.Time == TimePrecisionSettingKind.Unspecified
            && settings.FractionalDigits == 0)
        {
            return true;
        }

        int separator = text.LastIndexOf("T", System.StringComparison.Ordinal);
        string clock = separator < 0 ? text : text.Substring(separator + 1);
        int zoneIndex = clock.IndexOf("Z", System.StringComparison.Ordinal);
        if (zoneIndex >= 0)
        {
            clock = clock.Substring(0, zoneIndex);
        }

        int fieldCount = 1;
        int index = 0;
        while (index < clock.Length)
        {
            if (clock[index] == ':')
            {
                fieldCount = fieldCount + 1;
            }

            index = index + 1;
        }

        if (clock.IndexOf(":", System.StringComparison.Ordinal) < 0)
        {
            int digits = this.CountDigitsBeforeFraction(clock);
            fieldCount = digits >= 6 ? 3 : digits >= 4 ? 2 : 1;
        }

        bool precisionMatches = settings.Time == TimePrecisionSettingKind.Unspecified
            || (settings.Time == TimePrecisionSettingKind.Hour && fieldCount == 1)
            || (settings.Time == TimePrecisionSettingKind.HourMinute && fieldCount == 2)
            || (settings.Time == TimePrecisionSettingKind.HourMinuteSecond && fieldCount == 3);
        bool fractionMatches = settings.FractionalDigits == 0
            || (value.HasFractionalDigits
                && value.FractionalDigits == settings.FractionalDigits);
        return precisionMatches && fractionMatches;
    }

    internal bool MatchesZoneTimeSetting(
        TimePropertySettings settings,
        AsnTimeValue value,
        string text)
    {
        if (settings.Zone == TimeZoneSettingKind.Unspecified)
        {
            return true;
        }

        bool utc = text.EndsWith("Z", System.StringComparison.Ordinal);
        if (settings.Zone == TimeZoneSettingKind.Utc)
        {
            return utc;
        }

        if (settings.Zone == TimeZoneSettingKind.LocalDifference)
        {
            return value.HasUtcOffsetMinutes && !utc;
        }

        return !value.HasUtcOffsetMinutes && !utc;
    }

    internal bool MatchesMidnightTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Midnight == TimeMidnightSettingKind.Unspecified)
        {
            return true;
        }

        bool end = text.IndexOf("24:00", System.StringComparison.Ordinal) >= 0
            || text.IndexOf("2400", System.StringComparison.Ordinal) >= 0;
        return settings.Midnight == TimeMidnightSettingKind.End ? end : !end;
    }

    internal bool MatchesIntervalTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Interval == TimeIntervalSettingKind.Unspecified)
        {
            return true;
        }

        int slash = text.IndexOf("/", System.StringComparison.Ordinal);
        if (settings.Interval == TimeIntervalSettingKind.Duration)
        {
            return slash < 0 && text.StartsWith("P", System.StringComparison.Ordinal);
        }

        if (slash < 0)
        {
            return false;
        }

        string left = text.Substring(0, slash);
        string right = text.Substring(slash + 1);
        bool leftDuration = left.StartsWith("P", System.StringComparison.Ordinal);
        bool rightDuration = right.StartsWith("P", System.StringComparison.Ordinal);
        if (settings.Interval == TimeIntervalSettingKind.StartDuration)
        {
            return !leftDuration && rightDuration;
        }

        if (settings.Interval == TimeIntervalSettingKind.DurationEnd)
        {
            return leftDuration && !rightDuration;
        }

        return !leftDuration && !rightDuration;
    }

    internal bool MatchesEndpointTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Endpoint == TimeEndpointSettingKind.Unspecified)
        {
            return true;
        }

        int slash = text.IndexOf("/", System.StringComparison.Ordinal);
        string endpoint = slash < 0 ? text : text.Substring(0, slash);
        bool hasTime = endpoint.IndexOf("T", System.StringComparison.Ordinal) >= 0;
        bool timeOnly = endpoint.IndexOf(":", System.StringComparison.Ordinal) >= 0
            && !hasTime;
        if (settings.Endpoint == TimeEndpointSettingKind.DateTime)
        {
            return hasTime;
        }

        if (settings.Endpoint == TimeEndpointSettingKind.Time)
        {
            return timeOnly;
        }

        return !hasTime && !timeOnly;
    }

    internal bool MatchesRecurrenceTimeSetting(TimePropertySettings settings, string text)
    {
        if (settings.Recurrence == TimeRecurrenceSettingKind.Unspecified)
        {
            return true;
        }

        if (!text.StartsWith("R", System.StringComparison.Ordinal))
        {
            return false;
        }

        int slash = text.IndexOf("/", System.StringComparison.Ordinal);
        string recurrence = slash < 0 ? text.Substring(1) : text.Substring(1, slash - 1);
        if (settings.Recurrence == TimeRecurrenceSettingKind.Unlimited)
        {
            return recurrence.Length == 0;
        }

        return this.CountDigits(recurrence) == settings.RecurrenceDigits
            && this.CountDigits(recurrence) == recurrence.Length;
    }

    internal int CountDigits(string text)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsDigit(text[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountLeadingYearDigits(string text)
    {
        int index = text.StartsWith("-", System.StringComparison.Ordinal)
            || text.StartsWith("+", System.StringComparison.Ordinal)
            ? 1
            : 0;
        int start = index;
        while (index < text.Length && char.IsDigit(text[index]))
        {
            index = index + 1;
        }

        return index - start;
    }

    internal int CountDigitsBeforeFraction(string text)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length && text[index] != '.' && text[index] != ',')
        {
            if (char.IsDigit(text[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal RuntimeConstraintEvaluationResult EvaluateComponentConstraint(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        if (node.CompiledComponentConstraints == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The component constraint has no compiled runtime metadata.");
        }

        NormalizedComponentConstraintSet set = node.CompiledComponentConstraints;
        if (type.EffectiveKind == AsnTypeKind.Real)
        {
            return this.EvaluateRealComponentConstraint(
                node,
                set,
                value,
                path);
        }
        if (type.EffectiveKind == AsnTypeKind.Duration)
        {
            return this.EvaluateDurationComponentConstraint(
                node,
                set,
                value,
                path);
        }
        if (set.CollectionElement)
        {
            int itemIndex = 0;
            while (itemIndex < value.Items.Length)
            {
                NormalizedComponentConstraint component = set.Components[0];
                if (component.ValueConstraint != null)
                {
                    string itemPath = path + "[" + itemIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "]";
                    ResolvedTypeSyntax itemType = component.ComponentType == null
                        ? type
                        : component.ComponentType;
                    RuntimeConstraintEvaluationResult itemResult = this.EvaluateGraph(
                        component.ValueConstraint,
                        value.Items[itemIndex],
                        itemType,
                        itemPath);
                    if (!itemResult.Success)
                    {
                        return itemResult;
                    }
                }

                itemIndex = itemIndex + 1;
            }

            return this.CreateSuccess();
        }

        int componentIndex = 0;
        while (componentIndex < set.Components.Length)
        {
            NormalizedComponentConstraint component = set.Components[componentIndex];
            AsnRuntimeFieldValue? field = this.FindRuntimeField(value, component.Name);
            bool present = field != null && field.Present;
            string componentPath = path + "." + component.Name;
            if (component.Presence == ComponentPresenceConstraintKind.Present && !present)
            {
                return this.CreateFailure(
                    node,
                    componentPath,
                    "A component required by WITH COMPONENTS is absent.");
            }

            if (component.Presence == ComponentPresenceConstraintKind.Absent && present)
            {
                return this.CreateFailure(
                    node,
                    componentPath,
                    "A component required to be ABSENT is present.");
            }

            if (present
                && field?.Value != null
                && component.ValueConstraint != null)
            {
                ResolvedTypeSyntax componentType = component.ComponentType == null
                    ? type
                    : component.ComponentType;
                RuntimeConstraintEvaluationResult componentResult = this.EvaluateGraph(
                    component.ValueConstraint,
                    field.Value,
                    componentType,
                    componentPath);
                if (!componentResult.Success)
                {
                    return componentResult;
                }
            }

            componentIndex = componentIndex + 1;
        }

        return this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateRealComponentConstraint(
        NormalizedConstraintNode node,
        NormalizedComponentConstraintSet set,
        AsnRuntimeValue value,
        string path)
    {
        AsnRealValue? real = value.RealValue;
        if (real == null
            || (real.Kind != AsnRealKind.FiniteDecimal
                && real.Kind != AsnRealKind.FiniteBinary))
        {
            return this.CreateFailure(
                node,
                path,
                "REAL WITH COMPONENTS requires a finite REAL value.");
        }

        int componentIndex = 0;
        while (componentIndex < set.Components.Length)
        {
            NormalizedComponentConstraint component =
                set.Components[componentIndex];
            System.Numerics.BigInteger componentValue =
                System.Numerics.BigInteger.Zero;
            if (component.Name == "mantissa")
            {
                componentValue = real.Kind == AsnRealKind.FiniteDecimal
                    ? real.DecimalCoefficient
                    : real.Mantissa;
            }
            else if (component.Name == "base")
            {
                componentValue = real.Kind == AsnRealKind.FiniteDecimal
                    ? new System.Numerics.BigInteger(10)
                    : new System.Numerics.BigInteger(real.Radix);
            }
            else if (component.Name == "exponent")
            {
                componentValue = real.Kind == AsnRealKind.FiniteDecimal
                    ? real.DecimalExponent
                    : real.Exponent;
            }
            if (component.ValueConstraint != null)
            {
                AsnRuntimeValue integer = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Integer,
                    HasIntegerValue = true,
                    IntegerValue = componentValue
                };
                ResolvedTypeSyntax? componentType =
                    component.ComponentType == null
                        ? set.GoverningType
                        : component.ComponentType;
                if (componentType == null)
                {
                    return this.CreateFailure(
                        node,
                        path + "." + component.Name,
                        "The REAL component constraint has no component type metadata.");
                }
                RuntimeConstraintEvaluationResult componentResult =
                    this.EvaluateGraph(
                        component.ValueConstraint,
                        integer,
                        componentType,
                        path + "." + component.Name);
                if (!componentResult.Success)
                {
                    return componentResult;
                }
            }
            componentIndex = componentIndex + 1;
        }
        return this.CreateSuccess();
    }

    internal RuntimeConstraintEvaluationResult EvaluateDurationComponentConstraint(
        NormalizedConstraintNode node,
        NormalizedComponentConstraintSet set,
        AsnRuntimeValue value,
        string path)
    {
        AsnTimeValue? duration = value.TimeValue;
        if (duration == null || duration.Kind != AsnTimeKind.Duration)
        {
            return this.CreateFailure(
                node,
                path,
                "DURATION WITH COMPONENTS requires a duration value.");
        }

        int componentIndex = 0;
        while (componentIndex < set.Components.Length)
        {
            NormalizedComponentConstraint component =
                set.Components[componentIndex];
            bool present = this.DurationComponentIsPresent(
                duration.Text,
                component.Name);
            string componentPath = path + "." + component.Name;
            if (component.Presence == ComponentPresenceConstraintKind.Present
                && !present)
            {
                return this.CreateFailure(
                    node,
                    componentPath,
                    "A DURATION component required by WITH COMPONENTS is absent.");
            }
            if (component.Presence == ComponentPresenceConstraintKind.Absent
                && present)
            {
                return this.CreateFailure(
                    node,
                    componentPath,
                    "A DURATION component required to be ABSENT is present.");
            }
            componentIndex = componentIndex + 1;
        }
        return this.CreateSuccess();
    }

    internal bool DurationComponentIsPresent(string text, string component)
    {
        int time = text.IndexOf('T');
        int end = text.Length;
        if (component == "fractional-part")
        {
            int seconds = text.LastIndexOf('S');
            if (seconds < 0)
            {
                return false;
            }
            int index = seconds - 1;
            while (index >= 0 && text[index] != 'T'
                && text[index] != 'P')
            {
                if (text[index] == '.' || text[index] == ',')
                {
                    return true;
                }
                if (text[index] < '0' || text[index] > '9')
                {
                    break;
                }
                index = index - 1;
            }
            return false;
        }
        char designator = component == "years" ? 'Y'
            : component == "months" || component == "minutes" ? 'M'
            : component == "weeks" ? 'W'
            : component == "days" ? 'D'
            : component == "hours" ? 'H'
            : component == "seconds" ? 'S'
            : '\0';
        if (designator == '\0')
        {
            return false;
        }
        int found = text.IndexOf(designator);
        if (designator != 'M')
        {
            return found >= 0;
        }
        if (component == "months")
        {
            end = time < 0 ? text.Length : time;
            found = text.IndexOf('M');
            return found >= 0 && found < end;
        }
        found = time < 0 ? -1 : text.IndexOf('M', time + 1);
        return found >= 0;
    }

    internal RuntimeConstraintEvaluationResult EvaluateOpenTypeConstraint(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        string path)
    {
        if (node.CompiledTypeConstraint == null
            || node.CompiledTypeConstraint.ConstrainedType == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The type constraint has no resolved constrained type.");
        }

        AsnOpenTypeValue? open = value.OpenTypeValue;
        if (open == null
            || !open.Resolved
            || open.Unresolved
            || open.Value == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The open value is unresolved for the required type constraint.");
        }

        ResolvedTypeSyntax constrained = node.CompiledTypeConstraint.ConstrainedType;
        if (!this.OpenTypeIdentityMatches(open, constrained))
        {
            return this.CreateFailure(
                node,
                path,
                "The resolved open value has a different type identity.");
        }

        return this.EvaluateContainedType(node, open.Value, constrained, path + ".value");
    }

    internal RuntimeConstraintEvaluationResult EvaluateContentsConstraint(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        string path)
    {
        if (node.CompiledContentsConstraint == null
            || node.CompiledContentsConstraint.ContainedType == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The contents constraint has no resolved contained type.");
        }

        AsnOpenTypeValue? contents = value.OpenTypeValue;
        if (contents == null
            || !contents.Resolved
            || contents.Unresolved
            || contents.Value == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The contained value is unresolved.");
        }

        ResolvedTypeSyntax contained = node.CompiledContentsConstraint.ContainedType;
        if (!this.OpenTypeIdentityMatches(contents, contained))
        {
            return this.CreateFailure(
                node,
                path,
                "The decoded contents have a different contained type identity.");
        }

        return this.EvaluateContainedType(node, contents.Value, contained, path + ".contents");
    }

    internal RuntimeConstraintEvaluationResult EvaluateTableConstraint(
        NormalizedConstraintNode node,
        AsnRuntimeValue value,
        string path)
    {
        if (node.CompiledTableConstraint == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The table constraint has no compiled dispatch relationship.");
        }

        AsnOpenTypeValue? open = value.OpenTypeValue;
        if (open == null
            || !open.Resolved
            || open.Unresolved
            || open.DispatchKey == null)
        {
            return this.CreateFailure(
                node,
                path,
                "The table-constrained open value has no resolved dispatch key.");
        }

        AsnOpenTypeDispatchEntry[] entries = node.CompiledTableConstraint.DispatchEntries;
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            AsnOpenTypeDispatchEntry entry = entries[entryIndex];
            if (entry.Key != null
                && this.RuntimeValueMatchesAsnValue(open.DispatchKey, entry.Key))
            {
                string typeIdentity = open.GetResolvedTypeIdentity();
                string typeName = open.TypeName == null ? "" : open.TypeName;
                if (entry.Type == null
                    || typeIdentity == entry.Type.Name
                    || typeIdentity.EndsWith(
                        "." + entry.Type.Name,
                        System.StringComparison.Ordinal)
                    || typeName == entry.Type.Name
                    || typeName.EndsWith(
                        "." + entry.Type.Name,
                        System.StringComparison.Ordinal))
                {
                    return this.CreateSuccess();
                }

                return this.CreateFailure(
                    node,
                    path,
                    "The table dispatch key resolved a different open type.");
            }

            entryIndex = entryIndex + 1;
        }

        return this.CreateFailure(
            node,
            path,
            "The open value dispatch key is not present in the governing table.");
    }

    internal RuntimeConstraintEvaluationResult EvaluateContainedType(
        NormalizedConstraintNode source,
        AsnRuntimeValue value,
        ResolvedTypeSyntax contained,
        string path)
    {
        RuntimeConstraintEvaluationResult result = this.EvaluateResolvedTree(
            value,
            contained,
            path);
        return result.Success
            ? result
            : result.ViolatedNode == null
                ? this.CreateFailure(
                    source,
                    result.ValuePath,
                    "The value does not satisfy the contained type.")
                : result;
    }

    internal RuntimeConstraintEvaluationResult EvaluateResolvedTree(
        AsnRuntimeValue value,
        ResolvedTypeSyntax type,
        string path)
    {
        if (type.EffectiveConstraint != null)
        {
            RuntimeConstraintEvaluationResult ownResult = this.EvaluateGraph(
                type.EffectiveConstraint,
                value,
                type,
                path);
            if (!ownResult.Success)
            {
                return ownResult;
            }
        }

        if ((type.EffectiveKind == AsnTypeKind.SequenceOf
                || type.EffectiveKind == AsnTypeKind.SetOf)
            && type.InnerType != null)
        {
            int itemIndex = 0;
            while (itemIndex < value.Items.Length)
            {
                RuntimeConstraintEvaluationResult itemResult = this.EvaluateResolvedTree(
                    value.Items[itemIndex],
                    type.InnerType,
                    path + "[" + itemIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "]");
                if (!itemResult.Success)
                {
                    return itemResult;
                }

                itemIndex = itemIndex + 1;
            }
        }

        if (type.EffectiveKind == AsnTypeKind.Sequence
            || type.EffectiveKind == AsnTypeKind.Set
            || type.EffectiveKind == AsnTypeKind.Choice)
        {
            int componentIndex = 0;
            while (componentIndex < type.Components.Length)
            {
                ResolvedComponentSyntax component = type.Components[componentIndex];
                AsnComponent? sourceComponent = component.Component;
                AsnRuntimeFieldValue? field = sourceComponent == null
                    ? null
                    : this.FindRuntimeField(value, sourceComponent.Name);
                if (field != null
                    && field.Present
                    && field.Value != null
                    && component.TypeResolution != null)
                {
                    RuntimeConstraintEvaluationResult componentResult =
                        this.EvaluateResolvedTree(
                            field.Value,
                            component.TypeResolution,
                            path + "." + sourceComponent?.Name);
                    if (!componentResult.Success)
                    {
                        return componentResult;
                    }
                }

                componentIndex = componentIndex + 1;
            }
        }

        return this.CreateSuccess();
    }

    internal bool TypeIdentityMatches(string runtimeName, ResolvedTypeSyntax type)
    {
        string sourceQualified = type.SourceDeclaration == null
            ? ""
            : type.SourceDeclaration.QualifiedName;
        string effectiveQualified = type.EffectiveDeclaration == null
            ? ""
            : type.EffectiveDeclaration.QualifiedName;
        string sourceName = type.SourceDeclaration == null
            ? type.SourceType == null ? "" : type.SourceType.Name
            : type.SourceDeclaration.Name;
        string effectiveName = type.EffectiveDeclaration == null
            ? type.EffectiveType == null ? "" : type.EffectiveType.Name
            : type.EffectiveDeclaration.Name;
        return runtimeName == sourceQualified
            || runtimeName == effectiveQualified
            || runtimeName == sourceName
            || runtimeName == effectiveName
            || (sourceName.Length > 0
                && runtimeName.EndsWith(
                    "." + sourceName,
                    System.StringComparison.Ordinal))
            || (effectiveName.Length > 0
                && runtimeName.EndsWith(
                    "." + effectiveName,
                    System.StringComparison.Ordinal));
    }

    internal bool OpenTypeIdentityMatches(
        AsnOpenTypeValue value,
        ResolvedTypeSyntax type)
    {
        if (value.ResolvedTypeId != null
            && value.ResolvedTypeId.Length > 0
            && this.TypeIdentityMatches(value.ResolvedTypeId, type))
        {
            return true;
        }

        return value.TypeName != null
            && value.TypeName.Length > 0
            && this.TypeIdentityMatches(value.TypeName, type);
    }

    internal AsnRuntimeFieldValue? FindRuntimeField(AsnRuntimeValue value, string name)
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

    internal bool RuntimeValueMatchesNode(
        AsnRuntimeValue value,
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        ResolvedValueSyntax? resolved = node.SourceSyntax == null
            || node.SourceSyntax.ValueSyntax == null
            ? null
            : node.SourceSyntax.ValueSyntax.Resolution;
        if (resolved != null)
        {
            return this.RuntimeValueMatchesResolvedValue(value, resolved);
        }

        return node.Value != null
            && this.RuntimeValueMatchesAsnValue(value, node.Value);
    }

    internal bool RuntimeValueMatchesResolvedValue(
        AsnRuntimeValue runtime,
        ResolvedValueSyntax expected)
    {
        if (expected.EffectiveValue == null)
        {
            return false;
        }

        if (!this.RuntimeValueMatchesAsnValue(runtime, expected.EffectiveValue))
        {
            return false;
        }

        if (expected.BitStringValue != null)
        {
            return runtime.BitStringValue != null
                && this.BitStringsEqual(runtime.BitStringValue, expected.BitStringValue);
        }

        if (expected.ObjectIdentifierValue != null)
        {
            return runtime.ObjectIdentifierValue != null
                && this.ObjectIdentifiersEqual(
                    runtime.ObjectIdentifierValue,
                    expected.ObjectIdentifierValue);
        }

        if (expected.EffectiveValue.Kind == AsnValueKind.OctetString)
        {
            return this.ByteArraysEqual(runtime.Octets, expected.Octets);
        }

        if (expected.Items.Length > 0 || runtime.Items.Length > 0)
        {
            if (runtime.Items.Length != expected.Items.Length)
            {
                return false;
            }

            int itemIndex = 0;
            while (itemIndex < expected.Items.Length)
            {
                if (!this.RuntimeValueMatchesResolvedValue(
                    runtime.Items[itemIndex],
                    expected.Items[itemIndex]))
                {
                    return false;
                }

                itemIndex = itemIndex + 1;
            }
        }

        int componentIndex = 0;
        while (componentIndex < expected.ComponentValues.Length)
        {
            ResolvedComponentValueSyntax expectedComponent =
                expected.ComponentValues[componentIndex];
            AsnRuntimeFieldValue? actualComponent = this.FindRuntimeField(
                runtime,
                expectedComponent.Name);
            bool actualPresent = actualComponent != null && actualComponent.Present;
            if (actualPresent != expectedComponent.Present)
            {
                return false;
            }

            if (expectedComponent.Present
                && expectedComponent.Value != null
                && (actualComponent?.Value == null
                    || !this.RuntimeValueMatchesResolvedValue(
                        actualComponent.Value,
                        expectedComponent.Value)))
            {
                return false;
            }

            componentIndex = componentIndex + 1;
        }

        return true;
    }

    internal bool RuntimeValueMatchesAsnValue(AsnRuntimeValue runtime, AsnValue expected)
    {
        if (expected.Kind == AsnValueKind.Integer
            || expected.Kind == AsnValueKind.Enumerated)
        {
            return runtime.HasIntegerValue
                && expected.HasIntegerValue
                && runtime.IntegerValue == expected.IntegerValue;
        }

        if (expected.Kind == AsnValueKind.Boolean)
        {
            bool expectedBoolean = string.Equals(
                expected.TextValue,
                "TRUE",
                System.StringComparison.OrdinalIgnoreCase);
            return runtime.HasBooleanValue && runtime.BooleanValue == expectedBoolean;
        }

        if (expected.Kind == AsnValueKind.Null)
        {
            return runtime.Kind == AsnRuntimeValueKind.Null;
        }

        if (expected.Kind == AsnValueKind.Real)
        {
            return runtime.RealValue != null
                && expected.RealValue != null
                && this.CompareRealValues(runtime.RealValue, expected.RealValue) == 0;
        }

        if (expected.Kind == AsnValueKind.BitString
            || expected.Kind == AsnValueKind.OctetString
            || expected.Kind == AsnValueKind.ObjectIdentifier
            || expected.Kind == AsnValueKind.RelativeObjectIdentifier
            || expected.Kind == AsnValueKind.Sequence
            || expected.Kind == AsnValueKind.Set
            || expected.Kind == AsnValueKind.SequenceOf
            || expected.Kind == AsnValueKind.SetOf
            || expected.Kind == AsnValueKind.Choice
            || expected.Kind == AsnValueKind.OpenType
            || expected.Kind == AsnValueKind.External
            || expected.Kind == AsnValueKind.EmbeddedPdv
            || expected.Kind == AsnValueKind.InstanceOf)
        {
            return true;
        }

        if (expected.Kind == AsnValueKind.Time)
        {
            return runtime.TimeValue != null
                && expected.TimeValue != null
                && runtime.TimeValue.Kind == expected.TimeValue.Kind
                && runtime.TimeValue.Text == expected.TimeValue.Text;
        }

        return runtime.TextValue == expected.TextValue;
    }

    internal bool BitStringsEqual(AsnBitStringValue left, AsnBitStringValue right)
    {
        return left.BitLength == right.BitLength
            && this.ByteArraysEqual(left.Bytes, right.Bytes);
    }

    internal bool ObjectIdentifiersEqual(
        AsnObjectIdentifierValue left,
        AsnObjectIdentifierValue right)
    {
        if (left.Relative != right.Relative || left.Arcs.Length != right.Arcs.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Arcs.Length)
        {
            if (left.Arcs[index] != right.Arcs[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ByteArraysEqual(byte[] left, byte[] right)
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

    internal bool RuntimeValueInRange(
        AsnRuntimeValue value,
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        if (node.IntegerSet != null && value.HasIntegerValue)
        {
            return node.IntegerSet.Contains(value.IntegerValue);
        }

        AsnValue? lower = node.SourceSyntax?.LowerValueSyntax?.Resolution?.EffectiveValue
            ?? node.LowerValue;
        AsnValue? upper = node.SourceSyntax?.UpperValueSyntax?.Resolution?.EffectiveValue
            ?? node.UpperValue;
        RuntimeComparisonResult lowerResult = new RuntimeComparisonResult
        {
            Success = node.LowerEndpointUnbounded
        };
        RuntimeComparisonResult upperResult = new RuntimeComparisonResult
        {
            Success = node.UpperEndpointUnbounded
        };
        if (!node.LowerEndpointUnbounded && lower != null)
        {
            lowerResult = this.CompareRuntimeToBound(value, lower, true);
        }

        if (!node.UpperEndpointUnbounded && upper != null)
        {
            upperResult = this.CompareRuntimeToBound(value, upper, false);
        }

        if (!lowerResult.Success || !upperResult.Success)
        {
            return false;
        }

        bool aboveLower = node.LowerEndpointUnbounded
            || lowerResult.Comparison > 0
            || (lowerResult.Comparison == 0 && node.LowerEndpointIncluded);
        bool belowUpper = node.UpperEndpointUnbounded
            || upperResult.Comparison < 0
            || (upperResult.Comparison == 0 && node.UpperEndpointIncluded);
        return aboveLower && belowUpper;
    }

    internal RuntimeComparisonResult CompareRuntimeToBound(
        AsnRuntimeValue runtime,
        AsnValue bound,
        bool lower)
    {
        if (runtime.HasIntegerValue && bound.HasIntegerValue)
        {
            return new RuntimeComparisonResult
            {
                Comparison = runtime.IntegerValue.CompareTo(bound.IntegerValue),
                Success = true
            };
        }

        if (runtime.RealValue != null && bound.RealValue != null)
        {
            int realComparison = this.CompareRealValues(runtime.RealValue, bound.RealValue);
            return new RuntimeComparisonResult
            {
                Comparison = realComparison,
                Success = realComparison != int.MinValue
            };
        }

        if (runtime.Kind == AsnRuntimeValueKind.CharacterString
            || runtime.Kind == AsnRuntimeValueKind.ObjectIdentifierIri
            || runtime.Kind == AsnRuntimeValueKind.RelativeObjectIdentifierIri
            || runtime.Kind == AsnRuntimeValueKind.Time)
        {
            return new RuntimeComparisonResult
            {
                Comparison = string.CompareOrdinal(runtime.TextValue, bound.TextValue),
                Success = true
            };
        }

        return new RuntimeComparisonResult
        {
            Comparison = lower ? -1 : 1
        };
    }

    internal int CompareRealValues(AsnRealValue left, AsnRealValue right)
    {
        RuntimeRealNumber leftNumber = this.CreateRuntimeRealNumber(left);
        RuntimeRealNumber rightNumber = this.CreateRuntimeRealNumber(right);
        if (!leftNumber.Success || !rightNumber.Success
            || leftNumber.SpecialRank == 3 || rightNumber.SpecialRank == 3)
        {
            return int.MinValue;
        }

        if (leftNumber.SpecialRank != rightNumber.SpecialRank)
        {
            return leftNumber.SpecialRank.CompareTo(rightNumber.SpecialRank);
        }

        if (leftNumber.SpecialRank != 0)
        {
            return 0;
        }

        return (leftNumber.Numerator * rightNumber.Denominator).CompareTo(
            rightNumber.Numerator * leftNumber.Denominator);
    }

    internal RuntimeRealNumber CreateRuntimeRealNumber(AsnRealValue value)
    {
        AsnRealValue effective = value;
        if (!value.Normalized)
        {
            RealNormalizationResult normalized = new RealValueNormalizer()
                .NormalizeRealValue(value);
            if (!normalized.Success || normalized.Value == null)
            {
                return new RuntimeRealNumber();
            }

            effective = normalized.Value;
        }

        if (effective.Kind == AsnRealKind.NegativeInfinity)
        {
            return new RuntimeRealNumber { SpecialRank = -1, Success = true };
        }

        if (effective.Kind == AsnRealKind.PositiveInfinity)
        {
            return new RuntimeRealNumber { SpecialRank = 1, Success = true };
        }

        if (effective.Kind == AsnRealKind.NotANumber)
        {
            return new RuntimeRealNumber { SpecialRank = 3, Success = true };
        }

        if (effective.Kind == AsnRealKind.Zero || effective.Kind == AsnRealKind.MinusZero)
        {
            return new RuntimeRealNumber { Success = true };
        }

        if (effective.Kind == AsnRealKind.FiniteDecimal)
        {
            return this.CreateDecimalRuntimeRealNumber(effective);
        }

        if (effective.Kind == AsnRealKind.FiniteBinary)
        {
            return this.CreateBinaryRuntimeRealNumber(effective);
        }

        return new RuntimeRealNumber();
    }

    internal RuntimeRealNumber CreateDecimalRuntimeRealNumber(AsnRealValue value)
    {
        if (value.DecimalExponent < new System.Numerics.BigInteger(int.MinValue)
            || value.DecimalExponent > new System.Numerics.BigInteger(int.MaxValue))
        {
            return new RuntimeRealNumber();
        }

        int exponent = (int)value.DecimalExponent;
        System.Numerics.BigInteger power = System.Numerics.BigInteger.Pow(
            new System.Numerics.BigInteger(10),
            exponent < 0 ? -exponent : exponent);
        return new RuntimeRealNumber
        {
            Numerator = exponent < 0
                ? value.DecimalCoefficient
                : value.DecimalCoefficient * power,
            Denominator = exponent < 0 ? power : System.Numerics.BigInteger.One,
            Success = true
        };
    }

    internal RuntimeRealNumber CreateBinaryRuntimeRealNumber(AsnRealValue value)
    {
        if (value.Exponent < new System.Numerics.BigInteger(int.MinValue)
            || value.Exponent > new System.Numerics.BigInteger(int.MaxValue))
        {
            return new RuntimeRealNumber();
        }

        int exponent = (int)value.Exponent;
        System.Numerics.BigInteger power = System.Numerics.BigInteger.Pow(
            new System.Numerics.BigInteger(value.Radix),
            exponent < 0 ? -exponent : exponent);
        return new RuntimeRealNumber
        {
            Numerator = exponent < 0 ? value.Mantissa : value.Mantissa * power,
            Denominator = exponent < 0 ? power : System.Numerics.BigInteger.One,
            Success = true
        };
    }

    internal void ValidatePublicConstraints(
        AsnRuntimeValue value,
        AsnTypeDescriptor type,
        string path,
        System.Collections.Generic.List<AsnDiagnostic> diagnostics)
    {
        if (!this.CanContinueValidation())
        {
            return;
        }

        if (!this.Resources.TryEnterNesting())
        {
            this.TryAddDiagnostic(
                diagnostics,
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Resource,
                    Code = "ASNR0002",
                    Message = "Runtime constraint validation exceeds the configured nesting-depth limit at "
                        + path + ".",
                    ValuePath = path
                });
            return;
        }

        int index = 0;
        while (index < type.Constraints.Length
            && this.CanContinueValidation())
        {
            AsnConstraintDescriptor descriptor = type.Constraints[index];
            if (descriptor.Constraint != null
                && descriptor.Constraint.RequiresApplicationValidation)
            {
                this.TryAddDiagnostic(
                    diagnostics,
                    new AsnDiagnostic
                    {
                        Severity = AsnDiagnosticSeverity.Error,
                        Category = AsnDiagnosticCategory.Constraint,
                        Code = "ASNC0032",
                        Message = "The constraint requires application-supplied validation at "
                            + path + ".",
                        ConstraintId = descriptor.StableId,
                        ConstraintKind = descriptor.Kind,
                        ValuePath = path,
                        Span = descriptor.Constraint.Span
                    });
            }

            if (this.CanContinueValidation())
            {
                this.ValidatePublicConstraintDescriptor(
                    value,
                    descriptor,
                    path,
                    diagnostics);
            }

            index = index + 1;
        }

        if ((type.Kind == AsnTypeKind.SequenceOf
                || type.Kind == AsnTypeKind.SetOf)
            && type.ElementType != null)
        {
            int itemIndex = 0;
            while (itemIndex < value.Items.Length
                && this.CanContinueValidation())
            {
                this.ValidatePublicConstraints(
                    value.Items[itemIndex],
                    type.ElementType,
                    path + "[" + itemIndex.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "]",
                    diagnostics);
                itemIndex = itemIndex + 1;
            }
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length
            && this.CanContinueValidation())
        {
            AsnFieldDescriptor field = type.Fields[fieldIndex];
            AsnRuntimeFieldValue? runtime = this.FindRuntimeField(
                value,
                field.Name);
            if (runtime != null && runtime.Present && runtime.Value != null
                && field.Type != null)
            {
                this.ValidatePublicConstraints(
                    runtime.Value,
                    field.Type,
                    path + "." + field.Name,
                    diagnostics);
            }
            fieldIndex = fieldIndex + 1;
        }

        this.Resources.LeaveNesting();
    }

    internal void ValidatePublicConstraintDescriptor(
        AsnRuntimeValue value,
        AsnConstraintDescriptor descriptor,
        string path,
        System.Collections.Generic.List<AsnDiagnostic> diagnostics)
    {
        if (!this.CanContinueValidation())
        {
            return;
        }

        int applicationIndex = 0;
        while (applicationIndex < descriptor.Applications.Length
            && this.CanContinueValidation())
        {
            this.ValidatePublicConstraintDescriptor(
                value,
                descriptor.Applications[applicationIndex],
                path,
                diagnostics);
            applicationIndex = applicationIndex + 1;
        }

        if (descriptor.RootIntervals.Length == 0
            || !this.HasPublicIntervalValue(value, descriptor))
        {
            return;
        }

        System.Numerics.BigInteger measured = this.GetPublicIntervalValue(
            value,
            descriptor);
        bool allowed = this.PublicIntervalsContain(
            descriptor.RootIntervals,
            measured);
        if (!allowed && descriptor.Extensible)
        {
            allowed = descriptor.OpenExtension
                || this.PublicIntervalsContain(
                    descriptor.ExtensionIntervals,
                    measured);
        }
        if (!allowed)
        {
            this.TryAddDiagnostic(
                diagnostics,
                new AsnDiagnostic
                {
                    Severity = AsnDiagnosticSeverity.Error,
                    Category = AsnDiagnosticCategory.Constraint,
                    Code = "ASNC0031",
                    Message = "The runtime value is outside the normalized constraint at "
                        + path + ".",
                    ConstraintId = descriptor.StableId,
                    ConstraintKind = descriptor.Kind,
                    ValuePath = path,
                    Span = descriptor.SourceSpan
                });
        }
    }

    internal bool CanContinueValidation()
    {
        return this.Limits.Unlimited
            || this.Resources.DiagnosticCount < this.Limits.MaximumDiagnostics;
    }

    internal bool TryAddDiagnostic(
        System.Collections.Generic.List<AsnDiagnostic> diagnostics,
        AsnDiagnostic diagnostic)
    {
        if (!this.Resources.TryChargeDiagnostic())
        {
            return false;
        }

        diagnostics.Add(diagnostic);
        return true;
    }

    internal bool HasPublicIntervalValue(
        AsnRuntimeValue value,
        AsnConstraintDescriptor descriptor)
    {
        if (descriptor.Kind == AsnConstraintKind.Size)
        {
            return value.Kind == AsnRuntimeValueKind.BitString
                && value.BitStringValue != null
                || value.Kind == AsnRuntimeValueKind.OctetString
                || value.Kind == AsnRuntimeValueKind.CharacterString
                || value.Kind == AsnRuntimeValueKind.SequenceOf
                || value.Kind == AsnRuntimeValueKind.SetOf
                || value.Kind == AsnRuntimeValueKind.ObjectIdentifier
                    && value.ObjectIdentifierValue != null
                || value.Kind == AsnRuntimeValueKind.RelativeObjectIdentifier
                    && value.ObjectIdentifierValue != null;
        }
        return (value.Kind == AsnRuntimeValueKind.Integer
                || value.Kind == AsnRuntimeValueKind.Enumerated)
            && value.HasIntegerValue;
    }

    internal System.Numerics.BigInteger GetPublicIntervalValue(
        AsnRuntimeValue value,
        AsnConstraintDescriptor descriptor)
    {
        if (descriptor.Kind != AsnConstraintKind.Size)
        {
            return value.IntegerValue;
        }
        if (value.Kind == AsnRuntimeValueKind.BitString
            && value.BitStringValue != null)
        {
            return value.BitStringValue.BitLength;
        }
        if (value.Kind == AsnRuntimeValueKind.OctetString)
        {
            return value.Octets.Length;
        }
        if (value.Kind == AsnRuntimeValueKind.CharacterString)
        {
            return this.PublicTextScalarCount(value.TextValue);
        }
        if (value.Kind == AsnRuntimeValueKind.SequenceOf
            || value.Kind == AsnRuntimeValueKind.SetOf)
        {
            return value.Items.Length;
        }
        return value.ObjectIdentifierValue == null
            ? System.Numerics.BigInteger.Zero
            : value.ObjectIdentifierValue.Arcs.Length;
    }

    internal int PublicTextScalarCount(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsHighSurrogate(current)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index = index + 1;
            }
            count = count + 1;
            index = index + 1;
        }
        return count;
    }

    internal bool PublicIntervalsContain(
        AsnIntervalDescriptor[] intervals,
        System.Numerics.BigInteger value)
    {
        int index = 0;
        while (index < intervals.Length)
        {
            AsnIntervalDescriptor interval = intervals[index];
            if ((!interval.HasLowerBound || value >= interval.LowerBound)
                && (!interval.HasUpperBound || value <= interval.UpperBound))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal AsnDiagnostic CreateDiagnostic(
        RuntimeConstraintEvaluationResult result,
        ResolvedTypeSyntax type)
    {
        NormalizedConstraintNode? node = result.ViolatedNode;
        int number = result.RequiresApplicationValidation
            ? ConstraintDiagnosticCodes.ApplicationValidationRequired
            : ConstraintDiagnosticCodes.RuntimeConstraintViolation;
        return new AsnDiagnostic
        {
            Severity = AsnDiagnosticSeverity.Error,
            Category = AsnDiagnosticCategory.Constraint,
            Code = "ASNC" + number.ToString(
                "D4",
                System.Globalization.CultureInfo.InvariantCulture),
            Message = result.Message + " Value path: " + result.ValuePath + ".",
            ConstraintId = node == null ? "" : this.CreateConstraintId(node, type),
            ConstraintKind = node == null ? AsnConstraintKind.Unknown : node.Kind,
            ValuePath = result.ValuePath,
            Span = node == null ? null : this.GetRuntimeNodeSpan(node)
        };
    }

    internal string CreateConstraintId(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        if (node.CompiledTableConstraint != null
            && node.CompiledTableConstraint.StableId.Length > 0)
        {
            return node.CompiledTableConstraint.StableId;
        }

        string owner = type.SourceDeclaration == null
            ? type.SourceType == null ? type.EffectiveKind.ToString() : type.SourceType.Name
            : type.SourceDeclaration.QualifiedName;
        AsnSourceSpan? span = this.GetRuntimeNodeSpan(node);
        string offset = span == null
            ? "unknown"
            : span.Start.Offset.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        return owner + ":constraint@" + offset;
    }

    internal AsnSourceSpan? GetRuntimeNodeSpan(NormalizedConstraintNode node)
    {
        if (node.SourceSyntax != null && node.SourceSyntax.Span != null)
        {
            return node.SourceSyntax.Span;
        }

        if (node.SourceNamedConstraint != null
            && node.SourceNamedConstraint.Span != null)
        {
            return node.SourceNamedConstraint.Span;
        }

        if (node.SourceGeneralConstraint != null
            && node.SourceGeneralConstraint.Span != null)
        {
            return node.SourceGeneralConstraint.Span;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            AsnSourceSpan? childSpan = this.GetRuntimeNodeSpan(node.Children[childIndex]);
            if (childSpan != null)
            {
                return childSpan;
            }

            childIndex = childIndex + 1;
        }

        return null;
    }

    internal RuntimeConstraintEvaluationResult CreateSuccess()
    {
        return new RuntimeConstraintEvaluationResult { Success = true };
    }

    internal RuntimeConstraintEvaluationResult CreateFailure(
        NormalizedConstraintNode node,
        string path,
        string message)
    {
        return new RuntimeConstraintEvaluationResult
        {
            ViolatedNode = node,
            ValuePath = path,
            Message = message
        };
    }

    internal RuntimeConstraintEvaluationResult CreateApplicationRequirement(
        NormalizedConstraintNode node,
        string path)
    {
        return new RuntimeConstraintEvaluationResult
        {
            ViolatedNode = node,
            ValuePath = path,
            Message = "The user-defined constraint requires application-supplied validation.",
            RequiresApplicationValidation = true
        };
    }
}
