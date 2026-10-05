import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StrategicPlanningAdministration } from './StrategicPlanningAdministration';

const api = vi.hoisted(() => ({
  getStrategicPlanningMastersPage: vi.fn(), getStrategicPlanningRelationships: vi.fn(), saveStrategicPlanningMaster: vi.fn(), linkStrategicPlanningRelationship: vi.fn(), disableStrategicPlanningRelationship: vi.fn(),
}));
const security = { canRead: () => true, canCreate: () => true, canUpdate: () => true };
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => security }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/CalendarMasterPicker', () => ({ CalendarMasterPicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <label>{label}<select aria-label={label} value={value} onChange={event => onChange(event.target.value)}><option value="" /><option value="year-1">2025/26</option></select></label> }));

describe('StrategicPlanningAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getStrategicPlanningMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'goal-1', code: 'SG1', name: 'Inclusive growth', description: null, effectiveFromFinancialYearPublicId: 'year-1', effectiveFromFinancialYearCode: '2025/26', effectiveToFinancialYearPublicId: null, effectiveToFinancialYearCode: null, displayOrder: 10, isActive: true, rowVersion: 'AQ==' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getStrategicPlanningRelationships.mockResolvedValue({ success: true, data: [] });
    api.saveStrategicPlanningMaster.mockResolvedValue({ success: true, data: {} }); api.linkStrategicPlanningRelationship.mockResolvedValue({ success: true, data: {} }); api.disableStrategicPlanningRelationship.mockResolvedValue({ success: true, data: {} });
  });

  it('updates an API-backed effective-dated master with concurrency and audit reason', async () => {
    render(<StrategicPlanningAdministration kind="strategic-goals" />); expect(await screen.findByText('Inclusive growth')).toBeInTheDocument(); fireEvent.click(screen.getByRole('button', { name: 'Edit' }));
    const governanceReason = screen.getAllByRole('textbox').filter(element => element.tagName === 'TEXTAREA')[1];
    fireEvent.change(governanceReason, { target: { value: 'Council corrected strategic wording' } }); fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    await waitFor(() => expect(api.saveStrategicPlanningMaster).toHaveBeenCalledWith('strategic-goals', 'goal-1', expect.objectContaining({ rowVersion: 'AQ==', effectiveFromFinancialYearPublicId: 'year-1', reason: 'Council corrected strategic wording' })));
  });

  it('exposes configurable optional relationships without enforcing a fixed hierarchy', async () => {
    render(<StrategicPlanningAdministration kind="strategic-goals" />); expect(await screen.findByText('Optional strategic relationships')).toBeInTheDocument();
    expect(screen.getByLabelText('Relationship')).toHaveValue('municipal-kpa-strategic-goal'); expect(screen.getByText(/no fixed hierarchy is imposed/i)).toBeInTheDocument();
  });

  it('uses the governed budget endpoint and does not expose strategic relationship controls', async () => {
    render(<StrategicPlanningAdministration kind="budget-sources" />);
    expect(await screen.findByText('Inclusive growth')).toBeInTheDocument();
    expect(api.getStrategicPlanningMastersPage).toHaveBeenCalledWith('budget-sources', expect.anything(), { includeInactive: true });
    expect(api.getStrategicPlanningRelationships).not.toHaveBeenCalled();
    expect(screen.queryByText('Optional strategic relationships')).not.toBeInTheDocument();
  });

  it('uses the governed performance-classification endpoint without strategic relationship controls', async () => {
    render(<StrategicPlanningAdministration kind="kpi-types" />);
    expect(await screen.findByText('Inclusive growth')).toBeInTheDocument();
    expect(api.getStrategicPlanningMastersPage).toHaveBeenCalledWith('kpi-types', expect.anything(), { includeInactive: true });
    expect(api.getStrategicPlanningRelationships).not.toHaveBeenCalled();
    expect(screen.queryByText('Optional strategic relationships')).not.toBeInTheDocument();
  });
});
