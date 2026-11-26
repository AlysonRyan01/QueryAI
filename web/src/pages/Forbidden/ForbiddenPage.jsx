import { Link } from 'react-router';
import { ShieldAlert } from 'lucide-react';
import EmptyState from '../../components/EmptyState.jsx';

export default function ForbiddenPage() {
  return (
    <EmptyState
      icon={ShieldAlert}
      title="Acesso negado"
      description="Esta área é restrita a administradores da instância."
    >
      <Link to="/">Voltar ao início</Link>
    </EmptyState>
  );
}
