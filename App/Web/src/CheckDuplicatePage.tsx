import { useState } from 'react'
import { checkDuplicate } from './api'
import { AlertCircleIcon, ExternalLinkIcon, FileTextIcon, ListChecksIcon, SearchIcon } from './icons'
import type { ActionKind, AsyncState, CheckDuplicateResponse } from './types'

export function CheckDuplicatePage(props: {
  owner: string
  repository: string
  repoReady: boolean
  runAction: (
    kind: ActionKind,
    setState: (s: AsyncState<CheckDuplicateResponse>) => void,
    fn: () => Promise<CheckDuplicateResponse>,
  ) => Promise<void>
}) {
  const { owner, repository, repoReady, runAction } = props
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [checkState, setCheckState] = useState<AsyncState<CheckDuplicateResponse>>({ status: 'idle' })

  const disabled = !repoReady || title.trim().length === 0

  return (
    <div className="check-page">
      <div className="check-page-head">
        <span className="action-icon action-icon-lg">
          <SearchIcon />
        </span>
        <div>
          <h1>Check duplicate</h1>
          <p className="description">
            Checks a candidate title and body against issues already imported for the selected repository. Read-only —
            nothing is written to GitHub or the database.
          </p>
        </div>
      </div>

      <div className="check-page-grid">
        <section className="card check-form-card">
          <h2 className="card-title">
            <FileTextIcon />
            Candidate issue
          </h2>
          <div className="field-column">
            <label>
              Title
              <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Issue title" />
            </label>
            <label>
              Body
              <textarea value={body} onChange={(e) => setBody(e.target.value)} placeholder="Issue body (optional)" rows={8} />
            </label>
          </div>
          <button
            onClick={() => runAction('check-duplicate', setCheckState, () => checkDuplicate(owner, repository, title, body))}
            disabled={disabled || checkState.status === 'loading'}
          >
            {checkState.status === 'loading' ? (
              <>
                <span className="spinner" />
                Checking…
              </>
            ) : (
              'Check for duplicates'
            )}
          </button>
          {!repoReady && <p className="hint">Choose an owner and repository above to enable this check.</p>}
          {checkState.status === 'error' && (
            <p className="error">
              <AlertCircleIcon />
              {checkState.message}
            </p>
          )}
        </section>

        <section className="card check-results-card">
          <h2 className="card-title">
            <ListChecksIcon />
            Result
          </h2>
          {checkState.status === 'idle' && (
            <div className="empty-state">
              <SearchIcon />
              <p>Fill in a title and run the check — candidates will show up here.</p>
            </div>
          )}
          {checkState.status === 'loading' && (
            <div className="empty-state">
              <span className="spinner spinner-lg" />
              <p>Comparing against imported issues…</p>
            </div>
          )}
          {checkState.status === 'error' && (
            <div className="empty-state">
              <AlertCircleIcon />
              <p>The last check failed — see the error on the left.</p>
            </div>
          )}
          {checkState.status === 'success' && <CheckResult data={checkState.data} />}
        </section>
      </div>
    </div>
  )
}

function CheckResult(props: { data: CheckDuplicateResponse }) {
  const r = props.data
  return (
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
        {r.processing.embeddingModel} · threshold {r.processing.similarityThreshold.toFixed(2)} · {r.processing.processingTimeMs}
        ms
      </p>
    </div>
  )
}
