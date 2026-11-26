import { useEffect, useState } from 'react';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { KeyRound, Pencil, Plus, Power, Search } from 'lucide-react';
import * as usersApi from '../../../api/users.js';
import { useAuth } from '../../../app/AuthProvider.jsx';
import Badge from '../../../components/Badge.jsx';
import Button from '../../../components/Button.jsx';
import ErrorMessage from '../../../components/ErrorMessage.jsx';
import Spinner from '../../../components/Spinner.jsx';
import Table from '../../../components/Table.jsx';
import ConfirmModal from './ConfirmModal.jsx';
import ResetPasswordModal from './ResetPasswordModal.jsx';
import UserFormModal from './UserFormModal.jsx';
import styles from './UsersPage.module.css';

const PAGE_SIZE = 20;
const dateFormatter = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

function useDebounced(value, delay = 300) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(id);
  }, [value, delay]);
  return debounced;
}

export default function UsersPage() {
  const { user: currentUser, refresh } = useAuth();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState(null); // { type: 'create'|'edit'|'reset'|'deactivate'|'activate', user? }
  const debouncedSearch = useDebounced(search);

  const usersQuery = useQuery({
    queryKey: ['users', { search: debouncedSearch, page }],
    queryFn: ({ signal }) =>
      usersApi.listUsers({ search: debouncedSearch, page, pageSize: PAGE_SIZE }, signal),
    placeholderData: keepPreviousData,
  });

  const afterChange = async (affectedUserId) => {
    await queryClient.invalidateQueries({ queryKey: ['users'] });
    if (affectedUserId === currentUser?.id) await refresh();
    setDialog(null);
  };

  const create = useMutation({ mutationFn: usersApi.createUser, onSuccess: () => afterChange() });
  const update = useMutation({
    mutationFn: ({ id, data }) => usersApi.updateUser(id, data),
    onSuccess: (_, { id }) => afterChange(id),
  });
  const reset = useMutation({
    mutationFn: ({ id, password }) => usersApi.resetPassword(id, password),
    onSuccess: (_, { id }) => afterChange(id),
  });
  const deactivate = useMutation({
    mutationFn: usersApi.deactivateUser,
    onSuccess: (_, id) => afterChange(id),
  });
  const activate = useMutation({
    mutationFn: usersApi.activateUser,
    onSuccess: (_, id) => afterChange(id),
  });

  const data = usersQuery.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  const columns = [
    {
      key: 'name',
      header: 'Nome',
      render: (u) => (
        <div className={styles.identity}>
          <span className={styles.name}>{u.name}</span>
          <span className={styles.email}>{u.email}</span>
        </div>
      ),
    },
    {
      key: 'role',
      header: 'Papel',
      render: (u) => (
        <Badge tone={u.role === 'Admin' ? 'accent' : 'neutral'}>
          {u.role === 'Admin' ? 'Administrador' : 'Analista'}
        </Badge>
      ),
    },
    {
      key: 'status',
      header: 'Situação',
      render: (u) => (
        <div className={styles.badges}>
          <Badge tone={u.isActive ? 'success' : 'danger'}>
            {u.isActive ? 'Ativo' : 'Desativado'}
          </Badge>
          {u.isLockedOut && <Badge tone="warning">Bloqueado</Badge>}
          {u.mustChangePassword && <Badge tone="neutral">Troca de senha pendente</Badge>}
        </div>
      ),
    },
    {
      key: 'lastLogin',
      header: 'Último acesso',
      render: (u) => (u.lastLoginAtUtc ? dateFormatter.format(new Date(u.lastLoginAtUtc)) : '—'),
    },
    {
      key: 'actions',
      header: 'Ações',
      className: styles.actionsCell,
      render: (u) => (
        <div className={styles.actions}>
          <Button
            size="sm"
            variant="ghost"
            aria-label={`Editar ${u.name}`}
            title="Editar"
            onClick={() => setDialog({ type: 'edit', user: u })}
          >
            <Pencil size={16} aria-hidden="true" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            aria-label={`Redefinir senha de ${u.name}`}
            title="Redefinir senha"
            onClick={() => setDialog({ type: 'reset', user: u })}
          >
            <KeyRound size={16} aria-hidden="true" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            aria-label={`${u.isActive ? 'Desativar' : 'Reativar'} ${u.name}`}
            title={u.isActive ? 'Desativar' : 'Reativar'}
            onClick={() => setDialog({ type: u.isActive ? 'deactivate' : 'activate', user: u })}
          >
            <Power size={16} aria-hidden="true" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <section aria-labelledby="users-title">
      <div className={styles.toolbar}>
        <h2 id="users-title" className={styles.title}>
          Usuários
        </h2>
        <div className={styles.search}>
          <Search size={16} aria-hidden="true" className={styles.searchIcon} />
          <input
            type="search"
            aria-label="Buscar por nome ou e-mail"
            placeholder="Buscar por nome ou e-mail"
            value={search}
            maxLength={100}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <Button onClick={() => setDialog({ type: 'create' })}>
          <Plus size={16} aria-hidden="true" /> Novo usuário
        </Button>
      </div>

      {usersQuery.isPending ? (
        <div className={styles.loading}>
          <Spinner size={24} />
        </div>
      ) : usersQuery.isError ? (
        <ErrorMessage error={usersQuery.error} />
      ) : (
        <>
          <Table
            caption="Usuários da instância"
            columns={columns}
            rows={data.items}
            rowKey={(u) => u.id}
          />
          <div className={styles.pagination}>
            <span>
              {data.total} usuário{data.total === 1 ? '' : 's'}
            </span>
            <div className={styles.pager}>
              <Button
                size="sm"
                variant="secondary"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                Anterior
              </Button>
              <span>
                Página {page} de {totalPages}
              </span>
              <Button
                size="sm"
                variant="secondary"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Próxima
              </Button>
            </div>
          </div>
        </>
      )}

      {dialog?.type === 'create' && (
        <UserFormModal
          onClose={() => setDialog(null)}
          onSubmit={(values) => create.mutateAsync(values)}
        />
      )}
      {dialog?.type === 'edit' && (
        <UserFormModal
          user={dialog.user}
          onClose={() => setDialog(null)}
          onSubmit={(values) => update.mutateAsync({ id: dialog.user.id, data: values })}
        />
      )}
      {dialog?.type === 'reset' && (
        <ResetPasswordModal
          user={dialog.user}
          onClose={() => setDialog(null)}
          onSubmit={(password) => reset.mutateAsync({ id: dialog.user.id, password })}
        />
      )}
      {dialog?.type === 'deactivate' && (
        <ConfirmModal
          title="Desativar usuário"
          message={`${dialog.user.name} perderá o acesso imediatamente. Você poderá reativar a conta depois.`}
          confirmLabel="Desativar"
          variant="danger"
          onClose={() => setDialog(null)}
          onConfirm={() => deactivate.mutateAsync(dialog.user.id)}
        />
      )}
      {dialog?.type === 'activate' && (
        <ConfirmModal
          title="Reativar usuário"
          message={`${dialog.user.name} voltará a ter acesso à instância.`}
          confirmLabel="Reativar"
          onClose={() => setDialog(null)}
          onConfirm={() => activate.mutateAsync(dialog.user.id)}
        />
      )}
    </section>
  );
}
