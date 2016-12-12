using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        Random r = new Random((int)DateTime.Now.Ticks);

        private int[,] RandomGame = new int[9, 9]
        {
            { 8,0,0,0,0,0,0,0,4 },
            { 0,0,3,7,0,4,0,8,1 },
            { 1,0,7,0,0,0,5,3,2 },
            { 0,8,0,0,0,0,0,2,7 },
            { 0,0,6,0,0,0,0,0,0 },
            { 0,2,0,3,0,0,4,0,9 },
            { 7,0,0,5,8,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0 },
            { 0,9,0,1,0,0,0,0,0 },
        };

        public SudokuData(Difficulty d)
        {
            var numShuffle = this.GetRandomShuffle(9);

            var rowShuffle = this.GetRandomShuffle(3);
            var rowInnerShuffle = new int[3][]
            {
                this.GetRandomShuffle(3),
                this.GetRandomShuffle(3),
                this.GetRandomShuffle(3),
            };

            var colShuffle = this.GetRandomShuffle(3);
            var colInnerShuffle = new int[3][]
            {
                this.GetRandomShuffle(3),
                this.GetRandomShuffle(3),
                this.GetRandomShuffle(3),
            };

            Enumerable.Range(0, MaxCount).Select(x => Enumerable.Range(0, MaxCount)
                .Select(y => new { X = x, Y = y })).SelectMany(a => a)
                .AsParallel().ForAll(co =>
                {
                    var xx = colShuffle[co.X / 3] * 3 + colInnerShuffle[co.X / 3][co.X % 3];
                    var yy = rowShuffle[co.Y / 3] * 3 + rowInnerShuffle[co.Y / 3][co.Y % 3];

                    this.Table[co.X, co.Y] = this.RandomGame[yy, xx] == 0 ? 0 : numShuffle[this.RandomGame[yy, xx] - 1] + 1;
                    if (this.Table[co.X, co.Y] > 0)
                    {
                        this.Readonly[co.X, co.Y] = true;
                    }
                });
        }

        private int[] GetRandomShuffle(int l)
        {
            int[] numbers = new int[l];
            for (int i = 0; i < l; i++)
            {
                numbers[i] = i;
            }

            for (int i = 0; i < l; i++)
            {
                numbers[i] = Interlocked.Exchange(ref numbers[r.Next(l)], numbers[i]);
            }

            return numbers;
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
