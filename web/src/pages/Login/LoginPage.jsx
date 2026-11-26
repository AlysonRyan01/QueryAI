import { useState } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router';
import { useAuth } from '../../app/AuthProvider.jsx';
import Button from '../../components/Button.jsx';
import Input from '../../components/Input.jsx';
import ErrorMessage from '../../components/ErrorMessage.jsx';
import styles from './LoginPage.module.css';

export function loginErrorMessage(error) {
  switch (error?.code) {
    case 'invalid_credentials':
      return 'E-mail ou senha inválidos.';
    case 'account_locked': {
      const minutes = Math.max(1, Math.ceil((error.retryAfterSeconds ?? 900) / 60));
      return `Conta bloqueada temporariamente. Tente novamente em ${minutes} minuto${minutes > 1 ? 's' : ''}.`;
    }
    case 'rate_limited':
      return 'Muitas tentativas. Aguarde um minuto e tente novamente.';
    case 'https_required':
      return 'Esta instância exige HTTPS. Acesse pelo endereço seguro.';
    case 'validation_failed':
      return 'Informe e-mail e senha.';
    default:
      return error?.title ?? 'Não foi possível entrar. Tente novamente.';
  }
}

function safeRedirect(value) {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const redirect = safeRedirect(searchParams.get('redirect'));

  if (user) return <Navigate to={user.mustChangePassword ? '/trocar-senha' : redirect} replace />;

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const loggedIn = await login(email, password);
      navigate(loggedIn.mustChangePassword ? '/trocar-senha' : redirect, { replace: true });
    } catch (err) {
      setError(err);
      setPassword('');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.page}>
      <form className={styles.card} onSubmit={handleSubmit} noValidate>
        <div className={styles.brand}>
          <span className={styles.logo} aria-hidden="true">
            Q
          </span>
          <h1>QueryAI</h1>
        </div>
        <p className={styles.subtitle}>Entre para consultar seus dados em linguagem natural.</p>

        {error && <ErrorMessage error={error} message={loginErrorMessage(error)} />}

        <Input
          label="E-mail"
          type="email"
          name="email"
          autoComplete="username"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <Input
          label="Senha"
          type="password"
          name="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <Button type="submit" loading={submitting} disabled={!email || !password}>
          Entrar
        </Button>
      </form>
    </div>
  );
}
