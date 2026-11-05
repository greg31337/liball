using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal class XsltFunctionInvocation
{
    internal XsltFunctionPlan Function;
    internal XsltComponent? Component;
    internal int Depth;

    internal XsltFunctionInvocation(XsltFunctionPlan function, XsltComponent? component, int depth)
    {
        Function = function;
        Component = component;
        Depth = depth;
    }
}

internal class XsltFunctionRuntime
{
    internal XsltTransformationState State;
    internal XsltVariableRuntime Variables;
    internal XsltTemplateRuntime Templates;
    internal XdmSequenceOperations Sequences;
    internal List<XsltFunctionInvocation> Invocations;
    internal int Depth;

    internal XsltFunctionRuntime(XsltTransformationState state)
    {
        State = state;
        Variables = state.VariableRuntime == null ? throw new InvalidOperationException("The variable runtime must be initialized before the function runtime.") : state.VariableRuntime;
        Templates = state.TemplateRuntime == null ? throw new InvalidOperationException("The template runtime must be initialized before the function runtime.") : state.TemplateRuntime;
        Sequences = new XdmSequenceOperations();
        Invocations = new List<XsltFunctionInvocation>();
        Depth = 0;
    }

    internal XdmSequence InvokeXsltFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext caller)
    {
        XsltComponent? component = ResolveXsltFunctionComponent(binding, caller.XsltCurrentComponent);
        XsltFunctionPlan? function = component == null ? null : component.Function;
        if (function == null)
        {
            throw Templates.CreateXsltTemplateDynamicError("The stylesheet function binding is unavailable.", XPathErrorCodes.XPDY0002, null);
        }

        return InvokeXsltFunctionPlanCore(function, component, arguments, caller);
    }

    internal XdmSequence InvokeXsltFunctionPlan(XsltFunctionPlan function, XdmSequence[] arguments, XPathDynamicContext caller)
    {
        XsltComponent? component = FindXsltFunctionComponent(function);
        return InvokeXsltFunctionPlanCore(function, component, arguments, caller);
    }

    internal XdmSequence InvokeXsltFunctionPlanCore(XsltFunctionPlan function, XsltComponent? component, XdmSequence[] arguments, XPathDynamicContext caller)
    {
        if (arguments.Length != function.Parameters.Length)
        {
            throw Templates.CreateXsltTemplateTypeError("The supplied argument count does not match the stylesheet function arity.", XPathErrorCodes.XPTY0004, function.Declaration);
        }

        XmlSecurityLimits? limits = caller.SecurityLimits;
        int maximumDepth = limits != null && limits.MaximumDepth > 0 ? limits.MaximumDepth : 256;
        if (Depth >= maximumDepth)
        {
            throw Templates.CreateXsltTemplateDynamicError("The stylesheet function recursion depth limit was exceeded.", XPathErrorCodes.FOER0000, function.Declaration);
        }

        XsltFunctionInvocation invocation = new XsltFunctionInvocation(function, component, Depth);
        Invocations.Add(invocation);
        Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.EnterComponent, component, null, function.ExpandedName, function.Declaration.Range, caller.ContextItem, Depth);
        Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.EnterFunction, component, null, function.ExpandedName, function.Declaration.Range, caller.ContextItem, Depth);
        Depth = Depth + 1;
        Task<XdmSequence> pending = Task.Run(() => EvaluateXsltFunctionInvocation(function, component, arguments, caller));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            Depth = Depth - 1;
            Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.ExitFunction, component, null, function.ExpandedName, function.Declaration.Range, caller.ContextItem, Depth);
            Templates.RecordXsltRuntimeTrace(XsltTraceEventKind.ExitComponent, component, null, function.ExpandedName, function.Declaration.Range, caller.ContextItem, Depth);
            Invocations.RemoveAt(Invocations.Count - 1);
            return pending.Result;
        }

        Exception failure = Templates.ReadXsltInvocationFailure(pending.Exception);
        AttachXsltFunctionErrorContext(failure);
        Depth = Depth - 1;
        Invocations.RemoveAt(Invocations.Count - 1);
        throw failure;
    }

    internal XdmSequence EvaluateXsltFunctionInvocation(XsltFunctionPlan function, XsltComponent? component, XdmSequence[] arguments, XPathDynamicContext caller)
    {
        XPathDynamicContext context = CreateXsltFunctionContext(caller, component);
        XsltVariableFrame frame = new XsltVariableFrame(null, [], context);
        int index = 0;
        while (index < function.Parameters.Length)
        {
            XsltVariablePlan parameter = function.Parameters[index];
            XdmSequence value = ConvertXsltFunctionValue(arguments[index], parameter.SequenceType, function, false);
            frame.Bindings.Add(new XsltRuntimeVariableBinding(parameter, value, false));
            index = index + 1;
        }

        XdmSequence empty = Sequences.CreateEmptyXdmSequence();
        XsltTemplateInvocation templateInvocation = new XsltTemplateInvocation(XsltTemplateDispatchKind.Named, null, null, State.Stylesheet.InitialMode, empty, 0, 0, Templates.Depth);
        templateInvocation.Frame = frame;
        XdmSequence result = Templates.SequenceRuntime.EvaluateXsltSequenceConstructor(function.Body, templateInvocation);
        return ConvertXsltFunctionValue(result, function.ReturnType, function, true);
    }

    internal void AttachXsltFunctionErrorContext(Exception failure)
    {
        XmlDiagnostic? diagnostic = Templates.ReadXsltExceptionDiagnostic(failure);
        if (diagnostic == null) return;
        string[] functions = CreateXsltFunctionStack();
        string[] existingFunctions = diagnostic.FunctionStack == null ? [] : diagnostic.FunctionStack;
        if (functions.Length > existingFunctions.Length) diagnostic.FunctionStack = functions;
        string[] templates = Templates.CreateXsltTemplateComponentStack();
        string[] components = new string[templates.Length + Depth];
        Array.Copy(templates, components, templates.Length);
        int depth = 0;
        while (depth < Depth)
        {
            int index = Invocations.Count - 1;
            components[templates.Length + depth] = "function";
            while (index >= 0)
            {
                XsltFunctionInvocation invocation = Invocations[index];
                if (invocation.Depth == depth)
                {
                    components[templates.Length + depth] = invocation.Component == null ? invocation.Function.ExpandedName + "#" + invocation.Function.Arity : invocation.Component.SymbolicIdentifier;
                    break;
                }

                index = index - 1;
            }

            depth = depth + 1;
        }

        string[] existingComponents = diagnostic.ComponentStack == null ? [] : diagnostic.ComponentStack;
        if (components.Length > existingComponents.Length) diagnostic.ComponentStack = components;
    }

    internal string[] CreateXsltFunctionStack()
    {
        string[] result = new string[Depth];
        int depth = 0;
        while (depth < Depth)
        {
            result[depth] = "function";
            int index = Invocations.Count - 1;
            while (index >= 0)
            {
                XsltFunctionInvocation invocation = Invocations[index];
                if (invocation.Depth == depth)
                {
                    result[depth] = invocation.Function.ExpandedName + "#" + invocation.Function.Arity;
                    break;
                }

                index = index - 1;
            }

            depth = depth + 1;
        }

        return result;
    }

    internal XPathDynamicContext CreateXsltFunctionContext(XPathDynamicContext caller, XsltComponent? component)
    {
        XPathDynamicContext result = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(caller);
        result.ContextItem = null;
        result.Position = 0;
        result.Size = 0;
        result.XsltCurrentGroupAvailable = false;
        result.XsltCurrentGroup = null;
        result.XsltCurrentGroupingKeyAvailable = false;
        result.XsltCurrentGroupingKey = null;
        result.XsltCurrentMergeGroupAvailable = false;
        result.XsltCurrentMergeGroup = null;
        result.XsltCurrentMergeSourceNames = null;
        result.XsltCurrentMergeSourceGroups = null;
        result.XsltCurrentMergeKeyAvailable = false;
        result.XsltCurrentMergeKey = null;
        result.XsltKeys = null;
        result.XsltRegexGroups = null;
        result.XsltFunctions = this;
        result.XsltCurrentComponent = component;
        result.XsltCurrentItem = null;
        result.Variables = Variables.CreateXsltRuntimeXPathBindings(null);
        result.VariableResolver = new XsltGlobalVariableResolver(Variables);
        return result;
    }

    internal XdmSequence ConvertXsltFunctionValue(XdmSequence supplied, XPathTypeSyntax? required, XsltFunctionPlan function, bool result)
    {
        if (required == null)
        {
            return Sequences.FreezeXdmSequenceStorage(supplied);
        }

        XPathStaticContext context = CreateXsltFunctionStaticContext(function);
        Task<XdmSequence> pending = Task.Run(() => new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(supplied, required, context));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return Sequences.FreezeXdmSequenceStorage(pending.Result);
        }

        string code = result ? XsltErrorCodes.XTTE0780 : XPathErrorCodes.XPTY0004;
        throw Templates.CreateXsltTemplateTypeError(result ? "The stylesheet function result does not match its declared type." : "A stylesheet function argument does not match its declared parameter type.", code, function.Declaration);
    }

    internal XPathStaticContext CreateXsltFunctionStaticContext(XsltFunctionPlan function)
    {
        XsltStaticContext source = function.StaticContext;
        XPathStaticContext result = new XPathStaticContext();
        result.LanguageVersion = XPathLanguageVersion.XPath31;
        result.CompatibilityMode = source.XPathCompatibilityMode;
        result.Namespaces = source.Namespaces;
        result.DefaultElementNamespace = source.XPathDefaultNamespace;
        result.DefaultFunctionNamespace = XPathNamespaces.Functions;
        result.BaseUri = source.BaseUri;
        result.DefaultCollation = source.DefaultCollation;
        result.Collations = [XPathNamespaces.CodepointCollation, XPathAsciiCaseInsensitiveCollation.Uri];
        result.SchemaSet = State.Stylesheet.SchemaSet;
        result.DecimalFormats = source.DecimalFormats;
        result.ResolverPolicy = State.DynamicContext.ResolverPolicy;
        result.SchemaAware = State.Executable.IsSchemaAware;
        result.AllowHigherOrderFunctions = true;
        return result;
    }

    internal XsltComponent? ResolveXsltFunctionComponent(XPathFunctionBinding binding, XsltComponent? owner)
    {
        XmlName? name = binding.Name;
        string expandedName = CreateXsltFunctionExpandedName(name);
        bool original = name != null && name.NamespaceUri == XsltNamespaces.Xslt && name.LocalName == "original";
        if (owner != null)
        {
            int referenceIndex = 0;
            while (referenceIndex < owner.References.Length)
            {
                XsltSymbolicReference reference = owner.References[referenceIndex];
                bool matches = reference.Kind == XsltComponentKind.Function && reference.Arity == binding.Arity && (original ? reference.Name == "Q{" + XsltNamespaces.Xslt + "}original" : reference.Name == expandedName);
                if (matches && reference.Target != null)
                {
                    return reference.Target;
                }

                referenceIndex = referenceIndex + 1;
            }
        }

        if (original)
        {
            return owner == null ? null : owner.OriginalComponent;
        }

        XsltPackagePlan? package = State.Stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int index = components.Length - 1;
        while (index >= 0)
        {
            XsltComponent candidate = components[index];
            if (candidate.Kind == XsltComponentKind.Function && candidate.Name == expandedName && candidate.Arity == binding.Arity && candidate.Visibility != XsltComponentVisibility.Hidden && candidate.Function != null)
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltComponent? FindXsltFunctionComponent(XsltFunctionPlan function)
    {
        XsltPackagePlan? package = State.Stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int index = components.Length - 1;
        while (index >= 0)
        {
            XsltComponent component = components[index];
            if (component.Kind == XsltComponentKind.Function && component.Function == function && component.Visibility != XsltComponentVisibility.Hidden)
            {
                return component;
            }

            index = index - 1;
        }

        return function.Component;
    }

    internal string CreateXsltFunctionExpandedName(XmlName? name)
    {
        return name == null ? "" : "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
    }
}
