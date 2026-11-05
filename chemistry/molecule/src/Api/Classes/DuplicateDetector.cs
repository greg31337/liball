using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Tracks structural identity keys and reports whether each added molecule was seen earlier.
/// </summary>
public class DuplicateDetector : IDuplicateDetector
{
    internal IdentityOptions Options = new IdentityOptions();
    internal Canonicalizer Canonicalizer = new Canonicalizer();
    internal ParentMoleculeFactory ParentFactory = new ParentMoleculeFactory();
    internal IMoleculeFormatService Formats = new MoleculeFormats();
    internal SdfReader SdfReader = new SdfReader();
    internal List<DuplicateEntry> Entries = new List<DuplicateEntry>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateDetector"/> class.
    /// </summary>
    public DuplicateDetector()
    {
        Options = new IdentityOptions();
        Canonicalizer = new Canonicalizer();
        ParentFactory = new ParentMoleculeFactory();
        Formats = new MoleculeFormats();
        SdfReader = new SdfReader();
        Entries = new List<DuplicateEntry>();
    }

    /// <summary>
    /// Configures the detector with explicit identity options and clears previously tracked entries.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    public void Configure(IdentityOptions? options)
    {
        Initialize(options, new MoleculeFormats());
    }

    /// <summary>
    /// Configures the detector with explicit identity options and a format service, then clears previously tracked entries.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    /// <param name="formats">The format service used by text ingestion.</param>
    public void ConfigureWithFormats(IdentityOptions? options, IMoleculeFormatService? formats)
    {
        Initialize(options, formats);
    }

    /// <summary>
    /// Creates a duplicate detector with explicit identity options.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>A configured duplicate detector.</returns>
    internal static DuplicateDetector Create(IdentityOptions? options)
    {
        return CreateWithFormats(options, new MoleculeFormats());
    }

    /// <summary>
    /// Creates a duplicate detector with explicit identity options and format service.
    /// </summary>
    /// <param name="options">The options controlling the operation.</param>
    /// <param name="formats">The format service used by text ingestion.</param>
    /// <returns>A configured duplicate detector.</returns>
    internal static DuplicateDetector CreateWithFormats(IdentityOptions? options, IMoleculeFormatService? formats)
    {
        DuplicateDetector detector = new DuplicateDetector();
        detector.Initialize(options, formats);
        return detector;
    }

    internal void Initialize(IdentityOptions? options, IMoleculeFormatService? formats)
    {
        Options = NormalizeOptions(options);
        Canonicalizer = new Canonicalizer();
        ParentFactory = new ParentMoleculeFactory();
        if (formats == null)
        {
            Formats = new MoleculeFormats();
        }
        else
        {
            Formats = formats;
        }
        SdfReader = new SdfReader();
        Entries = new List<DuplicateEntry>();
    }

    /// <summary>
    /// Adds the supplied value to this collection.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public DuplicateResult Add(Molecule? molecule)
    {
        DuplicateResult result = new DuplicateResult();
        result.InputIndex = Entries.Count;
        result.Policy = Options.Policy;
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("DUPLICATE_DETECTOR_NULL_MOLECULE", "Cannot add a null molecule to duplicate detector.");
            return result;
        }

        Molecule prepared = PrepareMolecule(molecule);
        string key = Canonicalizer.GetIdentityHashWithOptions(prepared, CreateDuplicateHashOptions());
        int firstIndex = FindFirstIndexByKey(key);
        result.Key = key;
        result.Molecule = MoleculeBuilder.CloneMolecule(prepared);

        if (firstIndex >= 0)
        {
            result.IsDuplicate = true;
            result.FirstIndex = firstIndex;
        }
        else
        {
            result.IsDuplicate = false;
            result.FirstIndex = result.InputIndex;
        }

        DuplicateEntry entry = new DuplicateEntry();
        entry.InputIndex = result.InputIndex;
        entry.FirstIndex = result.FirstIndex;
        entry.Key = key;
        entry.IsDuplicate = result.IsDuplicate;
        if (Options.StoreMolecules)
        {
            entry.Molecule = MoleculeBuilder.CloneMolecule(prepared);
        }

        Entries.Add(entry);
        return result;
    }

    /// <summary>
    /// Adds the string.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="formatOptions">The options used to detect and read the input format.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public DuplicateResult AddString(string? text, FormatOptions? formatOptions)
    {
        Molecule molecule = Formats.ReadString(text, NormalizeFormatOptions(formatOptions));
        return Add(molecule);
    }

    /// <summary>
    /// Adds the SDF record.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public DuplicateResult AddSdfRecord(SdfRecord? record)
    {
        DuplicateResult result = new DuplicateResult();
        result.InputIndex = Entries.Count;
        result.Policy = Options.Policy;
        if (record == null)
        {
            result.Success = false;
            result.Report.AddError("DUPLICATE_DETECTOR_NULL_SDF_RECORD", "Cannot add a null SDF record to duplicate detector.");
            return result;
        }

        result.RecordIndex = record.Index;
        if (!record.Success)
        {
            result.Success = false;
            CopyReport(record.Report, result.Report);
            if (result.Report.GetErrorCount() == 0)
            {
                result.Report.AddError("DUPLICATE_DETECTOR_INVALID_SDF_RECORD", "Cannot add an invalid SDF record to duplicate detector.");
            }

            return result;
        }

        result = Add(record.Molecule);
        result.RecordIndex = record.Index;
        return result;
    }

    /// <summary>
    /// Adds the many.
    /// </summary>
    /// <param name="molecules">The molecules.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<DuplicateResult> AddMany(List<Molecule>? molecules)
    {
        List<DuplicateResult> results = new List<DuplicateResult>();
        if (molecules == null)
        {
            return results;
        }

        int index = 0;
        while (index < molecules.Count)
        {
            results.Add(Add(molecules[index]));
            index = index + 1;
        }

        return results;
    }

    /// <summary>
    /// Adds the many.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<DuplicateResult> AddManyFromReader(MoleculeStreamReader? reader)
    {
        List<DuplicateResult> results = new List<DuplicateResult>();
        if (reader == null)
        {
            return results;
        }

        int recordIndex = 0;
        bool done = false;
        while (!done)
        {
            MoleculeStreamReadResult readResult = reader.ReadNext();
            if (readResult.EndOfStream)
            {
                done = true;
            }
            else
            {
                DuplicateResult duplicateResult = AddStreamReadResult(readResult, recordIndex);
                results.Add(duplicateResult);
                recordIndex = recordIndex + 1;
            }
        }

        return results;
    }

    internal DuplicateResult AddStreamReadResult(MoleculeStreamReadResult? readResult, int recordIndex)
    {
        DuplicateResult result = new DuplicateResult();
        result.InputIndex = Entries.Count;
        result.RecordIndex = recordIndex;
        result.Policy = Options.Policy;
        if (readResult == null)
        {
            result.Success = false;
            result.Report.AddError("DUPLICATE_DETECTOR_NULL_STREAM_RECORD", "Cannot add a null stream record to duplicate detector.");
            return result;
        }

        if (!readResult.Success)
        {
            result.Success = false;
            CopyReport(readResult.Report, result.Report);
            if (result.Report.GetErrorCount() == 0)
            {
                result.Report.AddError("DUPLICATE_DETECTOR_INVALID_STREAM_RECORD", "Cannot add an invalid stream record to duplicate detector.");
            }

            return result;
        }

        result = Add(readResult.Molecule);
        result.RecordIndex = recordIndex;
        return result;
    }

    /// <summary>
    /// Adds the SDF records.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<DuplicateResult> AddSdfRecords(List<SdfRecord>? records)
    {
        List<DuplicateResult> results = new List<DuplicateResult>();
        if (records == null)
        {
            return results;
        }

        int index = 0;
        while (index < records.Count)
        {
            results.Add(AddSdfRecord(records[index]));
            index = index + 1;
        }

        return results;
    }

    /// <summary>
    /// Adds the SDF text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<DuplicateResult> AddSdfText(string? text)
    {
        List<SdfRecord> records = SdfReader.ReadRecords(text);
        return AddSdfRecords(records);
    }

    /// <summary>
    /// Determines whether the value is duplicate.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsDuplicate(Molecule? molecule)
    {
        DuplicateResult result = Add(molecule);
        return result.IsDuplicate;
    }

    /// <summary>
    /// Returns the entry count.
    /// </summary>
    /// <returns>The resulting count or integer value.</returns>
    public int GetEntryCount()
    {
        return Entries.Count;
    }

    /// <summary>
    /// Clears all identity keys and molecules retained by the duplicate detector.
    /// </summary>
    public void Reset()
    {
        Entries.Clear();
    }

    /// <summary>
    /// Returns the duplicate groups.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<DuplicateGroup> GetDuplicateGroups()
    {
        List<DuplicateGroup> groups = new List<DuplicateGroup>();
        int index = 0;
        while (index < Entries.Count)
        {
            DuplicateEntry entry = Entries[index];
            if (entry.IsDuplicate)
            {
                DuplicateGroup group = FindOrCreateGroup(groups, entry.Key, entry.FirstIndex);
                group.DuplicateIndices.Add(entry.InputIndex);
            }

            index = index + 1;
        }

        return groups;
    }

    internal DuplicateGroup FindOrCreateGroup(List<DuplicateGroup> groups, string key, int firstIndex)
    {
        int index = 0;
        while (index < groups.Count)
        {
            if (groups[index].Key == key)
            {
                return groups[index];
            }

            index = index + 1;
        }

        DuplicateGroup group = new DuplicateGroup();
        group.Key = key;
        group.FirstIndex = firstIndex;
        groups.Add(group);
        return group;
    }

    internal int FindFirstIndexByKey(string key)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Key == key)
            {
                return Entries[index].FirstIndex;
            }

            index = index + 1;
        }

        return -1;
    }

    internal Molecule PrepareMolecule(Molecule molecule)
    {
        if (UsesParentMolecule(Options.Policy))
        {
            return ParentFactory.CreateParent(molecule, Options.Policy);
        }

        return MoleculeBuilder.CloneMolecule(molecule);
    }

    internal bool UsesParentMolecule(IdentityPolicy policy)
    {
        if (policy == IdentityPolicy.FragmentParent || policy == IdentityPolicy.ChargeParent || policy == IdentityPolicy.StereoParent || policy == IdentityPolicy.IsotopeParent || policy == IdentityPolicy.TautomerParent || policy == IdentityPolicy.SuperParent)
        {
            return true;
        }

        return false;
    }

    internal IdentityPolicy GetHashPolicy(IdentityPolicy policy)
    {
        if (UsesParentMolecule(policy))
        {
            return IdentityPolicy.Exact;
        }

        return policy;
    }

    internal IdentityOptions NormalizeOptions(IdentityOptions? options)
    {
        IdentityOptions normalized = new IdentityOptions();
        if (options != null)
        {
            normalized.Policy = options.Policy;
            normalized.StoreMolecules = options.StoreMolecules;
            normalized.IncludeIsotopes = options.IncludeIsotopes;
            normalized.IncludeCharges = options.IncludeCharges;
            normalized.IncludeHydrogens = options.IncludeHydrogens;
            normalized.IncludeAtomMaps = options.IncludeAtomMaps;
            normalized.IncludeAromaticity = options.IncludeAromaticity;
            normalized.IncludeStereo = options.IncludeStereo;
        }

        return normalized;
    }

    internal IdentityOptions CreateDuplicateHashOptions()
    {
        IdentityOptions hashOptions = NormalizeOptions(Options);
        hashOptions.Policy = GetHashPolicy(Options.Policy);
        return hashOptions;
    }

    internal FormatOptions NormalizeFormatOptions(FormatOptions? options)
    {
        if (options == null)
        {
            return new FormatOptions();
        }

        return options;
    }

    internal void CopyReport(MoleculeReport? source, MoleculeReport? target)
    {
        if (source == null || target == null)
        {
            return;
        }

        int index = 0;
        while (index < source.GetCount())
        {
            target.Add(source.Get(index));
            index = index + 1;
        }
    }
}
