import { History } from 'lucide-react';
import EmptyState from '../../components/EmptyState.jsx';
import PageHeader from '../../components/PageHeader.jsx';

export default function HistoryPage() {
  return (
    <>
      <PageHeader title="Histórico" description="Conversas e consultas anteriores." />
      <EmptyState
        icon={History}
        title="Nenhuma conversa ainda"
        description="Suas conversas, o SQL gerado e os resultados ficarão salvos aqui para você continuar depois."
      />
    </>
  );
}
