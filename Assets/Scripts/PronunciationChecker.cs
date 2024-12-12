using System;
using System.Linq;
using UnityEngine;
using TMPro;

public class PronunciationChecker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pronunciationFeedbackText;
    [SerializeField] private TextMeshProUGUI phraseText;

    private string[] targetPhrases = new string[]
    {
        "¿Me traés un café con leche, por favor?",
        "Quisiera un cortado, ¿tenés?",
        "¿Me podés poner un café solo, por favor?",
        "Quiero un latte grande, ¿está bien?",
        "¿Me das un té con leche?",
        "Voy a pedir un café helado, ¿me lo podés traer?",
        "¿Me traés una medialuna con un café?",
        "Quiero un capuchino, por favor.",
        "¿Podés traerme un jugo de naranja natural?",
        "Te pido un chocolate caliente con churros."
    };

    private int currentPhraseIndex = 0;

    void Start()
    {
        ShowNextPhrase();
    }

    // Ahora acepta dos parámetros: frase objetivo y frase dicha
    public void EvaluarPronunciacion(string fraseObjetivo, string fraseDicha)
    {
        float similarity = CheckPronunciacion(fraseObjetivo, fraseDicha);
        int puntuacion = Mathf.RoundToInt(similarity * 100);

        string feedback = $"Frase Objetivo: {fraseObjetivo}\n" +
                          $"Frase Dicha: {fraseDicha}\n" +
                          $"Puntuación: {puntuacion}%\n";

        // Detectar la palabra con mayor error
        string palabraErronea = DetectarPalabraConMasError(fraseObjetivo, fraseDicha);
        if (!string.IsNullOrEmpty(palabraErronea))
        {
            feedback += $"La palabra con más error de pronunciación podría ser: {palabraErronea}\n";
        }

        // Dar feedback según el puntaje
        if (similarity >= 0.9f)
        {
            feedback += "¡Excelente pronunciación!";
        }
        else if (similarity >= 0.75f)
        {
            feedback += "Bien, pero podrías mejorar la pronunciación.";
        }
        else
        {
            feedback += "No se entendió muy bien, intentá repetir.";
        }

        // Mostrar el feedback en el UI
        if (pronunciationFeedbackText != null)
        {
            pronunciationFeedbackText.text = feedback;
        }
        else
        {
            Debug.LogWarning("pronunciationFeedbackText no está asignado.");
        }

        Debug.Log(feedback);

        // Mostrar la siguiente frase después de un breve retraso
        Invoke("ShowNextPhrase", 2f);
    }

    /// <summary>
    /// Muestra la siguiente frase en la lista de frases objetivo.
    /// </summary>
    private void ShowNextPhrase()
    {
        if (currentPhraseIndex < targetPhrases.Length - 1)
        {
            currentPhraseIndex++;
            phraseText.text = targetPhrases[currentPhraseIndex];

            // Actualiza la frase objetivo actual
            UpdateFraseObjetivo();
        }
        else
        {
            phraseText.text = "¡Has completado todas las frases!";
            Debug.Log("¡Completaste todas las frases!");
        }
    }

    // Actualiza la frase objetivo con el texto de phraseText
    private void UpdateFraseObjetivo()
    {
        if (phraseText != null)
        {
            // Asigna el texto actual de phraseText a la frase objetivo
            string fraseObjetivoActual = phraseText.text;
            Debug.Log($"Frase Objetivo Actual: {fraseObjetivoActual}");
        }
        else
        {
            Debug.LogWarning("phraseText no está asignado.");
        }
    }

    // Método que devuelve la frase actual que está mostrando phraseText
    public string GetCurrentPhrase()
    {
        return phraseText.text;
    }

    private float CheckPronunciacion(string original, string spoken)
    {
        string normOriginal = NormalizeText(original);
        string normSpoken = NormalizeText(spoken);

        // Convertir a representación fonética (puedes integrar una API o lógica fonética aquí)
        string phoneticOriginal = GetPhoneticRepresentation(normOriginal);
        string phoneticSpoken = GetPhoneticRepresentation(normSpoken);

        // Comparar usando un algoritmo fonético (Soundex, Metaphone, o comparación fonética avanzada)
        int distance = PhoneticDistance(phoneticOriginal, phoneticSpoken);
        float similarity = 1f - (float)distance / phoneticOriginal.Length;
        return similarity;
    }

    private string NormalizeText(string input)
    {
        input = input.ToLowerInvariant()
                     .Replace("á", "a")
                     .Replace("é", "e")
                     .Replace("í", "i")
                     .Replace("ó", "o")
                     .Replace("ú", "u");

        char[] allowedChars = input.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
        return new string(allowedChars).Trim();
    }

    private string GetPhoneticRepresentation(string input)
    {
        // Reemplaza con la lógica real de transcripción fonética o integración de API
        return input.ToLowerInvariant(); // Este es un ejemplo simple
    }

    private int PhoneticDistance(string original, string spoken)
    {
        // Puedes utilizar un algoritmo fonético aquí (Soundex, Metaphone, o un análisis más avanzado)
        return LevenshteinDistance(original, spoken);
    }

    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }

    private string DetectarPalabraConMasError(string original, string spoken)
    {
        string normOriginal = NormalizeText(original);
        string normSpoken = NormalizeText(spoken);

        string[] palabrasOriginal = normOriginal.Split(' ');
        string[] palabrasSpoken = normSpoken.Split(' ');

        int len = Math.Min(palabrasOriginal.Length, palabrasSpoken.Length);

        string peorPalabra = null;
        float peorSimilitud = 1f;

        for (int i = 0; i < len; i++)
        {
            int dist = LevenshteinDistance(palabrasOriginal[i], palabrasSpoken[i]);
            float sim = 1f - (float)dist / palabrasOriginal[i].Length;

            if (sim < peorSimilitud)
            {
                peorSimilitud = sim;
                peorPalabra = palabrasSpoken[i];
            }
        }

        return peorPalabra;
    }
}
