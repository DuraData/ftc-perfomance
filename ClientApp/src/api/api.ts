import type {
  ApiResponse,
  PagedResult,
  TargetLibraryFacets,
  PerformanceTargetOptionDto,
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
  NotificationPageResult,
  PoeFileDto,
  SaveOpmsTargetTemplatePayload,
  SaveIpmsTargetTemplatePayload,
  SaveOpmsTargetPayload,
  SaveIpmsTargetPayload,
  SaveOpmsSubmissionPayload,
  SaveIpmsSubmissionPayload,
  SubmissionWorkflowActionPayload,
  DueDateExtensionPayload,
  PerformanceSuggestionResult,
  PerformanceSuggestionEvent,
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
  SdbipLayerMasterDto,
  OpmsImportBatchDto,
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
  InternalAuditAssessmentDto,
  InternalAuditConfigurationDto,
  InternalAuditSubmissionDto,
  ReportingWindowDto,
  RatingSchemeDto,
  PerformanceReportSummaryDto,
  OfficialReportTemplateDto,
  OfficialReportGenerationDto,
  OfficialReportFormat,
  OfficialReportType,
  OfficialReportJobDto,
  OfficialReportScheduleDto,
  OfficialReportScheduleCadence,
  OfficialReportRecipientKind,
  PerformancePeriodTargetDto,
  PerformanceTargetRevisionDto,
  KpiFieldRevisionDto,
  ReportingWindowExceptionDto,
  DepartmentLookupDto,
  UnitLookupDto,
  PerformanceLookupsDto,
  PerformanceRfiDto,
  StageRatingDto,
  SecurityNavigationItemDto,
  NotificationOutboxItemDto,
  NotificationPolicyDto,
  WorkingCalendarHolidayDto,
  NotificationTemplatePreviewDto,
  NotificationPreferenceDto,
  TidConfiguration,
  TidRegisterItem,
  TidSourceDocument,
  TidVersion,
  SaveTidVersionPayload,
  StrategicDocument,
  StrategicDocumentType,
  SaveStrategicDocumentVersionPayload,
  C88Workspace,
  C88IndicatorReport,
  EnterpriseSignInOptions,
  EnterpriseProviderOption,
  AuthenticationConfiguration,
  UserAuthenticator,
  AuthenticationEvent,
  Attachment,
  BudgetSource,
  BudgetType,
  Department,
  DepartmentUnit,
  Employee,
  Period,
  StrategicGoal,
  StrategicObjective,
  UnitOfMeasure,
  TargetNormalizationPreviewDto,
  TargetNormalizationResultDto,
  PerformanceDashboardDto,
  WorkflowQueueDto,
  WorkflowQueueName,
} from '../types';

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

function createIdempotencyKey(): string {
  if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();
  if (globalThis.crypto?.getRandomValues) {
    const bytes = globalThis.crypto.getRandomValues(new Uint8Array(16));
    return Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
  }
  throw new Error('Secure random generation is unavailable; the mutation was not sent.');
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

const NOT_SUPPLIED = 'Not supplied by API';

function toPeriodReference(id?: number | null): Period {
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: id === null || id === undefined ? NOT_SUPPLIED : `Reporting period ${id}`,
    startDate: '',
    endDate: '',
    fiscalYear: '',
    isActive: true,
  };
}

function toDepartmentReference(id?: number | null, name?: string | null, publicId?: string | null): Department {
  return {
    id: id === null || id === undefined ? '' : String(id),
    publicId: normalizeOptionalString(publicId),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    isActive: true,
    units: [],
    positions: [],
  };
}

function toUnitReference(
  id: number | null | undefined,
  name: string | null | undefined,
  department: Department,
  publicId?: string | null,
): DepartmentUnit | undefined {
  if ((id === null || id === undefined) && !normalizeOptionalString(name)) return undefined;
  return {
    id: id === null || id === undefined ? '' : String(id),
    publicId: normalizeOptionalString(publicId),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    department,
    isActive: true,
  };
}

function toEmployeeReference(id?: string | null, name?: string | null): Employee | undefined {
  const displayName = normalizeOptionalString(name);
  if (!id && !displayName) return undefined;
  return {
    id: id ?? '',
    firstName: '',
    lastName: '',
    displayName: displayName ?? NOT_SUPPLIED,
    email: '',
    identificationType: '',
    isActive: true,
  };
}

function toStrategicGoalReference(id?: number | null, name?: string | null): StrategicGoal {
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    isActive: true,
  };
}

function toStrategicObjectiveReference(
  id: number | null | undefined,
  name: string | null | undefined,
  strategicGoal: StrategicGoal,
): StrategicObjective {
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    strategicGoal,
    isActive: true,
  };
}

function toBudgetSourceReference(id?: number | null, name?: string | null): BudgetSource {
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    isActive: true,
  };
}

function toBudgetTypeReference(id?: number | null, name?: string | null): BudgetType {
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: normalizeOptionalString(name) ?? NOT_SUPPLIED,
    code: '',
    isActive: true,
  };
}

function toUnitOfMeasureReference(id?: number | null, name?: string | null): UnitOfMeasure {
  const normalizedName = normalizeOptionalString(name);
  return {
    id: id === null || id === undefined ? '' : String(id),
    name: normalizedName ?? NOT_SUPPLIED,
    code: normalizedName ?? '',
    isActive: true,
  };
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
    unitOfMeasure: toUnitOfMeasureReference(undefined, dto.unitOfMeasure),
    nationalKPA: dto.nationalKpa ?? '',
    municipalKPA: dto.municipalKpa ?? '',
    strategicGoal: normalizeOptionalString(dto.strategicGoal)
      ? toStrategicGoalReference(undefined, dto.strategicGoal)
      : undefined,
    strategicObjective: normalizeOptionalString(dto.strategicObjective)
      ? toStrategicObjectiveReference(
        undefined,
        dto.strategicObjective,
        toStrategicGoalReference(undefined, dto.strategicGoal),
      )
      : undefined,
    performanceObjective: dto.performanceObjective ?? '',
    outcome: normalizeOptionalString(dto.outcome),
    output: normalizeOptionalString(dto.output),
    priorityIssue: normalizeOptionalString(dto.priorityIssue),
    budgetSource: normalizeOptionalString(dto.budgetSource)
      ? toBudgetSourceReference(undefined, dto.budgetSource)
      : undefined,
    budgetType: normalizeOptionalString(dto.budgetType)
      ? toBudgetTypeReference(undefined, dto.budgetType)
      : undefined,
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
    unitOfMeasure: toUnitOfMeasureReference(undefined, dto.unitOfMeasure),
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

const targetUnitByKind: Record<number, OPMSTarget['targetUnitType']> = {
  0: 'absolute_count', 1: 'percentage', 2: 'absolute_count', 3: 'financial', 4: 'time_based',
  5: 'area_based', 6: 'volume_based', 7: 'index_scores', 8: 'ratios', 9: 'binary', 10: 'date',
  11: 'readiness_scale', 12: 'binary_determination', 13: 'qualitative', 14: 'zero_based',
  15: 'reverse_cumulative', 16: 'reverse_non_cumulative',
};

function canonicalPeriod(rows: PerformancePeriodTargetDto[], periodType: number) {
  return rows.find(item => item.periodType === periodType && item.isActive);
}

function numericTarget(value?: string | null): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function toOpmsTargetModel(dto: OpmsTargetDto): OPMSTarget {
  const department = toDepartmentReference(dto.departmentId, dto.departmentName, dto.departmentPublicId);
  const strategicGoal = toStrategicGoalReference(dto.strategicGoalId);
  const periods = dto.periodTargets ?? [];
  const q1 = canonicalPeriod(periods, 1); const q2 = canonicalPeriod(periods, 2); const mid = canonicalPeriod(periods, 3);
  const q3 = canonicalPeriod(periods, 4); const q4 = canonicalPeriod(periods, 5); const annual = canonicalPeriod(periods, 6);
  const canonicalUnitType = targetUnitByKind[annual?.unitKind ?? periods[0]?.unitKind ?? 2] ?? 'absolute_count';
  return {
    id: dto.id,
    publicId: dto.publicId,
    rowVersion: dto.rowVersion,
    sdbipLayer: dto.sdbipLayerPublicId ? { publicId: dto.sdbipLayerPublicId, code: dto.sdbipLayerCode ?? '', name: dto.sdbipLayerName ?? '' } : undefined,
    sourceTemplateId: dto.sourceTemplateId ?? undefined,
    sourceTemplateVersion: dto.sourceTemplateVersion ?? undefined,
    period: toPeriodReference(dto.periodId),
    department,
    unit: toUnitReference(dto.unitId, dto.unitName, department, dto.unitPublicId),
    assignedTo: toEmployeeReference(dto.assignedUserId, dto.assignedUserName),
    wards: dto.wardIds.map(id => ({ id: String(id), code: String(id), name: `Ward ${id}`, isActive: true })),
    wardIds: dto.wardIds,
    additionalAssignees: dto.additionalAssigneeIds.map(id => toEmployeeReference(id)).filter((employee): employee is Employee => Boolean(employee)),
    additionalAssigneeIds: dto.additionalAssigneeIds,
    voteNumbers: dto.voteNumberIds.map(id => ({ id: String(id), number: String(id), name: `Vote ${id}`, department, isActive: true })),
    voteNumberIds: dto.voteNumberIds,
    indicatorNumber: dto.indicatorNumber,
    isIndicatorNumberRevised: dto.isIndicatorNumberRevised,
    revisedIndicatorNumber: dto.revisedIndicatorNumber ?? undefined,
    originalOrderNumber: dto.originalOrderNumber,
    revisedOrderNumber: dto.revisedOrderNumber,
    nationalKPA: dto.nationalKpa,
    municipalKPA: dto.municipalKpa,
    strategicGoal,
    strategicObjective: toStrategicObjectiveReference(dto.strategicObjectiveId, undefined, strategicGoal),
    performanceObjective: dto.performanceObjective,
    targetName: dto.targetName,
    isTargetNameRevised: dto.isTargetNameRevised,
    revisedTargetName: dto.revisedTargetName ?? undefined,
    kpiDescription: dto.kpiDescription,
    isKpiDescriptionRevised: dto.isKpiDescriptionRevised,
    revisedKpiDescription: dto.revisedKpiDescription ?? undefined,
    baseline: dto.baseline,
    baselineDescription: dto.baselineDescription ?? '',
    annualTarget: numericTarget(annual?.targetValue),
    annualTargetDescription: annual?.description ?? '',
    budgetSource: toBudgetSourceReference(dto.budgetSourceId),
    budgetType: toBudgetTypeReference(dto.budgetTypeId),
    unitOfMeasure: toUnitOfMeasureReference(dto.unitOfMeasureId, canonicalUnitType),
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
    targetUnitType: canonicalUnitType,
    periodTargets: periods,
    q1Target: numericTarget(q1?.targetValue),
    q1Description: q1?.description ?? '',
    q1Budget: q1?.budgetValue ?? 0,
    q2Target: numericTarget(q2?.targetValue),
    q2Description: q2?.description ?? '',
    q2Budget: q2?.budgetValue ?? 0,
    midTermTarget: numericTarget(mid?.targetValue),
    midTermDescription: mid?.description ?? '',
    midTermBudget: mid?.budgetValue ?? 0,
    q3Target: numericTarget(q3?.targetValue),
    q3Description: q3?.description ?? '',
    q3Budget: q3?.budgetValue ?? 0,
    q3RevisedTarget: 0,
    q4Target: numericTarget(q4?.targetValue),
    q4Description: q4?.description ?? '',
    q4Budget: q4?.budgetValue ?? 0,
    q4RevisedTarget: 0,
    revisedAnnualTarget: 0,
    revisedAnnualBudget: 0,
    submissions: [],
    relatedIPMSTargets: [],
    attachments: [],
    CreatedOn: dto.createdAt,
  };
}

function toIpmsTargetModel(dto: IpmsTargetDto): IPMSTarget {
  const department = toDepartmentReference(dto.departmentId, dto.departmentName, dto.departmentPublicId);
  const strategicGoal = toStrategicGoalReference(dto.strategicGoalId);
  const periods = dto.periodTargets ?? [];
  const q1 = canonicalPeriod(periods, 1); const q2 = canonicalPeriod(periods, 2); const mid = canonicalPeriod(periods, 3);
  const q3 = canonicalPeriod(periods, 4); const q4 = canonicalPeriod(periods, 5); const annual = canonicalPeriod(periods, 6);
  const canonicalUnitType = targetUnitByKind[annual?.unitKind ?? periods[0]?.unitKind ?? 2] ?? 'absolute_count';
  return {
    id: dto.id,
    publicId: dto.publicId,
    rowVersion: dto.rowVersion,
    sourceTemplateId: dto.sourceTemplateId ?? undefined,
    sourceTemplateVersion: dto.sourceTemplateVersion ?? undefined,
    relatedOPMSTarget: undefined,
    period: toPeriodReference(dto.periodId),
    department,
    unit: toUnitReference(dto.unitId, dto.unitName, department, dto.unitPublicId),
    assignedTo: toEmployeeReference(dto.assignedUserId, dto.assignedUserName),
    indicatorNumber: dto.indicatorNumber,
    isIndicatorNumberRevised: dto.isIndicatorNumberRevised,
    revisedIndicatorNumber: dto.revisedIndicatorNumber ?? undefined,
    originalOrderNumber: dto.originalOrderNumber,
    revisedOrderNumber: dto.revisedOrderNumber,
    nationalKPA: dto.nationalKpa,
    municipalKPA: dto.municipalKpa,
    strategicGoal,
    strategicObjective: toStrategicObjectiveReference(dto.strategicObjectiveId, undefined, strategicGoal),
    performanceObjective: dto.performanceObjective,
    targetName: dto.targetName,
    isTargetNameRevised: dto.isTargetNameRevised,
    revisedTargetName: dto.revisedTargetName ?? undefined,
    kpiDescription: dto.kpiDescription,
    isKpiDescriptionRevised: dto.isKpiDescriptionRevised,
    revisedKpiDescription: dto.revisedKpiDescription ?? undefined,
    baseline: dto.baseline,
    annualTarget: numericTarget(annual?.targetValue),
    annualTargetDescription: annual?.description ?? '',
    budgetSource: toBudgetSourceReference(dto.budgetSourceId),
    budgetType: toBudgetTypeReference(dto.budgetTypeId),
    unitOfMeasure: toUnitOfMeasureReference(dto.unitOfMeasureId, canonicalUnitType),
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
    targetUnitType: canonicalUnitType,
    periodTargets: periods,
    q1Target: numericTarget(q1?.targetValue),
    q1Description: q1?.description ?? '',
    q1Budget: q1?.budgetValue ?? 0,
    q2Target: numericTarget(q2?.targetValue),
    q2Description: q2?.description ?? '',
    q2Budget: q2?.budgetValue ?? 0,
    midTermTarget: numericTarget(mid?.targetValue),
    midTermDescription: mid?.description ?? '',
    midTermBudget: mid?.budgetValue ?? 0,
    q3Target: numericTarget(q3?.targetValue),
    q3Description: q3?.description ?? '',
    q3Budget: q3?.budgetValue ?? 0,
    q3RevisedTarget: 0,
    q4Target: numericTarget(q4?.targetValue),
    q4Description: q4?.description ?? '',
    q4Budget: q4?.budgetValue ?? 0,
    q4RevisedTarget: 0,
    revisedAnnualTarget: 0,
    revisedAnnualBudget: 0,
    submissions: [],
    attachments: [],
    CreatedOn: dto.createdAt,
  };
}

function unresolvedOpmsTarget(id: string, targetName: string, indicatorNumber = ''): OPMSTarget {
  return toOpmsTargetModel({
    id,
    publicId: id,
    rowVersion: '',
    wardIds: [],
    additionalAssigneeIds: [],
    voteNumberIds: [],
    indicatorNumber,
    isIndicatorNumberRevised: false,
    originalOrderNumber: 1,
    revisedOrderNumber: 1,
    nationalKpa: '',
    municipalKpa: '',
    performanceObjective: '',
    targetName,
    isTargetNameRevised: false,
    kpiDescription: '',
    isKpiDescriptionRevised: false,
    baseline: 0,
    periodTargets: [],
    weight: 0,
    kpiType: '',
    indicatorType: '',
    isRevised: false,
    isWithdrawn: false,
    createdAt: '',
  });
}

function unresolvedIpmsTarget(id: string, targetName: string, indicatorNumber = ''): IPMSTarget {
  return toIpmsTargetModel({
    id,
    publicId: id,
    rowVersion: '',
    indicatorNumber,
    isIndicatorNumberRevised: false,
    originalOrderNumber: 1,
    revisedOrderNumber: 1,
    nationalKpa: '',
    municipalKpa: '',
    performanceObjective: '',
    targetName,
    isTargetNameRevised: false,
    kpiDescription: '',
    isKpiDescriptionRevised: false,
    baseline: 0,
    periodTargets: [],
    weight: 0,
    kpiType: '',
    indicatorType: '',
    isRevised: false,
    isWithdrawn: false,
    createdAt: '',
  });
}

function toSubmissionBaseState(dto: OpmsSubmissionDto | IpmsSubmissionDto) {
  const baseState = (dto as (OpmsSubmissionDto | IpmsSubmissionDto) & { baseState?: string }).baseState;
  return { baseState: baseState === 'SUBMITTED' ? 'SUBMITTED' as const : 'IN_PROGRESS' as const };
}

function numericActualProjection(value?: string | null): number {
  if (!value?.trim()) return 0;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function toOpmsSubmissionModel(dto: OpmsSubmissionDto): OPMSSubmission {
  const target = unresolvedOpmsTarget(dto.opmsTargetId, dto.targetName, dto.targetIndicatorNumber);
  return {
    ...toSubmissionBaseState(dto),
    id: dto.id,
    rowVersion: dto.rowVersion,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? '',
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: numericActualProjection(dto.actualPerformance),
    actualPerformance: dto.actualPerformance ?? undefined,
    systemSuggestedActualPerformance: dto.systemSuggestedActualPerformance ?? undefined,
    wasSystemSuggestionEdited: dto.wasSystemSuggestionEdited,
    suggestionGeneratedDate: dto.suggestionGeneratedDate ?? undefined,
    suggestionEditedByUserId: dto.suggestionEditedByUserId ?? undefined,
    suggestionEditedAt: dto.suggestionEditedAt ?? undefined,
    suggestionEditReason: dto.suggestionEditReason ?? undefined,
    achievementPercent: dto.achievementPercent ?? undefined,
    targetAchieved: dto.targetAchieved ?? undefined,
    reportingPeriodPublicId: dto.reportingPeriodPublicId ?? undefined,
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
    submitter: toEmployeeReference(dto.submittedByUserId, dto.submittedByName),
    submittedAt: dto.submittedAt ?? undefined,
    submittedByUserId: dto.submittedByUserId ?? undefined,
    verifier: toEmployeeReference(dto.verifierUserId, dto.verifierName),
    verifiedAt: dto.verifiedAt ?? undefined,
    verifierComments: dto.verifierComments ?? undefined,
    verifierComment: dto.verifierComment ?? undefined,
    verifierScore: dto.verifierScore ?? undefined,
    approver: toEmployeeReference(dto.approverUserId, dto.approverName),
    approvedAt: dto.approvedAt ?? undefined,
    approverComments: dto.approverComments ?? undefined,
    approverComment: dto.approverComment ?? undefined,
    approverScore: dto.approverScore ?? undefined,
    pmsOfficer: toEmployeeReference(dto.pmsOfficerUserId, dto.pmsOfficerName),
    pmsReviewedAt: dto.pmsReviewedAt ?? undefined,
    pmsComments: dto.pmsComments ?? undefined,
    pmsComment: dto.pmsComment ?? undefined,
    pmsRecommendation: dto.pmsRecommendation ?? undefined,
    pmsScore: dto.pmsScore ?? undefined,
    pmsResponseDueDate: dto.pmsResponseDueDate ?? undefined,
    pmsRfiComment: dto.pmsRfiComment ?? undefined,
    auditor: toEmployeeReference(dto.auditorUserId, dto.auditorName),
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
    attachments: [],
    comments: [],
    history: [],
  };
}

function toIpmsSubmissionModel(dto: IpmsSubmissionDto): IPMSSubmission {
  const target = unresolvedIpmsTarget(dto.ipmsTargetId, dto.targetName, dto.targetIndicatorNumber);
  return {
    ...toSubmissionBaseState(dto),
    id: dto.id,
    rowVersion: dto.rowVersion,
    target,
    quarter: coerceQuarter(dto.quarter),
    dueDate: dto.dueDate ?? '',
    extendedDueDate: dto.extendedDueDate ?? undefined,
    actual: numericActualProjection(dto.actualPerformance),
    actualPerformance: dto.actualPerformance ?? undefined,
    systemSuggestedActualPerformance: dto.systemSuggestedActualPerformance ?? undefined,
    wasSystemSuggestionEdited: dto.wasSystemSuggestionEdited,
    suggestionGeneratedDate: dto.suggestionGeneratedDate ?? undefined,
    suggestionEditedByUserId: dto.suggestionEditedByUserId ?? undefined,
    suggestionEditedAt: dto.suggestionEditedAt ?? undefined,
    suggestionEditReason: dto.suggestionEditReason ?? undefined,
    achievementPercent: dto.achievementPercent ?? undefined,
    targetAchieved: dto.targetAchieved ?? undefined,
    reportingPeriodPublicId: dto.reportingPeriodPublicId ?? undefined,
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
    submitter: toEmployeeReference(dto.submittedByUserId, dto.submittedByName),
    submittedAt: dto.submittedAt ?? undefined,
    submittedByUserId: dto.submittedByUserId ?? undefined,
    verifier: toEmployeeReference(dto.verifierUserId, dto.verifierName),
    verifiedAt: dto.verifiedAt ?? undefined,
    verifierComments: dto.verifierComments ?? undefined,
    verifierComment: dto.verifierComment ?? undefined,
    verifierScore: dto.verifierScore ?? undefined,
    approver: toEmployeeReference(dto.approverUserId, dto.approverName),
    approvedAt: dto.approvedAt ?? undefined,
    approverComments: dto.approverComments ?? undefined,
    approverComment: dto.approverComment ?? undefined,
    approverScore: dto.approverScore ?? undefined,
    pmsOfficer: toEmployeeReference(dto.pmsOfficerUserId, dto.pmsOfficerName),
    pmsReviewedAt: dto.pmsReviewedAt ?? undefined,
    pmsComments: dto.pmsComments ?? undefined,
    pmsComment: dto.pmsComment ?? undefined,
    pmsRecommendation: dto.pmsRecommendation ?? undefined,
    pmsScore: dto.pmsScore ?? undefined,
    pmsResponseDueDate: dto.pmsResponseDueDate ?? undefined,
    pmsRfiComment: dto.pmsRfiComment ?? undefined,
    auditor: toEmployeeReference(dto.auditorUserId, dto.auditorName),
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
    attachments: [],
    comments: [],
    history: [],
  };
}

function toAttachmentModel(dto: PoeFileDto): Attachment {
  return {
    id: dto.id,
    publicId: dto.publicId,
    evidenceBlobPublicId: dto.evidenceBlobPublicId,
    fileName: dto.fileName,
    fileSize: dto.sizeInBytes,
    fileType: dto.contentType ?? 'application/octet-stream',
    uploadedBy: toEmployeeReference(dto.uploadedByUserId, dto.uploadedByName)!,
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
  const suppliedHeaders = { ...(options.headers as Record<string, string>) };
  const isPost = (options.method ?? 'GET').toUpperCase() === 'POST';
  const idempotencyKey = suppliedHeaders['Idempotency-Key']
    ?? (isPost ? createIdempotencyKey() : undefined);
  const headers: Record<string, string> = {
    ...(isFormDataBody ? {} : { 'Content-Type': 'application/json' }),
    'X-OPMS-Request': 'same-origin',
    ...suppliedHeaders,
    ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
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
          'X-OPMS-Request': 'same-origin',
          ...suppliedHeaders,
          ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
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
  const problem = payload as { title?: string; detail?: string; errors?: Record<string, string[]>; code?: string; correlationId?: string };
  const errors = problem.errors ? Object.values(problem.errors).flat() : undefined;
  return {
    success: response.ok,
    data: response.ok ? payload as T : undefined,
    message: problem.detail ?? problem.title ?? `Request failed with status ${response.status}.${correlationId ? ` Correlation ID: ${correlationId}` : ''}`,
    errors,
    code: problem.code,
    correlationId: problem.correlationId ?? correlationId ?? undefined,
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
        headers: addTenantHeader({ 'Content-Type': 'application/json', 'X-OPMS-Request': 'same-origin' }),
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

export async function completeEnterpriseLogin(): Promise<ApiResponse<LoginResponse>> {
  return refreshAccessToken();
}

export async function getEnterpriseSignInOptions(municipalityCode: string): Promise<ApiResponse<EnterpriseSignInOptions>> {
  return get<EnterpriseSignInOptions>(`/v1/auth/enterprise/options/${encodeURIComponent(municipalityCode.trim())}`);
}

export function enterpriseSignInUrl(municipalityCode: string, providerCode: string): string {
  return `${API_BASE_URL}/v1/auth/enterprise/challenge/${encodeURIComponent(municipalityCode.trim())}/${encodeURIComponent(providerCode)}`;
}

export async function getAuthenticationConfiguration(): Promise<ApiResponse<AuthenticationConfiguration | null>> { return get<AuthenticationConfiguration | null>('/v1/admin/authentication'); }
export async function getAuthenticationProviders(): Promise<ApiResponse<EnterpriseProviderOption[]>> { return get<EnterpriseProviderOption[]>('/v1/admin/authentication/providers'); }
export async function saveAuthenticationConfiguration(payload: unknown): Promise<ApiResponse<AuthenticationConfiguration>> { return put<AuthenticationConfiguration>('/v1/admin/authentication', payload); }
export async function getUserAuthenticators(): Promise<ApiResponse<UserAuthenticator[]>> { return get<UserAuthenticator[]>('/v1/admin/authentication/authenticators'); }
export async function provisionUserAuthenticator(payload: unknown): Promise<ApiResponse<UserAuthenticator>> { return post<UserAuthenticator>('/v1/admin/authentication/authenticators', payload); }
export async function setUserAuthenticatorStatus(publicId: string, isActive: boolean, reason: string, rowVersion: string): Promise<ApiResponse<UserAuthenticator>> { return put<UserAuthenticator>(`/v1/admin/authentication/authenticators/${encodeURIComponent(publicId)}/status`, { isActive, reason, rowVersion }); }
export async function getAuthenticationEvents(): Promise<ApiResponse<AuthenticationEvent[]>> { return get<AuthenticationEvent[]>('/v1/admin/authentication/events?take=100'); }

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

export type CalendarMasterPageQuery = RegisterPageQuery & {
  active?: boolean;
  current?: boolean;
  municipalityFinancialYearId?: string;
  reportingPeriodType?: number;
};

function calendarMasterPageQuery(query: CalendarMasterPageQuery): string {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  if (query.active !== undefined) parameters.set('active', String(query.active));
  if (query.current !== undefined) parameters.set('current', String(query.current));
  if (query.municipalityFinancialYearId) parameters.set('municipalityFinancialYearId', query.municipalityFinancialYearId);
  if (query.reportingPeriodType !== undefined) parameters.set('reportingPeriodType', String(query.reportingPeriodType));
  return parameters.size ? `?${parameters.toString()}` : '';
}

export async function getReportingPeriodMastersPage(query: CalendarMasterPageQuery = {}): Promise<ApiResponse<PagedResult<ReportingPeriodMasterDto>>> {
  return get<PagedResult<ReportingPeriodMasterDto>>(`/v1/masters/reporting-periods/page${calendarMasterPageQuery(query)}`);
}

export async function getFinancialYearMasters(): Promise<ApiResponse<FinancialYearMasterDto[]>> {
  return get<FinancialYearMasterDto[]>('/v1/masters/financial-years');
}

export async function getFinancialYearMastersPage(query: CalendarMasterPageQuery = {}): Promise<ApiResponse<PagedResult<FinancialYearMasterDto>>> {
  return get<PagedResult<FinancialYearMasterDto>>(`/v1/masters/financial-years/page${calendarMasterPageQuery(query)}`);
}

export async function createFinancialYearMaster(payload: { code: string; name: string; startDate: string; endDate: string }): Promise<ApiResponse<FinancialYearMasterDto>> {
  return post<FinancialYearMasterDto>('/v1/masters/financial-years', payload);
}

export async function getMunicipalityFinancialYearMasters(): Promise<ApiResponse<MunicipalityFinancialYearMasterDto[]>> {
  return get<MunicipalityFinancialYearMasterDto[]>('/v1/masters/municipality-financial-years');
}

export async function getMunicipalityFinancialYearMastersPage(query: CalendarMasterPageQuery = {}): Promise<ApiResponse<PagedResult<MunicipalityFinancialYearMasterDto>>> {
  return get<PagedResult<MunicipalityFinancialYearMasterDto>>(`/v1/masters/municipality-financial-years/page${calendarMasterPageQuery(query)}`);
}

export async function createMunicipalityFinancialYearMaster(payload: { financialYearPublicId: string; isCurrent: boolean; effectiveFrom: string; effectiveTo?: string | null }): Promise<ApiResponse<MunicipalityFinancialYearMasterDto>> {
  return post<MunicipalityFinancialYearMasterDto>('/v1/masters/municipality-financial-years', payload);
}

export async function updateMunicipalityFinancialYearMaster(publicId: string, payload: { isCurrent: boolean; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string }): Promise<ApiResponse<MunicipalityFinancialYearMasterDto>> {
  return put<MunicipalityFinancialYearMasterDto>(`/v1/masters/municipality-financial-years/${publicId}`, payload);
}

export async function getSdbipLayerMasters(municipalityFinancialYearPublicId?: string, includeInactive = false): Promise<ApiResponse<SdbipLayerMasterDto[]>> {
  const query = new URLSearchParams();
  if (municipalityFinancialYearPublicId) query.set('municipalityFinancialYearId', municipalityFinancialYearPublicId);
  if (includeInactive) query.set('includeInactive', 'true');
  return get<SdbipLayerMasterDto[]>(`/v1/masters/sdbip-layers${query.size ? `?${query.toString()}` : ''}`);
}

export async function getSdbipLayerMastersPage(query: CalendarMasterPageQuery = {}): Promise<ApiResponse<PagedResult<SdbipLayerMasterDto>>> {
  return get<PagedResult<SdbipLayerMasterDto>>(`/v1/masters/sdbip-layers/page${calendarMasterPageQuery(query)}`);
}

export const stageOpmsImport = (layerPublicId: string, payload: unknown): Promise<ApiResponse<OpmsImportBatchDto>> => post<OpmsImportBatchDto>(`/v1/opms/imports/layers/${layerPublicId}/stage`, payload);
export const commitOpmsImport = (batchPublicId: string, payload: unknown): Promise<ApiResponse<OpmsImportBatchDto>> => post<OpmsImportBatchDto>(`/v1/opms/imports/${batchPublicId}/commit`, payload);

export async function downloadOpmsImportCsv(layerPublicId?: string): Promise<ApiResponse<boolean>> {
  const endpoint = layerPublicId ? `/v1/opms/imports/layers/${encodeURIComponent(layerPublicId)}/export.csv` : '/v1/opms/imports/template.csv';
  const headers: Record<string, string> = {}; addTenantHeader(headers);
  const response = await fetch(`${API_BASE_URL}${endpoint}`, { headers, credentials: 'include' });
  if (!response.ok) return readApiResponse<boolean>(response);
  const blob = await response.blob(); const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
  const fileName = match ? decodeURIComponent(match[1].replace(/"$/, '')) : layerPublicId ? 'sdbip-export.csv' : 'opms-sdbip-import-template.csv';
  const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = fileName; anchor.click(); URL.revokeObjectURL(url);
  return { success: true, data: true };
}

export async function createSdbipLayerMaster(payload: { municipalityFinancialYearPublicId: string; code: string; name: string; description?: string | null; displayOrder: number; reason: string }): Promise<ApiResponse<SdbipLayerMasterDto>> {
  return post<SdbipLayerMasterDto>('/v1/masters/sdbip-layers', payload);
}

export async function updateSdbipLayerMaster(publicId: string, payload: { code: string; name: string; description?: string | null; displayOrder: number; isActive: boolean; reason: string; rowVersion: string }): Promise<ApiResponse<SdbipLayerMasterDto>> {
  return put<SdbipLayerMasterDto>(`/v1/masters/sdbip-layers/${publicId}`, payload);
}

export async function createReportingPeriodMaster(payload: { municipalityFinancialYearPublicId: string; code: string; name: string; periodType: number; sequence: number; startDate: string; endDate: string }): Promise<ApiResponse<ReportingPeriodMasterDto>> {
  return post<ReportingPeriodMasterDto>('/v1/masters/reporting-periods', payload);
}

export async function getMunicipalEmployeesPage(query: RegisterPageQuery = {}, activeOnly = false): Promise<ApiResponse<PagedResult<MunicipalEmployeeDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  if (activeOnly) parameters.set('activeOnly', 'true');
  const suffix = parameters.size ? `?${parameters.toString()}` : '';
  return get<PagedResult<MunicipalEmployeeDto>>(`/v1/masters/employees/page${suffix}`);
}

export async function createMunicipalEmployee(payload: { employeeNumber: string; firstName: string; lastName: string; emailAddress?: string | null; identityUserId?: string | null; effectiveFrom: string; effectiveTo?: string | null }): Promise<ApiResponse<MunicipalEmployeeDto>> {
  return post<MunicipalEmployeeDto>('/v1/masters/employees', payload);
}

export async function updateMunicipalEmployee(publicId: string, payload: { firstName: string; lastName: string; emailAddress?: string | null; emailAddressSpecified?: boolean; identityUserId?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string }): Promise<ApiResponse<MunicipalEmployeeDto>> {
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

export type OrganizationMasterPageQuery = RegisterPageQuery & {
  active?: boolean;
  departmentPublicId?: string;
  unitPublicId?: string;
};

function organizationMasterPageQuery(query: OrganizationMasterPageQuery): string {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  if (query.active !== undefined) parameters.set('active', String(query.active));
  if (query.departmentPublicId) parameters.set('departmentPublicId', query.departmentPublicId);
  if (query.unitPublicId) parameters.set('unitPublicId', query.unitPublicId);
  return parameters.size ? `?${parameters.toString()}` : '';
}

export async function getDepartmentMastersPage(query: OrganizationMasterPageQuery = {}): Promise<ApiResponse<PagedResult<DepartmentMasterDto>>> {
  return get<PagedResult<DepartmentMasterDto>>(`/v1/masters/departments/page${organizationMasterPageQuery(query)}`);
}

export async function saveDepartmentMaster(publicId: string | null, payload: { code: string; name: string; description?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<DepartmentMasterDto>> {
  return publicId ? put<DepartmentMasterDto>(`/v1/masters/departments/${publicId}`, payload) : post<DepartmentMasterDto>('/v1/masters/departments', payload);
}

export async function getUnitMasters(): Promise<ApiResponse<UnitMasterDto[]>> {
  return get<UnitMasterDto[]>('/v1/masters/units');
}

export async function getUnitMastersPage(query: OrganizationMasterPageQuery = {}): Promise<ApiResponse<PagedResult<UnitMasterDto>>> {
  return get<PagedResult<UnitMasterDto>>(`/v1/masters/units/page${organizationMasterPageQuery(query)}`);
}

export async function saveUnitMaster(publicId: string | null, payload: { departmentPublicId: string; code: string; name: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<UnitMasterDto>> {
  return publicId ? put<UnitMasterDto>(`/v1/masters/units/${publicId}`, payload) : post<UnitMasterDto>('/v1/masters/units', payload);
}

export async function getPositionMasters(): Promise<ApiResponse<PositionMasterDto[]>> {
  return get<PositionMasterDto[]>('/v1/masters/positions');
}

export async function getPositionMastersPage(query: OrganizationMasterPageQuery = {}): Promise<ApiResponse<PagedResult<PositionMasterDto>>> {
  return get<PagedResult<PositionMasterDto>>(`/v1/masters/positions/page${organizationMasterPageQuery(query)}`);
}

export async function savePositionMaster(publicId: string | null, payload: { departmentPublicId: string; unitPublicId?: string | null; code: string; name: string; grade?: string | null; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<PositionMasterDto>> {
  return publicId ? put<PositionMasterDto>(`/v1/masters/positions/${publicId}`, payload) : post<PositionMasterDto>('/v1/masters/positions', payload);
}

export async function getWardMasters(): Promise<ApiResponse<import('../types').WardMasterDto[]>> {
  return get<import('../types').WardMasterDto[]>('/v1/masters/wards');
}

export async function getWardMastersPage(query: OrganizationMasterPageQuery = {}): Promise<ApiResponse<PagedResult<import('../types').WardMasterDto>>> {
  return get<PagedResult<import('../types').WardMasterDto>>(`/v1/masters/wards/page${organizationMasterPageQuery(query)}`);
}

export async function saveWardMaster(publicId: string | null, payload: { code: string; name: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; reason: string; rowVersion?: string | null }): Promise<ApiResponse<import('../types').WardMasterDto>> {
  return publicId ? put<import('../types').WardMasterDto>(`/v1/masters/wards/${publicId}`, payload) : post<import('../types').WardMasterDto>('/v1/masters/wards', payload);
}

export async function getVoteNumberMasters(): Promise<ApiResponse<import('../types').VoteNumberMasterDto[]>> {
  return get<import('../types').VoteNumberMasterDto[]>('/v1/masters/vote-numbers');
}

export async function getVoteNumberMastersPage(query: OrganizationMasterPageQuery = {}): Promise<ApiResponse<PagedResult<import('../types').VoteNumberMasterDto>>> {
  return get<PagedResult<import('../types').VoteNumberMasterDto>>(`/v1/masters/vote-numbers/page${organizationMasterPageQuery(query)}`);
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

export async function revisePerformancePeriodTarget(publicId: string, payload: { unitKind: number; direction: number; targetValue: string; budgetValue?: number; description?: string; isTargetRevised: boolean; isBudgetRevised: boolean; isActive: boolean; reason: string; approvalReference: string; effectiveAt: string; rowVersion: string }): Promise<ApiResponse<PerformancePeriodTargetDto>> {
  return put<PerformancePeriodTargetDto>(`/v1/performance-period-targets/${publicId}`, payload);
}

export async function getTargetNormalizationPreview(targetKind: 1 | 2, page = 1, pageSize = 50): Promise<ApiResponse<PagedResult<TargetNormalizationPreviewDto>>> {
  return get<PagedResult<TargetNormalizationPreviewDto>>(`/v1/target-normalization?targetKind=${targetKind}&page=${page}&pageSize=${pageSize}`);
}

export async function executeTargetNormalization(payload: { targetKind: 1 | 2; targetPublicIds: string[]; reason: string }): Promise<ApiResponse<TargetNormalizationResultDto>> {
  return post<TargetNormalizationResultDto>('/v1/target-normalization/execute', payload);
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

export async function getInternalAuditConfigurations(): Promise<ApiResponse<InternalAuditConfigurationDto[]>> {
  return get<InternalAuditConfigurationDto[]>('/v1/internal-audit/configurations');
}

export async function saveInternalAuditConfiguration(payload: { municipalityFinancialYearPublicId: string; model: 1 | 2; effectiveFrom: string; reason: string; currentRowVersion?: string }): Promise<ApiResponse<InternalAuditConfigurationDto>> {
  return post<InternalAuditConfigurationDto>('/v1/internal-audit/configurations', payload);
}

export async function getInternalAuditSubmission(kind: 1 | 2, submissionId: string): Promise<ApiResponse<InternalAuditSubmissionDto>> {
  return get<InternalAuditSubmissionDto>(`/v1/internal-audit/submissions/${kind}/${encodeURIComponent(submissionId)}`);
}

export async function saveInternalAuditAssessment(kind: 1 | 2, submissionId: string, payload: { outcome: 1 | 2 | 3 | 4; detailedObservation: string; comment?: string; findings?: string; recommendation?: string; score?: number; responseDueAt?: string; previousAssessmentPublicId?: string }): Promise<ApiResponse<InternalAuditAssessmentDto>> {
  return post<InternalAuditAssessmentDto>(`/v1/internal-audit/submissions/${kind}/${encodeURIComponent(submissionId)}/assessments`, payload);
}

export async function getDepartments(): Promise<ApiResponse<DepartmentLookupDto[]>> {
  return get<DepartmentLookupDto[]>('/departments');
}

export async function getUnits(): Promise<ApiResponse<UnitLookupDto[]>> {
  return get<UnitLookupDto[]>('/units');
}

export async function getPerformanceLookups(): Promise<ApiResponse<PerformanceLookupsDto>> {
  return get<PerformanceLookupsDto>('/v1/performance-lookups');
}

export async function getRatingSchemes(): Promise<ApiResponse<RatingSchemeDto[]>> {
  return get<RatingSchemeDto[]>('/v1/workflow/rating-schemes');
}

export async function getSubmissionStageRatings(kind: number, submissionId: string): Promise<ApiResponse<StageRatingDto[]>> {
  return get<StageRatingDto[]>(`/v1/workflow/submissions/${kind}/${submissionId}/ratings`);
}

export async function getPendingNotificationDeliveries(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<NotificationOutboxItemDto>>> {
  return get<PagedResult<NotificationOutboxItemDto>>(`/v1/notification-operations/pending/page${registerPageQuery(query)}`);
}

export async function retryNotificationDelivery(item: NotificationOutboxItemDto, reason: string): Promise<ApiResponse<NotificationOutboxItemDto>> {
  return post<NotificationOutboxItemDto>(`/v1/notification-operations/${item.publicId}/retry`, { reason, rowVersion: item.rowVersion });
}

export async function getNotificationPolicies(): Promise<ApiResponse<NotificationPolicyDto[]>> {
  return get<NotificationPolicyDto[]>('/v1/notification-policies');
}

export async function createNotificationPolicy(payload: {
  municipalityFinancialYearPublicId: string; previousVersionPublicId?: string | null; code: string; name: string; scope: 1 | 2 | 3; source: 1 | 2;
  submissionKind?: 1 | 2 | null; workflowStageCode?: string | null; reportingPeriodPublicId?: string | null; isMandatory: boolean; deliveryPaused: boolean;
  channels: string[]; titleTemplate: string; messageTemplate: string; effectiveFrom: string; effectiveTo?: string | null;
  rules: Array<{ code: string; workingDayOffset: number; recipientKind: 1 | 2 | 3; recipientValues: string[] }>; reason: string;
}): Promise<ApiResponse<NotificationPolicyDto>> {
  return post<NotificationPolicyDto>('/v1/notification-policies', payload);
}

export async function activateNotificationPolicy(item: NotificationPolicyDto, reason: string): Promise<ApiResponse<NotificationPolicyDto>> {
  return post<NotificationPolicyDto>(`/v1/notification-policies/${item.publicId}/activate`, { rowVersion: item.rowVersion, reason });
}

export async function setNotificationPolicyDeliveryState(item: NotificationPolicyDto, paused: boolean, reason: string): Promise<ApiResponse<NotificationPolicyDto>> {
  return post<NotificationPolicyDto>(`/v1/notification-policies/${item.publicId}/delivery-state`, { paused, rowVersion: item.rowVersion, reason });
}

export async function copyNotificationPolicy(item: NotificationPolicyDto, municipalityFinancialYearPublicId: string, reason: string): Promise<ApiResponse<NotificationPolicyDto>> {
  return post<NotificationPolicyDto>(`/v1/notification-policies/${item.publicId}/copy-to-financial-year`, { municipalityFinancialYearPublicId, reason });
}

export async function previewNotificationPolicy(item: NotificationPolicyDto): Promise<ApiResponse<NotificationTemplatePreviewDto>> {
  return post<NotificationTemplatePreviewDto>(`/v1/notification-policies/${item.publicId}/preview`, { item: 'Performance submission', period: item.reportingPeriodName ?? 'Quarter 1', deadlineAt: new Date().toISOString(), workingDayOffset: item.rules[0]?.workingDayOffset ?? 0 });
}

export async function testNotificationPolicy(item: NotificationPolicyDto): Promise<ApiResponse<boolean>> {
  return post<boolean>(`/v1/notification-policies/${item.publicId}/test`, { item: 'Performance submission', period: item.reportingPeriodName ?? 'Quarter 1', deadlineAt: new Date().toISOString(), workingDayOffset: item.rules[0]?.workingDayOffset ?? 0 });
}

export async function runDueNotificationPolicies(): Promise<ApiResponse<number>> {
  return post<number>('/v1/notification-policies/run-due', {});
}

export async function getWorkingCalendarHolidays(): Promise<ApiResponse<WorkingCalendarHolidayDto[]>> {
  return get<WorkingCalendarHolidayDto[]>('/v1/notification-policies/holidays');
}

export async function addWorkingCalendarHoliday(payload: { municipalityFinancialYearPublicId: string; date: string; name: string; reason: string }): Promise<ApiResponse<WorkingCalendarHolidayDto>> {
  return post<WorkingCalendarHolidayDto>('/v1/notification-policies/holidays', payload);
}

export async function getMyNotificationPreferences(): Promise<ApiResponse<NotificationPreferenceDto>> {
  return get<NotificationPreferenceDto>('/v1/notification-policies/preferences/me');
}

export async function saveMyNotificationPreferences(payload: NotificationPreferenceDto): Promise<ApiResponse<NotificationPreferenceDto>> {
  return put<NotificationPreferenceDto>('/v1/notification-policies/preferences/me', payload);
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

export async function getOfficialReportTemplates(kind: 1 | 2, includeHistory = false): Promise<ApiResponse<OfficialReportTemplateDto[]>> {
  return get<OfficialReportTemplateDto[]>(`/v1/reports/official/templates?kind=${kind}&includeHistory=${includeHistory}`);
}

export async function getOfficialReportTemplatesPage(kind: 1 | 2, includeHistory = false, municipalityFinancialYearPublicId?: string, page: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OfficialReportTemplateDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(page).slice(1));
  parameters.set('kind', String(kind));
  parameters.set('includeHistory', String(includeHistory));
  if (municipalityFinancialYearPublicId) parameters.set('municipalityFinancialYearPublicId', municipalityFinancialYearPublicId);
  return get<PagedResult<OfficialReportTemplateDto>>(`/v1/reports/official/templates/page?${parameters.toString()}`);
}

export async function saveOfficialReportTemplate(payload: {
  previousVersionPublicId?: string | null;
  previousVersionRowVersion?: string | null;
  municipalityFinancialYearPublicId?: string | null;
  submissionKind: 1 | 2;
  code: string;
  name: string;
  format: OfficialReportFormat;
  reportType: OfficialReportType;
  headingTemplate: string;
  columns: string[];
  effectiveFrom: string;
  effectiveTo?: string | null;
  approvalReference: string;
  reason: string;
}): Promise<ApiResponse<OfficialReportTemplateDto>> {
  return post<OfficialReportTemplateDto>('/v1/reports/official/templates', payload);
}

export async function getOfficialReportGenerations(kind: 1 | 2, reportingPeriodPublicId?: string): Promise<ApiResponse<OfficialReportGenerationDto[]>> {
  const query = new URLSearchParams({ kind: String(kind) });
  if (reportingPeriodPublicId) query.set('reportingPeriodPublicId', reportingPeriodPublicId);
  return get<OfficialReportGenerationDto[]>(`/v1/reports/official/generations?${query}`);
}

export async function getOfficialReportGenerationsPage(kind: 1 | 2, reportingPeriodPublicId?: string, page: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OfficialReportGenerationDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(page).slice(1));
  parameters.set('kind', String(kind));
  if (reportingPeriodPublicId) parameters.set('reportingPeriodPublicId', reportingPeriodPublicId);
  return get<PagedResult<OfficialReportGenerationDto>>(`/v1/reports/official/generations/page?${parameters.toString()}`);
}

export async function generateOfficialReport(payload: { templatePublicId: string; municipalityFinancialYearPublicId: string; reportingPeriodPublicId: string; previousGenerationPublicId?: string | null; departmentPublicId?: string | null; unitPublicId?: string | null }): Promise<ApiResponse<OfficialReportGenerationDto>> {
  return post<OfficialReportGenerationDto>('/v1/reports/official/generations', payload);
}

export async function getOfficialReportJobsPage(kind: 1 | 2, query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OfficialReportJobDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  parameters.set('kind', String(kind));
  return get<PagedResult<OfficialReportJobDto>>(`/v1/reports/official/jobs/page?${parameters.toString()}`);
}

export async function queueOfficialReportJob(payload: { templatePublicId: string; municipalityFinancialYearPublicId: string; reportingPeriodPublicId: string; previousGenerationPublicId?: string | null; departmentPublicId?: string | null; unitPublicId?: string | null }): Promise<ApiResponse<OfficialReportJobDto>> {
  return post<OfficialReportJobDto>('/v1/reports/official/jobs', payload);
}

export async function retryOfficialReportJob(publicId: string, reason: string, rowVersion: string): Promise<ApiResponse<OfficialReportJobDto>> {
  return post<OfficialReportJobDto>(`/v1/reports/official/jobs/${encodeURIComponent(publicId)}/retry`, { reason, rowVersion });
}

export async function getOfficialReportSchedules(kind: 1 | 2, includeHistory = false): Promise<ApiResponse<OfficialReportScheduleDto[]>> {
  return get<OfficialReportScheduleDto[]>(`/v1/reports/official/schedules?kind=${kind}&includeHistory=${includeHistory}`);
}

export async function getOfficialReportSchedulesPage(kind: 1 | 2, includeHistory = false, query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OfficialReportScheduleDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  parameters.set('kind', String(kind));
  parameters.set('includeHistory', String(includeHistory));
  return get<PagedResult<OfficialReportScheduleDto>>(`/v1/reports/official/schedules/page?${parameters.toString()}`);
}

export async function saveOfficialReportSchedule(payload: {
  previousVersionPublicId?: string | null;
  previousVersionRowVersion?: string | null;
  templatePublicId: string;
  municipalityFinancialYearPublicId: string;
  reportingPeriodPublicId: string;
  departmentPublicId?: string | null;
  unitPublicId?: string | null;
  code: string;
  name: string;
  cadence: OfficialReportScheduleCadence;
  interval: number;
  nextRunAt?: string | null;
  effectiveTo?: string | null;
  recipientKind: OfficialReportRecipientKind;
  recipientValues: string[];
  channels: string[];
  isMandatory: boolean;
  isActive: boolean;
  approvalReference: string;
  reason: string;
}): Promise<ApiResponse<OfficialReportScheduleDto>> {
  return post<OfficialReportScheduleDto>('/v1/reports/official/schedules', payload);
}

export async function runOfficialReportSchedule(publicId: string): Promise<ApiResponse<OfficialReportJobDto>> {
  return post<OfficialReportJobDto>(`/v1/reports/official/schedules/${encodeURIComponent(publicId)}/run`, {});
}

export async function downloadOfficialReport(publicId: string, fileName: string): Promise<ApiResponse<boolean>> {
  const headers: Record<string, string> = {};
  addTenantHeader(headers);
  const response = await fetch(`${API_BASE_URL}/v1/reports/official/generations/${encodeURIComponent(publicId)}/content`, { headers, credentials: 'include' });
  if (!response.ok) return readApiResponse<boolean>(response);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url; anchor.download = fileName; anchor.click();
  URL.revokeObjectURL(url);
  return { success: true, data: true };
}

export async function getUsers(): Promise<ApiResponse<AdminUserDetail[]>> {
  return get<AdminUserDetail[]>('/users');
}

export async function getUsersPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<AdminUserDetail>>> {
  return get<PagedResult<AdminUserDetail>>(`/users/page${registerPageQuery(query)}`);
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

export async function getSecurityUsersPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<SecurityUserSummary>>> {
  return get<PagedResult<SecurityUserSummary>>(`/v1/security/users/page${registerPageQuery(query)}`);
}

export async function getSecurityUserRoles(userId: string): Promise<ApiResponse<SecurityUserRoleConfiguration>> {
  return get<SecurityUserRoleConfiguration>(`/v1/security/users/${userId}/roles`);
}

export async function saveSecurityUserRoles(userId: string, current: SecurityUserRoleConfiguration, assignments: Array<{ roleId: string; municipalityId?: number; departmentId?: number; departmentPublicId?: string; unitId?: number; unitPublicId?: string; effectiveFrom?: string; effectiveTo?: string }>): Promise<ApiResponse<boolean>> {
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

export async function getLoginAuditLogs(query: RegisterPageQuery = {}, failuresOnly = false): Promise<ApiResponse<PagedResult<LoginAuditLog>>> {
  const pageQuery = registerPageQuery(query);
  const suffix = failuresOnly ? `${pageQuery || '?'}${pageQuery ? '&' : ''}failuresOnly=true` : pageQuery;
  return get<PagedResult<LoginAuditLog>>(`/v1/audit/login-logs/page${suffix}`);
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
  departmentPublicId?: string | null;
  unitId?: number | null;
  unitPublicId?: string | null;
  targetId?: string | null;
  kpiId?: string | null;
  projectId?: string | null;
  taskId?: string | null;
  permissionCode: string;
}): Promise<ApiResponse<AccessSimulationResult>> {
  return post<AccessSimulationResult>('/access/simulate', payload);
}

export async function getOpmsTargetTemplates(): Promise<ApiResponse<OpmsTargetTemplate[]>> {
  const response = await get<OpmsTargetTemplateDto[]>('/v1/opms-target-library');
  return mapResponse(response, items => items.map(toOpmsTemplateModel));
}

export type TargetLibraryPageQuery = RegisterPageQuery & {
  status?: 'all' | 'active' | 'archived';
  primaryArea?: string;
  functionalArea?: string;
  classification?: string;
  targetUnitType?: string;
  version?: number;
};

function targetLibraryPageQuery(query: TargetLibraryPageQuery) {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  if (query.status) parameters.set('status', query.status);
  if (query.primaryArea) parameters.set('primaryArea', query.primaryArea);
  if (query.functionalArea) parameters.set('functionalArea', query.functionalArea);
  if (query.classification) parameters.set('classification', query.classification);
  if (query.targetUnitType) parameters.set('targetUnitType', query.targetUnitType);
  if (query.version) parameters.set('version', String(query.version));
  return `?${parameters.toString()}`;
}

export async function getOpmsTargetTemplatesPage(query: TargetLibraryPageQuery = {}): Promise<ApiResponse<PagedResult<OpmsTargetTemplate>>> {
  const response = await get<PagedResult<OpmsTargetTemplateDto>>(`/v1/opms-target-library/page${targetLibraryPageQuery(query)}`);
  return mapResponse(response, page => ({ ...page, items: page.items.map(toOpmsTemplateModel) }));
}

export async function getOpmsTargetTemplateFacets(): Promise<ApiResponse<TargetLibraryFacets>> {
  return get<TargetLibraryFacets>('/v1/opms-target-library/facets');
}

export async function getOpmsTargetTemplate(id: string | number): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await get<OpmsTargetTemplateDto>(`/v1/opms-target-library/${id}`);
  return mapResponse(response, toOpmsTemplateModel);
}

export async function createOpmsTargetTemplate(payload: SaveOpmsTargetTemplatePayload): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await post<OpmsTargetTemplateDto>('/v1/opms-target-library', toOpmsTemplatePayload(payload));
  return mapResponse(response, toOpmsTemplateModel);
}

export async function updateOpmsTargetTemplate(id: string | number, payload: SaveOpmsTargetTemplatePayload): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await put<OpmsTargetTemplateDto>(`/v1/opms-target-library/${id}`, toOpmsTemplatePayload(payload));
  return mapResponse(response, toOpmsTemplateModel);
}

export async function archiveOpmsTargetTemplate(id: string | number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/v1/opms-target-library/${id}`);
}

export async function duplicateOpmsTargetTemplate(id: string | number): Promise<ApiResponse<OpmsTargetTemplate>> {
  const response = await post<OpmsTargetTemplateDto>(`/v1/opms-target-library/${id}/duplicate`);
  return mapResponse(response, toOpmsTemplateModel);
}

export async function getIpmsTargetTemplates(): Promise<ApiResponse<IpmsTargetTemplate[]>> {
  const response = await get<IpmsTargetTemplateDto[]>('/v1/ipms-target-library');
  return mapResponse(response, items => items.map(toIpmsTemplateModel));
}

export async function getIpmsTargetTemplatesPage(query: TargetLibraryPageQuery = {}): Promise<ApiResponse<PagedResult<IpmsTargetTemplate>>> {
  const response = await get<PagedResult<IpmsTargetTemplateDto>>(`/v1/ipms-target-library/page${targetLibraryPageQuery(query)}`);
  return mapResponse(response, page => ({ ...page, items: page.items.map(toIpmsTemplateModel) }));
}

export async function getIpmsTargetTemplateFacets(): Promise<ApiResponse<TargetLibraryFacets>> {
  return get<TargetLibraryFacets>('/v1/ipms-target-library/facets');
}

export async function getIpmsTargetTemplate(id: string | number): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await get<IpmsTargetTemplateDto>(`/v1/ipms-target-library/${id}`);
  return mapResponse(response, toIpmsTemplateModel);
}

export async function createIpmsTargetTemplate(payload: SaveIpmsTargetTemplatePayload): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await post<IpmsTargetTemplateDto>('/v1/ipms-target-library', toIpmsTemplatePayload(payload));
  return mapResponse(response, toIpmsTemplateModel);
}

export async function updateIpmsTargetTemplate(id: string | number, payload: SaveIpmsTargetTemplatePayload): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await put<IpmsTargetTemplateDto>(`/v1/ipms-target-library/${id}`, toIpmsTemplatePayload(payload));
  return mapResponse(response, toIpmsTemplateModel);
}

export async function archiveIpmsTargetTemplate(id: string | number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/v1/ipms-target-library/${id}`);
}

export async function duplicateIpmsTargetTemplate(id: string | number): Promise<ApiResponse<IpmsTargetTemplate>> {
  const response = await post<IpmsTargetTemplateDto>(`/v1/ipms-target-library/${id}/duplicate`);
  return mapResponse(response, toIpmsTemplateModel);
}

export async function getOpmsPerformanceDashboard(): Promise<ApiResponse<PerformanceDashboardDto>> {
  return get<PerformanceDashboardDto>('/v1/performance-dashboards/opms');
}

export async function getIpmsPerformanceDashboard(): Promise<ApiResponse<PerformanceDashboardDto>> {
  return get<PerformanceDashboardDto>('/v1/performance-dashboards/ipms');
}

export async function getWorkflowQueue(queue: WorkflowQueueName, page = 1, pageSize = 25): Promise<ApiResponse<WorkflowQueueDto>> {
  const parameters = new URLSearchParams({ queue, page: String(page), pageSize: String(pageSize) });
  return get<WorkflowQueueDto>(`/v1/workflow-queues?${parameters.toString()}`);
}

export type RegisterPageQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  targetPublicId?: string;
  relatedOpmsTargetPublicId?: string;
  departmentPublicId?: string;
  lifecycle?: 'active' | 'revised' | 'withdrawn';
  reportingPeriodType?: 1 | 2 | 3 | 4 | 5 | 6;
};

function registerPageQuery(query: RegisterPageQuery): string {
  const parameters = new URLSearchParams();
  if (query.page !== undefined) parameters.set('page', String(query.page));
  if (query.pageSize !== undefined) parameters.set('pageSize', String(query.pageSize));
  if (query.search?.trim()) parameters.set('search', query.search.trim());
  if (query.sortBy?.trim()) parameters.set('sortBy', query.sortBy.trim());
  if (query.sortDirection) parameters.set('sortDirection', query.sortDirection);
  if (query.targetPublicId?.trim()) parameters.set('targetPublicId', query.targetPublicId.trim());
  if (query.relatedOpmsTargetPublicId?.trim()) parameters.set('relatedOpmsTargetPublicId', query.relatedOpmsTargetPublicId.trim());
  if (query.departmentPublicId?.trim()) parameters.set('departmentPublicId', query.departmentPublicId.trim());
  if (query.lifecycle) parameters.set('lifecycle', query.lifecycle);
  if (query.reportingPeriodType !== undefined) parameters.set('reportingPeriodType', String(query.reportingPeriodType));
  const value = parameters.toString();
  return value ? `?${value}` : '';
}

export async function getOpmsTargetsPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OPMSTarget>>> {
  const response = await get<PagedResult<OpmsTargetDto>>(`/v1/opms-targets/page${registerPageQuery(query)}`);
  return mapResponse(response, page => ({ ...page, items: page.items.map(toOpmsTargetModel) }));
}

export function getOpmsTargetOptions(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<PerformanceTargetOptionDto>>> {
  return get<PagedResult<PerformanceTargetOptionDto>>(`/v1/opms-targets/options${registerPageQuery(query)}`);
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

export async function reviseOpmsTargetOrdering(id: string, payload: { originalOrderNumber: number; revisedOrderNumber: number; reason: string; approvalReference: string; effectiveAt: string; rowVersion: string }): Promise<ApiResponse<OPMSTarget>> {
  const response = await put<OpmsTargetDto>(`/v1/opms-targets/${id}/ordering`, payload);
  return mapResponse(response, toOpmsTargetModel);
}

export type ReviseKpiDefinitionPayload = {
  isIndicatorNumberRevised: boolean;
  revisedIndicatorNumber?: string;
  isTargetNameRevised: boolean;
  revisedTargetName?: string;
  isKpiDescriptionRevised: boolean;
  revisedKpiDescription?: string;
  reason: string;
  approvalReference: string;
  effectiveAt: string;
  rowVersion: string;
};

export async function reviseOpmsTargetDefinition(id: string, payload: ReviseKpiDefinitionPayload): Promise<ApiResponse<OPMSTarget>> {
  const response = await put<OpmsTargetDto>(`/v1/opms-targets/${id}/field-revisions`, payload);
  return mapResponse(response, toOpmsTargetModel);
}

export function getOpmsTargetFieldRevisions(id: string): Promise<ApiResponse<KpiFieldRevisionDto[]>> {
  return get<KpiFieldRevisionDto[]>(`/v1/opms-targets/${id}/field-revisions`);
}

export function getOpmsTargetOrderingRevisions(id: string): Promise<ApiResponse<KpiFieldRevisionDto[]>> {
  return get<KpiFieldRevisionDto[]>(`/v1/opms-targets/${id}/ordering-revisions`);
}

export async function getIpmsTargetsPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<IPMSTarget>>> {
  const response = await get<PagedResult<IpmsTargetDto>>(`/v1/ipms-targets/page${registerPageQuery(query)}`);
  return mapResponse(response, page => ({ ...page, items: page.items.map(toIpmsTargetModel) }));
}

export function getIpmsTargetOptions(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<PerformanceTargetOptionDto>>> {
  return get<PagedResult<PerformanceTargetOptionDto>>(`/v1/ipms-targets/options${registerPageQuery(query)}`);
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

export async function reviseIpmsTargetOrdering(id: string, payload: { originalOrderNumber: number; revisedOrderNumber: number; reason: string; approvalReference: string; effectiveAt: string; rowVersion: string }): Promise<ApiResponse<IPMSTarget>> {
  const response = await put<IpmsTargetDto>(`/v1/ipms-targets/${id}/ordering`, payload);
  return mapResponse(response, toIpmsTargetModel);
}

export async function reviseIpmsTargetDefinition(id: string, payload: ReviseKpiDefinitionPayload): Promise<ApiResponse<IPMSTarget>> {
  const response = await put<IpmsTargetDto>(`/v1/ipms-targets/${id}/field-revisions`, payload);
  return mapResponse(response, toIpmsTargetModel);
}

export function getIpmsTargetFieldRevisions(id: string): Promise<ApiResponse<KpiFieldRevisionDto[]>> {
  return get<KpiFieldRevisionDto[]>(`/v1/ipms-targets/${id}/field-revisions`);
}

export function getIpmsTargetOrderingRevisions(id: string): Promise<ApiResponse<KpiFieldRevisionDto[]>> {
  return get<KpiFieldRevisionDto[]>(`/v1/ipms-targets/${id}/ordering-revisions`);
}

export async function getOpmsSubmissionsPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<OPMSSubmission>>> {
  const response = await get<PagedResult<OpmsSubmissionDto>>(`/v1/opms-submissions/page${registerPageQuery(query)}`);
  return mapResponse(response, page => ({
    ...page,
    items: page.items.map(toOpmsSubmissionModel),
  }));
}

export async function getOpmsSubmission(id: string): Promise<ApiResponse<OPMSSubmission>> {
  const response = await get<OpmsSubmissionDto>(`/opms-submissions/${id}`);
  return mapResponse(response, toOpmsSubmissionModel);
}

export async function createOpmsSubmission(payload: SaveOpmsSubmissionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const response = await post<OpmsSubmissionDto>('/opms-submissions', payload);
  return mapResponse(response, toOpmsSubmissionModel);
}

export async function updateOpmsSubmission(id: string, payload: SaveOpmsSubmissionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const response = await put<OpmsSubmissionDto>(`/opms-submissions/${id}`, payload);
  return mapResponse(response, toOpmsSubmissionModel);
}

export async function withdrawOpmsSubmission(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<OPMSSubmission>> {
  const response = await post<OpmsSubmissionDto>(`/v1/opms-submissions/${id}/withdraw`, payload);
  return mapResponse(response, toOpmsSubmissionModel);
}

export const generateOpmsConsolidationSuggestion = (id: string): Promise<ApiResponse<PerformanceSuggestionResult>> =>
  post<PerformanceSuggestionResult>(`/v1/opms-submissions/${id}/consolidation-suggestion`);

export const saveOpmsConsolidatedActual = (id: string, payload: { actualPerformance: string; editReason?: string; rowVersion: string }): Promise<ApiResponse<PerformanceSuggestionResult>> =>
  put<PerformanceSuggestionResult>(`/v1/opms-submissions/${id}/consolidated-actual`, payload);

export const getOpmsConsolidationHistory = (id: string): Promise<ApiResponse<PerformanceSuggestionEvent[]>> =>
  get<PerformanceSuggestionEvent[]>(`/v1/opms-submissions/${id}/consolidation-history`);

export async function applyOpmsSubmissionWorkflowAction(
  id: string,
  action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score',
  payload: SubmissionWorkflowActionPayload,
): Promise<ApiResponse<OPMSSubmission>> {
  const response = await post<OpmsSubmissionDto>(`/opms-submissions/${id}/${action}`, payload);
  return mapResponse(response, toOpmsSubmissionModel);
}

export async function extendOpmsSubmissionDueDate(id: string, payload: DueDateExtensionPayload): Promise<ApiResponse<OPMSSubmission>> {
  const response = await post<OpmsSubmissionDto>(`/opms-submissions/${id}/extend-due-date`, payload);
  return mapResponse(response, toOpmsSubmissionModel);
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

export async function getIpmsSubmissionsPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<IPMSSubmission>>> {
  const response = await get<PagedResult<IpmsSubmissionDto>>(`/v1/ipms-submissions/page${registerPageQuery(query)}`);
  return mapResponse(response, page => ({
    ...page,
    items: page.items.map(toIpmsSubmissionModel),
  }));
}

export async function getIpmsSubmission(id: string): Promise<ApiResponse<IPMSSubmission>> {
  const response = await get<IpmsSubmissionDto>(`/ipms-submissions/${id}`);
  return mapResponse(response, toIpmsSubmissionModel);
}

export async function createIpmsSubmission(payload: SaveIpmsSubmissionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const response = await post<IpmsSubmissionDto>('/ipms-submissions', payload);
  return mapResponse(response, toIpmsSubmissionModel);
}

export async function updateIpmsSubmission(id: string, payload: SaveIpmsSubmissionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const response = await put<IpmsSubmissionDto>(`/ipms-submissions/${id}`, payload);
  return mapResponse(response, toIpmsSubmissionModel);
}

export async function withdrawIpmsSubmission(id: string, payload: { reason: string; rowVersion: string }): Promise<ApiResponse<IPMSSubmission>> {
  const response = await post<IpmsSubmissionDto>(`/v1/ipms-submissions/${id}/withdraw`, payload);
  return mapResponse(response, toIpmsSubmissionModel);
}

export const generateIpmsConsolidationSuggestion = (id: string): Promise<ApiResponse<PerformanceSuggestionResult>> =>
  post<PerformanceSuggestionResult>(`/v1/ipms-submissions/${id}/consolidation-suggestion`);

export const saveIpmsConsolidatedActual = (id: string, payload: { actualPerformance: string; editReason?: string; rowVersion: string }): Promise<ApiResponse<PerformanceSuggestionResult>> =>
  put<PerformanceSuggestionResult>(`/v1/ipms-submissions/${id}/consolidated-actual`, payload);

export const getIpmsConsolidationHistory = (id: string): Promise<ApiResponse<PerformanceSuggestionEvent[]>> =>
  get<PerformanceSuggestionEvent[]>(`/v1/ipms-submissions/${id}/consolidation-history`);

export async function applyIpmsSubmissionWorkflowAction(
  id: string,
  action: 'submit' | 'verify' | 'verify-reject' | 'approve' | 'reject' | 'review' | 'audit' | 'score',
  payload: SubmissionWorkflowActionPayload,
): Promise<ApiResponse<IPMSSubmission>> {
  const response = await post<IpmsSubmissionDto>(`/ipms-submissions/${id}/${action}`, payload);
  return mapResponse(response, toIpmsSubmissionModel);
}

export async function extendIpmsSubmissionDueDate(id: string, payload: DueDateExtensionPayload): Promise<ApiResponse<IPMSSubmission>> {
  const response = await post<IpmsSubmissionDto>(`/ipms-submissions/${id}/extend-due-date`, payload);
  return mapResponse(response, toIpmsSubmissionModel);
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

export async function getNotifications(query: RegisterPageQuery = {}, includeAll = false): Promise<ApiResponse<NotificationPageResult>> {
  const pageQuery = registerPageQuery(query);
  const suffix = includeAll ? `${pageQuery || '?'}${pageQuery ? '&' : ''}includeAll=true` : pageQuery;
  return get<NotificationPageResult>(`/v1/notifications/page${suffix}`);
}

export async function markNotificationRead(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/notifications/${id}/read`);
}

export async function getAuditTrails(take = 200, filter: { entityName?: string; entityId?: string } = {}): Promise<ApiResponse<AuditTrailEntryDto[]>> {
  const parameters = new URLSearchParams({ take: String(take) });
  if (filter.entityName?.trim()) parameters.set('entityName', filter.entityName.trim());
  if (filter.entityId?.trim()) parameters.set('entityId', filter.entityId.trim());
  return get<AuditTrailEntryDto[]>(`/v1/audit/trails?${parameters.toString()}`);
}

export async function getAuditTrailsPage(query: RegisterPageQuery = {}, filter: { entityName?: string; entityId?: string } = {}): Promise<ApiResponse<PagedResult<AuditTrailEntryDto>>> {
  const parameters = new URLSearchParams(registerPageQuery(query).slice(1));
  if (filter.entityName?.trim()) parameters.set('entityName', filter.entityName.trim());
  if (filter.entityId?.trim()) parameters.set('entityId', filter.entityId.trim());
  const value = parameters.toString();
  return get<PagedResult<AuditTrailEntryDto>>(`/v1/audit/trails/page${value ? `?${value}` : ''}`);
}

export async function getIdpPlansPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<IdpPlanSummary>>> {
  return get<PagedResult<IdpPlanSummary>>(`/idp/plans/page${registerPageQuery(query)}`);
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

export async function stageIdpHierarchyImport(
  planPublicId: string,
  payload: { clientRequestId: string; sourceFileName: string; rows: import('../types').IdpHierarchyImportRowPayload[] },
): Promise<ApiResponse<import('../types').IdpImportBatch>> {
  return post<import('../types').IdpImportBatch>(`/v1/idp/plans/${planPublicId}/imports/hierarchy/stage`, payload);
}

export async function commitIdpImport(
  batchPublicId: string,
  payload: { rowVersion: string; reason: string },
): Promise<ApiResponse<import('../types').IdpImportBatch>> {
  return post<import('../types').IdpImportBatch>(`/v1/idp/imports/${batchPublicId}/commit`, payload);
}

export async function commitIdpHierarchyImport(
  batchPublicId: string,
  payload: { rowVersion: string; reason: string },
): Promise<ApiResponse<import('../types').IdpImportBatch>> {
  return post<import('../types').IdpImportBatch>(`/v1/idp/imports/${batchPublicId}/commit-hierarchy`, payload);
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

export async function getTidConfiguration(): Promise<ApiResponse<TidConfiguration>> {
  return get<TidConfiguration>('/v1/tids/configuration');
}

export async function updateTidConfiguration(payload: { tidEnabled: boolean; allKpisRequired: boolean; rowVersion: string; reason: string }): Promise<ApiResponse<TidConfiguration>> {
  return put<TidConfiguration>('/v1/tids/configuration', payload);
}

export async function getTidRegister(search?: string): Promise<ApiResponse<TidRegisterItem[]>> {
  const query = search?.trim() ? `?search=${encodeURIComponent(search.trim())}` : '';
  return get<TidRegisterItem[]>(`/v1/tids${query}`);
}

export async function getTidRegisterPage(query: RegisterPageQuery = {}): Promise<ApiResponse<PagedResult<TidRegisterItem>>> {
  return get<PagedResult<TidRegisterItem>>(`/v1/tids/page${registerPageQuery(query)}`);
}

export async function getTidHistory(targetPublicId: string): Promise<ApiResponse<TidVersion[]>> {
  return get<TidVersion[]>(`/v1/tids/targets/${targetPublicId}`);
}

export async function createTidVersion(targetPublicId: string, payload: SaveTidVersionPayload): Promise<ApiResponse<TidVersion>> {
  return post<TidVersion>(`/v1/tids/targets/${targetPublicId}/versions`, payload);
}

export async function uploadTidSourceDocument(tidPublicId: string, file: File, title: string): Promise<ApiResponse<TidSourceDocument>> {
  const form = new FormData();
  form.append('file', file);
  form.append('title', title);
  return postForm<TidSourceDocument>(`/v1/tids/${tidPublicId}/documents`, form);
}

export async function rescanTidSourceDocument(tidPublicId: string, documentPublicId: string): Promise<ApiResponse<TidSourceDocument>> {
  return post<TidSourceDocument>(`/v1/tids/${tidPublicId}/documents/${documentPublicId}/rescan`);
}

export async function downloadTidSourceDocument(document: TidSourceDocument): Promise<ApiResponse<boolean>> {
  const headers: Record<string, string> = {};
  addTenantHeader(headers);
  const response = await fetch(document.contentUrl, { headers, credentials: 'include' });
  if (!response.ok) return readApiResponse<boolean>(response);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = window.document.createElement('a');
  anchor.href = url;
  anchor.download = document.fileName;
  anchor.click();
  URL.revokeObjectURL(url);
  return { success: true, data: true };
}

export async function getStrategicDocumentTypes(includeInactive = false): Promise<ApiResponse<StrategicDocumentType[]>> {
  return get<StrategicDocumentType[]>(`/v1/strategic-documents/types${includeInactive ? '?includeInactive=true' : ''}`);
}

export async function createStrategicDocumentType(payload: {
  code: string; name: string; description?: string | null; allowsExternalLinks: boolean; isActive: boolean;
  displayOrder: number; reason: string;
}): Promise<ApiResponse<StrategicDocumentType>> {
  return post<StrategicDocumentType>('/v1/strategic-documents/types', payload);
}

export async function updateStrategicDocumentType(publicId: string, payload: {
  code: string; name: string; description?: string | null; allowsExternalLinks: boolean; isActive: boolean;
  displayOrder: number; rowVersion: string; reason: string;
}): Promise<ApiResponse<StrategicDocumentType>> {
  return put<StrategicDocumentType>(`/v1/strategic-documents/types/${publicId}`, payload);
}

export async function getStrategicDocuments(options?: {
  municipalityFinancialYearPublicId?: string; includeHistory?: boolean; search?: string;
}): Promise<ApiResponse<StrategicDocument[]>> {
  const query = new URLSearchParams();
  if (options?.municipalityFinancialYearPublicId) query.set('municipalityFinancialYearPublicId', options.municipalityFinancialYearPublicId);
  if (options?.includeHistory) query.set('includeHistory', 'true');
  if (options?.search?.trim()) query.set('search', options.search.trim());
  const suffix = query.size ? `?${query.toString()}` : '';
  return get<StrategicDocument[]>(`/v1/strategic-documents${suffix}`);
}

export async function getStrategicDocumentsPage(page: RegisterPageQuery = {}, options?: {
  municipalityFinancialYearPublicId?: string; includeHistory?: boolean;
}): Promise<ApiResponse<PagedResult<StrategicDocument>>> {
  const parameters = new URLSearchParams(registerPageQuery(page).slice(1));
  if (options?.municipalityFinancialYearPublicId) parameters.set('municipalityFinancialYearPublicId', options.municipalityFinancialYearPublicId);
  if (options?.includeHistory) parameters.set('includeHistory', 'true');
  const suffix = parameters.size ? `?${parameters.toString()}` : '';
  return get<PagedResult<StrategicDocument>>(`/v1/strategic-documents/page${suffix}`);
}

export async function getStrategicDocumentHistory(familyId: string): Promise<ApiResponse<StrategicDocument[]>> {
  return get<StrategicDocument[]>(`/v1/strategic-documents/families/${familyId}/versions`);
}

export async function createStrategicDocumentVersion(payload: SaveStrategicDocumentVersionPayload): Promise<ApiResponse<StrategicDocument>> {
  const form = new FormData();
  form.append('municipalityFinancialYearPublicId', payload.municipalityFinancialYearPublicId);
  form.append('documentTypePublicId', payload.documentTypePublicId);
  if (payload.previousVersionPublicId) form.append('previousVersionPublicId', payload.previousVersionPublicId);
  if (payload.previousVersionRowVersion) form.append('previousVersionRowVersion', payload.previousVersionRowVersion);
  if (payload.sdbipLayer) form.append('sdbipLayer', payload.sdbipLayer);
  form.append('title', payload.title);
  if (payload.description) form.append('description', payload.description);
  form.append('documentDate', payload.documentDate);
  form.append('displayOrder', String(payload.displayOrder));
  if (payload.externalUrl) form.append('externalUrl', payload.externalUrl);
  if (payload.file) form.append('file', payload.file);
  form.append('reason', payload.reason);
  return postForm<StrategicDocument>('/v1/strategic-documents/versions', form);
}

export async function approveStrategicDocument(publicId: string, payload: { rowVersion: string; approvalReference: string; reason: string }): Promise<ApiResponse<StrategicDocument>> {
  return post<StrategicDocument>(`/v1/strategic-documents/${publicId}/approve`, payload);
}

export async function publishStrategicDocument(publicId: string, payload: { rowVersion: string; publicationDate: string; reason: string }): Promise<ApiResponse<StrategicDocument>> {
  return post<StrategicDocument>(`/v1/strategic-documents/${publicId}/publish`, payload);
}

export async function retireStrategicDocument(publicId: string, payload: { rowVersion: string; reason: string }): Promise<ApiResponse<StrategicDocument>> {
  return post<StrategicDocument>(`/v1/strategic-documents/${publicId}/retire`, payload);
}

export async function rescanStrategicDocument(publicId: string): Promise<ApiResponse<StrategicDocument>> {
  return post<StrategicDocument>(`/v1/strategic-documents/${publicId}/rescan`);
}

export async function downloadStrategicDocument(document: StrategicDocument): Promise<ApiResponse<boolean>> {
  if (!document.contentUrl || !document.fileName) return { success: false, message: 'Managed document content is unavailable.' };
  const headers: Record<string, string> = {};
  addTenantHeader(headers);
  const response = await fetch(document.contentUrl, { headers, credentials: 'include' });
  if (!response.ok) return readApiResponse<boolean>(response);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = window.document.createElement('a');
  anchor.href = url;
  anchor.download = document.fileName;
  anchor.click();
  URL.revokeObjectURL(url);
  return { success: true, data: true };
}

export async function getC88Workspace(municipalityFinancialYearPublicId?: string, includeReports = true): Promise<ApiResponse<C88Workspace>> {
  const parameters = new URLSearchParams();
  if (municipalityFinancialYearPublicId) parameters.set('municipalityFinancialYearPublicId', municipalityFinancialYearPublicId);
  if (!includeReports) parameters.set('includeReports', 'false');
  const query = parameters.size ? `?${parameters.toString()}` : '';
  return get<C88Workspace>(`/v1/c88/workspace${query}`);
}

export async function getC88ReportsPage(query: RegisterPageQuery = {}, municipalityFinancialYearPublicId?: string): Promise<ApiResponse<PagedResult<C88IndicatorReport>>> {
  const pageQuery = registerPageQuery(query);
  const yearQuery = municipalityFinancialYearPublicId
    ? `${pageQuery ? '&' : '?'}municipalityFinancialYearPublicId=${encodeURIComponent(municipalityFinancialYearPublicId)}`
    : '';
  return get<PagedResult<C88IndicatorReport>>(`/v1/c88/reports/page${pageQuery}${yearQuery}`);
}

export const createC88CatalogueVersion = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/catalogue-versions', payload);
export const updateC88CatalogueVersion = (publicId: string, payload: unknown): Promise<ApiResponse<string>> => put<string>(`/v1/c88/catalogue-versions/${publicId}`, payload);
export const createC88CatalogueItem = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/catalogue-items', payload);
export const createC88Indicator = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/indicators', payload);
export const createC88ComplianceQuestion = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/compliance-questions', payload);
export const configureC88 = (payload: unknown): Promise<ApiResponse<string>> => put<string>('/v1/c88/configurations', payload);
export const saveC88IndicatorPlan = (payload: unknown): Promise<ApiResponse<string>> => put<string>('/v1/c88/plans', payload);
export const createC88Calendar = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/calendars', payload);
export const createC88Assignment = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/assignments', payload);
export const createC88Workflow = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/workflows', payload);
export const createC88Mapping = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/mappings', payload);
export const createC88ReportVersion = (payload: unknown): Promise<ApiResponse<string>> => post<string>('/v1/c88/reports', payload);
export const submitC88Report = (publicId: string, rowVersion: string, reason: string): Promise<ApiResponse<string>> => post<string>(`/v1/c88/reports/${publicId}/submit`, { rowVersion, reason });
export const verifyC88Report = (publicId: string, rowVersion: string, reason: string): Promise<ApiResponse<string>> => post<string>(`/v1/c88/reports/${publicId}/verify`, { rowVersion, reason });
export const returnC88Report = (publicId: string, rowVersion: string, reason: string): Promise<ApiResponse<string>> => post<string>(`/v1/c88/reports/${publicId}/return`, { rowVersion, reason });
export const finalSubmitC88Report = (publicId: string, rowVersion: string, reason: string): Promise<ApiResponse<string>> => post<string>(`/v1/c88/reports/${publicId}/final-submit`, { rowVersion, reason });
