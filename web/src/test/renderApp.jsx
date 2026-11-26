import { render } from '@testing-library/react';
import { QueryClientProvider } from '@tanstack/react-query';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { vi } from 'vitest';
import { AuthProvider } from '../app/AuthProvider.jsx';
import { createQueryClient } from '../app/queryClient.js';
import { routes } from '../app/router.jsx';

const json = (status, body, headers = {}) =>
  new Response(body === null ? null : JSON.stringify(body), {
    status,
    headers: {
      'content-type': status >= 400 ? 'application/problem+json' : 'application/json',
      ...headers,
    },
  });

export const problem = (status, code, title = code, extra = {}) =>
  json(status, { status, code, title, traceId: 'trace-123', ...extra });

export const ok = (body) => json(200, body);

export function renderApp(path, handler) {
  const fetchMock = vi.fn(async (url, init = {}) => {
    const response = await handler(String(url), init);
    if (response) return response;
    if (String(url).startsWith('/api/version')) return ok({ version: '0.1.0-test' });
    return problem(404, 'not_found');
  });
  vi.stubGlobal('fetch', fetchMock);

  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const queryClient = createQueryClient();
  const utils = render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { ...utils, router, fetchMock };
}

export const ADMIN = {
  id: 'a1',
  name: 'Ana Admin',
  email: 'ana@queryai.test',
  role: 'Admin',
  mustChangePassword: false,
};
export const ANALYST = {
  ...ADMIN,
  id: 'b2',
  name: 'Beto Analista',
  email: 'beto@queryai.test',
  role: 'Analyst',
};
