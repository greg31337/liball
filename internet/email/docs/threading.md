# Thread identity extraction

`new EmailThreadAnalyzer().ExtractWithMessage` derives bounded, deterministic threading evidence
from one `EmailMessage`. It does not claim to reproduce a mailbox server's
conversation algorithm, and it does not build a multi-message graph.

The result keeps the distinct evidence needed by a graph builder:

- all usable, de-duplicated `Message-ID`, `References`, and `In-Reply-To`
  identifiers in header order;
- the last `References` identifier as the immediate parent candidate, or a
  single `In-Reply-To` identifier when no reference exists;
- the first `References` identifier as the conversation key, otherwise a
  single `In-Reply-To`, otherwise the message's own first `Message-ID`;
- the decoded, normalized subject and the source selected for the thread key.

Multiple `In-Reply-To` identifiers are retained as evidence but are not guessed
into a parent or conversation identity. Identifiers are canonicalized with the
ID-left preserved and the ID-right compared and emitted case-insensitively.
Parser recovery is reported through `EmailThreadIdentity.GetIssues()`;
recovered identifiers are excluded by default and require
`SetAllowRecoveredIdentifiers(true)`.

## Normalized-subject fallback

When no identifier supplies a conversation key, subject fallback is enabled by
default and is explicitly reported as `EmailThreadIdentitySource.NormalizedSubject`.
It can be disabled with `SetEnableSubjectFallback(false)`.

Normalization decodes the field before applying Unicode NFKC, collapses
whitespace and unsafe ASCII controls, removes repeated leading `Re`, `Fw`,
`Fwd`, `Aw`, and `Sv` markers (including numeric counters), recognizes an
outer `[Fwd: ...]` wrapper and a trailing `(fwd)`, then uses invariant lowercase.
Markers are removed only at these boundaries; text such as
`[Project] Re: Topic` remains content. An empty normalized subject does not
produce a fallback key.

Identifier, header, input-byte, subject-byte, and subject-character limits are
configured by `EmailThreadOptions`. Exceeding one throws `EmailLimitException`
instead of returning a misleading partial identity.

## Conversation graphs

`new EmailThreadAnalyzer().BuildGraphWithMessages` returns one `EmailThreadNode` per input message
and preserves input order in `EmailThreadGraph.GetNodes()`. Explicit parent
evidence links a node to the first input node owning the referenced canonical
`Message-ID`. Roots and each node's children also retain input order, including
when a child appears before its parent in the supplied list.

The graph is always returned as an acyclic forest. Observable recovery rules
are deterministic:

- duplicate message identifiers produce `ThreadDuplicateMessageId`; the first
  input owner remains the target for later parent lookups;
- absent explicit parents produce `ThreadParentMissing`, and the node remains a
  root;
- an edge that would close a cycle is omitted and every participant receives
  `ThreadCycleDetected`;
- messages whose identity source is normalized subject are grouped under the
  first same-subject input node and every such node receives
  `ThreadSubjectFallbackUsed`, including a fallback root.

Subject grouping is intentionally flat and heuristic. It is never applied to a
message that has usable identifier evidence, and disabling subject fallback in
`EmailThreadOptions` also disables graph grouping by subject. Graph-wide
message and indexed-identifier limits complement the per-message extraction
limits. Diagnostics are exposed both on affected nodes and on the graph; graph
diagnostics also include the underlying identity-extraction issues.
