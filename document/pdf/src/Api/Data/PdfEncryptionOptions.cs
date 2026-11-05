using System;

namespace pdf;

/// <summary>
/// Contains the passwords and permission flags used to encrypt a PDF document.
/// </summary>
/// <remarks>
/// PDF permission flags are viewer-enforced restrictions and do not replace control over access to the generated file.
/// </remarks>
public class PdfEncryptionOptions
{
    /// <summary>
    /// Stores the user password used by this object.
    /// </summary>
    public string UserPassword;
    /// <summary>
    /// Stores the owner password used by this object.
    /// </summary>
    public string OwnerPassword;
    /// <summary>
    /// Stores the permissions used by this object.
    /// </summary>
    public PdfPermissions Permissions;

    /// <summary>
    /// Initializes a new set of standard PDF encryption options.
    /// </summary>
    /// <param name="userPassword">The user password to use for this operation.</param>
    /// <param name="ownerPassword">The owner password to use for this operation.</param>
    /// <param name="permissions">The permissions to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfEncryptionOptions(string userPassword, string ownerPassword, PdfPermissions permissions)
    {
        if (userPassword == null)
        {
            throw new ArgumentNullException("userPassword");
        }

        if (ownerPassword == null)
        {
            throw new ArgumentNullException("ownerPassword");
        }

        if (permissions == null)
        {
            throw new ArgumentNullException("permissions");
        }

        ValidatePassword(userPassword, "userPassword");
        ValidatePassword(ownerPassword, "ownerPassword");

        UserPassword = userPassword;
        OwnerPassword = ownerPassword;
        Permissions = permissions;
    }

    internal void Validate()
    {
        if (UserPassword == null)
        {
            throw new ArgumentNullException("UserPassword");
        }

        if (OwnerPassword == null)
        {
            throw new ArgumentNullException("OwnerPassword");
        }

        if (Permissions == null)
        {
            throw new ArgumentNullException("Permissions");
        }

        ValidatePassword(UserPassword, "UserPassword");
        ValidatePassword(OwnerPassword, "OwnerPassword");
    }

    internal static void ValidatePassword(string password, string name)
    {
        if (password.Length > 32)
        {
            throw new ArgumentException("Standard encryption passwords cannot exceed 32 ASCII characters.", name);
        }

        foreach (char character in password)
        {
            if (character > 127)
            {
                throw new ArgumentException("Standard encryption passwords must contain only ASCII characters.", name);
            }
        }
    }
}
