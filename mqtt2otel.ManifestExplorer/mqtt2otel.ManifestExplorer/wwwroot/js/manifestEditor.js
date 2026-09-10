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