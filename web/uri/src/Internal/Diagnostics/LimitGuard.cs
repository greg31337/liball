namespace uri;

internal static class LimitGuard
{
    internal const string InputLimitMessage = "The input exceeds the configured character limit.";
    internal const string ComponentLimitMessage = "The component exceeds the configured character limit.";
    internal const string CollectionLimitMessage = "The collection exceeds the configured item limit.";
    internal const string NestingLimitMessage = "The input exceeds the configured nesting limit.";
    internal const string OutputLimitMessage = "The output exceeds the configured character limit.";

    internal static UriDiagnostic? CheckInputCharacters(
        int characterCount,
        OperationLimits limits,
        DiagnosticDomain domain)
    {
        int maximum = limits.MaxInputCharacters;
        if (maximum >= 0 && characterCount <= maximum)
        {
            return null;
        }

        return CreateCharacterLimitDiagnostic(
            domain,
            UriComponent.Input,
            characterCount,
            maximum,
            0,
            InputLimitMessage);
    }

    internal static UriDiagnostic? CheckComponentCharacters(
        int characterCount,
        OperationLimits limits,
        DiagnosticDomain domain,
        UriComponent component,
        int componentOffset)
    {
        int maximum = limits.MaxComponentCharacters;
        if (maximum >= 0 && characterCount <= maximum)
        {
            return null;
        }

        return CreateCharacterLimitDiagnostic(
            domain,
            component,
            characterCount,
            maximum,
            componentOffset,
            ComponentLimitMessage);
    }

    internal static UriDiagnostic? CheckCollectionItems(
        int itemCount,
        int maximumItems,
        DiagnosticDomain domain,
        UriComponent component,
        int offset)
    {
        if (maximumItems >= 0 && itemCount <= maximumItems)
        {
            return null;
        }

        return DiagnosticFactory.CreateError(
            domain,
            UriDiagnosticCode.LimitExceeded,
            component,
            offset,
            0,
            CollectionLimitMessage);
    }

    internal static UriDiagnostic? CheckPathSegments(
        int segmentCount,
        OperationLimits limits,
        DiagnosticDomain domain,
        int offset)
    {
        return CheckCollectionItems(
            segmentCount,
            limits.MaxPathSegments,
            domain,
            UriComponent.Path,
            offset);
    }

    internal static UriDiagnostic? CheckQueryItems(
        int itemCount,
        OperationLimits limits,
        DiagnosticDomain domain,
        int offset)
    {
        return CheckCollectionItems(
            itemCount,
            limits.MaxQueryItems,
            domain,
            UriComponent.Query,
            offset);
    }

    internal static UriDiagnostic? CheckTemplateExpressions(
        int expressionCount,
        OperationLimits limits,
        int offset)
    {
        return CheckCollectionItems(
            expressionCount,
            limits.MaxTemplateExpressions,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateExpression,
            offset);
    }

    internal static UriDiagnostic? CheckTemplateVariables(
        int variableCount,
        OperationLimits limits,
        int offset)
    {
        return CheckCollectionItems(
            variableCount,
            limits.MaxTemplateVariables,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateVariable,
            offset);
    }

    internal static UriDiagnostic? CheckTemplateCompositeMembers(
        int memberCount,
        OperationLimits limits,
        int offset)
    {
        return CheckCollectionItems(
            memberCount,
            limits.MaxTemplateCompositeMembers,
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateVariable,
            offset);
    }

    internal static UriDiagnostic? CheckWebLinks(
        int linkCount,
        OperationLimits limits,
        int offset)
    {
        return CheckCollectionItems(
            linkCount,
            limits.MaxWebLinks,
            DiagnosticDomain.WebLink,
            UriComponent.WebLink,
            offset);
    }

    internal static UriDiagnostic? CheckWebLinkParameters(
        int parameterCount,
        OperationLimits limits,
        int offset)
    {
        return CheckCollectionItems(
            parameterCount,
            limits.MaxWebLinkParameters,
            DiagnosticDomain.WebLink,
            UriComponent.LinkParameter,
            offset);
    }

    internal static UriDiagnostic? CheckNestingDepth(
        int nestingDepth,
        OperationLimits limits,
        DiagnosticDomain domain,
        UriComponent component,
        int offset)
    {
        int maximum = limits.MaxNestingDepth;
        if (maximum >= 0 && nestingDepth <= maximum)
        {
            return null;
        }

        return DiagnosticFactory.CreateError(
            domain,
            UriDiagnosticCode.LimitExceeded,
            component,
            offset,
            1,
            NestingLimitMessage);
    }

    internal static UriDiagnostic? CheckOutputCharacters(
        int characterCount,
        OperationLimits limits,
        DiagnosticDomain domain)
    {
        int maximum = limits.MaxOutputCharacters;
        if (maximum >= 0 && characterCount <= maximum)
        {
            return null;
        }

        return CreateCharacterLimitDiagnostic(
            domain,
            UriComponent.Output,
            characterCount,
            maximum,
            0,
            OutputLimitMessage);
    }

    internal static UriDiagnostic CreateCharacterLimitDiagnostic(
        DiagnosticDomain domain,
        UriComponent component,
        int characterCount,
        int maximumCharacters,
        int baseOffset,
        string message)
    {
        int offset = baseOffset;
        int length = 0;

        if (maximumCharacters >= 0)
        {
            if (baseOffset <= int.MaxValue - maximumCharacters)
            {
                offset = baseOffset + maximumCharacters;
            }

            if (characterCount > maximumCharacters)
            {
                length = characterCount - maximumCharacters;
            }
        }

        return DiagnosticFactory.CreateError(
            domain,
            UriDiagnosticCode.LimitExceeded,
            component,
            offset,
            length,
            message);
    }
}
