# Wet Viewer

Run with Docker Compose:

```powershell
docker compose up --build
```

Open http://localhost:8080.

The solution is split into two projects:

- `ui/`: Node + React application served by nginx.
- `backend/`: .NET Core API reading BWB XML files from `/data`.

The local `data` directory is mounted into the backend container as `/data`.

Swagger/OpenAPI is available at http://localhost:8080/swagger.

Add or remove BWB XML files in `data/` and refresh the page. Only wetten from that folder are shown.
