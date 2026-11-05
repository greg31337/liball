using System;

namespace uri;

internal static class TemplateExpanderCore
{
    internal const string CompositePrefixMessage = "Prefix modifiers cannot be applied to composite URI Template values.";

    internal static UriTemplateExpansionResult ExpandValues(
        UriTemplate template,
        UriTemplateBinding[] bindings)
    {
        return ExpandResult(
            template,
            bindings,
            new UriTemplateExpansionOptions());
    }

    internal static string ExpandStrict(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        UriTemplateExpansionResult result = ExpandResult(
            template,
            bindings,
            options);
        if (!result.Success)
        {
            UriDiagnostic diagnostic = result.Diagnostic ??
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.UriTemplate);
            throw new UriTemplateExpansionException(diagnostic);
        }

        return result.Text;
    }

    internal static UriTemplateExpansionResult ExpandResult(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        return ExpandEngine(template, bindings, options, false);
    }

    internal static UriTemplateExpansionResult ExpandDiagnostic(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        return ExpandEngine(template, bindings, options, true);
    }

    internal static string ExpandPartial(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        if (template == null || bindings == null || options == null ||
            options.Limits == null)
        {
            throw new UriTemplateExpansionException(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate));
        }

        UriDiagnostic? modelFailure = ValidateExpansionModel(
            template,
            bindings);
        if (modelFailure != null)
        {
            throw new UriTemplateExpansionException(modelFailure);
        }

        OperationLimits limits = options.Limits;
        UriDiagnostic? memberLimit = LimitGuard.CheckTemplateCompositeMembers(
            0,
            limits,
            0);
        if (memberLimit != null)
        {
            throw new UriTemplateExpansionException(memberLimit);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            throw new UriTemplateExpansionException(
                CreateOutputDiagnostic(status));
        }

        TemplateExpansionState state = new TemplateExpansionState(limits);
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            UriDiagnostic? diagnostic = null;
            if (part.Kind == UriTemplatePartKind.Literal)
            {
                diagnostic = WriteLiteral(part, output);
            }
            else if (IsExpressionResolved(part, bindings))
            {
                diagnostic = WriteExpression(part, bindings, output, state);
            }
            else
            {
                AppendOriginalPart(template, part, output);
            }

            if (diagnostic != null)
            {
                throw new UriTemplateExpansionException(diagnostic);
            }

            status = output.GetStatus();
            if (!status.Success)
            {
                throw new UriTemplateExpansionException(
                    CreateOutputDiagnostic(status));
            }

            partIndex = partIndex + 1;
        }

        return output.GetText();
    }

    internal static bool IsExpressionResolved(
        UriTemplatePart part,
        UriTemplateBinding[] bindings)
    {
        int variableIndex = 0;
        while (variableIndex < part.Variables.Length)
        {
            UriTemplateBinding? binding = FindBinding(
                part.Variables[variableIndex].Name,
                bindings);
            if (binding == null ||
                binding.Value.Kind == UriTemplateValueKind.Undefined)
            {
                return false;
            }

            variableIndex = variableIndex + 1;
        }

        return true;
    }

    internal static UriTemplateExpansionResult ExpandEngine(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options,
        bool diagnosticOutput)
    {
        if (template == null || bindings == null || options == null ||
            options.Limits == null)
        {
            return CreateExpansionFailure(
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate));
        }

        UriDiagnostic? modelFailure = ValidateExpansionModel(
            template,
            bindings);
        if (modelFailure != null)
        {
            return CreateExpansionFailure(modelFailure);
        }

        OperationLimits limits = options.Limits;
        UriDiagnostic? memberLimit = LimitGuard.CheckTemplateCompositeMembers(
            0,
            limits,
            0);
        if (memberLimit != null)
        {
            return CreateExpansionFailure(memberLimit);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        TextOutputResult initialStatus = output.GetStatus();
        if (!initialStatus.Success)
        {
            return CreateExpansionFailure(CreateOutputDiagnostic(initialStatus));
        }

        TemplateExpansionState state = new TemplateExpansionState(limits);
        UriDiagnostic? firstDiagnostic = null;
        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart part = template.Parts[partIndex];
            int partOutputOffset = output.GetLength();
            UriDiagnostic? diagnostic;
            if (part.Kind == UriTemplatePartKind.Literal)
            {
                diagnostic = WriteLiteral(part, output);
            }
            else
            {
                diagnostic = WriteExpression(part, bindings, output, state);
            }

            TextOutputResult status = output.GetStatus();
            if (!status.Success)
            {
                UriDiagnostic outputDiagnostic = CreateOutputDiagnostic(status);
                if (diagnosticOutput)
                {
                    return CreateDiagnosticFailure(output, outputDiagnostic);
                }

                return CreateExpansionFailure(outputDiagnostic);
            }

            if (diagnostic != null)
            {
                if (!diagnosticOutput)
                {
                    return CreateExpansionFailure(diagnostic);
                }

                if (firstDiagnostic == null)
                {
                    firstDiagnostic = diagnostic;
                }

                output.Length = partOutputOffset;
                if (part.Kind == UriTemplatePartKind.Expression)
                {
                    AppendOriginalPart(template, part, output);
                }
                else
                {
                    AppendOriginalRemainder(template, part.Range.Offset, output);
                }

                status = output.GetStatus();
                if (!status.Success)
                {
                    return CreateDiagnosticFailure(
                        output,
                        CreateOutputDiagnostic(status));
                }

                if (part.Kind == UriTemplatePartKind.Literal)
                {
                    return CreateDiagnosticFailure(output, firstDiagnostic);
                }
            }

            partIndex = partIndex + 1;
        }

        string text = output.GetText();
        if (firstDiagnostic != null)
        {
            return new UriTemplateExpansionResult(
                false,
                text,
                text.Length,
                firstDiagnostic);
        }

        return new UriTemplateExpansionResult(
            true,
            text,
            text.Length,
            null);
    }

    internal static UriDiagnostic? WriteLiteral(
        UriTemplatePart part,
        TextOutput output)
    {
        ReadOnlySpan<char> text = part.Literal.AsSpan();
        byte[] scalarBytes = new byte[4];
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    part.Range.Offset,
                    UriComponent.TemplateLiteral);
                if (!triplet.Success)
                {
                    int errorOffset = part.Range.Offset + offset;
                    int errorLength = 1;
                    if (triplet.Diagnostic != null)
                    {
                        errorOffset = triplet.Diagnostic.Range.Offset;
                        errorLength = triplet.Diagnostic.Range.Length;
                    }

                    return DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidPercentEncoding,
                        UriComponent.TemplateLiteral,
                        errorOffset,
                        errorLength,
                        DiagnosticFactory.InvalidPercentEncodingMessage);
                }

                output.AppendSpan(text.Slice(offset, 3));
                offset = triplet.NextOffset;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.UriTemplate,
                    UriComponent.TemplateLiteral,
                    part.Range.Offset + scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            if (scalar.ScalarValue <= 127 &&
                (Ascii.IsUnreserved((char)scalar.ScalarValue) ||
                    Ascii.IsReserved((char)scalar.ScalarValue)))
            {
                output.AppendCharacter((char)scalar.ScalarValue);
            }
            else
            {
                AppendEncodedScalar(scalar.ScalarValue, scalarBytes, output);
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static UriDiagnostic? WriteExpression(
        UriTemplatePart part,
        UriTemplateBinding[] bindings,
        TextOutput output,
        TemplateExpansionState state)
    {
        TemplateOperatorMetadata metadata = TemplateOperatorTable.Get(part.Operator);
        bool emitted = false;
        int variableIndex = 0;
        while (variableIndex < part.Variables.Length)
        {
            UriTemplateVariable variable = part.Variables[variableIndex];
            UriTemplateBinding? binding = FindBinding(variable.Name, bindings);
            if (binding == null || binding.Value.Kind == UriTemplateValueKind.Undefined)
            {
                variableIndex = variableIndex + 1;
                continue;
            }

            if (binding.Value.Kind == UriTemplateValueKind.Map)
            {
                UriDiagnostic? memberLimit = ConsumeCompositeMembers(
                    binding.Value.Map.Length,
                    variable,
                    state);
                if (memberLimit != null)
                {
                    return memberLimit;
                }

                if (variable.HasPrefix)
                {
                    return DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidTemplateModifier,
                        UriComponent.TemplateVariable,
                        variable.Range.Offset + variable.Name.Length,
                        1,
                        CompositePrefixMessage);
                }

                UriTemplateMapEntry[] members = binding.Value.Map;
                int definedCount = 0;
                int validationIndex = 0;
                while (validationIndex < members.Length)
                {
                    UriTemplateMapEntry member = members[validationIndex];
                    if (member.Defined)
                    {
                        UriDiagnostic? invalidKey = ValidateScalar(member.Key);
                        if (invalidKey != null)
                        {
                            return invalidKey;
                        }

                        UriDiagnostic? invalidValue = ValidateScalar(member.Value);
                        if (invalidValue != null)
                        {
                            return invalidValue;
                        }

                        definedCount = definedCount + 1;
                    }

                    validationIndex = validationIndex + 1;
                }

                if (definedCount == 0)
                {
                    variableIndex = variableIndex + 1;
                    continue;
                }

                if (variable.Explode)
                {
                    int memberIndex = 0;
                    while (memberIndex < members.Length)
                    {
                        UriTemplateMapEntry member = members[memberIndex];
                        if (!member.Defined)
                        {
                            memberIndex = memberIndex + 1;
                            continue;
                        }

                        if (emitted)
                        {
                            output.AppendText(metadata.Separator);
                        }
                        else
                        {
                            output.AppendText(metadata.Prefix);
                        }

                        WriteScalarValue(
                            member.Key.AsSpan(),
                            metadata.AllowReserved,
                            output);
                        if (member.Value.Length == 0)
                        {
                            output.AppendText(metadata.EmptyValue);
                        }
                        else
                        {
                            output.AppendCharacter('=');
                            WriteScalarValue(
                                member.Value.AsSpan(),
                                metadata.AllowReserved,
                                output);
                        }

                        emitted = true;
                        memberIndex = memberIndex + 1;
                    }
                }
                else
                {
                    if (emitted)
                    {
                        output.AppendText(metadata.Separator);
                    }
                    else
                    {
                        output.AppendText(metadata.Prefix);
                    }

                    if (metadata.Named)
                    {
                        output.AppendText(variable.Name);
                        output.AppendCharacter('=');
                    }

                    int memberIndex = 0;
                    int emittedMembers = 0;
                    while (memberIndex < members.Length)
                    {
                        UriTemplateMapEntry member = members[memberIndex];
                        if (!member.Defined)
                        {
                            memberIndex = memberIndex + 1;
                            continue;
                        }

                        if (emittedMembers > 0)
                        {
                            output.AppendCharacter(',');
                        }

                        WriteScalarValue(
                            member.Key.AsSpan(),
                            metadata.AllowReserved,
                            output);
                        output.AppendCharacter(',');
                        WriteScalarValue(
                            member.Value.AsSpan(),
                            metadata.AllowReserved,
                            output);
                        emittedMembers = emittedMembers + 1;
                        memberIndex = memberIndex + 1;
                    }

                    emitted = true;
                }

                variableIndex = variableIndex + 1;
                continue;
            }

            if (binding.Value.Kind == UriTemplateValueKind.List)
            {
                UriDiagnostic? memberLimit = ConsumeCompositeMembers(
                    binding.Value.List.Length,
                    variable,
                    state);
                if (memberLimit != null)
                {
                    return memberLimit;
                }

                if (variable.HasPrefix)
                {
                    return DiagnosticFactory.CreateError(
                        DiagnosticDomain.UriTemplate,
                        UriDiagnosticCode.InvalidTemplateModifier,
                        UriComponent.TemplateVariable,
                        variable.Range.Offset + variable.Name.Length,
                        1,
                        CompositePrefixMessage);
                }

                string[] members = binding.Value.List;
                int validationIndex = 0;
                while (validationIndex < members.Length)
                {
                    UriDiagnostic? invalidMember = ValidateScalar(
                        members[validationIndex]);
                    if (invalidMember != null)
                    {
                        return invalidMember;
                    }

                    validationIndex = validationIndex + 1;
                }

                if (members.Length == 0)
                {
                    variableIndex = variableIndex + 1;
                    continue;
                }

                if (variable.Explode)
                {
                    int memberIndex = 0;
                    while (memberIndex < members.Length)
                    {
                        if (emitted)
                        {
                            output.AppendText(metadata.Separator);
                        }
                        else
                        {
                            output.AppendText(metadata.Prefix);
                        }

                        string member = members[memberIndex];
                        if (metadata.Named)
                        {
                            output.AppendText(variable.Name);
                            if (member.Length == 0)
                            {
                                output.AppendText(metadata.EmptyValue);
                            }
                            else
                            {
                                output.AppendCharacter('=');
                            }
                        }

                        WriteScalarValue(
                            member.AsSpan(),
                            metadata.AllowReserved,
                            output);
                        emitted = true;
                        memberIndex = memberIndex + 1;
                    }
                }
                else
                {
                    if (emitted)
                    {
                        output.AppendText(metadata.Separator);
                    }
                    else
                    {
                        output.AppendText(metadata.Prefix);
                    }

                    if (metadata.Named)
                    {
                        output.AppendText(variable.Name);
                        output.AppendCharacter('=');
                    }

                    int memberIndex = 0;
                    while (memberIndex < members.Length)
                    {
                        if (memberIndex > 0)
                        {
                            output.AppendCharacter(',');
                        }

                        WriteScalarValue(
                            members[memberIndex].AsSpan(),
                            metadata.AllowReserved,
                            output);
                        memberIndex = memberIndex + 1;
                    }

                    emitted = true;
                }

                variableIndex = variableIndex + 1;
                continue;
            }

            string value = binding.Value.Scalar;
            UriDiagnostic? invalidUnicode = ValidateScalar(value);
            if (invalidUnicode != null)
            {
                return invalidUnicode;
            }

            int valueLength = value.Length;
            if (variable.HasPrefix)
            {
                valueLength = GetPrefixCodeUnitLength(
                    value,
                    variable.PrefixLength);
            }

            if (emitted)
            {
                output.AppendText(metadata.Separator);
            }
            else
            {
                output.AppendText(metadata.Prefix);
            }

            if (metadata.Named)
            {
                output.AppendText(variable.Name);
                if (valueLength == 0)
                {
                    output.AppendText(metadata.EmptyValue);
                }
                else
                {
                    output.AppendCharacter('=');
                }
            }

            WriteScalarValue(
                value.AsSpan(0, valueLength),
                metadata.AllowReserved,
                output);
            emitted = true;
            variableIndex = variableIndex + 1;
        }

        return null;
    }

    internal static UriDiagnostic? ValidateExpansionModel(
        UriTemplate template,
        UriTemplateBinding[] bindings)
    {
        if (template.Parts == null || template.OriginalText == null)
        {
            return DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.UriTemplate);
        }

        int partIndex = 0;
        while (partIndex < template.Parts.Length)
        {
            UriTemplatePart? part = template.Parts[partIndex];
            if (part == null || part.Range == null ||
                part.Range.Offset < 0 || part.Range.Length < 0)
            {
                return DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate);
            }

            if (part.Kind == UriTemplatePartKind.Literal)
            {
                if (part.Literal == null)
                {
                    return DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.UriTemplate);
                }
            }
            else if (part.Kind == UriTemplatePartKind.Expression)
            {
                if (part.Variables == null ||
                    part.Variables.Length == 0 ||
                    !TemplateComposerCore.IsDefinedOperator(part.Operator))
                {
                    return DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.UriTemplate);
                }

                int variableIndex = 0;
                while (variableIndex < part.Variables.Length)
                {
                    UriTemplateVariable? variable =
                        part.Variables[variableIndex];
                    if (variable == null || variable.Range == null ||
                        variable.Range.Offset < 0 ||
                        variable.Range.Length < 0)
                    {
                        return DiagnosticFactory.CreateInvalidInput(
                            DiagnosticDomain.UriTemplate);
                    }

                    UriDiagnostic? variableFailure =
                        TemplateComposerCore.ValidateVariable(
                            variable,
                            variable.Range.Offset);
                    if (variableFailure != null)
                    {
                        return variableFailure;
                    }

                    variableIndex = variableIndex + 1;
                }
            }
            else
            {
                return DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate);
            }

            partIndex = partIndex + 1;
        }

        int bindingIndex = 0;
        while (bindingIndex < bindings.Length)
        {
            UriTemplateBinding? binding = bindings[bindingIndex];
            if (binding == null || binding.Name == null ||
                binding.Value == null ||
                !IsValidValue(binding.Value))
            {
                return DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.UriTemplate);
            }

            bindingIndex = bindingIndex + 1;
        }

        return null;
    }

    internal static bool IsValidValue(UriTemplateValue value)
    {
        if (value.Kind == UriTemplateValueKind.Undefined)
        {
            return true;
        }

        if (value.Kind == UriTemplateValueKind.Scalar)
        {
            return value.Scalar != null;
        }

        if (value.Kind == UriTemplateValueKind.List)
        {
            if (value.List == null)
            {
                return false;
            }

            int memberIndex = 0;
            while (memberIndex < value.List.Length)
            {
                if (value.List[memberIndex] == null)
                {
                    return false;
                }

                memberIndex = memberIndex + 1;
            }

            return true;
        }

        if (value.Kind == UriTemplateValueKind.Map)
        {
            if (value.Map == null)
            {
                return false;
            }

            int memberIndex = 0;
            while (memberIndex < value.Map.Length)
            {
                UriTemplateMapEntry? member = value.Map[memberIndex];
                if (member == null || member.Key == null ||
                    member.Value == null)
                {
                    return false;
                }

                memberIndex = memberIndex + 1;
            }

            return true;
        }

        return false;
    }

    internal static UriDiagnostic? ConsumeCompositeMembers(
        int additionalMembers,
        UriTemplateVariable variable,
        TemplateExpansionState state)
    {
        if (additionalMembers > int.MaxValue - state.CompositeMembers)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.TemplateVariable,
                variable.Range.Offset,
                0,
                LimitGuard.CollectionLimitMessage);
        }

        int memberCount = state.CompositeMembers + additionalMembers;
        UriDiagnostic? diagnostic = LimitGuard.CheckTemplateCompositeMembers(
            memberCount,
            state.Limits,
            variable.Range.Offset);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        state.CompositeMembers = memberCount;
        return null;
    }

    internal static UriTemplateBinding? FindBinding(
        string name,
        UriTemplateBinding[] bindings)
    {
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index].Name == name)
            {
                return bindings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static UriDiagnostic? ValidateScalar(string value)
    {
        int offset = 0;
        while (offset < value.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(
                value.AsSpan(),
                offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.UriTemplate,
                    UriComponent.TemplateVariable,
                    scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static int GetPrefixCodeUnitLength(
        string value,
        int maximumScalars)
    {
        ReadOnlySpan<char> text = value.AsSpan();
        int offset = 0;
        int scalarCount = 0;
        while (offset < text.Length && scalarCount < maximumScalars)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    0,
                    UriComponent.TemplateVariable);
                if (triplet.Success)
                {
                    offset = triplet.NextOffset;
                    scalarCount = scalarCount + 1;
                    continue;
                }
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            offset = offset + scalar.CodeUnitLength;
            scalarCount = scalarCount + 1;
        }

        return offset;
    }

    internal static void WriteScalarValue(
        ReadOnlySpan<char> value,
        bool allowReserved,
        TextOutput output)
    {
        byte[] scalarBytes = new byte[4];
        int offset = 0;
        while (offset < value.Length)
        {
            if (allowReserved && value[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    value,
                    offset,
                    0,
                    UriComponent.TemplateVariable);
                if (triplet.Success)
                {
                    output.AppendSpan(value.Slice(offset, 3));
                    offset = triplet.NextOffset;
                    continue;
                }
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(value, offset);
            bool allowed = false;
            if (scalar.ScalarValue <= 127)
            {
                if (allowReserved)
                {
                    allowed = PercentAllowlist.IsUriTemplateReservedExpansionCharacter(
                        (char)scalar.ScalarValue);
                }
                else
                {
                    allowed = PercentAllowlist.IsUriTemplateSimpleExpansionCharacter(
                        (char)scalar.ScalarValue);
                }
            }

            if (allowed)
            {
                output.AppendCharacter((char)scalar.ScalarValue);
            }
            else
            {
                AppendEncodedScalar(scalar.ScalarValue, scalarBytes, output);
            }

            offset = offset + scalar.CodeUnitLength;
        }
    }

    internal static void AppendEncodedScalar(
        int scalarValue,
        byte[] scalarBytes,
        TextOutput output)
    {
        int byteCount = Utf8CodecCore.WriteScalar(
            scalarValue,
            scalarBytes,
            0);
        int byteOffset = 0;
        while (byteOffset < byteCount)
        {
            AppendPercentByte(scalarBytes[byteOffset], output);
            byteOffset = byteOffset + 1;
        }
    }

    internal static void AppendPercentByte(byte value, TextOutput output)
    {
        output.AppendCharacter('%');
        output.AppendCharacter(Ascii.GetUpperHexCharacter(value >> 4));
        output.AppendCharacter(Ascii.GetUpperHexCharacter(value & 15));
    }

    internal static UriTemplateExpansionResult CreateExpansionFailure(
        UriDiagnostic diagnostic)
    {
        return new UriTemplateExpansionResult(false, "", 0, diagnostic);
    }

    internal static UriTemplateExpansionResult CreateDiagnosticFailure(
        TextOutput output,
        UriDiagnostic diagnostic)
    {
        string text = output.GetText();
        return new UriTemplateExpansionResult(
            false,
            text,
            text.Length,
            diagnostic);
    }

    internal static UriDiagnostic CreateOutputDiagnostic(TextOutputResult status)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            status.ErrorOffset,
            status.ErrorLength,
            LimitGuard.OutputLimitMessage);
    }

    internal static void AppendOriginalPart(
        UriTemplate template,
        UriTemplatePart part,
        TextOutput output)
    {
        int offset = part.Range.Offset;
        int length = part.Range.Length;
        if (offset >= 0 && length >= 0 &&
            offset <= template.OriginalText.Length &&
            length <= template.OriginalText.Length - offset)
        {
            output.AppendSpan(template.OriginalText.AsSpan(offset, length));
        }
    }

    internal static void AppendOriginalRemainder(
        UriTemplate template,
        int offset,
        TextOutput output)
    {
        if (offset >= 0 && offset <= template.OriginalText.Length)
        {
            output.AppendSpan(template.OriginalText.AsSpan(offset));
        }
    }
}
