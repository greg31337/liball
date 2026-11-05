using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="CompanyProvider"/>.</summary>
public interface ICompanyProvider
{
    /// <summary>Generates a localized, explicitly synthetic company name.</summary>
    /// <remarks>The name is composed from fictional vocabulary and does not claim uniqueness, registration, or trademark clearance.</remarks>
    /// <returns>A deterministic company name containing a localized legal-form abbreviation.</returns>
    string Name();
    /// <summary>Selects a localized company legal-form abbreviation.</summary>
    /// <returns>A deterministic abbreviation suitable for synthetic company-name fixtures.</returns>
    string Suffix();
    /// <summary>Selects a localized company legal-form name.</summary>
    /// <returns>A deterministic full legal-form label.</returns>
    string LegalForm();
    /// <summary>Selects a localized generic industry label.</summary>
    /// <remarks>The label is fictional classification text and is not an official registry or statistical-code assignment.</remarks>
    /// <returns>A deterministic industry label.</returns>
    string Industry();
    /// <summary>Selects a localized company department label.</summary>
    /// <returns>A deterministic department label.</returns>
    string Department();
    /// <summary>Selects a project-authored catch-phrase descriptor.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic descriptor suitable for composing synthetic copy.</returns>
    string CatchDescriptor();
    /// <summary>Selects a project-authored catch-phrase subject.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic subject suitable for composing synthetic copy.</returns>
    string CatchSubject();
    /// <summary>Selects a project-authored catch-phrase benefit.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic benefit suitable for composing synthetic copy.</returns>
    string CatchBenefit();
    /// <summary>Composes a synthetic catch phrase from three project-authored vocabularies.</summary>
    /// <remarks>The result is newly composed fixture text, not copied marketing copy or a factual claim.</remarks>
    /// <returns>A deterministic descriptor, subject, and benefit composition.</returns>
    string CatchPhrase();
    /// <summary>Selects a project-authored buzz-phrase verb.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic verb suitable for composing synthetic copy.</returns>
    string BuzzVerb();
    /// <summary>Selects a project-authored buzz-phrase adjective.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic adjective suitable for composing synthetic copy.</returns>
    string BuzzAdjective();
    /// <summary>Selects a project-authored buzz-phrase noun.</summary>
    /// <remarks>The component vocabulary is original project material distributed under the project license.</remarks>
    /// <returns>A deterministic noun suitable for composing synthetic copy.</returns>
    string BuzzNoun();
    /// <summary>Composes a synthetic buzz phrase from three project-authored vocabularies.</summary>
    /// <remarks>The result is newly composed fixture text, not copied marketing copy or a factual claim.</remarks>
    /// <returns>A deterministic verb, adjective, and noun composition.</returns>
    string BuzzPhrase();
    /// <summary>Composes a project-authored synthetic company slogan.</summary>
    /// <remarks>The result is generic fixture copy and does not reproduce or claim a real slogan.</remarks>
    /// <returns>A deterministic purpose-and-outcome slogan.</returns>
    string Slogan();
    /// <summary>Generates a localized business-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and does not claim issuance, registration, ownership, or operational validity.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    string BusinessId();
    /// <summary>Generates a localized business-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and does not claim issuance, registration, ownership, or operational validity.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    string BusinessIdentifier();
    /// <summary>Generates a localized tax-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and must not be used for filing, registration, identity, or tax purposes.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    string TaxId();
    /// <summary>Generates a localized tax-registration identifier shape.</summary>
    /// <remarks>The value is synthetic and must not be used for filing, registration, identity, or tax purposes.</remarks>
    /// <returns>A deterministic country-shaped identifier.</returns>
    string TaxIdentifier();
    /// <summary>Validates a business identifier against the resolved locale's structural presentation.</summary>
    /// <param name="value">The business identifier-shaped value.</param>
    /// <returns><c>true</c> when the shape and any implemented checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsBusinessIdValid(string value);
    /// <summary>Validates a tax identifier against the resolved locale's structural presentation.</summary>
    /// <param name="value">The tax identifier-shaped value.</param>
    /// <returns><c>true</c> when the shape and any implemented checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsTaxIdValid(string value);
    /// <summary>Generates correlated, localized business and tax identifier shapes.</summary>
    /// <remarks>The record is explicitly synthetic and never claims entity or tax registration.</remarks>
    /// <returns>A deterministic identifier record with structural validation and safety metadata.</returns>
    CompanyIdentifierRecord Identifiers();
    /// <summary>Generates a correlated, explicitly synthetic company profile.</summary>
    /// <remarks>
    /// The name drives a reserved example domain and mailbox. Phone, address, and
    /// identifier records retain their own locale, region, jurisdiction, and safety
    /// metadata without claiming registration, reachability, or deliverability.
    /// </remarks>
    /// <returns>A deterministic name, domain, email, industry, phrase, phone, address, and identifier fixture.</returns>
    CompanyProfile Profile();
}
