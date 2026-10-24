-- Catalog schema, version 001: the Course aggregate as two tables.
-- The domain validates first to return useful errors; these constraints keep the stored data valid whatever writes it
-- (scripts, migrations, bugs, concurrent requests). Every constraint is named so a violation says which rule failed.
-- Applied by the schema tests today; EF Core migrations take over in module 6 and must reproduce this contract.
-- "Not blank" means at least one non-whitespace character: btrim() alone trims only spaces, so a tab-only title would
-- pass, while the domain's string.Trim() removes every kind of whitespace.

CREATE TABLE courses
(
    id uuid NOT NULL,
    -- The instructor's Entra `oid`. No foreign key: identities live in the identity provider (ADR 0009).
    instructor_id uuid NOT NULL,
    title text NOT NULL,
    slug text NOT NULL,
    -- Unconstrained numeric plus explicit checks: numeric(12, 2) would silently round 10.129 to 10.13.
    price_amount numeric NOT NULL,
    price_currency char(3) NOT NULL,
    status text NOT NULL DEFAULT 'draft',

    CONSTRAINT pk_courses
        PRIMARY KEY (id),

    CONSTRAINT uq_courses_slug
        UNIQUE (slug),

    CONSTRAINT ck_courses_instructor_id_not_empty
        CHECK (instructor_id <> '00000000-0000-0000-0000-000000000000'::uuid),

    CONSTRAINT ck_courses_title_length
        CHECK (title ~ '[^[:space:]]' AND char_length(btrim(title)) <= 120),

    CONSTRAINT ck_courses_slug_not_blank
        CHECK (slug ~ '[^[:space:]]'),

    CONSTRAINT ck_courses_price_range
        CHECK (price_amount BETWEEN 0 AND 9999999999.99),

    CONSTRAINT ck_courses_price_scale
        CHECK (price_amount = round(price_amount, 2)),

    CONSTRAINT ck_courses_currency
        CHECK (price_currency::text ~ '^[A-Z]{3}$'),

    CONSTRAINT ck_courses_status
        CHECK (status IN ('draft', 'published', 'archived'))
);

-- Lessons belong to the Course aggregate: no life of their own, so deleting a course deletes them.
-- Courses are normally archived, not deleted. Tables have no order, so the aggregate's list order is stored.
CREATE TABLE lessons
(
    id uuid NOT NULL,
    course_id uuid NOT NULL,
    title text NOT NULL,
    duration_seconds integer NOT NULL,
    position integer NOT NULL,

    CONSTRAINT pk_lessons
        PRIMARY KEY (id),

    CONSTRAINT fk_lessons_course
        FOREIGN KEY (course_id)
        REFERENCES courses (id)
        ON DELETE CASCADE,

    CONSTRAINT uq_lessons_course_position
        UNIQUE (course_id, position),

    CONSTRAINT ck_lessons_title_not_blank
        CHECK (title ~ '[^[:space:]]'),

    CONSTRAINT ck_lessons_duration_positive
        CHECK (duration_seconds > 0),

    CONSTRAINT ck_lessons_position_positive
        CHECK (position > 0)
);
