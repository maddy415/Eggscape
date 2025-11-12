using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "VN/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [TextArea(2, 6)]
        public string text;

        [Tooltip("Se definido, troca o fundo antes de mostrar este texto.")]
        public Sprite backgroundOverride;

        [Tooltip("Velocidade local (chars/seg). 0 = usa global do controller.")]
        public float localTypeSpeed = 0f;

        [Tooltip("Pausa extra (segundos) ao final desta frase antes de permitir avançar (0 = nenhuma).")]
        public float endHold = 0f;
    }

    public List<Entry> entries = new List<Entry>();
}