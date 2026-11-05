namespace molecule;


/// <summary>
/// Selects the electron and ring restrictions used for aromaticity perception.
/// </summary>
public enum AromaticityModelKind
{
    /// <summary>
    /// Uses a Daylight-like 4n+2 model with supported five-member lone-pair heterocycles.
    /// </summary>
    DaylightLike = 0,
    /// <summary>
    /// Uses a conservative MDL-like model limited to six-member carbon and nitrogen pi rings.
    /// </summary>
    MdlLike = 1
}
