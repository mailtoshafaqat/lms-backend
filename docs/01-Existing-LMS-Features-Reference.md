# Existing LMS Features — Reference Inventory

**Document version:** 1.0  
**Date:** June 2026  
**Purpose:** List features found in leading exam-prep LMS apps today (reference benchmark)  
**Status:** For client review — describes **what already exists in the market**, not what we have built yet  
**Reference:** Industry-standard exam-prep LMS pattern (e.g. MDCAT/ECAT coaching platforms)

---

## 1. About this document

This document captures **existing, proven LMS features** used by successful exam-prep academies. It is based on analysis of a production LMS used for **MDCAT, ECAT, NUMS, NET**, and similar entry-test preparation.

Use this list to:
- Show the client what a **complete baseline** looks like today  
- Agree which existing features are **must-have** for launch  
- Separate **market-standard features** from **new AI-powered additions** (see Document 02)

> **Note:** This is a **separate product** from MockPilot (developer mock API platform).

---

## 2. User roles (existing pattern)

| Role | Existing capability |
|------|---------------------|
| **Student** | Attend classes, access content, attempt tests, track grades |
| **Teacher** | Deliver live classes, answer doubts, content tied to subjects |
| **Institute / Admin** | Manage batches, enrolments, and content *(implied in white-label setups)* |
| **Support** | Technical help via chat/WhatsApp-style channel |

---

## 3. Course & content structure (existing)

```
Session / Bundle  →  Subject  →  Unit  →  Topic  →  Learning assets
```

| Feature | Description |
|---------|-------------|
| **Bundles / sessions** | e.g. Premium batch, early-prep session — student sees only enrolled bundles |
| **Subjects** | Biology, Chemistry, Physics, English, Logical Reasoning, Mathematics, etc. |
| **Units & topics** | Syllabus broken into teachable units and topics |
| **Province / board shortlists** | Revision lists per region (Punjab, Sindh, KPK, Balochistan, Federal) |
| **Prev / next navigation** | Move between topics without returning to menu |

---

## 4. Student dashboard (existing)

| Feature | Description |
|---------|-------------|
| **Live class widget** | Shows currently scheduled live session |
| **Explore courses** | Access enrolled bundles and subjects |
| **My grades** | Performance summary |
| **Leaderboard** | Rank among batch peers (per subject) |
| **Ask Book** | AI/textbook Q&A with references |
| **Ask Teacher** | Direct doubt channel to teachers |
| **Learning tools** | Quiz builder, flashcard builder, mistake diary |
| **Support** | Help desk / WhatsApp support |
| **Side menu** | Dashboard, live classes, courses, WhatsApp support links |

---

## 5. Live classes (existing)

| Feature | Description |
|---------|-------------|
| **Scheduled live sessions** | Class appears on dashboard when active |
| **One-tap join** | Simple join flow (often via Zoom or similar) |
| **Teacher screen + audio** | Student sees instructor presentation |
| **Pinch-to-zoom** | Zoom lecture area on mobile |
| **In-class Q&A** | Students submit questions during live session |
| **Automatic recording** | Live class saved and linked to topic |
| **Recording replay** | Same topic shows video after class ends |
| **Fullscreen on mobile** | Rotate device for full-screen lecture |

---

## 6. Per-topic learning pack (existing)

Each topic typically includes:

| Asset | Description |
|-------|-------------|
| **Video lecture** | Recorded live class or uploaded lesson |
| **Notes** | Teacher notes matching the lecture (revision) |
| **Flashcards** | Question → tap → answer (active recall) |
| **DPT / practice test** | Topic-level MCQ quiz |
| **Text explanations** | Per-question rationale after submission |
| **Video discussion** | Video walkthrough of quiz questions |

---

## 7. Assessment & testing (existing)

| Feature | Description |
|---------|-------------|
| **Difficulty levels** | Easy, Medium, Hard |
| **All questions mode** | Full topic MCQ set in one attempt |
| **Question navigator** | Jump to any question |
| **Flag for review** | Mark questions to revisit before submit |
| **Timed attempts** | Optional countdown (via quiz builder) |
| **Results screen** | Score, correct / incorrect / skipped |
| **Per-MCQ explanation** | Why the right answer is correct |
| **Video discussion** | Teacher explains each MCQ in video form |
| **Unit test** | Full test at end of unit |
| **PYQ test** | Past-year questions per unit |
| **Unit / PYQ discussions** | Full test reviewed in video |
| **Batch announcements** | Unit tests announced (e.g. WhatsApp channel) |
| **MDCAT-style volume** | Custom quizzes up to ~180 MCQs |

---

## 8. Ask Book — textbook Q&A (existing)

| Feature | Description |
|---------|-------------|
| **Subject + unit selection** | Scope question to syllabus area |
| **Book-grounded answers** | Answer with **page reference** (province-specific textbooks) |
| **Internet fallback** | If not in book, answer with online sources |
| **Save answers** | Bookmark responses for later revision |
| **Short + detailed answer** | Enough detail for exam prep |

---

## 9. Ask Teacher — human doubt resolution (existing)

| Feature | Description |
|---------|-------------|
| **Menu-driven flow** | Academic question vs counseling |
| **Subject routing** | Bio, Chemistry, Physics, English, Logical Reasoning, etc. |
| **Same-day response** | Teachers answer within defined SLA |
| **Text questions** | Type and send doubt |
| **Image / voice** | Attach photo or voice note (via messaging channel) |
| **Counseling** | Study plan, backlog, motivation — separate from subject doubts |
| **Queue** | First-come-first-served; student waits for turn |

---

## 10. My Grades — progress tracking (existing)

| Feature | Description |
|---------|-------------|
| **Subject overview** | Overall quiz performance per subject |
| **Drill-down** | Subject → Unit → Topic scores |
| **Weak area detection** | e.g. low % on a specific topic |
| **Attempt history** | All quizzes and tests logged |
| **Target framing** | Goal % linked to exam readiness (e.g. 95% target) |

---

## 11. Leaderboard (existing)

| Feature | Description |
|---------|-------------|
| **Per-subject ranking** | Position among enrolled students |
| **Batch competition** | Motivation to improve daily |
| **Self-improvement focus** | Compare with self and peers |

---

## 12. Learning tools — self-study builders (existing)

### Quiz builder
| Feature | Description |
|---------|-------------|
| Subject select | Choose one or more subjects |
| Unit / topic select | Multi-select units and topics |
| Difficulty | Easy / Medium / Hard |
| Question count | Custom count (up to exam-style max) |
| Time limit | e.g. 60 minutes for 100 MCQs |
| Generate & attempt | Custom quiz for revision |
| Results + explanations | Same as standard DPT flow |

### Flashcard builder
| Feature | Description |
|---------|-------------|
| Unit / topic select | Choose syllabus areas |
| Deck size | Adjust number of cards |
| Review mode | Flip card → verify answer |

### Mistake diary
| Feature | Description |
|---------|-------------|
| Wrong answer log | Aggregates mistakes from all attempts |
| Topic grouping | Errors grouped by unit/topic |
| Weekly review | Revisit mistakes before next attempt |
| Subject totals | Count of attempts and errors per subject |

---

## 13. Support (existing)

| Feature | Description |
|---------|-------------|
| **WhatsApp / chat support** | Technical and LMS issues |
| **Self-service flows** | Auto-resolve common problems |
| **Human agent** | Escalation when needed |
| **FIFO queue** | First-come-first-served responses |
| **Issue types** | Live class access, login, content, general |

---

## 14. White-label elements (existing in market)

These are standard when academies resell or brand their own platform:

| Feature | Description |
|---------|-------------|
| **Academy branding** | Logo, name, colours on student app |
| **Enrolled batches only** | Student sees only their purchased sessions |
| **Multi-subject batches** | One enrolment covers multiple subjects |
| **Teacher per subject** | Routed doubts and content by subject |

---

## 15. Feature summary checklist (existing)

### Dashboard & navigation
- [ ] Live class card  
- [ ] Explore courses / bundles  
- [ ] My grades  
- [ ] Leaderboard  
- [ ] Ask Book  
- [ ] Ask Teacher  
- [ ] Learning tools entry  
- [ ] Support  

### Live & recorded learning
- [ ] Live class join  
- [ ] In-class Q&A  
- [ ] Session recording  
- [ ] Video lecture per topic  
- [ ] Notes per topic  
- [ ] Flashcards per topic  

### Testing
- [ ] Topic practice test (DPT)  
- [ ] Difficulty levels  
- [ ] Question navigator & flag  
- [ ] Text explanations  
- [ ] Video discussion  
- [ ] Unit test  
- [ ] PYQ test  
- [ ] Unit / PYQ video discussions  

### Progress & practice
- [ ] My Grades drill-down  
- [ ] Leaderboard  
- [ ] Quiz builder  
- [ ] Flashcard builder  
- [ ] Mistake diary  

### Support & doubts
- [ ] Ask Book (textbook Q&A)  
- [ ] Ask Teacher (human)  
- [ ] Counseling channel  
- [ ] Technical support  

### Content structure
- [ ] Bundle → Subject → Unit → Topic  
- [ ] Province shortlists  
- [ ] Prev / next topic navigation  

---

## 16. What this document does NOT include

- New AI features we propose to add (see **Document 02**)  
- Development timeline or pricing  
- MockPilot product features  

---

**Next document:** `02-White-Label-AI-LMS-Proposal.md` — existing features **plus** new AI-based LMS capabilities for your white-label product.
