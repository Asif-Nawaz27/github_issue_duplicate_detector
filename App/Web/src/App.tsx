import { useEffect, useState } from 'react'
import './App.css'
import { AddOwnerModal } from './AddOwnerModal'
import { ApiError, checkDuplicate, generateEmbeddings, importIssues, listOwners, listRepositoriesByOwner } from './api'
import { SearchableCombobox } from './SearchableCombobox'
import {
  AlertCircleIcon,
  CheckCircleIcon,
  ClockIcon,
  ExternalLinkIcon,
  FolderGitIcon,
  ImportIcon,
  PlusIcon,
  SearchIcon,
  SparkleIcon,
} from './icons'
import type {
  ActionKind,
  ActivityEntry,
  CheckDuplicateResponse,
  EmbeddingGenerationResult,
  IssueImportResult,
  Owner,
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

const actionMeta: Record<ActionKind, { label: string; icon: (props: { className?: string }) => React.ReactElement; tone: string }> = {
  import: { label: 'Import', icon: ImportIcon, tone: 'import' },
  'generate-embeddings': { label: 'Embed', icon: SparkleIcon, tone: 'embed' },
  'check-duplicate': { label: 'Check', icon: SearchIcon, tone: 'check' },
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

  const [owners, setOwners] = useState<Owner[]>([])
  const [ownersError, setOwnersError] = useState('')
  const [isAddOwnerOpen, setIsAddOwnerOpen] = useState(false)

  const [repositoryNames, setRepositoryNames] = useState<string[]>([])
  const [repositoriesError, setRepositoriesError] = useState('')

  useEffect(() => {
    listOwners()
      .then(setOwners)
      .catch((err) => setOwnersError(err instanceof ApiError ? err.message : 'Failed to load owners — is the API running?'))
  }, [])

  // Repository suggestions are scoped to the selected owner, so re-fetch (and drop any
  // previously-selected repository, which likely belongs to a different owner) whenever it changes.
  useEffect(() => {
    setRepository('')
    setRepositoriesError('')

    if (!owner.trim()) {
      setRepositoryNames([])
      return
    }

    listRepositoriesByOwner(owner)
      .then(setRepositoryNames)
      .catch((err) => setRepositoriesError(err instanceof ApiError ? err.message : 'Failed to load repositories — is the API running?'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [owner])

  function handleOwnerCreated(newOwner: Owner) {
    setOwners((prev) => [...prev, newOwner].sort((a, b) => a.name.localeCompare(b.name)))
    setOwner(newOwner.name)
    setIsAddOwnerOpen(false)
  }

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
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">IS</span>
          <div className="brand-text">
            <span className="brand-name">IssueSense</span>
            <span className="brand-tag">Duplicate detection console</span>
          </div>
        </div>
        <div className={`repo-chip ${repoReady ? 'repo-chip-active' : ''}`}>
          <span className="repo-chip-dot" />
          {repoReady ? `${owner}/${repository}` : 'No repository selected'}
        </div>
      </header>

      <section className="card toolbar-card">
        <div className="toolbar-card-head">
          <span className="toolbar-icon">
            <FolderGitIcon />
          </span>
          <div>
            <h2>Repository</h2>
            <p className="description">Choose the GitHub owner and repository to run actions against.</p>
          </div>
        </div>
        <div className="field-row">
          <label>
            Owner
            <div className="owner-field">
              <SearchableCombobox
                items={owners.map((o) => o.name)}
                value={owner}
                onChange={setOwner}
                placeholder="Search owners…"
                emptyItemsMessage="No owners yet"
              />
              <button type="button" className="secondary" onClick={() => setIsAddOwnerOpen(true)}>
                <PlusIcon />
                Add owner
              </button>
            </div>
          </label>
          <label>
            Repository
            <SearchableCombobox
              items={repositoryNames}
              value={repository}
              onChange={setRepository}
              placeholder="e.g. hello-world"
              emptyItemsMessage={owner.trim() ? 'No imported repositories yet — type a new one' : 'Choose an owner first'}
              allowFreeText
              disabled={!owner.trim()}
            />
          </label>
        </div>
        {ownersError && <p className="error">{ownersError}</p>}
        {repositoriesError && <p className="error">{repositoriesError}</p>}
        {!repoReady && <p className="hint">Choose an owner and enter a repository to enable the actions below.</p>}
      </section>

      <section className="actions-grid">
        <ActionCard
          kind="import"
          title="Import issues"
          description="Pulls every issue from GitHub into the local database."
          buttonLabel="Import"
          disabled={!repoReady}
          state={importState}
          onRun={() => runAction('import', setImportState, () => importIssues(owner, repository))}
          renderResult={(r) => (
            <div className="stat-grid">
              <Stat label="Discovered" value={r.issuesDiscovered} />
              <Stat label="Created" value={r.issuesCreated} emphasize />
              <Stat label="Updated" value={r.issuesUpdated} />
              <Stat label="Skipped" value={r.issuesSkipped} muted />
            </div>
          )}
        />

        <ActionCard
          kind="generate-embeddings"
          title="Generate embeddings"
          description="Embeds any imported issues that don't have one yet."
          buttonLabel="Generate"
          disabled={!repoReady}
          state={embedState}
          onRun={() => runAction('generate-embeddings', setEmbedState, () => generateEmbeddings(owner, repository))}
          renderResult={(r) => (
            <div className="stat-grid">
              <Stat label="Processed" value={r.totalIssuesProcessed} />
              <Stat label="Embedded" value={r.embeddingsGenerated} emphasize />
              <Stat label="Skipped" value={r.issuesSkipped} muted />
              <Stat label="Failed" value={r.failures} danger={r.failures > 0} />
            </div>
          )}
        />

        <ActionCard
          kind="check-duplicate"
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
                            <ExternalLinkIcon className="link-icon" />
                          </a>
                        </td>
                        <td>
                          <div className="similarity-cell">
                            <span className="similarity-track">
                              <span className="similarity-fill" style={{ width: `${(c.similarity * 100).toFixed(1)}%` }} />
                            </span>
                            {(c.similarity * 100).toFixed(1)}%
                          </div>
                        </td>
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
        <div className="activity-head">
          <h2>Activity</h2>
          {activity.length > 0 && <span className="activity-count">{activity.length}</span>}
        </div>
        {activity.length === 0 ? (
          <p className="hint">Nothing yet — run an action above and it'll show up here.</p>
        ) : (
          <ul className="activity-list">
            {activity.map((entry) => {
              const meta = actionMeta[entry.kind]
              const Icon = meta.icon
              return (
                <li key={entry.id} className={`activity-item activity-${entry.status}`}>
                  <span className={`activity-icon activity-icon-${entry.status}`}>
                    <Icon />
                  </span>
                  <div className="activity-body">
                    <div className="activity-line">
                      <span className="activity-kind">{meta.label}</span>
                      <span className="activity-repo">
                        {entry.owner}/{entry.repository}
                      </span>
                      <span className="activity-status-icon">
                        {entry.status === 'success' ? <CheckCircleIcon /> : <AlertCircleIcon />}
                      </span>
                    </div>
                    <div className="activity-summary">{entry.summary}</div>
                    <div className="activity-meta">
                      <ClockIcon />
                      {entry.startedAt.toLocaleTimeString()} · {entry.durationMs}ms
                    </div>
                  </div>
                </li>
              )
            })}
          </ul>
        )}
      </section>

      {isAddOwnerOpen && <AddOwnerModal onClose={() => setIsAddOwnerOpen(false)} onCreated={handleOwnerCreated} />}
    </div>
  )
}

function Stat(props: { label: string; value: number; emphasize?: boolean; muted?: boolean; danger?: boolean }) {
  const { label, value, emphasize, muted, danger } = props
  const cls = ['stat-tile']
  if (emphasize) cls.push('stat-tile-emphasize')
  if (muted) cls.push('stat-tile-muted')
  if (danger) cls.push('stat-tile-danger')
  return (
    <div className={cls.join(' ')}>
      <span className="stat-value">{value}</span>
      <span className="stat-label">{label}</span>
    </div>
  )
}

function ActionCard<T extends IssueImportResult | EmbeddingGenerationResult | CheckDuplicateResponse>(props: {
  kind: ActionKind
  title: string
  description: string
  buttonLabel: string
  disabled: boolean
  state: AsyncState<T>
  onRun: () => void
  extraFields?: React.ReactNode
  renderResult: (data: T) => React.ReactNode
}) {
  const { kind, title, description, buttonLabel, disabled, state, onRun, extraFields, renderResult } = props
  const meta = actionMeta[kind]
  const Icon = meta.icon

  return (
    <section className={`card action-card action-card-${meta.tone}`}>
      <div className="action-card-head">
        <span className="action-icon">
          <Icon />
        </span>
        <div>
          <h2>{title}</h2>
          <p className="description">{description}</p>
        </div>
      </div>
      {extraFields && <div className="field-column">{extraFields}</div>}
      <button onClick={onRun} disabled={disabled || state.status === 'loading'}>
        {state.status === 'loading' ? (
          <>
            <span className="spinner" />
            Running…
          </>
        ) : (
          buttonLabel
        )}
      </button>
      {state.status === 'error' && (
        <p className="error">
          <AlertCircleIcon />
          {state.message}
        </p>
      )}
      {state.status === 'success' && <div className="result">{renderResult(state.data)}</div>}
    </section>
  )
}

export default App
