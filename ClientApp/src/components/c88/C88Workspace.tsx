import { useCallback, useEffect, useState, type ReactNode } from 'react';
import {
  configureC88, createC88Assignment, createC88Calendar, createC88CatalogueItem, createC88CatalogueVersion,
  createC88ComplianceQuestion, createC88Indicator, createC88Mapping, createC88ReportVersion, createC88Workflow,
  finalSubmitC88Report, getC88AssignmentsPage, getC88CalendarsPage, getC88CatalogueItemsPage, getC88CatalogueVersionsPage, getC88ComplianceQuestionsPage, getC88ConfigurationsPage, getC88IndicatorsPage, getC88MappingsPage, getC88PlansPage, getC88ReportsPage, getC88WorkflowsPage, getMunicipalEmployeesPage,
  returnC88Report, saveC88IndicatorPlan, submitC88Report, updateC88CatalogueVersion, verifyC88Report,
} from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { C88Assignment, C88CatalogueItem, C88CatalogueItemKind, C88CatalogueVersion, C88ComplianceQuestion, C88Configuration, C88Indicator, C88IndicatorPlan, C88IndicatorReport, C88Mapping, C88ReportingCalendar, C88Workflow, MunicipalEmployeeDto } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { AppShell } from '../layout/AppShell';
import { TargetPicker } from '../common/TargetPicker';
import { Badge, Button, Card, EmptyState } from '../ui';

const field = 'mt-1 w-full rounded border border-secondary-300 bg-white px-2 py-1.5 text-sm dark:border-secondary-700 dark:bg-secondary-900';
const today = () => new Date().toISOString().slice(0, 10);
function Section({ title, children }: { title: string; children: ReactNode }) { return <Card><h2 className="mb-3 text-lg font-semibold">{title}</h2>{children}</Card>; }

function CatalogueItemPicker({ versionId, kind, value, onChange, placeholder }: { versionId: string; kind: C88CatalogueItemKind; value: string; onChange: (value: string) => void; placeholder: string }) {
  const [rows, setRows] = useState<C88CatalogueItem[]>([]);
  const [selected, setSelected] = useState<C88CatalogueItem | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  useEffect(() => {
    if (!versionId) { setRows([]); setTotalPages(0); return; }
    void getC88CatalogueItemsPage({ page, pageSize: 10, search, sortBy: 'displayOrder', sortDirection: 'asc' }, { catalogueVersionPublicId: versionId, kind, active: true })
      .then(result => { setRows(result.data?.items ?? []); setTotalPages(result.data?.totalPages ?? 0); });
  }, [kind, page, search, versionId]);
  useEffect(() => {
    if (!value) { setSelected(null); return; }
    const visible = rows.find(item => item.publicId === value);
    if (visible) { setSelected(visible); return; }
    void getC88CatalogueItemsPage({ page: 1, pageSize: 1 }, { catalogueItemPublicId: value, catalogueVersionPublicId: versionId, kind })
      .then(result => setSelected(result.data?.items[0] ?? null));
  }, [kind, rows, value, versionId]);
  const options = selected && !rows.some(item => item.publicId === selected.publicId) ? [selected, ...rows] : rows;
  return <div><input aria-label={`Search ${placeholder}`} className={field} placeholder={`Search ${placeholder.toLowerCase()}`} value={search} onChange={event => { setSearch(event.target.value); setPage(1); }} /><select aria-label={placeholder} className={field} value={value} onChange={event => onChange(event.target.value)}><option value="">{placeholder}</option>{options.map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select>{totalPages > 1 && <div className="mt-1 flex items-center justify-end gap-1"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage(current => current - 1)}>Previous</Button><span className="text-xs">{page}/{totalPages}</span><Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage(current => current + 1)}>Next</Button></div>}</div>;
}

export function C88Workspace() {
  const { pushToast } = useApp();
  const { canCreate, canRead, canUpdate, canExecute } = useSecurity();
  const [reportRows, setReportRows] = useState<C88IndicatorReport[]>([]);
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
  const [configurationRows, setConfigurationRows] = useState<C88Configuration[]>([]);
  const [configurationPage, setConfigurationPage] = useState(1);
  const [configurationTotalPages, setConfigurationTotalPages] = useState(0);
  const [configurationTotalCount, setConfigurationTotalCount] = useState(0);
  const [configurationSearch, setConfigurationSearch] = useState('');
  const [selectedConfiguration, setSelectedConfiguration] = useState<C88Configuration | null>(null);
  const [versionRows, setVersionRows] = useState<C88CatalogueVersion[]>([]);
  const [versionPage, setVersionPage] = useState(1);
  const [versionTotalPages, setVersionTotalPages] = useState(0);
  const [versionTotalCount, setVersionTotalCount] = useState(0);
  const [versionSearch, setVersionSearch] = useState('');
  const [configuredVersion, setConfiguredVersion] = useState<C88CatalogueVersion | null>(null);
  const [selectedCatalogueVersion, setSelectedCatalogueVersion] = useState<C88CatalogueVersion | null>(null);
  const [indicatorRows, setIndicatorRows] = useState<C88Indicator[]>([]);
  const [indicatorPage, setIndicatorPage] = useState(1);
  const [indicatorTotalPages, setIndicatorTotalPages] = useState(0);
  const [indicatorTotalCount, setIndicatorTotalCount] = useState(0);
  const [indicatorSearch, setIndicatorSearch] = useState('');
  const [selectedIndicator, setSelectedIndicator] = useState<C88Indicator | null>(null);
  const [assignmentRows, setAssignmentRows] = useState<C88Assignment[]>([]);
  const [assignmentPage, setAssignmentPage] = useState(1);
  const [assignmentTotalPages, setAssignmentTotalPages] = useState(0);
  const [assignmentTotalCount, setAssignmentTotalCount] = useState(0);
  const [assignmentSearch, setAssignmentSearch] = useState('');
  const [mappingRows, setMappingRows] = useState<C88Mapping[]>([]);
  const [mappingPage, setMappingPage] = useState(1);
  const [mappingTotalPages, setMappingTotalPages] = useState(0);
  const [mappingTotalCount, setMappingTotalCount] = useState(0);
  const [mappingSearch, setMappingSearch] = useState('');
  const [planRows, setPlanRows] = useState<C88IndicatorPlan[]>([]);
  const [planPage, setPlanPage] = useState(1);
  const [planTotalPages, setPlanTotalPages] = useState(0);
  const [planTotalCount, setPlanTotalCount] = useState(0);
  const [planSearch, setPlanSearch] = useState('');
  const [selectedPlan, setSelectedPlan] = useState<C88IndicatorPlan | null>(null);
  const [calendarRows, setCalendarRows] = useState<C88ReportingCalendar[]>([]);
  const [calendarPage, setCalendarPage] = useState(1);
  const [calendarTotalPages, setCalendarTotalPages] = useState(0);
  const [calendarTotalCount, setCalendarTotalCount] = useState(0);
  const [calendarSearch, setCalendarSearch] = useState('');
  const [workflowRows, setWorkflowRows] = useState<C88Workflow[]>([]);
  const [workflowPage, setWorkflowPage] = useState(1);
  const [workflowTotalPages, setWorkflowTotalPages] = useState(0);
  const [workflowTotalCount, setWorkflowTotalCount] = useState(0);
  const [workflowSearch, setWorkflowSearch] = useState('');
  const [currentWorkflow, setCurrentWorkflow] = useState<C88Workflow | null>(null);
  const [questionRows, setQuestionRows] = useState<C88ComplianceQuestion[]>([]);
  const [questionPage, setQuestionPage] = useState(1);
  const [questionTotalPages, setQuestionTotalPages] = useState(0);
  const [questionTotalCount, setQuestionTotalCount] = useState(0);
  const [questionSearch, setQuestionSearch] = useState('');
  const [catalogueRows, setCatalogueRows] = useState<C88CatalogueItem[]>([]);
  const [cataloguePage, setCataloguePage] = useState(1);
  const [catalogueTotalPages, setCatalogueTotalPages] = useState(0);
  const [catalogueTotalCount, setCatalogueTotalCount] = useState(0);
  const [catalogueSearch, setCatalogueSearch] = useState('');
  const [captureQuestions, setCaptureQuestions] = useState<C88ComplianceQuestion[]>([]);
  const [captureQuestionPage, setCaptureQuestionPage] = useState(1);
  const [captureQuestionTotalPages, setCaptureQuestionTotalPages] = useState(0);
  const [edition, setEdition] = useState({ code: '', name: '', editionDate: today(), effectiveFrom: today() });
  const [catalogueItem, setCatalogueItem] = useState({ catalogueVersionPublicId: '', kind: 'Sector' as C88CatalogueItemKind, code: '', name: '', parentItemPublicId: '' });
  const [indicatorDraft, setIndicatorDraft] = useState({ catalogueVersionPublicId: '', code: '', name: '', definition: '', officialTechnicalIndicatorDescription: '', valueType: 'Decimal', calculationOperator: 'None', elementCode: '', elementName: '', municipalCategoryPublicId: '', readinessTierPublicId: '' });
  const [plan, setPlan] = useState({ baselineValue: '', mediumTermTarget: '', annualTarget: '', missingDataExplanation: '', estimatedAvailability: '' });
  const [calendar, setCalendar] = useState({ reportTypePublicId: '', code: '', name: '', opensAt: today(), closesAt: today(), dueAt: today() });
  const [assignment, setAssignment] = useState({ employeePublicId: '', role: 'PrimaryCapturer' });
  const [mapping, setMapping] = useState({ opmsTargetPublicId: '', mappingType: 'Direct' });
  const [reportValues, setReportValues] = useState<Record<string, string>>({});
  const [responses, setResponses] = useState<Record<string, string>>({});
  const [questionReportTypeId, setQuestionReportTypeId] = useState('');
  const [questionResponseTypeId, setQuestionResponseTypeId] = useState('');

  const canReadModule = canRead('C88_INDICATOR') || canRead('C88_REPORT');
  const canReadIndicators = canRead('C88_INDICATOR');
  const canReadReports = canRead('C88_REPORT');
  const visibleConfigurations = yearId ? configurationRows.filter(item => item.municipalityFinancialYearPublicId === yearId) : configurationRows;
  const effectiveConfigurationId = configurationId || visibleConfigurations[0]?.publicId || '';
  const configuration = configurationRows.find(item => item.publicId === effectiveConfigurationId) ?? selectedConfiguration ?? undefined;
  const indicator = indicatorRows.find(item => item.publicId === indicatorId) ?? selectedIndicator;
  const activeVersionId = configuration?.catalogueVersionPublicId ?? catalogueItem.catalogueVersionPublicId;
  const selectedCalendar = calendarRows.find(item => item.publicId === calendarId);
  const indicatorOptions = indicator && !indicatorRows.some(item => item.publicId === indicator.publicId) ? [indicator, ...indicatorRows] : indicatorRows;
  const versionOptions = [...versionRows];
  for (const item of [configuredVersion, selectedCatalogueVersion])
    if (item && !versionOptions.some(option => option.publicId === item.publicId)) versionOptions.unshift(item);
  const configurationOptions = selectedConfiguration && !visibleConfigurations.some(item => item.publicId === selectedConfiguration.publicId)
    ? [selectedConfiguration, ...visibleConfigurations] : visibleConfigurations;

  const load = useCallback(async () => {
    if (!canReadModule) return;
    const [configurationResult, versionResult, catalogueResult, indicatorResult, reportResult, planResult, assignmentResult, mappingResult, calendarResult, workflowResult, questionResult] = await Promise.all([
      getC88ConfigurationsPage({ page: configurationPage, pageSize: 25, search: configurationSearch, sortBy: 'financialYear', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: yearId || undefined }),
      getC88CatalogueVersionsPage({ page: versionPage, pageSize: 25, search: versionSearch, sortBy: 'editionDate', sortDirection: 'desc' }),
      getC88CatalogueItemsPage({ page: cataloguePage, pageSize: 10, search: catalogueSearch, sortBy: 'displayOrder', sortDirection: 'asc' }, { catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId || undefined, kind: catalogueItem.kind }),
      getC88IndicatorsPage({ page: indicatorPage, pageSize: 10, search: indicatorSearch, sortBy: 'code', sortDirection: 'asc' }, { catalogueVersionPublicId: activeVersionId || undefined, active: true }),
      canReadReports
        ? getC88ReportsPage({ page: reportPage, pageSize: 25, search: reportSearch, sortBy: reportSortBy, sortDirection: reportSortDirection }, yearId || undefined)
        : Promise.resolve({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 }, message: undefined }),
      canReadIndicators
        ? getC88PlansPage({ page: planPage, pageSize: 10, search: planSearch, sortBy: 'indicatorCode', sortDirection: 'asc' }, { municipalityFinancialYearPublicId: yearId || undefined })
        : Promise.resolve({ success: true, data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }, message: undefined }),
      getC88AssignmentsPage({ page: assignmentPage, pageSize: 10, search: assignmentSearch, sortBy: 'effectiveFrom', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: yearId || undefined }),
      getC88MappingsPage({ page: mappingPage, pageSize: 10, search: mappingSearch, sortBy: 'createdAt', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: yearId || undefined }),
      getC88CalendarsPage({ page: calendarPage, pageSize: 10, search: calendarSearch, sortBy: 'opensAt', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: yearId || undefined, configurationPublicId: effectiveConfigurationId || undefined }),
      getC88WorkflowsPage({ page: workflowPage, pageSize: 10, search: workflowSearch, sortBy: 'versionNumber', sortDirection: 'desc' }, { municipalityFinancialYearPublicId: yearId || undefined, configurationPublicId: effectiveConfigurationId || undefined }),
      getC88ComplianceQuestionsPage({ page: questionPage, pageSize: 10, search: questionSearch, sortBy: 'sequence', sortDirection: 'asc' }, { catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId || activeVersionId || undefined }),
    ]);
    setReportRows(reportResult.data?.items ?? []);
    setConfigurationRows(configurationResult.data?.items ?? []); setConfigurationTotalCount(configurationResult.data?.totalCount ?? 0); setConfigurationTotalPages(configurationResult.data?.totalPages ?? 0);
    setVersionRows(versionResult.data?.items ?? []); setVersionTotalCount(versionResult.data?.totalCount ?? 0); setVersionTotalPages(versionResult.data?.totalPages ?? 0);
    setCatalogueRows(catalogueResult.data?.items ?? []); setCatalogueTotalCount(catalogueResult.data?.totalCount ?? 0); setCatalogueTotalPages(catalogueResult.data?.totalPages ?? 0);
    setIndicatorRows(indicatorResult.data?.items ?? []); setIndicatorTotalCount(indicatorResult.data?.totalCount ?? 0); setIndicatorTotalPages(indicatorResult.data?.totalPages ?? 0);
    if (!reportResult.success) pushToast('error', reportResult.message ?? 'Unable to load Circular 88 reports.');
    if (!configurationResult.success) pushToast('error', configurationResult.message ?? 'Unable to load C88 configurations.');
    if (!versionResult.success) pushToast('error', versionResult.message ?? 'Unable to load C88 catalogue versions.');
    if (!catalogueResult.success) pushToast('error', catalogueResult.message ?? 'Unable to load C88 catalogue items.');
    if (!indicatorResult.success) pushToast('error', indicatorResult.message ?? 'Unable to load C88 indicators.');
    setReportTotalCount(reportResult.data?.totalCount ?? 0);
    setReportTotalPages(reportResult.data?.totalPages ?? 0);
    setPlanRows(planResult.data?.items ?? []); setPlanTotalCount(planResult.data?.totalCount ?? 0); setPlanTotalPages(planResult.data?.totalPages ?? 0);
    setAssignmentRows(assignmentResult.data?.items ?? []); setAssignmentTotalCount(assignmentResult.data?.totalCount ?? 0); setAssignmentTotalPages(assignmentResult.data?.totalPages ?? 0);
    setMappingRows(mappingResult.data?.items ?? []); setMappingTotalCount(mappingResult.data?.totalCount ?? 0); setMappingTotalPages(mappingResult.data?.totalPages ?? 0);
    setCalendarRows(calendarResult.data?.items ?? []); setCalendarTotalCount(calendarResult.data?.totalCount ?? 0); setCalendarTotalPages(calendarResult.data?.totalPages ?? 0);
    setWorkflowRows(workflowResult.data?.items ?? []); setWorkflowTotalCount(workflowResult.data?.totalCount ?? 0); setWorkflowTotalPages(workflowResult.data?.totalPages ?? 0);
    setQuestionRows(questionResult.data?.items ?? []); setQuestionTotalCount(questionResult.data?.totalCount ?? 0); setQuestionTotalPages(questionResult.data?.totalPages ?? 0);
    if (!assignmentResult.success) pushToast('error', assignmentResult.message ?? 'Unable to load C88 assignments.');
    if (!mappingResult.success) pushToast('error', mappingResult.message ?? 'Unable to load C88 mappings.');
    if (!planResult.success) pushToast('error', planResult.message ?? 'Unable to load C88 plans.');
    if (!calendarResult.success) pushToast('error', calendarResult.message ?? 'Unable to load C88 reporting calendars.');
    if (!workflowResult.success) pushToast('error', workflowResult.message ?? 'Unable to load C88 workflows.');
    if (!questionResult.success) pushToast('error', questionResult.message ?? 'Unable to load C88 compliance questions.');
  }, [activeVersionId, assignmentPage, assignmentSearch, calendarPage, calendarSearch, canReadIndicators, canReadModule, canReadReports, catalogueItem.catalogueVersionPublicId, catalogueItem.kind, cataloguePage, catalogueSearch, configurationPage, configurationSearch, effectiveConfigurationId, indicatorPage, indicatorSearch, mappingPage, mappingSearch, planPage, planSearch, pushToast, questionPage, questionSearch, reportPage, reportSearch, reportSortBy, reportSortDirection, versionPage, versionSearch, workflowPage, workflowSearch, yearId]);

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
    const eligible = yearId ? configurationRows.filter(item => item.municipalityFinancialYearPublicId === yearId) : configurationRows;
    const visible = eligible.find(item => item.publicId === configurationId);
    if (visible) { setSelectedConfiguration(visible); return; }
    if (!configurationId) {
      const first = eligible[0] ?? null;
      setConfigurationId(first?.publicId ?? '');
      setSelectedConfiguration(first);
      return;
    }
    void getC88ConfigurationsPage({ page: 1, pageSize: 1 }, { configurationPublicId: configurationId, municipalityFinancialYearPublicId: yearId || undefined }).then(result => {
      const exact = result.data?.items[0] ?? null;
      setSelectedConfiguration(exact);
      if (!exact) setConfigurationId('');
      if (!result.success) pushToast('error', result.message ?? 'Unable to load the selected C88 configuration.');
    });
  }, [configurationId, configurationRows, pushToast, yearId]);
  useEffect(() => {
    const publicId = configuration?.catalogueVersionPublicId;
    if (!publicId) { setConfiguredVersion(null); return; }
    const visible = versionRows.find(item => item.publicId === publicId);
    if (visible) { setConfiguredVersion(visible); return; }
    void getC88CatalogueVersionsPage({ page: 1, pageSize: 1 }, { catalogueVersionPublicId: publicId }).then(result => {
      setConfiguredVersion(result.data?.items[0] ?? null);
      if (!result.success) pushToast('error', result.message ?? 'Unable to load the configured C88 catalogue version.');
    });
  }, [configuration?.catalogueVersionPublicId, pushToast, versionRows]);
  useEffect(() => {
    const publicId = catalogueItem.catalogueVersionPublicId;
    if (!publicId) { setSelectedCatalogueVersion(null); return; }
    const visible = versionRows.find(item => item.publicId === publicId);
    if (visible) { setSelectedCatalogueVersion(visible); return; }
    void getC88CatalogueVersionsPage({ page: 1, pageSize: 1 }, { catalogueVersionPublicId: publicId }).then(result => {
      setSelectedCatalogueVersion(result.data?.items[0] ?? null);
      if (!result.success) pushToast('error', result.message ?? 'Unable to load the selected C88 catalogue version.');
    });
  }, [catalogueItem.catalogueVersionPublicId, pushToast, versionRows]);
  useEffect(() => {
    setCalendarId(value => calendarRows.some(item => item.publicId === value && item.configurationPublicId === effectiveConfigurationId) ? value : calendarRows.find(item => item.configurationPublicId === effectiveConfigurationId)?.publicId ?? '');
  }, [calendarRows, effectiveConfigurationId]);
  useEffect(() => { setIndicatorId(''); setSelectedIndicator(null); setIndicatorPage(1); }, [configuration?.catalogueVersionPublicId]);
  useEffect(() => {
    const visible = indicatorRows.find(item => item.publicId === indicatorId);
    if (visible) { setSelectedIndicator(visible); return; }
    if (indicatorId) return;
    const first = indicatorRows.find(item => item.catalogueVersionPublicId === configuration?.catalogueVersionPublicId && item.isActive) ?? null;
    setIndicatorId(first?.publicId ?? '');
    setSelectedIndicator(first);
  }, [configuration?.catalogueVersionPublicId, indicatorId, indicatorRows]);
  useEffect(() => {
    if (!canReadIndicators || !effectiveConfigurationId || !indicatorId) {
      setSelectedPlan(null);
      setPlan({ baselineValue: '', mediumTermTarget: '', annualTarget: '', missingDataExplanation: '', estimatedAvailability: '' });
      return;
    }
    void getC88PlansPage({ page: 1, pageSize: 1, sortBy: 'createdAt', sortDirection: 'desc' }, { configurationPublicId: effectiveConfigurationId, indicatorPublicId: indicatorId }).then(result => {
      const current = result.data?.items[0] ?? null;
      setSelectedPlan(current);
      setPlan({ baselineValue: current?.baselineValue ?? '', mediumTermTarget: current?.mediumTermTarget ?? '', annualTarget: current?.annualTarget ?? '', missingDataExplanation: current?.missingDataExplanation ?? '', estimatedAvailability: current?.estimatedAvailability?.slice(0, 10) ?? '' });
      if (!result.success) pushToast('error', result.message ?? 'Unable to load the selected C88 plan.');
    });
  }, [canReadIndicators, effectiveConfigurationId, indicatorId, pushToast]);
  useEffect(() => {
    if (!effectiveConfigurationId) { setCurrentWorkflow(null); return; }
    void getC88WorkflowsPage({ page: 1, pageSize: 1, sortBy: 'versionNumber', sortDirection: 'desc' }, { configurationPublicId: effectiveConfigurationId, current: true }).then(result => {
      setCurrentWorkflow(result.data?.items[0] ?? null);
      if (!result.success) pushToast('error', result.message ?? 'Unable to load the current C88 workflow.');
    });
  }, [effectiveConfigurationId, pushToast]);
  useEffect(() => {
    if (!selectedCalendar?.reportTypePublicId) {
      setCaptureQuestions([]);
      setCaptureQuestionTotalPages(0);
      return;
    }
    void getC88ComplianceQuestionsPage(
      { page: captureQuestionPage, pageSize: 10, sortBy: 'sequence', sortDirection: 'asc' },
      { catalogueVersionPublicId: activeVersionId || undefined, reportTypePublicId: selectedCalendar.reportTypePublicId, active: true },
    ).then(result => {
      setCaptureQuestions(result.data?.items ?? []);
      setCaptureQuestionTotalPages(result.data?.totalPages ?? 0);
      if (!result.success) pushToast('error', result.message ?? 'Unable to load C88 capture questions.');
    });
  }, [activeVersionId, captureQuestionPage, pushToast, selectedCalendar?.reportTypePublicId]);
  useEffect(() => { setCaptureQuestionPage(1); }, [calendarId]);

  const run = async (operation: () => Promise<{ success: boolean; message?: string }>, success: string) => {
    setBusy(true);
    const result = await operation();
    setBusy(false);
    if (!result.success) pushToast('error', result.message ?? 'Circular 88 action failed.');
    else { pushToast('success', success); setReason(''); await load(); }
  };

  if (!canReadModule) return <AppShell><EmptyState title="Circular 88 unavailable" description="Your effective role does not grant C88 indicator or report access." /></AppShell>;

  const publishedVersions = versionOptions.filter(item => item.isPublished && item.isActive);
  const currentReports = reportRows.filter(item => item.isCurrent !== false && item.configurationPublicId === effectiveConfigurationId);

  return <AppShell>
    <div className="space-y-4">
      <div><h1 className="text-2xl font-semibold">Circular 88</h1><p className="text-sm text-secondary-500">Independent Treasury catalogue, planning, reporting, compliance and workflow.</p></div>

      <Section title="Municipality and financial-year configuration">
        <div className="mb-3 grid gap-2 md:grid-cols-2"><label className="text-sm">Search configurations<input aria-label="Search C88 configurations" className={field} value={configurationSearch} onChange={event => { setConfigurationSearch(event.target.value); setConfigurationPage(1); }} /></label><label className="text-sm">Search catalogue editions<input aria-label="Search C88 catalogue versions" className={field} value={versionSearch} onChange={event => { setVersionSearch(event.target.value); setVersionPage(1); }} /></label></div>
        <div className="grid gap-3 md:grid-cols-4">
          <CalendarMasterPicker kind="municipality-financial-year" label="Financial year" value={yearId} onChange={value => { setYearId(value); setConfigurationId(''); setSelectedConfiguration(null); setConfigurationPage(1); setIndicatorSearch(''); setIndicatorPage(1); setReportPage(1); setPlanPage(1); setCalendarPage(1); setWorkflowPage(1); }} />
          <label className="text-sm">Configuration<select className={field} value={effectiveConfigurationId} onChange={event => { setConfigurationId(event.target.value); setIndicatorSearch(''); setIndicatorPage(1); setCalendarPage(1); setWorkflowPage(1); }}><option value="">Not configured</option>{configurationOptions.map(item => <option key={item.publicId} value={item.publicId}>{item.financialYearCode} · {item.catalogueVersionCode}</option>)}</select></label>
          <label className="text-sm">Published edition<select className={field} value={configuration?.catalogueVersionPublicId ?? catalogueItem.catalogueVersionPublicId} onChange={event => setCatalogueItem(value => ({ ...value, catalogueVersionPublicId: event.target.value }))}><option value="">Select edition</option>{publishedVersions.map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select></label>
          <div className="flex items-end"><Badge variant={configuration?.isEnabled ? 'success' : 'warning'}>{configuration?.isEnabled ? 'Enabled' : 'Disabled'}</Badge></div>
        </div>
        <div className="mt-2 flex items-center justify-between text-xs text-secondary-500"><span>{configurationTotalCount} configurations · {versionTotalCount} catalogue editions</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={configurationPage <= 1} onClick={() => setConfigurationPage(value => value - 1)}>Previous configurations</Button><Button size="sm" variant="outline" disabled={configurationPage >= configurationTotalPages} onClick={() => setConfigurationPage(value => value + 1)}>Next configurations</Button><Button size="sm" variant="outline" disabled={versionPage <= 1} onClick={() => setVersionPage(value => value - 1)}>Previous editions</Button><Button size="sm" variant="outline" disabled={versionPage >= versionTotalPages} onClick={() => setVersionPage(value => value + 1)}>Next editions</Button></div></div>
        {canExecute('C88_INDICATOR.CONFIGURE') && <div className="mt-3 flex gap-2"><Button disabled={busy || !yearId || !(configuration?.catalogueVersionPublicId || catalogueItem.catalogueVersionPublicId)} onClick={() => void run(() => configureC88({ municipalityFinancialYearPublicId: yearId, catalogueVersionPublicId: configuration?.catalogueVersionPublicId || catalogueItem.catalogueVersionPublicId, isEnabled: !(configuration?.isEnabled ?? false), effectiveFrom: new Date().toISOString(), effectiveTo: null, reason, rowVersion: configuration?.rowVersion ?? null }), configuration?.isEnabled ? 'C88 disabled without changing OPMS.' : 'C88 enabled.')}>{configuration?.isEnabled ? 'Disable C88' : 'Enable C88'}</Button><input className={field} placeholder="Reason" value={reason} onChange={event => setReason(event.target.value)} /></div>}
      </Section>

      {canExecute('C88_INDICATOR.MANAGE_CATALOGUE') && <Section title="Versioned Treasury catalogue">
        <div className="grid gap-2 md:grid-cols-5"><input className={field} placeholder="Edition code" value={edition.code} onChange={e => setEdition({ ...edition, code: e.target.value })} /><input className={field} placeholder="Edition name" value={edition.name} onChange={e => setEdition({ ...edition, name: e.target.value })} /><input className={field} type="date" value={edition.editionDate} onChange={e => setEdition({ ...edition, editionDate: e.target.value })} /><input className={field} type="date" value={edition.effectiveFrom} onChange={e => setEdition({ ...edition, effectiveFrom: e.target.value })} /><Button disabled={busy || !reason} onClick={() => void run(() => createC88CatalogueVersion({ ...edition, effectiveTo: null, isPublished: false, isActive: true, reason, rowVersion: null }), 'Catalogue edition created.')}>Create edition</Button></div>
        <div className="mt-3 grid gap-2 md:grid-cols-6"><select className={field} value={catalogueItem.catalogueVersionPublicId} onChange={e => { setCatalogueItem({ ...catalogueItem, catalogueVersionPublicId: e.target.value, parentItemPublicId: '' }); setCataloguePage(1); }}><option value="">Draft edition</option>{versionOptions.filter(item => !item.isPublished).map(item => <option key={item.publicId} value={item.publicId}>{item.code}</option>)}</select><select className={field} value={catalogueItem.kind} onChange={e => { setCatalogueItem({ ...catalogueItem, kind: e.target.value as C88CatalogueItemKind, parentItemPublicId: '' }); setCataloguePage(1); }}>{['Sector','Outcome','IndicatorType','MunicipalCategory','ReadinessTier','ReportType','ResponseType'].map(kind => <option key={kind}>{kind}</option>)}</select><input className={field} placeholder="Item code" value={catalogueItem.code} onChange={e => setCatalogueItem({ ...catalogueItem, code: e.target.value })} /><input className={field} placeholder="Item name" value={catalogueItem.name} onChange={e => setCatalogueItem({ ...catalogueItem, name: e.target.value })} />{catalogueItem.kind === 'Outcome' ? <CatalogueItemPicker versionId={catalogueItem.catalogueVersionPublicId} kind="Sector" value={catalogueItem.parentItemPublicId} onChange={value => setCatalogueItem({ ...catalogueItem, parentItemPublicId: value })} placeholder="Parent sector" /> : <div />}<Button disabled={busy || !reason} onClick={() => void run(() => createC88CatalogueItem({ ...catalogueItem, description: null, parentItemPublicId: catalogueItem.parentItemPublicId || null, displayOrder: 0, isActive: true, reason, rowVersion: null }), 'Catalogue item added.')}>Add item</Button></div>
        <div className="mt-3"><div className="flex items-end justify-between gap-2"><label className="text-sm">Search {catalogueItem.kind} items<input aria-label="Search C88 catalogue items" className={field} value={catalogueSearch} onChange={event => { setCatalogueSearch(event.target.value); setCataloguePage(1); }} /></label><span className="text-xs text-secondary-500">{catalogueTotalCount} items</span></div><div className="mt-2 grid gap-1 md:grid-cols-2">{catalogueRows.map(item => <div key={item.publicId} className="rounded border p-2 text-xs"><strong>{item.code}</strong> · {item.name}</div>)}</div>{catalogueTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={cataloguePage <= 1} onClick={() => setCataloguePage(value => value - 1)}>Previous items</Button><span className="self-center text-xs">{cataloguePage}/{catalogueTotalPages}</span><Button size="sm" variant="outline" disabled={cataloguePage >= catalogueTotalPages} onClick={() => setCataloguePage(value => value + 1)}>Next items</Button></div>}</div>
        <div className="mt-3 grid gap-2 md:grid-cols-5"><input className={field} placeholder="Indicator code" value={indicatorDraft.code} onChange={e => setIndicatorDraft({ ...indicatorDraft, code: e.target.value, catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId })} /><input className={field} placeholder="Indicator name" value={indicatorDraft.name} onChange={e => setIndicatorDraft({ ...indicatorDraft, name: e.target.value })} /><input className={field} placeholder="Official definition" value={indicatorDraft.definition} onChange={e => setIndicatorDraft({ ...indicatorDraft, definition: e.target.value })} /><input className={field} placeholder="Official TID" value={indicatorDraft.officialTechnicalIndicatorDescription} onChange={e => setIndicatorDraft({ ...indicatorDraft, officialTechnicalIndicatorDescription: e.target.value })} /><Button disabled={busy || !reason} onClick={() => void run(() => createC88Indicator({ ...indicatorDraft, catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId, sectorPublicId: null, outcomePublicId: null, indicatorTypePublicId: null, officialFormulaText: null, requiresBaseline: true, requiresMediumTermTarget: true, requiresAnnualTarget: true, isActive: true, dataElements: indicatorDraft.elementCode ? [{ code: indicatorDraft.elementCode, name: indicatorDraft.elementName, description: null, valueType: indicatorDraft.valueType, isRequired: true, sequence: 1 }] : [], applicability: indicatorDraft.municipalCategoryPublicId ? [{ municipalCategoryPublicId: indicatorDraft.municipalCategoryPublicId, readinessTierPublicId: indicatorDraft.readinessTierPublicId || null, isApplicable: true, notes: null }] : [], reason }), 'Indicator added.')}>Add indicator</Button><input className={field} placeholder="Data-element code" value={indicatorDraft.elementCode} onChange={e => setIndicatorDraft({ ...indicatorDraft, elementCode: e.target.value })} /><input className={field} placeholder="Data-element name" value={indicatorDraft.elementName} onChange={e => setIndicatorDraft({ ...indicatorDraft, elementName: e.target.value })} /><CatalogueItemPicker versionId={catalogueItem.catalogueVersionPublicId} kind="MunicipalCategory" value={indicatorDraft.municipalCategoryPublicId} onChange={value => setIndicatorDraft({ ...indicatorDraft, municipalCategoryPublicId: value })} placeholder="Municipal category" /><CatalogueItemPicker versionId={catalogueItem.catalogueVersionPublicId} kind="ReadinessTier" value={indicatorDraft.readinessTierPublicId} onChange={value => setIndicatorDraft({ ...indicatorDraft, readinessTierPublicId: value })} placeholder="Readiness tier" /></div>
        <div className="mt-3 flex flex-wrap gap-2">{versionOptions.filter(item => !item.isPublished).map(item => <Button key={item.publicId} variant="secondary" disabled={busy || !reason} onClick={() => void run(() => updateC88CatalogueVersion(item.publicId, { ...item, isPublished: true, reason }), `${item.code} published and locked.`)}>Publish {item.code}</Button>)}</div>
      </Section>}

      <Section title="Planning, assignments, workflow and mappings">
        <div className="mb-3 flex items-end justify-between gap-2"><label className="text-sm">Search indicators<input aria-label="Search C88 indicators" className={field} value={indicatorSearch} onChange={event => { setIndicatorSearch(event.target.value); setIndicatorPage(1); }} /></label><span className="text-xs text-secondary-500">{indicatorTotalCount} indicators</span></div>
        <div className="grid gap-3 md:grid-cols-3"><label className="text-sm">Indicator<select className={field} value={indicatorId} onChange={e => setIndicatorId(e.target.value)}><option value="">Select indicator</option>{indicatorOptions.map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select></label><input className={field} placeholder="Baseline" value={plan.baselineValue} onChange={e => setPlan({ ...plan, baselineValue: e.target.value })} /><input className={field} placeholder="Medium-term target" value={plan.mediumTermTarget} onChange={e => setPlan({ ...plan, mediumTermTarget: e.target.value })} /><input className={field} placeholder="Annual target" value={plan.annualTarget} onChange={e => setPlan({ ...plan, annualTarget: e.target.value })} />{canUpdate('C88_INDICATOR') && <Button disabled={busy || !effectiveConfigurationId || !indicatorId || !reason} onClick={() => void run(() => saveC88IndicatorPlan({ configurationPublicId: effectiveConfigurationId, indicatorPublicId: indicatorId, ...plan, estimatedAvailability: plan.estimatedAvailability || null, reason, rowVersion: selectedPlan?.rowVersion ?? null }), 'C88 plan saved.')}>Save plan</Button>}</div>
        {indicatorTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={indicatorPage <= 1} onClick={() => setIndicatorPage(value => value - 1)}>Previous indicators</Button><span className="self-center text-xs">{indicatorPage}/{indicatorTotalPages}</span><Button size="sm" variant="outline" disabled={indicatorPage >= indicatorTotalPages} onClick={() => setIndicatorPage(value => value + 1)}>Next indicators</Button></div>}
        {canExecute('C88_INDICATOR.MANAGE_ASSIGNMENTS') && <div className="mt-3 space-y-2"><div className="grid gap-2 md:grid-cols-4"><input aria-label="Search C88 assignment employees" className={field} placeholder="Search employees" value={employeeSearch} onChange={e => { setEmployeeSearch(e.target.value); setEmployeePage(1); }} /><select aria-label="C88 assignment employee" className={field} value={assignment.employeePublicId} onChange={e => setAssignment({ ...assignment, employeePublicId: e.target.value })}><option value="">Employee</option>{employees.map(item => <option key={item.publicId} value={item.publicId}>{item.firstName} {item.lastName}</option>)}</select><select aria-label="C88 assignment role" className={field} value={assignment.role} onChange={e => setAssignment({ ...assignment, role: e.target.value })}>{['PrimaryCapturer','Contributor','ReviewerVerifier','FinalSubmitter'].map(role => <option key={role}>{role}</option>)}</select><Button disabled={busy || !effectiveConfigurationId || !indicatorId || !reason} onClick={() => void run(() => createC88Assignment({ configurationPublicId: effectiveConfigurationId, indicatorPublicId: indicatorId, ...assignment, effectiveFrom: new Date().toISOString(), effectiveTo: null, isActive: true, reason, rowVersion: null }), 'Assignment created.')}>Assign</Button></div>{employeeTotalPages > 1 && <div className="flex items-center justify-end gap-2 text-xs text-secondary-500"><Button size="sm" variant="outline" disabled={employeePage <= 1} onClick={() => setEmployeePage(value => Math.max(1, value - 1))}>Previous employees</Button><span>Page {employeePage} of {employeeTotalPages}</span><Button size="sm" variant="outline" disabled={employeePage >= employeeTotalPages} onClick={() => setEmployeePage(value => value + 1)}>Next employees</Button></div>}</div>}
        {canExecute('C88_INDICATOR.MANAGE_WORKFLOW') && <div className="mt-3"><Button disabled={busy || !effectiveConfigurationId || !reason} onClick={() => void run(() => createC88Workflow({ configurationPublicId: effectiveConfigurationId, effectiveFrom: new Date().toISOString(), effectiveTo: null, stages: [{ sequence: 1, kind: 'Capturer', name: 'Capturer', requiredRole: 'PrimaryCapturer', isActive: true }, { sequence: 2, kind: 'ReviewerVerifier', name: 'Reviewer / Verifier', requiredRole: 'ReviewerVerifier', isActive: true }, { sequence: 3, kind: 'FinalSubmission', name: 'Final Submission', requiredRole: 'FinalSubmitter', isActive: true }], reason, previousWorkflowPublicId: currentWorkflow?.publicId ?? null, previousWorkflowRowVersion: currentWorkflow?.rowVersion ?? null }), 'Independent C88 workflow version created.')}>Create workflow version</Button></div>}
        {canExecute('C88_INDICATOR.MANAGE_MAPPING') && <div className="mt-3 grid gap-2 md:grid-cols-4"><TargetPicker kind="opms" label="OPMS KPI" value={mapping.opmsTargetPublicId} valueField="publicId" onChange={value => setMapping({ ...mapping, opmsTargetPublicId: value })} /><select aria-label="Mapping type" className={field} value={mapping.mappingType} onChange={e => setMapping({ ...mapping, mappingType: e.target.value })}><option>Direct</option><option>Contributing</option></select><Button disabled={busy || !effectiveConfigurationId || !indicatorId || !reason} onClick={() => void run(() => createC88Mapping({ configurationPublicId: effectiveConfigurationId, indicatorPublicId: indicatorId, ...mapping, reason, isActive: true, rowVersion: null }), 'Alignment-only mapping created.')}>Map without copying</Button></div>}
        {canReadIndicators && <div className="mt-4"><div className="flex items-end justify-between gap-2"><label className="text-sm">Search plans<input aria-label="Search C88 plans" className={field} value={planSearch} onChange={event => { setPlanSearch(event.target.value); setPlanPage(1); }} /></label><span className="text-xs text-secondary-500">{planTotalCount} plans</span></div><div className="mt-2 grid gap-2 md:grid-cols-2">{planRows.map(item => <button type="button" key={item.publicId} onClick={() => { setConfigurationId(item.configurationPublicId); setIndicatorSearch(item.indicatorCode); setIndicatorPage(1); setIndicatorId(item.indicatorPublicId); }} className="rounded border p-2 text-left text-xs"><strong>{item.indicatorCode}</strong><br /><span className="text-secondary-500">Baseline: {item.baselineValue ?? 'Not set'} · Annual: {item.annualTarget ?? 'Not set'}</span></button>)}</div>{planTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={planPage <= 1} onClick={() => setPlanPage(value => value - 1)}>Previous plans</Button><span className="self-center text-xs">{planPage}/{planTotalPages}</span><Button size="sm" variant="outline" disabled={planPage >= planTotalPages} onClick={() => setPlanPage(value => value + 1)}>Next plans</Button></div>}</div>}
        <div className="mt-4 grid gap-4 lg:grid-cols-2">
          <div><div className="flex items-end justify-between gap-2"><label className="text-sm">Search assignments<input aria-label="Search C88 assignments" className={field} value={assignmentSearch} onChange={event => { setAssignmentSearch(event.target.value); setAssignmentPage(1); }} /></label><span className="text-xs text-secondary-500">{assignmentTotalCount} assignments</span></div><div className="mt-2 space-y-1">{assignmentRows.map(item => <div key={item.publicId} className="rounded border p-2 text-xs"><strong>{item.employeeName}</strong> · {item.role}<br /><span className="text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleDateString()} · {item.isActive ? 'Active' : 'Inactive'}</span></div>)}</div>{assignmentTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={assignmentPage <= 1} onClick={() => setAssignmentPage(value => value - 1)}>Previous</Button><span className="self-center text-xs">{assignmentPage}/{assignmentTotalPages}</span><Button size="sm" variant="outline" disabled={assignmentPage >= assignmentTotalPages} onClick={() => setAssignmentPage(value => value + 1)}>Next</Button></div>}</div>
          <div><div className="flex items-end justify-between gap-2"><label className="text-sm">Search mappings<input aria-label="Search C88 mappings" className={field} value={mappingSearch} onChange={event => { setMappingSearch(event.target.value); setMappingPage(1); }} /></label><span className="text-xs text-secondary-500">{mappingTotalCount} mappings</span></div><div className="mt-2 space-y-1">{mappingRows.map(item => <div key={item.publicId} className="rounded border p-2 text-xs"><strong>{item.opmsIndicatorNumber}</strong> · {item.mappingType}<br /><span className="text-secondary-500">{item.reason} · {item.isActive ? 'Active' : 'Inactive'}</span></div>)}</div>{mappingTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={mappingPage <= 1} onClick={() => setMappingPage(value => value - 1)}>Previous</Button><span className="self-center text-xs">{mappingPage}/{mappingTotalPages}</span><Button size="sm" variant="outline" disabled={mappingPage >= mappingTotalPages} onClick={() => setMappingPage(value => value + 1)}>Next</Button></div>}</div>
        </div>
        <div className="mt-4"><div className="flex items-end justify-between gap-2"><label className="text-sm">Search workflow versions<input aria-label="Search C88 workflows" className={field} value={workflowSearch} onChange={event => { setWorkflowSearch(event.target.value); setWorkflowPage(1); }} /></label><span className="text-xs text-secondary-500">{workflowTotalCount} workflow versions</span></div><div className="mt-2 grid gap-2 md:grid-cols-2">{workflowRows.map(item => <div key={item.publicId} className="rounded border p-2 text-xs"><strong>Version {item.versionNumber}</strong> · {item.isCurrent ? 'Current' : 'Historic'}<br /><span className="text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleDateString()} · {item.stages.length} stages</span></div>)}</div>{workflowTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={workflowPage <= 1} onClick={() => setWorkflowPage(value => value - 1)}>Previous workflows</Button><span className="self-center text-xs">{workflowPage}/{workflowTotalPages}</span><Button size="sm" variant="outline" disabled={workflowPage >= workflowTotalPages} onClick={() => setWorkflowPage(value => value + 1)}>Next workflows</Button></div>}</div>
      </Section>

      <Section title="Reporting calendar and capture">
        {canExecute('C88_INDICATOR.MANAGE_WORKFLOW') && <div className="grid gap-2 md:grid-cols-5"><CatalogueItemPicker versionId={activeVersionId} kind="ReportType" value={calendar.reportTypePublicId} onChange={value => setCalendar({ ...calendar, reportTypePublicId: value })} placeholder="Report type" /><input className={field} placeholder="Calendar code" value={calendar.code} onChange={e => setCalendar({ ...calendar, code: e.target.value })} /><input className={field} placeholder="Calendar name" value={calendar.name} onChange={e => setCalendar({ ...calendar, name: e.target.value })} /><Button disabled={busy || !effectiveConfigurationId || !reason} onClick={() => void run(() => createC88Calendar({ configurationPublicId: effectiveConfigurationId, ...calendar, reportingPeriodPublicId: null, isActive: true, reason, rowVersion: null }), 'Reporting calendar created.')}>Create calendar</Button></div>}
        <div className="mt-3 flex items-end justify-between gap-2"><label className="text-sm">Search calendars<input aria-label="Search C88 calendars" className={field} value={calendarSearch} onChange={event => { setCalendarSearch(event.target.value); setCalendarPage(1); }} /></label><span className="text-xs text-secondary-500">{calendarTotalCount} calendars</span></div>
        <div className="mt-3 grid gap-2 md:grid-cols-3"><select className={field} value={calendarId} onChange={e => { setCalendarId(e.target.value); setCaptureQuestionPage(1); }}><option value="">Calendar</option>{calendarRows.map(item => <option key={item.publicId} value={item.publicId}>{item.code} · {item.name}</option>)}</select>{indicator?.dataElements.map(element => <input key={element.publicId} className={field} placeholder={`${element.code} · ${element.name}`} value={reportValues[element.publicId] ?? ''} onChange={e => setReportValues({ ...reportValues, [element.publicId]: e.target.value })} />)}{captureQuestions.map(question => <input key={question.publicId} className={field} placeholder={question.prompt} value={responses[question.publicId] ?? ''} onChange={e => setResponses({ ...responses, [question.publicId]: e.target.value })} />)}{canCreate('C88_REPORT') && <Button disabled={busy || !reason || !calendarId || !indicatorId} onClick={() => void run(() => createC88ReportVersion({ configurationPublicId: effectiveConfigurationId, calendarPublicId: calendarId, indicatorPublicId: indicatorId, previousReportPublicId: selectedReport?.publicId ?? null, previousReportRowVersion: selectedReport?.rowVersion ?? null, missingDataExplanation: null, estimatedAvailability: null, dataElementValues: indicator?.dataElements.map(element => ({ dataElementPublicId: element.publicId, value: reportValues[element.publicId] || null, missingDataExplanation: null, estimatedAvailability: null })) ?? [], complianceResponses: Object.entries(responses).map(([questionPublicId, response]) => ({ questionPublicId, response: response || null, comment: null })), reason }), 'C88 report version created.')}>Save report version</Button>}</div>
        {captureQuestionTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={captureQuestionPage <= 1} onClick={() => setCaptureQuestionPage(value => value - 1)}>Previous questions</Button><span className="self-center text-xs">Question page {captureQuestionPage}/{captureQuestionTotalPages}</span><Button size="sm" variant="outline" disabled={captureQuestionPage >= captureQuestionTotalPages} onClick={() => setCaptureQuestionPage(value => value + 1)}>Next questions</Button></div>}
        {calendarTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={calendarPage <= 1} onClick={() => setCalendarPage(value => value - 1)}>Previous calendars</Button><span className="self-center text-xs">{calendarPage}/{calendarTotalPages}</span><Button size="sm" variant="outline" disabled={calendarPage >= calendarTotalPages} onClick={() => setCalendarPage(value => value + 1)}>Next calendars</Button></div>}
      </Section>

      <Section title="Governed report register">
        {canReadReports && <div className="mb-3 grid gap-2 md:grid-cols-[minmax(0,1fr)_13rem_10rem]"><label className="text-sm">Search reports<input className={field} value={reportSearchInput} onChange={event => setReportSearchInput(event.target.value)} placeholder="Indicator code or name" /></label><label className="text-sm">Sort reports<select className={field} value={reportSortBy} onChange={event => { setReportPage(1); setReportSortBy(event.target.value); }}><option value="createdAt">Created</option><option value="indicatorCode">Indicator code</option><option value="state">State</option><option value="versionNumber">Version</option></select></label><label className="text-sm">Direction<select className={field} value={reportSortDirection} onChange={event => { setReportPage(1); setReportSortDirection(event.target.value as 'asc' | 'desc'); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select></label></div>}
        <div className="space-y-2">{currentReports.map(report => <button type="button" key={report.publicId} onClick={() => setSelectedReport(report)} className="w-full rounded border p-3 text-left"><div className="flex justify-between"><strong>{report.indicatorCode} · v{report.versionNumber}</strong><Badge variant={report.state === 'FinalSubmitted' ? 'success' : 'default'}>{report.state}</Badge></div><div className="text-xs text-secondary-500">Calculated value: {report.calculatedValue ?? 'Not calculated'} · Stage {report.currentStageSequence}</div></button>)}</div>
        {canReadReports && reportTotalPages > 1 && <div className="mt-3 flex items-center justify-between"><p className="text-xs text-secondary-500">Page {reportPage} of {reportTotalPages} · {reportTotalCount} reports</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={busy || reportPage === 1} onClick={() => setReportPage(value => Math.max(1, value - 1))}>Previous</Button><Button variant="outline" size="sm" disabled={busy || reportPage === reportTotalPages} onClick={() => setReportPage(value => Math.min(reportTotalPages, value + 1))}>Next</Button></div></div>}
        {selectedReport && <div className="mt-3 flex flex-wrap gap-2"><input className={field} placeholder="Workflow reason" value={reason} onChange={e => setReason(e.target.value)} />{canExecute('C88_REPORT.SUBMIT') && ['Draft','Rework'].includes(selectedReport.state) && <Button disabled={busy || !reason} onClick={() => void run(() => submitC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report submitted.')}>Submit</Button>}{canExecute('C88_REPORT.VERIFY') && selectedReport.state === 'Submitted' && <Button disabled={busy || !reason} onClick={() => void run(() => verifyC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report verified.')}>Verify</Button>}{canExecute('C88_REPORT.RETURN') && ['Submitted','Verified'].includes(selectedReport.state) && <Button variant="secondary" disabled={busy || !reason} onClick={() => void run(() => returnC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report returned for rework.')}>Return</Button>}{canExecute('C88_REPORT.FINAL_SUBMIT') && selectedReport.state === 'Verified' && <Button disabled={busy || !reason} onClick={() => void run(() => finalSubmitC88Report(selectedReport.publicId, selectedReport.rowVersion, reason), 'Report finally submitted.')}>Final submit</Button>}</div>}
      </Section>

      {canExecute('C88_INDICATOR.MANAGE_CATALOGUE') && <Section title="Compliance question administration"><div className="grid gap-2 md:grid-cols-2"><CatalogueItemPicker versionId={catalogueItem.catalogueVersionPublicId} kind="ReportType" value={questionReportTypeId} onChange={setQuestionReportTypeId} placeholder="Question report type" /><CatalogueItemPicker versionId={catalogueItem.catalogueVersionPublicId} kind="ResponseType" value={questionResponseTypeId} onChange={setQuestionResponseTypeId} placeholder="Question response type" /></div><div className="mt-3 flex items-end justify-between gap-2"><label className="text-sm">Search questions<input aria-label="Search C88 compliance questions" className={field} value={questionSearch} onChange={event => { setQuestionSearch(event.target.value); setQuestionPage(1); }} /></label><span className="text-xs text-secondary-500">{questionTotalCount} questions</span></div><div className="mt-2 space-y-1">{questionRows.map(item => <div key={item.publicId} className="rounded border p-2 text-xs"><strong>{item.code}</strong> · {item.prompt}<br /><span className="text-secondary-500">Sequence {item.sequence} · {item.isRequired ? 'Required' : 'Optional'} · {item.isActive ? 'Active' : 'Inactive'}</span></div>)}</div>{questionTotalPages > 1 && <div className="mt-2 flex justify-end gap-2"><Button size="sm" variant="outline" disabled={questionPage <= 1} onClick={() => setQuestionPage(value => value - 1)}>Previous</Button><span className="self-center text-xs">{questionPage}/{questionTotalPages}</span><Button size="sm" variant="outline" disabled={questionPage >= questionTotalPages} onClick={() => setQuestionPage(value => value + 1)}>Next</Button></div>}<Button className="mt-3" disabled={busy || !reason || !questionReportTypeId || !questionResponseTypeId} onClick={() => void run(async () => { const latest = await getC88ComplianceQuestionsPage({ page: 1, pageSize: 1, sortBy: 'sequence', sortDirection: 'desc' }, { catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId }); if (!latest.success) return latest; const sequence = (latest.data?.items[0]?.sequence ?? 0) + 1; return createC88ComplianceQuestion({ catalogueVersionPublicId: catalogueItem.catalogueVersionPublicId, reportTypePublicId: questionReportTypeId, responseTypePublicId: questionResponseTypeId, code: `Q${sequence}`, prompt: 'New governed compliance question', isRequired: true, sequence, isActive: true, reason }); }, 'Compliance question created.')}>Add required question</Button></Section>}
    </div>
  </AppShell>;
}
