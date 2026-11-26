import { useState } from 'react';
import Modal from '../../../components/Modal.jsx';
import Input from '../../../components/Input.jsx';
import Button from '../../../components/Button.jsx';
import ErrorMessage from '../../../components/ErrorMessage.jsx';
import inputStyles from '../../../components/Input.module.css';
import { fieldErrorsOf, userErrorMessage } from './userErrors.js';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function validate(values, isEdit) {
  const errors = {};
  const name = values.name.trim();
  if (name.length < 2 || name.length > 120)
    errors.name = 'O nome deve ter entre 2 e 120 caracteres.';
  if (!isEdit) {
    if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Informe um e-mail válido.';
    if (values.temporaryPassword.length < 12)
      errors.temporaryPassword = 'A senha deve ter no mínimo 12 caracteres.';
  }
  return errors;
}

export default function UserFormModal({ user, onSubmit, onClose }) {
  const isEdit = Boolean(user);
  const [values, setValues] = useState({
    name: user?.name ?? '',
    email: user?.email ?? '',
    role: user?.role ?? 'Analyst',
    temporaryPassword: '',
  });
  const [errors, setErrors] = useState({});
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const update = (field) => (event) => setValues((v) => ({ ...v, [field]: event.target.value }));

  const handleSubmit = async (event) => {
    event.preventDefault();
    const validation = validate(values, isEdit);
    setErrors(validation);
    setError(null);
    if (Object.keys(validation).length) return;

    setSubmitting(true);
    try {
      await onSubmit(
        isEdit
          ? { name: values.name.trim(), role: values.role }
          : { ...values, name: values.name.trim(), email: values.email.trim() },
      );
    } catch (err) {
      const apiFieldErrors = fieldErrorsOf(err);
      if (Object.keys(apiFieldErrors).length) setErrors(apiFieldErrors);
      if (err.code === 'email_already_exists') setErrors({ email: userErrorMessage(err) });
      else setError({ ...err, title: userErrorMessage(err) });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open
      title={isEdit ? 'Editar usuário' : 'Novo usuário'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" form="user-form" loading={submitting}>
            {isEdit ? 'Salvar' : 'Criar usuário'}
          </Button>
        </>
      }
    >
      <form id="user-form" onSubmit={handleSubmit} noValidate style={{ display: 'contents' }}>
        <ErrorMessage error={error} />
        <Input label="Nome" value={values.name} onChange={update('name')} error={errors.name} />
        <Input
          label="E-mail"
          type="email"
          value={values.email}
          onChange={update('email')}
          error={errors.email}
          disabled={isEdit}
        />
        <div className={inputStyles.field}>
          <label htmlFor="user-role" className={inputStyles.label}>
            Papel
          </label>
          <select id="user-role" value={values.role} onChange={update('role')}>
            <option value="Analyst">Analista</option>
            <option value="Admin">Administrador</option>
          </select>
        </div>
        {!isEdit && (
          <Input
            label="Senha temporária"
            type="password"
            autoComplete="new-password"
            value={values.temporaryPassword}
            onChange={update('temporaryPassword')}
            error={errors.temporaryPassword}
            hint="Mínimo de 12 caracteres. O usuário deverá trocá-la no primeiro acesso."
          />
        )}
      </form>
    </Modal>
  );
}
