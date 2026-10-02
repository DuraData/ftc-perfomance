import type { IdpHierarchyImportRowPayload, IdpKpiImportRowPayload } from '../../types';

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

function parseDate(value: string, row: number, field: string): string {
  const normalized = value.trim();
  if (!/^\d{4}-\d{2}-\d{2}$/.test(normalized) || Number.isNaN(Date.parse(`${normalized}T00:00:00.000Z`))) {
    throw new Error(`CSV row ${row}: ${field} must use YYYY-MM-DD.`);
  }
  return `${normalized}T00:00:00.000Z`;
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

const hierarchyHeaders = [
  'outcomeCode', 'outcomeName', 'outcomeDescription', 'outcomeSortOrder',
  'objectiveCode', 'objectiveName', 'objectiveDescription', 'objectiveBaseline', 'objectiveTarget',
  'objectiveStartDate', 'objectiveEndDate', 'objectiveBudget', 'objectiveSortOrder',
  'priorityCode', 'priorityName', 'priorityDescription', 'prioritySortOrder',
  'programmeCode', 'programmeName', 'programmeDescription', 'programmePlannedBudget', 'programmeApprovedBudget', 'programmeActualExpenditure',
  'projectCode', 'projectName', 'projectDescription', 'projectCategory', 'projectBudget', 'projectFundingSource',
  'projectStartDate', 'projectEndDate', 'projectStatus',
] as const;

export function parseIdpHierarchyCsv(csv: string): IdpHierarchyImportRowPayload[] {
  const records = parseRecords(csv.replace(/^\uFEFF/, ''));
  if (records.length < 2) throw new Error('CSV must contain a header and at least one data row.');
  const headerMap = new Map(records[0].map((header, index) => [normalizeHeader(header), index]));
  for (const header of hierarchyHeaders) {
    if (!headerMap.has(normalizeHeader(header))) throw new Error(`CSV is missing required header '${header}'.`);
  }
  const read = (record: string[], header: string) => record[headerMap.get(normalizeHeader(header))!] ?? '';
  return records.slice(1).map((record, index) => {
    const sourceRowNumber = index + 2;
    return {
      sourceRowNumber,
      outcomeCode: read(record, 'outcomeCode').trim(), outcomeName: read(record, 'outcomeName').trim(), outcomeDescription: read(record, 'outcomeDescription').trim(), outcomeSortOrder: parseNumber(read(record, 'outcomeSortOrder'), sourceRowNumber, 'outcomeSortOrder'),
      objectiveCode: read(record, 'objectiveCode').trim(), objectiveName: read(record, 'objectiveName').trim(), objectiveDescription: read(record, 'objectiveDescription').trim(), objectiveBaseline: parseNumber(read(record, 'objectiveBaseline'), sourceRowNumber, 'objectiveBaseline'), objectiveTarget: parseNumber(read(record, 'objectiveTarget'), sourceRowNumber, 'objectiveTarget'), objectiveDepartmentCode: read(record, 'objectiveDepartmentCode').trim() || null, objectiveStartDate: parseDate(read(record, 'objectiveStartDate'), sourceRowNumber, 'objectiveStartDate'), objectiveEndDate: parseDate(read(record, 'objectiveEndDate'), sourceRowNumber, 'objectiveEndDate'), objectiveBudget: parseNumber(read(record, 'objectiveBudget'), sourceRowNumber, 'objectiveBudget'), objectiveSortOrder: parseNumber(read(record, 'objectiveSortOrder'), sourceRowNumber, 'objectiveSortOrder'),
      priorityCode: read(record, 'priorityCode').trim(), priorityName: read(record, 'priorityName').trim(), priorityDescription: read(record, 'priorityDescription').trim(), prioritySortOrder: parseNumber(read(record, 'prioritySortOrder'), sourceRowNumber, 'prioritySortOrder'),
      programmeCode: read(record, 'programmeCode').trim(), programmeName: read(record, 'programmeName').trim(), programmeDescription: read(record, 'programmeDescription').trim(), programmeDepartmentCode: read(record, 'programmeDepartmentCode').trim() || null, programmePlannedBudget: parseNumber(read(record, 'programmePlannedBudget'), sourceRowNumber, 'programmePlannedBudget'), programmeApprovedBudget: parseNumber(read(record, 'programmeApprovedBudget'), sourceRowNumber, 'programmeApprovedBudget'), programmeActualExpenditure: parseNumber(read(record, 'programmeActualExpenditure'), sourceRowNumber, 'programmeActualExpenditure'),
      projectCode: read(record, 'projectCode').trim(), projectName: read(record, 'projectName').trim(), projectDescription: read(record, 'projectDescription').trim(), projectCategory: read(record, 'projectCategory').trim(), projectDepartmentCode: read(record, 'projectDepartmentCode').trim() || null, projectBudget: parseNumber(read(record, 'projectBudget'), sourceRowNumber, 'projectBudget'), projectFundingSource: read(record, 'projectFundingSource').trim(), projectStartDate: parseDate(read(record, 'projectStartDate'), sourceRowNumber, 'projectStartDate'), projectEndDate: parseDate(read(record, 'projectEndDate'), sourceRowNumber, 'projectEndDate'), projectStatus: read(record, 'projectStatus').trim(), communityNeedReference: read(record, 'communityNeedReference').trim() || null,
    };
  });
}

export const idpHierarchyCsvTemplate = [
  'outcomeCode,outcomeName,outcomeDescription,outcomeSortOrder,objectiveCode,objectiveName,objectiveDescription,objectiveBaseline,objectiveTarget,objectiveDepartmentCode,objectiveStartDate,objectiveEndDate,objectiveBudget,objectiveSortOrder,priorityCode,priorityName,priorityDescription,prioritySortOrder,programmeCode,programmeName,programmeDescription,programmeDepartmentCode,programmePlannedBudget,programmeApprovedBudget,programmeActualExpenditure,projectCode,projectName,projectDescription,projectCategory,projectDepartmentCode,projectBudget,projectFundingSource,projectStartDate,projectEndDate,projectStatus,communityNeedReference',
  'OUT-1,Inclusive growth,Inclusive local economy,1,OBJ-1,Increase employment,Create sustainable jobs,25,35,LED,2026-07-01,2031-06-30,5000000,1,PRI-1,Economic development,Priority investment area,1,PROG-1,Enterprise support,Support local enterprises,LED,3000000,2500000,0,PROJ-1,Enterprise hub,Establish a municipal enterprise hub,Capital,LED,2500000,Municipal grant,2026-07-01,2028-06-30,Planned,WARD-NEED-1',
].join('\n');
