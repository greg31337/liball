namespace qrcode;

internal class EciRegistry
{
    internal const int MinimumAssignment = 0;
    internal const int OneByteMaximumAssignment = 127;
    internal const int TwoByteMaximumAssignment = 16383;
    internal const int MaximumQrAssignment = 999999;
    internal const int MaximumProtocolAssignment = 811799;
    internal const int CharacterSetKind = 0;
    internal const int BinaryKind = 1;
    internal const int TransformationKind = 2;
    internal const int ApplicationKind = 3;
    internal const int ProtocolControlKind = 4;
    internal const int BinaryAssignment = 899;
    internal const int GzipAssignment = 1800;
    internal const int TransformationPrefixAssignment = 799996;
    internal const int InvalidTransformationAssignment1 = 799997;
    internal const int InvalidTransformationAssignment2 = 799998;
    internal const int NullTransformationAssignment = 799999;
    internal const int GenericApplicationLauncherAssignment = 800000;
    internal const int InterpretativePrefixAssignment = 810899;

    static readonly int[] Assignments = new int[]
    {
        0, 1, 2, 3,
        4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
        15, 16, 17, 18,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
        170, 899, 1800, 799996, 799999, 800000, 810899
    };

    static readonly string[] CanonicalNames = new string[]
    {
        "CP437 with GLI 0 rules",
        "ISO-8859-1 with GLI 1 rules",
        "CP437",
        "ISO-8859-1",
        "ISO-8859-2",
        "ISO-8859-3",
        "ISO-8859-4",
        "ISO-8859-5",
        "ISO-8859-6",
        "ISO-8859-7",
        "ISO-8859-8",
        "ISO-8859-9",
        "ISO-8859-10",
        "ISO-8859-11",
        "ISO-8859-13",
        "ISO-8859-14",
        "ISO-8859-15",
        "ISO-8859-16",
        "Shift_JIS",
        "windows-1250",
        "windows-1251",
        "windows-1252",
        "windows-1256",
        "UTF-16BE",
        "UTF-8",
        "US-ASCII",
        "Big5",
        "GB2312",
        "KS X 1001",
        "GBK",
        "GB18030",
        "UTF-16LE",
        "UTF-32BE",
        "UTF-32LE",
        "ISO/IEC 646 invariant",
        "8-bit binary data",
        "GZIP compressed data",
        "Transformation prefix",
        "Null transformation",
        "Generic application launcher",
        "Interpretative prefix"
    };

    static readonly string[] AliasGroups = new string[]
    {
        "GLI0|CP437-GLI0",
        "GLI1|ISO-8859-1-GLI1",
        "IBM437|IBM-437|437|csPC8CodePage437",
        "ISO8859-1|ISO_8859-1|latin1|latin-1|L1|IBM819|CP819|csISOLatin1",
        "ISO8859-2|ISO_8859-2|latin2|latin-2|L2|csISOLatin2",
        "ISO8859-3|ISO_8859-3|latin3|latin-3|L3|csISOLatin3",
        "ISO8859-4|ISO_8859-4|latin4|latin-4|L4|csISOLatin4",
        "ISO8859-5|ISO_8859-5|csISOLatinCyrillic",
        "ISO8859-6|ISO_8859-6|csISOLatinArabic",
        "ISO8859-7|ISO_8859-7|csISOLatinGreek",
        "ISO8859-8|ISO_8859-8|csISOLatinHebrew",
        "ISO8859-9|ISO_8859-9|latin5|latin-5|L5|csISOLatin5",
        "ISO8859-10|ISO_8859-10|latin6|latin-6|L6|csISOLatin6",
        "ISO8859-11|ISO_8859-11|TIS-620",
        "ISO8859-13|ISO_8859-13|latin7|latin-7|L7",
        "ISO8859-14|ISO_8859-14|latin8|latin-8|L8",
        "ISO8859-15|ISO_8859-15|latin9|latin-9|L9",
        "ISO8859-16|ISO_8859-16|latin10|latin-10|L10",
        "Shift-JIS|SJIS|MS_Kanji|csShiftJIS",
        "CP1250|windows1250",
        "CP1251|windows1251",
        "CP1252|windows1252",
        "CP1256|windows1256",
        "UTF16BE|UnicodeBig|UnicodeBigUnmarked",
        "UTF8",
        "ASCII|ISO646-US|ISO_646.irv:1991|ANSI_X3.4-1968|csASCII",
        "BIG-5|BIGFIVE|csBig5",
        "GB_2312|EUC-CN|csGB2312",
        "EUC-KR|KS_C_5601-1987|KS-X-1001|csEUCKR",
        "CP936|MS936|windows-936",
        "GB-18030",
        "UTF16LE|UnicodeLittleUnmarked",
        "UTF32BE",
        "UTF32LE",
        "ISO646-INV|ISO-IR-170|INV",
        "BINARY|BINARY-DATA|8BIT-BINARY",
        "GZIP|GZIP-COMPRESSION",
        "TRANSFORMATION-PREFIX",
        "NULL-TRANSFORMATION",
        "APPLICATION-LAUNCHER|GENERIC-APPLICATION-LAUNCHER",
        "INTERPRETATIVE-PREFIX|INTERPRETIVE-PREFIX"
    };

    internal static int GetRegisteredAssignmentCount()
    {
        return Assignments.Length;
    }

    internal static int GetRegisteredAssignment(int index)
    {
        if (index < 0 || index >= Assignments.Length)
        {
            throw new global::System.ArgumentOutOfRangeException("index");
        }

        return Assignments[index];
    }

    internal static bool IsRegistered(int assignment)
    {
        return FindRegisteredIndex(assignment) >= 0;
    }

    internal static string GetCanonicalName(int assignment)
    {
        int index = FindRegisteredIndex(assignment);
        if (index < 0)
        {
            throw new global::System.ArgumentException("The ECI assignment is not registered.", "assignment");
        }

        return CanonicalNames[index];
    }

    internal static int FindAssignment(string? name)
    {
        if (name == null)
        {
            throw new global::System.ArgumentNullException("name");
        }

        for (int index = 0; index < Assignments.Length; index++)
        {
            if (global::System.String.Equals(name, CanonicalNames[index], global::System.StringComparison.OrdinalIgnoreCase))
            {
                return Assignments[index];
            }

            string[] aliases = AliasGroups[index].Split('|');
            for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
            {
                if (global::System.String.Equals(name, aliases[aliasIndex], global::System.StringComparison.OrdinalIgnoreCase))
                {
                    return Assignments[index];
                }
            }
        }

        throw new global::System.ArgumentException("The ECI name or alias is not registered.", "name");
    }

    internal static int GetEntryKind(int assignment)
    {
        int index = FindRegisteredIndex(assignment);
        if (index < 0)
        {
            throw new global::System.ArgumentException("The ECI assignment is not registered.", "assignment");
        }

        if (assignment == BinaryAssignment)
        {
            return BinaryKind;
        }

        if (assignment == GzipAssignment)
        {
            return TransformationKind;
        }

        if (assignment == GenericApplicationLauncherAssignment)
        {
            return ApplicationKind;
        }

        if (assignment == TransformationPrefixAssignment
            || assignment == NullTransformationAssignment
            || assignment == InterpretativePrefixAssignment)
        {
            return ProtocolControlKind;
        }

        return CharacterSetKind;
    }

    internal static bool IsProtocolOnly(int assignment)
    {
        return GetEntryKind(assignment) != CharacterSetKind;
    }

    internal static bool HasDotNetEncoding(int assignment)
    {
        if (!IsRegistered(assignment))
        {
            return false;
        }

        return assignment == 1
            || assignment == 3
            || assignment == 25
            || assignment == 26
            || assignment == 27
            || assignment == 33
            || assignment == 34
            || assignment == 35;
    }

    internal static global::System.Text.Encoding GetDotNetEncoding(int assignment)
    {
        if (!HasDotNetEncoding(assignment))
        {
            throw new global::System.NotSupportedException("The ECI assignment has no built-in .NET conversion path.");
        }

        if (assignment == 1 || assignment == 3)
        {
            return global::System.Text.Encoding.GetEncoding(
                28591,
                global::System.Text.EncoderFallback.ExceptionFallback,
                global::System.Text.DecoderFallback.ExceptionFallback);
        }

        if (assignment == 25)
        {
            return new global::System.Text.UnicodeEncoding(true, false, true);
        }

        if (assignment == 26)
        {
            return new global::System.Text.UTF8Encoding(false, true);
        }

        if (assignment == 27)
        {
            return global::System.Text.Encoding.GetEncoding(
                20127,
                global::System.Text.EncoderFallback.ExceptionFallback,
                global::System.Text.DecoderFallback.ExceptionFallback);
        }

        if (assignment == 33)
        {
            return new global::System.Text.UnicodeEncoding(false, false, true);
        }

        if (assignment == 34)
        {
            return new global::System.Text.UTF32Encoding(true, false, true);
        }

        return new global::System.Text.UTF32Encoding(false, false, true);
    }

    internal static int GetDesignatorByteCount(int assignment)
    {
        ValidateQrAssignment(assignment);
        if (assignment <= OneByteMaximumAssignment)
        {
            return 1;
        }

        if (assignment <= TwoByteMaximumAssignment)
        {
            return 2;
        }

        return 3;
    }

    internal static bool IsPrivateUseAssignment(int assignment)
    {
        ValidateQrAssignment(assignment);
        return assignment >= 400000 && assignment <= 799995
            || assignment >= 810900 && assignment <= MaximumProtocolAssignment;
    }

    internal static bool IsInvalidProtocolAssignment(int assignment)
    {
        ValidateQrAssignment(assignment);
        return assignment == InvalidTransformationAssignment1
            || assignment == InvalidTransformationAssignment2
            || assignment > MaximumProtocolAssignment;
    }

    internal static void ValidateQrAssignment(int assignment)
    {
        if (assignment < MinimumAssignment || assignment > MaximumQrAssignment)
        {
            throw new global::System.ArgumentOutOfRangeException("assignment");
        }
    }

    static int FindRegisteredIndex(int assignment)
    {
        for (int index = 0; index < Assignments.Length; index++)
        {
            if (Assignments[index] == assignment)
            {
                return index;
            }
        }

        return -1;
    }
}
