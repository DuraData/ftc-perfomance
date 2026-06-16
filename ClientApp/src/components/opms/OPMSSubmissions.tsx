import { useMemo, useState } from 'react';
import { Plus, Download, Eye } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, FormRow } from '../common/Form';
import {
  mockIPMSSubmissions,
  mockOPMSSubmissions,
  statusLabels,
} from '../../data/mockData';
import type { IPMSSubmission, OPMSSubmission } from '../../types';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';

export function OPMSSubmissionsList() {
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const allSubmissions = useMemo(() => mockOPMSSubmissions, []);

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
    </div>
  );

  return (
    <AppShell title="OPMS Submissions" subtitle="All OPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="OPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Badge variant="primary">{allSubmissions.length} submissions</Badge>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
              <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => {
                // For now, select the first draft submission or first submission
                const firstSubmission = allSubmissions.find(s => s.status === 'draft') || allSubmissions[0];
                if (firstSubmission) {
                  setSelectedSubmission(firstSubmission);
                }
              }}>New Submission</Button>
            </div>
          </div>
          <Card>
            <DataTable data={allSubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} actions={actions} getRowId={(row) => row.id} />
          </Card>
        </div>
      )}
    </AppShell>
  );
}

export function IPMSSubmissionsList() {
  const [selectedSubmission, setSelectedSubmission] = useState<IPMSSubmission | null>(null);
  const allSubmissions = useMemo(() => mockIPMSSubmissions, []);

  const columns = [
    { id: 'target', header: 'Target', accessor: (row: IPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', header: 'Quarter', accessor: (row: IPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: IPMSSubmission) => new Date(row.dueDate).toLocaleDateString() },
    { id: 'actual', header: 'Actual', accessor: (row: IPMSSubmission) => row.actual?.toLocaleString() ?? '-' },
    { id: 'variance', header: 'Var', accessor: (row: IPMSSubmission) => <span className={row.variance && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{row.variance ? `${row.variance > 0 ? '+' : ''}${row.variance}%` : '-'}</span> },
    { id: 'status', header: 'Status', accessor: (row: IPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status.includes('pending') ? 'warning' : 'default'}>{statusLabels[row.status]}</Badge> },
  ];

  const actions = (row: IPMSSubmission) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); setSelectedSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
    </div>
  );

  return (
    <AppShell title="IPMS Submissions" subtitle="All IPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="IPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Badge variant="primary">{allSubmissions.length} submissions</Badge>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
              <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => {
                // Select the first submission for "New Submission" demo
                if (allSubmissions.length > 0) {
                  setSelectedSubmission(allSubmissions[0]);
                }
              }}>New Submission</Button>
            </div>
          </div>
          <Card>
            <DataTable data={allSubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} actions={actions} getRowId={(row) => row.id} />
          </Card>
        </div>
      )}
    </AppShell>
  );
}

export function VoteNumbersPage() {
  const [voteNumbers, setVoteNumbers] = useState([
    { id: '1', number: 'V001', name: 'Roads Infrastructure', amount: 25000000, department: 'Infrastructure', isActive: true },
    { id: '2', number: 'V002', name: 'Water Infrastructure', amount: 35000000, department: 'Infrastructure', isActive: true },
    { id: '3', number: 'V003', name: 'Electricity Infrastructure', amount: 45000000, department: 'Infrastructure', isActive: true },
  ]);

  const [selectedVote, setSelectedVote] = useState<typeof voteNumbers[0] | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const columns = [
    { id: 'number', header: 'Vote #', accessor: (row: typeof voteNumbers[0]) => <span className="font-mono">{row.number}</span> },
    { id: 'name', header: 'Name', accessor: (row: typeof voteNumbers[0]) => row.name },
    { id: 'department', header: 'Department', accessor: (row: typeof voteNumbers[0]) => row.department },
    { id: 'amount', header: 'Amount', accessor: (row: typeof voteNumbers[0]) => `R ${(row.amount / 1000000).toFixed(1)}M` },
    { id: 'status', header: 'Status', accessor: (_row: typeof voteNumbers[0]) => <Badge size="sm" variant="success">Active</Badge> },
  ];

  const handleDelete = (id: string) => {
    setVoteNumbers(voteNumbers.filter(v => v.id !== id));
  };

  return (
    <AppShell title="Vote Numbers" subtitle="Budget vote numbers">
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Badge variant="primary">{voteNumbers.length} votes</Badge>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
            <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => {
              setSelectedVote(null);
              setIsEditing(true);
              setShowCreateModal(true);
            }}>Add</Button>
          </div>
        </div>
        <Card>
          <DataTable data={voteNumbers} columns={columns} onRowClick={(row) => { setSelectedVote(row); setIsEditing(false); setShowCreateModal(true); }} getRowId={(row) => row.id} />
        </Card>

        <Modal isOpen={showCreateModal} onClose={() => { setShowCreateModal(false); setSelectedVote(null); setIsEditing(false); }} title={selectedVote ? (isEditing ? "Edit Vote Number" : selectedVote.name) : "New Vote Number"} size="md">
          {selectedVote && !isEditing ? (
            <div className="space-y-2">
              <div><p className="text-[10px] text-secondary-500">Vote Number</p><p className="text-sm font-mono">{selectedVote.number}</p></div>
              <div><p className="text-[10px] text-secondary-500">Department</p><p className="text-sm">{selectedVote.department}</p></div>
              <div><p className="text-[10px] text-secondary-500">Budget Amount</p><p className="text-sm font-bold">R {selectedVote.amount.toLocaleString()}</p></div>
              <div className="flex justify-between gap-2 pt-2 border-t">
                <Button variant="ghost" size="sm" className="text-red-500" onClick={() => { handleDelete(selectedVote.id); setShowCreateModal(false); setSelectedVote(null); }}>Delete</Button>
                <div className="flex gap-2">
                  <Button variant="ghost" size="sm" onClick={() => { setShowCreateModal(false); setSelectedVote(null); }}>Close</Button>
                  <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>Edit</Button>
                </div>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <FormRow cols={2}>
                <Input label="Vote Number" placeholder="e.g., V001" required defaultValue={selectedVote?.number} />
                <Input label="Name" placeholder="Vote name" required defaultValue={selectedVote?.name} />
              </FormRow>
              <FormRow cols={2}>
                <Input label="Amount (R)" type="number" placeholder="Budget amount" defaultValue={selectedVote?.amount} />
                <Select label="Department" options={[{ value: 'infra', label: 'Infrastructure' }, { value: 'comm', label: 'Community' }]} placeholder="Select" />
              </FormRow>
              <div className="flex justify-end gap-2 mt-4">
                <Button variant="ghost" size="sm" onClick={() => { setShowCreateModal(false); setSelectedVote(null); setIsEditing(false); }}>Cancel</Button>
                <Button variant="primary" size="sm">{selectedVote ? "Save Changes" : "Create"}</Button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AppShell>
  );
}
