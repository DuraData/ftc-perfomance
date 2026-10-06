import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { InternalAuditAssessmentPanel } from './InternalAuditAssessmentPanel';

const api = vi.hoisted(() => ({
  getInternalAuditSubmission: vi.fn(),
  getInternalAuditAssessmentsPage: vi.fn(),
  saveInternalAuditAssessment: vi.fn(),
}));

vi.mock('../../api/api', () => api);

describe('InternalAuditAssessmentPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getInternalAuditSubmission.mockResolvedValue({
      success: true,
      data: {
        configuration: { publicId: 'config-1', municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', model: 2, version: 1, isCurrent: true, effectiveFrom: '2026-07-01T00:00:00Z', reason: 'Approved model', rowVersion: 'AQ==' },
        latestAssessment: null,
      },
    });
    api.getInternalAuditAssessmentsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
    api.saveInternalAuditAssessment.mockResolvedValue({ success: true, data: { publicId: 'assessment-1' } });
  });

  it('shows exactly the simplified model fields and requires an RFI due date for Not Satisfactory', async () => {
    render(<InternalAuditAssessmentPanel submissionId="submission-1" canAssess />);

    expect(await screen.findByText('Satisfactory / Not Satisfactory')).toBeInTheDocument();
    expect(screen.getByLabelText('IA Assessment Status')).toBeInTheDocument();
    expect(screen.getByLabelText(/IA Detailed Observation/)).toBeInTheDocument();
    expect(screen.queryByLabelText('Comments')).not.toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('IA Assessment Status'), { target: { value: '4' } });
    expect(screen.getByLabelText(/IA RFI Due Date/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/IA Detailed Observation/), { target: { value: 'Unsigned invoices do not support the actual.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Record assessment' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('IA RFI Due Date is required');

    fireEvent.change(screen.getByLabelText(/IA RFI Due Date/), { target: { value: '2030-01-02T10:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Record assessment' }));
    await waitFor(() => expect(api.saveInternalAuditAssessment).toHaveBeenCalledWith(1, 'submission-1', expect.objectContaining({ outcome: 4, detailedObservation: 'Unsigned invoices do not support the actual.', responseDueAt: expect.any(String) })));
  });

  it('renders immutable assessment and RFI history for read-only users', async () => {
    api.getInternalAuditSubmission.mockResolvedValue({
      success: true,
      data: {
        configuration: { publicId: 'config-1', municipalityFinancialYearPublicId: 'year-1', financialYearCode: '2026/27', model: 2, version: 1, isCurrent: true, effectiveFrom: '2026-07-01T00:00:00Z', reason: 'Approved model', rowVersion: 'AQ==' },
        latestAssessment: { publicId: 'assessment-1', model: 2, outcome: 4, detailedObservation: 'Evidence is incomplete.', assessedByUserId: 'auditor-1', assessedByName: 'Audit User', assessedAt: '2026-10-03T08:00:00Z', rfiPublicId: 'rfi-1', rfiResponseDueAt: '2026-10-10T08:00:00Z' },
      },
    });
    api.getInternalAuditAssessmentsPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'assessment-1', model: 2, outcome: 4, detailedObservation: 'Evidence is incomplete.', assessedByUserId: 'auditor-1', assessedByName: 'Audit User', assessedAt: '2026-10-03T08:00:00Z', rfiPublicId: 'rfi-1', rfiResponseDueAt: '2026-10-10T08:00:00Z' }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 } });
    render(<InternalAuditAssessmentPanel submissionId="submission-1" canAssess={false} />);

    expect(await screen.findByText('Evidence is incomplete.')).toBeInTheDocument();
    expect(screen.getByText(/IA RFI due/)).toBeInTheDocument();
    expect(api.getInternalAuditAssessmentsPage).toHaveBeenCalledWith(1, 'submission-1', expect.objectContaining({ page: 1, pageSize: 10, sortBy: 'assessedAt' }));
    expect(screen.queryByRole('button', { name: /assessment/i })).not.toBeInTheDocument();
  });
});
