namespace xml;

/// <summary>Provides the public XSLT transformer implementation.</summary>
public class XsltTransformer : IXsltTransformer
{
    /// <summary>Creates an XSLT transformer.</summary>
    public XsltTransformer()
    {
    }

    /// <summary>Executes an XSLT transformation.</summary>
    public XsltTransformResult TransformXslt(XsltExecutable executable, XsltTransformOptions options)
    {
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        if (state.TemplateRuntime == null)
        {
            throw new System.InvalidOperationException("The XSLT template runtime is unavailable.");
        }

        state.TemplateRuntime.ExecuteXsltInitialTemplateInvocation();
        return state.Result;
    }
}
