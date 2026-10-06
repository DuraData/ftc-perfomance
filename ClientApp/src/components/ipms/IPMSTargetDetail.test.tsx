import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { IPMSTarget } from '../../types';
import { GeneralInfoTab, IPMSTargetDetail } from './IPMSTargetDetail';

const api = vi.hoisted(() => ({
  getIpmsTarget: vi.fn(),
  getIpmsSubmissionsPage: vi.fn(),
  getAuditTrailsPage: vi.fn(),
}));

vi.mock('../../api/api', async importOriginal => ({ ...(await importOriginal<typeof import('../../api/api')>()), ...api }));
vi.mock('../../context/AppContext', () => ({ useApp: () => ({ setCurrentPath: vi.fn() }) }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/Tabs', () => ({ Tabs: ({ tabs, onChange }: { tabs: Array<{ id: string; label: string; badge?: number }>; onChange: (id: string) => void }) => <div>{tabs.map(tab => <button key={tab.id} type="button" onClick={() => onChange(tab.id)}>{tab.label} {tab.badge ?? ''}</button>)}</div> }));

describe('IPMS target detail', () => {
  it('renders target relationships from the API model instead of fixture options', () => {
    const target = {
      period: { id: 'period-live', name: 'Live Quarter 1' },
      department: { id: 'department-live', name: 'Live Finance' },
      unit: { id: 'unit-live', name: 'Live Budget Unit' },
      assignedTo: { id: 'employee-live', displayName: 'Live Employee' },
      indicatorNumber: 'IPMS-LIVE-1',
      targetName: 'Live Employee KPI',
      kpiDescription: 'Live KPI description',
      targetUnitType: 'percentage',
      unitOfMeasure: { id: 'uom-live', name: 'Percent' },
      kpiType: 'Quantitative',
      indicatorType: 'Output',
      weight: 20,
    } as unknown as IPMSTarget;

    render(<GeneralInfoTab target={target} />);

    expect(screen.getByDisplayValue('Live Quarter 1')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Finance')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Budget Unit')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Live Employee')).toBeInTheDocument();
    expect(screen.getByDisplayValue('IPMS-LIVE-1')).toBeInTheDocument();
  });

  it('pages the target submission ledger with its authoritative total', async () => {
    const target = {
      id: 'target-public', period: { id: 'period-live', name: 'Live Quarter 1' }, department: { id: 'department-live', name: 'Live Finance' }, unit: null,
      assignedTo: null, indicatorNumber: 'IPMS-LIVE-1', targetName: 'Live Employee KPI', kpiDescription: 'Live KPI description', targetUnitType: 'percentage',
      unitOfMeasure: { id: 'uom-live', name: 'Percent' }, kpiType: 'Quantitative', indicatorType: 'Output', annualTarget: 100, baseline: 50, weight: 20,
    } as unknown as IPMSTarget;
    api.getIpmsTarget.mockResolvedValue({ success: true, data: target });
    api.getIpmsSubmissionsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 26, totalPages: 2 } }));
    api.getAuditTrailsPage.mockImplementation(async ({ page }: { page: number }) => ({ success: true, data: { items: [], page, pageSize: 25, totalCount: 26, totalPages: 2 } }));

    render(<IPMSTargetDetail targetId="target-public" />);
    await waitFor(() => expect(api.getIpmsSubmissionsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, targetPublicId: 'target-public' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'IpmsTarget', entityId: 'target-public' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Submissions 26' }));
    expect(screen.getByText('26 submissions · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next submissions' }));
    await waitFor(() => expect(api.getIpmsSubmissionsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, targetPublicId: 'target-public' }));

    fireEvent.click(screen.getByRole('button', { name: 'Audit' }));
    expect(screen.getByText('26 audit entries · Page 1 of 2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Next audit entries' }));
    await waitFor(() => expect(api.getAuditTrailsPage).toHaveBeenLastCalledWith({ page: 2, pageSize: 25, sortBy: 'createdAt', sortDirection: 'desc' }, { entityName: 'IpmsTarget', entityId: 'target-public' }));
  });
});
