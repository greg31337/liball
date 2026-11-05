# Specification and research source manifest

Downloaded on 2026-08-27. RFC text and errata are from the RFC Editor; registries are from IANA; implementation descriptions are from their official project sites or repositories. The implementation repositories were also inspected at the commits recorded below, but their source trees are not vendored here.

## Normative and supporting RFCs

| Local file | Subject | Source |
| --- | --- | --- |
| `rfc/rfc3539.txt` | AAA transport profile; watchdog and failover background | https://www.rfc-editor.org/rfc/rfc3539.txt |
| `rfc/rfc5729.txt` | Routing by User-Name and realm | https://www.rfc-editor.org/rfc/rfc5729.txt |
| `rfc/rfc6408.txt` | Diameter S-NAPTR peer discovery | https://www.rfc-editor.org/rfc/rfc6408.txt |
| `rfc/rfc6733.txt` | Diameter Base Protocol | https://www.rfc-editor.org/rfc/rfc6733.txt |
| `rfc/rfc6737.txt` | Diameter capabilities update application | https://www.rfc-editor.org/rfc/rfc6737.txt |
| `rfc/rfc7068.txt` | Diameter overload-control requirements | https://www.rfc-editor.org/rfc/rfc7068.txt |
| `rfc/rfc7075.txt` | Realm-based redirection | https://www.rfc-editor.org/rfc/rfc7075.txt |
| `rfc/rfc7683.txt` | Diameter overload indication conveyance | https://www.rfc-editor.org/rfc/rfc7683.txt |
| `rfc/rfc7944.txt` | Diameter routing message priority | https://www.rfc-editor.org/rfc/rfc7944.txt |
| `rfc/rfc8506.txt` | Diameter Credit-Control Application | https://www.rfc-editor.org/rfc/rfc8506.txt |
| `rfc/rfc8581.txt` | Agent overload and peer overload reports | https://www.rfc-editor.org/rfc/rfc8581.txt |
| `rfc/rfc8582.txt` | Diameter overload rate control | https://www.rfc-editor.org/rfc/rfc8582.txt |
| `rfc/rfc8583.txt` | Diameter load information conveyance | https://www.rfc-editor.org/rfc/rfc8583.txt |
| `rfc/rfc6733-errata.html` | RFC 6733 errata search snapshot | https://www.rfc-editor.org/errata_search.php?rfc=6733 |
| `rfc/rfc8506-errata.html` | RFC 8506 errata search snapshot | https://www.rfc-editor.org/errata_search.php?rfc=8506 |

## Live registries

| Local file | Subject | Source |
| --- | --- | --- |
| `iana/aaa-parameters.xml` | AVP codes and values, application IDs, command codes, and flags | https://www.iana.org/assignments/aaa-parameters/aaa-parameters.xml |
| `iana/s-naptr-parameters.xml` | S-NAPTR service and protocol tags | https://www.iana.org/assignments/s-naptr-parameters/s-naptr-parameters.xml |
| `iana/service-names-port-numbers.csv` | Diameter TCP/SCTP service and port registrations | https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.csv |
| `iana/enterprise-numbers.txt` | Vendor-ID / private enterprise number registry | https://www.iana.org/assignments/enterprise-numbers.txt |

## Existing implementation references

| Local file | Subject | Source |
| --- | --- | --- |
| `references/freeDiameter-README.txt` | freeDiameter component overview | https://raw.githubusercontent.com/freeDiameter/freeDiameter/master/README |
| `references/freeDiameter-features.html` | freeDiameter feature inventory | https://www.freediameter.net/trac/wiki/Features.html |
| `references/freeDiameter-api.html` | freeDiameter protocol, dictionary, session, peer, routing, and extension API overview | https://www.freediameter.net/trac/wiki/Dev/API.html |
| `references/freeDiameter-extensions.html` | freeDiameter application, routing, dictionary, peer, and diagnostic extension inventory | https://www.freediameter.net/trac/wiki/Extensions.html |
| `references/jDiameter-README.md` | RestComm jDiameter feature and application overview | https://raw.githubusercontent.com/RestComm/jdiameter/master/README.md |
| `references/jDiameter-user-guide.html` | jDiameter architecture, peer, routing, session, and application guide | https://docs.quarkiverse.io/quarkus-jdiameter/dev/Diameter_User_Guide.html |
| `references/Open5GS-README.md` | Open5GS project overview | https://raw.githubusercontent.com/open5gs/open5gs/main/README.md |
| `references/Open5GS-freeDiameter-mme.conf.in` | Real-world Open5GS/freeDiameter peer, transport, TLS, extension, and timer configuration | https://raw.githubusercontent.com/open5gs/open5gs/main/configs/freeDiameter/mme.conf.in |

Inspected source revisions:

- freeDiameter: `9d4bc47584bfeced9de708fe00f5629ac4689db5`
- RestComm jDiameter: `7f3909342cca30ffc7e69b7d8b4111f5fde52ece`
- Open5GS: `d261bd43d859ec3d9c9f95a441dc295ce0814a7b`
