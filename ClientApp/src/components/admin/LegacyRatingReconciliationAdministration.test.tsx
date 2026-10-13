import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { LegacyRatingReconciliationAdministration } from './LegacyRatingReconciliationAdministration';

const api = vi.hoisted(() => ({
  getLegacyRatingReconciliationPreview: vi.fn(),
  executeLegacyRatingReconciliation: vi.fn(),
}));
const pushToast = vi.fn();
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: ['WORKFLOW.RECONCILE_LEGACY_RATINGS'], pushToast }),
}));

describe('LegacyRatingReconciliationAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getLegacyRatingReconciliationPreview.mockResolvedValue({
      success: true,
      data: {
        page: 1,
        pageSize: 25,
        totalCount: 2,
        totalPages: 1,
        items: [
          {
            submissionKind: 1,
            submissionPublicId: '11111111-1111-4111-8111-111111111111',
            sourceKind: 1,
            sourceKey: 'GENERIC:source-1',
            sourceScore: 4,
            sourceActorName: 'Historical Verifier',
            sourceOccurredAt: '2038-08-02T09:30:00Z',
            status: 'RequiresMapping',
            statusReason: 'Two exact configured actions require explicit selection.',
            candidates: [
              { workflowActionPublicId: 'action-1', actionCode: 'VERIFY', stageCode: 'VERIFY', stageName: 'Verify', ratingSchemePublicId: 'scheme-1', ratingSchemeCode: 'FIVE', ratingValuePublicId: 'value-1', ratingValue: 4, ratingLabel: 'Achieved', actorName: 'Historical Verifier', occurredAt: '2038-08-02T09:30:00Z', hasMatchingCanonicalRating: false },
              { workflowActionPublicId: 'action-2', actionCode: 'APPROVE', stageCode: 'APPROVE', stageName: 'Approve', ratingSchemePublicId: 'scheme-1', ratingSchemeCode: 'FIVE', ratingValuePublicId: 'value-1', ratingValue: 4, ratingLabel: 'Achieved', actorName: 'Historical Verifier', occurredAt: '2038-08-02T10:30:00Z', hasMatchingCanonicalRating: true },
            ],
          },
          {
            submissionKind: 1,
            submissionPublicId: '22222222-2222-4222-8222-222222222222',
            sourceKind: 1,
            sourceKey: 'GENERIC:source-2',
            sourceScore: 3,
            sourceActorName: 'Unavailable historical actor',
            status: 'Blocked',
            statusReason: 'No same-actor exact configured value exists.',
            candidates: [],
          },
        ],
      },
    });
    api.executeLegacyRatingReconciliation.mockResolvedValue({
      success: true,
      data: { reconciliationPublicId: 'reconciliation-1', stageRatingPublicId: 'rating-1', usedExistingRating: true },
    });
  });

  it('never maps automatically and executes only the explicitly selected action with a reason', async () => {
    render(<LegacyRatingReconciliationAdministration />);
    await waitFor(() => expect(api.getLegacyRatingReconciliationPreview).toHaveBeenCalledWith(1, 1, 1, 25));
    expect(api.executeLegacyRatingReconciliation).not.toHaveBeenCalled();
    expect(screen.getByText('No same-actor exact configured value exists.')).toBeInTheDocument();

    const execute = screen.getByRole('button', { name: 'Reconcile this source' });
    expect(execute).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Evidenced workflow action for 11111111-1111-4111-8111-111111111111'), { target: { value: 'action-2' } });
    expect(execute).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Reconciliation reason'), { target: { value: 'Approved historical evidence mapping CR-99' } });
    fireEvent.click(execute);

    await waitFor(() => expect(api.executeLegacyRatingReconciliation).toHaveBeenCalledWith({
      submissionKind: 1,
      sourceKind: 1,
      sourceKey: 'GENERIC:source-1',
      workflowActionPublicId: 'action-2',
      reason: 'Approved historical evidence mapping CR-99',
    }));
    expect(pushToast).toHaveBeenCalledWith('success', 'Historical score linked to existing canonical rating');
  });

  it('loads each selected register and source independently', async () => {
    render(<LegacyRatingReconciliationAdministration />);
    await waitFor(() => expect(api.getLegacyRatingReconciliationPreview).toHaveBeenCalledWith(1, 1, 1, 25));
    fireEvent.change(screen.getByLabelText('Submission register'), { target: { value: '2' } });
    await waitFor(() => expect(api.getLegacyRatingReconciliationPreview).toHaveBeenLastCalledWith(2, 1, 1, 25));
    fireEvent.change(screen.getByLabelText('Legacy score source'), { target: { value: '5' } });
    await waitFor(() => expect(api.getLegacyRatingReconciliationPreview).toHaveBeenLastCalledWith(2, 5, 1, 25));
  });
});
