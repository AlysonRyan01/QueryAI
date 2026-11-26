import { NavLink } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Database, History, LayoutDashboard, MessageSquare, Settings } from 'lucide-react';
import { useAuth } from '../../app/AuthProvider.jsx';
import { getApiVersion } from '../../api/auth.js';
import styles from './Sidebar.module.css';

const NAV_ITEMS = [
  { to: '/', label: 'Início', icon: LayoutDashboard, end: true },
  { to: '/conexoes', label: 'Conexões', icon: Database },
  { to: '/chat', label: 'Chat', icon: MessageSquare },
  { to: '/historico', label: 'Histórico', icon: History },
  { to: '/configuracoes', label: 'Configurações', icon: Settings, adminOnly: true },
];

export default function Sidebar({ open, onNavigate }) {
  const { user } = useAuth();
  const { data: apiVersion } = useQuery({
    queryKey: ['api-version'],
    queryFn: ({ signal }) => getApiVersion(signal),
    staleTime: Infinity,
  });

  const items = NAV_ITEMS.filter((item) => !item.adminOnly || user?.role === 'Admin');

  return (
    <aside
      id="sidebar"
      className={`${styles.sidebar} ${open ? styles.open : ''}`}
      aria-label="Barra lateral"
    >
      <div className={styles.brand}>
        <span className={styles.logo} aria-hidden="true">
          Q
        </span>
        QueryAI
      </div>
      <nav className={styles.nav} aria-label="Navegação principal">
        {items.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            onClick={onNavigate}
            className={({ isActive }) => `${styles.link} ${isActive ? styles.active : ''}`}
          >
            <Icon size={18} aria-hidden="true" />
            {label}
          </NavLink>
        ))}
      </nav>
      <footer className={styles.footer}>
        <span>Web {import.meta.env.VITE_APP_VERSION}</span>
        <span>API {apiVersion?.version ?? '…'}</span>
      </footer>
    </aside>
  );
}
