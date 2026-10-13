import { useCallback, useEffect, useMemo, useState } from 'react';
import { Building2, RefreshCw, ShieldAlert } from 'lucide-react';
import { executeLegacyTenantOwnershipReconciliation, getLegacyTenantOwnershipPreview } from '../../api/api';
import type { LegacyTenantEntityType, LegacyTenantOwnershipPreviewDto, PagedResult } from '../../types';
import { useApp } from '../../context/AppContext';
import { Badge, Button, Card } from '../ui';
import { Select, Textarea } from '../common/Form';

const emptyPage = (page = 1): PagedResult<LegacyTenantOwnershipPreviewDto> => ({ items: [], page, pageSize: 25, totalCount: 0, totalPages: 0 });
const entityOptions: Array<{ value: LegacyTenantEntityType; label: string }> = [
  { value: 1, label: 'Departments' },
  { value: 2, label: 'Units' },
  { value: 3, label: 'OPMS targets' },
  { value: 4, label: 'IPMS targets' },
  { value: 5, label: 'OPMS submissions' },
  { value: 6, label: 'IPMS submissions' },
];

export function LegacyTenantOwnershipReconciliationAdministration() {
  const { permissions, pushToast } = useApp();
  const canReconcile = useMemo(() => permissions.some(code => code.toLowerCase() === 'security.reconcile_tenant_ownership'), [permissions]);
  const [entityType, setEntityType] = useState<LegacyTenantEntityType>(1);
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<LegacyTenantOwnershipPreviewDto>>(emptyPage());
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    const response = await getLegacyTenantOwnershipPreview(entityType, page, 25);
    if (!response.success || !response.data) {
      setError(response.message ?? 'Tenant-ownership preview could not be loaded.');
      setResult(emptyPage(page));
    } else setResult(response.data);
    setBusy(false);
  }, [entityType, page]);

  useEffect(() => { void load(); }, [load]);

  const execute = async (item: LegacyTenantOwnershipPreviewDto) => {
    const normalizedReason = reason.trim();
    if (!canReconcile || item.status !== 'Ready' || normalizedReason.length < 10) {
      setError('A ready record, system reconciliation permission and a governance reason of at least 10 characters are required.');
      return;
    }
    setBusy(true);
    setError(null);
    const response = await executeLegacyTenantOwnershipReconciliation({
      entityType: item.entityType,
      entityPublicId: item.entityPublicId,
      rowVersion: item.rowVersion,
      reason: normalizedReason,
    });
    if (!response.success || !response.data) setError(response.message ?? 'Tenant-ownership reconciliation failed.');
    else {
      pushToast('success', `${item.label} linked to the selected municipality`);
      setReason('');
      await load();
    }
    setBusy(false);
  };

  return <div className="space-y-4">
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex gap-3"><Building2 className="mt-0.5 h-5 w-5 text-primary-600" /><div><h3 className="font-semibold text-secondary-900 dark:text-white">Legacy tenant-root cutover</h3><p className="mt-1 max-w-3xl text-sm text-secondary-600 dark:text-secondary-300">Attach a legacy record to the selected municipality only when its existing department, unit, assignment, target or submitter relationships prove exactly one owner. Missing, foreign and conflicting evidence remains blocked.</p></div></div>
        <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
      </div>
      {!canReconcile && <div role="alert" className="mt-3 rounded-lg border border-warning-200 bg-warning-50 p-3 text-sm text-warning-800">Read-only: system scope and `SECURITY.RECONCILE_TENANT_OWNERSHIP` are required.</div>}
      {error && <div role="alert" className="mt-3 rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="mt-4 grid gap-3 sm:grid-cols-[240px_1fr] sm:items-end">
        <Select label="Legacy register" value={entityType} options={entityOptions} onChange={event => { setEntityType(Number(event.target.value) as LegacyTenantEntityType); setPage(1); }} />
        <Textarea label="Tenant reconciliation reason" value={reason} maxLength={1000} onChange={event => setReason(event.target.value)} placeholder="Approved cutover reference and relationship evidence" />
      </div>
    </Card>

    <Card className="overflow-hidden">
      <div className="border-b border-secondary-200 p-4 dark:border-secondary-700"><h3 className="font-semibold">Unowned legacy records</h3><p className="text-xs text-secondary-500">{result.totalCount} record{result.totalCount === 1 ? '' : 's'} · page {result.page} of {Math.max(1, result.totalPages)}</p></div>
      <div className="divide-y divide-secondary-200 dark:divide-secondary-700">
        {result.items.map(item => <div key={item.entityPublicId} className="grid gap-3 p-4 md:grid-cols-[1fr_auto] md:items-center">
          <div><div className="flex flex-wrap items-center gap-2"><p className="font-medium text-secondary-900 dark:text-white">{item.label}</p><Badge variant={item.status === 'Ready' ? 'warning' : 'error'}>{item.status}</Badge></div><p className={`mt-1 text-sm ${item.status === 'Ready' ? 'text-secondary-600 dark:text-secondary-300' : 'text-error-700'}`}>{item.status !== 'Ready' && <ShieldAlert className="mr-1 inline h-4 w-4" />}{item.message}</p>{item.sourceEvidence && <p className="mt-1 text-xs text-secondary-500">Evidence: {item.sourceEvidence}</p>}</div>
          <Button variant="primary" disabled={busy || !canReconcile || item.status !== 'Ready' || reason.trim().length < 10} onClick={() => void execute(item)}>Reconcile</Button>
        </div>)}
        {!busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">No unowned legacy records exist in this register.</p>}
        {busy && !result.items.length && <p className="p-6 text-center text-sm text-secondary-500">Loading tenant-ownership preview…</p>}
      </div>
      <div className="flex items-center justify-between border-t border-secondary-200 p-3 dark:border-secondary-700"><Button size="sm" variant="outline" disabled={busy || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={busy || page >= result.totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></div>
    </Card>
  </div>;
}
