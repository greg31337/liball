using System.Text;

namespace csv;

/// <summary>
/// Opens CSV file streams and combines CSV files.
/// </summary>
public interface ICsvFileService
{
    /// <summary>Opens a UTF-8 CSV reader with byte-order-mark detection.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    CsvReader OpenReader(string path, CsvReaderOptions? options);

    /// <summary>Opens a UTF-8 CSV reader with byte-order-mark detection.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    CsvReader OpenReaderUtf8(string path, CsvReaderOptions? options);

    /// <summary>Opens an ASCII CSV reader.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <returns>The opened reader.</returns>
    CsvReader OpenReaderAscii(string path, CsvReaderOptions? options);

    /// <summary>Opens a CSV reader using an explicit encoding.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Reader options; null uses defaults.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <returns>The opened reader.</returns>
    CsvReader OpenReaderWithEncoding(string path, CsvReaderOptions? options, Encoding encoding);

    /// <summary>Opens a UTF-8 CSV writer, honoring the append option.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenWriter(string path, CsvWriterOptions? options);

    /// <summary>Opens a replacing UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenWriterUtf8(string path, CsvWriterOptions? options);

    /// <summary>Opens a replacing ASCII CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenWriterAscii(string path, CsvWriterOptions? options);

    /// <summary>Opens an appending UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenAppendWriter(string path, CsvWriterOptions? options);

    /// <summary>Opens an appending UTF-8 CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenAppendWriterUtf8(string path, CsvWriterOptions? options);

    /// <summary>Opens an appending ASCII CSV writer.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenAppendWriterAscii(string path, CsvWriterOptions? options);

    /// <summary>Opens a CSV writer using an explicit encoding and append mode.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Writer options; null uses defaults.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="append">Whether to append to the file.</param>
    /// <returns>The opened writer.</returns>
    CsvWriter OpenWriterWithEncoding(string path, CsvWriterOptions? options, Encoding encoding, bool append);

    /// <summary>Concatenates files by field position into an output file.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <param name="writerOptions">Writer options.</param>
    /// <returns>The number of data rows written.</returns>
    long ConcatenateFilesByPosition(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions);

    /// <summary>Concatenates files by field position into a row writer.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="writer">The destination row writer.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <returns>The number of data rows written.</returns>
    long ConcatenateToWriterByPosition(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions);

    /// <summary>Unions files by header name into an output file.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <param name="writerOptions">Writer options.</param>
    /// <returns>The number of data rows written.</returns>
    long UnionFilesByName(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions);

    /// <summary>Unions files by header name into a row writer.</summary>
    /// <param name="inputPaths">Input file paths.</param>
    /// <param name="writer">The destination row writer.</param>
    /// <param name="readerOptions">Reader options.</param>
    /// <returns>The number of data rows written.</returns>
    long UnionToWriterByName(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions);
}
