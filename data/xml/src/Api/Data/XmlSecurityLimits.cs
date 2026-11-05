namespace xml;

/// <summary>
/// Carries limits for attacker-controlled XML processing work.
/// </summary>
public class XmlSecurityLimits
{
    /// <summary>
    /// The maximum element depth.
    /// </summary>
    public int MaximumDepth;
    /// <summary>
    /// The maximum node count.
    /// </summary>
    public long MaximumNodeCount;
    /// <summary>
    /// The maximum attributes on one element.
    /// </summary>
    public int MaximumAttributesPerElement;
    /// <summary>
    /// The maximum XML name length in characters.
    /// </summary>
    public int MaximumNameLength;
    /// <summary>
    /// The maximum text node length in characters.
    /// </summary>
    public long MaximumTextLength;
    /// <summary>
    /// The maximum lexical token length in characters.
    /// </summary>
    public int MaximumTokenLength;
    /// <summary>
    /// The maximum total input characters.
    /// </summary>
    public long MaximumInputCharacters;
    /// <summary>
    /// The maximum total input bytes.
    /// </summary>
    public long MaximumInputBytes;
    /// <summary>
    /// The maximum DTD characters.
    /// </summary>
    public long MaximumDtdCharacters;
    /// <summary>
    /// The maximum entity nesting depth.
    /// </summary>
    public int MaximumEntityDepth;
    /// <summary>
    /// The maximum entity activation count.
    /// </summary>
    public long MaximumEntityCount;
    /// <summary>
    /// The maximum expanded entity characters.
    /// </summary>
    public long MaximumEntityCharacters;
    /// <summary>
    /// The maximum entity amplification ratio.
    /// </summary>
    public long MaximumEntityAmplification;
    /// <summary>
    /// The maximum external resource count.
    /// </summary>
    public int MaximumExternalResources;
    /// <summary>
    /// The maximum nested resource depth.
    /// </summary>
    public int MaximumResourceDepth;
    /// <summary>
    /// The maximum bytes read from one external resource.
    /// </summary>
    public long MaximumResourceBytes;
    /// <summary>
    /// The maximum work units between cancellation polls.
    /// </summary>
    public int CancellationPollingInterval;
}

