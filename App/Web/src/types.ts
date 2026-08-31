export interface IssueImportResult {
  issuesDiscovered: number
  issuesCreated: number
  issuesUpdated: number
  issuesSkipped: number
}

export interface EmbeddingGenerationResult {
  totalIssuesProcessed: number
  embeddingsGenerated: number
  issuesSkipped: number
  failures: number
}

export type DuplicateClassification = 'HighConfidence' | 'Possible' | 'Unlikely'

export interface DuplicateCandidateResponse {
  issueNumber: number
  title: string
  url: string
  similarity: number
  classification: DuplicateClassification
}

export interface ProcessingInfoResponse {
  embeddingModel: string
  similarityThreshold: number
  processingTimeMs: number
}

export interface CheckDuplicateResponse {
  isPotentialDuplicate: boolean
  confidence: DuplicateClassification
  candidates: DuplicateCandidateResponse[]
  processing: ProcessingInfoResponse
}

export interface Owner {
  id: number
  name: string
  createdDate: string | null
  changedDate: string | null
}

export type ActionKind = 'import' | 'generate-embeddings' | 'check-duplicate'

export interface ActivityEntry {
  id: string
  kind: ActionKind
  startedAt: Date
  durationMs: number
  owner: string
  repository: string
  status: 'success' | 'error'
  summary: string
  detail?: IssueImportResult | EmbeddingGenerationResult | CheckDuplicateResponse
}
