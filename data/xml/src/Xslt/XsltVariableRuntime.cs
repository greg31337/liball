using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xml;

internal enum XsltVariableEvaluationStatus
{
    Uninitialized = 0,
    Evaluating = 1,
    Succeeded = 2,
    Failed = 3
}

internal class XsltVariableEvaluation
{
    internal XsltVariablePlan Variable;
    internal XsltVariableEvaluationStatus Status;
    internal XdmSequence? Value;
    internal Exception? Failure;
    internal int EvaluationCount;

    internal XsltVariableEvaluation(XsltVariablePlan variable)
    {
        Variable = variable;
        Status = XsltVariableEvaluationStatus.Uninitialized;
        Value = null;
        Failure = null;
        EvaluationCount = 0;
    }
}

internal class XsltRuntimeVariableBinding
{
    internal XsltVariablePlan Variable;
    internal XdmSequence Value;
    internal bool IsTunnel;

    internal XsltRuntimeVariableBinding(XsltVariablePlan variable, XdmSequence value, bool isTunnel)
    {
        Variable = variable;
        Value = value;
        IsTunnel = isTunnel;
    }
}

internal class XsltVariableFrame
{
    internal XsltVariableFrame? Parent;
    internal List<XsltRuntimeVariableBinding> Bindings;
    internal List<XPathVariableBinding> DynamicBindings;
    internal XsltParameter[] TunnelParameters;
    internal XPathDynamicContext Context;

    internal XsltVariableFrame(XsltVariableFrame? parent, XsltParameter[] tunnelParameters, XPathDynamicContext context)
    {
        Parent = parent;
        Bindings = new List<XsltRuntimeVariableBinding>();
        DynamicBindings = new List<XPathVariableBinding>();
        TunnelParameters = tunnelParameters;
        Context = context;
    }
}

internal class XsltVariableRuntime
{
    internal XsltTransformationState State;
    internal XsltVariableEvaluation[] Globals;

    internal XsltVariableRuntime(XsltTransformationState state)
    {
        State = state;
        Globals = CreateXsltGlobalEvaluations(state.Stylesheet.Globals);
        ValidateXsltRequiredGlobalParameters();
    }

    internal XsltVariableEvaluation[] CreateXsltGlobalEvaluations(XsltVariablePlan[] variables)
    {
        XsltVariableEvaluation[] result = new XsltVariableEvaluation[variables.Length];
        int index = 0;
        while (index < variables.Length)
        {
            result[index] = new XsltVariableEvaluation(variables[index]);
            index = index + 1;
        }

        return result;
    }

    internal void ValidateXsltRequiredGlobalParameters()
    {
        int index = 0;
        while (index < Globals.Length)
        {
            XsltVariablePlan variable = Globals[index].Variable;
            if (variable.IsParameter && variable.IsRequired && FindXsltSuppliedParameter(State.Parameters, variable.Name, false) == null)
            {
                throw CreateXsltVariableDynamicError("A required stylesheet parameter was not supplied.", XsltErrorCodes.XTDE0050, variable.Declaration);
            }

            index = index + 1;
        }
    }

    internal XdmSequence EvaluateXsltGlobalVariable(XmlName name)
    {
        XsltVariableEvaluation? evaluation = FindXsltGlobalEvaluation(name);
        if (evaluation == null)
        {
            throw CreateXsltVariableDynamicError("The requested global variable does not exist.", XPathErrorCodes.XPDY0002, null);
        }

        return EvaluateXsltGlobalSlot(evaluation);
    }

    internal XdmSequence EvaluateXsltGlobalSlot(XsltVariableEvaluation evaluation)
    {
        if (evaluation.Status == XsltVariableEvaluationStatus.Succeeded && evaluation.Value != null)
        {
            return evaluation.Value;
        }

        if (evaluation.Status == XsltVariableEvaluationStatus.Failed && evaluation.Failure != null)
        {
            throw evaluation.Failure;
        }

        if (evaluation.Status == XsltVariableEvaluationStatus.Evaluating)
        {
            XsltDynamicException cycle = CreateXsltVariableDynamicError("A circular global variable dependency was evaluated.", XsltErrorCodes.XTDE0640, evaluation.Variable.Declaration);
            evaluation.Status = XsltVariableEvaluationStatus.Failed;
            evaluation.Failure = cycle;
            throw cycle;
        }

        evaluation.Status = XsltVariableEvaluationStatus.Evaluating;
        evaluation.EvaluationCount = evaluation.EvaluationCount + 1;
        Task<XdmSequence> pending = Task.Run(() => EvaluateXsltGlobalSlotCore(evaluation));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            XdmSequence value = new XsltTransformationEngine().CopyXsltRuntimeSequence(pending.Result);
            evaluation.Value = value;
            evaluation.Status = XsltVariableEvaluationStatus.Succeeded;
            return value;
        }

        Exception failure = ReadXsltVariableTaskFailure(pending.Exception, evaluation.Variable);
        evaluation.Failure = failure;
        evaluation.Status = XsltVariableEvaluationStatus.Failed;
        throw failure;
    }

    internal XdmSequence EvaluateXsltGlobalSlotCore(XsltVariableEvaluation evaluation)
    {
        XsltVariablePlan variable = evaluation.Variable;
        XsltParameter? supplied = variable.IsParameter ? FindXsltSuppliedParameter(State.Parameters, variable.Name, false) : null;
        XdmSequence value;
        if (supplied != null && supplied.Value != null)
        {
            value = supplied.Value;
        }
        else
        {
            EvaluateXsltVariableDependencies(variable);
            value = EvaluateXsltVariableDefault(variable, null);
        }

        return ConvertXsltVariableValue(variable, value);
    }

    internal void EvaluateXsltVariableDependencies(XsltVariablePlan variable)
    {
        int dependencyIndex = 0;
        while (dependencyIndex < variable.Dependencies.Length)
        {
            XsltVariableEvaluation? dependency = FindXsltGlobalEvaluationByPlan(variable.Dependencies[dependencyIndex]);
            if (dependency != null)
            {
                EvaluateXsltGlobalSlot(dependency);
            }

            dependencyIndex = dependencyIndex + 1;
        }
    }

    internal XdmSequence EvaluateXsltVariableDefault(XsltVariablePlan variable, XsltVariableFrame? frame)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        if (variable.ValueKind == XsltVariableValueKind.StaticValue && variable.Binding.Value != null)
        {
            return variable.Binding.Value;
        }

        if (variable.ValueKind == XsltVariableValueKind.Select && variable.SelectPlan != null)
        {
            return EvaluateXsltVariableSelect(variable, frame);
        }

        if (variable.ValueKind == XsltVariableValueKind.EmptySequence)
        {
            return sequences.CreateEmptyXdmSequence();
        }

        if (variable.ValueKind == XsltVariableValueKind.EmptyString)
        {
            XdmAtomicValue empty = new XdmFactory().CreateXdmStringAtomicValue("", XdmAtomicType.String);
            return sequences.CreateXdmAtomicSequence(empty);
        }

        if (variable.ValueKind == XsltVariableValueKind.Required)
        {
            string code = variable.IsGlobal ? XsltErrorCodes.XTDE0050 : XsltErrorCodes.XTDE0700;
            throw CreateXsltVariableDynamicError("A required XSLT parameter was not supplied.", code, variable.Declaration);
        }

        if (variable.ValueKind == XsltVariableValueKind.Content && State.TemplateRuntime != null)
        {
            return State.TemplateRuntime.SequenceRuntime.EvaluateXsltVariableContent(variable, frame);
        }

        throw CreateXsltVariableDynamicError("Variable sequence-constructor evaluation is not available for this value.", XsltErrorCodes.XTDE0030, variable.Declaration);
    }

    internal XdmSequence EvaluateXsltVariableSelect(XsltVariablePlan variable, XsltVariableFrame? frame)
    {
        XPathPlan? plan = variable.SelectPlan;
        if (plan == null)
        {
            throw new InvalidOperationException("A select variable has no compiled XPath plan.");
        }

        XPathDynamicContext context = CreateXsltVariableDynamicContext(frame);
        if (frame == null)
        {
            context.XsltCurrentComponent = variable.Component;
        }
        XPathRuntime runtime = new XPathRuntime(plan, context);
        runtime.Resources = State.DocumentPool;
        return runtime.EvaluateXPathPlan();
    }

    internal XPathDynamicContext CreateXsltVariableDynamicContext(XsltVariableFrame? frame)
    {
        XPathDynamicContext basis = frame == null ? State.DynamicContext : frame.Context;
        XPathDynamicContext result = new XPathFunctionContextFactory().CopyXPathFunctionDynamicComponents(basis);
        result.XsltKeys = frame == null ? null : basis.XsltKeys;
        result.XsltAccumulators = State.AccumulatorRuntime;
        if (frame == null)
        {
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
            result.XsltRegexGroups = null;
            result.XsltCurrentComponent = null;
        }
        result.Variables = CreateXsltRuntimeXPathBindings(frame);
        result.VariableResolver = new XsltGlobalVariableResolver(this);
        return result;
    }

    internal XPathVariableBinding[] CreateXsltRuntimeXPathBindings(XsltVariableFrame? frame)
    {
        List<XPathVariableBinding> result = new List<XPathVariableBinding>();
        List<XsltVariableFrame> frames = new List<XsltVariableFrame>();
        XsltVariableFrame? current = frame;
        while (current != null)
        {
            frames.Add(current);
            current = current.Parent;
        }

        int frameIndex = frames.Count - 1;
        while (frameIndex >= 0)
        {
            List<XsltRuntimeVariableBinding> bindings = frames[frameIndex].Bindings;
            int index = 0;
            while (index < bindings.Count)
            {
                SetXsltXPathVariableBinding(result, CreateXsltXPathVariableBinding(bindings[index].Variable, bindings[index].Value));
                index = index + 1;
            }

            List<XPathVariableBinding> dynamicBindings = frames[frameIndex].DynamicBindings;
            index = 0;
            while (index < dynamicBindings.Count)
            {
                SetXsltXPathVariableBinding(result, dynamicBindings[index]);
                index = index + 1;
            }

            frameIndex = frameIndex - 1;
        }

        return result.ToArray();
    }

    internal XPathVariableBinding CreateXsltXPathVariableBinding(XsltVariablePlan variable, XdmSequence value)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = variable.Name;
        result.Value = value;
        result.DeclaredType = variable.Binding.DeclaredType;
        result.DeclaredOccurrence = variable.Binding.DeclaredOccurrence;
        result.StaticTypeSyntax = variable.SequenceType;
        return result;
    }

    internal void SetXsltXPathVariableBinding(List<XPathVariableBinding> bindings, XPathVariableBinding value)
    {
        int index = 0;
        while (index < bindings.Count)
        {
            if (SameXsltVariableName(bindings[index].Name, value.Name))
            {
                bindings[index] = value;
                return;
            }

            index = index + 1;
        }

        bindings.Add(value);
    }

    internal XdmSequence ConvertXsltVariableValue(XsltVariablePlan variable, XdmSequence supplied)
    {
        if (variable.SequenceType == null)
        {
            return new XsltTransformationEngine().CopyXsltRuntimeSequence(supplied);
        }

        XPathStaticContext context = CreateXsltVariableConversionContext(variable);
        Task<XdmSequence> pending = Task.Run(() => new XPathFunctionTypeConverter().ConvertXPathFunctionTypedValue(supplied, variable.SequenceType, context));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return new XsltTransformationEngine().CopyXsltRuntimeSequence(pending.Result);
        }

        string code = variable.IsParameter ? XsltErrorCodes.XTTE0590 : XsltErrorCodes.XTTE0570;
        throw CreateXsltVariableTypeError("The supplied variable value does not match its declared type.", code, variable.Declaration);
    }

    internal XPathStaticContext CreateXsltVariableConversionContext(XsltVariablePlan variable)
    {
        if (variable.SelectPlan != null)
        {
            return new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(variable.SelectPlan.Environment);
        }

        XsltStaticContext source = variable.StaticContext;
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

    internal XsltVariableFrame BindXsltTemplateParameters(XsltVariablePlan[] declarations, XsltParameter[] supplied, XsltVariableFrame? parent, XPathDynamicContext context)
    {
        XsltParameter[] tunnels = MergeXsltTunnelParameters(parent == null ? [] : parent.TunnelParameters, supplied);
        XsltVariableFrame frame = new XsltVariableFrame(parent, tunnels, context);
        int index = 0;
        while (index < declarations.Length)
        {
            XsltVariablePlan variable = declarations[index];
            XsltParameter? parameter = variable.IsTunnel ? FindXsltSuppliedParameter(tunnels, variable.Name, true) : FindXsltSuppliedParameter(supplied, variable.Name, false);
            XdmSequence value;
            if (parameter != null && parameter.Value != null)
            {
                value = ConvertXsltVariableValue(variable, parameter.Value);
            }
            else
            {
                EvaluateXsltVariableDependencies(variable);
                value = ConvertXsltVariableValue(variable, EvaluateXsltVariableDefault(variable, frame));
            }

            frame.Bindings.Add(new XsltRuntimeVariableBinding(variable, value, variable.IsTunnel));
            index = index + 1;
        }

        return frame;
    }

    internal XdmSequence EvaluateXsltLocalVariable(XsltVariablePlan variable, XsltVariableFrame frame)
    {
        EvaluateXsltVariableDependencies(variable);
        XdmSequence value = ConvertXsltVariableValue(variable, EvaluateXsltVariableDefault(variable, frame));
        frame.Bindings.Add(new XsltRuntimeVariableBinding(variable, value, false));
        return value;
    }

    internal XsltParameter[] MergeXsltTunnelParameters(XsltParameter[] inherited, XsltParameter[] supplied)
    {
        List<XsltParameter> result = new List<XsltParameter>();
        int index = 0;
        while (index < inherited.Length)
        {
            result.Add(inherited[index]);
            index = index + 1;
        }

        index = 0;
        while (index < supplied.Length)
        {
            XsltParameter parameter = supplied[index];
            if (parameter.IsTunnel)
            {
                int existing = FindXsltParameterIndex(result, parameter.Name, true);
                if (existing < 0)
                {
                    result.Add(parameter);
                }
                else
                {
                    result[existing] = parameter;
                }
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal int FindXsltParameterIndex(List<XsltParameter> parameters, XmlName? name, bool tunnel)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (parameters[index].IsTunnel == tunnel && SameXsltVariableName(parameters[index].Name, name))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal XsltParameter? FindXsltSuppliedParameter(XsltParameter[] parameters, XmlName name, bool tunnel)
    {
        int index = parameters.Length - 1;
        while (index >= 0)
        {
            if (parameters[index].IsTunnel == tunnel && SameXsltVariableName(parameters[index].Name, name))
            {
                return parameters[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal XsltVariableEvaluation? FindXsltGlobalEvaluation(XmlName name)
    {
        return ResolveXsltGlobalEvaluation(name, null);
    }

    internal XsltVariableEvaluation? ResolveXsltGlobalEvaluation(XmlName name, XsltComponent? owner)
    {
        string expandedName = "Q{" + (name.NamespaceUri == null ? "" : name.NamespaceUri) + "}" + (name.LocalName == null ? "" : name.LocalName);
        if (owner != null)
        {
            int referenceIndex = 0;
            while (referenceIndex < owner.References.Length)
            {
                XsltSymbolicReference reference = owner.References[referenceIndex];
                XsltVariablePlan? target = reference.Target == null ? null : reference.Target.Variable;
                if (reference.Kind == XsltComponentKind.Variable && reference.Name == expandedName && target != null)
                {
                    return FindXsltGlobalEvaluationByPlan(target);
                }

                referenceIndex = referenceIndex + 1;
            }
        }

        XsltPackagePlan? package = State.Stylesheet.Package;
        XsltComponent[] components = package == null ? [] : package.Components;
        int componentIndex = components.Length - 1;
        while (componentIndex >= 0)
        {
            XsltComponent component = components[componentIndex];
            if (component.Kind == XsltComponentKind.Variable && component.Name == expandedName && component.Visibility != XsltComponentVisibility.Hidden && component.Variable != null)
            {
                XsltVariableEvaluation? selected = FindXsltGlobalEvaluationByPlan(component.Variable);
                if (selected != null)
                {
                    return selected;
                }
            }

            componentIndex = componentIndex - 1;
        }

        int index = 0;
        while (index < Globals.Length)
        {
            if (SameXsltVariableName(Globals[index].Variable.Name, name))
            {
                return Globals[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltVariableEvaluation? FindXsltGlobalEvaluationByPlan(XsltVariablePlan variable)
    {
        int index = 0;
        while (index < Globals.Length)
        {
            if (object.ReferenceEquals(Globals[index].Variable, variable))
            {
                return Globals[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XsltRuntimeVariableBinding? FindXsltFrameBinding(XsltVariableFrame frame, XmlName name, bool tunnel)
    {
        XsltVariableFrame? current = frame;
        while (current != null)
        {
            int index = current.Bindings.Count - 1;
            while (index >= 0)
            {
                XsltRuntimeVariableBinding binding = current.Bindings[index];
                if (binding.IsTunnel == tunnel && SameXsltVariableName(binding.Variable.Name, name))
                {
                    return binding;
                }

                index = index - 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool SameXsltVariableName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return left.LocalName == right.LocalName && leftNamespace == rightNamespace;
    }

    internal Exception ReadXsltVariableTaskFailure(AggregateException? failure, XsltVariablePlan variable)
    {
        if (failure != null && failure.InnerException != null)
        {
            return failure.InnerException;
        }

        return CreateXsltVariableDynamicError("The variable initializer failed.", XsltErrorCodes.XTDE0640, variable.Declaration);
    }

    internal XsltDynamicException CreateXsltVariableDynamicError(string message, string code, XmlNode? node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        return new XsltDynamicException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }

    internal XsltTypeException CreateXsltVariableTypeError(string message, string code, XmlNode node)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        return new XsltTypeException(message, diagnostic, diagnostic.Range == null ? null : diagnostic.Range.Start);
    }
}
