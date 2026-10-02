import { useCallback, useEffect, useState } from 'react';
import { FileCheck2, MessageCircleQuestion, RefreshCw } from 'lucide-react';
import { closePerformanceRfi, getIpmsSubmissionAttachments, getOpmsSubmissionAttachments, getPerformanceRfis, raisePerformanceRfi, respondPerformanceRfi } from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { Attachment, PerformanceRfiDto } from '../../types';
import { Input, Textarea } from '../common/Form';
import { Badge, Button } from '../ui';

const localDate = (date = new Date(Date.now() + 2 * 86400000)) => new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);

export function PerformanceRfiWorkspace({ kind, submissionId }: { kind: 1 | 2; submissionId: string }) {
  const { pushToast } = useApp();
  const security = useSecurity();
  const prefix = kind === 1 ? 'OPMS' : 'IPMS';
  const canRaise = security.canExecute(`${prefix}_RFI.RAISE`);
  const canRespond = security.canExecute(`${prefix}_RFI.RESPOND`);
  const canClose = security.canExecute(`${prefix}_RFI.CLOSE`);
  const [rows, setRows] = useState<PerformanceRfiDto[]>([]);
  const [question, setQuestion] = useState('');
  const [responseDueAt, setResponseDueAt] = useState(localDate);
  const [responses, setResponses] = useState<Record<string, string>>({});
  const [availableEvidence, setAvailableEvidence] = useState<Attachment[]>([]);
  const [questionEvidence, setQuestionEvidence] = useState<string[]>([]);
  const [responseEvidence, setResponseEvidence] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setBusy(true); setError('');
    const [result, evidenceResult] = await Promise.all([
      getPerformanceRfis(kind, submissionId),
      kind === 1 ? getOpmsSubmissionAttachments(submissionId) : getIpmsSubmissionAttachments(submissionId),
    ]);
    if (!result.success) setError(result.message ?? 'RFIs could not be loaded.');
    else if (!evidenceResult.success) setError(evidenceResult.message ?? 'Submission evidence could not be loaded.');
    setRows(result.data ?? []);
    setAvailableEvidence((evidenceResult.data ?? []).filter(file => file.isActive !== false && file.publicId && file.scanStatus === 'Clean' && !file.isQuarantined));
    setBusy(false);
  }, [kind, submissionId]);

  useEffect(() => { void load(); }, [load]);

  const raise = async () => {
    if (!question.trim()) { setError('Question is required.'); return; }
    setBusy(true); setError('');
    const result = await raisePerformanceRfi(kind, submissionId, { question: question.trim(), responseDueAt: new Date(responseDueAt).toISOString(), evidencePublicIds: questionEvidence });
    if (!result.success) setError(result.message ?? 'RFI could not be raised.');
    else { setQuestion(''); setQuestionEvidence([]); pushToast('success', 'RFI raised'); await load(); }
    setBusy(false);
  };

  const respond = async (row: PerformanceRfiDto) => {
    const response = responses[row.publicId]?.trim();
    if (!response) { setError('Response is required.'); return; }
    setBusy(true); setError('');
    const result = await respondPerformanceRfi(row.publicId, { response, rowVersion: row.rowVersion, evidencePublicIds: responseEvidence[row.publicId] ?? [] });
    if (!result.success) setError(result.message ?? 'RFI response could not be saved.');
    else { setResponses(current => ({ ...current, [row.publicId]: '' })); setResponseEvidence(current => ({ ...current, [row.publicId]: [] })); pushToast('success', 'RFI response recorded'); await load(); }
    setBusy(false);
  };

  const evidencePicker = (selected: string[], update: (next: string[]) => void, label: string) => availableEvidence.length > 0 && <fieldset className="rounded border border-secondary-200 p-2 dark:border-secondary-700">
    <legend className="px-1 text-xs font-medium text-secondary-600 dark:text-secondary-300">{label}</legend>
    <div className="space-y-1">{availableEvidence.map(file => <label key={file.publicId} className="flex items-center gap-2 text-xs"><input type="checkbox" aria-label={`${label}: ${file.fileName}`} checked={selected.includes(file.publicId!)} onChange={event => update(event.target.checked ? [...selected, file.publicId!] : selected.filter(id => id !== file.publicId))} /><span>{file.fileName}</span><span className="text-success-700">Clean</span></label>)}</div>
  </fieldset>;

  const close = async (row: PerformanceRfiDto) => {
    setBusy(true); setError('');
    const result = await closePerformanceRfi(row.publicId, { rowVersion: row.rowVersion });
    if (!result.success) setError(result.message ?? 'RFI could not be closed.');
    else { pushToast('success', 'RFI closed'); await load(); }
    setBusy(false);
  };

  return <div className="mt-5 rounded-lg border border-secondary-200 p-4 dark:border-secondary-700">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><h3 className="flex items-center gap-2 font-semibold"><MessageCircleQuestion className="h-4 w-4" />Requests for information</h3><p className="mt-1 text-xs text-secondary-500">Governed questions, responses, due dates, concurrency, and closure history.</p></div><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh RFIs</Button></div>
    {error && <div role="alert" className="mt-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{error}</div>}
    {canRaise && <div className="mt-4 grid gap-3 md:grid-cols-[1fr_15rem_auto]"><div className="space-y-2"><Textarea label="New question" value={question} onChange={event => setQuestion(event.target.value)} />{evidencePicker(questionEvidence, setQuestionEvidence, 'Evidence supporting the question')}</div><Input label="Response due" type="datetime-local" value={responseDueAt} onChange={event => setResponseDueAt(event.target.value)} /><div className="self-end"><Button variant="primary" onClick={() => void raise()} disabled={busy}>Raise RFI</Button></div></div>}
    <div className="mt-4 space-y-3">{rows.map(row => <div key={row.publicId} className="rounded-lg border border-secondary-200 bg-secondary-50 p-3 dark:border-secondary-700 dark:bg-secondary-800"><div className="flex flex-wrap items-start justify-between gap-2"><div><p className="text-sm font-medium">{row.question}</p><p className="mt-1 text-xs text-secondary-500">Raised {new Date(row.raisedAt).toLocaleString()} · due {new Date(row.responseDueAt).toLocaleString()}</p></div><Badge variant={row.closedAt ? 'success' : row.respondedAt ? 'warning' : 'error'}>{row.closedAt ? 'Closed' : row.respondedAt ? 'Responded' : 'Open'}</Badge></div>{row.response && <p className="mt-3 rounded bg-white p-2 text-sm dark:bg-secondary-900">{row.response}</p>}{row.evidence?.length > 0 && <div className="mt-3 space-y-1">{row.evidence.map(link => <a key={link.publicId} href={link.url || undefined} className="flex items-center gap-2 text-xs text-primary-700 hover:underline" aria-disabled={!link.url}><FileCheck2 className="h-3.5 w-3.5" />{link.fileName} · {link.purpose === 1 ? 'question' : link.purpose === 2 ? 'response' : 'closure'} evidence</a>)}</div>}{!row.respondedAt && canRespond && <div className="mt-3 grid gap-2 sm:grid-cols-[1fr_18rem_auto]"><Textarea label="Response" value={responses[row.publicId] ?? ''} onChange={event => setResponses(current => ({ ...current, [row.publicId]: event.target.value }))} />{evidencePicker(responseEvidence[row.publicId] ?? [], next => setResponseEvidence(current => ({ ...current, [row.publicId]: next })), 'Evidence supporting the response')}<div className="self-end"><Button size="sm" variant="outline" onClick={() => void respond(row)} disabled={busy}>Respond</Button></div></div>}{row.respondedAt && !row.closedAt && canClose && <div className="mt-3"><Button size="sm" variant="success" onClick={() => void close(row)} disabled={busy}>Close RFI</Button></div>}</div>)}{!rows.length && <p className="text-sm text-secondary-500">{busy ? 'Loading RFIs…' : 'No RFIs recorded for this submission.'}</p>}</div>
  </div>;
}
