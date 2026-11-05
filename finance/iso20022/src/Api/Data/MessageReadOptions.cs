using System.Collections.Generic;

namespace iso20022;

/// <summary>Controls generic XML message reading and materialization.</summary>
public class MessageReadOptions
{
    /// <summary>The namespace strictness policy.</summary>
    public NamespaceMode NamespaceMode = NamespaceMode.Strict;
    /// <summary>The policy for content without a known model.</summary>
    public UnknownContentMode UnknownContentMode = UnknownContentMode.Preserve;
    /// <summary>An exact or compatible version selection policy when supplied.</summary>
    public VersionSelector? VersionSelector;
    /// <summary>The resource bounds applied while reading.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether comments are retained in generic trees.</summary>
    public bool PreserveComments;
    /// <summary>Whether processing instructions are retained in generic trees.</summary>
    public bool PreserveProcessingInstructions;
    /// <summary>Whether whitespace-only text nodes are retained.</summary>
    public bool PreserveWhitespace = true;
    /// <summary>Whether source lexical forms are retained when possible.</summary>
    public bool PreserveLexicalValues = true;
    /// <summary>The paths to materialize when partial extraction is requested.</summary>
    public List<XmlPathData> SelectedPaths = new List<XmlPathData>();
    /// <summary>The qualified names accepted as caller-configured wrappers.</summary>
    public List<QualifiedNameData> CustomWrapperNames = new List<QualifiedNameData>();
}
