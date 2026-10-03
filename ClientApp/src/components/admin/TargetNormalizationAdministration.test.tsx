import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TargetNormalizationAdministration } from './TargetNormalizationAdministration';

const api = vi.hoisted(() => ({ getTargetNormalizationPreview: vi.fn(), executeTargetNormalization: vi.fn() }));
const pushToast = vi.fn();
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: ['OPMS_KPI.NORMALIZE_LEGACY'], pushToast }) }));

describe('TargetNormalizationAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getTargetNormalizationPreview.mockResolvedValue({ success: true, data: { page: 1, pageSize: 50, totalCount: 3, totalPages: 1, items: [
      { targetPublicId: 'ready-1', indicatorNumber: 'KPI-1', targetName: 'Ready target', status: 'Ready', missingPeriodTargets: 2 },
      { targetPublicId: 'blocked-1', indicatorNumber: 'KPI-2', targetName: 'Blocked target', status: 'Blocked', missingPeriodTargets: 0, error: 'Legacy revision requires governance evidence.' },
      { targetPublicId: 'done-1', indicatorNumber: 'KPI-3', targetName: 'Normalized target', status: 'Normalized', missingPeriodTargets: 0 },
    ] } });
    api.executeTargetNormalization.mockResolvedValue({ success: true, data: { selectedTargets: 1, addedPeriodTargets: 2, alreadyNormalizedTargets: 0 } });
  });

  it('shows readiness, prevents blocked selection and executes explicit reconciliation', async () => {
    render(<TargetNormalizationAdministration />);
    await waitFor(() => expect(api.getTargetNormalizationPreview).toHaveBeenCalledWith(1, 1, 50));
    expect(screen.getByText('Legacy revision requires governance evidence.')).toBeInTheDocument();
    expect(screen.getByLabelText('Select KPI-2')).toBeDisabled();

    fireEvent.click(screen.getByLabelText('Select KPI-1'));
    fireEvent.change(screen.getByLabelText('Reconciliation reason'), { target: { value: 'Approved migration rehearsal CR-42' } });
    fireEvent.click(screen.getByRole('button', { name: 'Reconcile selected (1)' }));

    await waitFor(() => expect(api.executeTargetNormalization).toHaveBeenCalledWith({ targetKind: 1, targetPublicIds: ['ready-1'], reason: 'Approved migration rehearsal CR-42' }));
    expect(pushToast).toHaveBeenCalledWith('success', '2 normalized period targets created');
  });

  it('switches to the IPMS register and resets paging selection', async () => {
    render(<TargetNormalizationAdministration />);
    await screen.findByText('KPI-1 · Ready target');
    fireEvent.change(screen.getByLabelText('Target register'), { target: { value: '2' } });
    await waitFor(() => expect(api.getTargetNormalizationPreview).toHaveBeenLastCalledWith(2, 1, 50));
  });
});
