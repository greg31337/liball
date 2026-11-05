using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides company generation operations.
/// </summary>
public class CompanyProvider : ICompanyProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal CompanyProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("company");
    }

    /// <summary>Generates a localized, explicitly synthetic company name.</summary>
    /// <remarks>The name is composed from fictional vocabulary and does not claim uniqueness, registration, or trademark clearance.</remarks>
    /// <returns>A deterministic company name containing a localized legal-form abbreviation.</returns>
    public string Name()
    {
        return CompanyGenerator.Name(Context, RandomValue);
    }

    /// <summary>Selects a localized company legal-form abbreviation.</summary>
    /// <returns>A deterministic abbreviation suitable for synthetic company-name fixtures.</returns>
    public string Suffix()
    {
        return CompanyGenerator.Suffix(Context, RandomValue);
    }

    /// <summary>Selects a localized company legal-form name.</summary>
    /// <returns>A deterministic full legal-form label.</returns>
    public string LegalForm()
    {
        return CompanyGenerator.LegalForm(Context, RandomValue);
    }

    /// <summary>Selects a localized generic industry label.</summary>
    /// <remarks>The label is fictional classification text and is not an official registry or statistical-code assignment.</remarks>
    /// <returns>A deterministic industry label.</returns>
    public string Industry()
    {
        return CompanyGenerator.Industry(Context, RandomValue);
    }

    /// <summary>Selects a localized company department label.</summary>
    /// <returns>A deterministic department label.</returns>
    public string Department()
    {
        return CompanyGenerator.Department(Context, RandomValue);
    }

    /// <summary>Selects a project-authored catch-phrase descriptor.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic descriptor suitable for composing synthetic copy.</returns>
    public string CatchDescriptor()
    {
        return CompanyGenerator.CatchDescriptor(Context, RandomValue);
    }

    /// <summary>Selects a project-authored catch-phrase subject.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic subject suitable for composing synthetic copy.</returns>
    public string CatchSubject()
    {
        return CompanyGenerator.CatchSubject(Context, RandomValue);
    }

    /// <summary>Selects a project-authored catch-phrase benefit.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic benefit suitable for composing synthetic copy.</returns>
    public string CatchBenefit()
    {
        return CompanyGenerator.CatchBenefit(Context, RandomValue);
    }

    /// <summary>Composes a synthetic catch phrase from three project-authored vocabularies.</summary>
    /// <remarks>The result is newly composed fixture text, not copied marketing copy or a factual claim.</remarks>
    /// <returns>A deterministic descriptor, subject, and benefit composition.</returns>
    public string CatchPhrase()
    {
        return CompanyGenerator.CatchPhrase(Context, RandomValue);
    }

    /// <summary>Selects a project-authored buzz-phrase verb.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic verb suitable for composing synthetic copy.</returns>
    public string BuzzVerb()
    {
        return CompanyGenerator.BuzzVerb(Context, RandomValue);
    }

    /// <summary>Selects a project-authored buzz-phrase adjective.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic adjective suitable for composing synthetic copy.</returns>
    public string BuzzAdjective()
    {
        return CompanyGenerator.BuzzAdjective(Context, RandomValue);
    }

    /// <summary>Selects a project-authored buzz-phrase noun.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic noun suitable for composing synthetic copy.</returns>
    public string BuzzNoun()
    {
        return CompanyGenerator.BuzzNoun(Context, RandomValue);
    }

    /// <summary>Composes a synthetic buzz phrase from three project-authored vocabularies.</summary>
    /// <remarks>The result is newly composed fixture text, not copied marketing copy or a factual claim.</remarks>
    /// <returns>A deterministic verb, adjective, and noun composition.</returns>
    public string BuzzPhrase()
    {
        return CompanyGenerator.BuzzPhrase(Context, RandomValue);
    }

    /// <summary>Composes a project-authored synthetic company slogan.</summary>
    /// <remarks>The result is generic fixture copy and does not reproduce or claim a real slogan.</remarks>
    /// <returns>A deterministic purpose-and-outcome slogan.</returns>
    public string Slogan()
    {
        return CompanyGenerator.Slogan(Context, RandomValue);
    }

    /// <summary>Generates a localized business-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and does not claim issuance, registration, ownership, or operational validity.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    public string BusinessId()
    {
        return CompanyGenerator.BusinessIdentifier(Context, RandomValue);
    }

    /// <summary>Generates a localized business-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and does not claim issuance, registration, ownership, or operational validity.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    public string BusinessIdentifier()
    {
        return CompanyGenerator.BusinessIdentifier(Context, RandomValue);
    }

    /// <summary>Generates a localized tax-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and must not be used for filing, registration, identity, or tax purposes.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    public string TaxId()
    {
        return CompanyGenerator.TaxIdentifier(Context, RandomValue);
    }

    /// <summary>Generates a localized tax-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and must not be used for filing, registration, identity, or tax purposes.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    public string TaxIdentifier()
    {
        return CompanyGenerator.TaxIdentifier(Context, RandomValue);
    }

    /// <summary>Validates a business identifier against the resolved locale's structural presentation.</summary>
    /// <param name="value">The business identifier-shaped value.</param>
    /// <returns><c>true</c> when the shape and any implemented checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool IsBusinessIdValid(string value)
    {
        return CompanyGenerator.IsBusinessIdentifierValid(Context, value);
    }

    /// <summary>Validates a tax identifier against the resolved locale's structural presentation.</summary>
    /// <param name="value">The tax identifier-shaped value.</param>
    /// <returns><c>true</c> when the shape and any implemented checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool IsTaxIdValid(string value)
    {
        return CompanyGenerator.IsTaxIdentifierValid(Context, value);
    }

    /// <summary>Generates correlated, localized business and tax identifier shapes.</summary>
    /// <remarks>The record is explicitly synthetic and never claims entity or tax registration.</remarks>
    /// <returns>A deterministic identifier record with structural validation and safety metadata.</returns>
    public CompanyIdentifierRecord Identifiers()
    {
        return CompanyGenerator.Identifiers(Context, RandomValue);
    }

    /// <summary>Generates a correlated, explicitly synthetic company profile.</summary>
    /// <remarks>
    /// The name drives a reserved example domain and mailbox. Phone, address, and
    /// identifier records retain their own locale, region, jurisdiction, and safety
    /// metadata without claiming registration, reachability, or deliverability.
    /// </remarks>
    /// <returns>A deterministic name, domain, email, industry, phrase, phone, address, and identifier fixture.</returns>
    public CompanyProfile Profile()
    {
        return CompanyGenerator.Profile(Context, RandomValue);
    }
}
