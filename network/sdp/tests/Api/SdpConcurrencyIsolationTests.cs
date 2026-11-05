using System.Threading;
using System.Threading.Tasks;

namespace sdp.Tests.Api;

public class SdpConcurrencyIsolationTests
{
    [Test]
    public void SharedServices_KeepConcurrentOperationsAndMutableResultsIsolated()
    {
        const int operationCount = 32;
        ISdpParser parser = new SdpParser();
        ISdpWriter writer = new SdpWriter();
        ISdpValidator validator = new SdpValidator();
        ISdpRegistry registry = new SdpRegistry();
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task<ConcurrentOperationResult>[] tasks = new Task<ConcurrentOperationResult>[operationCount];
        int index = 0;
        while (index < tasks.Length)
        {
            int operationIndex = index;
            tasks[index] = Task.Run(() => RunOperation(operationIndex, start, parser, writer, validator, registry));
            index++;
        }

        start.Set();
        ConcurrentOperationResult[] results = Task.WhenAll(tasks).GetAwaiter().GetResult();
        start.Dispose();

        index = 0;
        while (index < results.Length)
        {
            ConcurrentOperationResult result = results[index];
            string lineEnding = index % 2 == 0 ? "\r\n" : "\n";

            Assert.Multiple(() =>
            {
                Assert.That(result.Parse.Success, Is.True);
                Assert.That(result.Parse.Description!.SessionName, Is.EqualTo("Concurrent-" + index));
                Assert.That(result.Parse.Diagnostics, Is.Empty);
                Assert.That(result.Parse.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(index + 8));
                Assert.That(result.Write.Success, Is.True);
                Assert.That(result.Write.Text, Is.EqualTo(Input(index, lineEnding)));
                Assert.That(result.Write.Diagnostics, Is.Empty);
                Assert.That(result.Write.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(index + 8));
                Assert.That(result.Validation.Success, Is.True);
                Assert.That(result.Validation.Issues, Is.Empty);
                Assert.That(result.Validation.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(index + 8));
                Assert.That(result.Registration.Found, Is.True);
                Assert.That(result.Registration.Name, Is.EqualTo("operation-" + index));
                Assert.That(result.Registration.References, Does.Contain("operation-" + index));
            });
            index++;
        }

        results[0].Parse.Description!.SessionName = "mutated";
        results[0].Parse.Diagnostics.Add(new SdpDiagnostic { Code = "operation-local" });
        results[0].Parse.AppliedOptions.MaximumDiagnosticCount = 999;
        results[0].Write.Diagnostics.Add(new SdpDiagnostic { Code = "operation-local" });
        results[0].Write.AppliedOptions.MaximumDiagnosticCount = 999;
        results[0].Validation.Issues.Add(new SdpValidationIssue { Code = "operation-local" });
        results[0].Validation.AppliedOptions.MaximumDiagnosticCount = 999;
        results[0].Registration.References.Clear();
        SdpRegisteredToken freshRegistration = registry.GetMediaTypeRegistration(SdpMediaTypes.Audio);

        Assert.Multiple(() =>
        {
            Assert.That(results[1].Parse.Description!.SessionName, Is.EqualTo("Concurrent-1"));
            Assert.That(results[1].Parse.Diagnostics, Is.Empty);
            Assert.That(results[1].Parse.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(9));
            Assert.That(results[1].Write.Diagnostics, Is.Empty);
            Assert.That(results[1].Write.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(9));
            Assert.That(results[1].Validation.Issues, Is.Empty);
            Assert.That(results[1].Validation.AppliedOptions.MaximumDiagnosticCount, Is.EqualTo(9));
            Assert.That(results[1].Registration.References, Does.Contain("operation-1"));
            Assert.That(freshRegistration.Name, Is.EqualTo(SdpMediaTypes.Audio));
            Assert.That(freshRegistration.References, Is.EqualTo(new[] { "rfc:rfc8866" }));
            Assert.That(results[0].Parse.Description, Is.Not.SameAs(results[1].Parse.Description));
            Assert.That(results[0].Parse.Diagnostics, Is.Not.SameAs(results[1].Parse.Diagnostics));
            Assert.That(results[0].Write.Diagnostics, Is.Not.SameAs(results[1].Write.Diagnostics));
            Assert.That(results[0].Validation.Issues, Is.Not.SameAs(results[1].Validation.Issues));
            Assert.That(results[0].Registration.References, Is.Not.SameAs(results[1].Registration.References));
        });
    }

    static ConcurrentOperationResult RunOperation(int index, ManualResetEventSlim start, ISdpParser parser, ISdpWriter writer, ISdpValidator validator, ISdpRegistry registry)
    {
        start.Wait();
        int diagnosticLimit = index + 8;
        SdpParseResult parse = parser.ParseText(Input(index, "\r\n"), new SdpParseOptions { MaximumDiagnosticCount = diagnosticLimit });
        SdpLineEnding lineEnding = index % 2 == 0 ? SdpLineEnding.CrLf : SdpLineEnding.Lf;
        SdpWriteResult write = writer.WriteText(parse.Description!, new SdpWriteOptions
        {
            LineEnding = lineEnding,
            MaximumDiagnosticCount = diagnosticLimit
        });
        SdpValidationResult validation = validator.ValidateDescription(parse.Description!, new SdpValidationOptions { MaximumDiagnosticCount = diagnosticLimit });
        string mediaType = index % 2 == 0 ? SdpMediaTypes.Audio : SdpMediaTypes.Video;
        SdpRegisteredToken registration = registry.GetMediaTypeRegistration(mediaType);
        registration.Name = "operation-" + index;
        registration.References.Add("operation-" + index);
        return new ConcurrentOperationResult
        {
            Parse = parse,
            Write = write,
            Validation = validation,
            Registration = registration
        };
    }

    static string Input(int index, string lineEnding)
    {
        string identifier = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "v=0" + lineEnding
            + "o=- " + identifier + " " + identifier + " IN IP4 192.0.2.1" + lineEnding
            + "s=Concurrent-" + index + lineEnding
            + "c=IN IP4 192.0.2.1" + lineEnding
            + "t=0 0" + lineEnding
            + "a=x-operation:" + index + lineEnding;
    }

    class ConcurrentOperationResult
    {
        public SdpParseResult Parse = new SdpParseResult();
        public SdpWriteResult Write = new SdpWriteResult();
        public SdpValidationResult Validation = new SdpValidationResult();
        public SdpRegisteredToken Registration = new SdpRegisteredToken();
    }
}
