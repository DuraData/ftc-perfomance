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
const security = vi.hoisted(() => ({
  canUpdate: vi.fn(() => true),
  canExecute: vi.fn(() => true),
  canReadField: vi.fn(() => true),
  canEditField: vi.fn(() => true),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('PerformancePeriodTargetEditor', () => {
  beforeEach(() => {
    security.canUpdate.mockReturnValue(true);
    security.canExecute.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    api.getPerformanceConfigurationCatalogue.mockResolvedValue({ success: true, data: {
      opmsUnits: [
        { publicId: 'unit-number', code: 'NUMBER', name: 'Number', inputControlType: 'NUMERIC', valueDataType: 'DECIMAL', decimalPlaces: 2, minValue: 0, supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-target-or-higher', requiresComponentUi: false, isQualitative: false, engineUnitKind: 2, isActive: true },
        { publicId: 'unit-date', code: 'DATE', name: 'Date', inputControlType: 'DATE', valueDataType: 'DATE', supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-on-or-before', requiresComponentUi: false, isQualitative: false, engineUnitKind: 10, isActive: true },
        { publicId: 'unit-ratio', code: 'RATIO', name: 'Ratio', inputControlType: 'RATIO', valueDataType: 'TEXT', decimalPlaces: 6, minValue: 0, supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-target-or-higher', requiresComponentUi: true, isQualitative: false, engineUnitKind: 8, isActive: true },
      ],
      ratioComparisonMethods: [
        { publicId: 'ratio-normalized', code: 'NORMALIZED_RATIO', name: 'Normalized ratio', description: 'Normalize two segments.', supportsAutoCalculation: true, isActive: true },
        { publicId: 'ratio-manual', code: 'MANUAL', name: 'Manual', description: 'Manual assessment.', supportsAutoCalculation: false, isActive: true },
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
      { publicId: 'revision-1', fieldName: 'TargetValue', originalValue: '20', revisedValue: '25', reason: 'Council adjustment', approvalReference: 'COUNCIL-1', effectiveAt: '2026-10-01T00:00:00Z', revisedByUserPublicId: 'user-public-id', revisedByName: 'Revision User', recordedAt: '2026-10-01T00:00:00Z' },
    ], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.revisePerformancePeriodTarget.mockResolvedValue({ success: true, data: {} });
  });

  it('loads canonical values and opens governed revision history', async () => {
    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);

    await waitFor(() => expect(api.getPerformancePeriodTargets).toHaveBeenCalledWith(1, 'target-public-id'));
    expect(await screen.findByText('25')).toBeInTheDocument();
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

  it('shows the governed required-POE configuration on the reporting-period target', async () => {
    api.getPerformancePeriodTargets.mockResolvedValue({ success: true, data: [{
      publicId: 'value-1', reportingPeriodPublicId: 'period-q1', periodCode: 'Q1', periodType: 1,
      unitKind: 2, direction: 1, opmsUnitPublicId: 'unit-number', performanceDirectionPublicId: 'direction-target-or-higher',
      targetValue: '25', originalUnitKind: 2, originalTargetValue: '25', isTargetRevised: false, isBudgetRevised: false,
      requiredPoeDescription: 'Signed completion certificate.', requiresPoe: true, isActive: true, rowVersion: 'AQ==',
    }] });

    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);

    expect(await screen.findByText('POE required')).toBeInTheDocument();
    expect(screen.getByText(/Signed completion certificate/)).toBeInTheDocument();
  });

  it('masks protected period values and hides mutation actions without member grants', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);

    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);

    expect(await screen.findByText('Restricted')).toBeInTheDocument();
    expect(screen.queryByText('25')).not.toBeInTheDocument();
    expect(screen.queryByText('1000')).not.toBeInTheDocument();
    expect(screen.queryByText('Households connected')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Add period/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Revise/i })).not.toBeInTheDocument();
  });

  it('edits governed ratio labels and sends the definition row version', async () => {
    api.getPerformancePeriodTargets.mockResolvedValue({ success: true, data: [{
      publicId: 'ratio-value', reportingPeriodPublicId: 'period-q1', periodCode: 'Q1', periodType: 1,
      unitKind: 8, direction: 1, opmsUnitPublicId: 'unit-ratio', opmsUnitCode: 'RATIO',
      performanceDirectionPublicId: 'direction-target-or-higher', performanceDirectionCode: 'TARGET_OR_HIGHER',
      targetValue: '4:3000', originalUnitKind: 8, originalOpmsUnitPublicId: 'unit-ratio', originalOpmsUnitCode: 'RATIO',
      originalTargetValue: '4:3000', isTargetRevised: false, isBudgetRevised: false, isActive: true, rowVersion: 'AQ==',
      ratioDefinition: {
        publicId: 'ratio-definition', ratioSegmentCount: 2, ratioComparisonMethodPublicId: 'ratio-normalized',
        ratioComparisonMethodCode: 'NORMALIZED_RATIO', ratioComparisonMethodName: 'Normalized ratio', isActive: true, rowVersion: 'Ag==',
        segments: [
          { publicId: 'segment-1', segmentPosition: 1, segmentLabel: 'Officers', decimalPlaces: 0, minValue: 0, isActive: true, rowVersion: 'Aw==' },
          { publicId: 'segment-2', segmentPosition: 2, segmentLabel: 'Population', decimalPlaces: 0, minValue: 1, isActive: true, rowVersion: 'BA==' },
        ],
      },
    }] });

    render(<PerformancePeriodTargetEditor kind={1} targetPublicId="target-public-id" />);
    fireEvent.click(await screen.findByRole('button', { name: /Revise/i }));
    expect(await screen.findByRole('spinbutton', { name: 'Revised target value Officers' })).toHaveValue(4);
    expect(screen.getByRole('spinbutton', { name: 'Revised target value Population' })).toHaveValue(3000);
    fireEvent.change(screen.getByLabelText(/Segment 1 label/i), { target: { value: 'Municipal officers' } });
    fireEvent.change(screen.getByLabelText(/approval reference/i), { target: { value: 'COUNCIL-RATIO-1' } });
    fireEvent.change(screen.getByLabelText(/revision reason/i), { target: { value: 'Clarify ratio labels' } });
    fireEvent.click(screen.getByRole('button', { name: /Record revision/i }));

    await waitFor(() => expect(api.revisePerformancePeriodTarget).toHaveBeenCalledWith('ratio-value', expect.objectContaining({
      ratioDefinition: expect.objectContaining({
        ratioSegmentCount: 2,
        ratioComparisonMethodPublicId: 'ratio-normalized',
        rowVersion: 'Ag==',
        segments: expect.arrayContaining([expect.objectContaining({ segmentPosition: 1, segmentLabel: 'Municipal officers' })]),
      }),
    })));
  });
});
