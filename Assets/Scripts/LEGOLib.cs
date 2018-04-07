using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

using Random = UnityEngine.Random;

namespace LEGO {
    public enum Direction { NORTH, WEST, SOUTH, EAST }

    public class Brick {
        public readonly int[] Dimensions;
        public int[] Position { get; private set; }
        public Direction? Facing { get; private set; }
        public bool IsPlaced { get; private set; }
        private int[][] Bounds;
        public int BrickColor;
        private static int brickIDCounter = 0;
        public int BrickID;

        public Brick() : this(new int[] { 2, 3 }, 0) { }

        public Brick(int width, int depth) : this(new int[] { width, depth }) { }

        public Brick(int width, int depth, int color) : this(new int[] { width, depth }, color) { }

        public Brick(int[] dimensions, int color = 0) {
            BrickID = brickIDCounter++;
            Dimensions = dimensions;
            Position = null;
            Facing = null;
            IsPlaced = false;
            Bounds = null;
            BrickColor = color;
        }

        public Brick UpdateLocation(int[] position, Direction? direction = null) {
            Position = position;
            Facing = direction != null ? direction : Facing;
            IsPlaced = true;
            CalculateBounds();
            return this;
        }

        public void Shift(int x_offset, int y_offset, int z_offset = 0) {
            Position[0] += x_offset;
            Position[1] += y_offset;
            Position[2] += z_offset;
            CalculateBounds();
        }

        public int[][] GetBounds() {
            if (Bounds != null) {
                return Bounds;
            } else {
                CalculateBounds();
                return Bounds;
            }
        }

        public void CalculateBounds() {
            switch (Facing) {
                case Direction.NORTH:
                    Bounds = new int[][] { new int[] { Position[0], Position[1] }, new int[] { Position[0] + Dimensions[0] - 1, Position[1] + Dimensions[1] - 1 } };
                    break;
                case Direction.SOUTH:
                    Bounds = new int[][] { new int[] { Position[0] - Dimensions[0] + 1, Position[1] - Dimensions[1] + 1 }, new int[] { Position[0], Position[1] } };
                    break;
                case Direction.WEST:
                    Bounds = new int[][] { new int[] { Position[0] - Dimensions[1] + 1, Position[1] }, new int[] { Position[0], Position[1] + Dimensions[0] - 1 } };
                    break;
                case Direction.EAST:
                    Bounds = new int[][] { new int[] { Position[0], Position[1] - Dimensions[0] + 1 }, new int[] { Position[0] + Dimensions[1] - 1, Position[1] } };
                    break;
            }
        }

        public Rect GetRect() {
            switch (Facing) {
                case Direction.NORTH:
                    return new Rect(Position[0], Position[1], Dimensions[0], Dimensions[1]);
                case Direction.SOUTH:
                    return new Rect(Position[0] - Dimensions[0] + 1, Position[1] - Dimensions[1] + 1, Dimensions[0], Dimensions[1]);
                case Direction.WEST:
                    return new Rect(Position[0] - Dimensions[1] + 1, Position[1], Dimensions[1], Dimensions[0]);
                case Direction.EAST:
                    return new Rect(Position[0], Position[1] - Dimensions[0] + 1, Dimensions[1], Dimensions[0]);
            }
            throw new Exception("Cannot place brick there. Invalid Direction.");
        }

        public static Brick Create(int[] dimensions, int[] position, Direction direction) {
            return new Brick(dimensions).UpdateLocation(position, direction);
        }

        public override string ToString() {
            return string.Format("Brick[id={7},x={0},y={1},z={2},width={3},depth={4},direction={5},color={6}]", Position[0], Position[1], Position[2], Dimensions[0], Dimensions[1], Facing, BrickColor, BrickID);
        }
    }

    public class Connection {
        public readonly Brick TopBrick;
        public readonly Brick BottomBrick;

        public Connection(Brick topBrick, Brick bottomBrick) {
            TopBrick = topBrick;
            BottomBrick = bottomBrick;
        }
    }

    public class Structure {
        public readonly int[] Dimensions;
        public List<Brick> Pieces;
        public List<Brick>[] Layers;
        public List<Connection> Connections;
        private bool HeightAdjustmentsAllowed;

        public Structure() : this(new int[] { 8, 8, 8 }) { }

        public Structure(int width, int depth, int height) : this(new int[] { width, depth, height }) { }

        public Structure(int[] dimensions, bool allowHeightAdjustments = true) {
            Dimensions = dimensions;
            Pieces = new List<Brick>();
            Connections = new List<Connection>();
            Layers = new List<Brick>[Dimensions[2]];
            HeightAdjustmentsAllowed = allowHeightAdjustments;
            for (int i = 0; i < Dimensions[2]; i++) {
                Layers[i] = new List<Brick>();
            }
        }


        public Structure AddBrick(Brick brick, int[] position, Direction direction) {
            brick.UpdateLocation(position, direction);
            // Check if piece can be placed here.
            int[][] bounds = brick.GetBounds();
            Rect brickRect = brick.GetRect();
            foreach (Brick other in Layers[position[2]]) {
                if (brickRect.Overlaps(other.GetRect())) throw new Exception("Cannot place brick there. Collision.");
            }
            if (bounds[0][0] >= 0 && bounds[0][1] >= 0 && bounds[1][0] < Dimensions[0] && bounds[1][1] < Dimensions[1] && position[2] >= 0 && position[2] < Dimensions[2]) {
                Pieces.Add(brick);
                Layers[position[2]].Add(brick);
                PopulateConnections(brick);
                return this;
            } else {
                int[] newPosition = (int[])position.Clone();
                bool recalculateLayers = false;
                if (bounds[0][0] < 0) {
                    //Shift structure RIGHT
                    int shift = 0 - bounds[0][0];
                    foreach (Brick piece in Pieces) {
                        if (piece.GetBounds()[1][0] + shift >= Dimensions[0]) {
                            throw new Exception("Cannot place brick there. Out of bounds.");
                        }
                    }
                    foreach (Brick piece in Pieces) {
                        piece.Shift(shift, 0);
                    }
                    newPosition[0] += shift;
                }
                if (bounds[0][1] < 0) {
                    //Shift structure UP
                    int shift = 0 - bounds[0][1];
                    foreach (Brick piece in Pieces) {
                        if (piece.GetBounds()[1][1] + shift >= Dimensions[1]) {
                            throw new Exception("Cannot place brick there. Out of bounds.");
                        }
                    }
                    foreach (Brick piece in Pieces) {
                        piece.Shift(0, shift);
                    }
                    newPosition[1] += shift;
                }
                if (bounds[1][0] >= Dimensions[0]) {
                    //Shift structure LEFT
                    int shift = Dimensions[0] - bounds[1][0] - 1;
                    foreach (Brick piece in Pieces) {
                        if (piece.GetBounds()[0][0] + shift < 0) {
                            throw new Exception("Cannot place brick there. Out of bounds.");
                        }
                    }
                    foreach (Brick piece in Pieces) {
                        piece.Shift(shift, 0);
                    }
                    newPosition[0] += shift;
                }
                if (bounds[1][1] >= Dimensions[1]) {
                    //Shift structure LEFT
                    int shift = Dimensions[1] - bounds[1][1] - 1;
                    foreach (Brick piece in Pieces) {
                        if (piece.GetBounds()[0][1] + shift < 0) {
                            throw new Exception("Cannot place brick there. Out of bounds.");
                        }
                    }
                    foreach (Brick piece in Pieces) {
                        piece.Shift(0, shift);
                    }
                    newPosition[1] += shift;
                }
                if (position[2] < 0) {
                    if (!HeightAdjustmentsAllowed) throw new Exception("Cannot place brick there. Out of bounds.");
                    foreach (Brick piece in Pieces) {
                        if (piece.Position[2] + 1 >= Dimensions[2]) {
                            Dimensions[2]++;
                        }
                        piece.Shift(0, 0, 1);
                    }
                    newPosition[2]++;
                    recalculateLayers = true;
                }
                if (position[2] >= Dimensions[2]) {
                    if (!HeightAdjustmentsAllowed) throw new Exception("Cannot place brick there. Out of bounds.");
                    Dimensions[2] = position[2] - 1;
                }
                brick.UpdateLocation(newPosition, direction);
                Pieces.Add(brick);
                if (recalculateLayers) {
                    Layers = new List<Brick>[Dimensions[2]];
                    for (int i = 0; i < Dimensions[2]; i++) {
                        Layers[i] = new List<Brick>();
                    }
                    foreach (Brick piece in Pieces) {
                        Layers[piece.Position[2]].Add(piece);
                    }
                } else {
                    Layers[newPosition[2]].Add(brick);
                }
                PopulateConnections(brick);
                return this;
            }
        }

        public Structure AddBrick(Brick brick, int x, int y, int z, Direction direction) {
            return AddBrick(brick, new int[] { x, y, z }, direction);
        }

        private void PopulateConnections(Brick brick) {
            Rect brickRect = brick.GetRect();
            if (brick.Position[2] > 0) {
                foreach (Brick other in Layers[brick.Position[2] - 1]) {
                    if (brickRect.Overlaps(other.GetRect())) {
                        Connections.Add(new Connection(brick, other));
                    }
                }
            }
            if (brick.Position[2] < Dimensions[2] - 1) {
                foreach (Brick other in Layers[brick.Position[2] + 1]) {
                    if (brickRect.Overlaps(other.GetRect())) {
                        Connections.Add(new Connection(other, brick));
                    }
                }
            }
        }
    }

    public class StructureGenerator {
        public int PieceCount;
        public List<Brick> Pieces;
        public List<Brick> PlacedPieces;
        public int[] Dimensions;
        private Structure ResultStructure;

        private static int[][] PieceTypes = new int[][] { // {WEIGHT, WIDTH, DEPTH}
            new int[] { 5, 3, 2 },
            new int[] { 2, 4, 2 },
            new int[] { 3, 3, 1 },
            new int[] { 3, 4, 1 },
            new int[] { 2, 2, 2 }
        };

        public StructureGenerator(int pieces = 10, int[] dimensions = null) {
            PieceCount = pieces;
            Pieces = new List<Brick>();
            PlacedPieces = new List<Brick>();
            Dimensions = dimensions == null ? new int[] { 8, 8, 8 } : dimensions;
        }

        public Structure Generate() {
            Structure result = new Structure(Dimensions);

            // STEP 1: Choose N Random Bricks
            List<int> PieceOptions = new List<int>();
            for (int i = 0; i < PieceTypes.Length; i++) {
                for (int j = 0; j < PieceTypes[i][0]; j++) {
                    PieceOptions.Add(i);
                }
            }

            List<int> randomColors = HelperExtensions.IntRangeList(0, 10);
            randomColors.Shuffle();

            for (int i = 0; i < PieceCount; i++) {
                int pieceNum = PieceOptions[Random.Range(0, PieceOptions.Count)];
                Pieces.Add(new Brick(PieceTypes[pieceNum][1], PieceTypes[pieceNum][2], randomColors[i]));
            }

            // STEP 2: Randomize Brick Order and Place Starting Brick
            Pieces.Shuffle();

            Brick startingBrick = Pieces[0];
            result.AddBrick(startingBrick, 0, 0, 0, (Direction)Random.Range(0, 4));
            PlacedPieces.Add(startingBrick);

            List<int> directions = HelperExtensions.IntRangeList(0, 4);
            for (int currentBrick = 1; currentBrick < PieceCount; currentBrick++) {
                PlacedPieces.Shuffle();
                // Choose Random Placed Piece
                for (int n_brick = 0; n_brick < PlacedPieces.Count; n_brick++) {
                    Brick brick = PlacedPieces[n_brick];
                    // Choose Random Face
                    int r_face = Random.Range(0, 2);
                    for (int c_face = 0; c_face < 2; c_face++) {
                        int n_face = (c_face + r_face) % 2;
                        // Choose Random Stud
                        List<int> studs = HelperExtensions.IntRangeList(0, brick.Dimensions[0] * brick.Dimensions[1]);
                        for (int n_stud = 0; n_stud < brick.Dimensions[0] * brick.Dimensions[1]; n_stud++) {
                            directions.Shuffle();
                            for (int n_direction = 0; n_direction < 4; n_direction++) {
                                try {
                                    int[][] bounds = brick.GetBounds();
                                    // TODO: Check if bounds are same as another
                                    if (brick.Facing == Direction.NORTH || brick.Facing == Direction.SOUTH) {
                                        result.AddBrick(Pieces[currentBrick], bounds[0][0] + studs[n_stud] % brick.Dimensions[0], bounds[0][1] + studs[n_stud] / brick.Dimensions[0], brick.Position[2] + 2 * n_face - 1, (Direction)directions[n_direction]);
                                    } else {
                                        result.AddBrick(Pieces[currentBrick], bounds[0][0] + studs[n_stud] % brick.Dimensions[1], bounds[0][1] + studs[n_stud] / brick.Dimensions[1], brick.Position[2] + 2 * n_face - 1, (Direction)directions[n_direction]);
                                    }
                                    PlacedPieces.Add(Pieces[currentBrick]);
                                    goto next_brick;
                                } catch { }
                            }
                        }
                    }
                }
                next_brick:;
            }

            ResultStructure = result;
            return result;
        }

        public List<int[]> GetManualPages(bool showTop = true) {
            if (ResultStructure == null) throw new NullReferenceException();
            List<int[]> result = new List<int[]>();

            foreach (Connection connection in ResultStructure.Connections) {
                int[] grid = new int[Dimensions[0] * Dimensions[1]];
                Brick top = connection.TopBrick;
                Brick bottom = connection.BottomBrick;
                int[][] topBounds = top.GetBounds();
                int[][] bottomBounds = bottom.GetBounds();
                int minX = Math.Min(topBounds[0][0], bottomBounds[0][0]);
                int minY = Math.Min(topBounds[0][1], bottomBounds[0][1]);
                int maxX = Math.Max(topBounds[1][0], bottomBounds[1][0]);
                int maxY = Math.Max(topBounds[1][1], bottomBounds[1][1]);
                int width = maxX - minX + 1;
                int height = maxY - minY + 1;
                int shiftX = (Dimensions[0] - width) / 2 - minX;
                int shiftY = (Dimensions[1] - height) / 2 - minY;
                for (int x = bottomBounds[0][0] + shiftX; x <= bottomBounds[1][0] + shiftX; x++) {
                    for (int y = bottomBounds[0][1] + shiftY; y <= bottomBounds[1][1] + shiftY; y++) {
                        grid[y * Dimensions[0] + x] = bottom.BrickColor + 1;
                    }
                }
                if (showTop) {
                    for (int x = topBounds[0][0] + shiftX; x <= topBounds[1][0] + shiftX; x++) {
                        for (int y = topBounds[0][1] + shiftY; y <= topBounds[1][1] + shiftY; y++) {
                            grid[y * Dimensions[0] + x] = top.BrickColor + 1;
                        }
                    }
                }
                result.Add(grid);
            }

            return result;
        }

        public List<int[]> GetPieceDisplays(bool randomRotations = false) {
            if (ResultStructure == null) throw new NullReferenceException();
            List<int[]> result = new List<int[]>();

            foreach (Brick piece in ResultStructure.Pieces.OrderBy(x => x.BrickID)) {
            //foreach (Brick piece in ResultStructure.Pieces.OrderBy(x => x.BrickColor)) {
            int[] grid = new int[Dimensions[0] * Dimensions[1]];
                int[][] pieceBounds = piece.GetBounds();
                int minX = pieceBounds[0][0];
                int minY = pieceBounds[0][1];
                int maxX = pieceBounds[1][0];
                int maxY = pieceBounds[1][1];
                int width = maxX - minX + 1;
                int height = maxY - minY + 1;
                int shiftX = (Dimensions[0] - width) / 2 - minX;
                int shiftY = (Dimensions[1] - height) / 2 - minY;
                for (int x = pieceBounds[0][0] + shiftX; x <= pieceBounds[1][0] + shiftX; x++) {
                    for (int y = pieceBounds[0][1] + shiftY; y <= pieceBounds[1][1] + shiftY; y++) {
                        grid[y * Dimensions[0] + x] = piece.BrickColor + 1;
                    }
                }
                if (randomRotations) {
                    result.Add(grid.Rotate(Random.Range(0, 4), Dimensions[0], Dimensions[1]));
                } else {
                    result.Add(grid);
                }
            }

            return result;
        }

        public int[] GetSolutionDisplay(int face = 0) {
            if (ResultStructure == null) throw new NullReferenceException();
            int[] grid = new int[Dimensions[0] * Dimensions[1]];
            int minX = 99;
            int minY = 99;
            int maxX = 0;
            int maxY = 0;
            foreach (Brick piece in ResultStructure.Pieces) {
                int[][] bounds = piece.GetBounds();
                if (bounds[0][0] < minX) minX = bounds[0][0];
                if (bounds[0][1] < minY) minY = bounds[0][1];
                if (bounds[1][0] > maxX) maxX = bounds[1][0];
                if (bounds[1][1] > maxY) maxY = bounds[1][1];
            }
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            int shiftX = (Dimensions[0] - width) / 2 - minX;
            int shiftY = (Dimensions[1] - height) / 2 - minY;
            if (face == 1) {
                foreach (Brick piece in ResultStructure.Pieces.OrderBy(x => x.Position[2])) {
                    int[][] bounds = piece.GetBounds();
                    for (int x = bounds[0][0] + shiftX; x <= bounds[1][0] + shiftX; x++) {
                        for (int y = bounds[0][1] + shiftY; y <= bounds[1][1] + shiftY; y++) {
                            grid[y * Dimensions[0] + x] = piece.BrickColor + 1;
                        }
                    }
                }
            } else {
                foreach (Brick piece in ResultStructure.Pieces.OrderByDescending(x => x.Position[2])) {
                    int[][] bounds = piece.GetBounds();
                    for (int x = bounds[0][0] + shiftX; x <= bounds[1][0] + shiftX; x++) {
                        for (int y = bounds[0][1] + shiftY; y <= bounds[1][1] + shiftY; y++) {
                            grid[y * Dimensions[0] + (Dimensions[0] - 1 - x)] = piece.BrickColor + 1;
                        }
                    }
                }
            }
            return grid;
        }
    }
}

public static class HelperExtensions {
    public static void Shuffle<T>(this IList<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static T[] SubArray<T>(this T[] data, int index, int length) {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    public static T[] Rotate<T>(this T[] data, int direction, int dimX, int dimY) {
        T[] result = new T[data.Length];
        switch(direction) {
            case 0:
                return data;
            case 1:
                for (int i = 0; i < data.Length; i++) {
                    result[i] = data[dimX - 1 - (i / dimX) + (i % dimX) * dimX];
                }
                return result;
            case 2:
                for (int i = 0; i < data.Length; i++) {
                    result[i] = data[dimX - 1 - (i % dimX) + (dimY - 1 - (i / dimX)) * dimX];
                }
                return result;
            case 3:
                for (int i = 0; i < data.Length; i++) {
                    result[i] = data[(i / dimX) + (dimX - 1 - (i % dimX)) * dimX];
                }
                return result;
        }
        return null;
    }

    public static List<int> IntRangeList(int min, int max) {
        List<int> result = new List<int>();
        for (int i = min; i < max; i++) {
            result.Add(i);
        }
        result.Shuffle();
        return result;
    }

    public static IEnumerable<string> SplitInGroups(this string original, int size) {
        var p = 0;
        var l = original.Length;
        while (l - p > size) {
            yield return original.Substring(p, size);
            p += size;
        }
        yield return original.Substring(p);
    }
}