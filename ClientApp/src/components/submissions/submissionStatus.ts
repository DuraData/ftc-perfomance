import type { SubmissionStatus } from '../../types';

export const submissionStatusLabels: Record<SubmissionStatus, string> = {
  draft: 'Draft',
  submitted: 'Submitted',
  pending_verification: 'Pending Verification',
  verified: 'Verified',
  verify_rejected: 'Verification Rejected',
  pending_approval: 'Pending Approval',
  approved: 'Approved',
  rejected: 'Rejected',
  reviewed: 'Reviewed',
  returned_for_info: 'Returned for Information',
  audited: 'Audited',
  completed: 'Completed',
};

export const submissionStatusColors: Record<SubmissionStatus, string> = {
  draft: 'bg-secondary-100 text-secondary-800',
  submitted: 'bg-blue-100 text-blue-800',
  pending_verification: 'bg-amber-100 text-amber-800',
  verified: 'bg-cyan-100 text-cyan-800',
  verify_rejected: 'bg-rose-100 text-rose-800',
  pending_approval: 'bg-orange-100 text-orange-800',
  approved: 'bg-green-100 text-green-800',
  rejected: 'bg-red-100 text-red-800',
  reviewed: 'bg-indigo-100 text-indigo-800',
  returned_for_info: 'bg-yellow-100 text-yellow-800',
  audited: 'bg-violet-100 text-violet-800',
  completed: 'bg-emerald-100 text-emerald-800',
};
