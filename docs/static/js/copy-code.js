document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("pre > code").forEach((codeBlock) => {
    const pre = codeBlock.parentNode;

    // Find the outermost highlight container
    let container = pre;
    while (container && container.classList && !container.classList.contains("highlight")) {
      container = container.parentNode;
    }

    // If no highlight container found, fallback to <pre>
    if (!container) container = pre;

    if (!container.parentNode) return;

    // Create wrapper
    const wrapper = document.createElement("div");
    wrapper.classList.add("code-wrapper");

    // Insert wrapper before container
    container.parentNode.insertBefore(wrapper, container);

    // Move container inside wrapper
    wrapper.appendChild(container);

    // Create button
    const button = document.createElement("button");
    button.classList.add("copy-button");
    button.innerHTML = `
      <span class="copy-icon">
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" 
             viewBox="0 0 24 24" fill="none" stroke="blue" 
             stroke-width="1" stroke-linecap="round" stroke-linejoin="round">
          <rect x="9" y="9" width="13" height="13" rx="2"></rect>
          <path d="M5 15H4a2 2 0 0 1-2-2V4
                   a2 2 0 0 1 2-2h9
                   a2 2 0 0 1 2 2v1"></path>
        </svg>
      </span>
      <span class="copy-text"></span>
    `;

    button.addEventListener("click", () => {
      let text = "";
    
      // Hugo/Chroma with line numbers uses tables
      const table = container.querySelector("table");
    
      if (table) {
        // grab only code cells (skip line numbers)
        const codeCells = table.querySelectorAll("td:last-child code");
    
        text = Array.from(codeCells)
          .map(cell => cell.innerText);
      } else {
        // fallback (no line numbers)
        text = codeBlock.innerText;
      }
    
      text = text.toString().replaceAll("\n\n", "\n");
  
      navigator.clipboard.writeText(text).then(() => {
        button.classList.add("copied");
        button.querySelector(".copy-text").innerText = "Copied";
    
        setTimeout(() => {
          button.classList.remove("copied");
          button.querySelector(".copy-text").innerText = "";
        }, 1500);
      });
    });

    wrapper.insertBefore(button, container);
  });
});

