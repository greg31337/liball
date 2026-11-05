using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Writes calendar in its wire or storage format.
/// </summary>
public class EmailCalendarWriter : IEmailCalendarWriterContract
{
    internal static byte[] WriteImplementationWithCalendar(EmailCalendar calendar)
    {
        return WriteImplementationWithCalendarAndOptions(calendar, new EmailCalendarWriteOptions());
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] WriteWithCalendar(EmailCalendar calendar)
    {
        return EmailCalendarWriter.WriteImplementationWithCalendar(calendar);
    }

    internal static byte[] WriteImplementationWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        if (calendar == null)
            throw new ArgumentNullException("calendar");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCalendarWriteOptions snapshot = options.Clone();
        List<string> lines = BuildLines(calendar);
        Validate(lines);
        Encoding encoding = GetEncoding(snapshot.GetCharset());
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < lines.Count)
        {
            WriteFolded(lines[index], encoding, snapshot, output);
            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public byte[] WriteWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        return EmailCalendarWriter.WriteImplementationWithCalendarAndOptions(calendar, options);
    }

    internal static MimeEntity CreateEntityImplementationWithCalendar(EmailCalendar calendar)
    {
        return CreateEntityImplementationWithCalendarAndOptions(calendar, new EmailCalendarWriteOptions());
    }

    /// <summary>
    /// Creates an entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateEntityWithCalendar(EmailCalendar calendar)
    {
        return EmailCalendarWriter.CreateEntityImplementationWithCalendar(calendar);
    }

    internal static MimeEntity CreateEntityImplementationWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        if (calendar == null)
            throw new ArgumentNullException("calendar");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCalendarWriteOptions snapshot = options.Clone();
        byte[] bytes = WriteImplementationWithCalendarAndOptions(calendar, snapshot);
        string text = GetEncoding(snapshot.GetCharset()).GetString(bytes);
        if (calendar.HasMethod())
            return new EmailEntityFactory().CreateCalendar(text, calendar.GetMethodName(), snapshot.GetCharset());
        return new EmailEntityFactory().CreateContent(text, "text", "calendar", snapshot.GetCharset());
    }

    /// <summary>
    /// Creates an entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity CreateEntityWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        return EmailCalendarWriter.CreateEntityImplementationWithCalendarAndOptions(calendar, options);
    }

    internal static MimeEntity CreateImipEntityImplementationWithCalendar(EmailCalendar calendar)
    {
        return CreateImipEntityImplementationWithCalendarAndOptions(calendar, new EmailCalendarWriteOptions());
    }

    /// <summary>
    /// Creates an imip entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity CreateImipEntityWithCalendar(EmailCalendar calendar)
    {
        return EmailCalendarWriter.CreateImipEntityImplementationWithCalendar(calendar);
    }

    internal static MimeEntity CreateImipEntityImplementationWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        if (calendar == null)
            throw new ArgumentNullException("calendar");
        if (!calendar.HasMethod())
            throw new InvalidOperationException("An iMIP entity requires a METHOD calendar property.");
        return CreateEntityImplementationWithCalendarAndOptions(calendar, options);
    }

    /// <summary>
    /// Creates an imip entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public MimeEntity CreateImipEntityWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options)
    {
        return EmailCalendarWriter.CreateImipEntityImplementationWithCalendarAndOptions(calendar, options);
    }

    static List<string> BuildLines(EmailCalendar calendar)
    {
        List<string> lines = new List<string>();
        lines.Add("BEGIN:VCALENDAR");
        List<EmailCalendarProperty> properties = calendar.GetProperties();
        int index = 0;
        while (index < properties.Count)
        {
            lines.Add(FormatProperty(properties[index]));
            index++;
        }

        List<EmailCalendarComponent> components = calendar.GetComponents();
        index = 0;
        while (index < components.Count)
        {
            AddComponent(lines, components[index]);
            index++;
        }

        lines.Add("END:VCALENDAR");
        return lines;
    }

    static void AddComponent(List<string> lines, EmailCalendarComponent component)
    {
        lines.Add("BEGIN:" + component.GetName());
        List<EmailCalendarProperty> properties = component.GetProperties();
        int index = 0;
        while (index < properties.Count)
        {
            lines.Add(FormatProperty(properties[index]));
            index++;
        }

        List<EmailCalendarComponent> children = component.GetComponents();
        index = 0;
        while (index < children.Count)
        {
            AddComponent(lines, children[index]);
            index++;
        }

        lines.Add("END:" + component.GetName());
    }

    static string FormatProperty(EmailCalendarProperty property)
    {
        StringBuilder output = new StringBuilder();
        output.Append(property.GetName());
        List<EmailCalendarParameter> parameters = property.GetParameters();
        int index = 0;
        while (index < parameters.Count)
        {
            EmailCalendarParameter parameter = parameters[index];
            output.Append(';');
            output.Append(parameter.GetName());
            output.Append('=');
            int valueIndex = 0;
            while (valueIndex < parameter.GetValueCount())
            {
                if (valueIndex > 0)
                    output.Append(',');
                output.Append(FormatParameterValue(parameter.GetValueAt(valueIndex)));
                valueIndex++;
            }

            index++;
        }

        output.Append(':');
        output.Append(property.GetRawValue());
        return output.ToString();
    }

    static string FormatParameterValue(string value)
    {
        bool quote = value.IndexOf(':') >= 0 || value.IndexOf(';') >= 0 || value.IndexOf(',') >= 0 || value.Length > 0 && (value[0] == ' ' || value[value.Length - 1] == ' ');
        return quote ? "\"" + value + "\"" : value;
    }

    static void Validate(List<string> lines)
    {
        string source = string.Join("\r\n", lines) + "\r\n";
        EmailCalendarParseOptions options = new EmailCalendarParseOptions();
        options.SetMode(EmailStructuredValueParseMode.Strict);
        EmailCalendarParseResult parsed = new EmailCalendarParser().ParseWithInputAndOptionsUsingStringAndEmailCalendarParseOptions(source, options);
        if (!parsed.IsSuccess())
            throw new InvalidOperationException("The calendar model cannot be generated because required or structured values are invalid.");
    }

    static void WriteFolded(string line, Encoding encoding, EmailCalendarWriteOptions options, List<byte> output)
    {
        int index = 0;
        bool continuation = false;
        if (line.Length == 0)
        {
            AppendCrlf(output, options.GetMaximumOutputBytes());
            return;
        }

        while (index < line.Length)
        {
            int available = options.GetFoldOctets() - (continuation ? 1 : 0);
            int start = index;
            int used = 0;
            while (index < line.Length)
            {
                int characterLength = char.IsHighSurrogate(line[index]) && index + 1 < line.Length && char.IsLowSurrogate(line[index + 1]) ? 2 : 1;
                int bytes = encoding.GetByteCount(line.Substring(index, characterLength));
                if (used > 0 && used + bytes > available)
                    break;
                if (bytes > available)
                    throw new InvalidOperationException("A calendar character cannot fit within the configured fold width.");
                used += bytes;
                index += characterLength;
            }

            if (continuation)
                AppendWithOutputAndValueAndMaximum(output, 32, options.GetMaximumOutputBytes());
            byte[] segment = encoding.GetBytes(line.Substring(start, index - start));
            AppendWithOutputAndValuesAndMaximum(output, segment, options.GetMaximumOutputBytes());
            AppendCrlf(output, options.GetMaximumOutputBytes());
            continuation = true;
        }
    }

    static Encoding GetEncoding(string? charset)
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(charset);
        return Encoding.GetEncoding(lookup.GetCodePage(), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    static void AppendCrlf(List<byte> output, int maximum)
    {
        AppendWithOutputAndValueAndMaximum(output, 13, maximum);
        AppendWithOutputAndValueAndMaximum(output, 10, maximum);
    }

    static void AppendWithOutputAndValueAndMaximum(List<byte> output, byte value, int maximum)
    {
        if (output.Count >= maximum)
            throw new EmailLimitException("MaxCalendarOutputBytes", maximum, output.Count + 1);
        output.Add(value);
    }

    static void AppendWithOutputAndValuesAndMaximum(List<byte> output, byte[] values, int maximum)
    {
        int index = 0;
        while (index < values.Length)
        {
            AppendWithOutputAndValueAndMaximum(output, values[index], maximum);
            index++;
        }
    }
}
