-- A published course's outline: its lessons in position order, each with the second it starts at,
-- its share of the course, and the course total. @course_id is the course id.
-- The quoted aliases are the OutlineLesson properties that EF Core fills.
-- The join to courses keeps drafts and archived courses private: their outline is empty, like an unknown id.
-- Window functions keep one row per lesson, where GROUP BY would collapse them into one row per course.
-- position is unique per course (uq_lessons_course_position), so the running sum has no ties.
-- 100.0 makes the division numeric; with integers 300 * 100 / 1800 truncates to 16.
SELECT
    l.id AS "Id",
    l.position AS "Position",
    l.title AS "Title",
    l.duration_seconds AS "DurationSeconds",
    sum(l.duration_seconds) OVER (
        PARTITION BY l.course_id
        ORDER BY l.position
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) - l.duration_seconds AS "StartOffsetSeconds",
    round(
        l.duration_seconds * 100.0
        / sum(l.duration_seconds) OVER (PARTITION BY l.course_id),
        1
    ) AS "PercentageOfCourse",
    sum(l.duration_seconds) OVER (PARTITION BY l.course_id) AS "TotalDurationSeconds"
FROM lessons AS l
INNER JOIN courses AS c
    ON c.id = l.course_id
WHERE l.course_id = @course_id
  AND c.status = 'published'
ORDER BY l.position;
