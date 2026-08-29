import { useState } from 'react'
import './App.css'
import { ApiError, checkDuplicate, generateEmbeddings, importIssues } from './api'
import type {
  ActionKind,
  ActivityEntry,
  CheckDuplicateResponse,
  EmbeddingGenerationResult,
  IssueImportResult,
} from './types'

type AsyncState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; message: string }

function newEntryId() {
  return crypto.randomUUID()
}

function summarize(kind: ActionKind, result: IssueImportResult | EmbeddingGenerationResult | CheckDuplicateResponse): string {
  switch (kind) {
    case 'import': {
      const r = result as IssueImportResult
      return `${r.issuesDiscovered} discovered · ${r.issuesCreated} created · ${r.issuesUpdated} updated · ${r.issuesSkipped} skipped`
    }
    case 'generate-embeddings': {
      const r = result as EmbeddingGenerationResult
      return `${r.embeddingsGenerated}/${r.totalIssuesProcessed} embedded · ${r.issuesSkipped} skipped · ${r.failures} failed`
    }
    case 'check-duplicate': {
      const r = result as CheckDuplicateResponse
      return r.candidates.length === 0
        ? 'No candidates above the similarity threshold'
        : `${r.candidates.length} candidate(s) · strongest: ${r.confidence}`
    }
  }
}

function App() {
  const [owner, setOwner] = useState('')
  const [repository, setRepository] = useState('')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')

  const [importState, setImportState] = useState<AsyncState<IssueImportResult>>({ status: 'idle' })
  const [embedState, setEmbedState] = useState<AsyncState<EmbeddingGenerationResult>>({ status: 'idle' })
  const [checkState, setCheckState] = useState<AsyncState<CheckDuplicateResponse>>({ status: 'idle' })

  const [activity, setActivity] = useState<ActivityEntry[]>([])

  const repoReady = owner.trim().length > 0 && repository.trim().length > 0

  async function runAction<T extends IssueImportResult | EmbeddingGenerationResult | CheckDuplicateResponse>(
    kind: ActionKind,
    setState: (s: AsyncState<T>) => void,
    fn: () => Promise<T>,
  ) {
    const startedAt = new Date()
    setState({ status: 'loading' })
    try {
      const data = await fn()
      setState({ status: 'success', data })
      setActivity((prev) => [
        {
          id: newEntryId(),
          kind,
          startedAt,
          durationMs: Date.now() - startedAt.getTime(),
          owner,
          repository,
          status: 'success',
          summary: summarize(kind, data),
          detail: data,
        },
        ...prev,
      ])
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Request failed — is the API running?'
      setState({ status: 'error', message })
      setActivity((prev) => [
        {
          id: newEntryId(),
          kind,
          startedAt,
          durationMs: Date.now() - startedAt.getTime(),
          owner,
          repository,
          status: 'error',
          summary: message,
        },
        ...prev,
      ])
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <h1>IssueSense</h1>
        <p>Trigger duplicate-detection actions against a repository and watch what happens.</p>
      </header>

      <section className="card">
        <h2>Repository</h2>
        <div className="field-row">
          <label>
            Owner
            <input value={owner} onChange={(e) => setOwner(e.target.value)} placeholder="e.g. octocat" />
          </label>
          <label>
            Repository
            <input value={repository} onChange={(e) => setRepository(e.target.value)} placeholder="e.g. hello-world" />
          </label>
        </div>
        {!repoReady && <p className="hint">Enter an owner and repository to enable the actions below.</p>}
      </section>

      <section className="actions-grid">
        <ActionCard
          title="Import issues"
          description="Pulls every issue from GitHub into the local database."
          buttonLabel="Import"
          disabled={!repoReady}
          state={importState}
          onRun={() => runAction('import', setImportState, () => importIssues(owner, repository))}
          renderResult={(r) => (
            <dl className="result-grid">
              <dt>Discovered</dt>
              <dd>{r.issuesDiscovered}</dd>
              <dt>Created</dt>
              <dd>{r.issuesCreated}</dd>
              <dt>Updated</dt>
              <dd>{r.issuesUpdated}</dd>
              <dt>Skipped</dt>
              <dd>{r.issuesSkipped}</dd>
            </dl>
          )}
        />

        <ActionCard
          title="Generate embeddings"
          description="Embeds any imported issues that don't have one yet."
          buttonLabel="Generate"
          disabled={!repoReady}
          state={embedState}
          onRun={() => runAction('generate-embeddings', setEmbedState, () => generateEmbeddings(owner, repository))}
          renderResult={(r) => (
            <dl className="result-grid">
              <dt>Processed</dt>
              <dd>{r.totalIssuesProcessed}</dd>
              <dt>Embedded</dt>
              <dd>{r.embeddingsGenerated}</dd>
              <dt>Skipped</dt>
              <dd>{r.issuesSkipped}</dd>
              <dt>Failed</dt>
              <dd>{r.failures}</dd>
            </dl>
          )}
        />

        <ActionCard
          title="Check duplicate"
          description="Checks a candidate title/body against existing issues. Read-only."
          buttonLabel="Check"
          disabled={!repoReady || title.trim().length === 0}
          state={checkState}
          onRun={() => runAction('check-duplicate', setCheckState, () => checkDuplicate(owner, repository, title, body))}
          extraFields={
            <>
              <label>
                Title
                <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Issue title" />
              </label>
              <label>
                Body
                <textarea value={body} onChange={(e) => setBody(e.target.value)} placeholder="Issue body (optional)" rows={3} />
              </label>
            </>
          }
          renderResult={(r) => (
            <div>
              <p className="check-summary">
                <span className={`badge badge-${r.confidence.toLowerCase()}`}>{r.confidence}</span>
                {r.isPotentialDuplicate ? ' Potential duplicate found' : ' No likely duplicate'}
              </p>
              {r.candidates.length > 0 && (
                <table className="candidates">
                  <thead>
                    <tr>
                      <th>Issue</th>
                      <th>Similarity</th>
                      <th>Classification</th>
                    </tr>
                  </thead>
                  <tbody>
                    {r.candidates.map((c) => (
                      <tr key={c.issueNumber}>
                        <td>
                          <a href={c.url} target="_blank" rel="noreferrer">
                            #{c.issueNumber} {c.title}
                          </a>
                        </td>
                        <td>{(c.similarity * 100).toFixed(1)}%</td>
                        <td>
                          <span className={`badge badge-${c.classification.toLowerCase()}`}>{c.classification}</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              <p className="processing-info">
                {r.processing.embeddingModel} · threshold {r.processing.similarityThreshold.toFixed(2)} ·{' '}
                {r.processing.processingTimeMs}ms
              </p>
            </div>
          )}
        />
      </section>

      <section className="card activity">
        <h2>Activity</h2>
        {activity.length === 0 ? (
          <p className="hint">Nothing yet — run an action above and it'll show up here.</p>
        ) : (
          <ul className="activity-list">
            {activity.map((entry) => (
              <li key={entry.id} className={`activity-item activity-${entry.status}`}>
                <div className="activity-line">
                  <span className="activity-time">{entry.startedAt.toLocaleTimeString()}</span>
                  <span className="activity-kind">{entry.kind}</span>
                  <span className="activity-repo">
                    {entry.owner}/{entry.repository}
                  </span>
                  <span className="activity-duration">{entry.durationMs}ms</span>
                </div>
                <div className="activity-summary">{entry.summary}</div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function ActionCard<T extends IssueImportResult | EmbeddingGenerationResult | CheckDuplicateResponse>(props: {
  title: string
  description: string
  buttonLabel: string
  disabled: boolean
  state: AsyncState<T>
  onRun: () => void
  extraFields?: React.ReactNode
  renderResult: (data: T) => React.ReactNode
}) {
  const { title, description, buttonLabel, disabled, state, onRun, extraFields, renderResult } = props

  return (
    <section className="card action-card">
      <h2>{title}</h2>
      <p className="description">{description}</p>
      {extraFields && <div className="field-column">{extraFields}</div>}
      <button onClick={onRun} disabled={disabled || state.status === 'loading'}>
        {state.status === 'loading' ? 'Running…' : buttonLabel}
      </button>
      {state.status === 'error' && <p className="error">{state.message}</p>}
      {state.status === 'success' && <div className="result">{renderResult(state.data)}</div>}
    </section>
  )
}

export default App
