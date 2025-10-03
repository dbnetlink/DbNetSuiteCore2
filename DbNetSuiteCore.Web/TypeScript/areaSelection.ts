import Map from "esri/Map";
import Extent from "esri/geometry/Extent";
import GeometryEngine from "esri/geometry/geometryEngine";
import EsriMap from "esri/Map";
import MapView from "esri/views/MapView";
import Config from "esri/config";
import Polygon from "esri/geometry/Polygon";
import Graphic from "esri/Graphic";
import Color from "esri/Color";
import FeatureLayer from "esri/layers/FeatureLayer";
import GeoJSONLayer from "esri/layers/GeoJSONLayer";
import GraphicsLayer from "esri/layers/GraphicsLayer";
import SimpleFillSymbol from "esri/symbols/SimpleFillSymbol";
import LayerList from "esri/widgets/LayerList";
import Legend from "esri/widgets/Legend";
import EsriRequest from "esri/request";
import PopupTemplate from "esri/PopupTemplate";
import FeatureTable from "esri/widgets/FeatureTable";
import SimpleRenderer from "esri/renderers/SimpleRenderer";
import GeoJsonUtils from "esri/geometry/support/jsonUtils";
import * as geometryEngine from "esri/geometry/geometryEngine";
import { Geometry } from "esri/geometry";

//Config.apiKey = new Utility().esriApiKey();

const map = new Map({ basemap: "osm" });
const view = new MapView({
    container: "map",
    map: map,
    center: [-0.1657, 51.5074], // Hyde Park area
    zoom: 14
});

const selectionSymbol = new SimpleFillSymbol({
    color: new Color([255, 0, 0, 0.3]),
    outline: {
        color: [0, 0, 255, 0.3],
        width: 1
    }
});

const unselectionSymbol = new SimpleFillSymbol({
    color: new Color([0, 0, 255, 0.1]),
    outline: {
        color: [0, 0, 255, 0.3],
        width: 1
    }
});

let gridLayer = new GraphicsLayer();

let simpleRenderer = new SimpleRenderer({
    symbol : unselectionSymbol
})

const geojsonLayer = new GeoJSONLayer({
    url: "/geojson/hydepark.json",
    renderer: simpleRenderer
});
map.add(geojsonLayer);

geojsonLayer.queryFeatures().then((results) => {
    const polygon = results.features[0].geometry;
  //  generateGrid(polygon);
});



let selectedGraphics = [];

let formReference = null


map.add(gridLayer);

 (document.querySelector(".right-half") as HTMLDivElement).style.display = ''


view.on("click", async (event) => {
    console.log(`Latitude:${event.mapPoint.latitude} Longitude:${event.mapPoint.longitude}`)

    const opts = {
        include: gridLayer
    };
    view.hitTest(event, opts).then((response) => {
        if (response.results.length) {
            const graphic: Graphic = (response.results[0] as __esri.GraphicHit).graphic;
            if (graphic) {
                graphic.attributes.selected = !graphic.attributes.selected;

                if (graphic.attributes.selected) {
                    selectedGraphics.push(graphic);
                } else {
                    selectedGraphics = selectedGraphics.filter(g => g !== graphic);
                }

                graphic.symbol = graphic.attributes.selected ? selectionSymbol : unselectionSymbol;

                saveSelectedAreas()
                //   gridLayer.refresh();
            }
        }
    });


});

function saveSelectedAreas() {
    const geoJSONFeatures = selectedGraphics.map(graphic => {
        return graphic.geometry.toJSON();
    });

    formReference.formControl("geojson").value = JSON.stringify(geoJSONFeatures);
    console.log("Selected Geometries:", geoJSONFeatures);
}

export function restoreSelectedAreas(sender, args) {
    let geojsonString = sender.formControl("geojson").value
    selectedGraphics = [];
    const esriJsonPolygons = JSON.parse(geojsonString);

    console.log(`geojson:${geojsonString}`)

    const allGraphics = gridLayer.graphics.toArray();
    allGraphics.forEach((graphic) => { graphic.symbol = unselectionSymbol, graphic.attributes.selected = false });

    esriJsonPolygons.forEach(polygonJson => {
        const restoredGeometry = Polygon.fromJSON(polygonJson);
        for (const graphic of allGraphics) {
            const existingGeometry = graphic.geometry;
            if (geometryEngine.equals(existingGeometry, restoredGeometry)) {
                graphic.symbol = selectionSymbol;
                graphic.attributes.selected = true;
                selectedGraphics.push(graphic);
                break;
            }
        };
    });
}

export function saveFormReference(sender, args) {
    formReference = sender
}

function generateGrid(areaPolygon:__esri.Geometry) {
    view.when(() => {
        const extent = areaPolygon.extent

        const cellSize = 0.00225; // ~250m in degrees (approximate)
        const graphics = [];

        for (let x = extent.xmin; x < extent.xmax; x += cellSize) {
            for (let y = extent.ymin; y < extent.ymax; y += cellSize) {
                const square = new Polygon({
                    rings: [[
                        [x, y],
                        [x + cellSize, y],
                        [x + cellSize, y + cellSize],
                        [x, y + cellSize],
                        [x, y]
                    ]],
                    spatialReference: { wkid: 4326 }
                });

                if (geometryEngine.intersects(square, areaPolygon) == false) {
                    continue;
                }

                const graphic = new Graphic({
                    geometry: square,
                    symbol: unselectionSymbol,
                    attributes: { selected: false }
                });

                graphics.push(graphic);
            }
        }

        gridLayer.addMany(graphics);
        (document.querySelector(".right-half") as HTMLDivElement).style.display = ''
    });

}

/*
function generateGrid(areaPolygon) {
    const extent = areaPolygon.extent;
    gridLayer = new GraphicsLayer();
    map.add(gridLayer);

    const cellSize = 250; // meters
    const spatialRef = areaPolygon.spatialReference;

    const xmin = extent.xmin;
    const ymin = extent.ymin;
    const xmax = extent.xmax;
    const ymax = extent.ymax;

    const simpleFillSymbol = new SimpleFillSymbol({
        color: new Color([255, 0, 0, 0.3]),
        outline: {
            color: [0, 0, 255, 0.2],
            width: 1
        }
    });

    for (let x = xmin; x < xmax; x += cellSize) {
        for (let y = ymin; y < ymax; y += cellSize) {
            const square = new Polygon({
                rings: [
                    [x, y],
                    [x + cellSize, y],
                    [x + cellSize, y + cellSize],
                    [x, y + cellSize],
                    [x, y]
                ],
                spatialReference: spatialRef
            });

            if (geometryEngine.intersects(square, areaPolygon)) {
                const graphic = new Graphic({
                    geometry: square,
                    symbol: simpleFillSymbol
                });
                gridLayer.add(graphic);
            }
        }
    }

    return gridLayer;
}
*/