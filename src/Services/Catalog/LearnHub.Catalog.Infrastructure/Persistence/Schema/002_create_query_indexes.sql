-- Catalog schema, version 002: indexes for the versioned read queries. Each one names the query that needs it.
-- 001's constraints already create pk_courses (id), uq_courses_slug (slug), pk_lessons (id) and
-- uq_lessons_course_position (course_id, position). PostgreSQL does not index a foreign key column by itself, but
-- uq_lessons_course_position starts with course_id, so it already serves fk_lessons_course, its cascade, the outline
-- and the lesson joins. No index duplicates those, and none is added on status alone (three values, low selectivity).
-- Whether the planner uses these is measured with EXPLAIN, not assumed.

-- list_published_courses*.sql: only published rows, in (title, slug) order, which the keyset cursor compares.
-- Partial, so drafts and archived courses cost nothing here. The query must repeat status = 'published' literally.
CREATE INDEX ix_courses_published_title_slug
    ON courses (title, slug)
    WHERE status = 'published';

-- list_instructor_courses.sql: equality on instructor_id first, then its (title, slug) order. Every status.
CREATE INDEX ix_courses_instructor_title_slug
    ON courses (instructor_id, title, slug);
