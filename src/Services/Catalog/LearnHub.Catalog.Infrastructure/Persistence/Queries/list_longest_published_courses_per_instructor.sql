-- Up to three published courses per instructor, longest first.
-- course_totals aggregates first (one row per course, 0 for a course without lessons);
-- ranked_courses numbers them within each instructor. The rank filter goes outside, because
-- WHERE runs before window functions, and LIMIT 3 would cut the whole result, not each instructor.
-- The unique slug breaks ties, so exactly three make the cut and the choice is deterministic.
WITH course_totals AS (
    SELECT
        c.id,
        c.instructor_id,
        c.title,
        c.slug,
        coalesce(sum(l.duration_seconds), 0) AS total_duration_seconds
    FROM courses AS c
    LEFT JOIN lessons AS l
        ON l.course_id = c.id
    WHERE c.status = 'published'
    GROUP BY
        c.id,
        c.instructor_id,
        c.title,
        c.slug
),
ranked_courses AS (
    SELECT
        ct.id,
        ct.instructor_id,
        ct.title,
        ct.slug,
        ct.total_duration_seconds,
        row_number() OVER (
            PARTITION BY ct.instructor_id
            ORDER BY ct.total_duration_seconds DESC, ct.slug
        ) AS rank
    FROM course_totals AS ct
)
SELECT
    instructor_id,
    id,
    title,
    slug,
    total_duration_seconds,
    rank
FROM ranked_courses
WHERE rank <= 3
ORDER BY
    instructor_id,
    rank;
