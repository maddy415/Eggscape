using UnityEngine;

/// <summary>
/// Interface para objetos que podem ser afetados por slow motion
/// </summary>
public interface ISlowMotionable
{
    /// <summary>
    /// Define o multiplicador de velocidade do objeto
    /// </summary>
    /// <param name="scale">0.2f = 20% da velocidade normal, 1.0f = velocidade normal</param>
    void SetSlowMotion(float scale);
    
    /// <summary>
    /// Retorna o objeto à velocidade normal
    /// </summary>
    void ResetSpeed();
}