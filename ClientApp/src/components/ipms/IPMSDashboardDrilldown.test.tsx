import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { IPMSTargetList } from './IPMSTargetList';
import { IPMSSubmissionsList } from '../opms/OPMSSubmissions';

const api = vi.hoisted(() => ({
  getIpmsTargetsPage: vi.fn(),
  getIpmsSubmissionsPage: vi.fn(),
}));

vi.mock('../../api/api', async importOriginal => ({ ...(await importOriginal<typeof import('../../api/api')>()), ...api }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast: vi.fn(), setCurrentPath: vi.fn() }) }));
vi.mock('../security/AccessControl', () => ({ useHasAnyPermission: () => false }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/DataTable', () => ({ DataTable: ({ emptyMessage }: { emptyMessage?: string }) => <div>{emptyMessage}</div> }));
vi.mock('../library/TargetLibraries', () => ({ IpmsTemplateSelectionModal: () => null }));
vi.mock('../common/GovernedWithdrawalDialog', () => ({ GovernedWithdrawalDialog: () => null }));
vi.mock('../common/Modal', () => ({ Modal: () => null }));
vi.mock('../submissions/SubmissionWorkspace', () => ({ SubmissionWorkspace: () => null }));

const emptyPage = { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };

describe('IPMS dashboard drill-down registers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getIpmsTargetsPage.mockResolvedValue({ success: true, data: emptyPage });
    api.getIpmsSubmissionsPage.mockResolvedValue({ success: true, data: emptyPage });
  });

  it('passes the dashboard financial year, period and outcome to the target register API', async () => {
    window.history.replaceState({}, '', '/ipms/targets?municipalityFinancialYearPublicId=year-1&reportingPeriodPublicId=period-1&dashboardFilter=achieved');

    render(<IPMSTargetList />);

    await waitFor(() => expect(api.getIpmsTargetsPage).toHaveBeenCalledWith(expect.objectContaining({
      municipalityFinancialYearPublicId: 'year-1',
      reportingPeriodPublicId: 'period-1',
      dashboardFilter: 'achieved',
    })));
    expect(screen.getByText('Dashboard filter: achieved')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear dashboard filter' }));
    await waitFor(() => {
      const calls = api.getIpmsTargetsPage.mock.calls;
      const lastQuery = calls[calls.length - 1]?.[0];
      expect(lastQuery).not.toHaveProperty('dashboardFilter');
    });
  });

  it('passes the dashboard financial year, period and state to the submission register API', async () => {
    window.history.replaceState({}, '', '/ipms/submissions?municipalityFinancialYearPublicId=year-1&reportingPeriodPublicId=period-1&dashboardFilter=approved');

    render(<IPMSSubmissionsList />);

    await waitFor(() => expect(api.getIpmsSubmissionsPage).toHaveBeenCalledWith(expect.objectContaining({
      municipalityFinancialYearPublicId: 'year-1',
      reportingPeriodPublicId: 'period-1',
      dashboardFilter: 'approved',
    })));
    expect(screen.getByText('Dashboard filter: approved')).toBeInTheDocument();
  });
});
