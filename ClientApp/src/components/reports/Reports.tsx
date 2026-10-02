import { useCallback, useEffect, useMemo, useState } from 'react';
import { BarChart3, Download, RefreshCw, Target, TrendingUp } from 'lucide-react';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Select } from '../common/Form';
import { downloadPerformanceReportCsv, getPerformanceReportSummary, getReportingPeriodMasters } from '../../api/api';
import type { PerformanceReportSummaryDto, ReportingPeriodMasterDto } from '../../types';
import { useApp } from '../../context/AppContext';

export function Reports() {
  const { permissions, pushToast } = useApp();
  const [kind, setKind] = useState<1 | 2>(1);
  const [periodId, setPeriodId] = useState('');
  const [periods, setPeriods] = useState<ReportingPeriodMasterDto[]>([]);
  const [summary, setSummary] = useState<PerformanceReportSummaryDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const permissionSet = useMemo(() => new Set(permissions.map(value => value.toUpperCase())), [permissions]);
  const canExport = permissionSet.has(kind === 1 ? 'OPMS_REPORT.EXPORT' : 'IPMS_REPORT.EXPORT') || permissionSet.has('REPORTS.EXPORT');

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [periodResult, summaryResult] = await Promise.all([getReportingPeriodMasters(), getPerformanceReportSummary(kind, periodId || undefined)]);
    setPeriods(periodResult.data ?? []);
    if (!summaryResult.success || !summaryResult.data) { setSummary(null); setError(summaryResult.message ?? 'Report could not be generated.'); }
    else setSummary(summaryResult.data);
    if (!periodResult.success) setError(periodResult.message ?? 'Reporting periods could not be loaded.');
    setBusy(false);
  }, [kind, periodId]);

  useEffect(() => { void load(); }, [load]);

  const exportCsv = async () => {
    setBusy(true); setError(null);
    const result = await downloadPerformanceReportCsv(kind, periodId || undefined);
    if (!result.success) setError(result.message ?? 'CSV export failed.');
    else pushToast('success', 'Performance CSV downloaded');
    setBusy(false);
  };

  const metrics = summary ? [
    { label: 'Configured targets', value: summary.targetCount, tone: 'primary' as const },
    { label: 'Submissions', value: summary.submissionCount, tone: 'default' as const },
    { label: 'Achieved', value: summary.achievedCount, tone: 'success' as const },
    { label: 'At risk', value: summary.atRiskCount, tone: 'error' as const },
    { label: 'Pending result', value: summary.pendingCount, tone: 'warning' as const },
    { label: 'Average achievement', value: summary.averageAchievementPercent == null ? '—' : `${summary.averageAchievementPercent.toFixed(1)}%`, tone: 'primary' as const },
  ] : [];

  return (
    <AppShell title="Performance Reports" subtitle="Tenant-scoped server analytics">
      <div className="space-y-5">
        <Card className="p-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div className="grid min-w-[20rem] flex-1 gap-3 sm:grid-cols-2">
              <Select label="Performance framework" value={kind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPeriodId(''); }} />
              <Select label="Reporting period" value={periodId} options={[{ value: '', label: 'All periods' }, ...periods.map(period => ({ value: period.publicId, label: `${period.code} · ${period.name}` }))]} onChange={event => setPeriodId(event.target.value)} />
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
              <Button size="sm" variant="primary" icon={<Download className="h-4 w-4" />} onClick={() => void exportCsv()} disabled={busy || !canExport}>Export CSV</Button>
            </div>
          </div>
        </Card>

        {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">{error}</div>}

        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
          {metrics.map(metric => <Card key={metric.label} className="p-4"><Badge variant={metric.tone}>{metric.label}</Badge><p className="mt-3 text-2xl font-bold text-secondary-900 dark:text-white">{metric.value}</p></Card>)}
        </div>

        <div className="grid gap-5 xl:grid-cols-[1.3fr_0.7fr]">
          <Card className="p-4">
            <div className="mb-4 flex items-center gap-2"><TrendingUp className="h-5 w-5 text-primary-600" /><h2 className="font-semibold text-secondary-900 dark:text-white">Achievement by department</h2></div>
            <div className="h-80">
              {summary?.departments.length ? <ResponsiveContainer width="100%" height="100%"><BarChart data={summary.departments}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="department" tick={{ fontSize: 10 }} /><YAxis tick={{ fontSize: 10 }} /><Tooltip /><Bar name="Average achievement %" dataKey="averageAchievementPercent" fill="#3b82f6" radius={[4, 4, 0, 0]} /></BarChart></ResponsiveContainer> : <div className="flex h-full items-center justify-center text-sm text-secondary-500">{busy ? 'Generating report…' : 'No scoped performance data for this selection.'}</div>}
            </div>
          </Card>
          <Card className="p-4">
            <div className="mb-4 flex items-center gap-2"><BarChart3 className="h-5 w-5 text-primary-600" /><h2 className="font-semibold text-secondary-900 dark:text-white">Department detail</h2></div>
            <div className="space-y-2">{summary?.departments.map(row => <div key={row.department} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex items-center justify-between"><p className="font-medium text-secondary-900 dark:text-white">{row.department}</p><Badge variant="default">{row.submissionCount} submissions</Badge></div><div className="mt-2 flex items-center justify-between text-xs text-secondary-500"><span>{row.achievedCount} achieved</span><span>{row.averageAchievementPercent == null ? 'No score' : `${row.averageAchievementPercent.toFixed(1)}% average`}</span></div></div>)}</div>
          </Card>
        </div>

        <div className="flex items-center gap-2 text-xs text-secondary-500"><Target className="h-4 w-4" /><span>Generated from tenant-filtered canonical targets and submissions{summary ? ` at ${new Date(summary.generatedAt).toLocaleString()}` : ''}.</span></div>
      </div>
    </AppShell>
  );
}
