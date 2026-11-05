namespace tiff;

internal class TiffReaderLifetime
{
    internal ITiffRandomAccessSource Source;
    internal bool IsOpen;
    internal bool CloseSource;
    internal long VisibleLength;

    internal TiffReaderLifetime(ITiffRandomAccessSource source, bool closeSource, bool isOpen)
    {
        Source = source;
        CloseSource = closeSource;
        IsOpen = isOpen;
        if (source != null && source.GetIsOpen())
        {
            VisibleLength = source.GetLength();
        }
        else
        {
            VisibleLength = 0;
        }
    }

    internal void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        if (CloseSource && Source != null)
        {
            Source.Close();
        }
    }

    internal void CloseAfterFailedOpen()
    {
        IsOpen = false;
        if (CloseSource && Source != null)
        {
            Source.Close();
        }
    }
}
