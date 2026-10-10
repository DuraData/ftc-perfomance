import { useCallback, useEffect, useMemo, useState } from 'react';
import { Plus, Download, Eye, CalendarRange } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { DataTable } from '../common/DataTable';
import { Modal } from '../common/Modal';
import { Input, Select, FormRow, FormHero, FormPanel } from '../common/Form';
import { submissionStatusLabels as statusLabels } from '../submissions/submissionStatus';
import type { IPMSSubmission, OPMSSubmission } from '../../types';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';
import { TargetPicker } from '../common/TargetPicker';
import { useApp } from '../../context/AppContext';
import {
  applyIpmsSubmissionWorkflowAction,
  applyOpmsSubmissionWorkflowAction,
  assessIpmsSubmissionAttachment,
  assessOpmsSubmissionAttachment,
  createIpmsSubmission,
  createOpmsSubmission,
  withdrawIpmsSubmission,
  withdrawOpmsSubmission,
  extendIpmsSubmissionDueDate,
  extendOpmsSubmissionDueDate,
  getIpmsSubmissionsPage,
  getIpmsTarget,
  getOpmsSubmissionsPage,
  getOpmsTarget,
  uploadIpmsSubmissionAttachment,
  uploadOpmsSubmissionAttachment,
  rescanIpmsSubmissionAttachment,
  rescanOpmsSubmissionAttachment,
  replaceIpmsSubmissionAttachment,
  replaceOpmsSubmissionAttachment,
  placeIpmsEvidenceLegalHold,
  placeOpmsEvidenceLegalHold,
  releaseIpmsEvidenceLegalHold,
  releaseOpmsEvidenceLegalHold,
  requestIpmsEvidenceDisposal,
  requestOpmsEvidenceDisposal,
  updateIpmsSubmission,
  updateOpmsSubmission,
  type RegisterPageQuery,
} from '../../api/api';

type SubmissionSetupForm = {
  targetId: string;
  reportingPeriodPublicId: string;
  actualPerformance: string;
};

type SubmissionSetupErrors = Partial<Record<'targetId' | 'reportingPeriodPublicId', string>>;

function validateSubmissionSetup(form: SubmissionSetupForm): SubmissionSetupErrors {
  const errors: SubmissionSetupErrors = {};
  if (!form.targetId.trim()) errors.targetId = 'Select a target before creating the submission.';
  if (!form.reportingPeriodPublicId.trim()) errors.reportingPeriodPublicId = 'Select a reporting period.';
  return errors;
}

function SubmissionPeriodSelect({
  kind,
  targetId,
  value,
  onChange,
  error,
}: {
  kind: 'opms' | 'ipms';
  targetId: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
}) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string>();

  useEffect(() => {
    let cancelled = false;
    if (!targetId) {
      setOptions([]);
      setLoadError(undefined);
      return () => { cancelled = true; };
    }
    const load = async () => {
      setIsLoading(true);
      setLoadError(undefined);
      const result = kind === 'opms' ? await getOpmsTarget(targetId) : await getIpmsTarget(targetId);
      if (cancelled) return;
      if (!result.success || !result.data) {
        setOptions([]);
        setLoadError(result.message ?? 'Unable to load reporting periods for this target.');
      } else {
        setOptions(result.data.periodTargets
          .filter(period => period.isActive)
          .sort((left, right) => left.periodType - right.periodType)
          .map(period => ({ value: period.reportingPeriodPublicId, label: period.periodCode })));
      }
      setIsLoading(false);
    };
    void load();
    return () => { cancelled = true; };
  }, [kind, targetId]);

  return (
    <Select
      label="Reporting period"
      options={[{ value: '', label: targetId ? 'Select reporting period' : 'Select a target first' }, ...options]}
      value={value}
      onChange={event => onChange(event.target.value)}
      disabled={!targetId || isLoading}
      required
      error={error ?? loadError}
    />
  );
}

function formatOptionalDate(value?: string): string {
  if (!value) return '-';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '-' : parsed.toLocaleDateString();
}

function formatVariance(value?: number | null): string {
  if (value === undefined || value === null) return '-';
  return `${value > 0 ? '+' : ''}${value}`;
}

type DashboardSubmissionFilter = 'draft' | 'submitted' | 'returned' | 'approved';

function readDashboardSubmissionScope(): Pick<RegisterPageQuery, 'municipalityFinancialYearPublicId' | 'reportingPeriodPublicId' | 'dashboardFilter'> {
  const parameters = new URLSearchParams(window.location.search);
  const filter = parameters.get('dashboardFilter');
  const dashboardFilter = filter === 'draft' || filter === 'submitted' || filter === 'returned' || filter === 'approved'
    ? filter as DashboardSubmissionFilter
    : undefined;
  return {
    municipalityFinancialYearPublicId: parameters.get('municipalityFinancialYearPublicId') || undefined,
    reportingPeriodPublicId: parameters.get('reportingPeriodPublicId') || undefined,
    dashboardFilter,
  };
}

export function OPMSSubmissionsList() {
  const { pushToast } = useApp();
  const [opmsSubmissions, setOpmsSubmissions] = useState<OPMSSubmission[]>([]);
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [workflowBusy, setWorkflowBusy] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [formErrors, setFormErrors] = useState<SubmissionSetupErrors>({});
  const [form, setForm] = useState({
    targetId: '',
    reportingPeriodPublicId: '',
    actualPerformance: '',
  });
  const allSubmissions = useMemo(() => opmsSubmissions, [opmsSubmissions]);
  const [dashboardScope, setDashboardScope] = useState(readDashboardSubmissionScope);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    const submissionsResult = await getOpmsSubmissionsPage({ page, pageSize: 25, search, sortBy, sortDirection, ...dashboardScope });

    if (submissionsResult.success && submissionsResult.data) {
      setOpmsSubmissions(submissionsResult.data.items);
      setTotalCount(submissionsResult.data.totalCount);
    } else {
      pushToast('error', submissionsResult.message ?? 'Failed to load OPMS submissions');
    }

    setIsLoading(false);
  }, [dashboardScope, page, pushToast, search, sortBy, sortDirection]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openSubmission = (submission: OPMSSubmission) => setSelectedSubmission(submission);

  const resetForm = () => {
    setForm({
      targetId: '',
      reportingPeriodPublicId: '',
      actualPerformance: '',
    });
    setFormErrors({});
  };

  const columns = [
    { id: 'target', sortKey: 'indicatorNumber', header: 'Target', accessor: (row: OPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', sortKey: 'quarter', header: 'Quarter', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => <span className={row.dueDate && new Date(row.dueDate) < new Date() && row.status === 'draft' ? 'text-error-600' : ''}>{formatOptionalDate(row.dueDate)}</span> },
    { id: 'actual', header: 'Actual', accessor: (row: OPMSSubmission) => row.actualPerformance?.trim() || '-' },
    { id: 'variance', header: 'Var', accessor: (row: OPMSSubmission) => <span className={row.variance != null && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{formatVariance(row.variance)}</span> },
    { id: 'status', sortKey: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status.includes('pending') ? 'warning' : 'default'}>{statusLabels[row.status]}</Badge> },
  ];

  const actions = (row: OPMSSubmission) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); void openSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
    </div>
  );

  const handleCreateSubmission = async () => {
    const validationErrors = validateSubmissionSetup(form);
    setFormErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setIsCreating(true);
    const result = await createOpmsSubmission({
      opmsTargetId: form.targetId,
      reportingPeriodPublicId: form.reportingPeriodPublicId,
      actualPerformance: form.actualPerformance.trim() || null,
      varianceReason: null,
      correctiveMeasure: null,
    });
    setIsCreating(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to create submission');
      return;
    }

    setOpmsSubmissions(prev => [result.data!, ...prev].slice(0, 25));
    setTotalCount(count => count + 1);
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    await openSubmission(result.data);
  };

  const persistSubmission = async (submission: OPMSSubmission) => {
    const result = await updateOpmsSubmission(submission.id, {
      opmsTargetId: submission.target.id,
      reportingPeriodPublicId: submission.reportingPeriodPublicId ?? '',
      actualPerformance: submission.actualPerformance?.trim() || null,
      varianceReason: submission.varianceReason ?? null,
      correctiveMeasure: submission.correctiveMeasure ?? null,
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
          onWithdraw={(reason) => {
            void (async () => {
              if (!selectedSubmission.rowVersion) {
                pushToast('error', 'Refresh the submission before withdrawing it.');
                return;
              }
              const result = await withdrawOpmsSubmission(selectedSubmission.id, { reason, rowVersion: selectedSubmission.rowVersion });
              if (result.success && result.data) {
                setOpmsSubmissions(prev => prev.map(item => item.id === result.data!.id ? result.data! : item));
                setSelectedSubmission(result.data);
                pushToast('success', 'Submission withdrawn');
              } else {
                pushToast('error', result.message ?? 'Failed to withdraw submission');
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
          onRescanAttachment={(attachmentId) => {
            void (async () => {
              const result = await rescanOpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
              if (result.success && result.data) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) };
                setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast(result.data.isQuarantined ? 'error' : 'success', result.message ?? 'Evidence scan completed');
              } else pushToast('error', result.message ?? 'Evidence rescan failed');
            })();
          }}
          onAssessAttachment={(attachmentId, outcome, comment) => {
            void (async () => {
              const result = await assessOpmsSubmissionAttachment(selectedSubmission.id, attachmentId, { outcome, comment });
              if (result.success && result.data) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) };
                setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast('success', 'Evidence assessment recorded');
              } else pushToast('error', result.message ?? 'Evidence assessment failed');
            })();
          }}
          onReplaceAttachment={(attachmentId, replacementPublicId, reason, supersededRowVersion, replacementRowVersion) => {
            void (async () => {
              const result = await replaceOpmsSubmissionAttachment(selectedSubmission.id, attachmentId, { replacementEvidencePublicId: replacementPublicId, reason, supersededRowVersion, replacementRowVersion });
              if (result.success && result.data) {
                const attachments = selectedSubmission.attachments.filter(item => item.id !== attachmentId).map(item => item.publicId === replacementPublicId ? result.data! : item);
                const updated = { ...selectedSubmission, attachments };
                setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast('success', 'Evidence replacement recorded');
              } else pushToast('error', result.message ?? 'Evidence replacement failed');
            })();
          }}
          onPlaceAttachmentHold={(attachmentId, holdReference, reason) => {
            void (async () => { const result = await placeOpmsEvidenceLegalHold(selectedSubmission.id, attachmentId, { holdReference, reason }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Legal hold placed'); } else pushToast('error', result.message ?? 'Legal hold failed'); })();
          }}
          onReleaseAttachmentHold={(attachmentId, holdId, reason) => {
            void (async () => { const result = await releaseOpmsEvidenceLegalHold(selectedSubmission.id, attachmentId, holdId, { reason }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Legal hold released'); } else pushToast('error', result.message ?? 'Legal hold release failed'); })();
          }}
          onDisposeAttachment={(attachmentId, approvalReference, reason, rowVersion) => {
            void (async () => { const result = await requestOpmsEvidenceDisposal(selectedSubmission.id, attachmentId, { approvalReference, reason, rowVersion }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setOpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Evidence disposal queued'); } else pushToast('error', result.message ?? 'Evidence disposal request failed'); })();
          }}
          onWorkflowAction={(action, payload) => { void runWorkflowAction(action, payload); }}
          onExtendDueDate={(payload) => { void extendDueDate(payload); }}
          workflowBusy={workflowBusy}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Badge variant="primary">{totalCount} submissions</Badge>
              {dashboardScope.dashboardFilter ? <Badge variant="info">Dashboard filter: {dashboardScope.dashboardFilter}</Badge> : null}
              {dashboardScope.dashboardFilter ? <Button size="sm" variant="ghost" onClick={() => {
                window.history.replaceState({}, '', '/opms/submissions');
                setDashboardScope({});
              }}>Clear dashboard filter</Button> : null}
            </div>
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
              searchable
              searchPlaceholder="Search OPMS submissions..."
              serverState={{
                page,
                pageSize: 25,
                totalCount,
                search: searchInput,
                sortBy,
                sortDirection,
                onPageChange: setPage,
                onSearchChange: setSearchInput,
                onSortChange: (nextSort, nextDirection) => { setPage(1); setSortBy(nextSort); setSortDirection(nextDirection); },
              }}
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
                    <TargetPicker
                      kind="opms"
                      label="Target"
                      value={form.targetId}
                      onChange={(value) => {
                        setForm(prev => ({ ...prev, targetId: value, reportingPeriodPublicId: '' }));
                        setFormErrors(prev => ({ ...prev, targetId: undefined, reportingPeriodPublicId: undefined }));
                      }}
                      required
                      error={formErrors.targetId}
                    />
                    <SubmissionPeriodSelect kind="opms" targetId={form.targetId} value={form.reportingPeriodPublicId} onChange={(value) => {
                      setForm(prev => ({ ...prev, reportingPeriodPublicId: value }));
                      setFormErrors(prev => ({ ...prev, reportingPeriodPublicId: undefined }));
                    }} error={formErrors.reportingPeriodPublicId} />
                  </FormRow>
                  <Input label="Actual Performance" value={form.actualPerformance} onChange={(e) => setForm(prev => ({ ...prev, actualPerformance: e.target.value }))} helpText="Optional while the submission remains in progress; required validation is enforced when it is submitted." />
                </FormPanel>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" loading={isCreating} onClick={() => { void handleCreateSubmission(); }}>Create</Button>
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
  const [selectedSubmission, setSelectedSubmission] = useState<IPMSSubmission | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [workflowBusy, setWorkflowBusy] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [formErrors, setFormErrors] = useState<SubmissionSetupErrors>({});
  const [form, setForm] = useState({
    targetId: '',
    reportingPeriodPublicId: '',
    actualPerformance: '',
  });
  const allSubmissions = useMemo(() => ipmsSubmissions, [ipmsSubmissions]);
  const [dashboardScope, setDashboardScope] = useState(readDashboardSubmissionScope);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    const submissionsResult = await getIpmsSubmissionsPage({ page, pageSize: 25, search, sortBy, sortDirection, ...dashboardScope });

    if (submissionsResult.success && submissionsResult.data) {
      setIpmsSubmissions(submissionsResult.data.items);
      setTotalCount(submissionsResult.data.totalCount);
    } else {
      pushToast('error', submissionsResult.message ?? 'Failed to load IPMS submissions');
    }

    setIsLoading(false);
  }, [dashboardScope, page, pushToast, search, sortBy, sortDirection]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openSubmission = (submission: IPMSSubmission) => setSelectedSubmission(submission);

  const resetForm = () => {
    setForm({
      targetId: '',
      reportingPeriodPublicId: '',
      actualPerformance: '',
    });
    setFormErrors({});
  };

  const columns = [
    { id: 'target', sortKey: 'indicatorNumber', header: 'Target', accessor: (row: IPMSSubmission) => <div><p className="font-medium">{row.target.targetName}</p><p className="text-[10px] text-secondary-500">{row.target.indicatorNumber}</p></div> },
    { id: 'quarter', sortKey: 'quarter', header: 'Quarter', accessor: (row: IPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: IPMSSubmission) => formatOptionalDate(row.dueDate) },
    { id: 'actual', header: 'Actual', accessor: (row: IPMSSubmission) => row.actualPerformance?.trim() || '-' },
    { id: 'variance', header: 'Var', accessor: (row: IPMSSubmission) => <span className={row.variance != null && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{formatVariance(row.variance)}</span> },
    { id: 'status', sortKey: 'status', header: 'Status', accessor: (row: IPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status.includes('pending') ? 'warning' : 'default'}>{statusLabels[row.status]}</Badge> },
  ];

  const actions = (row: IPMSSubmission) => (
    <div className="flex items-center justify-end gap-0.5">
      <button onClick={(e) => { e.stopPropagation(); void openSubmission(row); }} className="p-1 rounded hover:bg-secondary-100"><Eye className="w-3.5 h-3.5 text-secondary-400" /></button>
    </div>
  );

  const handleCreateSubmission = async () => {
    const validationErrors = validateSubmissionSetup(form);
    setFormErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setIsCreating(true);
    const result = await createIpmsSubmission({
      ipmsTargetId: form.targetId,
      reportingPeriodPublicId: form.reportingPeriodPublicId,
      actualPerformance: form.actualPerformance.trim() || null,
      varianceReason: null,
      correctiveMeasure: null,
    });
    setIsCreating(false);

    if (!result.success || !result.data) {
      pushToast('error', result.message ?? 'Failed to create submission');
      return;
    }

    setIpmsSubmissions(prev => [result.data!, ...prev].slice(0, 25));
    setTotalCount(count => count + 1);
    pushToast('success', 'Submission created');
    setShowCreateModal(false);
    await openSubmission(result.data);
  };

  const persistSubmission = async (submission: IPMSSubmission) => {
    const result = await updateIpmsSubmission(submission.id, {
      ipmsTargetId: submission.target.id,
      reportingPeriodPublicId: submission.reportingPeriodPublicId ?? '',
      actualPerformance: submission.actualPerformance?.trim() || null,
      varianceReason: submission.varianceReason ?? null,
      correctiveMeasure: submission.correctiveMeasure ?? null,
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
          onWithdraw={(reason) => {
            void (async () => {
              if (!selectedSubmission.rowVersion) {
                pushToast('error', 'Refresh the submission before withdrawing it.');
                return;
              }
              const result = await withdrawIpmsSubmission(selectedSubmission.id, { reason, rowVersion: selectedSubmission.rowVersion });
              if (result.success && result.data) {
                setIpmsSubmissions(prev => prev.map(item => item.id === result.data!.id ? result.data! : item));
                setSelectedSubmission(result.data);
                pushToast('success', 'Submission withdrawn');
              } else {
                pushToast('error', result.message ?? 'Failed to withdraw submission');
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
          onRescanAttachment={(attachmentId) => {
            void (async () => {
              const result = await rescanIpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
              if (result.success && result.data) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) };
                setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast(result.data.isQuarantined ? 'error' : 'success', result.message ?? 'Evidence scan completed');
              } else pushToast('error', result.message ?? 'Evidence rescan failed');
            })();
          }}
          onAssessAttachment={(attachmentId, outcome, comment) => {
            void (async () => {
              const result = await assessIpmsSubmissionAttachment(selectedSubmission.id, attachmentId, { outcome, comment });
              if (result.success && result.data) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) };
                setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast('success', 'Evidence assessment recorded');
              } else pushToast('error', result.message ?? 'Evidence assessment failed');
            })();
          }}
          onReplaceAttachment={(attachmentId, replacementPublicId, reason, supersededRowVersion, replacementRowVersion) => {
            void (async () => {
              const result = await replaceIpmsSubmissionAttachment(selectedSubmission.id, attachmentId, { replacementEvidencePublicId: replacementPublicId, reason, supersededRowVersion, replacementRowVersion });
              if (result.success && result.data) {
                const attachments = selectedSubmission.attachments.filter(item => item.id !== attachmentId).map(item => item.publicId === replacementPublicId ? result.data! : item);
                const updated = { ...selectedSubmission, attachments };
                setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated);
                pushToast('success', 'Evidence replacement recorded');
              } else pushToast('error', result.message ?? 'Evidence replacement failed');
            })();
          }}
          onPlaceAttachmentHold={(attachmentId, holdReference, reason) => {
            void (async () => { const result = await placeIpmsEvidenceLegalHold(selectedSubmission.id, attachmentId, { holdReference, reason }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Legal hold placed'); } else pushToast('error', result.message ?? 'Legal hold failed'); })();
          }}
          onReleaseAttachmentHold={(attachmentId, holdId, reason) => {
            void (async () => { const result = await releaseIpmsEvidenceLegalHold(selectedSubmission.id, attachmentId, holdId, { reason }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Legal hold released'); } else pushToast('error', result.message ?? 'Legal hold release failed'); })();
          }}
          onDisposeAttachment={(attachmentId, approvalReference, reason, rowVersion) => {
            void (async () => { const result = await requestIpmsEvidenceDisposal(selectedSubmission.id, attachmentId, { approvalReference, reason, rowVersion }); if (result.success && result.data) { const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.map(item => item.id === attachmentId ? result.data! : item) }; setIpmsSubmissions(prev => prev.map(item => item.id === updated.id ? updated : item)); setSelectedSubmission(updated); pushToast('success', 'Evidence disposal queued'); } else pushToast('error', result.message ?? 'Evidence disposal request failed'); })();
          }}
          onWorkflowAction={(action, payload) => { void runWorkflowAction(action, payload); }}
          onExtendDueDate={(payload) => { void extendDueDate(payload); }}
          workflowBusy={workflowBusy}
        />
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Badge variant="primary">{totalCount} submissions</Badge>
              {dashboardScope.dashboardFilter ? <Badge variant="info">Dashboard filter: {dashboardScope.dashboardFilter}</Badge> : null}
              {dashboardScope.dashboardFilter ? <Button size="sm" variant="ghost" onClick={() => {
                window.history.replaceState({}, '', '/ipms/submissions');
                setDashboardScope({});
              }}>Clear dashboard filter</Button> : null}
            </div>
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
              searchable
              searchPlaceholder="Search IPMS submissions..."
              serverState={{
                page,
                pageSize: 25,
                totalCount,
                search: searchInput,
                sortBy,
                sortDirection,
                onPageChange: setPage,
                onSearchChange: setSearchInput,
                onSortChange: (nextSort, nextDirection) => { setPage(1); setSortBy(nextSort); setSortDirection(nextDirection); },
              }}
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
                    <TargetPicker
                      kind="ipms"
                      label="Target"
                      value={form.targetId}
                      onChange={(value) => {
                        setForm(prev => ({ ...prev, targetId: value, reportingPeriodPublicId: '' }));
                        setFormErrors(prev => ({ ...prev, targetId: undefined, reportingPeriodPublicId: undefined }));
                      }}
                      required
                      error={formErrors.targetId}
                    />
                    <SubmissionPeriodSelect kind="ipms" targetId={form.targetId} value={form.reportingPeriodPublicId} onChange={(value) => {
                      setForm(prev => ({ ...prev, reportingPeriodPublicId: value }));
                      setFormErrors(prev => ({ ...prev, reportingPeriodPublicId: undefined }));
                    }} error={formErrors.reportingPeriodPublicId} />
                  </FormRow>
                  <Input label="Actual Performance" value={form.actualPerformance} onChange={(e) => setForm(prev => ({ ...prev, actualPerformance: e.target.value }))} helpText="Optional while the submission remains in progress; required validation is enforced when it is submitted." />
                </FormPanel>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2 border-t border-secondary-200 pt-4 dark:border-secondary-700">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>Cancel</Button>
              <Button variant="primary" size="sm" loading={isCreating} onClick={() => { void handleCreateSubmission(); }}>Create</Button>
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
