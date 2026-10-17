import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { OPMSTargetFilters, OPMSTargetList } from './OPMSTargetList';

const api = vi.hoisted(() => ({
  getDepartmentMastersPage: vi.fn(),
  getUnitMastersPage: vi.fn(),
  getPositionMastersPage: vi.fn(),
  getWardMastersPage: vi.fn(),
  getVoteNumberMastersPage: vi.fn(),
  getOpmsTargetsPage: vi.fn(),
}));
const hasAnyPermission = vi.hoisted(() => vi.fn<(codes: string[]) => boolean>(() => true));
vi.mock('../../api/api', () => api);
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ setCurrentPath: vi.fn(), pushToast: vi.fn() }) }));
vi.mock('../security/AccessControl', () => ({ useHasAnyPermission: (codes: string[]) => hasAnyPermission(codes) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: ReactNode }) => <>{children}</> }));
vi.mock('../common/DataTable', () => ({ DataTable: ({ data, actions }: { data: unknown[]; actions?: (row: unknown) => ReactNode }) => <div>{data.map((row, index) => <div key={index}>{actions?.(row)}</div>)}</div> }));
vi.mock('../library/TargetLibraries', () => ({ OpmsTemplateSelectionModal: () => null }));
vi.mock('../common/GovernedWithdrawalDialog', () => ({ GovernedWithdrawalDialog: () => null }));

describe('OPMSTargetFilters', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    hasAnyPermission.mockReturnValue(true);
  });

  it('loads bounded department options and emits a stable public-id filter', async () => {
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [{ publicId: 'department-public', code: 'WAT', name: 'Live Water Services', isActive: true }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    const onFilterChange = vi.fn();
    render(<OPMSTargetFilters onFilterChange={onFilterChange} />);

    await waitFor(() => expect(api.getDepartmentMastersPage).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 25, active: true })));
    fireEvent.change(screen.getByLabelText('Department'), { target: { value: 'department-public' } });

    expect(screen.getByRole('option', { name: 'WAT · Live Water Services' })).toHaveValue('department-public');
    expect(onFilterChange).toHaveBeenCalledWith({ department: 'department-public', status: '' });
  });

  it('hides withdrawal when the reason member cannot be edited', async () => {
    hasAnyPermission.mockImplementation(codes => !codes.includes('OPMS_KPI.WithdrawalReason.UPDATE'));
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOpmsTargetsPage.mockResolvedValue({ success: true, data: { items: [{ id: 'target-1', isWithdrawn: false }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<OPMSTargetList />);

    await waitFor(() => expect(api.getOpmsTargetsPage).toHaveBeenCalled());
    expect(screen.queryByTitle('Withdraw')).not.toBeInTheDocument();
  });

  it('does not expose mutation or submission actions to a KPI read-only user', async () => {
    hasAnyPermission.mockReturnValue(false);
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOpmsTargetsPage.mockResolvedValue({ success: true, data: { items: [{ id: 'target-1', isWithdrawn: false }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<OPMSTargetList />);

    await waitFor(() => expect(api.getOpmsTargetsPage).toHaveBeenCalled());
    expect(screen.getByText('Read Only')).toBeInTheDocument();
    expect(screen.getByText('All permitted departments')).toBeInTheDocument();
    expect(api.getDepartmentMastersPage).not.toHaveBeenCalled();
    expect(screen.queryByTitle('Edit')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Copy')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Start Submission')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'New Target' })).not.toBeInTheDocument();
  });

  it('exposes only edit controls to a KPI update-only user', async () => {
    hasAnyPermission.mockImplementation(codes => codes.includes('OPMS_KPI.UPDATE'));
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOpmsTargetsPage.mockResolvedValue({ success: true, data: { items: [{ id: 'target-1', isWithdrawn: false }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<OPMSTargetList />);

    await waitFor(() => expect(api.getOpmsTargetsPage).toHaveBeenCalled());
    expect(screen.getByTitle('Edit')).toBeInTheDocument();
    expect(screen.queryByText('Read Only')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Copy')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Withdraw')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Start Submission')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create From OPMS Library' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'New Target' })).not.toBeInTheDocument();
  });

  it('exposes only creation controls to a KPI create-only user', async () => {
    hasAnyPermission.mockImplementation(codes => codes.includes('OPMS_KPI.CREATE'));
    api.getDepartmentMastersPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    api.getOpmsTargetsPage.mockResolvedValue({ success: true, data: { items: [{ id: 'target-1', isWithdrawn: false }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });

    render(<OPMSTargetList />);

    await waitFor(() => expect(api.getOpmsTargetsPage).toHaveBeenCalled());
    expect(screen.getByTitle('Copy')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create From OPMS Library' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'New Target' })).toBeInTheDocument();
    expect(screen.queryByText('Read Only')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Edit')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Withdraw')).not.toBeInTheDocument();
    expect(screen.queryByTitle('Start Submission')).not.toBeInTheDocument();
  });
});
