import { render, screen } from '@testing-library/react';
import { RiskOperations } from './RiskOperations';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const security = vi.hoisted(() => ({
  canExecute: vi.fn(() => true), canReadField: vi.fn(() => true), canEditField: vi.fn(() => true),
}));
const api = vi.hoisted(() => ({
  getStrategicRisksPage: vi.fn(), getStrategicRiskHeatmap: vi.fn(), getStrategicRiskOwnersPage: vi.fn(),
  getStrategicRiskAssessmentsPage: vi.fn(), getStrategicRiskTreatmentsPage: vi.fn(), getStrategicRiskReviewsPage: vi.fn(),
  recordStrategicRiskAssessment: vi.fn(), saveStrategicRiskTreatment: vi.fn(), recordStrategicRiskReview: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../../api/api', () => api);

const page = (items: unknown[]) => ({ success: true, data: { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: items.length ? 1 : 0 } });

describe('governed strategic-risk operations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canExecute.mockReturnValue(true); security.canReadField.mockReturnValue(true); security.canEditField.mockReturnValue(true);
    api.getStrategicRisksPage.mockResolvedValue(page([{ publicId: 'risk-1', riskReference: 'SR-1', riskTitle: 'Flood exposure', isActive: true }]));
    api.getStrategicRiskOwnersPage.mockResolvedValue(page([{ publicId: 'owner-1', fullName: 'Risk Owner', email: 'owner@example.test' }]));
    api.getStrategicRiskHeatmap.mockResolvedValue({ success: true, data: { cells: [{ likelihood: 3, impact: 4, count: 2 }], activeRisks: 3, assessedRisks: 2, unassessedRisks: 1, openTreatments: 4, overdueTreatments: 1 } });
    api.getStrategicRiskAssessmentsPage.mockResolvedValue(page([{ publicId: 'assessment-1', strategicRiskPublicId: 'risk-1', riskReference: 'SR-1', riskTitle: 'Flood exposure', assessedAt: '2026-10-01T00:00:00Z', inherentLikelihood: 5, inherentImpact: 4, inherentScore: 20, residualLikelihood: 3, residualImpact: 4, residualScore: 12, controlAssessment: 'Verified drainage controls', assessedByUserPublicId: 'owner-1', assessedByName: 'Risk Owner', createdAt: '2026-10-01T00:00:00Z' }]));
    api.getStrategicRiskTreatmentsPage.mockResolvedValue(page([{ publicId: 'treatment-1', strategicRiskPublicId: 'risk-1', riskTitle: 'Flood exposure', title: 'Clear drains', description: 'Monthly maintenance', ownerUserPublicId: 'owner-1', ownerName: 'Risk Owner', dueAt: '2026-11-01T00:00:00Z', status: 'IN_PROGRESS', progressPercent: 40, createdAt: '2026-10-01T00:00:00Z', rowVersion: 'AQ==' }]));
    api.getStrategicRiskReviewsPage.mockResolvedValue(page([{ publicId: 'review-1', strategicRiskPublicId: 'risk-1', riskTitle: 'Flood exposure', reviewedAt: '2026-10-01T00:00:00Z', outcome: 'Risk remains controlled', nextReviewAt: '2027-01-01T00:00:00Z', reviewedByUserPublicId: 'owner-1', reviewedByName: 'Risk Owner', createdAt: '2026-10-01T00:00:00Z' }]));
  });

  it('renders a scored residual heatmap from persisted latest-assessment data', async () => {
    render(<RiskOperations view="heatmap" />);
    expect(await screen.findByLabelText('Likelihood 3, impact 4: 2 risks, High')).toHaveTextContent('2');
    expect(screen.getByText('Overdue treatments')).toBeInTheDocument();
  });

  it('renders distinct assessment, treatment, and review evidence instead of one shared register', async () => {
    const { rerender } = render(<RiskOperations view="assessments" />);
    expect(await screen.findAllByText('Record assessment')).toHaveLength(2);
    expect(screen.getByText('Verified drainage controls')).toBeInTheDocument();
    rerender(<RiskOperations view="treatments" />);
    expect(await screen.findByText('Create treatment action')).toBeInTheDocument();
    expect(screen.getByText('Clear drains')).toBeInTheDocument();
    rerender(<RiskOperations view="reviews" />);
    expect(await screen.findByText('Record periodic review')).toBeInTheDocument();
    expect(screen.getByText('Risk remains controlled')).toBeInTheDocument();
  });

  it('does not expose narrative mutations when only action permission is granted', async () => {
    security.canEditField.mockReturnValue(false);
    render(<RiskOperations view="assessments" />);
    expect(await screen.findByText('Verified drainage controls')).toBeInTheDocument();
    expect(screen.queryByText('Record assessment')).not.toBeInTheDocument();
    expect(security.canExecute).toHaveBeenCalledWith('STRATEGIC_RISK.RECORD_ASSESSMENT');
    expect(security.canEditField).toHaveBeenCalledWith('STRATEGIC_RISK', 'AssessmentControls');
  });
});
