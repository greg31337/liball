using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2Rfc9106VectorTests
{
    [TestCase(
        Argon2Variant.D,
        "b8819791a0359660bb7709c85fa48f04d5d82c05c5f215ccdb885491717cf757" +
        "082c28b951be381410b5fc2eb7274033b9fdc7ae672bcaac5d179097a4af3109",
        "512b391b6f1162975371d30919734294f868e3be3984f3c1a13a4db9fabe4acb")]
    [TestCase(
        Argon2Variant.I,
        "c46065815276a0b3e731731c902f1fd80cf776907fbb7b6a5ca72e7b56011fee" +
        "ca446c86dd75b9469a5e6879dec4b72d0863fb939b982e5f397cc7d164fddaa9",
        "c814d9d1dc7f37aa13f0d77f2494bda1c8de6b016dd388d29952a4c4672b6ce8")]
    [TestCase(
        Argon2Variant.Id,
        "2889de487eb42ae500c0007ed9252f1069eadec40d5765b485de6dc2437a67b8" +
        "546a2f0acc1a0882db8fcf74714b472e94df421a5da1112ffa11434370a1e997",
        "0d640df58d78766c08c037a34a8b53c9d01ef0452d75b65eb52520e96b01e659")]
    public void PublishedVectorMatchesInputsH0EveryPassAndFinalTag(
        Argon2Variant variant,
        string expectedInitialHash,
        string expectedTag)
    {
        byte[] password = CreateBytes(32, 0x01);
        byte[] salt = CreateBytes(16, 0x02);
        byte[] secret = CreateBytes(8, 0x03);
        byte[] associatedData = CreateBytes(12, 0x04);
        Argon2Parameters parameters = CreateParameters(variant);
        byte[] initialHash = new byte[Argon2Limits.PrehashDigestLength];

        Argon2InitialHash.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            32,
            initialHash);

        Argon2Memory memory = new Argon2Memory(parameters, 3);
        InitializeFirstBlocks(initialHash, memory);
        ulong[][] expectedPassWords = GetExpectedPassWords(variant);
        int passIndex = 0;
        while (passIndex < expectedPassWords.Length)
        {
            FillPass(parameters, memory, passIndex);
            Assert.That(
                GetPublishedWords(memory),
                Is.EqualTo(expectedPassWords[passIndex]),
                "RFC 9106 intermediate words after pass " + passIndex);
            passIndex = passIndex + 1;
        }

        byte[] stagedTag = new byte[32];
        Argon2Finalization.ComputeTag(
            memory,
            new ulong[Argon2Limits.WordsPerBlock],
            stagedTag);

        IArgon2Primitive primitive = new Argon2Primitive();
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 4;
        byte[] primitiveTag = new byte[32];
        primitive.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            primitiveTag);

        Assert.Multiple(() =>
        {
            Assert.That(password, Has.Length.EqualTo(32).And.All.EqualTo(0x01));
            Assert.That(salt, Has.Length.EqualTo(16).And.All.EqualTo(0x02));
            Assert.That(secret, Has.Length.EqualTo(8).And.All.EqualTo(0x03));
            Assert.That(associatedData, Has.Length.EqualTo(12).And.All.EqualTo(0x04));
            Assert.That(ToHex(initialHash), Is.EqualTo(expectedInitialHash));
            Assert.That(ToHex(stagedTag), Is.EqualTo(expectedTag));
            Assert.That(ToHex(primitiveTag), Is.EqualTo(expectedTag));
        });
    }

    static Argon2Parameters CreateParameters(Argon2Variant variant)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = Argon2Version.Version13;
        parameters.MemorySizeKiB = 32L;
        parameters.Iterations = 3L;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static byte[] CreateBytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static void InitializeFirstBlocks(byte[] initialHash, Argon2Memory memory)
    {
        byte[] firstBlock = new byte[Argon2Limits.BlockSizeInBytes];
        byte[] secondBlock = new byte[Argon2Limits.BlockSizeInBytes];
        int laneIndex = 0;
        while (laneIndex < memory.GetLaneCount())
        {
            Argon2InitialHash.ExpandFirstBlocksForLane(
                initialHash,
                laneIndex,
                firstBlock,
                secondBlock);
            long laneStart = (long)laneIndex * memory.GetBlocksPerLane();
            memory.WriteBlockBytes(laneStart, firstBlock);
            memory.WriteBlockBytes(laneStart + 1L, secondBlock);
            laneIndex = laneIndex + 1;
        }
    }

    static void FillPass(
        Argon2Parameters parameters,
        Argon2Memory memory,
        int passIndex)
    {
        int sliceIndex = 0;
        while (sliceIndex < Argon2Limits.SynchronizationPoints)
        {
            int laneIndex = 0;
            while (laneIndex < parameters.DegreeOfParallelism)
            {
                Argon2LaneFiller.FillSegment(
                    parameters,
                    memory,
                    passIndex,
                    laneIndex,
                    sliceIndex,
                    new ulong[Argon2Limits.WordsPerBlock],
                    new ulong[Argon2Limits.WordsPerBlock],
                    new ulong[Argon2Limits.WordsPerBlock]);
                laneIndex = laneIndex + 1;
            }

            sliceIndex = sliceIndex + 1;
        }
    }

    static ulong[] GetPublishedWords(Argon2Memory memory)
    {
        Span<ulong> firstBlock = memory.GetBlock(0L);
        Span<ulong> lastBlock = memory.GetBlock(31L);
        return new ulong[]
        {
            firstBlock[0],
            firstBlock[1],
            firstBlock[2],
            firstBlock[3],
            lastBlock[124],
            lastBlock[125],
            lastBlock[126],
            lastBlock[127]
        };
    }

    static ulong[][] GetExpectedPassWords(Argon2Variant variant)
    {
        if (variant == Argon2Variant.D)
        {
            return new ulong[][]
            {
                new ulong[]
                {
                    0xDB2FEA6B2C6F5C8AUL, 0x719413BE00F82634UL,
                    0xA1E3F6DD42AA25CCUL, 0x3EA8EFD4D55AC0D1UL,
                    0x28D17914AEA9734CUL, 0x6A4622176522E398UL,
                    0x951AA08AEECB2C05UL, 0x6A6C49D2CB75D5B6UL
                },
                new ulong[]
                {
                    0xD3801200410F8C0DUL, 0x0BF9E8A6E442BA6DUL,
                    0xE2CA92FE9C541FCCUL, 0x6269FE6DB177A388UL,
                    0x9EACFCFBDB3CE0FCUL, 0x07DEDAEB0AEE71ACUL,
                    0x074435FAD91548F4UL, 0x2DBFFF23F31B5883UL
                },
                new ulong[]
                {
                    0x5F047B575C5FF4D2UL, 0xF06985DBF11C91A8UL,
                    0x89EFB2759F9A8964UL, 0x7486A73F62F9B142UL,
                    0x57CFB9D20479DA49UL, 0x4099654BC6607F69UL,
                    0xF142A1126075A5C8UL, 0xC341B3CA45C10DA5UL
                }
            };
        }

        if (variant == Argon2Variant.I)
        {
            return new ulong[][]
            {
                new ulong[]
                {
                    0xF8F9E84545DB08F6UL, 0x9B073A5C87AA2D97UL,
                    0xD1E868D75CA8D8E4UL, 0x349634174E1AEBCCUL,
                    0x975F596583745E30UL, 0xE349BDD7EDEB3092UL,
                    0xB751A689B7A83659UL, 0xC570F2AB2A86CF00UL
                },
                new ulong[]
                {
                    0xB2E4DDFCF76DC85AUL, 0x4FFD0626C89A2327UL,
                    0x4AF1440FFF212980UL, 0x1E77299C7408505BUL,
                    0xE4274FD675D1E1D6UL, 0x903FFFB7C4A14C98UL,
                    0x7E5DB55DEF471966UL, 0x421B3C6E9555B79DUL
                },
                new ulong[]
                {
                    0xAF2A8BD8482C2F11UL, 0x785442294FA55E6DUL,
                    0x9256A768529A7F96UL, 0x25A1C1F5BB953766UL,
                    0x68CF72FCCC7112B9UL, 0x91E8C6F8BB0AD70DUL,
                    0x4F59C8BD65CBB765UL, 0x71E436F035F30ED0UL
                }
            };
        }

        return new ulong[][]
        {
            new ulong[]
            {
                0x6B2E09F10671BD43UL, 0xF69F5C27918A21BEUL,
                0xDEA7810EA41290E1UL, 0x6787F7171870F893UL,
                0x377FA81666DC7F2BUL, 0x50E586398A9C39C8UL,
                0x6F732732A550924AUL, 0x81F88B28683EA8E5UL
            },
            new ulong[]
            {
                0x3653EC9D01583DF9UL, 0x69EF53A72D1E1FD3UL,
                0x35635631744AB54FUL, 0x599512E96A37AB6EUL,
                0x4D4B435CEA35CAA6UL, 0xC582210D99AD1359UL,
                0xD087971B36FD6D77UL, 0xA55222A93754C692UL
            },
            new ulong[]
            {
                0x942363968CE597A4UL, 0xA22448C0BDAD5760UL,
                0xA5F80662B6FA8748UL, 0xA0F9B9CE392F719FUL,
                0xD723359B485F509BUL, 0xCB78824F42375111UL,
                0x35BC8CC6E83B1875UL, 0x0B012846A40F346AUL
            }
        };
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
