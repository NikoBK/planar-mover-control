using System.Numerics;

namespace PlanarMoverControl
{
    public class Constants
    {
        /* TILES
         *
         * Note that each tile is 60x60 (0.060f x 0.060f) where placing the shuttle at any interval of 60 will align it
         * perfectly within or across multiple tiles. The table surface is 6 (w) x 8 (h) tiles in dimension with its current setup
         * which means the top left corner Y should be: y = (8 * 60 * 2) - 60 = 0.900f and the top right corner x should be x = (6 * 60 * 2) - 60 = 0.660f.
         *
         */

        public static Dictionary<int, Vector2> OuterCorners = new Dictionary<int, Vector2>() {
            {0, new Vector2(0.060f, 0.060f)},
            {1, new Vector2(0.060f, 0.900f)},
            {2, new Vector2(0.660f, 0.900f)},
            {3, new Vector2(0.660f, 0.060f)}
        };

        // Viapoints for 'outer' highway - closest proximity to the table top's edge
        public static Dictionary<int, Vector2> MovePointsTest = new Dictionary<int, Vector2>() {
            // Key: Station Position, Value: Pos (X, Y)
            {0, new Vector2(0.060f, 0.060f)},
            {1, new Vector2(0.660f, 0.060f)},
            {2, new Vector2(0.060f, 0.450f)},
            {3, new Vector2(0.660f, 0.450f)},
            {4, new Vector2(0.060f, 0.900f)},
            {5, new Vector2(0.660f, 0.900f)},
        };

        // A list of viapoints that runs one or more movers on the inner highway (closest proximity to obstacle)
        public static Dictionary<int, Vector2> InnerHighwayViaPoints = new Dictionary<int, Vector2>() {
            // Key: Viapoint id, Value: Pos (X,Y)
            {0, new Vector2(0.060f, 0.060f)},
            {3, new Vector2(0.060f, 0.060f)},
            {2, new Vector2(0.060f, 0.060f)},
            {1, new Vector2(0.060f, 0.060f)},
            {4, new Vector2(0.060f, 0.060f)},
            {6, new Vector2(0.060f, 0.060f)}
        };
    }
}
