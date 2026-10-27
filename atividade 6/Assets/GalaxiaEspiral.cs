using UnityEngine;

public class GalaxiaEspiral : MonoBehaviour
{
    [Header("Quantidade de partículas")]
    public int quantidadeEstrelas = 7000;
    public int quantidadeGas = 3500;
    public int quantidadeBulbo = 1500;
    public int quantidadeHalo = 600;

    [Header("Forma da galáxia")]
    public float raio = 10f;
    public int bracos = 2;
    [Tooltip("Quanto os braços se enrolam (em radianos até a borda)")]
    public float enrolamento = 3.5f;
    [Tooltip("Espalhamento das partículas em volta do braço")]
    public float dispersaoAngular = 0.3f;
    public float espessura = 0.25f;

    [Header("Orientação")]
    [Tooltip("Rotação aplicada ao objeto quando o jogo começa")]
    public Vector3 rotacaoInicial = new Vector3(134f, -20f, 9f);

    [Header("Movimento")]
    public float velocidadeRotacao = 0.6f;

    [Header("Cores")]
    public Color corCentro = new Color(1f, 0.82f, 0.5f);
    public Color corBorda = new Color(0.5f, 0.7f, 1f);
    public Color corPoeira = new Color(0.9f, 0.45f, 0.15f);
    public Color corHalo = new Color(0.3f, 0.5f, 1f);

    private class Grupo
    {
        public ParticleSystem ps;
        public ParticleSystem.Particle[] particulas;
        public float[] r;
        public float[] angulo;
        public float[] altura;
    }

    private Grupo halo;
    private Grupo estrelas;
    private Grupo gas;
    private Grupo bulbo;
    private Material material;

    void Start()
    {
        // Aplica a rotação (só a rotação, a posição não é alterada)
        transform.localEulerAngles = rotacaoInicial;

        material = CriarMaterial();

        // Halo: brilho azul bem sutil em volta do disco
        halo = CriarGrupo("Halo", quantidadeHalo);
        PreencherHalo(halo);

        // Gás: nuvens transparentes ao longo dos braços
        gas = CriarGrupo("Gas", quantidadeGas);
        PreencherBracos(gas, 1.0f, 0.8f, 2.0f, 0.03f, 0.09f, true);

        // Estrelas: pontinhos pequenos e brilhantes
        estrelas = CriarGrupo("Estrelas", quantidadeEstrelas);
        PreencherBracos(estrelas, 0.9f, 0.04f, 0.14f, 0.55f, 1f, false);

        // Bulbo: brilho amarelado no centro
        bulbo = CriarGrupo("Bulbo", quantidadeBulbo);
        PreencherBulbo(bulbo);
    }

    void Update()
    {
        Atualizar(halo);
        Atualizar(gas);
        Atualizar(estrelas);
        Atualizar(bulbo);
    }

    // ---------- Criação dos sistemas ----------

    Grupo CriarGrupo(string nome, int quantidade)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = quantidade;
        main.startLifetime = 1000000f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emissao = ps.emission;
        emissao.enabled = false;

        var forma = ps.shape;
        forma.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Emit(quantidade);
        ps.Play();

        Grupo g = new Grupo();
        g.ps = ps;
        g.particulas = new ParticleSystem.Particle[quantidade];
        g.r = new float[quantidade];
        g.angulo = new float[quantidade];
        g.altura = new float[quantidade];
        ps.GetParticles(g.particulas);
        return g;
    }

    void PreencherBracos(Grupo g, float expoenteRaio, float tamMin, float tamMax,
                         float alfaMin, float alfaMax, bool ehGas)
    {
        for (int i = 0; i < g.particulas.Length; i++)
        {
            // Mais partículas perto do centro
            float t = Mathf.Pow(Random.value, 1f + expoenteRaio * 0.6f);
            float r = Mathf.Max(0.3f, t * raio);
            float rNorm = r / raio;

            // Posição ao longo de um braço espiral + espalhamento
            int braco = Random.Range(0, bracos);
            float baseAngulo = braco * (2f * Mathf.PI / bracos);
            float angulo = baseAngulo + rNorm * enrolamento
                           + Gauss() * dispersaoAngular * (ehGas ? 1f : 0.7f);

            g.r[i] = r;
            g.angulo[i] = angulo;
            g.altura[i] = Gauss() * espessura * (1.2f - rNorm);

            // Cor: amarelada no centro, azulada na borda
            Color cor = Color.Lerp(corCentro, corBorda, Mathf.Pow(rNorm, 0.7f));
            if (ehGas)
            {
                // Poeira alaranjada misturada com gás azul
                Color poeira = Color.Lerp(corPoeira, corBorda, rNorm);
                cor = Color.Lerp(cor, poeira, Random.value);
            }
            cor.a = Random.Range(alfaMin, alfaMax);

            g.particulas[i].startColor = cor;
            g.particulas[i].startSize = Random.Range(tamMin, tamMax);
            g.particulas[i].startLifetime = 1000000f;
            g.particulas[i].remainingLifetime = 1000000f;
        }
    }

    void PreencherBulbo(Grupo g)
    {
        for (int i = 0; i < g.particulas.Length; i++)
        {
            float r = Mathf.Abs(Gauss()) * raio * 0.13f;
            g.r[i] = Mathf.Max(0.05f, r);
            g.angulo[i] = Random.value * Mathf.PI * 2f;
            g.altura[i] = Gauss() * espessura * 1.5f;

            // Mistura de branco, amarelo e um pouco de laranja
            Color cor = Color.Lerp(Color.white, corCentro, Random.value);
            cor = Color.Lerp(cor, corPoeira, Random.value * 0.35f);
            cor.a = Random.Range(0.04f, 0.12f);

            g.particulas[i].startColor = cor;
            g.particulas[i].startSize = Random.Range(0.8f, 2.2f);
            g.particulas[i].startLifetime = 1000000f;
            g.particulas[i].remainingLifetime = 1000000f;
        }
    }

    void PreencherHalo(Grupo g)
    {
        for (int i = 0; i < g.particulas.Length; i++)
        {
            float r = Mathf.Pow(Random.value, 0.8f) * raio * 1.0f;
            g.r[i] = Mathf.Max(0.3f, r);
            g.angulo[i] = Random.value * Mathf.PI * 2f;
            g.altura[i] = Gauss() * espessura;

            Color cor = Color.Lerp(corHalo, corBorda, Random.value);
            cor.a = Random.Range(0.015f, 0.035f);

            g.particulas[i].startColor = cor;
            g.particulas[i].startSize = Random.Range(2.5f, 4.5f);
            g.particulas[i].startLifetime = 1000000f;
            g.particulas[i].remainingLifetime = 1000000f;
        }
    }

    // ---------- Movimento ----------

    void Atualizar(Grupo g)
    {
        float tempo = Time.time;

        for (int i = 0; i < g.particulas.Length; i++)
        {
            float r = g.r[i];

            // Rotação diferencial: o centro gira mais rápido que a borda
            float omega = velocidadeRotacao * (raio * 0.15f) / (r + raio * 0.1f);
            float a = g.angulo[i] + omega * tempo;

            g.particulas[i].position = new Vector3(
                Mathf.Cos(a) * r,
                g.altura[i],
                Mathf.Sin(a) * r);
        }

        g.ps.SetParticles(g.particulas, g.particulas.Length);
    }

    // ---------- Auxiliares ----------

    // Número aleatório com distribuição normal (Box-Muller)
    float Gauss()
    {
        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }

    Material CriarMaterial()
    {
        // Textura de bolinha com borda suave (gerada por código)
        int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = (x - tam / 2f) / (tam / 2f);
                float dy = (y - tam / 2f) / (tam / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alfa = Mathf.Clamp01(1f - d);
                alfa = alfa * alfa;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
            }
        }
        tex.Apply();

        // Tenta shaders aditivos primeiro (brilho mais bonito)
        string[] shaders =
        {
            "Legacy Shaders/Particles/Additive",
            "Mobile/Particles/Additive",
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default"
        };

        Shader shader = null;
        foreach (string nome in shaders)
        {
            shader = Shader.Find(nome);
            if (shader != null) break;
        }

        Material mat = new Material(shader);
        mat.mainTexture = tex;
        return mat;
    }
}