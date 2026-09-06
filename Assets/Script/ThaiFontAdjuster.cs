using System.Text;
using UnityEngine;

/// <summary>
/// Utility for adjusting Thai vowel and tone mark positions to ensure correct rendering in Unity TextMeshPro.
/// </summary>
public static class ThaiFontAdjuster
{
    // Thai character codes
    private const char THAI_KO_KAI = '\u0E01';
    private const char THAI_HO_NOK_HUK = '\u0E2E';
    private const char THAI_SARA_AM = '\u0E33';
    private const char THAI_NIKHAHIT = '\u0E4D';

    // Tall consonants that cause tone marks / upper vowels to shift up or left
    // ป (0E1B), ฝ (0E1D), ฟ (0E1E), ฬ (0E2C)
    private static bool IsTallConsonant(char c)
    {
        return c == '\u0E1B' || c == '\u0E1D' || c == '\u0E1E' || c == '\u0E2C';
    }

    // Lower consonants with tail
    // ฐ (0E10), ญ (0E0D)
    private static bool IsTailConsonant(char c)
    {
        return c == '\u0E10' || c == '\u0E0D';
    }

    // Upper vowels (Level 1): ิ ี ึ ื ั ็
    public static bool IsUpperVowel(char c)
    {
        return c == '\u0E31' || (c >= '\u0E34' && c <= '\u0E37') || c == '\u0E47';
    }

    // Lower vowels: ุ ู ฺ
    public static bool IsLowerVowel(char c)
    {
        return c >= '\u0E38' && c <= '\u0E3A';
    }

    // Tone marks (Level 2): ่ ้ ๊ ๋ ์ ํ ๎
    public static bool IsToneMark(char c)
    {
        return (c >= '\u0E48' && c <= '\u0E4C') || c == '\u0E4D' || c == '\u0E4E';
    }

    /// <summary>
    /// Checks if a character is any Thai combining vowel or tone mark
    /// </summary>
    public static bool IsThaiCombiningMark(char c)
    {
        return IsUpperVowel(c) || IsLowerVowel(c) || IsToneMark(c);
    }

    /// <summary>
    /// Adjusts Thai text for proper display.
    /// Handles SARA AM (0E33) combined with tone marks (e.g. น + ้ + ำ -> น + ํ + ้ + า)
    /// </summary>
    public static string Adjust(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        StringBuilder sb = new StringBuilder(text.Length + 10);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // Handle Tone mark + SARA AM (e.g. น้ำ, ทำ, ป้ำ)
            // If tone mark is followed by SARA_AM (0E33), convert SARA_AM to NIKHAHIT (0E4D) + Tone + SARA AA (0E32)
            if (IsToneMark(c) && i + 1 < text.Length && text[i + 1] == THAI_SARA_AM)
            {
                sb.Append(THAI_NIKHAHIT);
                sb.Append(c);
                sb.Append('\u0E32'); // SARA AA
                i++; // skip SARA AM
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
