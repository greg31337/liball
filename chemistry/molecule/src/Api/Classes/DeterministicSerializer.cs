using System.Text;

namespace molecule;


/// <summary>
/// Serializes molecular graphs to a stable library-specific text representation and reads them back.
/// </summary>
public class DeterministicSerializer : IMoleculeReader, IMoleculeWriter, IDeterministicSerializer
{
    internal string Version;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicSerializer"/> class.
    /// </summary>
    public DeterministicSerializer()
    {
        Version = "MOLDET1";
    }

    /// <summary>
    /// Writes the molecule using deterministic text serialization.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The deterministic text.</returns>
    public string Write(Molecule? molecule)
    {
        return Serialize(molecule);
    }

    /// <summary>
    /// Reads a molecule from deterministic text serialization.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule.</returns>
    public Molecule Read(string? text)
    {
        return Deserialize(text);
    }

    /// <summary>
    /// Writes the complete molecular graph in stable canonical order using the library-specific text format.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Serialize(Molecule? molecule)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Version);
        builder.Append("\n");

        if (molecule == null)
        {
            builder.Append("ATOMS\t0\n");
            builder.Append("BONDS\t0\n");
            builder.Append("END\n");
            return builder.ToString();
        }

        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);
        builder.Append("ATOMS\t");
        builder.Append(order.AtomIdsByCanonicalIndex.Count);
        builder.Append("\n");
        builder.Append("BONDS\t");
        builder.Append(order.BondIdsByCanonicalIndex.Count);
        builder.Append("\n");

        int atomIndex = 0;
        while (atomIndex < order.AtomIdsByCanonicalIndex.Count)
        {
            AppendAtomLine(builder, molecule, order.GetOriginalAtomId(atomIndex));
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < order.BondIdsByCanonicalIndex.Count)
        {
            AppendBondLine(builder, molecule, order, bondIndex);
            bondIndex = bondIndex + 1;
        }

        builder.Append("END\n");
        return builder.ToString();
    }

    /// <summary>
    /// Reads the library-specific deterministic text representation into a molecule.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Deserialize(string? text)
    {
        DeterministicParseResult result = Parse(text);
        return result.Molecule;
    }

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public DeterministicParseResult Parse(string? text)
    {
        DeterministicParseResult result = new DeterministicParseResult();
        result.Success = true;

        string normalized = NormalizeLineEndings(text);
        string[] lines = normalized.Split('\n');
        if (lines.Length < 4)
        {
            Fail(result, "Input does not contain a complete deterministic molecule record.", 1);
            return result;
        }

        if (lines[0] != Version)
        {
            Fail(result, "Input does not start with the deterministic molecule version header.", 1);
            return result;
        }

        int atomCount = ParseHeaderCount(lines[1], "ATOMS", result, 2);
        int bondCount = ParseHeaderCount(lines[2], "BONDS", result, 3);
        if (!result.Success)
        {
            return result;
        }

        MoleculeBuilder builder = new MoleculeBuilder();
        int lineIndex = 3;
        int atomIndex = 0;
        while (atomIndex < atomCount)
        {
            if (lineIndex >= lines.Length)
            {
                Fail(result, "Input ended before all atom records were read.", lineIndex + 1);
                return result;
            }

            ParseAtomLine(lines[lineIndex], builder, result, lineIndex + 1);
            if (!result.Success)
            {
                return result;
            }

            atomIndex = atomIndex + 1;
            lineIndex = lineIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < bondCount)
        {
            if (lineIndex >= lines.Length)
            {
                Fail(result, "Input ended before all bond records were read.", lineIndex + 1);
                return result;
            }

            ParseBondLine(lines[lineIndex], builder, result, lineIndex + 1);
            if (!result.Success)
            {
                return result;
            }

            bondIndex = bondIndex + 1;
            lineIndex = lineIndex + 1;
        }

        if (lineIndex >= lines.Length || lines[lineIndex] != "END")
        {
            Fail(result, "Input does not end with an END record.", lineIndex + 1);
            return result;
        }

        result.Molecule = builder.Build();
        return result;
    }

    internal void AppendAtomLine(StringBuilder builder, Molecule molecule, int atomId)
    {
        Atom atom = molecule.Atoms[atomId];
        builder.Append("ATOM");
        AppendIntField(builder, atom.AtomicNumber);
        AppendIntField(builder, atom.IsotopeMassNumber);
        AppendIntField(builder, atom.FormalCharge);
        AppendIntField(builder, atom.ExplicitHydrogenCount);
        AppendIntField(builder, atom.ImplicitHydrogenCount);
        AppendIntField(builder, atom.RadicalElectronCount);
        AppendIntField(builder, atom.AtomMapNumber);
        AppendIntField(builder, BoolToInt(atom.IsAromatic));
        AppendIntField(builder, (int)atom.QueryOperation);
        AppendStringField(builder, atom.QueryValue);
        AppendStringField(builder, atom.Alias);
        AppendStringField(builder, atom.Label);
        builder.Append("\n");
    }

    internal void AppendBondLine(StringBuilder builder, Molecule molecule, CanonicalOrder order, int canonicalBondIndex)
    {
        int bondId = order.GetOriginalBondId(canonicalBondIndex);
        Bond bond = molecule.Bonds[bondId];
        builder.Append("BOND");
        AppendIntField(builder, order.GetCanonicalBondBeginAtomIndex(canonicalBondIndex));
        AppendIntField(builder, order.GetCanonicalBondEndAtomIndex(canonicalBondIndex));
        AppendIntField(builder, GetDirectionalBeginAtomIndex(order, bond));
        AppendIntField(builder, GetDirectionalEndAtomIndex(order, bond));
        AppendIntField(builder, (int)bond.Order);
        AppendIntField(builder, BoolToInt(bond.IsAromatic));
        AppendIntField(builder, (int)bond.Stereo);
        AppendIntField(builder, (int)bond.Direction);
        AppendIntField(builder, BoolToInt(bond.IsDative));
        AppendIntField(builder, (int)bond.QueryOperation);
        AppendStringField(builder, bond.QueryValue);
        AppendStringField(builder, bond.SourceFlags);
        builder.Append("\n");
    }

    internal void ParseAtomLine(string line, MoleculeBuilder builder, DeterministicParseResult result, int lineNumber)
    {
        string[] parts = line.Split('\t');
        if (parts.Length != 13 || parts[0] != "ATOM")
        {
            Fail(result, "Invalid atom record.", lineNumber);
            return;
        }

        AtomSpec spec = AtomSpec.CreateEmpty();
        spec.AtomicNumber = ParseInt(parts[1], result, "atom atomic number", lineNumber);
        spec.IsotopeMassNumber = ParseInt(parts[2], result, "atom isotope", lineNumber);
        spec.FormalCharge = ParseInt(parts[3], result, "atom charge", lineNumber);
        spec.ExplicitHydrogenCount = ParseInt(parts[4], result, "atom explicit hydrogen count", lineNumber);
        spec.ImplicitHydrogenCount = ParseInt(parts[5], result, "atom implicit hydrogen count", lineNumber);
        spec.RadicalElectronCount = ParseInt(parts[6], result, "atom radical electron count", lineNumber);
        spec.AtomMapNumber = ParseInt(parts[7], result, "atom map number", lineNumber);
        spec.IsAromatic = ParseBoolInt(parts[8], result, "atom aromatic flag", lineNumber);
        spec.QueryOperation = (QueryOperation)ParseInt(parts[9], result, "atom query operation", lineNumber);
        spec.QueryValue = Unescape(parts[10], result, lineNumber);
        spec.Alias = Unescape(parts[11], result, lineNumber);
        spec.Label = Unescape(parts[12], result, lineNumber);
        if (!result.Success)
        {
            return;
        }

        builder.AddAtom(spec);
    }

    internal void ParseBondLine(string line, MoleculeBuilder builder, DeterministicParseResult result, int lineNumber)
    {
        string[] parts = line.Split('\t');
        if (parts.Length != 13 || parts[0] != "BOND")
        {
            Fail(result, "Invalid bond record.", lineNumber);
            return;
        }

        int beginAtomId = ParseInt(parts[1], result, "bond begin atom id", lineNumber);
        int endAtomId = ParseInt(parts[2], result, "bond end atom id", lineNumber);
        int directionalBeginAtomId = ParseInt(parts[3], result, "bond directional begin atom id", lineNumber);
        int directionalEndAtomId = ParseInt(parts[4], result, "bond directional end atom id", lineNumber);
        BondSpec spec = new BondSpec();
        spec.Order = (BondOrder)ParseInt(parts[5], result, "bond order", lineNumber);
        spec.IsAromatic = ParseBoolInt(parts[6], result, "bond aromatic flag", lineNumber);
        spec.Stereo = (BondStereo)ParseInt(parts[7], result, "bond stereo", lineNumber);
        spec.Direction = (BondDirection)ParseInt(parts[8], result, "bond direction", lineNumber);
        spec.IsDative = ParseBoolInt(parts[9], result, "bond dative flag", lineNumber);
        spec.QueryOperation = (QueryOperation)ParseInt(parts[10], result, "bond query operation", lineNumber);
        spec.QueryValue = Unescape(parts[11], result, lineNumber);
        spec.SourceFlags = Unescape(parts[12], result, lineNumber);
        if (!result.Success)
        {
            return;
        }

        if (HasDirectionalBondSpecMeaning(spec))
        {
            beginAtomId = directionalBeginAtomId;
            endAtomId = directionalEndAtomId;
        }

        int bondId = builder.AddBond(beginAtomId, endAtomId, spec);
        if (bondId < 0)
        {
            Fail(result, "Bond record references an atom id that does not exist.", lineNumber);
        }
    }

    internal int ParseHeaderCount(string line, string label, DeterministicParseResult result, int lineNumber)
    {
        string[] parts = line.Split('\t');
        if (parts.Length != 2 || parts[0] != label)
        {
            Fail(result, "Invalid " + label + " header.", lineNumber);
            return 0;
        }

        int value = ParseInt(parts[1], result, label + " count", lineNumber);
        if (value < 0)
        {
            Fail(result, label + " count cannot be negative.", lineNumber);
            return 0;
        }

        return value;
    }

    internal int ParseInt(string text, DeterministicParseResult result, string fieldName, int lineNumber)
    {
        string value = NormalizeString(text);
        if (value.Length == 0)
        {
            Fail(result, "Missing integer value for " + fieldName + ".", lineNumber);
            return 0;
        }

        int sign = 1;
        int index = 0;
        if (value[0] == '-')
        {
            sign = -1;
            index = 1;
        }

        if (index >= value.Length)
        {
            Fail(result, "Invalid integer value for " + fieldName + ".", lineNumber);
            return 0;
        }

        long limit = int.MaxValue;
        if (sign < 0)
        {
            limit = (long)int.MaxValue + 1;
        }

        long parsed = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsDigit(character))
            {
                Fail(result, "Invalid integer value for " + fieldName + ".", lineNumber);
                return 0;
            }

            int digit = character - '0';
            if (parsed > (limit - digit) / 10)
            {
                Fail(result, "Integer value for " + fieldName + " is out of range.", lineNumber);
                return 0;
            }

            parsed = parsed * 10 + digit;
            index = index + 1;
        }

        if (parsed == (long)int.MaxValue + 1)
        {
            return int.MinValue;
        }

        return (int)parsed * sign;
    }

    internal bool ParseBoolInt(string text, DeterministicParseResult result, string fieldName, int lineNumber)
    {
        int value = ParseInt(text, result, fieldName, lineNumber);
        if (value == 0)
        {
            return false;
        }

        if (value == 1)
        {
            return true;
        }

        Fail(result, "Boolean integer for " + fieldName + " must be 0 or 1.", lineNumber);
        return false;
    }

    internal int GetDirectionalBeginAtomIndex(CanonicalOrder order, Bond bond)
    {
        if (!HasDirectionalBondMeaning(bond))
        {
            return -1;
        }

        return order.GetCanonicalAtomIndex(bond.BeginAtomId);
    }

    internal int GetDirectionalEndAtomIndex(CanonicalOrder order, Bond bond)
    {
        if (!HasDirectionalBondMeaning(bond))
        {
            return -1;
        }

        return order.GetCanonicalAtomIndex(bond.EndAtomId);
    }

    internal bool HasDirectionalBondMeaning(Bond bond)
    {
        if (bond.Direction != BondDirection.None)
        {
            return true;
        }

        if (bond.Stereo == BondStereo.Wedge || bond.Stereo == BondStereo.Hash || bond.Stereo == BondStereo.UpOrDown)
        {
            return true;
        }

        return false;
    }

    internal bool HasDirectionalBondSpecMeaning(BondSpec bond)
    {
        if (bond.Direction != BondDirection.None)
        {
            return true;
        }

        if (bond.Stereo == BondStereo.Wedge || bond.Stereo == BondStereo.Hash || bond.Stereo == BondStereo.UpOrDown)
        {
            return true;
        }

        return false;
    }

    internal bool IsDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal void AppendIntField(StringBuilder builder, int value)
    {
        builder.Append("\t");
        builder.Append(value);
    }

    internal void AppendStringField(StringBuilder builder, string value)
    {
        builder.Append("\t");
        AppendEscaped(builder, value);
    }

    internal void AppendEscaped(StringBuilder builder, string value)
    {
        string normalized = NormalizeString(value);
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }
    }

    internal string Unescape(string value, DeterministicParseResult result, int lineNumber)
    {
        StringBuilder builder = new StringBuilder();
        string normalized = NormalizeString(value);
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character != '\\')
            {
                builder.Append(character);
                index = index + 1;
            }
            else
            {
                if (index + 1 >= normalized.Length)
                {
                    Fail(result, "String field ends with an incomplete escape sequence.", lineNumber);
                    return "";
                }

                char escaped = normalized[index + 1];
                if (escaped == '\\')
                {
                    builder.Append('\\');
                }
                else if (escaped == 't')
                {
                    builder.Append('\t');
                }
                else if (escaped == 'n')
                {
                    builder.Append('\n');
                }
                else if (escaped == 'r')
                {
                    builder.Append('\r');
                }
                else
                {
                    Fail(result, "String field contains an unsupported escape sequence.", lineNumber);
                    return "";
                }

                index = index + 2;
            }
        }

        return builder.ToString();
    }

    internal string NormalizeLineEndings(string? value)
    {
        return NormalizeString(value).Replace("\r\n", "\n").Replace('\r', '\n');
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal int BoolToInt(bool value)
    {
        if (value)
        {
            return 1;
        }

        return 0;
    }

    internal void Fail(DeterministicParseResult result, string message, int lineNumber)
    {
        result.Success = false;
        result.ErrorMessage = NormalizeString(message);
        result.LineNumber = lineNumber;
        result.Molecule = new Molecule();
    }
}

