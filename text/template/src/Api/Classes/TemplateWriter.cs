using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Writes rendered output to a text writer while enforcing the configured maximum output length.
/// </summary>
public class TemplateWriter : ITemplateWriter
{
    /// <summary>
    /// Specifies the writer for this writer.
    /// </summary>
    internal TextWriter Writer;
    /// <summary>
    /// Specifies the security policy for this writer.
    /// </summary>
    internal TemplateSecurityPolicy SecurityPolicy;
    /// <summary>
    /// Specifies the written length for this writer.
    /// </summary>
    internal int WrittenLength;

    /// <summary>
    /// Initializes a new template writer and validates any supplied values.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateWriter(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        Writer = writer;
        SecurityPolicy = new TemplateSecurityPolicy();
        WrittenLength = 0;
    }

    /// <summary>
    /// Configures the security policy used to enforce output limits.
    /// </summary>
    /// <param name="securityPolicy">The policy that supplies output limits.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>securityPolicy</c> is <c>null</c>.</exception>
    public void Configure(TemplateSecurityPolicy securityPolicy)
    {
        if (securityPolicy == null)
        {
            throw new ArgumentNullException("securityPolicy");
        }

        securityPolicy.Validate();
        SecurityPolicy = securityPolicy;
    }

    /// <summary>
    /// Creates a template writer using limits derived from the supplied options.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The configured template writer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateWriter Create(TextWriter writer, TemplateOptions options)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return Create(writer, TemplateSecurityPolicy.FromOptions(options));
    }

    /// <summary>
    /// Creates a template writer using the supplied security policy.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="securityPolicy">The policy that supplies output and runtime limits.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The configured template writer.</returns>
    internal static TemplateWriter Create(TextWriter writer, TemplateSecurityPolicy securityPolicy)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (securityPolicy == null)
        {
            throw new ArgumentNullException("securityPolicy");
        }

        securityPolicy.Validate();

        TemplateWriter templateWriter = new TemplateWriter(writer);
        templateWriter.SecurityPolicy = securityPolicy;
        return templateWriter;
    }

    /// <summary>
    /// Returns the remaining length.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public int GetRemainingLength()
    {
        int remainingLength = SecurityPolicy.MaximumOutputLength - WrittenLength;
        if (remainingLength < 0)
        {
            return 0;
        }

        return remainingLength;
    }

    /// <summary>
    /// Determines whether the requested operation remains within configured limits.
    /// </summary>
    /// <param name="length">The number of characters in the range or output.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument lies outside the supported range.</exception>
    public bool CanWrite(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length > 2147483647 - WrittenLength)
        {
            return false;
        }

        return SecurityPolicy.IsOutputLengthAllowed(WrittenLength + length);
    }

    /// <summary>
    /// Writes the supplied content while enforcing the configured output limit.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        EnsureCanWrite(text.Length);
        Writer.Write(text);
        WrittenLength = WrittenLength + text.Length;
    }

    /// <summary>
    /// Writes the supplied content while enforcing the configured output limit.
    /// </summary>
    /// <param name="value">The value to process.</param>
    public void WriteCharacter(char value)
    {
        EnsureCanWrite(1);
        Writer.Write(value);
        WrittenLength = WrittenLength + 1;
    }

    /// <summary>
    /// Writes the supplied content while enforcing the configured output limit.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void WriteValue(TemplateValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Write(TemplateOutputEncoder.ConvertValueToText(value));
    }

    /// <summary>
    /// Writes the supplied content while enforcing the configured output limit.
    /// </summary>
    public void WriteLine()
    {
        Write(Writer.NewLine);
    }

    /// <summary>
    /// Writes the supplied content while enforcing the configured output limit.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void WriteTextLine(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length > 2147483647 - Writer.NewLine.Length)
        {
            throw new InvalidOperationException("Template output length limit exceeded.");
        }

        int length = text.Length + Writer.NewLine.Length;
        EnsureCanWrite(length);
        Writer.Write(text);
        Writer.Write(Writer.NewLine);
        WrittenLength = WrittenLength + length;
    }

    /// <summary>
    /// Flushes buffered content from the underlying text writer.
    /// </summary>
    public void Flush()
    {
        Writer.Flush();
    }

    internal Task WriteAsync(string text, CancellationToken cancellationToken)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        EnsureCanWrite(text.Length);
        Task task = Writer.WriteAsync(text.AsMemory(), cancellationToken);
        return task.ContinueWith((Task completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                Exception error = completed.Exception;
                if (completed.Exception.InnerExceptions.Count == 1)
                {
                    error = completed.Exception.InnerExceptions[0];
                }

                return Task.FromException(error);
            }

            WrittenLength = WrittenLength + text.Length;
            return Task.CompletedTask;
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task FlushAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return Writer.FlushAsync(cancellationToken);
    }

    internal void EnsureCanWrite(int length)
    {
        if (!CanWrite(length))
        {
            throw new InvalidOperationException("Template output length limit exceeded.");
        }
    }

}
