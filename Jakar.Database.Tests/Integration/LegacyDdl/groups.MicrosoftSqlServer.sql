CREATE TABLE groups (
    id              uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    user_id         uniqueidentifier              NOT NULL,
    date_created    datetimeoffset                NOT NULL,
    last_modified   datetimeoffset                NULL,
    name_of_group   nvarchar(450)                 NOT NULL UNIQUE,
    normalized_name nvarchar(450)                 NULL UNIQUE,
    rights          nvarchar(max)                 NOT NULL,
    additional_data nvarchar(max)                 NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);