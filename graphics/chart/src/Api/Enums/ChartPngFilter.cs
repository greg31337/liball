namespace chart;
/// <summary>Defines PNG scanline filter selection.</summary>
public enum ChartPngFilter
{
    /// <summary>Selects the lowest-scoring filter independently for each row.</summary>
    Adaptive = -1,
    /// <summary>Uses the PNG None filter.</summary>
    None = 0,
    /// <summary>Uses the PNG Sub filter.</summary>
    Sub = 1,
    /// <summary>Uses the PNG Up filter.</summary>
    Up = 2,
    /// <summary>Uses the PNG Average filter.</summary>
    Average = 3,
    /// <summary>Uses the PNG Paeth filter.</summary>
    Paeth = 4
}
