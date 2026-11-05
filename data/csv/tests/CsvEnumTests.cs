using System;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvEnumTests
{
    [Test]
    public void CsvErrorMode_DefinesReaderErrorPolicies()
    {
        Assert.That((int)CsvErrorMode.Strict, Is.EqualTo(0));
        Assert.That((int)CsvErrorMode.Relaxed, Is.EqualTo(1));
        Assert.That((int)CsvErrorMode.SkipBadRow, Is.EqualTo(2));
        Assert.That((int)CsvErrorMode.CollectRejects, Is.EqualTo(3));
    }

    [Test]
    public void CsvQuoteMode_DefinesWriterQuotePolicies()
    {
        Assert.That((int)CsvQuoteMode.Minimal, Is.EqualTo(0));
        Assert.That((int)CsvQuoteMode.All, Is.EqualTo(1));
        Assert.That((int)CsvQuoteMode.None, Is.EqualTo(2));
        Assert.That((int)CsvQuoteMode.NonNull, Is.EqualTo(3));
    }

    [Test]
    public void CsvTrimMode_DefinesSupportedTrimPolicies()
    {
        Assert.That((int)CsvTrimMode.None, Is.EqualTo(0));
        Assert.That((int)CsvTrimMode.Unquoted, Is.EqualTo(1));
        Assert.That((int)CsvTrimMode.QuotedAndUnquoted, Is.EqualTo(2));
        Assert.That((int)CsvTrimMode.Left, Is.EqualTo(3));
        Assert.That((int)CsvTrimMode.Right, Is.EqualTo(4));
        Assert.That((int)CsvTrimMode.Both, Is.EqualTo(5));
    }

    [Test]
    public void HeaderAndFieldPolicyEnums_DefineExpectedPolicies()
    {
        Assert.That((int)CsvDuplicateHeaderMode.Allow, Is.EqualTo(0));
        Assert.That((int)CsvDuplicateHeaderMode.FirstWins, Is.EqualTo(1));
        Assert.That((int)CsvDuplicateHeaderMode.LastWins, Is.EqualTo(2));
        Assert.That((int)CsvDuplicateHeaderMode.Error, Is.EqualTo(3));

        Assert.That((int)CsvMissingHeaderMode.Generate, Is.EqualTo(0));
        Assert.That((int)CsvMissingHeaderMode.Empty, Is.EqualTo(1));
        Assert.That((int)CsvMissingHeaderMode.Error, Is.EqualTo(2));

        Assert.That((int)CsvMissingFieldMode.Empty, Is.EqualTo(0));
        Assert.That((int)CsvMissingFieldMode.Null, Is.EqualTo(1));
        Assert.That((int)CsvMissingFieldMode.Error, Is.EqualTo(2));

        Assert.That((int)CsvExtraFieldMode.Allow, Is.EqualTo(0));
        Assert.That((int)CsvExtraFieldMode.Ignore, Is.EqualTo(1));
        Assert.That((int)CsvExtraFieldMode.Error, Is.EqualTo(2));
    }

    [Test]
    public void CsvInjectionMode_DefinesSpreadsheetFormulaPolicies()
    {
        Assert.That((int)CsvInjectionMode.None, Is.EqualTo(0));
        Assert.That((int)CsvInjectionMode.Strip, Is.EqualTo(1));
        Assert.That((int)CsvInjectionMode.Escape, Is.EqualTo(2));
        Assert.That((int)CsvInjectionMode.Error, Is.EqualTo(3));
    }

    [Test]
    public void CsvTypeKind_DefinesInferenceKinds()
    {
        Assert.That((int)CsvTypeKind.Unknown, Is.EqualTo(0));
        Assert.That((int)CsvTypeKind.Null, Is.EqualTo(1));
        Assert.That((int)CsvTypeKind.Boolean, Is.EqualTo(2));
        Assert.That((int)CsvTypeKind.Int32, Is.EqualTo(3));
        Assert.That((int)CsvTypeKind.Int64, Is.EqualTo(4));
        Assert.That((int)CsvTypeKind.Decimal, Is.EqualTo(5));
        Assert.That((int)CsvTypeKind.Double, Is.EqualTo(6));
        Assert.That((int)CsvTypeKind.DateTime, Is.EqualTo(7));
        Assert.That((int)CsvTypeKind.Guid, Is.EqualTo(8));
        Assert.That((int)CsvTypeKind.Binary, Is.EqualTo(9));
        Assert.That((int)CsvTypeKind.String, Is.EqualTo(10));
        Assert.That((int)CsvTypeKind.DateOnly, Is.EqualTo(11));
        Assert.That((int)CsvTypeKind.TimeOnly, Is.EqualTo(12));
    }

    [Test]
    public void CsvNewLineMode_DefinesReadNewlinePolicies()
    {
        Assert.That((int)CsvNewLineMode.Auto, Is.EqualTo(0));
        Assert.That((int)CsvNewLineMode.Crlf, Is.EqualTo(1));
        Assert.That((int)CsvNewLineMode.Lf, Is.EqualTo(2));
        Assert.That((int)CsvNewLineMode.Cr, Is.EqualTo(3));
    }

    [Test]
    public void CsvParserState_DefinesStateMachineStates()
    {
        Assert.That((int)CsvParserState.StartField, Is.EqualTo(0));
        Assert.That((int)CsvParserState.InField, Is.EqualTo(1));
        Assert.That((int)CsvParserState.InQuotedField, Is.EqualTo(2));
        Assert.That((int)CsvParserState.AfterQuote, Is.EqualTo(3));
        Assert.That((int)CsvParserState.AfterDelimiter, Is.EqualTo(4));
        Assert.That((int)CsvParserState.EndRecord, Is.EqualTo(5));
    }

    [TestCase(typeof(CsvErrorMode))]
    [TestCase(typeof(CsvQuoteMode))]
    [TestCase(typeof(CsvTrimMode))]
    [TestCase(typeof(CsvDuplicateHeaderMode))]
    [TestCase(typeof(CsvMissingHeaderMode))]
    [TestCase(typeof(CsvMissingFieldMode))]
    [TestCase(typeof(CsvExtraFieldMode))]
    [TestCase(typeof(CsvInjectionMode))]
    [TestCase(typeof(CsvTypeKind))]
    [TestCase(typeof(CsvNewLineMode))]
    public void CsvPolicyTypes_ArePublicEnums(Type enumType)
    {
        Assert.That(enumType.IsEnum, Is.True);
        Assert.That(enumType.IsPublic, Is.True);
    }

    [Test]
    public void CsvParserState_IsAnInternalImplementationEnum()
    {
        Assert.That(typeof(CsvParserState).IsEnum, Is.True);
        Assert.That(typeof(CsvParserState).IsNotPublic, Is.True);
    }
}
