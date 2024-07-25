using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Minesweeper.Core.Boards;
using Minesweeper.Core.Cells;

namespace Minesweeper.Core
{
    public class Cell
    {
        public int XLoc { get; set; }
        public int YLoc { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
        public Point CenterPos { get; set; }
        public Point TopLeftPos { get; set; }
        public Point BottomLeftPos { get; set; }
        public int CellSize { get; set; }
        public CellState CellState { get; set; }
        public CellType CellType { get; set; }
        public int NumMines { get; set; }
        public Board Board { get; set; }
        public decimal MinePercentage { get; set; }
        public decimal ConstraintPercentage { get; set; }
        public Rectangle Bounds { get; private set; }
        private List<Cell> Surrounding { get; set; }
        public CellConstraint Constraint { get; private set; }

        /// <summary>
        /// Which cells around this cell have been flagged already?
        /// </summary>
        public List<Cell> SurroundingFlagged => GetNeighborCells().Where(cell => cell.Flagged).ToList();

        /// <summary>
        /// How many mines has this cell still got around it to be identified?
        /// </summary>
        public int MinesRemaining => Opened && NumMines > 0 ? (NumMines - SurroundingFlagged.Count) : 0;

        /// <summary>
        /// Has this cell been marked as a mine?
        /// </summary>
        public bool Flagged => CellType == CellType.Flagged || CellType == CellType.FlaggedMine;

        /// <summary>
        /// Is this cell still closed?
        /// </summary>
        public bool Closed => CellState == CellState.Closed;

        /// <summary>
        /// Has this cell been opened?
        /// </summary>
        public bool Opened => CellState == CellState.Opened;

        /// <summary>
        /// Return whether the type of this cell is a Mine.
        /// </summary>
        /// <returns></returns>
        public bool IsMine => CellType == CellType.Mine || CellType == CellType.FlaggedMine;

        /// <summary>
        /// Constructs a new <see cref="Cell"/>
        /// </summary>
        public Cell(int x, int y, Board board)
        {
            XLoc = x;
            YLoc = y;
            CellSize = Board.CellSize;
            CellState = CellState.Closed;
            CellType = CellType.Regular;
            MinePercentage = -1;
            ConstraintPercentage = -1;
            Bounds = new Rectangle(XLoc * CellSize, YLoc * CellSize, CellSize, CellSize);
            Board = board;
            XPos = XLoc * CellSize;
            YPos = YLoc * CellSize;
            CenterPos = new Point(XPos + (CellSize / 2 - 10), YPos + (CellSize / 2 - 10));
            TopLeftPos = new Point(XPos, YPos);
            BottomLeftPos = new Point(XPos, YPos + (CellSize - 10));
            Constraint = new CellConstraint { Cell = this };
        }

        /// <summary>
        /// Responds to user click event to flag this cell.
        /// </summary>
        public void OnFlag()
        {
            CellType = CellType switch
            {
                CellType.Regular => CellType.Flagged,
                CellType.Mine => CellType.FlaggedMine,
                CellType.Flagged => CellType.Regular,
                CellType.FlaggedMine => CellType.Mine,
                _ => throw new Exception($"Unknown cell type: {CellType}"),
            };

            Board.Minesweeper.UpdateMinesRemaining();
            Board.Minesweeper.Invalidate();
        }

        /// <summary>
        /// Responds to user click event to open this cell.
        /// </summary>
        public void OnClick(bool recursiveCall = false)
        {
            // Recursive cell opening stops when it gets to a non-regular cell or a cell that's already open.
            if (recursiveCall)
            {
                if (CellType != CellType.Regular || CellState != CellState.Closed)
                    return;
            }

            // Cell was a mine
            if (CellType == CellType.Mine)
            {
                CellState = CellState.Opened;
                Board.RevealMines();
                return;
            }

            // Regular cell
            if (CellType == CellType.Regular)
            {
                CellState = CellState.Opened;
            }

            // Recursively open surrounding cells.
            if (NumMines == 0 || MinesRemaining == 0)
            {
                foreach (var n in GetNeighborCells())
                {
                    n.OnClick(true);
                }
            }
        }

        /// <summary>
        /// Get a list of cells that are directly neighboring a provided cell.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetNeighborCells()
        {
            if (Surrounding == null)
            {
                Surrounding = new List<Cell>();

                for (var x = -1; x < 2; x++)
                {
                    for (var y = -1; y < 2; y++)
                    {
                        // Can't be your own neighbor!
                        if (x == 0 && y == 0)
                            continue;

                        // Cell would be out of bounds
                        if (XLoc + x < 0 || XLoc + x >= Board.Width || YLoc + y < 0 || YLoc + y >= Board.Height)
                            continue;

                        Surrounding.Add(Board.Cells[XLoc + x, YLoc + y]);
                    }
                }
            }

            return Surrounding;
        }

        /// <summary>
        /// Work out the percentage % of this cell being a mine.
        /// </summary>
        /// <returns></returns>
        public void CalculateMinePercentage()
        {
            // Cell has been flagged, we can assume this is always going to be a mine.
            if (Flagged)
            {
                MinePercentage = 100;
                return;
            }

            // Cell has been opened, this therefore can never be a mine.
            if (Opened)
            {
                MinePercentage = 0;
                return;
            }

            // Percentage has already been hard-set so there's no need to re-calculate.
            if (MinePercentage == 0M || MinePercentage == 100M)
            {
                return;
            }

            var percent = 0M;
            var checkedCells = 0;

            foreach (var nc in GetNeighborCells())
            {
                var surroundingMines = nc.NumMines;

                if (surroundingMines < 1)
                {
                    continue;
                }

                if (nc.CellState == CellState.Closed)
                {
                    continue;
                }

                var availCells = nc.GetNeighborCells().Where(ncc => ncc.Closed && !ncc.Flagged).ToList().Count;
                var flaggedCells = nc.GetNeighborCells().Where(ncc =>ncc.Flagged).ToList().Count;
                var leftToFind = surroundingMines - flaggedCells;

                // 0% chance of being a mine
                if (flaggedCells == surroundingMines)
                {
                    MinePercentage = 0;
                    return;
                }

                // 100% of being a mine
                if (surroundingMines == (availCells + flaggedCells))
                {
                    MinePercentage = 100;
                    return;
                }

                checkedCells += 1;
                percent += (leftToFind * 1.0M / availCells) * 100;
            }

            // Unable to determine - did not consider any cells.
            if (checkedCells == 0)
            {
                MinePercentage = -1;
                return;
            }

            MinePercentage = Math.Round(percent / (checkedCells > 0 ? checkedCells : 1));
        }

        public decimal MinePercentageFromConstraints()
        {
            var percent = 0M;
            return percent;
        }

        /// <summary>
        /// Updates this cells constraints.
        /// </summary>
        public void UpdateConstraints()
        {
            // Clear existing:
            Constraint.Constraints.Clear();
            Constraint.NumMines = 0;

            // Cell has already been solved or not even opened yet.
            if (MinesRemaining == 0 || Closed || Flagged)
            {
                return;
            }

            // Re-calculate:
            foreach (var cell in GetNeighborCells())
            {
                // Opened/already flagged/already resolved are of no use:
                if (cell.Closed && !cell.Flagged)
                {
                    cell.Constraint.WhatIConstrain.Add(this);

                    Constraint.Constraints.Add(cell);
                }
            }

            Constraint.NumMines = MinesRemaining;
        }

        /// <summary>
        /// Attempt to resolve the constraints of this cell to identify other than obvious locations
        /// where the mines/clear cells are located.
        /// </summary>
        public void ResolveConstraints()
        {
            var safe = new HashSet<Cell>();
            var mines = new HashSet<Cell>();

            foreach (var cell in GetNeighborCells())
            {
                var currentSet = cell.Constraint.Constraints;
                var masterSet = Constraint.Constraints;

                // If this is a subset of constrains the the differences in the sets are mines.
                if (currentSet.IsSubsetOf(masterSet))
                {
                    // Cells that are different in the set
                    var difference = masterSet.Except(currentSet).ToList();

                    // Providing that the subset can fully satisfy the superset
                    if (cell.Constraint.NumMines == Constraint.NumMines)
                    {
                        // Subsets constraints satisfy the superset. Any cells difference cannot be mines.
                        foreach (var cs in difference)
                        {
                            safe.Add(cs);
                        }
                    }

                    // Number of mines remaining here does not fully satisfy, (some or all) of the difference cells will be mines.
                    if (cell.Constraint.NumMines < Constraint.NumMines)
                    {
                        // Cells difference must all be mines to satisfy the master set after considering the subset.
                        if (difference.Count + cell.Constraint.NumMines == Constraint.NumMines)
                        {
                            foreach (var cs in difference)
                            {
                                mines.Add(cs);
                            }
                        }
                    }
                }
            }

            // Mark any cells identified as safe as never containing a mine:
            foreach (var cell in safe)
            {
                cell.MinePercentage = 0M;
            }

            // Mark any cells identified as mines as 100% being a mine:
            foreach (var cell in mines)
            {
                cell.MinePercentage = 100M;
            }
        }
    }
}
