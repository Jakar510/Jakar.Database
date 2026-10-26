CREATE TABLE user_adreesses (
    key_id       uniqueidentifier              NOT NULL,
    value_id     uniqueidentifier              NOT NULL,
    date_created datetimeoffset                NOT NULL,

    FOREIGN KEY (key_id)   REFERENCES users(id),
    FOREIGN KEY (value_id) REFERENCES addresses(id),
    PRIMARY KEY (key_id, value_id)

);