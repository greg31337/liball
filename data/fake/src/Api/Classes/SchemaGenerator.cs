using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace fake;
/// <summary>
/// Provides schema-driven generation operations.
/// </summary>
public class SchemaGenerator : ISchemaGenerator
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    SchemaRuntime RuntimeValue;
    internal SchemaGenerator(GenerationContext context, Fake fakeValue)
    {
        Context = context;
        RandomValue = context.ProviderRandom("schema");
        RuntimeValue = new SchemaRuntime(context, fakeValue, RandomValue);
    }

    /// <summary>Validates a schema without generating values or consuming deterministic state.</summary>
    /// <param name="schema">The schema to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="FakeException">Thrown when the schema contains invalid configuration, too many nodes, excessive depth, or a node cycle.</exception>
    public void Validate(FakeSchema schema)
    {
        if (schema == null)
            throw new System.ArgumentNullException("schema");
        ValidateFromRoot(schema.Root);
    }

    /// <summary>Validates a schema node tree without generating values or consuming deterministic state.</summary>
    /// <param name="root">The root node to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="FakeException">Thrown when the tree contains invalid configuration, too many nodes, excessive depth, or a node cycle.</exception>
    public void ValidateFromRoot(SchemaNode root)
    {
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Validates and deterministically generates one schema result.</summary>
    /// <remarks>Object nodes return insertion-ordered dictionaries and array nodes return object arrays. Derived and condition callbacks receive the partially generated local object. References resolve from prior local fields and then prior root fields.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation, reference resolution, sequence evaluation, or uniqueness fails.</exception>
    public object? Generate(FakeSchema schema)
    {
        if (schema == null)
            throw new System.ArgumentNullException("schema");
        return GenerateFromRoot(schema.Root);
    }

    /// <summary>Validates and deterministically generates one schema-node result.</summary>
    /// <remarks>Object nodes return insertion-ordered dictionaries and array nodes return object arrays. Derived and condition callbacks receive the partially generated local object. References resolve from prior local fields and then prior root fields.</remarks>
    /// <param name="root">The root node to generate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation, reference resolution, sequence evaluation, or uniqueness fails.</exception>
    public object? GenerateFromRoot(SchemaNode root)
    {
        if (root == null)
            throw new System.ArgumentNullException("root");
        return RuntimeValue.Generate(root);
    }

    /// <summary>Validates and deterministically generates one schema result with cancellation.</summary>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="cancellationToken">The token checked before and after evaluation.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object? GenerateFromSchemaAndCancellationToken(FakeSchema schema, CancellationToken cancellationToken)
    {
        SchemaNode root = RequireSchema(schema);
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        return GenerateValidatedFromRootAndCancellationToken(root, cancellationToken);
    }

    /// <summary>Generates a bounded number of schema results in evaluation order.</summary>
    /// <remarks>Each result receives a fresh correlation root while named sequence and uniqueness state continues across results and other operations.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to generate.</param>
    /// <returns>A new array containing the generated results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object?[] Many(FakeSchema schema, int count)
    {
        return ManyFromSchemaAndCountAndCancellationToken(
            schema,
            count,
            CancellationToken.None);
    }

    /// <summary>Generates a bounded number of schema results with cancellation.</summary>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to generate.</param>
    /// <param name="cancellationToken">The token checked before allocation and each delivery.</param>
    /// <returns>A new array containing the generated results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object?[] ManyFromSchemaAndCountAndCancellationToken(FakeSchema schema, int count, CancellationToken cancellationToken)
    {
        SchemaNode root = RequireSchema(schema);
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        cancellationToken.ThrowIfCancellationRequested();
        object?[] result = new object?[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = GenerateValidatedFromRootAndCancellationToken(
                root,
                cancellationToken);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Generates a bounded number of schema results and delivers them to a callback in evaluation order.</summary>
    /// <remarks>If generation or delivery fails, completed deliveries remain observable and the exception is propagated.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to deliver.</param>
    /// <param name="callback">The callback invoked once for each generated result.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public void Deliver(FakeSchema schema, int count, Action<object?> callback)
    {
        DeliverFromSchemaAndCountAndCallbackAndCancellationToken(
            schema,
            count,
            callback,
            CancellationToken.None);
    }

    /// <summary>Generates and delivers a bounded number of schema results with cancellation.</summary>
    /// <remarks>If generation, delivery, or cancellation fails, completed deliveries remain observable and the exception is propagated.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to deliver.</param>
    /// <param name="callback">The callback invoked once for each generated result.</param>
    /// <param name="cancellationToken">The token checked before generation and each delivery.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public void DeliverFromSchemaAndCountAndCallbackAndCancellationToken(FakeSchema schema, int count, Action<object?> callback, CancellationToken cancellationToken)
    {
        SchemaNode root = RequireSchema(schema);
        if (callback == null)
            throw new ArgumentNullException("callback");
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        cancellationToken.ThrowIfCancellationRequested();
        int index = 0;
        while (index < count)
        {
            object? value = GenerateValidatedFromRootAndCancellationToken(
                root,
                cancellationToken);
            callback(value);
            index = index + 1;
        }
    }

    /// <summary>Fills an existing one-dimensional array with schema results.</summary>
    /// <remarks>If generation or assignment fails, the completed destination prefix remains filled and the exception is propagated.</remarks>
    /// <param name="values">The array to fill in ascending index order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The same array after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>values</c> is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public Array Fill(Array values, FakeSchema schema)
    {
        return FillFromArrayAndSchemaAndCancellationToken(
            values,
            schema,
            CancellationToken.None);
    }

    /// <summary>Fills an existing one-dimensional array with schema results and cancellation.</summary>
    /// <param name="values">The array to fill in ascending index order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="cancellationToken">The token checked before generation and each assignment.</param>
    /// <returns>The same array after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>values</c> is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public Array FillFromArrayAndSchemaAndCancellationToken(Array values, FakeSchema schema, CancellationToken cancellationToken)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        SchemaNode root = RequireSchema(schema);
        if (values.Rank != 1)
            throw new ArgumentException(
                "values must be a one-dimensional array.",
                "values");
        Guard.Count(
            values.Length,
            Context.CapturedOptions.MaximumCount,
            "values");
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        cancellationToken.ThrowIfCancellationRequested();
        int lowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < values.Length)
        {
            object? value = GenerateValidatedFromRootAndCancellationToken(
                root,
                cancellationToken);
            values.SetValue(value, lowerBound + index);
            index = index + 1;
        }
        return values;
    }

    /// <summary>Fills the existing positions of a non-generic list with schema results.</summary>
    /// <remarks>The list count is not changed. If generation or assignment fails, the completed prefix remains filled and the exception is propagated.</remarks>
    /// <param name="values">The list whose existing positions are filled in ascending order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The same list after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public IList FillFromListAndSchema(IList values, FakeSchema schema)
    {
        return FillFromListAndSchemaAndCancellationToken(
            values,
            schema,
            CancellationToken.None);
    }

    /// <summary>Fills the existing positions of a non-generic list with schema results and cancellation.</summary>
    /// <param name="values">The list whose existing positions are filled in ascending order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="cancellationToken">The token checked before generation and each assignment.</param>
    /// <returns>The same list after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public IList FillFromListAndSchemaAndCancellationToken(IList values, FakeSchema schema, CancellationToken cancellationToken)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        SchemaNode root = RequireSchema(schema);
        int count = values.Count;
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "values");
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        cancellationToken.ThrowIfCancellationRequested();
        int index = 0;
        while (index < count)
        {
            object? value = GenerateValidatedFromRootAndCancellationToken(
                root,
                cancellationToken);
            values[index] = value;
            index = index + 1;
        }
        return values;
    }

    /// <summary>Returns a deterministic zero-based schema page without advancing this generator.</summary>
    /// <remarks>The page is replayed from the captured seed in an isolated generation context. Correlation state resets for each record, while sequence and uniqueness state advances across records within the replay.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of results in the page.</param>
    /// <returns>A new array containing the requested page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument or the page extent exceeds configured count limits.</exception>
    /// <exception cref="NotSupportedException">Thrown when cryptographic random mode prevents deterministic replay.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object?[] Page(FakeSchema schema, long pageIndex, int pageSize)
    {
        return PageFromSchemaAndPageIndexAndPageSizeAndCancellationToken(
            schema,
            pageIndex,
            pageSize,
            CancellationToken.None);
    }

    /// <summary>Returns a deterministic zero-based schema page with cancellation and without advancing this generator.</summary>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of results in the page.</param>
    /// <param name="cancellationToken">The token checked before replay and each generated result.</param>
    /// <returns>A new array containing the requested page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument or the page extent exceeds configured count limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="NotSupportedException">Thrown when cryptographic random mode prevents deterministic replay.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object?[] PageFromSchemaAndPageIndexAndPageSizeAndCancellationToken(FakeSchema schema, long pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        SchemaNode root = RequireSchema(schema);
        if (pageIndex < 0L)
        {
            throw new ArgumentOutOfRangeException(
                "pageIndex",
                pageIndex,
                "pageIndex must be zero or greater.");
        }
        Guard.Count(
            pageSize,
            Context.CapturedOptions.MaximumCount,
            "pageSize");
        if (pageSize > 0 &&
            pageIndex >
                ((long)Context.CapturedOptions.MaximumCount - pageSize) /
                pageSize)
        {
            throw new ArgumentOutOfRangeException(
                "pageIndex",
                pageIndex,
                "The page extent exceeds the configured count limit.");
        }
        SchemaValidator.Validate(root, Context.CapturedOptions.MaximumCount);
        cancellationToken.ThrowIfCancellationRequested();
        object?[] result = new object?[pageSize];
        if (pageSize == 0)
            return result;
        if (Context.CapturedOptions.RandomMode == RandomMode.Cryptographic)
        {
            throw new NotSupportedException(
                "Deterministic schema paging is not supported in cryptographic random mode.");
        }

        Fake replayFake = new Fake(Context.OptionsSnapshot());
        replayFake.Context.LocaleRegistrations =
            Context.LocaleRegistrations.SnapshotStore();
        SchemaGenerator replay = replayFake.Schema();
        long skip = pageIndex * pageSize;
        long skipped = 0L;
        while (skipped < skip)
        {
            replay.GenerateValidatedFromRootAndCancellationToken(
                root,
                cancellationToken);
            skipped = skipped + 1L;
        }
        int index = 0;
        while (index < result.Length)
        {
            result[index] =
                replay.GenerateValidatedFromRootAndCancellationToken(
                    root,
                    cancellationToken);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Evaluates a schema as an alias for <see cref="Generate(FakeSchema)"/>.</summary>
    /// <param name="schema">The schema to evaluate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object? Evaluate(FakeSchema schema)
    {
        return Generate(schema);
    }

    /// <summary>Evaluates a schema node as an alias for <see cref="GenerateFromRoot(SchemaNode)"/>.</summary>
    /// <param name="root">The root node to evaluate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    public object? EvaluateFromRoot(SchemaNode root)
    {
        return GenerateFromRoot(root);
    }

    /// <summary>Creates a scalar callback node.</summary>
    /// <param name="generate">The callback that receives the owning <see cref="Fake"/> during evaluation.</param>
    /// <returns>A scalar schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    public SchemaNode Scalar(System.Func<Fake, object?> generate)
    {
        if (generate == null)
            throw new System.ArgumentNullException("generate");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Scalar);
        result.ScalarFactory = generate;
        return result;
    }

    /// <summary>Creates a scalar node from a parameterless callback.</summary>
    /// <param name="generate">The callback invoked during evaluation.</param>
    /// <returns>A scalar schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    public SchemaNode ScalarFromGenerate(System.Func<object?> generate)
    {
        if (generate == null)
            throw new System.ArgumentNullException("generate");
        System.Func<Fake, object?> adapter = value => generate();
        return Scalar(adapter);
    }

    /// <summary>Creates one named object field.</summary>
    /// <param name="name">The nonempty field name.</param>
    /// <param name="node">The node evaluated for the field.</param>
    /// <returns>A schema field.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public SchemaField Field(string name, SchemaNode node)
    {
        return new SchemaField(name, node);
    }

    /// <summary>Creates an object node with deterministic field order.</summary>
    /// <param name="fields">The ordered named fields to snapshot.</param>
    /// <returns>An object schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>fields</c> or one of its entries is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the field count exceeds configured limits.</exception>
    public SchemaNode Object(SchemaField[] fields)
    {
        if (fields == null)
            throw new System.ArgumentNullException("fields");
        Guard.Count(fields.Length, Context.CapturedOptions.MaximumCount, "fields");
        SchemaField[] snapshot = new SchemaField[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] == null)
                throw new System.ArgumentException("fields cannot contain null at index " + index + ".", "fields");
            snapshot[index] = fields[index];
            index = index + 1;
        }

        SchemaNode result = new SchemaNode(SchemaNodeKind.Object);
        result.Fields = snapshot;
        return result;
    }

    /// <summary>Creates a fixed-count array node.</summary>
    /// <param name="item">The node evaluated for every array item.</param>
    /// <param name="count">The exact item count.</param>
    /// <returns>An array schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    public SchemaNode Array(SchemaNode item, int count)
    {
        return ArrayFromItemAndMinimumCountAndMaximumCount(item, count, count);
    }

    /// <summary>Creates a bounded variable-count array node.</summary>
    /// <param name="item">The node evaluated for every array item.</param>
    /// <param name="minimumCount">The inclusive minimum item count.</param>
    /// <param name="maximumCount">The inclusive maximum item count.</param>
    /// <returns>An array schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is invalid or exceeds configured limits.</exception>
    public SchemaNode ArrayFromItemAndMinimumCountAndMaximumCount(SchemaNode item, int minimumCount, int maximumCount)
    {
        if (item == null)
            throw new System.ArgumentNullException("item");
        Guard.Count(minimumCount, Context.CapturedOptions.MaximumCount, "minimumCount");
        Guard.Count(maximumCount, Context.CapturedOptions.MaximumCount, "maximumCount");
        if (maximumCount < minimumCount)
            throw new System.ArgumentOutOfRangeException("maximumCount", maximumCount, "maximumCount must be greater than or equal to minimumCount.");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Array);
        result.Item = item;
        result.MinimumCount = minimumCount;
        result.MaximumCount = maximumCount;
        return result;
    }

    /// <summary>Creates a constant-value node.</summary>
    /// <param name="value">The value returned during evaluation, including null.</param>
    /// <returns>A constant schema node.</returns>
    public SchemaNode Constant(object? value)
    {
        SchemaNode result = new SchemaNode(SchemaNodeKind.Constant);
        result.ConstantValue = value;
        return result;
    }

    /// <summary>Creates a reference to a previously evaluated field path.</summary>
    /// <param name="fieldPath">The dot-separated field path.</param>
    /// <returns>A reference schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>fieldPath</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>fieldPath</c> is malformed.</exception>
    public SchemaNode Reference(string fieldPath)
    {
        string path = FakeRuleSet.ValidateMemberPath(fieldPath, "fieldPath");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Reference);
        result.ReferencePath = path;
        return result;
    }

    /// <summary>Creates a node derived from the current correlation context.</summary>
    /// <param name="derive">The callback invoked with the current generated object context.</param>
    /// <returns>A derived schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>derive</c> is null.</exception>
    public SchemaNode Derived(System.Func<object, object?> derive)
    {
        if (derive == null)
            throw new System.ArgumentNullException("derive");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Derived);
        result.Deriver = derive;
        return result;
    }

    /// <summary>Creates a two-branch condition node.</summary>
    /// <param name="predicate">The callback that inspects the current correlation context.</param>
    /// <param name="whenTrue">The node evaluated when the predicate returns true.</param>
    /// <param name="whenFalse">The node evaluated when the predicate returns false.</param>
    /// <returns>A condition schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    public SchemaNode Conditional(System.Func<object, bool> predicate, SchemaNode whenTrue, SchemaNode whenFalse)
    {
        if (predicate == null)
            throw new System.ArgumentNullException("predicate");
        if (whenTrue == null)
            throw new System.ArgumentNullException("whenTrue");
        if (whenFalse == null)
            throw new System.ArgumentNullException("whenFalse");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Condition);
        result.Predicate = predicate;
        result.WhenTrue = whenTrue;
        result.WhenFalse = whenFalse;
        return result;
    }

    /// <summary>Creates a two-branch condition node.</summary>
    /// <remarks>This is an alias for <see cref="Conditional(System.Func{object, bool}, SchemaNode, SchemaNode)"/>.</remarks>
    /// <param name="predicate">The callback that inspects the current correlation context.</param>
    /// <param name="whenTrue">The node evaluated when the predicate returns true.</param>
    /// <param name="whenFalse">The node evaluated when the predicate returns false.</param>
    /// <returns>A condition schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    public SchemaNode Condition(System.Func<object, bool> predicate, SchemaNode whenTrue, SchemaNode whenFalse)
    {
        return Conditional(predicate, whenTrue, whenFalse);
    }

    /// <summary>Creates one weighted schema choice.</summary>
    /// <param name="node">The node evaluated when selected.</param>
    /// <param name="weight">A finite nonnegative relative weight.</param>
    /// <returns>A weighted schema variant.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>weight</c> is negative or non-finite.</exception>
    public SchemaVariant Choice(SchemaNode node, double weight)
    {
        return new SchemaVariant(node, weight);
    }

    /// <summary>Creates a weighted variant node.</summary>
    /// <param name="variants">The weighted choices to snapshot in deterministic order.</param>
    /// <returns>A weighted variant schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>variants</c> or one of its entries is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when no variants are supplied or their total weight is not positive and finite.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the variant count exceeds configured limits.</exception>
    public SchemaNode Variant(SchemaVariant[] variants)
    {
        if (variants == null)
            throw new System.ArgumentNullException("variants");
        Guard.Count(variants.Length, Context.CapturedOptions.MaximumCount, "variants");
        if (variants.Length == 0)
            throw new System.ArgumentException("variants must contain at least one choice.", "variants");
        SchemaVariant[] snapshot = new SchemaVariant[variants.Length];
        double total = 0.0;
        int index = 0;
        while (index < variants.Length)
        {
            if (variants[index] == null)
                throw new System.ArgumentException("variants cannot contain null at index " + index + ".", "variants");
            snapshot[index] = variants[index];
            total = total + variants[index].Weight;
            index = index + 1;
        }

        if (total <= 0.0 || System.Double.IsInfinity(total))
            throw new System.ArgumentException("variants must have a positive finite total weight.", "variants");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Variant);
        result.Variants = snapshot;
        return result;
    }

    /// <summary>Creates a named sequence node starting at zero with an increment of one.</summary>
    /// <param name="name">The nonempty sequence-state name.</param>
    /// <returns>A sequence schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public SchemaNode Sequence(string name)
    {
        return SequenceFromNameAndStartAndIncrement(name, 0L, 1L);
    }

    /// <summary>Creates a named signed-integer sequence node.</summary>
    /// <param name="name">The nonempty sequence-state name.</param>
    /// <param name="start">The first value returned by the sequence.</param>
    /// <param name="increment">The nonzero value added after each result.</param>
    /// <returns>A sequence schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    public SchemaNode SequenceFromNameAndStartAndIncrement(string name, long start, long increment)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        if (increment == 0L)
            throw new System.ArgumentOutOfRangeException("increment", increment, "increment must not be zero.");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Sequence);
        result.StateName = name;
        result.SequenceStart = start;
        result.SequenceIncrement = increment;
        return result;
    }

    /// <summary>Creates a named uniqueness node using the configured retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until it produces an unused value.</param>
    /// <returns>A unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public SchemaNode Unique(string name, SchemaNode source)
    {
        return UniqueNode(name, source, null, Context.CapturedOptions.UniquenessRetryLimit);
    }

    /// <summary>Creates a named uniqueness node with an explicit retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until it produces an unused value.</param>
    /// <param name="retryLimit">The positive attempts allowed for one result.</param>
    /// <returns>A unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>retryLimit</c> is not positive.</exception>
    public SchemaNode UniqueFromNameAndSourceAndRetryLimit(string name, SchemaNode source, int retryLimit)
    {
        return UniqueNode(name, source, null, retryLimit);
    }

    /// <summary>Creates a named composite-uniqueness node using the configured retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until its selected key is unused.</param>
    /// <param name="compositeKey">The callback selecting ordered string key parts from each candidate.</param>
    /// <returns>A composite unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public SchemaNode UniqueFromNameAndSourceAndCompositeKey(string name, SchemaNode source, System.Func<object?, string[]> compositeKey)
    {
        return UniqueFromNameAndSourceAndCompositeKeyAndRetryLimit(name, source, compositeKey, Context.CapturedOptions.UniquenessRetryLimit);
    }

    /// <summary>Creates a named composite-uniqueness node with an explicit retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until its selected key is unused.</param>
    /// <param name="compositeKey">The callback selecting ordered string key parts from each candidate.</param>
    /// <param name="retryLimit">The positive attempts allowed for one result.</param>
    /// <returns>A composite unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>retryLimit</c> is not positive.</exception>
    public SchemaNode UniqueFromNameAndSourceAndCompositeKeyAndRetryLimit(string name, SchemaNode source, System.Func<object?, string[]> compositeKey, int retryLimit)
    {
        if (compositeKey == null)
            throw new System.ArgumentNullException("compositeKey");
        return UniqueNode(name, source, compositeKey, retryLimit);
    }

    SchemaNode UniqueNode(string name, SchemaNode source, System.Func<object?, string[]>? compositeKey, int retryLimit)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        if (source == null)
            throw new System.ArgumentNullException("source");
        Guard.Positive(retryLimit, "retryLimit");
        SchemaNode result = new SchemaNode(SchemaNodeKind.Unique);
        result.StateName = name;
        result.Source = source;
        result.UniqueKeySelector = compositeKey;
        result.RetryLimit = retryLimit;
        return result;
    }

    /// <summary>Resets a named sequence shared by schema, profile, and random generation.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetSequence(string name)
    {
        Context.ResetSequence(name);
    }

    /// <summary>Resets every named sequence in this generation context.</summary>
    public void ResetSequenceFromNoArguments()
    {
        Context.ResetSequenceFromNoArguments();
    }

    /// <summary>Resets a named uniqueness scope shared by schema, profile, and random generation.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetUnique(string name)
    {
        Context.ResetUnique(name);
    }

    /// <summary>Resets every named uniqueness scope in this generation context.</summary>
    public void ResetUniqueFromNoArguments()
    {
        Context.ResetUniqueFromNoArguments();
    }

    /// <summary>Wraps a schema root for generation operations.</summary>
    /// <param name="root">The non-null root node.</param>
    /// <returns>A fake schema containing the root.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    public FakeSchema Schema(SchemaNode root)
    {
        return new FakeSchema(root);
    }

    SchemaNode RequireSchema(FakeSchema schema)
    {
        if (schema == null)
            throw new ArgumentNullException("schema");
        return schema.Root;
    }

    object? GenerateValidatedFromRootAndCancellationToken(
        SchemaNode root,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        object? result = RuntimeValue.GenerateValidated(root);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
