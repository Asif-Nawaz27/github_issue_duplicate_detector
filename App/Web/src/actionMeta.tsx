import { ImportIcon, SearchIcon, SparkleIcon } from './icons'
import type { ActionKind } from './types'

export const actionMeta: Record<ActionKind, { label: string; icon: (props: { className?: string }) => React.ReactElement; tone: string }> = {
  import: { label: 'Import', icon: ImportIcon, tone: 'import' },
  'generate-embeddings': { label: 'Embed', icon: SparkleIcon, tone: 'embed' },
  'check-duplicate': { label: 'Check', icon: SearchIcon, tone: 'check' },
}
