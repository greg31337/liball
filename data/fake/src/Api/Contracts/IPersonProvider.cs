using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="PersonProvider"/>.</summary>
public interface IPersonProvider
{
    /// <summary>
    /// Generates a given name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided given name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string FirstName();
    /// <summary>
    /// Generates a given name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided given name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    string FirstNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a middle name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided middle name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no middle names.</exception>
    string MiddleName();
    /// <summary>
    /// Generates a middle name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided middle name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    string MiddleNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a family name using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided family name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string LastName();
    /// <summary>
    /// Generates a family name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided family name applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    string LastNameFromSexCategory(string sexCategory);
    /// <summary>Generates a maiden family-name component.</summary>
    /// <returns>A locale-provided maiden name.</returns>
    string MaidenName();
    /// <summary>Generates a maiden name for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided maiden name applicable to the category.</returns>
    string MaidenNameFromSexCategory(string sexCategory);
    /// <summary>Generates a patronymic name component.</summary>
    /// <returns>A locale-provided patronymic.</returns>
    string Patronymic();
    /// <summary>Generates a patronymic for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided patronymic applicable to the category.</returns>
    string PatronymicFromSexCategory(string sexCategory);
    /// <summary>Generates a matronymic name component.</summary>
    /// <returns>A locale-provided matronymic.</returns>
    string Matronymic();
    /// <summary>Generates a matronymic for an explicit locale naming category.</summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided matronymic applicable to the category.</returns>
    string MatronymicFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a name prefix using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided name prefix.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no prefixes.</exception>
    string Prefix();
    /// <summary>
    /// Generates a name prefix for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided prefix applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    string PrefixFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a name suffix using the configured locale and its weighted naming categories.
    /// </summary>
    /// <returns>A locale-provided name suffix.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no suffixes.</exception>
    string Suffix();
    /// <summary>
    /// Generates a name suffix for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A locale-provided suffix applicable to the category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or has no applicable value.</exception>
    string SuffixFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates the first Unicode text element of a locale-provided given name.
    /// </summary>
    /// <returns>A complete initial text element without an added punctuation mark.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string Initial();
    /// <summary>
    /// Generates the first Unicode text element of a given name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A complete initial text element without an added punctuation mark.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string InitialFromSexCategory(string sexCategory);
    /// <summary>Generates two initials from correlated given and family name components.</summary>
    /// <returns>Two complete Unicode initial text elements without punctuation.</returns>
    string Initials();
    /// <summary>Generates a requested number of correlated name initials.</summary>
    /// <param name="count">The nonnegative number of initials.</param>
    /// <returns>Complete Unicode initial text elements without punctuation.</returns>
    string InitialsFromCount(int count);
    /// <summary>
    /// Generates a name using a weighted full-name template from the configured locale.
    /// </summary>
    /// <returns>A correlated full name in locale-defined component order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string FullName();
    /// <summary>
    /// Generates a full name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated full name in locale-defined component order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string FullNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a name using a weighted informal display template from the configured locale.
    /// </summary>
    /// <returns>A correlated informal display name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string DisplayName();
    /// <summary>
    /// Generates an informal display name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated informal display name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string DisplayNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a name using a weighted formal template from the configured locale.
    /// </summary>
    /// <returns>A correlated formal name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string FormalName();
    /// <summary>
    /// Generates a formal name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated formal name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string FormalNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a full name in the resolved locale's native script and component order.
    /// </summary>
    /// <returns>A correlated native-form full name.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string NativeName();
    /// <summary>
    /// Generates a native-form full name for an explicit locale naming category.
    /// </summary>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated native-form full name.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string NativeNameFromSexCategory(string sexCategory);
    /// <summary>
    /// Generates a full name and transliterates supported scripts to an ASCII Latin form.
    /// </summary>
    /// <remarks>Characters outside the declared Greek, Cyrillic, Arabic, Hebrew, and Latin mappings are omitted from the transliterated result.</remarks>
    /// <returns>A correlated transliterated full name in locale-defined component order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string TransliteratedName();
    /// <summary>
    /// Generates and transliterates a full name for an explicit locale naming category.
    /// </summary>
    /// <remarks>Characters outside the declared Greek, Cyrillic, Arabic, Hebrew, and Latin mappings are omitted from the transliterated result.</remarks>
    /// <param name="sexCategory">A naming category declared by the resolved locale data.</param>
    /// <returns>A correlated transliterated full name in locale-defined component order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>sexCategory</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>sexCategory</c> is empty, whitespace, or unknown.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string TransliteratedNameFromSexCategory(string sexCategory);
    /// <summary>Generates a given name in the resolved locale's native script.</summary>
    /// <returns>A native-script given name.</returns>
    string NativeFirstName();
    /// <summary>Generates a middle name in the resolved locale's native script.</summary>
    /// <returns>A native-script middle name.</returns>
    string NativeMiddleName();
    /// <summary>Generates a family name in the resolved locale's native script.</summary>
    /// <returns>A native-script family name.</returns>
    string NativeLastName();
    /// <summary>Generates a maiden name in the resolved locale's native script.</summary>
    /// <returns>A native-script maiden name.</returns>
    string NativeMaidenName();
    /// <summary>Generates a patronymic in the resolved locale's native script.</summary>
    /// <returns>A native-script patronymic.</returns>
    string NativePatronymic();
    /// <summary>Generates a matronymic in the resolved locale's native script.</summary>
    /// <returns>A native-script matronymic.</returns>
    string NativeMatronymic();
    /// <summary>Generates and transliterates a given-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedFirstName();
    /// <summary>Generates and transliterates a middle-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedMiddleName();
    /// <summary>Generates and transliterates a family-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedLastName();
    /// <summary>Generates and transliterates a maiden-name component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedMaidenName();
    /// <summary>Generates and transliterates a patronymic component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedPatronymic();
    /// <summary>Generates and transliterates a matronymic component.</summary>
    /// <returns>An ASCII Latin transliteration where supported.</returns>
    string TransliteratedMatronymic();
    /// <summary>
    /// Generates a requested valid unusual name case from the configured locale.
    /// </summary>
    /// <param name="nameCase">The structural or textual name case to generate.</param>
    /// <returns>A weighted locale-provided name exhibiting the requested case.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>nameCase</c> is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable, malformed, or does not define the requested case.</exception>
    string NameCase(PersonNameCase nameCase);
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
    string NameCaseFromNameCaseAndSexCategory(PersonNameCase nameCase, string sexCategory);
    /// <summary>
    /// Generates a locale-provided gender identity label independently of names and pronouns.
    /// </summary>
    /// <remarks>The bundled weights are synthetic choices and do not represent population statistics.</remarks>
    /// <returns>A gender identity label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string Gender();
    /// <summary>
    /// Generates one correlated locale-provided pronoun set independently of gender and names.
    /// </summary>
    /// <returns>A new pronoun-set value containing all grammatical forms.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    PronounSet Pronouns();
    /// <summary>
    /// Generates a short, neutral, project-authored biography.
    /// </summary>
    /// <returns>A locale-provided biography sentence.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string Biography();
    /// <summary>
    /// Generates a locale-provided zodiac-sign label independently of any birth date.
    /// </summary>
    /// <returns>A zodiac-sign label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string ZodiacSign();
    /// <summary>
    /// Generates a locale-provided marital-status label.
    /// </summary>
    /// <returns>A marital-status label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string MaritalStatus();
    /// <summary>
    /// Generates a locale-provided interpersonal relationship label.
    /// </summary>
    /// <returns>A relationship label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string Relationship();
    /// <summary>
    /// Generates a locale-provided role within a household.
    /// </summary>
    /// <returns>A household-role label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string HouseholdRole();
    /// <summary>
    /// Generates a locale-provided household-type label.
    /// </summary>
    /// <returns>A household-type label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string HouseholdType();
    /// <summary>
    /// Generates a broad demographic label from a requested locale-provided category.
    /// </summary>
    /// <remarks>Categories are independent scalar fixtures and are not correlated with other person values.</remarks>
    /// <param name="category">A category returned by <c>DemographicCategories</c>.</param>
    /// <returns>A weighted value from the requested demographic category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>category</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>category</c> is empty, whitespace, or unavailable.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string Demographic(string category);
    /// <summary>
    /// Returns the locale-provided broad demographic category identifiers.
    /// </summary>
    /// <returns>A new array in ordinal category order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string[] DemographicCategories();
    /// <summary>Generates a birth date whose completed age is between 18 and 80.</summary>
    /// <returns>A calendar birth date in the default adult age range.</returns>
    System.DateOnly BirthDate();
    /// <summary>Generates a birth date with an exact completed age.</summary>
    /// <param name="age">The nonnegative completed age.</param>
    /// <returns>A birth date having the requested age.</returns>
    System.DateOnly BirthDateFromAge(int age);
    /// <summary>Generates a birth date within an inclusive completed-age range.</summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth date whose completed age is in the requested range.</returns>
    System.DateOnly BirthDateFromMinimumAgeAndMaximumAge(int minimumAge, int maximumAge);
    /// <summary>Calculates completed age at the captured reference date.</summary>
    /// <param name="birthDate">The birth date to evaluate.</param>
    /// <returns>The number of completed calendar years.</returns>
    int Age(System.DateOnly birthDate);
    /// <summary>Generates a correlated birth date and completed age.</summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth-date and age pair evaluated at the captured reference date.</returns>
    BirthDateAge BirthDateAndAge(int minimumAge, int maximumAge);
    /// <summary>
    /// Generates a locale-provided job title.
    /// </summary>
    /// <returns>A weighted job-title label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobTitle();
    /// <summary>
    /// Generates a locale-provided job descriptor.
    /// </summary>
    /// <returns>A weighted job descriptor.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobDescriptor();
    /// <summary>
    /// Generates a locale-provided occupational area.
    /// </summary>
    /// <returns>A weighted job-area label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobArea();
    /// <summary>
    /// Generates a locale-provided employment arrangement type.
    /// </summary>
    /// <returns>A weighted job-type label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobType();
    /// <summary>
    /// Generates a locale-provided job seniority level.
    /// </summary>
    /// <returns>A weighted seniority label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobSeniority();
    /// <summary>
    /// Generates a locale-provided organizational department.
    /// </summary>
    /// <returns>A weighted department label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobDepartment();
    /// <summary>
    /// Generates a locale-provided employment-status label.
    /// </summary>
    /// <returns>A weighted employment-status label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string JobStatus();
    /// <summary>
    /// Generates a locale-provided salary-band label.
    /// </summary>
    /// <remarks>The invariant labels are nonnumeric and make no claim about currency or compensation amount.</remarks>
    /// <returns>A weighted salary-band label.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    string SalaryBand();
    /// <summary>Generates a correlated locale-provided work profile.</summary>
    /// <returns>A new synthetic work profile.</returns>
    PersonWorkProfile Work();
    /// <summary>Generates a correlated synthetic education profile.</summary>
    /// <returns>A new synthetic student profile.</returns>
    StudentProfile Education();
    /// <summary>
    /// Generates a correlated synthetic person profile.
    /// </summary>
    /// <remarks>Contact values use reserved or clearly fictional fixtures and make no claim of deliverability.</remarks>
    /// <returns>A new correlated person profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale profile data is unavailable or malformed.</exception>
    PersonProfile Record();
    /// <summary>
    /// Generates a correlated synthetic household whose members share its contact location.
    /// </summary>
    /// <remarks>Contact values use reserved or clearly fictional fixtures and make no claim of deliverability.</remarks>
    /// <returns>A new correlated household profile.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale profile data is unavailable or malformed.</exception>
    HouseholdProfile Household();
}
