using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrSegmenterExhaustiveTests
{
    // Small, fixed repertoire with independent byte mappings below. Supplementary
    // characters are tokens so neither corpus generation nor the oracle can split a surrogate pair.
    static readonly string[] Alphabet = ["1", "A", "a", "é", "漢", "€", "😀"];
    static readonly int[] Assignments = [3, 1, 20, 26, 25, 27, 29, 33, 34, 35];

    [TestCaseSource(nameof(Profiles))]
    public void EveryStringThroughFourScalarsHasMinimumCost(Profile profile, bool reversePriorities)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        int[] priorities = reversePriorities ? Assignments.Reverse().ToArray() : [];
        int count = 0;
        foreach (string[] scalars in Strings(4))
        {
            Compare(segmenter, scalars, profile, priorities);
            count++;
        }
        Assert.That(count, Is.EqualTo(2801)); // 1 + 7 + 49 + 343 + 2401.
    }

    [TestCase("a12345678a", 1)]
    [TestCase("a12345678a", 10)]
    [TestCase("a12345678a", 27)]
    [TestCase("ééééé😀ééééé", 1)]
    [TestCase("😀1234567890😀", 1)]
    [TestCase("a漢漢漢漢a", 1)]
    public void LongerModeAndEciTransitionsMatchCompletePartitionSearch(string text, int version)
    {
        // These extend the exhaustive short corpus across mode-switch break-even points.
        // They still enumerate every scalar boundary, rather than only the visible runs.
        string[] scalars = text.EnumerateRunes().Select(r => r.ToString()).ToArray();
        Profile profile = Model2(version);
        Compare(new QrSegmenter(), scalars, profile, []);
    }

    [TestCase(1, "11111111")]
    [TestCase(2, "AAAAAAAA")]
    [TestCase(3, "aaaaaaaaaaaaaaaa")]
    [TestCase(3, "漢漢漢漢漢漢漢漢")]
    public void ForcedCountFieldSplitsMatchCompletePartitionSearch(int version, string text)
    {
        int[][] widths = [[3, 0, 0, 0], [4, 3, 0, 0], [5, 4, 4, 3]];
        Profile profile = new Profile(QrSymbolKind.Micro, version, version - 1, widths[version - 1], false);
        Compare(new QrSegmenter(), text.EnumerateRunes().Select(r => r.ToString()).ToArray(), profile, []);
    }

    static IEnumerable<TestCaseData> Profiles()
    {
        List<Profile> profiles = [Model2(1), Model2(9), Model2(10), Model2(26), Model2(27), Model2(40)];
        profiles.AddRange(new[]
        {
            new Profile(QrSymbolKind.Micro, 1, 0, [3, 0, 0, 0], false),
            new Profile(QrSymbolKind.Micro, 2, 1, [4, 3, 0, 0], false),
            new Profile(QrSymbolKind.Micro, 3, 2, [5, 4, 4, 3], false),
            new Profile(QrSymbolKind.Micro, 4, 3, [6, 5, 5, 4], false),
            new Profile(QrSymbolKind.rMQR, 0, 3, [4, 3, 3, 2], true),
            new Profile(QrSymbolKind.rMQR, 31, 3, [9, 8, 8, 7], true),
            new Profile(QrSymbolKind.LegacyModel1, 1, 4, [10, 9, 8, 8], false),
            new Profile(QrSymbolKind.LegacyModel1, 10, 4, [12, 11, 16, 10], false)
        });
        foreach (Profile profile in profiles)
            foreach (bool reverse in new[] { false, true })
                yield return new TestCaseData(profile, reverse).SetName($"Exhaustive_{profile.Kind}_{profile.Version}_ReversePriorities_{reverse}");
    }

    static Profile Model2(int version) => new Profile(QrSymbolKind.Model2, version, 4,
        version < 10 ? [10, 9, 8, 8] : version < 27 ? [12, 11, 16, 10] : [14, 13, 16, 12], true);

    static IEnumerable<string[]> Strings(int maximumLength)
    {
        yield return [];
        for (int length = 1; length <= maximumLength; length++)
        {
            int combinations = (int)Math.Pow(Alphabet.Length, length);
            for (int number = 0; number < combinations; number++)
            {
                string[] scalars = new string[length];
                int remaining = number;
                for (int index = 0; index < length; index++)
                {
                    scalars[index] = Alphabet[remaining % Alphabet.Length];
                    remaining /= Alphabet.Length;
                }
                yield return scalars;
            }
        }
    }

    static void Compare(IQrSegmenter segmenter, string[] scalars, Profile profile, int[] priorities)
    {
        string text = string.Concat(scalars);
        string context = $"{profile.Kind} v{profile.Version}, text={text}, scalars={scalars.Length}, priorities=[{string.Join(',', priorities)}]";
        int minimum = MinimumBits(scalars, profile);
        if (minimum == int.MaxValue)
        {
            Assert.Throws<QrInputException>(() => segmenter.OptimizeTextSegments(text, profile.Kind, profile.Version,
                priorities, false, -1, null, 100, 1000), context);
            return;
        }
        QrSegment[] segments = segmenter.OptimizeTextSegments(text, profile.Kind, profile.Version,
            priorities, false, -1, null, 100, 1000);
        int actual = InspectOutput(segments, scalars, profile, context);
        Assert.That(actual, Is.EqualTo(minimum), context);
    }

    static int MinimumBits(string[] scalars, Profile profile)
    {
        // Precompute legal segments only. The search itself deliberately has no
        // memoized position/ECI states, recurrence table, or production cost helpers.
        List<Choice>[] choices = new List<Choice>[scalars.Length];
        for (int start = 0; start < scalars.Length; start++)
        {
            choices[start] = [];
            for (int end = scalars.Length; end > start; end--)
            {
                string[] part = scalars[start..end];
                for (int mode = 0; mode < 4; mode++)
                {
                    if (profile.Widths[mode] == 0) continue;
                    foreach (int assignment in mode == 2 ? (profile.Eci ? Assignments : [3]) : new[] { 3 })
                    {
                        int count = part.Length;
                        if (mode == 2)
                        {
                            byte[]? bytes = Encode(part, assignment);
                            if (bytes == null) continue;
                            count = bytes.Length;
                        }
                        else if (!CanUseMode(part, mode)) continue;
                        if (count >= 1 << profile.Widths[mode]) continue;
                        choices[start].Add(new Choice(end, mode, assignment,
                            profile.ModeWidth + profile.Widths[mode] + PayloadBits(mode, count)));
                    }
                }
            }
        }
        int best = int.MaxValue;
        void Visit(int position, int activeEci, int bits)
        {
            if (position == scalars.Length) { best = Math.Min(best, bits); return; }
            if (bits >= best) return; // All remaining segments have positive cost.
            foreach (Choice choice in choices[position])
            {
                int nextEci = choice.Mode == 2 ? choice.Assignment : activeEci;
                // Moving an ECI immediately before the byte segment it affects
                // preserves cost, so standalone ECI-only branches cannot improve the minimum.
                int switchBits = nextEci == activeEci ? 0 : profile.ModeWidth + 8;
                Visit(choice.End, nextEci, bits + choice.Bits + switchBits);
            }
        }
        Visit(0, 3, 0);
        return best;
    }

    static int InspectOutput(QrSegment[] segments, string[] scalars, Profile profile, string context)
    {
        int position = 0, activeEci = 3, bits = 0;
        bool pendingEci = false;
        foreach (QrSegment segment in segments)
        {
            if (segment.Mode == QrEncodingMode.Eci)
            {
                Assert.That(profile.Eci, Is.True, context);
                Assert.That(pendingEci, Is.False, context);
                Assert.That(Assignments, Does.Contain(segment.EciAssignment), context);
                Assert.That(segment.EciAssignment, Is.Not.EqualTo(activeEci), context);
                Assert.That(segment.Data, Is.EqualTo(new byte[] { (byte)segment.EciAssignment }), context);
                Assert.That(segment.CharacterCount, Is.Zero, context);
                activeEci = segment.EciAssignment;
                pendingEci = true;
                bits += profile.ModeWidth + 8;
                continue;
            }
            int mode = segment.Mode switch
            {
                QrEncodingMode.Numeric => 0, QrEncodingMode.Alphanumeric => 1,
                QrEncodingMode.Byte => 2, QrEncodingMode.Kanji => 3, _ => -1
            };
            Assert.That(mode, Is.InRange(0, 3), context);
            Assert.That(profile.Widths[mode], Is.GreaterThan(0), context);
            Assert.That(segment.Text, Is.Not.Null.And.Not.Empty, context);
            string[] part = segment.Text!.EnumerateRunes().Select(r => r.ToString()).ToArray();
            Assert.That(part, Is.EqualTo(scalars.Skip(position).Take(part.Length).ToArray()), context);
            position += part.Length;
            if (pendingEci) Assert.That(mode, Is.EqualTo(2), context);
            pendingEci = false;
            byte[]? expectedBytes = mode == 2 ? Encode(part, activeEci)
                : mode == 3 ? Encode(part, 20) : Encoding.ASCII.GetBytes(string.Concat(part));
            Assert.That(expectedBytes, Is.Not.Null, context);
            Assert.That(segment.Data, Is.EqualTo(expectedBytes), context);
            int count = mode == 2 ? expectedBytes!.Length : part.Length;
            Assert.That(segment.CharacterCount, Is.EqualTo(count), context);
            Assert.That(count, Is.LessThan(1 << profile.Widths[mode]), context);
            if (mode == 2) Assert.That(segment.EciAssignment, Is.EqualTo(activeEci), context);
            else Assert.That(CanUseMode(part, mode), Is.True, context);
            bits += profile.ModeWidth + profile.Widths[mode] + PayloadBits(mode, count);
        }
        Assert.That(pendingEci, Is.False, context);
        Assert.That(position, Is.EqualTo(scalars.Length), context);
        return bits;
    }

    static bool CanUseMode(string[] scalars, int mode) => mode switch
    {
        0 => scalars.All(s => s.Length == 1 && s[0] >= '0' && s[0] <= '9'),
        1 => scalars.All(s => s.Length == 1 && "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:".Contains(s[0])),
        3 => scalars.All(s => s == "漢"),
        _ => false
    };

    static int PayloadBits(int mode, int count) => mode switch
    {
        0 => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 == 1 ? 4 : 7),
        1 => count / 2 * 11 + count % 2 * 6, 2 => count * 8, 3 => count * 13,
        _ => throw new AssertionException("Unexpected mode")
    };

    static byte[]? Encode(string[] scalars, int assignment)
    {
        string text = string.Concat(scalars);
        switch (assignment)
        {
            case 26: return new UTF8Encoding(false, true).GetBytes(text);
            case 25: return new UnicodeEncoding(true, false, true).GetBytes(text);
            case 33: return new UnicodeEncoding(false, false, true).GetBytes(text);
            case 34: return new UTF32Encoding(true, false, true).GetBytes(text);
            case 35: return new UTF32Encoding(false, false, true).GetBytes(text);
        }
        List<byte> bytes = [];
        foreach (string scalar in scalars)
        {
            if (scalar.Length == 1 && scalar[0] <= 127) bytes.Add((byte)scalar[0]);
            else if ((assignment == 1 || assignment == 3) && scalar == "é") bytes.Add(0xe9);
            else if (assignment == 20 && scalar == "漢") bytes.AddRange([0x8a, 0xbf]);
            else if (assignment == 29 && scalar == "é") bytes.AddRange([0xa8, 0xa6]);
            else return null;
        }
        return bytes.ToArray();
    }

    public record Profile(QrSymbolKind Kind, int Version, int ModeWidth, int[] Widths, bool Eci);
    record Choice(int End, int Mode, int Assignment, int Bits);
}
