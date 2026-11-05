using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvFileTests
{
    [Test]
    public void OpenWriterAndOpenReader_WriteAndReadUtf8CsvFile()
    {
        string path = CreateTempPath();
        try
        {
            CsvWriter writer = CsvFile.OpenWriter(path, CsvWriterOptions.CreateDefault());
            writer.WriteRow(CsvRow.Create(new string[] { "Id", "Name" }, 1));
            writer.WriteRow(CsvRow.Create(new string[] { "1", "Ada" }, 2));
            writer.Close();

            CsvReader reader = CsvFile.OpenReader(path, CsvReaderOptions.CreateDefault());

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetField(0), Is.EqualTo("Id"));
            Assert.That(reader.GetField(1), Is.EqualTo("Name"));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetField(0), Is.EqualTo("1"));
            Assert.That(reader.GetField(1), Is.EqualTo("Ada"));

            reader.Close();
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void OpenAppendWriter_AppendsRowsToExistingFile()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllText(path, "existing\r\n", Encoding.UTF8);
            CsvWriter writer = CsvFile.OpenAppendWriter(path, CsvWriterOptions.CreateDefault());

            writer.WriteRow(CsvRow.Create(new string[] { "next" }, 2));
            writer.Close();

            Assert.That(File.ReadAllText(path, Encoding.UTF8), Is.EqualTo("existing\r\nnext\r\n"));
            Assert.That(writer.Options.Append, Is.True);
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void OpenWriter_UsesAppendOptionWhenConfigured()
    {
        string path = CreateTempPath();
        try
        {
            File.WriteAllText(path, "first\r\n", Encoding.UTF8);
            CsvWriterOptions options = CsvWriterOptions.CreateDefault();
            options.Append = true;
            CsvWriter writer = CsvFile.OpenWriter(path, options);

            writer.WriteRow(CsvRow.Create(new string[] { "second" }, 2));
            writer.Close();

            Assert.That(File.ReadAllText(path, Encoding.UTF8), Is.EqualTo("first\r\nsecond\r\n"));
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void OpenWriterAscii_WritesAsciiEncodedFile()
    {
        string path = CreateTempPath();
        try
        {
            CsvWriter writer = CsvFile.OpenWriterAscii(path, CsvWriterOptions.CreateDefault());

            writer.WriteRow(CsvRow.Create(new string[] { "café" }, 1));
            writer.Close();

            Assert.That(File.ReadAllText(path, Encoding.ASCII), Is.EqualTo("caf?\r\n"));
        }
        finally
        {
            DeleteTempPath(path);
        }
    }

    [Test]
    public void ConcatenateByPosition_WritesFirstHeaderAndRowsFromAllFiles()
    {
        string firstPath = CreateTempPath();
        string secondPath = CreateTempPath();
        string outputPath = CreateTempPath();
        try
        {
            File.WriteAllText(firstPath, "Id,Name\n1,Ada\n2,Grace\n", Encoding.UTF8);
            File.WriteAllText(secondPath, "Code,Full\n3,Linus\n", Encoding.UTF8);
            CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
            readerOptions.HeaderOptions.HasHeader = true;
            CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
            writerOptions.WriteHeader = true;

            long count = CsvFile.ConcatenateByPosition(new string[] { firstPath, secondPath }, outputPath, readerOptions, writerOptions);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(File.ReadAllText(outputPath, Encoding.UTF8), Is.EqualTo("Id,Name\r\n1,Ada\r\n2,Grace\r\n3,Linus\r\n"));
        }
        finally
        {
            DeleteTempPath(firstPath);
            DeleteTempPath(secondPath);
            DeleteTempPath(outputPath);
        }
    }

    [Test]
    public void ConcatenateByPosition_KeepsRowsByPositionWhenNoHeaderIsConfigured()
    {
        string firstPath = CreateTempPath();
        string secondPath = CreateTempPath();
        string outputPath = CreateTempPath();
        try
        {
            File.WriteAllText(firstPath, "A,B\n1,2\n", Encoding.UTF8);
            File.WriteAllText(secondPath, "C,D\n3,4\n", Encoding.UTF8);

            long count = CsvFile.ConcatenateByPosition(new string[] { firstPath, secondPath }, outputPath, CsvReaderOptions.CreateDefault(), CsvWriterOptions.CreateDefault());

            Assert.That(count, Is.EqualTo(4));
            Assert.That(File.ReadAllText(outputPath, Encoding.UTF8), Is.EqualTo("A,B\r\n1,2\r\nC,D\r\n3,4\r\n"));
        }
        finally
        {
            DeleteTempPath(firstPath);
            DeleteTempPath(secondPath);
            DeleteTempPath(outputPath);
        }
    }

    [Test]
    public void UnionByName_ReordersLaterFilesToFirstHeader()
    {
        string firstPath = CreateTempPath();
        string secondPath = CreateTempPath();
        string outputPath = CreateTempPath();
        try
        {
            File.WriteAllText(firstPath, "Id,Name,Dept\n1,Ada,Eng\n", Encoding.UTF8);
            File.WriteAllText(secondPath, "Dept,Id,Name\nOps,2,Grace\nEng,3,Linus\n", Encoding.UTF8);
            CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
            writerOptions.WriteHeader = true;

            long count = CsvFile.UnionByName(new string[] { firstPath, secondPath }, outputPath, CsvReaderOptions.CreateDefault(), writerOptions);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(File.ReadAllText(outputPath, Encoding.UTF8), Is.EqualTo("Id,Name,Dept\r\n1,Ada,Eng\r\n2,Grace,Ops\r\n3,Linus,Eng\r\n"));
        }
        finally
        {
            DeleteTempPath(firstPath);
            DeleteTempPath(secondPath);
            DeleteTempPath(outputPath);
        }
    }

    [Test]
    public void UnionByName_UsesHeaderOptionsForColumnLookup()
    {
        string firstPath = CreateTempPath();
        string secondPath = CreateTempPath();
        string outputPath = CreateTempPath();
        try
        {
            File.WriteAllText(firstPath, "Id,Name\n1,Ada\n", Encoding.UTF8);
            File.WriteAllText(secondPath, "name,id\nGrace,2\n", Encoding.UTF8);
            CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
            readerOptions.HeaderOptions.CaseSensitive = false;
            CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
            writerOptions.WriteHeader = true;

            long count = CsvFile.UnionByName(new string[] { firstPath, secondPath }, outputPath, readerOptions, writerOptions);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(File.ReadAllText(outputPath, Encoding.UTF8), Is.EqualTo("Id,Name\r\n1,Ada\r\n2,Grace\r\n"));
        }
        finally
        {
            DeleteTempPath(firstPath);
            DeleteTempPath(secondPath);
            DeleteTempPath(outputPath);
        }
    }

    public static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), "csv-tests-" + Guid.NewGuid().ToString("N") + ".csv");
        return path;
    }

    public static void DeleteTempPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
