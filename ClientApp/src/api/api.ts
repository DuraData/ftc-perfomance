import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
  RegisterRequest,
  AdminUserDetail,
  AdminRole,
  AdminPermission,
  AdminPermissionGroup,
  RolePermission,
  UserPermissions,
  UserPermissionOverride,
  MenuItem,
  LoginAuditLog,
  DemoUser,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

let accessToken: string | null = localStorage.getItem('auth_token');
let refreshToken: string | null = localStorage.getItem('refresh_token');

function setTokens(access: string, refresh: string) {
  accessToken = access;
  refreshToken = refresh;
  localStorage.setItem('auth_token', access);
  localStorage.setItem('refresh_token', refresh);
}

function clearTokens() {
  accessToken = null;
  refreshToken = null;
  localStorage.removeItem('auth_token');
  localStorage.removeItem('refresh_token');
}

async function fetchApi<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };

  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && refreshToken) {
    try {
      const refreshResult = await refreshAccessToken();
      if (refreshResult.success) {
        // Retry the original request with new token
        const retryHeaders: Record<string, string> = {
          'Content-Type': 'application/json',
          ...(options.headers as Record<string, string>),
          'Authorization': `Bearer ${accessToken}`,
        };
        const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
          ...options,
          headers: retryHeaders,
        });
        return await retryResponse.json();
      }
    } catch (e) {
      clearTokens();
      window.location.href = '/login';
    }
  }

  return await response.json();
}

async function get<T>(endpoint: string): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'GET' });
}

async function post<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function put<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function patch<T>(endpoint: string, body?: unknown): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'PATCH', body: body === undefined ? undefined : JSON.stringify(body) });
}

async function del<T>(endpoint: string): Promise<ApiResponse<T>> {
  return fetchApi<T>(endpoint, { method: 'DELETE' });
}

async function refreshAccessToken(): Promise<ApiResponse<LoginResponse>> {
  const response = await fetch(`${API_BASE_URL}/auth/refresh-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ accessToken, refreshToken } as RefreshTokenRequest),
  });
  const data = await response.json();
  if (data.success && data.data) {
    setTokens(data.data.accessToken, data.data.refreshToken);
  }
  return data;
}

export async function login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  const result = await fetchApi<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  });
  if (result.success && result.data) {
    setTokens(result.data.accessToken, result.data.refreshToken);
  }
  return result;
}

export async function register(data: RegisterRequest): Promise<ApiResponse<boolean>> {
  return await fetchApi<boolean>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function getDemoUsers(): Promise<ApiResponse<DemoUser[]>> {
  return get<DemoUser[]>('/auth/demo-users');
}

export async function logout() {
  clearTokens();
}

export function isAuthenticated() {
  return !!accessToken;
}

export { setTokens, clearTokens };

export async function getMyMenu(): Promise<ApiResponse<MenuItem[]>> {
  return get<MenuItem[]>('/navigation/my-menu');
}

export async function getMyPermissions(): Promise<ApiResponse<string[]>> {
  return get<string[]>('/access/my-permissions');
}

export async function getUsers(): Promise<ApiResponse<AdminUserDetail[]>> {
  return get<AdminUserDetail[]>('/users');
}

export async function getUser(id: string): Promise<ApiResponse<AdminUserDetail>> {
  return get<AdminUserDetail>(`/users/${id}`);
}

export async function createUser(payload: { firstName: string; lastName: string; email: string; password: string; phoneNumber?: string }): Promise<ApiResponse<AdminUserDetail>> {
  return post<AdminUserDetail>('/users', payload);
}

export async function updateUser(payload: { id: string; firstName: string; lastName: string; phoneNumber?: string; isActive: boolean }): Promise<ApiResponse<AdminUserDetail>> {
  return put<AdminUserDetail>(`/users/${payload.id}`, {
    firstName: payload.firstName,
    lastName: payload.lastName,
    phoneNumber: payload.phoneNumber ?? null,
    isActive: payload.isActive,
  });
}

export async function activateUser(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/users/${id}/activate`);
}

export async function deactivateUser(id: string): Promise<ApiResponse<boolean>> {
  return patch<boolean>(`/users/${id}/deactivate`);
}

export async function deleteUser(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/users/${id}`);
}

export async function setUserRoles(userId: string, roleIds: string[]): Promise<ApiResponse<boolean>> {
  return post<boolean>(`/users/${userId}/roles`, { roleIds });
}

export async function getUserPermissions(userId: string): Promise<ApiResponse<UserPermissions>> {
  return get<UserPermissions>(`/users/${userId}/permissions`);
}

export async function setUserPermissionOverrides(userId: string, overrides: UserPermissionOverride[]): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/users/${userId}/permission-overrides`, { overrides });
}

export async function getRoles(): Promise<ApiResponse<AdminRole[]>> {
  return get<AdminRole[]>('/roles');
}

export async function createRole(payload: { name: string; description?: string }): Promise<ApiResponse<AdminRole>> {
  return post<AdminRole>('/roles', payload);
}

export async function updateRole(payload: { id: string; name: string; description?: string }): Promise<ApiResponse<AdminRole>> {
  return put<AdminRole>(`/roles/${payload.id}`, { name: payload.name, description: payload.description ?? null });
}

export async function deleteRole(id: string): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/roles/${id}`);
}

export async function getRolePermissions(roleId: string): Promise<ApiResponse<RolePermission[]>> {
  return get<RolePermission[]>(`/roles/${roleId}/permissions`);
}

export async function setRolePermissions(roleId: string, permissionIds: number[]): Promise<ApiResponse<boolean>> {
  return put<boolean>(`/roles/${roleId}/permissions`, { permissionIds });
}

export async function getPermissions(): Promise<ApiResponse<AdminPermission[]>> {
  return get<AdminPermission[]>('/permissions');
}

export async function getPermissionsGrouped(): Promise<ApiResponse<AdminPermissionGroup[]>> {
  return get<AdminPermissionGroup[]>('/permissions/grouped');
}

export async function createPermission(payload: Omit<AdminPermission, 'id'>): Promise<ApiResponse<AdminPermission>> {
  return post<AdminPermission>('/permissions', payload);
}

export async function updatePermission(payload: AdminPermission): Promise<ApiResponse<AdminPermission>> {
  const { id, ...rest } = payload;
  return put<AdminPermission>(`/permissions/${id}`, rest);
}

export async function deletePermission(id: number): Promise<ApiResponse<boolean>> {
  return del<boolean>(`/permissions/${id}`);
}

export async function getLoginAuditLogs(take = 200): Promise<ApiResponse<LoginAuditLog[]>> {
  return get<LoginAuditLog[]>(`/audit/login-logs?take=${take}`);
}
