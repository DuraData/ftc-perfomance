export type ImportPeriod = { periodType: number; unitKind: number; direction: number; targetValue: string; budgetValue?: number; description?: string };
export type SdbipImportRow = { sourceRowNumber: number; indicatorNumber: string; existingIndicatorNumber?: string; orderNumber: number; targetName: string; kpiDescription: string; departmentCode: string; unitCode?: string; nationalKpa: string; municipalKpa: string; performanceObjective: string; baseline: number; weight: number; kpiType: string; indicatorType: string; periodTargets: ImportPeriod[] };

const periods: Array<[string, number]> = [['Q1', 0], ['Q2', 1], ['MID_TERM', 4], ['Q3', 2], ['Q4', 3], ['ANNUAL', 5]];
const unitKinds: Record<string, number> = { NONE: 0, ABSOLUTE_COUNT: 1, PERCENTAGE_BASED: 2, CUMULATIVE: 3, NON_CUMULATIVE: 4, REVERSE_CUMULATIVE: 5, REVERSE_NON_CUMULATIVE: 6, TIME_BASED: 7, QUALITATIVE_TARGETS: 8, DATE: 9 };
const directions: Record<string, number> = { HIGHER_IS_BETTER: 0, LOWER_IS_BETTER: 1, EXACT_TARGET: 2 };

function csvRows(text: string): string[][] {
  const rows: string[][] = []; let row: string[] = []; let value = ''; let quoted = false;
  for (let index = 0; index < text.length; index += 1) {
    const char = text[index];
    if (char === '"' && quoted && text[index + 1] === '"') { value += '"'; index += 1; }
    else if (char === '"') quoted = !quoted;
    else if (char === ',' && !quoted) { row.push(value.trim()); value = ''; }
    else if ((char === '\n' || char === '\r') && !quoted) { if (char === '\r' && text[index + 1] === '\n') index += 1; row.push(value.trim()); if (row.some(Boolean)) rows.push(row); row = []; value = ''; }
    else value += char;
  }
  row.push(value.trim()); if (row.some(Boolean)) rows.push(row); return rows;
}

export function parseSdbipImportCsv(text: string): SdbipImportRow[] {
  const rows = csvRows(text); if (rows.length < 2) throw new Error('The CSV must contain a header and at least one data row.');
  const headers = rows[0].map(value => value.trim().toUpperCase());
  const at = (row: string[], name: string, required = true) => { const index = headers.indexOf(name); if (index < 0 && required) throw new Error(`Missing required column ${name}.`); return index < 0 ? '' : row[index]?.trim() ?? ''; };
  return rows.slice(1).map((row, index) => {
    const periodTargets = periods.flatMap(([prefix, periodType]) => {
      const targetValue = at(row, `${prefix}_TARGET`, prefix === 'ANNUAL'); if (!targetValue) return [];
      const unitText = at(row, `${prefix}_UNIT`).toUpperCase().replace(/[ -]+/g, '_');
      if (!(unitText in unitKinds)) throw new Error(`Row ${index + 2}, ${prefix}_UNIT: unknown OPMS unit '${unitText}'.`);
      const directionText = at(row, `${prefix}_DIRECTION`, false).toUpperCase().replace(/[ -]+/g, '_') || 'HIGHER_IS_BETTER';
      if (!(directionText in directions)) throw new Error(`Row ${index + 2}, ${prefix}_DIRECTION: unknown direction '${directionText}'.`);
      const budget = at(row, `${prefix}_BUDGET`, false);
      return [{ periodType, unitKind: unitKinds[unitText], direction: directions[directionText], targetValue, budgetValue: budget ? Number(budget) : undefined, description: at(row, `${prefix}_DESCRIPTION`, false) }];
    });
    return { sourceRowNumber: index + 2, indicatorNumber: at(row, 'INDICATOR_NUMBER'), existingIndicatorNumber: at(row, 'EXISTING_INDICATOR_NUMBER', false) || undefined, orderNumber: Number(at(row, 'ORDER_NUMBER')), targetName: at(row, 'TARGET_NAME'), kpiDescription: at(row, 'KPI_DESCRIPTION'), departmentCode: at(row, 'DEPARTMENT_CODE'), unitCode: at(row, 'UNIT_CODE', false) || undefined, nationalKpa: at(row, 'NATIONAL_KPA'), municipalKpa: at(row, 'MUNICIPAL_KPA'), performanceObjective: at(row, 'PERFORMANCE_OBJECTIVE'), baseline: Number(at(row, 'BASELINE')), weight: Number(at(row, 'WEIGHT')), kpiType: at(row, 'KPI_TYPE'), indicatorType: at(row, 'INDICATOR_TYPE'), periodTargets };
  });
}
