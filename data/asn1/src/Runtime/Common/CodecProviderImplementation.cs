namespace asn1;

internal class CodecProviderImplementation : IAsnCodecProvider
{
    public IAsnCodec CreateCodec(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        if (catalog == null)
        {
            throw new System.ArgumentNullException("catalog");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!this.IsSupportedRule(options.EncodingRules))
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        return new ConfiguredCodec(
            new CatalogSnapshotImplementation().CreateSnapshot(catalog),
            this.CloneOptions(options));
    }

    internal bool IsSupportedRule(AsnEncodingRules rules)
    {
        return rules >= AsnEncodingRules.Ber
            && rules <= AsnEncodingRules.Ecn;
    }

    internal AsnCodecOptions CloneOptions(AsnCodecOptions source)
    {
        return new AsnCodecOptions
        {
            EncodingRules = source.EncodingRules,
            UnknownDataPolicy = source.UnknownDataPolicy,
            ValidateValues = source.ValidateValues,
            UseIndefiniteLengths = source.UseIndefiniteLengths,
            ResourceLimits = source.ResourceLimits == null
                ? null
                : this.CloneResourceLimits(source.ResourceLimits),
            ObjectSetRegistry = source.ObjectSetRegistry,
            PerInstructionRegistry = this.SnapshotPerInstructionRegistry(
                source.PerInstructionRegistry)
        };
    }

    internal IPerInstructionRegistry? SnapshotPerInstructionRegistry(
        IPerInstructionRegistry? registry)
    {
        return registry == null
            ? null
            : new PerInstructionRegistrySnapshotBuilder()
                .CreateSnapshot(registry).Snapshot;
    }

    internal AsnResourceLimits CloneResourceLimits(AsnResourceLimits source)
    {
        return new AsnResourceLimits
        {
            Unlimited = source.Unlimited,
            MaximumNestingDepth = source.MaximumNestingDepth,
            MaximumCollectionElements = source.MaximumCollectionElements,
            MaximumTagOctets = source.MaximumTagOctets,
            MaximumLengthOctets = source.MaximumLengthOctets,
            MaximumIntegerBits = source.MaximumIntegerBits,
            MaximumTextCharacters = source.MaximumTextCharacters,
            MaximumOpenTypeBytes = source.MaximumOpenTypeBytes,
            MaximumTotalAllocationBytes = source.MaximumTotalAllocationBytes,
            MaximumDiagnostics = source.MaximumDiagnostics
        };
    }
}
