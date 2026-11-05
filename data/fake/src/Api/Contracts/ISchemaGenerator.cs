using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="SchemaGenerator"/>.</summary>
public interface ISchemaGenerator
{
    /// <summary>Validates a schema without generating values or consuming deterministic state.</summary>
    /// <param name="schema">The schema to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="FakeException">Thrown when the schema contains invalid configuration, too many nodes, excessive depth, or a node cycle.</exception>
    void Validate(FakeSchema schema);
    /// <summary>Validates a schema node tree without generating values or consuming deterministic state.</summary>
    /// <param name="root">The root node to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="FakeException">Thrown when the tree contains invalid configuration, too many nodes, excessive depth, or a node cycle.</exception>
    void ValidateFromRoot(SchemaNode root);
    /// <summary>Validates and deterministically generates one schema result.</summary>
    /// <remarks>Object nodes return insertion-ordered dictionaries and array nodes return object arrays. Derived and condition callbacks receive the partially generated local object. References resolve from prior local fields and then prior root fields.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation, reference resolution, sequence evaluation, or uniqueness fails.</exception>
    object? Generate(FakeSchema schema);
    /// <summary>Validates and deterministically generates one schema-node result.</summary>
    /// <remarks>Object nodes return insertion-ordered dictionaries and array nodes return object arrays. Derived and condition callbacks receive the partially generated local object. References resolve from prior local fields and then prior root fields.</remarks>
    /// <param name="root">The root node to generate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation, reference resolution, sequence evaluation, or uniqueness fails.</exception>
    object? GenerateFromRoot(SchemaNode root);
    /// <summary>Validates and deterministically generates one schema result with cancellation.</summary>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="cancellationToken">The token checked before and after evaluation.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    object? GenerateFromSchemaAndCancellationToken(FakeSchema schema, CancellationToken cancellationToken);
    /// <summary>Generates a bounded number of schema results in evaluation order.</summary>
    /// <remarks>Each result receives a fresh correlation root while named sequence and uniqueness state continues across results and other operations.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to generate.</param>
    /// <returns>A new array containing the generated results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    object?[] Many(FakeSchema schema, int count);
    /// <summary>Generates a bounded number of schema results with cancellation.</summary>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to generate.</param>
    /// <param name="cancellationToken">The token checked before allocation and each delivery.</param>
    /// <returns>A new array containing the generated results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    object?[] ManyFromSchemaAndCountAndCancellationToken(FakeSchema schema, int count, CancellationToken cancellationToken);
    /// <summary>Generates a bounded number of schema results and delivers them to a callback in evaluation order.</summary>
    /// <remarks>If generation or delivery fails, completed deliveries remain observable and the exception is propagated.</remarks>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="count">The number of results to deliver.</param>
    /// <param name="callback">The callback invoked once for each generated result.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    void Deliver(FakeSchema schema, int count, Action<object?> callback);
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
    void DeliverFromSchemaAndCountAndCallbackAndCancellationToken(FakeSchema schema, int count, Action<object?> callback, CancellationToken cancellationToken);
    /// <summary>Fills an existing one-dimensional array with schema results.</summary>
    /// <remarks>If generation or assignment fails, the completed destination prefix remains filled and the exception is propagated.</remarks>
    /// <param name="values">The array to fill in ascending index order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The same array after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>values</c> is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    Array Fill(Array values, FakeSchema schema);
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
    Array FillFromArrayAndSchemaAndCancellationToken(Array values, FakeSchema schema, CancellationToken cancellationToken);
    /// <summary>Fills the existing positions of a non-generic list with schema results.</summary>
    /// <remarks>The list count is not changed. If generation or assignment fails, the completed prefix remains filled and the exception is propagated.</remarks>
    /// <param name="values">The list whose existing positions are filled in ascending order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <returns>The same list after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count or generated output exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    IList FillFromListAndSchema(IList values, FakeSchema schema);
    /// <summary>Fills the existing positions of a non-generic list with schema results and cancellation.</summary>
    /// <param name="values">The list whose existing positions are filled in ascending order.</param>
    /// <param name="schema">The schema to generate.</param>
    /// <param name="cancellationToken">The token checked before generation and each assignment.</param>
    /// <returns>The same list after generation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count or generated output exceeds a configured limit.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    IList FillFromListAndSchemaAndCancellationToken(IList values, FakeSchema schema, CancellationToken cancellationToken);
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
    object?[] Page(FakeSchema schema, long pageIndex, int pageSize);
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
    object?[] PageFromSchemaAndPageIndexAndPageSizeAndCancellationToken(FakeSchema schema, long pageIndex, int pageSize, CancellationToken cancellationToken);
    /// <summary>Evaluates a schema as an alias for <see cref="Generate(FakeSchema)"/>.</summary>
    /// <param name="schema">The schema to evaluate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>schema</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    object? Evaluate(FakeSchema schema);
    /// <summary>Evaluates a schema node as an alias for <see cref="GenerateFromRoot(SchemaNode)"/>.</summary>
    /// <param name="root">The root node to evaluate.</param>
    /// <returns>The generated schema value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when generated output exceeds the configured output-size limit.</exception>
    /// <exception cref="FakeException">Thrown when validation or evaluation fails.</exception>
    object? EvaluateFromRoot(SchemaNode root);
    /// <summary>Creates a scalar callback node.</summary>
    /// <param name="generate">The callback that receives the owning <see cref="Fake"/> during evaluation.</param>
    /// <returns>A scalar schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    SchemaNode Scalar(System.Func<Fake, object?> generate);
    /// <summary>Creates a scalar node from a parameterless callback.</summary>
    /// <param name="generate">The callback invoked during evaluation.</param>
    /// <returns>A scalar schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    SchemaNode ScalarFromGenerate(System.Func<object?> generate);
    /// <summary>Creates one named object field.</summary>
    /// <param name="name">The nonempty field name.</param>
    /// <param name="node">The node evaluated for the field.</param>
    /// <returns>A schema field.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    SchemaField Field(string name, SchemaNode node);
    /// <summary>Creates an object node with deterministic field order.</summary>
    /// <param name="fields">The ordered named fields to snapshot.</param>
    /// <returns>An object schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>fields</c> or one of its entries is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the field count exceeds configured limits.</exception>
    SchemaNode Object(SchemaField[] fields);
    /// <summary>Creates a fixed-count array node.</summary>
    /// <param name="item">The node evaluated for every array item.</param>
    /// <param name="count">The exact item count.</param>
    /// <returns>An array schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    SchemaNode Array(SchemaNode item, int count);
    /// <summary>Creates a bounded variable-count array node.</summary>
    /// <param name="item">The node evaluated for every array item.</param>
    /// <param name="minimumCount">The inclusive minimum item count.</param>
    /// <param name="maximumCount">The inclusive maximum item count.</param>
    /// <returns>An array schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is invalid or exceeds configured limits.</exception>
    SchemaNode ArrayFromItemAndMinimumCountAndMaximumCount(SchemaNode item, int minimumCount, int maximumCount);
    /// <summary>Creates a constant-value node.</summary>
    /// <param name="value">The value returned during evaluation, including null.</param>
    /// <returns>A constant schema node.</returns>
    SchemaNode Constant(object? value);
    /// <summary>Creates a reference to a previously evaluated field path.</summary>
    /// <param name="fieldPath">The dot-separated field path.</param>
    /// <returns>A reference schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>fieldPath</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>fieldPath</c> is malformed.</exception>
    SchemaNode Reference(string fieldPath);
    /// <summary>Creates a node derived from the current correlation context.</summary>
    /// <param name="derive">The callback invoked with the current generated object context.</param>
    /// <returns>A derived schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>derive</c> is null.</exception>
    SchemaNode Derived(System.Func<object, object?> derive);
    /// <summary>Creates a two-branch condition node.</summary>
    /// <param name="predicate">The callback that inspects the current correlation context.</param>
    /// <param name="whenTrue">The node evaluated when the predicate returns true.</param>
    /// <param name="whenFalse">The node evaluated when the predicate returns false.</param>
    /// <returns>A condition schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    SchemaNode Conditional(System.Func<object, bool> predicate, SchemaNode whenTrue, SchemaNode whenFalse);
    /// <summary>Creates a two-branch condition node.</summary>
    /// <remarks>This is an alias for <see cref="Conditional(System.Func{object, bool}, SchemaNode, SchemaNode)"/>.</remarks>
    /// <param name="predicate">The callback that inspects the current correlation context.</param>
    /// <param name="whenTrue">The node evaluated when the predicate returns true.</param>
    /// <param name="whenFalse">The node evaluated when the predicate returns false.</param>
    /// <returns>A condition schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    SchemaNode Condition(System.Func<object, bool> predicate, SchemaNode whenTrue, SchemaNode whenFalse);
    /// <summary>Creates one weighted schema choice.</summary>
    /// <param name="node">The node evaluated when selected.</param>
    /// <param name="weight">A finite nonnegative relative weight.</param>
    /// <returns>A weighted schema variant.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>weight</c> is negative or non-finite.</exception>
    SchemaVariant Choice(SchemaNode node, double weight);
    /// <summary>Creates a weighted variant node.</summary>
    /// <param name="variants">The weighted choices to snapshot in deterministic order.</param>
    /// <returns>A weighted variant schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>variants</c> or one of its entries is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when no variants are supplied or their total weight is not positive and finite.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the variant count exceeds configured limits.</exception>
    SchemaNode Variant(SchemaVariant[] variants);
    /// <summary>Creates a named sequence node starting at zero with an increment of one.</summary>
    /// <param name="name">The nonempty sequence-state name.</param>
    /// <returns>A sequence schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    SchemaNode Sequence(string name);
    /// <summary>Creates a named signed-integer sequence node.</summary>
    /// <param name="name">The nonempty sequence-state name.</param>
    /// <param name="start">The first value returned by the sequence.</param>
    /// <param name="increment">The nonzero value added after each result.</param>
    /// <returns>A sequence schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    SchemaNode SequenceFromNameAndStartAndIncrement(string name, long start, long increment);
    /// <summary>Creates a named uniqueness node using the configured retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until it produces an unused value.</param>
    /// <returns>A unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    SchemaNode Unique(string name, SchemaNode source);
    /// <summary>Creates a named uniqueness node with an explicit retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until it produces an unused value.</param>
    /// <param name="retryLimit">The positive attempts allowed for one result.</param>
    /// <returns>A unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>retryLimit</c> is not positive.</exception>
    SchemaNode UniqueFromNameAndSourceAndRetryLimit(string name, SchemaNode source, int retryLimit);
    /// <summary>Creates a named composite-uniqueness node using the configured retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until its selected key is unused.</param>
    /// <param name="compositeKey">The callback selecting ordered string key parts from each candidate.</param>
    /// <returns>A composite unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    SchemaNode UniqueFromNameAndSourceAndCompositeKey(string name, SchemaNode source, System.Func<object?, string[]> compositeKey);
    /// <summary>Creates a named composite-uniqueness node with an explicit retry limit.</summary>
    /// <param name="name">The nonempty uniqueness-scope name.</param>
    /// <param name="source">The child node retried until its selected key is unused.</param>
    /// <param name="compositeKey">The callback selecting ordered string key parts from each candidate.</param>
    /// <param name="retryLimit">The positive attempts allowed for one result.</param>
    /// <returns>A composite unique schema node.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>retryLimit</c> is not positive.</exception>
    SchemaNode UniqueFromNameAndSourceAndCompositeKeyAndRetryLimit(string name, SchemaNode source, System.Func<object?, string[]> compositeKey, int retryLimit);
    /// <summary>Resets a named sequence shared by schema, profile, and random generation.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    void ResetSequence(string name);
    /// <summary>Resets every named sequence in this generation context.</summary>
    void ResetSequenceFromNoArguments();
    /// <summary>Resets a named uniqueness scope shared by schema, profile, and random generation.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    void ResetUnique(string name);
    /// <summary>Resets every named uniqueness scope in this generation context.</summary>
    void ResetUniqueFromNoArguments();
    /// <summary>Wraps a schema root for generation operations.</summary>
    /// <param name="root">The non-null root node.</param>
    /// <returns>A fake schema containing the root.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>root</c> is null.</exception>
    FakeSchema Schema(SchemaNode root);
}
