import React, { useEffect, useState } from 'react';
import { Target, Eye, EyeOff, Building2 } from 'lucide-react';
import { Button } from '../ui';
import { Input } from '../common/Form';
import { useApp } from '../../context/AppContext';
import { enterpriseSignInUrl, getEnterpriseSignInOptions, requestPasswordReset, resetPassword } from '../../api/api';
import type { EnterpriseSignInOptions } from '../../types';

type AuthMode = 'login' | 'forgot' | 'reset';

export function Login() {
  const { login, resumeEnterpriseLogin, setCurrentPath } = useApp();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [mfaRequired, setMfaRequired] = useState(false);
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [authenticationCode, setAuthenticationCode] = useState('');
  const resetParameters = new URLSearchParams(window.location.hash.replace(/^#/, ''));
  const [mode, setMode] = useState<AuthMode>(window.location.pathname === '/reset-password' ? 'reset' : 'login');
  const [resetEmail, setResetEmail] = useState(resetParameters.get('email') ?? '');
  const [resetToken] = useState(resetParameters.get('token') ?? '');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [notice, setNotice] = useState('');
  const [municipalityCode, setMunicipalityCode] = useState('');
  const [enterpriseOptions, setEnterpriseOptions] = useState<EnterpriseSignInOptions | null>(null);

  useEffect(() => {
    const enterpriseStatus = new URLSearchParams(window.location.search).get('enterprise');
    if (enterpriseStatus !== 'complete') {
      if (enterpriseStatus === 'failed') setError('Enterprise sign-in could not be completed. Contact your administrator if the account should be linked.');
      return;
    }
    setLoading(true);
    void resumeEnterpriseLogin().then(result => {
      if (result === 'failed') setError('Enterprise sign-in could not be completed.');
    }).finally(() => setLoading(false));
  }, [resumeEnterpriseLogin]);

  const discoverEnterpriseOptions = async () => {
    setError(''); setEnterpriseOptions(null); setLoading(true);
    try {
      const result = await getEnterpriseSignInOptions(municipalityCode);
      if (result.success && result.data) setEnterpriseOptions(result.data);
      else setError(result.message ?? 'No sign-in configuration was found for that municipality.');
    } catch { setError('Sign-in options could not be loaded.'); }
    finally { setLoading(false); }
  };

  const returnToLogin = () => {
    window.history.replaceState({}, '', '/login');
    setMode('login');
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const result = await login(
        identifier,
        password,
        mfaRequired && !useRecoveryCode ? authenticationCode : undefined,
        mfaRequired && useRecoveryCode ? authenticationCode : undefined,
      );
      if (result === 'success') {
        setCurrentPath('/dashboard');
      } else if (result === 'mfa_enrollment_required' || result === 'password_change_required') {
        setCurrentPath('/settings');
      } else if (result === 'mfa_required') {
        setMfaRequired(true);
        setAuthenticationCode('');
      } else {
        setError(mfaRequired ? 'Invalid authentication code' : 'Invalid username, email, or password');
      }
    } catch {
      setError('Something went wrong. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleForgotPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setNotice('');
    setLoading(true);
    try {
      const result = await requestPasswordReset(resetEmail);
      if (result.success) setNotice(result.message ?? 'If an active account matches that email address, password reset instructions will be sent.');
      else setError('The request could not be completed. Please try again later.');
    } catch {
      setError('The request could not be completed. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setNotice('');
    if (!resetToken) { setError('The password reset link is invalid or incomplete.'); return; }
    if (newPassword !== confirmPassword) { setError('The new passwords do not match.'); return; }
    setLoading(true);
    try {
      const result = await resetPassword(resetEmail, resetToken, newPassword);
      if (result.success) {
        returnToLogin();
        setNotice(result.message ?? 'Password reset completed. Sign in with the new password.');
        setNewPassword('');
        setConfirmPassword('');
      } else {
        setError(result.errors?.join(' ') || result.message || 'The password reset link is invalid or has expired.');
      }
    } catch {
      setError('The password reset could not be completed. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-900 via-primary-800 to-secondary-900 flex items-center justify-center p-6">
      <div className="w-full max-w-xl">
        {/* Logo and Title */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-white/10 backdrop-blur-sm rounded-2xl mb-4">
            <Target className="w-10 h-10 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-white mb-2">Performance Management System</h1>
          <p className="text-primary-200">Municipal Performance Tracking Platform</p>
        </div>

        {/* Login Card */}
        <div className="bg-white dark:bg-secondary-900 rounded-2xl shadow-2xl p-6 md:p-8">
          <h2 className="text-xl font-semibold text-secondary-900 dark:text-white text-center mb-6">
            {mode === 'login' ? 'Sign in to your account' : mode === 'forgot' ? 'Reset your password' : 'Choose a new password'}
          </h2>

          {error && (
            <div className="mb-4 p-3 bg-error-50 dark:bg-error-900/20 border border-error-200 dark:border-error-800 rounded-lg">
              <p className="text-sm text-error-700 dark:text-error-400">{error}</p>
            </div>
          )}

          {notice && (
            <div className="mb-4 p-3 bg-success-50 dark:bg-success-900/20 border border-success-200 dark:border-success-800 rounded-lg">
              <p className="text-sm text-success-700 dark:text-success-400">{notice}</p>
            </div>
          )}

          {mode === 'login' && <form onSubmit={handleSubmit} className="space-y-4">
            <div className="rounded-lg border border-secondary-200 dark:border-secondary-700 p-3 space-y-3">
              <Input label="Municipality code" type="text" value={municipalityCode} onChange={(event) => { setMunicipalityCode(event.target.value); setEnterpriseOptions(null); }} placeholder="For example: CPT" />
              <Button type="button" variant="secondary" className="w-full" disabled={loading || municipalityCode.trim().length < 2} onClick={discoverEnterpriseOptions}>
                <Building2 className="w-4 h-4 mr-2" /> Find sign-in options
              </Button>
              {enterpriseOptions?.providers.map(provider => (
                <Button key={provider.code} type="button" variant="primary" className="w-full" onClick={() => window.location.assign(enterpriseSignInUrl(enterpriseOptions.municipalityCode, provider.code))}>
                  Sign in with {provider.displayName}
                </Button>
              ))}
              {enterpriseOptions && enterpriseOptions.providers.length === 0 ? <p className="text-sm text-secondary-500">No enterprise provider is enabled for this municipality.</p> : null}
            </div>

            {enterpriseOptions?.localEnabled === false ? <p className="text-sm text-secondary-600 dark:text-secondary-300">Local password sign-in is disabled for {enterpriseOptions.municipalityName}.</p> : null}
            {enterpriseOptions?.localEnabled !== false ? <>
            <Input
              label="Username or Email"
              type="text"
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              placeholder="Enter your username or email"
              required
              disabled={mfaRequired}
            />

            <div className="relative">
              <Input
                label="Password"
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                required
                disabled={mfaRequired}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="text-secondary-400 hover:text-secondary-600"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
              />
            </div>

            {mfaRequired && <div className="space-y-2">
              <Input
                label={useRecoveryCode ? 'Recovery code' : 'Authenticator code'}
                type="text"
                value={authenticationCode}
                onChange={(e) => setAuthenticationCode(e.target.value)}
                placeholder={useRecoveryCode ? 'Enter a recovery code' : 'Enter the 6-digit code'}
                required
              />
              <button type="button" className="text-sm text-primary-600 hover:text-primary-700" onClick={() => { setUseRecoveryCode(!useRecoveryCode); setAuthenticationCode(''); }}>
                {useRecoveryCode ? 'Use authenticator code' : 'Use a recovery code'}
              </button>
            </div>}

            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              loading={loading}
              disabled={loading}
            >
              {loading ? 'Signing in...' : mfaRequired ? 'Verify and sign in' : 'Sign In'}
            </Button>
            {!mfaRequired && <button type="button" className="w-full text-sm text-primary-600 hover:text-primary-700" onClick={() => { setResetEmail(identifier); setMode('forgot'); setError(''); setNotice(''); }}>
              Forgot your password?
            </button>}
            </> : null}
          </form>}

          {mode === 'forgot' && <form onSubmit={handleForgotPassword} className="space-y-4">
            <p className="text-sm text-secondary-600 dark:text-secondary-300">Enter your account email. The response is identical whether or not an account exists.</p>
            <Input label="Email address" type="email" value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} required />
            <Button type="submit" variant="primary" size="lg" className="w-full" loading={loading} disabled={loading}>Send reset instructions</Button>
            <button type="button" className="w-full text-sm text-primary-600 hover:text-primary-700" onClick={returnToLogin}>Back to sign in</button>
          </form>}

          {mode === 'reset' && <form onSubmit={handleResetPassword} className="space-y-4">
            <Input label="Email address" type="email" value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} required />
            <Input label="New password" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required />
            <Input label="Confirm new password" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
            <p className="text-xs text-secondary-500">Use at least 12 characters with uppercase, lowercase, number, symbol, and at least four distinct characters. Common or breached passwords are rejected.</p>
            <Button type="submit" variant="primary" size="lg" className="w-full" loading={loading} disabled={loading}>Reset password</Button>
            <button type="button" className="w-full text-sm text-primary-600 hover:text-primary-700" onClick={returnToLogin}>Back to sign in</button>
          </form>}

        </div>

        {/* Footer */}
        <p className="text-center text-primary-200 text-sm mt-6">
          © 2025 Municipal Performance Management System
        </p>
      </div>
    </div>
  );
}
