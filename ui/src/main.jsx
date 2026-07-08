import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { LawList } from "./components/LawList.jsx";
import { TableOfContents } from "./components/TableOfContents.jsx";
import { LawArticle } from "./components/LawArticle.jsx";
import { assertOk, currentRoute, filterSections, findLaw, lawPath, scrollToTarget } from "./utils/lawRoutes.js";
import "./styles/main.scss";

function App() {
  const [laws, setLaws] = useState([]);
  const [activeLaw, setActiveLaw] = useState(null);
  const [model, setModel] = useState(null);
  const [status, setStatus] = useState("Loading wetten...");
  const [query, setQuery] = useState("");
  const [routeTarget, setRouteTarget] = useState("");

  useEffect(() => {
    let cancelled = false;
    fetch("/api/wetten")
      .then(assertOk)
      .then((response) => response.json())
      .then(async (items) => {
        if (cancelled) return;
        setLaws(items);
        const route = currentRoute();
        const selected = findLaw(items, route.lawSlug) ?? items[0] ?? null;
        if (!selected) {
          setStatus("No wetten found in /data.");
          return;
        }
        await loadLaw(selected, { replaceUrl: !route.lawSlug, targetId: route.targetId });
      })
      .catch((error) => !cancelled && setStatus(`Could not load wetten. ${error.message}`));
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const onPopState = async () => {
      const route = currentRoute();
      const selected = findLaw(laws, route.lawSlug) ?? laws[0] ?? null;
      if (!selected) return;
      if (selected.slug !== activeLaw?.slug) {
        await loadLaw(selected, { replaceUrl: false, targetId: route.targetId });
      } else {
        setRouteTarget(route.targetId);
        scrollToTarget(route.targetId, { retry: true });
      }
    };
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, [laws, activeLaw]);

  useEffect(() => {
    if (!model || !routeTarget) return;
    return scrollToTarget(routeTarget, { retry: true });
  }, [model, routeTarget]);

  async function loadLaw(law, options = {}) {
    const { replaceUrl = false, targetId = "" } = options;
    setStatus(`Loading ${law.title}...`);
    setActiveLaw(law);
    setModel(null);
    setQuery("");
    setRouteTarget(targetId);

    const parsedLaw = await fetch(`/api/wetten/${encodeURIComponent(law.slug)}/json`)
      .then(assertOk)
      .then((response) => response.json());

    setModel(parsedLaw);
    setStatus("");
    const nextPath = lawPath(law, targetId);
    if (replaceUrl) window.history.replaceState({}, "", nextPath);
    if (!replaceUrl && targetId) window.history.pushState({}, "", nextPath);
  }

  function selectLaw(law) {
    loadLaw(law)
      .then(() => window.history.pushState({}, "", lawPath(law)))
      .catch((error) => setStatus(`Could not load ${law.title}. ${error.message}`));
  }

  const filteredSections = useMemo(() => filterSections(model?.sections ?? [], query), [model, query]);

  return (
    <>
      <header className="app-header">
        <div>
          <p className="eyebrow">BWB XML viewer</p>
          <h1>{model?.shortTitle ?? "Wet Viewer"}</h1>
        </div>
        <a className="api-link" href="/swagger">Swagger</a>
      </header>

      <main className="app-shell">
        <aside className="sidebar" aria-label="Wetten">
          <LawList laws={laws} activeLaw={activeLaw} onSelectLaw={selectLaw} />
          <TableOfContents toc={model?.toc ?? []} activeLaw={activeLaw} />
        </aside>

        <section className="reader">
          <div className="toolbar">
            <label className="search">
              <span>Search</span>
              <input value={query} onChange={(event) => setQuery(event.target.value)} type="search" placeholder="Artikel, begrip, hoofdstuk..." />
            </label>
            <div className="meta-strip">
              {model?.inwerking && <span className="chip">In werking: {model.inwerking}</span>}
              {model && <span className="chip">{model.stats.articles} artikelen</span>}
              {model?.stats.chapters > 0 && <span className="chip">{model.stats.chapters} hoofdstukken</span>}
            </div>
          </div>

          <article className="document">
            <header className="document-header">
              <p className="eyebrow">{[model?.kind, model?.bwbId].filter(Boolean).join(" / ")}</p>
              <h2>{model?.shortTitle ?? status}</h2>
              {model?.longTitle && model.longTitle !== model.shortTitle && <p>{model.longTitle}</p>}
            </header>
            {status && !model && <div className="notice">{status}</div>}
            <div className="content">
              {filteredSections.map((section) => (
                <LawArticle key={section.id} section={section} activeLaw={activeLaw} />
              ))}
            </div>
          </article>
        </section>
      </main>
    </>
  );
}

createRoot(document.getElementById("root")).render(<App />);
