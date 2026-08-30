# Nomopsis

Nomopsis is a browser-based viewer and API for Dutch legislation stored as BWB XML. It discovers XML files from a local data directory, extracts their metadata and document structure, and presents the laws with navigation, search, stable article links, and access to the original XML or parsed JSON.

## Features

- Lists the available laws and their BWB metadata.
- Renders chapters, sections, articles, paragraphs, lists, and inline formatting.
- Provides table-of-contents navigation and client-side document search.
- Supports direct links to laws and articles.
- Returns a complete law or a selected article, paragraph, or subparagraph as XML or JSON.
- Includes an OpenAPI document and Swagger UI.

## Architecture

Nomopsis consists of two applications:

- `ui/` contains the React 19 and Vite frontend. Its production image is served by nginx.
- `backend/` contains the ASP.NET Core 9 API and BWB XML parser.
- `backend.Tests/` contains the backend parser and filtering checks.
- `data/` is the local, untracked directory from which laws are loaded.

In the Docker Compose setup, nginx serves the UI and proxies `/api`, `/health`, and `/swagger` to the backend. The local `data/` directory is mounted read-only at `/data` in the backend container.

## Quick start with Docker

Requirements:

- Docker with Docker Compose
- One or more BWB XML files (optional, but required to display laws)

Create the data directory if it does not exist and place the XML files in it:

```text
data/
├── BWBR0001854.xml
└── BWBR0039896_2024-07-01_0.xml
```

Start Nomopsis from the repository root:

```bash
docker compose up --build
```

Then open:

- Application: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

Stop the application with `Ctrl+C`, followed by:

```bash
docker compose down
```

Changes to files in `data/` are visible after refreshing the browser; rebuilding the containers is not required.

## Container images

The CI/CD workflow builds and publishes two images to GitHub Container Registry:

- `ghcr.io/endeavoury/nomopsis-backend`
- `ghcr.io/endeavoury/nomopsis-ui`

Pull requests build both images without publishing them. Pushes to `master`, version tags such as `v1.2.3`, and manually dispatched runs publish the images. Published tags include the branch or semantic version and an immutable `sha-<commit>` tag. A successful build from the default branch also updates `latest`.

For private packages, authenticate before pulling:

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u USERNAME --password-stdin
docker pull ghcr.io/endeavoury/nomopsis-backend:latest
docker pull ghcr.io/endeavoury/nomopsis-ui:latest
```

The token needs at least the `read:packages` scope. Images built from a fork are published under that fork owner's GHCR namespace.

## BWB XML data

Nomopsis scans the configured data directory for top-level `*.xml` files. It uses the root `bwb-id` attribute as the law slug. If that attribute is absent, a `BWBR` identifier is extracted from the filename; otherwise, the filename without its extension is used.

The displayed title is taken from `citeertitel`, then `intitule`, and finally the filename. Invalid XML files remain visible in the law list with their parsing error included in the metadata.

The default data location is `/data`. Override it for a locally running backend with the `DATA_PATH` environment variable or the ASP.NET Core configuration key `Data:Path`.

## API

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/health` | Returns the backend health status. |
| `GET` | `/api/wetten` | Lists all discovered laws. |
| `GET` | `/api/wetten/{slug}/metadata` | Returns metadata for one law. |
| `GET` | `/api/wetten/{slug}/xml` | Returns the original law as XML. |
| `GET` | `/api/wetten/{slug}/json` | Returns the parsed document as JSON. |
| `GET` | `/swagger/v1/swagger.json` | Returns the OpenAPI document. |

The XML and JSON document routes accept an optional `artikel` query parameter:

```bash
curl "http://localhost:8080/api/wetten/BWBR0039896/json?artikel=47"
curl "http://localhost:8080/api/wetten/BWBR0039896/xml?artikel=47.1"
curl "http://localhost:8080/api/wetten/BWBR0039896/json?artikel=47.1a"
```

Supported references are:

- `47` for an article;
- `47.1` for a paragraph (`lid`);
- `47.1a` for a lettered subparagraph.

## Local development

Local development requires Node.js 22 or newer and the .NET 9 SDK.

Start the backend on the port expected by the Vite proxy:

```bash
DATA_PATH="$PWD/data" ASPNETCORE_URLS=http://localhost:5080 \
  dotnet run --project backend/Nomopsis.Api.csproj
```

In another terminal, start the frontend:

```bash
cd ui
npm install
npm run dev
```

Open http://localhost:5173. Vite proxies API, health, and Swagger requests to `http://localhost:5080`.

To create a production frontend build:

```bash
cd ui
npm run build
```

## Tests

Run the backend checks from the repository root:

```bash
dotnet run --project backend.Tests/Nomopsis.Api.Tests.csproj
```

The checks cover BWB identifiers derived from filenames, XML parsing and inline content, supported article-reference formats, and filtering a document to a requested legal section.

## Troubleshooting

### No laws are shown

Confirm that XML files exist directly inside `data/` and that their filenames end in `.xml`. With Docker Compose, verify that the backend can see them:

```bash
docker compose exec backend ls -la /data
```

### The frontend cannot reach the API during local development

The Vite development server expects the backend at `http://localhost:5080`. Start the backend with the `ASPNETCORE_URLS` value shown above, or update the proxy targets in `ui/vite.config.js`.

### A law has the wrong title or identifier

Check the XML document's `bwb-id`, `citeertitel`, and `intitule` values. When `bwb-id` is missing, make sure the filename contains the intended `BWBR` identifier.
