import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { ADMIN, ANALYST, ok, problem, renderApp } from '../test/renderApp.jsx';

const meIs = (me) => (url) => {
  if (url === '/api/auth/me') return me ? ok(me) : problem(401, 'unauthorized');
  if (url.startsWith('/api/users')) return ok({ items: [], total: 0, page: 1, pageSize: 20 });
  return undefined;
};

describe('route guards', () => {
  it('redirects anonymous users to /login keeping the target', async () => {
    const { router } = renderApp('/historico', meIs(null));

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'));
    expect(router.state.location.search).toBe('?redirect=%2Fhistorico');
  });

  it('forces /trocar-senha while the password change is pending', async () => {
    const { router } = renderApp('/conexoes', meIs({ ...ADMIN, mustChangePassword: true }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/trocar-senha'));
  });

  it('shows the layout with all areas for an admin', async () => {
    renderApp('/', meIs(ADMIN));

    const nav = await screen.findByRole('navigation', { name: 'Navegação principal' });
    for (const label of ['Início', 'Conexões', 'Chat', 'Histórico', 'Configurações']) {
      expect(nav).toHaveTextContent(label);
    }
    expect(screen.getByText('Suas consultas recentes aparecerão aqui')).toBeInTheDocument();
  });

  it('hides Configurações from analysts and denies direct access', async () => {
    renderApp('/configuracoes/usuarios', meIs(ANALYST));

    expect(await screen.findByText('Acesso negado')).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Navegação principal' })).not.toHaveTextContent(
      'Configurações',
    );
  });

  it.each(['/conexoes', '/chat', '/historico'])('renders an empty state for %s', async (path) => {
    renderApp(path, meIs(ANALYST));

    expect(await screen.findByRole('heading', { level: 2 })).toBeInTheDocument();
  });

  it('renders not found for unknown routes', async () => {
    renderApp('/nao-existe', meIs(ANALYST));

    expect(await screen.findByText('Página não encontrada')).toBeInTheDocument();
  });
});
