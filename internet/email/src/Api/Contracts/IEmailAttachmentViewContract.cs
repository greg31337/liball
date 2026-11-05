using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAttachmentView"/>.
/// </summary>
public interface IEmailAttachmentViewContract
{
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity GetEntity();
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    EmailPath GetPath();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailAttachmentKind GetKind();
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    EmailMediaType? GetMediaType();
    /// <summary>
    /// Determines whether the current value or state is body.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body; otherwise, <c>false</c>.</returns>
    bool IsBody();
    /// <summary>
    /// Determines whether the current value or state is inline resource.
    /// </summary>
    /// <returns><c>true</c> when the value or state is inline resource; otherwise, <c>false</c>.</returns>
    bool IsInlineResource();
    /// <summary>
    /// Determines whether the current value or state is downloadable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is downloadable; otherwise, <c>false</c>.</returns>
    bool IsDownloadable();
    /// <summary>
    /// Determines whether the current value or state is attachment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment; otherwise, <c>false</c>.</returns>
    bool IsAttachment();
    /// <summary>
    /// Determines whether the current value or state is nested message.
    /// </summary>
    /// <returns><c>true</c> when the value or state is nested message; otherwise, <c>false</c>.</returns>
    bool IsNestedMessage();
    /// <summary>
    /// Determines whether the current value or state is cryptographic part.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cryptographic part; otherwise, <c>false</c>.</returns>
    bool IsCryptographicPart();
    /// <summary>
    /// Determines whether the current value or state is signature.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature; otherwise, <c>false</c>.</returns>
    bool IsSignature();
    /// <summary>
    /// Determines whether the current value or state is encryption control.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encryption control; otherwise, <c>false</c>.</returns>
    bool IsEncryptionControl();
    /// <summary>
    /// Determines whether the current value or state is encrypted payload.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted payload; otherwise, <c>false</c>.</returns>
    bool IsEncryptedPayload();
    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    bool HasFilename();
    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    string? GetFilename();
    /// <summary>
    /// Gets the safe filename.
    /// </summary>
    /// <returns>The current safe filename.</returns>
    string GetSafeFilename();
    /// <summary>
    /// Gets the safe filename.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The current safe filename.</returns>
    string GetSafeFilenameWithOptions(EmailFilenameOptions options);
    /// <summary>
    /// Gets the safe filename result.
    /// </summary>
    /// <returns>The current safe filename result.</returns>
    EmailFilenameResult GetSafeFilenameResult();
    /// <summary>
    /// Gets the safe filename result.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The current safe filename result.</returns>
    EmailFilenameResult GetSafeFilenameResultWithOptions(EmailFilenameOptions options);
    /// <summary>
    /// Suggests the safe filename.
    /// </summary>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string SuggestSafeFilename(List<string> existingFilenames);
    /// <summary>
    /// Suggests the safe filename.
    /// </summary>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string SuggestSafeFilenameWithOptions(List<string> existingFilenames, EmailFilenameOptions options);
    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    bool HasDisposition();
    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    EmailDisposition? GetDisposition();
    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    bool HasContentId();
    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    EmailMessageId? GetContentId();
    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    bool HasContentLocation();
    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    string? GetContentLocation();
    /// <summary>
    /// Copies the decoded content to to a separate destination.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    long CopyDecodedContentTo(Stream output);
    /// <summary>
    /// Copies the decoded content to to a separate destination.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long CopyDecodedContentToWithLimit(Stream output, long maximumBytes);
    /// <summary>
    /// Saves the decoded content.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    long SaveDecodedContent(string path);
    /// <summary>
    /// Saves the decoded content.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long SaveDecodedContentWithLimit(string path, long maximumBytes);
    /// <summary>
    /// Reads the decoded prefix from the input.
    /// </summary>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ReadDecodedPrefix(int maximumPrefixBytes);
    /// <summary>
    /// Measures the decoded length.
    /// </summary>
    /// <returns>The computed count or size.</returns>
    long MeasureDecodedLength();
    /// <summary>
    /// Measures the decoded length.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureDecodedLengthWithLimit(long maximumBytes);
    /// <summary>
    /// Computes the decoded hash.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeDecodedHash(EmailHashAlgorithm algorithm);
    /// <summary>
    /// Computes the decoded hash.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeDecodedHashWithLimit(EmailHashAlgorithm algorithm, long maximumBytes);
    /// <summary>
    /// Inspects the content type.
    /// </summary>
    /// <returns>The content type analysis produced by the operation.</returns>
    EmailContentTypeAnalysis InspectContentType();
    /// <summary>
    /// Decodes the text.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeText();
    /// <summary>
    /// Decodes the text.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeTextWithOptions(EmailTextDecodeOptions options);
    /// <summary>
    /// Processes the text.
    /// </summary>
    /// <returns>The text decode result produced by the operation.</returns>
    EmailTextDecodeResult ProcessText();
    /// <summary>
    /// Processes the text.
    /// </summary>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>The text decode result produced by the operation.</returns>
    EmailTextDecodeResult ProcessTextWithOptions(EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions);
}
