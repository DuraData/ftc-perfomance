import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  ArrowLeft,
  Check,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  ClipboardCheck,
  Edit2,
  FileBadge,
  FileText,
  MessageSquare,
  ShieldCheck,
  Sparkles,
  TimerReset,
} from 'lucide-react';
import { Button, Badge, Card } from '../ui';
import { Tabs } from '../common/Tabs';
import { FileUpload } from '../common/FileUpload';
import { statusLabels } from '../../data/mockData';
import { mockEmployees } from '../../data/mockData';
import type {
  Attachment,
  IPMSSubmission,
  OPMSSubmission,
  SubmissionComment,
  SubmissionStatus,
} from '../../types';

type SubmissionRecord = OPMSSubmission | IPMSSubmission;
type WorkspaceMode = 'review' | 'list';

interface SubmissionWorkspaceProps {
  submission: SubmissionRecord;
  submissionType: 'OPMS' | 'IPMS';
  titlePrefix?: string;
  subtitle?: string;
  onBack?: () => void;
  mode?: WorkspaceMode;
  onSave?: (submission: SubmissionRecord) => void;
  onDelete?: () => void;
  onAttachmentsChange?: (attachments: Attachment[]) => void;
  onUploadAttachments?: (files: File[]) => void;
  onDeleteAttachment?: (attachmentId: string) => void;
  onWorkflowAction?: (
    action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score',
    payload: { comment?: string; score?: number },
  ) => void;
  onExtendDueDate?: (payload: { extendedDueDate: string; reason: string }) => void;
  workflowBusy?: boolean;
}

interface WorkflowStep {
  id: string;
  label: string;
}

const workflowSteps: WorkflowStep[] = [
  { id: 'draft', label: 'Draft' },
  { id: 'submitted', label: 'Submitted' },
  { id: 'pending_verification', label: 'Verification' },
  { id: 'approved_gate', label: 'Approval' },
  { id: 'pms_gate', label: 'PMS Review' },
  { id: 'audited_gate', label: 'Audit' },
  { id: 'completed', label: 'Completed' },
];

const statusStepMap: Record<SubmissionStatus, number> = {
  draft: 0,
  submitted: 1,
  pending_verification: 2,
  verified: 2,
  verify_rejected: 1,
  pending_approval: 3,
  approved: 3,
  rejected: 1,
  reviewed: 4,
  returned_for_info: 1,
  audited: 5,
  completed: 6,
};

function isOPMSSubmission(submission: SubmissionRecord): submission is OPMSSubmission {
  return 'comments' in submission;
}

function formatDate(value?: string) {
  if (!value) return '-';
  return new Date(value).toLocaleDateString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

function formatDateTime(value?: string) {
  if (!value) return '-';
  return new Date(value).toLocaleString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatValue(value?: number, suffix?: string) {
  if (value === undefined || value === null) return '-';
  return `${value.toLocaleString()}${suffix ? ` ${suffix}` : ''}`;
}

function formatVariance(value?: number) {
  if (value === undefined || value === null) return '-';
  return `${value > 0 ? '+' : ''}${value}%`;
}

function getAttachments(submission: SubmissionRecord): Attachment[] {
  return submission.attachments ?? [];
}

function getComments(submission: SubmissionRecord): SubmissionComment[] {
  return isOPMSSubmission(submission) ? submission.comments : [];
}

function getScore(submission: SubmissionRecord) {
  return 'submitterScore' in submission ? submission.submitterScore : undefined;
}

function getActualExpenditure(submission: SubmissionRecord) {
  return 'actualExpenditure' in submission ? submission.actualExpenditure : undefined;
}

function getVariance(submission: SubmissionRecord) {
  return 'variance' in submission ? submission.variance : undefined;
}

function getVarianceReason(submission: SubmissionRecord) {
  return 'varianceReason' in submission ? submission.varianceReason : undefined;
}

function getCorrectiveMeasure(submission: SubmissionRecord) {
  return 'correctiveMeasure' in submission ? submission.correctiveMeasure : undefined;
}

function getVerifier(submission: SubmissionRecord) {
  return 'verifier' in submission ? submission.verifier : undefined;
}

function getVerifiedAt(submission: SubmissionRecord) {
  return 'verifiedAt' in submission ? submission.verifiedAt : undefined;
}

function getVerifierComments(submission: SubmissionRecord) {
  return 'verifierComments' in submission ? submission.verifierComments : undefined;
}

function getApprover(submission: SubmissionRecord) {
  return 'approver' in submission ? submission.approver : undefined;
}

function getApprovedAt(submission: SubmissionRecord) {
  return 'approvedAt' in submission ? submission.approvedAt : undefined;
}

function getApproverComments(submission: SubmissionRecord) {
  return 'approverComments' in submission ? submission.approverComments : undefined;
}

function getPmsOfficer(submission: SubmissionRecord) {
  return 'pmsOfficer' in submission ? submission.pmsOfficer : undefined;
}

function getPmsReviewedAt(submission: SubmissionRecord) {
  return 'pmsReviewedAt' in submission ? submission.pmsReviewedAt : undefined;
}

function getPmsComments(submission: SubmissionRecord) {
  return 'pmsComments' in submission ? submission.pmsComments : undefined;
}

function getAuditor(submission: SubmissionRecord) {
  return 'auditor' in submission ? submission.auditor : undefined;
}

function getAuditedAt(submission: SubmissionRecord) {
  return 'auditedAt' in submission ? submission.auditedAt : undefined;
}

function getAuditorComments(submission: SubmissionRecord) {
  return 'auditorComments' in submission ? submission.auditorComments : undefined;
}

function getStatusBadgeVariant(status: SubmissionStatus) {
  if (status === 'approved' || status === 'completed') return 'success' as const;
  if (status === 'pending_verification' || status === 'pending_approval') return 'warning' as const;
  if (status === 'verified' || status === 'audited') return 'info' as const;
  if (status === 'rejected' || status === 'returned_for_info') return 'error' as const;
  return 'default' as const;
}

function Section({
  title,
  icon,
  children,
}: {
  title: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <Card className="overflow-hidden" padding="none">
      <div className="flex items-center gap-2 border-b border-secondary-200 px-4 py-3 text-sm font-semibold text-secondary-900 dark:border-secondary-700 dark:text-white">
        <span className="text-primary-600 dark:text-primary-400">{icon}</span>
        <span>{title}</span>
      </div>
      <div className="space-y-4 px-4 py-4">{children}</div>
    </Card>
  );
}

function Field({
  label,
  value,
  wide = false,
  editable = false,
  type = 'text',
  onChange,
}: {
  label: string;
  value: React.ReactNode;
  wide?: boolean;
  editable?: boolean;
  type?: string;
  onChange?: (val: string) => void;
}) {
  return (
    <div className={wide ? 'md:col-span-3' : ''}>
      <p className="mb-1.5 text-[11px] font-semibold uppercase tracking-wide text-secondary-500">{label}</p>
      {editable ? (
        <input
          type={type}
          defaultValue={typeof value === 'string' || typeof value === 'number' ? value : ''}
          onChange={(e) => onChange?.(e.target.value)}
          className="w-full min-h-11 rounded-lg border border-secondary-200 bg-white px-3 py-2 text-sm text-secondary-900 dark:border-secondary-700 dark:bg-secondary-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      ) : (
        <div className="min-h-11 rounded-lg border border-secondary-200 bg-secondary-50 px-3 py-2 text-sm text-secondary-900 dark:border-secondary-700 dark:bg-secondary-800 dark:text-white">
          {value}
        </div>
      )}
    </div>
  );
}

function WorkflowRail({ status }: { status: SubmissionStatus }) {
  const currentIndex = statusStepMap[status];

  return (
    <div className="rounded-xl border border-secondary-200 bg-white px-4 py-4 dark:border-secondary-700 dark:bg-secondary-900">
      <div className="flex items-center gap-2 overflow-x-auto">
        {workflowSteps.map((step, index) => {
          const complete = index < currentIndex;
          const current = index === currentIndex;

          return (
            <React.Fragment key={step.id}>
              <div className="flex min-w-max items-center gap-2">
                <div
                  className={`flex h-7 w-7 items-center justify-center rounded-full border text-xs font-semibold ${
                    complete
                      ? 'border-success-600 bg-success-600 text-white'
                      : current
                        ? 'border-primary-600 bg-primary-600 text-white'
                        : 'border-secondary-300 bg-white text-secondary-500 dark:border-secondary-600 dark:bg-secondary-900'
                  }`}
                >
                  {complete ? <Check className="h-4 w-4" /> : index + 1}
                </div>
                <span className={`text-xs font-medium ${current ? 'text-secondary-900 dark:text-white' : 'text-secondary-500'}`}>
                  {step.label}
                </span>
              </div>
              {index < workflowSteps.length - 1 && (
                <div className="h-0.5 min-w-12 flex-1 rounded bg-secondary-200 dark:bg-secondary-700">
                  <div
                    className={`h-0.5 rounded ${index < currentIndex ? 'bg-success-600' : 'bg-secondary-200 dark:bg-secondary-700'}`}
                  />
                </div>
              )}
            </React.Fragment>
          );
        })}
      </div>
    </div>
  );
}

export function SubmissionWorkspace({
  submission,
  submissionType,
  titlePrefix = 'Workflow / Verification',
  subtitle,
  onBack,
  mode = 'review',
  onSave,
  onDelete,
  onAttachmentsChange,
  onUploadAttachments,
  onDeleteAttachment,
  onWorkflowAction,
  onExtendDueDate,
  workflowBusy = false,
}: SubmissionWorkspaceProps) {
  const [activeTab, setActiveTab] = useState('details');
  const [isEditing, setIsEditing] = useState(false);
  const [draftSubmission, setDraftSubmission] = useState<SubmissionRecord>(submission);
  const [workflowComment, setWorkflowComment] = useState('');
  const [workflowScore, setWorkflowScore] = useState('');
  const [extendedDueDate, setExtendedDueDate] = useState('');
  const [extensionReason, setExtensionReason] = useState('');
  useEffect(() => {
    setDraftSubmission(submission);
    setIsEditing(false);
    setWorkflowComment('');
    setWorkflowScore('');
    setExtendedDueDate(submission.extendedDueDate?.slice(0, 10) ?? submission.dueDate?.slice(0, 10) ?? '');
    setExtensionReason('');
  }, [submission]);

  const currentSubmission = draftSubmission;
  const tabs = useMemo(
    () => [
      { id: 'details', label: 'Submission Details', icon: <FileText className="w-3.5 h-3.5" /> },
      { id: 'evidence', label: 'Proof of Evidence', icon: <FileBadge className="w-3.5 h-3.5" /> },
      { id: 'verification', label: 'Verification', icon: <ClipboardCheck className="w-3.5 h-3.5" /> },
      { id: 'approval', label: 'Approval', icon: <CheckCircle2 className="w-3.5 h-3.5" /> },
      { id: 'pms', label: 'PMS Section', icon: <Sparkles className="w-3.5 h-3.5" /> },
      { id: 'auditor', label: 'Auditor Information', icon: <ShieldCheck className="w-3.5 h-3.5" /> },
      { id: 'comments', label: 'Comments & History', icon: <MessageSquare className="w-3.5 h-3.5" /> },
    ],
    [],
  );

  const attachments = getAttachments(currentSubmission);
  const comments = getComments(currentSubmission);
  const score = getScore(currentSubmission);
  const variance = getVariance(currentSubmission);
  const actualExpenditure = getActualExpenditure(currentSubmission);
  const targetUnit = currentSubmission.target.unitOfMeasure.symbol || currentSubmission.target.unitOfMeasure.name;

  const smallTitle = `${titlePrefix}`;
  const pageTitle = `Submission: ${submissionType}-${currentSubmission.quarter}-${currentSubmission.id.padStart(4, '0')}`;
  const scoreDisplay = score !== undefined ? `${score} / 5` : currentSubmission.status === 'approved' ? '4 / 5' : '-';

  const updateDraftSubmission = (updater: (current: SubmissionRecord) => SubmissionRecord) => {
    setDraftSubmission(prev => updater(prev));
  };

  const handleSave = () => {
    onSave?.(draftSubmission);
    setIsEditing(false);
  };

  const syncAttachments = (nextAttachments: Attachment[]) => {
    updateDraftSubmission(current => ({ ...current, attachments: nextAttachments }));
    onAttachmentsChange?.(nextAttachments);
  };

  const uploadedFileItems = attachments.map(attachment => ({
    id: attachment.id,
    name: attachment.fileName,
    size: attachment.fileSize,
    type: attachment.fileType,
    uploadedAt: attachment.uploadedAt,
    uploadedBy: attachment.uploadedBy.displayName,
    documentType: attachment.documentType,
    url: attachment.url,
    progress: 100,
  }));

  const buildAttachment = (file: File): Attachment => ({
    id: `att-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    fileName: file.name,
    fileSize: file.size,
    fileType: file.type,
    uploadedBy: currentSubmission.submitter ?? getVerifier(currentSubmission) ?? getApprover(currentSubmission) ?? mockEmployees[0],
    uploadedAt: new Date().toISOString(),
    documentType: 'evidence',
    url: URL.createObjectURL(file),
  });

  const triggerWorkflowAction = (action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score') => {
    if (action === 'score') {
      const parsedScore = Number(workflowScore);
      if (Number.isNaN(parsedScore)) {
        return;
      }

      onWorkflowAction?.(action, {
        comment: workflowComment || undefined,
        score: parsedScore,
      });
      return;
    }

    onWorkflowAction?.(action, {
      comment: workflowComment || undefined,
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-semibold text-secondary-500">{smallTitle}</p>
          <h2 className="text-2xl font-semibold text-secondary-900 dark:text-white">{pageTitle}</h2>
          {subtitle && <p className="mt-1 text-sm text-secondary-500">{subtitle}</p>}
        </div>
        <div className="flex items-center gap-2">
          {!isEditing ? (
            <Button variant="outline" size="sm" icon={<Edit2 className="w-4 h-4" />} onClick={() => setIsEditing(true)}>
              Edit
            </Button>
          ) : (
            <>
              <Button variant="outline" size="sm" onClick={() => setIsEditing(false)}>
                Cancel
              </Button>
              <Button variant="primary" size="sm" onClick={handleSave}>
                Save
              </Button>
            </>
          )}
          {onDelete && (
            <Button variant="error" size="sm" onClick={onDelete}>
              Delete
            </Button>
          )}
          {onBack && (
            <Button variant="outline" size="sm" icon={<ArrowLeft className="w-4 h-4" />} onClick={onBack}>
              Back
            </Button>
          )}
        </div>
      </div>

      <Card className="space-y-4" padding="lg">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div className="flex items-start gap-3">
            <div className="rounded-xl bg-primary-100 p-3 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300">
              <FileText className="h-5 w-5" />
            </div>
            <div className="space-y-2">
              <div className="flex flex-wrap items-center gap-2">
                <h3 className="text-xl font-semibold text-secondary-900 dark:text-white">{currentSubmission.target.targetName}</h3>
                <Badge variant="info" size="md">{`${submissionType}-${currentSubmission.id.padStart(4, '0')}`}</Badge>
              </div>
              <p className="text-sm text-secondary-500">
                {currentSubmission.quarter} {currentSubmission.target.period.fiscalYear} · {currentSubmission.submitter?.displayName ?? 'Unassigned'} · {currentSubmission.target.department.name}
              </p>
              <div className="flex flex-wrap gap-2">
                <Badge variant={getStatusBadgeVariant(currentSubmission.status)}>{statusLabels[currentSubmission.status]}</Badge>
                <Badge variant="primary">{currentSubmission.quarter}</Badge>
                <Badge variant="default">{submissionType} Submission</Badge>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/80 md:grid-cols-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Quarter</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">
                {currentSubmission.quarter} {currentSubmission.target.period.fiscalYear}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Due</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">{formatDate(currentSubmission.dueDate)}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Actual</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">
                {formatValue(currentSubmission.actual, targetUnit)}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Score</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">{scoreDisplay}</p>
            </div>
          </div>
        </div>

        <WorkflowRail status={currentSubmission.status} />
      </Card>

      <div className="flex items-center justify-between gap-2 rounded-xl border border-secondary-200 bg-white px-3 py-2 dark:border-secondary-700 dark:bg-secondary-900">
        <Button variant="ghost" size="sm" icon={<ChevronLeft className="h-4 w-4" />} onClick={onBack}>
          Previous
        </Button>
        <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} variant="compact" />
        <Button variant="ghost" size="sm" icon={<ChevronRight className="h-4 w-4" />} iconPosition="right">
          Next
        </Button>
      </div>

      {activeTab === 'details' && (
        <div className="space-y-4">
          <Section title="Reporting Period" icon={<TimerReset className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field label="Quarter" value={`${currentSubmission.quarter} ${currentSubmission.target.period.fiscalYear}`} />
              <Field label="Due Date" value={formatDate(currentSubmission.dueDate)} />
              <Field label="Extended Due Date" value={formatDate(currentSubmission.extendedDueDate)} />
            </div>
          </Section>

          <Section title="Actual Performance" icon={<CheckCircle2 className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field
                label="Actual"
                value={currentSubmission.actual ?? ''}
                editable={isEditing}
                type="number"
                onChange={(value) => updateDraftSubmission(current => ({ ...current, actual: Number(value || 0) }))}
              />
              <Field
                label="Actual Expenditure"
                value={actualExpenditure !== undefined ? actualExpenditure : ''}
                editable={isEditing}
                type="number"
                onChange={(value) => updateDraftSubmission(current => 'actualExpenditure' in current ? { ...current, actualExpenditure: Number(value || 0) } : current)}
              />
              <Field label="Variance" value={formatVariance(variance)} />
              <Field
                label="Actual Performance Description"
                value={currentSubmission.actualDescription || ''}
                wide
                editable={isEditing}
                onChange={(value) => updateDraftSubmission(current => ({ ...current, actualDescription: value }))}
              />
            </div>
          </Section>

          <Section title="Variance & Corrective Action" icon={<AlertTriangle className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field
                label="Variance Reason"
                value={getVarianceReason(currentSubmission) || ''}
                editable={isEditing}
                onChange={(value) => updateDraftSubmission(current => 'varianceReason' in current ? { ...current, varianceReason: value } : current)}
              />
              <Field
                label="Corrective Measure"
                value={getCorrectiveMeasure(currentSubmission) || ''}
                wide
                editable={isEditing}
                onChange={(value) => updateDraftSubmission(current => 'correctiveMeasure' in current ? { ...current, correctiveMeasure: value } : current)}
              />
              <Field label="Submitter Score" value={scoreDisplay} />
              <Field label="Submitter Status" value={<Badge variant={getStatusBadgeVariant(currentSubmission.status)}>{statusLabels[currentSubmission.status]}</Badge>} />
            </div>
          </Section>
        </div>
      )}

      {activeTab === 'evidence' && (
        <Section title="Proof of Evidence" icon={<FileBadge className="h-4 w-4" />}>
          <FileUpload
            existingFiles={uploadedFileItems}
            maxFiles={undefined}
            onUpload={(files) => {
              if (onUploadAttachments) {
                onUploadAttachments(files);
                return;
              }
              syncAttachments([...attachments, ...files.map(buildAttachment)]);
            }}
            onRemove={(fileId) => {
              if (onDeleteAttachment) {
                onDeleteAttachment(fileId);
                return;
              }
              syncAttachments(attachments.filter(attachment => attachment.id !== fileId));
            }}
          />
        </Section>
      )}

      {activeTab === 'verification' && (
        <Section title="Verification" icon={<ClipboardCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Verified By" value={getVerifier(currentSubmission)?.displayName || 'Pending'} />
            <Field label="Verified At" value={formatDateTime(getVerifiedAt(currentSubmission))} />
            <Field label="Verification Notes" value={getVerifierComments(currentSubmission) || 'Moderate scores and clear the item for audit.'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'approval' && (
        <Section title="Approval" icon={<CheckCircle2 className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Approver" value={getApprover(currentSubmission)?.displayName || 'Pending'} />
            <Field label="Approved At" value={formatDateTime(getApprovedAt(currentSubmission))} />
            <Field label="Approval Comment" value={getApproverComments(currentSubmission) || '-'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'pms' && (
        <Section title="PMS Section" icon={<Sparkles className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="PMS Officer" value={getPmsOfficer(currentSubmission)?.displayName || 'Pending'} />
            <Field label="Reviewed At" value={formatDateTime(getPmsReviewedAt(currentSubmission))} />
            <Field label="PMS Notes" value={getPmsComments(currentSubmission) || 'Ready for moderation and workflow closure.'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'auditor' && (
        <Section title="Auditor Information" icon={<ShieldCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Auditor" value={getAuditor(currentSubmission)?.displayName || 'Pending'} />
            <Field label="Audited At" value={formatDateTime(getAuditedAt(currentSubmission))} />
            <Field label="Findings" value={getAuditorComments(currentSubmission) || '-'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'comments' && (
        <Section title="Comments & History" icon={<MessageSquare className="h-4 w-4" />}>
          {comments.length === 0 ? (
            <div className="rounded-lg border border-dashed border-secondary-300 bg-secondary-50 px-4 py-10 text-center text-sm text-secondary-500 dark:border-secondary-700 dark:bg-secondary-800">
              No comments or workflow history recorded yet.
            </div>
          ) : (
            <div className="space-y-3">
              {comments.map((comment) => (
                <div
                  key={comment.id}
                  className="rounded-lg border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800"
                >
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-medium text-secondary-900 dark:text-white">{comment.author.displayName}</p>
                    <p className="text-xs text-secondary-500">{formatDateTime(comment.createdAt)}</p>
                  </div>
                  <p className="mt-2 text-sm text-secondary-700 dark:text-secondary-300">{comment.content}</p>
                </div>
              ))}
            </div>
          )}
        </Section>
      )}

      {(onWorkflowAction || onExtendDueDate) && (
        <Section title="Workflow Actions" icon={<Check className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field
              label="Workflow Comment"
              value={workflowComment}
              editable
              onChange={setWorkflowComment}
              wide
            />
            <Field
              label="Score"
              value={workflowScore}
              editable
              type="number"
              onChange={setWorkflowScore}
            />
            <Field
              label="Extended Due Date"
              value={extendedDueDate}
              editable
              type="date"
              onChange={setExtendedDueDate}
            />
            <Field
              label="Extension Reason"
              value={extensionReason}
              editable
              onChange={setExtensionReason}
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="primary" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('submit')}>
              Submit
            </Button>
            <Button variant="outline" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('verify')}>
              Verify
            </Button>
            <Button variant="outline" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('verify-reject')}>
              Verify Reject
            </Button>
            <Button variant="success" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('approve')}>
              Approve
            </Button>
            <Button variant="error" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('reject')}>
              Reject
            </Button>
            <Button variant="outline" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('review')}>
              Review
            </Button>
            <Button variant="outline" size="sm" disabled={workflowBusy} onClick={() => triggerWorkflowAction('audit')}>
              Audit
            </Button>
            <Button variant="outline" size="sm" disabled={workflowBusy || workflowScore.trim() === ''} onClick={() => triggerWorkflowAction('score')}>
              Save Score
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={workflowBusy || !extendedDueDate || !extensionReason.trim()}
              onClick={() => onExtendDueDate?.({ extendedDueDate, reason: extensionReason.trim() })}
            >
              Extend Due Date
            </Button>
          </div>
        </Section>
      )}

      <div className="flex flex-col justify-between gap-3 rounded-xl border border-secondary-200 bg-white px-4 py-3 dark:border-secondary-700 dark:bg-secondary-900 md:flex-row md:items-center">
        <p className="text-sm text-secondary-500">
          {mode === 'review' ? 'Moderate scores and clear the item for audit.' : 'Open the next submission to continue review.'}
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" icon={<AlertTriangle className="h-4 w-4" />}>
            Escalate
          </Button>
          <Button variant="success" size="sm" icon={<CheckCircle2 className="h-4 w-4" />}>
            Clear Review
          </Button>
        </div>
      </div>
    </div>
  );
}
