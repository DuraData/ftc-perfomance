import { buildOpmsPayload, getTargetUnitLabel, toApiUnitType, toXafUnitType, validateRequiredFields } from './TargetFormPages';

describe('TargetFormPages helpers', () => {
  it('serializes relationship editors as typed arrays rather than CSV fields', () => {
    const payload = buildOpmsPayload({ wardIds: '1, 2,2', additionalAssigneeIds: 'user-a,user-b,user-a', voteNumberIds: '10,11' } as never);
    expect(payload.wardIds).toEqual([1, 2]);
    expect(payload.additionalAssigneeIds).toEqual(['user-a', 'user-b']);
    expect(payload.voteNumberIds).toEqual([10, 11]);
  });
  it('maps legacy unit type to XAF unit type', () => {
    expect(toXafUnitType('percentage')).toBe('PercentageBased');
    expect(toXafUnitType('absolute_count')).toBe('AbsoluteCount');
    expect(toXafUnitType('unknown')).toBe('unknown');
  });

  it('maps XAF unit type to API unit type', () => {
    expect(toApiUnitType('PercentageBased')).toBe('percentage');
    expect(toApiUnitType('AbsoluteCount')).toBe('absolute_count');
    expect(toApiUnitType('UnknownValue')).toBe('UnknownValue');
  });

  it('returns label for a known target unit type', () => {
    expect(getTargetUnitLabel('Financial')).toBe('Financial');
    expect(getTargetUnitLabel('Date')).toBe('Date');
  });

  it('returns default label when unit type is unknown', () => {
    expect(getTargetUnitLabel('Unknown')).toBe('Target Value');
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
