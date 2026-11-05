namespace pptx.Tests;

public class PptxFixtureDeck
{
    public string Family;
    public string DisplayName;
    public string Application;
    public string Source;
    public byte[] Bytes;

    public PptxFixtureDeck(string family, string displayName, string application, string source, byte[] bytes)
    {
        Family = family;
        DisplayName = displayName;
        Application = application;
        Source = source;
        Bytes = bytes;
    }
}
