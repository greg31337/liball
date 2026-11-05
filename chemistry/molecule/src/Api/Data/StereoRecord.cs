using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Stores the parsed fields for one stereo record.
/// </summary>
public class StereoRecord
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The kind.
    /// </summary>
    public string Kind;
    /// <summary>
    /// The typed stereo form.
    /// </summary>
    public StereoKind Type;
    /// <summary>
    /// Whether this record intentionally stores a typed placeholder rather than a fully perceived stereo assignment.
    /// </summary>
    public bool IsPlaceholder;
    /// <summary>
    /// The center atom identifier.
    /// </summary>
    public int CenterAtomId;
    /// <summary>
    /// The ligand atom identifiers.
    /// </summary>
    public List<int> LigandAtomIds;
    /// <summary>
    /// The chirality.
    /// </summary>
    public Chirality Chirality;
    /// <summary>
    /// The stereo bond identifier.
    /// </summary>
    public int StereoBondId;
    /// <summary>
    /// The begin ligand atom identifier.
    /// </summary>
    public int BeginLigandAtomId;
    /// <summary>
    /// The end ligand atom identifier.
    /// </summary>
    public int EndLigandAtomId;
    /// <summary>
    /// The configuration.
    /// </summary>
    public DoubleBondConfiguration Configuration;
    /// <summary>
    /// The group identifier.
    /// </summary>
    public int GroupId;
    /// <summary>
    /// The source.
    /// </summary>
    public string Source;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereoRecord"/> class.
    /// </summary>
    public StereoRecord()
    {
        Id = -1;
        Kind = "";
        Type = StereoKind.Unknown;
        IsPlaceholder = false;
        CenterAtomId = -1;
        LigandAtomIds = new List<int>();
        Chirality = Chirality.None;
        StereoBondId = -1;
        BeginLigandAtomId = -1;
        EndLigandAtomId = -1;
        Configuration = DoubleBondConfiguration.None;
        GroupId = -1;
        Source = "";
    }

    /// <summary>
    /// Creates a stereo record with the supplied identity and kind.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>A stereo record with initialized identity and collection fields.</returns>
    internal static StereoRecord Create(int id, string? kind)
    {
        StereoRecord record = new StereoRecord();
        record.Id = id;
        if (kind == null)
        {
            record.Kind = "";
        }
        else
        {
            record.Kind = kind;
        }

        record.Type = record.ResolveStereoKind();

        return record;
    }

    /// <summary>
    /// Determines whether the value is tetrahedral.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsTetrahedral()
    {
        return Type == StereoKind.Tetrahedral || Kind == "tetrahedral";
    }

    /// <summary>
    /// Determines whether the value is double bond.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsDoubleBond()
    {
        return Type == StereoKind.DoubleBond || Kind == "double-bond";
    }

    internal bool IsExtendedStereo()
    {
        return Type == StereoKind.Allene
            || Type == StereoKind.Atropisomeric
            || Type == StereoKind.NonTetrahedral
            || Kind == "allene"
            || Kind == "atropisomeric"
            || Kind == "non-tetrahedral";
    }

    /// <summary>
    /// Sets the tetrahedral.
    /// </summary>
    /// <param name="centerAtomId">The center atom identifier.</param>
    /// <param name="ligand0AtomId">The ligand 0 atom identifier.</param>
    /// <param name="ligand1AtomId">The ligand 1 atom identifier.</param>
    /// <param name="ligand2AtomId">The ligand 2 atom identifier.</param>
    /// <param name="ligand3AtomId">The ligand 3 atom identifier.</param>
    /// <param name="chirality">The chirality.</param>
    internal void SetTetrahedral(int centerAtomId, int ligand0AtomId, int ligand1AtomId, int ligand2AtomId, int ligand3AtomId, Chirality chirality)
    {
        Kind = "tetrahedral";
        Type = StereoKind.Tetrahedral;
        IsPlaceholder = false;
        CenterAtomId = centerAtomId;
        LigandAtomIds.Clear();
        LigandAtomIds.Add(ligand0AtomId);
        LigandAtomIds.Add(ligand1AtomId);
        LigandAtomIds.Add(ligand2AtomId);
        LigandAtomIds.Add(ligand3AtomId);
        Chirality = chirality;
    }

    /// <summary>
    /// Sets the double bond.
    /// </summary>
    /// <param name="stereoBondId">The stereo bond identifier.</param>
    /// <param name="beginLigandAtomId">The begin ligand atom identifier.</param>
    /// <param name="endLigandAtomId">The end ligand atom identifier.</param>
    /// <param name="configuration">The configuration.</param>
    internal void SetDoubleBond(int stereoBondId, int beginLigandAtomId, int endLigandAtomId, DoubleBondConfiguration configuration)
    {
        Kind = "double-bond";
        Type = StereoKind.DoubleBond;
        IsPlaceholder = false;
        StereoBondId = stereoBondId;
        BeginLigandAtomId = beginLigandAtomId;
        EndLigandAtomId = endLigandAtomId;
        Configuration = configuration;
    }

    internal void SetExtendedStereoPlaceholder(StereoKind type, int centerAtomId, int stereoBondId, int[] carrierAtomIds)
    {
        Type = type;
        Kind = GetExtendedStereoKindName(type);
        IsPlaceholder = true;
        CenterAtomId = centerAtomId;
        StereoBondId = stereoBondId;
        Chirality = Chirality.Unspecified;
        Configuration = DoubleBondConfiguration.Unspecified;
        LigandAtomIds.Clear();
        int index = 0;
        while (index < carrierAtomIds.Length)
        {
            LigandAtomIds.Add(carrierAtomIds[index]);
            index = index + 1;
        }
    }

    internal StereoKind ResolveStereoKind()
    {
        if (Kind == "tetrahedral")
        {
            return StereoKind.Tetrahedral;
        }

        if (Kind == "double-bond")
        {
            return StereoKind.DoubleBond;
        }

        if (Kind == "allene")
        {
            return StereoKind.Allene;
        }

        if (Kind == "atropisomeric")
        {
            return StereoKind.Atropisomeric;
        }

        if (Kind == "non-tetrahedral")
        {
            return StereoKind.NonTetrahedral;
        }

        return StereoKind.Unknown;
    }

    internal string GetExtendedStereoKindName(StereoKind type)
    {
        if (type == StereoKind.Allene)
        {
            return "allene";
        }

        if (type == StereoKind.Atropisomeric)
        {
            return "atropisomeric";
        }

        if (type == StereoKind.NonTetrahedral)
        {
            return "non-tetrahedral";
        }

        return "";
    }

    /// <summary>
    /// Adds the ligand.
    /// </summary>
    /// <param name="atomId">The zero-based atom identifier.</param>
    internal void AddLigand(int atomId)
    {
        LigandAtomIds.Add(atomId);
    }

    /// <summary>
    /// Returns the ligand count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetLigandCount()
    {
        return LigandAtomIds.Count;
    }

    /// <summary>
    /// Returns the implicit ligand count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    internal int GetImplicitLigandCount()
    {
        int count = 0;
        int index = 0;
        while (index < LigandAtomIds.Count)
        {
            if (LigandAtomIds[index] < 0)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
