namespace ttf;

internal static class Woff2KnownTags
{
    static readonly int[] tags = new int[]
    {
        0x636D6170, 0x68656164, 0x68686561, 0x686D7478,
        0x6D617870, 0x6E616D65, 0x4F532F32, 0x706F7374,
        0x63767420, 0x6670676D, 0x676C7966, 0x6C6F6361,
        0x70726570, 0x43464620, 0x564F5247, 0x45424454,
        0x45424C43, 0x67617370, 0x68646D78, 0x6B65726E,
        0x4C545348, 0x50434C54, 0x56444D58, 0x76686561,
        0x766D7478, 0x42415345, 0x47444546, 0x47504F53,
        0x47535542, 0x45425343, 0x4A535446, 0x4D415448,
        0x43424454, 0x43424C43, 0x434F4C52, 0x4350414C,
        0x53564720, 0x73626978, 0x61636E74, 0x61766172,
        0x62646174, 0x626C6F63, 0x62736C6E, 0x63766172,
        0x66647363, 0x66656174, 0x666D7478, 0x66766172,
        0x67766172, 0x68737479, 0x6A757374, 0x6C636172,
        0x6D6F7274, 0x6D6F7278, 0x6F706264, 0x70726F70,
        0x7472616B, 0x5A617066, 0x53696C66, 0x476C6174,
        0x476C6F63, 0x46656174, 0x53696C6C
    };

    internal static int GetCount()
    {
        return tags.Length;
    }

    internal static int GetTag(int index)
    {
        if (index < 0 || index >= tags.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tags[index];
    }
}
