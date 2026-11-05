using System;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="ChecksumProvider"/>.</summary>
public interface IChecksumProvider
{
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
    IChecksum Engine(ChecksumKind kind);
    /// <summary>
    /// Returns the cached Luhn decimal checksum engine.
    /// </summary>
    /// <returns>The Luhn engine for this generator.</returns>
    LuhnChecksum Luhn();
    /// <summary>
    /// Returns the cached Verhoeff decimal checksum engine.
    /// </summary>
    /// <returns>The Verhoeff engine for this generator.</returns>
    VerhoeffChecksum Verhoeff();
    /// <summary>
    /// Returns the cached Damm decimal checksum engine.
    /// </summary>
    /// <returns>The Damm engine for this generator.</returns>
    DammChecksum Damm();
    /// <summary>
    /// Returns a weighted modulo-10 engine using repeating right-anchored weights 3 and 1.
    /// </summary>
    /// <remarks>This default weighting is suitable for GTIN-family check digits; structural length validation belongs to the identifier provider.</remarks>
    /// <returns>The cached default weighted modulo-10 engine.</returns>
    Modulo10Checksum Modulo10();
    /// <summary>
    /// Returns a weighted modulo-10 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    Modulo10Checksum Modulo10FromWeights(int[] weights);
    /// <summary>
    /// Returns a weighted modulo-10 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    Modulo10Checksum Modulo10FromWeightsUsingReadOnlySpanOfInt32(ReadOnlySpan<int> weights);
    /// <summary>
    /// Returns a weighted modulo-11 engine using repeating right-anchored weights two through seven.
    /// </summary>
    /// <returns>The cached default weighted modulo-11 engine.</returns>
    Modulo11Checksum Modulo11();
    /// <summary>
    /// Returns a weighted modulo-11 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    Modulo11Checksum Modulo11FromWeights(int[] weights);
    /// <summary>
    /// Returns a weighted modulo-11 engine with explicit repeating right-anchored weights.
    /// </summary>
    /// <param name="weights">Positive weights, beginning with the weight for the rightmost payload digit.</param>
    /// <returns>A new engine containing an independent copy of the weights.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a weight or count is outside configured limits.</exception>
    Modulo11Checksum Modulo11FromWeightsUsingReadOnlySpanOfInt32(ReadOnlySpan<int> weights);
    /// <summary>
    /// Returns the cached ISO 7064 MOD 97-10 decimal checksum engine.
    /// </summary>
    /// <returns>The MOD 97-10 engine for this generator.</returns>
    Mod97Checksum Mod97();
    /// <summary>
    /// Returns the cached compact ISBN-10 checksum engine.
    /// </summary>
    /// <returns>An engine requiring nine payload digits and accepting <c>X</c> as check value ten.</returns>
    StandardIdentifierChecksum Isbn10();
    /// <summary>
    /// Returns the cached compact ISBN-13 checksum engine.
    /// </summary>
    /// <returns>An engine requiring twelve payload digits beginning with 978 or 979.</returns>
    StandardIdentifierChecksum Isbn13();
    /// <summary>
    /// Returns the cached compact ISSN checksum engine.
    /// </summary>
    /// <returns>An engine requiring seven payload digits and accepting <c>X</c> as check value ten.</returns>
    StandardIdentifierChecksum Issn();
    /// <summary>
    /// Returns the cached compact EAN-8 checksum engine.
    /// </summary>
    /// <returns>An engine requiring seven payload digits.</returns>
    StandardIdentifierChecksum Ean8();
    /// <summary>
    /// Returns the cached compact UPC-A checksum engine.
    /// </summary>
    /// <returns>An engine requiring eleven payload digits.</returns>
    StandardIdentifierChecksum UpcA();
    /// <summary>
    /// Returns the cached compact GTIN checksum engine.
    /// </summary>
    /// <returns>An engine supporting GTIN payload lengths 7, 11, 12, and 13.</returns>
    StandardIdentifierChecksum Gtin();
    /// <summary>
    /// Returns the cached 49 CFR Part 565 VIN checksum engine.
    /// </summary>
    /// <returns>The VIN checksum engine for this generator.</returns>
    VinChecksum Vin();
}
