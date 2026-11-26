import { Database } from 'lucide-react';
import EmptyState from '../../components/EmptyState.jsx';
import PageHeader from '../../components/PageHeader.jsx';

export default function ConnectionsPage() {
  return (
    <>
      <PageHeader title="Conexões" description="Bancos de dados disponíveis para consulta." />
      <EmptyState
        icon={Database}
        title="Nenhuma conexão cadastrada"
        description="Em breve você poderá conectar bancos PostgreSQL e SQL Server com credenciais somente leitura."
      />
    </>
  );
}
