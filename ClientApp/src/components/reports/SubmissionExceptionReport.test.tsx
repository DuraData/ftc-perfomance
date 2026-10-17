import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SubmissionExceptionReport } from './SubmissionExceptionReport';

const api = vi.hoisted(() => ({ getOpmsSubmissionsPage: vi.fn(), getIpmsSubmissionsPage: vi.fn() }));
vi.mock('../../api/api', () => api);
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <div>{children}</div> }));
vi.mock('../common/CalendarMasterPicker', () => ({
  CalendarMasterPicker: ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => <select aria-label={label} value={value} onChange={event => onChange(event.target.value)}><option value="">Select</option><option value={label === 'Financial year' ? 'year-1' : 'period-1'}>{label} option</option></select>,
}));

const row = {
  id: 'submission-1', baseState: 'SUBMITTED', quarter: 'Q1', dueDate: '2026-09-30T00:00:00Z', actual: 8,
  achievementPercent: 80, status: 'returned_for_info', attachments: [], comments: [], history: [],
  target: { id: 'target-1', indicatorNumber: 'KPI-1', targetName: 'Service delivery', submissions: [], periodTargets: [], targetUnitType: 'NUMBER' },
};

describe('SubmissionExceptionReport', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getOpmsSubmissionsPage.mockResolvedValue({ success: true, data: { items: [row], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
    api.getIpmsSubmissionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
  });

  it('loads the returned register only after a governed period is selected', async () => {
    render(<SubmissionExceptionReport mode="returned" />);
    expect(screen.getByRole('status')).toHaveTextContent(/Select a financial year and reporting period/);
    expect(api.getOpmsSubmissionsPage).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText('Financial year'), { target: { value: 'year-1' } });
    fireEvent.change(screen.getByLabelText('Reporting period'), { target: { value: 'period-1' } });

    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenCalledWith(expect.objectContaining({
      municipalityFinancialYearPublicId: 'year-1', reportingPeriodPublicId: 'period-1', dashboardFilter: 'returned', pageSize: 25,
    })));
    expect(await screen.findByText('KPI-1')).toBeInTheDocument();
    expect(screen.getByText('Returned For Info')).toBeInTheDocument();
  });

  it('requests the server-authoritative overdue filter and shows an explicit empty state', async () => {
    api.getOpmsSubmissionsPage.mockResolvedValue({ success: true, data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 } });
    render(<SubmissionExceptionReport mode="overdue" />);
    fireEvent.change(screen.getByLabelText('Financial year'), { target: { value: 'year-1' } });
    fireEvent.change(screen.getByLabelText('Reporting period'), { target: { value: 'period-1' } });

    await waitFor(() => expect(api.getOpmsSubmissionsPage).toHaveBeenCalledWith(expect.objectContaining({ dashboardFilter: 'overdue' })));
    expect(await screen.findByRole('status')).toHaveTextContent('No overdue submissions match this scope.');
  });
});
