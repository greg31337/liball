namespace avif.Tests;

public class ResultCodeTests
{
    [Test]
    public void StableResultCodesUseTheirReservedRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)AvifResultCode.Success, Is.EqualTo(0));
            Assert.That((int)AvifResultCode.NeedMoreData, Is.InRange(1, 999));
            Assert.That((int)AvifResultCode.InvalidArgument, Is.InRange(1000, 1999));
            Assert.That((int)AvifResultCode.IoError, Is.InRange(2000, 2999));
            Assert.That((int)AvifResultCode.InvalidData, Is.InRange(3000, 3999));
            Assert.That((int)AvifResultCode.UnsupportedProtection, Is.InRange(4000, 4999));
            Assert.That((int)AvifResultCode.LimitExceeded, Is.InRange(5000, 5999));
            Assert.That((int)AvifResultCode.DecodeFailed, Is.InRange(6000, 6999));
            Assert.That((int)AvifResultCode.UnknownError, Is.EqualTo(9000));
        });
    }

    [Test]
    public void DiagnosticEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)AvifDiagnosticSeverity.Information, Is.EqualTo(0));
            Assert.That((int)AvifDiagnosticSeverity.Warning, Is.EqualTo(1));
            Assert.That((int)AvifDiagnosticSeverity.Error, Is.EqualTo(2));
            Assert.That((int)AvifDiagnosticSeverity.Fatal, Is.EqualTo(3));
            Assert.That((int)AvifDiagnosticCategory.General, Is.EqualTo(0));
            Assert.That((int)AvifDiagnosticCategory.Protection, Is.EqualTo(15));
        });
    }
}
