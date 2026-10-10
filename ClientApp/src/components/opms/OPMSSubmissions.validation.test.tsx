import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { OPMSSubmissionsList } from './OPMSSubmissions';

const pushToast = vi.hoisted(() => vi.fn());
const api = vi.hoisted(() => ({
  createOpmsSubmission: vi.fn(),
  getOpmsSubmissionsPage: vi.fn(),
  getOpmsTargetOptions: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => ({ pushToast }) }));
vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../layout/AppShell', () => ({
  AppShell: ({ children }: { children: React.ReactNode }) => <main>{children}</main>,
}));

describe('OPMS submission creation validation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getOpmsSubmissionsPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    api.getOpmsTargetOptions.mockResolvedValue({
      success: true,
      data: {
        items: [{ id: 'target-1', publicId: 'public-1', indicatorNumber: 'KPI-001', targetName: 'Road maintenance', departmentName: 'Infrastructure' }],
        page: 1,
        pageSize: 25,
        totalCount: 1,
        totalPages: 1,
      },
    });
    api.createOpmsSubmission.mockResolvedValue({ success: false, message: 'Controlled test stop' });
  });

  it('blocks a missing target locally and keeps actual performance optional for an in-progress draft', async () => {
    render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'New Submission' }));
    await screen.findByRole('option', { name: 'KPI-001 · Road maintenance · Infrastructure' });
    expect(screen.getByText(/Optional while the submission remains in progress/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Create' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Select a target before creating the submission.');
    expect(api.createOpmsSubmission).not.toHaveBeenCalled();

    fireEvent.change(screen.getByRole('combobox', { name: /^Target/ }), { target: { value: 'target-1' } });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));

    await waitFor(() => expect(api.createOpmsSubmission).toHaveBeenCalledWith(expect.objectContaining({
      opmsTargetId: 'target-1',
      quarter: 'Q1',
      actualPerformance: null,
    })));
  });

  it('renders omitted due date and actual as blank values in the register', async () => {
    api.getOpmsSubmissionsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          id: 'submission-1',
          target: { targetName: 'Road maintenance', indicatorNumber: 'KPI-001' },
          quarter: 'Q1',
          dueDate: '',
          actual: 0,
          actualPerformance: undefined,
          status: 'draft',
        }],
        page: 1,
        pageSize: 25,
        totalCount: 1,
        totalPages: 1,
      },
    });

    render(<OPMSSubmissionsList />);

    const row = await screen.findByRole('row', { name: /Road maintenance KPI-001/ });
    const cells = within(row).getAllByRole('cell');
    expect(cells[2]).toHaveTextContent('-');
    expect(cells[2]).not.toHaveTextContent('Invalid Date');
    expect(cells[3]).toHaveTextContent('-');
  });

  it('renders canonical absolute variance without a fabricated percentage suffix', async () => {
    api.getOpmsSubmissionsPage.mockResolvedValue({
      success: true,
      data: {
        items: [{
          id: 'submission-variance',
          target: { targetName: 'Road maintenance', indicatorNumber: 'KPI-001' },
          quarter: 'Q1', dueDate: '', actualPerformance: '8', variance: -2, status: 'submitted',
        }],
        page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
      },
    });

    render(<OPMSSubmissionsList />);

    const row = await screen.findByRole('row', { name: /Road maintenance KPI-001/ });
    const cells = within(row).getAllByRole('cell');
    expect(cells[4]).toHaveTextContent('-2');
    expect(cells[4]).not.toHaveTextContent('-2%');
  });
});
