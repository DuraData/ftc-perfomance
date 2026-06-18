import { useCallback, useEffect, useMemo, useState } from 'react';
import { Plus, Download, Eye, Trash2, FileText, CalendarRange } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, FormRow, FormHero, FormPanel } from '../common/Form';
import {
  statusLabels,
} from '../../data/mockData';
import type { IPMSSubmission, OPMSSubmission, OPMSTarget, IPMSTarget } from '../../types';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';
import { useApp } from '../../context/AppContext';
import {
  applyIpmsSubmissionWorkflowAction,
  applyOpmsSubmissionWorkflowAction,
  createIpmsSubmission,
  createOpmsSubmission,
  deleteIpmsSubmissionAttachment,
  deleteIpmsSubmission,
  deleteOpmsSubmissionAttachment,
  deleteOpmsSubmission,
  extendIpmsSubmissionDueDate,
  extendOpmsSubmissionDueDate,
  getIpmsSubmissionAttachments,
  getIpmsSubmissions,
  getIpmsTargets,
  getOpmsSubmissionAttachments,
  getOpmsSubmissions,
  getOpmsTargets,
  uploadIpmsSubmissionAttachment,
  uploadOpmsSubmissionAttachment,
  updateIpmsSubmission,
  updateOpmsSubmission,
} from '../../api/api';

export function OPMSSubmissionsList() {
  const { pushToast } = useApp();
  const [opmsSubmissions, setOpmsSubmissions] = useState<OPMSSubmission[]>([]);
  const [opmsTargets, setOpmsTargets] = useState<OPMSTarget[]>([]);
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [workflowBusy, setWorkflowBusy] = useState(false);
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

  const loadData = useCallback(async () => {
    setIsLoading(true);
    const [targetsResult, submissionsResult] = await Promise.all([
      getOpmsTargets(),
      getOpmsSubmissions(),
    ]);

    if (targetsResult.success && targetsResult.data) {
      setOpmsTargets(targetsResult.data);
    } else {
      pushToast('error', targetsResult.message ?? 'Failed to load OPMS targets');
    }

    if (submissionsResult.success && submissionsResult.data) {
      setOpmsSubmissions(submissionsResult.data);
    } else {
      pushToast('error', submissionsResult.message ?? 'Failed to load OPMS submissions');
    }

    setIsLoading(false);
  }, [pushToast]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openSubmission = async (submission: OPMSSubmission) => {
    const attachmentsResult = await getOpmsSubmissionAttachments(submission.id);
    const hydrated = {
      ...submission,
      attachments: attachmentsResult.success && attachmentsResult.data ? attachmentsResult.data : submission.attachments,
    };
    setSelectedSubmission(hydrated);
    setOpmsSubmissions(prev => prev.map(item => item.id === submission.id ? hydrated : item));
  };

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
      <button onClick={(e) => { e.stopPropagation(); void openSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          void (async () => {
            const result = await deleteOpmsSubmission(row.id);
            if (result.success) {
              setOpmsSubmissions(prev => prev.filter(item => item.id !== row.id));
              if (selectedSubmission?.id === row.id) setSelectedSubmission(null);
              pushToast('success', 'Submission deleted');
            } else {
              pushToast('error', result.message ?? 'Failed to delete submission');
            }
          })();
        }}
        className="p-1 rounded hover:bg-error-50"
      >
        <Trash2 className="w-3.5 h-3.5 text-error-500" />
      </button>
    </div>
  );

  const handleCreateSubmission = async () => {
    const result = await createOpmsSubmission({
      opmsTargetId: form.targetId,
      quarter: form.quarter,
      actual: Number(form.actual || 0),
      actualDescription: form.actualDescription || null,
      varianceReason: null,
      correctiveMeasure: null,
      dueDate: form.dueDate,
    });

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to create submission');
      return;
    }

    setOpmsSubmissions(prev => [result.data!, ...prev]);
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    await openSubmission(result.data);
  };

  const persistSubmission = async (submission: OPMSSubmission) => {
    const result = await updateOpmsSubmission(submission.id, {
      opmsTargetId: submission.target.id,
      quarter: submission.quarter,
      actual: submission.actual,
      actualDescription: submission.actualDescription ?? null,
      varianceReason: submission.varianceReason ?? null,
      correctiveMeasure: submission.correctiveMeasure ?? null,
      dueDate: submission.dueDate,
    });

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to update submission');
      return;
    }

    setOpmsSubmissions(prev => prev.map(item => item.id === submission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev?.id === submission.id ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', 'Submission updated');
  };

  const runWorkflowAction = async (
    action: Parameters<typeof applyOpmsSubmissionWorkflowAction>[1],
    payload: { comment?: string; score?: number },
  ) => {
    if (!selectedSubmission) return;
    setWorkflowBusy(true);
    const result = await applyOpmsSubmissionWorkflowAction(selectedSubmission.id, action, payload);
    setWorkflowBusy(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? `Failed to ${action} submission`);
      return;
    }

    setOpmsSubmissions(prev => prev.map(item => item.id === selectedSubmission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', `Submission ${action} completed`);
  };

  const extendDueDate = async (payload: { extendedDueDate: string; reason: string }) => {
    if (!selectedSubmission) return;
    setWorkflowBusy(true);
    const result = await extendOpmsSubmissionDueDate(selectedSubmission.id, payload);
    setWorkflowBusy(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to extend due date');
      return;
    }

    setOpmsSubmissions(prev => prev.map(item => item.id === selectedSubmission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', 'Due date extended');
  };

  return (
    <AppShell title="OPMS Submissions" subtitle="All OPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="OPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
          onSave={(updated) => { void persistSubmission(updated as OPMSSubmission); }}
          onDelete={() => {
            void (async () => {
              const result = await deleteOpmsSubmission(selectedSubmission.id);
              if (result.success) {
                setOpmsSubmissions(prev => prev.filter(item => item.id !== selectedSubmission.id));
                setSelectedSubmission(null);
                pushToast('success', 'Submission deleted');
              } else {
                pushToast('error', result.message ?? 'Failed to delete submission');
              }
            })();
          }}
          onAttachmentsChange={(attachments) => {
            const updated = { ...selectedSubmission, attachments };
            setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
            setSelectedSubmission(updated);
          }}
          onUploadAttachments={(files) => {
            void (async () => {
              const results = await Promise.all(files.map(file => uploadOpmsSubmissionAttachment(selectedSubmission.id, file)));
              const uploaded = results.filter(result => result.success && result.data).map(result => result.data!);
              if (uploaded.length > 0) {
                const updated = { ...selectedSubmission, attachments: [...selectedSubmission.attachments, ...uploaded] };
                setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
                setSelectedSubmission(updated);
                pushToast('success', `${uploaded.length} file${uploaded.length === 1 ? '' : 's'} uploaded`);
              }
            })();
          }}
          onDeleteAttachment={(attachmentId) => {
            void (async () => {
              const result = await deleteOpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
              if (result.success) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.filter(item => item.id !== attachmentId) };
                setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
                setSelectedSubmission(updated);
                pushToast('success', 'Attachment deleted');
              } else {
                pushToast('error', result.message ?? 'Failed to delete attachment');
              }
            })();
          }}
          onWorkflowAction={(action, payload) => { void runWorkflowAction(action, payload); }}
          onExtendDueDate={(payload) => { void extendDueDate(payload); }}
          workflowBusy={workflowBusy}
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
            <DataTable
              data={allSubmissions}
              columns={columns}
              onRowClick={(row) => { void openSubmission(row); }}
              actions={actions}
              getRowId={(row) => row.id}
              emptyMessage={isLoading ? 'Loading OPMS submissions...' : 'No OPMS submissions found'}
            />
          </Card>
          <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New OPMS Submission" size="lg">
            <div className="space-y-5">
              <FormHero
                eyebrow="Submission Management"
                title="Create OPMS submission"
                description="Capture the reporting period, target, and actual performance using the standardized add/edit form layout."
                badges={<Badge variant="default">New Submission</Badge>}
              />
              <div className="grid gap-4">
                <FormPanel title="Submission Setup" description="Select the target and reporting period for this OPMS submission." icon={<CalendarRange className="h-5 w-5" />}>
                  <FormRow cols={2}>
                    <Select
                      label="Target"
                      options={opmsTargets.map(target => ({ value: target.id, label: `${target.indicatorNumber} - ${target.targetName}` }))}
                      value={form.targetId}
                      onChange={(e) => setForm(prev => ({ ...prev, targetId: e.target.value }))}
                      required
                    />
                    <Select
                      label="Quarter"
                      options={['Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'].map(value => ({ value, label: value }))}
                      value={form.quarter}
                      onChange={(e) => setForm(prev => ({ ...prev, quarter: e.target.value }))}
                      required
                    />
                  </FormRow>
                  <FormRow cols={3}>
                    <Input label="Due Date" type="date" value={form.dueDate} onChange={(e) => setForm(prev => ({ ...prev, dueDate: e.target.value }))} required />
                    <Input label="Actual" type="number" value={form.actual} onChange={(e) => setForm(prev => ({ ...prev, actual: e.target.value }))} required />
                    <Input label="Variance %" type="number" value={form.variance} onChange={(e) => setForm(prev => ({ ...prev, variance: e.target.value }))} />
                  </FormRow>
                </FormPanel>
                <FormPanel title="Submission Details" description="Provide workflow status and a concise performance narrative." icon={<FileText className="h-5 w-5" />}>
                  <Select
                    label="Status"
                    options={Object.entries(statusLabels).map(([value, label]) => ({ value, label }))}
                    value={form.status}
                    onChange={(e) => setForm(prev => ({ ...prev, status: e.target.value }))}
                    required
                  />
                  <Input label="Performance Description" value={form.actualDescription} onChange={(e) => setForm(prev => ({ ...prev, actualDescription: e.target.value }))} />
                </FormPanel>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" onClick={() => { void handleCreateSubmission(); }}>Create</Button>
            </div>
          </Modal>
        </div>
      )}
    </AppShell>
  );
}

export function IPMSSubmissionsList() {
  const { pushToast } = useApp();
  const [ipmsSubmissions, setIpmsSubmissions] = useState<IPMSSubmission[]>([]);
  const [ipmsTargets, setIpmsTargets] = useState<IPMSTarget[]>([]);
  const [selectedSubmission, setSelectedSubmission] = useState<IPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [workflowBusy, setWorkflowBusy] = useState(false);
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

  const loadData = useCallback(async () => {
    setIsLoading(true);
    const [targetsResult, submissionsResult] = await Promise.all([
      getIpmsTargets(),
      getIpmsSubmissions(),
    ]);

    if (targetsResult.success && targetsResult.data) {
      setIpmsTargets(targetsResult.data);
    } else {
      pushToast('error', targetsResult.message ?? 'Failed to load IPMS targets');
    }

    if (submissionsResult.success && submissionsResult.data) {
      setIpmsSubmissions(submissionsResult.data);
    } else {
      pushToast('error', submissionsResult.message ?? 'Failed to load IPMS submissions');
    }

    setIsLoading(false);
  }, [pushToast]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openSubmission = async (submission: IPMSSubmission) => {
    const attachmentsResult = await getIpmsSubmissionAttachments(submission.id);
    const hydrated = {
      ...submission,
      attachments: attachmentsResult.success && attachmentsResult.data ? attachmentsResult.data : submission.attachments,
    };
    setSelectedSubmission(hydrated);
    setIpmsSubmissions(prev => prev.map(item => item.id === submission.id ? hydrated : item));
  };

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
      <button onClick={(e) => { e.stopPropagation(); void openSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          void (async () => {
            const result = await deleteIpmsSubmission(row.id);
            if (result.success) {
              setIpmsSubmissions(prev => prev.filter(item => item.id !== row.id));
              if (selectedSubmission?.id === row.id) setSelectedSubmission(null);
              pushToast('success', 'Submission deleted');
            } else {
              pushToast('error', result.message ?? 'Failed to delete submission');
            }
          })();
        }}
        className="p-1 rounded hover:bg-error-50"
      >
        <Trash2 className="w-3.5 h-3.5 text-error-500" />
      </button>
    </div>
  );

  const handleCreateSubmission = async () => {
    const result = await createIpmsSubmission({
      ipmsTargetId: form.targetId,
      quarter: form.quarter,
      actual: Number(form.actual || 0),
      actualDescription: form.actualDescription || null,
      varianceReason: null,
      correctiveMeasure: null,
      dueDate: form.dueDate,
    });

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to create submission');
      return;
    }

    setIpmsSubmissions(prev => [result.data!, ...prev]);
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    await openSubmission(result.data);
  };

  const persistSubmission = async (submission: IPMSSubmission) => {
    const result = await updateIpmsSubmission(submission.id, {
      ipmsTargetId: submission.target.id,
      quarter: submission.quarter,
      actual: submission.actual,
      actualDescription: submission.actualDescription ?? null,
      varianceReason: submission.varianceReason ?? null,
      correctiveMeasure: submission.correctiveMeasure ?? null,
      dueDate: submission.dueDate,
    });

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to update submission');
      return;
    }

    setIpmsSubmissions(prev => prev.map(item => item.id === submission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev?.id === submission.id ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', 'Submission updated');
  };

  const runWorkflowAction = async (
    action: Parameters<typeof applyIpmsSubmissionWorkflowAction>[1],
    payload: { comment?: string; score?: number },
  ) => {
    if (!selectedSubmission) return;
    setWorkflowBusy(true);
    const result = await applyIpmsSubmissionWorkflowAction(selectedSubmission.id, action, payload);
    setWorkflowBusy(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? `Failed to ${action} submission`);
      return;
    }

    setIpmsSubmissions(prev => prev.map(item => item.id === selectedSubmission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', `Submission ${action} completed`);
  };

  const extendDueDate = async (payload: { extendedDueDate: string; reason: string }) => {
    if (!selectedSubmission) return;
    setWorkflowBusy(true);
    const result = await extendIpmsSubmissionDueDate(selectedSubmission.id, payload);
    setWorkflowBusy(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to extend due date');
      return;
    }

    setIpmsSubmissions(prev => prev.map(item => item.id === selectedSubmission.id ? { ...result.data!, attachments: item.attachments } : item));
    setSelectedSubmission(prev => prev ? { ...result.data!, attachments: prev.attachments } : prev);
    pushToast('success', 'Due date extended');
  };

  return (
    <AppShell title="IPMS Submissions" subtitle="All IPMS target submissions">
      {selectedSubmission ? (
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="IPMS"
          titlePrefix="Workflow / Verification"
          onBack={() => setSelectedSubmission(null)}
          onSave={(updated) => { void persistSubmission(updated as IPMSSubmission); }}
          onDelete={() => {
            void (async () => {
              const result = await deleteIpmsSubmission(selectedSubmission.id);
              if (result.success) {
                setIpmsSubmissions(prev => prev.filter(item => item.id !== selectedSubmission.id));
                setSelectedSubmission(null);
                pushToast('success', 'Submission deleted');
              } else {
                pushToast('error', result.message ?? 'Failed to delete submission');
              }
            })();
          }}
          onAttachmentsChange={(attachments) => {
            const updated = { ...selectedSubmission, attachments };
            setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
            setSelectedSubmission(updated);
          }}
          onUploadAttachments={(files) => {
            void (async () => {
              const results = await Promise.all(files.map(file => uploadIpmsSubmissionAttachment(selectedSubmission.id, file)));
              const uploaded = results.filter(result => result.success && result.data).map(result => result.data!);
              if (uploaded.length > 0) {
                const updated = { ...selectedSubmission, attachments: [...selectedSubmission.attachments, ...uploaded] };
                setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
                setSelectedSubmission(updated);
                pushToast('success', `${uploaded.length} file${uploaded.length === 1 ? '' : 's'} uploaded`);
              }
            })();
          }}
          onDeleteAttachment={(attachmentId) => {
            void (async () => {
              const result = await deleteIpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
              if (result.success) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.filter(item => item.id !== attachmentId) };
                setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item));
                setSelectedSubmission(updated);
                pushToast('success', 'Attachment deleted');
              } else {
                pushToast('error', result.message ?? 'Failed to delete attachment');
              }
            })();
          }}
          onWorkflowAction={(action, payload) => { void runWorkflowAction(action, payload); }}
          onExtendDueDate={(payload) => { void extendDueDate(payload); }}
          workflowBusy={workflowBusy}
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
            <DataTable
              data={allSubmissions}
              columns={columns}
              onRowClick={(row) => { void openSubmission(row); }}
              actions={actions}
              getRowId={(row) => row.id}
              emptyMessage={isLoading ? 'Loading IPMS submissions...' : 'No IPMS submissions found'}
            />
          </Card>
          <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="New IPMS Submission" size="lg">
            <div className="space-y-5">
              <FormHero
                eyebrow="Submission Management"
                title="Create IPMS submission"
                description="Capture the employee performance reporting details using the same add/edit page design."
                badges={<Badge variant="default">New Submission</Badge>}
              />
              <div className="grid gap-4">
                <FormPanel title="Submission Setup" description="Select the target and reporting period for this IPMS submission." icon={<CalendarRange className="h-5 w-5" />}>
                  <FormRow cols={2}>
                    <Select
                      label="Target"
                      options={ipmsTargets.map(target => ({ value: target.id, label: `${target.indicatorNumber} - ${target.targetName}` }))}
                      value={form.targetId}
                      onChange={(e) => setForm(prev => ({ ...prev, targetId: e.target.value }))}
                      required
                    />
                    <Select
                      label="Quarter"
                      options={['Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'].map(value => ({ value, label: value }))}
                      value={form.quarter}
                      onChange={(e) => setForm(prev => ({ ...prev, quarter: e.target.value }))}
                      required
                    />
                  </FormRow>
                  <FormRow cols={3}>
                    <Input label="Due Date" type="date" value={form.dueDate} onChange={(e) => setForm(prev => ({ ...prev, dueDate: e.target.value }))} required />
                    <Input label="Actual" type="number" value={form.actual} onChange={(e) => setForm(prev => ({ ...prev, actual: e.target.value }))} required />
                    <Input label="Variance %" type="number" value={form.variance} onChange={(e) => setForm(prev => ({ ...prev, variance: e.target.value }))} />
                  </FormRow>
                </FormPanel>
                <FormPanel title="Submission Details" description="Provide workflow status and a concise performance narrative." icon={<FileText className="h-5 w-5" />}>
                  <Select
                    label="Status"
                    options={Object.entries(statusLabels).map(([value, label]) => ({ value, label }))}
                    value={form.status}
                    onChange={(e) => setForm(prev => ({ ...prev, status: e.target.value }))}
                    required
                  />
                  <Input label="Performance Description" value={form.actualDescription} onChange={(e) => setForm(prev => ({ ...prev, actualDescription: e.target.value }))} />
                </FormPanel>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" onClick={() => { void handleCreateSubmission(); }}>Create</Button>
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
    { id: 'status', header: 'Status', accessor: () => <Badge size="sm" variant="success">Active</Badge> },
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
