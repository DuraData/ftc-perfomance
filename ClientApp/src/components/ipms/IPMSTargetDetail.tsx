import { useEffect, useState } from 'react';
import {
  ArrowLeft,
  FileText,
  Target,
  TrendingUp,
  Paperclip,
  History,
} from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Button, Badge, Card } from '../ui';
import { Tabs } from '../common/Tabs';
import { Input, Select, Textarea, FormSection, FormRow } from '../common/Form';
import { FileUpload } from '../common/FileUpload';
import { DataTable } from '../common/DataTable';
import { useApp } from '../../context/AppContext';
import { SubmissionWorkspace } from '../submissions/SubmissionWorkspace';
import {
  deleteIpmsSubmissionAttachment,
  getAuditTrails,
  deleteIpmsSubmission as deleteIpmsSubmissionApi,
  getIpmsSubmissionAttachments,
  getIpmsSubmissions as getIpmsSubmissionsApi,
  getIpmsTarget as getIpmsTargetApi,
  uploadIpmsSubmissionAttachment,
  updateIpmsSubmission as updateIpmsSubmissionApi,
} from '../../api/api';
import {
  mockDepartments,
  mockDepartmentUnits,
  mockPeriods,
  mockStrategicGoals,
  mockStrategicObjectives,
  mockUnitsOfMeasure,
  mockEmployees,
  targetUnitTypes,
} from '../../data/mockData';
import type { IPMSTarget, IPMSSubmission, AuditTrailEntryDto } from '../../types';

interface TargetDetailProps {
  targetId?: string;
}

function GeneralInfoTab({ target }: { target: IPMSTarget }) {
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

function StrategyTab({ target }: { target: IPMSTarget }) {
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
        {target.relatedOPMSTarget && (
          <div className="mt-3 p-3 border border-secondary-200 rounded-lg bg-secondary-50">
            <p className="text-xs font-semibold text-secondary-600 mb-1">Linked OPMS Target</p>
            <p className="text-sm font-medium">{target.relatedOPMSTarget.indicatorNumber} - {target.relatedOPMSTarget.targetName}</p>
          </div>
        )}
      </FormSection>
    </div>
  );
}

function QuarterlyTargetsTab({ target }: { target: IPMSTarget }) {
  const quarters = [
    { id: 'q1', label: 'Q1 (Jul-Sep)', targetValue: target.q1Target },
    { id: 'q2', label: 'Q2 (Oct-Dec)', targetValue: target.q2Target },
    { id: 'mid', label: 'Mid-Year', targetValue: target.midTermTarget },
    { id: 'q3', label: 'Q3 (Jan-Mar)', targetValue: target.q3Target },
    { id: 'q4', label: 'Q4 (Apr-Jun)', targetValue: target.q4Target },
  ];

  return (
    <div className="space-y-4 pointer-events-none opacity-80">
      <FormSection title="Quarterly Breakdown">
        <div className="space-y-2">
          {quarters.map(q => (
            <Card key={q.id} className="p-3">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-secondary-900 dark:text-white">{q.label}</span>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <Input label="Target" type="number" defaultValue={q.targetValue ?? ''} />
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

function SubmissionsTab({
  submissions,
  onUpdateSubmission,
  onDeleteSubmission,
}: {
  submissions: IPMSSubmission[];
  onUpdateSubmission: (submission: IPMSSubmission) => void;
  onDeleteSubmission: (submissionId: string) => void;
}) {
  const [selectedSubmission, setSelectedSubmission] = useState<IPMSSubmission | null>(null);

  const openSubmission = async (submission: IPMSSubmission) => {
    const attachmentsResult = await getIpmsSubmissionAttachments(submission.id);
    setSelectedSubmission({
      ...submission,
      attachments: attachmentsResult.success && attachmentsResult.data ? attachmentsResult.data : submission.attachments,
    });
  };

  const columns = [
    { id: 'quarter', header: 'Quarter', accessor: (row: IPMSSubmission) => row.quarter },
    { id: 'due', header: 'Due', accessor: (row: IPMSSubmission) => new Date(row.dueDate).toLocaleDateString() },
    { id: 'actual', header: 'Actual', accessor: (row: IPMSSubmission) => row.actual?.toLocaleString() ?? '-' },
    { id: 'variance', header: 'Var', accessor: (row: IPMSSubmission) => <span className={row.variance && row.variance < 0 ? 'text-error-600' : 'text-success-600'}>{row.variance ? `${row.variance > 0 ? '+' : ''}${row.variance}%` : '-'}</span> },
    { id: 'status', header: 'Status', accessor: (row: IPMSSubmission) => <Badge size="sm" variant={row.status === 'approved' ? 'success' : row.status === 'pending_verification' ? 'warning' : 'default'}>{row.status.replace('_', ' ')}</Badge> },
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
          submissionType="IPMS"
          onSave={(updated) => {
            onUpdateSubmission(updated as IPMSSubmission);
            setSelectedSubmission(updated as IPMSSubmission);
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
              const results = await Promise.all(files.map(file => uploadIpmsSubmissionAttachment(selectedSubmission.id, file)));
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
              const result = await deleteIpmsSubmissionAttachment(selectedSubmission.id, attachmentId);
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

function AttachmentsTab({
  target,
  onAttachmentsChange,
}: {
  target: IPMSTarget;
  onAttachmentsChange: (attachments: IPMSTarget['attachments']) => void;
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

export function IPMSTargetDetail({ targetId = '1' }: TargetDetailProps) {
  const { setCurrentPath } = useApp();
  const [activeTab, setActiveTab] = useState('general');
  const [target, setTarget] = useState<IPMSTarget | null>(null);
  const [ipmsSubmissions, setIpmsSubmissions] = useState<IPMSSubmission[]>([]);
  const [auditEntries, setAuditEntries] = useState<AuditTrailEntryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);
      const [targetResult, submissionsResult] = await Promise.all([
        getIpmsTargetApi(targetId),
        getIpmsSubmissionsApi(),
      ]);

      if (targetResult.success && targetResult.data) {
        setTarget(targetResult.data);
      } else {
        setTarget(null);
      }

      if (submissionsResult.success && submissionsResult.data) {
        setIpmsSubmissions(submissionsResult.data);
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
          entry.entityName.toLowerCase().includes('ipmstarget'),
        ));
      }
    };

    void loadAudit();
  }, [targetId]);

  if (isLoading) {
    return (
      <AppShell title="IPMS Target Detail" subtitle="Loading target">
        <Card>
          <p className="text-sm text-secondary-500">Loading target...</p>
        </Card>
      </AppShell>
    );
  }

  if (!target) {
    return (
      <AppShell title="IPMS Target Detail" subtitle="Target not found">
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
    { id: 'submissions', label: 'Submissions', icon: <FileText className="w-3.5 h-3.5" />, badge: ipmsSubmissions.filter(s => s.target.id === target.id).length },
    { id: 'attachments', label: 'Files', icon: <Paperclip className="w-3.5 h-3.5" /> },
    { id: 'history', label: 'Audit', icon: <History className="w-3.5 h-3.5" /> },
  ];

  const renderTabContent = () => {
    switch (activeTab) {
      case 'general': return <GeneralInfoTab target={target} />;
      case 'strategy': return <StrategyTab target={target} />;
      case 'quarterly': return <QuarterlyTargetsTab target={target} />;
      case 'submissions': return (
        <SubmissionsTab
          submissions={ipmsSubmissions.filter(s => s.target.id === target.id)}
          onUpdateSubmission={(submission) => {
            void (async () => {
              const result = await updateIpmsSubmissionApi(submission.id, {
                ipmsTargetId: submission.target.id,
                quarter: submission.quarter,
                actual: submission.actual,
                actualDescription: submission.actualDescription ?? null,
                varianceReason: submission.varianceReason ?? null,
                correctiveMeasure: submission.correctiveMeasure ?? null,
                dueDate: submission.dueDate ?? null,
              });
              if (result.success && result.data) {
                setIpmsSubmissions(prev => prev.map(item => item.id === submission.id ? result.data! : item));
              }
            })();
          }}
          onDeleteSubmission={(submissionId) => {
            void (async () => {
              const result = await deleteIpmsSubmissionApi(submissionId);
              if (result.success) {
                setIpmsSubmissions(prev => prev.filter(item => item.id !== submissionId));
              }
            })();
          }}
        />
      );
      case 'attachments': return <AttachmentsTab target={target} onAttachmentsChange={(attachments) => setTarget(prev => prev ? { ...prev, attachments } : prev)} />;
      case 'history': return <HistoryTab entries={auditEntries} />;
      default: return <GeneralInfoTab target={target} />;
    }
  };

  return (
    <AppShell title="IPMS Target Detail" subtitle={target.targetName}>
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button onClick={() => setCurrentPath('/ipms/targets')} className="p-1.5 rounded hover:bg-secondary-100 dark:hover:bg-secondary-800">
              <ArrowLeft className="w-4 h-4 text-secondary-500" />
            </button>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-bold text-secondary-900 dark:text-white">{target.indicatorNumber}</h2>
                {target.isRevised ? <Badge size="sm" variant="warning">Revised</Badge> : <Badge size="sm" variant="success">Active</Badge>}
              </div>
              <p className="text-xs text-secondary-500">{target.targetName}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setCurrentPath('/ipms/targets')}>Back To Targets</Button>
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
