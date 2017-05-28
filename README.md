# MorphSVG

Creates a GIF that morphs between pairs of SVG paths.

Takes a filename as an argument, which contains all the configuration for creating a GIF out of the SVG file. The pairs of SVG IDs are specified in it, plus other properties for the PNG rendering and GIF creation.

Morphing will be performed from the first ID to the second ID in each pair, showing only the interpolated version. Interpolation can be either linear or hermite.

Types of SVG objects supported are PATH, CIRCLE and ELLIPSE. Not all kinds of paths will work, both paths for each pair must have the same number of segments with the same kind of movement (with some exceptions).

Uses Inkscape for PNG rendering and ImageMagick for GIF creation. Paths to both are specified as constants inside the C# file.

**Disclaimer**: Will not probably work out-of-the-box for any SVG file, since the code was created for just one project.

Example SVG and config files provided, that create this output (after resizing and extreme compression by GFYcat):

![WEBM output](https://thumbs.gfycat.com/SpiffyFalseAuklet-size_restricted.gif)
