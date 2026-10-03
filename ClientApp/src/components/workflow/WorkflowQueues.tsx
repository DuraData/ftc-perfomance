import { useEffect, useState } from 'react';
import { CheckSquare, Clock, Eye, FileText, RotateCcw, Users } from 'lucide-react';
import { getIpmsSubmission, getOpmsSubmission, getWorkflowQueue } from '../../api/api';
import { useApp } from '../../context/AppContext';
import type { IPMSSubmission, OPMSSubmission, WorkflowQueueCountsDto, WorkflowQueueItemDto, WorkflowQueueName } from '../../types';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Tabs } from '../common/Tabs';
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

function QueueCard({ title, count, icon, color, onClick }: { title: string; count: number; icon: React.ReactNode; color: string; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-3 rounded-lg border border-secondary-200 bg-white p-3 text-left transition-all hover:border-secondary-300 hover:shadow dark:border-secondary-700 dark:bg-secondary-900">
      <div className={`rounded-lg p-2 ${color}`}>{icon}</div>
      <div className="flex-1"><p className="text-xl font-bold text-secondary-900 dark:text-white">{count}</p><p className="text-xs text-secondary-500">{title}</p></div>
    </button>
  );
}

function SubmissionDetailModal({ submission, isOpen, onClose, showAudit = false }: { submission: SubmissionDetail | null; isOpen: boolean; onClose: () => void; showAudit?: boolean }) {
  const [activeTab, setActiveTab] = useState('details');
  if (!submission) return null;
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
        <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant="compact" />
        <div className="py-3">
          {activeTab === 'details' && <div className="grid grid-cols-2 gap-3">
            <div><p className="text-[10px] text-secondary-500">Due Date</p><p className="text-sm font-medium">{submission.dueDate ? new Date(submission.dueDate).toLocaleDateString() : '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Actual</p><p className="text-sm font-medium">{submission.actualPerformance ?? '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Variance</p><p className={`text-sm font-medium ${submission.variance && submission.variance < 0 ? 'text-error-600' : 'text-success-600'}`}>{submission.variance ? `${submission.variance > 0 ? '+' : ''}${submission.variance}%` : '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Expenditure</p><p className="text-sm font-medium">R {submission.actualExpenditure?.toLocaleString() ?? '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Submitter</p><p className="text-sm font-medium">{submission.submitter?.displayName ?? '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Submitted</p><p className="text-sm font-medium">{submission.submittedAt ? new Date(submission.submittedAt).toLocaleDateString() : '-'}</p></div>
            {submission.varianceReason && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Variance Reason</p><p className="text-xs">{submission.varianceReason}</p></div>}
          </div>}
          {activeTab === 'verification' && <div className="grid grid-cols-2 gap-3">
            <div><p className="text-[10px] text-secondary-500">Verified By</p><p className="text-sm font-medium">{submission.verifier?.displayName ?? 'Pending'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Verified At</p><p className="text-sm font-medium">{submission.verifiedAt ? new Date(submission.verifiedAt).toLocaleDateString() : '-'}</p></div>
            {submission.verifierComments && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Comments</p><p className="text-xs">{submission.verifierComments}</p></div>}
          </div>}
          {activeTab === 'approval' && <div className="grid grid-cols-2 gap-3">
            <div><p className="text-[10px] text-secondary-500">Approved By</p><p className="text-sm font-medium">{submission.approver?.displayName ?? 'Pending'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Approved At</p><p className="text-sm font-medium">{submission.approvedAt ? new Date(submission.approvedAt).toLocaleDateString() : '-'}</p></div>
          </div>}
          {activeTab === 'audit' && <InternalAuditAssessmentPanel submissionId={submission.id} canAssess={showAudit} />}
        </div>
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
  const result = item.kind === 'opms' ? await getOpmsSubmission(item.id) : await getIpmsSubmission(item.id);
  return result.success ? result.data ?? null : null;
}

export function WorkflowQueues() {
  const { currentPath, pushToast } = useApp();
  const routeQueue: Record<string, WorkflowQueueName> = {
    '/workflow/verification': 'verification', '/workflow/approval': 'approval',
    '/workflow/pms-review': 'pms', '/workflow/auditor-review': 'auditor',
  };
  const [selectedQueue, setSelectedQueue] = useState<WorkflowQueueName | null>(routeQueue[currentPath] ?? null);
  const [selectedItem, setSelectedItem] = useState<WorkflowQueueItemDto | null>(null);
  const [selectedSubmission, setSelectedSubmission] = useState<SubmissionDetail | null>(null);
  const [items, setItems] = useState<WorkflowQueueItemDto[]>([]);
  const [counts, setCounts] = useState(emptyCounts);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true); setLoadError(null);
    void getWorkflowQueue(selectedQueue ?? 'all', page).then(result => {
      if (cancelled) return;
      if (!result.success || !result.data) {
        const message = result.message ?? 'Failed to load the workflow queue.';
        setLoadError(message); pushToast('error', message); setItems([]); return;
      }
      setCounts(result.data.counts); setItems(result.data.page.items); setTotalPages(result.data.page.totalPages);
    }).catch(() => {
      if (cancelled) return;
      const message = 'The workflow service is unavailable.';
      setLoadError(message); pushToast('error', message); setItems([]);
    }).finally(() => { if (!cancelled) setIsLoading(false); });
    return () => { cancelled = true; };
  }, [page, pushToast, selectedQueue]);

  const openSubmission = async (item: WorkflowQueueItemDto) => {
    setSelectedItem(item);
    const detail = await loadSubmissionDetail(item);
    if (!detail) { pushToast('error', 'The selected submission is no longer available.'); setSelectedItem(null); return; }
    setSelectedSubmission(detail);
  };
  const queues = [
    { id: 'my-submissions' as const, title: 'My Submissions', count: counts.mySubmissions, icon: <FileText className="h-5 w-5 text-white" />, color: 'bg-blue-500' },
    { id: 'verification' as const, title: 'Pending Verification', count: counts.verification, icon: <Users className="h-5 w-5 text-white" />, color: 'bg-amber-500' },
    { id: 'approval' as const, title: 'Pending Approval', count: counts.approval, icon: <CheckSquare className="h-5 w-5 text-white" />, color: 'bg-orange-500' },
    { id: 'pms' as const, title: 'PMS Review', count: counts.pms, icon: <Clock className="h-5 w-5 text-white" />, color: 'bg-primary-500' },
    { id: 'auditor' as const, title: 'Auditor Queue', count: counts.auditor, icon: <Eye className="h-5 w-5 text-white" />, color: 'bg-violet-500' },
    { id: 'returned' as const, title: 'Returned Items', count: counts.returned, icon: <RotateCcw className="h-5 w-5 text-white" />, color: 'bg-rose-500' },
  ];
  const currentQueue = queues.find(queue => queue.id === selectedQueue);
  const columns = [
    { id: 'target', header: 'Target', accessor: (row: WorkflowQueueItemDto) => <div><p className="font-medium text-secondary-900 dark:text-white">{row.kind === 'ipms' ? '[IPMS] ' : ''}{row.targetName}</p><p className="text-[10px] text-secondary-500">{row.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: WorkflowQueueItemDto) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: WorkflowQueueItemDto) => row.dueDate ? new Date(row.dueDate).toLocaleDateString() : '-' },
    { id: 'submitter', header: 'Submitter', accessor: (row: WorkflowQueueItemDto) => row.submittedByName ?? '-' },
    { id: 'status', header: 'Status', accessor: (row: WorkflowQueueItemDto) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : 'warning'}>{statusLabels[row.status]}</Badge> },
  ];
  return <AppShell title="Workflow Queues" subtitle="Manage authorised OPMS and IPMS work items"><div className="space-y-4">
    {isLoading && <Card><p className="py-6 text-center text-sm text-secondary-500">Loading authorised workflow items…</p></Card>}
    {!isLoading && loadError && <EmptyState icon={<FileText className="h-6 w-6" />} title="Workflow queue unavailable" description={loadError} />}
    {!isLoading && !loadError && <div className="grid grid-cols-2 gap-2 md:grid-cols-3 lg:grid-cols-6">{queues.map(queue => <QueueCard key={queue.id} {...queue} onClick={() => { setPage(1); setSelectedQueue(queue.id === selectedQueue ? null : queue.id); }} />)}</div>}
    {selectedQueue && currentQueue && !loadError && <Card>
      <div className="mb-3 flex items-center justify-between"><h3 className="text-sm font-semibold text-secondary-900 dark:text-white">{currentQueue.title}</h3><Button variant="ghost" size="sm" onClick={() => { setPage(1); setSelectedQueue(null); }}>Close</Button></div>
      <DataTable data={items} columns={columns} onRowClick={(row) => { void openSubmission(row); }} emptyMessage="No items" getRowId={(row) => `${row.kind}-${row.id}`} />
      <QueuePagination page={page} totalPages={totalPages} onChange={setPage} />
    </Card>}
    <SubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission} onClose={() => { setSelectedSubmission(null); setSelectedItem(null); }} showAudit={selectedQueue === 'auditor' && selectedItem?.kind === 'opms'} />
  </div></AppShell>;
}

export function MyWorkQueue() {
  const { currentPath, pushToast } = useApp();
  const routeQueue: Record<string, WorkflowQueueName> = {
    '/workflow/my-drafts': 'my-drafts', '/workflow/pending-submission': 'pending-submission',
    '/workflow/returned-submissions': 'my-returned', '/workflow/under-verification': 'under-verification',
    '/workflow/under-review': 'under-review', '/workflow/under-approval': 'under-approval',
    '/workflow/internal-audit-returned': 'internal-audit-returned', '/workflow/approved-closed': 'approved-closed',
  };
  const queue = routeQueue[currentPath] ?? 'my-submissions';
  const [selectedSubmission, setSelectedSubmission] = useState<SubmissionDetail | null>(null);
  const [items, setItems] = useState<WorkflowQueueItemDto[]>([]);
  const [counts, setCounts] = useState(emptyCounts);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [loadError, setLoadError] = useState<string | null>(null);
  useEffect(() => { setPage(1); }, [queue]);
  useEffect(() => {
    let cancelled = false; setLoadError(null);
    void getWorkflowQueue(queue, page).then(result => {
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
  }, [page, pushToast, queue]);
  const openSubmission = async (item: WorkflowQueueItemDto) => {
    const detail = await loadSubmissionDetail(item);
    if (!detail) pushToast('error', 'The selected submission is no longer available.'); else setSelectedSubmission(detail);
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
    { id: 'reviewer', header: 'Current Reviewer', accessor: (row: WorkflowQueueItemDto) => row.verifierName ?? row.approverName ?? 'Pending' },
    { id: 'status', header: 'Status', accessor: (row: WorkflowQueueItemDto) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'draft' ? 'default' : 'warning'}>{statusLabels[row.status]}</Badge> },
  ];
  return <AppShell title="My Work Queue" subtitle="Your submissions and workflow statuses"><div className="space-y-4">
    <div className="grid grid-cols-2 gap-2 md:grid-cols-4">{summary.map(item => <Card key={item.label} className="p-3"><p className="text-lg font-bold text-secondary-900 dark:text-white">{item.value}</p><p className="text-xs text-secondary-500">{item.label}</p></Card>)}</div>
    {loadError ? <EmptyState icon={<FileText className="h-6 w-6" />} title="Work queue unavailable" description={loadError} /> : <Card><DataTable data={items} columns={columns} onRowClick={(row) => { void openSubmission(row); }} emptyMessage="No items" getRowId={(row) => `${row.kind}-${row.id}`} /><QueuePagination page={page} totalPages={totalPages} onChange={setPage} /></Card>}
    <SubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission} onClose={() => setSelectedSubmission(null)} />
  </div></AppShell>;
}
