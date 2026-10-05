import { useCallback, useEffect, useState } from 'react';
import { Layers3, Plus, RefreshCw } from 'lucide-react';
import { getGlobalStrategicReferences, saveGlobalStrategicReference, setGlobalStrategicReferenceAvailability, type GlobalStrategicReferenceKind } from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { GlobalStrategicReferenceDto } from '../../types';
import { FormPanel, Input, Select, Textarea } from '../common/Form';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';

const emptyForm = () => ({ code: '', name: '', description: '', displayOrder: '10', isActive: 'true', reason: '' });

export function GlobalStrategicReferenceAdministration({ kind }: { kind: GlobalStrategicReferenceKind }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [rows, setRows] = useState<GlobalStrategicReferenceDto[]>([]);
  const [selected, setSelected] = useState<GlobalStrategicReferenceDto | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [availabilityReason, setAvailabilityReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isNationalKpa = kind === 'national-kpas';
  const resource = isNationalKpa ? 'NATIONAL_KPA' : 'BACK_TO_BASICS_PILLAR';
  const title = isNationalKpa ? 'National KPAs' : 'Back-to-Basics Pillars';

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const result = await getGlobalStrategicReferences(kind);
    if (!result.success) setError(result.message ?? `${title} could not be loaded.`);
    setRows(result.data ?? []);
    setBusy(false);
  }, [kind, title]);

  useEffect(() => { void load(); }, [load]);

  const clear = () => { setSelected(null); setForm(emptyForm()); };
  const edit = (item: GlobalStrategicReferenceDto) => {
    setSelected(item);
    setForm({ code: item.code, name: item.name, description: item.description ?? '', displayOrder: String(item.displayOrder), isActive: String(item.isActive), reason: '' });
  };

  const save = async () => {
    const displayOrder = Number(form.displayOrder);
    if (!form.code.trim() || !form.name.trim() || !Number.isInteger(displayOrder) || displayOrder < 0) { setError('Code, name, and a non-negative whole-number display order are required.'); return; }
    if (form.reason.trim().length < 10) { setError('A governance reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const result = await saveGlobalStrategicReference(kind, selected?.publicId ?? null, { code: form.code, name: form.name, description: form.description || null, displayOrder, isActive: form.isActive === 'true', reason: form.reason.trim(), rowVersion: selected?.rowVersion ?? null });
    if (!result.success) setError(result.message ?? 'Reference record could not be saved.');
    else { pushToast('success', `${title.slice(0, -1)} saved with audit history`); clear(); await load(); }
    setBusy(false);
  };

  const setAvailability = async (item: GlobalStrategicReferenceDto) => {
    if (availabilityReason.trim().length < 10) { setError('Enter an availability reason of at least 10 characters first.'); return; }
    setBusy(true); setError(null);
    const result = await setGlobalStrategicReferenceAvailability(kind, item.publicId, { isEnabled: !item.isEnabledForMunicipality, reason: availabilityReason.trim(), rowVersion: item.availabilityRowVersion ?? null });
    if (!result.success) setError(result.message ?? 'Municipality availability could not be changed.');
    else { pushToast('success', `${item.name} availability updated`); setAvailabilityReason(''); await load(); }
    setBusy(false);
  };

  return <AppShell title={title} subtitle="Global references with optional municipality availability controls">
    <div className="space-y-5">
      <div className="flex justify-end"><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        {(selected ? security.canUpdate(resource) : security.canCreate(resource)) && <FormPanel title={selected ? `Edit ${title.slice(0, -1)}` : `Create ${title.slice(0, -1)}`} description="Global changes require system scope and are protected by optimistic concurrency." icon={<Layers3 className="h-5 w-5" />}>
          <div className="grid grid-cols-2 gap-2"><Input label="Code" value={form.code} onChange={event => setForm(current => ({ ...current, code: event.target.value }))} required /><Input label="Display order" type="number" min="0" step="1" value={form.displayOrder} onChange={event => setForm(current => ({ ...current, displayOrder: event.target.value }))} required /></div>
          <Input label="Name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} required />
          <Textarea label="Description" value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} />
          <Select label="Global status" value={form.isActive} options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]} onChange={event => setForm(current => ({ ...current, isActive: event.target.value }))} />
          <Textarea label="Governance reason" value={form.reason} onChange={event => setForm(current => ({ ...current, reason: event.target.value }))} required />
          <div className="flex gap-2"><Button icon={<Plus className="h-4 w-4" />} onClick={() => void save()} disabled={busy}>{selected ? 'Save changes' : 'Create'}</Button>{selected && <Button variant="outline" onClick={clear}>Cancel</Button>}</div>
        </FormPanel>}
        <Card className="p-4">
          <div className="flex items-center justify-between"><h3 className="font-semibold">Authoritative register</h3><Badge variant="primary">{rows.length}</Badge></div>
          {security.canUpdate(resource) && <div className="mt-3"><Textarea label="Municipality availability reason" value={availabilityReason} onChange={event => setAvailabilityReason(event.target.value)} /></div>}
          <div className="mt-3 space-y-2">{rows.map(item => <div key={item.publicId} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code} · order {item.displayOrder}</p><div className="mt-2 flex gap-2"><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Globally active' : 'Globally inactive'}</Badge><Badge variant={item.isEnabledForMunicipality ? 'success' : 'warning'}>{item.isEnabledForMunicipality ? 'Available here' : 'Hidden here'}</Badge></div></div><div className="flex gap-2">{security.canUpdate(resource) && <Button size="sm" variant="outline" onClick={() => edit(item)}>Edit</Button>}{security.canUpdate(resource) && <Button size="sm" variant="ghost" onClick={() => void setAvailability(item)} disabled={busy}>{item.isEnabledForMunicipality ? 'Hide for municipality' : 'Enable for municipality'}</Button>}</div></div>)}{!rows.length && <p className="text-sm text-secondary-500">No records.</p>}</div>
        </Card>
      </div>
    </div>
  </AppShell>;
}
