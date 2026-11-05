using System.Collections.Generic;

namespace iso20022;

internal class PlaceAndNameEvaluationResult
{
    internal bool Success = true;
    internal List<XmlPathData> Paths = new List<XmlPathData>();
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
