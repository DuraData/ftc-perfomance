import { buildOpmsPayload, childrenFor, getTargetUnitLabel, resolvePerformanceTemplateHints, toApiUnitType, toXafUnitType, validateRequiredFields } from './TargetFormPages';

describe('TargetFormPages helpers', () => {
  it('serializes relationship editors as typed arrays rather than CSV fields', () => {
    const payload = buildOpmsPayload({ departmentId: 'department-public-id', unitId: 'unit-public-id', wardIds: '1, 2,2', additionalAssigneeIds: 'user-a,user-b,user-a', voteNumberIds: '10,11', nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1', budgetTypePublicId: 'type-1', budgetSources: [{ budgetSourcePublicId: 'source-1', amount: '125.50' }, { budgetSourcePublicId: 'source-2', amount: '' }], kpiUnitOfMeasurePublicId: 'unit-master-1' } as never);
    expect(payload.departmentId).toBeNull();
    expect(payload.departmentPublicId).toBe('department-public-id');
    expect(payload.unitId).toBeNull();
    expect(payload.unitPublicId).toBe('unit-public-id');
    expect(payload.wardIds).toEqual([1, 2]);
    expect(payload.additionalAssigneeIds).toEqual(['user-a', 'user-b']);
    expect(payload.voteNumberIds).toEqual([10, 11]);
    expect(payload.budgetTypePublicId).toBe('type-1');
    expect(payload.budgetSources).toEqual([{ budgetSourcePublicId: 'source-1', amount: 125.5 }, { budgetSourcePublicId: 'source-2', amount: null }]);
    expect(payload.kpiUnitOfMeasurePublicId).toBe('unit-master-1');
    expect(payload).toMatchObject({ nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1' });
  });

  it('serializes target periods with canonical OPMS unit and direction ids', () => {
    const configuration = {
      opmsUnits: [{ publicId: 'unit-percent', code: 'PERCENT', name: 'Percentage', inputControlType: 'NUMERIC', valueDataType: 'DECIMAL', supportsAutoVariance: true, defaultPerformanceDirectionPublicId: 'direction-higher', requiresComponentUi: false, isQualitative: false, engineUnitKind: 1, isActive: true }],
      performanceDirections: [{ publicId: 'direction-higher', code: 'TARGET_OR_HIGHER', name: 'Target or higher', description: 'At least target.', engineDirection: 1, isActive: true }],
    };
    const payload = buildOpmsPayload({ wardIds: '', additionalAssigneeIds: '', voteNumberIds: '', budgetSources: [], q1Target: '25', q1UnitType: 'PercentageBased', q1Budget: '', q1Description: 'Quarter target' } as never, configuration);

    expect(payload.periodTargets).toEqual([expect.objectContaining({
      periodType: 1,
      unitKind: 1,
      opmsUnitPublicId: 'unit-percent',
      performanceDirectionPublicId: 'direction-higher',
    })]);
  });

  it('filters children only when the selected parent has configured mappings', () => {
    const items = [{ publicId: 'goal-1', name: 'Goal 1', displayOrder: 1 }, { publicId: 'goal-2', name: 'Goal 2', displayOrder: 2 }];
    const catalogue = { nationalKpas: [], municipalKpas: [], backToBasicsPillars: [], strategicGoals: items, strategicInterventions: [], strategicObjectives: [], performanceObjectives: [], relationships: [{ relationshipType: 'municipal-kpa-strategic-goal', parentPublicId: 'kpa-1', childPublicId: 'goal-2' }] };
    expect(childrenFor(catalogue, 'municipal-kpa-strategic-goal', 'kpa-1', items)).toEqual([items[1]]);
    expect(childrenFor(catalogue, 'municipal-kpa-strategic-goal', 'kpa-without-mappings', items)).toEqual(items);
  });

  it('resolves reusable template classification hints to governed catalogue ids', () => {
    const form = { kpiType: '', kpiTypePublicId: '', kpiTypeHint: 'output', indicatorType: '', indicatorTypePublicId: '', indicatorTypeHint: 'QUANT', functionalArea: '', functionalAreaPublicId: '', functionalAreaHint: 'Technical Services', standardClassification: '', standardClassificationPublicId: '', standardClassificationHint: 'SERVICE', kpiUnitOfMeasurePublicId: '', unitOfMeasureHint: 'Count' };
    const item = (publicId: string, code: string, name: string) => ({ publicId, code, name, displayOrder: 1 });
    const catalogue = { kpiTypes: [item('kpi-id', 'OUTPUT', 'Output')], indicatorTypes: [item('indicator-id', 'QUANT', 'Quantitative')], functionalAreas: [item('area-id', 'TECH', 'Technical Services')], standardClassifications: [item('class-id', 'SERVICE', 'Service Delivery')], kpiUnitsOfMeasure: [item('unit-id', 'COUNT', 'Count')] };
    expect(resolvePerformanceTemplateHints(form, catalogue as never)).toMatchObject({
      kpiTypePublicId: 'kpi-id', kpiType: 'Output', indicatorTypePublicId: 'indicator-id', indicatorType: 'Quantitative',
      functionalAreaPublicId: 'area-id', functionalArea: 'Technical Services', standardClassificationPublicId: 'class-id', standardClassification: 'Service Delivery',
      kpiUnitOfMeasurePublicId: 'unit-id', kpiTypeHint: '', indicatorTypeHint: '', functionalAreaHint: '', standardClassificationHint: '', unitOfMeasureHint: '',
    });
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
