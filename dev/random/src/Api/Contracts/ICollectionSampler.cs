using System;
using System.Collections;

namespace random;

/// <summary>
/// Defines random selection and reordering operations over arrays, lists, and streams.
/// </summary>
public interface ICollectionSampler
{
    /// <summary>Chooses one value uniformly from an array.</summary>
    object? ChooseFromArray(Array values);

    /// <summary>Chooses one value uniformly from a list.</summary>
    object? ChooseFromList(IList values);

    /// <summary>Fills an array with independent uniform choices from an array.</summary>
    void FillChoicesFromArray(Array values, Array destination);

    /// <summary>Fills an array with independent uniform choices from a list.</summary>
    void FillChoicesFromList(IList values, Array destination);

    /// <summary>Shuffles an array in place.</summary>
    void ShuffleArray(Array values);

    /// <summary>Shuffles a mutable list in place.</summary>
    void ShuffleList(IList values);

    /// <summary>Moves a uniform ordered sample into an array prefix.</summary>
    void PartialShuffleArray(Array values, int count);

    /// <summary>Moves a uniform ordered sample into a mutable list prefix.</summary>
    void PartialShuffleList(IList values, int count);

    /// <summary>Returns a shuffled copy of an array.</summary>
    Array ShuffledArrayCopy(Array values);

    /// <summary>Returns a shuffled object-array copy of a list.</summary>
    object?[] ShuffledListCopy(IList values);

    /// <summary>Returns a shuffled permutation of consecutive indexes.</summary>
    int[] Permutation(int count);

    /// <summary>Returns an ascending uniform subset of indexes.</summary>
    int[] Subset(int populationSize, int count);

    /// <summary>Returns an ascending uniform combination of indexes.</summary>
    int[] Combination(int populationSize, int count);

    /// <summary>Returns a uniform permutation with no fixed points.</summary>
    int[] Derangement(int count);

    /// <summary>Returns an ordered sample of distinct array positions.</summary>
    Array SampleArrayWithoutReplacement(Array values, int count);

    /// <summary>Returns an ordered sample of distinct list positions.</summary>
    object?[] SampleListWithoutReplacement(IList values, int count);

    /// <summary>Returns a one-pass reservoir sample from a stream.</summary>
    object?[] ReservoirSample(IEnumerable values, int count);

    /// <summary>Returns a reservoir sample from a random-access list.</summary>
    object?[] ReservoirSampleList(IList values, int count);

    /// <summary>Chooses one Boolean from an array.</summary>
    bool ChooseBooleanArray(bool[] values);

    /// <summary>Chooses one unsigned byte from an array.</summary>
    byte ChooseByteArray(byte[] values);

    /// <summary>Chooses one signed byte from an array.</summary>
    sbyte ChooseSByteArray(sbyte[] values);

    /// <summary>Chooses one signed 16-bit integer from an array.</summary>
    short ChooseInt16Array(short[] values);

    /// <summary>Chooses one unsigned 16-bit integer from an array.</summary>
    ushort ChooseUInt16Array(ushort[] values);

    /// <summary>Chooses one signed 32-bit integer from an array.</summary>
    int ChooseInt32Array(int[] values);

    /// <summary>Chooses one unsigned 32-bit integer from an array.</summary>
    uint ChooseUInt32Array(uint[] values);

    /// <summary>Chooses one signed 64-bit integer from an array.</summary>
    long ChooseInt64Array(long[] values);

    /// <summary>Chooses one unsigned 64-bit integer from an array.</summary>
    ulong ChooseUInt64Array(ulong[] values);

    /// <summary>Chooses one character from an array.</summary>
    char ChooseCharacterArray(char[] values);

    /// <summary>Chooses one single-precision value from an array.</summary>
    float ChooseSingleArray(float[] values);

    /// <summary>Chooses one double-precision value from an array.</summary>
    double ChooseDoubleArray(double[] values);

    /// <summary>Chooses one decimal value from an array.</summary>
    decimal ChooseDecimalArray(decimal[] values);

    /// <summary>Chooses one string from an array.</summary>
    string? ChooseStringArray(string?[] values);

    /// <summary>Chooses one object from an array.</summary>
    object? ChooseObjectArray(object?[] values);
}
