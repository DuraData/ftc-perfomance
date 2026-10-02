import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TidWorkspace } from './TidWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const capabilities = vi.hoisted(() => ({
  canRead: vi.fn(() => true), canCreate: vi.fn(() => true), canUpdate: vi.fn(() => true),
  canDelete: vi.fn(() => false), canExport: vi.fn(() => false), canImport: vi.fn(() => false),
  canExecute: vi.fn(() => true), canReadField: vi.fn(() => true), canEditField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  getTidConfiguration: vi.fn(), updateTidConfiguration: vi.fn(), getTidRegister: vi.fn(), getTidHistory: vi.fn(),
  createTidVersion: vi.fn(), uploadTidSourceDocument: vi.fn(), downloadTidSourceDocument: vi.fn(), getMunicipalEmployees: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => capabilities }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => api);

const configuration = { municipalityPublicId: 'municipality-1', tidEnabled: true, allKpisRequired: true, scopedKpiCount: 1, currentTidCount: 1, missingTidCount: 0, rowVersion: 'AQ==' };
const version = {
  publicId: 'tid-1', targetPublicId: 'target-1', versionNumber: 1, previousVersionPublicId: null,
  indicatorDefinition: 'Households with access', purpose: 'Measure access', dataSource: 'Billing', collectionMethod: 'Extract',
  calculationMethod: 'Numerator divided by denominator', numeratorDescription: 'Served households', denominatorDescription: 'All households',
  limitations: null, assumptions: null, verificationMethod: 'Reconcile', responsibleEmployeePublicId: null, responsibleEmployeeName: null,
  notes: null, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, isCurrent: true, createdAt: '2026-07-01T00:00:00Z',
  createdByUserId: 'owner', rowVersion: 'Ag==', sourceDocuments: [],
};
const item = { targetPublicId: 'target-1', indicatorNumber: 'KPI-1', targetName: 'Water access', departmentName: 'Infrastructure', unitName: null, tidRequired: true, currentVersion: version };

describe('TID workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capabilities.canRead.mockReturnValue(true);
    capabilities.canCreate.mockReturnValue(true);
    capabilities.canUpdate.mockReturnValue(true);
    capabilities.canExecute.mockReturnValue(true);
    api.getMunicipalEmployees.mockResolvedValue({ success: true, data: [] });
    api.getTidConfiguration.mockResolvedValue({ success: true, data: configuration });
    api.getTidRegister.mockResolvedValue({ success: true, data: [item] });
    api.getTidHistory.mockResolvedValue({ success: true, data: [version] });
    api.updateTidConfiguration.mockResolvedValue({ success: true, data: { ...configuration, tidEnabled: false, allKpisRequired: false, rowVersion: 'Aw==' } });
    api.createTidVersion.mockResolvedValue({ success: true, data: { ...version, publicId: 'tid-2', versionNumber: 2, previousVersionPublicId: version.publicId, rowVersion: 'Aw==' } });
  });

  it('updates optional municipality policy with reason and concurrency', async () => {
    render(<TidWorkspace />);
    await screen.findByText('Municipality TID policy');
    fireEvent.click(screen.getByLabelText('Enable TID'));
    fireEvent.change(screen.getByLabelText('TID configuration reason'), { target: { value: 'Municipality opted out' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Policy' }));

    await waitFor(() => expect(api.updateTidConfiguration).toHaveBeenCalledWith({
      tidEnabled: false, allKpisRequired: false, rowVersion: 'AQ==', reason: 'Municipality opted out',
    }));
  });

  it('creates a successor TID version without transporting target or actual values', async () => {
    render(<TidWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /KPI-1/i }));
    await screen.findByDisplayValue('Households with access');
    fireEvent.change(screen.getByLabelText('TID effective from'), { target: { value: '2027-07-01' } });
    fireEvent.change(screen.getByLabelText('TID version reason'), { target: { value: 'Approved methodology revision' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Successor Version' }));

    await waitFor(() => expect(api.createTidVersion).toHaveBeenCalled());
    const payload = api.createTidVersion.mock.calls[0][1];
    expect(api.createTidVersion.mock.calls[0][0]).toBe('target-1');
    expect(payload).toMatchObject({ previousVersionRowVersion: 'Ag==', reason: 'Approved methodology revision', effectiveFrom: '2027-07-01T00:00:00.000Z' });
    expect(payload).not.toHaveProperty('annualTarget');
    expect(payload).not.toHaveProperty('actual');
    expect(payload).not.toHaveProperty('unitOfMeasure');
    expect(payload).not.toHaveProperty('workflow');
  });

  it('keeps operational history visible while hiding authoring controls from read-only users', async () => {
    capabilities.canCreate.mockReturnValue(false);
    capabilities.canUpdate.mockReturnValue(false);
    capabilities.canExecute.mockReturnValue(false);
    render(<TidWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /KPI-1/i }));
    expect(await screen.findByText('Version 1')).toBeInTheDocument();
    expect(screen.queryByLabelText('TID indicator definition')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save Policy' })).not.toBeInTheDocument();
  });
});
