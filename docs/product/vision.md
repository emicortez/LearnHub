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

## Key capabilities

| Capability | What it means | Bounded context |
|---|---|---|
| Courses from YouTube videos | Courses are made of lessons that embed YouTube videos, including curated lists of individual videos | Catalog |
| AI tutor | Answers questions about the course content and helps when the student is stuck | AI Tutor (core) |
| AI summaries | Short summaries of lessons and videos to review or preview them | AI Tutor (core) |
| Enrollment | Free or paid enrollment, with the price fixed at enrollment time | Enrollment |
| Progress | Track completed lessons and pick up where you left off | Progress |
| Student whiteboard | A real-time board to sketch and take notes while studying | To be decided |

The AI capabilities are what make LearnHub different from a classic course marketplace, and they get the most design effort ([context map](../architecture/context-map.md)).

## What LearnHub is not

- **Not a video host.** Videos are embedded from YouTube; there is no upload, transcoding, or streaming.
- **Not an identity or payments product.** Both use external providers.
- **Not built for scale first.** It targets a demo load within a USD 20 monthly budget ([quality attributes](../architecture/quality-attributes.md)).

## Open questions

- **Business model:** paid courses through a payment provider, free courses only, or both?
- **Who can teach:** can any student become an instructor, or does a platform admin approve instructors?
- **Multiple roles:** can one user be an instructor and a student at the same time? (Likely yes, as on Udemy.)
- **Source for AI summaries:** a video's transcript is not freely downloadable through the official YouTube API unless you own the video. Summaries may need instructor-provided transcripts or notes.
- **Whiteboard scope:** personal notes per lesson, or shared between students?

Each question is answered when the feature that depends on it is built. Answers that are hard to reverse become ADRs.
