using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as v 3000 data.
/// </summary>
public class V3000Writer : IV3000Writer
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="V3000Writer"/> class.
    /// </summary>
    public V3000Writer()
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
        return WriteV3000WithOptions(molecule, null).Text;
    }

    /// <summary>
    /// Writes MOL V3000 using explicit feature policies.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The molfile write options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The output text and diagnostics.</returns>
    public MolfileWriteResult WriteV3000WithOptions(Molecule? molecule, MolfileWriteOptions? options)
    {
        MolfileWriteResult result = new MolfileWriteResult();
        result.Version = MolfileVersion.V3000;
        if (molecule == null)
        {
            result.Report.AddError("MOLFILE_NULL_MOLECULE", "Cannot write a null molecule.");
            return result;
        }

        MolfileWriteOptions safeOptions = NormalizeWriteOptions(options);
        StringBuilder builder = new StringBuilder();
        AppendHeader(builder, molecule);
        builder.Append("  0  0  0  0  0  0            999 V3000");
        builder.Append('\n');
        builder.Append("M  V30 BEGIN CTAB");
        builder.Append('\n');
        AppendCountsLine(builder, molecule, safeOptions);
        AppendAtomBlock(builder, molecule, safeOptions);
        AppendBondBlock(builder, molecule);
        if (safeOptions.WriteEnhancedStereo)
        {
            AppendCollectionBlock(builder, molecule);
        }
        builder.Append("M  V30 END CTAB");
        builder.Append('\n');
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

    internal void AppendCountsLine(StringBuilder builder, Molecule molecule, MolfileWriteOptions options)
    {
        builder.Append("M  V30 COUNTS ");
        builder.Append(molecule.Atoms.Count);
        builder.Append(' ');
        builder.Append(molecule.Bonds.Count);
        builder.Append(" 0 0 ");
        if (options.WriteEnhancedStereo)
        {
            builder.Append(CountWritableCollections(molecule));
        }
        else
        {
            builder.Append(0);
        }
        builder.Append('\n');
    }

    internal void AppendAtomBlock(StringBuilder builder, Molecule molecule, MolfileWriteOptions options)
    {
        builder.Append("M  V30 BEGIN ATOM");
        builder.Append('\n');
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            AppendAtomLine(builder, molecule, molecule.Atoms[atomIndex], atomIndex, options);
            atomIndex = atomIndex + 1;
        }

        builder.Append("M  V30 END ATOM");
        builder.Append('\n');
    }

    internal void AppendAtomLine(StringBuilder builder, Molecule molecule, Atom atom, int atomIndex, MolfileWriteOptions options)
    {
        builder.Append("M  V30 ");
        builder.Append(atomIndex + 1);
        builder.Append(' ');
        builder.Append(GetAtomSymbol(atom, options));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomX(atom)));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomY(atom)));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomZ(atom)));
        builder.Append(' ');
        builder.Append(atom.AtomMapNumber);
        AppendAtomProperties(builder, molecule, atom, atomIndex, options);
        builder.Append('\n');
    }

    internal void AppendAtomProperties(StringBuilder builder, Molecule molecule, Atom atom, int atomIndex, MolfileWriteOptions options)
    {
        if (options.WriteCommonProperties && atom.FormalCharge != 0)
        {
            builder.Append(" CHG=");
            builder.Append(atom.FormalCharge);
        }

        if (options.WriteCommonProperties && atom.IsotopeMassNumber != 0)
        {
            builder.Append(" MASS=");
            builder.Append(atom.IsotopeMassNumber);
        }

        if (options.WriteCommonProperties && atom.RadicalElectronCount != 0)
        {
            builder.Append(" RAD=");
            builder.Append(GetRadicalCode(atom.RadicalElectronCount));
        }

        int cfg = GetAtomCfg(molecule, atomIndex);
        if (cfg != 0)
        {
            builder.Append(" CFG=");
            builder.Append(cfg);
        }

        if (options.WriteAliases && NormalizeString(atom.Alias) != "")
        {
            builder.Append(" LABEL=\"");
            builder.Append(NormalizeAlias(atom.Alias));
            builder.Append('"');
        }
    }

    internal void AppendBondBlock(StringBuilder builder, Molecule molecule)
    {
        builder.Append("M  V30 BEGIN BOND");
        builder.Append('\n');
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            AppendBondLine(builder, molecule.Bonds[bondIndex], bondIndex);
            bondIndex = bondIndex + 1;
        }

        builder.Append("M  V30 END BOND");
        builder.Append('\n');
    }

    internal void AppendBondLine(StringBuilder builder, Bond bond, int bondIndex)
    {
        builder.Append("M  V30 ");
        builder.Append(bondIndex + 1);
        builder.Append(' ');
        builder.Append(GetBondType(bond));
        builder.Append(' ');
        builder.Append(bond.BeginAtomId + 1);
        builder.Append(' ');
        builder.Append(bond.EndAtomId + 1);

        int cfg = GetBondCfg(bond);
        if (cfg != 0)
        {
            builder.Append(" CFG=");
            builder.Append(cfg);
        }

        builder.Append('\n');
    }

    internal void AppendCollectionBlock(StringBuilder builder, Molecule molecule)
    {
        if (CountWritableCollections(molecule) == 0)
        {
            return;
        }

        builder.Append("M  V30 BEGIN COLLECTION");
        builder.Append('\n');
        int groupIndex = 0;
        while (groupIndex < molecule.StereoGroups.Groups.Count)
        {
            EnhancedStereoGroup group = molecule.StereoGroups.Groups[groupIndex];
            if (CountWritableGroupAtoms(molecule, group) > 0)
            {
                AppendCollectionGroup(builder, molecule, group);
            }

            groupIndex = groupIndex + 1;
        }

        builder.Append("M  V30 END COLLECTION");
        builder.Append('\n');
    }

    internal void AppendCollectionGroup(StringBuilder builder, Molecule molecule, EnhancedStereoGroup group)
    {
        builder.Append("M  V30 ");
        builder.Append(GetCollectionLabel(group));
        builder.Append(" ATOMS=(");
        builder.Append(CountWritableGroupAtoms(molecule, group));

        int recordIndex = 0;
        while (recordIndex < group.StereoRecordIds.Count)
        {
            int stereoRecordId = group.StereoRecordIds[recordIndex];
            if (stereoRecordId >= 0 && stereoRecordId < molecule.StereoRecords.Count)
            {
                StereoRecord record = molecule.StereoRecords[stereoRecordId];
                if (record.IsTetrahedral() && molecule.IsValidAtomId(record.CenterAtomId))
                {
                    builder.Append(' ');
                    builder.Append(record.CenterAtomId + 1);
                }
            }

            recordIndex = recordIndex + 1;
        }

        builder.Append(')');
        builder.Append('\n');
    }

    internal int CountWritableCollections(Molecule molecule)
    {
        int count = 0;
        int index = 0;
        while (index < molecule.StereoGroups.Groups.Count)
        {
            if (CountWritableGroupAtoms(molecule, molecule.StereoGroups.Groups[index]) > 0)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountWritableGroupAtoms(Molecule molecule, EnhancedStereoGroup group)
    {
        int count = 0;
        int index = 0;
        while (index < group.StereoRecordIds.Count)
        {
            int stereoRecordId = group.StereoRecordIds[index];
            if (stereoRecordId >= 0 && stereoRecordId < molecule.StereoRecords.Count)
            {
                StereoRecord record = molecule.StereoRecords[stereoRecordId];
                if (record.IsTetrahedral() && molecule.IsValidAtomId(record.CenterAtomId))
                {
                    count = count + 1;
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal string GetCollectionLabel(EnhancedStereoGroup group)
    {
        if (NormalizeString(group.Label) != "")
        {
            return group.Label;
        }

        if (group.Type == "abs")
        {
            return "MDLV30/STEABS";
        }

        if (group.Type == "and")
        {
            return "MDLV30/STERAC" + (group.Id + 1).ToString();
        }

        return "MDLV30/STEREL" + (group.Id + 1).ToString();
    }

    internal int GetAtomCfg(Molecule molecule, int atomId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsTetrahedral() && record.CenterAtomId == atomId)
            {
                return GetChiralityCfg(record.Chirality);
            }

            index = index + 1;
        }

        return 0;
    }

    internal int GetChiralityCfg(Chirality chirality)
    {
        if (chirality == Chirality.Clockwise)
        {
            return 1;
        }

        if (chirality == Chirality.CounterClockwise)
        {
            return 2;
        }

        if (chirality == Chirality.Unspecified)
        {
            return 3;
        }

        return 0;
    }

    internal int GetBondCfg(Bond bond)
    {
        if (bond.Stereo == BondStereo.Wedge || bond.Direction == BondDirection.Up)
        {
            return 1;
        }

        if (bond.Stereo == BondStereo.UpOrDown || bond.Direction == BondDirection.Either)
        {
            return 2;
        }

        if (bond.Stereo == BondStereo.Hash || bond.Direction == BondDirection.Down)
        {
            return 3;
        }

        return 0;
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

    internal string GetAtomSymbol(Atom atom, MolfileWriteOptions options)
    {
        if (options.WriteAtomLists && (atom.QueryOperation == QueryOperation.InList || atom.QueryOperation == QueryOperation.NotInList) && NormalizeString(atom.QueryValue) != "")
        {
            if (atom.QueryOperation == QueryOperation.NotInList)
            {
                return "NOT[" + NormalizeAtomList(atom.QueryValue) + "]";
            }

            return "[" + NormalizeAtomList(atom.QueryValue) + "]";
        }

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

    internal string NormalizeAtomList(string value)
    {
        string text = NormalizeString(value);
        text = text.Replace(" ", "");
        text = text.Replace("[", "");
        text = text.Replace("]", "");
        return text;
    }

    internal string NormalizeAlias(string value)
    {
        string text = NormalizeLineText(value);
        return text.Replace('"', '\'');
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

    internal string FormatCoordinate(double value)
    {
        return NumberFormatting.FormatDoubleWithPattern(value, "0.0000");
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
