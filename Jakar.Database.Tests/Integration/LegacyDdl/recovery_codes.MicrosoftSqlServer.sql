CREATE TABLE recovery_codes (
    id              uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    user_id         uniqueidentifier              NOT NULL,
    date_created    datetimeoffset                NOT NULL,
    last_modified   datetimeoffset                NULL,
    code            nvarchar(450)                 NOT NULL UNIQUE,
    additional_data nvarchar(max)                 NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);