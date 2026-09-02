import { useEffect, useState } from 'react'
import './App.css'
import { AddOwnerModal } from './AddOwnerModal'
import { CheckDuplicatePage } from './CheckDuplicatePage'
import { Dashboard } from './Dashboard'
import { ApiError, listOwners, listRepositoriesByOwner } from './api'
import { FolderGitIcon, LayoutGridIcon, PlusIcon, SearchIcon } from './icons'
import { SearchableCombobox } from './SearchableCombobox'
import type {
  ActionKind,
  ActivityEntry,
  AsyncState,
  CheckDuplicateResponse,
  EmbeddingGenerationResult,
  IssueImportResult,
  Owner,
} from './types'

type Page = 'dashboard' | 'check-duplicate'

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
  const [page, setPage] = useState<Page>('dashboard')

  const [owner, setOwner] = useState('')
  const [repository, setRepository] = useState('')

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

      <nav className="page-nav">
        <button
          type="button"
          className={page === 'dashboard' ? 'nav-tab nav-tab-active' : 'nav-tab'}
          onClick={() => setPage('dashboard')}
        >
          <LayoutGridIcon />
          Dashboard
        </button>
        <button
          type="button"
          className={page === 'check-duplicate' ? 'nav-tab nav-tab-active' : 'nav-tab'}
          onClick={() => setPage('check-duplicate')}
        >
          <SearchIcon />
          Check duplicate
        </button>
      </nav>

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

      {page === 'dashboard' ? (
        <Dashboard owner={owner} repository={repository} repoReady={repoReady} activity={activity} runAction={runAction} />
      ) : (
        <CheckDuplicatePage owner={owner} repository={repository} repoReady={repoReady} runAction={runAction} />
      )}

      {isAddOwnerOpen && <AddOwnerModal onClose={() => setIsAddOwnerOpen(false)} onCreated={handleOwnerCreated} />}
    </div>
  )
}

export default App
