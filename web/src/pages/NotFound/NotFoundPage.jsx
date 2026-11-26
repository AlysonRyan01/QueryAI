import { Link } from 'react-router';
import { SearchX } from 'lucide-react';
import EmptyState from '../../components/EmptyState.jsx';

export default function NotFoundPage() {
  return (
    <EmptyState
      icon={SearchX}
      title="Página não encontrada"
      description="O endereço acessado não existe."
    >
      <Link to="/">Voltar ao início</Link>
    </EmptyState>
  );
}
