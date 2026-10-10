import { useEffect, useState } from 'react';
import { BarChart3, CalendarRange, ClipboardList, FileText, Layers, RefreshCw } from 'lucide-react';
import { Badge, Button, Card, EmptyState, LoadingSpinner, ProgressBar } from '../ui';
import { getIpmsPerformanceDashboard } from '../../api/api';
import { AppShell } from '../layout/AppShell';
import { useApp } from '../../context/AppContext';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import type { PerformanceDashboardDto } from '../../types';

const emptyDashboard: PerformanceDashboardDto = {
  totalTargets: 0,
  activeTargets: 0,
  completedTargets: 0,
  overdueTargets: 0,
  atRiskTargets: 0,
  outstandingTargets: 0,
  draftSubmissions: 0,
  submittedSubmissions: 0,
  returnedSubmissions: 0,
  approvedSubmissions: 0,
  pendingVerification: 0,
  pendingApproval: 0,
};

function percentage(value: number, denominator: number) {
  return denominator > 0 ? (value / denominator) * 100 : 0;
}

function windowBadgeVariant(state?: string | null): 'success' | 'warning' | 'default' | 'info' {
  if (state === 'Open') return 'success';
  if (state === 'Upcoming') return 'info';
  if (state === 'Not configured') return 'warning';
  return 'default';
}

function formatDate(value?: string | null) {
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toLocaleDateString();
}

function tile(label: string, value: number, denominator: number, onClick: () => void, disabled = false) {
  return (
    <Button
      key={label}
      variant="outline"
      className="min-h-24 flex-1 flex-col items-start gap-1 rounded-xl border-secondary-200 p-4 text-left hover:border-primary-500"
      onClick={onClick}
      disabled={disabled}
      aria-label={`${label}: ${value} of ${denominator}`}
    >
      <span className="text-xs uppercase tracking-wide text-secondary-500">{label}</span>
      <span className="text-2xl font-semibold text-secondary-900 dark:text-white">{value}</span>
      <span className="text-xs font-normal text-secondary-500">of {denominator} scoped KPI{denominator === 1 ? '' : 's'}</span>
    </Button>
  );
}

export function IPMSDashboardPage() {
  const { setCurrentPath } = useApp();
  const [stats, setStats] = useState<PerformanceDashboardDto>(emptyDashboard);
  const [financialYearId, setFinancialYearId] = useState('');
  const [reportingPeriodId, setReportingPeriodId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [retryRevision, setRetryRevision] = useState(0);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setLoading(true);
      setError(undefined);
      const result = await getIpmsPerformanceDashboard({
        municipalityFinancialYearPublicId: financialYearId || undefined,
        reportingPeriodPublicId: reportingPeriodId || undefined,
      });
      if (cancelled) return;
      if (!result.success || !result.data) {
        setError(result.message ?? 'Unable to load the governed IPMS dashboard.');
      } else {
        setStats(result.data);
        const resolvedYear = result.data.municipalityFinancialYearPublicId ?? '';
        const resolvedPeriod = result.data.reportingPeriodPublicId ?? '';
        if (resolvedYear !== financialYearId) setFinancialYearId(resolvedYear);
        if (resolvedPeriod !== reportingPeriodId) setReportingPeriodId(resolvedPeriod);
      }
      setLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [financialYearId, reportingPeriodId, retryRevision]);

  const achievedRate = percentage(stats.completedTargets, stats.totalTargets);
  const atRiskRate = percentage(stats.atRiskTargets, stats.totalTargets);
  const outstandingRate = percentage(stats.outstandingTargets, stats.totalTargets);
  const selectedPeriod = stats.periodBreakdown?.find(item => item.reportingPeriodPublicId === reportingPeriodId);
  const windowDates = selectedPeriod?.opensAt || selectedPeriod?.closesAt
    ? `${formatDate(selectedPeriod.opensAt) ?? 'Not set'} – ${formatDate(selectedPeriod.closesAt) ?? 'Not set'}`
    : 'No reporting window dates configured';

  const quickLinks = [
    { label: 'KPIs', path: '/ipms/targets' },
    { label: 'Submissions', path: '/ipms/submissions' },
    { label: 'Reports', path: '/reports/ipms-performance' },
  ];
  const drilldownPath = (route: '/ipms/targets' | '/ipms/submissions', filter: 'assigned' | 'achieved' | 'at-risk' | 'outstanding' | 'draft' | 'submitted' | 'returned' | 'approved') => {
    const parameters = new URLSearchParams({
      municipalityFinancialYearPublicId: financialYearId,
      reportingPeriodPublicId: reportingPeriodId,
      dashboardFilter: filter,
    });
    return `${route}?${parameters.toString()}`;
  };
  const drilldownDisabled = loading || !financialYearId || !reportingPeriodId;

  return (
    <AppShell title="IPMS Dashboard" subtitle="Individual performance results and workflow progress in the selected governed period">
      <div className="space-y-6">
        <Card>
          <div className="grid gap-4 lg:grid-cols-2">
            <CalendarMasterPicker
              kind="municipality-financial-year"
              label="Financial year"
              value={financialYearId}
              selectedLabel={stats.financialYearCode && stats.financialYearName ? `${stats.financialYearCode} · ${stats.financialYearName}` : undefined}
              onChange={value => {
                setFinancialYearId(value);
                setReportingPeriodId('');
              }}
              emptyLabel="Use current financial year"
            />
            <CalendarMasterPicker
              kind="reporting-period"
              label="Reporting period"
              value={reportingPeriodId}
              municipalityFinancialYearId={financialYearId || undefined}
              selectedLabel={stats.reportingPeriodCode && stats.reportingPeriodName ? `${stats.reportingPeriodCode} · ${stats.reportingPeriodName}` : undefined}
              onChange={setReportingPeriodId}
              emptyLabel="Use current actionable period"
              disabled={!financialYearId}
            />
          </div>
          <div className="mt-4 flex flex-wrap items-center gap-2 text-sm text-secondary-600" aria-live="polite">
            {loading ? <><LoadingSpinner size="sm" /> Refreshing governed metrics…</> : null}
            {!loading && stats.reportingWindowState ? <Badge variant={windowBadgeVariant(stats.reportingWindowState)} size="md">{stats.reportingWindowState}</Badge> : null}
            {!loading && stats.reportingPeriodName ? <span>{stats.reportingPeriodName} · {windowDates}</span> : null}
          </div>
        </Card>

        {error ? (
          <Card>
            <EmptyState
              icon={<RefreshCw className="h-5 w-5" />}
              title="IPMS dashboard unavailable"
              description={error}
              action={<Button variant="outline" onClick={() => setRetryRevision(value => value + 1)}>Try again</Button>}
            />
          </Card>
        ) : null}

        {!error && !loading && !stats.municipalityFinancialYearPublicId ? (
          <Card>
            <EmptyState
              icon={<CalendarRange className="h-5 w-5" />}
              title="No active financial year context"
              description="Configure or select an active municipality financial year before using the IPMS dashboard."
            />
          </Card>
        ) : null}

        {!error && stats.municipalityFinancialYearPublicId ? <>
          <section aria-labelledby="ipms-performance-results" className="space-y-3">
            <div>
              <h2 id="ipms-performance-results" className="text-lg font-semibold text-secondary-900 dark:text-white">Performance results</h2>
              <p className="text-sm text-secondary-500">KPI outcomes for {stats.reportingPeriodName ?? 'the selected period'}; each measure states its scoped denominator.</p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              {tile('Assigned KPIs', stats.totalTargets, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/targets', 'assigned')), drilldownDisabled)}
              {tile('Achieved KPIs', stats.completedTargets, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/targets', 'achieved')), drilldownDisabled)}
              {tile('At Risk KPIs', stats.atRiskTargets, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/targets', 'at-risk')), drilldownDisabled)}
              {tile('Outstanding KPIs', stats.outstandingTargets, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/targets', 'outstanding')), drilldownDisabled)}
            </div>
          </section>

          <section aria-labelledby="ipms-workflow-progress" className="space-y-3">
            <div>
              <h2 id="ipms-workflow-progress" className="text-lg font-semibold text-secondary-900 dark:text-white">Workflow progress</h2>
              <p className="text-sm text-secondary-500">Submission and review states are shown separately from performance achievement.</p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              {tile('Draft submissions', stats.draftSubmissions, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/submissions', 'draft')), drilldownDisabled)}
              {tile('Submitted', stats.submittedSubmissions, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/submissions', 'submitted')), drilldownDisabled)}
              {tile('Returned', stats.returnedSubmissions, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/submissions', 'returned')), drilldownDisabled)}
              {tile('Approved', stats.approvedSubmissions, stats.totalTargets, () => setCurrentPath(drilldownPath('/ipms/submissions', 'approved')), drilldownDisabled)}
            </div>
          </section>

          <div className="grid gap-4 lg:grid-cols-3">
            <Card>
              <div className="flex items-center gap-2">
                <BarChart3 className="h-5 w-5 text-primary-600" />
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Recorded stage ratings</h3>
              </div>
              <div className="mt-4 space-y-3">
                {stats.ratingBreakdown?.length ? stats.ratingBreakdown.map(item => (
                  <div key={item.label} className="flex items-center justify-between gap-3 text-sm">
                    <span className="text-secondary-700 dark:text-secondary-300">{item.label}</span>
                    <Badge variant="primary" size="md">{item.count}</Badge>
                  </div>
                )) : <p className="text-sm text-secondary-500">No human stage ratings are recorded in this scope.</p>}
              </div>
            </Card>

            <Card>
              <div className="flex items-center gap-2">
                <Layers className="h-5 w-5 text-primary-600" />
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Team performance</h3>
              </div>
              <div className="mt-4 space-y-4">
                {stats.teamBreakdown?.length ? stats.teamBreakdown.map(item => (
                  <div key={item.departmentPublicId ?? item.departmentName} className="space-y-1">
                    <div className="flex items-center justify-between gap-3 text-sm">
                      <span className="font-medium text-secondary-800 dark:text-secondary-200">{item.departmentName}</span>
                      <span className="text-secondary-500">{item.achievedCount} of {item.targetCount} achieved</span>
                    </div>
                    <ProgressBar value={item.achievedCount} max={Math.max(item.targetCount, 1)} color="success" showLabel />
                    {item.atRiskCount ? <p className="text-xs text-warning-700">{item.atRiskCount} at risk</p> : null}
                  </div>
                )) : <p className="text-sm text-secondary-500">No scoped team targets are available.</p>}
              </div>
            </Card>

            <Card>
              <div className="flex items-center gap-2">
                <ClipboardList className="h-5 w-5 text-primary-600" />
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Review queue</h3>
              </div>
              <dl className="mt-4 space-y-3 text-sm">
                <div className="flex items-center justify-between"><dt className="text-secondary-600">Pending verification</dt><dd className="font-semibold">{stats.pendingVerification}</dd></div>
                <div className="flex items-center justify-between"><dt className="text-secondary-600">Pending approval</dt><dd className="font-semibold">{stats.pendingApproval}</dd></div>
              </dl>
            </Card>
          </div>

          <Card>
            <div className="flex items-center gap-2">
              <CalendarRange className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Financial year periods</h3>
            </div>
            <p className="mt-1 text-sm text-secondary-500">Select a period to reload every dashboard measure from the same authorised population.</p>
            <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {stats.periodBreakdown?.map(period => (
                <Button
                  key={period.reportingPeriodPublicId}
                  variant={period.reportingPeriodPublicId === reportingPeriodId ? 'secondary' : 'outline'}
                  className="min-h-24 flex-col items-start p-3 text-left"
                  onClick={() => setReportingPeriodId(period.reportingPeriodPublicId)}
                >
                  <span className="flex w-full items-center justify-between gap-2">
                    <span className="font-semibold">{period.code} · {period.name}</span>
                    <Badge variant={windowBadgeVariant(period.windowState)}>{period.windowState}</Badge>
                  </span>
                  <span className="text-xs font-normal text-secondary-500">{period.submissionCount} submissions · {period.outstandingCount} outstanding</span>
                </Button>
              ))}
            </div>
          </Card>

          <Card>
            <div className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-primary-600" />
              <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Quick links</h3>
            </div>
            <div className="mt-4 grid gap-3 sm:grid-cols-3">
              {quickLinks.map(link => (
                <Button key={link.path} variant="outline" onClick={() => setCurrentPath(link.path)}>{link.label}</Button>
              ))}
            </div>
          </Card>

          <span className="sr-only">Achieved {achievedRate.toFixed(1)} percent; at risk {atRiskRate.toFixed(1)} percent; outstanding {outstandingRate.toFixed(1)} percent.</span>
        </> : null}
      </div>
    </AppShell>
  );
}
