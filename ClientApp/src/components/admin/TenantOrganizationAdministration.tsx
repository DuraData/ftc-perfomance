import { useCallback, useEffect, useState } from 'react';
import { Building2, Plus, RefreshCw } from 'lucide-react';
import { getDepartmentMastersPage, getPositionMastersPage, getUnitMastersPage, saveDepartmentMaster, savePositionMaster, saveUnitMaster } from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { DepartmentMasterDto, PositionMasterDto, UnitMasterDto } from '../../types';
import { AppShell } from '../layout/AppShell';
import { FormPanel, Input, Select, Textarea } from '../common/Form';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';
import { Badge, Button, Card } from '../ui';

type MasterKind = 'departments' | 'units' | 'positions';
type MasterRow = DepartmentMasterDto | UnitMasterDto | PositionMasterDto;
const today = () => new Date().toISOString().slice(0, 10);
const dateValue = (value?: string | null) => value ? value.slice(0, 10) : '';
const atUtc = (value: string) => new Date(`${value}T00:00:00Z`).toISOString();

export function TenantOrganizationAdministration({ kind }: { kind: MasterKind }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [departments, setDepartments] = useState<DepartmentMasterDto[]>([]);
  const [units, setUnits] = useState<UnitMasterDto[]>([]);
  const [positions, setPositions] = useState<PositionMasterDto[]>([]);
  const [selected, setSelected] = useState<MasterRow | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');
  const [sort, setSort] = useState('name:asc');
  const [form, setForm] = useState({ departmentPublicId: '', unitPublicId: '', code: '', name: '', description: '', grade: '', isActive: 'true', effectiveFrom: today(), effectiveTo: '', reason: '' });
  const resource = kind === 'departments' ? 'DEPARTMENT' : kind === 'units' ? 'UNIT' : 'POSITION';
  const title = kind === 'departments' ? 'Departments' : kind === 'units' ? 'Department Units' : 'Positions';
  const rows = kind === 'departments' ? departments : kind === 'units' ? units : positions;
  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [sortBy, direction] = sort.split(':');
    const sortDirection: 'asc' | 'desc' = direction === 'asc' ? 'asc' : 'desc';
    const query = { page, pageSize: 25, search: search || undefined, sortBy, sortDirection, active: status === 'all' ? undefined : status === 'active' };
    const pageResult = await (kind === 'departments' ? getDepartmentMastersPage(query) : kind === 'units' ? getUnitMastersPage(query) : getPositionMastersPage(query));
    if (!pageResult.success) setError(pageResult.message ?? 'Organization masters could not be loaded.');
    const pageData = pageResult.data;
    if (kind === 'departments') setDepartments((pageData?.items ?? []) as DepartmentMasterDto[]);
    else if (kind === 'units') setUnits((pageData?.items ?? []) as UnitMasterDto[]);
    else setPositions((pageData?.items ?? []) as PositionMasterDto[]);
    setTotalCount(pageData?.totalCount ?? 0); setTotalPages(pageData?.totalPages ?? 0);
    if (pageData && pageData.items.length === 0 && page > 1) setPage(current => Math.max(1, current - 1));
    setBusy(false);
  }, [kind, page, search, sort, status]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => { setSearch(searchInput.trim()); setPage(1); }, 250); return () => window.clearTimeout(timer); }, [searchInput]);

  const clear = () => { setSelected(null); setForm({ departmentPublicId: '', unitPublicId: '', code: '', name: '', description: '', grade: '', isActive: 'true', effectiveFrom: today(), effectiveTo: '', reason: '' }); };
  const edit = (item: MasterRow) => {
    setSelected(item);
    setForm({
      departmentPublicId: 'departmentPublicId' in item ? item.departmentPublicId : '',
      unitPublicId: 'unitPublicId' in item ? item.unitPublicId ?? '' : '',
      code: item.code, name: item.name,
      description: 'description' in item ? item.description ?? '' : '',
      grade: 'grade' in item ? item.grade ?? '' : '',
      isActive: String(item.isActive), effectiveFrom: dateValue(item.effectiveFrom), effectiveTo: dateValue(item.effectiveTo), reason: '',
    });
  };

  const save = async () => {
    if (!form.code.trim() || !form.name.trim() || (kind !== 'departments' && !form.departmentPublicId)) { setError('Code, name, and the applicable department are required.'); return; }
    if (form.reason.trim().length < 10) { setError('A governance reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const common = { code: form.code, name: form.name, isActive: form.isActive === 'true', effectiveFrom: atUtc(form.effectiveFrom), effectiveTo: form.effectiveTo ? atUtc(form.effectiveTo) : null, reason: form.reason.trim(), rowVersion: selected?.rowVersion ?? null };
    const result = kind === 'departments'
      ? await saveDepartmentMaster(selected?.publicId ?? null, { ...common, description: form.description || null })
      : kind === 'units'
        ? await saveUnitMaster(selected?.publicId ?? null, { ...common, departmentPublicId: form.departmentPublicId })
        : await savePositionMaster(selected?.publicId ?? null, { ...common, departmentPublicId: form.departmentPublicId, unitPublicId: form.unitPublicId || null, grade: form.grade || null });
    if (!result.success) setError(result.message ?? `${title.slice(0, -1)} could not be saved.`);
    else { pushToast('success', `${title.slice(0, -1)} saved with audit history`); clear(); await load(); }
    setBusy(false);
  };

  return <AppShell title={title} subtitle="Tenant-scoped, effective-dated organization masters">
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="grid flex-1 gap-2 sm:grid-cols-3">
          <Input label={`Search ${title.toLowerCase()}`} value={searchInput} onChange={event => setSearchInput(event.target.value)} />
          <Select label="Status" value={status} options={[{ value: 'all', label: 'All statuses' }, { value: 'active', label: 'Active' }, { value: 'inactive', label: 'Inactive' }]} onChange={event => { setStatus(event.target.value); setPage(1); }} />
          <Select label="Sort" value={sort} options={[{ value: 'name:asc', label: 'Name A-Z' }, { value: 'name:desc', label: 'Name Z-A' }, { value: 'code:asc', label: 'Code A-Z' }, ...(kind === 'departments' ? [] : [{ value: 'department:asc', label: 'Department A-Z' }]), { value: 'effectiveFrom:desc', label: 'Newest effective date' }]} onChange={event => { setSort(event.target.value); setPage(1); }} />
        </div>
        <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
      </div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {(selected ? security.canUpdate(resource) : security.canCreate(resource)) && <FormPanel title={selected ? `Edit ${title.slice(0, -1).toLowerCase()}` : `Create ${title.slice(0, -1).toLowerCase()}`} description="All changes require a reason and use optimistic concurrency." icon={<Building2 className="h-5 w-5" />}>
          {kind !== 'departments' && <OrganizationMasterPicker kind="department" label="Department" value={form.departmentPublicId} selectedLabel={selected && 'departmentName' in selected ? selected.departmentName : undefined} onChange={value => setForm(current => ({ ...current, departmentPublicId: value, unitPublicId: '' }))} required />}
          {kind === 'positions' && <OrganizationMasterPicker kind="unit" label="Unit" value={form.unitPublicId} departmentPublicId={form.departmentPublicId || undefined} selectedLabel={selected && 'unitName' in selected ? selected.unitName ?? undefined : undefined} emptyLabel="Department-level position" disabled={!form.departmentPublicId} onChange={value => setForm(current => ({ ...current, unitPublicId: value }))} />}
          <div className="grid grid-cols-2 gap-2"><Input label="Code" value={form.code} onChange={event => setForm(current => ({ ...current, code: event.target.value }))} required /><Input label="Name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} required /></div>
          {kind === 'departments' && <Textarea label="Description" value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} />}
          {kind === 'positions' && <Input label="Grade" value={form.grade} onChange={event => setForm(current => ({ ...current, grade: event.target.value }))} />}
          <div className="grid grid-cols-2 gap-2"><Input label="Effective from" type="date" value={form.effectiveFrom} onChange={event => setForm(current => ({ ...current, effectiveFrom: event.target.value }))} /><Input label="Effective to" type="date" value={form.effectiveTo} onChange={event => setForm(current => ({ ...current, effectiveTo: event.target.value }))} /></div>
          <Select label="Status" value={form.isActive} options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]} onChange={event => setForm(current => ({ ...current, isActive: event.target.value }))} />
          <Textarea label="Governance reason" value={form.reason} onChange={event => setForm(current => ({ ...current, reason: event.target.value }))} required />
          <div className="flex gap-2"><Button icon={<Plus className="h-4 w-4" />} onClick={() => void save()} disabled={busy}>{selected ? 'Save changes' : 'Create'}</Button>{selected && <Button variant="outline" onClick={clear}>Cancel</Button>}</div>
        </FormPanel>}
        <Card className="p-4"><div className="flex items-center justify-between"><h3 className="font-semibold">{title} register</h3><Badge variant="primary">{totalCount}</Badge></div><div className="mt-3 space-y-2">{rows.map(item => <button type="button" key={item.publicId} onClick={() => edit(item)} className="flex w-full items-center justify-between rounded-lg border border-secondary-200 p-3 text-left dark:border-secondary-700"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code}{'departmentName' in item ? ` · ${item.departmentName}` : ''}{'unitName' in item && item.unitName ? ` / ${item.unitName}` : ''}</p><p className="mt-1 text-xs text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleDateString()} — {item.effectiveTo ? new Date(item.effectiveTo).toLocaleDateString() : 'open-ended'}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></button>)}{!rows.length && <p className="text-sm text-secondary-500">No records.</p>}</div>{totalPages > 1 && <div className="mt-4 flex items-center justify-between text-xs text-secondary-500"><span>Page {page} of {totalPages} · {totalCount} records</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={busy || page >= totalPages} onClick={() => setPage(current => current + 1)}>Next</Button></div></div>}</Card>
      </div>
    </div>
  </AppShell>;
}
