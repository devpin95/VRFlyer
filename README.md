# VRFlyer

A VR game where you fly a helicopter around a low-poly world.

# Progress

## July 2022, Refueling, Setting VR Position, Procedural Lakes

Showing fuel usage and refueling at pads, new position menu for resetting camera position, and new procedural lakes using distance functions to lower overall terrain height to ensure a lake will fit within a single terrain chunk.

[(Video)](https://www.youtube.com/watch?v=0Zpy7_TObtM)

## May 2022, Trees Attempt 1

First try at generating trees on the terrain. Just made a pool of 10000 tree objects at the start of the game that the vegetation component can pull from. A grid of chunks is set up the same way the terrain is created. Each terrain chunk has a grid of vegetation chunks that fit within it's bounds. Every time the terrain chunk is generated with a new offset, the vegetation grid is looped through and any chunk that is "too high" (right now it's just an arbitrary height) is deactivated. That just means that we won't generate any trees there. After initializing the vegetation grid, we loop through the grid again and find it's LOD - if the chunk is close enough to the player, then we activate the chunk and generate the trees inside it. Then as the player moves, we loop through the chunks again and decide if a chunk is now close enough or too far away from the player. Whenever a chunk goes from unactive to active, tree's for that chunk are generated. Generating the trees is just randomly selecting a (x, z) coordinate and sampling the height map at that point. Since the height map is a discrete grid, we just use Bilinear interpolation to figure out the actual height at the (x, z) point.

https://en.wikipedia.org/wiki/Bilinear_interpolation

Each tree prefab has its materials to use GPU instancing, which makes the game run really fast when there are a lot of trees in the scene. The only performance hit right now is generating/removing the trees for each chunk. The functions for allocating and deallocating trees are Coroutines that place/remove trees in batches, but so far that doesn't seen to make it run much faster. I need to find a better way of doing this because FPS drops to ~40 while the chunks are being handled which is way too slow for VR.

[(Video)](https://www.youtube.com/watch?v=NRWPsG2M1z0)

## May 2022, Using the GPS to fly to a different pad

Just a quick flight between the starting pad and a random pad that spawned nearby. Each terrain chunk has a chance so spawn a pad. When it does spawn a pad, it reports it's location to the GPS who then adds an icon to the map and it's details to the destination list. If you select the helipad (or any destination, I'll add different POIs eventually), the GPS draws a line between the helicopter location and the destination so that you can follow it to the pad. In this video, the GPS also shows the distance in miles to the pad along with the "ETA" which right now doesn't really work but at least it gives a time.

[(Video)](https://www.youtube.com/watch?v=3OFLG2NmkWk)

## May 2022, Quick flight with GPS

Just flying around with the new gps and landing pads.

[(Video)](https://www.youtube.com/watch?v=3m1aTgvAZ5w)

## Jan 2022, Flying for an hour with infinite terrain

Just a timelapse of flying for an hour with the first iteration of infinite terrain. I watched an episode of The Witcher while I flew

[(Video)](https://www.youtube.com/watch?v=902SQoiaaV4)

### Jan 2022, Attitude Indicator Demo

just a quick demo of the attitude indicator I made for the helicopter

[(Video)](https://www.youtube.com/watch?v=pY0isyZu_jk)

## Dec 2021, Terrain Test

[(Video)](https://www.youtube.com/watch?v=pWh-TwPLZEQ)

## Dec 2021, VR Helicopter Test Flight

[(Video)](https://www.youtube.com/watch?v=YySf_Qwh5A4)