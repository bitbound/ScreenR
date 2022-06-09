class Rectangle {
    x;
    y;
    width;
    height;
    left;
    top;
    right;
    bottom;
    isEmpty
}

/**
 * 
 * @param {HTMLCanvasElement} canvas
 * @param {Uint8Array} imageBytes
 * @param {Rectangle} area
 */
export async function drawImage(canvas, imageBytes, area) {
    console.log(canvas);
    console.log(imageBytes);
    console.log(area);

    // For use with unmarshalled JS interop.
    //const dataPtr = Blazor.platform.getArrayEntryPtr(imageBytes, 0, 4);
    //const length = Blazor.platform.getArrayLength(imageBytes);
    //var imageArray = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);

    let context2D = canvas.getContext("2d");
    
    let bitmap = await createImageBitmap(new Blob([imageBytes]));

    context2D.drawImage(bitmap, 0, 0, area.width, area.height);

    return 0;
}