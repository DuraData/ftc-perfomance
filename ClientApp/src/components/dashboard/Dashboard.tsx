import { Compass, Shield, UserRound } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { useApp } from '../../context/AppContext';
import type { MenuItem } from '../../types';

type NavigationLink = { label: string; path: string };

function flattenNavigation(items: MenuItem[]): NavigationLink[] {
  return items.flatMap(item => [
    ...(item.path && item.path !== '/dashboard' ? [{ label: item.label, path: item.path }] : []),
    ...flattenNavigation(item.children ?? []),
  ]);
}

export function Dashboard() {
  const { menuItems, permissions, roles, userProfile, setCurrentPath } = useApp();
  const navigation = flattenNavigation(menuItems);
  const quickActions = navigation.slice(0, 8);

  return (
    <AppShell title="Dashboard" subtitle="Your tenant-scoped access and available work areas">
      <div className="space-y-6">
        <Card>
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-xl font-bold text-secondary-900 dark:text-white">
                Welcome back, {userProfile?.firstName ?? 'user'}
              </h2>
              <p className="mt-1 text-secondary-500 dark:text-secondary-400">
                {userProfile?.department ?? 'No department assigned'}
                {userProfile?.position ? ` • ${userProfile.position}` : ''}
              </p>
            </div>
            <Badge variant="success">Tenant access loaded</Badge>
          </div>
        </Card>

        <div className="grid gap-6 lg:grid-cols-3">
          <Card className="lg:col-span-2">
            <div className="flex items-center gap-2">
              <Compass className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Available work areas</h3>
            </div>
            {quickActions.length > 0 ? (
              <div className="mt-4 grid gap-2 sm:grid-cols-2">
                {quickActions.map(action => (
                  <Button key={`${action.path}-${action.label}`} variant="outline" onClick={() => setCurrentPath(action.path)}>
                    {action.label}
                  </Button>
                ))}
              </div>
            ) : (
              <p className="mt-4 text-sm text-secondary-500 dark:text-secondary-400">
                No work areas are currently assigned. Ask a security administrator to review your roles and scopes.
              </p>
            )}
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <UserRound className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Assigned roles</h3>
            </div>
            <div className="mt-4 flex flex-wrap gap-2">
              {roles.length > 0
                ? roles.map(role => <Badge key={role}>{role}</Badge>)
                : <span className="text-sm text-secondary-500">No active roles</span>}
            </div>
          </Card>
        </div>

        <Card>
          <div className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-primary-600" />
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Effective security context</h3>
          </div>
          <p className="mt-3 text-sm leading-6 text-secondary-600 dark:text-secondary-300">
            Navigation and operation access are calculated by the server for the active municipality. You currently have
            {' '}<span className="font-semibold text-secondary-900 dark:text-white">{permissions.length}</span> effective permission
            {permissions.length === 1 ? '' : 's'} across <span className="font-semibold text-secondary-900 dark:text-white">{navigation.length}</span> available page
            {navigation.length === 1 ? '' : 's'}.
          </p>
        </Card>
      </div>
    </AppShell>
  );
}
