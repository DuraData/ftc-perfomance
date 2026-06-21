import { AppProvider, useApp } from './context/AppContext';
import { Dashboard } from './components/dashboard/Dashboard';
import { OPMSTargetList } from './components/opms/OPMSTargetList';
import { OPMSTargetDetail } from './components/opms/OPMSTargetDetail';
import { OPMSSubmissionsList, IPMSSubmissionsList, VoteNumbersPage } from './components/opms/OPMSSubmissions';
import { IPMSTargetList } from './components/ipms/IPMSTargetList';
import { IPMSTargetDetail } from './components/ipms/IPMSTargetDetail';
import { WorkflowQueues, MyWorkQueue } from './components/workflow/WorkflowQueues';
import { EmployeeList, DepartmentList, DepartmentUnitList, PositionList } from './components/hr/HRManagement';
import { PeriodList, ApprovalSetupList, LookupTables } from './components/admin/AdminManagement';
import { AdminAuditLogsPage, AdminPermissionsPage, AdminRolesPage, AdminUsersPage } from './components/admin/SystemAdmin';
import { RoleImplementationAuditPage } from './components/admin/RoleImplementationAuditPage';
import { PermissionSimulationPage, RoleAccessMatrixPage, RolePermissionCrudAuditPage, SystemCoverageAuditPage } from './components/admin/AccessGovernancePages';
import {
  CountriesPage, ProvincesPage, CitiesPage, SuburbsPage, AddressesPage,
  OrganisationsPage, IndustriesPage, ContactsPage, ResumesPage, OccupationsPage,
  BudgetTypesPage, StrategicGoalsPage, StrategicObjectivesPage, UnitOfMeasurePage,
  KPAsPage, MunicipalKPAsPage, DepartmentalObjectivesPage, OutputsPage,
  PerformanceObjectivesPage, PriorityIssuesPage
} from './components/admin/GenericLookupPages';
import {
  IPMSTargetLibraryDetail,
  IPMSTargetLibraryList,
  IPMSTargetTemplateFormPage,
  OPMSTargetLibraryDetail,
  OPMSTargetLibraryList,
  OPMSTargetTemplateFormPage,
} from './components/library/TargetLibraries';
import { TaskManagement } from './components/tasks/TaskManagement';
import { IPMSTargetFormPage, OPMSTargetFormPage } from './components/targets/TargetFormPages';
import { KPILibrary } from './components/kpi/KPILibrary';
import { Reports } from './components/reports/Reports';
import { Settings } from './components/settings/Settings';
import { Login } from './components/auth/Login';
import { AccessDeniedPage, useCanAccessPath } from './components/security/AccessControl';
import {
  IdpAlignmentMatrixPage,
  IdpCommunityParticipationPage,
  IdpHierarchyPage,
  IdpPlanManagementPage,
  IdpPlanningDashboardPage,
  IdpReportsPage,
} from './components/idp/IdpWorkspace';

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
      case '/opms/library':
        return <OPMSTargetLibraryList />;
      case '/opms/targets':
        return <OPMSTargetList />;
      case '/opms/submissions':
        return <OPMSSubmissionsList />;
      case '/opms/vote-numbers':
        return <VoteNumbersPage />;
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
        return <EmployeeList />;
      case '/hr/departments':
        return <DepartmentList />;
      case '/hr/units':
        return <DepartmentUnitList />;
      case '/hr/positions':
        return <PositionList />;
      case '/hr/contacts':
        return <ContactsPage />;
      case '/hr/resumes':
        return <ResumesPage />;
      case '/tasks':
        return <TaskManagement />;
      case '/admin/periods':
        return <PeriodList />;
      case '/admin/organisations':
        return <OrganisationsPage />;
      case '/admin/approval-setup':
        return <ApprovalSetupList />;
      case '/admin/lookups':
        return <LookupTables />;
      case '/admin/users':
        return <AdminUsersPage />;
      case '/admin/roles':
        return <AdminRolesPage />;
      case '/admin/permissions':
        return <AdminPermissionsPage />;
      case '/admin/audit':
        return <AdminAuditLogsPage />;
      case '/system-administration/users':
        return <AdminUsersPage />;
      case '/system-administration/roles':
        return <AdminRolesPage />;
      case '/system-administration/permissions':
        return <AdminPermissionsPage />;
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
        return <BudgetTypesPage />;
      case '/admin/strategic-goals':
        return <StrategicGoalsPage />;
      case '/admin/strategic-objectives':
        return <StrategicObjectivesPage />;
      case '/admin/units-measure':
        return <UnitOfMeasurePage />;
      case '/admin/kpas':
        return <KPAsPage />;
      case '/admin/municipal-kpas':
        return <MunicipalKPAsPage />;
      case '/admin/departmental-objectives':
        return <DepartmentalObjectivesPage />;
      case '/admin/outputs':
        return <OutputsPage />;
      case '/admin/performance-objectives':
        return <PerformanceObjectivesPage />;
      case '/admin/priority-issues':
        return <PriorityIssuesPage />;
      case '/admin/occupations':
        return <OccupationsPage />;
      case '/admin/industries':
        return <IndustriesPage />;
      case '/location/countries':
        return <CountriesPage />;
      case '/location/provinces':
        return <ProvincesPage />;
      case '/location/cities':
        return <CitiesPage />;
      case '/location/suburbs':
        return <SuburbsPage />;
      case '/location/addresses':
        return <AddressesPage />;
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
      case '/idp/reports':
        return <IdpReportsPage />;
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

  return <>{renderPage()}</>;
}

function App() {
  return (
    <AppProvider>
      <AppContent />
    </AppProvider>
  );
}

export default App;
