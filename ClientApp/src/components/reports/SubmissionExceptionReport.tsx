import { useCallback, useEffect, useState } from 'react';
import { AlertTriangle, RefreshCw, RotateCcw, Search } from 'lucide-react';
import { getIpmsSubmissionsPage, getOpmsSubmissionsPage } from '../../api/api';
import type { IPMSSubmission, OPMSSubmission } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { Select } from '../common/Form';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { useApp } from '../../context/AppContext';

type SubmissionExceptionReportProps = { mode: 'returned' | 'overdue' };
type SubmissionRow = OPMSSubmission | IPMSSubmission;

const formatDate = (value?: string) => {
  if (!value) return 'Not set';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? 'Not set' : parsed.toLocaleDateString();
};

const statusLabel = (value: string) => value.replace(/_/g, ' ').replace(/\b\w/g, (character: string) => character.toUpperCase());

export function SubmissionExceptionReport({ mode }: SubmissionExceptionReportProps) {
  const { currentFinancialYearPublicId, switchFinancialYear } = useApp();
  const [kind, setKind] = useState<1 | 2>(1);
  const [yearId, setYearId] = useState(currentFinancialYearPublicId ?? '');
  const [periodId, setPeriodId] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<SubmissionRow[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const title = mode === 'returned' ? 'Returned Submissions' : 'Overdue Submissions';
  const subtitle = mode === 'returned'
    ? 'Tenant- and scope-filtered submissions returned for information or correction'
    : 'Tenant- and scope-filtered open submissions past their governed due date';
  const Icon = mode === 'returned' ? RotateCcw : AlertTriangle;

  const load = useCallback(async () => {
    if (!periodId) {
      setRows([]); setTotalCount(0); setTotalPages(0); setError(null);
      return;
    }
    setLoading(true); setError(null);
    const query = {
      page, pageSize: 25, search, sortBy: mode === 'overdue' ? 'createdAt' : 'status', sortDirection: 'desc' as const,
      municipalityFinancialYearPublicId: yearId || undefined,
      reportingPeriodPublicId: periodId,
      dashboardFilter: mode,
    };
    const result = kind === 1 ? await getOpmsSubmissionsPage(query) : await getIpmsSubmissionsPage(query);
    if (!result.success || !result.data) {
      setRows([]); setTotalCount(0); setTotalPages(0);
      setError(result.message ?? `${title} could not be loaded.`);
    } else {
      setRows(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
    }
    setLoading(false);
  }, [kind, mode, page, periodId, search, title, yearId]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  return <AppShell title={title} subtitle={subtitle}>
    <div className="space-y-5">
      <Card className="p-4">
        <div className="grid items-end gap-3 md:grid-cols-4">
          <Select label="Performance framework" value={kind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPage(1); }} />
          <CalendarMasterPicker kind="municipality-financial-year" label="Financial year" value={yearId} onChange={value => { setYearId(value); if (value) switchFinancialYear(value); setPeriodId(''); setPage(1); }} />
          <CalendarMasterPicker kind="reporting-period" label="Reporting period" value={periodId} municipalityFinancialYearId={yearId || undefined} emptyLabel="Select reporting period" required onChange={value => { setPeriodId(value); setPage(1); }} />
          <Button variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={loading || !periodId}>Refresh</Button>
        </div>
      </Card>

      <Card className="p-4">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Icon className="h-5 w-5 text-primary-600" />
            <div><h2 className="font-semibold">{title} register</h2><p className="text-xs text-secondary-500">{totalCount} matching submissions</p></div>
          </div>
          <label className="relative min-w-[16rem]">
            <span className="sr-only">Search {title.toLowerCase()}</span>
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-secondary-400" />
            <input aria-label={`Search ${title.toLowerCase()}`} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder="Search KPI, target, period or status" className="w-full rounded-lg border border-secondary-300 bg-white py-2 pl-9 pr-3 text-sm dark:border-secondary-700 dark:bg-secondary-900" />
          </label>
        </div>

        {!periodId && <div role="status" className="rounded-lg border border-dashed border-secondary-300 p-8 text-center text-sm text-secondary-600 dark:border-secondary-700 dark:text-secondary-300">Select a financial year and reporting period to load this governed register.</div>}
        {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
        {periodId && loading && <div role="status" className="p-8 text-center text-sm text-secondary-500">Loading {title.toLowerCase()}…</div>}
        {periodId && !loading && !error && rows.length === 0 && <div role="status" className="rounded-lg border border-dashed border-secondary-300 p-8 text-center text-sm text-secondary-600 dark:border-secondary-700 dark:text-secondary-300">No {title.toLowerCase()} match this scope.</div>}
        {rows.length > 0 && <div className="overflow-x-auto">
          <table aria-label={`${title} register`} className="min-w-full text-left text-sm">
            <thead><tr className="border-b border-secondary-200 text-xs text-secondary-500 dark:border-secondary-700"><th scope="col" className="px-2 py-2">KPI</th><th scope="col" className="px-2 py-2">Target</th><th scope="col" className="px-2 py-2">Period</th><th scope="col" className="px-2 py-2">Due date</th><th scope="col" className="px-2 py-2">Status</th><th scope="col" className="px-2 py-2">Achievement</th></tr></thead>
            <tbody>{rows.map(row => <tr key={row.id} className="border-b border-secondary-100 dark:border-secondary-800"><td className="px-2 py-3 font-medium">{row.target.indicatorNumber}</td><td className="px-2 py-3">{row.target.targetName}</td><td className="px-2 py-3">{row.quarter}</td><td className="px-2 py-3">{formatDate(row.extendedDueDate || row.dueDate)}{row.extendedDueDate && <div className="text-xs text-secondary-500">Extended</div>}</td><td className="px-2 py-3"><Badge variant={mode === 'overdue' ? 'error' : 'warning'}>{statusLabel(row.status)}</Badge></td><td className="px-2 py-3">{row.achievementPercent == null ? '—' : `${row.achievementPercent.toFixed(1)}%`}</td></tr>)}</tbody>
          </table>
        </div>}
        {periodId && totalPages > 1 && <div className="mt-4 flex items-center justify-between text-sm"><span>Page {page} of {totalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1 || loading} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={page >= totalPages || loading} onClick={() => setPage(value => value + 1)}>Next</Button></div></div>}
      </Card>
    </div>
  </AppShell>;
}
