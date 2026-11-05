using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Serializes HTML nodes deterministically while enforcing configured limits.</summary>
internal static class HtmlSerializer
{
    /// <summary>Serializes the supplied node with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <returns>The serialized outer HTML for <c>node</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown when output would exceed the configured character limit.</exception>
    public static string Serialize(HtmlNode node)
    {
        return Serialize(node, HtmlSerializerOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Serializes the supplied node with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The serialized outer HTML for <c>node</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown when output would exceed the configured character limit.</exception>
    public static string Serialize(HtmlNode node,
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        ValidateNodeAndOptions(node, options);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        Serialize(node, writer, options, cancellationToken);
        return builder.ToString();
    }

    internal static string? Serialize(HtmlNode node,
        HtmlSerializerOptions options, CancellationToken cancellationToken,
        HtmlBudget budget)
    {
        ValidateNodeAndOptions(node, options);
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken, budget);
        serializer.SetSuppressLimitException();
        serializer.WriteOuter(node);
        return serializer.WasLimitExceeded() ? null : builder.ToString();
    }

    /// <summary>Serializes the supplied node with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="writer">The destination that receives serialized text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown after a safe prefix has been written when further output would exceed the configured character limit.</exception>
    public static void Serialize(HtmlNode node, TextWriter writer)
    {
        Serialize(node, writer, HtmlSerializerOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Serializes the supplied node with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="writer">The destination that receives serialized text.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown after a safe prefix has been written when further output would exceed the configured character limit.</exception>
    public static void Serialize(HtmlNode node, TextWriter writer,
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        ValidateArguments(node, writer, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.WriteOuter(node);
    }

    /// <summary>Serializes only the supplied node's children with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <returns>The concatenated serialized HTML of the node's children.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown when output would exceed the configured character limit.</exception>
    public static string SerializeChildren(HtmlNode node)
    {
        return SerializeChildren(node, HtmlSerializerOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Serializes only the supplied node's children with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The concatenated serialized HTML of the node's children.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown when output would exceed the configured character limit.</exception>
    public static string SerializeChildren(HtmlNode node,
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        ValidateNodeAndOptions(node, options);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        SerializeChildren(node, writer, options, cancellationToken);
        return builder.ToString();
    }

    /// <summary>Serializes only the supplied node's children with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="writer">The destination that receives serialized text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown after a safe prefix has been written when further output would exceed the configured character limit.</exception>
    public static void SerializeChildren(HtmlNode node, TextWriter writer)
    {
        SerializeChildren(node, writer, HtmlSerializerOptions.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Serializes only the supplied node's children with the requested output settings.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="writer">The destination that receives serialized text.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    /// <exception cref="HtmlSerializationLimitException">Thrown after a safe prefix has been written when further output would exceed the configured character limit.</exception>
    public static void SerializeChildren(HtmlNode node, TextWriter writer,
        HtmlSerializerOptions options, CancellationToken cancellationToken)
    {
        ValidateArguments(node, writer, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.WriteInner(node);
    }

    internal static HtmlSerializationResult SerializeOuterResult(
        HtmlNode node, HtmlSerializerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateNodeAndOptions(node, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.SetSuppressLimitException();
        serializer.WriteOuter(node);
        return HtmlSerializationResult.CreateString(builder.ToString(),
            serializer.WasLimitExceeded());
    }

    internal static HtmlSerializationResult SerializeInnerResult(
        HtmlNode node, HtmlSerializerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateNodeAndOptions(node, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        StringBuilder builder = new StringBuilder();
        StringWriter writer = new StringWriter(builder);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.SetSuppressLimitException();
        serializer.WriteInner(node);
        return HtmlSerializationResult.CreateString(builder.ToString(),
            serializer.WasLimitExceeded());
    }

    internal static HtmlSerializationResult WriteOuterResult(HtmlNode node,
        TextWriter writer, HtmlSerializerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateArguments(node, writer, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.SetSuppressLimitException();
        serializer.WriteOuter(node);
        return HtmlSerializationResult.CreateWriter(
            serializer.GetCharactersWritten(),
            serializer.WasLimitExceeded());
    }

    internal static HtmlSerializationResult WriteInnerResult(HtmlNode node,
        TextWriter writer, HtmlSerializerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateArguments(node, writer, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSerializationValidator.Validate(node, cancellationToken);
        HtmlSerializationWriter serializer = HtmlSerializationWriter.Create(
            writer, options.Copy(), cancellationToken);
        serializer.SetSuppressLimitException();
        serializer.WriteInner(node);
        return HtmlSerializationResult.CreateWriter(
            serializer.GetCharactersWritten(),
            serializer.WasLimitExceeded());
    }

    internal static void ValidateNodeAndOptions(HtmlNode? node,
        HtmlSerializerOptions options)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
    }

    internal static void ValidateArguments(HtmlNode node, TextWriter writer,
        HtmlSerializerOptions options)
    {
        ValidateNodeAndOptions(node, options);
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }
    }
}
