using uspto;

namespace uspto.Tests;

public class TsdrPolicyTests
{
    [TestCase(DocumentFormat.Pdf, ApiService.TsdrPdfAndZip)]
    [TestCase(DocumentFormat.Zip, ApiService.TsdrPdfAndZip)]
    [TestCase(DocumentFormat.Json, ApiService.Tsdr)]
    [TestCase(DocumentFormat.Xml, ApiService.Tsdr)]
    [TestCase(DocumentFormat.Html, ApiService.Tsdr)]
    [TestCase(DocumentFormat.Text, ApiService.Tsdr)]
    [TestCase(DocumentFormat.Unknown, ApiService.Tsdr)]
    public void PublishedRepresentationClassesSelectTheCorrectRateBucket(DocumentFormat format, ApiService expected)
    {
        ApiService result = TsdrPolicy.ClassifyRateService(format);

        Assert.That(result, Is.EqualTo(expected));
    }
}
