using UnityEngine;

public static class MapFunctions
{
    /// <summary>
    /// Creates a tunnel of length height. Takes into account roughness and windyness
    /// </summary>
    /// <param name="map">The array that holds the map information</param>
    /// <param name="width">The width of the map</param>
    /// <param name="height">The height of the map</param>
    /// <param name="minPathWidth">The min width of the path</param>
    /// <param name="maxPathWidth">The max width of the path, ensure it is smaller than then width of the map</param>
    /// <param name="maxPathChange">The max amount we can change the center point of the path by</param>
    /// <param name="roughness">How much the edges of the tunnel vary</param>
    /// <param name="windyness">how much the direction of the tunnel varies</param>
    /// <returns>The map after being tunneled</returns>
    public static int[,] DirectionalTunnel(int seed, int[,] map, int minPathWidth, int maxPathWidth, int maxPathChange, int roughness, int windyness, int tunnelCenterDivisor)
    {
        //This value goes from its minus counterpart to its positive value, in this case with a width value of 1, the width of the tunnel is 3
        int tunnelWidth = 1;

        //Set the start X position to the center of the tunnel
        int x = tunnelCenterDivisor;

        //Set up our seed for the random.
        System.Random rand = new System.Random(seed);

        //Create the first part of the tunnel
        for (int i = -tunnelWidth; i <= tunnelWidth; i++)
        {
            map[x + i, 0] = 0;
        }

        //Cycle through the array
        for (int y = 1; y < map.GetUpperBound(1); y++)
        {
            //Check if we can change the roughness
            if (rand.Next(0, 100) > roughness)
            {

                //Get the amount we will change for the width
                int widthChange = rand.Next(-maxPathWidth, maxPathWidth);
                tunnelWidth += widthChange;

                //Check to see we arent making the path too small
                if (tunnelWidth < minPathWidth)
                {
                    tunnelWidth = minPathWidth;
                }

                //Check that the path width isnt over our maximum
                if (tunnelWidth > maxPathWidth)
                {
                    tunnelWidth = maxPathWidth;
                }
            }

            //Check if we can change the windyness
            if (rand.Next(0, 100) > windyness)
            {
                //Get the amount we will change for the x position
                int xChange = rand.Next(-maxPathChange, maxPathChange);
                x += xChange;

                //Check we arent too close to the left side of the map
                if (x < maxPathWidth)
                {
                    x = maxPathWidth;
                }
                //Check we arent too close to the right side of the map
                if (x > (map.GetUpperBound(0) - maxPathWidth))
                {
                    x = map.GetUpperBound(0) - maxPathWidth;
                }

            }

            //Work through the width of the tunnel
            for (int i = -tunnelWidth; i <= tunnelWidth; i++)
            {
                map[x + i, y] = 0;
            }
        }
        return map;
    }

    /// <summary>
    /// Generates the top layer of our level using Random Walk
    /// </summary>
    /// <param name="map">Map that we are using to generate</param>
    /// <param name="seed">The seed we will use in our random</param>
    /// <returns>The random walk map generated</returns>
    public static int[,] RandomWalkTop(int[,] map, float seed)
    {
        //Seed our random
        System.Random rand = new System.Random(seed.GetHashCode());

        //Set our starting height
        int lastHeight = rand.Next(0, map.GetUpperBound(1));

        //Cycle through our width
        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            //Flip a coin
            int nextMove = rand.Next(2);

            //If heads, and we aren't near the bottom, minus some height
            if (nextMove == 0 && lastHeight > 2)
            {
                lastHeight--;
            }
            //If tails, and we aren't near the top, add some height
            else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) - 2)
            {
                lastHeight++;
            }

            //Circle through from the lastheight to the bottom
            for (int y = lastHeight; y >= 0; y--)
            {
               // if (map[x, y] == 0) continue;

                map[x, y] = 1;
            }
        }
        //Return the map
        return map;
    }

    /// <summary>
	/// Generates a smoothed random walk top.
	/// </summary>
	/// <param name="map">Map to modify</param>
	/// <param name="seed">The seed for the random</param>
	/// <param name="minSectionWidth">The minimum width of the current height to have before changing the height</param>
	/// <returns>The modified map with a smoothed random walk</returns>
	public static int[,] RandomWalkTopSmoothed(int[,] map, float seed, int minSectionWidth)
    {
        //Seed our random
        System.Random rand = new System.Random(seed.GetHashCode());

        //Determine the start position
        int lastHeight = rand.Next(0, map.GetUpperBound(1));
        //Used to keep track of the current sections width
        int sectionWidth = 0;

        //Work through the array width
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            //Used to determine which direction to go
            //Determine the next move
            int nextMove = rand.Next(2);

            //Only change the height if we have used the current height more than the minimum required section width
            if (nextMove == 0 && lastHeight > 0 && sectionWidth > minSectionWidth)
            {
                lastHeight--;
                sectionWidth = 0;
            }
            else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) && sectionWidth > minSectionWidth)
            {
                lastHeight++;
                sectionWidth = 0;
            }
            //Increment the section width
            sectionWidth++;
            
            //Work our way from the height down to 0
            for (int y = lastHeight; y >= 0; y--)
            {
               // if (map[x, y] == 0) continue;

                map[x, y] = 1;
            }
        }

        //Return the modified map
        return map;
    }

    public static int[,] GenerateCellularAutomata(int width, int height, float seed, int fillPercent, bool edgesAreWalls)
    {
        //Seed our random number generator
        System.Random rand = new System.Random(seed.GetHashCode());

        //Initialise the map
        int[,] map = new int[width, height];

        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            for (int y = 0; y < map.GetUpperBound(1); y++)
            {
                //If we have the edges set to be walls, ensure the cell is set to on (1)
                if (edgesAreWalls && (x == 0 || x == map.GetUpperBound(0) - 1 || y == 0 || y == map.GetUpperBound(1) - 1))
                {
                    map[x, y] = 1;
                }
                else
                {
                    //Randomly generate the grid
                    map[x, y] = (rand.Next(0, 100) < fillPercent) ? 1 : 0;
                }
            }
        }
        return map;
    }

    public static int[,] SmoothMooreCellularAutomata(int[,] map, bool edgesAreWalls, int smoothCount)
    {
        for (int i = 0; i < smoothCount; i++)
        {
            for (int x = 0; x < map.GetUpperBound(0); x++)
            {
                for (int y = 0; y < map.GetUpperBound(1); y++)
                {
                    int surroundingTiles = GetMooreSurroundingTiles(map, x, y, edgesAreWalls);

                    if (edgesAreWalls && (x == 0 || x == (map.GetUpperBound(0) - 1) || y == 0 || y == (map.GetUpperBound(1) - 1)))
                    {
                        //Set the edge to be a wall if we have edgesAreWalls to be true
                        map[x, y] = 1;
                    }
                    //The default moore rule requires more than 4 neighbours
                    else if (surroundingTiles > 4)
                    {
                        map[x, y] = 1;
                    }
                    else if (surroundingTiles < 4)
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }
        //Return the modified map
        return map;
    }

    static int GetMooreSurroundingTiles(int[,] map, int x, int y, bool edgesAreWalls)
    {
        /* Moore Neighbourhood looks like this ('T' is our tile, 'N' is our neighbours)
         * 
         * N N N
         * N T N
         * N N N
         * 
         */

        int tileCount = 0;

        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < map.GetUpperBound(0) && neighbourY >= 0 && neighbourY < map.GetUpperBound(1))
                {
                    //We don't want to count the tile we are checking the surroundings of
                    if (neighbourX != x || neighbourY != y)
                    {
                        tileCount += map[neighbourX, neighbourY];
                    }
                }
            }
        }
        return tileCount;
    }
}
