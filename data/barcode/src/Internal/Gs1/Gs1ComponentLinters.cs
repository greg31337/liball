namespace barcode;

internal class Gs1LinterResult
{
    internal bool Success;
    internal string Code = "";
    internal int ErrorOffset;
    internal int ErrorLength;
}

internal static class Gs1ComponentLinters
{
    internal static readonly string[] Names = new string[]
    {
        "couponcode", "couponposoffer", "csum", "csumalpha",
        "gcppos1", "gcppos2", "hasnondigit", "hh", "hhmi",
        "hyphen", "iban", "importeridx", "iso3166", "iso3166999",
        "iso3166alpha2", "iso4217", "iso5218", "latitude",
        "longitude", "mediatype", "mi", "nonzero", "nozeroprefix",
        "packagetype", "pcenc", "pieceoftotal", "posinseqslash", "ss",
        "winding", "yesno", "yymmd0", "yymmdd", "yyyymmdd", "zero",
    };

    static readonly int[] AlphaChecksumPrimes = new int[]
    {
        2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37,
        41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89,
        97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151,
        157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223,
        227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
        283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359,
        367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433,
        439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503,
        509,
    };

    static readonly ulong[] Iso3166Bits = new ulong[]
    {
        0x08a888898888b888UL, 0x8aa80a2888888888UL,
        0x0888888a22232889UL, 0x88188a2221e322a2UL,
        0x2a2a180088888888UL, 0x888a888888888888UL,
        0x888288a2622a22a2UL, 0x222222228808b888UL,
        0x8888970808222222UL, 0x2de102888888a222UL,
        0x320a1b222222a203UL, 0xe20808c8088888a8UL,
        0x8888889a89002021UL, 0xe080222a00082102UL,
        0x0000000000000000UL, 0x0000000000000000UL,
    };

    static readonly ulong[] Iso3166Alpha2Bits = new ulong[]
    {
        0x1e9afb77f7bdbb7bUL, 0xe4fc21a8012b0070UL,
        0x003a900df9dfa800UL, 0xb160181ef00202c0UL,
        0x00b8d42f8281f2bfUL, 0x3fffeba4d2100080UL,
        0x023cf1ca80000002UL, 0x008a8fbfe75cddf9UL,
        0x59820820eaa10200UL, 0x4002000000000800UL,
        0x1020020080000000UL,
    };

    static readonly ulong[] Iso4217Bits = new ulong[]
    {
        0x008800008808b808UL, 0x8880082080880808UL,
        0x0880808800220008UL, 0x8010820202822000UL,
        0x0202000008000000UL, 0x8888088888888080UL,
        0x088a88a262222002UL, 0x022200008800a080UL,
        0x88080c0008220200UL, 0x2022028880000020UL,
        0x1202000000202002UL, 0xa200008000088888UL,
        0x0088880081002020UL, 0x2080002800002200UL,
        0x0400000fe6adbfdfUL, 0xfdfdfce225000000UL,
    };

    internal const string PackageTypes =
        " 1A 1B 1D 1F 1G 1W 200 201 202 203 204 205 206 210 211 212 2C"
        + " 3A 3H 43 44 4A 4B 4C 4D 4F 4G 4H 5H 5L 5M 6H 6P 7A 7B"
        + " 8 8A 8B 8C 9 AA AB AC AD AF AG AH AI AJ AL AM AP APE AT AV"
        + " B4 BB BC BD BE BF BG BGE BH BI BJ BK BL BM BME BN BO BP BQ BR BRI BS BT BU BV BW BX BY BZ"
        + " CA CB CBL CC CCE CD CE CF CG CH CI CJ CK CL CM CN CO CP CQ CR CS CT CU CV CW CX CY CZ"
        + " DA DB DC DG DH DI DJ DK DL DM DN DP DPE DR DS DT DU DV DW DX DY"
        + " E1 E2 E3 EC ED EE EF EG EH EI EN FB FC FD FE FI FL FO FOB FP FPE FR FT FW FX"
        + " GB GI GL GR GU GY GZ HA HB HC HG HN HR IA IB IC ID IE IF IG IH IK IL IN IZ"
        + " JB JC JG JR JT JY KG KI LAB LE LG LT LU LV LZ MA MB MC ME MPE MR MS MT MW MX"
        + " NA NE NF NG NS NT NU NV OA OB OC OD OE OF OK OPE OT OU"
        + " P2 PA PAE PB PC PD PE PF PG PH PI PJ PK PL PLP PN PO POP PP PPE PR PT PU PUE PV PX PY PZ"
        + " QA QB QC QD QF QG QH QJ QK QL QM QN QP QQ QR QS RB1 RB2 RB3 RCB RD RG RJ RK RL RO RT RZ"
        + " S1 SA SB SC SD SE SEC SH SI SK SL SM SO SP SS ST STL SU SV SW SX SY SZ"
        + " T1 TB TC TD TE TEV TG THE TI TK TL TN TO TR TRE TS TT TTE TU TV TW TWE TY TZ"
        + " UC UN UUE VA VG VI VK VL VN VO VP VQ VR VS VY"
        + " WA WB WC WD WF WG WH WJ WK WL WM WN WP WQ WR WRP WS WT WU WV WW WX WY WZ"
        + " X11 X12 X15 X16 X17 X18 X19 X20 X3 XA XB XC XD XF XG XH XJ XK"
        + " YA YB YC YD YF YG YH YJ YK YL YM YN YP YQ YR YS YT YV YW YX YY YZ"
        + " ZA ZB ZC ZD ZF ZG ZH ZJ ZK ZL ZM ZN ZP ZQ ZR ZS ZT ZU ZV ZW ZX ZY ZZ ";

    internal static Gs1LinterResult Lint(
        string? linterName,
        string? data)
    {
        if (linterName == null || data == null)
        {
            return Failure("GS1_LINTER_NULL_INPUT", 0, 0);
        }

        switch (linterName)
        {
            case "couponcode":
                return Gs1CouponLinters.LintCouponCode(data);
            case "couponposoffer":
                return Gs1CouponLinters.LintPositiveOffer(data);
            case "csum":
                return LintNumericCheckDigit(data);
            case "csumalpha":
                return LintAlphaCheckPair(data);
            case "gcppos1":
                return LintCompanyPrefix(data);
            case "gcppos2":
                return LintCompanyPrefixAfterIndicator(data);
            case "hasnondigit":
                return LintHasNonDigit(data);
            case "hh":
                return LintHour(data);
            case "hhmi":
                return LintHourMinute(data);
            case "hyphen":
                return LintHyphen(data);
            case "iban":
                return LintIban(data);
            case "importeridx":
                return LintImporterIndex(data);
            case "iso3166":
                return LintIso3166(data);
            case "iso3166999":
                return LintIso3166Or999(data);
            case "iso3166alpha2":
                return LintIso3166Alpha2(data);
            case "iso4217":
                return LintIso4217(data);
            case "iso5218":
                return LintIso5218(data);
            case "latitude":
                return LintLatitude(data);
            case "longitude":
                return LintLongitude(data);
            case "mediatype":
                return LintMediaType(data);
            case "mi":
                return LintMinute(data);
            case "nonzero":
                return LintNonZero(data);
            case "nozeroprefix":
                return LintNoZeroPrefix(data);
            case "packagetype":
                return LintPackageType(data);
            case "pcenc":
                return LintPercentEncoding(data);
            case "pieceoftotal":
                return LintPieceOfTotal(data);
            case "posinseqslash":
                return LintPositionInSequence(data);
            case "ss":
                return LintSecond(data);
            case "winding":
                return LintWinding(data);
            case "yesno":
                return LintYesNo(data);
            case "yymmd0":
                return LintShortDateAllowDayZero(data);
            case "yymmdd":
                return LintShortDate(data);
            case "yyyymmdd":
                return LintLongDate(data);
            case "zero":
                return LintZero(data);
            default:
                return Failure("GS1_LINTER_UNKNOWN", 0, linterName.Length);
        }
    }

    internal static bool SupportsLinter(string? linterName)
    {
        if (linterName == null)
        {
            return false;
        }

        int index = 0;
        while (index < Names.Length)
        {
            if (Names[index] == linterName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static Gs1LinterResult SuccessResult()
    {
        Gs1LinterResult result = new Gs1LinterResult();
        result.Success = true;
        result.Code = "GS1_LINTER_OK";
        return result;
    }

    internal static Gs1LinterResult FailureResult(
        string code,
        int offset,
        int length)
    {
        return Failure(code, offset, length);
    }

    internal static Gs1LinterResult LintCompanyPrefixForCoupon(
        string data)
    {
        return LintCompanyPrefix(data);
    }

    internal static Gs1LinterResult LintDateForCoupon(string data)
    {
        return LintShortDate(data);
    }

    static Gs1LinterResult LintNumericCheckDigit(string data)
    {
        if (data.Length == 0)
        {
            return Failure("GS1_LINTER_TOO_SHORT_FOR_CHECK_DIGIT", 0, 0);
        }

        int weight = data.Length % 2 == 0 ? 3 : 1;
        int sum = 0;
        int index = 0;
        while (index < data.Length - 1)
        {
            char character = data[index];
            if (!IsDigit(character))
            {
                return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", index, 1);
            }

            sum = sum + weight * (character - '0');
            weight = 4 - weight;
            index = index + 1;
        }

        char checkCharacter = data[data.Length - 1];
        if (!IsDigit(checkCharacter))
        {
            return Failure(
                "GS1_LINTER_NON_DIGIT_CHARACTER",
                data.Length - 1,
                1);
        }

        int checkDigit = (10 - sum % 10) % 10;
        if (checkCharacter - '0' != checkDigit)
        {
            return Failure(
                "GS1_LINTER_INCORRECT_CHECK_DIGIT",
                data.Length - 1,
                1);
        }

        return Success();
    }

    static Gs1LinterResult LintAlphaCheckPair(string data)
    {
        const string cset32 = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        if (data.Length > AlphaChecksumPrimes.Length + 2)
        {
            return Failure(
                "GS1_LINTER_TOO_LONG_FOR_CHECK_PAIR_IMPLEMENTATION",
                0,
                data.Length);
        }

        if (data.Length < 2)
        {
            return Failure(
                "GS1_LINTER_TOO_SHORT_FOR_CHECK_PAIR",
                0,
                data.Length);
        }

        if (data.Length == 2)
        {
            if (data != "22")
            {
                return Failure("GS1_LINTER_INCORRECT_CHECK_PAIR", 0, 2);
            }

            return Success();
        }

        int sum = 0;
        int index = 0;
        while (index < data.Length - 2)
        {
            int weight = Gs1ElementStringParser.Cset82Characters.IndexOf(
                data[index]);
            if (weight < 0)
            {
                return Failure(
                    "GS1_LINTER_INVALID_CSET82_CHARACTER",
                    index,
                    1);
            }

            sum = sum + weight
                * AlphaChecksumPrimes[data.Length - 3 - index];
            index = index + 1;
        }

        sum = sum % 1021;
        if (data[data.Length - 2] != cset32[sum >> 5]
            || data[data.Length - 1] != cset32[sum & 31])
        {
            return Failure(
                "GS1_LINTER_INCORRECT_CHECK_PAIR",
                data.Length - 2,
                2);
        }

        return Success();
    }

    static Gs1LinterResult LintCompanyPrefix(string data)
    {
        if (data.Length < 4)
        {
            return Failure("GS1_LINTER_TOO_SHORT_FOR_GCP", 0, data.Length);
        }

        int index = 0;
        while (index < 4)
        {
            if (!IsDigit(data[index]))
            {
                return Failure("GS1_LINTER_INVALID_GCP_PREFIX", index, 1);
            }

            index = index + 1;
        }

        return Success();
    }

    static Gs1LinterResult LintCompanyPrefixAfterIndicator(string data)
    {
        if (data.Length < 2)
        {
            return Failure("GS1_LINTER_TOO_SHORT_FOR_GCP", 0, data.Length);
        }

        Gs1LinterResult result = LintCompanyPrefix(data.Substring(1));
        return ShiftFailure(result, 1);
    }

    static Gs1LinterResult LintHasNonDigit(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return Success();
            }

            index = index + 1;
        }

        return Failure(
            "GS1_LINTER_REQUIRES_NON_DIGIT_CHARACTER",
            0,
            data.Length);
    }

    static Gs1LinterResult LintHour(string data)
    {
        return LintClockPart(
            data,
            23,
            "GS1_LINTER_HOUR_TOO_SHORT",
            "GS1_LINTER_HOUR_TOO_LONG",
            "GS1_LINTER_ILLEGAL_HOUR");
    }

    static Gs1LinterResult LintHourMinute(string data)
    {
        if (data.Length != 4)
        {
            return Failure(
                data.Length < 4
                    ? "GS1_LINTER_HOUR_WITH_MINUTE_TOO_SHORT"
                    : "GS1_LINTER_HOUR_WITH_MINUTE_TOO_LONG",
                0,
                data.Length);
        }

        Gs1LinterResult hour = LintHour(data.Substring(0, 2));
        if (!hour.Success)
        {
            return hour;
        }

        Gs1LinterResult minute = LintMinute(data.Substring(2, 2));
        return ShiftFailure(minute, 2);
    }

    static Gs1LinterResult LintHyphen(string data)
    {
        if (data.Length == 0)
        {
            return Failure("GS1_LINTER_NOT_HYPHEN", 0, 0);
        }

        int index = 0;
        while (index < data.Length)
        {
            if (data[index] != '-')
            {
                return Failure("GS1_LINTER_NOT_HYPHEN", 0, data.Length);
            }

            index = index + 1;
        }

        return Success();
    }

    static Gs1LinterResult LintIban(string data)
    {
        if (data.Length < 4)
        {
            return Failure("GS1_LINTER_IBAN_TOO_SHORT", 0, data.Length);
        }

        Gs1LinterResult country = LintIso3166Alpha2(data.Substring(0, 2));
        if (!country.Success)
        {
            return Failure("GS1_LINTER_ILLEGAL_IBAN_COUNTRY_CODE", 0, 2);
        }

        if (data.Length > 34)
        {
            return Failure("GS1_LINTER_IBAN_TOO_LONG", 0, data.Length);
        }

        if (data.Length <= 10)
        {
            return Failure("GS1_LINTER_IBAN_TOO_SHORT", 0, data.Length);
        }

        int checksum = 0;
        int index = 4;
        while (index < data.Length + 4)
        {
            int actualIndex = index < data.Length
                ? index
                : index - data.Length;
            char character = data[actualIndex];
            int value = IbanCharacterValue(character);
            if (value < 0)
            {
                return Failure(
                    "GS1_LINTER_INVALID_IBAN_CHARACTER",
                    actualIndex,
                    1);
            }

            checksum = checksum * (value < 10 ? 10 : 100) + value;
            checksum = checksum % 97;
            index = index + 1;
        }

        if (checksum != 1)
        {
            return Failure("GS1_LINTER_INCORRECT_IBAN_CHECKSUM", 2, 2);
        }

        return Success();
    }

    static Gs1LinterResult LintImporterIndex(string data)
    {
        if (data.Length != 1)
        {
            return Failure(
                "GS1_LINTER_IMPORTER_IDX_MUST_BE_ONE_CHARACTER",
                0,
                data.Length);
        }

        char character = data[0];
        bool valid = character == '-'
            || character == '_'
            || IsDigit(character)
            || (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z');
        if (!valid)
        {
            return Failure("GS1_LINTER_INVALID_IMPORT_IDX_CHARACTER", 0, 1);
        }

        return Success();
    }

    static Gs1LinterResult LintIso3166(string data)
    {
        if (data.Length == 3
            && IsDigit(data[0])
            && IsDigit(data[1])
            && IsDigit(data[2]))
        {
            int value = (data[0] - '0') * 100
                + (data[1] - '0') * 10
                + data[2] - '0';
            if (LookupBit(Iso3166Bits, value))
            {
                return Success();
            }
        }

        return Failure("GS1_LINTER_NOT_ISO3166", 0, data.Length);
    }

    static Gs1LinterResult LintIso3166Or999(string data)
    {
        if (data == "999")
        {
            return Success();
        }

        Gs1LinterResult result = LintIso3166(data);
        if (result.Success)
        {
            return result;
        }

        return Failure("GS1_LINTER_NOT_ISO3166_OR_999", 0, data.Length);
    }

    static Gs1LinterResult LintIso3166Alpha2(string data)
    {
        if (data.Length == 2
            && data[0] >= 'A'
            && data[0] <= 'Z'
            && data[1] >= 'A'
            && data[1] <= 'Z')
        {
            int value = (data[0] - 'A') * 26 + data[1] - 'A';
            if (LookupBit(Iso3166Alpha2Bits, value))
            {
                return Success();
            }
        }

        return Failure("GS1_LINTER_NOT_ISO3166_ALPHA2", 0, data.Length);
    }

    static Gs1LinterResult LintIso4217(string data)
    {
        if (data.Length == 3
            && IsDigit(data[0])
            && IsDigit(data[1])
            && IsDigit(data[2]))
        {
            int value = (data[0] - '0') * 100
                + (data[1] - '0') * 10
                + data[2] - '0';
            if (LookupBit(Iso4217Bits, value))
            {
                return Success();
            }
        }

        return Failure("GS1_LINTER_NOT_ISO4217", 0, data.Length);
    }

    static Gs1LinterResult LintIso5218(string data)
    {
        if (data.Length != 1
            || (data[0] != '0'
                && data[0] != '1'
                && data[0] != '2'
                && data[0] != '9'))
        {
            return Failure(
                "GS1_LINTER_INVALID_BIOLOGICAL_SEX_CODE",
                0,
                data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintLatitude(string data)
    {
        return LintCoordinate(
            data,
            1800000000L,
            "GS1_LINTER_LATITUDE_INVALID_LENGTH",
            "GS1_LINTER_INVALID_LATITUDE");
    }

    static Gs1LinterResult LintLongitude(string data)
    {
        return LintCoordinate(
            data,
            3600000000L,
            "GS1_LINTER_LONGITUDE_INVALID_LENGTH",
            "GS1_LINTER_INVALID_LONGITUDE");
    }

    static Gs1LinterResult LintMediaType(string data)
    {
        if (data.Length == 2 && IsDigit(data[0]) && IsDigit(data[1]))
        {
            int value = (data[0] - '0') * 10 + data[1] - '0';
            if ((value >= 1 && value <= 10) || value >= 80)
            {
                return Success();
            }
        }

        return Failure("GS1_LINTER_INVALID_MEDIA_TYPE", 0, data.Length);
    }

    static Gs1LinterResult LintMinute(string data)
    {
        return LintClockPart(
            data,
            59,
            "GS1_LINTER_MINUTE_TOO_SHORT",
            "GS1_LINTER_MINUTE_TOO_LONG",
            "GS1_LINTER_ILLEGAL_MINUTE");
    }

    static Gs1LinterResult LintNonZero(string data)
    {
        bool hasNonZero = false;
        int index = 0;
        while (index < data.Length)
        {
            char character = data[index];
            if (!IsDigit(character))
            {
                return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", index, 1);
            }

            if (character != '0')
            {
                hasNonZero = true;
            }

            index = index + 1;
        }

        if (!hasNonZero)
        {
            return Failure("GS1_LINTER_ILLEGAL_ZERO_VALUE", 0, data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintNoZeroPrefix(string data)
    {
        if (data.Length > 0 && data[0] == '0')
        {
            return Failure("GS1_LINTER_ILLEGAL_ZERO_PREFIX", 0, 1);
        }

        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", index, 1);
            }

            index = index + 1;
        }

        return Success();
    }

    static Gs1LinterResult LintPackageType(string data)
    {
        if (PackageTypes.IndexOf(" " + data + " ") < 0)
        {
            return Failure("GS1_LINTER_INVALID_PACKAGE_TYPE", 0, data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintPercentEncoding(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (data[index] == '%')
            {
                if (index + 2 >= data.Length)
                {
                    return Failure(
                        "GS1_LINTER_INVALID_PERCENT_SEQUENCE",
                        index,
                        data.Length - index);
                }

                if (!IsHexDigit(data[index + 1])
                    || !IsHexDigit(data[index + 2]))
                {
                    return Failure(
                        "GS1_LINTER_INVALID_PERCENT_SEQUENCE",
                        index,
                        3);
                }

                index = index + 2;
            }

            index = index + 1;
        }

        return Success();
    }

    static Gs1LinterResult LintPieceOfTotal(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", index, 1);
            }

            index = index + 1;
        }

        if (data.Length == 0 || data.Length % 2 != 0)
        {
            return Failure(
                "GS1_LINTER_INVALID_LENGTH_FOR_PIECE_OF_TOTAL",
                0,
                data.Length);
        }

        int halfLength = data.Length / 2;
        bool pieceIsZero = true;
        bool totalIsZero = true;
        int comparison = 0;
        index = 0;
        while (index < halfLength)
        {
            char piece = data[index];
            char total = data[halfLength + index];
            if (piece != '0')
            {
                pieceIsZero = false;
            }

            if (total != '0')
            {
                totalIsZero = false;
            }

            if (comparison == 0 && piece != total)
            {
                comparison = piece < total ? -1 : 1;
            }

            index = index + 1;
        }

        if (pieceIsZero)
        {
            return Failure("GS1_LINTER_ZERO_PIECE_NUMBER", 0, halfLength);
        }

        if (totalIsZero)
        {
            return Failure(
                "GS1_LINTER_ZERO_TOTAL_PIECES",
                halfLength,
                halfLength);
        }

        if (comparison > 0)
        {
            return Failure(
                "GS1_LINTER_PIECE_NUMBER_EXCEEDS_TOTAL",
                0,
                data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintPositionInSequence(string data)
    {
        int slashOffset = 0;
        while (slashOffset < data.Length && IsDigit(data[slashOffset]))
        {
            slashOffset = slashOffset + 1;
        }

        if (slashOffset == 0
            || slashOffset >= data.Length
            || data[slashOffset] != '/')
        {
            return Failure(
                "GS1_LINTER_POSITION_IN_SEQUENCE_MALFORMED",
                0,
                data.Length);
        }

        int index = slashOffset + 1;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return Failure(
                    "GS1_LINTER_POSITION_IN_SEQUENCE_MALFORMED",
                    0,
                    data.Length);
            }

            index = index + 1;
        }

        if (slashOffset == data.Length - 1)
        {
            return Failure(
                "GS1_LINTER_POSITION_IN_SEQUENCE_MALFORMED",
                0,
                data.Length);
        }

        int endLength = data.Length - slashOffset - 1;
        if (data[0] == '0')
        {
            return Failure(
                "GS1_LINTER_ILLEGAL_ZERO_PREFIX",
                0,
                slashOffset);
        }

        if (data[slashOffset + 1] == '0')
        {
            return Failure(
                "GS1_LINTER_ILLEGAL_ZERO_PREFIX",
                slashOffset + 1,
                endLength);
        }

        bool exceeds = slashOffset > endLength;
        if (slashOffset == endLength)
        {
            index = 0;
            while (index < slashOffset && data[index] == data[slashOffset + 1 + index])
            {
                index = index + 1;
            }

            if (index < slashOffset
                && data[index] > data[slashOffset + 1 + index])
            {
                exceeds = true;
            }
        }

        if (exceeds)
        {
            return Failure(
                "GS1_LINTER_POSITION_EXCEEDS_END",
                0,
                data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintSecond(string data)
    {
        return LintClockPart(
            data,
            59,
            "GS1_LINTER_SECOND_TOO_SHORT",
            "GS1_LINTER_SECOND_TOO_LONG",
            "GS1_LINTER_ILLEGAL_SECOND");
    }

    static Gs1LinterResult LintWinding(string data)
    {
        if (data.Length != 1
            || (data[0] != '0' && data[0] != '1' && data[0] != '9'))
        {
            return Failure(
                "GS1_LINTER_INVALID_WINDING_DIRECTION",
                0,
                data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintYesNo(string data)
    {
        if (data.Length != 1 || (data[0] != '0' && data[0] != '1'))
        {
            return Failure("GS1_LINTER_NOT_ZERO_OR_ONE", 0, data.Length);
        }

        return Success();
    }

    static Gs1LinterResult LintShortDateAllowDayZero(string data)
    {
        if (data.Length != 6)
        {
            return Failure(
                data.Length < 6
                    ? "GS1_LINTER_DATE_TOO_SHORT"
                    : "GS1_LINTER_DATE_TOO_LONG",
                0,
                data.Length);
        }

        int invalidOffset = FindNonDigit(data);
        if (invalidOffset >= 0)
        {
            return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", invalidOffset, 1);
        }

        int shortYear = ParseTwoDigits(data, 0);
        int year;
        if (shortYear - 21 >= 51)
        {
            year = 1900 + shortYear;
        }
        else if (shortYear - 21 > -50)
        {
            year = 2000 + shortYear;
        }
        else
        {
            year = 2100 + shortYear;
        }

        return ValidateCalendarDate(
            year,
            ParseTwoDigits(data, 2),
            ParseTwoDigits(data, 4),
            2,
            4);
    }

    static Gs1LinterResult LintShortDate(string data)
    {
        Gs1LinterResult result = LintShortDateAllowDayZero(data);
        if (!result.Success)
        {
            return result;
        }

        if (data[4] == '0' && data[5] == '0')
        {
            return Failure("GS1_LINTER_ILLEGAL_DAY", 4, 2);
        }

        return Success();
    }

    static Gs1LinterResult LintLongDate(string data)
    {
        if (data.Length != 8)
        {
            return Failure(
                data.Length < 8
                    ? "GS1_LINTER_DATE_TOO_SHORT"
                    : "GS1_LINTER_DATE_TOO_LONG",
                0,
                data.Length);
        }

        int invalidOffset = FindNonDigit(data);
        if (invalidOffset >= 0)
        {
            return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", invalidOffset, 1);
        }

        int year = ParseTwoDigits(data, 0) * 100
            + ParseTwoDigits(data, 2);
        Gs1LinterResult result = ValidateCalendarDate(
            year,
            ParseTwoDigits(data, 4),
            ParseTwoDigits(data, 6),
            4,
            6);
        if (!result.Success)
        {
            return result;
        }

        if (data[6] == '0' && data[7] == '0')
        {
            return Failure("GS1_LINTER_ILLEGAL_DAY", 6, 2);
        }

        return Success();
    }

    static Gs1LinterResult LintZero(string data)
    {
        if (data.Length == 0)
        {
            return Failure("GS1_LINTER_NOT_ZERO", 0, 0);
        }

        int index = 0;
        while (index < data.Length)
        {
            if (data[index] != '0')
            {
                return Failure("GS1_LINTER_NOT_ZERO", 0, data.Length);
            }

            index = index + 1;
        }

        return Success();
    }

    static Gs1LinterResult LintClockPart(
        string data,
        int maximum,
        string tooShortCode,
        string tooLongCode,
        string invalidCode)
    {
        if (data.Length != 2)
        {
            return Failure(
                data.Length < 2 ? tooShortCode : tooLongCode,
                0,
                data.Length);
        }

        int invalidOffset = FindNonDigit(data);
        if (invalidOffset >= 0)
        {
            return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", invalidOffset, 1);
        }

        if (ParseTwoDigits(data, 0) > maximum)
        {
            return Failure(invalidCode, 0, 2);
        }

        return Success();
    }

    static Gs1LinterResult LintCoordinate(
        string data,
        long maximum,
        string lengthCode,
        string rangeCode)
    {
        if (data.Length != 10)
        {
            return Failure(lengthCode, 0, data.Length);
        }

        long value = 0;
        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return Failure("GS1_LINTER_NON_DIGIT_CHARACTER", index, 1);
            }

            value = value * 10 + data[index] - '0';
            index = index + 1;
        }

        if (value > maximum)
        {
            return Failure(rangeCode, 0, 10);
        }

        return Success();
    }

    static Gs1LinterResult ValidateCalendarDate(
        int year,
        int month,
        int day,
        int monthOffset,
        int dayOffset)
    {
        if (month < 1 || month > 12)
        {
            return Failure("GS1_LINTER_ILLEGAL_MONTH", monthOffset, 2);
        }

        int maximumDay;
        if (month == 2)
        {
            bool leapYear = (year % 4 == 0 && year % 100 != 0)
                || year % 400 == 0;
            maximumDay = leapYear ? 29 : 28;
        }
        else if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            maximumDay = 30;
        }
        else
        {
            maximumDay = 31;
        }

        if (day > maximumDay)
        {
            return Failure("GS1_LINTER_ILLEGAL_DAY", dayOffset, 2);
        }

        return Success();
    }

    static int FindNonDigit(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigit(data[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int ParseTwoDigits(string data, int offset)
    {
        return (data[offset] - '0') * 10 + data[offset + 1] - '0';
    }

    static int IbanCharacterValue(char character)
    {
        if (IsDigit(character))
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'Z')
        {
            return character - 'A' + 10;
        }

        return -1;
    }

    static bool LookupBit(ulong[] values, int bit)
    {
        int word = bit / 64;
        int shift = 63 - bit % 64;
        return (values[word] & (1UL << shift)) != 0;
    }

    static bool IsDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    static bool IsHexDigit(char character)
    {
        return IsDigit(character)
            || (character >= 'A' && character <= 'F')
            || (character >= 'a' && character <= 'f');
    }

    static Gs1LinterResult ShiftFailure(
        Gs1LinterResult result,
        int offset)
    {
        if (!result.Success)
        {
            result.ErrorOffset = result.ErrorOffset + offset;
        }

        return result;
    }

    static Gs1LinterResult Success()
    {
        return SuccessResult();
    }

    static Gs1LinterResult Failure(
        string code,
        int offset,
        int length)
    {
        Gs1LinterResult result = new Gs1LinterResult();
        result.Success = false;
        result.Code = code;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        return result;
    }
}
