import type { NextConfig } from "next";

// Em producao o server e alcancado pela rede interna do compose (build arg no Dockerfile).
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:3333";

const nextConfig: NextConfig = {
  output: "standalone",
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
  // Browser fala so com o web (mesma origem); o web repassa /api/* ao server.
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${apiUrl}/:path*` }];
  },
};

export default nextConfig;
