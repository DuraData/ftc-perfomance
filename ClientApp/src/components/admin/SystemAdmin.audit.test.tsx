import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AdminAuditLogsPage } from './SystemAdmin';

const api = vi.hoisted(() => ({
  getLoginAuditLogs: vi.fn(),
  getAuditTrailsPage: vi.fn(),
}));

vi.mock('../../api/api', async importOriginal => ({
  ...(await importOriginal<typeof import('../../api/api')>()),
  ...api,
}));
vi.mock('../../context/AppContext', () => ({
  useApp: () => ({ permissions: ['Audit.LoginLogs.View', 'Audit.Trails.View'], pushToast: vi.fn() }),
}));
vi.mock('../layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }));

const login = {
  publicId: '22222222-2222-2222-2222-222222222222', userId: 'user-a', email: 'anna@example.test', ipAddress: '127.0.0.1', userAgent: 'test',
  success: false, failureReason: 'Account locked', loggedAt: '2026-01-03T00:00:00Z',
};
const trail = {
  publicId: '11111111-1111-1111-1111-111111111111', municipalityId: 1,
  entityName: 'OpmsSubmission', entityId: 'submission-a', action: 'Approve', changedBy: 'auditor-a',
  changedAt: '2026-01-03T00:00:00Z', correlationId: 'correlation-a',
};

describe('Audit administration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getLoginAuditLogs.mockResolvedValue({ success: true, data: { items: [login], page: 1, pageSize: 25, totalCount: 26, totalPages: 2 } });
    api.getAuditTrailsPage.mockResolvedValue({ success: true, data: { items: [trail], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 } });
  });

  it('uses authoritative server paging, search, failure filtering, and audit tabs', async () => {
    render(<AdminAuditLogsPage />);

    expect(await screen.findByText('anna@example.test')).toBeInTheDocument();
    expect(api.getLoginAuditLogs).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' }, false);
    expect(screen.getByText('26 logs')).toBeInTheDocument();

    fireEvent.click(screen.getByLabelText('Next page'));
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }), false));

    fireEvent.change(screen.getByLabelText('Search login audit logs'), { target: { value: 'locked' } });
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 1, search: 'locked' }), false));

    fireEvent.click(screen.getByLabelText('Failures only'));
    await waitFor(() => expect(api.getLoginAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ page: 1, search: 'locked' }), true));

    fireEvent.click(screen.getByRole('button', { name: 'Audit Trails' }));
    expect(await screen.findByText('OpmsSubmission')).toBeInTheDocument();
    expect(api.getAuditTrailsPage).toHaveBeenCalledWith({ page: 1, pageSize: 25, search: '', sortBy: 'createdAt', sortDirection: 'desc' });
  });
});
