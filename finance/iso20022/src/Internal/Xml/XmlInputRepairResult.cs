using System.Collections.Generic;

namespace iso20022;

internal class XmlInputRepairResult
{
    internal string Text = string.Empty;
    internal byte[] Bytes = [];
    internal bool Changed;
    internal List<MessageRepair> Repairs = new List<MessageRepair>();
}
