import { useCallback, useEffect, useState } from 'react';
import { Activity, AlertTriangle, Clock3, Gauge, HeartPulse, RefreshCw } from 'lucide-react';
import { getSystemOperationsOverview } from '../../api/api';
import type { SystemOperationsOverview } from '../../types';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, StatCard } from '../ui';

const statusVariant = (status: string): 'success' | 'warning' | 'error' =>
  status === 'Healthy' ? 'success' : status === 'Degraded' ? 'warning' : 'error';

const duration = (seconds: number) => {
  const days = Math.floor(seconds / 86_400);
  const hours = Math.floor((seconds % 86_400) / 3_600);
  const minutes = Math.floor((seconds % 3_600) / 60);
  return [days ? `${days}d` : '', hours ? `${hours}h` : '', `${minutes}m`].filter(Boolean).join(' ');
};

export function SystemOperationsPage() {
  const [overview, setOverview] = useState<SystemOperationsOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    const result = await getSystemOperationsOverview();
    if (result.success && result.data) {
      setOverview(result.data);
      setError(null);
    } else {
      setOverview(null);
      setError(result.message ?? 'Operational readiness could not be loaded.');
    }
    setLoading(false);
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <AppShell title="System Operations" subtitle="Live application readiness, request telemetry, and alert thresholds">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-secondary-500 dark:text-secondary-400">
          Sensitive provider details and connection information are intentionally excluded.
        </p>
        <Button variant="outline" loading={loading} icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()}>
          Refresh status
        </Button>
      </div>

      {loading && !overview ? (
        <Card><div role="status" aria-live="polite" className="py-8 text-center text-sm text-secondary-500">Loading operational readiness…</div></Card>
      ) : error ? (
        <Card><div role="alert" className="flex items-center gap-2 py-4 text-sm text-error-600"><AlertTriangle className="h-4 w-4" />{error}</div></Card>
      ) : overview ? (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard title="Readiness" value={overview.status} icon={<HeartPulse className="h-5 w-5 text-primary-700" />} />
            <StatCard title="Application uptime" value={duration(overview.uptimeSeconds)} icon={<Clock3 className="h-5 w-5 text-primary-700" />} />
            <StatCard title="Completed requests" value={overview.requests.completed.toLocaleString()} icon={<Activity className="h-5 w-5 text-primary-700" />} />
            <StatCard title="Average response" value={`${overview.requests.averageDurationMilliseconds.toFixed(2)} ms`} icon={<Gauge className="h-5 w-5 text-primary-700" />} />
          </div>

          <div className="grid gap-4 xl:grid-cols-[2fr_1fr]">
            <Card padding="none" className="overflow-hidden">
              <div className="flex items-center justify-between border-b border-secondary-200 px-4 py-3 dark:border-secondary-700">
                <h2 className="font-semibold text-secondary-900 dark:text-white">Readiness checks</h2>
                <Badge variant={statusVariant(overview.status)}>{overview.status}</Badge>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-secondary-200 dark:divide-secondary-700">
                  <thead className="bg-secondary-50 dark:bg-secondary-800/60"><tr>{['Check', 'Status', 'Duration'].map(label => <th key={label} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-secondary-500">{label}</th>)}</tr></thead>
                  <tbody className="divide-y divide-secondary-200 dark:divide-secondary-700">
                    {overview.checks.map(check => <tr key={check.name}>
                      <td className="px-4 py-3 text-sm font-medium text-secondary-900 dark:text-white">{check.name}</td>
                      <td className="px-4 py-3"><Badge variant={statusVariant(check.status)}>{check.status}</Badge></td>
                      <td className="px-4 py-3 text-sm text-secondary-600 dark:text-secondary-300">{check.durationMilliseconds.toFixed(2)} ms</td>
                    </tr>)}
                  </tbody>
                </table>
              </div>
            </Card>

            <div className="space-y-4">
              <Card>
                <h2 className="mb-3 font-semibold text-secondary-900 dark:text-white">Request telemetry</h2>
                <dl className="space-y-2 text-sm">
                  {[['Started', overview.requests.started], ['Active', overview.requests.active], ['Failed', overview.requests.failed], ['Failure rate', `${overview.requests.failureRatePercent.toFixed(2)}%`]].map(([label, value]) =>
                    <div key={label} className="flex justify-between gap-4"><dt className="text-secondary-500">{label}</dt><dd className="font-medium text-secondary-900 dark:text-white">{value}</dd></div>)}
                </dl>
              </Card>
              <Card>
                <h2 className="mb-3 font-semibold text-secondary-900 dark:text-white">Configured alerts</h2>
                <dl className="space-y-2 text-sm">
                  <div className="flex justify-between gap-4"><dt className="text-secondary-500">Minimum sample</dt><dd className="font-medium text-secondary-900 dark:text-white">{overview.alertThresholds.minimumRequests} requests</dd></div>
                  <div className="flex justify-between gap-4"><dt className="text-secondary-500">Failure rate</dt><dd className="font-medium text-secondary-900 dark:text-white">{overview.alertThresholds.failureRatePercent}%</dd></div>
                  <div className="flex justify-between gap-4"><dt className="text-secondary-500">Average duration</dt><dd className="font-medium text-secondary-900 dark:text-white">{overview.alertThresholds.averageDurationMilliseconds} ms</dd></div>
                </dl>
              </Card>
            </div>
          </div>
          <p className="text-xs text-secondary-500">Last checked {new Date(overview.generatedAt).toLocaleString()} · Full check completed in {overview.totalHealthCheckDurationMilliseconds.toFixed(2)} ms</p>
        </div>
      ) : null}
    </AppShell>
  );
}
