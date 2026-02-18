// Jakar.Database :: Jakar.Database
// 02/03/2026  14:21

namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum PostgresCollectionType
{
    METADATA_COLLECTIONS,
    RESTRICTIONS,
    DATA_SOURCE_INFORMATION,
    DATA_TYPES,
    RESERVED_WORDS,

    // custom collections for npgsql
    DATABASES,
    SCHEMATA,
    TABLES,
    COLUMNS,
    VIEWS,
    MATERIALIZED_VIEWS,
    USERS,
    INDEXES,
    INDEX_COLUMNS,
    CONSTRAINTS,
    PRIMARY_KEY,
    UNIQUE_KEYS,
    FOREIGN_KEYS,
    CONSTRAINT_COLUMNS
}
