import { useCallback, useEffect, useState } from 'react';
import { History, Plus, RefreshCw, Save } from 'lucide-react';
import {
  createPerformancePeriodTarget,
  getPerformanceConfigurationCatalogue,
  getPerformancePeriodTargets,
  revisePerformancePeriodTarget,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { PerformanceConfigurationCatalogueDto, PerformancePeriodTargetDto } from '../../types';
import { Input, Select, Textarea } from '../common/Form';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { Badge, Button, Card } from '../ui';
import { RevisionHistoryRegister } from './RevisionHistoryRegister';

const legacyUnits = [
  [1, 'Percentage'], [2, 'Absolute count'], [3, 'Financial'], [4, 'Time'], [5, 'Area'],
  [6, 'Volume'], [7, 'Index score'], [8, 'Ratio'], [9, 'Binary'], [10, 'Date'],
  [11, 'Readiness scale'], [12, 'Binary determination'], [13, 'Qualitative'], [14, 'Zero based'],
  [15, 'Reverse cumulative'], [16, 'Reverse non-cumulative'],
] as const;
const legacyDirections = [[1, 'Higher is better'], [2, 'Lower is better'], [3, 'Exact target']] as const;

type EditorState = {
  reportingPeriodPublicId: string;
  unitKind: number;
  direction: number;
  opmsUnitPublicId: string;
  performanceDirectionPublicId: string;
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

const blank = (configuration?: PerformanceConfigurationCatalogueDto): EditorState => {
  const unit = configuration?.opmsUnits[0];
  const direction = configuration?.performanceDirections.find(item => item.publicId === unit?.defaultPerformanceDirectionPublicId);
  return ({
  reportingPeriodPublicId: '', unitKind: unit?.engineUnitKind ?? 2, direction: direction?.engineDirection ?? 1,
  opmsUnitPublicId: unit?.publicId ?? '', performanceDirectionPublicId: direction?.publicId ?? '', targetValue: '', budgetValue: '', description: '',
  isActive: true, isTargetRevised: false, isBudgetRevised: false, reason: '', approvalReference: '', effectiveAt: new Date().toISOString().slice(0, 16),
  });
};

export function PerformancePeriodTargetEditor({ kind, targetPublicId }: { kind: 1 | 2; targetPublicId: string }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const resource = kind === 1 ? 'OPMS_KPI' : 'IPMS_KPI';
  const canCreate = security.canCreate(resource);
  const canRevise = security.canExecute(`${resource}.REVISE`);
  const [rows, setRows] = useState<PeriodTarget[]>([]);
  const [editing, setEditing] = useState<PeriodTarget | null>(null);
  const [creating, setCreating] = useState(false);
  const [historyRefresh, setHistoryRefresh] = useState(0);
  const [configuration, setConfiguration] = useState<PerformanceConfigurationCatalogueDto>();
  const [form, setForm] = useState<EditorState>(blank);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setBusy(true); setError('');
    const [targetResult, configurationResult] = await Promise.all([
      getPerformancePeriodTargets(kind, targetPublicId),
      getPerformanceConfigurationCatalogue(),
    ]);
    setRows((targetResult.data ?? []).map(row => ({ ...row, periodName: row.periodCode })));
    if (configurationResult.success && configurationResult.data) setConfiguration(configurationResult.data);
    if (!targetResult.success) setError(targetResult.message ?? 'Period targets could not be loaded.');
    else if (!configurationResult.success || !configurationResult.data) setError(configurationResult.message ?? 'OPMS unit configuration could not be loaded.');
    setBusy(false);
  }, [kind, targetPublicId]);

  useEffect(() => { void load(); }, [load]);

  const setValue = <K extends keyof EditorState>(key: K, value: EditorState[K]) => setForm(current => ({ ...current, [key]: value }));

  const startCreate = () => {
    setEditing(null); setCreating(true); setForm(blank(configuration));
  };

  const startRevision = async (row: PeriodTarget) => {
    setEditing(row);
    setCreating(false);
    setForm({
      reportingPeriodPublicId: row.reportingPeriodPublicId,
      unitKind: row.revisedUnitKind ?? row.originalUnitKind,
      direction: row.direction,
      opmsUnitPublicId: row.opmsUnitPublicId ?? configuration?.opmsUnits.find(item => item.engineUnitKind === row.unitKind)?.publicId ?? '',
      performanceDirectionPublicId: row.performanceDirectionPublicId ?? configuration?.performanceDirections.find(item => item.engineDirection === row.direction)?.publicId ?? '',
      targetValue: row.revisedTargetValue ?? row.originalTargetValue,
      budgetValue: row.revisedBudgetValue == null ? (row.originalBudgetValue == null ? '' : String(row.originalBudgetValue)) : String(row.revisedBudgetValue),
      description: row.description ?? '', isActive: row.isActive,
      isTargetRevised: row.isTargetRevised, isBudgetRevised: row.isBudgetRevised,
      reason: '', approvalReference: '', effectiveAt: new Date().toISOString().slice(0, 16),
    });
  };

  const save = async () => {
    if (!form.targetValue.trim() || (!editing && !form.reportingPeriodPublicId) || !form.opmsUnitPublicId || !form.performanceDirectionPublicId) { setError('Reporting period, OPMS unit, performance direction and target value are required.'); return; }
    if (editing && (!form.reason.trim() || !form.approvalReference.trim())) { setError('A revision reason and approval reference are required.'); return; }
    setBusy(true); setError('');
    const common = { unitKind: form.unitKind, direction: form.direction, opmsUnitPublicId: form.opmsUnitPublicId, performanceDirectionPublicId: form.performanceDirectionPublicId, targetValue: form.targetValue.trim(), budgetValue: form.budgetValue ? Number(form.budgetValue) : undefined, description: form.description.trim() || undefined };
    const result = editing
      ? await revisePerformancePeriodTarget(editing.publicId, { ...common, isTargetRevised: form.isTargetRevised, isBudgetRevised: form.isBudgetRevised, isActive: form.isActive, reason: form.reason.trim(), approvalReference: form.approvalReference.trim(), effectiveAt: new Date(form.effectiveAt).toISOString(), rowVersion: editing.rowVersion })
      : await createPerformancePeriodTarget({ ...common, targetKind: kind, targetPublicId, reportingPeriodPublicId: form.reportingPeriodPublicId });
    if (!result.success) { setError(result.message ?? 'Period target could not be saved.'); setBusy(false); return; }
    pushToast('success', editing ? 'Period target revision recorded' : 'Period target created');
    if (editing) setHistoryRefresh(value => value + 1);
    setEditing(null); setCreating(false); setForm(blank(configuration));
    await load();
  };

  const unitOptions = configuration?.opmsUnits.map(item => ({ value: item.publicId, label: `${item.name}${item.symbol ? ` (${item.symbol})` : ''}` })) ?? [];
  const directionOptions = configuration?.performanceDirections.map(item => ({ value: item.publicId, label: item.name })) ?? [];
  const unitLabel = (row: PerformancePeriodTargetDto, original = false) => {
    const publicId = original ? row.originalOpmsUnitPublicId : row.opmsUnitPublicId;
    const code = original ? row.originalOpmsUnitCode : row.opmsUnitCode;
    return configuration?.opmsUnits.find(item => item.publicId === publicId)?.name
      ?? code
      ?? legacyUnits.find(item => item[0] === (original ? row.originalUnitKind : row.unitKind))?.[1]
      ?? 'Unknown unit';
  };
  const directionLabel = (row: PerformancePeriodTargetDto) => configuration?.performanceDirections.find(item => item.publicId === row.performanceDirectionPublicId)?.name
    ?? row.performanceDirectionCode
    ?? legacyDirections.find(item => item[0] === row.direction)?.[1]
    ?? 'Unknown direction';

  return (
    <Card className="border border-primary-200 bg-primary-50/30 p-4 dark:border-primary-900 dark:bg-primary-950/10">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div><h2 className="font-semibold text-secondary-900 dark:text-white">Authoritative reporting-period targets</h2><p className="mt-1 text-xs text-secondary-500">One typed, versioned value per KPI and reporting period. Revisions preserve approved history.</p></div>
        <div className="flex gap-2"><Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>{canCreate && <Button size="sm" variant="primary" icon={<Plus className="h-4 w-4" />} onClick={startCreate} disabled={busy}>Add period</Button>}</div>
      </div>
      {error && <div role="alert" className="mt-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {rows.map(row => <div key={row.publicId} className="rounded-lg border border-secondary-200 bg-white p-3 dark:border-secondary-700 dark:bg-secondary-900"><div className="flex items-center justify-between"><strong>{row.periodCode}</strong><div className="flex gap-1">{(row.isTargetRevised || row.isBudgetRevised) && <Badge variant="warning">Revised</Badge>}<Badge variant={row.isActive ? 'success' : 'default'}>{row.isActive ? 'Active' : 'Inactive'}</Badge></div></div><p className="mt-2 text-lg font-semibold">{row.targetValue}</p><p className="text-xs text-secondary-500">{unitLabel(row)} · {directionLabel(row)}</p>{row.isTargetRevised && <p className="mt-1 text-xs text-secondary-500">Original: {row.originalTargetValue} ({unitLabel(row, true)})</p>}{row.description && <p className="mt-2 text-xs text-secondary-600">{row.description}</p>}{canRevise && <Button className="mt-3" size="sm" variant="outline" icon={<History className="h-4 w-4" />} onClick={() => void startRevision(row)}>Revise</Button>}</div>)}
        {!rows.length && <p className="text-sm text-secondary-500">{busy ? 'Loading canonical values…' : 'No canonical period targets have been configured.'}</p>}
      </div>
      {(editing || creating) && <div className="mt-5 rounded-lg border border-secondary-200 bg-white p-4 dark:border-secondary-700 dark:bg-secondary-900"><h3 className="font-medium">{editing ? `Record approved ${editing.periodCode} revision` : 'Add reporting-period target'}</h3><div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-3"><CalendarMasterPicker kind="reporting-period" label="Reporting period" value={form.reportingPeriodPublicId} disabled={!!editing} selectedLabel={editing ? `${editing.periodCode} · ${editing.periodName}` : undefined} excludedValues={editing ? [] : rows.map(row => row.reportingPeriodPublicId)} onChange={value => setValue('reportingPeriodPublicId', value)} required />{editing && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isTargetRevised} onChange={event => setValue('isTargetRevised', event.target.checked)} /> Revised target and unit</label>}<Select label={editing ? 'Revised OPMS unit' : 'OPMS unit'} value={form.opmsUnitPublicId} disabled={!!editing && !form.isTargetRevised} options={unitOptions} onChange={event => { const unit = configuration?.opmsUnits.find(item => item.publicId === event.target.value); const direction = configuration?.performanceDirections.find(item => item.publicId === unit?.defaultPerformanceDirectionPublicId); setForm(current => ({ ...current, opmsUnitPublicId: event.target.value, unitKind: unit?.engineUnitKind ?? current.unitKind, performanceDirectionPublicId: direction?.publicId ?? current.performanceDirectionPublicId, direction: direction?.engineDirection ?? current.direction })); }} /><Select label="Performance direction" value={form.performanceDirectionPublicId} options={directionOptions} onChange={event => { const direction = configuration?.performanceDirections.find(item => item.publicId === event.target.value); setForm(current => ({ ...current, performanceDirectionPublicId: event.target.value, direction: direction?.engineDirection ?? current.direction })); }} /><Input label={editing ? 'Revised target value' : 'Target value'} value={form.targetValue} disabled={!!editing && !form.isTargetRevised} onChange={event => setValue('targetValue', event.target.value)} required={!editing || form.isTargetRevised} />{editing && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isBudgetRevised} onChange={event => setValue('isBudgetRevised', event.target.checked)} /> Revised budget</label>}<Input label={editing ? 'Revised budget value' : 'Budget value'} type="number" min="0" step="0.01" disabled={!!editing && !form.isBudgetRevised} value={form.budgetValue} onChange={event => setValue('budgetValue', event.target.value)} /><Textarea label="Description" value={form.description} onChange={event => setValue('description', event.target.value)} />{editing && <><Input label="External approval reference" value={form.approvalReference} onChange={event => setValue('approvalReference', event.target.value)} required /><Input label="Effective at" type="datetime-local" value={form.effectiveAt} onChange={event => setValue('effectiveAt', event.target.value)} required /><Textarea label="Revision reason" value={form.reason} onChange={event => setValue('reason', event.target.value)} required /><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isActive} onChange={event => setValue('isActive', event.target.checked)} /> Active value</label></>}</div><div className="mt-4"><Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => void save()} disabled={busy}>{editing ? 'Record revision' : 'Create target value'}</Button></div>{editing && <RevisionHistoryRegister key={editing.publicId} source="period" kind={kind === 1 ? 'opms' : 'ipms'} parentId={editing.publicId} title="Revision history" emptyMessage="No period-target revisions recorded." refreshKey={historyRefresh} compact />}</div>}
    </Card>
  );
}
