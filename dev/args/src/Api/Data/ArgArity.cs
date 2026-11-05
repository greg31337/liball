namespace args;

/// <summary>
/// Describes the minimum and maximum number of values accepted by a symbol.
/// </summary>
public class ArgArity
{
    /// <summary>
    /// The maximum-arity sentinel that means there is no upper value limit.
    /// </summary>
    public const int Unbounded = -1;

    /// <summary>
    /// The smallest accepted value count.
    /// </summary>
    public int Minimum;
    /// <summary>
    /// The largest accepted value count, or Unbounded when no upper limit applies.
    /// </summary>
    public int Maximum;

    internal ArgArity(int minimum, int maximum)
    {
        if (minimum < 0)
        {
            throw new System.ArgumentOutOfRangeException("minimum");
        }

        if (maximum != Unbounded && maximum < minimum)
        {
            throw new System.ArgumentOutOfRangeException("maximum");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Determines whether a value count falls within this arity.
    /// </summary>
    /// <param name="count">The number of values to test.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool Allows(int count)
    {
        if (count < Minimum)
        {
            return false;
        }

        if (Maximum == Unbounded)
        {
            return true;
        }

        return count <= Maximum;
    }
}
