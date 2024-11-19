interface Dictionary<T> {
    [Key: string]: T;
}

var DbNetSuiteCore: any = {};
var controlArray: Dictionary<ComponentControl> = {}
DbNetSuiteCore.controlArray = controlArray;
DbNetSuiteCore.createClientControl = function (controlId: string, clientEvents) {
    document.addEventListener('htmx:afterRequest', function (evt) {
        if (!DbNetSuiteCore.controlArray[controlId]) {

            var clientControl = {}

            if (controlId.startsWith("Grid")) {
                clientControl = new GridControl(controlId);
            }
            if (controlId.startsWith("Select")) {
                clientControl = new SelectControl(controlId);
            }

            for (const [key, value] of Object.entries(clientEvents)) {
                (clientControl as ComponentControl).eventHandlers[key] = window[value.toString()]
            }
            DbNetSuiteCore.controlArray[controlId] = clientControl;
        }
        DbNetSuiteCore.controlArray[controlId].afterRequest(evt);
    });
}

class ComponentControl {
    controlId: string = "";
    formControl: HTMLFormElement;
    parentControl: ComponentControl;
    childControls: Dictionary<ComponentControl> = {};
    controlContainer: HTMLElement;
    eventHandlers = {};

    constructor(controlId) {
        this.controlId = controlId;
        this.formControl = document.querySelector(this.formSelector())
        this.formControl.style.display = '';
        this.controlContainer = this.formControl.parentElement
    }

    public setCaption(text) {
        var caption = this.controlElement("div.caption")
        if (caption) {
            caption.innerText = text;
        }
    }

    protected invokeEventHandler(eventName, args = {}) {
        window.dispatchEvent(new CustomEvent(`Grid${eventName}`, { detail: this.controlId }));
        if (this.eventHandlers.hasOwnProperty(eventName) == false) {
            return;
        }
        if (typeof this.eventHandlers[eventName] === 'function') {
            this.eventHandlers[eventName](this, args)
        }
        else {
            this.message(`Javascript function for event type '${eventName}' is not defined`, 'error', 3)
        }
    }

    protected message(text, style = 'info', delay = 1) {
        var toast = this.controlContainer.querySelector("#toastMessage") as HTMLElement
        //toast.classList.add(`alert-${style}`)
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.parentElement.style.marginLeft = `-${toast.parentElement.clientWidth / 2}px`
            toast.parentElement.style.marginTop = `-${toast.parentElement.clientHeight / 2}px`
            toast.parentElement.style.display = 'none'
            return
        }
        toast.parentElement.style.display = 'block'
        let self = this
        window.setTimeout(() => { self.message("") }, delay * 1000)
    }

    protected formSelector() {
        return `#${this.controlId}`
    }

    protected controlElements(selector) {
        return this.formControl.querySelectorAll(selector);
    }

    protected controlElement(selector) {
        return this.formControl.querySelector(selector);
    }

    protected triggerName(evt: any) {
        return (evt.detail.requestConfig.headers['HX-Trigger-Name'] ?? '').toLowerCase()
    }

    protected updateLinkedControls(linkedIds: string, primaryKey: string, url:string = null) {
        var linkedIdArray = linkedIds.split(",");
        
        linkedIdArray.forEach(linkedId => {
            this.isElementLoaded(`#${linkedId}`).then((selector) => {
                var linkedControl = DbNetSuiteCore.controlArray[linkedId]
                linkedControl.parentControl = this;
                this.childControls[linkedId] = linkedControl;
                if (url != null && linkedControl.dataSourceIsFileSystem()) {
                    primaryKey = url;
                }
                linkedControl.loadFromParent(primaryKey);
            })
        })
    }

    public dataSourceIsFileSystem() {
        return this.formControl.dataset.datasourcetype == "FileSystem"
    }

    protected loadFromParent(primaryKey: string) {
        let selector = `#${this.controlId} input[name="primaryKey"]`
        let pk = htmx.find(selector) as HTMLInputElement

        this.formControl.setAttribute("hx-vals", JSON.stringify({ primaryKey: primaryKey }))

        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.controlId}`, "submit");
        }
    }

    protected isElementLoaded = async selector => {
        while (document.querySelector(selector) === null) {
            await new Promise(resolve => requestAnimationFrame(resolve))
        }
        return document.querySelector(selector);
    };
}
