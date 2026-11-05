namespace icc;

/// <summary>
/// Defines ICC.1 colour-space signatures and ICC.2 extended-space prefixes.
/// </summary>
public static class IccColorSpaceSignatures
{
    /// <summary>The CIE XYZ signature <c>XYZ </c>.</summary>
    public const uint Xyz = 0x58595a20;

    /// <summary>The CIE Lab signature <c>Lab </c>.</summary>
    public const uint Lab = 0x4c616220;

    /// <summary>The CIE Luv signature <c>Luv </c>.</summary>
    public const uint Luv = 0x4c757620;

    /// <summary>The YCbCr signature <c>YCbr</c>.</summary>
    public const uint YCbCr = 0x59436272;

    /// <summary>The CIE Yxy signature <c>Yxy </c>.</summary>
    public const uint Yxy = 0x59787920;

    /// <summary>The RGB signature <c>RGB </c>.</summary>
    public const uint Rgb = 0x52474220;

    /// <summary>The grayscale signature <c>GRAY</c>.</summary>
    public const uint Gray = 0x47524159;

    /// <summary>The HSV signature <c>HSV </c>.</summary>
    public const uint Hsv = 0x48535620;

    /// <summary>The HLS signature <c>HLS </c>.</summary>
    public const uint Hls = 0x484c5320;

    /// <summary>The CMYK signature <c>CMYK</c>.</summary>
    public const uint Cmyk = 0x434d594b;

    /// <summary>The CMY signature <c>CMY </c>.</summary>
    public const uint Cmy = 0x434d5920;

    /// <summary>The named-colour signature <c>nmcl</c>.</summary>
    public const uint NamedColor = 0x6e6d636c;

    /// <summary>The one-channel signature <c>1CLR</c>.</summary>
    public const uint OneColor = 0x31434c52;

    /// <summary>The two-channel signature <c>2CLR</c>.</summary>
    public const uint TwoColor = 0x32434c52;

    /// <summary>The three-channel signature <c>3CLR</c>.</summary>
    public const uint ThreeColor = 0x33434c52;

    /// <summary>The four-channel signature <c>4CLR</c>.</summary>
    public const uint FourColor = 0x34434c52;

    /// <summary>The five-channel signature <c>5CLR</c>.</summary>
    public const uint FiveColor = 0x35434c52;

    /// <summary>The six-channel signature <c>6CLR</c>.</summary>
    public const uint SixColor = 0x36434c52;

    /// <summary>The seven-channel signature <c>7CLR</c>.</summary>
    public const uint SevenColor = 0x37434c52;

    /// <summary>The eight-channel signature <c>8CLR</c>.</summary>
    public const uint EightColor = 0x38434c52;

    /// <summary>The nine-channel signature <c>9CLR</c>.</summary>
    public const uint NineColor = 0x39434c52;

    /// <summary>The ten-channel signature <c>ACLR</c>.</summary>
    public const uint TenColor = 0x41434c52;

    /// <summary>The eleven-channel signature <c>BCLR</c>.</summary>
    public const uint ElevenColor = 0x42434c52;

    /// <summary>The twelve-channel signature <c>CCLR</c>.</summary>
    public const uint TwelveColor = 0x43434c52;

    /// <summary>The thirteen-channel signature <c>DCLR</c>.</summary>
    public const uint ThirteenColor = 0x44434c52;

    /// <summary>The fourteen-channel signature <c>ECLR</c>.</summary>
    public const uint FourteenColor = 0x45434c52;

    /// <summary>The fifteen-channel signature <c>FCLR</c>.</summary>
    public const uint FifteenColor = 0x46434c52;

    /// <summary>The ICC.2 extended N-channel device-data signature prefix <c>nc</c>.</summary>
    public const uint ExtendedDevicePrefix = 0x6e630000;

    /// <summary>The ICC.2 reflectance-spectral signature prefix <c>rs</c>.</summary>
    public const uint ReflectanceSpectralPrefix = 0x72730000;

    /// <summary>The ICC.2 transmission-spectral signature prefix <c>ts</c>.</summary>
    public const uint TransmissionSpectralPrefix = 0x74730000;

    /// <summary>The ICC.2 radiant-spectral signature prefix <c>es</c>.</summary>
    public const uint RadiantSpectralPrefix = 0x65730000;

    /// <summary>The ICC.2 bi-spectral signature prefix <c>bs</c>.</summary>
    public const uint BiSpectralPrefix = 0x62730000;

    /// <summary>The ICC.2 sparse-matrix spectral signature prefix <c>sm</c>.</summary>
    public const uint SparseMatrixSpectralPrefix = 0x736d0000;

    /// <summary>The ICC.2 multiplex-channel signature prefix <c>mc</c>.</summary>
    public const uint MultiplexPrefix = 0x6d630000;
}
