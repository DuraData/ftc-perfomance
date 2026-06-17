import { useMemo, useState } from 'react';
import { Plus, Download, Eye, Trash2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, FormRow } from '../common/Form';
import {
  statusLabels,
} from '../../data/mockData';
import type { IPMSSubmission, OPMSSubmission } from '../../types';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';
import { useApp } from '../../context/AppContext';

export function OPMSSubmissionsList() {
  const {
    opmsSubmissions,
    opmsTargets,
    createOPMSSubmission,
    updateOPMSSubmission,
    deleteOPMSSubmission,
    pushToast,
  } = useApp();
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [form, setForm] = useState({
    targetId: '',
    quarter: 'Q1',
    dueDate: new Date().toISOString().slice(0, 10),
    actual: '0',
    variance: '0',
    status: 'draft',
    actualDescription: '',
  });
  const allSubmissions = useMemo(() => opmsSubmissions, [opmsSubmissions]);

  const resetForm = () => {
    setForm({
      targetId: opmsTargets[0]?.id ?? '',
      quarter: 'Q1',
      dueDate: new Date().toISOString().slice(0, 10),
      actual: '0',
      variance: '0',
      status: 'draft',
      actualDescription: '',
    });
  };

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
      <button
        onClick={(e) => {
          e.stopPropagation();
          deleteOPMSSubmission(row.id);
          if (selectedSubmission?.id === row.id) setSelectedSubmission(null);
          pushToast('success', 'Submission deleted');
        }}
        className="p-1 rounded hover:bg-error-50"
      >
        <Trash2 className="w-3.5 h-3.5 text-error-500" />
      </button>
    </div>
  );

  const handleCreateSubmission = () => {
    const target = opmsTargets.find(item => item.id === form.targetId) ?? opmsTargets[0];
    const template = allSubmissions[0];
    if (!target || !template) return;

    const created = createOPMSSubmission({
      ...template,
      id: '',
      target,
      quarter: form.quarter as OPMSSubmission['quarter'],
      dueDate: form.dueDate,
      actual: Number(form.actual || 0),
      variance: Number(form.variance || 0),
      actualDescription: form.actualDescription,
      status: form.status as OPMSSubmission['status'],
      attachments: [],
      comments: [],
      history: [],
      submittedAt: undefined,
      verifiedAt: undefined,
      approvedAt: undefined,
      auditedAt: undefined,
      pmsReviewedAt: undefined,
    });
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    setSelectedSubmission(created);
  };

  return (
    <AppShell title="OPMS Submissions" subtitle="All OPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="OPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
          onSave={(updated) => {
            updateOPMSSubmission(updated as OPMSSubmission);
            setSelectedSubmission(updated as OPMSSubmission);
            pushToast('success', 'Submission updated');
          }}
          onDelete={() => {
            deleteOPMSSubmission(selectedSubmission.id);
            setSelectedSubmission(null);
            pushToast('success', 'Submission deleted');
          }}
          onAttachmentsChange={(attachments) => {
            const updated = { ...selectedSubmission, attachments };
            updateOPMSSubmission(updated);
            setSelectedSubmission(updated);
          }}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Badge variant="primary">{allSubmissions.length} submissions</Badge>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
              <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => { resetForm(); setShowCreateModal(true); }}>New Submission</Button>
            </div>
          </div>
          <Card>
            <DataTable data={allSubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} actions={actions} getRowId={(row) => row.id} />
          </Card>
          <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New OPMS Submission" size="lg">
            <div className="space-y-3">
              <FormRow cols={2}>
                <Select
                  label="Target"
                  options={opmsTargets.map(target => ({ value: target.id, label: `${target.indicatorNumber} - ${target.targetName}` }))}
                  value={form.targetId}
                  onChange={(e) => setForm(prev => ({ ...prev, targetId: e.target.value }))}
                />
                <Select
                  label="Quarter"
                  options={['Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'].map(value => ({ value, label: value }))}
                  value={form.quarter}
                  onChange={(e) => setForm(prev => ({ ...prev, quarter: e.target.value }))}
                />
              </FormRow>
              <FormRow cols={3}>
                <Input label="Due Date" type="date" value={form.dueDate} onChange={(e) => setForm(prev => ({ ...prev, dueDate: e.target.value }))} />
                <Input label="Actual" type="number" value={form.actual} onChange={(e) => setForm(prev => ({ ...prev, actual: e.target.value }))} />
                <Input label="Variance %" type="number" value={form.variance} onChange={(e) => setForm(prev => ({ ...prev, variance: e.target.value }))} />
              </FormRow>
              <Select
                label="Status"
                options={Object.entries(statusLabels).map(([value, label]) => ({ value, label }))}
                value={form.status}
                onChange={(e) => setForm(prev => ({ ...prev, status: e.target.value }))}
              />
              <Input label="Performance Description" value={form.actualDescription} onChange={(e) => setForm(prev => ({ ...prev, actualDescription: e.target.value }))} />
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" onClick={handleCreateSubmission}>Create</Button>
            </div>
          </Modal>
        </div>
      )}
    </AppShell>
  );
}

export function IPMSSubmissionsList() {
  const {
    ipmsSubmissions,
    ipmsTargets,
    createIPMSSubmission,
    updateIPMSSubmission,
    deleteIPMSSubmission,
    pushToast,
  } = useApp();
  const [selectedSubmission, setSelectedSubmission] = useState<IPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [form, setForm] = useState({
    targetId: '',
    quarter: 'Q1',
    dueDate: new Date().toISOString().slice(0, 10),
    actual: '0',
    variance: '0',
    status: 'draft',
    actualDescription: '',
  });
  const allSubmissions = useMemo(() => ipmsSubmissions, [ipmsSubmissions]);

  const resetForm = () => {
    setForm({
      targetId: ipmsTargets[0]?.id ?? '',
      quarter: 'Q1',
      dueDate: new Date().toISOString().slice(0, 10),
      actual: '0',
      variance: '0',
      status: 'draft',
      actualDescription: '',
    });
  };

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
      <button
        onClick={(e) => {
          e.stopPropagation();
          deleteIPMSSubmission(row.id);
          if (selectedSubmission?.id === row.id) setSelectedSubmission(null);
          pushToast('success', 'Submission deleted');
        }}
        className="p-1 rounded hover:bg-error-50"
      >
        <Trash2 className="w-3.5 h-3.5 text-error-500" />
      </button>
    </div>
  );

  const handleCreateSubmission = () => {
    const target = ipmsTargets.find(item => item.id === form.targetId) ?? ipmsTargets[0];
    const template = allSubmissions[0];
    if (!target || !template) return;

    const created = createIPMSSubmission({
      ...template,
      id: '',
      target,
      quarter: form.quarter as IPMSSubmission['quarter'],
      dueDate: form.dueDate,
      actual: Number(form.actual || 0),
      variance: Number(form.variance || 0),
      actualDescription: form.actualDescription,
      status: form.status as IPMSSubmission['status'],
      attachments: [],
      history: [],
      submittedAt: undefined,
      verifiedAt: undefined,
      approvedAt: undefined,
      auditedAt: undefined,
    });
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    setSelectedSubmission(created);
  };

  return (
    <AppShell title="IPMS Submissions" subtitle="All IPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="IPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
          onSave={(updated) => {
            updateIPMSSubmission(updated as IPMSSubmission);
            setSelectedSubmission(updated as IPMSSubmission);
            pushToast('success', 'Submission updated');
          }}
          onDelete={() => {
            deleteIPMSSubmission(selectedSubmission.id);
            setSelectedSubmission(null);
            pushToast('success', 'Submission deleted');
          }}
          onAttachmentsChange={(attachments) => {
            const updated = { ...selectedSubmission, attachments };
            updateIPMSSubmission(updated);
            setSelectedSubmission(updated);
          }}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Badge variant="primary">{allSubmissions.length} submissions</Badge>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" icon={<Download className="w-3.5 h-3.5" />}>Export</Button>
              <Button variant="primary" size="sm" icon={<Plus className="w-3.5 h-3.5" />} onClick={() => { resetForm(); setShowCreateModal(true); }}>New Submission</Button>
            </div>
          </div>
          <Card>
            <DataTable data={allSubmissions} columns={columns} onRowClick={(row) => setSelectedSubmission(row)} actions={actions} getRowId={(row) => row.id} />
          </Card>
          <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New IPMS Submission" size="lg">
            <div className="space-y-3">
              <FormRow cols={2}>
                <Select
                  label="Target"
                  options={ipmsTargets.map(target => ({ value: target.id, label: `${target.indicatorNumber} - ${target.targetName}` }))}
                  value={form.targetId}
                  onChange={(e) => setForm(prev => ({ ...prev, targetId: e.target.value }))}
                />
                <Select
                  label="Quarter"
                  options={['Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'].map(value => ({ value, label: value }))}
                  value={form.quarter}
                  onChange={(e) => setForm(prev => ({ ...prev, quarter: e.target.value }))}
                />
              </FormRow>
              <FormRow cols={3}>
                <Input label="Due Date" type="date" value={form.dueDate} onChange={(e) => setForm(prev => ({ ...prev, dueDate: e.target.value }))} />
                <Input label="Actual" type="number" value={form.actual} onChange={(e) => setForm(prev => ({ ...prev, actual: e.target.value }))} />
                <Input label="Variance %" type="number" value={form.variance} onChange={(e) => setForm(prev => ({ ...prev, variance: e.target.value }))} />
              </FormRow>
              <Select
                label="Status"
                options={Object.entries(statusLabels).map(([value, label]) => ({ value, label }))}
                value={form.status}
                onChange={(e) => setForm(prev => ({ ...prev, status: e.target.value }))}
              />
              <Input label="Performance Description" value={form.actualDescription} onChange={(e) => setForm(prev => ({ ...prev, actualDescription: e.target.value }))} />
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" onClick={handleCreateSubmission}>Create</Button>
            </div>
          </Modal>
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
