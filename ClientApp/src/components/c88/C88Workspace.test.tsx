import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { C88Workspace } from './C88Workspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const capabilities = vi.hoisted(() => ({
  canRead: vi.fn(() => true), canCreate: vi.fn(() => true), canUpdate: vi.fn(() => true),
  canDelete: vi.fn(() => false), canExport: vi.fn(() => false), canImport: vi.fn(() => false),
  canExecute: vi.fn(() => true), canReadField: vi.fn(() => true), canEditField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  configureC88: vi.fn(), createC88Assignment: vi.fn(), createC88Calendar: vi.fn(), createC88CatalogueItem: vi.fn(),
  createC88CatalogueVersion: vi.fn(), createC88ComplianceQuestion: vi.fn(), createC88Indicator: vi.fn(),
  createC88Mapping: vi.fn(), createC88ReportVersion: vi.fn(), createC88Workflow: vi.fn(), finalSubmitC88Report: vi.fn(),
  getC88ReportsPage: vi.fn(), getC88Workspace: vi.fn(), getMunicipalEmployeesPage: vi.fn(), getMunicipalityFinancialYearMasters: vi.fn(), getOpmsTargetOptions: vi.fn(), getOpmsTarget: vi.fn(),
  returnC88Report: vi.fn(), saveC88IndicatorPlan: vi.fn(), submitC88Report: vi.fn(), updateC88CatalogueVersion: vi.fn(), verifyC88Report: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => capabilities }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => api);

const workspace = {
  configurations: [{ publicId: 'config-1', municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', catalogueVersionPublicId: 'version-1', catalogueVersionCode: '2026.1', isEnabled: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }],
  catalogueVersions: [{ publicId: 'version-1', code: '2026.1', name: 'Treasury edition', editionDate: '2026-01-01', effectiveFrom: '2026-07-01', isPublished: true, isActive: true, rowVersion: 'AQ==' }],
  catalogueItems: [{ publicId: 'report-type-1', catalogueVersionPublicId: 'version-1', kind: 'ReportType', code: 'Q', name: 'Quarterly', displayOrder: 1, isActive: true, rowVersion: 'AQ==' }],
  indicators: [{ publicId: 'indicator-1', catalogueVersionPublicId: 'version-1', code: 'C88-1', name: 'Official indicator', definition: 'Definition', officialTechnicalIndicatorDescription: 'TID', valueType: 'Decimal', calculationOperator: 'None', requiresBaseline: true, requiresMediumTermTarget: true, requiresAnnualTarget: true, isActive: true, rowVersion: 'AQ==', dataElements: [{ publicId: 'element-1', code: 'VALUE', name: 'Value', valueType: 'Decimal', isRequired: true, sequence: 1, rowVersion: 'AQ==' }], applicability: [] }],
  complianceQuestions: [], plans: [],
  calendars: [{ publicId: 'calendar-1', configurationPublicId: 'config-1', reportTypePublicId: 'report-type-1', code: 'Q1', name: 'Quarter 1', opensAt: '2026-07-01', closesAt: '2027-06-30', dueAt: '2027-07-01', isActive: true, rowVersion: 'AQ==' }],
  reports: [{ publicId: 'report-1', reportFamilyId: 'family-1', versionNumber: 1, isCurrent: true, configurationPublicId: 'config-1', calendarPublicId: 'calendar-1', indicatorPublicId: 'indicator-1', indicatorCode: 'C88-1', state: 'Draft', currentStageSequence: 1, calculatedValue: '42', createdAt: '2026-08-01', rowVersion: 'Ag==', dataElementValues: [], complianceResponses: [], workflowActions: [] }],
  assignments: [], workflows: [], mappings: [],
};
const year = { publicId: 'year-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01', rowVersion: 'AQ==' };

describe('Circular 88 workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capabilities.canRead.mockReturnValue(true); capabilities.canCreate.mockReturnValue(true); capabilities.canUpdate.mockReturnValue(true); capabilities.canExecute.mockReturnValue(true);
    api.getC88Workspace.mockResolvedValue({ success: true, data: workspace });
    api.getC88ReportsPage.mockResolvedValue({ success: true, data: { items: workspace.reports, page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMasters.mockResolvedValue({ success: true, data: [year] });
    api.getMunicipalEmployeesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOpmsTargetOptions.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.submitC88Report.mockResolvedValue({ success: true, data: 'report-1' });
  });

  it('keeps operational reporting visible while hiding catalogue administration', async () => {
    capabilities.canCreate.mockReturnValue(false); capabilities.canUpdate.mockReturnValue(false); capabilities.canExecute.mockReturnValue(false);
    render(<C88Workspace />);
    expect(await screen.findByText(/C88-1 · v1/)).toBeInTheDocument();
    expect(api.getMunicipalEmployeesPage).not.toHaveBeenCalled();
    expect(screen.queryByText('Versioned Treasury catalogue')).not.toBeInTheDocument();
    expect(screen.getByText('Calculated value: 42 · Stage 1')).toBeInTheDocument();
  });

  it('submits the independent report action with its RowVersion and reason', async () => {
    render(<C88Workspace />);
    fireEvent.click(await screen.findByRole('button', { name: /C88-1 · v1/ }));
    fireEvent.change(screen.getByPlaceholderText('Workflow reason'), { target: { value: 'Ready for C88 verification' } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(api.submitC88Report).toHaveBeenCalledWith('report-1', 'Ag==', 'Ready for C88 verification'));
  });
});
