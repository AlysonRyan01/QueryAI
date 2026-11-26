import { describe, expect, it, vi } from 'vitest';
import { ApiError, http } from './httpClient.js';

function mockFetch(status, body, headers = {}) {
  const fn = vi.fn().mockResolvedValue(
    new Response(body === null ? null : JSON.stringify(body), {
      status,
      headers: {
        'content-type': status >= 400 ? 'application/problem+json' : 'application/json',
        ...headers,
      },
    }),
  );
  vi.stubGlobal('fetch', fn);
  return fn;
}

describe('httpClient', () => {
  it('sends the CSRF header and JSON body on POST', async () => {
    const fetchMock = mockFetch(200, { ok: true });

    await http.post('/api/x', { a: 1 });

    const [, init] = fetchMock.mock.calls[0];
    expect(init.headers['X-Requested-With']).toBe('QueryAI');
    expect(init.headers['Content-Type']).toBe('application/json');
    expect(init.credentials).toBe('same-origin');
    expect(init.body).toBe('{"a":1}');
  });

  it('does not send the CSRF header on GET', async () => {
    const fetchMock = mockFetch(200, { ok: true });

    await http.get('/api/x');

    expect(fetchMock.mock.calls[0][1].headers['X-Requested-With']).toBeUndefined();
  });

  it('returns null on 204', async () => {
    mockFetch(204, null);
    await expect(http.post('/api/x')).resolves.toBeNull();
  });

  it('parses problem+json into ApiError', async () => {
    mockFetch(409, {
      title: 'Já existe um usuário com este e-mail.',
      status: 409,
      code: 'email_already_exists',
      traceId: 'abc123',
      errors: { email: ['x'] },
    });

    const error = await http.post('/api/users', {}).catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(409);
    expect(error.code).toBe('email_already_exists');
    expect(error.traceId).toBe('abc123');
    expect(error.errors.email).toEqual(['x']);
  });

  it('maps network failures to network_error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    const error = await http.get('/api/x').catch((e) => e);

    expect(error.code).toBe('network_error');
  });
});
