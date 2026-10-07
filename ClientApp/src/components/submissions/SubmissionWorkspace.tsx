import React, { useCallback, useEffect, useMemo, useState } from 'react';
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
import { submissionStatusLabels as statusLabels } from './submissionStatus';
import type {
  Attachment,
  IPMSSubmission,
  OPMSSubmission,
  SubmissionComment,
  SubmissionStatus,
  PerformanceSuggestionEvent,
} from '../../types';
import {
  generateIpmsConsolidationSuggestion,
  generateOpmsConsolidationSuggestion,
  assessIpmsSubmissionAttachment,
  assessOpmsSubmissionAttachment,
  getIpmsConsolidationHistoryPage,
  getIpmsSubmission,
  getIpmsSubmissionAttachmentsPage,
  getOpmsConsolidationHistoryPage,
  getOpmsSubmission,
  getOpmsSubmissionAttachmentsPage,
  placeIpmsEvidenceLegalHold,
  placeOpmsEvidenceLegalHold,
  releaseIpmsEvidenceLegalHold,
  releaseOpmsEvidenceLegalHold,
  replaceIpmsSubmissionAttachment,
  replaceOpmsSubmissionAttachment,
  requestIpmsEvidenceDisposal,
  requestOpmsEvidenceDisposal,
  rescanIpmsSubmissionAttachment,
  rescanOpmsSubmissionAttachment,
  saveIpmsConsolidatedActual,
  saveOpmsConsolidatedActual,
  uploadIpmsSubmissionAttachment,
  uploadOpmsSubmissionAttachment,
} from '../../api/api';
import { PerformanceRfiWorkspace } from '../workflow/PerformanceRfiWorkspace';
import { StageRatingHistory } from '../workflow/StageRatingHistory';
import { useSecurity } from '../../context/SecurityContext';
import { GovernedWithdrawalDialog } from '../common/GovernedWithdrawalDialog';

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
  onWithdraw?: (reason: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onAttachmentsChange?: (attachments: Attachment[]) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onUploadAttachments?: (files: File[]) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onRescanAttachment?: (attachmentId: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onAssessAttachment?: (attachmentId: string, outcome: 1 | 2 | 3, comment?: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onReplaceAttachment?: (attachmentId: string, replacementPublicId: string, reason: string, supersededRowVersion: string, replacementRowVersion: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onPlaceAttachmentHold?: (attachmentId: string, holdReference: string, reason: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onReleaseAttachmentHold?: (attachmentId: string, holdId: string, reason: string) => void;
  /** @deprecated Evidence is loaded and mutated by the bounded workspace register. */
  onDisposeAttachment?: (attachmentId: string, approvalReference: string, reason: string, rowVersion: string) => void;
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
  onWithdraw,
  onWorkflowAction,
  onExtendDueDate,
  workflowBusy = false,
}: SubmissionWorkspaceProps) {
  const security = useSecurity();
  const [activeTab, setActiveTab] = useState('details');
  const [isEditing, setIsEditing] = useState(false);
  const [draftSubmission, setDraftSubmission] = useState<SubmissionRecord>(submission);
  const [workflowComment, setWorkflowComment] = useState('');
  const [workflowScore, setWorkflowScore] = useState('');
  const [extendedDueDate, setExtendedDueDate] = useState('');
  const [extensionReason, setExtensionReason] = useState('');
  const [showWithdrawal, setShowWithdrawal] = useState(false);
  const [consolidationBusy, setConsolidationBusy] = useState(false);
  const [consolidationError, setConsolidationError] = useState('');
  const [consolidatedActual, setConsolidatedActual] = useState(submission.actualPerformance ?? '');
  const [consolidationReason, setConsolidationReason] = useState('');
  const [consolidationHistory, setConsolidationHistory] = useState<PerformanceSuggestionEvent[]>([]);
  const [consolidationHistoryPage, setConsolidationHistoryPage] = useState(1);
  const [consolidationHistoryTotalCount, setConsolidationHistoryTotalCount] = useState(0);
  const [consolidationHistoryTotalPages, setConsolidationHistoryTotalPages] = useState(0);
  const [consolidationHistorySearchInput, setConsolidationHistorySearchInput] = useState('');
  const [consolidationHistorySearch, setConsolidationHistorySearch] = useState('');
  const [consolidationHistoryEventType, setConsolidationHistoryEventType] = useState('');
  const [consolidationHistoryBusy, setConsolidationHistoryBusy] = useState(false);
  const [consolidationHistoryError, setConsolidationHistoryError] = useState('');
  const [consolidationHistoryRevision, setConsolidationHistoryRevision] = useState(0);
  const [attachments, setAttachments] = useState<Attachment[]>([]);
  const [evidencePage, setEvidencePage] = useState(1);
  const [evidenceTotalCount, setEvidenceTotalCount] = useState(0);
  const [evidenceTotalPages, setEvidenceTotalPages] = useState(0);
  const [evidenceSearchInput, setEvidenceSearchInput] = useState('');
  const [evidenceSearch, setEvidenceSearch] = useState('');
  const [evidenceScanStatus, setEvidenceScanStatus] = useState('');
  const [evidenceLifecycle, setEvidenceLifecycle] = useState('');
  const [evidenceBusy, setEvidenceBusy] = useState(false);
  const [evidenceError, setEvidenceError] = useState('');
  const [evidenceNotice, setEvidenceNotice] = useState('');
  const [evidenceRevision, setEvidenceRevision] = useState(0);
  useEffect(() => {
    setDraftSubmission(submission);
    setIsEditing(false);
    setWorkflowComment('');
    setWorkflowScore('');
    setExtendedDueDate(submission.extendedDueDate?.slice(0, 10) ?? submission.dueDate?.slice(0, 10) ?? '');
    setExtensionReason('');
    setConsolidatedActual(submission.actualPerformance ?? '');
    setConsolidationReason('');
    setConsolidationError('');
    setConsolidationHistory([]);
    setConsolidationHistoryPage(1);
    setConsolidationHistoryTotalCount(0);
    setConsolidationHistoryTotalPages(0);
    setConsolidationHistorySearchInput('');
    setConsolidationHistorySearch('');
    setConsolidationHistoryEventType('');
    setConsolidationHistoryError('');
    setAttachments([]);
    setEvidencePage(1);
    setEvidenceSearchInput('');
    setEvidenceSearch('');
    setEvidenceScanStatus('');
    setEvidenceLifecycle('');
    setEvidenceNotice('');
  }, [submission]);

  useEffect(() => {
    const normalized = evidenceSearchInput.trim();
    if (normalized === evidenceSearch) return;
    const timeout = window.setTimeout(() => { setEvidencePage(1); setEvidenceSearch(normalized); }, 300);
    return () => window.clearTimeout(timeout);
  }, [evidenceSearch, evidenceSearchInput]);

  const loadEvidence = useCallback(async () => {
    if (activeTab !== 'evidence') return;
    setEvidenceBusy(true);
    setEvidenceError('');
    const query = {
      page: evidencePage,
      pageSize: 25,
      search: evidenceSearch,
      sortBy: 'uploadedAt',
      sortDirection: 'desc' as const,
      scanStatus: evidenceScanStatus || undefined,
      active: evidenceLifecycle === 'active' ? true : evidenceLifecycle === 'retired' ? false : undefined,
    };
    const result = submissionType === 'OPMS'
      ? await getOpmsSubmissionAttachmentsPage(submission.id, query)
      : await getIpmsSubmissionAttachmentsPage(submission.id, query);
    if (result.success && result.data) {
      setAttachments(result.data.items);
      setEvidenceTotalCount(result.data.totalCount);
      setEvidenceTotalPages(result.data.totalPages);
    } else {
      setAttachments([]);
      setEvidenceTotalCount(0);
      setEvidenceTotalPages(0);
      setEvidenceError(result.message ?? 'Submission evidence could not be loaded.');
    }
    setEvidenceBusy(false);
  }, [activeTab, evidenceLifecycle, evidencePage, evidenceScanStatus, evidenceSearch, submission.id, submissionType]);

  useEffect(() => {
    // The monotonic revision deliberately re-runs the active page after a governed mutation.
    void evidenceRevision;
    void loadEvidence();
  }, [evidenceRevision, loadEvidence]);

  useEffect(() => {
    const normalized = consolidationHistorySearchInput.trim();
    if (normalized === consolidationHistorySearch) return;
    const timeout = window.setTimeout(() => {
      setConsolidationHistoryPage(1);
      setConsolidationHistorySearch(normalized);
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [consolidationHistorySearch, consolidationHistorySearchInput]);

  const loadConsolidationHistory = useCallback(async () => {
    if (submission.quarter !== 'Mid-Year' && submission.quarter !== 'Annual') return;
    setConsolidationHistoryBusy(true);
    setConsolidationHistoryError('');
    const query = {
      page: consolidationHistoryPage,
      pageSize: 10,
      search: consolidationHistorySearch,
      eventType: consolidationHistoryEventType
        ? consolidationHistoryEventType as PerformanceSuggestionEvent['eventType']
        : undefined,
      sortBy: 'occurredAt',
      sortDirection: 'desc' as const,
    };
    const response = submissionType === 'OPMS'
      ? await getOpmsConsolidationHistoryPage(submission.id, query)
      : await getIpmsConsolidationHistoryPage(submission.id, query);
    if (response.success && response.data) {
      setConsolidationHistory(response.data.items);
      setConsolidationHistoryTotalCount(response.data.totalCount);
      setConsolidationHistoryTotalPages(response.data.totalPages);
    } else {
      setConsolidationHistory([]);
      setConsolidationHistoryTotalCount(0);
      setConsolidationHistoryTotalPages(0);
      setConsolidationHistoryError(response.message ?? 'Suggestion history could not be loaded.');
    }
    setConsolidationHistoryBusy(false);
  }, [consolidationHistoryEventType, consolidationHistoryPage, consolidationHistorySearch, submission.id, submission.quarter, submissionType]);

  useEffect(() => {
    void consolidationHistoryRevision;
    let active = true;
    const load = async () => {
      try {
        if (active) await loadConsolidationHistory();
      } catch {
        if (active) {
          setConsolidationHistory([]);
          setConsolidationHistoryError('Suggestion history could not be loaded.');
          setConsolidationHistoryBusy(false);
        }
      }
    };
    void load();
    return () => { active = false; };
  }, [consolidationHistoryRevision, loadConsolidationHistory]);

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

  const comments = getComments(currentSubmission);
  const score = getScore(currentSubmission);
  const variance = getVariance(currentSubmission);
  const actualExpenditure = getActualExpenditure(currentSubmission);
  const targetUnit = currentSubmission.target.unitOfMeasure.symbol || currentSubmission.target.unitOfMeasure.name;
  const isConsolidationPeriod = currentSubmission.quarter === 'Mid-Year' || currentSubmission.quarter === 'Annual';
  const resourceCode = `${submissionType}_SUBMISSION`;
  const poeResourceCode = `${submissionType}_POE`;
  const canReadActual = security.canReadField(resourceCode, 'ActualPerformance');
  const canReadSuggestionActor = security.canReadField(resourceCode, 'SuggestionActor');
  const canReadSuggestionReason = security.canReadField(resourceCode, 'SuggestionReason');
  const canReadSuggestionCorrelation = security.canReadField(resourceCode, 'SuggestionCorrelationId');
  const canReadSubmitterIdentity = security.canReadField(resourceCode, 'SubmitterIdentity');
  const canReadSubmitterScore = security.canReadField(resourceCode, 'SubmitterScore');
  const canReadVerifierIdentity = security.canReadField(resourceCode, 'VerifierIdentity');
  const canReadVerifierComment = security.canReadField(resourceCode, 'VerifierComment');
  const canReadApproverIdentity = security.canReadField(resourceCode, 'ApproverIdentity');
  const canReadApproverComment = security.canReadField(resourceCode, 'ApproverComment');
  const canReadPmsIdentity = security.canReadField(resourceCode, 'PmsIdentity');
  const canReadPmsComment = security.canReadField(resourceCode, 'PmsComment');
  const canReadAuditorIdentity = security.canReadField(resourceCode, 'InternalAuditAssessedBy');
  const canReadAuditorObservation = security.canReadField(resourceCode, 'InternalAuditObservation');
  const canReadAuditorComment = security.canReadField(resourceCode, 'InternalAuditComment');
  const canEditActual = security.canUpdate(resourceCode) && security.canEditField(resourceCode, 'ActualPerformance');
  const canReadVariance = security.canReadField(resourceCode, 'Variance');
  const canReadVarianceReason = security.canReadField(resourceCode, 'VarianceReason');
  const canEditVarianceReason = security.canUpdate(resourceCode) && security.canEditField(resourceCode, 'VarianceReason');
  const canReadCorrectiveMeasure = security.canReadField(resourceCode, 'CorrectiveMeasure');
  const canEditCorrectiveMeasure = security.canUpdate(resourceCode) && security.canEditField(resourceCode, 'CorrectiveMeasure');
  const canEditSubmissionMembers = canEditActual || canEditVarianceReason || canEditCorrectiveMeasure;
  const canManageConsolidation = canEditActual;
  const canReadPoeUploader = security.canReadField(poeResourceCode, 'UploadedByUserId') || security.canReadField(poeResourceCode, 'UploadedByName');
  const canReadPoeScanDetail = security.canReadField(poeResourceCode, 'ScanDetail');
  const canReadAssessmentComment = security.canReadField(poeResourceCode, 'AssessmentComment');
  const canReadAssessorId = security.canReadField(poeResourceCode, 'AssessedByUserId');
  const canReadAssessorName = security.canReadField(poeResourceCode, 'AssessedByName');
  const canReadAssessmentCorrelation = security.canReadField(poeResourceCode, 'AssessmentCorrelationId');
  const canReadReplacementActorId = security.canReadField(poeResourceCode, 'ReplacedByUserId');
  const canReadReplacementActorName = security.canReadField(poeResourceCode, 'ReplacedByName');
  const canReadReplacementCorrelation = security.canReadField(poeResourceCode, 'ReplacementCorrelationId');
  const canReadHoldActorId = security.canReadField(poeResourceCode, 'LegalHoldActorUserId');
  const canReadHoldActorName = security.canReadField(poeResourceCode, 'LegalHoldActorName');
  const canReadDisposalActorId = security.canReadField(poeResourceCode, 'DisposalRequestedByUserId');
  const canReadDisposalActorName = security.canReadField(poeResourceCode, 'DisposalRequestedByName');
  const canReadDisposalDetail = security.canReadField(poeResourceCode, 'DisposalDetail');

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

  const uploadedFileItems = attachments.map(attachment => ({
    id: attachment.id,
    publicId: attachment.publicId,
    evidenceBlobPublicId: attachment.evidenceBlobPublicId,
    name: attachment.fileName,
    size: attachment.fileSize,
    type: attachment.fileType,
    uploadedAt: attachment.uploadedAt,
    uploadedBy: canReadPoeUploader ? attachment.uploadedBy?.displayName : undefined,
    documentType: attachment.documentType,
    url: attachment.url,
    scanStatus: attachment.scanStatus,
    isQuarantined: attachment.isQuarantined,
    scanDetail: canReadPoeScanDetail ? attachment.scanDetail : undefined,
    assessments: attachment.assessments?.map(item => ({
      ...item,
      comment: canReadAssessmentComment ? item.comment : undefined,
      assessedByUserId: canReadAssessorId ? item.assessedByUserId : undefined,
      assessedByName: canReadAssessorName ? item.assessedByName : undefined,
      correlationId: canReadAssessmentCorrelation ? item.correlationId : undefined,
    })),
    rowVersion: attachment.rowVersion,
    replacementOf: attachment.replacementOf ? {
      ...attachment.replacementOf,
      replacedByUserId: canReadReplacementActorId ? attachment.replacementOf.replacedByUserId : undefined,
      replacedByName: canReadReplacementActorName ? attachment.replacementOf.replacedByName : undefined,
      correlationId: canReadReplacementCorrelation ? attachment.replacementOf.correlationId : undefined,
    } : attachment.replacementOf,
    legalHolds: attachment.legalHolds?.map(item => ({
      ...item,
      placedByUserId: canReadHoldActorId ? item.placedByUserId : undefined,
      placedByName: canReadHoldActorName ? item.placedByName : undefined,
      releasedByUserId: canReadHoldActorId ? item.releasedByUserId : undefined,
      releasedByName: canReadHoldActorName ? item.releasedByName : undefined,
    })),
    isActive: attachment.isActive,
    retainUntil: attachment.retainUntil,
    disposals: attachment.disposals?.map(item => ({
      ...item,
      requestedByUserId: canReadDisposalActorId ? item.requestedByUserId : undefined,
      requestedByName: canReadDisposalActorName ? item.requestedByName : undefined,
      detail: canReadDisposalDetail ? item.detail : undefined,
    })),
    isContentDeleted: attachment.isContentDeleted,
    progress: 100,
  }));

  const refreshEvidence = () => setEvidenceRevision(value => value + 1);

  const uploadEvidence = async (files: File[]) => {
    setEvidenceBusy(true);
    const results = await Promise.all(files.map(file => submissionType === 'OPMS'
      ? uploadOpmsSubmissionAttachment(submission.id, file)
      : uploadIpmsSubmissionAttachment(submission.id, file)));
    const successes = results.filter(result => result.success).length;
    const failure = results.find(result => !result.success);
    if (successes) setEvidenceNotice(`${successes} file${successes === 1 ? '' : 's'} uploaded.`);
    if (failure) setEvidenceError(failure.message ?? 'One or more evidence files could not be uploaded.');
    setEvidencePage(1);
    refreshEvidence();
  };

  const mutateEvidence = async (
    operation: () => Promise<{ success: boolean; message?: string | null }>,
    successMessage: string,
  ) => {
    setEvidenceBusy(true);
    setEvidenceError('');
    setEvidenceNotice('');
    const result = await operation();
    if (result.success) setEvidenceNotice(successMessage);
    else setEvidenceError(result.message ?? 'Evidence operation failed.');
    refreshEvidence();
  };

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

  const refreshConsolidation = async () => {
    const response = submissionType === 'OPMS'
      ? await getOpmsSubmission(currentSubmission.id)
      : await getIpmsSubmission(currentSubmission.id);
    if (response.success && response.data) {
      setDraftSubmission(response.data);
      setConsolidatedActual(response.data.actualPerformance ?? '');
    }
    setConsolidationHistoryPage(1);
    setConsolidationHistoryRevision(value => value + 1);
  };

  const generateConsolidation = async () => {
    setConsolidationBusy(true);
    setConsolidationError('');
    try {
      const response = submissionType === 'OPMS'
        ? await generateOpmsConsolidationSuggestion(currentSubmission.id)
        : await generateIpmsConsolidationSuggestion(currentSubmission.id);
      if (!response.success) {
        setConsolidationError(response.message || response.data?.explanation || 'The suggestion could not be generated.');
        return;
      }
      await refreshConsolidation();
    } catch (error) {
      setConsolidationError(error instanceof Error ? error.message : 'The suggestion could not be generated.');
    } finally {
      setConsolidationBusy(false);
    }
  };

  const saveConsolidation = async () => {
    if (!currentSubmission.rowVersion || !consolidatedActual.trim()) return;
    setConsolidationBusy(true);
    setConsolidationError('');
    try {
      const payload = { actualPerformance: consolidatedActual.trim(), editReason: consolidationReason.trim() || undefined, rowVersion: currentSubmission.rowVersion };
      const response = submissionType === 'OPMS'
        ? await saveOpmsConsolidatedActual(currentSubmission.id, payload)
        : await saveIpmsConsolidatedActual(currentSubmission.id, payload);
      if (!response.success) {
        setConsolidationError(response.message || response.data?.explanation || 'The consolidated actual could not be saved.');
        return;
      }
      await refreshConsolidation();
      setConsolidationReason('');
    } catch (error) {
      setConsolidationError(error instanceof Error ? error.message : 'The consolidated actual could not be saved.');
    } finally {
      setConsolidationBusy(false);
    }
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
          {!isEditing && !currentSubmission.isDisabled && canEditSubmissionMembers ? (
            <Button variant="outline" size="sm" icon={<Edit2 className="w-4 h-4" />} onClick={() => setIsEditing(true)}>
              Edit
            </Button>
          ) : isEditing ? (
            <>
              <Button variant="outline" size="sm" onClick={() => setIsEditing(false)}>
                Cancel
              </Button>
              <Button variant="primary" size="sm" onClick={handleSave}>
                Save
              </Button>
            </>
          ) : null}
          {onWithdraw && !currentSubmission.isDisabled && (
            <Button variant="error" size="sm" onClick={() => setShowWithdrawal(true)}>
              Withdraw
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
                {currentSubmission.quarter} {currentSubmission.target.period.fiscalYear} · {canReadSubmitterIdentity ? (currentSubmission.submitter?.displayName ?? 'Unassigned') : 'Restricted'} · {currentSubmission.target.department.name}
              </p>
              <div className="flex flex-wrap gap-2">
                <Badge variant={getStatusBadgeVariant(currentSubmission.status)}>{statusLabels[currentSubmission.status]}</Badge>
                <Badge variant="primary">{currentSubmission.quarter}</Badge>
                <Badge variant="default">{submissionType} Submission</Badge>
                {currentSubmission.isDisabled ? <Badge variant="error">Withdrawn</Badge> : null}
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
                {canReadActual ? (currentSubmission.actualPerformance || formatValue(currentSubmission.actual, targetUnit)) : 'Restricted'}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Score</p>
              <p className="mt-1 text-base font-semibold text-secondary-900 dark:text-white">{canReadSubmitterScore ? scoreDisplay : 'Restricted'}</p>
            </div>
          </div>
        </div>

        <WorkflowRail status={currentSubmission.status} />
      </Card>
      <GovernedWithdrawalDialog
        isOpen={showWithdrawal}
        recordLabel={`${submissionType} submission`}
        onClose={() => setShowWithdrawal(false)}
        onConfirm={(reason) => {
          onWithdraw?.(reason);
          setShowWithdrawal(false);
        }}
      />

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
              {canReadActual && <Field
                label="Actual Performance"
                value={currentSubmission.actualPerformance ?? ''}
                editable={isEditing && canEditActual}
                onChange={(value) => updateDraftSubmission(current => ({ ...current, actualPerformance: value, actual: Number.isFinite(Number(value)) ? Number(value) : 0 }))}
              />}
              {canReadActual && <Field
                label="Actual Expenditure"
                value={actualExpenditure !== undefined ? actualExpenditure : ''}
                editable={isEditing && canEditActual}
                type="number"
                onChange={(value) => updateDraftSubmission(current => 'actualExpenditure' in current ? { ...current, actualExpenditure: Number(value || 0) } : current)}
              />}
              {canReadVariance && <Field label="Variance" value={formatVariance(variance)} />}
            </div>
          </Section>

          {isConsolidationPeriod && canReadActual && (
            <Section title="Governed Consolidation" icon={<Sparkles className="h-4 w-4" />}>
              <p className="text-sm text-secondary-600 dark:text-secondary-300">
                The system suggestion is calculated from submitted source quarters. It is retained permanently when the final actual is accepted or edited.
              </p>
              {consolidationError && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 px-3 py-2 text-sm text-error-700">{consolidationError}</div>}
              <div className="grid gap-4 md:grid-cols-3">
                <Field label="System Suggestion" value={currentSubmission.systemSuggestedActualPerformance || 'Not generated'} />
                <Field label="Final Actual" value={currentSubmission.actualPerformance || '-'} />
                <Field label="Suggestion Generated" value={formatDateTime(currentSubmission.suggestionGeneratedDate)} />
                <Field label="Edited" value={currentSubmission.wasSystemSuggestionEdited ? 'Yes' : 'No'} />
                {canReadSuggestionActor && <Field label="Suggestion Edited By" value={currentSubmission.suggestionEditedByName || currentSubmission.suggestionEditedByUserPublicId || '-'} />}
                {canReadSuggestionReason && <Field label="Edit Reason" value={currentSubmission.suggestionEditReason || '-'} wide />}
              </div>
              {canManageConsolidation && currentSubmission.baseState === 'IN_PROGRESS' && !currentSubmission.isDisabled && (
                <div className="space-y-3 rounded-lg border border-secondary-200 p-3 dark:border-secondary-700">
                  <div className="grid gap-3 md:grid-cols-2">
                    <label className="text-sm font-medium text-secondary-700 dark:text-secondary-200">
                      Final actual
                      <input aria-label="Final consolidated actual" className="mt-1 min-h-11 w-full rounded-lg border border-secondary-200 bg-white px-3 dark:border-secondary-700 dark:bg-secondary-800" value={consolidatedActual} onChange={event => setConsolidatedActual(event.target.value)} />
                    </label>
                    <label className="text-sm font-medium text-secondary-700 dark:text-secondary-200">
                      Reason when changing the suggestion
                      <input aria-label="Consolidation edit reason" className="mt-1 min-h-11 w-full rounded-lg border border-secondary-200 bg-white px-3 dark:border-secondary-700 dark:bg-secondary-800" value={consolidationReason} onChange={event => setConsolidationReason(event.target.value)} />
                    </label>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="outline" size="sm" disabled={consolidationBusy || Boolean(currentSubmission.systemSuggestedActualPerformance)} onClick={() => void generateConsolidation()}>
                      Generate suggestion
                    </Button>
                    <Button variant="primary" size="sm" disabled={consolidationBusy || !currentSubmission.systemSuggestedActualPerformance || !consolidatedActual.trim() || (consolidatedActual.trim() !== currentSubmission.systemSuggestedActualPerformance && !consolidationReason.trim())} onClick={() => void saveConsolidation()}>
                      Save final actual
                    </Button>
                  </div>
                </div>
              )}
              <div className="space-y-2">
                  <div className="flex flex-wrap items-end justify-between gap-3">
                    <div>
                      <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Suggestion history</h4>
                      <p className="text-xs text-secondary-500">{consolidationHistoryTotalCount} immutable event{consolidationHistoryTotalCount === 1 ? '' : 's'}</p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <input
                        aria-label="Search suggestion history"
                        className="min-h-10 rounded-lg border border-secondary-200 bg-white px-3 text-sm dark:border-secondary-700 dark:bg-secondary-800"
                        placeholder="Search history"
                        value={consolidationHistorySearchInput}
                        onChange={event => setConsolidationHistorySearchInput(event.target.value)}
                      />
                      <select
                        aria-label="Filter suggestion history by event type"
                        className="min-h-10 rounded-lg border border-secondary-200 bg-white px-3 text-sm dark:border-secondary-700 dark:bg-secondary-800"
                        value={consolidationHistoryEventType}
                        onChange={event => { setConsolidationHistoryPage(1); setConsolidationHistoryEventType(event.target.value); }}
                      >
                        <option value="">All event types</option>
                        <option value="Generated">Generated</option>
                        <option value="Accepted">Accepted</option>
                        <option value="Edited">Edited</option>
                      </select>
                    </div>
                  </div>
                  {consolidationHistoryError && <div role="alert" className="rounded-lg border border-error-200 bg-error-50 px-3 py-2 text-sm text-error-700">{consolidationHistoryError}</div>}
                  {consolidationHistoryBusy && <p className="text-sm text-secondary-500">Loading suggestion history…</p>}
                  {consolidationHistory.map(event => (
                    <div key={event.publicId} className="rounded-lg border border-secondary-200 px-3 py-2 text-sm dark:border-secondary-700">
                      <div className="flex flex-wrap justify-between gap-2"><span className="font-medium">{event.eventType}</span><span className="text-secondary-500">{formatDateTime(event.occurredAt)}</span></div>
                      <p className="mt-1 text-secondary-600 dark:text-secondary-300">Suggestion {event.systemSuggestedActualPerformance ?? '-'} · Final {event.actualPerformance ?? '-'} · Sources {event.sourcePeriods.join(', ') || '-'}</p>
                      {canReadSuggestionActor && (event.actorName || event.actorUserPublicId) && <p className="mt-1 text-secondary-600 dark:text-secondary-300">Actor: {event.actorName || event.actorUserPublicId}</p>}
                      {canReadSuggestionReason && event.reason && <p className="mt-1 text-secondary-600 dark:text-secondary-300">Reason: {event.reason}</p>}
                      {canReadSuggestionCorrelation && event.correlationId && <p className="mt-1 text-secondary-600 dark:text-secondary-300">Correlation: {event.correlationId}</p>}
                    </div>
                  ))}
                  {!consolidationHistoryBusy && !consolidationHistoryError && consolidationHistory.length === 0 && (
                    <p className="text-sm text-secondary-500">No suggestion history matches the current filters.</p>
                  )}
                  {consolidationHistoryTotalPages > 1 && (
                    <div className="flex items-center justify-between gap-3 text-sm">
                      <Button variant="outline" size="sm" aria-label="Previous suggestion history" disabled={consolidationHistoryPage <= 1 || consolidationHistoryBusy} onClick={() => setConsolidationHistoryPage(page => Math.max(1, page - 1))}>Previous</Button>
                      <span>Page {consolidationHistoryPage} of {consolidationHistoryTotalPages}</span>
                      <Button variant="outline" size="sm" aria-label="Next suggestion history" disabled={consolidationHistoryPage >= consolidationHistoryTotalPages || consolidationHistoryBusy} onClick={() => setConsolidationHistoryPage(page => page + 1)}>Next</Button>
                    </div>
                  )}
                </div>
            </Section>
          )}

          <Section title="Variance & Corrective Action" icon={<AlertTriangle className="h-4 w-4" />}>
            <div className="grid gap-4 md:grid-cols-3">
              {canReadVarianceReason && <Field
                label="Variance Reason"
                value={getVarianceReason(currentSubmission) || ''}
                editable={isEditing && canEditVarianceReason}
                onChange={(value) => updateDraftSubmission(current => 'varianceReason' in current ? { ...current, varianceReason: value } : current)}
              />}
              {canReadCorrectiveMeasure && <Field
                label="Corrective Measure"
                value={getCorrectiveMeasure(currentSubmission) || ''}
                wide
                editable={isEditing && canEditCorrectiveMeasure}
                onChange={(value) => updateDraftSubmission(current => 'correctiveMeasure' in current ? { ...current, correctiveMeasure: value } : current)}
              />}
              {canReadSubmitterScore && <Field label="Submitter Score" value={scoreDisplay} />}
              <Field label="Submitter Status" value={<Badge variant={getStatusBadgeVariant(currentSubmission.status)}>{statusLabels[currentSubmission.status]}</Badge>} />
            </div>
          </Section>
        </div>
      )}

      {activeTab === 'evidence' && (
        <Section title="Proof of Evidence" icon={<FileBadge className="h-4 w-4" />}>
          <div className="mb-4 flex flex-wrap items-end gap-2">
            <label className="text-xs text-secondary-600">Search
              <input aria-label="Search submission evidence" value={evidenceSearchInput} onChange={event => setEvidenceSearchInput(event.target.value)} className="block rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900" />
            </label>
            <label className="text-xs text-secondary-600">Scan status
              <select aria-label="Filter evidence scan status" value={evidenceScanStatus} onChange={event => { setEvidenceScanStatus(event.target.value); setEvidencePage(1); }} className="block rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900">
                <option value="">All</option><option value="Clean">Clean</option><option value="Pending">Pending</option><option value="ThreatDetected">Threat detected</option><option value="ScanFailed">Scan failed</option>
              </select>
            </label>
            <label className="text-xs text-secondary-600">Lifecycle
              <select aria-label="Filter evidence lifecycle" value={evidenceLifecycle} onChange={event => { setEvidenceLifecycle(event.target.value); setEvidencePage(1); }} className="block rounded border border-secondary-300 bg-white px-2 py-2 text-sm dark:border-secondary-700 dark:bg-secondary-900">
                <option value="">All</option><option value="active">Active</option><option value="retired">Retired</option>
              </select>
            </label>
            <span className="pb-2 text-xs text-secondary-500">{evidenceTotalCount} evidence records</span>
          </div>
          {evidenceError && <div role="alert" className="mb-3 rounded border border-error-200 bg-error-50 p-2 text-sm text-error-700">{evidenceError}</div>}
          {evidenceNotice && <div role="status" className="mb-3 rounded border border-success-200 bg-success-50 p-2 text-sm text-success-700">{evidenceNotice}</div>}
          <FileUpload
            existingFiles={uploadedFileItems}
            maxFiles={undefined}
            disabled={currentSubmission.isDisabled || evidenceBusy}
            onUpload={(files) => { void uploadEvidence(files); }}
            onRescan={(attachmentId) => { void mutateEvidence(() => submissionType === 'OPMS' ? rescanOpmsSubmissionAttachment(submission.id, attachmentId) : rescanIpmsSubmissionAttachment(submission.id, attachmentId), 'Evidence scan completed'); }}
            onAssess={security.canExecute(`${submissionType}_POE.ASSESS`) ? (attachmentId, outcome, comment) => { void mutateEvidence(() => submissionType === 'OPMS' ? assessOpmsSubmissionAttachment(submission.id, attachmentId, { outcome, comment }) : assessIpmsSubmissionAttachment(submission.id, attachmentId, { outcome, comment }), 'Evidence assessment recorded'); } : undefined}
            onReplace={security.canExecute(`${submissionType}_POE.REPLACE`) ? (attachmentId, replacementPublicId, reason, supersededRowVersion, replacementRowVersion) => { void mutateEvidence(() => submissionType === 'OPMS' ? replaceOpmsSubmissionAttachment(submission.id, attachmentId, { replacementEvidencePublicId: replacementPublicId, reason, supersededRowVersion, replacementRowVersion }) : replaceIpmsSubmissionAttachment(submission.id, attachmentId, { replacementEvidencePublicId: replacementPublicId, reason, supersededRowVersion, replacementRowVersion }), 'Evidence replacement recorded'); } : undefined}
            onPlaceHold={security.canExecute(`${submissionType}_POE.PLACE_HOLD`) ? (attachmentId, holdReference, reason) => { void mutateEvidence(() => submissionType === 'OPMS' ? placeOpmsEvidenceLegalHold(submission.id, attachmentId, { holdReference, reason }) : placeIpmsEvidenceLegalHold(submission.id, attachmentId, { holdReference, reason }), 'Legal hold placed'); } : undefined}
            onReleaseHold={security.canExecute(`${submissionType}_POE.RELEASE_HOLD`) ? (attachmentId, holdId, reason) => { void mutateEvidence(() => submissionType === 'OPMS' ? releaseOpmsEvidenceLegalHold(submission.id, attachmentId, holdId, { reason }) : releaseIpmsEvidenceLegalHold(submission.id, attachmentId, holdId, { reason }), 'Legal hold released'); } : undefined}
            onDispose={security.canExecute(`${submissionType}_POE.DISPOSE`) ? (attachmentId, approvalReference, reason, rowVersion) => { void mutateEvidence(() => submissionType === 'OPMS' ? requestOpmsEvidenceDisposal(submission.id, attachmentId, { approvalReference, reason, rowVersion }) : requestIpmsEvidenceDisposal(submission.id, attachmentId, { approvalReference, reason, rowVersion }), 'Evidence disposal queued'); } : undefined}
          />
          <div className="mt-3 flex items-center justify-between text-xs text-secondary-500"><span>{evidenceBusy ? 'Loading evidence…' : `Page ${evidencePage} of ${Math.max(1, evidenceTotalPages)}`}</span><div className="flex gap-2"><Button size="sm" variant="outline" disabled={evidenceBusy || evidencePage <= 1} onClick={() => setEvidencePage(value => value - 1)}>Previous evidence</Button><Button size="sm" variant="outline" disabled={evidenceBusy || evidencePage >= evidenceTotalPages} onClick={() => setEvidencePage(value => value + 1)}>Next evidence</Button></div></div>
        </Section>
      )}

      {activeTab === 'verification' && (
        <Section title="Verification" icon={<ClipboardCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            {canReadVerifierIdentity && <Field label="Verified By" value={getVerifier(currentSubmission)?.displayName || 'Pending'} />}
            <Field label="Verified At" value={formatDateTime(getVerifiedAt(currentSubmission))} />
            {canReadVerifierComment && <Field label="Verification Notes" value={getVerifierComments(currentSubmission) || '-'} wide />}
          </div>
        </Section>
      )}

      {activeTab === 'approval' && (
        <Section title="Approval" icon={<CheckCircle2 className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            {canReadApproverIdentity && <Field label="Approver" value={getApprover(currentSubmission)?.displayName || 'Pending'} />}
            <Field label="Approved At" value={formatDateTime(getApprovedAt(currentSubmission))} />
            {canReadApproverComment && <Field label="Approval Comment" value={getApproverComments(currentSubmission) || '-'} wide />}
          </div>
        </Section>
      )}

      {activeTab === 'pms' && (
        <Section title="PMS Section" icon={<Sparkles className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            {canReadPmsIdentity && <Field label="PMS Officer" value={getPmsOfficer(currentSubmission)?.displayName || 'Pending'} />}
            <Field label="Reviewed At" value={formatDateTime(getPmsReviewedAt(currentSubmission))} />
            {canReadPmsComment && <Field label="PMS Notes" value={getPmsComments(currentSubmission) || '-'} wide />}
          </div>
        </Section>
      )}

      {activeTab === 'auditor' && (
        <Section title="Auditor Information" icon={<ShieldCheck className="h-4 w-4" />}>
          <div className="grid gap-4 md:grid-cols-2">
            {canReadAuditorIdentity && <Field label="Auditor" value={getAuditor(currentSubmission)?.displayName || 'Pending'} />}
            {canReadAuditorObservation && <Field label="Audited At" value={formatDateTime(getAuditedAt(currentSubmission))} />}
            {canReadAuditorComment && <Field label="Findings" value={getAuditorComments(currentSubmission) || '-'} wide />}
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
          <PerformanceRfiWorkspace kind={submissionType === 'OPMS' ? 1 : 2} submissionId={currentSubmission.id} />
          <StageRatingHistory kind={submissionType === 'OPMS' ? 1 : 2} submissionId={currentSubmission.id} />
        </Section>
      )}

      {!currentSubmission.isDisabled && (onWorkflowAction || onExtendDueDate) && (
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
