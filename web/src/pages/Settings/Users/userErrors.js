export function userErrorMessage(error) {
  switch (error?.code) {
    case 'email_already_exists':
      return 'Já existe um usuário com este e-mail.';
    case 'last_admin':
      return 'A instância precisa de pelo menos um administrador ativo.';
    case 'concurrency_conflict':
      return 'O usuário foi alterado por outra pessoa. Recarregue a página e tente novamente.';
    case 'not_found':
      return 'Usuário não encontrado.';
    default:
      return error?.title ?? 'Não foi possível concluir a operação.';
  }
}

export function fieldErrorsOf(error) {
  return Object.fromEntries(
    Object.entries(error?.errors ?? {}).map(([key, messages]) => [key, messages[0]]),
  );
}
