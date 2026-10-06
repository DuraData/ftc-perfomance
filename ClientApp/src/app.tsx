import { lazy, Suspense } from 'react';
import { AppProvider, useApp } from './context/AppContext';
import { SecurityProvider } from './context/SecurityContext';
import { Login } from './components/auth/Login';
import { AccessDeniedPage, useCanAccessPath } from './components/security/AccessControl';

const Dashboard = lazy(() => import('./components/dashboard/Dashboard').then(module => ({ default: module.Dashboard })));
const OPMSTargetList = lazy(() => import('./components/opms/OPMSTargetList').then(module => ({ default: module.OPMSTargetList })));
const SdbipImportWorkspace = lazy(() => import('./components/opms/SdbipImportWorkspace').then(module => ({ default: module.SdbipImportWorkspace })));
const OPMSTargetDetail = lazy(() => import('./components/opms/OPMSTargetDetail').then(module => ({ default: module.OPMSTargetDetail })));
const OPMSSubmissionsList = lazy(() => import('./components/opms/OPMSSubmissions').then(module => ({ default: module.OPMSSubmissionsList })));
const IPMSSubmissionsList = lazy(() => import('./components/opms/OPMSSubmissions').then(module => ({ default: module.IPMSSubmissionsList })));
const IPMSTargetList = lazy(() => import('./components/ipms/IPMSTargetList').then(module => ({ default: module.IPMSTargetList })));
const IPMSTargetDetail = lazy(() => import('./components/ipms/IPMSTargetDetail').then(module => ({ default: module.IPMSTargetDetail })));
const WorkflowQueues = lazy(() => import('./components/workflow/WorkflowQueues').then(module => ({ default: module.WorkflowQueues })));
const MyWorkQueue = lazy(() => import('./components/workflow/WorkflowQueues').then(module => ({ default: module.MyWorkQueue })));
const TenantOrganizationAdministration = lazy(() => import('./components/admin/TenantOrganizationAdministration').then(module => ({ default: module.TenantOrganizationAdministration })));
const TenantReferenceAdministration = lazy(() => import('./components/admin/TenantReferenceAdministration').then(module => ({ default: module.TenantReferenceAdministration })));
const GlobalStrategicReferenceAdministration = lazy(() => import('./components/admin/GlobalStrategicReferenceAdministration').then(module => ({ default: module.GlobalStrategicReferenceAdministration })));
const LookupTables = lazy(() => import('./components/admin/AdminManagement').then(module => ({ default: module.LookupTables })));
const AdminAuditLogsPage = lazy(() => import('./components/admin/SystemAdmin').then(module => ({ default: module.AdminAuditLogsPage })));
const AdminUsersPage = lazy(() => import('./components/admin/SystemAdmin').then(module => ({ default: module.AdminUsersPage })));
const RoleImplementationAuditPage = lazy(() => import('./components/admin/RoleImplementationAuditPage').then(module => ({ default: module.RoleImplementationAuditPage })));
const PermissionSimulationPage = lazy(() => import('./components/admin/AccessGovernancePages').then(module => ({ default: module.PermissionSimulationPage })));
const RoleAccessMatrixPage = lazy(() => import('./components/admin/AccessGovernancePages').then(module => ({ default: module.RoleAccessMatrixPage })));
const RolePermissionCrudAuditPage = lazy(() => import('./components/admin/AccessGovernancePages').then(module => ({ default: module.RolePermissionCrudAuditPage })));
const SystemCoverageAuditPage = lazy(() => import('./components/admin/AccessGovernancePages').then(module => ({ default: module.SystemCoverageAuditPage })));
const StrategicPlanningAdministration = lazy(() => import('./components/admin/StrategicPlanningAdministration').then(module => ({ default: module.StrategicPlanningAdministration })));
const IPMSTargetLibraryDetail = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.IPMSTargetLibraryDetail })));
const IPMSTargetLibraryList = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.IPMSTargetLibraryList })));
const IPMSTargetTemplateFormPage = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.IPMSTargetTemplateFormPage })));
const OPMSTargetLibraryDetail = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.OPMSTargetLibraryDetail })));
const OPMSTargetLibraryList = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.OPMSTargetLibraryList })));
const OPMSTargetTemplateFormPage = lazy(() => import('./components/library/TargetLibraries').then(module => ({ default: module.OPMSTargetTemplateFormPage })));
const TaskManagement = lazy(() => import('./components/tasks/TaskManagement').then(module => ({ default: module.TaskManagement })));
const IPMSTargetFormPage = lazy(() => import('./components/targets/TargetFormPages').then(module => ({ default: module.IPMSTargetFormPage })));
const OPMSTargetFormPage = lazy(() => import('./components/targets/TargetFormPages').then(module => ({ default: module.OPMSTargetFormPage })));
const KPILibrary = lazy(() => import('./components/kpi/KPILibrary').then(module => ({ default: module.KPILibrary })));
const Reports = lazy(() => import('./components/reports/Reports').then(module => ({ default: module.Reports })));
const Settings = lazy(() => import('./components/settings/Settings').then(module => ({ default: module.Settings })));
const SecurityAdministrationPage = lazy(() => import('./components/security/SecurityAdministration').then(module => ({ default: module.SecurityAdministrationPage })));
const AuthenticationAdministrationPage = lazy(() => import('./components/security/AuthenticationAdministration').then(module => ({ default: module.AuthenticationAdministrationPage })));
const WorkflowGovernanceAdminPage = lazy(() => import('./components/admin/WorkflowGovernanceAdmin').then(module => ({ default: module.WorkflowGovernanceAdminPage })));
const TenantCalendarAdministration = lazy(() => import('./components/admin/TenantCalendarAdministration').then(module => ({ default: module.TenantCalendarAdministration })));
const TenantEmployeeAdministration = lazy(() => import('./components/admin/TenantEmployeeAdministration').then(module => ({ default: module.TenantEmployeeAdministration })));
const IdpAlignmentMatrixPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpAlignmentMatrixPage })));
const IdpCommunityParticipationPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpCommunityParticipationPage })));
const IdpDocumentsPage = lazy(() => import('./components/idp/IdpDocumentsPage').then(module => ({ default: module.IdpDocumentsPage })));
const IdpHierarchyPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpHierarchyPage })));
const IdpPlanManagementPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpPlanManagementPage })));
const IdpPlanningDashboardPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpPlanningDashboardPage })));
const IdpReportsPage = lazy(() => import('./components/idp/IdpWorkspace').then(module => ({ default: module.IdpReportsPage })));
const OPMSDashboardPage = lazy(() => import('./components/opms/OPMSDashboard').then(module => ({ default: module.OPMSDashboardPage })));
const TidWorkspace = lazy(() => import('./components/tid/TidWorkspace').then(module => ({ default: module.TidWorkspace })));
const StrategicDocumentsWorkspace = lazy(() => import('./components/documents/StrategicDocumentsWorkspace').then(module => ({ default: module.StrategicDocumentsWorkspace })));
const C88Workspace = lazy(() => import('./components/c88/C88Workspace').then(module => ({ default: module.C88Workspace })));
const IPMSDashboardPage = lazy(() => import('./components/ipms/IPMSDashboard').then(module => ({ default: module.IPMSDashboardPage })));
const RiskDashboardPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskDashboardPage })));
const RiskRegisterPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskRegisterPage })));
const RiskAssessmentsPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskAssessmentsPage })));
const RiskTreatmentPlansPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskTreatmentPlansPage })));
const RiskReviewsPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskReviewsPage })));
const RiskHeatmapPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskHeatmapPage })));
const RiskReportsPage = lazy(() => import('./components/risk/RiskWorkspace').then(module => ({ default: module.RiskReportsPage })));

function AppContent() {
  const { currentPath, isAuthenticated } = useApp();
  const canAccessPath = useCanAccessPath(currentPath);

  if (!isAuthenticated) {
    return <Login />;
  }

  if (!canAccessPath) {
    return <AccessDeniedPage />;
  }

  const renderPage = () => {
    if (currentPath === '/opms/targets/new') {
      return <OPMSTargetFormPage />;
    }
    if (currentPath === '/ipms/targets/new') {
      return <IPMSTargetFormPage />;
    }
    if (currentPath.startsWith('/opms/targets/') && currentPath.endsWith('/edit')) {
      const id = currentPath.split('/')[3];
      return <OPMSTargetFormPage targetId={id} />;
    }
    if (currentPath.startsWith('/ipms/targets/') && currentPath.endsWith('/edit')) {
      const id = currentPath.split('/')[3];
      return <IPMSTargetFormPage targetId={id} />;
    }
    if (currentPath === '/opms/library/new') {
      return <OPMSTargetTemplateFormPage />;
    }
    if (currentPath === '/ipms/library/new') {
      return <IPMSTargetTemplateFormPage />;
    }
    if (currentPath.startsWith('/opms/library/') && currentPath.endsWith('/edit')) {
      const id = currentPath.split('/')[3];
      return <OPMSTargetTemplateFormPage templateId={id} />;
    }
    if (currentPath.startsWith('/ipms/library/') && currentPath.endsWith('/edit')) {
      const id = currentPath.split('/')[3];
      return <IPMSTargetTemplateFormPage templateId={id} />;
    }
    if (currentPath.startsWith('/opms/library/') && currentPath !== '/opms/library') {
      const id = currentPath.split('/')[3];
      return <OPMSTargetLibraryDetail templateId={id} />;
    }
    if (currentPath.startsWith('/ipms/library/') && currentPath !== '/ipms/library') {
      const id = currentPath.split('/')[3];
      return <IPMSTargetLibraryDetail templateId={id} />;
    }
    if (currentPath.startsWith('/ipms/targets/') && currentPath !== '/ipms/targets') {
      const id = currentPath.split('/').pop();
      return <IPMSTargetDetail targetId={id} />;
    }
    if (currentPath.startsWith('/opms/targets/') && currentPath !== '/opms/targets') {
      const id = currentPath.split('/').pop();
      return <OPMSTargetDetail targetId={id} />;
    }

    switch (currentPath) {
      case '/dashboard':
        return <Dashboard />;
      case '/kpi-library':
        return <KPILibrary />;
      case '/opms/dashboard':
        return <OPMSDashboardPage />;
      case '/opms/library':
        return <OPMSTargetLibraryList />;
      case '/opms/targets':
        return <OPMSTargetList />;
      case '/opms/import':
        return <SdbipImportWorkspace />;
      case '/opms/submissions':
        return <OPMSSubmissionsList />;
      case '/opms/vote-numbers':
        return <TenantReferenceAdministration kind="vote-numbers" />;
      case '/opms/tids':
        return <TidWorkspace />;
      case '/strategic-documents':
        return <StrategicDocumentsWorkspace />;
      case '/c88/planning':
      case '/c88/reporting':
      case '/c88/compliance':
      case '/c88/mapping':
      case '/c88/reports':
        return <C88Workspace />;
      case '/ipms/dashboard':
        return <IPMSDashboardPage />;
      case '/ipms/library':
        return <IPMSTargetLibraryList />;
      case '/ipms/targets':
        return <IPMSTargetList />;
      case '/ipms/submissions':
        return <IPMSSubmissionsList />;
      case '/workflow/my-queue':
        return <MyWorkQueue />;
      case '/workflow/my-drafts':
        return <MyWorkQueue />;
      case '/workflow/pending-submission':
        return <MyWorkQueue />;
      case '/workflow/returned-submissions':
        return <MyWorkQueue />;
      case '/workflow/under-verification':
        return <MyWorkQueue />;
      case '/workflow/under-review':
        return <MyWorkQueue />;
      case '/workflow/under-approval':
        return <MyWorkQueue />;
      case '/workflow/internal-audit-returned':
        return <MyWorkQueue />;
      case '/workflow/approved-closed':
        return <MyWorkQueue />;
      case '/workflow/verification':
        return <WorkflowQueues />;
      case '/workflow/approval':
        return <WorkflowQueues />;
      case '/workflow/pms-review':
        return <WorkflowQueues />;
      case '/workflow/auditor-review':
        return <WorkflowQueues />;
      case '/hr/employees':
        return <TenantEmployeeAdministration />;
      case '/hr/departments':
        return <TenantOrganizationAdministration kind="departments" />;
      case '/hr/units':
        return <TenantOrganizationAdministration kind="units" />;
      case '/hr/positions':
        return <TenantOrganizationAdministration kind="positions" />;
      case '/admin/wards':
        return <TenantReferenceAdministration kind="wards" />;
      case '/hr/contacts':
        return <TenantEmployeeAdministration />;
      case '/hr/resumes':
        return <TenantEmployeeAdministration />;
      case '/tasks':
        return <TaskManagement />;
      case '/admin/periods':
        return <TenantCalendarAdministration />;
      case '/admin/organisations':
        return <TenantOrganizationAdministration kind="departments" />;
      case '/admin/approval-setup':
        return <WorkflowGovernanceAdminPage />;
      case '/admin/lookups':
        return <LookupTables />;
      case '/admin/users':
        return <AdminUsersPage />;
      case '/admin/roles':
        return <SecurityAdministrationPage />;
      case '/admin/permissions':
        return <SecurityAdministrationPage />;
      case '/admin/audit':
        return <AdminAuditLogsPage />;
      case '/system-administration/users':
        return <AdminUsersPage />;
      case '/system-administration/roles':
        return <SecurityAdministrationPage />;
      case '/system-administration/permissions':
        return <SecurityAdministrationPage />;
      case '/system-administration/security':
        return <SecurityAdministrationPage />;
      case '/system-administration/authentication':
        return <AuthenticationAdministrationPage />;
      case '/system-administration/audit-logs':
        return <AdminAuditLogsPage />;
      case '/system-administration/role-implementation-audit':
        return <RoleImplementationAuditPage />;
      case '/system-administration/role-access-matrix':
        return <RoleAccessMatrixPage />;
      case '/system-administration/permission-simulation':
        return <PermissionSimulationPage />;
      case '/system-administration/system-coverage-audit':
        return <SystemCoverageAuditPage />;
      case '/system-administration/role-permission-crud-audit':
        return <RolePermissionCrudAuditPage />;
      case '/admin/budget-types':
        return <StrategicPlanningAdministration kind="budget-types" />;
      case '/admin/budget-sources':
        return <StrategicPlanningAdministration kind="budget-sources" />;
      case '/admin/kpi-types':
        return <StrategicPlanningAdministration kind="kpi-types" />;
      case '/admin/indicator-types':
        return <StrategicPlanningAdministration kind="indicator-types" />;
      case '/admin/functional-areas':
        return <StrategicPlanningAdministration kind="functional-areas" />;
      case '/admin/standard-classifications':
        return <StrategicPlanningAdministration kind="standard-classifications" />;
      case '/admin/strategic-goals':
        return <StrategicPlanningAdministration kind="strategic-goals" />;
      case '/admin/strategic-interventions':
        return <StrategicPlanningAdministration kind="strategic-interventions" />;
      case '/admin/strategic-objectives':
        return <StrategicPlanningAdministration kind="strategic-objectives" />;
      case '/admin/units-measure':
        return <StrategicPlanningAdministration kind="kpi-units-of-measure" />;
      case '/admin/kpas':
        return <GlobalStrategicReferenceAdministration kind="national-kpas" />;
      case '/admin/back-to-basics-pillars':
        return <GlobalStrategicReferenceAdministration kind="back-to-basics-pillars" />;
      case '/admin/municipal-kpas':
        return <StrategicPlanningAdministration kind="municipal-kpas" />;
      case '/admin/performance-objectives':
        return <StrategicPlanningAdministration kind="performance-objectives" />;
      case '/admin/occupations':
        return <TenantOrganizationAdministration kind="positions" />;
      case '/reports':
        return <Reports />;
      case '/reports/opms-performance':
        return <Reports />;
      case '/reports/ipms-performance':
        return <Reports />;
      case '/reports/idp-summary':
        return <Reports />;
      case '/reports/submission-status':
        return <Reports />;
      case '/reports/evidence-register':
        return <Reports />;
      case '/reports/returned-submissions':
        return <Reports />;
      case '/reports/overdue-submissions':
        return <Reports />;
      case '/idp/dashboard':
        return <IdpPlanningDashboardPage />;
      case '/idp/overview':
        return <IdpPlanManagementPage />;
      case '/idp/strategic-objectives':
        return <IdpHierarchyPage />;
      case '/idp/projects':
        return <IdpHierarchyPage />;
      case '/idp/kpis':
        return <IdpHierarchyPage />;
      case '/idp/plans':
        return <IdpPlanManagementPage />;
      case '/idp/hierarchy':
        return <IdpHierarchyPage />;
      case '/idp/community':
        return <IdpCommunityParticipationPage />;
      case '/idp/alignment':
        return <IdpAlignmentMatrixPage />;
      case '/idp/documents':
        return <IdpDocumentsPage />;
      case '/idp/reports':
        return <IdpReportsPage />;
      case '/risk/dashboard':
        return <RiskDashboardPage />;
      case '/risk/register':
        return <RiskRegisterPage />;
      case '/risk/assessments':
        return <RiskAssessmentsPage />;
      case '/risk/treatment-plans':
        return <RiskTreatmentPlansPage />;
      case '/risk/reviews':
        return <RiskReviewsPage />;
      case '/risk/heatmap':
        return <RiskHeatmapPage />;
      case '/risk/reports':
        return <RiskReportsPage />;
      case '/settings':
        return <Settings />;
      case '/notifications':
        return <Settings />;
      case '/my-profile':
        return <Settings />;
      default:
        return <Dashboard />;
    }
  };

  return (
    <Suspense fallback={<div role="status" aria-live="polite" className="p-6 text-sm text-secondary-600">Loading workspace…</div>}>
      {renderPage()}
    </Suspense>
  );
}

function App() {
  return (
    <AppProvider>
      <SecurityProvider>
        <AppContent />
      </SecurityProvider>
    </AppProvider>
  );
}

export default App;
