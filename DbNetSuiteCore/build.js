import * as esbuild from 'esbuild';
import fs from 'fs';

const files = [
    'TypeScript/componentControl.ts',
    'TypeScript/gridControl.ts',
    'TypeScript/selectControl.ts',
    'TypeScript/formControl.ts',
    'TypeScript/treeControl.ts',
    'TypeScript/draggableDialog.ts',
    'TypeScript/dialog.ts',
    'TypeScript/viewDialog.ts',
    'TypeScript/confirmDialog.ts',
    'TypeScript/htmlEditor.ts',
    'TypeScript/searchDialog.ts',
    'TypeScript/lookupDialog.ts'
];

const merged = files.map(f => fs.readFileSync(f, 'utf8')).join('\n');

await esbuild.build({
    stdin: {
        contents: merged,
        loader: 'ts',
        resolveDir: 'TypeScript',
    },
    bundle: false,  // no module resolution, just transpile
    target: 'es6',
    keepNames: true,
    outfile: 'Resources/JS/dbnetsuite-core.bundle.js',
});