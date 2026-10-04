import { useState } from 'react';
import { commitOpmsImport, downloadOpmsImportCsv, stageOpmsImport } from '../../api/api';
import type { OpmsImportBatchDto } from '../../types';
import { parseSdbipImportCsv } from './sdbipImportCsv';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';

export function SdbipImportWorkspace() {
  const [layer, setLayer] = useState('');
  const [batch, setBatch] = useState<OpmsImportBatchDto>(); const [error, setError] = useState('');
  const [reason, setReason] = useState(''); const [approval, setApproval] = useState(''); const [effectiveAt, setEffectiveAt] = useState('');
  const stage = async (file?: File) => {
    if (!file || !layer) return; setError(''); setBatch(undefined);
    try {
      const rows = parseSdbipImportCsv(await file.text());
      const response = await stageOpmsImport(layer, { clientRequestId: crypto.randomUUID(), sourceFileName: file.name, rows });
      if (!response.success || !response.data) throw new Error(response.message ?? 'Unable to stage import.'); setBatch(response.data);
    } catch (failure) { setError(failure instanceof Error ? failure.message : 'Unable to read import.'); }
  };
  const commit = async () => {
    if (!batch) return; setError('');
    const response = await commitOpmsImport(batch.publicId, { reason, approvalReference: approval || null, effectiveAt: effectiveAt || null, rowVersion: batch.rowVersion });
    if (!response.success || !response.data) setError(response.message ?? 'Unable to commit import.'); else setBatch(response.data);
  };
  const download = async (exportLayer?: string) => { const response = await downloadOpmsImportCsv(exportLayer); if (!response.success) setError(response.message ?? 'Download failed.'); };
  return <div className="p-6 space-y-5">
    <div><h1 className="text-2xl font-semibold">SDBIP import and reconciliation</h1><p className="text-sm text-secondary-600">Upload the governed wide CSV template. Nothing is written to the KPI register until the complete preview is valid and committed.</p></div>
    <div className="bg-white dark:bg-secondary-800 rounded-lg border p-4 grid gap-4 md:grid-cols-2">
      <CalendarMasterPicker kind="sdbip-layer" label="SDBIP layer" value={layer} onChange={setLayer} required />
      <label className="text-sm">Wide CSV file<input className="mt-1 block w-full" type="file" accept=".csv,text/csv" onChange={event => void stage(event.target.files?.[0])} /></label>
      <div className="md:col-span-2 flex flex-wrap gap-2"><button type="button" className="rounded border px-3 py-2" onClick={() => void download()}>Download blank template</button><button type="button" className="rounded border px-3 py-2 disabled:opacity-50" disabled={!layer} onClick={() => void download(layer)}>Export current SDBIP</button></div>
    </div>
    {error && <div role="alert" className="rounded border border-red-300 bg-red-50 p-3 text-red-800">{error}</div>}
    {batch && <div className="space-y-4">
      <div className="grid grid-cols-2 md:grid-cols-5 gap-2">{[['New', batch.newRows], ['Unchanged', batch.unchangedRows], ['Changed', batch.changedRows], ['Invalid', batch.invalidRows], ['Total', batch.totalRows]].map(([name, value]) => <div key={name} className="rounded border p-3"><div className="text-xs text-secondary-500">{name}</div><div className="text-xl font-semibold">{value}</div></div>)}</div>
      <div className="overflow-auto rounded border"><table className="min-w-full text-sm"><thead><tr className="bg-secondary-50"><th className="p-2 text-left">Row</th><th className="p-2 text-left">KPI</th><th className="p-2 text-left">Result</th><th className="p-2 text-left">Diagnostic</th></tr></thead><tbody>{batch.rows.map(row => <tr key={row.publicId} className="border-t"><td className="p-2">{row.sourceRowNumber}</td><td className="p-2">{row.reference}</td><td className="p-2">{row.status}</td><td className="p-2">{row.errorPeriod && `${row.errorPeriod} / `}{row.errorField && `${row.errorField}: `}{row.errorMessage}</td></tr>)}</tbody></table></div>
      {batch.status === 'Staged' && <div className="rounded border p-4 grid gap-3 md:grid-cols-3"><input className="rounded border p-2" placeholder="Commit reason" value={reason} onChange={event => setReason(event.target.value)} /><input className="rounded border p-2" placeholder="Approval reference (changed rows)" value={approval} onChange={event => setApproval(event.target.value)} /><input className="rounded border p-2" type="datetime-local" value={effectiveAt} onChange={event => setEffectiveAt(event.target.value)} /><button type="button" className="rounded bg-primary-600 px-4 py-2 text-white disabled:opacity-50" disabled={batch.invalidRows > 0 || !reason.trim()} onClick={() => void commit()}>Commit complete batch</button></div>}
    </div>}
  </div>;
}
