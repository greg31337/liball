# Universal Structured Logging Library Development Plan

## Goal

Build a universal structured logging library for .NET that is small, dependency-free, predictable under load, and ergonomic enough for everyday application logging. The implementation must use only built-in .NET APIs and must conform to the simplified C# subset in `syntax.md`.

## Implementation Constraints

- Use only built-in .NET namespaces such as `System`, `System.IO`, `System.Text`, `System.Text.Json`, `System.Diagnostics`, `System.Threading`, and `System.Collections`.
- Do not depend on Serilog, NLog, Microsoft.Extensions.Logging, OpenTelemetry packages, or any other external library.
- Source code may use narrowly scoped interfaces that follow `syntax.md`; it must avoid properties, records, structs, attributes, class inheritance, `try`/`catch`, LINQ, generic type declarations, `params`, default parameters, extension methods, and other forbidden syntax.
- Prefer passive data classes with ordinary fields and interface-implementing behavioral classes with instance factory or operation methods.
- Since `try`/`catch` is forbidden, reliability features should use explicit `LogResult` values, defensive validation, and controlled built-in sinks rather than exception-driven control flow.
- Treat every explicitly `public` class, enum, constructor, method, and field as part of the supported API. Mark every implementation detail explicitly `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Use only the XML elements allowed by `syntax.md`, and explain observable mechanics and lifecycle behavior in plain English.

## Research Notes

- Serilog highlights fluent configuration, multiple sinks, output templates, minimum levels, per-sink minimum levels, enrichers, filters, sub-loggers, message templates, structured properties, and close/flush lifecycle.
  Source: https://github.com/serilog/serilog/wiki/Configuration-Basics
- Winston highlights logger creation with levels, formats, transports, multiple transports, per-transport levels/formats, containers/named loggers, custom transports, exception handling, and stream-oriented output.
  Source: https://github.com/winstonjs/winston
- SLF4J emphasizes a narrow facade separated from configuration, provider/back-end choice, parameterized messages for lower disabled-log cost, bridges from legacy APIs, and mapped diagnostic context.
  Source: https://www.slf4j.org/manual.html
- Logback emphasizes named logger hierarchies, effective level inheritance, selection before event creation, appenders, layouts, filters, additive routing, MDC, lazy event fields, and performance of disabled logging.
  Source: https://logback.qos.ch/manual/architecture.html
- Loguru emphasizes one easy logger, configurable sinks, formatting, filtering, JSON serialization, contextual binding, context-local state, file rotation/retention/compression, queueing, and safe sink error handling.
  Source: https://loguru.readthedocs.io/en/stable/api/logger.html
- Zap emphasizes strongly typed fields, a more ergonomic sugared API, fast structured JSON output, low allocation, explicit sync/flush, sampling, and sink abstraction.
  Source: https://pkg.go.dev/go.uber.org/zap
- NLog emphasizes structured and traditional logging, targets, layouts, target-specific output formats, runtime configuration changes, routing rules, buffering, failover, async writing, and file archival.
  Source: https://nlog-project.org/
- Go `log/slog` emphasizes records with time/level/message/attributes, handlers, `Enabled` checks before record construction, contextual attributes with `With`, groups, dynamic level variables, JSON/text handlers, and concurrency-safe handler design.
  Source: https://pkg.go.dev/log/slog

## Feature Ladder, Low Level To High Level

### 1. Core Types

- `LogLevel` enum with `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`, and `Off`.
- `LogValueKind` enum for null, string, bool, integer, decimal, floating point, date/time, duration, exception, object text, array, dictionary, and raw JSON-compatible value categories.
- `LogValue` class that stores normalized values without relying on anonymous objects or reflection-heavy behavior by default.
- `LogField` class for key-value pairs with validated names.
- `LogEvent` class for timestamp, level, logger name, event id/name, message template, rendered message, fields, exception, activity identifiers, thread/process data, and source hints.
- `LogResult` class for success/failure status from sinks and pipeline operations.
- `LogClock` class to isolate UTC timestamp acquisition for tests.
- `LogEventBuilder` class to collect fields before event creation.

### 2. Message Templates And Field Binding

- Message template parser for Serilog-style `{Name}` tokens.
- Placeholder binding from positional values into structured fields.
- Literal brace escaping.
- Template cache with bounded size.
- Rendered message generation that reuses parsed templates.
- Duplicate field name handling with stable suffixes.
- Format hint storage without requiring full formatting support in v1.
- Optional destructuring hint recognition for future compatibility.

### 3. Value Normalization And Serialization Safety

- Explicit typed field factory methods for string, bool, int, long, decimal, double, date/time, duration, exception, and object text.
- Array/list value capture with maximum length.
- Dictionary value capture with maximum entry count.
- Object capture fallback using `ToString()` only, avoiding general reflection in the first implementation.
- Maximum string length handling.
- Maximum nesting depth handling.
- Redaction support by field name.
- Reserved field collision handling.
- Stable JSON escaping through `System.Text.Json` or a built-in encoder.

### 4. Logger And Level Selection

- Root logger.
- Named/category loggers.
- Child logger creation with inherited context.
- Global minimum level.
- Per-logger minimum level.
- Per-sink minimum level.
- Cheap `IsEnabled(level)` checks before event allocation.
- Fatal level handling without forcing process exit.
- Level switch object for runtime changes.
- No-op logger.

### 5. Context, Scopes, And Correlation

- Static global fields applied to every event.
- Logger-bound fields through `WithField` and `WithFields`.
- Scope stack using `AsyncLocal`.
- Scope disposal through an explicit scope object.
- Correlation fields from `System.Diagnostics.Activity`.
- Thread id and process id enrichers.
- Machine name and application name enrichers.
- Event id and event name support.
- Field groups/namespaces for subsystem-specific data.

### 6. Enrichment, Filtering, Sampling, And Routing

- Enricher pipeline using `LogEnricher` defaults or `ILogEnricher` implementations.
- Filter pipeline using `LogFilter` defaults or `ILogFilter` implementations with include/exclude decisions.
- Sampling filter for high-volume events.
- Rate limit filter for repeated events.
- Category prefix filter.
- Field existence/value filter.
- Fanout to multiple sinks.
- Sink-specific filters.
- Sink-specific level thresholds.
- Sub-pipeline support through concrete pipeline objects.

### 7. Formatters

- Compact newline-delimited JSON formatter.
- Full JSON formatter.
- Plain text formatter.
- Console text formatter with optional color.
- Key-value formatter.
- Output template formatter for common fields.
- Exception formatter.
- Activity trace/span formatting.
- UTC/local timestamp choice.
- Stable field ordering option.

### 8. Built-In Sinks

- Null sink.
- Memory sink for tests.
- TextWriter sink.
- Console sink with stdout/stderr routing.
- Debug sink using `System.Diagnostics.Debug`.
- Trace sink using `System.Diagnostics.Trace`.
- File sink.
- Rolling file sink by size.
- Rolling file sink by date.
- Retention cleanup by file count and age.
- EventSource sink for built-in .NET event tracing.
- Delegate sink for callback-based output and `ILogSink` for stateful custom destinations.

### 9. Async, Batching, And Backpressure

- Synchronous pipeline as the baseline.
- Explicitly drained queue sink wrapper backed by built-in collections.
- Batching sink wrapper.
- Bounded queue capacity.
- Backpressure modes: block, drop newest, drop oldest.
- Dropped-event counters.
- Flush method.
- Close and drain method.
- Shutdown timeout.
- Recursive drain, flush, and close lifecycle for queued and nested sinks.

### 10. Reliability And Diagnostics

- Explicit sink result reporting.
- Self-log channel for internal logger diagnostics.
- Sink failure counters.
- Circuit breaker wrapper for repeatedly failing sinks.
- Health snapshot object.
- Flush result aggregation.
- Safe behavior when no sinks are configured.
- Reentrancy guard for self-logging.

### 11. Configuration

- Fluent-style builder accepting concrete defaults and narrow interface extensions.
- Programmatic configuration as the primary API.
- JSON configuration loader using `System.Text.Json`.
- Environment variable overrides.
- Runtime level switches.
- Optional file watcher for configuration reload using `FileSystemWatcher`.
- Validation diagnostics before creating the logger.
- Immutable-by-convention runtime pipeline after build.

### 12. Developer Experience

- Explicitly owned logger instances for simple apps.
- Factory class for named loggers.
- Concise level methods.
- Structured field helper methods.
- `For(name)` and `ForTypeName(name)` category helpers.
- `With` methods for contextual child loggers.
- `BeginScope` methods for request/workflow context.
- `Measure`/timer helper for durations.
- Documentation examples.
- Migration notes from common logging libraries.

### 13. Testing And Quality

- Syntax conformance tests for all source files.
- Unit tests for levels, templates, field binding, value conversion, JSON output, text output, context, filtering, routing, file rolling, async behavior, flushing, and shutdown.
- Concurrency tests for shared loggers and sinks.
- Allocation and performance smoke tests.
- Golden output tests for formatters.
- Failure-path tests using explicit failing sinks.
- Public API compatibility checklist.

## Critical Must-Have Feature Set

- Structured event model with level, timestamp, logger name, message template, rendered message, fields, exception, and correlation data.
- Cheap disabled logging through `IsEnabled` and early level checks.
- Named loggers and category hierarchy.
- Message templates with structured property capture.
- Strongly typed field helpers.
- Context propagation through logger-bound fields and async scopes.
- Enrichers for common runtime metadata.
- Filters, sampling, and per-sink thresholds.
- Multiple sinks with fanout.
- JSON and human-readable text formatters.
- Console, file, rolling file, text writer, debug, trace, null, memory, and delegate sinks.
- Async and batching wrappers with bounded backpressure policies.
- Flush/close lifecycle.
- Self diagnostics for sink and pipeline failures.
- Programmatic builder configuration.
- JSON configuration loading using only built-in .NET APIs.
- Runtime level switches.
- Tests that preserve simplified C# syntax conformance.

## Public API Completion Tasks

- [DONE] T001: Expose a public event-builder workflow that can create `LogEvent` instances with an injected clock, event id and name, message template and values, exception, fields, trace context, runtime metadata, source hints, and redaction before passing them to `WriteEvent`.
- [DONE] T002: Add public strongly typed `LogValue` and `LogField` creation methods for null, string, bool, int, long, decimal, double, date/time, duration, exception, object text, arrays, dictionaries, and validated raw JSON, including configurable string, collection, and nesting limits.
- [DONE] T003: Add public factory entry points for root, named, runtime-level-switch, and explicit no-op loggers so applications can create the planned logger forms without relying on internal constructors or indirect configuration.
- [DONE] T004: Add `WithField`, `WithLogField`, and `WithFields` public logger-context operations that create contextual child loggers while preserving inherited category, level, filter, enricher, sink, and scope behavior.
- [DONE] T005: Add public field-group and namespace creation and binding operations that preserve nested structured values in JSON and render predictably in text and key-value formats.
- [DONE] T006: Add a public `ForTypeName` category operation with deterministic type-name normalization and the same hierarchy and inherited-context behavior as `For`.
- [DONE] T007: Expose public sub-pipeline composition operations that can install, replace, inspect, and clear child sink pipelines with sink-specific levels and filters, fanout, and recursive lifecycle handling.
- [DONE] T008: Add public formatter configuration for UTC or local timestamps and stable field ordering across compact JSON, full JSON, text, console, key-value, and output-template formatters.
- [DONE] T009: Add a public duration measurement helper that uses an injectable built-in .NET time source, emits a duration field at completion, is safe to complete more than once, and participates in normal levels, context, filtering, and sink routing.
- [DONE] T010: Add public-consumer tests for every enum value, data-class constructor and field, result state, health snapshot, rolling and retention policy, event-builder operation, and typed value and field factory, including null, invalid, truncation, nesting, and overflow boundaries.
- [DONE] T011: Add public-consumer tests for every logger factory form, level switch, enabled check, general write operation, convenience level operation, exception path, event write, no-op behavior, category hierarchy, state counter, and counter reset.
- [DONE] T012: Add public-consumer tests for contextual child loggers, `With` operations, type-name categories, global and grouped fields, async-local nested and out-of-order scopes, activity correlation, custom enrichers and clocks, and duration measurement.
- [DONE] T013: Add public-consumer tests for all built-in and interface-based filters, sampling and windowed rate limiting, field and category matching, logger and sink thresholds, fanout, sink-specific routing, and nested sub-pipelines.
- [DONE] T014: Add golden and edge-case tests for every public formatter and formatter-composition operation, covering JSON validity and escaping, raw values, non-finite numbers, exception and activity output, line endings, output templates, timestamp mode, color mode, and stable field ordering.
- [DONE] T015: Add public-consumer tests for null, memory, text-writer, console, debug, trace, file, rolling file, EventSource, delegate, and custom interface sinks, including state inspection, filters, thresholds, retention, flushing, draining, closing, and repeated lifecycle calls.
- [DONE] T016: Add public-consumer tests for asynchronous, batching, and circuit-breaker sink wrappers across all backpressure modes, capacities, batch boundaries, nested wrappers, shutdown timeouts, dropped events, failures, recovery, and recursive lifecycle aggregation.
- [DONE] T017: Add public-consumer tests for programmatic configuration, JSON loading, environment overrides, validation diagnostics, runtime category changes, file watching and reload callbacks, redaction, self diagnostics, reentrancy protection, no-sink safety, and pipeline health.
- [DONE] T018: Add concurrency tests through public contracts for shared loggers, level switches, scopes, filters, diagnostics, memory and delegate sinks, queued and batched sinks, file rolling, reload lifecycle, counter accuracy, and close-versus-write races.
- [DONE] T019: Add an external-consumer API coverage suite that implements every public interface and exercises every public constructor, method, and observable field, then fill all remaining publicly reachable success, boundary, failure, and lifecycle coverage gaps.
