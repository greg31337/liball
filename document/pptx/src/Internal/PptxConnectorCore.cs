namespace pptx;

internal static class PptxConnectorCore
{
    internal const string Straight = "straight";
    internal const string Elbow = "elbow";
    internal const string Curved = "curved";
    internal const string StraightPreset = "straightConnector1";
    internal const string ElbowPreset = "bentConnector3";
    internal const string CurvedPreset = "curvedConnector3";

    internal static string NormalizeConnectorType(string connectorType)
    {
        string clean = PptxDiagnostic.CleanText(connectorType);
        if (clean.Length == 0)
        {
            return Straight;
        }

        if (clean == "line")
        {
            return Straight;
        }

        if (clean == StraightPreset)
        {
            return Straight;
        }

        if (clean == "bent")
        {
            return Elbow;
        }

        if (clean == ElbowPreset)
        {
            return Elbow;
        }

        if (clean == "curve")
        {
            return Curved;
        }

        if (clean == CurvedPreset)
        {
            return Curved;
        }

        if (clean == Elbow)
        {
            return Elbow;
        }

        if (clean == Curved)
        {
            return Curved;
        }

        return Straight;
    }
    internal static string GetPresetGeometry(string connectorType)
    {
        string clean = NormalizeConnectorType(connectorType);
        if (clean == Elbow)
        {
            return ElbowPreset;
        }

        if (clean == Curved)
        {
            return CurvedPreset;
        }

        return StraightPreset;
    }
    internal static bool IsConnectorPreset(string presetGeometry)
    {
        string clean = PptxDiagnostic.CleanText(presetGeometry);
        if (clean == Straight)
        {
            return true;
        }

        if (clean == StraightPreset)
        {
            return true;
        }

        if (clean == Elbow)
        {
            return true;
        }

        if (clean == Curved)
        {
            return true;
        }

        if (clean == CurvedPreset)
        {
            return true;
        }

        return false;
    }
}

