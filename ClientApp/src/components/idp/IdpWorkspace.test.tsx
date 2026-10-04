import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IdpPlanManagementPage, IdpPlanningDashboardPage } from './IdpWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }));
const security = vi.hoisted(() => ({ canImport: vi.fn(() => true) }));
const api = vi.hoisted(() => ({
  createIdpPlan: vi.fn(),
  createIdpPlanVersion: vi.fn(),
  getIdpPlansPage: vi.fn(),
  getIdpPlanHierarchy: vi.fn(),
  getIdpDashboard: vi.fn(),
  getIdpImportBatches: vi.fn(),
  stageIdpKpiImport: vi.fn(),
  stageIdpHierarchyImport: vi.fn(),
  commitIdpImport: vi.fn(),
  commitIdpHierarchyImport: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../security/AccessControl', () => ({ useHasAnyPermission: () => true }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => ({
  ...api,
  createIdpComment: vi.fn(),
  getIdpAlignmentMatrix: vi.fn(),
  getIdpReport: vi.fn(),
  createIdpCommunitySession: vi.fn(),
}));

const predecessor = {
  id: 7,
  publicId: '1c80989a-060c-4b22-94f0-379d54aee8a6',
  municipalityName: 'Blue Hills',
  planTitle: 'Current IDP',
  planCode: 'IDP-2026',
  startFinancialYear: 2026,
  endFinancialYear: 2031,
  status: 'Published',
  currentVersionNumber: 1,
  createdAt: '2026-07-01T00:00:00Z',
  approvedAt: '2026-07-01T00:00:00Z',
  rowVersion: 'AQID',
  planFamilyId: '7f761cbb-c853-48e5-a838-68009b05944d',
  predecessorPlanPublicId: null,
  effectiveFrom: '2026-07-01T00:00:00Z',
  effectiveTo: null,
  publishedAt: '2026-07-01T00:00:00Z',
  publicationReference: 'Council resolution 2026/17',
};

describe('IDP plan lineage workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canImport.mockReturnValue(true);
    api.getIdpPlansPage.mockResolvedValue({
      success: true,
      data: { items: [predecessor], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 },
    });
    api.getIdpPlanHierarchy.mockResolvedValue({ success: true, data: { versions: [] } });
    api.getIdpDashboard.mockResolvedValue({ success: true, data: null });
    api.getIdpImportBatches.mockResolvedValue({ success: true, data: [] });
    api.createIdpPlan.mockResolvedValue({ success: true, data: predecessor });
    api.createIdpPlanVersion.mockResolvedValue({ success: true, data: {} });
  });

  it('routes dashboard creation to the governed plan workspace without synthetic writes', async () => {
    render(<IdpPlanningDashboardPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Manage Plans and Versions' }));

    expect(app.setCurrentPath).toHaveBeenCalledWith('/idp/plans');
    expect(api.createIdpPlan).not.toHaveBeenCalled();
    expect(api.createIdpPlanVersion).not.toHaveBeenCalled();
  });

  it('submits user-entered predecessor and publication metadata', async () => {
    render(<IdpPlanManagementPage />);
    await screen.findByRole('option', { name: 'IDP-2026 - Current IDP' });
    expect(screen.getByLabelText('Hierarchy/project CSV file')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'KPI definitions' }));
    expect(screen.getByLabelText('KPI CSV file')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Plan title'), { target: { value: 'Successor IDP' } });
    fireEvent.change(screen.getByLabelText('Plan code'), { target: { value: 'IDP-2031' } });
    fireEvent.change(screen.getByLabelText('Start financial year'), { target: { value: '2031' } });
    fireEvent.change(screen.getByLabelText('End financial year'), { target: { value: '2036' } });
    fireEvent.change(screen.getByLabelText('Predecessor plan'), { target: { value: predecessor.publicId } });
    fireEvent.change(screen.getByLabelText('Plan effective from'), { target: { value: '2031-07-01' } });
    fireEvent.change(screen.getByLabelText('Plan publication reference'), { target: { value: 'Council resolution 2031/42' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }));

    await waitFor(() => expect(api.createIdpPlan).toHaveBeenCalledWith(expect.objectContaining({
      municipalityName: '',
      planTitle: 'Successor IDP',
      planCode: 'IDP-2031',
      startFinancialYear: 2031,
      endFinancialYear: 2036,
      predecessorPlanPublicId: predecessor.publicId,
      effectiveFrom: '2031-07-01T00:00:00.000Z',
      publicationReference: 'Council resolution 2031/42',
    })));
  });

  it('does not render import controls without the dynamic import capability', async () => {
    security.canImport.mockReturnValue(false);
    render(<IdpPlanManagementPage />);
    await screen.findByRole('option', { name: 'IDP-2026 - Current IDP' });
    expect(screen.queryByLabelText('KPI CSV file')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Hierarchy/project CSV file')).not.toBeInTheDocument();
    expect(api.getIdpImportBatches).not.toHaveBeenCalled();
  });

  it('queries the plan register through bounded server paging and search', async () => {
    api.getIdpPlansPage.mockResolvedValue({
      success: true,
      data: { items: [predecessor], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 },
    });
    render(<IdpPlanManagementPage />);

    await screen.findByText('26 plans');
    expect(api.getIdpPlansPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25 }));
    fireEvent.change(screen.getByLabelText('Search IDP plans'), { target: { value: '2031' } });
    await waitFor(() => expect(api.getIdpPlansPage).toHaveBeenLastCalledWith(expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: '2031',
    })));
  });
});
