import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Login } from './Login';

const app = vi.hoisted(() => ({ login: vi.fn(), setCurrentPath: vi.fn() }));
vi.mock('../../context/AppContext', () => ({ useApp: () => app }));

describe('MFA login challenge', () => {
  beforeEach(() => vi.clearAllMocks());

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
});
