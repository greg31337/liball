namespace molecule;


/// <summary>
/// Parses inchi text into the library's object model.
/// </summary>
public class InChIParser : IInChIParser
{
    /// <summary>
    /// Initializes an InChI layer parser.
    /// </summary>
    public InChIParser()
    {
    }

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting in ch irecord.</returns>
    public InChIRecord Parse(string? text)
    {
        InChIRecord record = new InChIRecord();
        record.OriginalText = NormalizeString(text).Trim();

        if (record.OriginalText.Length == 0)
        {
            AddError(record, "INCHI_EMPTY", "InChI text is empty.", 1, 1);
            return record;
        }

        if (!record.OriginalText.StartsWith("InChI="))
        {
            AddError(record, "INCHI_PREFIX_MISSING", "InChI text must start with InChI=.", 1, 1);
            return record;
        }

        record.Prefix = "InChI";
        string body = record.OriginalText.Substring(6);
        string[] tokens = body.Split('/');

        if (tokens.Length == 0 || tokens[0].Length == 0)
        {
            AddError(record, "INCHI_VERSION_MISSING", "InChI version is missing.", 1, 7);
            return record;
        }

        ParseVersion(record, tokens[0]);

        if (tokens.Length < 2 || tokens[1].Length == 0)
        {
            AddError(record, "INCHI_FORMULA_MISSING", "InChI formula layer is missing.", 1, 7 + tokens[0].Length);
            return record;
        }

        record.FormulaLayer = tokens[1];
        ParseLayers(record, tokens);

        if (record.Report.GetErrorCount() > 0)
        {
            record.Success = false;
        }

        return record;
    }

    internal void ParseVersion(InChIRecord record, string token)
    {
        if (token.EndsWith("S"))
        {
            record.IsStandard = true;
            record.Version = token.Substring(0, token.Length - 1);
        }
        else
        {
            record.IsStandard = false;
            record.Version = token;
        }

        if (record.Version.Length == 0)
        {
            AddError(record, "INCHI_VERSION_MISSING", "InChI version is missing.", 1, 7);
        }
    }

    internal void ParseLayers(InChIRecord record, string[] tokens)
    {
        bool inIsotopeLayer = false;
        bool inFixedHLayer = false;
        int index = 2;

        while (index < tokens.Length)
        {
            string token = NormalizeString(tokens[index]);
            if (token.Length == 0)
            {
                AddWarning(record, "INCHI_EMPTY_LAYER", "InChI contains an empty layer.", 1, 1);
            }
            else
            {
                char label = token[0];
                string value = token.Substring(1);

                if (label == 'f')
                {
                    inFixedHLayer = true;
                    inIsotopeLayer = false;
                    record.FixedHLayer = AppendPlainLayer(record.FixedHLayer, value);
                }
                else if (inFixedHLayer)
                {
                    record.FixedHLayer = AppendLabeledLayer(record.FixedHLayer, token);
                }
                else if (label == 'i')
                {
                    inIsotopeLayer = true;
                    record.IsotopeLayer = AppendPlainLayer(record.IsotopeLayer, value);
                }
                else if (inIsotopeLayer && IsIsotopeSublayer(label))
                {
                    record.IsotopeLayer = AppendLabeledLayer(record.IsotopeLayer, token);
                }
                else
                {
                    inIsotopeLayer = false;
                    ParseMainLayer(record, label, value, token);
                }
            }

            index = index + 1;
        }
    }

    internal void ParseMainLayer(InChIRecord record, char label, string value, string token)
    {
        if (label == 'c')
        {
            record.ConnectivityLayer = AppendPlainLayer(record.ConnectivityLayer, value);
        }
        else if (label == 'h')
        {
            record.HydrogenLayer = AppendPlainLayer(record.HydrogenLayer, value);
            string tautomer = ExtractTautomerLayer(value);
            if (tautomer.Length > 0)
            {
                record.TautomerLayer = AppendPlainLayer(record.TautomerLayer, tautomer);
            }
        }
        else if (label == 'q')
        {
            record.ChargeLayer = AppendPlainLayer(record.ChargeLayer, value);
        }
        else if (label == 'p')
        {
            record.ProtonLayer = AppendPlainLayer(record.ProtonLayer, value);
        }
        else if (IsStereoLayer(label))
        {
            record.StereoLayer = AppendLabeledLayer(record.StereoLayer, token);
        }
        else
        {
            record.OtherLayers.Add(new MoleculeMetadataEntry(label.ToString(), value));
            AddWarning(record, "INCHI_LAYER_UNRECOGNIZED", "InChI contains an unrecognized layer.", 1, 1);
        }
    }

    internal string ExtractTautomerLayer(string hydrogenLayer)
    {
        string text = NormalizeString(hydrogenLayer);
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '(')
            {
                return text.Substring(index);
            }

            index = index + 1;
        }

        return "";
    }

    internal bool IsStereoLayer(char label)
    {
        if (label == 'b')
        {
            return true;
        }

        if (label == 't')
        {
            return true;
        }

        if (label == 'm')
        {
            return true;
        }

        if (label == 's')
        {
            return true;
        }

        return false;
    }

    internal bool IsIsotopeSublayer(char label)
    {
        if (label == 'h')
        {
            return true;
        }

        if (IsStereoLayer(label))
        {
            return true;
        }

        return false;
    }

    internal string AppendPlainLayer(string existing, string value)
    {
        string safeExisting = NormalizeString(existing);
        string safeValue = NormalizeString(value);
        if (safeExisting.Length == 0)
        {
            return safeValue;
        }

        return safeExisting + ";" + safeValue;
    }

    internal string AppendLabeledLayer(string existing, string token)
    {
        string safeExisting = NormalizeString(existing);
        string safeToken = NormalizeString(token);
        if (safeExisting.Length == 0)
        {
            return "/" + safeToken;
        }

        return safeExisting + "/" + safeToken;
    }

    internal void AddError(InChIRecord record, string code, string message, int lineNumber, int columnNumber)
    {
        record.Success = false;
        MoleculeError error = new MoleculeError(code, message, new DiagnosticLocation("inchi", lineNumber, columnNumber, -1, record.OriginalText), -1, -1, ErrorSeverity.Error);
        record.Report.Add(error);
    }

    internal void AddWarning(InChIRecord record, string code, string message, int lineNumber, int columnNumber)
    {
        MoleculeError error = new MoleculeError(code, message, new DiagnosticLocation("inchi", lineNumber, columnNumber, -1, record.OriginalText), -1, -1, ErrorSeverity.Warning);
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

