import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  configureC88, createC88Assignment, createC88Calendar, createC88CatalogueItem, createC88CatalogueVersion,
  createC88ComplianceQuestion, createC88Indicator, createC88Mapping, createC88ReportVersion, createC88Workflow,
  finalSubmitC88Report, getC88ReportsPage, getC88Workspace, getMunicipalEmployeesPage, getMunicipalityFinancialYearMasters,
  returnC88Report, saveC88IndicatorPlan, submitC88Report, updateC88CatalogueVersion, verifyC88Report,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { C88CatalogueItemKind, C88IndicatorReport, C88Workspace, MunicipalEmployeeDto, MunicipalityFinancialYearMasterDto } from '../../types';
import { AppShell } from '../layout/AppShell';
import { TargetPicker } from '../common/TargetPicker';
import { Badge, Button, Card, EmptyState } from '../ui';

const field = 'mt-1 w-full rounded border border-secondary-300 bg-white px-2 py-1.5 text-sm dark:border-secondary-700 dark:bg-secondary-900';
const today = () => new Date().toISOString().slice(0, 10);
const emptyWorkspace: C88Workspace = { configurations: [], catalogueVersions: [], catalogueItems: [], indicators: [], complianceQuestions: [], plans: [], calendars: [], reports: [], assignments: [], workflows: [], mappings: [] };
function Section({ title, children }: { title: string; children: ReactNode }) { return <Card><h2 className="mb-3 text-lg font-semibold">{title}</h2>{children}</Card>; }

export function C88Workspace() {
  const { pushToast } = useApp();
  const { canCreate, canRead, canUpdate, canExecute } = useSecurity();
  const [data, setData] = useState<C88Workspace>(emptyWorkspace);
  const [years, setYears] = useState<MunicipalityFinancialYearMasterDto[]>([]);
  const [employees, setEmployees] = useState<MunicipalEmployeeDto[]>([]);
  const [employeePage, setEmployeePage] = useState(1);
  const [employeeTotalPages, setEmployeeTotalPages] = useState(0);
  const [employeeSearch, setEmployeeSearch] = useState('');
  const [yearId, setYearId] = useState('');
  const [configurationId, setConfigurationId] = useState('');
  const [indicatorId, setIndicatorId] = useState('');
  const [calendarId, setCalendarId] = useState('');
  const [selectedReport, setSelectedReport] = useState<C88IndicatorReport | null>(null);
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [reportPage, setReportPage] = useState(1);
  const [reportTotalCount, setReportTotalCount] = useState(0);
  const [reportTotalPages, setReportTotalPages] = useState(0);
  const [reportSearchInput, setReportSearchInput] = useState('');
  const [reportSearch, setReportSearch] = useState('');
  const [reportSortBy, setReportSortBy] = useState('createdAt');
  const [reportSortDirection, setReportSortDirection] = useState<'asc' | 'desc'>('desc');
  const [edition, setEdition] = useState({ code: '', name: '', editionDate: today(), effectiveFrom: today() });
  const [catalogueItem, setCatalogueItem] = useState({ catalogueVersionPublicId: '', kind: 'Sector' as C88CatalogueItemKind, code: '', name: '', parentItemPublicId: '' });
  const [indicatorDraft, setIndicatorDraft] = useState({ catalogueVersionPublicId: '', code: '', name: '', definition: '', officialTechnicalIndicatorDescription: '', valueType: 'Decimal', calculationOperator: 'None', elementCode: '', elementName: '', municipalCategoryPublicId: '', readinessTierPublicId: '' });
  const [plan, setPlan] = useState({ baselineValue: '', mediumTermTarget: '', annualTarget: '', missingDataExplanation: '', estimatedAvailability: '' });
  const [calendar, setCalendar] = useState({ reportTypePublicId: '', code: '', name: '', opensAt: today(), closesAt: today(), dueAt: today() });
  const [assignment, setAssignment] = useState({ employeePublicId: '', role: 'PrimaryCapturer' });
  const [mapping, setMapping] = useState({ opmsTargetPublicId: '', mappingType: 'Direct' });
  const [reportValues, setReportValues] = useState<Record<string, string>>({});
  const [responses, setResponses] = useState<Record<string, string>>({});

  const canReadModule = canRead('C88_INDICATOR') || canRead('C88_REPORT');
  const canReadReports = canRead('C88_REPORT');
  const effectiveConfigurationId = configurationId || data.configurations[0]?.publicId || '';
  const configuration = data.configurations.find(item => item.publicId === effectiveConfigurationId);
  const indicator = data.indicators.find(item => item.publicId === indicatorId);
  const activeVersionId = configuration?.catalogueVersionPublicId ?? catalogueItem.catalogueVersionPublicId;
  const reportTypes = data.catalogueItems.filter(item => item.catalogueVersionPublicId === activeVersionId && item.kind === 'ReportType' && item.isActive);
  const draftReportTypes = data.catalogueItems.filter(item => item.catalogueVersionPublicId === catalogueItem.catalogueVersionPublicId && item.kind === 'ReportType' && item.isActive);
  const draftResponseTypes = data.catalogueItems.filter(item => item.catalogueVersionPublicId === catalogueItem.catalogueVersionPublicId && item.kind === 'ResponseType' && item.isActive);
  const questions = useMemo(() => data.complianceQuestions.filter(item => item.reportTypePublicId === calendar.reportTypePublicId && item.isActive), [calendar.reportTypePublicId, data.complianceQuestions]);

  const load = useCallback(async () => {
    if (!canReadModule) return;
    const [workspace, yearResult, reportResult] = await Promise.all([
      getC88Workspace(yearId || undefined, false), getMunicipalityFinancialYearMasters(),
      canReadReports
        ? getC88ReportsPage({ page: reportPage, pageSize: 25, search: reportSearch, sortBy: reportSortBy, sortDirection: reportSortDirection }, yearId || undefined)
        : Promise.resolve({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 }, message: undefined }),
    ]);
    if (!workspace.success) pushToast('error', workspace.message ?? 'Unable to load Circular 88.');
    else setData({ ...(workspace.data ?? emptyWorkspace), reports: reportResult.data?.items ?? [] });
    if (!reportResult.success) pushToast('error', reportResult.message ?? 'Unable to load Circular 88 reports.');
    setReportTotalCount(reportResult.data?.totalCount ?? 0);
    setReportTotalPages(reportResult.data?.totalPages ?? 0);
    if (yearResult.success) setYears((yearResult.data ?? []).filter(item => item.isActive));
  }, [canReadModule, canReadReports, pushToast, reportPage, reportSearch, reportSortBy, reportSortDirection, yearId]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    if (!canExecute('C88_INDICATOR.MANAGE_ASSIGNMENTS')) return;
    void getMunicipalEmployeesPage({ page: employeePage, pageSize: 25, search: employeeSearch, sortBy: 'name', sortDirection: 'asc' }, true).then(result => {
      setEmployees(result.data?.items ?? []);
      setEmployeeTotalPages(result.data?.totalPages ?? 0);
    });
  }, [canExecute, employeePage, employeeSearch]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setReportPage(1); setReportSearch(reportSearchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [reportSearchInput]);
  useEffect(() => {
    setYearId(value => value || years.find(item => item.isCurrent)?.publicId || years[0]?.publicId || '');
    setConfigurationId(value => data.configurations.some(item => item.publicId === value) ? value : data.configurations[0]?.publicId || '');
  }, [data.configurations, years]);
  useEffect(() => {
    const firstIndicator = data.indicators.find(item => item.catalogueVersionPublicId === configuration?.catalogueVersionPublicId && item.isActive);
    setIndicatorId(value => data.indicators.some(item => item.publicId === value && item.catalogueVersionPublicId === configuration?.catalogueVersionPublicId) ? value : firstIndicator?.publicId ?? '');
    setCalendarId(value => data.calendars.some(item => item.publicId === value && item.configurationPublicId === effectiveConfigurationId) ? value : data.calendars.find(item => item.configurationPublicId === effectiveConfigurationId)?.publicId ?? '');
  }, [configuration?.catalogueVersionPublicId, data.calendars, data.indicators, effectiveConfigurationId]);

  const run = async (operation: () => Promise<{ success: boolean; message?: string }>, success: string) => {
    setBusy(true);
    const result = await operation();
    setBusy(false);
    if (!result.success) pushToast('error', result.message ?? 'Circular 88 action failed.');
    else { pushToast('success', success); setReason(''); await load(); }
  };

  if (!canReadModule) return <AppShell><EmptyState title="Circular 88 unavailable" description="Your effective role does not grant C88 indicator or report access." /></AppShell>;

  const publishedVersions = data.catalogueVersions.filter(item => item.isPublished && item.isActive);
  const currentReports = data.reports.filter(item => item.isCurrent !== false && item.configurationPublicId === effectiveConfigurationId);

  return <AppShell>
    <div className="space-y-4">
      <div><h1 className="text-2xl font-semibold">Circular 88</h1><p className="text-sm text-secondary-500">Independent Treasury catalogue, planning, reporting, compliance and workflow.</p></div>

      <Section title="Municipality and financial-year configuration">
        <div className="grid gap-3 md:grid-cols-4">
          <label className="text-sm">Financial year<select className={field} value={yearId} onChange={event => { setYearId(event.target.value); setReportPage(1); }}>{years.map(item => <option key={item.publicId} value={item.publicId}>{item.code}</option>)}</select></label>
          <label className="text-sm">Configuration<select className={field} value={effectiveConfigurationId} onChange={event => setConfigurationId(event.target.value)}><option value="">Not configured</option>{data.configurations.map(item => <option key={item.publicId} value={item.publicId}>{item.financialYearCode} · {item.catalogueVersionCode}</option>)}</select></label>
          <label className="text-sm">Published edition<select className={field} value={configuration?.catalogueVersionPublicId ?? catalogueItem.catalogueVersionPublicId} onChange={event => setCatalogueItem(value => ({ ...value, catalogueVersionPublicId: event.target.value }))}><option value="">Select edition</option>{publishedVersions.map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select></label>
          <div className="flex items-end"><Badge variant={configuration?.isEnabled ? 'success' : 'warning'}>{configuration?.isEnabled ? 'Enabled' : 'Disabled'}</Badge></div>
        </div>
        {canExecute('C88_INDICATOR.CONFIGURE') && <div className="mt-3 flex gap-2"><Button disabled={busy || !yearId || !(configuration?.catalogueVersionPublicId || catalogueItem.catalogueVersionPublicId)} onClick={() => void run(() => configureC88({ municipalityFinancialYearPublicId: yearId, catalogueVersionPublicId: configuration?.catalogueVersionPublicId || catalogueItem.catalogueVersionPublicId, isEnabled: !(configuration?.isEnabled ?? false), effectiveFrom: new Date().toISOString(), effectiveTo: null, reason, rowVersion: configuration?.rowVersion ?? null }), configuration?.isEnabled ? 'C88 disabled without changing OPMS.' : 'C88 enabled.')}>{configuration?.isEnabled ? 'Disable C88' : 'Enable C88'}</Button><input className={field} placeholder="Reason" value={reason} onChange={event => setReason(event.target.value)} /></div>}
      </Section>

      {canExecute('C88_INDICATOR.MANAGE_CATALOGUE') && <Section title="Versioned Treasury catalogue">
        <div className="grid gap-2 md:grid-cols-5"><input className={field} placeholder="Edition code" value={edition.code} onChange={e => setEdition({ ...edition, code: e.target.value })} /><input className={field} placeholder="Edition name" value={edition.name} onChange={e => setEdition({ ...edition, name: e.target.value })} /><input className={field} type="date" value={edition.editionDate} onChange={e => setEdition({ ...edition, editionDate: e.target.value })} /><input className={field} type="date" value={edition.effectiveFrom} onChange={e => setEdition({ ...edition, effectiveFrom: e.target.value })} /><Button disabled={busy || !reason} onClick={() => void run(() => createC88CatalogueVersion({ ...edition, effectiveTo: null, isPublished: false, isActive: true, reason, rowVersion: null }), 'Catalogue edition created.')}>Create edition</Button></div>
        <div className="mt-3 grid gap-2 md:grid-cols-6"><select className={field} value={catalogueItem.catalogueVersionPublicId} onChange={e => setCatalogueItem({ ...catalogueItem, catalogueVersionPublicId: e.target.value })}><option value="">Draft edition</option>{data.catalogueVersions.filter(item => !item.isPublished).map(item => <option key={item.publicId} value={item.publicId}>{item.code}</option>)}</select><select className={field} value={catalogueItem.kind} onChange={e => setCatalogueItem({ ...catalogueItem, kind: e.target.value as C88CatalogueItemKind })}>{['Sector','Outcome','IndicatorType','MunicipalCategory','ReadinessTier','ReportType','ResponseType'].map(kind => <option key={kind}>{kind}</option>)}</select><input className={field} placeholder="Item code" value={catalogueItem.code} onChange={e => setCatalogueItem({ ...catalogueItem, code: e.target.value })} /><input className={field} placeholder="Item name" value={catalogueItem.name} onChange={e => setCatalogueItem({ ...catalogueItem, name: e.target.value })} /><select className={field} value={catalogueItem.parentItemPublicId} onChange={e => setCatalogueItem({ ...catalogueItem, parentItemPublicId: e.target.value })}><option value="">No parent</option>{data.catalogueItems.filter(item => item.catalogueVersionPublicId === catalogueItem.catalogueVersionPublicId).map(item => <option key={item.publicId} value={item.publicId}>{item.kind} · {item.code}</option>)}</select><Button disabled={busy || !reason} onClick={() => void run(() => createC88CatalogueItem({ ...catalogueItem, description: null, parentItemPublicId: catalogueItem.parentItemPublicId || null, displayOrder: 0, isActive: true, reason, rowVersion: null }), 'Catalogue item added.')}>Add item</Button></div>
        <div className="mt-3 grid gap-2 md:grid-cols-5"><input className={field} placeholder="Indicator code" value={indicatorDraft.code} onChange={e => setIndicatorDraft({ ...indicatorDraft, code: e.target.value, catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId })} /><input className={field} placeholder="Indicator name" value={indicatorDraft.name} onChange={e => setIndicatorDraft({ ...indicatorDraft, name: e.target.value })} /><input className={field} placeholder="Official definition" value={indicatorDraft.definition} onChange={e => setIndicatorDraft({ ...indicatorDraft, definition: e.target.value })} /><input className={field} placeholder="Official TID" value={indicatorDraft.officialTechnicalIndicatorDescription} onChange={e => setIndicatorDraft({ ...indicatorDraft, officialTechnicalIndicatorDescription: e.target.value })} /><Button disabled={busy || !reason} onClick={() => void run(() => createC88Indicator({ ...indicatorDraft, catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId, sectorPublicId: null, outcomePublicId: null, indicatorTypePublicId: null, officialFormulaText: null, requiresBaseline: true, requiresMediumTermTarget: true, requiresAnnualTarget: true, isActive: true, dataElements: indicatorDraft.elementCode ? [{ code: indicatorDraft.elementCode, name: indicatorDraft.elementName, description: null, valueType: indicatorDraft.valueType, isRequired: true, sequence: 1 }] : [], applicability: indicatorDraft.municipalCategoryPublicId ? [{ municipalCategoryPublicId: indicatorDraft.municipalCategoryPublicId, readinessTierPublicId: indicatorDraft.readinessTierPublicId || null, isApplicable: true, notes: null }] : [], reason }), 'Indicator added.')}>Add indicator</Button><input className={field} placeholder="Data-element code" value={indicatorDraft.elementCode} onChange={e => setIndicatorDraft({ ...indicatorDraft, elementCode: e.target.value })} /><input className={field} placeholder="Data-element name" value={indicatorDraft.elementName} onChange={e => setIndicatorDraft({ ...indicatorDraft, elementName: e.target.value })} /><select className={field} value={indicatorDraft.municipalCategoryPublicId} onChange={e => setIndicatorDraft({ ...indicatorDraft, municipalCategoryPublicId: e.target.value })}><option value="">Municipal category</option>{data.catalogueItems.filter(item => item.catalogueVersionPublicId === catalogueItem.catalogueVersionPublicId && item.kind === 'MunicipalCategory').map(item => <option key={item.publicId} value={item.publicId}>{item.code}</option>)}</select><select className={field} value={indicatorDraft.readinessTierPublicId} onChange={e => setIndicatorDraft({ ...indicatorDraft, readinessTierPublicId: e.target.value })}><option value="">Readiness tier</option>{data.catalogueItems.filter(item => item.catalogueVersionPublicId === catalogueItem.catalogueVersionPublicId && item.kind === 'ReadinessTier').map(item => <option key={item.publicId} value={item.publicId}>{item.code}</option>)}</select></div>
        <div className="mt-3 flex flex-wrap gap-2">{data.catalogueVersions.filter(item => !item.isPublished).map(item => <Button key={item.publicId} variant="secondary" disabled={busy || !reason} onClick={() => void run(() => updateC88CatalogueVersion(item.publicId, { ...item, isPublished: true, reason }), `${item.code} published and locked.`)}>Publish {item.code}</Button>)}</div>
      </Section>}

      <Section title="Planning, assignments, workflow and mappings">
        <div className="grid gap-3 md:grid-cols-3"><label className="text-sm">Indicator<select className={field} value={indicatorId} onChange={e => setIndicatorId(e.target.value)}>{data.indicators.filter(item => item.catalogueVersionPublicId === configuration?.catalogueVersionPublicId).map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select></label><input className={field} placeholder="Baseline" value={plan.baselineValue} onChange={e => setPlan({ ...plan, baselineValue: e.target.value })} /><input className={field} placeholder="Medium-term target" value={plan.mediumTermTarget} onChange={e => setPlan({ ...plan, mediumTermTarget: e.target.value })} /><input className={field} placeholder="Annual target" value={plan.annualTarget} onChange={e => setPlan({ ...plan, annualTarget: e.target.value })} />{canUpdate('C88_INDICATOR') && <Button disabled={busy || !configurationId || !indicatorId || !reason} onClick={() => void run(() => saveC88IndicatorPlan({ configurationPublicId: configurationId, indicatorPublicId: indicatorId, ...plan, estimatedAvailability: plan.estimatedAvailability || null, reason, rowVersion: data.plans.find(item => item.configurationPublicId === configurationId && item.indicatorPublicId === indicatorId)?.rowVersion ?? null }), 'C88 plan saved.')}>Save plan</Button>}</div>
        {canExecute('C88_INDICATOR.MANAGE_ASSIGNMENTS') && <div className="mt-3 space-y-2"><div className="grid gap-2 md:grid-cols-4"><input aria-label="Search C88 assignment employees" className={field} placeholder="Search employees" value={employeeSearch} onChange={e => { setEmployeeSearch(e.target.value); setEmployeePage(1); }} /><select aria-label="C88 assignment employee" className={field} value={assignment.employeePublicId} onChange={e => setAssignment({ ...assignment, employeePublicId: e.target.value })}><option value="">Employee</option>{employees.map(item => <option key={item.publicId} value={item.publicId}>{item.firstName} {item.lastName}</option>)}</select><select aria-label="C88 assignment role" className={field} value={assignment.role} onChange={e => setAssignment({ ...assignment, role: e.target.value })}>{['PrimaryCapturer','Contributor','ReviewerVerifier','FinalSubmitter'].map(role => <option key={role}>{role}</option>)}</select><Button disabled={busy || !reason} onClick={() => void run(() => createC88Assignment({ configurationPublicId: configurationId, indicatorPublicId: indicatorId, ...assignment, effectiveFrom: new Date().toISOString(), effectiveTo: null, isActive: true, reason, rowVersion: null }), 'Assignment created.')}>Assign</Button></div>{employeeTotalPages > 1 && <div className="flex items-center justify-end gap-2 text-xs text-secondary-500"><Button size="sm" variant="outline" disabled={employeePage <= 1} onClick={() => setEmployeePage(value => Math.max(1, value - 1))}>Previous employees</Button><span>Page {employeePage} of {employeeTotalPages}</span><Button size="sm" variant="outline" disabled={employeePage >= employeeTotalPages} onClick={() => setEmployeePage(value => value + 1)}>Next employees</Button></div>}</div>}
        {canExecute('C88_INDICATOR.MANAGE_WORKFLOW') && <div className="mt-3"><Button disabled={busy || !configurationId || !reason} onClick={() => { const previous = data.workflows.find(item => item.configurationPublicId === configurationId && item.isCurrent); void run(() => createC88Workflow({ configurationPublicId: configurationId, effectiveFrom: new Date().toISOString(), effectiveTo: null, stages: [{ sequence: 1, kind: 'Capturer', name: 'Capturer', requiredRole: 'PrimaryCapturer', isActive: true }, { sequence: 2, kind: 'ReviewerVerifier', name: 'Reviewer / Verifier', requiredRole: 'ReviewerVerifier', isActive: true }, { sequence: 3, kind: 'FinalSubmission', name: 'Final Submission', requiredRole: 'FinalSubmitter', isActive: true }], reason, previousWorkflowPublicId: previous?.publicId ?? null, previousWorkflowRowVersion: previous?.rowVersion ?? null }), 'Independent C88 workflow version created.'); }}>Create workflow version</Button></div>}
        {canExecute('C88_INDICATOR.MANAGE_MAPPING') && <div className="mt-3 grid gap-2 md:grid-cols-4"><TargetPicker kind="opms" label="OPMS KPI" value={mapping.opmsTargetPublicId} valueField="publicId" onChange={value => setMapping({ ...mapping, opmsTargetPublicId: value })} /><select aria-label="Mapping type" className={field} value={mapping.mappingType} onChange={e => setMapping({ ...mapping, mappingType: e.target.value })}><option>Direct</option><option>Contributing</option></select><Button disabled={busy || !reason} onClick={() => void run(() => createC88Mapping({ configurationPublicId: configurationId, indicatorPublicId: indicatorId, ...mapping, reason, isActive: true, rowVersion: null }), 'Alignment-only mapping created.')}>Map without copying</Button></div>}
      </Section>

      <Section title="Reporting calendar and capture">
        {canExecute('C88_INDICATOR.MANAGE_WORKFLOW') && <div className="grid gap-2 md:grid-cols-5"><select className={field} value={calendar.reportTypePublicId} onChange={e => setCalendar({ ...calendar, reportTypePublicId: e.target.value })}><option value="">Report type</option>{reportTypes.map(item => <option key={item.publicId} value={item.publicId}>{item.name}</option>)}</select><input className={field} placeholder="Calendar code" value={calendar.code} onChange={e => setCalendar({ ...calendar, code: e.target.value })} /><input className={field} placeholder="Calendar name" value={calendar.name} onChange={e => setCalendar({ ...calendar, name: e.target.value })} /><Button disabled={busy || !reason} onClick={() => void run(() => createC88Calendar({ configurationPublicId: configurationId, ...calendar, reportingPeriodPublicId: null, isActive: true, reason, rowVersion: null }), 'Reporting calendar created.')}>Create calendar</Button></div>}
        <div className="mt-3 grid gap-2 md:grid-cols-3"><select className={field} value={calendarId} onChange={e => setCalendarId(e.target.value)}><option value="">Calendar</option>{data.calendars.filter(item => item.configurationPublicId === configurationId).map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select>{indicator?.dataElements.map(element => <input key={element.publicId} className={field} placeholder={`${element.code} · ${element.name}`} value={reportValues[element.publicId] ?? ''} onChange={e => setReportValues({ ...reportValues, [element.publicId]: e.target.value })} />)}{questions.map(question => <input key={question.publicId} className={field} placeholder={question.prompt} value={responses[question.publicId] ?? ''} onChange={e => setResponses({ ...responses, [question.publicId]: e.target.value })} />)}{canCreate('C88_REPORT') && <Button disabled={busy || !reason || !calendarId || !indicatorId} onClick={() => void run(() => createC88ReportVersion({ configurationPublicId: configurationId, calendarPublicId: calendarId, indicatorPublicId: indicatorId, previousReportPublicId: selectedReport?.publicId ?? null, previousReportRowVersion: selectedReport?.rowVersion ?? null, missingDataExplanation: null, estimatedAvailability: null, dataElementValues: indicator?.dataElements.map(element => ({ dataElementPublicId: element.publicId, value: reportValues[element.publicId] || null, missingDataExplanation: null, estimatedAvailability: null })) ?? [], complianceResponses: questions.map(question => ({ questionPublicId: question.publicId, response: responses[question.publicId] || null, comment: null })), reason }), 'C88 report version created.')}>Save report version</Button>}</div>
      </Section>

      <Section title="Governed report register">
        {canReadReports && <div className="mb-3 grid gap-2 md:grid-cols-[minmax(0,1fr)_13rem_10rem]"><label className="text-sm">Search reports<input className={field} value={reportSearchInput} onChange={event => setReportSearchInput(event.target.value)} placeholder="Indicator code or name" /></label><label className="text-sm">Sort reports<select className={field} value={reportSortBy} onChange={event => { setReportPage(1); setReportSortBy(event.target.value); }}><option value="createdAt">Created</option><option value="indicatorCode">Indicator code</option><option value="state">State</option><option value="versionNumber">Version</option></select></label><label className="text-sm">Direction<select className={field} value={reportSortDirection} onChange={event => { setReportPage(1); setReportSortDirection(event.target.value as 'asc' | 'desc'); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label></div>}
        <div className="space-y-2">{currentReports.map(report => <button type="button" key={report.publicId} onClick={() => setSelectedReport(report)} className="w-full rounded border p-3 text-left"><div className="flex justify-between"><strong>{report.indicatorCode} · v{report.versionNumber}</strong><Badge variant={report.state === 'FinalSubmitted' ? 'success' : 'default'}>{report.state}</Badge></div><div className="text-xs text-secondary-500">Calculated value: {report.calculatedValue ?? 'Not calculated'} · Stage {report.currentStageSequence}</div></button>)}</div>
        {canReadReports && reportTotalPages > 1 && <div className="mt-3 flex items-center justify-between"><p className="text-xs text-secondary-500">Page {reportPage} of {reportTotalPages} · {reportTotalCount} reports</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={busy || reportPage === 1} onClick={() => setReportPage(value => Math.max(1, value - 1))}>Previous</Button><Button variant="outline" size="sm" disabled={busy || reportPage === reportTotalPages} onClick={() => setReportPage(value => Math.min(reportTotalPages, value + 1))}>Next</Button></div></div>}
        {selectedReport && <div className="mt-3 flex flex-wrap gap-2"><input className={field} placeholder="Workflow reason" value={reason} onChange={e => setReason(e.target.value)} />{canExecute('C88_REPORT.SUBMIT') && ['Draft','Rework'].includes(selectedReport.state) && <Button disabled={busy || !reason} onClick={() => void run(() => submitC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report submitted.')}>Submit</Button>}{canExecute('C88_REPORT.VERIFY') && selectedReport.state === 'Submitted' && <Button disabled={busy || !reason} onClick={() => void run(() => verifyC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report verified.')}>Verify</Button>}{canExecute('C88_REPORT.RETURN') && ['Submitted','Verified'].includes(selectedReport.state) && <Button variant="secondary" disabled={busy || !reason} onClick={() => void run(() => returnC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report returned for rework.')}>Return</Button>}{canExecute('C88_REPORT.FINAL_SUBMIT') && selectedReport.state === 'Verified' && <Button disabled={busy || !reason} onClick={() => void run(() => finalSubmitC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report finally submitted.')}>Final submit</Button>}</div>}
      </Section>

      {canExecute('C88_INDICATOR.MANAGE_CATALOGUE') && draftReportTypes.length > 0 && draftResponseTypes.length > 0 && <Section title="Compliance question administration"><Button disabled={busy || !reason} onClick={() => void run(() => createC88ComplianceQuestion({ catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId, reportTypePublicId: draftReportTypes[0].publicId, responseTypePublicId: draftResponseTypes[0].publicId, code: `Q${data.complianceQuestions.length + 1}`, prompt: 'New governed compliance question', isRequired: true, sequence: data.complianceQuestions.length + 1, isActive: true, reason }), 'Compliance question created.')}>Add required question</Button></Section>}
    </div>
  </AppShell>;
}
