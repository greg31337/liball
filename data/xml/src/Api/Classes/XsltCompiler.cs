namespace xml;

/// <summary>Compiles XSLT stylesheets and packages into reusable immutable executables.</summary>
public class XsltCompiler : IXsltCompiler
{
    /// <summary>Creates an XSLT compiler.</summary>
    public XsltCompiler()
    {
    }

    /// <summary>Compiles an XSLT stylesheet or package.</summary>
    public XsltExecutable CompileXslt(XmlInput stylesheet, XsltCompileOptions options, IXmlResourceResolver resolver)
    {
        Guard.NotNull(stylesheet, "stylesheet");
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        ValidateXsltCompileOptions(options);
        return new XsltStylesheetLoader(options, resolver, new XsltPackageCompilationContext(), false).CompileXsltStylesheet(stylesheet);
    }

    internal void ValidateXsltCompileOptions(XsltCompileOptions options)
    {
        if (options.ConformanceLevel != XsltConformanceLevel.Basic && options.ConformanceLevel != XsltConformanceLevel.SchemaAware && options.ConformanceLevel != XsltConformanceLevel.Streaming)
        {
            throw new System.ArgumentException("The XSLT conformance level is invalid.", "options");
        }

        if (options.RecoveryPolicy != XsltRecoveryPolicy.Strict && options.RecoveryPolicy != XsltRecoveryPolicy.Recover)
        {
            throw new System.ArgumentException("The XSLT recovery policy is invalid.", "options");
        }

        XmlSchemaSet? schemas = options.SchemaSet;
        if (options.ConformanceLevel == XsltConformanceLevel.SchemaAware && schemas != null && schemas.CompiledSchemaSet == null && (!schemas.IsImmutable || schemas.CompiledSet == null))
        {
            throw new System.ArgumentException("Schema-aware XSLT compilation requires an immutable compiled schema set.", "options");
        }
    }
}
