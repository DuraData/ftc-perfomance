# OPMS V3.9 Repository Compliance Assessment

**Repository:** `ftc-perfomance`  
**Requirements baseline:** `OPMS Requirements V3.9.docx`  
**Assessment date:** 2026-10-01  
**Assessment type:** Static repository, schema, configuration, and automated-test review

## 1. Executive summary

The repository is **not compliant with the OPMS V3.9 baseline in its current form**. It contains a substantial working prototype: an integrated ASP.NET Core/EF Core/SQL Server backend, a React client, Identity-based authentication, permission records, OPMS/IPMS targets and submissions, workflow endpoints, audit-related tables, notifications, IDP functionality, and automated tests. Those assets are reusable. They do not, however, implement several architectural invariants that V3.9 treats as foundational.

The most consequential gaps are structural rather than cosmetic:

- there is no Municipality tenant root and no server-enforced MunicipalityId boundary across business tables;
- financial years and reporting periods are not modelled through the required municipality/financial-year hierarchy;
- OPMS period targets remain wide columns on the KPI, while V3.9 requires normalized period-specific rows with one authoritative `TargetValue` and a period-specific OPMS unit;
- submissions allow multiple competing actual representations, client-provided variance, free-text quarters, `draft` state, duplicate KPI/period submissions, mutable stage columns, and hard deletion;
- workflow is hard-coded rather than configuration-driven and lacks the canonical append-only action ledger, RFI model, stage-rating ledger, scoped reporting windows, and exception model;
- evidence files are stored under the publicly served web root, have unlimited request size, lack type/hash/scanning controls, and are physically deleted;
- users, employees, assignments, tenant scopes, and effective-dated organization history are not separated as required;
- API versioning, concurrency tokens, public identifiers, idempotency, standardized error handling, server-side pagination/filtering, background jobs, health monitoring, backup/recovery evidence, TID, and C88 are absent or not verifiable;
- production and default configuration files contain tracked plaintext secrets and passwords, while the browser stores bearer and refresh tokens in `localStorage`.

These are release-blocking issues for a V3.9 production claim. The repository should be treated as an earlier-domain prototype requiring a controlled schema and workflow migration, not as a nearly compliant system needing only incremental UI additions.

Positive findings are meaningful but narrower: authorization policies are enforced on many controllers; the repository has permission, scope, override, and effective-dated assignment concepts; fixed OPMS/IPMS workflow actions enforce some separation of duties; IDP has a real domain and controller surface; the backend builds; 16 compiled backend tests and 53 frontend tests passed when invoked correctly. The frontend also exposes the expected unit-type choices. These strengths reduce implementation effort but do not compensate for missing V3.9 invariants.

No overall compliance percentage is stated. V3.9 requirements are not equally weighted, and a percentage would obscure that tenant isolation, authoritative value modelling, workflow immutability, and secure evidence handling are gating controls.

## 2. Scope and assessment method

### 2.1 Scope

The review covered:

- the V3.9 Word requirements document, including the final V3.9 baseline and the authoritative physical schema in section 32;
- ASP.NET Core application composition and security configuration;
- EF Core entities, mappings, migrations, indexes, relationships, and seed behavior;
- OPMS, IPMS, IDP, access-control, workflow, authentication, evidence, audit, and reporting controllers/services;
- React routes, API mappings, target/submission forms, workflow queues, reports, HR and lookup screens;
- repository configuration, tracked secrets, tests, and build-quality signals.

The assessment did not alter application code or data. Runtime infrastructure, external identity providers, deployed database contents, network controls, backup jobs, malware scanners, mail infrastructure, and production observability were not available for direct inspection.

### 2.2 Evidence method

Each conclusion was based on one or more of:

1. direct comparison of the V3.9 business rules and physical schema with repository entities and mappings;
2. tracing controller requests through authorization, persistence, workflow, notification, and file paths;
3. searching for required cross-cutting mechanisms such as MunicipalityId, RowVersion, `/api/v1`, idempotency, health checks, rate limiting, background workers, TID, and C88;
4. reviewing frontend data sources and submission/target serialization;
5. executing build, type-check, lint, and test commands.

Absence from the reviewed repository is classified as **NOT IMPLEMENTED** when the requirement necessarily requires code/schema that is missing. Environment-dependent controls are **NOT VERIFIABLE** when source alone cannot establish their operational state.

### 2.3 Verification results

| Check | Result | Interpretation |
|---|---|---|
| Backend build | Passed with 0 errors and 0 warnings | The host compiles, but compilation is not requirements compliance. |
| Frontend unit tests | 53/53 passed | Useful component/mapping coverage exists. |
| Backend tests via `dotnet vstest` | 16/16 passed | The compiled test assembly runs successfully. |
| Backend tests via ordinary `dotnet test` | Build only; no tests reported | `tests/FTCERP.Tests/FTCERP.Tests.csproj:1-8` does not declare `IsTestProject`; the normal developer/CI command can produce a false-green result. |
| Frontend type-check | Failed | Status casing mismatches, a nonexistent `dueDate` property, missing Vitest globals, and unused variables prevent a clean static check. |
| Frontend lint | Failed with 8 errors and 4 warnings | The client is not at a clean quality gate. |
| CI workflow | None found | No repository-enforced build/test/security gate was identified. |

## 3. Classification definitions

| Classification | Meaning in this report |
|---|---|
| **COMPLIANT** | Repository evidence implements the material V3.9 rule without a known contradiction. |
| **PARTIALLY COMPLIANT** | A recognizable implementation exists, but required scope, invariants, security, configurability, or persistence are incomplete. |
| **NON-COMPLIANT** | Implemented behavior directly contradicts a V3.9 rule. |
| **NOT IMPLEMENTED** | No material implementation of the required capability was found. |
| **NOT VERIFIABLE** | Compliance depends on runtime or external evidence not present in the repository. |
| **NOT APPLICABLE** | The requirement does not apply to the reviewed repository scope. |

## 4. Architecture and technology assessment

### 4.1 Current architecture

The repository is a single ASP.NET Core web host targeting .NET 10, using EF Core 10, SQL Server, ASP.NET Core Identity, JWT bearer authentication, and Swagger (`FTCERP.Host.csproj:1-24`; `Program.cs:20-45`). It builds and serves a React 18/Vite/TypeScript SPA from `wwwroot` (`FTCERP.Host.csproj:8-10,39-47`; `Program.cs:129-146`). This is a coherent deployable shape for a modular monolith.

The implementation is organized into `API`, `Domain`, and `Infrastructure` folders, but domain behavior is frequently implemented directly in controllers and entities are persistence-oriented mutable records. Workflow, target semantics, evidence security, and tenancy are not isolated behind enforceable domain/application boundaries.

### 4.2 V3.9 architectural fitness

**Classification: PARTIALLY COMPLIANT**

The technology stack can support V3.9, but the current domain and persistence architecture does not. A modular monolith remains viable if tenant context, authoritative value parsing, workflow state transitions, file access, audit append operations, and reporting-window decisions become centralized services with database-backed invariants.

The API currently uses unversioned controller routes such as `api/opms-submissions` (`API/Controllers/OpmsSubmissionsController.cs:14`) rather than `/api/v1`. Swagger labels a document as v1, but that is not URL/API contract versioning (`Program.cs:22-38,118-120`). Controllers return entire collections and the common table filters/sorts/paginates in memory (`ClientApp/src/components/common/DataTable.tsx:59-85`), which conflicts with the V3.9 API baseline for server-side pagination, filtering, sorting, and search.

### 4.3 Deployment and operations

**Classification: NOT VERIFIABLE / NOT IMPLEMENTED**

No repository evidence was found for readiness/liveness endpoints, distributed correlation IDs, structured security-event logging, metrics, tracing, alert definitions, background-job infrastructure, backup scheduling, restore exercises, RPO/RTO controls, or disaster-recovery runbooks. HTTPS redirection exists (`Program.cs:124`), but JWT metadata validation is explicitly disabled (`Program.cs:69-82`). Actual perimeter TLS and platform controls are not verifiable.

## 5. Detailed requirements assessment

### 5.1 Multi-municipality tenancy and core masters

**Classification: NOT IMPLEMENTED**

V3.9 makes Municipality the tenant root and requires server-side tenant filtering on every read and write. `ApplicationDbContext` has no Municipality or MunicipalityFinancialYear DbSet, and core entities do not carry MunicipalityId (`Infrastructure/Persistence/ApplicationDbContext.cs:13-64`). Department codes are globally unique rather than tenant-scoped (`Infrastructure/Persistence/ApplicationDbContext.cs:94-106`). `Ward` stores Municipality as text rather than a foreign key (`Domain/Entities/LookupEntities.cs:3-120`).

`UserScope` can express institution/department/unit-like scopes, but has no MunicipalityId or effective dates (`Domain/Entities/UserScope.cs:3-30`). More critically, access evaluation permits a record when the user has a permission but no matching scope or assignment (`Infrastructure/Security/AccessControlService.cs:115-160`, especially `154-155`). This is the opposite of deny-by-default tenant isolation.

Required masters for municipality, financial year, municipality-financial-year activation, and canonical reporting periods are absent. The existing `Period` combines code, dates, and a fiscal-year string without the required hierarchy (`Domain/Entities/LookupEntities.cs:3-13`).

### 5.2 Organization, people, identity, and assignments

**Classification: PARTIALLY COMPLIANT**

Departments, units, positions, Identity users, roles, permissions, overrides, scopes, and target assignments exist. `UserAssignment` includes assignment type and effective start/end dates, so the repository has a useful seed for submitter, verifier, approver, auditor, delegation, and additional-assignee concepts (`Domain/Entities/UserAssignment.cs:3-30`). Permission and override evaluation is data-backed (`Infrastructure/Security/AccessControlService.cs:57-112`).

However, V3.9 separates login identity from MunicipalEmployee and effective-dated EmployeeAssignment. `ApplicationUser` directly contains current department, unit, position, and manager references (`Domain/Entities/ApplicationUser.cs:5-33`), so historical organization placement cannot be represented correctly. Assignments are not anchored to a tenant employee record, overlap prevention is not enforced at database level, and the current organization admin UI still relies on mock employee/department/position data (`ClientApp/src/components/hr/HRManagement.tsx:1-10,95-103`; mock lists are also used throughout that file).

### 5.3 Authentication, sessions, and authorization

**Classification: NON-COMPLIANT**

Positive controls include ASP.NET Core Identity password hashing, validated JWT issuer/audience/lifetime/signature, controller authorization policies, permission claims, user overrides, and some resource-level access checks (`Program.cs:47-89`; `Infrastructure/Security/AccessControlService.cs:57-160`).

Material V3.9 conflicts are present:

- the only implemented authentication mode is local Identity/JWT; Entra ID, Active Directory, and HYBRID modes were not found;
- privileged-user MFA is not implemented;
- browser access and refresh tokens are stored in `localStorage` (`ClientApp/src/api/api.ts:75-92`) rather than secure HttpOnly sessions/cookies;
- refresh tokens are stored in raw form and rotation does not revoke the prior token (`Infrastructure/Auth/JwtService.cs:64-74`; `API/Controllers/AuthController.cs:106-135`);
- login calls use `lockoutOnFailure: false`, and failed-login auditing is absent (`API/Controllers/AuthController.cs:42-80`);
- public registration creates an active, email-confirmed Submitter (`API/Controllers/AuthController.cs:82-104`);
- a public demo-user endpoint reveals configured demo credentials (`API/Controllers/AuthController.cs:154-158`);
- password minimum length is only six and non-alphanumeric characters are optional (`Program.cs:48-55`);
- no rate limiter, request throttling, or explicit account-lockout policy is configured;
- authorization contains a hard-coded SuperAdmin bypass (`Program.cs:170-191`; `Infrastructure/Security/AccessControlService.cs:57-63`).

The checked-in `appsettings.json` and `appsettings.Production.json` contain plaintext database credentials, a JWT signing secret, and administrative/demo passwords (`appsettings.json:10-25`; `appsettings.Production.json:10-25`). Both files are tracked. These secrets must be considered compromised and rotated; merely moving them later is insufficient.

### 5.4 KPI, strategic, budget, and organizational linkage

**Classification: PARTIALLY COMPLIANT**

OPMS and IPMS target entities include department, unit, assignee, supervisor, strategic goal/objective, budget source/type, unit of measure, weight, KPI type, indicator type, and related OPMS target concepts (`Domain/Entities/WorkflowGovernanceEntities.cs:41-177`). This supports a portion of V3.9 capture.

The model is not relationally complete. `WardIds`, `AdditionalAssigneeIds`, and `VoteNumberIds` are stored as delimited strings (`Domain/Entities/WorkflowGovernanceEntities.cs:50-52`) instead of junction tables. Several strategic classifications remain free text. Municipality and financial-year foreign keys are absent. Required delete restrictions and tenant-scoped uniqueness cannot be enforced consistently, while multiple relationships use cascade deletion in `ApplicationDbContext`.

### 5.5 Reporting-period target model and dynamic unit engine

**Classification: NON-COMPLIANT**

This is a core V3.9 mismatch. V3.9 requires normalized KPI-period rows with one authoritative `TargetValue`, period-specific `OPMSTargetUnit`, and UOM as display metadata. The repository stores Q1, Q2, Mid-Term, Q3, Q4, annual, budget, description, and revised values as wide columns on `OpmsTarget` and `IpmsTarget` (`Domain/Entities/WorkflowGovernanceEntities.cs:79-98,146-166`; `Infrastructure/Persistence/ApplicationDbContext.cs:204-259`).

The client displays the expected unit catalogue, including percentage, count, financial, area, volume, index, ratio, time, binary, date, readiness, qualitative, zero-based, and reverse variants (`ClientApp/src/components/targets/TargetFormPages.tsx:43-60`). It also renders period-specific unit selectors (`ClientApp/src/components/targets/TargetFormPages.tsx:1128-1137`). But payload construction sends only one KPI-level `targetUnitType` and converts every target value to JavaScript `Number` (`ClientApp/src/components/targets/TargetFormPages.tsx:640-684`). The selected Q1/Q2/Mid/Q3/Q4/annual unit values are therefore not persisted. Dates, qualitative values, ratios, binary outcomes, and readiness values cannot retain their authoritative textual/structured meaning.

No centralized parser/validator/calculator was found for the full unit catalogue. Ratios are not constrained to 2-4 non-negative segments or serialized canonically as colon-delimited values. Directionality, zero-target handling, reverse/cumulative semantics, date/time semantics, readiness-scale validation, and unit-specific variance logic are not enforced server-side. The submission form accepts a numeric Actual and even exposes a user-editable Variance field (`ClientApp/src/components/opms/OPMSSubmissions.tsx:300-329`).

### 5.6 Revision model

**Classification: NON-COMPLIANT**

V3.9 requires field-specific revision flags and deterministic original-versus-revised ordering. The repository uses a generic `IsRevised` plus a small subset of revised Q3/Q4/annual fields (`Domain/Entities/WorkflowGovernanceEntities.cs:76-98`). It does not provide revision flags and paired values for every governed target/budget field, revision effective date, approval reference, reason, or actor. Revisions are edited in place rather than represented as auditable approved changes. Externally approved revisions therefore cannot be reliably reconstructed.

### 5.7 Submission identity, state, and actual performance

**Classification: NON-COMPLIANT**

V3.9 permits one logical submission per KPI and reporting period, with base submission states separated from workflow-stage state. The current POST creates a new row on every request, and no unique KPI/period index exists (`API/Controllers/OpmsSubmissionsController.cs:80-122`; `Infrastructure/Persistence/ApplicationDbContext.cs:374-460`). `Quarter` is free text, not a ReportingPeriod foreign key (`Domain/Entities/WorkflowGovernanceEntities.cs:180-247`).

The entity keeps `Actual`, `ActualDescription`, and `ActualPerformanceDescription` as competing representations and persists a client-supplied `Variance` (`Domain/Entities/WorkflowGovernanceEntities.cs:180-247`; `API/Requests/LoginRequest.cs:238-251`). V3.9 requires one authoritative `ActualPerformance` interpreted by the period unit. The API also accepts client-controlled due dates and mutable variance/description data.

New submissions are initialized to `draft` (`API/Controllers/OpmsSubmissionsController.cs:80-122`), while V3.9 expressly rejects DRAFT as an OPMS base-submission state. The entity mixes submission status with mutable Submitter/Verifier/Approver/PMS/Auditor status columns (`Domain/Entities/WorkflowGovernanceEntities.cs:180-247`) instead of preserving base state and deriving workflow from append-only actions.

Submission PUT and DELETE endpoints permit mutable overwrite and physical deletion (`API/Controllers/OpmsSubmissionsController.cs:126-185`). This prevents reliable historical reconstruction and conflicts with the V3.9 no-hard-delete/audit baseline.

### 5.8 Workflow configuration, segregation of duties, and action history

**Classification: PARTIALLY COMPLIANT**

The fixed workflow has submit, verify, verification-reject, approve, reject, PMS review, audit, and score actions (`API/Controllers/OpmsSubmissionsController.cs:298-326`). Transition checks reject invalid states and prohibit several self-review combinations. Audit rows, review comments, findings, scores, due-date extensions, and notifications provide useful partial history.

The workflow is nevertheless hard-coded. There are no WorkflowDefinitions/WorkflowStages, tenant/year configuration, optional verifier stage, bypass rule, independent PMS/IA routing configuration, canonical `SubmissionWorkflowActions` ledger, RFI entity and response cycle, stage-specific rating scheme/value records, or policy-driven finalization. Status columns are overwritten. Comments/findings/scores/audit snapshots are fragmented across tables rather than forming the required ordered action history.

Audit persistence is also not transactionally coupled to every business mutation: `WorkflowGovernanceService` inserts audit/notification records through separate saves (`Infrastructure/Security/WorkflowGovernanceService.cs:23-79`). A failure can leave the business change and its audit trail inconsistent.

### 5.9 Reporting windows, deadlines, and exceptions

**Classification: NOT IMPLEMENTED**

The submission stores due date and extension values, and there is a DueDateExtension entity. V3.9 instead requires configured ReportingWindows by municipality/financial year/period, scoped exceptions, actor/reason/expiry, and enforcement for both normal users and administrators. No such model or centralized decision service exists. The current endpoint mutates submission due-date fields, and create/update requests can supply the due date directly (`API/Controllers/OpmsSubmissionsController.cs:361-420`; `API/Requests/LoginRequest.cs:238-251`).

### 5.10 Evidence files and POE assessments

**Classification: NON-COMPLIANT**

V3.9 separates immutable file metadata (`OPMS_Files`) from POE associations (`OPMS_POEs`) and requires protected object storage, SHA-256, controlled download, validation/scanning, and append-only assessment history.

The repository uses one `PoeFile` entity for both the physical file and its submission association, with a mutable storage path and without SHA-256, lifecycle state, source, RFI link, or assessment ledger (`Domain/Entities/WorkflowGovernanceEntities.cs:318-331`). Upload endpoints use `[RequestSizeLimit(long.MaxValue)]`, preserve arbitrary extensions, and write beneath `wwwroot/uploads`; deletes physically remove the file (`API/Controllers/OpmsSubmissionsController.cs:214-294`; equivalent behavior exists in `API/Controllers/IpmsSubmissionsController.cs:214-294`). `UseStaticFiles` exposes the web root (`Program.cs:129-130`), and URL mapping returns permanent public paths (`API/Controllers/PerformanceApiSupport.cs:13-17`).

No MIME/extension allowlist, content signature check, malware scan, maximum policy size, hash calculation, quarantine, private authorization-mediated download, retention rule, or immutable assessment action was found. This is a high-severity security and records-integrity gap.

### 5.11 Auditability and records retention

**Classification: PARTIALLY COMPLIANT**

The `AuditTrail` entity records entity, action, actor, timestamp, and JSON old/new values (`Domain/Entities/WorkflowGovernanceEntities.cs:348-361`). Workflow comments, findings, scores, and extensions add traceability.

The audit design lacks MunicipalityId, explicit field name, reason, session, correlation ID, source IP/device context, and a guaranteed append-only database policy. Whole-row JSON snapshots are less queryable than V3.9 field/action ledgers. Hard-delete endpoints exist for targets, submissions, and attachments, and cascade relationships can erase dependent history. No retention schedule, legal hold, immutable archive, or tamper-evidence implementation was found.

### 5.12 Notifications and escalation

**Classification: PARTIALLY COMPLIANT**

In-app notification records are created for selected workflow events (`Infrastructure/Security/WorkflowGovernanceService.cs:39-79`). There is no durable business-event/outbox model, background dispatcher, email/SMS integration, retry/dead-letter handling, idempotency key, escalation scheduler, reminder policy, or delivery audit. Synchronous row creation is not the V3.9 notification pipeline.

### 5.13 Reporting and dashboards

**Classification: NON-COMPLIANT**

The client contains dashboards and a report catalogue, but the main Reports screen uses hard-coded sample chart data and static overdue rows (`ClientApp/src/components/reports/Reports.tsx:10-35,50-99`). PDF and Excel buttons have no export handlers (`ClientApp/src/components/reports/Reports.tsx:101-107`). Several reports are only a generic preview.

The IDP report endpoint returns UTF-8 text bytes while labeling them as PDF, Excel, or Word (`API/Controllers/IdpController.cs:415-438`). That is not a valid export implementation. Server-side report filtering, asynchronous large exports, role/tenant scoping, export audit, and approved-data-only rules were not demonstrated.

### 5.14 API contract and integration baseline

**Classification: NON-COMPLIANT**

Swagger exists and bearer authentication is described (`Program.cs:20-38`), but the V3.9 contract baseline is missing:

- routes are not namespaced under `/api/v1`;
- entities expose internal string/integer IDs rather than stable PublicId values;
- business rows lack RowVersion/ETag concurrency;
- POST idempotency keys are not implemented;
- collection endpoints generally return complete datasets;
- standard filter/sort/search/page metadata is absent;
- validation is inconsistent and request DTOs lack comprehensive annotations;
- no centralized exception/problem-details mapping establishes the required 400/401/403/404/409/413/415 behavior;
- concurrent update conflicts cannot reliably return 409;
- upload content failures cannot reliably return 413/415 because upload size is unlimited and type checks are absent.

The permissive CORS policy accepts any origin, method, and header (`Program.cs:93-100,122`). That is unsuitable for the production trust boundary.

### 5.15 IDP independence and lineage

**Classification: PARTIALLY COMPLIANT**

IDP is materially implemented: plans, versions, strategic hierarchy, projects, KPIs, documents, alignments, dashboards, reports, and controller tests exist (`Domain/Entities/IdpEntities.cs:79-248`; `API/Controllers/IdpController.cs`). OPMS entities do not require an IDP foreign key, so IDP appears non-blocking, which aligns with V3.9.

The lineage baseline is incomplete. `IdpPlan` stores municipality name as text and integer start/end years, without MunicipalityId, PlanFamily, prior-plan lineage, effective dates, or RowVersion (`Domain/Entities/IdpEntities.cs:79-100`). Versions lack explicit predecessor lineage (`Domain/Entities/IdpEntities.cs:102-118`). The strategic hierarchy is fixed rather than configurable. IDP-to-SDBIP mapping junctions, import staging, validation/reconciliation, and governed publication controls were not found. Boolean `Circular88Linked` and `TreasuryTidLinked` fields on an IDP KPI do not implement either integration (`Domain/Entities/IdpEntities.cs:226-248`).

### 5.16 TID and C88

**Classification: NOT IMPLEMENTED**

No Treasury Indicator Definition domain, import/version process, validation engine, mapping tables, or non-blocking compliance result was found. No independent C88 dataset, calculation model, workflow, or publication lifecycle was found. A pair of link booleans is not sufficient. V3.9 makes these optional/independent, so their absence need not block core OPMS operation, but the repository must not claim those capabilities.

### 5.17 User interface completeness and accessibility

**Classification: PARTIALLY COMPLIANT**

The React application has broad navigation and screens for targets, submissions, workflow queues, access governance, IDP, dashboards, libraries, and settings. Target forms visibly cover many V3.9 concepts.

Several screens are prototype-only. Generic master pages take in-memory `mockData`, and their create/update/delete/export buttons do not persist (`ClientApp/src/components/admin/GenericLookupPages.tsx:20-28,53-64,106-120,140-177`). HR pages also use mock data. Reports are static as described above. Some workflow/UI modules import mock submissions or employee/status data. The frontend type-check and lint failures also mean deployable consistency is not established.

No systematic WCAG evidence, keyboard test suite, screen-reader audit, contrast audit, or localization strategy was found. Accessibility conformance is therefore **NOT VERIFIABLE**, even though semantic React controls are used in places.

### 5.18 Testing, quality gates, and documentation

**Classification: PARTIALLY COMPLIANT**

Automated tests cover API mapping, access-control helpers, target form utilities, evidence UI logic, submission calculations, queues, context, workflow governance, entity shape, and IDP controllers. The passing tests are useful regression assets.

Coverage omits the highest-risk V3.9 invariants: tenant isolation, permission/scope deny-by-default behavior, authentication abuse cases, refresh-token replay, configured workflow variants, duplicate submission prevention, optimistic concurrency, reporting-window enforcement, unit parsing/calculation across all unit types, file authorization and validation, append-only history, TID/C88 independence, and real report exports. The normal backend test project is not correctly identified to `dotnet test` (`tests/FTCERP.Tests/FTCERP.Tests.csproj:1-8`). There is no CI workflow, coverage threshold, migration verification, security scan, or deployment runbook. The client README remains starter-oriented rather than an operational product guide.

## 6. Consolidated compliance matrix

| ID | V3.9 requirement area | Status | Repository evidence | Required disposition |
|---|---|---|---|---|
| R-01 | Municipality tenant root | **NOT IMPLEMENTED** | No Municipality DbSet/entity in `ApplicationDbContext.cs:13-64` | Introduce tenant root and mandatory foreign keys. |
| R-02 | Server-side tenant isolation | **NON-COMPLIANT** | Scope fallback permits unscoped access in `AccessControlService.cs:154-155` | Deny by default; enforce tenant predicates and write validation. |
| R-03 | Municipality financial years | **NOT IMPLEMENTED** | Existing Period has a fiscal-year string in `LookupEntities.cs:3-13` | Add FinancialYear and MunicipalityFinancialYear masters. |
| R-04 | Canonical reporting periods | **PARTIALLY COMPLIANT** | Period and quarter labels exist; submission quarter is free text | Add canonical periods and FK every period-bound record. |
| R-05 | Employee separate from identity | **NON-COMPLIANT** | `ApplicationUser.cs:5-33` embeds organization placement | Add MunicipalEmployee and identity link. |
| R-06 | Effective-dated employee placement | **NOT IMPLEMENTED** | No EmployeeAssignment history | Add effective-dated, overlap-constrained assignments. |
| R-07 | Effective-dated KPI workflow assignments | **PARTIALLY COMPLIANT** | `UserAssignment.cs:3-30` has dates/types | Tenant-anchor it and enforce overlaps/role semantics. |
| R-08 | Dynamic permissions and overrides | **PARTIALLY COMPLIANT** | Permission/scope/override evaluation in `AccessControlService.cs:57-160` | Remove bypass/fallback risks and tenant-scope all decisions. |
| R-09 | Configurable local/Entra/AD/hybrid auth | **NOT IMPLEMENTED** | Local Identity/JWT only | Add configured providers and account-link rules. |
| R-10 | MFA for privileged users | **NOT IMPLEMENTED** | No MFA flow/configuration | Enforce MFA and recovery/audit controls. |
| R-11 | Secure session/token storage | **NON-COMPLIANT** | Tokens in localStorage, `api.ts:75-92` | Use secure HttpOnly cookies/session or equivalent BFF. |
| R-12 | Lockout/rate limiting/failed-login audit | **NON-COMPLIANT** | Lockout disabled; no rate limiter; only success logged | Implement abuse controls and complete auth-event audit. |
| R-13 | Secrets management | **NON-COMPLIANT** | Tracked secrets in both appsettings files | Rotate immediately; use secret provider and scanning. |
| R-14 | Tenant-scoped organization masters | **NON-COMPLIANT** | Globally unique departments in `ApplicationDbContext.cs:94-106` | Add tenant composite uniqueness/effective dates. |
| R-15 | Relational KPI mappings | **NON-COMPLIANT** | CSV ID fields in `WorkflowGovernanceEntities.cs:50-52` | Migrate to junction tables. |
| R-16 | Normalized KPI-period target row | **NON-COMPLIANT** | Wide Q1-Q4/Mid/Annual columns | Create period-target table and migrate wide values. |
| R-17 | One authoritative TargetValue | **NON-COMPLIANT** | Numeric wide targets and descriptions coexist | Store one authoritative string/value per period. |
| R-18 | Period-specific OPMS unit | **NON-COMPLIANT** | UI selectors not included in payload; one KPI unit | Persist unit per period row. |
| R-19 | UOM display-only distinction | **PARTIALLY COMPLIANT** | Separate UOM and target-unit fields exist | Enforce semantic separation in domain and API. |
| R-20 | Full unit validation/calculation engine | **NOT IMPLEMENTED** | Catalogue exists only in UI; no server engine | Build centralized parser, validator, comparator, formatter. |
| R-21 | Ratio 2-4 segment rule | **NOT IMPLEMENTED** | No canonical ratio parser/storage | Add transient segment input and colon serialization. |
| R-22 | Field-specific revisions | **NON-COMPLIANT** | Generic IsRevised and partial revised columns | Add explicit flags, metadata, and deterministic selection. |
| R-23 | One logical KPI-period submission | **NON-COMPLIANT** | Unconditional inserts; no unique index | Add composite unique key and idempotent create. |
| R-24 | Base submission state model | **NON-COMPLIANT** | New rows use `draft`; fixed stage status columns | Use IN_PROGRESS/SUBMITTED and separate workflow state. |
| R-25 | One authoritative ActualPerformance | **NON-COMPLIANT** | Multiple actual fields in entity/request | Consolidate and parse according to period unit. |
| R-26 | Server-calculated variance/status | **NON-COMPLIANT** | Client enters variance in `OPMSSubmissions.tsx:315-329` | Ignore client calculations; compute centrally. |
| R-27 | Configurable workflow definitions/stages | **NOT IMPLEMENTED** | Fixed action endpoints and transition code | Add tenant/year workflow configuration. |
| R-28 | Optional verifier/bypass | **NOT IMPLEMENTED** | Verifier is fixed in chain | Add policy-driven optional/bypass routing. |
| R-29 | PMS and IA independence | **PARTIALLY COMPLIANT** | Separate review/audit actions exist | Model independent assignments/stages and completion. |
| R-30 | Segregation of duties | **PARTIALLY COMPLIANT** | Several self-review checks exist | Make rules data-backed and test all paths. |
| R-31 | Append-only workflow action ledger | **NOT IMPLEMENTED** | Mutable status columns plus fragmented tables | Add canonical immutable action rows. |
| R-32 | RFI lifecycle | **NOT IMPLEMENTED** | No RFI entity/workflow | Add issue, response, attachments, closure history. |
| R-33 | Rating schemes and stage ratings | **PARTIALLY COMPLIANT** | Tenant rating schemes/values can be assigned to stages; required values are server-validated and persisted in an append-only stage-rating ledger | Reconcile legacy `SubmissionScore` rows and derive achievement-based ratings where configured. |
| R-34 | Reporting windows and scoped exceptions | **NOT IMPLEMENTED** | Client-controlled due dates/extensions | Add configured windows and exception decision service. |
| R-35 | Protected file metadata plus POE association | **NON-COMPLIANT** | Single `PoeFile`, `WorkflowGovernanceEntities.cs:318-331` | Split OPMS_Files and OPMS_POEs. |
| R-36 | File hash, validation, scan, private download | **NON-COMPLIANT** | Unlimited public-web-root upload and hard delete | Move to private store; validate, hash, scan, authorize. |
| R-37 | Append-only POE assessments | **NOT IMPLEMENTED** | No assessment ledger | Add assessment actions linked to POE/stage/actor. |
| R-38 | Immutable, queryable audit | **PARTIALLY COMPLIANT** | AuditTrail has actor/time/JSON snapshot | Add tenant/field/reason/session/correlation and immutability. |
| R-39 | No hard delete of governed records | **NON-COMPLIANT** | Target/submission/attachment DELETE endpoints | Replace with lifecycle state/soft withdrawal and retention. |
| R-40 | Durable notification pipeline | **PARTIALLY COMPLIANT** | Transactional outbox, idempotent in-app/email fan-out, retry/backoff, provider receipt ledger, readiness checks, dead-letter query, and audited replay are implemented | Add SMS adapter and policy-driven reminders/escalations; provision and rehearse the production email provider. |
| R-41 | Real dashboards and reports | **NON-COMPLIANT** | Static data in `Reports.tsx:30-107` | Implement governed queries and real exports. |
| R-42 | API `/api/v1` baseline | **NON-COMPLIANT** | Route is `api/opms-submissions` | Introduce versioned routes/contracts. |
| R-43 | PublicId and optimistic concurrency | **NOT IMPLEMENTED** | No PublicId/RowVersion on business entities | Add stable external IDs and RowVersion/ETag. |
| R-44 | Standard errors and validation | **PARTIALLY COMPLIANT** | Ad hoc ApiResponse exists | Centralize ProblemDetails and required status mapping. |
| R-45 | Pagination/filter/sort/search | **NON-COMPLIANT** | Collections loaded whole; client slices in `DataTable.tsx:59-85` | Implement server query contracts and bounds. |
| R-46 | POST idempotency | **NOT IMPLEMENTED** | No key or request-result ledger | Add endpoint idempotency for mutation/import/export jobs. |
| R-47 | IDP non-blocking independence | **COMPLIANT** | OPMS does not require an IDP FK | Preserve this separation during migration. |
| R-48 | IDP plan/version lineage | **PARTIALLY COMPLIANT** | Plan/version domain exists, lineage fields do not | Add PlanFamily, predecessor/effective/publication metadata. |
| R-49 | IDP import/reconciliation | **NOT IMPLEMENTED** | No staging/validation/reconciliation pipeline | Add optional governed import workflow. |
| R-50 | TID independent integration | **NOT IMPLEMENTED** | Only a boolean link indicator | Add versioned definitions/mappings/results if in scope. |
| R-51 | C88 independent subsystem | **NOT IMPLEMENTED** | Only a boolean link indicator | Implement separately if in scope; do not block OPMS. |
| R-52 | Health, monitoring, correlation | **NOT IMPLEMENTED** | No health checks/correlation/metrics code | Add operational telemetry and alerts. |
| R-53 | Backup, restore, DR | **NOT VERIFIABLE** | No infrastructure/runbook evidence | Define RPO/RTO and prove restore exercises. |
| R-54 | Automated quality gate | **PARTIALLY COMPLIANT** | Tests exist, but typecheck/lint fail and no CI | Fix project discovery and enforce build/test/security gates. |
| R-55 | Accessibility | **NOT VERIFIABLE** | No accessibility audit/test evidence | Establish WCAG target and automated/manual verification. |

## 7. Priority gap register

| Priority | Gap | Impact | Key evidence | Recommended action |
|---|---|---|---|---|
| **P0** | Tracked production/default secrets | Credential compromise, unauthorized DB/API/admin access | `appsettings.json:10-25`; `appsettings.Production.json:10-25` | Rotate every exposed credential/signing secret immediately, purge where policy permits, move to a managed secret store, add secret scanning. |
| **P0** | No tenant root or enforced isolation | Cross-municipality disclosure and modification | `ApplicationDbContext.cs:13-64`; `AccessControlService.cs:154-155` | Establish tenant context and mandatory MunicipalityId; deny unscoped requests; add isolation tests before onboarding multiple municipalities. |
| **P0** | Public, unlimited, unvalidated evidence uploads | Malware hosting, data exposure, storage exhaustion, records loss | `OpmsSubmissionsController.cs:214-294`; `Program.cs:129-130` | Disable current public path; move files private; size/type/signature/hash/scan controls; authorized streaming download; immutable lifecycle. |
| **P0** | Browser tokens and weak auth abuse controls | Token theft/replay and account compromise | `api.ts:75-92`; `AuthController.cs:42-80,106-158` | Move to secure session architecture, rotate/revoke refresh tokens, enable lockout/rate limits/MFA, remove public registration/demo credentials. |
| **P0** | Authoritative target/actual model contradicts V3.9 | Incorrect KPI interpretation, variance, audit and reporting | `WorkflowGovernanceEntities.cs:79-98,180-247`; `TargetFormPages.tsx:640-684` | Design normalized period target/submission schema and centralized unit engine before adding more features. |
| **P0** | Governed records can be overwritten/deleted | Loss of statutory/audit evidence | `OpmsSubmissionsController.cs:126-185,265-294` | Replace mutation/deletion with append-only actions, versioning, withdrawal and retention rules. |
| **P1** | Hard-coded mutable workflow | Cannot support municipal policy variants or reconstruct decisions | `OpmsSubmissionsController.cs:298-420` | Implement workflow definitions/stages/actions/RFIs/ratings and transactionally append actions. |
| **P1** | Duplicate/free-text submissions and no concurrency | Duplicate quarter data, lost updates, nondeterministic reports | `OpmsSubmissionsController.cs:80-159`; `ApplicationDbContext.cs:374-460` | Add canonical period FK, unique keys, PublicId, RowVersion/ETag and idempotency. |
| **P1** | Reporting windows absent | Unauthorized late/early capture and inconsistent extensions | Due dates accepted in `LoginRequest.cs:238-251` | Add scoped windows/exceptions and one server decision service. |
| **P1** | API contract baseline absent | Unsafe integrations and poor scalability | Unversioned routes; `DataTable.tsx:59-85` | Version API, paginate server-side, standardize errors/validation and mutation semantics. |
| **P1** | Reporting UI/export is synthetic | Decision-makers may rely on fabricated/incomplete output | `Reports.tsx:30-107`; `IdpController.cs:415-438` | Remove sample output from production paths; implement governed, scoped queries and valid file formats. |
| **P2** | Identity/person/history conflated | Incorrect accountability after transfers/delegations | `ApplicationUser.cs:5-33` | Separate employee, login identity, placement and workflow assignment histories. |
| **P2** | IDP lineage and imports incomplete | Weak traceability from plans to KPIs | `IdpEntities.cs:79-118,226-248` | Add plan-family/version lineage and optional staging/reconciliation. |
| **P2** | No durable notification/outbox | Lost or duplicated reminders/escalations | `WorkflowGovernanceService.cs:39-79` | Add transactional outbox, worker, retries, idempotency and delivery audit. |
| **P2** | Quality gates are unreliable | Regressions can merge/deploy undetected | `FTCERP.Tests.csproj:1-8`; frontend failures; no `.github` workflow | Correct test discovery, fix type/lint errors, add CI and coverage/security/migration gates. |
| **P3** | TID/C88 not implemented | Optional compliance/integration capability unavailable | No corresponding domain or controllers | Plan as independent modules after the core OPMS baseline is stable. |

## 8. Migration impact assessment

### 8.1 Data migration complexity

**Impact: High**

The migration is not a simple additive EF migration. Existing wide KPI records must be decomposed into period rows; current target and actual fields must be reconciled into authoritative values; free-text quarters must be mapped to canonical periods; generic/revised fields must be converted to governed revision records; delimited identifiers must become junction rows; and duplicate submissions must be resolved before a unique constraint can be enabled.

Every existing record will need a Municipality and financial-year assignment. Because those values are absent, automated inference will be incomplete. A controlled staging process with exception reports and business-owner sign-off is required.

### 8.2 Compatibility risks

- Existing frontend payloads assume numeric target/actual values and wide quarterly fields. A compatibility adapter or coordinated client release is required.
- Existing IDs are used as API identifiers. Introducing PublicId should preserve internal keys while clients transition.
- Workflow status strings and fixed stage columns cannot be mapped blindly to an append-only ledger; a migration rulebook must define synthetic historical actions and confidence levels.
- Existing file URLs may be externally referenced. Moving to private storage requires a protected retrieval bridge and a one-time hash/metadata backfill.
- Removing `draft` must distinguish incomplete data entry from a submitted workflow action.
- Correcting tenant isolation will intentionally reduce what some currently over-broad users can see.

### 8.3 Recommended migration pattern

1. Freeze the V3.9 logical and physical target model before further feature work.
2. Introduce new tables alongside legacy tables; do not destructively reshape production data in one release.
3. Build repeatable, idempotent backfill jobs with reconciliation counts and exception tables.
4. Dual-read in verification environments, comparing old and new KPI/report outputs.
5. Cut writes to the new authoritative model behind a controlled feature flag.
6. Retain legacy rows read-only for audit until retention and legal requirements permit archival.
7. Remove compatibility fields only after signed data reconciliation and rollback rehearsal.

## 9. Remediation roadmap

### Phase 0 — Containment and baseline (immediate)

- Rotate exposed database, JWT, admin, and demo credentials; remove public credential disclosure and self-registration.
- Disable or strictly constrain current evidence uploads and public static access.
- Replace allow-all CORS with environment-specific origins.
- Establish an architecture decision record for tenant context, normalized target values, unit engine, workflow ledger, and private file storage.
- Fix backend test discovery and make build, tests, type-check, lint, dependency audit, and secret scanning mandatory.

**Exit criteria:** no known repository secret remains valid; unsafe upload/public-demo paths are disabled; a repeatable clean quality gate exists.

### Phase 1 — Tenant, identity, and authoritative masters

- Add Municipality, FinancialYear, MunicipalityFinancialYear, ReportingPeriod, and tenant-scoped organization/master tables.
- Add server-resolved tenant context and mandatory read/write enforcement.
- Separate ApplicationUser, MunicipalEmployee, EmployeeAssignment, and effective-dated workflow assignments.
- Introduce PublicId and RowVersion conventions; change delete rules to restrict governed history.

**Exit criteria:** automated tests prove two tenants cannot read, infer, update, upload to, or report on each other's records; concurrent writes return deterministic conflicts.

### Phase 2 — KPI-period values, revisions, and submissions

- Create normalized KPI-period targets with one `TargetValue`, one period unit, budget, description, and revision metadata.
- Implement the centralized server-side unit parser/validator/comparator/formatter, including ratio and reverse-unit rules.
- Create one logical submission per KPI/period with authoritative `ActualPerformance`, narrative/corrective fields, base state, unique key, and idempotent creation.
- Migrate/reconcile wide targets, free-text quarters, duplicate submissions, and competing actual fields.

**Exit criteria:** every supported unit has positive/negative/boundary tests, and all variance/status outputs are calculated server-side from reconciled authoritative values.

### Phase 3 — Workflow, windows, audit, and files

- Add configurable workflow definitions/stages, optional verifier and bypass, independent PMS/IA assignments, action ledger, RFIs, rating schemes/values, and stage ratings.
- Add reporting windows and scoped exceptions with centralized enforcement.
- Split physical file metadata from POE associations; add SHA-256, private storage, scan/quarantine, authorized download, retention, and append-only assessments.
- Make business change, workflow action, audit, and outbox event one transaction.

**Exit criteria:** complete histories are reconstructable without mutable status columns; no governed record or file is hard-deleted through normal APIs.

### Phase 4 — API, reports, operations, and IDP completion

- Publish `/api/v1` contracts with paging/filtering/sorting/search, ProblemDetails, validation, idempotency, PublicId, and ETag/RowVersion behavior.
- Replace mock/static UI paths with tenant-scoped services and real export jobs.
- Add notification workers, retries, escalation, delivery audit, health endpoints, correlation, metrics, tracing, alerts, backups, restore tests, and runbooks.
- Complete IDP lineage, SDBIP mapping, and optional import/reconciliation without creating an OPMS dependency.

**Exit criteria:** end-to-end tests cover browser/API/database/file/event paths; operational readiness and restore evidence are approved.

### Phase 5 — Optional independent modules

- Implement TID definitions, versioning, mappings, validation, and compliance results if required.
- Implement C88 as a separate dataset/workflow/publication module if required.

**Exit criteria:** each module can be enabled or disabled without changing core OPMS submission eligibility or workflow.

## 10. Final assessment

The repository demonstrates useful product intent and several reusable foundations, but it **cannot be certified or represented as OPMS V3.9 compliant**. The decisive reason is not the number of incomplete screens; it is that the current persistence and security model contradicts V3.9's core invariants for tenancy, period-specific authoritative values, workflow immutability, evidence protection, revision traceability, and API concurrency.

The safest path is a staged modernization that preserves the current application as a migration source while introducing a new V3.9-aligned schema and domain layer alongside it. Security containment and tenant isolation should precede feature expansion. The normalized period/value model and unit engine should precede workflow and reporting work, because every downstream calculation, approval, audit entry, and export depends on them.

Production acceptance should require, at minimum, demonstrable tenant isolation, rotated secrets, secure sessions, private validated file storage, normalized authoritative target/actual data, unique and concurrent-safe submissions, configurable append-only workflow, enforced reporting windows, real scoped reporting, reliable automated quality gates, and operational recovery evidence. Until those conditions are met, deployment should be limited to controlled non-production evaluation using synthetic data.

## 11. Implementation progress after the initial assessment

### 11.1 V3.9 security containment and dynamic-security foundation

**Implementation date:** 2026-10-01  
**Overall updated status:** **PARTIALLY COMPLIANT**

The first modernization increment introduces a database-backed, XAF-style security foundation while preserving the original assessment findings that remain unresolved. It does not yet justify a complete V3.9 security-compliance claim.

| Requirement | Previous status | New status | Implementation and evidence | Migration and tests | Remaining issue |
|---|---|---|---|---|---|
| Secrets management | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | Tracked database credentials, signing secret, and bootstrap passwords were removed from `appsettings.json` and `appsettings.Production.json`; environment placeholders are documented in `.env.example`; local secure-file storage is ignored in `.gitignore`. | Configuration builds successfully. | Every previously exposed real credential must still be rotated outside the repository; production secret-provider configuration remains deployment work. |
| Authentication abuse controls | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | Identity now requires a 12-character complex password, deterministic common-password denial, production k-anonymity breach screening, five-attempt temporary lockout, authentication rate limiting, failed-login event persistence, real authenticator/recovery-code MFA, an enforced current-password change flow, and enumeration-safe one-time password reset. The breach service receives only a padded five-character SHA-1 prefix query; production fails password writes closed and readiness fails when screening is unavailable. Reset instructions use the configured email channel without persisting or logging the token; completion rotates the security stamp, revokes every governed session, and records token-free audit evidence. Privileged enrollment is driven by effective permission codes rather than role names; unenrolled privileged users and users flagged for password change are restricted to the appropriate remediation/profile/logout APIs (`Program.cs`; `API/Controllers/AuthController.cs`; `Infrastructure/Auth/MfaRequirementPolicy.cs`; `Infrastructure/Auth/PasswordChangePolicy.cs`; `Infrastructure/Auth/PasswordResetNotifier.cs`; `Infrastructure/Auth/CompromisedPasswordValidator.cs`). Public registration requires `Admin.Users.Manage`, and the public demo-credential endpoint/UI were removed. | Backend policy/JWT/middleware/reset/breach-protocol/readiness tests and frontend challenge/enrollment/password-change/reset tests pass. | External identity providers and authentication-policy administration remain. |
| Session/token handling | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | Raw refresh tokens are no longer persisted; SHA-256 digests are stored. Both access and refresh values now use path-scoped HttpOnly, SameSite=Strict cookies, and neither secret is returned to or stored by the SPA. Refresh rotation identifies the governed family from the one-way persisted refresh digest and sends no browser-readable token body. Access JWTs carry a session identifier and security-stamp snapshot; every authenticated request validates the active database session, account state, current security stamp, idle timeout, absolute timeout, and revocation state. Configurable concurrent-session limits, session listing, per-session revoke, logout, and logout-all are implemented. Password change, password reset, and MFA enable/disable rotate the Identity security stamp and revoke all governed session families. Bearer headers remain supported with precedence for Swagger and non-browser API clients (`Infrastructure/Auth/JwtService.cs`; `Infrastructure/Auth/AuthCookiePolicy.cs`; `API/Controllers/AuthController.cs`; `Program.cs`; `ClientApp/src/api/api.ts`; `Settings.tsx`). | Cookie policy/response-shape regression coverage, refresh coalescing, no-bearer/no-token-body client tests, reset invalidation, rotation/revocation, concurrent limits, JWT remediation claims, SQLite digest uniqueness, Settings transport, and full quality gates pass. | Configurable Entra ID/Active Directory/hybrid providers, production distributed-session load/revocation rehearsal, and browser penetration/CSRF acceptance remain. |
| CORS, health, and request protection | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | Allow-all CORS was replaced by configured origins with credentials; authentication endpoints use a fixed-window limiter; HSTS is enabled outside development; live/readiness health endpoints, database/outbox/scanner/object-storage/notification-channel checks, validated correlation IDs, and normalized ProblemDetails were added (`Program.cs`; `Infrastructure/Health`; `CorrelationIdMiddleware.cs`). | Backend build, correlation, outbox, fail-closed scanner/storage health, object-store operation contract, and missing-email-provider health tests pass. | Distributed tracing, metrics, and production alerting remain; external storage, scanner, and notification endpoints must be provisioned for readiness to pass. |
| Dynamic roles and role assignments | **PARTIALLY COMPLIANT** | **PARTIALLY COMPLIANT** | Roles carry stable code, PublicId, municipality ownership, effective dates, soft-disable state, and RowVersion. Effective-dated `SecurityUserRoleAssignment` records are append-preserving; administration now authors municipality, department, unit, start, and end, verifies selected-tenant ownership and unit/department consistency, and replaces against expected assignment IDs/RowVersions (`SecurityAdministrationController.cs`; `SecurityAdministration.tsx`). | Security tests reject mismatched unit/department scope; frontend tests verify the complete assignment payload. Migration `V39SecurityTenantFoundation` backfills Identity role links idempotently. | Municipality ownership remains incomplete on older business records; production HTTP concurrency and delegated-administration testing remain. |
| Deny-by-default, explicit deny, and record scope | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | The no-scope permissive fallback and unconditional SuperAdmin allow-all paths were removed. Active/effective role rules are combined centrally, explicit DENY overrides ALLOW, and the selected permission scope now constrains matching user/assignment scope. Legacy direct-user overrides are restriction-only and can no longer manufacture grants or bypass a role DENY. Authorization policies and the client permission refresh endpoint re-evaluate the database rather than trusting stale JWT permission claims. OPMS and IPMS KPI/submission/report queries apply scope predicates in SQL (`AccessControlService.cs`; `AccessController.cs`; `Program.cs`; performance controllers). | `DynamicSecurityTests.cs` verifies default deny, multi-role DENY precedence, user-override non-escalation, mismatched-scope rejection, effective-dated role-assignment scope, and navigation; latest backend suite passes 90/90. | Query-level filtering and stable-code enforcement must still be propagated to every remaining resource controller; mandatory MunicipalityId is not yet present on all records. |
| Security resource/action/member registry | **NOT IMPLEMENTED** | **PARTIALLY COMPLIANT** | Registered `SecurityResource`, `SecurityActionDefinition`, `SecurityMemberDefinition`, typed Permission metadata, and stable codes were added. The additive startup bootstrap is independent of demo seeding, is idempotent, does not overwrite administrator rules, and migrates existing OPMS grants to stable codes. The OPMS submission member catalogue includes actual performance, variance, submitted date, and internal-audit observation (`DynamicSecurityEntities.cs`; `SecurityRegistrySeeder.cs`). | Unique registry indexes and RowVersion fields are in the V3.9 security migration. | The full member catalogue and mappings for every resource remain incomplete. |
| Dynamic navigation | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | Navigation definitions are database records with stable codes, public IDs, hierarchy, routes, display order, required permission codes, active state and RowVersion. Login and `GET /api/navigation/my-menu` use `GetAuthorizedNavigationAsync`; empty inaccessible parents are omitted. System-scope registry administration now supports audited create/reparent/reorder/reroute/permission/deactivation with cycle and active-child guards, and the SPA exposes a hierarchical editor (`SecurityNavigationItem`; `AccessControlService.cs`; `SecurityAdministrationController.cs`; `NavigationRegistryEditor.tsx`). | Automated tests prove unauthorized leaves and empty parents are excluded and that public-ID hierarchy mutation rejects cycles. | Several secondary lookup routes remain to be registered; production HTTP concurrency and accessibility/browser verification remain. |
| Versioned security administration API | **NOT IMPLEMENTED** | **PARTIALLY COMPLIANT** | `/api/v1/security` exposes tenant-filtered role/user catalogues, audited tenant-role create/update, resource/action/navigation/member/permission registries, effective-dated user-role replacement, per-role permission rules, effective-permission preview, and governed navigation-definition mutation. Updates validate registered codes/scopes, prevent delegated grants the actor lacks, protect SYSTEM scope, compare RowVersion, return 409 on stale writes, and persist old/new audit data. The legacy role-permission and direct-user-grant mutation contracts return 410 rather than operating as a second authorization system (`SecurityAdministrationController.cs`; `RolesController.cs`; `UsersController.cs`). | Latest backend build and 90/90 backend tests pass. | Resource/action/member definition mutation, complete audit-reason coverage, ETag conventions, and HTTP integration tests remain. |
| Frontend security administration and capability service | **NOT IMPLEMENTED** | **PARTIALLY COMPLIANT** | `SecurityContext.tsx` centrally exposes resource CRUD, import/export, action, and member checks. `SecurityAdministration.tsx` provides tenant-role creation/general maintenance, department/unit/effective-date role assignment, Entity/CRUD, Navigation, Member, Action and Report grids, ALLOW/DENY/scope editing, optimistic-concurrency save, tenant-filtered effective-permission preview, and a public-ID hierarchical navigation editor. Generic lookup grids, reporting, workflow-governance, notification delivery operations, scoped window exceptions, RFI workspaces, canonical target-value editing, stage rating assignment, immutable rating history, POE assessment/replacement/legal-hold/disposal history, tenant calendars, organization masters, municipal employees, effective-dated placements, relational OPMS target mappings, governed ward/vote-number registers, cookie-only session administration, authenticator MFA, password change, and password reset use real governed APIs and stable capability checks where applicable. | Frontend type-check/lint and production build pass; 101/101 tests pass across 26 files. | Per-record UI capability responses and field controls across every form remain. |
| OPMS/IPMS API CRUD/action/member enforcement | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | OPMS and IPMS KPI and submission controllers now use stable registry codes for CRUD and workflow actions. KPI/submission list scope is translated into SQL. Submission responses redact Actual Performance, Variance, Submitted Date and Internal Audit fields unless member READ is effective; protected updates return 403 rather than silently accepting fields. Legacy role grants are additively mapped to the stable codes. | Backend build and security tests pass. | Equivalent enforcement is still required across IDP and all other modules. |
| Relational OPMS target mappings | **NON-COMPLIANT** | **COMPLIANT** | Ward, additional-assignee, and vote-number selections are authoritative tenant-owned junction records with restricted foreign keys, public IDs, tenant query filters, and per-target uniqueness. API contracts carry typed arrays; server validation rejects inactive, unknown, or cross-municipality references. Legacy CSV columns are preserved only as migration evidence and are not written by current code. | SQLite proves relational uniqueness and tenant isolation; the SQL Server migration contains guarded, deduplicated legacy backfill; frontend tests prove typed-array transport. | Native SQL Server execution remains an environment acceptance item. |
| Governed ward and vote-number masters | **NON-COMPLIANT** | **COMPLIANT** | Ward and Vote Number now have mandatory municipality ownership, public IDs, effective dates, soft-active state, RowVersion, tenant-scoped code uniqueness, query filters, write guards, restricted tenant relationships, stable security resources/navigation, reasoned audit writes, and no delete API. Vote numbers require an active same-tenant department. Deactivation is rejected while governed performance/IDP references exist. | SQLite proves same-code cross-tenant coexistence, within-tenant uniqueness, tenant query isolation, and stale-writer rejection. Backend/controller and frontend tests prove tenant relationships, audit reasons, versioned routes, concurrency-token transport, and real register/target-form usage. | Native SQL Server migration/FK/index/`rowversion` execution remains pending because the local service cannot be started from this host. |
| Secure POE storage | **NON-COMPLIANT** | **PARTIALLY COMPLIANT** | OPMS/IPMS uploads are capped at 25 MB, restricted to configured extension/MIME pairs, signature-validated, SHA-256 hashed, privately stored through one configurable filesystem/HTTP object-store abstraction, and sent to a configurable external HTTP malware scanner. Physical bytes and scan/integrity metadata live in tenant-owned `EvidenceBlob` records, while POE and IDP workflow/retention metadata remains in separate associations. Only an explicit clean verdict clears quarantine and exposes a protected URL; unavailable, unknown, error, and threat responses fail closed. Authorized rescan is the sole release path, RFI links retain association provenance, and POE has append-only assessment, replacement, legal-hold, and retention-disposal history. Disposal revalidates transactionally and deletes a blob only after the last governed association permits it. Hard delete remains prohibited. | Backend and frontend coverage verifies signatures, scanner outcomes, scanner/storage readiness, traversal rejection, opaque object keys, write/read/delete contracts, blob tenant isolation, shared-blob association behavior, RFI policy, append-only governance, idempotent storage deletion, and governed UI transport. The migration deterministically backfills both POE and IDP physical metadata before dropping duplicate columns. | Production object-store and scanner services/credentials must be provisioned, followed by live upload/download/rescan/disposal and failure-recovery rehearsal. |
| External notification delivery | **NOT IMPLEMENTED** | **PARTIALLY COMPLIANT** | Configured in-app/email fan-out uses a vendor-neutral HTTP sender, stable idempotency key, bounded timeout, retry-safe partial success, provider/reference/detail evidence, dead-letter listing, audited RowVersion replay, and readiness validation. | Worker tests prove first-attempt provider failure, preserved in-app success, email retry, single delivery row, and final provider receipt; operations UI replay is covered. | Production provider/credentials, SMS, templates/localization, policy-driven reminder/escalation scheduling, and provider sandbox/live rehearsal remain. |
| Quality gate and CI | **PARTIALLY COMPLIANT** | **PARTIALLY COMPLIANT** | The backend project now declares `IsTestProject`; normal `dotnet test` executes tests. Frontend type and lint defects were corrected. `.github/workflows/quality.yml` covers restore, build, backend/frontend tests, type-check, lint, migration script generation, dependency audit, npm audit, and secret scanning. Patched transitive floors are explicitly pinned for Microsoft.OpenApi and the SQLite native bundle. | Backend: 123/123 passed plus one explicitly skipped SQL Server integration gate. Frontend: 101/101 passed. Type-check, lint, release builds, model/snapshot consistency, 271,968-byte idempotent migration SQL generation, and the earlier online transitive vulnerability scan pass. | The dedicated local SQL Server service was installed but stopped and could not be started from the non-administrative execution host; SQL Server migration, constraint, and native `rowversion` execution therefore remain unverified. CI has not yet run on the remote platform. |

### 11.2 Migration evidence

`Migrations/20261001214559_V39SecurityTenantFoundation.cs` is the combined additive foundation migration. It creates the municipality/security registries, tenant masters, employee/assignment history, tenant foreign keys, public identifiers, concurrency tokens, and effective-dated role assignments. It enriches roles/permissions/scopes, assigns safe legacy defaults, generates unique public identifiers before enabling unique indexes, backfills role codes and existing `AspNetUserRoles` rows, and preserves existing scopes and role permissions. `Migrations/20261001215219_V39TenantIsolationFilters.cs` records the query-filter model metadata; the runtime filters themselves are enforced by `ApplicationDbContext`. An idempotent SQL script was generated successfully from the migration chain.

### 11.3 Current security acceptance boundary

The central engine now proves default deny, explicit deny precedence, permission-specific scope, effective-dated assignment scope, SQL-level OPMS list filtering, dynamic navigation, member redaction/update protection, audited concurrent role-rule editing, and tenant-filtered administration catalogues. The implementation is not yet complete against the XAF-style brief because these controls have not been migrated across every controller/module; the navigation hierarchy and user-assignment experience are not fully editable in the new screen; tenant ownership has not propagated to all governed entities; and comprehensive HTTP/direct-bypass tests remain. Those items remain required before this section can be marked **COMPLIANT**.

### 11.4 Phase 1 tenant and authoritative-master foundation

The repository now has a real `Municipality` tenant root plus `FinancialYear`, `MunicipalityFinancialYear`, `ReportingPeriod`, `MunicipalEmployee`, and effective-dated `EmployeeAssignment` entities. Departments, units, users, OPMS/IPMS targets, and OPMS/IPMS submissions carry migration-stage tenant ownership and public identifiers. Tenant-owned reads are guarded by EF Core global query filters, and tracked writes are stamped or rejected centrally by `ApplicationDbContext`; legacy rows with no reconciled municipality remain deliberately hidden rather than being assigned to an arbitrary tenant.

`TenantResolutionMiddleware` resolves `X-Municipality-Id` against active database role assignments, automatically selects a sole authorized municipality, denies an unauthorized header, and defaults multi-context non-system users to a sentinel that yields no tenant records. Effective permissions and navigation are recalculated against the selected municipality. The SPA stores the selection only for the browser session, sends it on normal, refresh, retry, and logout requests, clears tenant-sensitive UI capabilities until a multi-municipality user selects a context, and exposes a top-bar municipality selector. `GET /api/v1/tenancy/my-contexts` returns only authorized active choices.

`TenantMastersController` introduces versioned, public-ID-based APIs for financial years, municipality-year activation, reporting periods, employees, and employee assignments. `OrganizationMastersController` adds governed department, unit, and position masters. The APIs validate effective bounds and tenant-relative relationships, use RowVersion optimistic concurrency on updates, require governance reasons for organization changes, and serialize employee-assignment creation to prevent overlapping effective-date ranges. Legacy integer department/unit mutation routes return HTTP 410; lookup reads remain compatible. `TenantIsolationTests.cs` and `OrganizationMastersControllerTests.cs` prove cross-tenant read exclusion, cross-tenant write rejection, automatic tenant stamping, organization uniqueness, normalized-value isolation, and append-only revision protection.

This is still **PARTIALLY COMPLIANT** for Phase 1. Tenant ownership has since been propagated through the governed IDP, workflow, reporting, notification, audit/outbox, and evidence increments, and tenant calendar/organization/employee/placement administration is now available as recorded in sections 11.31 and 11.32. Remaining legacy nullable tenant keys must be reconciled and made mandatory; tenant-master endpoints still need full HTTP-host integration coverage; and concurrent isolation, filtered uniqueness, and unique-current-year behavior require execution against the required SQL Server instance.

### 11.5 Phase 2 normalized values, revisions, and logical submissions

`PerformancePeriodTarget` now represents one authoritative target value per KPI and reporting period, with a period-specific `PerformanceUnitKind`, direction, budget, description, tenant ownership, public identifier, and RowVersion. Filtered unique indexes enforce one OPMS or IPMS target row per reporting period, and a database check constraint requires exactly one KPI type. `PerformanceTargetRevision` records every changed field with original/revised values, reason, approval reference, effective date, actor, and record time; the DbContext rejects update or deletion of revision history. `PerformancePeriodTargetsController` provides tenant-filtered, public-ID-based create/read/revise/history APIs with dynamic permission and record-scope checks.

The centralized `PerformanceUnitEngine` parses and canonicalizes the full client unit catalogue, including invariant numeric formats, 0–100 percentages, whole non-negative counts, two-to-four-segment colon ratios, ISO dates, binary aliases, readiness levels, qualitative text, zero-based values, and reverse units. It calculates variance, achievement percentage, and achieved status server-side with explicit higher/lower/exact direction and zero-target protection. Positive, negative, boundary, ratio, reverse, date, and divide-by-zero tests are included.

OPMS and IPMS submission writes now resolve an active configured reporting period and normalized KPI-period target, persist canonical `ActualPerformance`, and calculate variance/achievement on the server. Filtered unique database indexes enforce one logical submission per KPI/period, and repeated POST returns the existing logical submission. Clients can no longer select due dates through create/update or persist their own variance. IPMS also now uses the stable dynamic registry, SQL-translated record scope, member-level actual/variance/submitted-date/internal-audit redaction, and soft retention for targets/submissions. The additive `V39NormalizedPerformanceValues` and `V39GovernedRecordRetention` migrations were included in a successfully generated 163,473-byte idempotent SQL script. Backend tests pass 54/54.

The OPMS and IPMS edit experiences now expose an authoritative reporting-period target editor backed by the normalized API. It lists typed canonical values, permits only unused active reporting periods for creation, requires an approval reference and reason for revision, submits RowVersion for concurrency, and displays the immutable revision history. The legacy wide quarterly controls remain only on initial legacy-compatible creation while historical backfill and cutover are unresolved.

Phase 2 remains **PARTIALLY COMPLIANT** until legacy wide target rows and free-text quarters are reconciled/backfilled, initial creation writes exclusively to the normalized model, all legacy actual fields are retired after a compatibility window, submission base state is separated completely from workflow state, and production SQL migration/backfill rehearsals resolve any historical duplicates.

### 11.6 Phase 3 configurable workflow and reporting-window foundation

`WorkflowDefinition` and ordered `WorkflowStageDefinition` records now configure a tenant/year/submission-kind workflow, including required permission/action codes, optional stages, bypass conditions, and segregation-of-duties behavior. `SubmissionWorkflowInstance` records the current configured stage while append-only `SubmissionWorkflowAction` rows preserve the canonical ordered action history. `PerformanceRfi`, `ReportingWindow`, scoped `ReportingWindowException`, `RatingScheme`, and `RatingValue` establish the remaining Phase 3 domain vocabulary.

`ConfigurableWorkflowService` validates transitions, actor permissions, optional-stage bypass, terminal states, and separation of duties. `ReportingWindowService` centralizes normal-window and scoped-exception decisions. The OPMS and IPMS action endpoints use the configured engine when an active definition exists and persist the canonical workflow action, compatibility status projection, and audit record in one database transaction; the legacy transition path remains only as a compatibility fallback when no definition exists. `/api/v1/workflow` provides tenant-filtered definition, reporting-window, scoped-exception, rating-scheme, action-history, and RFI lifecycle APIs. Window exceptions require exactly one active user/department/unit scope, a reason, and a later close. Rating ranges reject duplicate values/order and overlapping intervals. RFI raise/respond/close actions enforce target-derived record scope, RowVersion concurrency, and append to the canonical workflow ledger without advancing its stage. `ConfigurableWorkflowTests.cs` covers configured advancement, segregation-of-duties rejection, and scoped window extensions. Migration `20261001222238_V39ConfigurableWorkflow` is included in the verified migration chain.

This remains **PARTIALLY COMPLIANT**. Scoped-exception administration, submission RFI workspaces, immutable stage-specific ratings, governed RFI evidence associations, external email delivery, and governed workflow version comparison/retirement are now available. Historical status/score reconciliation remains. Workflow definitions validate registered action-permission pairs, terminal-stage shape, backward rejection targets, and rating-scheme assignments, while generic history reads use target-derived department/unit/owner scope. Section 11.30 records the version-governance implementation.

### 11.7 Phase 3 evidence-integrity increment

Evidence upload now validates PDF, PNG, JPEG, DOCX, and XLSX content signatures before writing, rejects executable headers and extension/content spoofing, calculates SHA-256, stores outside the public web root, and exposes files only through authorized streaming endpoints. Evidence metadata now carries tenant ownership, public identity, scan state, quarantine state, active/retention lifecycle, replacement reference, and RowVersion. Lists and downloads exclude inactive, quarantined, or unverified records. The additive `20261001222725_V39EvidenceIntegrity` migration gives every legacy record a unique public identifier and hash placeholder, marks it `LegacyUnverified`, quarantines it, and assigns a seven-year retention date before unique indexes are created.

The complete idempotent migration script includes the evidence, audit/outbox, IDP-isolation, and notification-delivery migrations and generates successfully at 197,313 bytes. Backend build and 65/65 tests pass; frontend production build, 56/56 tests, TypeScript type-check, and ESLint pass.

This original increment did **not** claim antivirus compliance: `SignatureValidated` meant only structural file-signature validation. Section 11.17 adds the external-scanner boundary and fail-closed quarantine-to-release workflow. Production acceptance still requires deployment of the scanner provider plus a physical-file/POE-association split, immutable assessment/replacement actions, object-storage durability controls, legal holds, and retention disposal evidence.

### 11.8 Tenant audit and transactional-outbox foundation

`AuditTrail` and `Notification` records now carry tenant ownership and public identifiers and are protected by tenant query filters and the centralized cross-tenant write guard. Audit rows also provide correlation, reason, and user-agent fields, and the DbContext rejects audit update or deletion. `BusinessEventOutbox` adds tenant-owned durable event payloads, aggregate identity, availability/processing state, retry count, failure detail, correlation, and optimistic concurrency.

The OPMS and IPMS workflow paths now stage the compatibility status projection, canonical configured action, review comment, audit row, in-app notifications, and notification outbox event before a single `SaveChanges`; legacy and configured transitions therefore no longer commit these records in separate steps. `WorkflowGovernanceService` retains convenience methods for independent callers but exposes queue methods for transactionally composed operations. Tests prove tenant stamping, audit immutability, and paired notification/outbox persistence. Migration `20261001223655_V39TenantAuditOutbox` generates unique public identifiers for existing audit/notification rows, derives legacy tenant ownership from their user, and preserves unreconciled rows as hidden rather than assigning an arbitrary tenant.

The hosted notification worker now consumes pending events in batches, deduplicates recipient delivery with a unique idempotency key, records tenant-scoped `NotificationDeliveryAttempt` rows, marks successful events, and applies bounded exponential retry state. Exhausted retries and delayed backlog affect readiness through `OutboxHealthCheck`. Migration `20261001225822_V39NotificationDeliveryWorker` adds the delivery ledger, and its automated test proves duplicate-recipient suppression plus idempotent reprocessing.

This remains **PARTIALLY COMPLIANT**. Section 11.21 adds external email delivery, provider receipts, fail-closed readiness, and operator dead-letter replay. SMS, templates/localization, escalation/reminder scheduling, and production provider rehearsal remain. Audit reason/correlation/user-agent capture must also be populated consistently outside governed mutation paths, and production database permissions or triggers should reinforce append-only history against writes that bypass EF Core.

### 11.9 API correlation and IDP tenant isolation

The HTTP pipeline now validates or creates an `X-Correlation-ID`, echoes it to callers, places it in the structured logging scope, and adds it to RFC 7807 `ProblemDetails`. Central exception handling maps authorization, validation, concurrency, and unexpected errors without exposing internal detail for server failures; otherwise-empty API status responses are also normalized. Middleware tests cover safe caller IDs and unsafe-ID replacement.

IDP plans now carry tenant ownership, public identifiers, tenant-relative unique plan codes, and RowVersion. Global filters cover the complete IDP aggregate graph through its plan relationship, including versions, hierarchy, participation, risks, budgets, documents, collaboration comments, and tasks; legacy plans with no reconciled tenant are hidden. The tenant write guard stamps new plans and now rejects the multi-tenant no-selection sentinel instead of persisting it as an invalid municipality. Plan creation derives the municipality name from the selected active tenant in normal HTTP operation and saves the initial version plus audit in one transaction. Plan responses expose PublicId and RowVersion, and updates accept optimistic concurrency while retaining integer-route compatibility. Migration `20261001224717_V39IdpTenantIsolation` backfills public identifiers and derives legacy ownership from the creating user before enabling unique indexes. Tests prove plan and child isolation, legacy hiding, and missing-selection rejection.

This remains **PARTIALLY COMPLIANT**. IDP routes and most child DTOs still expose internal integer identifiers for compatibility, child rows rely on the tenant-filtered aggregate relationship instead of carrying mandatory direct tenant keys, and stable registry/member/action enforcement has not been applied uniformly across every IDP endpoint. Section 11.18 records the governed public-ID document boundary that replaces the former client-supplied storage-path contract.

### 11.10 Workflow governance administration UI

The mock approval-setup page has been replaced on its existing route by `WorkflowGovernanceAdminPage`. It loads authoritative tenant reporting periods and configured workflow/window/rating records, creates versioned OPMS or IPMS stage definitions, exposes segregation/optional-bypass/rejection-stage controls, creates reporting windows, and authors non-overlapping rating values. The route is protected by `WORKFLOW.CONFIGURE` ahead of the generic administration rule. The shared client transport now normalizes RFC 7807 responses into the existing `ApiResponse` shape so validation detail remains visible. UI tests cover authoritative loading, tab navigation, and default-deny/stable-permission routing.

This remains **PARTIALLY COMPLIANT** until legacy score reconciliation and full accessibility/browser verification are added. Rating schemes can now be assigned to individual stages as described in section 11.19, section 11.23 closes the RFI evidence-association gap, and section 11.30 closes repository-side workflow version comparison/retirement.

### 11.11 Tenant-scoped performance reporting

`PerformanceReportsController` now provides versioned OPMS/IPMS summary and CSV endpoints backed by canonical tenant-filtered targets and submissions. Both endpoints resolve stable report actions through the central evaluator and apply department, unit, owner and assigned-target query scope before aggregation or export. Summary results expose configured-target, submission, achieved, at-risk, pending and department-average measures; CSV export is capped at 100,000 rows, includes a UTF-8 BOM, quotes fields, and neutralizes spreadsheet formula prefixes. Generate/export activity is written to the tenant audit ledger, and CSV encoding tests cover formula, delimiter, quote and line-break cases.

The previous static report sample was replaced by a live reporting dashboard with OPMS/IPMS and authoritative-period selection, server metrics, department visualization/detail, stable route permissions, and a governed CSV download. UI regression tests prove server data rendering and export capability behavior. Real PDF/XLSX templates, asynchronous large-report jobs, scheduled distribution, report-definition administration, and broader report families remain outstanding; this increment is therefore **PARTIALLY COMPLIANT**.

### 11.12 Dynamic-security consolidation increment

The versioned security surface now supports audited, tenant-bound role creation and RowVersion-protected role maintenance in the same administration screen used for permissions and effective-access preview. Role codes are stable and tenant-unique; system roles cannot be changed through tenant administration; municipality administrators cannot create roles outside their active context or assign permissions they do not themselves administer. Effective-dated assignment replacement remains append-preserving and audited.

The old backend hard-coded menu builders were removed. Runtime navigation and role-audit diagnostics now use the database registry and identical parent-pruning logic. The additive registry was expanded across the application’s primary route families, while compatibility grants are migrated additively to stable navigation codes without overwriting administrator configuration. Legacy direct user ALLOW overrides and legacy permission mutation endpoints no longer provide a parallel escalation path; allow grants flow through tenant-scoped roles, while old user overrides can only reduce access. Automated tests cover both explicit-DENY precedence and attempted user-override escalation.

At this increment, backend **72/72** tests passed; frontend **58/58** tests passed; TypeScript type-check, ESLint, frontend production build, and the **197,313-byte** idempotent migration script passed. The dependency advisories recorded at that point are remediated in section 11.16. Incomplete HTTP/direct-bypass integration coverage, incomplete member enforcement outside performance submissions, missing registry-definition editors, and incomplete tenant propagation across older modules remain. The XAF-style security requirement remains **PARTIALLY COMPLIANT**, not production-certified.

### 11.13 Canonical reporting-period target editor increment

OPMS and IPMS edit routes now consume the public KPI identifiers returned by the versioned APIs and embed a shared `PerformancePeriodTargetEditor`. The editor loads active tenant reporting periods and canonical values, filters already-configured periods during creation, supports the complete unit and direction catalogue, and uses stable resource capabilities for create/update affordances. Revisions require reason, approval reference, effective date, active state, and the current RowVersion; successful writes refresh from the authoritative service, while revision history remains visible and append-only.

The legacy wide quarterly panels are no longer shown during edit, which prevents an existing KPI from presenting two competing edit surfaces. They remain on initial creation only as an explicit compatibility bridge pending historical-data backfill and an atomic create flow that writes the KPI and normalized period rows together. API response models now carry target PublicId values end to end so UI routes do not derive canonical operations from internal database identifiers.

Latest verification: backend **72/72** tests pass; frontend **60/60** tests pass across 12 files; TypeScript type-check, ESLint, and the frontend production build pass. The verified idempotent migration artifact remains **197,313 bytes** because this increment changes application contracts and UI behavior without changing the schema. Phase 2 remains **PARTIALLY COMPLIANT** until the compatibility creation path and production data reconciliation are retired and rehearsed.

### 11.14 Scoped reporting-window exception administration

The workflow-governance window tab now opens a governed exception workspace for an existing reporting window. Administrators choose exactly one active user, department, or unit using tenant-filtered records, set an extended close later than the normal server-owned close, and provide the approval reason. Existing exceptions are loaded from the authoritative API and refreshed after creation. User, department, and unit response contracts now expose their PublicId values, so exception requests use external identifiers while the existing compatibility identifiers remain response-only.

The canonical target-revision transport was also corrected to call the controller's `/revisions` route rather than the obsolete `/history` path. A transport-level test now verifies this contract in addition to the editor behavior tests, preventing a mocked component test from masking a broken real request.

At this increment, backend **72/72** tests passed; frontend **62/62** tests passed across 13 files; TypeScript type-check, ESLint, frontend production build, and `git diff --check` passed. The dependency advisories observed at that point are remediated in section 11.16; the bundle-size warning remains.

### 11.15 Submission RFI workspace

The shared OPMS/IPMS submission workspace now embeds a governed RFI panel in Comments & History. It loads the tenant- and record-scoped RFI ledger and exposes raise, respond, and close actions only when the current capability set includes the corresponding stable `OPMS_RFI.*` or `IPMS_RFI.*` action. Questions carry a future response due time; responses and closure submit the current RowVersion; every successful action reloads authoritative server state. This client behavior complements the existing server transaction that records the RFI mutation, workflow action, audit entry, and notification/outbox event.

At this increment, backend **72/72** tests passed; frontend **63/63** tests passed across 14 files; TypeScript type-check, ESLint, and the frontend production build passed. Phase 3 remained **PARTIALLY COMPLIANT** because RFI evidence associations, stage ratings, workflow retirement/comparison, external notification channels, and production browser/accessibility verification were outstanding. Sections 11.19, 11.21, and 11.23 subsequently close the stage-rating, external-email, and RFI evidence-association gaps respectively.

### 11.16 Dependency advisory remediation

The host now pins `Microsoft.OpenApi` **2.12.2**, replacing the vulnerable 2.4.1 transitive resolution while staying on the Swashbuckle-compatible 2.x line. The test project pins `SQLitePCLRaw.bundle_e_sqlite3` **2.1.13**, which resolves the core, provider, and native library to 2.1.13 instead of vulnerable 2.1.11. These are deliberate top-level dependency floors rather than warning suppression.

Restore from the repository's nuget.org-only configuration succeeds; backend **72/72** tests pass after the upgrade. An online `dotnet list ... package --vulnerable --include-transitive` check reports that the test project and its referenced host have **no vulnerable packages** from the current source. Dependency risk is therefore improved from a known high-severity blocker to ongoing monitoring; CI execution, future advisory drift, and controlled update cadence remain operational responsibilities.

### 11.17 Fail-closed malware scanning and evidence release

`IEvidenceMalwareScanner` and its HTTP implementation now provide a vendor-neutral external scan boundary with configurable scan/health endpoints, bounded timeout, optional API key, provider reference, and structured verdict. Uploads still undergo local signature validation and hashing first, then call the external scanner. Only the exact `Clean` verdict sets `IsQuarantined = false`; threats, unknown results, HTTP errors, timeouts, and absent configuration all remain quarantined and return no download URL. Readiness is unhealthy when the scanner health endpoint is absent or unreachable, making a missing production integration visible rather than silently bypassed.

Both OPMS and IPMS expose record-scoped, permission-checked rescan actions. Rescanning updates provider/reference/detail/time metadata, releases only after a clean result, and writes an audit action. Downloads independently require `SignatureVerified`, `ScanStatus == Clean`, and a non-quarantined active record. Secure file resolution now canonicalizes the private root and rejects traversal. The SPA displays scan/quarantine detail, withholds download for quarantined evidence, and exposes the governed rescan action in all shared submission workspaces.

Migration `20261002055523_V39EvidenceMalwareScanning` adds scanner evidence fields and explicitly quarantines every existing non-clean row, converting former `SignatureValidated` rows to `PendingMalwareScan`. The full idempotent migration script generates successfully at **198,868 bytes**. Backend **76/76** tests pass; frontend **64/64** tests pass across 15 files; TypeScript type-check, ESLint, production build, and `git diff --check` pass. This remains **PARTIALLY COMPLIANT** until a production scanner is provisioned and durability, evidence-association, legal-hold, replacement, and disposal controls are completed.

### 11.18 Governed IDP document storage

The legacy `POST /api/idp/documents` metadata contract no longer persists client-supplied file names, sizes, approval state, or storage paths and now returns HTTP 410. Versioned tenant-filtered routes under `/api/v1/idp/plans/{planPublicId}/documents` accept multipart content and use plan/document public identifiers. The server owns the private path, derives document version order, validates extension/MIME and binary signature, calculates SHA-256, calls the fail-closed external scanner, applies seven-year retention, and records scanner evidence. Only active, signature-verified documents with an exact `Clean` verdict receive a protected download URL; canonical path validation and the same clean-state checks are repeated at download. Authorized rescans are audited and are the only route from quarantine to release. Plan deletion is restricted while retained documents exist, and internal document/plan identifiers and physical storage paths are absent from the response contract.

Migration `20261002060518_V39GovernedIdpDocuments` adds public identity, integrity, scanner, quarantine, retention, active-state, and RowVersion fields. It gives every legacy row a unique ID before creating the unique index and explicitly marks historical metadata `LegacyUnverified`, unverified, and quarantined rather than assuming old content is safe. Automated tests prove that the former storage-path contract is retired and that quarantined records never receive a download URL. Backend **78/78** tests and frontend **64/64** tests pass; TypeScript type-check, ESLint, host/frontend builds, and the **203,659-byte** idempotent migration script pass.

This increment remains **PARTIALLY COMPLIANT** because production scanner/object-storage provisioning, legal holds, retention disposal evidence, independent document approval actions, and uniform member/action security across the wider IDP aggregate remain outstanding.

### 11.19 Configured, immutable workflow stage ratings

Workflow stage definitions can now reference an active tenant rating scheme and independently declare whether a rating is mandatory. Definition creation rejects required stages without a scheme and rejects cross-tenant or inactive scheme references. At action time, the configurable workflow engine rejects a missing required rating, a rating on an unconfigured stage, or a numeric value that is not one of the assigned scheme's registered values.

An accepted value creates `SubmissionStageRating` alongside the canonical workflow action. The append-only tenant ledger retains public identity, workflow instance/action/stage provenance, rating scheme and value identities, numeric value, immutable label snapshot, optional achievement/comment context, actor, timestamp, and correlation identifier. EF Core rejects updates and deletes, and all foreign-key deletes are restricted. The scoped ratings endpoint returns only ratings for a submission the caller may read. The workflow administration UI assigns schemes per stage and marks required stages; the shared submission history renders the authoritative scheme, stage, value snapshot, actor, time, and comment rather than inferring history from mutable submission score columns.

Migration `20261002061346_V39ImmutableStageRatings` adds the optional stage-to-scheme relationship and the normalized ledger with unique action and public-ID constraints. Automated service tests prove missing-rating rejection, configured-value persistence, label/correlation snapshots, and append-only enforcement; a UI test proves authoritative rating history rendering. Backend **78/78** and frontend **65/65** tests pass, as do TypeScript type-check, ESLint, host/frontend builds, the **209,507-byte** idempotent migration script, and `git diff --check`.

This requirement remains **PARTIALLY COMPLIANT** until historical `SubmissionScore` and embedded verifier/approver/PMS/auditor score columns are reconciled into the ledger, achievement-derived automatic rating selection is configured where required, and production SQL/browser rehearsals are completed.

### 11.20 Governed hierarchical navigation registry

`SecurityNavigationItem` now has a non-enumerable public identifier in addition to its immutable stable code. The versioned security API returns parent public IDs and both active and inactive definitions, and adds create/update operations protected by `SECURITY.MANAGE_NAVIGATION` plus an explicit effective `SECURITY.SYSTEM_SCOPE` check because the registry is global. Mutations require a selected municipality for the tenant audit, a 5-500 character reason, valid application-relative route, registered permission, bounded display order, and current RowVersion. The server rejects self/descendant cycles, missing or inactive parents, invalid permission codes, and deactivation of a node with active children. There is no hard-delete endpoint.

The Security Administration page now embeds `NavigationRegistryEditor`, which renders the authoritative tree, preserves immutable codes, edits parent/order/name/route/icon/permission/active state, submits RowVersion automatically, and exposes server validation/concurrency messages. Successful writes record old/new registry state, reason, correlation, actor, IP, and user-agent in the append-only tenant audit ledger. Migration `20261002062050_V39GovernedNavigationRegistry` assigns every existing definition a unique public ID before creating its unique index.

Automated backend coverage proves public-ID parent creation, audit-reason persistence, and descendant-cycle rejection. Frontend coverage proves hierarchical loading and audited optimistic updates. Backend **79/79** and frontend **66/66** tests pass; TypeScript type-check, ESLint, host/frontend builds, the **210,605-byte** idempotent migration script, and `git diff --check` pass.

Dynamic navigation remains **PARTIALLY COMPLIANT** pending registration of every secondary route, production HTTP concurrency/security testing, and browser accessibility validation of the editor.

### 11.21 External notification fan-out and delivery operations

The notification worker now fans each transactional outbox event across the configured channel set. `IN_APP` remains the default development channel; production enables `IN_APP,EMAIL`. `HttpEmailNotificationSender` is a vendor-neutral bounded-time HTTP adapter with optional secret API key and a stable provider idempotency header. A delivery is successful only after the provider returns a successful response. Missing sender, recipient address, endpoint, provider error, timeout, or HTTP failure is retained as a failed result and keeps the outbox event pending with exponential backoff.

`NotificationDeliveryAttempt` is now a durable per-event/recipient/channel record rather than a one-shot marker. It preserves attempt count, latest status/time, delivery time, provider, provider reference, response detail, and error under one stable idempotency key. Successful channels are skipped on retry while failed channels reuse and update their existing ledger row, preventing duplicate in-app or email sends. The event is marked processed only after every configured channel for every recipient has succeeded. `NotificationChannelHealthCheck` makes production readiness unhealthy when a configured adapter or the enabled email endpoint is absent.

`/api/v1/notification-operations/pending` exposes the tenant-filtered pending/dead-letter queue and channel evidence to workflow administrators. Replay requires a reason and current RowVersion, resets retry scheduling without erasing successful receipts, and records old/new state, actor, reason, correlation, IP, and user-agent in the audit ledger. The Workflow Governance UI includes a Delivery tab for provider evidence and reasoned replay. Migration `20261002063011_V39ExternalNotificationDelivery` adds provider evidence and attempt counters without narrowing or discarding the existing error column.

Automated tests prove partial first-attempt success, external failure retention, idempotent retry, final provider receipt, and fail-closed readiness; the UI test proves evidence rendering and reasoned replay. Backend **82/82** and frontend **68/68** tests pass; TypeScript type-check, ESLint, host/frontend builds, the **211,922-byte** idempotent migration script, and `git diff --check` pass. Notification compliance remains **PARTIALLY COMPLIANT** until SMS, reusable templates/localization, policy-driven reminders/escalations, production provider provisioning, and live delivery rehearsals are complete.

### 11.22 Effective-dated role assignment scope editor

Security Administration now edits the full assignment record rather than reducing it to role checkboxes. Each selected role carries its municipality, optional department and unit, effective-from, and optional effective-to values. Unit choices are filtered by department in the client. The server independently validates time ordering, selected-tenant ownership, active department/unit existence, and department/unit consistency before append-preserving replacement against expected assignment IDs and RowVersions. Automated tests cover the complete client payload and server rejection of a unit outside the selected department.

This closes the previously documented assignment-control UI gap, while overall dynamic security remains **PARTIALLY COMPLIANT** pending complete tenant propagation across legacy resources, per-record capability responses throughout the SPA, production HTTP concurrency tests, and full member enforcement outside the performance modules.

### 11.23 Append-only RFI evidence provenance

Performance RFIs can now reference governed POE at both question and response time without copying files or bypassing the existing release controls. `PerformanceRfiEvidence` is a tenant-owned append-only ledger with public identity, RFI and POE provenance, purpose, actor, timestamp, and correlation identifier. Foreign-key deletion is restricted and `ApplicationDbContext` rejects modification or deletion after insertion. The unique RFI/POE/purpose constraint prevents duplicate links for one action context.

`RfiEvidencePolicy` accepts at most 20 distinct public identifiers and requires every referenced record to exist in the selected municipality, belong to the same submission and kind, remain active, have a verified binary signature, have an exact clean malware verdict, and not be quarantined. The workflow API returns immutable evidence-link metadata and a protected URL only through the underlying POE release rule. The RFI workspace loads the submission's released evidence, lets an authorized actor associate it with a question or response, and renders returned question/response provenance.

Migration `20261002063908_V39RfiEvidenceProvenance` creates the normalized ledger with restricted relationships and unique public/provenance indexes. Backend tests prove quarantine and cross-submission rejection plus append-only enforcement; frontend tests prove response association payloads and authoritative ledger rendering. Backend **84/84** and frontend **69/69** tests pass, along with TypeScript type-check, ESLint, host/frontend builds, and the **214,950-byte** idempotent migration script.

The RFI evidence-association requirement is implemented. Wider POE compliance remains **PARTIALLY COMPLIANT** pending the physical-blob/association split for all evidence contexts, an immutable replacement ledger, legal holds, retention disposal evidence, durable object storage, and production scanner provisioning. Section 11.24 closes the assessment-ledger gap.

### 11.24 Immutable POE assessment history

Released OPMS and IPMS evidence now carries an append-only `PoeEvidenceAssessment` history rather than a mutable acceptance flag. Each decision records a public identifier, tenant, POE identity, accepted/rejected/needs-clarification outcome, bounded comment, assessor, timestamp, and correlation identifier. All relationships use restricted deletion, and the DbContext rejects updates and deletes. `PoeAssessmentPolicy` permits assessment only for active, signature-verified, clean, non-quarantined evidence; rejected and clarification outcomes require an explanatory comment.

The OPMS/IPMS assessment endpoints enforce the stable `OPMS_POE.ASSESS` or `IPMS_POE.ASSESS` action under the submission's record scope, append the decision, and write a tenant audit event. Existing internal-audit grants are additively mapped to the new action without overwriting administrator policy. Attachment responses include the complete ordered decision history. The shared evidence workspace exposes assessment controls only when the action is effective and renders every authoritative decision rather than replacing the previous one.

Migration `20261002064933_V39ImmutablePoeAssessments` creates the tenant ledger and its restricted actor, municipality, and POE relationships. Backend tests prove quarantine and comment policy plus append-only enforcement; frontend coverage proves decision submission and history rendering. Backend **85/85** and frontend **70/70** tests pass, together with TypeScript type-check, ESLint, host/frontend builds, and the **217,463-byte** idempotent migration script.

POE assessment history is implemented. Evidence governance remains **PARTIALLY COMPLIANT** until physical blob/association separation, legal hold and disposal controls, durable object storage, and production scanner integration are completed. Section 11.25 closes the replacement-provenance gap.

### 11.25 Immutable POE replacement provenance

OPMS and IPMS now replace evidence through an explicit ledger instead of deletion or in-place file mutation. `PoeEvidenceReplacement` preserves the public identity of the retained predecessor and released successor, municipality, bounded reason, actor, timestamp, and correlation identifier. Both file relationships and the actor/tenant relationships use restricted deletion; unique predecessor and successor indexes prevent branching or reuse within a replacement step; DbContext rejects ledger modification and deletion.

The replacement action requires stable `OPMS_POE.REPLACE` or `IPMS_POE.REPLACE` permission under the submission's effective record scope. Both records must be active and belong to the same submission, the successor must be signature-verified, clean, and released, and neither record may already conflict with replacement provenance. The client sends both SQL row-version tokens, producing a conflict rather than silently replacing concurrently changed evidence. On success the predecessor is deactivated but retained, the successor remains active with its provenance, and a tenant audit event records the transition. Existing POE-upload grants are additively mapped to the replacement action.

The shared evidence workspace lets an authorized user select a separately uploaded clean successor, enter a reason, and submit both concurrency tokens. It retains the predecessor as visibly retired evidence and renders authoritative replacement history. Migration `20261002065833_V39ImmutablePoeReplacementProvenance` creates the normalized ledger with restricted relationships and unique public/predecessor/successor indexes. Tests prove same-submission and clean-successor policy, append-only enforcement, UI payload construction, and the exact API route/body contract.

Backend **86/86** and frontend **72/72** tests passed at this increment, together with TypeScript type-check, ESLint, host/frontend builds, and the **220,573-byte** idempotent migration script. Replacement provenance is implemented; broader evidence compliance remains **PARTIALLY COMPLIANT** pending physical blob/association separation, retention-disposal workflows, durable object storage, and production scanner provisioning. Section 11.26 closes the legal-hold gap.

### 11.26 Append-only POE legal holds

POE preservation is now represented by append-only place/release events instead of a mutable boolean. Each `PoeLegalHoldEvent` carries a stable hold identifier, unique event public ID, evidence and municipality identity, place/release action, external case/reference, bounded reason, actor, timestamp, and correlation identifier. Unique evidence/hold/action indexing prevents duplicate placement or release events, all relationships use restricted deletion, and DbContext rejects event updates or deletes. Current hold state is derived from the complete event stream through `PoeLegalHoldPolicy`.

OPMS and IPMS expose governed place and release actions under stable `*.PLACE_HOLD` and `*.RELEASE_HOLD` permissions with submission record scope. Placement rejects duplicate active references; release requires an existing active hold and becomes conflict-safe through the unique release event. Both operations persist tenant audit events. Internal-audit grants are additively mapped to hold administration without overwriting administrator-defined rules.

Attachment responses return placed/released history and active state. The shared evidence workspace renders that history, lets an authorized user place a hold with reference and reason, and requires a reason when releasing it. Migration `20261002070702_V39PoeLegalHoldEvents` creates the tenant event ledger and restricted user/municipality/evidence relationships. Tests prove derived active-state behavior, append-only enforcement, UI placement/release payloads, and the exact release route.

Backend **87/87** and frontend **74/74** tests pass, along with TypeScript type-check, ESLint, host/frontend builds, and the **223,086-byte** idempotent migration script. Legal-hold governance is implemented. Evidence compliance remains **PARTIALLY COMPLIANT** pending retention-disposal execution/evidence, physical blob/association separation, durable object storage, and production scanner provisioning.

### 11.27 Governed POE retention disposal

Expired POE is now disposed through an asynchronous append-only event stream rather than an unaudited delete endpoint. `PoeDisposalEvent` records a stable disposal identifier and event public ID, municipality and evidence identity, requested/completed/failed action, bounded approval reference and reason, actor, timestamp, correlation identifier, and processor detail. Unique evidence/disposal/action indexing prevents duplicate terminal evidence, all relationships use restricted deletion, and `ApplicationDbContext` rejects event updates or deletes.

`PoeDisposalPolicy` permits a request only when evidence is already inactive, its retention date has expired, no legal hold is active, the approval reference and reason are valid, and no pending or completed disposal exists. OPMS and IPMS routes require stable `*.DISPOSE` permission under the submission's effective record scope and compare the submitted evidence RowVersion before queueing the request. Existing internal-audit grants are additively mapped to the disposal action. The original hard-delete routes remain prohibited.

`PoeDisposalWorker` processes persisted requests under a serializable transaction and re-checks active state, retention eligibility, and legal holds immediately before touching storage. The storage adapter constrains paths to the private evidence root, treats already-absent content as idempotent success, and reports traversal or I/O failure without claiming deletion. Every outcome is appended as a completed or failed event, so a failed attempt remains visible and can be resubmitted under a new governed approval. The API exposes the full derived disposal history; inactive evidence no longer receives a download URL; and the shared evidence workspace shows retired/retention state, failure details, and capability-gated request controls.

Migration `20261002072020_V39PoeRetentionDisposal` creates the tenant-owned event ledger with restricted actor, municipality, and evidence relationships plus unique public/provenance indexes. Backend tests prove eligibility, legal-hold blocking, append-only enforcement, canonical-root protection, physical deletion, and idempotency. Frontend tests prove history rendering, retry controls, approval/reason/RowVersion payload construction, and the exact API route. Backend **89/89** and frontend **76/76** tests pass, along with TypeScript type-check, ESLint, host/frontend production builds, `git diff --check`, and the **225,655-byte** idempotent migration script.

Retention-disposal execution and evidence are implemented. POE storage remains **PARTIALLY COMPLIANT** pending physical blob/association separation, durable object-storage integration and health monitoring, plus production malware-scanner provisioning and rehearsal. Section 11.28 closes the physical blob/association gap.

### 11.28 Tenant-owned physical evidence blobs and governed associations

Physical storage is no longer embedded in a submission association. The new tenant-owned `EvidenceBlob` aggregate carries public identity, opaque storage key, MIME type and byte size, SHA-256, signature-verification result, malware verdict/quarantine state, scanner provider/reference/detail/time, physical-deletion state, creation time, and RowVersion. `PoeFile` now contains submission identity, display file name, retention/activity/replacement metadata, uploader, and a restricted blob foreign key. Governed `IdpDocument` similarly retains plan/version/category/title/version/approval/retention metadata while referencing the same physical model. Storage keys remain absent from response contracts; public blob identity and deletion state are exposed for traceability.

All POE and IDP upload, list, protected-download, and rescan paths now read physical metadata through `EvidenceBlob`. Assessment, replacement, RFI association, protected URL, quarantine, and signature policies evaluate the blob's current state. Tenant query filters and write guards cover blobs directly. POE disposal executes at the association layer and checks for any other undisposed POE or IDP association before deleting bytes; shared content is retained with an explicit completion detail, while final-reference deletion marks the blob without erasing its metadata.

Migration `20261002073125_V39EvidenceBlobAssociations` is deliberately data preserving. It creates `EvidenceBlobs`, adds nullable association keys, rejects unexpectedly long legacy storage paths, deterministically inserts one blob for every existing POE and IDP document, assigns every association, makes the keys required, and only then drops the duplicated physical columns. The rollback path restores those columns from the blob records before removing the relationships and blob table. Restricted foreign keys prevent association or blob cascades, and unique public plus tenant/hash indexes support traceability and integrity queries.

Automated tests prove tenant filtering for physical blobs, multiple governed associations referencing one blob without duplicated physical metadata, governed IDP response blob identity, and existing POE/IDP scan/release policies after the split. Model/snapshot validation reports no pending changes. The generated idempotent SQL confirms blob creation and backfill occur before legacy storage columns are dropped. Backend **90/90** and frontend **76/76** tests pass, with TypeScript type-check, ESLint, host/frontend production builds, and the **247,844-byte** idempotent migration script.

The physical blob/association split is implemented for both POE and governed IDP documents. At this increment, evidence storage remained **PARTIALLY COMPLIANT** pending a durable production object-storage provider and health check plus external malware-scanner provisioning. Section 11.29 closes the repository-side provider and health-monitoring gap while retaining live provisioning and rehearsal as production acceptance work.

### 11.29 Configurable evidence storage and readiness

All physical evidence operations now cross the single `IEvidenceBlobStorage` boundary. OPMS POE, IPMS POE, governed IDP documents, malware rescans, protected downloads, and retention disposal no longer construct filesystem paths or read/write/delete bytes directly. The development `FileSystemEvidenceBlobStorage` keeps content below a configurable private root and rejects rooted, drive-relative, traversal, and control-character keys. The production `HttpEvidenceBlobStorage` sends a normalized opaque object key and secret API key to a configured durable-storage gateway for bounded-time PUT, GET, and DELETE operations. Redirect following is disabled so storage credentials cannot be forwarded to a redirect target; missing configuration, transport failure, oversized responses, and non-success provider responses fail closed.

Upload routes persist evidence metadata only after the provider confirms the byte write. If the database write fails, they make a best-effort idempotent provider deletion so a failed transaction does not knowingly leave an orphaned object. Downloads distinguish an absent object from an unavailable provider, never disclose a storage key, and return bytes only after the existing active/signature/clean/quarantine/deletion authorization predicate succeeds. Rescans obtain bytes through the same provider. The disposal worker also resolves this interface, preserving its final-association and legal-hold checks while allowing the durable provider to execute the physical delete.

`EvidenceStorageHealthCheck` is part of readiness. The local provider performs a write/read/delete probe; the HTTP provider calls its authenticated health endpoint. Development explicitly selects `FileSystem`; production explicitly selects `Http`; any unknown provider value fails startup rather than silently choosing local disk. `.env.example` documents only placeholders for endpoint, health endpoint, API key, and timeout. Provider credentials remain external configuration and are never persisted with `EvidenceBlob` metadata.

Tests prove the HTTP write/read/delete/health contract, normalized opaque keys, traversal rejection without an outbound request, missing-configuration failure, local write/read/delete behavior, canonical-root protection, and idempotent deletion. A focused search confirms there is no remaining controller-level physical-file bypass. Backend **92/92** and frontend **76/76** tests pass, together with TypeScript type-check, ESLint, backend/frontend release builds, model/snapshot consistency, `git diff --check`, and the unchanged **247,844-byte** idempotent migration script.

The repository-side durable-storage adapter and health-monitoring gap is closed. Production acceptance still requires provisioning the selected object-store gateway and malware scanner, injecting secrets through the deployment secret provider, and rehearsing upload, authorized download, rescan, provider outage, retry, and governed disposal against those live services.

### 11.30 Governed workflow versions, retirement, and pinned instances

Workflow version lifecycle is now explicit and reconstructable. Creating a version requires a bounded governance reason, validates that its effective start follows the predecessor, preserves the predecessor row, and records both the new version and each supersession in the tenant audit ledger. A future-dated successor no longer disables the currently effective definition prematurely: the predecessor receives a non-destructive `EffectiveTo`, the successor is selected only after `EffectiveFrom`, and already-ended historical ranges are not rewritten. The administration UI distinguishes Effective, Scheduled, Superseded, and Retired versions.

`GET /api/v1/workflow/definitions/compare` compares only two versions in the same tenant, financial year, submission type, and stable workflow code. `WorkflowDefinitionComparer` reports added, removed, modified, and unchanged stages and identifies changes in sequence, action/permission codes, optional/bypass rules, segregation-of-duties rules, terminal/rejection behavior, rating assignment, and active state. The UI offers predecessor comparison and displays the authoritative field-level differences.

`POST /api/v1/workflow/definitions/{publicId}/retire` performs reasoned, RowVersion-protected retirement. It validates the selected tenant explicitly even when reading an inactive definition, rejects invalid time ranges and repeat retirement, persists an old/new audit entry in the same save, and never deletes the definition or its stages. The UI sends the current concurrency token and explains that existing workflow instances remain pinned.

The runtime pinning invariant was also corrected. `ConfigurableWorkflowService` now loads an existing instance and its referenced definition before considering the currently effective definition. Only a new instance selects the latest effective active version. As a result, activating or retiring a definition cannot redirect an in-flight submission to stages from another version. Automated tests reproduce the former cross-version transition risk and prove that the action advances to the next stage in the original definition; additional tests cover comparison semantics, tenant-bound API comparison, reasoned audited retirement, and frontend comparison/retirement transport.

Backend **96/96** and frontend **78/78** tests pass, together with TypeScript type-check, ESLint, backend/frontend release builds, model/snapshot consistency, and `git diff --check`. No schema change was required, so the idempotent migration script remains **247,844 bytes**. Workflow comparison and retirement are repository-complete; legacy mutable score/status reconciliation, production-database concurrency rehearsal, and full browser/accessibility verification remain acceptance work.

### 11.31 Tenant calendar, employee, and effective-dated placement administration

The placeholder period and employee routes are replaced by real tenant-master administration. `TenantCalendarAdministration.tsx` consumes `/api/v1/masters` to create system-scope financial years, activate them for the selected municipality, promote a municipality year with its `RowVersion`, and create canonical bounded reporting periods. The earlier client route mismatch was corrected from the nonexistent `/v1/tenant-masters/reporting-periods` path to the controller's versioned `/v1/masters/reporting-periods` contract. Every create/update surface is gated by the stable `FINANCIAL_YEAR` or `REPORTING_PERIOD` capability.

`TenantEmployeeAdministration.tsx` now lists tenant-owned municipal employees independently of login identities, optionally links an Identity account, and creates effective-dated department/unit placements through public identifiers. Placement history is retained. An active placement can be ended only through `PUT /api/v1/masters/employee-assignments/{publicId}/close`, which requires a 10-to-1000-character reason and the current `RowVersion`, validates the end date, soft-closes the assignment, and persists old/new state plus reason, tenant, actor, correlation, address, and user-agent evidence in the append-only audit ledger. Employee deactivation is rejected while an active placement remains, preventing orphaned organizational authority.

Row-version model configuration is now provider-aware: SQL Server continues to use native generated `rowversion`, while non-SQL relational/unit providers use caller-managed concurrency bytes. This permits interim SQLite relational verification without changing application code or weakening the SQL Server production model. The host declares a local-development User Secrets identifier, and the local connection is stored outside source control; no local SQL credential appears in application settings, frontend assets, source-controlled documentation, or test source.

Automated coverage proves calendar transport and `RowVersion` promotion, employee/public-ID placement creation, governed placement closure transport, retained history, reasoned audit persistence, active-placement deactivation protection, relational unique employee constraints, and tenant query-filter isolation. Backend **99/99** tests pass with the dedicated SQL Server test explicitly skipped, and frontend **82/82** tests pass across **21** files. TypeScript type-check, ESLint, the combined release build, model/snapshot consistency, the unchanged **247,844-byte** idempotent migration script, and `git diff --check` pass.

The SQL Server integration fixture is restricted in code to `localhost\\SQLEXPRESS` and `OPMS_IntegrationTests`; it creates the dedicated database if absent, applies the full EF migration chain, wraps deterministic test data in a rollback transaction, and verifies tenant isolation, relational uniqueness, and native stale-writer conflicts. On this run, Windows reported the installed `SQL Server (SQLEXPRESS)` service as stopped and denied both service-start attempts from the execution host. Consequently this module is repository-complete and SQLite-relationally verified, but native SQL Server migration/foreign-key/filtered-index/`rowversion` acceptance remains pending and is not represented as passed.

### 11.32 Governed tenant organization masters

The department/unit placeholder administration is replaced by `TenantOrganizationAdministration.tsx`, backed by `/api/v1/masters/departments`, `/units`, and `/positions`. Department, unit, and the new `Position` master expose public identifiers, effective dates, active state, and RowVersion. Position ownership is tenant-mandatory and relates to one department and an optional unit. The controller rejects a unit outside the selected department, tenant filters exclude cross-municipality records, tenant write protection covers positions, and tenant-relative unique position codes are database-enforced. All creates and updates require a bounded governance reason; updates compare RowVersion and append old/new/reason/actor/correlation/request evidence to the tenant audit ledger. Deactivation is rejected while an active employee placement references the affected department, unit, or position.

`EmployeeAssignment` now optionally references the governed position master while retaining `PositionCode` and `PositionName` snapshots so later establishment edits cannot rewrite historical attribution. New UI-created placements must select a position valid for their exact department/unit relationship; the server derives the snapshots rather than trusting client-entered position text. Nullable linkage preserves pre-migration placement history non-destructively. `V39GovernedOrganizationMasters` creates the position table, relationship, tenant uniqueness, effective-date check, and SQL Server-native RowVersion. Legacy integer department/unit POST, PUT, and DELETE routes now return HTTP 410, eliminating the mutation bypass; existing lookup GET contracts remain available during client migration. Dynamic navigation adds the Positions page and the common security registry supplies `POSITION` CRUD permissions without role-name checks.

Automated tests prove reasoned/audited position creation, department/unit relationship validation, governed-position placement snapshots, retired legacy mutations, SQLite relational uniqueness, tenant query isolation, frontend RowVersion transport, and versioned route use. Backend **104/104** tests pass with one SQL Server integration test explicitly skipped; frontend **84/84** tests pass across **22** files. TypeScript type-check, ESLint, backend/frontend release builds, model/snapshot consistency, and the **251,170-byte** idempotent SQL migration script pass. `git diff --check` and secret scanning are recorded after final staging. Phase 1 remains **PARTIALLY COMPLIANT** because nullable tenant keys on legacy department/unit rows still require controlled reconciliation, the full HTTP-host concurrency path is not yet exercised, and native SQL Server acceptance remains pending.

### 11.33 Governed authentication sessions

`RefreshToken` now represents a governed session-token generation rather than an isolated bearer digest. Each row carries a public identifier, stable session-family identifier, created/last-used network evidence, bounded user-agent evidence, idle activity, absolute expiry, security-stamp snapshot, revocation reason, and RowVersion. Tokens remain one-way SHA-256 digests. `JwtService` rotates refresh tokens inside the original family and preserves its absolute deadline; it enforces configurable idle, absolute, and concurrent-session limits, revokes the oldest family when the configured cap is reached, and provides user-bound list/revoke/revoke-all operations. The refresh path rejects revoked, idle, absolute-expired, account-disabled, or security-stamp-stale sessions.

Every new access JWT includes `sid` and `security_stamp`. `JwtBearerEvents.OnTokenValidated` fails authenticated requests when the user is absent/disabled, the current Identity security stamp differs, or the backing session family is absent, revoked, idle, or expired. This closes the former direct-API window in which a logout, account disable, or credential security-stamp change could leave an otherwise valid access token usable until expiry. Activity is refreshed at a bounded one-minute cadence to avoid a write per request. Normal logout revokes the complete current family even if the refresh cookie is unavailable; logout-all revokes every active generation.

Versioned `GET /api/v1/auth/sessions`, per-session revoke, and revoke-all endpoints operate only on the authenticated user's sessions and require a bounded reason for revocation. The Security Settings screen no longer displays a fabricated Chrome session: it loads current server evidence, distinguishes the current session, exposes per-device revoke and sign-out-all, and clears local authentication after the current family is revoked. The migration uses a 128-character transition column so both 64-character digests and legacy 88-character raw values can migrate without truncation; it assigns unique public/session identifiers and deliberately revokes every pre-governance session with `Session migration requires reauthentication`, forcing a safe one-time sign-in instead of pretending old access tokens belong to a governed family.

Automated tests prove hashed bounded session creation, JWT family/security-stamp claims, immediate family revocation, concurrent-session eviction, SQLite unique digest enforcement, real Settings rendering/revocation, and versioned route transport. Backend **108/108** tests passed with one SQL Server integration test explicitly skipped; frontend **87/87** tests passed across **23** files. TypeScript type-check, ESLint, backend/frontend release builds, model/snapshot consistency, the legacy-session backfill marker, and the **255,887-byte** idempotent migration script passed at this increment. Authentication remained **PARTIALLY COMPLIANT** pending the MFA increment in section 11.34, configurable Entra ID/Active Directory/hybrid providers, password-change/reset UX with explicit security-stamp rotation, compromised-password screening, cookie-only/BFF access-token handling, and production distributed-session rehearsal.

### 11.34 Permission-driven privileged-user MFA

ASP.NET Core Identity authenticator tokens and recovery codes now provide the second factor; no role name is treated as privileged. `JwtSettings.MfaRequiredPermissionCodes` contains stable dynamic-security actions, and `MfaRequirementPolicy` compares them with current effective permissions. JWTs flag unenrolled privileged users, while `MfaEnrollmentMiddleware` also re-evaluates current database permissions on every authenticated API request. A newly assigned privileged permission therefore restricts the user without recompilation or waiting for token refresh. Direct API calls return 403 `MFA_ENROLLMENT_REQUIRED`; only versioned MFA enrollment, profile, and logout routes remain available until enrollment completes.

Password validation now returns a distinct MFA challenge. Login accepts either a normalized authenticator code or a one-time recovery code, records failed second-factor attempts, and does not issue tokens until verification succeeds. Versioned status, setup, enable, and disable endpoints use Identity's per-user authenticator key and recovery-code stores. Setup returns an interoperable `otpauth` URI and formatted shared key. Enabling generates ten recovery codes; enabling or disabling updates the Identity security stamp, revokes every governed session family, removes the refresh cookie, and writes an MFA audit event without persisting shared keys, submitted codes, passwords, or recovery codes in the audit trail.

The login UI presents an explicit second-factor stage and supports recovery-code sign-in. Security Settings supports authenticator setup, one-time recovery-code display, status/recovery-count inspection, and password-plus-code disable. Unenrolled privileged users are directed to this screen, which explains why the rest of the API is restricted. Misleading remember-me, forgot-password, and fake MFA controls were removed rather than presenting unavailable security functions.

Automated tests prove permission-driven enrollment without role-name logic, JWT enrollment claims, direct-API blocking with enrollment endpoint allowance, the authenticator challenge client flow, recovery-code presentation, and exact versioned route transport. Backend **110/110** tests passed with one SQL Server integration test explicitly skipped; frontend **90/90** tests passed across **24** files. TypeScript type-check, ESLint, host/frontend builds, and `git diff --check` passed at this increment. No schema migration was required because ASP.NET Core Identity already persists `TwoFactorEnabled`, authenticator keys, and recovery codes in provider-neutral Identity tables. Authentication remained **PARTIALLY COMPLIANT** pending the enforced password-change increment in section 11.35, configurable Entra ID/Active Directory/hybrid providers, password reset, compromised-password screening, cookie-only/BFF access-token handling, recovery-code regeneration, and production distributed-session/MFA rehearsal.

### 11.35 Enforced password change and session invalidation

`ApplicationUser.MustChangePassword` is now an enforced security state rather than a display-only flag. New JWTs carry `password_change_required`, and `PasswordChangeMiddleware` also reads the current Identity record so an administrator-applied requirement affects an existing session without recompilation or token refresh. Authenticated users in that state receive 403 `PASSWORD_CHANGE_REQUIRED` for direct application APIs and may access only the versioned password-change route, their profile, and logout. Password remediation intentionally precedes privileged MFA enrollment when both are required.

`POST /api/v1/auth/password/change` uses `UserManager.ChangePasswordAsync`, so the configured 12-character complexity and password validators remain authoritative. A successful change clears the forced-change state, explicitly rotates the Identity security stamp, revokes every governed session family, removes the refresh cookie, and appends a `UserAuthentication/PasswordChange` audit event without logging either password. The operations execute in one relational transaction and require the current password; validation failures return Identity's safe validation descriptions without partially clearing the flag.

The login context detects `MustChangePassword` and routes the user directly to Security Settings. The former disabled password mock is replaced with controlled current/new/confirmation fields, local confirmation matching, the configured policy summary, server validation display, and sign-out after success. Exact versioned route transport is covered, and the middleware test proves both direct-API denial and password-endpoint allowance.

Backend **111/111** tests pass with one SQL Server integration test explicitly skipped; frontend **92/92** tests pass across **24** files. TypeScript type-check, ESLint, host/frontend builds, and `git diff --check` pass. No provider-specific schema or code was introduced: Identity's existing user fields, SQLite interim verification, and SQL Server deployment path are unchanged. Authentication remains **PARTIALLY COMPLIANT** pending password-reset delivery, compromised-password screening, configurable Entra ID/Active Directory/hybrid providers, cookie-only/BFF access-token handling, and production distributed-session rehearsal.

### 11.36 Relational OPMS ward, assignee, and vote mappings

OPMS targets no longer author or read comma-delimited ward, additional-assignee, or vote-number columns. `OpmsTargetWard`, `OpmsTargetAdditionalAssignee`, and `OpmsTargetVoteNumber` are tenant-owned junction entities with public identifiers, restricted target/tenant/reference relationships, target/reference unique indexes, and mandatory tenant write/query enforcement. The original columns remain mapped as explicitly named `Legacy*` properties solely so historical source values are preserved; current application code never mutates them.

The OPMS save contract now accepts typed, deduplicated arrays. Before any write, the server bounds each collection and verifies that wards are active and match the selected municipality's code/name, vote numbers are active and belong to one of its departments, and additional assignees are active and have either direct tenant ownership or a currently effective tenant role assignment. Create and update replace the authoritative junction set within the target write, while response contracts are composed from those junctions. Reads eagerly load all three relationships. The SPA uses the versioned `/api/v1/opms-targets` route, serializes its collection editors as arrays instead of CSV, and retains raw returned identifiers even when a local display lookup is incomplete rather than silently replacing them with mock relationships.

Migration `20261002093105_V39RelationalOpmsTargetMappings` creates all three normalized tables. Its SQL Server backfill parses the retained legacy strings with `STRING_SPLIT`, discards invalid or cross-tenant references, deduplicates valid relationships before unique indexes are created, and assigns new public IDs without changing the legacy columns. New-database SQLite verification uses the same EF model and proves foreign-key uniqueness plus tenant query isolation; production deployment continues to use the SQL Server provider without application-code changes.

Backend **112/112** tests pass with one SQL Server integration test explicitly skipped; frontend **94/94** tests pass across **24** files. TypeScript type-check, ESLint, host/frontend builds, model/snapshot consistency, `git diff --check`, and the **264,910-byte** idempotent SQL Server migration script pass. R-15 relational KPI mappings are repository-complete for these three OPMS relationships. The ward and vote-number lifecycle gap identified at this increment is closed in section 11.37; native SQL Server migration execution remains an environment acceptance item.

### 11.37 Governed ward and vote-number masters

`Ward` and `VoteNumber` are now tenant-rooted masters rather than global lookups. Both carry mandatory `MunicipalityId`, `PublicId`, effective dates, soft-active state and concurrency tokens; their codes are unique per municipality and query filters plus the central write guard prevent cross-tenant access. Vote Number also requires a governed department. The retained Ward municipality text column is explicitly mapped as `LegacyMunicipality` for historical evidence only. No hard-delete contract is exposed.

`OrganizationMastersController` provides stable `/api/v1/masters/wards` and `/api/v1/masters/vote-numbers` GET/POST/PUT contracts with dynamic `WARD.*` and `VOTE_NUMBER.*` policies. Writes normalize codes, validate dates/amounts/department ownership, require governance reasons, audit old/new state, demand RowVersion on update, and reject deactivation while OPMS or IDP references remain. The security registry seeds both resources and their navigation entries additively. The former mock-only Vote Numbers route and the new Wards route now use a capability-gated governed register; OPMS target capture loads active relational IDs from these APIs instead of mock reference lists.

Migration `20261002094321_V39GovernedWardAndVoteMasters` adds the tenant keys, public identifiers, effective dates, RowVersion columns, tenant uniqueness and restricted municipality foreign keys. Its SQL Server backfill resolves each legacy ward only when its municipality text has one unambiguous code/name match, derives each vote tenant from its department, generates identifiers, preserves legacy text, and deliberately aborts when reconciliation is unresolved instead of silently assigning records to the wrong municipality. The application model remains provider-neutral: SQL Server retains native generated `rowversion`, while SQLite advances concurrency bytes before non-SQL writes without changing deployment code.

Backend **114/114** tests pass with one SQL Server integration test explicitly skipped; frontend **96/96** tests pass across **25** files. SQLite verification proves relational uniqueness, tenant isolation and stale-writer rejection. TypeScript type-check, ESLint, host/frontend build, model/snapshot consistency, and the **271,968-byte** idempotent SQL Server migration script pass. Native SQL Server execution remains pending solely as an environment acceptance gate.

### 11.38 Cookie-only browser access sessions

The SPA no longer receives, persists, reads, or sends an access JWT. Login and refresh response contracts contain only the session expiry and authorized profile/capability projection; both access and refresh secrets are issued as `HttpOnly`, `SameSite=Strict`, essential cookies. The short-lived access cookie is scoped to `/api`, while the refresh cookie is scoped to `/api/auth`. Production cookies require HTTPS. Cookie deletion now repeats the original path and security policy, so logout, current/all-session revocation, password change, and MFA state changes reliably expire both values rather than attempting to delete a differently scoped cookie.

`JwtBearerEvents.OnMessageReceived` accepts the access cookie for browser calls. An explicit Bearer header retains precedence, preserving Swagger and non-browser API compatibility without creating a second authorization model. Refresh no longer accepts an expired access token in its request body: it resolves the user and session family from the hashed refresh-cookie value, applies the existing account, revocation, idle, absolute-expiry, and security-stamp rules, then rotates the family transactionally. The browser sends credentialed requests and retains only a non-secret tab marker for routing. Concurrent 401 responses share one in-flight refresh operation, preventing avoidable same-tab rotation races. Legacy `auth_token` storage is removed on module load and during every authentication transition.

Backend regression tests prove that production cookie policy is HttpOnly, Secure, Strict, essential, and correctly path-scoped, and that the login projection cannot serialize access or refresh token properties. Frontend tests prove that login stores no token, protected calls send no Bearer header, refresh sends no token body, credential cookies are included, and simultaneous failures coalesce to one rotation. Backend **116/116** tests pass with one SQL Server integration test explicitly skipped; frontend **98/98** tests pass across **26** files. TypeScript type-check, ESLint, host/frontend production builds, and `git diff --check` pass. No schema migration or provider-specific persistence code was required, so SQLite interim verification and SQL Server deployment use the same application implementation; the idempotent SQL Server migration script remains **271,968 bytes**.

Session/token storage is repository-complete for the current local-identity architecture. At that increment, authentication remained **PARTIALLY COMPLIANT** pending password-reset delivery, compromised-password screening, configurable Entra ID/Active Directory/hybrid providers, production distributed-session rehearsal, and browser penetration/CSRF acceptance. Section 11.39 closes the password-reset repository gap.

### 11.39 Enumeration-safe one-time password reset

The public password-reset request and completion contracts are implemented at `/api/v1/auth/password/forgot` and `/api/v1/auth/password/reset` under the existing authentication rate limit. Request responses are deliberately identical for missing, inactive, unconfirmed, and valid accounts. Identity generates a bounded-lifetime, single-use data-protection token only for an eligible user. `PasswordResetNotifier` delivers the percent-encoded link directly through the configured email adapter with a non-secret idempotency key. Email and token are carried in the link fragment, which browsers do not send to the web server or access logs; the token is never placed in the workflow outbox, application database, audit values, or API response. Production rejects a non-HTTPS reset origin, and the configured token lifetime is also applied to Identity's data-protection token provider.

Successful reset executes inside a relational transaction, applies the same Identity password validators as registration/change, clears `MustChangePassword`, rotates the security stamp, revokes every governed session family, clears any browser session cookies, and appends `PasswordResetCompleted` authentication audit evidence without the password or token. Invalid, expired, and already-consumed links return a common error. A request audit stores only delivery success/provider metadata. The unauthenticated login surface now provides request and reset forms, confirmation matching, policy guidance, generic account-discovery text, server validation messages, and safe return to sign-in. Email, token, and password travel only in POST bodies; no secret is added to an API URL.

SQLite controller integration proves equal known/unknown responses, eligible-user delivery, token-free request audit, real Identity password replacement, forced-change clearing, security-stamp rotation, governed-session revocation, and completion audit. Delivery tests prove exact encoding for tokens containing reserved characters and production HTTPS enforcement. Frontend tests prove the generic request flow, link consumption, success return, and versioned POST transport. Backend **119/119** tests pass with one SQL Server integration test explicitly skipped; frontend **101/101** tests pass across **26** files. TypeScript type-check, ESLint, backend/frontend production builds, and `git diff --check` pass. No schema migration or provider-specific application code was introduced; the SQL Server migration script remains **271,968 bytes**.

Password-reset delivery is repository-complete and requires only the already-documented production email provider provisioning. At that increment, authentication remained **PARTIALLY COMPLIANT** pending compromised-password screening, configurable Entra ID/Active Directory/hybrid providers, authentication-policy administration, production distributed-session rehearsal, and browser penetration/CSRF acceptance. Section 11.40 closes the compromised-password repository gap.

### 11.40 Compromised-password screening

`CompromisedPasswordValidator` is registered in the ASP.NET Core Identity validator pipeline, so administrator-created accounts, self-service password change, forced password remediation, seed-controlled password replacement, and one-time password reset cannot bypass the same rule. A deterministic local denylist always rejects common/default OPMS and administrative passwords, including when external screening is intentionally disabled for offline development. The login and settings surfaces state that common or breached values are rejected rather than presenting complexity alone as sufficient security.

Production enables a fail-closed `PwnedPasswordLookup`. It computes SHA-1 locally, sends only the first five hexadecimal characters to the configured HTTPS range endpoint, requests response padding, supplies the provider-required user agent, bounds the response size and timeout, ignores zero-count padding entries, and compares the remaining suffix locally. Neither the password nor its complete hash leaves the application. Successful prefix ranges are cached briefly without password/user association. The implementation follows the provider's documented [Pwned Passwords k-anonymity range protocol](https://haveibeenpwned.com/API/v3). Any provider failure blocks password creation/change/reset with a safe retry message in production. `CompromisedPasswordHealthCheck` also makes readiness unhealthy when fail-closed screening cannot reach the range service and reports degraded configuration if screening is enabled without fail-closed behavior.

Automated tests prove that only the five-character prefix is transmitted, the full hash and password are absent from the request, padding and user-agent headers are present, padded rows are ignored, occurrence thresholds are enforced, prefix results are cached, local common values are rejected without a network call, remote breach matches are rejected, fail-closed outages reject password writes, disabled development screening retains local protection, and production readiness fails on provider outage. Backend **123/123** tests pass with one SQL Server integration test explicitly skipped; frontend **101/101** tests pass across **26** files. TypeScript type-check, ESLint, backend/frontend production builds, and `git diff --check` pass. No schema migration or database-provider-specific application code was introduced; the SQL Server migration script remains **271,968 bytes**.

Compromised-password screening is repository-complete for local Identity. Authentication remains **PARTIALLY COMPLIANT** pending configurable Entra ID/Active Directory/hybrid providers, administrator-facing authentication-policy governance, production distributed-session rehearsal, and browser penetration/CSRF acceptance.
