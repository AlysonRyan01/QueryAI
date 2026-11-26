import { http } from './httpClient.js';

export const login = (email, password) => http.post('/api/auth/login', { email, password });

export const logout = () => http.post('/api/auth/logout');

export const getMe = (signal) => http.get('/api/auth/me', { signal });

export const changePassword = (currentPassword, newPassword) =>
  http.post('/api/auth/change-password', { currentPassword, newPassword });

export const getApiVersion = (signal) => http.get('/api/version', { signal });
