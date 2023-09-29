using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.

    //public class EvilBot : IChessBot
    //{
    //    // Piece values: null, pawn, knight, bishop, rook, queen, king
    //    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    //    public Move Think(Board board, Timer timer)
    //    {
    //        Move[] allMoves = board.GetLegalMoves();

    //        // Pick a random move to play if nothing better is found
    //        Random rng = new();
    //        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
    //        int highestValueCapture = 0;

    //        foreach (Move move in allMoves)
    //        {
    //            // Always play checkmate in one
    //            if (MoveIsCheckmate(board, move))
    //            {
    //                moveToPlay = move;
    //                break;
    //            }

    //            // Find highest value capture
    //            Piece capturedPiece = board.GetPiece(move.TargetSquare);
    //            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

    //            if (capturedPieceValue > highestValueCapture)
    //            {
    //                moveToPlay = move;
    //                highestValueCapture = capturedPieceValue;
    //            }
    //        }

    //        return moveToPlay;
    //    }

    //    // Test if this move gives checkmate
    //    bool MoveIsCheckmate(Board board, Move move)
    //    {
    //        board.MakeMove(move);
    //        bool isMate = board.IsInCheckmate();
    //        board.UndoMove(move);
    //        return isMate;
    //    }
    ////}
    //public class EvilBot : IChessBot
    //{
    //    Board b;
    //    bool color;
    //    public Move Think(Board board, Timer timer)
    //    {
    //        b = board;
    //        color = b.IsWhiteToMove;
    //        var (moves, rng) = (board.GetLegalMoves(), new Random());
    //        return moves.OrderByDescending(CalcMovePoints).MaxBy(MoveIsCheckmate);
    //    }

    //    int CalcCaptureRisk(Square x, PieceType t)
    //    {
    //        return B2I(b.SquareIsAttackedByOpponent(x)) * (int)t * 5;
    //    }

    //    int B2I(bool b)
    //    {
    //        return b ? 1 : 0;
    //    }

    //    int CalcMovePoints(Move m)
    //    {
    //        return (int)Distance(m.TargetSquare, m.StartSquare) + (int)m.CapturePieceType * 5 + (int)m.PromotionPieceType * 100 + EnemyKingDistanceInverse(m) * 3 - CalcCaptureRisk(m.TargetSquare, m.MovePieceType) + CalcCaptureRisk(m.StartSquare, m.MovePieceType);
    //    }

    //    int EnemyKingDistanceInverse(Move m)
    //    {
    //        return 8 - (int)Distance(b.GetKingSquare(!color), m.TargetSquare);
    //    }

    //    int MoveIsCheckmate(Move move)
    //    {
    //        b.MakeMove(move);
    //        bool isMate = b.IsInCheckmate();
    //        b.UndoMove(move);
    //        return B2I(isMate);
    //    }

    //    Vector2 SquareVec(Square x)
    //    {
    //        return new Vector2(x.Rank, x.File);
    //    }
    //    float Distance(Square x, Square y)
    //    {
    //        return (SquareVec(x) - SquareVec(y)).Length();
    //    }
    //}

    public class EvilBot : IChessBot
    {
        //Consts.
        private const int EVALUATION_RECURSIVE_DEPTH = 3;//This is how many moves ahead the bot will think about.
                                                         //The consts after this line are values of a move based on the state of the board after that move 
        private const int NO_ENEMY_CAPTURE_VALUE = -1000000; //When a move doesn't capture anything it is given this weight.
        private const int ENEMY_CAPTURED_VALUE = 1000;
        private const int CHECKMATE_VALUE = 1000000000;
        private const int CHECK_VALUE = 100000;
        private const int DRAW_VALUE = -100000;
        //The consts after this line are values of a move based on if the move is a special move type.
        private const int CASTLES_VALUE = 1000;
        private const int ENPASSANT_VALUE = 1000;
        private const int CAPTURE_VALUE = 500;
        //The consts after this line are the weights added to each move when it doesn't
        //lead to a capture depending on the piece being moved.
        private const int KING_MOVE_SCORE_WEIGHT = -1000;
        private const int QUEEN_MOVE_SCORE_WEIGHT = 100;
        private const int ROOK_MOVE_SCORE_WEIGHT = 200;
        private const int BISHOP_MOVE_SCORE_WEIGHT = 200;
        private const int KNIGHT_MOVE_SCORE_WEIGHT = 100;
        private const int PAWN_MOVE_SCORE_WEIGHT = 1000;

        //Variables.
        private Board m_board;
        private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };// Piece values: null, pawn, knight, bishop, rook, queen, king
        private int boardValueLastFrame = 0;
        private int BLACK_MULTIPLIER;
        private int WHITE_MULTIPLIER;

        //Debug variables.
        private int highestValueLastTime;

        public Move Think(Board board, Timer timer)
        {
            //Cache the state of the board.
            m_board = board;

            //Get all the legal moves.
            Move[] moves = m_board.GetLegalMoves();

            //Evalulate each move and choose best one.
            Random rng = new();
            Move bestMove = moves[rng.Next(moves.Length)];
            int highestValue = Evaluate(bestMove, EVALUATION_RECURSIVE_DEPTH);
            foreach (Move move in moves)
            {
                //Evaluate the move.
                int value = Evaluate(move, EVALUATION_RECURSIVE_DEPTH);
                if (value > highestValue)
                {
                    bestMove = move;
                    highestValue = value;
                }
            }

            //DEBUG
            if (highestValueLastTime != highestValue)
            {
                highestValueLastTime = highestValue;
         //       ChessChallenge.Application.ConsoleHelper.Log("HighestValue: " + highestValue.ToString());
            }

            //Return the move to make.
            return bestMove;
        }

        public int Evaluate(Move a_move, int a_deepness)
        {
            PieceType movePieceType = a_move.MovePieceType;
            int currentDepth = a_deepness - 1;
            //Initalise return value.
            int moveEvaluationScore = 0;

            //Get the current turn's colour.
            bool currentTurnIsWhite = m_board.IsWhiteToMove;
            if (currentTurnIsWhite)
            {
                //Count white pieces vs black pieces.
                //White pieces give positive value and black pieces give negative value.
                BLACK_MULTIPLIER = -1;
                WHITE_MULTIPLIER = 1;
            }
            else
            {
                //Count white pieces vs black pieces.
                //White pieces give negative value and black pieces give positive value.
                BLACK_MULTIPLIER = 1;
                WHITE_MULTIPLIER = -1;
            }

            //Get the original board value.
            int boardValueBeforeMove = GetValueOfBoard(m_board);

            //Make move then get the score of the state of the board afterwards.
            m_board.MakeMove(a_move);

            //Check the board for different main game states.
            if (m_board.IsInCheckmate())
            {
                //Always do checkmate.
                m_board.UndoMove(a_move);
                return CHECKMATE_VALUE;
            }

            if (m_board.IsInCheck())
            {
                moveEvaluationScore += CHECK_VALUE;
            }

            if (m_board.IsDraw())
            { //Stalemate, draw, repetition, insuffcient material etc...
                m_board.UndoMove(a_move);
                moveEvaluationScore += DRAW_VALUE;
                return moveEvaluationScore;
            }

            if (a_move.IsCastles)
            {
                moveEvaluationScore += CASTLES_VALUE;
            }

            if (a_move.IsEnPassant)
            {
                moveEvaluationScore += ENPASSANT_VALUE;
            }

            if (a_move.IsCapture)
            {
                moveEvaluationScore += CAPTURE_VALUE;
            }

            //Get the value of the whole board if the move is made.
            int valueOfBoardIfMoveIsMade = GetValueOfBoard(m_board);
            int netMoveScore = (valueOfBoardIfMoveIsMade - boardValueBeforeMove);
            if (netMoveScore <= 0)
            {
                //If move does not capture a piece.
                moveEvaluationScore += NO_ENEMY_CAPTURE_VALUE; //Discourage bot from not capturing.

                //Then give it more incentive to move certain pieces in that case.
                switch (movePieceType)
                {
                    case PieceType.King:
                        {
                            moveEvaluationScore += KING_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    case PieceType.Queen:
                        {
                            moveEvaluationScore += QUEEN_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    case PieceType.Rook:
                        {
                            moveEvaluationScore += ROOK_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    case PieceType.Bishop:
                        {
                            moveEvaluationScore += BISHOP_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    case PieceType.Knight:
                        {
                            moveEvaluationScore += KNIGHT_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    case PieceType.Pawn:
                        {
                            moveEvaluationScore += PAWN_MOVE_SCORE_WEIGHT;
                            break;
                        }
                    default:
                        {
                            break;
                        }

                }
            }
            else
            {
                moveEvaluationScore += ENEMY_CAPTURED_VALUE;
            }

            if (currentDepth > 0)
            {
                //Get list of next posisble moves.
                Move[] nextMoves = m_board.GetLegalMoves();

                //Evaluate each of those moves with 1 less depth than the previous call of evaluate.
                //When it reaches 0 then the recursive loop will exit with the best approximate move.
                int worstScoreForPreviousPlayer = int.MaxValue;
                foreach (Move move in nextMoves)
                {
                    int moveScore = -Evaluate(move, currentDepth); //Inverts the evaluation score as what's best for the next player won't be best for the current player.
                    if (moveScore < worstScoreForPreviousPlayer)
                    {
                        worstScoreForPreviousPlayer = moveScore;
                    }
                }

                //add the score to the moves score.
                moveEvaluationScore += worstScoreForPreviousPlayer;
            }


            //Return board to original state.
            m_board.UndoMove(a_move);

            //Return the value.
            return moveEvaluationScore;
        }

        private int GetValueOfBoard(Board a_board)
        {
            PieceList[] allPieces = m_board.GetAllPieceLists();
            int boardValue = 0;
            foreach (PieceList pieces in allPieces)
            {
                bool isWhitePieceList = pieces.IsWhitePieceList;
                int piecesCountTimesPieceValue = pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList];
                if (isWhitePieceList)
                {
                    boardValue += (WHITE_MULTIPLIER * piecesCountTimesPieceValue);
                }
                else
                {
                    boardValue += (BLACK_MULTIPLIER * piecesCountTimesPieceValue);
                }
            }
            return boardValue;
        }
    }
}