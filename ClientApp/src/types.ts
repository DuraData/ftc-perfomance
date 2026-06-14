import type { ReactNode } from "react";

export type ThemeMode = "light" | "dark";

export type AppRole =
  | "sysadmin"
  | "performance-manager"
  | "department-manager"
  | "submitter"
  | "verifier"
  | "approver"
  | "auditor"
  | "hr-admin"
  | "executive-viewer";

export type StatusTone =
  | "default"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "accent";

export type TargetUnitType =
  | "Percentage"
  | "Absolute Count"
  | "Financial"
  | "Area Based"
  | "Volume Based"
  | "Index Scores"
  | "Ratios"
  | "Time Based"
  | "Binary"
  | "Date"
  | "Readiness Scale"
  | "Qualitative"
  | "Zero Based"
  | "Reverse Cumulative"
  | "Reverse Non-Cumulative"
  | "Binary Determination";

export type WorkflowAction =
  | "save-draft"
  | "submit"
  | "verify"
  | "approve"
  | "reject"
  | "request-information"
  | "return-to-submitter"
  | "record-auditor-finding"
  | "complete";

export interface BaseEntity {
  id: string;
  name: string;
  code?: string;
  description?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  archived?: boolean;
}

export interface AuditEntry {
  id: string;
  action: string;
  actor: string;
  timestamp: string;
  notes: string;
}

export interface FileAsset {
  id: string;
  fileName: string;
  documentType: string;
  sizeLabel: string;
  uploadedBy: string;
  uploadedAt: string;
  progress?: number;
  uploading?: boolean;
  notes?: string;
}

export interface AdditionalAssignee {
  id: string;
  employeeId: string;
  employeeName: string;
  role: string;
  department: string;
}

export interface VoteNumber extends BaseEntity {
  voteCode: string;
  budgetSource: string;
  budgetType: string;
  amount: number;
  owner: string;
}

export interface OpmsUserSubmit extends BaseEntity {
  quarter: string;
  submitterName: string;
  actual: string;
  score: number;
  status: string;
  dueDate: string;
}

export interface IpmsUserSubmit extends BaseEntity {
  quarter: string;
  employeeName: string;
  actual: string;
  score: number;
  status: string;
  dueDate: string;
}

export interface QuarterlyTargetValue {
  value: string;
  description: string;
  unit: string;
  budget: string;
  revisedValue?: string;
  revisedBudget?: string;
}

export interface DynamicTargetUnitField {
  key: string;
  label: string;
  value: string;
}

export interface TargetBase extends BaseEntity {
  period: string;
  department: string;
  unit: string;
  wards: string;
  assignedTo: string;
  indicatorNumber: string;
  nationalKpa: string;
  municipalKpa: string;
  strategicGoal: string;
  strategicObjective: string;
  performanceObjective: string;
  targetName: string;
  kpiDescription: string;
  baseline: string;
  annualTargetValue: string;
  annualTargetDescription: string;
  budgetSource: string;
  budgetType: string;
  unitOfMeasure: string;
  weight: number;
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
  quarterlyTargets: {
    q1: QuarterlyTargetValue;
    q2: QuarterlyTargetValue;
    midTerm: QuarterlyTargetValue;
    q3: QuarterlyTargetValue;
    q4: QuarterlyTargetValue;
    annual: QuarterlyTargetValue;
  };
  dynamicTargetUnitFields: DynamicTargetUnitField[];
  additionalAssignees: AdditionalAssignee[];
  attachments: FileAsset[];
  auditHistory: AuditEntry[];
}

export interface OpmsTarget extends TargetBase {
  userSubmissions: OpmsUserSubmit[];
  voteNumbers: VoteNumber[];
  relatedIpmsTargets: Array<{
    id: string;
    indicatorNumber: string;
    targetName: string;
    assignedTo: string;
    weight: number;
    status: string;
  }>;
}

export interface IpmsTarget extends TargetBase {
  relatedOpmsTargetId: string;
  relatedOpmsTargetLabel: string;
  userSubmissions: IpmsUserSubmit[];
}

export interface KpiLibrary extends BaseEntity {
  templateCode: string;
  category: string;
  kpa: string;
  targetUnitType: TargetUnitType;
  defaultAnnualTarget: string;
  department: string;
}

export interface SubmissionBase extends BaseEntity {
  targetId: string;
  indicatorNumber: string;
  quarter: string;
  dueDate: string;
  extendedDueDate: string;
  actual: string;
  actualPerformanceDescription: string;
  actualExpenditure: string;
  variance: string;
  varianceReason: string;
  correctiveMeasure: string;
  submitterScore: string;
  submitterStatus: string;
  auditorStatus: string;
  auditorComment: string;
  auditorRecommendation: string;
  auditorScore: string;
  verifierStatus: string;
  verifierComment: string;
  verifierScore: string;
  approverComment: string;
  approverStatus: string;
  approverScore: string;
  pmsStatus: string;
  pmsComment: string;
  pmsRecommendation: string;
  pmsScore: string;
  pmsRfiComment: string;
  proofOfEvidenceUploads: FileAsset[];
  verificationInformation: AuditEntry[];
  approvalHistory: AuditEntry[];
  pmsHistory: AuditEntry[];
  auditorInformation: AuditEntry[];
  commentsAndHistory: AuditEntry[];
}

export interface OpmsSubmission extends SubmissionBase {
  submissionType: "OPMS";
}

export interface IpmsSubmission extends SubmissionBase {
  submissionType: "IPMS";
}

export interface Task extends BaseEntity {
  priority: "Low" | "Medium" | "High";
  assignedEmployeeIds: string[];
  assignedEmployees: string[];
  estimatedWorkHours: number;
  actualWorkHours: number;
  dueDate: string;
  notes: string;
}

export interface Department extends BaseEntity {
  manager: string;
  employees: string[];
  departmentUnits: string[];
  positions: string[];
}

export interface DepartmentUnit extends BaseEntity {
  departmentId: string;
  departmentName: string;
  lead: string;
}

export interface Position extends BaseEntity {
  departmentId: string;
  unitId: string;
  occupationId: string;
  grade: string;
}

export interface Occupation extends BaseEntity {
  family: string;
}

export interface Contact extends BaseEntity {
  employeeId?: string;
  email: string;
  phone: string;
  contactType: string;
}

export interface PortfolioFile extends BaseEntity {
  resumeId: string;
  fileName: string;
  fileType: string;
  sizeLabel: string;
}

export interface Resume extends BaseEntity {
  employeeId: string;
  employeeName: string;
  summary: string;
  portfolioFiles: PortfolioFile[];
}

export interface Employee extends BaseEntity {
  employeeNumber: string;
  departmentId: string;
  departmentName: string;
  unitId: string;
  unitName: string;
  positionId: string;
  positionTitle: string;
  email: string;
  phone: string;
  tasks: Task[];
  addresses: string[];
  assignments: string[];
  changeHistory: AuditEntry[];
}

export interface Period extends BaseEntity {
  startDate: string;
  endDate: string;
  financialYear: string;
}

export interface Organisation extends BaseEntity {
  registrationNumber: string;
  industryId: string;
}

export interface Industry extends BaseEntity {}
export interface BudgetSource extends BaseEntity {}
export interface BudgetType extends BaseEntity {}
export interface StrategicGoal extends BaseEntity {}
export interface StrategicObjective extends BaseEntity {}
export interface UnitOfMeasure extends BaseEntity {}
export interface Kpa extends BaseEntity {}
export interface MunicipalKpa extends BaseEntity {}
export interface DepartmentalObjective extends BaseEntity {}
export interface OutputRecord extends BaseEntity {}
export interface PerformanceObjective extends BaseEntity {}
export interface PriorityIssue extends BaseEntity {}

export interface UserApprovalSetup extends BaseEntity {
  user: string;
  userEmail: string;
  approver: string;
  approverEmail: string;
  isAdminApprover: boolean;
}

export interface Country extends BaseEntity {}

export interface Province extends BaseEntity {
  countryId: string;
  countryName: string;
}

export interface City extends BaseEntity {
  provinceId: string;
  provinceName: string;
}

export interface Suburb extends BaseEntity {
  cityId: string;
  cityName: string;
}

export interface Address extends BaseEntity {
  countryId: string;
  provinceId: string;
  cityId: string;
  suburbId: string;
  addressLine: string;
  postalCode: string;
}

export interface ReportDefinition extends BaseEntity {
  routeKey: string;
  metric: string;
}

export interface Reports {
  definitions: ReportDefinition[];
}

export type EntityRecord =
  | OpmsTarget
  | IpmsTarget
  | KpiLibrary
  | OpmsSubmission
  | IpmsSubmission
  | VoteNumber
  | Employee
  | Department
  | DepartmentUnit
  | Position
  | Occupation
  | Contact
  | Resume
  | PortfolioFile
  | Task
  | Period
  | Organisation
  | Industry
  | UserApprovalSetup
  | BudgetSource
  | BudgetType
  | StrategicGoal
  | StrategicObjective
  | UnitOfMeasure
  | Kpa
  | MunicipalKpa
  | DepartmentalObjective
  | OutputRecord
  | PerformanceObjective
  | PriorityIssue
  | Country
  | Province
  | City
  | Suburb
  | Address
  | ReportDefinition;

export type EntityKey =
  | "opmsTargets"
  | "ipmsTargets"
  | "opmsSubmissions"
  | "ipmsSubmissions"
  | "voteNumbers"
  | "kpiLibrary"
  | "employees"
  | "departments"
  | "departmentUnits"
  | "positions"
  | "occupations"
  | "contacts"
  | "resumes"
  | "portfolioFiles"
  | "tasks"
  | "periods"
  | "organisations"
  | "industries"
  | "approvalSetup"
  | "budgetSources"
  | "budgetTypes"
  | "strategicGoals"
  | "strategicObjectives"
  | "unitsOfMeasure"
  | "kpas"
  | "municipalKpas"
  | "departmentalObjectives"
  | "outputs"
  | "performanceObjectives"
  | "priorityIssues"
  | "countries"
  | "provinces"
  | "cities"
  | "suburbs"
  | "addresses"
  | "reports";

export interface EntityStore {
  opmsTargets: OpmsTarget[];
  ipmsTargets: IpmsTarget[];
  opmsSubmissions: OpmsSubmission[];
  ipmsSubmissions: IpmsSubmission[];
  voteNumbers: VoteNumber[];
  kpiLibrary: KpiLibrary[];
  employees: Employee[];
  departments: Department[];
  departmentUnits: DepartmentUnit[];
  positions: Position[];
  occupations: Occupation[];
  contacts: Contact[];
  resumes: Resume[];
  portfolioFiles: PortfolioFile[];
  tasks: Task[];
  periods: Period[];
  organisations: Organisation[];
  industries: Industry[];
  approvalSetup: UserApprovalSetup[];
  budgetSources: BudgetSource[];
  budgetTypes: BudgetType[];
  strategicGoals: StrategicGoal[];
  strategicObjectives: StrategicObjective[];
  unitsOfMeasure: UnitOfMeasure[];
  kpas: Kpa[];
  municipalKpas: MunicipalKpa[];
  departmentalObjectives: DepartmentalObjective[];
  outputs: OutputRecord[];
  performanceObjectives: PerformanceObjective[];
  priorityIssues: PriorityIssue[];
  countries: Country[];
  provinces: Province[];
  cities: City[];
  suburbs: Suburb[];
  addresses: Address[];
  reports: ReportDefinition[];
}

export interface FormFieldOption {
  label: string;
  value: string;
}

export interface FormFieldConfig {
  key: string;
  label: string;
  type?:
    | "text"
    | "textarea"
    | "number"
    | "date"
    | "select"
    | "multiselect"
    | "checkbox";
  placeholder?: string;
  options?: FormFieldOption[];
  required?: boolean;
}

export interface ActionButtonConfig<T = EntityRecord> {
  label: string;
  tone?: StatusTone;
  hidden?: (record: T, role: AppRole) => boolean;
  onClick: (record: T) => void;
}

export interface ModuleRoute {
  key: string;
  label: string;
  path: string;
  icon: string;
  category: string;
  entityKey?: EntityKey;
  emptyDescription?: string;
  filters?: string[];
  exportable?: boolean;
  hasChildCrud?: boolean;
}

export interface SidebarGroup {
  label: string;
  items: ModuleRoute[];
}

export interface ToastMessage {
  id: string;
  title: string;
  description: string;
  tone: StatusTone;
}

export interface SearchResult {
  id: string;
  label: string;
  sublabel: string;
  path: string;
  icon: string;
}

export interface RoleDefinition {
  id: AppRole;
  label: string;
  shortLabel: string;
  description: string;
  dashboardSummary: string;
  queueFocus: string;
}

export interface DashboardStat {
  id: string;
  title: string;
  value: string;
  delta: string;
  tone: StatusTone;
  icon: string;
}

export interface DashboardQueueCard {
  id: string;
  title: string;
  count: string;
  route: string;
  tone: StatusTone;
}

export interface DashboardModel {
  stats: DashboardStat[];
  queues: DashboardQueueCard[];
  highlights: Array<{ label: string; value: string }>;
}

export interface AppContextValue {
  role: AppRole;
  theme: ThemeMode;
  loggedIn: boolean;
  toasts: ToastMessage[];
  dataVersion: number;
  store: EntityStore;
  setRole: (role: AppRole) => void;
  toggleTheme: () => void;
  login: () => void;
  logout: () => void;
  createRecord: <T extends EntityRecord>(key: EntityKey, record: T) => void;
  updateRecord: <T extends EntityRecord>(key: EntityKey, id: string, record: T) => void;
  deleteRecord: (key: EntityKey, id: string) => void;
  addToast: (toast: Omit<ToastMessage, "id">) => void;
}

export interface DetailTab {
  key: string;
  label: string;
  icon?: ReactNode;
}
