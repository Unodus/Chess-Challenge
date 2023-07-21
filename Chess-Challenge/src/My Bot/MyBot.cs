using ChessChallenge.API;

public class MyBot : IChessBot
{

    int i = 0;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        i++;

        return moves[0];
    }
}