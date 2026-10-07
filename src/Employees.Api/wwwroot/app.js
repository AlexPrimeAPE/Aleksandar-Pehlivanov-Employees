const { useState, useRef } = React;
const html = htm.bind(React.createElement);

const MAX_BYTES = 10 * 1024 * 1024;

const formatDays = (n) => `${n.toLocaleString()} ${n === 1 ? "day" : "days"}`;

function formatSize(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function validateFile(file) {
  if (!/\.(csv|txt)$/i.test(file.name)) return "Please choose a .csv or .txt file.";
  if (file.size === 0) return "That file is empty.";
  if (file.size > MAX_BYTES) return `That file is ${formatSize(file.size)}. The limit is 10 MB.`;
  return null;
}

async function requestAnalysis(file, dateOrder) {
  const body = new FormData();
  body.append("file", file);

  let response;
  try {
    response = await fetch(`/api/analyze?dateOrder=${dateOrder}`, { method: "POST", body });
  } catch {
    throw new Error("We couldn't reach the server. Make sure the app is still running, then try again.");
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? `The server returned an error (${response.status}).`);
  }
  return response.json();
}

function DropZone({ onFile }) {
  const [dragging, setDragging] = useState(false);
  const inputRef = useRef(null);

  const handleDrop = (event) => {
    event.preventDefault();
    setDragging(false);
    const file = event.dataTransfer.files[0];
    if (file) onFile(file);
  };

  const handleChange = (event) => {
    const file = event.target.files[0];
    event.target.value = "";
    if (file) onFile(file);
  };

  return html`
    <button type="button"
            className=${`dropzone${dragging ? " dragging" : ""}`}
            onClick=${() => inputRef.current.click()}
            onDragOver=${(e) => { e.preventDefault(); setDragging(true); }}
            onDragLeave=${() => setDragging(false)}
            onDrop=${handleDrop}>
      <strong>Drop your CSV file here, or click to browse</strong>
      <span>Columns: EmpID, ProjectID, DateFrom, DateTo. Use NULL for "still working". Up to 10 MB.</span>
    </button>
    <input ref=${inputRef} type="file" accept=".csv,.txt,text/csv,text/plain" className="sr-only" tabIndex="-1"
           onChange=${handleChange} aria-label="Choose CSV file" />`;
}

function DateOrderPicker({ value, onChange }) {
  return html`
    <fieldset>
      <legend>If a date like 03/04/2020 is unclear, read it as</legend>
      <label className="radio">
        <input type="radio" name="dateOrder" checked=${value === "dmy"} onChange=${() => onChange("dmy")} />
        ${" "}Day first (3 April)
      </label>
      <label className="radio">
        <input type="radio" name="dateOrder" checked=${value === "mdy"} onChange=${() => onChange("mdy")} />
        ${" "}Month first (4 March)
      </label>
      <p className="hint">Dates like 2020-03-04 or 31/12/2020 are never ambiguous.</p>
    </fieldset>`;
}

function SampleFiles({ onPick }) {
  return html`
    <div className="samples">
      <strong>No file handy?</strong>
      <button type="button" className="link-button" onClick=${() => onPick("samples/sample.csv")}>Try a sample with mixed date formats</button>
      <button type="button" className="link-button" onClick=${() => onPick("samples/sample-with-tie.csv")}>Try a sample with a tie</button>
    </div>`;
}

function LongestPairsGrid({ pairs }) {
  const isTie = pairs.length > 1;

  return html`
    <div className="pair-card">
      <div className="table-wrap">
        <table>
          <caption className="sr-only">Common projects of the pair or pairs who worked together the longest</caption>
          <thead>
            <tr><th>Employee ID #1</th><th>Employee ID #2</th><th>Project ID</th><th>Days worked</th></tr>
          </thead>
          ${pairs.map((pair) => html`
            <tbody key=${`${pair.empId1}-${pair.empId2}`}>
              ${pair.projects.map((project) => html`
                <tr key=${project.projectId}>
                  <td>${project.empId1}</td><td>${project.empId2}</td>
                  <td>${project.projectId}</td><td>${project.daysWorked.toLocaleString()}</td>
                </tr>`)}
              <tr className="pair-total">
                <td className="label" colSpan="3">
                  Total for employees ${pair.empId1} & ${pair.empId2}
                  ${isTie && html`<span className="badge">Tied</span>`}
                </td>
                <td>${pair.totalDays.toLocaleString()}</td>
              </tr>
            </tbody>`)}
        </table>
      </div>
    </div>`;
}

function SkippedRows({ errors }) {
  if (errors.length === 0) return null;
  return html`
    <details>
      <summary>${errors.length} ${errors.length === 1 ? "row was" : "rows were"} skipped</summary>
      <ul>${errors.map((e) => html`<li key=${e.line}>Line ${e.line}: ${e.message}</li>`)}</ul>
    </details>`;
}

function Results({ data }) {
  const { pairs, errors, rowsRead } = data;
  const isTie = pairs.length > 1;

  let content;
  if (rowsRead === 0) {
    content = html`<p className="notice">We couldn't read any valid rows from this file. Check the skipped rows below for what went wrong.</p>`;
  } else if (pairs.length === 0) {
    content = html`<p className="notice">We read ${rowsRead} ${rowsRead === 1 ? "row" : "rows"}, but no two employees worked on the same project at the same time.</p>`;
  } else {
    content = html`
      <h2>${isTie ? `${pairs.length} pairs are tied for the longest time together` : "Longest time working together"}</h2>
      ${isTie && html`<p className="hint">Each of the pairs below worked together for exactly ${formatDays(pairs[0].totalDays)}.</p>`}
      <${LongestPairsGrid} pairs=${pairs} />`;
  }

  return html`<section aria-live="polite">${content}<${SkippedRows} errors=${errors} /></section>`;
}

function App() {
  const [file, setFile] = useState(null);
  const [dateOrder, setDateOrder] = useState("dmy");
  const [state, setState] = useState({ status: "idle" });
  const latestRequest = useRef(0);

  async function analyze(selected, order) {
    const requestId = ++latestRequest.current;
    setFile(selected);

    const problem = validateFile(selected);
    if (problem) { setState({ status: "error", message: problem }); return; }

    setState({ status: "loading" });
    try {
      const data = await requestAnalysis(selected, order);
      if (requestId === latestRequest.current) setState({ status: "done", data, id: requestId });
    } catch (err) {
      if (requestId === latestRequest.current) setState({ status: "error", message: err.message });
    }
  }

  async function useSample(path) {
    const response = await fetch(path);
    const blob = await response.blob();
    analyze(new File([blob], path.split("/").pop(), { type: "text/csv" }), dateOrder);
  }

  function changeDateOrder(order) {
    setDateOrder(order);
    if (file) analyze(file, order);
  }

  function startOver() {
    latestRequest.current++;
    setFile(null);
    setState({ status: "idle" });
  }

  return html`
    <main>
      <h1>Who worked together the longest?</h1>
      <p className="lead">Upload a CSV of project assignments and we'll find the pair of employees with the most shared days on common projects.</p>

      <${DropZone} onFile=${(f) => analyze(f, dateOrder)} />

      <div className="options">
        <${DateOrderPicker} value=${dateOrder} onChange=${changeDateOrder} />
        <${SampleFiles} onPick=${useSample} />
      </div>

      ${file && html`
        <div className="file-chip">
          <span>${file.name} <small>(${formatSize(file.size)})</small></span>
          <button type="button" className="link-button" onClick=${startOver}>Start over</button>
        </div>`}

      ${state.status === "loading" && html`<div className="status" role="status"><span className="spinner"></span>Analyzing your file…</div>`}
      ${state.status === "error" && html`<p className="notice error" role="alert">${state.message}</p>`}
      ${state.status === "done" && html`<${Results} key=${state.id} data=${state.data} />`}
    </main>`;
}

ReactDOM.createRoot(document.getElementById("root")).render(html`<${App} />`);
