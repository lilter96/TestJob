CREATE TABLE IF NOT EXISTS elements
(
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    attribute_value text,
    html            text NOT NULL
);
