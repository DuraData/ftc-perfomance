import React, { useState } from 'react';
import { Target, Eye, EyeOff } from 'lucide-react';
import { Button } from '../ui';
import { Input } from '../common/Form';
import { useApp } from '../../context/AppContext';

export function Login() {
  const { login, setCurrentPath } = useApp();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [mfaRequired, setMfaRequired] = useState(false);
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [authenticationCode, setAuthenticationCode] = useState('');

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
      } else if (result === 'mfa_enrollment_required') {
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
            Sign in to your account
          </h2>

          {error && (
            <div className="mb-4 p-3 bg-error-50 dark:bg-error-900/20 border border-error-200 dark:border-error-800 rounded-lg">
              <p className="text-sm text-error-700 dark:text-error-400">{error}</p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
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
          </form>

        </div>

        {/* Footer */}
        <p className="text-center text-primary-200 text-sm mt-6">
          © 2025 Municipal Performance Management System
        </p>
      </div>
    </div>
  );
}
