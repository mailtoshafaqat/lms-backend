# White-Label AI LMS — Build Progress Tracker

**Purpose:** Module-wise development record. Single source of truth for what is **Done**, **In progress**, and **Remaining**.
**Approach:** Prototype-first, contract-driven, built **module by module** (see `03-Technical-Architecture-LMS.md` §3.4–3.5).

**Status legend:** ⬜ Not started · 🟡 In progress · ✅ Done · ⏸️ Blocked

**Per-module definition of done:** Design finalized → API contract defined → Backend API built → Frontend wired → Tested.

---

## Phase 1 — MVP (match the video)

| # | Module | Design | API contract | Backend | Frontend | Tested | Status |
|---|--------|:------:|:------------:|:-------:|:--------:|:------:|:------:|
| 0 | **Foundation** (modular-monolith scaffold, EF Core + SQL Server, tenant context, IModule, event bus, JWT/Swagger) | n/a | ✅ | ✅ | n/a | ⬜ | 🟡 |
| 1 | **Auth & accounts** (admin-managed signup, login/refresh/me, roles, JWT, password hashing, forced reset, **forgot/reset password**) | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 2 | **Dashboard + course hierarchy** (bundle→subject→unit→topic) | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |
| 3 | **Video lectures + notes** | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |
| 4 | **MCQ engine** (DPT + results + explanations) | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |
| 5 | **Flashcards** | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 6 | **My Grades + basic leaderboard** | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 7 | **Bundle enrollment** | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 8 | **Admin CMS for content** | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 8b | **Admin-managed students + email/SMTP settings** (Platform module) | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 9 | **Live classes (Zoom)** | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |

---

## Phase 2 — White-label + differentiation

| # | Module | Design | API contract | Backend | Frontend | Tested | Status |
|---|--------|:------:|:------------:|:-------:|:--------:|:------:|:------:|
| 9b | **SuperAdmin SaaS** (tenant registry, BYO flags, institute admin provisioning) | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 10 | **Multi-tenant branding** (display name, logo upload/URL, primary color, subdomain resolution, public + admin + SuperAdmin APIs) | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 11 | **Landing-page builder** (Hero/Features/Footer sections, admin editor, public render) | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 |
| 12 | **Quiz & flashcard builders** (edit, reorder, titles) | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |
| 13 | **Mistake diary** (auto-track wrong MCQs, resolve, re-test list) | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |
| 14 | **Ask Kitab (RAG)** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⏸️ |
| 15 | **Live class recordings** (Zoom recording → members-only lecture) | ✅ | ✅ | ✅ | ✅ | ⬜ | 🟡 |

---

## Phase 3 — Beat PIS

| # | Module | Design | API contract | Backend | Frontend | Tested | Status |
|---|--------|:------:|:------------:|:-------:|:--------:|:------:|:------:|
| 16 | **In-app teacher Q&A** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| 17 | **Full mock exams** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| 18 | **Mobile apps** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| 19 | **Payments + parent portal** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| 20 | **Institute analytics** | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |

---

## Change log

| Date | Module | Update |
|------|--------|--------|
| 2026-06-08 | — | Tracker created |
| 2026-06-08 | Foundation | .NET 10 modular-monolith scaffolded (`backend/`): Shared kernel (BaseEntity/TenantEntity, ITenantContext, IModule + ModuleRegistry, in-memory IEventBus, ICurrentUser, Result), JWT + Swagger host, builds clean |
| 2026-06-08 | Auth | Identity module built: User/RefreshToken, PBKDF2 hasher, JWT service, AuthService (register/login/refresh) + `/me`, AuthController, UserRegisteredEvent, initial EF migration (`identity` schema) |
| 2026-06-08 | Frontend | Next.js 15 prototype scaffolded (`frontend/`, MockPilot stack: React 19 + Tailwind + shadcn-style + lucide): landing, login, register, dashboard (mock data); builds clean |
| 2026-06-08 | Auth (wire) | Frontend wired to real API: `lib/api.ts` client + `lib/auth.ts` token storage; login/register call `/api/v1/auth` and store JWT; backend CORS for localhost:3000 |
| 2026-06-08 | Courses | Module 2 built: `Lms.Modules.Courses` (Bundle/Subject/Unit/Topic, `courses` schema, tenant filters), CourseService + read endpoints (`/bundles`, `/bundles/{id}`, `/subjects/{id}/units`, `/units/{id}/topics`, `/topics/recent`), dev seeder, EF migration; dashboard wired to live bundles + topics |
| 2026-06-08 | Storage | Shared `IFileStorage` abstraction + `LocalDiskFileStorage` (MVP) + options/DI — swap to Blob/R2 later without touching features |
| 2026-06-08 | Content | Module 3 built: `Lms.Modules.Content` (Lecture/Note, `content` schema, tenant filters), ContentService, endpoints (`/topics/{id}/content`, `/files/{key}` download, `/admin/files` upload via IFileStorage), seeder (sample video+notes per topic), EF migration; frontend topic page with video player + notes, dashboard topic cards link to it |
| 2026-06-08 | Assessments | Module 4 (MCQ engine) built: `Lms.Modules.Assessments` (Quiz/Question/Attempt, `assessments` schema, tenant filters), QuizService (server-side scoring, answer key never leaked to quiz GET), endpoints (`/topics/{id}/quiz`, `/quizzes/{id}`, `POST /quizzes/{id}/attempts`), publishes `QuizSubmittedEvent` (for future Grades/Leaderboard/Mistake-Diary), DPT seeder (3 MCQs/topic), EF migration; frontend quiz runner (`/quiz/[topicId]`) with answer/submit + results & explanations, linked from topic page |
| 2026-06-08 | Infra/DB | Switched dev DB to Docker SQL Server (`localhost,14330`); API auto-creates `WhiteLabelLms` + migrates all schemas. Fixed CourseSeeder: was only inserting bundles (set child→parent nav without adding to parent collections) so subjects/units/topics never seeded — now builds full graph via parent collections |
| 2026-06-08 | Progress | Module 6 (Grades + leaderboard) built: `Lms.Modules.Progress` (QuizResult, `progress` schema, tenant filters). Demonstrates hybrid module comms — async: subscribes to `QuizSubmittedEvent` (handler records grade); sync: leaderboard resolves names via new shared `IUserDirectory` (impl in Identity). Endpoints `/me/grades`, `/leaderboard` (best-per-quiz points, isMe flag), EF migration; dashboard My Grades + Leaderboard wired to live API (mock removed). E2E verified (submit→grade→leaderboard) |
| 2026-06-08 | Flashcards | Module 5 built: `Lms.Modules.Flashcards` (Deck/Card, `flashcards` schema, tenant filters), service + `GET /topics/{id}/flashcards`, seeder (4 cards/topic), EF migration; frontend flip-card review page (`/flashcards/[topicId]`) with prev/next, linked from topic page. E2E verified |
| 2026-06-08 | Enrollment | Module 7 built: `Lms.Modules.Enrollment` (Enrollment, `enrollment` schema, tenant filters, unique user+bundle). Sync cross-module via new `IBundleCatalog` contract in Courses (reads bundle title/price/validity) to compute expiry. Endpoints `POST /bundles/{id}/enroll`, `GET /me/enrollments`; EF migration; dashboard Enroll button + Enrolled badge w/ expiry. E2E verified (enroll, duplicate blocked, list) |
| 2026-06-08 | SuperAdmin SaaS | BYO multi-tenant SaaS layer: `platform.Tenants` registry with per-institute flags (`LiveClassesEnabled`, `ZoomMode=TenantManaged`, `PaymentMode=TenantManaged`, `AllowStudentSelfEnroll`, `AllowAdminCreateStudent`, `Status`, `Plan`). Shared `ITenantFeaturesProvider` + `IInstituteAdminProvisioner`. SuperAdmin APIs `GET/POST /superadmin/tenants`, `PUT .../flags`, `POST .../admins` `[Authorize(SuperAdmin)]`. Seeded `superadmin@platform.com`/`SuperAdmin123!` (system tenant) + demo tenant row. Login uses `IgnoreQueryFilters` (multi-tenant), blocks suspended institutes, returns `tenant` features in auth response. Flags enforced: self-enroll blocked when off, admin student create, live classes. Frontend `/superadmin` + `/superadmin/tenants/[id]`, dashboard respects flags, Platform nav for SuperAdmin |
| 2026-06-08 | LiveClasses | Module 9 (Zoom live classes) built: `Lms.Modules.LiveClasses` (`live` schema, tenant filters). `LiveClass` (bundle, schedule, duration, join/start url, meeting id, passcode, provider Manual/Zoom, cancel). `ZoomMeetingService` creates real meetings via per-tenant **Zoom Server-to-Server OAuth** (token + `users/me/meetings`), with a **manual join-link fallback** when Zoom isn't configured. Cross-module reads via new shared `IEnrollmentReader` (active bundle ids, impl in Enrollment) + `IBundleCatalog`; per-tenant Zoom creds in Platform `TenantSettings` via shared `ITenantZoomSettingsProvider`. Endpoints: student `GET /me/live-classes` (only enrolled courses, host start-url hidden, ended hidden), admin `GET/POST/DELETE /admin/live-classes` + `zoom-status` `[Authorize(Policy=Teacher)]`, settings `GET/PUT /admin/settings/zoom` (secret never returned). State (Upcoming/Live/Ended/Cancelled) computed from schedule. Frontend: dashboard live-classes section wired to API w/ Join button (mock removed), `/admin/live-classes` scheduler + list, `/admin/settings/zoom`, admin nav tabs. EF migrations (`AddZoomSettings`, `InitialLiveClasses`). E2E verified (manual create, no-link rejected 400, enrolled student sees it / unenrolled doesn't, start-url hidden from students, zoom-status flips on config, cancel) |
| 2026-06-08 | Auth/Platform | Switched to **admin-managed registration** (no public self-signup — `/auth/register` removed). New `Lms.Modules.Platform` (`platform` schema) holds per-tenant email/SMTP settings for white-label. Shared kernel: `IEmailSender`+`EmailMessage`, `ITenantEmailSettingsProvider`+`TenantEmailSettings`, `IEnrollmentWriter`+`EnrollmentSummary`. `SmtpEmailSender` (MailKit) sends via the tenant's SMTP, with a dev-outbox fallback (writes `dev-emails/*.html`) when unconfigured. Identity: `User.MustChangePassword`/`CreatedAt`, `AdminUserService` (create student → temp password → optional enroll via `IEnrollmentWriter` → emails credentials), `GET/POST /admin/students`, `POST /auth/change-password`; admin endpoints `[Authorize(Policy=InstituteAdmin)]`. Frontend: removed register page/CTAs, admin nav (Content/Students/Email settings), `/admin/students` (create + list, shows temp password + email status), `/admin/settings/email` (SMTP config, password never returned), `/account/password` forced-reset flow on first login. E2E verified (register 404, create+enroll+outbox email, student forced reset, non-admin 403, settings persist) |
| 2026-06-08 | Admin CMS | Module 8 built (no new schemas — write APIs over existing tables, all `[Authorize(Policy=Teacher)]` under `/api/v1/admin`): Courses admin (create/delete bundle/subject/unit/topic), Content admin (add/delete lecture & note), Assessments admin (get-or-create quiz, add/delete question, admin GET incl. answer key), Flashcards admin (get-or-create deck, add/delete card). Seeded dev admin `admin@demo.com`/`Admin123!` (InstituteAdmin). Frontend: role-gated Admin link, `/admin` content-tree manager (lazy expand bundle→subject→unit→topic, inline add/delete), `/admin/topics/[id]` editor (lectures/notes/MCQs/flashcards). E2E verified (build tree+content, student answer-key hidden, non-admin 403, cascade delete) |
| 2026-06-08 | Auth | **Forgot/reset password** (market-standard, all roles): `identity.PasswordResetTokens` (single-use, 1hr expiry), `POST /auth/forgot-password` (always `{ sent: true }` — no email enumeration), `POST /auth/reset-password`, tenant-scoped reset email via `IEmailSender.SendForTenantAsync` + dev-outbox. Frontend: `/forgot-password`, `/reset-password`, login link + `?tenant=` slug. E2E verified (outbox email, reset, login) |
| 2026-06-08 | Branding | Module 10 (MVP branding): `TenantSettings` fields (`DisplayName`, `LogoUrl`, `PrimaryColor`, `SupportEmail`). Public `GET /public/branding/{slug}` (no auth), institute admin `GET/PUT /admin/settings/branding`, SuperAdmin `GET/PUT /superadmin/tenants/{id}/branding`. Frontend: `lib/branding.ts` applies `--brand` CSS var, `BrandHeader` on login/dashboard, `/admin/settings/branding`, SuperAdmin tenant detail branding section. Demo seeded as "Demo Academy" `#0b3d91`. E2E verified |
| 2026-06-08 | Phase 2 polish | White-label finish: `CustomDomain` on tenants (SuperAdmin), `FaviconUrl` + branded email templates (`IBrandedEmailRenderer`), landing sections Testimonials/CoursesShowcase/Stats + admin add/remove. Module 12: quiz/flashcard update+reorder APIs + topic editor. Module 13: `progress.MistakeEntries`, `GET /me/mistakes`, `/mistakes` UI. Module 15: `RecordingUrl` on live class → members-only lecture (`MembersOnly` on `Lecture`). Module 14 (RAG) deferred |
| 2026-06-08 | Phase 2 | **Landing-page builder** (Module 11): `platform.LandingPages` + `PageSections` (Hero/Features/Footer, JSON content). Public `GET /public/landing/{slug}`, admin `GET/PUT /admin/settings/landing`. Frontend section registry on `/`, admin editor `/admin/settings/landing`. Demo seeded with hero + 3 feature cards + footer. **Subdomain resolution**: `ITenantResolver` + `TenantResolutionMiddleware` (`demo.localhost` → tenant), `TenantContext` uses JWT → subdomain → default; Next.js `middleware.ts` sets `lms.tenantSlug` cookie; CORS allows `*.localhost:3000`. **Logo upload**: `POST /admin/files?folder=branding` (image-only, 2 MB), admin branding UI file picker + preview, `resolveAssetUrl` for API paths. E2E verified (public landing API, builds clean) |
