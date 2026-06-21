import { getTargetUnitLabel, toApiUnitType, toXafUnitType, validateRequiredFields } from './TargetFormPages';

describe('TargetFormPages helpers', () => {
  it('maps legacy unit type to XAF unit type', () => {
    expect(toXafUnitType('percentage')).toBe('PercentageBased');
    expect(toXafUnitType('absolute_count')).toBe('AbsoluteCount');
    expect(toXafUnitType('unknown')).toBe('unknown');
  });

  it('maps XAF unit type to API unit type', () => {
    expect(toApiUnitType('PercentageBased')).toBe('percentage');
    expect(toApiUnitType('AbsoluteCount')).toBe('absolute_count');
    expect(toApiUnitType('UnknownValue' as any)).toBe('UnknownValue');
  });

  it('returns label for a known target unit type', () => {
    expect(getTargetUnitLabel('Financial')).toBe('Financial');
    expect(getTargetUnitLabel('Date')).toBe('Date');
  });

  it('returns default label when unit type is unknown', () => {
    expect(getTargetUnitLabel('Unknown' as any)).toBe('Target Value');
  });

  it('validates required fields and returns missing field messages', () => {
    const result = validateRequiredFields([
      { label: 'Target Name', value: '' },
      { label: 'Department', value: 'Dept 1' },
      { label: 'Baseline', value: '  ' },
    ]);

    expect(result).toEqual(['Target Name is required.', 'Baseline is required.']);
  });
});
