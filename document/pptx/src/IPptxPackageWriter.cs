using System.Collections.Generic;
using System.IO;

namespace pptx;

internal interface IPptxPackageWriter
{
    PptxResult Write(Stream stream, List<PptxZipEntry> entries, PptxZipWriteOptions options);
}
