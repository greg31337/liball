using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Represents an attachment source.
/// </summary>
public class EmailAttachmentSource : IEmailAttachmentSourceContract
{
    EmailContent content;
    string? filename;
    EmailMediaType mediaType;
    bool mediaTypeExplicit;
    EmailMessageId? contentId;
    string? contentLocation;
    string? description;
    EmailAttachmentSource(EmailContent content, string? filename)
    {
        ValidateContent(content, "content");
        this.content = content;
        this.filename = null;
        mediaType = ParseMediaType("application/octet-stream");
        mediaTypeExplicit = false;
        contentId = null;
        contentLocation = null;
        description = null;
        if (filename != null)
        {
            SetFilename(filename);
        }
    }

    internal static EmailAttachmentSource CreateMemoryImplementationWithValue(byte[] value)
    {
        return CreateMemoryImplementationWithValueAndFilename(value, null);
    }

    internal static EmailAttachmentSource CreateMemoryImplementationWithValueAndFilename(byte[] value, string? filename)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return new EmailAttachmentSource(new EmailContentOperations().CreateMemoryCopy(value, false), filename);
    }

    internal static EmailAttachmentSource CreateStreamImplementationWithValueAndOwnership(Stream value, EmailContentOwnership ownership)
    {
        return CreateStreamImplementationWithValueAndOwnershipAndFilename(value, ownership, null);
    }

    internal static EmailAttachmentSource CreateStreamImplementationWithValueAndOwnershipAndFilename(Stream value, EmailContentOwnership ownership, string? filename)
    {
        return new EmailAttachmentSource(new EmailContentOperations().CreateStream(value, ownership), filename);
    }

    internal static EmailAttachmentSource CreateFileImplementationWithPath(string path)
    {
        ValidateLocalPath(path);
        string fullPath = Path.GetFullPath(path);
        return new EmailAttachmentSource(new EmailContentOperations().CreateFile(fullPath, EmailContentOwnership.Borrowed), Path.GetFileName(fullPath));
    }

    internal static EmailAttachmentSource CreateFileImplementationWithPathAndFilename(string path, string filename)
    {
        ValidateLocalPath(path);
        return new EmailAttachmentSource(new EmailContentOperations().CreateFile(path, EmailContentOwnership.Borrowed), filename);
    }

    internal static EmailAttachmentSource CreateContentImplementationWithValue(EmailContent value)
    {
        return CreateContentImplementationWithValueAndFilename(value, null);
    }

    internal static EmailAttachmentSource CreateContentImplementationWithValueAndFilename(EmailContent value, string? filename)
    {
        return new EmailAttachmentSource(value, filename);
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    public EmailContent GetContent()
    {
        return content;
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
    /// Sets the filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetFilename(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("An attachment filename cannot be empty.", "value");
        }

        filename = new EmailFilename().SanitizeWithFilename(value).GetFilename();
        if (!mediaTypeExplicit)
        {
            mediaType = InferMediaType(filename);
        }
    }

    /// <summary>
    /// Clears the stored filename value or values.
    /// </summary>
    public void ClearFilename()
    {
        filename = null;
        if (!mediaTypeExplicit)
        {
            mediaType = ParseMediaType("application/octet-stream");
        }
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public EmailMediaType GetMediaType()
    {
        return mediaType;
    }

    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMediaType(string value)
    {
        EmailMediaType parsed = ParseMediaType(value);
        ValidateSourceMediaType(parsed, "value");
        mediaType = parsed;
        mediaTypeExplicit = true;
    }

    /// <summary>
    /// Clears the stored media type value or values.
    /// </summary>
    public void ClearMediaType()
    {
        mediaTypeExplicit = false;
        mediaType = filename == null ? ParseMediaType("application/octet-stream") : InferMediaType(filename);
    }

    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    public bool HasContentId()
    {
        return contentId != null;
    }

    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    public EmailMessageId? GetContentId()
    {
        return contentId;
    }

    /// <summary>
    /// Sets the content ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetContentId(EmailMessageId? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!new EmailMessageIdFormatter().FormatContentIdWithIdentifier(value).IsSuccess())
        {
            throw new ArgumentException("The content identifier cannot be formatted safely.", "value");
        }

        contentId = value;
    }

    /// <summary>
    /// Clears the stored content ID value or values.
    /// </summary>
    public void ClearContentId()
    {
        contentId = null;
    }

    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    public bool HasContentLocation()
    {
        return contentLocation != null;
    }

    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    public string? GetContentLocation()
    {
        return contentLocation;
    }

    /// <summary>
    /// Sets the content location used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public void SetContentLocation(string? value)
    {
        ValidateText(value, "value");
        if ((value ?? throw new InvalidOperationException()).Length == 0)
        {
            throw new ArgumentException("A content location cannot be empty.", "value");
        }

        contentLocation = value;
    }

    /// <summary>
    /// Clears the stored content location value or values.
    /// </summary>
    public void ClearContentLocation()
    {
        contentLocation = null;
    }

    /// <summary>
    /// Determines whether the description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the description is available; otherwise, <c>false</c>.</returns>
    public bool HasDescription()
    {
        return description != null;
    }

    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <returns>The current description.</returns>
    public string? GetDescription()
    {
        return description;
    }

    /// <summary>
    /// Sets the description used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetDescription(string? value)
    {
        ValidateText(value, "value");
        description = value;
    }

    /// <summary>
    /// Clears the stored description value or values.
    /// </summary>
    public void ClearDescription()
    {
        description = null;
    }

    /// <summary>
    /// Creates an attachment entity configured for the requested behavior.
    /// </summary>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateAttachmentEntity()
    {
        string attachmentFilename = filename == null ? "attachment" : filename;
        return CreateEntity(false, attachmentFilename);
    }

    /// <summary>
    /// Creates a resource entity configured for the requested behavior.
    /// </summary>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public MimeEntity CreateResourceEntity()
    {
        if (contentId == null && contentLocation == null)
        {
            throw new InvalidOperationException("A related resource requires a content ID or content location.");
        }

        return CreateEntity(true, filename);
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailAttachmentSource Clone()
    {
        EmailAttachmentSource copy = new EmailAttachmentSource(content, null);
        copy.filename = filename;
        copy.mediaType = mediaType;
        copy.mediaTypeExplicit = mediaTypeExplicit;
        copy.contentId = contentId;
        copy.contentLocation = contentLocation;
        copy.description = description;
        return copy;
    }

    MimeEntity CreateEntity(bool inline, string? entityFilename)
    {
        ValidateContent(content, "content");
        EmailMediaType entityMediaType = entityFilename == null ? mediaType : AddParameterWithSourceAndNameAndValueUsingEmailMediaTypeAndStringAndString(mediaType, "name", entityFilename);
        EmailDisposition disposition = CreateDisposition(inline ? "inline" : "attachment", entityFilename);
        MimeEntityKind kind = GetEntityKind(entityMediaType);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, new EmailHeadersOperations().CreateWith(), entityMediaType, disposition, "base64", contentId, contentLocation, null, description, null, content, new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    static EmailMediaType InferMediaType(string filename)
    {
        string? inferred = new EmailMimeType().GetMediaTypeForFilename(filename);
        EmailMediaType parsed = ParseMediaType(inferred == null ? "application/octet-stream" : inferred);
        if (string.Equals(parsed.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase) || string.Equals(parsed.GetTopLevelType(), "message", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMediaType("application/octet-stream");
        }

        return parsed;
    }

    static MimeEntityKind GetEntityKind(EmailMediaType value)
    {
        if (value.IsType("text", "calendar"))
        {
            return MimeEntityKind.Calendar;
        }

        if (value.IsType("application", "ms-tnef") || value.IsType("application", "vnd.ms-tnef") || value.IsType("application", "x-tnef"))
        {
            return MimeEntityKind.Tnef;
        }

        return MimeEntityKind.Leaf;
    }

    static void ValidateSourceMediaType(EmailMediaType value, string parameterName)
    {
        if (string.Equals(value.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase) || string.Equals(value.GetTopLevelType(), "message", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Attachment content sources require a non-container media type.", parameterName);
        }
    }

    static EmailMediaType ParseMediaType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        EmailMediaTypeParseResult parsed = new EmailMediaTypeParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailStructuredValueParseMode.Strict);
        if (!parsed.IsSuccess() || !parsed.IsComplete())
        {
            throw new ArgumentException("A valid complete media type is required.", "value");
        }

        return parsed.GetValue();
    }

    static EmailMediaType AddParameterWithSourceAndNameAndValueUsingEmailMediaTypeAndStringAndString(EmailMediaType source, string name, string value)
    {
        EmailParameterList parameters = AddParameterWithSourceAndNameAndValueUsingEmailParameterListAndStringAndString(source.GetParameters(), name, value);
        return new EmailMediaType(source.GetTopLevelType(), source.GetSubtype(), parameters, new byte[0], new EmailSourcePosition(0, 1, 1), false);
    }

    static EmailDisposition CreateDisposition(string type, string? filename)
    {
        EmailParameterList parameters = filename == null ? EmptyParameters() : AddParameterWithSourceAndNameAndValueUsingEmailParameterListAndStringAndString(EmptyParameters(), "filename", filename);
        return new EmailDisposition(type, parameters, new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    static EmailParameterList AddParameterWithSourceAndNameAndValueUsingEmailParameterListAndStringAndString(EmailParameterList source, string name, string value)
    {
        List<EmailParameter> physical = source.GetAll();
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        int index = 0;
        while (index < source.GetDecodedCount())
        {
            decoded.Add(source.GetDecodedAt(index));
            index++;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter(name, bytes, bytes, true, new byte[0], position));
        decoded.Add(new EmailRfc2231DecodedParameter(name, bytes, "utf-8", true, null, false, true, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailParameterList(physical, decoded);
    }

    static EmailParameterList EmptyParameters()
    {
        return new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }

    static void ValidateContent(EmailContent value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.IsClosed())
        {
            throw new ObjectDisposedException(parameterName);
        }
    }

    static void ValidateText(string? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == 0 || current < 32 && current != '\t' || current == 127)
            {
                throw new ArgumentException("MIME metadata cannot contain line breaks, NUL, or unsafe controls.", parameterName);
            }

            index++;
        }
    }

    static void ValidateLocalPath(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (path.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || path.IndexOf("://", StringComparison.Ordinal) >= 0)
        {
            throw new ArgumentException("Only local filesystem paths are supported; URL fetching is not available.", "path");
        }
    }
}
