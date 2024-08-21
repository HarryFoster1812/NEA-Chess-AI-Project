using System.Collections.Generic;

namespace Engine
{
    public class Generation
    {
        public enum Promotions { All, QueenOnly, QueenAndKnight }

        public Promotions promotionsToGenerate = Promotions.All;
        public static readonly ushort[][] PromotionFlags = { new ushort[] { 4, 5, 6, 7 }, new ushort[] { 7 }, new ushort[] { 7, 4 } };  // stores the promotion flags which are passed to the move constructor

        static private readonly bool[] directionConsts = { true, false, false, true, true, false, false, true }; // the directions where the LSB is moving away or towards the king square (true for moving away)

        int MoveColour; // the index whose turn (0 white, 1 black)
        int EnemyColour; // the other index

        byte KingSquare; // the index of the current players king
        ulong KingSquareBitboard; // the bitboard of the kings location

        public bool isCheck;
        public bool isDoubleCheck;

        bool genQuietMoves; // only generate quiet moves (moves that are not captures)


        // If in check, this bitboard contains squares in line from checking piece up to king
        // If not in check, all bits are set to 1
        ulong checkRayMask;

        ulong pinRayMask; // the bitboard with the pins that are on the king
        ulong pinH; // the horizontal pins (E and W)
        ulong pinV; // the vertical pins (N and S)
        ulong pinHV; // the horizontal and verical pins (E, W, N, S)
        ulong pinD; // the diagonal pins (NE, SE, SW, NW)
        ulong[] pinDirectionMasks = new ulong[9]; // {N, E, S, W, NE, SE, SW, NW, Full bitboard}
        internal ulong opponentAttacks; // all of the square that the opponents can move to / are attacking
        internal ulong opponentAttacksWithoutPawns; // opponents attacks without the opponent pawnmoves
        ulong AllPieces; // all the pieces on the board
        ulong notFriendlyPieces; // all of the opponents pieces

        internal Bitboard board;

        // If only captures should be generated, this will have 1s only in positions of enemy pieces.
        // Otherwise it will have 1s everywhere.
        ulong moveTypeMask;

        /// <summary>
        /// Generates all of the legal moves for any bitboard
        /// </summary>
        /// <param name="board">the amount of time in ms that white has left on the clock</param>
        /// <param name="capturesOnly">Limits the moves generated to be captures only. Default value is false</param>
        /// <returns>A list of valid moves</returns>
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

        /// <summary>
        /// Loops through the bitboard of legal moves and creates a Move struct for each and adds them to the list of valid moves
        /// </summary>
        /// <param name="moves">The list of moves</param>
        /// <param name="legalMoves">The bitboard of legal moves (places the piece can move to)</param>
        /// <param name="start">The bitboard of the start location</param>
        /// <param name="insertAtFront">If true inserts the move at the start of the list</param>
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

        /// <summary>
        /// Generates all of the promotions and adds them to the list of moves
        /// </summary>
        /// <param name="moves">The list of moves</param>
        /// <param name="start">The bitboard of the start location</param>
        /// <param name="destination">The bitboard of the destination of the move</param>
        void genPromotions(ref List<Move> moves, ulong start, ulong destination)
        {
            foreach (ushort i in PromotionFlags[(int)promotionsToGenerate])
            {
                moves.Insert(0, new Move(start, destination, i));
            }
        }

        /// <summary>
        /// Generates all of legal king moves and adds them to the list
        /// </summary>
        /// <param name="moves">The list of moves</param>
        private void genKingMoves(ref List<Move> moves)
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

        /// <summary>
        /// Generates all of legal knight moves and adds them to the list
        /// </summary>
        /// <param name="moves">The list of moves</param>
        void genKnightMoves(ref List<Move> moves)
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

        /// <summary>
        /// Generates all of legal moves for the sliding pieces (rooks, bishops and queens) and adds them to the list
        /// </summary>
        /// <param name="moves">The list of moves</param>
        void genSlidingMoves(ref List<Move> moves)
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
                ulong OrthognalPiece = BitBoardTools.popLSB(ref OrthogonalSliders); // gets the bitboard the pieces location
                byte PieceSquareIndex = BitBoardTools.BitboardToIndex(OrthognalPiece); // converts it into an index
                ulong legalMoves = Magic.getRookBlockerAttacks(OrthognalPiece, AllPieces); // gets all the pseudo-legal moves via the magic bitboard tables
                legalMoves &= checkRayMask & moveTypeMask & notFriendlyPieces; // legalises the pseudo-legal moves

                if (IsPinned(PieceSquareIndex))
                { // pinned rook can only move horizontally and vertically
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
                { // pinned bishops can only move along the pinned diagonals
                    legalMoves &= pinD;
                }

                addLegalMoves(ref moves, legalMoves, DiagonalPiece, false);
            }
        }

        /// <summary>
        /// Generates all of legal pawn moves and adds them to the list
        /// </summary>
        /// <param name="moves">The list of moves</param>
        internal void genPawnMoves(ref List<Move> moves)
        {
            ulong Pawns = Bitboard.getPawns(board.Board, MoveColour); // gets the bitboard of all the pawns to loop through

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
                        { // if it is on the starting rank then generate double pushes
                            ulong pawnDoublePush;
                            pawnDoublePush = BitBoardTools.pushPawn(BitBoardTools.pushPawn(pawnLoc, MoveColour), MoveColour); // double push the pawn

                            pawnDoublePush &= ~AllPieces; // if it does not end up on a piece (the double push is empty)
                            pawnDoublePush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex]; // make sure the pawn is not pinned or causes a check
                            if (pawnDoublePush != 0) // if it can double push add the move
                            {
                                moves.Add(new Move(pawnLoc, pawnDoublePush, Move.enPassantSet));
                            }
                        }
                        // add the single pawn push
                        pawnPush &= checkRayMask & moveTypeMask & pinDirectionMasks[pinDirectionMaskIndex];
                        addLegalMoves(ref moves, pawnPush, pawnLoc, false);
                    }

                    else
                    { // it is a promotion
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
                        ulong potenialEnPassant = pawnCaptures & (BitBoardTools.AFile >> (board.enPassantFile - 1)); // gets the enpassant square that can be taken on
                        ulong enPassantSquare = BitBoardTools.pushPawn(potenialEnPassant, EnemyColour); // gets the square that the enemy pawn is on

                        if (potenialEnPassant > 0 && (enPassantSquare & checkRayMask) > 0 && notCheckAfterEnPassant(potenialEnPassant, pawnLoc, enPassantSquare))
                        {
                            moves.Insert(0, new Move(pawnLoc, potenialEnPassant, Move.enPassantCapture));
                        }
                    }

                    pawnCaptures &= board.Board[EnemyColour] & checkRayMask;

                    if (pawnCaptures != 0)
                    {
                        if ((BitBoardTools.usefulPawnRanks[MoveColour] & pawnCaptures) == 0) // it is not a capture promotion
                        {
                            addLegalMoves(ref moves, pawnCaptures, pawnLoc, true);
                        }

                        else
                        {
                            // it is a capture promotion
                            while (pawnCaptures != 0)
                            { // loop over each capture
                                ulong pawnCapturePromotion = BitBoardTools.popLSB(ref pawnCaptures);
                                genPromotions(ref moves, pawnLoc, pawnCapturePromotion);
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Calculates if the king will be in check after the enpassant
        /// </summary>
        /// <param name="destination">Where the capturing pawn will end up</param>
        /// <param name="start">Where the capturing pawn started</param>
        /// <param name="enPassantSquare">Where the EnPassant pawn is</param>
        /// <returns>True if it is a vaild EnPassant capture, False if not</returns>
        bool notCheckAfterEnPassant(ulong destination, ulong start, ulong enPassantSquare)
        {
            ulong afterEnPassantAllPieces = AllPieces; // copy the pieces
            afterEnPassantAllPieces ^= destination | start | enPassantSquare; // add the destination pieces and remove the start and captured pawn
            ulong attacks = Magic.getRookBlockerAttacks(KingSquareBitboard, afterEnPassantAllPieces); // gets the rook blocks since they are the only piece which causes this error

            if ((attacks & Bitboard.getRooks(board.Board, EnemyColour)) > 0) // checks is the rook is attacking the king after the enpassant
            {
                return false; // not valid
            }

            return true; // valid move
        }

        /// <summary>
        /// Resets all of the local variables which are used to generate the moves
        /// </summary>
        /// <param name="capturesonly">Sets if the moves generated are only ones that capture the opponents pieces useful for quicence searches</param>
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

        /// <summary>
        /// Calculates the pin and check masks which are used to generate moves and also builds the bitboard of all of squares the opponents are attacking
        /// </summary>
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

        /// <summary>
        /// Calculates the pin and check masks and also if it is check or double check
        /// </summary>
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
                        if (board.IsColour(tempPiece, EnemyColour)) // if it is an enemy piece
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

                    // no friendly piece was found but there was pinning piece
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

        /// <summary>
        /// Calls the get slider attacks and fills the sider attck bitboards
        /// </summary>
        public void CalculateOpponentSlidingAttacks()
        {
            ulong OrthogonalSliders = Bitboard.getRooks(board.Board, EnemyColour) | Bitboard.getQueens(board.Board, EnemyColour);
            ulong DiagonalSliders = Bitboard.getBishops(board.Board, EnemyColour) | Bitboard.getQueens(board.Board, EnemyColour);


            GetSliderAttack(OrthogonalSliders, AllPieces ^ KingSquareBitboard, true);
            GetSliderAttack(DiagonalSliders, AllPieces ^ KingSquareBitboard, false);

        }

        internal void GetSliderAttack(ulong pieces, ulong blockers, bool isRook)
        {
            while (pieces != 0) // loop over each sliding piece
            {
                ulong loc = BitBoardTools.popLSB(ref pieces); // get the location
                if (isRook)
                {
                    opponentAttacksWithoutPawns |= Magic.getRookBlockerAttacks(loc, blockers); // get the pseudo-legal rooks moves
                }

                else
                {
                    opponentAttacksWithoutPawns |= Magic.getBishopBlockerAttacks(loc, blockers); // get the pseudo-legal bishop moves
                }
            }
        }

        /// <summary>
        /// Calculates if the piece on the given square is pinned
        /// </summary>
        /// <param name="square">The index location of the piece</param>
        /// <returns>True if the piece is pinned, False if the piece is not pinned</returns>
        bool IsPinned(byte square)
        {
            return ((pinRayMask >> square) & 1) == 1;
        }

        /// <summary>
        /// Calculates the type of pin (North pin, North-east pin, East pin, ect.).
        /// Should only be called if the given piece is pinned.
        /// </summary>
        /// <param name="square">The bitboard of the piece</param>
        /// <returns>The index of the pin mask in the pinDirectionMasks array. If the piece was not pinned the index pointing to a full bitboard is returned</returns>
        int pinType(ulong square)
        {
            for (int i = 0; i < pinDirectionMasks.Length - 1; i++)
            {
                if ((square & pinDirectionMasks[i]) > 0)
                {
                    return i;
                }
            }
            return 8; // if it not pinned return the full bitboard
        }
    }
}
/*
    Notes:
        Everything should be working so DONT TOUCH ANYTHING. I DONT CARE IF IT MIGHT IMPROVE PERFORMANCE JUST DONT TOUCH IT.
 */