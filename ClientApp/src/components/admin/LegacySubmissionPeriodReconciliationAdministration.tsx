import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertTriangle, CalendarRange, RefreshCw } from 'lucide-react';
import { executeLegacySubmissionPeriodReconciliation, getLegacySubmissionPeriodPreview } from '../../api/api';
import type { LegacySubmissionPeriodPreviewDto, PagedResult } from '../../types';
import { useApp } from '../../context/AppContext';
import { Badge, Button, Card } from '../ui';
import { Select, Textarea } from '../common/Form';

const emptyPage = (page = 1): PagedResult<LegacySubmissionPeriodPreviewDto> => ({ items: [], page, pageSize: 25, totalCount: 0, totalPages: 0 });

export function LegacySubmissionPeriodReconciliationAdministration() {
  const { permissions, pushToast } = useApp();
  const canReconcile = useMemo(() => permissions.some(code => code.toLowerCase() === 'workflow.reconcile_legacy_periods'), [permissions]);
  const [kind, setKind] = useState<1 | 2>(1);
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<LegacySubmissionPeriodPreviewDto>>(emptyPage());
  const [selections, setSelections] = useState<Record<string, string>>({});
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    const response = await getLegacySubmissionPeriodPreview(kind, page, 25);
    if (!response.success || !response.data) {
      setError(response.message ?? 'Legacy submission-period preview could not be loaded.');
      setResult(emptyPage(page));
    } else {
      setResult(response.data);
      setSelections({});
    }
    setBusy(false);
  }, [kind, page]);

  useEffect(() => { void load(); }, [load]);

  const execute = async (item: LegacySubmissionPeriodPreviewDto) => {
    const reportingPeriodPublicId = selections[item.submissionPublicId];
    const normalizedReason = reason.trim();
    if (!canReconcile || !reportingPeriodPublicId || normalizedReason.length < 10) {
      setError('Select one eligible canonical period and enter a governance reason of at least 10 characters.');
      return;
    }
    setBusy(true);
    setError(null);
    const response = await executeLegacySubmissionPeriodReconciliation({
      submissionKind: item.submissionKind,
      submissionPublicId: item.submissionPublicId,
      reportingPeriodPublicId,
      reason: normalizedReason,
      rowVersion: item.rowVersion,
    });
    if (!response.success || !response.data) setError(response.message ?? 'Submission-period reconciliation failed.');
    else {
      pushToast('success', `Submission linked to canonical period ${response.data.reportingPeriodCode}`);
      setReason('');
      await load();
    }
    setBusy(false);
  };

  return <div className="space-y-4">
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex gap-3"><CalendarRange className="mt-0.5 h-5 w-5 text-primary-600" /><div><h3 className="font-semibold text-secondary-900 dark:text-white">Legacy submission-period cutover</h3><p className="mt-1 max-w-3xl text-sm text-secondary-600 dark:text-secondary-300">Map preserved free-text quarters only to canonical periods already configured for the same target and financial year. Duplicate logical submissions and unknown quarter labels remain blocked; the source text is retained as immutable evidence.</p></div></div>
        <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
      </div>
      {!canReconcile && <div role="alert" className="mt-3 rounded-lg border border-warning-200 bg-warning-50 p-3 text-sm text-warning-800">Read-only: `WORKFLOW.RECONCILE_LEGACY_PERIODS` is required to execute reconciliation.</div>}
      {error && <div role="alert" className="mt-3 rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 sm:grid-cols-[220px_1fr] sm:items-end">
        <Select label="Submission register" value={kind} options={[{ value: 1, label: 'OPMS submissions' }, { value: 2, label: 'IPMS submissions' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPage(1); }} />
        <Textarea label="Period reconciliation reason" value={reason} maxLength={1000} onChange={event => setReason(event.target.value)} placeholder="Approved cutover reference and evidence" />
      </div>
    </Card>

    <Card className="overflow-hidden">
      <div className="border-b border-secondary-200 p-4 dark:border-secondary-700"><h3 className="font-semibold">Unreconciled submissions</h3><p className="text-xs text-secondary-500">{result.totalCount} submission{result.totalCount === 1 ? '' : 's'} · page {result.page} of {Math.max(1, result.totalPages)}</p></div>
      <div className="divide-y divide-secondary-200 dark:divide-secondary-700">
        {result.items.map(item => {
          const eligible = item.candidates.filter(candidate => !candidate.hasSubmissionConflict);
          const selected = selections[item.submissionPublicId] ?? '';
          return <div key={item.submissionPublicId} className="space-y-3 p-4">
            <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-medium text-secondary-900 dark:text-white">{item.indicatorNumber} · {item.targetName}</p><p className="text-xs text-secondary-500">Preserved quarter: {item.legacyQuarter || 'blank'}</p></div><Badge variant={item.status === 'Blocked' ? 'error' : 'warning'}>{item.status}</Badge></div>
            <p className={`text-sm ${item.status === 'Blocked' ? 'text-error-700' : 'text-secondary-600 dark:text-secondary-300'}`}>{item.status === 'Blocked' && <AlertTriangle className="mr-1 inline h-4 w-4" />}{item.message}</p>
            {item.candidates.some(candidate => candidate.hasSubmissionConflict) && <p className="text-xs text-error-700">A matching period already has another logical submission and cannot be selected.</p>}
            {eligible.length > 0 && <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-end"><Select label={`Canonical period for ${item.indicatorNumber}`} value={selected} placeholder="Select the evidenced period" options={eligible.map(candidate => ({ value: candidate.reportingPeriodPublicId, label: `${candidate.code} · ${candidate.name} · ${candidate.financialYearCode}` }))} onChange={event => setSelections(current => ({ ...current, [item.submissionPublicId]: event.target.value }))} /><Button variant="primary" disabled={busy || !canReconcile || !selected || reason.trim().length < 10} onClick={() => void execute(item)}>Reconcile {item.indicatorNumber}</Button></div>}
          </div>;
        })}
        {!busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">No unreconciled submissions are available in this municipality.</p>}
        {busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">Loading submission-period preview…</p>}
      </div>
      <div className="flex items-center justify-between border-t border-secondary-200 p-3 dark:border-secondary-700"><Button size="sm" variant="outline" disabled={busy || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={busy || page >= result.totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>
    </Card>
  </div>;
}
