import { useEffect, useMemo, useState } from 'react';
import { CheckCircle2, Shield, XCircle } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Input, Select } from '../common/Form';
import { getPermissions, getRoleAccessMatrix, getSystemCoverageAudit, getUsers, simulateAccess } from '../../api/api';
import type { AccessSimulationResult, AdminPermission, AdminUserDetail, RoleAccessMatrixRow, SystemCoverageAuditRow } from '../../types';

function BooleanPill({ value }: { value: boolean }) {
  return value ? (
    <span className="inline-flex items-center gap-1 text-success-600 dark:text-success-400">
      <CheckCircle2 className="h-4 w-4" />
      Yes
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 text-error-600 dark:text-error-400">
      <XCircle className="h-4 w-4" />
      No
    </span>
  );
}

export function RoleAccessMatrixPage() {
  const [rows, setRows] = useState<RoleAccessMatrixRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      const result = await getRoleAccessMatrix();
      if (!active) return;
      if (result.success && result.data) {
        setRows(result.data);
        setError(null);
      } else {
        setRows([]);
        setError(result.message ?? 'Failed to load role access matrix.');
      }
      setLoading(false);
    };
    void load();
    return () => {
      active = false;
    };
  }, []);

  return (
    <AppShell title="Role Access Matrix" subtitle="Role permissions, scope profile, menus, actions, reports, and demo users">
      <Card className="overflow-hidden">
        {loading ? (
          <div className="px-6 py-8 text-sm text-secondary-500">Loading role access matrix...</div>
        ) : error ? (
          <div className="px-6 py-8 text-sm text-error-600">{error}</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-secondary-200 dark:divide-secondary-700">
              <thead className="bg-secondary-50 dark:bg-secondary-800/60">
                <tr>
                  {['Role', 'Permissions', 'Scope', 'Menus', 'Allowed Actions', 'Reports', 'Test User'].map(header => (
                    <th key={header} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">
                      {header}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-secondary-200 dark:divide-secondary-700">
                {rows.map(row => (
                  <tr key={row.role}>
                    <td className="px-4 py-4 text-sm font-semibold text-secondary-900 dark:text-white">{row.role}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.permissions.join(', ') || '-'}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.scope.join(', ') || '-'}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.menus.join(', ') || '-'}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.allowedActions.join(', ') || '-'}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.reports.join(', ') || '-'}</td>
                    <td className="px-4 py-4 text-xs text-secondary-600 dark:text-secondary-300">{row.testUser ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </AppShell>
  );
}

export function PermissionSimulationPage() {
  const [users, setUsers] = useState<AdminUserDetail[]>([]);
  const [permissions, setPermissions] = useState<AdminPermission[]>([]);
  const [result, setResult] = useState<AccessSimulationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    userId: '',
    permissionCode: '',
    departmentId: '',
    unitId: '',
    targetId: '',
    kpiId: '',
    projectId: '',
    taskId: '',
  });

  useEffect(() => {
    let active = true;
    const load = async () => {
      const [usersResult, permissionsResult] = await Promise.all([getUsers(), getPermissions()]);
      if (!active) return;
      setUsers(usersResult.data ?? []);
      setPermissions(permissionsResult.data ?? []);
    };
    void load();
    return () => {
      active = false;
    };
  }, []);

  const selectedUser = useMemo(
    () => users.find(item => item.user.id === form.userId) ?? null,
    [form.userId, users],
  );

  const simulate = async () => {
    if (!form.userId || !form.permissionCode) {
      setError('Select a user and permission to simulate.');
      return;
    }

    setLoading(true);
    setError(null);
    const response = await simulateAccess({
      userId: form.userId,
      permissionCode: form.permissionCode,
      departmentId: form.departmentId ? Number(form.departmentId) : null,
      unitId: form.unitId ? Number(form.unitId) : null,
      targetId: form.targetId || null,
      kpiId: form.kpiId || null,
      projectId: form.projectId || null,
      taskId: form.taskId || null,
    });
    if (response.success && response.data) {
      setResult(response.data);
    } else {
      setResult(null);
      setError(response.message ?? 'Simulation failed.');
    }
    setLoading(false);
  };

  return (
    <AppShell title="Permission Simulation" subtitle="Test whether a user can perform a scoped action and see why">
      <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <Card className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Select
              label="User"
              value={form.userId}
              onChange={(event) => setForm(prev => ({ ...prev, userId: event.target.value }))}
              options={users.map(item => ({ value: item.user.id, label: `${item.user.fullName} (${item.roles.map(role => role.name).join(', ') || 'No Role'})` }))}
              placeholder="Select user"
            />
            <Select
              label="Permission"
              value={form.permissionCode}
              onChange={(event) => setForm(prev => ({ ...prev, permissionCode: event.target.value }))}
              options={permissions.map(item => ({ value: item.code, label: item.code }))}
              placeholder="Select permission"
            />
            <Input label="Department Id" value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} />
            <Input label="Unit Id" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} />
            <Input label="Target Id" value={form.targetId} onChange={(event) => setForm(prev => ({ ...prev, targetId: event.target.value }))} />
            <Input label="KPI Id" value={form.kpiId} onChange={(event) => setForm(prev => ({ ...prev, kpiId: event.target.value }))} />
            <Input label="Project Id" value={form.projectId} onChange={(event) => setForm(prev => ({ ...prev, projectId: event.target.value }))} />
            <Input label="Task Id" value={form.taskId} onChange={(event) => setForm(prev => ({ ...prev, taskId: event.target.value }))} />
          </div>
          {error && <p className="text-sm text-error-600 dark:text-error-400">{error}</p>}
          <div className="flex justify-end">
            <Button variant="primary" onClick={simulate} disabled={loading}>
              {loading ? 'Simulating...' : 'Run Simulation'}
            </Button>
          </div>
        </Card>

        <Card className="space-y-4">
          <div className="flex items-center gap-3">
            <div className="rounded-full bg-primary-100 p-2 text-primary-600 dark:bg-primary-900/20 dark:text-primary-300">
              <Shield className="h-5 w-5" />
            </div>
            <div>
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Simulation Result</h3>
              <p className="text-xs text-secondary-500 dark:text-secondary-400">
                {selectedUser ? `${selectedUser.user.fullName} • ${selectedUser.roles.map(role => role.name).join(', ')}` : 'Choose a user to begin'}
              </p>
            </div>
          </div>
          {result ? (
            <div className="space-y-3 text-sm">
              <div className="flex items-center gap-2">
                <Badge variant={result.allowed ? 'success' : 'error'}>{result.allowed ? 'Allowed' : 'Denied'}</Badge>
                <span className="text-secondary-600 dark:text-secondary-300">{result.reason}</span>
              </div>
              <div>
                <p className="font-semibold text-secondary-900 dark:text-white">Matched Scopes</p>
                <p className="text-secondary-600 dark:text-secondary-300">{result.matchedScopes.join(', ') || '-'}</p>
              </div>
              <div>
                <p className="font-semibold text-secondary-900 dark:text-white">Matched Assignments</p>
                <p className="text-secondary-600 dark:text-secondary-300">{result.matchedAssignments.join(', ') || '-'}</p>
              </div>
              <div>
                <p className="font-semibold text-secondary-900 dark:text-white">Effective Permissions</p>
                <p className="text-secondary-600 dark:text-secondary-300">{result.effectivePermissions.join(', ') || '-'}</p>
              </div>
            </div>
          ) : (
            <p className="text-sm text-secondary-500 dark:text-secondary-400">No simulation has been run yet.</p>
          )}
        </Card>
      </div>
    </AppShell>
  );
}

export function SystemCoverageAuditPage() {
  const [rows, setRows] = useState<SystemCoverageAuditRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const load = async () => {
      setLoading(true);
      const response = await getSystemCoverageAudit();
      if (!active) return;
      if (response.success && response.data) {
        setRows(response.data);
        setError(null);
      } else {
        setRows([]);
        setError(response.message ?? 'Failed to load system coverage audit.');
      }
      setLoading(false);
    };
    void load();
    return () => {
      active = false;
    };
  }, []);

  const coveredCount = useMemo(
    () => rows.filter(row => Object.values(row).slice(1).every(Boolean)).length,
    [rows],
  );

  return (
    <AppShell title="System Coverage Audit" subtitle="Checks whether each EPMS role has seeded data, menus, permissions, scope filtering, workflow, and governance support">
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-3">
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Roles Audited</p>
            <p className="mt-2 text-3xl font-bold text-secondary-900 dark:text-white">{rows.length}</p>
          </Card>
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Fully Covered</p>
            <p className="mt-2 text-3xl font-bold text-success-600 dark:text-success-400">{coveredCount}</p>
          </Card>
          <Card>
            <p className="text-sm font-semibold text-secondary-500 dark:text-secondary-400">Remaining Gaps</p>
            <p className="mt-2 text-3xl font-bold text-warning-600 dark:text-warning-400">{Math.max(rows.length - coveredCount, 0)}</p>
          </Card>
        </div>

        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-secondary-200 px-6 py-4 dark:border-secondary-700">
            <div>
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Coverage Matrix</h3>
              <p className="mt-1 text-sm text-secondary-500 dark:text-secondary-400">
                Verifies the role-by-role implementation footprint across the current backend and frontend foundation.
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={() => window.location.reload()}>
              Refresh
            </Button>
          </div>
          {loading ? (
            <div className="px-6 py-8 text-sm text-secondary-500 dark:text-secondary-400">Loading coverage audit...</div>
          ) : error ? (
            <div className="px-6 py-8 text-sm text-error-600 dark:text-error-400">{error}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-secondary-200 dark:divide-secondary-700">
                <thead className="bg-secondary-50 dark:bg-secondary-800/60">
                  <tr>
                    {['Role', 'Seeded User', 'Dashboard', 'Menu', 'Permissions', 'Scope Filtering', 'CRUD', 'Workflow', 'Reports', 'Audit Trail', 'Notifications'].map(header => (
                      <th key={header} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">
                        {header}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-secondary-200 dark:divide-secondary-700">
                  {rows.map(row => (
                    <tr key={row.role}>
                      <td className="px-4 py-4 text-sm font-semibold text-secondary-900 dark:text-white">{row.role}</td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.seededUser} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.dashboard} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.menu} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.permissions} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.scopeFiltering} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.crud} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.workflowActions} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.reports} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.auditTrail} /></td>
                      <td className="px-4 py-4 text-sm"><BooleanPill value={row.notifications} /></td>
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
