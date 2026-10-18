import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { LegacyTenantOwnershipReconciliationAdministration } from './LegacyTenantOwnershipReconciliationAdministration';

const api = vi.hoisted(() => ({
  getLegacyTenantOwnershipPreview: vi.fn(),
  executeLegacyTenantOwnershipReconciliation: vi.fn(),
}));
const pushToast = vi.fn();
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: ['SECURITY.RECONCILE_TENANT_OWNERSHIP'], pushToast }) }));

describe('LegacyTenantOwnershipReconciliationAdministration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getLegacyTenantOwnershipPreview.mockResolvedValue({ success: true, data: {
      page: 1, pageSize: 25, totalCount: 2, totalPages: 1, items: [
        { entityType: 1, entityPublicId: 'department-ready', label: 'ROADS · Roads', status: 'Ready', message: 'Exactly one municipality is proven by authoritative relationships.', sourceEvidence: 'position.department', rowVersion: 'AQID' },
        { entityType: 1, entityPublicId: 'department-blocked', label: 'UNKNOWN · Unknown', status: 'Blocked', message: 'No authoritative tenant-owned relationship proves municipality ownership.', sourceEvidence: '', rowVersion: 'BAUG' },
      ],
    } });
    api.executeLegacyTenantOwnershipReconciliation.mockResolvedValue({ success: true, data: {
      reconciliationPublicId: 'reconciliation-1', entityType: 1, entityPublicId: 'department-ready',
    } });
  });

  it('executes only a ready record with a substantive governance reason and RowVersion', async () => {
    render(<LegacyTenantOwnershipReconciliationAdministration />);
    await waitFor(() => expect(api.getLegacyTenantOwnershipPreview).toHaveBeenCalledWith(1, 1, 25));
    expect(await screen.findByText('No authoritative tenant-owned relationship proves municipality ownership.')).toBeInTheDocument();
    const buttons = screen.getAllByRole('button', { name: 'Reconcile' });
    expect(buttons[0]).toBeDisabled();
    expect(buttons[1]).toBeDisabled();

    fireEvent.change(screen.getByLabelText('Tenant reconciliation reason'), { target: { value: 'Approved tenant cutover CR-201' } });
    expect(buttons[0]).toBeEnabled();
    expect(buttons[1]).toBeDisabled();
    fireEvent.click(buttons[0]);

    await waitFor(() => expect(api.executeLegacyTenantOwnershipReconciliation).toHaveBeenCalledWith({
      entityType: 1,
      entityPublicId: 'department-ready',
      rowVersion: 'AQID',
      reason: 'Approved tenant cutover CR-201',
    }));
    expect(pushToast).toHaveBeenCalledWith('success', 'ROADS · Roads linked to the selected municipality');
  });

  it('loads every supported legacy register independently', async () => {
    render(<LegacyTenantOwnershipReconciliationAdministration />);
    await screen.findByText('ROADS · Roads');
    fireEvent.change(screen.getByLabelText('Legacy register'), { target: { value: '6' } });
    await waitFor(() => expect(api.getLegacyTenantOwnershipPreview).toHaveBeenLastCalledWith(6, 1, 25));
  });
});
