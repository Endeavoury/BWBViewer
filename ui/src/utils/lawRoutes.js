export function assertOk(response) {
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response;
}

export function currentRoute() {
  const parts = window.location.pathname.split("/").filter(Boolean).map(decodeURIComponent);
  return { lawSlug: parts[0] || "", targetId: parts[1] ? legalTargetFromPath(parts.slice(1).join("-")) : "" };
}

export function legalPath(law, targetId = "") {
  if (!law) return "/";
  return `/${encodeURIComponent(law.slug)}${targetId ? `/${encodeURIComponent(targetId)}` : ""}`;
}

export function lawPath(law, targetId = "") {
  return legalPath(law, targetId);
}

export function findLaw(laws, slug) {
  if (!slug) return null;
  return laws.find((law) => law.slug.toLowerCase() === slug.toLowerCase());
}

export function legalClick(event) {
  if (event.currentTarget.origin !== window.location.origin) return;
  const currentLaw = currentRoute().lawSlug.toLowerCase();
  const nextLaw = event.currentTarget.pathname.split("/").filter(Boolean)[0]?.toLowerCase() ?? "";
  if (nextLaw && nextLaw !== currentLaw) return;
  event.preventDefault();
  window.history.pushState({}, "", event.currentTarget.pathname);
  scrollToTarget(currentRoute().targetId, { retry: true });
}

export function scrollToTarget(targetId, options = {}) {
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
    if (attempts > 0) frame = window.requestAnimationFrame(tryScroll);
  };

  frame = window.requestAnimationFrame(tryScroll);
  return () => {
    if (frame) window.cancelAnimationFrame(frame);
  };
}

export function hrefFromLegalDoc(doc = "", fallbackBwbId = "") {
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

export function legalReference(articleNumber, pathParts = []) {
  if (!articleNumber) return "";
  const suffix = pathParts.map((part, index) => index === 0 && /^\d/.test(part) ? `.${part}` : part).join("");
  return `Artikel ${articleNumber}${suffix}`;
}

export function legalAnchor(parts) {
  return parts.filter(Boolean).map((part) => normalizeLegalPart(part)).join("-");
}

export function legalTargetFromPath(value = "") {
  const normalized = normalizeLegalPart(value);
  return normalized.startsWith("artikel-") ? normalized : `artikel-${normalized}`;
}

export function filterSections(sections, query) {
  const normalized = normalizeText(query);
  if (!normalized) return sections;
  return sections.map((section) => filterSection(section, normalized)).filter(Boolean);
}

export function normalizeLegalPart(value = "") {
  return String(value).trim().replace(/\u00b0/g, "").replace(/\u00ba/g, "").replace(/Â°/g, "").replace(/\.$/, "").replace(/\s+/g, "-").replace(/[^a-zA-Z0-9.-]/g, "").toLowerCase();
}

export function cssName(name) {
  return String(name || "").replace(/[^a-zA-Z0-9_-]/g, "-").toLowerCase();
}

function filterSection(section, normalized) {
  const ownMatch = normalizeText(`${section.title} ${(section.bodyNodes ?? []).map(textOf).join(" ")}`).includes(normalized);
  if (section.type === "article") return ownMatch ? section : null;
  const children = section.children.map((child) => filterSection(child, normalized)).filter(Boolean);
  return ownMatch || children.length ? { ...section, children } : null;
}

function textOf(node) {
  return `${node?.text ?? ""} ${(node?.children ?? []).map(textOf).join(" ")}`;
}

function normalizeText(value = "") {
  return value.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/\s+/g, " ").trim();
}
