import { idpKpiCsvTemplate, parseIdpKpiCsv } from './idpImportCsv';

describe('IDP KPI CSV parser', () => {
  it('parses the governed template and preserves source rows', () => {
    const rows = parseIdpKpiCsv(idpKpiCsvTemplate);
    expect(rows).toHaveLength(1);
    expect(rows[0]).toMatchObject({
      sourceRowNumber: 2,
      projectCode: 'PROJ-001',
      kpiCode: 'KPI-001',
      annualTarget: 100,
      circular88Linked: false,
    });
  });

  it('supports quoted commas and escaped quotes', () => {
    const csv = `${idpKpiCsvTemplate.split('\n')[0]}\nPROJ,KPI,"Water, access","A ""quoted"" description",x,1,2,3,,System,Annual,Outcome,yes,0`;
    const [row] = parseIdpKpiCsv(csv);
    expect(row.kpiName).toBe('Water, access');
    expect(row.description).toBe('A "quoted" description');
    expect(row.circular88Linked).toBe(true);
  });

  it('rejects missing headers and invalid typed values before transport', () => {
    expect(() => parseIdpKpiCsv('projectCode\nP')).toThrow(/missing required header|header and at least/i);
    expect(() => parseIdpKpiCsv(idpKpiCsvTemplate.replace(',0,100,', ',invalid,100,'))).toThrow(/baseline must be a number/i);
  });
});
