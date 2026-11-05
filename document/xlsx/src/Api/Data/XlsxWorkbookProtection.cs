using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook protection data used by the public workbook API.
/// </summary>
public class XlsxWorkbookProtection
{
    /// <summary>
    /// Stores the workbook password value used by this model.
    /// </summary>
    public string? WorkbookPassword;
    /// <summary>
    /// Stores the workbook algorithm name value used by this model.
    /// </summary>
    public string? WorkbookAlgorithmName;
    /// <summary>
    /// Stores the workbook hash value value used by this model.
    /// </summary>
    public string? WorkbookHashValue;
    /// <summary>
    /// Stores the workbook salt value value used by this model.
    /// </summary>
    public string? WorkbookSaltValue;
    /// <summary>
    /// Stores the number of workbook spin items processed by the operation.
    /// </summary>
    public uint WorkbookSpinCount;
    /// <summary>
    /// Indicates whether workbook spin count is present and should be written.
    /// </summary>
    public bool HasWorkbookSpinCount;
    /// <summary>
    /// Stores the revisions password value used by this model.
    /// </summary>
    public string? RevisionsPassword;
    /// <summary>
    /// Stores the revisions algorithm name value used by this model.
    /// </summary>
    public string? RevisionsAlgorithmName;
    /// <summary>
    /// Stores the revisions hash value value used by this model.
    /// </summary>
    public string? RevisionsHashValue;
    /// <summary>
    /// Stores the revisions salt value value used by this model.
    /// </summary>
    public string? RevisionsSaltValue;
    /// <summary>
    /// Stores the number of revisions spin items processed by the operation.
    /// </summary>
    public uint RevisionsSpinCount;
    /// <summary>
    /// Indicates whether revisions spin count is present and should be written.
    /// </summary>
    public bool HasRevisionsSpinCount;
    /// <summary>
    /// Stores the lock structure value used by this model.
    /// </summary>
    public bool LockStructure;
    /// <summary>
    /// Indicates whether lock structure is present and should be written.
    /// </summary>
    public bool HasLockStructure;
    /// <summary>
    /// Contains lock windows.
    /// </summary>
    public bool LockWindows;
    /// <summary>
    /// Indicates whether lock windows is present and should be written.
    /// </summary>
    public bool HasLockWindows;
    /// <summary>
    /// Stores the lock revision value used by this model.
    /// </summary>
    public bool LockRevision;
    /// <summary>
    /// Indicates whether lock revision is present and should be written.
    /// </summary>
    public bool HasLockRevision;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook protection with its default state.
    /// </summary>
    public XlsxWorkbookProtection()
    {
        WorkbookPassword = "";
        WorkbookAlgorithmName = "";
        WorkbookHashValue = "";
        WorkbookSaltValue = "";
        WorkbookSpinCount = 0;
        HasWorkbookSpinCount = false;
        RevisionsPassword = "";
        RevisionsAlgorithmName = "";
        RevisionsHashValue = "";
        RevisionsSaltValue = "";
        RevisionsSpinCount = 0;
        HasRevisionsSpinCount = false;
        LockStructure = false;
        HasLockStructure = false;
        LockWindows = false;
        HasLockWindows = false;
        LockRevision = false;
        HasLockRevision = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
