import { useEffect, useMemo, useState } from 'react';
import { Briefcase, Plus, RefreshCw, UserRound } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormPanel, Input, Select } from '../common/Form';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import {
  closeEmployeeAssignment,
  createEmployeeAssignment,
  createMunicipalEmployee,
  getDepartments,
  getEmployeeAssignments,
  getMunicipalEmployees,
  getUnits,
  getUsers,
  updateMunicipalEmployee,
} from '../../api/api';
import type { AdminUserDetail, DepartmentLookupDto, EmployeeAssignmentMasterDto, MunicipalEmployeeDto, UnitLookupDto } from '../../types';

const today = () => new Date().toISOString().slice(0, 10);
const atUtc = (value: string) => new Date(`${value}T00:00:00Z`).toISOString();

export function TenantEmployeeAdministration() {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [departments, setDepartments] = useState<DepartmentLookupDto[]>([]);
  const [units, setUnits] = useState<UnitLookupDto[]>([]);
  const [users, setUsers] = useState<AdminUserDetail[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [assignments, setAssignments] = useState<EmployeeAssignmentMasterDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [employee, setEmployee] = useState({ employeeNumber: '', firstName: '', lastName: '', emailAddress: '', identityUserId: '', effectiveFrom: today() });
  const [assignment, setAssignment] = useState({ departmentPublicId: '', unitPublicId: '', positionCode: '', positionName: '', effectiveFrom: today(), effectiveTo: '', isPrimary: true });
  const [closure, setClosure] = useState({ effectiveTo: today(), reason: '' });
  const selected = employees.find(item => item.publicId === selectedId) ?? null;
  const selectedDepartmentId = departments.find(item => item.publicId === assignment.departmentPublicId)?.id;
  const availableUnits = useMemo(() => units.filter(item => !selectedDepartmentId || item.departmentId === selectedDepartmentId), [selectedDepartmentId, units]);

  const load = async () => {
    setBusy(true); setError(null);
    const [employeeResult, departmentResult, unitResult, userResult] = await Promise.all([getMunicipalEmployees(), getDepartments(), getUnits(), getUsers()]);
    const failed = [employeeResult, departmentResult, unitResult, userResult].find(result => !result.success);
    if (failed) setError(failed.message ?? 'Employee masters could not be loaded.');
    setEmployees(employeeResult.data ?? []); setDepartments(departmentResult.data ?? []); setUnits(unitResult.data ?? []); setUsers(userResult.data ?? []);
    setBusy(false);
  };

  useEffect(() => { void load(); }, []);

  const selectEmployee = async (publicId: string) => {
    setSelectedId(publicId); setBusy(true); setError(null);
    const result = await getEmployeeAssignments(publicId);
    if (!result.success) setError(result.message ?? 'Placement history could not be loaded.');
    setAssignments(result.data ?? []); setBusy(false);
  };

  const saveEmployee = async () => {
    if (!employee.employeeNumber.trim() || !employee.firstName.trim() || !employee.lastName.trim()) { setError('Employee number, first name, and last name are required.'); return; }
    setBusy(true); setError(null);
    const result = await createMunicipalEmployee({ ...employee, emailAddress: employee.emailAddress || null, identityUserId: employee.identityUserId || null, effectiveFrom: atUtc(employee.effectiveFrom), effectiveTo: null });
    if (!result.success) setError(result.message ?? 'Employee could not be created.');
    else { pushToast('success', 'Employee created'); setEmployee({ employeeNumber: '', firstName: '', lastName: '', emailAddress: '', identityUserId: '', effectiveFrom: today() }); await load(); }
    setBusy(false);
  };

  const deactivateEmployee = async () => {
    if (!selected) return;
    setBusy(true); setError(null);
    const result = await updateMunicipalEmployee(selected.publicId, { firstName: selected.firstName, lastName: selected.lastName, emailAddress: selected.emailAddress, identityUserId: selected.identityUserId, isActive: false, effectiveFrom: selected.effectiveFrom, effectiveTo: new Date().toISOString(), rowVersion: selected.rowVersion });
    if (!result.success) setError(result.message ?? 'Employee could not be deactivated.');
    else { pushToast('success', 'Employee deactivated without deleting placement history'); setSelectedId(''); setAssignments([]); await load(); }
    setBusy(false);
  };

  const saveAssignment = async () => {
    if (!selected || !assignment.departmentPublicId || !assignment.positionCode.trim() || !assignment.positionName.trim()) { setError('Employee, department, position code, and position name are required.'); return; }
    setBusy(true); setError(null);
    const result = await createEmployeeAssignment({ employeePublicId: selected.publicId, departmentPublicId: assignment.departmentPublicId, unitPublicId: assignment.unitPublicId || null, positionCode: assignment.positionCode, positionName: assignment.positionName, effectiveFrom: atUtc(assignment.effectiveFrom), effectiveTo: assignment.effectiveTo ? atUtc(assignment.effectiveTo) : null, isPrimary: assignment.isPrimary });
    if (!result.success) setError(result.message ?? 'Placement could not be created.');
    else { pushToast('success', 'Effective-dated placement created'); setAssignment({ departmentPublicId: '', unitPublicId: '', positionCode: '', positionName: '', effectiveFrom: today(), effectiveTo: '', isPrimary: true }); await selectEmployee(selected.publicId); }
    setBusy(false);
  };

  const closeAssignment = async (item: EmployeeAssignmentMasterDto) => {
    if (closure.reason.trim().length < 10) { setError('A closure reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const result = await closeEmployeeAssignment(item.publicId, { effectiveTo: atUtc(closure.effectiveTo), reason: closure.reason.trim(), rowVersion: item.rowVersion });
    if (!result.success) setError(result.message ?? 'Placement could not be ended.');
    else { pushToast('success', 'Placement ended with its history retained'); setClosure({ effectiveTo: today(), reason: '' }); await selectEmployee(item.employeePublicId); }
    setBusy(false);
  };

  return <AppShell title="Municipal Employees" subtitle="Tenant-owned people and effective-dated organizational placements">
    <div className="space-y-5">
      <div className="flex justify-end"><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {security.canCreate('EMPLOYEE') && <FormPanel title="Create employee" description="Identity linkage is optional and does not replace the municipal employee record." icon={<UserRound className="h-5 w-5" />}>
          <Input label="Employee number" value={employee.employeeNumber} onChange={event => setEmployee(current => ({ ...current, employeeNumber: event.target.value }))} required />
          <div className="grid grid-cols-2 gap-2"><Input label="First name" value={employee.firstName} onChange={event => setEmployee(current => ({ ...current, firstName: event.target.value }))} required /><Input label="Last name" value={employee.lastName} onChange={event => setEmployee(current => ({ ...current, lastName: event.target.value }))} required /></div>
          <Input label="Email" type="email" value={employee.emailAddress} onChange={event => setEmployee(current => ({ ...current, emailAddress: event.target.value }))} />
          <Select label="Linked login" value={employee.identityUserId} placeholder="No linked login" options={users.filter(item => item.user.isActive).map(item => ({ value: item.user.id, label: `${item.user.fullName} · ${item.user.email}` }))} onChange={event => setEmployee(current => ({ ...current, identityUserId: event.target.value }))} />
          <Input label="Effective from" type="date" value={employee.effectiveFrom} onChange={event => setEmployee(current => ({ ...current, effectiveFrom: event.target.value }))} />
          <Button icon={<Plus className="h-4 w-4" />} onClick={() => void saveEmployee()} disabled={busy}>Create employee</Button>
        </FormPanel>}
        <Card className="p-4"><h3 className="font-semibold">Employee register</h3><div className="mt-3 space-y-2">{employees.map(item => <button type="button" key={item.publicId} onClick={() => void selectEmployee(item.publicId)} className={`flex w-full items-center justify-between rounded-lg border p-3 text-left ${selectedId === item.publicId ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-secondary-200 dark:border-secondary-700'}`}><div><p className="font-medium">{item.firstName} {item.lastName}</p><p className="text-xs text-secondary-500">{item.employeeNumber} · {item.emailAddress || 'No email'}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></button>)}</div></Card>
      </div>
      {selected && <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {security.canCreate('EMPLOYEE_ASSIGNMENT') && selected.isActive && <FormPanel title={`New placement · ${selected.firstName} ${selected.lastName}`} description="Overlapping effective dates are rejected by the server." icon={<Briefcase className="h-5 w-5" />}>
          <Select label="Department" value={assignment.departmentPublicId} placeholder="Select department" options={departments.map(item => ({ value: item.publicId, label: `${item.code} · ${item.name}` }))} onChange={event => setAssignment(current => ({ ...current, departmentPublicId: event.target.value, unitPublicId: '' }))} />
          <Select label="Unit" value={assignment.unitPublicId} placeholder="No unit" options={availableUnits.map(item => ({ value: item.publicId, label: item.name }))} onChange={event => setAssignment(current => ({ ...current, unitPublicId: event.target.value }))} />
          <div className="grid grid-cols-2 gap-2"><Input label="Position code" value={assignment.positionCode} onChange={event => setAssignment(current => ({ ...current, positionCode: event.target.value }))} /><Input label="Position name" value={assignment.positionName} onChange={event => setAssignment(current => ({ ...current, positionName: event.target.value }))} /></div>
          <div className="grid grid-cols-2 gap-2"><Input label="Effective from" type="date" value={assignment.effectiveFrom} onChange={event => setAssignment(current => ({ ...current, effectiveFrom: event.target.value }))} /><Input label="Effective to" type="date" value={assignment.effectiveTo} onChange={event => setAssignment(current => ({ ...current, effectiveTo: event.target.value }))} /></div>
          <Checkbox label="Primary placement" checked={assignment.isPrimary} onChange={event => setAssignment(current => ({ ...current, isPrimary: event.target.checked }))} />
          <Button onClick={() => void saveAssignment()} disabled={busy}>Create placement</Button>
        </FormPanel>}
        <Card className="p-4"><div className="flex items-center justify-between gap-2"><div><h3 className="font-semibold">Placement history</h3><p className="text-xs text-secondary-500">{selected.employeeNumber} · immutable historical rows</p></div>{security.canUpdate('EMPLOYEE') && selected.isActive && <Button size="sm" variant="outline" onClick={() => void deactivateEmployee()} disabled={busy}>Deactivate employee</Button>}</div><div className="mt-3 space-y-2">{assignments.map(item => <div key={item.publicId} className="rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex justify-between gap-3"><div><p className="font-medium">{item.positionName}</p><p className="text-xs text-secondary-500">{item.positionCode} · {item.departmentName}{item.unitName ? ` / ${item.unitName}` : ''}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isPrimary ? 'Primary' : 'Additional'}</Badge></div><p className="mt-1 text-xs text-secondary-500">{new Date(item.effectiveFrom).toLocaleDateString()} — {item.effectiveTo ? new Date(item.effectiveTo).toLocaleDateString() : 'Current'}</p>{item.isActive && security.canUpdate('EMPLOYEE_ASSIGNMENT') && <div className="mt-3 space-y-2 rounded-lg bg-secondary-50 p-3 dark:bg-secondary-800"><p className="text-xs font-semibold text-secondary-700 dark:text-secondary-200">End this placement</p><Input label="Placement end date" type="date" value={closure.effectiveTo} onChange={event => setClosure(current => ({ ...current, effectiveTo: event.target.value }))} /><Input label="Closure reason" value={closure.reason} onChange={event => setClosure(current => ({ ...current, reason: event.target.value }))} required /><Button size="sm" variant="outline" onClick={() => void closeAssignment(item)} disabled={busy}>End placement</Button></div>}</div>)}{!assignments.length && <p className="text-sm text-secondary-500">No placement history.</p>}</div></Card>
      </div>}
    </div>
  </AppShell>;
}
