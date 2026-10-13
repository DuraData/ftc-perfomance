import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  getStrategicRiskAssessmentsPage, getStrategicRiskHeatmap, getStrategicRiskOwnersPage,
  getStrategicRiskReviewsPage, getStrategicRisksPage, getStrategicRiskTreatmentsPage,
  recordStrategicRiskAssessment, recordStrategicRiskReview, saveStrategicRiskTreatment,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type {
  StrategicRiskAssessmentDto, StrategicRiskDto, StrategicRiskHeatmapDto, StrategicRiskOwnerOptionDto,
  StrategicRiskReviewDto, StrategicRiskTreatmentDto,
} from '../../types';
import { Input, Select, Textarea } from '../common/Form';
import { Badge, Button, Card, EmptyState, StatCard } from '../ui';

export type RiskOperationsView = 'dashboard' | 'assessments' | 'treatments' | 'reviews' | 'heatmap' | 'reports';

const today = () => new Date().toISOString().slice(0, 10);
const futureDate = (days: number) => new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);
const toIso = (value: string) => new Date(`${value}T12:00:00`).toISOString();
const scores = [1, 2, 3, 4, 5].map(value => ({ value: String(value), label: String(value) }));
const riskLabel = (risk: StrategicRiskDto) => `${risk.riskReference ? `${risk.riskReference} · ` : ''}${risk.riskTitle}`;
const severity = (score: number) => score >= 16 ? 'Critical' : score >= 10 ? 'High' : score >= 5 ? 'Moderate' : 'Low';
const severityClass = (score: number) => score >= 16 ? 'bg-error-600 text-white' : score >= 10 ? 'bg-warning-500 text-white' : score >= 5 ? 'bg-warning-100 text-warning-900' : 'bg-success-100 text-success-900';

export function RiskOperations({ view }: { view: RiskOperationsView }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [risks, setRisks] = useState<StrategicRiskDto[]>([]);
  const [owners, setOwners] = useState<StrategicRiskOwnerOptionDto[]>([]);
  const [assessments, setAssessments] = useState<StrategicRiskAssessmentDto[]>([]);
  const [treatments, setTreatments] = useState<StrategicRiskTreatmentDto[]>([]);
  const [reviews, setReviews] = useState<StrategicRiskReviewDto[]>([]);
  const [heatmap, setHeatmap] = useState<StrategicRiskHeatmapDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);

  const [assessment, setAssessment] = useState({ risk: '', assessedAt: today(), inherentLikelihood: '3', inherentImpact: '3', residualLikelihood: '2', residualImpact: '2', controls: '', reason: '' });
  const [treatment, setTreatment] = useState({ publicId: '', risk: '', title: '', description: '', owner: '', dueAt: futureDate(30), status: 'OPEN', progress: '0', reason: '', rowVersion: '' });
  const [review, setReview] = useState({ risk: '', reviewedAt: today(), outcome: '', nextReviewAt: futureDate(90), reason: '' });

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const base = await Promise.all([
      getStrategicRisksPage({ page: 1, pageSize: 100, sortBy: 'reference', sortDirection: 'asc' }, { active: true }),
      getStrategicRiskHeatmap(),
      security.canReadField('STRATEGIC_RISK', 'OwnerUserId')
        ? getStrategicRiskOwnersPage({ page: 1, pageSize: 100, sortBy: 'fullName', sortDirection: 'asc' })
        : Promise.resolve({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 }, message: undefined as string | undefined }),
    ]);
    const query = { page, pageSize: 25, search, sortDirection: 'desc' as const };
    const [assessmentResult, treatmentResult, reviewResult] = await Promise.all([
      getStrategicRiskAssessmentsPage({ ...query, sortBy: 'assessedAt' }),
      getStrategicRiskTreatmentsPage({ ...query, sortBy: 'dueAt' }),
      getStrategicRiskReviewsPage({ ...query, sortBy: 'reviewedAt' }),
    ]);
    const failed = [...base, assessmentResult, treatmentResult, reviewResult].find(result => !result.success);
    if (failed) setError(failed.message ?? 'Risk operations could not be loaded.');
    setRisks(base[0].data?.items ?? []); setHeatmap(base[1].data ?? null); setOwners(base[2].data?.items ?? []);
    setAssessments(assessmentResult.data?.items ?? []); setTreatments(treatmentResult.data?.items ?? []); setReviews(reviewResult.data?.items ?? []);
    const relevant = view === 'assessments' ? assessmentResult.data : view === 'treatments' ? treatmentResult.data : view === 'reviews' ? reviewResult.data : null;
    setTotalPages(relevant?.totalPages ?? 0); setBusy(false);
  }, [page, search, security, view]);

  useEffect(() => { void load(); }, [load]);

  const riskOptions = [{ value: '', label: 'Select strategic risk' }, ...risks.map(item => ({ value: item.publicId, label: riskLabel(item) }))];
  const ownerOptions = [{ value: '', label: 'Select accountable owner' }, ...owners.map(item => ({ value: item.publicId, label: `${item.fullName} · ${item.email}` }))];
  const canAssess = security.canExecute('STRATEGIC_RISK.RECORD_ASSESSMENT') && security.canEditField('STRATEGIC_RISK', 'AssessmentControls');
  const canTreat = security.canExecute('STRATEGIC_RISK.MANAGE_TREATMENT')
    && security.canEditField('STRATEGIC_RISK', 'TreatmentDescription')
    && security.canReadField('STRATEGIC_RISK', 'OwnerUserId') && security.canEditField('STRATEGIC_RISK', 'OwnerUserId');
  const canReview = security.canExecute('STRATEGIC_RISK.RECORD_REVIEW') && security.canEditField('STRATEGIC_RISK', 'ReviewOutcome');

  const submitAssessment = async () => {
    if (!assessment.risk || assessment.reason.trim().length < 10) return setError('Select a risk and provide a substantive assessment reason.');
    setBusy(true); setError(null);
    const result = await recordStrategicRiskAssessment({ strategicRiskPublicId: assessment.risk, assessedAt: toIso(assessment.assessedAt), inherentLikelihood: Number(assessment.inherentLikelihood), inherentImpact: Number(assessment.inherentImpact), residualLikelihood: Number(assessment.residualLikelihood), residualImpact: Number(assessment.residualImpact), controlAssessment: assessment.controls.trim() || null, reason: assessment.reason.trim() });
    if (!result.success) setError(result.message ?? 'Assessment could not be recorded.');
    else { pushToast('success', 'Immutable risk assessment recorded.'); setAssessment(current => ({ ...current, controls: '', reason: '' })); await load(); }
    setBusy(false);
  };

  const submitTreatment = async () => {
    if (!treatment.risk || !treatment.owner || treatment.reason.trim().length < 10) return setError('Risk, owner and a substantive governance reason are required.');
    setBusy(true); setError(null);
    const result = await saveStrategicRiskTreatment(treatment.publicId || null, { strategicRiskPublicId: treatment.risk, title: treatment.title.trim(), description: treatment.description.trim() || null, ownerUserPublicId: treatment.owner, dueAt: toIso(treatment.dueAt), status: treatment.status as StrategicRiskTreatmentDto['status'], progressPercent: Number(treatment.progress), reason: treatment.reason.trim(), rowVersion: treatment.rowVersion || null });
    if (!result.success) setError(result.message ?? 'Treatment could not be saved.');
    else { pushToast('success', 'Risk treatment saved with audit evidence.'); setTreatment({ publicId: '', risk: '', title: '', description: '', owner: '', dueAt: futureDate(30), status: 'OPEN', progress: '0', reason: '', rowVersion: '' }); await load(); }
    setBusy(false);
  };

  const submitReview = async () => {
    if (!review.risk || review.reason.trim().length < 10) return setError('Select a risk and provide a substantive review reason.');
    setBusy(true); setError(null);
    const result = await recordStrategicRiskReview({ strategicRiskPublicId: review.risk, reviewedAt: toIso(review.reviewedAt), outcome: review.outcome.trim(), nextReviewAt: review.nextReviewAt ? toIso(review.nextReviewAt) : null, reason: review.reason.trim() });
    if (!result.success) setError(result.message ?? 'Review could not be recorded.');
    else { pushToast('success', 'Immutable risk review recorded.'); setReview(current => ({ ...current, outcome: '', reason: '' })); await load(); }
    setBusy(false);
  };

  const heatmapGrid = useMemo(() => {
    const counts = new Map((heatmap?.cells ?? []).map(cell => [`${cell.likelihood}:${cell.impact}`, cell.count]));
    return [5, 4, 3, 2, 1].map(likelihood => [1, 2, 3, 4, 5].map(impact => ({ likelihood, impact, count: counts.get(`${likelihood}:${impact}`) ?? 0 })));
  }, [heatmap]);

  const heatmapPanel = <Card>
    <div className="flex items-center justify-between"><div><h2 className="font-semibold">Residual risk heatmap</h2><p className="text-xs text-secondary-500">Latest immutable assessment per active strategic risk</p></div><Badge variant="primary">{heatmap?.assessedRisks ?? 0} assessed</Badge></div>
    <div className="mt-4 grid grid-cols-[auto_repeat(5,minmax(2.75rem,1fr))] gap-1 text-center text-xs">
      <span className="self-center font-medium [writing-mode:vertical-rl]">Likelihood</span>
      <div className="col-span-5 grid grid-cols-5 gap-1">{heatmapGrid.flat().map(cell => { const score = cell.likelihood * cell.impact; return <div key={`${cell.likelihood}-${cell.impact}`} aria-label={`Likelihood ${cell.likelihood}, impact ${cell.impact}: ${cell.count} risks, ${severity(score)}`} className={`rounded p-3 ${severityClass(score)}`}><span className="block text-base font-bold">{cell.count}</span><span>{cell.likelihood}×{cell.impact}</span></div>; })}</div>
      <span />
      <div className="col-span-5 grid grid-cols-5"><span>1</span><span>2</span><span>3</span><span>4</span><span>5</span></div>
      <span /><span className="col-span-5 font-medium">Impact</span>
    </div>
  </Card>;

  const searchPager = (label: string) => <div className="flex flex-wrap items-end justify-between gap-3"><Input label={`Search ${label}`} value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} /><div className="flex items-center gap-2 text-xs text-secondary-500"><span>Page {page} of {Math.max(totalPages, 1)}</span><Button size="sm" variant="outline" disabled={page <= 1 || busy} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= totalPages || busy} onClick={() => setPage(value => value + 1)}>Next</Button></div></div>;

  return <div className="space-y-5">
    {error && <div role="alert" className="rounded border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
    {(view === 'dashboard' || view === 'heatmap') && <><div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5"><StatCard title="Active risks" value={heatmap?.activeRisks ?? 0} /><StatCard title="Assessed" value={heatmap?.assessedRisks ?? 0} /><StatCard title="Unassessed" value={heatmap?.unassessedRisks ?? 0} /><StatCard title="Open treatments" value={heatmap?.openTreatments ?? 0} /><StatCard title="Overdue treatments" value={heatmap?.overdueTreatments ?? 0} /></div>{heatmapPanel}</>}

    {view === 'assessments' && <>
      {canAssess && <Card><h2 className="font-semibold">Record assessment</h2><div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-4"><Select label="Strategic risk" value={assessment.risk} options={riskOptions} onChange={event => setAssessment(current => ({ ...current, risk: event.target.value }))} /><Input label="Assessment date" type="date" value={assessment.assessedAt} onChange={event => setAssessment(current => ({ ...current, assessedAt: event.target.value }))} /><Select label="Inherent likelihood" value={assessment.inherentLikelihood} options={scores} onChange={event => setAssessment(current => ({ ...current, inherentLikelihood: event.target.value }))} /><Select label="Inherent impact" value={assessment.inherentImpact} options={scores} onChange={event => setAssessment(current => ({ ...current, inherentImpact: event.target.value }))} /><Select label="Residual likelihood" value={assessment.residualLikelihood} options={scores} onChange={event => setAssessment(current => ({ ...current, residualLikelihood: event.target.value }))} /><Select label="Residual impact" value={assessment.residualImpact} options={scores} onChange={event => setAssessment(current => ({ ...current, residualImpact: event.target.value }))} /><div className="md:col-span-2"><Textarea label="Control effectiveness and assessment" value={assessment.controls} onChange={event => setAssessment(current => ({ ...current, controls: event.target.value }))} /></div><div className="md:col-span-2"><Textarea label="Governance reason" value={assessment.reason} onChange={event => setAssessment(current => ({ ...current, reason: event.target.value }))} required /></div></div><Button className="mt-3" disabled={busy} onClick={() => void submitAssessment()}>Record assessment</Button></Card>}
      <Card>{searchPager('assessments')}<div className="mt-3 space-y-2">{assessments.map(item => <div key={item.publicId} className="rounded border p-3"><div className="flex justify-between gap-3"><div><p className="font-medium">{item.riskReference ? `${item.riskReference} · ` : ''}{item.riskTitle}</p><p className="text-xs text-secondary-500">{new Date(item.assessedAt).toLocaleDateString()} · {item.assessedByName}</p></div><div className="flex gap-2"><Badge variant="warning">Inherent {item.inherentScore}</Badge><Badge variant="info">Residual {item.residualScore}</Badge></div></div>{item.controlAssessment && <p className="mt-2 text-sm">{item.controlAssessment}</p>}</div>)}{!assessments.length && <EmptyState title="No risk assessments" description="No assessment matches the current filter." />}</div></Card>
    </>}

    {view === 'treatments' && <>
      {canTreat && <Card><h2 className="font-semibold">{treatment.publicId ? 'Update treatment action' : 'Create treatment action'}</h2><div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-4"><Select label="Strategic risk" value={treatment.risk} options={riskOptions} disabled={Boolean(treatment.publicId)} onChange={event => setTreatment(current => ({ ...current, risk: event.target.value }))} /><Select label="Accountable owner" value={treatment.owner} options={ownerOptions} onChange={event => setTreatment(current => ({ ...current, owner: event.target.value }))} /><Input label="Action title" value={treatment.title} onChange={event => setTreatment(current => ({ ...current, title: event.target.value }))} required /><Input label="Due date" type="date" value={treatment.dueAt} onChange={event => setTreatment(current => ({ ...current, dueAt: event.target.value }))} /><Select label="Status" value={treatment.status} options={[{ value: 'OPEN', label: 'Open' }, { value: 'IN_PROGRESS', label: 'In progress' }, { value: 'COMPLETED', label: 'Completed' }, { value: 'CANCELLED', label: 'Cancelled' }]} onChange={event => setTreatment(current => ({ ...current, status: event.target.value, progress: event.target.value === 'COMPLETED' ? '100' : current.progress }))} /><Input label="Progress percent" type="number" min={0} max={100} value={treatment.progress} onChange={event => setTreatment(current => ({ ...current, progress: event.target.value }))} /><div className="md:col-span-2"><Textarea label="Mitigation action detail" value={treatment.description} onChange={event => setTreatment(current => ({ ...current, description: event.target.value }))} /></div><div className="md:col-span-2"><Textarea label="Governance reason" value={treatment.reason} onChange={event => setTreatment(current => ({ ...current, reason: event.target.value }))} required /></div></div><div className="mt-3 flex gap-2"><Button disabled={busy} onClick={() => void submitTreatment()}>Save treatment</Button>{treatment.publicId && <Button variant="outline" onClick={() => setTreatment({ publicId: '', risk: '', title: '', description: '', owner: '', dueAt: futureDate(30), status: 'OPEN', progress: '0', reason: '', rowVersion: '' })}>Cancel edit</Button>}</div></Card>}
      <Card>{searchPager('treatments')}<div className="mt-3 space-y-2">{treatments.map(item => <div key={item.publicId} className="rounded border p-3"><div className="flex justify-between gap-3"><div><p className="font-medium">{item.title}</p><p className="text-sm">{item.riskReference ? `${item.riskReference} · ` : ''}{item.riskTitle}</p><p className="text-xs text-secondary-500">{item.ownerName ? `Owner: ${item.ownerName} · ` : ''}Due {new Date(item.dueAt).toLocaleDateString()}</p></div><div className="flex items-center gap-2"><Badge variant={item.status === 'COMPLETED' ? 'success' : item.status === 'CANCELLED' ? 'default' : 'warning'}>{item.status.replace('_', ' ')}</Badge><Badge variant="info">{item.progressPercent}%</Badge></div></div>{item.description && <p className="mt-2 text-sm">{item.description}</p>}{canTreat && item.ownerUserPublicId && item.status !== 'COMPLETED' && item.status !== 'CANCELLED' && <Button className="mt-2" size="sm" variant="outline" onClick={() => setTreatment({ publicId: item.publicId, risk: item.strategicRiskPublicId, title: item.title, description: item.description ?? '', owner: item.ownerUserPublicId ?? '', dueAt: item.dueAt.slice(0, 10), status: item.status, progress: String(item.progressPercent), reason: '', rowVersion: item.rowVersion })}>Update</Button>}</div>)}{!treatments.length && <EmptyState title="No treatment actions" description="No treatment matches the current filter." />}</div></Card>
    </>}

    {view === 'reviews' && <>
      {canReview && <Card><h2 className="font-semibold">Record periodic review</h2><div className="mt-3 grid gap-3 md:grid-cols-2"><Select label="Strategic risk" value={review.risk} options={riskOptions} onChange={event => setReview(current => ({ ...current, risk: event.target.value }))} /><Input label="Review date" type="date" value={review.reviewedAt} onChange={event => setReview(current => ({ ...current, reviewedAt: event.target.value }))} /><Input label="Next review date" type="date" value={review.nextReviewAt} onChange={event => setReview(current => ({ ...current, nextReviewAt: event.target.value }))} /><Textarea label="Review outcome" value={review.outcome} onChange={event => setReview(current => ({ ...current, outcome: event.target.value }))} required /><div className="md:col-span-2"><Textarea label="Governance reason" value={review.reason} onChange={event => setReview(current => ({ ...current, reason: event.target.value }))} required /></div></div><Button className="mt-3" disabled={busy} onClick={() => void submitReview()}>Record review</Button></Card>}
      <Card>{searchPager('reviews')}<div className="mt-3 space-y-2">{reviews.map(item => <div key={item.publicId} className="rounded border p-3"><p className="font-medium">{item.riskReference ? `${item.riskReference} · ` : ''}{item.riskTitle}</p><p className="text-xs text-secondary-500">Reviewed {new Date(item.reviewedAt).toLocaleDateString()} by {item.reviewedByName}{item.nextReviewAt ? ` · next ${new Date(item.nextReviewAt).toLocaleDateString()}` : ''}</p>{item.outcome && <p className="mt-2 text-sm">{item.outcome}</p>}</div>)}{!reviews.length && <EmptyState title="No risk reviews" description="No review matches the current filter." />}</div></Card>
    </>}

    {view === 'reports' && <><Card><h2 className="font-semibold">Risk-control report</h2><p className="text-sm text-secondary-500">Live tenant-scoped evidence from the risk register, latest assessments, accountable actions and periodic reviews.</p></Card>{heatmapPanel}<div className="grid gap-5 xl:grid-cols-3"><Card><h3 className="font-semibold">Latest assessments</h3>{assessments.slice(0, 10).map(item => <p key={item.publicId} className="border-b py-2 text-sm">{item.riskTitle} · residual {item.residualScore}</p>)}</Card><Card><h3 className="font-semibold">Treatment actions</h3>{treatments.slice(0, 10).map(item => <p key={item.publicId} className="border-b py-2 text-sm">{item.title} · {item.status.replace('_', ' ')} · {item.progressPercent}%</p>)}</Card><Card><h3 className="font-semibold">Reviews</h3>{reviews.slice(0, 10).map(item => <p key={item.publicId} className="border-b py-2 text-sm">{item.riskTitle} · {new Date(item.reviewedAt).toLocaleDateString()}</p>)}</Card></div></>}
  </div>;
}
