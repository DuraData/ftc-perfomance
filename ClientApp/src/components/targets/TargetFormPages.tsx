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
} from '../../types';

const targetUnitTypeOptions: { value: TargetUnitType; label: string }[] = [
  { value: 'percentage', label: 'Percentage' },
  { value: 'absolute_count', label: 'Absolute Count' },
  { value: 'financial', label: 'Financial' },
  { value: 'area_based', label: 'Area Based' },
  { value: 'volume_based', label: 'Volume Based' },
  { value: 'index_scores', label: 'Index Scores' },
  { value: 'ratios', label: 'Ratios' },
  { value: 'time_based', label: 'Time Based' },
  { value: 'binary', label: 'Binary' },
  { value: 'date', label: 'Date' },
  { value: 'readiness_scale', label: 'Readiness Scale' },
  { value: 'qualitative', label: 'Qualitative' },
  { value: 'zero_based', label: 'Zero Based' },
  { value: 'reverse_cumulative', label: 'Reverse Cumulative' },
  { value: 'reverse_non_cumulative', label: 'Reverse Non-Cumulative' },
  { value: 'binary_determination', label: 'Binary Determination' },
];

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
  q1Target: string;
  q2Target: string;
  midTermTarget: string;
  q3Target: string;
  q4Target: string;
};

function numberText(value?: number) {
  return value === undefined || value === null ? '' : String(value);
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
    targetUnitType: baseTarget.targetUnitType,
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
    targetUnitType: baseTarget.targetUnitType,
    q1Target: '',
    q2Target: '',
    midTermTarget: '',
    q3Target: '',
    q4Target: '',
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
    targetUnitType: target.targetUnitType,
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
    targetUnitType: target.targetUnitType,
    q1Target: numberText(target.q1Target),
    q2Target: numberText(target.q2Target),
    midTermTarget: numberText(target.midTermTarget),
    q3Target: numberText(target.q3Target),
    q4Target: numberText(target.q4Target),
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
    targetUnitType: template.targetUnitType,
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
    targetUnitType: template.targetUnitType,
  };
}

function buildOpmsPayload(form: OpmsFormState): SaveOpmsTargetPayload {
  return {
    indicatorNumber: form.indicatorNumber,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    unitId: form.unitId ? Number(form.unitId) : null,
    assignedUserId: form.assignedToId || null,
    kpiId: null,
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    baseline: Number(form.baseline || 0),
    annualTarget: Number(form.annualTarget || 0),
    weight: Number(form.weight || 0),
    isArchived: form.isWithdrawn,
  };
}

function buildIpmsPayload(form: IpmsFormState): SaveIpmsTargetPayload {
  return {
    indicatorNumber: form.indicatorNumber,
    targetName: form.targetName,
    kpiDescription: form.kpiDescription,
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    unitId: form.unitId ? Number(form.unitId) : null,
    assignedUserId: form.assignedToId || null,
    relatedOpmsTargetId: form.relatedOPMSTargetId || null,
    kpiId: null,
    sourceTemplateId: form.sourceTemplateId || null,
    sourceTemplateVersion: form.sourceTemplateVersion ? Number(form.sourceTemplateVersion) : null,
    annualTarget: Number(form.annualTarget || 0),
    weight: Number(form.weight || 0),
    isArchived: form.isRevised,
  };
}

function parseIdList(value: string) {
  return value.split(',').map(item => item.trim()).filter(Boolean);
}

export function OPMSTargetFormPage({ targetId }: { targetId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [form, setForm] = useState<OpmsFormState>(createDefaultOpmsFormState());
  const [existingTarget, setExistingTarget] = useState<OPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);

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

        <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
          <FormPanel title="Planning Setup" description="Define ownership, period, template source, and local assignment information." icon={<CalendarRange className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Source Template Id" value={form.sourceTemplateId} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateId: event.target.value }))} />
              <Input label="Template Version" value={form.sourceTemplateVersion} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateVersion: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Period" required value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={mockPeriods.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Department" required value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={mockDepartments.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...mockDepartmentUnits.filter(item => !form.departmentId || item.department.id === form.departmentId).map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Assigned User" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="Wards" value={form.wardIds} onChange={(event) => setForm(prev => ({ ...prev, wardIds: event.target.value }))} helpText="Comma separated ward ids or names." />
              <Input label="Additional Assignees" value={form.additionalAssigneeIds} onChange={(event) => setForm(prev => ({ ...prev, additionalAssigneeIds: event.target.value }))} helpText="Comma separated employee ids." />
            </FormRow>
            <Input label="Vote Numbers" value={form.voteNumberIds} onChange={(event) => setForm(prev => ({ ...prev, voteNumberIds: event.target.value }))} helpText="Comma separated vote number ids." />
          </FormPanel>

          <FormPanel title="Target Definition" description="Capture the full identity and strategic alignment of the OPMS target." icon={<Target className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Indicator Number" required value={form.indicatorNumber} onChange={(event) => setForm(prev => ({ ...prev, indicatorNumber: event.target.value }))} />
              <Input label="Target Name" required value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="National KPA" required value={form.nationalKPA} onChange={(event) => setForm(prev => ({ ...prev, nationalKPA: event.target.value }))} />
              <Input label="Municipal KPA" required value={form.municipalKPA} onChange={(event) => setForm(prev => ({ ...prev, municipalKPA: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value }))} options={mockStrategicGoals.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={mockStrategicObjectives.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Measurement Details" description="Define numeric measures, unit configuration, and performance classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" required type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Textarea label="Baseline Description" rows={3} value={form.baselineDescription} onChange={(event) => setForm(prev => ({ ...prev, baselineDescription: event.target.value }))} />
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
            <FormRow cols={4}>
              <Select label="Target Unit Type" required value={form.targetUnitType} onChange={(event) => setForm(prev => ({ ...prev, targetUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
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
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <Card className="border border-secondary-200 dark:border-secondary-700" padding="md">
              <div className="space-y-3">
                <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Q1</h4>
                <Input label="Target" type="number" value={form.q1Target} onChange={(event) => setForm(prev => ({ ...prev, q1Target: event.target.value }))} />
                <Input label="Budget" type="number" value={form.q1Budget} onChange={(event) => setForm(prev => ({ ...prev, q1Budget: event.target.value }))} />
                <Textarea label="Description" rows={3} value={form.q1Description} onChange={(event) => setForm(prev => ({ ...prev, q1Description: event.target.value }))} />
              </div>
            </Card>
            <Card className="border border-secondary-200 dark:border-secondary-700" padding="md">
              <div className="space-y-3">
                <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Q2</h4>
                <Input label="Target" type="number" value={form.q2Target} onChange={(event) => setForm(prev => ({ ...prev, q2Target: event.target.value }))} />
                <Input label="Budget" type="number" value={form.q2Budget} onChange={(event) => setForm(prev => ({ ...prev, q2Budget: event.target.value }))} />
                <Textarea label="Description" rows={3} value={form.q2Description} onChange={(event) => setForm(prev => ({ ...prev, q2Description: event.target.value }))} />
              </div>
            </Card>
            <Card className="border border-secondary-200 dark:border-secondary-700" padding="md">
              <div className="space-y-3">
                <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Mid-Year</h4>
                <Input label="Target" type="number" value={form.midTermTarget} onChange={(event) => setForm(prev => ({ ...prev, midTermTarget: event.target.value }))} />
                <Input label="Budget" type="number" value={form.midTermBudget} onChange={(event) => setForm(prev => ({ ...prev, midTermBudget: event.target.value }))} />
                <Textarea label="Description" rows={3} value={form.midTermDescription} onChange={(event) => setForm(prev => ({ ...prev, midTermDescription: event.target.value }))} />
              </div>
            </Card>
            <Card className="border border-secondary-200 dark:border-secondary-700" padding="md">
              <div className="space-y-3">
                <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">Q3 / Q4</h4>
                <Input label="Q3 Target" type="number" value={form.q3Target} onChange={(event) => setForm(prev => ({ ...prev, q3Target: event.target.value }))} />
                <Input label="Q3 Budget" type="number" value={form.q3Budget} onChange={(event) => setForm(prev => ({ ...prev, q3Budget: event.target.value }))} />
                <Input label="Q3 Revised" type="number" value={form.q3RevisedTarget} onChange={(event) => setForm(prev => ({ ...prev, q3RevisedTarget: event.target.value }))} />
                <Textarea label="Q3 Description" rows={2} value={form.q3Description} onChange={(event) => setForm(prev => ({ ...prev, q3Description: event.target.value }))} />
                <Input label="Q4 Target" type="number" value={form.q4Target} onChange={(event) => setForm(prev => ({ ...prev, q4Target: event.target.value }))} />
                <Input label="Q4 Budget" type="number" value={form.q4Budget} onChange={(event) => setForm(prev => ({ ...prev, q4Budget: event.target.value }))} />
                <Input label="Q4 Revised" type="number" value={form.q4RevisedTarget} onChange={(event) => setForm(prev => ({ ...prev, q4RevisedTarget: event.target.value }))} />
                <Textarea label="Q4 Description" rows={2} value={form.q4Description} onChange={(event) => setForm(prev => ({ ...prev, q4Description: event.target.value }))} />
              </div>
            </Card>
          </div>
          <FormRow cols={2}>
            <Input label="Revised Annual Target" type="number" value={form.revisedAnnualTarget} onChange={(event) => setForm(prev => ({ ...prev, revisedAnnualTarget: event.target.value }))} />
            <Input label="Revised Annual Budget" type="number" value={form.revisedAnnualBudget} onChange={(event) => setForm(prev => ({ ...prev, revisedAnnualBudget: event.target.value }))} />
          </FormRow>
        </FormPanel>

        <FormPanel title="Workflow Flags" description="Track revision and withdrawal attributes for the live target." icon={<Building2 className="h-5 w-5" />}>
          <div className="grid gap-4 md:grid-cols-2">
            <Checkbox label="Target Revised" checked={form.isRevised} onChange={(event) => setForm(prev => ({ ...prev, isRevised: event.target.checked }))} />
            <Checkbox label="Target Withdrawn" checked={form.isWithdrawn} onChange={(event) => setForm(prev => ({ ...prev, isWithdrawn: event.target.checked }))} />
          </div>
          <Textarea label="Reason For Withdrawal" rows={3} value={form.reasonForWithdrawal} onChange={(event) => setForm(prev => ({ ...prev, reasonForWithdrawal: event.target.value }))} />
        </FormPanel>

        <div className="flex justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
          <Button variant="outline" onClick={() => setCurrentPath(targetId ? `/opms/targets/${targetId}` : '/opms/targets')}>
            Cancel
          </Button>
          <Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>
            {targetId ? 'Save Target' : 'Create Target'}
          </Button>
        </div>
      </div>
    </AppShell>
  );
}

export function IPMSTargetFormPage({ targetId }: { targetId?: string }) {
  const { pushToast, setCurrentPath } = useApp();
  const [form, setForm] = useState<IpmsFormState>(createDefaultIpmsFormState());
  const [existingTarget, setExistingTarget] = useState<IPMSTarget | null>(null);
  const [isLoading, setIsLoading] = useState(!!targetId);

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

        <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
          <FormPanel title="Alignment And Ownership" description="Select the planning period, related OPMS target, employee, supervisor, and department." icon={<CalendarRange className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Source Template Id" value={form.sourceTemplateId} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateId: event.target.value }))} />
              <Input label="Template Version" value={form.sourceTemplateVersion} onChange={(event) => setForm(prev => ({ ...prev, sourceTemplateVersion: event.target.value }))} />
            </FormRow>
            <Select label="Related OPMS Target" value={form.relatedOPMSTargetId} onChange={(event) => setForm(prev => ({ ...prev, relatedOPMSTargetId: event.target.value }))} options={[{ value: '', label: 'No Link' }, ...mockOPMSTargets.map(item => ({ value: item.id, label: `${item.indicatorNumber} - ${item.targetName}` }))]} />
            <FormRow cols={2}>
              <Select label="Period" required value={form.periodId} onChange={(event) => setForm(prev => ({ ...prev, periodId: event.target.value }))} options={mockPeriods.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Department" required value={form.departmentId} onChange={(event) => setForm(prev => ({ ...prev, departmentId: event.target.value }))} options={mockDepartments.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Unit" value={form.unitId} onChange={(event) => setForm(prev => ({ ...prev, unitId: event.target.value }))} options={[{ value: '', label: 'No Unit' }, ...mockDepartmentUnits.filter(item => !form.departmentId || item.department.id === form.departmentId).map(item => ({ value: item.id, label: item.name }))]} />
              <Select label="Employee" value={form.assignedToId} onChange={(event) => setForm(prev => ({ ...prev, assignedToId: event.target.value }))} options={[{ value: '', label: 'Select Employee' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
            </FormRow>
            <Select label="Supervisor" value={form.supervisorId} onChange={(event) => setForm(prev => ({ ...prev, supervisorId: event.target.value }))} options={[{ value: '', label: 'Select Supervisor' }, ...mockEmployees.filter(item => !form.departmentId || item.department?.id === form.departmentId).map(item => ({ value: item.id, label: item.displayName }))]} />
          </FormPanel>

          <FormPanel title="Target Definition" description="Define strategic alignment and the employee-level performance target." icon={<UserSquare2 className="h-5 w-5" />}>
            <FormRow cols={2}>
              <Input label="Indicator Number" required value={form.indicatorNumber} onChange={(event) => setForm(prev => ({ ...prev, indicatorNumber: event.target.value }))} />
              <Input label="Target Name" required value={form.targetName} onChange={(event) => setForm(prev => ({ ...prev, targetName: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Input label="National KPA" required value={form.nationalKPA} onChange={(event) => setForm(prev => ({ ...prev, nationalKPA: event.target.value }))} />
              <Input label="Municipal KPA" required value={form.municipalKPA} onChange={(event) => setForm(prev => ({ ...prev, municipalKPA: event.target.value }))} />
            </FormRow>
            <FormRow cols={2}>
              <Select label="Strategic Goal" value={form.strategicGoalId} onChange={(event) => setForm(prev => ({ ...prev, strategicGoalId: event.target.value }))} options={mockStrategicGoals.map(item => ({ value: item.id, label: item.name }))} />
              <Select label="Strategic Objective" value={form.strategicObjectiveId} onChange={(event) => setForm(prev => ({ ...prev, strategicObjectiveId: event.target.value }))} options={mockStrategicObjectives.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Input label="Performance Objective" required value={form.performanceObjective} onChange={(event) => setForm(prev => ({ ...prev, performanceObjective: event.target.value }))} />
            <Textarea label="KPI Description" required rows={4} value={form.kpiDescription} onChange={(event) => setForm(prev => ({ ...prev, kpiDescription: event.target.value }))} />
          </FormPanel>
        </div>

        <div className="grid gap-4 xl:grid-cols-2">
          <FormPanel title="Performance Measures" description="Set numeric measures, unit configuration, and classification." icon={<BarChart3 className="h-5 w-5" />}>
            <FormRow cols={4}>
              <Input label="Baseline" type="number" value={form.baseline} onChange={(event) => setForm(prev => ({ ...prev, baseline: event.target.value }))} />
              <Input label="Annual Target" required type="number" value={form.annualTarget} onChange={(event) => setForm(prev => ({ ...prev, annualTarget: event.target.value }))} />
              <Input label="Weight %" required type="number" value={form.weight} onChange={(event) => setForm(prev => ({ ...prev, weight: event.target.value }))} />
              <Select label="Unit Of Measure" required value={form.unitOfMeasureId} onChange={(event) => setForm(prev => ({ ...prev, unitOfMeasureId: event.target.value }))} options={mockUnitsOfMeasure.map(item => ({ value: item.id, label: item.name }))} />
            </FormRow>
            <Textarea label="Annual Target Description" rows={3} value={form.annualTargetDescription} onChange={(event) => setForm(prev => ({ ...prev, annualTargetDescription: event.target.value }))} />
            <FormRow cols={4}>
              <Select label="Target Unit Type" required value={form.targetUnitType} onChange={(event) => setForm(prev => ({ ...prev, targetUnitType: event.target.value as TargetUnitType }))} options={targetUnitTypeOptions} />
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
          <FormRow cols={5}>
            <Input label="Q1 Target" type="number" value={form.q1Target} onChange={(event) => setForm(prev => ({ ...prev, q1Target: event.target.value }))} />
            <Input label="Q2 Target" type="number" value={form.q2Target} onChange={(event) => setForm(prev => ({ ...prev, q2Target: event.target.value }))} />
            <Input label="Mid-Year Target" type="number" value={form.midTermTarget} onChange={(event) => setForm(prev => ({ ...prev, midTermTarget: event.target.value }))} />
            <Input label="Q3 Target" type="number" value={form.q3Target} onChange={(event) => setForm(prev => ({ ...prev, q3Target: event.target.value }))} />
            <Input label="Q4 Target" type="number" value={form.q4Target} onChange={(event) => setForm(prev => ({ ...prev, q4Target: event.target.value }))} />
          </FormRow>
        </FormPanel>

        <div className="flex justify-end gap-3 border-t border-secondary-200 pt-4 dark:border-secondary-700">
          <Button variant="outline" onClick={() => setCurrentPath(targetId ? `/ipms/targets/${targetId}` : '/ipms/targets')}>
            Cancel
          </Button>
          <Button variant="primary" icon={<Save className="h-4 w-4" />} onClick={() => { void handleSave(); }}>
            {targetId ? 'Save Target' : 'Create Target'}
          </Button>
        </div>
      </div>
    </AppShell>
  );
}
