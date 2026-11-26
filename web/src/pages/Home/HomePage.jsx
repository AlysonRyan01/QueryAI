import { LayoutDashboard } from 'lucide-react';
import { useAuth } from '../../app/AuthProvider.jsx';
import EmptyState from '../../components/EmptyState.jsx';
import PageHeader from '../../components/PageHeader.jsx';

export default function HomePage() {
  const { user } = useAuth();
  const firstName = user?.name?.split(' ')[0];
  return (
    <>
      <PageHeader
        title={firstName ? `Olá, ${firstName}` : 'Início'}
        description="Visão geral das suas análises."
      />
      <EmptyState
        icon={LayoutDashboard}
        title="Suas consultas recentes aparecerão aqui"
        description="Assim que você conectar um banco de dados e fizer perguntas no chat, os resultados mais recentes ficarão disponíveis nesta página."
      />
    </>
  );
}
