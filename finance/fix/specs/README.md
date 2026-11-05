# Specification and research source manifest

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Retrieved on 2026-08-27 for development planning. The files remain subject to their upstream licenses and notices.

## FIX standards and machine-readable repositories

- `fix/FIX_TagValue_Encoding_v1.0_June_2020.pdf`: FIX Trading Community, FIX TagValue Encoding v1.0 Technical Standard.
- `fix/FIX_Session_Layer_with_Errata_November_2020.pdf`: FIX Trading Community, FIX Session Layer Technical Standard with November 2020 errata.
- `fix/FIX_Session_Testcases_June_2020.pdf`: FIX Trading Community, session-layer conformance cases.
- `fix/FIX_Transport_1.1.pdf`: FIXT 1.1 session protocol.
- `fix/application/`: FIX Latest EP284 application specification volumes and the time-precision addendum.
- `fix/FIXOrchestra_FIX.Latest_EP309/` (original ZIP removed; see the download list): current FIX Latest EP309 Orchestra repository and v1.0 schemas from the official download page.
- `fix/quickfix-dictionaries/`: QuickFIX's XML dictionaries for FIX 4.0 through FIX 5.0 SP2 and FIXT 1.1, retained as non-normative interoperability fixtures.

## Orchestra, binary framing, and transport security

- `orchestra/FIX-Orchestra-v1.0-Technical-Standard.pdf`: final Orchestra v1.0 Technical Standard.
- `orchestra/Orchestra-v1.1-RC3-Technical-Specification.pdf`: current Orchestra v1.1 RC3 specification.
- `orchestra/Orchestra-Schema-v1.1-RC3/` (original ZIP removed; see the download list): repository and interfaces schemas for v1.1 RC3.
- `sbe/`: SBE v1.0 standard, errata, source, schema, and examples from `FIXTradingCommunity/fix-simple-binary-encoding`.
- `sofh/`: SOFH v1.0 draft and v1.1 RC1 sources from `FIXTradingCommunity/fix-simple-open-framing-header`.
- `fixs/`: FIX-over-TLS v1.0 and v1.1 RC2 materials from `FIXTradingCommunity/fixs-specification`.
- `rfcs/`: RFC 2119, 2978, 7301, 8174, and 8446 from the RFC Editor.

## Existing-engine research

`research/` contains selected upstream READMEs, configuration/validation/group guides, release notes, and architecture material for QuickFIX, QuickFIX/J, QuickFIX/n, Artio, and OnixS. These are comparative inputs, not normative FIX specifications.

Primary upstream locations:

- https://fixtrading.org/standards/technical-standards/
- https://fixtrading.org/standards/fixsession/
- https://fixtrading.org/standards/fix-protocol/
- https://fixtrading.org/standards/orchestra/fix-orchestra-standard/
- https://github.com/FIXTradingCommunity/fix-simple-binary-encoding
- https://github.com/FIXTradingCommunity/fix-simple-open-framing-header
- https://github.com/FIXTradingCommunity/fixs-specification
- https://github.com/quickfix/quickfix
- https://github.com/quickfix-j/quickfixj
- https://github.com/connamara/quickfixn
- https://github.com/artiofix/artio
- https://www.onixs.biz/net-fix-engine.html
