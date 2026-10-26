CREATE TABLE recovery_codes (
    id              uuid                          DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id         uuid                          NOT NULL,
    date_created    timestamptz                   NOT NULL,
    last_modified   timestamptz                   NULL,
    code            text                          NOT NULL UNIQUE,
    additional_data json                          NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);