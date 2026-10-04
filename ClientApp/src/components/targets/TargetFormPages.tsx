/* eslint-disable react-refresh/only-export-components */
import { useEffect, useState } from 'react';
import { ArrowLeft, BarChart3, Building2, CalendarRange, Save, Target, UserSquare2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormHero, FormPanel, FormRow, Input, Select, Textarea } from '../common/Form';
import { TargetPicker } from '../common/TargetPicker';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';
import { useApp } from '../../context/AppContext';
import { PerformancePeriodTargetEditor } from './PerformancePeriodTargetEditor';
import {
  createIpmsTarget,
  createOpmsTarget,
  getIpmsTarget,
  getIpmsTargetOptions,
  getIpmsTargetTemplate,
  getOpmsTarget,
  getOpmsTargetTemplate,
  updateIpmsTarget,
  updateOpmsTarget,
} from '../../api/api';
import { usePerformanceReferenceData } from '../../hooks/usePerformanceReferenceData';
import { canonicalPeriodTarget, performanceUnitValue } from '../../lib/performanceTargetContract';
import type {
  IPMSTarget,
  IpmsTargetTemplate,
  OPMSTarget,
  OpmsTargetTemplate,
  SaveIpmsTargetPayload,
  SaveOpmsTargetPayload,
  SaveTargetPeriodValuePayload,
  PerformanceTargetOptionDto,
  TargetUnitType,
  VoteNumberMasterDto,
  WardMasterDto,
  XafUnitValue,
} from '../../types';

const targetUnitTypeOptions: { value: TargetUnitType; label: string }[] = [
  { value: 'PercentageBased', label: 'Percentage Based' },
  { value: 'AbsoluteCount', label: 'Absolute Count' },
  { value: 'Financial', label: 'Financial' },
  { value: 'AreaBased', label: 'Area Based' },
  { value: 'VolumeBased', label: 'Volume Based' },
  { value: 'IndexScores', label: 'Index Scores' },
  { value: 'Ratios', label: 'Ratios' },
  { value: 'TimeBased', label: 'Time Based' },
  { value: 'Binary', label: 'Binary' },
  { value: 'Date', label: 'Date' },
  { value: 'ReadinessScale', label: 'Readiness Scale' },
  { value: 'QualitativeTargets', label: 'Qualitative Targets' },
  { value: 'ZeroBased', label: 'Zero Based' },
  { value: 'ReverseCumulative', label: 'Reverse Cumulative' },
  { value: 'ReverseNonCumulative', label: 'Reverse Non-Cumulative' },
  { value: 'BinaryDetermination', label: 'Binary Determination' },
  { value: 'None', label: 'None' },
];

const legacyToXafUnitMap: Record<string, XafUnitValue> = {
  percentage: 'PercentageBased',
  absolute_count: 'AbsoluteCount',
  financial: 'Financial',
  area_based: 'AreaBased',
  volume_based: 'VolumeBased',
  index_scores: 'IndexScores',
  ratios: 'Ratios',
  time_based: 'TimeBased',
  binary: 'Binary',
  date: 'Date',
  readiness_scale: 'ReadinessScale',
  qualitative: 'QualitativeTargets',
  zero_based: 'ZeroBased',
  reverse_cumulative: 'ReverseCumulative',
  reverse_non_cumulative: 'ReverseNonCumulative',
  binary_determination: 'BinaryDetermination',
};

const xafToLegacyUnitMap: Record<XafUnitValue, TargetUnitType> = {
  None: 'absolute_count',
  PercentageBased: 'percentage',
  AbsoluteCount: 'absolute_count',
  Financial: 'financial',
  TimeBased: 'time_based',
  AreaBased: 'area_based',
  VolumeBased: 'volume_based',
  IndexScores: 'index_scores',
  Ratios: 'ratios',
  Binary: 'binary',
  Date: 'date',
  ReadinessScale: 'readiness_scale',
  BinaryDetermination: 'binary_determination',
  QualitativeTargets: 'qualitative',
  ZeroBased: 'zero_based',
  ReverseCumulative: 'reverse_cumulative',
  ReverseNonCumulative: 'reverse_non_cumulative',
};

export function toXafUnitType(value: TargetUnitType): TargetUnitType;
export function toXafUnitType(value: string): string;
export function toXafUnitType(value: string): TargetUnitType | string {
  return legacyToXafUnitMap[value] ?? value;
}

export function toApiUnitType(value: TargetUnitType): TargetUnitType;
export function toApiUnitType(value: string): string;
export function toApiUnitType(value: string): TargetUnitType | string {
  return xafToLegacyUnitMap[value as XafUnitValue] ?? value;
}

export function getTargetUnitLabel(value: string) {
  return targetUnitTypeOptions.find(item => item.value === value)?.label ?? 'Target Value';
}

export function validateRequiredFields(values: Array<{ label: string; value: string | undefined }>) {
  return values
    .filter(item => !item.value || !item.value.trim())
    .map(item => `${item.label} is required.`);
}

function getFieldValidationError(errors: string[], label: string) {
  const normalizedLabel = label.toLowerCase();
  return errors.find(error => error.toLowerCase().startsWith(normalizedLabel));
}

type OpmsFormState = {
  sdbipLayerPublicId: string;
  sourceTemplateId: string;
  sourceTemplateVersion: string;
  periodId: string;
  departmentId: string;
  unitId: string;
  assignedToId: string;
  wardIds: string;
  additionalAssigneeIds: string;
  voteNumberIds: string;
  indicatorNumber: string;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoalId: string;
  strategicObjectiveId: string;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: string;
  baselineDescription: string;
  annualTarget: string;
  annualTargetDescription: string;
  budgetSourceId: string;
  budgetTypeId: string;
  unitOfMeasureId: string;
  weight: string;
  kpiType: string;
  indicatorType: string;
  functionalArea: string;
  standardClassification: string;
  idpReference: string;
  internalReference: string;
  fmsLink: string;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal: string;
  targetUnitType: TargetUnitType;
  q1UnitType: TargetUnitType;
  q2UnitType: TargetUnitType;
  midTermUnitType: TargetUnitType;
  q3UnitType: TargetUnitType;
  q4UnitType: TargetUnitType;
  annualUnitType: TargetUnitType;
  q1Target: string;
  q1Description: string;
  q1Budget: string;
  q2Target: string;
  q2Description: string;
  q2Budget: string;
  midTermTarget: string;
  midTermDescription: string;
  midTermBudget: string;
  q3Target: string;
  q3Description: string;
  q3Budget: string;
  q3RevisedTarget: string;
  q4Target: string;
  q4Description: string;
  q4Budget: string;
  q4RevisedTarget: string;
  revisedAnnualTarget: string;
  revisedAnnualBudget: string;
  createdOn: string;
  createdBy: string;
  updatedOn: string;
  updatedBy: string;
};

type IpmsFormState = {
  sourceTemplateId: string;
  sourceTemplateVersion: string;
  relatedOPMSTargetId: string;
  periodId: string;
  departmentId: string;
  unitId: string;
  assignedToId: string;
  supervisorId: string;
  indicatorNumber: string;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoalId: string;
  strategicObjectiveId: string;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: string;
  annualTarget: string;
  annualTargetDescription: string;
  budgetSourceId: string;
  budgetTypeId: string;
  unitOfMeasureId: string;
  weight: string;
  kpiType: string;
  indicatorType: string;
  functionalArea: string;
  idpReference: string;
  internalReference: string;
  isRevised: boolean;
  targetUnitType: TargetUnitType;
  q1UnitType: TargetUnitType;
  q2UnitType: TargetUnitType;
  midTermUnitType: TargetUnitType;
  q3UnitType: TargetUnitType;
  q4UnitType: TargetUnitType;
  annualUnitType: TargetUnitType;
  q1Target: string;
  q1Description: string;
  q1Budget: string;
  q2Target: string;
  q2Description: string;
  q2Budget: string;
  midTermTarget: string;
  midTermDescription: string;
  midTermBudget: string;
  q3Target: string;
  q3Description: string;
  q3Budget: string;
  q3RevisedTarget: string;
  q4Target: string;
  q4Description: string;
  q4Budget: string;
  q4RevisedTarget: string;
  revisedAnnualTarget: string;
  revisedAnnualBudget: string;
  createdOn: string;
  createdBy: string;
  updatedOn: string;
  updatedBy: string;
};

function numberText(value?: number) {
  return value === undefined || value === null ? '' : String(value);
}

function parseCsvIds(value: string) {
  return value
    .split(',')
    .map(item => item.trim())
    .filter(Boolean);
}

function appendCsvId(value: string, id: string) {
  const ids = new Set(parseCsvIds(value));
  ids.add(id);
  return Array.from(ids).join(',');
}

function removeCsvId(value: string, id: string) {
  return parseCsvIds(value)
    .filter(item => item !== id)
    .join(',');
}

function createDefaultOpmsFormState(): OpmsFormState {
  return {
    sdbipLayerPublicId: '',
    sourceTemplateId: '',
    sourceTemplateVersion: '',
    periodId: '',
    departmentId: '',
    unitId: '',
    assignedToId: '',
    wardIds: '',
    additionalAssigneeIds: '',
    voteNumberIds: '',
    indicatorNumber: '',
    nationalKPA: '',
    municipalKPA: '',
    strategicGoalId: '',
    strategicObjectiveId: '',
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    baselineDescription: '',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetSourceId: '',
    budgetTypeId: '',
    unitOfMeasureId: '',
    weight: '0',
    kpiType: '',
    indicatorType: '',
    functionalArea: '',
    standardClassification: '',
    idpReference: '',
    internalReference: '',
    fmsLink: '',
    isRevised: false,
    isWithdrawn: false,
    reasonForWithdrawal: '',
    targetUnitType: 'AbsoluteCount',
    q1UnitType: 'AbsoluteCount',
    q2UnitType: 'AbsoluteCount',
    midTermUnitType: 'AbsoluteCount',
    q3UnitType: 'AbsoluteCount',
    q4UnitType: 'AbsoluteCount',
    annualUnitType: 'AbsoluteCount',
    q1Target: '',
    q1Description: '',
    q1Budget: '',
    q2Target: '',
    q2Description: '',
    q2Budget: '',
    midTermTarget: '',
    midTermDescription: '',
    midTermBudget: '',
    q3Target: '',
    q3Description: '',
    q3Budget: '',
    q3RevisedTarget: '',
    q4Target: '',
    q4Description: '',
    q4Budget: '',
    q4RevisedTarget: '',
    revisedAnnualTarget: '',
    revisedAnnualBudget: '',
    createdOn: '',
    createdBy: '',
    updatedOn: '',
    updatedBy: '',
  };
}

function createDefaultIpmsFormState(): IpmsFormState {
  return {
    sourceTemplateId: '',
    sourceTemplateVersion: '',
    relatedOPMSTargetId: '',
    periodId: '',
    departmentId: '',
    unitId: '',
    assignedToId: '',
    supervisorId: '',
    indicatorNumber: '',
    nationalKPA: '',
    municipalKPA: '',
    strategicGoalId: '',
    strategicObjectiveId: '',
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetSourceId: '',
    budgetTypeId: '',
    unitOfMeasureId: '',
    weight: '0',
    kpiType: '',
    indicatorType: '',
    functionalArea: '',
    idpReference: '',
    internalReference: '',
    isRevised: false,
    targetUnitType: 'AbsoluteCount',
    q1UnitType: 'AbsoluteCount',
    q2UnitType: 'AbsoluteCount',
    midTermUnitType: 'AbsoluteCount',
    q3UnitType: 'AbsoluteCount',
    q4UnitType: 'AbsoluteCount',
    annualUnitType: 'AbsoluteCount',
    q1Target: '',
    q1Description: '',
    q1Budget: '',
    q2Target: '',
    q2Description: '',
    q2Budget: '',
    midTermTarget: '',
    midTermDescription: '',
    midTermBudget: '',
    q3Target: '',
    q3Description: '',
    q3Budget: '',
    q3RevisedTarget: '',
    q4Target: '',
    q4Description: '',
    q4Budget: '',
    q4RevisedTarget: '',
    revisedAnnualTarget: '',
    revisedAnnualBudget: '',
    createdOn: '',
    createdBy: '',
    updatedOn: '',
    updatedBy: '',
  };
}

function opmsFormFromTarget(target: OPMSTarget): OpmsFormState {
  const period = (periodType: number) => target.periodTargets.find(item => item.periodType === periodType && item.isActive);
  const q1 = period(1); const q2 = period(2); const midTerm = period(3);
  const q3 = period(4); const q4 = period(5); const annual = period(6);
  return {
    sdbipLayerPublicId: target.sdbipLayer?.publicId ?? '',
    sourceTemplateId: target.sourceTemplateId ?? '',
    sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
    periodId: target.period.id,
    departmentId: target.department.id,
    unitId: target.unit?.id ?? '',
    assignedToId: target.assignedTo?.id ?? '',
    wardIds: (target.wardIds ?? target.wards?.map(item => Number(item.id)) ?? []).join(','),
    additionalAssigneeIds: (target.additionalAssigneeIds ?? target.additionalAssignees.map(item => item.id)).join(','),
    voteNumberIds: (target.voteNumberIds ?? target.voteNumbers.map(item => Number(item.id))).join(','),
    indicatorNumber: target.indicatorNumber,
    nationalKPA: target.nationalKPA,
    municipalKPA: target.municipalKPA,
    strategicGoalId: target.strategicGoal.id,
    strategicObjectiveId: target.strategicObjective.id,
    performanceObjective: target.performanceObjective,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    baseline: String(target.baseline),
    baselineDescription: target.baselineDescription ?? '',
    annualTarget: annual?.targetValue ?? '',
    annualTargetDescription: annual?.description ?? '',
    budgetSourceId: target.budgetSource.id,
    budgetTypeId: target.budgetType.id,
    unitOfMeasureId: target.unitOfMeasure.id,
    weight: String(target.weight),
    kpiType: target.kpiType,
    indicatorType: target.indicatorType,
    functionalArea: target.functionalArea ?? '',
    standardClassification: target.standardClassification ?? '',
    idpReference: target.idpReference ?? '',
    internalReference: target.internalReference ?? '',
    fmsLink: target.fmsLink ?? '',
    isRevised: target.isRevised,
    isWithdrawn: target.isWithdrawn,
    reasonForWithdrawal: target.reasonForWithdrawal ?? '',
    targetUnitType: toXafUnitType(target.targetUnitType),
    q1UnitType: performanceUnitValue(q1?.unitKind),
    q2UnitType: performanceUnitValue(q2?.unitKind),
    midTermUnitType: performanceUnitValue(midTerm?.unitKind),
    q3UnitType: performanceUnitValue(q3?.unitKind),
    q4UnitType: performanceUnitValue(q4?.unitKind),
    annualUnitType: performanceUnitValue(annual?.unitKind),
    q1Target: q1?.targetValue ?? '',
    q1Description: q1?.description ?? '',
    q1Budget: numberText(q1?.budgetValue ?? undefined),
    q2Target: q2?.targetValue ?? '',
    q2Description: q2?.description ?? '',
    q2Budget: numberText(q2?.budgetValue ?? undefined),
    midTermTarget: midTerm?.targetValue ?? '',
    midTermDescription: midTerm?.description ?? '',
    midTermBudget: numberText(midTerm?.budgetValue ?? undefined),
    q3Target: q3?.targetValue ?? '',
    q3Description: q3?.description ?? '',
    q3Budget: numberText(q3?.budgetValue ?? undefined),
    q3RevisedTarget: numberText(target.q3RevisedTarget),
    q4Target: q4?.targetValue ?? '',
    q4Description: q4?.description ?? '',
    q4Budget: numberText(q4?.budgetValue ?? undefined),
    q4RevisedTarget: numberText(target.q4RevisedTarget),
    revisedAnnualTarget: numberText(target.revisedAnnualTarget),
    revisedAnnualBudget: numberText(target.revisedAnnualBudget),
    createdOn: target.CreatedOn ?? '',
    createdBy: target.CreatedBy ?? '',
    updatedOn: target.UpdatedOn ?? '',
    updatedBy: target.UpdatedBy ?? '',
  };
}

function ipmsFormFromTarget(target: IPMSTarget): IpmsFormState {
  const period = (periodType: number) => target.periodTargets.find(item => item.periodType === periodType && item.isActive);
  const q1 = period(1); const q2 = period(2); const midTerm = period(3);
  const q3 = period(4); const q4 = period(5); const annual = period(6);
  return {
    sourceTemplateId: target.sourceTemplateId ?? '',
    sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
    relatedOPMSTargetId: target.relatedOPMSTarget?.id ?? '',
    periodId: target.period.id,
    departmentId: target.department.id,
    unitId: target.unit?.id ?? '',
    assignedToId: target.assignedTo?.id ?? '',
    supervisorId: target.assignedTo?.manager?.id ?? '',
    indicatorNumber: target.indicatorNumber,
    nationalKPA: target.nationalKPA,
    municipalKPA: target.municipalKPA,
    strategicGoalId: target.strategicGoal.id,
    strategicObjectiveId: target.strategicObjective.id,
    performanceObjective: target.performanceObjective,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    baseline: String(target.baseline),
    annualTarget: annual?.targetValue ?? '',
    annualTargetDescription: annual?.description ?? '',
    budgetSourceId: target.budgetSource.id,
    budgetTypeId: target.budgetType.id,
    unitOfMeasureId: target.unitOfMeasure.id,
    weight: String(target.weight),
    kpiType: target.kpiType,
    indicatorType: target.indicatorType,
    functionalArea: target.functionalArea ?? '',
    idpReference: target.idpReference ?? '',
    internalReference: target.internalReference ?? '',
    isRevised: target.isRevised,
    targetUnitType: toXafUnitType(target.targetUnitType),
    q1UnitType: performanceUnitValue(q1?.unitKind),
    q2UnitType: performanceUnitValue(q2?.unitKind),
    midTermUnitType: performanceUnitValue(midTerm?.unitKind),
    q3UnitType: performanceUnitValue(q3?.unitKind),
    q4UnitType: performanceUnitValue(q4?.unitKind),
    annualUnitType: performanceUnitValue(annual?.unitKind),
    q1Target: q1?.targetValue ?? '',
    q1Description: q1?.description ?? '',
    q1Budget: numberText(q1?.budgetValue ?? undefined),
    q2Target: q2?.targetValue ?? '',
    q2Description: q2?.description ?? '',
    q2Budget: numberText(q2?.budgetValue ?? undefined),
    midTermTarget: midTerm?.targetValue ?? '',
    midTermDescription: midTerm?.description ?? '',
    midTermBudget: numberText(midTerm?.budgetValue ?? undefined),
    q3Target: q3?.targetValue ?? '',
    q3Description: q3?.description ?? '',
    q3Budget: numberText(q3?.budgetValue ?? undefined),
    q3RevisedTarget: '',
    q4Target: q4?.targetValue ?? '',
    q4Description: q4?.description ?? '',
    q4Budget: numberText(q4?.budgetValue ?? undefined),
    q4RevisedTarget: '',
    revisedAnnualTarget: '',
    revisedAnnualBudget: '',
    createdOn: target.CreatedOn ?? '',
    createdBy: target.CreatedBy ?? '',
    updatedOn: target.UpdatedOn ?? '',
    updatedBy: target.UpdatedBy ?? '',
  };
}

function opmsFormFromTemplate(template: OpmsTargetTemplate): OpmsFormState {
  const defaults = createDefaultOpmsFormState();
  return {
    ...defaults,
    sourceTemplateId: template.id,
    sourceTemplateVersion: String(template.version),
    departmentId: template.department?.id ?? defaults.departmentId,
    indicatorNumber: template.indicatorNumber,
    nationalKPA: template.nationalKPA,
    municipalKPA: template.municipalKPA,
    strategicGoalId: template.strategicGoal?.id ?? defaults.strategicGoalId,
    strategicObjectiveId: template.strategicObjective?.id ?? defaults.strategicObjectiveId,
    performanceObjective: template.performanceObjective,
    targetName: template.targetName,
    kpiDescription: template.kpiDescription,
    baseline: String(template.baseline),
    annualTarget: String(template.annualTarget),
    annualTargetDescription: template.annualTargetDescription,
    budgetSourceId: template.budgetSource?.id ?? defaults.budgetSourceId,
    budgetTypeId: template.budgetType?.id ?? defaults.budgetTypeId,
    unitOfMeasureId: template.unitOfMeasure.id,
    weight: String(template.weight),
    kpiType: template.kpiType,
    indicatorType: template.indicatorType,
    functionalArea: template.functionalArea ?? '',
    standardClassification: template.standardClassification ?? '',
    idpReference: template.idpReference ?? '',
    internalReference: template.internalReference ?? '',
    fmsLink: template.fmsLink ?? '',
    targetUnitType: toXafUnitType(template.targetUnitType),
    q1UnitType: toXafUnitType(template.targetUnitType),
    q2UnitType: toXafUnitType(template.targetUnitType),
    midTermUnitType: toXafUnitType(template.targetUnitType),
    q3UnitType: toXafUnitType(template.targetUnitType),
    q4UnitType: toXafUnitType(template.targetUnitType),
    annualUnitType: toXafUnitType(template.targetUnitType),
    q1Target: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q1')?.target),
    q1Description: template.defaultQuarterlyTargets.find(item => item.quarter === 'Q1')?.description ?? '',
    q1Budget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q1')?.budget),
    q2Target: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q2')?.target),
    q2Description: template.defaultQuarterlyTargets.find(item => item.quarter === 'Q2')?.description ?? '',
    q2Budget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q2')?.budget),
    midTermTarget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Mid-Year')?.target),
    midTermDescription: template.defaultQuarterlyTargets.find(item => item.quarter === 'Mid-Year')?.description ?? '',
    midTermBudget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Mid-Year')?.budget),
    q3Target: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q3')?.target),
    q3Description: template.defaultQuarterlyTargets.find(item => item.quarter === 'Q3')?.description ?? '',
    q3Budget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q3')?.budget),
    q4Target: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q4')?.target),
    q4Description: template.defaultQuarterlyTargets.find(item => item.quarter === 'Q4')?.description ?? '',
    q4Budget: numberText(template.defaultQuarterlyTargets.find(item => item.quarter === 'Q4')?.budget),
  };
}

function ipmsFormFromTemplate(template: IpmsTargetTemplate): IpmsFormState {
  const defaults = createDefaultIpmsFormState();
  return {
    ...defaults,
    sourceTemplateId: template.id,
    sourceTemplateVersion: String(template.version),
    departmentId: template.department?.id ?? defaults.departmentId,
    indicatorNumber: template.templateCode,
    targetName: template.targetName,
    kpiDescription: template.kpiDescription,
    annualTarget: String(template.annualTarget),
    annualTargetDescription: template.annualTargetDescription,
    unitOfMeasureId: template.unitOfMeasure.id,
    weight: String(template.weight),
    functionalArea: template.functionalArea ?? '',
    targetUnitType: toXafUnitType(template.targetUnitType),
    q1UnitType: toXafUnitType(template.targetUnitType),
    q2UnitType: toXafUnitType(template.targetUnitType),
    midTermUnitType: toXafUnitType(template.targetUnitType),
    q3UnitType: toXafUnitType(template.targetUnitType),
    q4UnitType: toXafUnitType(template.targetUnitType),
    annualUnitType: toXafUnitType(template.targetUnitType),
  };
}

function periodTarget(
  periodType: 1 | 2 | 3 | 4 | 5 | 6,
  targetValue?: string,
  unitValue?: string,
  budgetValue?: string,
  description?: string,
): SaveTargetPeriodValuePayload | null {
  if (!targetValue?.trim()) return null;
  return canonicalPeriodTarget(periodType, targetValue, unitValue ?? 'AbsoluteCount', budgetValue ? Number(budgetValue) : null, description);
}

function targetValueInputType(value: string): 'text' | 'number' | 'date' {
  const unit = toXafUnitType(value);
  if (unit === 'Date') return 'date';
  if (['None', 'QualitativeTargets', 'Binary', 'BinaryDetermination', 'Ratios'].includes(unit)) return 'text';
  return 'number';
}

function buildCanonicalPeriodTargets(form: OpmsFormState | IpmsFormState): SaveTargetPeriodValuePayload[] {
  return [
    periodTarget(1, form.q1Target, form.q1UnitType, form.q1Budget, form.q1Description),
    periodTarget(2, form.q2Target, form.q2UnitType, form.q2Budget, form.q2Description),
    periodTarget(3, form.midTermTarget, form.midTermUnitType, form.midTermBudget, form.midTermDescription),
    periodTarget(4, form.q3Target, form.q3UnitType, form.q3Budget, form.q3Description),
    periodTarget(5, form.q4Target, form.q4UnitType, form.q4Budget, form.q4Description),
    periodTarget(6, form.annualTarget, form.annualUnitType, '', form.annualTargetDescription),
  ].filter((item): item is SaveTargetPeriodValuePayload => item !== null);
}

export function buildOpmsPayload(form: OpmsFormState): SaveOpmsTargetPayload {
  return {
    sdbipLayerPublicId: form.sdbipLayerPublicId || null,
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    periodId: form.periodId ? Number(form.periodId) : null,
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    unitId: form.unitId ? Number(form.unitId) : null,
    assignedUserId: form.assignedToId || null,
    wardIds: [...new Set(parseCsvIds(form.wardIds).map(Number).filter(Number.isSafeInteger))],
    additionalAssigneeIds: [...new Set(parseCsvIds(form.additionalAssigneeIds))],
    voteNumberIds: [...new Set(parseCsvIds(form.voteNumberIds).map(Number).filter(Number.isSafeInteger))],
    indicatorNumber: form.indicatorNumber,
    nationalKpa: form.nationalKPA,
    municipalKpa: form.municipalKPA,
    strategicGoalId: form.strategicGoalId ? Number(form.strategicGoalId) : null,
    strategicObjectiveId: form.strategicObjectiveId ? Number(form.strategicObjectiveId) : null,
    performanceObjective: form.performanceObjective,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    baseline: Number(form.baseline || 0),
    baselineDescription: form.baselineDescription || null,
    budgetSourceId: form.budgetSourceId ? Number(form.budgetSourceId) : null,
    budgetTypeId: form.budgetTypeId ? Number(form.budgetTypeId) : null,
    unitOfMeasureId: form.unitOfMeasureId ? Number(form.unitOfMeasureId) : null,
    weight: Number(form.weight || 0),
    kpiType: form.kpiType,
    indicatorType: form.indicatorType,
    functionalArea: form.functionalArea || null,
    standardClassification: form.standardClassification || null,
    idpReference: form.idpReference || null,
    internalReference: form.internalReference || null,
    fmsLink: form.fmsLink || null,
    isRevised: form.isRevised,
    periodTargets: buildCanonicalPeriodTargets(form),
  };
}

function buildIpmsPayload(form: IpmsFormState): SaveIpmsTargetPayload {
  return {
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    relatedOpmsTargetId: form.relatedOPMSTargetId || null,
    periodId: form.periodId ? Number(form.periodId) : null,
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    unitId: form.unitId ? Number(form.unitId) : null,
    assignedUserId: form.assignedToId || null,
    supervisorId: form.supervisorId || null,
    indicatorNumber: form.indicatorNumber,
    nationalKpa: form.nationalKPA,
    municipalKpa: form.municipalKPA,
    strategicGoalId: form.strategicGoalId ? Number(form.strategicGoalId) : null,
    strategicObjectiveId: form.strategicObjectiveId ? Number(form.strategicObjectiveId) : null,
    performanceObjective: form.performanceObjective,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    baseline: Number(form.baseline || 0),
    budgetSourceId: form.budgetSourceId ? Number(form.budgetSourceId) : null,
    budgetTypeId: form.budgetTypeId ? Number(form.budgetTypeId) : null,
    unitOfMeasureId: form.unitOfMeasureId ? Number(form.unitOfMeasureId) : null,
    weight: Number(form.weight || 0),
    kpiType: form.kpiType,
    indicatorType: form.indicatorType,
    functionalArea: form.functionalArea || null,
    idpReference: form.idpReference || null,
    internalReference: form.internalReference || null,
    isRevised: form.isRevised,
    periodTargets: buildCanonicalPeriodTargets(form),
  };
}

function validateOpmsForm(form: OpmsFormState) {
  const errors = validateRequiredFields([
    { label: 'SDBIP Layer', value: form.sdbipLayerPublicId },
    { label: 'Period', value: form.periodId },
    { label: 'Department', value: form.departmentId },
    { label: 'Indicator Number', value: form.indicatorNumber },
    { label: 'Target Name', value: form.targetName },
    { label: 'National KPA', value: form.nationalKPA },
    { label: 'Municipal KPA', value: form.municipalKPA },
    { label: 'Performance Objective', value: form.performanceObjective },
    { label: 'KPI Description', value: form.kpiDescription },
    { label: 'Annual Target', value: form.annualTarget },
    { label: 'Weight %', value: form.weight },
    { label: 'Unit of Measure', value: form.unitOfMeasureId },
  ]);


  return errors;
}

function validateIpmsForm(form: IpmsFormState) {
  return validateRequiredFields([
    { label: 'Period', value: form.periodId },
    { label: 'Department', value: form.departmentId },
    { label: 'Indicator Number', value: form.indicatorNumber },
    { label: 'Target Name', value: form.targetName },
    { label: 'National KPA', value: form.nationalKPA },
    { label: 'Municipal KPA', value: form.municipalKPA },
    { label: 'Performance Objective', value: form.performanceObjective },
    { label: 'KPI Description', value: form.kpiDescription },
    { label: 'Annual Target', value: form.annualTarget },
    { label: 'Weight %', value: form.weight },
    { label: 'Unit of Measure', value: form.unitOfMeasureId },
  ]);
}

export function OPMSTargetFormPage({ targetId }: { targetId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const referenceData = usePerformanceReferenceData(true);
  const [form, setForm] = useState<OpmsFormState>(createDefaultOpmsFormState());
  const [existingTarget, setExistingTarget] = useState<OPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [selectedWard, setSelectedWard] = useState<WardMasterDto>();
  const [selectedAssigneeId, setSelectedAssigneeId] = useState('');
  const [selectedVoteNumber, setSelectedVoteNumber] = useState<VoteNumberMasterDto>();
  const [referenceLabels, setReferenceLabels] = useState<Record<string, string>>({});
  const [relatedIpmsTargets, setRelatedIpmsTargets] = useState<PerformanceTargetOptionDto[]>([]);
  const [relatedIpmsPage, setRelatedIpmsPage] = useState(1);
  const [relatedIpmsTotalPages, setRelatedIpmsTotalPages] = useState(0);
  const [relatedIpmsTotalCount, setRelatedIpmsTotalCount] = useState(0);

  useEffect(() => {
    let cancelled = false;
    const loadRelated = async () => {
      if (!existingTarget?.publicId) {
        setRelatedIpmsTargets([]);
        setRelatedIpmsTotalCount(0);
        setRelatedIpmsTotalPages(0);
        return;
      }
      const result = await getIpmsTargetOptions({
        page: relatedIpmsPage,
        pageSize: 25,
        sortBy: 'indicatorNumber',
        sortDirection: 'asc',
        relatedOpmsTargetPublicId: existingTarget.publicId,
      });
      if (cancelled || !result.success || !result.data) return;
      setRelatedIpmsTargets(result.data.items);
      setRelatedIpmsTotalCount(result.data.totalCount);
      setRelatedIpmsTotalPages(result.data.totalPages);
    };
    void loadRelated();
    return () => { cancelled = true; };
  }, [existingTarget?.publicId, relatedIpmsPage]);

  useEffect(() => {
    const initialize = async () => {
      setIsLoading(true);

      if (targetId) {
        const result = await getOpmsTarget(targetId);
        if (result.success && result.data) {
          setExistingTarget(result.data);
          setForm(opmsFormFromTarget(result.data));
        } else {
          pushToast('error', result.message ?? 'Failed to load OPMS target');
        }
        setIsLoading(false);
        return;
      }

      const pendingTemplateId = localStorage.getItem('pending_opms_template_id');
      if (!pendingTemplateId) {
        setExistingTarget(null);
        setForm(createDefaultOpmsFormState());
        setIsLoading(false);
        return;
      }

      localStorage.removeItem('pending_opms_template_id');
      const templateResult = await getOpmsTargetTemplate(pendingTemplateId);
      if (templateResult.success && templateResult.data) {
        setForm(opmsFormFromTemplate(templateResult.data));
      } else {
        pushToast('error', templateResult.message ?? 'Failed to load OPMS template');
        setForm(createDefaultOpmsFormState());
      }
      setIsLoading(false);
    };

    void initialize();
  }, [pushToast, targetId]);

  const handleSave = async () => {
    const errors = validateOpmsForm(form);
    if (errors.length > 0) {
      setValidationErrors(errors);
      pushToast('error', 'Resolve validation issues before saving');
      return;
    }

    setValidationErrors([]);
    const payload = buildOpmsPayload(form);
    const result = targetId
      ? await updateOpmsTarget(targetId, payload)
      : await createOpmsTarget(payload);

    if (result.success && result.data) {
      pushToast('success', targetId ? 'OPMS target updated' : 'OPMS target created');
      setCurrentPath(`/opms/targets/${result.data.id}`);
      return;
    }

    pushToast('error', result.message ?? `Failed to ${targetId ? 'update' : 'create'} OPMS target`);
  };

  if (isLoading || referenceData.isLoading) {
    return (
      <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full OPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  if (referenceData.error) {
    return (
      <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full OPMS target workspace">
        <Card><p className="text-sm text-error-600">{referenceData.error}</p></Card>
      </AppShell>
    );
  }

  const { departments, units, employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups } = referenceData;
  const employeeIdentityOptions = employees.filter(item => item.identityUserId).map(item => ({ value: item.identityUserId!, label: `${item.firstName} ${item.lastName}` }));
  const selectedDepartment = departments.find(item => String(item.id) === form.departmentId);
  const selectedPeriod = lookups.periods.find(item => String(item.id) === form.periodId);
  const selectedUnit = units.find(item => String(item.id) === form.unitId);
  const selectedUom = lookups.unitsOfMeasure.find(item => String(item.id) === form.unitOfMeasureId);
  const selectedWardIds = parseCsvIds(form.wardIds);
  const selectedAssigneeIds = parseCsvIds(form.additionalAssigneeIds);
  const selectedVoteIds = parseCsvIds(form.voteNumberIds);
  const fieldError = (label: string) => getFieldValidationError(validationErrors, label);

  return (
    <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full-page OPMS target capture workspace">
      <div className="space-y-6">
        <div className="flex items-center justify-between gap-3">
          <Button variant="ghost" icon={<ArrowLeft className="h-4 w-4" />} onClick={() => setCurrentPath(targetId ? `/opms/targets/${targetId}` : '/opms/targets')}>
            Back
          </Button>
          <div className="flex gap-2">
            <Badge variant="info">{targetId ? 'Edit Mode' : 'New Record'}</Badge>
            <Badge variant="default">OPMS</Badge>
            {form.sourceTemplateId && <Badge variant="primary">Template Based</Badge>}
          </div>
        </div>

        <FormHero
          eyebrow="OPMS Target Workspace"
          title={targetId ? 'Update OPMS target using the full page form' : 'Create OPMS target using the full page form'}
          description="This workspace replaces the old modal flow. Capture planning setup, full target definition, measurement details, quarterly structure, and references on one actual page."
          badges={<Badge variant="default">{targetId ? existingTarget?.indicatorNumber ?? 'Edit' : 'Create'}</Badge>}
        />

        {validationErrors.length > 0 && (
          <Card className="border border-error-200 bg-error-50 dark:border-error-800 dark:bg-error-900/20">
            <h3 className="text-sm font-semibold text-error-700 dark:text-error-200">Validation Summary</h3>
            <ul className="mt-2 space-y-1 text-xs text-error-700 dark:text-error-200">
              {validationErrors.map(error => (
                <li key={error}>- {error}</li>
              ))}
            </ul>
          </Card>
        )}

        <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
          <FormPanel title="Planning Setup" description="Define ownership, period, template source, and local assignment information." icon={<CalendarRange className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Source Template Id" value={form.sourceTemplateId} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateId: event.target.value }))} />
              <Input label="Template Version" value={form.sourceTemplateVersion} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateVersion: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Period" required error={fieldError('Period')} value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={lookups.periods.map(item => ({ value: String(item.id), label: item.name }))} />
              <CalendarMasterPicker kind="sdbip-layer" label="SDBIP Layer" required value={form.sdbipLayerPublicId} selectedLabel={existingTarget?.sdbipLayer ? `${existingTarget.sdbipLayer.code} · ${existingTarget.sdbipLayer.name}` : undefined} onChange={value => setForm(prev => ({ ...prev, sdbipLayerPublicId: value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Department" required error={fieldError('Department')} value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={departments.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...units.filter(item => !form.departmentId || String(item.departmentId) === form.departmentId).map(item => ({ value: String(item.id), label: item.name }))]} />
            </FormRow>
            <Input label="Search employees" value={employeeSearch} onChange={(event) => setEmployeeSearch(event.target.value)} />
            <Select label="Assigned User" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...(form.assignedToId && !employeeIdentityOptions.some(item => item.value === form.assignedToId) ? [{ value: form.assignedToId, label: 'Current assigned employee' }] : []), ...employeeIdentityOptions]} />
            <FormRow cols={3}>
              <OrganizationMasterPicker kind="ward" label="Wards" value={selectedWard?.publicId ?? ''} emptyLabel="Select Ward" onChange={(_, option) => setSelectedWard(option as WardMasterDto | undefined)} />
              <div className="flex items-end">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    if (!selectedWard) return;
                    const id = String(selectedWard.id);
                    setForm(prev => ({ ...prev, wardIds: appendCsvId(prev.wardIds, id) }));
                    setReferenceLabels(current => ({ ...current, [`ward:${id}`]: selectedWard.name }));
                    setSelectedWard(undefined);
                  }}
                >
                  Add Ward
                </Button>
              </div>
              <Input label="Ward Ids" value={form.wardIds} readOnly helpText="Maintained automatically by the collection editor." />
            </FormRow>
            <div className="flex flex-wrap gap-2">
              {selectedWardIds.length === 0 ? <p className="text-xs text-secondary-500">No wards linked.</p> : null}
              {selectedWardIds.map(wardId => {
                const wardName = referenceLabels[`ward:${wardId}`] ?? existingTarget?.wards?.find(item => String(item.id) === wardId)?.name;
                return (
                  <button
                    key={wardId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, wardIds: removeCsvId(prev.wardIds, wardId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove ward"
                  >
                    {wardName ?? wardId} x
                  </button>
                );
              })}
            </div>

            <FormRow cols={3}>
              <Select
                label="Additional Assignees"
                value={selectedAssigneeId}
                onChange={(event) => setSelectedAssigneeId(event.target.value)}
                options={[
                  { value: '', label: 'Select Employee' },
                  ...employeeIdentityOptions,
                ]}
              />
              <div className="flex items-end">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    if (!selectedAssigneeId) return;
                    setForm(prev => ({ ...prev, additionalAssigneeIds: appendCsvId(prev.additionalAssigneeIds, selectedAssigneeId) }));
                    setSelectedAssigneeId('');
                  }}
                >
                  Add Assignee
                </Button>
              </div>
              <Input label="Assignee Ids" value={form.additionalAssigneeIds} readOnly helpText="Maintained automatically by the collection editor." />
            </FormRow>
            <div className="flex flex-wrap gap-2">
              {selectedAssigneeIds.length === 0 ? <p className="text-xs text-secondary-500">No additional assignees linked.</p> : null}
              {selectedAssigneeIds.map(assigneeId => {
                const assignee = employees.find(item => item.identityUserId === assigneeId);
                return (
                  <button
                    key={assigneeId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, additionalAssigneeIds: removeCsvId(prev.additionalAssigneeIds, assigneeId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove assignee"
                  >
                    {assignee ? `${assignee.firstName} ${assignee.lastName}` : assigneeId} x
                  </button>
                );
              })}
            </div>
            {employeeTotalPages > 1 && <div className="flex items-center gap-2 text-xs text-secondary-500"><Button size="sm" variant="outline" disabled={employeePage <= 1} onClick={() => setEmployeePage(value => Math.max(1, value - 1))}>Previous employees</Button><span>Page {employeePage} of {employeeTotalPages}</span><Button size="sm" variant="outline" disabled={employeePage >= employeeTotalPages} onClick={() => setEmployeePage(value => value + 1)}>Next employees</Button></div>}

            <FormRow cols={3}>
              <OrganizationMasterPicker kind="vote-number" label="Vote Numbers" value={selectedVoteNumber?.publicId ?? ''} emptyLabel="Select Vote Number" onChange={(_, option) => setSelectedVoteNumber(option as VoteNumberMasterDto | undefined)} />
              <div className="flex items-end">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    if (!selectedVoteNumber) return;
                    const id = String(selectedVoteNumber.id);
                    setForm(prev => ({ ...prev, voteNumberIds: appendCsvId(prev.voteNumberIds, id) }));
                    setReferenceLabels(current => ({ ...current, [`vote:${id}`]: `${selectedVoteNumber.number} - ${selectedVoteNumber.name}` }));
                    setSelectedVoteNumber(undefined);
                  }}
                >
                  Add Vote Number
                </Button>
              </div>
              <Input label="Vote Number Ids" value={form.voteNumberIds} readOnly helpText="Maintained automatically by the collection editor." />
            </FormRow>
            <div className="flex flex-wrap gap-2">
              {selectedVoteIds.length === 0 ? <p className="text-xs text-secondary-500">No vote numbers linked.</p> : null}
              {selectedVoteIds.map(voteId => {
                const existingVote = existingTarget?.voteNumbers?.find(item => String(item.id) === voteId);
                const voteLabel = referenceLabels[`vote:${voteId}`] ?? (existingVote ? `${existingVote.number} - ${existingVote.name}` : undefined);
                return (
                  <button
                    key={voteId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, voteNumberIds: removeCsvId(prev.voteNumberIds, voteId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove vote number"
                  >
                    {voteLabel ?? voteId} x
                  </button>
                );
              })}
            </div>
          </FormPanel>

          <FormPanel title="Target Definition" description="Capture the full identity and strategic alignment of the OPMS target." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Indicator Number" required error={fieldError('Indicator Number')} value={form.indicatorNumber} onChange={(event) => setForm(prev => ({ ...prev, indicatorNumber: event.target.value }))} />
              <Input label="Target Name" required error={fieldError('Target Name')} value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="National KPA" required value={form.nationalKPA} onChange={(event) => setForm(prev => ({ ...prev, nationalKPA: event.target.value }))} />
              <Input label="Municipal KPA" required value={form.municipalKPA} onChange={(event) => setForm(prev => ({ ...prev, municipalKPA: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value, strategicObjectiveId: '' }))} options={lookups.strategicGoals.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={lookups.strategicObjectives.filter(item => !form.strategicGoalId || String(item.strategicGoalId) === form.strategicGoalId).map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Measurement Details" description="Define numeric measures, unit configuration, and performance classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" required type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type={targetValueInputType(form.annualUnitType)} value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={lookups.unitsOfMeasure.map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <Textarea label="Baseline Description" rows={3} value={form.baselineDescription} onChange={(event) => setForm(prev => ({ ...prev, baselineDescription: event.target.value }))} />
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
            <FormRow cols={4}>
              <Select
                label="Target Unit Type"
                required
                value={form.targetUnitType}
                onChange={(event) => {
                  const unitType = event.target.value as TargetUnitType;
                  setForm(prev => ({
                    ...prev,
                    targetUnitType: unitType,
                    q1UnitType: unitType,
                    q2UnitType: unitType,
                    midTermUnitType: unitType,
                    q3UnitType: unitType,
                    q4UnitType: unitType,
                    annualUnitType: unitType,
                  }));
                }}
                options={targetUnitTypeOptions}
              />
              <Input label="KPI Type" value={form.kpiType} onChange={(event) => setForm(prev => ({ ...prev, kpiType: event.target.value }))} />
              <Input label="Indicator Type" value={form.indicatorType} onChange={(event) => setForm(prev => ({ ...prev, indicatorType: event.target.value }))} />
              <Input label="Functional Area" value={form.functionalArea} onChange={(event) => setForm(prev => ({ ...prev, functionalArea: event.target.value }))} />
            </FormRow>
            <Input label="Standard Classification" value={form.standardClassification} onChange={(event) => setForm(prev => ({ ...prev, standardClassification: event.target.value }))} />
          </FormPanel>

          <FormPanel title="Budget And References" description="Capture budget linkage, identifiers, and external references." icon={<Building2 className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Select label="Budget Source" value={form.budgetSourceId} onChange={(event) => setForm(prev => ({ ...prev, budgetSourceId: event.target.value }))} options={lookups.budgetSources.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Budget Type" value={form.budgetTypeId} onChange={(event) => setForm(prev => ({ ...prev, budgetTypeId: event.target.value }))} options={lookups.budgetTypes.map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="IDP Reference" value={form.idpReference} onChange={(event) => setForm(prev => ({ ...prev, idpReference: event.target.value }))} />
              <Input label="Internal Reference" value={form.internalReference} onChange={(event) => setForm(prev => ({ ...prev, internalReference: event.target.value }))} />
            </FormRow>
            <Input label="FMS Link" value={form.fmsLink} onChange={(event) => setForm(prev => ({ ...prev, fmsLink: event.target.value }))} />
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Department</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedDepartment?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Period</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedPeriod?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Unit</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUnit?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Measure</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUom?.name ?? '-'}</p>
              </div>
            </div>
          </FormPanel>
        </div>

        {targetId && existingTarget?.publicId && <PerformancePeriodTargetEditor kind={1} targetPublicId={existingTarget.publicId} />}
        <div className={targetId ? 'hidden' : ''} aria-hidden={targetId ? true : undefined}>
        <FormPanel title="Initial legacy quarterly values" description="Used only while creating the KPI. After creation, authoritative values are maintained by reporting period." icon={<CalendarRange className="h-5 w-5" />}>
          <FormRow cols={3}>
            <Select label="Q1 Unit" value={form.q1UnitType} onChange={(event) => setForm(prev => ({ ...prev, q1UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Q2 Unit" value={form.q2UnitType} onChange={(event) => setForm(prev => ({ ...prev, q2UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Mid-Year Unit" value={form.midTermUnitType} onChange={(event) => setForm(prev => ({ ...prev, midTermUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
          </FormRow>
          <FormRow cols={3}>
            <Select label="Q3 Unit" value={form.q3UnitType} onChange={(event) => setForm(prev => ({ ...prev, q3UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Q4 Unit" value={form.q4UnitType} onChange={(event) => setForm(prev => ({ ...prev, q4UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Annual Unit" value={form.annualUnitType} onChange={(event) => setForm(prev => ({ ...prev, annualUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
          </FormRow>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {/* Q1 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 1</h4>
                  <p className="text-xs text-secondary-500">Jul – Sep</p>
                </div>
                {form.q1Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q1Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q1UnitType)} value={form.q1Target} onChange={(e) => setForm(prev => ({ ...prev, q1Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q1Description} onChange={(e) => setForm(prev => ({ ...prev, q1Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q1UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q2 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 2</h4>
                  <p className="text-xs text-secondary-500">Oct – Dec</p>
                </div>
                {form.q2Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q2Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q2UnitType)} value={form.q2Target} onChange={(e) => setForm(prev => ({ ...prev, q2Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q2Description} onChange={(e) => setForm(prev => ({ ...prev, q2Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q2UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Mid-Year */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden ring-2 ring-blue-500`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-blue-50 dark:bg-blue-900/20">
                <div>
                  <h4 className="text-sm font-semibold text-blue-900 dark:text-blue-100">Mid-Year</h4>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Jan – Jun</p>
                </div>
                {form.midTermBudget && <span className="text-xs font-semibold text-blue-700 dark:text-blue-300">BUDGET R {Number(form.midTermBudget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.midTermUnitType)} value={form.midTermTarget} onChange={(e) => setForm(prev => ({ ...prev, midTermTarget: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.midTermDescription} onChange={(e) => setForm(prev => ({ ...prev, midTermDescription: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.midTermUnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q3 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 3</h4>
                  <p className="text-xs text-secondary-500">Jan – Mar</p>
                </div>
                {form.q3Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q3Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q3UnitType)} value={form.q3Target} onChange={(e) => setForm(prev => ({ ...prev, q3Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q3Description} onChange={(e) => setForm(prev => ({ ...prev, q3Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q3UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q4 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 4</h4>
                  <p className="text-xs text-secondary-500">Apr – Jun</p>
                </div>
                {form.q4Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q4Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q4UnitType)} value={form.q4Target} onChange={(e) => setForm(prev => ({ ...prev, q4Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q4Description} onChange={(e) => setForm(prev => ({ ...prev, q4Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q4UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Annual */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Annual</h4>
                  <p className="text-xs text-secondary-500">Full year</p>
                </div>
                {form.annualTarget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.annualTarget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <p className="text-2xl font-bold">{form.annualTarget || 0}</p>
                </div>
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.annualUnitType)}</span>
                </div>
              </div>
            </Card>
          </div>

          <div className="mt-4 bg-secondary-50 dark:bg-secondary-900/50 p-3 rounded-lg flex items-center justify-between">
            <p className="text-xs text-secondary-600">Required fields marked • changes auto-save as draft</p>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setCurrentPath(targetId ? `/opms/targets/${targetId}` : '/opms/targets')}>Cancel</Button>
              <Button variant="outline" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>Save Draft</Button>
              <Button variant="primary" onClick={() => { void handleSave(); }}>Submit for Approval</Button>
            </div>
          </div>
        </FormPanel>
        </div>

        <FormPanel title="Workflow Flags" description="Track revision and withdrawal attributes for the live target." icon={<Building2 className="h-5 w-5" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Checkbox label="Target Revised" checked={form.isRevised} onChange={(event) => setForm(prev => ({ ...prev, isRevised: event.target.checked }))} />
          </div>
          {form.isWithdrawn ? (
            <div className="rounded-lg border border-error-200 bg-error-50 p-3 text-xs text-error-800 dark:border-error-800 dark:bg-error-950/30 dark:text-error-200">
              This target is withdrawn and cannot be edited. Reason: {form.reasonForWithdrawal || 'Recorded in lifecycle history'}
            </div>
          ) : null}
        </FormPanel>

        <FormPanel title="Related Targets" description="Track linked child resources and associated IPMS records." icon={<Target className="h-5 w-5" />}>
          <div className="space-y-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-secondary-500">Linked IPMS Targets</p>
              {relatedIpmsTargets.length === 0 ? (
                <p className="mt-1 text-sm text-secondary-500">No related IPMS targets linked yet.</p>
              ) : (
                <ul className="mt-2 space-y-2">
                  {relatedIpmsTargets.map(item => (
                    <li key={item.id} className="rounded-lg border border-secondary-200 px-3 py-2 text-sm text-secondary-700 dark:border-secondary-700 dark:text-secondary-200">
                      {item.indicatorNumber} - {item.targetName}
                    </li>
                  ))}
                </ul>
              )}
              {relatedIpmsTotalCount > 0 && <div className="mt-2 flex items-center justify-between text-xs text-secondary-500"><span>{relatedIpmsTotalCount} linked target{relatedIpmsTotalCount === 1 ? '' : 's'}</span><span className="flex items-center gap-2"><Button type="button" size="sm" variant="ghost" disabled={relatedIpmsPage <= 1} onClick={() => setRelatedIpmsPage(value => Math.max(1, value - 1))}>Previous</Button><span>Page {relatedIpmsPage} of {Math.max(relatedIpmsTotalPages, 1)}</span><Button type="button" size="sm" variant="ghost" disabled={relatedIpmsPage >= relatedIpmsTotalPages} onClick={() => setRelatedIpmsPage(value => value + 1)}>Next</Button></span></div>}
            </div>
            <p className="text-xs text-amber-700 dark:text-amber-300">
              Archive/Cascade warning: unlinking or archiving parent OPMS targets should be validated against linked IPMS targets before save.
            </p>
          </div>
        </FormPanel>

        <FormPanel title="Audit Metadata" description="Read-only audit fields mirrored from target history." icon={<Building2 className="h-5 w-5" />}>
          <FormRow cols={2}>
            <Input label="Created On" value={form.createdOn || '-'} readOnly />
            <Input label="Created By" value={form.createdBy || '-'} readOnly />
          </FormRow>
          <FormRow cols={2}>
            <Input label="Updated On" value={form.updatedOn || '-'} readOnly />
            <Input label="Updated By" value={form.updatedBy || '-'} readOnly />
          </FormRow>
        </FormPanel>


      </div>
    </AppShell>
  );
}

export function IPMSTargetFormPage({ targetId }: { targetId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const referenceData = usePerformanceReferenceData(true);
  const [form, setForm] = useState<IpmsFormState>(createDefaultIpmsFormState());
  const [existingTarget, setExistingTarget] = useState<IPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [linkedOpmsLabel, setLinkedOpmsLabel] = useState('');

  useEffect(() => {
    const initialize = async () => {
      setIsLoading(true);

      if (targetId) {
        const result = await getIpmsTarget(targetId);
        if (result.success && result.data) {
          setExistingTarget(result.data);
          setForm(ipmsFormFromTarget(result.data));
        } else {
          pushToast('error', result.message ?? 'Failed to load IPMS target');
        }
        setIsLoading(false);
        return;
      }

      const pendingTemplateId = localStorage.getItem('pending_ipms_template_id');
      if (!pendingTemplateId) {
        setExistingTarget(null);
        setForm(createDefaultIpmsFormState());
        setIsLoading(false);
        return;
      }

      localStorage.removeItem('pending_ipms_template_id');
      const templateResult = await getIpmsTargetTemplate(pendingTemplateId);
      if (templateResult.success && templateResult.data) {
        setForm(ipmsFormFromTemplate(templateResult.data));
      } else {
        pushToast('error', templateResult.message ?? 'Failed to load IPMS template');
        setForm(createDefaultIpmsFormState());
      }
      setIsLoading(false);
    };

    void initialize();
  }, [pushToast, targetId]);

  const handleSave = async () => {
    const errors = validateIpmsForm(form);
    if (errors.length > 0) {
      setValidationErrors(errors);
      pushToast('error', 'Resolve validation issues before saving');
      return;
    }

    setValidationErrors([]);
    const payload = buildIpmsPayload(form);
    const result = targetId
      ? await updateIpmsTarget(targetId, payload)
      : await createIpmsTarget(payload);

    if (result.success && result.data) {
      pushToast('success', targetId ? 'IPMS target updated' : 'IPMS target created');
      setCurrentPath(`/ipms/targets/${result.data.id}`);
      return;
    }

    pushToast('error', result.message ?? `Failed to ${targetId ? 'update' : 'create'} IPMS target`);
  };

  if (isLoading || referenceData.isLoading) {
    return (
      <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full IPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  if (referenceData.error) {
    return (
      <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full IPMS target workspace">
        <Card><p className="text-sm text-error-600">{referenceData.error}</p></Card>
      </AppShell>
    );
  }

  const { departments, units, employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups } = referenceData;
  const employeeIdentityOptions = employees.filter(item => item.identityUserId).map(item => ({ value: item.identityUserId!, label: `${item.firstName} ${item.lastName}` }));
  const selectedDepartment = departments.find(item => String(item.id) === form.departmentId);
  const selectedPeriod = lookups.periods.find(item => String(item.id) === form.periodId);
  const selectedUnit = units.find(item => String(item.id) === form.unitId);
  const fieldError = (label: string) => getFieldValidationError(validationErrors, label);

  return (
    <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full-page IPMS target capture workspace">
      <div className="space-y-6">
        <div className="flex items-center justify-between gap-3">
          <Button variant="ghost" icon={<ArrowLeft className="h-4 w-4" />} onClick={() => setCurrentPath(targetId ? `/ipms/targets/${targetId}` : '/ipms/targets')}>
            Back
          </Button>
          <div className="flex gap-2">
            <Badge variant="info">{targetId ? 'Edit Mode' : 'New Record'}</Badge>
            <Badge variant="default">IPMS</Badge>
            {form.sourceTemplateId && <Badge variant="primary">Template Based</Badge>}
          </div>
        </div>

        <FormHero
          eyebrow="IPMS Target Workspace"
          title={targetId ? 'Update IPMS target using the full page form' : 'Create IPMS target using the full page form'}
          description="This page replaces the old IPMS modal flow and exposes the full target setup in one workspace."
          badges={<Badge variant="default">{targetId ? existingTarget?.indicatorNumber ?? 'Edit' : 'Create'}</Badge>}
        />

        {validationErrors.length > 0 && (
          <Card className="border border-error-200 bg-error-50 dark:border-error-800 dark:bg-error-900/20">
            <h3 className="text-sm font-semibold text-error-700 dark:text-error-200">Validation Summary</h3>
            <ul className="mt-2 space-y-1 text-xs text-error-700 dark:text-error-200">
              {validationErrors.map(error => (
                <li key={error}>- {error}</li>
              ))}
            </ul>
          </Card>
        )}

        <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
          <FormPanel title="Alignment And Ownership" description="Select the planning period, related OPMS target, employee, supervisor, and department." icon={<CalendarRange className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Source Template Id" value={form.sourceTemplateId} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateId: event.target.value }))} />
              <Input label="Template Version" value={form.sourceTemplateVersion} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateVersion: event.target.value }))} />
            </FormRow>
            <FormRow cols={3}>
              <TargetPicker kind="opms" label="Related OPMS Target" emptyLabel="No link" value={form.relatedOPMSTargetId} onChange={(value, option) => { setForm(prev => ({ ...prev, relatedOPMSTargetId: value })); setLinkedOpmsLabel(option ? `${option.indicatorNumber} - ${option.targetName}` : ''); }} />
              <div className="flex items-end">
                <Button variant="outline" className="w-full" disabled={!form.relatedOPMSTargetId} onClick={() => setForm(prev => ({ ...prev, relatedOPMSTargetId: '' }))}>
                  Unlink
                </Button>
              </div>
              <Input label="Linked OPMS" value={linkedOpmsLabel || (form.relatedOPMSTargetId ? 'Selected above' : 'Not linked')} readOnly />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Period" required error={fieldError('Period')} value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={lookups.periods.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Department" required error={fieldError('Department')} value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={departments.map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...units.filter(item => !form.departmentId || String(item.departmentId) === form.departmentId).map(item => ({ value: String(item.id), label: item.name }))]} />
              <Input label="Search employees" value={employeeSearch} onChange={(event) => setEmployeeSearch(event.target.value)} />
            </FormRow>
            <FormRow cols={2}><Select label="Employee" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...(form.assignedToId && !employeeIdentityOptions.some(item => item.value === form.assignedToId) ? [{ value: form.assignedToId, label: 'Current employee' }] : []), ...employeeIdentityOptions]} /><Select label="Supervisor" value={form.supervisorId} onChange={(event) => setForm(prev => ({ ...prev, supervisorId: event.target.value }))} options={[{ value: '', label: 'Select Supervisor' }, ...(form.supervisorId && !employeeIdentityOptions.some(item => item.value === form.supervisorId) ? [{ value: form.supervisorId, label: 'Current supervisor' }] : []), ...employeeIdentityOptions]} /></FormRow>
            {employeeTotalPages > 1 && <div className="flex items-center gap-2 text-xs text-secondary-500"><Button size="sm" variant="outline" disabled={employeePage <= 1} onClick={() => setEmployeePage(value => Math.max(1, value - 1))}>Previous employees</Button><span>Page {employeePage} of {employeeTotalPages}</span><Button size="sm" variant="outline" disabled={employeePage >= employeeTotalPages} onClick={() => setEmployeePage(value => value + 1)}>Next employees</Button></div>}
          </FormPanel>

          <FormPanel title="Target Definition" description="Define strategic alignment and the employee-level performance target." icon={<UserSquare2 className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Indicator Number" required error={fieldError('Indicator Number')} value={form.indicatorNumber} onChange={(event) => setForm(prev => ({ ...prev, indicatorNumber: event.target.value }))} />
              <Input label="Target Name" required error={fieldError('Target Name')} value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="National KPA" required value={form.nationalKPA} onChange={(event) => setForm(prev => ({ ...prev, nationalKPA: event.target.value }))} />
              <Input label="Municipal KPA" required value={form.municipalKPA} onChange={(event) => setForm(prev => ({ ...prev, municipalKPA: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value, strategicObjectiveId: '' }))} options={lookups.strategicGoals.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={lookups.strategicObjectives.filter(item => !form.strategicGoalId || String(item.strategicGoalId) === form.strategicGoalId).map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Performance Measures" description="Set numeric measures, unit configuration, and classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type={targetValueInputType(form.annualUnitType)} value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={lookups.unitsOfMeasure.map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
            <FormRow cols={4}>
              <Select
                label="Target Unit Type"
                required
                value={form.targetUnitType}
                onChange={(event) => {
                  const unitType = event.target.value as TargetUnitType;
                  setForm(prev => ({
                    ...prev,
                    targetUnitType: unitType,
                    q1UnitType: unitType,
                    q2UnitType: unitType,
                    midTermUnitType: unitType,
                    q3UnitType: unitType,
                    q4UnitType: unitType,
                    annualUnitType: unitType,
                  }));
                }}
                options={targetUnitTypeOptions}
              />
              <Input label="KPI Type" value={form.kpiType} onChange={(event) => setForm(prev => ({ ...prev, kpiType: event.target.value }))} />
              <Input label="Indicator Type" value={form.indicatorType} onChange={(event) => setForm(prev => ({ ...prev, indicatorType: event.target.value }))} />
              <Input label="Functional Area" value={form.functionalArea} onChange={(event) => setForm(prev => ({ ...prev, functionalArea: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Budget Source" value={form.budgetSourceId} onChange={(event) => setForm(prev => ({ ...prev, budgetSourceId: event.target.value }))} options={lookups.budgetSources.map(item => ({ value: String(item.id), label: item.name }))} />
              <Select label="Budget Type" value={form.budgetTypeId} onChange={(event) => setForm(prev => ({ ...prev, budgetTypeId: event.target.value }))} options={lookups.budgetTypes.map(item => ({ value: String(item.id), label: item.name }))} />
            </FormRow>
          </FormPanel>

          <FormPanel title="References And Review" description="Maintain linkage and quick review context on the same page." icon={<Building2 className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="IDP Reference" value={form.idpReference} onChange={(event) => setForm(prev => ({ ...prev, idpReference: event.target.value }))} />
              <Input label="Internal Reference" value={form.internalReference} onChange={(event) => setForm(prev => ({ ...prev, internalReference: event.target.value }))} />
            </FormRow>
            <Checkbox label="Target Revised" checked={form.isRevised} onChange={(event) => setForm(prev => ({ ...prev, isRevised: event.target.checked }))} />
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Department</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedDepartment?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Period</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedPeriod?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Unit</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUnit?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Related OPMS</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{linkedOpmsLabel || (form.relatedOPMSTargetId ? 'Selected above' : 'Not Linked')}</p>
              </div>
            </div>
          </FormPanel>
        </div>

        {targetId && existingTarget?.publicId && <PerformancePeriodTargetEditor kind={2} targetPublicId={existingTarget.publicId} />}
        <div className={targetId ? 'hidden' : ''} aria-hidden={targetId ? true : undefined}>
        <FormPanel title="Initial legacy quarterly values" description="Used only while creating the KPI. After creation, authoritative values are maintained by reporting period." icon={<CalendarRange className="h-5 w-5" />}>
          <FormRow cols={3}>
            <Select label="Q1 Unit" value={form.q1UnitType} onChange={(event) => setForm(prev => ({ ...prev, q1UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Q2 Unit" value={form.q2UnitType} onChange={(event) => setForm(prev => ({ ...prev, q2UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Mid-Year Unit" value={form.midTermUnitType} onChange={(event) => setForm(prev => ({ ...prev, midTermUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
          </FormRow>
          <FormRow cols={3}>
            <Select label="Q3 Unit" value={form.q3UnitType} onChange={(event) => setForm(prev => ({ ...prev, q3UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Q4 Unit" value={form.q4UnitType} onChange={(event) => setForm(prev => ({ ...prev, q4UnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
            <Select label="Annual Unit" value={form.annualUnitType} onChange={(event) => setForm(prev => ({ ...prev, annualUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
          </FormRow>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {/* Q1 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 1</h4>
                  <p className="text-xs text-secondary-500">Jul – Sep</p>
                </div>
                {form.q1Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q1Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q1UnitType)} value={form.q1Target} onChange={(e) => setForm(prev => ({ ...prev, q1Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q1Description} onChange={(e) => setForm(prev => ({ ...prev, q1Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q1UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q2 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 2</h4>
                  <p className="text-xs text-secondary-500">Oct – Dec</p>
                </div>
                {form.q2Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q2Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q2UnitType)} value={form.q2Target} onChange={(e) => setForm(prev => ({ ...prev, q2Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q2Description} onChange={(e) => setForm(prev => ({ ...prev, q2Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q2UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Mid-Year */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden ring-2 ring-blue-500`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-blue-50 dark:bg-blue-900/20">
                <div>
                  <h4 className="text-sm font-semibold text-blue-900 dark:text-blue-100">Mid-Year</h4>
                  <p className="text-xs text-blue-600 dark:text-blue-400">Jan – Jun</p>
                </div>
                {form.midTermBudget && <span className="text-xs font-semibold text-blue-700 dark:text-blue-300">BUDGET R {Number(form.midTermBudget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.midTermUnitType)} value={form.midTermTarget} onChange={(e) => setForm(prev => ({ ...prev, midTermTarget: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.midTermDescription} onChange={(e) => setForm(prev => ({ ...prev, midTermDescription: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.midTermUnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q3 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 3</h4>
                  <p className="text-xs text-secondary-500">Jan – Mar</p>
                </div>
                {form.q3Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q3Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q3UnitType)} value={form.q3Target} onChange={(e) => setForm(prev => ({ ...prev, q3Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q3Description} onChange={(e) => setForm(prev => ({ ...prev, q3Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q3UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Q4 */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Quarter 4</h4>
                  <p className="text-xs text-secondary-500">Apr – Jun</p>
                </div>
                {form.q4Budget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.q4Budget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <Input type={targetValueInputType(form.q4UnitType)} value={form.q4Target} onChange={(e) => setForm(prev => ({ ...prev, q4Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q4Description} onChange={(e) => setForm(prev => ({ ...prev, q4Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q4UnitType)}</span>
                </div>
              </div>
            </Card>

            {/* Annual */}
            <Card className={`border ${form.isRevised ? 'border-blue-500' : 'border-secondary-200 dark:border-secondary-700'} rounded-lg overflow-hidden`} padding="none">
              <div className="flex items-center justify-between px-4 py-3 border-b border-secondary-200 dark:border-secondary-700 bg-secondary-50 dark:bg-secondary-900">
                <div>
                  <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Annual</h4>
                  <p className="text-xs text-secondary-500">Full year</p>
                </div>
                {form.annualTarget && <span className="text-xs font-semibold text-secondary-600 dark:text-secondary-400">BUDGET R {Number(form.annualTarget).toLocaleString()}</span>}
              </div>
              <div className="p-4 space-y-3">
                <div>
                  <p className="text-xs font-medium text-secondary-500 uppercase tracking-wide">Target</p>
                  <p className="text-2xl font-bold">{form.annualTarget || 0}</p>
                </div>
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.annualUnitType)}</span>
                </div>
              </div>
            </Card>
          </div>

          <div className="mt-4 bg-secondary-50 dark:bg-secondary-900/50 p-3 rounded-lg flex items-center justify-between">
            <p className="text-xs text-secondary-600">Required fields marked • changes auto-save as draft</p>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setCurrentPath(targetId ? `/ipms/targets/${targetId}` : '/ipms/targets')}>Cancel</Button>
              <Button variant="outline" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>Save Draft</Button>
              <Button variant="primary" onClick={() => { void handleSave(); }}>Submit for Approval</Button>
            </div>
          </div>
        </FormPanel>
        </div>

        <FormPanel title="Audit Metadata" description="Read-only audit fields mirrored from target history." icon={<Building2 className="h-5 w-5" />}>
          <FormRow cols={2}>
            <Input label="Created On" value={form.createdOn || '-'} readOnly />
            <Input label="Created By" value={form.createdBy || '-'} readOnly />
          </FormRow>
          <FormRow cols={2}>
            <Input label="Updated On" value={form.updatedOn || '-'} readOnly />
            <Input label="Updated By" value={form.updatedBy || '-'} readOnly />
          </FormRow>
        </FormPanel>


      </div>
    </AppShell>
  );
}
