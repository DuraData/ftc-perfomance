import { useState } from 'react';
import {
  Clock,
  FileText,
  Users,
  Eye,
  CheckSquare,
  MessageSquare,
  RotateCcw,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, EmptyState } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Tabs } from '../common/Tabs';
import { mockOPMSSubmissions, statusLabels, statusColors } from '../../data/mockData';
import type { OPMSSubmission } from '../../types';

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
    { id: 'proof', label: 'Evidence' },
    { id: 'verification', label: 'Verification' },
    { id: 'approval', label: 'Approval' },
    { id: 'pms', label: 'PMS' },
    { id: 'auditor', label: 'Auditor' },
    { id: 'comments', label: 'Comments' },
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
          {activeTab === 'proof' && <EmptyState icon={<FileText className="w-6 h-6" />} title="No documents" action={<Button variant="outline" size="sm">Upload</Button>} />}
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
          {activeTab === 'comments' && <div className="text-center text-secondary-500 py-6 text-xs">No comments yet</div>}
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between pt-3 border-t border-secondary-200">
          <div className="flex items-center gap-1">
            <Button variant="outline" size="sm" icon={<RotateCcw className="w-3.5 h-3.5" />}>Return</Button>
            <Button variant="ghost" size="sm" icon={<MessageSquare className="w-3.5 h-3.5" />}>Comment</Button>
          </div>
          <div className="flex items-center gap-1">
            {submission.status === 'pending_verification' && <Button variant="success" size="sm" icon={<CheckSquare className="w-3.5 h-3.5" />}>Verify</Button>}
            {submission.status === 'verified' && <Button variant="primary" size="sm" icon={<CheckSquare className="w-3.5 h-3.5" />}>Approve</Button>}
          </div>
        </div>
      </div>
    </Modal>
  );
}

export function WorkflowQueues() {
  const [selectedQueue, setSelectedQueue] = useState<string | null>(null);
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);

  const queues = [
    { id: 'my-submissions', title: 'My Submissions', count: 3, icon: <FileText className="w-5 h-5 text-white" />, color: 'bg-blue-500', submissions: mockOPMSSubmissions.slice(0, 3) },
    { id: 'verification', title: 'Pending Verification', count: mockOPMSSubmissions.filter(s => s.status === 'pending_verification').length, icon: <Users className="w-5 h-5 text-white" />, color: 'bg-amber-500', submissions: mockOPMSSubmissions.filter(s => s.status === 'pending_verification') },
    { id: 'approval', title: 'Pending Approval', count: 8, icon: <CheckSquare className="w-5 h-5 text-white" />, color: 'bg-orange-500', submissions: mockOPMSSubmissions.filter(s => s.status === 'verified') },
    { id: 'pms', title: 'PMS Review', count: 4, icon: <Clock className="w-5 h-5 text-white" />, color: 'bg-primary-500', submissions: mockOPMSSubmissions.filter(s => s.status === 'approved') },
    { id: 'auditor', title: 'Auditor Queue', count: 2, icon: <Eye className="w-5 h-5 text-white" />, color: 'bg-violet-500', submissions: [] },
    { id: 'returned', title: 'Returned Items', count: mockOPMSSubmissions.filter(s => s.status === 'returned_for_info').length, icon: <RotateCcw className="w-5 h-5 text-white" />, color: 'bg-rose-500', submissions: mockOPMSSubmissions.filter(s => s.status === 'returned_for_info') },
  ];

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
        {/* Queue cards */}
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-2">
          {queues.map(queue => (
            <QueueCard key={queue.id} {...queue} onClick={() => setSelectedQueue(queue.id === selectedQueue ? null : queue.id)} />
          ))}
        </div>

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
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const mySubmissions = mockOPMSSubmissions.filter(s => s.submitter?.id === '2' || s.status === 'pending_verification' || s.status === 'submitted');

  const columns = [
    { id: 'target', header: 'Target', accessor: (row: OPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Qtr', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => <span className={new Date(row.dueDate) < new Date() && row.status === 'draft' ? 'text-error-600 font-medium' : ''}>{new Date(row.dueDate).toLocaleDateString()}</span> },
    { id: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'draft' ? 'default' : 'warning'}>{statusLabels[row.status]}</Badge> },
    { id: 'actions', header: '', accessor: (row: OPMSSubmission) => row.status === 'draft' ? <Button variant="primary" size="sm">Complete</Button> : <Button variant="ghost" size="sm">View</Button> },
  ];

  return (
    <AppShell title="My Work Queue" subtitle="Your pending tasks">
      <div className="space-y-4">
        <Card>
          <DataTable data={mySubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} emptyMessage="No items" getRowId={(row) => row.id} />
        </Card>
        <SubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission} onClose={() => setSelectedSubmission(null)} />
      </div>
    </AppShell>
  );
}
