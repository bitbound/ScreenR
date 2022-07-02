class Rectangle {
    top;
    left;
    width;
    height;
}

class DrawUnmarshalledInfo extends Rectangle {
    canvasId;
}

var decoder = new TextDecoder("utf-8");

/**
 * 
 * @param {HTMLCanvasElement} canvas
 * @param {Uint8Array} imageBytes
 * @param {Rectangle} area
 */
export async function drawImage(canvas, imageBytes, area) {
    let context2D = canvas.getContext("2d");
    
    let bitmap = await createImageBitmap(new Blob([imageBytes]));

    context2D.drawImage(bitmap, 0, 0, area.width, area.height);

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