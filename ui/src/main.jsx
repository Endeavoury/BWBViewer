import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
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
        scrollToTarget(route.targetId);
      }
    };
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, [laws, activeLaw]);

  async function loadLaw(law, options = {}) {
    const { replaceUrl = false, targetId = "" } = options;
    setStatus(`Loading ${law.title}...`);
    setActiveLaw(law);
    setModel(null);
    setQuery("");
    setRouteTarget(targetId);
    const xmlText = await fetch(law.xmlUrl).then(assertOk).then((response) => response.text());
    const parsed = parseXml(xmlText);
    if (parsed.error) throw new Error(parsed.error);
    const nextModel = buildModel(parsed.doc, law);
    setModel(nextModel);
    setStatus("");
    const nextPath = lawPath(law, targetId);
    if (replaceUrl) window.history.replaceState({}, "", nextPath);
    if (!replaceUrl && targetId) window.history.pushState({}, "", nextPath);
  }

  function selectLaw(law) {
    loadLaw(law, { replaceUrl: false }).then(() => {
      window.history.pushState({}, "", lawPath(law));
    }).catch((error) => setStatus(`Could not load ${law.title}. ${error.message}`));
  }

  const filteredSections = useMemo(() => filterSections(model?.sections ?? [], query), [model, query]);

  useEffect(() => {
    if (!model || !routeTarget) return;
    const cancel = scrollToTarget(routeTarget, { retry: true });
    return cancel;
  }, [model, routeTarget]);

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
          <section className="panel">
            <h2>Wetten</h2>
            <div className="law-list" role="list">
              {laws.map((law) => (
                <button
                  key={law.slug}
                  type="button"
                  className={`law-item${law.slug === activeLaw?.slug ? " active" : ""}`}
                  onClick={() => selectLaw(law)}
                >
                  <strong>{law.title}</strong>
                  <span>{law.bwbId} {law.effectiveDate ? `- ${law.effectiveDate}` : ""}</span>
                </button>
              ))}
            </div>
          </section>
          <section className="panel">
            <h2>Inhoud</h2>
            <nav className="toc" aria-label="Inhoudsopgave">
              {(model?.toc ?? []).map((entry) => (
                <a key={`${entry.id}-${entry.depth}`} href={legalPath(activeLaw, entry.id)} style={{ "--depth": Math.min(entry.depth, 3) }} onClick={legalClick}>
                  <strong>{entry.title}</strong>
                  <span>{entry.kind}</span>
                </a>
              ))}
            </nav>
          </section>
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
                <LawSection key={section.id} section={section} activeLaw={activeLaw} />
              ))}
            </div>
          </article>
        </section>
      </main>
    </>
  );
}

function LawSection({ section, activeLaw }) {
  if (section.type === "article") {
    return (
      <section id={section.id} className={`article${section.isAuthority ? " article-authority" : ""}`} data-xml-id={section.xmlId}>
        <div className="article-heading">
          <h3>{section.title}</h3>
          {section.isAuthority && <span className="authority-badge">Bevoegdheid</span>}
          <a className="self-link" href={legalPath(activeLaw, section.id)} onClick={legalClick} aria-label={`Link to ${section.reference}`}>#</a>
        </div>
        {section.bodyNodes.map((node, index) => (
          <XmlNode key={index} node={node} context={{ articleNumber: section.articleNumber, pathParts: [] }} activeLaw={activeLaw} />
        ))}
      </section>
    );
  }

  return (
    <section id={section.id} className="section">
      {React.createElement(section.level <= 1 ? "h2" : "h3", { className: "section-title" }, section.title)}
      {section.bodyNodes.map((node, index) => <XmlNode key={index} node={node} context={{}} activeLaw={activeLaw} />)}
      {section.children.map((child) => <LawSection key={child.id} section={child} activeLaw={activeLaw} />)}
    </section>
  );
}

function XmlNode({ node, context, activeLaw }) {
  if (node.localName === "lid") return <LidNode node={node} context={context} activeLaw={activeLaw} />;
  if (node.localName === "lijst") return <ListNode node={node} context={context} activeLaw={activeLaw} />;
  if (["al", "considerans.al", "wij"].includes(node.localName)) return <p>{inlineNodes(node, activeLaw)}</p>;
  if (node.localName === "meta-data" || node.localName === "kop") return null;

  const children = childElements(node).filter((child) => !["meta-data", "kop"].includes(child.localName));
  const direct = directText(node);
  return (
    <div className={`xml-block xml-${cssName(node.localName)}`}>
      {direct && <p>{inlineNodes(node, activeLaw)}</p>}
      {children.map((child, index) => <XmlNode key={index} node={child} context={context} activeLaw={activeLaw} />)}
    </div>
  );
}

function LidNode({ node, context, activeLaw }) {
  const lidNumber = directText(childElements(node).find((child) => child.localName === "lidnr"));
  const pathParts = lidNumber ? [lidNumber] : [...(context.pathParts ?? [])];
  const reference = legalReference(context.articleNumber, pathParts);
  const id = reference ? legalAnchor(["artikel", context.articleNumber, ...pathParts]) : "";
  return (
    <section id={id || undefined} className="lid-block">
      {reference && <div className="legal-ref"><a href={legalPath(activeLaw, id)} onClick={legalClick}>{reference}</a></div>}
      {childElements(node).filter((child) => !["lidnr", "meta-data"].includes(child.localName)).map((child, index) => (
        <XmlNode key={index} node={child} context={{ ...context, pathParts }} activeLaw={activeLaw} />
      ))}
    </section>
  );
}

function ListNode({ node, context, activeLaw }) {
  return (
    <ol className="law-list-block">
      {childElements(node).filter((child) => child.localName === "li").map((li, index) => {
        const rawNumber = directText(childElements(li).find((child) => child.localName === "li.nr")) || "";
        const part = normalizeLegalPart(rawNumber);
        const pathParts = part ? [...(context.pathParts ?? []), part] : [...(context.pathParts ?? [])];
        const reference = legalReference(context.articleNumber, pathParts);
        const id = reference ? legalAnchor(["artikel", context.articleNumber, ...pathParts]) : "";
        return (
          <li key={index} id={id || undefined}>
            {reference ? <a className="li-nr" href={legalPath(activeLaw, id)} onClick={legalClick}>{reference}</a> : <span className="li-nr">{rawNumber}</span>}
            <div>
              {childElements(li).filter((child) => !["li.nr", "meta-data"].includes(child.localName)).map((child, childIndex) => (
                <XmlNode key={childIndex} node={child} context={{ ...context, pathParts }} activeLaw={activeLaw} />
              ))}
            </div>
          </li>
        );
      })}
    </ol>
  );
}

function assertOk(response) {
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response;
}

function parseXml(xmlText) {
  const doc = new DOMParser().parseFromString(xmlText, "application/xml");
  const parserError = doc.querySelector("parsererror");
  return parserError ? { error: parserError.textContent, doc } : { doc };
}

function buildModel(doc, law) {
  const root = doc.documentElement;
  const wetgeving = doc.querySelector("wetgeving");
  const shortTitle = directText(doc.querySelector("citeertitel")) || directText(doc.querySelector("intitule")) || law.title;
  const longTitle = directText(doc.querySelector("intitule"));
  const wettekst = doc.querySelector("wettekst");
  const topNodes = wettekst ? childElements(wettekst) : childElements(wetgeving || root);
  const sections = topNodes
    .filter((node) => !["meta-data", "citeertitel", "intitule"].includes(node.localName))
    .map((node, index) => buildSection(node, index, []))
    .filter(Boolean);
  const toc = [];
  collectToc(sections, toc, 0);
  return {
    bwbId: law.bwbId,
    kind: law.kind,
    inwerking: law.effectiveDate,
    shortTitle,
    longTitle,
    sections,
    toc,
    stats: {
      articles: doc.querySelectorAll("artikel").length,
      chapters: doc.querySelectorAll("hoofdstuk").length
    }
  };
}

function buildSection(node, index, context = []) {
  if (node.localName === "artikel") return buildArticle(node, index, context);
  const children = childElements(node).filter((child) => child.localName !== "kop" && child.localName !== "meta-data");
  const title = headingText(node) || readableName(node.localName);
  const nextContext = [...context, title];
  return {
    type: "section",
    id: safeId(node.getAttribute("id") || `section-${index}`),
    level: sectionLevel(node.localName),
    nodeName: node.localName,
    title,
    bodyNodes: children.filter((child) => child.localName !== "artikel" && !isContainer(child)),
    children: children.filter((child) => child.localName === "artikel" || isContainer(child)).map((child, childIndex) => child.localName === "artikel" ? buildArticle(child, childIndex, nextContext) : buildSection(child, childIndex, nextContext)).filter(Boolean)
  };
}

function buildArticle(node, index, context = []) {
  const articleNumber = articleNumberFromNode(node);
  const title = headingText(node) || node.getAttribute("label") || "Artikel";
  return {
    type: "article",
    id: articleNumber ? legalAnchor(["artikel", articleNumber]) : safeId(node.getAttribute("id") || `article-${index}`),
    xmlId: node.getAttribute("id") || "",
    title,
    articleNumber,
    reference: articleNumber ? `Artikel ${articleNumber}` : title,
    isAuthority: context.some((titlePart) => normalizeText(titlePart).includes("bevoegdhed")),
    bodyNodes: childElements(node).filter((child) => child.localName !== "kop" && child.localName !== "meta-data")
  };
}

function collectToc(items, entries, depth) {
  items.forEach((item) => {
    entries.push({ id: item.id, title: item.title, kind: item.type === "article" ? "Artikel" : readableName(item.nodeName), depth });
    if (item.children) collectToc(item.children, entries, depth + 1);
  });
}

function filterSections(sections, query) {
  const normalized = normalizeText(query);
  if (!normalized) return sections;
  return sections.map((section) => filterSection(section, normalized)).filter(Boolean);
}

function filterSection(section, normalized) {
  const ownMatch = normalizeText(`${section.title} ${(section.bodyNodes ?? []).map(textOf).join(" ")}`).includes(normalized);
  if (section.type === "article") return ownMatch ? section : null;
  const children = section.children.map((child) => filterSection(child, normalized)).filter(Boolean);
  return ownMatch || children.length ? { ...section, children } : null;
}

function inlineNodes(node, activeLaw) {
  return Array.from(node.childNodes).map((child, index) => {
    if (child.nodeType === Node.TEXT_NODE) return child.textContent.replace(/\s+/g, " ");
    if (child.nodeType !== Node.ELEMENT_NODE || child.localName === "meta-data") return null;
    const text = textOf(child);
    if (child.localName === "nadruk") return <span key={index} className={child.getAttribute("type") === "vet" ? "strong" : "emphasis"}>{text}</span>;
    if (["intref", "extref"].includes(child.localName)) {
      const href = child.localName === "intref" ? hrefFromLegalDoc(child.getAttribute("doc") || "", child.getAttribute("bwb-id") || activeLaw?.bwbId || "") : "";
      return href ? <a key={index} className="ref" href={href} onClick={legalClick}>{text}</a> : <span key={index} className="ref">{text}</span>;
    }
    return text;
  });
}

function legalClick(event) {
  if (event.currentTarget.origin !== window.location.origin) return;
  const currentLaw = currentRoute().lawSlug.toLowerCase();
  const nextLaw = event.currentTarget.pathname.split("/").filter(Boolean)[0]?.toLowerCase() ?? "";
  if (nextLaw && nextLaw !== currentLaw) return;
  event.preventDefault();
  window.history.pushState({}, "", event.currentTarget.pathname);
  scrollToTarget(currentRoute().targetId, { retry: true });
}

function currentRoute() {
  const parts = window.location.pathname.split("/").filter(Boolean).map(decodeURIComponent);
  return { lawSlug: parts[0] || "", targetId: parts[1] ? legalTargetFromPath(parts.slice(1).join("-")) : "" };
}

function legalPath(law, targetId = "") {
  if (!law) return "/";
  return `/${encodeURIComponent(law.slug)}${targetId ? `/${encodeURIComponent(targetId)}` : ""}`;
}

function lawPath(law, targetId = "") {
  return legalPath(law, targetId);
}

function findLaw(laws, slug) {
  if (!slug) return null;
  return laws.find((law) => law.slug.toLowerCase() === slug.toLowerCase());
}

function scrollToTarget(targetId, options = {}) {
  if (!targetId) return;
  const { retry = false } = options;
  let attempts = retry ? 20 : 1;
  let frame = 0;

  const tryScroll = () => {
    const target = document.getElementById(targetId);
    if (target) {
      target.scrollIntoView({ block: "start" });
      return;
    }
    attempts -= 1;
    if (attempts > 0) {
      frame = window.requestAnimationFrame(tryScroll);
    }
  };

  frame = window.requestAnimationFrame(tryScroll);
  return () => {
    if (frame) window.cancelAnimationFrame(frame);
  };
}

function hrefFromLegalDoc(doc = "", fallbackBwbId = "") {
  const normalized = doc.replace(/&amp;/g, "&");
  const query = normalized.includes("?") ? normalized.slice(normalized.indexOf("?") + 1) : normalized;
  const params = new URLSearchParams(query);
  const article = params.get("artikel");
  if (!article) return "";
  const wetSlug = fallbackBwbId || (normalized.match(/c:(BWBR\d+)/i) || [])[1] || "";
  const parts = ["artikel", article];
  const lid = params.get("lid");
  if (lid) parts.push(lid);
  params.getAll("o").forEach((part) => parts.push(part));
  return `/${encodeURIComponent(wetSlug.toUpperCase())}/${encodeURIComponent(legalAnchor(parts))}`;
}

function legalReference(articleNumber, pathParts = []) {
  if (!articleNumber) return "";
  const suffix = pathParts.map((part, index) => index === 0 && /^\d/.test(part) ? `.${part}` : part).join("");
  return `Artikel ${articleNumber}${suffix}`;
}

function legalAnchor(parts) {
  return parts.filter(Boolean).map((part) => normalizeLegalPart(part)).join("-");
}

function legalTargetFromPath(value = "") {
  const normalized = normalizeLegalPart(value);
  return normalized.startsWith("artikel-") ? normalized : `artikel-${normalized}`;
}

function normalizeLegalPart(value = "") {
  return String(value).trim().replace(/\u00b0/g, "").replace(/\u00ba/g, "").replace(/Â°/g, "").replace(/\.$/, "").replace(/\s+/g, "-").replace(/[^a-zA-Z0-9.-]/g, "").toLowerCase();
}

function articleNumberFromNode(node) {
  const kop = childElements(node).find((child) => child.localName === "kop");
  const nr = directText(childElements(kop).find((child) => child.localName === "nr"));
  const label = node.getAttribute("label") || "";
  return nr || (label.match(/\d+[a-z]?/i) || [""])[0];
}

function headingText(node) {
  const kop = childElements(node).find((child) => child.localName === "kop");
  if (!kop) return node.getAttribute("label") || "";
  const label = directText(childElements(kop).find((child) => child.localName === "label"));
  const nr = directText(childElements(kop).find((child) => child.localName === "nr"));
  const title = directText(childElements(kop).find((child) => child.localName === "titel"));
  return [label, nr, title].filter(Boolean).join(" ");
}

function directText(node) {
  if (!node) return "";
  return Array.from(node.childNodes).filter((child) => child.nodeType === Node.TEXT_NODE).map((child) => child.textContent).join(" ").replace(/\s+/g, " ").trim();
}

function textOf(node) {
  return (node?.textContent || "").replace(/\s+/g, " ").trim();
}

function childElements(node) {
  return Array.from(node?.children || []);
}

function isContainer(node) {
  return ["hoofdstuk", "paragraaf", "sub-paragraaf", "afdeling", "titeldeel"].includes(node.localName);
}

function sectionLevel(name) {
  return { hoofdstuk: 1, paragraaf: 2, "sub-paragraaf": 3, afdeling: 2, titeldeel: 1 }[name] || 2;
}

function readableName(name = "") {
  return name.replace(/-/g, " ").replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function safeId(id) {
  return String(id || "").replace(/[^a-zA-Z0-9_-]/g, "-");
}

function cssName(name) {
  return safeId(name).toLowerCase();
}

function normalizeText(value = "") {
  return value.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/\s+/g, " ").trim();
}

createRoot(document.getElementById("root")).render(<App />);
