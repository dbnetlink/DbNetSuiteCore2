class HtmlEditor {
    textarea: HTMLTextAreaElement;
    formControl: FormControl;
    editorInstance: any;
    static TinyMCE: string = "TinyMCE"
    static CKEditor: string = "CKEditor"
    static Froala: string = "Froala"
    constructor(textarea: HTMLTextAreaElement, formControl: FormControl) {
        this.textarea = textarea;
        this.formControl = formControl;
        this.initialise();
    }

    public reset(textarea: HTMLTextAreaElement) {
        switch (HtmlEditor.editorName(textarea)) {
            case HtmlEditor.TinyMCE:
                HtmlEditor.editor('tinymce').remove(`#${textarea.id}`);
                break;
            case HtmlEditor.CKEditor:
                this.editorInstance.destroy();
                break;
        }
    }

    public initialise() {
        switch (HtmlEditor.editorName(this.textarea)) {
            case HtmlEditor.TinyMCE:
                this.initTinyMceEditor();
                break;
            case HtmlEditor.CKEditor:
                this.initCKEditorEditor();
                break;
            case HtmlEditor.Froala:
                this.initFroalaEditor();
                break;
        }
    }

    public assignContent(evt: any) {
        switch (HtmlEditor.editorName(this.textarea)) {
            case HtmlEditor.TinyMCE:
                this.textarea.value = HtmlEditor.editor('tinymce').get(this.textarea.id).getContent();
                evt.detail.parameters[this.textarea.name] = this.textarea.value;
                break;
            case HtmlEditor.CKEditor:
                this.textarea.value = this.editorInstance.getData()
                evt.detail.parameters[this.textarea.name] = this.textarea.value;
                break;
            case HtmlEditor.Froala:
                this.textarea.value = this.editorInstance.html.get(true);
                evt.detail.parameters[this.textarea.name] = this.textarea.value;
                break;
        }
    }

    private initTinyMceEditor() {
        var config = {
            selector: `#${this.textarea.id}`,
            license_key: 'gpl',
        };

        this.updateConfiguration(config);
        HtmlEditor.editor('tinymce').init(config);
    }

    private updateConfiguration(config: any) {
        this.formControl.configureHtmlEditor(config, this.textarea.name.substring(1));
    }

    private initCKEditorEditor() {
        const {
            ClassicEditor,
            Essentials,
            Bold,
            Italic,
            Font,
            Paragraph
        } = window['CKEDITOR'];

        let config = {
            licenseKey: "",
            plugins: [Essentials, Bold, Italic, Font, Paragraph],
            toolbar: [
                'undo', 'redo', '|', 'bold', 'italic', '|',
                'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor'
            ]
        };

        this.updateConfiguration(config);

        ClassicEditor
            .create(document.querySelector(`#${this.textarea.id}_ckeditor`), config).then(editor => {
                this.editorInstance = editor;
            });
    }

    private initFroalaEditor() {
        this.editorInstance = new window['FroalaEditor'](`#${this.textarea.id}_froala`);
    }

    private static editorName(textarea: HTMLTextAreaElement) {
        return textarea.dataset.htmleditor;
    }

    private static removeElement(textarea: HTMLTextAreaElement) {
        switch (HtmlEditor.editorName(textarea)) {
            case HtmlEditor.Froala:
            case HtmlEditor.CKEditor:
                document.querySelector(`#${textarea.id}_${HtmlEditor.editorName(textarea).toLowerCase()}`).remove();
                break;
        }
    }

    public static editor(editor: string) {
        switch (editor) {
            case HtmlEditor.TinyMCE:
                editor = editor.toLowerCase();
                break;
            case HtmlEditor.Froala:
                editor = 'FroalaEditor';
                break;
            case HtmlEditor.CKEditor:
                editor = 'CKEDITOR';
                break;
        }
        return window[editor];
    }
}