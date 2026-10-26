CREATE TABLE roles (
    id                uuid                          DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id           uuid                          NOT NULL,
    date_created      timestamptz                   NOT NULL,
    last_modified     timestamptz                   NULL,
    concurrency_stamp text                          NOT NULL,
    name_of_role      text                          NOT NULL UNIQUE,
    normalized_name   text                          NOT NULL UNIQUE,
    rights            text                          NOT NULL,
    additional_data   json                          NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);