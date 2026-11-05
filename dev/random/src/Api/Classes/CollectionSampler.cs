using System;
using System.Collections;

namespace random;

/// <summary>
/// Provides random selection and reordering operations over arrays, lists, and streams.
/// </summary>
/// <remarks>
/// Every operation uses the retained generator. Results depend on collection order and
/// the generator call sequence. In-place operations require mutable inputs. This class
/// is not safe for concurrent use without external synchronization.
/// </remarks>
public class CollectionSampler : ICollectionSampler
{
    IRandomSource generator;

    /// <summary>
    /// Creates a collection sampler backed by a random source.
    /// </summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public CollectionSampler(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        this.generator = generator;
    }

    /// <summary>Chooses one value uniformly from a nonempty one-dimensional array.</summary>
    /// <param name="values">The source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The array is null, empty, or not one-dimensional.</exception>
    public object? ChooseFromArray(Array values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one value uniformly from a nonempty list.</summary>
    /// <param name="values">The source list.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The list is null or empty.</exception>
    public object? ChooseFromList(IList values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Fills an array with independent uniform choices from an array.</summary>
    /// <param name="values">The nonempty one-dimensional source array.</param>
    /// <param name="destination">The one-dimensional destination array.</param>
    /// <remarks>The arrays may be the same; every choice uses the original source values.</remarks>
    /// <exception cref="RandomArgumentException">An array is invalid or a source element cannot be stored in the destination.</exception>
    public void FillChoicesFromArray(Array values, Array destination)
    {
        RandomCollections.FillChoices(generator, values, destination);
    }

    /// <summary>Fills an array with independent uniform choices from a list.</summary>
    /// <param name="values">The nonempty source list.</param>
    /// <param name="destination">The one-dimensional destination array.</param>
    /// <remarks>Every choice is captured before destination writes.</remarks>
    /// <exception cref="RandomArgumentException">An input is invalid or a source element cannot be stored in the destination.</exception>
    public void FillChoicesFromList(IList values, Array destination)
    {
        RandomCollections.FillChoices(generator, values, destination);
    }

    /// <summary>Shuffles a one-dimensional array in place.</summary>
    /// <param name="values">The mutable array.</param>
    /// <exception cref="RandomArgumentException">The array is null or not one-dimensional.</exception>
    public void ShuffleArray(Array values)
    {
        RandomCollections.Shuffle(generator, values);
    }

    /// <summary>Shuffles a mutable list in place.</summary>
    /// <param name="values">The mutable list.</param>
    /// <exception cref="RandomArgumentException">The list is null or read-only.</exception>
    public void ShuffleList(IList values)
    {
        RandomCollections.Shuffle(generator, values);
    }

    /// <summary>Moves a uniform ordered sample into the requested array prefix.</summary>
    /// <param name="values">The one-dimensional array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An input is invalid or the count is outside the array bounds.</exception>
    public void PartialShuffleArray(Array values, int count)
    {
        RandomCollections.PartialShuffle(generator, values, count);
    }

    /// <summary>Moves a uniform ordered sample into the requested mutable-list prefix.</summary>
    /// <param name="values">The mutable list to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An input is invalid or the count is outside the list bounds.</exception>
    public void PartialShuffleList(IList values, int count)
    {
        RandomCollections.PartialShuffle(generator, values, count);
    }

    /// <summary>Returns a shuffled copy of a one-dimensional array.</summary>
    /// <param name="values">The source array.</param>
    /// <returns>A shuffled array with the same runtime type and bounds.</returns>
    /// <exception cref="RandomArgumentException">The array is null or not one-dimensional.</exception>
    public Array ShuffledArrayCopy(Array values)
    {
        return RandomCollections.ShuffledCopy(generator, values);
    }

    /// <summary>Returns a shuffled object-array copy of a list.</summary>
    /// <param name="values">The source list.</param>
    /// <returns>A shuffled copy containing the list values.</returns>
    /// <exception cref="RandomArgumentException">The list is null.</exception>
    public object?[] ShuffledListCopy(IList values)
    {
        return RandomCollections.ShuffledCopy(generator, values);
    }

    /// <summary>Returns a uniform permutation of the integers from zero to count exclusive.</summary>
    /// <param name="count">The nonnegative permutation length.</param>
    /// <returns>The shuffled permutation.</returns>
    /// <exception cref="RandomArgumentException">The count is negative.</exception>
    public int[] Permutation(int count)
    {
        return RandomCollections.Permutation(generator, count);
    }

    /// <summary>Returns a uniform subset of ascending integer indexes.</summary>
    /// <param name="populationSize">The nonnegative exclusive upper index bound.</param>
    /// <param name="count">The number of indexes to select.</param>
    /// <returns>The selected distinct indexes in ascending order.</returns>
    /// <exception cref="RandomArgumentException">The population or count is invalid.</exception>
    public int[] Subset(int populationSize, int count)
    {
        return RandomCollections.Subset(generator, populationSize, count);
    }

    /// <summary>Returns a uniform combination of ascending integer indexes.</summary>
    /// <param name="populationSize">The nonnegative exclusive upper index bound.</param>
    /// <param name="count">The number of indexes to select.</param>
    /// <returns>The selected distinct indexes in ascending order.</returns>
    /// <exception cref="RandomArgumentException">The population or count is invalid.</exception>
    public int[] Combination(int populationSize, int count)
    {
        return RandomCollections.Combination(generator, populationSize, count);
    }

    /// <summary>Returns a uniform permutation with no fixed points.</summary>
    /// <param name="count">The derangement length.</param>
    /// <returns>A permutation in which each value differs from its index.</returns>
    /// <exception cref="RandomArgumentException">The count is negative or is one.</exception>
    public int[] Derangement(int count)
    {
        return RandomCollections.Derangement(generator, count);
    }

    /// <summary>Returns an ordered sample of distinct positions from an array.</summary>
    /// <param name="values">The one-dimensional source array.</param>
    /// <param name="count">The number of values to sample.</param>
    /// <returns>An array with the source runtime element type.</returns>
    /// <exception cref="RandomArgumentException">An input is invalid or the count is outside the source bounds.</exception>
    public Array SampleArrayWithoutReplacement(Array values, int count)
    {
        return RandomCollections.SampleWithoutReplacement(generator, values, count);
    }

    /// <summary>Returns an ordered sample of distinct positions from a list.</summary>
    /// <param name="values">The source list.</param>
    /// <param name="count">The number of values to sample.</param>
    /// <returns>The sampled list values.</returns>
    /// <exception cref="RandomArgumentException">An input is invalid or the count is outside the source bounds.</exception>
    public object?[] SampleListWithoutReplacement(IList values, int count)
    {
        return RandomCollections.SampleWithoutReplacement(generator, values, count);
    }

    /// <summary>Returns a one-pass Algorithm R reservoir sample from a stream.</summary>
    /// <param name="values">The source sequence.</param>
    /// <param name="count">The number of values to sample.</param>
    /// <returns>The completed reservoir.</returns>
    /// <remarks>A zero count returns without requesting an enumerator.</remarks>
    /// <exception cref="RandomArgumentException">An input is invalid or the source has fewer values than requested.</exception>
    /// <exception cref="RandomStateException">The traversal exceeds the supported signed 64-bit count.</exception>
    public object?[] ReservoirSample(IEnumerable values, int count)
    {
        return RandomCollections.ReservoirSample(generator, values, count);
    }

    /// <summary>Returns an Algorithm R reservoir sample from a random-access list.</summary>
    /// <param name="values">The source list.</param>
    /// <param name="count">The number of values to sample.</param>
    /// <returns>The completed reservoir.</returns>
    /// <exception cref="RandomArgumentException">An input is invalid or the count is outside the source bounds.</exception>
    public object?[] ReservoirSampleList(IList values, int count)
    {
        return RandomCollections.ReservoirSample(generator, values, count);
    }

    /// <summary>Chooses one Boolean uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public bool ChooseBooleanArray(bool[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one unsigned byte uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public byte ChooseByteArray(byte[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one signed byte uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public sbyte ChooseSByteArray(sbyte[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one signed 16-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public short ChooseInt16Array(short[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one unsigned 16-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public ushort ChooseUInt16Array(ushort[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one signed 32-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public int ChooseInt32Array(int[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one unsigned 32-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public uint ChooseUInt32Array(uint[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one signed 64-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public long ChooseInt64Array(long[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one unsigned 64-bit integer uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public ulong ChooseUInt64Array(ulong[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one character uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public char ChooseCharacterArray(char[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one single-precision value uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public float ChooseSingleArray(float[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one double-precision value uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public double ChooseDoubleArray(double[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one decimal value uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    public decimal ChooseDecimalArray(decimal[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one string uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value, which may be null.</returns>
    public string? ChooseStringArray(string?[] values)
    {
        return RandomCollections.Choose(generator, values);
    }

    /// <summary>Chooses one object uniformly from an array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value, which may be null.</returns>
    public object? ChooseObjectArray(object?[] values)
    {
        return RandomCollections.Choose(generator, values);
    }
}
