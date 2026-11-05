namespace qrcode;

/// <summary>Identifies an ISO-style numeric symbol-quality grade band.</summary>
public enum QrQualityGrade
{
    /// <summary>No calibrated overall grade is available.</summary>
    NotGraded = -1,

    /// <summary>Numeric grade 0.0.</summary>
    Grade0 = 0,

    /// <summary>Numeric grade 1.0.</summary>
    Grade1 = 1,

    /// <summary>Numeric grade 2.0.</summary>
    Grade2 = 2,

    /// <summary>Numeric grade 3.0.</summary>
    Grade3 = 3,

    /// <summary>Numeric grade 4.0.</summary>
    Grade4 = 4
}
