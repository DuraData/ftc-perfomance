import { useCallback, useEffect, useState } from 'react';
import {
  approveStrategicDocument,
  createStrategicDocumentType,
  createStrategicDocumentVersion,
  downloadStrategicDocument,
  getStrategicDocumentHistory,
  getStrategicDocumentsPage,
  getStrategicDocumentTypesPage,
  publishStrategicDocument,
  rescanStrategicDocument,
  retireStrategicDocument,
  updateStrategicDocumentType,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { SaveStrategicDocumentVersionPayload, StrategicDocument, StrategicDocumentType } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';
import { StrategicDocumentTypePicker } from './StrategicDocumentTypePicker';

const fieldClass = 'mt-1 w-full rounded border border-secondary-300 bg-white px-2 py-1.5 text-sm text-secondary-900 dark:border-secondary-700 dark:bg-secondary-900 dark:text-white';
const areaClass = `${fieldClass} min-h-20`;
const today = () => new Date().toISOString().slice(0, 10);

const emptyDocument = (): SaveStrategicDocumentVersionPayload => ({
  municipalityFinancialYearPublicId: '', documentTypePublicId: '', previousVersionPublicId: null,
  previousVersionRowVersion: null, sdbipLayer: null, title: '', description: null,
  documentDate: today(), displayOrder: 0, externalUrl: null, file: null, reason: '',
});

const emptyType = () => ({
  publicId: '', code: '', name: '', description: '', allowsExternalLinks: false, isActive: true,
  displayOrder: 0, rowVersion: '', reason: '',
});

export function StrategicDocumentsWorkspace() {
  const { pushToast } = useApp();
  const { canCreate, canRead, canUpdate, canExecute } = useSecurity();
  const canManage = canCreate('STRATEGIC_DOCUMENT') || canUpdate('STRATEGIC_DOCUMENT');
  const canManageTypes = canExecute('STRATEGIC_DOCUMENT.MANAGE_TYPES');
  const [types, setTypes] = useState<StrategicDocumentType[]>([]);
  const [typePage, setTypePage] = useState(1);
  const [typeTotalCount, setTypeTotalCount] = useState(0);
  const [typeTotalPages, setTypeTotalPages] = useState(0);
  const [typeSearchInput, setTypeSearchInput] = useState('');
  const [typeSearch, setTypeSearch] = useState('');
  const [typeSortBy, setTypeSortBy] = useState('displayOrder');
  const [typeSortDirection, setTypeSortDirection] = useState<'asc' | 'desc'>('asc');
  const [documents, setDocuments] = useState<StrategicDocument[]>([]);
  const [history, setHistory] = useState<StrategicDocument[]>([]);
  const [selected, setSelected] = useState<StrategicDocument | null>(null);
  const [yearFilter, setYearFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [draft, setDraft] = useState<SaveStrategicDocumentVersionPayload>(emptyDocument);
  const [selectedType, setSelectedType] = useState<StrategicDocumentType>();
  const [typeDraft, setTypeDraft] = useState(emptyType);
  const [contentMode, setContentMode] = useState<'file' | 'link'>('file');
  const [actionReason, setActionReason] = useState('');
  const [approvalReference, setApprovalReference] = useState('');
  const [publicationDate, setPublicationDate] = useState(today());
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!canRead('STRATEGIC_DOCUMENT')) return;
    const [typeResult, documentResult] = await Promise.all([
      canManageTypes
        ? getStrategicDocumentTypesPage(
          { page: typePage, pageSize: 25, search: typeSearch, sortBy: typeSortBy, sortDirection: typeSortDirection },
        )
        : Promise.resolve(null),
      getStrategicDocumentsPage(
        { page, pageSize: 25, search, sortBy, sortDirection },
        { municipalityFinancialYearPublicId: yearFilter || undefined },
      ),
    ]);
    if (typeResult && !typeResult.success) pushToast('error', typeResult.message ?? 'Unable to load strategic-document types.');
    else if (typeResult) {
      setTypes(typeResult.data?.items ?? []);
      setTypeTotalCount(typeResult.data?.totalCount ?? 0);
      setTypeTotalPages(typeResult.data?.totalPages ?? 0);
    }
    if (!documentResult.success) pushToast('error', documentResult.message ?? 'Unable to load strategic documents.');
    else {
      setDocuments(documentResult.data?.items ?? []);
      setTotalCount(documentResult.data?.totalCount ?? 0);
      setTotalPages(documentResult.data?.totalPages ?? 0);
    }
  }, [canManageTypes, canRead, page, pushToast, search, sortBy, sortDirection, typePage, typeSearch, typeSortBy, typeSortDirection, yearFilter]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    const nextSearch = searchInput.trim();
    if (nextSearch === search) return;
    const timeout = window.setTimeout(() => { setPage(1); setSearch(nextSearch); setSelected(null); }, 300);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput]);
  useEffect(() => {
    const nextSearch = typeSearchInput.trim();
    if (nextSearch === typeSearch) return;
    const timeout = window.setTimeout(() => { setTypePage(1); setTypeSearch(nextSearch); }, 300);
    return () => window.clearTimeout(timeout);
  }, [typeSearch, typeSearchInput]);

  const selectType = useCallback((publicId: string, option?: StrategicDocumentType) => {
    setDraft(current => ({ ...current, documentTypePublicId: publicId }));
    setSelectedType(option);
  }, []);

  const selectDocument = async (document: StrategicDocument) => {
    setSelected(document);
    if (!canManage) return;
    const result = await getStrategicDocumentHistory(document.documentFamilyId);
    if (!result.success) return pushToast('error', result.message ?? 'Unable to load version history.');
    setHistory(result.data ?? []);
  };

  const startSuccessor = (document: StrategicDocument) => {
    setSelected(document);
    setSelectedType(undefined);
    setContentMode(document.externalUrl ? 'link' : 'file');
    setDraft({
      municipalityFinancialYearPublicId: document.municipalityFinancialYearPublicId,
      documentTypePublicId: document.documentTypePublicId,
      previousVersionPublicId: document.publicId,
      previousVersionRowVersion: document.rowVersion,
      sdbipLayer: document.sdbipLayer ?? null,
      title: document.title,
      description: document.description ?? null,
      documentDate: today(),
      displayOrder: document.displayOrder,
      externalUrl: document.externalUrl ?? null,
      file: null,
      reason: '',
    });
  };

  const saveVersion = async () => {
    if (!draft.municipalityFinancialYearPublicId || !draft.documentTypePublicId || !draft.title.trim() || !draft.reason.trim())
      return pushToast('error', 'Financial year, type, title, and governance reason are required.');
    if (contentMode === 'file' && !draft.file) return pushToast('error', 'Select a managed document file.');
    if (contentMode === 'link' && !draft.externalUrl?.trim()) return pushToast('error', 'Provide an approved HTTPS link.');
    setBusy(true);
    try {
      const result = await createStrategicDocumentVersion({
        ...draft,
        title: draft.title.trim(),
        description: draft.description?.trim() || null,
        sdbipLayer: draft.sdbipLayer?.trim() || null,
        documentDate: draft.documentDate.length === 10 ? `${draft.documentDate}T00:00:00.000Z` : draft.documentDate,
        externalUrl: contentMode === 'link' ? draft.externalUrl?.trim() || null : null,
        file: contentMode === 'file' ? draft.file : null,
        reason: draft.reason.trim(),
      });
      if (!result.success || !result.data) return pushToast('error', result.message ?? 'Unable to create the strategic-document version.');
      pushToast(result.data.isQuarantined ? 'info' : 'success', result.message ?? `Version ${result.data.versionNumber} created.`);
      setDraft(emptyDocument());
      setSelectedType(undefined);
      setSelected(result.data);
      await load();
      const historyResult = await getStrategicDocumentHistory(result.data.documentFamilyId);
      setHistory(historyResult.data ?? []);
    } finally { setBusy(false); }
  };

  const saveType = async () => {
    if (!typeDraft.code.trim() || !typeDraft.name.trim() || !typeDraft.reason.trim()) return pushToast('error', 'Type code, name, and reason are required.');
    setBusy(true);
    try {
      const payload = {
        code: typeDraft.code.trim(), name: typeDraft.name.trim(), description: typeDraft.description.trim() || null,
        allowsExternalLinks: typeDraft.allowsExternalLinks, isActive: typeDraft.isActive,
        displayOrder: typeDraft.displayOrder, reason: typeDraft.reason.trim(),
      };
      const result = typeDraft.publicId
        ? await updateStrategicDocumentType(typeDraft.publicId, { ...payload, rowVersion: typeDraft.rowVersion })
        : await createStrategicDocumentType(payload);
      if (!result.success) return pushToast('error', result.message ?? 'Unable to save the controlled document type.');
      pushToast('success', 'Strategic-document type saved.');
      setTypeDraft(emptyType());
      await load();
    } finally { setBusy(false); }
  };

  const runAction = async (action: 'approve' | 'publish' | 'retire' | 'rescan') => {
    if (!selected) return;
    if (action !== 'rescan' && !actionReason.trim()) return pushToast('error', 'A governance reason is required.');
    if (action === 'approve' && !approvalReference.trim()) return pushToast('error', 'An approval reference is required.');
    setBusy(true);
    try {
      const result = action === 'approve'
        ? await approveStrategicDocument(selected.publicId, { rowVersion: selected.rowVersion, approvalReference: approvalReference.trim(), reason: actionReason.trim() })
        : action === 'publish'
          ? await publishStrategicDocument(selected.publicId, { rowVersion: selected.rowVersion, publicationDate: `${publicationDate}T00:00:00.000Z`, reason: actionReason.trim() })
          : action === 'retire'
            ? await retireStrategicDocument(selected.publicId, { rowVersion: selected.rowVersion, reason: actionReason.trim() })
            : await rescanStrategicDocument(selected.publicId);
      if (!result.success || !result.data) return pushToast('error', result.message ?? `Unable to ${action} the document.`);
      setSelected(result.data);
      setActionReason('');
      setApprovalReference('');
      pushToast(result.data.isQuarantined ? 'info' : 'success', result.message ?? `Document ${action} completed.`);
      await load();
      const historyResult = await getStrategicDocumentHistory(result.data.documentFamilyId);
      setHistory(historyResult.data ?? []);
    } finally { setBusy(false); }
  };

  return (
    <AppShell title="Strategic Documents" subtitle="Controlled municipality and financial-year publications with governed versions, approval, and private file delivery">
      <div className="space-y-4">
        <Card>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[1fr_1fr_12rem_9rem_auto] md:items-end">
            <CalendarMasterPicker kind="municipality-financial-year" label="Strategic document financial year filter" emptyLabel="All active years" value={yearFilter} onChange={value => { setYearFilter(value); setPage(1); setSelected(null); }} />
            <label className="text-xs text-secondary-600">Search<input aria-label="Search strategic documents" className={fieldClass} value={searchInput} onChange={event => setSearchInput(event.target.value)} /></label>
            <label className="text-xs text-secondary-600">Sort<select aria-label="Sort strategic documents" className={fieldClass} value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); setSelected(null); }}><option value="createdAt">Created</option><option value="title">Title</option><option value="documentDate">Document date</option><option value="financialYear">Financial year</option><option value="displayOrder">Display order</option><option value="versionNumber">Version</option></select></label>
            <label className="text-xs text-secondary-600">Direction<select aria-label="Strategic document sort direction" className={fieldClass} value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); setSelected(null); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label>
            <Button variant="outline" onClick={() => void load()}>Refresh</Button>
          </div>
        </Card>

        {canManageTypes ? (
          <Card>
            <h2 className="font-semibold">Controlled document types</h2>
            <div className="mt-3 grid gap-3 md:grid-cols-[1fr_12rem_10rem]">
              <label className="text-xs text-secondary-600">Search types<input aria-label="Search document types" className={fieldClass} value={typeSearchInput} onChange={event => setTypeSearchInput(event.target.value)} /></label>
              <label className="text-xs text-secondary-600">Sort types<select aria-label="Sort document types" className={fieldClass} value={typeSortBy} onChange={event => { setTypeSortBy(event.target.value); setTypePage(1); }}><option value="displayOrder">Display order</option><option value="code">Code</option><option value="name">Name</option><option value="status">Status</option></select></label>
              <label className="text-xs text-secondary-600">Direction<select aria-label="Document type sort direction" className={fieldClass} value={typeSortDirection} onChange={event => { setTypeSortDirection(event.target.value as 'asc' | 'desc'); setTypePage(1); }}><option value="asc">Ascending</option><option value="desc">Descending</option></select></label>
            </div>
            <div className="mt-3 grid gap-3 md:grid-cols-4">
              <label className="text-xs text-secondary-600">Code<input aria-label="Document type code" className={fieldClass} value={typeDraft.code} onChange={event => setTypeDraft({ ...typeDraft, code: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Name<input aria-label="Document type name" className={fieldClass} value={typeDraft.name} onChange={event => setTypeDraft({ ...typeDraft, name: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Display order<input aria-label="Document type display order" type="number" min="0" className={fieldClass} value={typeDraft.displayOrder} onChange={event => setTypeDraft({ ...typeDraft, displayOrder: Number(event.target.value) })} /></label>
              <div className="flex items-end gap-4 pb-2"><label className="flex items-center gap-2 text-sm"><input aria-label="Allow external links" type="checkbox" checked={typeDraft.allowsExternalLinks} onChange={event => setTypeDraft({ ...typeDraft, allowsExternalLinks: event.target.checked })} /> External links</label><label className="flex items-center gap-2 text-sm"><input aria-label="Document type active" type="checkbox" checked={typeDraft.isActive} onChange={event => setTypeDraft({ ...typeDraft, isActive: event.target.checked })} /> Active</label></div>
              <label className="text-xs text-secondary-600 md:col-span-2">Description<input aria-label="Document type description" className={fieldClass} value={typeDraft.description} onChange={event => setTypeDraft({ ...typeDraft, description: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Governance reason<input aria-label="Document type reason" className={fieldClass} value={typeDraft.reason} onChange={event => setTypeDraft({ ...typeDraft, reason: event.target.value })} /></label>
              <div className="self-end"><Button disabled={busy} onClick={() => void saveType()}>{typeDraft.publicId ? 'Update Type' : 'Add Type'}</Button></div>
            </div>
            <div className="mt-3 flex flex-wrap gap-2">{types.map(type => <button key={type.publicId} className="rounded border border-secondary-200 px-2 py-1 text-xs dark:border-secondary-700" onClick={() => setTypeDraft({ ...type, description: type.description ?? '', reason: '' })}>{type.code} · {type.name}{!type.isActive ? ' (inactive)' : ''}</button>)}</div>
            <div className="mt-3 flex items-center justify-between gap-2 text-xs text-secondary-500" aria-live="polite"><span>{typeTotalCount} controlled document type{typeTotalCount === 1 ? '' : 's'}</span><span className="flex items-center gap-2"><Button type="button" size="sm" variant="ghost" disabled={busy || typePage <= 1} onClick={() => setTypePage(current => Math.max(1, current - 1))}>Previous controlled types</Button><span>Page {typePage} of {Math.max(typeTotalPages, 1)}</span><Button type="button" size="sm" variant="ghost" disabled={busy || typePage >= typeTotalPages} onClick={() => setTypePage(current => current + 1)}>Next controlled types</Button></span></div>
          </Card>
        ) : null}

        {canManage ? (
          <Card>
            <div className="flex items-start justify-between gap-3"><div><h2 className="font-semibold">{draft.previousVersionPublicId ? 'Create successor version' : 'Add strategic document'}</h2><p className="text-xs text-secondary-500">A successor preserves its predecessor and becomes the administrative current version. Existing published content remains visible until the successor is published.</p></div>{draft.previousVersionPublicId ? <Button variant="ghost" onClick={() => setDraft(emptyDocument())}>Cancel successor</Button> : null}</div>
            <div className="mt-3 grid gap-3 md:grid-cols-3">
              <CalendarMasterPicker kind="municipality-financial-year" label="Strategic document financial year" value={draft.municipalityFinancialYearPublicId} selectedLabel={selected?.financialYearCode} onChange={value => setDraft({ ...draft, municipalityFinancialYearPublicId: value })} required />
              <StrategicDocumentTypePicker value={draft.documentTypePublicId} selectedLabel={selected ? `${selected.documentTypeCode} · ${selected.documentTypeName}` : undefined} onChange={selectType} disabled={busy} />
              <label className="text-xs text-secondary-600">Optional SDBIP layer<input aria-label="Strategic document SDBIP layer" className={fieldClass} value={draft.sdbipLayer ?? ''} onChange={event => setDraft({ ...draft, sdbipLayer: event.target.value })} /></label>
              <label className="text-xs text-secondary-600 md:col-span-2">User-facing title<input aria-label="Strategic document title" className={fieldClass} value={draft.title} onChange={event => setDraft({ ...draft, title: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Document date<input aria-label="Strategic document date" type="date" className={fieldClass} value={draft.documentDate.slice(0, 10)} onChange={event => setDraft({ ...draft, documentDate: event.target.value })} /></label>
              <label className="text-xs text-secondary-600 md:col-span-2">Description<textarea aria-label="Strategic document description" className={areaClass} value={draft.description ?? ''} onChange={event => setDraft({ ...draft, description: event.target.value })} /></label>
              <label className="text-xs text-secondary-600">Display order<input aria-label="Strategic document display order" type="number" min="0" className={fieldClass} value={draft.displayOrder} onChange={event => setDraft({ ...draft, displayOrder: Number(event.target.value) })} /></label>
              <div className="md:col-span-3 flex gap-4"><label className="flex items-center gap-2 text-sm"><input aria-label="Use managed file" type="radio" checked={contentMode === 'file'} onChange={() => setContentMode('file')} /> Managed file</label><label className="flex items-center gap-2 text-sm"><input aria-label="Use approved external link" type="radio" disabled={!selectedType?.allowsExternalLinks} checked={contentMode === 'link'} onChange={() => setContentMode('link')} /> Approved HTTPS link</label></div>
              {contentMode === 'file' ? <label className="text-xs text-secondary-600 md:col-span-2">Managed file<input aria-label="Strategic document file" type="file" accept=".pdf,.docx,.xlsx,.pptx,.png,.jpg,.jpeg" className={fieldClass} onChange={event => setDraft({ ...draft, file: event.target.files?.[0] ?? null })} /></label> : <label className="text-xs text-secondary-600 md:col-span-2">Approved HTTPS URL<input aria-label="Strategic document external URL" type="url" className={fieldClass} value={draft.externalUrl ?? ''} onChange={event => setDraft({ ...draft, externalUrl: event.target.value })} /></label>}
              <label className="text-xs text-secondary-600">Version reason<input aria-label="Strategic document version reason" className={fieldClass} value={draft.reason} onChange={event => setDraft({ ...draft, reason: event.target.value })} /></label>
              <div className="md:col-span-3"><Button disabled={busy} onClick={() => void saveVersion()}>{draft.previousVersionPublicId ? 'Create Successor Version' : 'Create Initial Version'}</Button></div>
            </div>
          </Card>
        ) : null}

        <div className="grid gap-4 xl:grid-cols-[1.35fr_0.85fr]">
          <Card>
            <h2 className="font-semibold">{canManage ? 'Current document register' : 'Approved publications'}</h2>
            <div className="mt-3 space-y-3">
              {documents.map(document => (
                <button key={document.publicId} className={`w-full rounded border p-3 text-left ${selected?.publicId === document.publicId ? 'border-primary-500 bg-primary-50 dark:bg-primary-950/20' : 'border-secondary-200 dark:border-secondary-700'}`} onClick={() => void selectDocument(document)}>
                  <div className="flex flex-wrap items-start justify-between gap-2"><div><p className="font-medium">{document.title}</p><p className="text-xs text-secondary-500">{document.documentTypeName} · {document.financialYearCode}{document.sdbipLayer ? ` · ${document.sdbipLayer}` : ''}</p></div><div className="flex gap-2"><Badge variant="primary">v{document.versionNumber}</Badge>{document.isPublished ? <Badge variant="success">Published</Badge> : document.isApproved ? <Badge variant="warning">Approved</Badge> : <Badge>Draft</Badge>}{!document.isActive ? <Badge>Retired</Badge> : null}</div></div>
                  <p className="mt-2 text-sm text-secondary-700 dark:text-secondary-300">{document.description ?? 'No description supplied.'}</p>
                </button>
              ))}
              {!documents.length ? <EmptyState title="No strategic documents" description={canManage ? 'Create a controlled document version for an active municipality financial year.' : 'No active, approved, published document is available in this context.'} /> : null}
            </div>
            {totalPages > 1 ? <div className="mt-3 flex items-center justify-between"><p className="text-xs text-secondary-500">Page {page} of {totalPages} · {totalCount} documents</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={busy || page === 1} onClick={() => { setSelected(null); setPage(value => Math.max(1, value - 1)); }}>Previous</Button><Button variant="outline" size="sm" disabled={busy || page === totalPages} onClick={() => { setSelected(null); setPage(value => Math.min(totalPages, value + 1)); }}>Next</Button></div></div> : null}
          </Card>

          <Card>
            {!selected ? <EmptyState title="Select a document" description="Choose a document to view publication and content details." /> : (
              <div className="space-y-4">
                <div><h2 className="font-semibold">{selected.title}</h2><p className="text-xs text-secondary-500">Version {selected.versionNumber} · dated {new Date(selected.documentDate).toLocaleDateString()}</p></div>
                <div className="grid gap-2 text-sm"><p><strong>Approval:</strong> {selected.isApproved ? `${selected.approvalReference} · ${new Date(selected.approvedAt!).toLocaleString()}` : 'Pending'}</p><p><strong>Publication:</strong> {selected.isPublished ? new Date(selected.publicationDate!).toLocaleDateString() : 'Pending'}</p><p><strong>Content:</strong> {selected.fileName ?? selected.externalUrl ?? 'Unavailable'}</p>{selected.scanStatus ? <p><strong>Scan:</strong> {selected.scanStatus}{selected.isQuarantined ? ' (quarantined)' : ''}</p> : null}</div>
                <div className="flex flex-wrap gap-2">{selected.contentUrl && !selected.isQuarantined && selected.scanStatus === 'Clean' ? <Button variant="outline" onClick={() => void downloadStrategicDocument(selected)}>Download</Button> : null}{selected.externalUrl ? <a className="rounded bg-primary-600 px-3 py-2 text-sm text-white" href={selected.externalUrl} target="_blank" rel="noreferrer">Open publication</a> : null}{canManage && selected.isCurrent ? <Button variant="outline" onClick={() => startSuccessor(selected)}>New Version</Button> : null}</div>
                {canManage ? (
                  <div className="space-y-3 border-t border-secondary-200 pt-3 dark:border-secondary-700">
                    <label className="text-xs text-secondary-600">Action reason<input aria-label="Strategic document action reason" className={fieldClass} value={actionReason} onChange={event => setActionReason(event.target.value)} /></label>
                    {!selected.isApproved ? <label className="text-xs text-secondary-600">Approval reference<input aria-label="Strategic document approval reference" className={fieldClass} value={approvalReference} onChange={event => setApprovalReference(event.target.value)} /></label> : null}
                    {selected.isApproved && !selected.isPublished ? <label className="text-xs text-secondary-600">Publication date<input aria-label="Strategic document publication date" type="date" className={fieldClass} value={publicationDate} onChange={event => setPublicationDate(event.target.value)} /></label> : null}
                    <div className="flex flex-wrap gap-2">{!selected.isApproved && canExecute('STRATEGIC_DOCUMENT.APPROVE') ? <Button disabled={busy || selected.isQuarantined} onClick={() => void runAction('approve')}>Approve</Button> : null}{selected.isApproved && !selected.isPublished && canExecute('STRATEGIC_DOCUMENT.PUBLISH') ? <Button disabled={busy} onClick={() => void runAction('publish')}>Publish</Button> : null}{selected.isActive && canExecute('STRATEGIC_DOCUMENT.RETIRE') ? <Button variant="outline" disabled={busy} onClick={() => void runAction('retire')}>Retire</Button> : null}{selected.isQuarantined && canExecute('STRATEGIC_DOCUMENT.RESCAN') ? <Button variant="outline" disabled={busy} onClick={() => void runAction('rescan')}>Rescan</Button> : null}</div>
                  </div>
                ) : null}
                {canManage && history.length ? <div className="border-t border-secondary-200 pt-3 dark:border-secondary-700"><h3 className="text-sm font-semibold">Version and action history</h3><div className="mt-2 space-y-2">{history.map(version => <div key={version.publicId} className="rounded bg-secondary-50 p-2 text-xs dark:bg-secondary-800"><p className="font-medium">Version {version.versionNumber} · {version.isCurrent ? 'Current' : 'Superseded'}</p>{version.events.map(event => <p key={event.publicId}>{new Date(event.occurredAt).toLocaleString()} · {event.action} · {event.reason}</p>)}</div>)}</div></div> : null}
              </div>
            )}
          </Card>
        </div>
      </div>
    </AppShell>
  );
}
