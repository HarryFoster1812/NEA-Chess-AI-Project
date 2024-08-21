using Engine.Representaion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static Engine.BitBoardTools;
using static Engine.Tools;

namespace Engine
{

    public enum GameState
    {
        InProgress,
        Checkmate,
        StaleMate,
    };

    [Serializable]
    public class Bitboard
    {
        public ulong[] Board = new ulong[8]; // Eight bitboards

        // Board[0] = White Pieces eg. 0x 00000000 00000000 00000000 00000000 00000000 00000000 11111111 11111111 
        // Board[1] = Black Pieces eg. 0x 11111111 11111111 00000000 00000000 00000000 00000000 00000000 00000000
        // Board[2] = Pawns        eg. 0x 00000000 11111111 00000000 00000000 00000000 00000000 11111111 00000000
        // Board[3] = Knights      eg. 0x 01000010 00000000 00000000 00000000 00000000 00000000 00000000 01000010
        // Board[4] = Bishops      eg. 0x 00100100 00000000 00000000 00000000 00000000 00000000 00000000 00100100
        // Board[5] = Rooks        eg. 0x 10000001 00000000 00000000 00000000 00000000 00000000 00000000 10000001
        // Board[6] = Queens       eg. 0x 00010000 00000000 00000000 00000000 00000000 00000000 00000000 00010000
        // Board[7] = Kings        eg. 0x 00001000 00000000 00000000 00000000 00000000 00000000 00000000 00001000

        public ushort castlingRights;

        /*
            0001 - White King can castle kingside 
            0010 - White King can castle queenside
            0100 - Black King can castle kingside
            1000 - Black King can castle queenside
         */

        public ushort plyCount = 0;

        public ushort moveCount = 1;

        public short enPassantFile = -1;

        public int capturedPiece = -1;

        public bool WhiteToPlay = true;

        public ZobristKey zobristKey; // the hash of the current position

        [NonSerialized]
        public Generation generator = new Generation();

        public GameState state = GameState.InProgress;

        Stack<BoardState> boardStateHistory = new Stack<BoardState>();

        public Bitboard()
        {

            Board[0] = 0b00000000_00000000_00000000_00000000_00000000_00000000_11111111_11111111;
            Board[1] = 0b11111111_11111111_00000000_00000000_00000000_00000000_00000000_00000000;
            Board[2] = 0b00000000_11111111_00000000_00000000_00000000_00000000_11111111_00000000;
            Board[3] = 0b01000010_00000000_00000000_00000000_00000000_00000000_00000000_01000010;
            Board[4] = 0b00100100_00000000_00000000_00000000_00000000_00000000_00000000_00100100;
            Board[5] = 0b10000001_00000000_00000000_00000000_00000000_00000000_00000000_10000001;
            Board[6] = 0b00010000_00000000_00000000_00000000_00000000_00000000_00000000_00010000;
            Board[7] = 0b00001000_00000000_00000000_00000000_00000000_00000000_00000000_00001000;

            castlingRights = 0b1111;
            zobristKey = Zobrist.CalculateZobrist(this);
        }

        #region Make/Take move
        public void MakeMove(Move move)
        {
            boardStateHistory.Push(new BoardState(zobristKey, castlingRights, plyCount, moveCount, capturedPiece, enPassantFile, state, WhiteToPlay)); // add zobrist hash
            byte start = move.startIndex;
            byte destination = move.destinationIndex;
            ulong startBitboard = IndexToBitboard(start);
            ulong destinationBitboard = IndexToBitboard(destination);
            int MoveColour = WhiteToPlay ? 0 : 1;
            int EnemyColour = 1 - MoveColour;

            zobristKey.moveKey = 0;
            zobristKey.enPassantKey = 0;
            zobristKey.castlingKey = 0;

            enPassantFile = -1;
            capturedPiece = -1;

            plyCount++;


            // if capture
            if ((Board[EnemyColour] & destinationBitboard) > 0)
            {

                capturedPiece = getPieceType(Board, destinationBitboard);
                Board[EnemyColour] ^= destinationBitboard;
                Board[capturedPiece] ^= destinationBitboard;
                Zobrist.changePieceSquare(destination, (capturedPiece - 2) * 2 + EnemyColour, ref zobristKey.pieceKey);

                plyCount = 0; // piece has been captured so the 50 move rule resets
            }
            int pieceType = getPieceType(Board, startBitboard);

            if (pieceType == 2)
            {
                plyCount = 0; // pawn has advanced so the 50 move rule resets
            }

            Board[MoveColour] ^= destinationBitboard | startBitboard;

            Board[pieceType] ^= destinationBitboard | startBitboard;



            Zobrist.changePieceSquare(start, (pieceType - 2) * 2 + MoveColour, ref zobristKey.pieceKey);
            Zobrist.changePieceSquare(destination, (pieceType - 2) * 2 + MoveColour, ref zobristKey.pieceKey);

            // deal with flags
            if (move.flag != 0)
            {

                if (move.isPromotion)
                {
                    int promotionType = move.pieceIndex + 3;
                    Board[promotionType] ^= destinationBitboard;
                    Board[2] ^= destinationBitboard;
                }

                else if (move.flag == Move.castling)
                {
                    switch (destination)
                    {
                        case 1: // white kingside
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.KC);
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.QC);

                            Board[MoveColour] ^= 1;
                            Board[(int)BB.Rooks] ^= 1;
                            Zobrist.changePieceSquare(0, 6 + MoveColour, ref zobristKey.pieceKey);



                            Board[MoveColour] ^= 4;
                            Board[(int)BB.Rooks] ^= 4;
                            Zobrist.changePieceSquare(2, 6 + MoveColour, ref zobristKey.pieceKey);

                            break;

                        case 5:
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.KC);
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.QC);


                            Board[MoveColour] ^= 128;
                            Board[(int)BB.Rooks] ^= 128;
                            Zobrist.changePieceSquare(7, 6 + MoveColour, ref zobristKey.pieceKey);

                            Board[MoveColour] ^= 16;
                            Board[(int)BB.Rooks] ^= 16;
                            Zobrist.changePieceSquare(4, 6 + MoveColour, ref zobristKey.pieceKey);
                            break;

                        case 57:
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.kc);
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.qc);


                            Board[MoveColour] ^= 72057594037927936; // 2^56 
                            Board[(int)BB.Rooks] ^= 72057594037927936;
                            Zobrist.changePieceSquare(56, 6 + MoveColour, ref zobristKey.pieceKey);

                            Board[MoveColour] ^= 288230376151711744; // 2^58
                            Board[(int)BB.Rooks] ^= 288230376151711744;
                            Zobrist.changePieceSquare(58, 6 + MoveColour, ref zobristKey.pieceKey);
                            break;

                        case 61:
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.kc);
                            castlingRights = clearCastleBit(castlingRights, (ushort)Tools.Castling.qc);


                            Board[MoveColour] ^= 9223372036854775808; // 2^63
                            Board[(int)BB.Rooks] ^= 9223372036854775808;
                            Zobrist.changePieceSquare(63, 6 + MoveColour, ref zobristKey.pieceKey);

                            Board[MoveColour] ^= 1152921504606846976; // 2^60
                            Board[(int)BB.Rooks] ^= 1152921504606846976;
                            Zobrist.changePieceSquare(60, 6 + MoveColour, ref zobristKey.pieceKey);
                            break;
                    }
                }

                else if (move.flag == Move.enPassantSet)
                {
                    enPassantFile = CalculateFile(destination);
                }

                else if (move.flag == Move.enPassantCapture)
                {
                    ulong enPassantSquare = pushPawn(destinationBitboard, EnemyColour);
                    Board[2] ^= enPassantSquare;
                    Board[EnemyColour] ^= enPassantSquare;
                    Zobrist.changePieceSquare((ushort)(destination + 8 * (MoveColour == 1 ? 1 : -1)), 0 + EnemyColour, ref zobristKey.pieceKey);
                }
            }

            // if the king moves or the rook moves change the castling rights
            changeCastlingRights(start);
            changeCastlingRights(destination);


            if (!WhiteToPlay)
            {
                moveCount++;
                zobristKey.moveKey ^= Zobrist.zobristTables[14][0];
            }

            ulong tempCastling = castlingRights;
            while (tempCastling != 0)
            {
                ulong piece = BitBoardTools.popLSB(ref tempCastling);
                int index = BitBoardTools.BitboardToIndex(piece);
                zobristKey.castlingKey ^= Zobrist.zobristTables[12][index];
            }

            if (enPassantFile != -1)
            {
                int index = enPassantFile - 1;
                // if the squares directly next to the pushed pawn are enemy pawns then apply the hash
                ulong pawnTempLoc = pushPawn(destinationBitboard, EnemyColour);
                ulong ajecentMask = BitBoardTools.getPawnAttacks(pawnTempLoc, MoveColour);
                ulong enemyPawns = Bitboard.getPawns(Board, EnemyColour);
                if ((ajecentMask & enemyPawns) > 0)
                {
                    zobristKey.enPassantKey ^= Zobrist.zobristTables[13][index];
                }
            }

            WhiteToPlay = !WhiteToPlay;

        }

        private void changeCastlingRights(byte location)
        {

            switch (location)
            {
                case 0:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.KC);

                    break;
                case 3:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.KC);
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.QC);

                    break;

                case 7:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.QC);

                    break;

                case 56:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.kc);

                    break;

                case 59:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.kc);
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.qc);

                    break;

                case 63:
                    castlingRights = clearCastleBit(castlingRights, (ushort)Castling.qc);

                    break;

                default: break;
            }
        }

        public void UndoMove(Move move)
        {
            // unload previous board state
            BoardState previousState = boardStateHistory.Pop();
            WhiteToPlay = !WhiteToPlay;

            byte start = move.startIndex;
            byte destination = move.destinationIndex;
            ulong startBitboard = IndexToBitboard(start);
            ulong destinationBitboard = IndexToBitboard(destination);

            int MoveColour = WhiteToPlay ? 0 : 1;
            int EnemyColour = 1 - MoveColour;

            int pieceType = getPieceType(Board, destinationBitboard);

            // if capture - replace the piece
            if (capturedPiece != -1)
            {
                Board[EnemyColour] ^= destinationBitboard;
                Board[capturedPiece] ^= destinationBitboard;
            }

            Board[MoveColour] ^= destinationBitboard | startBitboard;
            Board[pieceType] ^= destinationBitboard | startBitboard;


            // deal with flags
            if (move.flag != 0)
            {
                // if promotion - replace the pawn
                if (move.isPromotion)
                {
                    Board[pieceType] ^= startBitboard;
                    Board[2] ^= startBitboard;
                }

                else if (move.flag == Move.castling)
                {
                    // if castle - reset rook position
                    switch (destination)
                    {
                        case 1: // white kingside
                            Board[MoveColour] ^= 1;
                            Board[(int)BB.Rooks] ^= 1;
                            Board[MoveColour] ^= 4;
                            Board[(int)BB.Rooks] ^= 4;
                            break;

                        case 5: // white queenside
                            Board[MoveColour] ^= 128;
                            Board[(int)BB.Rooks] ^= 128;
                            Board[MoveColour] ^= 16;
                            Board[(int)BB.Rooks] ^= 16;
                            break;

                        case 57: // black kingside
                            Board[MoveColour] ^= 72057594037927936; // 2^56 
                            Board[(int)BB.Rooks] ^= 72057594037927936;
                            Board[MoveColour] ^= 288230376151711744; // 2^58
                            Board[(int)BB.Rooks] ^= 288230376151711744;
                            break;

                        case 61: // black queenside
                            Board[MoveColour] ^= 9223372036854775808; // 2^63
                            Board[(int)BB.Rooks] ^= 9223372036854775808;
                            Board[MoveColour] ^= 1152921504606846976; //2^60
                            Board[(int)BB.Rooks] ^= 1152921504606846976;
                            break;
                    }
                }

                // enpassant capture - add pawn to board
                else if (move.flag == Move.enPassantCapture)
                {
                    ulong enPassantSquare = pushPawn(destinationBitboard, EnemyColour);
                    Board[2] ^= enPassantSquare;
                    Board[EnemyColour] ^= enPassantSquare;
                }


            }
            // reset the board variables
            capturedPiece = previousState.capturedPiece;
            enPassantFile = previousState.enpassant_flag;
            castlingRights = previousState.castlingRights;
            plyCount = previousState.ply;
            moveCount = previousState.moves;
            state = previousState.gameState;
            zobristKey = previousState.zobristKey;
        }

        #endregion

        public bool isMate()
        { // this does not need to be called in the searching algorithm as when the moves are being generated it already calculates if it is mate, this is only used for checking in the ui 
            generator.board = this;
            generator.InitiliseVars(false);
            List<Move> moves = generator.GenerateMoves(this);
            if (moves.Count == 0 && generator.isCheck) return true;
            return false;
        }

        public bool isStalemate()
        { // this does not need to be called in the searching algorithm as when the moves are being generated it already calculates if it is mate, this is only used for checking in the ui 
            generator.board = this;
            generator.InitiliseVars(false);
            List<Move> moves = generator.GenerateMoves(this);
            if (moves.Count == 0 && !generator.isCheck) return true;
            return false;
        }

        public bool isCheck()
        {
            generator.board = this;
            generator.InitiliseVars(false);
            generator.CalculateCheckAttacks();
            if (generator.isCheck) return true;
            return false;
        }

        public static int getPieceType(ulong[] Board, ulong square)
        { // will only work if there is a piece on given square
            if ((Board[2] & square) > 0) return 2;
            else if ((Board[3] & square) > 0) return 3;
            else if ((Board[4] & square) > 0) return 4;
            else if ((Board[5] & square) > 0) return 5;
            else if ((Board[6] & square) > 0) return 6;
            else if ((Board[7] & square) > 0) return 7;

            return -1; // there was not a piece on the given square
        }

        public static bool canCastleKingside(ushort castling, int turn)
        {
            if (turn == 0)
            {
                return ((castling & 1) == 1);
            }

            return ((castling & 4) == 4);
        }

        public static bool canCastleQueenside(ushort castling, int turn)
        {
            if (turn == 0)
            {
                return ((castling & 2) == 2);
            }

            return ((castling & 8) == 8);
        }

        #region get piece board functions
        public static ulong getPawns(ulong[] Board, int turn)
        { // return a bitboard of the pawns of the given colour
            return (Board[(int)BB.Pawns] & Board[turn]);
        }

        public static ulong getKnights(ulong[] Board, int turn)
        {
            return (Board[(int)BB.Knights] & Board[turn]);
        }

        public static ulong getBishops(ulong[] Board, int turn)
        {
            return (Board[(int)BB.Bishops] & Board[turn]);
        }

        public static ulong getRooks(ulong[] Board, int turn)
        {
            return (Board[(int)BB.Rooks] & Board[turn]);
        }

        public static ulong getQueens(ulong[] Board, int turn)
        {
            return (Board[(int)BB.Queen] & Board[turn]);
        }

        public static ulong getKings(ulong[] Board, int turn)
        {
            return (Board[(int)BB.King] & Board[turn]);
        }
        #endregion

        public bool IsColour(ulong loc, int colour)
        {
            if ((loc & Board[colour]) > 0)
            {
                return true;
            }

            return false;
        }

        public bool IsDiagonal(ulong loc)
        {
            if ((loc & Board[(int)BB.Bishops]) > 0)
            {
                return true;
            }
            else if ((loc & Board[(int)BB.Queen]) > 0) return true;

            return false;
        }

        public bool IsOrthogonal(ulong loc)
        {
            if ((loc & Board[(int)BB.Rooks]) > 0)
            {
                return true;
            }
            else if ((loc & Board[(int)BB.Queen]) > 0) return true;

            return false;
        }

    }
}

// allow a deep copy to be made of the bitboard
public static class Extensions
{
    public static T DeepClone<T>(this T obj)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Position = 0;

            return (T)formatter.Deserialize(stream);
        }
    }
}

