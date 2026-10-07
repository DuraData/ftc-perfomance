import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { IdpDocumentsPage } from './IdpDocumentsPage';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const security = vi.hoisted(() => ({
  canRead: vi.fn(() => true),
  canCreate: vi.fn(() => true),
  canExecute: vi.fn(() => true),
  canReadField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  downloadIdpDocument: vi.fn(),
  getIdpDocumentsPage: vi.fn(),
  getIdpPlansPage: vi.fn(),
  rescanIdpDocument: vi.fn(),
  uploadIdpDocument: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../../api/api', () => api);

const plan = {
  id: 7, publicId: 'plan-public-id', municipalityName: 'Blue Hills', planTitle: 'Current IDP', planCode: 'IDP-2026',
  startFinancialYear: 2026, endFinancialYear: 2031, status: 'Published', currentVersionNumber: 2,
  createdAt: '2026-07-01T00:00:00Z', rowVersion: 'AQ==', planFamilyId: 'family-id', effectiveFrom: '2026-07-01T00:00:00Z',
};
const document = {
  publicId: 'document-public-id', idpPlanPublicId: plan.publicId, planVersionNumber: 2, category: 'Policy', title: 'Approved IDP policy',
  fileName: 'approved.pdf', downloadUrl: 'https://opms.test/api/v1/idp/plans/plan-public-id/documents/document-public-id/content',
  contentType: 'application/pdf', sizeInBytes: 2048, versionNumber: 1, isApproved: false, uploadedAt: '2026-10-01T10:00:00Z',
  uploadedByUserId: 'owner', uploadedByName: 'Document Owner', sha256: 'a'.repeat(64), signatureVerified: true, scanStatus: 'Clean',
  isQuarantined: false, scannerProvider: 'test', scannerReference: 'scan-1', scanDetail: null, scannedAt: '2026-10-01T10:00:01Z',
  retainUntil: '2033-10-01T10:00:00Z', evidenceBlobPublicId: 'blob-public-id', isContentDeleted: false, rowVersion: 'Ag==',
};

describe('IDP document register', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canRead.mockReturnValue(true);
    security.canCreate.mockReturnValue(true);
    security.canExecute.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    api.getIdpPlansPage.mockResolvedValue({ success: true, data: { items: [plan], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getIdpDocumentsPage.mockResolvedValue({ success: true, data: { items: [document], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    api.uploadIdpDocument.mockResolvedValue({ success: true, data: document, message: 'Document uploaded.' });
    api.rescanIdpDocument.mockResolvedValue({ success: true, data: document, message: 'Document released.' });
    api.downloadIdpDocument.mockResolvedValue({ success: true, data: true });
  });

  it('loads, filters, searches, sorts, and pages through the bounded plan-scoped endpoint', async () => {
    render(<IdpDocumentsPage />);
    expect(await screen.findByText('Approved IDP policy')).toBeInTheDocument();
    expect(api.getIdpDocumentsPage).toHaveBeenCalledWith(plan.publicId,
      expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'uploadedAt', sortDirection: 'desc' }),
      { category: undefined, scanStatus: undefined, quarantined: undefined });

    fireEvent.change(screen.getByLabelText('Search IDP documents'), { target: { value: 'policy' } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply search' }));
    await waitFor(() => expect(api.getIdpDocumentsPage).toHaveBeenCalledWith(plan.publicId,
      expect.objectContaining({ search: 'policy' }), expect.anything()));

    fireEvent.change(screen.getByLabelText('Filter IDP document category'), { target: { value: 'Policy' } });
    await waitFor(() => expect(api.getIdpDocumentsPage).toHaveBeenCalledWith(plan.publicId,
      expect.anything(), expect.objectContaining({ category: 'Policy' })));

    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(api.getIdpDocumentsPage).toHaveBeenCalledWith(plan.publicId,
      expect.objectContaining({ page: 2 }), expect.anything()));
  });

  it('uploads, rescans, and downloads through governed actions', async () => {
    render(<IdpDocumentsPage />);
    await screen.findByText('Approved IDP policy');
    const file = new File(['%PDF-1.7'], 'council.pdf', { type: 'application/pdf' });
    fireEvent.change(screen.getByLabelText('IDP document title'), { target: { value: 'Council approved IDP' } });
    fireEvent.change(screen.getByLabelText('IDP document category'), { target: { value: 'SignedIdp' } });
    fireEvent.change(screen.getByLabelText('IDP document plan version'), { target: { value: '2' } });
    fireEvent.change(screen.getByLabelText('IDP document file'), { target: { files: [file] } });
    fireEvent.click(screen.getByRole('button', { name: 'Upload and scan' }));
    await waitFor(() => expect(api.uploadIdpDocument).toHaveBeenCalledWith(plan.publicId, {
      file, category: 'SignedIdp', title: 'Council approved IDP', planVersionNumber: 2,
    }));

    fireEvent.click(screen.getByRole('button', { name: 'Rescan' }));
    await waitFor(() => expect(api.rescanIdpDocument).toHaveBeenCalledWith(plan.publicId, document.publicId));
    fireEvent.click(screen.getByRole('button', { name: 'Download' }));
    await waitFor(() => expect(api.downloadIdpDocument).toHaveBeenCalledWith(document));
  });

  it('does not call protected endpoints when direct navigation lacks permission', async () => {
    security.canRead.mockReturnValue(false);
    render(<IdpDocumentsPage />);
    expect(screen.getByText('You do not have permission to read IDP documents.')).toBeInTheDocument();
    expect(api.getIdpPlansPage).not.toHaveBeenCalled();
    expect(api.getIdpDocumentsPage).not.toHaveBeenCalled();
  });

  it('hides denied uploader and scanner metadata plus independent actions', async () => {
    security.canCreate.mockReturnValue(false);
    security.canExecute.mockReturnValue(false);
    security.canReadField.mockReturnValue(false);
    api.getIdpDocumentsPage.mockResolvedValue({ success: true, data: { items: [{ ...document, scanDetail: 'Protected scanner diagnostic' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<IdpDocumentsPage />);
    expect(await screen.findByText('Approved IDP policy')).toBeInTheDocument();
    expect(screen.queryByText('Document Owner')).not.toBeInTheDocument();
    expect(screen.queryByText('Provider: test')).not.toBeInTheDocument();
    expect(screen.queryByText('Reference: scan-1')).not.toBeInTheDocument();
    expect(screen.queryByText('Protected scanner diagnostic')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Upload and scan' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rescan' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Download' })).toBeInTheDocument();
  });
});
