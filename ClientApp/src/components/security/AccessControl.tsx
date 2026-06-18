/* eslint-disable react-refresh/only-export-components */
import { useMemo } from 'react';
import { ShieldAlert } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Card } from '../ui';
import { useApp } from '../../context/AppContext';

const routePermissionMap: Array<{ match: (path: string) => boolean; permissions: string[] }> = [
  { match: (path) => path.startsWith('/opms/library'), permissions: ['OPMS.Library.View', 'OPMS.Library.Create', 'OPMS.Library.Edit', 'OPMS.Library.UseTemplate'] },
  { match: (path) => path.startsWith('/ipms/library'), permissions: ['IPMS.Library.View', 'IPMS.Library.Create', 'IPMS.Library.Edit', 'IPMS.Library.UseTemplate'] },
  { match: (path) => path.startsWith('/opms/targets'), permissions: ['OPMS.View', 'Targets.View', 'Targets.Manage'] },
  { match: (path) => path.startsWith('/ipms/targets'), permissions: ['IPMS.View', 'Targets.View', 'Targets.Manage'] },
  { match: (path) => path === '/opms/submissions', permissions: ['OPMS.View', 'Workflow.Submit.View', 'Workflow.Verify.View', 'Workflow.Approve.View', 'Workflow.Review.View', 'Workflow.Audit.View'] },
  { match: (path) => path === '/ipms/submissions', permissions: ['IPMS.View', 'Workflow.Submit.View', 'Workflow.Verify.View', 'Workflow.Approve.View', 'Workflow.Review.View', 'Workflow.Audit.View'] },
  { match: (path) => path.startsWith('/workflow/verification'), permissions: ['Workflow.Verify.View'] },
  { match: (path) => path.startsWith('/workflow/approval'), permissions: ['Workflow.Approve.View'] },
  { match: (path) => path.startsWith('/workflow/pms-review'), permissions: ['Workflow.Review.View'] },
  { match: (path) => path.startsWith('/workflow/auditor-review'), permissions: ['Workflow.Audit.View'] },
  { match: (path) => path.startsWith('/hr/departments'), permissions: ['Departments.View', 'Departments.Manage'] },
  { match: (path) => path.startsWith('/hr/units'), permissions: ['Units.View', 'Units.Manage'] },
  { match: (path) => path.startsWith('/hr/employees'), permissions: ['Admin.Users.Manage', 'UserDirectory.View'] },
  { match: (path) => path.startsWith('/system-administration/users'), permissions: ['Admin.Users.Manage'] },
  { match: (path) => path.startsWith('/system-administration/roles'), permissions: ['Admin.Roles.Manage'] },
  { match: (path) => path.startsWith('/system-administration/permissions'), permissions: ['Admin.Permissions.Manage'] },
  { match: (path) => path.startsWith('/system-administration/audit-logs'), permissions: ['Audit.LoginLogs.View', 'Audit.Logs.View'] },
  { match: (path) => path.startsWith('/system-administration/role-implementation-audit'), permissions: ['RoleImplementationAudit.View'] },
  { match: (path) => path.startsWith('/system-administration/role-access-matrix'), permissions: ['SystemAdministration.RoleAccessMatrix.View'] },
  { match: (path) => path.startsWith('/system-administration/permission-simulation'), permissions: ['SystemAdministration.PermissionSimulation.View', 'Admin.Users.Manage'] },
  { match: (path) => path.startsWith('/system-administration/system-coverage-audit'), permissions: ['SystemAdministration.SystemCoverageAudit.View'] },
  { match: (path) => path.startsWith('/reports'), permissions: ['Reports.View', 'Reports.Department.View', 'Reports.Approval.View', 'Reports.Verification.View', 'Reports.InternalAudit.View', 'Audit.Reports.View'] },
  { match: (path) => path.startsWith('/settings'), permissions: ['Notifications.View', 'Notifications.Manage'] },
];

function hasPermissionCode(permissionCodes: string[], granted: string[]) {
  return permissionCodes.some(code => granted.some(grantedCode => grantedCode.toLowerCase() === code.toLowerCase()));
}

export function useHasPermission(code: string) {
  const { permissions, isSuperAdmin } = useApp();
  return useMemo(() => isSuperAdmin || hasPermissionCode([code], permissions), [code, isSuperAdmin, permissions]);
}

export function useHasAnyPermission(codes: string[]) {
  const { permissions, isSuperAdmin } = useApp();
  return useMemo(() => isSuperAdmin || hasPermissionCode(codes, permissions), [codes, isSuperAdmin, permissions]);
}

export function useCanAccessPath(path: string) {
  const { permissions, isSuperAdmin } = useApp();
  return useMemo(() => {
    if (isSuperAdmin || path === '/dashboard') return true;
    const rule = routePermissionMap.find(item => item.match(path));
    if (!rule) return true;
    return hasPermissionCode(rule.permissions, permissions);
  }, [isSuperAdmin, path, permissions]);
}

export function AccessDeniedPage() {
  const { setCurrentPath } = useApp();

  return (
    <AppShell title="Access Denied" subtitle="Your role, permissions, or scope do not allow this page">
      <Card className="max-w-3xl">
        <div className="flex items-start gap-4">
          <div className="rounded-full bg-error-100 p-3 text-error-600 dark:bg-error-900/20 dark:text-error-400">
            <ShieldAlert className="h-6 w-6" />
          </div>
          <div className="space-y-3">
            <div>
              <h2 className="text-lg font-semibold text-secondary-900 dark:text-white">You do not have access to this route</h2>
              <p className="mt-1 text-sm text-secondary-500 dark:text-secondary-400">
                The page is protected by backend-driven permissions. If you believe this is incorrect, ask a Super Admin or Admin to review your role, scope, and assignments.
              </p>
            </div>
            <div className="flex gap-2">
              <Button variant="primary" onClick={() => setCurrentPath('/dashboard')}>
                Go To Dashboard
              </Button>
              <Button variant="outline" onClick={() => window.history.back()}>
                Go Back
              </Button>
            </div>
          </div>
        </div>
      </Card>
    </AppShell>
  );
}
