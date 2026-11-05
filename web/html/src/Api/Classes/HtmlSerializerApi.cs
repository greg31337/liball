using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlSerializer operations.</summary>
public class HtmlSerializerApi : IHtmlSerializerApi
{
    /// <summary>Creates deterministic canonical settings with lowercase HTML names and double-quoted attributes.</summary>
    /// <returns>A new independently mutable canonical-options object.</returns>
    public HtmlSerializerOptions CreateCanonicalOptions()
    {
        return HtmlSerializerOptions.CreateCanonical();
    }

    /// <summary>Creates settings that retain stored name casing and DOM attribute order.</summary>
    /// <returns>A new independently mutable preserve-options object.</returns>
    public HtmlSerializerOptions CreatePreserveOptions()
    {
        return HtmlSerializerOptions.CreatePreserve();
    }

    /// <summary>Creates canonical settings that encode non-ASCII scalars as numeric character references.</summary>
    /// <returns>A new independently mutable ASCII-only options object.</returns>
    public HtmlSerializerOptions CreateAsciiOnlyOptions()
    {
        HtmlSerializerOptions options = HtmlSerializerOptions.CreateCanonical();
        options.SetAsciiOnly(true);
        return options;
    }

    /// <summary>Creates compact settings that safely omit optional HTML tags.</summary>
    /// <returns>A new independently mutable compact-options object.</returns>
    public HtmlSerializerOptions CreateCompactOptions()
    {
        return HtmlSerializerOptions.CreateCompact();
    }

    /// <summary>Creates settings that add safe indentation and line breaks.</summary>
    /// <returns>A new independently mutable pretty-options object.</returns>
    public HtmlSerializerOptions CreatePrettyOptions()
    {
        return HtmlSerializerOptions.CreatePretty();
    }

    /// <summary>Executes the SerializeNode operation.</summary>
    public string SerializeNode(HtmlNode node)
    {
        return HtmlSerializer.Serialize(node);
    }

    /// <summary>Executes the SerializeNodeWithOptions operation.</summary>
    public string SerializeNodeWithOptions(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlSerializer.Serialize(node, CreateOptions(options),
            cancellationToken);
    }

    /// <summary>Executes the WriteNode operation.</summary>
    public void WriteNode(HtmlNode node, TextWriter writer)
    {
        HtmlSerializer.Serialize(node, writer);
    }

    /// <summary>Executes the WriteNodeWithOptions operation.</summary>
    public void WriteNodeWithOptions(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken)
    {
        HtmlSerializer.Serialize(node, writer, CreateOptions(options),
            cancellationToken);
    }

    /// <summary>Executes the SerializeChildren operation.</summary>
    public string SerializeChildren(HtmlNode node)
    {
        return HtmlSerializer.SerializeChildren(node);
    }

    /// <summary>Executes the SerializeChildrenWithOptions operation.</summary>
    public string SerializeChildrenWithOptions(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeChildren(node, CreateOptions(options),
            cancellationToken);
    }

    /// <summary>Executes the WriteChildren operation.</summary>
    public void WriteChildren(HtmlNode node, TextWriter writer)
    {
        HtmlSerializer.SerializeChildren(node, writer);
    }

    /// <summary>Executes the WriteChildrenWithOptions operation.</summary>
    public void WriteChildrenWithOptions(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken)
    {
        HtmlSerializer.SerializeChildren(node, writer,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Serializes every top-level node in a complete document.</summary>
    /// <param name = "document">The document whose root children are serialized.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>A bounded string result with explicit completion status.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult SerializeDocument(HtmlDocument document,
        IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeOuterResult(GetDocumentRoot(document),
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Serializes every top-level node in a document fragment.</summary>
    /// <param name = "fragment">The document-fragment node to serialize.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>A bounded string result with explicit completion status.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when <c>fragment</c> is not a document-fragment node.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult SerializeFragment(HtmlNode fragment,
        IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeOuterResult(ValidateFragment(fragment),
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Serializes a node and its descendants, or the top-level contents of a document or fragment node.</summary>
    /// <param name = "node">The subtree root.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>A bounded string result with explicit completion status.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult SerializeSubtree(HtmlNode node,
        IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeOuterResult(node,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Serializes only the children of a node.</summary>
    /// <param name = "node">The node whose children are serialized.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>A bounded string result with explicit completion status.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult SerializeInnerHtml(HtmlNode node,
        IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeInnerResult(node,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Serializes one non-container node together with its descendants.</summary>
    /// <param name = "node">The document type, element, text, or comment node to serialize.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>A bounded string result with explicit completion status.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when <c>node</c> is a document or fragment container.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult SerializeOuterHtml(HtmlNode node,
        IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.SerializeOuterResult(ValidateOuterNode(node),
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Writes every top-level node in a complete document.</summary>
    /// <param name = "document">The document whose root children are written.</param>
    /// <param name = "writer">The destination that receives serialized text.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>The completion status and number of characters written.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult WriteDocument(HtmlDocument document,
        TextWriter writer, IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.WriteOuterResult(GetDocumentRoot(document),
            writer, CreateOptions(options), cancellationToken);
    }

    /// <summary>Writes every top-level node in a document fragment.</summary>
    /// <param name = "fragment">The document-fragment node to write.</param>
    /// <param name = "writer">The destination that receives serialized text.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>The completion status and number of characters written.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when <c>fragment</c> is not a document-fragment node.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult WriteFragment(HtmlNode fragment,
        TextWriter writer, IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.WriteOuterResult(ValidateFragment(fragment),
            writer, CreateOptions(options), cancellationToken);
    }

    /// <summary>Writes a node and its descendants, or the top-level contents of a document or fragment node.</summary>
    /// <param name = "node">The subtree root.</param>
    /// <param name = "writer">The destination that receives serialized text.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>The completion status and number of characters written.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult WriteSubtree(HtmlNode node,
        TextWriter writer, IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.WriteOuterResult(node, writer,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Writes only the children of a node.</summary>
    /// <param name = "node">The node whose children are written.</param>
    /// <param name = "writer">The destination that receives serialized text.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>The completion status and number of characters written.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult WriteInnerHtml(HtmlNode node,
        TextWriter writer, IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.WriteInnerResult(node, writer,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Writes one non-container node together with its descendants.</summary>
    /// <param name = "node">The document type, element, text, or comment node to write.</param>
    /// <param name = "writer">The destination that receives serialized text.</param>
    /// <param name = "options">The settings copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked throughout serialization.</param>
    /// <returns>The completion status and number of characters written.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when <c>node</c> is a document or fragment container.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before serialization completes.</exception>
    public HtmlSerializationResult WriteOuterHtml(HtmlNode node,
        TextWriter writer, IHtmlSerializerConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlSerializer.WriteOuterResult(ValidateOuterNode(node),
            writer, CreateOptions(options), cancellationToken);
    }

    internal static HtmlSerializerOptions CreateOptions(
        IHtmlSerializerConfiguration options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.Compile().CreateOptions();
    }

    internal static HtmlNode GetDocumentRoot(HtmlDocument? document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return document.GetRoot();
    }

    internal static HtmlNode ValidateFragment(HtmlNode? fragment)
    {
        if (fragment == null)
        {
            throw new ArgumentNullException("fragment");
        }

        if (fragment.GetKind() != HtmlNodeKind.Fragment)
        {
            throw new ArgumentException(
                "Fragment serialization requires a document-fragment node.",
                "fragment");
        }

        return fragment;
    }

    internal static HtmlNode ValidateOuterNode(HtmlNode? node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        HtmlNodeKind kind = node.GetKind();
        if (kind == HtmlNodeKind.Document || kind == HtmlNodeKind.Fragment)
        {
            throw new ArgumentException(
                "Outer HTML requires a node with its own serialized representation.",
                "node");
        }

        return node;
    }
}
