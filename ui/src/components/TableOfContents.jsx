import { legalClick, legalPath } from "../utils/lawRoutes.js";

export function TableOfContents({ toc, activeLaw }) {
  return (
    <section className="panel">
      <h2>Inhoud</h2>
      <nav className="toc" aria-label="Inhoudsopgave">
        {toc.map((entry) => (
          <a key={`${entry.id}-${entry.depth}`} href={legalPath(activeLaw, entry.id)} style={{ "--depth": Math.min(entry.depth, 3) }} onClick={legalClick}>
            <strong>{entry.title}</strong>
            <span>{entry.kind}</span>
          </a>
        ))}
      </nav>
    </section>
  );
}
