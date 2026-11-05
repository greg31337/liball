using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvCoverageGapTests
{
    [Test]
    public void UnionByName_WritesEmptyFieldsForColumnsMissingFromLaterFiles()
    {
        string firstPath = CreateTempPath();
        string secondPath = CreateTempPath();
        string outputPath = CreateTempPath();
        try
        {
            File.WriteAllText(firstPath, "Id,Name,Dept\n1,Ada,Eng\n", Encoding.UTF8);
            File.WriteAllText(secondPath, "Id,Dept\n2,Ops\n", Encoding.UTF8);
            CsvWriterOptions writerOptions = CsvWriterOptions.CreateDefault();
            writerOptions.WriteHeader = true;

            long count = CsvFile.UnionByName(new string[] { firstPath, secondPath }, outputPath, CsvReaderOptions.CreateDefault(), writerOptions);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(File.ReadAllText(outputPath, Encoding.UTF8), Is.EqualTo("Id,Name,Dept\r\n1,Ada,Eng\r\n2,,Ops\r\n"));
        }
        finally
        {
            DeleteTempPath(firstPath);
            DeleteTempPath(secondPath);
            DeleteTempPath(outputPath);
        }
    }

    [Test]
    public void LoadTable_PreserveDiagnosticsFalseOmitsReaderAndRowDiagnostics()
    {
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.HeaderOptions.HasHeader = true;
        readerOptions.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,B\n1\n2,ok\n"), readerOptions);
        CsvTableOptions tableOptions = CsvTableOptions.CreateDefault();
        tableOptions.PreserveDiagnostics = false;

        CsvTable table = CsvProcessor.LoadTable(reader, tableOptions);

        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MissingField"));
        Assert.That(table.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(table.CountRows(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(table.GetRow(1).Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(table.GetRow(0).GetFieldByName("B"), Is.EqualTo(""));
    }

    [Test]
    public void CsvTable_SelectColumnsByNameProjectsMissingColumnsAsEmptyFields()
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Name" }, 1), headerOptions);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "1", "Ada" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "Grace" }, 3));

        CsvTable projected = table.SelectColumnsByName(new string[] { "Name", "Missing", "Id" });

        Assert.That(projected.Header.Names, Is.EqualTo(new string[] { "Name", "", "Id" }));
        Assert.That(projected.Schema.Count(), Is.EqualTo(3));
        Assert.That(projected.Schema.GetColumn(1).Name, Is.EqualTo(""));
        Assert.That(projected.GetRow(0).Fields, Is.EqualTo(new string[] { "Ada", "", "1" }));
        Assert.That(projected.GetRow(1).Fields, Is.EqualTo(new string[] { "Grace", "", "2" }));
        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Id", "Name" }));
    }

    [Test]
    public void CsvInjectionGuard_StripsRepeatedDangerousPrefixesAfterLeadingSpaces()
    {
        Assert.That(CsvInjectionGuard.IsDangerous("safe"), Is.False);
        Assert.That(CsvInjectionGuard.IsDangerous(" \tcmd"), Is.True);
        Assert.That(CsvInjectionGuard.StripDangerousPrefix("  -=@cmd"), Is.EqualTo("cmd"));
        Assert.That(CsvInjectionGuard.Apply(" \t=SUM(A1:A2)", CsvInjectionMode.Strip), Is.EqualTo("SUM(A1:A2)"));
        Assert.That(CsvInjectionGuard.Apply("safe", CsvInjectionMode.Escape), Is.EqualTo("safe"));
        Assert.Throws<InvalidOperationException>(() => CsvInjectionGuard.Apply("-10", CsvInjectionMode.Error));
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), "csv-tests-" + Guid.NewGuid().ToString("N") + ".csv");
        return path;
    }

    static void DeleteTempPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
