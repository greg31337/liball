using System.IO;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingFailureCleanupCompletionTests
{
    [Test]
    public void StreamingEntryPoints_RejectUnreadableAndUnwritableStreams()
    {
        MemoryStream unreadableWorkbook = new MemoryStream();
        unreadableWorkbook.Close();
        MemoryStream unreadableSheet = new MemoryStream();
        unreadableSheet.Close();
        MemoryStream unwritableWorkbook = new MemoryStream(new byte[16], false);
        MemoryStream unwritableSheet = new MemoryStream(new byte[16], false);

        XlsxStreamingWorkbookReadResult workbookRead = new XlsxStreamingWorkbookReader().ReadStream(unreadableWorkbook);
        XlsxStreamingSheetOpenResult sheetRead = new XlsxStreamingSheetReader().OpenStream(unreadableSheet);
        XlsxStreamingWorkbookWriteResult workbookWrite = new XlsxStreamingWorkbookWriter().BeginStream(unwritableWorkbook);
        XlsxStreamingSheetWriteResult sheetWrite = new XlsxStreamingSheetWriter().BeginStream(unwritableSheet);

        Assert.That(workbookRead.IsValid, Is.False);
        Assert.That(HasDiagnostic(workbookRead.Validation, "streamingWorkbook.open.streamNotReadable"), Is.True);
        Assert.That(sheetRead.IsValid, Is.False);
        Assert.That(HasDiagnostic(sheetRead.Validation, "streamingSheet.open.streamNotReadable"), Is.True);
        Assert.That(workbookWrite.IsValid, Is.False);
        Assert.That(workbookWrite.Session, Is.Null);
        Assert.That(HasDiagnostic(workbookWrite.Validation, "streamingWorkbook.write.streamNotWritable"), Is.True);
        Assert.That(sheetWrite.IsValid, Is.False);
        Assert.That(sheetWrite.Session, Is.Null);
        Assert.That(HasDiagnostic(sheetWrite.Validation, "streamingSheet.write.streamNotWritable"), Is.True);
    }

    [Test]
    public void StreamingSheetNullTextRow_DoesNotWriteRowAndSessionStillCloses()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingSheetWriteResult begin = new XlsxStreamingSheetWriter().BeginStream(stream);

        XlsxStreamingSheetWriteResult invalid = begin.Session!.WriteTextRow(null!);
        XlsxStreamingSheetWriteResult close = begin.Session.Close();
        string xml = Encoding.UTF8.GetString(stream.ToArray());

        Assert.That(invalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalid.Validation, "streamingSheet.write.rowValuesNull"), Is.True);
        Assert.That(close.IsValid, Is.False);
        Assert.That(close.RowCount, Is.EqualTo(0));
        Assert.That(close.CellCount, Is.EqualTo(0));
        Assert.That(xml, Does.Not.Contain("<row"));
        Assert.That(stream.CanWrite, Is.True);
    }

    [Test]
    public void StreamingWorkbookNullTextRow_DoesNotWriteRowAndSessionStillCloses()
    {
        MemoryStream stream = new MemoryStream();
        XlsxStreamingWorkbookWriteResult begin = new XlsxStreamingWorkbookWriter().BeginStream(stream);
        begin.Session!.BeginSheet("Data");

        XlsxStreamingWorkbookWriteResult invalid = begin.Session.WriteTextRow(null!);
        XlsxStreamingWorkbookWriteResult close = begin.Session.Close();

        Assert.That(invalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalid.Validation, "streamingWorkbook.write.rowValuesNull"), Is.True);
        Assert.That(close.IsValid, Is.False);
        Assert.That(close.RowCount, Is.EqualTo(0));
        Assert.That(close.CellCount, Is.EqualTo(0));
        Assert.That(close.SheetCount, Is.EqualTo(1));
        Assert.That(stream.CanWrite, Is.True);
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
