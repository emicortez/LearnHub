-- One instructor's courses in every status (drafts and archived included), with lesson count and total duration.
-- $1 is the instructor id (the Entra oid). Same join, aggregates and ordering as list_published_courses.sql.
SELECT
    c.id,
    c.title,
    c.slug,
    c.status,
    c.price_amount,
    c.price_currency,
    count(l.id) AS lesson_count,
    coalesce(sum(l.duration_seconds), 0) AS total_duration_seconds
FROM courses AS c
LEFT JOIN lessons AS l
    ON l.course_id = c.id
WHERE c.instructor_id = $1
GROUP BY
    c.id,
    c.title,
    c.slug,
    c.status,
    c.price_amount,
    c.price_currency
ORDER BY
    c.title,
    c.slug;
