# Product vision

A living, one-page north star for LearnHub. It states what the product is for and what it is not; individual features are decided as the work reaches them.

## Why LearnHub exists

LearnHub has two goals, in this order:

1. **Learning by doing.** It is the hands-on project of a structured .NET and architecture curriculum. Each feature exists because it exercises a real skill, and the curriculum decides when it is built.
2. **A useful product.** It should be good enough to use and to show: a course platform worth opening.

When the two conflict, learning wins.

## The problem

Online courses are mostly passive: long videos, little help when you get stuck, and nothing that adapts to what you already know. Students drop out halfway, and good free material, like the best YouTube videos on a topic, is scattered and hard to follow as a path.

## Who it is for

| Role | Who | Wants |
|---|---|---|
| **Platform admin** | The LearnHub operator (the owner of the platform, like Udemy itself) | Manage users and instructors, moderate courses, and see how the platform is doing |
| **Instructor** | Creates and sells courses | Publish and price courses built from their own videos or curated ones, without running video infrastructure |
| **Student** | Any registered user who learns | Learn a topic at their own pace, get unstuck quickly, and see their progress |

Roles are claims issued by Identity ([context map](../architecture/context-map.md)); each service authorizes with them.

Users sign up and sign in with Google or with their own email and password. Both go through the identity provider, so LearnHub never sees a password ([ADR 0009](../architecture/adr/0009-delegated-authentication-with-entra-external-id.md)).

### Who may do what

| Rule | Status |
|---|---|
| Only a user with the `instructor` role creates courses, and only through an app granted the `courses.write` scope | Built |
| Only the course's instructor publishes it; anyone else gets 403 | Built |
| Becoming an instructor needs a platform admin's approval; signing up grants no instructor role | Decided (assigned in the identity provider) |
| Instructor and student are separate accounts. An instructor may link a separate student account to switch quickly, but each keeps its own roles, tokens, and courses | Decided, not built |
| An admin never publishes or unpublishes on an instructor's behalf; they can suspend a course and request changes | Decided, not built |
| A draft is visible only to its instructor and to admins; anyone else gets 404. Published courses are public | Decided, not built |
| Editorial flow: the instructor submits a final version (`InReview`), an admin approves it or requests changes, and only after `Approved` does the instructor publish. Nothing is published while in review | Decided, not built |

The rules that are not built yet arrive with the features that need them: lessons, then review and moderation. Today a course goes straight from draft to published.

## Key capabilities

| Capability | What it means | Bounded context |
|---|---|---|
| Courses from YouTube videos | Courses are made of lessons that embed YouTube videos, including curated lists of individual videos | Catalog |
| AI tutor | Answers questions about the course content and helps when the student is stuck | AI Tutor (core) |
| AI summaries | Short summaries of lessons and videos to review or preview them | AI Tutor (core) |
| Enrollment | Free or paid enrollment, with the price fixed at enrollment time | Enrollment |
| Progress | Track completed lessons and pick up where you left off | Progress |
| Live viewers | "N people are viewing this course now", updated in real time | Catalog |
| Instructor announcements | Instructors notify enrolled students about content updates or special classes: a persisted inbox, pushed live to connected students | To be decided (needs Enrollment) |
| Lesson Q&A | Students ask questions on a lesson and instructors answer, asynchronously as on Udemy (not real time) | To be decided |
| Student whiteboard | A board to sketch and take notes while studying (postponed) | To be decided |

The AI capabilities are what make LearnHub different from a classic course marketplace, and they get the most design effort ([context map](../architecture/context-map.md)).

## What LearnHub is not

- **Not a video host.** Videos are embedded from YouTube; there is no upload, transcoding, or streaming.
- **Not an identity or payments product.** Both use external providers.
- **Not built for scale first.** It targets a demo load within a USD 20 monthly budget ([quality attributes](../architecture/quality-attributes.md)).

## Open questions

- **Business model:** paid courses through a payment provider, free courses only, or both?
- **Source for AI summaries:** a video's transcript is not freely downloadable through the official YouTube API unless you own the video. Summaries may need instructor-provided transcripts or notes.
- **Whiteboard scope:** personal notes per lesson, or shared between students?

Each question is answered when the feature that depends on it is built. Answers that are hard to reverse become ADRs.
