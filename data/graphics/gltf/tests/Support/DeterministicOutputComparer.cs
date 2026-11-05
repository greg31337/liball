using System.Globalization;

namespace gltf.Tests.Support;

internal sealed class DeterministicByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? left, byte[]? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        return left.AsSpan().SequenceEqual(right);
    }

    public int GetHashCode(byte[] value)
    {
        uint hash = 2166136261;
        foreach (byte item in value)
        {
            hash ^= item;
            hash *= 16777619;
        }

        return (int)hash;
    }
}

internal static class DeterministicOutputComparer
{
    internal static void AssertWriteResultsEqual(
        GltfWriteResult expected,
        GltfWriteResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.PrimaryBytes, Is.EqualTo(expected.PrimaryBytes));
            Assert.That(actual.Resources.Resources.Select(resource => resource.Bytes),
                Is.EqualTo(expected.Resources.Resources.Select(resource => resource.Bytes)));
            Assert.That(actual.Manifest.Select(ManifestKey),
                Is.EqualTo(expected.Manifest.Select(ManifestKey)));
            Assert.That(actual.Diagnostics.Select(DiagnosticKey),
                Is.EqualTo(expected.Diagnostics.Select(DiagnosticKey)));
        });
    }

    internal static void AssertConversionResultsEqual(
        GltfConversionResult expected,
        GltfConversionResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.PrimaryBytes, Is.EqualTo(expected.PrimaryBytes));
            Assert.That(actual.PrimaryContentHash,
                Is.EqualTo(expected.PrimaryContentHash));
            Assert.That(actual.Resources.Resources.Select(resource => resource.Bytes),
                Is.EqualTo(expected.Resources.Resources.Select(resource => resource.Bytes)));
            Assert.That(actual.Manifest.Select(ManifestKey),
                Is.EqualTo(expected.Manifest.Select(ManifestKey)));
            Assert.That(actual.Diagnostics.Select(DiagnosticKey),
                Is.EqualTo(expected.Diagnostics.Select(DiagnosticKey)));
        });
    }

    private static string ManifestKey(GltfResourceManifestEntry entry)
    {
        return string.Join(
            "|",
            ((int)entry.Kind).ToString(CultureInfo.InvariantCulture),
            entry.EffectiveUri,
            entry.MimeType,
            entry.ByteLength.ToString(CultureInfo.InvariantCulture),
            entry.ContentHash,
            entry.Embedded ? "embedded" : "external",
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
            issue.ResourceUri);
    }
}
