using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an ARC seal header selection.
/// </summary>
public class EmailArcSealHeaderSelection : IEmailArcSealHeaderSelectionContract
{
    int instance;
    EmailSignatureHeaderKind kind;
    int sourceIndex;
    EmailHeader header;
    bool signatureValueBlanked;
    /// <summary>
    /// Creates a new ARC seal header selection.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "signatureValueBlanked">Whether to enable the signature value blanked behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSealHeaderSelection(int instance, EmailSignatureHeaderKind kind, int sourceIndex, EmailHeader header, bool signatureValueBlanked)
    {
        if (instance < 1 || instance > 50)
            throw new ArgumentOutOfRangeException("instance");
        if (kind < EmailSignatureHeaderKind.ArcMessageSignature || kind > EmailSignatureHeaderKind.ArcAuthenticationResults)
            throw new ArgumentOutOfRangeException("kind");
        if (sourceIndex < 0)
            throw new ArgumentOutOfRangeException("sourceIndex");
        if (header == null)
            throw new ArgumentNullException("header");
        this.instance = instance;
        this.kind = kind;
        this.sourceIndex = sourceIndex;
        this.header = header.Clone();
        this.signatureValueBlanked = signatureValueBlanked;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return instance;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailSignatureHeaderKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    public int GetSourceIndex()
    {
        return sourceIndex;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailHeader GetHeader()
    {
        return header.Clone();
    }

    /// <summary>
    /// Determines whether the operation or source value was signature value blanked.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasSignatureValueBlanked()
    {
        return signatureValueBlanked;
    }
}
