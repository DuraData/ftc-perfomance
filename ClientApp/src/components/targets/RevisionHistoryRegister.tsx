import { useCallback, useEffect, useRef, useState } from 'react';
import {
  getIpmsTargetFieldRevisionsPage,
  getIpmsTargetOrderingRevisionsPage,
  getOpmsTargetFieldRevisionsPage,
  getOpmsTargetOrderingRevisionsPage,
  getPerformanceTargetRevisionsPage,
} from '../../api/api';
import type { KpiFieldRevisionDto } from '../../types';
import { useSecurity } from '../../context/SecurityContext';
import { Button } from '../ui';

type Props = {
  source: 'definition' | 'ordering' | 'period';
  kind: 'opms' | 'ipms';
  parentId: string;
  title: string;
  emptyMessage: string;
  refreshKey?: number;
  compact?: boolean;
};

export function RevisionHistoryRegister({ source, kind, parentId, title, emptyMessage, refreshKey = 0, compact = false }: Props) {
  const security = useSecurity();
  const resource = kind === 'opms' ? 'OPMS_KPI' : 'IPMS_KPI';
  const canReadOriginal = security.canReadField(resource, 'RevisionOriginalValue');
  const canReadRevised = security.canReadField(resource, 'RevisionRevisedValue');
  const canReadReason = security.canReadField(resource, 'RevisionReason');
  const canReadApproval = security.canReadField(resource, 'RevisionApprovalReference');
  const canReadActor = security.canReadField(resource, 'RevisionActor');
  const [items, setItems] = useState<KpiFieldRevisionDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const requestSequence = useRef(0);

  useEffect(() => {
    const normalized = searchInput.trim();
    if (normalized === search) return;
    const timeout = window.setTimeout(() => { setPage(1); setSearch(normalized); }, 300);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput]);

  const load = useCallback(async () => {
    const requestId = ++requestSequence.current;
    setBusy(true);
    setError('');
    const query = { page, pageSize: 10, search, sortBy: 'recordedAt', sortDirection: 'desc' as const };
    const result = source === 'period'
      ? await getPerformanceTargetRevisionsPage(parentId, query)
      : source === 'definition'
        ? await (kind === 'opms' ? getOpmsTargetFieldRevisionsPage : getIpmsTargetFieldRevisionsPage)(parentId, query)
        : await (kind === 'opms' ? getOpmsTargetOrderingRevisionsPage : getIpmsTargetOrderingRevisionsPage)(parentId, query);
    if (requestId !== requestSequence.current) return;
    if (result.success && result.data) {
      setItems(result.data.items);
      setTotalCount(result.data.totalCount);
      setTotalPages(result.data.totalPages);
    } else {
      setItems([]);
      setTotalCount(0);
      setTotalPages(0);
      setError(result.message ?? 'Revision history could not be loaded.');
    }
    setBusy(false);
  }, [kind, page, parentId, search, source]);

  useEffect(() => {
    void refreshKey;
    void load();
    return () => { requestSequence.current += 1; };
  }, [load, refreshKey]);

  return <div className={compact ? 'mt-4' : 'mt-5'}>
    <div className="flex flex-wrap items-end justify-between gap-2">
      <div><h3 className="text-sm font-medium">{title}</h3><p className="text-xs text-secondary-500">{totalCount} immutable revision{totalCount === 1 ? '' : 's'}</p></div>
      <input aria-label={`Search ${title.toLowerCase()}`} className="min-h-9 rounded border border-secondary-200 bg-white px-2 text-xs dark:border-secondary-700 dark:bg-secondary-800" placeholder="Search revisions" value={searchInput} onChange={event => setSearchInput(event.target.value)} />
    </div>
    {error && <p role="alert" className="mt-2 text-xs text-danger-600">{error}</p>}
    {busy && <p className="mt-2 text-xs text-secondary-500">Loading revision history…</p>}
    {!busy && !error && items.length === 0 ? <p className="mt-2 text-xs text-secondary-500">{emptyMessage}</p> : <ul className="mt-2 space-y-1 text-xs text-secondary-600">{items.map(item => <li key={item.publicId} className={compact ? '' : 'rounded border border-secondary-200 p-2 dark:border-secondary-700'}>
      {new Date(item.recordedAt).toLocaleString()} · <strong>{item.fieldName}</strong>
      {canReadOriginal && <>: {item.originalValue ?? '—'}</>}{canReadRevised && <> → {item.revisedValue ?? '—'}</>}
      {canReadApproval && item.approvalReference && <> · {item.approvalReference}</>}
      {canReadActor && item.revisedByName && <> · {item.revisedByName}{item.revisedByUserPublicId ? ` (${item.revisedByUserPublicId})` : ''}</>}
      {!compact && <div className="text-secondary-500">{canReadReason && item.reason ? <>{item.reason} · </> : null}effective {new Date(item.effectiveAt).toLocaleString()}</div>}
    </li>)}</ul>}
    {totalPages > 1 && <div className="mt-3 flex items-center justify-between gap-2 text-xs"><Button size="sm" variant="outline" aria-label={`Previous ${title.toLowerCase()}`} disabled={page <= 1 || busy} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous</Button><span>Page {page} of {totalPages}</span><Button size="sm" variant="outline" aria-label={`Next ${title.toLowerCase()}`} disabled={page >= totalPages || busy} onClick={() => setPage(value => value + 1)}>Next</Button></div>}
  </div>;
}
