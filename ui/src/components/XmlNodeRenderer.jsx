import { cssName, hrefFromLegalDoc, legalAnchor, legalClick, legalPath, legalReference, normalizeLegalPart } from "../utils/lawRoutes.js";

export function XmlNodeRenderer({ node, context, activeLaw }) {
  if (node.localName === "#text") return node.text;
  if (node.localName === "lid") return <LidNode node={node} context={context} activeLaw={activeLaw} />;
  if (node.localName === "lijst") return <ListNode node={node} context={context} activeLaw={activeLaw} />;
  if (["al", "considerans.al", "wij"].includes(node.localName)) return <p>{inlineNodes(node, activeLaw)}</p>;
  if (node.localName === "meta-data" || node.localName === "kop") return null;

  const children = node.children?.filter((child) => !["meta-data", "kop"].includes(child.localName)) ?? [];
  const direct = directText(node);
  return (
    <div className={`xml-block xml-${cssName(node.localName)}`}>
      {direct && <p>{inlineNodes(node, activeLaw)}</p>}
      {children.map((child, index) => <XmlNodeRenderer key={index} node={child} context={context} activeLaw={activeLaw} />)}
    </div>
  );
}

function LidNode({ node, context, activeLaw }) {
  const lidNumber = directText(node.children?.find((child) => child.localName === "lidnr"));
  const pathParts = lidNumber ? [lidNumber] : [...(context.pathParts ?? [])];
  const reference = legalReference(context.articleNumber, pathParts);
  const id = reference ? legalAnchor(["artikel", context.articleNumber, ...pathParts]) : "";
  return (
    <section id={id || undefined} className="lid-block">
      {reference && <div className="legal-ref"><a href={legalPath(activeLaw, id)} onClick={legalClick}>{reference}</a></div>}
      {(node.children ?? []).filter((child) => !["lidnr", "meta-data"].includes(child.localName)).map((child, index) => (
        <XmlNodeRenderer key={index} node={child} context={{ ...context, pathParts }} activeLaw={activeLaw} />
      ))}
    </section>
  );
}

function ListNode({ node, context, activeLaw }) {
  return (
    <ol className="law-list-block">
      {(node.children ?? []).filter((child) => child.localName === "li").map((li, index) => {
        const rawNumber = directText(li.children?.find((child) => child.localName === "li.nr")) || "";
        const part = normalizeLegalPart(rawNumber);
        const pathParts = part ? [...(context.pathParts ?? []), part] : [...(context.pathParts ?? [])];
        const reference = legalReference(context.articleNumber, pathParts);
        const id = reference ? legalAnchor(["artikel", context.articleNumber, ...pathParts]) : "";
        return (
          <li key={index} id={id || undefined}>
            {reference ? <a className="li-nr" href={legalPath(activeLaw, id)} onClick={legalClick}>{reference}</a> : <span className="li-nr">{rawNumber}</span>}
            <div>
              {(li.children ?? []).filter((child) => !["li.nr", "meta-data"].includes(child.localName)).map((child, childIndex) => (
                <XmlNodeRenderer key={childIndex} node={child} context={{ ...context, pathParts }} activeLaw={activeLaw} />
              ))}
            </div>
          </li>
        );
      })}
    </ol>
  );
}

function inlineNodes(node, activeLaw) {
  return (node.children ?? []).length
    ? node.children.map((child, index) => inlineChild(child, activeLaw, index))
    : node.text;
}

function inlineChild(child, activeLaw, index) {
  const text = child.text ?? "";
  if (child.localName === "nadruk") return <span key={index} className={child.attributes?.type === "vet" ? "strong" : "emphasis"}>{text}</span>;
  if (["intref", "extref"].includes(child.localName)) {
    const href = child.localName === "intref" ? hrefFromLegalDoc(child.attributes?.doc ?? "", child.attributes?.["bwb-id"] ?? activeLaw?.bwbId ?? "") : "";
    return href ? <a key={index} className="ref" href={href} onClick={legalClick}>{text}</a> : <span key={index} className="ref">{text}</span>;
  }
  return text;
}

function directText(node) {
  return node?.text ?? "";
}
