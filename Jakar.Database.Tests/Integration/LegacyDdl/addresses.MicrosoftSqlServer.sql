CREATE TABLE addresses (
    is_primary        bit                           NOT NULL,
    id                uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    user_id           uniqueidentifier              NULL,
    date_created      datetimeoffset                NOT NULL,
    last_modified     datetimeoffset                NULL,
    address           nvarchar(450)                 NULL,
    city              nvarchar(450)                 NOT NULL,
    country           nvarchar(450)                 NOT NULL,
    line1             nvarchar(450)                 NOT NULL,
    line2             nvarchar(450)                 NOT NULL,
    postal_code       nvarchar(450)                 NOT NULL,
    state_or_province nvarchar(450)                 NOT NULL,
    additional_data   nvarchar(max)                 NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);