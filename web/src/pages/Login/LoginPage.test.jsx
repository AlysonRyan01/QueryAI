import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ADMIN, ok, problem, renderApp } from '../../test/renderApp.jsx';

async function submitLogin() {
  const user = userEvent.setup();
  await user.type(await screen.findByLabelText('E-mail'), 'ana@queryai.test');
  await user.type(screen.getByLabelText('Senha'), 'some-password-123');
  await user.click(screen.getByRole('button', { name: 'Entrar' }));
}

function loginRespondsWith(response) {
  return (url, init) => {
    if (url === '/api/auth/me') return problem(401, 'unauthorized');
    if (url === '/api/auth/login' && init.method === 'POST') return response();
    return undefined;
  };
}

describe('LoginPage', () => {
  it.each([
    [problem(401, 'invalid_credentials'), 'E-mail ou senha inválidos.'],
    [
      problem(423, 'account_locked', 'x', { retryAfterSeconds: 600 }),
      'Conta bloqueada temporariamente. Tente novamente em 10 minutos.',
    ],
    [problem(429, 'rate_limited'), 'Muitas tentativas. Aguarde um minuto e tente novamente.'],
    [problem(400, 'https_required'), 'Esta instância exige HTTPS. Acesse pelo endereço seguro.'],
  ])('shows a friendly message for %#', async (response, message) => {
    renderApp(
      '/login',
      loginRespondsWith(() => response),
    );

    await submitLogin();

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByText(/trace-123/)).toBeInTheDocument();
  });

  it('redirects to the password change when required', async () => {
    const { router } = renderApp(
      '/login',
      loginRespondsWith(() => ok({ ...ADMIN, mustChangePassword: true })),
    );

    await submitLogin();

    await waitFor(() => expect(router.state.location.pathname).toBe('/trocar-senha'));
  });

  it('returns to the original page after login', async () => {
    const { router } = renderApp(
      '/login?redirect=%2Fhistorico',
      loginRespondsWith(() => ok(ADMIN)),
    );

    await submitLogin();

    await waitFor(() => expect(router.state.location.pathname).toBe('/historico'));
  });

  it('ignores external redirect targets', async () => {
    const { router } = renderApp(
      '/login?redirect=%2F%2Fevil.example',
      loginRespondsWith(() => ok(ADMIN)),
    );

    await submitLogin();

    await waitFor(() => expect(router.state.location.pathname).toBe('/'));
  });
});
