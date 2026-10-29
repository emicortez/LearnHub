-- Public catalog listing, first page: the same rows, totals and order as list_published_courses.sql.
-- $1 = page size. Each following page starts after this page's last row (list_published_courses_next_page.sql).
-- The page of courses is picked first and only its lessons are aggregated: limiting after the GROUP BY would
-- aggregate every published course to return a few.
WITH page AS (
    SELECT
        c.id,
        c.title,
        c.slug,
        c.price_amount,
        c.price_currency
    FROM courses AS c
    WHERE c.status = 'published'
    ORDER BY
        c.title,
        c.slug
    LIMIT $1
)
SELECT
    p.id,
    p.title,
    p.slug,
    p.price_amount,
    p.price_currency,
    count(l.id) AS lesson_count,
    coalesce(sum(l.duration_seconds), 0) AS total_duration_seconds
FROM page AS p
LEFT JOIN lessons AS l
    ON l.course_id = p.id
GROUP BY
    p.id,
    p.title,
    p.slug,
    p.price_amount,
    p.price_currency
ORDER BY
    p.title,
    p.slug;
