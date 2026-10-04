import { useCallback, useEffect, useMemo, useState } from 'react';
import { BarChart3, Download, FileText, History, RefreshCw, Target, TrendingUp } from 'lucide-react';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Select } from '../common/Form';
import { downloadOfficialReport, downloadPerformanceReportCsv, generateOfficialReport, getDepartments, getMunicipalityFinancialYearMasters, getOfficialReportGenerationsPage, getOfficialReportJobsPage, getOfficialReportSchedulesPage, getOfficialReportTemplates, getPerformanceReportSummary, getReportingPeriodMasters, getUnits, queueOfficialReportJob, retryOfficialReportJob, runOfficialReportSchedule, saveOfficialReportSchedule, saveOfficialReportTemplate } from '../../api/api';
import type { DepartmentLookupDto, MunicipalityFinancialYearMasterDto, OfficialReportFormat, OfficialReportGenerationDto, OfficialReportJobDto, OfficialReportRecipientKind, OfficialReportScheduleCadence, OfficialReportScheduleDto, OfficialReportTemplateDto, OfficialReportType, PerformanceReportSummaryDto, ReportingPeriodMasterDto, UnitLookupDto } from '../../types';
import { useApp } from '../../context/AppContext';

const reportTypes: { value: OfficialReportType; label: string; code: string; columns: string[] }[] = [
  { value: 1, label: 'Quarterly OPMS/IPMS performance', code: 'QUARTERLY', columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'] },
  { value: 2, label: 'Mid-Term performance', code: 'MID_TERM', columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'] },
  { value: 3, label: 'Annual performance', code: 'ANNUAL', columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'] },
  { value: 4, label: 'Departmental performance', code: 'DEPARTMENTAL', columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'] },
  { value: 5, label: 'Unit performance', code: 'UNIT', columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'] },
  { value: 6, label: 'Performance summary', code: 'PERFORMANCE_SUMMARY', columns: ['group', 'configuredTargets', 'submissions', 'achieved', 'atRisk', 'pending', 'averageAchievementPercent'] },
  { value: 7, label: 'Workflow status', code: 'WORKFLOW_STATUS', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'workflow', 'status', 'currentStage', 'startedAt', 'completedAt'] },
  { value: 8, label: 'Submission register', code: 'SUBMISSIONS', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'actualPerformance', 'achievementPercent', 'submittedBy', 'submittedAt', 'status'] },
  { value: 9, label: 'Verification register', code: 'VERIFICATION', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'action', 'actor', 'occurredAt', 'comment', 'rating'] },
  { value: 10, label: 'Approval register', code: 'APPROVAL', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'action', 'actor', 'occurredAt', 'comment', 'rating'] },
  { value: 11, label: 'PMS review', code: 'PMS', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'action', 'actor', 'occurredAt', 'comment', 'rating'] },
  { value: 12, label: 'Internal Audit assessment', code: 'INTERNAL_AUDIT', columns: ['submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'outcome', 'observation', 'findings', 'recommendation', 'score', 'actor', 'occurredAt'] },
  { value: 13, label: 'Outstanding RFI', code: 'OUTSTANDING_RFI', columns: ['rfiId', 'submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'question', 'status', 'raisedBy', 'raisedAt', 'responseDueAt', 'response', 'respondedBy', 'respondedAt'] },
  { value: 14, label: 'POE / evidence register', code: 'EVIDENCE', columns: ['evidenceId', 'submissionId', 'indicator', 'targetName', 'department', 'unit', 'period', 'fileName', 'contentType', 'sizeInBytes', 'sha256', 'scanStatus', 'uploadedBy', 'uploadedAt', 'retainUntil'] },
  { value: 15, label: 'Audit trail', code: 'AUDIT_TRAIL', columns: ['entityName', 'entityId', 'action', 'changedBy', 'changedAt', 'reason', 'correlationId'] },
  { value: 16, label: 'Version trail', code: 'VERSION_TRAIL', columns: ['source', 'entityId', 'field', 'originalValue', 'revisedValue', 'versionNumber', 'actor', 'effectiveAt', 'reason', 'approvalReference'] },
];
const reportTypeLabel = (value: OfficialReportType) => reportTypes.find(item => item.value === value)?.label ?? 'Official report';
const jobStateLabel = (value: number) => ['', 'Queued', 'Processing', 'Retry pending', 'Completed', 'Failed', 'Cancelled'][value] ?? 'Unknown';
const storedGenerationFilters = (generation?: OfficialReportGenerationDto) => {
  if (!generation) return {};
  try {
    const value = JSON.parse(generation.filterJson) as { departmentPublicId?: string | null; unitPublicId?: string | null };
    return { departmentPublicId: value.departmentPublicId ?? undefined, unitPublicId: value.unitPublicId ?? undefined };
  } catch { return {}; }
};

export function Reports() {
  const { permissions, pushToast } = useApp();
  const [kind, setKind] = useState<1 | 2>(1);
  const [periodId, setPeriodId] = useState('');
  const [periods, setPeriods] = useState<ReportingPeriodMasterDto[]>([]);
  const [years, setYears] = useState<MunicipalityFinancialYearMasterDto[]>([]);
  const [yearId, setYearId] = useState('');
  const [templates, setTemplates] = useState<OfficialReportTemplateDto[]>([]);
  const [generations, setGenerations] = useState<OfficialReportGenerationDto[]>([]);
  const [generationPage, setGenerationPage] = useState(1);
  const [generationTotalCount, setGenerationTotalCount] = useState(0);
  const [generationTotalPages, setGenerationTotalPages] = useState(0);
  const [generationSearchInput, setGenerationSearchInput] = useState('');
  const [generationSearch, setGenerationSearch] = useState('');
  const [generationSortBy, setGenerationSortBy] = useState('generatedAt');
  const [generationSortDirection, setGenerationSortDirection] = useState<'asc' | 'desc'>('desc');
  const [jobs, setJobs] = useState<OfficialReportJobDto[]>([]);
  const [jobPage, setJobPage] = useState(1);
  const [jobTotalCount, setJobTotalCount] = useState(0);
  const [jobTotalPages, setJobTotalPages] = useState(0);
  const [jobSearchInput, setJobSearchInput] = useState('');
  const [jobSearch, setJobSearch] = useState('');
  const [jobSortBy, setJobSortBy] = useState('requestedAt');
  const [jobSortDirection, setJobSortDirection] = useState<'asc' | 'desc'>('desc');
  const [schedules, setSchedules] = useState<OfficialReportScheduleDto[]>([]);
  const [schedulePage, setSchedulePage] = useState(1);
  const [scheduleTotalCount, setScheduleTotalCount] = useState(0);
  const [scheduleTotalPages, setScheduleTotalPages] = useState(0);
  const [scheduleSearchInput, setScheduleSearchInput] = useState('');
  const [scheduleSearch, setScheduleSearch] = useState('');
  const [scheduleSortBy, setScheduleSortBy] = useState('code');
  const [scheduleSortDirection, setScheduleSortDirection] = useState<'asc' | 'desc'>('asc');
  const [templateId, setTemplateId] = useState('');
  const [departments, setDepartments] = useState<DepartmentLookupDto[]>([]);
  const [units, setUnits] = useState<UnitLookupDto[]>([]);
  const [departmentId, setDepartmentId] = useState('');
  const [unitId, setUnitId] = useState('');
  const [templateDraft, setTemplateDraft] = useState({ reportType: 1 as OfficialReportType, code: 'QUARTERLY', name: 'Quarterly performance report', format: 4 as OfficialReportFormat, headingTemplate: '{Municipality} · {FinancialYear} {Period} PERFORMANCE REPORT', approvalReference: '', reason: '', previousVersionPublicId: '' });
  const [scheduleDraft, setScheduleDraft] = useState({ previousVersionPublicId: '', code: 'REPORT-DISTRIBUTION', name: 'Official report distribution', cadence: 1 as OfficialReportScheduleCadence, interval: 1, nextRunAt: '', recipientKind: 1 as OfficialReportRecipientKind, recipientValues: '', channels: 'IN_APP', isMandatory: true, isActive: true, approvalReference: '', reason: '' });
  const [retryReasons, setRetryReasons] = useState<Record<string, string>>({});
  const [summary, setSummary] = useState<PerformanceReportSummaryDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const permissionSet = useMemo(() => new Set(permissions.map(value => value.toUpperCase())), [permissions]);
  const canExport = permissionSet.has(kind === 1 ? 'OPMS_REPORT.EXPORT' : 'IPMS_REPORT.EXPORT') || permissionSet.has('REPORTS.EXPORT');
  const canReadOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.READ' : 'IPMS_REPORT.READ') || permissionSet.has('REPORTS.VIEW');
  const canGenerateOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.GENERATE' : 'IPMS_REPORT.GENERATE') || permissionSet.has('REPORTS.GENERATE');
  const canConfigureOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.CONFIGURE' : 'IPMS_REPORT.CONFIGURE');
  const availablePeriods = useMemo(() => periods.filter(period => !yearId || period.municipalityFinancialYearPublicId === yearId), [periods, yearId]);
  const selectedTemplate = templates.find(template => template.publicId === templateId);
  const selectedDepartment = departments.find(department => department.publicId === departmentId);
  const filteredUnits = units.filter(unit => !selectedDepartment || unit.departmentId === selectedDepartment.id);

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [periodResult, yearResult, summaryResult, templateResult, departmentResult, unitResult] = await Promise.all([
      getReportingPeriodMasters(),
      getMunicipalityFinancialYearMasters(),
      getPerformanceReportSummary(kind, periodId || undefined),
      canReadOfficial ? getOfficialReportTemplates(kind) : Promise.resolve({ success: true, data: [] as OfficialReportTemplateDto[], message: undefined }),
      getDepartments(),
      getUnits(),
    ]);
    setPeriods(periodResult.data ?? []);
    const loadedYears = yearResult.data ?? [];
    setYears(loadedYears);
    setYearId(current => current || loadedYears.find(year => year.isCurrent)?.publicId || loadedYears[0]?.publicId || '');
    setTemplates(templateResult.data ?? []);
    setTemplateId(current => (templateResult.data ?? []).some(template => template.publicId === current) ? current : templateResult.data?.[0]?.publicId ?? '');
    setDepartments(departmentResult.data ?? []);
    setUnits(unitResult.data ?? []);
    if (!summaryResult.success || !summaryResult.data) { setSummary(null); setError(summaryResult.message ?? 'Report could not be generated.'); }
    else setSummary(summaryResult.data);
    if (!periodResult.success) setError(periodResult.message ?? 'Reporting periods could not be loaded.');
    else if (!yearResult.success) setError(yearResult.message ?? 'Municipality financial years could not be loaded.');
    else if (!templateResult.success) setError(templateResult.message ?? 'Official report templates could not be loaded.');
    else if (!departmentResult.success || !unitResult.success) setError('Department and unit report filters could not be loaded.');
    setBusy(false);
  }, [canReadOfficial, kind, periodId]);

  const loadJobs = useCallback(async () => {
    if (!canReadOfficial) {
      setJobs([]); setJobTotalCount(0); setJobTotalPages(0); return;
    }
    const result = await getOfficialReportJobsPage(kind, { page: jobPage, pageSize: 25, search: jobSearch, sortBy: jobSortBy, sortDirection: jobSortDirection });
    setJobs(result.data?.items ?? []);
    setJobTotalCount(result.data?.totalCount ?? 0);
    setJobTotalPages(result.data?.totalPages ?? 0);
    if (!result.success) setError(result.message ?? 'Official report jobs could not be loaded.');
  }, [canReadOfficial, jobPage, jobSearch, jobSortBy, jobSortDirection, kind]);

  const loadGenerations = useCallback(async () => {
    if (!canReadOfficial) {
      setGenerations([]); setGenerationTotalCount(0); setGenerationTotalPages(0); return;
    }
    const result = await getOfficialReportGenerationsPage(kind, periodId || undefined, { page: generationPage, pageSize: 25, search: generationSearch, sortBy: generationSortBy, sortDirection: generationSortDirection });
    setGenerations(result.data?.items ?? []);
    setGenerationTotalCount(result.data?.totalCount ?? 0);
    setGenerationTotalPages(result.data?.totalPages ?? 0);
    if (!result.success) setError(result.message ?? 'Official report history could not be loaded.');
  }, [canReadOfficial, generationPage, generationSearch, generationSortBy, generationSortDirection, kind, periodId]);

  const loadSchedules = useCallback(async () => {
    if (!canConfigureOfficial) {
      setSchedules([]); setScheduleTotalCount(0); setScheduleTotalPages(0); return;
    }
    const result = await getOfficialReportSchedulesPage(kind, false, { page: schedulePage, pageSize: 25, search: scheduleSearch, sortBy: scheduleSortBy, sortDirection: scheduleSortDirection });
    setSchedules(result.data?.items ?? []);
    setScheduleTotalCount(result.data?.totalCount ?? 0);
    setScheduleTotalPages(result.data?.totalPages ?? 0);
    if (!result.success) setError(result.message ?? 'Official report schedules could not be loaded.');
  }, [canConfigureOfficial, kind, schedulePage, scheduleSearch, scheduleSortBy, scheduleSortDirection]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { void loadGenerations(); }, [loadGenerations]);
  useEffect(() => { void loadJobs(); }, [loadJobs]);
  useEffect(() => { void loadSchedules(); }, [loadSchedules]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setGenerationPage(1); setGenerationSearch(generationSearchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [generationSearchInput]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setJobPage(1); setJobSearch(jobSearchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [jobSearchInput]);
  useEffect(() => {
    const timeout = window.setTimeout(() => { setSchedulePage(1); setScheduleSearch(scheduleSearchInput.trim()); }, 300);
    return () => window.clearTimeout(timeout);
  }, [scheduleSearchInput]);

  const exportCsv = async () => {
    setBusy(true); setError(null);
    const result = await downloadPerformanceReportCsv(kind, periodId || undefined);
    if (!result.success) setError(result.message ?? 'CSV export failed.');
    else pushToast('success', 'Performance CSV downloaded');
    setBusy(false);
  };

  const generate = async (previous?: OfficialReportGenerationDto) => {
    const effectiveTemplateId = previous?.templatePublicId ?? templateId;
    const effectiveYearId = previous?.municipalityFinancialYearPublicId ?? yearId;
    const effectivePeriodId = previous?.reportingPeriodPublicId ?? periodId;
    if (!effectiveTemplateId || !effectiveYearId || !effectivePeriodId) { setError('Select a financial year, reporting period and approved template.'); return; }
    const effectiveTemplate = templates.find(template => template.publicId === effectiveTemplateId);
    if (!previous && effectiveTemplate?.reportType === 4 && !departmentId) { setError('Select a department for the departmental report.'); return; }
    if (!previous && effectiveTemplate?.reportType === 5 && !unitId) { setError('Select a unit for the unit report.'); return; }
    const priorFilters = storedGenerationFilters(previous);
    setBusy(true); setError(null);
    const result = await generateOfficialReport({ templatePublicId: effectiveTemplateId, municipalityFinancialYearPublicId: effectiveYearId, reportingPeriodPublicId: effectivePeriodId, previousGenerationPublicId: previous?.publicId, departmentPublicId: previous ? priorFilters.departmentPublicId : departmentId || undefined, unitPublicId: previous ? priorFilters.unitPublicId : unitId || undefined });
    if (!result.success) setError(result.message ?? 'Official report generation failed.');
    else { pushToast('success', previous ? 'New immutable official report version generated' : 'Official report generated'); await loadGenerations(); }
    setBusy(false);
  };

  const downloadOfficial = async (generation: OfficialReportGenerationDto) => {
    setBusy(true); setError(null);
    const result = await downloadOfficialReport(generation.publicId, generation.fileName);
    if (!result.success) setError(result.message ?? 'Official report download failed.');
    setBusy(false);
  };

  const queueGeneration = async (previous?: OfficialReportGenerationDto) => {
    const effectiveTemplateId = previous?.templatePublicId ?? templateId;
    const effectiveYearId = previous?.municipalityFinancialYearPublicId ?? yearId;
    const effectivePeriodId = previous?.reportingPeriodPublicId ?? periodId;
    const effectiveTemplate = templates.find(template => template.publicId === effectiveTemplateId);
    if (!effectiveTemplateId || !effectiveYearId || !effectivePeriodId) { setError('Select a financial year, reporting period and approved template.'); return; }
    if (!previous && effectiveTemplate?.reportType === 4 && !departmentId) { setError('Select a department for the departmental report.'); return; }
    if (!previous && effectiveTemplate?.reportType === 5 && !unitId) { setError('Select a unit for the unit report.'); return; }
    const priorFilters = storedGenerationFilters(previous);
    setBusy(true); setError(null);
    const result = await queueOfficialReportJob({ templatePublicId: effectiveTemplateId, municipalityFinancialYearPublicId: effectiveYearId, reportingPeriodPublicId: effectivePeriodId, previousGenerationPublicId: previous?.publicId, departmentPublicId: previous ? priorFilters.departmentPublicId : departmentId || undefined, unitPublicId: previous ? priorFilters.unitPublicId : unitId || undefined });
    if (!result.success) setError(result.message ?? 'Official report job could not be queued.');
    else { pushToast('success', 'Official report generation queued'); await loadJobs(); }
    setBusy(false);
  };

  const saveSchedule = async () => {
    if (!templateId || !yearId || !periodId) { setError('Select a template, financial year and reporting period before configuring a schedule.'); return; }
    const previous = schedules.find(schedule => schedule.publicId === scheduleDraft.previousVersionPublicId);
    const values = scheduleDraft.recipientValues.split(',').map(value => value.trim()).filter(Boolean);
    const channels = scheduleDraft.channels.split(',').map(value => value.trim().toUpperCase()).filter(Boolean);
    if (!scheduleDraft.code.trim() || !scheduleDraft.name.trim() || !scheduleDraft.approvalReference.trim() || scheduleDraft.reason.trim().length < 5 || !values.length || !channels.length || scheduleDraft.isActive && !scheduleDraft.nextRunAt) { setError('Complete the schedule identity, next run, recipients, channels, approval reference and audit reason.'); return; }
    setBusy(true); setError(null);
    const result = await saveOfficialReportSchedule({
      previousVersionPublicId: previous?.publicId, previousVersionRowVersion: previous?.rowVersion,
      templatePublicId: templateId, municipalityFinancialYearPublicId: yearId, reportingPeriodPublicId: periodId,
      departmentPublicId: departmentId || undefined, unitPublicId: unitId || undefined,
      code: scheduleDraft.code, name: scheduleDraft.name, cadence: scheduleDraft.cadence, interval: scheduleDraft.interval,
      nextRunAt: scheduleDraft.nextRunAt ? new Date(scheduleDraft.nextRunAt).toISOString() : undefined,
      recipientKind: scheduleDraft.recipientKind, recipientValues: values, channels, isMandatory: scheduleDraft.isMandatory, isActive: scheduleDraft.isActive,
      approvalReference: scheduleDraft.approvalReference, reason: scheduleDraft.reason,
    });
    if (!result.success) setError(result.message ?? 'Official report schedule could not be saved.');
    else { pushToast('success', previous ? 'Report schedule version created' : 'Report schedule created'); setScheduleDraft(value => ({ ...value, previousVersionPublicId: '', approvalReference: '', reason: '' })); await loadSchedules(); }
    setBusy(false);
  };

  const runSchedule = async (schedule: OfficialReportScheduleDto) => {
    setBusy(true); setError(null);
    const result = await runOfficialReportSchedule(schedule.publicId);
    if (!result.success) setError(result.message ?? 'Scheduled report run could not be queued.'); else { pushToast('success', 'Scheduled report run queued'); await loadJobs(); }
    setBusy(false);
  };

  const retryJob = async (job: OfficialReportJobDto) => {
    const reason = retryReasons[job.publicId]?.trim() ?? '';
    if (reason.length < 5) { setError('Enter a retry reason of at least five characters.'); return; }
    setBusy(true); setError(null);
    const result = await retryOfficialReportJob(job.publicId, reason, job.rowVersion);
    if (!result.success) setError(result.message ?? 'Report job could not be retried.'); else { pushToast('success', 'Report job queued for retry'); await loadJobs(); }
    setBusy(false);
  };

  const saveTemplate = async () => {
    if (!templateDraft.code.trim() || !templateDraft.name.trim() || !templateDraft.approvalReference.trim() || !templateDraft.reason.trim()) { setError('Template code, name, approval reference and reason are required.'); return; }
    const previous = templates.find(template => template.publicId === templateDraft.previousVersionPublicId);
    const definition = reportTypes.find(item => item.value === templateDraft.reportType)!;
    setBusy(true); setError(null);
    const result = await saveOfficialReportTemplate({
      previousVersionPublicId: previous?.publicId,
      previousVersionRowVersion: previous?.rowVersion,
      municipalityFinancialYearPublicId: yearId || null,
      submissionKind: kind,
      reportType: templateDraft.reportType,
      code: templateDraft.code,
      name: templateDraft.name,
      format: templateDraft.format,
      headingTemplate: templateDraft.headingTemplate,
      columns: definition.columns,
      effectiveFrom: new Date().toISOString(),
      approvalReference: templateDraft.approvalReference,
      reason: templateDraft.reason,
    });
    if (!result.success) setError(result.message ?? 'Official report template could not be saved.');
    else { pushToast('success', previous ? 'Official template version created' : 'Official template created'); setTemplateDraft(value => ({ ...value, approvalReference: '', reason: '', previousVersionPublicId: '' })); await load(); }
    setBusy(false);
  };

  const metrics = summary ? [
    { label: 'Configured targets', value: summary.targetCount, tone: 'primary' as const },
    { label: 'Submissions', value: summary.submissionCount, tone: 'default' as const },
    { label: 'Achieved', value: summary.achievedCount, tone: 'success' as const },
    { label: 'At risk', value: summary.atRiskCount, tone: 'error' as const },
    { label: 'Pending result', value: summary.pendingCount, tone: 'warning' as const },
    { label: 'Average achievement', value: summary.averageAchievementPercent == null ? '—' : `${summary.averageAchievementPercent.toFixed(1)}%`, tone: 'primary' as const },
  ] : [];

  return (
    <AppShell title="Performance Reports" subtitle="Tenant-scoped server analytics">
      <div className="space-y-5">
        <Card className="p-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div className="grid min-w-[20rem] flex-1 gap-3 sm:grid-cols-3">
              <Select label="Performance framework" value={kind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPeriodId(''); setGenerationPage(1); setJobPage(1); setSchedulePage(1); }} />
              <Select label="Financial year" value={yearId} options={years.map(year => ({ value: year.publicId, label: `${year.code}${year.isCurrent ? ' · Current' : ''}` }))} onChange={event => { setYearId(event.target.value); setPeriodId(''); }} />
              <Select label="Reporting period" value={periodId} options={[{ value: '', label: 'All periods' }, ...availablePeriods.map(period => ({ value: period.publicId, label: `${period.code} · ${period.name}` }))]} onChange={event => { setPeriodId(event.target.value); setGenerationPage(1); }} />
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => { void load(); void loadGenerations(); void loadJobs(); void loadSchedules(); }} disabled={busy}>Refresh</Button>
              <Button size="sm" variant="primary" icon={<Download className="h-4 w-4" />} onClick={() => void exportCsv()} disabled={busy || !canExport}>Export CSV</Button>
            </div>
          </div>
        </Card>

        {canReadOfficial && <Card className="p-4">
          <div className="mb-4 flex items-center gap-2"><FileText className="h-5 w-5 text-primary-600" /><div><h2 className="font-semibold text-secondary-900 dark:text-white">Official municipality report</h2><p className="text-xs text-secondary-500">Generate a retained, immutable output from the latest authorised data. Regeneration appends a version.</p></div></div>
          <div className="flex flex-wrap items-end gap-3">
            <div className="min-w-[18rem] flex-1"><Select label="Approved template" value={templateId} options={[{ value: '', label: templates.length ? 'Select a template' : 'No approved template configured' }, ...templates.filter(template => !template.municipalityFinancialYearPublicId || template.municipalityFinancialYearPublicId === yearId).map(template => ({ value: template.publicId, label: `${reportTypeLabel(template.reportType)} · ${template.name} · v${template.versionNumber} · ${['', 'CSV', 'Excel', 'Word', 'PDF'][template.format]}` }))]} onChange={event => { setTemplateId(event.target.value); setDepartmentId(''); setUnitId(''); }} /></div>
            {selectedTemplate?.reportType === 4 && <div className="min-w-[14rem]"><Select label="Department" value={departmentId} options={[{ value: '', label: 'Select a department' }, ...departments.map(department => ({ value: department.publicId, label: department.name }))]} onChange={event => { setDepartmentId(event.target.value); setUnitId(''); }} /></div>}
            {selectedTemplate?.reportType === 5 && <div className="min-w-[14rem]"><Select label="Unit" value={unitId} options={[{ value: '', label: 'Select a unit' }, ...filteredUnits.map(unit => ({ value: unit.publicId, label: unit.name }))]} onChange={event => setUnitId(event.target.value)} /></div>}
            <Button size="sm" variant="outline" icon={<FileText className="h-4 w-4" />} onClick={() => void generate()} disabled={busy || !canGenerateOfficial || !templateId || !yearId || !periodId}>Generate now</Button>
            <Button size="sm" variant="primary" icon={<History className="h-4 w-4" />} onClick={() => void queueGeneration()} disabled={busy || !canGenerateOfficial || !templateId || !yearId || !periodId}>Queue generation</Button>
          </div>
          {!periodId && <p className="mt-2 text-xs text-warning-700">Choose one reporting period before generating an official output.</p>}
        </Card>}

        {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">{error}</div>}

        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
          {metrics.map(metric => <Card key={metric.label} className="p-4"><Badge variant={metric.tone}>{metric.label}</Badge><p className="mt-3 text-2xl font-bold text-secondary-900 dark:text-white">{metric.value}</p></Card>)}
        </div>

        <div className="grid gap-5 xl:grid-cols-[1.3fr_0.7fr]">
          <Card className="p-4">
            <div className="mb-4 flex items-center gap-2"><TrendingUp className="h-5 w-5 text-primary-600" /><h2 className="font-semibold text-secondary-900 dark:text-white">Achievement by department</h2></div>
            <div className="h-80">
              {summary?.departments.length ? <ResponsiveContainer width="100%" height="100%"><BarChart data={summary.departments}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="department" tick={{ fontSize: 10 }} /><YAxis tick={{ fontSize: 10 }} /><Tooltip /><Bar name="Average achievement %" dataKey="averageAchievementPercent" fill="#3b82f6" radius={[4, 4, 0, 0]} /></BarChart></ResponsiveContainer> : <div className="flex h-full items-center justify-center text-sm text-secondary-500">{busy ? 'Generating report…' : 'No scoped performance data for this selection.'}</div>}
            </div>
          </Card>
          <Card className="p-4">
            <div className="mb-4 flex items-center gap-2"><BarChart3 className="h-5 w-5 text-primary-600" /><h2 className="font-semibold text-secondary-900 dark:text-white">Department detail</h2></div>
            <div className="space-y-2">{summary?.departments.map(row => <div key={row.department} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex items-center justify-between"><p className="font-medium text-secondary-900 dark:text-white">{row.department}</p><Badge variant="default">{row.submissionCount} submissions</Badge></div><div className="mt-2 flex items-center justify-between text-xs text-secondary-500"><span>{row.achievedCount} achieved</span><span>{row.averageAchievementPercent == null ? 'No score' : `${row.averageAchievementPercent.toFixed(1)}% average`}</span></div></div>)}</div>
          </Card>
        </div>

        {canReadOfficial && <Card className="p-4">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-2"><div className="flex items-center gap-2"><History className="h-5 w-5 text-primary-600" /><div><h2 className="font-semibold text-secondary-900 dark:text-white">Official generation history</h2><p className="text-xs text-secondary-500">Previous official files remain retrievable after corrections or regeneration.</p></div></div><Badge variant="primary">{generationTotalCount} generations</Badge></div>
          <div className="mb-4 grid gap-2 md:grid-cols-[1fr_11rem_9rem]">
            <input aria-label="Search official generations" placeholder="Template, year, period, file, or generator" className="rounded border border-secondary-300 p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={generationSearchInput} onChange={event => setGenerationSearchInput(event.target.value)} />
            <select aria-label="Sort official generations" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={generationSortBy} onChange={event => { setGenerationSortBy(event.target.value); setGenerationPage(1); }}><option value="generatedAt">Generated date</option><option value="templateName">Template name</option><option value="versionNumber">Version</option><option value="rowCount">Rows</option><option value="financialYear">Financial year</option><option value="reportingPeriod">Reporting period</option></select>
            <select aria-label="Official generation sort direction" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={generationSortDirection} onChange={event => { setGenerationSortDirection(event.target.value as 'asc' | 'desc'); setGenerationPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select>
          </div>
          <div className="overflow-x-auto"><table className="min-w-full text-left text-sm"><thead><tr className="border-b border-secondary-200 text-xs text-secondary-500 dark:border-secondary-700"><th className="px-2 py-2">Template</th><th className="px-2 py-2">Period</th><th className="px-2 py-2">Version</th><th className="px-2 py-2">Generated</th><th className="px-2 py-2">Rows</th><th className="px-2 py-2">Snapshot</th><th className="px-2 py-2">Actions</th></tr></thead><tbody>{generations.map(generation => <tr key={generation.publicId} className="border-b border-secondary-100 dark:border-secondary-800"><td className="px-2 py-3"><strong>{generation.templateName}</strong><div className="text-xs text-secondary-500">{reportTypeLabel(generation.reportType)} · Template v{generation.templateVersion} · {['', 'CSV', 'Excel', 'Word', 'PDF'][generation.format]}</div></td><td className="px-2 py-3">{generation.financialYearCode} · {generation.reportingPeriodCode}</td><td className="px-2 py-3">v{generation.versionNumber}</td><td className="px-2 py-3">{new Date(generation.generatedAt).toLocaleString()}<div className="text-xs text-secondary-500">{generation.generatedBy}</div></td><td className="px-2 py-3">{generation.rowCount}</td><td className="px-2 py-3 font-mono text-xs" title={generation.dataVersionReference}>{generation.dataVersionReference.slice(0, 12)}…</td><td className="px-2 py-3"><div className="flex flex-wrap gap-2"><Button size="sm" variant="outline" onClick={() => void downloadOfficial(generation)} disabled={busy}>Download</Button><Button size="sm" variant="outline" onClick={() => void generate(generation)} disabled={busy || !canGenerateOfficial}>Regenerate now</Button><Button size="sm" variant="outline" onClick={() => void queueGeneration(generation)} disabled={busy || !canGenerateOfficial}>Queue version</Button></div></td></tr>)}</tbody></table></div>
          {!generations.length && <p className="py-5 text-center text-sm text-secondary-500">No official report versions exist for this selection.</p>}
          {generationTotalPages > 1 && <div className="mt-4 flex items-center justify-between text-xs text-secondary-500"><span>Generation page {generationPage} of {generationTotalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || generationPage <= 1} onClick={() => setGenerationPage(value => Math.max(1, value - 1))}>Previous generations</Button><Button size="sm" variant="outline" disabled={busy || generationPage >= generationTotalPages} onClick={() => setGenerationPage(value => value + 1)}>Next generations</Button></div></div>}
        </Card>}

        {canReadOfficial && <Card className="p-4">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-2"><div className="flex items-center gap-2"><History className="h-5 w-5 text-primary-600" /><div><h2 className="font-semibold text-secondary-900 dark:text-white">Asynchronous report jobs</h2><p className="text-xs text-secondary-500">Durable jobs re-evaluate live permissions before generation. Scheduled deliveries continue through the notification receipt ledger.</p></div></div><Badge variant="primary">{jobTotalCount} jobs</Badge></div>
          <div className="mb-4 grid gap-2 md:grid-cols-[1fr_11rem_9rem]">
            <input aria-label="Search report jobs" placeholder="Template, year, period, requester, or error" className="rounded border border-secondary-300 p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={jobSearchInput} onChange={event => setJobSearchInput(event.target.value)} />
            <select aria-label="Sort report jobs" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={jobSortBy} onChange={event => { setJobSortBy(event.target.value); setJobPage(1); }}><option value="requestedAt">Requested date</option><option value="availableAt">Available date</option><option value="state">State</option><option value="templateName">Template name</option><option value="attemptCount">Attempts</option></select>
            <select aria-label="Report job sort direction" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={jobSortDirection} onChange={event => { setJobSortDirection(event.target.value as 'asc' | 'desc'); setJobPage(1); }}><option value="desc">Descending</option><option value="asc">Ascending</option></select>
          </div>
          <div className="space-y-2">{jobs.map(job => <div key={job.publicId} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex flex-wrap items-center justify-between gap-2"><div><p className="font-medium text-secondary-900 dark:text-white">{job.templateName} · {reportTypeLabel(job.reportType)}</p><p className="text-xs text-secondary-500">{job.financialYearCode} · {job.reportingPeriodCode} · requested by {job.requestedBy} · attempt {job.attemptCount}</p></div><Badge variant={job.state === 4 ? 'success' : job.state === 5 ? 'error' : job.state === 3 ? 'warning' : 'default'}>{jobStateLabel(job.state)}</Badge></div>{job.fileName && <p className="mt-2 text-xs text-secondary-600">Generated: {job.fileName}{job.distributionOutboxPublicId ? ' · distribution queued' : ''}</p>}{job.lastError && <p className="mt-2 text-xs text-error-600">{job.lastError}</p>}{job.state === 5 && canConfigureOfficial && <div className="mt-2 flex flex-wrap items-end gap-2"><label className="min-w-[18rem] flex-1 text-xs">Retry reason<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={retryReasons[job.publicId] ?? ''} onChange={event => setRetryReasons(current => ({ ...current, [job.publicId]: event.target.value }))} /></label><Button size="sm" variant="outline" onClick={() => void retryJob(job)} disabled={busy}>Retry</Button></div>}</div>)}</div>
          {!jobs.length && <p className="py-4 text-center text-sm text-secondary-500">No asynchronous report jobs exist.</p>}
          {jobTotalPages > 1 && <div className="mt-4 flex items-center justify-between text-xs text-secondary-500"><span>Page {jobPage} of {jobTotalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || jobPage <= 1} onClick={() => setJobPage(value => Math.max(1, value - 1))}>Previous</Button><Button size="sm" variant="outline" disabled={busy || jobPage >= jobTotalPages} onClick={() => setJobPage(value => value + 1)}>Next</Button></div></div>}
        </Card>}

        {canConfigureOfficial && <Card className="p-4">
          <h2 className="font-semibold text-secondary-900 dark:text-white">Approved template administration</h2>
          <p className="mb-4 text-xs text-secondary-500">Create a template or select a current template to append a successor version. Existing versions are retained.</p>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <label className="text-sm">Version lineage<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.previousVersionPublicId} onChange={event => { const previous = templates.find(item => item.publicId === event.target.value); setTemplateDraft(value => ({ ...value, previousVersionPublicId: event.target.value, reportType: previous?.reportType ?? value.reportType, code: previous?.code ?? value.code, name: previous?.name ?? value.name, format: previous?.format ?? value.format, headingTemplate: previous?.headingTemplate ?? value.headingTemplate })); }}><option value="">New template family</option>{templates.map(template => <option key={template.publicId} value={template.publicId}>{reportTypeLabel(template.reportType)} · {template.code} · v{template.versionNumber}</option>)}</select></label>
            <label className="text-sm">Report class<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.reportType} disabled={Boolean(templateDraft.previousVersionPublicId)} onChange={event => { const definition = reportTypes.find(item => item.value === Number(event.target.value) as OfficialReportType)!; setTemplateDraft(value => ({ ...value, reportType: definition.value, code: definition.code, name: definition.label, headingTemplate: `{Municipality} · {FinancialYear} {Period} · ${definition.label.toUpperCase()}` })); }}>{reportTypes.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
            <label className="text-sm">Template code<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.code} onChange={event => setTemplateDraft(value => ({ ...value, code: event.target.value }))} /></label>
            <label className="text-sm">Template name<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.name} onChange={event => setTemplateDraft(value => ({ ...value, name: event.target.value }))} /></label>
            <label className="text-sm">Output format<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.format} onChange={event => setTemplateDraft(value => ({ ...value, format: Number(event.target.value) as OfficialReportFormat }))}><option value={1}>CSV</option><option value={2}>Excel (.xlsx)</option><option value={3}>Word (.docx)</option><option value={4}>PDF</option></select></label>
            <label className="text-sm md:col-span-2">Heading template<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.headingTemplate} onChange={event => setTemplateDraft(value => ({ ...value, headingTemplate: event.target.value }))} /><span className="text-xs text-secondary-500">Tokens: {'{Municipality}'}, {'{FinancialYear}'}, {'{Period}'}</span></label>
            <label className="text-sm">Approval reference<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.approvalReference} onChange={event => setTemplateDraft(value => ({ ...value, approvalReference: event.target.value }))} /></label>
            <label className="text-sm">Reason<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.reason} onChange={event => setTemplateDraft(value => ({ ...value, reason: event.target.value }))} /></label>
          </div>
          <div className="mt-3 flex justify-end"><Button size="sm" variant="primary" onClick={() => void saveTemplate()} disabled={busy || !yearId}>{templateDraft.previousVersionPublicId ? 'Create template version' : 'Create approved template'}</Button></div>
        </Card>}

        {canConfigureOfficial && <Card className="p-4">
          <div className="mb-1 flex flex-wrap items-center justify-between gap-2"><h2 className="font-semibold text-secondary-900 dark:text-white">Governed report scheduling and distribution</h2><Badge variant="primary">{scheduleTotalCount} schedules</Badge></div>
          <p className="mb-4 text-xs text-secondary-500">Schedules are versioned and bind the selected template, year, period, filters, recipients and approved delivery channels.</p>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <label className="text-sm">Schedule lineage<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.previousVersionPublicId} onChange={event => { const previous = schedules.find(item => item.publicId === event.target.value); setScheduleDraft(value => previous ? ({ ...value, previousVersionPublicId: previous.publicId, code: previous.code, name: previous.name, cadence: previous.cadence, interval: previous.interval, nextRunAt: previous.nextRunAt?.slice(0, 16) ?? '', recipientKind: previous.recipientKind, recipientValues: previous.recipientValues.join(','), channels: previous.channels.join(','), isMandatory: previous.isMandatory, isActive: previous.isActive }) : ({ ...value, previousVersionPublicId: '' })); }}><option value="">New schedule family</option>{schedules.map(schedule => <option key={schedule.publicId} value={schedule.publicId}>{schedule.code} · v{schedule.versionNumber}</option>)}</select></label>
            <label className="text-sm">Schedule code<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.code} onChange={event => setScheduleDraft(value => ({ ...value, code: event.target.value }))} /></label>
            <label className="text-sm">Schedule name<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.name} onChange={event => setScheduleDraft(value => ({ ...value, name: event.target.value }))} /></label>
            <label className="text-sm">Cadence<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.cadence} onChange={event => setScheduleDraft(value => ({ ...value, cadence: Number(event.target.value) as OfficialReportScheduleCadence }))}><option value={1}>Once</option><option value={2}>Daily</option><option value={3}>Weekly</option><option value={4}>Monthly</option></select></label>
            <label className="text-sm">Interval<input type="number" min={1} max={365} className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.interval} onChange={event => setScheduleDraft(value => ({ ...value, interval: Math.max(1, Number(event.target.value)) }))} /></label>
            <label className="text-sm">Next run (local time)<input type="datetime-local" className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.nextRunAt} onChange={event => setScheduleDraft(value => ({ ...value, nextRunAt: event.target.value }))} /></label>
            <label className="text-sm">Recipient source<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.recipientKind} onChange={event => setScheduleDraft(value => ({ ...value, recipientKind: Number(event.target.value) as OfficialReportRecipientKind }))}><option value={1}>Specific user IDs</option><option value={2}>Dynamic role codes</option></select></label>
            <label className="text-sm">{scheduleDraft.recipientKind === 1 ? 'User IDs' : 'Role codes'}<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.recipientValues} onChange={event => setScheduleDraft(value => ({ ...value, recipientValues: event.target.value }))} /><span className="text-xs text-secondary-500">Comma-separated; roles resolve when each job is materialized.</span></label>
            <label className="text-sm">Channels<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.channels} onChange={event => setScheduleDraft(value => ({ ...value, channels: event.target.value }))} /><span className="text-xs text-secondary-500">IN_APP, EMAIL, SMS</span></label>
            <label className="text-sm">Approval reference<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.approvalReference} onChange={event => setScheduleDraft(value => ({ ...value, approvalReference: event.target.value }))} /></label>
            <label className="text-sm md:col-span-2">Audit reason<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={scheduleDraft.reason} onChange={event => setScheduleDraft(value => ({ ...value, reason: event.target.value }))} /></label>
            <div className="flex items-center gap-4 text-sm"><label><input type="checkbox" checked={scheduleDraft.isActive} onChange={event => setScheduleDraft(value => ({ ...value, isActive: event.target.checked }))} /> Active</label><label><input type="checkbox" checked={scheduleDraft.isMandatory} onChange={event => setScheduleDraft(value => ({ ...value, isMandatory: event.target.checked }))} /> Mandatory delivery</label></div>
          </div>
          <div className="mt-3 flex justify-end"><Button size="sm" variant="primary" onClick={() => void saveSchedule()} disabled={busy || !templateId || !yearId || !periodId}>{scheduleDraft.previousVersionPublicId ? 'Create schedule version' : 'Create schedule'}</Button></div>
          <div className="mt-4 grid gap-2 md:grid-cols-[1fr_11rem_9rem]">
            <input aria-label="Search report schedules" placeholder="Code, name, template, period, or approval" className="rounded border border-secondary-300 p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={scheduleSearchInput} onChange={event => setScheduleSearchInput(event.target.value)} />
            <select aria-label="Sort report schedules" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={scheduleSortBy} onChange={event => { setScheduleSortBy(event.target.value); setSchedulePage(1); }}><option value="code">Code</option><option value="name">Name</option><option value="templateName">Template name</option><option value="nextRunAt">Next run</option><option value="createdAt">Created date</option><option value="versionNumber">Version</option></select>
            <select aria-label="Report schedule sort direction" className="rounded border border-secondary-300 bg-white p-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" value={scheduleSortDirection} onChange={event => { setScheduleSortDirection(event.target.value as 'asc' | 'desc'); setSchedulePage(1); }}><option value="asc">Ascending</option><option value="desc">Descending</option></select>
          </div>
          <div className="mt-4 space-y-2">{schedules.map(schedule => <div key={schedule.publicId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-secondary-200 p-3 text-sm dark:border-secondary-700"><div><strong>{schedule.name}</strong><div className="text-xs text-secondary-500">{reportTypeLabel(schedule.reportType)} · {schedule.financialYearCode} {schedule.reportingPeriodCode} · v{schedule.versionNumber} · {schedule.nextRunAt ? new Date(schedule.nextRunAt).toLocaleString() : 'No next run'} · {schedule.channels.join(', ')}</div></div><Button size="sm" variant="outline" onClick={() => void runSchedule(schedule)} disabled={busy}>Run now</Button></div>)}</div>
          {!schedules.length && <p className="py-5 text-center text-sm text-secondary-500">No current report schedules match this search.</p>}
          {scheduleTotalPages > 1 && <div className="mt-4 flex items-center justify-between text-xs text-secondary-500"><span>Schedule page {schedulePage} of {scheduleTotalPages}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={busy || schedulePage <= 1} onClick={() => setSchedulePage(value => Math.max(1, value - 1))}>Previous schedules</Button><Button size="sm" variant="outline" disabled={busy || schedulePage >= scheduleTotalPages} onClick={() => setSchedulePage(value => value + 1)}>Next schedules</Button></div></div>}
        </Card>}

        <div className="flex items-center gap-2 text-xs text-secondary-500"><Target className="h-4 w-4" /><span>Official report classes are generated from tenant-filtered authoritative performance, workflow, assurance, evidence, audit and version datasets{summary ? `; summary refreshed at ${new Date(summary.generatedAt).toLocaleString()}` : ''}.</span></div>
      </div>
    </AppShell>
  );
}
