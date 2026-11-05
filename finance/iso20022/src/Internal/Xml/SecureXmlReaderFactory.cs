using System;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class SecureXmlReaderFactory
{
    internal static XmlReaderSettings CreateSettings(MessageReadOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ResourceLimitGuard guard = new ResourceLimitGuard(options.Limits);
        guard.EnsureInputByteCount(0);
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.CheckCharacters = true;
        settings.CloseInput = true;
        settings.ConformanceLevel = ConformanceLevel.Auto;
        settings.IgnoreComments = !options.PreserveComments;
        settings.IgnoreProcessingInstructions = !options.PreserveProcessingInstructions;
        settings.IgnoreWhitespace = !options.PreserveWhitespace;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = options.Limits.MaxTotalBytes;
        return settings;
    }

    internal static BoundedXmlReader CreateFromBytes(byte[] bytes, MessageReadOptions options)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ResourceLimitGuard guard = new ResourceLimitGuard(options.Limits);
        guard.EnsureInputByteCount(bytes.LongLength);
        XmlReaderSettings settings = CreateSettings(options);
        MemoryStream input = new MemoryStream(bytes, false);
        XmlReader reader = XmlReader.Create(input, settings);
        return new BoundedXmlReader(reader, guard);
    }

    internal static BoundedXmlReader CreateFromText(string text, MessageReadOptions options)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ResourceLimitGuard guard = new ResourceLimitGuard(options.Limits);
        guard.EnsureInputByteCount(Encoding.UTF8.GetByteCount(text));
        XmlReaderSettings settings = CreateSettings(options);
        StringReader input = new StringReader(text);
        XmlReader reader = XmlReader.Create(input, settings);
        return new BoundedXmlReader(reader, guard);
    }
}
