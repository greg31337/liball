namespace odf;

/// <summary>
/// Provides OpenFormula namespace names, error literals, and standardized function names.
/// </summary>
public static class OdfFormulaNames
{
    /// <summary>
    /// The OpenFormula namespace name.
    /// </summary>
    public const string OpenFormulaNamespace = "urn:oasis:names:tc:opendocument:xmlns:of:1.2";
    /// <summary>
    /// The standard OpenFormula namespace prefix.
    /// </summary>
    public const string OpenFormulaPrefix = "of";
    /// <summary>
    /// The legacy OpenOffice formula namespace name.
    /// </summary>
    public const string LegacyFormulaNamespace = "http://openoffice.org/2004/calc";
    /// <summary>
    /// The conventional legacy OpenOffice formula namespace prefix.
    /// </summary>
    public const string LegacyFormulaPrefix = "oooc";
    /// <summary>
    /// The empty-intersection error literal.
    /// </summary>
    public const string ErrorNull = "#NULL!";
    /// <summary>
    /// The division-by-zero error literal.
    /// </summary>
    public const string ErrorDivisionByZero = "#DIV/0!";
    /// <summary>
    /// The invalid-value error literal.
    /// </summary>
    public const string ErrorValue = "#VALUE!";
    /// <summary>
    /// The invalid-reference error literal.
    /// </summary>
    public const string ErrorReference = "#REF!";
    /// <summary>
    /// The unknown-name error literal.
    /// </summary>
    public const string ErrorName = "#NAME?";
    /// <summary>
    /// The invalid-number error literal.
    /// </summary>
    public const string ErrorNumber = "#NUM!";
    /// <summary>
    /// The unavailable-value error literal.
    /// </summary>
    public const string ErrorNotAvailable = "#N/A";
    /// <summary>
    /// The standardized <c>ABS</c> function name.
    /// </summary>
    public const string ABS = "ABS";
    /// <summary>
    /// The standardized <c>ACCRINT</c> function name.
    /// </summary>
    public const string ACCRINT = "ACCRINT";
    /// <summary>
    /// The standardized <c>ACCRINTM</c> function name.
    /// </summary>
    public const string ACCRINTM = "ACCRINTM";
    /// <summary>
    /// The standardized <c>ACOS</c> function name.
    /// </summary>
    public const string ACOS = "ACOS";
    /// <summary>
    /// The standardized <c>ACOSH</c> function name.
    /// </summary>
    public const string ACOSH = "ACOSH";
    /// <summary>
    /// The standardized <c>ACOT</c> function name.
    /// </summary>
    public const string ACOT = "ACOT";
    /// <summary>
    /// The standardized <c>ACOTH</c> function name.
    /// </summary>
    public const string ACOTH = "ACOTH";
    /// <summary>
    /// The standardized <c>ADDRESS</c> function name.
    /// </summary>
    public const string ADDRESS = "ADDRESS";
    /// <summary>
    /// The standardized <c>AMORLINC</c> function name.
    /// </summary>
    public const string AMORLINC = "AMORLINC";
    /// <summary>
    /// The standardized <c>AND</c> function name.
    /// </summary>
    public const string AND = "AND";
    /// <summary>
    /// The standardized <c>ARABIC</c> function name.
    /// </summary>
    public const string ARABIC = "ARABIC";
    /// <summary>
    /// The standardized <c>AREAS</c> function name.
    /// </summary>
    public const string AREAS = "AREAS";
    /// <summary>
    /// The standardized <c>ASC</c> function name.
    /// </summary>
    public const string ASC = "ASC";
    /// <summary>
    /// The standardized <c>ASIN</c> function name.
    /// </summary>
    public const string ASIN = "ASIN";
    /// <summary>
    /// The standardized <c>ASINH</c> function name.
    /// </summary>
    public const string ASINH = "ASINH";
    /// <summary>
    /// The standardized <c>ATAN</c> function name.
    /// </summary>
    public const string ATAN = "ATAN";
    /// <summary>
    /// The standardized <c>ATAN2</c> function name.
    /// </summary>
    public const string ATAN2 = "ATAN2";
    /// <summary>
    /// The standardized <c>ATANH</c> function name.
    /// </summary>
    public const string ATANH = "ATANH";
    /// <summary>
    /// The standardized <c>AVEDEV</c> function name.
    /// </summary>
    public const string AVEDEV = "AVEDEV";
    /// <summary>
    /// The standardized <c>AVERAGE</c> function name.
    /// </summary>
    public const string AVERAGE = "AVERAGE";
    /// <summary>
    /// The standardized <c>AVERAGEA</c> function name.
    /// </summary>
    public const string AVERAGEA = "AVERAGEA";
    /// <summary>
    /// The standardized <c>AVERAGEIF</c> function name.
    /// </summary>
    public const string AVERAGEIF = "AVERAGEIF";
    /// <summary>
    /// The standardized <c>AVERAGEIFS</c> function name.
    /// </summary>
    public const string AVERAGEIFS = "AVERAGEIFS";
    /// <summary>
    /// The standardized <c>BASE</c> function name.
    /// </summary>
    public const string BASE = "BASE";
    /// <summary>
    /// The standardized <c>BESSELI</c> function name.
    /// </summary>
    public const string BESSELI = "BESSELI";
    /// <summary>
    /// The standardized <c>BESSELJ</c> function name.
    /// </summary>
    public const string BESSELJ = "BESSELJ";
    /// <summary>
    /// The standardized <c>BESSELK</c> function name.
    /// </summary>
    public const string BESSELK = "BESSELK";
    /// <summary>
    /// The standardized <c>BESSELY</c> function name.
    /// </summary>
    public const string BESSELY = "BESSELY";
    /// <summary>
    /// The standardized <c>BETADIST</c> function name.
    /// </summary>
    public const string BETADIST = "BETADIST";
    /// <summary>
    /// The standardized <c>BETAINV</c> function name.
    /// </summary>
    public const string BETAINV = "BETAINV";
    /// <summary>
    /// The standardized <c>BIN2DEC</c> function name.
    /// </summary>
    public const string BIN2DEC = "BIN2DEC";
    /// <summary>
    /// The standardized <c>BIN2HEX</c> function name.
    /// </summary>
    public const string BIN2HEX = "BIN2HEX";
    /// <summary>
    /// The standardized <c>BIN2OCT</c> function name.
    /// </summary>
    public const string BIN2OCT = "BIN2OCT";
    /// <summary>
    /// The standardized <c>BINOMDIST</c> function name.
    /// </summary>
    public const string BINOMDIST = "BINOMDIST";
    /// <summary>
    /// The standardized <c>BINOM_DIST_RANGE</c> function name.
    /// </summary>
    public const string BINOM_DIST_RANGE = "BINOM_DIST_RANGE";
    /// <summary>
    /// The standardized <c>BITAND</c> function name.
    /// </summary>
    public const string BITAND = "BITAND";
    /// <summary>
    /// The standardized <c>BITLSHIFT</c> function name.
    /// </summary>
    public const string BITLSHIFT = "BITLSHIFT";
    /// <summary>
    /// The standardized <c>BITOR</c> function name.
    /// </summary>
    public const string BITOR = "BITOR";
    /// <summary>
    /// The standardized <c>BITRSHIFT</c> function name.
    /// </summary>
    public const string BITRSHIFT = "BITRSHIFT";
    /// <summary>
    /// The standardized <c>BITXOR</c> function name.
    /// </summary>
    public const string BITXOR = "BITXOR";
    /// <summary>
    /// The standardized <c>CEILING</c> function name.
    /// </summary>
    public const string CEILING = "CEILING";
    /// <summary>
    /// The standardized <c>CELL</c> function name.
    /// </summary>
    public const string CELL = "CELL";
    /// <summary>
    /// The standardized <c>CHAR</c> function name.
    /// </summary>
    public const string CHAR = "CHAR";
    /// <summary>
    /// The standardized <c>CHISQDIST</c> function name.
    /// </summary>
    public const string CHISQDIST = "CHISQDIST";
    /// <summary>
    /// The standardized <c>CHISQINV</c> function name.
    /// </summary>
    public const string CHISQINV = "CHISQINV";
    /// <summary>
    /// The standardized <c>CHOOSE</c> function name.
    /// </summary>
    public const string CHOOSE = "CHOOSE";
    /// <summary>
    /// The standardized <c>CLEAN</c> function name.
    /// </summary>
    public const string CLEAN = "CLEAN";
    /// <summary>
    /// The standardized <c>CODE</c> function name.
    /// </summary>
    public const string CODE = "CODE";
    /// <summary>
    /// The standardized <c>COLUMN</c> function name.
    /// </summary>
    public const string COLUMN = "COLUMN";
    /// <summary>
    /// The standardized <c>COLUMNS</c> function name.
    /// </summary>
    public const string COLUMNS = "COLUMNS";
    /// <summary>
    /// The standardized <c>COMBIN</c> function name.
    /// </summary>
    public const string COMBIN = "COMBIN";
    /// <summary>
    /// The standardized <c>COMBINA</c> function name.
    /// </summary>
    public const string COMBINA = "COMBINA";
    /// <summary>
    /// The standardized <c>COMPLEX</c> function name.
    /// </summary>
    public const string COMPLEX = "COMPLEX";
    /// <summary>
    /// The standardized <c>CONCATENATE</c> function name.
    /// </summary>
    public const string CONCATENATE = "CONCATENATE";
    /// <summary>
    /// The standardized <c>CONFIDENCE</c> function name.
    /// </summary>
    public const string CONFIDENCE = "CONFIDENCE";
    /// <summary>
    /// The standardized <c>CONVERT</c> function name.
    /// </summary>
    public const string CONVERT = "CONVERT";
    /// <summary>
    /// The standardized <c>CORREL</c> function name.
    /// </summary>
    public const string CORREL = "CORREL";
    /// <summary>
    /// The standardized <c>COS</c> function name.
    /// </summary>
    public const string COS = "COS";
    /// <summary>
    /// The standardized <c>COSH</c> function name.
    /// </summary>
    public const string COSH = "COSH";
    /// <summary>
    /// The standardized <c>COT</c> function name.
    /// </summary>
    public const string COT = "COT";
    /// <summary>
    /// The standardized <c>COTH</c> function name.
    /// </summary>
    public const string COTH = "COTH";
    /// <summary>
    /// The standardized <c>COUNT</c> function name.
    /// </summary>
    public const string COUNT = "COUNT";
    /// <summary>
    /// The standardized <c>COUNTA</c> function name.
    /// </summary>
    public const string COUNTA = "COUNTA";
    /// <summary>
    /// The standardized <c>COUNTBLANK</c> function name.
    /// </summary>
    public const string COUNTBLANK = "COUNTBLANK";
    /// <summary>
    /// The standardized <c>COUNTIF</c> function name.
    /// </summary>
    public const string COUNTIF = "COUNTIF";
    /// <summary>
    /// The standardized <c>COUNTIFS</c> function name.
    /// </summary>
    public const string COUNTIFS = "COUNTIFS";
    /// <summary>
    /// The standardized <c>COUPDAYBS</c> function name.
    /// </summary>
    public const string COUPDAYBS = "COUPDAYBS";
    /// <summary>
    /// The standardized <c>COUPDAYS</c> function name.
    /// </summary>
    public const string COUPDAYS = "COUPDAYS";
    /// <summary>
    /// The standardized <c>COUPDAYSNC</c> function name.
    /// </summary>
    public const string COUPDAYSNC = "COUPDAYSNC";
    /// <summary>
    /// The standardized <c>COUPNCD</c> function name.
    /// </summary>
    public const string COUPNCD = "COUPNCD";
    /// <summary>
    /// The standardized <c>COUPNUM</c> function name.
    /// </summary>
    public const string COUPNUM = "COUPNUM";
    /// <summary>
    /// The standardized <c>COUPPCD</c> function name.
    /// </summary>
    public const string COUPPCD = "COUPPCD";
    /// <summary>
    /// The standardized <c>COVAR</c> function name.
    /// </summary>
    public const string COVAR = "COVAR";
    /// <summary>
    /// The standardized <c>CRITBINOM</c> function name.
    /// </summary>
    public const string CRITBINOM = "CRITBINOM";
    /// <summary>
    /// The standardized <c>CSC</c> function name.
    /// </summary>
    public const string CSC = "CSC";
    /// <summary>
    /// The standardized <c>CSCH</c> function name.
    /// </summary>
    public const string CSCH = "CSCH";
    /// <summary>
    /// The standardized <c>CUMIPMT</c> function name.
    /// </summary>
    public const string CUMIPMT = "CUMIPMT";
    /// <summary>
    /// The standardized <c>CUMPRINC</c> function name.
    /// </summary>
    public const string CUMPRINC = "CUMPRINC";
    /// <summary>
    /// The standardized <c>DATE</c> function name.
    /// </summary>
    public const string DATE = "DATE";
    /// <summary>
    /// The standardized <c>DATEDIF</c> function name.
    /// </summary>
    public const string DATEDIF = "DATEDIF";
    /// <summary>
    /// The standardized <c>DATEVALUE</c> function name.
    /// </summary>
    public const string DATEVALUE = "DATEVALUE";
    /// <summary>
    /// The standardized <c>DAVERAGE</c> function name.
    /// </summary>
    public const string DAVERAGE = "DAVERAGE";
    /// <summary>
    /// The standardized <c>DAY</c> function name.
    /// </summary>
    public const string DAY = "DAY";
    /// <summary>
    /// The standardized <c>DAYS</c> function name.
    /// </summary>
    public const string DAYS = "DAYS";
    /// <summary>
    /// The standardized <c>DAYS360</c> function name.
    /// </summary>
    public const string DAYS360 = "DAYS360";
    /// <summary>
    /// The standardized <c>DB</c> function name.
    /// </summary>
    public const string DB = "DB";
    /// <summary>
    /// The standardized <c>DCOUNT</c> function name.
    /// </summary>
    public const string DCOUNT = "DCOUNT";
    /// <summary>
    /// The standardized <c>DCOUNTA</c> function name.
    /// </summary>
    public const string DCOUNTA = "DCOUNTA";
    /// <summary>
    /// The standardized <c>DDB</c> function name.
    /// </summary>
    public const string DDB = "DDB";
    /// <summary>
    /// The standardized <c>DDE</c> function name.
    /// </summary>
    public const string DDE = "DDE";
    /// <summary>
    /// The standardized <c>DEC2BIN</c> function name.
    /// </summary>
    public const string DEC2BIN = "DEC2BIN";
    /// <summary>
    /// The standardized <c>DEC2HEX</c> function name.
    /// </summary>
    public const string DEC2HEX = "DEC2HEX";
    /// <summary>
    /// The standardized <c>DEC2OCT</c> function name.
    /// </summary>
    public const string DEC2OCT = "DEC2OCT";
    /// <summary>
    /// The standardized <c>DECIMAL</c> function name.
    /// </summary>
    public const string DECIMAL = "DECIMAL";
    /// <summary>
    /// The standardized <c>DEGREES</c> function name.
    /// </summary>
    public const string DEGREES = "DEGREES";
    /// <summary>
    /// The standardized <c>DELTA</c> function name.
    /// </summary>
    public const string DELTA = "DELTA";
    /// <summary>
    /// The standardized <c>DEVSQ</c> function name.
    /// </summary>
    public const string DEVSQ = "DEVSQ";
    /// <summary>
    /// The standardized <c>DGET</c> function name.
    /// </summary>
    public const string DGET = "DGET";
    /// <summary>
    /// The standardized <c>DISC</c> function name.
    /// </summary>
    public const string DISC = "DISC";
    /// <summary>
    /// The standardized <c>DMAX</c> function name.
    /// </summary>
    public const string DMAX = "DMAX";
    /// <summary>
    /// The standardized <c>DMIN</c> function name.
    /// </summary>
    public const string DMIN = "DMIN";
    /// <summary>
    /// The standardized <c>DOLLAR</c> function name.
    /// </summary>
    public const string DOLLAR = "DOLLAR";
    /// <summary>
    /// The standardized <c>DOLLARDE</c> function name.
    /// </summary>
    public const string DOLLARDE = "DOLLARDE";
    /// <summary>
    /// The standardized <c>DOLLARFR</c> function name.
    /// </summary>
    public const string DOLLARFR = "DOLLARFR";
    /// <summary>
    /// The standardized <c>DPRODUCT</c> function name.
    /// </summary>
    public const string DPRODUCT = "DPRODUCT";
    /// <summary>
    /// The standardized <c>DSTDEV</c> function name.
    /// </summary>
    public const string DSTDEV = "DSTDEV";
    /// <summary>
    /// The standardized <c>DSTDEVP</c> function name.
    /// </summary>
    public const string DSTDEVP = "DSTDEVP";
    /// <summary>
    /// The standardized <c>DSUM</c> function name.
    /// </summary>
    public const string DSUM = "DSUM";
    /// <summary>
    /// The standardized <c>DURATION</c> function name.
    /// </summary>
    public const string DURATION = "DURATION";
    /// <summary>
    /// The standardized <c>DVAR</c> function name.
    /// </summary>
    public const string DVAR = "DVAR";
    /// <summary>
    /// The standardized <c>DVARP</c> function name.
    /// </summary>
    public const string DVARP = "DVARP";
    /// <summary>
    /// The standardized <c>EASTERSUNDAY</c> function name.
    /// </summary>
    public const string EASTERSUNDAY = "EASTERSUNDAY";
    /// <summary>
    /// The standardized <c>EDATE</c> function name.
    /// </summary>
    public const string EDATE = "EDATE";
    /// <summary>
    /// The standardized <c>EFFECT</c> function name.
    /// </summary>
    public const string EFFECT = "EFFECT";
    /// <summary>
    /// The standardized <c>EOMONTH</c> function name.
    /// </summary>
    public const string EOMONTH = "EOMONTH";
    /// <summary>
    /// The standardized <c>ERF</c> function name.
    /// </summary>
    public const string ERF = "ERF";
    /// <summary>
    /// The standardized <c>ERFC</c> function name.
    /// </summary>
    public const string ERFC = "ERFC";
    /// <summary>
    /// The standardized <c>ERROR_TYPE</c> function name.
    /// </summary>
    public const string ERROR_TYPE = "ERROR_TYPE";
    /// <summary>
    /// The standardized <c>EUROCONVERT</c> function name.
    /// </summary>
    public const string EUROCONVERT = "EUROCONVERT";
    /// <summary>
    /// The standardized <c>EVEN</c> function name.
    /// </summary>
    public const string EVEN = "EVEN";
    /// <summary>
    /// The standardized <c>EXACT</c> function name.
    /// </summary>
    public const string EXACT = "EXACT";
    /// <summary>
    /// The standardized <c>EXP</c> function name.
    /// </summary>
    public const string EXP = "EXP";
    /// <summary>
    /// The standardized <c>EXPONDIST</c> function name.
    /// </summary>
    public const string EXPONDIST = "EXPONDIST";
    /// <summary>
    /// The standardized <c>FACT</c> function name.
    /// </summary>
    public const string FACT = "FACT";
    /// <summary>
    /// The standardized <c>FACTDOUBLE</c> function name.
    /// </summary>
    public const string FACTDOUBLE = "FACTDOUBLE";
    /// <summary>
    /// The standardized <c>FALSE</c> function name.
    /// </summary>
    public const string FALSE = "FALSE";
    /// <summary>
    /// The standardized <c>FDIST</c> function name.
    /// </summary>
    public const string FDIST = "FDIST";
    /// <summary>
    /// The standardized <c>FIND</c> function name.
    /// </summary>
    public const string FIND = "FIND";
    /// <summary>
    /// The standardized <c>FINDB</c> function name.
    /// </summary>
    public const string FINDB = "FINDB";
    /// <summary>
    /// The standardized <c>FINV</c> function name.
    /// </summary>
    public const string FINV = "FINV";
    /// <summary>
    /// The standardized <c>FISHER</c> function name.
    /// </summary>
    public const string FISHER = "FISHER";
    /// <summary>
    /// The standardized <c>FISHERINV</c> function name.
    /// </summary>
    public const string FISHERINV = "FISHERINV";
    /// <summary>
    /// The standardized <c>FIXED</c> function name.
    /// </summary>
    public const string FIXED = "FIXED";
    /// <summary>
    /// The standardized <c>FLOOR</c> function name.
    /// </summary>
    public const string FLOOR = "FLOOR";
    /// <summary>
    /// The standardized <c>FORECAST</c> function name.
    /// </summary>
    public const string FORECAST = "FORECAST";
    /// <summary>
    /// The standardized <c>FORMULA</c> function name.
    /// </summary>
    public const string FORMULA = "FORMULA";
    /// <summary>
    /// The standardized <c>FREQUENCY</c> function name.
    /// </summary>
    public const string FREQUENCY = "FREQUENCY";
    /// <summary>
    /// The standardized <c>FTEST</c> function name.
    /// </summary>
    public const string FTEST = "FTEST";
    /// <summary>
    /// The standardized <c>FV</c> function name.
    /// </summary>
    public const string FV = "FV";
    /// <summary>
    /// The standardized <c>FVSCHEDULE</c> function name.
    /// </summary>
    public const string FVSCHEDULE = "FVSCHEDULE";
    /// <summary>
    /// The standardized <c>GAMMA</c> function name.
    /// </summary>
    public const string GAMMA = "GAMMA";
    /// <summary>
    /// The standardized <c>GAMMADIST</c> function name.
    /// </summary>
    public const string GAMMADIST = "GAMMADIST";
    /// <summary>
    /// The standardized <c>GAMMAINV</c> function name.
    /// </summary>
    public const string GAMMAINV = "GAMMAINV";
    /// <summary>
    /// The standardized <c>GAMMALN</c> function name.
    /// </summary>
    public const string GAMMALN = "GAMMALN";
    /// <summary>
    /// The standardized <c>GAUSS</c> function name.
    /// </summary>
    public const string GAUSS = "GAUSS";
    /// <summary>
    /// The standardized <c>GCD</c> function name.
    /// </summary>
    public const string GCD = "GCD";
    /// <summary>
    /// The standardized <c>GEOMEAN</c> function name.
    /// </summary>
    public const string GEOMEAN = "GEOMEAN";
    /// <summary>
    /// The standardized <c>GESTEP</c> function name.
    /// </summary>
    public const string GESTEP = "GESTEP";
    /// <summary>
    /// The standardized <c>GETPIVOTDATA</c> function name.
    /// </summary>
    public const string GETPIVOTDATA = "GETPIVOTDATA";
    /// <summary>
    /// The standardized <c>GROWTH</c> function name.
    /// </summary>
    public const string GROWTH = "GROWTH";
    /// <summary>
    /// The standardized <c>HARMEAN</c> function name.
    /// </summary>
    public const string HARMEAN = "HARMEAN";
    /// <summary>
    /// The standardized <c>HEX2BIN</c> function name.
    /// </summary>
    public const string HEX2BIN = "HEX2BIN";
    /// <summary>
    /// The standardized <c>HEX2DEC</c> function name.
    /// </summary>
    public const string HEX2DEC = "HEX2DEC";
    /// <summary>
    /// The standardized <c>HEX2OCT</c> function name.
    /// </summary>
    public const string HEX2OCT = "HEX2OCT";
    /// <summary>
    /// The standardized <c>HLOOKUP</c> function name.
    /// </summary>
    public const string HLOOKUP = "HLOOKUP";
    /// <summary>
    /// The standardized <c>HOUR</c> function name.
    /// </summary>
    public const string HOUR = "HOUR";
    /// <summary>
    /// The standardized <c>HYPERLINK</c> function name.
    /// </summary>
    public const string HYPERLINK = "HYPERLINK";
    /// <summary>
    /// The standardized <c>HYPGEOMDIST</c> function name.
    /// </summary>
    public const string HYPGEOMDIST = "HYPGEOMDIST";
    /// <summary>
    /// The standardized <c>IF</c> function name.
    /// </summary>
    public const string IF = "IF";
    /// <summary>
    /// The standardized <c>IFERROR</c> function name.
    /// </summary>
    public const string IFERROR = "IFERROR";
    /// <summary>
    /// The standardized <c>IFNA</c> function name.
    /// </summary>
    public const string IFNA = "IFNA";
    /// <summary>
    /// The standardized <c>IMABS</c> function name.
    /// </summary>
    public const string IMABS = "IMABS";
    /// <summary>
    /// The standardized <c>IMAGINARY</c> function name.
    /// </summary>
    public const string IMAGINARY = "IMAGINARY";
    /// <summary>
    /// The standardized <c>IMARGUMENT</c> function name.
    /// </summary>
    public const string IMARGUMENT = "IMARGUMENT";
    /// <summary>
    /// The standardized <c>IMCONJUGATE</c> function name.
    /// </summary>
    public const string IMCONJUGATE = "IMCONJUGATE";
    /// <summary>
    /// The standardized <c>IMCOS</c> function name.
    /// </summary>
    public const string IMCOS = "IMCOS";
    /// <summary>
    /// The standardized <c>IMCOSH</c> function name.
    /// </summary>
    public const string IMCOSH = "IMCOSH";
    /// <summary>
    /// The standardized <c>IMCOT</c> function name.
    /// </summary>
    public const string IMCOT = "IMCOT";
    /// <summary>
    /// The standardized <c>IMCSC</c> function name.
    /// </summary>
    public const string IMCSC = "IMCSC";
    /// <summary>
    /// The standardized <c>IMCSCH</c> function name.
    /// </summary>
    public const string IMCSCH = "IMCSCH";
    /// <summary>
    /// The standardized <c>IMDIV</c> function name.
    /// </summary>
    public const string IMDIV = "IMDIV";
    /// <summary>
    /// The standardized <c>IMEXP</c> function name.
    /// </summary>
    public const string IMEXP = "IMEXP";
    /// <summary>
    /// The standardized <c>IMLN</c> function name.
    /// </summary>
    public const string IMLN = "IMLN";
    /// <summary>
    /// The standardized <c>IMLOG10</c> function name.
    /// </summary>
    public const string IMLOG10 = "IMLOG10";
    /// <summary>
    /// The standardized <c>IMLOG2</c> function name.
    /// </summary>
    public const string IMLOG2 = "IMLOG2";
    /// <summary>
    /// The standardized <c>IMPOWER</c> function name.
    /// </summary>
    public const string IMPOWER = "IMPOWER";
    /// <summary>
    /// The standardized <c>IMPRODUCT</c> function name.
    /// </summary>
    public const string IMPRODUCT = "IMPRODUCT";
    /// <summary>
    /// The standardized <c>IMREAL</c> function name.
    /// </summary>
    public const string IMREAL = "IMREAL";
    /// <summary>
    /// The standardized <c>IMSEC</c> function name.
    /// </summary>
    public const string IMSEC = "IMSEC";
    /// <summary>
    /// The standardized <c>IMSECH</c> function name.
    /// </summary>
    public const string IMSECH = "IMSECH";
    /// <summary>
    /// The standardized <c>IMSIN</c> function name.
    /// </summary>
    public const string IMSIN = "IMSIN";
    /// <summary>
    /// The standardized <c>IMSINH</c> function name.
    /// </summary>
    public const string IMSINH = "IMSINH";
    /// <summary>
    /// The standardized <c>IMSQRT</c> function name.
    /// </summary>
    public const string IMSQRT = "IMSQRT";
    /// <summary>
    /// The standardized <c>IMSUB</c> function name.
    /// </summary>
    public const string IMSUB = "IMSUB";
    /// <summary>
    /// The standardized <c>IMSUM</c> function name.
    /// </summary>
    public const string IMSUM = "IMSUM";
    /// <summary>
    /// The standardized <c>IMTAN</c> function name.
    /// </summary>
    public const string IMTAN = "IMTAN";
    /// <summary>
    /// The standardized <c>INDEX</c> function name.
    /// </summary>
    public const string INDEX = "INDEX";
    /// <summary>
    /// The standardized <c>INDIRECT</c> function name.
    /// </summary>
    public const string INDIRECT = "INDIRECT";
    /// <summary>
    /// The standardized <c>INFO</c> function name.
    /// </summary>
    public const string INFO = "INFO";
    /// <summary>
    /// The standardized <c>INT</c> function name.
    /// </summary>
    public const string INT = "INT";
    /// <summary>
    /// The standardized <c>INTERCEPT</c> function name.
    /// </summary>
    public const string INTERCEPT = "INTERCEPT";
    /// <summary>
    /// The standardized <c>INTRATE</c> function name.
    /// </summary>
    public const string INTRATE = "INTRATE";
    /// <summary>
    /// The standardized <c>IPMT</c> function name.
    /// </summary>
    public const string IPMT = "IPMT";
    /// <summary>
    /// The standardized <c>IRR</c> function name.
    /// </summary>
    public const string IRR = "IRR";
    /// <summary>
    /// The standardized <c>ISBLANK</c> function name.
    /// </summary>
    public const string ISBLANK = "ISBLANK";
    /// <summary>
    /// The standardized <c>ISERR</c> function name.
    /// </summary>
    public const string ISERR = "ISERR";
    /// <summary>
    /// The standardized <c>ISERROR</c> function name.
    /// </summary>
    public const string ISERROR = "ISERROR";
    /// <summary>
    /// The standardized <c>ISEVEN</c> function name.
    /// </summary>
    public const string ISEVEN = "ISEVEN";
    /// <summary>
    /// The standardized <c>ISFORMULA</c> function name.
    /// </summary>
    public const string ISFORMULA = "ISFORMULA";
    /// <summary>
    /// The standardized <c>ISLOGICAL</c> function name.
    /// </summary>
    public const string ISLOGICAL = "ISLOGICAL";
    /// <summary>
    /// The standardized <c>ISNA</c> function name.
    /// </summary>
    public const string ISNA = "ISNA";
    /// <summary>
    /// The standardized <c>ISNONTEXT</c> function name.
    /// </summary>
    public const string ISNONTEXT = "ISNONTEXT";
    /// <summary>
    /// The standardized <c>ISNUMBER</c> function name.
    /// </summary>
    public const string ISNUMBER = "ISNUMBER";
    /// <summary>
    /// The standardized <c>ISODD</c> function name.
    /// </summary>
    public const string ISODD = "ISODD";
    /// <summary>
    /// The standardized <c>ISOWEEKNUM</c> function name.
    /// </summary>
    public const string ISOWEEKNUM = "ISOWEEKNUM";
    /// <summary>
    /// The standardized <c>ISPMT</c> function name.
    /// </summary>
    public const string ISPMT = "ISPMT";
    /// <summary>
    /// The standardized <c>ISREF</c> function name.
    /// </summary>
    public const string ISREF = "ISREF";
    /// <summary>
    /// The standardized <c>ISTEXT</c> function name.
    /// </summary>
    public const string ISTEXT = "ISTEXT";
    /// <summary>
    /// The standardized <c>JIS</c> function name.
    /// </summary>
    public const string JIS = "JIS";
    /// <summary>
    /// The standardized <c>KURT</c> function name.
    /// </summary>
    public const string KURT = "KURT";
    /// <summary>
    /// The standardized <c>LARGE</c> function name.
    /// </summary>
    public const string LARGE = "LARGE";
    /// <summary>
    /// The standardized <c>LCM</c> function name.
    /// </summary>
    public const string LCM = "LCM";
    /// <summary>
    /// The standardized <c>LEFT</c> function name.
    /// </summary>
    public const string LEFT = "LEFT";
    /// <summary>
    /// The standardized <c>LEFTB</c> function name.
    /// </summary>
    public const string LEFTB = "LEFTB";
    /// <summary>
    /// The standardized <c>LEGACY_CHIDIST</c> function name.
    /// </summary>
    public const string LEGACY_CHIDIST = "LEGACY_CHIDIST";
    /// <summary>
    /// The standardized <c>LEGACY_CHIINV</c> function name.
    /// </summary>
    public const string LEGACY_CHIINV = "LEGACY_CHIINV";
    /// <summary>
    /// The standardized <c>LEGACY_CHITEST</c> function name.
    /// </summary>
    public const string LEGACY_CHITEST = "LEGACY_CHITEST";
    /// <summary>
    /// The standardized <c>LEGACY_FDIST</c> function name.
    /// </summary>
    public const string LEGACY_FDIST = "LEGACY_FDIST";
    /// <summary>
    /// The standardized <c>LEGACY_FINV</c> function name.
    /// </summary>
    public const string LEGACY_FINV = "LEGACY_FINV";
    /// <summary>
    /// The standardized <c>LEGACY_NORMSDIST</c> function name.
    /// </summary>
    public const string LEGACY_NORMSDIST = "LEGACY_NORMSDIST";
    /// <summary>
    /// The standardized <c>LEGACY_NORMSINV</c> function name.
    /// </summary>
    public const string LEGACY_NORMSINV = "LEGACY_NORMSINV";
    /// <summary>
    /// The standardized <c>LEGACY_TDIST</c> function name.
    /// </summary>
    public const string LEGACY_TDIST = "LEGACY_TDIST";
    /// <summary>
    /// The standardized <c>LEN</c> function name.
    /// </summary>
    public const string LEN = "LEN";
    /// <summary>
    /// The standardized <c>LENB</c> function name.
    /// </summary>
    public const string LENB = "LENB";
    /// <summary>
    /// The standardized <c>LINEST</c> function name.
    /// </summary>
    public const string LINEST = "LINEST";
    /// <summary>
    /// The standardized <c>LN</c> function name.
    /// </summary>
    public const string LN = "LN";
    /// <summary>
    /// The standardized <c>LOG</c> function name.
    /// </summary>
    public const string LOG = "LOG";
    /// <summary>
    /// The standardized <c>LOG10</c> function name.
    /// </summary>
    public const string LOG10 = "LOG10";
    /// <summary>
    /// The standardized <c>LOGEST</c> function name.
    /// </summary>
    public const string LOGEST = "LOGEST";
    /// <summary>
    /// The standardized <c>LOGINV</c> function name.
    /// </summary>
    public const string LOGINV = "LOGINV";
    /// <summary>
    /// The standardized <c>LOGNORMDIST</c> function name.
    /// </summary>
    public const string LOGNORMDIST = "LOGNORMDIST";
    /// <summary>
    /// The standardized <c>LOOKUP</c> function name.
    /// </summary>
    public const string LOOKUP = "LOOKUP";
    /// <summary>
    /// The standardized <c>LOWER</c> function name.
    /// </summary>
    public const string LOWER = "LOWER";
    /// <summary>
    /// The standardized <c>MATCH</c> function name.
    /// </summary>
    public const string MATCH = "MATCH";
    /// <summary>
    /// The standardized <c>MAX</c> function name.
    /// </summary>
    public const string MAX = "MAX";
    /// <summary>
    /// The standardized <c>MAXA</c> function name.
    /// </summary>
    public const string MAXA = "MAXA";
    /// <summary>
    /// The standardized <c>MDETERM</c> function name.
    /// </summary>
    public const string MDETERM = "MDETERM";
    /// <summary>
    /// The standardized <c>MDURATION</c> function name.
    /// </summary>
    public const string MDURATION = "MDURATION";
    /// <summary>
    /// The standardized <c>MEDIAN</c> function name.
    /// </summary>
    public const string MEDIAN = "MEDIAN";
    /// <summary>
    /// The standardized <c>MID</c> function name.
    /// </summary>
    public const string MID = "MID";
    /// <summary>
    /// The standardized <c>MIDB</c> function name.
    /// </summary>
    public const string MIDB = "MIDB";
    /// <summary>
    /// The standardized <c>MIN</c> function name.
    /// </summary>
    public const string MIN = "MIN";
    /// <summary>
    /// The standardized <c>MINA</c> function name.
    /// </summary>
    public const string MINA = "MINA";
    /// <summary>
    /// The standardized <c>MINUTE</c> function name.
    /// </summary>
    public const string MINUTE = "MINUTE";
    /// <summary>
    /// The standardized <c>MINVERSE</c> function name.
    /// </summary>
    public const string MINVERSE = "MINVERSE";
    /// <summary>
    /// The standardized <c>MIRR</c> function name.
    /// </summary>
    public const string MIRR = "MIRR";
    /// <summary>
    /// The standardized <c>MMULT</c> function name.
    /// </summary>
    public const string MMULT = "MMULT";
    /// <summary>
    /// The standardized <c>MOD</c> function name.
    /// </summary>
    public const string MOD = "MOD";
    /// <summary>
    /// The standardized <c>MODE</c> function name.
    /// </summary>
    public const string MODE = "MODE";
    /// <summary>
    /// The standardized <c>MONTH</c> function name.
    /// </summary>
    public const string MONTH = "MONTH";
    /// <summary>
    /// The standardized <c>MROUND</c> function name.
    /// </summary>
    public const string MROUND = "MROUND";
    /// <summary>
    /// The standardized <c>MULTINOMIAL</c> function name.
    /// </summary>
    public const string MULTINOMIAL = "MULTINOMIAL";
    /// <summary>
    /// The standardized <c>MULTIPLE_OPERATIONS</c> function name.
    /// </summary>
    public const string MULTIPLE_OPERATIONS = "MULTIPLE_OPERATIONS";
    /// <summary>
    /// The standardized <c>MUNIT</c> function name.
    /// </summary>
    public const string MUNIT = "MUNIT";
    /// <summary>
    /// The standardized <c>N</c> function name.
    /// </summary>
    public const string N = "N";
    /// <summary>
    /// The standardized <c>NA</c> function name.
    /// </summary>
    public const string NA = "NA";
    /// <summary>
    /// The standardized <c>NEGBINOMDIST</c> function name.
    /// </summary>
    public const string NEGBINOMDIST = "NEGBINOMDIST";
    /// <summary>
    /// The standardized <c>NETWORKDAYS</c> function name.
    /// </summary>
    public const string NETWORKDAYS = "NETWORKDAYS";
    /// <summary>
    /// The standardized <c>NOMINAL</c> function name.
    /// </summary>
    public const string NOMINAL = "NOMINAL";
    /// <summary>
    /// The standardized <c>NORMDIST</c> function name.
    /// </summary>
    public const string NORMDIST = "NORMDIST";
    /// <summary>
    /// The standardized <c>NORMINV</c> function name.
    /// </summary>
    public const string NORMINV = "NORMINV";
    /// <summary>
    /// The standardized <c>NOT</c> function name.
    /// </summary>
    public const string NOT = "NOT";
    /// <summary>
    /// The standardized <c>NOW</c> function name.
    /// </summary>
    public const string NOW = "NOW";
    /// <summary>
    /// The standardized <c>NPER</c> function name.
    /// </summary>
    public const string NPER = "NPER";
    /// <summary>
    /// The standardized <c>NPV</c> function name.
    /// </summary>
    public const string NPV = "NPV";
    /// <summary>
    /// The standardized <c>NUMBERVALUE</c> function name.
    /// </summary>
    public const string NUMBERVALUE = "NUMBERVALUE";
    /// <summary>
    /// The standardized <c>OCT2BIN</c> function name.
    /// </summary>
    public const string OCT2BIN = "OCT2BIN";
    /// <summary>
    /// The standardized <c>OCT2DEC</c> function name.
    /// </summary>
    public const string OCT2DEC = "OCT2DEC";
    /// <summary>
    /// The standardized <c>OCT2HEX</c> function name.
    /// </summary>
    public const string OCT2HEX = "OCT2HEX";
    /// <summary>
    /// The standardized <c>ODD</c> function name.
    /// </summary>
    public const string ODD = "ODD";
    /// <summary>
    /// The standardized <c>ODDFPRICE</c> function name.
    /// </summary>
    public const string ODDFPRICE = "ODDFPRICE";
    /// <summary>
    /// The standardized <c>ODDFYIELD</c> function name.
    /// </summary>
    public const string ODDFYIELD = "ODDFYIELD";
    /// <summary>
    /// The standardized <c>ODDLPRICE</c> function name.
    /// </summary>
    public const string ODDLPRICE = "ODDLPRICE";
    /// <summary>
    /// The standardized <c>ODDLYIELD</c> function name.
    /// </summary>
    public const string ODDLYIELD = "ODDLYIELD";
    /// <summary>
    /// The standardized <c>OFFSET</c> function name.
    /// </summary>
    public const string OFFSET = "OFFSET";
    /// <summary>
    /// The standardized <c>OR</c> function name.
    /// </summary>
    public const string OR = "OR";
    /// <summary>
    /// The standardized <c>PDURATION</c> function name.
    /// </summary>
    public const string PDURATION = "PDURATION";
    /// <summary>
    /// The standardized <c>PEARSON</c> function name.
    /// </summary>
    public const string PEARSON = "PEARSON";
    /// <summary>
    /// The standardized <c>PERCENTILE</c> function name.
    /// </summary>
    public const string PERCENTILE = "PERCENTILE";
    /// <summary>
    /// The standardized <c>PERCENTRANK</c> function name.
    /// </summary>
    public const string PERCENTRANK = "PERCENTRANK";
    /// <summary>
    /// The standardized <c>PERMUT</c> function name.
    /// </summary>
    public const string PERMUT = "PERMUT";
    /// <summary>
    /// The standardized <c>PERMUTATIONA</c> function name.
    /// </summary>
    public const string PERMUTATIONA = "PERMUTATIONA";
    /// <summary>
    /// The standardized <c>PHI</c> function name.
    /// </summary>
    public const string PHI = "PHI";
    /// <summary>
    /// The standardized <c>PI</c> function name.
    /// </summary>
    public const string PI = "PI";
    /// <summary>
    /// The standardized <c>PMT</c> function name.
    /// </summary>
    public const string PMT = "PMT";
    /// <summary>
    /// The standardized <c>POISSON</c> function name.
    /// </summary>
    public const string POISSON = "POISSON";
    /// <summary>
    /// The standardized <c>POWER</c> function name.
    /// </summary>
    public const string POWER = "POWER";
    /// <summary>
    /// The standardized <c>PPMT</c> function name.
    /// </summary>
    public const string PPMT = "PPMT";
    /// <summary>
    /// The standardized <c>PRICE</c> function name.
    /// </summary>
    public const string PRICE = "PRICE";
    /// <summary>
    /// The standardized <c>PRICEDISC</c> function name.
    /// </summary>
    public const string PRICEDISC = "PRICEDISC";
    /// <summary>
    /// The standardized <c>PRICEMAT</c> function name.
    /// </summary>
    public const string PRICEMAT = "PRICEMAT";
    /// <summary>
    /// The standardized <c>PROB</c> function name.
    /// </summary>
    public const string PROB = "PROB";
    /// <summary>
    /// The standardized <c>PRODUCT</c> function name.
    /// </summary>
    public const string PRODUCT = "PRODUCT";
    /// <summary>
    /// The standardized <c>PROPER</c> function name.
    /// </summary>
    public const string PROPER = "PROPER";
    /// <summary>
    /// The standardized <c>PV</c> function name.
    /// </summary>
    public const string PV = "PV";
    /// <summary>
    /// The standardized <c>QUARTILE</c> function name.
    /// </summary>
    public const string QUARTILE = "QUARTILE";
    /// <summary>
    /// The standardized <c>QUOTIENT</c> function name.
    /// </summary>
    public const string QUOTIENT = "QUOTIENT";
    /// <summary>
    /// The standardized <c>RADIANS</c> function name.
    /// </summary>
    public const string RADIANS = "RADIANS";
    /// <summary>
    /// The standardized <c>RAND</c> function name.
    /// </summary>
    public const string RAND = "RAND";
    /// <summary>
    /// The standardized <c>RANDBETWEEN</c> function name.
    /// </summary>
    public const string RANDBETWEEN = "RANDBETWEEN";
    /// <summary>
    /// The standardized <c>RANK</c> function name.
    /// </summary>
    public const string RANK = "RANK";
    /// <summary>
    /// The standardized <c>RATE</c> function name.
    /// </summary>
    public const string RATE = "RATE";
    /// <summary>
    /// The standardized <c>RECEIVED</c> function name.
    /// </summary>
    public const string RECEIVED = "RECEIVED";
    /// <summary>
    /// The standardized <c>REPLACE</c> function name.
    /// </summary>
    public const string REPLACE = "REPLACE";
    /// <summary>
    /// The standardized <c>REPLACEB</c> function name.
    /// </summary>
    public const string REPLACEB = "REPLACEB";
    /// <summary>
    /// The standardized <c>REPT</c> function name.
    /// </summary>
    public const string REPT = "REPT";
    /// <summary>
    /// The standardized <c>RIGHT</c> function name.
    /// </summary>
    public const string RIGHT = "RIGHT";
    /// <summary>
    /// The standardized <c>RIGHTB</c> function name.
    /// </summary>
    public const string RIGHTB = "RIGHTB";
    /// <summary>
    /// The standardized <c>ROMAN</c> function name.
    /// </summary>
    public const string ROMAN = "ROMAN";
    /// <summary>
    /// The standardized <c>ROUND</c> function name.
    /// </summary>
    public const string ROUND = "ROUND";
    /// <summary>
    /// The standardized <c>ROUNDDOWN</c> function name.
    /// </summary>
    public const string ROUNDDOWN = "ROUNDDOWN";
    /// <summary>
    /// The standardized <c>ROUNDUP</c> function name.
    /// </summary>
    public const string ROUNDUP = "ROUNDUP";
    /// <summary>
    /// The standardized <c>ROW</c> function name.
    /// </summary>
    public const string ROW = "ROW";
    /// <summary>
    /// The standardized <c>ROWS</c> function name.
    /// </summary>
    public const string ROWS = "ROWS";
    /// <summary>
    /// The standardized <c>RRI</c> function name.
    /// </summary>
    public const string RRI = "RRI";
    /// <summary>
    /// The standardized <c>RSQ</c> function name.
    /// </summary>
    public const string RSQ = "RSQ";
    /// <summary>
    /// The standardized <c>SEARCH</c> function name.
    /// </summary>
    public const string SEARCH = "SEARCH";
    /// <summary>
    /// The standardized <c>SEARCHB</c> function name.
    /// </summary>
    public const string SEARCHB = "SEARCHB";
    /// <summary>
    /// The standardized <c>SEC</c> function name.
    /// </summary>
    public const string SEC = "SEC";
    /// <summary>
    /// The standardized <c>SECH</c> function name.
    /// </summary>
    public const string SECH = "SECH";
    /// <summary>
    /// The standardized <c>SECOND</c> function name.
    /// </summary>
    public const string SECOND = "SECOND";
    /// <summary>
    /// The standardized <c>SERIESSUM</c> function name.
    /// </summary>
    public const string SERIESSUM = "SERIESSUM";
    /// <summary>
    /// The standardized <c>SHEET</c> function name.
    /// </summary>
    public const string SHEET = "SHEET";
    /// <summary>
    /// The standardized <c>SHEETS</c> function name.
    /// </summary>
    public const string SHEETS = "SHEETS";
    /// <summary>
    /// The standardized <c>SIGN</c> function name.
    /// </summary>
    public const string SIGN = "SIGN";
    /// <summary>
    /// The standardized <c>SIN</c> function name.
    /// </summary>
    public const string SIN = "SIN";
    /// <summary>
    /// The standardized <c>SINH</c> function name.
    /// </summary>
    public const string SINH = "SINH";
    /// <summary>
    /// The standardized <c>SKEW</c> function name.
    /// </summary>
    public const string SKEW = "SKEW";
    /// <summary>
    /// The standardized <c>SKEWP</c> function name.
    /// </summary>
    public const string SKEWP = "SKEWP";
    /// <summary>
    /// The standardized <c>SLN</c> function name.
    /// </summary>
    public const string SLN = "SLN";
    /// <summary>
    /// The standardized <c>SLOPE</c> function name.
    /// </summary>
    public const string SLOPE = "SLOPE";
    /// <summary>
    /// The standardized <c>SMALL</c> function name.
    /// </summary>
    public const string SMALL = "SMALL";
    /// <summary>
    /// The standardized <c>SQRT</c> function name.
    /// </summary>
    public const string SQRT = "SQRT";
    /// <summary>
    /// The standardized <c>SQRTPI</c> function name.
    /// </summary>
    public const string SQRTPI = "SQRTPI";
    /// <summary>
    /// The standardized <c>STANDARDIZE</c> function name.
    /// </summary>
    public const string STANDARDIZE = "STANDARDIZE";
    /// <summary>
    /// The standardized <c>STDEV</c> function name.
    /// </summary>
    public const string STDEV = "STDEV";
    /// <summary>
    /// The standardized <c>STDEVA</c> function name.
    /// </summary>
    public const string STDEVA = "STDEVA";
    /// <summary>
    /// The standardized <c>STDEVP</c> function name.
    /// </summary>
    public const string STDEVP = "STDEVP";
    /// <summary>
    /// The standardized <c>STDEVPA</c> function name.
    /// </summary>
    public const string STDEVPA = "STDEVPA";
    /// <summary>
    /// The standardized <c>STEYX</c> function name.
    /// </summary>
    public const string STEYX = "STEYX";
    /// <summary>
    /// The standardized <c>SUBSTITUTE</c> function name.
    /// </summary>
    public const string SUBSTITUTE = "SUBSTITUTE";
    /// <summary>
    /// The standardized <c>SUBTOTAL</c> function name.
    /// </summary>
    public const string SUBTOTAL = "SUBTOTAL";
    /// <summary>
    /// The standardized <c>SUM</c> function name.
    /// </summary>
    public const string SUM = "SUM";
    /// <summary>
    /// The standardized <c>SUMIF</c> function name.
    /// </summary>
    public const string SUMIF = "SUMIF";
    /// <summary>
    /// The standardized <c>SUMIFS</c> function name.
    /// </summary>
    public const string SUMIFS = "SUMIFS";
    /// <summary>
    /// The standardized <c>SUMPRODUCT</c> function name.
    /// </summary>
    public const string SUMPRODUCT = "SUMPRODUCT";
    /// <summary>
    /// The standardized <c>SUMSQ</c> function name.
    /// </summary>
    public const string SUMSQ = "SUMSQ";
    /// <summary>
    /// The standardized <c>SUMX2MY2</c> function name.
    /// </summary>
    public const string SUMX2MY2 = "SUMX2MY2";
    /// <summary>
    /// The standardized <c>SUMX2PY2</c> function name.
    /// </summary>
    public const string SUMX2PY2 = "SUMX2PY2";
    /// <summary>
    /// The standardized <c>SUMXMY2</c> function name.
    /// </summary>
    public const string SUMXMY2 = "SUMXMY2";
    /// <summary>
    /// The standardized <c>SYD</c> function name.
    /// </summary>
    public const string SYD = "SYD";
    /// <summary>
    /// The standardized <c>T</c> function name.
    /// </summary>
    public const string T = "T";
    /// <summary>
    /// The standardized <c>TAN</c> function name.
    /// </summary>
    public const string TAN = "TAN";
    /// <summary>
    /// The standardized <c>TANH</c> function name.
    /// </summary>
    public const string TANH = "TANH";
    /// <summary>
    /// The standardized <c>TBILLEQ</c> function name.
    /// </summary>
    public const string TBILLEQ = "TBILLEQ";
    /// <summary>
    /// The standardized <c>TBILLPRICE</c> function name.
    /// </summary>
    public const string TBILLPRICE = "TBILLPRICE";
    /// <summary>
    /// The standardized <c>TBILLYIELD</c> function name.
    /// </summary>
    public const string TBILLYIELD = "TBILLYIELD";
    /// <summary>
    /// The standardized <c>TEXT</c> function name.
    /// </summary>
    public const string TEXT = "TEXT";
    /// <summary>
    /// The standardized <c>TIME</c> function name.
    /// </summary>
    public const string TIME = "TIME";
    /// <summary>
    /// The standardized <c>TIMEVALUE</c> function name.
    /// </summary>
    public const string TIMEVALUE = "TIMEVALUE";
    /// <summary>
    /// The standardized <c>TINV</c> function name.
    /// </summary>
    public const string TINV = "TINV";
    /// <summary>
    /// The standardized <c>TODAY</c> function name.
    /// </summary>
    public const string TODAY = "TODAY";
    /// <summary>
    /// The standardized <c>TRANSPOSE</c> function name.
    /// </summary>
    public const string TRANSPOSE = "TRANSPOSE";
    /// <summary>
    /// The standardized <c>TREND</c> function name.
    /// </summary>
    public const string TREND = "TREND";
    /// <summary>
    /// The standardized <c>TRIM</c> function name.
    /// </summary>
    public const string TRIM = "TRIM";
    /// <summary>
    /// The standardized <c>TRIMMEAN</c> function name.
    /// </summary>
    public const string TRIMMEAN = "TRIMMEAN";
    /// <summary>
    /// The standardized <c>TRUE</c> function name.
    /// </summary>
    public const string TRUE = "TRUE";
    /// <summary>
    /// The standardized <c>TRUNC</c> function name.
    /// </summary>
    public const string TRUNC = "TRUNC";
    /// <summary>
    /// The standardized <c>TTEST</c> function name.
    /// </summary>
    public const string TTEST = "TTEST";
    /// <summary>
    /// The standardized <c>TYPE</c> function name.
    /// </summary>
    public const string TYPE = "TYPE";
    /// <summary>
    /// The standardized <c>UNICHAR</c> function name.
    /// </summary>
    public const string UNICHAR = "UNICHAR";
    /// <summary>
    /// The standardized <c>UNICODE</c> function name.
    /// </summary>
    public const string UNICODE = "UNICODE";
    /// <summary>
    /// The standardized <c>UPPER</c> function name.
    /// </summary>
    public const string UPPER = "UPPER";
    /// <summary>
    /// The standardized <c>VALUE</c> function name.
    /// </summary>
    public const string VALUE = "VALUE";
    /// <summary>
    /// The standardized <c>VAR</c> function name.
    /// </summary>
    public const string VAR = "VAR";
    /// <summary>
    /// The standardized <c>VARA</c> function name.
    /// </summary>
    public const string VARA = "VARA";
    /// <summary>
    /// The standardized <c>VARP</c> function name.
    /// </summary>
    public const string VARP = "VARP";
    /// <summary>
    /// The standardized <c>VARPA</c> function name.
    /// </summary>
    public const string VARPA = "VARPA";
    /// <summary>
    /// The standardized <c>VDB</c> function name.
    /// </summary>
    public const string VDB = "VDB";
    /// <summary>
    /// The standardized <c>VLOOKUP</c> function name.
    /// </summary>
    public const string VLOOKUP = "VLOOKUP";
    /// <summary>
    /// The standardized <c>WEEKDAY</c> function name.
    /// </summary>
    public const string WEEKDAY = "WEEKDAY";
    /// <summary>
    /// The standardized <c>WEEKNUM</c> function name.
    /// </summary>
    public const string WEEKNUM = "WEEKNUM";
    /// <summary>
    /// The standardized <c>WEIBULL</c> function name.
    /// </summary>
    public const string WEIBULL = "WEIBULL";
    /// <summary>
    /// The standardized <c>WORKDAY</c> function name.
    /// </summary>
    public const string WORKDAY = "WORKDAY";
    /// <summary>
    /// The standardized <c>XIRR</c> function name.
    /// </summary>
    public const string XIRR = "XIRR";
    /// <summary>
    /// The standardized <c>XNPV</c> function name.
    /// </summary>
    public const string XNPV = "XNPV";
    /// <summary>
    /// The standardized <c>XOR</c> function name.
    /// </summary>
    public const string XOR = "XOR";
    /// <summary>
    /// The standardized <c>YEAR</c> function name.
    /// </summary>
    public const string YEAR = "YEAR";
    /// <summary>
    /// The standardized <c>YEARFRAC</c> function name.
    /// </summary>
    public const string YEARFRAC = "YEARFRAC";
    /// <summary>
    /// The standardized <c>YIELD</c> function name.
    /// </summary>
    public const string YIELD = "YIELD";
    /// <summary>
    /// The standardized <c>YIELDDISC</c> function name.
    /// </summary>
    public const string YIELDDISC = "YIELDDISC";
    /// <summary>
    /// The standardized <c>YIELDMAT</c> function name.
    /// </summary>
    public const string YIELDMAT = "YIELDMAT";
    /// <summary>
    /// The standardized <c>ZTEST</c> function name.
    /// </summary>
    public const string ZTEST = "ZTEST";
}
