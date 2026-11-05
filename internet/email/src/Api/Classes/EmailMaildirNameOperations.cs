using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMaildirName"/>.
/// </summary>
public class EmailMaildirNameOperations : IEmailMaildirNameOperationsContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMaildirName Parse(string fileName, EmailMaildirFolder folder)
    {
        return EmailMaildirName.ParseImplementation(fileName, folder);
    }

    /// <summary>
    /// Determines whether the current value or state is safe unique name.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is safe unique name; otherwise, <c>false</c>.</returns>
    public bool IsSafeUniqueName(string value)
    {
        return EmailMaildirName.IsSafeUniqueNameImplementation(value);
    }

    /// <summary>
    /// Determines whether the current value or state is safe file name.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <returns><c>true</c> when the value or state is safe file name; otherwise, <c>false</c>.</returns>
    public bool IsSafeFileName(string? value, EmailMaildirFolder folder)
    {
        return EmailMaildirName.IsSafeFileNameImplementation(value, folder);
    }

    /// <summary>
    /// Creates an unique name configured for the requested behavior.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    public string CreateUniqueNameWith()
    {
        return EmailMaildirName.CreateUniqueNameImplementationWith();
    }

    /// <summary>
    /// Creates an unique name configured for the requested behavior.
    /// </summary>
    /// <param name = "hostName">The host name used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string CreateUniqueNameWithHostName(string hostName)
    {
        return EmailMaildirName.CreateUniqueNameImplementationWithHostName(hostName);
    }

    /// <summary>
    /// Creates a new file name configured for the requested behavior.
    /// </summary>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateNewFileName(string uniqueName)
    {
        return EmailMaildirName.CreateNewFileNameImplementation(uniqueName);
    }

    /// <summary>
    /// Creates a current file name configured for the requested behavior.
    /// </summary>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateCurrentFileName(string uniqueName, EmailMaildirFlags flags)
    {
        return EmailMaildirName.CreateCurrentFileNameImplementation(uniqueName, flags);
    }

    /// <summary>
    /// Creates a canonical current filename while retaining supported unknown version 2 flag characters.
    /// </summary>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <param name = "flags">The standard flags used by the operation.</param>
    /// <param name = "unknownFlagText">The supported unknown flag characters to preserve.</param>
    /// <returns>The canonical current filename.</returns>
    public string CreateCurrentFileNamePreservingUnknownFlags(string uniqueName, EmailMaildirFlags flags, string unknownFlagText)
    {
        return EmailMaildirName.CreateCurrentFileNamePreservingUnknownFlagsImplementation(uniqueName, flags, unknownFlagText);
    }

    /// <summary>
    /// Validates the supplied unique name against the selected policy.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void ValidateUniqueName(string value, string parameterName)
    {
        EmailMaildirName.ValidateUniqueNameImplementation(value, parameterName);
    }
}
