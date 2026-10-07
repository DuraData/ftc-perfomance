import { useEffect, useState } from 'react';
import { BarChart3, FileText, Layers, Map, RefreshCcw, Users } from 'lucide-react';
import {
  createIdpComment,
  createIdpPlan,
  createIdpPlanVersion,
    commitIdpHierarchyImport,
    commitIdpImport,
  getIdpImportBatch,
  getIdpImportBatchesPage,
  getIdpAlignmentMatrixPage,
  getIdpDashboard,
  getIdpPlansPage,
  getIdpHierarchyPathsPage,
  getIdpPlanVersionsPage,
  getIdpStakeholderEngagementsPage,
  getIdpReport,
  stageIdpHierarchyImport,
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
  IdpHierarchyPath,
  IdpImportBatch,
  IdpImportBatchSummary,
  IdpHierarchyImportRowPayload,
  IdpKpiImportRowPayload,
  IdpPlanSummary,
  IdpPlanVersion,
  IdpReportDocument,
  IdpStakeholderEngagement,
} from '../../types';
import { idpHierarchyCsvTemplate, idpKpiCsvTemplate, parseIdpHierarchyCsv, parseIdpKpiCsv } from './idpImportCsv';
import { IdpPlanPicker } from './IdpPlanPicker';
import { Input, Select } from '../common/Form';

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
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async (planId = selectedPlanId) => {
    setBusy(true);
    try {
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
          <IdpPlanPicker label="Dashboard plan" value={selectedPlanId ? String(selectedPlanId) : ''} autoSelectFirst onChange={value => { const planId = value ? Number(value) : null; setSelectedPlanId(planId); void load(planId); }} />
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
  const { canImport, canReadField } = useSecurity();
  const canManagePlan = useHasAnyPermission(['IDP.Plan.Manage', 'IDP.Version.Manage']);
  const canImportKpis = canImport('IDP_INDICATOR');
  const canImportHierarchy = canImport('IDP_PLAN');
  const canReadImportRequestId = canReadField('IDP_PLAN', 'ImportClientRequestId');
  const canReadImportFileName = canReadField('IDP_PLAN', 'ImportSourceFileName');
  const canReadImportHash = canReadField('IDP_PLAN', 'ImportSourceHash');
  const canReadImportActor = canReadField('IDP_PLAN', 'ImportActor');
  const canReadImportPayload = canReadField('IDP_PLAN', 'ImportRowPayload');
  const canReadImportError = canReadField('IDP_PLAN', 'ImportErrorDetail');
  const canSearchImportHistory = canReadImportFileName || canReadImportHash || canReadImportActor;
  const currentYear = new Date().getFullYear();
  const today = new Date().toISOString().slice(0, 10);
  const [plans, setPlans] = useState<IdpPlanSummary[]>([]);
  const [planPage, setPlanPage] = useState(1);
  const [planTotalCount, setPlanTotalCount] = useState(0);
  const [planTotalPages, setPlanTotalPages] = useState(0);
  const [planSearch, setPlanSearch] = useState('');
  const [planSortBy, setPlanSortBy] = useState('createdAt');
  const [planSortDirection, setPlanSortDirection] = useState<'asc' | 'desc'>('desc');
  const [versions, setVersions] = useState<IdpPlanVersion[]>([]);
  const [versionPage, setVersionPage] = useState(1);
  const [versionTotalCount, setVersionTotalCount] = useState(0);
  const [versionTotalPages, setVersionTotalPages] = useState(0);
  const [versionSearchInput, setVersionSearchInput] = useState('');
  const [versionSearch, setVersionSearch] = useState('');
  const [versionRevision, setVersionRevision] = useState(0);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [importMode, setImportMode] = useState<'KPI' | 'HIERARCHY'>(canImportHierarchy ? 'HIERARCHY' : 'KPI');
  const [importRows, setImportRows] = useState<Array<IdpKpiImportRowPayload | IdpHierarchyImportRowPayload>>([]);
  const [importFileName, setImportFileName] = useState('');
  const [importBatch, setImportBatch] = useState<IdpImportBatch | null>(null);
  const [importHistory, setImportHistory] = useState<IdpImportBatchSummary[]>([]);
  const [importHistoryPage, setImportHistoryPage] = useState(1);
  const [importHistoryTotalCount, setImportHistoryTotalCount] = useState(0);
  const [importHistoryTotalPages, setImportHistoryTotalPages] = useState(0);
  const [importHistorySearchInput, setImportHistorySearchInput] = useState('');
  const [importHistorySearch, setImportHistorySearch] = useState('');
  const [importHistoryStatus, setImportHistoryStatus] = useState<'' | 'Staged' | 'Committed' | 'Cancelled'>('');
  const [importHistoryType, setImportHistoryType] = useState<'' | 'KPI' | 'HIERARCHY'>('');
  const [importHistorySortBy, setImportHistorySortBy] = useState('createdAt');
  const [importHistorySortDirection, setImportHistorySortDirection] = useState<'asc' | 'desc'>('desc');
  const [importHistoryRevision, setImportHistoryRevision] = useState(0);
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
    const plansResult = await getIdpPlansPage({
      page: planPage,
      pageSize: 25,
      search: planSearch,
      sortBy: planSortBy,
      sortDirection: planSortDirection,
    });
    const loadedPlans = plansResult.data?.items ?? [];
    setPlans(loadedPlans);
    setPlanTotalCount(plansResult.data?.totalCount ?? 0);
    setPlanTotalPages(plansResult.data?.totalPages ?? 0);
    const planId = loadedPlans.some(plan => plan.id === selectedPlanId) ? selectedPlanId : loadedPlans[0]?.id ?? null;
    setSelectedPlanId(planId);

    if (!planId) {
      setVersions([]);
      setVersionTotalCount(0);
      setVersionTotalPages(0);
      setImportHistory([]);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [planPage, planSearch, planSortBy, planSortDirection]);

  const selectPlan = (planId: number) => {
    setSelectedPlanId(planId);
    setVersionPage(1);
    setImportHistoryPage(1);
    setImportBatch(null);
  };

  const selectedPlan = plans.find(plan => plan.id === selectedPlanId) ?? null;

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setVersionPage(1);
      setVersionSearch(versionSearchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [versionSearchInput]);

  useEffect(() => {
    if (!selectedPlan) {
      setVersions([]);
      setVersionTotalCount(0);
      setVersionTotalPages(0);
      return;
    }
    void getIdpPlanVersionsPage(selectedPlan.publicId, {
      page: versionPage,
      pageSize: 25,
      search: versionSearch,
      sortBy: 'versionNumber',
      sortDirection: 'desc',
    }).then(result => {
      setVersions(result.data?.items ?? []);
      setVersionTotalCount(result.data?.totalCount ?? 0);
      setVersionTotalPages(result.data?.totalPages ?? 0);
    });
  }, [selectedPlan, versionPage, versionSearch, versionRevision]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setImportHistoryPage(1);
      setImportHistorySearch(importHistorySearchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [importHistorySearchInput]);

  useEffect(() => {
    if (!selectedPlan || (!canImportKpis && !canImportHierarchy)) {
      setImportHistory([]);
      setImportHistoryTotalCount(0);
      setImportHistoryTotalPages(0);
      return;
    }
    void getIdpImportBatchesPage(selectedPlan.publicId, {
      page: importHistoryPage,
      pageSize: 25,
      search: importHistorySearch,
      status: importHistoryStatus || undefined,
      importType: importHistoryType || undefined,
      sortBy: importHistorySortBy,
      sortDirection: importHistorySortDirection,
    }).then(result => {
      setImportHistory(result.data?.items ?? []);
      setImportHistoryTotalCount(result.data?.totalCount ?? 0);
      setImportHistoryTotalPages(result.data?.totalPages ?? 0);
    });
  }, [selectedPlan, canImportKpis, canImportHierarchy, importHistoryPage, importHistorySearch, importHistoryStatus, importHistoryType, importHistorySortBy, importHistorySortDirection, importHistoryRevision]);

  const openImportBatch = async (batchPublicId: string) => {
    const result = await getIdpImportBatch(batchPublicId);
    if (result.success && result.data) setImportBatch(result.data);
    else pushToast('error', result.message ?? 'Unable to load the import reconciliation detail.');
  };

  const stageImport = async () => {
    if (!selectedPlan || !importRows.length || !importFileName) {
      pushToast('error', `Select a plan and a valid ${importMode === 'KPI' ? 'KPI' : 'hierarchy'} CSV file first.`);
      return;
    }
    setImportBusy(true);
    try {
      const requestId = crypto.randomUUID();
      const result = importMode === 'KPI'
        ? await stageIdpKpiImport(selectedPlan.publicId, { clientRequestId: requestId, sourceFileName: importFileName, rows: importRows as IdpKpiImportRowPayload[] })
        : await stageIdpHierarchyImport(selectedPlan.publicId, { clientRequestId: requestId, sourceFileName: importFileName, rows: importRows as IdpHierarchyImportRowPayload[] });
      if (!result.success || !result.data) {
        pushToast('error', result.message ?? 'Unable to stage the import.');
        return;
      }
      setImportBatch(result.data);
      setImportHistoryPage(1);
      setImportHistoryRevision(value => value + 1);
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
        const commit = importBatch.importType === 'HIERARCHY' ? commitIdpHierarchyImport : commitIdpImport;
        const result = await commit(importBatch.publicId, { rowVersion: importBatch.rowVersion, reason: importReason.trim() });
      if (!result.success || !result.data) {
        pushToast('error', result.message ?? 'Unable to commit the import.');
        return;
      }
      setImportBatch(result.data);
      setImportHistoryRevision(value => value + 1);
      setImportReason('');
      pushToast('success', `${importBatch.importType === 'HIERARCHY' ? 'Hierarchy' : 'KPI'} import committed atomically.`);
    } finally {
      setImportBusy(false);
    }
  };

  const downloadImportTemplate = () => {
    const hierarchy = importMode === 'HIERARCHY';
    const url = URL.createObjectURL(new Blob([hierarchy ? idpHierarchyCsvTemplate : idpKpiCsvTemplate], { type: 'text/csv;charset=utf-8' }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = hierarchy ? 'idp-hierarchy-import-template.csv' : 'idp-kpi-import-template.csv';
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
      setVersionPage(1);
      setVersionRevision(value => value + 1);
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
              <IdpPlanPicker label="Predecessor plan" value={planDraft.predecessorPlanPublicId} valueField="publicId" emptyLabel="New plan family" onChange={value => setPlanDraft(current => ({ ...current, predecessorPlanPublicId: value }))} />
              <label className="text-xs text-secondary-600">Effective from<input aria-label="Plan effective from" type="date" className={fieldClass} value={planDraft.effectiveFrom} onChange={event => setPlanDraft({ ...planDraft, effectiveFrom: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Effective to<input aria-label="Plan effective to" type="date" className={fieldClass} value={planDraft.effectiveTo} onChange={event => setPlanDraft({ ...planDraft, effectiveTo: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Publication reference<input aria-label="Plan publication reference" className={fieldClass} value={planDraft.publicationReference} onChange={event => setPlanDraft({ ...planDraft, publicationReference: event.target.value })} /></label>
              <div className="md:col-span-2 xl:col-span-4"><Button variant="primary" onClick={() => void submitPlan()}>Create Plan</Button></div>
            </div>
          ) : null}
        </Card>

        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Plan Register</h3>
              <Badge variant="primary">{planTotalCount} plans</Badge>
            </div>
            <div className="mt-3 grid gap-2 sm:grid-cols-[1fr_10rem_9rem]">
              <input aria-label="Search IDP plans" placeholder="Code, title, municipality, or publication" className={fieldClass} value={planSearch} onChange={event => { setPlanSearch(event.target.value); setPlanPage(1); }} />
              <select aria-label="Sort IDP plans" className={fieldClass} value={planSortBy} onChange={event => { setPlanSortBy(event.target.value); setPlanPage(1); }}><option value="createdAt">Created date</option><option value="planCode">Plan code</option><option value="planTitle">Plan title</option><option value="status">Status</option><option value="effectiveFrom">Effective from</option><option value="startFinancialYear">Start year</option></select>
              <select aria-label="IDP plan sort direction" className={fieldClass} value={planSortDirection} onChange={event => { setPlanSortDirection(event.target.value as 'asc' | 'desc'); setPlanPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select>
            </div>
            <div className="mt-3 space-y-2">
              {plans.map(plan => (
                <button
                  key={plan.id}
                  onClick={() => selectPlan(plan.id)}
                  className={`w-full rounded border px-3 py-2 text-left ${selectedPlanId === plan.id ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-secondary-200 dark:border-secondary-700'}`}
                >
                  <p className="font-medium text-secondary-900 dark:text-secondary-100">{plan.planCode} - {plan.planTitle}</p>
                  <p className="text-xs text-secondary-500">{plan.startFinancialYear}/{plan.startFinancialYear + 1} to {plan.endFinancialYear}/{plan.endFinancialYear + 1} | Status: {plan.status}</p>
                  <p className="text-xs text-secondary-500">Family: {plan.planFamilyId} | Effective: {new Date(plan.effectiveFrom).toLocaleDateString()} | Publication: {plan.publicationReference ?? 'Not published'}</p>
                </button>
              ))}
              {!plans.length ? <p className="py-6 text-center text-sm text-secondary-500">No plans match the current search.</p> : null}
            </div>
            {planTotalPages > 1 ? <div className="mt-3 flex items-center justify-between text-xs text-secondary-500"><span>Page {planPage} of {planTotalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={planPage <= 1} onClick={() => setPlanPage(value => Math.max(1, value - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={planPage >= planTotalPages} onClick={() => setPlanPage(value => value + 1)}>Next</Button></div></div> : null}
          </Card>

          <Card>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Version Control</h3>
              <Badge variant="primary">{versionTotalCount} versions</Badge>
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
            <div className="mt-3">
              <input aria-label="Search IDP versions" placeholder="Label, review year, changes, or publication" className={fieldClass} value={versionSearchInput} onChange={event => setVersionSearchInput(event.target.value)} />
            </div>
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
            <div className="mt-3 flex items-center justify-between text-xs text-secondary-500">
              <span>Page {versionPage} of {Math.max(1, versionTotalPages)}</span>
              <div className="flex gap-2"><Button size="sm" variant="outline" disabled={versionPage <= 1} onClick={() => setVersionPage(value => value - 1)}>Previous versions</Button><Button size="sm" variant="outline" disabled={versionPage >= versionTotalPages} onClick={() => setVersionPage(value => value + 1)}>Next versions</Button></div>
            </div>
          </Card>
        </div>

        {canImportKpis || canImportHierarchy ? (
          <Card>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">IDP import reconciliation</h3>
                <p className="text-xs text-secondary-500">Stage a full hierarchy/project or KPI CSV, review NEW/UNCHANGED/CHANGED/INVALID rows, then commit the valid batch atomically.</p>
              </div>
              <Button variant="outline" onClick={downloadImportTemplate}>Download CSV Template</Button>
            </div>

            {canImportKpis && canImportHierarchy ? (
              <div className="mt-3 flex gap-2" role="group" aria-label="IDP import type">
                <Button variant={importMode === 'HIERARCHY' ? 'primary' : 'outline'} size="sm" onClick={() => { setImportMode('HIERARCHY'); setImportRows([]); setImportFileName(''); setImportBatch(null); }}>Hierarchy and projects</Button>
                <Button variant={importMode === 'KPI' ? 'primary' : 'outline'} size="sm" onClick={() => { setImportMode('KPI'); setImportRows([]); setImportFileName(''); setImportBatch(null); }}>KPI definitions</Button>
              </div>
            ) : null}

            <div className="mt-4 grid gap-3 md:grid-cols-[1fr_auto]">
              <label className="text-xs text-secondary-600">
                {importMode === 'HIERARCHY' ? 'Hierarchy/project' : 'KPI'} CSV file
                <input
                  aria-label={`${importMode === 'HIERARCHY' ? 'Hierarchy/project' : 'KPI'} CSV file`}
                  type="file"
                  accept=".csv,text/csv"
                  className={fieldClass}
                  onChange={event => {
                    const file = event.target.files?.[0];
                    if (!file) return;
                    void file.text().then(content => {
                      try {
                        const rows = importMode === 'HIERARCHY' ? parseIdpHierarchyCsv(content) : parseIdpKpiCsv(content);
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
                {(canReadImportRequestId || canReadImportFileName || canReadImportHash || canReadImportActor) ? <div className="text-xs text-secondary-500">
                  {canReadImportFileName && importBatch.sourceFileName ? <span>Source: {importBatch.sourceFileName}</span> : null}
                  {canReadImportHash && importBatch.sourceSha256 ? <span> · SHA-256: {importBatch.sourceSha256}</span> : null}
                  {canReadImportRequestId && importBatch.clientRequestId ? <span> · Request: {importBatch.clientRequestId}</span> : null}
                  {canReadImportActor && importBatch.createdByName ? <span> · Created by {importBatch.createdByName}{importBatch.createdByUserPublicId ? ` (${importBatch.createdByUserPublicId})` : ''}</span> : null}
                  {canReadImportActor && importBatch.committedByName ? <span> · Committed by {importBatch.committedByName}{importBatch.committedByUserPublicId ? ` (${importBatch.committedByUserPublicId})` : ''}</span> : null}
                </div> : null}
                <div className="max-h-80 overflow-auto rounded border border-secondary-200 dark:border-secondary-700">
                  <table className="min-w-full text-left text-xs">
                    <thead className="bg-secondary-50 dark:bg-secondary-800"><tr><th className="px-3 py-2">Row</th><th className="px-3 py-2">Reference</th><th className="px-3 py-2">Result</th>{canReadImportError ? <th className="px-3 py-2">Error</th> : null}</tr></thead>
                    <tbody>{importBatch.rows.map(row => <tr key={row.publicId} className="border-t border-secondary-200 dark:border-secondary-700"><td className="px-3 py-2">{row.sourceRowNumber}</td><td className="px-3 py-2">{row.reference}</td><td className="px-3 py-2"><Badge variant={row.status === 'Invalid' ? 'error' : row.status === 'Changed' ? 'warning' : row.status === 'New' ? 'success' : 'info'}>{row.status}</Badge></td>{canReadImportError ? <td className="px-3 py-2 text-error-700">{row.errorCode ? `${row.errorCode}: ${row.errorMessage}` : '—'}</td> : null}</tr>)}</tbody>
                  </table>
                </div>
                {canReadImportPayload && importBatch.rows.some(row => row.existingValueJson || row.normalizedJson || row.suppliedValue) ? <p className="text-xs text-secondary-500">Authorized reconciliation payload details are retained by the API for governed inspection.</p> : null}
                {importBatch.status === 'Staged' ? (
                  <div className="flex flex-wrap items-end gap-2">
                    <label className="min-w-72 flex-1 text-xs text-secondary-600">Commit reason<input aria-label="Import commit reason" className={fieldClass} value={importReason} onChange={event => setImportReason(event.target.value)} /></label>
                    <Button variant="primary" disabled={importBusy || importBatch.invalidRows > 0 || !importReason.trim()} onClick={() => void commitImport()}>Commit Valid Batch</Button>
                  </div>
                ) : null}
              </div>
            ) : null}

            <div className="mt-5 space-y-3">
              <div className="flex flex-wrap items-end gap-2">
                <h4 className="mr-auto text-sm font-semibold text-secondary-800 dark:text-secondary-200">Import history · {importHistoryTotalCount}</h4>
                {canSearchImportHistory ? <label className="text-xs text-secondary-600">Search<input aria-label="Search IDP import history" className={fieldClass} value={importHistorySearchInput} onChange={event => setImportHistorySearchInput(event.target.value)} /></label> : null}
                <label className="text-xs text-secondary-600">Status<select aria-label="Filter IDP import status" className={fieldClass} value={importHistoryStatus} onChange={event => { setImportHistoryStatus(event.target.value as typeof importHistoryStatus); setImportHistoryPage(1); }}><option value="">All</option><option value="Staged">Staged</option><option value="Committed">Committed</option><option value="Cancelled">Cancelled</option></select></label>
                <label className="text-xs text-secondary-600">Type<select aria-label="Filter IDP import type" className={fieldClass} value={importHistoryType} onChange={event => { setImportHistoryType(event.target.value as typeof importHistoryType); setImportHistoryPage(1); }}><option value="">All</option><option value="KPI">KPI</option><option value="HIERARCHY">Hierarchy</option></select></label>
                <label className="text-xs text-secondary-600">Sort<select aria-label="Sort IDP import history" className={fieldClass} value={importHistorySortBy} onChange={event => { setImportHistorySortBy(event.target.value); setImportHistoryPage(1); }}><option value="createdAt">Created</option>{canReadImportFileName ? <option value="fileName">File name</option> : null}<option value="status">Status</option><option value="importType">Type</option><option value="totalRows">Row count</option><option value="committedAt">Committed</option></select></label>
                <select aria-label="IDP import sort direction" className={fieldClass} value={importHistorySortDirection} onChange={event => { setImportHistorySortDirection(event.target.value as 'asc' | 'desc'); setImportHistoryPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select>
              </div>
              {importHistory.length ? (
                <div className="flex flex-wrap gap-2">{importHistory.map(batch => <button key={batch.publicId} type="button" className="rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-700" onClick={() => void openImportBatch(batch.publicId)}>{batch.importType}{canReadImportFileName && batch.sourceFileName ? ` · ${batch.sourceFileName}` : ''} · {batch.status} · {batch.totalRows} rows · {new Date(batch.createdAt).toLocaleString()}</button>)}</div>
              ) : <p className="text-sm text-secondary-500">No import batches match the current filters.</p>}
              <div className="flex items-center justify-between text-xs text-secondary-500">
                <span>Page {importHistoryPage} of {Math.max(1, importHistoryTotalPages)}</span>
                <div className="flex gap-2"><Button variant="outline" disabled={importHistoryPage <= 1} onClick={() => setImportHistoryPage(value => value - 1)}>Previous imports</Button><Button variant="outline" disabled={importHistoryPage >= importHistoryTotalPages} onClick={() => setImportHistoryPage(value => value + 1)}>Next imports</Button></div>
              </div>
            </div>
          </Card>
        ) : null}
      </div>
    </AppShell>
  );
}

export function IdpHierarchyPage() {
  const { pushToast } = useApp();
  const canManageHierarchy = useHasAnyPermission(['IDP.Hierarchy.Manage', 'IDP.Collaboration.Manage']);
  const [selectedPlan, setSelectedPlan] = useState<IdpPlanSummary | null>(null);
  const [pathRows, setPathRows] = useState<IdpHierarchyPath[]>([]);
  const [activeVersionId, setActiveVersionId] = useState<number | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('outcome');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    if (!selectedPlan) {
      setPathRows([]);
      setActiveVersionId(null);
      setTotalCount(0);
      setTotalPages(0);
      return;
    }
    void Promise.all([
      getIdpHierarchyPathsPage(selectedPlan.publicId, { page, pageSize: 25, search, sortBy, sortDirection }),
      getIdpPlanVersionsPage(selectedPlan.publicId, { page: 1, pageSize: 1, sortBy: 'versionNumber', sortDirection: 'desc' }, true),
    ]).then(([pathsResult, versionsResult]) => {
      setPathRows(pathsResult.data?.items ?? []);
      setTotalCount(pathsResult.data?.totalCount ?? 0);
      setTotalPages(pathsResult.data?.totalPages ?? 0);
      setActiveVersionId(versionsResult.data?.items[0]?.id ?? null);
    });
  }, [page, refreshKey, search, selectedPlan, sortBy, sortDirection]);

  return (
    <AppShell title="Planning Hierarchy" subtitle="Outcome-to-KPI traceability across the governed IDP chain">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => setRefreshKey(value => value + 1)}>Refresh</Button>
            {!canManageHierarchy ? <Badge variant="warning">Read Only</Badge> : null}
            <IdpPlanPicker label="Hierarchy plan" value={selectedPlan?.publicId ?? ''} valueField="publicId" autoSelectFirst onChange={(_value, plan) => { setSelectedPlan(plan ?? null); setPage(1); }} />
            {canManageHierarchy ? (
              <Button
                variant="outline"
                onClick={async () => {
                  if (!selectedPlan) return;
                  const result = await createIdpComment({
                    idpPlanId: selectedPlan.id,
                    idpPlanVersionId: activeVersionId,
                    entityName: 'IdpHierarchy',
                    entityId: selectedPlan.publicId,
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
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Hierarchy Drill-Down</h3>
            <Badge variant="primary">{totalCount} KPI paths</Badge>
          </div>
          <div className="mt-3 grid gap-2 md:grid-cols-[1fr_10rem_9rem]">
            <input aria-label="Search IDP hierarchy paths" placeholder="Search any hierarchy level" className="w-full rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-800 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-100" value={searchInput} onChange={event => setSearchInput(event.target.value)} />
            <select aria-label="Sort IDP hierarchy paths" className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); }}><option value="outcome">Outcome</option><option value="objective">Objective</option><option value="priority">Priority</option><option value="programme">Programme</option><option value="project">Project</option><option value="kpi">KPI</option></select>
            <select aria-label="IDP hierarchy sort direction" className="rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); }}><option value="asc">Ascending</option><option value="desc">Descending</option></select>
          </div>
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
                  <tr key={row.kpiPublicId} className="border-b border-secondary-100 dark:border-secondary-800">
                    <td className="px-2 py-2">{row.outcomeCode} - {row.outcomeName}</td>
                    <td className="px-2 py-2">{row.objectiveCode} - {row.objectiveName}</td>
                    <td className="px-2 py-2">{row.priorityCode} - {row.priorityName}</td>
                    <td className="px-2 py-2">{row.programmeCode} - {row.programmeName}</td>
                    <td className="px-2 py-2">{row.projectCode} - {row.projectName}</td>
                    <td className="px-2 py-2">{row.kpiCode} - {row.kpiName}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!pathRows.length ? <p className="p-3 text-sm text-secondary-500">No hierarchy records available for this plan.</p> : null}
          </div>
          <div className="mt-3 flex items-center justify-between text-xs text-secondary-500">
            <span>Page {page} of {Math.max(1, totalPages)}</span>
            <div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous paths</Button><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next paths</Button></div>
          </div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpCommunityParticipationPage() {
  const { pushToast } = useApp();
  const security = useSecurity();
  const canManageParticipation = useHasAnyPermission(['IDP.Participation.Manage']);
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [selectedPlanPublicId, setSelectedPlanPublicId] = useState('');
  const [dashboard, setDashboard] = useState<IdpDashboard | null>(null);
  const [stakeholders, setStakeholders] = useState<IdpStakeholderEngagement[]>([]);
  const [stakeholderPage, setStakeholderPage] = useState(1);
  const [stakeholderTotalCount, setStakeholderTotalCount] = useState(0);
  const [stakeholderTotalPages, setStakeholderTotalPages] = useState(0);
  const [stakeholderSearchInput, setStakeholderSearchInput] = useState('');
  const [stakeholderSearch, setStakeholderSearch] = useState('');
  const [stakeholderSort, setStakeholderSort] = useState('sessionDate');
  const [stakeholderRevision, setStakeholderRevision] = useState(0);
  const canReadContactPerson = security.canReadField('IDP_STAKEHOLDER', 'ContactPerson');
  const canReadContactEmail = security.canReadField('IDP_STAKEHOLDER', 'ContactEmail');

  const load = async (planId = selectedPlanId) => {
    if (planId) {
      const dashboardResult = await getIdpDashboard(planId);
      setDashboard(dashboardResult.data ?? null);
    } else {
      setDashboard(null);
    }
  };

  useEffect(() => {
    const next = stakeholderSearchInput.trim();
    if (next === stakeholderSearch) return;
    const timeout = window.setTimeout(() => { setStakeholderPage(1); setStakeholderSearch(next); }, 300);
    return () => window.clearTimeout(timeout);
  }, [stakeholderSearch, stakeholderSearchInput]);

  useEffect(() => {
    let cancelled = false;
    if (!selectedPlanPublicId) {
      setStakeholders([]); setStakeholderTotalCount(0); setStakeholderTotalPages(0);
      return () => { cancelled = true; };
    }
    void getIdpStakeholderEngagementsPage(selectedPlanPublicId, {
      page: stakeholderPage, pageSize: 25, search: stakeholderSearch || undefined,
      sortBy: stakeholderSort, sortDirection: stakeholderSort === 'sessionDate' ? 'desc' : 'asc',
    }).then(result => {
      if (cancelled) return;
      setStakeholders(result.data?.items ?? []);
      setStakeholderTotalCount(result.data?.totalCount ?? 0);
      setStakeholderTotalPages(result.data?.totalPages ?? 0);
    });
    return () => { cancelled = true; };
  }, [selectedPlanPublicId, stakeholderPage, stakeholderRevision, stakeholderSearch, stakeholderSort]);

  return (
    <AppShell title="Community Participation" subtitle="Ward consultations, public meetings, and stakeholder inputs">
      <div className="space-y-4">
        <Card>
          <div className="flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => { void load(); setStakeholderRevision(value => value + 1); }}>Refresh</Button>
            {!canManageParticipation ? <Badge variant="warning">Read Only</Badge> : null}
            <IdpPlanPicker label="Participation plan" value={selectedPlanPublicId} valueField="publicId" autoSelectFirst onChange={(value, plan) => { setSelectedPlanPublicId(value); setSelectedPlanId(plan?.id ?? null); setStakeholderPage(1); void load(plan?.id ?? null); }} />
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

        <Card>
          <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Stakeholder Engagement Register</h3>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            <Input label="Search stakeholder engagements" value={stakeholderSearchInput} onChange={event => setStakeholderSearchInput(event.target.value)} />
            <Select label="Sort stakeholder engagements" value={stakeholderSort} options={[
              { value: 'sessionDate', label: 'Session date' }, { value: 'stakeholderName', label: 'Stakeholder name' },
              { value: 'stakeholderType', label: 'Stakeholder type' },
              ...(canReadContactPerson ? [{ value: 'contactPerson', label: 'Contact person' }] : []),
              ...(canReadContactEmail ? [{ value: 'contactEmail', label: 'Contact email' }] : []),
            ]} onChange={event => { setStakeholderSort(event.target.value); setStakeholderPage(1); }} />
          </div>
          <div className="mt-3 overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="border-b border-secondary-200 text-left text-xs uppercase text-secondary-500 dark:border-secondary-700"><tr><th className="px-2 py-2">Session</th><th className="px-2 py-2">Stakeholder</th><th className="px-2 py-2">Type</th>{canReadContactPerson ? <th className="px-2 py-2">Contact person</th> : null}{canReadContactEmail ? <th className="px-2 py-2">Contact email</th> : null}<th className="px-2 py-2">Key input</th></tr></thead>
              <tbody>{stakeholders.map(item => <tr key={item.publicId} className="border-b border-secondary-100 dark:border-secondary-800"><td className="px-2 py-2">{new Date(item.sessionDate).toLocaleDateString()} · {item.venue}</td><td className="px-2 py-2">{item.stakeholderName}</td><td className="px-2 py-2">{item.stakeholderType}</td>{canReadContactPerson ? <td className="px-2 py-2">{item.contactPerson ?? '-'}</td> : null}{canReadContactEmail ? <td className="px-2 py-2">{item.contactEmail ?? '-'}</td> : null}<td className="px-2 py-2">{item.keyInput ?? '-'}</td></tr>)}</tbody>
            </table>
            {!stakeholders.length ? <p className="p-3 text-sm text-secondary-500">No stakeholder engagements match the current filters.</p> : null}
          </div>
          <div className="mt-3 flex items-center justify-between gap-2 text-xs text-secondary-500"><span>{stakeholderTotalCount} stakeholder engagement{stakeholderTotalCount === 1 ? '' : 's'}</span><span className="flex items-center gap-2"><Button size="sm" variant="ghost" disabled={stakeholderPage <= 1} onClick={() => setStakeholderPage(value => Math.max(1, value - 1))}>Previous stakeholders</Button><span>Page {stakeholderPage} of {Math.max(stakeholderTotalPages, 1)}</span><Button size="sm" variant="ghost" disabled={stakeholderPage >= stakeholderTotalPages} onClick={() => setStakeholderPage(value => value + 1)}>Next stakeholders</Button></span></div>
        </Card>
      </div>
    </AppShell>
  );
}

export function IdpAlignmentMatrixPage() {
  const canManageAlignment = useHasAnyPermission(['IDP.Alignment.Manage']);
  const [selectedPlanPublicId, setSelectedPlanPublicId] = useState('');
  const [matrix, setMatrix] = useState<IdpAlignmentMatrixItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [frameworkType, setFrameworkType] = useState('');
  const [sortBy, setSortBy] = useState('objective');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [refreshKey, setRefreshKey] = useState(0);
  const fieldClass = 'w-full rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-800 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-100';

  useEffect(() => {
    const nextSearch = searchInput.trim();
    if (nextSearch === search) return;
    const timeout = window.setTimeout(() => { setPage(1); setSearch(nextSearch); }, 300);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      if (!selectedPlanPublicId) {
        setMatrix([]); setTotalCount(0); setTotalPages(0);
        return;
      }
      const result = await getIdpAlignmentMatrixPage(
        selectedPlanPublicId,
        { page, pageSize: 25, search: search || undefined, sortBy, sortDirection },
        frameworkType || undefined,
      );
      if (cancelled) return;
      setMatrix(result.data?.items ?? []);
      setTotalCount(result.data?.totalCount ?? 0);
      setTotalPages(result.data?.totalPages ?? 0);
    };
    void load();
    return () => { cancelled = true; };
  }, [frameworkType, page, refreshKey, search, selectedPlanPublicId, sortBy, sortDirection]);

  return (
    <AppShell title="Alignment Matrix" subtitle="NDP, PGDS, DDM, sector, and municipal alignment mapping">
      <div className="space-y-4">
        <Card>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[1fr_1fr_12rem_12rem_10rem_auto] xl:items-end">
            <IdpPlanPicker label="Alignment plan" value={selectedPlanPublicId} valueField="publicId" autoSelectFirst onChange={value => { setSelectedPlanPublicId(value); setPage(1); }} />
            <label className="text-xs text-secondary-600">Search matrix<input aria-label="Search IDP alignment matrix" className={fieldClass} value={searchInput} onChange={event => setSearchInput(event.target.value)} /></label>
            <label className="text-xs text-secondary-600">Framework<select aria-label="Filter IDP alignment framework" className={fieldClass} value={frameworkType} onChange={event => { setFrameworkType(event.target.value); setPage(1); }}><option value="">All frameworks</option><option value="NationalDevelopmentPlan">NDP</option><option value="ProvincialGrowthStrategy">PGDS</option><option value="DistrictDevelopmentModel">DDM</option><option value="SectorPlan">Sector plan</option><option value="MunicipalGoal">Municipal goal</option><option value="Circular88">Circular 88</option><option value="TreasuryTid">Treasury TID</option></select></label>
            <label className="text-xs text-secondary-600">Sort<select aria-label="Sort IDP alignment matrix" className={fieldClass} value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); }}><option value="objective">Objective</option><option value="outcome">Outcome</option><option value="framework">Framework</option><option value="reference">Reference</option></select></label>
            <label className="text-xs text-secondary-600">Direction<select aria-label="IDP alignment sort direction" className={fieldClass} value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); }}><option value="asc">Ascending</option><option value="desc">Descending</option></select></label>
            <div className="flex items-center gap-2"><Button variant="outline" onClick={() => setRefreshKey(value => value + 1)}>Refresh</Button>{!canManageAlignment ? <Badge variant="warning">Read Only</Badge> : null}</div>
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
          <div className="mt-3 flex items-center justify-between gap-2 text-xs text-secondary-500"><span>{totalCount} alignment link{totalCount === 1 ? '' : 's'}</span><span className="flex items-center gap-2"><Button size="sm" variant="ghost" disabled={page <= 1} onClick={() => setPage(current => Math.max(1, current - 1))}>Previous alignments</Button><span>Page {page} of {Math.max(totalPages, 1)}</span><Button size="sm" variant="ghost" disabled={page >= totalPages} onClick={() => setPage(current => current + 1)}>Next alignments</Button></span></div>
        </Card>

      </div>
    </AppShell>
  );
}

export function IdpReportsPage() {
  const { pushToast } = useApp();
  const [selectedPlanId, setSelectedPlanId] = useState<number | null>(null);
  const [lastReport, setLastReport] = useState<IdpReportDocument | null>(null);

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
            <IdpPlanPicker label="Report plan" value={selectedPlanId ? String(selectedPlanId) : ''} autoSelectFirst onChange={value => setSelectedPlanId(value ? Number(value) : null)} />
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
