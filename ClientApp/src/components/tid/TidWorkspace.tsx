import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  createTidVersion,
  downloadTidSourceDocument,
  getMunicipalEmployeesPage,
  getTidConfiguration,
  getTidHistoryPage,
  getTidRegisterPage,
  rescanTidSourceDocument,
  updateTidConfiguration,
  uploadTidSourceDocument,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { MunicipalEmployeeDto, SaveTidVersionPayload, TidConfiguration, TidRegisterItem, TidVersion } from '../../types';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card, EmptyState } from '../ui';

const fieldClass = 'mt-1 w-full rounded border border-secondary-300 bg-white px-2 py-1.5 text-sm text-secondary-900 dark:border-secondary-700 dark:bg-secondary-900 dark:text-white';
const textAreaClass = `${fieldClass} min-h-20`;
const utcDate = () => new Date().toISOString().slice(0, 10);

const emptyDraft = (): SaveTidVersionPayload => ({
  indicatorDefinition: '', purpose: '', dataSource: '', collectionMethod: '', calculationMethod: '',
  numeratorDescription: null, denominatorDescription: null, limitations: null, assumptions: null,
  verificationMethod: '', responsibleEmployeePublicId: null, notes: null,
  effectiveFrom: `${utcDate()}T00:00:00.000Z`, previousVersionRowVersion: null, reason: '',
});

export function TidWorkspace() {
  const { pushToast } = useApp();
  const { canCreate, canRead, canUpdate, canExecute, canReadField } = useSecurity();
  const canConfigure = canExecute('TID.CONFIGURE');
  const canUpload = canExecute('TID.UPLOAD_SOURCE');
  const canRescan = canExecute('TID.RESCAN_SOURCE');
  const canReadSourceUploader = canReadField('TID', 'SourceUploadedByUserId') || canReadField('TID', 'SourceUploadedByName');
  const canReadSourceScannerProvider = canReadField('TID', 'SourceScannerProvider');
  const canReadSourceScannerReference = canReadField('TID', 'SourceScannerReference');
  const canReadSourceScanDetail = canReadField('TID', 'SourceScanDetail');
  const canReadCreator = canReadField('TID', 'CreatedByUserId');
  const [configuration, setConfiguration] = useState<TidConfiguration | null>(null);
  const [items, setItems] = useState<TidRegisterItem[]>([]);
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [employeePage, setEmployeePage] = useState(1);
  const [employeeTotalPages, setEmployeeTotalPages] = useState(0);
  const [employeeSearch, setEmployeeSearch] = useState('');
  const [selected, setSelected] = useState<TidRegisterItem | null>(null);
  const [history, setHistory] = useState<TidVersion[]>([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotalCount, setHistoryTotalCount] = useState(0);
  const [historyTotalPages, setHistoryTotalPages] = useState(0);
  const [historySearchInput, setHistorySearchInput] = useState('');
  const [historySearch, setHistorySearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('indicatorNumber');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [draft, setDraft] = useState<SaveTidVersionPayload>(emptyDraft);
  const [configurationReason, setConfigurationReason] = useState('');
  const [sourceTitle, setSourceTitle] = useState('');
  const [sourceFile, setSourceFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!canRead('TID')) return;
    const [configurationResult, registerResult] = await Promise.all([
      getTidConfiguration(),
      getTidRegisterPage({ page, pageSize: 25, search, sortBy, sortDirection }),
    ]);
    if (!configurationResult.success || !configurationResult.data) pushToast('error', configurationResult.message ?? 'Unable to load TID configuration.');
    else setConfiguration(configurationResult.data);
    if (!registerResult.success) pushToast('error', registerResult.message ?? 'Unable to load the TID register.');
    else {
      setItems(registerResult.data?.items ?? []);
      setTotalCount(registerResult.data?.totalCount ?? 0);
      setTotalPages(registerResult.data?.totalPages ?? 0);
    }
  }, [canRead, page, pushToast, search, sortBy, sortDirection]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    if (searchInput.trim() === search) return;
    const timeout = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()); setSelected(null); setHistory([]); }, 300);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput]);
  useEffect(() => {
    if (!canCreate('TID') && !canUpdate('TID')) return;
    void getMunicipalEmployeesPage({ page: employeePage, pageSize: 25, search: employeeSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(result => {
      setEmployees(result.data?.items ?? []);
      setEmployeeTotalPages(result.data?.totalPages ?? 0);
    });
  }, [canCreate, canUpdate, employeePage, employeeSearch]);

  const selectItem = useCallback(async (item: TidRegisterItem, requestedPage = 1, requestedSearch = '') => {
    setSelected(item);
    setHistoryPage(requestedPage);
    if (requestedPage === 1 && !requestedSearch) {
      setHistorySearch('');
      setHistorySearchInput('');
    }
    const result = await getTidHistoryPage(item.targetPublicId, {
      page: requestedPage, pageSize: 10, search: requestedSearch || undefined, sortBy: 'versionNumber', sortDirection: 'desc',
    });
    if (!result.success) {
      pushToast('error', result.message ?? 'Unable to load TID history.');
      return;
    }
    const versions = result.data?.items ?? [];
    setHistory(versions);
    setHistoryTotalCount(result.data?.totalCount ?? 0);
    setHistoryTotalPages(result.data?.totalPages ?? 0);
    const current = versions.find(version => version.isCurrent) ?? item.currentVersion;
    setDraft(current ? {
      indicatorDefinition: current.indicatorDefinition,
      purpose: current.purpose,
      dataSource: current.dataSource,
      collectionMethod: current.collectionMethod,
      calculationMethod: current.calculationMethod,
      numeratorDescription: current.numeratorDescription ?? null,
      denominatorDescription: current.denominatorDescription ?? null,
      limitations: current.limitations ?? null,
      assumptions: current.assumptions ?? null,
      verificationMethod: current.verificationMethod,
      responsibleEmployeePublicId: current.responsibleEmployeePublicId ?? null,
      notes: current.notes ?? null,
      effectiveFrom: `${utcDate()}T00:00:00.000Z`,
      previousVersionRowVersion: current.rowVersion,
      reason: '',
    } : emptyDraft());
  }, [pushToast]);

  useEffect(() => {
    const normalized = historySearchInput.trim();
    if (normalized === historySearch) return;
    const timeout = window.setTimeout(() => {
      setHistorySearch(normalized);
      if (selected) void selectItem(selected, 1, normalized);
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [historySearch, historySearchInput, selectItem, selected]);

  const saveConfiguration = async () => {
    if (!configuration || !configurationReason.trim()) return pushToast('error', 'A configuration reason is required.');
    setBusy(true);
    try {
      const result = await updateTidConfiguration({
        tidEnabled: configuration.tidEnabled,
        allKpisRequired: configuration.tidEnabled && configuration.allKpisRequired,
        rowVersion: configuration.rowVersion,
        reason: configurationReason.trim(),
      });
      if (!result.success || !result.data) return pushToast('error', result.message ?? 'Unable to update TID configuration.');
      setConfiguration(result.data);
      setConfigurationReason('');
      pushToast('success', 'TID configuration updated.');
      await load();
    } finally { setBusy(false); }
  };

  const saveVersion = async () => {
    if (!selected) return;
    if (!draft.reason.trim()) return pushToast('error', 'A version reason is required.');
    setBusy(true);
    try {
      const result = await createTidVersion(selected.targetPublicId, {
        ...draft,
        effectiveFrom: draft.effectiveFrom.length === 10 ? `${draft.effectiveFrom}T00:00:00.000Z` : draft.effectiveFrom,
        numeratorDescription: draft.numeratorDescription || null,
        denominatorDescription: draft.denominatorDescription || null,
        limitations: draft.limitations || null,
        assumptions: draft.assumptions || null,
        notes: draft.notes || null,
      });
      if (!result.success || !result.data) return pushToast('error', result.message ?? 'Unable to create the TID version.');
      pushToast('success', `TID version ${result.data.versionNumber} created.`);
      await load();
      await selectItem({ ...selected, currentVersion: result.data });
    } finally { setBusy(false); }
  };

  const uploadSource = async () => {
    const current = history.find(version => version.isCurrent) ?? selected?.currentVersion;
    if (!current || !sourceFile || !sourceTitle.trim()) return pushToast('error', 'Select a file and provide its title.');
    setBusy(true);
    try {
      const result = await uploadTidSourceDocument(current.publicId, sourceFile, sourceTitle.trim());
      if (!result.success) return pushToast('error', result.message ?? 'Unable to upload the source document.');
      setSourceFile(null); setSourceTitle('');
      pushToast(result.data?.isQuarantined ? 'info' : 'success', result.message ?? 'Source document uploaded.');
      if (selected) await selectItem(selected);
    } finally { setBusy(false); }
  };

  const currentVersion = useMemo(() => history.find(version => version.isCurrent) ?? selected?.currentVersion ?? null, [history, selected]);
  const canSaveVersion = selected && (selected.currentVersion ? canUpdate('TID') : canCreate('TID'));

  return (
    <AppShell title="Technical Indicator Descriptions" subtitle="Optional, versioned KPI methodology without replacing operational target, actual, unit, POE, or workflow data">
      <div className="space-y-4">
        {configuration ? (
          <Card>
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="font-semibold text-secondary-900 dark:text-white">Municipality TID policy</h2>
                <p className="text-xs text-secondary-500">{configuration.currentTidCount} of {configuration.scopedKpiCount} scoped KPIs have a current TID.</p>
              </div>
              <div className="flex gap-2">
                <Badge variant={configuration.tidEnabled ? 'success' : 'default'}>{configuration.tidEnabled ? 'Enabled' : 'Optional module disabled'}</Badge>
                {configuration.allKpisRequired ? <Badge variant={configuration.missingTidCount ? 'warning' : 'success'}>Missing {configuration.missingTidCount}</Badge> : null}
              </div>
            </div>
            {canConfigure ? (
              <div className="mt-3 grid gap-3 md:grid-cols-[auto_auto_1fr_auto] md:items-end">
                <label className="flex items-center gap-2 text-sm"><input aria-label="Enable TID" type="checkbox" checked={configuration.tidEnabled} onChange={event => setConfiguration({ ...configuration, tidEnabled: event.target.checked, allKpisRequired: event.target.checked && configuration.allKpisRequired })} /> Enable TID</label>
                <label className="flex items-center gap-2 text-sm"><input aria-label="Require TID for all KPIs" type="checkbox" disabled={!configuration.tidEnabled} checked={configuration.allKpisRequired} onChange={event => setConfiguration({ ...configuration, allKpisRequired: event.target.checked })} /> Require for all KPIs</label>
                <label className="text-xs text-secondary-600">Configuration reason<input aria-label="TID configuration reason" className={fieldClass} value={configurationReason} onChange={event => setConfigurationReason(event.target.value)} /></label>
                <Button disabled={busy} onClick={() => void saveConfiguration()}>Save Policy</Button>
              </div>
            ) : null}
          </Card>
        ) : null}

        {configuration?.tidEnabled ? (
          <div className="grid gap-4 xl:grid-cols-[minmax(20rem,0.8fr)_minmax(32rem,1.4fr)]">
            <Card>
              <div className="grid gap-2 sm:grid-cols-2">
                <label className="text-xs text-secondary-600 sm:col-span-2">Search KPIs<input aria-label="Search TID KPIs" className={fieldClass} value={searchInput} onChange={event => setSearchInput(event.target.value)} /></label>
                <label className="text-xs text-secondary-600">Sort<select aria-label="Sort TID KPIs" className={fieldClass} value={sortBy} onChange={event => { setSortBy(event.target.value); setPage(1); setSelected(null); setHistory([]); }}><option value="indicatorNumber">Indicator number</option><option value="targetName">Target name</option><option value="department">Department</option><option value="unit">Unit</option><option value="createdAt">Created</option></select></label>
                <label className="text-xs text-secondary-600">Direction<select aria-label="TID KPI sort direction" className={fieldClass} value={sortDirection} onChange={event => { setSortDirection(event.target.value as 'asc' | 'desc'); setPage(1); setSelected(null); setHistory([]); }}><option value="asc">Ascending</option><option value="desc">Descending</option></select></label>
              </div>
              <div className="mt-3 max-h-[42rem] space-y-2 overflow-auto">
                {items.map(item => (
                  <button key={item.targetPublicId} className={`w-full rounded border p-3 text-left ${selected?.targetPublicId === item.targetPublicId ? 'border-primary-500 bg-primary-50 dark:bg-primary-950/20' : 'border-secondary-200 dark:border-secondary-700'}`} onClick={() => void selectItem(item)}>
                    <div className="flex items-start justify-between gap-2"><span className="font-medium">{item.indicatorNumber}</span><Badge variant={item.currentVersion ? 'success' : item.tidRequired ? 'warning' : 'default'}>{item.currentVersion ? `v${item.currentVersion.versionNumber}` : 'No TID'}</Badge></div>
                    <p className="mt-1 text-sm text-secondary-700 dark:text-secondary-300">{item.targetName}</p>
                    <p className="text-xs text-secondary-500">{item.departmentName ?? 'No department'}{item.unitName ? ` / ${item.unitName}` : ''}</p>
                  </button>
                ))}
                {!items.length ? <EmptyState title="No authorised KPIs" description="No active KPI falls within your current TID read scope." /> : null}
              </div>
              {totalPages > 1 ? <div className="mt-3 flex items-center justify-between"><p className="text-xs text-secondary-500">Page {page} of {totalPages} · {totalCount} KPIs</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={busy || page === 1} onClick={() => { setSelected(null); setHistory([]); setPage(value => Math.max(1, value - 1)); }}>Previous</Button><Button variant="outline" size="sm" disabled={busy || page === totalPages} onClick={() => { setSelected(null); setHistory([]); setPage(value => Math.min(totalPages, value + 1)); }}>Next</Button></div></div> : null}
            </Card>

            <div className="space-y-4">
              {!selected ? <Card><EmptyState title="Select a KPI" description="Choose a KPI to view its current technical description and complete version history." /></Card> : (
                <>
                  <Card>
                    <div className="flex items-start justify-between"><div><h2 className="font-semibold">{selected.indicatorNumber} — {selected.targetName}</h2><p className="text-xs text-secondary-500">New versions preserve prior methodology and require optimistic concurrency.</p></div>{currentVersion ? <Badge variant="primary">Current v{currentVersion.versionNumber}</Badge> : null}</div>
                    {canSaveVersion ? (
                      <div className="mt-4 grid gap-3 md:grid-cols-2">
                        <label className="text-xs text-secondary-600 md:col-span-2">Indicator definition<textarea aria-label="TID indicator definition" className={textAreaClass} value={draft.indicatorDefinition} onChange={event => setDraft({ ...draft, indicatorDefinition: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600 md:col-span-2">Purpose<textarea aria-label="TID purpose" className={textAreaClass} value={draft.purpose} onChange={event => setDraft({ ...draft, purpose: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Data source<textarea aria-label="TID data source" className={textAreaClass} value={draft.dataSource} onChange={event => setDraft({ ...draft, dataSource: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Collection method<textarea aria-label="TID collection method" className={textAreaClass} value={draft.collectionMethod} onChange={event => setDraft({ ...draft, collectionMethod: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600 md:col-span-2">Calculation method<textarea aria-label="TID calculation method" className={textAreaClass} value={draft.calculationMethod} onChange={event => setDraft({ ...draft, calculationMethod: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Numerator description<textarea aria-label="TID numerator" className={textAreaClass} value={draft.numeratorDescription ?? ''} onChange={event => setDraft({ ...draft, numeratorDescription: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Denominator description<textarea aria-label="TID denominator" className={textAreaClass} value={draft.denominatorDescription ?? ''} onChange={event => setDraft({ ...draft, denominatorDescription: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Limitations<textarea aria-label="TID limitations" className={textAreaClass} value={draft.limitations ?? ''} onChange={event => setDraft({ ...draft, limitations: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Assumptions<textarea aria-label="TID assumptions" className={textAreaClass} value={draft.assumptions ?? ''} onChange={event => setDraft({ ...draft, assumptions: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600 md:col-span-2">Verification method<textarea aria-label="TID verification method" className={textAreaClass} value={draft.verificationMethod} onChange={event => setDraft({ ...draft, verificationMethod: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600">Search responsible employees<input aria-label="Search TID responsible employees" className={fieldClass} value={employeeSearch} onChange={event => { setEmployeeSearch(event.target.value); setEmployeePage(1); }} /></label>
                        <label className="text-xs text-secondary-600">Responsible employee<select aria-label="TID responsible employee" className={fieldClass} value={draft.responsibleEmployeePublicId ?? ''} onChange={event => setDraft({ ...draft, responsibleEmployeePublicId: event.target.value || null })}><option value="">Not assigned</option>{draft.responsibleEmployeePublicId && !employees.some(item => item.publicId === draft.responsibleEmployeePublicId) ? <option value={draft.responsibleEmployeePublicId}>{currentVersion?.responsibleEmployeeName ?? 'Current responsible employee'}</option> : null}{employees.map(employee => <option key={employee.publicId} value={employee.publicId}>{employee.employeeNumber ? `${employee.employeeNumber} — ` : ''}{employee.firstName} {employee.lastName}</option>)}</select></label>
                        {employeeTotalPages > 1 ? <div className="flex items-center gap-2 text-xs text-secondary-500 md:col-span-2"><Button size="sm" variant="outline" disabled={employeePage <= 1} onClick={() => setEmployeePage(value => Math.max(1, value - 1))}>Previous employees</Button><span>Page {employeePage} of {employeeTotalPages}</span><Button size="sm" variant="outline" disabled={employeePage >= employeeTotalPages} onClick={() => setEmployeePage(value => value + 1)}>Next employees</Button></div> : null}
                        <label className="text-xs text-secondary-600">Effective from<input aria-label="TID effective from" type="date" className={fieldClass} value={draft.effectiveFrom.slice(0, 10)} onChange={event => setDraft({ ...draft, effectiveFrom: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600 md:col-span-2">Notes<textarea aria-label="TID notes" className={textAreaClass} value={draft.notes ?? ''} onChange={event => setDraft({ ...draft, notes: event.target.value })} /></label>
                        <label className="text-xs text-secondary-600 md:col-span-2">Version reason<input aria-label="TID version reason" className={fieldClass} value={draft.reason} onChange={event => setDraft({ ...draft, reason: event.target.value })} /></label>
                        <div className="md:col-span-2"><Button disabled={busy} onClick={() => void saveVersion()}>{currentVersion ? 'Create Successor Version' : 'Create Initial TID'}</Button></div>
                      </div>
                    ) : null}
                  </Card>

                  {currentVersion && canUpload ? (
                    <Card>
                      <h3 className="font-semibold">Governed source document</h3>
                      <div className="mt-3 grid gap-3 md:grid-cols-[1fr_1fr_auto] md:items-end">
                        <label className="text-xs text-secondary-600">Document title<input aria-label="TID source document title" className={fieldClass} value={sourceTitle} onChange={event => setSourceTitle(event.target.value)} /></label>
                        <label className="text-xs text-secondary-600">Source file<input aria-label="TID source document file" type="file" accept=".pdf,.docx,.xlsx,.png,.jpg,.jpeg" className={fieldClass} onChange={event => setSourceFile(event.target.files?.[0] ?? null)} /></label>
                        <Button disabled={busy} onClick={() => void uploadSource()}>Upload</Button>
                      </div>
                    </Card>
                  ) : null}

                  <Card>
                    <div className="flex flex-wrap items-end justify-between gap-3"><div><h3 className="font-semibold">Version history</h3><p className="text-xs text-secondary-500">{historyTotalCount} immutable version{historyTotalCount === 1 ? '' : 's'}</p></div><label className="text-xs text-secondary-600">Search history<input aria-label="Search TID version history" className={fieldClass} value={historySearchInput} onChange={event => setHistorySearchInput(event.target.value)} /></label></div>
                    <div className="mt-3 space-y-3">
                      {history.map(version => (
                        <div key={version.publicId} className="rounded border border-secondary-200 p-3 text-sm dark:border-secondary-700">
                          <div className="flex flex-wrap items-center justify-between gap-2"><span className="font-medium">Version {version.versionNumber}</span><div className="flex gap-2">{version.isCurrent ? <Badge variant="success">Current</Badge> : <Badge>Superseded</Badge>}<span className="text-xs text-secondary-500">Effective {new Date(version.effectiveFrom).toLocaleDateString()}{version.effectiveTo ? ` – ${new Date(version.effectiveTo).toLocaleDateString()}` : ''}</span></div></div>
                          <p className="mt-2"><strong>Definition:</strong> {version.indicatorDefinition}</p>
                          <p><strong>Calculation:</strong> {version.calculationMethod}</p>
                          <p><strong>Responsible:</strong> {version.responsibleEmployeeName ?? 'Not assigned'}</p>
                          {canReadCreator && version.createdByUserPublicId ? <p><strong>Created by:</strong> {version.createdByName || version.createdByUserPublicId}</p> : null}
                          <div className="mt-2 space-y-1">{version.sourceDocuments.map(document => <div key={document.publicId} className="flex items-start justify-between gap-3 rounded bg-secondary-50 px-2 py-1 dark:bg-secondary-800"><div><span>{document.title} ({document.fileName})</span>{canReadSourceUploader ? <p className="text-xs text-secondary-500">Uploaded by {document.uploadedByName ?? document.uploadedByUserPublicId ?? 'Unavailable'}</p> : null}{canReadSourceScannerProvider && document.scannerProvider ? <p className="text-xs text-secondary-500">Provider: {document.scannerProvider}</p> : null}{canReadSourceScannerReference && document.scannerReference ? <p className="text-xs text-secondary-500">Reference: {document.scannerReference}</p> : null}{canReadSourceScanDetail && document.scanDetail ? <p className="text-xs text-secondary-500">{document.scanDetail}</p> : null}</div><div className="flex items-center gap-2"><Badge variant={document.isQuarantined ? 'warning' : 'success'}>{document.scanStatus}</Badge>{!document.isQuarantined && document.scanStatus === 'Clean' ? <Button size="sm" variant="ghost" onClick={() => void downloadTidSourceDocument(document)}>Download</Button> : canRescan ? <Button size="sm" variant="ghost" onClick={() => void rescanTidSourceDocument(version.publicId, document.publicId).then(() => selected && selectItem(selected))}>Rescan</Button> : null}</div></div>)}</div>
                        </div>
                      ))}
                      {!history.length ? <p className="text-sm text-secondary-500">No TID version exists for this KPI.</p> : null}
                    </div>
                    {historyTotalPages > 1 ? <div className="mt-3 flex items-center justify-between gap-2 text-xs"><Button size="sm" variant="outline" disabled={busy || historyPage <= 1} onClick={() => selected && void selectItem(selected, historyPage - 1, historySearch)}>Previous versions</Button><span>Page {historyPage} of {historyTotalPages}</span><Button size="sm" variant="outline" disabled={busy || historyPage >= historyTotalPages} onClick={() => selected && void selectItem(selected, historyPage + 1, historySearch)}>Next versions</Button></div> : null}
                  </Card>
                </>
              )}
            </div>
          </div>
        ) : configuration ? <Card><EmptyState title="TID is disabled" description="Core OPMS remains available. An authorised administrator may enable this optional municipality module." /></Card> : null}
      </div>
    </AppShell>
  );
}
