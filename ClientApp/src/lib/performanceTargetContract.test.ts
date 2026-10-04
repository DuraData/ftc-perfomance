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
});
