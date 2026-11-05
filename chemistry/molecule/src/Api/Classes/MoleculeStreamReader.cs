using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Reads molecule stream data into molecular objects.
/// </summary>
public class MoleculeStreamReader : IMoleculeStreamReader
{
    internal TextReader Reader = new StringReader("");
    internal FormatOptions Options = new FormatOptions();
    internal IMoleculeFormatService Formats = new MoleculeFormats();
    internal SdfReader SdfReader = new SdfReader();
    internal bool Ended;
    internal bool SingleRecordRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeStreamReader"/> class.
    /// </summary>
    public MoleculeStreamReader()
    {
        Reader = new StringReader("");
        Options = new FormatOptions();
        Formats = new MoleculeFormats();
        SdfReader = new SdfReader();
        Ended = true;
        SingleRecordRead = false;
    }

    /// <summary>
    /// Creates a reader using the default format service.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>A configured molecule stream reader.</returns>
    internal static MoleculeStreamReader Create(TextReader? reader, FormatOptions? options)
    {
        return CreateWithFormats(reader, options, new MoleculeFormats());
    }

    /// <summary>
    /// Creates a reader with an explicit format service.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <param name="formats">The format service used for detection and single-record reading.</param>
    /// <returns>A configured molecule stream reader.</returns>
    internal static MoleculeStreamReader CreateWithFormats(TextReader? reader, FormatOptions? options, IMoleculeFormatService? formats)
    {
        MoleculeStreamReader streamReader = new MoleculeStreamReader();
        streamReader.Initialize(reader, options, formats);
        return streamReader;
    }

    internal void Initialize(TextReader? reader, FormatOptions? options, IMoleculeFormatService? formats)
    {
        Reader = NormalizeReader(reader);
        Options = NormalizeOptions(options);
        Formats = NormalizeFormats(formats);
        SdfReader = new SdfReader();
        Ended = false;
        SingleRecordRead = false;
    }

    /// <summary>
    /// Open.
    /// </summary>
    /// <param name="reader">The text reader that supplies molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    public void Open(TextReader? reader, FormatOptions? options)
    {
        Reader = NormalizeReader(reader);
        Options = NormalizeOptions(options);
        Ended = false;
        SingleRecordRead = false;
    }

    /// <summary>
    /// Reads the next.
    /// </summary>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MoleculeStreamReadResult ReadNext()
    {
        MoleculeStreamReadResult result = new MoleculeStreamReadResult();
        if (Ended)
        {
            result.EndOfStream = true;
            return result;
        }

        FormatKind kind = ResolveKindForRead();
        if (kind == FormatKind.Sdf)
        {
            return ReadNextSdfRecord();
        }

        if (SingleRecordRead)
        {
            Ended = true;
            result.EndOfStream = true;
            return result;
        }

        string text = Reader.ReadToEnd();
        SingleRecordRead = true;
        Ended = true;
        result.Molecule = Formats.ReadString(text, Options);
        result.Success = true;
        return result;
    }

    internal MoleculeStreamReadResult ReadNextSdfRecord()
    {
        MoleculeStreamReadResult result = new MoleculeStreamReadResult();
        SdfRecord record = SdfReader.ReadSdfRecordWithOptions(Reader, Options.SdfRead);
        if (record.EndOfStream)
        {
            Ended = true;
            result.EndOfStream = true;
            return result;
        }

        result.Molecule = record.Molecule;
        result.Report = record.Report;
        result.Success = record.Success;
        result.EndOfStream = false;
        if (!record.Success && !Options.RecordStream.ContinueAfterError)
        {
            Ended = true;
        }

        return result;
    }

    /// <summary>
    /// Reads all available records and returns their molecules in input order.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> ReadAll()
    {
        List<Molecule> molecules = new List<Molecule>();
        bool done = false;
        while (!done)
        {
            MoleculeStreamReadResult result = ReadNext();
            if (result.EndOfStream)
            {
                done = true;
            }
            else
            {
                if (result.Success || Options.RecordStream.IncludeFailedRecords)
                {
                    molecules.Add(result.Molecule);
                }
            }
        }

        return molecules;
    }

    internal FormatKind ResolveKindForRead()
    {
        if (Options.KindHint != FormatKind.Unknown)
        {
            return Options.KindHint;
        }

        FormatDetectionResult detection = Formats.Detect("", Options.FileName);
        if (detection.Kind != FormatKind.Unknown)
        {
            return detection.Kind;
        }

        return FormatKind.Unknown;
    }

    internal TextReader NormalizeReader(TextReader? reader)
    {
        if (reader == null)
        {
            return new StringReader("");
        }

        return reader;
    }

    internal FormatOptions NormalizeOptions(FormatOptions? options)
    {
        if (options == null)
        {
            return new FormatOptions();
        }

        return options;
    }

    internal IMoleculeFormatService NormalizeFormats(IMoleculeFormatService? formats)
    {
        if (formats == null)
        {
            return new MoleculeFormats();
        }

        return formats;
    }
}
