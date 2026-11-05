using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as svg depiction data.
/// </summary>
public class SvgDepictionWriter : ISvgDepictionWriter
{
    internal Layout2D Layout;
    internal Geometry Geometry;
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgDepictionWriter"/> class.
    /// </summary>
    public SvgDepictionWriter()
    {
        Layout = new Layout2D();
        Geometry = new Geometry();
        Elements = new ElementTable();
    }

    /// <summary>
    /// Writes the string.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteString(Molecule? molecule)
    {
        return WriteStringWithOptions(molecule, new SvgDepictionOptions());
    }

    /// <summary>
    /// Writes the string using the specified depiction options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string WriteStringWithOptions(Molecule? molecule, SvgDepictionOptions? options)
    {
        SvgDepictionOptions safeOptions = NormalizeOptions(options);
        Molecule working = MoleculeBuilder.CloneMolecule(molecule);
        if (!AllAtomsHaveCoordinates(working))
        {
            Layout.Layout(working);
        }

        Layout.AssignStereoDepiction(working);
        BoundingBox3D box = GetAtomBoundingBox(working);
        double width = GetSvgWidth(box, safeOptions);
        double height = GetSvgHeight(box, safeOptions);
        StringBuilder builder = new StringBuilder();

        AppendSvgHeader(builder, width, height);
        AppendBonds(builder, working, box, safeOptions);
        AppendAtoms(builder, working, box, safeOptions);
        builder.Append("</svg>");
        return builder.ToString();
    }

    internal SvgDepictionOptions NormalizeOptions(SvgDepictionOptions? options)
    {
        SvgDepictionOptions safeOptions = new SvgDepictionOptions();
        if (options != null)
        {
            safeOptions.Scale = options.Scale;
            safeOptions.Padding = options.Padding;
            safeOptions.FontSize = options.FontSize;
            safeOptions.StrokeWidth = options.StrokeWidth;
            safeOptions.StereoWidth = options.StereoWidth;
        }

        if (safeOptions.Scale <= 0.0)
        {
            safeOptions.Scale = 40.0;
        }

        if (safeOptions.Padding < 0.0)
        {
            safeOptions.Padding = 0.0;
        }

        if (safeOptions.FontSize <= 0.0)
        {
            safeOptions.FontSize = 14.0;
        }

        if (safeOptions.StrokeWidth <= 0.0)
        {
            safeOptions.StrokeWidth = 2.0;
        }

        if (safeOptions.StereoWidth <= 0.0)
        {
            safeOptions.StereoWidth = 8.0;
        }

        return safeOptions;
    }

    internal bool AllAtomsHaveCoordinates(Molecule? molecule)
    {
        if (molecule == null)
        {
            return false;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (!molecule.Atoms[index].HasCoordinates)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal BoundingBox3D GetAtomBoundingBox(Molecule? molecule)
    {
        BoundingBox3D box = new BoundingBox3D();
        if (molecule == null)
        {
            return box;
        }

        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[index];
            if (atom.HasCoordinates)
            {
                box.Include(new Coordinate3D(atom.X, atom.Y, atom.Z));
            }

            index = index + 1;
        }

        return box;
    }

    internal double GetSvgWidth(BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!box.IsSet)
        {
            return options.Padding * 2.0;
        }

        return box.Width() * options.Scale + options.Padding * 2.0;
    }

    internal double GetSvgHeight(BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!box.IsSet)
        {
            return options.Padding * 2.0;
        }

        return box.Height() * options.Scale + options.Padding * 2.0;
    }

    internal void AppendSvgHeader(StringBuilder builder, double width, double height)
    {
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"");
        builder.Append(FormatSvgDouble(width));
        builder.Append("\" height=\"");
        builder.Append(FormatSvgDouble(height));
        builder.Append("\" viewBox=\"0 0 ");
        builder.Append(FormatSvgDouble(width));
        builder.Append(" ");
        builder.Append(FormatSvgDouble(height));
        builder.Append("\">");
    }

    internal void AppendBonds(StringBuilder builder, Molecule molecule, BoundingBox3D box, SvgDepictionOptions options)
    {
        int index = 0;
        while (index < molecule.Bonds.Count)
        {
            AppendBond(builder, molecule, molecule.Bonds[index], box, options);
            index = index + 1;
        }
    }

    internal void AppendBond(StringBuilder builder, Molecule molecule, Bond bond, BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
        {
            return;
        }

        Atom begin = molecule.Atoms[bond.BeginAtomId];
        Atom end = molecule.Atoms[bond.EndAtomId];
        double x1 = MapX(begin.X, box, options);
        double y1 = MapY(begin.Y, box, options);
        double x2 = MapX(end.X, box, options);
        double y2 = MapY(end.Y, box, options);

        if (bond.Stereo == BondStereo.Wedge || bond.Direction == BondDirection.Up)
        {
            AppendWedgeBond(builder, x1, y1, x2, y2, options);
            return;
        }

        if (bond.Stereo == BondStereo.Hash || bond.Direction == BondDirection.Down)
        {
            AppendHashBond(builder, x1, y1, x2, y2, options);
            return;
        }

        if (bond.Stereo == BondStereo.UpOrDown || bond.Direction == BondDirection.Either)
        {
            AppendLine(builder, x1, y1, x2, y2, "bond unknown", " stroke-dasharray=\"4 3\"", options.StrokeWidth);
            return;
        }

        if (bond.Order == BondOrder.Double)
        {
            AppendDoubleBond(builder, x1, y1, x2, y2, options);
            return;
        }

        if (bond.Order == BondOrder.Triple)
        {
            AppendTripleBond(builder, x1, y1, x2, y2, options);
            return;
        }

        AppendLine(builder, x1, y1, x2, y2, "bond single", "", options.StrokeWidth);
    }

    internal void AppendDoubleBond(StringBuilder builder, double x1, double y1, double x2, double y2, SvgDepictionOptions options)
    {
        Coordinate2D normal = GetNormal(x1, y1, x2, y2, 4.0);
        AppendLine(builder, x1 + normal.X, y1 + normal.Y, x2 + normal.X, y2 + normal.Y, "bond double", "", options.StrokeWidth);
        AppendLine(builder, x1 - normal.X, y1 - normal.Y, x2 - normal.X, y2 - normal.Y, "bond double", "", options.StrokeWidth);
    }

    internal void AppendTripleBond(StringBuilder builder, double x1, double y1, double x2, double y2, SvgDepictionOptions options)
    {
        Coordinate2D normal = GetNormal(x1, y1, x2, y2, 5.0);
        AppendLine(builder, x1, y1, x2, y2, "bond triple", "", options.StrokeWidth);
        AppendLine(builder, x1 + normal.X, y1 + normal.Y, x2 + normal.X, y2 + normal.Y, "bond triple", "", options.StrokeWidth);
        AppendLine(builder, x1 - normal.X, y1 - normal.Y, x2 - normal.X, y2 - normal.Y, "bond triple", "", options.StrokeWidth);
    }

    internal void AppendWedgeBond(StringBuilder builder, double x1, double y1, double x2, double y2, SvgDepictionOptions options)
    {
        Coordinate2D normal = GetNormal(x1, y1, x2, y2, options.StereoWidth);
        builder.Append("<polygon class=\"bond wedge\" points=\"");
        AppendPoint(builder, x1, y1);
        builder.Append(" ");
        AppendPoint(builder, x2 + normal.X, y2 + normal.Y);
        builder.Append(" ");
        AppendPoint(builder, x2 - normal.X, y2 - normal.Y);
        builder.Append("\" fill=\"black\" stroke=\"black\" stroke-width=\"");
        builder.Append(FormatSvgDouble(options.StrokeWidth));
        builder.Append("\"/>");
    }

    internal void AppendHashBond(StringBuilder builder, double x1, double y1, double x2, double y2, SvgDepictionOptions options)
    {
        int count = 6;
        int index = 1;
        while (index <= count)
        {
            double fraction = (double)index / (double)(count + 1);
            double x = x1 + (x2 - x1) * fraction;
            double y = y1 + (y2 - y1) * fraction;
            Coordinate2D normal = GetNormal(x1, y1, x2, y2, options.StereoWidth * fraction);
            AppendLine(builder, x + normal.X, y + normal.Y, x - normal.X, y - normal.Y, "bond hash", "", options.StrokeWidth);
            index = index + 1;
        }
    }

    internal void AppendLine(StringBuilder builder, double x1, double y1, double x2, double y2, string className, string extraAttributes, double strokeWidth)
    {
        builder.Append("<line class=\"");
        builder.Append(EscapeXml(className));
        builder.Append("\" x1=\"");
        builder.Append(FormatSvgDouble(x1));
        builder.Append("\" y1=\"");
        builder.Append(FormatSvgDouble(y1));
        builder.Append("\" x2=\"");
        builder.Append(FormatSvgDouble(x2));
        builder.Append("\" y2=\"");
        builder.Append(FormatSvgDouble(y2));
        builder.Append("\" stroke=\"black\" stroke-width=\"");
        builder.Append(FormatSvgDouble(strokeWidth));
        builder.Append("\" stroke-linecap=\"round\"");
        builder.Append(extraAttributes);
        builder.Append("/>");
    }

    internal void AppendAtoms(StringBuilder builder, Molecule molecule, BoundingBox3D box, SvgDepictionOptions options)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            AppendAtom(builder, molecule.Atoms[index], box, options);
            index = index + 1;
        }
    }

    internal void AppendAtom(StringBuilder builder, Atom atom, BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!atom.HasCoordinates)
        {
            return;
        }

        string label = GetAtomLabel(atom);
        double x = MapX(atom.X, box, options);
        double y = MapY(atom.Y, box, options);
        builder.Append("<text class=\"atom\" x=\"");
        builder.Append(FormatSvgDouble(x));
        builder.Append("\" y=\"");
        builder.Append(FormatSvgDouble(y));
        builder.Append("\" text-anchor=\"middle\" dominant-baseline=\"central\" font-family=\"Arial, sans-serif\" font-size=\"");
        builder.Append(FormatSvgDouble(options.FontSize));
        builder.Append("\" fill=\"black\">");
        builder.Append(EscapeXml(label));
        builder.Append("</text>");
    }

    internal string GetAtomLabel(Atom atom)
    {
        if (atom.Label != null && atom.Label != "")
        {
            return atom.Label;
        }

        Element element = Elements.GetElement(atom.AtomicNumber);
        if (element.Symbol != "")
        {
            return element.Symbol;
        }

        if (atom.Alias != null && atom.Alias != "")
        {
            return atom.Alias;
        }

        return "?";
    }

    internal double MapX(double x, BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!box.IsSet)
        {
            return options.Padding;
        }

        return options.Padding + (x - box.Minimum.X) * options.Scale;
    }

    internal double MapY(double y, BoundingBox3D box, SvgDepictionOptions options)
    {
        if (!box.IsSet)
        {
            return options.Padding;
        }

        return options.Padding + (box.Maximum.Y - y) * options.Scale;
    }

    internal Coordinate2D GetNormal(double x1, double y1, double x2, double y2, double length)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double distance = System.Math.Sqrt(dx * dx + dy * dy);
        if (distance == 0.0)
        {
            return new Coordinate2D(0.0, 0.0);
        }

        return new Coordinate2D(-dy / distance * length, dx / distance * length);
    }

    internal void AppendPoint(StringBuilder builder, double x, double y)
    {
        builder.Append(FormatSvgDouble(x));
        builder.Append(",");
        builder.Append(FormatSvgDouble(y));
    }

    internal string EscapeXml(string? text)
    {
        if (text == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '&')
            {
                builder.Append("&amp;");
            }
            else if (character == '<')
            {
                builder.Append("&lt;");
            }
            else if (character == '>')
            {
                builder.Append("&gt;");
            }
            else if (character == '"')
            {
                builder.Append("&quot;");
            }
            else if (character == '\'')
            {
                builder.Append("&apos;");
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal string FormatSvgDouble(double value)
    {
        return NumberFormatting.FormatDoubleWithPattern(value, "0.###");
    }
}
