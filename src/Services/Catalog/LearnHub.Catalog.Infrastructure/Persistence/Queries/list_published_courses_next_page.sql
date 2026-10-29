-- Public catalog listing, every page after the first (keyset pagination, instead of OFFSET).
-- $1, $2 = title and slug of the previous page's last row; $3 = page size.
-- The cursor is the whole sort key: comparing the title alone would skip courses that share the last row's title.
-- The row comparison (title, slug) > ($1, $2) is lexicographic and uses the same collation as the ORDER BY.
-- A separate statement from the first page, so neither needs an "$1 IS NULL OR ..." condition.
WITH page AS (
    SELECT
        c.id,
        c.title,
        c.slug,
        c.price_amount,
        c.price_currency
    FROM courses AS c
    WHERE c.status = 'published'
      AND (c.title, c.slug) > ($1, $2)
    ORDER BY
        c.title,
        c.slug
    LIMIT $3
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
