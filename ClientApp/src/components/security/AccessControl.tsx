/* eslint-disable react-refresh/only-export-components */
import { useMemo } from 'react';
import { FileQuestion, ShieldAlert } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Card } from '../ui';
import { useApp } from '../../context/AppContext';
import type { MenuItem } from '../../types';

export function hasPermissionCode(permissionCodes: string[], granted: string[]) {
  return permissionCodes.some(code => granted.some(grantedCode => grantedCode.toLowerCase() === code.toLowerCase()));
}

function normalizePath(path: string) {
  const normalized = path.split(/[?#]/, 1)[0].replace(/\/+$/, '');
  return normalized || '/';
}

function collectAuthorizedPaths(items: MenuItem[]): string[] {
  return items.flatMap(item => [
    ...(item.path ? [normalizePath(item.path)] : []),
    ...collectAuthorizedPaths(item.children ?? []),
  ]);
}

/**
 * Page access is derived from the tenant-filtered menu returned by the API.
 * Nested read routes inherit access from their nearest authorized menu route.
 * Known mutation routes additionally require their matching CRUD permission;
 * operation buttons and the API remain independent enforcement layers.
 */
const mutationRoutePermissions: Array<{ matches: (path: string) => boolean; permissions: string[] }> = [
  { matches: path => path === '/opms/targets/new', permissions: ['OPMS_KPI.CREATE'] },
  { matches: path => /^\/opms\/targets\/[^/]+\/edit$/.test(path), permissions: ['OPMS_KPI.UPDATE'] },
  { matches: path => path === '/ipms/targets/new', permissions: ['IPMS_KPI.CREATE'] },
  { matches: path => /^\/ipms\/targets\/[^/]+\/edit$/.test(path), permissions: ['IPMS_KPI.UPDATE'] },
  { matches: path => path === '/opms/library/new', permissions: ['OPMS_TARGET_TEMPLATE.CREATE'] },
  { matches: path => /^\/opms\/library\/[^/]+\/edit$/.test(path), permissions: ['OPMS_TARGET_TEMPLATE.UPDATE'] },
  { matches: path => path === '/ipms/library/new', permissions: ['IPMS_TARGET_TEMPLATE.CREATE'] },
  { matches: path => /^\/ipms\/library\/[^/]+\/edit$/.test(path), permissions: ['IPMS_TARGET_TEMPLATE.UPDATE'] },
];

const routeAccessParents: Record<string, string> = {
  '/configuration/workflow': '/admin/approval-setup',
  '/workflow/my-drafts': '/workflow/my-queue',
  '/workflow/pending-submission': '/workflow/my-queue',
  '/workflow/returned-submissions': '/workflow/my-queue',
  '/workflow/under-verification': '/workflow/my-queue',
  '/workflow/under-review': '/workflow/my-queue',
  '/workflow/under-approval': '/workflow/my-queue',
  '/workflow/internal-audit-returned': '/workflow/my-queue',
  '/workflow/approved-closed': '/workflow/my-queue',
};

export function canAccessPath(path: string, menuItems: MenuItem[], grantedPermissions: string[] = []) {
  const requested = normalizePath(path);
  const accessPath = routeAccessParents[requested] ?? requested;
  const hasAuthorizedParent = collectAuthorizedPaths(menuItems).some(authorized =>
    accessPath === authorized || accessPath.startsWith(`${authorized}/`));

  if (!hasAuthorizedParent) {
    return false;
  }

  const mutationRoute = mutationRoutePermissions.find(rule => rule.matches(requested));
  return !mutationRoute || hasPermissionCode(mutationRoute.permissions, grantedPermissions);
}

export function useHasPermission(code: string) {
  const { permissions } = useApp();
  return useMemo(() => hasPermissionCode([code], permissions), [code, permissions]);
}

export function useHasAnyPermission(codes: string[]) {
  const { permissions } = useApp();
  return useMemo(() => hasPermissionCode(codes, permissions), [codes, permissions]);
}

export function useCanAccessPath(path: string) {
  const { menuItems, permissions } = useApp();
  return useMemo(() => canAccessPath(path, menuItems, permissions), [path, menuItems, permissions]);
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
                The page is protected by backend-driven permissions. If you believe this is incorrect, ask an authorised security administrator to review your role, scope, and assignments.
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

export function NotFoundPage() {
  const { setCurrentPath } = useApp();

  return (
    <AppShell title="Page Not Found" subtitle="The requested application route does not exist">
      <Card className="max-w-3xl">
        <div className="flex items-start gap-4">
          <div className="rounded-full bg-secondary-100 p-3 text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300">
            <FileQuestion className="h-6 w-6" aria-hidden="true" />
          </div>
          <div className="space-y-3">
            <div>
              <h2 className="text-lg font-semibold text-secondary-900 dark:text-white">Page not found</h2>
              <p className="mt-1 text-sm text-secondary-500 dark:text-secondary-400">
                The address does not match a registered OPMS workspace. No substitute page was loaded.
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
