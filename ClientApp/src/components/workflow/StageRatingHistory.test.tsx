import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StageRatingHistory } from './StageRatingHistory';

const api = vi.hoisted(() => ({ getSubmissionStageRatingsPage: vi.fn() }));
const security = vi.hoisted(() => ({ canReadField: vi.fn(() => true) }));
vi.mock('../../api/api', () => api);
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('StageRatingHistory', () => {
  beforeEach(() => security.canReadField.mockReturnValue(true));

  it('renders the authoritative scheme and snapshot label', async () => {
    api.getSubmissionStageRatingsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'rating-1', workflowActionPublicId: 'action-1', stageCode: 'PMS_REVIEW',
      ratingSchemePublicId: 'scheme-1', ratingSchemeCode: 'FIVE_POINT', ratingValuePublicId: 'value-4',
      value: 4, label: 'Exceeded', comment: 'Evidence supports the score', ratedByUserId: 'reviewer',
      ratedByName: 'PMS Reviewer', ratedAt: '2026-10-02T08:00:00Z',
    }], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 } });

    render(<StageRatingHistory kind={1} submissionId="submission-1" />);

    expect(await screen.findByText('PMS_REVIEW · Exceeded')).toBeInTheDocument();
    expect(screen.getByText(/FIVE_POINT · PMS Reviewer/)).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 2 · 11 ratings')).toBeInTheDocument();
    expect(api.getSubmissionStageRatingsPage).toHaveBeenCalledWith(1, 'submission-1', { page: 1, pageSize: 10, sortBy: 'ratedAt', sortDirection: 'desc' });

    fireEvent.click(screen.getByRole('button', { name: 'Next ratings' }));
    await waitFor(() => expect(api.getSubmissionStageRatingsPage).toHaveBeenLastCalledWith(1, 'submission-1', { page: 2, pageSize: 10, sortBy: 'ratedAt', sortDirection: 'desc' }));
  });

  it('does not render hostile protected rating values without member grants', async () => {
    security.canReadField.mockReturnValue(false);
    api.getSubmissionStageRatingsPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'hostile-rating', workflowActionPublicId: 'hostile-action', stageCode: 'INTERNAL_AUDIT',
      ratingSchemePublicId: 'scheme-secret', ratingSchemeCode: 'AUDIT_SCHEME', ratingValuePublicId: 'value-secret',
      value: 1, label: 'Secret rating label', achievementPercent: 12, comment: 'Secret audit comment',
      ratedByUserId: 'secret-auditor', ratedByName: 'Secret Auditor', ratedAt: '2026-10-02T08:00:00Z',
    }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });

    render(<StageRatingHistory kind={1} submissionId="submission-1" />);

    expect(await screen.findByText('INTERNAL_AUDIT')).toBeInTheDocument();
    expect(screen.getByText(/AUDIT_SCHEME/)).toBeInTheDocument();
    expect(screen.queryByText('Secret rating label')).not.toBeInTheDocument();
    expect(screen.queryByText('Secret audit comment')).not.toBeInTheDocument();
    expect(screen.queryByText(/Secret Auditor|secret-auditor/)).not.toBeInTheDocument();
    expect(screen.queryByText('1')).not.toBeInTheDocument();
    expect(screen.queryByText(/Achievement: 12%/)).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('OPMS_WORKFLOW', 'StageRatingValue');
  });
});
