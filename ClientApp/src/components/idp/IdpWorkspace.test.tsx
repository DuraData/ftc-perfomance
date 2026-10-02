import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IdpPlanManagementPage, IdpPlanningDashboardPage } from './IdpWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }));
const api = vi.hoisted(() => ({
  createIdpPlan: vi.fn(),
  createIdpPlanVersion: vi.fn(),
  getIdpPlans: vi.fn(),
  getIdpPlanHierarchy: vi.fn(),
  getIdpDashboard: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../security/AccessControl', () => ({ useHasAnyPermission: () => true }));
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
    api.getIdpPlans.mockResolvedValue({ success: true, data: [predecessor] });
    api.getIdpPlanHierarchy.mockResolvedValue({ success: true, data: { versions: [] } });
    api.getIdpDashboard.mockResolvedValue({ success: true, data: null });
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
});
