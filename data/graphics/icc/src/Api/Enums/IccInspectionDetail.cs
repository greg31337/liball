namespace icc;

/// <summary>
/// Identifies the requested amount of profile inspection detail.
/// </summary>
public enum IccInspectionDetail
{
    /// <summary>
    /// Requests a compact profile summary.
    /// </summary>
    Summary = 0,

    /// <summary>
    /// Requests normal decoded profile details.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Requests detailed decoded profile information.
    /// </summary>
    Detailed = 2,

    /// <summary>
    /// Requests decoded information together with raw representations.
    /// </summary>
    Raw = 3
}
