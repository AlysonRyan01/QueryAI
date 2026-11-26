import { http } from './httpClient.js';

export function listUsers({ search = '', page = 1, pageSize = 20 } = {}, signal) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search.trim()) params.set('search', search.trim());
  return http.get(`/api/users?${params}`, { signal });
}

export const getUser = (id, signal) => http.get(`/api/users/${id}`, { signal });

export const createUser = (data) => http.post('/api/users', data);

export const updateUser = (id, data) => http.put(`/api/users/${id}`, data);

export const resetPassword = (id, temporaryPassword) =>
  http.post(`/api/users/${id}/reset-password`, { temporaryPassword });

export const deactivateUser = (id) => http.post(`/api/users/${id}/deactivate`);

export const activateUser = (id) => http.post(`/api/users/${id}/activate`);
