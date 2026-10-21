import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { OPMSTarget } from '../../types';
import { SdbipRevisionManagement } from './SdbipRevisionManagement';

const mocks = vi.hoisted(() => ({
  getOpmsTargetsPage: vi.fn(),
  pushToast: vi.fn(),
}));

vi.mock('../../api/api', () => ({ getOpmsTargetsPage: mocks.getOpmsTargetsPage }));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ currentFinancialYearPublicId: 'financial-year-1', pushToast: mocks.pushToast }),
}));
vi.mock('../layout/AppShell', () => ({
  AppShell: ({ title, children }: { title: string; children: React.ReactNode }) => <main><h1>{title}</h1>{children}</main>,
}));
vi.mock('../common/DataTable', () => ({
  DataTable: ({ data, actions, emptyMessage }: { data: OPMSTarget[]; actions: (target: OPMSTarget) => React.ReactNode; emptyMessage: string }) => <div>
    {data.length ? data.map(target => <div key={target.id}><span>{target.indicatorNumber}</span>{actions(target)}</div>) : emptyMessage}
  </div>,
}));
vi.mock('../targets/KpiDefinitionRevisionEditor', () => ({
  KpiDefinitionRevisionEditor: ({ target }: { target: OPMSTarget }) => <div>Definition editor for {target.indicatorNumber}</div>,
}));
vi.mock('../targets/KpiOrderingEditor', () => ({
  KpiOrderingEditor: ({ targetId }: { targetId: string }) => <div>Ordering editor for {targetId}</div>,
}));
vi.mock('../targets/PerformancePeriodTargetEditor', () => ({
  PerformancePeriodTargetEditor: ({ targetPublicId }: { targetPublicId: string }) => <div>Period editor for {targetPublicId}</div>,
}));

const target = {
  id: 'target-1',
  indicatorNumber: 'KPI-001',
  targetName: 'Approved roads target',
  kpiDescription: 'Maintain approved roads',
  originalOrderNumber: 4,
  revisedOrderNumber: 2,
  isIndicatorNumberRevised: false,
  isTargetNameRevised: false,
  isKpiDescriptionRevised: false,
  isRevised: true,
  isWithdrawn: false,
  department: { id: 'department-1', name: 'Infrastructure', code: 'INFRA', isActive: true },
  sdbipLayer: { publicId: 'layer-1', code: 'TOP', name: 'Top Layer' },
  rowVersion: 'row-version',
} as unknown as OPMSTarget;

describe('SdbipRevisionManagement', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getOpmsTargetsPage.mockResolvedValue({
      success: true,
      data: { items: [target], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 },
    });
  });

  it('loads only the active Financial Year and exposes the three governed revision areas', async () => {
    render(<SdbipRevisionManagement />);

    expect(await screen.findByText('KPI-001')).toBeInTheDocument();
    expect(mocks.getOpmsTargetsPage).toHaveBeenCalledWith(expect.objectContaining({
      municipalityFinancialYearPublicId: 'financial-year-1',
      page: 1,
      pageSize: 10,
    }));

    fireEvent.click(screen.getByRole('button', { name: 'Manage' }));
    expect(screen.getByText('Definition editor for KPI-001')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'KPI fields' })).toHaveAttribute('aria-selected', 'true');

    fireEvent.click(screen.getByRole('tab', { name: 'Period ordering' }));
    expect(screen.getByText('Ordering editor for target-1')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Period targets' }));
    expect(screen.getByText('Period editor for target-1')).toBeInTheDocument();
  });

  it('reports a bounded load failure without fabricating target data', async () => {
    mocks.getOpmsTargetsPage.mockResolvedValue({ success: false, message: 'Denied by current scope' });
    render(<SdbipRevisionManagement />);

    await waitFor(() => expect(mocks.pushToast).toHaveBeenCalledWith('error', 'Denied by current scope'));
    expect(screen.getByText('No SDBIP targets are available for the active Financial Year.')).toBeInTheDocument();
  });
});
