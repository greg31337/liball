namespace xml;

internal class XPathInlineFunctionImplementation : IXPathFunction
{
    internal XPathPlan BodyPlan;
    internal XPathPlanParameter[] Parameters;
    internal XPathTypeSyntax? ResultType;
    internal XPathStaticContext StaticContext;
    internal XPathFunctionTypeConverter TypeConverter;
    internal XPathFunctionContextFactory ContextFactory;

    internal XPathInlineFunctionImplementation(XPathPlan bodyPlan, XPathPlanParameter[] parameters, XPathTypeSyntax? resultType, XPathStaticContext staticContext)
    {
        BodyPlan = bodyPlan;
        Parameters = parameters;
        ResultType = resultType;
        StaticContext = staticContext;
        TypeConverter = new XPathFunctionTypeConverter();
        ContextFactory = new XPathFunctionContextFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XPathInlineFunctionImplementation current = this;
        XdmSequence[] currentArguments = arguments;
        XPathDynamicContext currentContext = context;
        XmlSecurityLimits? limits = context.SecurityLimits;
        int maximumDepth = limits != null && limits.MaximumDepth > 0 ? limits.MaximumDepth : 256;
        int polling = limits != null && limits.CancellationPollingInterval > 0 ? limits.CancellationPollingInterval : 1024;
        long maximumOperations = context.MaximumOperations > 0 ? context.MaximumOperations : long.MaxValue;
        XPathRuntimeLimiter recursionLimiter = new XPathRuntimeLimiter(maximumOperations, maximumDepth, polling, context.Cancellation);
        while (true)
        {
            recursionLimiter.CountXPathWork(current.BodyPlan.Range);
            XdmSequence[] converted = current.ConvertXPathInlineArguments(currentArguments);
            XPathDynamicContext invocation = current.ContextFactory.CreateXPathInlineDynamicContext(currentContext, current.Parameters, converted);
            XPathRuntime runtime = new XPathRuntime(current.BodyPlan, invocation);
            XdmSequence result = runtime.EvaluateXPathPlan();
            if (!runtime.FunctionEvaluator.HasTailCall)
            {
                return current.ConvertXPathInlineResult(result);
            }

            XdmFunctionItem? target = runtime.FunctionEvaluator.TailFunction;
            XdmSequence[] tailArguments = runtime.FunctionEvaluator.TailArguments;
            XPathDynamicContext? tailContext = runtime.FunctionEvaluator.TailContext;
            if (target == null || tailContext == null)
            {
                throw new System.InvalidOperationException("An XPath tail call is incomplete.");
            }

            XPathInlineFunctionImplementation? next = target.Implementation as XPathInlineFunctionImplementation;
            if (next == current)
            {
                XPathVariableBinding[] closure = target.ClosureVariables == null ? [] : target.ClosureVariables;
                currentArguments = tailArguments;
                currentContext = new XdmFunctionOperations().CreateXPathClosureContext(tailContext, closure);
            }
            else
            {
                XdmSequence tailResult = new XdmFunctionOperations().InvokeXdmFunctionItem(target, tailArguments, tailContext, current.StaticContext);
                return current.ConvertXPathInlineResult(tailResult);
            }
        }
    }

    internal XdmSequence[] ConvertXPathInlineArguments(XdmSequence[] arguments)
    {
        if (arguments.Length != Parameters.Length)
        {
            throw TypeConverter.CreateXPathFunctionTypeError("The supplied argument count does not match the inline function arity.");
        }

        XdmSequence[] result = new XdmSequence[arguments.Length];
        int index = 0;
        while (index < arguments.Length)
        {
            result[index] = TypeConverter.ConvertXPathFunctionTypedValue(arguments[index], Parameters[index].Type, StaticContext);
            index = index + 1;
        }

        return result;
    }

    internal XdmSequence ConvertXPathInlineArgument(int index, XdmSequence argument)
    {
        if (index < 0 || index >= Parameters.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return TypeConverter.ConvertXPathFunctionTypedValue(argument, Parameters[index].Type, StaticContext);
    }

    internal XdmSequence ConvertXPathInlineResult(XdmSequence result)
    {
        return TypeConverter.ConvertXPathFunctionTypedValue(result, ResultType, StaticContext);
    }

}
