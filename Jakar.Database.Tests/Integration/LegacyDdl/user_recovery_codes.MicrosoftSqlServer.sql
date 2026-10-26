CREATE TABLE user_recovery_codes (
    key_id       uniqueidentifier              NOT NULL,
    value_id     uniqueidentifier              NOT NULL,
    date_created datetimeoffset                NOT NULL,

    FOREIGN KEY (key_id)   REFERENCES users(id),
    FOREIGN KEY (value_id) REFERENCES recovery_codes(id),
    PRIMARY KEY (key_id, value_id)

);