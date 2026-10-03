import { useEffect, useMemo, useState } from 'react';
import {
  Clock,
  FileText,
  Users,
  Eye,
  CheckSquare,
  RotateCcw,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Tabs } from '../common/Tabs';
import { getIpmsSubmissions, getOpmsSubmissions } from '../../api/api';
import { useApp } from '../../context/AppContext';
import type { OPMSSubmission, SubmissionStatus } from '../../types';

const statusLabels: Record<SubmissionStatus, string> = {
  draft: 'Draft',
  submitted: 'Submitted',
  pending_verification: 'Pending Verification',
  verified: 'Verified',
  verify_rejected: 'Verification Rejected',
  pending_approval: 'Pending Approval',
  approved: 'Approved',
  rejected: 'Rejected',
  reviewed: 'Reviewed',
  returned_for_info: 'Returned for Information',
  audited: 'Audited',
  completed: 'Completed',
};

const statusColors: Record<SubmissionStatus, string> = {
  draft: 'bg-secondary-100 text-secondary-800',
  submitted: 'bg-blue-100 text-blue-800',
  pending_verification: 'bg-amber-100 text-amber-800',
  verified: 'bg-cyan-100 text-cyan-800',
  verify_rejected: 'bg-rose-100 text-rose-800',
  pending_approval: 'bg-orange-100 text-orange-800',
  approved: 'bg-green-100 text-green-800',
  rejected: 'bg-red-100 text-red-800',
  reviewed: 'bg-indigo-100 text-indigo-800',
  returned_for_info: 'bg-yellow-100 text-yellow-800',
  audited: 'bg-violet-100 text-violet-800',
  completed: 'bg-emerald-100 text-emerald-800',
};

function QueueCard({ title, count, icon, color, onClick }: { title: string; count: number; icon: React.ReactNode; color: string; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-3 p-3 bg-white dark:bg-secondary-900 rounded-lg border border-secondary-200 dark:border-secondary-700 hover:shadow hover:border-secondary-300 transition-all text-left">
      <div className={`p-2 rounded-lg ${color}`}>{icon}</div>
      <div className="flex-1">
        <p className="text-xl font-bold text-secondary-900 dark:text-white">{count}</p>
        <p className="text-xs text-secondary-500">{title}</p>
      </div>
    </button>
  );
}

function SubmissionDetailModal({ submission, isOpen, onClose }: { submission: OPMSSubmission | null; isOpen: boolean; onClose: () => void }) {
  const [activeTab, setActiveTab] = useState('details');

  if (!submission) return null;

  const tabs = [
    { id: 'details', label: 'Details' },
    { id: 'verification', label: 'Verification' },
    { id: 'approval', label: 'Approval' },
  ];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`${submission.quarter} Submission`} size="lg">
      <div className="space-y-3">
        {/* Status banner */}
        <div className={`p-3 rounded ${statusColors[submission.status]}`}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <FileText className="w-4 h-4" />
              <div>
                <p className="text-sm font-medium">{submission.target.targetName}</p>
                <p className="text-xs opacity-80">Status: {statusLabels[submission.status]}</p>
              </div>
            </div>
            <Badge size="sm" variant="info">{submission.quarter}</Badge>
          </div>
        </div>

        {/* Tabs */}
        <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant="compact" />

        {/* Tab content */}
        <div className="py-3">
          {activeTab === 'details' && (
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Due Date</p><p className="text-sm font-medium">{new Date(submission.dueDate).toLocaleDateString()}</p></div>
              <div><p className="text-[10px] text-secondary-500">Actual</p><p className="text-sm font-medium">{submission.actual?.toLocaleString() ?? '-'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Variance</p><p className={`text-sm font-medium ${submission.variance && submission.variance < 0 ? 'text-error-600' : 'text-success-600'}`}>{submission.variance ? `${submission.variance > 0 ? '+' : ''}${submission.variance}%` : '-'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Expenditure</p><p className="text-sm font-medium">R {submission.actualExpenditure?.toLocaleString() ?? '-'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Submitter</p><p className="text-sm font-medium">{submission.submitter?.displayName ?? '-'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Submitted</p><p className="text-sm font-medium">{submission.submittedAt ? new Date(submission.submittedAt).toLocaleDateString() : '-'}</p></div>
              {submission.actualDescription && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Description</p><p className="text-xs">{submission.actualDescription}</p></div>}
              {submission.varianceReason && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Variance Reason</p><p className="text-xs">{submission.varianceReason}</p></div>}
            </div>
          )}
          {activeTab === 'verification' && (
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Verified By</p><p className="text-sm font-medium">{submission.verifier?.displayName ?? 'Pending'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Verified At</p><p className="text-sm font-medium">{submission.verifiedAt ? new Date(submission.verifiedAt).toLocaleDateString() : '-'}</p></div>
              {submission.verifierComments && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Comments</p><p className="text-xs">{submission.verifierComments}</p></div>}
            </div>
          )}
          {activeTab === 'approval' && (
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Approved By</p><p className="text-sm font-medium">{submission.approver?.displayName ?? 'Pending'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Approved At</p><p className="text-sm font-medium">{submission.approvedAt ? new Date(submission.approvedAt).toLocaleDateString() : '-'}</p></div>
            </div>
          )}
        </div>
      </div>
    </Modal>
  );
}

export function WorkflowQueues() {
  const { currentPath, userProfile, pushToast } = useApp();
  const routeQueue: Record<string, string> = {
    '/workflow/verification': 'verification',
    '/workflow/approval': 'approval',
    '/workflow/pms-review': 'pms',
    '/workflow/auditor-review': 'auditor',
  };
  const [selectedQueue, setSelectedQueue] = useState<string | null>(routeQueue[currentPath] ?? null);
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [submissions, setSubmissions] = useState<OPMSSubmission[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setLoadError(null);
    void getOpmsSubmissions().then(result => {
      if (cancelled) return;
      if (!result.success) {
        const message = result.message ?? 'Failed to load the workflow queue.';
        setLoadError(message);
        pushToast('error', message);
        setSubmissions([]);
        return;
      }
      setSubmissions(result.data ?? []);
    }).catch(() => {
      if (cancelled) return;
      const message = 'The workflow service is unavailable.';
      setLoadError(message);
      pushToast('error', message);
      setSubmissions([]);
    }).finally(() => {
      if (!cancelled) setIsLoading(false);
    });
    return () => { cancelled = true; };
  }, [pushToast]);

  const queues = useMemo(() => {
    const matching = (statuses: SubmissionStatus[]) => submissions.filter(item => statuses.includes(item.status));
    const own = submissions.filter(item => item.submittedByUserId === userProfile?.id || item.submitter?.id === userProfile?.id);
    const definitions = [
      { id: 'my-submissions', title: 'My Submissions', icon: <FileText className="w-5 h-5 text-white" />, color: 'bg-blue-500', submissions: own },
      { id: 'verification', title: 'Pending Verification', icon: <Users className="w-5 h-5 text-white" />, color: 'bg-amber-500', submissions: matching(['pending_verification']) },
      { id: 'approval', title: 'Pending Approval', icon: <CheckSquare className="w-5 h-5 text-white" />, color: 'bg-orange-500', submissions: matching(['verified', 'pending_approval']) },
      { id: 'pms', title: 'PMS Review', icon: <Clock className="w-5 h-5 text-white" />, color: 'bg-primary-500', submissions: matching(['approved']) },
      { id: 'auditor', title: 'Auditor Queue', icon: <Eye className="w-5 h-5 text-white" />, color: 'bg-violet-500', submissions: matching(['reviewed']) },
      { id: 'returned', title: 'Returned Items', icon: <RotateCcw className="w-5 h-5 text-white" />, color: 'bg-rose-500', submissions: matching(['returned_for_info', 'verify_rejected', 'rejected']) },
    ];
    return definitions.map(queue => ({ ...queue, count: queue.submissions.length }));
  }, [submissions, userProfile?.id]);

  const columns = [
    { id: 'target', header: 'Target', accessor: (row: OPMSSubmission) => <div><p className="font-medium text-secondary-900 dark:text-white">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => new Date(row.dueDate).toLocaleDateString() },
    { id: 'actual', header: 'Actual', accessor: (row: OPMSSubmission) => row.actual?.toLocaleString() ?? '-' },
    { id: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : 'warning'}>{statusLabels[row.status]}</Badge> },
  ];

  const currentQueue = queues.find(q => q.id === selectedQueue);

  return (
    <AppShell title="Workflow Queues" subtitle="Manage work items">
      <div className="space-y-4">
        {isLoading && <Card><p className="py-6 text-center text-sm text-secondary-500">Loading authorised workflow items…</p></Card>}
        {!isLoading && loadError && <EmptyState icon={<FileText className="w-6 h-6" />} title="Workflow queue unavailable" description={loadError} />}
        {/* Queue cards */}
        {!isLoading && !loadError && <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2">
          {queues.map(queue => (
            <QueueCard key={queue.id} {...queue} onClick={() => setSelectedQueue(queue.id === selectedQueue ? null : queue.id)} />
          ))}
        </div>}

        {/* Queue details */}
        {selectedQueue && currentQueue && (
          <Card>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-secondary-900 dark:text-white">{currentQueue.title}</h3>
              <Button variant="ghost" size="sm" onClick={() => setSelectedQueue(null)}>Close</Button>
            </div>
            <DataTable data={currentQueue.submissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} emptyMessage="No items" getRowId={(row) => row.id} />
          </Card>
        )}

        <SubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission} onClose={() => setSelectedSubmission(null)} />
      </div>
    </AppShell>
  );
}

export function MyWorkQueue() {
  const { currentPath, userProfile, pushToast } = useApp();
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [submissions, setSubmissions] = useState<OPMSSubmission[]>([]);

  useEffect(() => {
    const load = async () => {
      const [opmsResult, ipmsResult] = await Promise.all([getOpmsSubmissions(), getIpmsSubmissions()]);

      if (!opmsResult.success) {
        pushToast('error', opmsResult.message ?? 'Failed to load OPMS workflow queue');
      }

      if (!ipmsResult.success) {
        pushToast('error', ipmsResult.message ?? 'Failed to load IPMS workflow queue');
      }

      const ownOpms = (opmsResult.data ?? []).filter(item => item.submitter?.id === userProfile?.id);
      const ownIpmsAsOpms = (ipmsResult.data ?? [])
        .filter(item => item.submitter?.id === userProfile?.id)
        .map(item => ({
          id: `ipms-${item.id}`,
          target: {
            id: item.target.id,
            targetName: `[IPMS] ${item.target.targetName}`,
            indicatorNumber: item.target.indicatorNumber,
          },
          quarter: item.quarter,
          dueDate: item.dueDate,
          actual: item.actual,
          variance: item.variance,
          status: item.status,
          submitter: item.submitter,
          verifier: item.verifier,
          approver: item.approver,
          comments: item.comments,
          attachments: item.attachments,
        } as unknown as OPMSSubmission));

      setSubmissions([...ownOpms, ...ownIpmsAsOpms]);
    };

    void load();
  }, [pushToast, userProfile?.id]);

  const mySubmissions = useMemo(() => {
    const filterByPath: Record<string, OPMSSubmission['status'][]> = {
      '/workflow/my-drafts': ['draft'],
      '/workflow/pending-submission': ['submitted'],
      '/workflow/returned-submissions': ['returned_for_info', 'verify_rejected', 'rejected'],
      '/workflow/under-verification': ['pending_verification'],
      '/workflow/under-review': ['reviewed'],
      '/workflow/under-approval': ['pending_approval', 'verified'],
      '/workflow/internal-audit-returned': ['audited'],
      '/workflow/approved-closed': ['approved', 'completed'],
    };

    const expectedStatuses = filterByPath[currentPath];
    if (!expectedStatuses) {
      return submissions;
    }

    return submissions.filter(item => expectedStatuses.includes(item.status));
  }, [currentPath, submissions]);

  const queueSummary = useMemo(() => {
    const count = (statuses: OPMSSubmission['status'][]) => submissions.filter(item => statuses.includes(item.status)).length;
    return [
      { label: 'My Drafts', value: count(['draft']) },
      { label: 'Pending Submission', value: count(['submitted']) },
      { label: 'Returned Submissions', value: count(['returned_for_info', 'verify_rejected', 'rejected']) },
      { label: 'Under Verification', value: count(['pending_verification']) },
      { label: 'Under Review', value: count(['reviewed']) },
      { label: 'Under Approval', value: count(['pending_approval', 'verified']) },
      { label: 'Internal Audit Returned', value: count(['audited']) },
      { label: 'Approved / Closed', value: count(['approved', 'completed']) },
    ];
  }, [submissions]);

  const columns = [
    { id: 'target', header: 'Target', accessor: (row: OPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => new Date(row.dueDate).toLocaleDateString() },
    {
      id: 'days',
      header: 'Days Outstanding',
      accessor: (row: OPMSSubmission) => {
        const diffMs = Date.now() - new Date(row.dueDate).getTime();
        const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        return days > 0 ? `${days} overdue` : `${Math.abs(days)} remaining`;
      }
    },
    { id: 'reviewer', header: 'Current Reviewer', accessor: (row: OPMSSubmission) => row.verifier?.displayName ?? row.approver?.displayName ?? 'Pending' },
    { id: 'comment', header: 'Last Comment', accessor: (row: OPMSSubmission) => row.comments?.[0]?.content ?? '-' },
    { id: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'draft' ? 'default' : 'warning'}>{statusLabels[row.status]}</Badge> },
    {
      id: 'actions',
      header: 'Actions',
      accessor: (row: OPMSSubmission) => row.status === 'draft'
        ? <Button variant="primary" size="sm">Edit Draft</Button>
        : <Button variant="ghost" size="sm">View History</Button>
    },
  ];

  return (
    <AppShell title="My Work Queue" subtitle="Your submissions and workflow statuses">
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
          {queueSummary.map(item => (
            <Card key={item.label} className="p-3">
              <p className="text-lg font-bold text-secondary-900 dark:text-white">{item.value}</p>
              <p className="text-xs text-secondary-500">{item.label}</p>
            </Card>
          ))}
        </div>
        <Card>
          <DataTable data={mySubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} emptyMessage="No items" getRowId={(row) => row.id} />
        </Card>
        <SubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission} onClose={() => setSelectedSubmission(null)} />
      </div>
    </AppShell>
  );
}
