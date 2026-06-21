# Role CRUD Functionality Audit

## Executive Summary

This audit verifies merged-role CRUD and workflow enforcement after consolidating to the required 10-role model.

What is now implemented:
- Canonical backend role model is aligned to merged roles only.
- Legacy role mappings were consolidated into merged role permission sets.
- Demo users and role implementation expectations were remapped to merged roles.
- A new in-app "Role Permission & CRUD Audit" page was added and is API-backed using existing access matrix + coverage APIs.
- Delegation metadata and manager hierarchy matching are implemented in access control checks.
- OPMS/IPMS submission workflow endpoints now enforce ownership + status-transition guardrails server-side.

Current verification level:
- Backend build: PASS.
- Frontend typecheck/lint: PASS.
- Full scope/delegation/manager-hierarchy scenario harness: PARTIAL (automation harness still pending).

## Current Merged Roles

1. Super Admin
2. Admin
3. Client Admin
4. Auditor General
5. PMS / Performance Manager
6. Internal Audit
7. Reviewer
8. Approver
9. Verifier
10. Submitter

## Legacy Role Mapping

- Municipal Manager -> PMS / Performance Manager
- KPI Approver -> Approver
- Head of Department -> Approver
- Deputy Head of Department -> Approver

No legacy role constants are retained in the active merged security model.

## CRUD Capability Matrix

Status legend: Pass, Partial, Missing, Security Risk

| Role | Scope | Module | Create | Read | View | Edit | Delete/Archive | Submit | Verify | Approve | Review | Audit | Report | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Super Admin | Institution, Department, Unit, Assigned | Cross-Module | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Pass |
| Admin | Institution | Cross-Module | Yes | Yes | Yes | Yes | Partial | No | No | No | No | Yes | Yes | Pass |
| Client Admin | Institution | Cross-Module | No | Yes | Yes | No | No | No | No | No | No | Yes | Yes | Pass |
| Auditor General | Institution (read-only) | Cross-Module | No | Yes | Yes | No | No | No | No | No | No | Yes | Yes | Pass |
| PMS / Performance Manager | Institution | Cross-Module | Partial | Yes | Yes | Partial | No | Yes | No | Yes | Partial | No | Yes | Partial |
| Internal Audit | Institution | Cross-Module | No | Yes | Yes | Partial | No | No | No | No | Yes | Yes | Yes | Partial |
| Reviewer | Institution/Scoped | Cross-Module | No | Yes | Yes | Partial | No | No | No | No | Yes | No | Yes | Partial |
| Approver | Department/Unit/Assigned | Cross-Module | Partial | Yes | Yes | Partial | No | No | No | Yes | No | No | Yes | Partial |
| Verifier | Department/Unit/Assigned | Cross-Module | No | Yes | Yes | Yes | No | No | Yes | No | No | No | Yes | Partial |
| Submitter | Department/Unit/Assigned | Cross-Module | No | Yes | Yes | Yes (scoped submissions/actuals) | No | Yes | No | No | No | No | Partial | Partial |

Notes:
- Matrix values align with seeded permission intent plus current API governance surfaces.
- The in-app Role Permission & CRUD Audit page computes status from API-backed role access and coverage data.

## Workflow Action Matrix

| Role | Submit | Verify | Approve | Review | Audit | Status |
|---|---|---|---|---|---|---|
| Super Admin | Yes | Yes | Yes | Yes | Yes | Pass |
| Admin | No | No | No | No | No | Pass |
| Client Admin | No | No | No | No | No | Pass |
| Auditor General | No | No | No | No | No | Pass |
| PMS / Performance Manager | Yes | No | Yes | Partial | No | Partial |
| Internal Audit | No | No | No | Yes | Yes | Partial |
| Reviewer | No | No | No | Yes | No | Partial |
| Approver | No | No | Yes | No | No | Partial |
| Verifier | No | Yes | No | No | No | Partial |
| Submitter | Yes | No | No | No | No | Partial |

## Scope Testing Results

Scenarios requested for proof coverage:

1. Submitter own targets: Partial
2. Submitter additional-assignee target: Partial
3. Verifier department scope: Partial
4. Verifier unit scope: Partial
5. Verifier specific target assignment: Partial
6. Approver department scope: Partial
7. Approver organisation scope: Partial
8. Reviewer organisation scope: Partial
9. Internal Audit organisation scope: Partial
10. Auditor General read-only: Pass
11. Admin setup permissions: Pass
12. Super Admin full permissions: Pass

Interpretation:
- Core permissions and route governance exist and are API-driven for the audit surfaces.
- Delegation windows and manager hierarchy ownership matching are implemented in authorization logic.
- Scenario execution is still marked Partial because end-to-end automated scenario evidence generation is not yet in place.

## Security Risks

1. Delegation rules are not fully modeled in current access evaluation, which can produce over-allow or over-deny outcomes in delegated scenarios.
2. Manager hierarchy traversal is not yet fully enforced in authorization decisions for complex ownership chains.
3. Some role capability proofs are inferred from permissions + coverage APIs rather than a complete end-to-end scenario executor for every transition path.

Updated assessment:
1. Delegation and manager hierarchy are now enforced for access matching, reducing prior over-allow risk.
2. Workflow transition validation now blocks out-of-order and self-approval/self-verification actions server-side.
3. Residual risk is primarily evidence quality (missing full automated scenario harness), not missing core policy checks.

## Missing Functionality

1. End-to-end automated scenario harness for all 12 scope/workflow tests with pass/fail evidence artifacts.
2. Comprehensive API-only validation for every UI action path marked as available.
3. Expanded manager hierarchy policy tests for deep chains and circular-reference safety cases.

## Recommended Fixes

1. Implement a scenario runner that executes all 12 required cases and records deterministic evidence.
2. Continue replacing any remaining visual-only behavior with API-backed mutation checks or mark as Missing until implemented.
3. Add integration tests for OPMS/IPMS workflow transition constraints (draft->submitted->verified->approved/reviewed->audited).
4. Re-run audit page + markdown update after scenario harness completion and reclassify Partial rows.
