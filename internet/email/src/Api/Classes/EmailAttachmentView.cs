using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Represents an attachment view.
/// </summary>
public class EmailAttachmentView : IEmailAttachmentViewContract
{
    MimeEntity entity;
    EmailPath path;
    EmailAttachmentKind kind;
    string? filename;
    /// <summary>
    /// Creates a new attachment view.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAttachmentView(MimeEntity entity, EmailPath path, EmailAttachmentKind kind, string? filename)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (kind < EmailAttachmentKind.Body || kind > EmailAttachmentKind.EncryptedPayload)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (filename != null && filename.Length == 0)
        {
            throw new ArgumentException("A filename cannot be empty.", "filename");
        }

        this.entity = entity;
        this.path = path;
        this.kind = kind;
        this.filename = filename;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity GetEntity()
    {
        return entity;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public EmailPath GetPath()
    {
        return path;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailAttachmentKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public EmailMediaType? GetMediaType()
    {
        return entity.GetMediaType();
    }

    /// <summary>
    /// Determines whether the current value or state is body.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body; otherwise, <c>false</c>.</returns>
    public bool IsBody()
    {
        return kind == EmailAttachmentKind.Body;
    }

    /// <summary>
    /// Determines whether the current value or state is inline resource.
    /// </summary>
    /// <returns><c>true</c> when the value or state is inline resource; otherwise, <c>false</c>.</returns>
    public bool IsInlineResource()
    {
        return kind == EmailAttachmentKind.InlineResource;
    }

    /// <summary>
    /// Determines whether the current value or state is downloadable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is downloadable; otherwise, <c>false</c>.</returns>
    public bool IsDownloadable()
    {
        return kind == EmailAttachmentKind.Attachment || kind == EmailAttachmentKind.NestedMessage;
    }

    /// <summary>
    /// Determines whether the current value or state is attachment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment; otherwise, <c>false</c>.</returns>
    public bool IsAttachment()
    {
        return kind == EmailAttachmentKind.Attachment;
    }

    /// <summary>
    /// Determines whether the current value or state is nested message.
    /// </summary>
    /// <returns><c>true</c> when the value or state is nested message; otherwise, <c>false</c>.</returns>
    public bool IsNestedMessage()
    {
        return kind == EmailAttachmentKind.NestedMessage;
    }

    /// <summary>
    /// Determines whether the current value or state is cryptographic part.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cryptographic part; otherwise, <c>false</c>.</returns>
    public bool IsCryptographicPart()
    {
        return kind == EmailAttachmentKind.Signature || kind == EmailAttachmentKind.EncryptionControl || kind == EmailAttachmentKind.EncryptedPayload;
    }

    /// <summary>
    /// Determines whether the current value or state is signature.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature; otherwise, <c>false</c>.</returns>
    public bool IsSignature()
    {
        return kind == EmailAttachmentKind.Signature;
    }

    /// <summary>
    /// Determines whether the current value or state is encryption control.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encryption control; otherwise, <c>false</c>.</returns>
    public bool IsEncryptionControl()
    {
        return kind == EmailAttachmentKind.EncryptionControl;
    }

    /// <summary>
    /// Determines whether the current value or state is encrypted payload.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted payload; otherwise, <c>false</c>.</returns>
    public bool IsEncryptedPayload()
    {
        return kind == EmailAttachmentKind.EncryptedPayload;
    }

    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    public bool HasFilename()
    {
        return filename != null;
    }

    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    public string? GetFilename()
    {
        return filename;
    }

    /// <summary>
    /// Gets the safe filename.
    /// </summary>
    /// <returns>The current safe filename.</returns>
    public string GetSafeFilename()
    {
        return new EmailFilename().SanitizeWithFilename(filename).GetFilename();
    }

    /// <summary>
    /// Gets the safe filename.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The current safe filename.</returns>
    public string GetSafeFilenameWithOptions(EmailFilenameOptions options)
    {
        return new EmailFilename().SanitizeWithFilenameAndOptions(filename, options).GetFilename();
    }

    /// <summary>
    /// Gets the safe filename result.
    /// </summary>
    /// <returns>The current safe filename result.</returns>
    public EmailFilenameResult GetSafeFilenameResult()
    {
        return new EmailFilename().SanitizeWithFilename(filename);
    }

    /// <summary>
    /// Gets the safe filename result.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The current safe filename result.</returns>
    public EmailFilenameResult GetSafeFilenameResultWithOptions(EmailFilenameOptions options)
    {
        return new EmailFilename().SanitizeWithFilenameAndOptions(filename, options);
    }

    /// <summary>
    /// Suggests the safe filename.
    /// </summary>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string SuggestSafeFilename(List<string> existingFilenames)
    {
        return new EmailFilename().SuggestWithFilenameAndExistingFilenames(filename, existingFilenames).GetFilename();
    }

    /// <summary>
    /// Suggests the safe filename.
    /// </summary>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string SuggestSafeFilenameWithOptions(List<string> existingFilenames, EmailFilenameOptions options)
    {
        return new EmailFilename().SuggestWithFilenameAndExistingFilenamesAndOptions(filename, existingFilenames, options).GetFilename();
    }

    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    public bool HasDisposition()
    {
        return entity.HasDisposition();
    }

    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    public EmailDisposition? GetDisposition()
    {
        return entity.GetDisposition();
    }

    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    public bool HasContentId()
    {
        return entity.HasContentId();
    }

    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    public EmailMessageId? GetContentId()
    {
        return entity.GetContentId();
    }

    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    public bool HasContentLocation()
    {
        return entity.HasContentLocation();
    }

    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    public string? GetContentLocation()
    {
        return entity.GetContentLocation();
    }

    /// <summary>
    /// Copies the decoded content to to a separate destination.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyDecodedContentTo(Stream output)
    {
        return new EmailDecodedContent().CopyToWithEntityAndOutput(entity, output);
    }

    /// <summary>
    /// Copies the decoded content to to a separate destination.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long CopyDecodedContentToWithLimit(Stream output, long maximumBytes)
    {
        return new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(entity, output, maximumBytes);
    }

    /// <summary>
    /// Saves the decoded content.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveDecodedContent(string path)
    {
        return new EmailDecodedContent().SaveWithEntityAndPath(entity, path);
    }

    /// <summary>
    /// Saves the decoded content.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long SaveDecodedContentWithLimit(string path, long maximumBytes)
    {
        return new EmailDecodedContent().SaveWithEntityAndPathAndMaximumBytes(entity, path, maximumBytes);
    }

    /// <summary>
    /// Reads the decoded prefix from the input.
    /// </summary>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ReadDecodedPrefix(int maximumPrefixBytes)
    {
        return new EmailDecodedContent().ReadPrefixWithEntityAndMaximumPrefixBytes(entity, maximumPrefixBytes);
    }

    /// <summary>
    /// Measures the decoded length.
    /// </summary>
    /// <returns>The computed count or size.</returns>
    public long MeasureDecodedLength()
    {
        return new EmailDecodedContent().MeasureLengthWithEntity(entity);
    }

    /// <summary>
    /// Measures the decoded length.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    public long MeasureDecodedLengthWithLimit(long maximumBytes)
    {
        return new EmailDecodedContent().MeasureLengthWithEntityAndMaximumBytes(entity, maximumBytes);
    }

    /// <summary>
    /// Computes the decoded hash.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeDecodedHash(EmailHashAlgorithm algorithm)
    {
        return new EmailDecodedContent().ComputeHashWithEntityAndAlgorithm(entity, algorithm);
    }

    /// <summary>
    /// Computes the decoded hash.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeDecodedHashWithLimit(EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return new EmailDecodedContent().ComputeHashWithEntityAndAlgorithmAndMaximumBytes(entity, algorithm, maximumBytes);
    }

    /// <summary>
    /// Inspects the content type.
    /// </summary>
    /// <returns>The content type analysis produced by the operation.</returns>
    public EmailContentTypeAnalysis InspectContentType()
    {
        return new EmailContentTypeInspector().InspectWithEntityAndFilenameAndPath(entity, filename, path);
    }

    /// <summary>
    /// Decodes the text.
    /// </summary>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeText()
    {
        return new EmailTextDecoder().DecodeWithAttachment(this);
    }

    /// <summary>
    /// Decodes the text.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeTextWithOptions(EmailTextDecodeOptions options)
    {
        return new EmailTextDecoder().DecodeWithAttachmentAndOptions(this, options);
    }

    /// <summary>
    /// Processes the text.
    /// </summary>
    /// <returns>The text decode result produced by the operation.</returns>
    public EmailTextDecodeResult ProcessText()
    {
        return new EmailTextProcessor().DecodeWithAttachment(this);
    }

    /// <summary>
    /// Processes the text.
    /// </summary>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>The text decode result produced by the operation.</returns>
    public EmailTextDecodeResult ProcessTextWithOptions(EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions)
    {
        return new EmailTextProcessor().DecodeWithAttachmentAndTextOptionsAndFlowedOptions(this, textOptions, flowedOptions);
    }
}
