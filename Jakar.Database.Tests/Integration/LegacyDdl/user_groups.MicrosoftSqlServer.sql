CREATE TABLE user_groups (
    key_id       uniqueidentifier              NOT NULL,
    value_id     uniqueidentifier              NOT NULL,
    date_created datetimeoffset                NOT NULL,

    FOREIGN KEY (key_id)   REFERENCES users(id),
    FOREIGN KEY (value_id) REFERENCES groups(id),
    PRIMARY KEY (key_id, value_id)

);