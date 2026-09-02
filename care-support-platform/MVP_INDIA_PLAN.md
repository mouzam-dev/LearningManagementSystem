# CareBridge — India MVP Plan (Hyderabad Pilot)

> Supersedes the generic scope in the BRD where they conflict. The BRD remains the full feature catalog; this document defines **what we actually build and test first**, positioned for India.

## 1. Positioning

**Not** another telemedicine app, an "AI doctor," a replacement doctor, or an autonomous diagnosis system.

> **"A continuous care and recovery-support platform working alongside your primary doctor."**

The primary doctor diagnoses and prescribes. The platform helps the patient follow the plan, monitor recovery, and stay connected with a qualified **supporting doctor** (a Registered Medical Practitioner). The value is *continuous post-consultation care* — the attention a typical short consultation cannot provide.

**Best-fit segments (launch focus):** post-illness recovery, chronic-condition follow-up (diabetes, hypertension first — they map directly to BP/sugar tracking), elderly patients, and medication-adherence support.

## 2. MVP Loop (the whole product, v1)

1. Patient visits their existing primary doctor.
2. Patient uploads/shares the prescription and treatment plan.
3. Platform creates a day-to-day recovery plan (medication schedule + tracking plan).
4. A supporting doctor (RMP) monitors the patient remotely.
5. Patient reports symptoms, medication adherence, and relevant vitals (BP / sugar / temperature / weight / SpO₂).
6. Supporting doctor reviews progress and contacts the patient when needed.
7. Anything concerning or outside the supporting doctor's scope → patient is directed **back to the primary consultant or emergency care (108/102)**.
8. The system maintains a simple **longitudinal recovery record**.

**MVP feature subset from the BRD:** PAT-01…PAT-14, PAT-18; DOC-01…DOC-09, DOC-13; ADM-01…ADM-03, ADM-06; PLT-01, PLT-02, PLT-03. Deferred past pilot: payments automation (PLT-05 — bill pilot patients manually/UPI), PAT-16/17, DOC-10…DOC-12, ADM-04/05/07…10 beyond basics.

## 3. Regulatory Design Constraints (India)

Designed in from day 1, per the **Telemedicine Practice Guidelines, 2020** (Board of Governors, MCI — now under NMC) and the **Digital Personal Data Protection Act, 2023**:

| Requirement | Product implication |
|---|---|
| **RMP identification** | Doctor onboarding captures name, State Medical Council registration number, qualification; admin verifies before activation; the doctor's name + registration number are **visible to the patient** on every interaction (dashboard header, guidance notes, summaries). |
| **Patient identification** | Patient onboarding captures name, age, contact; identity confirmed at first supporting-doctor interaction. |
| **Consent** | Explicit, recorded consent at onboarding (data sharing with supporting doctor, mode of communication); patient-initiated continuation implies consent per TPG, but we record it explicitly anyway. Revocable. |
| **Professional judgment / appropriate evaluation** | Supporting doctor's tools are framed as monitoring + guidance; UI copy never presents output as diagnosis; free-text observations always attributed to the RMP. |
| **In-person escalation** | First-class "consult your primary doctor / seek in-person care" escalation flow with urgency levels and mandatory patient acknowledgement; emergency banner: **108 (ambulance) / 112**. |
| **No prescribing on-platform (MVP)** | Prescriptions are *recorded* from the primary consultant. Supporting doctor does not prescribe in MVP — this keeps us clearly in "care support," out of TPG prescribing-list complexity. Revisit post-pilot with counsel. |
| **Records** | TPG requires RMPs to maintain records — the longitudinal recovery record + audit trail (PLT-03) is that record. Retention configurable. |
| **DPDP Act 2023** | Consent notices in plain language (English + Telugu + Hindi for Hyderabad pilot), purpose limitation, data-principal rights (access/erasure workflows), breach-notification runbook. |

Optional, post-pilot: ABDM (Ayushman Bharat Digital Mission) integration — ABHA-linked health records would strengthen the longitudinal record and credibility. Not MVP.

## 4. Pilot Experiment (Hyderabad)

**Scale:** 2–5 doctors, 50–100 patients, 8–12 weeks of live usage.

**The experiment is NOT "do patients like the app."** Two hypotheses:

1. **Willingness to pay:** patients (or their families — elderly segment often pays via children) will pay a monthly fee for continuous medical follow-up. Test with real pricing from week 1 (e.g., ₹499–₹999/month, manual UPI collection; discounting is allowed, free is not — free users tell you nothing about H1).
2. **Doctor-perceived clinical value:** supporting doctors believe the service genuinely improves adherence and recovery, enough to stake their name on it and refer patients.

**Instrumentation (decided before launch):** paid-conversion and month-2 renewal rate; check-in completion ≥5 days/week; adherence %; doctor time per patient per week; escalation count + appropriateness (doctor-reviewed); NPS from both sides at weeks 4 and 8.

**Kill / pivot / scale criteria:** agree on thresholds before the pilot (e.g., scale if month-2 renewal ≥ 60% and ≥ 3 of 5 doctors would recommend; pivot pricing model if usage is strong but renewal < 30%).

## 5. Build Order (UI-first)

Per the initial requirement — **UI first**:

| Phase | Output |
|---|---|
| 0 (now) | **Clickable UI prototype** of patient app + doctor dashboard (no backend) — for doctor/patient interviews and pilot recruitment. |
| 1 (wk 1–2) | Repo scaffold (.NET 8 API + Angular 20), auth with RMP verification flow, consent capture. |
| 2 (wk 3–4) | Patient loop: care-plan intake, medication schedule + reminders, dose logging, daily check-in, symptoms, vitals (BP/sugar/temp/weight/SpO₂). |
| 3 (wk 5–6) | Doctor loop: roster, patient detail dashboard, attention queue, observations, guidance, follow-ups, escalation flow. |
| 4 (wk 7) | Messaging, notifications, admin basics (verification, assignment), audit trail. |
| 5 (wk 8) | Hardening, Telugu/Hindi strings for patient-facing screens, pilot onboarding content, deploy. |

## 6. Open Decisions (need founder input, not blocking Phase 0–1)

- Working name — "CareBridge" is a placeholder.
- Supporting doctors: employed/contracted by us vs. marketplace (pilot: contracted, fixed per-patient stipend keeps incentives clean).
- Pricing point and who pays (patient vs. family plan).
- Legal counsel review of TPG/DPDP posture before real patient data (required before pilot, not before prototype).
