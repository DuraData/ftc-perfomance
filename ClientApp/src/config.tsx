import type {
  AppRole,
  DashboardModel,
  EntityKey,
  FormFieldConfig,
  ModuleRoute,
  RoleDefinition,
  SidebarGroup,
  TargetUnitType,
} from "./types";

const unitTypeOptions: TargetUnitType[] = [
  "Percentage",
  "Absolute Count",
  "Financial",
  "Area Based",
  "Volume Based",
  "Index Scores",
  "Ratios",
  "Time Based",
  "Binary",
  "Date",
  "Readiness Scale",
  "Qualitative",
  "Zero Based",
  "Reverse Cumulative",
  "Reverse Non-Cumulative",
  "Binary Determination",
];

export const roles: RoleDefinition[] = [
  {
    id: "sysadmin",
    label: "System Administrator",
    shortLabel: "Sys Admin",
    description: "Full administrative visibility and CRUD access.",
    dashboardSummary: "Platform operations, setup and audit readiness.",
    queueFocus: "Administration and escalations",
  },
  {
    id: "performance-manager",
    label: "Performance Manager",
    shortLabel: "PMS Officer",
    description: "Manages OPMS, workflow progression and executive reporting.",
    dashboardSummary: "Organisation performance, verification and reporting.",
    queueFocus: "PMS review and escalations",
  },
  {
    id: "department-manager",
    label: "Department Manager",
    shortLabel: "Dept Manager",
    description: "Owns departmental targets, submissions and staffing records.",
    dashboardSummary: "Department delivery and overdue follow-up.",
    queueFocus: "Department approvals and risk items",
  },
  {
    id: "submitter",
    label: "Submitter",
    shortLabel: "Submitter",
    description: "Captures actuals and submits performance evidence.",
    dashboardSummary: "Drafts, due dates and submission completion.",
    queueFocus: "My work queue and returned items",
  },
  {
    id: "verifier",
    label: "Verifier",
    shortLabel: "Verifier",
    description: "Reviews, verifies and returns incomplete submissions.",
    dashboardSummary: "Verification queue and evidence quality.",
    queueFocus: "Verification",
  },
  {
    id: "approver",
    label: "Approver",
    shortLabel: "Approver",
    description: "Approves or rejects verified submissions.",
    dashboardSummary: "Final sign-off workload and turnaround.",
    queueFocus: "Approval",
  },
  {
    id: "auditor",
    label: "Auditor",
    shortLabel: "Auditor",
    description: "Records audit findings and reviews history.",
    dashboardSummary: "Audit findings, exceptions and evidence completeness.",
    queueFocus: "Auditor review",
  },
  {
    id: "hr-admin",
    label: "HR Administrator",
    shortLabel: "HR Admin",
    description: "Maintains employees, positions, resumes and assignments.",
    dashboardSummary: "People data integrity and staffing records.",
    queueFocus: "HR maintenance",
  },
  {
    id: "executive-viewer",
    label: "Executive Viewer",
    shortLabel: "Executive",
    description: "Read-focused dashboards and reports for leadership.",
    dashboardSummary: "Organisation-wide performance at a glance.",
    queueFocus: "Reports and high-level completion",
  },
];

const module = (
  key: string,
  label: string,
  path: string,
  icon: string,
  category: string,
  entityKey?: EntityKey,
  exportable = true,
  hasChildCrud = false,
): ModuleRoute => ({
  key,
  label,
  path,
  icon,
  category,
  entityKey,
  exportable,
  hasChildCrud,
});

export const sidebarGroups: SidebarGroup[] = [
  {
    label: "Core",
    items: [module("dashboard", "Dashboard", "/app/dashboard", "dashboard", "Core")],
  },
  {
    label: "OPMS",
    items: [
      module("opms-home", "OPMS", "/app/opms", "view_quilt", "OPMS"),
      module("opms-targets", "OPMS Targets", "/app/opms/targets", "track_changes", "OPMS", "opmsTargets", true, true),
      module("opms-submissions", "OPMS Submissions", "/app/opms/submissions", "upload_file", "OPMS", "opmsSubmissions", true, true),
      module("vote-numbers", "Vote Numbers", "/app/opms/vote-numbers", "sell", "OPMS", "voteNumbers"),
    ],
  },
  {
    label: "IPMS",
    items: [
      module("ipms-home", "IPMS", "/app/ipms", "view_quilt", "IPMS"),
      module("ipms-targets", "IPMS Targets", "/app/ipms/targets", "adjust", "IPMS", "ipmsTargets", true, true),
      module("ipms-submissions", "IPMS Submissions", "/app/ipms/submissions", "assignment_turned_in", "IPMS", "ipmsSubmissions", true, true),
    ],
  },
  {
    label: "Library",
    items: [module("kpi-library", "KPI Library", "/app/kpi-library", "library_books", "Library", "kpiLibrary")],
  },
  {
    label: "Workflow",
    items: [
      module("workflow-home", "Workflow", "/app/workflow", "alt_route", "Workflow"),
      module("my-work-queue", "My Work Queue", "/app/workflow/my-work-queue", "work_history", "Workflow"),
      module("verification", "Verification", "/app/workflow/verification", "verified", "Workflow"),
      module("approval", "Approval", "/app/workflow/approval", "task_alt", "Workflow"),
      module("pms-review", "PMS Review", "/app/workflow/pms-review", "rule", "Workflow"),
      module("auditor-review", "Auditor Review", "/app/workflow/auditor-review", "gavel", "Workflow"),
      module("returned-for-information", "Returned for Information", "/app/workflow/returned-for-information", "assignment_return", "Workflow"),
      module("completed-submissions", "Completed Submissions", "/app/workflow/completed-submissions", "inventory", "Workflow"),
      module("overdue-items", "Overdue Items", "/app/workflow/overdue-items", "warning", "Workflow"),
    ],
  },
  {
    label: "Human Resources",
    items: [
      module("hr-home", "Human Resources", "/app/human-resources", "groups", "Human Resources"),
      module("employees", "Employees", "/app/human-resources/employees", "badge", "Human Resources", "employees"),
      module("departments", "Departments", "/app/human-resources/departments", "account_tree", "Human Resources", "departments", true, true),
      module("department-units", "Department Units", "/app/human-resources/department-units", "hub", "Human Resources", "departmentUnits"),
      module("positions", "Positions", "/app/human-resources/positions", "work", "Human Resources", "positions"),
      module("occupations", "Occupations", "/app/human-resources/occupations", "category", "Human Resources", "occupations"),
      module("contacts", "Contacts", "/app/human-resources/contacts", "contacts", "Human Resources", "contacts"),
      module("resumes", "Resumes", "/app/human-resources/resumes", "description", "Human Resources", "resumes", true, true),
      module("portfolio-files", "Portfolio Files", "/app/human-resources/portfolio-files", "folder", "Human Resources", "portfolioFiles"),
      module("tasks", "Tasks", "/app/human-resources/tasks", "checklist", "Human Resources", "tasks"),
    ],
  },
  {
    label: "Administration",
    items: [
      module("admin-home", "Administration", "/app/administration", "admin_panel_settings", "Administration"),
      module("periods", "Periods", "/app/administration/periods", "calendar_month", "Administration", "periods"),
      module("organisations", "Organisations", "/app/administration/organisations", "business", "Administration", "organisations"),
      module("industries", "Industries", "/app/administration/industries", "apartment", "Administration", "industries"),
      module("approval-setup", "Approval Setup", "/app/administration/approval-setup", "person_check", "Administration", "approvalSetup"),
      module("budget-sources", "Budget Sources", "/app/administration/budget-sources", "payments", "Administration", "budgetSources"),
      module("budget-types", "Budget Types", "/app/administration/budget-types", "request_quote", "Administration", "budgetTypes"),
      module("strategic-goals", "Strategic Goals", "/app/administration/strategic-goals", "flag", "Administration", "strategicGoals"),
      module("strategic-objectives", "Strategic Objectives", "/app/administration/strategic-objectives", "track_changes", "Administration", "strategicObjectives"),
      module("units-of-measure", "Unit of Measure", "/app/administration/unit-of-measure", "straighten", "Administration", "unitsOfMeasure"),
      module("kpas", "KPAs", "/app/administration/kpas", "fact_check", "Administration", "kpas"),
      module("municipal-kpas", "Municipal KPAs", "/app/administration/municipal-kpas", "location_city", "Administration", "municipalKpas"),
      module("departmental-objectives", "Departmental Objectives", "/app/administration/departmental-objectives", "assignment", "Administration", "departmentalObjectives"),
      module("outputs", "Outputs", "/app/administration/outputs", "output", "Administration", "outputs"),
      module("performance-objectives", "Performance Objectives", "/app/administration/performance-objectives", "target", "Administration", "performanceObjectives"),
      module("priority-issues", "Priority Issues", "/app/administration/priority-issues", "priority_high", "Administration", "priorityIssues"),
    ],
  },
  {
    label: "Location Setup",
    items: [
      module("location-home", "Location Setup", "/app/location-setup", "place", "Location Setup"),
      module("countries", "Countries", "/app/location-setup/countries", "public", "Location Setup", "countries"),
      module("provinces", "Provinces", "/app/location-setup/provinces", "map", "Location Setup", "provinces"),
      module("cities", "Cities", "/app/location-setup/cities", "location_city", "Location Setup", "cities"),
      module("suburbs", "Suburbs", "/app/location-setup/suburbs", "pin_drop", "Location Setup", "suburbs"),
      module("addresses", "Addresses", "/app/location-setup/addresses", "home_pin", "Location Setup", "addresses"),
    ],
  },
  {
    label: "Reporting",
    items: [
      module("reports", "Reports", "/app/reports", "monitoring", "Reporting", "reports"),
      module("crud-audit", "CRUD Coverage Audit", "/app/crud-coverage-audit", "check_circle", "Reporting"),
      module("settings", "Settings", "/app/settings", "settings", "Reporting", undefined, false),
    ],
  },
];

export const reportRoutes = [
  { label: "KPI Performance Summary", path: "/app/reports/kpi-performance-summary" },
  { label: "Department Performance", path: "/app/reports/department-performance" },
  { label: "Quarterly Submission Status", path: "/app/reports/quarterly-submission-status" },
  { label: "Overdue Submissions", path: "/app/reports/overdue-submissions" },
  { label: "Approval Turnaround", path: "/app/reports/approval-turnaround" },
  { label: "Variance Report", path: "/app/reports/variance-report" },
  { label: "Audit Findings", path: "/app/reports/audit-findings" },
  { label: "Annual Performance Report", path: "/app/reports/annual-performance-report" },
  { label: "OPMS/IPMS Alignment Report", path: "/app/reports/opms-ipms-alignment-report" },
];

const baseFields: FormFieldConfig[] = [
  { key: "name", label: "Name", required: true },
  { key: "code", label: "Code", required: true },
  { key: "status", label: "Status", required: true },
  { key: "description", label: "Description", type: "textarea" },
];

export const entityFieldMap: Partial<Record<EntityKey, FormFieldConfig[]>> = {
  voteNumbers: [
    ...baseFields,
    { key: "voteCode", label: "Vote Code", required: true },
    { key: "budgetSource", label: "Budget Source", required: true },
    { key: "budgetType", label: "Budget Type", required: true },
    { key: "amount", label: "Amount", type: "number", required: true },
    { key: "owner", label: "Owner", required: true },
  ],
  kpiLibrary: [
    ...baseFields,
    { key: "templateCode", label: "Template Code", required: true },
    { key: "category", label: "Category", required: true },
    { key: "kpa", label: "KPA", required: true },
    {
      key: "targetUnitType",
      label: "Dynamic Target Unit",
      type: "select",
      required: true,
      options: unitTypeOptions.map((value) => ({ label: value, value })),
    },
    { key: "defaultAnnualTarget", label: "Default Annual Target", required: true },
    { key: "department", label: "Department", required: true },
  ],
  employees: [
    ...baseFields,
    { key: "employeeNumber", label: "Employee Number", required: true },
    { key: "departmentName", label: "Department", required: true },
    { key: "unitName", label: "Department Unit", required: true },
    { key: "positionTitle", label: "Position", required: true },
    { key: "email", label: "Email", required: true },
    { key: "phone", label: "Phone", required: true },
  ],
  departments: [
    ...baseFields,
    { key: "manager", label: "Manager", required: true },
  ],
  departmentUnits: [
    ...baseFields,
    { key: "departmentName", label: "Department", required: true },
    { key: "lead", label: "Lead", required: true },
  ],
  positions: [
    ...baseFields,
    { key: "grade", label: "Grade", required: true },
    { key: "departmentId", label: "Department Id", required: true },
    { key: "unitId", label: "Unit Id", required: true },
    { key: "occupationId", label: "Occupation Id", required: true },
  ],
  occupations: [
    ...baseFields,
    { key: "family", label: "Family", required: true },
  ],
  contacts: [
    ...baseFields,
    { key: "email", label: "Email", required: true },
    { key: "phone", label: "Phone", required: true },
    { key: "contactType", label: "Contact Type", required: true },
  ],
  resumes: [
    ...baseFields,
    { key: "employeeName", label: "Employee", required: true },
    { key: "summary", label: "Summary", type: "textarea", required: true },
  ],
  portfolioFiles: [
    ...baseFields,
    { key: "resumeId", label: "Resume Id", required: true },
    { key: "fileName", label: "File Name", required: true },
    { key: "fileType", label: "File Type", required: true },
    { key: "sizeLabel", label: "Size", required: true },
  ],
  tasks: [
    ...baseFields,
    {
      key: "priority",
      label: "Priority",
      type: "select",
      required: true,
      options: [
        { label: "Low", value: "Low" },
        { label: "Medium", value: "Medium" },
        { label: "High", value: "High" },
      ],
    },
    { key: "assignedEmployees", label: "Assigned Employees", type: "textarea", required: true },
    { key: "estimatedWorkHours", label: "Estimated Work Hours", type: "number", required: true },
    { key: "actualWorkHours", label: "Actual Work Hours", type: "number", required: true },
    { key: "dueDate", label: "Due Date", type: "date", required: true },
    { key: "notes", label: "Notes", type: "textarea" },
  ],
  periods: [
    ...baseFields,
    { key: "financialYear", label: "Financial Year", required: true },
    { key: "startDate", label: "Start Date", type: "date", required: true },
    { key: "endDate", label: "End Date", type: "date", required: true },
  ],
  organisations: [
    ...baseFields,
    { key: "registrationNumber", label: "Registration Number", required: true },
    { key: "industryId", label: "Industry Id", required: true },
  ],
  approvalSetup: [
    ...baseFields,
    { key: "user", label: "User", required: true },
    { key: "userEmail", label: "User Email", required: true },
    { key: "approver", label: "Approver", required: true },
    { key: "approverEmail", label: "Approver Email", required: true },
    { key: "isAdminApprover", label: "Is Admin Approver", type: "checkbox" },
  ],
  countries: baseFields,
  provinces: [...baseFields, { key: "countryName", label: "Country", required: true }],
  cities: [...baseFields, { key: "provinceName", label: "Province", required: true }],
  suburbs: [...baseFields, { key: "cityName", label: "City", required: true }],
  addresses: [
    ...baseFields,
    { key: "countryId", label: "Country", required: true },
    { key: "provinceId", label: "Province", required: true },
    { key: "cityId", label: "City", required: true },
    { key: "suburbId", label: "Suburb", required: true },
    { key: "addressLine", label: "Address Line", required: true },
    { key: "postalCode", label: "Postal Code", required: true },
  ],
  industries: baseFields,
  budgetSources: baseFields,
  budgetTypes: baseFields,
  strategicGoals: baseFields,
  strategicObjectives: baseFields,
  unitsOfMeasure: baseFields,
  kpas: baseFields,
  municipalKpas: baseFields,
  departmentalObjectives: baseFields,
  outputs: baseFields,
  performanceObjectives: baseFields,
  priorityIssues: baseFields,
};

export const dashboardsByRole: Record<AppRole, DashboardModel> = {
  sysadmin: {
    stats: [
      { id: "s1", title: "Active Users", value: "126", delta: "+8", tone: "accent", icon: "groups" },
      { id: "s2", title: "Setup Records", value: "214", delta: "All synced", tone: "info", icon: "settings" },
      { id: "s3", title: "Open Escalations", value: "7", delta: "2 critical", tone: "danger", icon: "warning" },
      { id: "s4", title: "Audit Ready Modules", value: "36", delta: "100%", tone: "success", icon: "verified" },
    ],
    queues: [
      { id: "q1", title: "Approval Setup", count: "12", route: "/app/administration/approval-setup", tone: "accent" },
      { id: "q2", title: "CRUD Audit", count: "36/36", route: "/app/crud-coverage-audit", tone: "success" },
    ],
    highlights: [
      { label: "Admin Focus", value: "System stability and configuration" },
      { label: "Top Risk", value: "Overdue approval setup updates" },
    ],
  },
  "performance-manager": {
    stats: [
      { id: "p1", title: "OPMS Targets", value: "148", delta: "6 at risk", tone: "accent", icon: "track_changes" },
      { id: "p2", title: "PMS Reviews", value: "14", delta: "3 overdue", tone: "warning", icon: "rule" },
      { id: "p3", title: "Completed Submissions", value: "96", delta: "+11 this week", tone: "success", icon: "task_alt" },
      { id: "p4", title: "Audit Findings", value: "5", delta: "1 unresolved", tone: "danger", icon: "gavel" },
    ],
    queues: [
      { id: "q1", title: "PMS Review", count: "14", route: "/app/workflow/pms-review", tone: "accent" },
      { id: "q2", title: "Overdue Items", count: "23", route: "/app/workflow/overdue-items", tone: "danger" },
    ],
    highlights: [
      { label: "Executive Summary", value: "72% KPI completion this quarter" },
      { label: "Queue Focus", value: "Verification to approval handoff" },
    ],
  },
  "department-manager": {
    stats: [
      { id: "d1", title: "Department Targets", value: "32", delta: "4 at risk", tone: "accent", icon: "apartment" },
      { id: "d2", title: "Draft Submissions", value: "9", delta: "Due this week", tone: "warning", icon: "edit_note" },
      { id: "d3", title: "Employees", value: "84", delta: "2 vacancies", tone: "info", icon: "badge" },
      { id: "d4", title: "Approved", value: "26", delta: "+3", tone: "success", icon: "check_circle" },
    ],
    queues: [
      { id: "q1", title: "My Work Queue", count: "9", route: "/app/workflow/my-work-queue", tone: "accent" },
      { id: "q2", title: "Approval", count: "5", route: "/app/workflow/approval", tone: "warning" },
    ],
    highlights: [
      { label: "Department Focus", value: "Close Q3 submissions and HR cleanup" },
      { label: "Risk", value: "Late evidence on finance-linked KPIs" },
    ],
  },
  submitter: {
    stats: [
      { id: "sub1", title: "Drafts", value: "11", delta: "4 due soon", tone: "warning", icon: "draft" },
      { id: "sub2", title: "Returned for Info", value: "3", delta: "Action needed", tone: "danger", icon: "assignment_return" },
      { id: "sub3", title: "Submitted", value: "18", delta: "This quarter", tone: "success", icon: "send" },
      { id: "sub4", title: "Evidence Files", value: "42", delta: "Uploads ready", tone: "info", icon: "attach_file" },
    ],
    queues: [
      { id: "q1", title: "My Work Queue", count: "11", route: "/app/workflow/my-work-queue", tone: "accent" },
      { id: "q2", title: "Returned for Information", count: "3", route: "/app/workflow/returned-for-information", tone: "danger" },
    ],
    highlights: [
      { label: "Focus", value: "Complete quarter actuals and supporting files" },
      { label: "Priority", value: "Respond to verifier requests quickly" },
    ],
  },
  verifier: {
    stats: [
      { id: "v1", title: "To Verify", value: "21", delta: "6 due today", tone: "accent", icon: "verified" },
      { id: "v2", title: "Returned", value: "7", delta: "Need follow-up", tone: "warning", icon: "assignment_return" },
      { id: "v3", title: "Verified", value: "84", delta: "+12 this week", tone: "success", icon: "task_alt" },
      { id: "v4", title: "Evidence Gaps", value: "4", delta: "Critical", tone: "danger", icon: "error" },
    ],
    queues: [
      { id: "q1", title: "Verification", count: "21", route: "/app/workflow/verification", tone: "accent" },
      { id: "q2", title: "Returned for Information", count: "7", route: "/app/workflow/returned-for-information", tone: "warning" },
    ],
    highlights: [
      { label: "Focus", value: "Evidence quality and status accuracy" },
      { label: "Risk", value: "Overdue verification on high-weight KPIs" },
    ],
  },
  approver: {
    stats: [
      { id: "a1", title: "Awaiting Approval", value: "16", delta: "4 priority", tone: "accent", icon: "approval" },
      { id: "a2", title: "Rejected", value: "2", delta: "This month", tone: "danger", icon: "close" },
      { id: "a3", title: "Approved", value: "73", delta: "+9 this week", tone: "success", icon: "done_all" },
      { id: "a4", title: "Average Turnaround", value: "2.8d", delta: "-0.4d", tone: "info", icon: "schedule" },
    ],
    queues: [
      { id: "q1", title: "Approval", count: "16", route: "/app/workflow/approval", tone: "accent" },
      { id: "q2", title: "Completed Submissions", count: "73", route: "/app/workflow/completed-submissions", tone: "success" },
    ],
    highlights: [
      { label: "Focus", value: "Fast turnaround without skipping evidence review" },
      { label: "Report", value: "Approval Turnaround report available" },
    ],
  },
  auditor: {
    stats: [
      { id: "au1", title: "Auditor Review", value: "8", delta: "3 critical", tone: "accent", icon: "gavel" },
      { id: "au2", title: "Findings", value: "12", delta: "2 open", tone: "danger", icon: "report" },
      { id: "au3", title: "Cleared", value: "34", delta: "Quarter to date", tone: "success", icon: "verified_user" },
      { id: "au4", title: "Evidence Completeness", value: "91%", delta: "+2%", tone: "info", icon: "inventory_2" },
    ],
    queues: [
      { id: "q1", title: "Auditor Review", count: "8", route: "/app/workflow/auditor-review", tone: "accent" },
      { id: "q2", title: "Audit Findings", count: "12", route: "/app/reports/audit-findings", tone: "danger" },
    ],
    highlights: [
      { label: "Focus", value: "Capture findings and review trail integrity" },
      { label: "Risk", value: "Missing evidence on returned submissions" },
    ],
  },
  "hr-admin": {
    stats: [
      { id: "hr1", title: "Employees", value: "1284", delta: "14 pending updates", tone: "accent", icon: "badge" },
      { id: "hr2", title: "Positions", value: "206", delta: "6 vacant", tone: "warning", icon: "work" },
      { id: "hr3", title: "Resumes", value: "1048", delta: "96% coverage", tone: "success", icon: "description" },
      { id: "hr4", title: "Tasks", value: "19", delta: "5 overdue", tone: "danger", icon: "checklist" },
    ],
    queues: [
      { id: "q1", title: "Employees", count: "1284", route: "/app/human-resources/employees", tone: "accent" },
      { id: "q2", title: "Tasks", count: "19", route: "/app/human-resources/tasks", tone: "warning" },
    ],
    highlights: [
      { label: "Focus", value: "Staffing records, portfolios and assignments" },
      { label: "Gap", value: "Resume completeness on new hires" },
    ],
  },
  "executive-viewer": {
    stats: [
      { id: "e1", title: "Overall KPI Completion", value: "72%", delta: "+4.2%", tone: "accent", icon: "donut_large" },
      { id: "e2", title: "Strategic Goals On Track", value: "5/7", delta: "Stable", tone: "success", icon: "flag" },
      { id: "e3", title: "Overdue Submissions", value: "23", delta: "+6", tone: "danger", icon: "warning" },
      { id: "e4", title: "Average Score", value: "3.6/5", delta: "+0.2", tone: "info", icon: "star" },
    ],
    queues: [
      { id: "q1", title: "Reports", count: "9", route: "/app/reports", tone: "accent" },
      { id: "q2", title: "Completed Submissions", count: "96", route: "/app/workflow/completed-submissions", tone: "success" },
    ],
    highlights: [
      { label: "Focus", value: "Executive summary and statutory reporting" },
      { label: "Risk", value: "Overdue submissions in service delivery units" },
    ],
  },
};

export const getAllRoutes = (): ModuleRoute[] => sidebarGroups.flatMap((group) => group.items);

export const routeByPath = (path: string) => getAllRoutes().find((route) => route.path === path);
