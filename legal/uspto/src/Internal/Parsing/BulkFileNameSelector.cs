namespace uspto;

internal static class BulkFileNameSelector
{
    internal static string SelectBulkFileName(string? catalogFileName, global::System.Net.Http.HttpResponseMessage response)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required for bulk filename selection.", "response", "REQUIRED", null);
        }

        string? normalizedCatalog = null;
        if (!global::System.String.IsNullOrWhiteSpace(catalogFileName))
        {
            normalizedCatalog = NormalizeBulkFileName(catalogFileName, "catalogFileName");
        }

        string? dispositionFileName = ReadDispositionFileName(response);
        string? normalizedDisposition = null;
        if (!global::System.String.IsNullOrWhiteSpace(dispositionFileName))
        {
            normalizedDisposition = NormalizeBulkFileName(dispositionFileName, UsptoHeaders.ContentDisposition);
        }

        if (normalizedCatalog != null)
        {
            return normalizedCatalog;
        }

        if (normalizedDisposition != null)
        {
            return normalizedDisposition;
        }

        throw new ValidationException("A bulk download filename was not supplied by the catalog or response.", "FileName", "BULK_FILENAME_REQUIRED", null);
    }

    internal static string? ReadDispositionFileName(global::System.Net.Http.HttpResponseMessage response)
    {
        global::System.Net.Http.Headers.ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;
        if (disposition == null)
        {
            return null;
        }

        string? fileName = disposition.FileNameStar;
        if (global::System.String.IsNullOrWhiteSpace(fileName))
        {
            fileName = disposition.FileName;
        }

        if (fileName == null)
        {
            return null;
        }

        fileName = fileName.Trim();
        if (fileName.Length >= 2 && fileName[0] == '"' && fileName[fileName.Length - 1] == '"')
        {
            fileName = fileName.Substring(1, fileName.Length - 2);
        }

        return fileName;
    }

    internal static string NormalizeBulkFileName(string fileName, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("The bulk download filename cannot be blank.", fieldName, "BULK_FILENAME_INVALID", null);
        }

        string trimmed = fileName.Trim();
        if (HasBulkPathTraversal(trimmed))
        {
            throw new ValidationException("The bulk download filename cannot contain path information.", fieldName, "BULK_FILENAME_TRAVERSAL", null);
        }

        int index;
        for (index = 0; index < trimmed.Length; index += 1)
        {
            if (global::System.Char.IsControl(trimmed[index]) || global::System.Char.GetUnicodeCategory(trimmed[index]) == global::System.Globalization.UnicodeCategory.Format)
            {
                throw new ValidationException("The bulk download filename cannot contain control or direction-format characters.", fieldName, "BULK_FILENAME_CONTROL_CHARACTER", null);
            }
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(trimmed.Length);
        for (index = 0; index < trimmed.Length; index += 1)
        {
            char value = trimmed[index];
            if (value == '<' || value == '>' || value == '"' || value == '|' || value == '?' || value == '*')
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(value);
            }
        }

        string result = builder.ToString().TrimEnd(' ', '.');
        if (result.Length == 0 || result == "." || result == ".." || result == "~")
        {
            throw new ValidationException("The bulk download filename has special filesystem meaning.", fieldName, "BULK_FILENAME_SPECIAL", null);
        }

        if (IsBulkDeviceName(result))
        {
            throw new ValidationException("The bulk download filename is reserved by the filesystem.", fieldName, "BULK_FILENAME_DEVICE", null);
        }

        return result;
    }

    internal static bool HasBulkPathTraversal(string fileName)
    {
        if (fileName == "." || fileName == ".." || fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0 || fileName.IndexOf(':') >= 0)
        {
            return true;
        }

        string decoded = global::System.Uri.UnescapeDataString(fileName);
        return decoded == "." || decoded == ".." || decoded.IndexOf('/') >= 0 || decoded.IndexOf('\\') >= 0 || decoded.IndexOf(':') >= 0;
    }

    internal static bool IsBulkDeviceName(string fileName)
    {
        string comparison = fileName.TrimEnd(' ', '.');
        int dotIndex = comparison.IndexOf('.');
        if (dotIndex >= 0)
        {
            comparison = comparison.Substring(0, dotIndex).TrimEnd(' ', '.');
        }

        comparison = comparison.ToUpperInvariant();
        if (comparison == "CON" || comparison == "PRN" || comparison == "AUX" || comparison == "NUL" || comparison == "CLOCK$" || comparison == "CONIN$" || comparison == "CONOUT$")
        {
            return true;
        }

        if (comparison.Length == 4)
        {
            string prefix = comparison.Substring(0, 3);
            char suffix = comparison[3];
            bool reservedDigit = suffix >= '1' && suffix <= '9' || suffix == '¹' || suffix == '²' || suffix == '³';
            if ((prefix == "COM" || prefix == "LPT") && reservedDigit)
            {
                return true;
            }
        }

        return false;
    }
}
