import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertTriangle, CheckCircle2, DatabaseZap, RefreshCw } from 'lucide-react';
import { executeTargetNormalization, getTargetNormalizationPreview } from '../../api/api';
import type { PagedResult, TargetNormalizationPreviewDto } from '../../types';
import { useApp } from '../../context/AppContext';
import { Badge, Button, Card } from '../ui';
import { Select, Textarea } from '../common/Form';

const emptyPage = (page = 1): PagedResult<TargetNormalizationPreviewDto> => ({ items: [], page, pageSize: 50, totalCount: 0, totalPages: 0 });

export function TargetNormalizationAdministration() {
  const { permissions, pushToast } = useApp();
  const canNormalize = useMemo(() => permissions.some(code => code.toLowerCase() === 'opms_kpi.normalize_legacy'), [permissions]);
  const [kind, setKind] = useState<1 | 2>(1);
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<TargetNormalizationPreviewDto>>(emptyPage());
  const [selected, setSelected] = useState<string[]>([]);
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    const response = await getTargetNormalizationPreview(kind, page, 50);
    if (!response.success || !response.data) {
      setError(response.message ?? 'Target reconciliation preview could not be loaded.');
      setResult(emptyPage(page));
    } else {
      setResult(response.data);
      setSelected(current => current.filter(id => response.data!.items.some(item => item.targetPublicId === id && item.status === 'Ready')));
    }
    setBusy(false);
  }, [kind, page]);

  useEffect(() => { void load(); }, [load]);

  const ready = result.items.filter(item => item.status === 'Ready');
  const allReadySelected = ready.length > 0 && ready.every(item => selected.includes(item.targetPublicId));
  const toggleAllReady = () => setSelected(allReadySelected ? [] : ready.map(item => item.targetPublicId));
  const toggle = (id: string) => setSelected(current => current.includes(id) ? current.filter(value => value !== id) : [...current, id]);

  const execute = async () => {
    const normalizedReason = reason.trim();
    if (!canNormalize) { setError('You do not have permission to reconcile legacy targets.'); return; }
    if (!selected.length || normalizedReason.length < 10) { setError('Select at least one ready target and enter a governance reason of at least 10 characters.'); return; }
    setBusy(true);
    setError(null);
    const response = await executeTargetNormalization({ targetKind: kind, targetPublicIds: selected, reason: normalizedReason });
    if (!response.success || !response.data) setError(response.message ?? 'Target reconciliation failed.');
    else {
      pushToast('success', `${response.data.addedPeriodTargets} normalized period target${response.data.addedPeriodTargets === 1 ? '' : 's'} created`);
      setSelected([]);
      setReason('');
      await load();
    }
    setBusy(false);
  };

  return <div className="space-y-4">
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex gap-3"><DatabaseZap className="mt-0.5 h-5 w-5 text-primary-600" /><div><h3 className="font-semibold text-secondary-900 dark:text-white">Normalized target cutover</h3><p className="mt-1 max-w-3xl text-sm text-secondary-600 dark:text-secondary-300">Preview historic wide target values and create only missing governed period rows. Existing normalized values are never overwritten; ambiguous legacy revisions remain blocked for manual governance.</p></div></div>
        <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
      </div>
      {!canNormalize && <div role="alert" className="mt-3 rounded-lg border border-warning-200 bg-warning-50 p-3 text-sm text-warning-800">Read-only: `OPMS_KPI.NORMALIZE_LEGACY` is required to execute reconciliation.</div>}
      {error && <div role="alert" className="mt-3 rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 sm:grid-cols-[220px_1fr_auto] sm:items-end">
        <Select label="Target register" value={kind} options={[{ value: 1, label: 'OPMS targets' }, { value: 2, label: 'IPMS targets' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPage(1); setSelected([]); }} />
        <Textarea label="Reconciliation reason" value={reason} maxLength={1000} onChange={event => setReason(event.target.value)} placeholder="Approved cutover reference and reason" />
        <Button variant="primary" disabled={busy || !canNormalize || !selected.length || reason.trim().length < 10} onClick={() => void execute()}>Reconcile selected ({selected.length})</Button>
      </div>
    </Card>

    <Card className="overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-secondary-200 p-4 dark:border-secondary-700">
        <div><h3 className="font-semibold">Cutover preview</h3><p className="text-xs text-secondary-500">{result.totalCount} target{result.totalCount === 1 ? '' : 's'} · page {result.page} of {Math.max(1, result.totalPages)}</p></div>
        <Button size="sm" variant="outline" onClick={toggleAllReady} disabled={!ready.length || busy}>{allReadySelected ? 'Clear ready targets' : 'Select ready targets'}</Button>
      </div>
      <div className="divide-y divide-secondary-200 dark:divide-secondary-700">
        {result.items.map(item => <div key={item.targetPublicId} className="grid gap-2 p-4 md:grid-cols-[32px_1fr_auto] md:items-center">
          <input aria-label={`Select ${item.indicatorNumber}`} type="checkbox" checked={selected.includes(item.targetPublicId)} disabled={item.status !== 'Ready' || busy || !canNormalize} onChange={() => toggle(item.targetPublicId)} />
          <div><p className="font-medium text-secondary-900 dark:text-white">{item.indicatorNumber} · {item.targetName}</p>{item.error && <p className="mt-1 flex items-start gap-1 text-xs text-error-700"><AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0" />{item.error}</p>}</div>
          <div className="flex items-center gap-2"><Badge variant={item.status === 'Normalized' ? 'success' : item.status === 'Ready' ? 'warning' : 'error'}>{item.status}</Badge>{item.status === 'Ready' && <span className="text-xs text-secondary-500">{item.missingPeriodTargets} missing</span>}{item.status === 'Normalized' && <CheckCircle2 className="h-4 w-4 text-success-600" />}</div>
        </div>)}
        {!busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">No targets are available in this municipality.</p>}
        {busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">Loading reconciliation preview…</p>}
      </div>
      <div className="flex items-center justify-between border-t border-secondary-200 p-3 dark:border-secondary-700"><Button size="sm" variant="outline" disabled={busy || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={busy || page >= result.totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>
    </Card>
  </div>;
}
