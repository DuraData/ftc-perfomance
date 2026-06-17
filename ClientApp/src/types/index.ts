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
  | 'binary_determination';

export type Quarter = 'Q1' | 'Q2' | 'Mid-Year' | 'Q3' | 'Q4' | 'Annual';

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
export interface OPMSTarget {
  id: string;
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
  submissions: OPMSSubmission[];
  voteNumbers: VoteNumber[];
  relatedIPMSTargets: IPMSTarget[];
  additionalAssignees: Employee[];
  attachments: Attachment[];
}

// IPMS Target
export interface IPMSTarget {
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
  q2Target?: number;
  midTermTarget?: number;
  q3Target?: number;
  q4Target?: number;

  targetUnitType: TargetUnitType;
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
  submitterScore?: number;
  status: SubmissionStatus;
  submitter?: Employee;
  submittedAt?: string;
  verifier?: Employee;
  verifiedAt?: string;
  verifierComments?: string;
  approver?: Employee;
  approvedAt?: string;
  approverComments?: string;
  pmsOfficer?: Employee;
  pmsReviewedAt?: string;
  pmsComments?: string;
  auditor?: Employee;
  auditedAt?: string;
  auditorComments?: string;
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
  actualExpenditure?: number;
  variance?: number;
  varianceReason?: string;
  correctiveMeasure?: string;
  submitterScore?: number;
  status: SubmissionStatus;
  submitter?: Employee;
  submittedAt?: string;
  verifier?: Employee;
  verifiedAt?: string;
  verifierComments?: string;
  approver?: Employee;
  approvedAt?: string;
  approverComments?: string;
  pmsOfficer?: Employee;
  pmsReviewedAt?: string;
  pmsComments?: string;
  auditor?: Employee;
  auditedAt?: string;
  auditorComments?: string;
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
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  departmentId?: number | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  kpiId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  baseline: number;
  annualTarget: number;
  weight: number;
  isArchived: boolean;
  createdAt: string;
}

export interface IpmsTargetDto {
  id: string;
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  departmentId?: number | null;
  departmentName?: string | null;
  unitId?: number | null;
  unitName?: string | null;
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  relatedOpmsTargetId?: string | null;
  kpiId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  annualTarget: number;
  weight: number;
  isArchived: boolean;
  createdAt: string;
}

export interface OpmsSubmissionDto {
  id: string;
  opmsTargetId: string;
  targetName: string;
  quarter: string;
  status: string;
  actual?: number | null;
  actualDescription?: string | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submittedAt?: string | null;
  submittedByUserId?: string | null;
  submittedByName?: string | null;
  dueDate?: string | null;
  createdAt: string;
}

export interface IpmsSubmissionDto {
  id: string;
  ipmsTargetId: string;
  targetName: string;
  quarter: string;
  status: string;
  actual?: number | null;
  actualDescription?: string | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  submittedAt?: string | null;
  submittedByUserId?: string | null;
  submittedByName?: string | null;
  dueDate?: string | null;
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
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  departmentId?: number | null;
  unitId?: number | null;
  assignedUserId?: string | null;
  kpiId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  baseline: number;
  annualTarget: number;
  weight: number;
  isArchived: boolean;
}

export interface SaveIpmsTargetPayload {
  indicatorNumber: string;
  targetName: string;
  kpiDescription: string;
  departmentId?: number | null;
  unitId?: number | null;
  assignedUserId?: string | null;
  relatedOpmsTargetId?: string | null;
  kpiId?: string | null;
  sourceTemplateId?: string | null;
  sourceTemplateVersion?: number | null;
  annualTarget: number;
  weight: number;
  isArchived: boolean;
}

export interface SaveOpmsSubmissionPayload {
  opmsTargetId: string;
  quarter: string;
  actual?: number | null;
  actualDescription?: string | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  dueDate?: string | null;
}

export interface SaveIpmsSubmissionPayload {
  ipmsTargetId: string;
  quarter: string;
  actual?: number | null;
  actualDescription?: string | null;
  varianceReason?: string | null;
  correctiveMeasure?: string | null;
  dueDate?: string | null;
}

export interface SubmissionWorkflowActionPayload {
  comment?: string | null;
  score?: number | null;
}

export interface DueDateExtensionPayload {
  extendedDueDate: string;
  reason: string;
}
