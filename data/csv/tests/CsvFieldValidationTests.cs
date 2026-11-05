using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvFieldValidationTests
{
    [Test]
    public void Process_ValidatesEveryFieldAndRejectsInvalidRowsWithAllDiagnostics()
    {
        CsvReader reader = new CsvReader(new StringReader("ok,10\n,-2\n"), new CsvReaderOptions());
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, new CsvWriterOptions());
        CsvPipeline pipeline = new CsvPipeline();
        pipeline.FieldValidator = new CsvFieldValidator("InvalidField", "The field is invalid.", IsValidField, CreateFieldDiagnostic);
        pipeline.HasFieldValidator = true;
        List<int> handledColumns = new List<int>();
        pipeline.RejectHandler = new CsvRejectHandler((row, diagnostic) => handledColumns.Add(diagnostic.Column));
        pipeline.HasRejectHandler = true;
        ICsvProcessor processor = new CsvProcessorService();

        long count = processor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("ok,10\r\n"));
        Assert.That(pipeline.FieldsValidated, Is.EqualTo(4));
        Assert.That(pipeline.FieldsRejected, Is.EqualTo(2));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(1));
        Assert.That(pipeline.Rejects.Count, Is.EqualTo(1));
        Assert.That(pipeline.Rejects[0].Fields, Is.EqualTo(new string[] { "", "-2" }));
        Assert.That(pipeline.Rejects[0].Diagnostics.Items.Count, Is.EqualTo(2));
        Assert.That(pipeline.RejectDiagnostics.Items.Count, Is.EqualTo(2));
        Assert.That(pipeline.RejectDiagnostics.Items[0].Code, Is.EqualTo("InvalidField"));
        Assert.That(pipeline.RejectDiagnostics.Items[0].Column, Is.EqualTo(1));
        Assert.That(pipeline.RejectDiagnostics.Items[1].Column, Is.EqualTo(2));
        Assert.That(handledColumns, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void Process_FieldValidationRunsAfterTransformationAndRowValidation()
    {
        CsvReader reader = new CsvReader(new StringReader("reject,-1\nok,-2\n"), new CsvReaderOptions());
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, new CsvWriterOptions());
        CsvPipeline pipeline = new CsvPipeline();
        pipeline.Validator = new CsvRowValidator("InvalidRow", "The row is invalid.", IsValidRow, CreateRowDiagnostic);
        pipeline.HasValidator = true;
        pipeline.Transformer = new CsvRowTransformer(MakeSecondFieldValid);
        pipeline.HasTransformer = true;
        pipeline.FieldValidator = new CsvFieldValidator("InvalidField", "The field is invalid.", IsValidField, CreateFieldDiagnostic);
        pipeline.HasFieldValidator = true;
        ICsvProcessor processor = new CsvProcessorService();

        long count = processor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("ok,2\r\n"));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(2));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(1));
        Assert.That(pipeline.FieldsValidated, Is.EqualTo(2));
        Assert.That(pipeline.FieldsRejected, Is.EqualTo(0));
        Assert.That(pipeline.RejectDiagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(pipeline.RejectDiagnostics.Items[0].Code, Is.EqualTo("InvalidRow"));
    }

    [Test]
    public void Process_DisabledFieldValidatorDoesNotInvokeCallbacksOrCounters()
    {
        int callbackCount = 0;
        CsvFieldValidator validator = new CsvFieldValidator("InvalidField", "The field is invalid.", (row, index, value) =>
        {
            callbackCount = callbackCount + 1;
            return false;
        }, CreateFieldDiagnostic);
        CsvPipeline pipeline = new CsvPipeline();
        pipeline.FieldValidator = validator;
        pipeline.HasFieldValidator = false;
        CsvReader reader = new CsvReader(new StringReader("a,b\n"), new CsvReaderOptions());
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, new CsvWriterOptions());
        ICsvProcessor processor = new CsvProcessorService();

        long count = processor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(output.ToString(), Is.EqualTo("a,b\r\n"));
        Assert.That(callbackCount, Is.EqualTo(0));
        Assert.That(pipeline.FieldsValidated, Is.EqualTo(0));
        Assert.That(pipeline.FieldsRejected, Is.EqualTo(0));
    }

    static bool IsValidField(CsvRow row, int fieldIndex, string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        if (value[0] == '-')
        {
            return false;
        }

        return true;
    }

    static CsvDiagnostic CreateFieldDiagnostic(CsvRow row, int fieldIndex, string? value)
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = CsvDiagnostic.ProcessingCategory;
        diagnostic.Code = "InvalidField";
        diagnostic.Message = "The field is invalid.";
        diagnostic.Row = row.RowNumber;
        diagnostic.Column = fieldIndex + 1;
        diagnostic.Line = row.Line;
        diagnostic.CharacterOffset = row.CharacterOffset;
        diagnostic.RawContext = row.RawText;
        return diagnostic;
    }

    static bool IsValidRow(CsvRow row)
    {
        return row.Fields[0] != "reject";
    }

    static CsvDiagnostic CreateRowDiagnostic(CsvRow row)
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = CsvDiagnostic.ProcessingCategory;
        diagnostic.Code = "InvalidRow";
        diagnostic.Message = "The row is invalid.";
        diagnostic.Row = row.RowNumber;
        diagnostic.Line = row.Line;
        diagnostic.CharacterOffset = row.CharacterOffset;
        diagnostic.RawContext = row.RawText;
        return diagnostic;
    }

    static CsvRow MakeSecondFieldValid(CsvRow row)
    {
        CsvRow transformed = new CsvRow();
        transformed.RowNumber = row.RowNumber;
        transformed.Line = row.Line;
        transformed.CharacterOffset = row.CharacterOffset;
        transformed.RawText = row.RawText;
        transformed.Fields = new string?[] { row.Fields[0], "2" };
        return transformed;
    }
}
