namespace unicode;

/// <summary>
/// Describes one named Unicode code-point sequence.
/// </summary>
public class UnicodeNamedSequence
{
    /// <summary>
    /// The normative sequence name.
    /// </summary>
    public string Name;
    /// <summary>
    /// A caller-owned copy of the sequence code points.
    /// </summary>
    public int[] CodePoints;

    /// <summary>
    /// Initializes a named-sequence record and copies its code points.
    /// </summary>
    /// <param name="name">The normative sequence name.</param>
    /// <param name="codePoints">The code points in sequence order.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public UnicodeNamedSequence(string name, int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(name);
        System.ArgumentNullException.ThrowIfNull(codePoints);

        Name = name;
        CodePoints = new int[codePoints.Length];
        int index = 0;
        while (index < codePoints.Length)
        {
            CodePoints[index] = codePoints[index];
            index = index + 1;
        }
    }
}
