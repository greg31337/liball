using System;

namespace xlsx;

/// <summary>Defines workbook resource validation and active-content policy application.</summary>
public interface IXlsxWorkbookSecurityService
{
    /// <summary>Validates resource counts and applies configured preserve, remove, or reject policies.</summary>
    /// <param name="workbook">The workbook to evaluate.</param>
    /// <param name="options">The security limits and content policies.</param>
    /// <returns>Detected counts, removals, and diagnostics.</returns>
    XlsxSecurityPolicyResult Apply(XlsxWorkbook workbook, XlsxSecurityOptions? options);
}
