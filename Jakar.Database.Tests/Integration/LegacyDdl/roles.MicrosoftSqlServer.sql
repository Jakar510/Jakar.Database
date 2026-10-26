CREATE TABLE roles (
    id                uniqueidentifier              DEFAULT NEWID() PRIMARY KEY,
    user_id           uniqueidentifier              NOT NULL,
    date_created      datetimeoffset                NOT NULL,
    last_modified     datetimeoffset                NULL,
    concurrency_stamp nvarchar(max)                 NOT NULL,
    name_of_role      nvarchar(450)                 NOT NULL UNIQUE,
    normalized_name   nvarchar(450)                 NOT NULL UNIQUE,
    rights            nvarchar(max)                 NOT NULL,
    additional_data   nvarchar(max)                 NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);