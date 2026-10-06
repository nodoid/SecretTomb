using System;
using System.Collections.Generic;

namespace SecretTomb.Core.Graphics;

/// <summary>
/// The script of the tomb's builders: one 8 x 8 sign for each letter, as in the original, where
/// the inscription "A LA GLOIRE D'AXAYACATL" was written with its translation underneath, the key
/// to every other message in the tomb. Each sign is a rounded cartouche holding a symmetrical
/// pattern, generated so that no two letters look alike. Accents are ignored, and '#' is the
/// skull, the sign of death.
/// </summary>
public static class Glyphs
{
    public const int Size = 8;

    /// <summary>26 letters and the skull.</summary>
    public const int Count = 27;

    public const char Skull = '#';

    private static readonly byte[][] Signs = Build();

    public static ReadOnlySpan<byte> Rows(int index) => Signs[index];

    /// <summary>Sign number for a character, or -1 for a gap (spaces and punctuation).</summary>
    public static int IndexOf(char c)
    {
        c = char.ToUpperInvariant(Fold(c));
        if (c >= 'A' && c <= 'Z')
            return c - 'A';
        return c == Skull ? 26 : -1;
    }

    /// <summary>Removes the accent from a French letter.</summary>
    public static char Fold(char c) => c switch
    {
        'É' or 'È' or 'Ê' or 'Ë' or 'é' or 'è' or 'ê' or 'ë' => 'E',
        'À' or 'Â' or 'à' or 'â' => 'A',
        'Î' or 'Ï' or 'î' or 'ï' => 'I',
        'Ô' or 'ô' => 'O',
        'Ù' or 'Û' or 'Ü' or 'ù' or 'û' or 'ü' => 'U',
        'Ç' or 'ç' => 'C',
        _ => c,
    };

    private static byte[][] Build()
    {
        var signs = new byte[Count][];
        var used = new HashSet<int>();
        uint seed = 0xA2A7ACA7;
        for (int letter = 0; letter < 26; letter++)
        {
            int pattern;
            do
            {
                seed = seed * 1664525 + 1013904223;
                pattern = (int)(seed >> 8) & 0x7FFF;
            } while (!Distinct(pattern) || !used.Add(pattern));
            signs[letter] = Cartouche(pattern);
        }
        // The skull: a round head, two hollow eyes and teeth.
        signs[26] =
        [
            0b00111100,
            0b01111110,
            0b11011011,
            0b11011011,
            0b11111111,
            0b01100110,
            0b00111100,
            0b00101000,
        ];
        return signs;
    }

    /// <summary>Patterns need enough ink to read, and not so much that they become a blot.</summary>
    private static bool Distinct(int pattern)
    {
        int bits = 0;
        for (int p = pattern; p != 0; p >>= 1)
            bits += p & 1;
        return bits >= 5 && bits <= 10;
    }

    /// <summary>
    /// A cartouche: rounded frame with a 6 x 5 interior, mirrored left to right (15 pattern bits:
    /// three columns by five rows).
    /// </summary>
    private static byte[] Cartouche(int pattern)
    {
        var rows = new byte[Size];
        rows[0] = 0b01111110;
        rows[7] = 0b01111110;
        for (int y = 1; y <= 6; y++)
            rows[y] = 0b10000001;
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if ((pattern >> (r * 3 + c) & 1) == 0)
                    continue;
                // Interior columns 1..6; c 0..2 is the left half, mirrored to 6..4.
                rows[1 + r] |= (byte)(1 << (6 - c));
                rows[1 + r] |= (byte)(1 << (1 + c));
            }
        }
        return rows;
    }
}
