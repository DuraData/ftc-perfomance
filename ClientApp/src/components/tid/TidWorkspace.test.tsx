import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TidWorkspace } from './TidWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const capabilities = vi.hoisted(() => ({
  canRead: vi.fn(() => true), canCreate: vi.fn(() => true), canUpdate: vi.fn(() => true),
  canDelete: vi.fn(() => false), canExport: vi.fn(() => false), canImport: vi.fn(() => false),
  canExecute: vi.fn((code: string) => typeof code === 'string'), canReadField: vi.fn(() => true), canEditField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  getTidConfiguration: vi.fn(), updateTidConfiguration: vi.fn(), getTidRegisterPage: vi.fn(), getTidHistoryPage: vi.fn(),
  createTidVersion: vi.fn(), uploadTidSourceDocument: vi.fn(), downloadTidSourceDocument: vi.fn(), rescanTidSourceDocument: vi.fn(), getMunicipalEmployeesPage: vi.fn(),
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
  createdByUserPublicId: '00000000-0000-0000-0000-000000000001', createdByName: 'Protected Owner', rowVersion: 'Ag==', sourceDocuments: [],
};
const item = { targetPublicId: 'target-1', indicatorNumber: 'KPI-1', targetName: 'Water access', departmentName: 'Infrastructure', unitName: null, tidRequired: true, currentVersion: version };

describe('TID workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capabilities.canRead.mockReturnValue(true);
    capabilities.canCreate.mockReturnValue(true);
    capabilities.canUpdate.mockReturnValue(true);
    capabilities.canExecute.mockReturnValue(true);
    capabilities.canReadField.mockReturnValue(true);
    api.getMunicipalEmployeesPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getTidConfiguration.mockResolvedValue({ success: true, data: configuration });
    api.getTidRegisterPage.mockResolvedValue({ success: true, data: { items: [item], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getTidHistoryPage.mockResolvedValue({ success: true, data: { items: [version], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.updateTidConfiguration.mockResolvedValue({ success: true, data: { ...configuration, tidEnabled: false, allKpisRequired: false, rowVersion: 'Aw==' } });
    api.createTidVersion.mockResolvedValue({ success: true, data: { ...version, publicId: 'tid-2', versionNumber: 2, previousVersionPublicId: version.publicId, rowVersion: 'Aw==' } });
  });

  it('updates optional municipality policy with reason and concurrency', async () => {
    render(<TidWorkspace />);
    await screen.findByText('Municipality TID policy');
    expect(api.getMunicipalEmployeesPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'name', sortDirection: 'asc' }, true);
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

  it('hides protected source-document metadata and the independent rescan action', async () => {
    capabilities.canReadField.mockReturnValue(false);
    capabilities.canExecute.mockImplementation(code => code === 'TID.UPLOAD_SOURCE');
    const protectedDocument = {
      publicId: 'source-1', title: 'Protected methodology', fileName: 'source.pdf', contentType: 'application/pdf', sizeInBytes: 100,
      sha256: 'a'.repeat(64), scanStatus: 'ThreatDetected', isQuarantined: true, uploadedAt: '2026-07-01T01:00:00Z',
      uploadedByUserPublicId: '00000000-0000-0000-0000-000000000001', uploadedByName: 'Protected Owner', scannerProvider: 'ProtectedScanner',
      scannerReference: 'protected-reference', scanDetail: 'protected-detail', contentUrl: '/api/v1/tids/tid-1/documents/source-1/content',
    };
    api.getTidHistoryPage.mockResolvedValue({ success: true, data: { items: [{ ...version, sourceDocuments: [protectedDocument] }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });

    render(<TidWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /KPI-1/i }));
    expect(await screen.findByText(/Protected methodology/)).toBeInTheDocument();
    expect(screen.queryByText(/Protected Owner/)).not.toBeInTheDocument();
    expect(screen.queryByText(/ProtectedScanner/)).not.toBeInTheDocument();
    expect(screen.queryByText(/protected-reference/)).not.toBeInTheDocument();
    expect(screen.queryByText(/protected-detail/)).not.toBeInTheDocument();
    expect(screen.queryByText(/owner/)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rescan' })).not.toBeInTheDocument();
    expect(capabilities.canReadField).toHaveBeenCalledWith('TID', 'CreatedByUserId');
  });

  it('loads the authorised KPI register in bounded server pages', async () => {
    api.getTidRegisterPage.mockResolvedValue({ success: true, data: { items: [item], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<TidWorkspace />);

    expect(await screen.findByText('Page 1 of 2 · 26 KPIs')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));

    await waitFor(() => expect(api.getTidRegisterPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'indicatorNumber', sortDirection: 'asc' }),
    ));
  });

  it('searches and pages immutable TID history with authoritative totals', async () => {
    api.getTidHistoryPage.mockResolvedValue({ success: true, data: { items: [version], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 } });
    render(<TidWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /KPI-1/i }));

    expect(await screen.findByText('11 immutable versions')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next versions' }));
    await waitFor(() => expect(api.getTidHistoryPage).toHaveBeenCalledWith('target-1', expect.objectContaining({
      page: 2, pageSize: 10, sortBy: 'versionNumber', sortDirection: 'desc',
    })));

    fireEvent.change(screen.getByLabelText('Search TID version history'), { target: { value: 'billing' } });
    await waitFor(() => expect(api.getTidHistoryPage).toHaveBeenCalledWith('target-1', expect.objectContaining({
      page: 1, pageSize: 10, search: 'billing', sortBy: 'versionNumber', sortDirection: 'desc',
    })));
  });
});
