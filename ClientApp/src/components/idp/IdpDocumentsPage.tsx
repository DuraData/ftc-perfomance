import { useEffect, useState } from 'react';
import { Download, FileText, RefreshCw, ShieldCheck, Upload } from 'lucide-react';
import { downloadIdpDocument, getIdpDocumentsPage, rescanIdpDocument, uploadIdpDocument } from '../../api/api';
import { useApp } from '../../context/AppContext';
import type { IdpDocument } from '../../types';
import { useSecurity } from '../../context/SecurityContext';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';
import { IdpPlanPicker } from './IdpPlanPicker';

const pageSize = 25;
const fieldClass = 'mt-1 w-full rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm text-secondary-900 dark:border-secondary-700 dark:bg-secondary-900 dark:text-white';
const categories = ['SignedIdp', 'CouncilResolution', 'Policy', 'Framework', 'Circular', 'Guideline', 'ParticipationEvidence', 'PoeEvidence', 'Governance'];
const scanStatuses = ['Clean', 'Detected', 'ScannerUnavailable', 'Pending'];

const formatBytes = (value: number) => value >= 1024 * 1024
  ? `${(value / (1024 * 1024)).toFixed(1)} MB`
  : `${Math.max(1, Math.ceil(value / 1024))} KB`;

export function IdpDocumentsPage() {
  const { pushToast } = useApp();
  const security = useSecurity();
  const canRead = security.canRead('IDP_DOCUMENT');
  const canCreate = security.canCreate('IDP_DOCUMENT');
  const canRescan = security.canExecute('IDP_DOCUMENT.RESCAN');
  const canReadUploader = security.canReadField('IDP_DOCUMENT', 'UploadedByUserId') || security.canReadField('IDP_DOCUMENT', 'UploadedByName');
  const canReadScannerProvider = security.canReadField('IDP_DOCUMENT', 'ScannerProvider');
  const canReadScannerReference = security.canReadField('IDP_DOCUMENT', 'ScannerReference');
  const canReadScanDetail = security.canReadField('IDP_DOCUMENT', 'ScanDetail');
  const [planPublicId, setPlanPublicId] = useState('');
  const [documents, setDocuments] = useState<IdpDocument[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [scanStatus, setScanStatus] = useState('');
  const [quarantine, setQuarantine] = useState('');
  const [sortBy, setSortBy] = useState('uploadedAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [refreshKey, setRefreshKey] = useState(0);
  const [loading, setLoading] = useState(false);
  const [busyDocumentId, setBusyDocumentId] = useState('');
  const [uploading, setUploading] = useState(false);
  const [title, setTitle] = useState('');
  const [uploadCategory, setUploadCategory] = useState('Governance');
  const [planVersionNumber, setPlanVersionNumber] = useState('');
  const [file, setFile] = useState<File | null>(null);

  useEffect(() => {
    if (!canRead || !planPublicId) {
      setDocuments([]); setTotalCount(0); setTotalPages(0);
      return;
    }
    let cancelled = false;
    const load = async () => {
      setLoading(true);
      const result = await getIdpDocumentsPage(planPublicId, { page, pageSize, search: search || undefined, sortBy, sortDirection }, {
        category: category || undefined,
        scanStatus: scanStatus || undefined,
        quarantined: quarantine === '' ? undefined : quarantine === 'true',
      });
      if (cancelled) return;
      if (result.success && result.data) {
        setDocuments(result.data.items); setTotalCount(result.data.totalCount); setTotalPages(result.data.totalPages);
      } else {
        setDocuments([]); setTotalCount(0); setTotalPages(0);
        pushToast('error', result.message ?? 'Unable to load IDP documents.');
      }
      setLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [canRead, category, page, planPublicId, pushToast, quarantine, refreshKey, scanStatus, search, sortBy, sortDirection]);

  const upload = async () => {
    if (!planPublicId || !file || !title.trim()) {
      pushToast('error', 'Select a plan, document file, and title.');
      return;
    }
    const parsedVersion = planVersionNumber ? Number(planVersionNumber) : undefined;
    if (parsedVersion !== undefined && (!Number.isInteger(parsedVersion) || parsedVersion < 1)) {
      pushToast('error', 'Plan version must be a positive whole number.');
      return;
    }
    setUploading(true);
    const result = await uploadIdpDocument(planPublicId, { file, category: uploadCategory, title: title.trim(), planVersionNumber: parsedVersion });
    setUploading(false);
    if (!result.success) {
      pushToast('error', result.message ?? 'Unable to upload the IDP document.');
      return;
    }
    pushToast(result.data?.isQuarantined ? 'info' : 'success', result.message ?? 'IDP document uploaded.');
    setTitle(''); setPlanVersionNumber(''); setFile(null); setPage(1); setRefreshKey(value => value + 1);
  };

  const rescan = async (document: IdpDocument) => {
    setBusyDocumentId(document.publicId);
    const result = await rescanIdpDocument(planPublicId, document.publicId);
    setBusyDocumentId('');
    pushToast(result.success ? (result.data?.isQuarantined ? 'info' : 'success') : 'error', result.message ?? (result.success ? 'Document scan completed.' : 'Unable to rescan document.'));
    if (result.success) setRefreshKey(value => value + 1);
  };

  const download = async (document: IdpDocument) => {
    setBusyDocumentId(document.publicId);
    const result = await downloadIdpDocument(document);
    setBusyDocumentId('');
    if (!result.success) pushToast('error', result.message ?? 'Unable to download document.');
  };

  return <AppShell title="IDP Documents" subtitle="Governed IDP publications, resolutions, policies, and supporting evidence">
    <div className="space-y-4">
      {!canRead ? <Card><p className="text-sm text-secondary-600">You do not have permission to read IDP documents.</p></Card> : <>
        <Card>
          <div className="grid gap-3 lg:grid-cols-2">
            <IdpPlanPicker label="Document plan" value={planPublicId} autoSelectFirst onChange={value => { setPlanPublicId(value); setPage(1); }} />
            <form className="grid gap-3 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); setPage(1); setSearch(searchInput.trim()); }}>
              <label className="text-xs text-secondary-600 sm:col-span-2">Search documents<input aria-label="Search IDP documents" className={fieldClass} value={searchInput} onChange={event => setSearchInput(event.target.value)} placeholder="Title or file name" /></label>
              <Button type="submit" variant="outline">Apply search</Button>
              <Button type="button" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => setRefreshKey(value => value + 1)}>Refresh</Button>
            </form>
          </div>
          <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <label className="text-xs text-secondary-600">Category<select aria-label="Filter IDP document category" className={fieldClass} value={category} onChange={event => { setCategory(event.target.value); setPage(1); }}><option value="">All categories</option>{categories.map(value => <option key={value}>{value}</option>)}</select></label>
            <label className="text-xs text-secondary-600">Scan status<select aria-label="Filter IDP document scan status" className={fieldClass} value={scanStatus} onChange={event => { setScanStatus(event.target.value); setPage(1); }}><option value="">All scan states</option>{scanStatuses.map(value => <option key={value}>{value}</option>)}</select></label>
            <label className="text-xs text-secondary-600">Quarantine<select aria-label="Filter quarantined IDP documents" className={fieldClass} value={quarantine} onChange={event => { setQuarantine(event.target.value); setPage(1); }}><option value="">All documents</option><option value="false">Released</option><option value="true">Quarantined</option></select></label>
            <label className="text-xs text-secondary-600">Sort<select aria-label="Sort IDP documents" className={fieldClass} value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); }}><option value="uploadedAt">Uploaded</option><option value="title">Title</option><option value="fileName">File name</option><option value="category">Category</option><option value="scanStatus">Scan status</option><option value="versionNumber">Document version</option></select></label>
            <label className="text-xs text-secondary-600">Direction<select aria-label="IDP document sort direction" className={fieldClass} value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label>
          </div>
        </Card>

        {canCreate ? <Card>
          <h2 className="text-base font-semibold text-secondary-900 dark:text-white">Upload governed document</h2>
          <div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <label className="text-xs text-secondary-600">Title<input aria-label="IDP document title" className={fieldClass} value={title} onChange={event => setTitle(event.target.value)} /></label>
            <label className="text-xs text-secondary-600">Category<select aria-label="IDP document category" className={fieldClass} value={uploadCategory} onChange={event => setUploadCategory(event.target.value)}>{categories.map(value => <option key={value}>{value}</option>)}</select></label>
            <label className="text-xs text-secondary-600">Plan version (optional)<input aria-label="IDP document plan version" type="number" min="1" className={fieldClass} value={planVersionNumber} onChange={event => setPlanVersionNumber(event.target.value)} /></label>
            <label className="text-xs text-secondary-600">Document file<input aria-label="IDP document file" className={fieldClass} type="file" accept=".pdf,.png,.jpg,.jpeg,.docx,.xlsx" onChange={event => setFile(event.target.files?.[0] ?? null)} /></label>
          </div>
          <div className="mt-3"><Button icon={<Upload className="h-4 w-4" />} loading={uploading} disabled={!planPublicId || uploading} onClick={() => void upload()}>Upload and scan</Button></div>
        </Card> : null}

        <Card padding="none">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-secondary-200 text-sm dark:divide-secondary-700">
              <thead><tr className="text-left text-xs uppercase text-secondary-500"><th className="p-3">Document</th><th className="p-3">Category / version</th><th className="p-3">Upload</th><th className="p-3">Integrity</th><th className="p-3">Actions</th></tr></thead>
              <tbody className="divide-y divide-secondary-100 dark:divide-secondary-800">{documents.map(document => <tr key={document.publicId}>
                <td className="p-3"><p className="font-medium text-secondary-900 dark:text-white">{document.title}</p><p className="text-xs text-secondary-500">{document.fileName} · {formatBytes(document.sizeInBytes)}</p></td>
                <td className="p-3"><p>{document.category}</p><p className="text-xs text-secondary-500">Document v{document.versionNumber}{document.planVersionNumber ? ` · Plan v${document.planVersionNumber}` : ''}</p></td>
                <td className="p-3"><p>{new Date(document.uploadedAt).toLocaleString()}</p>{canReadUploader ? <p className="text-xs text-secondary-500">{document.uploadedByName ?? document.uploadedByUserPublicId ?? 'Uploader unavailable'}</p> : null}</td>
                <td className="p-3"><Badge variant={document.isQuarantined ? 'error' : document.scanStatus === 'Clean' ? 'success' : 'warning'}>{document.isQuarantined ? 'Quarantined' : document.scanStatus}</Badge><p className="mt-1 text-xs text-secondary-500">{document.signatureVerified ? 'Signature verified' : 'Signature unverified'}</p>{canReadScannerProvider && document.scannerProvider ? <p className="mt-1 text-xs text-secondary-500">Provider: {document.scannerProvider}</p> : null}{canReadScannerReference && document.scannerReference ? <p className="mt-1 text-xs text-secondary-500">Reference: {document.scannerReference}</p> : null}{canReadScanDetail && document.scanDetail ? <p className="mt-1 text-xs text-secondary-500">{document.scanDetail}</p> : null}</td>
                <td className="p-3"><div className="flex flex-wrap gap-2"><Button size="sm" variant="outline" icon={<Download className="h-3.5 w-3.5" />} disabled={!document.downloadUrl || busyDocumentId === document.publicId} onClick={() => void download(document)}>Download</Button>{canRescan ? <Button size="sm" variant="outline" icon={<ShieldCheck className="h-3.5 w-3.5" />} loading={busyDocumentId === document.publicId} disabled={document.isContentDeleted} onClick={() => void rescan(document)}>Rescan</Button> : null}</div></td>
              </tr>)}</tbody>
            </table>
          </div>
          {loading ? <p className="p-4 text-sm text-secondary-500" aria-live="polite">Loading IDP documents…</p> : null}
          {!loading && !documents.length ? <EmptyState icon={<FileText className="h-6 w-6" />} title="No IDP documents" description={planPublicId ? 'No documents match the selected plan and filters.' : 'Select an IDP plan to view its documents.'} /> : null}
          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-secondary-200 p-3 text-xs text-secondary-500 dark:border-secondary-700"><span>{totalCount} document{totalCount === 1 ? '' : 's'}</span><span className="flex items-center gap-2"><Button size="sm" variant="ghost" disabled={loading || page <= 1} onClick={() => setPage(value => Math.max(1, value - 1))}>Previous</Button><span>Page {page} of {Math.max(totalPages, 1)}</span><Button size="sm" variant="ghost" disabled={loading || page >= totalPages} onClick={() => setPage(value => value + 1)}>Next</Button></span></div>
        </Card>
      </>}
    </div>
  </AppShell>;
}
