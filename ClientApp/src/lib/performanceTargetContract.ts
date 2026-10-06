import type { PerformanceConfigurationCatalogueDto, PerformancePeriodTargetDto, SaveTargetPeriodValuePayload, XafUnitValue } from '../types';

const unitKindByValue: Record<string, number> = {
  none: 0,
  percentage: 1,
  percentagebased: 1,
  absolute_count: 2,
  absolutecount: 2,
  financial: 3,
  time_based: 4,
  timebased: 4,
  area_based: 5,
  areabased: 5,
  volume_based: 6,
  volumebased: 6,
  index_scores: 7,
  indexscores: 7,
  ratios: 8,
  binary: 9,
  date: 10,
  readiness_scale: 11,
  readinessscale: 11,
  binary_determination: 12,
  binarydetermination: 12,
  qualitative: 13,
  qualitativetargets: 13,
  zero_based: 14,
  zerobased: 14,
  reverse_cumulative: 15,
  reversecumulative: 15,
  reverse_non_cumulative: 16,
  reversenoncumulative: 16,
};

const unitValueByKind: Record<number, XafUnitValue> = {
  0: 'None', 1: 'PercentageBased', 2: 'AbsoluteCount', 3: 'Financial', 4: 'TimeBased', 5: 'AreaBased',
  6: 'VolumeBased', 7: 'IndexScores', 8: 'Ratios', 9: 'Binary', 10: 'Date', 11: 'ReadinessScale',
  12: 'BinaryDetermination', 13: 'QualitativeTargets', 14: 'ZeroBased', 15: 'ReverseCumulative', 16: 'ReverseNonCumulative',
};

export function performanceUnitKind(value: string): number {
  return unitKindByValue[value.trim().toLowerCase()] ?? 2;
}

export function performanceUnitValue(unitKind?: number): XafUnitValue {
  return unitValueByKind[unitKind ?? 2] ?? 'AbsoluteCount';
}

export function performanceDirection(unitKind: number): 1 | 2 | 3 {
  if ([4, 10, 15, 16].includes(unitKind)) return 2;
  if ([0, 9, 12, 13, 14].includes(unitKind)) return 3;
  return 1;
}

export function canonicalPeriodTarget(
  periodType: 1 | 2 | 3 | 4 | 5 | 6,
  targetValue: string | number,
  unit: string,
  budgetValue?: number | null,
  description?: string | null,
  configuration?: PerformanceConfigurationCatalogueDto,
): SaveTargetPeriodValuePayload {
  const unitKind = performanceUnitKind(unit);
  const opmsUnit = configuration?.opmsUnits.find(item => item.engineUnitKind === unitKind);
  const directionDefinition = configuration?.performanceDirections.find(item => item.publicId === opmsUnit?.defaultPerformanceDirectionPublicId);
  const result: SaveTargetPeriodValuePayload = {
    periodType,
    unitKind,
    direction: directionDefinition?.engineDirection ?? performanceDirection(unitKind),
    targetValue: String(targetValue).trim(),
    budgetValue: budgetValue ?? null,
    description: description?.trim() || null,
  };
  if (opmsUnit) result.opmsUnitPublicId = opmsUnit.publicId;
  if (directionDefinition) result.performanceDirectionPublicId = directionDefinition.publicId;
  return result;
}

export function canonicalSaveRows(rows: PerformancePeriodTargetDto[]): SaveTargetPeriodValuePayload[] {
  return rows.filter(row => row.isActive).map(row => {
    const result: SaveTargetPeriodValuePayload = {
      periodType: row.periodType as SaveTargetPeriodValuePayload['periodType'],
      unitKind: row.unitKind,
      direction: row.direction as SaveTargetPeriodValuePayload['direction'],
      targetValue: row.targetValue,
      budgetValue: row.budgetValue ?? null,
      description: row.description ?? null,
    };
    if (row.opmsUnitPublicId) result.opmsUnitPublicId = row.opmsUnitPublicId;
    if (row.performanceDirectionPublicId) result.performanceDirectionPublicId = row.performanceDirectionPublicId;
    return result;
  });
}
