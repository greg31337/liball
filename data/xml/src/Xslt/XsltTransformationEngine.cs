using System;
using System.Collections.Generic;

namespace xml;

internal enum XsltInvocationKind
{
    ApplyTemplates = 0,
    NamedTemplate = 1,
    Function = 2
}

internal class XsltPrincipalDestinationState
{
    internal XmlOutput? Output;
    internal string? BaseUri;
    internal XmlSerializationOptions SerializationOptions;
    internal XmlOutputWriter? Writer;
    internal XsltResultDisposition Disposition;

    internal XsltPrincipalDestinationState(XmlOutput? output, string? baseUri, XmlSerializationOptions serializationOptions, XmlOutputWriter? writer, XsltResultDisposition disposition)
    {
        Output = output;
        BaseUri = baseUri;
        SerializationOptions = serializationOptions;
        Writer = writer;
        Disposition = disposition;
    }
}

internal class XsltAccumulatorCopyMapping
{
    internal XmlNode Source;
    internal XmlNode Copy;

    internal XsltAccumulatorCopyMapping(XmlNode source, XmlNode copy)
    {
        Source = source;
        Copy = copy;
    }
}

internal class XsltTransformationState
{
    internal XsltExecutable Executable;
    internal XsltStylesheetPlan Stylesheet;
    internal XsltTransformOptions Options;
    internal XsltInvocationKind InvocationKind;
    internal XdmSequence InitialMatchSelection;
    internal XdmSequence InitialContextItem;
    internal XdmSequence[] FunctionParameters;
    internal XsltParameter[] InitialTemplateParameters;
    internal XsltTemplatePlan? InitialTemplate;
    internal XsltFunctionPlan? InitialFunction;
    internal XsltModePlan? InitialMode;
    internal string? InitialModeName;
    internal XdmSequence GlobalContextItem;
    internal XsltParameter[] Parameters;
    internal DateTimeOffset CurrentDateTime;
    internal XPathDynamicContext DynamicContext;
    internal XPathResourceRegistry DocumentPool;
    internal XsltPrincipalDestinationState PrincipalDestination;
    internal XsltVariableRuntime? VariableRuntime;
    internal XsltVariableFrame? InitialVariableFrame;
    internal XsltTemplateRuntime? TemplateRuntime;
    internal XsltEvaluateRuntime? EvaluateRuntime;
    internal XsltKeyRuntime? KeyRuntime;
    internal XsltAccumulatorRuntime? AccumulatorRuntime;
    internal XsltFunctionRuntime? FunctionRuntime;
    internal XsltResultDocumentRuntime? ResultDocumentRuntime;
    internal XsltStreamedNodeRuntime? StreamedNodeRuntime;
    internal XsltStreamingRuntime? StreamingRuntime;
    internal List<XsltAccumulatorCopyMapping> AccumulatorCopies;
    internal XsltTransformResult Result;

    internal XsltTransformationState(XsltExecutable executable, XsltStylesheetPlan stylesheet, XsltTransformOptions options, XdmSequence empty, XPathResourceRegistry documentPool, XsltPrincipalDestinationState principalDestination)
    {
        Executable = executable;
        Stylesheet = stylesheet;
        Options = options;
        InvocationKind = XsltInvocationKind.ApplyTemplates;
        InitialMatchSelection = empty;
        InitialContextItem = empty;
        FunctionParameters = [];
        InitialTemplateParameters = [];
        InitialTemplate = null;
        InitialFunction = null;
        InitialMode = null;
        InitialModeName = null;
        GlobalContextItem = empty;
        Parameters = [];
        CurrentDateTime = default(DateTimeOffset);
        DynamicContext = new XPathDynamicContext();
        DocumentPool = documentPool;
        PrincipalDestination = principalDestination;
        VariableRuntime = null;
        InitialVariableFrame = null;
        TemplateRuntime = null;
        EvaluateRuntime = null;
        KeyRuntime = null;
        AccumulatorRuntime = null;
        FunctionRuntime = null;
        ResultDocumentRuntime = null;
        StreamedNodeRuntime = null;
        StreamingRuntime = null;
        AccumulatorCopies = new List<XsltAccumulatorCopyMapping>();
        Result = new XsltTransformResult();
    }
}

internal class XsltTransformationEngine
{
    internal XsltTransformationState InitializeXsltTransformation(XsltExecutable executable, XsltTransformOptions options)
    {
        Guard.NotNull(executable, "executable");
        Guard.NotNull(options, "options");
        XsltStylesheetPlan? stylesheet = executable.CompiledPlan == null ? executable.Plan as XsltStylesheetPlan : executable.CompiledPlan;
        if (stylesheet == null || !stylesheet.IsImmutable || executable.CompiledPlan == null && !executable.IsImmutable)
        {
            throw new ArgumentException("An XSLT transformation requires an immutable compiled executable.", "executable");
        }

        if (options.Executable != null && !object.ReferenceEquals(options.Executable, executable))
        {
            throw new ArgumentException("The transformation options reference a different executable.", "options");
        }

        ValidateXsltTransformationEnums(options);
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmSequence empty = sequences.CreateEmptyXdmSequence();
        XdmSequence globalContext = PrepareXsltGlobalContextItem(stylesheet, options, sequences);
        XsltParameter[] parameters = CopyXsltRuntimeParameters(options.Parameters, sequences);
        XsltParameter[] initialTemplateParameters = options.InitialTemplateParameters == null ? parameters : CopyXsltRuntimeParameters(options.InitialTemplateParameters, sequences);
        XdmSequence[] functionParameters = CopyXsltRuntimeSequences(options.FunctionParameters);
        XmlSecurityLimits limits = CopyXsltRuntimeSecurityLimits(options.SecurityLimits);
        XsltCompileOptions? compiledOptions = executable.CompiledOptions == null ? executable.CompileOptions : executable.CompiledOptions;
        XmlResolverPolicy policy = CopyXsltRuntimeResolverPolicy(compiledOptions == null ? null : compiledOptions.ResolverPolicy);
        int maximumResources = limits.MaximumExternalResources > 0 ? limits.MaximumExternalResources : 64;
        XPathResourceRegistry documentPool = new XPathResourceRegistry(options.CollectionResolver, policy, maximumResources, true);
        documentPool.ResourceResolver = options.ResourceResolver == null ? new XmlDefaultResolver() : options.ResourceResolver;
        documentPool.MaximumResourceBytes = limits.MaximumResourceBytes > 0 ? limits.MaximumResourceBytes : 67108864;
        XsltPrincipalDestinationState destination = CreateXsltPrincipalDestination(stylesheet, options);
        XsltTransformOptions snapshot = CopyXsltTransformationOptions(executable, options, globalContext, parameters, limits, destination);
        XsltTransformationState state = new XsltTransformationState(executable, stylesheet, snapshot, empty, documentPool, destination);
        if (stylesheet.Conformance.Streaming)
        {
            state.StreamedNodeRuntime = new XsltStreamedNodeRuntime(limits.MaximumNodeCount);
        }

        state.GlobalContextItem = globalContext;
        state.Parameters = parameters;
        state.InitialTemplateParameters = initialTemplateParameters;
        state.FunctionParameters = functionParameters;
        state.CurrentDateTime = DateTimeOffset.Now;
        state.DynamicContext = CreateXsltDynamicContext(state, limits, policy, options.ResourceResolver);
        state.ResultDocumentRuntime = new XsltResultDocumentRuntime(state, options.ResultDocumentResolver == null ? new XsltResultDocumentCollector() : options.ResultDocumentResolver);
        SelectXsltInitialInvocation(state, options, sequences);
        RegisterXsltInitialDocuments(state);
        state.VariableRuntime = new XsltVariableRuntime(state);
        state.TemplateRuntime = new XsltTemplateRuntime(state);
        state.EvaluateRuntime = new XsltEvaluateRuntime(state);
        state.KeyRuntime = new XsltKeyRuntime(state);
        state.DynamicContext.XsltKeys = state.KeyRuntime;
        state.AccumulatorRuntime = new XsltAccumulatorRuntime(state);
        state.DynamicContext.XsltAccumulators = state.AccumulatorRuntime;
        state.FunctionRuntime = new XsltFunctionRuntime(state);
        state.DynamicContext.XsltFunctions = state.FunctionRuntime;
        if (state.StreamedNodeRuntime != null)
        {
            state.StreamingRuntime = new XsltStreamingRuntime(state);
        }

        if (state.InitialTemplate != null)
        {
            state.InitialVariableFrame = state.VariableRuntime.BindXsltTemplateParameters(state.InitialTemplate.Parameters, state.Parameters, null, state.DynamicContext);
        }

        state.Result = CreateXsltInitializedResult(state);
        return state;
    }

    internal void ValidateXsltTransformationEnums(XsltTransformOptions options)
    {
        if (options.RecoveryPolicy != XsltRecoveryPolicy.Strict && options.RecoveryPolicy != XsltRecoveryPolicy.Recover)
        {
            throw new ArgumentException("The XSLT recovery policy is invalid.", "options");
        }

        if (options.ResultDisposition < XsltResultDisposition.Pending || options.ResultDisposition > XsltResultDisposition.Reject)
        {
            throw new ArgumentException("The XSLT result disposition is invalid.", "options");
        }

        if (options.InitialTemplate != null && options.InitialFunction != null)
        {
            throw new ArgumentException("An XSLT invocation cannot select both an initial template and an initial function.", "options");
        }

        if (options.SourceDocument != null && options.InitialMatchSelection != null)
        {
            throw new ArgumentException("An XSLT invocation cannot supply both a source document and an initial match selection.", "options");
        }
    }

    internal XdmSequence PrepareXsltGlobalContextItem(XsltStylesheetPlan stylesheet, XsltTransformOptions options, XdmSequenceOperations sequences)
    {
        XdmSequence supplied;
        if (options.GlobalContextItem != null)
        {
            if (options.GlobalContextItem.Count > 1)
            {
                throw new ArgumentException("The global context item must be empty or a singleton.", "options");
            }

            supplied = CopyXsltRuntimeSequence(options.GlobalContextItem);
        }
        else if (options.SourceDocument != null)
        {
            XmlNode? source = options.SourceDocument.DocumentNode;
            if (source == null)
            {
                throw new ArgumentException("The principal source document has no document node.", "options");
            }

            supplied = sequences.CreateXdmNodeSequence(source);
        }
        else
        {
            supplied = sequences.CreateEmptyXdmSequence();
        }

        XsltGlobalContextItemPlan declaration = stylesheet.GlobalContextItem;
        if (declaration.Use == XsltContextItemUse.Absent)
        {
            return sequences.CreateEmptyXdmSequence();
        }

        if (declaration.Use == XsltContextItemUse.Required && supplied.Count == 0)
        {
            throw CreateXsltTypeInitializationError("The stylesheet requires a global context item.", XsltErrorCodes.XTTE0590, ReadXsltGlobalContextDeclaration(declaration));
        }

        if (supplied.Count > 0 && declaration.Type != null)
        {
            XdmSequenceType expected = new XPathFunctionTypeConverter().CreateXPathFunctionSequenceType(declaration.Type);
            if (!new XdmTypeConverter().InstanceOfXdmSequence(supplied, expected))
            {
                throw CreateXsltTypeInitializationError("The global context item does not match its declared type.", XsltErrorCodes.XTTE0590, ReadXsltGlobalContextDeclaration(declaration));
            }
        }

        return supplied;
    }

    internal XmlNode? ReadXsltGlobalContextDeclaration(XsltGlobalContextItemPlan declaration)
    {
        return declaration.Declarations.Length == 0 ? null : declaration.Declarations[0].Declaration;
    }

    internal XsltParameter[] CopyXsltRuntimeParameters(XsltParameter[]? supplied, XdmSequenceOperations sequences)
    {
        XsltParameter[] source = supplied == null ? [] : supplied;
        XsltParameter[] result = new XsltParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XsltParameter? parameter = source[index];
            if (parameter == null || parameter.Name == null || parameter.Name.LocalName == null || parameter.Name.LocalName.Length == 0 || parameter.Value == null)
            {
                throw new ArgumentException("Every XSLT parameter requires a name and value.", "options");
            }

            if (parameter.IsStatic)
            {
                throw new ArgumentException("Static XSLT parameters must be supplied during compilation.", "options");
            }

            int earlier = 0;
            while (earlier < index)
            {
                XsltParameter previous = result[earlier];
                if (previous.IsTunnel == parameter.IsTunnel && EqualXsltRuntimeNames(previous.Name, parameter.Name))
                {
                    throw new ArgumentException("XSLT parameter names must be unique within each tunnel category.", "options");
                }

                earlier = earlier + 1;
            }

            XsltParameter copy = new XsltParameter();
            copy.Name = CopyXsltRuntimeName(parameter.Name);
            copy.Value = CopyXsltRuntimeSequence(parameter.Value);
            copy.DeclaredType = parameter.DeclaredType;
            copy.DeclaredOccurrence = parameter.DeclaredOccurrence;
            copy.IsTunnel = parameter.IsTunnel;
            copy.IsStatic = false;
            copy.IsRequired = parameter.IsRequired;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal bool EqualXsltRuntimeNames(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return left.LocalName == right.LocalName && leftNamespace == rightNamespace;
    }

    internal void SelectXsltInitialInvocation(XsltTransformationState state, XsltTransformOptions source, XdmSequenceOperations sequences)
    {
        if (source.InitialFunction != null)
        {
            if (source.InitialMode != null)
            {
                throw CreateXsltDynamicInitializationError("An initial mode cannot be used with an initial function.", XsltErrorCodes.XTDE0044, null);
            }

            state.InvocationKind = XsltInvocationKind.Function;
            state.InitialFunction = FindXsltInitialFunction(state.Stylesheet, source.InitialFunction, state.FunctionParameters.Length);
            state.InitialContextItem = sequences.CreateEmptyXdmSequence();
            return;
        }

        if (source.InitialTemplate != null)
        {
            if (source.InitialMode != null)
            {
                throw CreateXsltDynamicInitializationError("An initial mode cannot be used with an initial named template.", XsltErrorCodes.XTDE0044, null);
            }

            state.InvocationKind = XsltInvocationKind.NamedTemplate;
            state.InitialTemplate = FindXsltInitialTemplate(state.Stylesheet, source.InitialTemplate);
            state.InitialContextItem = SelectXsltNamedTemplateContext(state.InitialTemplate, state.GlobalContextItem, sequences);
            return;
        }

        if (source.SourceDocument == null && source.InitialMatchSelection == null && state.GlobalContextItem.Count == 0)
        {
            XmlName defaultName = new XmlName();
            defaultName.LocalName = "initial-template";
            defaultName.NamespaceUri = XsltNamespaces.Xslt;
            state.InvocationKind = XsltInvocationKind.NamedTemplate;
            state.InitialTemplate = FindXsltInitialTemplate(state.Stylesheet, defaultName);
            state.InitialContextItem = SelectXsltNamedTemplateContext(state.InitialTemplate, state.GlobalContextItem, sequences);
            return;
        }

        state.InvocationKind = XsltInvocationKind.ApplyTemplates;
        state.InitialMatchSelection = source.InitialMatchSelection == null ? source.SourceDocument == null ? state.GlobalContextItem : sequences.CreateXdmNodeSequence(source.SourceDocument.DocumentNode == null ? throw new ArgumentException("The principal source document has no document node.", "options") : source.SourceDocument.DocumentNode) : CopyXsltRuntimeSequence(source.InitialMatchSelection);
        string modeName = source.InitialMode == null ? state.Stylesheet.InitialMode : CreateXsltRuntimeExpandedName(source.InitialMode);
        state.InitialModeName = modeName;
        state.InitialMode = FindXsltInitialMode(state.Stylesheet, modeName);
    }

    internal XsltTemplatePlan FindXsltInitialTemplate(XsltStylesheetPlan stylesheet, XmlName requested)
    {
        string name = CreateXsltRuntimeExpandedName(requested);
        XsltPackagePlan? package = stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int index = 0;
        while (index < components.Length)
        {
            XsltComponent component = components[index];
            if (component.Kind == XsltComponentKind.Template && component.Name == name && IsXsltPublicRuntimeComponent(component) && component.Template != null && !component.Template.IsAbstract)
            {
                return component.Template;
            }

            index = index + 1;
        }

        throw CreateXsltDynamicInitializationError("The requested initial named template is not public or does not exist.", XsltErrorCodes.XTDE0040, null);
    }

    internal XsltFunctionPlan FindXsltInitialFunction(XsltStylesheetPlan stylesheet, XmlName requested, int arity)
    {
        string name = CreateXsltRuntimeExpandedName(requested);
        XsltPackagePlan? package = stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int index = 0;
        while (index < components.Length)
        {
            XsltComponent component = components[index];
            if (component.Kind == XsltComponentKind.Function && component.Name == name && component.Arity == arity && IsXsltPublicRuntimeComponent(component) && component.Function != null && !component.Function.IsAbstract)
            {
                return component.Function;
            }

            index = index + 1;
        }

        throw CreateXsltDynamicInitializationError("The requested initial function and arity are not public or do not exist.", XsltErrorCodes.XTDE0041, null);
    }

    internal XsltModePlan FindXsltInitialMode(XsltStylesheetPlan stylesheet, string name)
    {
        int index = 0;
        while (index < stylesheet.Modes.Length)
        {
            XsltModePlan mode = stylesheet.Modes[index];
            if (mode.Name == name && IsXsltEligibleInitialMode(stylesheet, mode))
            {
                return mode;
            }

            index = index + 1;
        }

        throw CreateXsltDynamicInitializationError("The requested initial mode is not eligible or does not exist.", XsltErrorCodes.XTDE0045, null);
    }

    internal bool IsXsltEligibleInitialMode(XsltStylesheetPlan stylesheet, XsltModePlan mode)
    {
        if (mode.IsUnnamed || mode.Name == stylesheet.InitialMode || !stylesheet.DeclaredModes && mode.IsImplicit)
        {
            return true;
        }

        XsltComponent? component = mode.Component;
        return component != null && IsXsltPublicRuntimeComponent(component);
    }

    internal bool IsXsltPublicRuntimeComponent(XsltComponent component)
    {
        return component.Visibility == XsltComponentVisibility.Public || component.Visibility == XsltComponentVisibility.Final;
    }

    internal XdmSequence SelectXsltNamedTemplateContext(XsltTemplatePlan template, XdmSequence globalContext, XdmSequenceOperations sequences)
    {
        XsltContextItemPlan? context = template.ContextItem;
        if (context != null && context.Use == XsltContextItemUse.Absent)
        {
            return sequences.CreateEmptyXdmSequence();
        }

        if (context != null && context.Use == XsltContextItemUse.Required && globalContext.Count == 0)
        {
            throw CreateXsltTypeInitializationError("The initial named template requires a context item.", XsltErrorCodes.XTTE3090, context.Declaration);
        }

        return globalContext;
    }

    internal string CreateXsltRuntimeExpandedName(XmlName name)
    {
        if (name.LocalName == null || name.LocalName.Length == 0)
        {
            throw new ArgumentException("An initial XSLT component name requires a local name.", "options");
        }

        if (name.LocalName == "#unnamed")
        {
            return "#unnamed";
        }

        return "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + name.LocalName;
    }

    internal XsltPrincipalDestinationState CreateXsltPrincipalDestination(XsltStylesheetPlan stylesheet, XsltTransformOptions options)
    {
        XmlSerializationOptions source = options.SerializationOptions == null ? FindXsltDefaultSerializationOptions(stylesheet) : options.SerializationOptions;
        XmlSerializationOptions serialization = CopyXsltRuntimeSerializationOptions(source);
        new SerializationParameterValidator().ValidateSerializationParameters(serialization);
        XmlOutput? output = options.PrincipalOutput == null ? null : CopyXsltRuntimeOutput(options.PrincipalOutput);
        XmlOutputWriter? writer = output == null ? null : new XmlOutputWriter(output, serialization.Encoding, serialization.ByteOrderMark, 4096);
        string? baseUri = options.BaseOutputUri == null ? output == null ? null : output.BaseUri : options.BaseOutputUri;
        return new XsltPrincipalDestinationState(output, baseUri, serialization, writer, options.ResultDisposition);
    }

    internal XmlSerializationOptions FindXsltDefaultSerializationOptions(XsltStylesheetPlan stylesheet)
    {
        int index = 0;
        while (index < stylesheet.Outputs.Length)
        {
            if (stylesheet.Outputs[index].Name == "#default")
            {
                return stylesheet.Outputs[index].Options;
            }

            index = index + 1;
        }

        XsltOutputFormatPlan defaults = new XsltOutputFormatPlan(null, "#default");
        return defaults.Options;
    }

    internal XsltTransformOptions CopyXsltTransformationOptions(XsltExecutable executable, XsltTransformOptions source, XdmSequence globalContext, XsltParameter[] parameters, XmlSecurityLimits limits, XsltPrincipalDestinationState destination)
    {
        XsltTransformOptions result = new XsltTransformOptions();
        result.Executable = executable;
        result.SourceDocument = source.SourceDocument;
        result.GlobalContextItem = globalContext;
        result.InitialMatchSelection = source.InitialMatchSelection == null ? null : CopyXsltRuntimeSequence(source.InitialMatchSelection);
        result.InitialMode = CopyXsltRuntimeOptionalName(source.InitialMode);
        result.InitialTemplate = CopyXsltRuntimeOptionalName(source.InitialTemplate);
        result.InitialFunction = CopyXsltRuntimeOptionalName(source.InitialFunction);
        result.FunctionParameters = CopyXsltRuntimeSequences(source.FunctionParameters);
        result.Parameters = parameters;
        result.InitialTemplateParameters = source.InitialTemplateParameters == null ? parameters : CopyXsltRuntimeParameters(source.InitialTemplateParameters, new XdmSequenceOperations());
        result.PrincipalOutput = destination.Output;
        result.SerializationOptions = destination.SerializationOptions;
        result.ResultDocumentResolver = source.ResultDocumentResolver;
        result.MessageReceiver = source.MessageReceiver;
        result.TraceReceiver = source.TraceReceiver;
        result.BaseOutputUri = destination.BaseUri;
        result.RecoveryPolicy = source.RecoveryPolicy;
        result.ResourceResolver = source.ResourceResolver;
        result.CollectionResolver = source.CollectionResolver;
        result.XPathTraceReceiver = source.XPathTraceReceiver;
        result.Cancellation = source.Cancellation;
        result.SecurityLimits = limits;
        result.ResultDisposition = source.ResultDisposition;
        result.EnableTracing = source.EnableTracing;
        result.EnableAssertions = source.EnableAssertions;
        return result;
    }

    internal XPathDynamicContext CreateXsltDynamicContext(XsltTransformationState state, XmlSecurityLimits limits, XmlResolverPolicy policy, IXmlResourceResolver? resolver)
    {
        XPathDynamicContext result = new XPathDynamicContext();
        result.ContextItem = state.GlobalContextItem;
        result.Position = state.GlobalContextItem.Count == 0 ? 0 : 1;
        result.Size = state.GlobalContextItem.Count;
        result.BaseUri = state.Stylesheet.PrincipalModule.BaseUri;
        result.ImplicitTimezoneMinutes = (int)state.CurrentDateTime.Offset.TotalMinutes;
        result.CurrentDateTime = state.CurrentDateTime;
        result.ResolverPolicy = policy;
        result.ResourceResolver = resolver == null ? new XmlDefaultResolver() : resolver;
        result.CollectionResolver = state.Options.CollectionResolver;
        result.TraceReceiver = state.Options.XPathTraceReceiver;
        result.Cancellation = state.Options.Cancellation;
        result.SecurityLimits = limits;
        result.XsltCurrentItem = state.GlobalContextItem;
        result.XsltCurrentOutputUri = state.PrincipalDestination.BaseUri;
        result.XsltDocuments = state.DocumentPool;
        result.XsltStreamedNodes = state.StreamedNodeRuntime;
        result.ExtensionState = new XmlExtensionRuntimeState();
        return result;
    }

    internal void RegisterXsltInitialDocuments(XsltTransformationState state)
    {
        if (state.Options.SourceDocument != null)
        {
            RegisterXsltRuntimeDocument(state.DocumentPool, state.Options.SourceDocument);
        }

        if (state.GlobalContextItem.Count == 1)
        {
            XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(state.GlobalContextItem, 0);
            if (item.Node != null && item.Node.OwnerDocument != null && !object.ReferenceEquals(item.Node.OwnerDocument, state.Options.SourceDocument))
            {
                RegisterXsltRuntimeDocument(state.DocumentPool, item.Node.OwnerDocument);
            }
        }
    }

    internal void RegisterXsltRuntimeDocument(XPathResourceRegistry pool, XmlDocument document)
    {
        string? uri = document.DocumentUri == null ? document.BaseUri : document.DocumentUri;
        if (uri != null && uri.Length > 0 && UriTools.HasUriScheme(uri))
        {
            pool.RegisterXPathDocumentResource(uri, document);
        }
    }

    internal XsltTransformResult CreateXsltInitializedResult(XsltTransformationState state)
    {
        XsltTransformResult result = new XsltTransformResult();
        result.Executable = state.Executable;
        result.Options = state.Options;
        result.PrincipalValue = new XdmSequenceOperations().CreateEmptyXdmSequence();
        result.PrincipalDocument = null;
        result.SecondaryResults = [];
        result.Messages = [];
        result.TraceEvents = [];
        result.Diagnostics = [];
        result.Disposition = state.PrincipalDestination.Disposition;
        result.Succeeded = false;
        return result;
    }

    internal void CompleteXsltPrincipalResult(XsltTransformationState state, XdmSequence value)
    {
        XdmSequence frozen = new XdmSequenceOperations().FreezeXdmSequenceStorage(value);
        state.Result.PrincipalValue = frozen;
        state.Result.PrincipalDocument = FindXsltPrincipalResultDocument(frozen);
        if (state.PrincipalDestination.Output != null)
        {
            state.PrincipalDestination.SerializationOptions.ExtensionState = state.DynamicContext.ExtensionState;
            bool streamed = state.StreamingRuntime != null && state.StreamingRuntime.CompleteXsltStreamingResult(frozen);
            if (!streamed)
            {
                new XmlSerializer().SerializeXdmSequence(frozen, state.PrincipalDestination.Output, state.PrincipalDestination.SerializationOptions);
            }
        }

        state.Result.Succeeded = true;
    }

    internal XmlDocument? FindXsltPrincipalResultDocument(XdmSequence value)
    {
        if (value.Count != 1) return null;
        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(value, 0);
        XmlNode? node = item.Node;
        return node == null || node.Kind != XmlNodeKind.Document ? null : node.OwnerDocument;
    }

    internal XdmSequence CopyXsltRuntimeSequence(XdmSequence source)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        sequences.ValidateXdmSequenceStorage(source);
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < source.Count)
        {
            XdmSequenceItem item = sequences.ReadXdmSequenceItem(source, index);
            if (item.Node != null)
            {
                builder.AddXdmNode(item.Node);
            }
            else if (item.AtomicValue != null)
            {
                builder.AddXdmAtomicValue(new XdmFactory().FreezeXdmAtomicValue(item.AtomicValue));
            }
            else if (item.Map != null)
            {
                builder.AddXdmMap(CopyXsltRuntimeMap(item.Map));
            }
            else if (item.Array != null)
            {
                builder.AddXdmArray(CopyXsltRuntimeArray(item.Array));
            }
            else if (item.Function != null)
            {
                builder.AddXdmFunction(CopyXsltRuntimeFunction(item.Function));
            }

            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence[] CopyXsltRuntimeSequences(XdmSequence[]? supplied)
    {
        XdmSequence[] source = supplied == null ? [] : supplied;
        XdmSequence[] result = new XdmSequence[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XdmSequence? sequence = source[index];
            if (sequence == null)
            {
                throw new ArgumentException("Initial function parameters cannot contain null sequences.", "options");
            }

            result[index] = CopyXsltRuntimeSequence(sequence);
            index = index + 1;
        }

        return result;
    }

    internal XdmMapValue CopyXsltRuntimeMap(XdmMapValue source)
    {
        new XdmMapOperations().ValidateXdmMapStorage(source);
        XdmMapEntry[] entries = source.Entries == null ? [] : source.Entries;
        XdmMapEntry[] copied = new XdmMapEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry entry = entries[index];
            if (entry.Key == null || entry.Value == null)
            {
                throw new ArgumentException("An XDM map entry requires a key and value.", "options");
            }

            XdmMapEntry copy = new XdmMapEntry();
            copy.Key = new XdmFactory().FreezeXdmAtomicValue(entry.Key);
            copy.Value = CopyXsltRuntimeSequence(entry.Value);
            copied[index] = copy;
            index = index + 1;
        }

        XdmMapValue result = new XdmMapValue();
        result.Entries = copied;
        result.Count = copied.Length;
        result.IsImmutable = true;
        return result;
    }

    internal XdmArrayValue CopyXsltRuntimeArray(XdmArrayValue source)
    {
        new XdmArrayOperations().ValidateXdmArrayStorage(source);
        XdmSequence[] members = source.Members == null ? [] : source.Members;
        XdmSequence[] copied = new XdmSequence[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            copied[index] = CopyXsltRuntimeSequence(members[index]);
            index = index + 1;
        }

        XdmArrayValue result = new XdmArrayValue();
        result.Members = copied;
        result.Count = copied.Length;
        result.IsImmutable = true;
        return result;
    }

    internal XdmFunctionItem CopyXsltRuntimeFunction(XdmFunctionItem source)
    {
        XdmFunctionOperations operations = new XdmFunctionOperations();
        operations.ValidateXdmFunctionItem(source);
        XdmFunctionItem result = new XdmFunctionItem();
        result.Name = CopyXsltRuntimeOptionalName(source.Name);
        result.Arity = source.Arity;
        result.ParameterTypes = operations.CopyXdmFunctionTypes(source.ParameterTypes);
        result.ParameterOccurrences = operations.CopyXdmFunctionOccurrences(source.ParameterOccurrences);
        result.ResultType = source.ResultType;
        result.ResultOccurrence = source.ResultOccurrence;
        result.BindingKey = source.BindingKey;
        result.Implementation = source.Implementation;
        result.ClosureVariables = CopyXsltRuntimeClosureVariables(source.ClosureVariables);
        result.MapValue = source.MapValue == null ? null : CopyXsltRuntimeMap(source.MapValue);
        result.ArrayValue = source.ArrayValue == null ? null : CopyXsltRuntimeArray(source.ArrayValue);
        result.IsDeterministic = source.IsDeterministic;
        result.IsMap = source.IsMap;
        result.IsArray = source.IsArray;
        result.IsImmutable = true;
        return result;
    }

    internal XPathVariableBinding[] CopyXsltRuntimeClosureVariables(XPathVariableBinding[]? source)
    {
        XPathVariableBinding[] variables = source == null ? [] : source;
        XPathVariableBinding[] result = new XPathVariableBinding[variables.Length];
        int index = 0;
        while (index < variables.Length)
        {
            XPathVariableBinding? variable = variables[index];
            if (variable == null)
            {
                throw new ArgumentException("A function closure cannot contain a null variable.", "options");
            }

            XPathVariableBinding copy = new XPathVariableBinding();
            copy.Name = CopyXsltRuntimeOptionalName(variable.Name);
            copy.Value = variable.Value == null ? null : CopyXsltRuntimeSequence(variable.Value);
            copy.DeclaredType = variable.DeclaredType;
            copy.DeclaredOccurrence = variable.DeclaredOccurrence;
            copy.IsExternal = variable.IsExternal;
            copy.IsRequired = variable.IsRequired;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlName CopyXsltRuntimeName(XmlName source)
    {
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal XmlName? CopyXsltRuntimeOptionalName(XmlName? source)
    {
        return source == null ? null : CopyXsltRuntimeName(source);
    }

    internal XmlName[]? CopyXsltRuntimeNames(XmlName[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName[] result = new XmlName[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlName? name = source[index];
            if (name == null)
            {
                throw new ArgumentException("A serialization name array cannot contain null entries.", "options");
            }

            result[index] = CopyXsltRuntimeName(name);
            index = index + 1;
        }

        return result;
    }

    internal XmlCharacterMapEntry[]? CopyXsltRuntimeCharacterMaps(XmlCharacterMapEntry[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlCharacterMapEntry[] result = new XmlCharacterMapEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlCharacterMapEntry? entry = source[index];
            if (entry == null)
            {
                throw new ArgumentException("A character map cannot contain null entries.", "options");
            }

            XmlCharacterMapEntry copy = new XmlCharacterMapEntry();
            copy.CodePoint = entry.CodePoint;
            copy.Replacement = entry.Replacement;
            copy.MapName = CopyXsltRuntimeOptionalName(entry.MapName);
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlSerializationOptions CopyXsltRuntimeSerializationOptions(XmlSerializationOptions source)
    {
        XmlSerializationOptions result = new XmlSerializationOptions();
        result.Method = source.Method;
        result.ExtensionMethodName = CopyXsltRuntimeOptionalName(source.ExtensionMethodName);
        result.ExtensionMethods = CopyXsltRuntimeSerializationMethods(source.ExtensionMethods);
        result.ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(source.ExtensionPolicy);
        result.ExtensionState = source.ExtensionState;
        result.AllowDuplicateNames = source.AllowDuplicateNames;
        result.ByteOrderMark = source.ByteOrderMark;
        result.CDataSectionElements = CopyXsltRuntimeNames(source.CDataSectionElements);
        result.DoctypePublic = source.DoctypePublic;
        result.DoctypeSystem = source.DoctypeSystem;
        result.Encoding = source.Encoding;
        result.EscapeUriAttributes = source.EscapeUriAttributes;
        result.HtmlVersion = source.HtmlVersion;
        result.IncludeContentType = source.IncludeContentType;
        result.Indent = source.Indent;
        result.ItemSeparator = source.ItemSeparator;
        result.JsonNodeOutputMethod = source.JsonNodeOutputMethod;
        result.MediaType = source.MediaType;
        result.NormalizationForm = source.NormalizationForm;
        result.OmitXmlDeclaration = source.OmitXmlDeclaration;
        result.Standalone = source.Standalone;
        result.SuppressIndentation = CopyXsltRuntimeNames(source.SuppressIndentation);
        result.UndeclarePrefixes = source.UndeclarePrefixes;
        result.UseCharacterMaps = CopyXsltRuntimeCharacterMaps(source.UseCharacterMaps);
        result.Version = source.Version;
        result.InvalidCharacterPolicy = source.InvalidCharacterPolicy;
        result.InvalidCharacterReplacement = source.InvalidCharacterReplacement;
        result.NewLine = source.NewLine;
        return result;
    }

    internal XmlSerializationMethodBinding[]? CopyXsltRuntimeSerializationMethods(XmlSerializationMethodBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlSerializationMethodBinding[] result = new XmlSerializationMethodBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlSerializationMethodBinding item = source[index];
            XmlSerializationMethodBinding copy = new XmlSerializationMethodBinding();
            copy.Name = CopyXsltRuntimeOptionalName(item.Name);
            copy.Implementation = item.Implementation;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlOutput CopyXsltRuntimeOutput(XmlOutput source)
    {
        XmlOutput result = new XmlOutput();
        result.Kind = source.Kind;
        result.ByteStream = source.ByteStream;
        result.TextWriter = source.TextWriter;
        result.ByteSink = source.ByteSink;
        result.BaseUri = source.BaseUri;
        result.MediaType = source.MediaType;
        result.EncodingName = source.EncodingName;
        result.Encoding = source.Encoding;
        result.OwnsResource = source.OwnsResource;
        return result;
    }

    internal XmlSecurityLimits CopyXsltRuntimeSecurityLimits(XmlSecurityLimits? supplied)
    {
        XmlSecurityLimits defaults = new XPathRuntimeContext(new XPathDynamicContext()).Limits;
        XmlSecurityLimits source = supplied == null ? defaults : supplied;
        XmlSecurityLimits result = new XmlSecurityLimits();
        result.MaximumDepth = source.MaximumDepth > 0 ? source.MaximumDepth : defaults.MaximumDepth;
        result.MaximumNodeCount = source.MaximumNodeCount > 0 ? source.MaximumNodeCount : defaults.MaximumNodeCount;
        result.MaximumAttributesPerElement = source.MaximumAttributesPerElement > 0 ? source.MaximumAttributesPerElement : defaults.MaximumAttributesPerElement;
        result.MaximumNameLength = source.MaximumNameLength > 0 ? source.MaximumNameLength : defaults.MaximumNameLength;
        result.MaximumTextLength = source.MaximumTextLength > 0 ? source.MaximumTextLength : defaults.MaximumTextLength;
        result.MaximumTokenLength = source.MaximumTokenLength > 0 ? source.MaximumTokenLength : defaults.MaximumTokenLength;
        result.MaximumInputCharacters = source.MaximumInputCharacters > 0 ? source.MaximumInputCharacters : defaults.MaximumInputCharacters;
        result.MaximumInputBytes = source.MaximumInputBytes > 0 ? source.MaximumInputBytes : defaults.MaximumInputBytes;
        result.MaximumDtdCharacters = source.MaximumDtdCharacters > 0 ? source.MaximumDtdCharacters : defaults.MaximumDtdCharacters;
        result.MaximumEntityDepth = source.MaximumEntityDepth > 0 ? source.MaximumEntityDepth : defaults.MaximumEntityDepth;
        result.MaximumEntityCount = source.MaximumEntityCount > 0 ? source.MaximumEntityCount : defaults.MaximumEntityCount;
        result.MaximumEntityCharacters = source.MaximumEntityCharacters > 0 ? source.MaximumEntityCharacters : defaults.MaximumEntityCharacters;
        result.MaximumEntityAmplification = source.MaximumEntityAmplification > 0 ? source.MaximumEntityAmplification : defaults.MaximumEntityAmplification;
        result.MaximumExternalResources = source.MaximumExternalResources > 0 ? source.MaximumExternalResources : defaults.MaximumExternalResources;
        result.MaximumResourceDepth = source.MaximumResourceDepth > 0 ? source.MaximumResourceDepth : defaults.MaximumResourceDepth;
        result.MaximumResourceBytes = source.MaximumResourceBytes > 0 ? source.MaximumResourceBytes : defaults.MaximumResourceBytes;
        result.CancellationPollingInterval = source.CancellationPollingInterval > 0 ? source.CancellationPollingInterval : defaults.CancellationPollingInterval;
        return result;
    }

    internal XmlResolverPolicy CopyXsltRuntimeResolverPolicy(XmlResolverPolicy? supplied)
    {
        return new XPathRuntimeContext(new XPathDynamicContext { ResolverPolicy = supplied }).ResourcePolicy;
    }

    internal XsltDynamicException CreateXsltDynamicInitializationError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return new XsltDynamicException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }

    internal XsltTypeException CreateXsltTypeInitializationError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return new XsltTypeException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }
}
