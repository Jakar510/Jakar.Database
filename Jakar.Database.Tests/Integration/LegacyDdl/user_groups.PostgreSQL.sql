CREATE TABLE user_groups (
    key_id       uuid                          NOT NULL,
    value_id     uuid                          NOT NULL,
    date_created timestamptz                   NOT NULL,

    FOREIGN KEY (key_id)   REFERENCES users(id),
    FOREIGN KEY (value_id) REFERENCES groups(id),
    PRIMARY KEY (key_id, value_id)

);