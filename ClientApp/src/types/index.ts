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
  PriorYearOpmsId?: string;
  sourceTemplateId?: string;
  sourceTemplateVersion?: number;
  period: Period;
  department: Department;
  unit?: DepartmentUnit;
  wards?: Ward[];
  assignedTo?: Employee;
  indicatorNumber: string;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal: StrategicGoal;
  strategicObjective: StrategicObjective;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  baselineDescription?: string;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSource: BudgetSource;
  budgetType: BudgetType;
  unitOfMeasure: UnitOfMeasure;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string;
  standardClassification?: string;
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
  UserSubmit?: UserSubmitChild[];
  submissions: OPMSSubmission[];
  voteNumbers: VoteNumber[];
  VoteNumbers?: OpmsVoteNumberChild[];
  relatedIPMSTargets: IPMSTarget[];
  RelatedIPMSTargets?: string[];
  additionalAssignees: Employee[];
  AdditionalAssignees?: AdditionalAssigneeChild[];
  attachments: Attachment[];
}

// IPMS Target
export interface IPMSTarget extends BaseKpiTargetXafFields, TypedTargetPeriodValues, TargetAuditMetaFields {
  id: string;
  sourceTemplateId?: string;
  sourceTemplateVersion?: number;
  relatedOPMSTarget?: OPMSTarget;
  period: Period;
  department: Department;
  unit?: DepartmentUnit;
  assignedTo?: Employee;
  indicatorNumber: string;
  nationalKPA: string;
  municipalKPA: string;
  strategicGoal: StrategicGoal;
  strategicObjective: StrategicObjective;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSource: BudgetSource;
  budgetType: BudgetType;
  unitOfMeasure: UnitOfMeasure;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string;
  idpReference?: string;
  internalReference?: string;
  isRevised: boolean;

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
  UserSubmit?: UserSubmitChild[];
  submissions: IPMSSubmission[];
  attachments: Attachment[];
}

// Submissions
export interface OPMSSubmission {
  id: string;
  target: OPMSTarget;
  quarter: Quarter;
  dueDate: string;
  extendedDueDate?: string;
  actual: number;
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
  target: IPMSTarget;
  quarter: Quarter;
  dueDate: string;
  extendedDueDate?: string;
  actual: number;
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
  fileName: string;
  fileSize: number;
  fileType: string;
  uploadedBy: Employee;
  uploadedAt: string;
  documentType: string;
  url: string;
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
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserProfile;
  roles: string[];
  permissions: string[];
  menu: MenuItem[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
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
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  wardIds?: string | null;
  additionalAssigneeIds?: string | null;
  voteNumberIds?: string | null;
  indicatorNumber: string;
  nationalKpa: string;
  municipalKpa: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  baselineDescription?: string | null;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  unitOfMeasureId?: number | null;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string | null;
  standardClassification?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string | null;
  targetUnitType: string;
  q1Target?: number | null;
  q1Description?: string | null;
  q1Budget?: number | null;
  q2Target?: number | null;
  q2Description?: string | null;
  q2Budget?: number | null;
  midTermTarget?: number | null;
  midTermDescription?: string | null;
  midTermBudget?: number | null;
  q3Target?: number | null;
  q3Description?: string | null;
  q3Budget?: number | null;
  q3RevisedTarget?: number | null;
  q4Target?: number | null;
  q4Description?: string | null;
  q4Budget?: number | null;
  q4RevisedTarget?: number | null;
  revisedAnnualTarget?: number | null;
  revisedAnnualBudget?: number | null;
  createdAt: string;
}

export interface IpmsTargetDto {
  id: string;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  relatedOpmsTargetId?: string | null;
  periodId?: number | null;
  departmentId?: number | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  supervisorId?: string | null;
  indicatorNumber: string;
  nationalKpa: string;
  municipalKpa: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  unitOfMeasureId?: number | null;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  isRevised: boolean;
  targetUnitType: string;
  q1Target?: number | null;
  q1Description?: string | null;
  q1Budget?: number | null;
  q2Target?: number | null;
  q2Description?: string | null;
  q2Budget?: number | null;
  midTermTarget?: number | null;
  midTermDescription?: string | null;
  midTermBudget?: number | null;
  q3Target?: number | null;
  q3Description?: string | null;
  q3Budget?: number | null;
  q3RevisedTarget?: number | null;
  q4Target?: number | null;
  q4Description?: string | null;
  q4Budget?: number | null;
  q4RevisedTarget?: number | null;
  revisedAnnualTarget?: number | null;
  revisedAnnualBudget?: number | null;
  createdAt: string;
}

export interface OpmsSubmissionDto {
  id: string;
  opmsTargetId: string;
  targetName: string;
  quarter: string;
  status: string;
  submitterStatus?: string | null;
  verifierStatus?: string | null;
  approverStatus?: string | null;
  pmsStatus?: string | null;
  auditorStatus?: string | null;
  actual?: number | null;
  actualDescription?: string | null;
  actualPerformanceDescription?: string | null;
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
  createdBy?: string | null;
  createdOn?: string | null;
  updatedBy?: string | null;
  updatedOn?: string | null;
  organisationId?: string | null;
  createdAt: string;
}

export interface IpmsSubmissionDto {
  id: string;
  ipmsTargetId: string;
  targetName: string;
  quarter: string;
  status: string;
  submitterStatus?: string | null;
  verifierStatus?: string | null;
  approverStatus?: string | null;
  pmsStatus?: string | null;
  auditorStatus?: string | null;
  actual?: number | null;
  actualDescription?: string | null;
  actualPerformanceDescription?: string | null;
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

export interface AuditTrailEntryDto {
  id: number;
  entityName: string;
  entityId: string;
  action: string;
  oldValue?: string | null;
  newValue?: string | null;
  changedBy: string;
  changedAt: string;
  ipAddress?: string | null;
}

export interface PoeFileDto {
  id: string;
  submissionKind: string;
  submissionId: string;
  fileName: string;
  contentType?: string | null;
  sizeInBytes: number;
  uploadedByUserId: string;
  uploadedByName?: string | null;
  uploadedAt: string;
  url: string;
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
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  periodId?: number | null;
  departmentId?: number | null;
  unitId?: number | null;
  assignedUserId?: string | null;
  wardIds?: string | null;
  additionalAssigneeIds?: string | null;
  voteNumberIds?: string | null;
  indicatorNumber: string;
  nationalKpa: string;
  municipalKpa: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  baselineDescription?: string | null;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  unitOfMeasureId?: number | null;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string | null;
  standardClassification?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  fmsLink?: string | null;
  isRevised: boolean;
  isWithdrawn: boolean;
  reasonForWithdrawal?: string | null;
  targetUnitType: string;
  q1Target?: number | null;
  q1Description?: string | null;
  q1Budget?: number | null;
  q2Target?: number | null;
  q2Description?: string | null;
  q2Budget?: number | null;
  midTermTarget?: number | null;
  midTermDescription?: string | null;
  midTermBudget?: number | null;
  q3Target?: number | null;
  q3Description?: string | null;
  q3Budget?: number | null;
  q3RevisedTarget?: number | null;
  q4Target?: number | null;
  q4Description?: string | null;
  q4Budget?: number | null;
  q4RevisedTarget?: number | null;
  revisedAnnualTarget?: number | null;
  revisedAnnualBudget?: number | null;
}

export interface SaveIpmsTargetPayload {
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  relatedOpmsTargetId?: string | null;
  periodId?: number | null;
  departmentId?: number | null;
  unitId?: number | null;
  assignedUserId?: string | null;
  supervisorId?: string | null;
  indicatorNumber: string;
  nationalKpa: string;
  municipalKpa: string;
  strategicGoalId?: number | null;
  strategicObjectiveId?: number | null;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: number;
  annualTarget: number;
  annualTargetDescription: string;
  budgetSourceId?: number | null;
  budgetTypeId?: number | null;
  unitOfMeasureId?: number | null;
  weight: number;
  kpiType: string;
  indicatorType: string;
  functionalArea?: string | null;
  idpReference?: string | null;
  internalReference?: string | null;
  isRevised: boolean;
  targetUnitType: string;
  q1Target?: number | null;
  q1Description?: string | null;
  q1Budget?: number | null;
  q2Target?: number | null;
  q2Description?: string | null;
  q2Budget?: number | null;
  midTermTarget?: number | null;
  midTermDescription?: string | null;
  midTermBudget?: number | null;
  q3Target?: number | null;
  q3Description?: string | null;
  q3Budget?: number | null;
  q3RevisedTarget?: number | null;
  q4Target?: number | null;
  q4Description?: string | null;
  q4Budget?: number | null;
  q4RevisedTarget?: number | null;
  revisedAnnualTarget?: number | null;
  revisedAnnualBudget?: number | null;
}

export interface SaveOpmsSubmissionPayload {
  opmsTargetId: string;
  quarter: string;
  actual?: number | null;
  actualDescription?: string | null;
  actualPerformanceDescription?: string | null;
  actualExpenditure?: number | null;
  variance?: number | null;
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
  actual?: number | null;
  actualDescription?: string | null;
  actualPerformanceDescription?: string | null;
  actualExpenditure?: number | null;
  variance?: number | null;
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
  municipalityName: string;
  planTitle: string;
  planCode: string;
  startFinancialYear: number;
  endFinancialYear: number;
  status: string;
  currentVersionNumber: number;
  createdAt: string;
  approvedAt?: string | null;
}

export interface IdpPlanVersion {
  id: number;
  idpPlanId: number;
  versionNumber: number;
  versionType: string;
  versionLabel: string;
  reviewYear?: string | null;
  summaryOfChanges?: string | null;
  isActive: boolean;
  createdAt: string;
  createdByUserId: string;
}

export interface IdpStrategicOutcome {
  id: number;
  idpPlanId: number;
  code: string;
  name: string;
  description: string;
  sortOrder: number;
}

export interface IdpStrategicObjective {
  id: number;
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
  idpStrategicObjectiveId: number;
  name: string;
  description: string;
  sortOrder: number;
}

export interface IdpProgramme {
  id: number;
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
  alignmentMatrix: IdpAlignmentMatrixItem[];
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
}

export interface CreateIdpPlanVersionPayload {
  versionType: string;
  versionLabel: string;
  reviewYear?: string | null;
  summaryOfChanges?: string | null;
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
