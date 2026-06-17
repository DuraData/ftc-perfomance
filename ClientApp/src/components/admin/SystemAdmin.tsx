import { useEffect, useMemo, useState } from 'react';
import { Edit2, Plus, Trash2, Shield, Users, History, UserRound, KeyRound, ShieldCheck, FileText } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { ConfirmDialog, Modal } from '../common/Modal';
import { Checkbox, FormRow, Input, Textarea, FormHero, FormPanel } from '../common/Form';
import {
  activateUser,
  createPermission,
  createRole,
  createUser,
  deactivateUser,
  deletePermission,
  deleteRole,
  deleteUser,
  getAuditTrails,
  getLoginAuditLogs,
  getPermissions,
  getPermissionsGrouped,
  getUserPermissions,
  getRolePermissions,
  getRoles,
  getUsers,
  setRolePermissions,
  setUserPermissionOverrides,
  setUserRoles,
  updatePermission,
  updateRole,
  updateUser,
} from '../../api/api';
import type { AdminPermission, AdminPermissionGroup, AdminRole, AdminUserDetail, AuditTrailEntryDto, LoginAuditLog, UserPermissionOverride, UserPermissions } from '../../types';
import { useApp } from '../../context/AppContext';

function useHasPermission(code: string) {
  const { permissions, isSuperAdmin } = useApp();
  return useMemo(() => isSuperAdmin || permissions.some(p => p.toLowerCase() === code.toLowerCase()), [permissions, code, isSuperAdmin]);
}

export function AdminUsersPage() {
  const canManage = useHasPermission('Admin.Users.Manage');
  const { pushToast } = useApp();
  const [rows, setRows] = useState<AdminUserDetail[]>([]);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [roleModalOpen, setRoleModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUserDetail | null>(null);
  const [selectedUserForRoles, setSelectedUserForRoles] = useState<AdminUserDetail | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<AdminUserDetail | null>(null);

  const [permissionModalOpen, setPermissionModalOpen] = useState(false);
  const [selectedUserForPermissions, setSelectedUserForPermissions] = useState<AdminUserDetail | null>(null);
  const [allPermissions, setAllPermissions] = useState<AdminPermission[]>([]);
  const [userPermissions, setUserPermissions] = useState<UserPermissions | null>(null);
  const [overrideMap, setOverrideMap] = useState<Record<number, boolean>>({});

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: '',
    isActive: true,
  });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    (async () => {
      setLoading(true);
      setError(null);
      const [usersRes, rolesRes, permsRes] = await Promise.all([getUsers(), getRoles(), getPermissions()]);
      if (!usersRes.success) setError(usersRes.message ?? 'Failed to load users');
      if (!rolesRes.success) setError(rolesRes.message ?? 'Failed to load roles');
      if (!permsRes.success) setError(permsRes.message ?? 'Failed to load permissions');
      setRows(usersRes.data ?? []);
      setRoles(rolesRes.data ?? []);
      setAllPermissions(permsRes.data ?? []);
      setLoading(false);
    })();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ firstName: '', lastName: '', email: '', phoneNumber: '', password: '', isActive: true });
    setFormErrors({});
    setModalOpen(true);
  };

  const openEdit = (u: AdminUserDetail) => {
    setEditing(u);
    setForm({
      firstName: u.user.firstName,
      lastName: u.user.lastName,
      email: u.user.email,
      phoneNumber: u.user.phoneNumber ?? '',
      password: '',
      isActive: u.user.isActive,
    });
    setFormErrors({});
    setModalOpen(true);
  };

  const save = async () => {
    setError(null);
    setFormErrors({});
    if (!canManage) return;

    const nextErrors: Record<string, string> = {};
    if (!form.firstName.trim()) nextErrors.firstName = 'First name is required';
    if (!form.lastName.trim()) nextErrors.lastName = 'Last name is required';
    if (!editing && !form.email.trim()) nextErrors.email = 'Email is required';
    if (!editing && !form.password.trim()) nextErrors.password = 'Password is required';
    if (!editing && form.password.trim().length < 8) nextErrors.password = 'Password must be at least 8 characters';
    if (Object.keys(nextErrors).length > 0) {
      setFormErrors(nextErrors);
      return;
    }

    if (editing) {
      const res = await updateUser({
        id: editing.user.id,
        firstName: form.firstName,
        lastName: form.lastName,
        phoneNumber: form.phoneNumber || undefined,
        isActive: form.isActive,
      });
      if (!res.success || !res.data) {
        setError(res.message ?? 'Failed to update user');
        return;
      }
      pushToast('success', 'User updated');
    } else {
      const res = await createUser({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phoneNumber: form.phoneNumber || undefined,
        password: form.password,
      });
      if (!res.success) {
        setError(res.message ?? 'Failed to create user');
        return;
      }
      pushToast('success', 'User created');
    }

    const refresh = await getUsers();
    setRows(refresh.data ?? []);
    setModalOpen(false);
  };

  const toggleActive = async (u: AdminUserDetail) => {
    if (!canManage) return;
    const res = u.user.isActive ? await deactivateUser(u.user.id) : await activateUser(u.user.id);
    if (!res.success) {
      setError(res.message ?? 'Failed to update status');
      return;
    }
    const refresh = await getUsers();
    setRows(refresh.data ?? []);
    pushToast('success', u.user.isActive ? 'User deactivated' : 'User activated');
  };

  const requestDelete = (u: AdminUserDetail) => {
    setUserToDelete(u);
    setDeleteConfirmOpen(true);
  };

  const confirmDelete = async () => {
    if (!canManage || !userToDelete) return;
    const res = await deleteUser(userToDelete.user.id);
    if (!res.success) {
      setError(res.message ?? 'Failed to delete user');
      return;
    }
    setRows(prev => prev.filter(x => x.user.id !== userToDelete.user.id));
    setDeleteConfirmOpen(false);
    setUserToDelete(null);
    pushToast('success', 'User deleted');
  };

  const openRoles = (u: AdminUserDetail) => {
    setSelectedUserForRoles(u);
    setRoleModalOpen(true);
  };

  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);
  useEffect(() => {
    if (selectedUserForRoles) {
      setSelectedRoleIds(selectedUserForRoles.roles.map(r => r.id));
    }
  }, [selectedUserForRoles]);

  const saveRoles = async () => {
    if (!selectedUserForRoles) return;
    const res = await setUserRoles(selectedUserForRoles.user.id, selectedRoleIds);
    if (!res.success) {
      setError(res.message ?? 'Failed to update roles');
      return;
    }
    const refresh = await getUsers();
    setRows(refresh.data ?? []);
    setRoleModalOpen(false);
    setSelectedUserForRoles(null);
    pushToast('success', 'Roles updated');
  };

  const openPermissions = async (u: AdminUserDetail) => {
    setError(null);
    setSelectedUserForPermissions(u);
    const res = await getUserPermissions(u.user.id);
    if (!res.success || !res.data) {
      setError(res.message ?? 'Failed to load user permissions');
      return;
    }
    setUserPermissions(res.data);
    const map: Record<number, boolean> = {};
    (res.data.overrides ?? []).forEach(o => {
      map[o.permissionId] = o.isAllowed;
    });
    setOverrideMap(map);
    setPermissionModalOpen(true);
  };

  const savePermissionOverrides = async () => {
    if (!selectedUserForPermissions) return;
    const overrides: UserPermissionOverride[] = Object.entries(overrideMap).map(([permissionId, isAllowed]) => ({
      permissionId: Number(permissionId),
      code: '',
      isAllowed,
      reason: undefined,
    }));
    const res = await setUserPermissionOverrides(selectedUserForPermissions.user.id, overrides);
    if (!res.success) {
      setError(res.message ?? 'Failed to save overrides');
      return;
    }
    setPermissionModalOpen(false);
    setSelectedUserForPermissions(null);
    pushToast('success', 'Permission overrides saved');
  };

  const columns = [
    { id: 'email', header: 'Email', accessor: (r: AdminUserDetail) => r.user.email },
    { id: 'name', header: 'Name', accessor: (r: AdminUserDetail) => r.user.fullName },
    {
      id: 'roles',
      header: 'Roles',
      accessor: (r: AdminUserDetail) => (
        <div className="flex flex-wrap gap-1">
          {r.roles.slice(0, 3).map(role => (
            <Badge key={role.id} size="sm" variant="default">{role.name}</Badge>
          ))}
          {r.roles.length > 3 && <Badge size="sm" variant="default">+{r.roles.length - 3}</Badge>}
        </div>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      accessor: (r: AdminUserDetail) => r.user.isActive ? <Badge variant="success" size="sm">Active</Badge> : <Badge variant="error" size="sm">Inactive</Badge>,
    },
  ];

  const actions = (r: AdminUserDetail) => (
    <div className="flex items-center justify-end gap-1">
      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700" onClick={() => openEdit(r)}>
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800" onClick={() => openRoles(r)}>
        Roles
      </button>
      <button className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800" onClick={() => openPermissions(r)}>
        Permissions
      </button>
      <button className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800" onClick={() => toggleActive(r)}>
        {r.user.isActive ? 'Deactivate' : 'Activate'}
      </button>
      <button className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20" onClick={() => requestDelete(r)}>
        <Trash2 className="w-4 h-4 text-error-500" />
      </button>
    </div>
  );

  return (
    <AppShell title="Users" subtitle="System Administration: Users">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{rows.length} users</Badge>
          <div className="flex gap-2">
            <Button variant="primary" size="sm" icon={<Plus className="w-4 h-4" />} onClick={openCreate} disabled={!canManage}>
              Add User
            </Button>
          </div>
        </div>

        {error && (
          <div className="p-3 rounded-lg border border-error-200 bg-error-50 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">
            {error}
          </div>
        )}

        <Card>
          <DataTable data={rows} columns={columns} actions={actions} searchable getRowId={(r) => r.user.id} emptyMessage={loading ? 'Loading...' : 'No users'} />
        </Card>

        <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Edit User' : 'New User'} size="lg">
          <div className="space-y-5">
            <FormHero
              eyebrow="User Administration"
              title={editing ? 'Update user profile' : 'Create user profile'}
              description="Manage account identity, contact details, and activation status in the same enterprise form layout used elsewhere."
              badges={<Badge variant="default">{editing ? 'Edit Mode' : 'New User'}</Badge>}
            />
            <div className="grid gap-4 xl:grid-cols-[1.2fr_0.8fr]">
              <FormPanel
                title="Personal Information"
                description="Capture the basic identity and contact information for the user account."
                icon={<UserRound className="h-5 w-5" />}
              >
                <FormRow cols={2}>
                  <Input label="First Name" value={form.firstName} error={formErrors.firstName} onChange={(e) => setForm(prev => ({ ...prev, firstName: e.target.value }))} required />
                  <Input label="Last Name" value={form.lastName} error={formErrors.lastName} onChange={(e) => setForm(prev => ({ ...prev, lastName: e.target.value }))} required />
                </FormRow>
                <FormRow cols={2}>
                  <Input label="Email" type="email" value={form.email} error={formErrors.email} onChange={(e) => setForm(prev => ({ ...prev, email: e.target.value }))} required disabled={!!editing} />
                  <Input label="Phone Number" value={form.phoneNumber} onChange={(e) => setForm(prev => ({ ...prev, phoneNumber: e.target.value }))} />
                </FormRow>
              </FormPanel>
              <FormPanel
                title="Account Controls"
                description="Manage authentication and access status for the account."
                icon={<KeyRound className="h-5 w-5" />}
              >
                {!editing ? (
                  <>
                    <Input label="Password" type="password" value={form.password} error={formErrors.password} onChange={(e) => setForm(prev => ({ ...prev, password: e.target.value }))} required />
                    <div className="rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/60">
                      <Badge variant="default">Must change password on first login</Badge>
                    </div>
                  </>
                ) : (
                  <div className="rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/60">
                    <Checkbox
                      label="Active Account"
                      checked={form.isActive}
                      onChange={(e) => setForm(prev => ({ ...prev, isActive: e.target.checked }))}
                      description="Disable this to prevent the user from signing in."
                    />
                  </div>
                )}
              </FormPanel>
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="ghost" size="sm" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={save} disabled={!canManage}>{editing ? 'Save' : 'Create'}</Button>
          </div>
        </Modal>

        <Modal isOpen={roleModalOpen} onClose={() => setRoleModalOpen(false)} title="Assign Roles" size="lg">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Users className="w-4 h-4 text-secondary-400" />
              <p className="text-sm text-secondary-700 dark:text-secondary-300">{selectedUserForRoles?.user.email}</p>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {roles.map(r => (
                <Checkbox
                  key={r.id}
                  label={r.name}
                  description={r.description ?? ''}
                  checked={selectedRoleIds.includes(r.id)}
                  onChange={(e) => {
                    const checked = e.target.checked;
                    setSelectedRoleIds(prev => checked ? [...prev, r.id] : prev.filter(x => x !== r.id));
                  }}
                />
              ))}
            </div>
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => setRoleModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={saveRoles} disabled={!canManage}>Save</Button>
          </div>
        </Modal>

        <Modal isOpen={permissionModalOpen} onClose={() => setPermissionModalOpen(false)} title="User Permissions & Overrides" size="xl">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Users className="w-4 h-4 text-secondary-400" />
              <p className="text-sm text-secondary-700 dark:text-secondary-300">{selectedUserForPermissions?.user.email}</p>
            </div>

            {!userPermissions ? (
              <p className="text-sm text-secondary-500">Loading...</p>
            ) : (
              <div className="max-h-[55vh] overflow-auto pr-1 space-y-3">
                {Object.entries(
                  allPermissions.reduce<Record<string, AdminPermission[]>>((acc, p) => {
                    acc[p.module] = acc[p.module] ? [...acc[p.module], p] : [p];
                    return acc;
                  }, {})
                ).sort(([a], [b]) => a.localeCompare(b)).map(([module, perms]) => {
                  const fromRoles = new Set((userPermissions.fromRoles ?? []).map(x => x.toLowerCase()));
                  const hasAllEffective = perms.every(p => {
                    const inherited = fromRoles.has(p.code.toLowerCase());
                    const override = overrideMap[p.id];
                    const effective = override !== undefined ? override : inherited;
                    return effective;
                  });

                  return (
                    <div key={module} className="rounded-lg border border-secondary-200 dark:border-secondary-700 p-3">
                      <div className="flex items-center justify-between gap-3">
                        <p className="text-sm font-semibold text-secondary-900 dark:text-white">{module}</p>
                        <button
                          className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800"
                          onClick={() => {
                            setOverrideMap(prev => {
                              const next = { ...prev };
                              perms.forEach(p => {
                                const inherited = fromRoles.has(p.code.toLowerCase());
                                const desired = !hasAllEffective;
                                if (desired === inherited) delete next[p.id];
                                else next[p.id] = desired;
                              });
                              return next;
                            });
                          }}
                        >
                          {hasAllEffective ? 'Clear module overrides' : 'Select all in module'}
                        </button>
                      </div>

                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2">
                        {perms
                          .slice()
                          .sort((a, b) => a.code.localeCompare(b.code))
                          .map(p => {
                            const inherited = fromRoles.has(p.code.toLowerCase());
                            const override = overrideMap[p.id];
                            const effective = override !== undefined ? override : inherited;
                            const isOverride = override !== undefined && override !== inherited;

                            return (
                              <div key={p.id} className="flex items-start gap-2 p-2 rounded-lg hover:bg-secondary-50 dark:hover:bg-secondary-800">
                                <input
                                  type="checkbox"
                                  className="mt-0.5 w-3.5 h-3.5 rounded border-secondary-300 dark:border-secondary-600 text-primary-600 focus:ring-primary-500"
                                  checked={effective}
                                  onChange={() => {
                                    setOverrideMap(prev => {
                                      const next = { ...prev };
                                      const desired = !effective;
                                      if (desired === inherited) delete next[p.id];
                                      else next[p.id] = desired;
                                      return next;
                                    });
                                  }}
                                />
                                <div className="min-w-0">
                                  <p className="text-xs font-medium text-secondary-800 dark:text-secondary-200 truncate">{p.code}</p>
                                  <p className="text-[10px] text-secondary-500">
                                    {inherited ? 'Inherited' : 'Not inherited'}{isOverride ? ' • Overridden' : ''}
                                  </p>
                                </div>
                              </div>
                            );
                          })}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => setPermissionModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={savePermissionOverrides} disabled={!canManage}>Save</Button>
          </div>
        </Modal>

        <ConfirmDialog
          isOpen={deleteConfirmOpen}
          onClose={() => { setDeleteConfirmOpen(false); setUserToDelete(null); }}
          onConfirm={confirmDelete}
          title="Delete user"
          message={`Are you sure you want to delete "${userToDelete?.user.email}"? This action cannot be undone.`}
          confirmLabel="Delete"
          variant="danger"
        />
      </div>
    </AppShell>
  );
}

export function AdminRolesPage() {
  const canManage = useHasPermission('Admin.Roles.Manage');
  const { pushToast } = useApp();
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [permissionGroups, setPermissionGroups] = useState<AdminPermissionGroup[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminRole | null>(null);
  const [roleForm, setRoleForm] = useState({ name: '', description: '' });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [roleToDelete, setRoleToDelete] = useState<AdminRole | null>(null);

  const [permModalOpen, setPermModalOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<AdminRole | null>(null);
  const [selectedPermissionIds, setSelectedPermissionIds] = useState<number[]>([]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      const [r, pg] = await Promise.all([getRoles(), getPermissionsGrouped()]);
      setRoles(r.data ?? []);
      setPermissionGroups(pg.data ?? []);
      setError(r.success && pg.success ? null : (r.message ?? pg.message ?? 'Failed to load'));
      setLoading(false);
    })();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setRoleForm({ name: '', description: '' });
    setFormErrors({});
    setModalOpen(true);
  };

  const openEdit = (r: AdminRole) => {
    setEditing(r);
    setRoleForm({ name: r.name, description: r.description ?? '' });
    setFormErrors({});
    setModalOpen(true);
  };

  const save = async () => {
    setError(null);
    setFormErrors({});
    if (!canManage) return;

    if (!roleForm.name.trim()) {
      setFormErrors({ name: 'Role name is required' });
      return;
    }

    if (editing) {
      const res = await updateRole({ id: editing.id, name: roleForm.name, description: roleForm.description || undefined });
      if (!res.success) { setError(res.message ?? 'Failed to update role'); return; }
      pushToast('success', 'Role updated');
    } else {
      const res = await createRole({ name: roleForm.name, description: roleForm.description || undefined });
      if (!res.success) { setError(res.message ?? 'Failed to create role'); return; }
      pushToast('success', 'Role created');
    }
    const refresh = await getRoles();
    setRoles(refresh.data ?? []);
    setModalOpen(false);
  };

  const requestDelete = (r: AdminRole) => {
    setRoleToDelete(r);
    setDeleteConfirmOpen(true);
  };

  const confirmDelete = async () => {
    if (!canManage || !roleToDelete) return;
    setError(null);
    const res = await deleteRole(roleToDelete.id);
    if (!res.success) { setError(res.message ?? 'Failed to delete role'); return; }
    setRoles(prev => prev.filter(x => x.id !== roleToDelete.id));
    setDeleteConfirmOpen(false);
    setRoleToDelete(null);
    pushToast('success', 'Role deleted');
  };

  const openPermissions = async (r: AdminRole) => {
    setError(null);
    setSelectedRole(r);
    const res = await getRolePermissions(r.id);
    if (!res.success || !res.data) {
      setError(res.message ?? 'Failed to load role permissions');
      return;
    }
    setSelectedPermissionIds(res.data.filter(x => x.isAllowed).map(x => x.permissionId));
    setPermModalOpen(true);
  };

  const savePermissions = async () => {
    if (!selectedRole) return;
    const res = await setRolePermissions(selectedRole.id, selectedPermissionIds);
    if (!res.success) { setError(res.message ?? 'Failed to save permissions'); return; }
    setPermModalOpen(false);
    setSelectedRole(null);
    pushToast('success', 'Permissions updated');
  };

  const columns = [
    { id: 'name', header: 'Role', accessor: (r: AdminRole) => (
      <div className="space-y-0.5">
        <p className="text-sm font-medium text-secondary-900 dark:text-white">{r.name}</p>
        {r.description && <p className="text-[11px] text-secondary-500">{r.description}</p>}
      </div>
    ) },
    { id: 'system', header: 'System', accessor: (r: AdminRole) => r.isSystemRole ? <Badge variant="warning" size="sm">System</Badge> : <Badge variant="default" size="sm">Custom</Badge> },
    { id: 'active', header: 'Status', accessor: (r: AdminRole) => r.isActive ? <Badge variant="success" size="sm">Active</Badge> : <Badge variant="error" size="sm">Inactive</Badge> },
  ];

  const actions = (r: AdminRole) => (
    <div className="flex items-center justify-end gap-1">
      <button className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800" onClick={() => openPermissions(r)}>
        Permissions
      </button>
      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700" onClick={() => openEdit(r)} disabled={r.isSystemRole}>
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20" onClick={() => requestDelete(r)} disabled={r.isSystemRole}>
        <Trash2 className="w-4 h-4 text-error-500" />
      </button>
    </div>
  );

  return (
    <AppShell title="Roles" subtitle="System Administration: Roles">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{roles.length} roles</Badge>
          <Button variant="primary" size="sm" icon={<Plus className="w-4 h-4" />} onClick={openCreate} disabled={!canManage}>Add Role</Button>
        </div>

        {error && (
          <div className="p-3 rounded-lg border border-error-200 bg-error-50 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">
            {error}
          </div>
        )}

        <Card>
          <DataTable data={roles} columns={columns} actions={actions} searchable getRowId={(r) => r.id} emptyMessage={loading ? 'Loading...' : 'No roles'} />
        </Card>

        <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Edit Role' : 'New Role'} size="md">
          <div className="space-y-5">
            <FormHero
              eyebrow="Role Administration"
              title={editing ? 'Update role definition' : 'Create role definition'}
              description="Define the role name and its purpose before assigning permissions."
              badges={<Badge variant="default">{editing ? 'Edit Mode' : 'New Role'}</Badge>}
            />
            <FormPanel
              title="Role Details"
              description="Provide the core business identity for this role."
              icon={<ShieldCheck className="h-5 w-5" />}
            >
              <Input label="Name" value={roleForm.name} error={formErrors.name} onChange={(e) => setRoleForm(prev => ({ ...prev, name: e.target.value }))} required />
              <Textarea label="Description" rows={3} value={roleForm.description} onChange={(e) => setRoleForm(prev => ({ ...prev, description: e.target.value }))} />
            </FormPanel>
          </div>
          <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="ghost" size="sm" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={save} disabled={!canManage}>{editing ? 'Save' : 'Create'}</Button>
          </div>
        </Modal>

        <Modal isOpen={permModalOpen} onClose={() => setPermModalOpen(false)} title="Role Permissions" size="lg">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Shield className="w-4 h-4 text-secondary-400" />
              <p className="text-sm text-secondary-700 dark:text-secondary-300">{selectedRole?.name}</p>
            </div>
            <div className="space-y-3 max-h-[55vh] overflow-auto pr-1">
              {Object.entries(
                permissionGroups.reduce<Record<string, AdminPermissionGroup[]>>((acc, g) => {
                  acc[g.module] = acc[g.module] ? [...acc[g.module], g] : [g];
                  return acc;
                }, {})
              ).sort(([a], [b]) => a.localeCompare(b)).map(([module, groups]) => {
                const modulePermissionIds = groups.flatMap(g => g.permissions.map(p => p.id));
                const moduleAllSelected = modulePermissionIds.length > 0 && modulePermissionIds.every(id => selectedPermissionIds.includes(id));
                return (
                  <div key={module} className="rounded-lg border border-secondary-200 dark:border-secondary-700 p-3">
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-semibold text-secondary-900 dark:text-white">{module}</p>
                      <button
                        className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800"
                        onClick={() => {
                          setSelectedPermissionIds(prev => {
                            if (moduleAllSelected) {
                              return prev.filter(id => !modulePermissionIds.includes(id));
                            }
                            const next = new Set(prev);
                            modulePermissionIds.forEach(id => next.add(id));
                            return Array.from(next);
                          });
                        }}
                      >
                        {moduleAllSelected ? 'Clear module' : 'Select all'}
                      </button>
                    </div>

                    <div className="space-y-3 mt-2">
                      {groups.sort((a, b) => a.feature.localeCompare(b.feature)).map(group => (
                        <div key={`${group.module}:${group.feature}`}>
                          <div className="flex items-center justify-between">
                            <p className="text-xs font-semibold text-secondary-700 dark:text-secondary-300">{group.feature}</p>
                            <Badge size="sm" variant="default">{group.permissions.length}</Badge>
                          </div>
                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2">
                            {group.permissions
                              .slice()
                              .sort((a, b) => a.code.localeCompare(b.code))
                              .map(p => (
                                <Checkbox
                                  key={p.id}
                                  label={p.code}
                                  description={p.description ?? ''}
                                  checked={selectedPermissionIds.includes(p.id)}
                                  onChange={(e) => {
                                    const checked = e.target.checked;
                                    setSelectedPermissionIds(prev => checked ? [...prev, p.id] : prev.filter(x => x !== p.id));
                                  }}
                                />
                              ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => setPermModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={savePermissions} disabled={!canManage}>Save</Button>
          </div>
        </Modal>

        <ConfirmDialog
          isOpen={deleteConfirmOpen}
          onClose={() => { setDeleteConfirmOpen(false); setRoleToDelete(null); }}
          onConfirm={confirmDelete}
          title="Delete role"
          message={`Are you sure you want to delete "${roleToDelete?.name}"? This action cannot be undone.`}
          confirmLabel="Delete"
          variant="danger"
        />
      </div>
    </AppShell>
  );
}

export function AdminPermissionsPage() {
  const canManage = useHasPermission('Admin.Permissions.Manage');
  const { pushToast } = useApp();
  const [rows, setRows] = useState<AdminPermission[]>([]);
  const [groups, setGroups] = useState<AdminPermissionGroup[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [viewMode, setViewMode] = useState<'grouped' | 'table'>('grouped');

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminPermission | null>(null);
  const [form, setForm] = useState<Omit<AdminPermission, 'id'>>({
    module: '',
    feature: '',
    action: '',
    code: '',
    description: '',
    isActive: true,
  });
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [permissionToDelete, setPermissionToDelete] = useState<AdminPermission | null>(null);

  useEffect(() => {
    (async () => {
      setLoading(true);
      const [res, groupedRes] = await Promise.all([getPermissions(), getPermissionsGrouped()]);
      setRows(res.data ?? []);
      setGroups(groupedRes.data ?? []);
      setError(res.success && groupedRes.success ? null : (res.message ?? groupedRes.message ?? 'Failed to load permissions'));
      setLoading(false);
    })();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ module: '', feature: '', action: '', code: '', description: '', isActive: true });
    setFormErrors({});
    setModalOpen(true);
  };

  const openEdit = (p: AdminPermission) => {
    setEditing(p);
    setForm({
      module: p.module,
      feature: p.feature,
      action: p.action,
      code: p.code,
      description: p.description ?? '',
      isActive: p.isActive,
    });
    setFormErrors({});
    setModalOpen(true);
  };

  const save = async () => {
    setError(null);
    setFormErrors({});
    if (!canManage) return;

    const nextErrors: Record<string, string> = {};
    if (!form.module.trim()) nextErrors.module = 'Module is required';
    if (!form.feature.trim()) nextErrors.feature = 'Feature is required';
    if (!form.action.trim()) nextErrors.action = 'Action is required';
    if (!form.code.trim()) nextErrors.code = 'Code is required';
    if (Object.keys(nextErrors).length > 0) {
      setFormErrors(nextErrors);
      return;
    }

    if (editing) {
      const res = await updatePermission({ ...editing, ...form, description: form.description || undefined });
      if (!res.success) { setError(res.message ?? 'Failed to update permission'); return; }
      pushToast('success', 'Permission updated');
    } else {
      const res = await createPermission({ ...form, description: form.description || undefined });
      if (!res.success) { setError(res.message ?? 'Failed to create permission'); return; }
      pushToast('success', 'Permission created');
    }
    const [refresh, groupedRefresh] = await Promise.all([getPermissions(), getPermissionsGrouped()]);
    setRows(refresh.data ?? []);
    setGroups(groupedRefresh.data ?? []);
    setModalOpen(false);
  };

  const requestDelete = (p: AdminPermission) => {
    setPermissionToDelete(p);
    setDeleteConfirmOpen(true);
  };

  const confirmDelete = async () => {
    if (!canManage || !permissionToDelete) return;
    setError(null);
    const res = await deletePermission(permissionToDelete.id);
    if (!res.success) { setError(res.message ?? 'Failed to delete permission'); return; }
    setRows(prev => prev.filter(x => x.id !== permissionToDelete.id));
    setGroups(prev => prev.map(g => ({
      ...g,
      permissions: g.permissions.filter(pp => pp.id !== permissionToDelete.id)
    })).filter(g => g.permissions.length > 0));
    setDeleteConfirmOpen(false);
    setPermissionToDelete(null);
    pushToast('success', 'Permission deleted');
  };

  const columns = [
    { id: 'code', header: 'Code', accessor: (p: AdminPermission) => <span className="font-mono text-xs">{p.code}</span> },
    { id: 'module', header: 'Module', accessor: (p: AdminPermission) => p.module },
    { id: 'feature', header: 'Feature', accessor: (p: AdminPermission) => p.feature },
    { id: 'action', header: 'Action', accessor: (p: AdminPermission) => p.action },
    { id: 'status', header: 'Status', accessor: (p: AdminPermission) => p.isActive ? <Badge variant="success" size="sm">Active</Badge> : <Badge variant="error" size="sm">Inactive</Badge> },
  ];

  const actions = (p: AdminPermission) => (
    <div className="flex items-center justify-end gap-1">
      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700" onClick={() => openEdit(p)}>
        <Edit2 className="w-4 h-4 text-secondary-400" />
      </button>
      <button className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20" onClick={() => requestDelete(p)}>
        <Trash2 className="w-4 h-4 text-error-500" />
      </button>
    </div>
  );

  const filteredGroups = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return groups;
    return groups
      .map(g => ({
        ...g,
        permissions: g.permissions.filter(p =>
          [p.module, p.feature, p.action, p.code, p.description ?? ''].some(v => v.toLowerCase().includes(q))
        ),
      }))
      .filter(g => g.permissions.length > 0);
  }, [groups, search]);

  return (
    <AppShell title="Permissions" subtitle="System Administration: Permissions">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{rows.length} permissions</Badge>
          <div className="flex items-center gap-2">
            <Button variant={viewMode === 'grouped' ? 'secondary' : 'outline'} size="sm" onClick={() => setViewMode('grouped')}>Grouped</Button>
            <Button variant={viewMode === 'table' ? 'secondary' : 'outline'} size="sm" onClick={() => setViewMode('table')}>Table</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-4 h-4" />} onClick={openCreate} disabled={!canManage}>Add Permission</Button>
          </div>
        </div>

        {error && (
          <div className="p-3 rounded-lg border border-error-200 bg-error-50 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">
            {error}
          </div>
        )}

        {viewMode === 'table' ? (
          <Card>
            <DataTable data={rows} columns={columns} actions={actions} searchable getRowId={(p) => String(p.id)} emptyMessage={loading ? 'Loading...' : 'No permissions'} />
          </Card>
        ) : (
          <Card className="p-4">
            <div className="flex items-center justify-between gap-3 mb-3">
              <Input label="Search" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search module, feature, action, code..." />
              <Badge variant="default">{filteredGroups.reduce((sum, g) => sum + g.permissions.length, 0)} shown</Badge>
            </div>
            <div className="space-y-3">
              {filteredGroups
                .slice()
                .sort((a, b) => (a.module + a.feature).localeCompare(b.module + b.feature))
                .map(g => (
                  <div key={`${g.module}:${g.feature}`} className="rounded-lg border border-secondary-200 dark:border-secondary-700 p-3">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-semibold text-secondary-900 dark:text-white">{g.module}</p>
                      <Badge size="sm" variant="default">{g.feature}</Badge>
                    </div>
                    <div className="mt-2 space-y-2">
                      {g.permissions
                        .slice()
                        .sort((a, b) => a.code.localeCompare(b.code))
                        .map(p => (
                          <div key={p.id} className="flex items-center justify-between gap-3 rounded-lg border border-secondary-200 dark:border-secondary-700 px-3 py-2">
                            <div className="min-w-0">
                              <p className="text-xs font-mono text-secondary-900 dark:text-white truncate">{p.code}</p>
                              <p className="text-[10px] text-secondary-500">{p.description ?? ''}</p>
                            </div>
                            <div className="flex items-center gap-2">
                              {p.isActive ? <Badge variant="success" size="sm">Active</Badge> : <Badge variant="error" size="sm">Inactive</Badge>}
                              {actions(p)}
                            </div>
                          </div>
                        ))}
                    </div>
                  </div>
                ))}
              {filteredGroups.length === 0 && (
                <p className="text-sm text-secondary-500">{loading ? 'Loading...' : 'No permissions match your search.'}</p>
              )}
            </div>
          </Card>
        )}

        <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} title={editing ? 'Edit Permission' : 'New Permission'} size="lg">
          <div className="space-y-5">
            <FormHero
              eyebrow="Permission Administration"
              title={editing ? 'Update permission' : 'Create permission'}
              description="Define a permission code and the related module, feature, and action for access control."
              badges={<Badge variant="default">{editing ? 'Edit Mode' : 'New Permission'}</Badge>}
            />
            <div className="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
              <FormPanel
                title="Permission Mapping"
                description="Set the functional classification and code used by authorization checks."
                icon={<ShieldCheck className="h-5 w-5" />}
              >
                <FormRow cols={2}>
                  <Input label="Module" value={form.module} error={formErrors.module} onChange={(e) => setForm(prev => ({ ...prev, module: e.target.value }))} required />
                  <Input label="Feature" value={form.feature} error={formErrors.feature} onChange={(e) => setForm(prev => ({ ...prev, feature: e.target.value }))} required />
                </FormRow>
                <FormRow cols={2}>
                  <Input label="Action" value={form.action} error={formErrors.action} onChange={(e) => setForm(prev => ({ ...prev, action: e.target.value }))} required />
                  <Input label="Code" value={form.code} error={formErrors.code} onChange={(e) => setForm(prev => ({ ...prev, code: e.target.value }))} required />
                </FormRow>
              </FormPanel>
              <FormPanel
                title="Additional Information"
                description="Capture descriptive guidance and activation status."
                icon={<FileText className="h-5 w-5" />}
              >
                <Textarea label="Description" rows={4} value={form.description ?? ''} onChange={(e) => setForm(prev => ({ ...prev, description: e.target.value }))} />
                <div className="rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/60">
                  <Checkbox label="Active Permission" checked={form.isActive} onChange={(e) => setForm(prev => ({ ...prev, isActive: e.target.checked }))} />
                </div>
              </FormPanel>
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
            <Button variant="ghost" size="sm" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button variant="primary" size="sm" onClick={save} disabled={!canManage}>{editing ? 'Save' : 'Create'}</Button>
          </div>
        </Modal>

        <ConfirmDialog
          isOpen={deleteConfirmOpen}
          onClose={() => { setDeleteConfirmOpen(false); setPermissionToDelete(null); }}
          onConfirm={confirmDelete}
          title="Delete permission"
          message={`Are you sure you want to delete "${permissionToDelete?.code}"? This action cannot be undone.`}
          confirmLabel="Delete"
          variant="danger"
        />
      </div>
    </AppShell>
  );
}

export function AdminAuditLogsPage() {
  const canViewLoginLogs = useHasPermission('Audit.LoginLogs.View');
  const canViewAuditTrails = useHasPermission('Audit.Trails.View') || useHasPermission('Audit.Logs.View');
  const [activeTab, setActiveTab] = useState<'login' | 'trail'>(canViewLoginLogs ? 'login' : 'trail');
  const [rows, setRows] = useState<LoginAuditLog[]>([]);
  const [trailRows, setTrailRows] = useState<AuditTrailEntryDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showFailuresOnly, setShowFailuresOnly] = useState(false);
  const [selectedLog, setSelectedLog] = useState<LoginAuditLog | null>(null);
  const [selectedTrail, setSelectedTrail] = useState<AuditTrailEntryDto | null>(null);

  useEffect(() => {
    (async () => {
      if (!canViewLoginLogs && !canViewAuditTrails) return;
      setLoading(true);
      const [loginRes, trailsRes] = await Promise.all([
        canViewLoginLogs ? getLoginAuditLogs(200) : Promise.resolve({ success: true, data: [] as LoginAuditLog[] }),
        canViewAuditTrails ? getAuditTrails(250) : Promise.resolve({ success: true, data: [] as AuditTrailEntryDto[] }),
      ]);

      setRows(loginRes.data ?? []);
      setTrailRows(trailsRes.data ?? []);
      setError(
        !loginRes.success
          ? (loginRes.message ?? 'Failed to load login audit logs')
          : !trailsRes.success
          ? (trailsRes.message ?? 'Failed to load audit trails')
          : null,
      );
      setLoading(false);
    })();
  }, [canViewLoginLogs, canViewAuditTrails]);

  const loginColumns = [
    { id: 'email', header: 'Email', accessor: (l: LoginAuditLog) => l.email },
    { id: 'ip', header: 'IP', accessor: (l: LoginAuditLog) => l.ipAddress ?? '-' },
    { id: 'ua', header: 'User Agent', accessor: (l: LoginAuditLog) => <span className="text-xs">{l.userAgent ?? '-'}</span> },
    { id: 'result', header: 'Result', accessor: (l: LoginAuditLog) => l.success ? <Badge variant="success" size="sm">Success</Badge> : <Badge variant="error" size="sm">Fail</Badge> },
    { id: 'time', header: 'When', accessor: (l: LoginAuditLog) => new Date(l.loggedAt).toLocaleString() },
  ];

  const trailColumns = [
    { id: 'entity', header: 'Entity', accessor: (row: AuditTrailEntryDto) => row.entityName },
    { id: 'entityId', header: 'Entity ID', accessor: (row: AuditTrailEntryDto) => <span className="font-mono text-xs">{row.entityId}</span> },
    { id: 'action', header: 'Action', accessor: (row: AuditTrailEntryDto) => <Badge variant="info" size="sm">{row.action}</Badge> },
    { id: 'changedBy', header: 'Changed By', accessor: (row: AuditTrailEntryDto) => row.changedBy },
    { id: 'ip', header: 'IP', accessor: (row: AuditTrailEntryDto) => row.ipAddress ?? '-' },
    { id: 'time', header: 'When', accessor: (row: AuditTrailEntryDto) => new Date(row.changedAt).toLocaleString() },
  ];

  const loginActions = (l: LoginAuditLog) => (
    <button
      className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800"
      onClick={() => setSelectedLog(l)}
    >
      View
    </button>
  );

  const trailActions = (entry: AuditTrailEntryDto) => (
    <button
      className="px-2 py-1 text-[11px] rounded-lg border border-secondary-200 dark:border-secondary-700 hover:bg-secondary-50 dark:hover:bg-secondary-800"
      onClick={() => setSelectedTrail(entry)}
    >
      View
    </button>
  );

  const filteredRows = showFailuresOnly ? rows.filter(r => !r.success) : rows;

  return (
    <AppShell title="Audit Logs" subtitle="System Administration: Login logs and workflow audit trails">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge variant="primary">
              {activeTab === 'login' ? `${filteredRows.length} logs` : `${trailRows.length} entries`}
            </Badge>
            <div className="flex items-center gap-2 text-secondary-500">
              <History className="w-4 h-4" />
            </div>
          </div>
          <div className="flex items-center gap-3">
            {canViewLoginLogs && canViewAuditTrails && (
              <div className="inline-flex rounded-lg border border-secondary-200 p-1 dark:border-secondary-700">
                <button
                  className={`rounded-md px-3 py-1.5 text-xs font-medium ${activeTab === 'login' ? 'bg-primary-600 text-white' : 'text-secondary-600 dark:text-secondary-300'}`}
                  onClick={() => setActiveTab('login')}
                >
                  Login Logs
                </button>
                <button
                  className={`rounded-md px-3 py-1.5 text-xs font-medium ${activeTab === 'trail' ? 'bg-primary-600 text-white' : 'text-secondary-600 dark:text-secondary-300'}`}
                  onClick={() => setActiveTab('trail')}
                >
                  Audit Trails
                </button>
              </div>
            )}
            {activeTab === 'login' && (
              <Checkbox
                label="Failures only"
                checked={showFailuresOnly}
                onChange={(e) => setShowFailuresOnly(e.target.checked)}
              />
            )}
          </div>
        </div>

        {error && (
          <div className="p-3 rounded-lg border border-error-200 bg-error-50 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">
            {error}
          </div>
        )}

        <Card>
          {activeTab === 'login' ? (
            <DataTable data={filteredRows} columns={loginColumns} actions={loginActions} searchable getRowId={(l) => String(l.id)} emptyMessage={loading ? 'Loading...' : 'No logs'} />
          ) : (
            <DataTable data={trailRows} columns={trailColumns} actions={trailActions} searchable getRowId={(entry) => String(entry.id)} emptyMessage={loading ? 'Loading...' : 'No audit trail entries'} />
          )}
        </Card>

        {!canViewLoginLogs && !canViewAuditTrails && (
          <div className="p-3 rounded-lg border border-secondary-200 bg-secondary-50 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-300">
            Access denied.
          </div>
        )}

        <Modal isOpen={!!selectedLog} onClose={() => setSelectedLog(null)} title="Login Audit Log" size="md">
          {selectedLog && (
            <div className="space-y-3">
              <div>
                <p className="text-[10px] text-secondary-500">Email</p>
                <p className="text-sm font-medium text-secondary-900 dark:text-white">{selectedLog.email}</p>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-[10px] text-secondary-500">Result</p>
                  {selectedLog.success ? <Badge variant="success" size="sm">Success</Badge> : <Badge variant="error" size="sm">Fail</Badge>}
                </div>
                <div>
                  <p className="text-[10px] text-secondary-500">When</p>
                  <p className="text-xs text-secondary-700 dark:text-secondary-300">{new Date(selectedLog.loggedAt).toLocaleString()}</p>
                </div>
              </div>
              <div>
                <p className="text-[10px] text-secondary-500">IP Address</p>
                <p className="text-xs text-secondary-700 dark:text-secondary-300">{selectedLog.ipAddress ?? '-'}</p>
              </div>
              <div>
                <p className="text-[10px] text-secondary-500">User Agent</p>
                <p className="text-xs text-secondary-700 dark:text-secondary-300 break-words">{selectedLog.userAgent ?? '-'}</p>
              </div>
              {!selectedLog.success && (
                <div>
                  <p className="text-[10px] text-secondary-500">Failure Reason</p>
                  <p className="text-xs text-secondary-700 dark:text-secondary-300">{selectedLog.failureReason ?? '-'}</p>
                </div>
              )}
              <div className="flex justify-end">
                <Button variant="ghost" size="sm" onClick={() => setSelectedLog(null)}>Close</Button>
              </div>
            </div>
          )}
        </Modal>

        <Modal isOpen={!!selectedTrail} onClose={() => setSelectedTrail(null)} title="Audit Trail Entry" size="md">
          {selectedTrail && (
            <div className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-[10px] text-secondary-500">Entity</p>
                  <p className="text-sm font-medium text-secondary-900 dark:text-white">{selectedTrail.entityName}</p>
                </div>
                <div>
                  <p className="text-[10px] text-secondary-500">Action</p>
                  <p className="text-sm font-medium text-secondary-900 dark:text-white">{selectedTrail.action}</p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-[10px] text-secondary-500">Entity ID</p>
                  <p className="text-xs font-mono text-secondary-700 dark:text-secondary-300 break-all">{selectedTrail.entityId}</p>
                </div>
                <div>
                  <p className="text-[10px] text-secondary-500">Changed By</p>
                  <p className="text-xs text-secondary-700 dark:text-secondary-300">{selectedTrail.changedBy}</p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-[10px] text-secondary-500">Changed At</p>
                  <p className="text-xs text-secondary-700 dark:text-secondary-300">{new Date(selectedTrail.changedAt).toLocaleString()}</p>
                </div>
                <div>
                  <p className="text-[10px] text-secondary-500">IP Address</p>
                  <p className="text-xs text-secondary-700 dark:text-secondary-300">{selectedTrail.ipAddress ?? '-'}</p>
                </div>
              </div>
              <div>
                <p className="text-[10px] text-secondary-500">Old Value</p>
                <pre className="mt-1 max-h-40 overflow-auto rounded-lg bg-secondary-50 p-3 text-[11px] text-secondary-700 dark:bg-secondary-900 dark:text-secondary-300 whitespace-pre-wrap break-words">{selectedTrail.oldValue ?? '-'}</pre>
              </div>
              <div>
                <p className="text-[10px] text-secondary-500">New Value</p>
                <pre className="mt-1 max-h-40 overflow-auto rounded-lg bg-secondary-50 p-3 text-[11px] text-secondary-700 dark:bg-secondary-900 dark:text-secondary-300 whitespace-pre-wrap break-words">{selectedTrail.newValue ?? '-'}</pre>
              </div>
              <div className="flex justify-end">
                <Button variant="ghost" size="sm" onClick={() => setSelectedTrail(null)}>Close</Button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AppShell>
  );
}
