import { useEffect, useMemo, useState } from 'react';
import {
  getEffectiveSecurityPreview,
  getRoleSecurityConfiguration,
  getSecurityRolesPage,
  getSecurityPermissionDefinitionsPage,
  getSecurityUsersPage,
  getSecurityUserRoles,
  createSecurityRole,
  saveRoleSecurityConfiguration,
  saveSecurityUserRoles,
  updateSecurityRole,
} from '../../api/api';
import type {
  SecurityRoleSummary,
  SecurityUserSummary,
  SecurityUserRoleConfiguration,
  EffectiveSecurityPreview,
  RoleSecurityPermission,
  SecurityPermissionDefinition,
  SecurityPermissionState,
} from '../../types';
import { NavigationRegistryEditor } from './NavigationRegistryEditor';
import { SecurityRegistryEditor } from './SecurityRegistryEditor';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';

type EditableRule = Pick<RoleSecurityPermission, 'permissionCode' | 'state' | 'scopeType'>;
type AssignmentDraft = { rolePublicId: string; municipalityId?: number; departmentId?: number; departmentPublicId?: string; departmentName?: string; unitId?: number; unitPublicId?: string; unitName?: string; effectiveFrom: string; effectiveTo?: string };
const scopes = ['', 'Self', 'AssignedKpiScope', 'AssignedTargetScope', 'AssignedProjectScope', 'AssignedTaskScope', 'UnitScope', 'DepartmentScope', 'InstitutionScope', 'System'];
const kinds = ['Resource', 'Navigation', 'Member', 'Action', 'Report'] as const;
const toLocalDateTime = (value?: string) => value ? new Date(new Date(value).getTime() - new Date(value).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';

export function SecurityAdministrationPage() {
  const [roles, setRoles] = useState<SecurityRoleSummary[]>([]);
  const [rolePage, setRolePage] = useState(1);
  const [roleTotalPages, setRoleTotalPages] = useState(0);
  const [roleSearch, setRoleSearch] = useState('');
  const [users, setUsers] = useState<SecurityUserSummary[]>([]);
  const [userPage, setUserPage] = useState(1);
  const [userTotalPages, setUserTotalPages] = useState(0);
  const [userSearch, setUserSearch] = useState('');
  const [definitions, setDefinitions] = useState<SecurityPermissionDefinition[]>([]);
  const [definitionPage, setDefinitionPage] = useState(1);
  const [definitionTotalCount, setDefinitionTotalCount] = useState(0);
  const [definitionTotalPages, setDefinitionTotalPages] = useState(0);
  const [definitionSearch, setDefinitionSearch] = useState('');
  const [definitionRefreshToken, setDefinitionRefreshToken] = useState(0);
  const [roleId, setRoleId] = useState('');
  const [roleVersion, setRoleVersion] = useState('');
  const [rules, setRules] = useState<Record<string, EditableRule>>({});
  const [kind, setKind] = useState<(typeof kinds)[number]>('Resource');
  const [previewUserId, setPreviewUserId] = useState('');
  const [preview, setPreview] = useState<EffectiveSecurityPreview | null>(null);
  const [userRoles, setUserRoles] = useState<SecurityUserRoleConfiguration | null>(null);
  const [assignmentDrafts, setAssignmentDrafts] = useState<AssignmentDraft[]>([]);
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const [newRoleCode, setNewRoleCode] = useState('');
  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleDescription, setNewRoleDescription] = useState('');
  const [roleName, setRoleName] = useState('');
  const [roleDescription, setRoleDescription] = useState('');
  const [roleActive, setRoleActive] = useState(true);

  useEffect(() => {
    void getSecurityRolesPage({ page: rolePage, pageSize: 25, search: roleSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(roleResult => {
      const loadedRoles = roleResult.data?.items ?? [];
      setRoles(loadedRoles);
      setRoleTotalPages(roleResult.data?.totalPages ?? 0);
      setRoleId(current => loadedRoles.some(role => role.publicId === current) ? current : (loadedRoles[0]?.publicId ?? ''));
    });
  }, [rolePage, roleSearch]);

  useEffect(() => {
    void getSecurityUsersPage({ page: userPage, pageSize: 25, search: userSearch, sortBy: 'name', sortDirection: 'asc' }).then(result => {
      setUsers(result.data?.items ?? []);
      setUserTotalPages(result.data?.totalPages ?? 0);
    });
  }, [userPage, userSearch]);

  const selectedRole = roles.find(role => role.publicId === roleId);

  useEffect(() => {
    setRoleName(selectedRole?.name ?? '');
    setRoleDescription(selectedRole?.description ?? '');
    setRoleActive(selectedRole?.isActive ?? true);
  }, [selectedRole]);

  useEffect(() => {
    if (!roleId) return;
    setBusy(true);
    setMessage('');
    void getRoleSecurityConfiguration(roleId).then(result => {
      if (!result.success || !result.data) {
        setMessage(result.message ?? 'Unable to load role security.');
        return;
      }
      setRoleVersion(result.data.roleRowVersion);
      setRules(Object.fromEntries(result.data.permissions.map(rule => [rule.permissionCode, {
        permissionCode: rule.permissionCode,
        state: rule.state,
        scopeType: rule.scopeType,
      }])));
    }).finally(() => setBusy(false));
  }, [roleId]);

  useEffect(() => {
    void getSecurityPermissionDefinitionsPage({ page: definitionPage, pageSize: 25, search: definitionSearch, sortBy: 'code', sortDirection: 'asc' }, [kind]).then(result => {
      setDefinitions(result.data?.items ?? []);
      setDefinitionTotalCount(result.data?.totalCount ?? 0);
      setDefinitionTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) setMessage(result.message ?? 'Permission definitions could not be loaded.');
    });
  }, [definitionPage, definitionSearch, definitionRefreshToken, kind]);

  const visibleDefinitions = useMemo(() => definitions, [definitions]);

  const setState = (code: string, state: '' | SecurityPermissionState) => {
    setRules(current => {
      const next = { ...current };
      if (!state) delete next[code];
      else next[code] = { permissionCode: code, state, scopeType: next[code]?.scopeType };
      return next;
    });
  };

  const setScope = (code: string, scopeType: string) => {
    setRules(current => ({
      ...current,
      [code]: { permissionCode: code, state: current[code]?.state ?? 'ALLOW', scopeType: scopeType || undefined },
    }));
  };

  const save = async () => {
    if (!roleId || !roleVersion) return;
    setBusy(true);
    setMessage('');
    const result = await saveRoleSecurityConfiguration(roleId, roleVersion, Object.values(rules));
    if (!result.success) {
      setMessage(result.message ?? 'Security configuration could not be saved. Refresh before retrying.');
      setBusy(false);
      return;
    }
    const refreshed = await getRoleSecurityConfiguration(roleId);
    if (refreshed.data) setRoleVersion(refreshed.data.roleRowVersion);
    setMessage('Security configuration saved and audited. Affected users must refresh their session.');
    setBusy(false);
  };

  const createRole = async () => {
    setBusy(true); setMessage('');
    const result = await createSecurityRole({ roleCode: newRoleCode, name: newRoleName, description: newRoleDescription || undefined });
    if (!result.success || !result.data) {
      setMessage(result.message ?? 'Role could not be created.'); setBusy(false); return;
    }
    setRoleSearch(result.data.roleCode);
    setRolePage(1);
    setRoleId(result.data.publicId);
    setNewRoleCode(''); setNewRoleName(''); setNewRoleDescription('');
    setMessage('Tenant role created and audited. Configure its permissions below.');
    setBusy(false);
  };

  const saveRoleDetails = async () => {
    if (!selectedRole) return;
    setBusy(true); setMessage('');
    const result = await updateSecurityRole(selectedRole, { name: roleName, description: roleDescription || undefined, isActive: roleActive, effectiveFrom: selectedRole.effectiveFrom, effectiveTo: selectedRole.effectiveTo });
    if (!result.success || !result.data) {
      setMessage(result.message ?? 'Role details could not be saved.'); setBusy(false); return;
    }
    setRoles(current => current.map(role => role.publicId === result.data!.publicId ? result.data! : role));
    setMessage('Role details saved and audited.');
    setBusy(false);
  };

  const loadPreview = async () => {
    if (!previewUserId) return;
    const result = await getEffectiveSecurityPreview(previewUserId);
    setPreview(result.data ?? null);
    if (!result.success) setMessage(result.message ?? 'Unable to calculate effective permissions.');
  };

  const loadUserRoles = async (userPublicId: string) => {
    setPreviewUserId(userPublicId);
    setPreview(null);
    if (!userPublicId) { setUserRoles(null); setAssignmentDrafts([]); return; }
    const result = await getSecurityUserRoles(userPublicId);
    setUserRoles(result.data ?? null);
    setAssignmentDrafts(result.data?.assignments.map(item => ({ rolePublicId: item.rolePublicId, municipalityId: item.municipalityId, departmentId: item.departmentId, departmentPublicId: item.departmentPublicId, departmentName: item.departmentName, unitId: item.unitId, unitPublicId: item.unitPublicId, unitName: item.unitName, effectiveFrom: item.effectiveFrom, effectiveTo: item.effectiveTo })) ?? []);
  };

  const saveUserRoles = async () => {
    if (!userRoles) return;
    setBusy(true);
    const invalid = assignmentDrafts.find(item => item.effectiveTo && new Date(item.effectiveTo) <= new Date(item.effectiveFrom));
    if (invalid) { setMessage('Every assignment end must be later than its start.'); return; }
    const result = await saveSecurityUserRoles(userRoles.userPublicId, userRoles, assignmentDrafts.map(item => ({ ...item, effectiveFrom: new Date(item.effectiveFrom).toISOString(), effectiveTo: item.effectiveTo ? new Date(item.effectiveTo).toISOString() : undefined })));
    setMessage(result.success ? 'User role assignments saved, effective immediately, and audited.' : result.message ?? 'Role assignments could not be saved.');
    if (result.success) await loadUserRoles(userRoles.userPublicId);
    setBusy(false);
  };

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Security Administration</h1>
        <p className="mt-1 text-sm text-gray-600">Configure deny-by-default navigation, CRUD, field, action and scope rules. Explicit deny overrides allow.</p>
      </div>

      <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="grid gap-5 lg:grid-cols-2">
          <div>
            <h2 className="font-semibold text-gray-900">Role details</h2>
            <input aria-label="Search security roles" placeholder="Search role code, name or description" className="mt-3 w-full rounded border border-gray-300 p-2" value={roleSearch} onChange={event => { setRoleSearch(event.target.value); setRolePage(1); }} />
            <label className="mt-3 block text-sm font-medium text-gray-700" htmlFor="security-role">Role</label>
            <select id="security-role" className="mt-1 w-full rounded border border-gray-300 p-2" value={roleId} onChange={event => setRoleId(event.target.value)}>
              {roles.map(role => <option key={role.publicId} value={role.publicId}>{role.name}{role.isSystemRole ? ' (system)' : ''}</option>)}
            </select>
            {roleTotalPages > 1 && <div className="mt-2 flex items-center gap-2 text-sm text-gray-600"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={rolePage <= 1} onClick={() => setRolePage(value => Math.max(1, value - 1))}>Previous roles</button><span>Page {rolePage} of {roleTotalPages}</span><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={rolePage >= roleTotalPages} onClick={() => setRolePage(value => value + 1)}>Next roles</button></div>}
            {selectedRole && <div className="mt-3 grid gap-2">
              <div className="rounded bg-gray-50 px-3 py-2 font-mono text-xs text-gray-600">{selectedRole.roleCode}</div>
              <input aria-label="Role name" className="rounded border border-gray-300 p-2" value={roleName} onChange={event => setRoleName(event.target.value)} disabled={selectedRole.isSystemRole} />
              <textarea aria-label="Role description" className="rounded border border-gray-300 p-2" value={roleDescription} onChange={event => setRoleDescription(event.target.value)} disabled={selectedRole.isSystemRole} />
              <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={roleActive} onChange={event => setRoleActive(event.target.checked)} disabled={selectedRole.isSystemRole} /> Active</label>
              <button type="button" onClick={() => void saveRoleDetails()} disabled={busy || selectedRole.isSystemRole} className="w-fit rounded border border-blue-700 px-4 py-2 text-sm text-blue-700 disabled:opacity-50">Save role details</button>
            </div>}
          </div>
          <div>
            <h2 className="font-semibold text-gray-900">Create tenant role</h2>
            <div className="mt-3 grid gap-2">
              <input aria-label="New role code" placeholder="DEPARTMENT_SUBMITTER" className="rounded border border-gray-300 p-2 font-mono uppercase" value={newRoleCode} onChange={event => setNewRoleCode(event.target.value.toUpperCase())} />
              <input aria-label="New role name" placeholder="Department Submitter" className="rounded border border-gray-300 p-2" value={newRoleName} onChange={event => setNewRoleName(event.target.value)} />
              <textarea aria-label="New role description" placeholder="Role purpose" className="rounded border border-gray-300 p-2" value={newRoleDescription} onChange={event => setNewRoleDescription(event.target.value)} />
              <button type="button" onClick={() => void createRole()} disabled={busy || newRoleCode.trim().length < 3 || newRoleName.trim().length < 3} className="w-fit rounded bg-blue-700 px-4 py-2 text-sm font-medium text-white disabled:opacity-50">Create role</button>
            </div>
          </div>
        </div>
      </section>

      <section className="rounded-lg border border-gray-200 bg-white shadow-sm">
        <div className="flex flex-wrap border-b border-gray-200 px-4 pt-3">
          {kinds.map(item => (
            <button key={item} type="button" onClick={() => { setKind(item); setDefinitionPage(1); }} className={`mr-2 border-b-2 px-3 py-2 text-sm ${kind === item ? 'border-blue-600 font-medium text-blue-700' : 'border-transparent text-gray-600'}`}>
              {item === 'Resource' ? 'Entity / CRUD' : item}
            </button>
          ))}
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 p-4">
          <input aria-label="Search permission definitions" placeholder={`Search ${kind.toLowerCase()} permissions`} className="min-w-72 flex-1 rounded border border-gray-300 p-2" value={definitionSearch} onChange={event => { setDefinitionSearch(event.target.value); setDefinitionPage(1); }} />
          <span className="text-sm text-gray-600">{definitionTotalCount} permissions · Page {definitionPage} of {Math.max(definitionTotalPages, 1)}</span>
          <div className="flex gap-2"><button type="button" className="rounded border px-2 py-1 text-sm disabled:opacity-50" disabled={definitionPage <= 1} onClick={() => setDefinitionPage(value => Math.max(1, value - 1))}>Previous permissions</button><button type="button" className="rounded border px-2 py-1 text-sm disabled:opacity-50" disabled={definitionPage >= definitionTotalPages} onClick={() => setDefinitionPage(value => value + 1)}>Next permissions</button></div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50"><tr><th className="px-4 py-3 text-left">Permission</th><th className="px-4 py-3 text-left">Resource / Member</th><th className="px-4 py-3 text-left">Decision</th><th className="px-4 py-3 text-left">Record scope</th></tr></thead>
            <tbody className="divide-y divide-gray-100">
              {visibleDefinitions.map(definition => {
                const rule = rules[definition.code];
                return <tr key={definition.code}>
                  <td className="px-4 py-3"><div className="font-mono text-xs font-medium">{definition.code}</div><div className="text-gray-500">{definition.description}</div></td>
                  <td className="px-4 py-3 text-gray-700">{definition.resourceCode ?? definition.navigationCode ?? '-'}{definition.memberCode ? ` / ${definition.memberCode}` : ''}</td>
                  <td className="px-4 py-3"><select aria-label={`${definition.code} decision`} className="rounded border border-gray-300 p-2" value={rule?.state ?? ''} onChange={event => setState(definition.code, event.target.value as '' | SecurityPermissionState)}><option value="">No rule (deny)</option><option value="ALLOW">Allow</option><option value="DENY">Explicit deny</option></select></td>
                  <td className="px-4 py-3"><select aria-label={`${definition.code} scope`} disabled={!rule} className="rounded border border-gray-300 p-2 disabled:bg-gray-100" value={rule?.scopeType ?? ''} onChange={event => setScope(definition.code, event.target.value)}>{scopes.map(scope => <option key={scope} value={scope}>{scope || 'No additional scope'}</option>)}</select></td>
                </tr>;
              })}
              {!visibleDefinitions.length && <tr><td colSpan={4} className="px-4 py-8 text-center text-gray-500">No registered permissions in this category.</td></tr>}
            </tbody>
          </table>
        </div>
        <div className="flex items-center justify-between border-t border-gray-200 p-4">
          <span className="text-sm text-gray-600">{message}</span>
          <button type="button" disabled={busy || !roleId} onClick={() => void save()} className="rounded bg-blue-700 px-4 py-2 text-sm font-medium text-white disabled:opacity-50">{busy ? 'Working…' : 'Save role security'}</button>
        </div>
      </section>

      <NavigationRegistryEditor refreshToken={definitionRefreshToken} />

      <SecurityRegistryEditor onDefinitionsChanged={() => setDefinitionRefreshToken(value => value + 1)} />

      <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <h2 className="text-lg font-semibold">Effective permission preview</h2>
        <div className="mt-3 flex flex-wrap gap-2">
          <input aria-label="Search security users" placeholder="Search users" className="min-w-64 rounded border border-gray-300 p-2" value={userSearch} onChange={event => { setUserSearch(event.target.value); setUserPage(1); }} />
          <select className="min-w-80 rounded border border-gray-300 p-2" value={previewUserId} onChange={event => void loadUserRoles(event.target.value)}><option value="">Select a user</option>{users.map(item => <option key={item.publicId} value={item.publicId}>{item.fullName} ({item.email})</option>)}</select>
          <button type="button" onClick={() => void loadPreview()} disabled={!previewUserId} className="rounded border border-blue-700 px-4 py-2 text-blue-700 disabled:opacity-50">Calculate</button>
        </div>
        {userTotalPages > 1 && <div className="mt-2 flex items-center gap-2 text-sm text-gray-600"><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={userPage <= 1} onClick={() => setUserPage(value => Math.max(1, value - 1))}>Previous users</button><span>Page {userPage} of {userTotalPages}</span><button type="button" className="rounded border px-2 py-1 disabled:opacity-50" disabled={userPage >= userTotalPages} onClick={() => setUserPage(value => value + 1)}>Next users</button></div>}
        {userRoles && <div className="mt-4 rounded border border-gray-200 p-3">
          <h3 className="font-medium">Effective-dated roles for {userRoles.userName}</h3>
          <p className="mt-3 text-xs text-gray-500">Role choices follow the paged role search above; selections from other pages are preserved.</p>
          <div className="mt-2 grid gap-2 md:grid-cols-2 lg:grid-cols-3">{roles.filter(role => role.isActive).map(role => {
            const assigned = assignmentDrafts.some(item => item.rolePublicId === role.publicId);
            return <label key={role.publicId} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={assigned} onChange={event => setAssignmentDrafts(current => event.target.checked ? [...current, { rolePublicId: role.publicId, municipalityId: role.municipalityId, effectiveFrom: new Date().toISOString() }] : current.filter(item => item.rolePublicId !== role.publicId))}/><span>{role.name}</span></label>;
          })}</div>
          <div className="mt-4 space-y-3">{assignmentDrafts.map((assignment, index) => {
            const role = roles.find(item => item.publicId === assignment.rolePublicId);
            return <div key={assignment.rolePublicId} className="grid gap-2 rounded bg-gray-50 p-3 md:grid-cols-2 lg:grid-cols-5">
              <div className="text-sm font-medium text-gray-800">{role?.name ?? assignment.rolePublicId}<div className="font-mono text-xs text-gray-500">{role?.roleCode}</div></div>
              <OrganizationMasterPicker kind="department" label={`${role?.name} department`} value={assignment.departmentPublicId ?? ''} selectedLabel={assignment.departmentName} emptyLabel="All permitted departments" onChange={(value, option) => setAssignmentDrafts(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, departmentId: undefined, departmentPublicId: value || undefined, departmentName: option?.name, unitId: undefined, unitPublicId: undefined, unitName: undefined } : item))} />
              <OrganizationMasterPicker kind="unit" label={`${role?.name} unit`} value={assignment.unitPublicId ?? ''} selectedLabel={assignment.unitName} departmentPublicId={assignment.departmentPublicId} emptyLabel="All permitted units" onChange={(value, option) => setAssignmentDrafts(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, unitId: undefined, unitPublicId: value || undefined, unitName: option?.name } : item))} />
              <label className="text-xs text-gray-600">Effective from<input aria-label={`${role?.name} effective from`} type="datetime-local" className="mt-1 w-full rounded border border-gray-300 p-2 text-sm" value={toLocalDateTime(assignment.effectiveFrom)} onChange={event => setAssignmentDrafts(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, effectiveFrom: event.target.value } : item))} /></label>
              <label className="text-xs text-gray-600">Effective to<input aria-label={`${role?.name} effective to`} type="datetime-local" className="mt-1 w-full rounded border border-gray-300 p-2 text-sm" value={toLocalDateTime(assignment.effectiveTo)} onChange={event => setAssignmentDrafts(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, effectiveTo: event.target.value || undefined } : item))} /></label>
            </div>;
          })}</div>
          <button type="button" disabled={busy} onClick={() => void saveUserRoles()} className="mt-3 rounded bg-blue-700 px-4 py-2 text-sm font-medium text-white disabled:opacity-50">Save user roles</button>
        </div>}
        {preview && <div className="mt-4 grid gap-4 md:grid-cols-3"><PreviewList title="Roles" values={preview.roles}/><PreviewList title="Scopes" values={preview.scopes}/><PreviewList title="Effective permissions" values={preview.permissions}/></div>}
      </section>
    </div>
  );
}

function PreviewList({ title, values }: { title: string; values: string[] }) {
  return <div><h3 className="font-medium text-gray-800">{title}</h3><ul className="mt-2 max-h-64 overflow-auto rounded bg-gray-50 p-3 text-xs">{values.length ? values.map(value => <li key={value} className="mb-1 font-mono">{value}</li>) : <li className="text-gray-500">None</li>}</ul></div>;
}
