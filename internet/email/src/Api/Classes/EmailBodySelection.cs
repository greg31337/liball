using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a body selection.
/// </summary>
public class EmailBodySelection : IEmailBodySelectionContract
{
    MimeEntity? body;
    EmailBodyPreference preference;
    EmailBodySelectionQuality quality;
    EmailBodySelectionReason reason;
    EmailPath? path;
    List<MimeEntity> alternatives;
    List<EmailBodySelectionContext> contexts;
    /// <summary>
    /// Creates a new body selection.
    /// </summary>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "quality">The quality used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "alternatives">The alternatives used by the operation.</param>
    /// <param name = "contexts">The contexts used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailBodySelection(MimeEntity? body, EmailBodyPreference preference, EmailBodySelectionQuality quality, EmailBodySelectionReason reason, EmailPath? path, List<MimeEntity> alternatives, List<EmailBodySelectionContext> contexts)
    {
        ValidatePreference(preference);
        ValidateQuality(quality);
        ValidateReason(reason);
        if (alternatives == null)
        {
            throw new ArgumentNullException("alternatives");
        }

        if (contexts == null)
        {
            throw new ArgumentNullException("contexts");
        }

        if (body == null && path != null)
        {
            throw new ArgumentException("An unavailable body cannot have a path.", "path");
        }

        if (body != null && path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (body == null && quality != EmailBodySelectionQuality.Unavailable)
        {
            throw new ArgumentException("A missing body must have unavailable quality.", "quality");
        }

        if (body != null && quality == EmailBodySelectionQuality.Unavailable)
        {
            throw new ArgumentException("A selected body cannot have unavailable quality.", "quality");
        }

        if (body == null && reason != EmailBodySelectionReason.EncryptedContentUnavailable && reason != EmailBodySelectionReason.NoSupportedBody)
        {
            throw new ArgumentException("A missing body requires an unavailable selection reason.", "reason");
        }

        if (body != null && reason != EmailBodySelectionReason.PreferredMediaType && reason != EmailBodySelectionReason.FallbackMediaType)
        {
            throw new ArgumentException("A selected body requires a successful selection reason.", "reason");
        }

        if (reason == EmailBodySelectionReason.PreferredMediaType && quality != EmailBodySelectionQuality.Preferred)
        {
            throw new ArgumentException("A preferred-media-type reason requires preferred quality.", "quality");
        }

        if (reason == EmailBodySelectionReason.FallbackMediaType && quality != EmailBodySelectionQuality.Fallback)
        {
            throw new ArgumentException("A fallback-media-type reason requires fallback quality.", "quality");
        }

        if ((reason == EmailBodySelectionReason.EncryptedContentUnavailable || reason == EmailBodySelectionReason.NoSupportedBody) && quality != EmailBodySelectionQuality.Unavailable)
        {
            throw new ArgumentException("An unavailable reason requires unavailable quality.", "quality");
        }

        ValidateAlternatives(alternatives);
        ValidateContexts(contexts);
        if (body == null && alternatives.Count != 0)
        {
            throw new ArgumentException("A missing body cannot have alternatives.", "alternatives");
        }

        if (body != null && !ContainsReference(alternatives, body))
        {
            throw new ArgumentException("The selected body must occur in the alternatives list.", "alternatives");
        }

        this.body = body;
        this.preference = preference;
        this.quality = quality;
        this.reason = reason;
        this.path = path;
        this.alternatives = new List<MimeEntity>(alternatives);
        this.contexts = new List<EmailBodySelectionContext>(contexts);
    }

    /// <summary>
    /// Determines whether the body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body is available; otherwise, <c>false</c>.</returns>
    public bool HasBody()
    {
        return body != null;
    }

    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <returns>The current body.</returns>
    public MimeEntity? GetBody()
    {
        return body;
    }

    /// <summary>
    /// Gets the preference.
    /// </summary>
    /// <returns>The current preference.</returns>
    public EmailBodyPreference GetPreference()
    {
        return preference;
    }

    /// <summary>
    /// Gets the quality.
    /// </summary>
    /// <returns>The current quality.</returns>
    public EmailBodySelectionQuality GetQuality()
    {
        return quality;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public EmailBodySelectionReason GetReason()
    {
        return reason;
    }

    /// <summary>
    /// Determines whether the path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the path is available; otherwise, <c>false</c>.</returns>
    public bool HasPath()
    {
        return path != null;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <returns>The current path.</returns>
    public EmailPath? GetPath()
    {
        return path;
    }

    /// <summary>
    /// Gets the alternative count.
    /// </summary>
    /// <returns>The current alternative count.</returns>
    public int GetAlternativeCount()
    {
        return alternatives.Count;
    }

    /// <summary>
    /// Gets the alternative at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested alternative.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public MimeEntity GetAlternativeAt(int index)
    {
        if (index < 0 || index >= alternatives.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return alternatives[index];
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternatives()
    {
        return new List<MimeEntity>(alternatives);
    }

    /// <summary>
    /// Gets the context count.
    /// </summary>
    /// <returns>The current context count.</returns>
    public int GetContextCount()
    {
        return contexts.Count;
    }

    /// <summary>
    /// Gets the context at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested context.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBodySelectionContext GetContextAt(int index)
    {
        if (index < 0 || index >= contexts.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return contexts[index];
    }

    /// <summary>
    /// Gets the contexts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current contexts.</returns>
    public List<EmailBodySelectionContext> GetContexts()
    {
        return new List<EmailBodySelectionContext>(contexts);
    }

    /// <summary>
    /// Determines whether the context is present or available.
    /// </summary>
    /// <param name = "context">The context used by the operation.</param>
    /// <returns><c>true</c> when the context is available; otherwise, <c>false</c>.</returns>
    public bool HasContext(EmailBodySelectionContext context)
    {
        ValidateContext(context);
        return contexts.Contains(context);
    }

    static void ValidateAlternatives(List<MimeEntity> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Body alternatives cannot contain null entries.", "alternatives");
            }

            index++;
        }
    }

    static void ValidateContexts(List<EmailBodySelectionContext> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            ValidateContext(values[index]);
            index++;
        }
    }

    static bool ContainsReference(List<MimeEntity> values, MimeEntity expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (object.ReferenceEquals(values[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidatePreference(EmailBodyPreference value)
    {
        if (value < EmailBodyPreference.Html || value > EmailBodyPreference.PlainText)
        {
            throw new ArgumentOutOfRangeException("preference");
        }
    }

    static void ValidateQuality(EmailBodySelectionQuality value)
    {
        if (value < EmailBodySelectionQuality.Preferred || value > EmailBodySelectionQuality.Unavailable)
        {
            throw new ArgumentOutOfRangeException("quality");
        }
    }

    static void ValidateReason(EmailBodySelectionReason value)
    {
        if (value < EmailBodySelectionReason.PreferredMediaType || value > EmailBodySelectionReason.NoSupportedBody)
        {
            throw new ArgumentOutOfRangeException("reason");
        }
    }

    static void ValidateContext(EmailBodySelectionContext value)
    {
        if (value < EmailBodySelectionContext.Alternative || value > EmailBodySelectionContext.UnknownMultipart)
        {
            throw new ArgumentOutOfRangeException("context");
        }
    }
}
