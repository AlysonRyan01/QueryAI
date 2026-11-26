import { Navigate, Outlet, useLocation } from 'react-router';
import { useAuth } from './AuthProvider.jsx';
import Spinner from '../components/Spinner.jsx';
import ErrorMessage from '../components/ErrorMessage.jsx';
import ForbiddenPage from '../pages/Forbidden/ForbiddenPage.jsx';
import styles from './guards.module.css';

function FullPageStatus({ children }) {
  return <div className={styles.center}>{children}</div>;
}

export function RequireAuth() {
  const { user, isLoading, error } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <FullPageStatus>
        <Spinner size={28} />
      </FullPageStatus>
    );
  }

  if (error) {
    return (
      <FullPageStatus>
        <ErrorMessage error={error} />
      </FullPageStatus>
    );
  }

  if (!user) {
    const redirect = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?redirect=${redirect}`} replace />;
  }

  return <Outlet />;
}

export function RequirePasswordChanged() {
  const { user } = useAuth();
  if (user?.mustChangePassword) return <Navigate to="/trocar-senha" replace />;
  return <Outlet />;
}

export function RequireAdmin() {
  const { user } = useAuth();
  if (user?.role !== 'Admin') return <ForbiddenPage />;
  return <Outlet />;
}
