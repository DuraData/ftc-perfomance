import React, { useEffect, useMemo, useState } from 'react';
import { CheckCircle2, Clock3, XCircle } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Card, Badge, Button } from '../ui';
import type { RoleImplementationAuditRow } from '../../types';
import { getRoleImplementationAudit } from '../../api/api';

type AuditColumnKey = Exclude<keyof RoleImplementationAuditRow, 'role' | 'complete'>;

const columnLabels: Record<AuditColumnKey, string> = {
  dashboard: 'Dashboard',
  menus: 'Menus',
  crud: 'CRUD',
  scopeFiltering: 'Scope Filtering',
  notifications: 'Notifications',
  reports: 'Reports',
  auditTrail: 'Audit Trail',
};

function AuditCell({ value }: { value: boolean }) {
  return value ? (
    <span className="inline-flex items-center gap-1 text-success-600 dark:text-success-400">
      <CheckCircle2 className="h-4 w-4" />
      Yes
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 text-secondary-500 dark:text-secondary-400">
      <Clock3 className="h-4 w-4" />
      No
    </span>
  );
}

export function RoleImplementationAuditPage() {
  const [rows, setRows] = useState<RoleImplementationAuditRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const completedCount = useMemo(
    () => rows.filter(row => row.complete).length,
    [rows],
  );

  useEffect(() => {
    let active = true;

    const load = async () => {
      setLoading(true);
      setError(null);
      const result = await getRoleImplementationAudit();
      if (!active) return;

      if (!result.success || !result.data) {
        setError(result.message ?? 'Failed to load role implementation audit.');
        setRows([]);
      } else {
        setRows(result.data);
      }

      setLoading(false);
    };

    void load();
    return () => {
      active = false;
    };
  }, []);

  return (
    <AppShell
      title="Role Implementation Audit"
      subtitle="Progress matrix for the new Roles + Scopes + Assignments security model"
    >
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-3">
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Roles Tracked</p>
            <p className="mt-2 text-3xl font-bold text-secondary-900 dark:text-white">{rows.length}</p>
          </Card>
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Complete</p>
            <p className="mt-2 text-3xl font-bold text-success-600 dark:text-success-400">{completedCount}</p>
          </Card>
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Pending</p>
            <p className="mt-2 text-3xl font-bold text-warning-600 dark:text-warning-400">{Math.max(rows.length - completedCount, 0)}</p>
          </Card>
        </div>

        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-secondary-200 px-6 py-4 dark:border-secondary-700">
            <div>
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Implementation Matrix</h3>
              <p className="mt-1 text-sm text-secondary-500 dark:text-secondary-400">
                Tracks dashboards, menus, CRUD, scopes, notifications, reports, and audit support for each role.
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={() => window.location.reload()}>
              Refresh
            </Button>
          </div>

          {loading ? (
            <div className="px-6 py-8 text-sm text-secondary-500 dark:text-secondary-400">Loading role implementation audit...</div>
          ) : error ? (
            <div className="px-6 py-8 text-sm text-error-600 dark:text-error-400">{error}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-secondary-200 dark:divide-secondary-700">
                <thead className="bg-secondary-50 dark:bg-secondary-800/60">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">Role</th>
                    {Object.entries(columnLabels).map(([key, label]) => (
                      <th key={key} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">
                        {label}
                      </th>
                    ))}
                    <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">Complete</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-secondary-200 bg-white dark:divide-secondary-700 dark:bg-secondary-900">
                  {rows.map(row => (
                    <tr key={row.role}>
                      <td className="whitespace-nowrap px-6 py-4 text-sm font-semibold text-secondary-900 dark:text-white">{row.role}</td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.dashboard} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.menus} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.crud} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.scopeFiltering} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.notifications} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.reports} /></td>
                      <td className="px-4 py-4 text-sm"><AuditCell value={row.auditTrail} /></td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm">
                        {row.complete ? (
                          <Badge variant="success">Complete</Badge>
                        ) : (
                          <span className="inline-flex items-center gap-1 text-warning-600 dark:text-warning-400">
                            <XCircle className="h-4 w-4" />
                            In Progress
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
