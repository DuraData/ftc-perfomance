import { useEffect, useState } from 'react';
import {
  getIpmsTargetOrderingRevisions,
  getOpmsTargetOrderingRevisions,
  reviseIpmsTargetOrdering,
  reviseOpmsTargetOrdering,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { KpiFieldRevisionDto } from '../../types';
import { Input, Textarea } from '../common/Form';
import { Button, Card } from '../ui';

type Props = {
  kind: 'opms' | 'ipms';
  targetId: string;
  originalOrderNumber: number;
  revisedOrderNumber: number;
  rowVersion: string;
  onUpdated: (value: { originalOrderNumber: number; revisedOrderNumber: number; rowVersion: string }) => void;
};

export function KpiOrderingEditor({ kind, targetId, originalOrderNumber, revisedOrderNumber, rowVersion, onUpdated }: Props) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const canUpdate = security.canUpdate(kind === 'opms' ? 'OPMS_KPI' : 'IPMS_KPI');
  const [original, setOriginal] = useState(String(originalOrderNumber));
  const [revised, setRevised] = useState(String(revisedOrderNumber));
  const [reason, setReason] = useState('');
  const [approvalReference, setApprovalReference] = useState('');
  const [effectiveAt, setEffectiveAt] = useState(new Date().toISOString().slice(0, 16));
  const [history, setHistory] = useState<KpiFieldRevisionDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setOriginal(String(originalOrderNumber));
    setRevised(String(revisedOrderNumber));
  }, [originalOrderNumber, revisedOrderNumber]);

  useEffect(() => {
    const load = kind === 'opms' ? getOpmsTargetOrderingRevisions : getIpmsTargetOrderingRevisions;
    void load(targetId).then(result => setHistory(result.data ?? []));
  }, [kind, targetId]);

  const save = async () => {
    const originalValue = Number(original);
    const revisedValue = Number(revised);
    if (!Number.isInteger(originalValue) || originalValue <= 0 || !Number.isInteger(revisedValue) || revisedValue <= 0) {
      setError('Both order numbers must be positive integers.');
      return;
    }
    if (!reason.trim() || !approvalReference.trim() || !effectiveAt) {
      setError('Revision reason, approval reference and effective date are required.');
      return;
    }
    setBusy(true);
    setError('');
    const revise = kind === 'opms' ? reviseOpmsTargetOrdering : reviseIpmsTargetOrdering;
    const result = await revise(targetId, {
      originalOrderNumber: originalValue,
      revisedOrderNumber: revisedValue,
      reason: reason.trim(),
      approvalReference: approvalReference.trim(),
      effectiveAt: new Date(effectiveAt).toISOString(),
      rowVersion,
    });
    if (!result.success || !result.data) {
      setError(result.message ?? 'KPI ordering could not be revised.');
      setBusy(false);
      return;
    }
    onUpdated({ originalOrderNumber: result.data.originalOrderNumber, revisedOrderNumber: result.data.revisedOrderNumber, rowVersion: result.data.rowVersion ?? rowVersion });
    const historyResult = await (kind === 'opms' ? getOpmsTargetOrderingRevisions : getIpmsTargetOrderingRevisions)(targetId);
    setHistory(historyResult.data ?? []);
    setReason('');
    setApprovalReference('');
    pushToast('success', 'Approved KPI ordering revision recorded');
    setBusy(false);
  };

  return (
    <div className="space-y-4">
      <Card className="p-4">
        <h3 className="font-semibold text-secondary-900 dark:text-white">Period-based KPI ordering</h3>
        <p className="mt-1 text-xs text-secondary-500">Q1, Q2 and Mid-Term use original order. Q3, Q4 and Annual use revised order. Duplicate order numbers use the stable KPI identifier as a tie-breaker.</p>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          <Input label="Original order number" type="number" min="1" step="1" value={original} disabled={!canUpdate} onChange={event => setOriginal(event.target.value)} />
          <Input label="Revised order number" type="number" min="1" step="1" value={revised} disabled={!canUpdate} onChange={event => setRevised(event.target.value)} />
          {canUpdate && <>
            <Input label="Approval reference" value={approvalReference} onChange={event => setApprovalReference(event.target.value)} required />
            <Input label="Effective at" type="datetime-local" value={effectiveAt} onChange={event => setEffectiveAt(event.target.value)} required />
            <div className="md:col-span-2"><Textarea label="Revision reason" value={reason} onChange={event => setReason(event.target.value)} required /></div>
          </>}
        </div>
        {error && <p role="alert" className="mt-3 text-sm text-danger-600">{error}</p>}
        {canUpdate && <div className="mt-4"><Button variant="primary" onClick={() => void save()} disabled={busy}>Record ordering revision</Button></div>}
      </Card>
      <Card className="p-4">
        <h3 className="font-semibold text-secondary-900 dark:text-white">Immutable ordering history</h3>
        {!history.length ? <p className="mt-2 text-sm text-secondary-500">No ordering revisions recorded.</p> : <ul className="mt-3 space-y-2 text-sm">{history.map(item => <li key={item.publicId} className="rounded border border-secondary-200 p-2 dark:border-secondary-700"><strong>{item.fieldName}</strong>: {item.originalValue ?? '—'} → {item.revisedValue ?? '—'}<div className="text-xs text-secondary-500">{item.approvalReference} · {item.reason} · effective {new Date(item.effectiveAt).toLocaleString()}</div></li>)}</ul>}
      </Card>
    </div>
  );
}
