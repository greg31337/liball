using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides person-related generation operations.
/// </summary>
public class PersonProvider : IPersonProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal FakeRandom IdentityRandomValue;
    internal FakeRandom WorkRandomValue;
    internal FakeRandom LifeRandomValue;
    internal FakeRandom EducationRandomValue;
    internal FakeRandom ProfileRandomValue;
    internal PersonProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("person");
        IdentityRandomValue = context.ProviderRandom("person.identity");
        WorkRandomValue = context.ProviderRandom("person.work");
        LifeRandomValue = context.ProviderRandom("person.life");
        EducationRandomValue = context.ProviderRandom("person.education");
        ProfileRandomValue = context.ProviderRandom("person.profile");
    }

    /// <summary>
    /// Generates a given name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided given name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string FirstName()
    {
        return PersonNameGenerator.FirstName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a given name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided given name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    public string FirstNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.FirstName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a middle name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided middle name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no middle names.</exception>
    public string MiddleName()
    {
        return PersonNameGenerator.MiddleName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a middle name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided middle name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    public string MiddleNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.MiddleName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a family name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided family name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string LastName()
    {
        return PersonNameGenerator.LastName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a family name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided family name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    public string LastNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.LastName(Context, RandomValue, sexCategory);
    }

    /// <summary>Generates a maiden family-name component.</summary>
    /// <returns>A locale-provided maiden name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no maiden names.</exception>
    public string MaidenName()
    {
        return PersonNameGenerator.MaidenName(Context, RandomValue);
    }

    /// <summary>Generates a maiden name for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided maiden name applicable to the category.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    public string MaidenNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.MaidenName(Context, RandomValue, sexCategory);
    }

    /// <summary>Generates a patronymic name component.</summary>
    /// <returns>A locale-provided patronymic.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no patronymics.</exception>
    public string Patronymic()
    {
        return PersonNameGenerator.Patronymic(Context, RandomValue);
    }

    /// <summary>Generates a patronymic for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided patronymic applicable to the category.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    public string PatronymicFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.Patronymic(Context, RandomValue, sexCategory);
    }

    /// <summary>Generates a matronymic name component.</summary>
    /// <returns>A locale-provided matronymic.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no matronymics.</exception>
    public string Matronymic()
    {
        return PersonNameGenerator.Matronymic(Context, RandomValue);
    }

    /// <summary>Generates a matronymic for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided matronymic applicable to the category.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    public string MatronymicFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.Matronymic(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a name prefix using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided name prefix.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no prefixes.</exception>
    public string Prefix()
    {
        return PersonNameGenerator.Prefix(Context, RandomValue);
    }

    /// <summary>
    /// Generates a name prefix for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided prefix applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    public string PrefixFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.Prefix(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a name suffix using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided name suffix.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no suffixes.</exception>
    public string Suffix()
    {
        return PersonNameGenerator.Suffix(Context, RandomValue);
    }

    /// <summary>
    /// Generates a name suffix for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided suffix applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    public string SuffixFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.Suffix(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates the first Unicode text element of a locale-provided given name.
    /// </summary>
    /// <returns>A complete initial text element without an added punctuation mark.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string Initial()
    {
        return PersonNameGenerator.Initial(Context, RandomValue);
    }

    /// <summary>
    /// Generates the first Unicode text element of a given name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A complete initial text element without an added punctuation mark.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string InitialFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.Initial(Context, RandomValue, sexCategory);
    }

    /// <summary>Generates two initials from correlated given and family name components.</summary>
    /// <returns>Two complete Unicode initial text elements without punctuation.</returns>
    public string Initials()
    {
        return InitialsFromCount(2);
    }

    /// <summary>Generates a requested number of correlated name initials.</summary>
    /// <param name="count">The nonnegative number of initials.</param>
    /// <returns>Complete Unicode initial text elements without punctuation.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    public string InitialsFromCount(int count)
    {
        return PersonNameGenerator.Initials(Context, RandomValue, count);
    }

    /// <summary>
    /// Generates a name using a weighted full-name template from the configured locale.
    /// </summary>
    /// <returns>A correlated full name in locale-defined component order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string FullName()
    {
        return PersonNameGenerator.FullName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a full name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated full name in locale-defined component order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string FullNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.FullName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a name using a weighted informal display template from the configured locale.
    /// </summary>
    /// <returns>A correlated informal display name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string DisplayName()
    {
        return PersonNameGenerator.DisplayName(Context, RandomValue);
    }

    /// <summary>
    /// Generates an informal display name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated informal display name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string DisplayNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.DisplayName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a name using a weighted formal template from the configured locale.
    /// </summary>
    /// <returns>A correlated formal name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string FormalName()
    {
        return PersonNameGenerator.FormalName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a formal name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated formal name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string FormalNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.FormalName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a full name in the resolved locale's native script and component order.
    /// </summary>
    /// <returns>A correlated native-form full name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string NativeName()
    {
        return PersonNameGenerator.FullName(Context, RandomValue);
    }

    /// <summary>
    /// Generates a native-form full name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated native-form full name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string NativeNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.FullName(Context, RandomValue, sexCategory);
    }

    /// <summary>
    /// Generates a full name and transliterates supported scripts to an ASCII Latin form.
    /// </summary>
    /// <remarks>Characters outside the declared Greek, Cyrillic, Arabic, Hebrew, and Latin mappings are omitted from the transliterated result.</remarks>
    /// <returns>A correlated transliterated full name in locale-defined component order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string TransliteratedName()
    {
        return PersonNameGenerator.TransliteratedName(Context, RandomValue);
    }

    /// <summary>
    /// Generates and transliterates a full name for an explicit locale naming category.
    /// </summary>
    /// <remarks>Characters outside the declared Greek, Cyrillic, Arabic, Hebrew, and Latin mappings are omitted from the transliterated result.</remarks>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated transliterated full name in locale-defined component order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string TransliteratedNameFromSexCategory(string sexCategory)
    {
        return PersonNameGenerator.TransliteratedName(Context, RandomValue, sexCategory);
    }

    /// <summary>Generates a given name in the resolved locale's native script.</summary>
    /// <returns>A native-script given name.</returns>
    public string NativeFirstName()
    {
        return PersonNameGenerator.FirstName(Context, RandomValue);
    }

    /// <summary>Generates a middle name in the resolved locale's native script.</summary>
    /// <returns>A native-script middle name.</returns>
    public string NativeMiddleName()
    {
        return PersonNameGenerator.MiddleName(Context, RandomValue);
    }

    /// <summary>Generates a family name in the resolved locale's native script.</summary>
    /// <returns>A native-script family name.</returns>
    public string NativeLastName()
    {
        return PersonNameGenerator.LastName(Context, RandomValue);
    }

    /// <summary>Generates a maiden name in the resolved locale's native script.</summary>
    /// <returns>A native-script maiden name.</returns>
    public string NativeMaidenName()
    {
        return PersonNameGenerator.MaidenName(Context, RandomValue);
    }

    /// <summary>Generates a patronymic in the resolved locale's native script.</summary>
    /// <returns>A native-script patronymic.</returns>
    public string NativePatronymic()
    {
        return PersonNameGenerator.Patronymic(Context, RandomValue);
    }

    /// <summary>Generates a matronymic in the resolved locale's native script.</summary>
    /// <returns>A native-script matronymic.</returns>
    public string NativeMatronymic()
    {
        return PersonNameGenerator.Matronymic(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a given-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedFirstName()
    {
        return PersonNameGenerator.TransliteratedFirstName(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a middle-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedMiddleName()
    {
        return PersonNameGenerator.TransliteratedMiddleName(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a family-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedLastName()
    {
        return PersonNameGenerator.TransliteratedLastName(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a maiden-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedMaidenName()
    {
        return PersonNameGenerator.TransliteratedMaidenName(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a patronymic component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedPatronymic()
    {
        return PersonNameGenerator.TransliteratedPatronymic(Context, RandomValue);
    }

    /// <summary>Generates and transliterates a matronymic component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    public string TransliteratedMatronymic()
    {
        return PersonNameGenerator.TransliteratedMatronymic(Context, RandomValue);
    }

    /// <summary>
    /// Generates a requested valid unusual name case from the configured locale.
    /// </summary>
    /// <param name="nameCase">The structural or textual name case to generate.</param>
    /// <returns>A weighted locale-provided name exhibiting the requested case.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>nameCase</c> is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or does not define the requested case.</exception>
    public string NameCase(PersonNameCase nameCase)
    {
        return PersonNameGenerator.NameCase(Context, RandomValue, nameCase);
    }

    /// <summary>
    /// Generates a requested valid unusual name case for an explicit locale naming category.
    /// </summary>
    /// <param name="nameCase">The structural or textual name case to generate.</param>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A weighted locale-provided name applicable to the category and exhibiting the requested case.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>nameCase</c> is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or does not define an applicable requested case.</exception>
    public string NameCaseFromNameCaseAndSexCategory(PersonNameCase nameCase, string sexCategory)
    {
        return PersonNameGenerator.NameCase(Context, RandomValue, nameCase, sexCategory);
    }

    /// <summary>
    /// Generates a locale-provided gender identity label independently of names and pronouns.
    /// </summary>
    /// <remarks>The bundled weights are synthetic choices and do not represent population statistics.</remarks>
    /// <returns>A gender identity label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string Gender()
    {
        return PersonIdentityGenerator.Gender(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates one correlated locale-provided pronoun set independently of gender and names.
    /// </summary>
    /// <returns>A new pronoun-set value containing all grammatical forms.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public PronounSet Pronouns()
    {
        return PersonIdentityGenerator.Pronouns(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a short, neutral, project-authored biography.
    /// </summary>
    /// <returns>A locale-provided biography sentence.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string Biography()
    {
        return PersonIdentityGenerator.Biography(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided zodiac-sign label independently of any birth date.
    /// </summary>
    /// <returns>A zodiac-sign label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string ZodiacSign()
    {
        return PersonIdentityGenerator.ZodiacSign(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided marital-status label.
    /// </summary>
    /// <returns>A marital-status label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string MaritalStatus()
    {
        return PersonIdentityGenerator.MaritalStatus(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided interpersonal relationship label.
    /// </summary>
    /// <returns>A relationship label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string Relationship()
    {
        return PersonIdentityGenerator.Relationship(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided role within a household.
    /// </summary>
    /// <returns>A household-role label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string HouseholdRole()
    {
        return PersonIdentityGenerator.HouseholdRole(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided household-type label.
    /// </summary>
    /// <returns>A household-type label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string HouseholdType()
    {
        return PersonIdentityGenerator.HouseholdType(Context, IdentityRandomValue);
    }

    /// <summary>
    /// Generates a broad demographic label from a requested locale-provided category.
    /// </summary>
    /// <remarks>Categories are independent scalar fixtures and are not correlated with other person values.</remarks>
    /// <param name="category">A category returned by <c>DemographicCategories</c>.</param>
    /// <returns>A weighted value from the requested demographic category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>category</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>category</c> is empty, whitespace, or unavailable.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string Demographic(string category)
    {
        return PersonIdentityGenerator.Demographic(Context, IdentityRandomValue, category);
    }

    /// <summary>
    /// Returns the locale-provided broad demographic category identifiers.
    /// </summary>
    /// <returns>A new array in ordinal category order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string[] DemographicCategories()
    {
        return PersonIdentityGenerator.DemographicCategories(Context);
    }

    /// <summary>Generates a birth date whose completed age is between 18 and 80.</summary>
    /// <returns>A calendar birth date in the default adult age range.</returns>
    public System.DateOnly BirthDate()
    {
        return BirthDateFromMinimumAgeAndMaximumAge(18, 80);
    }

    /// <summary>Generates a birth date with an exact completed age.</summary>
    /// <param name="age">The nonnegative completed age.</param>
    /// <returns>A birth date having the requested age.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>age</c> is invalid or unrepresentable.</exception>
    public System.DateOnly BirthDateFromAge(int age)
    {
        return BirthDateFromMinimumAgeAndMaximumAge(age, age);
    }

    /// <summary>Generates a birth date within an inclusive completed-age range.</summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth date whose completed age is in the requested range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the ages are invalid or unrepresentable.</exception>
    public System.DateOnly BirthDateFromMinimumAgeAndMaximumAge(int minimumAge, int maximumAge)
    {
        return CalendarDateGenerator.BirthDate(
            LifeRandomValue,
            CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()),
            minimumAge,
            maximumAge);
    }

    /// <summary>Calculates completed age at the captured reference date.</summary>
    /// <param name="birthDate">The birth date to evaluate.</param>
    /// <returns>The number of completed calendar years.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>birthDate</c> is in the future.</exception>
    public int Age(System.DateOnly birthDate)
    {
        return CalendarDateGenerator.Age(
            birthDate,
            CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()));
    }

    /// <summary>Generates a correlated birth date and completed age.</summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth-date and age pair evaluated at the captured reference date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the ages are invalid or unrepresentable.</exception>
    public BirthDateAge BirthDateAndAge(int minimumAge, int maximumAge)
    {
        System.DateOnly birthDate = BirthDateFromMinimumAgeAndMaximumAge(
            minimumAge,
            maximumAge);
        return new BirthDateAge(birthDate, Age(birthDate));
    }

    /// <summary>
    /// Generates a locale-provided job title.
    /// </summary>
    /// <returns>A weighted job-title label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobTitle()
    {
        return PersonWorkGenerator.JobTitle(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided job descriptor.
    /// </summary>
    /// <returns>A weighted job descriptor.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobDescriptor()
    {
        return PersonWorkGenerator.JobDescriptor(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided occupational area.
    /// </summary>
    /// <returns>A weighted job-area label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobArea()
    {
        return PersonWorkGenerator.JobArea(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided employment arrangement type.
    /// </summary>
    /// <returns>A weighted job-type label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobType()
    {
        return PersonWorkGenerator.JobType(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided job seniority level.
    /// </summary>
    /// <returns>A weighted seniority label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobSeniority()
    {
        return PersonWorkGenerator.JobSeniority(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided organizational department.
    /// </summary>
    /// <returns>A weighted department label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobDepartment()
    {
        return PersonWorkGenerator.JobDepartment(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided employment-status label.
    /// </summary>
    /// <returns>A weighted employment-status label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string JobStatus()
    {
        return PersonWorkGenerator.JobStatus(Context, WorkRandomValue);
    }

    /// <summary>
    /// Generates a locale-provided salary-band label.
    /// </summary>
    /// <remarks>The invariant labels are nonnumeric and make no claim about currency or compensation amount.</remarks>
    /// <returns>A weighted salary-band label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string SalaryBand()
    {
        return PersonWorkGenerator.SalaryBand(Context, WorkRandomValue);
    }

    /// <summary>Generates a correlated locale-provided work profile.</summary>
    /// <returns>A new synthetic work profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public PersonWorkProfile Work()
    {
        return PersonWorkGenerator.Record(Context, WorkRandomValue);
    }

    /// <summary>Generates a correlated synthetic education profile through the education locale data.</summary>
    /// <returns>A new synthetic student profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public StudentProfile Education()
    {
        return EducationGenerator.Record(Context, EducationRandomValue);
    }

    /// <summary>
    /// Generates a correlated synthetic person profile.
    /// </summary>
    /// <remarks>Contact values use reserved or clearly fictional fixtures and make no claim of deliverability.</remarks>
    /// <returns>A new correlated person profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale profile data is unavailable or malformed.</exception>
    public PersonProfile Record()
    {
        return PersonProfileGenerator.Person(Context, ProfileRandomValue);
    }

    /// <summary>
    /// Generates a correlated synthetic household whose members share its contact location.
    /// </summary>
    /// <remarks>Contact values use reserved or clearly fictional fixtures and make no claim of deliverability.</remarks>
    /// <returns>A new correlated household profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale profile data is unavailable or malformed.</exception>
    public HouseholdProfile Household()
    {
        return PersonProfileGenerator.Household(Context, ProfileRandomValue);
    }
}
