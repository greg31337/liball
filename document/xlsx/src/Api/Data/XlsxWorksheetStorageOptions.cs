using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of worksheet storage operations.
/// </summary>
public class XlsxWorksheetStorageOptions
{
    /// <summary>
    /// Stores the force sparse value used by this model.
    /// </summary>
    public bool ForceSparse;
    /// <summary>
    /// Stores the force dense value used by this model.
    /// </summary>
    public bool ForceDense;
    /// <summary>
    /// Stores the force streaming value used by this model.
    /// </summary>
    public bool ForceStreaming;
    /// <summary>
    /// Stores the number of max dense cell items processed by the operation.
    /// </summary>
    public int MaxDenseCellCount;
    /// <summary>
    /// Stores the dense fill ratio threshold value used by this model.
    /// </summary>
    public double DenseFillRatioThreshold;
    /// <summary>
    /// Stores the streaming cell count threshold value used by this model.
    /// </summary>
    public int StreamingCellCountThreshold;
    /// <summary>
    /// Initializes a new worksheet storage options with its default state.
    /// </summary>
    public XlsxWorksheetStorageOptions()
    {
        ForceSparse = false;
        ForceDense = false;
        ForceStreaming = false;
        MaxDenseCellCount = 1000000;
        DenseFillRatioThreshold = 0.5d;
        StreamingCellCountThreshold = 1000000;
    }
}
