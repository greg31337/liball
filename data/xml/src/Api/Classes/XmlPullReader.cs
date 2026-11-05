namespace xml;

/// <summary>Provides the public normalized XML pull reader.</summary>
public class XmlPullReader : IXmlPullReader
{
    internal XmlReaderState State;
    internal PullDriver Driver;

    /// <summary>Creates an XML pull reader.</summary>
    public XmlPullReader()
    {
        Driver = new PullDriver();
        State = Driver.InspectXmlPullDriver();
    }

    /// <summary>Starts a pull parse.</summary>
    public XmlReaderState BeginXmlPull(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        State = Driver.BeginXmlPullDriver(input, options, resolver, reporter, cancellation);
        return State;
    }

    /// <summary>Advances the pull cursor.</summary>
    public XmlReaderState AdvanceXmlPull()
    {
        State = Driver.AdvanceXmlPullDriver();
        return State;
    }

    /// <summary>Inspects the pull cursor.</summary>
    public XmlReaderState InspectXmlPull()
    {
        State = Driver.InspectXmlPullDriver();
        return State;
    }

    /// <summary>Skips the current subtree.</summary>
    public XmlReaderState SkipXmlPullSubtree()
    {
        State = Driver.SkipXmlPullDriverSubtree();
        return State;
    }

    /// <summary>Closes the pull reader.</summary>
    public void CloseXmlPull()
    {
        Driver.CloseXmlPullDriver();
        State = Driver.InspectXmlPullDriver();
    }
}
