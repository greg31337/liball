using System.Collections.Generic;

namespace odf;

internal static class FormulaFunctionCatalog
{
    internal static readonly OdfFormulaFunctionInfo[] Functions =
        CreateFunctions();

    internal static OdfFormulaFunctionInfo? Find(string name)
    {
        int index = 0;
        while (index < Functions.Length)
        {
            if (NamesEqual(Functions[index].Name, name))
            {
                return Functions[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static void BindMetadata(OdfFormulaNode node)
    {
        if (node.Kind == OdfFormulaNodeKind.FunctionCall)
        {
            node.Function = Find(node.Text);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            BindMetadata(node.Children[index]);
            index = index + 1;
        }
    }

    static OdfFormulaFunctionInfo[] CreateFunctions()
    {
        List<OdfFormulaFunctionInfo> result =
            new List<OdfFormulaFunctionInfo>();
        AddDatabaseFunctions(result);
        AddInformationFunctions(result);
        AddExternalFunctions(result);
        AddDateTimeFunctions(result);
        AddLogicalFunctions(result);
        AddFinancialFunctions(result);
        AddMatrixFunctions(result);
        AddLookupFunctions(result);
        AddBitFunctions(result);
        AddComplexFunctions(result);
        AddMathematicalFunctions(result);
        AddRoundingFunctions(result);
        AddRepresentationFunctions(result);
        AddStatisticalFunctions(result);
        AddLegacyFunctions(result);
        AddTextFunctions(result);
        return result.ToArray();
    }

    static bool NamesEqual(string canonical, string supplied)
    {
        if (canonical.Length != supplied.Length)
        {
            return false;
        }
        int index = 0;
        while (index < canonical.Length)
        {
            char first = canonical[index];
            char second = supplied[index];
            if ((first == '_' || first == '.')
                && (second == '_' || second == '.'))
            {
                index = index + 1;
                continue;
            }
            if (global::System.Char.ToUpperInvariant(first)
                != global::System.Char.ToUpperInvariant(second))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void AddDatabaseFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.DAVERAGE, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DCOUNT, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DCOUNTA, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DGET, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DMAX, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DMIN, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DPRODUCT, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DSTDEV, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DSTDEVP, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DSUM, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DVAR, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DVARP, "Database", 3, 3,
            OdfFormulaConformance.Small, false, false);
    }

    static void AddInformationFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.AREAS, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.CELL, "Information", 1, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.COLUMN, "Information", 0, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COLUMNS, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.COUNT, "Information", 1,
            int.MaxValue, OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.COUNTA, "Information", 1,
            int.MaxValue, OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.COUNTBLANK, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.COUNTIF, "Information", 2, 2,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.COUNTIFS, "Information", 2,
            int.MaxValue, OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.ERROR_TYPE, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.FORMULA, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.INFO, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.ISBLANK, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISERR, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISERROR, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISEVEN, "Information", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ISFORMULA, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.ISLOGICAL, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISNA, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISNONTEXT, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISNUMBER, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISODD, "Information", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ISREF, "Information", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.ISTEXT, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.N, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.NA, "Information", 0, 0,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.NUMBERVALUE, "Information", 1, 3,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.ROW, "Information", 0, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ROWS, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.SHEET, "Information", 0, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.SHEETS, "Information", 0, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.TYPE, "Information", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.VALUE, "Information", 1, 1,
            OdfFormulaConformance.Small, false, false);
    }

    static void AddExternalFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.DDE, "External", 3, 4,
            OdfFormulaConformance.Large, true, true);
        Add(result, OdfFormulaNames.HYPERLINK, "External", 1, 2,
            OdfFormulaConformance.Large, false, false);
    }

    static void AddDateTimeFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.DATE, "DateTime", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DATEDIF, "DateTime", 3, 3,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.DATEVALUE, "DateTime", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DAY, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DAYS, "DateTime", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.DAYS360, "DateTime", 2, 3,
            OdfFormulaConformance.Medium, false, false);
        AddVersion14(result, OdfFormulaNames.EASTERSUNDAY, "DateTime", 0, 1,
            OdfFormulaConformance.Unknown, false, true);
        Add(result, OdfFormulaNames.EDATE, "DateTime", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.EOMONTH, "DateTime", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.HOUR, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISOWEEKNUM, "DateTime", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.MINUTE, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.MONTH, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.NETWORKDAYS, "DateTime", 2, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.NOW, "DateTime", 0, 0,
            OdfFormulaConformance.Small, false, true);
        Add(result, OdfFormulaNames.SECOND, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.TIME, "DateTime", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.TIMEVALUE, "DateTime", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.TODAY, "DateTime", 0, 0,
            OdfFormulaConformance.Small, false, true);
        Add(result, OdfFormulaNames.WEEKDAY, "DateTime", 1, 2,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.WEEKNUM, "DateTime", 1, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.WORKDAY, "DateTime", 2, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.YEAR, "DateTime", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.YEARFRAC, "DateTime", 2, 3,
            OdfFormulaConformance.Medium, false, false);
    }

    static void AddLogicalFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.AND, "Logical", 1, int.MaxValue,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.FALSE, "Logical", 0, 0,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.IF, "Logical", 1, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.IFERROR, "Logical", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IFNA, "Logical", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.NOT, "Logical", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.OR, "Logical", 1, int.MaxValue,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.TRUE, "Logical", 0, 0,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.XOR, "Logical", 1, int.MaxValue,
            OdfFormulaConformance.Large, false, false);
    }

    static void AddFinancialFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.ACCRINT, "Financial", 6, 8,
            OdfFormulaConformance.Unknown, false, false);
        Add(result, OdfFormulaNames.ACCRINTM, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.AMORLINC, "Financial", 6, 7,
            OdfFormulaConformance.Unknown, false, false);
        Add(result, OdfFormulaNames.COUPDAYBS, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COUPDAYS, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COUPDAYSNC, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COUPNCD, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COUPNUM, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.COUPPCD, "Financial", 3, 4,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.CUMIPMT, "Financial", 6, 6,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.CUMPRINC, "Financial", 6, 6,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DB, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DDB, "Financial", 4, 5,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.DISC, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DOLLARDE, "Financial", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DOLLARFR, "Financial", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.DURATION, "Financial", 5, 6,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.EFFECT, "Financial", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.FV, "Financial", 3, 5,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.FVSCHEDULE, "Financial", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.INTRATE, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.IPMT, "Financial", 4, 6,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IRR, "Financial", 1, 2,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ISPMT, "Financial", 4, 4,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.MDURATION, "Financial", 5, 6,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.MIRR, "Financial", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.NOMINAL, "Financial", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.NPER, "Financial", 3, 5,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.NPV, "Financial", 2, int.MaxValue,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ODDFPRICE, "Financial", 8, 9,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ODDFYIELD, "Financial", 8, 9,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ODDLPRICE, "Financial", 7, 8,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ODDLYIELD, "Financial", 7, 8,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.PDURATION, "Financial", 3, 3,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.PMT, "Financial", 3, 5,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.PPMT, "Financial", 4, 6,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.PRICE, "Financial", 6, 7,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.PRICEDISC, "Financial", 4, 5,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.PRICEMAT, "Financial", 5, 6,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.PV, "Financial", 3, 5,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.RATE, "Financial", 3, 6,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.RECEIVED, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.RRI, "Financial", 3, 3,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.SLN, "Financial", 3, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.SYD, "Financial", 4, 4,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.TBILLEQ, "Financial", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.TBILLPRICE, "Financial", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.TBILLYIELD, "Financial", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.VDB, "Financial", 5, 7,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.XIRR, "Financial", 2, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.XNPV, "Financial", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.YIELD, "Financial", 6, 7,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.YIELDDISC, "Financial", 4, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.YIELDMAT, "Financial", 5, 6,
            OdfFormulaConformance.Medium, false, false);
    }

    static void AddMatrixFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.MDETERM, "Matrix", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.MINVERSE, "Matrix", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.MMULT, "Matrix", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.MUNIT, "Matrix", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.TRANSPOSE, "Matrix", 1, 1,
            OdfFormulaConformance.Medium, false, false);
    }

    static void AddLookupFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.ADDRESS, "Lookup", 2, 5,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.CHOOSE, "Lookup", 2, int.MaxValue,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.GETPIVOTDATA, "Lookup", 2, int.MaxValue,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.HLOOKUP, "Lookup", 3, 4,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.INDEX, "Lookup", 1, 4,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.INDIRECT, "Lookup", 1, 2,
            OdfFormulaConformance.Large, false, true);
        Add(result, OdfFormulaNames.LOOKUP, "Lookup", 2, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.MATCH, "Lookup", 2, 3,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.MULTIPLE_OPERATIONS, "Lookup", 3, 5,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.OFFSET, "Lookup", 3, 5,
            OdfFormulaConformance.Medium, false, true);
        Add(result, OdfFormulaNames.VLOOKUP, "Lookup", 3, 4,
            OdfFormulaConformance.Small, false, false);
    }

    static void AddBitFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.BITAND, "Bit", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.BITLSHIFT, "Bit", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.BITOR, "Bit", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.BITRSHIFT, "Bit", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.BITXOR, "Bit", 2, 2,
            OdfFormulaConformance.Large, false, false);
    }

    static void AddComplexFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.COMPLEX, "Complex", 2, 3,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMABS, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMAGINARY, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMARGUMENT, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCONJUGATE, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCOS, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCOSH, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCOT, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCSC, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMCSCH, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMDIV, "Complex", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMEXP, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMLN, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMLOG10, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMLOG2, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMPOWER, "Complex", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMPRODUCT, "Complex", 1, int.MaxValue,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMREAL, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSIN, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSINH, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSEC, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSECH, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSQRT, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSUB, "Complex", 2, 2,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMSUM, "Complex", 1, int.MaxValue,
            OdfFormulaConformance.Large, false, false);
        Add(result, OdfFormulaNames.IMTAN, "Complex", 1, 1,
            OdfFormulaConformance.Large, false, false);
    }

    static void AddMathematicalFunctions(List<OdfFormulaFunctionInfo> result)
    {
        AddMath(result, OdfFormulaNames.ABS, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.ACOS, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.ACOSH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ACOT, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ACOTH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ASIN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.ASINH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ATAN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.ATAN2, 2, 2,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.ATANH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.BESSELI, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.BESSELJ, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.BESSELK, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.BESSELY, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.COMBIN, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.COMBINA, 2, 2,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.CONVERT, 3, 3,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.COS, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.COSH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.COT, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.COTH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.CSC, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.CSCH, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.DEGREES, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.DELTA, 1, 2,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.ERF, 1, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ERFC, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.EUROCONVERT, 3, 5,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.EVEN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.EXP, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.FACT, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.FACTDOUBLE, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.GAMMA, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.GAMMALN, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.GCD, 1, int.MaxValue,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.GESTEP, 1, 2,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.LCM, 1, int.MaxValue,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.LN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.LOG, 1, 2,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.LOG10, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.MOD, 2, 2,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.MULTINOMIAL, 1, int.MaxValue,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.ODD, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.PI, 0, 0,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.POWER, 2, 2,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.PRODUCT, 1, int.MaxValue,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.QUOTIENT, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.RADIANS, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.RAND, 0, 0,
            OdfFormulaConformance.Medium, true);
        AddMath(result, OdfFormulaNames.RANDBETWEEN, 2, 2,
            OdfFormulaConformance.Medium, true);
        AddMath(result, OdfFormulaNames.SEC, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.SERIESSUM, 4, 4,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SIGN, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SIN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.SINH, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SECH, 1, 1,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.SQRT, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.SQRTPI, 1, 1,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUBTOTAL, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUM, 1, int.MaxValue,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.SUMIF, 2, 3,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.SUMIFS, 3, int.MaxValue,
            OdfFormulaConformance.Large, false);
        AddMath(result, OdfFormulaNames.SUMPRODUCT, 1, int.MaxValue,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUMSQ, 1, int.MaxValue,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUMX2MY2, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUMX2PY2, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.SUMXMY2, 2, 2,
            OdfFormulaConformance.Medium, false);
        AddMath(result, OdfFormulaNames.TAN, 1, 1,
            OdfFormulaConformance.Small, false);
        AddMath(result, OdfFormulaNames.TANH, 1, 1,
            OdfFormulaConformance.Medium, false);
    }

    static void AddRepresentationFunctions(
        List<OdfFormulaFunctionInfo> result)
    {
        AddRepresentation(result, OdfFormulaNames.ARABIC, 1, 1,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.BASE, 2, 3,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.BIN2DEC, 1, 1,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.BIN2HEX, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.BIN2OCT, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.DEC2BIN, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.DEC2HEX, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.DEC2OCT, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.DECIMAL, 2, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.HEX2BIN, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.HEX2DEC, 1, 1,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.HEX2OCT, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.OCT2BIN, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.OCT2DEC, 1, 1,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.OCT2HEX, 1, 2,
            OdfFormulaConformance.Large);
        AddRepresentation(result, OdfFormulaNames.ROMAN, 1, 2,
            OdfFormulaConformance.Medium);
    }

    static void AddRoundingFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.CEILING, "Rounding", 1, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.FLOOR, "Rounding", 1, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.INT, "Rounding", 1, 1,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.MROUND, "Rounding", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ROUND, "Rounding", 1, 2,
            OdfFormulaConformance.Small, false, false);
        Add(result, OdfFormulaNames.ROUNDDOWN, "Rounding", 1, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.ROUNDUP, "Rounding", 1, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.TRUNC, "Rounding", 1, 2,
            OdfFormulaConformance.Small, false, false);
    }

    static void AddMath(
        List<OdfFormulaFunctionInfo> result,
        string name,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance,
        bool volatileFunction)
    {
        Add(result, name, "Mathematical", minimum, maximum,
            conformance, false, volatileFunction);
    }

    static void AddRepresentation(
        List<OdfFormulaFunctionInfo> result,
        string name,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance)
    {
        Add(result, name, "Conversion", minimum, maximum,
            conformance, false, false);
    }

    static void AddStatisticalFunctions(
        List<OdfFormulaFunctionInfo> result)
    {
        AddStatistic(result, OdfFormulaNames.AVEDEV, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.AVERAGE, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.AVERAGEA, 1, int.MaxValue,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.AVERAGEIF, 2, 3,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.AVERAGEIFS, 3, int.MaxValue,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.BETADIST, 3, 6,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.BETAINV, 3, 5,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.BINOM_DIST_RANGE, 3, 4,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.BINOMDIST, 4, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.CHISQDIST, 2, 3,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.CHISQINV, 2, 2,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.CONFIDENCE, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.CORREL, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.COVAR, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.CRITBINOM, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.DEVSQ, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.EXPONDIST, 2, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.FDIST, 3, 4,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.FINV, 3, 3,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.FISHER, 1, 1,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.FISHERINV, 1, 1,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.FORECAST, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.FREQUENCY, 2, 2,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.FTEST, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.GAMMADIST, 3, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.GAMMAINV, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.GAUSS, 1, 1,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.GEOMEAN, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.GROWTH, 1, 4,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.HARMEAN, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.HYPGEOMDIST, 4, 5,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.INTERCEPT, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.KURT, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.LARGE, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.LINEST, 1, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.LOGEST, 1, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.LOGINV, 1, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.LOGNORMDIST, 1, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.MAX, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.MAXA, 1, int.MaxValue,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.MEDIAN, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.MIN, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.MINA, 1, int.MaxValue,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.MODE, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.NEGBINOMDIST, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.NORMDIST, 3, 4,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.NORMINV, 3, 3,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.PEARSON, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.PERCENTILE, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.PERCENTRANK, 2, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.PERMUT, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.PERMUTATIONA, 2, 2,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.PHI, 1, 1,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.POISSON, 2, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.PROB, 3, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.QUARTILE, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.RANK, 2, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.RSQ, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.SKEW, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.SKEWP, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.SLOPE, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.SMALL, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.STANDARDIZE, 3, 3,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.STDEV, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.STDEVA, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.STDEVP, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.STDEVPA, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.STEYX, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.TINV, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.TREND, 1, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.TRIMMEAN, 2, 2,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.TTEST, 4, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.VAR, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.VARA, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.VARP, 1, int.MaxValue,
            OdfFormulaConformance.Small);
        AddStatistic(result, OdfFormulaNames.VARPA, 1, int.MaxValue,
            OdfFormulaConformance.Large);
        AddStatistic(result, OdfFormulaNames.WEIBULL, 4, 4,
            OdfFormulaConformance.Medium);
        AddStatistic(result, OdfFormulaNames.ZTEST, 2, 3,
            OdfFormulaConformance.Medium);
    }

    static void AddStatistic(
        List<OdfFormulaFunctionInfo> result,
        string name,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance)
    {
        Add(result, name, "Statistical", minimum, maximum,
            conformance, false, false);
    }

    static void AddLegacyFunctions(List<OdfFormulaFunctionInfo> result)
    {
        Add(result, OdfFormulaNames.LEGACY_CHIDIST, "Legacy", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_CHIINV, "Legacy", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_CHITEST, "Legacy", 2, 2,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_FDIST, "Legacy", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_FINV, "Legacy", 3, 3,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_NORMSDIST, "Legacy", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_NORMSINV, "Legacy", 1, 1,
            OdfFormulaConformance.Medium, false, false);
        Add(result, OdfFormulaNames.LEGACY_TDIST, "Legacy", 3, 3,
            OdfFormulaConformance.Medium, false, false);
    }

    static void AddTextFunctions(List<OdfFormulaFunctionInfo> result)
    {
        AddText(result, OdfFormulaNames.ASC, 1, 1,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.CHAR, 1, 1,
            OdfFormulaConformance.Medium);
        AddText(result, OdfFormulaNames.CLEAN, 1, 1,
            OdfFormulaConformance.Medium);
        AddText(result, OdfFormulaNames.CODE, 1, 1,
            OdfFormulaConformance.Medium);
        AddText(result, OdfFormulaNames.CONCATENATE, 1, int.MaxValue,
            OdfFormulaConformance.Medium);
        AddText(result, OdfFormulaNames.DOLLAR, 1, 2,
            OdfFormulaConformance.Unknown);
        AddText(result, OdfFormulaNames.EXACT, 2, 2,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.FIND, 2, 3,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.FINDB, 2, 3,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.FIXED, 1, 3,
            OdfFormulaConformance.Medium);
        AddText(result, OdfFormulaNames.JIS, 1, 1,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.LEFT, 1, 2,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.LEFTB, 1, 2,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.LEN, 1, 1,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.LENB, 1, 1,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.LOWER, 1, 1,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.MID, 3, 3,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.MIDB, 3, 3,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.PROPER, 1, 1,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.REPLACE, 4, 4,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.REPLACEB, 4, 4,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.REPT, 2, 2,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.RIGHT, 1, 2,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.RIGHTB, 1, 2,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.SEARCH, 2, 3,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.SEARCHB, 2, 3,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.SUBSTITUTE, 3, 4,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.T, 1, 1,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.TEXT, 2, 2,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.TRIM, 1, 1,
            OdfFormulaConformance.Small);
        AddText(result, OdfFormulaNames.UNICHAR, 1, 1,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.UNICODE, 1, 1,
            OdfFormulaConformance.Large);
        AddText(result, OdfFormulaNames.UPPER, 1, 1,
            OdfFormulaConformance.Small);
    }

    static void AddText(
        List<OdfFormulaFunctionInfo> result,
        string name,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance)
    {
        Add(result, name, "Text", minimum, maximum,
            conformance, false, false);
    }

    static void Add(
        List<OdfFormulaFunctionInfo> result,
        string name,
        string group,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance,
        bool external,
        bool volatileFunction)
    {
        OdfFormulaFunctionInfo info = new OdfFormulaFunctionInfo();
        info.Name = name;
        info.Group = group;
        info.MinimumArguments = minimum;
        info.MaximumArguments = maximum;
        info.Conformance = conformance;
        info.IntroducedVersion = OdfVersion.Version12;
        info.RequiresExternalAccess = external;
        info.IsVolatile = volatileFunction;
        result.Add(info);
    }

    static void AddVersion14(
        List<OdfFormulaFunctionInfo> result,
        string name,
        string group,
        int minimum,
        int maximum,
        OdfFormulaConformance conformance,
        bool external,
        bool volatileFunction)
    {
        OdfFormulaFunctionInfo info = new OdfFormulaFunctionInfo();
        info.Name = name;
        info.Group = group;
        info.MinimumArguments = minimum;
        info.MaximumArguments = maximum;
        info.Conformance = conformance;
        info.IntroducedVersion = OdfVersion.Version14;
        info.RequiresExternalAccess = external;
        info.IsVolatile = volatileFunction;
        result.Add(info);
    }
}
