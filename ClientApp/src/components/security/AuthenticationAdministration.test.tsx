import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AuthenticationAdministrationPage } from './AuthenticationAdministration';

const api = vi.hoisted(() => ({
  getAuthenticationConfiguration: vi.fn(), getAuthenticationEvents: vi.fn(), getAuthenticationProviders: vi.fn(),
  getSecurityUsers: vi.fn(), getUserAuthenticators: vi.fn(), provisionUserAuthenticator: vi.fn(),
  saveAuthenticationConfiguration: vi.fn(), setUserAuthenticatorStatus: vi.fn(),
}));
vi.mock('../../api/api', () => api);

describe('AuthenticationAdministrationPage', () => {
  beforeEach(() => {
    api.getAuthenticationConfiguration.mockResolvedValue({ success: true, data: {
      publicId: 'configuration-1', mode: 1, providerRegistrationCode: null, displayName: 'Local sign-in',
      isActive: true, effectiveFrom: '2026-10-01T08:00:00Z', effectiveTo: null, rowVersion: 'AQ==',
      policy: {
        publicId: 'policy-1', minimumPasswordLength: 14, maximumFailedAttempts: 4, lockoutMinutes: 30,
        requireMfaForPrivilegedLocalUsers: true, requireMfaForAllLocalUsers: false,
        requireFirstLoginPasswordChange: true, sessionIdleTimeoutMinutes: 20,
        sessionAbsoluteTimeoutHours: 12, maximumConcurrentSessions: 3, rowVersion: 'Ag==',
      },
    } });
    api.getAuthenticationEvents.mockResolvedValue({ success: true, data: [] });
    api.getAuthenticationProviders.mockResolvedValue({ success: true, data: [] });
    api.getSecurityUsers.mockResolvedValue({ success: true, data: [] });
    api.getUserAuthenticators.mockResolvedValue({ success: true, data: [] });
    api.saveAuthenticationConfiguration.mockResolvedValue({ success: true, data: {} });
  });

  it('loads and saves editable municipality policy values', async () => {
    render(<AuthenticationAdministrationPage />);
    const passwordLength = await screen.findByLabelText('Minimum password length');
    expect(passwordLength).toHaveValue(14);
    fireEvent.change(passwordLength, { target: { value: '18' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'MFA for all local users' }));
    fireEvent.change(screen.getByLabelText(/Governance reason/, { selector: '#authentication-policy-reason' }), { target: { value: 'Approved security policy update' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save configuration' }));

    await waitFor(() => expect(api.saveAuthenticationConfiguration).toHaveBeenCalledWith(expect.objectContaining({
      reason: 'Approved security policy update',
      policy: expect.objectContaining({ minimumPasswordLength: 18, requireMfaForAllLocalUsers: true, rowVersion: 'Ag==' }),
    })));
  });
});
