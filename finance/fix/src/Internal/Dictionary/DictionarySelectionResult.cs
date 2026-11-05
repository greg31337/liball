using System.Collections.Generic;

namespace fix;

internal class DictionarySelectionResult
{
    internal bool Success;
    internal FixDictionaryIdentity TransportIdentity = new FixDictionaryIdentity();
    internal FixDictionaryIdentity ApplicationIdentity = new FixDictionaryIdentity();
    internal List<DictionaryDiagnostic> Diagnostics = [];
}
