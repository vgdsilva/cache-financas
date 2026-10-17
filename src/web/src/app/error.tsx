'use client'

import { ConexaoPerdida } from '@/components/ConexaoPerdida'

export default function Error({ retry }: { error: Error; retry: () => void }) {
  return <ConexaoPerdida onRetry={retry} />
}
