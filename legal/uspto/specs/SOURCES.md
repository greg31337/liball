# Specification sources

Retrieved on 2026-08-27. SHA-256 hashes below describe the vendored files exactly as stored in this directory.

## USPTO Open Data Portal

The ODP query guide and the original multi-file OpenAPI bundle were taken from commit `026ea5fa7609c35c06a70b01711aece8c9f15f3a` of [patent-dev/uspto-odp](https://github.com/patent-dev/uspto-odp). That project records the files as unmodified downloads from the USPTO Swagger UI. The portal serves its interactive documentation from [data.uspto.gov](https://data.uspto.gov/apis), while current API traffic uses `https://api.uspto.gov`.

- `odp/ODP-API-Query-Spec.pdf` — ODP simplified query grammar.
- `odp/openapi/swagger.yaml` and `odp/openapi/odp-common-base.yaml` — patent file wrapper, status-code, bulk-product, petition, and combined PTAB contracts.
- `odp/openapi/trial-*.yaml` — original modular PTAB proceeding, decision, document, appeal, interference, and shared-schema contracts.
- `odp/openapi/oa-*.yaml` — Office Action text, citation, rejection, and enriched-citation DSAPI contracts.

The source bundle contains known discrepancies with live responses. The development plan requires tolerant manual parsing and fixture tests instead of silently treating the specifications as infallible.

## Trademark Status and Document Retrieval

- `trademark/tsdr-swagger.json` — TSDR Swagger contract from the same `patent-dev/uspto-odp` snapshot; the upstream source is the [USPTO TSDR Swagger service](https://developer.uspto.gov/swagger/tsdr-api-v1).
- `trademark/tsdr-api-key-manager-user-guide.pdf` — [USPTO TSDR API Key Manager user guide](https://www.uspto.gov/sites/default/files/documents/tm-enterprise-api-user-guide-v2.pdf).

## PatentsView

The files in `patentsview/` were taken from commit `e998bf2cae03d53f87f5d99e9ca4b9e388f0b288` of the USPTO-supported [PatentsView/PatentSearch-API](https://github.com/PatentsView/PatentSearch-API) repository.

- `patentsearch-openapi.json` — complete PatentSearch API contract.
- `SearchAPIReference.md` — authentication, request, response, query-language, sorting, and cursor-pagination behavior.
- `EndpointDictionary.md` — endpoint and field dictionary.
- `Examples.md` — request and pagination examples.
- `TextEndpointStatus.md` — long-text availability rules.

PatentsView moved to ODP in March 2026 and its online search services may be unavailable during the transition. The plan therefore treats direct PatentSearch access as capability-detected and makes ODP-hosted bulk datasets plus local queries the durable path.

## USPTO XML and dataset descriptions

- `uspto-xml/xml-resources.html` — snapshot of the [USPTO XML resources index](https://www.uspto.gov/learning-and-resources/xml-resources), including current patent grant/application DTD names and patent/trademark assignment and daily XML descriptions. Individual ODP-hosted DTD downloads require an authenticated portal session and were not saved as misleading HTML login pages.
- `bulk/patent-assignment-dataset.pdf` — [USPTO Patent Assignment Dataset description](https://www.uspto.gov/sites/default/files/documents/USPTO_Patents_Assignment_Dataset_WP.pdf).

## Protocol and format standards

- `standards/wipo/ST.9.pdf` — WIPO bibliographic data and INID codes.
- `standards/wipo/ST.13.pdf` — WIPO application-number format.
- `standards/wipo/ST.16.pdf` — WIPO patent-document kind codes.
- `standards/wipo/ST.36.pdf` — WIPO patent XML processing.
- `standards/rfc/rfc3339.txt` — timestamps.
- `standards/rfc/rfc3986.txt` — URI syntax and resolution.
- `standards/rfc/rfc4180.txt` — CSV conventions.
- `standards/rfc/rfc6266.txt` — `Content-Disposition` filename handling.
- `standards/rfc/rfc8259.txt` — JSON.
- `standards/rfc/rfc9110.txt` — HTTP semantics, conditions, ranges, and `Retry-After`.
- `standards/rfc/rfc9111.txt` — HTTP caching.
- `standards/rfc/rfc1951.txt` and `standards/rfc/rfc1952.txt` — DEFLATE and gzip.
- `standards/archive/zip-appnote.txt` — PKWARE ZIP application note.
- `standards/nist/FIPS-180-4.pdf` — SHA family specification.

## Research snapshots

The plan also compared these libraries without copying their implementation code:

- [patent-dev/uspto-odp](https://github.com/patent-dev/uspto-odp), commit `026ea5fa7609c35c06a70b01711aece8c9f15f3a`.
- [DunlapCoddingPC/pyUSPTO](https://github.com/DunlapCoddingPC/pyUSPTO), commit `e18699f5ce76826ff877942a04301da1d800afbf`.
- [KennethThompson/uspto_odp](https://github.com/KennethThompson/uspto_odp), commit `2bedad2c6f5ede35c6d25ac4e23253563569aaab`.
- [ip-tools/uspto-opendata-python](https://github.com/ip-tools/uspto-opendata-python), commit `d7f5925ab1480de74faacf933b765b5ebc747fa9`.

## SHA-256

```text
0a23ec6b285432e0fdadd0736a9e259436081b9d6d203b950b97e5b198670132  bulk/patent-assignment-dataset.pdf
b29c4ddb7de745a7a913dafbeae0850845ba8cefca0674c45a2302a221560762  odp/ODP-API-Query-Spec.pdf
5388cd1728405286215180987b5aeaf27ab955f5b646c26c418631efc04091cf  odp/openapi/oa-citations.yaml
10df3aa6688dd5856b0285789f04a7eb766da5f4e5ebb00a0162dba245682052  odp/openapi/oa-enriched-citations.yaml
028af96714af6d30392be0bc8b830d4618ec86d70fa9d3463efad38401acf5c9  odp/openapi/oa-rejections.yaml
e0c5398114e22a20fbd6687efd9793d89c0d22e988d159a5ac605d997da17529  odp/openapi/oa-text-retrieval.yaml
221fb5c196cf4d1f91542834f76eddcc520282d67034d9760dd32d904e44307c  odp/openapi/odp-common-base.yaml
4c9a59464ab1de915e7141603430b4955d5f6cc1ccd9d146102a5ae6378d4c17  odp/openapi/swagger.yaml
a29d8b6992495594f6ba38149ef054ab032cfe1af56526554cbedee769625890  odp/openapi/trial-appeal-decisions.yaml
335dd8a37df6806a7ac607948f6db94215b0045288a81714b41bf23ac623122f  odp/openapi/trial-common.yaml
f42d53381f9c1fbdfb9c85aadf12b9f1d15d553d538029110d140925a36840ef  odp/openapi/trial-decisions.yaml
ea313858f6869d29614fa93fc888fac319e279bf56fa7ff5ae68cbbcb60c04e7  odp/openapi/trial-documents.yaml
26cda11c3bf0f9d0898d1d3f217db12922dbb4a78810185dda392b6af054d5a2  odp/openapi/trial-interferences.yaml
21306d5f1c730a140d7b00452d32d4e9c02506c3149bac053dab7e4dc77be58c  odp/openapi/trial-proceedings.yaml
20be1a81950f02477d94ca404745d091ef9dbb8e61c9473dc966b78cce828a10  patentsview/EndpointDictionary.md
31799609c76bad6cf9c3a74b04939d3b172fd4faaa9aaf3359e6acf6d80c99fe  patentsview/Examples.md
6ba07a6c971a6785e19ca77d885c7bdaea27124758ac18ec4a6386d8fde704d9  patentsview/SearchAPIReference.md
21a8db2a9bc8ebeaac1c7f88fa89028143a391114f31e982af2bb833df8cf86e  patentsview/TextEndpointStatus.md
52f8bd53409f181f60adae9e01203f15396b2d5ea7ee243c9a98942035665007  patentsview/patentsearch-openapi.json
0b993022a7d320a0bf704e6980bea36fafd17a6066ab994db0a0c16278a50cd6  standards/archive/zip-appnote.txt
0455b406d89648d20cbde375561e19c245b9815e894164c2670772e3d54deb82  standards/nist/FIPS-180-4.pdf
5ebf4b5b7fe1c3a0c0ab9aa3ac8c0f3853a7dc484905e76e03b0b0f301350009  standards/rfc/rfc1951.txt
164ef0897b4cbec63abf1b57f069f3599bd0fb7c72c2a4dee21bd7e03ec9af67  standards/rfc/rfc1952.txt
9ab2b8864a85dca73a88f49b0927bc7bc85f596926e4fd1890905777924e700a  standards/rfc/rfc3339.txt
3102dae4b68cebe40337730312fcb612297b8928547267e8b3d1ee6002b2d683  standards/rfc/rfc3986.txt
5d9ec035aa40c0108362cb767c9e2d08c30f7225cb2a4715f484cb13c29b5f21  standards/rfc/rfc4180.txt
2887d464e7a2a15877aba5d54b2e8e0c06c32294c44c9d95eb8940069ab43d33  standards/rfc/rfc6266.txt
61a5378f4255c720beb2a4b4a63b29540147c140f36988bf086291989b4cd2d7  standards/rfc/rfc8259.txt
21c1cdce6ab0e5509b04d84a28000836c7a087cf786efe6f04877ebfff47232a  standards/rfc/rfc9110.txt
aeb52adb3279d5f23dae34f68af11bd5cef0a0aff7ffcd014c9ca93c5302cf3e  standards/rfc/rfc9111.txt
8c91abfc12b38d7a00dbbe9af29646b2a73e02a2daaa8ed9cb551975c45bb245  standards/wipo/ST.13.pdf
e4991d254ebf2a53b3c484927f47405f727a284921eece59e44bd11ab7587aea  standards/wipo/ST.16.pdf
e0b09c2e253c9df1366d660945b1e90355efd3e32e269f39e92e32d2243186a1  standards/wipo/ST.36.pdf
af48603d14ce5388e1d6416ab3adaba09f04cec2e594e7712c32c20e88713ab7  standards/wipo/ST.9.pdf
53d7870db1aa1a84ce782268283c62722f04fc907c0db8f322ea964e36b49474  trademark/tsdr-api-key-manager-user-guide.pdf
2e72951da2b0e26e1dcdcf625f08a93b4fcbf0e770b3a7d2d153e25234658d16  trademark/tsdr-swagger.json
b8ec9c94918278bc840263c2241266c729b75a72838840535ce5e9cdd0c50b5a  uspto-xml/xml-resources.html
```
