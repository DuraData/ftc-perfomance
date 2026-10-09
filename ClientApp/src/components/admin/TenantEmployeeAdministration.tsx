import { useCallback, useEffect, useState } from 'react';
import { Briefcase, Plus, RefreshCw, UserRound } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormPanel, Input, Select, Textarea } from '../common/Form';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import {
  closeEmployeeAssignment,
  createEmployeeAssignment,
  createMunicipalEmployee,
  getEmployeeAssignmentsPage,
  getMunicipalEmployeesPage,
  getUsersPage,
  updateMunicipalEmployee,
} from '../../api/api';
import type { AdminUserDetail, EmployeeAssignmentMasterDto, MunicipalEmployeeDto } from '../../types';

const today = () => new Date().toISOString().slice(0, 10);
const atUtc = (value: string) => new Date(`${value}T00:00:00Z`).toISOString();

export function TenantEmployeeAdministration() {
  const { pushToast } = useApp();
  const security = useSecurity();
  const canReadEmployeeNumber = security.canReadField('EMPLOYEE', 'EmployeeNumber');
  const canEditEmployeeNumber = security.canEditField('EMPLOYEE', 'EmployeeNumber');
  const canReadSalaryReference = security.canReadField('EMPLOYEE', 'SalaryReference');
  const canEditSalaryReference = security.canEditField('EMPLOYEE', 'SalaryReference');
  const canReadIdentityLink = security.canReadField('EMPLOYEE', 'IdentityUserId');
  const canEditIdentityLink = security.canEditField('EMPLOYEE', 'IdentityUserId');
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [users, setUsers] = useState<AdminUserDetail[]>([]);
  const [userPage, setUserPage] = useState(1);
  const [userTotalPages, setUserTotalPages] = useState(0);
  const [userSearch, setUserSearch] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [assignments, setAssignments] = useState<EmployeeAssignmentMasterDto[]>([]);
  const [assignmentPage, setAssignmentPage] = useState(1);
  const [assignmentTotalCount, setAssignmentTotalCount] = useState(0);
  const [assignmentTotalPages, setAssignmentTotalPages] = useState(0);
  const [assignmentSearchInput, setAssignmentSearchInput] = useState('');
  const [assignmentSearch, setAssignmentSearch] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [employee, setEmployee] = useState({ employeeNumber: '', salaryReference: '', firstName: '', lastName: '', emailAddress: '', identityUserPublicId: '', effectiveFrom: today(), reason: '' });
  const [deactivationReason, setDeactivationReason] = useState('');
  const [assignment, setAssignment] = useState({ departmentPublicId: '', unitPublicId: '', positionPublicId: '', effectiveFrom: today(), effectiveTo: '', isPrimary: true });
  const [closure, setClosure] = useState({ effectiveTo: today(), reason: '' });
  const selected = employees.find(item => item.publicId === selectedId) ?? null;
  const loadEmployees = useCallback(async () => {
    setBusy(true); setError(null);
    const employeeResult = await getMunicipalEmployeesPage({ page, pageSize: 25, search, sortBy, sortDirection });
    if (!employeeResult.success) setError(employeeResult.message ?? 'Employee register could not be loaded.');
    setEmployees(employeeResult.data?.items ?? []);
    setTotalCount(employeeResult.data?.totalCount ?? 0);
    setTotalPages(employeeResult.data?.totalPages ?? 0);
    if (employeeResult.data && employeeResult.data.totalPages > 0 && page > employeeResult.data.totalPages) setPage(employeeResult.data.totalPages);
    setBusy(false);
  }, [page, search, sortBy, sortDirection]);

  const loadUsers = useCallback(async () => {
    if (!canReadIdentityLink) { setUsers([]); setUserTotalPages(0); return; }
    const result = await getUsersPage({ page: userPage, pageSize: 25, search: userSearch, sortBy: 'name', sortDirection: 'asc' });
    if (!result.success) setError(result.message ?? 'Login directory could not be loaded.');
    setUsers(result.data?.items ?? []);
    setUserTotalPages(result.data?.totalPages ?? 0);
  }, [canReadIdentityLink, userPage, userSearch]);

  useEffect(() => { void loadEmployees(); }, [loadEmployees]);
  useEffect(() => { void loadUsers(); }, [loadUsers]);

  const loadAssignments = useCallback(async (publicId: string, requestedPage = 1, requestedSearch = '') => {
    setBusy(true); setError(null);
    const result = await getEmployeeAssignmentsPage(publicId, {
      page: requestedPage, pageSize: 10, search: requestedSearch || undefined, sortBy: 'effectiveFrom', sortDirection: 'desc',
    });
    if (!result.success) setError(result.message ?? 'Placement history could not be loaded.');
    setAssignments(result.data?.items ?? []);
    setAssignmentPage(result.data?.page ?? requestedPage);
    setAssignmentTotalCount(result.data?.totalCount ?? 0);
    setAssignmentTotalPages(result.data?.totalPages ?? 0);
    setBusy(false);
  }, []);

  const selectEmployee = async (publicId: string) => {
    setSelectedId(publicId);
    setAssignmentSearch('');
    setAssignmentSearchInput('');
    await loadAssignments(publicId);
  };

  useEffect(() => {
    const normalized = assignmentSearchInput.trim();
    if (normalized === assignmentSearch) return;
    const timeout = window.setTimeout(() => {
      setAssignmentSearch(normalized);
      if (selectedId) void loadAssignments(selectedId, 1, normalized);
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [assignmentSearch, assignmentSearchInput, loadAssignments, selectedId]);

  const saveEmployee = async () => {
    if (!employee.employeeNumber.trim() || !employee.firstName.trim() || !employee.lastName.trim() || employee.reason.trim().length < 10) { setError('Employee number, first name, last name, and a governance reason of at least 10 characters are required.'); return; }
    setBusy(true); setError(null);
    const result = await createMunicipalEmployee({ ...employee, salaryReference: canEditSalaryReference ? employee.salaryReference || null : null, emailAddress: security.canEditField('EMPLOYEE', 'EmailAddress') ? employee.emailAddress || null : null, identityUserPublicId: canEditIdentityLink ? employee.identityUserPublicId || null : null, effectiveFrom: atUtc(employee.effectiveFrom), effectiveTo: null });
    if (!result.success) setError(result.message ?? 'Employee could not be created.');
    else { pushToast('success', 'Employee created'); setEmployee({ employeeNumber: '', salaryReference: '', firstName: '', lastName: '', emailAddress: '', identityUserPublicId: '', effectiveFrom: today(), reason: '' }); if (page === 1) await loadEmployees(); else setPage(1); }
    setBusy(false);
  };

  const deactivateEmployee = async () => {
    if (!selected) return;
    if (deactivationReason.trim().length < 10) { setError('A deactivation reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const result = await updateMunicipalEmployee(selected.publicId, { firstName: selected.firstName, lastName: selected.lastName, salaryReference: null, salaryReferenceSpecified: false, emailAddress: null, emailAddressSpecified: false, identityUserPublicId: null, identityUserPublicIdSpecified: false, isActive: false, effectiveFrom: selected.effectiveFrom, effectiveTo: new Date().toISOString(), reason: deactivationReason.trim(), rowVersion: selected.rowVersion });
    if (!result.success) setError(result.message ?? 'Employee could not be deactivated.');
    else { pushToast('success', 'Employee deactivated without deleting placement history'); setDeactivationReason(''); setSelectedId(''); setAssignments([]); await loadEmployees(); }
    setBusy(false);
  };

  const saveAssignment = async () => {
    if (!selected || !assignment.departmentPublicId || !assignment.positionPublicId) { setError('Employee, department, and governed position are required.'); return; }
    setBusy(true); setError(null);
    const result = await createEmployeeAssignment({ employeePublicId: selected.publicId, departmentPublicId: assignment.departmentPublicId, unitPublicId: assignment.unitPublicId || null, positionPublicId: assignment.positionPublicId, effectiveFrom: atUtc(assignment.effectiveFrom), effectiveTo: assignment.effectiveTo ? atUtc(assignment.effectiveTo) : null, isPrimary: assignment.isPrimary });
    if (!result.success) setError(result.message ?? 'Placement could not be created.');
    else { pushToast('success', 'Effective-dated placement created'); setAssignment({ departmentPublicId: '', unitPublicId: '', positionPublicId: '', effectiveFrom: today(), effectiveTo: '', isPrimary: true }); await loadAssignments(selected.publicId, 1, assignmentSearch); }
    setBusy(false);
  };

  const closeAssignment = async (item: EmployeeAssignmentMasterDto) => {
    if (closure.reason.trim().length < 10) { setError('A closure reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const result = await closeEmployeeAssignment(item.publicId, { effectiveTo: atUtc(closure.effectiveTo), reason: closure.reason.trim(), rowVersion: item.rowVersion });
    if (!result.success) setError(result.message ?? 'Placement could not be ended.');
    else { pushToast('success', 'Placement ended with its history retained'); setClosure({ effectiveTo: today(), reason: '' }); await loadAssignments(item.employeePublicId, assignmentPage, assignmentSearch); }
    setBusy(false);
  };

  return <AppShell title="Municipal Employees" subtitle="Tenant-owned people and effective-dated organizational placements">
    <div className="space-y-5">
      <div className="flex justify-end"><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => { void loadEmployees(); void loadUsers(); }} disabled={busy}>Refresh</Button></div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {security.canCreate('EMPLOYEE') && canEditEmployeeNumber && <FormPanel title="Create employee" description="Identity linkage is optional and does not replace the municipal employee record." icon={<UserRound className="h-5 w-5" />}>
          <Input label="Employee number" value={employee.employeeNumber} onChange={event => setEmployee(current => ({ ...current, employeeNumber: event.target.value }))} required />
          {canReadSalaryReference && <Input label="Salary reference" value={employee.salaryReference} disabled={!canEditSalaryReference} onChange={event => setEmployee(current => ({ ...current, salaryReference: event.target.value }))} />}
          <div className="grid grid-cols-2 gap-2"><Input label="First name" value={employee.firstName} onChange={event => setEmployee(current => ({ ...current, firstName: event.target.value }))} required /><Input label="Last name" value={employee.lastName} onChange={event => setEmployee(current => ({ ...current, lastName: event.target.value }))} required /></div>
          {security.canReadField('EMPLOYEE', 'EmailAddress') && <Input label="Email" type="email" value={employee.emailAddress} disabled={!security.canEditField('EMPLOYEE', 'EmailAddress')} onChange={event => setEmployee(current => ({ ...current, emailAddress: event.target.value }))} />}
          {canReadIdentityLink && <Input label="Search linked logins" value={userSearch} disabled={!canEditIdentityLink} onChange={event => { setUserSearch(event.target.value); setUserPage(1); }} />}
          {canReadIdentityLink && <Select label="Linked login" value={employee.identityUserPublicId} disabled={!canEditIdentityLink} placeholder="No linked login" options={users.filter(item => item.user.isActive).map(item => ({ value: item.user.publicId, label: `${item.user.fullName} · ${item.user.email ?? 'protected email'}` }))} onChange={event => setEmployee(current => ({ ...current, identityUserPublicId: event.target.value }))} />}
          {canReadIdentityLink && userTotalPages > 1 && <div className="flex items-center gap-2 text-xs text-secondary-500"><Button size="sm" variant="outline" disabled={userPage <= 1} onClick={() => setUserPage(value => Math.max(1, value - 1))}>Previous logins</Button><span>Page {userPage} of {userTotalPages}</span><Button size="sm" variant="outline" disabled={userPage >= userTotalPages} onClick={() => setUserPage(value => value + 1)}>Next logins</Button></div>}
          <Input label="Effective from" type="date" value={employee.effectiveFrom} onChange={event => setEmployee(current => ({ ...current, effectiveFrom: event.target.value }))} />
          <Textarea label="Employee governance reason" value={employee.reason} onChange={event => setEmployee(current => ({ ...current, reason: event.target.value }))} required />
          <Button icon={<Plus className="h-4 w-4" />} onClick={() => void saveEmployee()} disabled={busy}>Create employee</Button>
        </FormPanel>}
        <Card className="p-4">
          <div className="flex items-center justify-between gap-2"><h3 className="font-semibold">Employee register</h3><Badge variant="primary">{totalCount} employees</Badge></div>
          <div className="mt-3 grid gap-2 md:grid-cols-[1fr_11rem_9rem]">
            <Input aria-label="Search employees" placeholder={canReadEmployeeNumber ? 'Number, name, or permitted protected fields' : 'Name or permitted protected fields'} value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} />
            <Select aria-label="Sort employees" value={sortBy} options={[{ value: 'name', label: 'Name' }, ...(canReadEmployeeNumber ? [{ value: 'employeeNumber', label: 'Employee number' }] : []), ...(canReadSalaryReference ? [{ value: 'salaryReference', label: 'Salary reference' }] : []), { value: 'status', label: 'Status' }, { value: 'effectiveFrom', label: 'Effective from' }, ...(security.canReadField('EMPLOYEE', 'EmailAddress') ? [{ value: 'email', label: 'Email' }] : [])]} onChange={event => { setSortBy(event.target.value); setPage(1); }} />
            <Select aria-label="Sort direction" value={sortDirection} options={[{ value: 'asc', label: 'Ascending' }, { value: 'desc', label: 'Descending' }]} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); }} />
          </div>
          <div className="mt-3 space-y-2">{employees.map(item => <button type="button" key={item.publicId} onClick={() => void selectEmployee(item.publicId)} className={`flex w-full items-center justify-between rounded-lg border p-3 text-left ${selectedId === item.publicId ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-secondary-200 dark:border-secondary-700'}`}><div><p className="font-medium">{item.firstName} {item.lastName}</p><p className="text-xs text-secondary-500">{item.employeeNumber ?? 'Employee number protected'}{canReadSalaryReference ? ` · Salary ${item.salaryReference || 'not set'}` : ''}{security.canReadField('EMPLOYEE', 'EmailAddress') ? ` · ${item.emailAddress || 'No email'}` : ''}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></button>)}{!busy && employees.length === 0 && <p className="py-6 text-center text-sm text-secondary-500">No employees match the current search.</p>}</div>
          {totalPages > 1 && <div className="mt-3 flex items-center justify-between text-xs text-secondary-500"><span>Page {page} of {totalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || page <= 1} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={busy || page >= totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div></div>}
        </Card>
      </div>
      {selected && <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {security.canCreate('EMPLOYEE_ASSIGNMENT') && selected.isActive && <FormPanel title={`New placement · ${selected.firstName} ${selected.lastName}`} description="Overlapping effective dates are rejected by the server." icon={<Briefcase className="h-5 w-5" />}>
          <OrganizationMasterPicker kind="department" label="Department" value={assignment.departmentPublicId} onChange={value => setAssignment(current => ({ ...current, departmentPublicId: value, unitPublicId: '', positionPublicId: '' }))} required />
          <OrganizationMasterPicker kind="unit" label="Unit" value={assignment.unitPublicId} departmentPublicId={assignment.departmentPublicId || undefined} emptyLabel="No unit" disabled={!assignment.departmentPublicId} onChange={value => setAssignment(current => ({ ...current, unitPublicId: value, positionPublicId: '' }))} />
          <OrganizationMasterPicker kind="position" label="Position" value={assignment.positionPublicId} departmentPublicId={assignment.departmentPublicId || undefined} unitPublicId={assignment.unitPublicId || undefined} emptyLabel="Select governed position" disabled={!assignment.departmentPublicId} onChange={value => setAssignment(current => ({ ...current, positionPublicId: value }))} required />
          <div className="grid grid-cols-2 gap-2"><Input label="Effective from" type="date" value={assignment.effectiveFrom} onChange={event => setAssignment(current => ({ ...current, effectiveFrom: event.target.value }))} /><Input label="Effective to" type="date" value={assignment.effectiveTo} onChange={event => setAssignment(current => ({ ...current, effectiveTo: event.target.value }))} /></div>
          <Checkbox label="Primary placement" checked={assignment.isPrimary} onChange={event => setAssignment(current => ({ ...current, isPrimary: event.target.checked }))} />
          <Button onClick={() => void saveAssignment()} disabled={busy}>Create placement</Button>
        </FormPanel>}
        <Card className="p-4"><div className="flex flex-wrap items-end justify-between gap-2"><div><h3 className="font-semibold">Placement history</h3><p className="text-xs text-secondary-500">{selected.employeeNumber ?? 'Protected employee number'} · {assignmentTotalCount} retained placement{assignmentTotalCount === 1 ? '' : 's'}</p></div><Input aria-label="Search placement history" placeholder="Position, department, or unit" value={assignmentSearchInput} onChange={event => setAssignmentSearchInput(event.target.value)} /></div>{security.canUpdate('EMPLOYEE') && selected.isActive && <div className="mt-3 flex flex-wrap items-end gap-2 rounded-lg bg-secondary-50 p-3 dark:bg-secondary-800"><div className="min-w-64 flex-1"><Textarea label="Employee deactivation reason" value={deactivationReason} onChange={event => setDeactivationReason(event.target.value)} required /></div><Button size="sm" variant="outline" onClick={() => void deactivateEmployee()} disabled={busy}>Deactivate employee</Button></div>}<div className="mt-3 space-y-2">{assignments.map(item => <div key={item.publicId} className="rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex justify-between gap-3"><div><p className="font-medium">{item.positionName}</p><p className="text-xs text-secondary-500">{item.positionCode} · {item.departmentName}{item.unitName ? ` / ${item.unitName}` : ''}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isPrimary ? 'Primary' : 'Additional'}</Badge></div><p className="mt-1 text-xs text-secondary-500">{new Date(item.effectiveFrom).toLocaleDateString()} — {item.effectiveTo ? new Date(item.effectiveTo).toLocaleDateString() : 'Current'}</p>{item.isActive && security.canUpdate('EMPLOYEE_ASSIGNMENT') && <div className="mt-3 space-y-2 rounded-lg bg-secondary-50 p-3 dark:bg-secondary-800"><p className="text-xs font-semibold text-secondary-700 dark:text-secondary-200">End this placement</p><Input label="Placement end date" type="date" value={closure.effectiveTo} onChange={event => setClosure(current => ({ ...current, effectiveTo: event.target.value }))} /><Input label="Closure reason" value={closure.reason} onChange={event => setClosure(current => ({ ...current, reason: event.target.value }))} required /><Button size="sm" variant="outline" onClick={() => void closeAssignment(item)} disabled={busy}>End placement</Button></div>}</div>)}{!assignments.length && <p className="text-sm text-secondary-500">No placements match the current history search.</p>}</div>{assignmentTotalPages > 1 && <div className="mt-3 flex items-center justify-between text-xs text-secondary-500"><span>Placement page {assignmentPage} of {assignmentTotalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || assignmentPage <= 1} onClick={() => void loadAssignments(selected.publicId, assignmentPage - 1, assignmentSearch)}>Previous placements</Button><Button size="sm" variant="outline" disabled={busy || assignmentPage >= assignmentTotalPages} onClick={() => void loadAssignments(selected.publicId, assignmentPage + 1, assignmentSearch)}>Next placements</Button></div></div>}</Card>
      </div>}
    </div>
  </AppShell>;
}
