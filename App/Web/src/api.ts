import type { CheckDuplicateResponse, EmbeddingGenerationResult, IssueImportResult, Owner } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  const text = await response.text()
  const payload = text ? JSON.parse(text) : null

  if (!response.ok) {
    const message =
      (payload && typeof payload === 'object' && 'error' in payload && String(payload.error)) ||
      `Request failed with status ${response.status}`
    throw new ApiError(message, response.status)
  }

  return payload as T
}

async function get<T>(path: string): Promise<T> {
  const response = await fetch(path)
  return handleResponse<T>(response)
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })

  return handleResponse<T>(response)
}

export function listRepositoriesByOwner(owner: string) {
  return get<string[]>(`/api/repositories/${encodeURIComponent(owner)}`)
}

export function importIssues(owner: string, repository: string) {
  return post<IssueImportResult>(`/api/repositories/${encodeURIComponent(owner)}/${encodeURIComponent(repository)}/import`)
}

export function generateEmbeddings(owner: string, repository: string) {
  return post<EmbeddingGenerationResult>(
    `/api/repositories/${encodeURIComponent(owner)}/${encodeURIComponent(repository)}/generate-embeddings`,
  )
}

export function checkDuplicate(owner: string, repository: string, title: string, body: string) {
  return post<CheckDuplicateResponse>(
    `/api/repositories/${encodeURIComponent(owner)}/${encodeURIComponent(repository)}/check-duplicate`,
    { title, body: body || null },
  )
}

export function listOwners() {
  return get<Owner[]>('/api/owners')
}

export function createOwner(name: string) {
  return post<Owner>('/api/owners', { name })
}
