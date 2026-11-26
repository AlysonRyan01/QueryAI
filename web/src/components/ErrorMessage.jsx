import { AlertCircle } from 'lucide-react';
import styles from './ErrorMessage.module.css';

export default function ErrorMessage({ error, message }) {
  if (!error && !message) return null;
  const text = message ?? error?.title ?? error?.message ?? 'Ocorreu um erro inesperado.';
  return (
    <div className={styles.error} role="alert">
      <AlertCircle size={16} aria-hidden="true" className={styles.icon} />
      <div>
        <p className={styles.text}>{text}</p>
        {error?.traceId && <p className={styles.trace}>Código de rastreamento: {error.traceId}</p>}
      </div>
    </div>
  );
}
