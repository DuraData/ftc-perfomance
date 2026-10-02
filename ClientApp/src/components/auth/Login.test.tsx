import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Login } from './Login';

const app = vi.hoisted(() => ({ login: vi.fn(), resumeEnterpriseLogin: vi.fn(), setCurrentPath: vi.fn() }));
const authApi = vi.hoisted(() => ({ requestPasswordReset: vi.fn(), resetPassword: vi.fn(), getEnterpriseSignInOptions: vi.fn(), enterpriseSignInUrl: vi.fn() }));
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));
vi.mock('../../api/api', () => authApi);

describe('MFA login challenge', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.history.replaceState({}, '', '/login');
    app.resumeEnterpriseLogin.mockResolvedValue('success');
  });

  it('discovers municipality authentication modes without exposing provider secrets', async () => {
    authApi.getEnterpriseSignInOptions.mockResolvedValue({ success: true, data: { municipalityCode: 'M1', municipalityName: 'Metro One', localEnabled: false, providers: [{ code: 'ENTRA', displayName: 'Work account', kind: 'MICROSOFT_ENTRA_ID' }] } });
    render(<Login />);
    fireEvent.change(screen.getByLabelText(/Municipality code/), { target: { value: 'M1' } });
    fireEvent.click(screen.getByRole('button', { name: /Find sign-in options/i }));
    expect(await screen.findByRole('button', { name: /Sign in with Work account/i })).toBeInTheDocument();
    expect(screen.getByText(/Local password sign-in is disabled/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/Username or Email/)).not.toBeInTheDocument();
  });

  it('requests and submits an authenticator code after password validation', async () => {
    app.login.mockResolvedValueOnce('mfa_required').mockResolvedValueOnce('success');
    render(<Login />);
    fireEvent.change(screen.getByLabelText(/Username or Email/), { target: { value: 'admin@example.test' } });
    fireEvent.change(screen.getByLabelText(/Password/), { target: { value: 'Password1!' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In' }));
    const code = await screen.findByLabelText(/Authenticator code/);
    fireEvent.change(code, { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Verify and sign in' }));
    await waitFor(() => expect(app.login).toHaveBeenLastCalledWith('admin@example.test', 'Password1!', '123456', undefined));
    expect(app.setCurrentPath).toHaveBeenCalledWith('/dashboard');
  });

  it('uses an enumeration-safe forgot-password flow', async () => {
    authApi.requestPasswordReset.mockResolvedValue({ success: true, data: true, message: 'If an active account matches that email address, password reset instructions will be sent.' });
    render(<Login />);
    fireEvent.click(screen.getByRole('button', { name: /Forgot your password/i }));
    fireEvent.change(screen.getByLabelText(/Email address/), { target: { value: 'person@example.test' } });
    fireEvent.click(screen.getByRole('button', { name: /Send reset instructions/i }));
    await waitFor(() => expect(authApi.requestPasswordReset).toHaveBeenCalledWith('person@example.test'));
    expect(screen.getByText(/If an active account matches/i)).toBeInTheDocument();
  });

  it('submits a reset-link token and returns to sign in after success', async () => {
    window.history.replaceState({}, '', '/reset-password#email=person%40example.test&token=one-time-token');
    authApi.resetPassword.mockResolvedValue({ success: true, data: true, message: 'Password reset completed. Sign in with the new password.' });
    render(<Login />);
    fireEvent.change(screen.getByLabelText(/^New password/), { target: { value: 'A-Strong-New-Password9!' } });
    fireEvent.change(screen.getByLabelText(/Confirm new password/), { target: { value: 'A-Strong-New-Password9!' } });
    fireEvent.click(screen.getByRole('button', { name: /^Reset password$/i }));
    await waitFor(() => expect(authApi.resetPassword).toHaveBeenCalledWith('person@example.test', 'one-time-token', 'A-Strong-New-Password9!'));
    expect(window.location.pathname).toBe('/login');
    expect(screen.getByText(/Password reset completed/i)).toBeInTheDocument();
  });
});
