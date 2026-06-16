import React, { useMemo, useState } from 'react';
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
import { statusLabels } from '../../data/mockData';
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
  pending_approval: 3,
  approved: 3,
  rejected: 1,
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
}: SubmissionWorkspaceProps) {
  const [activeTab, setActiveTab] = useState('details');
  const [isEditing, setIsEditing] = useState(false);
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

  const attachments = getAttachments(submission);
  const comments = getComments(submission);
  const score = getScore(submission);
  const variance = getVariance(submission);
  const actualExpenditure = getActualExpenditure(submission);
  const targetUnit = submission.target.unitOfMeasure.symbol || submission.target.unitOfMeasure.name;

  const smallTitle = `${titlePrefix}`;
  const pageTitle = `Submission: ${submissionType}-${submission.quarter}-${submission.id.padStart(4, '0')}`;
  const scoreDisplay = score !== undefined ? `${score} / 5` : submission.status === 'approved' ? '4 / 5' : '-';

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
              <Button variant="primary" size="sm" onClick={() => setIsEditing(false)}>
                Save
              </Button>
            </>
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
                <h3 className="text-xl font-semibold text-secondary-900 dark:text-white">{submission.target.targetName}</h3>
                <Badge variant="info" size="md">{`${submissionType}-${submission.id.padStart(4, '0')}`}</Badge>
              </div>
              <p className="text-sm text-secondary-500">
                {submission.quarter} {submission.target.period.fiscalYear} · {submission.submitter?.displayName ?? 'Unassigned'} · {submission.target.department.name}
              </p>
              <div className="flex flex-wrap gap-2">
                <Badge variant={getStatusBadgeVariant(submission.status)}>{statusLabels[submission.status]}</Badge>
                <Badge variant="primary">{submission.quarter}</Badge>
                <Badge variant="default">{submissionType} Submission</Badge>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 rounded-xl border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800/80 md:grid-cols-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Quarter</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">
                {submission.quarter} {submission.target.period.fiscalYear}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Due</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">{formatDate(submission.dueDate)}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Actual</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">
                {formatValue(submission.actual, targetUnit)}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Score</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">{scoreDisplay}</p>
            </div>
          </div>
        </div>

        <WorkflowRail status={submission.status} />
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
              <Field label="Quarter" value={`${submission.quarter} ${submission.target.period.fiscalYear}`} />
              <Field label="Due Date" value={formatDate(submission.dueDate)} />
              <Field label="Extended Due Date" value={formatDate(submission.extendedDueDate)} />
            </div>
          </Section>

          <Section title="Actual Performance" icon={<CheckCircle2 className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field label="Actual" value={formatValue(submission.actual, targetUnit)} />
              <Field label="Actual Expenditure" value={actualExpenditure !== undefined ? `R ${actualExpenditure.toLocaleString()}` : '-'} />
              <Field label="Variance" value={formatVariance(variance)} />
              <Field label="Actual Performance Description" value={submission.actualDescription || '-'} wide />
            </div>
          </Section>

          <Section title="Variance & Corrective Action" icon={<AlertTriangle className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field label="Variance Reason" value={getVarianceReason(submission) || '-'} />
              <Field label="Corrective Measure" value={getCorrectiveMeasure(submission) || '-'} wide />
              <Field label="Submitter Score" value={scoreDisplay} />
              <Field label="Submitter Status" value={<Badge variant={getStatusBadgeVariant(submission.status)}>{statusLabels[submission.status]}</Badge>} />
            </div>
          </Section>
        </div>
      )}

      {activeTab === 'evidence' && (
        <Section title="Proof of Evidence" icon={<FileBadge className="h-4 w-4" />}>
          {attachments.length === 0 ? (
            <div className="rounded-lg border border-dashed border-secondary-300 bg-secondary-50 px-4 py-10 text-center text-sm text-secondary-500 dark:border-secondary-700 dark:bg-secondary-800">
              No evidence uploaded yet for this submission.
            </div>
          ) : (
            <div className="space-y-3">
              {attachments.map((attachment) => (
                <div
                  key={attachment.id}
                  className="flex items-center justify-between rounded-lg border border-secondary-200 bg-secondary-50 px-4 py-3 dark:border-secondary-700 dark:bg-secondary-800"
                >
                  <div>
                    <p className="text-sm font-medium text-secondary-900 dark:text-white">{attachment.fileName}</p>
                    <p className="text-xs text-secondary-500">
                      {attachment.documentType} · {formatDateTime(attachment.uploadedAt)}
                    </p>
                  </div>
                  <Button variant="outline" size="sm">Download</Button>
                </div>
              ))}
            </div>
          )}
        </Section>
      )}

      {activeTab === 'verification' && (
        <Section title="Verification" icon={<ClipboardCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Verified By" value={getVerifier(submission)?.displayName || 'Pending'} />
            <Field label="Verified At" value={formatDateTime(getVerifiedAt(submission))} />
            <Field label="Verification Notes" value={getVerifierComments(submission) || 'Moderate scores and clear the item for audit.'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'approval' && (
        <Section title="Approval" icon={<CheckCircle2 className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Approver" value={getApprover(submission)?.displayName || 'Pending'} />
            <Field label="Approved At" value={formatDateTime(getApprovedAt(submission))} />
            <Field label="Approval Comment" value={getApproverComments(submission) || '-'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'pms' && (
        <Section title="PMS Section" icon={<Sparkles className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="PMS Officer" value={getPmsOfficer(submission)?.displayName || 'Pending'} />
            <Field label="Reviewed At" value={formatDateTime(getPmsReviewedAt(submission))} />
            <Field label="PMS Notes" value={getPmsComments(submission) || 'Ready for moderation and workflow closure.'} wide />
          </div>
        </Section>
      )}

      {activeTab === 'auditor' && (
        <Section title="Auditor Information" icon={<ShieldCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Auditor" value={getAuditor(submission)?.displayName || 'Pending'} />
            <Field label="Audited At" value={formatDateTime(getAuditedAt(submission))} />
            <Field label="Findings" value={getAuditorComments(submission) || '-'} wide />
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
