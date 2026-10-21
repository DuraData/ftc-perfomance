import { useEffect, useState } from 'react';
import { FileText } from 'lucide-react';
import { getIpmsSubmission, getOpmsSubmission, getWorkflowQueue } from '../../api/api';
import { useApp } from '../../context/AppContext';
import { useSecurity } from '../../context/SecurityContext';
import type { IPMSSubmission, OPMSSubmission, WorkflowQueueCountsDto, WorkflowQueueFilters, WorkflowQueueItemDto, WorkflowQueueName } from '../../types';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { DataTable } from '../common/DataTable';
import { Input, Select } from '../common/Form';
import { Modal } from '../common/Modal';
import { TabPanel, Tabs } from '../common/Tabs';
import { AppShell } from '../layout/AppShell';
import { submissionStatusColors as statusColors, submissionStatusLabels as statusLabels } from '../submissions/submissionStatus';
import { Badge, Button, Card, EmptyState } from '../ui';
import { InternalAuditAssessmentPanel } from './InternalAuditAssessmentPanel';

const emptyCounts: WorkflowQueueCountsDto = {
  mySubmissions: 0, verification: 0, approval: 0, pms: 0, auditor: 0, returned: 0,
  myDrafts: 0, pendingSubmission: 0, myReturned: 0, underVerification: 0, underReview: 0,
  underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
};

type SubmissionDetail = OPMSSubmission | IPMSSubmission;

type QueueRoute = { queue: WorkflowQueueName; title: string; subtitle: string };
type QueueFilterState = Required<WorkflowQueueFilters>;

const defaultQueueFilters: QueueFilterState = {
  search: '', municipalityFinancialYearPublicId: '', kind: 'all', period: '', status: 'all', sortBy: 'createdAt', sortDirection: 'desc',
};
const queueKinds = ['all', 'opms', 'ipms'] as const;
const queuePeriods = ['', 'Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'] as const;
const queueStatuses = ['all', 'draft', 'submitted', 'pending_verification', 'verified', 'pending_approval', 'approved', 'reviewed', 'returned_for_info', 'verify_rejected', 'rejected', 'audited', 'completed', 'cancelled'] as const;
const queueSortFields = ['createdAt', 'dueDate', 'targetName', 'indicatorNumber', 'quarter', 'status'] as const;
const queueSortDirections = ['asc', 'desc'] as const;
const publicIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function validPublicId(value: unknown): string {
  return typeof value === 'string' && publicIdPattern.test(value) ? value : '';
}

function readQueueFilters(queue: WorkflowQueueName, activeFinancialYearPublicId?: string | null): QueueFilterState {
  try {
    const raw = JSON.parse(sessionStorage.getItem(`workflow-queue-filters:${queue}`) ?? '{}') as Partial<QueueFilterState>;
    const stored: QueueFilterState = {
      search: typeof raw.search === 'string' ? raw.search.slice(0, 200) : '',
      municipalityFinancialYearPublicId: validPublicId(raw.municipalityFinancialYearPublicId),
      kind: queueKinds.includes(raw.kind as QueueFilterState['kind']) ? raw.kind as QueueFilterState['kind'] : 'all',
      period: queuePeriods.includes(raw.period as typeof queuePeriods[number]) ? raw.period as QueueFilterState['period'] : '',
      status: queueStatuses.includes(raw.status as QueueFilterState['status']) ? raw.status as QueueFilterState['status'] : 'all',
      sortBy: queueSortFields.includes(raw.sortBy as QueueFilterState['sortBy']) ? raw.sortBy as QueueFilterState['sortBy'] : 'createdAt',
      sortDirection: queueSortDirections.includes(raw.sortDirection as QueueFilterState['sortDirection']) ? raw.sortDirection as QueueFilterState['sortDirection'] : 'desc',
    };
    const parameters = new URLSearchParams(window.location.search);
    const linkedKind = parameters.get('kind');
    const linkedPeriod = parameters.get('period');
    const linkedStatus = parameters.get('status');
    return {
      ...stored,
      municipalityFinancialYearPublicId: validPublicId(parameters.get('municipalityFinancialYearPublicId')) || validPublicId(activeFinancialYearPublicId) || stored.municipalityFinancialYearPublicId,
      kind: queueKinds.includes(linkedKind as QueueFilterState['kind']) ? linkedKind as QueueFilterState['kind'] : stored.kind,
      period: queuePeriods.includes(linkedPeriod as typeof queuePeriods[number]) ? linkedPeriod as QueueFilterState['period'] : stored.period,
      status: queueStatuses.includes(linkedStatus as QueueFilterState['status']) ? linkedStatus as QueueFilterState['status'] : stored.status,
    };
  } catch {
    return { ...defaultQueueFilters };
  }
}

function useSessionQueueFilters(queue: WorkflowQueueName, activeFinancialYearPublicId?: string | null) {
  const [state, setState] = useState(() => ({ queue, filters: readQueueFilters(queue, activeFinancialYearPublicId) }));
  const filters = state.queue === queue ? state.filters : readQueueFilters(queue, activeFinancialYearPublicId);

  useEffect(() => {
    if (state.queue !== queue) setState({ queue, filters });
  }, [filters, queue, state.queue]);
  useEffect(() => {
    try { sessionStorage.setItem(`workflow-queue-filters:${queue}`, JSON.stringify(filters)); } catch { /* Storage can be unavailable in hardened browsers. */ }
  }, [filters, queue]);

  const updateFilters = (changes: Partial<QueueFilterState>) => setState({ queue, filters: { ...filters, ...changes } });
  const resetFilters = () => setState({ queue, filters: { ...defaultQueueFilters, municipalityFinancialYearPublicId: validPublicId(activeFinancialYearPublicId) } });
  return { filters, updateFilters, resetFilters };
}

function QueueFilters({ filters, onChange, onReset }: { filters: QueueFilterState; onChange: (changes: Partial<QueueFilterState>) => void; onReset: () => void }) {
  return <Card>
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-7">
      <Input label="Search KPI or indicator" value={filters.search} maxLength={200} onChange={event => onChange({ search: event.target.value })} />
      <CalendarMasterPicker kind="municipality-financial-year" label="Financial year" emptyLabel="All active years" value={filters.municipalityFinancialYearPublicId} onChange={value => onChange({ municipalityFinancialYearPublicId: value })} />
      <Select label="Framework" value={filters.kind} onChange={event => onChange({ kind: event.target.value as QueueFilterState['kind'] })} options={[
        { value: 'all', label: 'All frameworks' }, { value: 'opms', label: 'OPMS' }, { value: 'ipms', label: 'IPMS' },
      ]} />
      <Select label="Period" value={filters.period} onChange={event => onChange({ period: event.target.value as QueueFilterState['period'] })} options={[
        { value: '', label: 'All periods' }, { value: 'Q1', label: 'Quarter 1' }, { value: 'Q2', label: 'Quarter 2' },
        { value: 'Mid-Year', label: 'Mid-Term' }, { value: 'Q3', label: 'Quarter 3' }, { value: 'Q4', label: 'Quarter 4' }, { value: 'Annual', label: 'Annual' },
      ]} />
      <Select label="Status" value={filters.status} onChange={event => onChange({ status: event.target.value as QueueFilterState['status'] })} options={[
        { value: 'all', label: 'All queue statuses' },
        ...queueStatuses.filter(status => status !== 'all').map(status => ({ value: status, label: statusLabels[status] })),
      ]} />
      <div className="flex items-end gap-2 xl:col-span-2">
        <Select label="Sort by" value={filters.sortBy} onChange={event => onChange({ sortBy: event.target.value as QueueFilterState['sortBy'] })} options={[
          { value: 'createdAt', label: 'Newest activity' }, { value: 'dueDate', label: 'Due date' }, { value: 'targetName', label: 'KPI name' },
          { value: 'indicatorNumber', label: 'Indicator number' }, { value: 'quarter', label: 'Period' }, { value: 'status', label: 'Status' },
        ]} />
        <Select label="Direction" value={filters.sortDirection} onChange={event => onChange({ sortDirection: event.target.value as QueueFilterState['sortDirection'] })} options={[
          { value: 'desc', label: 'Descending' }, { value: 'asc', label: 'Ascending' },
        ]} />
        <Button type="button" variant="outline" size="sm" onClick={onReset}>Reset</Button>
      </div>
    </div>
  </Card>;
}

const governedQueueRoutes: Record<string, QueueRoute> = {
  '/workflow/verification': { queue: 'verification', title: 'Verification Queue', subtitle: 'Verify authorised OPMS and IPMS submissions' },
  '/workflow/approval': { queue: 'approval', title: 'Approval Queue', subtitle: 'Approve verified OPMS and IPMS submissions' },
  '/workflow/pms-review': { queue: 'pms', title: 'PMS Review Queue', subtitle: 'Complete authorised performance-management reviews' },
  '/workflow/auditor-review': { queue: 'auditor', title: 'Internal Audit Queue', subtitle: 'Complete authorised evidence-based internal audit reviews' },
};

const personalQueueRoutes: Record<string, QueueRoute> = {
  '/workflow/my-queue': { queue: 'my-submissions', title: 'My Queue', subtitle: 'Your submissions and workflow statuses' },
  '/workflow/my-drafts': { queue: 'my-drafts', title: 'My Drafts', subtitle: 'Your draft submissions awaiting completion' },
  '/workflow/pending-submission': { queue: 'pending-submission', title: 'Pending Submission', subtitle: 'Your completed work awaiting submission' },
  '/workflow/returned-submissions': { queue: 'my-returned', title: 'Returned Submissions', subtitle: 'Your submissions returned for correction' },
  '/workflow/under-verification': { queue: 'under-verification', title: 'Under Verification', subtitle: 'Your submissions currently awaiting verification' },
  '/workflow/under-review': { queue: 'under-review', title: 'Under Review', subtitle: 'Your submissions currently awaiting PMS review' },
  '/workflow/under-approval': { queue: 'under-approval', title: 'Under Approval', subtitle: 'Your submissions currently awaiting approval' },
  '/workflow/internal-audit-returned': { queue: 'internal-audit-returned', title: 'Internal Audit Returned', subtitle: 'Your submissions returned by Internal Audit' },
  '/workflow/approved-closed': { queue: 'approved-closed', title: 'Approved / Closed', subtitle: 'Your completed and approved submissions' },
};

function SubmissionDetailModal({ submission, kind, isOpen, onClose, showAudit = false, initialTab = 'details' }: { submission: SubmissionDetail | null; kind: 'opms' | 'ipms' | null; isOpen: boolean; onClose: () => void; showAudit?: boolean; initialTab?: 'details' | 'verification' | 'approval' | 'audit' }) {
  const [activeTab, setActiveTab] = useState(initialTab);
  const { setCurrentPath } = useApp();
  const security = useSecurity();
  useEffect(() => { if (isOpen) setActiveTab(initialTab); }, [initialTab, isOpen, submission?.id]);
  if (!submission || !kind) return null;
  const resource = kind === 'opms' ? 'OPMS_SUBMISSION' : 'IPMS_SUBMISSION';
  const canReadActual = security.canReadField(resource, 'ActualPerformance');
  const canReadVariance = security.canReadField(resource, 'Variance');
  const canReadVarianceReason = security.canReadField(resource, 'VarianceReason');
  const canReadSubmitter = security.canReadField(resource, 'SubmitterIdentity');
  const canReadSubmittedDate = security.canReadField(resource, 'SubmittedDate');
  const canReadVerifier = security.canReadField(resource, 'VerifierIdentity');
  const canReadVerifierComment = security.canReadField(resource, 'VerifierComment');
  const canReadApprover = security.canReadField(resource, 'ApproverIdentity');
  const tabs = [
    { id: 'details', label: 'Details' },
    { id: 'verification', label: 'Verification' },
    { id: 'approval', label: 'Approval' },
    ...(showAudit ? [{ id: 'audit', label: 'Internal Audit' }] : []),
  ];
  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`${submission.quarter} Submission`} size="lg">
      <div className="space-y-3">
        <div className={`rounded p-3 ${statusColors[submission.status]}`}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2"><FileText className="h-4 w-4" /><div><p className="text-sm font-medium">{submission.target.targetName}</p><p className="text-xs opacity-80">Status: {statusLabels[submission.status]}</p></div></div>
            <Badge size="sm" variant="info">{submission.quarter}</Badge>
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <Tabs id="workflow-queue-detail" ariaLabel="Workflow queues" tabs={tabs} activeTab={activeTab} onChange={tab => setActiveTab(tab as typeof activeTab)} variant="compact" />
          {kind === 'opms' && <Button
            variant="outline"
            size="sm"
            onClick={() => {
              const tab = activeTab === 'audit' ? 'internal-audit' : activeTab === 'details' ? 'reporting' : 'workflow';
              const parameters = new URLSearchParams({ tab });
              if (submission.reportingPeriodPublicId) parameters.set('period', submission.reportingPeriodPublicId);
              onClose();
              setCurrentPath(`/opms/targets/${submission.target.id}?${parameters.toString()}`);
            }}
          >Open KPI Workspace</Button>}
        </div>
        <TabPanel tabSetId="workflow-queue-detail" tabId={activeTab} className="py-3">
          {activeTab === 'details' && <div className="grid grid-cols-2 gap-3">
            <div><p className="text-[10px] text-secondary-500">Due Date</p><p className="text-sm font-medium">{submission.dueDate ? new Date(submission.dueDate).toLocaleDateString() : '-'}</p></div>
            {canReadActual && <div><p className="text-[10px] text-secondary-500">Actual</p><p className="text-sm font-medium">{submission.actualPerformance ?? '-'}</p></div>}
            {canReadVariance && <div><p className="text-[10px] text-secondary-500">Variance</p><p className={`text-sm font-medium ${submission.variance && submission.variance < 0 ? 'text-error-600' : 'text-success-600'}`}>{submission.variance ? `${submission.variance > 0 ? '+' : ''}${submission.variance}%` : '-'}</p></div>}
            {canReadActual && <div><p className="text-[10px] text-secondary-500">Expenditure</p><p className="text-sm font-medium">R {submission.actualExpenditure?.toLocaleString() ?? '-'}</p></div>}
            {canReadSubmitter && <div><p className="text-[10px] text-secondary-500">Submitter</p><p className="text-sm font-medium">{submission.submitter?.displayName ?? '-'}</p></div>}
            {canReadSubmittedDate && <div><p className="text-[10px] text-secondary-500">Submitted</p><p className="text-sm font-medium">{submission.submittedAt ? new Date(submission.submittedAt).toLocaleDateString() : '-'}</p></div>}
            {canReadVarianceReason && submission.varianceReason && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Variance Reason</p><p className="text-xs">{submission.varianceReason}</p></div>}
          </div>}
          {activeTab === 'verification' && <div className="grid grid-cols-2 gap-3">
            {canReadVerifier && <div><p className="text-[10px] text-secondary-500">Verified By</p><p className="text-sm font-medium">{submission.verifier?.displayName ?? 'Pending'}</p></div>}
            <div><p className="text-[10px] text-secondary-500">Verified At</p><p className="text-sm font-medium">{submission.verifiedAt ? new Date(submission.verifiedAt).toLocaleDateString() : '-'}</p></div>
            {canReadVerifierComment && submission.verifierComments && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Comments</p><p className="text-xs">{submission.verifierComments}</p></div>}
          </div>}
          {activeTab === 'approval' && <div className="grid grid-cols-2 gap-3">
            {canReadApprover && <div><p className="text-[10px] text-secondary-500">Approved By</p><p className="text-sm font-medium">{submission.approver?.displayName ?? 'Pending'}</p></div>}
            <div><p className="text-[10px] text-secondary-500">Approved At</p><p className="text-sm font-medium">{submission.approvedAt ? new Date(submission.approvedAt).toLocaleDateString() : '-'}</p></div>
          </div>}
          {activeTab === 'audit' && <InternalAuditAssessmentPanel submissionId={submission.id} canAssess={showAudit} />}
        </TabPanel>
      </div>
    </Modal>
  );
}

function QueuePagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return null;
  return <div className="mt-4 flex items-center justify-end gap-2">
    <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>Previous</Button>
    <span className="text-xs text-secondary-500">Page {page} of {totalPages}</span>
    <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Next</Button>
  </div>;
}

async function loadSubmissionDetail(item: WorkflowQueueItemDto): Promise<SubmissionDetail | null> {
  const result = item.kind === 'opms' ? await getOpmsSubmission(item.publicId) : await getIpmsSubmission(item.publicId);
  return result.success ? result.data ?? null : null;
}

export function WorkflowQueues() {
  const { currentPath, pushToast, currentFinancialYearPublicId } = useApp();
  const security = useSecurity();
  const route = governedQueueRoutes[currentPath] ?? governedQueueRoutes['/workflow/verification'];
  const queue = route.queue;
  const { filters, updateFilters, resetFilters } = useSessionQueueFilters(queue, currentFinancialYearPublicId);
  const [selectedItem, setSelectedItem] = useState<WorkflowQueueItemDto | null>(null);
  const [selectedSubmission, setSelectedSubmission] = useState<SubmissionDetail | null>(null);
  const [items, setItems] = useState<WorkflowQueueItemDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const changeFilters = (changes: Partial<QueueFilterState>) => { setPage(1); updateFilters(changes); };
  const clearFilters = () => { setPage(1); resetFilters(); };

  useEffect(() => { setPage(1); }, [queue]);
  useEffect(() => {
    let cancelled = false;
    setIsLoading(true); setLoadError(null);
    void getWorkflowQueue(queue, page, 25, filters).then(result => {
      if (cancelled) return;
      if (!result.success || !result.data) {
        const message = result.message ?? 'Failed to load the workflow queue.';
        setLoadError(message); pushToast('error', message); setItems([]); setTotalCount(0); return;
      }
      setItems(result.data.page.items); setTotalCount(result.data.page.totalCount); setTotalPages(result.data.page.totalPages);
    }).catch(() => {
      if (cancelled) return;
      const message = 'The workflow service is unavailable.';
      setLoadError(message); pushToast('error', message); setItems([]); setTotalCount(0);
    }).finally(() => { if (!cancelled) setIsLoading(false); });
    return () => { cancelled = true; };
  }, [filters, page, pushToast, queue]);

  const openSubmission = async (item: WorkflowQueueItemDto) => {
    setSelectedItem(item);
    const detail = await loadSubmissionDetail(item);
    if (!detail) { pushToast('error', 'The selected submission is no longer available.'); setSelectedItem(null); return; }
    setSelectedSubmission(detail);
  };
  const columns = [
    { id: 'target', header: 'Target', accessor: (row: WorkflowQueueItemDto) => <div><p className="font-medium text-secondary-900 dark:text-white">{row.kind === 'ipms' ? '[IPMS] ' : ''}{row.targetName}</p><p className="text-[10px] text-secondary-500">{row.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: WorkflowQueueItemDto) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: WorkflowQueueItemDto) => row.dueDate ? new Date(row.dueDate).toLocaleDateString() : '-' },
    { id: 'submitter', header: 'Submitter', accessor: (row: WorkflowQueueItemDto) => security.canReadField(row.kind === 'opms' ? 'OPMS_SUBMISSION' : 'IPMS_SUBMISSION', 'SubmitterIdentity') ? row.submittedByName ?? '-' : 'Restricted' },
    { id: 'status', header: 'Status', accessor: (row: WorkflowQueueItemDto) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : 'warning'}>{statusLabels[row.status]}</Badge> },
  ];
  return <AppShell title={route.title} subtitle={route.subtitle}><div className="space-y-4">
    <QueueFilters filters={filters} onChange={changeFilters} onReset={clearFilters} />
    {isLoading && <Card><p className="py-6 text-center text-sm text-secondary-500">Loading authorised workflow items…</p></Card>}
    {!isLoading && loadError && <EmptyState icon={<FileText className="h-6 w-6" />} title="Workflow queue unavailable" description={loadError} />}
    {!isLoading && !loadError && <Card>
      <div className="mb-3 flex items-center justify-between gap-3"><h2 className="text-sm font-semibold text-secondary-900 dark:text-white">{route.title}</h2><span className="text-xs text-secondary-500">{totalCount} authorised item{totalCount === 1 ? '' : 's'}</span></div>
      <DataTable tableLabel={`${route.title} submissions`} data={items} columns={columns} onRowClick={(row) => { void openSubmission(row); }} getRowActionLabel={(row) => `Open ${row.kind.toUpperCase()} submission for ${row.targetName}`} emptyMessage="No items" getRowId={(row) => `${row.kind}-${row.publicId}`} />
      <QueuePagination page={page} totalPages={totalPages} onChange={setPage} />
    </Card>}
    <SubmissionDetailModal submission={selectedSubmission} kind={selectedItem?.kind ?? null} isOpen={!!selectedSubmission} onClose={() => { setSelectedSubmission(null); setSelectedItem(null); }} showAudit={queue === 'auditor' && selectedItem?.kind === 'opms'} initialTab={queue === 'verification' ? 'verification' : queue === 'approval' ? 'approval' : queue === 'auditor' ? 'audit' : 'details'} />
  </div></AppShell>;
}

export function MyWorkQueue() {
  const { currentPath, pushToast, currentFinancialYearPublicId } = useApp();
  const security = useSecurity();
  const route = personalQueueRoutes[currentPath] ?? personalQueueRoutes['/workflow/my-queue'];
  const queue = route.queue;
  const { filters, updateFilters, resetFilters } = useSessionQueueFilters(queue, currentFinancialYearPublicId);
  const [selectedSubmission, setSelectedSubmission] = useState<SubmissionDetail | null>(null);
  const [selectedKind, setSelectedKind] = useState<'opms' | 'ipms' | null>(null);
  const [items, setItems] = useState<WorkflowQueueItemDto[]>([]);
  const [counts, setCounts] = useState(emptyCounts);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loadError, setLoadError] = useState<string | null>(null);
  const changeFilters = (changes: Partial<QueueFilterState>) => { setPage(1); updateFilters(changes); };
  const clearFilters = () => { setPage(1); resetFilters(); };
  useEffect(() => { setPage(1); }, [queue]);
  useEffect(() => {
    let cancelled = false; setLoadError(null);
    void getWorkflowQueue(queue, page, 25, filters).then(result => {
      if (cancelled) return;
      if (!result.success || !result.data) {
        const message = result.message ?? 'Failed to load your work queue.';
        setLoadError(message); pushToast('error', message); setItems([]); return;
      }
      setCounts(result.data.counts); setItems(result.data.page.items); setTotalPages(result.data.page.totalPages);
    }).catch(() => {
      if (cancelled) return;
      const message = 'The workflow service is unavailable.';
      setLoadError(message); pushToast('error', message); setItems([]);
    });
    return () => { cancelled = true; };
  }, [filters, page, pushToast, queue]);
  const openSubmission = async (item: WorkflowQueueItemDto) => {
    const detail = await loadSubmissionDetail(item);
    if (!detail) pushToast('error', 'The selected submission is no longer available.'); else { setSelectedSubmission(detail); setSelectedKind(item.kind); }
  };
  const summary = [
    { label: 'My Drafts', value: counts.myDrafts }, { label: 'Pending Submission', value: counts.pendingSubmission },
    { label: 'Returned Submissions', value: counts.myReturned }, { label: 'Under Verification', value: counts.underVerification },
    { label: 'Under Review', value: counts.underReview }, { label: 'Under Approval', value: counts.underApproval },
    { label: 'Internal Audit Returned', value: counts.internalAuditReturned }, { label: 'Approved / Closed', value: counts.approvedClosed },
  ];
  const columns = [
    { id: 'target', header: 'Target', accessor: (row: WorkflowQueueItemDto) => <div><p className="font-medium">{row.kind === 'ipms' ? '[IPMS] ' : ''}{row.targetName}</p><p className="text-[10px] text-secondary-500">{row.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: WorkflowQueueItemDto) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: WorkflowQueueItemDto) => row.dueDate ? new Date(row.dueDate).toLocaleDateString() : '-' },
    { id: 'days', header: 'Days Outstanding', accessor: (row: WorkflowQueueItemDto) => { if (!row.dueDate) return '-'; const days = Math.floor((Date.now() - new Date(row.dueDate).getTime()) / 86_400_000); return days > 0 ? `${days} overdue` : `${Math.abs(days)} remaining`; } },
    { id: 'reviewer', header: 'Current Reviewer', accessor: (row: WorkflowQueueItemDto) => {
      const resource = row.kind === 'opms' ? 'OPMS_SUBMISSION' : 'IPMS_SUBMISSION';
      const verifier = security.canReadField(resource, 'VerifierIdentity') ? row.verifierName : undefined;
      const approver = security.canReadField(resource, 'ApproverIdentity') ? row.approverName : undefined;
      return verifier ?? approver ?? 'Restricted';
    } },
    { id: 'status', header: 'Status', accessor: (row: WorkflowQueueItemDto) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'draft' ? 'default' : 'warning'}>{statusLabels[row.status]}</Badge> },
  ];
  return <AppShell title={route.title} subtitle={route.subtitle}><div className="space-y-4">
    <div className="grid grid-cols-2 gap-2 md:grid-cols-4">{summary.map(item => <Card key={item.label} className="p-3"><p className="text-lg font-bold text-secondary-900 dark:text-white">{item.value}</p><p className="text-xs text-secondary-500">{item.label}</p></Card>)}</div>
    <QueueFilters filters={filters} onChange={changeFilters} onReset={clearFilters} />
    {loadError ? <EmptyState icon={<FileText className="h-6 w-6" />} title="Work queue unavailable" description={loadError} /> : <Card><DataTable tableLabel={`${route.title} submissions`} data={items} columns={columns} onRowClick={(row) => { void openSubmission(row); }} getRowActionLabel={(row) => `Open ${row.kind.toUpperCase()} submission for ${row.targetName}`} emptyMessage="No items" getRowId={(row) => `${row.kind}-${row.publicId}`} /><QueuePagination page={page} totalPages={totalPages} onChange={setPage} /></Card>}
    <SubmissionDetailModal submission={selectedSubmission} kind={selectedKind} isOpen={!!selectedSubmission} onClose={() => { setSelectedSubmission(null); setSelectedKind(null); }} />
  </div></AppShell>;
}
