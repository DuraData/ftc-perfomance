# OPMS/IPMS Target Field and Rule Audit

Date: 2026-06-21
Scope: Frontend-only audit and update for OPMS/IPMS target structure parity with extracted XAF target fields, dynamic unit behavior, validations, and audit visibility.

## Summary

This pass implemented a structural frontend uplift in target forms and type models, with focus on:
- XAF-aligned unit enum values in form selectors.
- Per-period dynamic unit selectors for quarterly/annual target values.
- Validation summary and blocking required-field checks before save.
- Expanded target type definitions for XAF fields, typed period values, child collection shapes, and audit metadata.
- Read-only audit metadata display in OPMS/IPMS form workspaces.

Backend APIs and persistence contracts were intentionally not changed in this pass.

## Expected vs Implemented Matrix

| Area | Expected | Implemented | Status |
|---|---|---|---|
| Target unit enum values | Exact XAF enum values available in frontend forms | Added XAF-style values in form options (`PercentageBased`, `AbsoluteCount`, etc.) and mapping to legacy API values on save | PASS |
| Backward compatibility | Existing snake_case unit values continue to load correctly | Added legacy-to-XAF and XAF-to-legacy mappers in target form page | PASS |
| Form validation framework | Required fields validated with visible summary before save | Added top-level validation summary cards and blocking save for OPMS/IPMS required fields | PASS |
| Withdrawal rule | Reason required when withdrawn | Added rule in OPMS validation (`isWithdrawn => reason required`) | PASS |
| Dynamic period unit behavior | Unit selector per quarter/midterm/annual with immediate UI refresh | Added Q1/Q2/Mid/Q3/Q4/Annual unit selectors and dynamic unit labels per card | PASS |
| Typed target value structure | Frontend model supports typed period values per unit type | Added `TypedTargetPeriodValues` in `types/index.ts` | PASS |
| XAF base target fields | Frontend model includes extracted base fields | Added `BaseKpiTargetXafFields` and merged into `OPMSTarget` / `IPMSTarget` | PASS |
| Child collection model support | Include UserSubmit, vote numbers, additional assignees in model | Added `UserSubmitChild`, `OpmsVoteNumberChild`, `AdditionalAssigneeChild` and optional properties on target types | PASS |
| Audit metadata model support | Include Created/Updated metadata in model | Added `TargetAuditMetaFields` and merged into target types | PASS |
| Audit metadata UI | Read-only audit fields visible in target form/detail workflow | Added `Audit Metadata` panel (Created/Updated by/on) for OPMS/IPMS forms | PASS |
| Child collections UI editor | Add/edit/remove UI for all child collections in form workspace | Implemented OPMS collection editors for Wards, Additional Assignees, and Vote Numbers with add/remove interactions; related targets surfaced with linkage warning and IPMS unlink action. UserSubmit editors still pending | PARTIAL |
| Full tab-by-tab XAF page parity | Match full legacy tab structures and all conditional visibility rules | Partially implemented; major fields/units/validation now present, but complete tab parity not finished | PARTIAL |
| Role-based target visibility propagation | Apply new rules to all list/search/dashboard/report/workflow views | Existing submitter access work retained; this pass did not complete a full cross-view target visibility sweep | PARTIAL |
| Inline field-level error rendering everywhere | Error indicators next to all missing fields | Added inline errors for key required OPMS/IPMS fields (period, department, indicator, target name, objective, KPI description, annual target, weight, unit of measure, withdrawal reason). Full all-field coverage still pending | PARTIAL |

## Files Updated

- `ClientApp/src/types/index.ts`
  - Added XAF field interfaces and typed period-value model.
  - Expanded `TargetUnitType` union to include legacy and XAF-style values.
  - Extended `OPMSTarget` and `IPMSTarget` with XAF, child collection, and audit metadata fields.

- `ClientApp/src/components/targets/TargetFormPages.tsx`
  - Added legacy/XAF unit mapping helpers.
  - Switched form unit dropdowns to XAF-style values.
  - Added save-time validation and top-of-form validation summary.
   - Added inline field-level error rendering for key required OPMS/IPMS fields.
  - Added per-period unit selectors (Q1/Q2/Mid/Q3/Q4/Annual).
  - Added dynamic unit labels in quarterly cards.
   - Added OPMS child collection editors for wards, additional assignees, and vote numbers.
   - Added IPMS related OPMS unlink action and explicit linked-target preview.
  - Added read-only audit metadata panels.

## Remaining Frontend Work

1. Implement full child collection editors:
   - UserSubmit entries
   - Optional row-level edit metadata for collection items (beyond add/remove)

2. Implement full field-level inline validation messaging:
   - Per-input error props
   - Conditional required rules by selected unit/type

3. Complete full XAF tab parity:
   - Remaining extracted fields and conditional visibility in final section ordering
   - Any tab-specific behavior not yet mapped into page sections

4. Cross-page rule propagation:
   - Verify and enforce visibility/rule parity in list/search/dashboard/reports/workflow components

5. Optional: normalize API contracts to native XAF enum values end-to-end to remove mapping layer.

## Notes

- Frontend save payload still maps XAF selection back to legacy unit values for compatibility with existing API DTO handling.
- No backend endpoint or data contract changes were introduced in this pass.
