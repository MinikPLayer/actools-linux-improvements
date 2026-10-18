using System;
using System.Security.Cryptography;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Rijndael with 256-bit blocks (CBC, PKCS7) which is not available in .NET Core (only AES, 128-bit blocks, is). Needed to be able to read
    /// values encrypted by earlier versions. Not meant to be fast.
    /// </summary>
    public static class Rijndael256 {
        private const int Nb = 8; // 32 bytes block
        public const int BlockSize = Nb * 4;

        private static readonly byte[] SBox = new byte[256];
        private static readonly byte[] InvSBox = new byte[256];
        private static readonly byte[] Rcon = new byte[16];
        private static readonly int[] Shifts = { 0, 1, 3, 4 };

        static Rijndael256() {
            // S-box: multiplicative inverse + affine transformation
            byte p = 1, q = 1;
            do {
                p = (byte)(p ^ (p << 1) ^ ((p & 0x80) != 0 ? 0x1b : 0));
                q ^= (byte)(q << 1);
                q ^= (byte)(q << 2);
                q ^= (byte)(q << 4);
                if ((q & 0x80) != 0) q ^= 0x09;

                var x = (byte)(q ^ Rol(q, 1) ^ Rol(q, 2) ^ Rol(q, 3) ^ Rol(q, 4));
                SBox[p] = (byte)(x ^ 0x63);
            } while (p != 1);
            SBox[0] = 0x63;

            for (var i = 0; i < 256; i++) {
                InvSBox[SBox[i]] = (byte)i;
            }

            byte r = 1;
            for (var i = 1; i < Rcon.Length; i++) {
                Rcon[i] = r;
                r = XTime(r);
            }
        }

        private static byte Rol(byte b, int n) {
            return (byte)((b << n) | (b >> (8 - n)));
        }

        private static byte XTime(byte b) {
            return (byte)((b << 1) ^ ((b & 0x80) != 0 ? 0x1b : 0));
        }

        private static byte Mul(byte a, byte b) {
            byte r = 0;
            while (b != 0) {
                if ((b & 1) != 0) r ^= a;
                a = XTime(a);
                b >>= 1;
            }
            return r;
        }

        private class Schedule {
            public readonly byte[] RoundKeys;
            public readonly int Rounds;

            public Schedule(byte[] key) {
                if (key.Length != 16 && key.Length != 24 && key.Length != 32) {
                    throw new CryptographicException("Invalid key size");
                }

                var nk = key.Length / 4;
                Rounds = Math.Max(nk, Nb) + 6;

                var total = Nb * (Rounds + 1);
                RoundKeys = new byte[total * 4];
                Array.Copy(key, RoundKeys, key.Length);

                var temp = new byte[4];
                for (var i = nk; i < total; i++) {
                    Array.Copy(RoundKeys, (i - 1) * 4, temp, 0, 4);
                    if (i % nk == 0) {
                        var t = temp[0];
                        temp[0] = (byte)(SBox[temp[1]] ^ Rcon[i / nk]);
                        temp[1] = SBox[temp[2]];
                        temp[2] = SBox[temp[3]];
                        temp[3] = SBox[t];
                    } else if (nk > 6 && i % nk == 4) {
                        for (var j = 0; j < 4; j++) {
                            temp[j] = SBox[temp[j]];
                        }
                    }

                    for (var j = 0; j < 4; j++) {
                        RoundKeys[i * 4 + j] = (byte)(RoundKeys[(i - nk) * 4 + j] ^ temp[j]);
                    }
                }
            }
        }

        private static void AddRoundKey(byte[] state, Schedule s, int round) {
            var offset = round * BlockSize;
            for (var i = 0; i < BlockSize; i++) {
                state[i] ^= s.RoundKeys[offset + i];
            }
        }

        // state[c * 4 + r] is a byte at row r, column c
        private static void ShiftRows(byte[] state, bool inverse) {
            var copy = new byte[BlockSize];
            for (var r = 1; r < 4; r++) {
                for (var c = 0; c < Nb; c++) {
                    var from = inverse ? (c + Nb - Shifts[r]) % Nb : (c + Shifts[r]) % Nb;
                    copy[c * 4 + r] = state[from * 4 + r];
                }
            }

            for (var r = 1; r < 4; r++) {
                for (var c = 0; c < Nb; c++) {
                    state[c * 4 + r] = copy[c * 4 + r];
                }
            }
        }

        private static void SubBytes(byte[] state, byte[] box) {
            for (var i = 0; i < BlockSize; i++) {
                state[i] = box[state[i]];
            }
        }

        private static void MixColumns(byte[] state, bool inverse) {
            for (var c = 0; c < Nb; c++) {
                var a0 = state[c * 4];
                var a1 = state[c * 4 + 1];
                var a2 = state[c * 4 + 2];
                var a3 = state[c * 4 + 3];
                if (inverse) {
                    state[c * 4] = (byte)(Mul(a0, 14) ^ Mul(a1, 11) ^ Mul(a2, 13) ^ Mul(a3, 9));
                    state[c * 4 + 1] = (byte)(Mul(a0, 9) ^ Mul(a1, 14) ^ Mul(a2, 11) ^ Mul(a3, 13));
                    state[c * 4 + 2] = (byte)(Mul(a0, 13) ^ Mul(a1, 9) ^ Mul(a2, 14) ^ Mul(a3, 11));
                    state[c * 4 + 3] = (byte)(Mul(a0, 11) ^ Mul(a1, 13) ^ Mul(a2, 9) ^ Mul(a3, 14));
                } else {
                    state[c * 4] = (byte)(Mul(a0, 2) ^ Mul(a1, 3) ^ a2 ^ a3);
                    state[c * 4 + 1] = (byte)(a0 ^ Mul(a1, 2) ^ Mul(a2, 3) ^ a3);
                    state[c * 4 + 2] = (byte)(a0 ^ a1 ^ Mul(a2, 2) ^ Mul(a3, 3));
                    state[c * 4 + 3] = (byte)(Mul(a0, 3) ^ a1 ^ a2 ^ Mul(a3, 2));
                }
            }
        }

        private static void EncryptBlock(byte[] state, Schedule s) {
            AddRoundKey(state, s, 0);
            for (var round = 1; round < s.Rounds; round++) {
                SubBytes(state, SBox);
                ShiftRows(state, false);
                MixColumns(state, false);
                AddRoundKey(state, s, round);
            }

            SubBytes(state, SBox);
            ShiftRows(state, false);
            AddRoundKey(state, s, s.Rounds);
        }

        private static void DecryptBlock(byte[] state, Schedule s) {
            AddRoundKey(state, s, s.Rounds);
            for (var round = s.Rounds - 1; round >= 1; round--) {
                ShiftRows(state, true);
                SubBytes(state, InvSBox);
                AddRoundKey(state, s, round);
                MixColumns(state, true);
            }

            ShiftRows(state, true);
            SubBytes(state, InvSBox);
            AddRoundKey(state, s, 0);
        }

        /// <summary>
        /// CBC mode, PKCS7 padding.
        /// </summary>
        [NotNull]
        public static byte[] Encrypt([NotNull] byte[] key, [NotNull] byte[] iv, [NotNull] byte[] data) {
            if (iv.Length != BlockSize) throw new CryptographicException("Invalid IV size");

            var schedule = new Schedule(key);
            var padding = BlockSize - data.Length % BlockSize;
            var result = new byte[data.Length + padding];
            Array.Copy(data, result, data.Length);
            for (var i = data.Length; i < result.Length; i++) {
                result[i] = (byte)padding;
            }

            var previous = (byte[])iv.Clone();
            var block = new byte[BlockSize];
            for (var offset = 0; offset < result.Length; offset += BlockSize) {
                for (var i = 0; i < BlockSize; i++) {
                    block[i] = (byte)(result[offset + i] ^ previous[i]);
                }

                EncryptBlock(block, schedule);
                Array.Copy(block, 0, result, offset, BlockSize);
                Array.Copy(block, previous, BlockSize);
            }

            return result;
        }

        /// <summary>
        /// CBC mode, PKCS7 padding.
        /// </summary>
        [NotNull]
        public static byte[] Decrypt([NotNull] byte[] key, [NotNull] byte[] iv, [NotNull] byte[] data) {
            if (iv.Length != BlockSize) throw new CryptographicException("Invalid IV size");
            if (data.Length == 0 || data.Length % BlockSize != 0) throw new CryptographicException("Invalid data size");

            var schedule = new Schedule(key);
            var result = new byte[data.Length];
            var previous = (byte[])iv.Clone();
            var block = new byte[BlockSize];
            for (var offset = 0; offset < data.Length; offset += BlockSize) {
                Array.Copy(data, offset, block, 0, BlockSize);
                DecryptBlock(block, schedule);
                for (var i = 0; i < BlockSize; i++) {
                    result[offset + i] = (byte)(block[i] ^ previous[i]);
                }

                Array.Copy(data, offset, previous, 0, BlockSize);
            }

            var padding = result[result.Length - 1];
            if (padding == 0 || padding > BlockSize) throw new CryptographicException("Padding is invalid and cannot be removed.");
            for (var i = result.Length - padding; i < result.Length; i++) {
                if (result[i] != padding) throw new CryptographicException("Padding is invalid and cannot be removed.");
            }

            var unpadded = new byte[result.Length - padding];
            Array.Copy(result, unpadded, unpadded.Length);
            return unpadded;
        }
    }
}
