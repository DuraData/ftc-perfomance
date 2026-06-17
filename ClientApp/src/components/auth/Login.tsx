import React, { useEffect, useMemo, useState } from 'react';
import { Target, Eye, EyeOff, Copy, Check } from 'lucide-react';
import { Button } from '../ui';
import { Input } from '../common/Form';
import { useApp } from '../../context/AppContext';
import { getDemoUsers } from '../../api/api';
import type { DemoUser } from '../../types';

export function Login() {
  const { login, setCurrentPath } = useApp();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [demoUsers, setDemoUsers] = useState<DemoUser[]>([]);
  const [demoLoading, setDemoLoading] = useState(true);
  const [demoError, setDemoError] = useState('');
  const [copiedUserName, setCopiedUserName] = useState('');

  useEffect(() => {
    (async () => {
      const result = await getDemoUsers();
      if (result.success && result.data) {
        setDemoUsers(result.data);
        setDemoError('');
      } else {
        setDemoError(result.message ?? 'Failed to load demo users');
      }
      setDemoLoading(false);
    })();
  }, []);

  const groupedDemoUsers = useMemo(() => {
    return demoUsers.reduce<Record<string, DemoUser[]>>((acc, user) => {
      acc[user.role] = acc[user.role] ? [...acc[user.role], user] : [user];
      return acc;
    }, {});
  }, [demoUsers]);

  const fillDemoCredentials = (user: DemoUser) => {
    setIdentifier(user.userName);
    setPassword(user.password);
    setError('');
  };

  const copyCredentials = async (user: DemoUser) => {
    const credentials = [
      `Name: ${user.fullName}`,
      `Role: ${user.role}`,
      `Department: ${user.department}`,
      `Username: ${user.userName}`,
      `Email: ${user.email}`,
      `Password: ${user.password}`,
    ].join('\n');

    try {
      await navigator.clipboard.writeText(credentials);
      setCopiedUserName(user.userName);
      window.setTimeout(() => setCopiedUserName(''), 2000);
    } catch {
      setDemoError('Unable to copy credentials');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const success = await login(identifier, password);
      if (success) {
        setCurrentPath('/dashboard');
      } else {
        setError('Invalid username, email, or password');
      }
    } catch (err) {
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
            />

            <div className="relative">
              <Input
                label="Password"
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter your password"
                required
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

            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 text-sm text-secondary-600 dark:text-secondary-400">
                <input type="checkbox" className="rounded border-secondary-300" />
                Remember me
              </label>
              <button type="button" className="text-sm text-primary-600 hover:text-primary-700">
                Forgot password?
              </button>
            </div>

            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              loading={loading}
              disabled={loading}
            >
              {loading ? 'Signing in...' : 'Sign In'}
            </Button>
          </form>

          <div className="mt-6 pt-5 border-t border-secondary-200 dark:border-secondary-800">
            <div className="flex items-start justify-between gap-4 mb-4">
              <div>
                <h3 className="text-base font-semibold text-secondary-900 dark:text-white">Demo Users</h3>
                <p className="text-sm text-secondary-600 dark:text-secondary-400">
                  Choose a demo account to auto-fill credentials, then click Sign In.
                </p>
              </div>
            </div>

            {demoError && (
              <div className="mb-4 p-3 bg-error-50 dark:bg-error-900/20 border border-error-200 dark:border-error-800 rounded-lg">
                <p className="text-sm text-error-700 dark:text-error-400">{demoError}</p>
              </div>
            )}

            {demoLoading ? (
              <p className="text-sm text-secondary-500 dark:text-secondary-400">Loading demo users...</p>
            ) : (
              <div className="max-h-72 overflow-y-auto pr-1 space-y-3">
                {Object.entries(groupedDemoUsers).map(([role, users]) => (
                  <div key={role} className="rounded-xl border border-secondary-200 dark:border-secondary-800 p-3">
                    <div className="flex items-center justify-between gap-3 mb-2">
                      <h4 className="text-sm font-semibold text-secondary-900 dark:text-white">{role}</h4>
                      <p className="text-[11px] text-secondary-500 dark:text-secondary-400">{users.length} users</p>
                    </div>
                    <div className="space-y-2">
                      {users.map((user) => (
                        <div key={user.userName} className="flex items-center gap-2">
                          <button
                            type="button"
                            onClick={() => fillDemoCredentials(user)}
                            className="flex-1 rounded-lg border border-secondary-200 dark:border-secondary-700 bg-secondary-50/60 dark:bg-secondary-800/50 px-3 py-2 text-left hover:border-primary-300 hover:bg-primary-50 dark:hover:bg-secondary-800"
                          >
                            <p className="text-sm font-medium text-secondary-900 dark:text-white">{user.fullName}</p>
                            <p className="text-[11px] text-secondary-500 dark:text-secondary-400">
                              {user.role} · {user.department}
                            </p>
                          </button>
                          <button
                            type="button"
                            onClick={() => copyCredentials(user)}
                            className="inline-flex items-center justify-center rounded-lg border border-secondary-200 dark:border-secondary-700 px-2.5 py-2 text-secondary-500 hover:bg-secondary-50 dark:hover:bg-secondary-800"
                            title={`Copy credentials for ${user.fullName}`}
                          >
                            {copiedUserName === user.userName ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <p className="text-center text-primary-200 text-sm mt-6">
          © 2025 Municipal Performance Management System
        </p>
      </div>
    </div>
  );
}
