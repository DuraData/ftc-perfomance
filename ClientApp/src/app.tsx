import React from 'react';
import { AppProvider, useApp } from './context/AppContext';
import { Dashboard } from './components/dashboard/Dashboard';
import { OPMSTargetList } from './components/opms/OPMSTargetList';
import { OPMSTargetDetail } from './components/opms/OPMSTargetDetail';
import { OPMSSubmissionsList, IPMSSubmissionsList, VoteNumbersPage } from './components/opms/OPMSSubmissions';
import { IPMSTargetList } from './components/ipms/IPMSTargetList';
import { IPMSTargetDetail } from './components/ipms/IPMSTargetDetail';
import { WorkflowQueues, MyWorkQueue } from './components/workflow/WorkflowQueues';
import { EmployeeList, DepartmentList, DepartmentUnitList, PositionList } from './components/hr/HRManagement';
import { PeriodList, BudgetSourceList, ApprovalSetupList, LookupTables } from './components/admin/AdminManagement';
import { AdminAuditLogsPage, AdminPermissionsPage, AdminRolesPage, AdminUsersPage } from './components/admin/SystemAdmin';
import {
  CountriesPage, ProvincesPage, CitiesPage, SuburbsPage, AddressesPage,
  OrganisationsPage, IndustriesPage, ContactsPage, ResumesPage, OccupationsPage,
  BudgetTypesPage, StrategicGoalsPage, StrategicObjectivesPage, UnitOfMeasurePage,
  KPAsPage, MunicipalKPAsPage, DepartmentalObjectivesPage, OutputsPage,
  PerformanceObjectivesPage, PriorityIssuesPage
} from './components/admin/GenericLookupPages';
import { TaskManagement } from './components/tasks/TaskManagement';
import { KPILibrary } from './components/kpi/KPILibrary';
import { Reports } from './components/reports/Reports';
import { Settings } from './components/settings/Settings';
import { Login } from './components/auth/Login';

function AppContent() {
  const { currentPath, isAuthenticated } = useApp();

  if (!isAuthenticated) {
    return <Login />;
  }

  const renderPage = () => {
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
      case '/opms/targets':
        return <OPMSTargetList />;
      case '/opms/submissions':
        return <OPMSSubmissionsList />;
      case '/opms/vote-numbers':
        return <VoteNumbersPage />;
      case '/ipms/targets':
        return <IPMSTargetList />;
      case '/ipms/submissions':
        return <IPMSSubmissionsList />;
      case '/kpi-library':
        return <KPILibrary />;
      case '/workflow/my-queue':
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
      case '/settings':
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
