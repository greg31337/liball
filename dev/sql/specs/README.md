# SQL Parser Reference Snapshot

Large reference files are available through [Reference downloads](../REFERENCE-DOWNLOADS.md). The source inventory below also records historical acquisitions.

Retrieved on 2026-08-25 for offline implementation and test planning. Before this snapshot, `specs/` contained no files and was not sufficient for the library's universal-parser goal or its complete-SQLite acceptance target.

## SQLite Acceptance Baseline

- `sqlite/sqlite-doc-3.53.4.zip`: official SQLite 3.53.4 static documentation bundle from https://www.sqlite.org/2026/sqlite-doc-3530400.zip. It contains the complete language pages and syntax diagrams referenced by `todo.md`. Published SHA3-256: `7ccf86a52e7dd1fb9b31e63edcebe3b553f18f89cd26eef59c7f191a5111836e`.
- `sqlite/parse-3.53.4.y`: canonical Lemon SQL grammar from the official SQLite GitHub mirror at https://raw.githubusercontent.com/sqlite/sqlite/version-3.53.4/src/parse.y.
- `sqlite/tokenize-3.53.4.c`: canonical tokenizer implementation from the official SQLite GitHub mirror at https://raw.githubusercontent.com/sqlite/sqlite/version-3.53.4/src/tokenize.c.

The versioned documentation, grammar, and tokenizer together are the authoritative baseline for complete SQLite lexing, parsing, AST fidelity, formatting, generation, and acceptance tests.

## Dialect References

- `dialects/postgresql/`: PostgreSQL 18 SQL language, lexical structure, and value-expression pages from https://www.postgresql.org/docs/current/.
- `dialects/mysql/`: MySQL 8.4 language structure, expressions, and SQL statement reference from https://dev.mysql.com/doc/refman/8.4/en/.
- `dialects/mariadb/`: MariaDB SQL statement and comment syntax references from https://mariadb.com/docs/server/reference/sql-statements.
- `dialects/sqlserver/`: Microsoft Transact-SQL reference and syntax conventions from https://learn.microsoft.com/en-us/sql/t-sql/.
- `dialects/oracle/`: Oracle Database 21c SQL Language Reference landing page from https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/.
- `dialects/duckdb/duckdb-docs.pdf`: official DuckDB documentation PDF from https://blobs.duckdb.org/docs/duckdb-docs.pdf.
- `dialects/bigquery/`: GoogleSQL lexical structure and query syntax from https://cloud.google.com/bigquery/docs/reference/standard-sql/.
- `dialects/snowflake/query-syntax.md`: Snowflake query syntax from https://docs.snowflake.com/en/sql-reference/constructs.md.
- `dialects/redshift/`: Amazon Redshift SQL reference and command index from https://docs.aws.amazon.com/redshift/latest/dg/.
- `dialects/databricks/sql-language-reference.html`: Databricks SQL language reference from https://docs.databricks.com/aws/en/sql/language-manual/.

These are focused references for the incremental dialect descriptors. They are not claims of complete dialect support; each dialect should gain deeper snapshots as its implementation scope expands.

## Comparative Library Research

- `research/jsqlparser.html`: JSqlParser overview and feature documentation.
- `research/sqlglot.html`: SQLGlot API documentation.
- `research/apache-calcite-sql-reference.html`: Apache Calcite SQL language reference.
- `research/scriptdom-tsqlparser.html`: Microsoft ScriptDom parser API description.
- `research/sqlparser-rs.html`: sqlparser-rs crate documentation.

## SQL Standard Availability

The applicable foundation standard is ISO/IEC 9075-2:2023, cataloged at https://www.iso.org/standard/76584.html. The full standard is licensed and was not copied into this repository. The Generic and ANSI behavior should therefore be checked against lawfully available standard text when that work is implemented; vendor references must not be treated as substitutes for the standard.
