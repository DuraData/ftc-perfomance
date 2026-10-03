/* eslint-disable react-refresh/only-export-components */
import { useMemo } from 'react';
import { ShieldAlert } from 'lucide-react';
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
 * Nested detail routes inherit access from their nearest authorized menu route;
 * operation buttons and the API still enforce their specific CRUD permission.
 */
export function canAccessPath(path: string, menuItems: MenuItem[]) {
  const requested = normalizePath(path);
  return collectAuthorizedPaths(menuItems).some(authorized =>
    requested === authorized || requested.startsWith(`${authorized}/`));
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
  const { menuItems } = useApp();
  return useMemo(() => canAccessPath(path, menuItems), [path, menuItems]);
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
