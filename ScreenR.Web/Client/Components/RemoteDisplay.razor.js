class Rectangle {
    /**
     * @type {number}
     */
    top;

    /**
     * @type {number}
     */
    left;

    /**
     * @type {number}
     */
    width;

    /**
     * @type {number}
     */
    height;
}

class DrawUnmarshalledInfo extends Rectangle {
    canvasId;
}

/**
 * 
 * @param {HTMLCanvasElement} canvas
 * @param {Uint8Array} imageBytes
 * @param {Rectangle} area
 */
export async function drawImage(canvas, imageBytes, area) {
    let context2D = canvas.getContext("2d");
    
    let bitmap = await createImageBitmap(new Blob([imageBytes]));

    context2D.drawImage(bitmap, area.left, area.top, area.width, area.height);

    return 0;
}

export async function drawImageUnmarshalled(imageBytesPtr, drawInfo) {
    let imageArray = Blazor.platform.toUint8Array(imageBytesPtr);

    let left = Blazor.platform.readInt32Field(drawInfo, 0);
    let top = Blazor.platform.readInt32Field(drawInfo, 4);
    let width = Blazor.platform.readInt32Field(drawInfo, 8);
    let height = Blazor.platform.readInt32Field(drawInfo, 12);
    let canvasId = Blazor.platform.readStringField(drawInfo, 16);
    
    let canvas = document.getElementById(canvasId);
    let context2D = canvas.getContext("2d");

    let bitmap = await createImageBitmap(new Blob([imageArray]));

    context2D.drawImage(bitmap, left, top, width, height);
    
    return imageArray.byteLength;
}

/**
 * Retains a reference to the RemoteDisplay Blazor component and registers
 * event handlers for the canvas.
 * @param {any} remoteDisplayRef
 * @param {string} canvasId
 */
export async function setRemoteDisplay(remoteDisplayRef, canvasId) {
    let canvas = document.getElementById(canvasId);
    let touchCount = 0;

    canvas.addEventListener("touchstart", ev => {
        touchCount = ev.touches.length;
    });

    canvas.addEventListener("touchend", ev => {
        touchCount = ev.touches.length;
    });
    // TODO
    canvas.addEventListener("pointermove", ev => {
        if (ev.pointerType == "touch") {
            
        }
        else {

        }
    });

    canvas.addEventListener("pointerdown", ev => {
        if (ev.pointerType == "touch") {

        }
        else {

        }
    });

    canvas.addEventListener("pointerup", ev => {
        if (ev.pointerType == "touch") {

        }
        else {

        }
    });

    canvas.addEventListener("contextmenu", ev => {
        ev.preventDefault();
    });
}