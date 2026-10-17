'use client'

import { useState } from 'react'

export function ConexaoPerdida({ onRetry }: { onRetry: () => void }) {
  const [tentando, setTentando] = useState(false)

  function tentarNovamente() {
    setTentando(true)
    onRetry()
    // A tela some sozinha se a reconexao funcionar; se nao, libera o botao de novo.
    setTimeout(() => setTentando(false), 1500)
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center px-6 text-center">
      <svg
        viewBox="0 0 200 160"
        className="h-40 w-52 text-zinc-300 dark:text-zinc-700"
        fill="none"
        aria-hidden="true"
      >
        <circle cx="100" cy="80" r="62" fill="currentColor" opacity="0.35" />
        <rect x="52" y="52" width="96" height="56" rx="10" fill="white" className="dark:fill-zinc-900" stroke="#71717a" strokeWidth="3" />
        <circle cx="68" cy="68" r="4" fill="#ef4444" />
        <circle cx="82" cy="68" r="4" fill="#f59e0b" />
        <path d="M70 94h20M104 94h26" stroke="#a1a1aa" strokeWidth="4" strokeLinecap="round" />
        <path d="M100 120v12M88 132h24" stroke="#71717a" strokeWidth="3" strokeLinecap="round" />
        <path d="M150 38l10 10m0-10l-10 10" stroke="#ef4444" strokeWidth="4" strokeLinecap="round" />
      </svg>

      <h1 className="mt-8 text-2xl font-bold text-zinc-900 dark:text-zinc-50">
        Sem conexão com o servidor
      </h1>
      <p className="mt-3 max-w-sm text-zinc-600 dark:text-zinc-400">
        Não conseguimos falar com o servidor agora. Seus dados estão seguros. Verifique sua
        internet e tente novamente em instantes.
      </p>

      <button
        type="button"
        onClick={tentarNovamente}
        disabled={tentando}
        className="mt-8 rounded-full bg-zinc-900 px-8 py-3 font-medium text-white transition hover:bg-zinc-700 disabled:opacity-60 dark:bg-zinc-50 dark:text-zinc-900 dark:hover:bg-zinc-300"
      >
        {tentando ? 'Tentando...' : 'Tentar novamente'}
      </button>
    </main>
  )
}
