// Audio Mirror website: scroll reveals, sticky-nav border, chart tooltip, latest release info.
(() => {
  const root = document.documentElement;
  root.classList.add("js");

  // Reveal sections as they enter the viewport.
  const revealables = document.querySelectorAll(".reveal, .chart");
  if ("IntersectionObserver" in window) {
    const io = new IntersectionObserver((entries) => {
      for (const e of entries) {
        if (e.isIntersecting) {
          e.target.classList.add("in");
          io.unobserve(e.target);
        }
      }
    }, { rootMargin: "0px 0px -8% 0px", threshold: 0.12 });
    revealables.forEach((el) => io.observe(el));

    // Border under the nav once the page has left the top.
    const nav = document.querySelector(".nav");
    const sentinel = document.createElement("div");
    sentinel.style.cssText = "position:absolute;top:0;height:8px;width:1px;pointer-events:none";
    document.body.prepend(sentinel);
    new IntersectionObserver(([e]) => nav.classList.toggle("scrolled", !e.isIntersecting)).observe(sentinel);
  } else {
    revealables.forEach((el) => el.classList.add("in"));
  }

  // Latency chart tooltip (hover and keyboard focus).
  const body = document.querySelector(".chart-body");
  const tip = body && body.querySelector(".tip");
  if (tip) {
    const show = (row) => {
      const { min, avg, max } = row.dataset;
      const label = row.querySelector(".row-label").firstChild.textContent.trim();
      tip.innerHTML = `${label} buffer: <b>${min}</b> / <b>${avg}</b> / <b>${max}</b> ms`;
      const track = row.querySelector(".track");
      const b = body.getBoundingClientRect();
      const t = track.getBoundingClientRect();
      const x = t.left - b.left + (t.width * Number(avg)) / 70;
      const half = tip.offsetWidth / 2;
      tip.style.left = `${Math.min(Math.max(x, half), b.width - half)}px`;
      tip.style.top = `${row.offsetTop - tip.offsetHeight}px`;
      tip.classList.add("show");
    };
    const hide = () => tip.classList.remove("show");
    body.querySelectorAll(".chart-row").forEach((row) => {
      row.addEventListener("pointerenter", () => show(row));
      row.addEventListener("focus", () => show(row));
      row.addEventListener("pointerleave", hide);
      row.addEventListener("blur", hide);
    });
  }

  // Keep download links and version on the newest release without redeploying.
  const fmt = (bytes) => (bytes >= 10e6 ? `${Math.round(bytes / 1e6)} MB` : `${(bytes / 1e6).toFixed(1)} MB`);
  fetch("https://api.github.com/repos/yyusvf/audio-mirror/releases/latest", { headers: { Accept: "application/vnd.github+json" } })
    .then((r) => (r.ok ? r.json() : Promise.reject(r.status)))
    .then((rel) => {
      const version = String(rel.tag_name || "").replace(/^v/, "");
      const pick = (re) => (rel.assets || []).find((a) => re.test(a.name));
      const pairs = [["dl-setup", pick(/Setup\.exe$/i)], ["dl-portable", pick(/Portable\.zip$/i)]];
      for (const [id, asset] of pairs) {
        const link = document.getElementById(id);
        if (!link || !asset) continue;
        link.href = asset.browser_download_url;
        if (version) link.querySelector("[data-version]").textContent = version;
        link.querySelector("[data-size]").textContent = fmt(asset.size);
      }
    })
    .catch(() => { /* the static links in the markup still work */ });
})();
