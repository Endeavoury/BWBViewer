import { XmlNodeRenderer } from "./XmlNodeRenderer.jsx";
import { legalClick, legalPath } from "../utils/lawRoutes.js";

export function LawArticle({ section, activeLaw }) {
  if (section.type === "article") {
    return (
      <section id={section.id} className={`article${section.isAuthority ? " article-authority" : ""}`} data-xml-id={section.xmlId}>
        <div className="article-heading">
          <h3>{section.title}</h3>
          {section.isAuthority && <span className="authority-badge">Bevoegdheid</span>}
          <a className="self-link" href={legalPath(activeLaw, section.id)} onClick={legalClick} aria-label={`Link to ${section.reference}`}>#</a>
        </div>
        {section.bodyNodes.map((node, index) => (
          <XmlNodeRenderer key={index} node={node} context={{ articleNumber: section.articleNumber, pathParts: [] }} activeLaw={activeLaw} />
        ))}
      </section>
    );
  }

  return (
    <section id={section.id} className="section">
      {section.level <= 1 ? <h2 className="section-title">{section.title}</h2> : <h3 className="section-title">{section.title}</h3>}
      {section.bodyNodes.map((node, index) => <XmlNodeRenderer key={index} node={node} context={{}} activeLaw={activeLaw} />)}
      {section.children.map((child) => <LawArticle key={child.id} section={child} activeLaw={activeLaw} />)}
    </section>
  );
}
