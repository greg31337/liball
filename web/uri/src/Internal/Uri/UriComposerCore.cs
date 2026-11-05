namespace uri;

internal static class UriComposerCore
{
    internal static UriCompositionResult RecomposeParsedReference(
        UriReference? reference,
        OperationLimits? limits)
    {
        if (limits == null)
        {
            return CreateCompositionFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationOptions options = new UriValidationOptions();
        options.Limits = limits;
        return ComposeValidatedReference(reference, options);
    }

    internal static UriCompositionResult ComposeValidatedReference(
        UriReference? reference,
        UriValidationOptions? options)
    {
        UriValidationResult validation = UriValidatorCore.ValidateComponents(
            reference,
            options);
        if (!validation.Success)
        {
            return CreateCompositionFailure(validation.Diagnostic);
        }

        if (reference == null || options == null || options.Limits == null)
        {
            return CreateCompositionFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriMeasureResult measurement = MeasureUncheckedReference(
            reference,
            options.Limits);
        if (!measurement.Success)
        {
            return CreateCompositionFailure(measurement.Diagnostic);
        }

        return WriteReference(reference, measurement.Length);
    }

    internal static UriMeasureResult MeasureValidatedReference(
        UriReference? reference,
        UriValidationOptions? options)
    {
        UriValidationResult validation = UriValidatorCore.ValidateComponents(
            reference,
            options);
        if (!validation.Success)
        {
            return CreateMeasureFailure(validation.Diagnostic);
        }

        if (reference == null || options == null || options.Limits == null)
        {
            return CreateMeasureFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        return MeasureUncheckedReference(reference, options.Limits);
    }

    internal static UriMeasureResult MeasureUncheckedReference(
        UriReference reference,
        OperationLimits limits)
    {
        int length = 0;
        UriMeasureResult addition;

        if (reference.HasScheme)
        {
            addition = AddMeasuredLength(length, reference.Scheme.Length);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
            addition = AddMeasuredLength(length, 1);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
        }

        if (reference.HasAuthority)
        {
            addition = AddMeasuredLength(length, 2);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                addition = AddMeasuredLength(length, authority.UserInfo.Length);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
                addition = AddMeasuredLength(length, 1);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
            }

            if (IsIpLiteral(authority.HostKind))
            {
                addition = AddMeasuredLength(length, 1);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
            }

            addition = AddMeasuredLength(length, authority.Host.Length);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
            if (IsIpLiteral(authority.HostKind))
            {
                addition = AddMeasuredLength(length, 1);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
            }

            if (authority.HasPort)
            {
                addition = AddMeasuredLength(length, 1);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
                addition = AddMeasuredLength(length, authority.Port.Length);
                if (!addition.Success)
                {
                    return addition;
                }

                length = addition.Length;
            }
        }

        addition = AddMeasuredLength(length, reference.Path.Length);
        if (!addition.Success)
        {
            return addition;
        }

        length = addition.Length;
        if (reference.HasQuery)
        {
            addition = AddMeasuredLength(length, 1);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
            addition = AddMeasuredLength(length, reference.Query.Length);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
        }

        if (reference.HasFragment)
        {
            addition = AddMeasuredLength(length, 1);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
            addition = AddMeasuredLength(length, reference.Fragment.Length);
            if (!addition.Success)
            {
                return addition;
            }

            length = addition.Length;
        }

        UriDiagnostic? outputLimit = LimitGuard.CheckOutputCharacters(
            length,
            limits,
            DiagnosticDomain.Uri);
        if (outputLimit != null)
        {
            return CreateMeasureFailure(outputLimit);
        }

        return new UriMeasureResult(true, length, null);
    }

    internal static UriMeasureResult AddMeasuredLength(
        int currentLength,
        int additionalLength)
    {
        if (currentLength < 0 || additionalLength < 0)
        {
            return CreateMeasureFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        if (additionalLength > int.MaxValue - currentLength)
        {
            UriDiagnostic overflow = DiagnosticFactory.CreateError(
                DiagnosticDomain.Uri,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                int.MaxValue,
                0,
                LimitGuard.OutputLimitMessage);
            return CreateMeasureFailure(overflow);
        }

        return new UriMeasureResult(true, currentLength + additionalLength, null);
    }

    internal static bool IsIpLiteral(UriHostKind kind)
    {
        return kind == UriHostKind.Ipv6 || kind == UriHostKind.IpvFuture;
    }

    internal static UriCompositionResult WriteReference(
        UriReference reference,
        int measuredLength)
    {
        TextOutput output = new TextOutput(measuredLength);
        if (reference.HasScheme)
        {
            output.AppendText(reference.Scheme);
            output.AppendCharacter(':');
        }

        if (reference.HasAuthority)
        {
            output.AppendText("//");
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                output.AppendText(authority.UserInfo);
                output.AppendCharacter('@');
            }

            if (IsIpLiteral(authority.HostKind))
            {
                output.AppendCharacter('[');
            }

            output.AppendText(authority.Host);
            if (IsIpLiteral(authority.HostKind))
            {
                output.AppendCharacter(']');
            }

            if (authority.HasPort)
            {
                output.AppendCharacter(':');
                output.AppendText(authority.Port);
            }
        }

        output.AppendText(reference.Path);
        if (reference.HasQuery)
        {
            output.AppendCharacter('?');
            output.AppendText(reference.Query);
        }

        if (reference.HasFragment)
        {
            output.AppendCharacter('#');
            output.AppendText(reference.Fragment);
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            UriDiagnostic outputFailure = DiagnosticFactory.CreateError(
                DiagnosticDomain.Uri,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                status.ErrorOffset,
                status.ErrorLength,
                LimitGuard.OutputLimitMessage);
            return CreateCompositionFailure(outputFailure);
        }

        return new UriCompositionResult(
            true,
            output.GetText(),
            output.GetLength(),
            null);
    }

    internal static UriCompositionResult CreateCompositionFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriCompositionResult(false, "", 0, failure);
    }

    internal static UriMeasureResult CreateMeasureFailure(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriMeasureResult(false, 0, failure);
    }
}
