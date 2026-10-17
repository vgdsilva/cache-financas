import { Suspense } from 'react'

async function ApiStatus() {
  const base = process.env.API_INTERNAL_URL ?? 'http://localhost:3333'
  // Falha propaga ate o error.tsx, que mostra a tela de conexao perdida
  const res = await fetch(`${base}/health`, { cache: 'no-store' })
  if (!res.ok) throw new Error(`API respondeu ${res.status}`)
  return <p className="mt-2">API: {(await res.json()).status}</p>
}

export default function Home() {
  return (
    <main className="p-8">
      <h1 className="text-2xl font-bold">Cache Finanças</h1>
      <Suspense fallback={<p className="mt-2">API: ...</p>}>
        <ApiStatus />
      </Suspense>
    </main>
  )
}
