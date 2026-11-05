using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists Excel cell error values supported by typed cell-value operations.
/// </summary>
public enum XlsxCellErrorKind
{
    /// <summary>Selects the null intersection error.</summary>
    Null = 0,
    /// <summary>Selects the division by zero error.</summary>
    DivisionByZero = 1,
    /// <summary>Selects the invalid value error.</summary>
    Value = 2,
    /// <summary>Selects the invalid reference error.</summary>
    Reference = 3,
    /// <summary>Selects the unknown name error.</summary>
    Name = 4,
    /// <summary>Selects the invalid number error.</summary>
    Number = 5,
    /// <summary>Selects the unavailable value error.</summary>
    NotAvailable = 6,
    /// <summary>Selects the data retrieval error.</summary>
    GettingData = 7,
    /// <summary>Selects the dynamic-array spill error.</summary>
    Spill = 8,
    /// <summary>Selects the calculation error.</summary>
    Calculation = 9,
    /// <summary>Selects the linked-data field error.</summary>
    Field = 10,
    /// <summary>Selects the blocked-data error.</summary>
    Blocked = 11,
    /// <summary>Selects the unknown-data error.</summary>
    Unknown = 12,
    /// <summary>Selects the external connection error.</summary>
    Connection = 13,
    /// <summary>Selects the linked-data error.</summary>
    Data = 14,
    /// <summary>Selects the busy calculation error.</summary>
    Busy = 15,
    /// <summary>Selects the Python calculation error.</summary>
    Python = 16
}
