using System;
using System.Numerics;

namespace xml;

internal class XdmFunctionOperations
{
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;

    internal XdmFunctionOperations()
    {
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
    }

    internal XdmFunctionItem CreateXdmFunctionItem(XPathFunctionBinding binding, IXPathFunction implementation, XPathVariableBinding[] closureVariables)
    {
        Guard.NotNull(binding, "binding");
        Guard.NotNull(implementation, "implementation");
        Guard.NotNull(closureVariables, "closureVariables");
        ValidateXPathFunctionBinding(binding);
        XdmFunctionItem result = new XdmFunctionItem();
        result.Name = CopyXdmFunctionName(binding.Name);
        result.Arity = binding.Arity;
        result.ParameterTypes = CopyXdmFunctionTypes(binding.ParameterTypes);
        result.ParameterOccurrences = CopyXdmFunctionOccurrences(binding.ParameterOccurrences);
        result.ResultType = binding.ResultType;
        result.ResultOccurrence = binding.ResultOccurrence;
        result.BindingKey = binding.BindingKey;
        result.Implementation = implementation;
        result.ClosureVariables = FreezeXPathClosureVariables(closureVariables);
        result.IsDeterministic = binding.IsDeterministic;
        result.IsMap = false;
        result.IsArray = false;
        result.IsImmutable = true;
        return result;
    }

    internal XdmFunctionItem CreateXdmMapFunctionItem(XdmMapValue map)
    {
        Maps.ValidateXdmMapStorage(map);
        XdmFunctionItem result = new XdmFunctionItem();
        result.Arity = 1;
        result.ParameterTypes = [XdmAtomicType.AnyAtomicType];
        result.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.MapValue = Maps.CreateXdmMap(map.Entries == null ? [] : map.Entries);
        result.ClosureVariables = [];
        result.IsDeterministic = true;
        result.IsMap = true;
        result.IsArray = false;
        result.IsImmutable = true;
        return result;
    }

    internal XdmFunctionItem CreateXdmArrayFunctionItem(XdmArrayValue array)
    {
        Arrays.ValidateXdmArrayStorage(array);
        XdmFunctionItem result = new XdmFunctionItem();
        result.Arity = 1;
        result.ParameterTypes = [XdmAtomicType.Integer];
        result.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        result.ResultType = XdmAtomicType.AnyAtomicType;
        result.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        result.ArrayValue = Arrays.CreateXdmArray(array.Members == null ? [] : array.Members);
        result.ClosureVariables = [];
        result.IsDeterministic = true;
        result.IsMap = false;
        result.IsArray = true;
        result.IsImmutable = true;
        return result;
    }

    internal XdmSequence InvokeXdmFunctionItem(XdmFunctionItem function, XdmSequence[] arguments, XPathDynamicContext dynamicContext, XPathStaticContext staticContext)
    {
        ValidateXdmFunctionItem(function);
        Guard.NotNull(arguments, "arguments");
        Guard.NotNull(dynamicContext, "dynamicContext");
        Guard.NotNull(staticContext, "staticContext");
        RequireXdmFunctionArity(function, arguments.Length);
        if (function.IsMap)
        {
            XdmMapValue? map = function.MapValue;
            return InvokeXdmMapFunction(map == null ? throw new ArgumentException("A map function item has no map value.", "function") : map, arguments, staticContext);
        }

        if (function.IsArray)
        {
            XdmArrayValue? array = function.ArrayValue;
            return InvokeXdmArrayFunction(array == null ? throw new ArgumentException("An array function item has no array value.", "function") : array, arguments, staticContext);
        }

        IXPathFunction? implementation = function.Implementation;
        if (implementation == null)
        {
            throw CreateXdmFunctionDynamicError("The function item has no invocation implementation.", XPathErrorCodes.XPDY0002);
        }

        XdmSequence[] convertedArguments = ConvertXdmFunctionArguments(function, arguments, staticContext);
        XPathFunctionBinding binding = CreateXPathFunctionBinding(function);
        XPathDynamicContext closureContext = CreateXPathClosureContext(dynamicContext, function.ClosureVariables == null ? [] : function.ClosureVariables);
        XdmSequence? result = implementation.InvokeXPathFunction(binding, convertedArguments, closureContext);
        if (result == null)
        {
            throw CreateXdmFunctionDynamicError("The function implementation returned no XDM sequence.", XPathErrorCodes.XPDY0002);
        }

        Sequences.ValidateXdmSequenceStorage(result);
        XdmSequence convertedResult = ConvertXdmFunctionResult(function, result, staticContext);
        return Sequences.FreezeXdmSequenceStorage(convertedResult);
    }

    internal XdmSequence InvokeXdmMapFunction(XdmMapValue map, XdmSequence[] arguments, XPathStaticContext staticContext)
    {
        Guard.NotNull(map, "map");
        Guard.NotNull(arguments, "arguments");
        Guard.NotNull(staticContext, "staticContext");
        RequireXdmCallableArity(1, arguments.Length);
        XdmSequenceType expected = CreateXdmAtomicSequenceType(XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne);
        XdmSequence keySequence = Converter.ApplyXdmFunctionConversion(arguments[0], expected, staticContext);
        XdmAtomicValue? key = Sequences.ReadValidatedXdmSequenceItem(keySequence, 0).AtomicValue;
        if (key == null)
        {
            throw CreateXdmFunctionTypeError("A map function argument must be one atomic key.");
        }

        return Sequences.FreezeXdmSequenceStorage(Maps.GetXdmMapValue(map, key));
    }

    internal XdmSequence InvokeXdmArrayFunction(XdmArrayValue array, XdmSequence[] arguments, XPathStaticContext staticContext)
    {
        Guard.NotNull(array, "array");
        Guard.NotNull(arguments, "arguments");
        Guard.NotNull(staticContext, "staticContext");
        RequireXdmCallableArity(1, arguments.Length);
        XdmSequenceType expected = CreateXdmAtomicSequenceType(XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XdmSequence positionSequence = Converter.ApplyXdmFunctionConversion(arguments[0], expected, staticContext);
        XdmAtomicValue? positionValue = Sequences.ReadValidatedXdmSequenceItem(positionSequence, 0).AtomicValue;
        if (positionValue == null)
        {
            throw CreateXdmFunctionTypeError("An array function argument must be one integer position.");
        }

        BigInteger position = new XdmNumericDatatype().ReadXdmIntegerValue(positionValue);
        if (position < new BigInteger(int.MinValue) || position > new BigInteger(int.MaxValue))
        {
            throw Arrays.CreateXdmArrayDynamicError("The array position is out of bounds.", XPathErrorCodes.FOAY0001);
        }

        return Sequences.FreezeXdmSequenceStorage(Arrays.GetXdmArrayMember(array, (int)position));
    }

    internal void ValidateXdmFunctionItem(XdmFunctionItem function)
    {
        Guard.NotNull(function, "function");
        ValidateXdmFunctionSignature(function.Arity, function.ParameterTypes, function.ParameterOccurrences, function.ResultType, function.ResultOccurrence, "function");
        ValidateXdmFunctionName(function.Name, "function");
        if (function.IsMap && function.IsArray)
        {
            throw new ArgumentException("A function item cannot represent both a map and an array.", "function");
        }

        if (function.IsMap && (function.MapValue == null || function.ArrayValue != null || function.Implementation != null))
        {
            throw new ArgumentException("The map function item storage is inconsistent.", "function");
        }

        if (function.IsArray && (function.ArrayValue == null || function.MapValue != null || function.Implementation != null))
        {
            throw new ArgumentException("The array function item storage is inconsistent.", "function");
        }

        if (!function.IsMap && !function.IsArray && (function.MapValue != null || function.ArrayValue != null))
        {
            throw new ArgumentException("An ordinary function item cannot carry map or array storage.", "function");
        }

        XPathVariableBinding[] closure = function.ClosureVariables == null ? [] : function.ClosureVariables;
        ValidateXPathClosureVariables(closure, "function");
        if (function.IsMap || function.IsArray)
        {
            ValidateXdmContainerFunctionMetadata(function, closure);
        }

        if (function.IsMap && function.MapValue != null)
        {
            Maps.ValidateXdmMapStorage(function.MapValue);
        }

        if (function.IsArray && function.ArrayValue != null)
        {
            Arrays.ValidateXdmArrayStorage(function.ArrayValue);
        }
    }

    internal void ValidateXdmContainerFunctionMetadata(XdmFunctionItem function, XPathVariableBinding[] closure)
    {
        XdmAtomicType[] parameterTypes = function.ParameterTypes == null ? [] : function.ParameterTypes;
        XdmOccurrence[] parameterOccurrences = function.ParameterOccurrences == null ? [] : function.ParameterOccurrences;
        XdmAtomicType expectedType = function.IsMap ? XdmAtomicType.AnyAtomicType : XdmAtomicType.Integer;
        if (function.Name != null || function.Arity != 1 || parameterTypes.Length != 1 || parameterOccurrences.Length != 1 || parameterTypes[0] != expectedType || parameterOccurrences[0] != XdmOccurrence.ExactlyOne)
        {
            throw new ArgumentException("The map or array function signature is inconsistent.", "function");
        }

        if (function.ResultType != XdmAtomicType.AnyAtomicType || function.ResultOccurrence != XdmOccurrence.ZeroOrMore || closure.Length != 0)
        {
            throw new ArgumentException("A map or array function item has invalid result or closure metadata.", "function");
        }
    }

    internal void ValidateXPathFunctionBinding(XPathFunctionBinding binding)
    {
        Guard.NotNull(binding, "binding");
        ValidateXdmFunctionSignature(binding.Arity, binding.ParameterTypes, binding.ParameterOccurrences, binding.ResultType, binding.ResultOccurrence, "binding");
        ValidateXdmFunctionName(binding.Name, "binding");
    }

    internal void ValidateXdmFunctionSignature(int arity, XdmAtomicType[]? parameterTypes, XdmOccurrence[]? parameterOccurrences, XdmAtomicType resultType, XdmOccurrence resultOccurrence, string parameterName)
    {
        if (arity < 0)
        {
            throw new ArgumentException("A function arity cannot be negative.", parameterName);
        }

        if ((parameterTypes == null) != (parameterOccurrences == null))
        {
            throw new ArgumentException("Function parameter types and occurrences must be supplied together.", parameterName);
        }

        if (parameterTypes != null && parameterOccurrences != null)
        {
            if (parameterTypes.Length != arity || parameterOccurrences.Length != arity)
            {
                throw new ArgumentException("The function signature does not match its arity.", parameterName);
            }

            int index = 0;
            while (index < arity)
            {
                Types.GetXdmAtomicTypeDefinition(parameterTypes[index]);
                Sequences.ValidateXdmSequenceOccurrence(parameterOccurrences[index]);
                index = index + 1;
            }
        }

        Types.GetXdmAtomicTypeDefinition(resultType);
        Sequences.ValidateXdmSequenceOccurrence(resultOccurrence);
    }

    internal void ValidateXdmFunctionName(XmlName? name, string parameterName)
    {
        if (name != null && (name.LocalName == null || name.LocalName.Length == 0))
        {
            throw new ArgumentException("A named function must provide a local name.", parameterName);
        }
    }

    internal void RequireXdmFunctionArity(XdmFunctionItem function, int suppliedArity)
    {
        RequireXdmCallableArity(function.Arity, suppliedArity);
    }

    internal void RequireXdmCallableArity(int expectedArity, int suppliedArity)
    {
        if (expectedArity != suppliedArity)
        {
            throw CreateXdmFunctionTypeError("The supplied argument count does not match the function arity.");
        }
    }

    internal XdmSequence[] ConvertXdmFunctionArguments(XdmFunctionItem function, XdmSequence[] arguments, XPathStaticContext staticContext)
    {
        XdmSequence[] converted = new XdmSequence[arguments.Length];
        bool stylesheetFunction = IsXsltFunctionItem(function);
        int index = 0;
        while (index < arguments.Length)
        {
            XdmSequence? argument = arguments[index];
            if (argument == null)
            {
                throw new ArgumentException("A function argument cannot be null.", "arguments");
            }

            if (!stylesheetFunction && function.ParameterTypes != null && function.ParameterOccurrences != null)
            {
                XdmSequenceType expected = CreateXdmAtomicSequenceType(function.ParameterTypes[index], function.ParameterOccurrences[index]);
                converted[index] = Converter.ApplyXdmFunctionConversion(argument, expected, staticContext);
            }
            else
            {
                Sequences.ValidateXdmSequenceStorage(argument);
                converted[index] = Sequences.FreezeXdmSequenceStorage(argument);
            }

            index = index + 1;
        }

        return converted;
    }

    internal XdmSequence ConvertXdmFunctionResult(XdmFunctionItem function, XdmSequence result, XPathStaticContext staticContext)
    {
        if (function.ParameterTypes == null || IsXsltFunctionItem(function))
        {
            return result;
        }

        XdmSequenceType expected = CreateXdmAtomicSequenceType(function.ResultType, function.ResultOccurrence);
        return Converter.ApplyXdmFunctionConversion(result, expected, staticContext);
    }

    internal bool IsXsltFunctionItem(XdmFunctionItem function)
    {
        string? key = function.BindingKey;
        return key != null && (key.StartsWith("xslt-function:", StringComparison.Ordinal) || key.StartsWith("xslt-original-function:", StringComparison.Ordinal));
    }

    internal XdmSequenceType CreateXdmAtomicSequenceType(XdmAtomicType type, XdmOccurrence occurrence)
    {
        XdmSequenceType result = new XdmSequenceType();
        result.ItemKind = XdmItemKind.Atomic;
        result.AtomicType = type;
        result.Occurrence = occurrence;
        return result;
    }

    internal XPathFunctionBinding CreateXPathFunctionBinding(XdmFunctionItem function)
    {
        XPathFunctionBinding result = new XPathFunctionBinding();
        result.Name = CopyXdmFunctionName(function.Name);
        result.Arity = function.Arity;
        result.ParameterTypes = CopyXdmFunctionTypes(function.ParameterTypes);
        result.ParameterOccurrences = CopyXdmFunctionOccurrences(function.ParameterOccurrences);
        result.ResultType = function.ResultType;
        result.ResultOccurrence = function.ResultOccurrence;
        result.BindingKey = function.BindingKey;
        result.IsDeterministic = function.IsDeterministic;
        return result;
    }

    internal XPathDynamicContext CreateXPathClosureContext(XPathDynamicContext source, XPathVariableBinding[] closureVariables)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(closureVariables, "closureVariables");
        ValidateXPathClosureVariables(closureVariables, "closureVariables");
        XPathDynamicContext result = new XPathDynamicContext();
        result.ContextItem = source.ContextItem == null ? null : Sequences.FreezeXdmSequenceStorage(source.ContextItem);
        result.Position = source.Position;
        result.Size = source.Size;
        result.Variables = MergeXPathClosureVariables(source.Variables == null ? [] : source.Variables, closureVariables);
        result.VariableResolver = source.VariableResolver;
        result.BaseUri = source.BaseUri;
        result.DefaultCollectionUri = source.DefaultCollectionUri;
        result.ImplicitTimezoneMinutes = source.ImplicitTimezoneMinutes;
        result.DefaultLanguage = source.DefaultLanguage;
        result.CurrentDateTime = source.CurrentDateTime;
        result.EnvironmentNames = CopyXdmFunctionStrings(source.EnvironmentNames);
        result.EnvironmentValues = CopyXdmFunctionStrings(source.EnvironmentValues);
        result.ResolverPolicy = source.ResolverPolicy;
        result.CollectionResolver = source.CollectionResolver;
        result.ResourceResolver = source.ResourceResolver;
        result.TraceReceiver = source.TraceReceiver;
        result.Cancellation = source.Cancellation;
        result.SecurityLimits = source.SecurityLimits;
        result.MaximumOperations = source.MaximumOperations;
        result.XsltCurrentGroupAvailable = source.XsltCurrentGroupAvailable;
        result.XsltCurrentGroup = source.XsltCurrentGroup == null ? null : Sequences.FreezeXdmSequenceStorage(source.XsltCurrentGroup);
        result.XsltCurrentGroupingKeyAvailable = source.XsltCurrentGroupingKeyAvailable;
        result.XsltCurrentGroupingKey = source.XsltCurrentGroupingKey == null ? null : Sequences.FreezeXdmSequenceStorage(source.XsltCurrentGroupingKey);
        result.XsltCurrentMergeGroupAvailable = source.XsltCurrentMergeGroupAvailable;
        result.XsltCurrentMergeGroup = source.XsltCurrentMergeGroup == null ? null : Sequences.FreezeXdmSequenceStorage(source.XsltCurrentMergeGroup);
        result.XsltCurrentMergeSourceNames = source.XsltCurrentMergeSourceNames == null ? null : (string[])source.XsltCurrentMergeSourceNames.Clone();
        result.XsltCurrentMergeSourceGroups = CopyXdmMergeGroups(source.XsltCurrentMergeSourceGroups);
        result.XsltCurrentMergeKeyAvailable = source.XsltCurrentMergeKeyAvailable;
        result.XsltCurrentMergeKey = source.XsltCurrentMergeKey == null ? null : Sequences.FreezeXdmSequenceStorage(source.XsltCurrentMergeKey);
        result.XsltKeys = source.XsltKeys;
        result.XsltAccumulators = source.XsltAccumulators;
        result.XsltRegexGroups = source.XsltRegexGroups == null ? null : (string?[])source.XsltRegexGroups.Clone();
        result.XsltFunctions = source.XsltFunctions;
        result.XsltCurrentComponent = source.XsltCurrentComponent;
        result.XsltCurrentItem = source.XsltCurrentItem == null ? null : Sequences.FreezeXdmSequenceStorage(source.XsltCurrentItem);
        result.ExtensionState = source.ExtensionState;
        result.XsltCurrentOutputUri = source.XsltCurrentOutputUri;
        result.XsltDocuments = source.XsltDocuments;
        result.XsltStreamedNodes = source.XsltStreamedNodes;
        return result;
    }

    internal XdmSequence[]? CopyXdmMergeGroups(XdmSequence[]? groups)
    {
        if (groups == null)
        {
            return null;
        }

        XdmSequence[] result = new XdmSequence[groups.Length];
        int index = 0;
        while (index < groups.Length)
        {
            result[index] = Sequences.FreezeXdmSequenceStorage(groups[index]);
            index = index + 1;
        }

        return result;
    }

    internal XPathVariableBinding[] MergeXPathClosureVariables(XPathVariableBinding[] dynamicVariables, XPathVariableBinding[] closureVariables)
    {
        ValidateXPathClosureVariables(dynamicVariables, "dynamicVariables");
        ValidateXPathClosureVariables(closureVariables, "closureVariables");
        XPathVariableBinding[] merged = new XPathVariableBinding[dynamicVariables.Length + closureVariables.Length];
        int count = 0;
        int index = 0;
        while (index < dynamicVariables.Length)
        {
            if (!ContainsXPathVariableName(closureVariables, dynamicVariables[index].Name))
            {
                merged[count] = FreezeXPathVariableBinding(dynamicVariables[index]);
                count = count + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < closureVariables.Length)
        {
            merged[count] = FreezeXPathVariableBinding(closureVariables[index]);
            count = count + 1;
            index = index + 1;
        }

        XPathVariableBinding[] result = new XPathVariableBinding[count];
        Array.Copy(merged, result, count);
        return result;
    }

    internal XPathVariableBinding[] FreezeXPathClosureVariables(XPathVariableBinding[] variables)
    {
        ValidateXPathClosureVariables(variables, "closureVariables");
        XPathVariableBinding[] result = new XPathVariableBinding[variables.Length];
        int index = 0;
        while (index < variables.Length)
        {
            result[index] = FreezeXPathVariableBinding(variables[index]);
            index = index + 1;
        }

        return result;
    }

    internal XPathVariableBinding FreezeXPathVariableBinding(XPathVariableBinding variable)
    {
        XPathVariableBinding result = new XPathVariableBinding();
        result.Name = CopyXdmFunctionName(variable.Name);
        result.Value = variable.Value == null ? null : Sequences.FreezeXdmSequenceStorage(variable.Value);
        result.DeclaredType = variable.DeclaredType;
        result.DeclaredOccurrence = variable.DeclaredOccurrence;
        result.IsExternal = variable.IsExternal;
        result.IsRequired = variable.IsRequired;
        return result;
    }

    internal void ValidateXPathClosureVariables(XPathVariableBinding[] variables, string parameterName)
    {
        int index = 0;
        while (index < variables.Length)
        {
            XPathVariableBinding? variable = variables[index];
            if (variable == null || variable.Name == null || variable.Name.LocalName == null || variable.Name.LocalName.Length == 0)
            {
                throw new ArgumentException("Every captured variable must have a name.", parameterName);
            }

            if (variable.Value != null)
            {
                Sequences.ValidateXdmSequenceStorage(variable.Value);
            }

            int earlier = 0;
            while (earlier < index)
            {
                if (EqualXdmFunctionNames(variables[earlier].Name, variable.Name))
                {
                    throw new ArgumentException("A closure cannot capture the same variable name twice.", parameterName);
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal bool ContainsXPathVariableName(XPathVariableBinding[] variables, XmlName? name)
    {
        int index = 0;
        while (index < variables.Length)
        {
            if (EqualXdmFunctionNames(variables[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool EqualXdmFunctionNames(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal) && string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal);
    }

    internal XmlName? CopyXdmFunctionName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal XdmAtomicType[]? CopyXdmFunctionTypes(XdmAtomicType[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XdmAtomicType[] result = new XdmAtomicType[source.Length];
        Array.Copy(source, result, result.Length);
        return result;
    }

    internal XdmOccurrence[]? CopyXdmFunctionOccurrences(XdmOccurrence[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XdmOccurrence[] result = new XdmOccurrence[source.Length];
        Array.Copy(source, result, result.Length);
        return result;
    }

    internal string[]? CopyXdmFunctionStrings(string[]? source)
    {
        if (source == null)
        {
            return null;
        }

        string[] result = new string[source.Length];
        Array.Copy(source, result, result.Length);
        return result;
    }

    internal XPathTypeException CreateXdmFunctionTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }

    internal XPathDynamicException CreateXdmFunctionDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
