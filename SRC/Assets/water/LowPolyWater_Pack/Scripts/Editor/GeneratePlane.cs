
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LowPolyWater
{
    public class GeneratePlane : ScriptableWizard
    {
        public string nomeObjeto;            // Nome opcional que pode ser atribuído ao objeto plano criado

        public int segmentosLargura = 1;     // Número de segmentos para dividir o plano verticalmente
        public int segmentosAltura = 1;      // Número de segmentos para dividir o plano horizontalmente
        public float larguraPlano = 1.0f;
        public float alturaPlano = 1.0f;

        public bool adicionarColisor = false;    // Adicionar colisor de caixa?
        public Material material;                // Por padrão, é atribuído a 'LowPolyWaterMaterial' no editor

        static Camera cam;
        static Camera ultimaCameraUsada;

        // As malhas de plano geradas são salvas e carregadas da pasta Plane Meshes (você pode alterá-la para o que desejar)
        public static string localSalvamentoAtivo = "Assets/Low Poly Water/Plane Meshes/";

        [MenuItem("GameObject/LowPoly Water/Generate Water Plane...")]
        static void CriarAssistente()
        {
            cam = Camera.current;
            // Hack porque camera.current não retorna a câmera do editor se a visualização da cena não tiver foco
            if (!cam)
            {
                cam = ultimaCameraUsada;
            }
            else
            {
                ultimaCameraUsada = cam;
            }

            // Verifique se a pasta de local de salvamento de ativos existe
            // Se a pasta não existir, crie-a
            if (!Directory.Exists(localSalvamentoAtivo))
            {
                Directory.CreateDirectory(localSalvamentoAtivo);
            }

            // Abrir Assistente
            DisplayWizard("Gerar Plano de Água", typeof(GeneratePlane));
        }

        void OnWizardUpdate()
        {
            // O número máximo de segmentos é 254, porque uma malha não pode ter mais
            // do que 65000 vértices (254^2 = 64516 número máximo de vértices)
            segmentosLargura = Mathf.Clamp(segmentosLargura, 1, 254);
            segmentosAltura = Mathf.Clamp(segmentosAltura, 1, 254);
        }

        private void OnWizardCreate()
        {
            // Cria um objeto vazio
            GameObject plano = new GameObject();

            // Se o usuário não atribuiu um nome, por padrão o nome do objeto é 'Plano'
            if (string.IsNullOrEmpty(nomeObjeto))
            {
                plano.name = "Plano";
            }
            else
            {
                plano.name = nomeObjeto;
            }

            // Cria componentes Filtro de Malha e Renderizador de Malha
            MeshFilter filtroDeMalha = plano.AddComponent(typeof(MeshFilter)) as MeshFilter;
            MeshRenderer renderizadorDeMalha = plano.AddComponent((typeof(MeshRenderer))) as MeshRenderer;
            renderizadorDeMalha.sharedMaterial = material;

            // Gera um nome para a malha que será criada
            string nomeMalhaPlano = plano.name + segmentosLargura + "x" + segmentosAltura
                                        + "W" + larguraPlano + "H" + alturaPlano + ".asset";

            // Carrega a malha do local de salvamento
            Mesh m = (Mesh)AssetDatabase.LoadAssetAtPath(localSalvamentoAtivo + nomeMalhaPlano, typeof(Mesh));

            // Se não houver uma malha localizada em ativos, cria a malha
            if (m == null)
            {
                m = new Mesh();
                m.name = plano.name;

                int hCount2 = segmentosLargura + 1;
                int vCount2 = segmentosAltura + 1;
                int numTriangles = segmentosLargura * segmentosAltura * 6;
                int numVertices = hCount2 * vCount2;

                Vector3[] vertices = new Vector3[numVertices];
                Vector2[] uvs = new Vector2[numVertices];
                int[] triangles = new int[numTriangles];
                Vector4[] tangents = new Vector4[numVertices];
                Vector4 tangente = new Vector4(1f, 0f, 0f, -1f);
                Vector2 deslocamentoAncora = Vector2.zero;

                int index = 0;
                float fatorUVX = 1.0f / segmentosLargura;
                float fatorUVY = 1.0f / segmentosAltura;
                float escalaX = larguraPlano / segmentosLargura;
                float escalaY = alturaPlano / segmentosAltura;

                // Gera os vértices
                for (float y = 0.0f; y < vCount2; y++)
                {
                    for (float x = 0.0f; x < hCount2; x++)
                    {
                        vertices[index] = new Vector3(x * escalaX - larguraPlano / 2f - deslocamentoAncora.x, 0.0f, y * escalaY - alturaPlano / 2f - deslocamentoAncora.y);

                        tangents[index] = tangente;
                        uvs[index++] = new Vector2(x * fatorUVX, y * fatorUVY);
                    }
                }

                // Resetar o índice e gerar os triângulos
                index = 0;
                for (int y = 0; y < segmentosAltura; y++)
                {
                    for (int x = 0; x < segmentosLargura; x++)
                    {
                        triangles[index] = (y * hCount2) + x;
                        triangles[index + 1] = ((y + 1) * hCount2) + x;
                        triangles[index + 2] = (y * hCount2) + x + 1;

                        triangles[index + 3] = ((y + 1) * hCount2) + x;
                        triangles[index + 4] = ((y + 1) * hCount2) + x + 1;
                        triangles[index + 5] = (y * hCount2) + x + 1;
                        index += 6;
                    }
                }

                // Atualiza as propriedades da malha (vértices, UVs, triângulos, normais etc.)
                m.vertices = vertices;
                m.uv = uvs;
                m.triangles = triangles;
                m.tangents = tangents;
                m.RecalculateNormals();

                // Salva a malha recém-criada no local de salvamento para recarregar mais tarde
                AssetDatabase.CreateAsset(m, localSalvamentoAtivo + nomeMalhaPlano);
                AssetDatabase.SaveAssets();
            }

            // Atualiza malha
            filtroDeMalha.sharedMesh = m;
            m.RecalculateBounds();

            // Se adicionar colisor estiver definido como verdadeiro, adicione um colisor de caixa
            if (adicionarColisor)
                plano.AddComponent(typeof(BoxCollider));

            // Adicione LowPolyWater como componente
            plano.AddComponent<LowPolyWater>();

            Selection.activeObject = plano;
        }
    }
}
