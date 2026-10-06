import { canonicalPeriodTarget, canonicalSaveRows, performanceUnitValue } from './performanceTargetContract';

describe('canonical performance target contract', () => {
  it('preserves typed non-numeric values and derives direction from the period unit', () => {
    expect(canonicalPeriodTarget(1, 'Council approved', 'QualitativeTargets')).toEqual({
      periodType: 1,
      unitKind: 13,
      direction: 3,
      targetValue: 'Council approved',
      budgetValue: null,
      description: null,
    });
    expect(canonicalPeriodTarget(6, '2027-06-30', 'Date').direction).toBe(2);
  });

  it('round-trips canonical API rows without wide annual or quarter projections', () => {
    const rows = canonicalSaveRows([{
      publicId: 'target-value', reportingPeriodPublicId: 'period', periodCode: 'Q1', periodType: 1,
      unitKind: 8, direction: 1, targetValue: '2:1', budgetValue: 10, description: 'Ratio', isActive: true, rowVersion: 'AQ==',
      originalUnitKind: 8, originalTargetValue: '2:1', originalBudgetValue: 10, isTargetRevised: false, isBudgetRevised: false,
    }]);
    expect(rows).toEqual([{ periodType: 1, unitKind: 8, direction: 1, targetValue: '2:1', budgetValue: 10, description: 'Ratio' }]);
    expect(performanceUnitValue(8)).toBe('Ratios');
  });

  it('uses persisted OPMS unit and performance-direction identifiers when a catalogue is supplied', () => {
    const result = canonicalPeriodTarget(6, '2027-06-30', 'Date', null, null, {
      opmsUnits: [{ publicId: 'unit-date', code: 'DATE', name: 'Date', inputControlType: 'DATE', valueDataType: 'DATE', supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-before', requiresComponentUi: false, isQualitative: false, engineUnitKind: 10, isActive: true }],
      performanceDirections: [{ publicId: 'direction-before', code: 'ON_OR_BEFORE_DATE', name: 'On or before date', description: 'On time.', engineDirection: 2, isActive: true }],
    });

    expect(result).toMatchObject({
      unitKind: 10,
      direction: 2,
      opmsUnitPublicId: 'unit-date',
      performanceDirectionPublicId: 'direction-before',
    });
  });
});
