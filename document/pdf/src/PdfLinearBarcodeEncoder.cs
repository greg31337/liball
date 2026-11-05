using System;
using System.Collections.Generic;

namespace pdf;

internal class PdfLinearBarcode
{
    internal bool[] Modules;
    internal string DisplayText;

    internal PdfLinearBarcode(bool[] modules, string displayText)
    {
        Modules = modules;
        DisplayText = displayText;
    }
}

internal static class PdfLinearBarcodeEncoder
{
    internal static readonly string[] Code128Patterns = new string[]
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    };
    internal static readonly string Code39Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
    internal static readonly string[] Code39Patterns = new string[]
    {
        "nnnwwnwnn", "wnnwnnnnw", "nnwwnnnnw", "wnwwnnnnn", "nnnwwnnnw", "wnnwwnnnn", "nnwwwnnnn", "nnnwnnwnw", "wnnwnnwnn", "nnwwnnwnn",
        "wnnnnwnnw", "nnwnnwnnw", "wnwnnwnnn", "nnnnwwnnw", "wnnnwwnnn", "nnwnwwnnn", "nnnnnwwnw", "wnnnnwwnn", "nnwnnwwnn", "nnnnwwwnn",
        "wnnnnnnww", "nnwnnnnww", "wnwnnnnwn", "nnnnwnnww", "wnnnwnnwn", "nnwnwnnwn", "nnnnnnwww", "wnnnnnwwn", "nnwnnnwwn", "nnnnwnwwn",
        "wwnnnnnnw", "nwwnnnnnw", "wwwnnnnnn", "nwnnwnnnw", "wwnnwnnnn", "nwwnwnnnn", "nwnnnnwnw", "wwnnnnwnn", "nwwnnnwnn", "nwnwnwnnn",
        "nwnwnnnwn", "nwnnnwnwn", "nnnwnwnwn", "nwnnwnwnn"
    };
    internal static readonly string[] EanLeftOdd = new string[] { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
    internal static readonly string[] EanLeftEven = new string[] { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
    internal static readonly string[] EanRight = new string[] { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
    internal static readonly string[] EanParity = new string[] { "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL" };

    internal static PdfLinearBarcode Encode(string text, PdfBarcodeFormat format)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0)
        {
            throw new ArgumentException("Barcode text cannot be empty.", "text");
        }

        if (text.Length > 256)
        {
            throw new ArgumentException("Barcode text exceeds the supported length.", "text");
        }

        if (format == PdfBarcodeFormat.Code39)
        {
            return EncodeCode39(text);
        }

        if (format == PdfBarcodeFormat.Code128)
        {
            return EncodeCode128(text);
        }

        if (format == PdfBarcodeFormat.Ean13)
        {
            return EncodeEan13(text);
        }

        if (format == PdfBarcodeFormat.UpcA)
        {
            return EncodeUpcA(text);
        }

        throw new ArgumentException("Barcode format is unsupported.", "format");
    }

    internal static PdfLinearBarcode EncodeCode39(string text)
    {
        string normalized = text.ToUpperInvariant();
        if (normalized.IndexOf('*') >= 0)
        {
            throw new ArgumentException("Code 39 text cannot contain the start or stop character.", "text");
        }

        for (int index = 0; index < normalized.Length; index++)
        {
            if (Code39Characters.IndexOf(normalized[index]) < 0)
            {
                throw new ArgumentException("Code 39 text contains an unsupported character.", "text");
            }
        }

        List<bool> modules = new List<bool>();
        AppendQuietZone(modules, 10);
        string framed = "*" + normalized + "*";
        for (int index = 0; index < framed.Length; index++)
        {
            int patternIndex = Code39Characters.IndexOf(framed[index]);
            string pattern = Code39Patterns[patternIndex];
            bool bar = true;
            for (int element = 0; element < pattern.Length; element++)
            {
                int width = pattern[element] == 'w' ? 3 : 1;
                AppendModules(modules, bar, width);
                bar = !bar;
            }

            if (index + 1 < framed.Length)
            {
                modules.Add(false);
            }
        }

        AppendQuietZone(modules, 10);
        return new PdfLinearBarcode(modules.ToArray(), normalized);
    }

    internal static PdfLinearBarcode EncodeCode128(string text)
    {
        List<int> values = new List<int>();
        values.Add(104);
        int checksum = 104;
        for (int index = 0; index < text.Length; index++)
        {
            int character = text[index];
            if (character < 32 || character > 126)
            {
                throw new ArgumentException("Code 128B text must contain printable ASCII characters.", "text");
            }

            int value = character - 32;
            values.Add(value);
            checksum += value * (index + 1);
        }

        values.Add(checksum % 103);
        values.Add(106);
        List<bool> modules = new List<bool>();
        AppendQuietZone(modules, 10);
        for (int index = 0; index < values.Count; index++)
        {
            AppendCode128Pattern(modules, Code128Patterns[values[index]]);
        }

        AppendQuietZone(modules, 10);
        return new PdfLinearBarcode(modules.ToArray(), text);
    }

    internal static PdfLinearBarcode EncodeEan13(string text)
    {
        ValidateDigits(text, "EAN-13");
        string normalized;
        if (text.Length == 12)
        {
            normalized = text + CalculateEanCheckDigit(text).ToString();
        }
        else if (text.Length == 13)
        {
            int expected = CalculateEanCheckDigit(text.Substring(0, 12));
            if (text[12] - '0' != expected)
            {
                throw new ArgumentException("EAN-13 check digit is invalid.", "text");
            }

            normalized = text;
        }
        else
        {
            throw new ArgumentException("EAN-13 requires twelve data digits or thirteen digits including the check digit.", "text");
        }

        List<bool> modules = new List<bool>();
        AppendQuietZone(modules, 11);
        AppendBinaryPattern(modules, "101");
        string parity = EanParity[normalized[0] - '0'];
        for (int index = 1; index <= 6; index++)
        {
            int digit = normalized[index] - '0';
            AppendBinaryPattern(modules, parity[index - 1] == 'L' ? EanLeftOdd[digit] : EanLeftEven[digit]);
        }

        AppendBinaryPattern(modules, "01010");
        for (int index = 7; index < 13; index++)
        {
            AppendBinaryPattern(modules, EanRight[normalized[index] - '0']);
        }

        AppendBinaryPattern(modules, "101");
        AppendQuietZone(modules, 11);
        return new PdfLinearBarcode(modules.ToArray(), normalized);
    }

    internal static PdfLinearBarcode EncodeUpcA(string text)
    {
        ValidateDigits(text, "UPC-A");
        string normalized;
        if (text.Length == 11)
        {
            int check = CalculateEanCheckDigit("0" + text);
            normalized = text + check.ToString();
        }
        else if (text.Length == 12)
        {
            int expected = CalculateEanCheckDigit("0" + text.Substring(0, 11));
            if (text[11] - '0' != expected)
            {
                throw new ArgumentException("UPC-A check digit is invalid.", "text");
            }

            normalized = text;
        }
        else
        {
            throw new ArgumentException("UPC-A requires eleven data digits or twelve digits including the check digit.", "text");
        }

        PdfLinearBarcode ean = EncodeEan13("0" + normalized);
        return new PdfLinearBarcode(ean.Modules, normalized);
    }

    internal static int CalculateEanCheckDigit(string firstTwelveDigits)
    {
        int sum = 0;
        for (int index = 0; index < firstTwelveDigits.Length; index++)
        {
            int digit = firstTwelveDigits[index] - '0';
            sum += digit * (index % 2 == 0 ? 1 : 3);
        }

        return (10 - sum % 10) % 10;
    }

    internal static void ValidateDigits(string text, string formatName)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                throw new ArgumentException(formatName + " text must contain only digits.", "text");
            }
        }
    }

    internal static void AppendCode128Pattern(List<bool> modules, string pattern)
    {
        bool bar = true;
        for (int index = 0; index < pattern.Length; index++)
        {
            AppendModules(modules, bar, pattern[index] - '0');
            bar = !bar;
        }
    }

    internal static void AppendBinaryPattern(List<bool> modules, string pattern)
    {
        for (int index = 0; index < pattern.Length; index++)
        {
            modules.Add(pattern[index] == '1');
        }
    }

    internal static void AppendQuietZone(List<bool> modules, int width)
    {
        AppendModules(modules, false, width);
    }

    internal static void AppendModules(List<bool> modules, bool value, int count)
    {
        for (int index = 0; index < count; index++)
        {
            modules.Add(value);
        }
    }
}
