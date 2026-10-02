import { useEffect, useMemo, useState } from 'react';
import { BarChart3, FileText, Layers, Map, RefreshCcw, Users } from 'lucide-react';
import {
  createIdpComment,
  createIdpPlan,
  createIdpPlanVersion,
  commitIdpImport,
  getIdpImportBatches,
  getIdpAlignmentMatrix,
  getIdpDashboard,
  getIdpPlans,
  getIdpPlanHierarchy,
  getIdpReport,
  stageIdpKpiImport,
  createIdpCommunitySession,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import { useHasAnyPermission } from '../security/AccessControl';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';
import type {
  IdpAlignmentMatrixItem,
  IdpDashboard,
  IdpHierarchy,
  IdpImportBatch,
  IdpKpiImportRowPayload,
  IdpPlanSummary,
  IdpPlanVersion,
  IdpReportDocument,
} from '../../types';
import { idpKpiCsvTemplate, parseIdpKpiCsv } from './idpImportCsv';

function metricCard(title: string, value: string | number, caption?: string) {
  return (
    <Card key={title}>
      <p className="text-xs uppercase tracking-wide text-secondary-500 dark:text-secondary-400">{title}</p>
      <p className="mt-2 text-2xl font-bold text-secondary-900 dark:text-white">{value}</p>
      {caption ? <p className="mt-1 text-xs text-secondary-500 dark:text-secondary-400">{caption}</p> : null}
    </Card>
  );
}

export function IdpPlanningDashboardPage() {
  const { setCurrentPath } = useApp();
  const canManagePlan = useHasAnyPermission(['IDP.Plan.Manage', 'IDP.Version.Manage']);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setBusy(true);
    try {
      const plansResult = await getIdpPlans();
      const loadedPlans = plansResult.data ?? [];
      setPlans(loadedPlans);
      const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
      setSelectedPlanId(planId);

      if (planId) {
        const dashboardResult = await getIdpDashboard(planId);
        setDashboard(dashboardResult.data ?? null);
      } else {
        setDashboard(null);
      }
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="IDP Dashboard" subtitle="Executive strategic planning and implementation view">
      <div className="space-y-5">
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" icon={<RefreshCcw className="h-4 w-4" />} onClick={() => void load()}>
            Refresh
          </Button>
          {!canManagePlan ? <Badge variant="warning">Read Only</Badge> : null}
          {canManagePlan ? (
            <Button
              variant="primary"
              onClick={() => setCurrentPath('/idp/plans')}
            >
              Manage Plans and Versions
            </Button>
          ) : null}
          <select
            value={selectedPlanId ?? ''}
            onChange={(event) => {
              const value = Number(event.target.value);
              setSelectedPlanId(Number.isNaN(value) ? null : value);
            }}
            className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
          >
            {plans.map(plan => (
              <option key={plan.id} value={plan.id}>{plan.planCode} - {plan.planTitle}</option>
            ))}
          </select>
        </div>

        {busy ? <Card><p className="text-sm text-secondary-500">Loading IDP dashboard...</p></Card> : null}

        {!busy && !dashboard ? (
          <EmptyState
            icon={<Map className="h-6 w-6" />}
            title="No IDP dashboard data"
            description="Create an IDP plan to unlock strategic progress, participation, and budget analytics."
          />
        ) : null}

        {dashboard ? (
          <>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              {metricCard('Strategic Outcomes', dashboard.outcomes)}
              {metricCard('Strategic Objectives', dashboard.objectives)}
              {metricCard('Projects', dashboard.projects)}
              {metricCard('KPIs', dashboard.kpis)}
              {metricCard('Community Sessions', dashboard.communitySessions)}
              {metricCard('Risk Items', dashboard.risks)}
              {metricCard('KPI Achievement', `${dashboard.kpiAchievementRate.toFixed(2)}%`)}
              {metricCard('Budget Utilization', `${dashboard.approvedBudget > 0 ? ((dashboard.actualExpenditure / dashboard.approvedBudget) * 100).toFixed(2) : '0.00'}%`)}
            </div>

            <div className="grid gap-4 lg:grid-cols-3">
              <Card>
                <div className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Budget Integration</h3>
                </div>
                <div className="mt-4 space-y-2 text-sm text-secondary-700 dark:text-secondary-300">
                  <p>Planned Budget: <span className="font-semibold">R {dashboard.plannedBudget.toLocaleString()}</span></p>
                  <p>Approved Budget: <span className="font-semibold">R {dashboard.approvedBudget.toLocaleString()}</span></p>
                  <p>Actual Expenditure: <span className="font-semibold">R {dashboard.actualExpenditure.toLocaleString()}</span></p>
                </div>
              </Card>

              <Card>
                <div className="flex items-center gap-2">
                  <Layers className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Top Strategic Risks</h3>
                </div>
                <div className="mt-3 flex flex-wrap gap-2">
                  {dashboard.topRiskTitles.length ? dashboard.topRiskTitles.map(risk => <Badge key={risk} variant="warning">{risk}</Badge>) : <p className="text-sm text-secondary-500">No risks linked.</p>}
                </div>
              </Card>

              <Card>
                <div className="flex items-center gap-2">
                  <Users className="h-5 w-5 text-primary-600" />
                  <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Ward Participation</h3>
                </div>
                <div className="mt-3 space-y-2">
                  {dashboard.wardParticipation.slice(0, 5).map(ward => (
                    <div key={ward.wardId} className="rounded border border-secondary-200 px-2 py-1 text-sm dark:border-secondary-700">
                      <p className="font-medium text-secondary-900 dark:text-secondary-100">{ward.wardName}</p>
                      <p className="text-xs text-secondary-500">Meetings: {ward.meetingCount} | Participants: {ward.participantsCount} | Needs: {ward.needsCaptured}</p>
                    </div>
                  ))}
                </div>
              </Card>
            </div>
          </>
        ) : null}
      </div>
    </AppShell>
  );
}

export function IdpPlanManagementPage() {
  const { pushToast } = useApp();
  const { canImport } = useSecurity();
  const canManagePlan = useHasAnyPermission(['IDP.Plan.Manage', 'IDP.Version.Manage']);
  const canImportKpis = canImport('IDP_INDICATOR');
  const currentYear = new Date().getFullYear();
  const today = new Date().toISOString().slice(0, 10);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [versions, setVersions] = useState<IdpPlanVersion[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [importRows, setImportRows] = useState<IdpKpiImportRowPayload[]>([]);
  const [importFileName, setImportFileName] = useState('');
  const [importBatch, setImportBatch] = useState<IdpImportBatch | null>(null);
  const [importHistory, setImportHistory] = useState<IdpImportBatch[]>([]);
  const [importReason, setImportReason] = useState('');
  const [importBusy, setImportBusy] = useState(false);
  const [planDraft, setPlanDraft] = useState({
    planTitle: '',
    planCode: '',
    startFinancialYear: currentYear,
    endFinancialYear: currentYear + 5,
    predecessorPlanPublicId: '',
    effectiveFrom: today,
    effectiveTo: '',
    publicationReference: '',
  });
  const [versionDraft, setVersionDraft] = useState({
    versionType: 'AnnualReview',
    versionLabel: '',
    reviewYear: `${currentYear}/${currentYear + 1}`,
    summaryOfChanges: '',
    effectiveFrom: today,
    publicationReference: '',
  });

  const fieldClass = 'w-full rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-800 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-100';

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const hierarchyResult = await getIdpPlanHierarchy(planId);
      const hierarchy = hierarchyResult.data;
      setVersions(hierarchy?.versions ?? []);
      const selected = loadedPlans.find(plan => plan.id === planId);
      if (selected && canImportKpis) {
        const historyResult = await getIdpImportBatches(selected.publicId);
        setImportHistory(historyResult.data ?? []);
      }
    } else {
      setVersions([]);
      setImportHistory([]);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectPlan = async (planId: number) => {
    setSelectedPlanId(planId);
    const hierarchyResult = await getIdpPlanHierarchy(planId);
    setVersions(hierarchyResult.data?.versions ?? []);
    const selected = plans.find(plan => plan.id === planId);
    if (selected && canImportKpis) {
      const historyResult = await getIdpImportBatches(selected.publicId);
      setImportHistory(historyResult.data ?? []);
    }
    setImportBatch(null);
  };

  const selectedPlan = plans.find(plan => plan.id === selectedPlanId) ?? null;

  const stageImport = async () => {
    if (!selectedPlan || !importRows.length || !importFileName) {
      pushToast('error', 'Select a plan and a valid KPI CSV file first.');
      return;
    }
    setImportBusy(true);
    try {
      const result = await stageIdpKpiImport(selectedPlan.publicId, {
        clientRequestId: crypto.randomUUID(),
        sourceFileName: importFileName,
        rows: importRows,
      });
      if (!result.success || !result.data) {
        pushToast('error', result.message ?? 'Unable to stage the KPI import.');
        return;
      }
      setImportBatch(result.data);
      setImportHistory(history => [result.data!, ...history.filter(item => item.publicId !== result.data!.publicId)]);
      pushToast(result.data.invalidRows ? 'info' : 'success', result.data.invalidRows ? 'Reconciliation contains invalid rows.' : 'Reconciliation preview is ready.');
    } finally {
      setImportBusy(false);
    }
  };

  const commitImport = async () => {
    if (!importBatch || !importReason.trim()) {
      pushToast('error', 'A commit reason is required.');
      return;
    }
    setImportBusy(true);
    try {
      const result = await commitIdpImport(importBatch.publicId, { rowVersion: importBatch.rowVersion, reason: importReason.trim() });
      if (!result.success || !result.data) {
        pushToast('error', result.message ?? 'Unable to commit the KPI import.');
        return;
      }
      setImportBatch(result.data);
      setImportHistory(history => history.map(item => item.publicId === result.data!.publicId ? result.data! : item));
      setImportReason('');
      pushToast('success', 'KPI import committed atomically.');
    } finally {
      setImportBusy(false);
    }
  };

  const downloadImportTemplate = () => {
    const url = URL.createObjectURL(new Blob([idpKpiCsvTemplate], { type: 'text/csv;charset=utf-8' }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'idp-kpi-import-template.csv';
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const submitPlan = async () => {
    if (!planDraft.planTitle.trim() || !planDraft.planCode.trim() || !planDraft.effectiveFrom) {
      pushToast('error', 'Plan title, code, and effective date are required.');
      return;
    }

    const result = await createIdpPlan({
      municipalityName: '',
      planTitle: planDraft.planTitle.trim(),
      planCode: planDraft.planCode.trim(),
      startFinancialYear: planDraft.startFinancialYear,
      endFinancialYear: planDraft.endFinancialYear,
      predecessorPlanPublicId: planDraft.predecessorPlanPublicId || null,
      effectiveFrom: `${planDraft.effectiveFrom}T00:00:00.000Z`,
      effectiveTo: planDraft.effectiveTo ? `${planDraft.effectiveTo}T00:00:00.000Z` : null,
      publicationReference: planDraft.publicationReference.trim() || null,
    });

    if (result.success) {
      pushToast('success', 'IDP plan created with governed lineage.');
      setPlanDraft(draft => ({ ...draft, planTitle: '', planCode: '', predecessorPlanPublicId: '', publicationReference: '' }));
      await load();
    } else {
      pushToast('error', result.message ?? 'Failed to create plan.');
    }
  };

  const submitVersion = async () => {
    if (!selectedPlanId || !versionDraft.versionLabel.trim() || !versionDraft.effectiveFrom) {
      pushToast('error', 'Select a plan and provide a version label and effective date.');
      return;
    }

    const result = await createIdpPlanVersion(selectedPlanId, {
      versionType: versionDraft.versionType,
      versionLabel: versionDraft.versionLabel.trim(),
      reviewYear: versionDraft.reviewYear.trim() || null,
      summaryOfChanges: versionDraft.summaryOfChanges.trim() || null,
      effectiveFrom: `${versionDraft.effectiveFrom}T00:00:00.000Z`,
      publicationReference: versionDraft.publicationReference.trim() || null,
    });

    if (result.success) {
      pushToast('success', 'IDP version created and linked to its predecessor.');
      setVersionDraft(draft => ({ ...draft, versionLabel: '', summaryOfChanges: '', publicationReference: '' }));
      await selectPlan(selectedPlanId);
    } else {
      pushToast('error', result.message ?? 'Failed to create version.');
    }
  };

  return (
    <AppShell title="IDP Plans" subtitle="Five-year plan lifecycle, annual reviews, and version governance">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Create governed plan</h3>
              <p className="text-xs text-secondary-500">Capture the approved identity, effective period, publication reference, and optional predecessor.</p>
            </div>
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            {!canManagePlan ? <Badge variant="warning">Read Only</Badge> : null}
          </div>
          {canManagePlan ? (
            <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
              <label className="text-xs text-secondary-600">Plan title<input aria-label="Plan title" className={fieldClass} value={planDraft.planTitle} onChange={event => setPlanDraft({ ...planDraft, planTitle: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Plan code<input aria-label="Plan code" className={fieldClass} value={planDraft.planCode} onChange={event => setPlanDraft({ ...planDraft, planCode: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Start financial year<input aria-label="Start financial year" type="number" className={fieldClass} value={planDraft.startFinancialYear} onChange={event => setPlanDraft({ ...planDraft, startFinancialYear: Number(event.target.value) })} /></label>
              <label className="text-xs text-secondary-600">End financial year<input aria-label="End financial year" type="number" className={fieldClass} value={planDraft.endFinancialYear} onChange={event => setPlanDraft({ ...planDraft, endFinancialYear: Number(event.target.value) })} /></label>
              <label className="text-xs text-secondary-600">Predecessor plan<select aria-label="Predecessor plan" className={fieldClass} value={planDraft.predecessorPlanPublicId} onChange={event => setPlanDraft({ ...planDraft, predecessorPlanPublicId: event.target.value })}><option value="">New plan family</option>{plans.map(plan => <option key={plan.publicId} value={plan.publicId}>{plan.planCode} - {plan.planTitle}</option>)}</select></label>
              <label className="text-xs text-secondary-600">Effective from<input aria-label="Plan effective from" type="date" className={fieldClass} value={planDraft.effectiveFrom} onChange={event => setPlanDraft({ ...planDraft, effectiveFrom: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Effective to<input aria-label="Plan effective to" type="date" className={fieldClass} value={planDraft.effectiveTo} onChange={event => setPlanDraft({ ...planDraft, effectiveTo: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Publication reference<input aria-label="Plan publication reference" className={fieldClass} value={planDraft.publicationReference} onChange={event => setPlanDraft({ ...planDraft, publicationReference: event.target.value })} /></label>
              <div className="md:col-span-2 xl:col-span-4"><Button variant="primary" onClick={() => void submitPlan()}>Create Plan</Button></div>
            </div>
          ) : null}
        </Card>

        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Plan Register</h3>
            <div className="mt-3 space-y-2">
              {plans.map(plan => (
                <button
                  key={plan.id}
                  onClick={() => void selectPlan(plan.id)}
                  className={`w-full rounded border px-3 py-2 text-left ${selectedPlanId === plan.id ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-secondary-200 dark:border-secondary-700'}`}
                >
                  <p className="font-medium text-secondary-900 dark:text-secondary-100">{plan.planCode} - {plan.planTitle}</p>
                  <p className="text-xs text-secondary-500">{plan.startFinancialYear}/{plan.startFinancialYear + 1} to {plan.endFinancialYear}/{plan.endFinancialYear + 1} | Status: {plan.status}</p>
                  <p className="text-xs text-secondary-500">Family: {plan.planFamilyId} | Effective: {new Date(plan.effectiveFrom).toLocaleDateString()} | Publication: {plan.publicationReference ?? 'Not published'}</p>
                </button>
              ))}
            </div>
          </Card>

          <Card>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Version Control</h3>
            </div>
            {canManagePlan ? (
              <div className="mt-3 grid gap-3 md:grid-cols-2">
                <label className="text-xs text-secondary-600">Version type<select aria-label="Version type" className={fieldClass} value={versionDraft.versionType} onChange={event => setVersionDraft({ ...versionDraft, versionType: event.target.value })}><option value="AnnualReview">Annual review</option><option value="Revised">Revised</option><option value="Amended">Amended</option></select></label>
                <label className="text-xs text-secondary-600">Version label<input aria-label="Version label" className={fieldClass} value={versionDraft.versionLabel} onChange={event => setVersionDraft({ ...versionDraft, versionLabel: event.target.value })} /></label>
                <label className="text-xs text-secondary-600">Review year<input aria-label="Review year" className={fieldClass} value={versionDraft.reviewYear} onChange={event => setVersionDraft({ ...versionDraft, reviewYear: event.target.value })} /></label>
                <label className="text-xs text-secondary-600">Effective from<input aria-label="Version effective from" type="date" className={fieldClass} value={versionDraft.effectiveFrom} onChange={event => setVersionDraft({ ...versionDraft, effectiveFrom: event.target.value })} /></label>
                <label className="text-xs text-secondary-600">Publication reference<input aria-label="Version publication reference" className={fieldClass} value={versionDraft.publicationReference} onChange={event => setVersionDraft({ ...versionDraft, publicationReference: event.target.value })} /></label>
                <label className="text-xs text-secondary-600 md:col-span-2">Summary of changes<textarea aria-label="Summary of changes" className={fieldClass} rows={3} value={versionDraft.summaryOfChanges} onChange={event => setVersionDraft({ ...versionDraft, summaryOfChanges: event.target.value })} /></label>
                <div className="md:col-span-2"><Button variant="outline" disabled={!selectedPlanId} onClick={() => void submitVersion()}>Create Version</Button></div>
              </div>
            ) : null}
            <div className="mt-3 space-y-2">
              {versions.map(version => (
                <div key={version.id} className="rounded border border-secondary-200 px-3 py-2 text-sm dark:border-secondary-700">
                  <p className="font-medium text-secondary-900 dark:text-secondary-100">v{version.versionNumber} - {version.versionLabel}</p>
                  <p className="text-xs text-secondary-500">Type: {version.versionType} | Review Year: {version.reviewYear ?? 'N/A'} | Active: {version.isActive ? 'Yes' : 'No'}</p>
                  <p className="text-xs text-secondary-500">Effective: {new Date(version.effectiveFrom).toLocaleDateString()} | Predecessor: {version.predecessorVersionPublicId ?? 'Original'} | Publication: {version.publicationReference ?? 'Not published'}</p>
                </div>
              ))}
              {!versions.length ? <p className="text-sm text-secondary-500">No versions available.</p> : null}
            </div>
          </Card>
        </div>

        {canImportKpis ? (
          <Card>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">KPI import reconciliation</h3>
                <p className="text-xs text-secondary-500">Stage the complete CSV, review NEW/UNCHANGED/CHANGED/INVALID rows, then commit the valid batch atomically.</p>
              </div>
              <Button variant="outline" onClick={downloadImportTemplate}>Download CSV Template</Button>
            </div>

            <div className="mt-4 grid gap-3 md:grid-cols-[1fr_auto]">
              <label className="text-xs text-secondary-600">
                KPI CSV file
                <input
                  aria-label="KPI CSV file"
                  type="file"
                  accept=".csv,text/csv"
                  className={fieldClass}
                  onChange={event => {
                    const file = event.target.files?.[0];
                    if (!file) return;
                    void file.text().then(content => {
                      try {
                        const rows = parseIdpKpiCsv(content);
                        setImportRows(rows);
                        setImportFileName(file.name);
                        setImportBatch(null);
                        pushToast('success', `${file.name}: ${rows.length} rows parsed.`);
                      } catch (error) {
                        setImportRows([]);
                        setImportFileName('');
                        pushToast('error', error instanceof Error ? error.message : 'Unable to parse CSV.');
                      }
                    });
                  }}
                />
              </label>
              <div className="self-end"><Button variant="primary" disabled={importBusy || !selectedPlan || !importRows.length} onClick={() => void stageImport()}>{importBusy ? 'Working...' : 'Stage and Reconcile'}</Button></div>
            </div>
            {importFileName ? <p className="mt-2 text-xs text-secondary-500">Selected: {importFileName} ({importRows.length} data rows)</p> : null}

            {importBatch ? (
              <div className="mt-4 space-y-3">
                <div className="flex flex-wrap gap-2">
                  <Badge variant="primary">Total {importBatch.totalRows}</Badge>
                  <Badge variant="success">New {importBatch.newRows}</Badge>
                  <Badge variant="info">Unchanged {importBatch.unchangedRows}</Badge>
                  <Badge variant="warning">Changed {importBatch.changedRows}</Badge>
                  <Badge variant={importBatch.invalidRows ? 'error' : 'success'}>Invalid {importBatch.invalidRows}</Badge>
                  <Badge variant={importBatch.status === 'Committed' ? 'success' : 'default'}>{importBatch.status}</Badge>
                </div>
                <div className="max-h-80 overflow-auto rounded border border-secondary-200 dark:border-secondary-700">
                  <table className="min-w-full text-left text-xs">
                    <thead className="bg-secondary-50 dark:bg-secondary-800"><tr><th className="px-3 py-2">Row</th><th className="px-3 py-2">Reference</th><th className="px-3 py-2">Result</th><th className="px-3 py-2">Error</th></tr></thead>
                    <tbody>{importBatch.rows.map(row => <tr key={row.publicId} className="border-t border-secondary-200 dark:border-secondary-700"><td className="px-3 py-2">{row.sourceRowNumber}</td><td className="px-3 py-2">{row.reference}</td><td className="px-3 py-2"><Badge variant={row.status === 'Invalid' ? 'error' : row.status === 'Changed' ? 'warning' : row.status === 'New' ? 'success' : 'info'}>{row.status}</Badge></td><td className="px-3 py-2 text-error-700">{row.errorCode ? `${row.errorCode}: ${row.errorMessage}` : '—'}</td></tr>)}</tbody>
                  </table>
                </div>
                {importBatch.status === 'Staged' ? (
                  <div className="flex flex-wrap items-end gap-2">
                    <label className="min-w-72 flex-1 text-xs text-secondary-600">Commit reason<input aria-label="Import commit reason" className={fieldClass} value={importReason} onChange={event => setImportReason(event.target.value)} /></label>
                    <Button variant="primary" disabled={importBusy || importBatch.invalidRows > 0 || !importReason.trim()} onClick={() => void commitImport()}>Commit Valid Batch</Button>
                  </div>
                ) : null}
              </div>
            ) : null}

            {importHistory.length ? (
              <div className="mt-5">
                <h4 className="text-sm font-semibold text-secondary-800 dark:text-secondary-200">Recent import batches</h4>
                <div className="mt-2 flex flex-wrap gap-2">{importHistory.slice(0, 10).map(batch => <button key={batch.publicId} type="button" className="rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-700" onClick={() => setImportBatch(batch)}>{batch.sourceFileName} · {batch.status} · {new Date(batch.createdAt).toLocaleString()}</button>)}</div>
              </div>
            ) : null}
          </Card>
        ) : null}
      </div>
    </AppShell>
  );
}

export function IdpHierarchyPage() {
  const { pushToast } = useApp();
  const canManageHierarchy = useHasAnyPermission(['IDP.Hierarchy.Manage', 'IDP.Collaboration.Manage']);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [hierarchy, setHierarchy] = useState<IdpHierarchy | null>(null);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const hierarchyResult = await getIdpPlanHierarchy(planId);
      setHierarchy(hierarchyResult.data ?? null);
    } else {
      setHierarchy(null);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const pathRows = useMemo(() => {
    if (!hierarchy) return [];

    return hierarchy.outcomes.flatMap(outcome => {
      const objectives = hierarchy.objectives.filter(objective => objective.idpStrategicOutcomeId === outcome.id);
      return objectives.flatMap(objective => {
        const priorities = hierarchy.priorities.filter(priority => priority.idpStrategicObjectiveId === objective.id);
        return priorities.flatMap(priority => {
          const programmes = hierarchy.programmes.filter(programme => programme.idpDevelopmentPriorityId === priority.id);
          return programmes.flatMap(programme => {
            const projects = hierarchy.projects.filter(project => project.idpProgrammeId === programme.id);
            return projects.flatMap(project => {
              const kpis = hierarchy.kpis.filter(kpi => kpi.idpProjectId === project.id);
              return kpis.map(kpi => ({ outcome, objective, priority, programme, project, kpi }));
            });
          });
        });
      });
    });
  }, [hierarchy]);

  return (
    <AppShell title="Planning Hierarchy" subtitle="Outcome to annual target traceability across the full IDP chain">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            {!canManageHierarchy ? <Badge variant="warning">Read Only</Badge> : null}
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
            {canManageHierarchy ? (
              <Button
                variant="outline"
                onClick={async () => {
                  if (!selectedPlanId) return;
                  const result = await createIdpComment({
                    idpPlanId: selectedPlanId,
                    idpPlanVersionId: hierarchy?.versions.find(v => v.isActive)?.id ?? null,
                    entityName: 'IdpHierarchy',
                    entityId: selectedPlanId.toString(),
                    comment: 'Hierarchy review checkpoint captured from planning workspace',
                  });

                  pushToast(result.success ? 'success' : 'error', result.success ? 'Hierarchy review comment added.' : (result.message ?? 'Failed to add comment.'));
                }}
              >
                Add Review Comment
              </Button>
            ) : null}
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Hierarchy Drill-Down</h3>
          <div className="mt-3 overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="border-b border-secondary-200 text-left text-xs uppercase text-secondary-500 dark:border-secondary-700">
                <tr>
                  <th className="px-2 py-2">Outcome</th>
                  <th className="px-2 py-2">Objective</th>
                  <th className="px-2 py-2">Priority</th>
                  <th className="px-2 py-2">Programme</th>
                  <th className="px-2 py-2">Project</th>
                  <th className="px-2 py-2">KPI</th>
                </tr>
              </thead>
              <tbody>
                {pathRows.map(row => (
                  <tr key={`${row.kpi.id}`} className="border-b border-secondary-100 dark:border-secondary-800">
                    <td className="px-2 py-2">{row.outcome.code} - {row.outcome.name}</td>
                    <td className="px-2 py-2">{row.objective.code} - {row.objective.name}</td>
                    <td className="px-2 py-2">{row.priority.name}</td>
                    <td className="px-2 py-2">{row.programme.programmeCode}</td>
                    <td className="px-2 py-2">{row.project.projectCode}</td>
                    <td className="px-2 py-2">{row.kpi.kpiCode}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!pathRows.length ? <p className="p-3 text-sm text-secondary-500">No hierarchy records available for this plan.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpCommunityParticipationPage() {
  const { pushToast } = useApp();
  const canManageParticipation = useHasAnyPermission(['IDP.Participation.Manage']);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const dashboardResult = await getIdpDashboard(planId);
      setDashboard(dashboardResult.data ?? null);
    } else {
      setDashboard(null);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="Community Participation" subtitle="Ward consultations, public meetings, and stakeholder inputs">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            {!canManageParticipation ? <Badge variant="warning">Read Only</Badge> : null}
            <select
              value={selectedPlanId ?? ''}
              onChange={async event => {
                const planId = Number(event.target.value);
                setSelectedPlanId(planId);

                const dashboardResult = await getIdpDashboard(planId);
                setDashboard(dashboardResult.data ?? null);
              }}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
            {canManageParticipation ? (
              <Button
                variant="primary"
                onClick={async () => {
                  if (!selectedPlanId) {
                    pushToast('error', 'Select an IDP plan before logging participation.');
                    return;
                  }

                  const now = new Date();
                  const result = await createIdpCommunitySession({
                    idpPlanId: selectedPlanId,
                    participationType: 'PublicMeeting',
                    sessionDate: now.toISOString(),
                    venue: 'Municipal Hall',
                    wardId: null,
                    participantsCount: 120,
                    attendanceRegisterPath: '/documents/idp/public-meeting-attendance.pdf',
                    minutesPath: '/documents/idp/public-meeting-minutes.pdf',
                  });

                  if (result.success) {
                    pushToast('success', 'Community participation session logged.');
                    await load();
                  } else {
                    pushToast('error', result.message ?? 'Failed to log participation session.');
                  }
                }}
              >
                Log Public Meeting
              </Button>
            ) : null}
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Ward Participation Overview</h3>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            {(dashboard?.wardParticipation ?? []).map(item => (
              <div key={item.wardId} className="rounded border border-secondary-200 p-3 dark:border-secondary-700">
                <p className="font-medium text-secondary-900 dark:text-secondary-100">{item.wardName}</p>
                <p className="text-xs text-secondary-500">Meetings: {item.meetingCount} | Participants: {item.participantsCount} | Needs captured: {item.needsCaptured}</p>
              </div>
            ))}
            {!(dashboard?.wardParticipation.length) ? <p className="text-sm text-secondary-500">No participation records available.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpAlignmentMatrixPage() {
  const canManageAlignment = useHasAnyPermission(['IDP.Alignment.Manage']);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [matrix, setMatrix] = useState<IdpAlignmentMatrixItem[]>([]);

  const load = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    const planId = selectedPlanId ?? loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (planId) {
      const matrixResult = await getIdpAlignmentMatrix(planId);
      setMatrix(matrixResult.data ?? []);
    } else {
      setMatrix([]);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <AppShell title="Alignment Matrix" subtitle="NDP, PGDS, DDM, sector, and municipal alignment mapping">
      <div className="space-y-4">
        <Card>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
            {!canManageAlignment ? <Badge variant="warning">Read Only</Badge> : null}
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode}</option>)}
            </select>
          </div>
        </Card>

        <Card>
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="border-b border-secondary-200 text-left text-xs uppercase text-secondary-500 dark:border-secondary-700">
                <tr>
                  <th className="px-2 py-2">Strategic Outcome</th>
                  <th className="px-2 py-2">Objective</th>
                  <th className="px-2 py-2">Framework</th>
                  <th className="px-2 py-2">Reference</th>
                </tr>
              </thead>
              <tbody>
                {matrix.map(item => (
                  <tr key={`${item.objectiveCode}-${item.frameworkReferenceCode}`} className="border-b border-secondary-100 dark:border-secondary-800">
                    <td className="px-2 py-2">{item.strategicOutcomeCode} - {item.strategicOutcomeName}</td>
                    <td className="px-2 py-2">{item.objectiveCode} - {item.objectiveName}</td>
                    <td className="px-2 py-2">{item.frameworkType}</td>
                    <td className="px-2 py-2">{item.frameworkReferenceCode} - {item.frameworkReferenceTitle}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!matrix.length ? <p className="p-3 text-sm text-secondary-500">No alignment links available for this plan.</p> : null}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpReportsPage() {
  const { pushToast } = useApp();
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [lastReport, setLastReport] = useState<IdpReportDocument | null>(null);

  const loadPlans = async () => {
    const plansResult = await getIdpPlans();
    const loadedPlans = plansResult.data ?? [];
    setPlans(loadedPlans);
    setSelectedPlanId(current => current ?? loadedPlans[0]?.id ?? null);
  };

  useEffect(() => {
    void loadPlans();
  }, []);

  const generate = async (reportType: string, format: 'pdf' | 'excel' | 'word') => {
    if (!selectedPlanId) {
      pushToast('error', 'Select an IDP plan first.');
      return;
    }

    const result = await getIdpReport(selectedPlanId, reportType, format);
    if (result.success && result.data) {
      setLastReport(result.data);
      pushToast('success', `${reportType} report generated (${format.toUpperCase()}).`);
      return;
    }

    pushToast('error', result.message ?? 'Failed to generate report.');
  };

  return (
    <AppShell title="IDP Reporting Centre" subtitle="Generate annual, five-year, strategic, ward, provincial and national submissions">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <select
              value={selectedPlanId ?? ''}
              onChange={event => setSelectedPlanId(Number(event.target.value))}
              className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >
              {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.planCode} - {plan.planTitle}</option>)}
            </select>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('annual', 'pdf')}>Annual PDF</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('five-year', 'word')}>Five-Year Word</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('ward-based', 'excel')}>Ward Excel</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('provincial-submission', 'pdf')}>Provincial PDF</Button>
            <Button variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate('national-submission', 'pdf')}>National PDF</Button>
          </div>
        </Card>

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Last Generated Report</h3>
          {lastReport ? (
            <div className="mt-3 space-y-1 text-sm text-secondary-700 dark:text-secondary-300">
              <p><span className="font-medium">Name:</span> {lastReport.reportName}</p>
              <p><span className="font-medium">File:</span> {lastReport.fileName}</p>
              <p><span className="font-medium">Type:</span> {lastReport.contentType}</p>
              <p><span className="font-medium">Payload Size:</span> {lastReport.content.length} bytes</p>
            </div>
          ) : (
            <p className="mt-3 text-sm text-secondary-500">No report generated in this session.</p>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
