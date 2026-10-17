import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { WorkflowActionHistory } from './WorkflowActionHistory';

const api = vi.hoisted(() => ({ getSubmissionWorkflowActionsPage: vi.fn() }));
const security = vi.hoisted(() => ({ canReadField: vi.fn(() => true) }));
vi.mock('../../api/api', () => api);
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('WorkflowActionHistory', () => {
  beforeEach(() => security.canReadField.mockReturnValue(true));

  it('renders the ordered append-only action ledger and pages it', async () => {
    api.getSubmissionWorkflowActionsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'action-1', sequence: 2, actionCode: 'OPMS_SUBMISSION.VERIFY', outcome: 7,
      actorUserPublicId: 'actor-1', actorName: 'Independent Verifier', comment: 'Evidence verified',
      occurredAt: '2026-10-17T12:31:15Z',
    }], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 } });

    render(<WorkflowActionHistory kind={1} submissionId="submission-1" />);

    expect(await screen.findByText('2. VERIFY')).toBeInTheDocument();
    expect(screen.getByText(/Completed · Independent Verifier/)).toBeInTheDocument();
    expect(screen.getByText('Evidence verified')).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 2 · 11 actions')).toBeInTheDocument();
    expect(api.getSubmissionWorkflowActionsPage).toHaveBeenCalledWith(1, 'submission-1', { page: 1, pageSize: 10, sortBy: 'sequence', sortDirection: 'asc' });

    fireEvent.click(screen.getByRole('button', { name: 'Next actions' }));
    await waitFor(() => expect(api.getSubmissionWorkflowActionsPage).toHaveBeenLastCalledWith(1, 'submission-1', { page: 2, pageSize: 10, sortBy: 'sequence', sortDirection: 'asc' }));
  });

  it('does not render protected actor, comment, or rating members without grants', async () => {
    security.canReadField.mockReturnValue(false);
    api.getSubmissionWorkflowActionsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'action-secret', sequence: 3, actionCode: 'OPMS_SUBMISSION.APPROVE', outcome: 'Complete',
      actorUserPublicId: 'actor-secret', actorName: 'Secret Approver', comment: 'Secret comment', ratingValue: 2,
      occurredAt: '2026-10-17T12:33:44Z',
    }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });

    render(<WorkflowActionHistory kind={1} submissionId="submission-1" />);

    expect(await screen.findByText('3. APPROVE')).toBeInTheDocument();
    expect(screen.getByText(/Completed/)).toBeInTheDocument();
    expect(screen.queryByText(/Secret Approver|actor-secret/)).not.toBeInTheDocument();
    expect(screen.queryByText('Secret comment')).not.toBeInTheDocument();
    expect(screen.queryByText('Rating 2')).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('OPMS_WORKFLOW', 'ActionComment');
  });
});
