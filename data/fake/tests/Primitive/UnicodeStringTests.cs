using fake;

namespace fake.Tests;

[TestFixture]
public class UnicodeStringTests
{
    [Test]
    public void FullRangeScalarsAreDeterministicAndNeverSurrogates()
    {
        PrimitiveProvider first = Fake.Create(3401).Primitive();
        PrimitiveProvider second = Fake.Create(3401).Primitive();
        bool foundBmp = false;
        bool foundSupplementary = false;

        for (int index = 0; index < 20000; index++)
        {
            int actual = first.UnicodeScalar();

            Assert.That(actual, Is.EqualTo(second.UnicodeScalar()));
            Assert.That(actual, Is.InRange(0, 0x10FFFF));
            Assert.That(actual < 0xD800 || actual > 0xDFFF, Is.True);
            foundBmp = foundBmp || actual < 0x10000;
            foundSupplementary = foundSupplementary || actual >= 0x10000;
        }

        Assert.That(foundBmp, Is.True);
        Assert.That(foundSupplementary, Is.True);
    }

    [Test]
    public void ScalarBoundsSkipTheSurrogateGapUniformly()
    {
        PrimitiveProvider primitive = Fake.Create(3402).Primitive();
        bool foundBeforeGap = false;
        bool foundAfterGap = false;

        for (int index = 0; index < 10000; index++)
        {
            int scalar = primitive.UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(0xD7F0, 0xE010, true);

            Assert.That(scalar, Is.InRange(0xD7F0, 0xE010));
            Assert.That(scalar < 0xD800 || scalar > 0xDFFF, Is.True);
            foundBeforeGap = foundBeforeGap || scalar <= 0xD7FF;
            foundAfterGap = foundAfterGap || scalar >= 0xE000;
        }

        Assert.That(foundBeforeGap, Is.True);
        Assert.That(foundAfterGap, Is.True);
        Assert.That(primitive.UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(0xD7FF, 0xD7FF, true), Is.EqualTo(0xD7FF));
        Assert.That(primitive.UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(0xE000, 0xE000, true), Is.EqualTo(0xE000));
        Assert.That(primitive.UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(0x10FFFF, 0x10FFFF, true), Is.EqualTo(0x10FFFF));
    }

    [Test]
    public void UnicodeStringsCountScalarsAndProduceWellFormedUtf16()
    {
        PrimitiveProvider first = Fake.Create(3403).Primitive();
        PrimitiveProvider second = Fake.Create(3403).Primitive();

        string actual = first.Unicode(10000);

        Assert.That(actual, Is.EqualTo(second.Unicode(10000)));
        AssertWellFormed(actual, 10000);
        Assert.That(actual.Length, Is.GreaterThan(10000));
        Assert.That(actual.Length, Is.LessThanOrEqualTo(20000));
    }

    [Test]
    public void BoundedUnicodeStringsHandleBmpAndSupplementaryScalarsExactly()
    {
        PrimitiveProvider primitive = Fake.Create(3404).Primitive();

        string bmp = primitive.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(50, 'A', 'A', true);
        string supplementary = primitive.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(50, 0x10000, 0x10000, true);
        string maximum = primitive.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(1, 0x10FFFF, 0x10FFFF, true);

        Assert.That(bmp, Is.EqualTo(new string('A', 50)));
        Assert.That(supplementary, Has.Length.EqualTo(100));
        Assert.That(maximum, Has.Length.EqualTo(2));
        AssertWellFormed(supplementary, 50);
        AssertWellFormed(maximum, 1);
        Assert.That(char.ConvertToUtf32(maximum, 0), Is.EqualTo(0x10FFFF));
    }

    [Test]
    public void ScalarAndStringRangesHonorExclusiveMaximums()
    {
        PrimitiveProvider primitive = Fake.Create(3405).Primitive();

        Assert.That(primitive.UnicodeScalarFromMinimumScalarAndMaximumScalar('A', 'B'), Is.EqualTo('A'));
        Assert.That(primitive.UnicodeFromCountAndMinimumScalarAndMaximumScalar(20, 'z', '{'), Is.EqualTo(new string('z', 20)));

        for (int index = 0; index < 1000; index++)
        {
            int scalar = primitive.UnicodeScalarFromMinimumScalarAndMaximumScalar(0x10000, 0x10100);
            Assert.That(scalar, Is.InRange(0x10000, 0x100FF));
        }
    }

    [TestCase("en-US", "Latn")]
    [TestCase("ru-RU", "Cyrl")]
    [TestCase("el-GR", "Grek")]
    [TestCase("ar-EG", "Arab")]
    [TestCase("he-IL", "Hebr")]
    [TestCase("hi-IN", "Deva")]
    [TestCase("th-TH", "Thai")]
    [TestCase("bn-BD", "Beng")]
    [TestCase("am-ET", "Ethi")]
    [TestCase("bo-Tibt-CN", "Tibt")]
    [TestCase("dv-MV", "Thaa")]
    [TestCase("hy-AM", "Armn")]
    [TestCase("ka-GE", "Geor")]
    [TestCase("zh-CN", "Hans")]
    [TestCase("zh-TW", "Hant")]
    [TestCase("zh-Hani", "Hani")]
    [TestCase("ja-JP", "Jpan")]
    [TestCase("ko-KR", "Kore")]
    [TestCase("sr-Latn-RS", "Latn")]
    public void LocaleScriptUsesExplicitOrRegistryDefaultScript(string locale, string script)
    {
        PrimitiveProvider primitive = Fake.Create(3406).Primitive();
        string expectedAlphabet = ScriptAlphabet(script);

        string actual = primitive.LocaleScriptFromCountAndLocale(5000, locale);

        Assert.That(actual, Has.Length.EqualTo(5000));
        Assert.That(actual.All(expectedAlphabet.Contains), Is.True);
        Assert.That(actual.Distinct().Order(), Is.EqualTo(expectedAlphabet.Distinct().Order()));
        AssertWellFormed(actual, 5000);
    }

    [Test]
    public void ConfiguredLocaleScriptMatchesExplicitResolvedScript()
    {
        PrimitiveProvider configured = Fake.Create("ru-RU", 3407L).Primitive();
        PrimitiveProvider explicitScript = Fake.Create("en-US", 3407L).Primitive();

        Assert.That(configured.LocaleScript(1000), Is.EqualTo(explicitScript.UnicodeFromCountAndScript(1000, "Cyrl")));
    }

    [Test]
    public void ScriptSubtagsAreCaseInsensitiveAndDeterministic()
    {
        PrimitiveProvider first = Fake.Create(3408).Primitive();
        PrimitiveProvider second = Fake.Create(3408).Primitive();

        string actual = first.UnicodeFromCountAndScript(1000, "latn");

        Assert.That(actual, Is.EqualTo(second.UnicodeFromCountAndScript(1000, "Latn")));
        Assert.That(actual, Does.Match("^[A-Za-z]{1000}$"));
    }

    [Test]
    public void InvalidScalarRangesFailBeforeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3409).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3409).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalar(-1, 1)).ParamName, Is.EqualTo("minimumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalar(0, 0x110000)).ParamName, Is.EqualTo("maximumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalar(0xD800, 0xE000)).ParamName, Is.EqualTo("minimumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalar(0, 0xDFFF)).ParamName, Is.EqualTo("maximumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalar(10, 10)).ParamName, Is.EqualTo("maximumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(11, 10, true)).ParamName, Is.EqualTo("maximumScalar"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Unicode(-1)).ParamName, Is.EqualTo("count"));
        });

        Assert.That(actual.UnicodeFromCountAndMinimumScalarAndMaximumScalar(100, 'A', 'Z'), Is.EqualTo(expectedPosition.UnicodeFromCountAndMinimumScalarAndMaximumScalar(100, 'A', 'Z')));
    }

    [Test]
    public void InvalidOrAmbiguousScriptsFailBeforeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3410).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3410).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => actual.UnicodeFromCountAndScript(1, (string)null)).ParamName, Is.EqualTo("script"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.UnicodeFromCountAndScript(1, " ")).ParamName, Is.EqualTo("script"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.UnicodeFromCountAndScript(1, "Nope")).ParamName, Is.EqualTo("script"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.UnicodeFromCountAndScript(1, "Zyyy")).ParamName, Is.EqualTo("script"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.LocaleScriptFromCountAndLocale(1, "az-AZ")).ParamName, Is.EqualTo("locale"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.LocaleScriptFromCountAndLocale(1, "x-private")).ParamName, Is.EqualTo("locale"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.LocaleScriptFromCountAndLocale(1, "not_a_locale")).ParamName, Is.EqualTo("locale"));
        });

        Assert.That(actual.UnicodeFromCountAndScript(100, "Latn"), Is.EqualTo(expectedPosition.UnicodeFromCountAndScript(100, "Latn")));
    }

    [Test]
    public void UnicodeGenerationHonorsConfiguredOutputLimitBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(3411L);
        options.MaximumOutputSize = 5;
        PrimitiveProvider actual = Fake.Create(options).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(options).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Unicode(3)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(3, 0x10000, 0x10000, true)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UnicodeFromCountAndScript(6, "Latn")).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.LocaleScript(6)).ParamName, Is.EqualTo("count"));
        });

        Assert.That(actual.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(5, 'A', 'A', true), Is.EqualTo("AAAAA"));
        expectedPosition.UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(5, 'A', 'A', true);
        Assert.That(actual.UnicodeFromCountAndMinimumScalarAndMaximumScalar(5, 'A', 'Z'), Is.EqualTo(expectedPosition.UnicodeFromCountAndMinimumScalarAndMaximumScalar(5, 'A', 'Z')));
    }

    [Test]
    public void EmptyUnicodeOperationsDoNotConsumeEntropy()
    {
        PrimitiveProvider actual = Fake.Create(3412).Primitive();
        PrimitiveProvider expectedPosition = Fake.Create(3412).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Unicode(0), Is.Empty);
            Assert.That(actual.UnicodeFromCountAndMinimumScalarAndMaximumScalar(0, 'A', 'Z'), Is.Empty);
            Assert.That(actual.UnicodeFromCountAndScript(0, "Latn"), Is.Empty);
            Assert.That(actual.LocaleScript(0), Is.Empty);
            Assert.That(actual.LocaleScriptFromCountAndLocale(0, "zh-TW"), Is.Empty);
        });

        Assert.That(actual.UnicodeFromCountAndMinimumScalarAndMaximumScalar(100, 'A', 'Z'), Is.EqualTo(expectedPosition.UnicodeFromCountAndMinimumScalarAndMaximumScalar(100, 'A', 'Z')));
    }

    static void AssertWellFormed(string value, int expectedScalarCount)
    {
        int index = 0;
        int scalarCount = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                Assert.That(index + 1, Is.LessThan(value.Length));
                Assert.That(char.IsLowSurrogate(value[index + 1]), Is.True);
                index += 2;
            }
            else
            {
                Assert.That(char.IsLowSurrogate(character), Is.False);
                index++;
            }
            scalarCount++;
        }
        Assert.That(scalarCount, Is.EqualTo(expectedScalarCount));
    }

    static string ScriptAlphabet(string script)
    {
        return script switch
        {
            "Latn" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            "Cyrl" => "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя",
            "Grek" => "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρσςτυφχψω",
            "Arab" => "ابتثجحخدذرزسشصضطظعغفقكلمنهويءأإآؤئىة",
            "Hebr" => "אבגדהוזחטיךכלםמןנסעףפץצקרשת",
            "Deva" => "अआइईउऊऋएऐओऔकखगघङचछजझञटठडढणतथदधनपफबभमयरलवशषसह",
            "Thai" => "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรลวศษสหฬอฮ",
            "Beng" => "অআইঈউঊঋএঐওঔকখগঘঙচছজঝঞটঠডঢণতথদধনপফবভমযরলশষসহ",
            "Ethi" => "ሀለሐመሠረሰቀበተነአከወዘየደገጠጰጸፀፈፐ",
            "Tibt" => "ཀཁགངཅཆཇཉཏཐདནཔཕབམཙཚཛཝཞཟའཡརལཤསཧཨ",
            "Thaa" => "ހށނރބޅކއވމފދތލގޏސޑޒޓޔޕޖޗޘޙޚޛޜޝޞޟޠޡ",
            "Armn" => "ԱԲԳԴԵԶԷԸԹԺԻԼԽԾԿՀՁՂՃՄՅՆՇՈՉՊՋՌՍՎՏՐՑՒՓՔՕՖաբգդեզէըթժիլխծկհձղճմյնշոչպջռսվտրցւփքօֆ",
            "Geor" => "აბგდევზთიკლმნოპჟრსტუფქღყშჩცძწჭხჯჰ",
            "Hans" => "的一是不了有和人这中大为上个国我以要他时来用们生到作地于出就分对成会可主发年动同工也能下过子说产种面而方后多定行学法所民得经",
            "Hant" => "的一是不了有和人這中大為上個國我以要他時來用們生到作地於出就分對成會可主發年動同工也能下過子說產種面而方後多定行學法所民得經",
            "Hani" => "一二三四五六七八九十天地人日月山水火木金土大小上下左右中本正生年",
            "Jpan" => "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん",
            "Kore" => "가나다라마바사아자차카타파하거너더러머버서어저처커터퍼허고노도로모보소오조초코토포호",
            _ => throw new AssertionException("Missing expected script alphabet for " + script)
        };
    }
}
