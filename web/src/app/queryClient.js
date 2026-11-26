import { QueryCache, QueryClient } from '@tanstack/react-query';

export const ME_KEY = ['me'];

export function createQueryClient() {
  const client = new QueryClient({
    queryCache: new QueryCache({
      onError: (error, query) => {
        if (error?.status === 401 && query.queryKey[0] !== ME_KEY[0]) {
          client.setQueryData(ME_KEY, null);
        }
      },
    }),
    defaultOptions: {
      queries: {
        retry: (count, error) => error?.status >= 500 && count < 2,
        refetchOnWindowFocus: false,
      },
    },
  });
  return client;
}
