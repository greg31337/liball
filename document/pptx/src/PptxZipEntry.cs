namespace pptx;

internal class PptxZipEntry
{
    internal string Name;
    internal string PartName;
    internal byte[] Bytes;
    internal long Length;
    internal long CompressedLength;

    internal PptxZipEntry(string name, string partName, byte[] bytes, long length, long compressedLength)
    {
        Name = PptxDiagnostic.CleanText(name);
        PartName = PptxDiagnostic.CleanText(partName);
        if (bytes == null)
        {
            Bytes = new byte[0];
        }
        else
        {
            Bytes = bytes;
        }

        Length = length;
        CompressedLength = compressedLength;
    }
}
