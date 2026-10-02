import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PerformancePeriodTargetEditor } from './PerformancePeriodTargetEditor';

const api = vi.hoisted(() => ({
  getReportingPeriodMasters: vi.fn(),
  getPerformancePeriodTargets: vi.fn(),
  getPerformanceTargetRevisions: vi.fn(),
  createPerformancePeriodTarget: vi.fn(),
  revisePerformancePeriodTarget: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('PerformancePeriodTargetEditor', () => {
  beforeEach(() => {
    api.getReportingPeriodMasters.mockResolvedValue({ success: true, data: [
      { publicId: 'period-q1', code: 'Q1', name: 'Quarter 1', isActive: true },
      { publicId: 'period-q2', code: 'Q2', name: 'Quarter 2', isActive: true },
    ] });
    api.getPerformancePeriodTargets.mockResolvedValue({ success: true, data: [
      { publicId: 'value-1', reportingPeriodPublicId: 'period-q1', periodCode: 'Q1', unitKind: 2, direction: 1, targetValue: '25', budgetValue: 1000, description: 'Households connected', isActive: true, rowVersion: 'AQ==' },
    ] });
    api.getPerformanceTargetRevisions.mockResolvedValue({ success: true, data: [
      { publicId: 'revision-1', fieldName: 'TargetValue', originalValue: '20', revisedValue: '25', reason: 'Council adjustment', approvalReference: 'COUNCIL-1', effectiveAt: '2026-10-01T00:00:00Z', revisedByUserId: 'user', recordedAt: '2026-10-01T00:00:00Z' },
    ] });
  });

  it('loads canonical values and opens governed revision history', async () => {
    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);

    await waitFor(() => expect(api.getPerformancePeriodTargets).toHaveBeenCalledWith(1, 'target-public-id'));
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByText('Households connected')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Revise/i }));
    await waitFor(() => expect(api.getPerformanceTargetRevisions).toHaveBeenCalledWith('value-1'));
    expect(await screen.findByText('Revise Q1')).toBeInTheDocument();
    expect(await screen.findByLabelText(/Approval reference/)).toBeInTheDocument();
    expect(await screen.findByText(/COUNCIL-1/)).toBeInTheDocument();
  });

  it('offers only reporting periods without an existing canonical value', async () => {
    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);
    await waitFor(() => expect(screen.getByRole('button', { name: /Add period/i })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: /Add period/i }));

    expect(screen.getByRole('option', { name: 'Q2 · Quarter 2' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Q1 · Quarter 1' })).not.toBeInTheDocument();
  });
});
