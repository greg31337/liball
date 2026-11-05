using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal class ExternalCodeXsdReadState
{
    internal bool Success = true;
    internal bool SawSchema;
    internal ExternalCodeCatalog Catalog = new ExternalCodeCatalog();
    internal ExternalCodeSet? CurrentCodeSet;
    internal ExternalCodeValue? CurrentCodeValue;
    internal HashSet<string> CodeSetIdentifiers = new HashSet<string>();
    internal ExternalCodeXsdCaptureKind CaptureKind;
    internal int CaptureDepth = -1;
    internal StringBuilder CaptureText = new StringBuilder();
    internal ExternalCodeImportRequest Request = new ExternalCodeImportRequest();
    internal ExternalCodeImportResult Result = new ExternalCodeImportResult();
}
