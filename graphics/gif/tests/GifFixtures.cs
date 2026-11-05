namespace gif.Tests;

public class GifFixtures
{
    public static byte[] MinimalGif87a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 55, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] MinimalGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] TransparentGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xF9, 4, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] InterlacedGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 4, 0, 0x81, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            0x2C, 0, 0, 0, 0, 2, 0, 4, 0, 0x40,
            2, 7, 4, 65, 81, 12, 195, 113, 5, 0,
            0x3B
        };
    }

    public static byte[] LocalColorTableGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0x80,
            255, 0, 0, 0, 0, 255,
            2, 2, 76, 1, 0,
            0x3B
        };
    }

    public static byte[] AnimatedDisposalMethod0Gif89a()
    {
        return AnimatedDisposalGif89a(0);
    }

    public static byte[] AnimatedDisposalMethod1Gif89a()
    {
        return AnimatedDisposalGif89a(1);
    }

    public static byte[] AnimatedDisposalMethod2Gif89a()
    {
        return AnimatedDisposalGif89a(2);
    }

    public static byte[] AnimatedDisposalMethod3Gif89a()
    {
        return AnimatedDisposalGif89a(3);
    }

    public static byte[] NetscapeLoopGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48,
            3, 1, 5, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] CommentGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFE, 7, 102, 105, 120, 116, 117, 114, 101, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] ApplicationExtensionGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49,
            3, 9, 8, 7, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] PlainTextGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0x01, 12, 0, 0, 0, 0, 1, 0, 1, 0, 8, 8, 1, 0,
            5, 112, 108, 97, 105, 110, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    public static byte[] ComplexValidGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            3, 0, 2, 0, 0x81, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            0x21, 0xFE, 3, 99, 111, 109, 4, 112, 108, 101, 120, 0,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48,
            3, 1, 2, 0, 0,
            0x21, 0xF9, 4, 4, 5, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 3, 0, 2, 0, 0,
            2, 1, 12, 2, 126, 5, 0,
            0x21, 0xF9, 4, 13, 7, 0, 0, 0,
            0x2C, 1, 0, 0, 0, 2, 0, 2, 0, 0x81,
            0, 0, 0, 255, 255, 0, 255, 0, 255, 0, 255, 255,
            2, 2, 68, 52, 1, 5, 0,
            0x21, 0xF9, 4, 5, 11, 0, 0, 0,
            0x2C, 2, 0, 0, 0, 1, 0, 2, 0, 0,
            2, 2, 196, 10, 0,
            0x3B
        };
    }

    static byte[] AnimatedDisposalGif89a(int disposalMethod)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0x81, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 0, 255, 0, 255, 0,
            0x21, 0xF9, 4, 4, 1, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 76, 1, 0,
            0x21, 0xF9, 4, (byte)(disposalMethod * 4), 1, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 84, 1, 0,
            0x21, 0xF9, 4, 4, 1, 0, 0, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 92, 1, 0,
            0x3B
        };
    }
}
