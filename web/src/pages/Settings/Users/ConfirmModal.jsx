import { useState } from 'react';
import Modal from '../../../components/Modal.jsx';
import Button from '../../../components/Button.jsx';
import ErrorMessage from '../../../components/ErrorMessage.jsx';
import { userErrorMessage } from './userErrors.js';

export default function ConfirmModal({
  title,
  message,
  confirmLabel,
  variant = 'primary',
  onConfirm,
  onClose,
}) {
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const handleConfirm = async () => {
    setError(null);
    setSubmitting(true);
    try {
      await onConfirm();
    } catch (err) {
      setError({ ...err, title: userErrorMessage(err) });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant={variant} loading={submitting} onClick={handleConfirm}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <p style={{ margin: 0 }}>{message}</p>
      <ErrorMessage error={error} />
    </Modal>
  );
}
