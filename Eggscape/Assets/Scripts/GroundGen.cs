using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerador de chão/cerca com suporte a slow motion
/// </summary>
public class GroundGen : MonoBehaviour, ISlowMotionable
{
    public GameObject prefabChao;
    public Transform genPos;
    public float larguraDoChao = 1f;
    public float velocidade = 5f;

    private List<GameObject> blocos = new List<GameObject>();
    private Camera cam;
    private float speedMultiplier = 1f; // Multiplicador de velocidade

    void Start()
    {
        cam = Camera.main;

        float larguraVisivel = 2f * cam.orthographicSize * cam.aspect;
        int numBlocos = Mathf.CeilToInt(larguraVisivel / larguraDoChao) + 2;

        for (int i = 0; i < numBlocos; i++)
        {
            float posX = genPos.position.x + i * larguraDoChao;
            Vector3 pos = new Vector3(posX, transform.position.y, 0);
            GameObject bloco = Instantiate(prefabChao, pos, Quaternion.identity, transform);
            blocos.Add(bloco);
        }
    }

    void Update()
    {
        // Aplica movimento com multiplicador de velocidade
        float currentSpeed = velocidade * speedMultiplier;

        foreach (GameObject bloco in blocos)
        {
            bloco.transform.Translate(Vector3.left * currentSpeed * Time.deltaTime, Space.World);
        }
        
        GameObject primeiro = blocos[0];
        float bordaEsquerda = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        if (primeiro.transform.position.x + larguraDoChao < bordaEsquerda)
        {
            blocos.RemoveAt(0);

            GameObject ultimo = blocos[blocos.Count - 1];
            float novaX = ultimo.transform.position.x + larguraDoChao;

            primeiro.transform.position = new Vector3(novaX, primeiro.transform.position.y, 0);
            blocos.Add(primeiro);
        }
    }

    #region ISlowMotionable Implementation

    public void SetSlowMotion(float scale)
    {
        speedMultiplier = scale;
        Debug.Log($"[GroundGen] Slow motion ativado: {scale * 100}% velocidade");
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
        Debug.Log("[GroundGen] Velocidade normal restaurada");
    }

    #endregion
}