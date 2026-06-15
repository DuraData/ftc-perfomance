import React, { useState } from 'react';
import { Plus, Download, Edit2, Eye, Send, CheckCircle, Clock, AlertTriangle, RotateCcw, FileText } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Tabs } from '../common/Tabs';
import { Input, Select, Textarea, FormSection, FormRow } from '../common/Form';
import { FileUpload } from '../common/FileUpload';
import { mockOPMSTargets, mockOPMSSubmissions, statusLabels, statusColors } from '../../data/mockData';
import type { OPMSTarget, OPMSSubmission } from '../../types';

function OPMSSubmissionFormModal({ isOpen, onClose, target, submission }: { isOpen: boolean; onClose: () => void; target?: OPMSTarget; submission?: OPMSSubmission }) {
  const quarters = [{ value: 'Q1', label: 'Q1 (Jul-Sep)' }, { value: 'Q2', label: 'Q2 (Oct-Dec)' }, { value: 'Mid-Year', label: 'Mid-Year' }, { value: 'Q3', label: 'Q3 (Jan-Mar)' }, { value: 'Q4', label: 'Q4 (Apr-Jun)' }];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={submission ? 'Edit Submission' : 'New Submission'} size="lg">
      <div className="space-y-3">
        <FormSection title="Target Information">
          <Select label="OPMS Target" options={mockOPMSTargets.map(t => ({ value: t.id, label: `${t.indicatorNumber} - ${t.targetName}` }))} defaultValue={target?.id || submission?.target.id} placeholder="Select target" required />
          <FormRow cols={2}>
            <Select label="Quarter" options={quarters} defaultValue={submission?.quarter || 'Q1'} required />
            <Input label="Due Date" type="date" defaultValue={submission?.dueDate || ''} required />
          </FormRow>
        </FormSection>

        <FormSection title="Performance Information">
          <FormRow cols={2}>
            <Input label="Actual Performance" type="number" defaultValue={submission?.actual || ''} placeholder="Actual value" required />
            <Input label="Actual Expenditure (R)" type="number" defaultValue={submission?.actualExpenditure || ''} placeholder="Amount" />
          </FormRow>
          <Textarea label="Performance Description" defaultValue={submission?.actualDescription || ''} placeholder="Describe performance achieved" rows={2} />
        </FormSection>

        <FormSection title="Variance Analysis">
          <Input label="Variance (%)" type="number" defaultValue={submission?.variance || ''} placeholder="Auto-calculated or enter" />
          <Textarea label="Variance Reason" defaultValue={submission?.varianceReason || ''} placeholder="Explain variance if applicable" rows={2} />
          <Textarea label="Corrective Measures" defaultValue={submission?.correctiveMeasure || ''} placeholder="Actions to address variance" rows={2} />
        </FormSection>

        <FormSection title="Supporting Documents">
          <FileUpload documentTypes={[{ value: 'evidence', label: 'Evidence' }, { value: 'report', label: 'Report' }]} />
        </FormSection>
      </div>

      <div className="flex items-center justify-between mt-4 pt-3 border-t">
        <Button variant="ghost" size="sm" onClick={onClose}>Cancel</Button>
        <div className="flex gap-2">
          <Button variant="outline" size="sm">Save Draft</Button>
          <Button variant="primary" size="sm" icon={<Send className="w-3.5 h-3.5" />}>Submit</Button>
        </div>
      </div>
    </Modal>
  );
}

function OPMSSubmissionDetailModal({ submission, isOpen, onClose }: { submission: OPMSSubmission | null; isOpen: boolean; onClose: () => void }) {
  const [activeTab, setActiveTab] = useState('details');

  if (!submission) return null;

  const tabs = [
    { id: 'details', label: 'Details' },
    { id: 'evidence', label: 'Evidence' },
    { id: 'verification', label: 'Verification' },
    { id: 'approval', label: 'Approval' },
    { id: 'pms', label: 'PMS' },
    { id: 'auditor', label: 'Auditor' },
    { id: 'comments', label: 'Comments' },
  ];

  const canVerify = submission.status === 'pending_verification';
  const canApprove = submission.status === 'verified';
  const canAudit = submission.status === 'approved';
  const canReturn = ['submitted', 'pending_verification', 'verified'].includes(submission.status);

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`${submission.quarter} Submission`} size="lg">
      {/* Status Banner */}
      <div className={`p-3 rounded mb-3 ${statusColors[submission.status]}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <FileText className="w-4 h-4" />
            <div>
              <p className="text-sm font-medium">{submission.target.targetName}</p>
              <p className="text-xs opacity-80">{submission.target.indicatorNumber} • {statusLabels[submission.status]}</p>
            </div>
          </div>
          <Badge size="sm" variant="info">{submission.quarter}</Badge>
        </div>
      </div>

      <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant="compact" />

      <div className="py-3 space-y-3">
        {activeTab === 'details' && (
          <div className="grid grid-cols-2 gap-3">
            <div><p className="text-[10px] text-secondary-500">Due Date</p><p className="text-sm font-medium">{new Date(submission.dueDate).toLocaleDateString()}</p></div>
            <div><p className="text-[10px] text-secondary-500">Extended Due</p><p className="text-sm font-medium">{submission.extendedDueDate ? new Date(submission.extendedDueDate).toLocaleDateString() : '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Target</p><p className="text-sm font-medium">{submission.target.annualTarget.toLocaleString()} {submission.target.unitOfMeasure.name}</p></div>
            <div><p className="text-[10px] text-secondary-500">Actual</p><p className="text-sm font-medium">{submission.actual?.toLocaleString() ?? '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Expenditure</p><p className="text-sm font-medium">R {submission.actualExpenditure?.toLocaleString() ?? '-'}</p></div>
            <div><p className="text-[10px] text-secondary-500">Variance</p><p className={`text-sm font-medium ${submission.variance && submission.variance < 0 ? 'text-error-600' : 'text-success-600'}`}>{submission.variance ? `${submission.variance > 0 ? '+' : ''}${submission.variance}%` : '-'}</p></div>
            {submission.actualDescription && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Description</p><p className="text-xs">{submission.actualDescription}</p></div>}
            {submission.varianceReason && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Variance Reason</p><p className="text-xs">{submission.varianceReason}</p></div>}
            {submission.correctiveMeasure && <div className="col-span-2"><p className="text-[10px] text-secondary-500">Corrective Measures</p><p className="text-xs">{submission.correctiveMeasure}</p></div>}
          </div>
        )}
        {activeTab === 'evidence' && (
          <div className="space-y-2">
            {submission.attachments.length === 0 ? (
              <div className="text-center py-6">
                <AlertTriangle className="w-8 h-8 mx-auto text-secondary-400 mb-2" />
                <p className="text-xs text-secondary-500">No evidence documents uploaded</p>
                <Button variant="outline" size="sm" className="mt-2">Upload Document</Button>
              </div>
            ) : (
              submission.attachments.map(a => (
                <div key={a.id} className="flex items-center justify-between p-2 bg-secondary-50 rounded">
                  <span className="text-xs">{a.fileName}</span>
                  <Button variant="ghost" size="sm">Download</Button>
                </div>
              ))
            )}
          </div>
        )}
        {activeTab === 'verification' && (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Verified By</p><p className="text-sm font-medium">{submission.verifier?.displayName || 'Pending'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Verified At</p><p className="text-sm font-medium">{submission.verifiedAt ? new Date(submission.verifiedAt).toLocaleString() : '-'}</p></div>
            </div>
            {submission.verifierComments && <div><p className="text-[10px] text-secondary-500">Comments</p><p className="text-xs">{submission.verifierComments}</p></div>}
            {!submission.verifier && canVerify && (
              <div className="space-y-2 pt-2 border-t">
                <Textarea label="Verification Comments" placeholder="Add verification notes" rows={2} />
                <div className="flex gap-2">
                  <Button variant="success" size="sm" icon={<CheckCircle className="w-3.5 h-3.5" />}>Verify</Button>
                  <Button variant="error" size="sm" icon={<RotateCcw className="w-3.5 h-3.5" />}>Return for Info</Button>
                </div>
              </div>
            )}
          </div>
        )}
        {activeTab === 'approval' && (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Approved By</p><p className="text-sm font-medium">{submission.approver?.displayName || 'Pending'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Approved At</p><p className="text-sm font-medium">{submission.approvedAt ? new Date(submission.approvedAt).toLocaleString() : '-'}</p></div>
            </div>
            {submission.approverComments && <div><p className="text-[10px] text-secondary-500">Comments</p><p className="text-xs">{submission.approverComments}</p></div>}
            {!submission.approver && canApprove && (
              <div className="space-y-2 pt-2 border-t">
                <Textarea label="Approval Comments" placeholder="Add approval notes" rows={2} />
                <div className="flex gap-2">
                  <Button variant="success" size="sm" icon={<CheckCircle className="w-3.5 h-3.5" />}>Approve</Button>
                  <Button variant="error" size="sm" icon={<AlertTriangle className="w-3.5 h-3.5" />}>Reject</Button>
                </div>
              </div>
            )}
          </div>
        )}
        {activeTab === 'pms' && (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">PMS Score</p><p className="text-sm font-medium">{submission.submitterScore ?? '-'}</p></div>
              <div><p className="text-[10px] text-secondary-500">PMS Officer</p><p className="text-sm font-medium">{submission.pmsOfficer?.displayName || 'Pending'}</p></div>
            </div>
            {submission.pmsComments && <div><p className="text-[10px] text-secondary-500">PMS Comments</p><p className="text-xs">{submission.pmsComments}</p></div>}
          </div>
        )}
        {activeTab === 'auditor' && (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div><p className="text-[10px] text-secondary-500">Audited By</p><p className="text-sm font-medium">{submission.auditor?.displayName || 'Pending'}</p></div>
              <div><p className="text-[10px] text-secondary-500">Audited At</p><p className="text-sm font-medium">{submission.auditedAt ? new Date(submission.auditedAt).toLocaleString() : '-'}</p></div>
            </div>
            {submission.auditorComments && <div><p className="text-[10px] text-secondary-500">Findings</p><p className="text-xs">{submission.auditorComments}</p></div>}
            {canAudit && (
              <div className="space-y-2 pt-2 border-t">
                <Textarea label="Audit Findings" placeholder="Record audit findings" rows={2} />
                <Button variant="primary" size="sm" icon={<CheckCircle className="w-3.5 h-3.5" />}>Complete Audit</Button>
              </div>
            )}
          </div>
        )}
        {activeTab === 'comments' && (
          <div className="space-y-2">
            {submission.comments.length === 0 ? (
              <div className="text-center py-6">
                <p className="text-xs text-secondary-500">No comments yet</p>
              </div>
            ) : (
              submission.comments.map(c => (
                <div key={c.id} className="p-2 bg-secondary-50 rounded">
                  <div className="flex items-center justify-between mb-1">
                    <p className="text-xs font-medium">{c.author.displayName}</p>
                    <p className="text-[10px] text-secondary-500">{new Date(c.createdAt).toLocaleString()}</p>
                  </div>
                  <p className="text-xs">{c.content}</p>
                </div>
              ))
            )}
            <Textarea placeholder="Add a comment..." rows={2} />
            <Button variant="outline" size="sm">Add Comment</Button>
          </div>
        )}
      </div>

      {/* Workflow Actions */}
      <div className="flex items-center justify-between pt-3 border-t">
        <div className="flex gap-1">
          {canReturn && <Button variant="outline" size="sm" icon={<RotateCcw className="w-3.5 h-3.5" />}>Return</Button>}
        </div>
        <Button variant="ghost" size="sm" onClick={onClose}>Close</Button>
      </div>
    </Modal>
  );
}

export function OPMSSubmissionsList() {
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedTarget, setSelectedTarget] = useState<OPMSTarget | null>(null);

  // Get all submissions with their targets
  const allSubmissions = mockOPMSSubmissions;

  const columns = [
    { id: 'target', header: 'Target', accessor: (row: OPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Quarter', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => <span className={new Date(row.dueDate) < new Date() && row.status === 'draft' ? 'text-error-600' : ''}>{new Date(row.dueDate).toLocaleDateString()}</span> },
    { id: 'actual', header: 'Actual', accessor: (row: OPMSSubmission) => row.actual?.toLocaleString() ?? '-' },
    { id: 'variance', header: 'Var', accessor: (row: OPMSSubmission) => <span className={row.variance && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{row.variance ? `${row.variance > 0 ? '+' : ''}${row.variance}%` : '-'}</span> },
    { id: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status.includes('pending') ? 'warning' : 'default'}>{statusLabels[row.status]}</Badge> },
  ];

  const actions = (row: OPMSSubmission) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); setSelectedSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
      {row.status === 'draft' && <button onClick={(e) => { e.stopPropagation(); setSelectedSubmission(row); setShowCreateModal(true); }} className="p-1 rounded hover:bg-secondary-100"><Edit2 className="w-3.5 h-3.5 text-secondary-400" /></button>}
    </div>
  );

  return (
    <AppShell title="OPMS Submissions" subtitle="All OPMS target submissions">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{allSubmissions.length} submissions</Badge>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>New Submission</Button>
          </div>
        </div>
        <Card>
          <DataTable data={allSubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} actions={actions} getRowId={(row) => row.id} />
        </Card>

        <OPMSSubmissionFormModal isOpen={showCreateModal} onClose={() => { setShowCreateModal(false); setSelectedSubmission(null); }} />
        <OPMSSubmissionDetailModal submission={selectedSubmission} isOpen={!!selectedSubmission && !showCreateModal} onClose={() => setSelectedSubmission(null)} />
      </div>
    </AppShell>
  );
}

export function VoteNumbersPage() {
  const voteNumbers = [
    { id: '1', number: 'V001', name: 'Roads Infrastructure', amount: 25000000, department: 'Infrastructure', isActive: true },
    { id: '2', number: 'V002', name: 'Water Infrastructure', amount: 35000000, department: 'Infrastructure', isActive: true },
    { id: '3', number: 'V003', name: 'Electricity Infrastructure', amount: 45000000, department: 'Infrastructure', isActive: true },
  ];

  const [selectedVote, setSelectedVote] = useState<typeof voteNumbers[0] | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const columns = [
    { id: 'number', header: 'Vote #', accessor: (row: typeof voteNumbers[0]) => <span className="font-mono">{row.number}</span> },
    { id: 'name', header: 'Name', accessor: (row: typeof voteNumbers[0]) => row.name },
    { id: 'department', header: 'Department', accessor: (row: typeof voteNumbers[0]) => row.department },
    { id: 'amount', header: 'Amount', accessor: (row: typeof voteNumbers[0]) => `R ${(row.amount / 1000000).toFixed(1)}M` },
    { id: 'status', header: 'Status', accessor: (row: typeof voteNumbers[0]) => <Badge size="sm" variant="success">Active</Badge> },
  ];

  return (
    <AppShell title="Vote Numbers" subtitle="Budget vote numbers">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{voteNumbers.length} votes</Badge>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => setShowCreateModal(true)}>Add</Button>
          </div>
        </div>
        <Card>
          <DataTable data={voteNumbers} columns={columns} onRowClick={(row) => setSelectedVote(row)} getRowId={(row) => row.id} />
        </Card>

        <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New Vote Number" size="md">
          <div className="space-y-3">
            <FormRow cols={2}>
              <Input label="Vote Number" placeholder="e.g., V001" required />
              <Input label="Name" placeholder="Vote name" required />
            </FormRow>
            <FormRow cols={2}>
              <Input label="Amount (R)" type="number" placeholder="Budget amount" />
              <Select label="Department" options={[{ value: 'infra', label: 'Infrastructure' }, { value: 'comm', label: 'Community' }]} placeholder="Select" />
            </FormRow>
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
            <Button variant="primary" size="sm">Create</Button>
          </div>
        </Modal>

        <Modal isOpen={!!selectedVote && !showCreateModal} onClose={() => setSelectedVote(null)} title={selectedVote?.name} size="sm">
          {selectedVote && (
            <div className="space-y-2">
              <div><p className="text-[10px] text-secondary-500">Vote Number</p><p className="text-sm font-mono">{selectedVote.number}</p></div>
              <div><p className="text-[10px] text-secondary-500">Department</p><p className="text-sm">{selectedVote.department}</p></div>
              <div><p className="text-[10px] text-secondary-500">Budget Amount</p><p className="text-sm font-bold">R {selectedVote.amount.toLocaleString()}</p></div>
              <div className="flex justify-end gap-2 pt-2 border-t">
                <Button variant="ghost" size="sm" onClick={() => setSelectedVote(null)}>Close</Button>
                <Button variant="outline" size="sm">Edit</Button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AppShell>
  );
}
