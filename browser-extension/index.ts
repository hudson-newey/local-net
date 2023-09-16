// ==UserScript==
// @name         Local Net
// @namespace    http://localhost:8080/
// @version      0.1
// @description  Opens links in using Local Net when you on alt-click on a link.
// @author       Local Net
// @match        *://*/*
// @grant        none
// ==/UserScript==

(() => {
    "use strict";

    document.addEventListener("click", (event: MouseEvent) => {
        if (!event.target) {
            return;
        }

        const target: HTMLAnchorElement = event.target as HTMLAnchorElement;

        if (event.altKey && target.tagName === "A") {
            event.preventDefault();
            const url = target.href;
            const modifiedUrl = `http://localhost:8080/?interceptor-url=${url}`;

            if (event.ctrlKey) {
                window.open(modifiedUrl, "_blank");
            } else {
                window.location.href = modifiedUrl;
            }
        }
    });
})();
