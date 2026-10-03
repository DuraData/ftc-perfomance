import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { WorkflowQueues } from './WorkflowQueues';

const apiMocks = vi.hoisted(() => ({
  getOpmsSubmissions: vi.fn(),
  getIpmsSubmissions: vi.fn(),
  getInternalAuditSubmission: vi.fn(),
  saveInternalAuditAssessment: vi.fn(),
}));

const appMocks = vi.hoisted(() => ({
  pushToast: vi.fn(),
  currentPath: '/workflow/verification',
  userProfile: { id: 'submitter-live' },
}));

vi.mock('../../api/api', () => apiMocks);
vi.mock('../../context/AppContext', () => ({ useApp: () => appMocks }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));

describe('WorkflowQueues', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders queue counts and rows from the authorised API response', async () => {
    apiMocks.getOpmsSubmissions.mockResolvedValue({
      success: true,
      data: [{
        id: 'submission-live',
        baseState: 'SUBMITTED',
        rowVersion: 'AQ==',
        target: { id: 'target-live', targetName: 'Live Water KPI', indicatorNumber: 'LIVE-001' },
        quarter: 'Q1',
        dueDate: '2026-10-15T00:00:00Z',
        actual: 5,
        status: 'pending_verification',
        submittedByUserId: 'submitter-live',
        submitter: { id: 'submitter-live', displayName: 'Live Submitter' },
        attachments: [],
        comments: [],
        history: [],
      }],
    });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Live Water KPI')).toBeInTheDocument();
    expect(screen.getByText('LIVE-001')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /1\s+Pending Verification/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /0\s+Pending Approval/i })).toBeInTheDocument();
    expect(apiMocks.getOpmsSubmissions).toHaveBeenCalledTimes(1);
  });

  it('shows an explicit service error instead of fixture submissions', async () => {
    apiMocks.getOpmsSubmissions.mockResolvedValue({ success: false, message: 'Authorised queue request failed.' });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Workflow queue unavailable')).toBeInTheDocument();
    expect(screen.getByText('Authorised queue request failed.')).toBeInTheDocument();
    expect(appMocks.pushToast).toHaveBeenCalledWith('error', 'Authorised queue request failed.');
  });
});
