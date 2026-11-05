using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvCheckedArithmeticTests
{
    [Test]
    public void ParseDigitHelpers_RejectOffsetsWithoutOverflowing()
    {
        Assert.That(CsvConverter.ParseTwoDigits("12", int.MaxValue), Is.EqualTo(-1));
        Assert.That(CsvConverter.ParseTwoDigits("12", int.MinValue), Is.EqualTo(-1));
        Assert.That(CsvConverter.ParseFourDigits("1234", int.MaxValue), Is.EqualTo(-1));
        Assert.That(CsvConverter.ParseFourDigits("1234", int.MinValue), Is.EqualTo(-1));
    }

    [Test]
    public void SnifferArithmetic_HandlesValuesOutsideIntScoreRange()
    {
        Assert.That(CsvSniffer.Absolute(int.MinValue), Is.EqualTo(2147483648L));
        Assert.That(CsvSniffer.ClampScore(long.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.That(CsvSniffer.ClampScore(long.MinValue), Is.EqualTo(int.MinValue));
    }

    [Test]
    public void GeneratedColumnNames_HandleMaximumIndex()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvHeader header = new CsvHeader();

        Assert.That(header.ResolveMissingName(options, int.MaxValue), Is.EqualTo("Column2147483648"));
        Assert.That(CsvSchema.GetSampleColumnName(new CsvTable(), int.MaxValue), Is.EqualTo("Column2147483648"));
    }
}
