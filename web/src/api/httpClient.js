export const CSRF_HEADER = 'X-Requested-With';
export const CSRF_VALUE = 'QueryAI';

const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

export class ApiError extends Error {
  constructor(problem) {
    super(problem.title);
    this.name = 'ApiError';
    this.status = problem.status;
    this.code = problem.code;
    this.title = problem.title;
    this.detail = problem.detail;
    this.traceId = problem.traceId;
    this.errors = problem.errors ?? {};
    this.retryAfterSeconds = problem.retryAfterSeconds;
  }
}

const NETWORK_ERROR = {
  status: 0,
  code: 'network_error',
  title: 'Não foi possível conectar ao servidor. Verifique sua conexão.',
};

export async function request(path, { method = 'GET', body, signal } = {}) {
  const headers = { Accept: 'application/json' };
  if (UNSAFE_METHODS.has(method)) headers[CSRF_HEADER] = CSRF_VALUE;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(path, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      credentials: 'same-origin',
      signal,
    });
  } catch (error) {
    if (error?.name === 'AbortError') throw error;
    throw new ApiError(NETWORK_ERROR);
  }

  if (response.status === 204) return null;

  const contentType = response.headers.get('content-type') ?? '';
  const isJson = contentType.includes('json');
  const payload = isJson ? await response.json().catch(() => null) : null;

  if (!response.ok) {
    throw new ApiError({
      status: response.status,
      code: payload?.code ?? `http_${response.status}`,
      title: payload?.title ?? 'Ocorreu um erro inesperado.',
      detail: payload?.detail,
      traceId: payload?.traceId,
      errors: payload?.errors,
      retryAfterSeconds:
        payload?.retryAfterSeconds ?? parseRetryAfter(response.headers.get('retry-after')),
    });
  }

  return payload;
}

function parseRetryAfter(value) {
  const seconds = Number.parseInt(value ?? '', 10);
  return Number.isFinite(seconds) ? seconds : undefined;
}

export const http = {
  get: (path, options) => request(path, { ...options, method: 'GET' }),
  post: (path, body, options) => request(path, { ...options, method: 'POST', body }),
  put: (path, body, options) => request(path, { ...options, method: 'PUT', body }),
  delete: (path, options) => request(path, { ...options, method: 'DELETE' }),
};
