import type { CheckDuplicateResponse, EmbeddingGenerationResult, IssueImportResult } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })

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
