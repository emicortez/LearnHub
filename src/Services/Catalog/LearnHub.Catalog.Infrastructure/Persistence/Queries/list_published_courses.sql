-- Public catalog listing: every published course with its lesson count and total duration.
-- LEFT JOIN keeps a course without lessons; count(l.id) and coalesce(sum(...), 0) give it 0 and 0,
-- where count(*) would count the course row itself and sum would return NULL.
-- Title is not unique, so the unique slug breaks ties and the order is deterministic.
SELECT
    c.id,
    c.title,
    c.slug,
    c.price_amount,
    c.price_currency,
    count(l.id) AS lesson_count,
    coalesce(sum(l.duration_seconds), 0) AS total_duration_seconds
FROM courses AS c
LEFT JOIN lessons AS l
    ON l.course_id = c.id
WHERE c.status = 'published'
GROUP BY
    c.id,
    c.title,
    c.slug,
    c.price_amount,
    c.price_currency
ORDER BY
    c.title,
    c.slug;
