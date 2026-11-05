using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace fix;

internal class DictionaryXmlLoadResult
{
    internal XmlDocument? Document;
    internal Exception? Error;
    internal int Line;
    internal int Column;
}

internal static class DictionaryXmlLoader
{
    internal static DictionaryXmlLoadResult LoadDocument(Stream source, XmlReaderSettings settings)
    {
        DictionaryXmlLoadResult result = new DictionaryXmlLoadResult();
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        using (XmlReader reader = XmlReader.Create(source, settings))
        {
            Task task = Task.Run(() => document.Load(reader));
            while (!task.IsCompleted) Thread.Yield();
            if (task.IsFaulted)
            {
                AggregateException? aggregate = task.Exception;
                Exception? error = aggregate == null ? null : aggregate.GetBaseException();
                result.Error = error;
                XmlException? xmlError = error as XmlException;
                if (xmlError != null)
                {
                    result.Line = xmlError.LineNumber;
                    result.Column = xmlError.LinePosition;
                }
                return result;
            }
        }
        result.Document = document;
        return result;
    }
}
