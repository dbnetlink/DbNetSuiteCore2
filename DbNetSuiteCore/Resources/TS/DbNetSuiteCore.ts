import type { Dictionary } from './types.js'
import { GridControl } from './GridControl.js'
import { FormControl } from './FormControl.js'
import { SelectControl } from './SelectControl.js'
import { TreeControl } from './TreeControl.js'

export class DbNetSuiteCore {
    public static controlArray: Dictionary<any> = {}
    public static namedControlArray: Dictionary<any> = {}

    public static createClientContro(controlId: string, clientId: string, clientEvents: object, deferredLoad: boolean = false) {
        document.addEventListener('htmx:afterRequest', function (evt) {
            DbNetSuiteCore.assignClientControl(controlId, clientId, clientEvents, deferredLoad);
            DbNetSuiteCore.controlArray[controlId].afterRequest(evt);
        });

        document.getElementById(controlId)!.addEventListener('htmx:responseError', function (evt: any) {
            evt.currentTarget.innerHTML = evt.detail.xhr.responseText;
        });

        htmx.on("htmx:responseError", function (evt: any) {
            const requestConfig = evt.detail.requestConfig;
            const xhr = evt.detail.xhr;
            alert(`<b>${requestConfig.verb} ${requestConfig.path}</b> returned <b>${xhr.status} ${xhr.statusText}</b><br>${xhr.responseText}`)
        })

        if (deferredLoad) {
            DbNetSuiteCore.assignClientControl(controlId, clientId, clientEvents, deferredLoad);
        }
    }

    public static assignClientControl(controlId: string, clientId: string, clientEvents: object, deferredLoad: boolean = false) {
        if (!DbNetSuiteCore.controlArray[controlId]) {

            var clientControl = {}

            if (controlId.startsWith("Grid")) {
                clientControl = new GridControl(controlId, deferredLoad);
            }
            if (controlId.startsWith("Select")) {
                clientControl = new SelectControl(controlId);
            }
            if (controlId.startsWith("Form")) {
                clientControl = new FormControl(controlId);
            }
            if (controlId.startsWith("Tree")) {
                clientControl = new TreeControl(controlId);
            }
            for (const [key, value] of Object.entries(clientEvents)) {
                const functionNameParts: Array<string> = value.toString().split('.') as Array<string>;
                try {
                    if (functionNameParts.length > 1) {
                        (clientControl as any).eventHandlers[key] = {
                            type: window[functionNameParts[0].toString() as keyof Window][functionNameParts[1].toString() as keyof Window],
                            name: value.toString()
                        }
                    }
                    else {
                        (clientControl as any).eventHandlers[key] = {
                            type: window[functionNameParts[0].toString() as keyof Window],
                            name: value.toString()
                        }
                    }
                }
                catch (ex) {
                    console.error(`Client-side event handler => ${value} not found`);
                }
            }
            DbNetSuiteCore.controlArray[controlId] = clientControl;
            DbNetSuiteCore.namedControlArray[clientId] = clientControl;
        }
    }

    public static waitFor(conditionFn: any, interval: number = 50) {
        return new Promise<void>(resolve => {
            const check = () => {
                if (conditionFn()) {
                    resolve();
                } else {
                    setTimeout(check, interval);
                }
            };
            check();
        });
    }
}