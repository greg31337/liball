namespace pptx;

internal static class PptxShapeTypeCore
{
    internal const string Rectangle = "rect";
    internal const string Ellipse = "ellipse";
    internal const string RoundRectangle = "roundRect";
    internal const string Triangle = "triangle";
    internal const string RightTriangle = "rtTriangle";
    internal const string Diamond = "diamond";
    internal const string Pentagon = "pentagon";
    internal const string Hexagon = "hexagon";
    internal const string Heptagon = "heptagon";
    internal const string Octagon = "octagon";
    internal const string Plus = "plus";
    internal const string Star5 = "star5";
    internal const string Heart = "heart";
    internal const string Cloud = "cloud";
    internal const string Cube = "cube";
    internal const string Can = "can";
    internal const string Donut = "donut";
    internal const string BlockArc = "blockArc";
    internal const string FoldedCorner = "foldedCorner";
    internal const string SmileyFace = "smileyFace";
    internal const string TextBox = "textBox";

    internal static string NormalizePresetGeometry(string geometry)
    {
        string clean = PptxDiagnostic.CleanText(geometry);
        if (clean.Length == 0)
        {
            return Rectangle;
        }

        if (clean == "rectangle")
        {
            return Rectangle;
        }

        if (clean == "roundRectangle")
        {
            return RoundRectangle;
        }

        if (clean == "rightTriangle")
        {
            return RightTriangle;
        }

        if (clean == TextBox)
        {
            return Rectangle;
        }

        return clean;
    }
    internal static bool IsCommonPresetGeometry(string geometry)
    {
        string clean = NormalizePresetGeometry(geometry);
        if (clean == Rectangle)
        {
            return true;
        }

        if (clean == Ellipse)
        {
            return true;
        }

        if (clean == RoundRectangle)
        {
            return true;
        }

        if (clean == Triangle)
        {
            return true;
        }

        if (clean == RightTriangle)
        {
            return true;
        }

        if (clean == Diamond)
        {
            return true;
        }

        if (clean == Pentagon)
        {
            return true;
        }

        if (clean == Hexagon)
        {
            return true;
        }

        if (clean == Heptagon)
        {
            return true;
        }

        if (clean == Octagon)
        {
            return true;
        }

        if (clean == Plus)
        {
            return true;
        }

        if (clean == Star5)
        {
            return true;
        }

        if (clean == Heart)
        {
            return true;
        }

        if (clean == Cloud)
        {
            return true;
        }

        if (clean == Cube)
        {
            return true;
        }

        if (clean == Can)
        {
            return true;
        }

        if (clean == Donut)
        {
            return true;
        }

        if (clean == BlockArc)
        {
            return true;
        }

        if (clean == FoldedCorner)
        {
            return true;
        }

        if (clean == SmileyFace)
        {
            return true;
        }

        return false;
    }
}

