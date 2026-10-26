CREATE TABLE addresses (
    is_primary        bool                          NOT NULL,
    id                uuid                          DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id           uuid                          NULL,
    date_created      timestamptz                   NOT NULL,
    last_modified     timestamptz                   NULL,
    address           text                          NULL,
    city              text                          NOT NULL,
    country           text                          NOT NULL,
    line1             text                          NOT NULL,
    line2             text                          NOT NULL,
    postal_code       text                          NOT NULL,
    state_or_province text                          NOT NULL,
    additional_data   json                          NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);