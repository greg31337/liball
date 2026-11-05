using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlSerializer operations.</summary>
public interface IHtmlSerializerApi
{
    /// <summary>Creates canonical serializer settings.</summary>
    public HtmlSerializerOptions CreateCanonicalOptions();
    /// <summary>Creates source-preserving serializer settings.</summary>
    public HtmlSerializerOptions CreatePreserveOptions();
    /// <summary>Creates ASCII-only serializer settings.</summary>
    public HtmlSerializerOptions CreateAsciiOnlyOptions();
    /// <summary>Creates compact serializer settings.</summary>
    public HtmlSerializerOptions CreateCompactOptions();
    /// <summary>Creates pretty-printed serializer settings.</summary>
    public HtmlSerializerOptions CreatePrettyOptions();
    /// <summary>Executes the SerializeNode operation.</summary>
    public string SerializeNode(HtmlNode node);
    /// <summary>Executes the SerializeNodeWithOptions operation.</summary>
    public string SerializeNodeWithOptions(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the WriteNode operation.</summary>
    public void WriteNode(HtmlNode node, TextWriter writer);
    /// <summary>Executes the WriteNodeWithOptions operation.</summary>
    public void WriteNodeWithOptions(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the SerializeChildren operation.</summary>
    public string SerializeChildren(HtmlNode node);
    /// <summary>Executes the SerializeChildrenWithOptions operation.</summary>
    public string SerializeChildrenWithOptions(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the WriteChildren operation.</summary>
    public void WriteChildren(HtmlNode node, TextWriter writer);
    /// <summary>Executes the WriteChildrenWithOptions operation.</summary>
    public void WriteChildrenWithOptions(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Serializes a whole document to a bounded string result.</summary>
    public HtmlSerializationResult SerializeDocument(HtmlDocument document, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Serializes a document fragment to a bounded string result.</summary>
    public HtmlSerializationResult SerializeFragment(HtmlNode fragment, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Serializes a node subtree to a bounded string result.</summary>
    public HtmlSerializationResult SerializeSubtree(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Serializes the children of a node to a bounded string result.</summary>
    public HtmlSerializationResult SerializeInnerHtml(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Serializes the outer HTML of a non-container node to a bounded string result.</summary>
    public HtmlSerializationResult SerializeOuterHtml(HtmlNode node, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Writes a whole document and returns its explicit completion status.</summary>
    public HtmlSerializationResult WriteDocument(HtmlDocument document, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Writes a document fragment and returns its explicit completion status.</summary>
    public HtmlSerializationResult WriteFragment(HtmlNode fragment, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Writes a node subtree and returns its explicit completion status.</summary>
    public HtmlSerializationResult WriteSubtree(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Writes the children of a node and returns its explicit completion status.</summary>
    public HtmlSerializationResult WriteInnerHtml(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
    /// <summary>Writes the outer HTML of a non-container node and returns its explicit completion status.</summary>
    public HtmlSerializationResult WriteOuterHtml(HtmlNode node, TextWriter writer, IHtmlSerializerConfiguration options, CancellationToken cancellationToken);
}
