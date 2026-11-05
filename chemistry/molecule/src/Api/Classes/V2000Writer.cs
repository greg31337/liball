using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as v 2000 data.
/// </summary>
public class V2000Writer : IV2000Writer
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="V2000Writer"/> class.
    /// </summary>
    public V2000Writer()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return WriteV2000WithOptions(molecule, null).Text;
    }

    /// <summary>
    /// Writes MOL V2000 using explicit feature policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The molfile write options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The output text and diagnostics.</returns>
    public MolfileWriteResult WriteV2000WithOptions(Molecule? molecule, MolfileWriteOptions? options)
    {
        MolfileWriteResult result = new MolfileWriteResult();
        result.Version = MolfileVersion.V2000;
        if (molecule == null)
        {
            result.Report.AddError("MOLFILE_NULL_MOLECULE", "Cannot write a null molecule.");
            return result;
        }

        if (molecule.Atoms.Count > 999 || molecule.Bonds.Count > 999)
        {
            result.Report.AddError("MOLFILE_V2000_COUNT_LIMIT", "MOL V2000 cannot represent more than 999 atoms or bonds.");
            return result;
        }

        MolfileWriteOptions safeOptions = NormalizeWriteOptions(options);
        StringBuilder builder = new StringBuilder();
        AppendHeader(builder, molecule);
        AppendCountsLine(builder, molecule);
        AppendAtomBlock(builder, molecule);
        AppendBondBlock(builder, molecule);
        AppendPropertyBlock(builder, molecule, safeOptions);
        builder.Append("M  END");
        builder.Append('\n');
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    internal void AppendHeader(StringBuilder builder, Molecule molecule)
    {
        builder.Append(NormalizeLineText(molecule.Metadata.GetPrimaryName()));
        builder.Append('\n');

        if (molecule.Metadata.Comments.Count > 0)
        {
            builder.Append(NormalizeLineText(molecule.Metadata.Comments[0]));
        }

        builder.Append('\n');

        if (molecule.Metadata.Comments.Count > 1)
        {
            builder.Append(NormalizeLineText(molecule.Metadata.Comments[1]));
        }

        builder.Append('\n');
    }

    internal void AppendCountsLine(StringBuilder builder, Molecule molecule)
    {
        AppendPaddedInt(builder, molecule.Atoms.Count, 3);
        AppendPaddedInt(builder, molecule.Bonds.Count, 3);
        builder.Append("  0  0  0  0            999 V2000");
        builder.Append('\n');
    }

    internal void AppendAtomBlock(StringBuilder builder, Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            AppendAtomLine(builder, molecule.Atoms[atomIndex]);
            atomIndex = atomIndex + 1;
        }
    }

    internal void AppendAtomLine(StringBuilder builder, Atom atom)
    {
        AppendCoordinate(builder, GetAtomX(atom));
        AppendCoordinate(builder, GetAtomY(atom));
        AppendCoordinate(builder, GetAtomZ(atom));
        builder.Append(' ');
        builder.Append(GetAtomSymbol(atom).PadRight(3));
        builder.Append("  0  0  0  0  0  0  0  0  0  0  0  0");
        builder.Append('\n');
    }

    internal void AppendBondBlock(StringBuilder builder, Molecule molecule)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            AppendBondLine(builder, molecule.Bonds[bondIndex]);
            bondIndex = bondIndex + 1;
        }
    }

    internal void AppendBondLine(StringBuilder builder, Bond bond)
    {
        AppendPaddedInt(builder, bond.BeginAtomId + 1, 3);
        AppendPaddedInt(builder, bond.EndAtomId + 1, 3);
        AppendPaddedInt(builder, GetBondType(bond), 3);
        AppendPaddedInt(builder, GetBondStereoCode(bond), 3);
        builder.Append("  0  0  0");
        builder.Append('\n');
    }

    internal void AppendPropertyBlock(StringBuilder builder, Molecule molecule, MolfileWriteOptions options)
    {
        if (options.WriteCommonProperties)
        {
            AppendAtomIntegerRecords(builder, molecule, "CHG");
            AppendAtomIntegerRecords(builder, molecule, "ISO");
            AppendAtomIntegerRecords(builder, molecule, "RAD");
        }

        if (options.WriteAtomLists)
        {
            AppendAtomListRecords(builder, molecule);
        }

        if (options.WriteAliases)
        {
            AppendAliasRecords(builder, molecule);
        }
    }

    internal void AppendAtomListRecords(StringBuilder builder, Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if ((atom.QueryOperation == QueryOperation.InList || atom.QueryOperation == QueryOperation.NotInList) && NormalizeString(atom.QueryValue) != "")
            {
                string[] values = atom.QueryValue.Split(',');
                builder.Append("M  ALS");
                AppendPaddedInt(builder, atomIndex + 1, 4);
                AppendPaddedInt(builder, values.Length, 3);
                builder.Append(' ');
                if (atom.QueryOperation == QueryOperation.NotInList)
                {
                    builder.Append('T');
                }
                else
                {
                    builder.Append('F');
                }

                int valueIndex = 0;
                while (valueIndex < values.Length)
                {
                    builder.Append(' ');
                    builder.Append(NormalizeString(values[valueIndex]).Trim());
                    valueIndex = valueIndex + 1;
                }

                builder.Append('\n');
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal MolfileWriteOptions NormalizeWriteOptions(MolfileWriteOptions? options)
    {
        MolfileWriteOptions safeOptions = new MolfileWriteOptions();
        if (options != null)
        {
            safeOptions.Version = options.Version;
            safeOptions.WriteAtomLists = options.WriteAtomLists;
            safeOptions.WriteAliases = options.WriteAliases;
            safeOptions.WriteCommonProperties = options.WriteCommonProperties;
            safeOptions.WriteEnhancedStereo = options.WriteEnhancedStereo;
            safeOptions.V2000AtomLimit = options.V2000AtomLimit;
            safeOptions.V2000BondLimit = options.V2000BondLimit;
        }

        return safeOptions;
    }

    internal void AppendAtomIntegerRecords(StringBuilder builder, Molecule molecule, string recordName)
    {
        List<int> atomNumbers = new List<int>();
        List<int> values = new List<int>();
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            int value = GetRecordValue(molecule.Atoms[atomIndex], recordName);
            if (value != 0)
            {
                atomNumbers.Add(atomIndex + 1);
                values.Add(value);
            }

            atomIndex = atomIndex + 1;
        }

        AppendIndexedRecords(builder, recordName, atomNumbers, values);
    }

    internal void AppendIndexedRecords(StringBuilder builder, string recordName, List<int> atomNumbers, List<int> values)
    {
        int index = 0;
        while (index < atomNumbers.Count)
        {
            int chunkCount = CountRecordChunk(atomNumbers.Count, index);
            builder.Append("M  ");
            builder.Append(recordName);
            AppendPaddedInt(builder, chunkCount, 3);

            int chunkIndex = 0;
            while (chunkIndex < chunkCount)
            {
                AppendPaddedInt(builder, atomNumbers[index + chunkIndex], 4);
                AppendPaddedInt(builder, values[index + chunkIndex], 4);
                chunkIndex = chunkIndex + 1;
            }

            builder.Append('\n');
            index = index + chunkCount;
        }
    }

    internal int CountRecordChunk(int totalCount, int startIndex)
    {
        int remaining = totalCount - startIndex;
        if (remaining > 8)
        {
            return 8;
        }

        return remaining;
    }

    internal void AppendAliasRecords(StringBuilder builder, Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (NormalizeString(molecule.Atoms[atomIndex].Alias) != "")
            {
                builder.Append("A");
                AppendPaddedInt(builder, atomIndex + 1, 5);
                builder.Append('\n');
                builder.Append(NormalizeLineText(molecule.Atoms[atomIndex].Alias));
                builder.Append('\n');
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal int GetRecordValue(Atom atom, string recordName)
    {
        if (recordName == "CHG")
        {
            return atom.FormalCharge;
        }

        if (recordName == "ISO")
        {
            return atom.IsotopeMassNumber;
        }

        if (recordName == "RAD")
        {
            return GetRadicalCode(atom.RadicalElectronCount);
        }

        return 0;
    }

    internal int GetRadicalCode(int radicalElectronCount)
    {
        if (radicalElectronCount == 1)
        {
            return 2;
        }

        if (radicalElectronCount == 2)
        {
            return 1;
        }

        return radicalElectronCount;
    }

    internal int GetBondType(Bond bond)
    {
        if (bond.IsDative || bond.Order == BondOrder.Dative)
        {
            return 9;
        }

        if (bond.Order == BondOrder.Single)
        {
            return 1;
        }

        if (bond.Order == BondOrder.Double)
        {
            return 2;
        }

        if (bond.Order == BondOrder.Triple)
        {
            return 3;
        }

        if (bond.Order == BondOrder.Aromatic || bond.IsAromatic)
        {
            return 4;
        }

        return 0;
    }

    internal int GetBondStereoCode(Bond bond)
    {
        if (bond.Stereo == BondStereo.Wedge || bond.Direction == BondDirection.Up)
        {
            return 1;
        }

        if (bond.Stereo == BondStereo.UpOrDown || bond.Direction == BondDirection.Either)
        {
            return 4;
        }

        if (bond.Stereo == BondStereo.Hash || bond.Direction == BondDirection.Down)
        {
            return 6;
        }

        if (bond.Stereo == BondStereo.Cis)
        {
            return 3;
        }

        return 0;
    }

    internal string GetAtomSymbol(Atom atom)
    {
        if (atom.AtomicNumber > 0)
        {
            Element element = Elements.GetElement(atom.AtomicNumber);
            if (element.Symbol != "")
            {
                return element.Symbol;
            }
        }

        if (NormalizeString(atom.Label) != "")
        {
            return atom.Label;
        }

        return "*";
    }

    internal double GetAtomX(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.X;
        }

        return 0.0;
    }

    internal double GetAtomY(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.Y;
        }

        return 0.0;
    }

    internal double GetAtomZ(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.Z;
        }

        return 0.0;
    }

    internal void AppendCoordinate(StringBuilder builder, double value)
    {
        string text = NumberFormatting.FormatDoubleWithPattern(value, "0.0000");
        builder.Append(text.PadLeft(10));
    }

    internal void AppendPaddedInt(StringBuilder builder, int value, int width)
    {
        builder.Append(value.ToString().PadLeft(width));
    }

    internal string NormalizeLineText(string value)
    {
        string text = NormalizeString(value);
        text = text.Replace('\r', ' ');
        text = text.Replace('\n', ' ');
        return text;
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
