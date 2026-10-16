import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { OPMSSubmissionsList } from './OPMSSubmissions';

const pushToast = vi.hoisted(() => vi.fn());
const app = vi.hoisted(() => ({ permissions: [] as string[] }));
const api = vi.hoisted(() => ({
  createOpmsSubmission: vi.fn(),
  getOpmsSubmission: vi.fn(),
  getOpmsSubmissionsPage: vi.fn(),
  getOpmsTarget: vi.fn(),
  getOpmsTargetOptions: vi.fn(),
}));

vi.mock('../../context/AppContext', () => ({ useApp: () => ({ permissions: app.permissions, pushToast }) }));
vi.mock('../../context/SecurityContext', () => ({
  useSecurity: () => ({
    canRead: () => true,
    canUpdate: () => true,
    canReadField: () => true,
    canEditField: () => true,
    canExecute: () => true,
  }),
}));
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
    Element.prototype.scrollIntoView = vi.fn();
    app.permissions = [
      'OPMS_SUBMISSION.CREATE',
      'OPMS_SUBMISSION.DueDate.READ',
      'OPMS_SUBMISSION.ActualPerformance.READ',
      'OPMS_SUBMISSION.ActualPerformance.UPDATE',
      'OPMS_SUBMISSION.Variance.READ',
    ];
    api.getOpmsSubmissionsPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
    api.getOpmsTargetOptions.mockResolvedValue({
      success: true,
      data: {
        items: [{ publicId: 'public-1', indicatorNumber: 'KPI-001', targetName: 'Road maintenance', departmentName: 'Infrastructure' }],
        page: 1,
        pageSize: 25,
        totalCount: 1,
        totalPages: 1,
      },
    });
    api.createOpmsSubmission.mockResolvedValue({ success: false, message: 'Controlled test stop' });
    api.getOpmsTarget.mockResolvedValue({
      success: true,
      data: {
        periodTargets: [{ reportingPeriodPublicId: 'period-q1', periodCode: 'Q1', periodType: 1, isActive: true }],
      },
    });
  });

  it('blocks a missing target locally and keeps actual performance optional for an in-progress draft', async () => {
    render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'New Submission' }));
    await screen.findByRole('option', { name: 'KPI-001 · Road maintenance · Infrastructure' });
    expect(screen.getByText(/Optional while the submission remains in progress/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Create' }));
    expect(await screen.findByText('Select a target before creating the submission.')).toBeInTheDocument();
    expect(screen.getByText('Select a reporting period.')).toBeInTheDocument();
    expect(api.createOpmsSubmission).not.toHaveBeenCalled();

    fireEvent.change(screen.getByRole('combobox', { name: /^Target/ }), { target: { value: 'public-1' } });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    fireEvent.change(await screen.findByRole('combobox', { name: /Reporting period/ }), { target: { value: 'period-q1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));

    await waitFor(() => expect(api.createOpmsSubmission).toHaveBeenCalledWith(expect.objectContaining({
      opmsTargetPublicId: 'public-1',
      reportingPeriodPublicId: 'period-q1',
      actualPerformance: null,
    })));
  });

  it('clears an actual value when the reporting period changes to a different configured control', async () => {
    api.getOpmsTarget.mockResolvedValue({
      success: true,
      data: {
        periodTargets: [
          {
            reportingPeriodPublicId: 'period-yes-no', periodCode: 'Q1', periodType: 1, isActive: true,
            valueConfiguration: { code: 'YES_NO', name: 'Yes or No', inputControlType: 'SELECT', valueDataType: 'BOOLEAN', supportsAutoVariance: true, requiresComponentUi: false, isQualitative: false, engineUnitKind: 9 },
          },
          {
            reportingPeriodPublicId: 'period-qualitative', periodCode: 'Q2', periodType: 2, isActive: true,
            valueConfiguration: { code: 'QUALITATIVE', name: 'Qualitative', inputControlType: 'TEXT', valueDataType: 'TEXT', supportsAutoVariance: false, requiresComponentUi: false, isQualitative: true, engineUnitKind: 13 },
          },
        ],
      },
    });

    render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'New Submission' }));
    await screen.findByRole('option', { name: 'KPI-001 · Road maintenance · Infrastructure' });
    fireEvent.change(screen.getByRole('combobox', { name: /^Target/ }), { target: { value: 'public-1' } });
    const period = await screen.findByRole('combobox', { name: /Reporting period/ });
    fireEvent.change(period, { target: { value: 'period-yes-no' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Actual Performance' }), { target: { value: 'true' } });

    fireEvent.change(period, { target: { value: 'period-qualitative' } });

    expect(screen.getByRole('textbox', { name: 'Actual Performance' })).toHaveValue('');
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

  it('loads submission details before opening the workspace so configured workflow stages are used', async () => {
    app.permissions = [...app.permissions, 'OPMS_SUBMISSION.WORKFLOW.VERIFY'];
    const registerSubmission = {
      id: 'submission-workflow',
      publicId: 'submission-workflow',
      target: {
        id: 'target-1',
        targetName: 'Road maintenance',
        indicatorNumber: 'KPI-001',
        period: { fiscalYear: '2026/27' },
        department: { name: 'Infrastructure' },
        unitOfMeasure: { name: 'Count', symbol: '' },
      },
      quarter: 'Q1' as const,
      dueDate: '',
      actualPerformance: '8',
      variance: -2,
      status: 'pending_verification' as const,
      submitterStatus: 'Submitted',
      attachments: [],
      createdAt: '2026-10-16T00:00:00Z',
    };
    api.getOpmsSubmissionsPage.mockResolvedValue({
      success: true,
      data: { items: [registerSubmission], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
    });
    api.getOpmsSubmission.mockResolvedValue({
      success: true,
      data: {
        ...registerSubmission,
        workflow: {
          stages: [
            { code: 'SUBMIT', name: 'Submit', sequence: 1, isTerminal: false },
            { code: 'VERIFY', name: 'Verify', sequence: 2, isTerminal: true },
          ],
          currentStageCode: 'VERIFY',
          completed: false,
          currentActorEligible: false,
        },
      },
    });

    render(<OPMSSubmissionsList />);
    fireEvent.click(await screen.findByRole('button', { name: 'View submission' }));

    await waitFor(() => expect(api.getOpmsSubmission).toHaveBeenCalledWith('submission-workflow'));
    expect(await screen.findByText('Submit')).toBeInTheDocument();
    expect(screen.getByText('Verify')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Verify$/ })).not.toBeInTheDocument();
  });

  it('allows a create-authorized user to start a blank draft without exposing a denied Actual Performance member', async () => {
    app.permissions = ['OPMS_SUBMISSION.CREATE'];

    render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'New Submission' }));
    expect(await screen.findByText(/Actual Performance is protected by member-level security/i)).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Actual Performance' })).not.toBeInTheDocument();
    await screen.findByRole('option', { name: 'KPI-001 · Road maintenance · Infrastructure' });
    fireEvent.change(screen.getByRole('combobox', { name: /^Target/ }), { target: { value: 'public-1' } });
    fireEvent.change(await screen.findByRole('combobox', { name: /Reporting period/ }), { target: { value: 'period-q1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));

    await waitFor(() => expect(api.createOpmsSubmission).toHaveBeenCalledWith(expect.objectContaining({
      opmsTargetPublicId: 'public-1',
      reportingPeriodPublicId: 'period-q1',
      actualPerformance: null,
    })));
  });

  it('closes an open create workspace when the effective CREATE permission is revoked', async () => {
    const { rerender } = render(<OPMSSubmissionsList />);

    fireEvent.click(await screen.findByRole('button', { name: 'New Submission' }));
    expect(screen.getByText('Create OPMS submission')).toBeInTheDocument();

    app.permissions = ['OPMS_SUBMISSION.READ'];
    rerender(<OPMSSubmissionsList />);

    expect(screen.queryByRole('button', { name: 'New Submission' })).not.toBeInTheDocument();
    expect(screen.queryByText('Create OPMS submission')).not.toBeInTheDocument();
    expect(api.createOpmsSubmission).not.toHaveBeenCalled();
  });
});
