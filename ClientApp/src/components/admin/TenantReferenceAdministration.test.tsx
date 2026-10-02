import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { TenantReferenceAdministration } from './TenantReferenceAdministration';

const api = vi.hoisted(() => ({
  getWardMasters: vi.fn(), getVoteNumberMasters: vi.fn(), getDepartmentMasters: vi.fn(),
  saveWardMaster: vi.fn(), saveVoteNumberMaster: vi.fn(),
}));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn() }) }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => ({ canCreate: () => true, canUpdate: () => true }) }));

describe('TenantReferenceAdministration', () => {
  beforeEach(() => {
    api.getDepartmentMasters.mockResolvedValue({ success: true, data: [{ publicId: 'department-1', code: 'FIN', name: 'Finance', isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'AQ==' }] });
    api.getVoteNumberMasters.mockResolvedValue({ success: true, data: [{ publicId: 'vote-1', id: 1, departmentPublicId: 'department-1', departmentName: 'Finance', code: 'V01', number: '001', name: 'Operating Vote', amount: 1250, isActive: true, effectiveFrom: '2026-07-01T00:00:00Z', rowVersion: 'Ag==' }] });
    api.getWardMasters.mockResolvedValue({ success: true, data: [] });
    api.saveVoteNumberMaster.mockResolvedValue({ success: true, data: {} });
  });

  it('edits a vote number through the governed API with tenant relationship and RowVersion', async () => {
    render(<TenantReferenceAdministration kind="vote-numbers" />);
    fireEvent.click(await screen.findByRole('button', { name: /Operating Vote/ }));
    const reason = screen.getAllByRole('textbox').find(element => element.tagName === 'TEXTAREA');
    expect(reason).toBeDefined();
    fireEvent.change(reason!, { target: { value: 'Council approved budget amendment' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => expect(api.saveVoteNumberMaster).toHaveBeenCalledWith('vote-1', expect.objectContaining({
      departmentPublicId: 'department-1', code: 'V01', number: '001', amount: 1250, rowVersion: 'Ag==', reason: 'Council approved budget amendment',
    })));
  });
});
