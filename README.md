# SortingVisualization.WPF
Playing around with visualizing sorting algorithms in WPF

Saw these [images](https://imgur.com/gallery/RM3wl) from a HN post, and I thought it was really interesting. So I thought I would give it a try myself, and quickly pieced together a WPF app. Most of the algorithms I found were in Stackoverflow or some tech articles. WPF is probably not ideal/optimal for animation, but it was quick to put together.

## Videos

* [Insertion Sort - 1](https://youtu.be/ECHz2fVbDM0)
* [Insertion Sort - 2](https://youtu.be/rrtV4Y1mDWA)
* [Insertion Sort - Spiral - 1](https://youtu.be/8e8YrrR1LWA)
* [Insertion Sort - Spiral - 2](https://youtu.be/ns-0n_AhKNM)
* [Quicksort](https://youtu.be/-fF6FpphYoA)
* [Heapsort](https://youtu.be/btx6aXcwivs)

## Notes
* Clean code so it's easier to add new sort algorithms
* Find a faster / more efficient way to draw bitmaps (Look into WriteableBitmap)
* Play around with the pixels ordinal value (top to bottom and left to right), (spiral order?)

### Top-Bottom-Left-Right
Currently this is how the pixels are ordered. 
```
0   1  2  3  4 
5   6  7  8  9 
10 11 12 13 14 
15 16 17 18 19
```

### Spiral 
Starting from the outside, and working inwards. Ordering the pixels in this way should give an interesting animation.

```
0  1  2  3  4
13 14 15 16 5
12 19 18 17 6
11 10 9  8  7
```
