namespace unicode;

/// <summary>
/// Selects how malformed Unicode input is handled during scalar and byte conversion.
/// </summary>
public enum UnicodeMalformedInputPolicy
{
    /// <summary>
    /// Replaces each malformed input subsequence with U+FFFD.
    /// </summary>
    Replace = 0,
    /// <summary>
    /// Rejects malformed input by throwing an encoding or decoding fallback exception.
    /// </summary>
    Strict = 1,
}

/// <summary>
/// Selects a standard Unicode byte encoding and byte order.
/// </summary>
public enum UnicodeByteEncodingKind
{
    /// <summary>
    /// UTF-8 without an emitted byte-order mark.
    /// </summary>
    Utf8 = 0,
    /// <summary>
    /// Little-endian UTF-16 without an emitted byte-order mark.
    /// </summary>
    Utf16Le = 1,
    /// <summary>
    /// Big-endian UTF-16 without an emitted byte-order mark.
    /// </summary>
    Utf16Be = 2,
    /// <summary>
    /// Little-endian UTF-32 without an emitted byte-order mark.
    /// </summary>
    Utf32Le = 3,
    /// <summary>
    /// Big-endian UTF-32 without an emitted byte-order mark.
    /// </summary>
    Utf32Be = 4,
}

/// <summary>
/// Selects one of the four standard Unicode normalization forms.
/// </summary>
public enum UnicodeNormalizationForm
{
    /// <summary>
    /// Canonical decomposition followed by canonical composition (NFC).
    /// </summary>
    Nfc = 0,
    /// <summary>
    /// Fully ordered canonical decomposition (NFD).
    /// </summary>
    Nfd = 1,
    /// <summary>
    /// Compatibility decomposition followed by canonical composition (NFKC).
    /// </summary>
    Nfkc = 2,
    /// <summary>
    /// Fully ordered compatibility decomposition (NFKD).
    /// </summary>
    Nfkd = 3,
}

/// <summary>
/// Reports whether text is known to satisfy a normalization form without always normalizing it.
/// </summary>
public enum UnicodeQuickCheckResult
{
    /// <summary>
    /// The text definitely satisfies the selected normalization form.
    /// </summary>
    Yes = 0,
    /// <summary>
    /// The text definitely does not satisfy the selected normalization form.
    /// </summary>
    No = 1,
    /// <summary>
    /// The text may require normalization; perform full normalization to decide.
    /// </summary>
    Maybe = 2,
}

/// <summary>
/// Identifies a Unicode General_Category value.
/// </summary>
public enum UnicodeGeneralCategory
{
    /// <summary>
    /// Represents the Unicode other Not Assigned value.
    /// </summary>
    OtherNotAssigned = 0,
    /// <summary>
    /// Represents the Unicode uppercase Letter value.
    /// </summary>
    UppercaseLetter = 1,
    /// <summary>
    /// Represents the Unicode lowercase Letter value.
    /// </summary>
    LowercaseLetter = 2,
    /// <summary>
    /// Represents the Unicode titlecase Letter value.
    /// </summary>
    TitlecaseLetter = 3,
    /// <summary>
    /// Represents the Unicode modifier Letter value.
    /// </summary>
    ModifierLetter = 4,
    /// <summary>
    /// Represents the Unicode other Letter value.
    /// </summary>
    OtherLetter = 5,
    /// <summary>
    /// Represents the Unicode nonspacing Mark value.
    /// </summary>
    NonspacingMark = 6,
    /// <summary>
    /// Represents the Unicode spacing Mark value.
    /// </summary>
    SpacingMark = 7,
    /// <summary>
    /// Represents the Unicode enclosing Mark value.
    /// </summary>
    EnclosingMark = 8,
    /// <summary>
    /// Represents the Unicode decimal Number value.
    /// </summary>
    DecimalNumber = 9,
    /// <summary>
    /// Represents the Unicode letter Number value.
    /// </summary>
    LetterNumber = 10,
    /// <summary>
    /// Represents the Unicode other Number value.
    /// </summary>
    OtherNumber = 11,
    /// <summary>
    /// Represents the Unicode connector Punctuation value.
    /// </summary>
    ConnectorPunctuation = 12,
    /// <summary>
    /// Represents the Unicode dash Punctuation value.
    /// </summary>
    DashPunctuation = 13,
    /// <summary>
    /// Represents the Unicode open Punctuation value.
    /// </summary>
    OpenPunctuation = 14,
    /// <summary>
    /// Represents the Unicode close Punctuation value.
    /// </summary>
    ClosePunctuation = 15,
    /// <summary>
    /// Represents the Unicode initial Punctuation value.
    /// </summary>
    InitialPunctuation = 16,
    /// <summary>
    /// Represents the Unicode final Punctuation value.
    /// </summary>
    FinalPunctuation = 17,
    /// <summary>
    /// Represents the Unicode other Punctuation value.
    /// </summary>
    OtherPunctuation = 18,
    /// <summary>
    /// Represents the Unicode math Symbol value.
    /// </summary>
    MathSymbol = 19,
    /// <summary>
    /// Represents the Unicode currency Symbol value.
    /// </summary>
    CurrencySymbol = 20,
    /// <summary>
    /// Represents the Unicode modifier Symbol value.
    /// </summary>
    ModifierSymbol = 21,
    /// <summary>
    /// Represents the Unicode other Symbol value.
    /// </summary>
    OtherSymbol = 22,
    /// <summary>
    /// Represents the Unicode space Separator value.
    /// </summary>
    SpaceSeparator = 23,
    /// <summary>
    /// Represents the Unicode line Separator value.
    /// </summary>
    LineSeparator = 24,
    /// <summary>
    /// Represents the Unicode paragraph Separator value.
    /// </summary>
    ParagraphSeparator = 25,
    /// <summary>
    /// Represents the Unicode control value.
    /// </summary>
    Control = 26,
    /// <summary>
    /// Represents the Unicode format value.
    /// </summary>
    Format = 27,
    /// <summary>
    /// Represents the Unicode surrogate value.
    /// </summary>
    Surrogate = 28,
    /// <summary>
    /// Represents the Unicode private Use value.
    /// </summary>
    PrivateUse = 29,
}

/// <summary>
/// Identifies the writing system associated with a Unicode scalar.
/// </summary>
/// <remarks>
/// This enum contains the scripts used by the library's identifier, security, and PDF APIs. Other Unicode Script values are reported as Unknown.
/// </remarks>
public enum UnicodeScript
{
    /// <summary>
    /// Represents the Unicode unknown value.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the Unicode common value.
    /// </summary>
    Common = 1,
    /// <summary>
    /// Represents the Unicode inherited value.
    /// </summary>
    Inherited = 2,
    /// <summary>
    /// Represents the Unicode latin value.
    /// </summary>
    Latin = 3,
    /// <summary>
    /// Represents the Unicode greek value.
    /// </summary>
    Greek = 4,
    /// <summary>
    /// Represents the Unicode cyrillic value.
    /// </summary>
    Cyrillic = 5,
    /// <summary>
    /// Represents the Unicode hebrew value.
    /// </summary>
    Hebrew = 6,
    /// <summary>
    /// Represents the Unicode arabic value.
    /// </summary>
    Arabic = 7,
    /// <summary>
    /// Represents the Unicode devanagari value.
    /// </summary>
    Devanagari = 8,
    /// <summary>
    /// Represents the Unicode hiragana value.
    /// </summary>
    Hiragana = 9,
    /// <summary>
    /// Represents the Unicode katakana value.
    /// </summary>
    Katakana = 10,
    /// <summary>
    /// Represents the Unicode han value.
    /// </summary>
    Han = 11,
    /// <summary>
    /// Represents the Unicode hangul value.
    /// </summary>
    Hangul = 12,
    /// <summary>Represents the Unicode Armenian value.</summary>
    Armenian = 13,
    /// <summary>Represents the Unicode Bengali value.</summary>
    Bengali = 14,
    /// <summary>Represents the Unicode Ethiopic value.</summary>
    Ethiopic = 15,
    /// <summary>Represents the Unicode Georgian value.</summary>
    Georgian = 16,
    /// <summary>Represents the Unicode Grantha value.</summary>
    Grantha = 17,
    /// <summary>Represents the Unicode Gujarati value.</summary>
    Gujarati = 18,
    /// <summary>Represents the Unicode Gurmukhi value.</summary>
    Gurmukhi = 19,
    /// <summary>Represents the Unicode Kannada value.</summary>
    Kannada = 20,
    /// <summary>Represents the Unicode Khmer value.</summary>
    Khmer = 21,
    /// <summary>Represents the Unicode Lao value.</summary>
    Lao = 22,
    /// <summary>Represents the Unicode Malayalam value.</summary>
    Malayalam = 23,
    /// <summary>Represents the Unicode Myanmar value.</summary>
    Myanmar = 24,
    /// <summary>Represents the Unicode Oriya value.</summary>
    Oriya = 25,
    /// <summary>Represents the Unicode Sinhala value.</summary>
    Sinhala = 26,
    /// <summary>Represents the Unicode Tamil value.</summary>
    Tamil = 27,
    /// <summary>Represents the Unicode Telugu value.</summary>
    Telugu = 28,
    /// <summary>Represents the Unicode Thaana value.</summary>
    Thaana = 29,
    /// <summary>Represents the Unicode Thai value.</summary>
    Thai = 30,
    /// <summary>Represents the Unicode Tibetan value.</summary>
    Tibetan = 31,
}

/// <summary>
/// Identifies the Unicode block containing a scalar.
/// </summary>
/// <remarks>
/// This enum contains the blocks currently surfaced by the public API. Scalars in other Unicode blocks are reported as NoBlock.
/// </remarks>
public enum UnicodeBlock
{
    /// <summary>
    /// Represents the Unicode no Block value.
    /// </summary>
    NoBlock = 0,
    /// <summary>
    /// Represents the Unicode basic Latin value.
    /// </summary>
    BasicLatin = 1,
    /// <summary>
    /// Represents the Unicode latin1 Supplement value.
    /// </summary>
    Latin1Supplement = 2,
    /// <summary>
    /// Represents the Unicode combining Diacritical Marks value.
    /// </summary>
    CombiningDiacriticalMarks = 3,
    /// <summary>
    /// Represents the Unicode greek And Coptic value.
    /// </summary>
    GreekAndCoptic = 4,
    /// <summary>
    /// Represents the Unicode cyrillic value.
    /// </summary>
    Cyrillic = 5,
    /// <summary>
    /// Represents the Unicode hebrew value.
    /// </summary>
    Hebrew = 6,
    /// <summary>
    /// Represents the Unicode arabic value.
    /// </summary>
    Arabic = 7,
    /// <summary>
    /// Represents the Unicode devanagari value.
    /// </summary>
    Devanagari = 8,
    /// <summary>
    /// Represents the Unicode hiragana value.
    /// </summary>
    Hiragana = 9,
    /// <summary>
    /// Represents the Unicode katakana value.
    /// </summary>
    Katakana = 10,
    /// <summary>
    /// Represents the Unicode cjk Unified Ideographs value.
    /// </summary>
    CjkUnifiedIdeographs = 11,
    /// <summary>
    /// Represents the Unicode hangul Syllables value.
    /// </summary>
    HangulSyllables = 12,
    /// <summary>
    /// Represents the Unicode private Use Area value.
    /// </summary>
    PrivateUseArea = 13,
    /// <summary>
    /// Represents the Unicode specials value.
    /// </summary>
    Specials = 14,
}

/// <summary>
/// Identifies a scalar's Unicode bidirectional class.
/// </summary>
public enum UnicodeBidiClass
{
    /// <summary>
    /// Represents the Unicode left To Right value.
    /// </summary>
    LeftToRight = 0,
    /// <summary>
    /// Represents the Unicode right To Left value.
    /// </summary>
    RightToLeft = 1,
    /// <summary>
    /// Represents the Unicode arabic Letter value.
    /// </summary>
    ArabicLetter = 2,
    /// <summary>
    /// Represents the Unicode european Number value.
    /// </summary>
    EuropeanNumber = 3,
    /// <summary>
    /// Represents the Unicode european Separator value.
    /// </summary>
    EuropeanSeparator = 4,
    /// <summary>
    /// Represents the Unicode european Terminator value.
    /// </summary>
    EuropeanTerminator = 5,
    /// <summary>
    /// Represents the Unicode arabic Number value.
    /// </summary>
    ArabicNumber = 6,
    /// <summary>
    /// Represents the Unicode common Separator value.
    /// </summary>
    CommonSeparator = 7,
    /// <summary>
    /// Represents the Unicode nonspacing Mark value.
    /// </summary>
    NonspacingMark = 8,
    /// <summary>
    /// Represents the Unicode boundary Neutral value.
    /// </summary>
    BoundaryNeutral = 9,
    /// <summary>
    /// Represents the Unicode paragraph Separator value.
    /// </summary>
    ParagraphSeparator = 10,
    /// <summary>
    /// Represents the Unicode segment Separator value.
    /// </summary>
    SegmentSeparator = 11,
    /// <summary>
    /// Represents the Unicode white Space value.
    /// </summary>
    WhiteSpace = 12,
    /// <summary>
    /// Represents the Unicode other Neutral value.
    /// </summary>
    OtherNeutral = 13,
}

/// <summary>
/// Identifies a scalar's East_Asian_Width value.
/// </summary>
public enum UnicodeEastAsianWidth
{
    /// <summary>
    /// Represents the Unicode neutral value.
    /// </summary>
    Neutral = 0,
    /// <summary>
    /// Represents the Unicode ambiguous value.
    /// </summary>
    Ambiguous = 1,
    /// <summary>
    /// Represents the Unicode halfwidth value.
    /// </summary>
    Halfwidth = 2,
    /// <summary>
    /// Represents the Unicode fullwidth value.
    /// </summary>
    Fullwidth = 3,
    /// <summary>
    /// Represents the Unicode narrow value.
    /// </summary>
    Narrow = 4,
    /// <summary>
    /// Represents the Unicode wide value.
    /// </summary>
    Wide = 5,
}

/// <summary>
/// Selects a supported binary Unicode property.
/// </summary>
public enum UnicodeBinaryProperty
{
    /// <summary>
    /// Represents the Unicode white Space value.
    /// </summary>
    WhiteSpace = 0,
    /// <summary>
    /// Represents the Unicode default Ignorable Code Point value.
    /// </summary>
    DefaultIgnorableCodePoint = 1,
    /// <summary>
    /// Represents the Unicode uppercase value.
    /// </summary>
    Uppercase = 2,
    /// <summary>
    /// Represents the Unicode lowercase value.
    /// </summary>
    Lowercase = 3,
    /// <summary>
    /// Represents the Unicode alphabetic value.
    /// </summary>
    Alphabetic = 4,
    /// <summary>
    /// Represents the Unicode emoji value.
    /// </summary>
    Emoji = 5,
    /// <summary>
    /// Represents the Unicode emoji Presentation value.
    /// </summary>
    EmojiPresentation = 6,
    /// <summary>
    /// Represents the Unicode extended Pictographic value.
    /// </summary>
    ExtendedPictographic = 7,
    /// <summary>
    /// Represents the Unicode emoji Component value.
    /// </summary>
    EmojiComponent = 8,
    /// <summary>
    /// Represents the Unicode emoji Modifier value.
    /// </summary>
    EmojiModifier = 9,
    /// <summary>
    /// Represents the Unicode emoji Modifier Base value.
    /// </summary>
    EmojiModifierBase = 10,
    /// <summary>
    /// Represents the Unicode variation Selector value.
    /// </summary>
    VariationSelector = 11,
    /// <summary>
    /// Represents the Unicode noncharacter Code Point value.
    /// </summary>
    NoncharacterCodePoint = 12,
    /// <summary>
    /// Represents the Unicode pattern White Space value.
    /// </summary>
    PatternWhiteSpace = 13,
    /// <summary>
    /// Represents the Unicode pattern Syntax value.
    /// </summary>
    PatternSyntax = 14,
    /// <summary>
    /// Represents the Unicode private Use general-category property.
    /// </summary>
    PrivateUse = 15,
    /// <summary>
    /// Represents the Unicode control general-category property.
    /// </summary>
    Control = 16,
}

/// <summary>
/// Identifies the decomposition type of a Unicode scalar.
/// </summary>
public enum UnicodeDecompositionType
{
    /// <summary>No decomposition mapping is assigned.</summary>
    None = 0,
    /// <summary>The mapping is canonical.</summary>
    Canonical = 1,
    /// <summary>The mapping is a font variant.</summary>
    Font = 2,
    /// <summary>The mapping is a no-break variant.</summary>
    NoBreak = 3,
    /// <summary>The mapping is an initial presentation form.</summary>
    Initial = 4,
    /// <summary>The mapping is a medial presentation form.</summary>
    Medial = 5,
    /// <summary>The mapping is a final presentation form.</summary>
    Final = 6,
    /// <summary>The mapping is an isolated presentation form.</summary>
    Isolated = 7,
    /// <summary>The mapping is an encircled form.</summary>
    Circle = 8,
    /// <summary>The mapping is a superscript form.</summary>
    Super = 9,
    /// <summary>The mapping is a subscript form.</summary>
    Sub = 10,
    /// <summary>The mapping is a vertical form.</summary>
    Vertical = 11,
    /// <summary>The mapping is a wide form.</summary>
    Wide = 12,
    /// <summary>The mapping is a narrow form.</summary>
    Narrow = 13,
    /// <summary>The mapping is a small form.</summary>
    Small = 14,
    /// <summary>The mapping is a squared form.</summary>
    Square = 15,
    /// <summary>The mapping is a fraction form.</summary>
    Fraction = 16,
    /// <summary>The mapping is another compatibility form.</summary>
    Compat = 17,
}

/// <summary>
/// Identifies the kind of numeric value assigned to a Unicode scalar.
/// </summary>
public enum UnicodeNumericType
{
    /// <summary>No numeric value is assigned.</summary>
    None = 0,
    /// <summary>The scalar is a decimal digit.</summary>
    Decimal = 1,
    /// <summary>The scalar is a digit outside the decimal-digit class.</summary>
    Digit = 2,
    /// <summary>The scalar has another numeric value.</summary>
    Numeric = 3,
}

/// <summary>
/// Identifies the paired-bracket role used by the Unicode bidirectional algorithm.
/// </summary>
public enum UnicodeBidiPairedBracketType
{
    /// <summary>The scalar is not a paired bracket.</summary>
    None = 0,
    /// <summary>The scalar is an opening paired bracket.</summary>
    Open = 1,
    /// <summary>The scalar is a closing paired bracket.</summary>
    Close = 2,
}

/// <summary>
/// Identifies a Unicode joining type.
/// </summary>
public enum UnicodeJoiningType
{
    /// <summary>The scalar does not join.</summary>
    NonJoining = 0,
    /// <summary>The scalar joins on both sides.</summary>
    DualJoining = 1,
    /// <summary>The scalar joins to the right.</summary>
    RightJoining = 2,
    /// <summary>The scalar causes joining.</summary>
    JoinCausing = 3,
    /// <summary>The scalar is transparent for joining.</summary>
    Transparent = 4,
    /// <summary>The scalar joins to the left.</summary>
    LeftJoining = 5,
}

/// <summary>
/// Identifies a Unicode joining group.
/// </summary>
public enum UnicodeJoiningGroup
{
    /// <summary>No joining group is assigned.</summary>
    NoJoiningGroup = 0,
    /// <summary>The Kashmiri Yeh joining group.</summary>
    KashmiriYeh = 1,
    /// <summary>The Alef joining group.</summary>
    Alef = 2,
    /// <summary>The Waw joining group.</summary>
    Waw = 3,
    /// <summary>The Yeh joining group.</summary>
    Yeh = 4,
    /// <summary>The Beh joining group.</summary>
    Beh = 5,
    /// <summary>The Teh Marbuta joining group.</summary>
    TehMarbuta = 6,
    /// <summary>The Hah joining group.</summary>
    Hah = 7,
    /// <summary>The Dal joining group.</summary>
    Dal = 8,
    /// <summary>The Reh joining group.</summary>
    Reh = 9,
    /// <summary>The Seen joining group.</summary>
    Seen = 10,
    /// <summary>The Sad joining group.</summary>
    Sad = 11,
    /// <summary>The Tah joining group.</summary>
    Tah = 12,
    /// <summary>The Ain joining group.</summary>
    Ain = 13,
    /// <summary>The Gaf joining group.</summary>
    Gaf = 14,
    /// <summary>The Farsi Yeh joining group.</summary>
    FarsiYeh = 15,
    /// <summary>The Feh joining group.</summary>
    Feh = 16,
    /// <summary>The Qaf joining group.</summary>
    Qaf = 17,
    /// <summary>The Kaf joining group.</summary>
    Kaf = 18,
    /// <summary>The Lam joining group.</summary>
    Lam = 19,
    /// <summary>The Meem joining group.</summary>
    Meem = 20,
    /// <summary>The Noon joining group.</summary>
    Noon = 21,
    /// <summary>The Heh joining group.</summary>
    Heh = 22,
    /// <summary>The Swash Kaf joining group.</summary>
    SwashKaf = 23,
    /// <summary>The Nya joining group.</summary>
    Nya = 24,
    /// <summary>The Knotted Heh joining group.</summary>
    KnottedHeh = 25,
    /// <summary>The Heh Goal joining group.</summary>
    HehGoal = 26,
    /// <summary>The Teh Marbuta Goal joining group.</summary>
    TehMarbutaGoal = 27,
    /// <summary>The Yeh With Tail joining group.</summary>
    YehWithTail = 28,
    /// <summary>The Yeh Barree joining group.</summary>
    YehBarree = 29,
    /// <summary>The Alaph joining group.</summary>
    Alaph = 30,
    /// <summary>The Beth joining group.</summary>
    Beth = 31,
    /// <summary>The Gamal joining group.</summary>
    Gamal = 32,
    /// <summary>The Dalath Rish joining group.</summary>
    DalathRish = 33,
    /// <summary>The He joining group.</summary>
    He = 34,
    /// <summary>The Syriac Waw joining group.</summary>
    SyriacWaw = 35,
    /// <summary>The Zain joining group.</summary>
    Zain = 36,
    /// <summary>The Heth joining group.</summary>
    Heth = 37,
    /// <summary>The Teth joining group.</summary>
    Teth = 38,
    /// <summary>The Yudh joining group.</summary>
    Yudh = 39,
    /// <summary>The Yudh He joining group.</summary>
    YudhHe = 40,
    /// <summary>The Kaph joining group.</summary>
    Kaph = 41,
    /// <summary>The Lamadh joining group.</summary>
    Lamadh = 42,
    /// <summary>The Mim joining group.</summary>
    Mim = 43,
    /// <summary>The Nun joining group.</summary>
    Nun = 44,
    /// <summary>The Semkath joining group.</summary>
    Semkath = 45,
    /// <summary>The Final Semkath joining group.</summary>
    FinalSemkath = 46,
    /// <summary>The E joining group.</summary>
    E = 47,
    /// <summary>The Pe joining group.</summary>
    Pe = 48,
    /// <summary>The Reversed Pe joining group.</summary>
    ReversedPe = 49,
    /// <summary>The Sadhe joining group.</summary>
    Sadhe = 50,
    /// <summary>The Qaph joining group.</summary>
    Qaph = 51,
    /// <summary>The Shin joining group.</summary>
    Shin = 52,
    /// <summary>The Taw joining group.</summary>
    Taw = 53,
    /// <summary>The Zhain joining group.</summary>
    Zhain = 54,
    /// <summary>The Khaph joining group.</summary>
    Khaph = 55,
    /// <summary>The Fe joining group.</summary>
    Fe = 56,
    /// <summary>The Burushaski Yeh Barree joining group.</summary>
    BurushaskiYehBarree = 57,
    /// <summary>The Malayalam Nga joining group.</summary>
    MalayalamNga = 58,
    /// <summary>The Malayalam Ja joining group.</summary>
    MalayalamJa = 59,
    /// <summary>The Malayalam Nya joining group.</summary>
    MalayalamNya = 60,
    /// <summary>The Malayalam Tta joining group.</summary>
    MalayalamTta = 61,
    /// <summary>The Malayalam Nna joining group.</summary>
    MalayalamNna = 62,
    /// <summary>The Malayalam Nnna joining group.</summary>
    MalayalamNnna = 63,
    /// <summary>The Malayalam Bha joining group.</summary>
    MalayalamBha = 64,
    /// <summary>The Malayalam Ra joining group.</summary>
    MalayalamRa = 65,
    /// <summary>The Malayalam Lla joining group.</summary>
    MalayalamLla = 66,
    /// <summary>The Malayalam Llla joining group.</summary>
    MalayalamLlla = 67,
    /// <summary>The Malayalam Ssa joining group.</summary>
    MalayalamSsa = 68,
    /// <summary>The Thin Yeh joining group.</summary>
    ThinYeh = 69,
    /// <summary>The Vertical Tail joining group.</summary>
    VerticalTail = 70,
    /// <summary>The Rohingya Yeh joining group.</summary>
    RohingyaYeh = 71,
    /// <summary>The Straight Waw joining group.</summary>
    StraightWaw = 72,
    /// <summary>The African Feh joining group.</summary>
    AfricanFeh = 73,
    /// <summary>The African Qaf joining group.</summary>
    AfricanQaf = 74,
    /// <summary>The African Noon joining group.</summary>
    AfricanNoon = 75,
    /// <summary>The Manichaean Aleph joining group.</summary>
    ManichaeanAleph = 76,
    /// <summary>The Manichaean Beth joining group.</summary>
    ManichaeanBeth = 77,
    /// <summary>The Manichaean Gimel joining group.</summary>
    ManichaeanGimel = 78,
    /// <summary>The Manichaean Daleth joining group.</summary>
    ManichaeanDaleth = 79,
    /// <summary>The Manichaean Waw joining group.</summary>
    ManichaeanWaw = 80,
    /// <summary>The Manichaean Zayin joining group.</summary>
    ManichaeanZayin = 81,
    /// <summary>The Manichaean Heth joining group.</summary>
    ManichaeanHeth = 82,
    /// <summary>The Manichaean Teth joining group.</summary>
    ManichaeanTeth = 83,
    /// <summary>The Manichaean Yodh joining group.</summary>
    ManichaeanYodh = 84,
    /// <summary>The Manichaean Kaph joining group.</summary>
    ManichaeanKaph = 85,
    /// <summary>The Manichaean Lamedh joining group.</summary>
    ManichaeanLamedh = 86,
    /// <summary>The Manichaean Dhamedh joining group.</summary>
    ManichaeanDhamedh = 87,
    /// <summary>The Manichaean Thamedh joining group.</summary>
    ManichaeanThamedh = 88,
    /// <summary>The Manichaean Mem joining group.</summary>
    ManichaeanMem = 89,
    /// <summary>The Manichaean Nun joining group.</summary>
    ManichaeanNun = 90,
    /// <summary>The Manichaean Samekh joining group.</summary>
    ManichaeanSamekh = 91,
    /// <summary>The Manichaean Ayin joining group.</summary>
    ManichaeanAyin = 92,
    /// <summary>The Manichaean Pe joining group.</summary>
    ManichaeanPe = 93,
    /// <summary>The Manichaean Sadhe joining group.</summary>
    ManichaeanSadhe = 94,
    /// <summary>The Manichaean Qoph joining group.</summary>
    ManichaeanQoph = 95,
    /// <summary>The Manichaean Resh joining group.</summary>
    ManichaeanResh = 96,
    /// <summary>The Manichaean Taw joining group.</summary>
    ManichaeanTaw = 97,
    /// <summary>The Manichaean One joining group.</summary>
    ManichaeanOne = 98,
    /// <summary>The Manichaean Five joining group.</summary>
    ManichaeanFive = 99,
    /// <summary>The Manichaean Ten joining group.</summary>
    ManichaeanTen = 100,
    /// <summary>The Manichaean Twenty joining group.</summary>
    ManichaeanTwenty = 101,
    /// <summary>The Manichaean Hundred joining group.</summary>
    ManichaeanHundred = 102,
    /// <summary>The Hanifi Rohingya Pa joining group.</summary>
    HanifiRohingyaPa = 103,
    /// <summary>The Hanifi Rohingya Kinna Ya joining group.</summary>
    HanifiRohingyaKinnaYa = 104,
    /// <summary>The Thin Noon joining group.</summary>
    ThinNoon = 105,
}

/// <summary>
/// Identifies the Unicode version in which a scalar was assigned.
/// </summary>
public enum UnicodeAge
{
    /// <summary>The scalar is unassigned.</summary>
    Unassigned = 0,
    /// <summary>The scalar was assigned in Unicode 1.1.</summary>
    Version1_1 = 1,
    /// <summary>The scalar was assigned in Unicode 2.0.</summary>
    Version2_0 = 2,
    /// <summary>The scalar was assigned in Unicode 2.1.</summary>
    Version2_1 = 3,
    /// <summary>The scalar was assigned in Unicode 3.0.</summary>
    Version3_0 = 4,
    /// <summary>The scalar was assigned in Unicode 3.1.</summary>
    Version3_1 = 5,
    /// <summary>The scalar was assigned in Unicode 3.2.</summary>
    Version3_2 = 6,
    /// <summary>The scalar was assigned in Unicode 4.0.</summary>
    Version4_0 = 7,
    /// <summary>The scalar was assigned in Unicode 4.1.</summary>
    Version4_1 = 8,
    /// <summary>The scalar was assigned in Unicode 5.0.</summary>
    Version5_0 = 9,
    /// <summary>The scalar was assigned in Unicode 5.1.</summary>
    Version5_1 = 10,
    /// <summary>The scalar was assigned in Unicode 5.2.</summary>
    Version5_2 = 11,
    /// <summary>The scalar was assigned in Unicode 6.0.</summary>
    Version6_0 = 12,
    /// <summary>The scalar was assigned in Unicode 6.1.</summary>
    Version6_1 = 13,
    /// <summary>The scalar was assigned in Unicode 6.2.</summary>
    Version6_2 = 14,
    /// <summary>The scalar was assigned in Unicode 6.3.</summary>
    Version6_3 = 15,
    /// <summary>The scalar was assigned in Unicode 7.0.</summary>
    Version7_0 = 16,
    /// <summary>The scalar was assigned in Unicode 8.0.</summary>
    Version8_0 = 17,
    /// <summary>The scalar was assigned in Unicode 9.0.</summary>
    Version9_0 = 18,
    /// <summary>The scalar was assigned in Unicode 10.0.</summary>
    Version10_0 = 19,
    /// <summary>The scalar was assigned in Unicode 11.0.</summary>
    Version11_0 = 20,
    /// <summary>The scalar was assigned in Unicode 12.0.</summary>
    Version12_0 = 21,
    /// <summary>The scalar was assigned in Unicode 12.1.</summary>
    Version12_1 = 22,
    /// <summary>The scalar was assigned in Unicode 13.0.</summary>
    Version13_0 = 23,
    /// <summary>The scalar was assigned in Unicode 14.0.</summary>
    Version14_0 = 24,
    /// <summary>The scalar was assigned in Unicode 15.0.</summary>
    Version15_0 = 25,
    /// <summary>The scalar was assigned in Unicode 15.1.</summary>
    Version15_1 = 26,
    /// <summary>The scalar was assigned in Unicode 16.0.</summary>
    Version16_0 = 27,
    /// <summary>The scalar was assigned in Unicode 17.0.</summary>
    Version17_0 = 28,
}

/// <summary>
/// Identifies the Indic conjunct-break property of a scalar.
/// </summary>
public enum UnicodeIndicConjunctBreak
{
    /// <summary>No Indic conjunct-break role is assigned.</summary>
    None = 0,
    /// <summary>The scalar is a linker.</summary>
    Linker = 1,
    /// <summary>The scalar is a consonant.</summary>
    Consonant = 2,
    /// <summary>The scalar extends an Indic conjunct.</summary>
    Extend = 3,
}

/// <summary>
/// Identifies the Hangul syllable type of a scalar.
/// </summary>
public enum UnicodeHangulSyllableType
{
    /// <summary>The property is not applicable.</summary>
    NotApplicable = 0,
    /// <summary>The scalar is a leading Jamo.</summary>
    L = 1,
    /// <summary>The scalar is a vowel Jamo.</summary>
    V = 2,
    /// <summary>The scalar is a trailing Jamo.</summary>
    T = 3,
    /// <summary>The scalar is an LV syllable.</summary>
    Lv = 4,
    /// <summary>The scalar is an LVT syllable.</summary>
    Lvt = 5,
}

/// <summary>
/// Identifies a scalar's grapheme-cluster boundary class.
/// </summary>
public enum UnicodeGraphemeBreakClass
{
    /// <summary>
    /// Represents the Unicode other value.
    /// </summary>
    Other = 0,
    /// <summary>
    /// The carriage-return (CR) boundary class.
    /// </summary>
    Cr = 1,
    /// <summary>
    /// The line-feed (LF) boundary class.
    /// </summary>
    Lf = 2,
    /// <summary>
    /// Represents the Unicode control value.
    /// </summary>
    Control = 3,
    /// <summary>
    /// Represents the Unicode extend value.
    /// </summary>
    Extend = 4,
    /// <summary>
    /// Represents the Unicode regional Indicator value.
    /// </summary>
    RegionalIndicator = 5,
    /// <summary>
    /// Represents the Unicode prepend value.
    /// </summary>
    Prepend = 6,
    /// <summary>
    /// Represents the Unicode spacing Mark value.
    /// </summary>
    SpacingMark = 7,
    /// <summary>
    /// A leading Hangul Jamo boundary class.
    /// </summary>
    L = 8,
    /// <summary>
    /// A vowel Hangul Jamo boundary class.
    /// </summary>
    V = 9,
    /// <summary>
    /// A trailing Hangul Jamo boundary class.
    /// </summary>
    T = 10,
    /// <summary>
    /// A Hangul LV syllable boundary class.
    /// </summary>
    Lv = 11,
    /// <summary>
    /// A Hangul LVT syllable boundary class.
    /// </summary>
    Lvt = 12,
    /// <summary>
    /// The zero-width joiner (ZWJ) boundary class.
    /// </summary>
    Zwj = 13,
    /// <summary>
    /// Represents the Unicode extended Pictographic value.
    /// </summary>
    ExtendedPictographic = 14,
}

/// <summary>
/// Identifies a scalar's word-boundary class.
/// </summary>
public enum UnicodeWordBreakClass
{
    /// <summary>
    /// Represents the Unicode other value.
    /// </summary>
    Other = 0,
    /// <summary>
    /// The carriage-return (CR) boundary class.
    /// </summary>
    Cr = 1,
    /// <summary>
    /// The line-feed (LF) boundary class.
    /// </summary>
    Lf = 2,
    /// <summary>
    /// Represents the Unicode newline value.
    /// </summary>
    Newline = 3,
    /// <summary>
    /// Represents the Unicode extend value.
    /// </summary>
    Extend = 4,
    /// <summary>
    /// Represents the Unicode format value.
    /// </summary>
    Format = 5,
    /// <summary>
    /// Represents the Unicode katakana value.
    /// </summary>
    Katakana = 6,
    /// <summary>
    /// The Unicode ALetter word-boundary class.
    /// </summary>
    ALetter = 7,
    /// <summary>
    /// Represents the Unicode mid Letter value.
    /// </summary>
    MidLetter = 8,
    /// <summary>
    /// Represents the Unicode mid Num value.
    /// </summary>
    MidNum = 9,
    /// <summary>
    /// Represents the Unicode mid Num Let value.
    /// </summary>
    MidNumLet = 10,
    /// <summary>
    /// Represents the Unicode numeric value.
    /// </summary>
    Numeric = 11,
    /// <summary>
    /// Represents the Unicode extend Num Let value.
    /// </summary>
    ExtendNumLet = 12,
    /// <summary>
    /// Represents the Unicode regional Indicator value.
    /// </summary>
    RegionalIndicator = 13,
    /// <summary>
    /// Represents the Unicode hebrew Letter value.
    /// </summary>
    HebrewLetter = 14,
    /// <summary>
    /// Represents the Unicode single Quote value.
    /// </summary>
    SingleQuote = 15,
    /// <summary>
    /// Represents the Unicode double Quote value.
    /// </summary>
    DoubleQuote = 16,
    /// <summary>
    /// The zero-width joiner (ZWJ) boundary class.
    /// </summary>
    Zwj = 17,
    /// <summary>
    /// Represents the Unicode extended Pictographic value.
    /// </summary>
    ExtendedPictographic = 18,
    /// <summary>
    /// A horizontal-space sequence boundary class.
    /// </summary>
    WSegSpace = 19,
}

/// <summary>
/// Identifies a scalar's sentence-boundary class.
/// </summary>
public enum UnicodeSentenceBreakClass
{
    /// <summary>
    /// Represents the Unicode other value.
    /// </summary>
    Other = 0,
    /// <summary>
    /// The carriage-return (CR) boundary class.
    /// </summary>
    Cr = 1,
    /// <summary>
    /// The line-feed (LF) boundary class.
    /// </summary>
    Lf = 2,
    /// <summary>
    /// A paragraph or line separator boundary class.
    /// </summary>
    Sep = 3,
    /// <summary>
    /// Represents the Unicode extend value.
    /// </summary>
    Extend = 4,
    /// <summary>
    /// Represents the Unicode format value.
    /// </summary>
    Format = 5,
    /// <summary>
    /// The space sentence-boundary class.
    /// </summary>
    Sp = 6,
    /// <summary>
    /// Represents the Unicode lower value.
    /// </summary>
    Lower = 7,
    /// <summary>
    /// Represents the Unicode upper value.
    /// </summary>
    Upper = 8,
    /// <summary>
    /// The other-letter sentence-boundary class.
    /// </summary>
    OLetter = 9,
    /// <summary>
    /// Represents the Unicode numeric value.
    /// </summary>
    Numeric = 10,
    /// <summary>
    /// An abbreviation terminator such as a period.
    /// </summary>
    ATerm = 11,
    /// <summary>
    /// Punctuation that can continue a sentence terminator sequence.
    /// </summary>
    SContinue = 12,
    /// <summary>
    /// A sentence terminator such as an exclamation mark.
    /// </summary>
    STerm = 13,
    /// <summary>
    /// Represents the Unicode close value.
    /// </summary>
    Close = 14,
}

/// <summary>
/// Identifies a scalar's line-breaking class.
/// </summary>
public enum UnicodeLineBreakClass
{
    /// <summary>
    /// Represents the Unicode unknown value.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the Unicode mandatory Break value.
    /// </summary>
    MandatoryBreak = 1,
    /// <summary>
    /// Represents the Unicode carriage Return value.
    /// </summary>
    CarriageReturn = 2,
    /// <summary>
    /// Represents the Unicode line Feed value.
    /// </summary>
    LineFeed = 3,
    /// <summary>
    /// Represents the Unicode combining Mark value.
    /// </summary>
    CombiningMark = 4,
    /// <summary>
    /// Represents the Unicode surrogate value.
    /// </summary>
    Surrogate = 5,
    /// <summary>
    /// Represents the Unicode zero Width Space value.
    /// </summary>
    ZeroWidthSpace = 6,
    /// <summary>
    /// Represents the Unicode inseparable value.
    /// </summary>
    Inseparable = 7,
    /// <summary>
    /// Represents the Unicode nonstarter value.
    /// </summary>
    Nonstarter = 8,
    /// <summary>
    /// Represents the Unicode opening Punctuation value.
    /// </summary>
    OpeningPunctuation = 9,
    /// <summary>
    /// Represents the Unicode closing Punctuation value.
    /// </summary>
    ClosingPunctuation = 10,
    /// <summary>
    /// Represents the Unicode quotation value.
    /// </summary>
    Quotation = 11,
    /// <summary>
    /// Represents the Unicode glue value.
    /// </summary>
    Glue = 12,
    /// <summary>
    /// Represents the Unicode contingent Break value.
    /// </summary>
    ContingentBreak = 14,
    /// <summary>
    /// Represents the Unicode space value.
    /// </summary>
    Space = 15,
    /// <summary>
    /// Represents the Unicode break After value.
    /// </summary>
    BreakAfter = 16,
    /// <summary>
    /// Represents the Unicode break Before value.
    /// </summary>
    BreakBefore = 17,
    /// <summary>
    /// Represents the Unicode break Both value.
    /// </summary>
    BreakBoth = 18,
    /// <summary>
    /// Represents the Unicode hyphen value.
    /// </summary>
    Hyphen = 19,
    /// <summary>
    /// Represents the Unicode alphabetic value.
    /// </summary>
    Alphabetic = 20,
    /// <summary>
    /// Represents the Unicode numeric value.
    /// </summary>
    Numeric = 21,
    /// <summary>
    /// Represents the Unicode infix Numeric value.
    /// </summary>
    InfixNumeric = 22,
    /// <summary>
    /// Represents the Unicode ideographic value.
    /// </summary>
    Ideographic = 23,
    /// <summary>
    /// Represents the Unicode emoji Base value.
    /// </summary>
    EmojiBase = 24,
    /// <summary>
    /// Represents the Unicode emoji Modifier value.
    /// </summary>
    EmojiModifier = 25,
    /// <summary>
    /// Represents the Unicode zero Width Joiner value.
    /// </summary>
    ZeroWidthJoiner = 26,
    /// <summary>Represents the Unicode ambiguous line-break class.</summary>
    Ambiguous = 27,
    /// <summary>Represents the Unicode aksara line-break class.</summary>
    Aksara = 28,
    /// <summary>Represents the Unicode aksara pre-base line-break class.</summary>
    AksaraPrebase = 29,
    /// <summary>Represents the Unicode aksara-start line-break class.</summary>
    AksaraStart = 30,
    /// <summary>Represents the Unicode conditional Japanese starter line-break class.</summary>
    ConditionalJapaneseStarter = 31,
    /// <summary>Represents the Unicode closing-parenthesis line-break class.</summary>
    ClosingParenthesis = 32,
    /// <summary>Represents the Unicode exclamation line-break class.</summary>
    Exclamation = 33,
    /// <summary>Represents a Hangul LV syllable line-break class.</summary>
    HangulLvSyllable = 34,
    /// <summary>Represents a Hangul LVT syllable line-break class.</summary>
    HangulLvtSyllable = 35,
    /// <summary>Represents the Unicode unambiguous-hyphen line-break class.</summary>
    UnambiguousHyphen = 36,
    /// <summary>Represents the Unicode Hebrew-letter line-break class.</summary>
    HebrewLetter = 37,
    /// <summary>Represents a leading Hangul Jamo line-break class.</summary>
    HangulLeadingJamo = 38,
    /// <summary>Represents a trailing Hangul Jamo line-break class.</summary>
    HangulTrailingJamo = 39,
    /// <summary>Represents a vowel Hangul Jamo line-break class.</summary>
    HangulVowelJamo = 40,
    /// <summary>Represents the Unicode next-line line-break class.</summary>
    NextLine = 41,
    /// <summary>Represents the Unicode postfix-numeric line-break class.</summary>
    PostfixNumeric = 42,
    /// <summary>Represents the Unicode prefix-numeric line-break class.</summary>
    PrefixNumeric = 43,
    /// <summary>Represents the Unicode regional-indicator line-break class.</summary>
    RegionalIndicator = 44,
    /// <summary>Represents the Unicode complex-context line-break class.</summary>
    ComplexContext = 45,
    /// <summary>Represents the Unicode break-symbols line-break class.</summary>
    BreakSymbols = 46,
    /// <summary>Represents the Unicode virama-final line-break class.</summary>
    ViramaFinal = 47,
    /// <summary>Represents the Unicode virama line-break class.</summary>
    Virama = 48,
    /// <summary>Represents the Unicode word-joiner line-break class.</summary>
    WordJoiner = 49,
}

/// <summary>
/// Identifies whether a permitted Unicode line break is optional or mandatory.
/// </summary>
public enum UnicodeLineBreakStatus
{
    /// <summary>
    /// The line may break at this position.
    /// </summary>
    Optional = 0,
    /// <summary>
    /// The line must break at this position.
    /// </summary>
    Mandatory = 1,
}

/// <summary>
/// Selects the normalization and folding used to build a caseless comparison key.
/// </summary>
public enum UnicodeCompareKeyMode
{
    /// <summary>
    /// Full Unicode case folding without normalization.
    /// </summary>
    OrdinalFold = 0,
    /// <summary>
    /// NFC normalization followed by full Unicode case folding.
    /// </summary>
    NfcFold = 1,
    /// <summary>
    /// Unicode NFKC case folding for compatibility-insensitive comparison.
    /// </summary>
    NfkcFold = 2,
}

/// <summary>
/// Selects the normalization requirement applied by a constructed identifier profile.
/// </summary>
public enum UnicodeIdentifierNormalizationPolicy
{
    /// <summary>
    /// The profile does not require or apply normalization.
    /// </summary>
    None = 0,
    /// <summary>
    /// The profile requires and applies NFC normalization.
    /// </summary>
    Nfc = 1,
    /// <summary>
    /// The profile requires and applies NFD normalization.
    /// </summary>
    Nfd = 2,
    /// <summary>
    /// The profile requires and applies NFKC normalization.
    /// </summary>
    Nfkc = 3,
    /// <summary>
    /// The profile requires and applies NFKD normalization.
    /// </summary>
    Nfkd = 4,
}

/// <summary>
/// Selects the comparison-key behavior applied by a constructed identifier profile.
/// </summary>
public enum UnicodeIdentifierCasePolicy
{
    /// <summary>
    /// Identifier comparison preserves case.
    /// </summary>
    Sensitive = 0,
    /// <summary>
    /// Identifier comparison uses full Unicode case folding without normalization.
    /// </summary>
    OrdinalFold = 1,
    /// <summary>
    /// Identifier comparison uses NFC normalization and full Unicode case folding.
    /// </summary>
    NfcFold = 2,
    /// <summary>
    /// Identifier comparison uses Unicode NFKC case folding.
    /// </summary>
    NfkcFold = 3,
}

/// <summary>
/// Identifies the severity of a Unicode diagnostic.
/// </summary>
public enum UnicodeDiagnosticSeverity
{
    /// <summary>
    /// The diagnostic provides information and does not indicate a problem.
    /// </summary>
    Information = 0,
    /// <summary>
    /// The diagnostic identifies a condition that may require attention.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// The diagnostic identifies a failed requirement.
    /// </summary>
    Error = 2,
}

/// <summary>
/// Reports the most severe outcome of Unicode identifier validation.
/// </summary>
public enum UnicodeIdentifierStatus
{
    /// <summary>
    /// The check completed successfully.
    /// </summary>
    Valid = 0,
    /// <summary>
    /// The identifier is empty.
    /// </summary>
    Empty = 1,
    /// <summary>
    /// The first scalar is not permitted at the start of an identifier.
    /// </summary>
    InvalidStart = 2,
    /// <summary>
    /// A later scalar is not permitted in an identifier.
    /// </summary>
    InvalidContinue = 3,
    /// <summary>
    /// The profile disallows at least one scalar.
    /// </summary>
    Disallowed = 4,
    /// <summary>
    /// The identifier is valid only after applying the profile's normalization form.
    /// </summary>
    NeedsNormalization = 5,
    /// <summary>
    /// The identifier uses more than one significant script.
    /// </summary>
    MixedScript = 6,
    /// <summary>
    /// A Unicode security check found an unsafe condition.
    /// </summary>
    Unsafe = 7,
    /// <summary>
    /// The input contains malformed UTF-16 that does not decode to a Unicode scalar.
    /// </summary>
    InvalidScalar = 8,
}

/// <summary>
/// Reports the most severe outcome of PRECIS-style string validation.
/// </summary>
public enum UnicodePrecisStatus
{
    /// <summary>
    /// The check completed successfully.
    /// </summary>
    Valid = 0,
    /// <summary>
    /// The prepared value is empty but the profile requires content.
    /// </summary>
    Empty = 1,
    /// <summary>
    /// The input contains malformed UTF-16 that does not decode to a Unicode scalar.
    /// </summary>
    InvalidScalar = 2,
    /// <summary>
    /// The prepared value contains a space disallowed by the profile.
    /// </summary>
    DisallowedSpace = 3,
    /// <summary>
    /// The prepared value contains a control character.
    /// </summary>
    DisallowedControl = 4,
    /// <summary>
    /// The prepared value violates the profile's identifier-class rules.
    /// </summary>
    DisallowedIdentifier = 5,
    /// <summary>
    /// Mapping, case conversion, or normalization would change the input.
    /// </summary>
    NeedsPreparation = 6,
    /// <summary>
    /// The prepared value contains a code point disallowed by its PRECIS string class.
    /// </summary>
    DisallowedCodePoint = 7,
    /// <summary>
    /// A context-dependent code point does not satisfy its contextual rule.
    /// </summary>
    ContextualRule = 8,
    /// <summary>
    /// The prepared value violates the RFC 5893 bidirectional-text rule.
    /// </summary>
    BidiRule = 9,
}

/// <summary>
/// Reports the outcome of IDNA-adjacent Unicode label validation.
/// </summary>
public enum UnicodeIdnaStatus
{
    /// <summary>The label passed every validation rule.</summary>
    Valid = 0,
    /// <summary>The prepared label is empty.</summary>
    Empty = 1,
    /// <summary>The input contains malformed UTF-16.</summary>
    InvalidScalar = 2,
    /// <summary>The label contains a label separator.</summary>
    LabelSeparator = 3,
    /// <summary>The label violates a hyphen placement rule.</summary>
    Hyphen = 4,
    /// <summary>The label begins with a combining mark.</summary>
    LeadingMark = 5,
    /// <summary>The label contains a disallowed code point.</summary>
    DisallowedCodePoint = 6,
    /// <summary>A context-dependent code point does not satisfy its contextual rule.</summary>
    ContextualRule = 7,
    /// <summary>The label violates the RFC 5893 bidirectional-text rule.</summary>
    BidiRule = 8,
}

/// <summary>
/// Identifies Unicode security conditions found in an identifier.
/// </summary>
/// <remarks>
/// Values are powers of two and may be combined in <see cref="UnicodeSecurityCheckResult.Flags"/>.
/// </remarks>
public enum UnicodeSecurityFlag
{
    /// <summary>
    /// No condition is present.
    /// </summary>
    None = 0,
    /// <summary>
    /// More than one significant script is present.
    /// </summary>
    MixedScript = 1,
    /// <summary>
    /// A comparison found a distinct string with the same UTS #39 skeleton.
    /// </summary>
    Confusable = 2,
    /// <summary>
    /// A comparison found whole-script confusable strings.
    /// </summary>
    WholeScriptConfusable = 4,
    /// <summary>
    /// An invisible or repeated combining-mark pattern is present.
    /// </summary>
    Invisible = 8,
    /// <summary>
    /// A Default_Ignorable_Code_Point is present.
    /// </summary>
    DefaultIgnorable = 16,
    /// <summary>
    /// A join-control character is present.
    /// </summary>
    JoinControl = 32,
    /// <summary>
    /// A bidirectional-control character is present.
    /// </summary>
    BidiControl = 64,
    /// <summary>
    /// A variation selector is present.
    /// </summary>
    VariationSelector = 128,
    /// <summary>
    /// The identifier does not meet the allowed security profile.
    /// </summary>
    Restricted = 256,
    /// <summary>
    /// The identifier or label violates bidirectional safety rules.
    /// </summary>
    BidiUnsafe = 512,
}

/// <summary>
/// Identifies whether a scalar is permitted by the UTS #39 General Security Profile.
/// </summary>
public enum UnicodeIdentifierSecurityStatus
{
    /// <summary>
    /// The scalar is restricted by the General Security Profile.
    /// </summary>
    Restricted = 0,
    /// <summary>
    /// The scalar is allowed by the General Security Profile.
    /// </summary>
    Allowed = 1,
}

/// <summary>
/// Identifies a UTS #39 Identifier_Type classification.
/// </summary>
public enum UnicodeIdentifierType
{
    /// <summary>The scalar is unassigned, private-use, a surrogate, or a non-whitespace control.</summary>
    NotCharacter = 0,
    /// <summary>The scalar has the Unicode Deprecated property.</summary>
    Deprecated = 1,
    /// <summary>The scalar has default-ignorable behavior.</summary>
    DefaultIgnorable = 2,
    /// <summary>The scalar cannot occur in NFKC-normalized text.</summary>
    NotNfkc = 3,
    /// <summary>The scalar does not have the XID_Continue property.</summary>
    NotXid = 4,
    /// <summary>The scalar belongs only to an excluded script.</summary>
    Exclusion = 5,
    /// <summary>The scalar is no longer in modern use.</summary>
    Obsolete = 6,
    /// <summary>The scalar has specialized or technical usage.</summary>
    Technical = 7,
    /// <summary>The scalar is uncommon or has uncertain usage.</summary>
    UncommonUse = 8,
    /// <summary>The scalar belongs to a limited-use script.</summary>
    LimitedUse = 9,
    /// <summary>The scalar is an explicit security-profile inclusion.</summary>
    Inclusion = 10,
    /// <summary>The scalar is recommended for identifiers.</summary>
    Recommended = 11,
}

/// <summary>
/// Classifies an identifier by the UTS #39 restriction-level model.
/// </summary>
public enum UnicodeRestrictionLevel
{
    /// <summary>
    /// Every scalar is ASCII.
    /// </summary>
    Ascii = 0,
    /// <summary>
    /// The identifier uses one script and meets the restrictive identifier profile.
    /// </summary>
    SingleScriptRestrictive = 1,
    /// <summary>
    /// The script combination is one of the highly restrictive UTS #39 combinations.
    /// </summary>
    HighlyRestrictive = 2,
    /// <summary>
    /// The script combination meets the moderately restrictive UTS #39 rules.
    /// </summary>
    ModeratelyRestrictive = 3,
    /// <summary>
    /// The identifier is allowed but does not meet a stricter script policy.
    /// </summary>
    MinimallyRestrictive = 4,
    /// <summary>
    /// The identifier does not meet the allowed identifier profile.
    /// </summary>
    Unrestricted = 5,
}

/// <summary>
/// Identifies the byte encoding selected for a PDF text string.
/// </summary>
public enum UnicodePdfTextEncoding
{
    /// <summary>
    /// The single-byte PDFDocEncoding character set.
    /// </summary>
    PdfDocEncoding = 0,
    /// <summary>
    /// Big-endian UTF-16 prefixed with a byte-order mark.
    /// </summary>
    Utf16BeWithBom = 1,
}

/// <summary>
/// Identifies the resolved direction of a PDF layout run.
/// </summary>
public enum UnicodePdfBidiDirection
{
    /// <summary>
    /// A left-to-right layout run.
    /// </summary>
    LeftToRight = 0,
    /// <summary>
    /// A right-to-left layout run.
    /// </summary>
    RightToLeft = 1,
    /// <summary>
    /// No strong direction was resolved.
    /// </summary>
    Neutral = 2,
}

/// <summary>
/// Classifies a Unicode scalar for simple PDF text-width heuristics.
/// </summary>
public enum UnicodePdfTextWidth
{
    /// <summary>
    /// The scalar does not contribute display width by itself.
    /// </summary>
    Zero = 0,
    /// <summary>
    /// The scalar normally occupies one display column.
    /// </summary>
    Narrow = 1,
    /// <summary>
    /// The scalar normally occupies two display columns.
    /// </summary>
    Wide = 2,
}

/// <summary>
/// Reports the most severe outcome of PDF ToUnicode map validation.
/// </summary>
public enum UnicodePdfCMapStatus
{
    /// <summary>
    /// The check completed successfully.
    /// </summary>
    Valid = 0,
    /// <summary>
    /// The glyph map contains no mappings.
    /// </summary>
    EmptyMap = 1,
    /// <summary>
    /// At least one glyph mapping is invalid.
    /// </summary>
    InvalidMapping = 2,
    /// <summary>
    /// More than one mapping uses the same PDF character code.
    /// </summary>
    DuplicateCharacterCode = 3,
    /// <summary>
    /// A glyph mapping contains no Unicode scalar sequence.
    /// </summary>
    MissingUnicodeScalars = 4,
    /// <summary>
    /// A glyph mapping contains a value that is not a Unicode scalar.
    /// </summary>
    InvalidUnicodeScalar = 5,
    /// <summary>
    /// A glyph maps to U+FFFD, which usually indicates lost source text.
    /// </summary>
    ReplacementCharacter = 6,
    /// <summary>
    /// The glyph map does not provide a usable PDF CMap font name.
    /// </summary>
    InvalidFontName = 7,
}

/// <summary>
/// Identifies a PDF text context that carries accessibility or document text.
/// </summary>
public enum UnicodePdfTextContext
{
    /// <summary>Marked-content replacement text used for extraction.</summary>
    ActualText = 0,
    /// <summary>Alternative text for non-text content.</summary>
    AltText = 1,
    /// <summary>Text displayed for a document bookmark.</summary>
    Bookmark = 2,
    /// <summary>Text displayed for an outline item.</summary>
    Outline = 3,
    /// <summary>Text associated with a structure element.</summary>
    StructureElement = 4,
    /// <summary>Document metadata text.</summary>
    Metadata = 5,
}

/// <summary>
/// Identifies the PDF accessibility or archival conformance profile being checked.
/// </summary>
public enum UnicodePdfConformance
{
    /// <summary>PDF/A archival conformance.</summary>
    PdfA = 0,
    /// <summary>PDF/UA accessibility conformance.</summary>
    PdfUa = 1,
    /// <summary>Combined PDF/A and PDF/UA conformance.</summary>
    PdfAAndPdfUa = 2,
}

/// <summary>
/// Reports the severity of a Unicode-related PDF diagnostic.
/// </summary>
public enum UnicodePdfDiagnosticStatus
{
    /// <summary>
    /// The check completed successfully.
    /// </summary>
    Valid = 0,
    /// <summary>
    /// The text contains malformed UTF-16.
    /// </summary>
    InvalidTextScalar = 1,
    /// <summary>
    /// The text should be replaced or normalized before use as PDF ActualText.
    /// </summary>
    NeedsActualTextPreparation = 2,
    /// <summary>
    /// The text cannot be represented exactly with PDFDocEncoding and requires UTF-16BE.
    /// </summary>
    RequiresUtf16Be = 3,
    /// <summary>
    /// A font has no usable character-code-to-Unicode mappings.
    /// </summary>
    MissingUnicodeMap = 4,
    /// <summary>
    /// A font's character-code-to-Unicode mappings fail CMap validation.
    /// </summary>
    InvalidUnicodeMap = 5,
    /// <summary>
    /// Accessibility text is not normalized or otherwise prepared for its PDF context.
    /// </summary>
    NeedsAccessibilityTextPreparation = 6,
    /// <summary>
    /// Accessibility text contains a scalar prohibited by the preparation profile.
    /// </summary>
    ProhibitedTextScalar = 7,
    /// <summary>
    /// Accessibility text contains U+FFFD and may have lost source text.
    /// </summary>
    ReplacementTextScalar = 8,
}
