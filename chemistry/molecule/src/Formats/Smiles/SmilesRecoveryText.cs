using System.Collections.Generic;

namespace molecule;


internal class SmilesRecoveryText
{
    internal string Text;
    internal MoleculeReport Report;
    internal int RecoveredIssueCount;

    internal SmilesRecoveryText()
    {
        Text = "";
        Report = new MoleculeReport();
        RecoveredIssueCount = 0;
    }
}

