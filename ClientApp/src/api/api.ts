import type {
  ApiResponse,
  AuditTrailEntryDto,
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RegisterRequest,
  AdminUserDetail,
  AdminRole,
  AdminPermission,
  AdminPermissionGroup,
  RolePermission,
  UserPermissions,
  UserPermissionOverride,
  MenuItem,
  LoginAuditLog,
  DemoUser,
  RoleImplementationAuditRow,
  AccessSimulationResult,
  RoleAccessMatrixRow,
  SystemCoverageAuditRow,
  OPMSTarget,
  IPMSTarget,
  OPMSSubmission,
  IPMSSubmission,
  OpmsTargetTemplate,
  IpmsTargetTemplate,
  OpmsTargetTemplateDto,
  IpmsTargetTemplateDto,
  OpmsTargetDto,
  IpmsTargetDto,
  OpmsSubmissionDto,
  IpmsSubmissionDto,
  NotificationDto,
  PoeFileDto,
  SaveOpmsTargetTemplatePayload,
  SaveIpmsTargetTemplatePayload,
  SaveOpmsTargetPayload,
  SaveIpmsTargetPayload,
  SaveOpmsSubmissionPayload,
  SaveIpmsSubmissionPayload,
  SubmissionWorkflowActionPayload,
  DueDateExtensionPayload,
  TemplateQuarterlyTarget,
  Quarter,
  SubmissionStatus,
  TargetUnitType,
  IdpPlanSummary,
  IdpPlanVersion,
  IdpHierarchy,
  IdpDashboard,
  IdpAlignmentMatrixItem,
  IdpReportDocument,
  CreateIdpPlanPayload,
  CreateIdpPlanVersionPayload,
  CreateIdpCommentPayload,
  CreateIdpCommunitySessionPayload,
} from '../types';
import {
  mockBudgetSources,
  mockBudgetTypes,
  mockDepartments,
  mockDepartmentUnits,
  mockEmployees,
  mockIPMSSubmissions,
  mockIPMSTargets,
  mockOPMSSubmissions,
  mockOPMSTargets,
  mockPeriods,
  mockStrategicGoals,
  mockStrategicObjectives,
  mockUnitsOfMeasure,
} from '../data/mockData';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

let accessToken: string | null = localStorage.getItem('auth_token');
let refreshToken: string | null = localStorage.getItem('refresh_token');

function setTokens(access: string, refresh: string) {
  accessToken = access;
  refreshToken = refresh;
  localStorage.setItem('auth_token', access);
  localStorage.setItem('refresh_token', refresh);
}

function clearTokens() {
  accessToken = null;
  refreshToken = null;
  localStorage.removeItem('auth_token');
  localStorage.removeItem('refresh_token');
}

function mapResponse<TIn, TOut>(
  response: ApiResponse<TIn>,
  mapper: (value: TIn) => TOut,
): ApiResponse<TOut> {
  if (!response.success || response.data === undefined) {
    return response as unknown as ApiResponse<TOut>;
  }

  return {
    ...response,
    data: mapper(response.data),
  };
}

function normalizeOptionalString(value?: string | null) {
  return value?.trim() ? value : undefined;
}

function coerceTargetUnitType(value?: string | null): TargetUnitType {
  const normalized = (value ?? '').trim() as TargetUnitType;
  const knownTypes: TargetUnitType[] = [
    'percentage',
    'absolute_count',
    'financial',
    'area_based',
    'volume_based',
    'index_scores',
    'ratios',
    'time_based',
    'binary',
    'date',
    'readiness_scale',
    'qualitative',
    'zero_based',
    'reverse_cumulative',
    'reverse_non_cumulative',
    'binary_determination',
  ];

  return knownTypes.includes(normalized) ? normalized : 'absolute_count';
}

function coerceQuarter(value?: string | null): Quarter {
  const normalized = (value ?? '').trim() as Quarter;
  const knownQuarters: Quarter[] = ['Q1', 'Q2', 'Mid-Year', 'Q3', 'Q4', 'Annual'];
  return knownQuarters.includes(normalized) ? normalized : 'Q1';
}

function coerceSubmissionStatus(value?: string | null): SubmissionStatus {
  const normalized = (value ?? '').trim() as SubmissionStatus;
  const knownStatuses: SubmissionStatus[] = [
    'draft',
    'submitted',
    'pending_verification',
    'verified',
    'verify_rejected',
    'pending_approval',
    'approved',
    'rejected',
    'reviewed',
    'returned_for_info',
    'audited',
    'completed',
  ];

  return knownStatuses.includes(normalized) ? normalized : 'draft';
}

function parseJsonArray<T>(value?: string | null, fallback: T[] = []): T[] {
  if (!value) {
    return fallback;
  }

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed as T[] : fallback;
  } catch {
    return fallback;
  }
}

function pickUnitOfMeasure(name?: string | null) {
  return mockUnitsOfMeasure.find(unit =>
    unit.name.toLowerCase() === (name ?? '').toLowerCase() ||
    unit.code.toLowerCase() === (name ?? '').toLowerCase(),
  ) ?? mockUnitsOfMeasure[0];
}

function pickDepartment(id?: number | null, name?: string | null) {
  return mockDepartments.find(department =>
    (id !== null && id !== undefined && department.id === String(id)) ||
    (!!name && department.name.toLowerCase() === name.toLowerCase()),
  ) ?? {
    ...mockDepartments[0],
    id: id !== null && id !== undefined ? String(id) : mockDepartments[0]?.id ?? 'department-0',
    name: name ?? mockDepartments[0]?.name ?? 'Unassigned Department',
  };
}

function pickUnit(id?: number | null, name?: string | null, departmentId?: number | null) {
  return mockDepartmentUnits.find(unit =>
    (id !== null && id !== undefined && unit.id === String(id)) ||
    (!!name && unit.name.toLowerCase() === name.toLowerCase()) ||
    (departmentId !== null && departmentId !== undefined && unit.department.id === String(departmentId)),
  );
}

function resolveQuarterlyTargets(value?: string | null): TemplateQuarterlyTarget[] {
  return parseJsonArray<TemplateQuarterlyTarget>(value).map(item => ({
    quarter: coerceQuarter(item.quarter),
    target: item.target,
    description: item.description,
    budget: item.budget,
  }));
}

function resolveTaskTemplates(value?: string | null) {
  return parseJsonArray<string>(value).filter(Boolean);
}

function toOpmsTemplateModel(dto: OpmsTargetTemplateDto): OpmsTargetTemplate {
  return {
    id: String(dto.id),
    templateCode: dto.templateCode,
    templateName: dto.templateName,
    indicatorNumber: dto.indicatorNumber,
    targetName: dto.targetName,
    kpiDescription: dto.kpiDescription,
    baseline: dto.baseline,
    annualTarget: dto.annualTarget,
    annualTargetDescription: dto.annualTargetDescription ?? '',
    targetUnitType: coerceTargetUnitType(dto.targetUnitType),
    unitOfMeasure: pickUnitOfMeasure(dto.unitOfMeasure),
    nationalKPA: dto.nationalKpa ?? '',
    municipalKPA: dto.municipalKpa ?? '',
    strategicGoal: mockStrategicGoals.find(goal => goal.name === dto.strategicGoal),
    strategicObjective: mockStrategicObjectives.find(objective => objective.name === dto.strategicObjective),
    performanceObjective: dto.performanceObjective ?? '',
    outcome: normalizeOptionalString(dto.outcome),
    output: normalizeOptionalString(dto.output),
    priorityIssue: normalizeOptionalString(dto.priorityIssue),
    budgetSource: mockBudgetSources.find(item => item.name === dto.budgetSource),
    budgetType: mockBudgetTypes.find(item => item.name === dto.budgetType),
    weight: dto.weight,
    kpiType: dto.kpiType ?? '',
    indicatorType: dto.indicatorType ?? '',
    functionalArea: normalizeOptionalString(dto.functionalArea),
    standardClassification: normalizeOptionalString(dto.standardClassification),
    idpReference: normalizeOptionalString(dto.idpReference),
    internalReference: normalizeOptionalString(dto.internalReference),
    fmsLink: normalizeOptionalString(dto.fmsLink),
    defaultQuarterlyTargets: resolveQuarterlyTargets(dto.defaultQuarterlyTargetsJson),
    defaultBudgetInformation: normalizeOptionalString(dto.defaultBudgetInformation),
    defaultPoeRequirements: normalizeOptionalString(dto.defaultPoeRequirements),
    isActive: dto.isActive,
    isArchived: dto.isArchived,
    version: dto.version,
    createdBy: dto.createdBy ?? 'System',
    createdDate: dto.createdDate,
  };
}

function toIpmsTemplateModel(dto: IpmsTargetTemplateDto): IpmsTargetTemplate {
  return {
    id: String(dto.id),
    templateCode: dto.templateCode,
    templateName: dto.templateName,
    targetName: dto.targetName,
    kpiDescription: dto.kpiDescription,
    performanceArea: dto.performanceArea ?? '',
    employeeLevel: dto.employeeLevel ?? '',
    jobGrade: dto.jobGrade ?? '',
    targetUnitType: coerceTargetUnitType(dto.targetUnitType),
    unitOfMeasure: pickUnitOfMeasure(dto.unitOfMeasure),
    annualTarget: dto.annualTarget,
    annualTargetDescription: dto.annualTargetDescription ?? '',
    weight: dto.weight,
    defaultRatingMethod: normalizeOptionalString(dto.defaultRatingMethod),
    defaultScoreScale: normalizeOptionalString(dto.defaultScoreScale),
    defaultPoeRequirements: normalizeOptionalString(dto.defaultPoeRequirements),
    defaultTaskTemplates: resolveTaskTemplates(dto.defaultTaskTemplatesJson),
    linkedOpmsTargetRequired: dto.linkedOpmsTargetRequired,
    functionalArea: normalizeOptionalString(dto.functionalArea),
    isActive: dto.isActive,
    isArchived: dto.isArchived,
    version: dto.version,
    createdBy: dto.createdBy ?? 'System',
    createdDate: dto.createdDate,
  };
}

function toOpmsTargetModel(dto: OpmsTargetDto): OPMSTarget {
  const baseTarget = mockOPMSTargets[0];
  return {
    ...baseTarget,
    id: dto.id,
    sourceTemplateId: dto.sourceTemplateId ?? undefined,
    sourceTemplateVersion: dto.sourceTemplateVersion ?? undefined,
    period: mockPeriods.find(p => p.id === (dto.periodId?.toString() ?? '')) ?? baseTarget.period,
    department: pickDepartment(dto.departmentId, dto.departmentName),
    unit: pickUnit(dto.unitId, dto.unitName, dto.departmentId),
    assignedTo: mockEmployees.find(e => e.id === dto.assignedUserId) ?? baseTarget.assignedTo,
    indicatorNumber: dto.indicatorNumber,
    nationalKPA: dto.nationalKpa,
    municipalKPA: dto.municipalKpa,
    strategicGoal: mockStrategicGoals.find(sg => sg.id === (dto.strategicGoalId?.toString() ?? '')) ?? baseTarget.strategicGoal,
    strategicObjective: mockStrategicObjectives.find(so => so.id === (dto.strategicObjectiveId?.toString() ?? '')) ?? baseTarget.strategicObjective,
    performanceObjective: dto.performanceObjective,
    targetName: dto.targetName,
    kpiDescription: dto.kpiDescription,
    baseline: dto.baseline,
    baselineDescription: dto.baselineDescription ?? '',
    annualTarget: dto.annualTarget,
    annualTargetDescription: dto.annualTargetDescription,
    budgetSource: mockBudgetSources.find(bs => bs.id === (dto.budgetSourceId?.toString() ?? '')) ?? baseTarget.budgetSource,
    budgetType: mockBudgetTypes.find(bt => bt.id === (dto.budgetTypeId?.toString() ?? '')) ?? baseTarget.budgetType,
    unitOfMeasure: mockUnitsOfMeasure.find(uom => uom.id === (dto.unitOfMeasureId?.toString() ?? '')) ?? baseTarget.unitOfMeasure,
    weight: dto.weight,
    kpiType: dto.kpiType,
    indicatorType: dto.indicatorType,
    functionalArea: dto.functionalArea ?? '',
    standardClassification: dto.standardClassification ?? '',
    idpReference: dto.idpReference ?? '',
    internalReference: dto.internalReference ?? '',
    fmsLink: dto.fmsLink ?? '',
    isRevised: dto.isRevised,
    isWithdrawn: dto.isWithdrawn,
    reasonForWithdrawal: dto.reasonForWithdrawal ?? '',
    targetUnitType: dto.targetUnitType as TargetUnitType,
    q1Target: dto.q1Target ?? 0,
    q1Description: dto.q1Description ?? '',
    q1Budget: dto.q1Budget ?? 0,
    q2Target: dto.q2Target ?? 0,
    q2Description: dto.q2Description ?? '',
    q2Budget: dto.q2Budget ?? 0,
    midTermTarget: dto.midTermTarget ?? 0,
    midTermDescription: dto.midTermDescription ?? '',
    midTermBudget: dto.midTermBudget ?? 0,
    q3Target: dto.q3Target ?? 0,
    q3Description: dto.q3Description ?? '',
    q3Budget: dto.q3Budget ?? 0,
    q3RevisedTarget: dto.q3RevisedTarget ?? 0,
    q4Target: dto.q4Target ?? 0,
    q4Description: dto.q4Description ?? '',
    q4Budget: dto.q4Budget ?? 0,
    q4RevisedTarget: dto.q4RevisedTarget ?? 0,
    revisedAnnualTarget: dto.revisedAnnualTarget ?? 0,
    revisedAnnualBudget: dto.revisedAnnualBudget ?? 0,
    createdAt: dto.createdAt as never,
  } as OPMSTarget;
}

function toIpmsTargetModel(dto: IpmsTargetDto): IPMSTarget {
  const baseTarget = mockIPMSTargets[0];
  return {
    ...baseTarget,
    id: dto.id,
    sourceTemplateId: dto.sourceTemplateId ?? undefined,
    sourceTemplateVersion: dto.sourceTemplateVersion ?? undefined,
    relatedOPMSTarget: dto.relatedOpmsTargetId ? mockOPMSTargets.find(target => target.id === dto.relatedOpmsTargetId) : undefined,
    period: mockPeriods.find(p => p.id === (dto.periodId?.toString() ?? '')) ?? baseTarget.period,
    department: pickDepartment(dto.departmentId, dto.departmentName),
    unit: pickUnit(dto.unitId, dto.unitName, dto.departmentId),
    assignedTo: mockEmployees.find(e => e.id === dto.assignedUserId) ?? baseTarget.assignedTo,
    indicatorNumber: dto.indicatorNumber,
    nationalKPA: dto.nationalKpa,
    municipalKPA: dto.municipalKpa,
    strategicGoal: mockStrategicGoals.find(sg => sg.id === (dto.strategicGoalId?.toString() ?? '')) ?? baseTarget.strategicGoal,
    strategicObjective: mockStrategicObjectives.find(so => so.id === (dto.strategicObjectiveId?.toString() ?? '')) ?? baseTarget.strategicObjective,
    performanceObjective: dto.performanceObjective,
    targetName: dto.targetName,
    kpiDescription: dto.kpiDescription,
    baseline: dto.baseline,
    annualTarget: dto.annualTarget,
    annualTargetDescription: dto.annualTargetDescription,
    budgetSource: mockBudgetSources.find(bs => bs.id === (dto.budgetSourceId?.toString() ?? '')) ?? baseTarget.budgetSource,
    budgetType: mockBudgetTypes.find(bt => bt.id === (dto.budgetTypeId?.toString() ?? '')) ?? baseTarget.budgetType,
    unitOfMeasure: mockUnitsOfMeasure.find(uom => uom.id === (dto.unitOfMeasureId?.toString() ?? '')) ?? baseTarget.unitOfMeasure,
    weight: dto.weight,
    kpiType: dto.kpiType,
    indicatorType: dto.indicatorType,
    functionalArea: dto.functionalArea ?? '',
    idpReference: dto.idpReference ?? '',
    internalReference: dto.internalReference ?? '',
    isRevised: dto.isRevised,
    targetUnitType: dto.targetUnitType as TargetUnitType,
    q1Target: dto.q1Target ?? 0,
    q1Description: dto.q1Description ?? '',
    q1Budget: dto.q1Budget ?? 0,
    q2Target: dto.q2Target ?? 0,
    q2Description: dto.q2Description ?? '',
    q2Budget: dto.q2Budget ?? 0,
    midTermTarget: dto.midTermTarget ?? 0,
    midTermDescription: dto.midTermDescription ?? '',
    midTermBudget: dto.midTermBudget ?? 0,
    q3Target: dto.q3Target ?? 0,
    q3Description: dto.q3Description ?? '',
    q3Budget: dto.q3Budget ?? 0,
    q3RevisedTarget: dto.q3RevisedTarget ?? 0,
    q4Target: dto.q4Target ?? 0,
    q4Description: dto.q4Description ?? '',
    q4Budget: dto.q4Budget ?? 0,
    q4RevisedTarget: dto.q4RevisedTarget ?? 0,
    revisedAnnualTarget: dto.revisedAnnualTarget ?? 0,
    revisedAnnualBudget: dto.revisedAnnualBudget ?? 0,
    createdAt: dto.createdAt as never,
  } as IPMSTarget;
}

function toOpmsSubmissionModel(dto: OpmsSubmissionDto, targets: OPMSTarget[]): OPMSSubmission {
  const baseSubmission = mockOPMSSubmissions[0];
  const target = targets.find(item => item.id === dto.opmsTargetId) ?? baseSubmission?.target ?? mockOPMSTargets[0];
  return {
    ...baseSubmission,
    id: dto.id,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? new Date().toISOString(),
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: dto.actual ?? 0,
    actualDescription: dto.actualDescription ?? undefined,
    actualPerformanceDescription: dto.actualPerformanceDescription ?? undefined,
    actualExpenditure: dto.actualExpenditure ?? undefined,
    variance: dto.variance ?? undefined,
    varianceReason: dto.varianceReason ?? undefined,
    correctiveMeasure: dto.correctiveMeasure ?? undefined,
    submitterScore: dto.submitterScore ?? undefined,
    submitterStatus: dto.submitterStatus ?? undefined,
    verifierStatus: dto.verifierStatus ?? undefined,
    approverStatus: dto.approverStatus ?? undefined,
    pmsStatus: dto.pmsStatus ?? undefined,
    auditorStatus: dto.auditorStatus ?? undefined,
    status: coerceSubmissionStatus(dto.status),
    submitter: dto.submittedByUserId ? mockEmployees.find(e => e.id === dto.submittedByUserId) : baseSubmission.submitter,
    submittedAt: dto.submittedAt ?? undefined,
    submittedByUserId: dto.submittedByUserId ?? undefined,
    verifier: dto.verifierUserId ? mockEmployees.find(e => e.id === dto.verifierUserId) : baseSubmission.verifier,
    verifiedAt: dto.verifiedAt ?? undefined,
    verifierComments: dto.verifierComments ?? undefined,
    verifierComment: dto.verifierComment ?? undefined,
    verifierScore: dto.verifierScore ?? undefined,
    approver: dto.approverUserId ? mockEmployees.find(e => e.id === dto.approverUserId) : baseSubmission.approver,
    approvedAt: dto.approvedAt ?? undefined,
    approverComments: dto.approverComments ?? undefined,
    approverComment: dto.approverComment ?? undefined,
    approverScore: dto.approverScore ?? undefined,
    pmsOfficer: dto.pmsOfficerUserId ? mockEmployees.find(e => e.id === dto.pmsOfficerUserId) : baseSubmission.pmsOfficer,
    pmsReviewedAt: dto.pmsReviewedAt ?? undefined,
    pmsComments: dto.pmsComments ?? undefined,
    pmsComment: dto.pmsComment ?? undefined,
    pmsRecommendation: dto.pmsRecommendation ?? undefined,
    pmsScore: dto.pmsScore ?? undefined,
    pmsResponseDueDate: dto.pmsResponseDueDate ?? undefined,
    pmsRfiComment: dto.pmsRfiComment ?? undefined,
    auditor: dto.auditorUserId ? mockEmployees.find(e => e.id === dto.auditorUserId) : baseSubmission.auditor,
    auditedAt: dto.auditedAt ?? undefined,
    auditorComments: dto.auditorComments ?? undefined,
    auditorComment: dto.auditorComment ?? undefined,
    auditorRecommendation: dto.auditorRecommendation ?? undefined,
    auditorScore: dto.auditorScore ?? undefined,
    auditorResponseDueDate: dto.auditorResponseDueDate ?? undefined,
    dueDateExtendedDays: dto.dueDateExtendedDays ?? undefined,
    poeType: dto.poeType ?? undefined,
    isDisabled: dto.isDisabled ?? undefined,
    createdBy: dto.createdBy ?? undefined,
    createdOn: dto.createdOn ?? undefined,
    updatedBy: dto.updatedBy ?? undefined,
    updatedOn: dto.updatedOn ?? undefined,
    organisationId: dto.organisationId ?? undefined,
  };
}

function toIpmsSubmissionModel(dto: IpmsSubmissionDto, targets: IPMSTarget[]): IPMSSubmission {
  const baseSubmission = mockIPMSSubmissions[0];
  const target = targets.find(item => item.id === dto.ipmsTargetId) ?? baseSubmission?.target ?? mockIPMSTargets[0];
  return {
    ...baseSubmission,
    id: dto.id,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? new Date().toISOString(),
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: dto.actual ?? 0,
    actualDescription: dto.actualDescription ?? undefined,
    actualPerformanceDescription: dto.actualPerformanceDescription ?? undefined,
    actualExpenditure: dto.actualExpenditure ?? undefined,
    variance: dto.variance ?? undefined,
    varianceReason: dto.varianceReason ?? undefined,
    correctiveMeasure: dto.correctiveMeasure ?? undefined,
    submitterScore: dto.submitterScore ?? undefined,
    submitterStatus: dto.submitterStatus ?? undefined,
    verifierStatus: dto.verifierStatus ?? undefined,
    approverStatus: dto.approverStatus ?? undefined,
    pmsStatus: dto.pmsStatus ?? undefined,
    auditorStatus: dto.auditorStatus ?? undefined,
    status: coerceSubmissionStatus(dto.status),
    submitter: dto.submittedByUserId ? mockEmployees.find(e => e.id === dto.submittedByUserId) : baseSubmission.submitter,
    submittedAt: dto.submittedAt ?? undefined,
    submittedByUserId: dto.submittedByUserId ?? undefined,
    verifier: dto.verifierUserId ? mockEmployees.find(e => e.id === dto.verifierUserId) : baseSubmission.verifier,
    verifiedAt: dto.verifiedAt ?? undefined,
    verifierComments: dto.verifierComments ?? undefined,
    verifierComment: dto.verifierComment ?? undefined,
    verifierScore: dto.verifierScore ?? undefined,
    approver: dto.approverUserId ? mockEmployees.find(e => e.id === dto.approverUserId) : baseSubmission.approver,
    approvedAt: dto.approvedAt ?? undefined,
    approverComments: dto.approverComments ?? undefined,
    approverComment: dto.approverComment ?? undefined,
    approverScore: dto.approverScore ?? undefined,
    pmsOfficer: dto.pmsOfficerUserId ? mockEmployees.find(e => e.id === dto.pmsOfficerUserId) : baseSubmission.pmsOfficer,
    pmsReviewedAt: dto.pmsReviewedAt ?? undefined,
    pmsComments: dto.pmsComments ?? undefined,
    pmsComment: dto.pmsComment ?? undefined,
    pmsRecommendation: dto.pmsRecommendation ?? undefined,
    pmsScore: dto.pmsScore ?? undefined,
    pmsResponseDueDate: dto.pmsResponseDueDate ?? undefined,
    pmsRfiComment: dto.pmsRfiComment ?? undefined,
    auditor: dto.auditorUserId ? mockEmployees.find(e => e.id === dto.auditorUserId) : baseSubmission.auditor,
    auditedAt: dto.auditedAt ?? undefined,
    auditorComments: dto.auditorComments ?? undefined,
    auditorComment: dto.auditorComment ?? undefined,
    auditorRecommendation: dto.auditorRecommendation ?? undefined,
    auditorScore: dto.auditorScore ?? undefined,
    auditorResponseDueDate: dto.auditorResponseDueDate ?? undefined,
    dueDateExtendedDays: dto.dueDateExtendedDays ?? undefined,
    poeType: dto.poeType ?? undefined,
    isDisabled: dto.isDisabled ?? undefined,
    createdBy: dto.createdBy ?? undefined,
    createdOn: dto.createdOn ?? undefined,
    updatedBy: dto.updatedBy ?? undefined,
    updatedOn: dto.updatedOn ?? undefined,
    organisationId: dto.organisationId ?? undefined,
  };
}

function toAttachmentModel(dto: PoeFileDto) {
  const uploadedBy = mockEmployees.find(employee => employee.id === dto.uploadedByUserId)
    ?? mockEmployees.find(employee => employee.displayName === dto.uploadedByName)
    ?? mockEmployees[0];

  return {
    id: dto.id,
    fileName: dto.fileName,
    fileSize: dto.sizeInBytes,
    fileType: dto.contentType ?? 'application/octet-stream',
    uploadedBy,
    uploadedAt: dto.uploadedAt,
    documentType: 'evidence',
    url: dto.url,
  };
}

function toOpmsTemplatePayload(template: SaveOpmsTargetTemplatePayload): SaveOpmsTargetTemplatePayload {
  return {
    ...template,
    annualTargetDescription: template.annualTargetDescription ?? null,
    unitOfMeasure: template.unitOfMeasure ?? null,
    nationalKpa: template.nationalKpa ?? null,
    municipalKpa: template.municipalKpa ?? null,
    strategicGoal: template.strategicGoal ?? null,
    strategicObjective: template.strategicObjective ?? null,
    performanceObjective: template.performanceObjective ?? null,
    outcome: template.outcome ?? null,
    output: template.output ?? null,
    priorityIssue: template.priorityIssue ?? null,
    budgetSource: template.budgetSource ?? null,
    budgetType: template.budgetType ?? null,
    kpiType: template.kpiType ?? null,
    indicatorType: template.indicatorType ?? null,
    functionalArea: template.functionalArea ?? null,
    standardClassification: template.standardClassification ?? null,
    idpReference: template.idpReference ?? null,
    internalReference: template.internalReference ?? null,
    fmsLink: template.fmsLink ?? null,
    defaultQuarterlyTargetsJson: template.defaultQuarterlyTargetsJson ?? null,
    defaultBudgetInformation: template.defaultBudgetInformation ?? null,
    defaultPoeRequirements: template.defaultPoeRequirements ?? null,
  };
}

function toIpmsTemplatePayload(template: SaveIpmsTargetTemplatePayload): SaveIpmsTargetTemplatePayload {
  return {
    ...template,
    performanceArea: template.performanceArea ?? null,
    employeeLevel: template.employeeLevel ?? null,
    jobGrade: template.jobGrade ?? null,
    unitOfMeasure: template.unitOfMeasure ?? null,
    annualTargetDescription: template.annualTargetDescription ?? null,
    defaultRatingMethod: template.defaultRatingMethod ?? null,
    defaultScoreScale: template.defaultScoreScale ?? null,
    defaultPoeRequirements: template.defaultPoeRequirements ?? null,
    defaultTaskTemplatesJson: template.defaultTaskTemplatesJson ?? null,
    functionalArea: template.functionalArea ?? null,
  };
}

async function fetchApi<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const isFormDataBody = typeof FormData !== 'undefined' && options.body instanceof FormData;
  const headers: Record<string, string> = {
    ...(isFormDataBody ? {} : { 'Content-Type': 'application/json' }),
    ...(options.headers as Record<string, string>),
  };

  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && refreshToken) {
    try {
      const refreshResult = await refreshAccessToken();
      if (refreshResult.success) {
        // Retry the original request with new token
        const retryHeaders: Record<string, string> = {
          ...(isFormDataBody ? {} : { 'Content-Type': 'application/json' }),
          ...(options.headers as Record<string, string>),
          'Authorization': `Bearer ${accessToken}`,
        };
        const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
          ...options,
          headers: retryHeaders,
        });
        return await retryResponse.json();
      }
    } catch {
      clearTokens();
      window.location.href = '/login';
    }
  }

  return await response.json();
}

async function get<T>(endpoint: string): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'GET' });
}

async function post<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function put<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function patch<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'PATCH', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function del<T>(endpoint: string): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'DELETE' });
}

async function postForm<T>(endpoint: string, body: FormData): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'POST', body });
}

async function refreshAccessToken(): Promise<ApiResponse<LoginResponse>> {
  const response = await fetch(`${API_BASE_URL}/auth/refresh-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ accessToken, refreshToken } as RefreshTokenRequest),
  });
  const data = await response.json();
  if (data.success && data.data) {
    setTokens(data.data.accessToken, data.data.refreshToken);
  }
  return data;
}

export async function login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  const result = await fetchApi<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  });
  if (result.success && result.data) {
    setTokens(result.data.accessToken, result.data.refreshToken);
  }
  return result;
}

export async function register(data: RegisterRequest): Promise<ApiResponse<boolean>> {
  return await fetchApi<boolean>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function getDemoUsers(): Promise<ApiResponse<DemoUser[]>> {
  return get<DemoUser[]>('/auth/demo-users');
}

export async function logout() {
  clearTokens();
}

export function isAuthenticated() {
  return !!accessToken;
}

export { setTokens, clearTokens };

export async function getMyMenu(): Promise<ApiResponse<MenuItem[]>> {
  return get<MenuItem[]>('/navigation/my-menu');
}

export async function getMyPermissions(): Promise<ApiResponse<string[]>> {
  return get<string[]>('/access/my-permissions');
}

export async function getUsers(): Promise<ApiResponse<AdminUserDetail[]>> {
  return get<AdminUserDetail[]>('/users');
}

export async function getUser(id: string): Promise<ApiResponse<AdminUserDetail>> {
  return get<AdminUserDetail>(`/users/${id}`);
}

export async function createUser(payload: { firstName: string; lastName: string; email: string; password: string; phoneNumber?: string }): Promise<ApiResponse<AdminUserDetail>> {
  return post<AdminUserDetail>('/users', payload);
}

export async function updateUser(payload: { id: string; firstName: string; lastName: string; phoneNumber?: string; isActive: boolean }): Promise<ApiResponse<AdminUserDetail>> {
  return put<AdminUserDetail>(`/users/${payload.id}`, {
    firstName: payload.firstName,
    lastName: payload.lastName,
    phoneNumber: payload.phoneNumber ?? null,
    isActive: payload.isActive,
  });
}

export async function activateUser(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/users/${id}/activate`);
}

export async function deactivateUser(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/users/${id}/deactivate`);
}

export async function deleteUser(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/users/${id}`);
}

export async function setUserRoles(userId: string, roleIds: string[]): Promise<ApiResponse<boolean>> {
  return post<boolean>(`/users/${userId}/roles`, { roleIds });
}

export async function getUserPermissions(userId: string): Promise<ApiResponse<UserPermissions>> {
  return get<UserPermissions>(`/users/${userId}/permissions`);
}

export async function setUserPermissionOverrides(userId: string, overrides: UserPermissionOverride[]): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/users/${userId}/permission-overrides`, { overrides });
}

export async function getRoles(): Promise<ApiResponse<AdminRole[]>> {
  return get<AdminRole[]>('/roles');
}

export async function createRole(payload: { name: string; description?: string }): Promise<ApiResponse<AdminRole>> {
  return post<AdminRole>('/roles', payload);
}

export async function updateRole(payload: { id: string; name: string; description?: string }): Promise<ApiResponse<AdminRole>> {
  return put<AdminRole>(`/roles/${payload.id}`, { name: payload.name, description: payload.description ?? null });
}

export async function deleteRole(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/roles/${id}`);
}

export async function getRolePermissions(roleId: string): Promise<ApiResponse<RolePermission[]>> {
  return get<RolePermission[]>(`/roles/${roleId}/permissions`);
}

export async function setRolePermissions(roleId: string, permissionIds: number[]): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/roles/${roleId}/permissions`, { permissionIds });
}

export async function getPermissions(): Promise<ApiResponse<AdminPermission[]>> {
  return get<AdminPermission[]>('/permissions');
}

export async function getPermissionsGrouped(): Promise<ApiResponse<AdminPermissionGroup[]>> {
  return get<AdminPermissionGroup[]>('/permissions/grouped');
}

export async function createPermission(payload: Omit<AdminPermission, 'id'>): Promise<ApiResponse<AdminPermission>> {
  return post<AdminPermission>('/permissions', payload);
}

export async function updatePermission(payload: AdminPermission): Promise<ApiResponse<AdminPermission>> {
  const { id, ...rest } = payload;
  return put<AdminPermission>(`/permissions/${id}`, rest);
}

export async function deletePermission(id: number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/permissions/${id}`);
}

export async function getLoginAuditLogs(take = 200): Promise<ApiResponse<LoginAuditLog[]>> {
  return get<LoginAuditLog[]>(`/audit/login-logs?take=${take}`);
}

export async function getRoleImplementationAudit(): Promise<ApiResponse<RoleImplementationAuditRow[]>> {
  return get<RoleImplementationAuditRow[]>('/role-implementation-audit');
}

export async function getRoleAccessMatrix(): Promise<ApiResponse<RoleAccessMatrixRow[]>> {
  return get<RoleAccessMatrixRow[]>('/access/role-access-matrix');
}

export async function getSystemCoverageAudit(): Promise<ApiResponse<SystemCoverageAuditRow[]>> {
  return get<SystemCoverageAuditRow[]>('/access/system-coverage-audit');
}

export async function simulateAccess(payload: {
  userId: string;
  role?: string;
  departmentId?: number | null;
  unitId?: number | null;
  targetId?: string | null;
  kpiId?: string | null;
  projectId?: string | null;
  taskId?: string | null;
  permissionCode: string;
}): Promise<ApiResponse<AccessSimulationResult>> {
  return post<AccessSimulationResult>('/access/simulate', payload);
}

export async function getOpmsTargetTemplates(): Promise<ApiResponse<OpmsTargetTemplate[]>> {
  const response = await get<OpmsTargetTemplateDto[]>('/opms-target-library');
  return mapResponse(response, items => items.map(toOpmsTemplateModel));
}

export async function getOpmsTargetTemplate(id: string | number): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await get<OpmsTargetTemplateDto>(`/opms-target-library/${id}`);
  return mapResponse(response, toOpmsTemplateModel);
}

export async function createOpmsTargetTemplate(payload: SaveOpmsTargetTemplatePayload): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await post<OpmsTargetTemplateDto>('/opms-target-library', toOpmsTemplatePayload(payload));
  return mapResponse(response, toOpmsTemplateModel);
}

export async function updateOpmsTargetTemplate(id: string | number, payload: SaveOpmsTargetTemplatePayload): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await put<OpmsTargetTemplateDto>(`/opms-target-library/${id}`, toOpmsTemplatePayload(payload));
  return mapResponse(response, toOpmsTemplateModel);
}

export async function archiveOpmsTargetTemplate(id: string | number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/opms-target-library/${id}`);
}

export async function duplicateOpmsTargetTemplate(id: string | number): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await post<OpmsTargetTemplateDto>(`/opms-target-library/${id}/duplicate`);
  return mapResponse(response, toOpmsTemplateModel);
}

export async function getIpmsTargetTemplates(): Promise<ApiResponse<IpmsTargetTemplate[]>> {
  const response = await get<IpmsTargetTemplateDto[]>('/ipms-target-library');
  return mapResponse(response, items => items.map(toIpmsTemplateModel));
}

export async function getIpmsTargetTemplate(id: string | number): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await get<IpmsTargetTemplateDto>(`/ipms-target-library/${id}`);
  return mapResponse(response, toIpmsTemplateModel);
}

export async function createIpmsTargetTemplate(payload: SaveIpmsTargetTemplatePayload): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await post<IpmsTargetTemplateDto>('/ipms-target-library', toIpmsTemplatePayload(payload));
  return mapResponse(response, toIpmsTemplateModel);
}

export async function updateIpmsTargetTemplate(id: string | number, payload: SaveIpmsTargetTemplatePayload): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await put<IpmsTargetTemplateDto>(`/ipms-target-library/${id}`, toIpmsTemplatePayload(payload));
  return mapResponse(response, toIpmsTemplateModel);
}

export async function archiveIpmsTargetTemplate(id: string | number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/ipms-target-library/${id}`);
}

export async function duplicateIpmsTargetTemplate(id: string | number): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await post<IpmsTargetTemplateDto>(`/ipms-target-library/${id}/duplicate`);
  return mapResponse(response, toIpmsTemplateModel);
}

export async function getOpmsTargets(): Promise<ApiResponse<OPMSTarget[]>> {
  const response = await get<OpmsTargetDto[]>('/opms-targets');
  return mapResponse(response, items => items.map(toOpmsTargetModel));
}

export async function getOpmsTarget(id: string): Promise<ApiResponse<OPMSTarget>> {
  const response = await get<OpmsTargetDto>(`/opms-targets/${id}`);
  return mapResponse(response, toOpmsTargetModel);
}

export async function createOpmsTarget(payload: SaveOpmsTargetPayload): Promise<ApiResponse<OPMSTarget>> {
  const response = await post<OpmsTargetDto>('/opms-targets', payload);
  return mapResponse(response, toOpmsTargetModel);
}

export async function updateOpmsTarget(id: string, payload: SaveOpmsTargetPayload): Promise<ApiResponse<OPMSTarget>> {
  const response = await put<OpmsTargetDto>(`/opms-targets/${id}`, payload);
  return mapResponse(response, toOpmsTargetModel);
}

export async function deleteOpmsTarget(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/opms-targets/${id}`);
}

export async function getIpmsTargets(): Promise<ApiResponse<IPMSTarget[]>> {
  const response = await get<IpmsTargetDto[]>('/ipms-targets');
  return mapResponse(response, items => items.map(toIpmsTargetModel));
}

export async function getIpmsTarget(id: string): Promise<ApiResponse<IPMSTarget>> {
  const response = await get<IpmsTargetDto>(`/ipms-targets/${id}`);
  return mapResponse(response, toIpmsTargetModel);
}

export async function createIpmsTarget(payload: SaveIpmsTargetPayload): Promise<ApiResponse<IPMSTarget>> {
  const response = await post<IpmsTargetDto>('/ipms-targets', payload);
  return mapResponse(response, toIpmsTargetModel);
}

export async function updateIpmsTarget(id: string, payload: SaveIpmsTargetPayload): Promise<ApiResponse<IPMSTarget>> {
  const response = await put<IpmsTargetDto>(`/ipms-targets/${id}`, payload);
  return mapResponse(response, toIpmsTargetModel);
}

export async function deleteIpmsTarget(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/ipms-targets/${id}`);
}

export async function getOpmsSubmissions(): Promise<ApiResponse<OPMSSubmission[]>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await get<OpmsSubmissionDto[]>('/opms-submissions');
  return mapResponse(response, items => items.map(item => toOpmsSubmissionModel(item, targets)));
}

export async function getOpmsSubmission(id: string): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await get<OpmsSubmissionDto>(`/opms-submissions/${id}`);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targets));
}

export async function createOpmsSubmission(payload: SaveOpmsSubmissionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<OpmsSubmissionDto>('/opms-submissions', payload);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targets));
}

export async function updateOpmsSubmission(id: string, payload: SaveOpmsSubmissionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await put<OpmsSubmissionDto>(`/opms-submissions/${id}`, payload);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targets));
}

export async function deleteOpmsSubmission(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/opms-submissions/${id}`);
}

export async function applyOpmsSubmissionWorkflowAction(
  id: string,
  action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score',
  payload: SubmissionWorkflowActionPayload,
): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<OpmsSubmissionDto>(`/opms-submissions/${id}/${action}`, payload);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targets));
}

export async function extendOpmsSubmissionDueDate(id: string, payload: DueDateExtensionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<OpmsSubmissionDto>(`/opms-submissions/${id}/extend-due-date`, payload);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targets));
}

export async function getOpmsSubmissionAttachments(id: string) {
  const response = await get<PoeFileDto[]>(`/opms-submissions/${id}/attachments`);
  return mapResponse(response, items => items.map(toAttachmentModel));
}

export async function uploadOpmsSubmissionAttachment(id: string, file: File) {
  const formData = new FormData();
  formData.append('file', file);
  const response = await postForm<PoeFileDto>(`/opms-submissions/${id}/attachments`, formData);
  return mapResponse(response, toAttachmentModel);
}

export async function deleteOpmsSubmissionAttachment(id: string, attachmentId: string) {
  return del<boolean>(`/opms-submissions/${id}/attachments/${attachmentId}`);
}

export async function getIpmsSubmissions(): Promise<ApiResponse<IPMSSubmission[]>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await get<IpmsSubmissionDto[]>('/ipms-submissions');
  return mapResponse(response, items => items.map(item => toIpmsSubmissionModel(item, targets)));
}

export async function getIpmsSubmission(id: string): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await get<IpmsSubmissionDto>(`/ipms-submissions/${id}`);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targets));
}

export async function createIpmsSubmission(payload: SaveIpmsSubmissionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<IpmsSubmissionDto>('/ipms-submissions', payload);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targets));
}

export async function updateIpmsSubmission(id: string, payload: SaveIpmsSubmissionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await put<IpmsSubmissionDto>(`/ipms-submissions/${id}`, payload);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targets));
}

export async function deleteIpmsSubmission(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/ipms-submissions/${id}`);
}

export async function applyIpmsSubmissionWorkflowAction(
  id: string,
  action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score',
  payload: SubmissionWorkflowActionPayload,
): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<IpmsSubmissionDto>(`/ipms-submissions/${id}/${action}`, payload);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targets));
}

export async function extendIpmsSubmissionDueDate(id: string, payload: DueDateExtensionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const targets = targetsResult.data ?? [];
  const response = await post<IpmsSubmissionDto>(`/ipms-submissions/${id}/extend-due-date`, payload);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targets));
}

export async function getIpmsSubmissionAttachments(id: string) {
  const response = await get<PoeFileDto[]>(`/ipms-submissions/${id}/attachments`);
  return mapResponse(response, items => items.map(toAttachmentModel));
}

export async function uploadIpmsSubmissionAttachment(id: string, file: File) {
  const formData = new FormData();
  formData.append('file', file);
  const response = await postForm<PoeFileDto>(`/ipms-submissions/${id}/attachments`, formData);
  return mapResponse(response, toAttachmentModel);
}

export async function deleteIpmsSubmissionAttachment(id: string, attachmentId: string) {
  return del<boolean>(`/ipms-submissions/${id}/attachments/${attachmentId}`);
}

export async function getNotifications(includeAll = false): Promise<ApiResponse<NotificationDto[]>> {
  const suffix = includeAll ? '?includeAll=true' : '';
  return get<NotificationDto[]>(`/notifications${suffix}`);
}

export async function markNotificationRead(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/notifications/${id}/read`);
}

export async function getAuditTrails(take = 200): Promise<ApiResponse<AuditTrailEntryDto[]>> {
  return get<AuditTrailEntryDto[]>(`/audit/trails?take=${take}`);
}

export async function getIdpPlans(): Promise<ApiResponse<IdpPlanSummary[]>> {
  return get<IdpPlanSummary[]>('/idp/plans');
}

export async function createIdpPlan(payload: CreateIdpPlanPayload): Promise<ApiResponse<IdpPlanSummary>> {
  return post<IdpPlanSummary>('/idp/plans', payload);
}

export async function createIdpPlanVersion(planId: number, payload: CreateIdpPlanVersionPayload): Promise<ApiResponse<IdpPlanVersion>> {
  return post<IdpPlanVersion>(`/idp/plans/${planId}/versions`, payload);
}

export async function getIdpPlanHierarchy(planId: number): Promise<ApiResponse<IdpHierarchy>> {
  return get<IdpHierarchy>(`/idp/plans/${planId}/hierarchy`);
}

export async function getIdpHierarchy(planId: number): Promise<ApiResponse<IdpHierarchy>> {
  return getIdpPlanHierarchy(planId);
}

export async function getIdpDashboard(planId: number): Promise<ApiResponse<IdpDashboard>> {
  return get<IdpDashboard>(`/idp/plans/${planId}/dashboard`);
}

export async function getIdpAlignmentMatrix(planId: number): Promise<ApiResponse<IdpAlignmentMatrixItem[]>> {
  return get<IdpAlignmentMatrixItem[]>(`/idp/plans/${planId}/alignment-matrix`);
}

export async function createIdpComment(payload: CreateIdpCommentPayload): Promise<ApiResponse<boolean>> {
  return mapResponse(await post<{ id: number }>('/idp/comments', payload), () => true);
}

export async function createIdpCommunitySession(payload: CreateIdpCommunitySessionPayload): Promise<ApiResponse<boolean>> {
  return mapResponse(await post<{ id: number }>('/idp/community-sessions', payload), () => true);
}

export async function getIdpReport(planId: number, reportType: string, format: 'pdf' | 'excel' | 'word'): Promise<ApiResponse<IdpReportDocument>> {
  return get<IdpReportDocument>(`/idp/plans/${planId}/reports/${reportType}?format=${format}`);
}

export async function requestIdpReport(planId: number, reportType: string, format: 'pdf' | 'excel' | 'word'): Promise<ApiResponse<IdpReportDocument>> {
  return getIdpReport(planId, reportType, format);
}
