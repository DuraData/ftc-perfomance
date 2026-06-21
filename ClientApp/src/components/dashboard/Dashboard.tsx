import { Bell, FileText, Shield, Target, Workflow } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Card, Badge } from '../ui';
import { useApp } from '../../context/AppContext';
import { mockDashboardStats } from '../../data/mockData';

type DashboardCard = { label: string; value: string; tone: 'primary' | 'success' | 'warning' | 'default' };

const dashboardConfig: Record<string, {
  subtitle: string;
  stats: DashboardCard[];
  queues: string[];
  reports: string[];
  quickActions: Array<{ label: string; path: string }>;
}> = {
  'Super Admin': {
    subtitle: 'Full system visibility across users, permissions, modules, and workflow',
    stats: [
      { label: 'Active Users', value: '126', tone: 'primary' },
      { label: 'Pending Workflows', value: '34', tone: 'warning' },
      { label: 'Audit Events', value: '482', tone: 'success' },
      { label: 'Open Notifications', value: '18', tone: 'default' },
    ],
    queues: ['Security approvals', 'Role changes', 'Permission overrides', 'Audit exceptions'],
    reports: ['Platform audit summary', 'Institution performance overview', 'Security configuration delta'],
    quickActions: [
      { label: 'Manage Users', path: '/system-administration/users' },
      { label: 'Open Role Matrix', path: '/system-administration/role-access-matrix' },
      { label: 'Run Permission Simulation', path: '/system-administration/permission-simulation' },
    ],
  },
  Admin: {
    subtitle: 'Operational administration for users, departments, units, setup, notifications, and due dates',
    stats: [
      { label: 'Users Managed', value: '87', tone: 'primary' },
      { label: 'Departments', value: '9', tone: 'default' },
      { label: 'Due Date Requests', value: '6', tone: 'warning' },
      { label: 'Notifications Queued', value: '12', tone: 'success' },
    ],
    queues: ['User provisioning', 'Department updates', 'Unit configuration', 'Due date extension approvals'],
    reports: ['User activity summary', 'Department setup completeness', 'Notification delivery report'],
    quickActions: [
      { label: 'Departments', path: '/hr/departments' },
      { label: 'Units', path: '/hr/units' },
      { label: 'Coverage Audit', path: '/system-administration/system-coverage-audit' },
    ],
  },
  'Client Admin': {
    subtitle: 'Read-only user directory and audit reporting access',
    stats: [
      { label: 'Directory Entries', value: '126', tone: 'primary' },
      { label: 'Audit Reports', value: '9', tone: 'success' },
      { label: 'Read-Only Modules', value: '6', tone: 'default' },
      { label: 'Exceptions', value: '0', tone: 'warning' },
    ],
    queues: ['User directory lookups', 'Audit report review', 'Compliance evidence checks'],
    reports: ['User audit trail', 'Access history summary', 'External assurance extract'],
    quickActions: [
      { label: 'Employees', path: '/hr/employees' },
      { label: 'Audit Logs', path: '/system-administration/audit-logs' },
    ],
  },
  'Auditor General': {
    subtitle: 'Institution-wide read-only oversight across OPMS, IPMS, POEs, and audit trails',
    stats: [
      { label: 'Audited Records', value: '241', tone: 'primary' },
      { label: 'POEs Reviewed', value: '132', tone: 'success' },
      { label: 'Open Findings', value: '14', tone: 'warning' },
      { label: 'Version Logs', value: '58', tone: 'default' },
    ],
    queues: ['Read-only audit review', 'Evidence checks', 'Institution scorecard review'],
    reports: ['Audit oversight dashboard', 'Institution performance report', 'Version trail summary'],
    quickActions: [
      { label: 'Reports', path: '/reports' },
      { label: 'Audit Logs', path: '/system-administration/audit-logs' },
    ],
  },
  'PMS / Performance Manager': {
    subtitle: 'Institution-wide monitoring, scoring, and approval for assigned OPMS work',
    stats: [
      { label: 'Institution Score', value: `${mockDashboardStats.averageScore.toFixed(1)}%`, tone: 'success' },
      { label: 'Pending Approvals', value: '8', tone: 'warning' },
      { label: 'Assigned KPIs', value: '12', tone: 'primary' },
      { label: 'IA Reports', value: '4', tone: 'default' },
    ],
    queues: ['Institution approvals', 'Assigned KPI scoring', 'Department monitoring', 'Escalated performance items'],
    reports: ['Institution scorecard', 'Department performance trend', 'Internal audit summary'],
    quickActions: [
      { label: 'OPMS Targets', path: '/opms/targets' },
      { label: 'Approval Queue', path: '/workflow/approval' },
    ],
  },
  'Internal Audit': {
    subtitle: 'Institution-wide audit reviews, findings, recommendations, and assurance reporting',
    stats: [
      { label: 'Pending Audits', value: '11', tone: 'warning' },
      { label: 'Findings Logged', value: '19', tone: 'primary' },
      { label: 'Recommendations', value: '24', tone: 'success' },
      { label: 'RFI Notifications', value: '5', tone: 'default' },
    ],
    queues: ['Submission audits', 'POE validation', 'Findings management', 'IA RFI follow-up'],
    reports: ['Internal audit dashboard', 'Assurance findings report', 'POE compliance status'],
    quickActions: [
      { label: 'Auditor Queue', path: '/workflow/auditor-review' },
      { label: 'Reports', path: '/reports' },
    ],
  },
  Reviewer: {
    subtitle: 'Review achievements, add comments, score items, and request more information',
    stats: [
      { label: 'Items To Review', value: '9', tone: 'warning' },
      { label: 'Scored Reviews', value: '21', tone: 'success' },
      { label: 'Open RFIs', value: '4', tone: 'primary' },
      { label: 'Comments Added', value: '37', tone: 'default' },
    ],
    queues: ['Achievement review', 'Not-achieved RFI', 'Comment follow-up', 'Scoring backlog'],
    reports: ['Review workload', 'Scoring summary', 'Returned item report'],
    quickActions: [
      { label: 'PMS Review', path: '/workflow/pms-review' },
      { label: 'Reports', path: '/reports' },
    ],
  },
  Approver: {
    subtitle: 'Approve, reject, and score submissions within the assigned scope',
    stats: [
      { label: 'Approvals Pending', value: '7', tone: 'warning' },
      { label: 'Approved This Period', value: '18', tone: 'success' },
      { label: 'Scoped KPIs', value: '10', tone: 'primary' },
      { label: 'Escalations', value: '2', tone: 'default' },
    ],
    queues: ['Approval queue', 'Rejected items', 'Scoring tasks', 'Scope exceptions'],
    reports: ['Approval summary', 'KPI approval trend', 'Scope-based approval report'],
    quickActions: [
      { label: 'Approval Queue', path: '/workflow/approval' },
      { label: 'OPMS Targets', path: '/opms/targets' },
    ],
  },
  Verifier: {
    subtitle: 'Verify, verify-reject, edit scoped submissions, and upload supporting evidence',
    stats: [
      { label: 'Verification Queue', value: '13', tone: 'warning' },
      { label: 'Verified Items', value: '31', tone: 'success' },
      { label: 'Scoped Units', value: '3', tone: 'primary' },
      { label: 'Returned Items', value: '2', tone: 'default' },
    ],
    queues: ['Verification queue', 'Verify-reject items', 'Actual corrections', 'POE additions'],
    reports: ['Verification summary', 'Returned submission trend', 'Scoped workload report'],
    quickActions: [
      { label: 'Verification Queue', path: '/workflow/verification' },
      { label: 'OPMS Submissions', path: '/opms/submissions' },
    ],
  },
  Submitter: {
    subtitle: 'View assigned KPIs, submit actuals, upload POEs, and manage scoped work',
    stats: [
      { label: 'Assigned KPIs', value: '11', tone: 'primary' },
      { label: 'Draft Submissions', value: '4', tone: 'warning' },
      { label: 'Submitted POEs', value: '17', tone: 'success' },
      { label: 'Notifications', value: '6', tone: 'default' },
    ],
    queues: ['My submissions', 'POE upload tasks', 'Returned items', 'Scoring tasks'],
    reports: ['My KPI progress', 'Submission status report'],
    quickActions: [
      { label: 'My Queue', path: '/workflow/my-queue' },
      { label: 'OPMS Submissions', path: '/opms/submissions' },
    ],
  },
};

function toneClass(tone: DashboardCard['tone']) {
  switch (tone) {
    case 'primary':
      return 'text-primary-600';
    case 'success':
      return 'text-success-600';
    case 'warning':
      return 'text-warning-600';
    default:
      return 'text-secondary-900 dark:text-white';
  }
}

export function Dashboard() {
  const { roles, userProfile, setCurrentPath } = useApp();
  const primaryRole = roles[0] ?? 'Submitter';
  const config = dashboardConfig[primaryRole] ?? dashboardConfig.Submitter;

  return (
    <AppShell title={`${primaryRole} Dashboard`} subtitle={config.subtitle}>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-secondary-900 dark:text-white">
              Welcome back, {userProfile?.firstName}
            </h2>
            <p className="text-secondary-500 dark:text-secondary-400">
              {userProfile?.department ? `${userProfile.department} • ` : ''}{primaryRole}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="success">Active Role</Badge>
            <Badge>{primaryRole}</Badge>
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {config.stats.map(stat => (
            <Card key={stat.label}>
              <p className="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400">{stat.label}</p>
              <p className={`mt-3 text-3xl font-bold ${toneClass(stat.tone)}`}>{stat.value}</p>
            </Card>
          ))}
        </div>
        <div className="grid gap-6 lg:grid-cols-3">
          <Card>
            <div className="flex items-center gap-2">
              <Workflow className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Work Queue</h3>
            </div>
            <div className="mt-4 space-y-3">
              {config.queues.map(item => (
                <div key={item} className="rounded-lg border border-secondary-200 px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:text-secondary-300">
                  {item}
                </div>
              ))}
            </div>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Reports</h3>
            </div>
            <div className="mt-4 space-y-3">
              {config.reports.map(item => (
                <div key={item} className="rounded-lg border border-secondary-200 px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:text-secondary-300">
                  {item}
                </div>
              ))}
            </div>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <Bell className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Quick Actions</h3>
            </div>
            <div className="mt-4 flex flex-col gap-2">
              {config.quickActions.map(action => (
                <Button key={action.label} variant="outline" onClick={() => setCurrentPath(action.path)}>
                  {action.label}
                </Button>
              ))}
            </div>
          </Card>
        </div>
        <div className="grid gap-6 lg:grid-cols-3">
          <Card className="lg:col-span-2">
            <div className="flex items-center gap-2">
              <Target className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Role Summary</h3>
            </div>
            <p className="mt-4 text-sm leading-6 text-secondary-600 dark:text-secondary-300">
              This dashboard variant is driven by the authenticated role returned from the backend. It prioritizes the work queue, quick actions, reports, and governance visibility expected for <span className="font-semibold text-secondary-900 dark:text-white">{primaryRole}</span>.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              {roles.map(role => (
                <Badge key={role} variant={role === primaryRole ? 'primary' : 'default'}>{role}</Badge>
              ))}
            </div>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Security Context</h3>
            </div>
            <div className="mt-4 space-y-2 text-sm text-secondary-600 dark:text-secondary-300">
              <p><span className="font-semibold text-secondary-900 dark:text-white">Department:</span> {userProfile?.department ?? '-'}</p>
              <p><span className="font-semibold text-secondary-900 dark:text-white">Position:</span> {userProfile?.position ?? '-'}</p>
              <p><span className="font-semibold text-secondary-900 dark:text-white">Average Score:</span> {mockDashboardStats.averageScore.toFixed(1)}%</p>
            </div>
          </Card>
        </div>
      </div>
    </AppShell>
  );
}
