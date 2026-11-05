namespace iso20022;

/// <summary>Signs raw ISO 20022 XML with explicit caller key material and algorithms.</summary>
public interface IXmlMessageSigner
{
    /// <summary>Signs selected contained message paths and inserts one XML signature deterministically.</summary>
    /// <param name="request">The source XML, key material, algorithms, references, and insertion path.</param>
    /// <returns>The stable signing result and signed XML.</returns>
    MessageSigningResult SignMessage(MessageSigningRequest? request);
}
