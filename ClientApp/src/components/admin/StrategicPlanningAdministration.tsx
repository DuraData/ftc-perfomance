import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link2, Plus, RefreshCw } from 'lucide-react';
import {
  disableStrategicPlanningRelationship,
  getStrategicPlanningMastersPage,
  getStrategicPlanningRelationships,
  linkStrategicPlanningRelationship,
  saveStrategicPlanningMaster,
  type StrategicPlanningMasterKind,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { StrategicPlanningMasterDto, StrategicPlanningRelationshipDto } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { FormPanel, Input, Select, Textarea } from '../common/Form';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';

const configuration: Record<StrategicPlanningMasterKind, { title: string; singular: string; resource: string }> = {
  'municipal-kpas': { title: 'Municipal KPAs', singular: 'Municipal KPA', resource: 'MUNICIPAL_KPA' },
  'strategic-goals': { title: 'Strategic Goals', singular: 'Strategic Goal', resource: 'STRATEGIC_GOAL' },
  'strategic-interventions': { title: 'Strategic Interventions', singular: 'Strategic Intervention', resource: 'STRATEGIC_INTERVENTION' },
  'strategic-objectives': { title: 'Strategic Objectives', singular: 'Strategic Objective', resource: 'STRATEGIC_OBJECTIVE' },
  'performance-objectives': { title: 'Performance Objectives', singular: 'Performance Objective', resource: 'PERFORMANCE_OBJECTIVE' },
};
const empty = () => ({ code: '', name: '', description: '', from: '', to: '', displayOrder: '10', isActive: 'true', reason: '' });
const relationTypes = [
  { value: 'municipal-kpa-strategic-goal', label: 'Municipal KPA → Strategic Goal', parent: 'municipal-kpas', child: 'strategic-goals' },
  { value: 'strategic-goal-intervention', label: 'Strategic Goal → Intervention', parent: 'strategic-goals', child: 'strategic-interventions' },
  { value: 'strategic-goal-objective', label: 'Strategic Goal → Strategic Objective', parent: 'strategic-goals', child: 'strategic-objectives' },
  { value: 'strategic-intervention-objective', label: 'Intervention → Strategic Objective', parent: 'strategic-interventions', child: 'strategic-objectives' },
  { value: 'strategic-objective-performance-objective', label: 'Strategic Objective → Performance Objective', parent: 'strategic-objectives', child: 'performance-objectives' },
] as const;

export function StrategicPlanningAdministration({ kind }: { kind: StrategicPlanningMasterKind }) {
  const { pushToast } = useApp(); const security = useSecurity(); const config = configuration[kind];
  const [rows, setRows] = useState<StrategicPlanningMasterDto[]>([]); const [selected, setSelected] = useState<StrategicPlanningMasterDto | null>(null);
  const [form, setForm] = useState(empty); const [search, setSearch] = useState(''); const [page, setPage] = useState(1); const [totalPages, setTotalPages] = useState(0);
  const [relationships, setRelationships] = useState<StrategicPlanningRelationshipDto[]>([]); const [relationshipReason, setRelationshipReason] = useState('');
  const [relationshipType, setRelationshipType] = useState(relationTypes[0].value); const [parentId, setParentId] = useState(''); const [childId, setChildId] = useState('');
  const [relationshipOptions, setRelationshipOptions] = useState<Record<string, StrategicPlanningMasterDto[]>>({});
  const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null);
  const relationship = relationTypes.find(item => item.value === relationshipType)!;

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [masters, links] = await Promise.all([
      getStrategicPlanningMastersPage(kind, { page, pageSize: 25, search: search || undefined, sortBy: 'displayOrder', sortDirection: 'asc' }, { includeInactive: true }),
      security.canRead('STRATEGIC_HIERARCHY') ? getStrategicPlanningRelationships(true) : Promise.resolve({ success: true, data: [] as StrategicPlanningRelationshipDto[] }),
    ]);
    if (!masters.success) setError(masters.message ?? `${config.title} could not be loaded.`);
    setRows(masters.data?.items ?? []); setTotalPages(masters.data?.totalPages ?? 0); setRelationships(links.data ?? []); setBusy(false);
  }, [config.title, kind, page, search, security]);
  useEffect(() => { void load(); }, [load]);

  useEffect(() => {
    if (!security.canCreate('STRATEGIC_HIERARCHY')) return;
    let cancelled = false;
    Promise.all(relationTypes.flatMap(item => [item.parent, item.child]).filter((value, index, values) => values.indexOf(value) === index).map(async optionKind => {
      const result = await getStrategicPlanningMastersPage(optionKind, { page: 1, pageSize: 100, sortBy: 'name', sortDirection: 'asc' });
      return [optionKind, result.data?.items ?? []] as const;
    })).then(entries => { if (!cancelled) setRelationshipOptions(Object.fromEntries(entries)); });
    return () => { cancelled = true; };
  }, [security]);

  const edit = (item: StrategicPlanningMasterDto) => { setSelected(item); setForm({ code: item.code ?? '', name: item.name, description: item.description ?? '', from: item.effectiveFromFinancialYearPublicId ?? '', to: item.effectiveToFinancialYearPublicId ?? '', displayOrder: String(item.displayOrder), isActive: String(item.isActive), reason: '' }); };
  const clear = () => { setSelected(null); setForm(empty()); };
  const save = async () => {
    const order = Number(form.displayOrder); if (!form.name.trim() || !Number.isInteger(order) || order < 0) { setError('Name and a non-negative whole-number display order are required.'); return; }
    if (form.reason.trim().length < 10) { setError('A governance reason of at least 10 characters is required.'); return; }
    setBusy(true); setError(null);
    const result = await saveStrategicPlanningMaster(kind, selected?.publicId ?? null, { code: form.code || null, name: form.name, description: form.description || null, effectiveFromFinancialYearPublicId: form.from || null, effectiveToFinancialYearPublicId: form.to || null, displayOrder: order, isActive: form.isActive === 'true', reason: form.reason, rowVersion: selected?.rowVersion });
    if (!result.success) setError(result.message ?? `${config.singular} could not be saved.`); else { pushToast('success', `${config.singular} saved with audit history`); clear(); await load(); } setBusy(false);
  };
  const link = async () => {
    if (!parentId || !childId || relationshipReason.trim().length < 10) { setError('Select both records and enter a reason of at least 10 characters.'); return; }
    const inactive = relationships.find(item => item.relationshipType === relationshipType && item.parentPublicId === parentId && item.childPublicId === childId && !item.isActive);
    setBusy(true); const result = await linkStrategicPlanningRelationship(relationshipType, parentId, childId, relationshipReason, inactive?.rowVersion);
    if (!result.success) setError(result.message ?? 'Relationship could not be created.'); else { pushToast('success', 'Optional strategic relationship saved'); setParentId(''); setChildId(''); setRelationshipReason(''); await load(); } setBusy(false);
  };
  const disable = async (item: StrategicPlanningRelationshipDto) => {
    if (relationshipReason.trim().length < 10) { setError('Enter a relationship reason of at least 10 characters first.'); return; }
    setBusy(true); const result = await disableStrategicPlanningRelationship(item.publicId, relationshipReason, item.rowVersion);
    if (!result.success) setError(result.message ?? 'Relationship could not be disabled.'); else { pushToast('success', 'Relationship disabled'); setRelationshipReason(''); await load(); } setBusy(false);
  };
  const parentOptions = useMemo(() => [{ value: '', label: 'Select parent' }, ...(relationshipOptions[relationship.parent] ?? []).map(item => ({ value: item.publicId, label: `${item.code ? `${item.code} · ` : ''}${item.name}` }))], [relationship.parent, relationshipOptions]);
  const childOptions = useMemo(() => [{ value: '', label: 'Select child' }, ...(relationshipOptions[relationship.child] ?? []).map(item => ({ value: item.publicId, label: `${item.code ? `${item.code} · ` : ''}${item.name}` }))], [relationship.child, relationshipOptions]);

  return <AppShell title={config.title} subtitle="Municipality-scoped, effective-dated strategic planning configuration">
    <div className="space-y-5">
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="flex gap-2"><Input label="Search register" value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} /><Button variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
      <div className="grid gap-5 xl:grid-cols-[0.85fr_1.15fr]">
        {(selected ? security.canUpdate(config.resource) : security.canCreate(config.resource)) && <FormPanel title={selected ? `Edit ${config.singular}` : `Create ${config.singular}`} description="Validity is optional and can span one or more configured municipal financial years." icon={<Plus className="h-5 w-5" />}>
          <div className="grid grid-cols-2 gap-2"><Input label="Code (optional)" value={form.code} onChange={event => setForm(current => ({ ...current, code: event.target.value }))} /><Input label="Display order" type="number" min="0" value={form.displayOrder} onChange={event => setForm(current => ({ ...current, displayOrder: event.target.value }))} /></div>
          <Input label="Name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} required /><Textarea label="Description" value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} />
          <CalendarMasterPicker kind="municipality-financial-year" label="Effective from (optional)" value={form.from} onChange={value => setForm(current => ({ ...current, from: value }))} selectedLabel={selected?.effectiveFromFinancialYearCode ?? undefined} />
          <CalendarMasterPicker kind="municipality-financial-year" label="Effective to (optional)" value={form.to} onChange={value => setForm(current => ({ ...current, to: value }))} selectedLabel={selected?.effectiveToFinancialYearCode ?? undefined} />
          <Select label="Status" value={form.isActive} options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]} onChange={event => setForm(current => ({ ...current, isActive: event.target.value }))} /><Textarea label="Governance reason" value={form.reason} onChange={event => setForm(current => ({ ...current, reason: event.target.value }))} required />
          <div className="flex gap-2"><Button onClick={() => void save()} disabled={busy}>{selected ? 'Save changes' : 'Create'}</Button>{selected && <Button variant="outline" onClick={clear}>Cancel</Button>}</div>
        </FormPanel>}
        <Card className="p-4"><div className="flex justify-between"><h3 className="font-semibold">Authoritative register</h3><Badge variant="primary">{rows.length}</Badge></div><div className="mt-3 space-y-2">{rows.map(item => <div key={item.publicId} className="flex items-center justify-between gap-3 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code || 'No code'} · order {item.displayOrder} · {item.effectiveFromFinancialYearCode || 'open'} to {item.effectiveToFinancialYearCode || 'open'}</p><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></div>{security.canUpdate(config.resource) && <Button size="sm" variant="outline" onClick={() => edit(item)}>Edit</Button>}</div>)}{!rows.length && <p className="text-sm text-secondary-500">No records.</p>}</div>{totalPages > 1 && <div className="mt-3 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>}</Card>
      </div>
      {security.canRead('STRATEGIC_HIERARCHY') && <Card className="p-4"><div className="flex items-center gap-2"><Link2 className="h-5 w-5" /><h3 className="font-semibold">Optional strategic relationships</h3></div><p className="mt-1 text-sm text-secondary-500">Configure only the relationships your municipality uses; no fixed hierarchy is imposed.</p>{security.canCreate('STRATEGIC_HIERARCHY') && <div className="mt-3 grid gap-2 lg:grid-cols-3"><Select label="Relationship" value={relationshipType} options={relationTypes.map(item => ({ value: item.value, label: item.label }))} onChange={event => { setRelationshipType(event.target.value as typeof relationshipType); setParentId(''); setChildId(''); }} /><Select label="Parent" value={parentId} options={parentOptions} onChange={event => setParentId(event.target.value)} /><Select label="Child" value={childId} options={childOptions} onChange={event => setChildId(event.target.value)} /><div className="lg:col-span-2"><Textarea label="Relationship governance reason" value={relationshipReason} onChange={event => setRelationshipReason(event.target.value)} /></div><Button onClick={() => void link()} disabled={busy}>Create relationship</Button></div>}<div className="mt-4 space-y-2">{relationships.map(item => <div key={item.publicId} className="flex items-center justify-between rounded border border-secondary-200 p-2 text-sm dark:border-secondary-700"><span>{item.parentName} → {item.childName} <Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></span>{item.isActive && security.canUpdate('STRATEGIC_HIERARCHY') && <Button size="sm" variant="ghost" onClick={() => void disable(item)}>Disable</Button>}</div>)}</div></Card>}
    </div>
  </AppShell>;
}
