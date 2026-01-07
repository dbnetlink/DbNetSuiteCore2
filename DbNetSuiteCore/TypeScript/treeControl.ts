class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return
        }

        this.tree = this.controlElement("div.tree-root");

        let selectElements = this.controlElements("select");
        this.tree.innerHTML = selectElements[1].innerHTML;
        selectElements[1].remove();

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.invokeEventHandler('OptionsLoaded');
        this.selectChanged(this.tree);
        this.checkForError();
    }

    private initialise() {
        this.controlElement("select").addEventListener("change", (ev: Event) => {
            this.selectChanged(ev.target as HTMLSelectElement);
        })
        this.invokeEventHandler('Initialised');
    }

    private selectChanged(target: HTMLSelectElement) {
        let url = '';
        if (target.selectedOptions.length) {
            var dataset = target.selectedOptions[0].dataset
            url = this.dataSourceIsFileSystem() && dataset.isdirectory && dataset.isdirectory.toLowerCase() == "true" ? dataset.path : ''
        }
        this.updateLinkedChildControls(target.selectedIndex.toString(), url);
        this.invokeEventHandler('LeafSelected', { selectedOptions: target.selectedOptions });
    }

    private updateLinkedChildControls(selectedIndex: string, url:string) {
        this.updateLinkedControls(this.getLinkedControlIds(), selectedIndex, url)
    }

    private checkForError() {
        var select = this.controlElement("select") as HTMLSelectElement;
        const error = select.querySelector("div");
        if (error) {
            select.parentElement.nextElementSibling.after(error)
        }
    }

    public getSelectedOptions(): HTMLOptionElement[] {

        return Array.from(this.tree.selectedOptions);
    }
}