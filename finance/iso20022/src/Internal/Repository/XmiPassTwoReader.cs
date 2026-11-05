using System;
using System.IO;
using System.Xml;

namespace iso20022;

internal static class XmiPassTwoReader
{
    internal static XmiPassTwoResult BuildGraph(
        Stream source,
        string sourceName,
        Iso20022Ecore2013Adapter adapter,
        XmiPassOneResult passOne,
        XmiPassTwoOptions options)
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

        if (passOne == null)
        {
            throw new ArgumentNullException("passOne");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateGraphOptions(options);
        long originalPosition = 0;
        bool restorePosition = source.CanSeek;
        if (restorePosition)
        {
            originalPosition = source.Position;
        }

        XmlReaderSettings settings =
            XmiPassOneReader.CreatePassOneSettings(options.ReaderLimits.MaximumCharacters);
        XmiPassTwoState state = new XmiPassTwoState(adapter, passOne, options);
        using (XmlReader reader = XmlReader.Create(source, settings, sourceName))
        {
            bool hasNode = reader.Read();
            while (hasNode)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    state.RecordGraphElement(reader, sourceName);
                }

                if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                {
                    state.RecordGraphText(reader);
                }

                hasNode = reader.Read();
            }
        }

        XmiPassTwoResult result = state.CompleteGraph();
        if (restorePosition)
        {
            source.Position = originalPosition;
        }

        return result;
    }

    internal static void ValidateGraphOptions(XmiPassTwoOptions options)
    {
        XmiPassOneReader.ValidateOptions(options.ReaderLimits);
        XmiPassOneReader.ValidatePositive(options.MaximumAttributeCount, "MaximumAttributeCount");
        XmiPassOneReader.ValidatePositive(options.MaximumRelationshipCount, "MaximumRelationshipCount");
        XmiPassOneReader.ValidatePositive(options.MaximumDiagnosticCount, "MaximumDiagnosticCount");
        XmiPassOneReader.ValidatePositive(
            options.MaximumAttributeValueLength,
            "MaximumAttributeValueLength");
    }
}
