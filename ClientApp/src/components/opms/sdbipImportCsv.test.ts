import { describe, expect, it } from 'vitest';
import { parseSdbipImportCsv } from './sdbipImportCsv';

const header = 'INDICATOR_NUMBER,ORDER_NUMBER,TARGET_NAME,KPI_DESCRIPTION,DEPARTMENT_CODE,NATIONAL_KPA,MUNICIPAL_KPA,PERFORMANCE_OBJECTIVE,BASELINE,WEIGHT,KPI_TYPE,INDICATOR_TYPE,ANNUAL_TARGET,ANNUAL_UNIT';
describe('parseSdbipImportCsv', () => {
  it('maps the wide annual columns to a normalized period target', () => {
    const [row] = parseSdbipImportCsv(`${header}\nKPI-1,1,Target,Description,FIN,KPA,MKPA,Objective,0,10,Output,Quantitative,100,absolute count`);
    expect(row.sourceRowNumber).toBe(2); expect(row.periodTargets[0]).toMatchObject({ periodType: 5, unitKind: 1, targetValue: '100' });
  });
  it('rejects unknown units without defaulting', () => expect(() => parseSdbipImportCsv(`${header}\nKPI-1,1,T,D,FIN,K,M,O,0,10,X,Y,1,mystery`)).toThrow(/unknown OPMS unit/));
});
