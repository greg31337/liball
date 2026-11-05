# Specification Sources

These files were retrieved on 2026-08-25 for offline use while implementing and testing the library.

- `rfc3339.txt`: RFC 3339, *Date and Time on the Internet: Timestamps*. Source: https://www.rfc-editor.org/rfc/rfc3339.txt
- `rfc5545.txt`: RFC 5545, *Internet Calendaring and Scheduling Core Object Specification (iCalendar)*. Source: https://www.rfc-editor.org/rfc/rfc5545.txt
- `rfc9557.txt`: RFC 9557, *Date and Time on the Internet: Timestamps with Additional Information*. This updates RFC 3339 and specifies bracketed time-zone annotations. Source: https://www.rfc-editor.org/rfc/rfc9557.txt
- `w3c-xsd-1.1-datatypes.html`: W3C XML Schema Definition Language 1.1 Part 2, including ISO-derived date, time, date-time, and duration lexical/value models. Source: https://www.w3.org/TR/2012/REC-xmlschema11-2-20120405/datatypes.html
- `iana-tzdb-theory.html`: IANA time-zone database scope, identifiers, civil-time model, limitations, and transition principles. Source: https://www.iana.org/time-zones/theory
- `dotnet-date-time-overview.html`: .NET date, time, offset, duration, and time-zone type overview. Source: https://learn.microsoft.com/en-us/dotnet/standard/datetime/
- `dotnet-dateonly-timeonly.html`: .NET `DateOnly` and `TimeOnly` behavior and interoperation guidance. Source: https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly
- `dotnet-time-zone-overview.html`: .NET `TimeZoneInfo` concepts and platform-backed time-zone behavior. Source: https://learn.microsoft.com/en-us/dotnet/standard/datetime/time-zone-overview
- `dotnet-time-provider-overview.html`: .NET `TimeProvider` clock abstraction guidance. Source: https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview

ISO 8601-1 and ISO 8601-2 are directly relevant, but their full texts are not freely downloadable from ISO. RFC 3339, RFC 5545, RFC 9557, and the W3C datatype recommendation provide locally available, authoritative open specifications for the supported timestamp, interval, recurrence, and duration representations.
