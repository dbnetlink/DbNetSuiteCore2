class HtmlEditor {
    textarea: HTMLTextAreaElement;
    constructor(textarea: HTMLTextAreaElement) {
        this.textarea = textarea;
        this.initialise();
    }

    public static reset(textarea: HTMLTextAreaElement) {
        switch (HtmlEditor.editorName(textarea)) {
            case 'TinyMCE':
                HtmlEditor.editor('tinymce').remove(`#${textarea.id}`);
                break;
        }
    }

    public initialise() {
        switch (HtmlEditor.editorName(this.textarea)) {
            case 'TinyMCE':
                this.initTinyMceEditor();
                break;
        }
    }

    public assignContent(evt: any) {
        switch (HtmlEditor.editorName(this.textarea)) {
            case 'TinyMCE':
                this.textarea.value = HtmlEditor.editor('tinymce').get(this.textarea.id).getContent();
                evt.detail.parameters[this.textarea.name] = this.textarea.value;
                break;
        }
    }

    private initTinyMceEditor() {
        var config = {
            selector: `#${this.textarea.id}`,
            license_key: 'gpl',
        };
        HtmlEditor.editor('tinymce').init(config);
    }

    private static editorName(textarea: HTMLTextAreaElement) {
        return textarea.dataset.htmleditor;
    }

    public static editor(editor: string) {
        return window[editor.toLowerCase()];
    }
}