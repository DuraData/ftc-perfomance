import { useEffect, useState } from 'react';
import { Clock3, Plus, RefreshCw, ShieldCheck, Star } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormPanel, Input, Select, Textarea } from '../common/Form';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import {
  createReportingWindowException,
  createRatingScheme,
  createReportingWindow,
  createWorkflowDefinition,
  compareWorkflowDefinitions,
  getDepartments,
  getRatingSchemes,
  getReportingWindowExceptions,
  getReportingWindows,
  getUnits,
  getUsersPage,
  getWorkflowDefinitions,
  getInternalAuditConfigurations,
  retireWorkflowDefinition,
  saveInternalAuditConfiguration,
} from '../../api/api';
import type { AdminUserDetail, DepartmentLookupDto, InternalAuditConfigurationDto, RatingSchemeDto, ReportingWindowDto, ReportingWindowExceptionDto, UnitLookupDto, WorkflowDefinitionComparisonDto, WorkflowDefinitionDto } from '../../types';
import { useApp } from '../../context/AppContext';
import { NotificationDeliveryOperations } from './NotificationDeliveryOperations';
import { NotificationPolicyAdministration } from './NotificationPolicyAdministration';
import { TargetNormalizationAdministration } from './TargetNormalizationAdministration';

type Tab = 'definitions' | 'windows' | 'ratings' | 'audit' | 'notifications' | 'delivery' | 'cutover';
type StageDraft = {
  code: string;
  name: string;
  requiredActionCode: string;
  requiredPermissionCode: string;
  isOptional: boolean;
  allowBypass: boolean;
  requireDifferentActorFromSubmitter: boolean;
  requireDifferentActorFromPreviousStage: boolean;
  rejectionStageCode: string;
  requiresRating: boolean;
  ratingSchemePublicId: string;
};

const localDate = (date = new Date()) => new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
const kindName = (kind: number) => kind === 1 ? 'OPMS' : 'IPMS';
const workflowStatus = (item: WorkflowDefinitionDto) => !item.isActive ? 'Retired' : item.effectiveTo && new Date(item.effectiveTo) <= new Date() ? 'Superseded' : item.effectiveFrom && new Date(item.effectiveFrom) > new Date() ? 'Scheduled' : 'Effective';
const emptyStage = (index: number): StageDraft => ({
  code: index === 0 ? 'SUBMIT' : 'VERIFY',
  name: index === 0 ? 'Submit' : 'Verify',
  requiredActionCode: index === 0 ? 'OPMS_SUBMISSION.SUBMIT' : 'OPMS_SUBMISSION.VERIFY',
  requiredPermissionCode: index === 0 ? 'OPMS_SUBMISSION.SUBMIT' : 'OPMS_SUBMISSION.VERIFY',
  isOptional: false,
  allowBypass: false,
  requireDifferentActorFromSubmitter: index > 0,
  requireDifferentActorFromPreviousStage: false,
  rejectionStageCode: index > 0 ? 'SUBMIT' : '',
  requiresRating: false,
  ratingSchemePublicId: '',
});

export function WorkflowGovernanceAdminPage() {
  const { pushToast } = useApp();
  const [tab, setTab] = useState<Tab>('definitions');
  const [definitions, setDefinitions] = useState<WorkflowDefinitionDto[]>([]);
  const [windows, setWindows] = useState<ReportingWindowDto[]>([]);
  const [ratings, setRatings] = useState<RatingSchemeDto[]>([]);
  const [auditConfigurations, setAuditConfigurations] = useState<InternalAuditConfigurationDto[]>([]);
  const [exceptionWindow, setExceptionWindow] = useState<ReportingWindowDto | null>(null);
  const [exceptions, setExceptions] = useState<ReportingWindowExceptionDto[]>([]);
  const [users, setUsers] = useState<AdminUserDetail[]>([]);
  const [departments, setDepartments] = useState<DepartmentLookupDto[]>([]);
  const [units, setUnits] = useState<UnitLookupDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [comparison, setComparison] = useState<WorkflowDefinitionComparisonDto | null>(null);
  const [retireReasons, setRetireReasons] = useState<Record<string, string>>({});

  const [definition, setDefinition] = useState({ code: 'DEFAULT', name: 'Default performance workflow', submissionKind: 1, effectiveFrom: localDate(), reason: '' });
  const [workflowYearId, setWorkflowYearId] = useState('');
  const [stages, setStages] = useState<StageDraft[]>([emptyStage(0), emptyStage(1)]);
  const [windowDraft, setWindowDraft] = useState({ reportingPeriodPublicId: '', submissionKind: 1, opensAt: localDate(), closesAt: localDate(new Date(Date.now() + 7 * 86400000)) });
  const [exceptionDraft, setExceptionDraft] = useState({ scopeType: 'department', scopePublicId: '', extendedClosesAt: localDate(new Date(Date.now() + 8 * 86400000)), reason: '' });
  const [rating, setRating] = useState({ code: '', name: '' });
  const [ratingValues, setRatingValues] = useState([{ value: '1', label: 'Not achieved', minimum: '0', maximum: '49.99' }, { value: '2', label: 'Achieved', minimum: '50', maximum: '100' }]);
  const [auditDraft, setAuditDraft] = useState({ municipalityFinancialYearPublicId: '', model: 1 as 1 | 2, effectiveFrom: localDate(), reason: '' });

  const load = async () => {
    setBusy(true);
    setError(null);
    const [definitionResult, windowResult, ratingResult, auditResult] = await Promise.all([
      getWorkflowDefinitions(), getReportingWindows(), getRatingSchemes(), getInternalAuditConfigurations(),
    ]);
    const failed = [definitionResult, windowResult, ratingResult, auditResult].find(result => !result.success);
    if (failed) setError(failed.message ?? 'Workflow configuration could not be loaded.');
    setDefinitions(definitionResult.data ?? []);
    setWindows(windowResult.data ?? []);
    setRatings(ratingResult.data ?? []);
    setAuditConfigurations(auditResult.data ?? []);
    setBusy(false);
  };

  useEffect(() => { void load(); }, []);

  const saveDefinition = async () => {
    if (!workflowYearId) { setError('Select a municipality financial year before defining a workflow.'); return; }
    setBusy(true); setError(null);
    const prefix = definition.submissionKind === 1 ? 'OPMS' : 'IPMS';
    const normalizedStages = stages.map((stage, index) => ({
      ...stage,
      sequence: index + 1,
      requiredActionCode: stage.requiredActionCode.replace(/^OPMS|^IPMS/, prefix),
      requiredPermissionCode: stage.requiredPermissionCode.replace(/^OPMS|^IPMS/, prefix),
      rejectionStageCode: stage.rejectionStageCode || null,
      ratingSchemePublicId: stage.ratingSchemePublicId || null,
      isTerminal: index === stages.length - 1,
    }));
    const result = await createWorkflowDefinition({
      municipalityFinancialYearPublicId: workflowYearId,
      submissionKind: definition.submissionKind,
      code: definition.code,
      name: definition.name,
      isActive: true,
      effectiveFrom: new Date(definition.effectiveFrom).toISOString(),
      reason: definition.reason,
      stages: normalizedStages,
    });
    if (!result.success) setError(result.message ?? 'Workflow definition could not be saved.');
    else { pushToast('success', 'Workflow definition version created'); await load(); }
    setBusy(false);
  };

  const compareWithPrevious = async (current: WorkflowDefinitionDto) => {
    const previous = definitions
      .filter(item => item.code === current.code && item.submissionKind === current.submissionKind && item.municipalityFinancialYearPublicId === current.municipalityFinancialYearPublicId && item.version < current.version)
      .sort((left, right) => right.version - left.version)[0];
    if (!previous) { setError('No earlier version exists in this workflow lineage.'); return; }
    setBusy(true); setError(null);
    const result = await compareWorkflowDefinitions(previous.publicId, current.publicId);
    if (!result.success || !result.data) setError(result.message ?? 'Workflow versions could not be compared.');
    else setComparison(result.data);
    setBusy(false);
  };

  const retireDefinition = async (target: WorkflowDefinitionDto) => {
    const reason = (retireReasons[target.publicId] ?? '').trim();
    if (reason.length < 10) { setError('Enter a retirement reason of at least 10 characters.'); return; }
    setBusy(true); setError(null);
    const result = await retireWorkflowDefinition(target.publicId, { reason, effectiveTo: new Date().toISOString(), rowVersion: target.rowVersion });
    if (!result.success) setError(result.message ?? 'Workflow definition could not be retired.');
    else {
      pushToast('success', 'Workflow version retired; existing instances remain pinned');
      setRetireReasons(current => ({ ...current, [target.publicId]: '' })); setComparison(null); await load();
    }
    setBusy(false);
  };

  const saveWindow = async () => {
    if (!windowDraft.reportingPeriodPublicId) { setError('Select a reporting period.'); return; }
    setBusy(true); setError(null);
    const result = await createReportingWindow({ ...windowDraft, opensAt: new Date(windowDraft.opensAt).toISOString(), closesAt: new Date(windowDraft.closesAt).toISOString() });
    if (!result.success) setError(result.message ?? 'Reporting window could not be saved.');
    else { pushToast('success', 'Reporting window created'); await load(); }
    setBusy(false);
  };

  const openExceptions = async (window: ReportingWindowDto) => {
    setBusy(true); setError(null); setExceptionWindow(window);
    setExceptionDraft({ scopeType: 'department', scopePublicId: '', extendedClosesAt: localDate(new Date(new Date(window.closesAt).getTime() + 86400000)), reason: '' });
    const [exceptionResult, userResult, departmentResult, unitResult] = await Promise.all([
      getReportingWindowExceptions(window.publicId), getUsersPage({ pageSize: 100, sortBy: 'name', sortDirection: 'asc' }), getDepartments(), getUnits(),
    ]);
    const failed = [exceptionResult, userResult, departmentResult, unitResult].find(result => !result.success);
    if (failed) setError(failed.message ?? 'Window exception data could not be loaded.');
    setExceptions(exceptionResult.data ?? []); setUsers(userResult.data?.items ?? []); setDepartments(departmentResult.data ?? []); setUnits(unitResult.data ?? []);
    setBusy(false);
  };

  const exceptionOptions = exceptionDraft.scopeType === 'user'
    ? users.filter(item => item.user.isActive).map(item => ({ value: item.user.publicId, label: item.user.fullName }))
    : exceptionDraft.scopeType === 'unit'
      ? units.map(item => ({ value: item.publicId, label: `${item.departmentName} · ${item.name}` }))
      : departments.map(item => ({ value: item.publicId, label: `${item.code} · ${item.name}` }));

  const saveException = async () => {
    if (!exceptionWindow || !exceptionDraft.scopePublicId || !exceptionDraft.reason.trim()) { setError('Scope and reason are required.'); return; }
    setBusy(true); setError(null);
    const payload = {
      extendedClosesAt: new Date(exceptionDraft.extendedClosesAt).toISOString(), reason: exceptionDraft.reason.trim(),
      ...(exceptionDraft.scopeType === 'user' ? { userPublicId: exceptionDraft.scopePublicId } : {}),
      ...(exceptionDraft.scopeType === 'department' ? { departmentPublicId: exceptionDraft.scopePublicId } : {}),
      ...(exceptionDraft.scopeType === 'unit' ? { unitPublicId: exceptionDraft.scopePublicId } : {}),
    };
    const result = await createReportingWindowException(exceptionWindow.publicId, payload);
    if (!result.success) setError(result.message ?? 'Window exception could not be saved.');
    else {
      pushToast('success', 'Scoped reporting-window exception approved');
      setExceptionDraft(current => ({ ...current, scopePublicId: '', reason: '' }));
      const refreshed = await getReportingWindowExceptions(exceptionWindow.publicId);
      setExceptions(refreshed.data ?? []);
    }
    setBusy(false);
  };

  const saveRating = async () => {
    setBusy(true); setError(null);
    const result = await createRatingScheme({
      code: rating.code,
      name: rating.name,
      values: ratingValues.map((value, index) => ({ value: Number(value.value), label: value.label, minimumAchievementPercent: Number(value.minimum), maximumAchievementPercent: Number(value.maximum), sortOrder: index + 1 })),
    });
    if (!result.success) setError(result.message ?? 'Rating scheme could not be saved.');
    else { pushToast('success', 'Rating scheme created'); setRating({ code: '', name: '' }); await load(); }
    setBusy(false);
  };

  const saveAuditModel = async () => {
    const yearId = auditDraft.municipalityFinancialYearPublicId;
    if (!yearId) { setError('Create a municipality financial year before selecting an Internal Audit model.'); return; }
    const current = auditConfigurations.find(item => item.municipalityFinancialYearPublicId === yearId && item.isCurrent);
    setBusy(true); setError(null);
    const result = await saveInternalAuditConfiguration({
      municipalityFinancialYearPublicId: yearId,
      model: auditDraft.model,
      effectiveFrom: new Date(auditDraft.effectiveFrom).toISOString(),
      reason: auditDraft.reason.trim(),
      currentRowVersion: current?.rowVersion,
    });
    if (!result.success) setError(result.message ?? 'Internal Audit model could not be selected.');
    else { pushToast('success', 'Internal Audit model version selected'); setAuditDraft(value => ({ ...value, municipalityFinancialYearPublicId: yearId, reason: '', effectiveFrom: localDate() })); await load(); }
    setBusy(false);
  };

  return (
    <AppShell title="Workflow Governance" subtitle="Tenant and financial-year configuration">
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex gap-2">
            {(['definitions', 'windows', 'ratings', 'audit', 'notifications', 'delivery', 'cutover'] as Tab[]).map(value => <Button key={value} size="sm" variant={tab === value ? 'primary' : 'outline'} onClick={() => setTab(value)}>{value === 'audit' ? 'Internal Audit' : value === 'cutover' ? 'Data Cutover' : value[0].toUpperCase() + value.slice(1)}</Button>)}
          </div>
          <Button size="sm" variant="ghost" icon={<RefreshCw className="h-4 w-4" />} onClick={() => void load()} disabled={busy}>Refresh</Button>
        </div>

        {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700 dark:border-error-800 dark:bg-error-900/20 dark:text-error-300">{error}</div>}

        {tab === 'definitions' && <div className="grid gap-5 xl:grid-cols-[1.15fr_0.85fr]">
          <FormPanel title="New workflow version" description="Stages are ordered and the final stage is terminal." icon={<ShieldCheck className="h-5 w-5" />}>
            <div className="grid gap-3 sm:grid-cols-2">
              <Input label="Code" value={definition.code} onChange={event => setDefinition(current => ({ ...current, code: event.target.value }))} required />
              <Input label="Name" value={definition.name} onChange={event => setDefinition(current => ({ ...current, name: event.target.value }))} required />
              <CalendarMasterPicker kind="municipality-financial-year" label="Municipality financial year" value={workflowYearId} onChange={setWorkflowYearId} required />
              <Select label="Submission type" value={definition.submissionKind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => setDefinition(current => ({ ...current, submissionKind: Number(event.target.value) }))} />
              <Input label="Effective from" type="datetime-local" value={definition.effectiveFrom} onChange={event => setDefinition(current => ({ ...current, effectiveFrom: event.target.value }))} />
              <Textarea label="Version reason" value={definition.reason} onChange={event => setDefinition(current => ({ ...current, reason: event.target.value }))} required />
            </div>
            <div className="space-y-3">
              {stages.map((stage, index) => <div key={index} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700">
                <div className="mb-2 flex items-center justify-between"><Badge variant={index === stages.length - 1 ? 'success' : 'default'}>Stage {index + 1}{index === stages.length - 1 ? ' · terminal' : ''}</Badge>{stages.length > 2 && <button className="text-xs text-error-600" onClick={() => setStages(current => current.filter((_, stageIndex) => stageIndex !== index))}>Remove</button>}</div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <Input label="Stage code" value={stage.code} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, code: event.target.value.toUpperCase() } : item))} />
                  <Input label="Name" value={stage.name} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, name: event.target.value } : item))} />
                  <Input label="Registered action / permission" value={stage.requiredActionCode} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, requiredActionCode: event.target.value.toUpperCase(), requiredPermissionCode: event.target.value.toUpperCase() } : item))} />
                  <Select label="Rejection stage" value={stage.rejectionStageCode} options={stages.slice(0, index).map(item => ({ value: item.code, label: item.name }))} placeholder="No rejection route" onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, rejectionStageCode: event.target.value } : item))} />
                  <Select label="Rating scheme" value={stage.ratingSchemePublicId} options={ratings.filter(item => item.isActive).map(item => ({ value: item.publicId, label: `${item.code} · ${item.name}` }))} placeholder="No rating at this stage" onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, ratingSchemePublicId: event.target.value, requiresRating: event.target.value ? item.requiresRating : false } : item))} />
                </div>
                <div className="mt-2 grid gap-2 sm:grid-cols-3"><Checkbox label="Different from submitter" checked={stage.requireDifferentActorFromSubmitter} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, requireDifferentActorFromSubmitter: event.target.checked } : item))} /><Checkbox label="Optional and bypassable" checked={stage.isOptional && stage.allowBypass} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, isOptional: event.target.checked, allowBypass: event.target.checked } : item))} /><Checkbox label="Rating required" checked={stage.requiresRating} disabled={!stage.ratingSchemePublicId} onChange={event => setStages(current => current.map((item, stageIndex) => stageIndex === index ? { ...item, requiresRating: event.target.checked } : item))} /></div>
              </div>)}
            </div>
            <div className="flex justify-between"><Button size="sm" variant="outline" icon={<Plus className="h-4 w-4" />} onClick={() => setStages(current => [...current, { ...emptyStage(current.length), code: `STAGE_${current.length + 1}`, name: `Stage ${current.length + 1}` }])}>Add stage</Button><Button size="sm" variant="primary" onClick={() => void saveDefinition()} disabled={busy}>Create version</Button></div>
          </FormPanel>
          <Card className="p-4"><h3 className="font-semibold text-secondary-900 dark:text-white">Version history</h3><div className="mt-3 space-y-3">{definitions.map(item => { const status = workflowStatus(item); return <div key={item.publicId} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex justify-between gap-3"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code} · v{item.version} · {kindName(item.submissionKind)}</p></div><Badge variant={status === 'Effective' ? 'success' : status === 'Scheduled' ? 'info' : 'default'}>{status}</Badge></div><div className="mt-2 flex flex-wrap gap-1">{item.stages.map(stage => <Badge key={stage.publicId} variant="default">{stage.sequence}. {stage.name}{stage.ratingSchemeCode ? ` · ${stage.ratingSchemeCode}${stage.requiresRating ? ' required' : ''}` : ''}</Badge>)}</div><div className="mt-3 flex flex-wrap gap-2"><Button type="button" size="sm" variant="outline" onClick={() => void compareWithPrevious(item)} disabled={busy || item.version <= 1}>Compare to previous</Button></div>{item.isActive && status !== 'Superseded' && <div className="mt-3 space-y-2 border-t border-secondary-200 pt-3 dark:border-secondary-700"><Textarea label={`Retirement reason for ${item.code} v${item.version}`} value={retireReasons[item.publicId] ?? ''} onChange={event => setRetireReasons(current => ({ ...current, [item.publicId]: event.target.value }))} required /><Button type="button" size="sm" variant="outline" onClick={() => void retireDefinition(item)} disabled={busy}>Retire version</Button><p className="text-xs text-secondary-500">Existing instances remain pinned to this version.</p></div>}</div>; })}{!definitions.length && <p className="text-sm text-secondary-500">No configured definitions.</p>}</div></Card>
          {comparison && <Card className="p-4 xl:col-span-2"><div className="flex items-center justify-between"><div><h3 className="font-semibold text-secondary-900 dark:text-white">Version comparison</h3><p className="text-sm text-secondary-500">{comparison.from.code} v{comparison.from.version} → v{comparison.to.version}</p></div><Button size="sm" variant="ghost" onClick={() => setComparison(null)}>Close comparison</Button></div><div className="mt-3 grid gap-2 md:grid-cols-2">{comparison.stageDifferences.map(change => <div key={change.stageCode} className="rounded-lg border border-secondary-200 p-3 text-sm dark:border-secondary-700"><div className="flex justify-between"><span className="font-medium">{change.stageCode}</span><Badge variant={change.change === 'Unchanged' ? 'default' : 'warning'}>{change.change}</Badge></div><p className="mt-1 text-xs text-secondary-500">Sequence {change.fromSequence ?? '—'} → {change.toSequence ?? '—'}</p>{change.changedFields.length > 0 && <p className="mt-1 text-xs text-secondary-500">Changed: {change.changedFields.join(', ')}</p>}</div>)}</div></Card>}
        </div>}

        {tab === 'windows' && <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
          <FormPanel title="Open a reporting window" description="Submission is rejected outside this server-enforced interval." icon={<Clock3 className="h-5 w-5" />}>
            <CalendarMasterPicker kind="reporting-period" label="Reporting period" value={windowDraft.reportingPeriodPublicId} onChange={value => setWindowDraft(current => ({ ...current, reportingPeriodPublicId: value }))} required />
            <Select label="Submission type" value={windowDraft.submissionKind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => setWindowDraft(current => ({ ...current, submissionKind: Number(event.target.value) }))} />
            <Input label="Opens" type="datetime-local" value={windowDraft.opensAt} onChange={event => setWindowDraft(current => ({ ...current, opensAt: event.target.value }))} />
            <Input label="Closes" type="datetime-local" value={windowDraft.closesAt} onChange={event => setWindowDraft(current => ({ ...current, closesAt: event.target.value }))} />
            <Button variant="primary" onClick={() => void saveWindow()} disabled={busy}>Create window</Button>
          </FormPanel>
          <Card className="p-4"><h3 className="font-semibold text-secondary-900 dark:text-white">Configured windows</h3><div className="mt-3 space-y-2">{windows.map(item => <div key={item.publicId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div><p className="font-medium">{item.periodCode} · {kindName(item.submissionKind)}</p><p className="text-xs text-secondary-500">{new Date(item.opensAt).toLocaleString()} — {new Date(item.closesAt).toLocaleString()}</p></div><div className="flex items-center gap-2"><Badge variant={item.isActive ? 'success' : 'default'}>{item.isActive ? 'Active' : 'Inactive'}</Badge><Button size="sm" variant="outline" onClick={() => void openExceptions(item)}>Manage exceptions</Button></div></div>)}</div></Card>
          {exceptionWindow && <div className="xl:col-span-2"><FormPanel title={`Scoped exceptions · ${exceptionWindow.periodCode} ${kindName(exceptionWindow.submissionKind)}`} description="An approved exception extends only one user, department, or unit beyond the normal close time." icon={<Clock3 className="h-5 w-5" />}>
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4"><Select label="Scope type" value={exceptionDraft.scopeType} options={[{ value: 'department', label: 'Department' }, { value: 'unit', label: 'Unit' }, { value: 'user', label: 'User' }]} onChange={event => setExceptionDraft(current => ({ ...current, scopeType: event.target.value, scopePublicId: '' }))} /><Select label="Scoped record" value={exceptionDraft.scopePublicId} placeholder="Select one scope" options={exceptionOptions} onChange={event => setExceptionDraft(current => ({ ...current, scopePublicId: event.target.value }))} /><Input label="Extended close" type="datetime-local" value={exceptionDraft.extendedClosesAt} onChange={event => setExceptionDraft(current => ({ ...current, extendedClosesAt: event.target.value }))} /><Textarea label="Approval reason" value={exceptionDraft.reason} onChange={event => setExceptionDraft(current => ({ ...current, reason: event.target.value }))} required /></div>
            <div className="flex justify-end"><Button variant="primary" onClick={() => void saveException()} disabled={busy}>Approve exception</Button></div>
            <div className="space-y-2">{exceptions.map(item => <div key={item.publicId} className="rounded-lg border border-secondary-200 p-3 text-sm dark:border-secondary-700"><div className="flex flex-wrap justify-between gap-2"><span>{item.userId ? `User ${item.userId}` : item.departmentId ? `Department #${item.departmentId}` : `Unit #${item.unitId}`}</span><Badge variant="default">until {new Date(item.extendedClosesAt).toLocaleString()}</Badge></div><p className="mt-1 text-xs text-secondary-500">{item.reason}</p></div>)}{!exceptions.length && <p className="text-sm text-secondary-500">No scoped exceptions for this window.</p>}</div>
          </FormPanel></div>}
        </div>}

        {tab === 'ratings' && <div className="grid gap-5 xl:grid-cols-[1fr_1fr]">
          <FormPanel title="Create rating scheme" description="Achievement ranges must be complete and non-overlapping." icon={<Star className="h-5 w-5" />}>
            <div className="grid gap-3 sm:grid-cols-2"><Input label="Code" value={rating.code} onChange={event => setRating(current => ({ ...current, code: event.target.value.toUpperCase() }))} /><Input label="Name" value={rating.name} onChange={event => setRating(current => ({ ...current, name: event.target.value }))} /></div>
            {ratingValues.map((value, index) => <div key={index} className="grid gap-2 rounded-xl border border-secondary-200 p-3 sm:grid-cols-4 dark:border-secondary-700"><Input label="Value" type="number" value={value.value} onChange={event => setRatingValues(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, value: event.target.value } : item))} /><Input label="Label" value={value.label} onChange={event => setRatingValues(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, label: event.target.value } : item))} /><Input label="Minimum %" type="number" value={value.minimum} onChange={event => setRatingValues(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, minimum: event.target.value } : item))} /><Input label="Maximum %" type="number" value={value.maximum} onChange={event => setRatingValues(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, maximum: event.target.value } : item))} /></div>)}
            <div className="flex justify-between"><Button size="sm" variant="outline" icon={<Plus className="h-4 w-4" />} onClick={() => setRatingValues(current => [...current, { value: String(current.length + 1), label: '', minimum: '', maximum: '' }])}>Add value</Button><Button size="sm" variant="primary" onClick={() => void saveRating()} disabled={busy}>Create scheme</Button></div>
          </FormPanel>
          <Card className="p-4"><h3 className="font-semibold text-secondary-900 dark:text-white">Rating schemes</h3><div className="mt-3 space-y-3">{ratings.map(item => <div key={item.publicId} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex justify-between"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code}</p></div><Badge variant={item.isActive ? 'success' : 'default'}>{item.values.length} values</Badge></div><div className="mt-2 flex flex-wrap gap-1">{item.values.map(value => <Badge key={value.publicId} variant="default">{value.value}: {value.label}</Badge>)}</div></div>)}</div></Card>
        </div>}

        {tab === 'audit' && <div className="grid gap-5 xl:grid-cols-[0.9fr_1.1fr]">
          <FormPanel title="Select Internal Audit model" description="The selection is tenant and financial-year specific. Replacing it creates an audited version." icon={<ShieldCheck className="h-5 w-5" />}>
            <CalendarMasterPicker kind="municipality-financial-year" label="Municipality financial year" value={auditDraft.municipalityFinancialYearPublicId} onChange={value => setAuditDraft(current => ({ ...current, municipalityFinancialYearPublicId: value }))} required />
            <Select label="Assessment model" value={auditDraft.model} options={[{ value: 1, label: 'Detailed IA Assessment' }, { value: 2, label: 'Satisfactory / Not Satisfactory' }]} onChange={event => setAuditDraft(value => ({ ...value, model: Number(event.target.value) as 1 | 2 }))} />
            <Input label="Effective from" type="datetime-local" value={auditDraft.effectiveFrom} onChange={event => setAuditDraft(value => ({ ...value, effectiveFrom: event.target.value }))} />
            <Textarea label="Governance reason" value={auditDraft.reason} maxLength={1000} onChange={event => setAuditDraft(value => ({ ...value, reason: event.target.value }))} required />
            <Button variant="primary" onClick={() => void saveAuditModel()} disabled={busy}>Create model version</Button>
          </FormPanel>
          <Card className="p-4"><h3 className="font-semibold text-secondary-900 dark:text-white">Model selection history</h3><div className="mt-3 space-y-3">{auditConfigurations.map(item => <div key={item.publicId} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex flex-wrap justify-between gap-2"><div><p className="font-medium">{item.model === 1 ? 'Detailed IA Assessment' : 'Satisfactory / Not Satisfactory'}</p><p className="text-xs text-secondary-500">{item.financialYearCode} · version {item.version}</p></div><Badge variant={item.isCurrent ? 'success' : 'default'}>{item.isCurrent ? 'Current' : 'Superseded'}</Badge></div><p className="mt-2 text-xs text-secondary-500">Effective {new Date(item.effectiveFrom).toLocaleString()}{item.effectiveTo ? ` — ${new Date(item.effectiveTo).toLocaleString()}` : ''}</p><p className="mt-1 text-xs">{item.reason}</p></div>)}{!auditConfigurations.length && <p className="text-sm text-secondary-500">No Internal Audit model has been selected.</p>}</div></Card>
        </div>}

        {tab === 'delivery' && <NotificationDeliveryOperations />}
        {tab === 'notifications' && <NotificationPolicyAdministration />}
        {tab === 'cutover' && <TargetNormalizationAdministration />}
      </div>
    </AppShell>
  );
}
