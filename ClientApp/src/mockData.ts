import type {
  Address,
  AdditionalAssignee,
  AuditEntry,
  BudgetSource,
  BudgetType,
  City,
  Contact,
  Country,
  Department,
  DepartmentUnit,
  DepartmentalObjective,
  DynamicTargetUnitField,
  Employee,
  EntityStore,
  FileAsset,
  Industry,
  IpmsSubmission,
  IpmsTarget,
  IpmsUserSubmit,
  Kpa,
  KpiLibrary,
  MunicipalKpa,
  Occupation,
  OpmsSubmission,
  OpmsTarget,
  OpmsUserSubmit,
  Organisation,
  OutputRecord,
  PerformanceObjective,
  Period,
  PortfolioFile,
  Position,
  PriorityIssue,
  Province,
  QuarterlyTargetValue,
  ReportDefinition,
  Resume,
  StrategicGoal,
  StrategicObjective,
  Suburb,
  TargetUnitType,
  Task,
  UnitOfMeasure,
  UserApprovalSetup,
  VoteNumber,
} from "./types";

const now = "2026-06-14T08:30:00Z";

const entity = <T extends { id: string; name: string; status: string }>(
  seed: T,
): T & { createdAt: string; updatedAt: string } => ({
  ...seed,
  createdAt: now,
  updatedAt: now,
});

const audit = (id: string, action: string, actor: string, notes: string): AuditEntry => ({
  id,
  action,
  actor,
  timestamp: now,
  notes,
});

const file = (id: string, fileName: string, documentType: string): FileAsset => ({
  id,
  fileName,
  documentType,
  sizeLabel: "248 KB",
  uploadedBy: "Thandi Molefe",
  uploadedAt: now,
  notes: "Mock uploaded document",
});

const quarterly = (value: string, unit: string, budget: string, revisedValue?: string): QuarterlyTargetValue => ({
  value,
  description: `Target measured in ${unit.toLowerCase()}.`,
  unit,
  budget,
  revisedValue,
  revisedBudget: revisedValue ? budget : undefined,
});

const dynamicFields = (unitType: TargetUnitType): DynamicTargetUnitField[] => {
  const map: Record<TargetUnitType, DynamicTargetUnitField[]> = {
    Percentage: [
      { key: "targetPercent", label: "Target Percentage", value: "18" },
      { key: "direction", label: "Direction", value: "Lower is better" },
    ],
    "Absolute Count": [
      { key: "targetCount", label: "Target Count", value: "2800" },
      { key: "countLabel", label: "Count Label", value: "Households" },
    ],
    Financial: [
      { key: "currency", label: "Currency", value: "ZAR" },
      { key: "amount", label: "Target Amount", value: "9500000" },
    ],
    "Area Based": [
      { key: "area", label: "Area Target", value: "120" },
      { key: "areaUnit", label: "Area Unit", value: "km2" },
    ],
    "Volume Based": [
      { key: "volume", label: "Volume Target", value: "4600" },
      { key: "volumeUnit", label: "Volume Unit", value: "litres" },
    ],
    "Index Scores": [
      { key: "indexName", label: "Index Name", value: "Citizen Satisfaction Index" },
      { key: "indexTarget", label: "Score Target", value: "4.1" },
    ],
    Ratios: [
      { key: "numerator", label: "Numerator Label", value: "Resolved Cases" },
      { key: "denominator", label: "Denominator Label", value: "Logged Cases" },
    ],
    "Time Based": [
      { key: "timeValue", label: "Target Time", value: "4" },
      { key: "timeUnit", label: "Time Unit", value: "Hours" },
    ],
    Binary: [{ key: "binaryGoal", label: "Binary Goal", value: "Yes" }],
    Date: [{ key: "targetDate", label: "Target Date", value: "2026-08-31" }],
    "Readiness Scale": [
      { key: "scale", label: "Scale", value: "0-5" },
      { key: "targetLevel", label: "Target Level", value: "4" },
    ],
    Qualitative: [{ key: "qualitativeMeasure", label: "Measure", value: "Narrative improvement" }],
    "Zero Based": [{ key: "zeroBaseline", label: "Zero Baseline Rule", value: "Start at zero each period" }],
    "Reverse Cumulative": [{ key: "reverseCumulativeRule", label: "Rule", value: "Decline monthly backlog" }],
    "Reverse Non-Cumulative": [{ key: "reverseNonCumulativeRule", label: "Rule", value: "Decrease open issues" }],
    "Binary Determination": [{ key: "determination", label: "Determination Rule", value: "Pass or Fail" }],
  };

  return map[unitType];
};

const makeVote = (id: string, voteCode: string, name: string, amount: number): VoteNumber =>
  entity({
    id,
    voteCode,
    name,
    code: voteCode,
    description: "Vote allocation for planned expenditure.",
    status: "Active",
    budgetSource: "Municipal Grant",
    budgetType: "Capital",
    amount,
    owner: "Budget & Treasury",
  });

const additionalAssignees: AdditionalAssignee[] = [
  { id: "asg-1", employeeId: "emp-2", employeeName: "P. Khumalo", role: "Reviewer", department: "Electrical Engineering" },
  { id: "asg-2", employeeId: "emp-3", employeeName: "L. Botha", role: "Support", department: "Budget & Treasury" },
];

const opmsUserSubmissions: OpmsUserSubmit[] = [
  entity({
    id: "opms-user-1",
    name: "Q3 submission",
    status: "Submitted",
    quarter: "Q3",
    submitterName: "S. Naidoo",
    actual: "17.2%",
    score: 4,
    dueDate: "2026-03-31",
  }),
  entity({
    id: "opms-user-2",
    name: "Q2 submission",
    status: "Approved",
    quarter: "Q2",
    submitterName: "S. Naidoo",
    actual: "18.4%",
    score: 3,
    dueDate: "2025-12-31",
  }),
];

const ipmsUserSubmissions: IpmsUserSubmit[] = [
  entity({
    id: "ipms-user-1",
    name: "Q3 pipe burst response",
    status: "Verified",
    quarter: "Q3",
    employeeName: "S. Naidoo",
    actual: "3.6 hrs",
    score: 4,
    dueDate: "2026-03-31",
  }),
  entity({
    id: "ipms-user-2",
    name: "Q2 pipe burst response",
    status: "Approved",
    quarter: "Q2",
    employeeName: "S. Naidoo",
    actual: "3.9 hrs",
    score: 4,
    dueDate: "2025-12-31",
  }),
];

export const tasks: Task[] = [
  entity({
    id: "task-1",
    name: "Prepare Q3 OPMS consolidation pack",
    code: "TSK-1001",
    description: "Compile departmental performance evidence and sign-off pack.",
    status: "In progress",
    priority: "High",
    assignedEmployeeIds: ["emp-1", "emp-3"],
    assignedEmployees: ["Thandi Molefe", "L. Botha"],
    estimatedWorkHours: 12,
    actualWorkHours: 8,
    dueDate: "2026-06-18",
    notes: "Awaiting final finance schedule.",
  }),
  entity({
    id: "task-2",
    name: "Review overdue IPMS submissions",
    code: "TSK-1002",
    description: "Check escalation list and return missing evidence.",
    status: "Open",
    priority: "Medium",
    assignedEmployeeIds: ["emp-4"],
    assignedEmployees: ["T. Dlamini"],
    estimatedWorkHours: 6,
    actualWorkHours: 1,
    dueDate: "2026-06-20",
    notes: "Verifier handoff pending.",
  }),
];

export const departments: Department[] = [
  entity({
    id: "dept-1",
    name: "Water & Sanitation",
    code: "WAT",
    description: "Municipal water distribution and sanitation services.",
    status: "Active",
    manager: "S. Naidoo",
    employees: ["emp-1"],
    departmentUnits: ["unit-1"],
    positions: ["pos-1"],
  }),
  entity({
    id: "dept-2",
    name: "Budget & Treasury",
    code: "BNT",
    description: "Budget management and financial performance.",
    status: "Active",
    manager: "L. Botha",
    employees: ["emp-3"],
    departmentUnits: ["unit-2"],
    positions: ["pos-2"],
  }),
];

export const departmentUnits: DepartmentUnit[] = [
  entity({
    id: "unit-1",
    name: "Distribution Operations",
    code: "WAT-DO",
    description: "Response and maintenance unit.",
    status: "Active",
    departmentId: "dept-1",
    departmentName: "Water & Sanitation",
    lead: "S. Naidoo",
  }),
  entity({
    id: "unit-2",
    name: "Financial Reporting",
    code: "BNT-FR",
    description: "Financial reporting and reconciliation unit.",
    status: "Active",
    departmentId: "dept-2",
    departmentName: "Budget & Treasury",
    lead: "L. Botha",
  }),
];

export const occupations: Occupation[] = [
  entity({ id: "occ-1", name: "Engineer", code: "ENG", description: "Engineering occupation", status: "Active", family: "Technical" }),
  entity({ id: "occ-2", name: "Accountant", code: "ACC", description: "Financial occupation", status: "Active", family: "Finance" }),
];

export const positions: Position[] = [
  entity({
    id: "pos-1",
    name: "Senior Water Engineer",
    code: "POS-001",
    description: "Leads water response performance execution.",
    status: "Active",
    departmentId: "dept-1",
    unitId: "unit-1",
    occupationId: "occ-1",
    grade: "P7",
  }),
  entity({
    id: "pos-2",
    name: "Finance Performance Analyst",
    code: "POS-002",
    description: "Monitors financial KPI delivery.",
    status: "Active",
    departmentId: "dept-2",
    unitId: "unit-2",
    occupationId: "occ-2",
    grade: "P6",
  }),
];

export const employees: Employee[] = [
  entity({
    id: "emp-1",
    name: "S. Naidoo",
    code: "EMP-001",
    description: "Water services lead.",
    status: "Active",
    employeeNumber: "100234",
    departmentId: "dept-1",
    departmentName: "Water & Sanitation",
    unitId: "unit-1",
    unitName: "Distribution Operations",
    positionId: "pos-1",
    positionTitle: "Senior Water Engineer",
    email: "s.naidoo@meridian.gov.za",
    phone: "+27 31 300 1001",
    tasks: [tasks[0]],
    addresses: ["addr-1"],
    assignments: ["OPMS-0412", "IPMS-1043"],
    changeHistory: [audit("emp-h1", "Profile updated", "HR Admin", "Changed reporting line.")],
  }),
  entity({
    id: "emp-3",
    name: "L. Botha",
    code: "EMP-003",
    description: "Finance performance analyst.",
    status: "Active",
    employeeNumber: "100435",
    departmentId: "dept-2",
    departmentName: "Budget & Treasury",
    unitId: "unit-2",
    unitName: "Financial Reporting",
    positionId: "pos-2",
    positionTitle: "Finance Performance Analyst",
    email: "l.botha@meridian.gov.za",
    phone: "+27 31 300 1003",
    tasks: [tasks[0], tasks[1]],
    addresses: ["addr-2"],
    assignments: ["OPMS-0205"],
    changeHistory: [audit("emp-h2", "Position changed", "HR Admin", "Promoted to analyst role.")],
  }),
];

export const contacts: Contact[] = [
  entity({
    id: "contact-1",
    name: "S. Naidoo Work",
    code: "CON-001",
    description: "Primary work contact.",
    status: "Active",
    employeeId: "emp-1",
    email: "s.naidoo@meridian.gov.za",
    phone: "+27 31 300 1001",
    contactType: "Internal",
  }),
  entity({
    id: "contact-2",
    name: "L. Botha Work",
    code: "CON-002",
    description: "Primary work contact.",
    status: "Active",
    employeeId: "emp-3",
    email: "l.botha@meridian.gov.za",
    phone: "+27 31 300 1003",
    contactType: "Internal",
  }),
];

export const portfolioFiles: PortfolioFile[] = [
  entity({
    id: "pf-1",
    name: "Engineering Registration",
    code: "PF-001",
    description: "Professional portfolio evidence.",
    status: "Uploaded",
    resumeId: "resume-1",
    fileName: "engineering-registration.pdf",
    fileType: "Portfolio",
    sizeLabel: "120 KB",
  }),
  entity({
    id: "pf-2",
    name: "Financial Management Certificate",
    code: "PF-002",
    description: "Professional portfolio evidence.",
    status: "Uploaded",
    resumeId: "resume-2",
    fileName: "financial-management-certificate.pdf",
    fileType: "Portfolio",
    sizeLabel: "144 KB",
  }),
];

export const resumes: Resume[] = [
  entity({
    id: "resume-1",
    name: "S. Naidoo Resume",
    code: "RES-001",
    description: "Current CV and supporting files.",
    status: "Active",
    employeeId: "emp-1",
    employeeName: "S. Naidoo",
    summary: "Water infrastructure specialist with municipal operations experience.",
    portfolioFiles: [portfolioFiles[0]],
  }),
  entity({
    id: "resume-2",
    name: "L. Botha Resume",
    code: "RES-002",
    description: "Current CV and supporting files.",
    status: "Active",
    employeeId: "emp-3",
    employeeName: "L. Botha",
    summary: "Finance analyst focused on public sector performance management.",
    portfolioFiles: [portfolioFiles[1]],
  }),
];

export const periods: Period[] = [
  entity({
    id: "period-1",
    name: "FY 2025/26",
    code: "2025-26",
    description: "Current financial year",
    status: "Open",
    startDate: "2025-07-01",
    endDate: "2026-06-30",
    financialYear: "2025/26",
  }),
  entity({
    id: "period-2",
    name: "FY 2026/27",
    code: "2026-27",
    description: "Next financial year",
    status: "Planned",
    startDate: "2026-07-01",
    endDate: "2027-06-30",
    financialYear: "2026/27",
  }),
];

export const industries: Industry[] = [
  entity({ id: "ind-1", name: "Municipal Government", code: "IND-001", description: "Local government industry", status: "Active" }),
  entity({ id: "ind-2", name: "Public Infrastructure", code: "IND-002", description: "Infrastructure services", status: "Active" }),
];

export const organisations: Organisation[] = [
  entity({
    id: "org-1",
    name: "FTCERP",
    code: "MER-MUN",
    description: "Primary organisation record.",
    status: "Active",
    registrationNumber: "REG-2026-001",
    industryId: "ind-1",
  }),
];

export const approvalSetup: UserApprovalSetup[] = [
  entity({
    id: "appr-1",
    name: "S. Naidoo Approval Setup",
    code: "APP-001",
    description: "Operational approval chain.",
    status: "Active",
    user: "S. Naidoo",
    userEmail: "s.naidoo@meridian.gov.za",
    approver: "Thandi Molefe",
    approverEmail: "thandi.molefe@meridian.gov.za",
    isAdminApprover: false,
  }),
  entity({
    id: "appr-2",
    name: "L. Botha Approval Setup",
    code: "APP-002",
    description: "Finance approval chain.",
    status: "Active",
    user: "L. Botha",
    userEmail: "l.botha@meridian.gov.za",
    approver: "CFO Office",
    approverEmail: "cfo.office@meridian.gov.za",
    isAdminApprover: true,
  }),
];

const generic = <T extends { id: string; name: string; code: string; description: string; status: string }>(
  items: T[],
): T[] => items.map((item) => entity(item));

export const budgetSources: BudgetSource[] = generic([
  { id: "bs-1", name: "Municipal Grant", code: "BS-001", description: "Equitable share and grant funding", status: "Active" },
  { id: "bs-2", name: "Own Revenue", code: "BS-002", description: "Own revenue collections", status: "Active" },
]);

export const budgetTypes: BudgetType[] = generic([
  { id: "bt-1", name: "Capital", code: "BT-001", description: "Capital expenditure", status: "Active" },
  { id: "bt-2", name: "Operational", code: "BT-002", description: "Operating expenditure", status: "Active" },
]);

export const strategicGoals: StrategicGoal[] = generic([
  { id: "sg-1", name: "Sustainable Infrastructure", code: "SG-001", description: "Deliver resilient services", status: "Active" },
  { id: "sg-2", name: "Good Governance", code: "SG-002", description: "Strengthen accountability", status: "Active" },
]);

export const strategicObjectives: StrategicObjective[] = generic([
  { id: "so-1", name: "Reduce Water Losses", code: "SO-001", description: "Improve water network efficiency", status: "Active" },
  { id: "so-2", name: "Improve Budget Accuracy", code: "SO-002", description: "Increase budget performance", status: "Active" },
]);

export const unitsOfMeasure: UnitOfMeasure[] = generic([
  { id: "uom-1", name: "Percent", code: "%", description: "Percentage measure", status: "Active" },
  { id: "uom-2", name: "Hours", code: "HRS", description: "Time measure", status: "Active" },
]);

export const kpas: Kpa[] = generic([
  { id: "kpa-1", name: "Basic Service Delivery", code: "KPA-001", description: "Core service delivery", status: "Active" },
  { id: "kpa-2", name: "Municipal Financial Viability", code: "KPA-002", description: "Financial sustainability", status: "Active" },
]);

export const municipalKpas: MunicipalKpa[] = generic([
  { id: "mkpa-1", name: "Water Services", code: "MKPA-001", description: "Municipal water service priorities", status: "Active" },
  { id: "mkpa-2", name: "Financial Governance", code: "MKPA-002", description: "Municipal finance priorities", status: "Active" },
]);

export const departmentalObjectives: DepartmentalObjective[] = generic([
  { id: "do-1", name: "Reduce Response Delays", code: "DO-001", description: "Improve service responsiveness", status: "Active" },
  { id: "do-2", name: "Tighten Budget Monitoring", code: "DO-002", description: "Improve forecast quality", status: "Active" },
]);

export const outputs: OutputRecord[] = generic([
  { id: "out-1", name: "Reduced leaks", code: "OUT-001", description: "Leak reduction output", status: "Active" },
  { id: "out-2", name: "Accurate reconciliations", code: "OUT-002", description: "Finance output", status: "Active" },
]);

export const performanceObjectives: PerformanceObjective[] = generic([
  { id: "po-1", name: "Reliable water distribution", code: "PO-001", description: "Water reliability objective", status: "Active" },
  { id: "po-2", name: "Responsible fiscal management", code: "PO-002", description: "Finance objective", status: "Active" },
]);

export const priorityIssues: PriorityIssue[] = generic([
  { id: "pi-1", name: "Water leakage backlog", code: "PI-001", description: "Network leakage issue", status: "Active" },
  { id: "pi-2", name: "Late vote capture", code: "PI-002", description: "Budget coding issue", status: "Active" },
]);

export const countries: Country[] = [
  entity({ id: "country-1", name: "South Africa", code: "ZA", description: "Country record", status: "Active" }),
];

export const provinces: Province[] = [
  entity({
    id: "province-1",
    name: "KwaZulu-Natal",
    code: "KZN",
    description: "Province record",
    status: "Active",
    countryId: "country-1",
    countryName: "South Africa",
  }),
];

export const cities: City[] = [
  entity({
    id: "city-1",
    name: "Durban",
    code: "DBN",
    description: "City record",
    status: "Active",
    provinceId: "province-1",
    provinceName: "KwaZulu-Natal",
  }),
];

export const suburbs: Suburb[] = [
  entity({
    id: "suburb-1",
    name: "Umhlanga",
    code: "UMH",
    description: "Suburb record",
    status: "Active",
    cityId: "city-1",
    cityName: "Durban",
  }),
  entity({
    id: "suburb-2",
    name: "Westville",
    code: "WVL",
    description: "Suburb record",
    status: "Active",
    cityId: "city-1",
    cityName: "Durban",
  }),
];

export const addresses: Address[] = [
  entity({
    id: "addr-1",
    name: "7 Reservoir Road",
    code: "ADDR-001",
    description: "Primary address",
    status: "Active",
    countryId: "country-1",
    provinceId: "province-1",
    cityId: "city-1",
    suburbId: "suburb-1",
    addressLine: "7 Reservoir Road",
    postalCode: "4319",
  }),
  entity({
    id: "addr-2",
    name: "14 Ledger Avenue",
    code: "ADDR-002",
    description: "Primary address",
    status: "Active",
    countryId: "country-1",
    provinceId: "province-1",
    cityId: "city-1",
    suburbId: "suburb-2",
    addressLine: "14 Ledger Avenue",
    postalCode: "3629",
  }),
];

export const voteNumbers: VoteNumber[] = [
  makeVote("vote-1", "V-101-001", "Water Loss Reduction Programme", 9500000),
  makeVote("vote-2", "V-202-110", "Finance Reconciliation Improvement", 2600000),
];

export const kpiLibrary: KpiLibrary[] = [
  entity({
    id: "kpi-1",
    name: "Reduce water distribution losses",
    code: "KPI-OPMS-0412",
    description: "Water loss reduction KPI template.",
    status: "Active",
    templateCode: "TPL-001",
    category: "OPMS",
    kpa: "Basic Service Delivery",
    targetUnitType: "Percentage",
    defaultAnnualTarget: "18%",
    department: "Water & Sanitation",
  }),
  entity({
    id: "kpi-2",
    name: "Pipe burst response time",
    code: "KPI-IPMS-1043",
    description: "Response time KPI template.",
    status: "Active",
    templateCode: "TPL-002",
    category: "IPMS",
    kpa: "Basic Service Delivery",
    targetUnitType: "Time Based",
    defaultAnnualTarget: "4 hrs",
    department: "Water & Sanitation",
  }),
];

export const opmsTargets: OpmsTarget[] = [
  entity({
    id: "opms-1",
    name: "Reduce water distribution losses",
    code: "OPMS-0412",
    description: "Organisation-wide target for reducing distribution losses.",
    status: "On track",
    period: "FY 2025/26",
    department: "Water & Sanitation",
    unit: "Distribution Operations",
    wards: "Wards 1-12",
    assignedTo: "S. Naidoo",
    indicatorNumber: "KPI-OPMS-0412",
    nationalKpa: "Basic Service Delivery",
    municipalKpa: "Water Services",
    strategicGoal: "Sustainable Infrastructure",
    strategicObjective: "Reduce Water Losses",
    performanceObjective: "Reliable water distribution",
    targetName: "Reduce water distribution losses",
    kpiDescription: "Percentage of non-revenue water losses reduced.",
    baseline: "24%",
    annualTargetValue: "18%",
    annualTargetDescription: "Achieve 18% or less distribution losses by year end.",
    budgetSource: "Municipal Grant",
    budgetType: "Capital",
    unitOfMeasure: "Percent",
    weight: 5,
    kpiType: "Outcome",
    indicatorType: "Lagging",
    functionalArea: "Operations",
    standardClassification: "Water Services",
    idpReference: "IDP-WS-12",
    internalReference: "INT-OPMS-0412",
    fmsLink: "https://mock.local/fms/opms-0412",
    isRevised: true,
    isWithdrawn: false,
    reasonForWithdrawal: "",
    targetUnitType: "Percentage",
    quarterlyTargets: {
      q1: quarterly("22%", "Percent", "R2.1m"),
      q2: quarterly("20%", "Percent", "R2.3m"),
      midTerm: quarterly("19.5%", "Percent", "R4.4m"),
      q3: quarterly("18.7%", "Percent", "R2.4m", "18.4%"),
      q4: quarterly("18%", "Percent", "R2.7m", "17.8%"),
      annual: quarterly("18%", "Percent", "R9.5m", "17.8%"),
    },
    dynamicTargetUnitFields: dynamicFields("Percentage"),
    userSubmissions: opmsUserSubmissions,
    voteNumbers: [voteNumbers[0]],
    relatedIpmsTargets: [
      {
        id: "ipms-1",
        indicatorNumber: "KPI-IPMS-1043",
        targetName: "Pipe burst response time",
        assignedTo: "S. Naidoo",
        weight: 10,
        status: "On track",
      },
    ],
    additionalAssignees,
    attachments: [file("file-opms-1", "water-loss-model.xlsx", "Planning"), file("file-opms-2", "engineering-evidence.pdf", "Evidence")],
    auditHistory: [audit("aud-1", "Created", "Performance Manager", "Target created from KPI library.")],
  }),
  entity({
    id: "opms-2",
    name: "Capital budget actual expenditure",
    code: "OPMS-0205",
    description: "Capital budget expenditure target.",
    status: "At risk",
    period: "FY 2025/26",
    department: "Budget & Treasury",
    unit: "Financial Reporting",
    wards: "All",
    assignedTo: "L. Botha",
    indicatorNumber: "KPI-OPMS-0205",
    nationalKpa: "Municipal Financial Viability",
    municipalKpa: "Financial Governance",
    strategicGoal: "Good Governance",
    strategicObjective: "Improve Budget Accuracy",
    performanceObjective: "Responsible fiscal management",
    targetName: "Capital budget actual expenditure",
    kpiDescription: "Actual capital expenditure as a percentage of budget.",
    baseline: "88%",
    annualTargetValue: "95%",
    annualTargetDescription: "Reach 95% expenditure by year end.",
    budgetSource: "Own Revenue",
    budgetType: "Operational",
    unitOfMeasure: "Percent",
    weight: 6,
    kpiType: "Output",
    indicatorType: "Leading",
    functionalArea: "Finance",
    standardClassification: "Budget",
    idpReference: "IDP-FI-02",
    internalReference: "INT-OPMS-0205",
    fmsLink: "https://mock.local/fms/opms-0205",
    isRevised: false,
    isWithdrawn: false,
    reasonForWithdrawal: "",
    targetUnitType: "Financial",
    quarterlyTargets: {
      q1: quarterly("22%", "Percent", "R600k"),
      q2: quarterly("48%", "Percent", "R700k"),
      midTerm: quarterly("48%", "Percent", "R1.3m"),
      q3: quarterly("71%", "Percent", "R650k"),
      q4: quarterly("95%", "Percent", "R650k"),
      annual: quarterly("95%", "Percent", "R2.6m"),
    },
    dynamicTargetUnitFields: dynamicFields("Financial"),
    userSubmissions: [],
    voteNumbers: [voteNumbers[1]],
    relatedIpmsTargets: [],
    additionalAssignees: [],
    attachments: [file("file-opms-3", "budget-tracker.xlsx", "Budget")],
    auditHistory: [audit("aud-2", "Updated", "Department Manager", "Adjusted Q3 spend assumptions.")],
  }),
];

export const ipmsTargets: IpmsTarget[] = [
  entity({
    id: "ipms-1",
    name: "Pipe burst response time",
    code: "IPMS-1043",
    description: "Individual target aligned to water loss reduction.",
    status: "On track",
    period: "FY 2025/26",
    department: "Water & Sanitation",
    unit: "Distribution Operations",
    wards: "Wards 1-12",
    assignedTo: "S. Naidoo",
    indicatorNumber: "KPI-IPMS-1043",
    nationalKpa: "Basic Service Delivery",
    municipalKpa: "Water Services",
    strategicGoal: "Sustainable Infrastructure",
    strategicObjective: "Reduce Water Losses",
    performanceObjective: "Reliable water distribution",
    targetName: "Pipe burst response time",
    kpiDescription: "Respond to pipe bursts within the agreed SLA.",
    baseline: "6 hrs",
    annualTargetValue: "4 hrs",
    annualTargetDescription: "Maintain an average response time of 4 hours or less.",
    budgetSource: "Municipal Grant",
    budgetType: "Operational",
    unitOfMeasure: "Hours",
    weight: 10,
    kpiType: "Output",
    indicatorType: "Leading",
    functionalArea: "Operations",
    standardClassification: "Water Repairs",
    idpReference: "IDP-WS-14",
    internalReference: "INT-IPMS-1043",
    fmsLink: "https://mock.local/fms/ipms-1043",
    isRevised: false,
    isWithdrawn: false,
    reasonForWithdrawal: "",
    targetUnitType: "Time Based",
    quarterlyTargets: {
      q1: quarterly("4.8", "Hours", "R180k"),
      q2: quarterly("4.2", "Hours", "R190k"),
      midTerm: quarterly("4.2", "Hours", "R370k"),
      q3: quarterly("4.0", "Hours", "R200k", "3.8"),
      q4: quarterly("3.8", "Hours", "R210k", "3.6"),
      annual: quarterly("4.0", "Hours", "R780k", "3.8"),
    },
    dynamicTargetUnitFields: dynamicFields("Time Based"),
    relatedOpmsTargetId: "opms-1",
    relatedOpmsTargetLabel: "OPMS-0412 · Reduce water distribution losses",
    userSubmissions: ipmsUserSubmissions,
    additionalAssignees,
    attachments: [file("file-ipms-1", "burst-response-log.xlsx", "Evidence")],
    auditHistory: [audit("aud-ipms-1", "Linked", "Performance Manager", "Linked to OPMS-0412.")],
  }),
];

export const opmsSubmissions: OpmsSubmission[] = [
  entity({
    id: "opms-sub-1",
    name: "OPMS Q3 Water Loss Submission",
    code: "SUB-OPMS-001",
    description: "Quarterly OPMS submission for water loss target.",
    status: "Verification",
    submissionType: "OPMS",
    targetId: "opms-1",
    indicatorNumber: "KPI-OPMS-0412",
    quarter: "Q3",
    dueDate: "2026-03-31",
    extendedDueDate: "2026-04-07",
    actual: "17.2%",
    actualPerformanceDescription: "Metered losses declined after intervention programme.",
    actualExpenditure: "R2.24m",
    variance: "-1.2%",
    varianceReason: "Replacement of high-loss sections completed early.",
    correctiveMeasure: "Continue district metering and leak repair campaigns.",
    submitterScore: "4",
    submitterStatus: "Submitted",
    auditorStatus: "Pending",
    auditorComment: "",
    auditorRecommendation: "",
    auditorScore: "",
    verifierStatus: "Pending",
    verifierComment: "",
    verifierScore: "",
    approverComment: "",
    approverStatus: "Pending",
    approverScore: "",
    pmsStatus: "In review",
    pmsComment: "Validate evidence against quarterly baseline.",
    pmsRecommendation: "Proceed to verification once finance confirms spend.",
    pmsScore: "4",
    pmsRfiComment: "",
    proofOfEvidenceUploads: [file("sub-file-1", "q3-water-evidence.pdf", "Evidence"), file("sub-file-2", "q3-spend.xlsx", "Financial")],
    verificationInformation: [audit("ver-1", "Queued", "Verifier", "Awaiting verification action.")],
    approvalHistory: [audit("app-1", "Created", "Submitter", "Submission started.")],
    pmsHistory: [audit("pms-1", "Reviewed", "Performance Manager", "Pre-check completed.")],
    auditorInformation: [],
    commentsAndHistory: [audit("c-1", "Comment", "Submitter", "Evidence pack attached.")],
  }),
];

export const ipmsSubmissions: IpmsSubmission[] = [
  entity({
    id: "ipms-sub-1",
    name: "IPMS Q3 Pipe Burst Response",
    code: "SUB-IPMS-001",
    description: "Quarterly IPMS submission for response time target.",
    status: "Approval",
    submissionType: "IPMS",
    targetId: "ipms-1",
    indicatorNumber: "KPI-IPMS-1043",
    quarter: "Q3",
    dueDate: "2026-03-31",
    extendedDueDate: "",
    actual: "3.6 hrs",
    actualPerformanceDescription: "Average response improved with standby roster.",
    actualExpenditure: "R182k",
    variance: "-0.4 hrs",
    varianceReason: "Improved call routing and standby coverage.",
    correctiveMeasure: "Maintain standby roster and field dispatch SLAs.",
    submitterScore: "4",
    submitterStatus: "Submitted",
    auditorStatus: "Recorded",
    auditorComment: "Evidence set complete.",
    auditorRecommendation: "Proceed to approval.",
    auditorScore: "4",
    verifierStatus: "Verified",
    verifierComment: "Sample checks completed.",
    verifierScore: "4",
    approverComment: "Good quarter performance.",
    approverStatus: "Pending",
    approverScore: "",
    pmsStatus: "Cleared",
    pmsComment: "Ready for approval.",
    pmsRecommendation: "Approve",
    pmsScore: "4",
    pmsRfiComment: "",
    proofOfEvidenceUploads: [file("sub-file-3", "dispatch-log.pdf", "Evidence")],
    verificationInformation: [audit("ver-2", "Verified", "Verifier", "Verified against dispatch logs.")],
    approvalHistory: [audit("app-2", "Awaiting approval", "Approver", "Awaiting final decision.")],
    pmsHistory: [audit("pms-2", "Cleared", "PMS Officer", "PMS review completed.")],
    auditorInformation: [audit("aud-sub-1", "Finding recorded", "Auditor", "No exceptions found.")],
    commentsAndHistory: [audit("c-2", "Comment", "Verifier", "Evidence satisfactory.")],
  }),
];

export const reports: ReportDefinition[] = [
  entity({ id: "rep-1", name: "KPI Performance Summary", code: "RPT-001", description: "Summary of KPI performance", status: "Available", routeKey: "kpi-performance-summary", metric: "KPI completion" }),
  entity({ id: "rep-2", name: "Department Performance", code: "RPT-002", description: "Department KPI performance", status: "Available", routeKey: "department-performance", metric: "Department status" }),
  entity({ id: "rep-3", name: "Quarterly Submission Status", code: "RPT-003", description: "Submission workflow status", status: "Available", routeKey: "quarterly-submission-status", metric: "Submission counts" }),
  entity({ id: "rep-4", name: "Overdue Submissions", code: "RPT-004", description: "Overdue workflow items", status: "Available", routeKey: "overdue-submissions", metric: "Overdue count" }),
  entity({ id: "rep-5", name: "Approval Turnaround", code: "RPT-005", description: "Approval turnaround times", status: "Available", routeKey: "approval-turnaround", metric: "Turnaround days" }),
  entity({ id: "rep-6", name: "Variance Report", code: "RPT-006", description: "Variance analysis", status: "Available", routeKey: "variance-report", metric: "Variance reasons" }),
  entity({ id: "rep-7", name: "Audit Findings", code: "RPT-007", description: "Audit finding summary", status: "Available", routeKey: "audit-findings", metric: "Findings" }),
  entity({ id: "rep-8", name: "Annual Performance Report", code: "RPT-008", description: "Annual performance report", status: "Available", routeKey: "annual-performance-report", metric: "Annual summary" }),
  entity({ id: "rep-9", name: "OPMS/IPMS Alignment Report", code: "RPT-009", description: "Target alignment report", status: "Available", routeKey: "opms-ipms-alignment-report", metric: "Alignment" }),
];

export const createInitialStore = (): EntityStore => ({
  opmsTargets,
  ipmsTargets,
  opmsSubmissions,
  ipmsSubmissions,
  voteNumbers,
  kpiLibrary,
  employees,
  departments,
  departmentUnits,
  positions,
  occupations,
  contacts,
  resumes,
  portfolioFiles,
  tasks,
  periods,
  organisations,
  industries,
  approvalSetup,
  budgetSources,
  budgetTypes,
  strategicGoals,
  strategicObjectives,
  unitsOfMeasure,
  kpas,
  municipalKpas,
  departmentalObjectives,
  outputs,
  performanceObjectives,
  priorityIssues,
  countries,
  provinces,
  cities,
  suburbs,
  addresses,
  reports,
});
