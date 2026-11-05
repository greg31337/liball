using System.Globalization;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
[NonParallelizable]
public class GltfDeterministicOutputTests
{
    [Test]
    public void ReadValidateAndWriteRemainStableAcrossCultures()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        byte[] json = System.Text.Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},"
            + "\"nodes\":[{\"translation\":[1234.5,-0.125,602.0]}],"
            + "\"scenes\":[{\"nodes\":[0]}],\"scene\":0}");
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Validate = false;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            GltfReadResult expectedRead = ReadValidated(json);
            GltfWriteResult expectedWrite = new GltfWriter().WriteBytes(
                expectedRead.Document!,
                writeOptions);
            string[] cultures = ["fr-FR", "tr-TR", "ar-SA"];
            int cultureIndex = 0;
            while (cultureIndex < cultures.Length)
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultures[cultureIndex]);
                CultureInfo.CurrentUICulture = new CultureInfo(cultures[cultureIndex]);
                GltfReadResult actualRead = ReadValidated(json);
                GltfWriteResult actualWrite = new GltfWriter().WriteBytes(
                    actualRead.Document!,
                    writeOptions);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        actualRead.Document!.Nodes[0].Translation,
                        Is.EqualTo(new double[] { 1234.5, -0.125, 602.0 }),
                        cultures[cultureIndex]);
                    Assert.That(
                        actualRead.Validation.Issues.Select(DiagnosticKey),
                        Is.EqualTo(expectedRead.Validation.Issues.Select(DiagnosticKey)),
                        cultures[cultureIndex]);
                });
                AssertWriteResultsEqual(expectedWrite, actualWrite);
                cultureIndex++;
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void WriteBytes_RepeatsAllOwnedOutputsAcrossCultures()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        GltfDocument document = CreateDocument();
        GltfWriteOptions options = CreateWriteOptions();
        GltfWriter writer = new GltfWriter();

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            GltfWriteResult expected = writer.WriteBytes(document, options);
            string[] cultures = ["fr-FR", "tr-TR", "ar-SA", "en-US"];
            int index = 0;
            while (index < cultures.Length)
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultures[index]);
                CultureInfo.CurrentUICulture = new CultureInfo(cultures[index]);
                GltfWriteResult actual = writer.WriteBytes(document, options);
                AssertWriteResultsEqual(expected, actual);
                index++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(expected.Diagnostics.Select(issue => issue.Code),
                    Is.EqualTo(new[] { "A_DIAGNOSTIC", "Z_DIAGNOSTIC" }));
                Assert.That(expected.Manifest.Select(entry => entry.Uri),
                    Is.EqualTo(new[]
                    {
                        "Resources/FILE.bin",
                        "Resources/FILE-1.bin"
                    }));
                Assert.That(document.Resources.Resources.All(resource =>
                    resource.EffectiveUri == null), Is.True);
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void ConvertDocument_RepeatsAllOwnedOutputsAcrossCultures()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        GltfDocument document = CreateDocument();
        GltfConversionOptions options = new GltfConversionOptions();
        options.InputFormat = GltfContainerFormat.Json;
        options.OutputFormat = GltfContainerFormat.Json;
        options.ResourceMode = GltfResourceMode.External;
        options.WriteOptions = CreateWriteOptions();
        GltfConverter converter = new GltfConverter();

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            GltfConversionResult expected = converter.ConvertDocument(
                document,
                options);
            string[] cultures = ["fr-FR", "tr-TR", "ar-SA", "en-US"];
            int index = 0;
            while (index < cultures.Length)
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultures[index]);
                CultureInfo.CurrentUICulture = new CultureInfo(cultures[index]);
                GltfConversionResult actual = converter.ConvertDocument(
                    document,
                    options);
                AssertConversionResultsEqual(expected, actual);
                index++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(expected.PrimaryContentHash,
                    Does.Match("^[0-9a-f]{64}$"));
                Assert.That(expected.Manifest.All(entry =>
                    entry.ContentHash != null
                    && entry.ContentHash.Length == 64), Is.True);
                Assert.That(expected.Diagnostics.Select(issue => issue.Code),
                    Is.EqualTo(new[] { "A_DIAGNOSTIC", "Z_DIAGNOSTIC" }));
                Assert.That(document.Resources.Resources.All(resource =>
                    resource.EffectiveUri == null), Is.True);
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static GltfDocument CreateDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        result.PropertyJson.Extensions["EXT_deterministic"] =
            new JsonObject
            {
                ["decimal"] = 1234.5,
                ["alpha"] = true
            };
        result.ExtensionsUsed.Add("EXT_deterministic");
        AddBuffer(result, [1, 2, 3, 4]);
        AddBuffer(result, [5, 6, 7, 8]);
        return result;
    }

    private static GltfReadResult ReadValidated(byte[] json)
    {
        GltfReadOptions options = new GltfReadOptions();
        options.Validate = true;
        GltfReadResult result = new GltfReader().ReadBytes(json, options);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Validation.IsValid, Is.True);
        });
        return result;
    }

    private static void AddBuffer(GltfDocument document, byte[] bytes)
    {
        int resourceIndex = document.Resources.Resources.Count;
        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Buffer;
        resource.UriKind = GltfUriKind.Relative;
        resource.SourceUri = "Resources/FILE.BIN";
        resource.MimeType = GltfMimeTypes.GltfBuffer;
        resource.Bytes = bytes;
        document.Resources.Resources.Add(resource);
        GltfBuffer buffer = new GltfBuffer();
        buffer.Uri = resource.SourceUri;
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        buffer.HasResource = true;
        buffer.Resource = resourceIndex;
        document.Buffers.Add(buffer);
    }

    private static GltfWriteOptions CreateWriteOptions()
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = GltfContainerFormat.Json;
        result.ResourceMode = GltfResourceMode.External;
        result.Deterministic = true;
        result.Validate = false;
        result.ExtensionHandlers.Add(new DiagnosticExtensionHandler());
        return result;
    }

    private static void AssertWriteResultsEqual(
        GltfWriteResult expected,
        GltfWriteResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.PrimaryBytes, Is.EqualTo(expected.PrimaryBytes));
            Assert.That(actual.Resources.Resources.Select(resource =>
                resource.Bytes), Is.EqualTo(expected.Resources.Resources.Select(
                    resource => resource.Bytes)));
            Assert.That(actual.Resources.Resources.Select(ResourceName),
                Is.EqualTo(expected.Resources.Resources.Select(ResourceName)));
            Assert.That(actual.Manifest.Select(ManifestKey),
                Is.EqualTo(expected.Manifest.Select(ManifestKey)));
            Assert.That(actual.Diagnostics.Select(DiagnosticKey),
                Is.EqualTo(expected.Diagnostics.Select(DiagnosticKey)));
        });
    }

    private static void AssertConversionResultsEqual(
        GltfConversionResult expected,
        GltfConversionResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.PrimaryBytes, Is.EqualTo(expected.PrimaryBytes));
            Assert.That(actual.PrimaryContentHash,
                Is.EqualTo(expected.PrimaryContentHash));
            Assert.That(actual.Resources.Resources.Select(resource =>
                resource.Bytes), Is.EqualTo(expected.Resources.Resources.Select(
                    resource => resource.Bytes)));
            Assert.That(actual.Resources.Resources.Select(ResourceName),
                Is.EqualTo(expected.Resources.Resources.Select(ResourceName)));
            Assert.That(actual.Manifest.Select(ManifestKey),
                Is.EqualTo(expected.Manifest.Select(ManifestKey)));
            Assert.That(actual.Diagnostics.Select(DiagnosticKey),
                Is.EqualTo(expected.Diagnostics.Select(DiagnosticKey)));
            Assert.That(actual.Decisions.Select(DecisionKey),
                Is.EqualTo(expected.Decisions.Select(DecisionKey)));
        });
    }

    private static string? ResourceName(GltfResource resource)
    {
        return resource.EffectiveUri ?? resource.SourceUri;
    }

    private static string ManifestKey(GltfResourceManifestEntry entry)
    {
        return string.Join(
            "|",
            ((int)entry.Kind).ToString(CultureInfo.InvariantCulture),
            entry.Uri,
            entry.EffectiveUri,
            entry.MimeType,
            entry.ByteLength.ToString(CultureInfo.InvariantCulture),
            entry.ContentHash,
            entry.Embedded ? "embedded" : "not-embedded",
            entry.External ? "external" : "not-external",
            ((int)entry.Mode).ToString(CultureInfo.InvariantCulture));
    }

    private static string DiagnosticKey(GltfValidationIssue issue)
    {
        return string.Join(
            "|",
            issue.JsonPointer,
            issue.Code,
            ((int)issue.Severity).ToString(CultureInfo.InvariantCulture),
            issue.Message,
            issue.ResourceUri,
            issue.HasByteOffset ? "offset" : "no-offset",
            issue.ByteOffset.ToString(CultureInfo.InvariantCulture),
            string.Join(",", issue.RelatedJsonPointers));
    }

    private static string DecisionKey(GltfConversionDecision decision)
    {
        return string.Join(
            "|",
            ((int)decision.Kind).ToString(CultureInfo.InvariantCulture),
            decision.Name,
            ((int)decision.Action).ToString(CultureInfo.InvariantCulture),
            decision.Required ? "required" : "optional",
            decision.RelatedExtension,
            ((int)decision.Status).ToString(CultureInfo.InvariantCulture),
            decision.Detail);
    }

    private sealed class DiagnosticExtensionHandler : IGltfExtensionHandler
    {
        public GltfExtensionDescriptor DescribeExtension()
        {
            GltfExtensionDescriptor result = new GltfExtensionDescriptor();
            result.Name = "EXT_deterministic";
            result.AllowsFallback = true;
            return result;
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            GltfExtensionResult result = CreateResult(context);
            result.Diagnostics.Add(CreateIssue("Z_DIAGNOSTIC"));
            result.Diagnostics.Add(CreateIssue("A_DIAGNOSTIC"));
            return result;
        }

        public GltfExtensionResult ValidateExtension(
            GltfExtensionContext context)
        {
            return CreateResult(context);
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            return CreateResult(context);
        }

        private static GltfExtensionResult CreateResult(
            GltfExtensionContext context)
        {
            GltfExtensionResult result = new GltfExtensionResult();
            result.Json = context.Json;
            return result;
        }

        private static GltfValidationIssue CreateIssue(string code)
        {
            GltfValidationIssue result = new GltfValidationIssue();
            result.Code = code;
            result.Severity = GltfValidationSeverity.Warning;
            result.Message = "deterministic diagnostic";
            result.JsonPointer = "/extensions/EXT_deterministic";
            return result;
        }
    }
}
