import { useEffect, useState } from 'react';
import { MessageCircleQuestion, RefreshCw } from 'lucide-react';
import { getRfiQueue } from '../../api/api';
import { useApp } from '../../context/AppContext';
import type { RfiQueueCountsDto, RfiQueueFilters, RfiQueueItemDto } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { DataTable } from '../common/DataTable';
import { Input, Select } from '../common/Form';
import { Modal } from '../common/Modal';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';
import { PerformanceRfiWorkspace } from './PerformanceRfiWorkspace';

type RfiQueueFilterState = Required<RfiQueueFilters>;

const defaults: RfiQueueFilterState = {
  search: '', municipalityFinancialYearPublicId: '', kind: 'all', period: '', status: 'all', sortBy: 'dueAt', sortDirection: 'asc',
};
const kinds = ['all', 'opms', 'ipms'] as const;
const periods = ['', 'Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'] as const;
const statuses = ['all', 'open', 'responded', 'overdue', 'closed'] as const;
const sortFields = ['raisedAt', 'dueAt', 'targetName', 'indicatorNumber', 'period', 'status'] as const;
const directions = ['asc', 'desc'] as const;
const publicIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const storageKeyPrefix = 'workflow-rfi-queue-filters';
const emptyCounts: RfiQueueCountsDto = { open: 0, responded: 0, overdue: 0, closed: 0 };

function initialFilters(storageKey: string, activeFinancialYearPublicId?: string | null): RfiQueueFilterState {
  try {
    const stored = JSON.parse(sessionStorage.getItem(storageKey) ?? '{}') as Partial<RfiQueueFilterState>;
    const parameters = new URLSearchParams(window.location.search);
    const linkedKind = parameters.get('kind');
    const linkedStatus = parameters.get('status');
    const linkedYear = parameters.get('municipalityFinancialYearPublicId');
    return {
      search: typeof stored.search === 'string' ? stored.search.slice(0, 200) : '',
      municipalityFinancialYearPublicId: linkedYear && publicIdPattern.test(linkedYear)
        ? linkedYear
        : activeFinancialYearPublicId && publicIdPattern.test(activeFinancialYearPublicId)
          ? activeFinancialYearPublicId
          : typeof stored.municipalityFinancialYearPublicId === 'string' && publicIdPattern.test(stored.municipalityFinancialYearPublicId)
            ? stored.municipalityFinancialYearPublicId
            : '',
      kind: kinds.includes(linkedKind as RfiQueueFilterState['kind'])
        ? linkedKind as RfiQueueFilterState['kind']
        : kinds.includes(stored.kind as RfiQueueFilterState['kind']) ? stored.kind as RfiQueueFilterState['kind'] : 'all',
      period: periods.includes(stored.period as typeof periods[number]) ? stored.period as RfiQueueFilterState['period'] : '',
      status: statuses.includes(linkedStatus as RfiQueueFilterState['status'])
        ? linkedStatus as RfiQueueFilterState['status']
        : statuses.includes(stored.status as RfiQueueFilterState['status']) ? stored.status as RfiQueueFilterState['status'] : 'all',
      sortBy: sortFields.includes(stored.sortBy as RfiQueueFilterState['sortBy']) ? stored.sortBy as RfiQueueFilterState['sortBy'] : 'dueAt',
      sortDirection: directions.includes(stored.sortDirection as RfiQueueFilterState['sortDirection']) ? stored.sortDirection as RfiQueueFilterState['sortDirection'] : 'asc',
    };
  } catch {
    return { ...defaults };
  }
}

function statusBadge(status: RfiQueueItemDto['status']) {
  const labels = { open: 'Open', responded: 'Responded', overdue: 'Overdue', closed: 'Closed' };
  const variants = { open: 'warning', responded: 'info', overdue: 'error', closed: 'success' } as const;
  return <Badge variant={variants[status]}>{labels[status]}</Badge>;
}

export function RfiQueue() {
  const { currentMunicipalityPublicId, currentFinancialYearPublicId, pushToast, userProfile } = useApp();
  const storageKey = `${storageKeyPrefix}:${userProfile?.publicId ?? 'unknown'}:${currentMunicipalityPublicId ?? 'system'}`;
  const [filters, setFilters] = useState<RfiQueueFilterState>(() => initialFilters(storageKey, currentFinancialYearPublicId));
  const [items, setItems] = useState<RfiQueueItemDto[]>([]);
  const [counts, setCounts] = useState<RfiQueueCountsDto>(emptyCounts);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [selected, setSelected] = useState<RfiQueueItemDto | null>(null);
  const [reload, setReload] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    try { sessionStorage.setItem(storageKey, JSON.stringify(filters)); } catch { /* Storage can be unavailable in hardened browsers. */ }
  }, [filters, storageKey]);
  useEffect(() => {
    let cancelled = false;
    setLoading(true); setError('');
    void getRfiQueue(page, 25, filters).then(result => {
      if (cancelled) return;
      if (!result.success || !result.data) {
        const message = result.message ?? 'The RFI queue could not be loaded.';
        setError(message); setItems([]); setCounts(emptyCounts); setTotalCount(0); setTotalPages(0); pushToast('error', message); return;
      }
      setItems(result.data.page.items);
      setCounts(result.data.counts);
      setTotalCount(result.data.page.totalCount);
      setTotalPages(result.data.page.totalPages);
    }).catch(() => {
      if (cancelled) return;
      const message = 'The RFI queue service is unavailable.';
      setError(message); setItems([]); setCounts(emptyCounts); setTotalCount(0); setTotalPages(0); pushToast('error', message);
    }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [filters, page, pushToast, reload]);

  const change = (next: Partial<RfiQueueFilterState>) => { setPage(1); setFilters(current => ({ ...current, ...next })); };
  const reset = () => { setPage(1); setFilters({ ...defaults }); };
  const columns = [
    { id: 'target', header: 'KPI', accessor: (row: RfiQueueItemDto) => <div><p className="font-medium text-secondary-900 dark:text-white">{row.kind === 'ipms' ? '[IPMS] ' : ''}{row.targetName}</p><p className="text-[10px] text-secondary-500">{row.indicatorNumber}</p></div> },
    { id: 'period', header: 'Period', accessor: (row: RfiQueueItemDto) => row.period },
    { id: 'question', header: 'Question', accessor: (row: RfiQueueItemDto) => row.question ?? 'Protected RFI question' },
    { id: 'due', header: 'Response due', accessor: (row: RfiQueueItemDto) => new Date(row.responseDueAt).toLocaleString() },
    { id: 'status', header: 'Status', accessor: (row: RfiQueueItemDto) => statusBadge(row.status) },
    { id: 'readiness', header: 'Readiness', accessor: (row: RfiQueueItemDto) => row.canRespond ? 'Ready to respond' : row.canClose ? 'Ready to close' : 'View only' },
  ];

  return <AppShell title="RFI Queue" subtitle="Respond to and close authorised OPMS and IPMS requests for information">
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
        {([['Open', counts.open], ['Responded', counts.responded], ['Overdue', counts.overdue], ['Closed', counts.closed]] as const).map(([label, value]) => <Card key={label} className="p-3"><p className="text-lg font-bold text-secondary-900 dark:text-white">{value}</p><p className="text-xs text-secondary-500">{label}</p></Card>)}
      </div>
      <Card>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-7">
          <Input label="Search KPI or indicator" value={filters.search} maxLength={200} onChange={event => change({ search: event.target.value })} />
          <CalendarMasterPicker kind="municipality-financial-year" label="Financial year" emptyLabel="All active years" value={filters.municipalityFinancialYearPublicId} onChange={value => change({ municipalityFinancialYearPublicId: value })} />
          <Select label="Framework" value={filters.kind} onChange={event => change({ kind: event.target.value as RfiQueueFilterState['kind'] })} options={[{ value: 'all', label: 'All frameworks' }, { value: 'opms', label: 'OPMS' }, { value: 'ipms', label: 'IPMS' }]} />
          <Select label="Period" value={filters.period} onChange={event => change({ period: event.target.value as RfiQueueFilterState['period'] })} options={[{ value: '', label: 'All periods' }, { value: 'Q1', label: 'Quarter 1' }, { value: 'Q2', label: 'Quarter 2' }, { value: 'Mid-Year', label: 'Mid-Term' }, { value: 'Q3', label: 'Quarter 3' }, { value: 'Q4', label: 'Quarter 4' }, { value: 'Annual', label: 'Annual' }]} />
          <Select label="RFI status" value={filters.status} onChange={event => change({ status: event.target.value as RfiQueueFilterState['status'] })} options={[{ value: 'all', label: 'All statuses' }, { value: 'open', label: 'Open' }, { value: 'responded', label: 'Responded' }, { value: 'overdue', label: 'Overdue' }, { value: 'closed', label: 'Closed' }]} />
          <Select label="Sort by" value={filters.sortBy} onChange={event => change({ sortBy: event.target.value as RfiQueueFilterState['sortBy'] })} options={[{ value: 'dueAt', label: 'Response due' }, { value: 'raisedAt', label: 'Raised at' }, { value: 'targetName', label: 'KPI name' }, { value: 'indicatorNumber', label: 'Indicator number' }, { value: 'period', label: 'Period' }, { value: 'status', label: 'Status' }]} />
          <div className="flex items-end gap-2"><Select label="Direction" value={filters.sortDirection} onChange={event => change({ sortDirection: event.target.value as RfiQueueFilterState['sortDirection'] })} options={[{ value: 'asc', label: 'Ascending' }, { value: 'desc', label: 'Descending' }]} /><Button type="button" variant="outline" size="sm" onClick={reset}>Reset</Button></div>
        </div>
      </Card>
      {loading && <Card><p className="py-6 text-center text-sm text-secondary-500">Loading authorised RFIs…</p></Card>}
      {!loading && error && <EmptyState icon={<MessageCircleQuestion className="h-6 w-6" />} title="RFI queue unavailable" description={error} />}
      {!loading && !error && <Card>
        <div className="mb-3 flex items-center justify-between gap-3"><h2 className="text-sm font-semibold text-secondary-900 dark:text-white">RFI Queue</h2><div className="flex items-center gap-2"><span className="text-xs text-secondary-500">{totalCount} authorised RFI{totalCount === 1 ? '' : 's'}</span><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => setReload(value => value + 1)}>Refresh</Button></div></div>
        <DataTable tableLabel="RFI queue" data={items} columns={columns} onRowClick={setSelected} getRowActionLabel={row => `Open ${row.kind.toUpperCase()} RFI for ${row.targetName}`} emptyMessage="No authorised RFIs match the selected filters" getRowId={row => row.publicId} />
        {totalPages > 1 && <div className="mt-4 flex items-center justify-end gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><span className="text-xs text-secondary-500">Page {page} of {totalPages}</span><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>}
      </Card>}
      <Modal isOpen={!!selected} onClose={() => setSelected(null)} title={selected ? `${selected.period} RFI · ${selected.targetName}` : 'RFI'} size="xl">
        {selected && <PerformanceRfiWorkspace kind={selected.kind === 'opms' ? 1 : 2} submissionId={selected.submissionPublicId} onChanged={() => setReload(value => value + 1)} />}
      </Modal>
    </div>
  </AppShell>;
}
