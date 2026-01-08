class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt:any) {
        let treeId = evt.target.closest("form").id;
        if (treeId.startsWith(this.controlId) == false) {
            return
        }

        this.tree = this.controlElement("div.tree-root");

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }
    }

    private toggleDropdown(ev) {
        this.controlElement("#dropdownMenu").classList.toggle("show");
    }

    private toggleNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        let nodeHeader = target.closest('.node-header');
        nodeHeader.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
        event.stopPropagation();
        const node = target.parentElement;
        node.classList.toggle('open');
        target.closest('.node').querySelector(".node-content").classList.toggle('hidden');
    }

    private selectLeaf(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedLeaf = target.closest("div");
        this.selectParentNodes(selectedLeaf)
    }

    private selectNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedNode = target.closest(".node") as HTMLElement;
        this.selectParentNodes(selectedNode)
    }

    private selectParentNodes(selectedElement: HTMLElement) {
        let path = [selectedElement.dataset.description];
        let parentNode: HTMLDivElement = selectedElement.parentElement.closest('.node');

        while (parentNode) {
            const headerText = parentNode.dataset.description;
            path.unshift(headerText);
            parentNode = parentNode.parentElement.parentElement.closest('.node');
        }

        let selectionLabel = this.controlElement("#selected-label");
        selectionLabel.innerHTML = `<span class="path-prefix">${selectionLabel.dataset.selectiontitle}</span>${path.join(' &gt; ')}`;
        this.controlElement("#dropdownMenu").classList.remove("show");
    }

    private reset(e:MouseEvent) {
        e.stopPropagation();
        let treeSearch: HTMLInputElement = this.controlElement('#treeSearch')
        treeSearch.value = '';
        treeSearch.dispatchEvent(new Event('input'));
        this.controlElement('#selected-label').innerText = 'Select Location...';
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
                    item.querySelector('.node-content').classList.add('hidden');
                }
            } else if (isMatch) {
                item.style.display = 'flex';
                let parentContent = item.closest('.node-content');
                while (parentContent) {
                    parentContent.previousElementSibling.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
                    parentContent.classList.remove('hidden');
                    parentContent.parentElement.classList.add('open');
                    parentContent.parentElement.style.display = 'flex';
                    parentContent = parentContent.parentElement.closest('.node-content');
                }
            } else {
                item.style.display = 'none';
            }
        });
    }

    private initialise() {
        this.controlElements('div.select-trigger').forEach(div => { div.addEventListener("click", (e:MouseEvent) => this.toggleDropdown(e)) });
        this.controlElements('span.open-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('span.close-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('span.leaf-text').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectLeaf(e)) });
        this.controlElements('span.node-text').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectNode(e)) });
        this.controlElement('#treeSearch').addEventListener('input', (e:InputEvent) => this.search(e));
        this.controlElement('#resetBtn').addEventListener('click', (e:MouseEvent) => this.reset(e));

        window.onclick = function (event) {
            if (!event.target.closest('.custom-select-wrapper')) {
                this.controlElement("#dropdownMenu").classList.remove("show");
            }
        }
        this.invokeEventHandler('Initialised');
    }
}