// Healthcheck do proprio web, independente do server.
export function GET() {
  return Response.json({ status: 'ok' })
}
