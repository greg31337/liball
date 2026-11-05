namespace molecule;


/// <summary>
/// Parses inchikey text into the library's object model.
/// </summary>
public class InChIKeyParser : IInChIKeyParser
{
    /// <summary>
    /// Initializes an InChIKey syntax parser and validator.
    /// </summary>
    public InChIKeyParser()
    {
    }

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting in ch ikey record.</returns>
    public InChIKeyRecord Parse(string? text)
    {
        InChIKeyRecord record = new InChIKeyRecord();
        record.OriginalText = NormalizeString(text);
        record.NormalizedText = record.OriginalText.Trim();

        ValidateShape(record);
        if (record.Report.GetErrorCount() > 0)
        {
            record.Success = false;
            return record;
        }

        record.FirstBlock = record.NormalizedText.Substring(0, 14);
        record.SecondBlock = record.NormalizedText.Substring(15, 10);
        record.RemainingHashBlock = record.SecondBlock.Substring(0, 8);
        record.VersionBlock = record.SecondBlock.Substring(8, 2);
        record.VersionCharacter = record.VersionBlock.Substring(0, 1);
        record.StandardCharacter = record.VersionBlock.Substring(1, 1);
        record.ProtonationIndicator = record.NormalizedText.Substring(26, 1);
        record.Success = true;
        return record;
    }

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool Validate(string? text)
    {
        InChIKeyRecord record = Parse(text);
        return record.Success;
    }

    internal void ValidateShape(InChIKeyRecord record)
    {
        if (record.NormalizedText.Length == 0)
        {
            AddError(record, "INCHIKEY_EMPTY", "InChIKey text is empty.", 1);
            return;
        }

        if (record.NormalizedText.Length != 27)
        {
            AddError(record, "INCHIKEY_LENGTH", "InChIKey must contain 27 characters.", 1);
            return;
        }

        if (record.NormalizedText[14] != '-' || record.NormalizedText[25] != '-')
        {
            AddError(record, "INCHIKEY_SEPARATOR", "InChIKey separators must appear after the first and second blocks.", 1);
        }

        ValidateLetters(record, 0, 14);
        ValidateLetters(record, 15, 25);
        ValidateLetters(record, 26, 27);
    }

    internal void ValidateLetters(InChIKeyRecord record, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!IsUppercaseLetter(record.NormalizedText[index]))
            {
                AddError(record, "INCHIKEY_CHARACTER", "InChIKey blocks can contain only uppercase A-Z letters.", index + 1);
                return;
            }

            index = index + 1;
        }
    }

    internal bool IsUppercaseLetter(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return true;
        }

        return false;
    }

    internal void AddError(InChIKeyRecord record, string code, string message, int columnNumber)
    {
        record.Success = false;
        MoleculeError error = new MoleculeError(code, message, new DiagnosticLocation("inchikey", 1, columnNumber, columnNumber - 1, record.NormalizedText), -1, -1, ErrorSeverity.Error);
        record.Report.Add(error);
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

