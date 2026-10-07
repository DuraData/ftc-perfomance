import { useCallback, useEffect, useState } from 'react';
import { getInternalAuditAssessmentsPage, getInternalAuditSubmission, saveInternalAuditAssessment } from '../../api/api';
import type { InternalAuditAssessmentDto, InternalAuditAssessmentOutcome, InternalAuditSubmissionDto, PagedResult } from '../../types';
import { Badge, Button } from '../ui';
import { Input, Select, Textarea } from '../common/Form';
import { useSecurity } from '../../context/SecurityContext';

const outcomeLabel: Record<InternalAuditAssessmentOutcome, string> = {
  1: 'IA Achievement',
  2: 'IA Not Achieved',
  3: 'Satisfactory',
  4: 'Not Satisfactory',
};

export function InternalAuditAssessmentPanel({ kind = 1, submissionId, canAssess }: { kind?: 1 | 2; submissionId: string; canAssess: boolean }) {
  const security = useSecurity();
  const resource = kind === 1 ? 'OPMS_SUBMISSION' : 'IPMS_SUBMISSION';
  const canReadObservation = security.canReadField(resource, 'InternalAuditObservation');
  const canEditObservation = security.canEditField(resource, 'InternalAuditObservation');
  const canReadComment = security.canReadField(resource, 'InternalAuditComment');
  const canEditComment = security.canEditField(resource, 'InternalAuditComment');
  const canReadFindings = security.canReadField(resource, 'InternalAuditFindings');
  const canEditFindings = security.canEditField(resource, 'InternalAuditFindings');
  const canReadRecommendation = security.canReadField(resource, 'InternalAuditRecommendation');
  const canEditRecommendation = security.canEditField(resource, 'InternalAuditRecommendation');
  const canReadScore = security.canReadField(resource, 'InternalAuditScore');
  const canEditScore = security.canEditField(resource, 'InternalAuditScore');
  const canReadAssessedBy = security.canReadField(resource, 'InternalAuditAssessedBy');
  const canReadRfi = security.canReadField(resource, 'InternalAuditRfi');
  const [data, setData] = useState<InternalAuditSubmissionDto | null>(null);
  const [history, setHistory] = useState<PagedResult<InternalAuditAssessmentDto> | null>(null);
  const [historyPage, setHistoryPage] = useState(1);
  const [historySearch, setHistorySearch] = useState('');
  const [outcome, setOutcome] = useState<InternalAuditAssessmentOutcome>(3);
  const [observation, setObservation] = useState('');
  const [comment, setComment] = useState('');
  const [findings, setFindings] = useState('');
  const [recommendation, setRecommendation] = useState('');
  const [score, setScore] = useState('');
  const [dueAt, setDueAt] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [result, historyResult] = await Promise.all([
      getInternalAuditSubmission(kind, submissionId),
      getInternalAuditAssessmentsPage(kind, submissionId, { page: historyPage, pageSize: 10, search: historySearch || undefined, sortBy: 'assessedAt', sortDirection: 'desc' }),
    ]);
    if (!result.success || !result.data) setError(result.message ?? 'Internal Audit assessment could not be loaded.');
    else if (!historyResult.success || !historyResult.data) setError(historyResult.message ?? 'Internal Audit history could not be loaded.');
    else {
      setData(result.data);
      setHistory(historyResult.data);
      setOutcome(result.data.configuration.model === 2 ? 3 : 1);
    }
    setBusy(false);
  }, [historyPage, historySearch, kind, submissionId]);

  useEffect(() => { void load(); }, [load]);
  const adverse = outcome === 2 || outcome === 4;
  const simplified = data?.configuration.model === 2;

  const save = async () => {
    if (!observation.trim()) { setError('IA Detailed Observation is required.'); return; }
    if (adverse && !dueAt) { setError('IA RFI Due Date is required for an adverse assessment.'); return; }
    setBusy(true); setError(null);
    const result = await saveInternalAuditAssessment(kind, submissionId, {
      outcome,
      detailedObservation: observation.trim(),
      ...(simplified ? {} : {
        comment: canEditComment ? comment.trim() || undefined : undefined,
        findings: canEditFindings ? findings.trim() || undefined : undefined,
        recommendation: canEditRecommendation ? recommendation.trim() || undefined : undefined,
        score: canEditScore && score !== '' ? Number(score) : undefined,
      }),
      responseDueAt: adverse ? new Date(dueAt).toISOString() : undefined,
      previousAssessmentPublicId: data?.latestAssessment?.publicId,
    });
    if (!result.success) setError(result.message ?? 'Assessment could not be saved.');
    else { setObservation(''); setComment(''); setFindings(''); setRecommendation(''); setScore(''); setDueAt(''); if (historyPage !== 1) setHistoryPage(1); else await load(); }
    setBusy(false);
  };

  if (busy && !data) return <p className="text-sm text-secondary-500">Loading Internal Audit model…</p>;
  if (!data) return <div role="alert" className="text-sm text-error-600">{error ?? 'Internal Audit model unavailable.'}</div>;

  return <div className="space-y-4">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div><p className="text-sm font-semibold">{simplified ? 'Satisfactory / Not Satisfactory' : 'Detailed IA Assessment'}</p><p className="text-xs text-secondary-500">{data.configuration.financialYearCode} · model version {data.configuration.version}</p></div>
      <Badge variant="info">Append-only history</Badge>
    </div>
    {error && <div role="alert" className="rounded border border-error-200 bg-error-50 p-2 text-xs text-error-700">{error}</div>}
    {canAssess && canEditObservation && <div className="space-y-3 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700">
      <Select label="IA Assessment Status" value={outcome} options={(simplified ? [{ value: 3, label: 'Satisfactory' }, { value: 4, label: 'Not Satisfactory' }] : [{ value: 1, label: 'IA Achievement' }, { value: 2, label: 'IA Not Achieved' }])} onChange={event => setOutcome(Number(event.target.value) as InternalAuditAssessmentOutcome)} />
      <Textarea label="IA Detailed Observation" value={observation} maxLength={4000} onChange={event => setObservation(event.target.value)} required />
      {!simplified && <div className="grid gap-3 sm:grid-cols-2">{canEditComment && <Textarea label="Comments" value={comment} maxLength={2000} onChange={event => setComment(event.target.value)} />}{canEditFindings && <Textarea label="Findings" value={findings} maxLength={4000} onChange={event => setFindings(event.target.value)} />}{canEditRecommendation && <Textarea label="Recommendation" value={recommendation} maxLength={4000} onChange={event => setRecommendation(event.target.value)} />}{canEditScore && <Input label="IA score / rating" type="number" value={score} onChange={event => setScore(event.target.value)} />}</div>}
      {adverse && <Input label="IA RFI Due Date" type="datetime-local" value={dueAt} onChange={event => setDueAt(event.target.value)} required />}
      <div className="flex justify-end"><Button variant="primary" size="sm" onClick={() => void save()} disabled={busy}>{data.latestAssessment ? 'Append reassessment' : 'Record assessment'}</Button></div>
    </div>}
    <div className="space-y-2">
      <div className="flex flex-wrap items-end justify-between gap-2"><p className="text-xs font-semibold uppercase tracking-wide text-secondary-500">Assessment and RFI history · {history?.totalCount ?? 0}</p><Input aria-label="Search Internal Audit history" placeholder="Search observation, findings, recommendation or auditor" value={historySearch} onChange={event => { setHistorySearch(event.target.value); setHistoryPage(1); }} /></div>
      {history?.items.map(item => <div key={item.publicId} className="rounded-lg border border-secondary-200 p-3 text-xs dark:border-secondary-700"><div className="flex flex-wrap justify-between gap-2"><Badge variant={item.outcome === 1 || item.outcome === 3 ? 'success' : 'warning'}>{outcomeLabel[item.outcome]}</Badge><span className="text-secondary-500">{new Date(item.assessedAt).toLocaleString()}{canReadAssessedBy && (item.assessedByName || item.assessedByUserId) ? ` · ${item.assessedByName ?? item.assessedByUserId}` : ''}</span></div>{canReadObservation && item.detailedObservation && <p className="mt-2 whitespace-pre-wrap">{item.detailedObservation}</p>}{canReadComment && item.comment && <p className="mt-1"><strong>Comments:</strong> {item.comment}</p>}{canReadFindings && item.findings && <p className="mt-1"><strong>Findings:</strong> {item.findings}</p>}{canReadRecommendation && item.recommendation && <p className="mt-1"><strong>Recommendation:</strong> {item.recommendation}</p>}{canReadScore && item.score != null && <p className="mt-1"><strong>Score:</strong> {item.score}</p>}{canReadRfi && item.rfiPublicId && <p className="mt-1 text-warning-700">IA RFI due {item.rfiResponseDueAt ? new Date(item.rfiResponseDueAt).toLocaleString() : 'not recorded'}</p>}</div>)}
      {!history?.items.length && <p className="text-sm text-secondary-500">No Internal Audit assessments recorded.</p>}
      {(history?.totalPages ?? 0) > 1 && <div className="flex items-center justify-between text-xs text-secondary-500"><span>Page {historyPage} of {history?.totalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || historyPage <= 1} onClick={() => setHistoryPage(value => Math.max(1, value - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={busy || historyPage >= (history?.totalPages ?? 1)} onClick={() => setHistoryPage(value => value + 1)}>Next</Button></div></div>}
    </div>
  </div>;
}
