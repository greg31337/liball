namespace odf;

internal class FormulaUnitData
{
    internal string Group = "";
    internal double Scale;
    internal double Offset;
    internal bool Prefixable;
    internal bool BinaryPrefixable;
    internal int Dimension = 1;
    internal OdfFormulaResult? Error;
}

internal static class FormulaUnitConversionFunctions
{
    internal static OdfFormulaResult Convert(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(arguments[0]);
        OdfFormulaResult fromText = FormulaValueModel.ConvertToText(arguments[1]);
        OdfFormulaResult toText = FormulaValueModel.ConvertToText(arguments[2]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (fromText.Error != OdfFormulaError.None)
        {
            return fromText;
        }
        if (toText.Error != OdfFormulaError.None)
        {
            return toText;
        }
        FormulaUnitData from = FindUnit(fromText.StringValue);
        FormulaUnitData to = FindUnit(toText.StringValue);
        if (from.Error != null)
        {
            return from.Error;
        }
        if (to.Error != null)
        {
            return to.Error;
        }
        if (from.Group != to.Group)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        double standard = number.NumericValue * from.Scale + from.Offset;
        return FormulaValueModel.CreateNumber(
            (standard - to.Offset) / to.Scale);
    }

    static FormulaUnitData FindUnit(string name)
    {
        FormulaUnitData exact = FindExactUnit(name);
        if (exact.Error == null)
        {
            return exact;
        }
        string[] binaryPrefixes = ["Yi", "Zi", "Ei", "Pi", "Ti", "Gi", "Mi", "Ki"];
        double[] binaryFactors =
        [
            1208925819614629174706176.0,
            1180591620717411303424.0,
            1152921504606846976.0,
            1125899906842624.0,
            1099511627776.0,
            1073741824.0,
            1048576.0,
            1024.0
        ];
        int index = 0;
        while (index < binaryPrefixes.Length)
        {
            if (name.StartsWith(
                binaryPrefixes[index],
                global::System.StringComparison.Ordinal))
            {
                FormulaUnitData unit = FindExactUnit(
                    name.Substring(binaryPrefixes[index].Length));
                if (unit.Error == null && unit.BinaryPrefixable)
                {
                    unit.Scale = unit.Scale * binaryFactors[index];
                    return unit;
                }
            }
            index = index + 1;
        }
        string[] prefixes =
        ["da", "Y", "Z", "E", "P", "T", "G", "M", "k", "h", "e",
            "d", "c", "m", "u", "n", "p", "f", "a", "z", "y"];
        double[] factors =
        [
            1E1, 1E24, 1E21, 1E18, 1E15, 1E12, 1E9, 1E6, 1E3, 1E2,
            1E1, 1E-1, 1E-2, 1E-3, 1E-6, 1E-9, 1E-12, 1E-15,
            1E-18, 1E-21, 1E-24
        ];
        index = 0;
        while (index < prefixes.Length)
        {
            if (name.StartsWith(
                prefixes[index],
                global::System.StringComparison.Ordinal))
            {
                FormulaUnitData unit = FindExactUnit(
                    name.Substring(prefixes[index].Length));
                if (unit.Error == null && unit.Prefixable)
                {
                    unit.Scale = unit.Scale * global::System.Math.Pow(
                        factors[index],
                        unit.Dimension);
                    return unit;
                }
            }
            index = index + 1;
        }
        return InvalidUnit();
    }

    static FormulaUnitData FindExactUnit(string name)
    {
        if (name == "m") return Unit("length", 1, true, false, 1);
        if (name == "ang") return Unit("length", 1E-10, true, false, 1);
        if (name == "ell") return Unit("length", 1.143, false, false, 1);
        if (name == "ft") return Unit("length", 0.3048, false, false, 1);
        if (name == "in") return Unit("length", 0.0254, false, false, 1);
        if (name == "ly") return Unit("length", 9460730472580800.0, true, false, 1);
        if (name == "mi") return Unit("length", 1609.344, false, false, 1);
        if (name == "Nmi") return Unit("length", 1852, false, false, 1);
        if (name == "parsec" || name == "pc")
            return Unit("length", 3.0856775814913673E16, true, false, 1);
        if (name == "Pica" || name == "picapt")
            return Unit("length", 0.0254 / 72, false, false, 1);
        if (name == "pica") return Unit("length", 0.0254 / 6, false, false, 1);
        if (name == "survey_mi")
            return Unit("length", 6336000.0 / 3937, false, false, 1);
        if (name == "yd") return Unit("length", 0.9144, false, false, 1);

        if (name == "m2" || name == "m^2") return Unit("area", 1, true, false, 2);
        if (name == "ang2" || name == "ang^2") return Unit("area", 1E-20, true, false, 2);
        if (name == "ar") return Unit("area", 100, true, false, 2);
        if (name == "ha") return Unit("area", 10000, false, false, 2);
        if (name == "ft2" || name == "ft^2") return Unit("area", 0.09290304, false, false, 2);
        if (name == "in2" || name == "in^2") return Unit("area", 0.00064516, false, false, 2);
        if (name == "ly2" || name == "ly^2") return Unit("area", 8.950542107481891E31, true, false, 2);
        if (name == "mi2" || name == "mi^2") return Unit("area", 2589988.110336, false, false, 2);
        if (name == "Nmi2" || name == "Nmi^2") return Unit("area", 3429904, false, false, 2);
        if (name == "Pica2" || name == "Pica^2" || name == "picapt2" || name == "picapt^2") return Unit("area", 0.00000012445135802469135, false, false, 2);
        if (name == "pica2" || name == "pica^2") return Unit("area", 0.00001792111111111111, false, false, 2);
        if (name == "yd2" || name == "yd^2") return Unit("area", 0.83612736, false, false, 2);
        if (name == "uk_acre") return Unit("area", 4046.8564224, false, false, 2);
        if (name == "us_acre") return Unit("area", 4046.872609874252, false, false, 2);
        if (name == "Morgen") return Unit("area", 2500, false, false, 2);

        if (name == "J") return Unit("energy", 1, true, false, 1);
        if (name == "BTU" || name == "btu") return Unit("energy", 1055.05585262, false, false, 1);
        if (name == "c") return Unit("energy", 4.184, true, false, 1);
        if (name == "cal") return Unit("energy", 4.1868, true, false, 1);
        if (name == "e") return Unit("energy", 1E-7, true, false, 1);
        if (name == "eV" || name == "ev") return Unit("energy", 1.602176634E-19, true, false, 1);
        if (name == "flb") return Unit("energy", 1.3558179483314, false, false, 1);
        if (name == "HPh" || name == "hh") return Unit("energy", 2684519.53769617, false, false, 1);
        if (name == "Wh" || name == "wh") return Unit("energy", 3600, true, false, 1);

        if (name == "N") return Unit("force", 1, true, false, 1);
        if (name == "dyn" || name == "dy") return Unit("force", 0.00001, true, false, 1);
        if (name == "lbf") return Unit("force", 4.4482216152605, false, false, 1);
        if (name == "pond") return Unit("force", 0.00980665, true, false, 1);

        if (name == "bit") return Unit("information", 1, true, true, 1);
        if (name == "byte") return Unit("information", 8, true, true, 1);
        if (name == "T") return Unit("magnetic", 1, true, false, 1);
        if (name == "ga") return Unit("magnetic", 0.0001, true, false, 1);

        if (name == "g") return Unit("mass", 1, true, false, 1);
        if (name == "grain") return Unit("mass", 0.06479891, false, false, 1);
        if (name == "cwt" || name == "shweight") return Unit("mass", 45359.237, false, false, 1);
        if (name == "uk_cwt" || name == "lcwt" || name == "hweight") return Unit("mass", 50802.34544, false, false, 1);
        if (name == "lbm") return Unit("mass", 453.59237, false, false, 1);
        if (name == "stone") return Unit("mass", 6350.29318, false, false, 1);
        if (name == "ton") return Unit("mass", 907184.74, false, false, 1);
        if (name == "ozm") return Unit("mass", 28.349523125, false, false, 1);
        if (name == "sg") return Unit("mass", 14593.9029372, false, false, 1);
        if (name == "u") return Unit("mass", 1.66053906660E-24, true, false, 1);
        if (name == "uk_ton" || name == "LTON" || name == "brton") return Unit("mass", 1016046.9088, false, false, 1);

        if (name == "W" || name == "w") return Unit("power", 1, true, false, 1);
        if (name == "HP" || name == "h") return Unit("power", 745.69987158227, false, false, 1);
        if (name == "PS") return Unit("power", 735.49875, false, false, 1);

        if (name == "Pa") return Unit("pressure", 1, true, false, 1);
        if (name == "atm") return Unit("pressure", 101325, true, false, 1);
        if (name == "mmHg") return Unit("pressure", 133.322387415, true, false, 1);
        if (name == "psi") return Unit("pressure", 6894.757293168, false, false, 1);
        if (name == "SI_at") return Unit("pressure", 98066.5, true, false, 1);
        if (name == "Torr") return Unit("pressure", 101325.0 / 760, false, false, 1);
        if (name == "at") return Unit("pressure", 101325, true, false, 1);

        if (name == "m/s" || name == "m/sec") return Unit("speed", 1, true, false, 1);
        if (name == "m/h" || name == "m/hr") return Unit("speed", 1.0 / 3600, true, false, 1);
        if (name == "mph") return Unit("speed", 1609.344 / 3600, false, false, 1);
        if (name == "kn") return Unit("speed", 1852.0 / 3600, false, false, 1);
        if (name == "admkn") return Unit("speed", 6080 * 0.3048 / 3600, false, false, 1);

        if (name == "C" || name == "cel") return Temperature(1, 273.15);
        if (name == "F" || name == "fah") return Temperature(5.0 / 9, 255.3722222222222);
        if (name == "K" || name == "kel") return Temperature(1, 0);
        if (name == "Rank") return Temperature(5.0 / 9, 0);
        if (name == "Reau") return Temperature(1.25, 273.15);

        if (name == "sec" || name == "s") return Unit("time", 1, true, false, 1);
        if (name == "mn" || name == "min") return Unit("time", 60, false, false, 1);
        if (name == "hr") return Unit("time", 3600, false, false, 1);
        if (name == "day" || name == "d") return Unit("time", 86400, false, false, 1);
        if (name == "yr") return Unit("time", 31557600, false, false, 1);

        if (name == "m3" || name == "m^3") return Unit("volume", 1, true, false, 3);
        if (name == "l" || name == "L" || name == "lt") return Unit("volume", 0.001, true, false, 1);
        if (name == "ang3" || name == "ang^3") return Unit("volume", 1E-30, true, false, 3);
        if (name == "ft3" || name == "ft^3") return Unit("volume", 0.028316846592, false, false, 3);
        if (name == "in3" || name == "in^3") return Unit("volume", 0.000016387064, false, false, 3);
        if (name == "ly3" || name == "ly^3") return Unit("volume", 8.468443202303688E47, true, false, 3);
        if (name == "mi3" || name == "mi^3") return Unit("volume", 4168181825.440579584, false, false, 3);
        if (name == "Nmi3" || name == "Nmi^3") return Unit("volume", 6352182208, false, false, 3);
        if (name == "Pica3" || name == "Pica^3" || name == "picapt3" || name == "picapt^3") return Unit("volume", 0.00000000004390242386831276, false, false, 3);
        if (name == "pica3" || name == "pica^3") return Unit("volume", 0.00000007586137037037037, false, false, 3);
        if (name == "yd3" || name == "yd^3") return Unit("volume", 0.764554857984, false, false, 3);
        if (name == "gal") return Unit("volume", 0.003785411784, false, false, 1);
        if (name == "qt") return Unit("volume", 0.000946352946, false, false, 1);
        if (name == "pt" || name == "us_pt") return Unit("volume", 0.000473176473, false, false, 1);
        if (name == "oz") return Unit("volume", 0.0000295735295625, false, false, 1);
        if (name == "cup") return Unit("volume", 0.0002365882365, false, false, 1);
        if (name == "tbs") return Unit("volume", 0.00001478676478125, false, false, 1);
        if (name == "tsp") return Unit("volume", 0.00000492892159375, false, false, 1);
        if (name == "tspm") return Unit("volume", 0.000005, false, false, 1);
        if (name == "uk_gal") return Unit("volume", 0.00454609, false, false, 1);
        if (name == "uk_qt") return Unit("volume", 0.0011365225, false, false, 1);
        if (name == "uk_pt") return Unit("volume", 0.00056826125, false, false, 1);
        if (name == "barrel") return Unit("volume", 0.158987294928, false, false, 1);
        if (name == "bushel") return Unit("volume", 0.03523907016688, false, false, 1);
        if (name == "GRT" || name == "regton") return Unit("volume", 2.8316846592, false, false, 1);
        if (name == "MTON") return Unit("volume", 1.13267386368, false, false, 1);
        return InvalidUnit();
    }

    static FormulaUnitData Unit(
        string group,
        double scale,
        bool prefixable,
        bool binary,
        int dimension)
    {
        FormulaUnitData result = new FormulaUnitData();
        result.Group = group;
        result.Scale = scale;
        result.Prefixable = prefixable;
        result.BinaryPrefixable = binary;
        result.Dimension = dimension;
        return result;
    }

    static FormulaUnitData Temperature(double scale, double offset)
    {
        FormulaUnitData result = Unit("temperature", scale, false, false, 1);
        result.Offset = offset;
        return result;
    }

    static FormulaUnitData InvalidUnit()
    {
        FormulaUnitData result = new FormulaUnitData();
        result.Error = FormulaValueModel.CreateError(
            OdfFormulaError.NotAvailable);
        return result;
    }
}
