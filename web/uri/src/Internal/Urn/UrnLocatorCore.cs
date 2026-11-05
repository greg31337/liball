using System;

namespace uri;

internal static class UrnLocatorCore
{
    internal const string QueryCollisionMessage = "The resolved locator and URN both contain a query component.";
    internal const string FragmentCollisionMessage = "The resolved locator and URN both contain a fragment component.";

    internal static UriTextResult SeparateAssignedName(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? precondition = ValidateUrnModel(urn, limits);
        if (precondition != null || urn == null || limits == null)
        {
            return UrnNormalizerCore.CreateFailure(precondition);
        }

        TextOutput output = new TextOutput(limits.MaxOutputCharacters);
        AppendAssignedName(urn, output);
        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return UrnNormalizerCore.CreateFailure(
                UrnNormalizerCore.CreateOutputDiagnostic(status));
        }

        string text = output.GetText();
        return new UriTextResult(true, text, text.Length, null);
    }

    internal static UriEditCoreResult ApplyUrnComponentsToLocator(
        Urn? urn,
        UriReference? locator,
        UrnLocatorMergeOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateMergeOptions(options);
        if (optionFailure != null || options == null)
        {
            return UriEditorCore.CreateEditFailure(optionFailure);
        }

        UriDiagnostic? urnFailure = ValidateUrnModel(urn, options.Limits);
        if (urnFailure != null || urn == null)
        {
            return UriEditorCore.CreateEditFailure(urnFailure);
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        UriValidationResult locatorValidation =
            UriValidatorCore.ValidateComponents(locator, validationOptions);
        if (!locatorValidation.Success || locator == null)
        {
            return UriEditorCore.CreateEditFailure(
                locatorValidation.Diagnostic);
        }

        UriDiagnostic? collision = GetCollisionDiagnostic(
            urn,
            locator,
            options);
        if (collision != null)
        {
            return UriEditorCore.CreateEditFailure(collision);
        }

        UriReference? edited = UriEditorCore.CloneEditableReference(locator);
        if (edited == null)
        {
            return UriEditorCore.CreateEditFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn));
        }

        ApplyQueryComponent(urn, edited, options.QueryCollisionMode);
        ApplyFragmentComponent(
            urn,
            edited,
            options.FragmentCollisionMode);
        return UriEditorCore.ValidateAndComposeEdit(
            edited,
            validationOptions);
    }

    internal static void ApplyQueryComponent(
        Urn urn,
        UriReference locator,
        UrnLocatorCollisionMode mode)
    {
        if (!urn.HasQComponent)
        {
            return;
        }

        if (!locator.HasQuery ||
            mode == UrnLocatorCollisionMode.ReplaceLocator)
        {
            locator.HasQuery = true;
            locator.Query = urn.QComponent;
        }
    }

    internal static void ApplyFragmentComponent(
        Urn urn,
        UriReference locator,
        UrnLocatorCollisionMode mode)
    {
        if (!urn.HasFComponent)
        {
            return;
        }

        if (!locator.HasFragment ||
            mode == UrnLocatorCollisionMode.ReplaceLocator)
        {
            locator.HasFragment = true;
            locator.Fragment = urn.FComponent;
        }
    }

    internal static UriDiagnostic? GetCollisionDiagnostic(
        Urn urn,
        UriReference locator,
        UrnLocatorMergeOptions options)
    {
        if (urn.HasQComponent && locator.HasQuery &&
            options.QueryCollisionMode == UrnLocatorCollisionMode.Reject)
        {
            return CreateCollisionDiagnostic(
                UriComponent.Query,
                QueryCollisionMessage);
        }

        if (urn.HasFComponent && locator.HasFragment &&
            options.FragmentCollisionMode == UrnLocatorCollisionMode.Reject)
        {
            return CreateCollisionDiagnostic(
                UriComponent.Fragment,
                FragmentCollisionMessage);
        }

        return null;
    }

    internal static UriDiagnostic? ValidateMergeOptions(
        UrnLocatorMergeOptions? options)
    {
        if (options == null || options.Limits == null ||
            !IsValidCollisionMode(options.QueryCollisionMode) ||
            !IsValidCollisionMode(options.FragmentCollisionMode))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Urn);
        }

        return null;
    }

    internal static bool IsValidCollisionMode(UrnLocatorCollisionMode mode)
    {
        return mode == UrnLocatorCollisionMode.Reject ||
            mode == UrnLocatorCollisionMode.KeepLocator ||
            mode == UrnLocatorCollisionMode.ReplaceLocator;
    }

    internal static UriDiagnostic? ValidateUrnModel(
        Urn? urn,
        OperationLimits? limits)
    {
        UriDiagnostic? fields =
            UrnNormalizerCore.ValidateCompleteUrnPreconditions(urn, limits);
        if (fields != null || urn == null || limits == null)
        {
            return fields;
        }

        TextOutput input = new TextOutput(limits.MaxInputCharacters);
        AppendCompleteUrn(urn, input);
        TextOutputResult status = input.GetStatus();
        if (!status.Success)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.Urn,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input,
                status.ErrorOffset,
                status.ErrorLength,
                LimitGuard.InputLimitMessage);
        }

        UrnParseResult parsed = UrnParserCore.ParseUrn(
            input.GetText().AsSpan(),
            UrnValidationMode.GenericSyntax);
        if (!parsed.Success)
        {
            return parsed.Diagnostic;
        }

        return GetComponentLimitDiagnostic(urn, limits);
    }

    internal static UriDiagnostic? GetComponentLimitDiagnostic(
        Urn urn,
        OperationLimits limits)
    {
        int namespaceOffset = urn.Scheme.Length + 1;
        UriDiagnostic? diagnostic = LimitGuard.CheckComponentCharacters(
            urn.NamespaceIdentifier.Length,
            limits,
            DiagnosticDomain.Urn,
            UriComponent.UrnNamespaceIdentifier,
            namespaceOffset);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        int namespaceSpecificStringOffset = namespaceOffset +
            urn.NamespaceIdentifier.Length + 1;
        diagnostic = LimitGuard.CheckComponentCharacters(
            urn.NamespaceSpecificString.Length,
            limits,
            DiagnosticDomain.Urn,
            UriComponent.UrnNamespaceSpecificString,
            namespaceSpecificStringOffset);
        if (diagnostic != null)
        {
            return diagnostic;
        }

        int offset = namespaceSpecificStringOffset +
            urn.NamespaceSpecificString.Length;
        if (urn.HasRComponent)
        {
            offset = offset + 2;
            diagnostic = LimitGuard.CheckComponentCharacters(
                urn.RComponent.Length,
                limits,
                DiagnosticDomain.Urn,
                UriComponent.UrnRComponent,
                offset);
            if (diagnostic != null)
            {
                return diagnostic;
            }

            offset = offset + urn.RComponent.Length;
        }

        if (urn.HasQComponent)
        {
            offset = offset + 2;
            diagnostic = LimitGuard.CheckComponentCharacters(
                urn.QComponent.Length,
                limits,
                DiagnosticDomain.Urn,
                UriComponent.UrnQComponent,
                offset);
            if (diagnostic != null)
            {
                return diagnostic;
            }

            offset = offset + urn.QComponent.Length;
        }

        if (urn.HasFComponent)
        {
            offset = offset + 1;
            diagnostic = LimitGuard.CheckComponentCharacters(
                urn.FComponent.Length,
                limits,
                DiagnosticDomain.Urn,
                UriComponent.UrnFComponent,
                offset);
        }

        return diagnostic;
    }

    internal static void AppendCompleteUrn(Urn urn, TextOutput output)
    {
        AppendAssignedName(urn, output);
        if (urn.HasRComponent)
        {
            output.AppendText("?+");
            output.AppendText(urn.RComponent);
        }

        if (urn.HasQComponent)
        {
            output.AppendText("?=");
            output.AppendText(urn.QComponent);
        }

        if (urn.HasFComponent)
        {
            output.AppendCharacter('#');
            output.AppendText(urn.FComponent);
        }
    }

    internal static void AppendAssignedName(Urn urn, TextOutput output)
    {
        output.AppendText(urn.Scheme);
        output.AppendCharacter(':');
        output.AppendText(urn.NamespaceIdentifier);
        output.AppendCharacter(':');
        output.AppendText(urn.NamespaceSpecificString);
    }

    internal static UriDiagnostic CreateCollisionDiagnostic(
        UriComponent component,
        string message)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.UnresolvableReference,
            component,
            0,
            0,
            message);
    }
}
