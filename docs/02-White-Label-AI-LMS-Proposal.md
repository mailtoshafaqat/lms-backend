# White-Label AI LMS — Client Proposal

**Document version:** 1.0  
**Date:** June 2026  
**Status:** For client review — no development started  
**Companion document:** `01-Existing-LMS-Features-Reference.md` (market baseline)  
**Product:** Standalone white-label LMS (separate from MockPilot)

---

## 1. Executive summary

We propose building a **white-label, AI-enhanced LMS** for exam-prep academies (MDCAT, ECAT, NUMS, NET, and similar programs).

The product has two layers:

| Layer | Description |
|-------|-------------|
| **Part A — Existing (proven) features** | Everything students and academies already expect from a modern coaching LMS (live class, courses, MCQs, grades, flashcards, etc.) |
| **Part B — New AI-based features** | Intelligent capabilities that go beyond today’s apps — smarter revision, automation, and institute analytics |

Each academy runs under **its own brand** (logo, colours, domain). Students see only the academy name — not the platform vendor.

**This document is for client sign-off before wireframes, SOW, or development.**

---

## 2. Product vision

| Item | Detail |
|------|--------|
| **Product type** | White-label LMS with AI |
| **Target market** | Medical & engineering entry-test coaching institutes |
| **End users** | Students, teachers, institute admins |
| **Operator** | Your company as platform super-admin |
| **Differentiator** | Full baseline LMS **plus** AI tutor, AI content tools, and predictive analytics |

---

## 3. User roles

| Role | Responsibilities |
|------|------------------|
| **Super Admin** | Platform operator — institutes, billing, global AI settings |
| **Institute Admin** | Branding, batches, teachers, content, reports |
| **Teacher** | Live classes, content, doubt answers, class analytics |
| **Student** | Learn, practise, track progress, use AI tools |
| **Parent** *(Phase 3)* | View ward progress |
| **Support Agent** | Technical and access issues |

---

## 4. Content structure

```
Institute (tenant)
 └── Bundle / Batch (e.g. "MDCAT Premium 2026")
      └── Subject
           └── Unit
                └── Topic
                     └── Assets: video, notes, flashcards, tests, AI tools
```

**White-label:** Each institute has isolated students, content, branding, and settings.

---

# PART A — Existing features (included in platform)

*These match proven LMS apps today. Full detail is in Document 01.*

## A1. Student dashboard

| # | Feature | Included |
|---|---------|----------|
| 1 | Live class — today’s session | ✅ |
| 2 | Explore enrolled courses / bundles | ✅ |
| 3 | My Grades — performance overview | ✅ |
| 4 | Leaderboard — batch ranking (per subject) | ✅ |
| 5 | Learning tools — quiz, flashcard, mistake diary | ✅ |
| 6 | Ask Book — textbook Q&A | ✅ *(enhanced with AI in Part B)* |
| 7 | Ask Teacher — human doubts | ✅ |
| 8 | Support — help desk / chat | ✅ |

## A2. Live & recorded learning

| # | Feature | Included |
|---|---------|----------|
| 9 | Join live class (integrated provider) | ✅ |
| 10 | In-class Q&A | ✅ |
| 11 | Auto-record live sessions | ✅ |
| 12 | Video lecture per topic | ✅ |
| 13 | Teacher notes per topic | ✅ |
| 14 | Flashcards per topic | ✅ |
| 15 | Fullscreen / mobile-friendly player | ✅ |

## A3. Assessments

| # | Feature | Included |
|---|---------|----------|
| 16 | Topic practice test (DPT) | ✅ |
| 17 | Easy / Medium / Hard difficulty | ✅ |
| 18 | Question navigator & flag for review | ✅ |
| 19 | Results + text explanations | ✅ |
| 20 | Video discussion per quiz | ✅ |
| 21 | Unit test at unit end | ✅ |
| 22 | PYQ (past paper) test per unit | ✅ |
| 23 | Unit / PYQ video discussions | ✅ |
| 24 | Custom quiz builder (subject, unit, topic, timer) | ✅ |
| 25 | Up to exam-style MCQ count (e.g. 180) | ✅ |

## A4. Progress & self-study

| # | Feature | Included |
|---|---------|----------|
| 26 | My Grades — subject → unit → topic drill-down | ✅ |
| 27 | Weak area highlighting | ✅ |
| 28 | Leaderboard per subject | ✅ |
| 29 | Flashcard builder (custom decks) | ✅ |
| 30 | Mistake diary — wrong MCQs by topic | ✅ |
| 31 | Province / board shortlists | ✅ |

## A5. Human support & doubts

| # | Feature | Included |
|---|---------|----------|
| 32 | Ask Teacher — subject-routed doubts | ✅ |
| 33 | Counseling channel (study strategy) | ✅ |
| 34 | Text / image / voice questions | ✅ |
| 35 | Technical support queue | ✅ |

## A6. Institute admin (white-label baseline)

| # | Feature | Included |
|---|---------|----------|
| 36 | Custom logo, colours, academy name | ✅ |
| 37 | Custom domain or subdomain | ✅ |
| 38 | Student & teacher management | ✅ |
| 39 | Batch / bundle enrolment | ✅ |
| 40 | Course CMS — subjects, units, topics | ✅ |
| 41 | Upload videos, notes, MCQs, flashcards | ✅ |
| 42 | Announcements to batch | ✅ |
| 43 | Basic usage & score reports | ✅ |

**Part A total: 43 baseline features** — market-standard LMS for exam prep.

---

# PART B — New AI-based features (our differentiation)

*These go beyond existing LMS apps and define the “AI LMS” product.*

## B1. AI tutor & Ask Book (enhanced)

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 44 | **AI tutor chat** | In-app conversational tutor (Urdu + English); explains concepts step-by-step | 2 |
| 45 | **RAG on institute books** | Answers grounded in uploaded textbooks with page + edition references | 2 |
| 46 | **Syllabus-aware scope** | AI only answers within enrolled subject/unit — no off-syllabus noise | 2 |
| 47 | **Source citations** | Book page, lecture link, or PYQ reference on every AI answer | 2 |
| 48 | **Save & revise AI answers** | Bookmark AI explanations into personal revision list | 2 |
| 49 | **“Explain like I’m 15” mode** | Simpler explanations for weak topics | 3 |
| 50 | **Voice input / output** | Ask by voice; listen to explanation (accessibility + mobile) | 3 |

## B2. AI content generation (for teachers & admins)

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 51 | **AI lecture summary** | Auto notes from recorded class video/audio | 2 |
| 52 | **AI flashcard generation** | Generate cards from notes or video transcript | 2 |
| 53 | **AI MCQ generation** | Draft MCQs from topic notes; teacher approves before publish | 2 |
| 54 | **AI PYQ tagging** | Tag past papers by topic and difficulty automatically | 3 |
| 55 | **AI bilingual content** | Urdu ↔ English field labels and explanations where needed | 3 |
| 56 | **Bulk import + AI cleanup** | Upload CSV MCQs; AI fixes formatting and validates distractors | 3 |

## B3. AI adaptive learning (student-facing)

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 57 | **Weak-topic recommender** | “Study these 3 topics today” based on grades + mistake diary | 2 |
| 58 | **Adaptive quiz** | Next quiz weights weak topics higher | 2 |
| 59 | **Spaced repetition scheduler** | Smart flashcard timing (Anki-style) | 3 |
| 60 | **Readiness score** | Predicted exam readiness % per subject | 3 |
| 61 | **Daily study plan** | AI-generated plan from backlog and exam date | 3 |
| 62 | **Mock exam AI debrief** | After full mock, AI summarizes strengths and gaps | 3 |

## B4. AI for teachers & institutes

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 63 | **Doubt triage** | AI suggests draft answer for teacher to approve before sending | 2 |
| 64 | **At-risk student alerts** | Flag students with dropping scores or low engagement | 2 |
| 65 | **Content gap analysis** | Which topics have no PYQ coverage or low quiz scores | 3 |
| 66 | **Teacher copilot** | Draft responses, rubrics, and parent update messages | 3 |
| 67 | **Batch performance AI report** | Weekly institute PDF: trends, weak units, recommendations | 3 |

## B5. AI platform & white-label extras

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 68 | **Per-institute AI quota** | Control AI usage by plan (fair use / billing) | 2 |
| 69 | **AI audit log** | What students asked; compliance and quality review | 2 |
| 70 | **Custom AI persona per institute** | Tone and language match academy brand | 3 |
| 71 | **Disable AI per batch** | Institutes can turn AI off for specific cohorts | 2 |

## B6. Platform extras (non-AI but new vs baseline)

| # | Feature | Description | Phase |
|---|---------|-------------|-------|
| 72 | **In-app chat** | Replace WhatsApp for doubts and support (long-term) | 2 |
| 73 | **Native mobile apps** | iOS & Android student app | 3 |
| 74 | **Push notifications** | Class, test, teacher reply alerts | 2 |
| 75 | **Full mock exam mode** | Timed 180-MCQ exam simulation | 2 |
| 76 | **Payments** | Stripe + local gateways (JazzCash, EasyPaisa) | 3 |
| 77 | **Parent portal** | Guardian view of progress | 3 |
| 78 | **Video DRM / watermark** | Protect recorded content | 3 |

**Part B total: 35 new capabilities** (44–78) — AI + platform differentiation.

---

## 5. Combined feature map

| Category | Existing (Part A) | New AI / platform (Part B) |
|----------|-------------------|----------------------------|
| Dashboard | 8 features | Daily AI study plan, readiness score |
| Live & content | 7 features | AI lecture summary, AI flashcard/MCQ gen |
| Assessments | 10 features | Adaptive quiz, mock exam AI debrief |
| Progress | 6 features | Weak-topic recommender, spaced repetition |
| Support | 4 features | AI doubt triage, in-app chat |
| Admin / white-label | 8 features | AI reports, quotas, at-risk alerts |
| **Subtotal** | **43** | **35** |
| **Grand total** | | **78 features** |

---

## 6. Phased delivery (recommended)

Practical build order: **match the video first (MVP)** → **white-label + differentiation** → **beat the competitor**.

| Phase | Goal | Scope |
|-------|------|-------|
| **Phase 0** | Sign-off | Client sign-off on this document |
| **Phase 1 — MVP (match the video)** | Feature parity | Dashboard + course hierarchy; video lectures + notes; MCQ engine (DPT + results + explanations); flashcards; My Grades + basic leaderboard; bundle enrollment; admin CMS for content; **live classes (Zoom)** |
| **Phase 2 — White-label + differentiation** | Stand out | Multi-tenant branding; quiz/flashcard builders; mistake diary; Ask Kitab (RAG); advanced live classes (native stream + recordings) |
| **Phase 3 — Beat PIS** | Win | In-app teacher Q&A (no WhatsApp); full mock exams; mobile apps; payments + parent portal; institute analytics |

*Exact timeline and cost in SOW after client marks priorities.*

---

## 7. White-label summary (for client)

| Capability | Detail |
|------------|--------|
| **Branding** | Logo, colours, favicon, email templates |
| **Domain** | `learn.youracademy.com` |
| **Data isolation** | Each institute separate |
| **Feature toggles** | Leaderboard, AI, PYQ per institute |
| **Plans** | Super admin can sell tiers to institutes |
| **No vendor branding** | Students see only academy name |

---

## 8. Decisions required from client

| # | Question | Options |
|---|----------|---------|
| 1 | Product / academy name for pilot? | |
| 2 | Target exam first? | MDCAT / ECAT / both |
| 3 | Phase 1 must-haves from Part A? | Mark in Document 01 checklist |
| 4 | Which Part B AI features are launch-critical? | e.g. AI tutor vs only RAG Ask Book |
| 5 | Languages? | English / Urdu / both |
| 6 | Live class provider? | Zoom (fast) vs built-in stream |
| 7 | Doubts channel at launch? | WhatsApp vs in-app chat |
| 8 | Payments at launch? | Yes / Phase 3 |
| 9 | Mobile at launch? | Web only / native apps later |
| 10 | Textbooks for AI RAG? | List editions per province |

---

## 9. Success criteria (MVP / Phase 1)

- [ ] One complete subject path: unit → topics → video + notes + DPT  
- [ ] Students attempt MCQs (DPT) and see results + explanations  
- [ ] Flashcards available per topic  
- [ ] My Grades + basic leaderboard show progress and weak topics  
- [ ] Bundle enrollment works end to end  
- [ ] Admin uploads content via CMS without developer help  
- [ ] Live class (Zoom) schedule + join works  

**Phase 2 success (white-label + differentiation):**
- [ ] One institute fully branded (multi-tenant, isolated data)  
- [ ] Teacher uses quiz/flashcard builders to publish content  
- [ ] Mistake diary captures wrong answers for review  
- [ ] Student asks Ask Kitab (RAG); cited answer scoped to syllabus returned  
- [ ] Advanced live class (native stream) + recording replay works  

---

## 10. Out of scope

- MockPilot (mock API product)  
- Content creation by vendor (unless separate contract)  
- Hardware / classroom equipment  
- Degree accreditation  

---

## 11. Next steps

1. Client reviews **Document 01** (existing) and **Document 02** (this proposal).  
2. Client marks Phase 1 priorities and answers Section 8.  
3. Vendor delivers wireframes for sign-off.  
4. SOW with fixed scope, timeline, and cost.  
5. Development starts **only after written approval**.

---

## 12. Document index

| File | Purpose |
|------|---------|
| `01-Existing-LMS-Features-Reference.md` | What exists in the market today (baseline) |
| `02-White-Label-AI-LMS-Proposal.md` | **This file** — baseline + new AI features for your product |

---

**Prepared for client review.**  
Please return marked priorities and Section 8 answers to proceed.
