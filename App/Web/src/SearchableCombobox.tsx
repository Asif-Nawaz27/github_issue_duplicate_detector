import { useEffect, useRef, useState } from 'react'
import { ChevronDownIcon } from './icons'

export function SearchableCombobox(props: {
  items: string[]
  value: string
  onChange: (value: string) => void
  placeholder: string
  emptyItemsMessage?: string
  /** If true, typing directly sets the value (autocomplete); if false, a value is only set by picking an item from the list. */
  allowFreeText?: boolean
  disabled?: boolean
}) {
  const { items, value, onChange, placeholder, emptyItemsMessage, allowFreeText = false, disabled } = props
  const [isOpen, setIsOpen] = useState(false)
  const [query, setQuery] = useState(value)
  const containerRef = useRef<HTMLDivElement>(null)

  // Keep the visible text in sync with the selected value whenever the dropdown is closed
  // (e.g. the value changes from outside this component, or free text is committed elsewhere).
  useEffect(() => {
    if (!isOpen) setQuery(value)
  }, [value, isOpen])

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
        setQuery(value)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [value])

  const filtered = items.filter((item) => item.toLowerCase().includes(query.trim().toLowerCase()))

  function select(item: string) {
    onChange(item)
    setQuery(item)
    setIsOpen(false)
  }

  return (
    <div className="combobox" ref={containerRef}>
      <input
        value={query}
        disabled={disabled}
        placeholder={items.length === 0 ? (emptyItemsMessage ?? placeholder) : placeholder}
        role="combobox"
        aria-expanded={isOpen}
        onFocus={() => setIsOpen(true)}
        onChange={(e) => {
          const next = e.target.value
          setQuery(next)
          setIsOpen(true)
          if (allowFreeText) onChange(next)
        }}
      />
      <ChevronDownIcon className={isOpen ? 'combobox-chevron combobox-chevron-open' : 'combobox-chevron'} />
      {isOpen && (
        <div className="combobox-panel">
          {filtered.length === 0 ? (
            <div className="combobox-empty">
              {items.length === 0 ? (emptyItemsMessage ?? 'Nothing to choose from yet.') : `No matches for "${query}"`}
            </div>
          ) : (
            <ul className="combobox-list">
              {filtered.map((item) => (
                <li key={item}>
                  <button
                    type="button"
                    className={item === value ? 'combobox-option combobox-option-selected' : 'combobox-option'}
                    onClick={() => select(item)}
                  >
                    {item}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
