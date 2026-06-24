window.copyToClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error("Clipboard copy failed", err);
        return false;
    }
};
