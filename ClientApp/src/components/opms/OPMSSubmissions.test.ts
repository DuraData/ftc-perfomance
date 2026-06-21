import { describe, it, expect } from 'vitest';

describe('Submission form validation and handling', () => {
  interface SubmissionFormData {
    targetId: string;
    quarter: string;
    actual?: number;
    actualDescription?: string;
    varianceReason?: string;
    actualExpenditure?: number;
  }

  const requiredFields = ['targetId', 'quarter'];

  const validateForm = (data: SubmissionFormData): string[] => {
    const errors: string[] = [];

    requiredFields.forEach(field => {
      if (!data[field as keyof SubmissionFormData]) {
        errors.push(`${field} is required`);
      }
    });

    if (data.actual !== undefined && data.actual < 0) {
      errors.push('Actual value cannot be negative');
    }

    if (data.actualExpenditure !== undefined && data.actualExpenditure < 0) {
      errors.push('Actual expenditure cannot be negative');
    }

    return errors;
  };

  it('validates required submission fields', () => {
    const validForm: SubmissionFormData = {
      targetId: 'target-1',
      quarter: 'Q1',
      actual: 100,
    };

    expect(validateForm(validForm)).toHaveLength(0);
  });

  it('rejects submission with missing targetId', () => {
    const invalidForm: SubmissionFormData = {
      targetId: '',
      quarter: 'Q1',
    };

    const errors = validateForm(invalidForm);
    expect(errors).toContain('targetId is required');
  });

  it('rejects submission with missing quarter', () => {
    const invalidForm: SubmissionFormData = {
      targetId: 'target-1',
      quarter: '',
    };

    const errors = validateForm(invalidForm);
    expect(errors).toContain('quarter is required');
  });

  it('rejects negative actual values', () => {
    const invalidForm: SubmissionFormData = {
      targetId: 'target-1',
      quarter: 'Q1',
      actual: -50,
    };

    const errors = validateForm(invalidForm);
    expect(errors).toContain('Actual value cannot be negative');
  });

  it('rejects negative expenditure values', () => {
    const invalidForm: SubmissionFormData = {
      targetId: 'target-1',
      quarter: 'Q1',
      actualExpenditure: -1000,
    };

    const errors = validateForm(invalidForm);
    expect(errors).toContain('Actual expenditure cannot be negative');
  });

  it('calculates variance percentage', () => {
    const calculateVariance = (target: number, actual: number): number | null => {
      if (target === 0) return null;
      return ((actual - target) / target) * 100;
    };

    expect(calculateVariance(100, 120)).toBeCloseTo(20);
    expect(calculateVariance(100, 100)).toBeCloseTo(0);
    expect(calculateVariance(100, 80)).toBeCloseTo(-20);
    expect(calculateVariance(0, 50)).toBeNull();
  });

  it('tracks submission status transitions', () => {
    type SubmissionStatus = 
      | 'draft'
      | 'submitted'
      | 'pending_verification'
      | 'verified'
      | 'pending_approval'
      | 'approved'
      | 'completed'
      | 'returned_for_info';

    const statusTransitions: Record<SubmissionStatus, SubmissionStatus[]> = {
      draft: ['submitted'],
      submitted: ['pending_verification', 'returned_for_info'],
      pending_verification: ['verified', 'returned_for_info'],
      verified: ['pending_approval', 'returned_for_info'],
      pending_approval: ['approved', 'returned_for_info'],
      approved: ['completed'],
      completed: [],
      returned_for_info: ['submitted'],
    };

    expect(statusTransitions.draft).toContain('submitted');
    expect(statusTransitions.submitted).toContain('pending_verification');
    expect(statusTransitions.approved).toContain('completed');
    expect(statusTransitions.completed).toHaveLength(0);
  });

  it('validates due date against submission date', () => {
    const validateDueDate = (dueDate: Date, submissionDate: Date): boolean => {
      return submissionDate <= dueDate;
    };

    const dueDate = new Date('2026-06-30');
    expect(validateDueDate(dueDate, new Date('2026-06-25'))).toBe(true);
    expect(validateDueDate(dueDate, new Date('2026-07-05'))).toBe(false);
  });

  it('calculates days overdue', () => {
    const calculateDaysOverdue = (dueDate: Date): number => {
      const now = new Date();
      const diffMs = now.getTime() - dueDate.getTime();
      return Math.floor(diffMs / (1000 * 60 * 60 * 24));
    };

    const pastDue = new Date();
    pastDue.setDate(pastDue.getDate() - 5);

    const daysOverdue = calculateDaysOverdue(pastDue);
    expect(daysOverdue).toBeGreaterThanOrEqual(5);
  });
});
