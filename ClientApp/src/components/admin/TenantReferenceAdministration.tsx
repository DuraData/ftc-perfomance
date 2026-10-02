import { useCallback, useEffect, useState } from 'react';
import { Landmark, Plus, RefreshCw } from 'lucide-react';
import { getDepartmentMasters, getVoteNumberMasters, getWardMasters, saveVoteNumberMaster, saveWardMaster } from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { DepartmentMasterDto, VoteNumberMasterDto, WardMasterDto } from '../../types';
import { FormPanel, Input, Select, Textarea } from '../common/Form';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';

type ReferenceKind = 'wards' | 'vote-numbers';
type ReferenceRow = WardMasterDto | VoteNumberMasterDto;
const today = () => new Date().toISOString().slice(0, 10);
const dateValue = (value?: string | null) => value ? value.slice(0, 10) : '';
const atUtc = (value: string) => new Date(`${value}T00:00:00Z`).toISOString();
const emptyForm = () => ({ departmentPublicId: '', code: '', number: '', name: '', amount: '0', isActive: 'true', effectiveFrom: today(), effectiveTo: '', reason: '' });

export function TenantReferenceAdministration({ kind }: { kind: ReferenceKind }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [wards, setWards] = useState<WardMasterDto[]>([]);
  const [voteNumbers, setVoteNumbers] = useState<VoteNumberMasterDto[]>([]);
  const [departments, setDepartments] = useState<DepartmentMasterDto[]>([]);
  const [selected, setSelected] = useState<ReferenceRow | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isVote = kind === 'vote-numbers';
  const resource = isVote ? 'VOTE_NUMBER' : 'WARD';
  const title = isVote ? 'Vote Numbers' : 'Wards';
  const rows: ReferenceRow[] = isVote ? voteNumbers : wards;

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    if (isVote) {
      const [voteResult, departmentResult] = await Promise.all([getVoteNumberMasters(), getDepartmentMasters()]);
      const failed = [voteResult, departmentResult].find(item => !item.success);
      if (failed) setError(failed.message ?? `${title} could not be loaded.`);
      setVoteNumbers(voteResult.data ?? []); setDepartments(departmentResult.data ?? []);
    } else {
      const wardResult = await getWardMasters();
      if (!wardResult.success) setError(wardResult.message ?? `${title} could not be loaded.`);
      setWards(wardResult.data ?? []);
    }
    setBusy(false);
  }, [isVote, title]);

  useEffect(() => { void load(); }, [load]);

  const clear = () => { setSelected(null); setForm(emptyForm()); };
  const edit = (item: ReferenceRow) => {
    setSelected(item);
    setForm({
      departmentPublicId: 'departmentPublicId' in item ? item.departmentPublicId : '',
      code: item.code,
      number: 'number' in item ? item.number : '',
      name: item.name,
      amount: 'amount' in item ? String(item.amount) : '0',
      isActive: String(item.isActive),
      effectiveFrom: dateValue(item.effectiveFrom),
      effectiveTo: dateValue(item.effectiveTo),
      reason: '',
    });
  };

  const save = async () => {
    if (!form.code.trim() || !form.name.trim() || (isVote && (!form.departmentPublicId || !form.number.trim()))) { setError('Complete all required fields.'); return; }
    if (form.reason.trim().length < 10) { setError('A governance reason of at least 10 characters is required.'); return; }
    const amount = Number(form.amount);
    if (isVote && (!Number.isFinite(amount) || amount < 0)) { setError('Amount must be a non-negative number.'); return; }
    setBusy(true); setError(null);
    const common = { code: form.code, name: form.name, isActive: form.isActive === 'true', effectiveFrom: atUtc(form.effectiveFrom), effectiveTo: form.effectiveTo ? atUtc(form.effectiveTo) : null, reason: form.reason.trim(), rowVersion: selected?.rowVersion ?? null };
    const result = isVote
      ? await saveVoteNumberMaster(selected?.publicId ?? null, { ...common, departmentPublicId: form.departmentPublicId, number: form.number, amount })
      : await saveWardMaster(selected?.publicId ?? null, common);
    if (!result.success) setError(result.message ?? `${title.slice(0, -1)} could not be saved.`);
    else { pushToast('success', `${title.slice(0, -1)} saved with audit history`); clear(); await load(); }
    setBusy(false);
  };

  return <AppShell title={title} subtitle="Tenant-scoped, effective-dated reference masters">
    <div className="space-y-5">
      <div className="flex justify-end"><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {(selected ? security.canUpdate(resource) : security.canCreate(resource)) && <FormPanel title={selected ? `Edit ${title.slice(0, -1).toLowerCase()}` : `Create ${title.slice(0, -1).toLowerCase()}`} description="Updates are audited, reasoned, and protected by optimistic concurrency." icon={<Landmark className="h-5 w-5" />}>
          {isVote && <Select label="Department" value={form.departmentPublicId} placeholder="Select department" options={departments.filter(item => item.isActive).map(item => ({ value: item.publicId, label: `${item.code} · ${item.name}` }))} onChange={event => setForm(current => ({ ...current, departmentPublicId: event.target.value }))} required />}
          <div className="grid grid-cols-2 gap-2"><Input label="Code" value={form.code} onChange={event => setForm(current => ({ ...current, code: event.target.value }))} required />{isVote && <Input label="Vote number" value={form.number} onChange={event => setForm(current => ({ ...current, number: event.target.value }))} required />}</div>
          <Input label="Name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} required />
          {isVote && <Input label="Amount (R)" type="number" min="0" step="0.01" value={form.amount} onChange={event => setForm(current => ({ ...current, amount: event.target.value }))} required />}
          <div className="grid grid-cols-2 gap-2"><Input label="Effective from" type="date" value={form.effectiveFrom} onChange={event => setForm(current => ({ ...current, effectiveFrom: event.target.value }))} /><Input label="Effective to" type="date" value={form.effectiveTo} onChange={event => setForm(current => ({ ...current, effectiveTo: event.target.value }))} /></div>
          <Select label="Status" value={form.isActive} options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]} onChange={event => setForm(current => ({ ...current, isActive: event.target.value }))} />
          <Textarea label="Governance reason" value={form.reason} onChange={event => setForm(current => ({ ...current, reason: event.target.value }))} required />
          <div className="flex gap-2"><Button icon={<Plus className="h-4 w-4" />} onClick={() => void save()} disabled={busy}>{selected ? 'Save changes' : 'Create'}</Button>{selected && <Button variant="outline" onClick={clear}>Cancel</Button>}</div>
        </FormPanel>}
        <Card className="p-4"><div className="flex items-center justify-between"><h3 className="font-semibold">{title} register</h3><Badge variant="primary">{rows.length}</Badge></div><div className="mt-3 space-y-2">{rows.map(item => <button type="button" key={item.publicId} onClick={() => edit(item)} className="flex w-full items-center justify-between rounded-lg border border-secondary-200 p-3 text-left dark:border-secondary-700"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code}{'number' in item ? ` · ${item.number} · ${item.departmentName} · R ${item.amount.toLocaleString()}` : ''}</p><p className="mt-1 text-xs text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleDateString()} — {item.effectiveTo ? new Date(item.effectiveTo).toLocaleDateString() : 'open-ended'}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></button>)}{!rows.length && <p className="text-sm text-secondary-500">No records.</p>}</div></Card>
      </div>
    </div>
  </AppShell>;
}
