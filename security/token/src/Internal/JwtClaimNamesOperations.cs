using System;
namespace token;

/// <summary>
/// Provides registered JWT claim names and exact-name lookup.
/// </summary>
internal static class JwtClaimNamesOperations
{
    /// <summary>The issuer claim.</summary>
    internal const string Issuer = JwtClaimNames.Issuer;

    /// <summary>The subject claim.</summary>
    internal const string Subject = JwtClaimNames.Subject;

    /// <summary>The audience claim.</summary>
    internal const string Audience = JwtClaimNames.Audience;

    /// <summary>The expiration time claim.</summary>
    internal const string ExpirationTime = JwtClaimNames.ExpirationTime;

    /// <summary>The not-before claim.</summary>
    internal const string NotBefore = JwtClaimNames.NotBefore;

    /// <summary>The issued-at claim.</summary>
    internal const string IssuedAt = JwtClaimNames.IssuedAt;

    /// <summary>The JWT identifier claim.</summary>
    internal const string JwtId = JwtClaimNames.JwtId;

    /// <summary>The authorized party claim.</summary>
    internal const string AuthorizedParty = JwtClaimNames.AuthorizedParty;

    /// <summary>The nonce claim.</summary>
    internal const string Nonce = JwtClaimNames.Nonce;

    /// <summary>The authentication time claim.</summary>
    internal const string AuthenticationTime = JwtClaimNames.AuthenticationTime;

    /// <summary>The access token hash claim.</summary>
    internal const string AccessTokenHash = JwtClaimNames.AccessTokenHash;

    /// <summary>The authorization code hash claim.</summary>
    internal const string CodeHash = JwtClaimNames.CodeHash;

    /// <summary>The authentication context class reference claim.</summary>
    internal const string AuthenticationContextClassReference = JwtClaimNames.AuthenticationContextClassReference;

    /// <summary>The authentication methods references claim.</summary>
    internal const string AuthenticationMethodsReferences = JwtClaimNames.AuthenticationMethodsReferences;

    /// <summary>The confirmation claim.</summary>
    internal const string Confirmation = JwtClaimNames.Confirmation;

    /// <summary>The OAuth scope claim.</summary>
    internal const string Scope = JwtClaimNames.Scope;

    /// <summary>The OAuth client identifier claim.</summary>
    internal const string ClientId = JwtClaimNames.ClientId;

    /// <summary>The DPoP HTTP method claim.</summary>
    internal const string HttpMethod = JwtClaimNames.HttpMethod;

    /// <summary>The DPoP HTTP target URI claim.</summary>
    internal const string HttpUri = JwtClaimNames.HttpUri;

    /// <summary>The DPoP access token hash claim.</summary>
    internal const string DpopAccessTokenHash = JwtClaimNames.DpopAccessTokenHash;

    /// <summary>The Security Event Token events claim.</summary>
    internal const string SecurityEvents = JwtClaimNames.SecurityEvents;

    /// <summary>The Security Event Token time-of-event claim.</summary>
    internal const string TimeOfEvent = JwtClaimNames.TimeOfEvent;

    /// <summary>The Security Event Token transaction identifier claim.</summary>
    internal const string TransactionId = JwtClaimNames.TransactionId;

    /// <summary>The SD-JWT array of object-property Disclosure digests.</summary>
    internal const string SelectiveDisclosureDigests = JwtClaimNames.SelectiveDisclosureDigests;

    /// <summary>The SD-JWT array-element Disclosure digest placeholder.</summary>
    internal const string SelectiveDisclosureArrayElement = JwtClaimNames.SelectiveDisclosureArrayElement;

    /// <summary>The SD-JWT Disclosure hash algorithm claim.</summary>
    internal const string SelectiveDisclosureAlgorithm = JwtClaimNames.SelectiveDisclosureAlgorithm;

    /// <summary>The SD-JWT Key Binding JWT hash claim.</summary>
    internal const string SelectiveDisclosureHash = JwtClaimNames.SelectiveDisclosureHash;

    static readonly string[] all = new string[]
    {
        Issuer, Subject, Audience, ExpirationTime, NotBefore, IssuedAt, JwtId,
        "name", "given_name", "family_name", "middle_name", "nickname",
        "preferred_username", "profile", "picture", "website", "email",
        "email_verified", "gender", "birthdate", "zoneinfo", "locale",
        "phone_number", "phone_number_verified", "address", "updated_at",
        AuthorizedParty, Nonce, AuthenticationTime, AccessTokenHash, CodeHash,
        AuthenticationContextClassReference, AuthenticationMethodsReferences,
        "sub_jwk", Confirmation, "sip_from_tag", "sip_date", "sip_callid",
        "sip_cseq_num", "sip_via_branch", "orig", "dest", "mky",
        SecurityEvents, TimeOfEvent, TransactionId, "rph", "sid", "vot", "vtm",
        "attest", "origid",
        "act", Scope, ClientId, "may_act", "jcard", "at_use_nbr", "div", "opt",
        "vc", "vp", "sph", "ace_profile", "cnonce", "exi", "roles", "groups",
        "entitlements", "token_introspection", "eat_nonce", "ueid", "sueids",
        "oemid", "hwmodel", "hwversion", "oemboot", "dbgstat", "location",
        "eat_profile", "submods", "uptime", "bootcount", "bootseed", "dloas",
        "swname", "swversion", "manifests", "measurements", "measres", "intuse",
        "cdniv", "cdnicrit", "cdniip", "cdniuc", "cdniets", "cdnistt", "cdnistd",
        "sig_val_claims", "authorization_details", "verified_claims", "place_of_birth",
        "nationalities", "birth_family_name", "birth_given_name", "birth_middle_name",
        "salutation", "title", "msisdn", "also_known_as", HttpMethod, HttpUri,
        DpopAccessTokenHash,
        "atc", "sub_id", "rcd", "rcdi", "crn", "msgi", "_claim_names",
        "_claim_sources", "rdap_allowed_purposes", "rdap_dnt_allowed", "geohash",
        SelectiveDisclosureDigests, SelectiveDisclosureArrayElement,
        SelectiveDisclosureAlgorithm, SelectiveDisclosureHash,
        "consumerPlmnId", "consumerSnpnId",
        "producerPlmnId", "producerSnpnId", "producerSnssaiList", "producerNsiList",
        "producerNfSetId", "producerNfServiceSetId", "sourceNfInstanceId",
        "analyticsIdList", "resOwnerId", "cmw", "jwks", "metadata", "constraints",
        "crit", "ref", "delegation", "logo_uri", "authority_hints",
        "trust_anchor_hints", "trust_marks", "trust_mark_issuers", "trust_mark_owners",
        "metadata_policy", "metadata_policy_crit", "source_endpoint", "keys",
        "trust_mark_type", "trust_chain", "trust_anchor", "status", "status_list",
        "ttl", "stpl"
    };

    /// <summary>
    /// Determines whether an exact, case-sensitive JWT claim name is registered.
    /// </summary>
    /// <param name="name">The JWT claim name.</param>
    /// <returns><c>true</c> when the name is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(string name)
    {
        return RegistryLookup.Contains(all, name, false);
    }

    /// <summary>
    /// Returns all registered JWT claim names known by this library.
    /// </summary>
    /// <returns>A new array containing the claim names.</returns>
    internal static string[] GetAll()
    {
        return RegistryLookup.Copy(all);
    }
}
