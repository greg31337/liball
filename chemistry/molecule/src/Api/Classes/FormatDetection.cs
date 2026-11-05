namespace molecule;


/// <summary>
/// Provides format detection data and operations for molecular graphs.
/// </summary>
public class FormatDetection : IFormatDetection
{
    /// <summary>
    /// Initializes a format detector that uses caller hints, file names, and content heuristics.
    /// </summary>
    public FormatDetection()
    {
    }

    /// <summary>
    /// Detects a supported molecule format from a caller hint, file name, or characteristic content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public FormatDetectionResult Detect(string? text, string? fileName)
    {
        return DetectWithHint(text, fileName, FormatKind.Unknown);
    }

    /// <summary>
    /// Detects a supported molecule format using the supplied hint.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="hint">The hint.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public FormatDetectionResult DetectWithHint(string? text, string? fileName, FormatKind hint)
    {
        FormatOptions options = new FormatOptions();
        options.KindHint = hint;
        options.FileName = NormalizeString(fileName);
        return DetectWithOptions(text, options);
    }

    internal FormatDetectionResult DetectWithOptions(string? text, FormatOptions options)
    {
        FormatDetectionResult result = new FormatDetectionResult();
        FormatOptions safeOptions = NormalizeOptions(options);
        string value = NormalizeString(text);

        if (safeOptions.KindHint != FormatKind.Unknown)
        {
            result.Kind = safeOptions.KindHint;
            result.Confidence = 100;
            result.Reason = "caller hint";
            return result;
        }

        result = DetectByMagicText(value);
        if (result.Kind != FormatKind.Unknown)
        {
            return result;
        }

        result = DetectByExtension(safeOptions.FileName);
        if (result.Kind != FormatKind.Unknown)
        {
            return result;
        }

        return DetectByContentHeuristic(value);
    }

    internal FormatDetectionResult DetectByMagicText(string text)
    {
        FormatDetectionResult result = new FormatDetectionResult();
        string trimmed = NormalizeString(text).Trim();

        if (trimmed.StartsWith("InChI="))
        {
            return CreateResult(FormatKind.InChI, 95, "InChI prefix");
        }

        if (IsLikelyInChIKey(trimmed))
        {
            return CreateResult(FormatKind.InChIKey, 90, "InChIKey pattern");
        }

        if (trimmed.StartsWith("MOLDET1"))
        {
            return CreateResult(FormatKind.DeterministicText, 95, "deterministic text header");
        }

        if (text.Contains("@<TRIPOS>MOLECULE"))
        {
            return CreateResult(FormatKind.Mol2, 95, "MOL2 TRIPOS header");
        }

        if (text.Contains("M  V30") || text.Contains("V3000"))
        {
            return CreateResult(FormatKind.MolV3000, 95, "MOL V3000 marker");
        }

        if (text.Contains("$$$$"))
        {
            return CreateResult(FormatKind.Sdf, 95, "SDF record separator");
        }

        if (text.Contains("V2000") || text.Contains("M  END"))
        {
            return CreateResult(FormatKind.MolV2000, 85, "MOL V2000 marker");
        }

        if (StartsWithPdbRecord(trimmed))
        {
            return CreateResult(FormatKind.Pdb, 90, "PDB record header");
        }

        return result;
    }

    internal FormatDetectionResult DetectByExtension(string fileName)
    {
        string extension = GetExtension(fileName);
        if (extension == ".sdf" || extension == ".sd")
        {
            return CreateResult(FormatKind.Sdf, 80, "file extension");
        }

        if (extension == ".mol")
        {
            return CreateResult(FormatKind.MolV2000, 75, "file extension");
        }

        if (extension == ".mol2")
        {
            return CreateResult(FormatKind.Mol2, 80, "file extension");
        }

        if (extension == ".pdb" || extension == ".ent")
        {
            return CreateResult(FormatKind.Pdb, 80, "file extension");
        }

        if (extension == ".smi" || extension == ".smiles")
        {
            return CreateResult(FormatKind.Smiles, 75, "file extension");
        }

        if (extension == ".sma" || extension == ".smarts")
        {
            return CreateResult(FormatKind.Smarts, 75, "file extension");
        }

        if (extension == ".inchi")
        {
            return CreateResult(FormatKind.InChI, 80, "file extension");
        }

        if (extension == ".inchikey")
        {
            return CreateResult(FormatKind.InChIKey, 80, "file extension");
        }

        if (extension == ".csv" || extension == ".tsv")
        {
            return CreateResult(FormatKind.VendorDelimited, 70, "file extension");
        }

        return new FormatDetectionResult();
    }

    internal FormatDetectionResult DetectByContentHeuristic(string text)
    {
        string trimmed = NormalizeString(text).Trim();
        if (trimmed == "")
        {
            return new FormatDetectionResult();
        }

        if (LooksDelimited(trimmed))
        {
            return CreateResult(FormatKind.VendorDelimited, 55, "delimited text heuristic");
        }

        if (LooksLikeSmarts(trimmed))
        {
            return CreateResult(FormatKind.Smarts, 60, "SMARTS heuristic");
        }

        if (LooksLikeSingleLineText(trimmed))
        {
            return CreateResult(FormatKind.Smiles, 50, "single-line molecule text heuristic");
        }

        return new FormatDetectionResult();
    }

    internal bool StartsWithPdbRecord(string text)
    {
        if (text.StartsWith("HEADER") || text.StartsWith("TITLE") || text.StartsWith("ATOM") || text.StartsWith("HETATM") || text.StartsWith("MODEL") || text.StartsWith("CONECT"))
        {
            return true;
        }

        return false;
    }

    internal bool LooksDelimited(string text)
    {
        if (!text.Contains("\n"))
        {
            return false;
        }

        string firstLine = GetFirstLine(text);
        if (firstLine.Contains("\t") || firstLine.Contains(","))
        {
            return true;
        }

        return false;
    }

    internal bool LooksLikeSmarts(string text)
    {
        if (text.Contains("[#") || text.Contains("~") || text.Contains(";") || text.Contains(",") || text.Contains("!") || text.Contains("$("))
        {
            return true;
        }

        return false;
    }

    internal bool LooksLikeSingleLineText(string text)
    {
        if (text.Contains("\n") || text.Contains("\r"))
        {
            return false;
        }

        return true;
    }

    internal bool IsLikelyInChIKey(string text)
    {
        if (text.Length != 27)
        {
            return false;
        }

        if (text[14] != '-' || text[25] != '-')
        {
            return false;
        }

        if (!ContainsOnlyInChIKeyLetters(text, 0, 14))
        {
            return false;
        }

        if (!ContainsOnlyInChIKeyLetters(text, 15, 25))
        {
            return false;
        }

        if (!ContainsOnlyInChIKeyLetters(text, 26, 27))
        {
            return false;
        }

        return true;
    }

    internal bool ContainsOnlyInChIKeyLetters(string text, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (text[index] < 'A' || text[index] > 'Z')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string GetFirstLine(string text)
    {
        string value = NormalizeString(text);
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\n' || value[index] == '\r')
            {
                return value.Substring(0, index);
            }

            index = index + 1;
        }

        return value;
    }

    internal string GetExtension(string fileName)
    {
        string value = NormalizeString(fileName).Trim();
        int index = value.Length - 1;
        while (index >= 0)
        {
            if (value[index] == '.')
            {
                return ToLowerAscii(value.Substring(index));
            }

            if (value[index] == '/' || value[index] == '\\')
            {
                return "";
            }

            index = index - 1;
        }

        return "";
    }

    internal string ToLowerAscii(string text)
    {
        string value = NormalizeString(text);
        string result = "";
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 'A' && character <= 'Z')
            {
                result = result + (char)(character + 32);
            }
            else
            {
                result = result + character;
            }

            index = index + 1;
        }

        return result;
    }

    internal FormatOptions NormalizeOptions(FormatOptions? options)
    {
        if (options == null)
        {
            return new FormatOptions();
        }

        return options;
    }

    internal FormatDetectionResult CreateResult(FormatKind kind, int confidence, string reason)
    {
        FormatDetectionResult result = new FormatDetectionResult();
        result.Kind = kind;
        result.Confidence = confidence;
        result.Reason = NormalizeString(reason);
        return result;
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

