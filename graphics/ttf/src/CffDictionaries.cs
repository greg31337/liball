namespace ttf;

internal class CffDictionaryReader
{
    CffDictionary dictionary;
    CffStringTable? strings;

    internal CffDictionaryReader(
        CffDictionary dictionary,
        CffStringTable? strings)
    {
        if (dictionary == null)
        {
            throw new System.ArgumentNullException("dictionary");
        }

        this.dictionary = dictionary;
        this.strings = strings;
    }

    internal CffDictionary GetDictionary()
    {
        return dictionary;
    }

    internal void ValidateKnownOperators(int[] operators)
    {
        int index = 0;

        while (index < operators.Length)
        {
            dictionary.FindEntry(operators[index]);
            index += 1;
        }
    }

    internal bool Has(int operatorCode)
    {
        return dictionary.FindEntry(operatorCode) != null;
    }

    internal CffDictEntry? Find(int operatorCode)
    {
        return dictionary.FindEntry(operatorCode);
    }

    internal CffDictEntry Require(int operatorCode, string description)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            throw FontFormatException.Create(
                $"A CFF dictionary is missing its required {description} operator.");
        }

        return entry;
    }

    internal double ReadNumber(int operatorCode, double defaultValue)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return defaultValue;
        }

        ValidateOperandCount(entry, 1, 1);
        return entry.GetOperand(0).GetNumber();
    }

    internal int ReadInteger(int operatorCode, int defaultValue)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return defaultValue;
        }

        ValidateOperandCount(entry, 1, 1);
        return entry.GetOperand(0).GetInteger();
    }

    internal int ReadRequiredInteger(
        int operatorCode,
        string description)
    {
        CffDictEntry entry = Require(operatorCode, description);
        ValidateOperandCount(entry, 1, 1);
        return entry.GetOperand(0).GetInteger();
    }

    internal bool ReadBoolean(int operatorCode, bool defaultValue)
    {
        int defaultInteger = defaultValue ? 1 : 0;
        int value = ReadInteger(operatorCode, defaultInteger);

        if (value != 0 && value != 1)
        {
            throw FontFormatException.Create(
                "A CFF DICT Boolean operand must be zero or one.");
        }

        return value != 0;
    }

    internal string? ReadOptionalString(int operatorCode)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return null;
        }

        return ReadSidEntry(entry);
    }

    internal string ReadRequiredString(
        int operatorCode,
        string description)
    {
        return ReadSidEntry(Require(operatorCode, description));
    }

    internal int ReadSid(int operatorCode, int defaultValue)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return defaultValue;
        }

        ValidateOperandCount(entry, 1, 1);
        int sid = entry.GetOperand(0).GetInteger();
        ValidateSid(sid);
        return sid;
    }

    internal string ReadSidOperand(CffDictEntry entry, int index)
    {
        if (index < 0 || index >= entry.GetOperandCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        int sid = entry.GetOperand(index).GetInteger();
        ValidateSid(sid);
        System.ArgumentNullException.ThrowIfNull(strings);
        return strings.GetString(sid);
    }

    internal double[] CopyNumbers(
        int operatorCode,
        double[] defaultValues,
        int minimumCount,
        int maximumCount)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return defaultValues.AsSpan().ToArray();
        }

        ValidateOperandCount(entry, minimumCount, maximumCount);
        return entry.CopyNumbers();
    }

    internal double[] CopyDeltaValues(
        int operatorCode,
        int minimumCount,
        int maximumCount)
    {
        CffDictEntry? entry = dictionary.FindEntry(operatorCode);

        if (entry == null)
        {
            return new double[0];
        }

        ValidateOperandCount(entry, minimumCount, maximumCount);
        return entry.CopyDeltaValues();
    }

    internal void ValidateOperandCount(
        CffDictEntry entry,
        int minimumCount,
        int maximumCount)
    {
        int count = entry.GetOperandCount();

        if (count < minimumCount || count > maximumCount)
        {
            throw FontFormatException.Create(
                "A CFF DICT operator has an invalid operand count.");
        }
    }

    internal void ValidateNonnegativeInteger(
        int operatorCode,
        int defaultValue)
    {
        if (ReadInteger(operatorCode, defaultValue) < 0)
        {
            throw FontFormatException.Create(
                "A CFF DICT offset or count must not be negative.");
        }
    }

    string ReadSidEntry(CffDictEntry entry)
    {
        ValidateOperandCount(entry, 1, 1);
        int sid = entry.GetOperand(0).GetInteger();
        ValidateSid(sid);
        System.ArgumentNullException.ThrowIfNull(strings);
        return strings.GetString(sid);
    }

    void ValidateSid(int sid)
    {
        if (strings == null)
        {
            throw FontFormatException.Create(
                "A CFF SID cannot be resolved without a string table.");
        }

        strings.GetString(sid);
    }
}

internal class CffTopDictionary
{
    static readonly int[] knownOperators = new int[]
    {
        CffDictOperator.Version,
        CffDictOperator.Notice,
        CffDictOperator.Copyright,
        CffDictOperator.FullName,
        CffDictOperator.FamilyName,
        CffDictOperator.Weight,
        CffDictOperator.IsFixedPitch,
        CffDictOperator.ItalicAngle,
        CffDictOperator.UnderlinePosition,
        CffDictOperator.UnderlineThickness,
        CffDictOperator.PaintType,
        CffDictOperator.CharstringType,
        CffDictOperator.FontMatrix,
        CffDictOperator.UniqueIdentifier,
        CffDictOperator.FontBoundingBox,
        CffDictOperator.StrokeWidth,
        CffDictOperator.ExtendedUniqueIdentifier,
        CffDictOperator.Charset,
        CffDictOperator.Encoding,
        CffDictOperator.CharStrings,
        CffDictOperator.Private,
        CffDictOperator.SyntheticBase,
        CffDictOperator.PostScript,
        CffDictOperator.BaseFontName,
        CffDictOperator.BaseFontBlend,
        CffDictOperator.RegistryOrderingSupplement,
        CffDictOperator.CidFontVersion,
        CffDictOperator.CidFontRevision,
        CffDictOperator.CidFontType,
        CffDictOperator.CidCount,
        CffDictOperator.UniqueIdentifierBase,
        CffDictOperator.FontDictionaryArray,
        CffDictOperator.FontDictionarySelect
    };
    CffDictionaryReader reader;

    internal CffTopDictionary(
        CffDictionary dictionary,
        CffStringTable strings)
    {
        if (strings == null)
        {
            throw new System.ArgumentNullException("strings");
        }

        reader = new CffDictionaryReader(dictionary, strings);
        Validate();
    }

    internal static CffTopDictionary Parse(
        FontDataSlice data,
        CffStringTable strings)
    {
        return new CffTopDictionary(CffDictParser.Parse(data), strings);
    }

    internal CffDictionary GetRawDictionary()
    {
        return reader.GetDictionary();
    }

    internal string? GetVersion()
    {
        return reader.ReadOptionalString(CffDictOperator.Version);
    }

    internal string? GetNotice()
    {
        return reader.ReadOptionalString(CffDictOperator.Notice);
    }

    internal string? GetCopyright()
    {
        return reader.ReadOptionalString(CffDictOperator.Copyright);
    }

    internal string? GetFullName()
    {
        return reader.ReadOptionalString(CffDictOperator.FullName);
    }

    internal string? GetFamilyName()
    {
        return reader.ReadOptionalString(CffDictOperator.FamilyName);
    }

    internal string? GetWeight()
    {
        return reader.ReadOptionalString(CffDictOperator.Weight);
    }

    internal bool IsFixedPitch()
    {
        return reader.ReadBoolean(CffDictOperator.IsFixedPitch, false);
    }

    internal double GetItalicAngle()
    {
        return reader.ReadNumber(CffDictOperator.ItalicAngle, 0);
    }

    internal double GetUnderlinePosition()
    {
        return reader.ReadNumber(CffDictOperator.UnderlinePosition, -100);
    }

    internal double GetUnderlineThickness()
    {
        return reader.ReadNumber(CffDictOperator.UnderlineThickness, 50);
    }

    internal int GetPaintType()
    {
        return reader.ReadInteger(CffDictOperator.PaintType, 0);
    }

    internal int GetCharstringType()
    {
        return reader.ReadInteger(CffDictOperator.CharstringType, 2);
    }

    internal double[] CopyFontMatrix()
    {
        return reader.CopyNumbers(
            CffDictOperator.FontMatrix,
            new double[] { 0.001, 0, 0, 0.001, 0, 0 },
            6,
            6);
    }

    internal double[] CopyFontBoundingBox()
    {
        return reader.CopyNumbers(
            CffDictOperator.FontBoundingBox,
            new double[] { 0, 0, 0, 0 },
            4,
            4);
    }

    internal bool HasUniqueIdentifier()
    {
        return reader.Has(CffDictOperator.UniqueIdentifier);
    }

    internal int GetUniqueIdentifier()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.UniqueIdentifier,
            "UniqueID");
    }

    internal double GetStrokeWidth()
    {
        return reader.ReadNumber(CffDictOperator.StrokeWidth, 0);
    }

    internal double[] CopyExtendedUniqueIdentifier()
    {
        return reader.CopyNumbers(
            CffDictOperator.ExtendedUniqueIdentifier,
            new double[0],
            1,
            48);
    }

    internal int GetCharsetOffset()
    {
        return reader.ReadInteger(CffDictOperator.Charset, 0);
    }

    internal bool HasCharset()
    {
        return reader.Has(CffDictOperator.Charset);
    }

    internal int GetEncodingOffset()
    {
        return reader.ReadInteger(CffDictOperator.Encoding, 0);
    }

    internal bool HasEncoding()
    {
        return reader.Has(CffDictOperator.Encoding);
    }

    internal int GetCharStringsOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.CharStrings,
            "CharStrings");
    }

    internal bool HasPrivateDictionary()
    {
        return reader.Has(CffDictOperator.Private);
    }

    internal int GetPrivateDictionarySize()
    {
        return GetPrivateOperand(0);
    }

    internal int GetPrivateDictionaryOffset()
    {
        return GetPrivateOperand(1);
    }

    internal bool HasSyntheticBase()
    {
        return reader.Has(CffDictOperator.SyntheticBase);
    }

    internal int GetSyntheticBase()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.SyntheticBase,
            "SyntheticBase");
    }

    internal string? GetPostScript()
    {
        return reader.ReadOptionalString(CffDictOperator.PostScript);
    }

    internal string? GetBaseFontName()
    {
        return reader.ReadOptionalString(CffDictOperator.BaseFontName);
    }

    internal double[] CopyBaseFontBlend()
    {
        return reader.CopyDeltaValues(
            CffDictOperator.BaseFontBlend,
            1,
            48);
    }

    internal bool IsCidKeyed()
    {
        return reader.Has(CffDictOperator.RegistryOrderingSupplement);
    }

    internal string GetRegistry()
    {
        return GetRosString(0);
    }

    internal string GetOrdering()
    {
        return GetRosString(1);
    }

    internal int GetSupplement()
    {
        CffDictEntry entry = reader.Require(
            CffDictOperator.RegistryOrderingSupplement,
            "ROS");
        return entry.GetOperand(2).GetInteger();
    }

    internal double GetCidFontVersion()
    {
        return reader.ReadNumber(CffDictOperator.CidFontVersion, 0);
    }

    internal double GetCidFontRevision()
    {
        return reader.ReadNumber(CffDictOperator.CidFontRevision, 0);
    }

    internal int GetCidFontType()
    {
        return reader.ReadInteger(CffDictOperator.CidFontType, 0);
    }

    internal int GetCidCount()
    {
        return reader.ReadInteger(CffDictOperator.CidCount, 8720);
    }

    internal bool HasUniqueIdentifierBase()
    {
        return reader.Has(CffDictOperator.UniqueIdentifierBase);
    }

    internal int GetUniqueIdentifierBase()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.UniqueIdentifierBase,
            "UIDBase");
    }

    internal int GetFontDictionaryArrayOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.FontDictionaryArray,
            "FDArray");
    }

    internal int GetFontDictionarySelectOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.FontDictionarySelect,
            "FDSelect");
    }

    void Validate()
    {
        reader.ValidateKnownOperators(knownOperators);
        GetVersion();
        GetNotice();
        GetCopyright();
        GetFullName();
        GetFamilyName();
        GetWeight();
        IsFixedPitch();
        GetItalicAngle();
        GetUnderlinePosition();
        GetUnderlineThickness();
        GetPaintType();

        if (GetCharstringType() != 2)
        {
            throw FontFormatException.Create(
                "An OpenType CFF font must use CharstringType 2.");
        }

        CopyFontMatrix();
        CopyFontBoundingBox();
        GetStrokeWidth();
        CopyExtendedUniqueIdentifier();
        reader.ValidateNonnegativeInteger(CffDictOperator.Charset, 0);
        reader.ValidateNonnegativeInteger(CffDictOperator.Encoding, 0);
        reader.ValidateNonnegativeInteger(CffDictOperator.CharStrings, -1);

        if (HasUniqueIdentifier())
        {
            if (GetUniqueIdentifier() < 0)
            {
                throw FontFormatException.Create(
                    "A CFF UniqueID must not be negative.");
            }
        }

        if (HasSyntheticBase())
        {
            if (GetSyntheticBase() < 0)
            {
                throw FontFormatException.Create(
                    "A CFF SyntheticBase must not be negative.");
            }
        }

        GetPostScript();
        GetBaseFontName();
        CopyBaseFontBlend();

        if (IsCidKeyed())
        {
            ValidateCidDictionary();
        }
        else
        {
            if (HasCidOnlyOperator())
            {
                throw FontFormatException.Create(
                    "A non-CID CFF Top DICT contains a CID-only operator.");
            }

            ValidatePrivateDictionaryReference(true);
        }
    }

    bool HasCidOnlyOperator()
    {
        return reader.Has(CffDictOperator.CidFontVersion) ||
            reader.Has(CffDictOperator.CidFontRevision) ||
            reader.Has(CffDictOperator.CidFontType) ||
            reader.Has(CffDictOperator.CidCount) ||
            reader.Has(CffDictOperator.UniqueIdentifierBase) ||
            reader.Has(CffDictOperator.FontDictionaryArray) ||
            reader.Has(CffDictOperator.FontDictionarySelect);
    }

    void ValidateCidDictionary()
    {
        CffDictEntry ros = reader.Require(
            CffDictOperator.RegistryOrderingSupplement,
            "ROS");
        reader.ValidateOperandCount(ros, 3, 3);
        GetRegistry();
        GetOrdering();

        if (GetSupplement() < 0 || GetCidCount() < 0)
        {
            throw FontFormatException.Create(
                "CFF CID metadata must not contain a negative count.");
        }

        GetCidFontVersion();
        GetCidFontRevision();
        GetCidFontType();

        if (HasUniqueIdentifierBase())
        {
            if (GetUniqueIdentifierBase() < 0)
            {
                throw FontFormatException.Create(
                    "A CFF UIDBase must not be negative.");
            }
        }

        reader.ValidateNonnegativeInteger(
            CffDictOperator.FontDictionaryArray,
            -1);
        reader.ValidateNonnegativeInteger(
            CffDictOperator.FontDictionarySelect,
            -1);
        ValidatePrivateDictionaryReference(false);
    }

    void ValidatePrivateDictionaryReference(bool required)
    {
        CffDictEntry? entry = reader.Find(CffDictOperator.Private);

        if (entry == null)
        {
            if (required)
            {
                reader.Require(CffDictOperator.Private, "Private");
            }

            return;
        }

        reader.ValidateOperandCount(entry, 2, 2);

        if (entry.GetOperand(0).GetInteger() < 0 ||
            entry.GetOperand(1).GetInteger() < 0)
        {
            throw FontFormatException.Create(
                "A CFF Private DICT size and offset must not be negative.");
        }
    }

    int GetPrivateOperand(int index)
    {
        CffDictEntry entry = reader.Require(
            CffDictOperator.Private,
            "Private");
        reader.ValidateOperandCount(entry, 2, 2);
        return entry.GetOperand(index).GetInteger();
    }

    string GetRosString(int index)
    {
        CffDictEntry entry = reader.Require(
            CffDictOperator.RegistryOrderingSupplement,
            "ROS");
        reader.ValidateOperandCount(entry, 3, 3);
        return reader.ReadSidOperand(entry, index);
    }
}

internal class CffPrivateDictionary
{
    static readonly int[] knownOperators = new int[]
    {
        CffDictOperator.BlueValues,
        CffDictOperator.OtherBlues,
        CffDictOperator.FamilyBlues,
        CffDictOperator.FamilyOtherBlues,
        CffDictOperator.BlueScale,
        CffDictOperator.BlueShift,
        CffDictOperator.BlueFuzz,
        CffDictOperator.StandardHorizontalWidth,
        CffDictOperator.StandardVerticalWidth,
        CffDictOperator.StemSnapHorizontal,
        CffDictOperator.StemSnapVertical,
        CffDictOperator.ForceBold,
        CffDictOperator.ForceBoldThreshold,
        CffDictOperator.LenIv,
        CffDictOperator.LanguageGroup,
        CffDictOperator.ExpansionFactor,
        CffDictOperator.InitialRandomSeed,
        CffDictOperator.LocalSubroutines,
        CffDictOperator.DefaultWidthX,
        CffDictOperator.NominalWidthX
    };
    CffDictionaryReader reader;

    internal CffPrivateDictionary(CffDictionary dictionary)
    {
        reader = new CffDictionaryReader(dictionary, null);
        Validate();
    }

    internal static CffPrivateDictionary Parse(FontDataSlice data)
    {
        return new CffPrivateDictionary(CffDictParser.Parse(data));
    }

    internal CffDictionary GetRawDictionary()
    {
        return reader.GetDictionary();
    }

    internal double[] CopyBlueValues()
    {
        return reader.CopyDeltaValues(CffDictOperator.BlueValues, 2, 14);
    }

    internal double[] CopyOtherBlues()
    {
        return reader.CopyDeltaValues(CffDictOperator.OtherBlues, 2, 10);
    }

    internal double[] CopyFamilyBlues()
    {
        return reader.CopyDeltaValues(CffDictOperator.FamilyBlues, 2, 14);
    }

    internal double[] CopyFamilyOtherBlues()
    {
        return reader.CopyDeltaValues(CffDictOperator.FamilyOtherBlues, 2, 10);
    }

    internal double GetBlueScale()
    {
        return reader.ReadNumber(CffDictOperator.BlueScale, 0.039625);
    }

    internal double GetBlueShift()
    {
        return reader.ReadNumber(CffDictOperator.BlueShift, 7);
    }

    internal double GetBlueFuzz()
    {
        return reader.ReadNumber(CffDictOperator.BlueFuzz, 1);
    }

    internal bool HasStandardHorizontalWidth()
    {
        return reader.Has(CffDictOperator.StandardHorizontalWidth);
    }

    internal double GetStandardHorizontalWidth()
    {
        return reader.ReadNumber(
            CffDictOperator.StandardHorizontalWidth,
            0);
    }

    internal bool HasStandardVerticalWidth()
    {
        return reader.Has(CffDictOperator.StandardVerticalWidth);
    }

    internal double GetStandardVerticalWidth()
    {
        return reader.ReadNumber(
            CffDictOperator.StandardVerticalWidth,
            0);
    }

    internal double[] CopyStemSnapHorizontal()
    {
        return reader.CopyDeltaValues(
            CffDictOperator.StemSnapHorizontal,
            1,
            12);
    }

    internal double[] CopyStemSnapVertical()
    {
        return reader.CopyDeltaValues(
            CffDictOperator.StemSnapVertical,
            1,
            12);
    }

    internal bool IsForceBold()
    {
        return reader.ReadBoolean(CffDictOperator.ForceBold, false);
    }

    internal bool HasForceBoldThreshold()
    {
        return reader.Has(CffDictOperator.ForceBoldThreshold);
    }

    internal double GetForceBoldThreshold()
    {
        return reader.ReadNumber(CffDictOperator.ForceBoldThreshold, 0);
    }

    internal bool HasLenIv()
    {
        return reader.Has(CffDictOperator.LenIv);
    }

    internal int GetLenIv()
    {
        return reader.ReadInteger(CffDictOperator.LenIv, 0);
    }

    internal int GetLanguageGroup()
    {
        return reader.ReadInteger(CffDictOperator.LanguageGroup, 0);
    }

    internal double GetExpansionFactor()
    {
        return reader.ReadNumber(CffDictOperator.ExpansionFactor, 0.06);
    }

    internal int GetInitialRandomSeed()
    {
        return reader.ReadInteger(CffDictOperator.InitialRandomSeed, 0);
    }

    internal bool HasLocalSubroutines()
    {
        return reader.Has(CffDictOperator.LocalSubroutines);
    }

    internal int GetLocalSubroutinesOffset()
    {
        return reader.ReadInteger(CffDictOperator.LocalSubroutines, 0);
    }

    internal double GetDefaultWidthX()
    {
        return reader.ReadNumber(CffDictOperator.DefaultWidthX, 0);
    }

    internal double GetNominalWidthX()
    {
        return reader.ReadNumber(CffDictOperator.NominalWidthX, 0);
    }

    void Validate()
    {
        reader.ValidateKnownOperators(knownOperators);
        ValidateEvenDelta(CffDictOperator.BlueValues, 14);
        ValidateEvenDelta(CffDictOperator.OtherBlues, 10);
        ValidateEvenDelta(CffDictOperator.FamilyBlues, 14);
        ValidateEvenDelta(CffDictOperator.FamilyOtherBlues, 10);
        GetBlueScale();
        GetBlueShift();
        GetBlueFuzz();

        if (HasStandardHorizontalWidth())
        {
            GetStandardHorizontalWidth();
        }

        if (HasStandardVerticalWidth())
        {
            GetStandardVerticalWidth();
        }

        CopyStemSnapHorizontal();
        CopyStemSnapVertical();
        IsForceBold();

        if (HasForceBoldThreshold())
        {
            GetForceBoldThreshold();
        }

        if (HasLenIv())
        {
            GetLenIv();
        }

        GetLanguageGroup();
        GetExpansionFactor();
        GetInitialRandomSeed();

        if (HasLocalSubroutines() && GetLocalSubroutinesOffset() < 0)
        {
            throw FontFormatException.Create(
                "A CFF local Subrs offset must not be negative.");
        }

        GetDefaultWidthX();
        GetNominalWidthX();
    }

    void ValidateEvenDelta(int operatorCode, int maximumCount)
    {
        CffDictEntry? entry = reader.Find(operatorCode);

        if (entry == null)
        {
            return;
        }

        reader.ValidateOperandCount(entry, 2, maximumCount);

        if (entry.GetOperandCount() % 2 != 0)
        {
            throw FontFormatException.Create(
                "A CFF blue-zone delta array must contain coordinate pairs.");
        }
    }
}

internal class CffFontDictionary
{
    static readonly int[] knownOperators = new int[]
    {
        CffDictOperator.FontName,
        CffDictOperator.FontMatrix,
        CffDictOperator.Private
    };
    CffDictionaryReader reader;

    internal CffFontDictionary(
        CffDictionary dictionary,
        CffStringTable strings)
    {
        if (strings == null)
        {
            throw new System.ArgumentNullException("strings");
        }

        reader = new CffDictionaryReader(dictionary, strings);
        Validate();
    }

    internal static CffFontDictionary Parse(
        FontDataSlice data,
        CffStringTable strings)
    {
        return new CffFontDictionary(CffDictParser.Parse(data), strings);
    }

    internal CffDictionary GetRawDictionary()
    {
        return reader.GetDictionary();
    }

    internal string? GetFontName()
    {
        return reader.ReadOptionalString(CffDictOperator.FontName);
    }

    internal bool HasFontMatrix()
    {
        return reader.Has(CffDictOperator.FontMatrix);
    }

    internal double[] CopyFontMatrix()
    {
        return reader.CopyNumbers(
            CffDictOperator.FontMatrix,
            new double[] { 0.001, 0, 0, 0.001, 0, 0 },
            6,
            6);
    }

    internal int GetPrivateDictionarySize()
    {
        return GetPrivateOperand(0);
    }

    internal int GetPrivateDictionaryOffset()
    {
        return GetPrivateOperand(1);
    }

    void Validate()
    {
        reader.ValidateKnownOperators(knownOperators);
        GetFontName();
        CopyFontMatrix();
        int size = GetPrivateDictionarySize();
        int offset = GetPrivateDictionaryOffset();

        if (size < 0 || offset < 0)
        {
            throw FontFormatException.Create(
                "A CFF Font DICT Private size and offset must not be negative.");
        }
    }

    int GetPrivateOperand(int index)
    {
        CffDictEntry entry = reader.Require(
            CffDictOperator.Private,
            "Private");
        reader.ValidateOperandCount(entry, 2, 2);
        return entry.GetOperand(index).GetInteger();
    }
}
