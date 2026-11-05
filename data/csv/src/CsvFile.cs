using System.IO;
using System.Text;

namespace csv;

/// <summary>
/// Opens CSV readers and writers for files and combines multiple CSV files.
/// </summary>
internal static class CsvFile
{
    /// <summary>
    /// Opens a UTF-8 file for forward-only CSV reading with byte-order-mark detection.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvReader OpenReader(string path, CsvReaderOptions? options)
    {
        CsvReader reader = OpenReaderUtf8(path, options);
        return reader;
    }

    /// <summary>
    /// Opens a UTF-8 file for forward-only CSV reading with byte-order-mark detection.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvReader OpenReaderUtf8(string path, CsvReaderOptions? options)
    {
        Encoding encoding = CreateUtf8Encoding();
        CsvReader reader = OpenReaderWithEncoding(path, options, encoding);
        return reader;
    }

    /// <summary>
    /// Opens an ASCII file for forward-only CSV reading.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvReader OpenReaderAscii(string path, CsvReaderOptions? options)
    {
        CsvReader reader = OpenReaderWithEncoding(path, options, Encoding.ASCII);
        return reader;
    }

    /// <summary>
    /// Opens a UTF-8 CSV writer, honoring the append option.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenWriter(string path, CsvWriterOptions? options)
    {
        CsvWriterOptions writerOptions = ResolveWriterOptions(options);
        if (writerOptions.Append)
        {
            CsvWriter appendWriter = OpenAppendWriterUtf8(path, writerOptions);
            return appendWriter;
        }

        CsvWriter writer = OpenWriterUtf8(path, writerOptions);
        return writer;
    }

    /// <summary>
    /// Writes files in order, preserving field positions and writing at most one header.
    /// </summary>
    /// <param name="inputPaths">Input paths in processing order.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Options shared by input readers.</param>
    /// <param name="writerOptions">Options for the output writer.</param>
    /// <returns>The number of data rows written.</returns>
    internal static long ConcatenateByPosition(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions)
    {
        CsvWriter writer = OpenWriter(outputPath, writerOptions);
        long count = ConcatenateByPosition(inputPaths, writer, readerOptions);
        writer.Close();
        return count;
    }

    /// <summary>
    /// Writes files in order while aligning fields to the first available header.
    /// </summary>
    /// <param name="inputPaths">Input paths in processing order.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="readerOptions">Options shared by input readers.</param>
    /// <param name="writerOptions">Options for the output writer.</param>
    /// <returns>The number of data rows written.</returns>
    internal static long UnionByName(string[] inputPaths, string outputPath, CsvReaderOptions? readerOptions, CsvWriterOptions? writerOptions)
    {
        CsvWriter writer = OpenWriter(outputPath, writerOptions);
        long count = UnionByName(inputPaths, writer, readerOptions);
        writer.Close();
        return count;
    }

    /// <summary>
    /// Writes files in order, preserving field positions and writing at most one header.
    /// </summary>
    /// <param name="inputPaths">Input paths in processing order.</param>
    /// <param name="writer">The destination CSV writer.</param>
    /// <param name="readerOptions">Options shared by input readers.</param>
    /// <returns>The number of data rows written.</returns>
    internal static long ConcatenateByPosition(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions)
    {
        CsvReaderOptions activeReaderOptions = ResolveReaderOptions(readerOptions);
        long count = 0;
        bool headerSeen = false;
        int index = 0;
        while (index < inputPaths.Length)
        {
            CsvReader reader = OpenReader(inputPaths[index], activeReaderOptions);
            headerSeen = ReadConcatenationHeader(reader, writer, headerSeen);
            while (reader.Read())
            {
                CsvRow row = reader.GetCurrentRow();
                writer.WriteRow(row);
                count = count + 1;
            }

            reader.Close();
            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Writes files in order while aligning fields to the first available header.
    /// </summary>
    /// <param name="inputPaths">Input paths in processing order.</param>
    /// <param name="writer">The destination CSV writer.</param>
    /// <param name="readerOptions">Options shared by input readers.</param>
    /// <returns>The number of data rows written.</returns>
    internal static long UnionByName(string[] inputPaths, ICsvRowWriter writer, CsvReaderOptions? readerOptions)
    {
        CsvReaderOptions activeReaderOptions = ResolveReaderOptions(readerOptions);
        long count = 0;
        bool headerSeen = false;
        CsvHeader outputHeader = new CsvHeader();
        int index = 0;
        while (index < inputPaths.Length)
        {
            CsvReader reader = OpenReader(inputPaths[index], activeReaderOptions);
            bool readHeader = reader.ReadHeader();
            if (readHeader)
            {
                if (!headerSeen)
                {
                    outputHeader = reader.GetHeaders().Clone();
                    if (writer.ShouldWriteHeader())
                    {
                        writer.WriteHeader(outputHeader);
                    }

                    headerSeen = true;
                }

                int[] columnMap = CreateUnionColumnMap(outputHeader, reader.GetHeaders());
                while (reader.Read())
                {
                    CsvRow row = CreateUnionRow(reader.GetCurrentRow(), outputHeader, columnMap);
                    writer.WriteRow(row);
                    count = count + 1;
                }
            }

            reader.Close();
            index = index + 1;
        }

        return count;
    }

    internal static bool ReadConcatenationHeader(ICsvRowReader reader, ICsvRowWriter writer, bool headerSeen)
    {
        if (!reader.ShouldReadHeader())
        {
            return headerSeen;
        }

        bool read = reader.ReadHeader();
        if (!read)
        {
            return headerSeen;
        }

        if (!headerSeen && writer.ShouldWriteHeader())
        {
            writer.WriteHeader(reader.GetHeaders());
        }

        return true;
    }

    internal static int[] CreateUnionColumnMap(CsvHeader outputHeader, CsvHeader inputHeader)
    {
        int[] indexes = new int[outputHeader.Names.Length];
        int index = 0;
        while (index < outputHeader.Names.Length)
        {
            indexes[index] = inputHeader.IndexOf(outputHeader.Names[index]);
            index = index + 1;
        }

        return indexes;
    }

    internal static CsvRow CreateUnionRow(CsvRow source, CsvHeader outputHeader, int[] columnMap)
    {
        string?[] fields = new string?[columnMap.Length];
        int index = 0;
        while (index < columnMap.Length)
        {
            fields[index] = source.GetField(columnMap[index]);
            index = index + 1;
        }

        CsvRow row = CsvRow.Create(fields, source.RowNumber);
        row.Line = source.Line;
        row.Column = source.Column;
        row.CharacterOffset = source.CharacterOffset;
        row.ByteOffset = source.ByteOffset;
        row.RawText = source.RawText;
        row.Header = outputHeader;
        row.Diagnostics = source.Diagnostics.Clone();
        return row;
    }

    /// <summary>
    /// Creates or replaces a UTF-8 CSV file without a byte-order mark.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenWriterUtf8(string path, CsvWriterOptions? options)
    {
        Encoding encoding = CreateUtf8Encoding();
        CsvWriter writer = OpenWriterWithEncoding(path, options, encoding, false);
        return writer;
    }

    /// <summary>
    /// Creates or replaces an ASCII CSV file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenWriterAscii(string path, CsvWriterOptions? options)
    {
        CsvWriter writer = OpenWriterWithEncoding(path, options, Encoding.ASCII, false);
        return writer;
    }

    /// <summary>
    /// Opens a UTF-8 CSV writer positioned to append records.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenAppendWriter(string path, CsvWriterOptions? options)
    {
        CsvWriter writer = OpenAppendWriterUtf8(path, options);
        return writer;
    }

    /// <summary>
    /// Opens a UTF-8 CSV writer positioned to append records without a byte-order mark.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenAppendWriterUtf8(string path, CsvWriterOptions? options)
    {
        Encoding encoding = CreateUtf8Encoding();
        CsvWriter writer = OpenWriterWithEncoding(path, options, encoding, true);
        return writer;
    }

    /// <summary>
    /// Opens an ASCII CSV writer positioned to append records.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenAppendWriterAscii(string path, CsvWriterOptions? options)
    {
        CsvWriter writer = OpenWriterWithEncoding(path, options, Encoding.ASCII, true);
        return writer;
    }

    /// <summary>
    /// Opens a file with the requested encoding while allowing a byte-order mark to override it.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvReader OpenReaderWithEncoding(string path, CsvReaderOptions? options, Encoding encoding)
    {
        FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader textReader = new StreamReader(stream, encoding, true);
        CsvReader reader = CsvReader.Create(textReader, options);
        return reader;
    }

    /// <summary>
    /// Opens a CSV file with the requested encoding for replacement or append.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <param name="encoding">The text encoding.</param>
    /// <param name="append">True to append; false to replace.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvWriter OpenWriterWithEncoding(string path, CsvWriterOptions? options, Encoding encoding, bool append)
    {
        CsvWriterOptions writerOptions = ResolveWriterOptions(options);
        FileMode mode = FileMode.Create;
        if (append)
        {
            mode = FileMode.Append;
        }

        FileStream stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read);
        StreamWriter textWriter = new StreamWriter(stream, encoding);
        if (append)
        {
            CsvWriter appendWriter = CsvWriter.CreateAppend(textWriter, writerOptions);
            return appendWriter;
        }

        CsvWriter writer = CsvWriter.Create(textWriter, writerOptions);
        return writer;
    }

    internal static CsvWriterOptions ResolveWriterOptions(CsvWriterOptions? options)
    {
        if (options == null)
        {
            CsvWriterOptions defaultOptions = CsvWriterOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal static CsvReaderOptions ResolveReaderOptions(CsvReaderOptions? options)
    {
        if (options == null)
        {
            CsvReaderOptions defaultOptions = CsvReaderOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal static Encoding CreateUtf8Encoding()
    {
        Encoding encoding = new UTF8Encoding(false);
        return encoding;
    }
}
