import { useCallback, useEffect, useState } from 'react';
import { History, Plus, RefreshCw, Save } from 'lucide-react';
import {
  createPerformancePeriodTarget,
  getPerformancePeriodTargets,
  getPerformanceTargetRevisions,
  revisePerformancePeriodTarget,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { PerformancePeriodTargetDto, PerformanceTargetRevisionDto } from '../../types';
import { Input, Select, Textarea } from '../common/Form';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { Badge, Button, Card } from '../ui';

const units = [
  [1, 'Percentage'], [2, 'Absolute count'], [3, 'Financial'], [4, 'Time'], [5, 'Area'],
  [6, 'Volume'], [7, 'Index score'], [8, 'Ratio'], [9, 'Binary'], [10, 'Date'],
  [11, 'Readiness scale'], [12, 'Binary determination'], [13, 'Qualitative'], [14, 'Zero based'],
  [15, 'Reverse cumulative'], [16, 'Reverse non-cumulative'],
] as const;
const directions = [[1, 'Higher is better'], [2, 'Lower is better'], [3, 'Exact target']] as const;

type EditorState = {
  reportingPeriodPublicId: string;
  unitKind: number;
  direction: number;
  targetValue: string;
  budgetValue: string;
  description: string;
  isActive: boolean;
  isTargetRevised: boolean;
  isBudgetRevised: boolean;
  reason: string;
  approvalReference: string;
  effectiveAt: string;
};

type PeriodTarget = PerformancePeriodTargetDto & { periodName: string };

const blank = (): EditorState => ({
  reportingPeriodPublicId: '', unitKind: 2, direction: 1, targetValue: '', budgetValue: '', description: '',
  isActive: true, isTargetRevised: false, isBudgetRevised: false, reason: '', approvalReference: '', effectiveAt: new Date().toISOString().slice(0, 16),
});

export function PerformancePeriodTargetEditor({ kind, targetPublicId }: { kind: 1 | 2; targetPublicId: string }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const resource = kind === 1 ? 'OPMS_KPI' : 'IPMS_KPI';
  const canCreate = security.canCreate(resource);
  const canRevise = security.canExecute(`${resource}.REVISE`);
  const [rows, setRows] = useState<PeriodTarget[]>([]);
  const [editing, setEditing] = useState<PeriodTarget | null>(null);
  const [creating, setCreating] = useState(false);
  const [history, setHistory] = useState<PerformanceTargetRevisionDto[]>([]);
  const [form, setForm] = useState<EditorState>(blank);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setBusy(true); setError('');
    const targetResult = await getPerformancePeriodTargets(kind, targetPublicId);
    setRows((targetResult.data ?? []).map(row => ({ ...row, periodName: row.periodCode })));
    if (!targetResult.success) setError(targetResult.message ?? 'Period targets could not be loaded.');
    setBusy(false);
  }, [kind, targetPublicId]);

  useEffect(() => { void load(); }, [load]);

  const setValue = <K extends keyof EditorState>(key: K, value: EditorState[K]) => setForm(current => ({ ...current, [key]: value }));

  const startCreate = () => {
    setEditing(null); setCreating(true); setHistory([]); setForm(blank());
  };

  const startRevision = async (row: PeriodTarget) => {
    setEditing(row);
    setCreating(false);
    setForm({
      reportingPeriodPublicId: row.reportingPeriodPublicId,
      unitKind: row.revisedUnitKind ?? row.originalUnitKind,
      direction: row.direction,
      targetValue: row.revisedTargetValue ?? row.originalTargetValue,
      budgetValue: row.revisedBudgetValue == null ? (row.originalBudgetValue == null ? '' : String(row.originalBudgetValue)) : String(row.revisedBudgetValue),
      description: row.description ?? '', isActive: row.isActive,
      isTargetRevised: row.isTargetRevised, isBudgetRevised: row.isBudgetRevised,
      reason: '', approvalReference: '', effectiveAt: new Date().toISOString().slice(0, 16),
    });
    const result = await getPerformanceTargetRevisions(row.publicId);
    setHistory(result.data ?? []);
  };

  const save = async () => {
    if (!form.targetValue.trim() || (!editing && !form.reportingPeriodPublicId)) { setError('Reporting period and target value are required.'); return; }
    if (editing && (!form.reason.trim() || !form.approvalReference.trim())) { setError('A revision reason and approval reference are required.'); return; }
    setBusy(true); setError('');
    const common = { unitKind: form.unitKind, direction: form.direction, targetValue: form.targetValue.trim(), budgetValue: form.budgetValue ? Number(form.budgetValue) : undefined, description: form.description.trim() || undefined };
    const result = editing
      ? await revisePerformancePeriodTarget(editing.publicId, { ...common, isTargetRevised: form.isTargetRevised, isBudgetRevised: form.isBudgetRevised, isActive: form.isActive, reason: form.reason.trim(), approvalReference: form.approvalReference.trim(), effectiveAt: new Date(form.effectiveAt).toISOString(), rowVersion: editing.rowVersion })
      : await createPerformancePeriodTarget({ ...common, targetKind: kind, targetPublicId, reportingPeriodPublicId: form.reportingPeriodPublicId });
    if (!result.success) { setError(result.message ?? 'Period target could not be saved.'); setBusy(false); return; }
    pushToast('success', editing ? 'Period target revision recorded' : 'Period target created');
    setEditing(null); setCreating(false); setHistory([]); setForm(blank());
    await load();
  };

  return (
    <Card className="border border-primary-200 bg-primary-50/30 p-4 dark:border-primary-900 dark:bg-primary-950/10">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div><h2 className="font-semibold text-secondary-900 dark:text-white">Authoritative reporting-period targets</h2><p className="mt-1 text-xs text-secondary-500">One typed, versioned value per KPI and reporting period. Revisions preserve approved history.</p></div>
        <div className="flex gap-2"><Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>{canCreate && <Button size="sm" variant="primary" icon={<Plus className="h-4 w-4" />} onClick={startCreate} disabled={busy}>Add period</Button>}</div>
      </div>
      {error && <div role="alert" className="mt-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {rows.map(row => <div key={row.publicId} className="rounded-lg border border-secondary-200 bg-white p-3 dark:border-secondary-700 dark:bg-secondary-900"><div className="flex items-center justify-between"><strong>{row.periodCode}</strong><div className="flex gap-1">{(row.isTargetRevised || row.isBudgetRevised) && <Badge variant="warning">Revised</Badge>}<Badge variant={row.isActive ? 'success' : 'default'}>{row.isActive ? 'Active' : 'Inactive'}</Badge></div></div><p className="mt-2 text-lg font-semibold">{row.targetValue}</p><p className="text-xs text-secondary-500">{units.find(unit => unit[0] === row.unitKind)?.[1] ?? 'Unknown unit'} · {directions.find(direction => direction[0] === row.direction)?.[1]}</p>{row.isTargetRevised && <p className="mt-1 text-xs text-secondary-500">Original: {row.originalTargetValue} ({units.find(unit => unit[0] === row.originalUnitKind)?.[1]})</p>}{row.description && <p className="mt-2 text-xs text-secondary-600">{row.description}</p>}{canRevise && <Button className="mt-3" size="sm" variant="outline" icon={<History className="h-4 w-4" />} onClick={() => void startRevision(row)}>Revise</Button>}</div>)}
        {!rows.length && <p className="text-sm text-secondary-500">{busy ? 'Loading canonical values…' : 'No canonical period targets have been configured.'}</p>}
      </div>
      {(editing || creating) && <div className="mt-5 rounded-lg border border-secondary-200 bg-white p-4 dark:border-secondary-700 dark:bg-secondary-900"><h3 className="font-medium">{editing ? `Record approved ${editing.periodCode} revision` : 'Add reporting-period target'}</h3><div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-3"><CalendarMasterPicker kind="reporting-period" label="Reporting period" value={form.reportingPeriodPublicId} disabled={!!editing} selectedLabel={editing ? `${editing.periodCode} · ${editing.periodName}` : undefined} excludedValues={editing ? [] : rows.map(row => row.reportingPeriodPublicId)} onChange={value => setValue('reportingPeriodPublicId', value)} required />{editing && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isTargetRevised} onChange={event => setValue('isTargetRevised', event.target.checked)} /> Revised target and unit</label>}<Select label={editing ? 'Revised unit' : 'Unit'} value={form.unitKind} disabled={!!editing && !form.isTargetRevised} options={units.map(([value, label]) => ({ value, label }))} onChange={event => setValue('unitKind', Number(event.target.value))} /><Select label="Direction" value={form.direction} options={directions.map(([value, label]) => ({ value, label }))} onChange={event => setValue('direction', Number(event.target.value))} /><Input label={editing ? 'Revised target value' : 'Target value'} value={form.targetValue} disabled={!!editing && !form.isTargetRevised} onChange={event => setValue('targetValue', event.target.value)} required={!editing || form.isTargetRevised} />{editing && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isBudgetRevised} onChange={event => setValue('isBudgetRevised', event.target.checked)} /> Revised budget</label>}<Input label={editing ? 'Revised budget value' : 'Budget value'} type="number" min="0" step="0.01" disabled={!!editing && !form.isBudgetRevised} value={form.budgetValue} onChange={event => setValue('budgetValue', event.target.value)} /><Textarea label="Description" value={form.description} onChange={event => setValue('description', event.target.value)} />{editing && <><Input label="External approval reference" value={form.approvalReference} onChange={event => setValue('approvalReference', event.target.value)} required /><Input label="Effective at" type="datetime-local" value={form.effectiveAt} onChange={event => setValue('effectiveAt', event.target.value)} required /><Textarea label="Revision reason" value={form.reason} onChange={event => setValue('reason', event.target.value)} required /><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isActive} onChange={event => setValue('isActive', event.target.checked)} /> Active value</label></>}</div><div className="mt-4"><Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => void save()} disabled={busy}>{editing ? 'Record revision' : 'Create target value'}</Button></div>{editing && history.length > 0 && <div className="mt-4"><h4 className="text-sm font-medium">Revision history</h4><ul className="mt-2 space-y-1 text-xs text-secondary-600">{history.map(item => <li key={item.publicId}>{new Date(item.recordedAt).toLocaleString()} · {item.fieldName}: {item.originalValue ?? '—'} → {item.revisedValue ?? '—'} · {item.approvalReference}</li>)}</ul></div>}</div>}
    </Card>
  );
}
