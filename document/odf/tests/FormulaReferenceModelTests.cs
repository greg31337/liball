using System.Numerics;

namespace odf.Tests;

public class FormulaReferenceModelTests
{
    [Test]
    public void ResolvesAbsoluteRelativeAndThreeDimensionalCoordinates()
    {
        OdfFormula formula = FormulaParser.Parse(
            "=[$'Sheet 1'.$A$2:Sheet2.C4]",
            32);
        OdfFormulaReference reference = formula.Root!.Reference!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(reference.Kind, Is.EqualTo(OdfFormulaNodeKind.Range));
            Assert.That(reference.HasEnd, Is.True);
            Assert.That(reference.IsThreeDimensional, Is.True);
            Assert.That(reference.Start.SheetName, Is.EqualTo("Sheet 1"));
            Assert.That(reference.Start.EffectiveSheetName,
                Is.EqualTo("Sheet 1"));
            Assert.That(reference.Start.ColumnName, Is.EqualTo("A"));
            Assert.That(reference.Start.ColumnNumber, Is.EqualTo(BigInteger.One));
            Assert.That(reference.Start.RowNumber, Is.EqualTo(new BigInteger(2)));
            Assert.That(reference.Start.SheetAbsolute, Is.True);
            Assert.That(reference.Start.ColumnAbsolute, Is.True);
            Assert.That(reference.Start.RowAbsolute, Is.True);
            Assert.That(reference.End.SheetName, Is.EqualTo("Sheet2"));
            Assert.That(reference.End.ColumnName, Is.EqualTo("C"));
            Assert.That(reference.End.ColumnNumber, Is.EqualTo(new BigInteger(3)));
            Assert.That(reference.End.RowNumber, Is.EqualTo(new BigInteger(4)));
            Assert.That(reference.End.SheetAbsolute, Is.False);
            Assert.That(reference.End.ColumnAbsolute, Is.False);
            Assert.That(reference.End.RowAbsolute, Is.False);
        });
    }

    [Test]
    public void RepresentsWholeRowsAndColumns()
    {
        OdfFormula columns = FormulaParser.Parse("=[.A:.C]", 32);
        OdfFormula rows = FormulaParser.Parse("=[.$1:.$10]", 32);
        OdfFormulaReference columnReference = columns.Root!.Reference!;
        OdfFormulaReference rowReference = rows.Root!.Reference!;

        Assert.Multiple(() =>
        {
            Assert.That(columnReference.Start.HasColumn, Is.True);
            Assert.That(columnReference.Start.HasRow, Is.False);
            Assert.That(columnReference.End.ColumnNumber,
                Is.EqualTo(new BigInteger(3)));
            Assert.That(rowReference.Start.HasColumn, Is.False);
            Assert.That(rowReference.Start.HasRow, Is.True);
            Assert.That(rowReference.Start.RowAbsolute, Is.True);
            Assert.That(rowReference.Start.RowNumber,
                Is.EqualTo(BigInteger.One));
            Assert.That(rowReference.End.RowNumber,
                Is.EqualTo(new BigInteger(10)));
        });
    }

    [Test]
    public void ResolvesExternalSourcesAndInheritedRangeSheets()
    {
        OdfFormula formula = FormulaParser.Parse(
            "=['file.ods'#$'Data'.$A1:.$B$2]",
            32);
        OdfFormulaReference reference = formula.Root!.Reference!;

        Assert.Multiple(() =>
        {
            Assert.That(formula.IsValid, Is.True);
            Assert.That(reference.Kind,
                Is.EqualTo(OdfFormulaNodeKind.ExternalReference));
            Assert.That(reference.HasSource, Is.True);
            Assert.That(reference.Source, Is.EqualTo("file.ods"));
            Assert.That(reference.Start.SheetName, Is.EqualTo("Data"));
            Assert.That(reference.Start.SheetAbsolute, Is.True);
            Assert.That(reference.End.HasSheet, Is.False);
            Assert.That(reference.End.EffectiveSheetName, Is.EqualTo("Data"));
            Assert.That(reference.End.ColumnAbsolute, Is.True);
            Assert.That(reference.End.RowAbsolute, Is.True);
            Assert.That(reference.IsThreeDimensional, Is.False);
        });
    }

    [Test]
    public void ResolvesGlobalLocalExternalAndLabelNames()
    {
        OdfFormula globalName = FormulaParser.Parse("=$$'Gross Profit'", 32);
        OdfFormula localName = FormulaParser.Parse(
            "='Sheet 1'.$$Local",
            32);
        OdfFormula externalName = FormulaParser.Parse(
            "='book.ods'#$$Remote",
            32);
        OdfFormula label = FormulaParser.Parse("='Row Label'", 32);

        Assert.Multiple(() =>
        {
            Assert.That(globalName.Root!.Reference!.Name,
                Is.EqualTo("Gross Profit"));
            Assert.That(globalName.Root.Reference.IsExplicitName, Is.True);
            Assert.That(localName.Root!.Reference!.Name, Is.EqualTo("Local"));
            Assert.That(localName.Root.Reference.NameSheet,
                Is.EqualTo("Sheet 1"));
            Assert.That(localName.Root.Reference.IsExplicitName, Is.True);
            Assert.That(externalName.Root!.Reference!.Kind,
                Is.EqualTo(OdfFormulaNodeKind.ExternalReference));
            Assert.That(externalName.Root.Reference.Source,
                Is.EqualTo("book.ods"));
            Assert.That(externalName.Root.Reference.Name, Is.EqualTo("Remote"));
            Assert.That(externalName.Root.Reference.HasSource, Is.True);
            Assert.That(label.Root!.Reference!.Name, Is.EqualTo("Row Label"));
            Assert.That(label.Root.Reference.IsLabel, Is.True);
            Assert.That(label.Root.Reference.IsExplicitName, Is.False);
        });
    }

    [Test]
    public void RepresentsReferenceOperatorsAndInvalidReferences()
    {
        OdfFormula operators = FormulaParser.Parse(
            "=[.A1:.C3]![.B2:.D4]~[.E1]",
            32);
        OdfFormula labels = FormulaParser.Parse(
            "='Row'!!'Column'",
            32);
        OdfFormula invalid = FormulaParser.Parse("=[#REF!]", 32);
        OdfFormulaReference concatenation = operators.Root!.Reference!;
        OdfFormulaReference intersection = concatenation.Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(concatenation.Kind,
                Is.EqualTo(OdfFormulaNodeKind.ReferenceConcatenation));
            Assert.That(concatenation.LexicalValue,
                Is.EqualTo("[.A1:.C3]![.B2:.D4]~[.E1]"));
            Assert.That(concatenation.Items, Has.Length.EqualTo(2));
            Assert.That(intersection.Kind,
                Is.EqualTo(OdfFormulaNodeKind.Intersection));
            Assert.That(intersection.Items, Has.Length.EqualTo(2));
            Assert.That(intersection.Items[0].HasEnd, Is.True);
            Assert.That(labels.Root!.Reference!.Kind,
                Is.EqualTo(OdfFormulaNodeKind.Intersection));
            Assert.That(labels.Root.Reference.Items[0].IsLabel, Is.True);
            Assert.That(labels.Root.Reference.Items[1].IsLabel, Is.True);
            Assert.That(invalid.Root!.Reference!.IsReferenceError, Is.True);
        });
    }
}
