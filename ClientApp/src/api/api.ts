import type {
  ApiResponse,
  AuditTrailEntryDto,
  LoginRequest,
  LoginResponse,
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
  SecurityPermissionDefinition,
  RoleSecurityConfiguration,
  RoleSecurityPermission,
  EffectiveSecurityPreview,
  SecurityRoleSummary,
  SecurityUserSummary,
  SecurityUserRoleConfiguration,
  TenantContextDto,
  ReportingPeriodMasterDto,
  FinancialYearMasterDto,
  MunicipalityFinancialYearMasterDto,
  MunicipalEmployeeDto,
  EmployeeAssignmentMasterDto,
  AuthSessionDto,
  MfaStatusDto,
  MfaSetupDto,
  MfaEnableDto,
  DepartmentMasterDto,
  UnitMasterDto,
  PositionMasterDto,
  WorkflowDefinitionDto,
  WorkflowDefinitionComparisonDto,
  ReportingWindowDto,
  RatingSchemeDto,
  PerformanceReportSummaryDto,
  PerformancePeriodTargetDto,
  PerformanceTargetRevisionDto,
  ReportingWindowExceptionDto,
  DepartmentLookupDto,
  UnitLookupDto,
  PerformanceRfiDto,
  StageRatingDto,
  SecurityNavigationItemDto,
  NotificationOutboxItemDto,
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
  mockWards,
  mockVoteNumbers,
} from '../data/mockData';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

const SESSION_MARKER_KEY = 'auth_session';
const TENANT_STORAGE_KEY = 'municipality_context_id';
let refreshPromise: Promise<ApiResponse<LoginResponse>> | null = null;
sessionStorage.removeItem('auth_token');

export function getCurrentMunicipalityId(): number | null {
  const value = sessionStorage.getItem(TENANT_STORAGE_KEY);
  if (!value) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}

export function setCurrentMunicipalityId(municipalityId: number | null) {
  if (municipalityId === null) sessionStorage.removeItem(TENANT_STORAGE_KEY);
  else sessionStorage.setItem(TENANT_STORAGE_KEY, String(municipalityId));
}

function addTenantHeader(headers: Record<string, string>) {
  const municipalityId = getCurrentMunicipalityId();
  if (municipalityId !== null) headers['X-Municipality-Id'] = String(municipalityId);
  return headers;
}

function markSessionEstablished() {
  sessionStorage.removeItem('auth_token');
  sessionStorage.setItem(SESSION_MARKER_KEY, '1');
}

function clearTokens() {
  sessionStorage.removeItem(SESSION_MARKER_KEY);
  // Remove legacy browser-readable tokens left by older deployments.
  sessionStorage.removeItem('auth_token');
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
    publicId: dto.publicId,
    rowVersion: dto.rowVersion,
    sourceTemplateId: dto.sourceTemplateId ?? undefined,
    sourceTemplateVersion: dto.sourceTemplateVersion ?? undefined,
    period: mockPeriods.find(p => p.id === (dto.periodId?.toString() ?? '')) ?? baseTarget.period,
    department: pickDepartment(dto.departmentId, dto.departmentName),
    unit: pickUnit(dto.unitId, dto.unitName, dto.departmentId),
    assignedTo: mockEmployees.find(e => e.id === dto.assignedUserId) ?? baseTarget.assignedTo,
    wards: dto.wardIds.map(id => mockWards.find(ward => ward.id === String(id)) ?? { id: String(id), code: String(id), name: `Ward ${id}`, municipality: '', isActive: true }),
    wardIds: dto.wardIds,
    additionalAssignees: dto.additionalAssigneeIds.map(id => mockEmployees.find(employee => employee.id === id)).filter((employee): employee is NonNullable<typeof employee> => Boolean(employee)),
    additionalAssigneeIds: dto.additionalAssigneeIds,
    voteNumbers: dto.voteNumberIds.map(id => mockVoteNumbers.find(vote => vote.id === String(id)) ?? { id: String(id), code: String(id), number: String(id), name: `Vote ${id}`, amount: 0, isActive: true }),
    voteNumberIds: dto.voteNumberIds,
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
    withdrawnAt: dto.withdrawnAt ?? undefined,
    withdrawnByUserId: dto.withdrawnByUserId ?? undefined,
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
    publicId: dto.publicId,
    rowVersion: dto.rowVersion,
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
    isWithdrawn: dto.isWithdrawn,
    reasonForWithdrawal: dto.reasonForWithdrawal ?? '',
    withdrawnAt: dto.withdrawnAt ?? undefined,
    withdrawnByUserId: dto.withdrawnByUserId ?? undefined,
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
    rowVersion: dto.rowVersion,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? new Date().toISOString(),
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: dto.actual ?? 0,
    actualPerformance: dto.actualPerformance ?? undefined,
    achievementPercent: dto.achievementPercent ?? undefined,
    targetAchieved: dto.targetAchieved ?? undefined,
    reportingPeriodPublicId: dto.reportingPeriodPublicId ?? undefined,
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
    withdrawalReason: dto.withdrawalReason ?? undefined,
    withdrawnAt: dto.withdrawnAt ?? undefined,
    withdrawnByUserId: dto.withdrawnByUserId ?? undefined,
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
    rowVersion: dto.rowVersion,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? new Date().toISOString(),
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: dto.actual ?? 0,
    actualPerformance: dto.actualPerformance ?? undefined,
    achievementPercent: dto.achievementPercent ?? undefined,
    targetAchieved: dto.targetAchieved ?? undefined,
    reportingPeriodPublicId: dto.reportingPeriodPublicId ?? undefined,
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
    withdrawalReason: dto.withdrawalReason ?? undefined,
    withdrawnAt: dto.withdrawnAt ?? undefined,
    withdrawnByUserId: dto.withdrawnByUserId ?? undefined,
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
    publicId: dto.publicId,
    evidenceBlobPublicId: dto.evidenceBlobPublicId,
    fileName: dto.fileName,
    fileSize: dto.sizeInBytes,
    fileType: dto.contentType ?? 'application/octet-stream',
    uploadedBy,
    uploadedAt: dto.uploadedAt,
    documentType: 'evidence',
    url: dto.url,
    scanStatus: dto.scanStatus,
    isQuarantined: dto.isQuarantined,
    scanDetail: dto.scanDetail ?? undefined,
    assessments: dto.assessments ?? [],
    rowVersion: dto.rowVersion,
    replacementOf: dto.replacementOf ?? undefined,
    replacedBy: dto.replacedBy ?? undefined,
    legalHolds: dto.legalHolds ?? [],
    isActive: dto.isActive,
    retainUntil: dto.retainUntil,
    disposals: dto.disposals ?? [],
    isContentDeleted: dto.isContentDeleted,
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

  addTenantHeader(headers);

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
    credentials: 'include',
  });

  if (response.status === 401 && endpoint !== '/auth/refresh-token' && endpoint !== '/auth/login') {
    try {
      const refreshResult = await refreshAccessToken();
      if (refreshResult.success) {
        const retryHeaders: Record<string, string> = {
          ...(isFormDataBody ? {} : { 'Content-Type': 'application/json' }),
          ...(options.headers as Record<string, string>),
        };
        addTenantHeader(retryHeaders);
        const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
          ...options,
          headers: retryHeaders,
          credentials: 'include',
        });
        return await readApiResponse<T>(retryResponse);
      }
      clearTokens();
    } catch {
      clearTokens();
      window.location.href = '/login';
    }
  }

  return await readApiResponse<T>(response);
}

async function readApiResponse<T>(response: Response): Promise<ApiResponse<T>> {
  const correlationId = response.headers.get('X-Correlation-ID');
  let payload: unknown;
  try {
    payload = await response.json();
  } catch {
    return { success: false, message: `The server returned ${response.status} without a valid response.${correlationId ? ` Correlation ID: ${correlationId}` : ''}` };
  }
  if (payload && typeof payload === 'object' && 'success' in payload && typeof (payload as { success?: unknown }).success === 'boolean') return payload as ApiResponse<T>;
  const problem = payload as { title?: string; detail?: string; errors?: Record<string, string[]>; correlationId?: string };
  const errors = problem.errors ? Object.values(problem.errors).flat() : undefined;
  return {
    success: response.ok,
    data: response.ok ? payload as T : undefined,
    message: problem.detail ?? problem.title ?? `Request failed with status ${response.status}.${correlationId ? ` Correlation ID: ${correlationId}` : ''}`,
    errors,
  };
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
  if (!refreshPromise) {
    refreshPromise = (async () => {
      const response = await fetch(`${API_BASE_URL}/auth/refresh-token`, {
        method: 'POST',
        headers: addTenantHeader({ 'Content-Type': 'application/json' }),
        credentials: 'include',
      });
      const data = await readApiResponse<LoginResponse>(response);
      if (data.success && data.data) markSessionEstablished();
      else clearTokens();
      return data;
    })().finally(() => { refreshPromise = null; });
  }
  return refreshPromise;
}

export async function login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  const result = await fetchApi<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  });
  if (result.success && result.data) {
    markSessionEstablished();
  }
  return result;
}

export async function register(data: RegisterRequest): Promise<ApiResponse<boolean>> {
  return await fetchApi<boolean>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function logout() {
  try {
    await fetchApi<boolean>('/auth/logout', { method: 'POST' });
  } finally {
    clearTokens();
    setCurrentMunicipalityId(null);
  }
}

export async function getAuthSessions(): Promise<ApiResponse<AuthSessionDto[]>> {
  return get<AuthSessionDto[]>('/v1/auth/sessions');
}

export async function revokeAuthSession(sessionId: string, reason: string): Promise<ApiResponse<boolean>> {
  return post<boolean>(`/v1/auth/sessions/${encodeURIComponent(sessionId)}/revoke`, { reason });
}

export async function revokeAllAuthSessions(reason: string): Promise<ApiResponse<number>> {
  return post<number>('/v1/auth/sessions/revoke-all', { reason });
}

export async function getMfaStatus(): Promise<ApiResponse<MfaStatusDto>> {
  return get<MfaStatusDto>('/v1/auth/mfa/status');
}

export async function setupMfa(): Promise<ApiResponse<MfaSetupDto>> {
  return post<MfaSetupDto>('/v1/auth/mfa/setup');
}

export async function enableMfa(code: string): Promise<ApiResponse<MfaEnableDto>> {
  return post<MfaEnableDto>('/v1/auth/mfa/enable', { code });
}

export async function disableMfa(password: string, code?: string, recoveryCode?: string): Promise<ApiResponse<boolean>> {
  return post<boolean>('/v1/auth/mfa/disable', { password, code, recoveryCode });
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<ApiResponse<boolean>> {
  return post<boolean>('/v1/auth/password/change', { currentPassword, newPassword });
}

export async function requestPasswordReset(email: string): Promise<ApiResponse<boolean>> {
  return post<boolean>('/v1/auth/password/forgot', { email });
}

export async function resetPassword(email: string, token: string, newPassword: string): Promise<ApiResponse<boolean>> {
  return post<boolean>('/v1/auth/password/reset', { email, token, newPassword });
}

export function isAuthenticated() {
  return sessionStorage.getItem(SESSION_MARKER_KEY) === '1';
}

export { clearTokens };

export async function getMyMenu(): Promise<ApiResponse<MenuItem[]>> {
  return get<MenuItem[]>('/navigation/my-menu');
}

export async function getMyPermissions(): Promise<ApiResponse<string[]>> {
  return get<string[]>('/access/my-permissions');
}

export async function getMyTenantContexts(): Promise<ApiResponse<TenantContextDto[]>> {
  return get<TenantContextDto[]>('/v1/tenancy/my-contexts');
}

export async function getReportingPeriodMasters(): Promise<ApiResponse<ReportingPeriodMasterDto[]>> {
  return get<ReportingPeriodMasterDto[]>('/v1/masters/reporting-periods');
}

export async function getFinancialYearMasters(): Promise<ApiResponse<FinancialYearMasterDto[]>> {
  return get<FinancialYearMasterDto[]>('/v1/masters/financial-years');
}

export async function createFinancialYearMaster(payload: { code: string; name: string; startDate: string; endDate: string }): Promise<ApiResponse<FinancialYearMasterDto>> {
  return post<FinancialYearMasterDto>('/v1/masters/financial-years', payload);
}

export async function getMunicipalityFinancialYearMasters(): Promise<ApiResponse<MunicipalityFinancialYearMasterDto[]>> {
  return get<MunicipalityFinancialYearMasterDto[]>('/v1/masters/municipality-financial-years');
}

export async function createMunicipalityFinancialYearMaster(payload: { financialYearPublicId: string; isCurrent: boolean; effectiveFrom: string; effectiveTo?: string | null }): Promise<ApiResponse<MunicipalityFinancialYearMasterDto>> {
  return post<MunicipalityFinancialYearMasterDto>('/v1/masters/municipality-financial-years', payload);
}

export async function updateMunicipalityFinancialYearMaster(publicId: string, payload: { isCurrent: boolean; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string }): Promise<ApiResponse<MunicipalityFinancialYearMasterDto>> {
  return put<MunicipalityFinancialYearMasterDto>(`/v1/masters/municipality-financial-years/${publicId}`, payload);
}

export async function createReportingPeriodMaster(payload: { municipalityFinancialYearPublicId: string; code: string; name: string; periodType: number; sequence: number; startDate: string; endDate: string }): Promise<ApiResponse<ReportingPeriodMasterDto>> {
  return post<ReportingPeriodMasterDto>('/v1/masters/reporting-periods', payload);
}

export async function getMunicipalEmployees(): Promise<ApiResponse<MunicipalEmployeeDto[]>> {
  return get<MunicipalEmployeeDto[]>('/v1/masters/employees');
}

export async function createMunicipalEmployee(payload: { employeeNumber: string; firstName: string; lastName: string; emailAddress?: string | null; identityUserId?: string | null; effectiveFrom: string; effectiveTo?: string | null }): Promise<ApiResponse<MunicipalEmployeeDto>> {
  return post<MunicipalEmployeeDto>('/v1/masters/employees', payload);
}

export async function updateMunicipalEmployee(publicId: string, payload: { firstName: string; lastName: string; emailAddress?: string | null; identityUserId?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string }): Promise<ApiResponse<MunicipalEmployeeDto>> {
  return put<MunicipalEmployeeDto>(`/v1/masters/employees/${publicId}`, payload);
}

export async function getEmployeeAssignments(employeePublicId: string): Promise<ApiResponse<EmployeeAssignmentMasterDto[]>> {
  return get<EmployeeAssignmentMasterDto[]>(`/v1/masters/employees/${employeePublicId}/assignments`);
}

export async function createEmployeeAssignment(payload: { employeePublicId: string; departmentPublicId: string; unitPublicId?: string | null; positionPublicId?: string | null; positionCode?: string | null; positionName?: string | null; effectiveFrom: string; effectiveTo?: string | null; isPrimary: boolean }): Promise<ApiResponse<EmployeeAssignmentMasterDto>> {
  return post<EmployeeAssignmentMasterDto>('/v1/masters/employee-assignments', payload);
}

export async function getDepartmentMasters(): Promise<ApiResponse<DepartmentMasterDto[]>> {
  return get<DepartmentMasterDto[]>('/v1/masters/departments');
}

export async function saveDepartmentMaster(publicId: string | null, payload: { code: string; name: string; description?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<DepartmentMasterDto>> {
  return publicId ? put<DepartmentMasterDto>(`/v1/masters/departments/${publicId}`, payload) : post<DepartmentMasterDto>('/v1/masters/departments', payload);
}

export async function getUnitMasters(): Promise<ApiResponse<UnitMasterDto[]>> {
  return get<UnitMasterDto[]>('/v1/masters/units');
}

export async function saveUnitMaster(publicId: string | null, payload: { departmentPublicId: string; code: string; name: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<UnitMasterDto>> {
  return publicId ? put<UnitMasterDto>(`/v1/masters/units/${publicId}`, payload) : post<UnitMasterDto>('/v1/masters/units', payload);
}

export async function getPositionMasters(): Promise<ApiResponse<PositionMasterDto[]>> {
  return get<PositionMasterDto[]>('/v1/masters/positions');
}

export async function savePositionMaster(publicId: string | null, payload: { departmentPublicId: string; unitPublicId?: string | null; code: string; name: string; grade?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<PositionMasterDto>> {
  return publicId ? put<PositionMasterDto>(`/v1/masters/positions/${publicId}`, payload) : post<PositionMasterDto>('/v1/masters/positions', payload);
}

export async function getWardMasters(): Promise<ApiResponse<import('../types').WardMasterDto[]>> {
  return get<import('../types').WardMasterDto[]>('/v1/masters/wards');
}

export async function saveWardMaster(publicId: string | null, payload: { code: string; name: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<import('../types').WardMasterDto>> {
  return publicId ? put<import('../types').WardMasterDto>(`/v1/masters/wards/${publicId}`, payload) : post<import('../types').WardMasterDto>('/v1/masters/wards', payload);
}

export async function getVoteNumberMasters(): Promise<ApiResponse<import('../types').VoteNumberMasterDto[]>> {
  return get<import('../types').VoteNumberMasterDto[]>('/v1/masters/vote-numbers');
}

export async function saveVoteNumberMaster(publicId: string | null, payload: { departmentPublicId: string; code: string; number: string; name: string; amount: number; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<import('../types').VoteNumberMasterDto>> {
  return publicId ? put<import('../types').VoteNumberMasterDto>(`/v1/masters/vote-numbers/${publicId}`, payload) : post<import('../types').VoteNumberMasterDto>('/v1/masters/vote-numbers', payload);
}

export async function closeEmployeeAssignment(publicId: string, payload: { effectiveTo: string; reason: string; rowVersion: string }): Promise<ApiResponse<EmployeeAssignmentMasterDto>> {
  return put<EmployeeAssignmentMasterDto>(`/v1/masters/employee-assignments/${publicId}/close`, payload);
}

export async function getPerformancePeriodTargets(kind: 1 | 2, targetPublicId: string): Promise<ApiResponse<PerformancePeriodTargetDto[]>> {
  const parameter = kind === 1 ? 'opmsTargetId' : 'ipmsTargetId';
  return get<PerformancePeriodTargetDto[]>(`/v1/performance-period-targets?${parameter}=${encodeURIComponent(targetPublicId)}`);
}

export async function createPerformancePeriodTarget(payload: { targetKind: 1 | 2; targetPublicId: string; reportingPeriodPublicId: string; unitKind: number; direction: number; targetValue: string; budgetValue?: number; description?: string }): Promise<ApiResponse<PerformancePeriodTargetDto>> {
  return post<PerformancePeriodTargetDto>('/v1/performance-period-targets', payload);
}

export async function revisePerformancePeriodTarget(publicId: string, payload: { unitKind: number; direction: number; targetValue: string; budgetValue?: number; description?: string; isActive: boolean; reason: string; approvalReference: string; effectiveAt: string; rowVersion: string }): Promise<ApiResponse<PerformancePeriodTargetDto>> {
  return put<PerformancePeriodTargetDto>(`/v1/performance-period-targets/${publicId}`, payload);
}

export async function getPerformanceTargetRevisions(publicId: string): Promise<ApiResponse<PerformanceTargetRevisionDto[]>> {
  return get<PerformanceTargetRevisionDto[]>(`/v1/performance-period-targets/${publicId}/revisions`);
}

export async function getWorkflowDefinitions(): Promise<ApiResponse<WorkflowDefinitionDto[]>> {
  return get<WorkflowDefinitionDto[]>('/v1/workflow/definitions');
}

export async function createWorkflowDefinition(payload: {
  municipalityFinancialYearPublicId: string;
  submissionKind: number;
  code: string;
  name: string;
  isActive: boolean;
  effectiveFrom: string;
  reason: string;
  stages: Array<Omit<import('../types').WorkflowStageDefinitionDto, 'publicId' | 'ratingSchemeCode'>>;
}): Promise<ApiResponse<WorkflowDefinitionDto>> {
  return post<WorkflowDefinitionDto>('/v1/workflow/definitions', payload);
}

export async function compareWorkflowDefinitions(from: string, to: string): Promise<ApiResponse<WorkflowDefinitionComparisonDto>> {
  return get<WorkflowDefinitionComparisonDto>(`/v1/workflow/definitions/compare?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`);
}

export async function retireWorkflowDefinition(publicId: string, payload: { reason: string; effectiveTo: string; rowVersion: string }): Promise<ApiResponse<WorkflowDefinitionDto>> {
  return post<WorkflowDefinitionDto>(`/v1/workflow/definitions/${publicId}/retire`, payload);
}

export async function getReportingWindows(): Promise<ApiResponse<ReportingWindowDto[]>> {
  return get<ReportingWindowDto[]>('/v1/workflow/reporting-windows');
}

export async function createReportingWindow(payload: { reportingPeriodPublicId: string; submissionKind: number; opensAt: string; closesAt: string }): Promise<ApiResponse<ReportingWindowDto>> {
  return post<ReportingWindowDto>('/v1/workflow/reporting-windows', payload);
}

export async function getReportingWindowExceptions(windowPublicId: string): Promise<ApiResponse<ReportingWindowExceptionDto[]>> {
  return get<ReportingWindowExceptionDto[]>(`/v1/workflow/reporting-windows/${windowPublicId}/exceptions`);
}

export async function createReportingWindowException(windowPublicId: string, payload: { userPublicId?: string; departmentPublicId?: string; unitPublicId?: string; extendedClosesAt: string; reason: string }): Promise<ApiResponse<ReportingWindowExceptionDto>> {
  return post<ReportingWindowExceptionDto>(`/v1/workflow/reporting-windows/${windowPublicId}/exceptions`, payload);
}

export async function getPerformanceRfis(kind: 1 | 2, submissionId: string): Promise<ApiResponse<PerformanceRfiDto[]>> {
  return get<PerformanceRfiDto[]>(`/v1/workflow/submissions/${kind}/${encodeURIComponent(submissionId)}/rfis`);
}

export async function raisePerformanceRfi(kind: 1 | 2, submissionId: string, payload: { question: string; responseDueAt: string; evidencePublicIds?: string[] }): Promise<ApiResponse<PerformanceRfiDto>> {
  return post<PerformanceRfiDto>(`/v1/workflow/submissions/${kind}/${encodeURIComponent(submissionId)}/rfis`, payload);
}

export async function respondPerformanceRfi(publicId: string, payload: { response: string; rowVersion: string; evidencePublicIds?: string[] }): Promise<ApiResponse<PerformanceRfiDto>> {
  return post<PerformanceRfiDto>(`/v1/workflow/rfis/${publicId}/respond`, payload);
}

export async function closePerformanceRfi(publicId: string, payload: { comment?: string; rowVersion: string }): Promise<ApiResponse<PerformanceRfiDto>> {
  return post<PerformanceRfiDto>(`/v1/workflow/rfis/${publicId}/close`, payload);
}

export async function getDepartments(): Promise<ApiResponse<DepartmentLookupDto[]>> {
  return get<DepartmentLookupDto[]>('/departments');
}

export async function getUnits(): Promise<ApiResponse<UnitLookupDto[]>> {
  return get<UnitLookupDto[]>('/units');
}

export async function getRatingSchemes(): Promise<ApiResponse<RatingSchemeDto[]>> {
  return get<RatingSchemeDto[]>('/v1/workflow/rating-schemes');
}

export async function getSubmissionStageRatings(kind: number, submissionId: string): Promise<ApiResponse<StageRatingDto[]>> {
  return get<StageRatingDto[]>(`/v1/workflow/submissions/${kind}/${submissionId}/ratings`);
}

export async function getPendingNotificationDeliveries(): Promise<ApiResponse<NotificationOutboxItemDto[]>> {
  return get<NotificationOutboxItemDto[]>('/v1/notification-operations/pending');
}

export async function retryNotificationDelivery(item: NotificationOutboxItemDto, reason: string): Promise<ApiResponse<NotificationOutboxItemDto>> {
  return post<NotificationOutboxItemDto>(`/v1/notification-operations/${item.publicId}/retry`, { reason, rowVersion: item.rowVersion });
}

export async function createRatingScheme(payload: { code: string; name: string; values: Array<{ value: number; label: string; minimumAchievementPercent?: number; maximumAchievementPercent?: number; sortOrder: number }> }): Promise<ApiResponse<RatingSchemeDto>> {
  return post<RatingSchemeDto>('/v1/workflow/rating-schemes', payload);
}

export async function getPerformanceReportSummary(kind: 1 | 2, reportingPeriodPublicId?: string): Promise<ApiResponse<PerformanceReportSummaryDto>> {
  const query = new URLSearchParams({ kind: String(kind) });
  if (reportingPeriodPublicId) query.set('reportingPeriodPublicId', reportingPeriodPublicId);
  return get<PerformanceReportSummaryDto>(`/v1/reports/performance-summary?${query}`);
}

export async function downloadPerformanceReportCsv(kind: 1 | 2, reportingPeriodPublicId?: string): Promise<ApiResponse<boolean>> {
  const query = new URLSearchParams({ kind: String(kind) });
  if (reportingPeriodPublicId) query.set('reportingPeriodPublicId', reportingPeriodPublicId);
  const headers: Record<string, string> = {};
  addTenantHeader(headers);
  const response = await fetch(`${API_BASE_URL}/v1/reports/performance.csv?${query}`, { headers, credentials: 'include' });
  if (!response.ok) return readApiResponse<boolean>(response);
  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
  const fileName = match ? decodeURIComponent(match[1].replace(/"$/, '')) : `${kind === 1 ? 'opms' : 'ipms'}-performance.csv`;
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url; anchor.download = fileName; anchor.click();
  URL.revokeObjectURL(url);
  return { success: true, data: true };
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

export async function getSecurityPermissionDefinitions(): Promise<ApiResponse<SecurityPermissionDefinition[]>> {
  return get<SecurityPermissionDefinition[]>('/v1/security/permissions');
}

export async function getSecurityNavigationRegistry(): Promise<ApiResponse<SecurityNavigationItemDto[]>> {
  return get<SecurityNavigationItemDto[]>('/v1/security/navigation/registry');
}

export async function createSecurityNavigationItem(payload: Omit<SecurityNavigationItemDto, 'publicId' | 'rowVersion' | 'isActive'> & { reason: string }): Promise<ApiResponse<SecurityNavigationItemDto>> {
  return post<SecurityNavigationItemDto>('/v1/security/navigation/registry', payload);
}

export async function updateSecurityNavigationItem(item: SecurityNavigationItemDto, payload: Omit<SecurityNavigationItemDto, 'publicId' | 'code' | 'rowVersion'> & { reason: string }): Promise<ApiResponse<SecurityNavigationItemDto>> {
  return put<SecurityNavigationItemDto>(`/v1/security/navigation/registry/${item.publicId}`, { ...payload, rowVersion: item.rowVersion });
}

export async function getSecurityRoles(): Promise<ApiResponse<SecurityRoleSummary[]>> {
  return get<SecurityRoleSummary[]>('/v1/security/roles');
}

export async function createSecurityRole(payload: { roleCode: string; name: string; description?: string; effectiveFrom?: string; effectiveTo?: string }): Promise<ApiResponse<SecurityRoleSummary>> {
  return post<SecurityRoleSummary>('/v1/security/roles', payload);
}

export async function updateSecurityRole(role: SecurityRoleSummary, payload: { name: string; description?: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string }): Promise<ApiResponse<SecurityRoleSummary>> {
  return put<SecurityRoleSummary>(`/v1/security/roles/${role.id}`, { ...payload, rowVersion: role.rowVersion });
}

export async function getSecurityUsers(): Promise<ApiResponse<SecurityUserSummary[]>> {
  return get<SecurityUserSummary[]>('/v1/security/users');
}

export async function getSecurityUserRoles(userId: string): Promise<ApiResponse<SecurityUserRoleConfiguration>> {
  return get<SecurityUserRoleConfiguration>(`/v1/security/users/${userId}/roles`);
}

export async function saveSecurityUserRoles(userId: string, current: SecurityUserRoleConfiguration, assignments: Array<{ roleId: string; municipalityId?: number; departmentId?: number; unitId?: number; effectiveFrom?: string; effectiveTo?: string }>): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/v1/security/users/${userId}/roles`, {
    expectedAssignments: current.assignments.map(item => ({ assignmentId: item.id, rowVersion: item.rowVersion })),
    assignments,
  });
}

export async function getRoleSecurityConfiguration(roleId: string): Promise<ApiResponse<RoleSecurityConfiguration>> {
  return get<RoleSecurityConfiguration>(`/v1/security/roles/${roleId}/permissions`);
}

export async function saveRoleSecurityConfiguration(roleId: string, roleRowVersion: string, permissions: Array<Pick<RoleSecurityPermission, 'permissionCode' | 'state' | 'scopeType'>>): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/v1/security/roles/${roleId}/permissions`, { roleRowVersion, permissions });
}

export async function getEffectiveSecurityPreview(userId: string): Promise<ApiResponse<EffectiveSecurityPreview>> {
  return get<EffectiveSecurityPreview>(`/v1/security/effective-permissions/${userId}`);
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
  const response = await get<OpmsTargetDto[]>('/v1/opms-targets');
  return mapResponse(response, items => items.map(toOpmsTargetModel));
}

export async function getOpmsTarget(id: string): Promise<ApiResponse<OPMSTarget>> {
  const response = await get<OpmsTargetDto>(`/v1/opms-targets/${id}`);
  return mapResponse(response, toOpmsTargetModel);
}

export async function createOpmsTarget(payload: SaveOpmsTargetPayload): Promise<ApiResponse<OPMSTarget>> {
  const response = await post<OpmsTargetDto>('/v1/opms-targets', payload);
  return mapResponse(response, toOpmsTargetModel);
}

export async function updateOpmsTarget(id: string, payload: SaveOpmsTargetPayload): Promise<ApiResponse<OPMSTarget>> {
  const response = await put<OpmsTargetDto>(`/v1/opms-targets/${id}`, payload);
  return mapResponse(response, toOpmsTargetModel);
}

export async function withdrawOpmsTarget(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<OPMSTarget>> {
  const response = await post<OpmsTargetDto>(`/v1/opms-targets/${id}/withdraw`, payload);
  return mapResponse(response, toOpmsTargetModel);
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

export async function withdrawIpmsTarget(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<IPMSTarget>> {
  const response = await post<IpmsTargetDto>(`/v1/ipms-targets/${id}/withdraw`, payload);
  return mapResponse(response, toIpmsTargetModel);
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

export async function withdrawOpmsSubmission(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<OPMSSubmission>> {
  const targetsResult = await getOpmsTargets();
  const response = await post<OpmsSubmissionDto>(`/v1/opms-submissions/${id}/withdraw`, payload);
  return mapResponse(response, item => toOpmsSubmissionModel(item, targetsResult.data ?? []));
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

export async function rescanOpmsSubmissionAttachment(id: string, attachmentId: string) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/rescan`);
  return mapResponse(response, toAttachmentModel);
}

export async function assessOpmsSubmissionAttachment(id: string, attachmentId: string, payload: { outcome: 1 | 2 | 3; comment?: string }) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/assessments`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function replaceOpmsSubmissionAttachment(id: string, attachmentId: string, payload: { replacementEvidencePublicId: string; reason: string; supersededRowVersion: string; replacementRowVersion: string }) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/replace`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function placeOpmsEvidenceLegalHold(id: string, attachmentId: string, payload: { holdReference: string; reason: string }) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/legal-holds`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function releaseOpmsEvidenceLegalHold(id: string, attachmentId: string, holdId: string, payload: { reason: string }) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/legal-holds/${holdId}/release`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function requestOpmsEvidenceDisposal(id: string, attachmentId: string, payload: { approvalReference: string; reason: string; rowVersion: string }) {
  const response = await post<PoeFileDto>(`/opms-submissions/${id}/attachments/${attachmentId}/disposals`, payload);
  return mapResponse(response, toAttachmentModel);
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

export async function withdrawIpmsSubmission(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<IPMSSubmission>> {
  const targetsResult = await getIpmsTargets();
  const response = await post<IpmsSubmissionDto>(`/v1/ipms-submissions/${id}/withdraw`, payload);
  return mapResponse(response, item => toIpmsSubmissionModel(item, targetsResult.data ?? []));
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

export async function rescanIpmsSubmissionAttachment(id: string, attachmentId: string) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/rescan`);
  return mapResponse(response, toAttachmentModel);
}

export async function assessIpmsSubmissionAttachment(id: string, attachmentId: string, payload: { outcome: 1 | 2 | 3; comment?: string }) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/assessments`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function replaceIpmsSubmissionAttachment(id: string, attachmentId: string, payload: { replacementEvidencePublicId: string; reason: string; supersededRowVersion: string; replacementRowVersion: string }) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/replace`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function placeIpmsEvidenceLegalHold(id: string, attachmentId: string, payload: { holdReference: string; reason: string }) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/legal-holds`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function releaseIpmsEvidenceLegalHold(id: string, attachmentId: string, holdId: string, payload: { reason: string }) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/legal-holds/${holdId}/release`, payload);
  return mapResponse(response, toAttachmentModel);
}

export async function requestIpmsEvidenceDisposal(id: string, attachmentId: string, payload: { approvalReference: string; reason: string; rowVersion: string }) {
  const response = await post<PoeFileDto>(`/ipms-submissions/${id}/attachments/${attachmentId}/disposals`, payload);
  return mapResponse(response, toAttachmentModel);
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

export async function getIdpImportBatches(planPublicId: string): Promise<ApiResponse<import('../types').IdpImportBatch[]>> {
  return get<import('../types').IdpImportBatch[]>(`/v1/idp/plans/${planPublicId}/imports`);
}

export async function stageIdpKpiImport(
  planPublicId: string,
  payload: { clientRequestId: string; sourceFileName: string; rows: import('../types').IdpKpiImportRowPayload[] },
): Promise<ApiResponse<import('../types').IdpImportBatch>> {
  return post<import('../types').IdpImportBatch>(`/v1/idp/plans/${planPublicId}/imports/kpis/stage`, payload);
}

export async function commitIdpImport(
  batchPublicId: string,
  payload: { rowVersion: string; reason: string },
): Promise<ApiResponse<import('../types').IdpImportBatch>> {
  return post<import('../types').IdpImportBatch>(`/v1/idp/imports/${batchPublicId}/commit`, payload);
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
