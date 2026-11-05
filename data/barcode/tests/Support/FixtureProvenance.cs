namespace barcode.Tests;

internal class FixtureProvenance
{
    internal string FixtureId = "";
    internal string Symbology = "";
    internal string Origin = "";
    internal string Source = "";
    internal string SourceVersion = "";
    internal string Generator = "";
    internal string GeneratorVersion = "";
    internal string Oracle = "";
    internal string Reference = "";
    internal string License = "";
    internal string Sha256 = "";
    internal string Notes = "";

    internal void RecordSha256(byte[] content)
    {
        Sha256 = CalculateSha256(content);
    }

    internal void VerifySha256(byte[] content)
    {
        string actual = CalculateSha256(content);
        if (!string.Equals(
            Sha256,
            actual,
            global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new global::System.InvalidOperationException(
                $"Fixture '{FixtureId}' does not match its recorded SHA-256 digest.");
        }
    }

    internal void ValidateRequiredMetadata()
    {
        RequireValue(FixtureId, "FixtureId");
        RequireValue(Symbology, "Symbology");
        RequireValue(Origin, "Origin");
        RequireValue(Source, "Source");
        RequireValue(SourceVersion, "SourceVersion");
        RequireValue(Oracle, "Oracle");
        RequireValue(Reference, "Reference");
        RequireValue(License, "License");
        RequireValue(Sha256, "Sha256");
    }

    internal static string CalculateSha256(byte[] content)
    {
        byte[] digest = global::System.Security.Cryptography.SHA256.HashData(
            content);
        return global::System.Convert.ToHexString(digest).ToLowerInvariant();
    }

    static void RequireValue(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new global::System.InvalidOperationException(
                $"Fixture provenance field '{field}' is required.");
        }
    }
}

internal class BarcodeTestFixture<T>
{
    internal T Value;
    internal FixtureProvenance Provenance;

    internal BarcodeTestFixture(T value, FixtureProvenance provenance)
    {
        Value = value;
        Provenance = provenance;
    }

    internal T RequireVerifiedValue(byte[] serializedContent)
    {
        Provenance.ValidateRequiredMetadata();
        Provenance.VerifySha256(serializedContent);
        return Value;
    }
}
