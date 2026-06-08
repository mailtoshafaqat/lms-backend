# White-Label AI LMS — Technical Architecture & Build Spec

**Document version:** 1.0
**Date:** June 2026
**Status:** Internal engineering document (not client-facing)
**Companion documents:** `01-Existing-LMS-Features-Reference.md`, `02-White-Label-AI-LMS-Proposal.md`
**Scope:** Backend API in **.NET Core (.NET 10) + SQL Server**, frontend in **Next.js (same stack as MockPilot admin portal)**

---

## 1. Purpose

This document translates the client proposal (Document 02) into a **phase-wise, feature-wise technical plan**: technology choices, solution structure, data model, API surface, and frontend modules. It is the bridge between sign-off and the SOW / sprint backlog.

Build order follows Document 02 §6:

| Phase | Goal |
|-------|------|
| **Phase 1 — MVP** | Match the competitor (feature parity, single brand) |
| **Phase 2 — White-label + differentiation** | Multi-tenant branding + AI/RAG + live classes |
| **Phase 3 — Beat PIS** | In-app Q&A, full mocks, mobile, payments, analytics |

---

## 2. Technology stack

### 2.1 Backend (.NET 10 / .NET Core)

| Concern | Choice | Notes |
|---------|--------|-------|
| Runtime | **.NET 10** | Same major runtime as MockPilot backend |
| API style | ASP.NET Core Web API (controllers) | REST + OpenAPI |
| Database | **SQL Server 2022** | Relational; LMS data is highly relational |
| ORM | **EF Core 10** (`Microsoft.EntityFrameworkCore.SqlServer`) | Code-first + migrations |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` + Google (`Google.Apis.Auth`) | Same packages MockPilot uses |
| API docs | **Swashbuckle.AspNetCore** (Swagger) | Same as MockPilot |
| Validation | **FluentValidation** | Request DTO validation |
| Mapping | **Mapster** or AutoMapper | Entity ↔ DTO |
| Background jobs | **Hangfire** (SQL Server storage) | Email, video transcode hooks, AI jobs |
| Caching | **IMemoryCache** (P1) → **Redis** (P2+) | Tenant config, leaderboards |
| Logging | **Serilog** → console + Seq/App Insights | Structured logs |
| Testing | xUnit + FluentAssertions | Mirrors MockPilot test projects |

> **Difference from MockPilot:** MockPilot backend uses **MongoDB**. This LMS uses **SQL Server + EF Core** (per requirement). The clean-architecture layering is kept identical.

### 2.2 Frontend (same as MockPilot admin portal)

| Concern | Choice |
|---------|--------|
| Framework | **Next.js 15** (App Router) |
| UI runtime | **React 19** |
| Language | **TypeScript 5.8** |
| Styling | **Tailwind CSS 3.4** |
| Components | **shadcn/ui** pattern — Radix (`@radix-ui/react-slot`), `class-variance-authority`, `clsx`, `tailwind-merge` |
| Icons | **lucide-react** |
| Data fetching | Server Components + Route Handlers; **TanStack Query** for client state |
| Forms | **react-hook-form** + **zod** |
| Auth | JWT in httpOnly cookie; middleware route guards |

Two frontends (both Next.js, same component library):
- **Learner/Teacher web app** (student + teacher portal)
- **Admin console** (super-admin + institute-admin) — mirrors `mockpilot-admin-portal` structure

### 2.3 Infrastructure

| Concern | Choice |
|---------|--------|
| File storage | **`IFileStorage` abstraction** — local disk (MVP) → Blob/S3/R2 (later) via DI swap |
| Video | Phase 1: tiny pilot on local disk OR Cloudflare R2/Bunny; later R2/CDN + HLS; Phase 2: Zoom live; Phase 3: native stream + DRM |
| AI (Phase 2) | OpenAI/Azure OpenAI for LLM; **SQL Server 2025 native vectors** for RAG → dedicated vector DB (Azure AI Search / pgvector / Qdrant) only at scale |
| Hosting | Containers (Docker) on Azure App Service / AKS |
| CI/CD | GitHub Actions → build, test, migrate, deploy |

### 2.4 Storage strategy (cost-aware)

All file access goes through a single abstraction so the provider can change without touching feature code:

```csharp
public interface IFileStorage
{
    Task<string> SaveAsync(string path, Stream content);  // returns key/url
    Task<Stream> OpenAsync(string key);
    Task DeleteAsync(string key);
}
// Phase 1: LocalDiskFileStorage  (/app/storage/...)
// Later:   BlobFileStorage / R2FileStorage (swap via DI registration)
```

| Content | Startup (cheapest) | When it grows |
|---------|--------------------|---------------|
| Notes (PDF/HTML), images, thumbnails, docs | **Local disk** ✅ | Blob/R2 via `IFileStorage` swap |
| Video lectures | Local disk for **tiny pilot only**, else cheap host | **Cloudflare R2** (zero egress) or **Bunny.net Stream** + CDN |

**Why:** storage is cheap; the real cost is **bandwidth/egress**, which dominates for **video**. Serving video from the single app server competes with API traffic and won't scale, so move video to object storage + CDN early. **Cloudflare R2** has **no egress fees** — usually cheaper than Azure Blob for video.

**Local-disk caveats (acceptable at startup, but these are the triggers to move to object storage):**
- Works for a **single app instance only** — breaks the moment you scale to 2+ instances.
- Not durable — disk failure / container redeploy can lose files; back up the storage folder or use a persistent volume.
- You manage backups/redundancy yourself (object storage does this automatically).

### 2.5 Database hosting (cost-aware)

EF Core keeps the app portable across either option (same `SqlServer` provider):

| Option | Cost note |
|--------|-----------|
| **Azure SQL (serverless)** ✅ recommended | No upfront license, **auto-pauses when idle**, managed backups — lowest total cost & effort at startup |
| **Self-hosted SQL Server** | Developer/Express **free** for pilot; **Standard license is costly** for prod + you manage backups/patching/HA |

> Use SQL Server **Developer Edition** locally (free) and **Azure SQL serverless** in prod for the cheapest path. Self-hosted Developer/Express is fine for a tiny pilot. Avoid buying Standard licenses upfront.

---

## 3. Solution architecture

### 3.1 Backend project layout (clean architecture, mirrors MockPilot)

```
Lms.sln
├── Lms.Domain            // entities, enums, domain logic (no deps)
├── Lms.Application        // use-cases, DTOs, interfaces, validators
├── Lms.Infrastructure     // EF Core, SQL Server, repositories, external services
├── Lms.Migrations         // EF Core migrations
├── Lms.API                // student/teacher REST API (JWT)
├── Lms.AdminAPI           // super-admin + institute-admin API
└── tests/Lms.Tests        // xUnit
```

### 3.2 Multi-tenancy strategy

- **Phase 1:** single tenant, but **`TenantId` column baked into every table from day one** (shared-DB, shared-schema). This avoids a costly re-architecture in Phase 2.
- **Phase 2:** activate tenant resolution by **subdomain/custom domain** → `TenantId` injected into a `ITenantContext` and applied as a **global EF Core query filter** (`HasQueryFilter`). Per-tenant branding/config table drives logo, colours, feature flags.
- Data isolation: row-level by `TenantId`; optional dedicated DB per large tenant later.

### 3.3 Request flow

```
Next.js (RSC / route handler)
   → API Gateway / reverse proxy
      → Lms.API controller
         → Application use-case (validation, authz)
            → Infrastructure repository (EF Core → SQL Server)
```

### 3.4 Modular architecture (modular monolith + feature-flag modules)

The system is a **modular monolith**: one deployable app, internally split into self-contained **feature modules**. Each module owns its entities, use-cases, and endpoints, and is gated by a **per-tenant feature flag** (plug-and-play per institute — no code forks).

```
Lms.API                      // one deployable
Lms.Modules.Courses          // bundles, subjects, units, topics
Lms.Modules.Assessments      // MCQ/DPT, attempts, results
Lms.Modules.Flashcards
Lms.Modules.Grades           // grades + leaderboard
Lms.Modules.Enrollment
Lms.Modules.LiveClasses      // Phase 1: Zoom; Phase 2/3: native stream
Lms.Modules.Branding         // Phase 2: white-label themes + landing-page builder
Lms.Modules.MistakeDiary     // Phase 2
Lms.Modules.AskKitab         // Phase 2: RAG (extractable to a service later)
Lms.Modules.Payments         // Phase 3
Lms.Modules.QnA              // Phase 3: in-app teacher Q&A
Lms.Shared                   // tenant context, auth, common
```

**Plug-and-play = feature flags per tenant**, not microservices or runtime plugins. A clean module boundary also lets any single module (e.g. `AskKitab`, video) be **extracted into a microservice later** without rewriting the app.

### 3.5 Module communication — hybrid model (market standard)

Modules **never** read each other's tables or internal code. They talk through **stable contracts**, using a **hybrid** of two mechanisms — the industry-standard approach:

- **Interface (synchronous call)** — when the caller needs a result *now*, or the side-effect must be **consistent in the same transaction**.
- **Event (publish/subscribe)** — when "something happened" and other modules **optionally** react (fan-out, flag-controlled).

**Decision rule:**

| Situation | Mechanism | Why |
|-----------|-----------|-----|
| Need an answer/decision now | **Interface** | Synchronous, returns a value |
| Must be consistent in same transaction | **Interface** (or event + Outbox) | No data loss |
| Optional / fan-out / flag-controlled reaction | **In-memory event** (MediatR) | Decoupled, plug-and-play |
| Must-not-be-lost or crosses a service/process | **Event + Outbox (+ queue)** | Durable, retryable |
| Heavy/slow background work | **Event → queue → worker** (Hangfire/queue) | Don't block the request |

**LMS mapping:**

```
Enrollment check before quiz        → interface  (must be correct now)
AI quota check / deduct             → interface  (must be consistent)
QuizSubmitted → grade recalculation → interface OR event+Outbox (must not lose)
QuizSubmitted → leaderboard         → in-memory event (optional/flagged)
QuizSubmitted → mistake diary       → in-memory event (optional/flagged)
Send email / push notification      → event + Outbox (+ queue later)
Video transcode / AI ingestion      → event → queue → worker (heavy/async)
```

**Event delivery progression (add infra only when justified):**

| Stage | Mechanism | Infrastructure |
|-------|-----------|----------------|
| Phase 1 (MVP) | In-memory events (MediatR notifications) | None |
| Phase 1–2, critical side-effects | In-memory **+ Outbox table** | SQL Server + Hangfire |
| Phase 2–3, heavy async | Real queue for specific jobs (video, AI ingest, bulk push) | Azure Service Bus / RabbitMQ |
| Module extracted to a service | Message queue between services | Queue required |

> Start with interfaces + in-memory events (zero extra infra). Add the **Outbox** when a side-effect must not be lost. Introduce a **message queue** only for cross-process/microservice or heavy-async needs — not upfront.

---

## 4. Core data model (Phase 1 foundation)

Key entities (all carry `TenantId`, `CreatedAt`, `UpdatedAt`):

| Entity | Key fields | Relationships |
|--------|-----------|---------------|
| `Tenant` | Id, Name, Subdomain, BrandingJson, FeatureFlags | 1–* everything |
| `User` | Id, Email, Role, PasswordHash/Provider | belongs to Tenant |
| `Bundle` | Id, Title, Price, ValidityDays | has many Subjects (via Enrollment) |
| `Subject` | Id, Title, Order | has many Units |
| `Unit` | Id, SubjectId, Title, Order | has many Topics |
| `Topic` | Id, UnitId, Title, Order | has Video, Notes, MCQs, Flashcards |
| `Lecture/Video` | Id, TopicId, Url, DurationSec | belongs to Topic |
| `Note` | Id, TopicId, ContentUrl/Html | belongs to Topic |
| `Question` (MCQ) | Id, TopicId, Stem, Options, CorrectKey, Explanation | belongs to Topic |
| `Quiz/DPT` | Id, TopicId/UnitId, Type, QuestionRefs | Daily Practice Test |
| `Attempt` | Id, UserId, QuizId, Score, AnswersJson, SubmittedAt | grades source |
| `Flashcard` | Id, TopicId, Front, Back | belongs to Topic |
| `Enrollment` | Id, UserId, BundleId, ExpiresAt | access control |
| `Grade` | derived from Attempt aggregates | per user/subject |
| `LeaderboardEntry` | UserId, Score, Rank (cached) | per tenant/bundle |

Phase 1 also adds: `LiveClass`, `Recording` (Zoom — see §6.9).
Phase 2+ adds: `BrandingTheme`, `LandingPage`/`PageSection` (white-label page builder), `MistakeDiaryEntry`, `Document`/`Embedding` (RAG), native-stream extensions to `LiveClass`/`Recording`.
Phase 3 adds: `QnAThread`, `MockExam`, `Payment`, `Subscription`, `ParentLink`, analytics rollups.

---

## 5. API conventions

- Base path: `/api/v1/...`; admin: `/api/v1/admin/...`
- Auth: `Authorization: Bearer <jwt>`; tenant resolved from host header (P2) or claim.
- Pagination: `?page=&pageSize=`; responses `{ data, page, pageSize, total }`.
- Errors: RFC 7807 `ProblemDetails`.
- All list endpoints tenant-scoped automatically via query filter.

---

## 6. Phase 1 — MVP (match the video)

Single brand, feature parity. Goal: a student can enroll, learn, practise, and see grades; an admin can upload all content.

### 6.1 Auth & accounts
- **Entities:** `User`, `Tenant` (single seeded), refresh tokens.
- **API:** `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me`, Google login.
- **Frontend:** login/register pages, JWT cookie, route middleware, role-based layout.

### 6.2 Dashboard + course hierarchy
- **Entities:** `Bundle`, `Subject`, `Unit`, `Topic`.
- **API:** `GET /bundles`, `GET /bundles/{id}`, `GET /subjects/{id}/units`, `GET /units/{id}/topics`, `GET /topics/{id}`.
- **Frontend:** student dashboard (enrolled bundles, continue learning), course tree navigation, topic detail page.

### 6.3 Video lectures + notes
- **Entities:** `Lecture/Video`, `Note`.
- **API:** `GET /topics/{id}/video`, `GET /topics/{id}/notes`, signed playback URL endpoint.
- **Frontend:** HLS video player (mark-as-watched, resume position), notes viewer/download.

### 6.4 MCQ engine (DPT + results + explanations)
- **Entities:** `Question`, `Quiz/DPT`, `Attempt`.
- **API:** `GET /quizzes/{id}`, `POST /quizzes/{id}/attempts`, `GET /attempts/{id}` (with per-question explanation).
- **Frontend:** quiz runner (timer, navigation), result screen with correct/incorrect + explanation, retry.

### 6.5 Flashcards
- **Entities:** `Flashcard`.
- **API:** `GET /topics/{id}/flashcards`.
- **Frontend:** flip-card deck per topic, "known / review" toggle (local in P1).

### 6.6 My Grades + basic leaderboard
- **Entities:** `Grade` (aggregates of `Attempt`), `LeaderboardEntry` (cached).
- **API:** `GET /me/grades`, `GET /me/grades/weak-topics`, `GET /leaderboard?bundleId=`.
- **Frontend:** grades page (per subject/topic, weak-topic highlights), leaderboard table.

### 6.7 Bundle enrollment
- **Entities:** `Enrollment`.
- **API:** `POST /enrollments` (manual/admin grant in P1; payment in P3), `GET /me/enrollments`, access checks middleware.
- **Frontend:** bundle catalog, enroll/access-gated content.

### 6.8 Admin CMS for content
- **API (AdminAPI):** full CRUD for `Bundle/Subject/Unit/Topic/Video/Note/Question/Quiz/Flashcard`, bulk MCQ import (CSV/Excel), media upload.
- **Frontend (Admin console):** content tree editor, MCQ bulk importer, media upload, publish/draft states.

### 6.9 Live classes (Zoom)
- **Entities:** `LiveClass` (schedule, Zoom meeting id/url, batch scope); `Recording` link (optional in P1).
- **Core rule:** audio/video never touches the app server — Zoom handles all media; our API only creates the meeting, issues join token, and stores metadata (keeps the app fast during calls).
- **API:** `GET /live-classes`, `POST /admin/live-classes` (creates Zoom meeting), `GET /live-classes/{id}/join` (returns Zoom signature/URL).
- **Infra:** Zoom API + **Zoom Meeting SDK** embedded in the Next.js page; recording webhook → store link async (Hangfire).
- **Frontend:** schedule list, join button (call runs in isolated SDK component), recording link if available.

**Phase 1 exit criteria:** Document 02 §9 (MVP) all green.

---

## 7. Phase 2 — White-label + differentiation

### 7.1 Multi-tenant branding
- **Entities:** `Tenant` activated, `BrandingTheme` (logo, colours, favicon, email templates), `FeatureFlag`.
- **Infra:** subdomain/custom-domain → `ITenantContext`; EF global query filters; per-tenant config cache (Redis).
- **API (AdminAPI):** `GET/PUT /admin/tenants/{id}/branding`, `GET/PUT /admin/tenants/{id}/features`.
- **Frontend:** theme provider reads tenant config at request time; super-admin tenant manager; institute-admin branding editor.

#### 7.1.1 Customization levels (what tenants can change)

Customization is **config/content-driven, never per-tenant code** — this preserves the single codebase and tenant security.

| Level | Tenant can change | Mechanism | Allowed |
|-------|-------------------|-----------|---------|
| **1. Theme / branding** | Logo, colours, fonts, favicon, email header, domain | `BrandingTheme` tokens (DB per tenant) | ✅ |
| **2. Landing page content** | Hero, banners, feature cards, course showcase, testimonials, stats, footer | **Section-based page builder** (content as data) | ✅ |
| **3. Full layout / custom code** | Arbitrary HTML/CSS/JS, structural rewrites | — | ❌ (forks codebase + XSS/security risk) |

- **Entities:** `LandingPage` (per tenant), `PageSection` (type, order, contentJson, isEnabled).
- **Model:** a landing page = an ordered list of typed sections stored as data:
```
LandingPage(tenantId)
  └── [ PageSection(Hero, {title, subtitle, image, cta}),
        PageSection(Features, {cards[]}),
        PageSection(CoursesShowcase, {...}),
        PageSection(Testimonials, {...}),
        PageSection(Footer, {...}) ]   // reorder / toggle per tenant
```
- **API (AdminAPI):** `GET/PUT /admin/tenants/{id}/landing` (sections + content), `GET /landing` (public render data by tenant).
- **Frontend:** registry of **reusable section components**; renders sections from tenant data at request time. Institute-admin gets a drag-reorder + content editor (Wix/Shopify-style).
- **Rule:** truly unique requests become **new reusable section types** available to all tenants — never one-off code for a single tenant.

### 7.2 Quiz & flashcard builders
- **API (AdminAPI):** authoring endpoints with versioning/preview; teacher role allowed.
- **Frontend:** rich quiz/flashcard builder UI (drag-order, media, bulk).

### 7.3 Mistake diary
- **Entities:** `MistakeDiaryEntry` (auto-created from wrong `Attempt` answers).
- **API:** `GET /me/mistakes`, `POST /me/mistakes/{id}/resolve`.
- **Frontend:** mistake review page, "re-test my mistakes".

### 7.4 Ask Kitab (RAG)
- **Entities:** `Document`, `Chunk`, `Embedding` (vectors).
- **Vector store:** start with **SQL Server 2025 native `VECTOR` type + `VECTOR_DISTANCE`** (one DB, no extra infra); move to a dedicated vector DB (Azure AI Search / pgvector / Qdrant) only at large scale. Swappable behind the `AskKitab` module.
- **Infra:** ingestion pipeline (chunk → embed → index); retrieval + LLM answer with citations; per-tenant content scope; AI quota.
- **API:** `POST /ai/ask` (question → cited answer), `POST /admin/ai/ingest`.
- **Frontend:** Ask Kitab chat panel with citations, scoped to syllabus.

### 7.5 Advanced live classes & native streaming (Phase 2/3)

> Basic **Zoom live classes ship in Phase 1** (see §6.9). Phase 2/3 adds native streaming and richer recording to drop the Zoom dependency.

**Core rule: live audio/video NEVER flows through the .NET app server.** Media goes through a dedicated, auto-scaling media server (Zoom / WebRTC SFU). The API handles only tiny metadata (create class, issue join token, store recording link). This is what keeps the app responsive — even a 2,000-student call is only a few small API requests, not 2,000 streams through our server.

```
Teacher & students ⇄ MEDIA SERVER (Zoom / WebRTC SFU)   ← all heavy A/V here
Lms.API → create class · issue join token · save recording link   (tiny calls only)
```

**Native streaming (drop Zoom dependency), chosen by interactivity:**

| Use case | Tech | Why |
|----------|------|-----|
| Interactive class (students talk, low latency) | **WebRTC + SFU** (LiveKit / Mediasoup / Janus) | real-time, scales one-to-many via SFU |
| One-way broadcast (teacher → many watch) | **RTMP ingest → transcode → LL-HLS + CDN** | cheapest for large audiences |

**Why the app never freezes during a call:**
1. Media bypasses the app server entirely → API latency unaffected by call size.
2. Media servers scale independently of the LMS.
3. Recording handled async (webhook + background job), never inline.
4. Call isolated in its own frontend component/SDK.
5. Cost: Zoom per-host now; native SFU/CDN only when volume justifies it.

**Phase 2 exit criteria:** Document 02 §9 (Phase 2) all green.

---

## 8. Phase 3 — Beat PIS

| Feature | Backend | Frontend |
|---------|---------|----------|
| **In-app teacher Q&A** | `QnAThread`, `QnAMessage`; SignalR realtime; doubt triage hook | chat threads, teacher answer queue |
| **Full mock exams** | `MockExam`, timed multi-section attempts, scoring | exam mode, sectional timer, detailed report |
| **Mobile apps** | same API; push (FCM/APNs) | React Native / Expo (shared design tokens) |
| **Payments + parent portal** | `Payment`, `Subscription`, gateway (Stripe/local); `ParentLink` | checkout, billing, parent progress view |
| **Institute analytics** | rollup tables / OLAP views; scheduled jobs | dashboards (engagement, performance, revenue) |

---

## 9. Cross-cutting concerns

| Concern | Approach |
|---------|----------|
| **Authorization** | Role + tenant claims; policy-based (`[Authorize(Policy="InstituteAdmin")]`); enrollment gate for content |
| **Security** | HTTPS, password hashing (ASP.NET Identity hasher), rate limiting, signed media URLs, input validation |
| **File storage** | `IFileStorage` abstraction: local disk (MVP) → Blob/R2 + CDN; videos as HLS; private signed URLs |
| **Audit/logging** | Serilog structured logs; admin action audit table |
| **Migrations** | EF Core migrations in `Lms.Migrations`; auto-apply on deploy via Hangfire/startup gate |
| **Config** | `appsettings.{env}.json` + secrets manager; per-tenant config in DB |
| **Testing** | Unit (Application), integration (API + SQL via Testcontainers) |

---

## 10. Non-functional requirements

| NFR | Target |
|-----|--------|
| Availability | 99.9% |
| API latency | < 300 ms p95 (non-AI) |
| Video start | < 2 s via CDN |
| Concurrency | 5k concurrent learners per tenant (P2 baseline) |
| Data isolation | strict per-tenant; verified by tests |
| Backups | daily SQL backup, PITR enabled |

---

## 11. Environments & delivery

| Env | Purpose |
|-----|---------|
| Dev | feature work, seeded demo tenant |
| Staging | client UAT, full data |
| Prod | live tenants |

CI/CD: GitHub Actions → restore/build → test → EF migrate → containerize → deploy. Frontend: Vercel or container alongside API.

---

## 12. Cost-phased technology (now vs future)

Principle: **cost grows with revenue, not ahead of it.** Add infrastructure only when scale/feature demand justifies it. AI usage is the one perpetual cost — gate it with per-tenant quotas from day one.

| Layer | Now (Phase 1 — keep cheap) | Future (add when justified) |
|-------|----------------------------|------------------------------|
| Backend | .NET 10 — **free** | same |
| Frontend | Next.js — **free** | same |
| Database | SQL Server **Developer** (dev) + **Azure SQL serverless** (prod, auto-pause) | scale up Azure SQL tier; or self-host Standard only if needed |
| File storage | **Local disk** via `IFileStorage` — notes/images | **Blob / Cloudflare R2** (R2 = no egress fees) |
| Video | local disk (tiny pilot) | **Cloudflare R2 / Bunny.net** + CDN |
| Events | in-memory (MediatR) + Outbox table — **free** | message queue (Service Bus/RabbitMQ) only for heavy async / microservices |
| Cache | in-memory — **free** | **Redis** (multi-tenant, leaderboards) |
| Background jobs | Hangfire (SQL storage) — **free** | dedicated workers |
| AI / LLM | — | **pay-per-use** (tutor, summaries, RAG) — quota-controlled |
| Vector store | — | **SQL Server 2025 native vectors** first → dedicated vector DB at scale |
| Live class | — | Zoom (P2) → native stream + DRM (P3) |
| Payments | — | gateway % per transaction (P3) |

**Biggest cost levers to watch:**
1. **Video bandwidth/egress** → use R2 (zero egress), not app-server streaming.
2. **SQL Server licensing** → Azure SQL serverless or Developer/Express; avoid upfront Standard licenses.
3. **AI usage** → per-tenant quota + start RAG on SQL Server native vectors (no separate vector-DB bill).

---

*Prepared as the engineering companion to the client proposal. Finalize the SOW after Phase 1 priorities and Section 8 decisions are returned.*
