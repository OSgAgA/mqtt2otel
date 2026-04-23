document.addEventListener("DOMContentLoaded", () => {
  // Re-render Mermaid when a tab is clicked
  document.querySelectorAll("[data-tabs] button").forEach((tabButton) => {
    tabButton.addEventListener("click", () => {
      if (window.mermaid) {
        window.mermaid.init(undefined, document.querySelectorAll(".mermaid"));
      }
    });
  });
});
