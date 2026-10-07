import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { MyWorkQueue, WorkflowQueues } from './WorkflowQueues';

const apiMocks = vi.hoisted(() => ({
  getWorkflowQueue: vi.fn(),
  getOpmsSubmission: vi.fn(),
  getIpmsSubmission: vi.fn(),
  getInternalAuditSubmission: vi.fn(),
  saveInternalAuditAssessment: vi.fn(),
}));

const appMocks = vi.hoisted(() => ({
  pushToast: vi.fn(),
  currentPath: '/workflow/verification',
  userProfile: { id: 'submitter-live' },
}));
const securityMocks = vi.hoisted(() => ({ canReadField: vi.fn() }));

vi.mock('../../api/api', () => apiMocks);
vi.mock('../../context/AppContext', () => ({ useApp: () => appMocks }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => securityMocks }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));

describe('WorkflowQueues', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    appMocks.currentPath = '/workflow/verification';
    securityMocks.canReadField.mockReturnValue(true);
  });

  it('renders queue counts and rows from the authorised API response', async () => {
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'verification',
        counts: {
          mySubmissions: 1, verification: 1, approval: 0, pms: 0, auditor: 0, returned: 0,
          myDrafts: 0, pendingSubmission: 0, myReturned: 0, underVerification: 1, underReview: 0,
          underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
        },
        page: {
          items: [{
            id: 'submission-live', publicId: 'submission-public', kind: 'opms', targetId: 'target-live',
            targetPublicId: 'target-public', targetName: 'Live Water KPI', indicatorNumber: 'LIVE-001',
            quarter: 'Q1', dueDate: '2026-10-15T00:00:00Z', status: 'pending_verification',
            submittedByUserPublicId: 'submitter-live', submittedByName: 'Live Submitter', createdAt: '2026-10-01T00:00:00Z',
          }],
          page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
        },
      },
    });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Live Water KPI')).toBeInTheDocument();
    expect(screen.getByText('LIVE-001')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /1\s+Pending Verification/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /0\s+Pending Approval/i })).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1);

    apiMocks.getOpmsSubmission.mockResolvedValue({ success: false });
    fireEvent.click(screen.getByText('Live Water KPI'));
    await waitFor(() => expect(apiMocks.getOpmsSubmission).toHaveBeenCalledWith('submission-live'));
  });

  it('loads personal returned work through the bounded combined queue', async () => {
    appMocks.currentPath = '/workflow/returned-submissions';
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'my-returned',
        counts: {
          mySubmissions: 1, verification: 0, approval: 0, pms: 0, auditor: 0, returned: 4,
          myDrafts: 0, pendingSubmission: 0, myReturned: 1, underVerification: 0, underReview: 0,
          underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
        },
        page: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
      },
    });

    render(<MyWorkQueue />);

    expect(await screen.findByText('Returned Submissions')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('my-returned', 1);
  });

  it('shows an explicit service error instead of fixture submissions', async () => {
    apiMocks.getWorkflowQueue.mockResolvedValue({ success: false, message: 'Authorised queue request failed.' });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Workflow queue unavailable')).toBeInTheDocument();
    expect(screen.getByText('Authorised queue request failed.')).toBeInTheDocument();
    expect(appMocks.pushToast).toHaveBeenCalledWith('error', 'Authorised queue request failed.');
  });

  it('does not render a hostile queue actor payload without the submission member grant', async () => {
    securityMocks.canReadField.mockReturnValue(false);
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'verification', counts: { ...emptyCountsForTest(), verification: 1 },
        page: { items: [{
          id: 'secret-submission', publicId: 'secret-public', kind: 'opms', targetId: 'target-secret',
          targetPublicId: 'target-public', targetName: 'Protected KPI', indicatorNumber: 'SEC-1', quarter: 'Q1',
          status: 'pending_verification', submittedByUserPublicId: 'actor-public', submittedByName: 'Secret Queue Actor',
          createdAt: '2026-10-01T00:00:00Z',
        }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
      },
    });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Protected KPI')).toBeInTheDocument();
    expect(screen.queryByText('Secret Queue Actor')).not.toBeInTheDocument();
    expect(screen.getByText('Restricted')).toBeInTheDocument();
    expect(securityMocks.canReadField).toHaveBeenCalledWith('OPMS_SUBMISSION', 'SubmitterIdentity');
  });
});

function emptyCountsForTest() {
  return {
    mySubmissions: 0, verification: 0, approval: 0, pms: 0, auditor: 0, returned: 0,
    myDrafts: 0, pendingSubmission: 0, myReturned: 0, underVerification: 0, underReview: 0,
    underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
  };
}
