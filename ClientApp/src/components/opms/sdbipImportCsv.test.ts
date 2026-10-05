import { describe, expect, it } from 'vitest';
import { parseSdbipImportCsv } from './sdbipImportCsv';

const header = 'INDICATOR_NUMBER,ORDER_NUMBER,TARGET_NAME,KPI_DESCRIPTION,DEPARTMENT_CODE,NATIONAL_KPA,MUNICIPAL_KPA,BACK_TO_BASICS_PILLAR,STRATEGIC_GOAL,STRATEGIC_INTERVENTION,STRATEGIC_OBJECTIVE,PERFORMANCE_OBJECTIVE,BASELINE,WEIGHT,KPI_TYPE,INDICATOR_TYPE,ANNUAL_TARGET,ANNUAL_UNIT';
describe('parseSdbipImportCsv', () => {
  it('maps the wide annual columns to a normalized period target', () => {
    const [row] = parseSdbipImportCsv(`${header}\nKPI-1,1,Target,Description,FIN,KPA,MKPA,B2B,Goal,Intervention,Objective,Performance Objective,0,10,Output,Quantitative,100,absolute count`);
    expect(row.sourceRowNumber).toBe(2); expect(row.periodTargets[0]).toMatchObject({ periodType: 6, unitKind: 2, direction: 1, targetValue: '100' });
    expect(row).toMatchObject({ nationalKpa: 'KPA', municipalKpa: 'MKPA', backToBasicsPillar: 'B2B', strategicGoal: 'Goal', strategicIntervention: 'Intervention', strategicObjective: 'Objective', performanceObjective: 'Performance Objective' });
  });
  it('rejects unknown units without defaulting', () => expect(() => parseSdbipImportCsv(`${header}\nKPI-1,1,T,D,FIN,K,M,B,G,I,O,P,0,10,X,Y,1,mystery`)).toThrow(/unknown OPMS unit/));
  it('uses the API enum values for every governed period', () => {
    const wideHeader = `${header},Q1_TARGET,Q1_UNIT,Q2_TARGET,Q2_UNIT,MID_TERM_TARGET,MID_TERM_UNIT,Q3_TARGET,Q3_UNIT,Q4_TARGET,Q4_UNIT`;
    const [row] = parseSdbipImportCsv(`${wideHeader}\nKPI-1,1,T,D,FIN,K,M,B,G,I,O,P,0,10,X,Y,6,absolute_count,1,percentage_based,2,financial,3,time_based,4,date,5,qualitative_targets`);
    expect(row.periodTargets.map(period => period.periodType).sort()).toEqual([1, 2, 3, 4, 5, 6]);
    expect(row.periodTargets.map(period => period.unitKind)).toEqual([1, 3, 4, 10, 13, 2]);
  });
});
