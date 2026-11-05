using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents one typed pivot-cache record.
/// </summary>
public class XlsxPivotCacheRecord
{
    /// <summary>Contains values in cache-field order.</summary>
    public List<XlsxPivotCacheValue> Values;

    /// <summary>Initializes an empty pivot-cache record.</summary>
    public XlsxPivotCacheRecord()
    {
        Values = new List<XlsxPivotCacheValue>();
    }
}
