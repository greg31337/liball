namespace cbor;

/// <summary>Compiles parsed CDDL documents into reusable schemas.</summary>
public class CddlCompiler : ICddlCompiler
{
    /// <summary>Creates a CDDL compiler.</summary>
    public CddlCompiler()
    {
    }

    /// <summary>Compiles a parsed document or throws on failure.</summary>
    /// <param name="document">The parsed source document.</param>
    /// <param name="options">The compilation policies and limits.</param>
    /// <returns>The compiled schema.</returns>
    public CddlSchema CompileSchema(CddlDocument document, CddlCompileOptions options)
    {
        CddlCompileResult result = TryCompileSchema(document, options);
        if (!result.IsSuccess || result.Schema == null)
        {
            if (result.Diagnostics.Length > 0)
            {
                CddlDiagnostic diagnostic = result.Diagnostics[0];
                throw new CddlCompileException(
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Span,
                    diagnostic.RuleName
                );
            }
            throw new CddlCompileException(
                "CDDL_COMPILE_FAILED",
                "The CDDL document could not be compiled.",
                null,
                null
            );
        }
        return result.Schema;
    }

    /// <summary>Compiles a parsed document and returns recoverable diagnostics.</summary>
    /// <param name="document">The parsed source document.</param>
    /// <param name="options">The compilation policies and limits.</param>
    /// <returns>The exception-less compilation result.</returns>
    public CddlCompileResult TryCompileSchema(
        CddlDocument document,
        CddlCompileOptions options)
    {
        if (options == null)
        {
            CddlCompileResult missingOptions = new CddlCompileResult();
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Code = "CDDL_COMPILE_OPTIONS_MISSING";
            diagnostic.Message = "CDDL compilation options are required.";
            missingOptions.Diagnostics = [diagnostic];
            return missingOptions;
        }
        if (document == null)
        {
            CddlCompileResult missingDocument = new CddlCompileResult();
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Code = "CDDL_COMPILE_DOCUMENT_MISSING";
            diagnostic.Message = "A parsed CDDL document is required.";
            missingDocument.Diagnostics = [diagnostic];
            missingDocument.Options = options;
            return missingDocument;
        }
        if (options.ControlRegistrations == null)
        {
            return CreateOptionsFailure(
                options,
                "CDDL_COMPILE_CONTROL_REGISTRATIONS_MISSING",
                "The control-registration collection is required."
            );
        }
        CddlCompileOptions snapshot = CloneOptions(options);
        int unknownPolicy = (int)snapshot.UnknownControlPolicy;
        if (unknownPolicy < (int)CddlUnknownControlPolicy.Reject
            || unknownPolicy > (int)CddlUnknownControlPolicy.Ignore)
        {
            return CreateOptionsFailure(
                snapshot,
                "CDDL_COMPILE_UNKNOWN_CONTROL_POLICY_INVALID",
                "The unknown-control compilation policy is not defined."
            );
        }
        CddlControlLookup controls = new CddlControlLookup(
            snapshot.ControlRegistrations.ToArray()
        );
        if (controls.ErrorMessage != null)
        {
            return CreateOptionsFailure(
                snapshot,
                "CDDL_COMPILE_CONTROL_REGISTRATION_INVALID",
                controls.ErrorMessage
            );
        }
        return new CddlCompilationPipeline(
            document,
            snapshot
        ).CompileDocument();
    }

    internal CddlCompileOptions CloneOptions(CddlCompileOptions source)
    {
        CddlCompileOptions options = new CddlCompileOptions();
        options.IncludeStandardPrelude = source.IncludeStandardPrelude;
        options.RootRuleName = source.RootRuleName;
        options.RuleResolver = source.RuleResolver;
        options.UnknownControlPolicy = source.UnknownControlPolicy;
        options.MaxDiagnostics = source.MaxDiagnostics;
        options.MaxNodes = source.MaxNodes;
        options.MaxGenericInstantiations = source.MaxGenericInstantiations;
        options.MaxReferenceDepth = source.MaxReferenceDepth;
        options.ControlRegistrations =
            new System.Collections.Generic.List<CddlControlRegistration>();
        int index = 0;
        while (index < source.ControlRegistrations.Count)
        {
            CddlControlRegistration? registration =
                source.ControlRegistrations[index];
            if (registration != null)
            {
                CddlControlRegistration copy = new CddlControlRegistration();
                copy.Name = registration.Name;
                copy.Handler = registration.Handler;
                copy.IsEnabled = registration.IsEnabled;
                options.ControlRegistrations.Add(copy);
            }
            else
            {
                options.ControlRegistrations.Add(
                    new CddlControlRegistration()
                );
            }
            index = index + 1;
        }
        return options;
    }

    internal CddlCompileResult CreateOptionsFailure(
        CddlCompileOptions options,
        string code,
        string message)
    {
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        CddlCompileResult result = new CddlCompileResult();
        result.Options = options;
        result.Diagnostics = [diagnostic];
        return result;
    }
}
