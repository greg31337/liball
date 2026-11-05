using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvInterfaceTests
{
    [Test]
    public void Process_UsesInterfaceSourcesSinksAndPipelineStages()
    {
        CsvHeader header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Value" }, 1), CsvHeaderOptions.CreateDefault());
        List<CsvRow> rows = new List<CsvRow>();
        rows.Add(CsvRow.Create(new string[] { "skip" }, 2));
        rows.Add(CsvRow.Create(new string[] { "bad" }, 3));
        rows.Add(CsvRow.Create(new string[] { "ok" }, 4));
        InterfaceRowReader reader = new InterfaceRowReader(header, rows);
        InterfaceRowWriter writer = new InterfaceRowWriter();
        InterfaceRejectHandler rejectHandler = new InterfaceRejectHandler();
        InterfaceProgressHandler progressHandler = new InterfaceProgressHandler();
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.UseFilter(new InterfaceFilter());
        pipeline.UseTransformer(new InterfaceTransformer());
        pipeline.UseValidator(new InterfaceValidator());
        pipeline.UseRejectHandler(rejectHandler);
        pipeline.UseProgressHandler(progressHandler);

        long count = CsvProcessor.Process(reader, writer, pipeline);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(writer.Headers.Count, Is.EqualTo(1));
        Assert.That(writer.Rows.Count, Is.EqualTo(1));
        Assert.That(writer.Rows[0].GetField(0), Is.EqualTo("OK"));
        Assert.That(pipeline.RowsFiltered, Is.EqualTo(1));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(2));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(1));
        Assert.That(rejectHandler.Count, Is.EqualTo(1));
        Assert.That(rejectHandler.LastCode, Is.EqualTo("BadValue"));
        Assert.That(progressHandler.Count, Is.EqualTo(3));
    }

    [Test]
    public void PipelineUseMethods_DisableStagesWhenPassedNull()
    {
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        pipeline.UseFilter(new InterfaceFilter());
        pipeline.UseTransformer(new InterfaceTransformer());
        pipeline.UseValidator(new InterfaceValidator());
        pipeline.UseRejectHandler(new InterfaceRejectHandler());
        pipeline.UseProgressHandler(new InterfaceProgressHandler());

        pipeline.UseFilter(null);
        pipeline.UseTransformer(null);
        pipeline.UseValidator(null);
        pipeline.UseRejectHandler(null);
        pipeline.UseProgressHandler(null);

        Assert.That(pipeline.HasFilter, Is.False);
        Assert.That(pipeline.HasTransformer, Is.False);
        Assert.That(pipeline.HasValidator, Is.False);
        Assert.That(pipeline.HasRejectHandler, Is.False);
        Assert.That(pipeline.HasProgressHandler, Is.False);
    }

    [Test]
    public void PublicInterfaces_ContainOnlyNamedInstanceMethods()
    {
        Type[] interfaceTypes = new Type[]
        {
            typeof(ICsvRowReader),
            typeof(ICsvRowWriter),
            typeof(ICsvRowFilter),
            typeof(ICsvRowTransformer),
            typeof(ICsvRowValidator),
            typeof(ICsvRejectHandler),
            typeof(ICsvProgressHandler)
        };

        int typeIndex = 0;
        while (typeIndex < interfaceTypes.Length)
        {
            Type interfaceType = interfaceTypes[typeIndex];
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.Name, Does.Match("^I[A-Z]"));
            Assert.That(interfaceType.GetFields(), Is.Empty);
            Assert.That(interfaceType.GetProperties(), Is.Empty);
            Assert.That(interfaceType.GetInterfaces(), Is.Empty);
            MethodInfo[] methods = interfaceType.GetMethods();
            int methodIndex = 0;
            while (methodIndex < methods.Length)
            {
                Assert.That(methods[methodIndex].IsStatic, Is.False);
                Assert.That(methods[methodIndex].IsGenericMethod, Is.False);
                methodIndex = methodIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }
    }
}

public class InterfaceRowReader : ICsvRowReader
{
    public CsvHeader Header;
    public List<CsvRow> Rows;
    public int Index;
    public CsvDiagnosticList Diagnostics;

    public InterfaceRowReader(CsvHeader header, List<CsvRow> rows)
    {
        Header = header;
        Rows = rows;
        Index = -1;
        Diagnostics = CsvDiagnosticList.Create();
    }

    public bool Read()
    {
        Index = Index + 1;
        if (Index < Rows.Count)
        {
            Rows[Index].Header = Header;
            return true;
        }

        return false;
    }

    public bool ReadHeader()
    {
        return Header.Names.Length > 0;
    }

    public CsvRow GetCurrentRow()
    {
        return Rows[Index];
    }

    public CsvHeader GetHeaders()
    {
        return Header;
    }

    public CsvDiagnosticList GetDiagnostics()
    {
        return Diagnostics;
    }

    public bool ShouldReadHeader()
    {
        return false;
    }
}

public class InterfaceRowWriter : ICsvRowWriter
{
    public List<CsvHeader> Headers;
    public List<CsvRow> Rows;

    public InterfaceRowWriter()
    {
        Headers = new List<CsvHeader>();
        Rows = new List<CsvRow>();
    }

    public void WriteHeader(CsvHeader header)
    {
        Headers.Add(header.Clone());
    }

    public void WriteRow(CsvRow row)
    {
        Rows.Add(row.Clone());
    }

    public bool ShouldWriteHeader()
    {
        return true;
    }
}

public class InterfaceFilter : ICsvRowFilter
{
    public bool Include(CsvRow row)
    {
        return row.GetField(0) != "skip";
    }
}

public class InterfaceTransformer : ICsvRowTransformer
{
    public CsvRow Transform(CsvRow row)
    {
        CsvRow transformed = row.Clone();
        string? value = transformed.GetField(0);
        if (value != null)
        {
            transformed.SetField(0, value.ToUpperInvariant());
        }
        return transformed;
    }
}

public class InterfaceValidator : ICsvRowValidator
{
    public bool IsValid(CsvRow row)
    {
        return row.GetField(0) != "BAD";
    }

    public CsvDiagnostic CreateDiagnostic(CsvRow row)
    {
        return CsvDiagnostic.CreateProcessingError("BadValue", "The transformed value is not accepted.", row.RowNumber, 0, row.Line, row.CharacterOffset, row.RawText);
    }
}

public class InterfaceRejectHandler : ICsvRejectHandler
{
    public int Count;
    public string LastCode;

    public InterfaceRejectHandler()
    {
        Count = 0;
        LastCode = "";
    }

    public void Handle(CsvRow row, CsvDiagnostic diagnostic)
    {
        Count = Count + 1;
        LastCode = diagnostic.Code;
    }
}

public class InterfaceProgressHandler : ICsvProgressHandler
{
    public int Count;

    public InterfaceProgressHandler()
    {
        Count = 0;
    }

    public void Report(CsvPipeline pipeline, CsvRow row)
    {
        Count = Count + 1;
    }
}
