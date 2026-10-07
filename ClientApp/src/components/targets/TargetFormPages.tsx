/* eslint-disable react-refresh/only-export-components */
import { useEffect, useState } from 'react';
import { ArrowLeft, BarChart3, Building2, CalendarRange, Save, Target, UserSquare2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormHero, FormPanel, FormRow, Input, Select, Textarea } from '../common/Form';
import { TargetPicker } from '../common/TargetPicker';
import { CalendarMasterPicker } from '../common/CalendarMasterPicker';
import { OrganizationMasterPicker } from '../common/OrganizationMasterPicker';
import { StrategicClassificationPicker } from '../common/StrategicClassificationPicker';
import { useApp } from '../../context/AppContext';
import { PerformancePeriodTargetEditor } from './PerformancePeriodTargetEditor';
import {
  createIpmsTarget,
  createOpmsTarget,
  getPerformanceConfigurationCatalogue,
  getStrategicClassificationPage,
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
  PerformanceConfigurationCatalogueDto,
  StrategicCatalogueItemDto,
  SdbipLayerMasterDto,
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
  municipalityFinancialYearPublicId: string;
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
  nationalKpaPublicId: string;
  municipalKpaPublicId: string;
  backToBasicsPillarPublicId: string;
  strategicGoalPublicId: string;
  strategicInterventionPublicId: string;
  strategicObjectivePublicId: string;
  performanceObjectivePublicId: string;
  strategicGoalId: string;
  strategicObjectiveId: string;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: string;
  baselineDescription: string;
  annualTarget: string;
  annualTargetDescription: string;
  budgetTypePublicId: string;
  budgetSources: Array<{ budgetSourcePublicId: string; amount: string }>;
  budgetTypeHint: string;
  budgetSourceHint: string;
  kpiTypeHint: string;
  indicatorTypeHint: string;
  functionalAreaHint: string;
  standardClassificationHint: string;
  kpiUnitOfMeasurePublicId: string;
  unitOfMeasureHint: string;
  weight: string;
  kpiType: string;
  kpiTypePublicId: string;
  indicatorType: string;
  indicatorTypePublicId: string;
  functionalArea: string;
  functionalAreaPublicId: string;
  standardClassification: string;
  standardClassificationPublicId: string;
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
  municipalityFinancialYearPublicId: string;
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
  nationalKpaPublicId: string;
  municipalKpaPublicId: string;
  backToBasicsPillarPublicId: string;
  strategicGoalPublicId: string;
  strategicInterventionPublicId: string;
  strategicObjectivePublicId: string;
  performanceObjectivePublicId: string;
  strategicGoalId: string;
  strategicObjectiveId: string;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: string;
  annualTarget: string;
  annualTargetDescription: string;
  budgetTypePublicId: string;
  budgetSources: Array<{ budgetSourcePublicId: string; amount: string }>;
  budgetTypeHint: string;
  budgetSourceHint: string;
  kpiTypeHint: string;
  indicatorTypeHint: string;
  functionalAreaHint: string;
  kpiUnitOfMeasurePublicId: string;
  unitOfMeasureHint: string;
  weight: string;
  kpiType: string;
  kpiTypePublicId: string;
  indicatorType: string;
  indicatorTypePublicId: string;
  functionalArea: string;
  functionalAreaPublicId: string;
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
    municipalityFinancialYearPublicId: '',
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
    nationalKpaPublicId: '',
    municipalKpaPublicId: '',
    backToBasicsPillarPublicId: '',
    strategicGoalPublicId: '',
    strategicInterventionPublicId: '',
    strategicObjectivePublicId: '',
    performanceObjectivePublicId: '',
    strategicGoalId: '',
    strategicObjectiveId: '',
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    baselineDescription: '',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetTypePublicId: '',
    budgetSources: [],
    budgetTypeHint: '',
    budgetSourceHint: '',
    kpiTypeHint: '',
    indicatorTypeHint: '',
    functionalAreaHint: '',
    standardClassificationHint: '',
    kpiUnitOfMeasurePublicId: '',
    unitOfMeasureHint: '',
    weight: '0',
    kpiType: '',
    kpiTypePublicId: '',
    indicatorType: '',
    indicatorTypePublicId: '',
    functionalArea: '',
    functionalAreaPublicId: '',
    standardClassification: '',
    standardClassificationPublicId: '',
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
    municipalityFinancialYearPublicId: '',
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
    nationalKpaPublicId: '',
    municipalKpaPublicId: '',
    backToBasicsPillarPublicId: '',
    strategicGoalPublicId: '',
    strategicInterventionPublicId: '',
    strategicObjectivePublicId: '',
    performanceObjectivePublicId: '',
    strategicGoalId: '',
    strategicObjectiveId: '',
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetTypePublicId: '',
    budgetSources: [],
    budgetTypeHint: '',
    budgetSourceHint: '',
    kpiTypeHint: '',
    indicatorTypeHint: '',
    functionalAreaHint: '',
    kpiUnitOfMeasurePublicId: '',
    unitOfMeasureHint: '',
    weight: '0',
    kpiType: '',
    kpiTypePublicId: '',
    indicatorType: '',
    indicatorTypePublicId: '',
    functionalArea: '',
    functionalAreaPublicId: '',
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
    municipalityFinancialYearPublicId: target.municipalityFinancialYearPublicId ?? '',
    sdbipLayerPublicId: target.sdbipLayer?.publicId ?? '',
    sourceTemplateId: target.sourceTemplateId ?? '',
    sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
    periodId: target.period.id,
    departmentId: target.department.publicId ?? '',
    unitId: target.unit?.publicId ?? '',
    assignedToId: target.assignedTo?.id ?? '',
    wardIds: (target.wardIds ?? target.wards?.map(item => Number(item.id)) ?? []).join(','),
    additionalAssigneeIds: (target.additionalAssigneePublicIds ?? target.additionalAssignees.map(item => item.id)).join(','),
    voteNumberIds: (target.voteNumberIds ?? target.voteNumbers.map(item => Number(item.id))).join(','),
    indicatorNumber: target.indicatorNumber,
    nationalKPA: target.nationalKPA,
    municipalKPA: target.municipalKPA,
    nationalKpaPublicId: target.nationalKpaPublicId ?? '',
    municipalKpaPublicId: target.municipalKpaPublicId ?? '',
    backToBasicsPillarPublicId: target.backToBasicsPillarPublicId ?? '',
    strategicGoalPublicId: target.strategicGoalPublicId ?? '',
    strategicInterventionPublicId: target.strategicInterventionPublicId ?? '',
    strategicObjectivePublicId: target.strategicObjectivePublicId ?? '',
    performanceObjectivePublicId: target.performanceObjectivePublicId ?? '',
    strategicGoalId: target.strategicGoal.id,
    strategicObjectiveId: target.strategicObjective.id,
    performanceObjective: target.performanceObjective,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    baseline: String(target.baseline),
    baselineDescription: target.baselineDescription ?? '',
    annualTarget: annual?.targetValue ?? '',
    annualTargetDescription: annual?.description ?? '',
    budgetTypePublicId: target.budgetTypePublicId ?? '',
    budgetSources: (target.budgetSources ?? []).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount == null ? '' : String(item.amount) })),
    budgetTypeHint: '',
    budgetSourceHint: '',
    kpiTypeHint: '',
    indicatorTypeHint: '',
    functionalAreaHint: '',
    standardClassificationHint: '',
    kpiUnitOfMeasurePublicId: target.kpiUnitOfMeasurePublicId ?? target.unitOfMeasure.id,
    unitOfMeasureHint: '',
    weight: String(target.weight),
    kpiType: target.kpiType,
    kpiTypePublicId: target.kpiTypePublicId ?? '',
    indicatorType: target.indicatorType,
    indicatorTypePublicId: target.indicatorTypePublicId ?? '',
    functionalArea: target.functionalArea ?? '',
    functionalAreaPublicId: target.functionalAreaPublicId ?? '',
    standardClassification: target.standardClassification ?? '',
    standardClassificationPublicId: target.standardClassificationPublicId ?? '',
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
    municipalityFinancialYearPublicId: target.municipalityFinancialYearPublicId ?? '',
    sourceTemplateId: target.sourceTemplateId ?? '',
    sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
    relatedOPMSTargetId: target.relatedOPMSTarget?.id ?? '',
    periodId: target.period.id,
    departmentId: target.department.publicId ?? '',
    unitId: target.unit?.publicId ?? '',
    assignedToId: target.assignedTo?.id ?? '',
    supervisorId: target.supervisorPublicId ?? '',
    indicatorNumber: target.indicatorNumber,
    nationalKPA: target.nationalKPA,
    municipalKPA: target.municipalKPA,
    nationalKpaPublicId: target.nationalKpaPublicId ?? '',
    municipalKpaPublicId: target.municipalKpaPublicId ?? '',
    backToBasicsPillarPublicId: target.backToBasicsPillarPublicId ?? '',
    strategicGoalPublicId: target.strategicGoalPublicId ?? '',
    strategicInterventionPublicId: target.strategicInterventionPublicId ?? '',
    strategicObjectivePublicId: target.strategicObjectivePublicId ?? '',
    performanceObjectivePublicId: target.performanceObjectivePublicId ?? '',
    strategicGoalId: target.strategicGoal.id,
    strategicObjectiveId: target.strategicObjective.id,
    performanceObjective: target.performanceObjective,
    targetName: target.targetName,
    kpiDescription: target.kpiDescription,
    baseline: String(target.baseline),
    annualTarget: annual?.targetValue ?? '',
    annualTargetDescription: annual?.description ?? '',
    budgetTypePublicId: target.budgetTypePublicId ?? '',
    budgetSources: (target.budgetSources ?? []).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount == null ? '' : String(item.amount) })),
    budgetTypeHint: '',
    budgetSourceHint: '',
    kpiTypeHint: '',
    indicatorTypeHint: '',
    functionalAreaHint: '',
    kpiUnitOfMeasurePublicId: target.kpiUnitOfMeasurePublicId ?? target.unitOfMeasure.id,
    unitOfMeasureHint: '',
    weight: String(target.weight),
    kpiType: target.kpiType,
    kpiTypePublicId: target.kpiTypePublicId ?? '',
    indicatorType: target.indicatorType,
    indicatorTypePublicId: target.indicatorTypePublicId ?? '',
    functionalArea: target.functionalArea ?? '',
    functionalAreaPublicId: target.functionalAreaPublicId ?? '',
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
    departmentId: template.department?.publicId ?? defaults.departmentId,
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
    budgetTypeHint: template.budgetType?.code || template.budgetType?.name || '',
    budgetSourceHint: template.budgetSource?.code || template.budgetSource?.name || '',
    kpiUnitOfMeasurePublicId: '',
    unitOfMeasureHint: template.unitOfMeasure.code || template.unitOfMeasure.name,
    weight: String(template.weight),
    kpiTypeHint: template.kpiType,
    indicatorTypeHint: template.indicatorType,
    functionalAreaHint: template.functionalArea ?? '',
    standardClassificationHint: template.standardClassification ?? '',
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
    departmentId: template.department?.publicId ?? defaults.departmentId,
    indicatorNumber: template.templateCode,
    targetName: template.targetName,
    kpiDescription: template.kpiDescription,
    annualTarget: String(template.annualTarget),
    annualTargetDescription: template.annualTargetDescription,
    kpiUnitOfMeasurePublicId: '',
    unitOfMeasureHint: template.unitOfMeasure.code || template.unitOfMeasure.name,
    weight: String(template.weight),
    functionalAreaHint: template.functionalArea ?? '',
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
  configuration?: PerformanceConfigurationCatalogueDto,
): SaveTargetPeriodValuePayload | null {
  if (!targetValue?.trim()) return null;
  return canonicalPeriodTarget(periodType, targetValue, unitValue ?? 'AbsoluteCount', budgetValue ? Number(budgetValue) : null, description, configuration);
}

function targetValueInputType(value: string): 'text' | 'number' | 'date' {
  const unit = toXafUnitType(value);
  if (unit === 'Date') return 'date';
  if (['None', 'QualitativeTargets', 'Binary', 'BinaryDetermination', 'Ratios'].includes(unit)) return 'text';
  return 'number';
}

function buildCanonicalPeriodTargets(form: OpmsFormState | IpmsFormState, configuration?: PerformanceConfigurationCatalogueDto): SaveTargetPeriodValuePayload[] {
  return [
    periodTarget(1, form.q1Target, form.q1UnitType, form.q1Budget, form.q1Description, configuration),
    periodTarget(2, form.q2Target, form.q2UnitType, form.q2Budget, form.q2Description, configuration),
    periodTarget(3, form.midTermTarget, form.midTermUnitType, form.midTermBudget, form.midTermDescription, configuration),
    periodTarget(4, form.q3Target, form.q3UnitType, form.q3Budget, form.q3Description, configuration),
    periodTarget(5, form.q4Target, form.q4UnitType, form.q4Budget, form.q4Description, configuration),
    periodTarget(6, form.annualTarget, form.annualUnitType, '', form.annualTargetDescription, configuration),
  ].filter((item): item is SaveTargetPeriodValuePayload => item !== null);
}

export function buildOpmsPayload(form: OpmsFormState, configuration?: PerformanceConfigurationCatalogueDto): SaveOpmsTargetPayload {
  return {
    sdbipLayerPublicId: form.sdbipLayerPublicId || null,
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    periodId: form.periodId ? Number(form.periodId) : null,
    departmentId: null,
    departmentPublicId: form.departmentId || null,
    unitId: null,
    unitPublicId: form.unitId || null,
    assignedUserPublicId: form.assignedToId || null,
    wardIds: [...new Set(parseCsvIds(form.wardIds).map(Number).filter(Number.isSafeInteger))],
    additionalAssigneePublicIds: [...new Set(parseCsvIds(form.additionalAssigneeIds))],
    voteNumberIds: [...new Set(parseCsvIds(form.voteNumberIds).map(Number).filter(Number.isSafeInteger))],
    indicatorNumber: form.indicatorNumber,
    nationalKpa: form.nationalKPA,
    municipalKpa: form.municipalKPA,
    nationalKpaPublicId: form.nationalKpaPublicId,
    municipalKpaPublicId: form.municipalKpaPublicId,
    backToBasicsPillarPublicId: form.backToBasicsPillarPublicId || null,
    strategicGoalPublicId: form.strategicGoalPublicId || null,
    strategicInterventionPublicId: form.strategicInterventionPublicId || null,
    strategicObjectivePublicId: form.strategicObjectivePublicId || null,
    performanceObjectivePublicId: form.performanceObjectivePublicId,
    strategicGoalId: form.strategicGoalId ? Number(form.strategicGoalId) : null,
    strategicObjectiveId: form.strategicObjectiveId ? Number(form.strategicObjectiveId) : null,
    performanceObjective: form.performanceObjective,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    baseline: Number(form.baseline || 0),
    baselineDescription: form.baselineDescription || null,
    budgetTypePublicId: form.budgetTypePublicId || null,
    budgetSources: (form.budgetSources ?? []).filter(item => item.budgetSourcePublicId).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount === '' ? null : Number(item.amount) })),
    kpiUnitOfMeasurePublicId: form.kpiUnitOfMeasurePublicId,
    weight: Number(form.weight || 0),
    kpiType: form.kpiType,
    kpiTypePublicId: form.kpiTypePublicId,
    indicatorType: form.indicatorType,
    indicatorTypePublicId: form.indicatorTypePublicId,
    functionalArea: form.functionalArea || null,
    functionalAreaPublicId: form.functionalAreaPublicId || null,
    standardClassification: form.standardClassification || null,
    standardClassificationPublicId: form.standardClassificationPublicId || null,
    idpReference: form.idpReference || null,
    internalReference: form.internalReference || null,
    fmsLink: form.fmsLink || null,
    isRevised: form.isRevised,
    periodTargets: buildCanonicalPeriodTargets(form, configuration),
  };
}

function buildIpmsPayload(form: IpmsFormState, configuration?: PerformanceConfigurationCatalogueDto): SaveIpmsTargetPayload {
  return {
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    relatedOpmsTargetId: form.relatedOPMSTargetId || null,
    periodId: form.periodId ? Number(form.periodId) : null,
    departmentId: null,
    departmentPublicId: form.departmentId || null,
    unitId: null,
    unitPublicId: form.unitId || null,
    assignedUserPublicId: form.assignedToId || null,
    supervisorPublicId: form.supervisorId || null,
    indicatorNumber: form.indicatorNumber,
    nationalKpa: form.nationalKPA,
    municipalKpa: form.municipalKPA,
    nationalKpaPublicId: form.nationalKpaPublicId,
    municipalKpaPublicId: form.municipalKpaPublicId,
    backToBasicsPillarPublicId: form.backToBasicsPillarPublicId || null,
    strategicGoalPublicId: form.strategicGoalPublicId || null,
    strategicInterventionPublicId: form.strategicInterventionPublicId || null,
    strategicObjectivePublicId: form.strategicObjectivePublicId || null,
    performanceObjectivePublicId: form.performanceObjectivePublicId,
    strategicGoalId: form.strategicGoalId ? Number(form.strategicGoalId) : null,
    strategicObjectiveId: form.strategicObjectiveId ? Number(form.strategicObjectiveId) : null,
    performanceObjective: form.performanceObjective,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    baseline: Number(form.baseline || 0),
    budgetTypePublicId: form.budgetTypePublicId || null,
    budgetSources: (form.budgetSources ?? []).filter(item => item.budgetSourcePublicId).map(item => ({ budgetSourcePublicId: item.budgetSourcePublicId, amount: item.amount === '' ? null : Number(item.amount) })),
    kpiUnitOfMeasurePublicId: form.kpiUnitOfMeasurePublicId,
    weight: Number(form.weight || 0),
    kpiType: form.kpiType,
    kpiTypePublicId: form.kpiTypePublicId,
    indicatorType: form.indicatorType,
    indicatorTypePublicId: form.indicatorTypePublicId,
    functionalArea: form.functionalArea || null,
    functionalAreaPublicId: form.functionalAreaPublicId || null,
    idpReference: form.idpReference || null,
    internalReference: form.internalReference || null,
    isRevised: form.isRevised,
    periodTargets: buildCanonicalPeriodTargets(form, configuration),
  };
}

function validateOpmsForm(form: OpmsFormState) {
  const errors = validateRequiredFields([
    { label: 'SDBIP Layer', value: form.sdbipLayerPublicId },
    { label: 'Municipality Financial Year', value: form.municipalityFinancialYearPublicId },
    { label: 'Period', value: form.periodId },
    { label: 'Department', value: form.departmentId },
    { label: 'Indicator Number', value: form.indicatorNumber },
    { label: 'Target Name', value: form.targetName },
    { label: 'National KPA', value: form.nationalKpaPublicId },
    { label: 'Municipal KPA', value: form.municipalKpaPublicId },
    { label: 'Back-to-Basics Pillar', value: form.backToBasicsPillarPublicId },
    { label: 'Strategic Goal', value: form.strategicGoalPublicId },
    { label: 'Strategic Intervention', value: form.strategicInterventionPublicId },
    { label: 'Strategic Objective', value: form.strategicObjectivePublicId },
    { label: 'Performance Objective', value: form.performanceObjectivePublicId },
    { label: 'KPI Description', value: form.kpiDescription },
    { label: 'Annual Target', value: form.annualTarget },
    { label: 'Weight %', value: form.weight },
    { label: 'Unit of Measure', value: form.kpiUnitOfMeasurePublicId },
    { label: 'KPI Type', value: form.kpiTypePublicId },
    { label: 'Indicator Type', value: form.indicatorTypePublicId },
  ]);
  errors.push(...validateBudgetSources(form.budgetSources));
  return errors;
}

function validateIpmsForm(form: IpmsFormState) {
  const errors = validateRequiredFields([
    { label: 'Municipality Financial Year', value: form.municipalityFinancialYearPublicId },
    { label: 'Period', value: form.periodId },
    { label: 'Department', value: form.departmentId },
    { label: 'Indicator Number', value: form.indicatorNumber },
    { label: 'Target Name', value: form.targetName },
    { label: 'National KPA', value: form.nationalKpaPublicId },
    { label: 'Municipal KPA', value: form.municipalKpaPublicId },
    { label: 'Back-to-Basics Pillar', value: form.backToBasicsPillarPublicId },
    { label: 'Strategic Goal', value: form.strategicGoalPublicId },
    { label: 'Strategic Intervention', value: form.strategicInterventionPublicId },
    { label: 'Strategic Objective', value: form.strategicObjectivePublicId },
    { label: 'Performance Objective', value: form.performanceObjectivePublicId },
    { label: 'KPI Description', value: form.kpiDescription },
    { label: 'Annual Target', value: form.annualTarget },
    { label: 'Weight %', value: form.weight },
    { label: 'Unit of Measure', value: form.kpiUnitOfMeasurePublicId },
    { label: 'KPI Type', value: form.kpiTypePublicId },
    { label: 'Indicator Type', value: form.indicatorTypePublicId },
  ]);
  errors.push(...validateBudgetSources(form.budgetSources));
  return errors;
}

function validateBudgetSources(sources: Array<{ budgetSourcePublicId: string; amount: string }>) {
  const selected = sources.filter(item => item.budgetSourcePublicId);
  const errors: string[] = [];
  if (new Set(selected.map(item => item.budgetSourcePublicId)).size !== selected.length) errors.push('Each Budget Source may be selected only once.');
  if (selected.some(item => item.amount !== '' && (!Number.isFinite(Number(item.amount)) || Number(item.amount) < 0))) errors.push('Budget Source amounts must be non-negative numbers.');
  return errors;
}

type StrategicTemplateLookup = {
  budgetSources?: StrategicCatalogueItemDto[];
  budgetTypes?: StrategicCatalogueItemDto[];
  kpiTypes?: StrategicCatalogueItemDto[];
  indicatorTypes?: StrategicCatalogueItemDto[];
  functionalAreas?: StrategicCatalogueItemDto[];
  standardClassifications?: StrategicCatalogueItemDto[];
  kpiUnitsOfMeasure?: StrategicCatalogueItemDto[];
};

type StrategicTemplateHints = {
  budgetSourceHint?: string;
  budgetTypeHint?: string;
  kpiTypeHint?: string;
  indicatorTypeHint?: string;
  functionalAreaHint?: string;
  standardClassificationHint?: string;
  unitOfMeasureHint?: string;
};

function useStrategicTemplateLookup(kind: 'opms' | 'ipms', municipalityFinancialYearPublicId: string, hints: StrategicTemplateHints) {
  const [catalogue, setCatalogue] = useState<StrategicTemplateLookup>();
  const [catalogueError, setCatalogueError] = useState('');
  const { budgetSourceHint, budgetTypeHint, kpiTypeHint, indicatorTypeHint, functionalAreaHint, standardClassificationHint, unitOfMeasureHint } = hints;
  useEffect(() => {
    let cancelled = false;
    if (!municipalityFinancialYearPublicId) {
      setCatalogue(undefined);
      setCatalogueError('');
      return () => { cancelled = true; };
    }
    const specifications = [
      ['budgetSources', 'budget-sources', budgetSourceHint],
      ['budgetTypes', 'budget-types', budgetTypeHint],
      ['kpiTypes', 'kpi-types', kpiTypeHint],
      ['indicatorTypes', 'indicator-types', indicatorTypeHint],
      ['functionalAreas', 'functional-areas', functionalAreaHint],
      ['standardClassifications', 'standard-classifications', standardClassificationHint],
      ['kpiUnitsOfMeasure', 'kpi-units-of-measure', unitOfMeasureHint],
    ] as const;
    const active = specifications.filter(([, , hint]) => Boolean(hint?.trim()));
    if (!active.length) {
      setCatalogue({});
      setCatalogueError('');
      return () => { cancelled = true; };
    }
    void Promise.all(active.map(async ([property, classificationKind, hint]) => {
      const result = await getStrategicClassificationPage(kind, municipalityFinancialYearPublicId, classificationKind,
        { page: 1, pageSize: 25, search: hint!.trim(), sortBy: 'name', sortDirection: 'asc' });
      return { property, hint: hint!.trim(), result };
    })).then(results => {
      if (cancelled) return;
      const failed = results.find(item => !item.result.success || !item.result.data);
      if (failed) {
        setCatalogue(undefined);
        setCatalogueError(failed.result.message ?? 'Unable to resolve template classifications.');
        return;
      }
      const resolved: StrategicTemplateLookup = {};
      for (const { property, hint, result } of results) {
        const normalized = hint.toLowerCase();
        const exact = result.data!.items.find(item => item.code?.trim().toLowerCase() === normalized || item.name.trim().toLowerCase() === normalized);
        resolved[property] = exact ? [exact] : [];
      }
      setCatalogue(resolved);
      setCatalogueError('');
    });
    return () => { cancelled = true; };
  }, [budgetSourceHint, budgetTypeHint, functionalAreaHint, indicatorTypeHint, kind, kpiTypeHint, municipalityFinancialYearPublicId, standardClassificationHint, unitOfMeasureHint]);
  return { catalogue, catalogueError };
}

function usePerformanceConfiguration() {
  const [configuration, setConfiguration] = useState<PerformanceConfigurationCatalogueDto>();
  const [configurationError, setConfigurationError] = useState('');
  const [configurationLoading, setConfigurationLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    void getPerformanceConfigurationCatalogue().then(result => {
      if (cancelled) return;
      if (result.success && result.data) {
        setConfiguration(result.data);
        setConfigurationError('');
      } else {
        setConfiguration(undefined);
        setConfigurationError(result.message ?? 'Unable to load OPMS unit and performance-direction configuration.');
      }
      setConfigurationLoading(false);
    });
    return () => { cancelled = true; };
  }, []);
  return { configuration, configurationError, configurationLoading };
}

function configuredUnitOptions(configuration?: PerformanceConfigurationCatalogueDto): { value: TargetUnitType; label: string }[] {
  if (!configuration) return targetUnitTypeOptions;
  return configuration.opmsUnits.map(item => ({
    value: performanceUnitValue(item.engineUnitKind),
    label: `${item.name}${item.symbol ? ` (${item.symbol})` : ''}`,
  }));
}

function BudgetSourceEditor({ targetKind, municipalityFinancialYearPublicId, value, selectedLabels, onChange }: {
  targetKind: 'opms' | 'ipms';
  municipalityFinancialYearPublicId: string;
  value: Array<{ budgetSourcePublicId: string; amount: string }>;
  selectedLabels?: Record<string, string>;
  onChange: (value: Array<{ budgetSourcePublicId: string; amount: string }>) => void;
}) {
  return <div className="space-y-2">
    <div className="flex items-center justify-between"><p className="text-sm font-medium">Budget Sources</p><Button size="sm" variant="outline" onClick={() => onChange([...value, { budgetSourcePublicId: '', amount: '' }])}>Add source</Button></div>
    {value.map((item, index) => <div key={`${index}-${item.budgetSourcePublicId}`} className="grid gap-2 md:grid-cols-[1fr_0.55fr_auto]">
      <StrategicClassificationPicker targetKind={targetKind} classificationKind="budget-sources" municipalityFinancialYearPublicId={municipalityFinancialYearPublicId} label={`Budget Source ${index + 1}`} value={item.budgetSourcePublicId} selectedLabel={selectedLabels?.[item.budgetSourcePublicId]} onChange={next => onChange(value.map((row, rowIndex) => rowIndex === index ? { ...row, budgetSourcePublicId: next } : row))} />
      <Input label="Amount (optional)" type="number" min="0" step="0.01" value={item.amount} onChange={event => onChange(value.map((row, rowIndex) => rowIndex === index ? { ...row, amount: event.target.value } : row))} />
      <div className="self-end"><Button size="sm" variant="ghost" onClick={() => onChange(value.filter((_, rowIndex) => rowIndex !== index))}>Remove</Button></div>
    </div>)}
    {!value.length && <p className="text-xs text-secondary-500">No budget source selected.</p>}
  </div>;
}

function resolveBudgetTemplateHints<T extends { budgetTypePublicId: string; budgetSources: Array<{ budgetSourcePublicId: string; amount: string }>; budgetTypeHint: string; budgetSourceHint: string }>(form: T, catalogue: StrategicTemplateLookup): T {
  if (!form.budgetTypeHint && !form.budgetSourceHint) return form;
  const matches = (items: StrategicCatalogueItemDto[] | undefined, hint: string) => items?.find(item => item.code?.toLowerCase() === hint.toLowerCase() || item.name.toLowerCase() === hint.toLowerCase());
  const type = matches(catalogue.budgetTypes, form.budgetTypeHint);
  const source = matches(catalogue.budgetSources, form.budgetSourceHint);
  return { ...form, budgetTypePublicId: type?.publicId ?? form.budgetTypePublicId, budgetSources: source && form.budgetSources.length === 0 ? [{ budgetSourcePublicId: source.publicId, amount: '' }] : form.budgetSources, budgetTypeHint: '', budgetSourceHint: '' };
}

export function resolvePerformanceTemplateHints<T extends {
  kpiType: string; kpiTypePublicId: string; kpiTypeHint: string;
  indicatorType: string; indicatorTypePublicId: string; indicatorTypeHint: string;
  functionalArea: string; functionalAreaPublicId: string; functionalAreaHint: string;
  standardClassification?: string; standardClassificationPublicId?: string; standardClassificationHint?: string;
  kpiUnitOfMeasurePublicId: string; unitOfMeasureHint: string;
}>(form: T, catalogue: StrategicTemplateLookup): T {
  if (!form.kpiTypeHint && !form.indicatorTypeHint && !form.functionalAreaHint && !form.standardClassificationHint && !form.unitOfMeasureHint) return form;
  const match = (items: StrategicCatalogueItemDto[] | undefined, hint: string | undefined) => {
    const normalized = hint?.trim().toLowerCase();
    return normalized ? items?.find(item => item.code?.trim().toLowerCase() === normalized || item.name.trim().toLowerCase() === normalized) : undefined;
  };
  const kpiType = match(catalogue.kpiTypes, form.kpiTypeHint);
  const indicatorType = match(catalogue.indicatorTypes, form.indicatorTypeHint);
  const functionalArea = match(catalogue.functionalAreas, form.functionalAreaHint);
  const standardClassification = match(catalogue.standardClassifications, form.standardClassificationHint);
  const unitOfMeasure = match(catalogue.kpiUnitsOfMeasure, form.unitOfMeasureHint);
  return {
    ...form,
    kpiTypePublicId: kpiType?.publicId ?? form.kpiTypePublicId,
    kpiType: kpiType?.name ?? form.kpiType,
    indicatorTypePublicId: indicatorType?.publicId ?? form.indicatorTypePublicId,
    indicatorType: indicatorType?.name ?? form.indicatorType,
    functionalAreaPublicId: functionalArea?.publicId ?? form.functionalAreaPublicId,
    functionalArea: functionalArea?.name ?? form.functionalArea,
    kpiUnitOfMeasurePublicId: unitOfMeasure?.publicId ?? form.kpiUnitOfMeasurePublicId,
    ...(form.standardClassificationPublicId !== undefined ? {
      standardClassificationPublicId: standardClassification?.publicId ?? form.standardClassificationPublicId,
      standardClassification: standardClassification?.name ?? form.standardClassification,
      standardClassificationHint: '',
    } : {}),
    kpiTypeHint: '', indicatorTypeHint: '', functionalAreaHint: '', unitOfMeasureHint: '',
  };
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
  const { catalogue, catalogueError } = useStrategicTemplateLookup('opms', form.municipalityFinancialYearPublicId, form);
  const { configuration, configurationError, configurationLoading } = usePerformanceConfiguration();
  const performanceUnitOptions = configuredUnitOptions(configuration);
  useEffect(() => {
    if (catalogue) setForm(current => resolvePerformanceTemplateHints(resolveBudgetTemplateHints(current, catalogue), catalogue));
  }, [catalogue, form.budgetSourceHint, form.budgetTypeHint, form.functionalAreaHint, form.indicatorTypeHint, form.kpiTypeHint, form.standardClassificationHint, form.unitOfMeasureHint]);

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
    if (!configuration) {
      pushToast('error', configurationError || 'OPMS unit configuration is not available.');
      return;
    }
    const errors = validateOpmsForm(form);
    if (errors.length > 0) {
      setValidationErrors(errors);
      pushToast('error', 'Resolve validation issues before saving');
      return;
    }

    setValidationErrors([]);
    const payload = buildOpmsPayload(form, configuration);
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

  if (isLoading || referenceData.isLoading || configurationLoading) {
    return (
      <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full OPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  if (referenceData.error || configurationError) {
    return (
      <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full OPMS target workspace">
        <Card><p className="text-sm text-error-600">{referenceData.error || configurationError}</p></Card>
      </AppShell>
    );
  }

  const { employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups } = referenceData;
  const employeeIdentityOptions = employees.filter(item => item.identityUserPublicId).map(item => ({ value: item.identityUserPublicId!, label: `${item.firstName} ${item.lastName}` }));
  const selectedPeriod = lookups.periods.find(item => String(item.id) === form.periodId);
  const selectedDepartmentName = referenceLabels[`department:${form.departmentId}`] ?? (existingTarget?.department.publicId === form.departmentId ? existingTarget.department.name : undefined);
  const selectedUnitName = referenceLabels[`unit:${form.unitId}`] ?? (existingTarget?.unit?.publicId === form.unitId ? existingTarget.unit.name : undefined);
  const selectedUomName = referenceLabels[`uom:${form.kpiUnitOfMeasurePublicId}`] ?? existingTarget?.unitOfMeasure.name;
  const budgetSourceLabels = Object.fromEntries((existingTarget?.budgetSources ?? []).map(item => [item.budgetSourcePublicId, `${item.code} · ${item.name}`]));
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
              <CalendarMasterPicker kind="municipality-financial-year" label="Municipality Financial Year" required value={form.municipalityFinancialYearPublicId} onChange={value => { setSelectedVoteNumber(undefined); setForm(prev => ({ ...prev, municipalityFinancialYearPublicId: value, sdbipLayerPublicId: '', voteNumberIds: '', nationalKpaPublicId: '', municipalKpaPublicId: '', backToBasicsPillarPublicId: '', strategicGoalPublicId: '', strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '', budgetTypePublicId: '', budgetSources: [], kpiTypePublicId: '', kpiType: '', indicatorTypePublicId: '', indicatorType: '', functionalAreaPublicId: '', functionalArea: '', standardClassificationPublicId: '', standardClassification: '', kpiUnitOfMeasurePublicId: '' })); }} />
              <CalendarMasterPicker kind="sdbip-layer" label="SDBIP Layer" required value={form.sdbipLayerPublicId} municipalityFinancialYearId={form.municipalityFinancialYearPublicId || undefined} selectedLabel={existingTarget?.sdbipLayer ? `${existingTarget.sdbipLayer.code} · ${existingTarget.sdbipLayer.name}` : undefined} onChange={(value, option) => setForm(prev => ({ ...prev, sdbipLayerPublicId: value, municipalityFinancialYearPublicId: (option as SdbipLayerMasterDto | undefined)?.municipalityFinancialYearPublicId ?? prev.municipalityFinancialYearPublicId }))} />
            </FormRow>
            <FormRow cols={2}>
              <OrganizationMasterPicker kind="department" label="Department" required value={form.departmentId} selectedLabel={selectedDepartmentName} emptyLabel="Select Department" onChange={(value, option) => { setForm(prev => ({ ...prev, departmentId: value, unitId: '' })); if (value && option) setReferenceLabels(current => ({ ...current, [`department:${value}`]: option.name })); }} />
              <OrganizationMasterPicker kind="unit" label="Unit" value={form.unitId} selectedLabel={selectedUnitName} departmentPublicId={form.departmentId || undefined} emptyLabel="No Unit" onChange={(value, option) => { setForm(prev => ({ ...prev, unitId: value })); if (value && option) setReferenceLabels(current => ({ ...current, [`unit:${value}`]: option.name })); }} />
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
                const assignee = employees.find(item => item.identityUserPublicId === assigneeId);
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
              <OrganizationMasterPicker kind="vote-number" label="Vote Numbers" value={selectedVoteNumber?.publicId ?? ''} municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId || undefined} emptyLabel="Select Vote Number" onChange={(_, option) => setSelectedVoteNumber(option as VoteNumberMasterDto | undefined)} />
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
            {catalogueError && <p className="text-sm text-error-600">{catalogueError}</p>}
            <FormRow cols={3}>
              <StrategicClassificationPicker targetKind="opms" classificationKind="national-kpas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="National KPA" required error={fieldError('National KPA')} value={form.nationalKpaPublicId} selectedLabel={form.nationalKPA} onChange={(value, item) => setForm(prev => ({ ...prev, nationalKpaPublicId: value, nationalKPA: item?.name ?? prev.nationalKPA }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="municipal-kpas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Municipal KPA" required error={fieldError('Municipal KPA')} value={form.municipalKpaPublicId} selectedLabel={form.municipalKPA} onChange={(value, item) => setForm(prev => ({ ...prev, municipalKpaPublicId: value, municipalKPA: item?.name ?? prev.municipalKPA, strategicGoalPublicId: '', strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="back-to-basics-pillars" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Back-to-Basics Pillar" value={form.backToBasicsPillarPublicId} selectedLabel={existingTarget?.backToBasicsPillar} onChange={value => setForm(prev => ({ ...prev, backToBasicsPillarPublicId: value }))} />
            </FormRow>
            <FormRow cols={2}>
              <StrategicClassificationPicker targetKind="opms" classificationKind="strategic-goals" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Goal" value={form.strategicGoalPublicId} selectedLabel={existingTarget?.strategicGoal.name} parentPublicId={form.municipalKpaPublicId} relationshipType="municipal-kpa-strategic-goal" onChange={value => setForm(prev => ({ ...prev, strategicGoalPublicId: value, strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="strategic-interventions" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Intervention" value={form.strategicInterventionPublicId} selectedLabel={existingTarget?.strategicIntervention} parentPublicId={form.strategicGoalPublicId} relationshipType="strategic-goal-intervention" onChange={value => setForm(prev => ({ ...prev, strategicInterventionPublicId: value, strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
            </FormRow>
            <FormRow cols={2}>
              <StrategicClassificationPicker targetKind="opms" classificationKind="strategic-objectives" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Objective" value={form.strategicObjectivePublicId} selectedLabel={existingTarget?.strategicObjective.name} parentPublicId={form.strategicInterventionPublicId || form.strategicGoalPublicId} relationshipType={form.strategicInterventionPublicId ? 'strategic-intervention-objective' : 'strategic-goal-objective'} onChange={value => setForm(prev => ({ ...prev, strategicObjectivePublicId: value, performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="performance-objectives" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjectivePublicId} selectedLabel={form.performanceObjective} parentPublicId={form.strategicObjectivePublicId} relationshipType="strategic-objective-performance-objective" onChange={(value, item) => setForm(prev => ({ ...prev, performanceObjectivePublicId: value, performanceObjective: item?.name ?? prev.performanceObjective }))} />
            </FormRow>
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Measurement Details" description="Define numeric measures, unit configuration, and performance classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" required type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type={targetValueInputType(form.annualUnitType)} value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="kpi-units-of-measure" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.kpiUnitOfMeasurePublicId} selectedLabel={selectedUomName} onChange={(value, item) => { if (item) setReferenceLabels(current => ({ ...current, [`uom:${value}`]: item.name })); setForm(prev => ({ ...prev, kpiUnitOfMeasurePublicId: value })); }} />
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
                options={performanceUnitOptions}
              />
              <StrategicClassificationPicker targetKind="opms" classificationKind="kpi-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="KPI Type" required value={form.kpiTypePublicId} selectedLabel={form.kpiType} onChange={(value, item) => setForm(prev => ({ ...prev, kpiTypePublicId: value, kpiType: item?.name ?? '' }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="indicator-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Indicator Type" required value={form.indicatorTypePublicId} selectedLabel={form.indicatorType} onChange={(value, item) => setForm(prev => ({ ...prev, indicatorTypePublicId: value, indicatorType: item?.name ?? '' }))} />
              <StrategicClassificationPicker targetKind="opms" classificationKind="functional-areas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Functional Area" value={form.functionalAreaPublicId} selectedLabel={form.functionalArea} onChange={(value, item) => setForm(prev => ({ ...prev, functionalAreaPublicId: value, functionalArea: item?.name ?? '' }))} />
            </FormRow>
            <StrategicClassificationPicker targetKind="opms" classificationKind="standard-classifications" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Standard Classification" value={form.standardClassificationPublicId} selectedLabel={form.standardClassification} onChange={(value, item) => setForm(prev => ({ ...prev, standardClassificationPublicId: value, standardClassification: item?.name ?? '' }))} />
          </FormPanel>

          <FormPanel title="Budget And References" description="Capture budget linkage, identifiers, and external references." icon={<Building2 className="h-5 w-5" />}>
            <StrategicClassificationPicker targetKind="opms" classificationKind="budget-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Budget Type" value={form.budgetTypePublicId} selectedLabel={existingTarget?.budgetTypeName} onChange={value => setForm(prev => ({ ...prev, budgetTypePublicId: value }))} />
            <BudgetSourceEditor targetKind="opms" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} value={form.budgetSources} selectedLabels={budgetSourceLabels} onChange={budgetSources => setForm(prev => ({ ...prev, budgetSources }))} />
            <FormRow cols={2}>
              <Input label="IDP Reference" value={form.idpReference} onChange={(event) => setForm(prev => ({ ...prev, idpReference: event.target.value }))} />
              <Input label="Internal Reference" value={form.internalReference} onChange={(event) => setForm(prev => ({ ...prev, internalReference: event.target.value }))} />
            </FormRow>
            <Input label="FMS Link" value={form.fmsLink} onChange={(event) => setForm(prev => ({ ...prev, fmsLink: event.target.value }))} />
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Department</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedDepartmentName ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Period</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedPeriod?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Unit</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUnitName ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Measure</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUomName ?? '-'}</p>
              </div>
            </div>
          </FormPanel>
        </div>

        {targetId && existingTarget?.publicId && <PerformancePeriodTargetEditor kind={1} targetPublicId={existingTarget.publicId} />}
        <div className={targetId ? 'hidden' : ''} aria-hidden={targetId ? true : undefined}>
        <FormPanel title="Initial legacy quarterly values" description="Used only while creating the KPI. After creation, authoritative values are maintained by reporting period." icon={<CalendarRange className="h-5 w-5" />}>
          <FormRow cols={3}>
            <Select label="Q1 Unit" value={form.q1UnitType} onChange={(event) => setForm(prev => ({ ...prev, q1UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Q2 Unit" value={form.q2UnitType} onChange={(event) => setForm(prev => ({ ...prev, q2UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Mid-Year Unit" value={form.midTermUnitType} onChange={(event) => setForm(prev => ({ ...prev, midTermUnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
          </FormRow>
          <FormRow cols={3}>
            <Select label="Q3 Unit" value={form.q3UnitType} onChange={(event) => setForm(prev => ({ ...prev, q3UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Q4 Unit" value={form.q4UnitType} onChange={(event) => setForm(prev => ({ ...prev, q4UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Annual Unit" value={form.annualUnitType} onChange={(event) => setForm(prev => ({ ...prev, annualUnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
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
  const { catalogue, catalogueError } = useStrategicTemplateLookup('ipms', form.municipalityFinancialYearPublicId, form);
  const { configuration, configurationError, configurationLoading } = usePerformanceConfiguration();
  const performanceUnitOptions = configuredUnitOptions(configuration);
  useEffect(() => {
    if (catalogue) setForm(current => resolvePerformanceTemplateHints(resolveBudgetTemplateHints(current, catalogue), catalogue));
  }, [catalogue, form.budgetSourceHint, form.budgetTypeHint, form.functionalAreaHint, form.indicatorTypeHint, form.kpiTypeHint, form.unitOfMeasureHint]);
  const [existingTarget, setExistingTarget] = useState<IPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [linkedOpmsLabel, setLinkedOpmsLabel] = useState('');
  const [organizationLabels, setOrganizationLabels] = useState<Record<string, string>>({});

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
    if (!configuration) {
      pushToast('error', configurationError || 'OPMS unit configuration is not available.');
      return;
    }
    const errors = validateIpmsForm(form);
    if (errors.length > 0) {
      setValidationErrors(errors);
      pushToast('error', 'Resolve validation issues before saving');
      return;
    }

    setValidationErrors([]);
    const payload = buildIpmsPayload(form, configuration);
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

  if (isLoading || referenceData.isLoading || configurationLoading) {
    return (
      <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full IPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  if (referenceData.error || configurationError) {
    return (
      <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full IPMS target workspace">
        <Card><p className="text-sm text-error-600">{referenceData.error || configurationError}</p></Card>
      </AppShell>
    );
  }

  const { employees, employeePage, employeeTotalPages, employeeSearch, setEmployeePage, setEmployeeSearch, lookups } = referenceData;
  const employeeIdentityOptions = employees.filter(item => item.identityUserPublicId).map(item => ({ value: item.identityUserPublicId!, label: `${item.firstName} ${item.lastName}` }));
  const selectedPeriod = lookups.periods.find(item => String(item.id) === form.periodId);
  const selectedDepartmentName = organizationLabels[`department:${form.departmentId}`] ?? (existingTarget?.department.publicId === form.departmentId ? existingTarget.department.name : undefined);
  const selectedUnitName = organizationLabels[`unit:${form.unitId}`] ?? (existingTarget?.unit?.publicId === form.unitId ? existingTarget.unit.name : undefined);
  const selectedUomName = organizationLabels[`uom:${form.kpiUnitOfMeasurePublicId}`] ?? existingTarget?.unitOfMeasure.name;
  const budgetSourceLabels = Object.fromEntries((existingTarget?.budgetSources ?? []).map(item => [item.budgetSourcePublicId, `${item.code} · ${item.name}`]));
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
            <FormRow cols={3}>
              <CalendarMasterPicker kind="municipality-financial-year" label="Municipality Financial Year" required value={form.municipalityFinancialYearPublicId} onChange={value => setForm(prev => ({ ...prev, municipalityFinancialYearPublicId: value, nationalKpaPublicId: '', municipalKpaPublicId: '', backToBasicsPillarPublicId: '', strategicGoalPublicId: '', strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '', budgetTypePublicId: '', budgetSources: [], kpiTypePublicId: '', kpiType: '', indicatorTypePublicId: '', indicatorType: '', functionalAreaPublicId: '', functionalArea: '', kpiUnitOfMeasurePublicId: '' }))} />
              <Select label="Period" required error={fieldError('Period')} value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={lookups.periods.map(item => ({ value: String(item.id), label: item.name }))} />
              <OrganizationMasterPicker kind="department" label="Department" required value={form.departmentId} selectedLabel={selectedDepartmentName} emptyLabel="Select Department" onChange={(value, option) => { setForm(prev => ({ ...prev, departmentId: value, unitId: '' })); if (value && option) setOrganizationLabels(current => ({ ...current, [`department:${value}`]: option.name })); }} />
            </FormRow>
            <FormRow cols={2}>
              <OrganizationMasterPicker kind="unit" label="Unit" value={form.unitId} selectedLabel={selectedUnitName} departmentPublicId={form.departmentId || undefined} emptyLabel="No Unit" onChange={(value, option) => { setForm(prev => ({ ...prev, unitId: value })); if (value && option) setOrganizationLabels(current => ({ ...current, [`unit:${value}`]: option.name })); }} />
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
            {catalogueError && <p className="text-sm text-error-600">{catalogueError}</p>}
            <FormRow cols={3}>
              <StrategicClassificationPicker targetKind="ipms" classificationKind="national-kpas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="National KPA" required error={fieldError('National KPA')} value={form.nationalKpaPublicId} selectedLabel={form.nationalKPA} onChange={(value, item) => setForm(prev => ({ ...prev, nationalKpaPublicId: value, nationalKPA: item?.name ?? prev.nationalKPA }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="municipal-kpas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Municipal KPA" required error={fieldError('Municipal KPA')} value={form.municipalKpaPublicId} selectedLabel={form.municipalKPA} onChange={(value, item) => setForm(prev => ({ ...prev, municipalKpaPublicId: value, municipalKPA: item?.name ?? prev.municipalKPA, strategicGoalPublicId: '', strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="back-to-basics-pillars" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Back-to-Basics Pillar" value={form.backToBasicsPillarPublicId} selectedLabel={existingTarget?.backToBasicsPillar} onChange={value => setForm(prev => ({ ...prev, backToBasicsPillarPublicId: value }))} />
            </FormRow>
            <FormRow cols={2}>
              <StrategicClassificationPicker targetKind="ipms" classificationKind="strategic-goals" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Goal" value={form.strategicGoalPublicId} selectedLabel={existingTarget?.strategicGoal.name} parentPublicId={form.municipalKpaPublicId} relationshipType="municipal-kpa-strategic-goal" onChange={value => setForm(prev => ({ ...prev, strategicGoalPublicId: value, strategicInterventionPublicId: '', strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="strategic-interventions" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Intervention" value={form.strategicInterventionPublicId} selectedLabel={existingTarget?.strategicIntervention} parentPublicId={form.strategicGoalPublicId} relationshipType="strategic-goal-intervention" onChange={value => setForm(prev => ({ ...prev, strategicInterventionPublicId: value, strategicObjectivePublicId: '', performanceObjectivePublicId: '' }))} />
            </FormRow>
            <FormRow cols={2}>
              <StrategicClassificationPicker targetKind="ipms" classificationKind="strategic-objectives" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Strategic Objective" value={form.strategicObjectivePublicId} selectedLabel={existingTarget?.strategicObjective.name} parentPublicId={form.strategicInterventionPublicId || form.strategicGoalPublicId} relationshipType={form.strategicInterventionPublicId ? 'strategic-intervention-objective' : 'strategic-goal-objective'} onChange={value => setForm(prev => ({ ...prev, strategicObjectivePublicId: value, performanceObjectivePublicId: '' }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="performance-objectives" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjectivePublicId} selectedLabel={form.performanceObjective} parentPublicId={form.strategicObjectivePublicId} relationshipType="strategic-objective-performance-objective" onChange={(value, item) => setForm(prev => ({ ...prev, performanceObjectivePublicId: value, performanceObjective: item?.name ?? prev.performanceObjective }))} />
            </FormRow>
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Performance Measures" description="Set numeric measures, unit configuration, and classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type={targetValueInputType(form.annualUnitType)} value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="kpi-units-of-measure" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.kpiUnitOfMeasurePublicId} selectedLabel={selectedUomName} onChange={(value, item) => { if (item) setOrganizationLabels(current => ({ ...current, [`uom:${value}`]: item.name })); setForm(prev => ({ ...prev, kpiUnitOfMeasurePublicId: value })); }} />
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
                options={performanceUnitOptions}
              />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="kpi-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="KPI Type" required value={form.kpiTypePublicId} selectedLabel={form.kpiType} onChange={(value, item) => setForm(prev => ({ ...prev, kpiTypePublicId: value, kpiType: item?.name ?? '' }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="indicator-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Indicator Type" required value={form.indicatorTypePublicId} selectedLabel={form.indicatorType} onChange={(value, item) => setForm(prev => ({ ...prev, indicatorTypePublicId: value, indicatorType: item?.name ?? '' }))} />
              <StrategicClassificationPicker targetKind="ipms" classificationKind="functional-areas" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Functional Area" value={form.functionalAreaPublicId} selectedLabel={form.functionalArea} onChange={(value, item) => setForm(prev => ({ ...prev, functionalAreaPublicId: value, functionalArea: item?.name ?? '' }))} />
            </FormRow>
            <FormRow cols={2}>
              <StrategicClassificationPicker targetKind="ipms" classificationKind="budget-types" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} label="Budget Type" value={form.budgetTypePublicId} selectedLabel={existingTarget?.budgetTypeName} onChange={value => setForm(prev => ({ ...prev, budgetTypePublicId: value }))} />
            </FormRow>
            <BudgetSourceEditor targetKind="ipms" municipalityFinancialYearPublicId={form.municipalityFinancialYearPublicId} value={form.budgetSources} selectedLabels={budgetSourceLabels} onChange={budgetSources => setForm(prev => ({ ...prev, budgetSources }))} />
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
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedDepartmentName ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Period</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedPeriod?.name ?? '-'}</p>
              </div>
              <div className="rounded-xl border border-secondary-200 px-3 py-3 dark:border-secondary-700">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-secondary-500">Unit</p>
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{selectedUnitName ?? '-'}</p>
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
            <Select label="Q1 Unit" value={form.q1UnitType} onChange={(event) => setForm(prev => ({ ...prev, q1UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Q2 Unit" value={form.q2UnitType} onChange={(event) => setForm(prev => ({ ...prev, q2UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Mid-Year Unit" value={form.midTermUnitType} onChange={(event) => setForm(prev => ({ ...prev, midTermUnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
          </FormRow>
          <FormRow cols={3}>
            <Select label="Q3 Unit" value={form.q3UnitType} onChange={(event) => setForm(prev => ({ ...prev, q3UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Q4 Unit" value={form.q4UnitType} onChange={(event) => setForm(prev => ({ ...prev, q4UnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
            <Select label="Annual Unit" value={form.annualUnitType} onChange={(event) => setForm(prev => ({ ...prev, annualUnitType: event.target.value as TargetUnitType }))} options={performanceUnitOptions} />
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
