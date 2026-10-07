import { useCallback, useEffect, useState } from 'react';
import { FileCheck2, MessageCircleQuestion, RefreshCw } from 'lucide-react';
import { closePerformanceRfi, getIpmsSubmissionAttachmentsPage, getOpmsSubmissionAttachmentsPage, getPerformanceRfisPage, raisePerformanceRfi, respondPerformanceRfi } from '../../api/api';
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
  const resource = `${prefix}_RFI`;
  const canRead = security.canRead(resource);
  const canReadQuestion = security.canReadField(resource, 'Question');
  const canReadRaisedBy = security.canReadField(resource, 'RaisedBy');
  const canReadResponse = security.canReadField(resource, 'Response');
  const canReadRespondedBy = security.canReadField(resource, 'RespondedBy');
  const canReadClosedBy = security.canReadField(resource, 'ClosedBy');
  const canReadEvidenceMetadata = security.canReadField(resource, 'EvidenceMetadata');
  const canReadEvidenceLinkedBy = security.canReadField(resource, 'EvidenceLinkedBy');
  const canEditQuestion = security.canEditField(resource, 'Question');
  const canEditResponse = security.canEditField(resource, 'Response');
  const canRaise = security.canExecute(`${prefix}_RFI.RAISE`) && canEditQuestion;
  const canRespond = security.canExecute(`${prefix}_RFI.RESPOND`) && canEditResponse;
  const canClose = security.canExecute(`${prefix}_RFI.CLOSE`);
  const canSearch = canReadQuestion || canReadRaisedBy || canReadResponse || canReadRespondedBy || canReadClosedBy || canReadEvidenceMetadata || canReadEvidenceLinkedBy;
  const [rows, setRows] = useState<PerformanceRfiDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'' | 'open' | 'responded' | 'closed' | 'overdue'>('');
  const [sortBy, setSortBy] = useState('raisedAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [question, setQuestion] = useState('');
  const [responseDueAt, setResponseDueAt] = useState(localDate);
  const [responses, setResponses] = useState<Record<string, string>>({});
  const [availableEvidence, setAvailableEvidence] = useState<Attachment[]>([]);
  const [evidencePage, setEvidencePage] = useState(1);
  const [evidenceTotalPages, setEvidenceTotalPages] = useState(0);
  const [evidenceTotalCount, setEvidenceTotalCount] = useState(0);
  const [evidenceSearchInput, setEvidenceSearchInput] = useState('');
  const [evidenceSearch, setEvidenceSearch] = useState('');
  const [questionEvidence, setQuestionEvidence] = useState<string[]>([]);
  const [responseEvidence, setResponseEvidence] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const loadRfis = useCallback(async () => {
    if (!canRead) { setRows([]); setTotalCount(0); setTotalPages(0); return; }
    setBusy(true); setError('');
    const result = await getPerformanceRfisPage(kind, submissionId, { page, pageSize: 25, search, status: status || undefined, sortBy, sortDirection });
    if (!result.success) setError(result.message ?? 'RFIs could not be loaded.');
    setRows(result.data?.items ?? []);
    setTotalCount(result.data?.totalCount ?? 0);
    setTotalPages(result.data?.totalPages ?? 0);
    setBusy(false);
  }, [canRead, kind, submissionId, page, search, status, sortBy, sortDirection]);

  const loadEvidence = useCallback(async () => {
    if (!canRaise && !canRespond) { setAvailableEvidence([]); setEvidenceTotalCount(0); setEvidenceTotalPages(0); return; }
    const query = { page: evidencePage, pageSize: 25, search: evidenceSearch, sortBy: 'uploadedAt', sortDirection: 'desc' as const, scanStatus: 'Clean', quarantined: false, active: true };
    const evidenceResult = kind === 1 ? await getOpmsSubmissionAttachmentsPage(submissionId, query) : await getIpmsSubmissionAttachmentsPage(submissionId, query);
    if (!evidenceResult.success) setError(evidenceResult.message ?? 'Submission evidence could not be loaded.');
    setAvailableEvidence((evidenceResult.data?.items ?? []).filter(file => file.publicId));
    setEvidenceTotalPages(evidenceResult.data?.totalPages ?? 0);
    setEvidenceTotalCount(evidenceResult.data?.totalCount ?? 0);
  }, [canRaise, canRespond, evidencePage, evidenceSearch, kind, submissionId]);

  useEffect(() => { void loadRfis(); }, [loadRfis]);
  useEffect(() => { void loadEvidence(); }, [loadEvidence]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);
  useEffect(() => {
    const normalized = evidenceSearchInput.trim();
    if (normalized === evidenceSearch) return;
    const timeout = window.setTimeout(() => { setEvidencePage(1); setEvidenceSearch(normalized); }, 300);
    return () => window.clearTimeout(timeout);
  }, [evidenceSearch, evidenceSearchInput]);

  const raise = async () => {
    if (!question.trim()) { setError('Question is required.'); return; }
    setBusy(true); setError('');
    const result = await raisePerformanceRfi(kind, submissionId, { question: question.trim(), responseDueAt: new Date(responseDueAt).toISOString(), evidencePublicIds: questionEvidence });
    if (!result.success) setError(result.message ?? 'RFI could not be raised.');
    else { setQuestion(''); setQuestionEvidence([]); if (page === 1) await loadRfis(); else setPage(1); pushToast('success', 'RFI raised'); }
    setBusy(false);
  };

  const respond = async (row: PerformanceRfiDto) => {
    const response = responses[row.publicId]?.trim();
    if (!response) { setError('Response is required.'); return; }
    setBusy(true); setError('');
    const result = await respondPerformanceRfi(row.publicId, { response, rowVersion: row.rowVersion, evidencePublicIds: responseEvidence[row.publicId] ?? [] });
    if (!result.success) setError(result.message ?? 'RFI response could not be saved.');
    else { setResponses(current => ({ ...current, [row.publicId]: '' })); setResponseEvidence(current => ({ ...current, [row.publicId]: [] })); await loadRfis(); pushToast('success', 'RFI response recorded'); }
    setBusy(false);
  };

  const evidencePicker = (selected: string[], update: (next: string[]) => void, label: string) => <fieldset className="rounded border border-secondary-200 p-2 dark:border-secondary-700">
    <legend className="px-1 text-xs font-medium text-secondary-600 dark:text-secondary-300">{label}</legend>
    <div className="mb-2 flex flex-wrap items-center gap-2"><Input aria-label={`${label} search`} value={evidenceSearchInput} onChange={event => setEvidenceSearchInput(event.target.value)} placeholder="Search clean evidence" /><span className="text-xs text-secondary-500">{evidenceTotalCount} available · {selected.length} selected</span></div>
    <div className="space-y-1">{availableEvidence.map(file => <label key={file.publicId} className="flex items-center gap-2 text-xs"><input type="checkbox" aria-label={`${label}: ${file.fileName}`} checked={selected.includes(file.publicId!)} onChange={event => update(event.target.checked ? [...selected, file.publicId!] : selected.filter(id => id !== file.publicId))} /><span>{file.fileName}</span><span className="text-success-700">Clean</span></label>)}</div>
    {!availableEvidence.length && <p className="text-xs text-secondary-500">No matching clean evidence.</p>}
    <div className="mt-2 flex items-center justify-between text-xs text-secondary-500"><span>Evidence page {evidencePage} of {Math.max(1, evidenceTotalPages)}</span><div className="flex gap-1"><Button size="sm" variant="outline" disabled={evidencePage <= 1} onClick={() => setEvidencePage(value => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={evidencePage >= evidenceTotalPages} onClick={() => setEvidencePage(value => value + 1)}>Next</Button></div></div>
  </fieldset>;

  const close = async (row: PerformanceRfiDto) => {
    setBusy(true); setError('');
    const result = await closePerformanceRfi(row.publicId, { rowVersion: row.rowVersion });
    if (!result.success) setError(result.message ?? 'RFI could not be closed.');
    else { await loadRfis(); pushToast('success', 'RFI closed'); }
    setBusy(false);
  };

  if (!canRead) return null;

  return <div className="mt-5 rounded-lg border border-secondary-200 p-4 dark:border-secondary-700">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><h3 className="flex items-center gap-2 font-semibold"><MessageCircleQuestion className="h-4 w-4" />Requests for information</h3><p className="mt-1 text-xs text-secondary-500">Governed questions, responses, due dates, concurrency, and closure history.</p></div><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => { void loadRfis(); void loadEvidence(); }} disabled={busy}>Refresh RFIs</Button></div>
    {error && <div role="alert" className="mt-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{error}</div>}
    {canRaise && <div className="mt-4 grid gap-3 md:grid-cols-[1fr_15rem_auto]"><div className="space-y-2"><Textarea label="New question" value={question} onChange={event => setQuestion(event.target.value)} />{evidencePicker(questionEvidence, setQuestionEvidence, 'Evidence supporting the question')}</div><Input label="Response due" type="datetime-local" value={responseDueAt} onChange={event => setResponseDueAt(event.target.value)} /><div className="self-end"><Button variant="primary" onClick={() => void raise()} disabled={busy}>Raise RFI</Button></div></div>}
    <div className="mt-4 flex flex-wrap items-end gap-2">{canSearch && <label className="text-xs text-secondary-600">Search<Input aria-label="Search RFIs" value={searchInput} onChange={event => setSearchInput(event.target.value)} /></label>}<label className="text-xs text-secondary-600">Status<select aria-label="Filter RFI status" className="block rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={status} onChange={event => { setStatus(event.target.value as typeof status); setPage(1); }}><option value="">All</option><option value="open">Open</option><option value="responded">Responded</option><option value="closed">Closed</option><option value="overdue">Overdue</option></select></label><label className="text-xs text-secondary-600">Sort<select aria-label="Sort RFIs" className="block rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); }}><option value="raisedAt">Raised</option><option value="dueAt">Due date</option><option value="status">Status</option></select></label><select aria-label="RFI sort direction" className="rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select><span className="text-xs text-secondary-500">{totalCount} RFIs</span></div>
    <div className="mt-4 space-y-3">{rows.map(row => <div key={row.publicId} className="rounded-lg border border-secondary-200 bg-secondary-50 p-3 dark:border-secondary-700 dark:bg-secondary-800"><div className="flex flex-wrap items-start justify-between gap-2"><div><p className="text-sm font-medium">{canReadQuestion && row.question ? row.question : 'Protected RFI question'}</p><p className="mt-1 text-xs text-secondary-500">Raised {new Date(row.raisedAt).toLocaleString()}{canReadRaisedBy && row.raisedByName ? ` by ${row.raisedByName}` : ''} · due {new Date(row.responseDueAt).toLocaleString()}</p></div><Badge variant={row.closedAt ? 'success' : row.respondedAt ? 'warning' : 'error'}>{row.closedAt ? 'Closed' : row.respondedAt ? 'Responded' : 'Open'}</Badge></div>{canReadResponse && row.response && <p className="mt-3 rounded bg-white p-2 text-sm dark:bg-secondary-900">{row.response}{canReadRespondedBy && row.respondedByName ? <span className="mt-1 block text-xs text-secondary-500">Responded by {row.respondedByName}</span> : null}</p>}{row.closedAt && canReadClosedBy && row.closedByName && <p className="mt-2 text-xs text-secondary-500">Closed by {row.closedByName}</p>}{canReadEvidenceMetadata && row.evidence?.length > 0 && <div className="mt-3 space-y-1">{row.evidence.map(link => <a key={link.publicId} href={link.url || undefined} className="flex items-center gap-2 text-xs text-primary-700 hover:underline" aria-disabled={!link.url}><FileCheck2 className="h-3.5 w-3.5" />{link.fileName || 'Evidence'} · {link.purpose === 1 ? 'question' : link.purpose === 2 ? 'response' : 'closure'} evidence{canReadEvidenceLinkedBy && link.linkedByName ? ` · linked by ${link.linkedByName}` : ''}</a>)}</div>}{!row.respondedAt && canRespond && <div className="mt-3 grid gap-2 sm:grid-cols-[1fr_18rem_auto]"><Textarea label="Response" value={responses[row.publicId] ?? ''} onChange={event => setResponses(current => ({ ...current, [row.publicId]: event.target.value }))} />{evidencePicker(responseEvidence[row.publicId] ?? [], next => setResponseEvidence(current => ({ ...current, [row.publicId]: next })), 'Evidence supporting the response')}<div className="self-end"><Button size="sm" variant="outline" onClick={() => void respond(row)} disabled={busy}>Respond</Button></div></div>}{row.respondedAt && !row.closedAt && canClose && <div className="mt-3"><Button size="sm" variant="success" onClick={() => void close(row)} disabled={busy}>Close RFI</Button></div>}</div>)}{!rows.length && <p className="text-sm text-secondary-500">{busy ? 'Loading RFIs…' : 'No RFIs recorded for this submission.'}</p>}</div>
    <div className="mt-3 flex items-center justify-between text-xs text-secondary-500"><span>Page {page} of {Math.max(1, totalPages)}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous RFIs</Button><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(value => value + 1)}>Next RFIs</Button></div></div>
  </div>;
}
