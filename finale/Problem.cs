using System;
using System.Collections.Generic;
using System.Linq;

namespace finale
{
	public class Problem
	{
		public int NbLines { get; private set;}
		public int NbCols { get; private set;}
		public int NbAltitudes { get; private set;}

		public int RayonCouverture { get; private set;}
		public int NbTours { get; private set;}

		public Localisation DepartBallons { get; private set;}

		public Caze GetCaze(Localisation loc){
			return GetCaze (loc.Line, loc.Col);
		}

		public Caze GetCaze (int r, int c){
            return _cazes[r][c];
		}

		public int NbAvailableBallons { get; private set; }

		private List<List<Caze>> _cazes;


		public Problem (int nbLines, int nbCols, int nbAltitudes, int rayonCouverture, int nbTours, Localisation departBallons, List<List<Caze>> cazes, int nbAvailableBallons)
		{
			NbLines = nbLines;
			NbCols = nbCols;
			NbAltitudes = nbAltitudes;

			RayonCouverture = rayonCouverture;
			NbTours = nbTours;

			DepartBallons = departBallons;
			_cazes = cazes;

			NbAvailableBallons = nbAvailableBallons;
		}

		public List<Caze> GetCazesReachedFrom(int r, int c)
		{
			var reached = new List<Caze> ();
			for (int i = -6; i < 7; i++)
			{
				for (int j = -6; j < 7; j++)
				{
					if (i * i + j * j > 7 * 7)
						continue; //not in range

					int cazeC = (c + i)%300;
					if (cazeC < 0)
						cazeC += 300;

					int cazeR = r + j;

					if (cazeR < 0 || cazeR > 75) //boundaries check
						continue;

					reached.Add (GetCaze (cazeR, cazeC));
				}
			}
			return reached;
		}

		public int GetNbTargetsReachedFrom(int r, int c)
		{
			return GetCazesReachedFrom (r, c).Count(caze => caze.IsTarget);
		}


        // Give all 3 possible postions for a baloon at next turn in that order :
        // if altitude -1, if stable, if altitude +1
        // !!! still buggy !!!
        public static List<Localisation> FindNextPossiblePostions(Problem problem, Localisation currentLoc, int currentAltitude)
        {
            // check winds at current
            var possiblePositions = new List<Localisation>();


            int nextCaseAltmin1Line = currentLoc.Line + problem.GetCaze(currentLoc).Winds[currentAltitude - 1].DeltaRow; // add out if needed
            int nextCaseAltmin1Col = (currentLoc.Col + problem.GetCaze(currentLoc).Winds[currentAltitude - 1].DeltaCol) % problem.NbCols;
            possiblePositions.Add(new Localisation(nextCaseAltmin1Line, nextCaseAltmin1Col));

            int nextCaseAlt0Line = currentLoc.Line + problem.GetCaze(currentLoc).Winds[currentAltitude].DeltaRow; // add out if needed
            int nextCaseAlt0Col = (currentLoc.Col + problem.GetCaze(currentLoc).Winds[currentAltitude].DeltaCol) % problem.NbCols;
            possiblePositions.Add(new Localisation(nextCaseAlt0Line, nextCaseAlt0Col));

            int nextCaseAltmax1Line = currentLoc.Line + problem.GetCaze(currentLoc).Winds[currentAltitude + 1].DeltaRow; // add out if needed
            int nextCaseAltmax1Col = (currentLoc.Col + problem.GetCaze(currentLoc).Winds[currentAltitude + 1].DeltaCol) % problem.NbCols;
            possiblePositions.Add(new Localisation(nextCaseAltmax1Line, nextCaseAltmax1Col));

            return possiblePositions;
        }

	}
}
