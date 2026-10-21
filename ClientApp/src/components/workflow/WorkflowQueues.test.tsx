import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { MyWorkQueue, WorkflowQueues } from './WorkflowQueues';

const apiMocks = vi.hoisted(() => ({
  getWorkflowQueue: vi.fn(),
  getOpmsSubmission: vi.fn(),
  getIpmsSubmission: vi.fn(),
  getInternalAuditSubmission: vi.fn(),
  saveInternalAuditAssessment: vi.fn(),
  getMunicipalityFinancialYearMastersPage: vi.fn(),
}));

const appMocks = vi.hoisted(() => ({
  pushToast: vi.fn(),
  setCurrentPath: vi.fn(),
  currentPath: '/workflow/verification',
  userProfile: { id: 'submitter-live' },
}));
const securityMocks = vi.hoisted(() => ({ canReadField: vi.fn() }));
const yearPublicId = '11111111-1111-4111-8111-111111111111';

vi.mock('../../api/api', () => apiMocks);
vi.mock('../../context/AppContext', () => ({ useApp: () => appMocks }));
vi.mock('../../context/SecurityContext', () => ({ useSecurity: () => securityMocks }));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children, title }: { children: ReactNode; title: string }) => <main aria-label={title}>{children}</main> }));

describe('WorkflowQueues', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    window.history.replaceState({}, '', '/workflow/verification');
    Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    appMocks.currentPath = '/workflow/verification';
    securityMocks.canReadField.mockReturnValue(true);
    apiMocks.getMunicipalityFinancialYearMastersPage.mockResolvedValue({
      success: true,
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    });
  });

  it('renders queue counts and rows from the authorised API response', async () => {
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'verification',
        counts: {
          mySubmissions: 1, verification: 1, approval: 0, pms: 0, auditor: 0, returned: 0,
          myDrafts: 0, pendingSubmission: 0, myReturned: 0, underVerification: 1, underReview: 0,
          underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
        },
        page: {
          items: [{
            id: 'submission-live', publicId: 'submission-public', kind: 'opms', targetId: 'target-live',
            targetPublicId: 'target-public', targetName: 'Live Water KPI', indicatorNumber: 'LIVE-001',
            quarter: 'Q1', dueDate: '2026-10-15T00:00:00Z', status: 'pending_verification',
            submittedByUserPublicId: 'submitter-live', submittedByName: 'Live Submitter', createdAt: '2026-10-01T00:00:00Z',
          }],
          page: 1, pageSize: 25, totalCount: 1, totalPages: 1,
        },
      },
    });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Live Water KPI')).toBeInTheDocument();
    expect(screen.getByText('LIVE-001')).toBeInTheDocument();
    expect(screen.getByRole('main', { name: 'Verification Queue' })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Verification Queue submissions' })).toBeInTheDocument();
    expect(screen.getByText('1 authorised item')).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, defaultQueueFiltersForTest());

    apiMocks.getOpmsSubmission.mockResolvedValue({ success: false });
    fireEvent.click(screen.getByText('Live Water KPI'));
    await waitFor(() => expect(apiMocks.getOpmsSubmission).toHaveBeenCalledWith('submission-public'));
  });

  it('loads personal returned work through the bounded combined queue', async () => {
    appMocks.currentPath = '/workflow/returned-submissions';
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'my-returned',
        counts: {
          mySubmissions: 1, verification: 0, approval: 0, pms: 0, auditor: 0, returned: 4,
          myDrafts: 0, pendingSubmission: 0, myReturned: 1, underVerification: 0, underReview: 0,
          underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
        },
        page: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
      },
    });

    render(<MyWorkQueue />);

    expect(await screen.findByText('Returned Submissions')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('my-returned', 1, 25, defaultQueueFiltersForTest());
  });

  it.each([
    ['/workflow/verification', 'verification', 'Verification Queue'],
    ['/workflow/approval', 'approval', 'Approval Queue'],
    ['/workflow/pms-review', 'pms', 'PMS Review Queue'],
    ['/workflow/auditor-review', 'auditor', 'Internal Audit Queue'],
  ] as const)('binds %s to its exact governed queue and shell identity', async (path, queue, title) => {
    appMocks.currentPath = path;
    apiMocks.getWorkflowQueue.mockResolvedValue(emptyQueueResponse(queue));

    render(<WorkflowQueues />);

    expect(await screen.findByRole('main', { name: title })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: `${title} submissions` })).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith(queue, 1, 25, defaultQueueFiltersForTest());
  });

  it.each([
    ['/workflow/my-queue', 'my-submissions', 'My Queue'],
    ['/workflow/my-drafts', 'my-drafts', 'My Drafts'],
    ['/workflow/pending-submission', 'pending-submission', 'Pending Submission'],
    ['/workflow/returned-submissions', 'my-returned', 'Returned Submissions'],
    ['/workflow/under-verification', 'under-verification', 'Under Verification'],
    ['/workflow/under-review', 'under-review', 'Under Review'],
    ['/workflow/under-approval', 'under-approval', 'Under Approval'],
    ['/workflow/internal-audit-returned', 'internal-audit-returned', 'Internal Audit Returned'],
    ['/workflow/approved-closed', 'approved-closed', 'Approved / Closed'],
  ] as const)('binds %s to its exact personal queue and shell identity', async (path, queue, title) => {
    appMocks.currentPath = path;
    apiMocks.getWorkflowQueue.mockResolvedValue(emptyQueueResponse(queue));

    render(<MyWorkQueue />);

    expect(await screen.findByRole('main', { name: title })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: `${title} submissions` })).toBeInTheDocument();
    expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith(queue, 1, 25, defaultQueueFiltersForTest());
  });

  it('rebinds the governed queue when navigation changes between workflow routes', async () => {
    apiMocks.getWorkflowQueue.mockImplementation((queue: string) => Promise.resolve(emptyQueueResponse(queue)));
    const { rerender } = render(<WorkflowQueues />);
    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, defaultQueueFiltersForTest()));

    appMocks.currentPath = '/workflow/approval';
    rerender(<WorkflowQueues />);

    expect(await screen.findByRole('main', { name: 'Approval Queue' })).toBeInTheDocument();
    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('approval', 1, 25, defaultQueueFiltersForTest()));
    expect(screen.getByRole('table', { name: 'Approval Queue submissions' })).toBeInTheDocument();
    expect(screen.queryByRole('table', { name: 'Verification Queue submissions' })).not.toBeInTheDocument();
  });

  it('opens an OPMS queue record at the governed KPI workflow and reporting-period deep link', async () => {
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'verification', counts: { ...emptyCountsForTest(), verification: 1 },
        page: { items: [{
          id: 'submission-live', publicId: 'submission-public', kind: 'opms', targetId: 'target-live',
          targetPublicId: 'target-public', targetName: 'Live Water KPI', indicatorNumber: 'LIVE-001',
          quarter: 'Q1', status: 'pending_verification', createdAt: '2026-10-01T00:00:00Z',
        }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
      },
    });
    apiMocks.getOpmsSubmission.mockResolvedValue({
      success: true,
      data: {
        id: 'submission-public', reportingPeriodPublicId: 'period-public', quarter: 'Q1', status: 'pending_verification',
        dueDate: '2026-10-15T00:00:00Z', actualPerformance: '42', variance: -2, actualExpenditure: 10,
        target: { id: 'target-public', targetName: 'Live Water KPI' },
      },
    });

    render(<WorkflowQueues />);
    fireEvent.click(await screen.findByText('Live Water KPI'));
    fireEvent.click(await screen.findByRole('button', { name: 'Open KPI Workspace' }));

    expect(appMocks.setCurrentPath).toHaveBeenCalledWith('/opms/targets/target-public?tab=workflow&period=period-public');
  });

  it('shows an explicit service error instead of fixture submissions', async () => {
    apiMocks.getWorkflowQueue.mockResolvedValue({ success: false, message: 'Authorised queue request failed.' });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Workflow queue unavailable')).toBeInTheDocument();
    expect(screen.getByText('Authorised queue request failed.')).toBeInTheDocument();
    expect(appMocks.pushToast).toHaveBeenCalledWith('error', 'Authorised queue request failed.');
  });

  it('does not render a hostile queue actor payload without the submission member grant', async () => {
    securityMocks.canReadField.mockReturnValue(false);
    apiMocks.getWorkflowQueue.mockResolvedValue({
      success: true,
      data: {
        queue: 'verification', counts: { ...emptyCountsForTest(), verification: 1 },
        page: { items: [{
          id: 'secret-submission', publicId: 'secret-public', kind: 'opms', targetId: 'target-secret',
          targetPublicId: 'target-public', targetName: 'Protected KPI', indicatorNumber: 'SEC-1', quarter: 'Q1',
          status: 'pending_verification', submittedByUserPublicId: 'actor-public', submittedByName: 'Secret Queue Actor',
          createdAt: '2026-10-01T00:00:00Z',
        }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 },
      },
    });

    render(<WorkflowQueues />);

    expect(await screen.findByText('Protected KPI')).toBeInTheDocument();
    expect(screen.queryByText('Secret Queue Actor')).not.toBeInTheDocument();
    expect(screen.getByText('Restricted')).toBeInTheDocument();
    expect(securityMocks.canReadField).toHaveBeenCalledWith('OPMS_SUBMISSION', 'SubmitterIdentity');
  });

  it('restores server-side queue filters for the session and updates them without client-side filtering', async () => {
    sessionStorage.setItem('workflow-queue-filters:verification', JSON.stringify({
      search: 'Water', municipalityFinancialYearPublicId: yearPublicId, kind: 'opms', period: 'Q2', status: 'pending_verification',
      sortBy: 'dueDate', sortDirection: 'asc',
    }));
    apiMocks.getWorkflowQueue.mockResolvedValue(emptyQueueResponse('verification'));

    render(<WorkflowQueues />);

    expect(await screen.findByLabelText('Search KPI or indicator')).toHaveValue('Water');
    expect(screen.getByLabelText('Framework')).toHaveValue('opms');
    expect(screen.getByLabelText('Period')).toHaveValue('Q2');
    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, {
      search: 'Water', municipalityFinancialYearPublicId: yearPublicId, kind: 'opms', period: 'Q2', status: 'pending_verification',
      sortBy: 'dueDate', sortDirection: 'asc',
    }));

    fireEvent.change(screen.getByLabelText('Search KPI or indicator'), { target: { value: 'Roads' } });

    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, {
      search: 'Roads', municipalityFinancialYearPublicId: yearPublicId, kind: 'opms', period: 'Q2', status: 'pending_verification',
      sortBy: 'dueDate', sortDirection: 'asc',
    }));
    expect(JSON.parse(sessionStorage.getItem('workflow-queue-filters:verification') ?? '{}')).toMatchObject({ search: 'Roads', municipalityFinancialYearPublicId: yearPublicId, kind: 'opms', period: 'Q2' });
  });

  it('uses the dashboard financial-year and framework deep link as the authoritative initial queue filter', async () => {
    sessionStorage.setItem('workflow-queue-filters:verification', JSON.stringify({
      ...defaultQueueFiltersForTest(), municipalityFinancialYearPublicId: '22222222-2222-4222-8222-222222222222', kind: 'ipms',
    }));
    window.history.replaceState({}, '', `/workflow/verification?municipalityFinancialYearPublicId=${yearPublicId}&kind=opms`);
    apiMocks.getWorkflowQueue.mockResolvedValue(emptyQueueResponse('verification'));

    render(<WorkflowQueues />);

    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, {
      ...defaultQueueFiltersForTest(), municipalityFinancialYearPublicId: yearPublicId, kind: 'opms',
    }));
    expect(screen.getByLabelText('Financial year')).toHaveValue(yearPublicId);
    expect(screen.getByLabelText('Framework')).toHaveValue('opms');
  });

  it('discards invalid session filter values before calling the queue API', async () => {
    sessionStorage.setItem('workflow-queue-filters:verification', JSON.stringify({
      search: 'x'.repeat(250), municipalityFinancialYearPublicId: 'not-a-public-id', kind: 'outside', period: 'Q9',
      status: 'invented', sortBy: 'raw-sql', sortDirection: 'sideways',
    }));
    apiMocks.getWorkflowQueue.mockResolvedValue(emptyQueueResponse('verification'));

    render(<WorkflowQueues />);

    await waitFor(() => expect(apiMocks.getWorkflowQueue).toHaveBeenCalledWith('verification', 1, 25, {
      ...defaultQueueFiltersForTest(), search: 'x'.repeat(200),
    }));
  });
});

function defaultQueueFiltersForTest() {
  return { search: '', municipalityFinancialYearPublicId: '', kind: 'all', period: '', status: 'all', sortBy: 'createdAt', sortDirection: 'desc' };
}

function emptyCountsForTest() {
  return {
    mySubmissions: 0, verification: 0, approval: 0, pms: 0, auditor: 0, returned: 0,
    myDrafts: 0, pendingSubmission: 0, myReturned: 0, underVerification: 0, underReview: 0,
    underApproval: 0, internalAuditReturned: 0, approvedClosed: 0,
  };
}

function emptyQueueResponse(queue: string) {
  return {
    success: true,
    data: {
      queue,
      counts: emptyCountsForTest(),
      page: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
    },
  };
}
