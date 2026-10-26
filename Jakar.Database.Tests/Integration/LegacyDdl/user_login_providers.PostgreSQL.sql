CREATE TABLE user_login_providers (
    id                    uuid                          DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id               uuid                          NOT NULL,
    date_created          timestamptz                   NOT NULL,
    last_modified         timestamptz                   NULL,
    login_provider        text                          NOT NULL,
    provider_display_name text                          NULL,
    provider_key          text                          NOT NULL,
    value                 text                          NULL,
    additional_data       json                          NULL,

    FOREIGN KEY (user_id) REFERENCES users(id)
);