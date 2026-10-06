import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { OPMSTarget } from '../../types';
import { AssigneesTab, OPMSTargetDetail, VoteNumbersTab } from './OPMSTargetDetail';

const api = vi.hoisted(() => ({
  getOpmsTarget: vi.fn(),
  getOpmsSubmissionsPage: vi.fn(),
  getIpmsTargetsPage: vi.fn(),
  getAuditTrailsPage: vi.fn(),
}));

vi.mock('../../api/api', async importOriginal => ({ ...(await importOriginal<typeof import('../../api/api')>()), ...api }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ setCurrentPath: vi.fn() }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/Tabs', () => ({ Tabs: ({ tabs, onChange }: { tabs: Array<{ id: string; label: string; badge?: number }>; onChange: (id: string) => void }) => <div>{tabs.map(tab => <button key={tab.id} type="button" onClick={() => onChange(tab.id)}>{tab.label} {tab.badge ?? ''}</button>)}</div> }));

const liveTarget = {
  department: { id: '17', name: 'Live Water Services' },
  voteNumbers: [{ id: 'vote-live', number: 'V-100', name: 'Live Capital Vote', amount: 2500 }],
  assignedTo: { id: 'employee-one', firstName: 'Live', lastName: 'Owner', displayName: 'Live Owner', email: 'owner@example.test' },
  additionalAssignees: [{ id: 'employee-two', firstName: 'Live', lastName: 'Support', displayName: 'Live Support', email: 'support@example.test' }],
} as OPMSTarget;

describe('OPMS target relational tabs', () => {
  it('renders vote numbers from the target API model', () => {
    render(<VoteNumbersTab target={liveTarget} />);

    expect(screen.getByText('V-100')).toBeInTheDocument();
    expect(screen.getByText('Live Capital Vote')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });

  it('renders only target assignees and removes duplicate actors', () => {
    render(<AssigneesTab target={{ ...liveTarget, additionalAssignees: [liveTarget.assignedTo!, ...liveTarget.additionalAssignees] }} />);

    expect(screen.getAllByText('Live Owner')).toHaveLength(1);
    expect(screen.getByText('Live Support')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
  });

  it('pages target submissions and linked IPMS records with authoritative totals', async () => {
    const detailTarget = {
      ...liveTarget,
      id: 'target-public',
      period: { id: 'period-1', name: 'Quarter 1' },
      unit: null,
      indicatorNumber: 'OPMS-1', targetName: 'Water KPI', kpiDescription: 'Description', targetUnitType: 'percentage',
      unitOfMeasure: { id: 'unit-1', name: 'Percent' }, kpiType: 'Quantitative', indicatorType: 'Output', annualTarget: 100, baseline: 50, weight: 10,
    } as unknown as OPMSTarget;
    api.getOpmsTarget.mockResolvedValue({ success: true, data: detailTarget });
    api.getOpmsSubmissionsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 26, totalPages: 2 } }));
    api.getIpmsTargetsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 27, totalPages: 2 } }));
    api.getAuditTrailsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 26, totalPages: 2 } }));

    render(<OPMSTargetDetail targetId="target-public" />);
    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, targetPublicId: 'target-public' }));
    await waitFor(() => expect(api.getIpmsTargetsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, relatedOpmsTargetPublicId: 'target-public' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'OpmsTarget', entityId: 'target-public' }));

    fireEvent.click(await screen.findByRole('button', { name: 'Submissions 26' }));
    expect(screen.getByText('26 submissions · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next submissions' }));
    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, targetPublicId: 'target-public' }));

    fireEvent.click(screen.getByRole('button', { name: 'IPMS 27' }));
    expect(screen.getByText('27 linked targets · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next linked targets' }));
    await waitFor(() => expect(api.getIpmsTargetsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, relatedOpmsTargetPublicId: 'target-public' }));

    fireEvent.click(screen.getByRole('button', { name: 'Audit' }));
    expect(screen.getByText('26 audit entries · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next audit entries' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'OpmsTarget', entityId: 'target-public' }));
  });
});
