using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvLargeStreamingTests
{
    [Test]
    public void CsvReader_ReadsManyRowsWithSmallBufferAcrossBoundaries()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.BufferSize = 7;
        CsvReader reader = CsvReader.Create(new StringReader(CreateLargeCsv(1000)), options);
        int count = 0;
        string? lastId = "";
        string? lastValue = "";

        while (reader.Read())
        {
            count = count + 1;
            lastId = reader.GetField(0);
            lastValue = reader.GetField(1);
        }

        Assert.That(count, Is.EqualTo(1000));
        Assert.That(lastId, Is.EqualTo("999"));
        Assert.That(lastValue, Is.EqualTo("value999"));
        Assert.That(reader.Buffer.Buffer.Length, Is.EqualTo(7));
    }

    [Test]
    public void CsvProcessor_ProcessesManyRowsWithProgressCallbacks()
    {
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.BufferSize = 9;
        CsvReader reader = CsvReader.Create(new StringReader(CreateLargeCsv(500)), readerOptions);
        StringWriter output = new StringWriter();
        CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());
        CountingProgressHandler progressHandler = new CountingProgressHandler();
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.HasProgressHandler = true;
        pipeline.ProgressHandler = progressHandler.CreateHandler();

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(500));
        Assert.That(progressHandler.Count, Is.EqualTo(500));
        Assert.That(progressHandler.LastRowsRead, Is.EqualTo(500));
        Assert.That(progressHandler.LastRowsWritten, Is.EqualTo(500));
        Assert.That(progressHandler.LastValue, Is.EqualTo("499"));
        Assert.That(reader.Buffer.Buffer.Length, Is.EqualTo(9));
    }

    [Test]
    public void LoadTable_MaxRowsStopsBeforeConsumingEntireLargeInput()
    {
        string text = CreateLargeCsv(200);
        TrackingChunkedTextReader textReader = new TrackingChunkedTextReader(text);
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.BufferSize = 8;
        CsvReader reader = CsvReader.Create(textReader, readerOptions);
        CsvTableOptions tableOptions = CsvTableOptions.CreateDefault();
        tableOptions.IncludeHeader = false;
        tableOptions.MaxRows = 3;

        CsvTable table = CsvProcessor.LoadTable(reader, tableOptions);

        Assert.That(table.CountRows(), Is.EqualTo(3));
        Assert.That(table.GetRow(0).Fields, Is.EqualTo(new string[] { "0", "value0" }));
        Assert.That(table.GetRow(2).Fields, Is.EqualTo(new string[] { "2", "value2" }));
        Assert.That(textReader.TotalCharactersRead, Is.LessThan(text.Length));
        Assert.That(textReader.MaxRequestedCount, Is.EqualTo(8));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("3"));
    }

    public static string CreateLargeCsv(int rows)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < rows)
        {
            builder.Append(index.ToString());
            builder.Append(",value");
            builder.Append(index.ToString());
            builder.Append('\n');
            index = index + 1;
        }

        string text = builder.ToString();
        return text;
    }
}

public class TrackingChunkedTextReader : StringReader
{
    public int ReadCalls;
    public int TotalCharactersRead;
    public int MaxRequestedCount;

    public TrackingChunkedTextReader(string text) : base(text)
    {
        ReadCalls = 0;
        TotalCharactersRead = 0;
        MaxRequestedCount = 0;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        ReadCalls = ReadCalls + 1;
        if (count > MaxRequestedCount)
        {
            MaxRequestedCount = count;
        }

        int read = base.Read(buffer, index, count);
        TotalCharactersRead = TotalCharactersRead + read;
        return read;
    }
}
