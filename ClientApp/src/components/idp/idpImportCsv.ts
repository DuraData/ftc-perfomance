import type { IdpKpiImportRowPayload } from '../../types';

const requiredHeaders = [
  'projectCode', 'kpiCode', 'kpiName', 'description', 'formula', 'baseline', 'annualTarget',
  'fiveYearTarget', 'dataSource', 'reportingFrequency', 'indicatorType', 'circular88Linked', 'treasuryTidLinked',
] as const;

const normalizeHeader = (value: string) => value.trim().replace(/[^a-z0-9]/gi, '').toLowerCase();

function parseRecords(csv: string): string[][] {
  const records: string[][] = [];
  let record: string[] = [];
  let value = '';
  let quoted = false;

  for (let index = 0; index < csv.length; index += 1) {
    const character = csv[index];
    if (quoted) {
      if (character === '"' && csv[index + 1] === '"') {
        value += '"';
        index += 1;
      } else if (character === '"') {
        quoted = false;
      } else {
        value += character;
      }
    } else if (character === '"') {
      quoted = true;
    } else if (character === ',') {
      record.push(value);
      value = '';
    } else if (character === '\n') {
      record.push(value.replace(/\r$/, ''));
      if (record.some(item => item.trim())) records.push(record);
      record = [];
      value = '';
    } else {
      value += character;
    }
  }

  if (quoted) throw new Error('CSV contains an unterminated quoted value.');
  record.push(value.replace(/\r$/, ''));
  if (record.some(item => item.trim())) records.push(record);
  return records;
}

function parseNumber(value: string, row: number, field: string): number {
  const parsed = Number(value.trim());
  if (!Number.isFinite(parsed)) throw new Error(`CSV row ${row}: ${field} must be a number.`);
  return parsed;
}

function parseBoolean(value: string, row: number, field: string): boolean {
  const normalized = value.trim().toLowerCase();
  if (['true', 'yes', '1'].includes(normalized)) return true;
  if (['false', 'no', '0', ''].includes(normalized)) return false;
  throw new Error(`CSV row ${row}: ${field} must be true/false, yes/no, or 1/0.`);
}

export function parseIdpKpiCsv(csv: string): IdpKpiImportRowPayload[] {
  const records = parseRecords(csv.replace(/^\uFEFF/, ''));
  if (records.length < 2) throw new Error('CSV must contain a header and at least one data row.');
  const headerMap = new Map(records[0].map((header, index) => [normalizeHeader(header), index]));
  for (const header of requiredHeaders) {
    if (!headerMap.has(normalizeHeader(header))) throw new Error(`CSV is missing required header '${header}'.`);
  }

  const read = (record: string[], header: string) => record[headerMap.get(normalizeHeader(header))!] ?? '';
  return records.slice(1).map((record, index) => {
    const sourceRowNumber = index + 2;
    return {
      sourceRowNumber,
      projectCode: read(record, 'projectCode').trim(),
      kpiCode: read(record, 'kpiCode').trim(),
      kpiName: read(record, 'kpiName').trim(),
      description: read(record, 'description').trim(),
      formula: read(record, 'formula').trim(),
      baseline: parseNumber(read(record, 'baseline'), sourceRowNumber, 'baseline'),
      annualTarget: parseNumber(read(record, 'annualTarget'), sourceRowNumber, 'annualTarget'),
      fiveYearTarget: parseNumber(read(record, 'fiveYearTarget'), sourceRowNumber, 'fiveYearTarget'),
      responsibleDepartmentCode: read(record, 'responsibleDepartmentCode').trim() || null,
      dataSource: read(record, 'dataSource').trim(),
      reportingFrequency: read(record, 'reportingFrequency').trim(),
      indicatorType: read(record, 'indicatorType').trim(),
      circular88Linked: parseBoolean(read(record, 'circular88Linked'), sourceRowNumber, 'circular88Linked'),
      treasuryTidLinked: parseBoolean(read(record, 'treasuryTidLinked'), sourceRowNumber, 'treasuryTidLinked'),
    };
  });
}

export const idpKpiCsvTemplate = [
  'projectCode,kpiCode,kpiName,description,formula,baseline,annualTarget,fiveYearTarget,responsibleDepartmentCode,dataSource,reportingFrequency,indicatorType,circular88Linked,treasuryTidLinked',
  'PROJ-001,KPI-001,Example KPI,Example description,Actual / Target,0,100,500,FIN,System,Quarterly,Output,false,false',
].join('\n');
