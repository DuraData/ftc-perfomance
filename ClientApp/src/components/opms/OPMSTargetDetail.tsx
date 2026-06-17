import React, { useEffect, useState } from 'react';
import {
  ArrowLeft,
  Save,
  Edit2,
  FileText,
  Target,
  TrendingUp,
  DollarSign,
  Link2,
  Users,
  Paperclip,
  History,
  Layers,
  CheckCircle,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card, ProgressBar } from '../ui';
import { Tabs, Accordion } from '../common/Tabs';
import { Input, Select, Textarea, FormSection, FormRow } from '../common/Form';
import { FileUpload } from '../common/FileUpload';
import { DataTable } from '../common/DataTable';
import { useApp } from '../../context/AppContext';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';
import {
  deleteOpmsSubmissionAttachment,
  getAuditTrails,
  deleteOpmsSubmission as deleteOpmsSubmissionApi,
  getOpmsSubmissionAttachments,
  getIpmsTargets as getIpmsTargetsApi,
  getOpmsSubmissions as getOpmsSubmissionsApi,
  getOpmsTarget as getOpmsTargetApi,
  uploadOpmsSubmissionAttachment,
  updateOpmsSubmission as updateOpmsSubmissionApi,
} from '../../api/api';
import {
  mockOPMSTargets,
  mockDepartments,
  mockDepartmentUnits,
  mockPeriods,
  mockStrategicGoals,
  mockStrategicObjectives,
  mockBudgetSources,
  mockBudgetTypes,
  mockUnitsOfMeasure,
  mockEmployees,
  targetUnitTypes,
  mockOPMSSubmissions,
  mockVoteNumbers,
  mockIPMSTargets,
} from '../../data/mockData';
import type { OPMSTarget, IPMSTarget, OPMSSubmission, Employee, AuditTrailEntryDto } from '../../types';

interface TargetDetailProps {
  targetId?: string;
}

function GeneralInfoTab({ target }: { target: OPMSTarget }) {
  return (
    <div className="space-y-4 pointer-events-none opacity-80">
      <FormSection title="Basic Information">
        <FormRow cols={3}>
          <Select
            label="Period"
            options={mockPeriods.map(p => ({ value: p.id, label: p.name }))}
            defaultValue={target.period.id}
            disabled
          />
          <Select
            label="Department"
            options={mockDepartments.map(d => ({ value: d.id, label: d.name }))}
            defaultValue={target.department.id}
            disabled
          />
          <Select
            label="Unit"
            options={mockDepartmentUnits.map(u => ({ value: u.id, label: u.name }))}
            defaultValue={target.unit?.id}
            placeholder="Select unit"
            disabled
          />
        </FormRow>
        <FormRow cols={2}>
          <Input
            label="Indicator Number"
            defaultValue={target.indicatorNumber}
            disabled
          />
          <Input
            label="Target Name"
            defaultValue={target.targetName}
            disabled
          />
        </FormRow>
        <Textarea
          label="KPI Description"
          defaultValue={target.kpiDescription}
          rows={2}
          disabled
        />
      </FormSection>

      <FormSection title="Target Unit Configuration">
        <FormRow cols={4}>
          <Select
            label="Target Unit Type"
            options={targetUnitTypes}
            defaultValue={target.targetUnitType}
            disabled
          />
          <Select
            label="Unit of Measure"
            options={mockUnitsOfMeasure.map(u => ({ value: u.id, label: u.name }))}
            defaultValue={target.unitOfMeasure.id}
            disabled
          />
          <Select
            label="KPI Type"
            options={[
              { value: 'quantitative', label: 'Quantitative' },
              { value: 'qualitative', label: 'Qualitative' },
              { value: 'binary', label: 'Binary' },
            ]}
            defaultValue={target.kpiType.toLowerCase()}
            disabled
          />
          <Select
            label="Indicator Type"
            options={[
              { value: 'input', label: 'Input' },
              { value: 'activity', label: 'Activity' },
              { value: 'output', label: 'Output' },
              { value: 'outcome', label: 'Outcome' },
              { value: 'impact', label: 'Impact' },
            ]}
            defaultValue={target.indicatorType.toLowerCase()}
            disabled
          />
        </FormRow>
      </FormSection>

      <FormSection title="Assignment">
        <FormRow cols={2}>
          <Select
            label="Assigned To"
            options={mockEmployees.map(e => ({ value: e.id, label: e.displayName }))}
            defaultValue={target.assignedTo?.id}
            placeholder="Select employee"
            disabled
          />
          <Input
            label="Weight (%)"
            type="number"
            defaultValue={target.weight}
            disabled
          />
        </FormRow>
      </FormSection>

    </div>
  );
}

function StrategyTab({ target }: { target: OPMSTarget }) {
  return (
    <div className="space-y-4 pointer-events-none opacity-80">
      <FormSection title="Strategic Alignment">
        <FormRow cols={2}>
          <Input label="National KPA" defaultValue={target.nationalKPA} />
          <Input label="Municipal KPA" defaultValue={target.municipalKPA} />
        </FormRow>
        <FormRow cols={2}>
          <Select
            label="Strategic Goal"
            options={mockStrategicGoals.map(g => ({ value: g.id, label: g.name }))}
            defaultValue={target.strategicGoal.id}
          />
          <Select
            label="Strategic Objective"
            options={mockStrategicObjectives.map(o => ({ value: o.id, label: o.name }))}
            defaultValue={target.strategicObjective.id}
          />
        </FormRow>
        <FormRow cols={2}>
          <Input label="Performance Objective" defaultValue={target.performanceObjective} />
          <Input label="Functional Area" defaultValue={target.functionalArea} />
        </FormRow>
      </FormSection>

      <FormSection title="References">
        <FormRow cols={3}>
          <Input label="IDP Reference" defaultValue={target.idpReference} />
          <Input label="Internal Reference" defaultValue={target.internalReference} />
          <Input label="FMS Link" defaultValue={target.fmsLink} />
        </FormRow>
        <Input label="Standard Classification" defaultValue={target.standardClassification} />
      </FormSection>
    </div>
  );
}

function QuarterlyTargetsTab({ target }: { target: OPMSTarget }) {
  const quarters = [
    { id: 'q1', label: 'Q1 (Jul-Sep)', targetValue: target.q1Target, description: target.q1Description, budget: target.q1Budget },
    { id: 'q2', label: 'Q2 (Oct-Dec)', targetValue: target.q2Target, description: target.q2Description, budget: target.q2Budget },
    { id: 'mid', label: 'Mid-Year', targetValue: target.midTermTarget, description: target.midTermDescription, budget: target.midTermBudget },
    { id: 'q3', label: 'Q3 (Jan-Mar)', targetValue: target.q3Target, description: target.q3Description, budget: target.q3Budget, revised: target.q3RevisedTarget },
    { id: 'q4', label: 'Q4 (Apr-Jun)', targetValue: target.q4Target, description: target.q4Description, budget: target.q4Budget, revised: target.q4RevisedTarget },
  ];

  return (
    <div className="space-y-4 pointer-events-none opacity-80">
      <FormSection title="Quarterly Breakdown">
        <div className="space-y-2">
          {quarters.map(q => (
            <Card key={q.id} className="p-3">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-secondary-900 dark:text-white">{q.label}</span>
                {q.revised && <Badge variant="warning" size="sm">Revised</Badge>}
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                <Input label="Target" type="number" defaultValue={q.targetValue ?? ''} />
                {q.revised !== undefined && <Input label="Revised" type="number" defaultValue={q.revised ?? ''} />}
                <Input label="Description" defaultValue={q.description ?? ''} />
                <Input label="Budget" type="number" defaultValue={q.budget ?? ''} />
              </div>
            </Card>
          ))}
        </div>
      </FormSection>

      <FormSection title="Annual Summary">
        <div className="grid grid-cols-3 gap-3">
          <Card className="p-3 text-center">
            <p className="text-lg font-bold text-secondary-900 dark:text-white">{target.annualTarget.toLocaleString()}</p>
            <p className="text-xs text-secondary-500">Annual Target</p>
          </Card>
          <Card className="p-3 text-center">
            <p className="text-lg font-bold text-success-600">{target.baseline.toLocaleString()}</p>
            <p className="text-xs text-secondary-500">Baseline</p>
          </Card>
          <Card className="p-3 text-center">
            <p className="text-lg font-bold text-primary-600">{(((target.annualTarget - target.baseline) / Math.max(target.baseline, 1)) * 100).toFixed(1)}%</p>
            <p className="text-xs text-secondary-500">Improvement</p>
          </Card>
        </div>
      </FormSection>
    </div>
  );
}

function BudgetTab({ target }: { target: OPMSTarget }) {
  return (
    <div className="space-y-4 pointer-events-none opacity-80">
      <FormSection title="Budget Information">
        <FormRow cols={2}>
          <Select
            label="Budget Source"
            options={mockBudgetSources.map(b => ({ value: b.id, label: b.name }))}
            defaultValue={target.budgetSource.id}
          />
          <Select
            label="Budget Type"
            options={mockBudgetTypes.map(b => ({ value: b.id, label: b.name }))}
            defaultValue={target.budgetType.id}
          />
        </FormRow>
      </FormSection>

      <FormSection title="Quarterly Budget Allocation">
        <div className="grid grid-cols-4 gap-3">
          {[
            { label: 'Q1 Budget', value: target.q1Budget },
            { label: 'Q2 Budget', value: target.q2Budget },
            { label: 'Q3 Budget', value: target.q3Budget },
            { label: 'Q4 Budget', value: target.q4Budget },
          ].map((q, i) => (
            <div key={i} className="text-center p-2 bg-secondary-50 dark:bg-secondary-800 rounded">
              <p className="text-xs text-secondary-500 mb-0.5">{q.label}</p>
              <p className="text-sm font-semibold text-secondary-900 dark:text-white">R {(q.value ?? 0).toLocaleString()}</p>
            </div>
          ))}
        </div>
        <div className="text-center p-3 bg-primary-50 dark:bg-primary-900/20 rounded mt-2">
          <p className="text-xs text-secondary-500">Total Annual Budget</p>
          <p className="text-xl font-bold text-primary-600">R {((target.q1Budget ?? 0) + (target.q2Budget ?? 0) + (target.q3Budget ?? 0) + (target.q4Budget ?? 0)).toLocaleString()}</p>
        </div>
      </FormSection>
    </div>
  );
}

function SubmissionsTab({
  target,
  submissions,
  onUpdateSubmission,
  onDeleteSubmission,
}: {
  target: OPMSTarget;
  submissions: OPMSSubmission[];
  onUpdateSubmission: (submission: OPMSSubmission) => void;
  onDeleteSubmission: (submissionId: string) => void;
}) {
  const [selectedSubmission, setSelectedSubmission] = useState<OPMSSubmission | null>(null);

  const openSubmission = async (submission: OPMSSubmission) => {
    const attachmentsResult = await getOpmsSubmissionAttachments(submission.id);
    setSelectedSubmission({
      ...submission,
      attachments: attachmentsResult.success && attachmentsResult.data ? attachmentsResult.data : submission.attachments,
    });
  };

  const columns = [
    { id: 'quarter', header: 'Quarter', accessor: (row: OPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: OPMSSubmission) => new Date(row.dueDate).toLocaleDateString() },
    { id: 'actual', header: 'Actual', accessor: (row: OPMSSubmission) => row.actual?.toLocaleString() ?? '-' },
    { id: 'variance', header: 'Var', accessor: (row: OPMSSubmission) => <span className={row.variance && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{row.variance ? `${row.variance > 0 ? '+' : ''}${row.variance}%` : '-'}</span> },
    { id: 'status', header: 'Status', accessor: (row: OPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'pending_verification' ? 'warning' : 'default'}>{row.status.replace('_', ' ')}</Badge> },
  ];

  if (selectedSubmission) {
    return (
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setSelectedSubmission(null)}>
            Back to Submissions
          </Button>
        </div>
        <SubmissionWorkspace
          submission={selectedSubmission}
          submissionType="OPMS"
          onSave={(updated) => {
            onUpdateSubmission(updated as OPMSSubmission);
            setSelectedSubmission(updated as OPMSSubmission);
          }}
          onDelete={() => {
            onDeleteSubmission(selectedSubmission.id);
            setSelectedSubmission(null);
          }}
          onAttachmentsChange={(attachments) => {
            const updated = { ...selectedSubmission, attachments };
            onUpdateSubmission(updated);
            setSelectedSubmission(updated);
          }}
          onUploadAttachments={(files) => {
            void (async () => {
              const results = await Promise.all(files.map(file => uploadOpmsSubmissionAttachment(selectedSubmission.id, file)));
              const uploaded = results.filter(result => result.success && result.data).map(result => result.data!);
              if (uploaded.length > 0) {
                const updated = { ...selectedSubmission, attachments: [...selectedSubmission.attachments, ...uploaded] };
                onUpdateSubmission(updated);
                setSelectedSubmission(updated);
              }
            })();
          }}
          onDeleteAttachment={(attachmentId) => {
            void (async () => {
              const result = await deleteOpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
              if (result.success) {
                const updated = { ...selectedSubmission, attachments: selectedSubmission.attachments.filter(item => item.id !== attachmentId) };
                onUpdateSubmission(updated);
                setSelectedSubmission(updated);
              }
            })();
          }}
        />
      </div>
    );
  }

  return (
    <DataTable 
      data={submissions} 
      columns={columns} 
      emptyMessage="No submissions yet" 
      getRowId={(row) => row.id}
      onRowClick={(row) => { void openSubmission(row); }}
    />
  );
}

function VoteNumbersTab({ target }: { target: OPMSTarget }) {
  const voteNumbers = mockVoteNumbers.filter(v => v.department.id === target.department.id);

  const columns = [
    { id: 'number', header: 'Vote #', accessor: (row: typeof voteNumbers[0]) => row.number },
    { id: 'name', header: 'Name', accessor: (row: typeof voteNumbers[0]) => row.name },
    { id: 'amount', header: 'Amount', accessor: (row: typeof voteNumbers[0]) => `R ${row.amount?.toLocaleString() ?? 'N/A'}` },
  ];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-secondary-600">Vote numbers for {target.department.name}</p>
        <Button variant="outline" size="sm">Add</Button>
      </div>
      <DataTable data={voteNumbers} columns={columns} emptyMessage="No vote numbers" getRowId={(row) => row.id} />
    </div>
  );
}

function RelatedIPMSTab({ ipmsTargets }: { ipmsTargets: IPMSTarget[] }) {
  const columns = [
    { id: 'indicator', header: 'Indicator', accessor: (row: IPMSTarget) => row.indicatorNumber },
    { id: 'name', header: 'Name', accessor: (row: IPMSTarget) => row.targetName },
    { id: 'target', header: 'Target', accessor: (row: IPMSTarget) => `${row.annualTarget} ${row.unitOfMeasure.name}` },
    { id: 'status', header: 'Status', accessor: () => <Badge size="sm" variant="success">Active</Badge> },
  ];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-secondary-600">Linked IPMS targets</p>
        <Button variant="primary" size="sm" icon={<Link2 className="w-3.5 h-3.5" />}>Link</Button>
      </div>
      <DataTable data={ipmsTargets} columns={columns} emptyMessage="No linked IPMS" getRowId={(row) => row.id} />
    </div>
  );
}

function AssigneesTab({ target }: { target: OPMSTarget }) {
  const assignees = mockEmployees.slice(0, 3);

  const columns = [
    { id: 'name', header: 'Name', accessor: (row: Employee) => (
      <div className="flex items-center gap-2">
        <div className="w-6 h-6 rounded-full bg-primary-100 dark:bg-primary-900 flex items-center justify-center">
          <span className="text-[10px] font-medium text-primary-700">{row.firstName[0]}{row.lastName[0]}</span>
        </div>
        <div>
          <p className="text-sm font-medium">{row.displayName}</p>
          <p className="text-[10px] text-secondary-500">{row.email}</p>
        </div>
      </div>
    )},
    { id: 'department', header: 'Department', accessor: (row: Employee) => row.department?.name ?? '-' },
    { id: 'position', header: 'Position', accessor: (row: Employee) => row.position?.title ?? '-' },
  ];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-secondary-600">Additional assignees</p>
        <Button variant="outline" size="sm">Add</Button>
      </div>
      <DataTable data={assignees} columns={columns} emptyMessage="No assignees" getRowId={(row) => row.id} />
    </div>
  );
}

function AttachmentsTab({
  target,
  onAttachmentsChange,
}: {
  target: OPMSTarget;
  onAttachmentsChange: (attachments: OPMSTarget['attachments']) => void;
}) {
  const existingFiles = (target.attachments ?? []).map(file => ({
    id: file.id,
    name: file.fileName,
    size: file.fileSize,
    type: file.fileType,
    progress: 100,
    uploadedAt: file.uploadedAt,
    uploadedBy: file.uploadedBy.displayName,
    documentType: file.documentType,
    url: file.url,
  }));

  return (
    <FileUpload
      existingFiles={existingFiles}
      maxFiles={undefined}
      documentTypes={[{ value: 'strategy', label: 'Strategy' }, { value: 'budget', label: 'Budget' }]}
      onUpload={(files) =>
        onAttachmentsChange([
          ...(target.attachments ?? []),
          ...files.map(file => ({
            id: `att-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
            fileName: file.name,
            fileSize: file.size,
            fileType: file.type,
            uploadedBy: target.assignedTo ?? mockEmployees[0],
            uploadedAt: new Date().toISOString(),
            documentType: 'strategy',
            url: URL.createObjectURL(file),
          })),
        ])
      }
      onRemove={(fileId) => onAttachmentsChange((target.attachments ?? []).filter(file => file.id !== fileId))}
    />
  );
}

function HistoryTab({ entries }: { entries: AuditTrailEntryDto[] }) {
  return (
    <div className="relative">
      <div className="absolute left-3 top-0 bottom-0 w-0.5 bg-secondary-200 dark:bg-secondary-700" />
      {entries.length === 0 && (
        <div className="rounded-lg border border-dashed border-secondary-300 bg-secondary-50 px-4 py-10 text-center text-sm text-secondary-500 dark:border-secondary-700 dark:bg-secondary-800">
          No audit trail entries recorded yet.
        </div>
      )}
      {entries.map((item) => (
        <div key={item.id} className="relative pl-8 pb-3 last:pb-0">
          <div className="absolute left-1.5 w-2.5 h-2.5 bg-primary-600 rounded-full border-2 border-white dark:border-secondary-900" />
          <div className="bg-secondary-50 dark:bg-secondary-800 rounded p-2">
            <div className="flex items-center justify-between mb-1">
              <span className="text-xs font-medium text-secondary-900 dark:text-white">{item.action}</span>
              <span className="text-[10px] text-secondary-500">{new Date(item.changedAt).toLocaleDateString()}</span>
            </div>
            <p className="text-xs text-secondary-600">{item.entityName} {item.entityId}</p>
            <p className="text-[10px] text-secondary-500 mt-0.5">By: {item.changedBy}</p>
          </div>
        </div>
      ))}
    </div>
  );
}

export function OPMSTargetDetail({ targetId = '1' }: TargetDetailProps) {
  const { setCurrentPath } = useApp();
  const [activeTab, setActiveTab] = useState('general');
  const [target, setTarget] = useState<OPMSTarget | null>(null);
  const [opmsSubmissions, setOpmsSubmissions] = useState<OPMSSubmission[]>([]);
  const [ipmsTargets, setIpmsTargets] = useState<IPMSTarget[]>([]);
  const [auditEntries, setAuditEntries] = useState<AuditTrailEntryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);
      const [targetResult, submissionsResult, ipmsTargetsResult] = await Promise.all([
        getOpmsTargetApi(targetId),
        getOpmsSubmissionsApi(),
        getIpmsTargetsApi(),
      ]);

      if (targetResult.success && targetResult.data) {
        setTarget(targetResult.data);
      } else {
        setTarget(null);
      }

      if (submissionsResult.success && submissionsResult.data) {
        setOpmsSubmissions(submissionsResult.data);
      }

      if (ipmsTargetsResult.success && ipmsTargetsResult.data) {
        setIpmsTargets(ipmsTargetsResult.data);
      }
      setIsLoading(false);
    };

    void loadData();
  }, [targetId]);

  useEffect(() => {
    const loadAudit = async () => {
      const auditResult = await getAuditTrails(250);
      if (auditResult.success && auditResult.data) {
        setAuditEntries(auditResult.data.filter(entry =>
          entry.entityId === targetId &&
          entry.entityName.toLowerCase().includes('opmstarget'),
        ));
      }
    };

    void loadAudit();
  }, [targetId]);

  if (isLoading) {
    return (
      <AppShell title="OPMS Target Detail" subtitle="Loading target">
        <Card>
          <p className="text-sm text-secondary-500">Loading target...</p>
        </Card>
      </AppShell>
    );
  }

  if (!target) {
    return (
      <AppShell title="OPMS Target Detail" subtitle="Target not found">
        <Card>
          <p className="text-sm text-secondary-500">Target not found.</p>
        </Card>
      </AppShell>
    );
  }

  const tabs = [
    { id: 'general', label: 'General', icon: <FileText className="w-3.5 h-3.5" /> },
    { id: 'strategy', label: 'Strategy', icon: <Target className="w-3.5 h-3.5" /> },
    { id: 'quarterly', label: 'Quarterly', icon: <TrendingUp className="w-3.5 h-3.5" /> },
    { id: 'budget', label: 'Budget', icon: <DollarSign className="w-3.5 h-3.5" /> },
    { id: 'submissions', label: 'Submissions', icon: <FileText className="w-3.5 h-3.5" />, badge: opmsSubmissions.filter(s => s.target.id === target.id).length },
    { id: 'votes', label: 'Votes', icon: <Layers className="w-3.5 h-3.5" /> },
    { id: 'ipms', label: 'IPMS', icon: <Link2 className="w-3.5 h-3.5" />, badge: ipmsTargets.filter(item => item.relatedOPMSTarget?.id === target.id).length },
    { id: 'assignees', label: 'Assignees', icon: <Users className="w-3.5 h-3.5" /> },
    { id: 'attachments', label: 'Files', icon: <Paperclip className="w-3.5 h-3.5" /> },
    { id: 'history', label: 'Audit', icon: <History className="w-3.5 h-3.5" /> },
  ];

  const renderTabContent = () => {
    switch (activeTab) {
      case 'general': return <GeneralInfoTab target={target} />;
      case 'strategy': return <StrategyTab target={target} />;
      case 'quarterly': return <QuarterlyTargetsTab target={target} />;
      case 'budget': return <BudgetTab target={target} />;
      case 'submissions': return (
        <SubmissionsTab
          target={target}
          submissions={opmsSubmissions.filter(s => s.target.id === target.id)}
          onUpdateSubmission={(submission) => {
            void (async () => {
              const result = await updateOpmsSubmissionApi(submission.id, {
                opmsTargetId: submission.target.id,
                quarter: submission.quarter,
                actual: submission.actual,
                actualDescription: submission.actualDescription ?? null,
                varianceReason: submission.varianceReason ?? null,
                correctiveMeasure: submission.correctiveMeasure ?? null,
                dueDate: submission.dueDate ?? null,
              });
              if (result.success && result.data) {
                setOpmsSubmissions(prev => prev.map(item => item.id === submission.id ? result.data! : item));
              }
            })();
          }}
          onDeleteSubmission={(submissionId) => {
            void (async () => {
              const result = await deleteOpmsSubmissionApi(submissionId);
              if (result.success) {
                setOpmsSubmissions(prev => prev.filter(item => item.id !== submissionId));
              }
            })();
          }}
        />
      );
      case 'votes': return <VoteNumbersTab target={target} />;
      case 'ipms': return <RelatedIPMSTab ipmsTargets={ipmsTargets.filter(item => item.relatedOPMSTarget?.id === target.id)} />;
      case 'assignees': return <AssigneesTab target={target} />;
      case 'attachments': return <AttachmentsTab target={target} onAttachmentsChange={(attachments) => setTarget(prev => prev ? { ...prev, attachments } : prev)} />;
      case 'history': return <HistoryTab entries={auditEntries} />;
      default: return <GeneralInfoTab target={target} />;
    }
  };

  return (
    <AppShell title="OPMS Target Detail" subtitle={target.targetName}>
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button onClick={() => setCurrentPath('/opms/targets')} className="p-1.5 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800">
              <ArrowLeft className="w-4 h-4 text-secondary-500" />
            </button>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-bold text-secondary-900 dark:text-white">{target.indicatorNumber}</h2>
                {target.isRevised ? <Badge size="sm" variant="warning">Revised</Badge> : target.isWithdrawn ? <Badge size="sm" variant="error">Withdrawn</Badge> : <Badge size="sm" variant="success">Active</Badge>}
              </div>
              <p className="text-xs text-secondary-500">{target.targetName}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setCurrentPath('/opms/targets')}>Back To Targets</Button>
            <Button variant="primary" size="sm" onClick={() => setActiveTab('submissions')}>Manage Submissions</Button>
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
          {[
            { label: 'Department', value: target.department.name },
            { label: 'Target', value: target.annualTarget.toLocaleString() },
            { label: 'Baseline', value: target.baseline.toLocaleString() },
            { label: 'Weight', value: `${target.weight}%` },
            { label: 'UoM', value: target.unitOfMeasure.name },
          ].map((stat, i) => (
            <Card key={i} className="p-2 text-center">
              <p className="text-[10px] text-secondary-500">{stat.label}</p>
              <p className="text-sm font-medium text-secondary-900 dark:text-white">{stat.value}</p>
            </Card>
          ))}
        </div>

        {/* Tabs */}
        <Card padding="none">
          <div className="border-b border-secondary-200 dark:border-secondary-700 px-3 py-2 sticky top-0 bg-white dark:bg-secondary-900 z-10">
            <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant="compact" />
          </div>
          <div className="p-4">
            {renderTabContent()}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}
