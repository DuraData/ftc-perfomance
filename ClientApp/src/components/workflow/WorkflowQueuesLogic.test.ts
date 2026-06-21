import { describe, it, expect } from 'vitest';

describe('Workflow queue state and filtering', () => {
  type SubmissionStatus = 
    | 'draft'
    | 'submitted'
    | 'pending_verification'
    | 'verified'
    | 'pending_approval'
    | 'approved'
    | 'completed'
    | 'returned_for_info'
    | 'reviewed'
    | 'audited'
    | 'verify_rejected'
    | 'rejected';

  interface QueueSubmission {
    id: string;
    status: SubmissionStatus;
    quarter: string;
    targetName: string;
    dueDate: Date;
  }

  const filterByPath = (submissions: QueueSubmission[], currentPath: string): QueueSubmission[] => {
    const pathFilterMap: Record<string, SubmissionStatus[]> = {
      '/workflow/my-drafts': ['draft'],
      '/workflow/pending-submission': ['submitted'],
      '/workflow/returned-submissions': ['returned_for_info', 'verify_rejected', 'rejected'],
      '/workflow/under-verification': ['pending_verification'],
      '/workflow/under-review': ['reviewed'],
      '/workflow/under-approval': ['pending_approval', 'verified'],
      '/workflow/internal-audit-returned': ['audited'],
      '/workflow/approved-closed': ['approved', 'completed'],
    };

    const expectedStatuses = pathFilterMap[currentPath];
    if (!expectedStatuses) return submissions;
    return submissions.filter(item => expectedStatuses.includes(item.status));
  };

  it('filters submissions by my-drafts queue', () => {
    const submissions: QueueSubmission[] = [
      { id: '1', status: 'draft', quarter: 'Q1', targetName: 'Target A', dueDate: new Date() },
      { id: '2', status: 'submitted', quarter: 'Q1', targetName: 'Target B', dueDate: new Date() },
      { id: '3', status: 'draft', quarter: 'Q2', targetName: 'Target C', dueDate: new Date() },
    ];

    const result = filterByPath(submissions, '/workflow/my-drafts');
    expect(result).toHaveLength(2);
    expect(result.every(s => s.status === 'draft')).toBe(true);
  });

  it('filters submissions by pending-submission queue', () => {
    const submissions: QueueSubmission[] = [
      { id: '1', status: 'draft', quarter: 'Q1', targetName: 'Target A', dueDate: new Date() },
      { id: '2', status: 'submitted', quarter: 'Q1', targetName: 'Target B', dueDate: new Date() },
      { id: '3', status: 'submitted', quarter: 'Q2', targetName: 'Target C', dueDate: new Date() },
    ];

    const result = filterByPath(submissions, '/workflow/pending-submission');
    expect(result).toHaveLength(2);
    expect(result.every(s => s.status === 'submitted')).toBe(true);
  });

  it('filters returned submissions including multiple rejection states', () => {
    const submissions: QueueSubmission[] = [
      { id: '1', status: 'returned_for_info', quarter: 'Q1', targetName: 'Target A', dueDate: new Date() },
      { id: '2', status: 'verify_rejected', quarter: 'Q1', targetName: 'Target B', dueDate: new Date() },
      { id: '3', status: 'rejected', quarter: 'Q2', targetName: 'Target C', dueDate: new Date() },
      { id: '4', status: 'submitted', quarter: 'Q2', targetName: 'Target D', dueDate: new Date() },
    ];

    const result = filterByPath(submissions, '/workflow/returned-submissions');
    expect(result).toHaveLength(3);
    expect(result.some(s => s.status === 'returned_for_info')).toBe(true);
    expect(result.some(s => s.status === 'verify_rejected')).toBe(true);
    expect(result.some(s => s.status === 'rejected')).toBe(true);
  });

  it('counts submissions by status', () => {
    const submissions: QueueSubmission[] = [
      { id: '1', status: 'draft', quarter: 'Q1', targetName: 'Target A', dueDate: new Date() },
      { id: '2', status: 'submitted', quarter: 'Q1', targetName: 'Target B', dueDate: new Date() },
      { id: '3', status: 'pending_verification', quarter: 'Q1', targetName: 'Target C', dueDate: new Date() },
      { id: '4', status: 'draft', quarter: 'Q2', targetName: 'Target D', dueDate: new Date() },
      { id: '5', status: 'approved', quarter: 'Q2', targetName: 'Target E', dueDate: new Date() },
    ];

    const count = (statuses: SubmissionStatus[]) =>
      submissions.filter(item => statuses.includes(item.status)).length;

    expect(count(['draft'])).toBe(2);
    expect(count(['submitted'])).toBe(1);
    expect(count(['pending_verification'])).toBe(1);
    expect(count(['approved'])).toBe(1);
    expect(count(['draft', 'submitted'])).toBe(3);
  });

  it('handles unknown queue paths gracefully', () => {
    const submissions: QueueSubmission[] = [
      { id: '1', status: 'draft', quarter: 'Q1', targetName: 'Target A', dueDate: new Date() },
      { id: '2', status: 'submitted', quarter: 'Q1', targetName: 'Target B', dueDate: new Date() },
    ];

    const result = filterByPath(submissions, '/unknown/path');
    expect(result).toEqual(submissions);
  });

  it('determines current reviewer based on submission status', () => {
    interface ReviewerInfo {
      displayName?: string;
    }

    interface SubmissionWithReviewers extends QueueSubmission {
      verifier?: ReviewerInfo;
      approver?: ReviewerInfo;
    }

    const getCurrentReviewer = (submission: SubmissionWithReviewers): string => {
      return submission.verifier?.displayName ?? submission.approver?.displayName ?? 'Pending';
    };

    const submission1: SubmissionWithReviewers = {
      id: '1',
      status: 'pending_verification',
      quarter: 'Q1',
      targetName: 'Target A',
      dueDate: new Date(),
      verifier: { displayName: 'John Verifier' },
    };

    expect(getCurrentReviewer(submission1)).toBe('John Verifier');

    const submission2: SubmissionWithReviewers = {
      id: '2',
      status: 'pending_approval',
      quarter: 'Q1',
      targetName: 'Target B',
      dueDate: new Date(),
      approver: { displayName: 'Jane Approver' },
    };

    expect(getCurrentReviewer(submission2)).toBe('Jane Approver');

    const submission3: SubmissionWithReviewers = {
      id: '3',
      status: 'submitted',
      quarter: 'Q1',
      targetName: 'Target C',
      dueDate: new Date(),
    };

    expect(getCurrentReviewer(submission3)).toBe('Pending');
  });
});
