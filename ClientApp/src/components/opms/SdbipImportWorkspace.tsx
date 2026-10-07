import { useEffect, useState } from 'react';
import { commitOpmsImport, downloadOpmsImportCsv, getOpmsImportBatch, getOpmsImportBatchesPage, stageOpmsImport } from '../../api/api';
import type { OpmsImportBatchDto, OpmsImportBatchSummaryDto } from '../../types';
import { useSecurity } from '../../context/SecurityContext';
import { parseSdbipImportCsv } from './sdbipImportCsv';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';

export function SdbipImportWorkspace() {
  const { canReadField } = useSecurity();
  const canReadRequestId = canReadField('OPMS_KPI', 'ImportClientRequestId');
  const canReadFileName = canReadField('OPMS_KPI', 'ImportSourceFileName');
  const canReadSourceHash = canReadField('OPMS_KPI', 'ImportSourceHash');
  const canReadActor = canReadField('OPMS_KPI', 'ImportActor');
  const canReadError = canReadField('OPMS_KPI', 'ImportErrorDetail');
  const canSearchHistory = canReadFileName || canReadSourceHash || canReadActor;
  const [layer, setLayer] = useState('');
  const [batch, setBatch] = useState<OpmsImportBatchDto>();
  const [history, setHistory] = useState<OpmsImportBatchSummaryDto[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotalPages, setHistoryTotalPages] = useState(0);
  const [historySearch, setHistorySearch] = useState('');
  const [historyStatus, setHistoryStatus] = useState('');
  const [historySort, setHistorySort] = useState('createdAt');
  const [historyRevision, setHistoryRevision] = useState(0);
  const [error, setError] = useState('');
  const [reason, setReason] = useState('');
  const [approval, setApproval] = useState('');
  const [effectiveAt, setEffectiveAt] = useState('');

  useEffect(() => {
    if (!layer) { setHistory([]); setHistoryTotalPages(0); return; }
    let cancelled = false;
    void getOpmsImportBatchesPage(layer, {
      page: historyPage, pageSize: 10, search: canSearchHistory ? historySearch : undefined,
      status: historyStatus || undefined, sortBy: historySort, sortDirection: 'desc',
    }).then(response => {
      if (cancelled) return;
      if (!response.success) { setError(response.message ?? 'Unable to load import history.'); return; }
      setHistory(response.data?.items ?? []);
      setHistoryTotalPages(response.data?.totalPages ?? 0);
    });
    return () => { cancelled = true; };
  }, [canSearchHistory, historyPage, historyRevision, historySearch, historySort, historyStatus, layer]);

  const stage = async (file?: File) => {
    if (!file || !layer) return;
    setError(''); setBatch(undefined);
    try {
      const rows = parseSdbipImportCsv(await file.text());
      const response = await stageOpmsImport(layer, { clientRequestId: crypto.randomUUID(), sourceFileName: file.name, rows });
      if (!response.success || !response.data) throw new Error(response.message ?? 'Unable to stage import.');
      setBatch(response.data); setHistoryPage(1); setHistoryRevision(value => value + 1);
    } catch (failure) { setError(failure instanceof Error ? failure.message : 'Unable to read import.'); }
  };
  const commit = async () => {
    if (!batch) return;
    setError('');
    const response = await commitOpmsImport(batch.publicId, { reason, approvalReference: approval || null, effectiveAt: effectiveAt || null, rowVersion: batch.rowVersion });
    if (!response.success || !response.data) setError(response.message ?? 'Unable to commit import.');
    else { setBatch(response.data); setHistoryPage(1); setHistoryRevision(value => value + 1); }
  };
  const openBatch = async (publicId: string) => {
    const response = await getOpmsImportBatch(publicId);
    if (!response.success || !response.data) setError(response.message ?? 'Unable to load import batch.');
    else setBatch(response.data);
  };
  const download = async (exportLayer?: string) => {
    const response = await downloadOpmsImportCsv(exportLayer);
    if (!response.success) setError(response.message ?? 'Download failed.');
  };

  return <div className="p-6 space-y-5">
    <div><h1 className="text-2xl font-semibold">SDBIP import and reconciliation</h1><p className="text-sm text-secondary-600">Upload the governed wide CSV template. Nothing is written to the KPI register until the complete preview is valid and committed.</p></div>
    <div className="bg-white dark:bg-secondary-800 rounded-lg border p-4 grid gap-4 md:grid-cols-2">
      <CalendarMasterPicker kind="sdbip-layer" label="SDBIP layer" value={layer} onChange={value => { setLayer(value); setHistoryPage(1); setBatch(undefined); }} required />
      <label className="text-sm">Wide CSV file<input className="mt-1 block w-full" type="file" accept=".csv,text/csv" onChange={event => void stage(event.target.files?.[0])} /></label>
      <div className="md:col-span-2 flex flex-wrap gap-2"><button type="button" className="rounded border px-3 py-2" onClick={() => void download()}>Download blank template</button><button type="button" className="rounded border px-3 py-2 disabled:opacity-50" disabled={!layer} onClick={() => void download(layer)}>Export current SDBIP</button></div>
    </div>
    {error && <div role="alert" className="rounded border border-red-300 bg-red-50 p-3 text-red-800">{error}</div>}
    {batch && <div className="space-y-4">
      {(canReadFileName || canReadRequestId || canReadSourceHash || canReadActor) && <div className="rounded border p-3 text-sm space-y-1">
        {canReadFileName && batch.sourceFileName && <div>Source: {batch.sourceFileName}</div>}
        {canReadRequestId && batch.clientRequestId && <div>Request: {batch.clientRequestId}</div>}
        {canReadSourceHash && batch.sourceSha256 && <div>SHA-256: {batch.sourceSha256}</div>}
        {canReadActor && batch.createdByName && <div>Created by: {batch.createdByName}</div>}
        {canReadActor && batch.committedByName && <div>Committed by: {batch.committedByName}</div>}
      </div>}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-2">{[['New', batch.newRows], ['Unchanged', batch.unchangedRows], ['Changed', batch.changedRows], ['Invalid', batch.invalidRows], ['Total', batch.totalRows]].map(([name, value]) => <div key={name} className="rounded border p-3"><div className="text-xs text-secondary-500">{name}</div><div className="text-xl font-semibold">{value}</div></div>)}</div>
      <div className="overflow-auto rounded border"><table className="min-w-full text-sm"><thead><tr className="bg-secondary-50"><th className="p-2 text-left">Row</th><th className="p-2 text-left">KPI</th><th className="p-2 text-left">Result</th>{canReadError && <th className="p-2 text-left">Diagnostic</th>}</tr></thead><tbody>{batch.rows.map(row => <tr key={row.publicId} className="border-t"><td className="p-2">{row.sourceRowNumber}</td><td className="p-2">{row.reference}</td><td className="p-2">{row.status}</td>{canReadError && <td className="p-2">{row.errorPeriod && `${row.errorPeriod} / `}{row.errorField && `${row.errorField}: `}{row.errorMessage}</td>}</tr>)}</tbody></table></div>
      {batch.status === 'Staged' && <div className="rounded border p-4 grid gap-3 md:grid-cols-3"><input className="rounded border p-2" placeholder="Commit reason" value={reason} onChange={event => setReason(event.target.value)} /><input className="rounded border p-2" placeholder="Approval reference (changed rows)" value={approval} onChange={event => setApproval(event.target.value)} /><input className="rounded border p-2" type="datetime-local" value={effectiveAt} onChange={event => setEffectiveAt(event.target.value)} /><button type="button" className="rounded bg-primary-600 px-4 py-2 text-white disabled:opacity-50" disabled={batch.invalidRows > 0 || !reason.trim()} onClick={() => void commit()}>Commit complete batch</button></div>}
    </div>}
    {layer && <section className="space-y-3 rounded border p-4" aria-label="SDBIP import history">
      <h2 className="font-semibold">Import history</h2>
      <div className="flex flex-wrap gap-2">
        {canSearchHistory && <input aria-label="Search SDBIP import history" className="rounded border p-2" value={historySearch} onChange={event => { setHistorySearch(event.target.value); setHistoryPage(1); }} placeholder="Search permitted metadata" />}
        <select aria-label="Filter SDBIP import status" className="rounded border p-2" value={historyStatus} onChange={event => { setHistoryStatus(event.target.value); setHistoryPage(1); }}><option value="">All statuses</option><option value="Staged">Staged</option><option value="Committed">Committed</option></select>
        <select aria-label="Sort SDBIP import history" className="rounded border p-2" value={historySort} onChange={event => { setHistorySort(event.target.value); setHistoryPage(1); }}><option value="createdAt">Created</option>{canReadFileName && <option value="fileName">File name</option>}<option value="status">Status</option><option value="totalRows">Row count</option><option value="committedAt">Committed</option></select>
      </div>
      <div className="flex flex-wrap gap-2">{history.map(item => <button key={item.publicId} type="button" className="rounded border px-2 py-1 text-xs" onClick={() => void openBatch(item.publicId)}>{canReadFileName && item.sourceFileName ? `${item.sourceFileName} · ` : ''}{item.status} · {item.totalRows} rows · {new Date(item.createdAt).toLocaleString()}</button>)}</div>
      {!history.length && <p className="text-sm text-secondary-500">No import batches match the current filters.</p>}
      <div className="flex items-center justify-between text-xs text-secondary-500"><span>Page {historyPage} of {Math.max(1, historyTotalPages)}</span><div className="flex gap-2"><button type="button" className="rounded border px-2 py-1" disabled={historyPage <= 1} onClick={() => setHistoryPage(value => value - 1)}>Previous</button><button type="button" className="rounded border px-2 py-1" disabled={historyPage >= historyTotalPages} onClick={() => setHistoryPage(value => value + 1)}>Next</button></div></div>
    </section>}
  </div>;
}
