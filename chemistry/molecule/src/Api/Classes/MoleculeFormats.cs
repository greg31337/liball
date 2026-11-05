using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Provides the main entry point for detecting, reading, and writing supported molecule formats.
/// </summary>
public class MoleculeFormats : IMoleculeFormatService, IMoleculeFormats
{
    internal FormatDetection Detection;
    internal MoleculeCodecRegistry Codecs;
    internal SmilesParser BuiltInSmilesReader;
    internal SmilesWriter BuiltInSmilesWriter;
    internal MolfileReader BuiltInMolfileReader;
    internal MolfileWriter BuiltInMolfileWriter;
    internal V3000MoleculeWriter BuiltInV3000Writer;
    internal SdfReader BuiltInSdfReader;
    internal SdfWriter BuiltInSdfWriter;
    internal DelimitedVendorWriter BuiltInVendorWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeFormats"/> class.
    /// </summary>
    public MoleculeFormats()
    {
        Detection = new FormatDetection();
        Codecs = new MoleculeCodecRegistry();
        BuiltInSmilesReader = new SmilesParser();
        BuiltInSmilesWriter = new SmilesWriter();
        BuiltInMolfileReader = new MolfileReader();
        BuiltInMolfileWriter = new MolfileWriter();
        BuiltInV3000Writer = new V3000MoleculeWriter(BuiltInMolfileWriter);
        BuiltInSdfReader = new SdfReader();
        BuiltInSdfWriter = new SdfWriter();
        BuiltInVendorWriter = new DelimitedVendorWriter();
        RegisterBuiltInCodecs();
    }

    internal void RegisterBuiltInCodecs()
    {
        InChIWorkflows inChIWorkflows = new InChIWorkflows();

        RegisterReader(FormatKind.Smiles, BuiltInSmilesReader);
        RegisterWriter(FormatKind.Smiles, BuiltInSmilesWriter);
        RegisterReader(FormatKind.MolV2000, BuiltInMolfileReader);
        RegisterWriter(FormatKind.MolV2000, BuiltInMolfileWriter);
        RegisterReader(FormatKind.MolV3000, BuiltInMolfileReader);
        RegisterWriter(FormatKind.MolV3000, BuiltInV3000Writer);
        RegisterReader(FormatKind.Sdf, BuiltInSdfReader);
        RegisterWriter(FormatKind.Sdf, BuiltInSdfWriter);
        RegisterReader(FormatKind.Mol2, new Mol2Reader());
        RegisterWriter(FormatKind.Mol2, new Mol2Writer());
        RegisterReader(FormatKind.Pdb, new PdbReader());
        RegisterWriter(FormatKind.Pdb, new PdbWriter());
        RegisterReader(FormatKind.InChI, new InChIMoleculeReader(inChIWorkflows));
        RegisterWriter(FormatKind.InChI, new InChIMoleculeWriter(inChIWorkflows));
        RegisterReader(FormatKind.InChIKey, new InChIKeyMoleculeReader(inChIWorkflows));
        RegisterWriter(FormatKind.InChIKey, new InChIKeyMoleculeWriter(inChIWorkflows));
        RegisterReader(FormatKind.VendorDelimited, new DelimitedVendorReader());
        RegisterWriter(FormatKind.VendorDelimited, BuiltInVendorWriter);
        DeterministicSerializer serializer = new DeterministicSerializer();
        RegisterReader(FormatKind.DeterministicText, serializer);
        RegisterWriter(FormatKind.DeterministicText, serializer);
    }

    /// <summary>
    /// Registers or replaces the molecular graph reader for a format kind.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <param name="reader">The reader implementation.</param>
    public void RegisterReader(FormatKind kind, IMoleculeReader reader)
    {
        Codecs.RegisterReader(kind, reader);
    }

    /// <summary>
    /// Registers or replaces the molecular graph writer for a format kind.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <param name="writer">The writer implementation.</param>
    public void RegisterWriter(FormatKind kind, IMoleculeWriter writer)
    {
        Codecs.RegisterWriter(kind, writer);
    }

    /// <summary>
    /// Determines whether a molecular graph reader is registered for the format.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <returns><c>true</c> when a reader is registered; otherwise, <c>false</c>.</returns>
    public bool CanReadMolecule(FormatKind kind)
    {
        return Codecs.GetReader(kind) != null;
    }

    /// <summary>
    /// Determines whether a molecular graph writer is registered for the format.
    /// </summary>
    /// <param name="kind">The format kind.</param>
    /// <returns><c>true</c> when a writer is registered; otherwise, <c>false</c>.</returns>
    public bool CanWriteMolecule(FormatKind kind)
    {
        return Codecs.GetWriter(kind) != null;
    }

    /// <summary>
    /// Detects a supported molecule format from a caller hint, file name, or characteristic content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public FormatDetectionResult Detect(string? text, string? fileName)
    {
        return Detection.Detect(text, fileName);
    }

    /// <summary>
    /// Opens the reader.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule stream reader.</returns>
    public MoleculeStreamReader OpenReader(TextReader? reader, FormatOptions? options)
    {
        return MoleculeStreamReader.CreateWithFormats(reader, options, this);
    }

    /// <summary>
    /// Opens the writer.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule stream writer.</returns>
    public MoleculeStreamWriter OpenWriter(TextWriter? writer, FormatOptions? options)
    {
        return MoleculeStreamWriter.CreateWithFormats(writer, options, this);
    }

    /// <summary>
    /// Reads one molecule after selecting a format from the supplied hint, file name, and content.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule ReadString(string? text, FormatOptions? options)
    {
        FormatOptions safeOptions = NormalizeOptions(options);
        FormatDetectionResult detection = Detection.DetectWithOptions(text, safeOptions);
        IMoleculeReader? reader = Codecs.GetReader(detection.Kind);
        if (detection.Kind == FormatKind.Smiles && reader == BuiltInSmilesReader)
        {
            return BuiltInSmilesReader.ParseSmilesWithOptions(text, safeOptions.SmilesRead).Molecule;
        }

        if ((detection.Kind == FormatKind.MolV2000 || detection.Kind == FormatKind.MolV3000) && reader == BuiltInMolfileReader)
        {
            return BuiltInMolfileReader.ReadMolfileWithOptions(text, safeOptions.MolfileRead).Molecule;
        }

        if (detection.Kind == FormatKind.Sdf && reader == BuiltInSdfReader)
        {
            SdfReadResult result = BuiltInSdfReader.ReadSdfWithOptions(text, safeOptions.SdfRead);
            if (result.Records.Count > 0)
            {
                return result.Records[0].Molecule;
            }

            return new Molecule();
        }

        return ReadStringAs(text, detection.Kind);
    }

    /// <summary>
    /// Reads one molecule using the explicitly selected format.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule ReadStringAs(string? text, FormatKind kind)
    {
        IMoleculeReader? reader = Codecs.GetReader(kind);
        if (reader != null)
        {
            return reader.Read(text);
        }

        return new Molecule();
    }

    /// <summary>
    /// Writes one molecule using the format selected by the supplied options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteString(Molecule? molecule, FormatOptions? options)
    {
        FormatOptions safeOptions = NormalizeOptions(options);
        FormatKind kind = ResolveWriteKind(safeOptions);
        IMoleculeWriter? writer = Codecs.GetWriter(kind);
        if (kind == FormatKind.Smiles && writer == BuiltInSmilesWriter)
        {
            return BuiltInSmilesWriter.WriteSmilesWithOptions(molecule, safeOptions.SmilesWrite);
        }

        if (kind == FormatKind.MolV2000 && writer == BuiltInMolfileWriter)
        {
            MolfileWriteOptions molfileOptions = CopyMolfileWriteOptions(safeOptions.MolfileWrite);
            molfileOptions.Version = MolfileVersion.V2000;
            return BuiltInMolfileWriter.WriteMolfileWithOptions(molecule, molfileOptions).Text;
        }

        if (kind == FormatKind.MolV3000 && writer == BuiltInV3000Writer)
        {
            MolfileWriteOptions molfileOptions = CopyMolfileWriteOptions(safeOptions.MolfileWrite);
            molfileOptions.Version = MolfileVersion.V3000;
            return BuiltInMolfileWriter.WriteMolfileWithOptions(molecule, molfileOptions).Text;
        }

        if (kind == FormatKind.Sdf && writer == BuiltInSdfWriter)
        {
            if (molecule == null)
            {
                return "";
            }

            SdfRecord record = new SdfRecord();
            record.Molecule = molecule;

            List<SdfRecord> records = new List<SdfRecord>();
            records.Add(record);
            return BuiltInSdfWriter.WriteSdfWithOptions(records, safeOptions.SdfWrite).Text;
        }

        if (kind == FormatKind.VendorDelimited && writer == BuiltInVendorWriter)
        {
            return BuiltInVendorWriter.WriteDelimitedVendor(molecule, safeOptions.VendorWrite);
        }

        return WriteStringAs(molecule, kind);
    }

    /// <summary>
    /// Writes one molecule using the explicitly selected format.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteStringAs(Molecule? molecule, FormatKind kind)
    {
        IMoleculeWriter? writer = Codecs.GetWriter(kind);
        if (writer != null)
        {
            return writer.Write(molecule);
        }

        return "";
    }

    internal FormatKind ResolveWriteKind(FormatOptions? options)
    {
        FormatOptions safeOptions = NormalizeOptions(options);
        if (safeOptions.KindHint != FormatKind.Unknown)
        {
            return safeOptions.KindHint;
        }

        FormatDetectionResult detection = Detection.Detect("", safeOptions.FileName);
        if (detection.Kind != FormatKind.Unknown)
        {
            return detection.Kind;
        }

        return FormatKind.Smiles;
    }

    internal FormatOptions NormalizeOptions(FormatOptions? options)
    {
        if (options == null)
        {
            return new FormatOptions();
        }

        return options;
    }

    internal MolfileWriteOptions CopyMolfileWriteOptions(MolfileWriteOptions? options)
    {
        MolfileWriteOptions copy = new MolfileWriteOptions();
        if (options != null)
        {
            copy.Version = options.Version;
            copy.WriteAtomLists = options.WriteAtomLists;
            copy.WriteAliases = options.WriteAliases;
            copy.WriteCommonProperties = options.WriteCommonProperties;
            copy.WriteEnhancedStereo = options.WriteEnhancedStereo;
            copy.V2000AtomLimit = options.V2000AtomLimit;
            copy.V2000BondLimit = options.V2000BondLimit;
        }

        return copy;
    }
}
