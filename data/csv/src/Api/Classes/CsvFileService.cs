using System.Text;

namespace csv;

/// <summary>
/// Provides instance-based CSV file operations.
/// </summary>
public class CsvFileService : ICsvFileService
{
    /// <summary>Initializes a CSV file service.</summary>
    public CsvFileService()
    {
    }

    /// <summary>Opens a UTF-8 CSV reader with byte-order-mark detection.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    public CsvReader OpenReader(string path, CsvReaderOptions? options)
    {
        return CsvFile.OpenReader(path, options);
    }

    /// <summary>Opens a UTF-8 CSV reader with byte-order-mark detection.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    public CsvReader OpenReaderUtf8(string path, CsvReaderOptions? options)
    {
        return CsvFile.OpenReaderUtf8(path, options);
    }

    /// <summary>Opens an ASCII CSV reader.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    public CsvReader OpenReaderAscii(string path, CsvReaderOptions? options)
    {
        return CsvFile.OpenReaderAscii(path, options);
    }

    /// <summary>Opens a CSV reader using an explicit encoding.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <returns>The opened reader.</returns>
    public CsvReader OpenReaderWithEncoding(string path, CsvReaderOptions? options, Encoding encoding)
    {
        return CsvFile.OpenReaderWithEncoding(path, options, encoding);
    }

    /// <summary>Opens a UTF-8 CSV writer, honoring the append option.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenWriter(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenWriter(path, options);
    }

    /// <summary>Opens a replacing UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenWriterUtf8(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenWriterUtf8(path, options);
    }

    /// <summary>Opens a replacing ASCII CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenWriterAscii(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenWriterAscii(path, options);
    }

    /// <summary>Opens an appending UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenAppendWriter(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenAppendWriter(path, options);
    }

    /// <summary>Opens an appending UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenAppendWriterUtf8(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenAppendWriterUtf8(path, options);
    }

    /// <summary>Opens an appending ASCII CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenAppendWriterAscii(string path, CsvWriterOptions? options)
    {
        return CsvFile.OpenAppendWriterAscii(path, options);
    }

    /// <summary>Opens a CSV writer using an explicit encoding and append mode.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="append">Whether to append to the file.</param>
    /// <returns>The opened writer.</returns>
    public CsvWriter OpenWriterWithEncoding(string path, CsvWriterOptions? options, Encoding encoding, bool append)
    {
        return CsvFile.OpenWriterWithEncoding(path, options, encoding, append);
    }

    /// <summary>Concatenates files by field position into an output file.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <param name="writerOptions">Writer options.</param>
    /// <returns>The number of data rows written.</returns>
    public long ConcatenateFilesByPosition(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions)
    {
        return CsvFile.ConcatenateByPosition(inputPaths, outputPath, readerOptions, writerOptions);
    }

    /// <summary>Concatenates files by field position into a row writer.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="writer">The destination row writer.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <returns>The number of data rows written.</returns>
    public long ConcatenateToWriterByPosition(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions)
    {
        return CsvFile.ConcatenateByPosition(inputPaths, writer, readerOptions);
    }

    /// <summary>Unions files by header name into an output file.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <param name="writerOptions">Writer options.</param>
    /// <returns>The number of data rows written.</returns>
    public long UnionFilesByName(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions)
    {
        return CsvFile.UnionByName(inputPaths, outputPath, readerOptions, writerOptions);
    }

    /// <summary>Unions files by header name into a row writer.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="writer">The destination row writer.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <returns>The number of data rows written.</returns>
    public long UnionToWriterByName(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions)
    {
        return CsvFile.UnionByName(inputPaths, writer, readerOptions);
    }
}
