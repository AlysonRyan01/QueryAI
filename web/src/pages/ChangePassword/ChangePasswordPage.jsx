import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from '../../app/AuthProvider.jsx';
import { changePassword } from '../../api/auth.js';
import Button from '../../components/Button.jsx';
import Input from '../../components/Input.jsx';
import ErrorMessage from '../../components/ErrorMessage.jsx';
import styles from '../Login/LoginPage.module.css';

const MIN_LENGTH = 12;

function validate({ current, next, confirm }) {
  const errors = {};
  if (!current) errors.current = 'Informe a senha atual.';
  if (next.length < MIN_LENGTH)
    errors.next = `A nova senha deve ter no mínimo ${MIN_LENGTH} caracteres.`;
  else if (next.length > 128) errors.next = 'A nova senha deve ter no máximo 128 caracteres.';
  else if (next === current) errors.next = 'A nova senha deve ser diferente da atual.';
  if (confirm !== next) errors.confirm = 'As senhas não conferem.';
  return errors;
}

export default function ChangePasswordPage() {
  const { user, refresh, logout } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ current: '', next: '', confirm: '' });
  const [fieldErrors, setFieldErrors] = useState({});
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const update = (field) => (event) => setForm((f) => ({ ...f, [field]: event.target.value }));

  const handleSubmit = async (event) => {
    event.preventDefault();
    const errors = validate(form);
    setFieldErrors(errors);
    setError(null);
    if (Object.keys(errors).length > 0) return;

    setSubmitting(true);
    try {
      await changePassword(form.current, form.next);
      await refresh();
      navigate('/', { replace: true });
    } catch (err) {
      if (err.code === 'invalid_current_password')
        setFieldErrors({ current: 'A senha atual está incorreta.' });
      else if (err.code === 'same_password')
        setFieldErrors({ next: 'A nova senha deve ser diferente da atual.' });
      else setError(err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <h1>Trocar senha</h1>
        <p className={styles.subtitle}>
          {user?.mustChangePassword
            ? 'Por segurança, defina uma nova senha antes de continuar.'
            : 'Defina uma nova senha para sua conta.'}
        </p>
        <ErrorMessage error={error} />
        <Input
          label="Senha atual"
          type="password"
          autoComplete="current-password"
          value={form.current}
          onChange={update('current')}
          error={fieldErrors.current}
        />
        <Input
          label="Nova senha"
          type="password"
          autoComplete="new-password"
          value={form.next}
          onChange={update('next')}
          error={fieldErrors.next}
          hint={`Mínimo de ${MIN_LENGTH} caracteres.`}
        />
        <Input
          label="Confirmar nova senha"
          type="password"
          autoComplete="new-password"
          value={form.confirm}
          onChange={update('confirm')}
          error={fieldErrors.confirm}
        />
        <Button type="submit" loading={submitting}>
          Salvar nova senha
        </Button>
        {user?.mustChangePassword ? (
          <Button variant="ghost" onClick={logout}>
            Sair
          </Button>
        ) : (
          <Button variant="ghost" onClick={() => navigate(-1)}>
            Cancelar
          </Button>
        )}
      </form>
    </div>
  );
}
