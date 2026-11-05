# Thematic data review

Review identifier: `repository-data-review-2026-08-15`

Scope: the runtime tables in `ColorData`, `VehicleData`, `AirlineData`,
`BookData`, `FoodData`, `AnimalData`, `ScienceData`, `HealthData`, and
`CultureData` created for tasks T122 through T130.

## Admission decision

The reviewed tables are admitted for fixture generation subject to the project
license limitation below. The review found no profanity, slurs, sexual content,
live secrets, personal datasets, substantial prose, lyrics, plots, quotations,
medical advice, legal advice, defamatory assertions, or military/intrusion
instructions.

All authored values are short original tokens or combinations written for this
repository on 2026-08-15. Comparative faker libraries and search-result lists
were used only as feature inventories and supplied no runtime values. Reviewer:
`repository-data-review-2026-08-15`.

The repository still has no root project license. Consequently, original
project-authored tables remain approved for development but are not approved as
an independently redistributable data pack until a root license is selected.
This limitation does not affect the separately licensed factual subsets listed
below, but their required notices must accompany redistribution.

## Dataset decisions

| Runtime table | Provenance and license boundary | Risk review and decision |
| --- | --- | --- |
| `ColorData` | Sixteen standardized CSS keyword/value facts selected from W3C CSS Color Module Level 4, Candidate Recommendation Draft 6 August 2026, under the W3C Software and Document License 2023; conversion code is project-authored. | **Approved.** The subset omits culturally sensitive legacy names, brands, palettes, prose, and meaning claims. Color alone must not be treated as accessible communication. |
| `VehicleData` | Original project-authored fixture combinations plus generic transport terms. VIN rules remain separately sourced as recorded in `SOURCES.md`. | **Approved after correction.** Makes and models now include Example, Fixture, Sample, Test, Demo, or Mock markers to reduce real-brand/model collision. Values make no manufacture, ownership, registration, emissions, or safety claim. |
| `AirlineData` | Original project-authored fixture combinations and generic cabin terms. | **Approved after correction.** Airlines and aircraft now include conspicuous fixture markers; airports already did. Codes are synthetic and unassigned. Values make no service, operation, booking, or safety claim. |
| `BookData` | Original compositional title, author, publisher, and series components plus short generic genre/format terms. | **Approved.** No copied title list, prose, plot, quotation, review, or living-author corpus was used. Short generated phrases may coincidentally resemble existing titles, so records explicitly deny a published-work claim. |
| `FoodData` | Original dish combinations plus short generic ingredient, beverage, diet, and allergen labels. | **Approved with non-clinical restriction.** Values contain no recipes, cultural-authenticity claims, nutrition advice, or food-safety assurance. Diet and allergen labels never assert suitability, presence, or absence. |
| `AnimalData` | Original fictional mountain components plus short common animal, species, habitat, breed, plant, and weather terms. | **Approved.** Correlations are broad fixtures, not taxonomy, ecology, breed certification, ownership, forecast, or observation. The table links no protected human trait to an animal or place. |
| `ScienceData` | Original experiment labels and generic science terms; selected SI quantity/unit/symbol/prefix/exponent facts from the BIPM SI Brochure, ninth edition, updated 2026, CC-BY-4.0. Element and planet entries are short factual identifiers. | **Approved.** No BIPM prose or full table is copied. Measurements and experiments explicitly deny calibration, observation, certification, or conducted research. |
| `HealthData` | Original conspicuously synthetic fixtures and short generic anatomy/blood-group facts. | **Approved only for non-clinical fixtures.** Symptoms are sample-prefixed; blood groups are fixture-prefixed; medicine forms end in `-shaped`; dosage text uses fixture units; codes use unassigned `TEST-DX-` and `TEST-PX-` prefixes. No diagnosis, treatment, coding completeness, patient, clinician, facility, or compatibility claim is made. |
| `CultureData` | Original names/titles and generic terms. NATO alphabet entries are only the 26 short factual identifiers verified against NATO's historical alphabet page; no NATO prose, signal material, or operational content is copied and no license to NATO content is claimed. | **Approved.** Artist/team names contain fixture markers. Music values contain no lyrics or copied prose. Military terms are administrative/support terms; hacker terms are benign defensive concepts. No live person, brand endorsement, secret, exploit instruction, or operational guidance is present. |

## Cross-cutting review

- Profanity policy: the admitted lists contain no profanity, slurs, sexual
  content, insults, or demeaning pairings. Future additions require the same
  manual review; substring blocklists alone are not sufficient.
- Defamation and live-person risk: no live-person dataset is present. Fictional
  people use conspicuous surnames such as Example, Fixture, Sample, Test, Demo,
  or Mock, and no negative attributes are attached to a person or organization.
- Cultural bias: the pack is intentionally small, invariant, and English-like.
  It does not claim cultural coverage, authenticity, translation, or
  representativeness. It avoids linking protected traits, nationalities,
  religions, or ethnicities to generated characteristics.
- Copyright: the project-authored material consists of short original tokens
  and combinations. No substantial prose, lyrics, plots, articles, reviews,
  recipes, or imitated living-author style is included.
- Regulated implications: health, nutrition, identifier, aviation, vehicle,
  weather, science, military, and security outputs carry explicit synthetic or
  non-authoritative semantics. They are fixtures, not medical/legal advice,
  food-safety guidance, forecasts, assignments, certifications, or operational
  instructions.

## Refresh rule

Repeat this review on every thematic vocabulary change and at least annually.
Record the author, date, source method, reviewer, license boundary, and changes
to every risk decision. Moving a table into `src/Data` also requires a complete
`manifest.json` entry and any license notice mandated by `LICENSES.md`.
