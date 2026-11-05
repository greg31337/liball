using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with entity factory.
/// </summary>
public class EmailEntityFactory : IEmailEntityFactoryContract
{
    internal static MimeEntity CreateDefaultLeafImplementation(EmailContent content)
    {
        ValidateContent(content);
        return CreateContentEntity(MimeEntityKind.Leaf, new EmailMimeDefaults().CreateDefaultMediaTypeWith(), null, content);
    }

    /// <summary>
    /// Creates a default leaf configured for the requested behavior.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateDefaultLeaf(EmailContent content)
    {
        return EmailEntityFactory.CreateDefaultLeafImplementation(content);
    }

    internal static MimeEntity CreateTextImplementationWithValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string charset = ContainsNonAsciiWithValueUsingString(value) ? "utf-8" : "us-ascii";
        return CreateTextImplementationWithValueAndSubtypeAndCharset(value, "plain", charset);
    }

    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateTextWithValue(string value)
    {
        return EmailEntityFactory.CreateTextImplementationWithValue(value);
    }

    internal static MimeEntity CreateTextImplementationWithValueAndSubtypeAndCharset(string? value, string subtype, string charset)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateToken(subtype, "subtype");
        ValidateToken(charset, "charset");
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(charset);
        if (!lookup.IsFound())
        {
            throw new ArgumentException("The requested text charset is not supported.", "charset");
        }

        string? canonicalCharset = lookup.GetCanonicalName();
        Encoding encoding = Encoding.GetEncoding(lookup.GetCodePage(), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] bytes = encoding.GetBytes(value);
        EmailMediaType mediaType = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted("text", subtype, canonicalCharset, false);
        string transferEncoding = ContainsNonAsciiWithValueUsingValueArray(bytes) ? "8bit" : "7bit";
        return CreateContentEntity(MimeEntityKind.Leaf, mediaType, transferEncoding, new EmailContentOperations().CreateMemoryCopy(bytes, false));
    }

    /// <summary>
    /// Creates a text configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateTextWithValueAndSubtypeAndCharset(string? value, string subtype, string charset)
    {
        return EmailEntityFactory.CreateTextImplementationWithValueAndSubtypeAndCharset(value, subtype, charset);
    }

    internal static MimeEntity CreateContentImplementation(string value, string type, string subtype, string? charset)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateToken(type, "type");
        ValidateToken(subtype, "subtype");
        ValidateToken(charset, "charset");
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(charset);
        if (!lookup.IsFound())
        {
            throw new ArgumentException("The requested content charset is not supported.", "charset");
        }

        string? canonicalCharset = lookup.GetCanonicalName();
        Encoding encoding = Encoding.GetEncoding(lookup.GetCodePage(), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] bytes = encoding.GetBytes(value);
        EmailMediaType mediaType = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted(type, subtype, canonicalCharset, false);
        MimeEntityKind kind = mediaType.IsType("text", "calendar") ? MimeEntityKind.Calendar : MimeEntityKind.Leaf;
        string transferEncoding = ContainsNonAsciiWithValueUsingValueArray(bytes) ? "8bit" : "7bit";
        return CreateContentEntity(kind, mediaType, transferEncoding, new EmailContentOperations().CreateMemoryCopy(bytes, false));
    }

    /// <summary>
    /// Creates a content configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateContent(string value, string type, string subtype, string? charset)
    {
        return EmailEntityFactory.CreateContentImplementation(value, type, subtype, charset);
    }

    internal static MimeEntity CreateCalendarImplementation(string value, string? method, string? charset)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateToken(method, "method");
        ValidateToken(charset, "charset");
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(charset);
        if (!lookup.IsFound())
        {
            throw new ArgumentException("The requested calendar charset is not supported.", "charset");
        }

        string? canonicalCharset = lookup.GetCanonicalName();
        Encoding encoding = Encoding.GetEncoding(lookup.GetCodePage(), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] bytes = encoding.GetBytes(value);
        EmailMediaType mediaType = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndMethodAndDefaulted("text", "calendar", canonicalCharset, (method ?? throw new InvalidOperationException()).ToUpperInvariant(), false);
        string transferEncoding = ContainsNonAsciiWithValueUsingValueArray(bytes) ? "8bit" : "7bit";
        return CreateContentEntity(MimeEntityKind.Calendar, mediaType, transferEncoding, new EmailContentOperations().CreateMemoryCopy(bytes, false));
    }

    /// <summary>
    /// Creates a calendar configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "method">The method used by the operation.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateCalendar(string value, string? method, string? charset)
    {
        return EmailEntityFactory.CreateCalendarImplementation(value, method, charset);
    }

    internal static MimeEntity CreateBinaryImplementationWithValue(byte[] value)
    {
        return CreateBinaryImplementationWithValueAndTypeAndSubtype(value, "application", "octet-stream");
    }

    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateBinaryWithValue(byte[] value)
    {
        return EmailEntityFactory.CreateBinaryImplementationWithValue(value);
    }

    internal static MimeEntity CreateBinaryImplementationWithValueAndTypeAndSubtype(byte[] value, string type, string subtype)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateToken(type, "type");
        ValidateToken(subtype, "subtype");
        EmailMediaType mediaType = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted(type, subtype, null, false);
        return CreateContentEntity(MimeEntityKind.Leaf, mediaType, "base64", new EmailContentOperations().CreateMemoryCopy(value, false));
    }

    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateBinaryWithValueAndTypeAndSubtype(byte[] value, string type, string subtype)
    {
        return EmailEntityFactory.CreateBinaryImplementationWithValueAndTypeAndSubtype(value, type, subtype);
    }

    internal static MimeEntity CreateBinaryImplementationWithContentAndMediaType(EmailContent content, EmailMediaType mediaType)
    {
        ValidateContent(content);
        if (mediaType == null)
        {
            throw new ArgumentNullException("mediaType");
        }

        return CreateContentEntity(MimeEntityKind.Leaf, mediaType, "base64", content);
    }

    /// <summary>
    /// Creates a binary configured for the requested behavior.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateBinaryWithContentAndMediaType(EmailContent content, EmailMediaType mediaType)
    {
        return EmailEntityFactory.CreateBinaryImplementationWithContentAndMediaType(content, mediaType);
    }

    internal static MimeEntity CreateMultipartImplementationWithSubtype(string subtype)
    {
        return CreateMultipartImplementationWithSubtypeAndChildren(subtype, new List<MimeEntity>());
    }

    /// <summary>
    /// Creates a multipart configured for the requested behavior.
    /// </summary>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateMultipartWithSubtype(string subtype)
    {
        return EmailEntityFactory.CreateMultipartImplementationWithSubtype(subtype);
    }

    internal static MimeEntity CreateMultipartImplementationWithSubtypeAndChildren(string subtype, List<MimeEntity> children)
    {
        ValidateToken(subtype, "subtype");
        if (children == null)
        {
            throw new ArgumentNullException("children");
        }

        EmailMediaType mediaType = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted("multipart", subtype, null, false);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), mediaType, null, children, null);
    }

    /// <summary>
    /// Creates a multipart configured for the requested behavior.
    /// </summary>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateMultipartWithSubtypeAndChildren(string subtype, List<MimeEntity> children)
    {
        return EmailEntityFactory.CreateMultipartImplementationWithSubtypeAndChildren(subtype, children);
    }

    internal static MimeEntity CreateNestedMessageImplementation(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), new EmailMimeDefaults().CreateMessageMediaType(false), null, new List<MimeEntity>(), message);
    }

    /// <summary>
    /// Creates a nested message configured for the requested behavior.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateNestedMessage(EmailMessage message)
    {
        return EmailEntityFactory.CreateNestedMessageImplementation(message);
    }

    internal static MimeEntity CreateDigestChildImplementation(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        EmailMediaType digest = CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted("multipart", "digest", null, false);
        EmailMediaType mediaType = new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(digest);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), mediaType, null, new List<MimeEntity>(), message);
    }

    /// <summary>
    /// Creates a digest child configured for the requested behavior.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateDigestChild(EmailMessage message)
    {
        return EmailEntityFactory.CreateDigestChildImplementation(message);
    }

    static MimeEntity CreateContentEntity(MimeEntityKind kind, EmailMediaType mediaType, string? transferEncoding, EmailContent content)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, new EmailHeadersOperations().CreateWith(), mediaType, null, transferEncoding, null, null, null, null, null, content, new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
    }

    static EmailMediaType CreateMediaTypeWithTypeAndSubtypeAndCharsetAndDefaulted(string type, string subtype, string? charset, bool defaulted)
    {
        EmailParameterList parameters = charset == null ? EmptyParameters() : TextParameters(charset, null);
        return new EmailMediaType(type, subtype, parameters, new byte[0], new EmailSourcePosition(0, 1, 1), defaulted);
    }

    static EmailMediaType CreateMediaTypeWithTypeAndSubtypeAndCharsetAndMethodAndDefaulted(string type, string subtype, string? charset, string method, bool defaulted)
    {
        return new EmailMediaType(type, subtype, TextParameters(charset, method), new byte[0], new EmailSourcePosition(0, 1, 1), defaulted);
    }

    static EmailParameterList TextParameters(string? charset, string? method)
    {
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        List<EmailParameter> physicalParameters = new List<EmailParameter>();
        List<EmailRfc2231DecodedParameter> decodedParameters = new List<EmailRfc2231DecodedParameter>();
        AddTextParameter(physicalParameters, decodedParameters, "charset", charset, position);
        if (method != null)
        {
            AddTextParameter(physicalParameters, decodedParameters, "method", method, position);
        }

        return new EmailParameterList(physicalParameters, decodedParameters);
    }

    static void AddTextParameter(List<EmailParameter> physicalParameters, List<EmailRfc2231DecodedParameter> decodedParameters, string name, string? text, EmailSourcePosition position)
    {
        byte[] value = Encoding.ASCII.GetBytes((text ?? throw new InvalidOperationException()));
        physicalParameters.Add(new EmailParameter(name, value, value, false, new byte[0], position));
        List<EmailRfc2231Parameter> sourceParameters = new List<EmailRfc2231Parameter>();
        sourceParameters.Add(new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuotedAndSourcePosition(name, value, false, position));
        decodedParameters.Add(new EmailRfc2231DecodedParameter(name, value, null, false, null, false, false, false, 1, sourceParameters));
    }

    static EmailParameterList EmptyParameters()
    {
        return new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }

    static void ValidateContent(EmailContent value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.IsClosed())
        {
            throw new ObjectDisposedException("value");
        }
    }

    static void ValidateToken(string? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A MIME token cannot be empty.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
            {
                throw new ArgumentException("A MIME token contains an invalid character.", parameterName);
            }

            index++;
        }
    }

    static bool ContainsNonAsciiWithValueUsingString(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool ContainsNonAsciiWithValueUsingValueArray(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
