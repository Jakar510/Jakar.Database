CREATE TABLE user_login_providers (
    id                    uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    user_id               uniqueidentifier              NOT NULL,
    date_created          datetimeoffset                NOT NULL,
    last_modified         datetimeoffset                NULL,
    login_provider        nvarchar(450)                 NOT NULL,
    provider_display_name nvarchar(max)                 NULL,
    provider_key          nvarchar(450)                 NOT NULL,
    value                 nvarchar(450)                 NULL,
    additional_data       nvarchar(max)                 NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);