import { render, screen } from '@testing-library/react';
import { StageRatingHistory } from './StageRatingHistory';

const api = vi.hoisted(() => ({ getSubmissionStageRatings: vi.fn() }));
vi.mock('../../api/api', () => api);

describe('StageRatingHistory', () => {
  it('renders the authoritative scheme and snapshot label', async () => {
    api.getSubmissionStageRatings.mockResolvedValue({ success: true, data: [{
      publicId: 'rating-1', workflowActionPublicId: 'action-1', stageCode: 'PMS_REVIEW',
      ratingSchemePublicId: 'scheme-1', ratingSchemeCode: 'FIVE_POINT', ratingValuePublicId: 'value-4',
      value: 4, label: 'Exceeded', comment: 'Evidence supports the score', ratedByUserId: 'reviewer',
      ratedByName: 'PMS Reviewer', ratedAt: '2026-10-02T08:00:00Z',
    }] });

    render(<StageRatingHistory kind={1} submissionId="submission-1" />);

    expect(await screen.findByText('PMS_REVIEW · Exceeded')).toBeInTheDocument();
    expect(screen.getByText(/FIVE_POINT · PMS Reviewer/)).toBeInTheDocument();
    expect(api.getSubmissionStageRatings).toHaveBeenCalledWith(1, 'submission-1');
  });
});
