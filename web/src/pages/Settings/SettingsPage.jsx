import { NavLink, Outlet } from 'react-router';
import PageHeader from '../../components/PageHeader.jsx';
import styles from './SettingsPage.module.css';

const TABS = [{ to: 'usuarios', label: 'Usuários' }];

export default function SettingsPage() {
  return (
    <>
      <PageHeader title="Configurações" description="Administração da instância." />
      <nav className={styles.tabs} aria-label="Seções de configurações">
        {TABS.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) => `${styles.tab} ${isActive ? styles.active : ''}`}
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </>
  );
}
