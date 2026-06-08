/**
 * Generates PDFs from docs/*.md (client proposal files)
 * Run from docs/: npm install && npm run pdf
 */
import { readFileSync } from "node:fs";
import { dirname, join, basename } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const docsDir = join(__dirname, "..");

const files = [
  "01-Existing-LMS-Features-Reference.md",
  "02-White-Label-AI-LMS-Proposal.md",
  "03-Technical-Architecture-LMS.md",
];

function escapeHtml(text) {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function inline(text) {
  return escapeHtml(text)
    .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    .replace(/✅/g, "&#10003;")
    .replace(/✓/g, "&#10003;")
    .replace(/—/g, "&mdash;");
}

function mdToHtml(markdown) {
  const lines = markdown.split("\n");
  const out = [];
  let inTable = false;
  let inUl = false;
  let inOl = false;
  let inPre = false;

  const closeUl = () => {
    if (inUl) {
      out.push("</ul>");
      inUl = false;
    }
  };

  const closeOl = () => {
    if (inOl) {
      out.push("</ol>");
      inOl = false;
    }
  };

  const closeTable = () => {
    if (inTable) {
      out.push("</tbody></table>");
      inTable = false;
    }
  };

  const closePre = () => {
    if (inPre) {
      out.push("</pre>");
      inPre = false;
    }
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (line.startsWith("```")) {
      closeUl();
      closeOl();
      closeTable();
      if (!inPre) {
        out.push('<pre class="code-block">');
        inPre = true;
      } else {
        closePre();
      }
      continue;
    }

    if (inPre) {
      out.push(escapeHtml(line));
      continue;
    }

    if (line.startsWith("# ")) {
      closeUl();
      closeOl();
      closeTable();
      out.push(`<h1>${inline(line.slice(2))}</h1>`);
      continue;
    }
    if (line.startsWith("## ")) {
      closeUl();
      closeOl();
      closeTable();
      out.push(`<h2>${inline(line.slice(3))}</h2>`);
      continue;
    }
    if (line.startsWith("### ")) {
      closeUl();
      closeOl();
      closeTable();
      out.push(`<h3>${inline(line.slice(4))}</h3>`);
      continue;
    }
    if (line.startsWith("#### ")) {
      closeUl();
      closeOl();
      closeTable();
      out.push(`<h4>${inline(line.slice(5))}</h4>`);
      continue;
    }

    if (line.startsWith("|")) {
      closeUl();
      closeOl();
      const cells = line
        .split("|")
        .slice(1, -1)
        .map((c) => c.trim());
      if (cells.every((c) => /^[-: ]+$/.test(c))) continue;
      if (!inTable) {
        out.push("<table><thead><tr>");
        cells.forEach((c) => out.push(`<th>${inline(c)}</th>`));
        out.push("</tr></thead><tbody>");
        inTable = true;
      } else {
        out.push("<tr>");
        cells.forEach((c) => out.push(`<td>${inline(c)}</td>`));
        out.push("</tr>");
      }
      continue;
    } else if (inTable && !line.startsWith("|")) {
      closeTable();
    }

    if (/^- \[[ xX]\] /.test(line)) {
      if (!inUl) {
        closeOl();
        out.push('<ul class="checklist">');
        inUl = true;
      }
      const text = line.replace(/^- \[[ xX]\] /, "");
      out.push(`<li>${inline(text)}</li>`);
      continue;
    }

    if (line.startsWith("- ")) {
      closeOl();
      if (!inUl) {
        out.push("<ul>");
        inUl = true;
      }
      out.push(`<li>${inline(line.slice(2))}</li>`);
      continue;
    }

    if (/^\d+\.\s/.test(line)) {
      closeUl();
      if (!inOl) {
        out.push("<ol>");
        inOl = true;
      }
      out.push(`<li>${inline(line.replace(/^\d+\.\s/, ""))}</li>`);
      continue;
    }

    if (!line.startsWith("- ") && !/^\d+\.\s/.test(line)) {
      closeUl();
      closeOl();
    }

    if (line.startsWith("---")) {
      out.push("<hr />");
      continue;
    }

    if (line.startsWith("> ")) {
      out.push(`<blockquote>${inline(line.slice(2))}</blockquote>`);
      continue;
    }

    if (line.trim() === "") {
      continue;
    }

    if (line.startsWith("*") && line.endsWith("*") && !line.startsWith("**")) {
      out.push(`<p class="muted">${inline(line.slice(1, -1))}</p>`);
      continue;
    }

    out.push(`<p>${inline(line)}</p>`);
  }

  closeUl();
  closeOl();
  closeTable();
  closePre();
  return out.join("\n");
}

function buildHtml(title, bodyHtml) {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>${escapeHtml(title)}</title>
  <style>
    @page { margin: 18mm 16mm; size: A4; }
    * { box-sizing: border-box; }
    body {
      font-family: "Segoe UI", system-ui, sans-serif;
      font-size: 10.5pt;
      line-height: 1.45;
      color: #0f172a;
      max-width: 100%;
    }
    h1 { font-size: 22pt; margin: 0 0 8px; color: #0b3d91; page-break-after: avoid; }
    h2 {
      font-size: 13pt;
      margin: 22px 0 8px;
      padding-bottom: 4px;
      border-bottom: 2px solid #e2e8f0;
      color: #1e293b;
      page-break-after: avoid;
    }
    h3 { font-size: 11pt; margin: 14px 0 6px; color: #334155; page-break-after: avoid; }
    h4 { font-size: 10.5pt; margin: 12px 0 4px; color: #475569; page-break-after: avoid; }
    p { margin: 6px 0; }
    p.muted { font-size: 9pt; color: #64748b; font-style: italic; }
    ul, ol { margin: 6px 0 10px 18px; padding: 0; }
    ul.checklist { list-style: none; margin-left: 4px; }
    ul.checklist li::before { content: "\\2610  "; color: #64748b; }
    li { margin: 3px 0; }
    blockquote {
      margin: 8px 0;
      padding: 8px 12px;
      border-left: 4px solid #3b82f6;
      background: #f8fafc;
      color: #334155;
    }
    hr { border: none; border-top: 1px solid #e2e8f0; margin: 16px 0; }
    code {
      font-family: Consolas, monospace;
      font-size: 9pt;
      background: #f1f5f9;
      padding: 1px 4px;
      border-radius: 3px;
    }
    pre.code-block {
      font-family: Consolas, monospace;
      font-size: 9pt;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 10px 12px;
      margin: 8px 0 12px;
      white-space: pre-wrap;
      page-break-inside: avoid;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin: 8px 0 14px;
      font-size: 9pt;
      page-break-inside: avoid;
    }
    th, td {
      border: 1px solid #cbd5e1;
      padding: 6px 8px;
      text-align: left;
      vertical-align: top;
    }
    th { background: #f8fafc; font-weight: 600; }
    tr:nth-child(even) td { background: #fafafa; }
    strong { color: #0f172a; }
  </style>
</head>
<body>
${bodyHtml}
</body>
</html>`;
}

const puppeteer = await import("puppeteer");

const browser = await puppeteer.launch({
  headless: true,
  args: ["--no-sandbox", "--disable-setuid-sandbox"],
});

try {
  for (const file of files) {
    const mdPath = join(docsDir, file);
    const pdfPath = join(docsDir, basename(file, ".md") + ".pdf");
    const md = readFileSync(mdPath, "utf8");
    const title = md.match(/^# (.+)/m)?.[1] ?? basename(file, ".md");
    const html = buildHtml(title, mdToHtml(md));

    const page = await browser.newPage();
    await page.setContent(html, { waitUntil: "networkidle0" });
    await page.pdf({
      path: pdfPath,
      format: "A4",
      printBackground: true,
      margin: { top: "18mm", right: "16mm", bottom: "18mm", left: "16mm" },
    });
    await page.close();
    console.log(`PDF written: ${pdfPath}`);
  }
} finally {
  await browser.close();
}
