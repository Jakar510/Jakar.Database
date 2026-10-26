CREATE TABLE files (
    file_size        bigint                        NOT NULL,
    id               uuid                          DEFAULT gen_random_uuid() PRIMARY KEY,
    date_created     timestamptz                   NOT NULL,
    last_modified    timestamptz                   NULL,
    mime_types       varchar(17)                   NULL,
    file_description text                          NULL,
    file_name        text                          NULL,
    file_type        text                          NULL,
    full_path        text                          NULL UNIQUE,
    hash             text                          NOT NULL,
    payload          text                          NOT NULL,
    additional_data  json                          NULL
);