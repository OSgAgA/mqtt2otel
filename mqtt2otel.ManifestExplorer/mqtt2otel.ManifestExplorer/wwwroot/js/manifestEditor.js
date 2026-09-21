window.manifestEditor = {
    configureSchema: function (schema) {
        const schemaObject = JSON.parse(schema);

        monacoYaml.configureMonacoYaml(monaco, {
            schemas: [
                {
                    uri: "inmemory://manifest-schema.json",
                    fileMatch: ["*"],
                    schema: schemaObject
                }
            ]
        });
    }
};

// Waits until the monacco editor is available
window.waitForMonaco = () => {
    timeout = 15000;

    return new Promise((resolve, reject) => {
        const start = Date.now();
        const check = () => {
            if (typeof monaco !== 'undefined' && monaco.editor) {
                resolve();
            } else if (Date.now() - start > timeoutMs) {
                reject(new Error('Monaco failed to load in time'));
            } else {
                setTimeout(check, 50);
            }
        };
        check();
    });
}