using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvDiagnosticTests
{
    [Test]
    public void CreateParserError_PopulatesLocationAndContext()
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateParserError("BadQuote", "Unexpected quote", 2, 3, 4, 12, "\"bad");

        Assert.That(diagnostic.Category, Is.EqualTo(CsvDiagnostic.ParserCategory));
        Assert.That(diagnostic.Code, Is.EqualTo("BadQuote"));
        Assert.That(diagnostic.Message, Is.EqualTo("Unexpected quote"));
        Assert.That(diagnostic.Row, Is.EqualTo(2));
        Assert.That(diagnostic.Column, Is.EqualTo(3));
        Assert.That(diagnostic.Line, Is.EqualTo(4));
        Assert.That(diagnostic.CharacterOffset, Is.EqualTo(12));
        Assert.That(diagnostic.RawContext, Is.EqualTo("\"bad"));
        Assert.That(diagnostic.IsError, Is.True);
    }

    [Test]
    public void FactoryMethods_AssignExpectedCategories()
    {
        Assert.That(CsvDiagnostic.CreateConverterError("C", "m", 0, 0, 0, 0, "").Category, Is.EqualTo(CsvDiagnostic.ConverterCategory));
        Assert.That(CsvDiagnostic.CreateHeaderError("H", "m", 0, 0, 0, 0, "").Category, Is.EqualTo(CsvDiagnostic.HeaderCategory));
        Assert.That(CsvDiagnostic.CreateSchemaError("S", "m", 0, 0, 0, 0, "").Category, Is.EqualTo(CsvDiagnostic.SchemaCategory));
        Assert.That(CsvDiagnostic.CreateProcessingError("P", "m", 0, 0, 0, 0, "").Category, Is.EqualTo(CsvDiagnostic.ProcessingCategory));
    }

    [Test]
    public void Clone_CopiesDiagnosticToIndependentInstance()
    {
        CsvDiagnostic original = CsvDiagnostic.CreateHeaderError("Duplicate", "Duplicate header", 1, 2, 1, 5, "a,a");
        CsvDiagnostic clone = original.Clone();
        original.Code = "Changed";

        Assert.That(clone.Category, Is.EqualTo(CsvDiagnostic.HeaderCategory));
        Assert.That(clone.Code, Is.EqualTo("Duplicate"));
        Assert.That(clone.Message, Is.EqualTo("Duplicate header"));
        Assert.That(clone.RawContext, Is.EqualTo("a,a"));
    }

    [Test]
    public void DiagnosticList_AddGetCountAndClear_WorkTogether()
    {
        CsvDiagnosticList list = CsvDiagnosticList.Create();
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateSchemaError("Type", "Could not infer type", 3, 1, 3, 20, "abc");

        list.Add(diagnostic);

        Assert.That(list.Count(), Is.EqualTo(1));
        Assert.That(list.Get(0), Is.SameAs(diagnostic));
        Assert.That(list.HasErrors(), Is.True);

        list.Clear();

        Assert.That(list.Count(), Is.EqualTo(0));
        Assert.That(list.HasErrors(), Is.False);
    }

    [Test]
    public void DiagnosticList_CloneCopiesContainedDiagnostics()
    {
        CsvDiagnosticList original = CsvDiagnosticList.Create();
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateProcessingError("Rejected", "Row rejected", 4, 0, 4, 30, "x");
        original.Add(diagnostic);

        CsvDiagnosticList clone = original.Clone();
        diagnostic.Code = "Changed";

        Assert.That(clone.Count(), Is.EqualTo(1));
        Assert.That(clone.Get(0), Is.Not.SameAs(diagnostic));
        Assert.That(clone.Get(0).Code, Is.EqualTo("Rejected"));
    }

    [Test]
    public void DiagnosticTypes_ExposePublicFieldsAndNoProperties()
    {
        AssertFieldBased(typeof(CsvDiagnostic));
        AssertFieldBased(typeof(CsvDiagnosticList));
    }

    static void AssertFieldBased(System.Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(fields.Length, Is.GreaterThan(0));
        Assert.That(properties, Is.Empty);
    }
}
