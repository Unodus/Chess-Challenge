using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    public static class Def // AKA - the magic-number zone
    {
        public static int[] pieceValues = { 0, 100, 900, 900, 2500, 8100, 10000 };
        public static int moveBudget = 750;

    }
    public struct MOVE // a Move with a score
    {
        int score;
        public Move move;
        public int interest;
        public int Budget;
        public MOVE(Move i, int j, int k)
        {
            move = i;
            score = j;
            interest = k;
            Budget = 0;
        }

        public void setScore(int i) // Not sure why I bothered with getters/setters... Will prob remove
        {
            score = i;
        }
        public void addScore(int i)
        {
            score += i;
        }

        public int getScore()
        {
            return score;
        }

        public void setBudget(int totalInterest, int moveBudget)
        {
            float ratio = 0;
            if (totalInterest >0)
            {
                ratio = interest / totalInterest;

            }

            Budget = moveBudget * (int)ratio;
            
        }


    }



    public class MinMaxFish 
    {
        public int totalInterest = 0;
        
        public MinMaxFish()
        {

        }


        MOVE[] getLegalMoves(Board board, int startingScore, int moveBudget)
        {
            var m = board.GetLegalMoves();
            
            var rng = new Random();

            m = m.OrderBy(e => rng.NextDouble()).ToArray();

            List<MOVE> moves = new List<MOVE>();
            foreach (Move i in m)
            {
                int localInterest = 0;
                int localScore = startingScore;



                if (i.TargetSquare.File != 0 && i.TargetSquare.File != 7)
                {
                    localScore += 10;
                    localInterest++;
                }
                if (i.MovePieceType != PieceType.King)
                {
                    localScore += 5;
                    localInterest++;
                }
                if(i.IsCastles)
                {
                    localScore += 10;
                    localInterest++;
                }

                if (i.IsPromotion)
                {
                    localScore += Def.pieceValues[(int)i.PromotionPieceType];
                    localInterest++;
                }

                if (i.IsCapture)
                {
                    localScore += Def.pieceValues[(int)i.CapturePieceType];
                    localInterest++;
                }

                if (board.SquareIsAttackedByOpponent(i.TargetSquare))
                {
                    localScore -= Def.pieceValues[(int)i.MovePieceType];
                    localInterest++;
                }
                totalInterest += localInterest;
                moves.Add(new MOVE(i, localScore, localInterest));
            }

            foreach (MOVE i in moves)
            {
                i.setBudget(totalInterest, moveBudget);
            }

            return moves.ToArray();
        }


        public MOVE Think(Board board, int moveBudget, int score)
        {


            Random rng = new Random();

            MOVE[] moves = getLegalMoves(board, score, moveBudget);
            MOVE bestMove = moves[0];

            foreach (MOVE move in moves)
            {
                board.MakeMove(move.move);
                if (board.IsInCheckmate())
                {
                    move.setScore(int.MaxValue);
                }
                else if (board.IsInStalemate() || board.IsInsufficientMaterial())
                {
                    move.setScore(0);
                }
                else if (move.Budget >= 1)
                {
                    MinMaxFish i = new MinMaxFish();

                    MOVE a = i.Think(board, move.Budget - 1, (move.getScore()) * -1);

                    move.setScore(  (a.getScore() * -1)) ;
                }

                if (move.getScore() > bestMove.getScore())
                {
                    bestMove = move;
                }
                board.UndoMove(move.move);

                 

            }

           // ConsoleHelper.Log(bestMove.move.MovePieceType.ToString() + " to " +bestMove.move.TargetSquare  + " " +bestMove.getScore() +" "+ depth, true, ConsoleColor.Red);


            return bestMove;

        }


    }

    
    public Move Think(Board board, Timer timer)
    {
    
        MinMaxFish fisher = new MinMaxFish();
        PieceList[] pieces = board.GetAllPieceLists();

        Move[] moves = board.GetLegalMoves();
        
        int whiteScore = 0;
        int blackScore = 0;

        for (int i = 0; i <= 5; i++)
        {
            foreach (Piece piece in pieces[i])
            {
                whiteScore += Def.pieceValues[i + 1];
            }
            foreach (Piece piece in pieces[i + 6])
            {
                blackScore += Def.pieceValues[i + 1];
            }
        }
       

        if (board.IsWhiteToMove) return fisher.Think(board, Def.moveBudget, whiteScore - blackScore).move;
        else return fisher.Think(board, Def.moveBudget, blackScore - whiteScore).move;



    }

}