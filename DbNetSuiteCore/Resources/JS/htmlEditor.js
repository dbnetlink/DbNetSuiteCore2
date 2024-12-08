class HtmlEditor {
    constructor(textarea) {
        this.textarea = textarea;
        this.initialise();
    }
    static reset(textarea) {
        switch (HtmlEditor.editorName(textarea)) {
            case 'TinyMCE':
                HtmlEditor.editor('tinymce').remove(`#${textarea.id}`);
                break;
        }
    }
    initialise() {
        switch (HtmlEditor.editorName(this.textarea)) {
            case 'TinyMCE':
                this.initTinyMceEditor();
                break;
        }
    }
    assignContent(evt) {
        switch (HtmlEditor.editorName(this.textarea)) {
            case 'TinyMCE':
                this.textarea.value = HtmlEditor.editor('tinymce').get(this.textarea.id).getContent();
                evt.detail.parameters[this.textarea.name] = this.textarea.value;
                break;
        }
    }
    initTinyMceEditor() {
        var config = {
            selector: `#${this.textarea.id}`,
            license_key: 'gpl',
        };
        HtmlEditor.editor('tinymce').init(config);
    }
    static editorName(textarea) {
        return textarea.dataset.htmleditor;
    }
    static editor(editor) {
        return window[editor.toLowerCase()];
    }
}
