using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sudoku
{
    internal enum Difficulty
    {
        Basic,
        Medium,
        Hard
    }

    internal enum PutResult
    {
        OK,
        Occupied,
        Cleared,
        Complete,
    }

    internal class SudokuData
    {
        private const int MaxCount = 9;
        public int[,] Table { get; private set; } = new int[MaxCount, MaxCount];
        public bool[,] Readonly { get; private set; } = new bool[MaxCount, MaxCount];

        public SudokuData(Difficulty d)
        {
            Random r = new Random((int)DateTime.Now.Ticks);
            Enumerable.Range(0, MaxCount).Select(x => Enumerable.Range(0, MaxCount)
                .Select(y => new { X = x, Y = y })).SelectMany(a => a)
                .AsParallel().ForAll(co =>
                {
                    this.Table[co.X, co.Y] = r.Next(MaxCount) + 1;
                });
        }

        private bool Check9(Func<int, int> fetchNumber)
        {
            var numbers = Enumerable.Range(0, MaxCount).Select(i => fetchNumber(i)).Distinct().OrderBy(i => i).ToArray();
            return numbers.Length == MaxCount && numbers[0] != 0;
        }

        public PutResult CheckResult()
        {
            bool complete = true;
            for (int i = 0; i < MaxCount; i++)
            {
                complete &= this.Check9(j => this.Table[i, j]);
                if (!complete) return PutResult.OK;
                complete &= this.Check9(j => this.Table[j, i]);
                if (!complete) return PutResult.OK;

                int row = (i / 3) * 3;
                int column = (i % 3) * 3;
                complete &= this.Check9(j => this.Table[column + j % 3, row + j / 3]);
                if (!complete) return PutResult.OK;
            }

            return PutResult.Complete;
        }

        public PutResult Put(int n, int x, int y)
        {
            if (this.Readonly[x, y])
            {
                return PutResult.Occupied;
            }

            if (n == 0)
            {
                if (this.Table[x, y] != 0)
                {
                    this.Table[x, y] = n;
                    return PutResult.Cleared;
                }
                else
                {
                    return PutResult.OK;
                }
            }
            else
            {
                this.Table[x, y] = n;
                return this.CheckResult();
            }
        }
    }
}
