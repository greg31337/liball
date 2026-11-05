using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimKey"/>.
/// </summary>
public interface IEmailDkimKeyContract
{
    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <returns>The current tags.</returns>
    EmailSignatureTagList GetTags();
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
    /// Gets the source kind.
    /// </summary>
    /// <returns>The current source kind.</returns>
    EmailDkimKeySourceKind GetSourceKind();
    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    EmailDkimKeyAlgorithmKind GetAlgorithmKind();
    /// <summary>
    /// Gets the key type.
    /// </summary>
    /// <returns>The current key type.</returns>
    string GetKeyType();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    string GetVersion();
    /// <summary>
    /// Gets the notes.
    /// </summary>
    /// <returns>The current notes.</returns>
    string GetNotes();
    /// <summary>
    /// Gets the hash algorithms.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hash algorithms.</returns>
    List<string> GetHashAlgorithms();
    /// <summary>
    /// Gets the service types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current service types.</returns>
    List<string> GetServiceTypes();
    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current flags.</returns>
    List<string> GetFlags();
    /// <summary>
    /// Gets the unknown tags.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unknown tags.</returns>
    List<EmailSignatureTag> GetUnknownTags();
    /// <summary>
    /// Determines whether the current value or state is revoked.
    /// </summary>
    /// <returns><c>true</c> when the value or state is revoked; otherwise, <c>false</c>.</returns>
    bool IsRevoked();
    /// <summary>
    /// Determines whether the current value or state is structurally valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is structurally valid; otherwise, <c>false</c>.</returns>
    bool IsStructurallyValid();
    /// <summary>
    /// Determines whether the current value or state is size policy compliant.
    /// </summary>
    /// <returns><c>true</c> when the value or state is size policy compliant; otherwise, <c>false</c>.</returns>
    bool IsSizePolicyCompliant();
    /// <summary>
    /// Determines whether the current value or state is supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supported; otherwise, <c>false</c>.</returns>
    bool IsSupported();
    /// <summary>
    /// Determines whether the current value or state is usable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is usable; otherwise, <c>false</c>.</returns>
    bool IsUsable();
    /// <summary>
    /// Determines whether the public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the public key is available; otherwise, <c>false</c>.</returns>
    bool HasPublicKey();
    /// <summary>
    /// Gets the public key data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current public key data.</returns>
    byte[] GetPublicKeyData();
    /// <summary>
    /// Determines whether the subject public key info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject public key info is available; otherwise, <c>false</c>.</returns>
    bool HasSubjectPublicKeyInfo();
    /// <summary>
    /// Gets the subject public key info.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current subject public key info.</returns>
    byte[]? GetSubjectPublicKeyInfo();
    /// <summary>
    /// Determines whether the RSA public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the RSA public key is available; otherwise, <c>false</c>.</returns>
    bool HasRsaPublicKey();
    /// <summary>
    /// Gets the RSA public key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current RSA public key.</returns>
    byte[]? GetRsaPublicKey();
    /// <summary>
    /// Determines whether the ed 25519 public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ed 25519 public key is available; otherwise, <c>false</c>.</returns>
    bool HasEd25519PublicKey();
    /// <summary>
    /// Gets the ed 25519 public key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current ed 25519 public key.</returns>
    byte[]? GetEd25519PublicKey();
    /// <summary>
    /// Gets the key bit count.
    /// </summary>
    /// <returns>The current key bit count.</returns>
    int GetKeyBitCount();
    /// <summary>
    /// Determines whether the current value or state is testing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is testing; otherwise, <c>false</c>.</returns>
    bool IsTesting();
    /// <summary>
    /// Determines whether the current policy requires strict identity.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool RequiresStrictIdentity();
    /// <summary>
    /// Determines whether the current policy allows hash algorithm.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the policy allows hash algorithm; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool AllowsHashAlgorithm(string value);
    /// <summary>
    /// Determines whether the current policy allows service.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the policy allows service; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool AllowsService(string value);
    /// <summary>
    /// Formats this key as a deterministic canonical DKIM DNS record.
    /// </summary>
    /// <returns>The format result.</returns>
    EmailValueFormatResult Format();
    /// <summary>
    /// Formats this key as a deterministic canonical DKIM DNS record with options.
    /// </summary>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatWithOptions(EmailValueFormatOptions options);
    /// <summary>
    /// Preserves this key's original safe DNS record when available.
    /// </summary>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatPreserving();
    /// <summary>
    /// Preserves this key's original safe DNS record when available, subject to formatting limits.
    /// </summary>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    EmailValueFormatResult FormatPreservingWithOptions(EmailValueFormatOptions options);
}
