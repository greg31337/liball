using System;
using System.IO;
using System.Xml;

namespace iso20022;

internal static class XmiPassOneReader
{
    internal static XmiPassOneResult BuildIndex(
        Stream source,
        string sourceName,
        Iso20022Ecore2013Adapter adapter,
        XmiPassOneOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (sourceName == null)
        {
            throw new ArgumentNullException("sourceName");
        }

        if (adapter == null)
        {
            throw new ArgumentNullException("adapter");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateOptions(options);
        long originalPosition = 0;
        bool restorePosition = source.CanSeek;
        if (restorePosition)
        {
            originalPosition = source.Position;
        }

        XmlReaderSettings settings = CreatePassOneSettings(options.MaximumCharacters);
        XmiPassOneState state = new XmiPassOneState(adapter, options);
        using (XmlReader reader = XmlReader.Create(source, settings, sourceName))
        {
            bool hasNode = reader.Read();
            while (hasNode)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    state.RecordElement(reader, sourceName);
                }

                hasNode = reader.Read();
            }
        }

        XmiPassOneResult result = state.CompleteIndex();
        if (restorePosition)
        {
            source.Position = originalPosition;
        }

        return result;
    }

    internal static XmlReaderSettings CreatePassOneSettings(int maximumCharacters)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.CheckCharacters = true;
        settings.CloseInput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = maximumCharacters;
        return settings;
    }

    internal static void ValidateOptions(XmiPassOneOptions options)
    {
        ValidatePositive(options.MaximumCharacters, "MaximumCharacters");
        ValidatePositive(options.MaximumDepth, "MaximumDepth");
        ValidatePositive(options.MaximumAttributesPerElement, "MaximumAttributesPerElement");
        ValidatePositive(options.MaximumElementCount, "MaximumElementCount");
        ValidatePositive(options.MaximumIdentifierCount, "MaximumIdentifierCount");
        ValidatePositive(options.MaximumDuplicateCount, "MaximumDuplicateCount");
    }

    internal static void ValidatePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("options", fieldName + " must be positive.");
        }
    }
}
