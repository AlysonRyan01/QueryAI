import { describe, expect, it } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ADMIN, ok, problem, renderApp } from '../../../test/renderApp.jsx';

const USERS = {
  items: [
    {
      ...ADMIN,
      isActive: true,
      isLockedOut: false,
      createdAtUtc: '2026-11-01T10:00:00Z',
      lastLoginAtUtc: null,
    },
    {
      id: 'c3',
      name: 'Carla Souza',
      email: 'carla@queryai.test',
      role: 'Analyst',
      isActive: false,
      isLockedOut: true,
      mustChangePassword: true,
      createdAtUtc: '2026-11-02T10:00:00Z',
      lastLoginAtUtc: '2026-11-10T13:30:00Z',
    },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
};

function api(overrides = {}) {
  return (url, init) => {
    if (url === '/api/auth/me') return ok(ADMIN);
    if (url.startsWith('/api/users?')) return ok(USERS);
    const key = `${init.method ?? 'GET'} ${url}`;
    return overrides[key]?.(init);
  };
}

describe('UsersPage', () => {
  it('lists users with role and status', async () => {
    renderApp('/configuracoes/usuarios', api());

    const table = await screen.findByRole('table');
    expect(within(table).getByText('Carla Souza')).toBeInTheDocument();
    expect(within(table).getByText('Desativado')).toBeInTheDocument();
    expect(within(table).getByText('Bloqueado')).toBeInTheDocument();
    expect(within(table).getAllByText('Administrador')).toHaveLength(1);
    expect(screen.getByText('2 usuários')).toBeInTheDocument();
  });

  it('validates the create form before calling the API', async () => {
    const user = userEvent.setup();
    const { fetchMock } = renderApp('/configuracoes/usuarios', api());

    await user.click(await screen.findByRole('button', { name: /Novo usuário/ }));
    await user.click(screen.getByRole('button', { name: 'Criar usuário' }));

    expect(
      await screen.findByText('O nome deve ter entre 2 e 120 caracteres.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Informe um e-mail válido.')).toBeInTheDocument();
    expect(screen.getByText('A senha deve ter no mínimo 12 caracteres.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
  });

  it('shows email_already_exists on the e-mail field', async () => {
    const user = userEvent.setup();
    renderApp(
      '/configuracoes/usuarios',
      api({ 'POST /api/users': () => problem(409, 'email_already_exists') }),
    );

    await user.click(await screen.findByRole('button', { name: /Novo usuário/ }));
    await user.type(screen.getByLabelText('Nome'), 'Daniel Lima');
    await user.type(screen.getByLabelText('E-mail'), 'carla@queryai.test');
    await user.type(screen.getByLabelText('Senha temporária'), 'Temporary-Pass-1');
    await user.click(screen.getByRole('button', { name: 'Criar usuário' }));

    expect(await screen.findByText('Já existe um usuário com este e-mail.')).toBeInTheDocument();
  });

  it('shows last_admin when deactivating the only admin', async () => {
    const user = userEvent.setup();
    renderApp(
      '/configuracoes/usuarios',
      api({ 'POST /api/users/a1/deactivate': () => problem(409, 'last_admin') }),
    );

    await user.click(await screen.findByRole('button', { name: 'Desativar Ana Admin' }));
    await user.click(screen.getByRole('button', { name: 'Desativar' }));

    expect(
      await screen.findByText('A instância precisa de pelo menos um administrador ativo.'),
    ).toBeInTheDocument();
  });
});
