namespace icc;

/// <summary>Builds and applies chromatic-adaptation matrices.</summary>
public class IccChromaticAdapter : IIccChromaticAdapter
{
    internal ServiceAvailability Core;

    /// <summary>Initializes a chromatic-adaptation service.</summary>
    public IccChromaticAdapter()
    {
        Core = new ServiceAvailability("Chromatic adaptation");
    }

    /// <summary>Builds a Bradford matrix.</summary>
    /// <param name="sourceWhitePoint">The source white point.</param>
    /// <param name="destinationWhitePoint">The destination white point.</param>
    /// <returns>The adaptation matrix.</returns>
    public double[] BuildBradfordMatrix(double[] sourceWhitePoint, double[] destinationWhitePoint)
    {
        return ChromaticAdaptationCore.BuildBradfordMatrix(sourceWhitePoint, destinationWhitePoint);
    }

    /// <summary>Applies an adaptation matrix.</summary>
    /// <param name="matrix">The adaptation matrix.</param>
    /// <param name="xyz">The XYZ values.</param>
    /// <returns>The adapted values.</returns>
    public double[] ApplyAdaptationMatrix(double[] matrix, double[] xyz)
    {
        return ChromaticAdaptationCore.ApplyAdaptationMatrix(matrix, xyz);
    }

    /// <summary>Inverts an adaptation matrix.</summary>
    /// <param name="matrix">The adaptation matrix.</param>
    /// <returns>The inverse matrix.</returns>
    public double[] InvertAdaptationMatrix(double[] matrix)
    {
        return ChromaticAdaptationCore.InvertAdaptationMatrix(matrix);
    }
}
