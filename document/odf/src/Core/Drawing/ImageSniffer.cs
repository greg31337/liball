namespace odf;

internal class ImageInspection
{
    internal OdfImageFormat Format;
    internal string MediaType = "";
    internal bool IsRecognized;
}

internal static class ImageSniffer
{
    internal static ImageInspection Inspect(byte[] data)
    {
        ImageInspection result = new ImageInspection();
        if (data == null)
        {
            return result;
        }
        if (IsPng(data))
        {
            SetResult(result, OdfImageFormat.Png, OdfMediaTypes.Png);
        }
        else if (IsJpeg(data))
        {
            SetResult(result, OdfImageFormat.Jpeg, OdfMediaTypes.Jpeg);
        }
        else if (IsGif(data))
        {
            SetResult(result, OdfImageFormat.Gif, OdfMediaTypes.Gif);
        }
        else if (IsBmp(data))
        {
            SetResult(result, OdfImageFormat.Bmp, OdfMediaTypes.Bmp);
        }
        else if (IsTiff(data))
        {
            SetResult(result, OdfImageFormat.Tiff, OdfMediaTypes.Tiff);
        }
        else if (IsWebP(data))
        {
            SetResult(result, OdfImageFormat.WebP, OdfMediaTypes.WebP);
        }
        else if (IsSvg(data))
        {
            SetResult(result, OdfImageFormat.Svg, OdfMediaTypes.Svg);
        }
        return result;
    }

    static void SetResult(
        ImageInspection result,
        OdfImageFormat format,
        string mediaType)
    {
        result.Format = format;
        result.MediaType = mediaType;
        result.IsRecognized = true;
    }

    static bool IsPng(byte[] data)
    {
        return data.Length >= 8
            && data[0] == 137
            && data[1] == 80
            && data[2] == 78
            && data[3] == 71
            && data[4] == 13
            && data[5] == 10
            && data[6] == 26
            && data[7] == 10;
    }

    static bool IsJpeg(byte[] data)
    {
        return data.Length >= 3
            && data[0] == 255
            && data[1] == 216
            && data[2] == 255;
    }

    static bool IsGif(byte[] data)
    {
        return data.Length >= 6
            && data[0] == 71
            && data[1] == 73
            && data[2] == 70
            && data[3] == 56
            && (data[4] == 55 || data[4] == 57)
            && data[5] == 97;
    }

    static bool IsBmp(byte[] data)
    {
        return data.Length >= 2 && data[0] == 66 && data[1] == 77;
    }

    static bool IsTiff(byte[] data)
    {
        return data.Length >= 4
            && ((data[0] == 73
                    && data[1] == 73
                    && data[2] == 42
                    && data[3] == 0)
                || (data[0] == 77
                    && data[1] == 77
                    && data[2] == 0
                    && data[3] == 42));
    }

    static bool IsWebP(byte[] data)
    {
        return data.Length >= 12
            && data[0] == 82
            && data[1] == 73
            && data[2] == 70
            && data[3] == 70
            && data[8] == 87
            && data[9] == 69
            && data[10] == 66
            && data[11] == 80;
    }

    static bool IsSvg(byte[] data)
    {
        if (data.Length == 0)
        {
            return false;
        }
        int count = data.Length < 4096 ? data.Length : 4096;
        int byteIndex = 0;
        while (byteIndex < count)
        {
            if (data[byteIndex] == 0)
            {
                return false;
            }
            byteIndex = byteIndex + 1;
        }
        int start = 0;
        if (count >= 3
            && data[0] == 239
            && data[1] == 187
            && data[2] == 191)
        {
            start = 3;
        }
        byteIndex = start;
        while (byteIndex < count)
        {
            if (data[byteIndex] > 127)
            {
                return false;
            }
            byteIndex = byteIndex + 1;
        }
        string text = global::System.Text.Encoding.ASCII.GetString(
            data,
            start,
            count - start);
        int index = 0;
        index = SkipWhitespace(text, index);
        if (StartsWith(text, index, "<?xml"))
        {
            int declarationEnd = text.IndexOf("?>", index, global::System.StringComparison.Ordinal);
            if (declarationEnd < 0)
            {
                return false;
            }
            index = SkipWhitespace(text, declarationEnd + 2);
        }
        while (StartsWith(text, index, "<!--"))
        {
            int commentEnd = text.IndexOf("-->", index, global::System.StringComparison.Ordinal);
            if (commentEnd < 0)
            {
                return false;
            }
            index = SkipWhitespace(text, commentEnd + 3);
        }
        if (!StartsWith(text, index, "<svg"))
        {
            return false;
        }
        int following = index + 4;
        return following < text.Length
            && (global::System.Char.IsWhiteSpace(text[following])
                || text[following] == '>'
                || text[following] == '/');
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length
            && global::System.Char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }
        return index;
    }

    static bool StartsWith(string text, int index, string value)
    {
        if (index < 0 || index + value.Length > text.Length)
        {
            return false;
        }
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (text[index + valueIndex] != value[valueIndex])
            {
                return false;
            }
            valueIndex = valueIndex + 1;
        }
        return true;
    }
}
