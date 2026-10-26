CREATE TABLE files (
    file_size        bigint                        NOT NULL,
    id               uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    date_created     datetimeoffset                NOT NULL,
    last_modified    datetimeoffset                NULL,
    mime_types       nvarchar(17)                  NULL,
    file_description nvarchar(max)                 NULL,
    file_name        nvarchar(max)                 NULL,
    file_type        nvarchar(max)                 NULL,
    full_path        nvarchar(450)                 NULL UNIQUE,
    hash             nvarchar(max)                 NOT NULL,
    payload          nvarchar(max)                 NOT NULL,
    additional_data  nvarchar(max)                 NULL
);