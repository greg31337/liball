using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSeal"/>.
/// </summary>
public interface IEmailArcSealContract
{
    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <returns>The current tags.</returns>
    EmailSignatureTagList GetTags();
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    string GetAlgorithm();
    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    EmailDkimAlgorithmKind GetAlgorithmKind();
    /// <summary>
    /// Gets the signature data.
    /// </summary>
    /// <returns>The current signature data.</returns>
    string GetSignatureData();
    /// <summary>
    /// Gets the signing domain.
    /// </summary>
    /// <returns>The current signing domain.</returns>
    string GetSigningDomain();
    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    string GetSelector();
    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    long GetTimestamp();
    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    EmailArcChainStatus GetChainStatus();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options);
}
