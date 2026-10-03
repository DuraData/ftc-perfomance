import { useCallback, useEffect, useMemo, useState } from 'react';
import { BarChart3, Download, FileText, History, RefreshCw, Target, TrendingUp } from 'lucide-react';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Select } from '../common/Form';
import { downloadOfficialReport, downloadPerformanceReportCsv, generateOfficialReport, getMunicipalityFinancialYearMasters, getOfficialReportGenerations, getOfficialReportTemplates, getPerformanceReportSummary, getReportingPeriodMasters, saveOfficialReportTemplate } from '../../api/api';
import type { MunicipalityFinancialYearMasterDto, OfficialReportFormat, OfficialReportGenerationDto, OfficialReportTemplateDto, PerformanceReportSummaryDto, ReportingPeriodMasterDto } from '../../types';
import { useApp } from '../../context/AppContext';

export function Reports() {
  const { permissions, pushToast } = useApp();
  const [kind, setKind] = useState<1 | 2>(1);
  const [periodId, setPeriodId] = useState('');
  const [periods, setPeriods] = useState<ReportingPeriodMasterDto[]>([]);
  const [years, setYears] = useState<MunicipalityFinancialYearMasterDto[]>([]);
  const [yearId, setYearId] = useState('');
  const [templates, setTemplates] = useState<OfficialReportTemplateDto[]>([]);
  const [generations, setGenerations] = useState<OfficialReportGenerationDto[]>([]);
  const [templateId, setTemplateId] = useState('');
  const [templateDraft, setTemplateDraft] = useState({ code: 'QUARTERLY', name: 'Quarterly performance report', format: 4 as OfficialReportFormat, headingTemplate: '{Municipality} · {FinancialYear} {Period} PERFORMANCE REPORT', approvalReference: '', reason: '', previousVersionPublicId: '' });
  const [summary, setSummary] = useState<PerformanceReportSummaryDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const permissionSet = useMemo(() => new Set(permissions.map(value => value.toUpperCase())), [permissions]);
  const canExport = permissionSet.has(kind === 1 ? 'OPMS_REPORT.EXPORT' : 'IPMS_REPORT.EXPORT') || permissionSet.has('REPORTS.EXPORT');
  const canReadOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.READ' : 'IPMS_REPORT.READ') || permissionSet.has('REPORTS.VIEW');
  const canGenerateOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.GENERATE' : 'IPMS_REPORT.GENERATE') || permissionSet.has('REPORTS.GENERATE');
  const canConfigureOfficial = permissionSet.has(kind === 1 ? 'OPMS_REPORT.CONFIGURE' : 'IPMS_REPORT.CONFIGURE');
  const availablePeriods = useMemo(() => periods.filter(period => !yearId || period.municipalityFinancialYearPublicId === yearId), [periods, yearId]);

  const load = useCallback(async () => {
    setBusy(true); setError(null);
    const [periodResult, yearResult, summaryResult, templateResult, generationResult] = await Promise.all([
      getReportingPeriodMasters(),
      getMunicipalityFinancialYearMasters(),
      getPerformanceReportSummary(kind, periodId || undefined),
      canReadOfficial ? getOfficialReportTemplates(kind) : Promise.resolve({ success: true, data: [] as OfficialReportTemplateDto[], message: undefined }),
      canReadOfficial ? getOfficialReportGenerations(kind, periodId || undefined) : Promise.resolve({ success: true, data: [] as OfficialReportGenerationDto[], message: undefined }),
    ]);
    setPeriods(periodResult.data ?? []);
    const loadedYears = yearResult.data ?? [];
    setYears(loadedYears);
    setYearId(current => current || loadedYears.find(year => year.isCurrent)?.publicId || loadedYears[0]?.publicId || '');
    setTemplates(templateResult.data ?? []);
    setTemplateId(current => (templateResult.data ?? []).some(template => template.publicId === current) ? current : templateResult.data?.[0]?.publicId ?? '');
    setGenerations(generationResult.data ?? []);
    if (!summaryResult.success || !summaryResult.data) { setSummary(null); setError(summaryResult.message ?? 'Report could not be generated.'); }
    else setSummary(summaryResult.data);
    if (!periodResult.success) setError(periodResult.message ?? 'Reporting periods could not be loaded.');
    else if (!yearResult.success) setError(yearResult.message ?? 'Municipality financial years could not be loaded.');
    else if (!templateResult.success) setError(templateResult.message ?? 'Official report templates could not be loaded.');
    else if (!generationResult.success) setError(generationResult.message ?? 'Official report history could not be loaded.');
    setBusy(false);
  }, [canReadOfficial, kind, periodId]);

  useEffect(() => { void load(); }, [load]);

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
    setBusy(true); setError(null);
    const result = await generateOfficialReport({ templatePublicId: effectiveTemplateId, municipalityFinancialYearPublicId: effectiveYearId, reportingPeriodPublicId: effectivePeriodId, previousGenerationPublicId: previous?.publicId });
    if (!result.success) setError(result.message ?? 'Official report generation failed.');
    else { pushToast('success', previous ? 'New immutable official report version generated' : 'Official report generated'); await load(); }
    setBusy(false);
  };

  const downloadOfficial = async (generation: OfficialReportGenerationDto) => {
    setBusy(true); setError(null);
    const result = await downloadOfficialReport(generation.publicId, generation.fileName);
    if (!result.success) setError(result.message ?? 'Official report download failed.');
    setBusy(false);
  };

  const saveTemplate = async () => {
    if (!templateDraft.code.trim() || !templateDraft.name.trim() || !templateDraft.approvalReference.trim() || !templateDraft.reason.trim()) { setError('Template code, name, approval reference and reason are required.'); return; }
    const previous = templates.find(template => template.publicId === templateDraft.previousVersionPublicId);
    setBusy(true); setError(null);
    const result = await saveOfficialReportTemplate({
      previousVersionPublicId: previous?.publicId,
      previousVersionRowVersion: previous?.rowVersion,
      municipalityFinancialYearPublicId: yearId || null,
      submissionKind: kind,
      code: templateDraft.code,
      name: templateDraft.name,
      format: templateDraft.format,
      headingTemplate: templateDraft.headingTemplate,
      columns: ['indicator', 'targetName', 'department', 'unit', 'period', 'targetValue', 'actualPerformance', 'variance', 'achievementPercent', 'targetAchieved', 'status'],
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
              <Select label="Performance framework" value={kind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => { setKind(Number(event.target.value) as 1 | 2); setPeriodId(''); }} />
              <Select label="Financial year" value={yearId} options={years.map(year => ({ value: year.publicId, label: `${year.code}${year.isCurrent ? ' · Current' : ''}` }))} onChange={event => { setYearId(event.target.value); setPeriodId(''); }} />
              <Select label="Reporting period" value={periodId} options={[{ value: '', label: 'All periods' }, ...availablePeriods.map(period => ({ value: period.publicId, label: `${period.code} · ${period.name}` }))]} onChange={event => setPeriodId(event.target.value)} />
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
              <Button size="sm" variant="primary" icon={<Download className="h-4 w-4" />} onClick={() => void exportCsv()} disabled={busy || !canExport}>Export CSV</Button>
            </div>
          </div>
        </Card>

        {canReadOfficial && <Card className="p-4">
          <div className="mb-4 flex items-center gap-2"><FileText className="h-5 w-5 text-primary-600" /><div><h2 className="font-semibold text-secondary-900 dark:text-white">Official municipality report</h2><p className="text-xs text-secondary-500">Generate a retained, immutable output from the latest authorised data. Regeneration appends a version.</p></div></div>
          <div className="flex flex-wrap items-end gap-3">
            <div className="min-w-[18rem] flex-1"><Select label="Approved template" value={templateId} options={[{ value: '', label: templates.length ? 'Select a template' : 'No approved template configured' }, ...templates.filter(template => !template.municipalityFinancialYearPublicId || template.municipalityFinancialYearPublicId === yearId).map(template => ({ value: template.publicId, label: `${template.name} · v${template.versionNumber} · ${['', 'CSV', 'Excel', 'Word', 'PDF'][template.format]}` }))]} onChange={event => setTemplateId(event.target.value)} /></div>
            <Button size="sm" variant="primary" icon={<FileText className="h-4 w-4" />} onClick={() => void generate()} disabled={busy || !canGenerateOfficial || !templateId || !yearId || !periodId}>Generate official version</Button>
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
          <div className="mb-4 flex items-center gap-2"><History className="h-5 w-5 text-primary-600" /><div><h2 className="font-semibold text-secondary-900 dark:text-white">Official generation history</h2><p className="text-xs text-secondary-500">Previous official files remain retrievable after corrections or regeneration.</p></div></div>
          <div className="overflow-x-auto"><table className="min-w-full text-left text-sm"><thead><tr className="border-b border-secondary-200 text-xs text-secondary-500 dark:border-secondary-700"><th className="px-2 py-2">Template</th><th className="px-2 py-2">Period</th><th className="px-2 py-2">Version</th><th className="px-2 py-2">Generated</th><th className="px-2 py-2">Rows</th><th className="px-2 py-2">Snapshot</th><th className="px-2 py-2">Actions</th></tr></thead><tbody>{generations.map(generation => <tr key={generation.publicId} className="border-b border-secondary-100 dark:border-secondary-800"><td className="px-2 py-3"><strong>{generation.templateName}</strong><div className="text-xs text-secondary-500">Template v{generation.templateVersion} · {['', 'CSV', 'Excel', 'Word', 'PDF'][generation.format]}</div></td><td className="px-2 py-3">{generation.financialYearCode} · {generation.reportingPeriodCode}</td><td className="px-2 py-3">v{generation.versionNumber}</td><td className="px-2 py-3">{new Date(generation.generatedAt).toLocaleString()}<div className="text-xs text-secondary-500">{generation.generatedBy}</div></td><td className="px-2 py-3">{generation.rowCount}</td><td className="px-2 py-3 font-mono text-xs" title={generation.dataVersionReference}>{generation.dataVersionReference.slice(0, 12)}…</td><td className="px-2 py-3"><div className="flex gap-2"><Button size="sm" variant="outline" onClick={() => void downloadOfficial(generation)} disabled={busy}>Download</Button><Button size="sm" variant="outline" onClick={() => void generate(generation)} disabled={busy || !canGenerateOfficial}>Regenerate</Button></div></td></tr>)}</tbody></table></div>
          {!generations.length && <p className="py-5 text-center text-sm text-secondary-500">No official report versions exist for this selection.</p>}
        </Card>}

        {canConfigureOfficial && <Card className="p-4">
          <h2 className="font-semibold text-secondary-900 dark:text-white">Approved template administration</h2>
          <p className="mb-4 text-xs text-secondary-500">Create a template or select a current template to append a successor version. Existing versions are retained.</p>
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <label className="text-sm">Version lineage<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.previousVersionPublicId} onChange={event => { const previous = templates.find(item => item.publicId === event.target.value); setTemplateDraft(value => ({ ...value, previousVersionPublicId: event.target.value, code: previous?.code ?? value.code, name: previous?.name ?? value.name, format: previous?.format ?? value.format, headingTemplate: previous?.headingTemplate ?? value.headingTemplate })); }}><option value="">New template family</option>{templates.map(template => <option key={template.publicId} value={template.publicId}>{template.code} · v{template.versionNumber}</option>)}</select></label>
            <label className="text-sm">Template code<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.code} onChange={event => setTemplateDraft(value => ({ ...value, code: event.target.value }))} /></label>
            <label className="text-sm">Template name<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.name} onChange={event => setTemplateDraft(value => ({ ...value, name: event.target.value }))} /></label>
            <label className="text-sm">Output format<select className="mt-1 w-full rounded border border-secondary-300 bg-white p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.format} onChange={event => setTemplateDraft(value => ({ ...value, format: Number(event.target.value) as OfficialReportFormat }))}><option value={1}>CSV</option><option value={2}>Excel (.xlsx)</option><option value={3}>Word (.docx)</option><option value={4}>PDF</option></select></label>
            <label className="text-sm md:col-span-2">Heading template<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.headingTemplate} onChange={event => setTemplateDraft(value => ({ ...value, headingTemplate: event.target.value }))} /><span className="text-xs text-secondary-500">Tokens: {'{Municipality}'}, {'{FinancialYear}'}, {'{Period}'}</span></label>
            <label className="text-sm">Approval reference<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.approvalReference} onChange={event => setTemplateDraft(value => ({ ...value, approvalReference: event.target.value }))} /></label>
            <label className="text-sm">Reason<input className="mt-1 w-full rounded border border-secondary-300 p-2 dark:border-secondary-700 dark:bg-secondary-900" value={templateDraft.reason} onChange={event => setTemplateDraft(value => ({ ...value, reason: event.target.value }))} /></label>
          </div>
          <div className="mt-3 flex justify-end"><Button size="sm" variant="primary" onClick={() => void saveTemplate()} disabled={busy || !yearId}>{templateDraft.previousVersionPublicId ? 'Create template version' : 'Create approved template'}</Button></div>
        </Card>}

        <div className="flex items-center gap-2 text-xs text-secondary-500"><Target className="h-4 w-4" /><span>Generated from tenant-filtered canonical targets and submissions{summary ? ` at ${new Date(summary.generatedAt).toLocaleString()}` : ''}.</span></div>
      </div>
    </AppShell>
  );
}
