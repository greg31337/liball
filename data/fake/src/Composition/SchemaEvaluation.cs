using System;
using System.Collections.Generic;
using System.Reflection;

namespace fake;

internal class SchemaValidationState
{
    internal int MaximumCount;
    internal int NodeCount;
    internal HashSet<SchemaNode> Active;
    internal HashSet<SchemaNode> Visited;

    internal SchemaValidationState(int maximumCount)
    {
        MaximumCount = maximumCount;
        NodeCount = 0;
        Active = new HashSet<SchemaNode>();
        Visited = new HashSet<SchemaNode>();
    }
}

internal static class SchemaValidator
{
    internal const int MaximumDepth = 128;

    internal static void Validate(SchemaNode root, int maximumCount)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        SchemaValidationState state = new SchemaValidationState(maximumCount);
        ValidateNode(root, state, 0, "$", null);
    }

    static void ValidateNode(
        SchemaNode? node,
        SchemaValidationState state,
        int depth,
        string path,
        string? fieldName)
    {
        if (node == null)
            throw Invalid(path, "contains a null node");
        if (depth > MaximumDepth)
            throw Invalid(path, "exceeds the maximum schema depth of " + MaximumDepth);
        if (state.Active.Contains(node))
            throw Invalid(path, "forms a schema node cycle");
        if (state.Visited.Contains(node))
            return;
        state.NodeCount = state.NodeCount + 1;
        if (state.NodeCount > state.MaximumCount)
            throw Invalid(path, "exceeds the configured schema node limit of " + state.MaximumCount);
        state.Active.Add(node);
        switch (node.Kind)
        {
            case SchemaNodeKind.Scalar:
                if (node.ScalarFactory == null)
                    throw Invalid(path, "has no scalar callback");
                break;
            case SchemaNodeKind.Object:
                ValidateObject(node, state, depth, path);
                break;
            case SchemaNodeKind.Array:
                ValidateArray(node, state, depth, path);
                break;
            case SchemaNodeKind.Constant:
                break;
            case SchemaNodeKind.Reference:
                ValidateReference(node, path);
                break;
            case SchemaNodeKind.Derived:
                if (node.Deriver == null)
                    throw Invalid(path, "has no derived callback");
                break;
            case SchemaNodeKind.Condition:
                ValidateCondition(node, state, depth, path);
                break;
            case SchemaNodeKind.Variant:
                ValidateVariant(node, state, depth, path);
                break;
            case SchemaNodeKind.Sequence:
                ValidateSequence(node, path);
                break;
            case SchemaNodeKind.Unique:
                ValidateUnique(node, state, depth, path);
                break;
            default:
                throw Invalid(path, "has an unsupported node kind");
        }
        state.Active.Remove(node);
        state.Visited.Add(node);
    }

    static void ValidateObject(
        SchemaNode node,
        SchemaValidationState state,
        int depth,
        string path)
    {
        if (node.Fields == null)
            throw Invalid(path, "has a null field collection");
        if (node.Fields.Length > state.MaximumCount)
            throw Invalid(path, "exceeds the configured field limit of " + state.MaximumCount);
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < node.Fields.Length)
        {
            SchemaField field = node.Fields[index];
            string fieldPath = path + ".fields[" + index + "]";
            if (field == null)
                throw Invalid(fieldPath, "is null");
            if (String.IsNullOrWhiteSpace(field.Name))
                throw Invalid(fieldPath, "has an empty field name");
            if (!names.Add(field.Name))
                throw Invalid(fieldPath, "duplicates field '" + field.Name + "'");
            ValidateNode(
                field.Node,
                state,
                depth + 1,
                path + "." + field.Name,
                field.Name);
            index = index + 1;
        }
    }

    static void ValidateArray(
        SchemaNode node,
        SchemaValidationState state,
        int depth,
        string path)
    {
        if (node.MinimumCount < 0)
            throw Invalid(path, "has a negative minimum array count");
        if (node.MaximumCount < node.MinimumCount)
            throw Invalid(path, "has an array maximum below its minimum");
        if (node.MaximumCount > state.MaximumCount)
            throw Invalid(path, "exceeds the configured array limit of " + state.MaximumCount);
        ValidateNode(node.Item, state, depth + 1, path + "[]", null);
    }

    static void ValidateReference(SchemaNode node, string path)
    {
        if (String.IsNullOrWhiteSpace(node.ReferencePath))
            throw Invalid(path, "has an empty reference path");
        int segmentLength = 0;
        int index = 0;
        while (index < node.ReferencePath.Length)
        {
            char character = node.ReferencePath[index];
            if (character == '.')
            {
                if (segmentLength == 0)
                    throw Invalid(path, "has an invalid reference path");
                segmentLength = 0;
            }
            else
            {
                if (Char.IsWhiteSpace(character) || Char.IsControl(character))
                    throw Invalid(path, "has an invalid reference path");
                segmentLength = segmentLength + 1;
            }
            index = index + 1;
        }
        if (segmentLength == 0)
            throw Invalid(path, "has an invalid reference path");
    }

    static void ValidateCondition(
        SchemaNode node,
        SchemaValidationState state,
        int depth,
        string path)
    {
        if (node.Predicate == null)
            throw Invalid(path, "has no condition predicate");
        ValidateNode(
            node.WhenTrue,
            state,
            depth + 1,
            path + ".whenTrue",
            null);
        ValidateNode(
            node.WhenFalse,
            state,
            depth + 1,
            path + ".whenFalse",
            null);
    }

    static void ValidateVariant(
        SchemaNode node,
        SchemaValidationState state,
        int depth,
        string path)
    {
        if (node.Variants == null)
            throw Invalid(path, "has a null variant collection");
        if (node.Variants.Length == 0)
            throw Invalid(path, "has no variants");
        if (node.Variants.Length > state.MaximumCount)
            throw Invalid(path, "exceeds the configured variant limit of " + state.MaximumCount);
        double total = 0.0;
        int index = 0;
        while (index < node.Variants.Length)
        {
            SchemaVariant variant = node.Variants[index];
            string variantPath = path + ".variants[" + index + "]";
            if (variant == null)
                throw Invalid(variantPath, "is null");
            if (Double.IsNaN(variant.Weight) ||
                Double.IsInfinity(variant.Weight) ||
                variant.Weight < 0.0)
                throw Invalid(variantPath, "has an invalid weight");
            total = total + variant.Weight;
            ValidateNode(
                variant.Node,
                state,
                depth + 1,
                variantPath,
                null);
            index = index + 1;
        }
        if (total <= 0.0 || Double.IsInfinity(total))
            throw Invalid(path, "does not have a positive finite total weight");
    }

    static void ValidateSequence(SchemaNode node, string path)
    {
        if (String.IsNullOrWhiteSpace(node.StateName))
            throw Invalid(path, "has an empty sequence name");
        if (node.SequenceIncrement == 0L)
            throw Invalid(path, "has a zero sequence increment");
    }

    static void ValidateUnique(
        SchemaNode node,
        SchemaValidationState state,
        int depth,
        string path)
    {
        if (String.IsNullOrWhiteSpace(node.StateName))
            throw Invalid(path, "has an empty uniqueness name");
        if (node.RetryLimit <= 0)
            throw Invalid(path, "has a nonpositive uniqueness retry limit");
        ValidateNode(node.Source, state, depth + 1, path + ".source", null);
    }

    static FakeException Invalid(string path, string reason)
    {
        return FakeException.Create("Schema node '" + path + "' " + reason + ".");
    }
}

internal class SchemaReferenceResult
{
    internal bool Found;
    internal object? Value;
}

internal static class SchemaReferenceResolver
{
    static readonly Type DictionaryType = new Dictionary<string, object?>().GetType();

    internal static object? Resolve(
        string path,
        Dictionary<string, object?> current,
        Dictionary<string, object?> root)
    {
        SchemaReferenceResult result = ResolveFrom(path, current);
        if (!result.Found && current != root)
            result = ResolveFrom(path, root);
        if (!result.Found)
            throw FakeException.Create("Schema reference '" + path + "' could not be resolved from previously evaluated values.");
        return result.Value;
    }

    static SchemaReferenceResult ResolveFrom(
        string path,
        Dictionary<string, object?> context)
    {
        SchemaReferenceResult result = new SchemaReferenceResult();
        result.Found = false;
        result.Value = null;
        if (context == null)
            return result;
        string[] segments = path.Split('.');
        object? value = context;
        int index = 0;
        while (index < segments.Length)
        {
            result = Read(value, segments[index]);
            if (!result.Found)
                return result;
            value = result.Value;
            if (value == null && index + 1 < segments.Length)
            {
                result.Found = false;
                return result;
            }
            index = index + 1;
        }
        result.Value = value;
        return result;
    }

    static SchemaReferenceResult Read(object? target, string name)
    {
        SchemaReferenceResult result = new SchemaReferenceResult();
        result.Found = false;
        result.Value = null;
        if (target == null)
            return result;
        Type type = target.GetType();
        if (type == DictionaryType)
        {
            Dictionary<string, object?> values = (Dictionary<string, object?>)target;
            if (values.ContainsKey(name))
            {
                result.Found = true;
                result.Value = values[name];
            }
            return result;
        }
        FieldInfo? field = type.GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public);
        if (field != null)
        {
            result.Found = true;
            result.Value = field.GetValue(target);
            return result;
        }
        PropertyInfo? property = type.GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public);
        if (property != null &&
            property.GetGetMethod() != null &&
            property.GetIndexParameters().Length == 0)
        {
            result.Found = true;
            result.Value = property.GetValue(target, null);
        }
        return result;
    }
}

internal class SchemaEvaluationState
{
    internal int MaximumOutputSize;
    internal int OutputSize;
    internal Dictionary<string, object?> Root;

    internal SchemaEvaluationState(int maximumOutputSize)
    {
        MaximumOutputSize = maximumOutputSize;
        OutputSize = 0;
        Root = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    internal void Consume(long amount)
    {
        OutputSize = Guard.OutputSize(
            (long)OutputSize + amount,
            MaximumOutputSize,
            "schema");
    }
}

internal static class SchemaOutputSize
{
    internal static long Measure(object? value)
    {
        HashSet<object> active = new HashSet<object>(
            ReferenceEqualityComparer.Instance);
        return Measure(value, 0, active);
    }

    static long Measure(
        object? value,
        int depth,
        HashSet<object> active)
    {
        if (value == null)
            return 1L;
        if (value.GetType() == String.Empty.GetType())
            return ((string)value).Length;
        if (!value.GetType().IsArray)
            return 1L;
        if (depth > SchemaValidator.MaximumDepth)
            throw FakeException.Create("A generated schema value exceeds the maximum output depth of " + SchemaValidator.MaximumDepth + ".");
        if (!active.Add(value))
            throw FakeException.Create("A generated schema value contains an array cycle.");
        Array values = (Array)value;
        long size = 1L;
        int index = 0;
        while (index < values.Length)
        {
            size = size + Measure(values.GetValue(index), depth + 1, active);
            index = index + 1;
        }
        active.Remove(value);
        return size;
    }
}

internal class SchemaRuntime
{
    GenerationContext Context;
    Fake FakeValue;
    FakeRandom RandomValue;

    internal SchemaRuntime(
        GenerationContext context,
        Fake fakeValue,
        FakeRandom randomValue)
    {
        Context = context;
        FakeValue = fakeValue;
        RandomValue = randomValue;
    }

    internal object? Generate(SchemaNode root)
    {
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        return GenerateValidated(root);
    }

    internal object? GenerateValidated(SchemaNode root)
    {
        SchemaEvaluationState state = new SchemaEvaluationState(
            Context.CapturedOptions.MaximumOutputSize);
        return Evaluate(root, state, state.Root, 0, true);
    }

    object? Evaluate(
        SchemaNode node,
        SchemaEvaluationState state,
        Dictionary<string, object?> current,
        int depth,
        bool rootPosition)
    {
        if (depth > SchemaValidator.MaximumDepth)
            throw FakeException.Create("Schema evaluation exceeds the maximum depth of " + SchemaValidator.MaximumDepth + ".");
        switch (node.Kind)
        {
            case SchemaNodeKind.Scalar:
                if (node.ScalarFactory == null)
                    throw FakeException.Create("A scalar schema node has no callback.");
                return Terminal(node.ScalarFactory(FakeValue), state);
            case SchemaNodeKind.Object:
                return EvaluateObject(node, state, depth, rootPosition);
            case SchemaNodeKind.Array:
                return EvaluateArray(node, state, current, depth);
            case SchemaNodeKind.Constant:
                return Terminal(node.ConstantValue, state);
            case SchemaNodeKind.Reference:
                if (node.ReferencePath == null)
                    throw FakeException.Create("A reference schema node has no path.");
                return Terminal(
                    SchemaReferenceResolver.Resolve(
                        node.ReferencePath,
                        current,
                        state.Root),
                    state);
            case SchemaNodeKind.Derived:
                if (node.Deriver == null)
                    throw FakeException.Create("A derived schema node has no callback.");
                return Terminal(node.Deriver(current), state);
            case SchemaNodeKind.Condition:
                return EvaluateCondition(
                    node,
                    state,
                    current,
                    depth,
                    rootPosition);
            case SchemaNodeKind.Variant:
                return EvaluateVariant(
                    node,
                    state,
                    current,
                    depth,
                    rootPosition);
            case SchemaNodeKind.Sequence:
                return Terminal(EvaluateSequence(node), state);
            case SchemaNodeKind.Unique:
                return EvaluateUnique(
                    node,
                    state,
                    current,
                    depth,
                    rootPosition);
            default:
                throw FakeException.Create("Schema evaluation encountered an unsupported node kind.");
        }
    }

    object EvaluateObject(
        SchemaNode node,
        SchemaEvaluationState state,
        int depth,
        bool rootPosition)
    {
        Dictionary<string, object?> result;
        if (rootPosition)
            result = state.Root;
        else
            result = new Dictionary<string, object?>(StringComparer.Ordinal);
        state.Consume(1L);
        int index = 0;
        while (index < node.Fields.Length)
        {
            SchemaField field = node.Fields[index];
            state.Consume(field.Name.Length);
            result[field.Name] = Evaluate(
                field.Node,
                state,
                result,
                depth + 1,
                false);
            index = index + 1;
        }
        return result;
    }

    object? EvaluateArray(
        SchemaNode node,
        SchemaEvaluationState state,
        Dictionary<string, object?> current,
        int depth)
    {
        int count = node.MinimumCount;
        if (node.MaximumCount != node.MinimumCount)
            count = RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(
                node.MinimumCount,
                node.MaximumCount,
                true);
        object?[] result = new object?[count];
        SchemaNode? item = node.Item;
        if (item == null)
            throw FakeException.Create("An array schema node has no item.");
        state.Consume(1L);
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Evaluate(
                item,
                state,
                current,
                depth + 1,
                false);
            index = index + 1;
        }
        return result;
    }

    object? EvaluateCondition(
        SchemaNode node,
        SchemaEvaluationState state,
        Dictionary<string, object?> current,
        int depth,
        bool rootPosition)
    {
        if (node.Predicate == null || node.WhenTrue == null || node.WhenFalse == null)
            throw FakeException.Create("A condition schema node is incomplete.");
        if (node.Predicate(current))
            return Evaluate(
                node.WhenTrue,
                state,
                current,
                depth + 1,
                rootPosition);
        return Evaluate(
            node.WhenFalse,
            state,
            current,
            depth + 1,
            rootPosition);
    }

    object? EvaluateVariant(
        SchemaNode node,
        SchemaEvaluationState state,
        Dictionary<string, object?> current,
        int depth,
        bool rootPosition)
    {
        double[] weights = new double[node.Variants.Length];
        int index = 0;
        while (index < weights.Length)
        {
            weights[index] = node.Variants[index].Weight;
            index = index + 1;
        }
        int selected = Selection.WeightedChoiceIndex(
            RandomValue,
            weights.Length,
            weights);
        return Evaluate(
            node.Variants[selected].Node,
            state,
            current,
            depth + 1,
            rootPosition);
    }

    object EvaluateSequence(SchemaNode node)
    {
        if (node.StateName == null)
            throw FakeException.Create("A sequence schema node has no state name.");
        return Context.GetSequenceScope(
            node.StateName,
            node.SequenceStart,
            node.SequenceIncrement).Next();
    }

    object? EvaluateUnique(
        SchemaNode node,
        SchemaEvaluationState state,
        Dictionary<string, object?> current,
        int depth,
        bool rootPosition)
    {
        if (node.StateName == null || node.Source == null)
            throw FakeException.Create("A unique schema node is incomplete.");
        UniqueScope scope = Context.GetUniqueScope(node.StateName);
        int initialSize = state.OutputSize;
        int attempt = 0;
        while (attempt < node.RetryLimit)
        {
            state.OutputSize = initialSize;
            object? value = Evaluate(
                node.Source,
                state,
                current,
                depth + 1,
                rootPosition);
            int remaining = node.RetryLimit - attempt - 1;
            bool accepted;
            if (node.UniqueKeySelector == null)
                accepted = scope.AcceptObject(value, remaining);
            else
                accepted = scope.AcceptComposite(
                    node.UniqueKeySelector(value),
                    remaining);
            if (accepted)
                return value;
            attempt = attempt + 1;
        }
        state.OutputSize = initialSize;
        throw FakeException.Create("Schema uniqueness scope '" + node.StateName + "' exhausted its retry limit after " + node.RetryLimit + " attempts.");
    }

    static object? Terminal(object? value, SchemaEvaluationState state)
    {
        state.Consume(SchemaOutputSize.Measure(value));
        return value;
    }
}
