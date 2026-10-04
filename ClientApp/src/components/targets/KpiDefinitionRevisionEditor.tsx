import { useEffect, useState } from 'react';
import { History, Save } from 'lucide-react';
import {
  getIpmsTargetFieldRevisions,
  getOpmsTargetFieldRevisions,
  reviseIpmsTargetDefinition,
  reviseOpmsTargetDefinition,
} from '../../api/api';
import { useSecurity } from '../../context/SecurityContext';
import type { IPMSTarget, KpiFieldRevisionDto, OPMSTarget } from '../../types';
import { Input, Textarea } from '../common/Form';
import { Button, Card } from '../ui';

type Props = {
  kind: 'opms' | 'ipms';
  target: OPMSTarget | IPMSTarget;
  onUpdated: (target: OPMSTarget | IPMSTarget) => void;
};

export function KpiDefinitionRevisionEditor({ kind, target, onUpdated }: Props) {
  const security = useSecurity();
  const canRevise = security.canExecute(kind === 'opms' ? 'OPMS_KPI.REVISE' : 'IPMS_KPI.REVISE');
  const [indicatorFlag, setIndicatorFlag] = useState(target.isIndicatorNumberRevised);
  const [indicator, setIndicator] = useState(target.revisedIndicatorNumber ?? target.indicatorNumber);
  const [nameFlag, setNameFlag] = useState(target.isTargetNameRevised);
  const [name, setName] = useState(target.revisedTargetName ?? target.targetName);
  const [kpiFlag, setKpiFlag] = useState(target.isKpiDescriptionRevised);
  const [kpi, setKpi] = useState(target.revisedKpiDescription ?? target.kpiDescription);
  const [reason, setReason] = useState('');
  const [approvalReference, setApprovalReference] = useState('');
  const [effectiveAt, setEffectiveAt] = useState(new Date().toISOString().slice(0, 16));
  const [history, setHistory] = useState<KpiFieldRevisionDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = kind === 'opms' ? getOpmsTargetFieldRevisions : getIpmsTargetFieldRevisions;
    void load(target.id).then(result => {
      if (result.success) setHistory((result.data ?? []).filter(item => ['IndicatorNumber', 'TargetName', 'KpiDescription'].includes(item.fieldName)));
      else setError(result.message ?? 'Revision history could not be loaded.');
    });
  }, [kind, target.id]);

  const save = async () => {
    if (!reason.trim() || !approvalReference.trim() || !effectiveAt) { setError('Revision reason, external approval reference and effective date are required.'); return; }
    if ((indicatorFlag && !indicator.trim()) || (nameFlag && !name.trim()) || (kpiFlag && !kpi.trim())) { setError('Every active revision flag requires its own revised value.'); return; }
    setBusy(true); setError('');
    const payload = {
      isIndicatorNumberRevised: indicatorFlag, revisedIndicatorNumber: indicatorFlag ? indicator.trim() : undefined,
      isTargetNameRevised: nameFlag, revisedTargetName: nameFlag ? name.trim() : undefined,
      isKpiDescriptionRevised: kpiFlag, revisedKpiDescription: kpiFlag ? kpi.trim() : undefined,
      reason: reason.trim(), approvalReference: approvalReference.trim(),
      effectiveAt: new Date(effectiveAt).toISOString(), rowVersion: target.rowVersion ?? '',
    };
    const result = kind === 'opms'
      ? await reviseOpmsTargetDefinition(target.id, payload)
      : await reviseIpmsTargetDefinition(target.id, payload);
    if (!result.success || !result.data) { setError(result.message ?? 'The approved revision could not be recorded.'); setBusy(false); return; }
    onUpdated(result.data);
    const revisions = await (kind === 'opms' ? getOpmsTargetFieldRevisions : getIpmsTargetFieldRevisions)(target.id);
    setHistory((revisions.data ?? []).filter(item => ['IndicatorNumber', 'TargetName', 'KpiDescription'].includes(item.fieldName)));
    setReason(''); setApprovalReference(''); setBusy(false);
  };

  return <Card className="p-4">
    <div className="flex items-center gap-2"><History className="h-4 w-4" /><h2 className="font-semibold">Approved KPI field revisions</h2></div>
    <p className="mt-1 text-xs text-secondary-500">Q1, Q2 and Mid-Term retain the originals. Q3, Q4 and Annual use only the independently flagged revised value.</p>
    {error && <div role="alert" className="mt-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{error}</div>}
    <div className="mt-4 grid gap-4 lg:grid-cols-3">
      <section><p className="text-xs text-secondary-500">Original KPI number: {target.indicatorNumber}</p><label className="mt-2 flex gap-2 text-sm"><input type="checkbox" checked={indicatorFlag} disabled={!canRevise} onChange={event => setIndicatorFlag(event.target.checked)} /> Revised KPI number</label><Input label="Revised KPI number" value={indicator} disabled={!canRevise || !indicatorFlag} onChange={event => setIndicator(event.target.value)} /></section>
      <section><p className="text-xs text-secondary-500">Original target name: {target.targetName}</p><label className="mt-2 flex gap-2 text-sm"><input type="checkbox" checked={nameFlag} disabled={!canRevise} onChange={event => setNameFlag(event.target.checked)} /> Revised target name</label><Textarea label="Revised target name" value={name} disabled={!canRevise || !nameFlag} onChange={event => setName(event.target.value)} /></section>
      <section><p className="text-xs text-secondary-500">Original KPI wording: {target.kpiDescription}</p><label className="mt-2 flex gap-2 text-sm"><input type="checkbox" checked={kpiFlag} disabled={!canRevise} onChange={event => setKpiFlag(event.target.checked)} /> Revised KPI wording</label><Textarea label="Revised KPI wording" value={kpi} disabled={!canRevise || !kpiFlag} onChange={event => setKpi(event.target.value)} /></section>
    </div>
    {canRevise && <div className="mt-4 grid gap-3 md:grid-cols-2"><Input label="External approval reference" value={approvalReference} onChange={event => setApprovalReference(event.target.value)} required /><Input label="Effective at" type="datetime-local" value={effectiveAt} onChange={event => setEffectiveAt(event.target.value)} required /><div className="md:col-span-2"><Textarea label="Revision reason" value={reason} onChange={event => setReason(event.target.value)} required /></div><Button variant="primary" icon={<Save className="h-4 w-4" />} disabled={busy} onClick={() => void save()}>Record approved revision</Button></div>}
    <div className="mt-5"><h3 className="text-sm font-medium">Immutable revision history</h3>{history.length === 0 ? <p className="mt-2 text-xs text-secondary-500">No KPI wording revisions recorded.</p> : <ul className="mt-2 space-y-1 text-xs text-secondary-600">{history.map(item => <li key={item.publicId}>{new Date(item.recordedAt).toLocaleString()} · {item.fieldName}: {item.originalValue ?? '—'} → {item.revisedValue ?? '—'} · {item.approvalReference}</li>)}</ul>}</div>
  </Card>;
}
