import { createContext, useCallback, useContext, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import * as authApi from '../api/auth.js';

export const ME_QUERY_KEY = ['me'];

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const queryClient = useQueryClient();
  const meQuery = useQuery({
    queryKey: ME_QUERY_KEY,
    queryFn: async ({ signal }) => {
      try {
        return await authApi.getMe(signal);
      } catch (error) {
        if (error?.status === 401) return null;
        throw error;
      }
    },
    staleTime: 60_000,
    retry: false,
  });

  const login = useCallback(
    async (email, password) => {
      const user = await authApi.login(email, password);
      queryClient.setQueryData(ME_QUERY_KEY, user);
      return user;
    },
    [queryClient],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      queryClient.clear();
      queryClient.setQueryData(ME_QUERY_KEY, null);
    }
  }, [queryClient]);

  const refresh = useCallback(
    () => queryClient.invalidateQueries({ queryKey: ME_QUERY_KEY }),
    [queryClient],
  );

  const value = useMemo(
    () => ({
      user: meQuery.data ?? null,
      isLoading: meQuery.isPending,
      error: meQuery.error,
      login,
      logout,
      refresh,
    }),
    [meQuery.data, meQuery.isPending, meQuery.error, login, logout, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>');
  return context;
}
