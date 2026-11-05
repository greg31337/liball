using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported formula node kind.
/// </summary>
public enum XlsxFormulaNodeKind
{
    /// <summary>
    /// Selects unknown for formula node kind operations.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects literal for formula node kind operations.
    /// </summary>
    Literal = 1,
    /// <summary>
    /// Selects name for formula node kind operations.
    /// </summary>
    Name = 2,
    /// <summary>
    /// Selects reference for formula node kind operations.
    /// </summary>
    Reference = 3,
    /// <summary>
    /// Selects external reference for formula node kind operations.
    /// </summary>
    ExternalReference = 4,
    /// <summary>
    /// Selects structured reference for formula node kind operations.
    /// </summary>
    StructuredReference = 5,
    /// <summary>
    /// Selects unary operation for formula node kind operations.
    /// </summary>
    UnaryOperation = 6,
    /// <summary>
    /// Selects binary operation for formula node kind operations.
    /// </summary>
    BinaryOperation = 7,
    /// <summary>
    /// Selects function call for formula node kind operations.
    /// </summary>
    FunctionCall = 8,
    /// <summary>
    /// Selects array constant for formula node kind operations.
    /// </summary>
    ArrayConstant = 9,
    /// <summary>
    /// Selects range for formula node kind operations.
    /// </summary>
    Range = 10
}
