# fake

`fake` is an offline, deterministic fake-data library for .NET 10. One `Fake`
instance provides random primitives, localized people and addresses,
standards-shaped identifiers, correlated profiles, schemas, difficult edge
cases, and text exports through a consistent method-based API.

The product assembly uses only the .NET runtime and BCL. Generation does not
perform network requests, open databases, send messages, or execute generated
commands.

## Quick start

The repository currently exposes the library project at `src/fake.csproj`.
Reference it from another project while developing locally:

```xml
<ItemGroup>
  <ProjectReference Include="../fake/src/fake.csproj" />
</ItemGroup>
```

Create a seeded generator and call a provider method:

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 42L });

string name = data.Person().FullName();
string email = data.Internet().Email();
string address = data.Address().FullAddress();
string identifier = data.Identifier().Uuid7();
PersonProfile person = data.Profile().Person();
```

Provider objects are cached by the root instance. Common entry points include
`Random()`, `Primitive()`, `Text()`, `Identifier()`, `Date()`, `Locale()`,
`Person()`, `Address()`, `Internet()`, `Phone()`, `Device()`, `Finance()`,
`Company()`, `Commerce()`, `System()`, `Database()`, `Git()`, and the thematic
providers. `Profile()`, `Schema()`, `Edge()`, and `Export()` compose those
lower-level APIs.

## API examples

These examples use provider methods, concrete public classes, public fields,
built-in arrays and collections, and explicit method variants. They do not depend on
LINQ, extension methods, generated properties, authored generic types, or
optional arguments.

### Random, primitive, and text values

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 100L });

int quantity = data.Random().IntFromMinimumAndMaximum(1, 11);
decimal ratio = data.Random().DecimalFromMinimumAndMaximum(0m, 1m);
byte[] bytes = data.Random().Bytes(16);

string digits = data.Primitive().Digits(12);
string hexadecimal = data.Primitive().HexFromCount(24);
string sentence = data.Text().SentenceFromWordCount(8);
string slug = data.Text().SlugFromWordCount(4);
string masked = data.Text().Format("INV-####-????");
```

Bounds are explicit. Two-argument numeric ranges use an inclusive minimum and
exclusive maximum unless a method accepts a boundary choice. Counts and
output sizes are checked against the captured options.

### Selection, sequences, and uniqueness

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 101L });
string[] states = new string[] { "new", "ready", "complete" };

string selected = data.Random().Choice(states);
string[] shuffled = data.Random().Shuffle(states);

SequenceState sequence = data.Random().SequenceFromNameAndStartAndIncrement("invoice", 1000L, 1L);
long firstNumber = sequence.Next();
long[] nextPage = sequence.Page(0L, 5);

UniqueScope emails = data.Random().Unique("customer-emails");
string email = emails.Next(() => data.Internet().ExampleEmail());
```

A uniqueness callback is retried only up to `UniquenessRetryLimit`; exhaustion
raises `FakeException`. Sequence and uniqueness state is named and scoped to
the root generator.

### Dates and time

```csharp
using fake;

FakeOptions options = new FakeOptions { Seed = 102L };
options.ReferenceTime = new DateTimeOffset(
    2045, 7, 8, 9, 10, 11, TimeSpan.Zero);
Fake data = new Fake(options);

DateTimeOffset past = data.Date().PastFromMaximumAge(TimeSpan.FromDays(30));
DateTimeOffset future = data.Date().FutureFromMaximumLead(TimeSpan.FromDays(30));
DateOnly businessDay = data.Date().BusinessDay();
BirthDateAge birth = data.Date().BirthDateAndAge(21, 65);
string timestamp = data.Date().Rfc3339FromPrecision(TemporalPrecision.Milliseconds);
string zone = data.Date().TimeZoneId();
```

Relative values use the captured reference clock, not `DateTime.Now` or
`DateTimeOffset.Now`.

### Standards-shaped identifiers and checksums

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 103L });
IdentifierProvider identifiers = data.Identifier();

string uuid4 = identifiers.Uuid4();
string uuid7 = identifiers.Uuid7();
string ulid = identifiers.Ulid();
string isbn13 = identifiers.Isbn13();
string gtin = identifiers.GtinFromLength(14);
string imei = identifiers.Imei();
string vin = identifiers.Vin();

bool isbnIsValid = identifiers.ValidateIdentifier(
    isbn13, IdentifierKind.Isbn13).ChecksumValidity == ValidityMode.Valid;
bool imeiIsValid = identifiers.ValidateImei(imei);

LuhnChecksum luhn = data.Checksum().Luhn();
string checkedNumber = luhn.Append("7992739871");
bool checksumIsValid = luhn.Validate(checkedNumber);
```

Append-style checksum engines also share the small `IChecksum` contract.
Use the kind-based lookup when application code should be independent of a
specific algorithm, while the named methods remain available when a concrete
engine or custom weights are needed:

```csharp
IChecksum checksum = data.Checksum().Engine(ChecksumKind.Luhn);
string complete = checksum.Append("7992739871");
bool valid = checksum.Validate(complete);
```

> **Safety warning:** these APIs generate structure, formatting, and checksums.
> A valid-looking UUID, ISBN, GTIN, IMEI, VIN, card number, IBAN, account number,
> token, or other identifier is not an issued credential or account. It does
> not prove assignment, ownership, authorization, funding, or authenticity.
> Never use generated values as secrets, private keys, login credentials, or
> evidence that an external resource exists.

### Reserved contact and network fixtures

```csharp
using fake;

FakeOptions options = new FakeOptions { Locale = "en-US", Seed = 104L };
options.SafetyMode = SafetyMode.Reserved;
Fake data = new Fake(options);

AddressRecord address = data.Address().Record();
PhoneRecord phone = data.Phone().Record();
string email = data.Internet().ExampleEmail();
string domain = data.Internet().ExampleDomain();
string ipv4 = data.Internet().Ipv4FromKind(NetworkAddressKind.Documentation);
string ipv6 = data.Internet().Ipv6FromKind(NetworkAddressKind.Documentation);
```

Reserved mode minimizes accidental contact with real systems, but callers must
still treat the output as test data and avoid transmitting it to external
services.

### Finance, commerce, and organizations

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 105L });

MoneyRecord money = data.Finance().MoneyFromCurrencyCode("USD");
PaymentRecord payment = data.Finance().PaymentFromCurrencyCode("USD");
CardRecord card = data.Finance().Card();
BankAccountRecord account = data.Finance().BankAccount();

CompanyProfile company = data.Company().Profile();
ProductProfile product = data.Commerce().Profile();
CommerceOrder order = data.Commerce().OrderDetailsFromLineCountAndCurrencyCode(3, "USD");
```

Financial and commerce values are synthetic fixtures. Card and bank shapes are
not authorized, open, payable, or connected to funds. Prices and exchange-like
values do not represent market data.

### Files, developer data, and thematic records

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 106L });

FileRecord file = data.System().File();
string connection = data.Database().ConnectionString();
string commit = data.Git().CommitEntry();
DeviceRecord device = data.Device().Record();

VehicleProfile vehicle = data.Vehicle().Record();
FlightProfile flight = data.Airline().Record();
BookRecord book = data.Book().Record();
FoodRecord food = data.Food().Record();
AnimalRecord animal = data.Animal().Record();
ScienceRecord science = data.Science().Record();
HealthRecord health = data.Health().Record();
StudentProfile student = data.Education().Record();
CultureRecord culture = data.Culture().Record();
```

File paths, connection strings, Git entries, health terms, travel records, and
other thematic outputs are returned as inert data. No file, repository,
database, booking, device, or clinical record is created or queried.

## Seeding and reproducibility

An integer, long, or options seed selects the stable deterministic random
stream. Equal seeds and equal options reproduce the same calls:

```csharp
using fake;

Fake first = new Fake(new FakeOptions { Seed = 1234L });
Fake second = new Fake(new FakeOptions { Seed = 1234L });

string firstName = first.Person().FullName();
string secondName = second.Person().FullName();
```

Each provider owns an isolated named stream. Adding an `Address()` call does
not move the `Person()` stream. This makes seeded fixtures less fragile when a
different provider is introduced elsewhere.

Use a named fork for an independently reproducible subtree:

```csharp
Fake root = new Fake(new FakeOptions { Seed = 1234L });
Fake tenant = root.Fork("tenant/example");

OrderProfile order = tenant.Profile().Order();
```

The same parent seed and fork name produce the same child. Creating a fork does
not consume the parent. A fork captures the parent's locale, fallback, safety,
generation mode, limits, normalization, and reference time, but starts with
fresh provider caches and dataset scopes.

`Reset()`, `Snapshot()`, and `Restore()` operate on the root `Random()` stream:

```csharp
Fake data = new Fake(new FakeOptions { Seed = 1234L });
FakeState checkpoint = data.Snapshot();

int value = data.Random().Int();
data.Restore(checkpoint);
int replayed = data.Random().Int();

data.Reset();
```

Provider streams and dataset-level sequence/unique scopes are intentionally
separate from the root snapshot. Reset those scopes through `Profile()` or
`Schema()` methods such as `ResetSequenceFromNoArguments()` and
`ResetUniqueFromNoArguments()`.

The implementation keeps standards algorithms behind a minimal internal
random-source contract. This preserves the public seeded behavior while making
exact entropy, version-bit, check-digit, and monotonic-state cases testable with
small scripted sources instead of seed searches or subclasses.

String and byte-array seeding is available through
`Random().ReseedFromSeedUsingString(...)` and
`Random().ReseedFromSeedUsingByteArray(...)`.
Set `RandomMode` to `Cryptographic` only when reproducibility is not required;
cryptographic streams do not support deterministic snapshot and restore.

## Options and the reference clock

`Fake` validates and copies `FakeOptions` during creation. Later changes to
the caller's options object do not alter an existing generator.

```csharp
using fake;

FakeOptions options = new FakeOptions
{
    Locale = "fr-CA",
    FallbackLocale = "en",
    Seed = 9001L
};
options.ReferenceTime = new DateTimeOffset(
    2040, 6, 15, 12, 0, 0, TimeSpan.Zero);
options.MaximumCount = 1000;
options.MaximumOutputSize = 100000;
options.UniquenessRetryLimit = 50;

Fake data = new Fake(options);
```

The captured `ReferenceTime` replaces the wall clock for relative dates,
time-bearing identifiers, expiries, timelines, and profiles. Set it explicitly
when fixtures must remain stable over time.

`MaximumCount`, `MaximumOutputSize`, and `UniquenessRetryLimit` bound work and
memory. Invalid arguments use standard argument exceptions with the public
parameter name. Missing data, exhausted uniqueness, and invalid cross-field
state use `FakeException` with deterministic diagnostics.

## Locales and fallback

Locale tags are validated and normalized as BCP 47 tags. With fallback enabled,
a request walks this deterministic order:

1. The exact requested locale.
2. Its progressively less-specific parents.
3. The configured fallback locale and its parents.
4. The invariant pack.

For example, `fr-CA` with fallback `en-US` tries `fr-CA`, `fr`, `en-US`, `en`,
then `invariant`. Duplicate tiers are removed.

```csharp
using fake;

FakeOptions options = new FakeOptions
{
    Locale = "fr-CA",
    FallbackLocale = "en-US",
    Seed = 42L
};
options.StrictnessMode = StrictnessMode.AllowFallback;
Fake data = new Fake(options);

string month = data.Date().MonthName();
FallbackTrace trace = data.Locale().FallbackTrace("person.names");
LocaleCoverage coverage = data.Locale().Coverage("fr-CA");
string[] supported = data.Locale().Supported();
```

`FallbackTrace` reports the requested locale, attempted locales, winning source,
data version, and provenance. `LocaleCoverage` separates native fields from
fallback fields. Set `StrictnessMode.Strict` to require an exact-locale field;
missing exact data then raises `FakeException` instead of falling back.

The bundled native metadata/date locales are:

`ar-EG`, `ar-SA`, `cs-CZ`, `da-DK`, `de-AT`, `de-CH`, `de-DE`, `el-GR`,
`en-AU`, `en-CA`, `en-GB`, `en-US`, `es-ES`, `es-MX`, `fa-IR`, `fi-FI`,
`fr-CA`, `fr-FR`, `he-IL`, `hi-IN`, `id-ID`, `it-IT`, `ja-JP`, `ko-KR`,
`nb-NO`, `nl-NL`, `pl-PL`, `pt-BR`, `pt-PT`, `ru-RU`, `sv-SE`, `th-TH`,
`tr-TR`, `uk-UA`, `vi-VN`, `zh-CN`, `zh-HK`, and `zh-TW`.

Provider-specific data may come from an invariant fallback even when locale
metadata is native. Inspect coverage rather than assuming every provider has a
native pack for every locale.

## Safe versus plausible data

Safety and validity are independent controls. The default
`SafetyMode.Reserved` prefers officially reserved or documentation-only
contact targets where such ranges exist. It is the appropriate default for
tests, examples, and screenshots.

```csharp
FakeOptions options = new FakeOptions { Seed = 42L };
options.SafetyMode = SafetyMode.Reserved;
Fake safe = new Fake(options);
```

Available safety modes are:

- `Reserved`: prefer reserved/example domains, networks, email addresses, and
  phone ranges.
- `SyntheticPlausible`: produce plausible shapes without claiming allocation,
  ownership, reachability, or deliverability.
- `UnrestrictedShape`: allow broader shapes while still making no allocation
  claim.

Generated people, organizations, products, accounts, identifiers, and
transactions are synthetic. A checksum-valid or standards-shaped value is not
evidence that it was issued, belongs to anyone, reaches a real destination, or
is suitable as a credential. Profile records expose safety/correlation fields
where applicable so callers can retain those semantics with exported fixtures.

## Edge and adversarial modes

Ordinary provider calls default to `GenerationMode.Typical`. Edge cases are
explicit and never leak into normal APIs accidentally.

```csharp
using fake;

FakeOptions options = new FakeOptions { Seed = 42L };
options.GenerationMode = GenerationMode.Adversarial;
Fake data = new Fake(options);

EdgeSelectionResult selected = data.Edge().Select();
string inertSqlText = data.Edge().AdversarialString(
    AdversarialTextContext.Sql);
EdgeCaseRecord[] unicodeCases = data.Edge().Unicode();
```

`GenerationMode` supports `Typical`, `Boundary`, `ValidUnusual`, `Invalid`,
`Mixed`, and `Adversarial`. `EdgeSelectionResult` identifies the selected edge,
its category, validity, safety mode, and deliberately violated rule. Explicit
methods such as `ValidUnusual()`, `Invalid()`, `Mixed()`, and `Adversarial()`
can be used without changing the configured mode.

Adversarial values are inert returned strings. The library does not execute
SQL or shell text, render HTML, make HTTP requests, or write paths.

## Correlated profiles

`Profile()` creates complete correlated objects with public fields. Available
profiles are person, household, company, product, order, payment, vehicle,
flight, device, and student.

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 42L });
PersonProfile person = data.Profile().Person();
OrderProfile order = data.Profile().Order();

string displayName = person.FullName;
decimal total = order.Total;
```

Profiles coordinate fields that should agree: names with usernames and email,
birth dates with ages, products with identifiers and currency, order lines with
totals, flights with routes and arrival times, and vehicles with model years.
Use direct providers when fields should be independent; use profiles when the
relationship is part of the fixture.

Named sequences and uniqueness scopes are shared by profile/schema generation
within one root `Fake`. Reset them explicitly when starting a new logical
dataset:

```csharp
data.Profile().ResetSequenceFromNoArguments();
data.Profile().ResetUniqueFromNoArguments();
```

## Schemas and composition

`Schema()` builds runtime schema trees without requiring authored generic
types. Nodes include scalars, constants, objects, arrays, references, derived
fields, conditions, weighted variants, sequences, and unique values.

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 42L });
SchemaGenerator schema = data.Schema();

SchemaNode customer = schema.Object(new SchemaField[]
{
    schema.Field("id", schema.SequenceFromNameAndStartAndIncrement("customer-id", 1000L, 1L)),
    schema.Field(
        "name",
        schema.Scalar(value => value.Person().FullName())),
    schema.Field(
        "email",
        schema.Unique(
            "customer-email",
            schema.Scalar(value => value.Internet().Email())))
});

object generated = schema.GenerateFromRoot(customer);
string json = data.Export().Json(generated);
```

Object fields are evaluated in declared order, which makes references and
derived fields deterministic. Validation reports the schema path for cycles,
missing references, invalid bounds, and invalid variants. Array size, recursion,
output size, cancellation, and uniqueness retries remain bounded by the
captured options.

## Export

Exports return strings and never perform I/O. Object exporters include public
fields and apply format-specific escaping.

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Seed = 42L });
PersonProfile person = data.Profile().Person();

string json = data.Export().Json(person);
string csv = data.Export().CsvFromRows(new object[] { person });
string xml = data.Export().XmlFromRootNameAndValue("Person", person);
string vcard = data.Export().VCard(person);
string sql = data.Export().SqlInsertFromTableAndValue("people", person);
```

JSON uses deterministic public-member ordering where promised and
culture-invariant machine values. CSV supports RFC 4180 quoting and optional
spreadsheet-formula guards. XML and vCard apply their format escaping and line
rules. SQL output quotes identifiers and literals for the selected dialect but
is only text; the library never connects to or writes to a database.

`JsonFromNoArguments()`, `CsvFromNoArguments()`, `XmlFromNoArguments()`,
`Yaml()`, and `SqlInsert()` also generate standalone
structured-text fixtures. Deliberately malformed variants are available only
through explicit methods such as `InvalidJson()`.

## Contributing a locale

Locale data is embedded from `src/Data/locales/**/*.json`. Put a contribution
under a directory named with the canonical BCP 47 tag, such as
`src/Data/locales/fr-CA/metadata.json`. A pack has this shape:

```json
{
  "schemaVersion": 1,
  "dataVersion": "1",
  "locale": "fr-CA",
  "fallbacks": ["fr", "invariant"],
  "reviewStatus": "reviewed",
  "provenance": [
    {
      "id": "source-id",
      "source": "Authoritative source name",
      "sourceVersion": "published version or review date",
      "license": "license identifier",
      "reviewer": "review record"
    }
  ],
  "fields": [
    {
      "name": "date.order",
      "provenance": "source-id",
      "value": "ymd"
    }
  ]
}
```

Contribution requirements:

- Use a canonical, registered BCP 47 tag and an explicit parent/invariant
  fallback chain.
- Keep `schemaVersion`, `dataVersion`, fallbacks, and review metadata consistent
  across fragments for the same locale/version.
- Give every field a provenance ID defined by the pack, including source,
  source version/date, license, and review information.
- Add only fields that were actually reviewed for that locale. Leave other
  providers on an explicit fallback rather than translating or guessing rules.
- Keep contact data reserved/synthetic, vocabularies licensed and generic, and
  sensitive demographic, medical, legal, and identity terminology narrowly
  sourced.
- Add native-value, fallback-chain, malformed-resource, normalization, output
  limit, and deterministic seeded tests for runtime behavior introduced under
  `src/`.

Applications can also register locale data at runtime without modifying the
assembly:

```csharp
using fake;

Fake data = new Fake(new FakeOptions { Locale = "fr-CA", Seed = 42L });
Dictionary<string, object> fields = new Dictionary<string, object>();
fields["text.word"] = "exemple";

data.Locale().RegisterFromLocaleAndFields("fr-CA", fields);
```

For a complete JSON pack, pass a readable `Stream` and a diagnostic resource
name to `Locale().RegisterFromStreamAndResourceName(stream, resourceName)`. The pack's `dataVersion`
must match the `FakeOptions.DataVersion` captured by that generator. Runtime
registrations are instance-scoped; root forks start without their parent's
registrations.

### Data versions and releases

Locale data is versioned independently from both the NuGet package and the JSON
schema. `FakeOptions.DataVersion` selects the locale data edition; every
fragment in the selected pack must declare the same `dataVersion`. The bundled
edition is currently `1`. `schemaVersion` describes the resource format only,
so changing it does not by itself identify a new data edition.

A code-only package release does not require a locale data-version change. Bump
the data version when a locale change would alter seeded output, fallback
coverage, normalization, or the meaning of a field. Do not silently rewrite a
published edition. Keep the old edition available when compatibility is
promised; otherwise call out its removal and the migration in the package
release notes.

The complete acquisition, transformation, human-review, admission, and release
procedure is in [`src/Data/REFRESH.md`](src/Data/REFRESH.md). Source permissions
and refresh cadences are in [`src/Data/SOURCES.md`](src/Data/SOURCES.md), and
required attributions are in [`src/Data/LICENSES.md`](src/Data/LICENSES.md).
Every package release that changes bundled data must update
[`RELEASE_NOTES.md`](RELEASE_NOTES.md) with the locale data version and exact
upstream release, file, serial, publication, or retrieval dates.

Before submitting a locale, run the full suite:

```text
dotnet test fake.slnx
```
