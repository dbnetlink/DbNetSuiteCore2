import { ComponentControl } from './ComponentControl.js'
import { Selection } from './types.js'

export class TreeControl extends ComponentControl {
    tree: HTMLDivElement|null = null;
    treeContainer: HTMLDivElement | null = null;
    searchEnabled: boolean = false;
    selectionLabel: HTMLDivElement | null = null;
    currentSelection: Selection|null = null;
    constructor(selectId:string) {
        super(selectId)
    }

    afterRequest(evt:any) {
        let treeId = evt.target.closest("form").id;
        if (treeId.startsWith(this.controlId) == false) {
            return
        }

        this.tree = this.controlElement("div.tree-root") as HTMLDivElement;

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.controlElements('span.open-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('span.close-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('div.leaf[selectable="true"]').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectLeaf(e)) });
        this.controlElements('span.node-text[selectable="true"]').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectNode(e)) });

        if (this.selectionLabel != null) {
            this.selectionLabel.innerText = this.selectionLabel.dataset.selectionplaceholder as string;
        }
    }

    private toggleDropdown(ev:Event) {
        (this.controlElement("#dropdownMenu") as HTMLElement).classList.toggle("show");
    }

    private toggleNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const nodeHeader = target.closest('.node-header') as HTMLElement;
        nodeHeader.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
        event.stopPropagation();
        const node:HTMLElement = target.parentElement as HTMLElement;
        node.classList.toggle('open');
        target.closest('.node')?.querySelector(".node-content")?.classList.toggle('hidden');
    }

    private selectLeaf(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedLeaf = target.closest("div") as HTMLElement;
        this.selectParentNodes(selectedLeaf)
    }

    private selectNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedNode = target.closest(".node-header") as HTMLElement;
        this.selectParentNodes(selectedNode)
    }

    private selectParentNodes(selectedElement: HTMLElement) {
        let previouslySelected = this.controlElement('.selected');
        if (previouslySelected) {
            previouslySelected.classList.remove("selected");
        };
        selectedElement.classList.add("selected"); 
        const divElement = selectedElement.closest('div[data-value]') as HTMLDivElement;
        let path = [(divElement.querySelector("span[data-level]") as HTMLSpanElement).innerText];
        let parentNode: HTMLDivElement = divElement.parentElement?.parentElement?.closest('.node') as HTMLDivElement;

        let parentValues = [];
        let parentDescriptions = [];

        while (parentNode) {
            const headerText = parentNode.dataset.description as string;
            path.unshift(headerText);
            parentDescriptions.push(parentNode.dataset.description);
            parentValues.push(parentNode.dataset.description);
            parentNode = parentNode.parentElement?.parentElement?.closest('.node') as HTMLDivElement;
        }

        if (this.selectionLabel) {
            this.selectionLabel.innerHTML = `<span class="path-prefix">${this.selectionLabel.dataset.selectiontitle}</span>${path.join(' &gt; ')}`;
            this.controlElement("#dropdownMenu")?.classList.remove("show");
        }

        this.updateLinkedControls(this.getLinkedControlIds(), selectedElement.dataset.value)

        let args: Selection = { value: selectedElement.dataset.value, description: selectedElement.dataset.description, parentValues: parentValues, parentDescriptions: parentDescriptions } as Selection;
        this.currentSelection = args;
        this.invokeEventHandler('ItemSelected', args);
    }

    private reset(e:MouseEvent) {
        e.stopPropagation();
        let treeSearch: HTMLInputElement = this.controlElement('#treeSearch') as HTMLInputElement;
        treeSearch.value = '';
        treeSearch.dispatchEvent(new Event('input'));
        if (this.selectionLabel) {
            this.selectionLabel.innerText = this.selectionLabel.dataset.selectionplaceholder as string;
        }
    }

    private search(e: InputEvent) {
        const filter = (e.target as HTMLInputElement).value.toLowerCase();
        const items = this.controlElements('.node, .leaf');

        this.controlElements('.close-icon').forEach(e => { e.classList.add('hidden') });
        this.controlElements('.open-icon').forEach(e => { e.classList.remove('hidden') });

        items.forEach(item => {
            const text = item.innerText.toLowerCase();
            const isMatch = text.includes(filter);

            if (!filter) {
                item.style.display = 'flex';
                if (item.classList.contains('node')) {
                    item.classList.remove('open');
                    item.querySelector('.node-content')?.classList.add('hidden');
                }
            } else if (isMatch) {
                item.style.display = 'flex';
                let parentContent = item.closest('.node-content');
                while (parentContent) {
                    parentContent.previousElementSibling?.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
                    parentContent.classList.remove('hidden');
                    const parentElement = parentContent.parentElement as HTMLElement;
                    parentElement?.classList.add('open');
                    parentElement.style.display = 'flex';
                    parentContent = parentElement.closest('.node-content');
                }
            } else {
                item.style.display = 'none';
            }
        });
    }

    public updateFixedFilterParameters(params: any) {
        this.updateFixedFilterParams(params);
    }

    public updateApiRequestParameters(params: any) {
        this.updateApiRequestParams(params);
    }

    private initialise() {
        this.loaded = true;
        this.searchEnabled = this.controlElement('.search-container') != null;
        this.treeContainer = this.controlElement('.tree-container') as HTMLDivElement;
        this.linkedControlIdsElement = this.treeContainer;

        this.controlElement('div.select-trigger')?.addEventListener("click", (e: MouseEvent) => this.toggleDropdown(e));

        if (this.searchEnabled) {
            this.controlElement('#treeSearch')?.addEventListener('input', this.debounce((e: InputEvent) => this.search(e)));
            this.controlElement('#resetBtn')?.addEventListener('click', (e: MouseEvent) => this.reset(e));
        }
        this.selectionLabel = this.controlElement("#selected-label") as HTMLDivElement;
        window.addEventListener("click", (e: MouseEvent) => { this.closeDropDown(e) });
        this.invokeEventHandler('Initialised');
    }

    public closeDropDown(event: any) {
        if (!event.target.closest('.tree-container')) {
            this.controlElement("#dropdownMenu")?.classList.remove("show");
        }
    }

    public getLeafElements() {
        return this.controlElements("span.leaf-text")
    }

    public getNodeElements() {
        return this.controlElements("span.node-text")
    }
}