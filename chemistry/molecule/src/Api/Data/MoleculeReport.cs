using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides molecule report data and operations for molecular graphs.
/// </summary>
public class MoleculeReport
{
    /// <summary>
    /// The errors.
    /// </summary>
    public List<MoleculeError> Errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeReport"/> class.
    /// </summary>
    public MoleculeReport()
    {
        Errors = new List<MoleculeError>();
    }

    /// <summary>
    /// Adds the supplied value to this collection.
    /// </summary>
    /// <param name="error">The error.</param>
    internal void Add(MoleculeError? error)
    {
        if (error == null)
        {
            return;
        }

        Errors.Add(error);
    }

    /// <summary>
    /// Adds the error.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The resulting molecule error.</returns>
    internal MoleculeError AddError(string code, string message)
    {
        MoleculeError error = MoleculeError.Create(code, message, ErrorSeverity.Error);
        Errors.Add(error);
        return error;
    }

    /// <summary>
    /// Adds the warning.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The resulting molecule error.</returns>
    internal MoleculeError AddWarning(string code, string message)
    {
        MoleculeError error = MoleculeError.Create(code, message, ErrorSeverity.Warning);
        Errors.Add(error);
        return error;
    }

    /// <summary>
    /// Adds the info.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The resulting molecule error.</returns>
    internal MoleculeError AddInfo(string code, string message)
    {
        MoleculeError error = MoleculeError.Create(code, message, ErrorSeverity.Info);
        Errors.Add(error);
        return error;
    }

    /// <summary>
    /// Get.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The resulting molecule error.</returns>
    internal MoleculeError Get(int index)
    {
        return Errors[index];
    }

    /// <summary>
    /// Returns the count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetCount()
    {
        return Errors.Count;
    }

    /// <summary>
    /// Returns the error count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetErrorCount()
    {
        int count = 0;
        int index = 0;
        while (index < Errors.Count)
        {
            if (Errors[index].IsError())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Returns the warning count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetWarningCount()
    {
        int count = 0;
        int index = 0;
        while (index < Errors.Count)
        {
            if (Errors[index].IsWarning())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Determines whether the value has errors.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return GetErrorCount() > 0;
    }

    /// <summary>
    /// Determines whether the value has warnings.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasWarnings()
    {
        return GetWarningCount() > 0;
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        Errors.Clear();
    }
}

