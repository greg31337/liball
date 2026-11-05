using System;

namespace fake;
/// <summary>
/// Provides deterministic checksum calculation, appending, and validation engines.
/// </summary>
public class ChecksumProvider : IChecksumProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    LuhnChecksum LuhnValue;
    VerhoeffChecksum VerhoeffValue;
    DammChecksum DammValue;
    Modulo10Checksum Modulo10Value;
    Modulo11Checksum Modulo11Value;
    Mod97Checksum Mod97Value;
    StandardIdentifierChecksum Isbn10Value;
    StandardIdentifierChecksum Isbn13Value;
    StandardIdentifierChecksum IssnValue;
    StandardIdentifierChecksum Ean8Value;
    StandardIdentifierChecksum UpcAValue;
    StandardIdentifierChecksum GtinValue;
    VinChecksum VinValue;
    internal ChecksumProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("checksum");
        int maximumCount = context.CapturedOptions.MaximumCount;
        int maximumOutputSize = context.CapturedOptions.MaximumOutputSize;
        LuhnValue = new LuhnChecksum(maximumCount, maximumOutputSize);
        VerhoeffValue = new VerhoeffChecksum(maximumCount, maximumOutputSize);
        DammValue = new DammChecksum(maximumCount, maximumOutputSize);
        Modulo10Value = new Modulo10Checksum(new int[] { 3, 1 }, maximumCount, maximumOutputSize);
        Modulo11Value = new Modulo11Checksum(new int[] { 2, 3, 4, 5, 6, 7 }, maximumCount, maximumOutputSize);
        Mod97Value = new Mod97Checksum(maximumCount, maximumOutputSize);
        Isbn10Value = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.Isbn10, maximumCount, maximumOutputSize);
        Isbn13Value = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.Isbn13, maximumCount, maximumOutputSize);
        IssnValue = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.Issn, maximumCount, maximumOutputSize);
        Ean8Value = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.Ean8, maximumCount, maximumOutputSize);
        UpcAValue = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.UpcA, maximumCount, maximumOutputSize);
        GtinValue = new StandardIdentifierChecksum(StandardIdentifierChecksumKind.Gtin, maximumCount, maximumOutputSize);
        VinValue = new VinChecksum(maximumCount, maximumOutputSize);
    }

    /// <summary>
    /// Returns the cached append-style checksum engine selected by kind.
    /// </summary>
    /// <remarks>
    /// Use the named methods when a concrete engine type or custom weights are
    /// required. VIN uses positional replacement rather than trailing append
    /// semantics and is therefore available only through <see cref="Vin"/>.
    /// </remarks>
    /// <param name="kind">The checksum algorithm or identifier contract.</param>
    /// <returns>The cached engine for this generator.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>kind</c> is invalid.</exception>
    public IChecksum Engine(ChecksumKind kind)
    {
        switch (kind)
        {
            case ChecksumKind.Luhn:
                return LuhnValue;
            case ChecksumKind.Verhoeff:
                return VerhoeffValue;
            case ChecksumKind.Damm:
                return DammValue;
            case ChecksumKind.Modulo10:
                return Modulo10Value;
            case ChecksumKind.Modulo11:
                return Modulo11Value;
            case ChecksumKind.Mod97:
                return Mod97Value;
            case ChecksumKind.Isbn10:
                return Isbn10Value;
            case ChecksumKind.Isbn13:
                return Isbn13Value;
            case ChecksumKind.Issn:
                return IssnValue;
            case ChecksumKind.Ean8:
                return Ean8Value;
            case ChecksumKind.UpcA:
                return UpcAValue;
            case ChecksumKind.Gtin:
                return GtinValue;
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "kind must identify a supported append-style checksum engine.");
        }
    }

    /// <summary>
    /// Returns the cached Luhn decimal checksum engine.
    /// </summary>
    /// <returns>The Luhn engine for this generator.</returns>
    public LuhnChecksum Luhn()
    {
        return LuhnValue;
    }

    /// <summary>
    /// Returns the cached Verhoeff decimal checksum engine.
    /// </summary>
    /// <returns>The Verhoeff engine for this generator.</returns>
    public VerhoeffChecksum Verhoeff()
    {
        return VerhoeffValue;
    }

    /// <summary>
    /// Returns the cached Damm decimal checksum engine.
    /// </summary>
    /// <returns>The Damm engine for this generator.</returns>
    public DammChecksum Damm()
    {
        return DammValue;
    }

    /// <summary>
    /// Returns a weighted modulo-10 engine using repeating right-anchored weights 3 and 1.
    /// </summary>
    /// <remarks>This default weighting is suitable for GTIN-family check digits; structural length validation belongs to the identifier provider.</remarks>
    /// <returns>The cached default weighted modulo-10 engine.</returns>
    public Modulo10Checksum Modulo10()
    {
        return Modulo10Value;
    }

    /// <summary>
    /// Returns a weighted modulo-10 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    public Modulo10Checksum Modulo10FromWeights(int[] weights)
    {
        Guard.NotNull(weights, "weights");
        return Modulo10FromWeightsUsingReadOnlySpanOfInt32(weights.AsSpan());
    }

    /// <summary>
    /// Returns a weighted modulo-10 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    public Modulo10Checksum Modulo10FromWeightsUsingReadOnlySpanOfInt32(ReadOnlySpan<int> weights)
    {
        return new Modulo10Checksum(ChecksumAlgorithms.Weights(weights, Context.CapturedOptions.MaximumCount), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a weighted modulo-11 engine using repeating right-anchored weights two through seven.
    /// </summary>
    /// <returns>The cached default weighted modulo-11 engine.</returns>
    public Modulo11Checksum Modulo11()
    {
        return Modulo11Value;
    }

    /// <summary>
    /// Returns a weighted modulo-11 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    public Modulo11Checksum Modulo11FromWeights(int[] weights)
    {
        Guard.NotNull(weights, "weights");
        return Modulo11FromWeightsUsingReadOnlySpanOfInt32(weights.AsSpan());
    }

    /// <summary>
    /// Returns a weighted modulo-11 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    public Modulo11Checksum Modulo11FromWeightsUsingReadOnlySpanOfInt32(ReadOnlySpan<int> weights)
    {
        return new Modulo11Checksum(ChecksumAlgorithms.Weights(weights, Context.CapturedOptions.MaximumCount), Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the cached ISO 7064 MOD 97-10 decimal checksum engine.
    /// </summary>
    /// <returns>The MOD 97-10 engine for this generator.</returns>
    public Mod97Checksum Mod97()
    {
        return Mod97Value;
    }

    /// <summary>
    /// Returns the cached compact ISBN-10 checksum engine.
    /// </summary>
    /// <returns>An engine requiring nine payload digits and accepting <c>X</c> as check value ten.</returns>
    public StandardIdentifierChecksum Isbn10()
    {
        return Isbn10Value;
    }

    /// <summary>
    /// Returns the cached compact ISBN-13 checksum engine.
    /// </summary>
    /// <returns>An engine requiring twelve payload digits beginning with 978 or 979.</returns>
    public StandardIdentifierChecksum Isbn13()
    {
        return Isbn13Value;
    }

    /// <summary>
    /// Returns the cached compact ISSN checksum engine.
    /// </summary>
    /// <returns>An engine requiring seven payload digits and accepting <c>X</c> as check value ten.</returns>
    public StandardIdentifierChecksum Issn()
    {
        return IssnValue;
    }

    /// <summary>
    /// Returns the cached compact EAN-8 checksum engine.
    /// </summary>
    /// <returns>An engine requiring seven payload digits.</returns>
    public StandardIdentifierChecksum Ean8()
    {
        return Ean8Value;
    }

    /// <summary>
    /// Returns the cached compact UPC-A checksum engine.
    /// </summary>
    /// <returns>An engine requiring eleven payload digits.</returns>
    public StandardIdentifierChecksum UpcA()
    {
        return UpcAValue;
    }

    /// <summary>
    /// Returns the cached compact GTIN checksum engine.
    /// </summary>
    /// <returns>An engine supporting GTIN payload lengths 7, 11, 12, and 13.</returns>
    public StandardIdentifierChecksum Gtin()
    {
        return GtinValue;
    }

    /// <summary>
    /// Returns the cached 49 CFR Part 565 VIN checksum engine.
    /// </summary>
    /// <returns>The VIN checksum engine for this generator.</returns>
    public VinChecksum Vin()
    {
        return VinValue;
    }
}
