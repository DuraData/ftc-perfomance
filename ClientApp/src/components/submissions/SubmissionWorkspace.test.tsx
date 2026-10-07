import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { OPMSSubmission } from '../../types';
import { SubmissionWorkspace } from './SubmissionWorkspace';

const security = vi.hoisted(() => ({
  canUpdate: vi.fn(),
  canReadField: vi.fn(),
  canEditField: vi.fn(),
  canExecute: vi.fn(),
}));

vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));

const api = vi.hoisted(() => ({
  getOpmsSubmissionAttachmentsPage: vi.fn(),
  getIpmsSubmissionAttachmentsPage: vi.fn(),
  getOpmsConsolidationHistoryPage: vi.fn(),
  getIpmsConsolidationHistoryPage: vi.fn(),
}));

vi.mock('../../api/api', () => api);

const submission = {
  id: '7',
  baseState: 'IN_PROGRESS',
  quarter: 'Q1',
  dueDate: '2026-10-31T00:00:00Z',
  actual: 50,
  actualPerformance: '50',
  variance: 5,
  varianceReason: 'Private reason',
  correctiveMeasure: 'Private corrective action',
  status: 'draft',
  target: {
    targetName: 'Water target',
    period: { fiscalYear: '2026/27' },
    department: { name: 'Infrastructure' },
    unitOfMeasure: { name: 'Percentage', symbol: '%' },
  },
  attachments: [],
  comments: [],
  history: [],
} as unknown as OPMSSubmission;

describe('SubmissionWorkspace member security', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Element.prototype.scrollIntoView = vi.fn();
    security.canUpdate.mockReturnValue(true);
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    security.canExecute.mockReturnValue(false);
    api.getOpmsSubmissionAttachmentsPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    api.getIpmsSubmissionAttachmentsPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    api.getOpmsConsolidationHistoryPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 },
    });
    api.getIpmsConsolidationHistoryPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 },
    });
  });

  it('does not render protected values or editing controls when member access is denied', () => {
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    expect(screen.getByText('Restricted')).toBeInTheDocument();
    expect(screen.queryByText('Private reason')).not.toBeInTheDocument();
    expect(screen.queryByText('Private corrective action')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument();
  });

  it('renders readable members and enables only fields with member update permission', () => {
    security.canReadField.mockImplementation((_resource: string, member: string) => member === 'VarianceReason' || member === 'CorrectiveMeasure');
    security.canEditField.mockImplementation((_resource: string, member: string) => member === 'VarianceReason');
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }));

    expect(screen.getByDisplayValue('Private reason')).toBeInTheDocument();
    expect(screen.queryByDisplayValue('Private corrective action')).not.toBeInTheDocument();
    expect(screen.getByText('Private corrective action')).toBeInTheDocument();
  });

  it('loads evidence from the bounded register and navigates authoritative pages', async () => {
    api.getOpmsSubmissionAttachmentsPage
      .mockResolvedValueOnce({
        success: true,
        data: { items: [{ id: 'evidence-1', publicId: 'public-1', fileName: 'first.pdf', fileSize: 10, fileType: 'application/pdf', uploadedBy: { displayName: 'Uploader' }, uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence', url: '/content/1', scanStatus: 'Clean' }], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 },
      })
      .mockResolvedValueOnce({
        success: true,
        data: { items: [{ id: 'evidence-26', publicId: 'public-26', fileName: 'last.pdf', fileSize: 10, fileType: 'application/pdf', uploadedBy: { displayName: 'Uploader' }, uploadedAt: '2026-09-01T00:00:00Z', documentType: 'evidence', url: '/content/26', scanStatus: 'Clean' }], page: 2, pageSize: 25, totalCount: 26, totalPages: 2 },
      });
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: /Proof of Evidence/i }));
    await waitFor(() => expect(screen.getByText('first.pdf')).toBeInTheDocument());
    expect(screen.getByText('26 evidence records')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Next evidence' }));
    await waitFor(() => expect(screen.getByText('last.pdf')).toBeInTheDocument());
    expect(api.getOpmsSubmissionAttachmentsPage).toHaveBeenLastCalledWith('7', expect.objectContaining({ page: 2, pageSize: 25, sortBy: 'uploadedAt' }));
  });

  it('does not render scanner detail supplied by the API without the POE member grant', async () => {
    api.getOpmsSubmissionAttachmentsPage.mockResolvedValue({
      success: true,
      data: { items: [{ id: 'evidence-1', publicId: 'public-1', fileName: 'protected.pdf', fileSize: 10, fileType: 'application/pdf', uploadedBy: { id: 'private-user', displayName: 'Private Uploader' }, uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence', url: '/content/1', scanStatus: 'ScanFailed', scanDetail: 'Private scanner diagnostic' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: /Proof of Evidence/i }));
    await waitFor(() => expect(screen.getByText('protected.pdf')).toBeInTheDocument());
    expect(screen.queryByText(/Private scanner diagnostic/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Private Uploader/)).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('OPMS_POE', 'ScanDetail');
  });

  it('renders scanner detail after the corresponding POE member grant is effective', async () => {
    security.canReadField.mockImplementation((resource: string, member: string) => resource === 'OPMS_POE' && (member === 'ScanDetail' || member === 'UploadedByName'));
    api.getOpmsSubmissionAttachmentsPage.mockResolvedValue({
      success: true,
      data: { items: [{ id: 'evidence-1', publicId: 'public-1', fileName: 'visible.pdf', fileSize: 10, fileType: 'application/pdf', uploadedBy: { id: 'authorized-user', displayName: 'Authorized Uploader' }, uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence', url: '/content/1', scanStatus: 'ScanFailed', scanDetail: 'Authorized scanner diagnostic' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: /Proof of Evidence/i }));
    await waitFor(() => expect(screen.getByText(/Authorized scanner diagnostic/)).toBeInTheDocument());
    expect(screen.getByText(/Authorized Uploader/)).toBeInTheDocument();
  });

  it('does not render nested POE governance secrets supplied by a hostile API payload', async () => {
    api.getOpmsSubmissionAttachmentsPage.mockResolvedValue({
      success: true,
      data: { items: [{
        id: 'evidence-governance', publicId: 'public-governance', fileName: 'governed.pdf', fileSize: 10,
        fileType: 'application/pdf', uploadedAt: '2026-10-01T00:00:00Z', documentType: 'evidence',
        url: '/content/governance', scanStatus: 'Clean', isActive: false, retainUntil: '2020-01-01T00:00:00Z',
        assessments: [{ publicId: 'assessment-1', outcome: 'Accepted', comment: 'Secret assessment note', assessedByUserId: 'secret-assessor-id', assessedByName: 'Secret Assessor', assessedAt: '2026-10-02T00:00:00Z', correlationId: 'secret-assessment-correlation' }],
        legalHolds: [{ holdId: 'hold-1', holdReference: 'CASE-1', isActive: true, placedReason: 'Preservation', placedByUserId: 'secret-legal-id', placedByName: 'Secret Legal Actor', placedAt: '2026-10-03T00:00:00Z' }],
        disposals: [{ disposalId: 'disposal-1', status: 'Failed', approvalReference: 'COUNCIL-1', reason: 'Retention elapsed', requestedByUserId: 'secret-disposal-id', requestedByName: 'Secret Disposal Actor', requestedAt: '2026-10-04T00:00:00Z', detail: 'Secret disposal failure' }],
      }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    render(<SubmissionWorkspace submission={submission} submissionType="OPMS" />);

    fireEvent.click(screen.getByRole('button', { name: /Proof of Evidence/i }));
    await waitFor(() => expect(screen.getByText('governed.pdf')).toBeInTheDocument());
    for (const secret of ['Secret assessment note', 'Secret Assessor', 'Secret Legal Actor', 'Secret Disposal Actor', 'Secret disposal failure'])
      expect(screen.queryByText(new RegExp(secret))).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('OPMS_POE', 'AssessmentComment');
    expect(security.canReadField).toHaveBeenCalledWith('OPMS_POE', 'DisposalDetail');
  });

  it('loads and navigates the bounded consolidation history register', async () => {
    security.canReadField.mockImplementation((_resource: string, member: string) => member === 'ActualPerformance');
    const midyear = { ...submission, quarter: 'Mid-Year', systemSuggestedActualPerformance: '50' } as OPMSSubmission;
    api.getOpmsConsolidationHistoryPage
      .mockResolvedValueOnce({
        success: true,
        data: { items: [{ publicId: 'event-1', eventType: 'Edited', systemSuggestedActualPerformance: '50', actualPerformance: '45', wasSystemSuggestionEdited: true, sourcePeriods: ['Q1', 'Q2'], actorUserId: 'user-1', reason: 'Reviewed evidence', occurredAt: '2026-10-02T00:00:00Z', correlationId: 'correlation-1' }], page: 1, pageSize: 10, totalCount: 11, totalPages: 2 },
      })
      .mockResolvedValueOnce({
        success: true,
        data: { items: [{ publicId: 'event-11', eventType: 'Generated', systemSuggestedActualPerformance: '50', actualPerformance: '50', wasSystemSuggestionEdited: false, sourcePeriods: ['Q1', 'Q2'], actorUserId: 'user-1', occurredAt: '2026-10-01T00:00:00Z', correlationId: 'correlation-11' }], page: 2, pageSize: 10, totalCount: 11, totalPages: 2 },
      });

    render(<SubmissionWorkspace submission={midyear} submissionType="OPMS" />);

    await waitFor(() => expect(screen.getByText(/Reason: Reviewed evidence/)).toBeInTheDocument());
    expect(screen.getByText('11 immutable events')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next suggestion history' }));
    await waitFor(() => expect(screen.getByText('Generated')).toBeInTheDocument());
    expect(api.getOpmsConsolidationHistoryPage).toHaveBeenLastCalledWith('7', expect.objectContaining({ page: 2, pageSize: 10, sortBy: 'occurredAt' }));
  });
});
