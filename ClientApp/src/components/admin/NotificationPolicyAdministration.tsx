import { useEffect, useMemo, useState } from 'react';
import { BellRing, CalendarDays } from 'lucide-react';
import {
  activateNotificationPolicy, addWorkingCalendarHoliday, copyNotificationPolicy, createNotificationPolicy, getNotificationPolicies,
  getWorkingCalendarHolidays, previewNotificationPolicy, runDueNotificationPolicies, setNotificationPolicyDeliveryState, testNotificationPolicy,
} from '../../api/api';
import type { NotificationPolicyDto, NotificationTemplatePreviewDto, ReportingPeriodMasterDto, WorkingCalendarHolidayDto } from '../../types';
import { useApp } from '../../context/AppContext';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormPanel, Input, Select, Textarea } from '../common/Form';

const localDate = (date = new Date()) => new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
const lifecycleName = (value: number) => ['Unknown', 'Draft', 'Active', 'Inactive', 'Superseded'][value] ?? 'Unknown';

export function NotificationPolicyAdministration({ periods }: { periods: ReportingPeriodMasterDto[] }) {
  const { pushToast } = useApp();
  const years = useMemo(() => Array.from(new Map(periods.map(period => [period.municipalityFinancialYearPublicId, period])).values()), [periods]);
  const [items, setItems] = useState<NotificationPolicyDto[]>([]);
  const [holidays, setHolidays] = useState<WorkingCalendarHolidayDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [preview, setPreview] = useState<NotificationTemplatePreviewDto | null>(null);
  const [draft, setDraft] = useState({
    municipalityFinancialYearPublicId: '', code: 'SUBMISSION_DUE', name: 'Submission deadline reminders', scope: 1 as 1 | 2 | 3,
    source: 1 as 1 | 2, submissionKind: 1 as 1 | 2, workflowStageCode: '', reportingPeriodPublicId: '', channels: ['IN_APP', 'EMAIL'],
    titleTemplate: '{Item} deadline', messageTemplate: '{Item} for {Period} is due on {DueDate}.', effectiveFrom: localDate(),
    offsets: '-7,-3,0,1', recipientKind: 1 as 1 | 2 | 3, recipientValues: '', reason: '',
  });
  const [holiday, setHoliday] = useState({ municipalityFinancialYearPublicId: '', date: '', name: '', reason: '' });

  const load = async () => {
    setBusy(true); setError(null);
    const [policyResult, holidayResult] = await Promise.all([getNotificationPolicies(), getWorkingCalendarHolidays()]);
    if (!policyResult.success || !holidayResult.success) setError(policyResult.message ?? holidayResult.message ?? 'Notification configuration could not be loaded.');
    setItems(policyResult.data ?? []); setHolidays(holidayResult.data ?? []); setBusy(false);
  };
  useEffect(() => { void load(); }, []);

  const saveDraft = async () => {
    const yearId = draft.municipalityFinancialYearPublicId || years[0]?.municipalityFinancialYearPublicId;
    if (!yearId) { setError('Create a reporting period before configuring reminders.'); return; }
    const offsets = draft.offsets.split(',').map(value => Number(value.trim())).filter(Number.isInteger);
    setBusy(true); setError(null);
    const result = await createNotificationPolicy({
      municipalityFinancialYearPublicId: yearId, code: draft.code, name: draft.name, scope: draft.scope, source: draft.source,
      submissionKind: draft.submissionKind, workflowStageCode: draft.scope === 2 ? draft.workflowStageCode : null,
      reportingPeriodPublicId: draft.scope === 3 ? draft.reportingPeriodPublicId : null, isMandatory: true, deliveryPaused: false,
      channels: draft.channels, titleTemplate: draft.titleTemplate, messageTemplate: draft.messageTemplate,
      effectiveFrom: new Date(draft.effectiveFrom).toISOString(), rules: offsets.map((offset, index) => ({
        code: offset < 0 ? `BEFORE_${Math.abs(offset)}_${index}` : offset === 0 ? `DUE_${index}` : `OVERDUE_${offset}_${index}`,
        workingDayOffset: offset, recipientKind: draft.recipientKind,
        recipientValues: draft.recipientValues.split(',').map(value => value.trim()).filter(Boolean),
      })), reason: draft.reason.trim(),
    });
    if (!result.success) setError(result.message ?? 'Draft policy could not be created.');
    else { pushToast('success', 'Notification policy draft created'); setDraft(current => ({ ...current, reason: '' })); await load(); }
    setBusy(false);
  };

  const act = async (item: NotificationPolicyDto, operation: 'activate' | 'pause' | 'resume' | 'copy' | 'preview' | 'test') => {
    const reason = reasons[item.publicId] ?? '';
    setBusy(true); setError(null);
    const result = operation === 'activate' ? await activateNotificationPolicy(item, reason)
      : operation === 'pause' ? await setNotificationPolicyDeliveryState(item, true, reason)
      : operation === 'resume' ? await setNotificationPolicyDeliveryState(item, false, reason)
      : operation === 'copy' ? await copyNotificationPolicy(item, draft.municipalityFinancialYearPublicId || years[0]?.municipalityFinancialYearPublicId || '', reason)
      : operation === 'test' ? await testNotificationPolicy(item) : await previewNotificationPolicy(item);
    if (!result.success) setError(result.message ?? `Policy could not ${operation}.`);
    else if (operation === 'preview') setPreview(result.data as NotificationTemplatePreviewDto);
    else { pushToast('success', `Notification policy ${operation} completed`); setReasons(current => ({ ...current, [item.publicId]: '' })); await load(); }
    setBusy(false);
  };

  const runDue = async () => {
    setBusy(true); const result = await runDueNotificationPolicies();
    if (!result.success) setError(result.message ?? 'Due notification processing failed.');
    else pushToast('success', result.message ?? `Queued ${result.data ?? 0} notification(s)`);
    setBusy(false);
  };

  const addHoliday = async () => {
    const yearId = holiday.municipalityFinancialYearPublicId || years[0]?.municipalityFinancialYearPublicId;
    if (!yearId) { setError('Select a financial year.'); return; }
    setBusy(true); const result = await addWorkingCalendarHoliday({ ...holiday, municipalityFinancialYearPublicId: yearId });
    if (!result.success) setError(result.message ?? 'Holiday could not be added.');
    else { pushToast('success', 'Working-calendar holiday added'); setHoliday(current => ({ ...current, date: '', name: '', reason: '' })); await load(); }
    setBusy(false);
  };

  const yearOptions = years.map((item, index) => ({ value: item.municipalityFinancialYearPublicId, label: `Financial year ${index + 1} · ${item.municipalityFinancialYearPublicId.slice(0, 8)}` }));
  return <div className="space-y-5">
    {error && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 p-3 text-sm text-error-700">{error}</div>}
    <div className="grid gap-5 xl:grid-cols-[1fr_1fr]">
      <FormPanel title="New notification policy draft" description="Policies inherit municipality → workflow stage → reporting period. Drafts have no effect until activated." icon={<BellRing className="h-5 w-5" />}>
        <div className="grid gap-3 sm:grid-cols-2">
          <Select label="Municipality financial year" value={draft.municipalityFinancialYearPublicId || years[0]?.municipalityFinancialYearPublicId || ''} options={yearOptions} onChange={event => setDraft(current => ({ ...current, municipalityFinancialYearPublicId: event.target.value }))} />
          <Select label="Source deadline" value={draft.source} options={[{ value: 1, label: 'Reporting window' }, { value: 2, label: 'RFI response' }]} onChange={event => setDraft(current => ({ ...current, source: Number(event.target.value) as 1 | 2 }))} />
          <Input label="Policy code" value={draft.code} onChange={event => setDraft(current => ({ ...current, code: event.target.value.toUpperCase() }))} />
          <Input label="Policy name" value={draft.name} onChange={event => setDraft(current => ({ ...current, name: event.target.value }))} />
          <Select label="Submission type" value={draft.submissionKind} options={[{ value: 1, label: 'OPMS' }, { value: 2, label: 'IPMS' }]} onChange={event => setDraft(current => ({ ...current, submissionKind: Number(event.target.value) as 1 | 2 }))} />
          <Select label="Inheritance scope" value={draft.scope} options={[{ value: 1, label: 'Municipality default' }, { value: 2, label: 'Workflow stage default' }, { value: 3, label: 'Reporting period override' }]} onChange={event => setDraft(current => ({ ...current, scope: Number(event.target.value) as 1 | 2 | 3 }))} />
          {draft.scope === 2 && <Input label="Workflow stage code" value={draft.workflowStageCode} onChange={event => setDraft(current => ({ ...current, workflowStageCode: event.target.value.toUpperCase() }))} />}
          {draft.scope === 3 && <Select label="Reporting period" value={draft.reportingPeriodPublicId} placeholder="Select period" options={periods.map(item => ({ value: item.publicId, label: `${item.code} · ${item.name}` }))} onChange={event => setDraft(current => ({ ...current, reportingPeriodPublicId: event.target.value }))} />}
          <Input label="Effective from" type="datetime-local" value={draft.effectiveFrom} onChange={event => setDraft(current => ({ ...current, effectiveFrom: event.target.value }))} />
          <div><Input label="Working-day offsets" value={draft.offsets} onChange={event => setDraft(current => ({ ...current, offsets: event.target.value }))} /><p className="mt-1 text-xs text-secondary-500">Comma-separated; negative before due, zero due day, positive overdue.</p></div>
          <Select label="Recipients" value={draft.recipientKind} options={[{ value: 1, label: 'Primary assignees' }, { value: 2, label: 'Dynamic role codes' }, { value: 3, label: 'Specific user IDs' }]} onChange={event => setDraft(current => ({ ...current, recipientKind: Number(event.target.value) as 1 | 2 | 3 }))} />
          {draft.recipientKind !== 1 && <div><Input label={draft.recipientKind === 2 ? 'Role codes' : 'User IDs'} value={draft.recipientValues} onChange={event => setDraft(current => ({ ...current, recipientValues: event.target.value }))} /><p className="mt-1 text-xs text-secondary-500">Comma-separated.</p></div>}
        </div>
        <div className="grid gap-2 sm:grid-cols-3">{['IN_APP', 'EMAIL', 'SMS'].map(channel => <Checkbox key={channel} label={channel.replace('_', ' ')} checked={draft.channels.includes(channel)} disabled={channel === 'IN_APP'} onChange={event => setDraft(current => ({ ...current, channels: event.target.checked ? [...current.channels, channel] : current.channels.filter(value => value !== channel) }))} />)}</div>
        <Input label="Title template" value={draft.titleTemplate} onChange={event => setDraft(current => ({ ...current, titleTemplate: event.target.value }))} />
        <Textarea label="Message template" value={draft.messageTemplate} onChange={event => setDraft(current => ({ ...current, messageTemplate: event.target.value }))} /><p className="text-xs text-secondary-500">Allowed placeholders: {'{Item}'}, {'{Period}'}, {'{Municipality}'}, {'{DueDate}'}, {'{Days}'}.</p>
        <Textarea label="Governance reason" value={draft.reason} onChange={event => setDraft(current => ({ ...current, reason: event.target.value }))} required />
        <Button variant="primary" onClick={() => void saveDraft()} disabled={busy}>Create draft</Button>
      </FormPanel>
      <Card className="p-4"><div className="flex items-center justify-between gap-3"><div><h3 className="font-semibold">Policy versions</h3><p className="text-xs text-secondary-500">Activation supersedes the effective policy at the same scope.</p></div><Button size="sm" variant="outline" onClick={() => void runDue()} disabled={busy}>Run due now</Button></div><div className="mt-3 space-y-3">
        {items.map(item => <div key={item.publicId} className="rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"><div className="flex flex-wrap justify-between gap-2"><div><p className="font-medium">{item.name}</p><p className="text-xs text-secondary-500">{item.code} · v{item.version} · {item.financialYearCode}</p></div><div className="flex gap-1"><Badge variant={item.lifecycle === 2 ? 'success' : 'default'}>{lifecycleName(item.lifecycle)}</Badge>{item.deliveryPaused && <Badge variant="warning">Paused</Badge>}</div></div><p className="mt-2 text-xs text-secondary-500">{item.channels.join(' + ')} · {item.rules.map(rule => `${rule.workingDayOffset}d`).join(', ')}</p><Textarea label={`Governance reason for ${item.code} v${item.version}`} value={reasons[item.publicId] ?? ''} onChange={event => setReasons(current => ({ ...current, [item.publicId]: event.target.value }))} /><div className="mt-2 flex flex-wrap gap-2"><Button size="sm" variant="outline" onClick={() => void act(item, 'preview')}>Preview</Button><Button size="sm" variant="outline" onClick={() => void act(item, 'test')} disabled={busy}>Queue test</Button>{item.lifecycle === 1 && <Button size="sm" variant="primary" onClick={() => void act(item, 'activate')} disabled={busy}>Activate</Button>}{item.lifecycle === 2 && <Button size="sm" variant="outline" onClick={() => void act(item, item.deliveryPaused ? 'resume' : 'pause')} disabled={busy}>{item.deliveryPaused ? 'Resume delivery' : 'Pause delivery'}</Button>}<Button size="sm" variant="outline" onClick={() => void act(item, 'copy')} disabled={busy}>Copy to selected FY</Button></div></div>)}
        {!items.length && <p className="text-sm text-secondary-500">No notification policies configured.</p>}
      </div>{preview && <div className="mt-4 rounded-lg border border-primary-200 bg-primary-50 p-3 text-sm"><p className="font-medium">{preview.title}</p><p>{preview.message}</p><p className="mt-1 text-xs">Channels: {preview.channels.join(', ')}</p></div>}</Card>
    </div>
    <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
      <FormPanel title="Working-calendar holiday" description="Reminder calculations exclude weekends and these municipality dates." icon={<CalendarDays className="h-5 w-5" />}>
        <Select label="Municipality financial year" value={holiday.municipalityFinancialYearPublicId || years[0]?.municipalityFinancialYearPublicId || ''} options={yearOptions} onChange={event => setHoliday(current => ({ ...current, municipalityFinancialYearPublicId: event.target.value }))} />
        <Input label="Date" type="date" value={holiday.date} onChange={event => setHoliday(current => ({ ...current, date: event.target.value }))} />
        <Input label="Holiday name" value={holiday.name} onChange={event => setHoliday(current => ({ ...current, name: event.target.value }))} />
        <Textarea label="Governance reason" value={holiday.reason} onChange={event => setHoliday(current => ({ ...current, reason: event.target.value }))} />
        <Button variant="primary" onClick={() => void addHoliday()} disabled={busy}>Add holiday</Button>
      </FormPanel>
      <Card className="p-4"><h3 className="font-semibold">Configured holidays</h3><div className="mt-3 space-y-2">{holidays.map(item => <div key={item.publicId} className="flex justify-between rounded-lg border border-secondary-200 p-3 text-sm dark:border-secondary-700"><span>{item.name}</span><span className="text-secondary-500">{new Date(item.date).toLocaleDateString()} · {item.financialYearCode}</span></div>)}{!holidays.length && <p className="text-sm text-secondary-500">No municipality holidays configured.</p>}</div></Card>
    </div>
  </div>;
}
