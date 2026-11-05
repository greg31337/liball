using System.Collections.Generic;

namespace iso20022;

internal class PlaceAndNameParseResult
{
    internal bool Success;
    internal bool Supported;
    internal bool Absolute;
    internal List<PlaceAndNameStep> Steps = new List<PlaceAndNameStep>();
    internal string ErrorMessage = string.Empty;
}
