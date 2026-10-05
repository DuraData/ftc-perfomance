import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StageRatingHistory } from './StageRatingHistory';

const api = vi.hoisted(() => ({ getSubmissionStageRatingsPage: vi.fn() }));
vi.mock('../../api/api', () => api);

describe('StageRatingHistory', () => {
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
});
