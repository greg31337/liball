namespace uri;

/// <summary>
/// Provides validated and bounded URI Template model composition.
/// </summary>
public class UriTemplateComposer : IUriTemplateComposer
{
    /// <summary>Initializes a stateless URI Template composer.</summary>
    public UriTemplateComposer()
    {
    }

    /// <summary>Composes the template model without semantic rewriting.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed URI Template text.</returns>
    /// <exception cref="UriTemplateSyntaxException">The model or a configured limit is invalid.</exception>
    public string ComposeTemplate(
        UriTemplate template,
        OperationLimits limits)
    {
        UriTextResult result = TemplateComposerCore.ComposeTemplate(
            template,
            limits);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Composes the template while uppercasing percent-triplet hexadecimal digits.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed URI Template text with canonical percent-triplet case.</returns>
    /// <exception cref="UriTemplateSyntaxException">The model or a configured limit is invalid.</exception>
    public string CanonicalizeTemplatePercentCase(
        UriTemplate template,
        OperationLimits limits)
    {
        UriTextResult result =
            TemplateComposerCore.CanonicalizePercentCase(
                template,
                limits);
        if (!result.Success)
        {
            throw CreateSyntaxException(result.Diagnostic);
        }

        return result.Text;
    }

    internal UriTemplateSyntaxException CreateSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.UriTemplate);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriTemplateSyntaxException(failure);
    }
}
