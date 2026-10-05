import { useCallback, useEffect, useState } from 'react';
import { AlertTriangle, Link2, Pencil, Plus, RefreshCw, ShieldCheck, Unlink } from 'lucide-react';
import {
  getMunicipalityFinancialYearMastersPage,
  getStrategicRiskLinksPage,
  getStrategicRisksPage,
  getStrategicRiskSummary,
  linkStrategicRisk,
  saveStrategicRisk,
  unlinkStrategicRisk,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { MunicipalityFinancialYearMasterDto, StrategicRiskDto, StrategicRiskKpiLinkDto, StrategicRiskSummaryDto } from '../../types';
import { FormPanel, Input, Select, Textarea } from '../common/Form';
import { TargetPicker } from '../common/TargetPicker';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState, StatCard } from '../ui';

type RiskView = 'dashboard' | 'register' | 'assessments' | 'treatments' | 'reviews' | 'heatmap' | 'reports';
const viewCopy: Record<RiskView, { title: string; subtitle: string }> = {
  dashboard: { title: 'Risk Dashboard', subtitle: 'Live municipality strategic-risk and KPI exposure summary' },
  register: { title: 'Strategic Risk Register', subtitle: 'Effective-dated, audited strategic risks and governed KPI relationships' },
  assessments: { title: 'Risk Assessments', subtitle: 'Current strategic-risk coverage and linked KPI exposure' },
  treatments: { title: 'Risk Treatment Coverage', subtitle: 'Review active strategic risks and KPIs affected by mitigation decisions' },
  reviews: { title: 'Risk Reviews', subtitle: 'Review the current effective-dated register and relationship history' },
  heatmap: { title: 'Risk Exposure', subtitle: 'Inspect current risk-to-KPI concentration using governed relationship data' },
  reports: { title: 'Risk Reports', subtitle: 'Live strategic-risk register and KPI relationship evidence' },
};
const emptyRisk = () => ({ reference: '', title: '', description: '', fromYear: '', toYear: '', active: 'true', reason: '' });

function RiskWorkspace({ view }: { view: RiskView }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [summary, setSummary] = useState<StrategicRiskSummaryDto | null>(null);
  const [risks, setRisks] = useState<StrategicRiskDto[]>([]);
  const [links, setLinks] = useState<StrategicRiskKpiLinkDto[]>([]);
  const [years, setYears] = useState<MunicipalityFinancialYearMasterDto[]>([]);
  const [selected, setSelected] = useState<StrategicRiskDto | null>(null);
  const [riskForm, setRiskForm] = useState(emptyRisk);
  const [targetPublicId, setTargetPublicId] = useState('');
  const [linkRiskPublicId, setLinkRiskPublicId] = useState('');
  const [primary, setPrimary] = useState(false);
  const [linkReason, setLinkReason] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('active');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!security.canRead('STRATEGIC_RISK')) return;
    setBusy(true); setError(null);
    const active = status === 'all' ? undefined : status === 'active';
    const [summaryResult, riskResult, linkResult, yearResult] = await Promise.all([
      getStrategicRiskSummary(),
      getStrategicRisksPage({ page, pageSize: 25, search, sortBy: 'reference', sortDirection: 'asc' }, { active }),
      getStrategicRiskLinksPage({ page: 1, pageSize: 25, search, sortBy: 'linkedAt', sortDirection: 'desc' }, { includeInactive: view === 'reviews' }),
      getMunicipalityFinancialYearMastersPage({ page: 1, pageSize: 100, sortBy: 'startDate', sortDirection: 'desc', active: true }),
    ]);
    if (!summaryResult.success || !riskResult.success || !linkResult.success || !yearResult.success) {
      setError(summaryResult.message ?? riskResult.message ?? linkResult.message ?? yearResult.message ?? 'Strategic risk data could not be loaded.');
    }
    setSummary(summaryResult.data ?? null);
    setRisks(riskResult.data?.items ?? []);
    setTotalCount(riskResult.data?.totalCount ?? 0);
    setTotalPages(riskResult.data?.totalPages ?? 0);
    setLinks(linkResult.data?.items ?? []);
    setYears(yearResult.data?.items ?? []);
    setBusy(false);
  }, [page, search, security, status, view]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    const timer = window.setTimeout(() => { setSearch(searchInput.trim()); setPage(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  const clearRisk = () => { setSelected(null); setRiskForm(emptyRisk()); };
  const editRisk = (risk: StrategicRiskDto) => {
    setSelected(risk);
    setRiskForm({
      reference: risk.riskReference ?? '', title: risk.riskTitle, description: risk.riskDescription ?? '',
      fromYear: risk.effectiveFromMunicipalityFinancialYearPublicId ?? '',
      toYear: risk.effectiveToMunicipalityFinancialYearPublicId ?? '', active: String(risk.isActive), reason: '',
    });
  };
  const saveRisk = async () => {
    if (!riskForm.title.trim() || !riskForm.reason.trim()) return setError('Risk title and governance reason are required.');
    setBusy(true); setError(null);
    const result = await saveStrategicRisk(selected?.publicId ?? null, {
      riskReference: riskForm.reference.trim() || null,
      riskTitle: riskForm.title.trim(), riskDescription: riskForm.description.trim() || null,
      effectiveFromMunicipalityFinancialYearPublicId: riskForm.fromYear || null,
      effectiveToMunicipalityFinancialYearPublicId: riskForm.toYear || null,
      isActive: riskForm.active === 'true', reason: riskForm.reason.trim(), rowVersion: selected?.rowVersion ?? null,
    });
    if (!result.success) setError(result.message ?? 'Strategic risk could not be saved.');
    else { pushToast('success', 'Strategic risk saved with audit history.'); clearRisk(); await load(); }
    setBusy(false);
  };
  const saveLink = async () => {
    if (!linkRiskPublicId || !targetPublicId || !linkReason.trim()) return setError('Risk, KPI and link reason are required.');
    setBusy(true); setError(null);
    const result = await linkStrategicRisk({ strategicRiskPublicId: linkRiskPublicId, targetPublicId, isPrimary: primary, reason: linkReason.trim() });
    if (!result.success) setError(result.message ?? 'Risk could not be linked to the KPI.');
    else {
      pushToast('success', 'Strategic risk linked to KPI.');
      setTargetPublicId(''); setLinkRiskPublicId(''); setPrimary(false); setLinkReason(''); await load();
    }
    setBusy(false);
  };
  const unlink = async (link: StrategicRiskKpiLinkDto) => {
    const reason = window.prompt('Reason for unlinking this strategic risk from the KPI:')?.trim();
    if (!reason) return;
    setBusy(true); setError(null);
    const result = await unlinkStrategicRisk(link.publicId, { reason, rowVersion: link.rowVersion });
    if (!result.success) setError(result.message ?? 'Risk link could not be closed.');
    else { pushToast('success', 'Risk link closed; history was retained.'); await load(); }
    setBusy(false);
  };

  const copy = viewCopy[view];
  const yearOptions = [{ value: '', label: 'Open-ended' }, ...years.map(year => ({ value: year.publicId, label: `${year.code}${year.isCurrent ? ' · current' : ''}` }))];
  const canEditRegister = view === 'register';
  if (!security.canRead('STRATEGIC_RISK')) return <AppShell title={copy.title} subtitle={copy.subtitle}><Card><EmptyState title="Access unavailable" description="Your current role does not grant strategic-risk read access." /></Card></AppShell>;
  return <AppShell title={copy.title} subtitle={copy.subtitle}>
    <div className="space-y-5">
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard title="All strategic risks" value={summary?.totalRisks ?? 0} icon={<AlertTriangle className="h-4 w-4 text-primary-600" />} />
        <StatCard title="Active risks" value={summary?.activeRisks ?? 0} />
        <StatCard title="Linked risks" value={summary?.linkedRisks ?? 0} />
        <StatCard title="Unlinked active risks" value={summary?.unlinkedActiveRisks ?? 0} />
        <StatCard title="Exposed KPIs" value={summary?.linkedKpis ?? 0} icon={<Link2 className="h-4 w-4 text-primary-600" />} />
      </div>
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-64 flex-1"><Input label="Search strategic risks" value={searchInput} onChange={event => setSearchInput(event.target.value)} /></div>
        <div className="min-w-44"><Select label="Status" value={status} options={[{ value: 'active', label: 'Active' }, { value: 'inactive', label: 'Inactive' }, { value: 'all', label: 'All statuses' }]} onChange={event => { setStatus(event.target.value); setPage(1); }} /></div>
        <Button variant="ghost" icon={<RefreshCw className="h-4 w-4" />} disabled={busy} onClick={() => void load()}>Refresh</Button>
      </div>
      <div className={`grid gap-5 ${canEditRegister ? 'xl:grid-cols-[0.85fr_1.15fr]' : ''}`}>
        {canEditRegister && (selected ? security.canUpdate('STRATEGIC_RISK') : security.canCreate('STRATEGIC_RISK')) && <FormPanel title={selected ? 'Edit strategic risk' : 'Create strategic risk'} description="Changes require a reason, retain history and use optimistic concurrency." icon={<ShieldCheck className="h-5 w-5" />}>
          <Input label="Risk reference" value={riskForm.reference} maxLength={100} onChange={event => setRiskForm(current => ({ ...current, reference: event.target.value }))} />
          <Input label="Risk title" value={riskForm.title} maxLength={500} required onChange={event => setRiskForm(current => ({ ...current, title: event.target.value }))} />
          <Textarea label="Description" value={riskForm.description} maxLength={2000} rows={4} onChange={event => setRiskForm(current => ({ ...current, description: event.target.value }))} />
          <div className="grid grid-cols-2 gap-2"><Select label="Effective from" value={riskForm.fromYear} options={yearOptions} onChange={event => setRiskForm(current => ({ ...current, fromYear: event.target.value }))} /><Select label="Effective to" value={riskForm.toYear} options={yearOptions} onChange={event => setRiskForm(current => ({ ...current, toYear: event.target.value }))} /></div>
          <Select label="Status" value={riskForm.active} options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]} onChange={event => setRiskForm(current => ({ ...current, active: event.target.value }))} />
          <Textarea label="Governance reason" value={riskForm.reason} rows={3} required onChange={event => setRiskForm(current => ({ ...current, reason: event.target.value }))} />
          <div className="flex gap-2"><Button icon={selected ? <Pencil className="h-4 w-4" /> : <Plus className="h-4 w-4" />} disabled={busy} onClick={() => void saveRisk()}>{selected ? 'Save changes' : 'Create risk'}</Button>{selected && <Button variant="outline" onClick={clearRisk}>Cancel</Button>}</div>
        </FormPanel>}
        <Card>
          <div className="flex items-center justify-between"><h2 className="font-semibold">Strategic risks</h2><Badge variant="primary">{totalCount}</Badge></div>
          <div className="mt-3 space-y-2">
            {risks.map(risk => <button type="button" key={risk.publicId} disabled={!canEditRegister} onClick={() => editRisk(risk)} className="flex w-full items-start justify-between gap-3 rounded-lg border border-secondary-200 p-3 text-left disabled:cursor-default dark:border-secondary-700">
              <div><p className="font-medium">{risk.riskReference ? `${risk.riskReference} · ` : ''}{risk.riskTitle}</p><p className="mt-1 text-xs text-secondary-500">{risk.effectiveFromFinancialYear ?? 'Open'} — {risk.effectiveToFinancialYear ?? 'open-ended'} · {risk.activeKpiLinks} active KPI link{risk.activeKpiLinks === 1 ? '' : 's'}</p>{risk.riskDescription && <p className="mt-1 text-sm text-secondary-600 dark:text-secondary-300">{risk.riskDescription}</p>}</div>
              <Badge variant={risk.isActive ? 'success' : 'default'}>{risk.isActive ? 'Active' : 'Inactive'}</Badge>
            </button>)}
            {!risks.length && <EmptyState title="No strategic risks" description="No risk is visible in the selected municipality and filter." />}
          </div>
          {totalPages > 1 && <div className="mt-4 flex items-center justify-between"><span className="text-xs text-secondary-500">Page {page} of {totalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1 || busy} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= totalPages || busy} onClick={() => setPage(value => value + 1)}>Next</Button></div></div>}
        </Card>
      </div>
      {canEditRegister && security.canExecute('STRATEGIC_RISK.LINK_KPI') && <Card>
        <h2 className="font-semibold">Link a strategic risk to an OPMS KPI</h2>
        <p className="mt-1 text-xs text-secondary-500">The API rechecks both the action permission and record-level KPI update scope.</p>
        <div className="mt-3 grid gap-3 lg:grid-cols-2">
          <Select label="Strategic risk" value={linkRiskPublicId} options={[{ value: '', label: 'Select active risk' }, ...risks.filter(risk => risk.isActive).map(risk => ({ value: risk.publicId, label: `${risk.riskReference ? `${risk.riskReference} · ` : ''}${risk.riskTitle}` }))]} onChange={event => setLinkRiskPublicId(event.target.value)} />
          <TargetPicker kind="opms" label="OPMS KPI" value={targetPublicId} valueField="publicId" onChange={setTargetPublicId} required />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={primary} onChange={event => setPrimary(event.target.checked)} /> Primary strategic risk for this KPI</label>
          <Textarea label="Link reason" value={linkReason} rows={2} required onChange={event => setLinkReason(event.target.value)} />
        </div>
        <Button className="mt-3" icon={<Link2 className="h-4 w-4" />} disabled={busy} onClick={() => void saveLink()}>Link risk</Button>
      </Card>}
      <Card>
        <div className="flex items-center justify-between"><h2 className="font-semibold">KPI relationship {view === 'reviews' ? 'history' : 'register'}</h2><Badge variant="info">{links.length} shown</Badge></div>
        <div className="mt-3 space-y-2">
          {links.map(link => <div key={link.publicId} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div><p className="font-medium">{link.riskReference ? `${link.riskReference} · ` : ''}{link.riskTitle}</p><p className="text-sm text-secondary-600 dark:text-secondary-300">{link.indicatorNumber} · {link.targetName}</p><p className="text-xs text-secondary-500">{link.departmentName ?? 'No department'}{link.unitName ? ` / ${link.unitName}` : ''} · linked {new Date(link.linkedAt).toLocaleDateString()}</p></div><div className="flex items-center gap-2">{link.isPrimary && <Badge variant="primary">Primary</Badge>}<Badge variant={link.isActive ? 'success' : 'default'}>{link.isActive ? 'Active' : 'Unlinked'}</Badge>{canEditRegister && link.isActive && security.canExecute('STRATEGIC_RISK.UNLINK_KPI') && <Button size="sm" variant="outline" icon={<Unlink className="h-3.5 w-3.5" />} disabled={busy} onClick={() => void unlink(link)}>Unlink</Button>}</div></div>)}
          {!links.length && <EmptyState title="No authorised KPI relationships" description="No strategic-risk link falls within your current KPI read scope." />}
        </div>
      </Card>
    </div>
  </AppShell>;
}

export function RiskDashboardPage() { return <RiskWorkspace view="dashboard" />; }
export function RiskRegisterPage() { return <RiskWorkspace view="register" />; }
export function RiskAssessmentsPage() { return <RiskWorkspace view="assessments" />; }
export function RiskTreatmentPlansPage() { return <RiskWorkspace view="treatments" />; }
export function RiskReviewsPage() { return <RiskWorkspace view="reviews" />; }
export function RiskHeatmapPage() { return <RiskWorkspace view="heatmap" />; }
export function RiskReportsPage() { return <RiskWorkspace view="reports" />; }
