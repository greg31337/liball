using System.IO;

namespace molecule;


/// <summary>
/// Writes molecular objects as molecule stream data.
/// </summary>
public class MoleculeStreamWriter : IMoleculeStreamWriter
{
    internal TextWriter Writer = new StringWriter();
    internal FormatOptions Options = new FormatOptions();
    internal IMoleculeFormatService Formats = new MoleculeFormats();
    internal int WrittenCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeStreamWriter"/> class.
    /// </summary>
    public MoleculeStreamWriter()
    {
        Writer = new StringWriter();
        Options = new FormatOptions();
        Formats = new MoleculeFormats();
        WrittenCount = 0;
    }

    /// <summary>
    /// Creates a writer using the default format service.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>A configured molecule stream writer.</returns>
    internal static MoleculeStreamWriter Create(TextWriter? writer, FormatOptions? options)
    {
        return CreateWithFormats(writer, options, new MoleculeFormats());
    }

    /// <summary>
    /// Creates a writer with an explicit format service.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <param name="formats">The format service used for detection and single-record writing.</param>
    /// <returns>A configured molecule stream writer.</returns>
    internal static MoleculeStreamWriter CreateWithFormats(TextWriter? writer, FormatOptions? options, IMoleculeFormatService? formats)
    {
        MoleculeStreamWriter streamWriter = new MoleculeStreamWriter();
        streamWriter.Initialize(writer, options, formats);
        return streamWriter;
    }

    internal void Initialize(TextWriter? writer, FormatOptions? options, IMoleculeFormatService? formats)
    {
        Writer = NormalizeWriter(writer);
        Options = NormalizeOptions(options);
        Formats = NormalizeFormats(formats);
        WrittenCount = 0;
    }

    /// <summary>
    /// Open.
    /// </summary>
    /// <param name="writer">The text writer that receives molecule data.</param>
    /// <param name="options">The options controlling the operation.</param>
    public void Open(TextWriter? writer, FormatOptions? options)
    {
        Writer = NormalizeWriter(writer);
        Options = NormalizeOptions(options);
        WrittenCount = 0;
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    public void Write(Molecule? molecule)
    {
        FormatKind kind = ResolveKindForWrite();
        Writer.Write(Formats.WriteString(molecule, Options));
        if (kind == FormatKind.Smiles)
        {
            Writer.Write('\n');
        }

        if (Options.RecordStream.FlushAfterRecord)
        {
            Writer.Flush();
        }

        WrittenCount = WrittenCount + 1;
    }

    internal FormatKind ResolveKindForWrite()
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

        return FormatKind.Smiles;
    }

    internal TextWriter NormalizeWriter(TextWriter? writer)
    {
        if (writer == null)
        {
            return new StringWriter();
        }

        return writer;
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
