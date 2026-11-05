namespace xml;

/// <summary>Provides a caller-controlled cancellation flag implementation shell.</summary>
public class XmlCancellationFlag : IXmlCancellation
{
    internal bool CancellationRequested;

    /// <summary>Creates an unset cancellation flag.</summary>
    public XmlCancellationFlag()
    {
        CancellationRequested = false;
    }

    /// <summary>Tests whether cancellation is requested.</summary>
    public bool IsXmlCancellationRequested()
    {
        return CancellationRequested;
    }
}
