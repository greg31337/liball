using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingSheetWriterTests
{
    [Test]
    public void WriteStream_DefaultsToInlineStringsAndWritesTypedRows()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriteResult beginResult = new XlsxStreamingSheetWriter().BeginStream(stream);
        XlsxStreamingSheetWriteSession? session = beginResult.Session;
        List<XlsxCellValue> values = new List<XlsxCellValue>();
        values.Add(XlsxCellValue.CreateText("Alpha"));
        values.Add(XlsxCellValue.CreateNumber(12.5d));
        values.Add(XlsxCellValue.CreateBoolean(true));

        session!.WriteRow(values);
        XlsxStreamingSheetWriteResult closeResult = session.Close();
        string xml = Encoding.UTF8.GetString(stream.ToArray());
        XlsxStreamingSheetOpenResult readResult = new XlsxStreamingSheetReader().OpenBytes(stream.ToArray());
        bool read = readResult.Cursor!.Read();

        Assert.That(beginResult.IsValid, Is.True);
        Assert.That(closeResult.IsValid, Is.True);
        Assert.That(closeResult.RowCount, Is.EqualTo(1));
        Assert.That(closeResult.CellCount, Is.EqualTo(3));
        Assert.That(xml, Does.Contain("t=\"inlineStr\""));
        Assert.That(xml, Does.Not.Contain("t=\"s\""));
        Assert.That(read, Is.True);
        Assert.That(readResult.Cursor.Current!.GetCell("A1")!.FormattedValue, Is.EqualTo("Alpha"));
        Assert.That(readResult.Cursor.Current.GetCell("B1")!.FormattedValue, Is.EqualTo("12.5"));
        Assert.That(readResult.Cursor.Current.GetCell("C1")!.FormattedValue, Is.EqualTo("TRUE"));
    }

    [Test]
    public void WriteStream_SharedStringModeIndexesAndDeduplicatesText()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriterOptions options = new XlsxStreamingSheetWriterOptions();
        options.UseSharedStrings = true;
        XlsxStreamingSheetWriteResult beginResult = new XlsxStreamingSheetWriter().BeginStreamWithOptions(stream, options);
        XlsxStreamingSheetWriteSession? session = beginResult.Session;

        session!.WriteTextRow(new List<string> { "Alpha", "Alpha", "Beta" });
        XlsxStreamingSheetWriteResult closeResult = session.Close();
        string xml = Encoding.UTF8.GetString(stream.ToArray());
        XlsxSharedStringTablePart sharedStrings = closeResult.SharedStrings;
        XlsxStreamingSheetOpenResult readResult = new XlsxStreamingSheetReader().OpenBytesWithWorkbookContext(stream.ToArray(), sharedStrings, new XlsxStylesPart(), false);
        bool read = readResult.Cursor!.Read();

        Assert.That(beginResult.IsValid, Is.True);
        Assert.That(closeResult.IsValid, Is.True);
        Assert.That(xml, Does.Contain("t=\"s\""));
        Assert.That(xml, Does.Contain("<v>0</v>"));
        Assert.That(xml, Does.Contain("<v>1</v>"));
        Assert.That(sharedStrings.Items.Count, Is.EqualTo(2));
        Assert.That(sharedStrings.Items[0].Text, Is.EqualTo("Alpha"));
        Assert.That(sharedStrings.Items[0].ReferenceCount, Is.EqualTo(2));
        Assert.That(sharedStrings.Items[1].Text, Is.EqualTo("Beta"));
        Assert.That(read, Is.True);
        Assert.That(readResult.Cursor.Current!.GetCell("A1")!.FormattedValue, Is.EqualTo("Alpha"));
        Assert.That(readResult.Cursor.Current.GetCell("B1")!.FormattedValue, Is.EqualTo("Alpha"));
        Assert.That(readResult.Cursor.Current.GetCell("C1")!.FormattedValue, Is.EqualTo("Beta"));
    }

    [Test]
    public void SharedStringMode_ProducesWritableSharedStringTable()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriterOptions options = new XlsxStreamingSheetWriterOptions();
        options.UseSharedStrings = true;
        XlsxStreamingSheetWriteResult beginResult = new XlsxStreamingSheetWriter().BeginStreamWithOptions(stream, options);

        beginResult.Session!.WriteTextRow(new List<string> { "North", "South", "North" });
        XlsxStreamingSheetWriteResult closeResult = beginResult.Session.Close();
        XlsxSharedStringTableWriteResult sharedStringWrite = new XlsxSharedStringTableWriter().WriteString(closeResult.SharedStrings);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(sharedStringWrite.Xml);

        Assert.That(closeResult.IsValid, Is.True);
        Assert.That(sharedStringWrite.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Items.Count, Is.EqualTo(2));
        Assert.That(roundTrip.Items[0].Text, Is.EqualTo("North"));
        Assert.That(roundTrip.Items[1].Text, Is.EqualTo("South"));
    }

    [Test]
    public void WriteStream_ReportsInvalidOperations()
    {
        XlsxStreamingSheetWriteResult? nullStream = new XlsxStreamingSheetWriter().BeginStream(null!);
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriteResult beginResult = new XlsxStreamingSheetWriter().BeginStream(stream);
        XlsxStreamingSheetWriteSession? session = beginResult.Session;

        XlsxStreamingSheetWriteResult? nullRow = session!.WriteRow(null!);
        session.Close();
        XlsxStreamingSheetWriteResult writeAfterClose = session.WriteTextRow(new List<string> { "Closed" });

        Assert.That(nullStream.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullStream, "streamingSheet.write.streamNull"), Is.True);
        Assert.That(nullRow.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRow, "streamingSheet.write.rowValuesNull"), Is.True);
        Assert.That(writeAfterClose.IsValid, Is.False);
        Assert.That(HasDiagnostic(writeAfterClose, "streamingSheet.write.closed"), Is.True);
    }

    static bool HasDiagnostic(XlsxStreamingSheetWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
