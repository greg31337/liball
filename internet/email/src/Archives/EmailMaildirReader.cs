using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailMaildirScanState
{
    internal List<EmailMaildirEntry> entries;
    internal EmailIssueList issues;
    internal long archiveBytes;

    internal EmailMaildirScanState()
    {
        entries = new List<EmailMaildirEntry>();
        issues = new EmailIssueList();
        archiveBytes = 0;
    }
}
