# CareBridge — Continuous Care & Recovery-Support Platform

> **Positioning:** "A continuous care and recovery-support platform working alongside your primary doctor." Not a telemedicine app, not an AI doctor, not a replacement for the primary consultant. Hyderabad, India pilot first.

New project package, kept temporarily inside the LMS repository until it moves to its own repo. Contents:

| File | What it is |
|---|---|
| [MVP_INDIA_PLAN.md](MVP_INDIA_PLAN.md) | **Start here.** India positioning, MVP loop, Telemedicine Practice Guidelines 2020 / DPDP Act design constraints, Hyderabad pilot experiment (2–5 doctors, 50–100 patients), UI-first build order. |
| [BRD_CareSupport_Platform.md](BRD_CareSupport_Platform.md) | Full feature catalog: 48 features across Patient (PAT-*), Supporting Doctor (DOC-*), Admin (ADM-*), Platform (PLT-*) modules, API surface, NFRs, workflows. The MVP builds the subset listed in the MVP plan. |
| [ARCHITECTURE.md](ARCHITECTURE.md) | .NET 8 + Angular 20 clean architecture (same stack as the LMS project), domain model/ERD, notification engine, attention queue, audit trail, security/authorization design. |
| [prototype/index.html](prototype/index.html) | **Phase 0 clickable UI prototype** — patient app (phone frame) + supporting-doctor dashboard, with sample Hyderabad data. Open the file in a browser, or view the published artifact. |

## The MVP loop

Patient visits their primary doctor → uploads the prescription/treatment plan → platform builds a day-to-day recovery plan → a verified supporting doctor (RMP) monitors remotely → patient logs doses, symptoms, BP/sugar/temperature → doctor reviews, guides, and when needed directs the patient **back to the primary consultant or emergency care (108/112)** → the system keeps a longitudinal recovery record.

## Next steps

1. Show the prototype to 2–3 doctors and a few target patients/families; iterate on the flows before writing backend code.
2. Phase 1 scaffold: `CareBridge.sln` (.NET 8, clean architecture) + `carebridge-angular` in a new repository.
3. Legal counsel review (TPG 2020, DPDP 2023) before any real patient data.
