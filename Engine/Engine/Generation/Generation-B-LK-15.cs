using System.Collections.Generic;

namespace Engine
{
    public class Generation
    {
        public enum Promotions { All, QueenOnly, QueenAndKnight }

        public Promotions promotionsToGenerate = Promotions.All;
        public static readonly ushort[][] PromotionFlags = { new ushort[] { 4, 5, 6, 7 }, new ushort[] { 7 }, new ushort[] { 7, 4 } };

        static private readonly bool[] directionConsts = { true, false, false, true, true, false, false, true }; // the directions which are moving away or towards the king square (true for moving away)

        int MoveColour;
        int EnemyColour;

        byte KingSquare;
        ulong KingSquareBitboard;

        public bool isCheck;
        public bool isDoubleCheck;

        bool genQuietMoves;


        // If in check, this bitboard contains squares in line from checking piece up to king
        // If not in check, all bits are set to 1
        ulong checkRayMask;

        ulong pinRayMask;
        ulong pinH;
        ulong pinV;
        ulong pinHV;
        ulong pinD;
        ulong[] pinDirectionMasks = new ulong[9]; // {N, E, S, W, NE, SE, SW, NW, Full bitboard}
        internal ulong opponentAttacks;
        internal ulong opponentAttacksWithoutPawns;
        ulong AllPieces;
        ulong notFriendlyPieces;

        internal Bitboard board;

        // If only captures should be generated, this will have 1s only in positions of enemy pieces.
        // Otherwise it will have 1s everywhere.
        ulong moveTypeMask;

        public List<Move> GenerateMoves(Bitboard board, bool capturesOnly = false)
        {
            List<Move> moves = new List<Move>();


            this.board = board;

            InitiliseVars(capturesOnly);

            CalculateCheckAttacks();

            genKingMoves(ref moves);

            if (!isDoubleCheck) // in double check only the king can move
            {
                genPawnMoves(ref moves);
                genKnightMoves(ref moves);
                genSlidingMoves(ref moves);
            }

            if (moves.Count == 0) // if there are no moves then the it is either checkmate or stale mate
            {
                if (isCheck)
                {
                    board.state = GameState.Checkmate;
                }
                else board.state = GameState.StaleMate;
            }

            return moves;
        }

        void addLegalMoves(ref List<Move> moves, ulong legalMoves, ulong start, bool insertAtFront = false)
        {
            while (legalMoves != 0)
            {
                ulong destination = BitBoardTools.popLSB(ref legalMoves);

                if (insertAtFront)
                {
                    moves.Insert(0, new Move(start, destination));
                }
                else
                {
                    moves.Add(new Move(start, destination));
                }
            }
        }

        void genPromotions(ref List<Move> moves, ulong start, ulong destination)
        {
            foreach (ushort i in PromotionFlags[(int)promotionsToGenerate])
            {
                moves.Insert(0, new Move(start, destination, i));
            }
        }

        internal void genKingMoves(ref List<Move> moves)
        {
            ulong PseudoLegal = BitBoardTools.KingAttacks[KingSquare];
            ulong kingMoves = PseudoLegal & (~(opponentAttacks | board.Board[MoveColour])) & moveTypeMask;
            addLegalMoves(ref moves, kingMoves, KingSquareBitboard, false);

            // Castling moves

            if (isCheck == false && moveTypeMask == ulong.MaxValue)
            {
                ulong clearedQueenSideAttack = (Tools.clearBit(6, opponentAttacks) & Tools.clearBit(62, opponentAttacks));
                ulong CheckBlockers = clearedQueenSideAttack | AllPieces; // the king can not move through or into check or if there is a piece blocking the path

                if (Bitboard.canCastleKingside(board.castlingRights, MoveColour) && (BitBoardTools.CastlingMasks[0, MoveColour] & CheckBlockers) == 0)
                {
                    byte destination = MoveColour == 0 ? (byte)1 : (byte)57;
                    moves.Add(new Move(KingSquare, destination, Move.castling));
                }

                if (Bitboard.canCastleQueenside(board.castlingRights, MoveColour) && (BitBoardTools.CastlingMasks[1, MoveColour] & CheckBlockers) == 0)
                {
                    byte destination = MoveColour == 0 ? (byte)5 : (byte)61;
                    moves.Add(new Move(KingSquare, destination, Move.castling));
                }
            }
        }

        internal void genKnightMoves(ref List<Move> moves)
        {
            ulong knights = Bitboard.getKnights(board.Board, MoveColour);

            knights &= ~pinRayMask; // pinned knights can't move

            while (knights != 0)
            { // for each knight
                ulong knightLoc = BitBoardTools.popLSB(ref knights);
                ulong knightAttacks = BitBoardTools.KnightAttacks[BitBoardTools.BitboardToIndex(knightLoc)];
                knightAttacks &= checkRayMask & moveTypeMask & notFriendlyPieces; // legalise the moves

                addLegalMoves(ref moves, knightAttacks, knightLoc, false);
            }
        }

        internal void genSlidingMoves(ref List<Move> moves)
        {
            // Orthogonal pieces

            ulong OrthogonalSliders = Bitboard.getRooks(board.Board, MoveColour) | Bitboard.getQueens(board.Board, MoveColour);
            OrthogonalSliders &= ~pinD; // diagonally pinned rooks can't move
            ulong DiagonalSliders = Bitboard.getBishops(board.Board, MoveColour) | Bitboard.getQueens(board.Board, MoveColour);
            DiagonalSliders &= ~pinHV; // orthogonally pinned bishops can't move

            if (isCheck)
            { // pinned pieces can not move in check 
                OrthogonalSliders &= ~pinRayMask;
                DiagonalSliders &= ~pinRayMask;
            }

            while (OrthogonalSliders != 0) // generate moves for each rooks
            {
                ulong OrthognalPiece = BitBoardTools.popLSB(ref OrthogonalSliders);
                byte PieceSquareIndex = BitBoardTools.BitboardToIndex(OrthognalPiece);
                ulong legalMoves = Magic.getRookBlockerAttacks(OrthognalPiece, AllPieces); // gets all the pseudo-legal moves
                legalMoves &= checkRayMask & moveTypeMask & notFriendlyPieces;

                if (IsPinned(PieceSquareIndex))
                {
                    legalMoves &= pinHV;
                }

                addLegalMoves(ref moves, legalMoves, OrthognalPiece, false);
            }

            while (DiagonalSliders != 0) // generate moves for each bishops
            {
                ulong DiagonalPiece = BitBoardTools.popLSB(ref DiagonalSliders);
                byte PieceSquareIndex = BitBoardTools.BitboardToIndex(DiagonalPiece);
                ulong legalMoves = Magic.getBishopBlockerAttacks(DiagonalPiece, AllPieces);
                legalMoves &= checkRayMask & moveTypeMask & notFriendlyPieces;

                if (IsPinned(PieceSquareIndex))
                {
                    legalMoves &= pinD;
                }

                addLegalMoves(ref moves, legalMoves, DiagonalPiece, false);
            }
        }

        internal void genPawnMoves(ref List<Move> moves)
        {
            ulong Pawns = Bitboard.getPawns(board.Board, MoveColour);

            while (Pawns != 0) // loops through each pawn
            {
                int pinDirectionMaskIndex = 8;

                ulong pawnLoc = BitBoardTools.popLSB(ref Pawns);
                if (IsPinned(BitBoardTools.BitboardToIndex(pawnLoc)))
                {
                    pinDirectionMaskIndex = pinType(pawnLoc); // gets the type of pin
                }

                if (genQuietMoves)
                { // generate pushes

                    // Push the pawn once
                    ulong pawnPush;

                    pawnPush = BitBoardTools.pushPawn(pawnLoc, MoveColour);

                    pawnPush &= ~AllPieces;

                    if ((BitBoardTools.usefulPawnRanks[MoveColour] & pawnPush) == 0) // if not a promotion
                    {

                        if ((BitBoardTools.usefulPawnRanks[MoveColour + 2] & pawnLoc) > 0 && pawnPush > 0)
                        { // if it is on the starting rank then gen double pushes
                            ulong pawnDoublePush;
                            pawnDoublePush = BitBoardTools.pushPawn(BitBoardTools.pushPawn(pawnLoc, MoveColour), MoveColour);

                            pawnDoublePush &= ~AllPieces;
                            pawnDoublePush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                            if (pawnDoublePush != 0)
                            {
                                moves.Add(new Move(pawnLoc, pawnDoublePush, Move.enPassantSet));
                            }
                        }
                        pawnPush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                        addLegalMoves(ref moves, pawnPush, pawnLoc, false);
                    }
                    else
                    {
                        pawnPush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                        if (pawnPush != 0)
                        {
                            genPromotions(ref moves, pawnLoc, pawnPush);
                        }
                    }
                }


                // gen captures / enpassant
                ulong pawnCaptures = BitBoardTools.getPawnAttacks(pawnLoc, MoveColour);
                pawnCaptures &= notFriendlyPieces & pinDirectionMasks[pinDirectionMaskIndex];

                if (pawnCaptures != 0)
                {
                    if ((pawnLoc & BitBoardTools.usefulPawnRanks[MoveColour + 4]) > 0 && board.enPassantFile != -1)
                    { // if on the correct enPassantrank && the enPassant flag is enabled 
                        ulong potenialEnPassant = pawnCaptures & (BitBoardTools.AFile >> (board.enPassantFile - 1));
                        ulong enPassantSquare = BitBoardTools.pushPawn(potenialEnPassant, EnemyColour);
                        if (potenialEnPassant > 0 && (enPassantSquare & checkRayMask) > 0 && notCheckAfterEnPassant(potenialEnPassant, pawnLoc, enPassantSquare))
                        {
                            moves.Insert(0, new Move(pawnLoc, potenialEnPassant, Move.enPassantCapture));
                        }
                    }

                    pawnCaptures &= board.Board[EnemyColour] & checkRayMask;

                    if (pawnCaptures != 0)
                    {
                        if ((BitBoardTools.usefulPawnRanks[MoveColour] & pawnCaptures) == 0)
                        {
                            addLegalMoves(ref moves, pawnCaptures, pawnLoc, true);
                        }

                        else
                        {
                            while (pawnCaptures != 0)
                            {
                                ulong pawnCapturePromotion = BitBoardTools.popLSB(ref pawnCaptures);
                                genPromotions(ref moves, pawnLoc, pawnCapturePromotion);
                            }
                        }
                    }
                }

            }
        }

        bool notCheckAfterEnPassant(ulong destination, ulong start, ulong enPassantSquare)
        {
            ulong afterEnPassantAllPieces = AllPieces;
            afterEnPassantAllPieces ^= destination | start | enPassantSquare;
            ulong attacks = Magic.getRookBlockerAttacks(KingSquareBitboard, afterEnPassantAllPieces);
            if ((attacks & Bitboard.getRooks(board.Board, EnemyColour)) > 0)
            {
                return false;
            }
            return true;
        }

        public void InitiliseVars(bool capturesonly)
        {
            isCheck = false;
            isDoubleCheck = false;

            checkRayMask = 0;
            pinRayMask = 0;

            MoveColour = board.WhiteToPlay ? 0 : 1;
            EnemyColour = 1 - MoveColour;

            KingSquareBitboard = board.Board[(int)Tools.BB.King] & board.Board[MoveColour];
            KingSquare = BitBoardTools.BitboardToIndex(KingSquareBitboard);
            AllPieces = board.Board[0] | board.Board[1];
            notFriendlyPieces = ~(board.Board[MoveColour]);

            opponentAttacks = 0;
            opponentAttacksWithoutPawns = 0;

            genQuietMoves = !capturesonly;

            pinDirectionMasks[0] = 0;
            pinDirectionMasks[1] = 0;
            pinDirectionMasks[2] = 0;
            pinDirectionMasks[3] = 0;
            pinDirectionMasks[4] = 0;
            pinDirectionMasks[5] = 0;
            pinDirectionMasks[6] = 0;
            pinDirectionMasks[7] = 0;
            pinDirectionMasks[8] = ulong.MaxValue;

            pinH = 0;
            pinD = 0;
            pinV = 0;
            pinHV = 0;

            moveTypeMask = capturesonly ? board.Board[EnemyColour] : ulong.MaxValue;
        }

        public void CalculateCheckAttacks()
        {
            CalculatePinMasksAndCheck();

            // Knight checks
            ulong enemyKnights = Bitboard.getKnights(board.Board, EnemyColour);
            ulong knightCheckers = BitBoardTools.KnightAttacks[KingSquare] & enemyKnights;
            if (knightCheckers > 0)
            {
                isDoubleCheck = isCheck;
                isCheck = true;
                checkRayMask |= knightCheckers;
            }
            while (enemyKnights != 0)
            {
                ulong knight = BitBoardTools.popLSB(ref enemyKnights);
                opponentAttacksWithoutPawns |= BitBoardTools.KnightAttacks[BitBoardTools.BitboardToIndex(knight)];
            }

            // pawn checks
            ulong enemyPawns = Bitboard.getPawns(board.Board, EnemyColour);
            ulong enemyPawnAttacks = BitBoardTools.pawnAttacks(enemyPawns, EnemyColour);
            ulong pawnCheckers = KingSquareBitboard & enemyPawnAttacks;
            if (pawnCheckers > 0)
            {
                isDoubleCheck = isCheck;
                isCheck = true;
                ulong pawnAttackers = board.WhiteToPlay ? BitBoardTools.wPawnAttacks[KingSquare] : BitBoardTools.bPawnAttacks[KingSquare];
                ulong pawnCheckMap = Bitboard.getPawns(board.Board, EnemyColour) & pawnAttackers;
                checkRayMask |= pawnCheckMap;
            }

            CalculateOpponentSlidingAttacks(); // all of the squares the opponents bishops and rook are attacking

            int enemyKingSquare = BitBoardTools.BitboardToIndex(Bitboard.getKings(board.Board, EnemyColour));
            opponentAttacksWithoutPawns |= BitBoardTools.KingAttacks[enemyKingSquare];

            opponentAttacks = opponentAttacksWithoutPawns | enemyPawnAttacks;

            pinHV = pinH | pinV;

            if (!isCheck)
            {
                checkRayMask = ulong.MaxValue; // the pieces can move anywhere as they dont need to block check
            }
        }

        public void CalculatePinMasksAndCheck()
        {
            for (int i = 0; i < 8; i++) // loop through each direction
            {
                ulong directionMagic = Magic.directions[KingSquare, i]; // gets the bitboard of the direction ray from the king square
                ulong piecesToCheck = directionMagic & (AllPieces ^ KingSquareBitboard); // gets the bitboard of the pieces on this ray
                ulong maybePin = 0; // the location of a piece that might cause a pin
                int friendlyPieces = 0; // counter of friendly pieces found along the 

                if (directionConsts[i]) // we know the LSB is moving away from the king square so the first piece will be a blocking or checking piece.
                {
                    while (piecesToCheck != 0) // through each piece 
                    {
                        ulong tempPiece = BitBoardTools.popLSB(ref piecesToCheck);
                        if (board.IsColour(tempPiece, EnemyColour))
                        {
                            if ((i <= 3 && board.IsOrthogonal(tempPiece)) || (i >= 4 && board.IsDiagonal(tempPiece))) // 0-3 are orthoganal rays, 4-7 are diagonal rays
                            { // make sure it is the right piece for the right direction
                                if (friendlyPieces == 0 && maybePin == 0)
                                {
                                    isDoubleCheck = isCheck;
                                    isCheck = true;
                                }
                                if (maybePin == 0)
                                {
                                    maybePin = tempPiece;
                                }
                            }
                            else
                            { // if the first piece is not a piece that can not deliver check then we dont need to search the rest of the ray
                                break;
                            }
                        }

                        else
                        { // it is a friendly piece
                            if (maybePin == 0)
                            { // there is not a checking piece before it so it could be pinned
                                friendlyPieces++;
                            }
                        }
                    }

                    if (isCheck && maybePin > 0 && friendlyPieces == 0) // it is a check
                    {
                        checkRayMask |= Magic.ForwardFill(KingSquareBitboard, maybePin, BitBoardTools.EdgeDirectionMask[i], BitBoardTools.DirectionMagnitudes[i]);
                    }

                    if (friendlyPieces == 1 && maybePin > 0) // forward fill to create the pin mask
                    {
                        ulong mask = Magic.ForwardFill(KingSquareBitboard, maybePin, BitBoardTools.EdgeDirectionMask[i], BitBoardTools.DirectionMagnitudes[i]);
                        pinRayMask |= mask;
                        switch (i)
                        {
                            case 0:
                            case 2:
                                pinV |= mask;
                                break;

                            case 1:
                            case 3:
                                pinH |= mask;

                                break;
                            default:
                                pinD |= mask;
                                break;
                        }
                        pinDirectionMasks[i] = mask;
                    }
                }

                else
                { // the LSB is moving towards the king
                    while (piecesToCheck != 0)
                    {
                        ulong tempPiece = BitBoardTools.popLSB(ref piecesToCheck);
                        if (board.IsColour(tempPiece, EnemyColour))
                        {
                            if ((i < 4 && board.IsOrthogonal(tempPiece)) || (i > 3 && board.IsDiagonal(tempPiece)))
                            {
                                maybePin = tempPiece; // it could be check or a pin
                                friendlyPieces = 0;
                            }

                            else
                            {
                                maybePin = 0;
                            }
                        }
                        else
                        {
                            if (maybePin != 0) // if it is before a piece that can check the king then we can ignore it
                            {
                                friendlyPieces++;
                            }
                        }
                    }

                    // 
                    if (friendlyPieces == 0 && maybePin > 0)
                    {
                        isDoubleCheck = isCheck;
                        isCheck = true;
                        checkRayMask |= Magic.BackwardFill(KingSquareBitboard, maybePin, BitBoardTools.EdgeDirectionMask[i], BitBoardTools.DirectionMagnitudes[i]);
                    }

                    if (friendlyPieces == 1 && maybePin > 0) // forward fill to create the pin mask
                    {

                        ulong mask = Magic.BackwardFill(KingSquareBitboard, maybePin, BitBoardTools.EdgeDirectionMask[i], BitBoardTools.DirectionMagnitudes[i]); ;
                        pinRayMask |= mask;
                        switch (i)
                        {
                            case 0:
                            case 2:
                                pinV |= mask;
                                break;
                            case 1:
                            case 3:
                                pinH |= mask;
                                break;
                            default:
                                pinD |= mask;
                                break;
                        }
                        pinDirectionMasks[i] = mask;
                    }
                }

                if (isDoubleCheck) // only the king can move in double check so we can stop searching for pins or checks.
                {
                    break;
                }
            }
        }

        public void CalculateOpponentSlidingAttacks()
        {
            ulong OrthogonalSliders = Bitboard.getRooks(board.Board, EnemyColour) | Bitboard.getQueens(board.Board, EnemyColour);
            ulong DiagonalSliders = Bitboard.getBishops(board.Board, EnemyColour) | Bitboard.getQueens(board.Board, EnemyColour);


            GetSliderAttack(OrthogonalSliders, AllPieces ^ KingSquareBitboard, true);
            GetSliderAttack(DiagonalSliders, AllPieces ^ KingSquareBitboard, false);

        }

        internal void GetSliderAttack(ulong pieces, ulong blockers, bool isRook)
        {
            while (pieces != 0)
            {
                ulong loc = BitBoardTools.popLSB(ref pieces);
                if (isRook)
                {
                    opponentAttacksWithoutPawns |= Magic.getRookBlockerAttacks(loc, blockers);
                }

                else
                {
                    opponentAttacksWithoutPawns |= Magic.getBishopBlockerAttacks(loc, blockers);
                }
            }
        }

        bool IsPinned(byte square)
        {
            return ((pinRayMask >> square) & 1) == 1;
        }

        int pinType(ulong square) // will only work if the piece is pinned, other-wise it will return a full bitboard
        {
            for (int i = 0; i < pinDirectionMasks.Length - 1; i++)
            {
                if ((square & pinDirectionMasks[i]) > 0)
                {
                    return i;
                }
            }
            return 8;
        }
    }
}
/*
    Notes:
        Everything should be working so DONT TOUCH ANYTHING. I DONT CARE IF IT MIGHT IMPROVE PERFORMANCE JUST DONT TOUCH IT.
 */



/*
 Original genPawnMoves:


        internal void genPawnMoves(ref List<Move> moves)
        {
            ulong Pawns = Bitboard.getPawns(board.Board, MoveColour);

            while (Pawns != 0)
            {
                int pinDirectionMaskIndex = 8;

                ulong pawnLoc = BitBoardTools.popLSB(ref Pawns);
                if (IsPinned(BitBoardTools.BitboardToIndex(pawnLoc)))
                {
                    pinDirectionMaskIndex = pinType(pawnLoc);
                }

                if (genQuietMoves)
                { // generate pushes

                    // Push the pawn once
                    ulong pawnPush;

                    pawnPush = BitBoardTools.pushPawn(pawnLoc, MoveColour);

                    pawnPush &= ~AllPieces;

                    if ((BitBoardTools.usefulPawnRanks[MoveColour] & pawnPush) == 0) // if not a promotion
                    {

                        if ((BitBoardTools.usefulPawnRanks[MoveColour + 2] & pawnLoc) > 0 && pawnPush > 0)
                        { // if it is on the starting rank then gen double pushes
                            ulong pawnDoublePush;
                            pawnDoublePush = BitBoardTools.pushPawn(BitBoardTools.pushPawn(pawnLoc, MoveColour), MoveColour);

                            pawnDoublePush &= ~AllPieces;
                            pawnDoublePush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                            if (pawnDoublePush != 0)
                            {
                                moves.Add(new Move(pawnLoc, pawnDoublePush, Move.enPassantSet));
                            }
                        }
                        pawnPush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                        addLegalMoves(ref moves, pawnPush, pawnLoc);
                    }
                    else
                    {
                        pawnPush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                        if (pawnPush != 0)
                        {
                            genPromotions(ref moves, pawnLoc, pawnPush);
                        }
                    }
                }


                // gen captures / enpassant
                ulong pawnCaptures = BitBoardTools.getPawnAttacks(pawnLoc, MoveColour);
                pawnCaptures &= notFriendlyPieces & pinDirectionMasks[pinDirectionMaskIndex];

                if (pawnCaptures != 0)
                {
                    if ((pawnLoc & BitBoardTools.usefulPawnRanks[MoveColour + 4]) > 0 && board.enPassantFile != -1)
                    { // if on the correct enPassantrank && the enPassant flag is enabled 
                        ulong potenialEnPassant = pawnCaptures & (BitBoardTools.AFile >> (board.enPassantFile - 1));
                        ulong enPassantSquare = BitBoardTools.pushPawn(potenialEnPassant, EnemyColour);
                        if (potenialEnPassant > 0 && (enPassantSquare & checkRayMask) > 0 && notCheckAfterEnPassant(potenialEnPassant, pawnLoc, enPassantSquare))
                        {
                            moves.Add(new Move(pawnLoc, potenialEnPassant, Move.enPassantCapture));
                        }
                    }

                    pawnCaptures &= board.Board[EnemyColour] & checkRayMask;

                    if (pawnCaptures != 0)
                    {
                        if ((BitBoardTools.usefulPawnRanks[MoveColour] & pawnCaptures) == 0)
                        {
                            addLegalMoves(ref moves, pawnCaptures, pawnLoc);
                        }

                        else
                        {
                            while (pawnCaptures != 0)
                            {
                                ulong pawnCapturePromotion = BitBoardTools.popLSB(ref pawnCaptures);
                                genPromotions(ref moves, pawnLoc, pawnCapturePromotion);
                            }
                        }
                    }
                }

            }
        }

 */