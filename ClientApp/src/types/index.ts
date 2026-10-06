// Core Types for Performance Management System

export type UserRole =
  | 'system_admin'
  | 'pms_officer'
  | 'department_manager'
  | 'submitter'
  | 'verifier'
  | 'approver'
  | 'auditor'
  | 'hr_admin'
  | 'viewer';

export type SubmissionStatus =
  | 'draft'
  | 'submitted'
  | 'pending_verification'
  | 'verified'
  | 'verify_rejected'
  | 'pending_approval'
  | 'approved'
  | 'rejected'
  | 'reviewed'
  | 'returned_for_info'
  | 'audited'
  | 'completed';

export type TargetUnitType =
  | 'percentage'
  | 'absolute_count'
  | 'financial'
  | 'area_based'
  | 'volume_based'
  | 'index_scores'
  | 'ratios'
  | 'time_based'
  | 'binary'
  | 'date'
  | 'readiness_scale'
  | 'qualitative'
  | 'zero_based'
  | 'reverse_cumulative'
  | 'reverse_non_cumulative'
  | 'binary_determination'
  | 'None'
  | 'PercentageBased'
  | 'AbsoluteCount'
  | 'Financial'
  | 'TimeBased'
  | 'AreaBased'
  | 'VolumeBased'
  | 'IndexScores'
  | 'Ratios'
  | 'Binary'
  | 'Date'
  | 'ReadinessScale'
  | 'BinaryDetermination'
  | 'QualitativeTargets'
  | 'ZeroBased'
  | 'ReverseCumulative'
  | 'ReverseNonCumulative';

export type Quarter = 'Q1' | 'Q2' | 'Mid-Year' | 'Q3' | 'Q4' | 'Annual';

export type XafUnitValue =
  | 'None'
  | 'PercentageBased'
  | 'AbsoluteCount'
  | 'Financial'
  | 'TimeBased'
  | 'AreaBased'
  | 'VolumeBased'
  | 'IndexScores'
  | 'Ratios'
  | 'Binary'
  | 'Date'
  | 'ReadinessScale'
  | 'BinaryDetermination'
  | 'QualitativeTargets'
  | 'ZeroBased'
  | 'ReverseCumulative'
  | 'ReverseNonCumulative';

export interface TargetAuditMetaFields {
  Oid?: string;
  CreatedBy?: string;
  CreatedOn?: string;
  UpdatedBy?: string;
  UpdatedOn?: string;
  OrganisationId?: string;
}

export interface BaseKpiTargetXafFields {
  Period?: string;
  Department?: string;
  Unit?: string;
  Wards?: string[];
  AssignedTo?: string;
  IndicatorNo?: string;
  NationalKPA?: string;
  MunicipalKPA?: string;
  BackToBasicPillar?: string;
  StrategicRisk?: string;
  StrategicIntervention?: string;
  ProjectCode?: string;
  StrategicGoal?: string;
  StrategicObjective?: string;
  PerformanceObjective?: string;
  TargetName?: string;
  Outcome?: string;
  Output?: string;
  PriorityIssue?: string;
  KPIDescription?: string;
  KpiRevised?: boolean;
  KpiType?: string;
  RevisedKpi?: string;
  IndicatorType?: string;
  FunctionalArea?: string;
  Demand?: string;
  PriorYearAnnualTarget?: number;
  PriorYearAnnualTargetDescription?: string;
  Backlog?: string;
  Baseline?: number;
  DepartmentalObjective?: string;
  PriorityArea?: string;
  KeyFocusArea?: string;
  Strategies?: string;
  UnitOfMeasure?: string;
  CalculationTargetType?: XafUnitValue;
  TargetQ1?: number;
  TargetQ1Description?: string;
  TargetUnitQ1?: XafUnitValue;
  TotalBudgetQ1?: number;
  TargetQ2?: number;
  TargetQ2Description?: string;
  TargetUnitQ2?: XafUnitValue;
  TargetQ2Budget?: number;
  TargetMidTerm?: number;
  TargetMidTermDescription?: string;
  MidTermTargetUnit?: XafUnitValue;
  MidTermBudget?: number;
  TargetQ3?: number;
  TargetQ3Description?: string;
  TargetUnitQ3?: XafUnitValue;
  TargetQ3Budget?: number;
  RevisedTargetQ3Unit?: XafUnitValue;
  RevisedTargetQ3Budget?: number;
  RevisedTargetQ3?: number;
  RevisedTargetQ3Description?: string;
  TargetQ4?: number;
  TargetQ4Description?: string;
  TargetUnitQ4?: XafUnitValue;
  TargetQ4Budget?: number;
  RevisedTargetQ4Unit?: XafUnitValue;
  RevisedTargetQ4Budget?: number;
  RevisedTargetQ4?: number;
  RevisedTargetQ4Description?: string;
  AnnualTarget?: number;
  AnnualTargetDescription?: string;
  AnnualTargetUnit?: XafUnitValue;
  AnnualBudget?: number;
  RevisedAnnualBudget?: number;
  RevisedAnnualTargetUnit?: XafUnitValue;
  RevisedAnnualTarget?: number;
  RevisedAnnualTargetDescription?: string;
  BudgetSource?: string;
  BudgetType?: string;
  StandardClassification?: string;
  Weight?: number;
  IDPRef?: string;
  InternalRef?: string;
  FMSLink?: string;
  Layer?: string;
  IsDisabled?: boolean;
  OrderNumber?: number;
  IsWithdrawn?: boolean;
  ReasonForWithdrawal?: string;
}

export interface TypedTargetPeriodValues {
  PercentageQ1?: string;
  PercentageQ2?: string;
  PercentageMidTerm?: string;
  PercentageQ3?: string;
  PercentageQ4?: string;
  PercentageAnnual?: string;
  AbsoluteCountQ1?: string;
  AbsoluteCountQ2?: string;
  AbsoluteCountMidTerm?: string;
  AbsoluteCountQ3?: string;
  AbsoluteCountQ4?: string;
  AbsoluteCountAnnual?: string;
  FinancialQ1?: string;
  FinancialQ2?: string;
  FinancialMidTerm?: string;
  FinancialQ3?: string;
  FinancialQ4?: string;
  FinancialAnnual?: string;
  AreaBasedQ1?: string;
  AreaBasedQ2?: string;
  AreaBasedMidTerm?: string;
  AreaBasedQ3?: string;
  AreaBasedQ4?: string;
  AreaBasedAnnual?: string;
  VolumeBasedQ1?: string;
  VolumeBasedQ2?: string;
  VolumeBasedMidTerm?: string;
  VolumeBasedQ3?: string;
  VolumeBasedQ4?: string;
  VolumeBasedAnnual?: string;
  IndexScoresQ1?: string;
  IndexScoresQ2?: string;
  IndexScoresMidTerm?: string;
  IndexScoresQ3?: string;
  IndexScoresQ4?: string;
  IndexScoresAnnual?: string;
  RatiosQ1?: string;
  RatiosQ2?: string;
  RatiosMidTerm?: string;
  RatiosQ3?: string;
  RatiosQ4?: string;
  RatiosAnnual?: string;
  TimeBasedQ1?: string;
  TimeBasedQ2?: string;
  TimeBasedMidTerm?: string;
  TimeBasedQ3?: string;
  TimeBasedQ4?: string;
  TimeBasedAnnual?: string;
  BinaryQ1?: string;
  BinaryQ2?: string;
  BinaryMidTerm?: string;
  BinaryQ3?: string;
  BinaryQ4?: string;
  BinaryAnnual?: string;
  DateQ1?: string;
  DateQ2?: string;
  DateMidTerm?: string;
  DateQ3?: string;
  DateQ4?: string;
  DateAnnual?: string;
  ReadinessQ1?: string;
  ReadinessQ2?: string;
  ReadinessMidTerm?: string;
  ReadinessQ3?: string;
  ReadinessQ4?: string;
  ReadinessAnnual?: string;
  QualitativeQ1?: string;
  QualitativeQ2?: string;
  QualitativeMidTerm?: string;
  QualitativeQ3?: string;
  QualitativeQ4?: string;
  QualitativeAnnual?: string;
  ZeroBasedQ1?: string;
  ZeroBasedQ2?: string;
  ZeroBasedMidTerm?: string;
  ZeroBasedQ3?: string;
  ZeroBasedQ4?: string;
  ZeroBasedAnnual?: string;
  ReverseCumulativeQ1?: string;
  ReverseCumulativeQ2?: string;
  ReverseCumulativeMidTerm?: string;
  ReverseCumulativeQ3?: string;
  ReverseCumulativeQ4?: string;
  ReverseCumulativeAnnual?: string;
  ReverseNonCumulativeQ1?: string;
  ReverseNonCumulativeQ2?: string;
  ReverseNonCumulativeMidTerm?: string;
  ReverseNonCumulativeQ3?: string;
  ReverseNonCumulativeQ4?: string;
  ReverseNonCumulativeAnnual?: string;
  BinaryDeterminationQ1?: string;
  BinaryDeterminationQ2?: string;
  BinaryDeterminationMidTerm?: string;
  BinaryDeterminationQ3?: string;
  BinaryDeterminationQ4?: string;
  BinaryDeterminationAnnual?: string;
}

export interface OpmsVoteNumberChild {
  Code: string;
  Description: string;
  Target?: string;
}

export interface AdditionalAssigneeChild {
  TargetType?: string;
  Role: string;
  Employee: string;
  Target?: string;
}

export interface UserSubmitChild {
  Oid: string;
  Employee?: string;
  Status?: SubmissionStatus;
  SubmittedOn?: string;
}

// User types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: UserRole;
  department?: Department;
  position?: Position;
  avatarUrl?: string;
  isActive: boolean;
  lastLogin?: string;
}

// Organization Structure
export interface Department {
  id: string;
  publicId?: string;
  name: string;
  code: string;
  description?: string;
  manager?: Employee;
  parentDepartment?: Department;
  isActive: boolean;
  units: DepartmentUnit[];
  positions: Position[];
}

export interface DepartmentUnit {
  id: string;
  publicId?: string;
  name: string;
  code: string;
  department: Department;
  head?: Employee;
  isActive: boolean;
}

export interface Position {
  id: string;
  title: string;
  code: string;
  department: Department;
  unit?: DepartmentUnit;
  level: number;
  isActive: boolean;
}

// Employee
export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  dateOfBirth?: string;
  identificationType: string;
  nationalId?: string;
  title?: string;
  notes?: string;
  manager?: Employee;
  position?: Position;
  department?: Department;
  departmentUnit?: DepartmentUnit;
  taxId?: string;
  address1?: string;
  address2?: string;
  phone?: string;
  mobile?: string;
  startDate?: string;
  isActive: boolean;
}

// OPMS Target
export interface OPMSTarget extends BaseKpiTargetXafFields, TypedTargetPeriodValues, TargetAuditMetaFields {
  id: string;
  publicId?: string;
  rowVersion?: string;
  municipalityFinancialYearPublicId?: string;
  nationalKpaPublicId?: string;
  municipalKpaPublicId?: string;
  backToBasicsPillarPublicId?: string;
  backToBasicsPillar?: string;
  strategicGoalPublicId?: string;
  strategicInterventionPublicId?: string;
  strategicIntervention?: string;
  strategicObjectivePublicId?: string;
  performanceObjectivePublicId?: string;
  sdbipLayer?: { publicId: string; code: string; name: string };
  PriorYearOpmsId?: string;
  sourceTemplateId?: string;
  sourceTemplateVersion?: number;
  period: Period;
  department: Department;
  unit?: DepartmentUnit;
  wards?: Ward[];
  wardIds?: number[];
  assignedTo?: Employee;
  indicatorNumber: string;
  isIndicatorNumberRevised: boolean;
  revisedIndicatorNumber?: string;
  originalOrderNumber: number;
  revisedOrderNumber: number;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal: StrategicGoal;
  strategicObjective: StrategicObjective;
  performanceObjective: string;
  targetName: string;
  isTargetNameRevised: boolean;
  revisedTargetName?: string;
  kpiDescription: string;
  isKpiDescriptionRevised: boolean;
  revisedKpiDescription?: string;
  baseline: number;
  baselineDescription?: string;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSource: BudgetSource;
  budgetType: BudgetType;
  budgetTypePublicId?: string;
  budgetTypeName?: string;
  budgetSources?: KpiBudgetSource[];
  unitOfMeasure: UnitOfMeasure;
  kpiUnitOfMeasurePublicId?: string;
  weight: number;
  kpiType: string;
  kpiTypePublicId?: string;
  indicatorType: string;
  indicatorTypePublicId?: string;
  functionalArea?: string;
  functionalAreaPublicId?: string;
  standardClassification?: string;
  standardClassificationPublicId?: string;
  idpReference?: string;
  internalReference?: string;
  fmsLink?: string;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string;

  // Quarterly Targets
  q1Target?: number;
  q1Description?: string;
  q1Budget?: number;
  q2Target?: number;
  q2Description?: string;
  q2Budget?: number;
  midTermTarget?: number;
  midTermDescription?: string;
  midTermBudget?: number;
  q3Target?: number;
  q3Description?: string;
  q3Budget?: number;
  q3RevisedTarget?: number;
  q4Target?: number;
  q4Description?: string;
  q4Budget?: number;
  q4RevisedTarget?: number;
  revisedAnnualTarget?: number;
  revisedAnnualBudget?: number;

  targetUnitType: TargetUnitType;
  periodTargets: PerformancePeriodTargetDto[];
  UserSubmit?: UserSubmitChild[];
  submissions: OPMSSubmission[];
  voteNumbers: VoteNumber[];
  voteNumberIds?: number[];
  VoteNumbers?: OpmsVoteNumberChild[];
  relatedIPMSTargets: IPMSTarget[];
  RelatedIPMSTargets?: string[];
  additionalAssignees: Employee[];
  additionalAssigneeIds?: string[];
  AdditionalAssignees?: AdditionalAssigneeChild[];
  attachments: Attachment[];
}

// IPMS Target
export interface IPMSTarget extends BaseKpiTargetXafFields, TypedTargetPeriodValues, TargetAuditMetaFields {
  id: string;
  publicId?: string;
  rowVersion?: string;
  municipalityFinancialYearPublicId?: string;
  nationalKpaPublicId?: string;
  municipalKpaPublicId?: string;
  backToBasicsPillarPublicId?: string;
  backToBasicsPillar?: string;
  strategicGoalPublicId?: string;
  strategicInterventionPublicId?: string;
  strategicIntervention?: string;
  strategicObjectivePublicId?: string;
  performanceObjectivePublicId?: string;
  sourceTemplateId?: string;
  sourceTemplateVersion?: number;
  relatedOPMSTarget?: OPMSTarget;
  period: Period;
  department: Department;
  unit?: DepartmentUnit;
  assignedTo?: Employee;
  indicatorNumber: string;
  isIndicatorNumberRevised: boolean;
  revisedIndicatorNumber?: string;
  originalOrderNumber: number;
  revisedOrderNumber: number;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal: StrategicGoal;
  strategicObjective: StrategicObjective;
  performanceObjective: string;
  targetName: string;
  isTargetNameRevised: boolean;
  revisedTargetName?: string;
  kpiDescription: string;
  isKpiDescriptionRevised: boolean;
  revisedKpiDescription?: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSource: BudgetSource;
  budgetType: BudgetType;
  budgetTypePublicId?: string;
  budgetTypeName?: string;
  budgetSources?: KpiBudgetSource[];
  unitOfMeasure: UnitOfMeasure;
  kpiUnitOfMeasurePublicId?: string;
  weight: number;
  kpiType: string;
  kpiTypePublicId?: string;
  indicatorType: string;
  indicatorTypePublicId?: string;
  functionalArea?: string;
  functionalAreaPublicId?: string;
  idpReference?: string;
  internalReference?: string;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string;
  withdrawnAt?: string;
  withdrawnByUserId?: string;

  // Quarterly Targets
  q1Target?: number;
  q1Description?: string;
  q1Budget?: number;
  q2Target?: number;
  q2Description?: string;
  q2Budget?: number;
  midTermTarget?: number;
  midTermDescription?: string;
  midTermBudget?: number;
  q3Target?: number;
  q3Description?: string;
  q3Budget?: number;
  q3RevisedTarget?: number;
  q4Target?: number;
  q4Description?: string;
  q4Budget?: number;
  q4RevisedTarget?: number;
  revisedAnnualTarget?: number;
  revisedAnnualBudget?: number;

  targetUnitType: TargetUnitType;
  periodTargets: PerformancePeriodTargetDto[];
  UserSubmit?: UserSubmitChild[];
  submissions: IPMSSubmission[];
  attachments: Attachment[];
}

// Submissions
export interface OPMSSubmission {
  id: string;
  rowVersion?: string;
  baseState: 'IN_PROGRESS' | 'SUBMITTED';
  target: OPMSTarget;
  quarter: Quarter;
  dueDate: string;
  extendedDueDate?: string;
  actual: number;
  actualPerformance?: string;
  systemSuggestedActualPerformance?: string;
  wasSystemSuggestionEdited?: boolean;
  suggestionGeneratedDate?: string;
  suggestionEditedByUserId?: string;
  suggestionEditedAt?: string;
  suggestionEditReason?: string;
  achievementPercent?: number;
  targetAchieved?: boolean;
  reportingPeriodPublicId?: string;
  actualDescription?: string;
  actualExpenditure?: number;
  variance?: number;
  varianceReason?: string;
  correctiveMeasure?: string;
  actualPerformanceDescription?: string;
  submitterScore?: number;
  submitterStatus?: string;
  verifierStatus?: string;
  approverStatus?: string;
  pmsStatus?: string;
  auditorStatus?: string;
  status: SubmissionStatus;
  submitter?: Employee;
  submittedAt?: string;
  submittedByUserId?: string;
  verifier?: Employee;
  verifiedAt?: string;
  verifierComments?: string;
  verifierComment?: string;
  verifierScore?: number;
  approver?: Employee;
  approvedAt?: string;
  approverComments?: string;
  approverComment?: string;
  approverScore?: number;
  pmsOfficer?: Employee;
  pmsReviewedAt?: string;
  pmsComments?: string;
  pmsComment?: string;
  pmsRecommendation?: string;
  pmsScore?: number;
  pmsResponseDueDate?: string;
  pmsRfiComment?: string;
  auditor?: Employee;
  auditedAt?: string;
  auditorComments?: string;
  auditorComment?: string;
  auditorRecommendation?: string;
  auditorScore?: number;
  auditorResponseDueDate?: string;
  dueDateExtendedDays?: number;
  poeType?: string;
  isDisabled?: boolean;
  withdrawalReason?: string;
  withdrawnAt?: string;
  withdrawnByUserId?: string;
  createdBy?: string;
  createdOn?: string;
  updatedBy?: string;
  updatedOn?: string;
  organisationId?: string;
  attachments: Attachment[];
  comments: SubmissionComment[];
  history: SubmissionHistory[];
}

export interface IPMSSubmission {
  id: string;
  rowVersion?: string;
  baseState: 'IN_PROGRESS' | 'SUBMITTED';
  target: IPMSTarget;
  quarter: Quarter;
  dueDate: string;
  extendedDueDate?: string;
  actual: number;
  actualPerformance?: string;
  systemSuggestedActualPerformance?: string;
  wasSystemSuggestionEdited?: boolean;
  suggestionGeneratedDate?: string;
  suggestionEditedByUserId?: string;
  suggestionEditedAt?: string;
  suggestionEditReason?: string;
  achievementPercent?: number;
  targetAchieved?: boolean;
  reportingPeriodPublicId?: string;
  actualDescription?: string;
  actualPerformanceDescription?: string;
  actualExpenditure?: number;
  variance?: number;
  varianceReason?: string;
  correctiveMeasure?: string;
  submitterScore?: number;
  submitterStatus?: string;
  verifierStatus?: string;
  approverStatus?: string;
  pmsStatus?: string;
  auditorStatus?: string;
  status: SubmissionStatus;
  submitter?: Employee;
  submittedAt?: string;
  submittedByUserId?: string;
  verifier?: Employee;
  verifiedAt?: string;
  verifierComments?: string;
  verifierComment?: string;
  verifierScore?: number;
  approver?: Employee;
  approvedAt?: string;
  approverComments?: string;
  approverComment?: string;
  approverScore?: number;
  pmsOfficer?: Employee;
  pmsReviewedAt?: string;
  pmsComments?: string;
  pmsComment?: string;
  pmsRecommendation?: string;
  pmsScore?: number;
  pmsResponseDueDate?: string;
  pmsRfiComment?: string;
  auditor?: Employee;
  auditedAt?: string;
  auditorComments?: string;
  auditorComment?: string;
  auditorRecommendation?: string;
  auditorScore?: number;
  auditorResponseDueDate?: string;
  dueDateExtendedDays?: number;
  poeType?: string;
  isDisabled?: boolean;
  withdrawalReason?: string;
  withdrawnAt?: string;
  withdrawnByUserId?: string;
  createdBy?: string;
  createdOn?: string;
  updatedBy?: string;
  updatedOn?: string;
  organisationId?: string;
  attachments: Attachment[];
  comments: SubmissionComment[];
  history: SubmissionHistory[];
}

// Supporting Types
export interface Period {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  fiscalYear: string;
  isActive: boolean;
}

export interface StrategicGoal {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface StrategicObjective {
  id: string;
  name: string;
  code: string;
  strategicGoal: StrategicGoal;
  description?: string;
  isActive: boolean;
}

export interface BudgetSource {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface BudgetType {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface KpiBudgetSource {
  publicId: string;
  budgetSourcePublicId: string;
  code: string;
  name: string;
  amount?: number | null;
}

export interface UnitOfMeasure {
  id: string;
  name: string;
  code: string;
  symbol?: string;
  description?: string;
  isActive: boolean;
}

export interface Ward {
  id: string;
  name: string;
  code: string;
  municipality?: string;
  isActive: boolean;
}

export interface VoteNumber {
  id: string;
  number: string;
  name: string;
  department: Department;
  amount?: number;
  description?: string;
  isActive: boolean;
}

export interface Attachment {
  id: string;
  publicId?: string;
  evidenceBlobPublicId?: string;
  fileName: string;
  fileSize: number;
  fileType: string;
  uploadedBy: Employee;
  uploadedAt: string;
  documentType: string;
  url: string;
  scanStatus?: string;
  isQuarantined?: boolean;
  scanDetail?: string;
  assessments?: PoeEvidenceAssessmentDto[];
  rowVersion?: string;
  replacementOf?: PoeEvidenceReplacementDto | null;
  replacedBy?: PoeEvidenceReplacementDto | null;
  legalHolds?: PoeLegalHoldDto[];
  isActive?: boolean;
  retainUntil?: string | null;
  disposals?: PoeDisposalDto[];
  isContentDeleted?: boolean;
}

export interface PoeEvidenceAssessmentDto {
  publicId: string;
  outcome: 'Accepted' | 'Rejected' | 'NeedsClarification';
  comment?: string | null;
  assessedByUserId: string;
  assessedByName?: string | null;
  assessedAt: string;
  correlationId: string;
}

export interface PoeEvidenceReplacementDto {
  publicId: string;
  supersededEvidencePublicId: string;
  supersededFileName: string;
  replacementEvidencePublicId: string;
  replacementFileName: string;
  reason: string;
  replacedByUserId: string;
  replacedByName?: string | null;
  replacedAt: string;
  correlationId: string;
}

export interface PoeLegalHoldDto {
  holdId: string;
  holdReference: string;
  isActive: boolean;
  placedReason: string;
  placedByUserId: string;
  placedByName?: string | null;
  placedAt: string;
  releasedReason?: string | null;
  releasedByUserId?: string | null;
  releasedByName?: string | null;
  releasedAt?: string | null;
}

export interface PoeDisposalDto {
  disposalId: string;
  status: 'Pending' | 'Completed' | 'Failed';
  approvalReference: string;
  reason: string;
  requestedByUserId: string;
  requestedByName?: string | null;
  requestedAt: string;
  completedAt?: string | null;
  failedAt?: string | null;
  detail?: string | null;
}

export interface SubmissionComment {
  id: string;
  content: string;
  author: Employee;
  createdAt: string;
}

export interface SubmissionHistory {
  id: string;
  action: string;
  performedBy: Employee;
  performedAt: string;
  previousStatus?: SubmissionStatus;
  newStatus: SubmissionStatus;
  comments?: string;
}

export interface TemplateQuarterlyTarget {
  quarter: Quarter;
  target?: number;
  description?: string;
  budget?: number;
}

export interface OpmsTargetTemplate {
  id: string;
  templateCode: string;
  templateName: string;
  indicatorNumber: string;
  department?: Department;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription: string;
  targetUnitType: TargetUnitType;
  unitOfMeasure: UnitOfMeasure;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal?: StrategicGoal;
  strategicObjective?: StrategicObjective;
  performanceObjective: string;
  outcome?: string;
  output?: string;
  priorityIssue?: string;
  budgetSource?: BudgetSource;
  budgetType?: BudgetType;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string;
  standardClassification?: string;
  idpReference?: string;
  internalReference?: string;
  fmsLink?: string;
  defaultQuarterlyTargets: TemplateQuarterlyTarget[];
  defaultBudgetInformation?: string;
  defaultPoeRequirements?: string;
  isActive: boolean;
  isArchived?: boolean;
  version: number;
  createdBy: string;
  createdDate: string;
}

export interface IpmsTargetTemplate {
  id: string;
  templateCode: string;
  templateName: string;
  department?: Department;
  targetName: string;
  kpiDescription: string;
  performanceArea: string;
  employeeLevel: string;
  jobGrade: string;
  targetUnitType: TargetUnitType;
  unitOfMeasure: UnitOfMeasure;
  annualTarget: number;
  annualTargetDescription: string;
  weight: number;
  defaultRatingMethod?: string;
  defaultScoreScale?: string;
  defaultPoeRequirements?: string;
  defaultTaskTemplates: string[];
  linkedOpmsTargetRequired: boolean;
  functionalArea?: string;
  isActive: boolean;
  isArchived?: boolean;
  version: number;
  createdBy: string;
  createdDate: string;
}

export interface OpmsTargetTemplateVersion {
  id: string;
  templateId: string;
  version: number;
  snapshot: OpmsTargetTemplate;
  createdBy: string;
  createdDate: string;
}

export interface IpmsTargetTemplateVersion {
  id: string;
  templateId: string;
  version: number;
  snapshot: IpmsTargetTemplate;
  createdBy: string;
  createdDate: string;
}

// KPI Library
export interface KPITemplate {
  id: string;
  name: string;
  department?: Department;
  unit?: DepartmentUnit;
  wards?: Ward[];
  indicatorNumber: string;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal?: StrategicGoal;
  strategicObjective?: StrategicObjective;
  performanceObjective: string;
  projectName?: string;
  kpiDescription: string;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string;
  demand?: number;
  backlog?: number;
  budgetSource?: BudgetSource;
  budgetType?: BudgetType;
  unitOfMeasure?: UnitOfMeasure;
  weight?: number;
  idpReference?: string;
  internalReference?: string;
  fmsLink?: string;
  documentType?: string;
  isActive: boolean;
}

// Task Management
export interface Task {
  id: string;
  title: string;
  description?: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  status: 'pending' | 'in_progress' | 'completed' | 'postponed' | 'cancelled';
  assignedTo: Employee[];
  estimatedHours?: number;
  actualHours?: number;
  dueDate: string;
  completedAt?: string;
  notes?: string;
  createdBy: Employee;
  createdAt: string;
  updatedAt: string;
}

// Approval Setup
export interface ApprovalSetup {
  id: string;
  user: Employee;
  userEmail: string;
  approver?: Employee;
  approverEmail?: string;
  isAdminApprover: boolean;
  department?: Department;
  isActive: boolean;
}

// Lookup Tables
export interface Organisation {
  id: string;
  name: string;
  code: string;
  type: string;
  isActive: boolean;
}

export interface Industry {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface KPA {
  id: string;
  name: string;
  code: string;
  type: 'national' | 'municipal';
  description?: string;
  isActive: boolean;
}

export interface DepartmentalObjective {
  id: string;
  name: string;
  code: string;
  department: Department;
  description?: string;
  isActive: boolean;
}

export interface PerformanceObjective {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface PriorityIssue {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface Output {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

// Location Types
export interface Country {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
}

export interface Province {
  id: string;
  name: string;
  code: string;
  country: Country;
  isActive: boolean;
}

export interface City {
  id: string;
  name: string;
  code: string;
  province: Province;
  isActive: boolean;
}

export interface Suburb {
  id: string;
  name: string;
  code: string;
  city: City;
  isActive: boolean;
}

export interface Address {
  id: string;
  line1: string;
  line2?: string;
  suburb: Suburb;
  city: City;
  province: Province;
  country: Country;
  postalCode: string;
  isActive: boolean;
}

export interface Contact {
  id: string;
  name: string;
  email: string;
  phone?: string;
  mobile?: string;
  address?: Address;
  organization?: Organisation;
  position?: string;
  notes?: string;
  isActive: boolean;
}

// Dashboard
export interface DashboardStats {
  totalTargets: number;
  completedTargets: number;
  pendingSubmissions: number;
  overdueSubmissions: number;
  pendingApprovals: number;
  pendingVerifications: number;
  pmsQueue: number;
  auditorQueue: number;
  averageScore: number;
  topPerformingDepartments: DepartmentPerformance[];
  recentActivity: ActivityItem[];
}

export interface DepartmentPerformance {
  department: Department;
  score: number;
  targetCount: number;
  completedCount: number;
  pendingCount: number;
  overdueCount: number;
}

export interface ActivityItem {
  id: string;
  type: 'submission' | 'approval' | 'verification' | 'comment' | 'target_created';
  description: string;
  user: User;
  timestamp: string;
  targetType?: string;
}

// Auth Types
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  code?: string;
  correlationId?: string;
}

export interface PerformanceDashboardDto {
  totalTargets: number;
  activeTargets: number;
  completedTargets: number;
  overdueTargets: number;
  atRiskTargets: number;
  outstandingTargets: number;
  draftSubmissions: number;
  submittedSubmissions: number;
  returnedSubmissions: number;
  approvedSubmissions: number;
  pendingVerification: number;
  pendingApproval: number;
}

export type WorkflowQueueName =
  | 'all'
  | 'my-submissions'
  | 'verification'
  | 'approval'
  | 'pms'
  | 'auditor'
  | 'returned'
  | 'my-drafts'
  | 'pending-submission'
  | 'my-returned'
  | 'under-verification'
  | 'under-review'
  | 'under-approval'
  | 'internal-audit-returned'
  | 'approved-closed';

export interface WorkflowQueueCountsDto {
  mySubmissions: number;
  verification: number;
  approval: number;
  pms: number;
  auditor: number;
  returned: number;
  myDrafts: number;
  pendingSubmission: number;
  myReturned: number;
  underVerification: number;
  underReview: number;
  underApproval: number;
  internalAuditReturned: number;
  approvedClosed: number;
}

export interface WorkflowQueueItemDto {
  id: string;
  publicId: string;
  kind: 'opms' | 'ipms';
  targetId: string;
  targetPublicId: string;
  targetName: string;
  indicatorNumber: string;
  quarter: Quarter;
  dueDate?: string;
  status: SubmissionStatus;
  submittedByUserId?: string;
  submittedByName?: string;
  verifierName?: string;
  approverName?: string;
  createdAt: string;
}

export interface WorkflowQueueDto {
  queue: WorkflowQueueName;
  counts: WorkflowQueueCountsDto;
  page: PagedResult<WorkflowQueueItemDto>;
}

export interface UserProfile {
  id: string;
  userName: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  position?: string;
  isActive: boolean;
  mustChangePassword: boolean;
}

export interface MenuItem {
  label: string;
  path?: string;
  icon?: string;
  children?: MenuItem[];
  isDivider: boolean;
}

export interface LoginResponse {
  expiresAt: string;
  user: UserProfile;
  roles: string[];
  permissions: string[];
  menu: MenuItem[];
  mfaEnrollmentRequired: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TargetLibraryFacets {
  primaryAreas: string[];
  functionalAreas: string[];
  classifications: string[];
  targetUnitTypes: string[];
  versions: number[];
}

export interface PerformanceTargetOptionDto {
  id: string;
  publicId: string;
  indicatorNumber: string;
  targetName: string;
  departmentId?: number | null;
  departmentName?: string | null;
  relatedOpmsTargetPublicId?: string | null;
}

export interface TargetNormalizationPreviewDto {
  targetPublicId: string;
  indicatorNumber: string;
  targetName: string;
  status: 'Blocked' | 'Ready' | 'Normalized';
  missingPeriodTargets: number;
  error?: string | null;
}

export interface TargetNormalizationResultDto {
  selectedTargets: number;
  addedPeriodTargets: number;
  alreadyNormalizedTargets: number;
}

export interface LoginRequest {
  email?: string | null;
  password: string;
  twoFactorCode?: string;
  recoveryCode?: string;
}

export interface MfaStatusDto {
  isEnabled: boolean;
  enrollmentRequired: boolean;
  recoveryCodesLeft: number;
}

export interface MfaSetupDto {
  sharedKey: string;
  authenticatorUri: string;
}

export interface MfaEnableDto {
  recoveryCodes: string[];
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string;
}

export interface AdminRole {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  isActive: boolean;
}

export interface AdminUser {
  id: string;
  publicId: string;
  userName: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  position?: string;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt?: string | null;
}

export interface AdminUserDetail {
  user: AdminUser;
  roles: AdminRole[];
}

export interface DemoUser {
  role: string;
  fullName: string;
  department: string;
  position: string;
  email: string;
  userName: string;
  password: string;
}

export interface AdminPermission {
  id: number;
  module: string;
  feature: string;
  action: string;
  code: string;
  description?: string;
  isActive: boolean;
}

export interface AdminPermissionGroup {
  module: string;
  feature: string;
  permissions: AdminPermission[];
}

export interface RolePermission {
  permissionId: number;
  code: string;
  isAllowed: boolean;
}

export interface TenantContextDto {
  id: number;
  publicId: string;
  code: string;
  name: string;
  isCurrent: boolean;
}

export interface ReportingPeriodMasterDto {
  publicId: string;
  municipalityFinancialYearPublicId: string;
  code: string;
  name: string;
  periodType: number;
  sequence: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  rowVersion: string;
}

export interface FinancialYearMasterDto {
  publicId: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  rowVersion: string;
}

export interface MunicipalityFinancialYearMasterDto {
  publicId: string;
  financialYearPublicId: string;
  code: string;
  name: string;
  isCurrent: boolean;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface SdbipLayerMasterDto {
  publicId: string;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  code: string;
  name: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
  rowVersion: string;
}

export interface OpmsImportRowDto { publicId: string; sourceRowNumber: number; reference: string; status: string; existingValueJson?: string | null; normalizedJson?: string | null; errorCode?: string | null; errorPeriod?: string | null; errorField?: string | null; suppliedValue?: string | null; errorMessage?: string | null }
export interface OpmsImportBatchDto { publicId: string; clientRequestId: string; sdbipLayerPublicId: string; sourceFileName: string; sourceSha256: string; status: string; totalRows: number; newRows: number; unchangedRows: number; changedRows: number; invalidRows: number; createdAt: string; committedAt?: string | null; rowVersion: string; rows: OpmsImportRowDto[] }

export interface MunicipalEmployeeDto {
  publicId: string;
  employeeNumber?: string | null;
  salaryReference?: string | null;
  firstName: string;
  lastName: string;
  emailAddress?: string | null;
  identityUserId?: string | null;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface EmployeeAssignmentMasterDto {
  publicId: string;
  employeePublicId: string;
  departmentPublicId: string;
  departmentName: string;
  unitPublicId?: string | null;
  unitName?: string | null;
  positionCode: string;
  positionName: string;
  positionPublicId?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  rowVersion: string;
}

  export interface AuthSessionDto {
  sessionId: string;
  createdAt: string;
  lastUsedAt: string;
  absoluteExpiresAt: string;
  createdByIp?: string | null;
  lastUsedByIp?: string | null;
    userAgent?: string | null;
    authenticationMethod: string;
  isCurrent: boolean;
}

export interface DepartmentMasterDto {
  publicId: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface UnitMasterDto {
  publicId: string;
  departmentPublicId: string;
  departmentName: string;
  code: string;
  name: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface PositionMasterDto {
  publicId: string;
  departmentPublicId: string;
  departmentName: string;
  unitPublicId?: string | null;
  unitName?: string | null;
  code: string;
  name: string;
  grade?: string | null;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface WardMasterDto {
  publicId: string;
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface VoteNumberMasterDto {
  publicId: string;
  id: number;
  departmentPublicId: string;
  departmentName: string;
  municipalityFinancialYearPublicId?: string | null;
  financialYearCode?: string | null;
  code: string;
  number: string;
  name: string;
  amount: number;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
}

export interface GlobalStrategicReferenceDto {
  publicId: string;
  code: string;
  name: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
  isEnabledForMunicipality: boolean;
  availabilityPublicId?: string | null;
  availabilityRowVersion?: string | null;
  rowVersion: string;
}

export interface StrategicPlanningMasterDto {
  publicId: string;
  code?: string | null;
  name: string;
  description?: string | null;
  effectiveFromFinancialYearPublicId?: string | null;
  effectiveFromFinancialYearCode?: string | null;
  effectiveToFinancialYearPublicId?: string | null;
  effectiveToFinancialYearCode?: string | null;
  displayOrder: number;
  isActive: boolean;
  rowVersion: string;
  symbol?: string | null;
}

export interface StrategicPlanningRelationshipDto {
  publicId: string;
  relationshipType: string;
  parentPublicId: string;
  parentName: string;
  childPublicId: string;
  childName: string;
  isActive: boolean;
  rowVersion: string;
}

export interface StrategicCatalogueItemDto {
  publicId: string;
  code?: string | null;
  name: string;
  displayOrder: number;
  symbol?: string | null;
}

export interface StrategicCatalogueRelationshipDto {
  relationshipType: string;
  parentPublicId: string;
  childPublicId: string;
}

export interface StrategicClassificationCatalogueDto {
  nationalKpas: StrategicCatalogueItemDto[];
  municipalKpas: StrategicCatalogueItemDto[];
  backToBasicsPillars: StrategicCatalogueItemDto[];
  strategicGoals: StrategicCatalogueItemDto[];
  strategicInterventions: StrategicCatalogueItemDto[];
  strategicObjectives: StrategicCatalogueItemDto[];
  performanceObjectives: StrategicCatalogueItemDto[];
  budgetSources?: StrategicCatalogueItemDto[];
  budgetTypes?: StrategicCatalogueItemDto[];
  kpiTypes?: StrategicCatalogueItemDto[];
  indicatorTypes?: StrategicCatalogueItemDto[];
  functionalAreas?: StrategicCatalogueItemDto[];
  standardClassifications?: StrategicCatalogueItemDto[];
  kpiUnitsOfMeasure?: StrategicCatalogueItemDto[];
  relationships: StrategicCatalogueRelationshipDto[];
}

export interface PerformancePeriodTargetDto {
  publicId: string;
  reportingPeriodPublicId: string;
  periodCode: string;
  periodType: number;
  unitKind: number;
  direction: number;
  opmsUnitPublicId?: string | null;
  opmsUnitCode?: string | null;
  performanceDirectionPublicId?: string | null;
  performanceDirectionCode?: string | null;
  targetValue: string;
  budgetValue?: number | null;
  description?: string | null;
  isActive: boolean;
  rowVersion: string;
  originalUnitKind: number;
  originalOpmsUnitPublicId?: string | null;
  originalOpmsUnitCode?: string | null;
  originalTargetValue: string;
  originalBudgetValue?: number | null;
  isTargetRevised: boolean;
  revisedUnitKind?: number | null;
  revisedTargetValue?: string | null;
  isBudgetRevised: boolean;
  revisedBudgetValue?: number | null;
}

export interface OpmsUnitDefinitionDto {
  publicId: string;
  code: string;
  name: string;
  inputControlType: string;
  valueDataType: string;
  symbol?: string | null;
  decimalPlaces?: number | null;
  minValue?: number | null;
  maxValue?: number | null;
  supportsAutoVariance: boolean;
  defaultPerformanceDirectionPublicId?: string | null;
  requiresComponentUi: boolean;
  isQualitative: boolean;
  engineUnitKind: number;
  isActive: boolean;
}

export interface PerformanceDirectionDefinitionDto {
  publicId: string;
  code: string;
  name: string;
  description: string;
  engineDirection: 1 | 2 | 3;
  isActive: boolean;
}

export interface PerformanceConfigurationCatalogueDto {
  opmsUnits: OpmsUnitDefinitionDto[];
  performanceDirections: PerformanceDirectionDefinitionDto[];
}

export interface PerformanceTargetRevisionDto {
  publicId: string;
  fieldName: string;
  originalValue?: string | null;
  revisedValue?: string | null;
  reason: string;
  approvalReference: string;
  effectiveAt: string;
  revisedByUserId: string;
  recordedAt: string;
}

export interface KpiFieldRevisionDto {
  publicId: string;
  fieldName: string;
  originalValue?: string | null;
  revisedValue?: string | null;
  reason: string;
  approvalReference: string;
  effectiveAt: string;
  revisedByUserId: string;
  recordedAt: string;
}

export interface WorkflowStageDefinitionDto {
  publicId: string;
  code: string;
  name: string;
  sequence: number;
  requiredActionCode: string;
  requiredPermissionCode: string;
  isOptional: boolean;
  allowBypass: boolean;
  requireDifferentActorFromSubmitter: boolean;
  requireDifferentActorFromPreviousStage: boolean;
  isTerminal: boolean;
  rejectionStageCode?: string | null;
  requiresRating: boolean;
  ratingSchemePublicId?: string | null;
  ratingSchemeCode?: string | null;
}

export interface StageRatingDto {
  publicId: string;
  workflowActionPublicId: string;
  stageCode: string;
  ratingSchemePublicId: string;
  ratingSchemeCode: string;
  ratingValuePublicId: string;
  value: number;
  label: string;
  achievementPercent?: number | null;
  comment?: string | null;
  ratedByUserId: string;
  ratedByName?: string | null;
  ratedAt: string;
}

export interface NotificationDeliveryAttemptDto {
  publicId: string;
  recipientUserId: string;
  channel: string;
  status: string;
  attemptCount: number;
  attemptedAt: string;
  deliveredAt?: string | null;
  provider?: string | null;
  providerReference?: string | null;
  error?: string | null;
  responseDetail?: string | null;
}

export interface NotificationOutboxItemDto {
  publicId: string;
  eventType: string;
  aggregateType: string;
  aggregateId: string;
  occurredAt: string;
  availableAt: string;
  attemptCount: number;
  lastError?: string | null;
  isDeadLetter: boolean;
  rowVersion: string;
  deliveries: NotificationDeliveryAttemptDto[];
}

export interface NotificationScheduleRuleDto {
  publicId: string;
  code: string;
  workingDayOffset: number;
  recipientKind: 1 | 2 | 3;
  recipientValues: string[];
  isActive: boolean;
}

export interface NotificationPolicyDto {
  publicId: string;
  familyId: string;
  version: number;
  code: string;
  name: string;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  scope: 1 | 2 | 3;
  source: 1 | 2;
  submissionKind?: 1 | 2 | null;
  workflowStageCode?: string | null;
  reportingPeriodPublicId?: string | null;
  reportingPeriodName?: string | null;
  lifecycle: 1 | 2 | 3 | 4;
  isMandatory: boolean;
  deliveryPaused: boolean;
  channels: string[];
  titleTemplate: string;
  messageTemplate: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rules: NotificationScheduleRuleDto[];
  rowVersion: string;
}

export interface WorkingCalendarHolidayDto {
  publicId: string;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  date: string;
  name: string;
  rowVersion: string;
}

export interface NotificationTemplatePreviewDto { title: string; message: string; channels: string[] }
export interface NotificationPreferenceDto { emailEnabled: boolean; smsEnabled: boolean; dailyDigestEnabled: boolean; weeklySummaryEnabled: boolean; rowVersion?: string | null }

export interface WorkflowDefinitionDto {
  publicId: string;
  municipalityFinancialYearPublicId: string;
  submissionKind: number;
  code: string;
  name: string;
  version: number;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  rowVersion: string;
  stages: WorkflowStageDefinitionDto[];
}

export interface WorkflowStageDifferenceDto {
  change: 'Added' | 'Removed' | 'Modified' | 'Unchanged';
  stageCode: string;
  fromSequence?: number | null;
  toSequence?: number | null;
  changedFields: string[];
}

export interface WorkflowDefinitionComparisonDto {
  from: WorkflowDefinitionDto;
  to: WorkflowDefinitionDto;
  stageDifferences: WorkflowStageDifferenceDto[];
}

export interface ReportingWindowDto {
  publicId: string;
  reportingPeriodPublicId: string;
  periodCode: string;
  submissionKind: number;
  opensAt: string;
  closesAt: string;
  isActive: boolean;
  rowVersion: string;
}

export interface ReportingWindowExceptionDto {
  publicId: string;
  userId?: string | null;
  departmentId?: number | null;
  unitId?: number | null;
  extendedClosesAt: string;
  reason: string;
  approvedByUserId: string;
  approvedAt: string;
  rowVersion: string;
}

export interface PerformanceRfiDto {
  publicId: string;
  question: string;
  raisedByUserId: string;
  raisedAt: string;
  responseDueAt: string;
  response?: string | null;
  respondedByUserId?: string | null;
  respondedAt?: string | null;
  closedByUserId?: string | null;
  closedAt?: string | null;
  rowVersion: string;
  evidence: RfiEvidenceDto[];
}

export type InternalAuditAssessmentModel = 1 | 2;
export type InternalAuditAssessmentOutcome = 1 | 2 | 3 | 4;

export interface InternalAuditConfigurationDto {
  publicId: string;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  model: InternalAuditAssessmentModel;
  version: number;
  isCurrent: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  reason: string;
  rowVersion: string;
}

export interface InternalAuditAssessmentDto {
  publicId: string;
  model: InternalAuditAssessmentModel;
  outcome: InternalAuditAssessmentOutcome;
  detailedObservation: string;
  comment?: string | null;
  findings?: string | null;
  recommendation?: string | null;
  score?: number | null;
  assessedByUserId: string;
  assessedByName?: string | null;
  assessedAt: string;
  previousAssessmentPublicId?: string | null;
  rfiPublicId?: string | null;
  rfiResponseDueAt?: string | null;
}

export interface InternalAuditSubmissionDto {
  configuration: InternalAuditConfigurationDto;
  latestAssessment?: InternalAuditAssessmentDto | null;
}

export interface RfiEvidenceDto {
  publicId: string;
  evidencePublicId: string;
  purpose: 1 | 2 | 3;
  fileName: string;
  contentType?: string | null;
  sizeInBytes: number;
  sha256: string;
  linkedByUserId: string;
  linkedAt: string;
  url: string;
}

export interface PerformanceLookupItemDto {
  id: number;
  code: string;
  name: string;
}

export interface PerformancePeriodLookupDto extends PerformanceLookupItemDto {
  startDate: string;
  endDate: string;
  fiscalYear: string;
}

export interface StrategicObjectiveLookupDto extends PerformanceLookupItemDto {
  strategicGoalId: number;
}

export interface UnitOfMeasureLookupDto extends PerformanceLookupItemDto {
  symbol?: string | null;
}

export interface PerformanceLookupsDto {
  periods: PerformancePeriodLookupDto[];
  strategicGoals: PerformanceLookupItemDto[];
  strategicObjectives: StrategicObjectiveLookupDto[];
  budgetSources: PerformanceLookupItemDto[];
  budgetTypes: PerformanceLookupItemDto[];
  unitsOfMeasure: UnitOfMeasureLookupDto[];
}

export interface RatingValueDto {
  publicId: string;
  value: number;
  label: string;
  minimumAchievementPercent?: number | null;
  maximumAchievementPercent?: number | null;
  sortOrder: number;
}

export interface RatingSchemeDto {
  publicId: string;
  code: string;
  name: string;
  isActive: boolean;
  rowVersion: string;
  values: RatingValueDto[];
}

export interface DepartmentPerformanceReportDto {
  department: string;
  submissionCount: number;
  achievedCount: number;
  averageAchievementPercent?: number | null;
}

export interface PerformanceReportSummaryDto {
  submissionKind: number;
  reportingPeriodPublicId?: string | null;
  generatedAt: string;
  targetCount: number;
  submissionCount: number;
  achievedCount: number;
  atRiskCount: number;
  pendingCount: number;
  averageAchievementPercent?: number | null;
  departments: DepartmentPerformanceReportDto[];
}

export type OfficialReportFormat = 1 | 2 | 3 | 4;
export type OfficialReportType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16;

export interface OfficialReportTemplateDto {
  publicId: string;
  templateFamilyPublicId: string;
  municipalityFinancialYearPublicId?: string | null;
  financialYearCode?: string | null;
  submissionKind: number;
  reportType: OfficialReportType;
  code: string;
  name: string;
  format: OfficialReportFormat;
  versionNumber: number;
  headingTemplate: string;
  columns: string[];
  isCurrent: boolean;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  approvalReference: string;
  reason: string;
  createdAt: string;
  rowVersion: string;
}

export interface OfficialReportGenerationDto {
  publicId: string;
  generationFamilyPublicId: string;
  versionNumber: number;
  templatePublicId: string;
  templateCode: string;
  templateName: string;
  templateVersion: number;
  format: OfficialReportFormat;
  submissionKind: number;
  reportType: OfficialReportType;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  reportingPeriodPublicId: string;
  reportingPeriodCode: string;
  scopeJson: string;
  filterJson: string;
  dataVersionReference: string;
  fileName: string;
  contentType: string;
  sizeInBytes: number;
  sha256: string;
  rowCount: number;
  generatedBy: string;
  generatedAt: string;
  downloadUrl: string;
}

export type OfficialReportScheduleCadence = 1 | 2 | 3 | 4;
export type OfficialReportRecipientKind = 1 | 2;
export type OfficialReportJobState = 1 | 2 | 3 | 4 | 5 | 6;

export interface OfficialReportScheduleDto {
  publicId: string;
  scheduleFamilyPublicId: string;
  versionNumber: number;
  previousVersionPublicId?: string | null;
  templatePublicId: string;
  templateName: string;
  reportType: OfficialReportType;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  reportingPeriodPublicId: string;
  reportingPeriodCode: string;
  departmentPublicId?: string | null;
  departmentName?: string | null;
  unitPublicId?: string | null;
  unitName?: string | null;
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
  isCurrent: boolean;
  isActive: boolean;
  approvalReference: string;
  reason: string;
  createdBy: string;
  createdAt: string;
  rowVersion: string;
}

export interface OfficialReportJobDto {
  publicId: string;
  schedulePublicId?: string | null;
  scheduleName?: string | null;
  state: OfficialReportJobState;
  templatePublicId: string;
  templateName: string;
  reportType: OfficialReportType;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  reportingPeriodPublicId: string;
  reportingPeriodCode: string;
  departmentPublicId?: string | null;
  departmentName?: string | null;
  unitPublicId?: string | null;
  unitName?: string | null;
  scheduledFor: string;
  availableAt: string;
  attemptCount: number;
  startedAt?: string | null;
  completedAt?: string | null;
  lastError?: string | null;
  requestedBy: string;
  requestedAt: string;
  generationPublicId?: string | null;
  fileName?: string | null;
  distributionOutboxPublicId?: string | null;
  recipientUserIds: string[];
  channels: string[];
  isMandatoryDistribution: boolean;
  retryReason?: string | null;
  rowVersion: string;
}

export type SecurityPermissionState = 'ALLOW' | 'DENY';

export interface SecurityPermissionDefinition {
  code: string;
  description?: string;
  kind: 'Resource' | 'Navigation' | 'Member' | 'Action' | 'Report';
  resourceCode?: string;
  operation?: string;
  memberCode?: string;
  navigationCode?: string;
  actionCode?: string;
}

export interface SecurityNavigationItemDto {
  publicId: string;
  code: string;
  parentPublicId?: string | null;
  parentCode?: string | null;
  parentName?: string | null;
  name: string;
  route?: string | null;
  iconKey?: string | null;
  displayOrder: number;
  requiredPermissionCode?: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface SecurityResourceDefinitionDto {
  publicId: string;
  code: string;
  name: string;
  type: 'ENTITY' | 'REPORT' | 'WORKFLOW' | 'SERVICE';
  description?: string | null;
  canCreate: boolean;
  canRead: boolean;
  canUpdate: boolean;
  canDelete: boolean;
  canExport: boolean;
  canImport: boolean;
  supportsMembers: boolean;
  supportsCriteria: boolean;
  isActive: boolean;
  rowVersion: string;
}

export interface SecurityActionDefinitionDto {
  publicId: string;
  code: string;
  name: string;
  resourceCode: string;
  description?: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface SecurityMemberDefinitionDto {
  publicId: string;
  resourceCode: string;
  memberCode: string;
  displayName: string;
  isSensitive: boolean;
  isSystemManaged: boolean;
  isActive: boolean;
  rowVersion: string;
}

export interface RoleSecurityPermission {
  permissionCode: string;
  kind: string;
  resourceCode?: string;
  memberCode?: string;
  navigationCode?: string;
  actionCode?: string;
  state: SecurityPermissionState;
  scopeType?: string;
  rowVersion: string;
}

export interface RoleSecurityConfiguration {
  roleId: string;
  publicId: string;
  name: string;
  roleRowVersion: string;
  permissions: RoleSecurityPermission[];
}

export interface EffectiveSecurityPreview {
  userId: string;
  roles: string[];
  permissions: string[];
  scopes: string[];
  assignments: string[];
}

export interface SecurityRoleSummary extends AdminRole {
  publicId: string;
  roleCode: string;
  municipalityId?: number;
  effectiveFrom: string;
  effectiveTo?: string;
  rowVersion: string;
}

export interface SecurityUserSummary {
  id: string;
  fullName: string;
  email: string;
}

export interface SecurityUserRoleAssignment {
  id: number;
  roleId: string;
  roleName: string;
  municipalityId?: number;
  departmentId?: number;
  departmentPublicId?: string;
  departmentName?: string;
  unitId?: number;
  unitPublicId?: string;
  unitName?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  rowVersion: string;
}

export interface SecurityUserRoleConfiguration {
  userId: string;
  userName: string;
  assignments: SecurityUserRoleAssignment[];
}

export interface UserPermissionOverride {
  permissionId: number;
  code: string;
  isAllowed: boolean;
  reason?: string;
}

export interface UserPermissions {
  fromRoles: string[];
  overrides: UserPermissionOverride[];
  effective: string[];
}

export interface LoginAuditLog {
  id: number;
  userId?: string | null;
  email: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  success: boolean;
  failureReason?: string | null;
  loggedAt: string;
}

export interface RoleImplementationAuditRow {
  role: string;
  dashboard: boolean;
  menus: boolean;
  crud: boolean;
  scopeFiltering: boolean;
  notifications: boolean;
  reports: boolean;
  auditTrail: boolean;
  complete: boolean;
}

export interface AccessSimulationResult {
  allowed: boolean;
  reason: string;
  effectivePermissions: string[];
  matchedScopes: string[];
  matchedAssignments: string[];
}

export interface RoleAccessMatrixRow {
  role: string;
  permissions: string[];
  scope: string[];
  menus: string[];
  allowedActions: string[];
  reports: string[];
  testUser?: string | null;
}

export interface SystemCoverageAuditRow {
  role: string;
  seededUser: boolean;
  dashboard: boolean;
  menu: boolean;
  permissions: boolean;
  scopeFiltering: boolean;
  crud: boolean;
  workflowActions: boolean;
  reports: boolean;
  auditTrail: boolean;
  notifications: boolean;
}

export interface OpmsTargetTemplateDto {
  id: number;
  templateCode: string;
  templateName: string;
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription?: string | null;
  targetUnitType: string;
  unitOfMeasure?: string | null;
  nationalKpa?: string | null;
  municipalKpa?: string | null;
  strategicGoal?: string | null;
  strategicObjective?: string | null;
  performanceObjective?: string | null;
  outcome?: string | null;
  output?: string | null;
  priorityIssue?: string | null;
  budgetSource?: string | null;
  budgetType?: string | null;
  weight: number;
  kpiType?: string | null;
  indicatorType?: string | null;
  functionalArea?: string | null;
  standardClassification?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  defaultQuarterlyTargetsJson?: string | null;
  defaultBudgetInformation?: string | null;
  defaultPoeRequirements?: string | null;
  isActive: boolean;
  isArchived: boolean;
  version: number;
  createdBy?: string | null;
  createdDate: string;
}

export interface IpmsTargetTemplateDto {
  id: number;
  templateCode: string;
  templateName: string;
  targetName: string;
  kpiDescription: string;
  performanceArea?: string | null;
  employeeLevel?: string | null;
  jobGrade?: string | null;
  targetUnitType: string;
  unitOfMeasure?: string | null;
  annualTarget: number;
  annualTargetDescription?: string | null;
  weight: number;
  defaultRatingMethod?: string | null;
  defaultScoreScale?: string | null;
  defaultPoeRequirements?: string | null;
  defaultTaskTemplatesJson?: string | null;
  linkedOpmsTargetRequired: boolean;
  functionalArea?: string | null;
  isActive: boolean;
  isArchived: boolean;
  version: number;
  createdBy?: string | null;
  createdDate: string;
}

export interface OpmsTargetDto {
  id: string;
  publicId: string;
  rowVersion: string;
  municipalityFinancialYearPublicId?: string | null;
  sdbipLayerPublicId?: string | null;
  sdbipLayerCode?: string | null;
  sdbipLayerName?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentPublicId?: string | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitPublicId?: string | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  wardIds: number[];
  additionalAssigneeIds: string[];
  voteNumberIds: number[];
  indicatorNumber: string;
  isIndicatorNumberRevised: boolean;
  revisedIndicatorNumber?: string | null;
  originalOrderNumber: number;
  revisedOrderNumber: number;
  nationalKpa: string;
  municipalKpa: string;
  nationalKpaPublicId?: string | null;
  municipalKpaPublicId?: string | null;
  backToBasicsPillarPublicId?: string | null;
  backToBasicsPillar?: string | null;
  strategicGoalPublicId?: string | null;
  strategicInterventionPublicId?: string | null;
  strategicIntervention?: string | null;
  strategicObjectivePublicId?: string | null;
  performanceObjectivePublicId?: string | null;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  isTargetNameRevised: boolean;
  revisedTargetName?: string | null;
  kpiDescription: string;
  isKpiDescriptionRevised: boolean;
  revisedKpiDescription?: string | null;
  baseline: number;
  baselineDescription?: string | null;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  budgetTypePublicId?: string | null;
  budgetTypeName?: string | null;
  budgetSources?: KpiBudgetSource[];
  unitOfMeasureId?: number | null;
  kpiUnitOfMeasurePublicId?: string | null;
  kpiUnitOfMeasureName?: string | null;
  kpiUnitOfMeasureSymbol?: string | null;
  weight: number;
  kpiType: string;
  kpiTypePublicId?: string | null;
  indicatorType: string;
  indicatorTypePublicId?: string | null;
  functionalArea?: string | null;
  functionalAreaPublicId?: string | null;
  standardClassification?: string | null;
  standardClassificationPublicId?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string | null;
  withdrawnAt?: string | null;
  withdrawnByUserId?: string | null;
  periodTargets: PerformancePeriodTargetDto[];
  createdAt: string;
}

export interface IpmsTargetDto {
  id: string;
  publicId: string;
  rowVersion: string;
  municipalityFinancialYearPublicId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  relatedOpmsTargetId?: string | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentPublicId?: string | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitPublicId?: string | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  supervisorId?: string | null;
  indicatorNumber: string;
  isIndicatorNumberRevised: boolean;
  revisedIndicatorNumber?: string | null;
  originalOrderNumber: number;
  revisedOrderNumber: number;
  nationalKpa: string;
  municipalKpa: string;
  nationalKpaPublicId?: string | null;
  municipalKpaPublicId?: string | null;
  backToBasicsPillarPublicId?: string | null;
  backToBasicsPillar?: string | null;
  strategicGoalPublicId?: string | null;
  strategicInterventionPublicId?: string | null;
  strategicIntervention?: string | null;
  strategicObjectivePublicId?: string | null;
  performanceObjectivePublicId?: string | null;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  isTargetNameRevised: boolean;
  revisedTargetName?: string | null;
  kpiDescription: string;
  isKpiDescriptionRevised: boolean;
  revisedKpiDescription?: string | null;
  baseline: number;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  budgetTypePublicId?: string | null;
  budgetTypeName?: string | null;
  budgetSources?: KpiBudgetSource[];
  unitOfMeasureId?: number | null;
  kpiUnitOfMeasurePublicId?: string | null;
  kpiUnitOfMeasureName?: string | null;
  kpiUnitOfMeasureSymbol?: string | null;
  weight: number;
  kpiType: string;
  kpiTypePublicId?: string | null;
  indicatorType: string;
  indicatorTypePublicId?: string | null;
  functionalArea?: string | null;
  functionalAreaPublicId?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string | null;
  withdrawnAt?: string | null;
  withdrawnByUserId?: string | null;
  periodTargets: PerformancePeriodTargetDto[];
  createdAt: string;
}

export interface OpmsSubmissionDto {
  id: string;
  rowVersion: string;
  baseState: string;
  opmsTargetId: string;
  targetName: string;
  targetIndicatorNumber: string;
  quarter: string;
  status: string;
  submitterStatus?: string | null;
  verifierStatus?: string | null;
  approverStatus?: string | null;
  pmsStatus?: string | null;
  auditorStatus?: string | null;
  actualPerformance?: string | null;
  systemSuggestedActualPerformance?: string | null;
  wasSystemSuggestionEdited?: boolean;
  suggestionGeneratedDate?: string | null;
  suggestionEditedByUserId?: string | null;
  suggestionEditedAt?: string | null;
  suggestionEditReason?: string | null;
  achievementPercent?: number | null;
  targetAchieved?: boolean | null;
  reportingPeriodPublicId?: string | null;
  actualExpenditure?: number | null;
  variance?: number | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submitterScore?: number | null;
  submittedAt?: string | null;
  submittedByUserId?: string | null;
  submittedByName?: string | null;
  verifierUserId?: string | null;
  verifierName?: string | null;
  verifiedAt?: string | null;
  verifierComments?: string | null;
  verifierComment?: string | null;
  verifierScore?: number | null;
  approverUserId?: string | null;
  approverName?: string | null;
  approvedAt?: string | null;
  approverComments?: string | null;
  approverComment?: string | null;
  approverScore?: number | null;
  pmsOfficerUserId?: string | null;
  pmsOfficerName?: string | null;
  pmsReviewedAt?: string | null;
  pmsComments?: string | null;
  pmsComment?: string | null;
  pmsRecommendation?: string | null;
  pmsScore?: number | null;
  pmsResponseDueDate?: string | null;
  pmsRfiComment?: string | null;
  auditorUserId?: string | null;
  auditorName?: string | null;
  auditedAt?: string | null;
  auditorComments?: string | null;
  auditorComment?: string | null;
  auditorRecommendation?: string | null;
  auditorScore?: number | null;
  auditorResponseDueDate?: string | null;
  dueDate?: string | null;
  extendedDueDate?: string | null;
  dueDateExtendedDays?: number | null;
  poeType?: string | null;
  isDisabled?: boolean | null;
  withdrawalReason?: string | null;
  withdrawnAt?: string | null;
  withdrawnByUserId?: string | null;
  createdBy?: string | null;
  createdOn?: string | null;
  updatedBy?: string | null;
  updatedOn?: string | null;
  organisationId?: string | null;
  createdAt: string;
}

export interface IpmsSubmissionDto {
  id: string;
  rowVersion: string;
  baseState: string;
  ipmsTargetId: string;
  targetName: string;
  targetIndicatorNumber: string;
  quarter: string;
  status: string;
  submitterStatus?: string | null;
  verifierStatus?: string | null;
  approverStatus?: string | null;
  pmsStatus?: string | null;
  auditorStatus?: string | null;
  actualPerformance?: string | null;
  systemSuggestedActualPerformance?: string | null;
  wasSystemSuggestionEdited?: boolean;
  suggestionGeneratedDate?: string | null;
  suggestionEditedByUserId?: string | null;
  suggestionEditedAt?: string | null;
  suggestionEditReason?: string | null;
  achievementPercent?: number | null;
  targetAchieved?: boolean | null;
  reportingPeriodPublicId?: string | null;
  actualExpenditure?: number | null;
  variance?: number | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submitterScore?: number | null;
  submittedAt?: string | null;
  submittedByUserId?: string | null;
  submittedByName?: string | null;
  verifierUserId?: string | null;
  verifierName?: string | null;
  verifiedAt?: string | null;
  verifierComments?: string | null;
  verifierComment?: string | null;
  verifierScore?: number | null;
  approverUserId?: string | null;
  approverName?: string | null;
  approvedAt?: string | null;
  approverComments?: string | null;
  approverComment?: string | null;
  approverScore?: number | null;
  pmsOfficerUserId?: string | null;
  pmsOfficerName?: string | null;
  pmsReviewedAt?: string | null;
  pmsComments?: string | null;
  pmsComment?: string | null;
  pmsRecommendation?: string | null;
  pmsScore?: number | null;
  pmsResponseDueDate?: string | null;
  pmsRfiComment?: string | null;
  auditorUserId?: string | null;
  auditorName?: string | null;
  auditedAt?: string | null;
  auditorComments?: string | null;
  auditorComment?: string | null;
  auditorRecommendation?: string | null;
  auditorScore?: number | null;
  auditorResponseDueDate?: string | null;
  dueDate?: string | null;
  extendedDueDate?: string | null;
  dueDateExtendedDays?: number | null;
  poeType?: string | null;
  isDisabled?: boolean | null;
  withdrawalReason?: string | null;
  withdrawnAt?: string | null;
  withdrawnByUserId?: string | null;
  createdBy?: string | null;
  createdOn?: string | null;
  updatedBy?: string | null;
  updatedOn?: string | null;
  organisationId?: string | null;
  createdAt: string;
}

export interface NotificationDto {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  entityName?: string | null;
  entityId?: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface PerformanceSuggestionResult {
  generated: boolean;
  manualRequired: boolean;
  code: string;
  explanation: string;
  systemSuggestedActualPerformance?: string | null;
  actualPerformance?: string | null;
  wasSystemSuggestionEdited: boolean;
  sourcePeriods: string[];
}

export interface PerformanceSuggestionEvent {
  publicId: string;
  eventType: 'Generated' | 'Accepted' | 'Edited';
  systemSuggestedActualPerformance?: string | null;
  actualPerformance?: string | null;
  wasSystemSuggestionEdited: boolean;
  effectiveCalculationType?: string | null;
  sourcePeriods: string[];
  actorUserId: string;
  reason?: string | null;
  occurredAt: string;
  correlationId: string;
}

export interface NotificationPageResult extends PagedResult<NotificationDto> {
  unreadCount: number;
}

export interface AuditTrailEntryDto {
  id: number;
  publicId: string;
  municipalityId?: number | null;
  entityName: string;
  entityId: string;
  action: string;
  oldValue?: string | null;
  newValue?: string | null;
  changedBy: string;
  changedAt: string;
  ipAddress?: string | null;
  correlationId?: string | null;
  reason?: string | null;
  userAgent?: string | null;
  sessionId?: string | null;
}

export interface PoeFileDto {
  id: string;
  publicId?: string;
  evidenceBlobPublicId?: string;
  submissionKind: string;
  submissionId: string;
  fileName: string;
  contentType?: string | null;
  sizeInBytes: number;
  uploadedByUserId: string;
  uploadedByName?: string | null;
  uploadedAt: string;
  url: string;
  sha256?: string;
  signatureVerified?: boolean;
  scanStatus?: string;
  isQuarantined?: boolean;
  assessments?: PoeEvidenceAssessmentDto[];
  rowVersion?: string;
  replacementOf?: PoeEvidenceReplacementDto | null;
  replacedBy?: PoeEvidenceReplacementDto | null;
  legalHolds?: PoeLegalHoldDto[];
  isActive?: boolean;
  disposals?: PoeDisposalDto[];
  isContentDeleted?: boolean;
  scannerProvider?: string | null;
  scannerReference?: string | null;
  scanDetail?: string | null;
  scannedAt?: string | null;
  retainUntil?: string | null;
}

export interface SaveOpmsTargetTemplatePayload {
  templateCode: string;
  templateName: string;
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription?: string | null;
  targetUnitType: string;
  unitOfMeasure?: string | null;
  nationalKpa?: string | null;
  municipalKpa?: string | null;
  strategicGoal?: string | null;
  strategicObjective?: string | null;
  performanceObjective?: string | null;
  outcome?: string | null;
  output?: string | null;
  priorityIssue?: string | null;
  budgetSource?: string | null;
  budgetType?: string | null;
  weight: number;
  kpiType?: string | null;
  indicatorType?: string | null;
  functionalArea?: string | null;
  standardClassification?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  defaultQuarterlyTargetsJson?: string | null;
  defaultBudgetInformation?: string | null;
  defaultPoeRequirements?: string | null;
  isActive: boolean;
}

export interface SaveIpmsTargetTemplatePayload {
  templateCode: string;
  templateName: string;
  targetName: string;
  kpiDescription: string;
  performanceArea?: string | null;
  employeeLevel?: string | null;
  jobGrade?: string | null;
  targetUnitType: string;
  unitOfMeasure?: string | null;
  annualTarget: number;
  annualTargetDescription?: string | null;
  weight: number;
  defaultRatingMethod?: string | null;
  defaultScoreScale?: string | null;
  defaultPoeRequirements?: string | null;
  defaultTaskTemplatesJson?: string | null;
  linkedOpmsTargetRequired: boolean;
  functionalArea?: string | null;
  isActive: boolean;
}

export interface SaveOpmsTargetPayload {
  sdbipLayerPublicId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentPublicId?: string | null;
  unitId?: number | null;
  unitPublicId?: string | null;
  assignedUserId?: string | null;
  wardIds?: number[];
  additionalAssigneeIds?: string[];
  voteNumberIds?: number[];
  indicatorNumber: string;
  originalOrderNumber?: number;
  nationalKpa: string;
  municipalKpa: string;
  nationalKpaPublicId: string;
  municipalKpaPublicId: string;
  backToBasicsPillarPublicId?: string | null;
  strategicGoalPublicId?: string | null;
  strategicInterventionPublicId?: string | null;
  strategicObjectivePublicId?: string | null;
  performanceObjectivePublicId: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  baselineDescription?: string | null;
  budgetTypePublicId?: string | null;
  budgetSources: Array<{ budgetSourcePublicId: string; amount?: number | null }>;
  kpiUnitOfMeasurePublicId: string;
  weight: number;
  kpiType: string;
  kpiTypePublicId: string;
  indicatorType: string;
  indicatorTypePublicId: string;
  functionalArea?: string | null;
  functionalAreaPublicId?: string | null;
  standardClassification?: string | null;
  standardClassificationPublicId?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  isRevised: boolean;
  periodTargets: SaveTargetPeriodValuePayload[];
}

export interface SaveIpmsTargetPayload {
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  relatedOpmsTargetId?: string | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentPublicId?: string | null;
  unitId?: number | null;
  unitPublicId?: string | null;
  assignedUserId?: string | null;
  supervisorId?: string | null;
  indicatorNumber: string;
  originalOrderNumber?: number;
  nationalKpa: string;
  municipalKpa: string;
  nationalKpaPublicId: string;
  municipalKpaPublicId: string;
  backToBasicsPillarPublicId?: string | null;
  strategicGoalPublicId?: string | null;
  strategicInterventionPublicId?: string | null;
  strategicObjectivePublicId?: string | null;
  performanceObjectivePublicId: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  budgetTypePublicId?: string | null;
  budgetSources: Array<{ budgetSourcePublicId: string; amount?: number | null }>;
  kpiUnitOfMeasurePublicId: string;
  weight: number;
  kpiType: string;
  kpiTypePublicId: string;
  indicatorType: string;
  indicatorTypePublicId: string;
  functionalArea?: string | null;
  functionalAreaPublicId?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  isRevised: boolean;
  periodTargets: SaveTargetPeriodValuePayload[];
}

export interface SaveTargetPeriodValuePayload {
  periodType: 1 | 2 | 3 | 4 | 5 | 6;
  unitKind: number;
  direction: 1 | 2 | 3;
  opmsUnitPublicId?: string | null;
  performanceDirectionPublicId?: string | null;
  targetValue: string;
  budgetValue?: number | null;
  description?: string | null;
}

export interface SaveOpmsSubmissionPayload {
  opmsTargetId: string;
  quarter: string;
  actualPerformance?: string | null;
  actualExpenditure?: number | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submitterScore?: number | null;
  poeType?: string | null;
  dueDate?: string | null;
  extendedDueDate?: string | null;
}

export interface SaveIpmsSubmissionPayload {
  ipmsTargetId: string;
  quarter: string;
  actualPerformance?: string | null;
  actualExpenditure?: number | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submitterScore?: number | null;
  poeType?: string | null;
  dueDate?: string | null;
  extendedDueDate?: string | null;
}

export interface SubmissionWorkflowActionPayload {
  comment?: string | null;
  score?: number | null;
  recommendation?: string | null;
  responseDueDate?: string | null;
  rfiComment?: string | null;
}

export interface DueDateExtensionPayload {
  extendedDueDate: string;
  reason: string;
}

export interface IdpPlanSummary {
  id: number;
  publicId: string;
  municipalityName: string;
  planTitle: string;
  planCode: string;
  startFinancialYear: number;
  endFinancialYear: number;
  status: string;
  currentVersionNumber: number;
  createdAt: string;
  approvedAt?: string | null;
  rowVersion: string;
  planFamilyId: string;
  predecessorPlanPublicId?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  publishedAt?: string | null;
  publicationReference?: string | null;
}

export interface IdpPlanVersion {
  id: number;
  publicId: string;
  idpPlanId: number;
  predecessorVersionPublicId?: string | null;
  versionNumber: number;
  versionType: string;
  versionLabel: string;
  reviewYear?: string | null;
  summaryOfChanges?: string | null;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
  publishedAt?: string | null;
  publicationReference?: string | null;
  rowVersion: string;
}

export interface IdpStrategicOutcome {
  id: number;
  publicId: string;
  rowVersion: string;
  idpPlanId: number;
  code: string;
  name: string;
  description: string;
  sortOrder: number;
}

export interface IdpStrategicObjective {
  id: number;
  publicId: string;
  rowVersion: string;
  idpStrategicOutcomeId: number;
  code: string;
  name: string;
  description: string;
  baselineValue: number;
  targetValue: number;
  responsibleDepartmentId?: number | null;
  responsibleDepartmentName?: string | null;
  strategicOwnerUserId?: string | null;
  strategicOwnerName?: string | null;
  startDate: string;
  endDate: string;
  budgetAllocation: number;
  sortOrder: number;
}

export interface IdpDevelopmentPriority {
  id: number;
  publicId: string;
  rowVersion: string;
  idpStrategicObjectiveId: number;
  priorityCode: string;
  name: string;
  description: string;
  sortOrder: number;
}

export interface IdpProgramme {
  id: number;
  publicId: string;
  rowVersion: string;
  idpDevelopmentPriorityId: number;
  programmeCode: string;
  name: string;
  description: string;
  responsibleDepartmentId?: number | null;
  responsibleDepartmentName?: string | null;
  plannedBudget: number;
  approvedBudget: number;
  actualExpenditure: number;
}

export interface IdpProject {
  id: number;
  publicId: string;
  rowVersion: string;
  idpProgrammeId: number;
  projectCode: string;
  projectName: string;
  description: string;
  category: string;
  departmentId?: number | null;
  departmentName?: string | null;
  budget: number;
  fundingSource: string;
  startDate: string;
  endDate: string;
  status: string;
  communityNeedReference?: string | null;
}

export interface IdpKpi {
  id: number;
  publicId: string;
  idpProjectId: number;
  kpiCode: string;
  kpiName: string;
  description: string;
  formula: string;
  baseline: number;
  annualTarget: number;
  fiveYearTarget: number;
  responsibleDepartmentId?: number | null;
  responsibleDepartmentName?: string | null;
  dataSource: string;
  reportingFrequency: string;
  indicatorType: string;
  circular88Linked: boolean;
  treasuryTidLinked: boolean;
  rowVersion: string;
}

export type IdpImportRowStatus = 'New' | 'Unchanged' | 'Changed' | 'Invalid';

export interface IdpImportRow {
  publicId: string;
  sourceRowNumber: number;
  reference: string;
  status: IdpImportRowStatus;
  existingValueJson?: string | null;
  normalizedJson?: string | null;
  errorCode?: string | null;
  errorField?: string | null;
  suppliedValue?: string | null;
  errorMessage?: string | null;
}

export interface IdpImportBatch {
  publicId: string;
  clientRequestId: string;
  idpPlanPublicId: string;
  importType: string;
  sourceFileName: string;
  sourceSha256: string;
  status: 'Staged' | 'Committed' | 'Cancelled';
  totalRows: number;
  newRows: number;
  unchangedRows: number;
  changedRows: number;
  invalidRows: number;
  createdByUserId: string;
  createdAt: string;
  committedByUserId?: string | null;
  committedAt?: string | null;
  rowVersion: string;
  rows: IdpImportRow[];
}

export type IdpImportBatchSummary = Omit<IdpImportBatch, 'rows'>;

export interface IdpKpiImportRowPayload {
  sourceRowNumber: number;
  projectCode: string;
  kpiCode: string;
  kpiName: string;
  description: string;
  formula: string;
  baseline: number;
  annualTarget: number;
  fiveYearTarget: number;
  responsibleDepartmentCode?: string | null;
  dataSource: string;
  reportingFrequency: string;
  indicatorType: string;
  circular88Linked: boolean;
  treasuryTidLinked: boolean;
}

export interface IdpHierarchyImportRowPayload {
  sourceRowNumber: number;
  outcomeCode: string;
  outcomeName: string;
  outcomeDescription: string;
  outcomeSortOrder: number;
  objectiveCode: string;
  objectiveName: string;
  objectiveDescription: string;
  objectiveBaseline: number;
  objectiveTarget: number;
  objectiveDepartmentCode?: string | null;
  objectiveStartDate: string;
  objectiveEndDate: string;
  objectiveBudget: number;
  objectiveSortOrder: number;
  priorityCode: string;
  priorityName: string;
  priorityDescription: string;
  prioritySortOrder: number;
  programmeCode: string;
  programmeName: string;
  programmeDescription: string;
  programmeDepartmentCode?: string | null;
  programmePlannedBudget: number;
  programmeApprovedBudget: number;
  programmeActualExpenditure: number;
  projectCode: string;
  projectName: string;
  projectDescription: string;
  projectCategory: string;
  projectDepartmentCode?: string | null;
  projectBudget: number;
  projectFundingSource: string;
  projectStartDate: string;
  projectEndDate: string;
  projectStatus: string;
  communityNeedReference?: string | null;
}

export interface IdpAnnualTarget {
  id: number;
  idpKpiId: number;
  financialYear: number;
  targetValue: number;
  actualValue?: number | null;
  progressComment?: string | null;
}

export interface IdpAlignmentMatrixItem {
  strategicOutcomeCode: string;
  strategicOutcomeName: string;
  objectiveCode: string;
  objectiveName: string;
  frameworkType: string;
  frameworkReferenceCode: string;
  frameworkReferenceTitle: string;
}

export interface IdpAlignmentLink {
  id: number;
  idpStrategicObjectiveId: number;
  frameworkType: string;
  frameworkReferenceCode: string;
  frameworkReferenceTitle: string;
  notes?: string | null;
}

export interface IdpRiskLink {
  id: number;
  idpStrategicObjectiveId?: number | null;
  idpProjectId?: number | null;
  idpKpiId?: number | null;
  riskReference: string;
  riskTitle: string;
  mitigationPlan?: string | null;
  riskLevel: string;
}

export interface IdpBudgetSnapshot {
  id: number;
  idpStrategicObjectiveId?: number | null;
  idpProjectId?: number | null;
  financialYear: number;
  plannedBudget: number;
  approvedBudget: number;
  actualExpenditure: number;
  sourceSystem: string;
  capturedAt: string;
}

export interface IdpDocument {
  publicId: string;
  idpPlanPublicId: string;
  planVersionNumber?: number | null;
  category: string;
  title: string;
  fileName: string;
  downloadUrl: string;
  contentType?: string | null;
  sizeInBytes: number;
  versionNumber: number;
  isApproved: boolean;
  uploadedAt: string;
  uploadedByUserId: string;
  uploadedByName?: string | null;
  sha256: string;
  signatureVerified: boolean;
  scanStatus: string;
  isQuarantined: boolean;
  scannerProvider?: string | null;
  scannerReference?: string | null;
  scanDetail?: string | null;
  scannedAt?: string | null;
  retainUntil?: string | null;
  evidenceBlobPublicId: string;
  isContentDeleted: boolean;
  rowVersion: string;
}

export interface IdpWardParticipation {
  wardId: number;
  wardName: string;
  meetingCount: number;
  participantsCount: number;
  needsCaptured: number;
}

export interface IdpDashboard {
  planId: number;
  planTitle: string;
  outcomes: number;
  objectives: number;
  projects: number;
  kpis: number;
  communitySessions: number;
  risks: number;
  plannedBudget: number;
  approvedBudget: number;
  actualExpenditure: number;
  kpiAchievementRate: number;
  topRiskTitles: string[];
  wardParticipation: IdpWardParticipation[];
  alignmentCount: number;
}

export interface IdpHierarchy {
  plan: IdpPlanSummary;
  versions: IdpPlanVersion[];
  outcomes: IdpStrategicOutcome[];
  objectives: IdpStrategicObjective[];
  priorities: IdpDevelopmentPriority[];
  programmes: IdpProgramme[];
  projects: IdpProject[];
  kpis: IdpKpi[];
  annualTargets: IdpAnnualTarget[];
  alignmentLinks: IdpAlignmentLink[];
  riskLinks: IdpRiskLink[];
  budgetSnapshots: IdpBudgetSnapshot[];
}

export interface IdpReportDocument {
  reportName: string;
  contentType: string;
  fileName: string;
  content: number[];
}

export interface CreateIdpPlanPayload {
  municipalityName: string;
  planTitle: string;
  planCode: string;
  startFinancialYear: number;
  endFinancialYear: number;
  predecessorPlanPublicId?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  publicationReference?: string | null;
}

export interface CreateIdpPlanVersionPayload {
  versionType: string;
  versionLabel: string;
  reviewYear?: string | null;
  summaryOfChanges?: string | null;
  effectiveFrom?: string | null;
  publicationReference?: string | null;
}

export interface CreateIdpCommentPayload {
  idpPlanId: number;
  idpPlanVersionId?: number | null;
  entityName: string;
  entityId: string;
  comment: string;
}

export interface CreateIdpCommunitySessionPayload {
  idpPlanId: number;
  participationType: string;
  sessionDate: string;
  venue: string;
  wardId?: number | null;
  participantsCount: number;
  attendanceRegisterPath?: string | null;
  minutesPath?: string | null;
}

export interface TidSourceDocument {
  publicId: string;
  title: string;
  fileName: string;
  contentType?: string | null;
  sizeInBytes: number;
  sha256: string;
  scanStatus: string;
  isQuarantined: boolean;
  uploadedAt: string;
  uploadedByUserId: string;
  contentUrl: string;
}

export interface TidVersion {
  publicId: string;
  targetPublicId: string;
  versionNumber: number;
  previousVersionPublicId?: string | null;
  indicatorDefinition: string;
  purpose: string;
  dataSource: string;
  collectionMethod: string;
  calculationMethod: string;
  numeratorDescription?: string | null;
  denominatorDescription?: string | null;
  limitations?: string | null;
  assumptions?: string | null;
  verificationMethod: string;
  responsibleEmployeePublicId?: string | null;
  responsibleEmployeeName?: string | null;
  notes?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isCurrent: boolean;
  createdAt: string;
  createdByUserId: string;
  rowVersion: string;
  sourceDocuments: TidSourceDocument[];
}

export interface TidConfiguration {
  municipalityPublicId: string;
  tidEnabled: boolean;
  allKpisRequired: boolean;
  scopedKpiCount: number;
  currentTidCount: number;
  missingTidCount: number;
  rowVersion: string;
}

export interface TidRegisterItem {
  targetPublicId: string;
  indicatorNumber: string;
  targetName: string;
  departmentName?: string | null;
  unitName?: string | null;
  tidRequired: boolean;
  currentVersion?: TidVersion | null;
}

export interface SaveTidVersionPayload {
  indicatorDefinition: string;
  purpose: string;
  dataSource: string;
  collectionMethod: string;
  calculationMethod: string;
  numeratorDescription?: string | null;
  denominatorDescription?: string | null;
  limitations?: string | null;
  assumptions?: string | null;
  verificationMethod: string;
  responsibleEmployeePublicId?: string | null;
  notes?: string | null;
  effectiveFrom: string;
  previousVersionRowVersion?: string | null;
  reason: string;
}

export interface StrategicDocumentType {
  publicId: string;
  code: string;
  name: string;
  description?: string | null;
  allowsExternalLinks: boolean;
  isActive: boolean;
  displayOrder: number;
  rowVersion: string;
}

export interface StrategicDocumentEvent {
  publicId: string;
  action: string;
  reason: string;
  actorUserId: string;
  occurredAt: string;
}

export interface StrategicDocument {
  publicId: string;
  documentFamilyId: string;
  previousVersionPublicId?: string | null;
  versionNumber: number;
  municipalityFinancialYearPublicId: string;
  financialYearCode: string;
  financialYearName: string;
  documentTypePublicId: string;
  documentTypeCode: string;
  documentTypeName: string;
  sdbipLayer?: string | null;
  title: string;
  description?: string | null;
  documentDate: string;
  displayOrder: number;
  isCurrent: boolean;
  isActive: boolean;
  isApproved: boolean;
  approvedAt?: string | null;
  approvedByUserId?: string | null;
  approvalReference?: string | null;
  isPublished: boolean;
  publicationDate?: string | null;
  publishedAt?: string | null;
  publishedByUserId?: string | null;
  createdAt: string;
  createdByUserId: string;
  fileName?: string | null;
  contentType?: string | null;
  sizeInBytes?: number | null;
  sha256?: string | null;
  scanStatus?: string | null;
  isQuarantined: boolean;
  externalUrl?: string | null;
  contentUrl?: string | null;
  rowVersion: string;
  events: StrategicDocumentEvent[];
}

export interface SaveStrategicDocumentVersionPayload {
  municipalityFinancialYearPublicId: string;
  documentTypePublicId: string;
  previousVersionPublicId?: string | null;
  previousVersionRowVersion?: string | null;
  sdbipLayer?: string | null;
  title: string;
  description?: string | null;
  documentDate: string;
  displayOrder: number;
  externalUrl?: string | null;
  file?: File | null;
  reason: string;
}

export interface StrategicRiskDto {
  publicId: string;
  riskReference?: string | null;
  riskTitle: string;
  riskDescription?: string | null;
  effectiveFromMunicipalityFinancialYearPublicId?: string | null;
  effectiveFromFinancialYear?: string | null;
  effectiveToMunicipalityFinancialYearPublicId?: string | null;
  effectiveToFinancialYear?: string | null;
  isActive: boolean;
  activeKpiLinks: number;
  createdAt: string;
  updatedAt?: string | null;
  rowVersion: string;
}

export interface StrategicRiskKpiLinkDto {
  publicId: string;
  strategicRiskPublicId: string;
  riskReference?: string | null;
  riskTitle: string;
  targetPublicId: string;
  indicatorNumber: string;
  targetName: string;
  departmentName?: string | null;
  unitName?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  linkedAt: string;
  linkReason: string;
  unlinkedAt?: string | null;
  unlinkReason?: string | null;
  rowVersion: string;
}

export interface StrategicRiskSummaryDto {
  totalRisks: number;
  activeRisks: number;
  linkedRisks: number;
  unlinkedActiveRisks: number;
  linkedKpis: number;
}

export interface SaveStrategicRiskPayload {
  riskReference?: string | null;
  riskTitle: string;
  riskDescription?: string | null;
  effectiveFromMunicipalityFinancialYearPublicId?: string | null;
  effectiveToMunicipalityFinancialYearPublicId?: string | null;
  isActive: boolean;
  reason: string;
  rowVersion?: string | null;
}

export type C88CatalogueItemKind = 'Sector' | 'Outcome' | 'IndicatorType' | 'MunicipalCategory' | 'ReadinessTier' | 'ReportType' | 'ResponseType';
export type C88AssignmentRole = 'PrimaryCapturer' | 'Contributor' | 'ReviewerVerifier' | 'FinalSubmitter';
export type C88ReportState = 'Draft' | 'Submitted' | 'Verified' | 'FinalSubmitted' | 'Rework';

export interface C88Configuration { publicId: string; municipalityFinancialYearPublicId: string; financialYearCode: string; catalogueVersionPublicId: string; catalogueVersionCode: string; isEnabled: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string }
export interface C88CatalogueVersion { publicId: string; code: string; name: string; editionDate: string; effectiveFrom: string; effectiveTo?: string | null; isPublished: boolean; isActive: boolean; rowVersion: string }
export interface C88CatalogueItem { publicId: string; catalogueVersionPublicId: string; kind: C88CatalogueItemKind; code: string; name: string; description?: string | null; parentItemPublicId?: string | null; displayOrder: number; isActive: boolean; rowVersion: string }
export interface C88DataElement { publicId: string; code: string; name: string; description?: string | null; valueType: string; isRequired: boolean; sequence: number; rowVersion: string }
export interface C88Indicator { publicId: string; catalogueVersionPublicId: string; code: string; name: string; definition: string; officialTechnicalIndicatorDescription: string; sectorPublicId?: string | null; outcomePublicId?: string | null; indicatorTypePublicId?: string | null; valueType: string; calculationOperator: string; officialFormulaText?: string | null; requiresBaseline: boolean; requiresMediumTermTarget: boolean; requiresAnnualTarget: boolean; isActive: boolean; rowVersion: string; dataElements: C88DataElement[]; applicability: Array<{ publicId: string; municipalCategoryPublicId: string; readinessTierPublicId?: string | null; isApplicable: boolean; notes?: string | null }> }
export interface C88ComplianceQuestion { publicId: string; catalogueVersionPublicId: string; reportTypePublicId: string; responseTypePublicId: string; code: string; prompt: string; isRequired: boolean; sequence: number; isActive: boolean; rowVersion: string }
export interface C88IndicatorPlan { publicId: string; configurationPublicId: string; indicatorPublicId: string; indicatorCode: string; baselineValue?: string | null; mediumTermTarget?: string | null; annualTarget?: string | null; missingDataExplanation?: string | null; estimatedAvailability?: string | null; rowVersion: string }
export interface C88ReportingCalendar { publicId: string; configurationPublicId: string; reportTypePublicId: string; reportingPeriodPublicId?: string | null; code: string; name: string; opensAt: string; closesAt: string; dueAt: string; isActive: boolean; rowVersion: string }
export interface C88WorkflowAction { publicId: string; fromStageSequence: number; toStageSequence: number; action: string; reason: string; occurredAt: string }
export interface C88IndicatorReport { publicId: string; reportFamilyId: string; versionNumber: number; isCurrent: boolean; configurationPublicId: string; calendarPublicId: string; indicatorPublicId: string; indicatorCode: string; state: C88ReportState; currentStageSequence: number; calculatedValue?: string | null; missingDataExplanation?: string | null; estimatedAvailability?: string | null; createdAt: string; finalSubmittedAt?: string | null; rowVersion: string; dataElementValues: Array<{ publicId: string; dataElementPublicId: string; value?: string | null; missingDataExplanation?: string | null; estimatedAvailability?: string | null }>; complianceResponses: Array<{ publicId: string; questionPublicId: string; response?: string | null; comment?: string | null }>; workflowActions: C88WorkflowAction[] }
export interface C88Assignment { publicId: string; configurationPublicId: string; indicatorPublicId: string; employeePublicId: string; employeeName: string; role: C88AssignmentRole; effectiveFrom: string; effectiveTo?: string | null; isActive: boolean; rowVersion: string }
export interface C88Workflow { publicId: string; configurationPublicId: string; versionNumber: number; isCurrent: boolean; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string; stages: Array<{ publicId: string; sequence: number; kind: string; name: string; requiredRole: C88AssignmentRole; isActive: boolean }> }
export interface C88Mapping { publicId: string; configurationPublicId: string; indicatorPublicId: string; opmsTargetPublicId: string; opmsIndicatorNumber: string; mappingType: 'Direct' | 'Contributing'; reason: string; isActive: boolean; rowVersion: string }

export interface EnterpriseProviderOption { code: string; displayName: string; kind: 'MICROSOFT_ENTRA_ID' | 'ACTIVE_DIRECTORY' }
export interface EnterpriseSignInOptions { municipalityCode: string; municipalityName: string; localEnabled: boolean; providers: EnterpriseProviderOption[] }
export interface AuthenticationPolicyConfiguration { publicId: string; minimumPasswordLength: number; maximumFailedAttempts: number; lockoutMinutes: number; requireMfaForPrivilegedLocalUsers: boolean; requireMfaForAllLocalUsers: boolean; requireFirstLoginPasswordChange: boolean; sessionIdleTimeoutMinutes: number; sessionAbsoluteTimeoutHours: number; maximumConcurrentSessions: number; rowVersion: string }
export interface AuthenticationConfiguration { publicId: string; mode: 1 | 2 | 3 | 4; providerRegistrationCode?: string | null; displayName: string; isActive: boolean; effectiveFrom: string; effectiveTo?: string | null; rowVersion: string; policy?: AuthenticationPolicyConfiguration | null }
export interface UserAuthenticator { publicId: string; userPublicId: string; userEmail: string; providerRegistrationCode: string; expectedEmail: string; issuer?: string | null; subject?: string | null; isActive: boolean; linkedAt?: string | null; lastAuthenticatedAt?: string | null; rowVersion: string }
export interface AuthenticationEvent { publicId: string; userId?: string | null; providerCode: string; eventType: string; success: boolean; failureCode?: string | null; occurredAt: string; ipAddress?: string | null; correlationId: string }
