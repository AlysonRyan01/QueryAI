import { useState } from 'react';
import Modal from '../../../components/Modal.jsx';
import Input from '../../../components/Input.jsx';
import Button from '../../../components/Button.jsx';
import ErrorMessage from '../../../components/ErrorMessage.jsx';
import { fieldErrorsOf, userErrorMessage } from './userErrors.js';

export default function ResetPasswordModal({ user, onSubmit, onClose }) {
  const [password, setPassword] = useState('');
  const [fieldError, setFieldError] = useState();
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError(null);
    if (password.length < 12) {
      setFieldError('A senha deve ter no mínimo 12 caracteres.');
      return;
    }
    setFieldError(undefined);
    setSubmitting(true);
    try {
      await onSubmit(password);
    } catch (err) {
      const fields = fieldErrorsOf(err);
      if (fields.temporaryPassword) setFieldError(fields.temporaryPassword);
      else setError({ ...err, title: userErrorMessage(err) });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open
      title="Redefinir senha"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" form="reset-form" loading={submitting}>
            Redefinir
          </Button>
        </>
      }
    >
      <form id="reset-form" onSubmit={handleSubmit} noValidate style={{ display: 'contents' }}>
        <p style={{ margin: 0 }}>
          Defina uma senha temporária para <strong>{user.name}</strong>. A conta será desbloqueada e
          a troca de senha será obrigatória no próximo acesso.
        </p>
        <ErrorMessage error={error} />
        <Input
          label="Senha temporária"
          type="password"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={fieldError}
        />
      </form>
    </Modal>
  );
}
