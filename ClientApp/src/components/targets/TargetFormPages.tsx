import { useEffect, useState } from 'react';
import { ArrowLeft, BarChart3, Building2, CalendarRange, Save, Target, UserSquare2 } from 'lucide-react';
import { AppShell } from '../layout/AppShell';
import { Badge, Button, Card } from '../ui';
import { Checkbox, FormHero, FormPanel, FormRow, Input, Select, Textarea } from '../common/Form';
import { useApp } from '../../context/AppContext';
import {
  createIpmsTarget,
  createOpmsTarget,
  getIpmsTarget,
  getIpmsTargetTemplate,
  getOpmsTarget,
  getOpmsTargetTemplate,
  updateIpmsTarget,
  updateOpmsTarget,
} from '../../api/api';
import {
  mockBudgetSources,
  mockBudgetTypes,
  mockDepartmentUnits,
  mockDepartments,
  mockEmployees,
  mockIPMSTargets,
  mockOPMSTargets,
  mockPeriods,
  mockStrategicGoals,
  mockStrategicObjectives,
  mockUnitsOfMeasure,
  mockVoteNumbers,
  mockWards,
} from '../../data/mockData';
import type {
  IPMSTarget,
  IpmsTargetTemplate,
  OPMSTarget,
  OpmsTargetTemplate,
  SaveIpmsTargetPayload,
  SaveOpmsTargetPayload,
  TargetUnitType,
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

function toXafUnitType(value: TargetUnitType): TargetUnitType {
  return legacyToXafUnitMap[value] ?? value;
}

function toApiUnitType(value: TargetUnitType): TargetUnitType {
  return xafToLegacyUnitMap[value as XafUnitValue] ?? value;
}

function getTargetUnitLabel(value: TargetUnitType) {
  return targetUnitTypeOptions.find(item => item.value === value)?.label ?? 'Target Value';
}

function validateRequiredFields(values: Array<{ label: string; value: string | undefined }>) {
  return values
    .filter(item => !item.value || !item.value.trim())
    .map(item => `${item.label} is required.`);
}

function getFieldValidationError(errors: string[], label: string) {
  const normalizedLabel = label.toLowerCase();
  return errors.find(error => error.toLowerCase().startsWith(normalizedLabel));
}

type OpmsFormState = {
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
  const baseTarget = mockOPMSTargets[0];
  return {
    sourceTemplateId: '',
    sourceTemplateVersion: '',
    periodId: mockPeriods[0]?.id ?? baseTarget.period.id,
    departmentId: mockDepartments[0]?.id ?? baseTarget.department.id,
    unitId: '',
    assignedToId: '',
    wardIds: '',
    additionalAssigneeIds: '',
    voteNumberIds: '',
    indicatorNumber: '',
    nationalKPA: baseTarget.nationalKPA,
    municipalKPA: baseTarget.municipalKPA,
    strategicGoalId: baseTarget.strategicGoal.id,
    strategicObjectiveId: baseTarget.strategicObjective.id,
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    baselineDescription: '',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetSourceId: baseTarget.budgetSource.id,
    budgetTypeId: baseTarget.budgetType.id,
    unitOfMeasureId: baseTarget.unitOfMeasure.id,
    weight: '0',
    kpiType: baseTarget.kpiType,
    indicatorType: baseTarget.indicatorType,
    functionalArea: baseTarget.functionalArea ?? '',
    standardClassification: baseTarget.standardClassification ?? '',
    idpReference: baseTarget.idpReference ?? '',
    internalReference: baseTarget.internalReference ?? '',
    fmsLink: baseTarget.fmsLink ?? '',
    isRevised: false,
    isWithdrawn: false,
    reasonForWithdrawal: '',
    targetUnitType: toXafUnitType(baseTarget.targetUnitType),
    q1UnitType: toXafUnitType(baseTarget.targetUnitType),
    q2UnitType: toXafUnitType(baseTarget.targetUnitType),
    midTermUnitType: toXafUnitType(baseTarget.targetUnitType),
    q3UnitType: toXafUnitType(baseTarget.targetUnitType),
    q4UnitType: toXafUnitType(baseTarget.targetUnitType),
    annualUnitType: toXafUnitType(baseTarget.targetUnitType),
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
  const baseTarget = mockIPMSTargets[0];
  return {
    sourceTemplateId: '',
    sourceTemplateVersion: '',
    relatedOPMSTargetId: '',
    periodId: mockPeriods[0]?.id ?? baseTarget.period.id,
    departmentId: mockDepartments[0]?.id ?? baseTarget.department.id,
    unitId: '',
    assignedToId: '',
    supervisorId: '',
    indicatorNumber: '',
    nationalKPA: baseTarget.nationalKPA,
    municipalKPA: baseTarget.municipalKPA,
    strategicGoalId: baseTarget.strategicGoal.id,
    strategicObjectiveId: baseTarget.strategicObjective.id,
    performanceObjective: '',
    targetName: '',
    kpiDescription: '',
    baseline: '0',
    annualTarget: '0',
    annualTargetDescription: '',
    budgetSourceId: baseTarget.budgetSource.id,
    budgetTypeId: baseTarget.budgetType.id,
    unitOfMeasureId: baseTarget.unitOfMeasure.id,
    weight: '0',
    kpiType: baseTarget.kpiType,
    indicatorType: baseTarget.indicatorType,
    functionalArea: baseTarget.functionalArea ?? '',
    idpReference: baseTarget.idpReference ?? '',
    internalReference: baseTarget.internalReference ?? '',
    isRevised: false,
    targetUnitType: toXafUnitType(baseTarget.targetUnitType),
    q1UnitType: toXafUnitType(baseTarget.targetUnitType),
    q2UnitType: toXafUnitType(baseTarget.targetUnitType),
    midTermUnitType: toXafUnitType(baseTarget.targetUnitType),
    q3UnitType: toXafUnitType(baseTarget.targetUnitType),
    q4UnitType: toXafUnitType(baseTarget.targetUnitType),
    annualUnitType: toXafUnitType(baseTarget.targetUnitType),
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
  return {
    sourceTemplateId: target.sourceTemplateId ?? '',
    sourceTemplateVersion: target.sourceTemplateVersion ? String(target.sourceTemplateVersion) : '',
    periodId: target.period.id,
    departmentId: target.department.id,
    unitId: target.unit?.id ?? '',
    assignedToId: target.assignedTo?.id ?? '',
    wardIds: target.wards?.map(item => item.id).join(',') ?? '',
    additionalAssigneeIds: target.additionalAssignees.map(item => item.id).join(','),
    voteNumberIds: target.voteNumbers.map(item => item.id).join(','),
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
    annualTarget: String(target.annualTarget),
    annualTargetDescription: target.annualTargetDescription,
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
    q1UnitType: toXafUnitType(target.TargetUnitQ1 ?? target.targetUnitType),
    q2UnitType: toXafUnitType(target.TargetUnitQ2 ?? target.targetUnitType),
    midTermUnitType: toXafUnitType(target.MidTermTargetUnit ?? target.targetUnitType),
    q3UnitType: toXafUnitType(target.TargetUnitQ3 ?? target.targetUnitType),
    q4UnitType: toXafUnitType(target.TargetUnitQ4 ?? target.targetUnitType),
    annualUnitType: toXafUnitType(target.AnnualTargetUnit ?? target.targetUnitType),
    q1Target: numberText(target.q1Target),
    q1Description: target.q1Description ?? '',
    q1Budget: numberText(target.q1Budget),
    q2Target: numberText(target.q2Target),
    q2Description: target.q2Description ?? '',
    q2Budget: numberText(target.q2Budget),
    midTermTarget: numberText(target.midTermTarget),
    midTermDescription: target.midTermDescription ?? '',
    midTermBudget: numberText(target.midTermBudget),
    q3Target: numberText(target.q3Target),
    q3Description: target.q3Description ?? '',
    q3Budget: numberText(target.q3Budget),
    q3RevisedTarget: numberText(target.q3RevisedTarget),
    q4Target: numberText(target.q4Target),
    q4Description: target.q4Description ?? '',
    q4Budget: numberText(target.q4Budget),
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
    annualTarget: String(target.annualTarget),
    annualTargetDescription: target.annualTargetDescription,
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
    q1UnitType: toXafUnitType(target.TargetUnitQ1 ?? target.targetUnitType),
    q2UnitType: toXafUnitType(target.TargetUnitQ2 ?? target.targetUnitType),
    midTermUnitType: toXafUnitType(target.MidTermTargetUnit ?? target.targetUnitType),
    q3UnitType: toXafUnitType(target.TargetUnitQ3 ?? target.targetUnitType),
    q4UnitType: toXafUnitType(target.TargetUnitQ4 ?? target.targetUnitType),
    annualUnitType: toXafUnitType(target.AnnualTargetUnit ?? target.targetUnitType),
    q1Target: numberText(target.q1Target),
    q1Description: '',
    q1Budget: '',
    q2Target: numberText(target.q2Target),
    q2Description: '',
    q2Budget: '',
    midTermTarget: numberText(target.midTermTarget),
    midTermDescription: '',
    midTermBudget: '',
    q3Target: numberText(target.q3Target),
    q3Description: '',
    q3Budget: '',
    q3RevisedTarget: '',
    q4Target: numberText(target.q4Target),
    q4Description: '',
    q4Budget: '',
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

function buildOpmsPayload(form: OpmsFormState): SaveOpmsTargetPayload {
  return {
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    periodId: form.periodId ? Number(form.periodId) : null,
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    unitId: form.unitId ? Number(form.unitId) : null,
    assignedUserId: form.assignedToId || null,
    wardIds: form.wardIds || null,
    additionalAssigneeIds: form.additionalAssigneeIds || null,
    voteNumberIds: form.voteNumberIds || null,
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
    annualTarget: Number(form.annualTarget || 0),
    annualTargetDescription: form.annualTargetDescription,
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
    isWithdrawn: form.isWithdrawn,
    reasonForWithdrawal: form.reasonForWithdrawal || null,
    targetUnitType: toApiUnitType(form.targetUnitType),
    q1Target: form.q1Target ? Number(form.q1Target) : null,
    q1Description: form.q1Description || null,
    q1Budget: form.q1Budget ? Number(form.q1Budget) : null,
    q2Target: form.q2Target ? Number(form.q2Target) : null,
    q2Description: form.q2Description || null,
    q2Budget: form.q2Budget ? Number(form.q2Budget) : null,
    midTermTarget: form.midTermTarget ? Number(form.midTermTarget) : null,
    midTermDescription: form.midTermDescription || null,
    midTermBudget: form.midTermBudget ? Number(form.midTermBudget) : null,
    q3Target: form.q3Target ? Number(form.q3Target) : null,
    q3Description: form.q3Description || null,
    q3Budget: form.q3Budget ? Number(form.q3Budget) : null,
    q3RevisedTarget: form.q3RevisedTarget ? Number(form.q3RevisedTarget) : null,
    q4Target: form.q4Target ? Number(form.q4Target) : null,
    q4Description: form.q4Description || null,
    q4Budget: form.q4Budget ? Number(form.q4Budget) : null,
    q4RevisedTarget: form.q4RevisedTarget ? Number(form.q4RevisedTarget) : null,
    revisedAnnualTarget: form.revisedAnnualTarget ? Number(form.revisedAnnualTarget) : null,
    revisedAnnualBudget: form.revisedAnnualBudget ? Number(form.revisedAnnualBudget) : null,
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
    annualTarget: Number(form.annualTarget || 0),
    annualTargetDescription: form.annualTargetDescription,
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
    targetUnitType: toApiUnitType(form.targetUnitType),
    q1Target: form.q1Target ? Number(form.q1Target) : null,
    q1Description: form.q1Description || null,
    q1Budget: form.q1Budget ? Number(form.q1Budget) : null,
    q2Target: form.q2Target ? Number(form.q2Target) : null,
    q2Description: form.q2Description || null,
    q2Budget: form.q2Budget ? Number(form.q2Budget) : null,
    midTermTarget: form.midTermTarget ? Number(form.midTermTarget) : null,
    midTermDescription: form.midTermDescription || null,
    midTermBudget: form.midTermBudget ? Number(form.midTermBudget) : null,
    q3Target: form.q3Target ? Number(form.q3Target) : null,
    q3Description: form.q3Description || null,
    q3Budget: form.q3Budget ? Number(form.q3Budget) : null,
    q3RevisedTarget: form.q3RevisedTarget ? Number(form.q3RevisedTarget) : null,
    q4Target: form.q4Target ? Number(form.q4Target) : null,
    q4Description: form.q4Description || null,
    q4Budget: form.q4Budget ? Number(form.q4Budget) : null,
    q4RevisedTarget: form.q4RevisedTarget ? Number(form.q4RevisedTarget) : null,
    revisedAnnualTarget: form.revisedAnnualTarget ? Number(form.revisedAnnualTarget) : null,
    revisedAnnualBudget: form.revisedAnnualBudget ? Number(form.revisedAnnualBudget) : null,
  };
}

function validateOpmsForm(form: OpmsFormState) {
  const errors = validateRequiredFields([
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

  if (form.isWithdrawn && !form.reasonForWithdrawal.trim()) {
    errors.push('Reason For Withdrawal is required when Target Withdrawn is checked.');
  }

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
  const [form, setForm] = useState<OpmsFormState>(createDefaultOpmsFormState());
  const [existingTarget, setExistingTarget] = useState<OPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [selectedWardId, setSelectedWardId] = useState('');
  const [selectedAssigneeId, setSelectedAssigneeId] = useState('');
  const [selectedVoteNumberId, setSelectedVoteNumberId] = useState('');

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

  if (isLoading) {
    return (
      <AppShell title={targetId ? 'Edit OPMS Target' : 'Create OPMS Target'} subtitle="Full OPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  const selectedDepartment = mockDepartments.find(item => item.id === form.departmentId);
  const selectedPeriod = mockPeriods.find(item => item.id === form.periodId);
  const selectedUnit = mockDepartmentUnits.find(item => item.id === form.unitId);
  const selectedUom = mockUnitsOfMeasure.find(item => item.id === form.unitOfMeasureId);
  const selectedWardIds = parseCsvIds(form.wardIds);
  const selectedAssigneeIds = parseCsvIds(form.additionalAssigneeIds);
  const selectedVoteIds = parseCsvIds(form.voteNumberIds);
  const relatedIpmsTargets = mockIPMSTargets.filter(item => item.relatedOPMSTarget?.id === existingTarget?.id);
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
              <Select label="Period" required error={fieldError('Period')} value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={mockPeriods.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Department" required error={fieldError('Department')} value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={mockDepartments.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...mockDepartmentUnits.filter(item => !form.departmentId || item.department.id === form.departmentId).map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Assigned User" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
            </FormRow>
            <FormRow cols={3}>
              <Select
                label="Wards"
                value={selectedWardId}
                onChange={(event) => setSelectedWardId(event.target.value)}
                options={[{ value: '', label: 'Select Ward' }, ...mockWards.map(item => ({ value: item.id, label: item.name }))]}
              />
              <div className="flex items-end">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    if (!selectedWardId) return;
                    setForm(prev => ({ ...prev, wardIds: appendCsvId(prev.wardIds, selectedWardId) }));
                    setSelectedWardId('');
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
                const ward = mockWards.find(item => item.id === wardId);
                return (
                  <button
                    key={wardId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, wardIds: removeCsvId(prev.wardIds, wardId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove ward"
                  >
                    {ward?.name ?? wardId} x
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
                  ...mockEmployees
                    .filter(item => !form.departmentId || item.department?.id === form.departmentId)
                    .map(item => ({ value: item.id, label: item.displayName })),
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
                const assignee = mockEmployees.find(item => item.id === assigneeId);
                return (
                  <button
                    key={assigneeId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, additionalAssigneeIds: removeCsvId(prev.additionalAssigneeIds, assigneeId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove assignee"
                  >
                    {assignee?.displayName ?? assigneeId} x
                  </button>
                );
              })}
            </div>

            <FormRow cols={3}>
              <Select
                label="Vote Numbers"
                value={selectedVoteNumberId}
                onChange={(event) => setSelectedVoteNumberId(event.target.value)}
                options={[{ value: '', label: 'Select Vote Number' }, ...mockVoteNumbers.map(item => ({ value: item.id, label: `${item.number} - ${item.description}` }))]}
              />
              <div className="flex items-end">
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => {
                    if (!selectedVoteNumberId) return;
                    setForm(prev => ({ ...prev, voteNumberIds: appendCsvId(prev.voteNumberIds, selectedVoteNumberId) }));
                    setSelectedVoteNumberId('');
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
                const vote = mockVoteNumbers.find(item => item.id === voteId);
                return (
                  <button
                    key={voteId}
                    type="button"
                    onClick={() => setForm(prev => ({ ...prev, voteNumberIds: removeCsvId(prev.voteNumberIds, voteId) }))}
                    className="rounded-full border border-secondary-300 px-3 py-1 text-xs text-secondary-700 hover:bg-secondary-100 dark:border-secondary-700 dark:text-secondary-200 dark:hover:bg-secondary-800"
                    title="Remove vote number"
                  >
                    {vote ? `${vote.number} - ${vote.description}` : voteId} x
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
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value }))} options={mockStrategicGoals.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={mockStrategicObjectives.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Measurement Details" description="Define numeric measures, unit configuration, and performance classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" required type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
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
              <Select label="Budget Source" value={form.budgetSourceId} onChange={(event) => setForm(prev => ({ ...prev, budgetSourceId: event.target.value }))} options={mockBudgetSources.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Budget Type" value={form.budgetTypeId} onChange={(event) => setForm(prev => ({ ...prev, budgetTypeId: event.target.value }))} options={mockBudgetTypes.map(item => ({ value: item.id, label: item.name }))} />
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

        <FormPanel title="Quarterly Targets" description="Capture the full quarterly and revision structure on the same page." icon={<CalendarRange className="h-5 w-5" />}>
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
                  <Input type="number" value={form.q1Target} onChange={(e) => setForm(prev => ({ ...prev, q1Target: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.q2Target} onChange={(e) => setForm(prev => ({ ...prev, q2Target: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.midTermTarget} onChange={(e) => setForm(prev => ({ ...prev, midTermTarget: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.q3Target} onChange={(e) => setForm(prev => ({ ...prev, q3Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q3Description} onChange={(e) => setForm(prev => ({ ...prev, q3Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q3UnitType)}</span>
                </div>
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.q3RevisedTarget} onChange={(e) => setForm(prev => ({ ...prev, q3RevisedTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.q3Budget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {form.q3Budget}</span>
                      </div>
                    )}
                  </div>
                )}
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
                  <Input type="number" value={form.q4Target} onChange={(e) => setForm(prev => ({ ...prev, q4Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q4Description} onChange={(e) => setForm(prev => ({ ...prev, q4Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q4UnitType)}</span>
                </div>
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.q4RevisedTarget} onChange={(e) => setForm(prev => ({ ...prev, q4RevisedTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.q4Budget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {form.q4Budget}</span>
                      </div>
                    )}
                  </div>
                )}
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
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.revisedAnnualTarget} onChange={(e) => setForm(prev => ({ ...prev, revisedAnnualTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.revisedAnnualBudget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {Number(form.revisedAnnualBudget).toLocaleString()}</span>
                      </div>
                    )}
                  </div>
                )}
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

        <FormPanel title="Workflow Flags" description="Track revision and withdrawal attributes for the live target." icon={<Building2 className="h-5 w-5" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Checkbox label="Target Revised" checked={form.isRevised} onChange={(event) => setForm(prev => ({ ...prev, isRevised: event.target.checked }))} />
            <Checkbox label="Target Withdrawn" checked={form.isWithdrawn} onChange={(event) => setForm(prev => ({ ...prev, isWithdrawn: event.target.checked }))} />
          </div>
          <Textarea label="Reason For Withdrawal" error={fieldError('Reason For Withdrawal')} rows={3} value={form.reasonForWithdrawal} onChange={(event) => setForm(prev => ({ ...prev, reasonForWithdrawal: event.target.value }))} />
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
  const [form, setForm] = useState<IpmsFormState>(createDefaultIpmsFormState());
  const [existingTarget, setExistingTarget] = useState<IPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

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

  if (isLoading) {
    return (
      <AppShell title={targetId ? 'Edit IPMS Target' : 'Create IPMS Target'} subtitle="Full IPMS target workspace">
        <Card>
          <p className="text-sm text-secondary-500">Loading target workspace...</p>
        </Card>
      </AppShell>
    );
  }

  const selectedDepartment = mockDepartments.find(item => item.id === form.departmentId);
  const selectedPeriod = mockPeriods.find(item => item.id === form.periodId);
  const selectedUnit = mockDepartmentUnits.find(item => item.id === form.unitId);
  const linkedOpms = mockOPMSTargets.find(item => item.id === form.relatedOPMSTargetId);
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
              <Select label="Related OPMS Target" value={form.relatedOPMSTargetId} onChange={(event) => setForm(prev => ({ ...prev, relatedOPMSTargetId: event.target.value }))} options={[{ value: '', label: 'No Link' }, ...mockOPMSTargets.map(item => ({ value: item.id, label: `${item.indicatorNumber} - ${item.targetName}` }))]} />
              <div className="flex items-end">
                <Button variant="outline" className="w-full" disabled={!form.relatedOPMSTargetId} onClick={() => setForm(prev => ({ ...prev, relatedOPMSTargetId: '' }))}>
                  Unlink
                </Button>
              </div>
              <Input label="Linked OPMS" value={linkedOpms ? `${linkedOpms.indicatorNumber} - ${linkedOpms.targetName}` : 'Not linked'} readOnly />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Period" required error={fieldError('Period')} value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={mockPeriods.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Department" required error={fieldError('Department')} value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={mockDepartments.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...mockDepartmentUnits.filter(item => !form.departmentId || item.department.id === form.departmentId).map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Employee" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
            </FormRow>
            <Select label="Supervisor" value={form.supervisorId} onChange={(event) => setForm(prev => ({ ...prev, supervisorId: event.target.value }))} options={[{ value: '', label: 'Select Supervisor' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
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
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value }))} options={mockStrategicGoals.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={mockStrategicObjectives.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required error={fieldError('Performance Objective')} value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required error={fieldError('KPI Description')} rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Performance Measures" description="Set numeric measures, unit configuration, and classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required error={fieldError('Annual Target')} type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required error={fieldError('Weight %')} type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required error={fieldError('Unit of Measure')} value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
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
              <Select label="Budget Source" value={form.budgetSourceId} onChange={(event) => setForm(prev => ({ ...prev, budgetSourceId: event.target.value }))} options={mockBudgetSources.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Budget Type" value={form.budgetTypeId} onChange={(event) => setForm(prev => ({ ...prev, budgetTypeId: event.target.value }))} options={mockBudgetTypes.map(item => ({ value: item.id, label: item.name }))} />
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
                <p className="mt-1 text-sm font-medium text-secondary-900 dark:text-white">{linkedOpms?.indicatorNumber ?? 'Not Linked'}</p>
              </div>
            </div>
          </FormPanel>
        </div>

        <FormPanel title="Quarterly Targets" description="Capture the live IPMS quarterly structure without using a modal." icon={<CalendarRange className="h-5 w-5" />}>
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
                  <Input type="number" value={form.q1Target} onChange={(e) => setForm(prev => ({ ...prev, q1Target: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.q2Target} onChange={(e) => setForm(prev => ({ ...prev, q2Target: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.midTermTarget} onChange={(e) => setForm(prev => ({ ...prev, midTermTarget: e.target.value }))} className="text-2xl font-bold h-10" />
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
                  <Input type="number" value={form.q3Target} onChange={(e) => setForm(prev => ({ ...prev, q3Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q3Description} onChange={(e) => setForm(prev => ({ ...prev, q3Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q3UnitType)}</span>
                </div>
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.q3RevisedTarget} onChange={(e) => setForm(prev => ({ ...prev, q3RevisedTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.q3Budget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {form.q3Budget}</span>
                      </div>
                    )}
                  </div>
                )}
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
                  <Input type="number" value={form.q4Target} onChange={(e) => setForm(prev => ({ ...prev, q4Target: e.target.value }))} className="text-2xl font-bold h-10" />
                </div>
                <Textarea label="" rows={2} value={form.q4Description} onChange={(e) => setForm(prev => ({ ...prev, q4Description: e.target.value }))} placeholder="Description" />
                <div className="flex items-center gap-2 text-xs text-secondary-500">
                  <span className="w-2 h-2 rounded-full bg-secondary-400"></span>
                  <span>{getTargetUnitLabel(form.q4UnitType)}</span>
                </div>
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.q4RevisedTarget} onChange={(e) => setForm(prev => ({ ...prev, q4RevisedTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.q4Budget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {form.q4Budget}</span>
                      </div>
                    )}
                  </div>
                )}
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
                {form.isRevised && (
                  <div className="mt-2 pt-2 border-t border-dashed border-secondary-300">
                    <p className="text-xs font-semibold text-amber-600 uppercase tracking-wide mb-1">REVISED</p>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-secondary-600">Revised Target</span>
                      <Input type="number" value={form.revisedAnnualTarget} onChange={(e) => setForm(prev => ({ ...prev, revisedAnnualTarget: e.target.value }))} className="text-right font-semibold" />
                    </div>
                    {form.revisedAnnualBudget && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-secondary-600">Revised Budget</span>
                        <span className="font-semibold">R {Number(form.revisedAnnualBudget).toLocaleString()}</span>
                      </div>
                    )}
                  </div>
                )}
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
