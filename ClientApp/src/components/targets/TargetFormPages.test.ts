import { buildOpmsPayload, childrenFor, getTargetUnitLabel, toApiUnitType, toXafUnitType, validateRequiredFields } from './TargetFormPages';

describe('TargetFormPages helpers', () => {
  it('serializes relationship editors as typed arrays rather than CSV fields', () => {
    const payload = buildOpmsPayload({ departmentId: 'department-public-id', unitId: 'unit-public-id', wardIds: '1, 2,2', additionalAssigneeIds: 'user-a,user-b,user-a', voteNumberIds: '10,11', nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1' } as never);
    expect(payload.departmentId).toBeNull();
    expect(payload.departmentPublicId).toBe('department-public-id');
    expect(payload.unitId).toBeNull();
    expect(payload.unitPublicId).toBe('unit-public-id');
    expect(payload.wardIds).toEqual([1, 2]);
    expect(payload.additionalAssigneeIds).toEqual(['user-a', 'user-b']);
    expect(payload.voteNumberIds).toEqual([10, 11]);
    expect(payload).toMatchObject({ nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1' });
  });

  it('filters children only when the selected parent has configured mappings', () => {
    const items = [{ publicId: 'goal-1', name: 'Goal 1', displayOrder: 1 }, { publicId: 'goal-2', name: 'Goal 2', displayOrder: 2 }];
    const catalogue = { nationalKpas: [], municipalKpas: [], backToBasicsPillars: [], strategicGoals: items, strategicInterventions: [], strategicObjectives: [], performanceObjectives: [], relationships: [{ relationshipType: 'municipal-kpa-strategic-goal', parentPublicId: 'kpa-1', childPublicId: 'goal-2' }] };
    expect(childrenFor(catalogue, 'municipal-kpa-strategic-goal', 'kpa-1', items)).toEqual([items[1]]);
    expect(childrenFor(catalogue, 'municipal-kpa-strategic-goal', 'kpa-without-mappings', items)).toEqual(items);
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
