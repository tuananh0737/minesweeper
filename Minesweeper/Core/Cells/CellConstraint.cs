using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper.Core.Cells
{
    public class CellConstraint
    {
        public Cell Cell { get; set; }
        public HashSet<Cell> Constraints { get; set; } = new HashSet<Cell>();
        public HashSet<Cell> WhatIConstrain { get; set; } = new HashSet<Cell>();
        public int NumMines { get; set; }
    }
}
