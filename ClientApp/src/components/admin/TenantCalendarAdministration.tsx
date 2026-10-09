import { useCallback, useEffect, useState } from 'react';
import { CalendarDays, CheckCircle2, Plus, RefreshCw } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormPanel, Input, Select, Textarea } from '../common/Form';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import {
  createFinancialYearMaster,
  createMunicipalityFinancialYearMaster,
  createReportingPeriodMaster,
  createSdbipLayerMaster,
  getMunicipalityFinancialYearMastersPage,
  getReportingPeriodMastersPage,
  getSdbipLayerMastersPage,
  updateMunicipalityFinancialYearMaster,
  updateSdbipLayerMaster,
} from '../../api/api';
import type { MunicipalityFinancialYearMasterDto, ReportingPeriodMasterDto, SdbipLayerMasterDto } from '../../types';

const date = (value: string) => new Date(`${value}T00:00:00Z`).toISOString();
const today = () => new Date().toISOString().slice(0, 10);
type RegisterState = { page: number; totalPages: number; totalCount: number; searchInput: string; search: string; status: string; sort: string };
const registerState = (sort: string): RegisterState => ({ page: 1, totalPages: 0, totalCount: 0, searchInput: '', search: '', status: 'all', sort });
const queryFrom = (page: number, search: string, status: string, sort: string) => {
  const [sortBy, direction] = sort.split(':');
  const sortDirection: 'asc' | 'desc' = direction === 'asc' ? 'asc' : 'desc';
  return { page, pageSize: 25, search: search || undefined, sortBy, sortDirection, active: status === 'all' ? undefined : status === 'active' };
};

function CalendarPagination({ state, label, onPage }: { state: RegisterState; label: string; onPage: (page: number) => void }) {
  if (state.totalPages <= 1) return null;
  return <div className="mt-4 flex items-center justify-between text-xs text-secondary-500"><span>{label} page {state.page} of {state.totalPages} · {state.totalCount} records</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={state.page <= 1} onClick={() => onPage(Math.max(1, state.page - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={state.page >= state.totalPages} onClick={() => onPage(state.page + 1)}>Next</Button></div></div>;
}

function CalendarRegisterControls({ state, label, sorts, onChange }: { state: RegisterState; label: string; sorts: Array<{ value: string; label: string }>; onChange: (next: RegisterState) => void }) {
  return <div className="mt-3 grid gap-2 md:grid-cols-3">
    <Input label={`Search ${label}`} value={state.searchInput} onChange={event => onChange({ ...state, searchInput: event.target.value })} />
    <Select label={`${label} status`} value={state.status} options={[{ value: 'all', label: 'All statuses' }, { value: 'active', label: 'Active' }, { value: 'inactive', label: 'Inactive' }]} onChange={event => onChange({ ...state, status: event.target.value, page: 1 })} />
    <Select label={`Sort ${label}`} value={state.sort} options={sorts} onChange={event => onChange({ ...state, sort: event.target.value, page: 1 })} />
  </div>;
}

export function TenantCalendarAdministration() {
  const { pushToast } = useApp();
  const security = useSecurity();
  const [municipalYears, setMunicipalYears] = useState<MunicipalityFinancialYearMasterDto[]>([]);
  const [periods, setPeriods] = useState<ReportingPeriodMasterDto[]>([]);
  const [layers, setLayers] = useState<SdbipLayerMasterDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [pickerRevision, setPickerRevision] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [municipalRegister, setMunicipalRegister] = useState<RegisterState>(() => registerState('startDate:desc'));
  const [periodRegister, setPeriodRegister] = useState<RegisterState>(() => registerState('sequence:asc'));
  const [layerRegister, setLayerRegister] = useState<RegisterState>(() => registerState('financialYear:desc'));
  const [year, setYear] = useState({ code: '', name: '', startDate: today(), endDate: today(), reason: '' });
  const [municipalYear, setMunicipalYear] = useState({ financialYearPublicId: '', isCurrent: true, effectiveFrom: today(), reason: '' });
  const [currentYearReason, setCurrentYearReason] = useState('');
  const [period, setPeriod] = useState({ municipalityFinancialYearPublicId: '', code: 'Q1', name: 'Quarter 1', periodType: 1, sequence: 1, startDate: today(), endDate: today(), reason: '' });
  const [selectedLayer, setSelectedLayer] = useState<SdbipLayerMasterDto | null>(null);
  const [layer, setLayer] = useState({ municipalityFinancialYearPublicId: '', code: '', name: '', description: '', displayOrder: 1, isActive: true, reason: '' });
  const { page: municipalPage, search: municipalSearch, sort: municipalSort, status: municipalStatus } = municipalRegister;
  const { page: periodPage, search: periodSearch, sort: periodSort, status: periodStatus } = periodRegister;
  const { page: layerPage, search: layerSearch, sort: layerSort, status: layerStatus } = layerRegister;

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [municipalResult, periodResult, layerResult] = await Promise.all([
      getMunicipalityFinancialYearMastersPage(queryFrom(municipalPage, municipalSearch, municipalStatus, municipalSort)),
      getReportingPeriodMastersPage(queryFrom(periodPage, periodSearch, periodStatus, periodSort)), getSdbipLayerMastersPage(queryFrom(layerPage, layerSearch, layerStatus, layerSort)),
    ]);
    const failed = [municipalResult, periodResult, layerResult].find(result => !result.success);
    if (failed) setError(failed.message ?? 'Tenant calendar masters could not be loaded.');
    setMunicipalYears(municipalResult.data?.items ?? []); setPeriods(periodResult.data?.items ?? []); setLayers(layerResult.data?.items ?? []);
    setMunicipalRegister(current => ({ ...current, totalCount: municipalResult.data?.totalCount ?? 0, totalPages: municipalResult.data?.totalPages ?? 0, page: municipalResult.data?.items.length === 0 && current.page > 1 ? current.page - 1 : current.page }));
    setPeriodRegister(current => ({ ...current, totalCount: periodResult.data?.totalCount ?? 0, totalPages: periodResult.data?.totalPages ?? 0, page: periodResult.data?.items.length === 0 && current.page > 1 ? current.page - 1 : current.page }));
    setLayerRegister(current => ({ ...current, totalCount: layerResult.data?.totalCount ?? 0, totalPages: layerResult.data?.totalPages ?? 0, page: layerResult.data?.items.length === 0 && current.page > 1 ? current.page - 1 : current.page }));
    setBusy(false);
  }, [layerPage, layerSearch, layerSort, layerStatus, municipalPage, municipalSearch, municipalSort, municipalStatus, periodPage, periodSearch, periodSort, periodStatus]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setMunicipalRegister(current => ({ ...current, search: current.searchInput.trim(), page: 1 })), 250); return () => window.clearTimeout(timer); }, [municipalRegister.searchInput]);
  useEffect(() => { const timer = window.setTimeout(() => setPeriodRegister(current => ({ ...current, search: current.searchInput.trim(), page: 1 })), 250); return () => window.clearTimeout(timer); }, [periodRegister.searchInput]);
  useEffect(() => { const timer = window.setTimeout(() => setLayerRegister(current => ({ ...current, search: current.searchInput.trim(), page: 1 })), 250); return () => window.clearTimeout(timer); }, [layerRegister.searchInput]);

  const saveYear = async () => {
    if (year.reason.trim().length < 10) { setError('Enter a governance reason of at least 10 characters for the financial year.'); return; }
    setBusy(true); setError(null);
    const result = await createFinancialYearMaster({ ...year, startDate: date(year.startDate), endDate: date(year.endDate) });
    if (!result.success) setError(result.message ?? 'Financial year could not be created.');
    else { pushToast('success', 'Financial year created'); setYear({ code: '', name: '', startDate: today(), endDate: today(), reason: '' }); setPickerRevision(value => value + 1); await load(); }
    setBusy(false);
  };

  const saveMunicipalYear = async () => {
    if (!municipalYear.financialYearPublicId || municipalYear.reason.trim().length < 10) { setError('Select a financial year and enter a governance reason of at least 10 characters.'); return; }
    setBusy(true); setError(null);
    const result = await createMunicipalityFinancialYearMaster({ ...municipalYear, effectiveFrom: date(municipalYear.effectiveFrom), effectiveTo: null });
    if (!result.success) setError(result.message ?? 'Municipality financial year could not be configured.');
    else { pushToast('success', 'Municipality financial year configured'); setPickerRevision(value => value + 1); await load(); }
    setBusy(false);
  };

  const makeCurrent = async (item: MunicipalityFinancialYearMasterDto) => {
    if (currentYearReason.trim().length < 10) { setError('Enter a governance reason of at least 10 characters before changing the current year.'); return; }
    setBusy(true); setError(null);
    const result = await updateMunicipalityFinancialYearMaster(item.publicId, { isCurrent: true, isActive: item.isActive, effectiveFrom: item.effectiveFrom, effectiveTo: item.effectiveTo, reason: currentYearReason, rowVersion: item.rowVersion });
    if (!result.success) setError(result.message ?? 'Current financial year could not be changed.');
    else { pushToast('success', `${item.code} is now current`); setCurrentYearReason(''); await load(); }
    setBusy(false);
  };

  const savePeriod = async () => {
    if (!period.municipalityFinancialYearPublicId || period.reason.trim().length < 10) { setError('Select a municipality financial year and enter a governance reason of at least 10 characters.'); return; }
    setBusy(true); setError(null);
    const result = await createReportingPeriodMaster({ ...period, startDate: date(period.startDate), endDate: date(period.endDate) });
    if (!result.success) setError(result.message ?? 'Reporting period could not be created.');
    else { pushToast('success', 'Reporting period created'); setPeriod(current => ({ ...current, code: '', name: '', sequence: current.sequence + 1, reason: '' })); setPickerRevision(value => value + 1); await load(); }
    setBusy(false);
  };

  const clearLayer = () => { setSelectedLayer(null); setLayer({ municipalityFinancialYearPublicId: '', code: '', name: '', description: '', displayOrder: 1, isActive: true, reason: '' }); };
  const editLayer = (item: SdbipLayerMasterDto) => {
    setSelectedLayer(item);
    setLayer({ municipalityFinancialYearPublicId: item.municipalityFinancialYearPublicId, code: item.code, name: item.name, description: item.description ?? '', displayOrder: item.displayOrder, isActive: item.isActive, reason: '' });
  };
  const saveLayer = async () => {
    if (!layer.municipalityFinancialYearPublicId || !layer.code.trim() || !layer.name.trim() || layer.displayOrder < 1 || layer.reason.trim().length < 10) { setError('Complete the SDBIP layer, positive display order and governance reason of at least 10 characters.'); return; }
    setBusy(true); setError(null);
    const result = selectedLayer
      ? await updateSdbipLayerMaster(selectedLayer.publicId, { code: layer.code, name: layer.name, description: layer.description || null, displayOrder: layer.displayOrder, isActive: layer.isActive, reason: layer.reason, rowVersion: selectedLayer.rowVersion })
      : await createSdbipLayerMaster({ municipalityFinancialYearPublicId: layer.municipalityFinancialYearPublicId, code: layer.code, name: layer.name, description: layer.description || null, displayOrder: layer.displayOrder, reason: layer.reason });
    if (!result.success) setError(result.message ?? 'SDBIP layer could not be saved.');
    else { pushToast('success', selectedLayer ? 'SDBIP layer updated' : 'SDBIP layer created'); clearLayer(); setPickerRevision(value => value + 1); await load(); }
    setBusy(false);
  };

  return <AppShell title="Tenant Calendar" subtitle="Financial-year activation, configurable SDBIP layers and canonical reporting periods">
    <div className="space-y-5">
      <div className="flex justify-end"><Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button></div>
      {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
      <div className="grid gap-5 xl:grid-cols-3">
        {security.canCreate('FINANCIAL_YEAR') && <FormPanel title="Global financial year" description="System-scope calendar shared as a selectable master." icon={<CalendarDays className="h-5 w-5" />}>
          <Input label="Code" value={year.code} onChange={event => setYear(current => ({ ...current, code: event.target.value }))} required />
          <Input label="Name" value={year.name} onChange={event => setYear(current => ({ ...current, name: event.target.value }))} required />
          <div className="grid grid-cols-2 gap-2"><Input label="Start" type="date" value={year.startDate} onChange={event => setYear(current => ({ ...current, startDate: event.target.value }))} /><Input label="End" type="date" value={year.endDate} onChange={event => setYear(current => ({ ...current, endDate: event.target.value }))} /></div>
          <Textarea label="Financial year governance reason" value={year.reason} onChange={event => setYear(current => ({ ...current, reason: event.target.value }))} required />
          <Button icon={<Plus className="h-4 w-4" />} onClick={() => void saveYear()} disabled={busy}>Create year</Button>
        </FormPanel>}
        {security.canCreate('FINANCIAL_YEAR') && <FormPanel title="Municipality year" description="Activate a global year in the selected municipality." icon={<CheckCircle2 className="h-5 w-5" />}>
          <CalendarMasterPicker kind="financial-year" label="Financial year" value={municipalYear.financialYearPublicId} onChange={value => setMunicipalYear(current => ({ ...current, financialYearPublicId: value }))} refreshKey={pickerRevision} required />
          <Input label="Effective from" type="date" value={municipalYear.effectiveFrom} onChange={event => setMunicipalYear(current => ({ ...current, effectiveFrom: event.target.value }))} />
          <Checkbox label="Make current" checked={municipalYear.isCurrent} onChange={event => setMunicipalYear(current => ({ ...current, isCurrent: event.target.checked }))} />
          <Textarea label="Municipality year governance reason" value={municipalYear.reason} onChange={event => setMunicipalYear(current => ({ ...current, reason: event.target.value }))} required />
          <Button onClick={() => void saveMunicipalYear()} disabled={busy}>Activate for municipality</Button>
        </FormPanel>}
        {security.canCreate('REPORTING_PERIOD') && <FormPanel title="Reporting period" description="Create a canonical period within an activated year." icon={<Plus className="h-5 w-5" />}>
          <CalendarMasterPicker kind="municipality-financial-year" label="Municipality year" value={period.municipalityFinancialYearPublicId} onChange={value => setPeriod(current => ({ ...current, municipalityFinancialYearPublicId: value }))} refreshKey={pickerRevision} required />
          <div className="grid grid-cols-2 gap-2"><Input label="Code" value={period.code} onChange={event => setPeriod(current => ({ ...current, code: event.target.value }))} /><Input label="Name" value={period.name} onChange={event => setPeriod(current => ({ ...current, name: event.target.value }))} /></div>
          <div className="grid grid-cols-2 gap-2"><Select label="Type" value={period.periodType} options={[['1','Quarter 1'],['2','Quarter 2'],['3','Mid-term'],['4','Quarter 3'],['5','Quarter 4'],['6','Annual']].map(([value,label]) => ({ value, label }))} onChange={event => setPeriod(current => ({ ...current, periodType: Number(event.target.value) }))} /><Input label="Sequence" type="number" min="1" value={period.sequence} onChange={event => setPeriod(current => ({ ...current, sequence: Number(event.target.value) }))} /></div>
          <div className="grid grid-cols-2 gap-2"><Input label="Start" type="date" value={period.startDate} onChange={event => setPeriod(current => ({ ...current, startDate: event.target.value }))} /><Input label="End" type="date" value={period.endDate} onChange={event => setPeriod(current => ({ ...current, endDate: event.target.value }))} /></div>
          <Textarea label="Reporting period governance reason" value={period.reason} onChange={event => setPeriod(current => ({ ...current, reason: event.target.value }))} required />
          <Button onClick={() => void savePeriod()} disabled={busy}>Create period</Button>
        </FormPanel>}
      </div>
      {(selectedLayer ? security.canUpdate('SDBIP_LAYER') : security.canCreate('SDBIP_LAYER')) && <FormPanel title={selectedLayer ? 'Edit SDBIP layer' : 'SDBIP layer'} description="Configure municipality-specific SDBIP names and ordering for an exact financial year." icon={<Plus className="h-5 w-5" />}>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <CalendarMasterPicker kind="municipality-financial-year" label="Municipality year" value={layer.municipalityFinancialYearPublicId} disabled={Boolean(selectedLayer)} selectedLabel={selectedLayer?.financialYearCode} onChange={value => setLayer(current => ({ ...current, municipalityFinancialYearPublicId: value }))} refreshKey={pickerRevision} required />
          <Input label="Code" value={layer.code} onChange={event => setLayer(current => ({ ...current, code: event.target.value }))} required />
          <Input label="Display name" value={layer.name} onChange={event => setLayer(current => ({ ...current, name: event.target.value }))} required />
          <Input label="Display order" type="number" min="1" value={layer.displayOrder} onChange={event => setLayer(current => ({ ...current, displayOrder: Number(event.target.value) }))} required />
        </div>
        <Textarea label="Description" value={layer.description} onChange={event => setLayer(current => ({ ...current, description: event.target.value }))} />
        {selectedLayer && <Checkbox label="Active" checked={layer.isActive} onChange={event => setLayer(current => ({ ...current, isActive: event.target.checked }))} />}
        <Textarea label="Governance reason" value={layer.reason} onChange={event => setLayer(current => ({ ...current, reason: event.target.value }))} required />
        <div className="flex gap-2"><Button onClick={() => void saveLayer()} disabled={busy}>{selectedLayer ? 'Save layer' : 'Create layer'}</Button>{selectedLayer && <Button variant="outline" onClick={clearLayer}>Cancel</Button>}</div>
      </FormPanel>}
      <Card className="p-4"><div className="flex items-center justify-between"><h3 className="font-semibold">Municipality financial years</h3><Badge variant="primary">{municipalRegister.totalCount}</Badge></div><CalendarRegisterControls state={municipalRegister} label="municipality years" sorts={[{ value: 'startDate:desc', label: 'Newest year' }, { value: 'code:asc', label: 'Code A-Z' }, { value: 'current:desc', label: 'Current first' }]} onChange={setMunicipalRegister} />{security.canUpdate('FINANCIAL_YEAR') && <div className="mt-3"><Textarea label="Current-year governance reason" value={currentYearReason} onChange={event => setCurrentYearReason(event.target.value)} required /></div>}<div className="mt-3 space-y-2">{municipalYears.map(item => <div key={item.publicId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div><p className="font-medium">{item.code} · {item.name}</p><p className="text-xs text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleDateString()}</p></div><div className="flex items-center gap-2"><Badge variant={item.isCurrent ? 'success' : item.isActive ? 'info' : 'default'}>{item.isCurrent ? 'Current' : item.isActive ? 'Active' : 'Inactive'}</Badge>{security.canUpdate('FINANCIAL_YEAR') && !item.isCurrent && item.isActive && <Button size="sm" variant="outline" onClick={() => void makeCurrent(item)}>Make current</Button>}</div></div>)}{!municipalYears.length && <p className="text-sm text-secondary-500">No municipality financial years.</p>}</div><CalendarPagination state={municipalRegister} label="Municipality years" onPage={page => setMunicipalRegister(current => ({ ...current, page }))} /></Card>
      <Card className="p-4"><div className="flex items-center justify-between"><h3 className="font-semibold">SDBIP layers</h3><Badge variant="primary">{layerRegister.totalCount}</Badge></div><CalendarRegisterControls state={layerRegister} label="SDBIP layers" sorts={[{ value: 'financialYear:desc', label: 'Newest year' }, { value: 'displayOrder:asc', label: 'Display order' }, { value: 'code:asc', label: 'Code A-Z' }]} onChange={setLayerRegister} /><div className="mt-3 grid gap-2 md:grid-cols-2 xl:grid-cols-3">{layers.map(item => <button type="button" key={item.publicId} onClick={() => editLayer(item)} className="rounded-lg border border-secondary-200 p-3 text-left dark:border-secondary-700"><div className="flex justify-between gap-2"><span className="font-medium">{item.code} · {item.name}</span><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></div><p className="mt-1 text-xs text-secondary-500">{item.financialYearCode} · order {item.displayOrder}</p>{item.description && <p className="mt-1 text-xs text-secondary-500">{item.description}</p>}</button>)}{!layers.length && <p className="text-sm text-secondary-500">No SDBIP layers configured.</p>}</div><CalendarPagination state={layerRegister} label="SDBIP layers" onPage={page => setLayerRegister(current => ({ ...current, page }))} /></Card>
      <Card className="p-4"><div className="flex items-center justify-between"><h3 className="font-semibold">Canonical periods</h3><Badge variant="primary">{periodRegister.totalCount}</Badge></div><CalendarRegisterControls state={periodRegister} label="reporting periods" sorts={[{ value: 'sequence:asc', label: 'Sequence' }, { value: 'startDate:desc', label: 'Newest start' }, { value: 'code:asc', label: 'Code A-Z' }]} onChange={setPeriodRegister} /><div className="mt-3 grid gap-2 md:grid-cols-2 xl:grid-cols-3">{periods.map(item => <div key={item.publicId} className="rounded-lg border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex justify-between"><span className="font-medium">{item.code} · {item.name}</span><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge></div><p className="mt-1 text-xs text-secondary-500">{new Date(item.startDate).toLocaleDateString()} — {new Date(item.endDate).toLocaleDateString()}</p></div>)}{!periods.length && <p className="text-sm text-secondary-500">No reporting periods.</p>}</div><CalendarPagination state={periodRegister} label="Reporting periods" onPage={page => setPeriodRegister(current => ({ ...current, page }))} /></Card>
    </div>
  </AppShell>;
}
