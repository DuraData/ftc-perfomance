import React, { useState } from 'react';
import { Target, Eye, EyeOff } from 'lucide-react';
import { Button } from '../ui';
import { Input } from '../common/Form';
import { useApp } from '../../context/AppContext';

export function Login() {
  const { login, setCurrentPath } = useApp();
  const [email, setEmail] = useState('superadmin@example.com');
  const [password, setPassword] = useState('P@ssw0rd123!');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const success = await login(email, password);
      if (success) {
        setCurrentPath('/dashboard');
      } else {
        setError('Invalid email or password');
      }
    } catch (err) {
      setError('Something went wrong. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-900 via-primary-800 to-secondary-900 flex items-center justify-center p-6">
      <div className="w-full max-w-md">
        {/* Logo and Title */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-white/10 backdrop-blur-sm rounded-2xl mb-4">
            <Target className="w-10 h-10 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-white mb-2">Performance Management System</h1>
          <p className="text-primary-200">Municipal Performance Tracking Platform</p>
        </div>

        {/* Login Card */}
        <div className="bg-white dark:bg-secondary-900 rounded-2xl shadow-2xl p-8">
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
              label="Email Address"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter your email"
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
        </div>

        {/* Footer */}
        <p className="text-center text-primary-200 text-sm mt-6">
          © 2025 Municipal Performance Management System
        </p>
      </div>
    </div>
  );
}
