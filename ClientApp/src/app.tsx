import { Navigate, Route, Routes, useNavigate } from "react-router-dom";
import { AppLayout, EmptyState } from "./components";
import { useAppContext } from "./store";
import {
  CrudCoverageAuditPage,
  DashboardPage,
  GenericEntityDetailRoute,
  GenericEntityFormRoute,
  GenericEntityListRoute,
  KpiLibraryListPage,
  LoginPage,
  OverviewPage,
  ReportViewPage,
  ReportsHomePage,
  SettingsPage,
  SubmissionDetailRoute,
  SubmissionFormRoute,
  TargetDetailRoute,
  TargetFormRoute,
  WorkflowQueuePage,
} from "./pages";

const NotFoundPage = () => {
  const navigate = useNavigate();

  return (
    <EmptyState
      title="Route not found"
      description="This route is not configured. Use the sidebar to navigate to an available page."
      actionLabel="Go to dashboard"
      onAction={() => navigate("/app/dashboard")}
    />
  );
};

const GuardedApp = () => {
  const { loggedIn } = useAppContext();

  if (!loggedIn) {
    return <Navigate to="/login" replace />;
  }

  return (
    <AppLayout>
      <Routes>
        <Route path="dashboard" element={<DashboardPage />} />

        <Route path="opms" element={<OverviewPage routeKey="opms-home" />} />
        <Route path="opms/targets" element={<GenericEntityListRoute entityKey="opmsTargets" />} />
        <Route path="opms/targets/new" element={<TargetFormRoute entityKey="opmsTargets" />} />
        <Route path="opms/targets/:id" element={<TargetDetailRoute entityKey="opmsTargets" />} />
        <Route path="opms/targets/:id/edit" element={<TargetFormRoute entityKey="opmsTargets" />} />
        <Route path="opms/submissions" element={<GenericEntityListRoute entityKey="opmsSubmissions" />} />
        <Route path="opms/submissions/new" element={<SubmissionFormRoute entityKey="opmsSubmissions" />} />
        <Route path="opms/submissions/:id" element={<SubmissionDetailRoute entityKey="opmsSubmissions" />} />
        <Route path="opms/submissions/:id/edit" element={<SubmissionFormRoute entityKey="opmsSubmissions" />} />
        <Route path="opms/vote-numbers" element={<GenericEntityListRoute entityKey="voteNumbers" />} />
        <Route path="opms/vote-numbers/new" element={<GenericEntityFormRoute entityKey="voteNumbers" />} />
        <Route path="opms/vote-numbers/:id" element={<GenericEntityDetailRoute entityKey="voteNumbers" />} />
        <Route path="opms/vote-numbers/:id/edit" element={<GenericEntityFormRoute entityKey="voteNumbers" />} />

        <Route path="ipms" element={<OverviewPage routeKey="ipms-home" />} />
        <Route path="ipms/targets" element={<GenericEntityListRoute entityKey="ipmsTargets" />} />
        <Route path="ipms/targets/new" element={<TargetFormRoute entityKey="ipmsTargets" />} />
        <Route path="ipms/targets/:id" element={<TargetDetailRoute entityKey="ipmsTargets" />} />
        <Route path="ipms/targets/:id/edit" element={<TargetFormRoute entityKey="ipmsTargets" />} />
        <Route path="ipms/submissions" element={<GenericEntityListRoute entityKey="ipmsSubmissions" />} />
        <Route path="ipms/submissions/new" element={<SubmissionFormRoute entityKey="ipmsSubmissions" />} />
        <Route path="ipms/submissions/:id" element={<SubmissionDetailRoute entityKey="ipmsSubmissions" />} />
        <Route path="ipms/submissions/:id/edit" element={<SubmissionFormRoute entityKey="ipmsSubmissions" />} />

        <Route path="kpi-library" element={<KpiLibraryListPage />} />
        <Route path="kpi-library/new" element={<GenericEntityFormRoute entityKey="kpiLibrary" />} />
        <Route path="kpi-library/:id" element={<GenericEntityDetailRoute entityKey="kpiLibrary" />} />
        <Route path="kpi-library/:id/edit" element={<GenericEntityFormRoute entityKey="kpiLibrary" />} />

        <Route path="workflow" element={<OverviewPage routeKey="workflow-home" />} />
        <Route path="workflow/my-work-queue" element={<WorkflowQueuePage queueKey="my-work-queue" />} />
        <Route path="workflow/verification" element={<WorkflowQueuePage queueKey="verification" />} />
        <Route path="workflow/approval" element={<WorkflowQueuePage queueKey="approval" />} />
        <Route path="workflow/pms-review" element={<WorkflowQueuePage queueKey="pms-review" />} />
        <Route path="workflow/auditor-review" element={<WorkflowQueuePage queueKey="auditor-review" />} />
        <Route path="workflow/returned-for-information" element={<WorkflowQueuePage queueKey="returned-for-information" />} />
        <Route path="workflow/completed-submissions" element={<WorkflowQueuePage queueKey="completed-submissions" />} />
        <Route path="workflow/overdue-items" element={<WorkflowQueuePage queueKey="overdue-items" />} />

        <Route path="human-resources" element={<OverviewPage routeKey="hr-home" />} />
        <Route path="human-resources/employees" element={<GenericEntityListRoute entityKey="employees" />} />
        <Route path="human-resources/employees/new" element={<GenericEntityFormRoute entityKey="employees" />} />
        <Route path="human-resources/employees/:id" element={<GenericEntityDetailRoute entityKey="employees" />} />
        <Route path="human-resources/employees/:id/edit" element={<GenericEntityFormRoute entityKey="employees" />} />
        <Route path="human-resources/departments" element={<GenericEntityListRoute entityKey="departments" />} />
        <Route path="human-resources/departments/new" element={<GenericEntityFormRoute entityKey="departments" />} />
        <Route path="human-resources/departments/:id" element={<GenericEntityDetailRoute entityKey="departments" />} />
        <Route path="human-resources/departments/:id/edit" element={<GenericEntityFormRoute entityKey="departments" />} />
        <Route path="human-resources/department-units" element={<GenericEntityListRoute entityKey="departmentUnits" />} />
        <Route path="human-resources/department-units/new" element={<GenericEntityFormRoute entityKey="departmentUnits" />} />
        <Route path="human-resources/department-units/:id" element={<GenericEntityDetailRoute entityKey="departmentUnits" />} />
        <Route path="human-resources/department-units/:id/edit" element={<GenericEntityFormRoute entityKey="departmentUnits" />} />
        <Route path="human-resources/positions" element={<GenericEntityListRoute entityKey="positions" />} />
        <Route path="human-resources/positions/new" element={<GenericEntityFormRoute entityKey="positions" />} />
        <Route path="human-resources/positions/:id" element={<GenericEntityDetailRoute entityKey="positions" />} />
        <Route path="human-resources/positions/:id/edit" element={<GenericEntityFormRoute entityKey="positions" />} />
        <Route path="human-resources/occupations" element={<GenericEntityListRoute entityKey="occupations" />} />
        <Route path="human-resources/occupations/new" element={<GenericEntityFormRoute entityKey="occupations" />} />
        <Route path="human-resources/occupations/:id" element={<GenericEntityDetailRoute entityKey="occupations" />} />
        <Route path="human-resources/occupations/:id/edit" element={<GenericEntityFormRoute entityKey="occupations" />} />
        <Route path="human-resources/contacts" element={<GenericEntityListRoute entityKey="contacts" />} />
        <Route path="human-resources/contacts/new" element={<GenericEntityFormRoute entityKey="contacts" />} />
        <Route path="human-resources/contacts/:id" element={<GenericEntityDetailRoute entityKey="contacts" />} />
        <Route path="human-resources/contacts/:id/edit" element={<GenericEntityFormRoute entityKey="contacts" />} />
        <Route path="human-resources/resumes" element={<GenericEntityListRoute entityKey="resumes" />} />
        <Route path="human-resources/resumes/new" element={<GenericEntityFormRoute entityKey="resumes" />} />
        <Route path="human-resources/resumes/:id" element={<GenericEntityDetailRoute entityKey="resumes" />} />
        <Route path="human-resources/resumes/:id/edit" element={<GenericEntityFormRoute entityKey="resumes" />} />
        <Route path="human-resources/portfolio-files" element={<GenericEntityListRoute entityKey="portfolioFiles" />} />
        <Route path="human-resources/portfolio-files/new" element={<GenericEntityFormRoute entityKey="portfolioFiles" />} />
        <Route path="human-resources/portfolio-files/:id" element={<GenericEntityDetailRoute entityKey="portfolioFiles" />} />
        <Route path="human-resources/portfolio-files/:id/edit" element={<GenericEntityFormRoute entityKey="portfolioFiles" />} />
        <Route path="human-resources/tasks" element={<GenericEntityListRoute entityKey="tasks" />} />
        <Route path="human-resources/tasks/new" element={<GenericEntityFormRoute entityKey="tasks" />} />
        <Route path="human-resources/tasks/:id" element={<GenericEntityDetailRoute entityKey="tasks" />} />
        <Route path="human-resources/tasks/:id/edit" element={<GenericEntityFormRoute entityKey="tasks" />} />

        <Route path="administration" element={<OverviewPage routeKey="admin-home" />} />
        <Route path="administration/periods" element={<GenericEntityListRoute entityKey="periods" />} />
        <Route path="administration/periods/new" element={<GenericEntityFormRoute entityKey="periods" />} />
        <Route path="administration/periods/:id" element={<GenericEntityDetailRoute entityKey="periods" />} />
        <Route path="administration/periods/:id/edit" element={<GenericEntityFormRoute entityKey="periods" />} />
        <Route path="administration/organisations" element={<GenericEntityListRoute entityKey="organisations" />} />
        <Route path="administration/organisations/new" element={<GenericEntityFormRoute entityKey="organisations" />} />
        <Route path="administration/organisations/:id" element={<GenericEntityDetailRoute entityKey="organisations" />} />
        <Route path="administration/organisations/:id/edit" element={<GenericEntityFormRoute entityKey="organisations" />} />
        <Route path="administration/industries" element={<GenericEntityListRoute entityKey="industries" />} />
        <Route path="administration/industries/new" element={<GenericEntityFormRoute entityKey="industries" />} />
        <Route path="administration/industries/:id" element={<GenericEntityDetailRoute entityKey="industries" />} />
        <Route path="administration/industries/:id/edit" element={<GenericEntityFormRoute entityKey="industries" />} />
        <Route path="administration/approval-setup" element={<GenericEntityListRoute entityKey="approvalSetup" />} />
        <Route path="administration/approval-setup/new" element={<GenericEntityFormRoute entityKey="approvalSetup" />} />
        <Route path="administration/approval-setup/:id" element={<GenericEntityDetailRoute entityKey="approvalSetup" />} />
        <Route path="administration/approval-setup/:id/edit" element={<GenericEntityFormRoute entityKey="approvalSetup" />} />
        <Route path="administration/budget-sources" element={<GenericEntityListRoute entityKey="budgetSources" />} />
        <Route path="administration/budget-sources/new" element={<GenericEntityFormRoute entityKey="budgetSources" />} />
        <Route path="administration/budget-sources/:id" element={<GenericEntityDetailRoute entityKey="budgetSources" />} />
        <Route path="administration/budget-sources/:id/edit" element={<GenericEntityFormRoute entityKey="budgetSources" />} />
        <Route path="administration/budget-types" element={<GenericEntityListRoute entityKey="budgetTypes" />} />
        <Route path="administration/budget-types/new" element={<GenericEntityFormRoute entityKey="budgetTypes" />} />
        <Route path="administration/budget-types/:id" element={<GenericEntityDetailRoute entityKey="budgetTypes" />} />
        <Route path="administration/budget-types/:id/edit" element={<GenericEntityFormRoute entityKey="budgetTypes" />} />
        <Route path="administration/strategic-goals" element={<GenericEntityListRoute entityKey="strategicGoals" />} />
        <Route path="administration/strategic-goals/new" element={<GenericEntityFormRoute entityKey="strategicGoals" />} />
        <Route path="administration/strategic-goals/:id" element={<GenericEntityDetailRoute entityKey="strategicGoals" />} />
        <Route path="administration/strategic-goals/:id/edit" element={<GenericEntityFormRoute entityKey="strategicGoals" />} />
        <Route path="administration/strategic-objectives" element={<GenericEntityListRoute entityKey="strategicObjectives" />} />
        <Route path="administration/strategic-objectives/new" element={<GenericEntityFormRoute entityKey="strategicObjectives" />} />
        <Route path="administration/strategic-objectives/:id" element={<GenericEntityDetailRoute entityKey="strategicObjectives" />} />
        <Route path="administration/strategic-objectives/:id/edit" element={<GenericEntityFormRoute entityKey="strategicObjectives" />} />
        <Route path="administration/unit-of-measure" element={<GenericEntityListRoute entityKey="unitsOfMeasure" />} />
        <Route path="administration/unit-of-measure/new" element={<GenericEntityFormRoute entityKey="unitsOfMeasure" />} />
        <Route path="administration/unit-of-measure/:id" element={<GenericEntityDetailRoute entityKey="unitsOfMeasure" />} />
        <Route path="administration/unit-of-measure/:id/edit" element={<GenericEntityFormRoute entityKey="unitsOfMeasure" />} />
        <Route path="administration/kpas" element={<GenericEntityListRoute entityKey="kpas" />} />
        <Route path="administration/kpas/new" element={<GenericEntityFormRoute entityKey="kpas" />} />
        <Route path="administration/kpas/:id" element={<GenericEntityDetailRoute entityKey="kpas" />} />
        <Route path="administration/kpas/:id/edit" element={<GenericEntityFormRoute entityKey="kpas" />} />
        <Route path="administration/municipal-kpas" element={<GenericEntityListRoute entityKey="municipalKpas" />} />
        <Route path="administration/municipal-kpas/new" element={<GenericEntityFormRoute entityKey="municipalKpas" />} />
        <Route path="administration/municipal-kpas/:id" element={<GenericEntityDetailRoute entityKey="municipalKpas" />} />
        <Route path="administration/municipal-kpas/:id/edit" element={<GenericEntityFormRoute entityKey="municipalKpas" />} />
        <Route path="administration/departmental-objectives" element={<GenericEntityListRoute entityKey="departmentalObjectives" />} />
        <Route path="administration/departmental-objectives/new" element={<GenericEntityFormRoute entityKey="departmentalObjectives" />} />
        <Route path="administration/departmental-objectives/:id" element={<GenericEntityDetailRoute entityKey="departmentalObjectives" />} />
        <Route path="administration/departmental-objectives/:id/edit" element={<GenericEntityFormRoute entityKey="departmentalObjectives" />} />
        <Route path="administration/outputs" element={<GenericEntityListRoute entityKey="outputs" />} />
        <Route path="administration/outputs/new" element={<GenericEntityFormRoute entityKey="outputs" />} />
        <Route path="administration/outputs/:id" element={<GenericEntityDetailRoute entityKey="outputs" />} />
        <Route path="administration/outputs/:id/edit" element={<GenericEntityFormRoute entityKey="outputs" />} />
        <Route path="administration/performance-objectives" element={<GenericEntityListRoute entityKey="performanceObjectives" />} />
        <Route path="administration/performance-objectives/new" element={<GenericEntityFormRoute entityKey="performanceObjectives" />} />
        <Route path="administration/performance-objectives/:id" element={<GenericEntityDetailRoute entityKey="performanceObjectives" />} />
        <Route path="administration/performance-objectives/:id/edit" element={<GenericEntityFormRoute entityKey="performanceObjectives" />} />
        <Route path="administration/priority-issues" element={<GenericEntityListRoute entityKey="priorityIssues" />} />
        <Route path="administration/priority-issues/new" element={<GenericEntityFormRoute entityKey="priorityIssues" />} />
        <Route path="administration/priority-issues/:id" element={<GenericEntityDetailRoute entityKey="priorityIssues" />} />
        <Route path="administration/priority-issues/:id/edit" element={<GenericEntityFormRoute entityKey="priorityIssues" />} />

        <Route path="location-setup" element={<OverviewPage routeKey="location-home" />} />
        <Route path="location-setup/countries" element={<GenericEntityListRoute entityKey="countries" />} />
        <Route path="location-setup/countries/new" element={<GenericEntityFormRoute entityKey="countries" />} />
        <Route path="location-setup/countries/:id" element={<GenericEntityDetailRoute entityKey="countries" />} />
        <Route path="location-setup/countries/:id/edit" element={<GenericEntityFormRoute entityKey="countries" />} />
        <Route path="location-setup/provinces" element={<GenericEntityListRoute entityKey="provinces" />} />
        <Route path="location-setup/provinces/new" element={<GenericEntityFormRoute entityKey="provinces" />} />
        <Route path="location-setup/provinces/:id" element={<GenericEntityDetailRoute entityKey="provinces" />} />
        <Route path="location-setup/provinces/:id/edit" element={<GenericEntityFormRoute entityKey="provinces" />} />
        <Route path="location-setup/cities" element={<GenericEntityListRoute entityKey="cities" />} />
        <Route path="location-setup/cities/new" element={<GenericEntityFormRoute entityKey="cities" />} />
        <Route path="location-setup/cities/:id" element={<GenericEntityDetailRoute entityKey="cities" />} />
        <Route path="location-setup/cities/:id/edit" element={<GenericEntityFormRoute entityKey="cities" />} />
        <Route path="location-setup/suburbs" element={<GenericEntityListRoute entityKey="suburbs" />} />
        <Route path="location-setup/suburbs/new" element={<GenericEntityFormRoute entityKey="suburbs" />} />
        <Route path="location-setup/suburbs/:id" element={<GenericEntityDetailRoute entityKey="suburbs" />} />
        <Route path="location-setup/suburbs/:id/edit" element={<GenericEntityFormRoute entityKey="suburbs" />} />
        <Route path="location-setup/addresses" element={<GenericEntityListRoute entityKey="addresses" />} />
        <Route path="location-setup/addresses/new" element={<GenericEntityFormRoute entityKey="addresses" />} />
        <Route path="location-setup/addresses/:id" element={<GenericEntityDetailRoute entityKey="addresses" />} />
        <Route path="location-setup/addresses/:id/edit" element={<GenericEntityFormRoute entityKey="addresses" />} />

        <Route path="reports" element={<ReportsHomePage />} />
        <Route path="reports/kpi-performance-summary" element={<ReportViewPage title="KPI Performance Summary" />} />
        <Route path="reports/department-performance" element={<ReportViewPage title="Department Performance" />} />
        <Route path="reports/quarterly-submission-status" element={<ReportViewPage title="Quarterly Submission Status" />} />
        <Route path="reports/overdue-submissions" element={<ReportViewPage title="Overdue Submissions" />} />
        <Route path="reports/approval-turnaround" element={<ReportViewPage title="Approval Turnaround" />} />
        <Route path="reports/variance-report" element={<ReportViewPage title="Variance Report" />} />
        <Route path="reports/audit-findings" element={<ReportViewPage title="Audit Findings" />} />
        <Route path="reports/annual-performance-report" element={<ReportViewPage title="Annual Performance Report" />} />
        <Route path="reports/opms-ipms-alignment-report" element={<ReportViewPage title="OPMS/IPMS Alignment Report" />} />
        <Route path="crud-coverage-audit" element={<CrudCoverageAuditPage />} />
        <Route path="settings" element={<SettingsPage />} />

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </AppLayout>
  );
};

export const App = () => (
  <Routes>
    <Route path="/" element={<Navigate to="/login" replace />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/app/*" element={<GuardedApp />} />
    <Route path="*" element={<Navigate to="/login" replace />} />
  </Routes>
);
