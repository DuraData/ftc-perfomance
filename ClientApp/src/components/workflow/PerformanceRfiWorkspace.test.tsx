import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PerformanceRfiWorkspace } from './PerformanceRfiWorkspace';

const api = vi.hoisted(() => ({
  getPerformanceRfis: vi.fn(),
  raisePerformanceRfi: vi.fn(),
  respondPerformanceRfi: vi.fn(),
  closePerformanceRfi: vi.fn(),
  getOpmsSubmissionAttachments: vi.fn(),
  getIpmsSubmissionAttachments: vi.fn(),
}));

vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canExecute: () => true }) }));

describe('PerformanceRfiWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getPerformanceRfis.mockResolvedValue({ success: true, data: [{ publicId: 'rfi-1', question: 'Clarify the variance', raisedByUserId: 'reviewer', raisedAt: '2026-10-01T00:00:00Z', responseDueAt: '2026-10-03T00:00:00Z', rowVersion: 'AQ==', evidence: [] }] });
    api.getOpmsSubmissionAttachments.mockResolvedValue({ success: true, data: [{ id: 'file-1', publicId: 'evidence-1', fileName: 'calculation.pdf', fileSize: 100, fileType: 'application/pdf', uploadedBy: {}, uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence', url: '/content', scanStatus: 'Clean', isQuarantined: false }] });
    api.getIpmsSubmissionAttachments.mockResolvedValue({ success: true, data: [] });
    api.respondPerformanceRfi.mockResolvedValue({ success: true, data: {} });
  });

  it('loads governed RFIs and submits a concurrent response', async () => {
    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    expect(await screen.findByText('Clarify the variance')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Response'), { target: { value: 'Supporting calculation attached.' } });
    fireEvent.click(screen.getByLabelText('Evidence supporting the response: calculation.pdf'));
    fireEvent.click(screen.getByRole('button', { name: 'Respond' }));

    await waitFor(() => expect(api.respondPerformanceRfi).toHaveBeenCalledWith('rfi-1', { response: 'Supporting calculation attached.', rowVersion: 'AQ==', evidencePublicIds: ['evidence-1'] }));
    expect(api.getPerformanceRfis).toHaveBeenCalledWith(1, 'submission-7');
  });

  it('renders immutable evidence provenance returned by the RFI ledger', async () => {
    api.getPerformanceRfis.mockResolvedValue({ success: true, data: [{
      publicId: 'rfi-1', question: 'Clarify the variance', raisedByUserId: 'reviewer', raisedAt: '2026-10-01T00:00:00Z', responseDueAt: '2026-10-03T00:00:00Z', rowVersion: 'AQ==',
      evidence: [{ publicId: 'link-1', evidencePublicId: 'evidence-1', purpose: 2, fileName: 'calculation.pdf', sizeInBytes: 100, sha256: 'abc', linkedByUserId: 'responder', linkedAt: '2026-10-02T00:00:00Z', url: '/api/opms-submissions/submission-7/attachments/file-1/content' }],
    }] });

    render(<PerformanceRfiWorkspace kind={1} submissionId="submission-7" />);

    const link = await screen.findByRole('link', { name: /calculation.pdf · response evidence/ });
    expect(link).toHaveAttribute('href', '/api/opms-submissions/submission-7/attachments/file-1/content');
  });
});
