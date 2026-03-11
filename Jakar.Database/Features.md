
# Database Type Compatibility (PostgresType vs Major Databases)

Databases compared:

- SQLite
- PostgreSQL
- Microsoft SQL Server
- Oracle
- MySQL
- Firebird

Legend:

✓ = Native or cleanly supported  
~ = Supported but with limitations or different storage representation  
✗ = Not supported natively

| PostgresType | SQLite | PostgreSQL | SQL Server | Oracle | MySQL | Firebird |
|---|---|---|---|---|---|---|
| Boolean | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Short (smallint) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| UShort | ~ | ~ | ~ | ~ | ~ | ~ |
| Int | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| UInt | ~ | ~ | ~ | ~ | ~ | ~ |
| Long | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| ULong | ~ | ~ | ~ | ~ | ~ | ~ |
| Int128 | ✗ | ~ | ✗ | ~ | ✗ | ✗ |
| UInt128 | ✗ | ~ | ✗ | ~ | ✗ | ✗ |
| Single (float/real) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Double | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Decimal | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Numeric | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Money | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ |
| Char | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| String (text/varchar) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| CiText | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Binary (bytea/blob) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Date | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Time | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| DateTime | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| DateTimeOffset | ✗ | ✓ | ✓ | ✓ | ✗ | ✗ |
| TimeSpan (interval) | ✗ | ✓ | ✗ | ✓ | ✗ | ✗ |
| Guid / UUID | ~ | ✓ | ✓ | ~ | ✓ | ✗ |
| Json | ~ | ✓ | ~ | ✓ | ✓ | ✗ |
| Jsonb | ✗ | ✓ | ✗ | ✗ | ~ | ✗ |
| JsonPath | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Xml | ✗ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Bit | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| VarBit | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Byte | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| SByte | ~ | ~ | ~ | ~ | ~ | ~ |
| Inet | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Cidr | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| MacAddr | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| MacAddr8 | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Box | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Circle | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Line | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LineSegment | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Path | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Point | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| Polygon | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| Geometry | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| Geography | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| TsVector | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| TsQuery | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| RegConfig | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Hstore | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| RefCursor | ✗ | ✓ | ✓ | ✓ | ✓ | ✗ |
| Oid | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| OidVector | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Xid | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Xid8 | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Cid | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| RegType | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Tid | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| PgLsn | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LTree | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LQuery | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LTxtQuery | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| IntVector (array) | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LongVector (array) | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| FloatVector (array) | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| DoubleVector (array) | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| IntegerRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| BigIntRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| NumericRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| TimestampRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| DateTimeOffsetRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| DateRange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| IntMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| LongMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| NumericMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| TimestampMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| DateTimeOffsetMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| DateMultirange | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |

## Summary

### Cleanly portable across all six databases
These types are safest for cross‑database schema generation:

- Boolean
- Short
- Int
- Long
- Single
- Double
- Decimal / Numeric
- Char
- String
- Binary
- Date
- Time
- DateTime

### PostgreSQL‑specific ecosystem

These types exist only in PostgreSQL (or require extensions):

- Arrays (`IntVector`, `LongVector`, etc.)
- Range types
- Multirange types
- Network types (`Inet`, `Cidr`, `MacAddr`)
- `Jsonb`
- `Hstore`
- `LTree`
- `PgLsn`
- Internal OID/XID types
- `TsVector`, `TsQuery`
- `CiText`

These are powerful but significantly reduce portability when designing a cross‑database ORM or schema layer.
