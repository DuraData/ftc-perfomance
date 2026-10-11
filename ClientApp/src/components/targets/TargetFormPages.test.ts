import { buildIpmsPayload, buildOpmsPayload, getTargetUnitLabel, resolvePerformanceTemplateHints, toApiUnitType, toXafUnitType, validateRequiredFields } from './TargetFormPages';
import type { PerformanceConfigurationCatalogueDto } from '../../types';

describe('TargetFormPages helpers', () => {
  it('serializes relationship editors as typed arrays rather than CSV fields', () => {
    const payload = buildOpmsPayload({ municipalityFinancialYearPublicId: 'year-public-id', sourceTemplateId: 'template-public-id', departmentId: 'department-public-id', unitId: 'unit-public-id', wardIds: 'ward-1, ward-2,ward-2', additionalAssigneeIds: 'user-a,user-b,user-a', voteNumberIds: 'vote-10,vote-11', nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1', budgetTypePublicId: 'type-1', budgetSources: [{ budgetSourcePublicId: 'source-1', amount: '125.50' }, { budgetSourcePublicId: 'source-2', amount: '' }], kpiUnitOfMeasurePublicId: 'unit-master-1' } as never);
    expect(payload).not.toHaveProperty('departmentId');
    expect(payload).not.toHaveProperty('unitId');
    expect(payload).not.toHaveProperty('periodId');
    expect(payload).not.toHaveProperty('sourceTemplateId');
    expect(payload.municipalityFinancialYearPublicId).toBe('year-public-id');
    expect(payload.sourceTemplatePublicId).toBe('template-public-id');
    expect(payload.departmentPublicId).toBe('department-public-id');
    expect(payload.unitPublicId).toBe('unit-public-id');
    expect(payload.wardPublicIds).toEqual(['ward-1', 'ward-2']);
    expect(payload.additionalAssigneePublicIds).toEqual(['user-a', 'user-b']);
    expect(payload.voteNumberPublicIds).toEqual(['vote-10', 'vote-11']);
    expect(payload.budgetTypePublicId).toBe('type-1');
    expect(payload.budgetSources).toEqual([{ budgetSourcePublicId: 'source-1', amount: 125.5 }, { budgetSourcePublicId: 'source-2', amount: null }]);
    expect(payload.kpiUnitOfMeasurePublicId).toBe('unit-master-1');
    expect(payload).toMatchObject({ nationalKpaPublicId: 'national-1', municipalKpaPublicId: 'municipal-1', backToBasicsPillarPublicId: 'pillar-1', strategicGoalPublicId: 'goal-1', strategicInterventionPublicId: 'intervention-1', strategicObjectivePublicId: 'objective-1', performanceObjectivePublicId: 'performance-1' });
  });

  it('serializes IPMS linkage and ownership using public identifiers only', () => {
    const payload = buildIpmsPayload({ municipalityFinancialYearPublicId: 'year-public-id', sourceTemplateId: 'template-public-id', relatedOPMSTargetId: 'opms-public-id', departmentId: 'department-public-id', unitId: 'unit-public-id', wardIds: '', additionalAssigneeIds: '', voteNumberIds: '', budgetSources: [] } as never);

    expect(payload).toMatchObject({
      municipalityFinancialYearPublicId: 'year-public-id',
      sourceTemplatePublicId: 'template-public-id',
      relatedOpmsTargetPublicId: 'opms-public-id',
      departmentPublicId: 'department-public-id',
      unitPublicId: 'unit-public-id',
    });
    for (const retired of ['sourceTemplateId', 'relatedOpmsTargetId', 'periodId', 'departmentId', 'unitId', 'strategicGoalId', 'strategicObjectiveId'])
      expect(payload).not.toHaveProperty(retired);
  });

  it('serializes target periods with canonical OPMS unit and direction ids', () => {
    const configuration: PerformanceConfigurationCatalogueDto = {
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
