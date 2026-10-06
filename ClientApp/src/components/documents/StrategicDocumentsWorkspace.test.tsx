import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StrategicDocumentsWorkspace } from './StrategicDocumentsWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const capabilities = vi.hoisted(() => ({
  canRead: vi.fn(() => true), canCreate: vi.fn(() => true), canUpdate: vi.fn(() => true),
  canDelete: vi.fn(() => false), canExport: vi.fn(() => false), canImport: vi.fn(() => false),
  canExecute: vi.fn(() => true), canReadField: vi.fn(() => true), canEditField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  approveStrategicDocument: vi.fn(), createStrategicDocumentType: vi.fn(), createStrategicDocumentVersion: vi.fn(),
  downloadStrategicDocument: vi.fn(), getMunicipalityFinancialYearMastersPage: vi.fn(), getStrategicDocumentHistoryPage: vi.fn(),
  getStrategicDocumentsPage: vi.fn(), getStrategicDocumentTypesPage: vi.fn(), publishStrategicDocument: vi.fn(),
  rescanStrategicDocument: vi.fn(), retireStrategicDocument: vi.fn(), updateStrategicDocumentType: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => capabilities }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => api);

const type = { publicId: 'type-1', code: 'IDP', name: 'Integrated Development Plan', description: null, allowsExternalLinks: true, isActive: true, displayOrder: 10, rowVersion: 'AQ==' };
const year = { publicId: 'year-1', financialYearPublicId: 'fy-1', code: '2026/27', name: '2026/27', isCurrent: true, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', effectiveTo: null, rowVersion: 'AQ==' };
const document = {
  publicId: 'document-1', documentFamilyId: 'family-1', previousVersionPublicId: null, versionNumber: 1,
  municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', financialYearName: '2026/27',
  documentTypePublicId: 'type-1', documentTypeCode: 'IDP', documentTypeName: 'Integrated Development Plan',
  sdbipLayer: 'Top Layer', title: 'Approved IDP', description: 'Municipal five-year plan', documentDate: '2026-07-01T00:00:00Z',
  displayOrder: 10, isCurrent: true, isActive: true, isApproved: false, approvedAt: null, approvedByUserId: null,
  approvalReference: null, isPublished: false, publicationDate: null, publishedAt: null, publishedByUserId: null,
  createdAt: '2026-07-01T00:00:00Z', createdByUserId: 'owner', fileName: null, contentType: null, sizeInBytes: null,
  sha256: null, scanStatus: null, isQuarantined: false, externalUrl: 'https://example.gov.za/idp.pdf', contentUrl: null,
  rowVersion: 'Ag==', events: [{ publicId: 'event-1', action: 'VersionCreated', reason: 'Initial version', actorUserId: 'owner', occurredAt: '2026-07-01T00:00:00Z' }],
};

describe('Strategic documents workspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capabilities.canRead.mockReturnValue(true);
    capabilities.canCreate.mockReturnValue(true);
    capabilities.canUpdate.mockReturnValue(true);
    capabilities.canExecute.mockReturnValue(true);
    api.getStrategicDocumentTypesPage.mockResolvedValue({ success: true, data: { items: [type], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [year], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getStrategicDocumentsPage.mockResolvedValue({ success: true, data: { items: [document], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getStrategicDocumentHistoryPage.mockResolvedValue({ success: true, data: { items: [document], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    api.createStrategicDocumentVersion.mockResolvedValue({ success: true, data: { ...document, publicId: 'document-2', title: 'Annual Review' } });
    api.approveStrategicDocument.mockResolvedValue({ success: true, data: { ...document, isApproved: true, approvalReference: 'Council 1/2026', rowVersion: 'Aw==' } });
  });

  it('keeps the published user view readable without administration controls', async () => {
    capabilities.canCreate.mockReturnValue(false);
    capabilities.canUpdate.mockReturnValue(false);
    capabilities.canExecute.mockReturnValue(false);
    render(<StrategicDocumentsWorkspace />);

    fireEvent.click(await screen.findByRole('button', { name: /Approved IDP/i }));
    expect(screen.queryByText('Controlled document types')).not.toBeInTheDocument();
    expect(screen.queryByText('Add strategic document')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Open publication' })).toHaveAttribute('href', 'https://example.gov.za/idp.pdf');
  });

  it('creates a link-backed governed version with municipality-year and type references', async () => {
    render(<StrategicDocumentsWorkspace />);
    await screen.findByText('Add strategic document');
    const yearSelectors = await screen.findAllByRole('combobox', { name: /Strategic document financial year/ });
    fireEvent.change(yearSelectors[yearSelectors.length - 1], { target: { value: 'year-1' } });
    fireEvent.click(screen.getByLabelText('Use approved external link'));
    fireEvent.change(screen.getByLabelText('Strategic document title'), { target: { value: 'Annual Review' } });
    fireEvent.change(screen.getByLabelText('Strategic document external URL'), { target: { value: 'https://example.gov.za/review.pdf' } });
    fireEvent.change(screen.getByLabelText('Strategic document version reason'), { target: { value: 'Council annual review' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Initial Version' }));

    await waitFor(() => expect(api.createStrategicDocumentVersion).toHaveBeenCalled());
    expect(api.createStrategicDocumentVersion.mock.calls[0][0]).toMatchObject({
      municipalityFinancialYearPublicId: 'year-1', documentTypePublicId: 'type-1', title: 'Annual Review',
      externalUrl: 'https://example.gov.za/review.pdf', file: null, reason: 'Council annual review',
    });
  });

  it('submits approval as an action with concurrency and council reference', async () => {
    render(<StrategicDocumentsWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /Approved IDP/i }));
    await screen.findByLabelText('Strategic document action reason');
    await new Promise(resolve => window.setTimeout(resolve, 350));
    fireEvent.change(screen.getByLabelText('Strategic document action reason'), { target: { value: 'Approved by council' } });
    fireEvent.change(screen.getByLabelText('Strategic document approval reference'), { target: { value: 'Council 1/2026' } });
    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    await waitFor(() => expect(api.approveStrategicDocument).toHaveBeenCalledWith('document-1', {
      rowVersion: 'Ag==', approvalReference: 'Council 1/2026', reason: 'Approved by council',
    }));
  });

  it('loads the strategic-document register in bounded server pages', async () => {
    api.getStrategicDocumentsPage.mockResolvedValue({ success: true, data: { items: [document], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<StrategicDocumentsWorkspace />);

    expect(await screen.findByText('Page 1 of 2 · 26 documents')).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole('button', { name: 'Next' }).find(button => !button.hasAttribute('disabled'))!);

    await waitFor(() => expect(api.getStrategicDocumentsPage).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }),
      { municipalityFinancialYearPublicId: undefined },
    ));
  });

  it('loads and pages controlled document types through the bounded register', async () => {
    api.getStrategicDocumentTypesPage.mockResolvedValue({ success: true, data: { items: [type], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<StrategicDocumentsWorkspace />);

    expect(await screen.findByText('26 controlled document types')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next controlled types' }));

    await waitFor(() => expect(api.getStrategicDocumentTypesPage).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'displayOrder', sortDirection: 'asc' }),
    ));
  });

  it('searches and pages immutable strategic-document history with authoritative totals', async () => {
    api.getStrategicDocumentHistoryPage.mockResolvedValue({ success: true, data: { items: [document], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 } });
    render(<StrategicDocumentsWorkspace />);
    fireEvent.click(await screen.findByRole('button', { name: /Approved IDP/i }));

    expect(await screen.findByText('11 immutable versions')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next versions' }));
    await waitFor(() => expect(api.getStrategicDocumentHistoryPage).toHaveBeenCalledWith('family-1', expect.objectContaining({
      page: 2, pageSize: 10, sortBy: 'versionNumber', sortDirection: 'desc',
    })));

    fireEvent.change(screen.getByLabelText('Search strategic document version history'), { target: { value: 'council' } });
    await waitFor(() => expect(api.getStrategicDocumentHistoryPage).toHaveBeenCalledWith('family-1', expect.objectContaining({
      page: 1, pageSize: 10, search: 'council', sortBy: 'versionNumber', sortDirection: 'desc',
    })));
  });
});
