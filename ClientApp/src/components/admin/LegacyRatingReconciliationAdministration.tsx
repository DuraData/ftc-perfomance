import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertTriangle, CheckCircle2, History, RefreshCw } from 'lucide-react';
import { executeLegacyRatingReconciliation, getLegacyRatingReconciliationPreview } from '../../api/api';
import type { LegacyRatingReconciliationPreviewDto, LegacyRatingSourceKind, PagedResult } from '../../types';
import { useApp } from '../../context/AppContext';
import { Badge, Button, Card } from '../ui';
import { Select, Textarea } from '../common/Form';

const emptyPage = (page = 1): PagedResult<LegacyRatingReconciliationPreviewDto> => ({ items: [], page, pageSize: 25, totalCount: 0, totalPages: 0 });
const sourceOptions = [
  { value: 1, label: 'Generic submission score' },
  { value: 2, label: 'Submitter score' },
  { value: 3, label: 'Verifier score' },
  { value: 4, label: 'Approver score' },
  { value: 5, label: 'PMS score' },
  { value: 6, label: 'Auditor score' },
];

export function LegacyRatingReconciliationAdministration() {
  const { permissions, pushToast } = useApp();
  const canReconcile = useMemo(() => permissions.some(code => code.toLowerCase() === 'workflow.reconcile_legacy_ratings'), [permissions]);
  const [submissionKind, setSubmissionKind] = useState<1 | 2>(1);
  const [sourceKind, setSourceKind] = useState<LegacyRatingSourceKind>(1);
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<LegacyRatingReconciliationPreviewDto>>(emptyPage());
  const [selectedActions, setSelectedActions] = useState<Record<string, string>>({});
  const [reason, setReason] = useState('');
  const [busySource, setBusySource] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    const response = await getLegacyRatingReconciliationPreview(submissionKind, sourceKind, page, 25);
    if (!response.success || !response.data) {
      setError(response.message ?? 'Historical rating reconciliation preview could not be loaded.');
      setResult(emptyPage(page));
    } else {
      setResult(response.data);
      setSelectedActions(current => Object.fromEntries(Object.entries(current).filter(([key, action]) =>
        response.data!.items.some(item => item.sourceKey === key && item.candidates.some(candidate => candidate.workflowActionPublicId === action)))));
    }
    setLoading(false);
  }, [page, sourceKind, submissionKind]);

  useEffect(() => { void load(); }, [load]);

  const changeRegister = (kind: 1 | 2) => {
    setSubmissionKind(kind);
    setPage(1);
    setSelectedActions({});
  };
  const changeSource = (kind: LegacyRatingSourceKind) => {
    setSourceKind(kind);
    setPage(1);
    setSelectedActions({});
  };

  const execute = async (item: LegacyRatingReconciliationPreviewDto) => {
    const workflowActionPublicId = selectedActions[item.sourceKey];
    const normalizedReason = reason.trim();
    if (!canReconcile) { setError('You do not have permission to reconcile historical ratings.'); return; }
    if (!workflowActionPublicId || normalizedReason.length < 10) {
      setError('Select the evidenced workflow action and enter a governance reason of at least 10 characters.');
      return;
    }
    setBusySource(item.sourceKey);
    setError(null);
    const response = await executeLegacyRatingReconciliation({
      submissionKind: item.submissionKind,
      sourceKind: item.sourceKind,
      sourceKey: item.sourceKey,
      workflowActionPublicId,
      reason: normalizedReason,
    });
    if (!response.success || !response.data) setError(response.message ?? 'Historical rating reconciliation failed.');
    else {
      pushToast('success', response.data.usedExistingRating ? 'Historical score linked to existing canonical rating' : 'Canonical rating evidence created');
      setSelectedActions(current => { const next = { ...current }; delete next[item.sourceKey]; return next; });
      setReason('');
      await load();
    }
    setBusySource(null);
  };

  return <div className="space-y-4">
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex gap-3"><History className="mt-0.5 h-5 w-5 text-primary-600" /><div><h3 className="font-semibold text-secondary-900 dark:text-white">Historical rating reconciliation</h3><p className="mt-1 max-w-3xl text-sm text-secondary-600 dark:text-secondary-300">Link preserved legacy score evidence to one administrator-selected, same-submission and same-actor workflow action. The source remains unchanged, values must match the configured stage scheme exactly, and no workflow provenance is invented.</p></div></div>
        <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={loading || busySource !== null}>Refresh</Button>
      </div>
      {!canReconcile && <div role="alert" className="mt-3 rounded-lg border border-warning-200 bg-warning-50 p-3 text-sm text-warning-800">Read-only: `WORKFLOW.RECONCILE_LEGACY_RATINGS` is required to execute reconciliation.</div>}
      {error && <div role="alert" className="mt-3 rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 md:grid-cols-[220px_260px_1fr]">
        <Select label="Submission register" value={submissionKind} options={[{ value: 1, label: 'OPMS submissions' }, { value: 2, label: 'IPMS submissions' }]} onChange={event => changeRegister(Number(event.target.value) as 1 | 2)} />
        <Select label="Legacy score source" value={sourceKind} options={sourceOptions} onChange={event => changeSource(Number(event.target.value) as LegacyRatingSourceKind)} />
        <Textarea label="Reconciliation reason" value={reason} maxLength={1000} onChange={event => setReason(event.target.value)} placeholder="Approved cutover reference and evidence rationale" />
      </div>
    </Card>

    <Card className="overflow-hidden">
      <div className="border-b border-secondary-200 p-4 dark:border-secondary-700"><h3 className="font-semibold">Evidence preview</h3><p className="text-xs text-secondary-500">{result.totalCount} source record{result.totalCount === 1 ? '' : 's'} · page {result.page} of {Math.max(1, result.totalPages)}</p></div>
      <div className="divide-y divide-secondary-200 dark:divide-secondary-700">
        {result.items.map(item => {
          const selectable = item.status === 'Ready' || item.status === 'RequiresMapping';
          const selected = selectedActions[item.sourceKey] ?? '';
          const executing = busySource === item.sourceKey;
          return <div key={item.sourceKey} className="space-y-3 p-4">
            <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-medium text-secondary-900 dark:text-white">{item.submissionKind === 1 ? 'OPMS' : 'IPMS'} · {item.submissionPublicId}</p><p className="mt-1 text-sm">Legacy score <strong>{item.sourceScore}</strong> · {item.sourceActorName}{item.sourceOccurredAt ? ` · ${new Date(item.sourceOccurredAt).toLocaleString()}` : ''}</p><p className="mt-1 text-xs text-secondary-500">{item.statusReason}</p></div><Badge variant={item.status === 'Reconciled' ? 'success' : selectable ? 'warning' : 'error'}>{item.status}</Badge></div>
            {selectable && <div className="grid gap-3 md:grid-cols-[1fr_auto] md:items-end">
              <Select label={`Evidenced workflow action for ${item.submissionPublicId}`} value={selected} options={[{ value: '', label: 'Select an exact action…' }, ...item.candidates.map(candidate => ({ value: candidate.workflowActionPublicId, label: `${candidate.stageCode} · ${candidate.actionCode} · ${candidate.ratingLabel} (${candidate.ratingValue}) · ${candidate.actorName} · ${new Date(candidate.occurredAt).toLocaleString()}${candidate.hasMatchingCanonicalRating ? ' · canonical rating exists' : ''}` }))]} onChange={event => setSelectedActions(current => ({ ...current, [item.sourceKey]: event.target.value }))} />
              <Button variant="primary" disabled={!canReconcile || !selected || reason.trim().length < 10 || loading || busySource !== null} onClick={() => void execute(item)}>{executing ? 'Reconciling…' : 'Reconcile this source'}</Button>
            </div>}
            {item.status === 'Blocked' && <p className="flex items-start gap-1 text-xs text-error-700"><AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0" />Manual evidence correction is required before this source can be reconciled.</p>}
            {item.status === 'Reconciled' && <p className="flex items-center gap-1 text-xs text-success-700"><CheckCircle2 className="h-3.5 w-3.5" />Immutable reconciliation evidence recorded.</p>}
          </div>;
        })}
        {!loading && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">No legacy score evidence exists for this source.</p>}
        {loading && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">Loading historical rating evidence…</p>}
      </div>
      <div className="flex items-center justify-between border-t border-secondary-200 p-3 dark:border-secondary-700"><Button size="sm" variant="outline" disabled={loading || busySource !== null || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={loading || busySource !== null || page >= result.totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>
    </Card>
  </div>;
}
