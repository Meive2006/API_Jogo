using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class ApiManager : MonoBehaviour
{
    [Header("Login Inputs")]
    public TMP_InputField emailInputLogin;
    public TMP_InputField senhaInputLogin;

    [Header("Registro Inputs")]
    public TMP_InputField emailInputRegistro;
    public TMP_InputField senhaInputRegistro;
    public TMP_InputField usuarioInputRegistro;

    [Header("UI Feedback")]
    public TMP_Text mensagemErroUI;
    public TMP_Text mensagemSalvarUI;

    string apiUrl = "https://us-central1-api-jogo.cloudfunctions.net/api";

    void Start()
    {
        StartCoroutine(CarregarPosicao());
    }

    public void RegistrarUsuario()
    {
        StartCoroutine(Registrar());
    }

    public void FazerLogin()
    {
        StartCoroutine(Login());
    }

    public void SalvarProgresso()
    {
        StartCoroutine(Salvar());
    }

    IEnumerator Registrar()
    {
        string json = JsonUtility.ToJson(new Usuario
        {
            email = emailInputRegistro.text,
            senha = senhaInputRegistro.text,
            usuario = usuarioInputRegistro.text
        });

        using UnityWebRequest req = new UnityWebRequest(apiUrl + "/registrar", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Registrado com sucesso!");
            StartCoroutine(MostrarMensagemTemporaria(mensagemErroUI, "Registrado com sucesso!"));

            emailInputRegistro.text = "";
            senhaInputRegistro.text = "";
            usuarioInputRegistro.text = "";
        }
        else
        {
            StartCoroutine(MostrarMensagemTemporaria(mensagemErroUI, "Campos Inválidos!"));
            Debug.LogError("Erro: " + req.downloadHandler.text);
        }
    }

    IEnumerator Login()
    {
        string json = JsonUtility.ToJson(new Usuario
        {
            email = emailInputLogin.text,
            senha = senhaInputLogin.text
        });

        using UnityWebRequest req = new UnityWebRequest(apiUrl + "/login", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resposta = JsonUtility.FromJson<RespostaLogin>(req.downloadHandler.text);
            PlayerPrefs.SetString("uid", resposta.uid);
            PlayerPrefs.SetFloat("x", resposta.posicao.x);
            PlayerPrefs.SetFloat("y", resposta.posicao.y);
            PlayerPrefs.SetFloat("z", resposta.posicao.z);
            
            emailInputLogin.text = "";
            senhaInputLogin.text = "";

            SceneManager.LoadScene("Cena Principal");
        }
        else
        {
            StartCoroutine(MostrarMensagemTemporaria(mensagemErroUI, "E-mail ou senha inválidos!"));
            Debug.LogError("Erro: " + req.downloadHandler.text);
        }
    }

    IEnumerator Salvar()
    {
        string uid = PlayerPrefs.GetString("uid");
        if (string.IsNullOrEmpty(uid)) yield break;

        Transform player = GameObject.FindWithTag("Player").transform;

        string json = JsonUtility.ToJson(new SalvarData
        {
            uid = uid,
            x = player.position.x,
            y = player.position.y,
            z = player.position.z
        });

        using UnityWebRequest req = new UnityWebRequest(apiUrl + "/salvarProgresso", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            StartCoroutine(MostrarMensagemTemporaria(mensagemSalvarUI, "Progresso salvo com sucesso!"));
        else
            StartCoroutine(MostrarMensagemTemporaria(mensagemSalvarUI, "Erro ao salvar: " + req.downloadHandler.text));
    }
    IEnumerator CarregarPosicao()
    {
        string uid = PlayerPrefs.GetString("uid", "");
        if (string.IsNullOrEmpty(uid)) yield break;

         GameObject playerObj = null;
        while ((playerObj = GameObject.FindWithTag("Player")) == null)
        {
        yield return null;
        }

    Transform player = playerObj.transform;

        using UnityWebRequest req = UnityWebRequest.Get(apiUrl + "/carregarProgresso/" + uid);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            WrapperPosicao dados = JsonUtility.FromJson<WrapperPosicao>(req.downloadHandler.text);
            player.position = new Vector3(dados.posicao.x, dados.posicao.y, dados.posicao.z);
        }
        else
        {
            Debug.LogError("Erro ao carregar progresso: " + req.downloadHandler.text);
        }
    }

    IEnumerator MostrarMensagemTemporaria(TMP_Text alvo, string mensagem, float tempo = 3f)
{
    alvo.text = mensagem;
    yield return new WaitForSeconds(tempo);
    alvo.text = "";
}

    [System.Serializable]
    public class Usuario
    {
        public string email;
        public string senha;
        public string usuario;
    }

    [System.Serializable]
    public class RespostaLogin
    {
        public string uid;
        public string usuario;
        public Posicao posicao;
    }

    [System.Serializable]
    public class Posicao
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class SalvarData
    {
        public string uid;
        public float x;
        public float y;
        public float z;
    }
    
    [System.Serializable]
    public class WrapperPosicao
    {
        public Posicao posicao;
    }

}
