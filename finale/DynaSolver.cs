using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace finale
{
    public struct InstructionNode
    {
        public int Move;
        public int ParentIdx;
    }
    public struct ScoreAndInstruction
    {
        public int Score;
        public int InstructionIdx;
    }

	public class DynaSolver
	{
	    private readonly Problem _problem;
	    private readonly Solution _solution;
        private readonly Vec2[] _placedBalloonsPosition = new Vec2[53];
        private readonly int[] _placedBalloonsAltitude = new int[53];

	    private readonly InstructionNode[] _tree = new InstructionNode[400*300*75*8];
	    private int _freeIdx;

        private int[,] _scoresCache;
        private byte[,] _alreadyCovered;

	    public DynaSolver (Problem problem)
		{
		    _problem = problem;
            _solution = new Solution(problem);
		}

		public Solution Solve ()
		{
		    var sw = Stopwatch.StartNew();
		    int max = 620000;

            //pre compute first move
            short firstR, firstC;
            ApplyWind(_problem.DepartBallons.R, _problem.DepartBallons.C, 1, out firstR, out firstC);

		    for (int b = 0; b < 53; b = (b+1)%53)
            {
                //clean solution for current balloon if any
                for (int t = 0; t < 400; t++)
                    _solution.Moves[t, b] = 0;

                Console.WriteLine(b);
                ComputeSolutionForBalloon(firstR, firstC, b);

                var score = Scorer.Score(_solution);
                if (score > max)
                {
                    Dumper.Dump(_solution, score + ".txt");
                    max = score;
                }

                Console.WriteLine(score);
                Console.WriteLine("(" + sw.Elapsed + ")");
                sw.Restart();
            }

		    return _solution;
		}

	    private void ComputeSolutionForBalloon(short firstR, short firstC, int b)
	    {
	        var previousScores = new ScoreAndInstruction[_problem.NbLines,_problem.NbCols,8];
	        var currentScores = new ScoreAndInstruction[_problem.NbLines,_problem.NbCols,8];
	        _alreadyCovered = InitBalloonLoop();

	        for (int t = 1 /*turn 0 is hardcoded*/; t < _problem.NbTours; t++)
	        {
	            InitTurnLoop(firstR, firstC, previousScores, t);

	            for (int r = 0; r < _problem.NbLines; r++)
	            {
	                for (int c = 0; c < _problem.NbCols; c++)
	                {
	                    for (int a = 0; a < 8; a++)
	                    {
	                        var prevScore = previousScores[r, c, a];
	                        if (prevScore.Score <= 0)
	                            continue;

	                        //try all moves from current position
	                        var lower = a == 0 ? 0 : -1;
	                        var upper = a == 7 ? 0 : 1;
	                        for (int da = lower; da <= upper; da++)
	                        {
	                            short newR, newC;
	                            if (ApplyWind(r, c, a + da + 1, out newR, out newC))
	                            {
	                                int oldScore = currentScores[newR, newC, a + da].Score;
	                                int newScore = prevScore.Score + _scoresCache[newR, newC];
	                                if (newScore > oldScore)
	                                {
	                                    currentScores[newR, newC, a + da].Score = newScore;
	                                    _tree[_freeIdx].Move = da;
	                                    _tree[_freeIdx].ParentIdx = prevScore.InstructionIdx;
	                                    currentScores[newR, newC, a + da].InstructionIdx = _freeIdx;
	                                    _freeIdx++;
	                                }
	                            }
	                        }
	                    }
	                }
	            }
	            //swap matrixes
	            var tmp = previousScores;
	            previousScores = currentScores;
	            currentScores = tmp;
	        }

	        var stepIdx = GetBestScore(previousScores);
	        FillSolutionWith(_tree[stepIdx], b);
	    }

	    private byte[,] InitBalloonLoop()
	    {
            //reset positions
	        for (int i = 0; i < 53; i++)
	        {
	            _placedBalloonsPosition[i] = _problem.DepartBallons;
	            _placedBalloonsAltitude[i] = 0;
	        }
	        MoveBalloons(0);
	        var alreadyCovered = GetCoveredCells();

	        //init t0
	        _tree[0].Move = 1; //the first instruction of all paths : lift off
	        _tree[0].ParentIdx = -1;
	        _freeIdx = 1;

	        return alreadyCovered;
	    }

        private void InitTurnLoop(short firstR, short firstC, ScoreAndInstruction[, ,] previousScores, int t)
        {
            //reset score cache
            _scoresCache = new int[75, 300];

            //a balloon can lift off any turn from the starting cell
            var scoreOnFirstCell = GetScoreAt(firstR, firstC, _alreadyCovered);
            if (previousScores[firstR, firstC, 0].Score < scoreOnFirstCell)
                previousScores[firstR, firstC, 0] = new ScoreAndInstruction
                {
                    Score = scoreOnFirstCell,
                    InstructionIdx = 0,
                };

            MoveBalloons(t);
            _alreadyCovered = GetCoveredCells();

            //precompute scores
            for (short r = 0; r < _problem.NbLines; r++)
                for (short c = 0; c < _problem.NbCols; c++)
                    GetScoreAt(r, c, _alreadyCovered);
        }

	    private int GetScoreAt(short r, short c, byte[,] alreadyCovered)
	    {
	        var hovered = _problem.GetListOfTargetReachedFrom(r, c);
	        int score = 0;
	        for (int i = 0; i < hovered.Count; i++)
	        {
	            var loc = hovered[i];
	            if (alreadyCovered[loc.R, loc.C] == 0)
	                score++;
	        }
	        _scoresCache[r, c] = score;
	        return score;
	    }

	    private void MoveBalloons(int turn)
        {
            for (int b = 0; b < 53; b++)
            {
                var pos = _placedBalloonsPosition[b];
                _placedBalloonsAltitude[b] += _solution.Moves[turn, b];
                short newR, newC;
                ApplyWind(pos.R, pos.C, _placedBalloonsAltitude[b], out newR, out newC);
                _placedBalloonsPosition[b] = new Vec2(newR, newC);
            }
	    }

	    private byte[,] GetCoveredCells()
	    {
	        var covered = new byte[75,300];
	        for (int b = 0; b < 53; b++)
	        {
	            if (_placedBalloonsAltitude[b] == 0)
	                continue; //have not left base yet

	            foreach (var loc in _problem.GetListOfTargetReachedFrom(_placedBalloonsPosition[b]))
	            {
	                covered[loc.R, loc.C] = 1;
	            }
	        }
	        return covered;
	    }

	    private void FillSolutionWith(InstructionNode step, int balloonIdx)
	    {
	        int turn = 399;
	        while (step.ParentIdx != -1)
	        {
                _solution.Moves[turn, balloonIdx] = step.Move;
	            step = _tree[step.ParentIdx];
	            turn--;
	        }
            _solution.Moves[turn, balloonIdx] = step.Move; //last (=first) move
	    }

	    private static int GetBestScore(ScoreAndInstruction[,,] scores)
	    {
	        var max = new ScoreAndInstruction();
	        for (int r = 0; r < 75; r++)
	        {
	            for (int c = 0; c < 300; c++)
	            {
	                for (int a = 0; a < 8; a++)
	                {
	                    if (scores[r, c, a].Score > max.Score)
	                    {
	                        max = scores[r, c, a];
	                    }
	                }
	            }
	        }

	        return max.InstructionIdx;
	    }

	    /// <returns>false if the new location is outside of the map</returns>
        private bool ApplyWind(int r, int c, int a, out short newR, out short newC)
        {
            var wind = _problem.Winds[r, c, a];
            newC = (short) ((300 + c + wind.C)%300);
            newR = (short) (r + wind.R);
            return newR >= 0 && newR < 75;
        }
    }
}
