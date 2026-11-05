using System.IO;

namespace pptx;

internal interface IPptxPackageReader
{
    PptxZipReadResult Read(Stream stream, PptxZipReadOptions options);
}
