using ChessChallenge.API;

public class MyBot : IChessBot
{
    {
    // factor parameters
    float materialFactor = 100f;
    float positionFactor = 1f;
    float mobilityFactor = 1f;

    // create a dictionary to map piece to piece value as well as grid
    private Dictionary<PieceType, (float, float[])> pieceValueMap;

    public EvilBot()
    {
        init();
    }

    private void init()
    {

        // Initialize the dictionary and assign float values to each piece
        pieceValueMap = new Dictionary<PieceType, (float, float[])>
    {
        { PieceType.Pawn, (1.0f, ToGrid(new ulong []  { 4340625615855026175, 261993005055, 16777215, 255 })) },
        { PieceType.Knight, (3.0f, ToGrid(new ulong []  { 4340625615855026175, 261993005055, 16777215, 255 }))},
        { PieceType.Bishop, (3.0f, ToGrid(new ulong []  { 4340625615855026175, 261993005055, 16777215, 255 }))},
        { PieceType.Rook, (5.0f, ToGrid(new ulong []  { 4340625615855026175, 261993005055, 16777215, 255 }))},
        { PieceType.Queen, (8.0f, ToGrid(new ulong []  { 4340625615855026175, 261993005055, 16777215, 255 }))},
        { PieceType.King, (100.0f, ToGrid(new ulong []  {  18446744073709551615, 18374686479671623680, 18374686479671623680, 16645304222761353216  }))},
        // Add other pieces and their corresponding float values here
    };
    }
    public Move Think(Board board, Timer timer)
    {
        return findBestMove(board, 3, board.IsWhiteToMove, board.IsWhiteToMove, timer);
    }

    private Move findBestMove(Board board, int depth, bool maximizingPlayer, bool WhiteToPlay, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        float bestValue = maximizingPlayer ? float.MinValue : float.MaxValue;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float minmaxResult = pvs(board, depth, float.MinValue, float.MaxValue, !maximizingPlayer, !WhiteToPlay);
            board.UndoMove(move);

            if ((maximizingPlayer && minmaxResult > bestValue) || (!maximizingPlayer && minmaxResult < bestValue))
            {
                bestValue = minmaxResult;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private float calcPositionScore(Board board)
    {
        if (board.IsDraw() || board.IsRepeatedPosition() || board.IsInsufficientMaterial())
        {
            return 0;
        }
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -100000 : 100000;
        }
        float materialScore = calculateBoardPieceValue(board); // for token efficiency
        float positionScore = calculatePiecePositionValue(board);
        float mobilityScore = calculateMobilityValue(board);
        return (materialScore * materialFactor) + (positionScore * positionFactor) + (mobilityScore * mobilityFactor);
    }

    private float calculateBoardPieceValue(Board board)
    {
        float value = 0;

        foreach (PieceList list in board.GetAllPieceLists())
        {
            int colorMult = list.IsWhitePieceList ? 1 : -1;
            value += pieceValueMap[list.TypeOfPieceInList].Item1 * list.Count * colorMult;
        }

        return value;
    }

    private float calculatePiecePositionValue(Board board)
    {
        float value = 0;
        foreach (PieceType pieceType in pieceValueMap.Keys)
        {
            float[] pieceMap = pieceValueMap[pieceType].Item2;
            float[] whiteBitBoardGrid = uLongToGrid(board.GetPieceBitboard(pieceType, true));
            float[] blackBitBoardGrid = uLongToGrid(board.GetPieceBitboard(pieceType, false));
            for (int i = 0; i < 64; i++)
            {
                value += whiteBitBoardGrid[63 - i] * pieceMap[i] - blackBitBoardGrid[i] * pieceMap[i];
            }
        }
        return value;
    }

    private float calculateMobilityValue(Board board)
    {
        return 0;
    }

    private bool SquareEquals(Square square, String name)
    {
        return square.Name.ToString().Equals(name);
    }

    private float[] uLongToGrid(ulong singleUlong)
    {
        return uLongsToGrid(new ulong[] { singleUlong })[0];
    }

    private float[][] uLongsToGrid(ulong[] ulongs)
    {
        float[][] floatGrid = new float[4][];
        for (int ulongIndex = 0; ulongIndex < ulongs.Length; ulongIndex++)
        {
            floatGrid[ulongIndex] = new float[64];
            for (int i = 0; i < 64; i++)
            {
                floatGrid[ulongIndex][63 - i] = (ulongs[ulongIndex] & (1UL << i)) != 0 ? 1.0f : 0.0f;
            }
        }
        return floatGrid;
    }

    private ulong[] gridToUlong(float[] grid) // helper method depricated
    {
        ulong[] ulongValues = new ulong[4];

        for (int ulongindex = 0; ulongindex < 4; ulongindex++)
        {
            for (int bitIndex = 0; bitIndex < grid.Length; bitIndex++)
            {
                if (grid[bitIndex] >= 1.0f * (ulongindex + 1))
                {
                    int leftBitIndex = 63 - bitIndex;
                    ulongValues[ulongindex] |= 1UL << leftBitIndex;
                }
            }
        }
        return ulongValues;
    }

    private float[] AddFloatArrays(float[][] arrays)
    {
        float[] result = new float[64];

        for (int i = 0; i < 64; i++)
        {
            result[i] = arrays[0][i] + arrays[1][i] + arrays[2][i] + arrays[3][i];
        }

        return result;
    }

    private float[] ToGrid(ulong[] ulongs)
    {
        return AddFloatArrays(uLongsToGrid(ulongs));
    }


    // principal variation search
    private float pvs(Board board, int depth, float alpha, float beta, bool maximizingPlayer, bool WhiteToPlay)
    {
        if (depth <= 0 || board.IsInCheckmate() || board.IsDraw())
        {
            float result = calcPositionScore(board);
            return result;
        }

        Move[] moves = board.GetLegalMoves();
        if (maximizingPlayer)
        {
            float value = float.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                // Principal Variation Search (PVS)
                float minmaxResult;
                if (move == moves[0]) // First move in the list, do a regular minimax search
                    minmaxResult = pvs(board, depth - 1, alpha, beta, false, !WhiteToPlay);
                else
                {
                    // Null window search with a reduced depth
                    minmaxResult = pvs(board, depth - 1, alpha, alpha + 1, false, !WhiteToPlay);
                    if (alpha < minmaxResult && minmaxResult < beta)
                        minmaxResult = pvs(board, depth - 1, minmaxResult, beta, false, !WhiteToPlay);
                }

                value = Math.Max(value, minmaxResult);
                alpha = Math.Max(alpha, value);
                board.UndoMove(move);

                if (beta <= alpha)
                    break; // Beta cut-off
            }
            return value;
        }
        else
        {
            float value = float.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                // Principal Variation Search (PVS)
                float minmaxResult;
                if (move == moves[0]) // First move in the list, do a regular minimax search
                    minmaxResult = pvs(board, depth - 1, alpha, beta, true, !WhiteToPlay);
                else
                {
                    // Null window search with a reduced depth
                    minmaxResult = pvs(board, depth - 1, beta - 1, beta, true, !WhiteToPlay);
                    if (alpha < minmaxResult && minmaxResult < beta)
                        minmaxResult = pvs(board, depth - 1, alpha, minmaxResult, true, !WhiteToPlay);
                }

                value = Math.Min(value, minmaxResult);
                beta = Math.Min(beta, value);
                board.UndoMove(move);

                if (beta <= alpha)
                    break; // Alpha cut-off
            }
            return value;
        }
    }
}
