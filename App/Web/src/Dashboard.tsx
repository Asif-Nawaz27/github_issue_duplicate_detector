import { useState } from 'react'
import { actionMeta } from './actionMeta'
import { generateEmbeddings, importIssues } from './api'
import { AlertCircleIcon, CheckCircleIcon, ClockIcon } from './icons'
import type { ActionKind, ActivityEntry, AsyncState, EmbeddingGenerationResult, IssueImportResult } from './types'

export function Dashboard(props: {
  owner: string
  repository: string
  repoReady: boolean
  activity: ActivityEntry[]
  runAction: <T extends IssueImportResult | EmbeddingGenerationResult>(
    kind: ActionKind,
    setState: (s: AsyncState<T>) => void,
    fn: () => Promise<T>,
  ) => Promise<void>
}) {
  const { owner, repository, repoReady, activity, runAction } = props

  const [importState, setImportState] = useState<AsyncState<IssueImportResult>>({ status: 'idle' })
  const [embedState, setEmbedState] = useState<AsyncState<EmbeddingGenerationResult>>({ status: 'idle' })

  // Check-duplicate runs show their result inline on their own page, so keep them out of this feed.
  const dashboardActivity = activity.filter((entry) => entry.kind !== 'check-duplicate')

  return (
    <>
      <section className="actions-grid actions-grid-dashboard">
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
      </section>

      <section className="card activity">
        <div className="activity-head">
          <h2>Activity</h2>
          {dashboardActivity.length > 0 && <span className="activity-count">{dashboardActivity.length}</span>}
        </div>
        {dashboardActivity.length === 0 ? (
          <p className="hint">Nothing yet — run an action above and it'll show up here.</p>
        ) : (
          <ul className="activity-list">
            {dashboardActivity.map((entry) => {
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
    </>
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

function ActionCard<T extends IssueImportResult | EmbeddingGenerationResult>(props: {
  kind: ActionKind
  title: string
  description: string
  buttonLabel: string
  disabled: boolean
  state: AsyncState<T>
  onRun: () => void
  renderResult: (data: T) => React.ReactNode
}) {
  const { kind, title, description, buttonLabel, disabled, state, onRun, renderResult } = props
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
