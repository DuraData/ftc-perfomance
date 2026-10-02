import { afterEach, describe, expect, it, vi } from 'vitest';
import { getMfaStatus, getMyPermissions, isAuthenticated, login, retryNotificationDelivery } from './api';

const jsonResponse = (body: unknown, status = 200) => new Response(JSON.stringify(body), {
  status,
  headers: { 'Content-Type': 'application/json' },
});

describe('cookie-only authentication sessions', () => {
  afterEach(() => {
    sessionStorage.clear();
    vi.unstubAllGlobals();
  });

  it('marks a successful session without storing or sending an access token', async () => {
    sessionStorage.setItem('auth_token', 'legacy-readable-token');
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({
      success: true,
      data: {
        expiresAt: '2026-10-02T12:00:00Z',
        user: {},
        roles: [],
        permissions: [],
        menu: [],
        mfaEnrollmentRequired: false,
      },
    }));
    vi.stubGlobal('fetch', fetchMock);

    await login({ email: 'user@example.test', password: 'not-a-real-password' });

    const request = fetchMock.mock.calls[0][1] as RequestInit;
    expect(request.credentials).toBe('include');
    expect(request.headers).not.toHaveProperty('Authorization');
    expect(request.headers).toHaveProperty('X-OPMS-Request', 'same-origin');
    expect(sessionStorage.getItem('auth_session')).toBe('1');
    expect(sessionStorage.getItem('auth_token')).toBeNull();
    expect(isAuthenticated()).toBe(true);
  });

  it('coalesces concurrent refreshes and retries without bearer headers or token bodies', async () => {
    let protectedCalls = 0;
    const fetchMock = vi.fn().mockImplementation((input: string, init?: RequestInit) => {
      if (input.includes('/auth/refresh-token')) {
        expect(init?.body).toBeUndefined();
        return Promise.resolve(jsonResponse({ success: true, data: { expiresAt: '2026-10-02T12:00:00Z' } }));
      }
      protectedCalls += 1;
      return Promise.resolve(protectedCalls <= 2
        ? jsonResponse({ success: false, message: 'Expired' }, 401)
        : jsonResponse({ success: true, data: {} }));
    });
    vi.stubGlobal('fetch', fetchMock);

    await Promise.all([getMfaStatus(), getMfaStatus()]);

    const refreshCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/auth/refresh-token'));
    expect(refreshCalls).toHaveLength(1);
    for (const [, init] of fetchMock.mock.calls) {
      expect((init as RequestInit).credentials).toBe('include');
      expect((init as RequestInit).headers).not.toHaveProperty('Authorization');
      expect((init as RequestInit).headers).toHaveProperty('X-OPMS-Request', 'same-origin');
    }
  });

  it('reuses one idempotency key when a POST is retried after session refresh', async () => {
    let protectedCalls = 0;
    const fetchMock = vi.fn().mockImplementation((input: string) => {
      if (input.includes('/auth/refresh-token')) {
        return Promise.resolve(jsonResponse({ success: true, data: { expiresAt: '2026-10-02T12:00:00Z' } }));
      }
      protectedCalls++;
      return Promise.resolve(protectedCalls === 1
        ? jsonResponse({ success: false, message: 'Expired' }, 401)
        : jsonResponse({ success: true, data: {} }));
    });
    vi.stubGlobal('fetch', fetchMock);

    await retryNotificationDelivery({ publicId: 'event-id', rowVersion: 'AQ==' } as never, 'Operator retry');

    const protectedRequests = fetchMock.mock.calls.filter(([url]) => !String(url).includes('/auth/refresh-token'));
    expect(protectedRequests).toHaveLength(2);
    const firstHeaders = protectedRequests[0][1]?.headers as Record<string, string>;
    const retryHeaders = protectedRequests[1][1]?.headers as Record<string, string>;
    expect(firstHeaders['Idempotency-Key']).toMatch(/^[A-Za-z0-9._:-]{8,128}$/);
    expect(retryHeaders['Idempotency-Key']).toBe(firstHeaders['Idempotency-Key']);
  });

  it('normalizes RFC 7807 errors without losing code, validation, or correlation evidence', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      type: 'about:blank', title: 'Conflict', status: 409, detail: 'The record changed.',
      code: 'STATE_CONFLICT', correlationId: 'request-409', errors: { rowVersion: ['Reload the record.'] },
    }), { status: 409, headers: { 'Content-Type': 'application/problem+json', 'X-Correlation-ID': 'request-409' } }));
    vi.stubGlobal('fetch', fetchMock);

    const result = await getMyPermissions();

    expect(result).toEqual(expect.objectContaining({
      success: false, message: 'The record changed.', code: 'STATE_CONFLICT', correlationId: 'request-409',
      errors: ['Reload the record.'],
    }));
  });
});
