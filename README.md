# KristofferStrube.Blazor.SVGEditor
A basic HTML SVG Editor written in Blazor WASM.

![Showcase](./docs/showcase.gif?raw=true)

## Demo
The project can be demoed at [https://kristofferstrube.github.io/Blazor.SVGEditor/](https://kristofferstrube.github.io/Blazor.SVGEditor/)

## Tag type support and attributes
- RECT (x, y, width, height, fill, stroke, stroke-width)
- POLYGON (points, fill, stroke, stroke-width)
- PATH (d, fill, stroke, stroke-width)
    - Movements
    - Lines
    - Vertical Lines
    - Horizontal Lines
    - Close Path

## Current goals
- Add support for touch devices
- Implement rest of sequences for path data
- Optimize path data so that it does not depend on chains of positions for relative sequences, but updates when needed instead.
- Show text in SVG.
- Implement text edit in SVG.
- Support more browsers using style attribute.