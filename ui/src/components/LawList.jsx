export function LawList({ laws, activeLaw, onSelectLaw }) {
  return (
    <section className="panel">
      <h2>Wetten</h2>
      <div className="law-list" role="list">
        {laws.map((law) => (
          <button
            key={law.slug}
            type="button"
            className={`law-item${law.slug === activeLaw?.slug ? " active" : ""}`}
            onClick={() => onSelectLaw(law)}
          >
            <strong>{law.title}</strong>
            <span>{law.bwbId} {law.effectiveDate ? `- ${law.effectiveDate}` : ""}</span>
          </button>
        ))}
      </div>
    </section>
  );
}
