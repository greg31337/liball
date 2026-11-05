using System;
using System.Security.Cryptography;
using System.Text;

namespace molecule;


/// <summary>
/// Provides structural hash data and operations for molecular graphs.
/// </summary>
public class StructuralHash : IStructuralHash
{
    internal string Version;
    internal ParentMoleculeFactory ParentFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralHash"/> class.
    /// </summary>
    public StructuralHash()
    {
        Version = "molecule-structural-exact-v1";
        ParentFactory = new ParentMoleculeFactory();
    }

    /// <summary>
    /// Compute.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Compute(Molecule? molecule)
    {
        return ComputeExact(molecule);
    }

    /// <summary>
    /// Computes a structural hash using the specified identity policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string ComputeWithPolicy(Molecule? molecule, IdentityPolicy policy)
    {
        string canonicalText = BuildCanonicalTextWithPolicy(molecule, policy);
        return ComputeHash(canonicalText);
    }

    /// <summary>
    /// Computes a structural hash using independent identity participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The resulting identity hash.</returns>
    public string ComputeWithOptions(Molecule? molecule, IdentityOptions? options)
    {
        return ComputeHash(BuildCanonicalTextWithOptions(molecule, options));
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string ComputeExact(Molecule? molecule)
    {
        string canonicalText = BuildCanonicalText(molecule);
        return ComputeHash(canonicalText);
    }

    internal string ComputeHash(string canonicalText)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(NormalizeString(canonicalText));
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Builds the canonical text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string BuildCanonicalText(Molecule? molecule)
    {
        return BuildCanonicalTextWithPolicy(molecule, IdentityPolicy.Exact);
    }

    /// <summary>
    /// Builds canonical text using the specified identity policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string BuildCanonicalTextWithPolicy(Molecule? molecule, IdentityPolicy policy)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Version);
        builder.Append("|");
        builder.Append("policy=");
        builder.Append(GetPolicyName(policy));
        builder.Append("|");

        if (molecule == null)
        {
            builder.Append("null");
            return builder.ToString();
        }

        Molecule identityMolecule = NormalizeParentPolicyMolecule(molecule, policy);
        IdentityPolicy serializationPolicy = GetSerializationPolicy(policy);
        CanonicalOrder order = CanonicalOrder.FromMolecule(identityMolecule);
        builder.Append("atoms=");
        builder.Append(identityMolecule.Atoms.Count);
        builder.Append("|bonds=");
        builder.Append(identityMolecule.Bonds.Count);
        builder.Append("|");

        int atomIndex = 0;
        while (atomIndex < order.AtomIdsByCanonicalIndex.Count)
        {
            AppendAtomRecordWithPolicy(builder, identityMolecule, order, atomIndex, serializationPolicy);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < order.BondIdsByCanonicalIndex.Count)
        {
            AppendBondRecordWithPolicy(builder, identityMolecule, order, bondIndex, serializationPolicy);
            bondIndex = bondIndex + 1;
        }

        AppendStereoRecordsWithPolicy(builder, identityMolecule, order, serializationPolicy);
        AppendStereoGroupsWithPolicy(builder, identityMolecule, serializationPolicy);
        return builder.ToString();
    }

    internal Molecule NormalizeParentPolicyMolecule(Molecule molecule, IdentityPolicy policy)
    {
        if (IsParentPolicy(policy))
        {
            return ParentFactory.CreateParent(molecule, policy);
        }

        return molecule;
    }

    internal bool IsParentPolicy(IdentityPolicy policy)
    {
        return policy == IdentityPolicy.FragmentParent
            || policy == IdentityPolicy.ChargeParent
            || policy == IdentityPolicy.StereoParent
            || policy == IdentityPolicy.IsotopeParent
            || policy == IdentityPolicy.TautomerParent
            || policy == IdentityPolicy.SuperParent;
    }

    internal IdentityPolicy GetSerializationPolicy(IdentityPolicy policy)
    {
        if (IsParentPolicy(policy))
        {
            return IdentityPolicy.Exact;
        }

        return policy;
    }

    /// <summary>
    /// Builds canonical text using independent identity participation options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The identity options; <c>null</c> uses exact defaults.</param>
    /// <returns>The canonical identity text.</returns>
    public string BuildCanonicalTextWithOptions(Molecule? molecule, IdentityOptions? options)
    {
        IdentityOptions safeOptions = NormalizeStructuralIdentityOptions(options);
        if (IdentityOptionsAreExactDefaults(safeOptions))
        {
            return BuildCanonicalTextWithPolicy(molecule, IdentityPolicy.Exact);
        }

        Molecule? normalized = NormalizeMoleculeWithIdentityOptions(molecule, safeOptions);
        StringBuilder builder = new StringBuilder();
        builder.Append("molecule-identity-options-v1|");
        AppendIdentityOption(builder, "isotope", safeOptions.IncludeIsotopes);
        AppendIdentityOption(builder, "charge", safeOptions.IncludeCharges);
        AppendIdentityOption(builder, "hydrogen", safeOptions.IncludeHydrogens);
        AppendIdentityOption(builder, "atom-map", safeOptions.IncludeAtomMaps);
        AppendIdentityOption(builder, "aromaticity", safeOptions.IncludeAromaticity);
        AppendIdentityOption(builder, "stereo", safeOptions.IncludeStereo);
        builder.Append(BuildCanonicalTextWithPolicy(normalized, safeOptions.Policy));
        return builder.ToString();
    }

    internal IdentityOptions NormalizeStructuralIdentityOptions(IdentityOptions? options)
    {
        IdentityOptions safeOptions = new IdentityOptions();
        if (options != null)
        {
            safeOptions.Policy = options.Policy;
            safeOptions.StoreMolecules = options.StoreMolecules;
            safeOptions.IncludeIsotopes = options.IncludeIsotopes;
            safeOptions.IncludeCharges = options.IncludeCharges;
            safeOptions.IncludeHydrogens = options.IncludeHydrogens;
            safeOptions.IncludeAtomMaps = options.IncludeAtomMaps;
            safeOptions.IncludeAromaticity = options.IncludeAromaticity;
            safeOptions.IncludeStereo = options.IncludeStereo;
        }

        return safeOptions;
    }

    internal bool IdentityOptionsAreExactDefaults(IdentityOptions options)
    {
        return options.Policy == IdentityPolicy.Exact
            && options.IncludeIsotopes
            && options.IncludeCharges
            && options.IncludeHydrogens
            && options.IncludeAtomMaps
            && options.IncludeAromaticity
            && options.IncludeStereo;
    }

    internal Molecule? NormalizeMoleculeWithIdentityOptions(Molecule? molecule, IdentityOptions options)
    {
        if (molecule == null)
        {
            return null;
        }

        Molecule normalized = MoleculeBuilder.CloneMolecule(molecule);
        if (IsParentPolicy(options.Policy))
        {
            normalized = NormalizeParentPolicyMolecule(molecule, options.Policy);
        }
        if (!options.IncludeHydrogens)
        {
            MoleculeBuilder editor = MoleculeBuilder.CreateFrom(normalized);
            int atomIndex = normalized.Atoms.Count - 1;
            while (atomIndex >= 0)
            {
                if (normalized.Atoms[atomIndex].AtomicNumber == 1)
                {
                    editor.RemoveAtom(atomIndex);
                }

                atomIndex = atomIndex - 1;
            }

            normalized = editor.Build();
        }

        IdentityPolicy participationPolicy = GetSerializationPolicy(options.Policy);
        bool connectivityLike = IsConnectivityLike(participationPolicy);
        int index = 0;
        while (index < normalized.Atoms.Count)
        {
            Atom atom = normalized.Atoms[index];
            if (!options.IncludeIsotopes || participationPolicy == IdentityPolicy.IsotopeInsensitive || connectivityLike)
            {
                atom.IsotopeMassNumber = 0;
            }

            if (!options.IncludeCharges || participationPolicy == IdentityPolicy.ChargeInsensitive || connectivityLike)
            {
                atom.FormalCharge = 0;
            }

            if (!options.IncludeHydrogens || connectivityLike)
            {
                atom.ExplicitHydrogenCount = 0;
                atom.ImplicitHydrogenCount = 0;
            }

            if (!options.IncludeAtomMaps || connectivityLike)
            {
                atom.AtomMapNumber = 0;
            }

            if (!options.IncludeAromaticity || connectivityLike)
            {
                atom.IsAromatic = false;
            }

            if (connectivityLike)
            {
                atom.RadicalElectronCount = 0;
                atom.QueryOperation = QueryOperation.None;
                atom.QueryValue = "";
                atom.Alias = "";
                atom.Label = "";
            }

            index = index + 1;
        }

        index = 0;
        while (index < normalized.Bonds.Count)
        {
            Bond bond = normalized.Bonds[index];
            if (!options.IncludeAromaticity || connectivityLike)
            {
                bond.IsAromatic = false;
                if (bond.Order == BondOrder.Aromatic)
                {
                    bond.Order = BondOrder.Single;
                }
            }

            if (!options.IncludeStereo || !IncludesStereo(participationPolicy))
            {
                bond.Stereo = BondStereo.None;
                bond.Direction = BondDirection.None;
            }

            if (connectivityLike)
            {
                bond.Order = BondOrder.Single;
                bond.IsDative = false;
                bond.QueryOperation = QueryOperation.None;
                bond.QueryValue = "";
                bond.SourceFlags = "";
            }

            index = index + 1;
        }

        if (!options.IncludeStereo || !IncludesStereo(participationPolicy))
        {
            normalized.StereoRecords.Clear();
            normalized.StereoGroups = new StereoGroups();
        }

        normalized.ClearCaches();
        return normalized;
    }

    internal void AppendIdentityOption(StringBuilder builder, string name, bool included)
    {
        builder.Append(name);
        builder.Append("=");
        builder.Append(included ? "1" : "0");
        builder.Append("|");
    }

    internal void AppendAtomRecord(StringBuilder builder, Molecule molecule, CanonicalOrder order, int canonicalAtomIndex)
    {
        AppendAtomRecordWithPolicy(builder, molecule, order, canonicalAtomIndex, IdentityPolicy.Exact);
    }

    internal void AppendAtomRecordWithPolicy(StringBuilder builder, Molecule molecule, CanonicalOrder order, int canonicalAtomIndex, IdentityPolicy policy)
    {
        int atomId = order.GetOriginalAtomId(canonicalAtomIndex);
        if (atomId < 0 || atomId >= molecule.Atoms.Count)
        {
            return;
        }

        Atom atom = molecule.Atoms[atomId];
        builder.Append("A|");
        builder.Append(canonicalAtomIndex);
        AppendInt(builder, atom.AtomicNumber);
        AppendInt(builder, SelectIsotopeMassNumber(atom, policy));
        AppendInt(builder, SelectFormalCharge(atom, policy));
        AppendInt(builder, SelectExplicitHydrogenCount(atom, policy));
        AppendInt(builder, SelectImplicitHydrogenCount(atom, policy));
        AppendInt(builder, SelectRadicalElectronCount(atom, policy));
        AppendInt(builder, SelectAtomMapNumber(atom, policy));
        AppendInt(builder, SelectAtomAromaticValue(atom, policy));
        AppendInt(builder, SelectAtomQueryOperationValue(atom, policy));
        AppendString(builder, SelectAtomQueryValue(atom, policy));
        AppendString(builder, SelectAtomAlias(atom, policy));
        AppendString(builder, SelectAtomLabel(atom, policy));
        builder.Append("|");
    }

    internal void AppendBondRecord(StringBuilder builder, Molecule molecule, CanonicalOrder order, int canonicalBondIndex)
    {
        AppendBondRecordWithPolicy(builder, molecule, order, canonicalBondIndex, IdentityPolicy.Exact);
    }

    internal void AppendBondRecordWithPolicy(StringBuilder builder, Molecule molecule, CanonicalOrder order, int canonicalBondIndex, IdentityPolicy policy)
    {
        int bondId = order.GetOriginalBondId(canonicalBondIndex);
        if (bondId < 0 || bondId >= molecule.Bonds.Count)
        {
            return;
        }

        Bond bond = molecule.Bonds[bondId];
        builder.Append("B|");
        builder.Append(canonicalBondIndex);
        AppendInt(builder, order.GetCanonicalBondBeginAtomIndex(canonicalBondIndex));
        AppendInt(builder, order.GetCanonicalBondEndAtomIndex(canonicalBondIndex));
        AppendInt(builder, SelectBondOrderValue(bond, policy));
        AppendInt(builder, SelectBondAromaticValue(bond, policy));
        AppendInt(builder, SelectBondStereoValue(bond, policy));
        AppendInt(builder, SelectBondDirectionValue(bond, policy));
        AppendInt(builder, SelectBondDativeValue(bond, policy));
        AppendInt(builder, GetDirectionalBeginAtomIndexWithPolicy(order, bond, policy));
        AppendInt(builder, GetDirectionalEndAtomIndexWithPolicy(order, bond, policy));
        AppendInt(builder, SelectBondQueryOperationValue(bond, policy));
        AppendString(builder, SelectBondQueryValue(bond, policy));
        AppendString(builder, SelectBondSourceFlags(bond, policy));
        builder.Append("|");
    }

    internal void AppendStereoRecords(StringBuilder builder, Molecule molecule, CanonicalOrder order)
    {
        AppendStereoRecordsWithPolicy(builder, molecule, order, IdentityPolicy.Exact);
    }

    internal void AppendStereoRecordsWithPolicy(StringBuilder builder, Molecule molecule, CanonicalOrder order, IdentityPolicy policy)
    {
        if (!IncludesStereo(policy))
        {
            builder.Append("stereo=0|");
            return;
        }

        builder.Append("stereo=");
        builder.Append(molecule.StereoRecords.Count);
        builder.Append("|");

        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            builder.Append("S|");
            AppendString(builder, record.Kind);
            AppendInt(builder, order.GetCanonicalAtomIndex(record.CenterAtomId));
            AppendInt(builder, (int)record.Chirality);
            AppendInt(builder, order.GetCanonicalBondIndex(record.StereoBondId));
            AppendInt(builder, order.GetCanonicalAtomIndex(record.BeginLigandAtomId));
            AppendInt(builder, order.GetCanonicalAtomIndex(record.EndLigandAtomId));
            AppendInt(builder, (int)record.Configuration);
            AppendInt(builder, record.GroupId);
            AppendString(builder, record.Source);
            AppendLigands(builder, order, record);
            builder.Append("|");
            index = index + 1;
        }
    }

    internal void AppendStereoGroups(StringBuilder builder, Molecule molecule)
    {
        AppendStereoGroupsWithPolicy(builder, molecule, IdentityPolicy.Exact);
    }

    internal void AppendStereoGroupsWithPolicy(StringBuilder builder, Molecule molecule, IdentityPolicy policy)
    {
        if (!IncludesStereo(policy))
        {
            builder.Append("groups=0|");
            return;
        }

        if (molecule.StereoGroups == null)
        {
            builder.Append("groups=0|");
            return;
        }

        builder.Append("groups=");
        builder.Append(molecule.StereoGroups.Groups.Count);
        builder.Append("|");

        int groupIndex = 0;
        while (groupIndex < molecule.StereoGroups.Groups.Count)
        {
            EnhancedStereoGroup group = molecule.StereoGroups.Groups[groupIndex];
            builder.Append("G|");
            AppendInt(builder, group.Id);
            AppendString(builder, group.Type);
            AppendString(builder, group.Source);
            AppendString(builder, group.Label);
            AppendIntList(builder, group.StereoRecordIds);
            builder.Append("|");
            groupIndex = groupIndex + 1;
        }
    }

    internal void AppendLigands(StringBuilder builder, CanonicalOrder order, StereoRecord record)
    {
        builder.Append("|L");
        int index = 0;
        while (index < record.LigandAtomIds.Count)
        {
            AppendInt(builder, order.GetCanonicalAtomIndex(record.LigandAtomIds[index]));
            index = index + 1;
        }
    }

    internal void AppendIntList(StringBuilder builder, System.Collections.Generic.List<int> values)
    {
        builder.Append("|I");
        int index = 0;
        while (index < values.Count)
        {
            AppendInt(builder, values[index]);
            index = index + 1;
        }
    }

    internal int GetDirectionalBeginAtomIndex(CanonicalOrder order, Bond bond)
    {
        return GetDirectionalBeginAtomIndexWithPolicy(order, bond, IdentityPolicy.Exact);
    }

    internal int GetDirectionalBeginAtomIndexWithPolicy(CanonicalOrder order, Bond bond, IdentityPolicy policy)
    {
        if (!HasDirectionalBondMeaningWithPolicy(bond, policy))
        {
            return -1;
        }

        return order.GetCanonicalAtomIndex(bond.BeginAtomId);
    }

    internal int GetDirectionalEndAtomIndex(CanonicalOrder order, Bond bond)
    {
        return GetDirectionalEndAtomIndexWithPolicy(order, bond, IdentityPolicy.Exact);
    }

    internal int GetDirectionalEndAtomIndexWithPolicy(CanonicalOrder order, Bond bond, IdentityPolicy policy)
    {
        if (!HasDirectionalBondMeaningWithPolicy(bond, policy))
        {
            return -1;
        }

        return order.GetCanonicalAtomIndex(bond.EndAtomId);
    }

    internal bool HasDirectionalBondMeaning(Bond bond)
    {
        return HasDirectionalBondMeaningWithPolicy(bond, IdentityPolicy.Exact);
    }

    internal bool HasDirectionalBondMeaningWithPolicy(Bond bond, IdentityPolicy policy)
    {
        if (!IncludesStereo(policy))
        {
            return false;
        }

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

    internal string GetPolicyName(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity)
        {
            return "connectivity";
        }

        if (policy == IdentityPolicy.StereoInsensitive)
        {
            return "stereo-insensitive";
        }

        if (policy == IdentityPolicy.ChargeInsensitive)
        {
            return "charge-insensitive";
        }

        if (policy == IdentityPolicy.IsotopeInsensitive)
        {
            return "isotope-insensitive";
        }

        if (policy == IdentityPolicy.TautomerInsensitive)
        {
            return "tautomer-insensitive";
        }

        if (policy == IdentityPolicy.FragmentParent)
        {
            return "fragment-parent";
        }

        if (policy == IdentityPolicy.ChargeParent)
        {
            return "charge-parent";
        }

        if (policy == IdentityPolicy.StereoParent)
        {
            return "stereo-parent";
        }

        if (policy == IdentityPolicy.IsotopeParent)
        {
            return "isotope-parent";
        }

        if (policy == IdentityPolicy.TautomerParent)
        {
            return "tautomer-parent";
        }

        if (policy == IdentityPolicy.SuperParent)
        {
            return "super-parent";
        }

        return "exact";
    }

    internal bool IncludesStereo(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.StereoInsensitive || policy == IdentityPolicy.StereoParent)
        {
            return false;
        }

        if (policy == IdentityPolicy.Connectivity || policy == IdentityPolicy.TautomerInsensitive || policy == IdentityPolicy.TautomerParent)
        {
            return false;
        }

        return true;
    }

    internal bool IsConnectivityLike(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.Connectivity || policy == IdentityPolicy.TautomerInsensitive || policy == IdentityPolicy.TautomerParent)
        {
            return true;
        }

        return false;
    }

    internal int SelectIsotopeMassNumber(Atom atom, IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.IsotopeInsensitive || IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.IsotopeMassNumber;
    }

    internal int SelectFormalCharge(Atom atom, IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.ChargeInsensitive || policy == IdentityPolicy.ChargeParent || IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.FormalCharge;
    }

    internal int SelectExplicitHydrogenCount(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.ExplicitHydrogenCount;
    }

    internal int SelectImplicitHydrogenCount(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.ImplicitHydrogenCount;
    }

    internal int SelectRadicalElectronCount(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.RadicalElectronCount;
    }

    internal int SelectAtomMapNumber(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return atom.AtomMapNumber;
    }

    internal int SelectAtomAromaticValue(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return BoolToInt(atom.IsAromatic);
    }

    internal int SelectAtomQueryOperationValue(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return (int)atom.QueryOperation;
    }

    internal string SelectAtomQueryValue(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return "";
        }

        return atom.QueryValue;
    }

    internal string SelectAtomAlias(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return "";
        }

        return atom.Alias;
    }

    internal string SelectAtomLabel(Atom atom, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return "";
        }

        return atom.Label;
    }

    internal int SelectBondOrderValue(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return (int)BondOrder.Single;
        }

        return (int)bond.Order;
    }

    internal int SelectBondAromaticValue(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return BoolToInt(bond.IsAromatic);
    }

    internal int SelectBondStereoValue(Bond bond, IdentityPolicy policy)
    {
        if (!IncludesStereo(policy))
        {
            return (int)BondStereo.None;
        }

        return (int)bond.Stereo;
    }

    internal int SelectBondDirectionValue(Bond bond, IdentityPolicy policy)
    {
        if (!IncludesStereo(policy))
        {
            return (int)BondDirection.None;
        }

        return (int)bond.Direction;
    }

    internal int SelectBondDativeValue(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return BoolToInt(bond.IsDative);
    }

    internal int SelectBondQueryOperationValue(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return 0;
        }

        return (int)bond.QueryOperation;
    }

    internal string SelectBondQueryValue(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return "";
        }

        return bond.QueryValue;
    }

    internal string SelectBondSourceFlags(Bond bond, IdentityPolicy policy)
    {
        if (IsConnectivityLike(policy))
        {
            return "";
        }

        return bond.SourceFlags;
    }

    internal void AppendInt(StringBuilder builder, int value)
    {
        builder.Append("|");
        builder.Append(value);
    }

    internal void AppendString(StringBuilder builder, string value)
    {
        builder.Append("|");
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
            else if (character == '|')
            {
                builder.Append("\\p");
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
}
