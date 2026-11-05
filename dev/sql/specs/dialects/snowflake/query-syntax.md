> ## Documentation Index
> Fetch the complete documentation index at: https://docs.snowflake.com/llms.txt
> Use this file to discover all available pages before exploring further.

<a id="provider-basic-information"></a>

<a id="standard-listing-auto-fulfillment-ui"></a>

<a id="manually-replicate-the-data"></a>

<a id="provider"></a>

<a id="private-listing"></a>

<a id="about-listing-consumers"></a>

# Query syntax

- [](/guides-overview-queries)
- [](/sql-reference/operators)
- [](/user-guide/querying-stage)
- [](/user-guide/querying-metadata)

Snowflake supports querying using standard [](/sql-reference/sql/select) statements and the following basic syntax:

```sqlsyntax
[ WITH ... ]
SELECT
  [ TOP <n> ]
   ...
[ INTO ... ]
[ FROM ...
   [ AT | BEFORE ... ]
   [ CHANGES ... ]
   [ CONNECT BY ... ]
   [ JOIN ... ]
   [ ASOF JOIN ... ]
   [ LATERAL ... ]
   [ MATCH_RECOGNIZE ... ]
   [ PIVOT | UNPIVOT ... ]
   [ VALUES ... ]
   [ SAMPLE ... ]
   [ RESAMPLE ... ]
   [ SEMANTIC_VIEW( ... ) ] ]
[ WHERE ... ]
[ GROUP BY ... ]
[ HAVING ... ]
[ QUALIFY ... ]
[ ORDER BY ... ]
[ LIMIT ... ]
[ FOR UPDATE ... ]
```

**Next Topics:**

- [](/sql-reference/constructs/with)
- [](/sql-reference/constructs/top_n)
- [](/sql-reference/constructs/into)
- [](/sql-reference/constructs/from)
  - [](/sql-reference/constructs/at-before)
  - [](/sql-reference/constructs/changes)
  - [](/sql-reference/constructs/connect-by)
  - [](/sql-reference/constructs/join)
  - [](/sql-reference/constructs/asof-join)
  - [LATERAL](/sql-reference/constructs/join-lateral)
  - [](/sql-reference/constructs/match_recognize)
  - [](/sql-reference/constructs/pivot)
  - [](/sql-reference/constructs/unpivot)
  - [](/sql-reference/constructs/values)
  - [](/sql-reference/constructs/sample)
  - [](/sql-reference/constructs/resample)
  - [](/sql-reference/constructs/semantic_view)

- [](/sql-reference/constructs/where)
- [](/sql-reference/constructs/group-by)
  - [](/sql-reference/constructs/group-by-cube)
  - [](/sql-reference/constructs/group-by-grouping-sets)
  - [](/sql-reference/constructs/group-by-rollup)
  - [](/sql-reference/constructs/having)

- [](/sql-reference/constructs/qualify)
- [](/sql-reference/constructs/order-by)
- [](/sql-reference/constructs/limit)
- [](/sql-reference/constructs/for-update)