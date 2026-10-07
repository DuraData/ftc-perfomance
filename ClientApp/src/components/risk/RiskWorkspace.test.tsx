import { render, screen } from '@testing-library/react';
import { RiskRegisterPage } from './RiskWorkspace';

const app = vi.hoisted(() => ({ pushToast: vi.fn() }));
const security = vi.hoisted(() => ({
  canRead: vi.fn(() => true), canCreate: vi.fn(() => true), canUpdate: vi.fn(() => true),
  canDelete: vi.fn(() => false), canExport: vi.fn(() => false), canImport: vi.fn(() => false),
  canExecute: vi.fn(() => false), canReadField: vi.fn(() => false), canEditField: vi.fn(() => false),
}));
const api = vi.hoisted(() => ({
  getMunicipalityFinancialYearMastersPage: vi.fn(), getStrategicRiskLinksPage: vi.fn(),
  getStrategicRisksPage: vi.fn(), getStrategicRiskSummary: vi.fn(), linkStrategicRisk: vi.fn(),
  saveStrategicRisk: vi.fn(), unlinkStrategicRisk: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));
vi.mock('../common/TargetPicker', () => ({ TargetPicker: () => <div>Target picker</div> }));

const risk = {
  publicId: 'risk-1', riskReference: 'SR-1', riskTitle: 'Water interruption',
  riskDescription: 'hostile protected assessment', effectiveFromMunicipalityFinancialYearPublicId: null,
  effectiveFromFinancialYear: null, effectiveToMunicipalityFinancialYearPublicId: null,
  effectiveToFinancialYear: null, isActive: true, activeKpiLinks: 1,
  createdAt: '2026-10-01T00:00:00Z', updatedAt: null, rowVersion: 'AQ==',
};
const link = {
  publicId: 'link-1', strategicRiskPublicId: 'risk-1', riskReference: 'SR-1', riskTitle: 'Water interruption',
  targetPublicId: 'target-1', indicatorNumber: 'KPI-1', targetName: 'Maintain supply',
  departmentName: 'Infrastructure', unitName: null, isPrimary: true, isActive: false,
  linkedAt: '2026-10-01T00:00:00Z', linkReason: 'hostile link rationale',
  unlinkedAt: '2026-10-02T00:00:00Z', unlinkReason: 'hostile mitigation outcome', rowVersion: 'Ag==',
};

describe('Strategic risk member security', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    security.canRead.mockReturnValue(true);
    security.canCreate.mockReturnValue(true);
    security.canUpdate.mockReturnValue(true);
    security.canExecute.mockReturnValue(false);
    security.canReadField.mockReturnValue(false);
    security.canEditField.mockReturnValue(false);
    api.getStrategicRiskSummary.mockResolvedValue({ success: true, data: { totalRisks: 1, activeRisks: 1, linkedRisks: 1, unlinkedActiveRisks: 0, linkedKpis: 1 } });
    api.getStrategicRisksPage.mockResolvedValue({ success: true, data: { items: [risk], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getStrategicRiskLinksPage.mockResolvedValue({ success: true, data: { items: [link], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getMunicipalityFinancialYearMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } });
  });

  it('does not render or edit hostile protected narratives without member grants', async () => {
    render(<RiskRegisterPage />);

    expect(await screen.findAllByText(/SR-1 · Water interruption/)).toHaveLength(2);
    expect(screen.queryByText('hostile protected assessment')).not.toBeInTheDocument();
    expect(screen.queryByText(/hostile link rationale/)).not.toBeInTheDocument();
    expect(screen.queryByText(/hostile mitigation outcome/)).not.toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Description' })).not.toBeInTheDocument();
    expect(security.canReadField).toHaveBeenCalledWith('STRATEGIC_RISK', 'RiskDescription');
    expect(security.canReadField).toHaveBeenCalledWith('STRATEGIC_RISK', 'LinkReason');
    expect(security.canReadField).toHaveBeenCalledWith('STRATEGIC_RISK', 'UnlinkReason');
  });
});
