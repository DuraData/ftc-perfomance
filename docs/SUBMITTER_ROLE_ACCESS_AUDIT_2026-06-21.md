# Submitter Role Access Audit

Date: 2026-06-21
Scope: Submitter / Performance Owner permission, navigation, route and action controls.

| Area | Should View | Should Create | Should Edit | Should Delete | Implemented | Notes |
|---|---|---|---|---|---|---|
| IDP Dashboard | Yes | No | No | No | Yes | Submitter menu and route access added for read-only dashboard. |
| IDP Strategic Objectives | Yes | No | No | No | Yes | IDP routes exposed as read-only with action buttons hidden when manage permissions are missing. |
| IDP Projects | Yes | No | No | No | Yes | Mapped to read-only IDP hierarchy views for submitter navigation. |
| IDP KPIs | Yes | No | No | No | Yes | Mapped to read-only IDP hierarchy views for submitter navigation. |
| OPMS Targets | Yes | No | No | No | Yes | Target create/edit/delete/copy actions now permission-gated; read-only badge shown. |
| OPMS Submissions | Yes | Yes | Yes | Yes (own submission lifecycle) | Yes | Submitter role now has OPMS submission create/edit/delete/submit and evidence upload permissions. |
| IPMS Targets | Yes | No | No | No | Yes | Target create/edit/delete actions now permission-gated; read-only badge shown. |
| IPMS Submissions | Yes | Yes | Yes | Yes (own submission lifecycle) | Yes | Submitter role now has IPMS submission create/edit/delete/submit and evidence upload permissions. |
| Workflow Queues | Yes (own only) | No | No | No | Yes | My queue now loads API submissions and filters to logged-in submitter; queue route mapping added. |
| Reports | Yes (filtered) | No | No | No | Yes | Submitter report catalogue reduced to personal and IDP summary report set. |
| Notifications | Yes | No | No | No | Yes | Submitter menu contains Notifications; route guarded by notification permission. |
| Admin | No | No | No | No | Yes | Route guard added for /admin/* and system administration routes. |
| HR | No | No | No | No | Yes | /hr/* routes require HR/admin permissions not granted to submitter. |
| Settings | No (except notification/profile views) | No | No | No | Yes | Submitter menu uses dedicated Notifications/My Profile entries and no admin settings access. |

## Notes

- Submitter role permission map in backend was updated to remove IDP definition/document management and target-definition management capabilities.
- Submitter role can now view IDP dashboards/alignment/report context and manage own OPMS/IPMS submission lifecycle.
- Navigation is now submitter-specific, with restricted admin/setup modules removed.
- Route-level restrictions now explicitly handle create/edit URLs and admin-style paths.
