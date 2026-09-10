
window.manifestCompletion = {
    register: function (schemaJson) {

        const schema = JSON.parse(schemaJson);

        if (window.manifestCompletionProvider) {
            window.manifestCompletionProvider.dispose();
        }

        window.manifestCompletionProvider =
            monaco.languages.registerCompletionItemProvider("yaml", {

                triggerCharacters: [":", " "],

                provideCompletionItems: function (model, position) {

                    const text = model.getValueInRange({
                        startLineNumber: 1,
                        startColumn: 1,
                        endLineNumber: position.lineNumber,
                        endColumn: position.column
                    });

                    const path = getYamlPath(text);

                    console.log("YAML path:", path);

                    const node = findSchemaNode(schema, path);

                    console.log("Schema node:", node);

                    if (!node || !node.properties) {
                        return { suggestions: [] };
                    }

                    const existingProperties =
                        getExistingProperties(text);

                    const suggestions = [];

                    for (const name of Object.keys(node.properties)) {

                        if (existingProperties.has(name)) {
                            continue;
                        }

                        const property = node.properties[name];

                        suggestions.push({
                            label: name,
                            kind: monaco.languages.CompletionItemKind.Property,
                            insertText: name + ": ",
                            detail: getDetail(property),
                            documentation: getDocumentation(property)
                        });
                    }

                    return {
                        suggestions: suggestions
                    };
                }
            });

        console.log("Manifest completion registered");
    }
};


function getYamlPath(text) {

    const lines = text.split(/\r?\n/);

    const stack = [];

    for (const line of lines) {

        if (!line.trim())
            continue;

        if (line.trimStart().startsWith("#"))
            continue;

        const indentation =
            line.length - line.trimStart().length;

        let content = line.trim();

        /*
         * Array item:
         *
         * - Name: test
         *
         * becomes:
         *
         * Name: test
         */
        if (content.startsWith("-")) {
            content = content.substring(1).trim();
        }

        const colonIndex = content.indexOf(":");

        /*
         * No ':' means this isn't a property declaration.
         */
        if (colonIndex < 0)
            continue;

        const key =
            content.substring(0, colonIndex).trim();

        if (!key)
            continue;

        /*
         * Remove everything at the same or deeper
         * indentation level.
         */
        while (
            stack.length > 0 &&
            stack[stack.length - 1].indentation >= indentation
        ) {
            stack.pop();
        }

        /*
         * If the property has a value on the same line,
         * it is normally a scalar and should NOT become
         * the current YAML object.
         *
         * Example:
         *
         * Name: My broker
         *
         * But if the value is empty:
         *
         * Endpoint:
         *
         * then it represents a nested object.
         */
        const value =
            content.substring(colonIndex + 1).trim();

        stack.push({
            key: key,
            indentation: indentation,
            hasValue: value.length > 0
        });
    }

    /*
     * Only properties which represent containers should
     * remain in the path.
     */
    return stack
        .filter(x => !x.hasValue)
        .map(x => x.key);
}


function findSchemaNode(schema, path) {

    let node = schema;

    for (const part of path) {

        if (!node)
            return null;

        /*
         * If we're currently inside an array,
         * move to the array element schema.
         */
        if (node.type === "array") {
            node = node.elementType;
        }

        if (!node || !node.properties)
            return null;

        node = node.properties[part];
    }

    /*
     * If the final node is an array,
     * completion should be based on its element.
     */
    if (node && node.type === "array") {
        node = node.elementType;
    }

    return node;
}


function getExistingProperties(text) {

    const lines = text.split(/\r?\n/);

    if (lines.length === 0)
        return new Set();

    const currentLine = lines[lines.length - 1];

    const currentIndent =
        currentLine.length -
        currentLine.trimStart().length;

    const result = new Set();

    for (let i = lines.length - 1; i >= 0; i--) {

        const line = lines[i];

        if (!line.trim())
            continue;

        const indent =
            line.length -
            line.trimStart().length;

        if (indent < currentIndent)
            break;

        if (indent !== currentIndent)
            continue;

        const match =
            line.trim().match(/^([^:#]+):/);

        if (match) {
            result.add(match[1].trim());
        }
    }

    return result;
}


function getDetail(property) {

    if (!property)
        return "";

    if (property.type === "array")
        return "array";

    if (property.type === "object")
        return "object";

    if (property.type === "dictionary")
        return "dictionary";

    if (property.values)
        return "enum: " + property.values.join(", ");

    return property.type || "";
}


function getDocumentation(property) {

    if (!property)
        return "";

    if (property.values) {
        return "Allowed values: " +
            property.values.join(", ");
    }

    return "";
}

