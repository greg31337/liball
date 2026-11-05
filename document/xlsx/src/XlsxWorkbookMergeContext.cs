using System;
using System.Collections.Generic;

namespace xlsx;

internal class XlsxWorkbookMergeContext
{
    internal XlsxWorkbook Destination;
    internal XlsxWorkbook Source;
    internal XlsxZipPackageReadResult SourcePackage;
    internal XlsxContentTypesPart SourceContentTypes;
    internal XlsxWorkbookMergeOptions Options;
    internal XlsxWorkbookMergeResult Result;
    internal int[] StyleMap;
    internal int[] SharedStringMap;
    internal List<XlsxWorksheetPart> CopiedWorksheets;

    internal XlsxWorkbookMergeContext(XlsxWorkbook destination, XlsxWorkbook source, XlsxZipPackageReadResult sourcePackage, XlsxContentTypesPart sourceContentTypes, XlsxWorkbookMergeOptions options, XlsxWorkbookMergeResult result)
    {
        Destination = destination;
        Source = source;
        SourcePackage = sourcePackage;
        SourceContentTypes = sourceContentTypes;
        Options = options;
        Result = result;
        StyleMap = new int[0];
        SharedStringMap = new int[0];
        CopiedWorksheets = new List<XlsxWorksheetPart>();
    }
}
