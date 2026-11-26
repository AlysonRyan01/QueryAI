import { MessageSquare } from 'lucide-react';
import EmptyState from '../../components/EmptyState.jsx';
import PageHeader from '../../components/PageHeader.jsx';

export default function ChatPage() {
  return (
    <>
      <PageHeader
        title="Chat"
        description="Pergunte em português e receba tabelas, gráficos e explicações."
      />
      <EmptyState
        icon={MessageSquare}
        title="O assistente de dados estará disponível em breve"
        description="Depois de cadastrar uma conexão, você poderá fazer perguntas como “Qual foi o faturamento de cada mês nos últimos seis meses?”."
      />
    </>
  );
}
