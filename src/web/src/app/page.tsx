import { Suspense } from 'react'

async function ApiStatus() {
  const base = process.env.API_INTERNAL_URL ?? 'http://localhost:3333'
  let status = 'offline'
  try {
    const res = await fetch(`${base}/health`, { cache: 'no-store' })
    status = (await res.json()).status
  } catch {}
  return <p className="mt-2">API: {status}</p>
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
