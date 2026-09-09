document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("pre > code").forEach((codeBlock) => {
        const pre = codeBlock.parentNode;

        // Find the outermost highlight container
        let container = pre;
        while (container && container.classList && !container.classList.contains("highlight")) {
            container = container.parentNode;
        }

        if (!container) container = pre;
        if (!container.parentNode) return;

        // Find metadata wrapper (added in shortcode)
        const meta = container.closest(".code-meta");
        const id = meta?.dataset.id;
        const field = meta?.dataset.field;

        // Create wrapper
        const wrapper = document.createElement("div");
        wrapper.classList.add("code-wrapper");

        container.parentNode.insertBefore(wrapper, container);
        wrapper.appendChild(container);

        // Copy button
        const copyBtn = document.createElement("button");
        copyBtn.classList.add("copy-button");
        copyBtn.innerHTML = `
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

        copyBtn.addEventListener("click", () => {
            let text = "";

            const table = container.querySelector("table");
            if (table) {
                const codeCells = table.querySelectorAll("td:last-child code");
                text = Array.from(codeCells).map(cell => cell.innerText);
            } else {
                text = codeBlock.innerText;
            }

            text = text.toString().replaceAll("\n\n", "\n");

            navigator.clipboard.writeText(text).then(() => {
                copyBtn.classList.add("copied");
                copyBtn.querySelector(".copy-text").innerText = "Copied";

                setTimeout(() => {
                    copyBtn.classList.remove("copied");
                    copyBtn.querySelector(".copy-text").innerText = "";
                }, 1500);
            });
        });

        // Explorer button
        const explorerBtn = document.createElement("button");
        explorerBtn.classList.add("copy-button");
        explorerBtn.classList.add("explorer-button");
        explorerBtn.innerHTML = `
      <span class="explorer-icon">
        <img src="/logo.png" style="width: 25px;"/>
      </span>
      <span class="explorer-text"></span>
    `;

        explorerBtn.addEventListener("click", () => {
            if (!id) return;

            const url = `https://explorer.mqtt2otel.org/${id}${field ? "#" + field : ""}`;
            window.open(url, "_blank");
        });

        //
        // Insert both buttons
        //
        wrapper.insertBefore(copyBtn, container);
        if (id) wrapper.insertBefore(explorerBtn, container);
    });
});
