import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PerformancePeriodTargetEditor } from './PerformancePeriodTargetEditor';

const api = vi.hoisted(() => ({
  getReportingPeriodMastersPage: vi.fn(),
  getPerformancePeriodTargets: vi.fn(),
  getPerformanceConfigurationCatalogue: vi.fn(),
  getPerformanceTargetRevisionsPage: vi.fn(),
  createPerformancePeriodTarget: vi.fn(),
  revisePerformancePeriodTarget: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canExecute: () => true }) }));

describe('PerformancePeriodTargetEditor', () => {
  beforeEach(() => {
    api.getPerformanceConfigurationCatalogue.mockResolvedValue({ success: true, data: {
      opmsUnits: [
        { publicId: 'unit-number', code: 'NUMBER', name: 'Number', inputControlType: 'NUMERIC', valueDataType: 'DECIMAL', decimalPlaces: 2, minValue: 0, supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-target-or-higher', requiresComponentUi: false, isQualitative: false, engineUnitKind: 2, isActive: true },
        { publicId: 'unit-date', code: 'DATE', name: 'Date', inputControlType: 'DATE', valueDataType: 'DATE', supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-on-or-before', requiresComponentUi: false, isQualitative: false, engineUnitKind: 10, isActive: true },
      ],
      performanceDirections: [
        { publicId: 'direction-target-or-higher', code: 'TARGET_OR_HIGHER', name: 'Target or higher', description: 'At least target.', engineDirection: 1, isActive: true },
        { publicId: 'direction-on-or-before', code: 'ON_OR_BEFORE_DATE', name: 'On or before date', description: 'On time.', engineDirection: 2, isActive: true },
      ],
    } });
    api.getReportingPeriodMastersPage.mockResolvedValue({ success: true, data: { items: [
      { publicId: 'period-q1', code: 'Q1', name: 'Quarter 1', periodType: 1, isActive: true },
      { publicId: 'period-q2', code: 'Q2', name: 'Quarter 2', periodType: 2, isActive: true },
    ], page: 1, pageSize: 25, totalCount: 2, totalPages: 1 } });
    api.getPerformancePeriodTargets.mockResolvedValue({ success: true, data: [
      { publicId: 'value-1', reportingPeriodPublicId: 'period-q1', periodCode: 'Q1', periodType: 1, unitKind: 2, direction: 1, opmsUnitPublicId: 'unit-number', opmsUnitCode: 'NUMBER', performanceDirectionPublicId: 'direction-target-or-higher', performanceDirectionCode: 'TARGET_OR_HIGHER', targetValue: '25', budgetValue: 1000, originalUnitKind: 2, originalOpmsUnitPublicId: 'unit-number', originalOpmsUnitCode: 'NUMBER', originalTargetValue: '25', originalBudgetValue: 1000, isTargetRevised: false, isBudgetRevised: false, description: 'Households connected', isActive: true, rowVersion: 'AQ==' },
    ] });
    api.getPerformanceTargetRevisionsPage.mockResolvedValue({ success: true, data: { items: [
      { publicId: 'revision-1', fieldName: 'TargetValue', originalValue: '20', revisedValue: '25', reason: 'Council adjustment', approvalReference: 'COUNCIL-1', effectiveAt: '2026-10-01T00:00:00Z', revisedByUserId: 'user', recordedAt: '2026-10-01T00:00:00Z' },
    ], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
  });

  it('loads canonical values and opens governed revision history', async () => {
    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);

    await waitFor(() => expect(api.getPerformancePeriodTargets).toHaveBeenCalledWith(1, 'target-public-id'));
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByText('Households connected')).toBeInTheDocument();
    expect(screen.getByText('Number · Target or higher')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Revise/i }));
    await waitFor(() => expect(api.getPerformanceTargetRevisionsPage).toHaveBeenCalledWith('value-1', expect.objectContaining({ page: 1, pageSize: 10, sortBy: 'recordedAt' })));
    expect(await screen.findByText('Record approved Q1 revision')).toBeInTheDocument();
    expect(await screen.findByLabelText(/approval reference/i)).toBeInTheDocument();
    expect(await screen.findByText(/COUNCIL-1/)).toBeInTheDocument();
  });

  it('offers only reporting periods without an existing canonical value', async () => {
    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);
    await waitFor(() => expect(screen.getByRole('button', { name: /Add period/i })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: /Add period/i }));

    expect(await screen.findByRole('option', { name: 'Q2 · Quarter 2' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Q1 · Quarter 1' })).not.toBeInTheDocument();
  });
});
