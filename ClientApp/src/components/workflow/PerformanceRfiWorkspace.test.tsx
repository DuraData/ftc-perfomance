import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PerformanceRfiWorkspace } from './PerformanceRfiWorkspace';

const api = vi.hoisted(() => ({
  getPerformanceRfisPage: vi.fn(),
  raisePerformanceRfi: vi.fn(),
  respondPerformanceRfi: vi.fn(),
  closePerformanceRfi: vi.fn(),
  getOpmsSubmissionAttachmentsPage: vi.fn(),
  getIpmsSubmissionAttachmentsPage: vi.fn(),
}));
const security = vi.hoisted(() => ({
  canRead: vi.fn(),
  canReadField: vi.fn(),
  canEditField: vi.fn(),
  canExecute: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

describe('PerformanceRfiWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canRead.mockReturnValue(true);
    security.canReadField.mockReturnValue(true);
    security.canEditField.mockReturnValue(true);
    security.canExecute.mockReturnValue(true);
    api.getPerformanceRfisPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'rfi-1', question: 'Clarify the variance', raisedByUserId: 'reviewer', raisedAt: '2026-10-01T00:00:00Z', responseDueAt: '2026-10-03T00:00:00Z', rowVersion: 'AQ==', evidence: [] }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getOpmsSubmissionAttachmentsPage.mockResolvedValue({ success: true, data: { items: [{ id: 'file-1', publicId: 'evidence-1', fileName: 'calculation.pdf', fileSize: 100, fileType: 'application/pdf', uploadedBy: {}, uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence', url: '/content', scanStatus: 'Clean', isQuarantined: false }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getIpmsSubmissionAttachmentsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.respondPerformanceRfi.mockResolvedValue({ success: true, data: {} });
  });

  it('loads governed RFIs and submits a concurrent response', async () => {
    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    expect(await screen.findByText('Clarify the variance')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Response'), { target: { value: 'Supporting calculation attached.' } });
    fireEvent.click(screen.getByLabelText('Evidence supporting the response: calculation.pdf'));
    fireEvent.click(screen.getByRole('button', { name: 'Respond' }));

    await waitFor(() => expect(api.respondPerformanceRfi).toHaveBeenCalledWith('rfi-1', { response: 'Supporting calculation attached.', rowVersion: 'AQ==', evidencePublicIds: ['evidence-1'] }));
    expect(api.getPerformanceRfisPage).toHaveBeenCalledWith(1, 'submission-7', expect.objectContaining({ page: 1, pageSize: 25, sortBy: 'raisedAt', sortDirection: 'desc' }));
  });

  it('renders immutable evidence provenance returned by the RFI ledger', async () => {
    api.getPerformanceRfisPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'rfi-1', question: 'Clarify the variance', raisedByUserId: 'reviewer', raisedAt: '2026-10-01T00:00:00Z', responseDueAt: '2026-10-03T00:00:00Z', rowVersion: 'AQ==',
      evidence: [{ publicId: 'link-1', evidencePublicId: '77777777-7777-7777-7777-777777777777', purpose: 2, fileName: 'calculation.pdf', sizeInBytes: 100, sha256: 'abc', linkedByUserId: 'responder', linkedAt: '2026-10-02T00:00:00Z', url: '/api/v1/opms-submissions/66666666-6666-6666-6666-666666666666/attachments/77777777-7777-7777-7777-777777777777/content' }],
    }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    const link = await screen.findByRole('link', { name: /calculation.pdf · response evidence/ });
    expect(link).toHaveAttribute('href', '/api/v1/opms-submissions/66666666-6666-6666-6666-666666666666/attachments/77777777-7777-7777-7777-777777777777/content');
  });

  it('transports search, lifecycle filter, sorting, and paging to the RFI page endpoint', async () => {
    api.getPerformanceRfisPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    expect(await screen.findByText('26 RFIs')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Search RFIs'), { target: { value: 'variance' } });
    fireEvent.change(screen.getByLabelText('Filter RFI status'), { target: { value: 'overdue' } });
    fireEvent.change(screen.getByLabelText('Sort RFIs'), { target: { value: 'dueAt' } });
    fireEvent.change(screen.getByLabelText('RFI sort direction'), { target: { value: 'asc' } });

    await waitFor(() => expect(api.getPerformanceRfisPage).toHaveBeenLastCalledWith(1, 'submission-7', expect.objectContaining({
      page: 1,
      pageSize: 25,
      search: 'variance',
      status: 'overdue',
      sortBy: 'dueAt',
      sortDirection: 'asc',
    })));
    fireEvent.click(screen.getByRole('button', { name: 'Next RFIs' }));
    await waitFor(() => expect(api.getPerformanceRfisPage).toHaveBeenLastCalledWith(1, 'submission-7', expect.objectContaining({ page: 2 })));
  });

  it('fails closed against protected RFI fields in a hostile payload', async () => {
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    api.getPerformanceRfisPage.mockResolvedValue({ success: true, data: { items: [{
      publicId: 'rfi-secret', question: 'Secret audit question', raisedByName: 'Secret Reviewer', raisedAt: '2026-10-01T00:00:00Z', responseDueAt: '2026-10-03T00:00:00Z',
      response: 'Secret management response', respondedByName: 'Secret Responder', respondedAt: '2026-10-02T00:00:00Z', closedByName: 'Secret Closer', closedAt: '2026-10-03T00:00:00Z', rowVersion: 'AQ==',
      evidence: [{ publicId: 'link-secret', evidencePublicId: 'evidence-secret', purpose: 2, fileName: 'secret-payroll.pdf', sha256: 'secret-hash', linkedByName: 'Secret Linker', linkedAt: '2026-10-02T00:00:00Z', url: '/secret-download' }],
    }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    expect(await screen.findByText('Protected RFI question')).toBeInTheDocument();
    expect(screen.queryByText('Secret audit question')).not.toBeInTheDocument();
    expect(screen.queryByText('Secret management response')).not.toBeInTheDocument();
    expect(screen.queryByText(/Secret Reviewer|Secret Responder|Secret Closer|Secret Linker/)).not.toBeInTheDocument();
    expect(screen.queryByText(/secret-payroll\.pdf/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Search RFIs')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Respond' })).not.toBeInTheDocument();
    expect(api.getOpmsSubmissionAttachmentsPage).not.toHaveBeenCalled();
  });
});
