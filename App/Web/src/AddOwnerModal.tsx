import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, createOwner } from './api'
import type { Owner } from './types'

export function AddOwnerModal(props: { onClose: () => void; onCreated: (owner: Owner) => void }) {
  const { onClose, onCreated } = props
  const [name, setName] = useState('')
  const [status, setStatus] = useState<'idle' | 'saving' | 'error'>('idle')
  const [error, setError] = useState('')

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return

    setStatus('saving')
    try {
      const owner = await createOwner(trimmed)
      onCreated(owner)
    } catch (err) {
      setStatus('error')
      setError(err instanceof ApiError ? err.message : 'Failed to create owner — is the API running?')
    }
  }

  return (
    <div
      className="modal-overlay"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose()
      }}
    >
      <div className="modal" role="dialog" aria-modal="true" aria-labelledby="add-owner-title">
        <h2 id="add-owner-title">Add owner</h2>
        <form onSubmit={handleSubmit}>
          <label>
            Name
            {/* eslint-disable-next-line jsx-a11y/no-autofocus */}
            <input autoFocus value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. octocat" maxLength={256} />
          </label>
          {status === 'error' && <p className="error">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" disabled={status === 'saving' || name.trim().length === 0}>
              {status === 'saving' ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
