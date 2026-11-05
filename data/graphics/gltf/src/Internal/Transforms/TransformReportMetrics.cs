using System.Collections.Generic;
using System.Security.Cryptography;

namespace gltf;

internal class TransformMetricSnapshot
{
    internal long ByteLength;
    internal string Hash = "";
}

internal class TransformReportMetrics
{
    internal TransformMetricSnapshot CreateMetricSnapshot(
        GltfDocument document,
        GltfTransformOptions options)
    {
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Json;
        writeOptions.ResourceMode = GltfResourceMode.Preserve;
        writeOptions.Indented = false;
        writeOptions.Deterministic = true;
        writeOptions.Validate = false;
        writeOptions.ValidationOptions = options.ValidationOptions;
        CopyExtensionHandlers(
            options.ValidationOptions.ExtensionHandlers,
            writeOptions.ExtensionHandlers);
        GltfWriteResult output = new GltfJsonWriter().WriteOutput(
            document,
            writeOptions,
            false);
        byte[] hash = SHA256.HashData(output.PrimaryBytes);
        TransformMetricSnapshot result = new TransformMetricSnapshot();
        result.ByteLength = output.PrimaryBytes.LongLength;
        result.Hash = System.Convert.ToHexString(hash).ToLowerInvariant();
        return result;
    }

    internal void SetOriginalMetrics(
        GltfTransformReport report,
        TransformMetricSnapshot snapshot)
    {
        report.OriginalByteLength = snapshot.ByteLength;
        report.OriginalHash = snapshot.Hash;
    }

    internal void SetResultMetrics(
        GltfTransformReport report,
        TransformMetricSnapshot snapshot)
    {
        report.ResultByteLength = snapshot.ByteLength;
        report.ResultHash = snapshot.Hash;
    }

    internal void SetRolledBackMetrics(GltfTransformReport report)
    {
        report.ResultByteLength = report.OriginalByteLength;
        report.ResultHash = report.OriginalHash;
    }

    internal void CopyExtensionHandlers(
        List<IGltfExtensionHandler> source,
        List<IGltfExtensionHandler> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
