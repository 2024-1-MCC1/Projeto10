using UnityEngine;


using UnityEngine;

namespace LowPolyWater
{
    public class LowPolyWater : MonoBehaviour
    {
        public float alturaOnda = 0.5f; // Altura da onda
        public float frequenciaOnda = 0.5f; // Frequência da onda
        public float comprimentoOnda = 0.75f; // Comprimento da onda

        // Posição de onde as ondas se originam
        public Vector3 posicaoOrigemOnda = new Vector3(0.0f, 0.0f, 0.0f);

        MeshFilter filtroDeMalha;
        Mesh malha;
        Vector3[] vertices;

        private void Awake()
        {
            // Obter o Filtro de Malha do objeto
            filtroDeMalha = GetComponent<MeshFilter>();
        }

        void Start()
        {
            CriarMalhaBaixaResolucao(filtroDeMalha);
        }

        /// <summary>
        /// Rearranja os vértices da malha para criar um efeito de 'baixa resolução'
        /// </summary>
        /// <param name="mf">Filtro de malha do objeto</param>
        /// <returns></returns>
        MeshFilter CriarMalhaBaixaResolucao(MeshFilter mf)
        {
            malha = mf.sharedMesh;

            // Obter os vértices originais da malha do objeto
            Vector3[] verticesOriginais = malha.vertices;

            // Obter a lista de índices de triângulos da malha do objeto
            int[] triangulos = malha.triangles;

            // Criar uma matriz de vetores para os novos vértices
            Vector3[] vertices = new Vector3[triangulos.Length];

            // Atribuir vértices para criar triângulos a partir da malha
            for (int i = 0; i < triangulos.Length; i++)
            {
                vertices[i] = verticesOriginais[triangulos[i]];
                triangulos[i] = i;
            }

            // Atualizar a malha do objeto com os novos vértices
            malha.vertices = vertices;
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateBounds();
            malha.RecalculateNormals();
            this.vertices = malha.vertices;

            return mf;
        }

        void Update()
        {
            GerarOndas();
        }

        /// <summary>
        /// Com base na altura e frequência da onda especificadas, gera
        /// movimento de onda originário da posição de origem da onda
        /// </summary>
        void GerarOndas()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];

                // Inicialmente define a altura da onda como 0
                v.y = 0.0f;

                // Obtém a distância entre a posição de origem da onda e o vértice atual
                float distancia = Vector3.Distance(v, posicaoOrigemOnda);
                distancia = (distancia % comprimentoOnda) / comprimentoOnda;

                // Oscila a altura da onda via seno para criar um efeito de onda
                v.y = alturaOnda * Mathf.Sin(Time.time * Mathf.PI * 2.0f * frequenciaOnda
                + (Mathf.PI * 2.0f * distancia));

                // Atualiza o vértice
                vertices[i] = v;
            }

            // Atualiza as propriedades da malha
            malha.vertices = vertices;
            malha.RecalculateNormals();
            malha.MarkDynamic();
            filtroDeMalha.mesh = malha;
        }
    }
}