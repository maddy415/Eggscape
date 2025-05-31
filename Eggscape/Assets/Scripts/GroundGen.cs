using System.Collections.Generic;
using UnityEngine;

public class GroundGen : MonoBehaviour
{
    public GameObject prefabChao;
    public Transform genPos;
    public float larguraDoChao = 1f;
    public float velocidade = 5f;
    //public float bordaEsquerda;

    private List<GameObject> blocos = new List<GameObject>();
    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        float larguraVisivel = 2f * cam.orthographicSize * cam.aspect;
        float x = genPos.position.x;

        
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
        foreach (GameObject bloco in blocos)
        {
            bloco.transform.Translate(Vector3.left * velocidade * Time.deltaTime);
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
}