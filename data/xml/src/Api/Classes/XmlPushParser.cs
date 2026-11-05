using System;

namespace xml;

/// <summary>Provides the public incremental XML parser implementation shell.</summary>
public class XmlPushParser : IXmlPushParser
{
    internal XmlParserStatus Status;
    internal PushDriver Driver;

    /// <summary>Creates an incremental XML parser.</summary>
    public XmlPushParser()
    {
        Driver = new PushDriver();
        Status = Driver.InspectXmlPushDriverStatus();
    }

    /// <summary>Begins an incremental XML parse.</summary>
    public XmlParserStatus BeginXmlPush(XmlInput input, XmlParserOptions options, IXmlEventHandler? handler, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        Status = Driver.BeginXmlPushDriver(input, options, handler, resolver, reporter, cancellation);
        return Status;
    }

    /// <summary>Feeds an incremental byte block.</summary>
    public XmlEventBatch FeedXmlPushBytes(ReadOnlyMemory<byte> bytes, bool isFinal)
    {
        XmlEventBatch result = Driver.FeedXmlPushByteBlock(bytes, isFinal);
        Status = result.Status;
        return result;
    }

    /// <summary>Feeds an incremental character block.</summary>
    public XmlEventBatch FeedXmlPushCharacters(ReadOnlyMemory<char> characters, bool isFinal)
    {
        XmlEventBatch result = Driver.FeedXmlPushCharacterBlock(characters, isFinal);
        Status = result.Status;
        return result;
    }

    /// <summary>Finishes the incremental parse.</summary>
    public XmlEventBatch FinishXmlPush()
    {
        XmlEventBatch result = Driver.FinishXmlPushDriver();
        Status = result.Status;
        return result;
    }

    /// <summary>Pauses the incremental parse.</summary>
    public XmlParserStatus PauseXmlPush()
    {
        Status = Driver.PauseXmlPushDriver();
        return Status;
    }

    /// <summary>Resumes the incremental parse.</summary>
    public XmlParserStatus ResumeXmlPush()
    {
        Status = Driver.ResumeXmlPushDriver();
        return Status;
    }

    /// <summary>Aborts the incremental parse.</summary>
    public XmlParserStatus AbortXmlPush()
    {
        Status = Driver.AbortXmlPushDriver();
        return Status;
    }

    /// <summary>Resets the incremental parser.</summary>
    public void ResetXmlPush()
    {
        Driver.ResetXmlPushDriver();
        Status = Driver.InspectXmlPushDriverStatus();
    }

    /// <summary>Closes the incremental parser.</summary>
    public void CloseXmlPush()
    {
        Driver.CloseXmlPushDriver();
        Status = Driver.InspectXmlPushDriverStatus();
    }

    /// <summary>Inspects the incremental parser status.</summary>
    public XmlParserStatus InspectXmlPushStatus()
    {
        Status = Driver.InspectXmlPushDriverStatus();
        return Status;
    }
}
