import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { LegacySubmissionPeriodReconciliationAdministration } from './LegacySubmissionPeriodReconciliationAdministration';

const api = vi.hoisted(() => ({
  getLegacySubmissionPeriodPreview: vi.fn(),
  executeLegacySubmissionPeriodReconciliation: vi.fn(),
}));
const pushToast = vi.fn();
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: ['WORKFLOW.RECONCILE_LEGACY_PERIODS'], pushToast }) }));

describe('LegacySubmissionPeriodReconciliationAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getLegacySubmissionPeriodPreview.mockResolvedValue({ success: true, data: {
      page: 1, pageSize: 25, totalCount: 2, totalPages: 1, items: [
        {
          submissionKind: 1, submissionPublicId: 'submission-1', indicatorNumber: 'KPI-1', targetName: 'Ready target',
          legacyQuarter: 'Quarter 1', status: 'Ready', message: 'One exact target-scoped period is available.', rowVersion: 'AQID',
          candidates: [{ reportingPeriodPublicId: 'period-1', code: 'Q1', name: 'Quarter 1', periodType: 1, financialYearCode: '2040/41', hasSubmissionConflict: false }],
        },
        {
          submissionKind: 1, submissionPublicId: 'submission-2', indicatorNumber: 'KPI-2', targetName: 'Duplicate target',
          legacyQuarter: 'Q1', status: 'Blocked', message: 'Every matching period already has another logical submission for this target.', rowVersion: 'BAUG',
          candidates: [{ reportingPeriodPublicId: 'period-2', code: 'Q1', name: 'Quarter 1', periodType: 1, financialYearCode: '2040/41', hasSubmissionConflict: true }],
        },
      ],
    } });
    api.executeLegacySubmissionPeriodReconciliation.mockResolvedValue({ success: true, data: {
      reconciliationPublicId: 'reconciliation-1', submissionPublicId: 'submission-1', reportingPeriodPublicId: 'period-1', reportingPeriodCode: 'Q1',
    } });
  });

  it('requires an explicit eligible period and governance reason before reconciliation', async () => {
    render(<LegacySubmissionPeriodReconciliationAdministration />);
    await waitFor(() => expect(api.getLegacySubmissionPeriodPreview).toHaveBeenCalledWith(1, 1, 25));
    expect(await screen.findByText('Every matching period already has another logical submission for this target.')).toBeInTheDocument();
    expect(screen.queryByLabelText('Canonical period for KPI-2')).not.toBeInTheDocument();
    const execute = screen.getByRole('button', { name: 'Reconcile KPI-1' });
    expect(execute).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Canonical period for KPI-1'), { target: { value: 'period-1' } });
    fireEvent.change(screen.getByLabelText('Period reconciliation reason'), { target: { value: 'Approved historical mapping CR-100' } });
    fireEvent.click(execute);

    await waitFor(() => expect(api.executeLegacySubmissionPeriodReconciliation).toHaveBeenCalledWith({
      submissionKind: 1,
      submissionPublicId: 'submission-1',
      reportingPeriodPublicId: 'period-1',
      reason: 'Approved historical mapping CR-100',
      rowVersion: 'AQID',
    }));
    expect(pushToast).toHaveBeenCalledWith('success', 'Submission linked to canonical period Q1');
  });

  it('loads OPMS and IPMS registers independently', async () => {
    render(<LegacySubmissionPeriodReconciliationAdministration />);
    await screen.findByText('KPI-1 · Ready target');
    fireEvent.change(screen.getByLabelText('Submission register'), { target: { value: '2' } });
    await waitFor(() => expect(api.getLegacySubmissionPeriodPreview).toHaveBeenLastCalledWith(2, 1, 25));
  });
});
